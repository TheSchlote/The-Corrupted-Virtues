using System;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The result of applying one ability to one target: what happened to its
    // HP, as plain data. Returned instead of raising events so the resolution
    // is pure and testable; the Unity layer turns this into UnitDamaged /
    // UnitHealed / UnitDied / CombatEnded.
    public readonly struct AbilityOutcome
    {
        public readonly bool IsHeal;
        public readonly UnitId TargetId;
        // Heal: HP actually restored (clamped to MaxHP). Damage: the computed
        // damage dealt (the pre-clamp number — an overkill still reports the
        // full hit, matching the original UnitDamaged payload).
        public readonly int Amount;
        public readonly int TargetHp;
        public readonly int TargetMaxHp;
        public readonly bool TargetDied;

        public AbilityOutcome(bool isHeal, UnitId targetId, int amount, int targetHp, int targetMaxHp, bool targetDied)
        {
            IsHeal = isHeal;
            TargetId = targetId;
            Amount = amount;
            TargetHp = targetHp;
            TargetMaxHp = targetMaxHp;
            TargetDied = targetDied;
        }
    }

    // Applies an ability's effect to a target's HP using the pure damage/heal
    // calculators, and reports the outcome. Lifted out of
    // CombatSliceOrchestrator.ResolveAbility (minus the event raising and win
    // check, which stay in the Unity layer because they touch CombatEvents and
    // the orchestrator's combat-over flag).
    public static class AbilityResolver
    {
        public static AbilityOutcome Resolve(
            CombatUnit attacker, CombatUnit target, AbilitySpec ability, ExecutionResult execution)
        {
            if (ability.Kind == AbilityKind.Support)
            {
                HealBreakdown heal = HealCalculator.ComputeHeal(attacker.Stats, ability, execution);
                int before = target.Hp;
                target.Hp = Math.Min(target.MaxHp, target.Hp + heal.FinalHeal);
                int applied = target.Hp - before;
                return new AbilityOutcome(
                    isHeal: true,
                    targetId: target.Id,
                    amount: applied,
                    targetHp: target.Hp,
                    targetMaxHp: target.MaxHp,
                    targetDied: false);
            }

            DamageBreakdown bd = DamageCalculator.ComputeDamage(
                attacker.Stats, attacker.Element,
                target.Stats, target.Element,
                ability, execution);

            int damage = bd.FinalDamage;
            target.Hp = Math.Max(0, target.Hp - damage);
            return new AbilityOutcome(
                isHeal: false,
                targetId: target.Id,
                amount: damage,
                targetHp: target.Hp,
                targetMaxHp: target.MaxHp,
                targetDied: target.Hp <= 0);
        }
    }
}
