using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public sealed class UnitActor : MonoBehaviour
    {
        private const float CellsPerSecond = 4f;
        private const float CellSize = CombatMapData.CellSize;

        public bool IsMoving { get; private set; }

        public IEnumerator MoveTo(Vector3 destination, TacticalPathfinding pathfinding)
        {
            if (pathfinding == null)
            {
                yield break;
            }

            if (IsMoving)
            {
                yield break;
            }

            IsMoving = true;

            Vector3Int currentCell = pathfinding.WorldToCell(transform.position);
            pathfinding.MarkCellUnoccupied(currentCell);

            List<Vector3> path = pathfinding.GetPath(transform.position, destination);
            if (path.Count == 0)
            {
                Debug.Log("Invalid path. Restoring occupancy of current cell.");
                pathfinding.MarkCellOccupied(currentCell);
                IsMoving = false;
                yield break;
            }

            yield return MoveAlongPath(path);

            Vector3Int destinationCell = pathfinding.WorldToCell(transform.position);
            pathfinding.MarkCellOccupied(destinationCell);
            IsMoving = false;
        }

        private IEnumerator MoveAlongPath(IReadOnlyList<Vector3> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 targetPosition = path[i];
                Vector3 startPosition = transform.position;

                float distance = Vector3.Distance(startPosition, targetPosition);
                if (distance <= Mathf.Epsilon)
                {
                    continue;
                }

                float moveTime = distance / (CellsPerSecond * CellSize);
                float elapsed = 0f;

                while (elapsed < moveTime)
                {
                    float t = elapsed / moveTime;
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                transform.position = targetPosition;
            }
        }
    }
}
