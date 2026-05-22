using System;

namespace TheCorruptedVirtues.Combat
{
    // Pure C# heal calculator for Support abilities (M2 slice 2). Healing
    // scales off the caster's Special Attack (the "special/support" stat) and
    // is amplified by the execution tier exactly like damage — so a botched
    // QTE (Miss) heals nothing and a Divine over-heals. No element matchup or
    // mitigation applies: a heal is a heal.
    public static class HealCalculator
    {
        public static HealBreakdown ComputeHeal(
            CombatStats caster,
            AbilitySpec ability,
            ExecutionResult executionResult)
        {
            float executionMultiplier = ExecutionModifiers.GetDamageMultiplier(executionResult);
            float baseHeal = ability.Power + (caster.SpecialAttack * ability.Scaling);
            float finalHealFloat = baseHeal * executionMultiplier;
            int finalHeal = Math.Max(0, (int)Math.Round(finalHealFloat, MidpointRounding.AwayFromZero));

            return new HealBreakdown(finalHeal, executionMultiplier, baseHeal);
        }
    }
}
