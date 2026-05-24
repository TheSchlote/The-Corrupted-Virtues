using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Builds the position-derived damage modifiers for one attack from the
    // attacker/target positions, the target's facing, and the terrain. One
    // place so the on-screen forecast and the live resolve always agree.
    //
    // High ground and flanking multiply (SituationalModifiers.Product), so the
    // best positioning — high ground AND behind the target — stacks.
    public static class CombatSituation
    {
        public static SituationalModifiers For(CombatUnit attacker, CombatUnit target, ElevationMap elevation)
        {
            float highGround = elevation == null
                ? 1.0f
                : ElevationRules.HighGroundMultiplier(
                    elevation.GetLevel(attacker.Coord),
                    elevation.GetLevel(target.Coord));

            float flanking = FlankingRules.Multiplier(attacker.Coord, target.Coord, target.Facing);

            return new SituationalModifiers(highGround, flanking);
        }
    }
}
