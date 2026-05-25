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

        // True if every tile a footprint covers at this anchor shares one level.
        // Multi-tile units must stand on uniform ground — they can't straddle an
        // elevation edge (keeps placement, the high-ground term, and any future
        // model flat-footed on one level). The footprint pathfinder treats
        // non-uniform anchors as impassable, so this is enforced in transit too,
        // not only at rest. 1x1 footprints are trivially uniform, so single-tile
        // units are never restricted.
        public bool IsUniformUnder(GridFootprint footprint, GridCoord anchor)
        {
            int anchorLevel = GetLevel(anchor);
            foreach (GridCoord cell in footprint.Cells(anchor))
            {
                if (GetLevel(cell) != anchorLevel)
                {
                    return false;
                }
            }
            return true;
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
