using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Flanking rule (M2 facing slice): a melee attacker hits harder from the
    // target's flank or rear. Tiers (first playtest, tunable): back 1.5, side
    // 1.25, front 1.0. Pure — compares where the attacker stands against the
    // target's facing. Fills the Flanking term of SituationalModifiers.
    public static class FlankingRules
    {
        public const float BackBonus = 1.5f;
        public const float SideBonus = 1.25f;
        public const float FrontBonus = 1.0f;

        public static float Multiplier(GridCoord attackerCoord, GridCoord targetCoord, Facing targetFacing)
        {
            // Which way the attacker lies relative to the target.
            Facing attackFrom = FacingRules.Toward(targetCoord, attackerCoord);

            if (attackFrom == targetFacing)
            {
                return FrontBonus; // the target is looking right at the attacker
            }

            if (attackFrom == FacingRules.Opposite(targetFacing))
            {
                return BackBonus; // attacker is directly behind the target
            }

            return SideBonus; // perpendicular — a flank
        }
    }
}
