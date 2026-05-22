using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Shared builder for the button-mash QTE UI: a fill bar that grows with
    // presses plus a caption (count + countdown). Mirrors SwingMeterUiFactory's
    // shape so the two QTE types sit in the same screen band.
    public static class ButtonMashUiFactory
    {
        public readonly struct ButtonMashUi
        {
            public readonly Canvas Canvas;
            public readonly GameObject Root;
            public readonly Slider Slider;
            public readonly TMP_Text Text;

            public ButtonMashUi(Canvas canvas, GameObject root, Slider slider, TMP_Text text)
            {
                Canvas = canvas;
                Root = root;
                Slider = slider;
                Text = text;
            }
        }

        public static ButtonMashUi Build(Transform parent = null)
        {
            Canvas canvas = null;
            Transform rootParent = parent;

            if (rootParent == null)
            {
                canvas = UiCanvas.CreateOverlay("ButtonMashCanvas");
                rootParent = canvas.transform;
            }

            GameObject root = new GameObject("ButtonMash");
            root.transform.SetParent(rootParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Slider slider = CreateFillBar(root.transform);
            TMP_Text text = CreateText(root.transform);

            return new ButtonMashUi(canvas, root, slider, text);
        }

        private static Slider CreateFillBar(Transform parent)
        {
            GameObject sliderObject = new GameObject("MashBar");
            sliderObject.transform.SetParent(parent, false);

            // Same slim band as the swing meter so the two QTEs feel related.
            RectTransform rectTransform = sliderObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.30f, 0.15f);
            rectTransform.anchorMax = new Vector2(0.70f, 0.19f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image backgroundImage = sliderObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0.0f;
            slider.maxValue = 1.0f;
            slider.value = 0.0f;
            slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            // "Reach the end" target marker so the player sees the goal.
            GameObject targetMark = new GameObject("TargetMark");
            targetMark.transform.SetParent(sliderObject.transform, false);
            RectTransform markRect = targetMark.AddComponent<RectTransform>();
            markRect.anchorMin = new Vector2(0.97f, 0.1f);
            markRect.anchorMax = new Vector2(1.0f, 0.9f);
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            Image markImage = targetMark.AddComponent<Image>();
            markImage.color = new Color(0.2f, 0.45f, 0.75f, 0.6f);
            markImage.raycastTarget = false;

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
            fillImage.color = new Color(0.25f, 0.8f, 0.85f, 1.0f);

            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static TMP_Text CreateText(Transform parent)
        {
            GameObject textObject = new GameObject("MashText");
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.30f, 0.20f);
            rectTransform.anchorMax = new Vector2(0.70f, 0.255f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "Button mash ready.";

            return text;
        }
    }
}
