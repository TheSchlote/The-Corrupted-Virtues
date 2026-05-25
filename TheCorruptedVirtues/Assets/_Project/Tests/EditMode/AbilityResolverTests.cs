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

        // === Area attacks (M2 AoE slice) ===

        private static AbilitySpec Nova()
        {
            return new AbilitySpec("Nova", AbilityKind.Special, ElementType.Fire, power: 20, scaling: 1.0f,
                mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                aoeRadius: 1);
        }

        [Test]
        public void ResolveArea_ReturnsOneOutcomePerTarget_AndDamagesEach()
        {
            CombatUnit attacker = Attacker();
            CombatUnit t1 = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);
            CombatUnit t2 = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(1, 1), hp: 10000, element: ElementType.Light);

            var outcomes = AbilityResolver.ResolveArea(attacker, new[] { t1, t2 }, Nova(), ExecutionResult.Hit, null);

            Assert.That(outcomes.Count, Is.EqualTo(2));
            Assert.That(t1.Hp, Is.LessThan(10000));
            Assert.That(t2.Hp, Is.LessThan(10000));
        }

        [Test]
        public void ResolveArea_FlatGround_AppliesNoSituationalBonus()
        {
            CombatUnit attacker = Attacker();
            CombatUnit areaTarget = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);
            CombatUnit plainTarget = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);

            var outcomes = AbilityResolver.ResolveArea(attacker, new[] { areaTarget }, Nova(), ExecutionResult.Hit, null);
            AbilityOutcome plain = AbilityResolver.Resolve(attacker, plainTarget, Nova(), ExecutionResult.Hit, SituationalModifiers.None);

            // No elevation, no flanking term for AoE -> identical to the no-bonus single hit.
            Assert.That(outcomes[0].Amount, Is.EqualTo(plain.Amount));
        }

        [Test]
        public void ResolveArea_HighGround_BoostsEveryHit()
        {
            ElevationMap elevation = new ElevationMap();
            elevation.SetLevel(new GridCoord(0, 0), 1); // attacker stands high

            CombatUnit attacker = Attacker(); // at (0,0)
            CombatUnit highHit = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);
            CombatUnit flatHit = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(1, 0), hp: 10000, element: ElementType.Light);

            var high = AbilityResolver.ResolveArea(attacker, new[] { highHit }, Nova(), ExecutionResult.Hit, elevation);
            var flat = AbilityResolver.ResolveArea(attacker, new[] { flatHit }, Nova(), ExecutionResult.Hit, null);

            Assert.That(high[0].Amount, Is.GreaterThan(flat[0].Amount));
        }
    }
}
