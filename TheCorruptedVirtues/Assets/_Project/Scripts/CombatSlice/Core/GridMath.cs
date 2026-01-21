namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Small grid math helpers.
    public static class GridMath
    {
        public static int ManhattanDistance(GridCoord a, GridCoord b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy);
        }

        public static int StepCount(System.Collections.Generic.IReadOnlyList<GridCoord> path)
        {
            if (path == null || path.Count == 0)
            {
                return 0;
            }

            return path.Count - 1;
        }
    }
}
