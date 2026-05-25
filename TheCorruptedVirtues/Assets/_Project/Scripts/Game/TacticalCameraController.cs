using UnityEngine;

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

        // Injected by CombatSliceBootstrap before Start() so the camera follows
        // the cursor without a fragile GameObject.Find("TacticalCursor") by name.
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        private void Start()
        {
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
            IGameInput input = GameInput.Current;

            yaw += input.CameraYaw * yawSpeed * Time.deltaTime;

            if (input.CameraLookActive)
            {
                Vector2 lookDelta = input.CameraLookDelta;
                yaw += lookDelta.x * mouseLookSensitivity;
                pitch -= lookDelta.y * mouseLookSensitivity;
            }

            float scroll = input.CameraZoomDelta;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * zoomSpeed * 0.01f;
            }

            if (input.CameraPanActive)
            {
                Vector2 panDelta = input.CameraPanDelta;
                Vector3 right = transform.right;
                Vector3 forward = Vector3.Cross(Vector3.up, right).normalized;
                Vector3 panMove = (-right * panDelta.x - forward * panDelta.y) * panSpeed;
                panOffset += panMove;
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
