using System.Collections.Generic;

namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Tracks occupied grid coordinates. The single-cell methods stay the
    // low-level primitive (pathfinding + 1x1 units depend on them); the
    // footprint methods layer multi-tile (N x N) occupancy on top of the
    // same cell set without changing single-cell semantics.
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

        public void AddFootprint(GridFootprint footprint, GridCoord anchor)
        {
            foreach (GridCoord cell in footprint.Cells(anchor))
            {
                occupied.Add(cell);
            }
        }

        public void RemoveFootprint(GridFootprint footprint, GridCoord anchor)
        {
            foreach (GridCoord cell in footprint.Cells(anchor))
            {
                occupied.Remove(cell);
            }
        }

        // True only if every cell the footprint covers at anchor is in bounds
        // and unoccupied. To move a multi-tile unit, remove its footprint
        // first, then test the destination (lift-and-place) so it does not
        // collide with its own current cells.
        public bool CanPlace(GridFootprint footprint, GridCoord anchor, GridBounds bounds)
        {
            foreach (GridCoord cell in footprint.Cells(anchor))
            {
                if (!bounds.Contains(cell) || occupied.Contains(cell))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
