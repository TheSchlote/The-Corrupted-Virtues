namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Orthogonal adjacency between two footprints (axis-aligned tile rects). Two
    // units are "adjacent" for melee if any cell of one is orthogonally next to
    // any cell of the other and they don't overlap. Generalises Manhattan-
    // distance-1 to multi-tile units, so a 2x2 boss can be attacked from — or
    // attack from — any of its sides. For two 1x1 footprints this is exactly
    // Manhattan-distance-1.
    public static class FootprintAdjacency
    {
        private static readonly GridCoord[] Orthogonal =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1),
        };

        public static bool AreAdjacent(GridFootprint a, GridCoord anchorA, GridFootprint b, GridCoord anchorB)
        {
            // Overlapping footprints are not adjacent (and shouldn't occur).
            foreach (GridCoord cellA in a.Cells(anchorA))
            {
                if (b.Covers(anchorB, cellA))
                {
                    return false;
                }
            }

            foreach (GridCoord cellA in a.Cells(anchorA))
            {
                for (int i = 0; i < Orthogonal.Length; i++)
                {
                    if (b.Covers(anchorB, cellA + Orthogonal[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
