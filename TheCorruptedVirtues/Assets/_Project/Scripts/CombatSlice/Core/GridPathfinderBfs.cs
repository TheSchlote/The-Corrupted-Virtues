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
