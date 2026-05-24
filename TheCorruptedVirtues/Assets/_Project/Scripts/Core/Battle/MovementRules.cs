using System;
using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Pure movement-reach math shared by the player's path preview and the
    // enemy AI: given a full path, how many step-edges can the unit actually
    // walk this turn. Capped by MoveRange, and stopped one tile short when the
    // destination is occupied by someone else (you can path *to* an enemy to
    // target it, but you halt adjacent rather than onto it).
    //
    // Lifted verbatim out of CombatSliceOrchestrator.ComputeReachableSteps.
    public static class MovementRules
    {
        public static int ComputeReachableSteps(
            IReadOnlyList<GridCoord> path,
            int moveRange,
            GridOccupancy occupancy,
            GridCoord unitCoord)
        {
            if (path == null || path.Count <= 1)
            {
                return 0;
            }

            int totalSteps = GridMath.StepCount(path);
            int reachable = Math.Min(totalSteps, moveRange);

            GridCoord destination = path[path.Count - 1];
            if (occupancy.IsOccupied(destination) && destination != unitCoord)
            {
                reachable = Math.Min(reachable, totalSteps - 1);
            }

            return Math.Max(0, reachable);
        }
    }
}
