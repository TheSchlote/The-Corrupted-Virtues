using System.Collections.Generic;

namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Tracks occupied grid coordinates.
    public sealed class GridOccupancy
    {
        private readonly HashSet<GridCoord> occupied = new HashSet<GridCoord>();

        public void Clear()
        {
            occupied.Clear();
        }

        public void Add(GridCoord coord)
        {
            occupied.Add(coord);
        }

        public void Remove(GridCoord coord)
        {
            occupied.Remove(coord);
        }

        public bool IsOccupied(GridCoord coord)
        {
            return occupied.Contains(coord);
        }
    }
}
