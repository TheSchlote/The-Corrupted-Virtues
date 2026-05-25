using TMPro;
using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Shared builder for the matching QTE UI: a large line of directional
    // prompts (the sequence to reproduce, recoloured as the player progresses)
    // plus a caption. Sits in the same screen band as the other QTEs.
    public static class MatchingUiFactory
    {
        public readonly struct MatchingUi
        {
            public readonly Canvas Canvas;
            public readonly GameObject Root;
            public readonly TMP_Text SequenceText;
            public readonly TMP_Text CaptionText;

            public MatchingUi(Canvas canvas, GameObject root, TMP_Text sequenceText, TMP_Text captionText)
            {
                Canvas = canvas;
                Root = root;
                SequenceText = sequenceText;
                CaptionText = captionText;
            }
        }

        public static MatchingUi Build(Transform parent = null)
        {
            Canvas canvas = null;
            Transform rootParent = parent;

            if (rootParent == null)
            {
                canvas = UiCanvas.CreateOverlay("MatchingCanvas");
                rootParent = canvas.transform;
            }

            GameObject root = new GameObject("Matching");
            root.transform.SetParent(rootParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            TMP_Text sequence = CreateText(root.transform, "SequenceText",
                new Vector2(0.20f, 0.155f), new Vector2(0.80f, 0.225f), fontSize: 46, "");
            TMP_Text caption = CreateText(root.transform, "CaptionText",
                new Vector2(0.30f, 0.125f), new Vector2(0.70f, 0.155f), fontSize: 20, "Matching ready.");

            return new MatchingUi(canvas, root, sequence, caption);
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, string initial)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.text = initial;

            return text;
        }
    }
}
