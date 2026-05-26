using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // What the enemy AI decided to do this turn, as plain data: who to hit,
    // the (possibly empty) path to walk first, whether an attack lands once the
    // move finishes, and which ability to use for it. The Unity layer animates
    // the move, spends the MP and raises events; the decision itself is pure and
    // unit-tested here.
    public readonly struct EnemyTurnPlan
    {
        public readonly CombatUnit Target;
        public readonly IReadOnlyList<GridCoord> MovePath;
        public readonly bool AttackAfterMove;
        // The ability the enemy will attack with (chosen for the target, MP
        // permitting). Null when the turn is move-only or an end-turn.
        public readonly AbilitySpec Ability;

        public EnemyTurnPlan(CombatUnit target, IReadOnlyList<GridCoord> movePath, bool attackAfterMove, AbilitySpec ability = null)
        {
            Target = target;
            MovePath = movePath;
            AttackAfterMove = attackAfterMove;
            Ability = ability;
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
        // Dispatch on the actor's AI policy. Only Aggressive ships today (the
        // original heuristic); the switch reserves the seam for future archetypes
        // without the caller — the simulator or the orchestrator — changing.
        public static EnemyTurnPlan Plan(CombatUnit actor, BattleState state, GridBounds bounds, ElevationMap elevation = null)
        {
            switch (actor.AiBehavior)
            {
                case AiBehavior.Aggressive:
                default:
                    return PlanAggressive(actor, state, bounds, elevation);
            }
        }

        // The shipped per-unit heuristic: focus-fire the weakest adjacent
        // opponent, else approach the nearest, else end the turn.
        private static EnemyTurnPlan PlanAggressive(CombatUnit actor, BattleState state, GridBounds bounds, ElevationMap elevation = null)
        {
            // Focus-fire: if an opponent is already adjacent, strike the weakest
            // one in place with the strongest ability the actor can afford. A
            // finishing (or biggest) hit in reach beats repositioning.
            CombatUnit adjacentTarget = FindWeakestAdjacent(actor, state);
            if (adjacentTarget != null)
            {
                return new EnemyTurnPlan(adjacentTarget, null, attackAfterMove: true, ChooseAbility(actor, adjacentTarget));
            }

            CombatUnit target = FindNearestEnemy(actor, state);
            if (target == null)
            {
                return EnemyTurnPlan.EndTurn;
            }

            // Multi-tile units (the mobile Boss) need footprint-aware
            // pathfinding; 1x1 units keep the original single-cell logic so
            // their behaviour — and the tests pinning it — stay unchanged.
            if (!actor.Footprint.IsSingle)
            {
                return PlanMultiTile(actor, target, state, bounds, elevation);
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
            return new EnemyTurnPlan(target, segment, attackAfter, attackAfter ? ChooseAbility(actor, target) : null);
        }

        // Footprint-aware approach for multi-tile units: a lift-and-place path
        // to the nearest anchor where the unit's footprint is adjacent to the
        // target, capped by MoveRange. Attacks if the move lands adjacent.
        private static EnemyTurnPlan PlanMultiTile(CombatUnit actor, CombatUnit target, BattleState state, GridBounds bounds, ElevationMap elevation = null)
        {
            GridOccupancy others = state.BuildOccupancyExcluding(actor);
            List<GridCoord> path = GridPathfinderBfs.FindFootprintApproach(
                actor.Coord, actor.Footprint, target.Coord, others, bounds, elevation);

            int totalSteps = path.Count - 1;
            if (totalSteps <= 0)
            {
                return EnemyTurnPlan.EndTurn;
            }

            int steps = totalSteps < actor.MoveRange ? totalSteps : actor.MoveRange;
            if (steps <= 0)
            {
                return EnemyTurnPlan.EndTurn;
            }

            List<GridCoord> segment = new List<GridCoord>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                segment.Add(path[i]);
            }

            GridCoord finalAnchor = segment[segment.Count - 1];
            bool attackAfter = FootprintAdjacency.AreAdjacent(
                actor.Footprint, finalAnchor, target.Footprint, target.Coord);
            return new EnemyTurnPlan(target, segment, attackAfter, attackAfter ? ChooseAbility(actor, target) : null);
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

        // The lowest-HP living opponent already adjacent to the actor's
        // footprint, or null if none are in striking range. Strict less-than
        // keeps the tie-break deterministic (first in roster order wins).
        public static CombatUnit FindWeakestAdjacent(CombatUnit actor, BattleState state)
        {
            CombatUnit weakest = null;
            IReadOnlyList<CombatUnit> units = state.Units;
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit candidate = units[i];
                if (!candidate.IsAlive || candidate.Faction == actor.Faction) continue;
                if (!FootprintAdjacency.AreAdjacent(actor.Footprint, actor.Coord, candidate.Footprint, candidate.Coord)) continue;

                if (weakest == null || candidate.Hp < weakest.Hp)
                {
                    weakest = candidate;
                }
            }
            return weakest;
        }

        // The strongest offensive ability the actor can afford against a target:
        // the one with the highest forecast Hit-tier damage (element matchup +
        // stats), among those within the MP budget. The basic attack (index 0,
        // free) is always a candidate, so this never returns null. Enemies don't
        // heal this slice, so Support abilities are skipped.
        public static AbilitySpec ChooseAbility(CombatUnit actor, CombatUnit target)
        {
            AbilitySpec best = actor.BasicAttack;
            int bestDamage = -1;
            IReadOnlyList<AbilitySpec> abilities = actor.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilitySpec ability = abilities[i];
                if (ability.Kind == AbilityKind.Support) continue;
                if (actor.Mp < ability.MpCost) continue;

                int damage = DamageCalculator.ComputeDamage(
                    actor.Stats, actor.Element,
                    target.Stats, target.Element,
                    ability, ExecutionResult.Hit, SituationalModifiers.None).FinalDamage;

                if (damage > bestDamage)
                {
                    bestDamage = damage;
                    best = ability;
                }
            }
            return best;
        }
    }
}
