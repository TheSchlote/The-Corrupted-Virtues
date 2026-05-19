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

        private Vector3 target;
        private Renderer primitiveRenderer;
        private Color baseColor;
        private Coroutine flashRoutine;

        public void Configure(Renderer renderer, Color color)
        {
            primitiveRenderer = renderer;
            baseColor = color;
            if (primitiveRenderer != null)
            {
                primitiveRenderer.material.color = color;
            }
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
            if (primitiveRenderer == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
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

        private IEnumerator Flash()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.PingPong(elapsed / duration * 2f, 1f);
                primitiveRenderer.material.color = Color.Lerp(baseColor, Color.white, k);
                yield return null;
            }

            primitiveRenderer.material.color = baseColor;
            flashRoutine = null;
        }
    }
}
