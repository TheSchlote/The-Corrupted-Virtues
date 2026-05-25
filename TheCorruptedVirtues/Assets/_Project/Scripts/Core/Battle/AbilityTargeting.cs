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

            if (ability.Kind == AbilityKind.Support)
            {
                // Heal self or an adjacent ally (Manhattan <= 1; self is 0).
                return candidate.Faction == actor.Faction
                    && GridMath.ManhattanDistance(actor.Coord, candidate.Coord) <= 1;
            }

            // Attacks strike an enemy whose footprint is orthogonally adjacent.
            return candidate.Faction != actor.Faction
                && FootprintAdjacency.AreAdjacent(actor.Footprint, actor.Coord, candidate.Footprint, candidate.Coord);
        }
    }
}
