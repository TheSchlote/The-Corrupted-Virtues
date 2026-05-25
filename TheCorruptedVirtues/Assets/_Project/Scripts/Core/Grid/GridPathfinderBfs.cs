using System.Collections.Generic;

namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Simple 4-way BFS pathfinder.
    public static class GridPathfinderBfs
    {
        private static readonly GridCoord[] Neighbors =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1)
        };

        public static List<GridCoord> FindPath(
            GridCoord start,
            GridCoord goal,
            GridOccupancy blocked,
            GridBounds bounds)
        {
            if (!bounds.Contains(start) || !bounds.Contains(goal))
            {
                return new List<GridCoord>();
            }

            if (start == goal)
            {
                return new List<GridCoord> { start };
            }

            Queue<GridCoord> queue = new Queue<GridCoord>();
            Dictionary<GridCoord, GridCoord> cameFrom = new Dictionary<GridCoord, GridCoord>();
            queue.Enqueue(start);
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                GridCoord current = queue.Dequeue();
                for (int i = 0; i < Neighbors.Length; i++)
                {
                    GridCoord next = current + Neighbors[i];
                    if (!bounds.Contains(next))
                    {
                        continue;
                    }

                    if (blocked != null && blocked.IsOccupied(next) && next != goal)
                    {
                        continue;
                    }

                    if (cameFrom.ContainsKey(next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    if (next == goal)
                    {
                        return ReconstructPath(cameFrom, start, goal);
                    }

                    queue.Enqueue(next);
                }
            }

            return new List<GridCoord>();
        }

        // Footprint-aware BFS: the anchor path from 'start' to the nearest
        // placeable anchor whose footprint is orthogonally adjacent to
        // 'targetCell' (without overlapping it). 'blocked' must EXCLUDE the
        // moving unit's own footprint (lift-and-place). When 'elevation' is
        // supplied, anchors where the footprint would straddle an elevation edge
        // are impassable — a multi-tile unit must keep its whole footprint on one
        // level. Empty if unreachable.
        public static List<GridCoord> FindFootprintApproach(
            GridCoord start,
            GridFootprint footprint,
            GridCoord targetCell,
            GridOccupancy blocked,
            GridBounds bounds,
            ElevationMap elevation = null)
        {
            GridOccupancy occ = blocked ?? new GridOccupancy();

            if (FootprintAdjacency.AreAdjacent(footprint, start, GridFootprint.Single, targetCell))
            {
                return new List<GridCoord> { start };
            }

            Queue<GridCoord> queue = new Queue<GridCoord>();
            Dictionary<GridCoord, GridCoord> cameFrom = new Dictionary<GridCoord, GridCoord>();
            queue.Enqueue(start);
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                GridCoord current = queue.Dequeue();
                for (int i = 0; i < Neighbors.Length; i++)
                {
                    GridCoord next = current + Neighbors[i];
                    if (cameFrom.ContainsKey(next))
                    {
                        continue;
                    }

                    if (!occ.CanPlace(footprint, next, bounds))
                    {
                        continue;
                    }

                    // A multi-tile unit never occupies a straddling footprint —
                    // not even in transit — so straddle anchors are impassable,
                    // not merely invalid stopping points. Consequence: such a
                    // unit can't cross or mount an elevation edge, so a map must
                    // leave it a same-level route to its targets.
                    if (elevation != null && !elevation.IsUniformUnder(footprint, next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    if (FootprintAdjacency.AreAdjacent(footprint, next, GridFootprint.Single, targetCell))
                    {
                        return ReconstructPath(cameFrom, start, next);
                    }

                    queue.Enqueue(next);
                }
            }

            return new List<GridCoord>();
        }

        private static List<GridCoord> ReconstructPath(
            Dictionary<GridCoord, GridCoord> cameFrom,
            GridCoord start,
            GridCoord goal)
        {
            List<GridCoord> path = new List<GridCoord>();
            GridCoord current = goal;
            path.Add(current);

            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
