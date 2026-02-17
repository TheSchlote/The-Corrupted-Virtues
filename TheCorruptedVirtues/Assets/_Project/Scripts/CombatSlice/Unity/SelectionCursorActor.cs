using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public sealed class SelectionCursorActor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float snapDelay = 0.2f;
        [SerializeField] private float snapSpeed = 0.2f;
        [SerializeField] private float heightTransitionSpeed = 8f;

        private CombatCameraGimbal _cameraGimbal;
        private TacticalPathfinding _pathfinding;

        private Vector3 _velocity;
        private Vector3 _targetGridPosition;
        private float _snapTimer;
        private bool _shouldSnap;
        private bool _snapInProgress;
        private Vector3 _snapStart;
        private float _snapElapsed;

        public void Setup(TacticalPathfinding pathfinding, CombatCameraGimbal cameraGimbal, Vector3 startPosition)
        {
            _pathfinding = pathfinding;
            _cameraGimbal = cameraGimbal;
            _targetGridPosition = startPosition;
            transform.position = startPosition;
        }

        public Vector3 GetSelectedTile()
        {
            return _targetGridPosition;
        }

        private void Update()
        {
            if (_pathfinding == null || _cameraGimbal == null)
            {
                return;
            }

            HandleInput(Time.deltaTime);
            MoveCursor(Time.deltaTime);
            UpdateSnap(Time.deltaTime);

            if (PlayerInputBridge.IsConfirmPressedDown())
            {
                Vector3Int hoveredCell = _pathfinding.WorldToCell(transform.position);
                Debug.Log($"Cursor is on tile: {hoveredCell}");
            }
        }

        private void HandleInput(float deltaTime)
        {
            Vector3 inputDirection = GetMovementDirection();
            if (inputDirection == Vector3.zero)
            {
                _velocity = Vector3.zero;
                HandleSnapTimer(deltaTime);
                return;
            }

            Vector3Int targetCell = _pathfinding.WorldToCell(
                transform.position + (inputDirection * _pathfinding.CellSize));
            targetCell = _pathfinding.GetBestAvailableCell(targetCell);

            if (_pathfinding.HasTile(targetCell))
            {
                _velocity = inputDirection * moveSpeed;
                _shouldSnap = true;
                _snapTimer = 0f;
                _snapInProgress = false;
            }
            else
            {
                _velocity = Vector3.zero;
                Debug.Log($"Blocked: Cannot move to {targetCell}.");
            }
        }

        private Vector3 GetMovementDirection()
        {
            Vector3 inputDirection = Vector3.zero;

            Vector3 forward = _cameraGimbal.transform.forward;
            Vector3 right = _cameraGimbal.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            if (PlayerInputBridge.IsMoveRightPressed())
            {
                inputDirection += right;
            }

            if (PlayerInputBridge.IsMoveLeftPressed())
            {
                inputDirection -= right;
            }

            if (PlayerInputBridge.IsMoveDownPressed())
            {
                inputDirection -= forward;
            }

            if (PlayerInputBridge.IsMoveUpPressed())
            {
                inputDirection += forward;
            }

            inputDirection.y = 0f;
            return inputDirection.normalized;
        }

        private void HandleSnapTimer(float deltaTime)
        {
            if (!_shouldSnap)
            {
                return;
            }

            _snapTimer += deltaTime;
            if (_snapTimer >= snapDelay)
            {
                BeginSnap();
                _shouldSnap = false;
            }
        }

        private void MoveCursor(float deltaTime)
        {
            if (_velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.position += _velocity * deltaTime;
            UpdateCursorHeight(deltaTime);
        }

        private void UpdateCursorHeight(float deltaTime)
        {
            Vector3Int targetCell = _pathfinding.GetBestAvailableCell(_pathfinding.WorldToCell(transform.position));
            float targetY = targetCell.y * _pathfinding.CellSize;

            Vector3 position = transform.position;
            position.y = Mathf.Lerp(position.y, targetY, deltaTime * heightTransitionSpeed);
            transform.position = position;
        }

        private void BeginSnap()
        {
            Vector3Int closestTile = _pathfinding.GetBestAvailableCell(_pathfinding.WorldToCell(transform.position));
            _targetGridPosition = _pathfinding.CellToWorld(closestTile);

            _snapStart = transform.position;
            _snapElapsed = 0f;
            _snapInProgress = true;
        }

        private void UpdateSnap(float deltaTime)
        {
            if (!_snapInProgress)
            {
                return;
            }

            _snapElapsed += deltaTime;
            float duration = Mathf.Max(0.01f, snapSpeed);
            float t = Mathf.Clamp01(_snapElapsed / duration);
            t = t * t * (3f - (2f * t));

            transform.position = Vector3.Lerp(_snapStart, _targetGridPosition, t);
            if (t >= 1f)
            {
                _snapInProgress = false;
            }
        }
    }
}
