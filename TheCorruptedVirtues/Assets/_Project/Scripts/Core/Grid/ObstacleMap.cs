using System.Collections.Generic;

namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Per-tile static impassability — terrain that blocks movement and
    // placement (walls, rubble, pits). Pure grid data with no combat or engine
    // knowledge: the sibling of ElevationMap and GridOccupancy. ElevationMap
    // grades the high-ground bonus, GridOccupancy tracks who is standing where,
    // and this marks tiles nothing can stand on or path through. A tile not
    // added is passable. Obstacles are folded into the occupancy the pathfinder
    // reads (see BattleState), so movement/placement code
    // treats them as blocked without consulting this directly.
    public sealed class ObstacleMap
    {
        private readonly HashSet<GridCoord> blocked = new HashSet<GridCoord>();

        // The blocked tiles, for the view (to raise obstacle geometry) and for
        // seeding occupancy. Read-only so callers can't mutate the set.
        public IReadOnlyCollection<GridCoord> Blocked => blocked;

        public bool IsBlocked(GridCoord coord)
        {
            return blocked.Contains(coord);
        }

        public void Add(GridCoord coord)
        {
            blocked.Add(coord);
        }

        public void Remove(GridCoord coord)
        {
            blocked.Remove(coord);
        }

        public void Clear()
        {
            blocked.Clear();
        }
    }
}
