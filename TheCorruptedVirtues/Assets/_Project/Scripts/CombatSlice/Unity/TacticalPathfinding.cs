using System.Collections.Generic;
using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public sealed class TacticalPathfinding : MonoBehaviour
    {
        private static readonly Vector3Int[] NeighborDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        private readonly Dictionary<Vector3Int, GridCellData> _tiles = new();
        private readonly HashSet<Vector3Int> _occupiedCells = new();

        public int CellSize { get; private set; } = CombatMapData.CellSize;
        public int WalkableTileId { get; private set; } = CombatMapData.WalkableTileId;

        public void SetupGrid(IReadOnlyList<GridCellData> cells, int cellSize, int walkableTileId)
        {
            CellSize = Mathf.Max(1, cellSize);
            WalkableTileId = walkableTileId;

            _tiles.Clear();
            _occupiedCells.Clear();

            for (int i = 0; i < cells.Count; i++)
            {
                GridCellData cell = cells[i];
                _tiles[cell.Coordinate] = cell;
            }
        }

        public bool HasTile(Vector3Int cell)
        {
            return _tiles.ContainsKey(cell);
        }

        public bool IsWalkableCell(Vector3Int cell)
        {
            return _tiles.TryGetValue(cell, out GridCellData tile) &&
                   tile.TileId == WalkableTileId &&
                   !_occupiedCells.Contains(cell);
        }

        public void MarkCellOccupied(Vector3Int cell)
        {
            _occupiedCells.Add(cell);
        }

        public void MarkCellUnoccupied(Vector3Int cell)
        {
            _occupiedCells.Remove(cell);
        }

        public bool IsCellOccupied(Vector3Int cell)
        {
            return _occupiedCells.Contains(cell);
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            float inverseCellSize = 1f / CellSize;
            int x = Mathf.FloorToInt(worldPosition.x * inverseCellSize);
            int y = Mathf.FloorToInt(worldPosition.y * inverseCellSize);
            int z = Mathf.FloorToInt(worldPosition.z * inverseCellSize);
            return new Vector3Int(x, y, z);
        }

        public Vector3 CellToWorld(Vector3Int cell)
        {
            return new Vector3(cell.x * CellSize, cell.y * CellSize, cell.z * CellSize);
        }

        public Vector3Int GetBestAvailableCell(Vector3Int cell)
        {
            if (HasTile(cell))
            {
                return cell;
            }

            Vector3Int lowerCell = cell + Vector3Int.down;
            Vector3Int upperCell = cell + Vector3Int.up;

            if (HasTile(lowerCell))
            {
                return lowerCell;
            }

            if (HasTile(upperCell))
            {
                return upperCell;
            }

            return FindClosestValidTile(cell, 5);
        }

        public List<Vector3> GetPath(Vector3 startWorld, Vector3 endWorld)
        {
            Vector3Int startCell = WorldToCell(startWorld);
            Vector3Int endCell = WorldToCell(endWorld);

            if (!IsWalkableCell(startCell) || !IsWalkableCell(endCell))
            {
                return new List<Vector3>(0);
            }

            var frontier = new Queue<Vector3Int>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();

            frontier.Enqueue(startCell);
            cameFrom[startCell] = startCell;

            while (frontier.Count > 0)
            {
                Vector3Int current = frontier.Dequeue();
                if (current == endCell)
                {
                    break;
                }

                IReadOnlyList<Vector3Int> neighbors = GetWalkableNeighbors(current);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Vector3Int neighbor = neighbors[i];
                    if (cameFrom.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    frontier.Enqueue(neighbor);
                }
            }

            if (!cameFrom.ContainsKey(endCell))
            {
                return new List<Vector3>(0);
            }

            var cellsPath = new List<Vector3Int>();
            Vector3Int walker = endCell;
            cellsPath.Add(walker);
            while (walker != startCell)
            {
                walker = cameFrom[walker];
                cellsPath.Add(walker);
            }

            cellsPath.Reverse();

            var worldPath = new List<Vector3>(cellsPath.Count);
            for (int i = 0; i < cellsPath.Count; i++)
            {
                worldPath.Add(CellToWorld(cellsPath[i]));
            }

            return worldPath;
        }

        private IReadOnlyList<Vector3Int> GetWalkableNeighbors(Vector3Int cell)
        {
            var neighbors = new List<Vector3Int>(4);

            for (int i = 0; i < NeighborDirections.Length; i++)
            {
                Vector3Int horizontalNeighbor = cell + NeighborDirections[i];
                Vector3Int upperNeighbor = horizontalNeighbor + Vector3Int.up;
                Vector3Int lowerNeighbor = horizontalNeighbor + Vector3Int.down;

                if (IsWalkableCell(horizontalNeighbor))
                {
                    neighbors.Add(horizontalNeighbor);
                }
                else if (IsWalkableCell(upperNeighbor))
                {
                    neighbors.Add(upperNeighbor);
                }
                else if (IsWalkableCell(lowerNeighbor))
                {
                    neighbors.Add(lowerNeighbor);
                }
            }

            return neighbors;
        }

        private Vector3Int FindClosestValidTile(Vector3Int startCell, int maxSearchRadius)
        {
            Vector3Int[] directions =
            {
                Vector3Int.right,
                Vector3Int.left,
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                Vector3Int.up,
                Vector3Int.down
            };

            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                for (int i = 0; i < directions.Length; i++)
                {
                    Vector3Int candidate = startCell + directions[i] * radius;
                    if (HasTile(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return startCell;
        }
    }
}
