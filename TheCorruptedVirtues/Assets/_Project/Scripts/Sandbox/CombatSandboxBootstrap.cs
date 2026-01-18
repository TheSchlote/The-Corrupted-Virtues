using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.Combat
{
    // Creates a minimal UI harness for the combat sandbox at runtime.
    public sealed class CombatSandboxBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Canvas canvas = CreateCanvas();
            Slider slider = CreateSlider(canvas.transform);
            TMP_Text text = CreateText(canvas.transform);

            CombatSandboxController controller = gameObject.AddComponent<CombatSandboxController>();
            controller.SetReferences(slider, text);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("CombatSandboxCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        private static Slider CreateSlider(Transform parent)
        {
            GameObject sliderObject = new GameObject("ResonanceSlider");
            sliderObject.transform.SetParent(parent, false);

            RectTransform rectTransform = sliderObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.6f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image backgroundImage = sliderObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0.0f;
            slider.maxValue = 1.0f;
            slider.value = 0.0f;

            CreateZone(sliderObject.transform, "ZoneFumble", 0.0f, 0.20f, new Color(0.45f, 0.1f, 0.1f, 0.5f));
            CreateZone(sliderObject.transform, "ZoneMiss", 0.20f, 0.40f, new Color(0.55f, 0.4f, 0.1f, 0.5f));
            CreateZone(sliderObject.transform, "ZoneHit", 0.40f, 0.80f, new Color(0.1f, 0.5f, 0.2f, 0.4f));
            CreateZone(sliderObject.transform, "ZoneDivine", 0.80f, 0.95f, new Color(0.2f, 0.45f, 0.75f, 0.5f));

            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1.0f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.85f, 0.35f, 0.15f, 1.0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(sliderObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20.0f, 20.0f);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.95f, 0.95f, 0.95f, 1.0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static void CreateZone(Transform parent, string name, float minX, float maxX, Color color)
        {
            GameObject zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(parent, false);

            RectTransform rectTransform = zoneObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(minX, 0.25f);
            rectTransform.anchorMax = new Vector2(maxX, 0.75f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image zoneImage = zoneObject.AddComponent<Image>();
            zoneImage.color = color;
            zoneImage.raycastTarget = false;
        }

        private static TMP_Text CreateText(Transform parent)
        {
            GameObject textObject = new GameObject("CombatSandboxText");
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = "Combat sandbox ready.";

            return text;
        }
    }
}
