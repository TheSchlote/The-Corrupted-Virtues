namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Rectangular grid bounds for clamp/contain checks.
    public readonly struct GridBounds
    {
        public readonly int Width;
        public readonly int Height;

        public GridBounds(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Contains(GridCoord coord)
        {
            return coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height;
        }
    }
}
