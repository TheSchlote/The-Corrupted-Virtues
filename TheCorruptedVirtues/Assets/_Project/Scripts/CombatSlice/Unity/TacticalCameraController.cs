using UnityEngine;
using UnityEngine.InputSystem;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Simple orbit camera that follows a target with mouse/keyboard controls.
    public sealed class TacticalCameraController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 focusOffset = Vector3.zero;
        [SerializeField] private float followLerp = 12f;
        [SerializeField] private float yawSpeed = 120f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float mouseLookSensitivity = 0.4f;
        [SerializeField] private float panSpeed = 0.02f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float minPitch = 20f;
        [SerializeField] private float maxPitch = 65f;

        private Vector3 focusPoint;
        private Vector3 panOffset;
        private float yaw;
        private float pitch = 45f;
        private float distance = 10f;

        private void Start()
        {
            if (followTarget == null)
            {
                GameObject cursorObject = GameObject.Find("TacticalCursor");
                if (cursorObject != null)
                {
                    followTarget = cursorObject.transform;
                }
            }

            if (followTarget != null)
            {
                focusPoint = followTarget.position + focusOffset;
            }
            else
            {
                focusPoint = transform.position;
            }

            Vector3 offset = transform.position - focusPoint;
            distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            if (offset.sqrMagnitude > 0.0001f)
            {
                yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                pitch = Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }
        }

        private void LateUpdate()
        {
            UpdateFocus();
            UpdateInput();
            ApplyCameraTransform();
        }

        private void UpdateFocus()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 desired = followTarget.position + focusOffset + panOffset;
            float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
            focusPoint = Vector3.Lerp(focusPoint, desired, t);
        }

        private void UpdateInput()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed)
                {
                    yaw -= yawSpeed * Time.deltaTime;
                }
                else if (Keyboard.current.eKey.isPressed)
                {
                    yaw += yawSpeed * Time.deltaTime;
                }
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed)
                {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    yaw += delta.x * mouseLookSensitivity;
                    pitch -= delta.y * mouseLookSensitivity;
                }

                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance -= scroll * zoomSpeed * 0.01f;
                }

                if (Mouse.current.middleButton.isPressed)
                {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    Vector3 right = transform.right;
                    Vector3 forward = Vector3.Cross(Vector3.up, right).normalized;
                    Vector3 panDelta = (-right * delta.x - forward * delta.y) * panSpeed;
                    panOffset += panDelta;
                }
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        private void ApplyCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = focusPoint + offset;
            transform.rotation = rotation;
        }
    }
}
