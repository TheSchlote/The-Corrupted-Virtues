using System.Collections;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Primitive-backed IUnitView: a capsule/cube that eases toward its
    // target cell and flashes when hit. Replaced wholesale once real models
    // exist; nothing else changes.
    public sealed class PrimitiveUnitView : MonoBehaviour, IUnitView
    {
        private const float MoveLerp = 14f;
        private const float HpBarHeight = 1.4f;
        private const float HpBarWidth = 1.05f;
        private const float HpBarThickness = 0.14f;

        private Vector3 target;
        private Renderer primitiveRenderer;
        private Color baseColor;
        private Coroutine flashRoutine;

        private Transform hpBarRoot;
        private Transform hpBarFill;
        private Transform hpBarPreview;
        private Transform activeIndicator;
        private Transform facingArrow;
        private Camera billboardCamera;

        private int cachedCurrentHp;
        private int cachedMaxHp;

        public void Configure(Renderer renderer, Color color)
        {
            primitiveRenderer = renderer;
            baseColor = color;
            ViewMaterials.SetColor(primitiveRenderer, color);
            BuildHpBar();
            BuildActiveIndicator();
            BuildFacingArrow();
        }

        public void Warp(Vector3 world)
        {
            target = world;
            transform.position = world;
        }

        public void MoveTo(Vector3 world)
        {
            target = world;
        }

        public void PlayHitFlash()
        {
            // Nothing to flash, and StartCoroutine throws on an inactive
            // GameObject (e.g. a hidden, already-dead unit).
            if (primitiveRenderer == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
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

        public void SetFacing(Facing facing)
        {
            if (facingArrow != null)
            {
                facingArrow.localRotation = Quaternion.Euler(0f, YawFor(facing), 0f);
            }
        }

        private static float YawFor(Facing facing)
        {
            // The tick points +Z by default = North; yaw clockwise about Y for
            // the rest (grid X -> world X, grid Y -> world Z).
            switch (facing)
            {
                case Facing.East: return 90f;
                case Facing.South: return 180f;
                case Facing.West: return 270f;
                default: return 0f; // North
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Despawn()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-MoveLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
        }

        private void LateUpdate()
        {
            if (hpBarRoot == null)
            {
                return;
            }

            // Billboard the bar to the camera so it stays readable from the
            // tactical angle without needing a real world-space Canvas.
            if (billboardCamera == null)
            {
                billboardCamera = Camera.main;
            }

            if (billboardCamera != null)
            {
                // Unity Quad's visible face has normal -Z. To make that face
                // the camera, we point +Z *away* from the camera so the
                // visible side (-Z) ends up facing the camera. Pointing +Z at
                // the camera would put the culled back-face toward us and the
                // bar would vanish.
                Vector3 awayFromCamera = hpBarRoot.position - billboardCamera.transform.position;
                if (awayFromCamera.sqrMagnitude > 0.0001f)
                {
                    hpBarRoot.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
                }
            }
        }

        private void BuildHpBar()
        {
            hpBarRoot = new GameObject("HpBar").transform;
            hpBarRoot.SetParent(transform, false);
            hpBarRoot.localPosition = new Vector3(0f, HpBarHeight, 0f);

            // Background — full-width dark plate.
            CreateBarQuad(
                "HpBarBackground",
                hpBarRoot,
                width: HpBarWidth,
                color: new Color(0.08f, 0.08f, 0.08f, 1f),
                localOffsetZ: 0f);

            // Fill — green, sits in front of the background; scaled by UpdateHp.
            hpBarFill = CreateBarQuad(
                "HpBarFill",
                hpBarRoot,
                width: HpBarWidth,
                color: new Color(0.45f, 0.9f, 0.45f, 1f),
                localOffsetZ: -0.005f);

            // Damage preview — muted red, sits in front of the fill. Hidden by
            // default; ShowDamagePreview positions/scales it to span the
            // would-be-lost portion of the bar.
            hpBarPreview = CreateBarQuad(
                "HpBarPreview",
                hpBarRoot,
                width: HpBarWidth,
                color: new Color(0.95f, 0.35f, 0.3f, 1f),
                localOffsetZ: -0.01f);
            hpBarPreview.gameObject.SetActive(false);
        }

        // Helper: place a bar segment between two normalised positions in
        // [0,1] along the bar (0 = left edge, 1 = right edge).
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
            // Quads pivot at center: scaling shrinks both ends, so we have to
            // recentre by the segment midpoint expressed in bar-local space.
            // Bar local-x ranges from -HpBarWidth/2 to +HpBarWidth/2.
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
            // A flat disc-ish quad just above the ground, larger than the
            // unit footprint, tinted with the unit's element colour. Visible
            // only when SetActiveIndicator(true) — gives an at-a-glance "it's
            // this unit's turn" cue without depending on real assets.
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ActiveIndicator";
            quad.transform.SetParent(transform, false);
            // Quad default normal is +Z. Rotating -90 around X tilts it so
            // the visible face points up (+Y) — readable from the tactical
            // camera which looks down.
            quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            quad.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            quad.transform.localPosition = new Vector3(0f, -0.49f, 0f);

            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            Renderer quadRenderer = quad.GetComponent<Renderer>();
            // Brighter than the body colour so it pops, but transparent-ish
            // feel via desaturation toward white.
            Color tint = Color.Lerp(baseColor, Color.white, 0.45f);
            quadRenderer.material = ViewMaterials.CreateColored(tint);
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            activeIndicator = quad.transform;
            activeIndicator.gameObject.SetActive(false);
        }

        private void BuildFacingArrow()
        {
            // A holder at the unit base that we yaw to the facing; a thin tick
            // poking out the front reads as "this way". Placeholder shape — a
            // real arrow arrives with art.
            facingArrow = new GameObject("FacingArrow").transform;
            facingArrow.SetParent(transform, false);
            facingArrow.localPosition = new Vector3(0f, -0.45f, 0f);

            GameObject tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = "FacingTick";
            tick.transform.SetParent(facingArrow, false);
            tick.transform.localScale = new Vector3(0.16f, 0.06f, 0.6f);
            tick.transform.localPosition = new Vector3(0f, 0f, 0.55f);

            Collider tickCollider = tick.GetComponent<Collider>();
            if (tickCollider != null)
            {
                Destroy(tickCollider);
            }

            Renderer tickRenderer = tick.GetComponent<Renderer>();
            Color tint = Color.Lerp(baseColor, Color.white, 0.6f);
            tickRenderer.material = ViewMaterials.CreateColored(tint);
            tickRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tickRenderer.receiveShadows = false;
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

        private IEnumerator Flash()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.PingPong(elapsed / duration * 2f, 1f);
                ViewMaterials.SetColor(primitiveRenderer, Color.Lerp(baseColor, Color.white, k));
                yield return null;
            }

            ViewMaterials.SetColor(primitiveRenderer, baseColor);
            flashRoutine = null;
        }
    }
}
