using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Transient combat feedback. For M1 this is the Gladius "Execution"
    // payoff: a brief tier callout (Fumble/Miss/Hit/Divine) coloured by
    // result. A primitive stand-in for real hit VFX behind the same seam.
    public sealed class VfxPresenter : MonoBehaviour
    {
        private CombatEvents events;
        private TMP_Text calloutText;
        private Coroutine calloutRoutine;

        public void Initialize(CombatEvents combatEvents)
        {
            events = combatEvents;
            BuildCanvas();
            events.ExecutionGraded += OnExecutionGraded;
        }

        private void OnDestroy()
        {
            if (events != null)
            {
                events.ExecutionGraded -= OnExecutionGraded;
            }
        }

        private void OnExecutionGraded(ExecutionGradedEvent e)
        {
            if (calloutText == null)
            {
                return;
            }

            calloutText.text = $"{e.Tier.ToString().ToUpperInvariant()}  x{e.Multiplier:0.00}";
            calloutText.color = ColorFor(e.Tier);

            if (calloutRoutine != null)
            {
                StopCoroutine(calloutRoutine);
            }

            calloutRoutine = StartCoroutine(ShowThenFade());
        }

        private IEnumerator ShowThenFade()
        {
            const float hold = 0.45f;
            const float fade = 0.55f;

            Color c = calloutText.color;
            c.a = 1f;
            calloutText.color = c;
            yield return new WaitForSeconds(hold);

            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(1f - elapsed / fade);
                calloutText.color = c;
                yield return null;
            }

            c.a = 0f;
            calloutText.color = c;
            calloutRoutine = null;
        }

        private static Color ColorFor(ExecutionResult tier)
        {
            switch (tier)
            {
                case ExecutionResult.Divine:
                    return new Color(0.4f, 0.7f, 1f);
                case ExecutionResult.Hit:
                    return new Color(0.45f, 0.9f, 0.5f);
                case ExecutionResult.Miss:
                    return new Color(0.95f, 0.8f, 0.35f);
                default:
                    return new Color(0.95f, 0.4f, 0.4f);
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("VfxCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject textObject = new GameObject("ExecutionCallout");
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.7f);
            rect.anchorMax = new Vector2(0.7f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            calloutText = textObject.AddComponent<TextMeshProUGUI>();
            calloutText.fontSize = 40;
            calloutText.alignment = TextAlignmentOptions.Center;
            calloutText.text = string.Empty;
            Color start = Color.white;
            start.a = 0f;
            calloutText.color = start;
        }
    }
}
