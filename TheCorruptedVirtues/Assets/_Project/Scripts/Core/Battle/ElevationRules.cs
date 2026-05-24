using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // High-ground rule: attacking from a strictly higher tile than the target
    // grants a flat damage bonus; equal or lower ground is neutral (no uphill
    // penalty). Pure combat-on-a-grid logic — reads tile levels from an
    // ElevationMap and produces the SituationalModifiers the damage calculator
    // consumes.
    //
    // Tuning (M2 terrain slice, first playtest): binary, bonus-only. The
    // magnitude is a single knob, expected to move after playtest.
    public static class ElevationRules
    {
        public const float HighGroundBonus = 1.25f;

        public static float HighGroundMultiplier(int attackerLevel, int targetLevel)
        {
            return attackerLevel > targetLevel ? HighGroundBonus : 1.0f;
        }

        // Convenience for the orchestrator: build the modifiers for one attack
        // straight from coords + the map (used identically by the live resolve
        // and the on-screen forecast, so the preview never lies). A null map
        // means "no terrain" → no effect.
        public static SituationalModifiers ModifiersFor(GridCoord attackerCoord, GridCoord targetCoord, ElevationMap elevation)
        {
            if (elevation == null)
            {
                return SituationalModifiers.None;
            }

            float highGround = HighGroundMultiplier(
                elevation.GetLevel(attackerCoord),
                elevation.GetLevel(targetCoord));
            return SituationalModifiers.FromHighGround(highGround);
        }
    }
}
