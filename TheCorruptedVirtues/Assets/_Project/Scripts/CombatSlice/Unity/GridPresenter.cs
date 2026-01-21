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

        public GridBounds Bounds => new GridBounds(width, height);
        public float UnitY => unitY;
        public float CursorY => cursorY;

        public Vector3 GridToWorld(GridCoord coord, float yOverride)
        {
            return new Vector3(origin.x + coord.X * cellSize, yOverride, origin.z + coord.Y * cellSize);
        }

        public Vector3 GridToWorld(GridCoord coord)
        {
            return new Vector3(origin.x + coord.X * cellSize, origin.y, origin.z + coord.Y * cellSize);
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
