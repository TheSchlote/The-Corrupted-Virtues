using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The single rule for "can this ability target the unit under the cursor?",
    // by faction + range only. Both the preview (what the HUD forecasts) and the
    // commit (what actually fires) call this, so they can never disagree — they
    // diverged once before (a 2x2 boss showed no forecast from non-anchor cells
    // yet still took the hit). Affordability and once-per-turn gating stay with
    // the caller; this is purely the spatial/faction question.
    public static class AbilityTargeting
    {
        public static bool IsValidTarget(CombatUnit actor, AbilitySpec ability, CombatUnit candidate)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            switch (ability.Targeting)
            {
                case TargetingMode.Self:
                    return ReferenceEquals(candidate, actor);

                case TargetingMode.Ally:
                    // Same side, the caster included (distance 0). Range 1 is
                    // today's heal-self-or-adjacent-ally rule.
                    return candidate.Faction == actor.Faction
                        && GridMath.ManhattanDistance(actor.Coord, candidate.Coord) <= ability.Range;

                default: // TargetingMode.Enemy
                    return candidate.Faction != actor.Faction
                        && EnemyInRange(actor, candidate, ability.Range);
            }
        }

        // Range 1 (every ability today) is footprint-aware orthogonal adjacency —
        // the rule the preview and commit share, including the 2x2 boss's
        // non-anchor cells. Range > 1 is a reserved seam for ranged abilities (no
        // content uses it yet): the nearest pair of footprint cells within range.
        private static bool EnemyInRange(CombatUnit actor, CombatUnit target, int range)
        {
            if (range <= 1)
            {
                return FootprintAdjacency.AreAdjacent(
                    actor.Footprint, actor.Coord, target.Footprint, target.Coord);
            }

            foreach (GridCoord a in actor.Footprint.Cells(actor.Coord))
            {
                foreach (GridCoord t in target.Footprint.Cells(target.Coord))
                {
                    if (GridMath.ManhattanDistance(a, t) <= range)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
