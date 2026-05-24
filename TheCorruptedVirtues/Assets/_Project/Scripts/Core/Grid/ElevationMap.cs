namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Per-tile terrain height in discrete levels (0 = ground). Pure grid data
    // with no combat or engine knowledge — the sibling of GridOccupancy.
    // Combat reads levels to grade the high-ground bonus; the view layer reads
    // them to raise tiles and the units standing on them. Any tile not set is
    // level 0.
    public sealed class ElevationMap
    {
        private readonly System.Collections.Generic.Dictionary<GridCoord, int> levels =
            new System.Collections.Generic.Dictionary<GridCoord, int>();

        public int GetLevel(GridCoord coord)
        {
            return levels.TryGetValue(coord, out int level) ? level : 0;
        }

        // Level 0 is the implicit default, so storing it would just waste an
        // entry — setting a tile back to 0 removes it instead.
        public void SetLevel(GridCoord coord, int level)
        {
            if (level == 0)
            {
                levels.Remove(coord);
                return;
            }

            levels[coord] = level;
        }

        public void Clear()
        {
            levels.Clear();
        }
    }
}
