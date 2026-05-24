using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // Characterization tests: pin the CURRENT behavior of the deterministic
    // combat core so future refactors can't silently change the math.
    public class ExecutionCalculatorTests
    {
        private readonly ExecutionCalculator calc = new ExecutionCalculator();

        // Pins the M2 slice 1 zone layout:
        //   Fumble  [0.00, 0.20)
        //   Miss    [0.20, 0.40)  AND  (0.92, 1.00]  ← overshoot whiffs
        //   Hit     [0.40, 0.85)
        //   Divine  [0.85, 0.92]
        // Boundary cases at 0.84 / 0.93 confirm the Divine edges; values
        // past Divine (0.93, 0.95, 1.00) are now Miss instead of Hit so
        // overshooting Divine is properly punished.
        [TestCase(0.00f, ExecutionResult.Fumble)]
        [TestCase(0.10f, ExecutionResult.Fumble)]
        [TestCase(0.19f, ExecutionResult.Fumble)]
        [TestCase(0.20f, ExecutionResult.Miss)]
        [TestCase(0.39f, ExecutionResult.Miss)]
        [TestCase(0.40f, ExecutionResult.Hit)]
        [TestCase(0.80f, ExecutionResult.Hit)]
        [TestCase(0.84f, ExecutionResult.Hit)]
        [TestCase(0.85f, ExecutionResult.Divine)]
        [TestCase(0.92f, ExecutionResult.Divine)]
        [TestCase(0.93f, ExecutionResult.Miss)]
        [TestCase(0.95f, ExecutionResult.Miss)]
        [TestCase(1.00f, ExecutionResult.Miss)]
        public void Evaluate_ReturnsExpectedTier(float input, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(input), Is.EqualTo(expected));
        }

        // Out-of-range inputs clamp to [0, 1] before evaluation. The high
        // clamp now lands in the overshoot Miss zone (was Hit pre-tuning).
        [TestCase(-5.0f, ExecutionResult.Fumble)]
        [TestCase(5.0f, ExecutionResult.Miss)]
        public void Evaluate_ClampsOutOfRangeInput(float input, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(input), Is.EqualTo(expected));
        }
    }

    public class ExecutionModifiersTests
    {
        [TestCase(ExecutionResult.Fumble, 0.5f)]
        [TestCase(ExecutionResult.Miss, 0.0f)]
        [TestCase(ExecutionResult.Hit, 1.0f)]
        [TestCase(ExecutionResult.Divine, 1.5f)]
        public void GetDamageMultiplier_MapsResultToMultiplier(ExecutionResult result, float expected)
        {
            Assert.That(ExecutionModifiers.GetDamageMultiplier(result), Is.EqualTo(expected).Within(1e-5f));
        }
    }

    public class ElementChartTests
    {
        [Test]
        public void SameElement_IsNeutral()
        {
            Assert.That(ElementChart.GetMultiplier(ElementType.Fire, ElementType.Fire), Is.EqualTo(1.0f).Within(1e-5f));
        }

        [TestCase(ElementType.Water, ElementType.Fire)]
        [TestCase(ElementType.Fire, ElementType.Nature)]
        [TestCase(ElementType.Nature, ElementType.Earth)]
        [TestCase(ElementType.Earth, ElementType.Electricity)]
        [TestCase(ElementType.Electricity, ElementType.Water)]
        [TestCase(ElementType.Light, ElementType.Dark)]
        [TestCase(ElementType.Dark, ElementType.Light)]
        public void Advantage_Is125(ElementType attacker, ElementType defender)
        {
            Assert.That(ElementChart.GetMultiplier(attacker, defender), Is.EqualTo(1.25f).Within(1e-5f));
        }

        [TestCase(ElementType.Fire, ElementType.Water)]
        [TestCase(ElementType.Nature, ElementType.Fire)]
        public void Disadvantage_Is080(ElementType attacker, ElementType defender)
        {
            Assert.That(ElementChart.GetMultiplier(attacker, defender), Is.EqualTo(0.8f).Within(1e-5f));
        }

        [TestCase(ElementType.Water, ElementType.Earth)]
        [TestCase(ElementType.Light, ElementType.Fire)]
        public void Unrelated_IsNeutral(ElementType attacker, ElementType defender)
        {
            Assert.That(ElementChart.GetMultiplier(attacker, defender), Is.EqualTo(1.0f).Within(1e-5f));
        }
    }

    public class DamageCalculatorTests
    {
        // CombatStats(maxHP, maxMP, attack, defense, specialAttack, specialDefense, speed)
        private static CombatStats Attacker() => new CombatStats(100, 0, 50, 0, 50, 0, 0);
        private static CombatStats Defender() => new CombatStats(100, 0, 0, 100, 0, 100, 0);

        [Test]
        public void PhysicalHit_NeutralElement_ComputesDeterministically()
        {
            var ability = new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Fire, 20, 1.0f);

            var r = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Fire, Defender(), ElementType.Fire, ability, ExecutionResult.Hit);

            // pre = 20 + 50*1.0 = 70 ; mitigation = 100/200 = 0.5 ; 70*0.5*1*1 = 35
            Assert.That(r.PreMitigationDamage, Is.EqualTo(70f).Within(1e-4f));
            Assert.That(r.MitigationFactor, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(r.ElementMultiplier, Is.EqualTo(1.0f).Within(1e-4f));
            Assert.That(r.ExecutionMultiplier, Is.EqualTo(1.0f).Within(1e-4f));
            Assert.That(r.FinalDamage, Is.EqualTo(35));
        }

        [Test]
        public void Divine_WithElementAdvantage_RoundsAwayFromZero()
        {
            var ability = new AbilitySpec("Torrent", AbilityKind.Physical, ElementType.Water, 20, 1.0f);

            // element Water vs Fire = 1.25 ; Divine = 1.5 ; 35 * 1.25 * 1.5 = 65.625 -> 66
            var r = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Nature, Defender(), ElementType.Fire, ability, ExecutionResult.Divine);

            Assert.That(r.ElementMultiplier, Is.EqualTo(1.25f).Within(1e-4f));
            Assert.That(r.ExecutionMultiplier, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(r.FinalDamage, Is.EqualTo(66));
        }

        [Test]
        public void Miss_DealsZeroDamage()
        {
            var ability = new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Fire, 20, 1.0f);

            var r = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Fire, Defender(), ElementType.Fire, ability, ExecutionResult.Miss);

            Assert.That(r.FinalDamage, Is.EqualTo(0));
        }

        [Test]
        public void Support_DealsNoDamageButReportsMultipliers()
        {
            var ability = new AbilitySpec("Bless", AbilityKind.Support, ElementType.Light, 0, 0f);

            var r = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Light, Defender(), ElementType.Dark, ability, ExecutionResult.Divine);

            Assert.That(r.FinalDamage, Is.EqualTo(0));
            Assert.That(r.PreMitigationDamage, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(r.MitigationFactor, Is.EqualTo(1.0f).Within(1e-4f));
        }

        [Test]
        public void HighGroundSituational_ScalesFinalDamage()
        {
            var ability = new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Fire, 20, 1.0f);

            // Neutral hit is 35 (see PhysicalHit test); ×1.25 high ground =
            // 43.75 -> 44 (round away from zero).
            var r = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Fire, Defender(), ElementType.Fire, ability, ExecutionResult.Hit,
                SituationalModifiers.FromHighGround(1.25f));

            Assert.That(r.Situational.HighGround, Is.EqualTo(1.25f).Within(1e-4f));
            Assert.That(r.FinalDamage, Is.EqualTo(44));
        }

        [Test]
        public void NoSituational_MatchesExplicitNone()
        {
            var ability = new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Fire, 20, 1.0f);

            var implicitNone = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Fire, Defender(), ElementType.Fire, ability, ExecutionResult.Hit);
            var explicitNone = DamageCalculator.ComputeDamage(
                Attacker(), ElementType.Fire, Defender(), ElementType.Fire, ability, ExecutionResult.Hit,
                SituationalModifiers.None);

            Assert.That(implicitNone.FinalDamage, Is.EqualTo(explicitNone.FinalDamage));
            Assert.That(implicitNone.FinalDamage, Is.EqualTo(35));
            Assert.That(explicitNone.Situational.Product, Is.EqualTo(1.0f).Within(1e-4f));
        }
    }
}
