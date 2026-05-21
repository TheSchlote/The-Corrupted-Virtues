using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Device-agnostic game input seam. Controllers read intent here instead
    // of touching Keyboard / Mouse / Gamepad directly, so input is swappable
    // and testable and keyboard/mouse + gamepad are supported from day one.
    public interface IGameInput
    {
        // Edge-triggered: true only on the frame the action is pressed.
        bool ConfirmPressed { get; }
        bool ResetPressed { get; }
        bool EndTurnPressed { get; }

        // Held grid-navigation intent; each axis is -1, 0, or 1.
        Vector2Int MoveAxis { get; }

        // Held camera-yaw nudge: -1, 0, or 1.
        float CameraYaw { get; }

        // Free-look: active while held; CameraLookDelta is the per-frame delta.
        bool CameraLookActive { get; }
        Vector2 CameraLookDelta { get; }

        // Screen-pan: active while held; CameraPanDelta is the per-frame delta.
        bool CameraPanActive { get; }
        Vector2 CameraPanDelta { get; }

        // Zoom delta this frame (wheel); positive = zoom in.
        float CameraZoomDelta { get; }
    }
}
