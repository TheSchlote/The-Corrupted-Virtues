using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Shared builder for the timed-press QTE UI: a static track with a painted
    // target (a wide Hit band and a narrow Divine core, both centred) and a thin
    // marker that sweeps across once. Sits in the same screen band as the swing
    // meter and button mash so the QTE family reads as one thing. The controller
    // paints the Hit/Divine zones per difficulty and drives the marker position.
    public static class TimedPressUiFactory
    {
        public readonly struct TimedPressUi
        {
            public readonly Canvas Canvas;
            public readonly GameObject Root;
            public readonly RectTransform Marker;
            public readonly RectTransform HitZone;
            public readonly RectTransform DivineZone;
            public readonly TMP_Text Text;

            public TimedPressUi(Canvas canvas, GameObject root, RectTransform marker, RectTransform hitZone, RectTransform divineZone, TMP_Text text)
            {
                Canvas = canvas;
                Root = root;
                Marker = marker;
                HitZone = hitZone;
                DivineZone = divineZone;
                Text = text;
            }
        }

        public static TimedPressUi Build(Transform parent = null)
        {
            Canvas canvas = null;
            Transform rootParent = parent;

            if (rootParent == null)
            {
                canvas = UiCanvas.CreateOverlay("TimedPressCanvas");
                rootParent = canvas.transform;
            }

            GameObject root = new GameObject("TimedPress");
            root.transform.SetParent(rootParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            RectTransform track = CreateTrack(root.transform);
            RectTransform hitZone = CreateZone(track, "HitZone", new Color(0.25f, 0.7f, 0.35f, 0.55f));
            RectTransform divineZone = CreateZone(track, "DivineZone", new Color(1f, 0.85f, 0.3f, 0.85f));
            RectTransform marker = CreateMarker(track);
            TMP_Text text = CreateText(root.transform);

            return new TimedPressUi(canvas, root, marker, hitZone, divineZone, text);
        }

        private static RectTransform CreateTrack(Transform parent)
        {
            GameObject trackObject = new GameObject("Track");
            trackObject.transform.SetParent(parent, false);

            // Same slim band as the swing meter / button mash.
            RectTransform rectTransform = trackObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.30f, 0.15f);
            rectTransform.anchorMax = new Vector2(0.70f, 0.19f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image backgroundImage = trackObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);
            backgroundImage.raycastTarget = false;

            return rectTransform;
        }

        // A horizontal band on the track; the controller sets its anchor x-range
        // to match the grader's window for the current difficulty.
        private static RectTransform CreateZone(Transform track, string name, Color color)
        {
            GameObject zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(track, false);

            RectTransform rectTransform = zoneObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.4f, 0.0f);
            rectTransform.anchorMax = new Vector2(0.6f, 1.0f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = zoneObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return rectTransform;
        }

        private static RectTransform CreateMarker(Transform track)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.transform.SetParent(track, false);

            RectTransform rectTransform = markerObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.0f, -0.15f);
            rectTransform.anchorMax = new Vector2(0.012f, 1.15f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = markerObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            return rectTransform;
        }

        private static TMP_Text CreateText(Transform parent)
        {
            GameObject textObject = new GameObject("TimedPressText");
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.30f, 0.20f);
            rectTransform.anchorMax = new Vector2(0.70f, 0.255f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "Timed press ready.";

            return text;
        }
    }
}
