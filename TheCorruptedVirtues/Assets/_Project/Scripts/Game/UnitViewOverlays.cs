using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // View-side HUD that floats above any unit visual — HP bar, damage-preview
    // overlay, and active-turn ring — billboarded to the tactical camera. Pulled
    // out so the model view renders the same affordances as the primitive one;
    // holds no gameplay state. (PrimitiveUnitView still has its own inline copy
    // for now; it can migrate onto this once the model path is confirmed on
    // screen. Boss aura / facing arrow live only on the primitive path so far.)
    public sealed class UnitViewOverlays : MonoBehaviour
    {
        private const float HpBarHeight = 1.4f;
        private const float HpBarWidth = 1.05f;
        private const float HpBarThickness = 0.14f;

        private Color baseColor;

        private Transform hpBarRoot;
        private Transform hpBarFill;
        private Transform hpBarPreview;
        private Transform activeIndicator;
        private Camera billboardCamera;

        private int cachedCurrentHp;
        private int cachedMaxHp;

        public void Init(Color elementColor, bool isBoss)
        {
            baseColor = elementColor;
            BuildHpBar(isBoss);
            BuildActiveIndicator();
        }

        public void UpdateHp(int current, int max)
        {
            cachedCurrentHp = current;
            cachedMaxHp = max;

            if (hpBarFill == null)
            {
                return;
            }

            SetBarSegment(hpBarFill, 0f, Ratio(current, max));
        }

        public void ShowDamagePreview(int previewDamage)
        {
            if (hpBarPreview == null || cachedMaxHp <= 0)
            {
                return;
            }

            int afterHp = Mathf.Max(0, cachedCurrentHp - previewDamage);
            float currentRatio = Ratio(cachedCurrentHp, cachedMaxHp);
            float afterRatio = Ratio(afterHp, cachedMaxHp);

            if (afterRatio >= currentRatio - 1e-4f)
            {
                hpBarPreview.gameObject.SetActive(false);
                return;
            }

            hpBarPreview.gameObject.SetActive(true);
            SetBarSegment(hpBarPreview, afterRatio, currentRatio);
        }

        public void ClearDamagePreview()
        {
            if (hpBarPreview != null)
            {
                hpBarPreview.gameObject.SetActive(false);
            }
        }

        public void SetActiveIndicator(bool active)
        {
            if (activeIndicator != null)
            {
                activeIndicator.gameObject.SetActive(active);
            }
        }

        private void LateUpdate()
        {
            if (hpBarRoot == null)
            {
                return;
            }

            if (billboardCamera == null)
            {
                billboardCamera = Camera.main;
            }

            // Quad's visible face has normal -Z, so point +Z away from the camera
            // to keep the readable side toward it (same trick as the primitive HUD).
            if (billboardCamera != null)
            {
                Vector3 awayFromCamera = hpBarRoot.position - billboardCamera.transform.position;
                if (awayFromCamera.sqrMagnitude > 0.0001f)
                {
                    hpBarRoot.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
                }
            }
        }

        private void BuildHpBar(bool isBoss)
        {
            hpBarRoot = new GameObject("HpBar").transform;
            hpBarRoot.SetParent(transform, false);
            hpBarRoot.localPosition = new Vector3(0f, HpBarHeight, 0f);

            CreateBarQuad("HpBarBackground", hpBarRoot, HpBarWidth, new Color(0.08f, 0.08f, 0.08f, 1f), 0f);

            // Boss pools read as a violet Corruption gauge; everyone else green.
            Color fillColor = isBoss
                ? new Color(0.66f, 0.30f, 0.85f, 1f)
                : new Color(0.45f, 0.9f, 0.45f, 1f);
            hpBarFill = CreateBarQuad("HpBarFill", hpBarRoot, HpBarWidth, fillColor, -0.005f);

            hpBarPreview = CreateBarQuad("HpBarPreview", hpBarRoot, HpBarWidth, new Color(0.95f, 0.35f, 0.3f, 1f), -0.01f);
            hpBarPreview.gameObject.SetActive(false);
        }

        // Place a bar segment between two normalised positions in [0,1] along the
        // bar; quads pivot at centre, so recentre by the segment midpoint.
        private static void SetBarSegment(Transform segment, float fromNormalized, float toNormalized)
        {
            float from = Mathf.Clamp01(fromNormalized);
            float to = Mathf.Clamp01(toNormalized);
            if (to < from)
            {
                float swap = from;
                from = to;
                to = swap;
            }

            float width = to - from;
            float center = (from + to) * 0.5f;

            Vector3 scale = segment.localScale;
            scale.x = HpBarWidth * width;
            segment.localScale = scale;

            Vector3 pos = segment.localPosition;
            pos.x = HpBarWidth * (center - 0.5f);
            segment.localPosition = pos;
        }

        private static float Ratio(int current, int max)
        {
            return max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        }

        private void BuildActiveIndicator()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ActiveIndicator";
            quad.transform.SetParent(transform, false);
            // Lay the quad flat (face up) so it reads as a ring from the tactical
            // camera looking down.
            quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            quad.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            Renderer quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.material = ViewMaterials.CreateColored(Color.Lerp(baseColor, Color.white, 0.45f));
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            activeIndicator = quad.transform;
            activeIndicator.gameObject.SetActive(false);
        }

        private Transform CreateBarQuad(string objectName, Transform parent, float width, Color color, float localOffsetZ)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = new Vector3(width, HpBarThickness, 1f);
            quad.transform.localPosition = new Vector3(0f, 0f, localOffsetZ);

            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            Renderer quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.material = ViewMaterials.CreateColored(color);
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            return quad.transform;
        }
    }
}
