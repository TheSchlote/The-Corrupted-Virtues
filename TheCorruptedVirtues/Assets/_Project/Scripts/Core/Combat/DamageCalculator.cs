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
            ExecutionResult executionResult)
        {
            return ComputeDamage(
                attacker, attackerElement, defender, defenderElement,
                ability, executionResult, SituationalModifiers.None);
        }

        public static DamageBreakdown ComputeDamage(
            CombatStats attacker,
            ElementType attackerElement,
            CombatStats defender,
            ElementType defenderElement,
            AbilitySpec ability,
            ExecutionResult executionResult,
            SituationalModifiers situational)
        {
            float elementMultiplier = ElementChart.GetMultiplier(ability.Element, defenderElement);
            float executionMultiplier = ExecutionModifiers.GetDamageMultiplier(executionResult);
            _ = attackerElement;

            if (ability.Kind == AbilityKind.Support)
            {
                return new DamageBreakdown(
                    0,
                    elementMultiplier,
                    executionMultiplier,
                    1.0f,
                    0.0f,
                    situational);
            }

            int attackStat = ability.Kind == AbilityKind.Physical ? attacker.Attack : attacker.SpecialAttack;
            int defenseStat = ability.Kind == AbilityKind.Physical ? defender.Defense : defender.SpecialDefense;

            float preMitigationDamage = ability.Power + (attackStat * ability.Scaling);
            float mitigationFactor = 100.0f / (100.0f + defenseStat);
            float mitigatedDamage = preMitigationDamage * mitigationFactor;

            float finalDamageFloat = mitigatedDamage * elementMultiplier * executionMultiplier * situational.Product;
            int finalDamage = Math.Max(0, (int)Math.Round(finalDamageFloat, MidpointRounding.AwayFromZero));

            return new DamageBreakdown(
                finalDamage,
                elementMultiplier,
                executionMultiplier,
                mitigationFactor,
                preMitigationDamage,
                situational);
        }
    }
}
