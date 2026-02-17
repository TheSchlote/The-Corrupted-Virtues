using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public sealed class CombatCameraGimbal : MonoBehaviour
    {
        [SerializeField] private float maxZoom = 3f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float zoomSpeed = 0.08f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -63f;
        [SerializeField] private float maxPitch = 17f;
        [SerializeField] private Vector3 baseCameraOffset = new(0f, 6f, -14f);

        private Transform _innerGimbal;
        private Camera _controlledCamera;
        private float _zoom = 1.5f;
        private float _pitch;
        private float _yaw;

        public void Setup(Camera cameraToControl)
        {
            if (cameraToControl == null)
            {
                Debug.LogError("CombatCameraGimbal requires a Camera instance.");
                return;
            }

            _controlledCamera = cameraToControl;
            _controlledCamera.transform.SetParent(_innerGimbal, false);
            _controlledCamera.transform.localRotation = Quaternion.identity;
            _controlledCamera.transform.localPosition = baseCameraOffset * _zoom;
        }

        private void Awake()
        {
            var inner = new GameObject("InnerGimbal");
            _innerGimbal = inner.transform;
            _innerGimbal.SetParent(transform, false);

            _pitch = _innerGimbal.localEulerAngles.x;
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            HandleCameraInput();
            UpdateCameraTransform();
        }

        private void HandleCameraInput()
        {
            if (PlayerInputBridge.IsRotateCameraHeld())
            {
                Vector2 pointerDelta = PlayerInputBridge.ReadPointerDelta();
                float deltaX = pointerDelta.x;
                float deltaY = pointerDelta.y;

                _yaw += deltaX * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch - (deltaY * mouseSensitivity), minPitch, maxPitch);
            }

            float scroll = PlayerInputBridge.ReadScrollDeltaY();
            if (!Mathf.Approximately(scroll, 0f))
            {
                _zoom = Mathf.Clamp(_zoom - (scroll * zoomSpeed), minZoom, maxZoom);
            }
        }

        private void UpdateCameraTransform()
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _innerGimbal.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            if (_controlledCamera != null)
            {
                _controlledCamera.transform.localPosition = baseCameraOffset * _zoom;
            }
        }
    }
}
