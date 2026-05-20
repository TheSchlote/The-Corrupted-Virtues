using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // All combat UI is built in code. A bare AddComponent<CanvasScaler>()
    // defaults to Constant Pixel Size, so fonts are a fixed pixel count and
    // render tiny on a 1440p/4K display and huge on a small one. This helper
    // builds the screen-overlay canvas once, with Scale With Screen Size, so
    // every canvas (HUD, VFX, swing meter) scales identically with resolution.
    public static class UiCanvas
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static Canvas CreateOverlay(string canvasName, Transform parent = null)
        {
            GameObject canvasObject = new GameObject(canvasName);
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
            }

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
