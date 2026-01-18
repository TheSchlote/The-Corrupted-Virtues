using System;

namespace TheCorruptedVirtues.Combat
{
    // Pure C# combat damage calculator for abilities.
    public static class DamageCalculator
    {
        public static DamageBreakdown ComputeDamage(
            CombatStats attacker,
            ElementType attackerElement,
            CombatStats defender,
            ElementType defenderElement,
            AbilitySpec ability,
            ResonanceResult resonanceResult)
        {
            float elementMultiplier = ElementChart.GetMultiplier(ability.Element, defenderElement);
            float resonanceMultiplier = ResonanceModifiers.GetDamageMultiplier(resonanceResult);
            _ = attackerElement;

            if (ability.Kind == AbilityKind.Support)
            {
                return new DamageBreakdown(
                    0,
                    elementMultiplier,
                    resonanceMultiplier,
                    1.0f,
                    0.0f);
            }

            int attackStat = ability.Kind == AbilityKind.Physical ? attacker.Attack : attacker.SpecialAttack;
            int defenseStat = ability.Kind == AbilityKind.Physical ? defender.Defense : defender.SpecialDefense;

            float preMitigationDamage = ability.Power + (attackStat * ability.Scaling);
            float mitigationFactor = 100.0f / (100.0f + defenseStat);
            float mitigatedDamage = preMitigationDamage * mitigationFactor;

            float finalDamageFloat = mitigatedDamage * elementMultiplier * resonanceMultiplier;
            int finalDamage = Math.Max(0, (int)Math.Round(finalDamageFloat, MidpointRounding.AwayFromZero));

            return new DamageBreakdown(
                finalDamage,
                elementMultiplier,
                resonanceMultiplier,
                mitigationFactor,
                preMitigationDamage);
        }
    }
}
