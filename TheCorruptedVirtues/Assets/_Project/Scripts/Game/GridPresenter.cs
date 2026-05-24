using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Bridges grid coordinates to world positions.
    public sealed class GridPresenter : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private Vector3 origin = Vector3.zero;
        [SerializeField] private float unitY = 1.0f;
        [SerializeField] private float cursorY = 0.05f;
        // World height added per elevation level. Terrain is otherwise flat.
        [SerializeField] private float cellHeight = 0.5f;

        // Set by the orchestrator at build time; null = flat ground.
        private ElevationMap elevation;

        public GridBounds Bounds => new GridBounds(width, height);
        public float UnitY => unitY;
        public float CursorY => cursorY;
        public float CellSize => cellSize;
        public float CellHeight => cellHeight;

        public void SetElevation(ElevationMap map)
        {
            elevation = map;
        }

        public int LevelAt(GridCoord coord)
        {
            return elevation != null ? elevation.GetLevel(coord) : 0;
        }

        // The world-space Y added to a tile because of its elevation. Folded
        // into GridToWorld so everything positioned through it — units, cursor,
        // path line — rides the terrain automatically.
        public float WorldHeightAt(GridCoord coord)
        {
            return LevelAt(coord) * cellHeight;
        }

        public Vector3 GridToWorld(GridCoord coord, float yOverride)
        {
            return new Vector3(origin.x + coord.X * cellSize, yOverride + WorldHeightAt(coord), origin.z + coord.Y * cellSize);
        }

        public Vector3 GridToWorld(GridCoord coord)
        {
            return new Vector3(origin.x + coord.X * cellSize, origin.y + WorldHeightAt(coord), origin.z + coord.Y * cellSize);
        }

        public GridCoord WorldToGrid(Vector3 world)
        {
            int x = Mathf.RoundToInt((world.x - origin.x) / cellSize);
            int y = Mathf.RoundToInt((world.z - origin.z) / cellSize);
            return new GridCoord(x, y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = new Vector3(origin.x + x * cellSize, origin.y, origin.z);
                Vector3 end = new Vector3(origin.x + x * cellSize, origin.y, origin.z + height * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= height; y++)
            {
                Vector3 start = new Vector3(origin.x, origin.y, origin.z + y * cellSize);
                Vector3 end = new Vector3(origin.x + width * cellSize, origin.y, origin.z + y * cellSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
