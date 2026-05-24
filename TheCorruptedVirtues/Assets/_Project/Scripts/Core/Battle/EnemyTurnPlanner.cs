using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // What the enemy AI decided to do this turn, as plain data: who to hit,
    // the (possibly empty) path to walk first, and whether an attack lands
    // once the move finishes. The Unity layer animates the move and raises
    // events; the decision itself is pure and unit-tested here.
    public readonly struct EnemyTurnPlan
    {
        public readonly CombatUnit Target;
        public readonly IReadOnlyList<GridCoord> MovePath;
        public readonly bool AttackAfterMove;

        public EnemyTurnPlan(CombatUnit target, IReadOnlyList<GridCoord> movePath, bool attackAfterMove)
        {
            Target = target;
            MovePath = movePath;
            AttackAfterMove = attackAfterMove;
        }

        // A path with more than one point is an actual walk (point 0 is the
        // unit's current tile).
        public bool HasMove => MovePath != null && MovePath.Count > 1;

        // Do nothing this turn (no reachable target).
        public static EnemyTurnPlan EndTurn => new EnemyTurnPlan(null, null, false);
    }

    // The per-unit enemy AI: walk toward the nearest living opponent and hit it
    // if you can reach (or already are) adjacent. Pure C#; lifted out of
    // CombatSliceOrchestrator (HandleEnemyTurn / FindNearestEnemy /
    // OnEnemyMoveComplete) so the heuristic is testable without the coroutine.
    public static class EnemyTurnPlanner
    {
        public static EnemyTurnPlan Plan(CombatUnit actor, BattleState state, GridBounds bounds)
        {
            CombatUnit target = FindNearestEnemy(actor, state);
            if (target == null)
            {
                return EnemyTurnPlan.EndTurn;
            }

            // Already adjacent: attack in place, no move.
            if (GridMath.ManhattanDistance(actor.Coord, target.Coord) == 1)
            {
                return new EnemyTurnPlan(target, null, attackAfterMove: true);
            }

            state.RebuildOccupancy();
            List<GridCoord> path = GridPathfinderBfs.FindPath(
                actor.Coord, target.Coord, state.Occupancy, bounds);

            int reachableSteps = MovementRules.ComputeReachableSteps(
                path, actor.MoveRange, state.Occupancy, actor.Coord);
            if (reachableSteps <= 0)
            {
                return EnemyTurnPlan.EndTurn;
            }

            int points = reachableSteps + 1;
            List<GridCoord> segment = new List<GridCoord>(points);
            for (int i = 0; i < points; i++)
            {
                segment.Add(path[i]);
            }

            GridCoord finalCoord = segment[segment.Count - 1];
            bool attackAfter = GridMath.ManhattanDistance(finalCoord, target.Coord) == 1;
            return new EnemyTurnPlan(target, segment, attackAfter);
        }

        // Nearest living opponent by Manhattan distance; ties keep the first in
        // roster order (matches the original orchestrator heuristic).
        public static CombatUnit FindNearestEnemy(CombatUnit actor, BattleState state)
        {
            CombatUnit nearest = null;
            int nearestDistance = int.MaxValue;
            IReadOnlyList<CombatUnit> units = state.Units;
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit candidate = units[i];
                if (!candidate.IsAlive) continue;
                if (candidate.Faction == actor.Faction) continue;

                int d = GridMath.ManhattanDistance(actor.Coord, candidate.Coord);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearest = candidate;
                }
            }
            return nearest;
        }
    }
}
