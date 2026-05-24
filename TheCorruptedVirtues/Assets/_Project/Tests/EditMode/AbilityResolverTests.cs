using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins AbilityResolver — the HP-mutation wrapper extracted from the
    // orchestrator. (The damage/heal numbers themselves are pinned by
    // CombatMathTests / AbilityExecutionTests; here we only check the clamp,
    // routing, and outcome reporting.)
    public class AbilityResolverTests
    {
        private static CombatUnit Attacker()
        {
            return BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), element: ElementType.Light);
        }

        [Test]
        public void Damage_Lethal_KillsAndReportsDeath()
        {
            CombatUnit attacker = Attacker();
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 1, element: ElementType.Light);

            AbilityOutcome outcome = AbilityResolver.Resolve(attacker, target, attacker.BasicAttack, ExecutionResult.Hit);

            Assert.That(outcome.IsHeal, Is.False);
            Assert.That(outcome.Amount, Is.GreaterThan(0));
            Assert.That(outcome.TargetHp, Is.EqualTo(0));
            Assert.That(outcome.TargetDied, Is.True);
            Assert.That(target.Hp, Is.EqualTo(0));
        }

        [Test]
        public void Damage_NonLethal_ReducesHpWithoutDeath()
        {
            CombatUnit attacker = Attacker();
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);

            AbilityOutcome outcome = AbilityResolver.Resolve(attacker, target, attacker.BasicAttack, ExecutionResult.Hit);

            Assert.That(outcome.TargetDied, Is.False);
            Assert.That(outcome.Amount, Is.GreaterThan(0));
            Assert.That(outcome.TargetHp, Is.EqualTo(10000 - outcome.Amount));
            Assert.That(target.Hp, Is.EqualTo(outcome.TargetHp));
        }

        [Test]
        public void Heal_CapsAtMaxHp()
        {
            CombatUnit attacker = Attacker();
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(1, 0), hp: 100);
            target.Hp = 99; // one below max
            AbilitySpec mend = new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f);

            AbilityOutcome outcome = AbilityResolver.Resolve(attacker, target, mend, ExecutionResult.Hit);

            Assert.That(outcome.IsHeal, Is.True);
            Assert.That(outcome.TargetHp, Is.EqualTo(100));
            Assert.That(outcome.Amount, Is.EqualTo(1)); // only the missing 1 HP applied
            Assert.That(outcome.TargetDied, Is.False);
        }

        [Test]
        public void Heal_RestoresHp_ReportsAppliedAmount()
        {
            CombatUnit attacker = Attacker();
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(1, 0), hp: 100);
            target.Hp = 10;
            AbilitySpec mend = new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f);

            AbilityOutcome outcome = AbilityResolver.Resolve(attacker, target, mend, ExecutionResult.Hit);

            Assert.That(outcome.IsHeal, Is.True);
            Assert.That(outcome.Amount, Is.GreaterThan(0));
            Assert.That(outcome.TargetHp, Is.EqualTo(10 + outcome.Amount));
            Assert.That(target.Hp, Is.EqualTo(outcome.TargetHp));
        }

        [Test]
        public void Damage_HighGround_DealsMoreThanLevelGround()
        {
            CombatUnit attacker = Attacker();
            CombatUnit groundTarget = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);
            CombatUnit highTarget = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);

            AbilityOutcome onLevel = AbilityResolver.Resolve(
                attacker, groundTarget, attacker.BasicAttack, ExecutionResult.Hit, SituationalModifiers.None);
            AbilityOutcome fromHigh = AbilityResolver.Resolve(
                attacker, highTarget, attacker.BasicAttack, ExecutionResult.Hit, SituationalModifiers.FromHighGround(1.25f));

            Assert.That(fromHigh.Amount, Is.GreaterThan(onLevel.Amount));
        }
    }
}
