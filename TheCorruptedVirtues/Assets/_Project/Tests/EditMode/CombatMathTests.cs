using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // Characterization tests: pin the CURRENT behavior of the deterministic
    // combat core so future refactors can't silently change the math.
    public class ExecutionCalculatorTests
    {
        private readonly ExecutionCalculator calc = new ExecutionCalculator();

        [TestCase(0.00f, ExecutionResult.Fumble)]
        [TestCase(0.10f, ExecutionResult.Fumble)]
        [TestCase(0.20f, ExecutionResult.Miss)]
        [TestCase(0.39f, ExecutionResult.Miss)]
        [TestCase(0.40f, ExecutionResult.Hit)]
        [TestCase(0.79f, ExecutionResult.Hit)]
        [TestCase(0.80f, ExecutionResult.Divine)]
        [TestCase(0.95f, ExecutionResult.Divine)]
        [TestCase(0.96f, ExecutionResult.Hit)]
        [TestCase(1.00f, ExecutionResult.Hit)]
        public void Evaluate_ReturnsExpectedTier(float input, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(input), Is.EqualTo(expected));
        }

        [TestCase(-5.0f, ExecutionResult.Fumble)]
        [TestCase(5.0f, ExecutionResult.Hit)]
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
    }
}
