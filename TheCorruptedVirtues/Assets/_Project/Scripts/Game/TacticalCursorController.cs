using UnityEngine;
using UnityEngine.InputSystem;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Handles cursor position and input for grid navigation.
    public sealed class TacticalCursorController : MonoBehaviour
    {
        [SerializeField] private GridPresenter gridPresenter;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float repeatDelaySeconds = 0.18f;

        public GridCoord CursorCoord { get; private set; }
        public bool IsLocked { get; set; }

        private float inputCooldown;

        public void Initialize(GridCoord startCoord)
        {
            CursorCoord = startCoord;
            SnapToGrid();
        }

        public bool TryMoveCursor()
        {
            if (IsLocked || gridPresenter == null)
            {
                return false;
            }

            Vector2Int delta = ReadDirectionalInput();
            if (delta == Vector2Int.zero)
            {
                inputCooldown = 0.0f;
                return false;
            }

            if (inputCooldown > 0.0f)
            {
                inputCooldown -= Time.deltaTime;
                return false;
            }

            inputCooldown = repeatDelaySeconds;
            Vector2Int step = GetCameraRelativeStep(delta);
            if (step == Vector2Int.zero)
            {
                return false;
            }

            GridCoord target = new GridCoord(CursorCoord.X + step.x, CursorCoord.Y + step.y);
            GridBounds bounds = gridPresenter.Bounds;
            if (!bounds.Contains(target))
            {
                return false;
            }

            CursorCoord = target;
            SnapToGrid();
            return true;
        }

        public void SnapToGrid()
        {
            if (gridPresenter == null)
            {
                return;
            }

            Vector3 world = gridPresenter.GridToWorld(CursorCoord, gridPresenter.CursorY);
            transform.position = world;
        }

        private static Vector2Int ReadDirectionalInput()
        {
            int x = 0;
            int y = 0;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    x -= 1;
                }
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    x += 1;
                }

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    y -= 1;
                }
                else if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    y += 1;
                }
            }

            return new Vector2Int(x, y);
        }

        private Vector2Int GetCameraRelativeStep(Vector2Int input)
        {
            Transform cam = cameraTransform;
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main.transform;
            }

            int inputX = input.x;
            int inputY = input.y;
            if (inputX != 0 && inputY != 0)
            {
                if (Mathf.Abs(inputX) >= Mathf.Abs(inputY))
                {
                    inputY = 0;
                }
                else
                {
                    inputX = 0;
                }
            }

            if (cam == null)
            {
                return new Vector2Int(inputX, inputY);
            }

            Vector3 forward = cam.forward;
            forward.y = 0.0f;
            forward.Normalize();
            Vector3 right = cam.right;
            right.y = 0.0f;
            right.Normalize();

            Vector3 worldDir = forward * inputY + right * inputX;
            if (worldDir.sqrMagnitude <= 0.0001f)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(worldDir.x) >= Mathf.Abs(worldDir.z))
            {
                return new Vector2Int(worldDir.x > 0 ? 1 : -1, 0);
            }

            return new Vector2Int(0, worldDir.z > 0 ? 1 : -1);
        }
    }
}
