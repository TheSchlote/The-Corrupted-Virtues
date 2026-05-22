using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Shared builder for the Gladius-style swing meter UI.
    public static class SwingMeterUiFactory
    {
        public readonly struct SwingMeterUi
        {
            public readonly Canvas Canvas;
            public readonly GameObject Root;
            public readonly Slider Slider;
            public readonly TMP_Text Text;
            // The three difficulty-dependent zones, handed back so the
            // controller can resize them to match the grader's window for the
            // ability being attempted (M2 slice 2). Fumble/Miss are fixed.
            public readonly RectTransform HitZone;
            public readonly RectTransform DivineZone;
            public readonly RectTransform OvershootZone;

            public SwingMeterUi(
                Canvas canvas,
                GameObject root,
                Slider slider,
                TMP_Text text,
                RectTransform hitZone,
                RectTransform divineZone,
                RectTransform overshootZone)
            {
                Canvas = canvas;
                Root = root;
                Slider = slider;
                Text = text;
                HitZone = hitZone;
                DivineZone = divineZone;
                OvershootZone = overshootZone;
            }
        }

        public static SwingMeterUi Build(Transform parent = null)
        {
            Canvas canvas = null;
            Transform rootParent = parent;

            if (rootParent == null)
            {
                canvas = CreateCanvas();
                rootParent = canvas.transform;
            }

            GameObject root = new GameObject("SwingMeter");
            root.transform.SetParent(rootParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Slider slider = CreateSlider(
                root.transform,
                out RectTransform hitZone,
                out RectTransform divineZone,
                out RectTransform overshootZone);
            TMP_Text text = CreateText(root.transform);

            return new SwingMeterUi(canvas, root, slider, text, hitZone, divineZone, overshootZone);
        }

        private static Canvas CreateCanvas()
        {
            return UiCanvas.CreateOverlay("SwingMeterCanvas");
        }

        private static Slider CreateSlider(
            Transform parent,
            out RectTransform hitZone,
            out RectTransform divineZone,
            out RectTransform overshootZone)
        {
            GameObject sliderObject = new GameObject("ExecutionSlider");
            sliderObject.transform.SetParent(parent, false);

            // A slim band low on the screen, clear of the battlefield and
            // above the HUD hint line (which sits at y 0.02-0.10).
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

            // Zone boundaries default to the Normal layout and mirror
            // ExecutionCalculator's constants — they're how the player reads
            // what tier they're aiming for. The Hit/Divine/Overshoot zones are
            // repositioned per ability by SwingMeterController.Begin so the
            // painted bands always match the grader. The overshoot zone past
            // Divine uses the same orange as Miss so the player learns
            // "orange = whiff" no matter where it appears.
            CreateZone(sliderObject.transform, "ZoneFumble", 0.0f, 0.20f, new Color(0.45f, 0.1f, 0.1f, 0.5f));
            CreateZone(sliderObject.transform, "ZoneMiss", 0.20f, 0.40f, new Color(0.55f, 0.4f, 0.1f, 0.5f));
            hitZone = CreateZone(sliderObject.transform, "ZoneHit", 0.40f, 0.85f, new Color(0.1f, 0.5f, 0.2f, 0.4f));
            divineZone = CreateZone(sliderObject.transform, "ZoneDivine", 0.85f, 0.92f, new Color(0.2f, 0.45f, 0.75f, 0.5f));
            overshootZone = CreateZone(sliderObject.transform, "ZoneOvershoot", 0.92f, 1.0f, new Color(0.55f, 0.4f, 0.1f, 0.5f));

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
            handleRect.sizeDelta = new Vector2(8.0f, 30.0f);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.95f, 0.95f, 0.95f, 1.0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static RectTransform CreateZone(Transform parent, string name, float minX, float maxX, Color color)
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

            return rectTransform;
        }

        private static TMP_Text CreateText(Transform parent)
        {
            GameObject textObject = new GameObject("SwingMeterText");
            textObject.transform.SetParent(parent, false);

            // Caption sitting just above the slim meter. Font matches the HUD
            // body text; the canvas scaler keeps it readable at any resolution.
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.30f, 0.20f);
            rectTransform.anchorMax = new Vector2(0.70f, 0.255f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "Swing meter ready.";

            return text;
        }
    }
}
