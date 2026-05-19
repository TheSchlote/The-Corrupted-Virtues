using UnityEngine;
using UnityEngine.InputSystem;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // IGameInput backed by the Unity Input System. Keyboard/mouse parity with
    // the original inline reads; gamepad added for the core loop (move /
    // confirm / reset / camera yaw). Stateless — reads live devices per query.
    public sealed class UnitySystemInput : IGameInput
    {
        private const float StickThreshold = 0.5f;

        public bool ConfirmPressed
        {
            get
            {
                Keyboard kb = Keyboard.current;
                Gamepad gp = Gamepad.current;
                bool keyboard = kb != null
                    && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
                bool gamepad = gp != null && gp.buttonSouth.wasPressedThisFrame;
                return keyboard || gamepad;
            }
        }

        public bool ResetPressed
        {
            get
            {
                Keyboard kb = Keyboard.current;
                Gamepad gp = Gamepad.current;
                bool keyboard = kb != null && kb.rKey.wasPressedThisFrame;
                bool gamepad = gp != null && gp.startButton.wasPressedThisFrame;
                return keyboard || gamepad;
            }
        }

        public Vector2Int MoveAxis
        {
            get
            {
                int x = 0;
                int y = 0;

                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                    {
                        x -= 1;
                    }
                    else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                    {
                        x += 1;
                    }

                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                    {
                        y -= 1;
                    }
                    else if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                    {
                        y += 1;
                    }
                }

                if (x == 0 && y == 0)
                {
                    Gamepad gp = Gamepad.current;
                    if (gp != null)
                    {
                        Vector2 stick = gp.leftStick.ReadValue();
                        if (gp.dpad.left.isPressed || stick.x < -StickThreshold)
                        {
                            x -= 1;
                        }
                        else if (gp.dpad.right.isPressed || stick.x > StickThreshold)
                        {
                            x += 1;
                        }

                        if (gp.dpad.down.isPressed || stick.y < -StickThreshold)
                        {
                            y -= 1;
                        }
                        else if (gp.dpad.up.isPressed || stick.y > StickThreshold)
                        {
                            y += 1;
                        }
                    }
                }

                return new Vector2Int(x, y);
            }
        }

        public float CameraYaw
        {
            get
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.qKey.isPressed)
                    {
                        return -1f;
                    }

                    if (kb.eKey.isPressed)
                    {
                        return 1f;
                    }
                }

                Gamepad gp = Gamepad.current;
                if (gp != null)
                {
                    if (gp.leftShoulder.isPressed)
                    {
                        return -1f;
                    }

                    if (gp.rightShoulder.isPressed)
                    {
                        return 1f;
                    }
                }

                return 0f;
            }
        }

        public bool CameraLookActive
        {
            get { return Mouse.current != null && Mouse.current.rightButton.isPressed; }
        }

        public Vector2 CameraLookDelta
        {
            get { return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; }
        }

        public bool CameraPanActive
        {
            get { return Mouse.current != null && Mouse.current.middleButton.isPressed; }
        }

        public Vector2 CameraPanDelta
        {
            get { return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; }
        }

        public float CameraZoomDelta
        {
            get { return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f; }
        }
    }
}
