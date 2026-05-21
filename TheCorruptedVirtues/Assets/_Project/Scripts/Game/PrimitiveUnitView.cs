using System.Collections;
using UnityEngine;

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
        private Camera billboardCamera;

        public void Configure(Renderer renderer, Color color)
        {
            primitiveRenderer = renderer;
            baseColor = color;
            ViewMaterials.SetColor(primitiveRenderer, color);
            BuildHpBar();
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
            if (hpBarFill == null)
            {
                return;
            }

            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            // Quads pivot at center, so scaling shrinks both ends. Offset the
            // fill leftward by the missing half-width so it drains right-to-left
            // like every HP bar in the genre.
            Vector3 scale = hpBarFill.localScale;
            scale.x = HpBarWidth * ratio;
            hpBarFill.localScale = scale;

            Vector3 pos = hpBarFill.localPosition;
            pos.x = -0.5f * HpBarWidth * (1f - ratio);
            hpBarFill.localPosition = pos;
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
