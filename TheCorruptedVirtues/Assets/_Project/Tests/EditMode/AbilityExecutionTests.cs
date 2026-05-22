using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // M2 slice 2 characterization: pins the new ability/QTE core — the
    // difficulty-scaled swing grading, the button-mash grader, Support
    // healing, and the AbilitySpec MP/QTE fields.

    public class ExecutionCalculatorDifficultyTests
    {
        // Normal reproduces the slice-1 window (a second guard alongside the
        // boundary cases in ExecutionCalculatorTests).
        [Test]
        public void Normal_DivineWindow_MatchesSlice1()
        {
            var calc = new ExecutionCalculator(QteDifficulty.Normal);
            Assert.That(calc.DivineMin, Is.EqualTo(0.85f).Within(1e-5f));
            Assert.That(calc.DivineMax, Is.EqualTo(0.92f).Within(1e-5f));
        }

        [Test]
        public void DefaultConstructor_IsNormal()
        {
            var byDefault = new ExecutionCalculator();
            var normal = new ExecutionCalculator(QteDifficulty.Normal);
            Assert.That(byDefault.DivineMin, Is.EqualTo(normal.DivineMin).Within(1e-5f));
            Assert.That(byDefault.DivineMax, Is.EqualTo(normal.DivineMax).Within(1e-5f));
        }

        [Test]
        public void HarderDifficulty_NarrowsDivineWindow()
        {
            float normal = Width(QteDifficulty.Normal);
            float hard = Width(QteDifficulty.Hard);
            float brutal = Width(QteDifficulty.Brutal);

            Assert.That(hard, Is.LessThan(normal));
            Assert.That(brutal, Is.LessThan(hard));

            static float Width(QteDifficulty d)
            {
                var c = new ExecutionCalculator(d);
                return c.DivineMax - c.DivineMin;
            }
        }

        // 0.85 is the Divine lower edge on Normal; harder settings push the
        // edge inward so the same input no longer scores Divine.
        [Test]
        public void ValueAtNormalDivineEdge_DropsBelowDivineOnHarder()
        {
            Assert.That(new ExecutionCalculator(QteDifficulty.Normal).Evaluate(0.85f), Is.EqualTo(ExecutionResult.Divine));
            Assert.That(new ExecutionCalculator(QteDifficulty.Hard).Evaluate(0.85f), Is.EqualTo(ExecutionResult.Hit));
            Assert.That(new ExecutionCalculator(QteDifficulty.Brutal).Evaluate(0.85f), Is.EqualTo(ExecutionResult.Hit));
        }

        [Test]
        public void OvershootShrinks_HighValueWhiffsSoonerOnHard()
        {
            // 0.92 is Divine on Normal but overshoots the 0.905 Hard ceiling.
            Assert.That(new ExecutionCalculator(QteDifficulty.Normal).Evaluate(0.92f), Is.EqualTo(ExecutionResult.Divine));
            Assert.That(new ExecutionCalculator(QteDifficulty.Hard).Evaluate(0.92f), Is.EqualTo(ExecutionResult.Miss));
        }
    }

    public class ButtonMashCalculatorTests
    {
        private readonly ButtonMashCalculator calc = new ButtonMashCalculator();

        [TestCase(0.00f, ExecutionResult.Fumble)]
        [TestCase(0.29f, ExecutionResult.Fumble)]
        [TestCase(0.30f, ExecutionResult.Miss)]
        [TestCase(0.59f, ExecutionResult.Miss)]
        [TestCase(0.60f, ExecutionResult.Hit)]
        [TestCase(0.99f, ExecutionResult.Hit)]
        [TestCase(1.00f, ExecutionResult.Divine)]
        public void Evaluate_MapsFillRatioToTier(float ratio, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(ratio), Is.EqualTo(expected));
        }

        [Test]
        public void Evaluate_ExceedingTarget_CapsAtDivine_NoOvershootPenalty()
        {
            // Unlike the swing meter, more presses never whiffs.
            Assert.That(calc.Evaluate(1.5f), Is.EqualTo(ExecutionResult.Divine));
            Assert.That(calc.Evaluate(99.0f), Is.EqualTo(ExecutionResult.Divine));
        }

        [Test]
        public void TargetPresses_RisesWithDifficulty()
        {
            int normal = ButtonMashCalculator.TargetPresses(QteDifficulty.Normal);
            int hard = ButtonMashCalculator.TargetPresses(QteDifficulty.Hard);
            int brutal = ButtonMashCalculator.TargetPresses(QteDifficulty.Brutal);

            Assert.That(normal, Is.EqualTo(6));
            Assert.That(hard, Is.GreaterThan(normal));
            Assert.That(brutal, Is.GreaterThan(hard));
        }
    }

    public class HealCalculatorTests
    {
        // CombatStats(maxHP, maxMP, attack, defense, specialAttack, specialDefense, speed)
        private static CombatStats Healer() => new CombatStats(100, 50, 0, 0, 40, 0, 0);

        [Test]
        public void Hit_HealsPowerPlusSpecialScaling()
        {
            var ability = new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, 20, 0.5f, 8, QteType.SwingMeter, QteDifficulty.Normal);

            // base = 20 + 40*0.5 = 40 ; Hit x1.0 = 40
            var r = HealCalculator.ComputeHeal(Healer(), ability, ExecutionResult.Hit);

            Assert.That(r.BaseHeal, Is.EqualTo(40f).Within(1e-4f));
            Assert.That(r.ExecutionMultiplier, Is.EqualTo(1.0f).Within(1e-4f));
            Assert.That(r.FinalHeal, Is.EqualTo(40));
        }

        [Test]
        public void Divine_Scales150Percent_RoundsAwayFromZero()
        {
            var ability = new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, 15, 0.5f, 8, QteType.SwingMeter, QteDifficulty.Normal);

            // base = 15 + 40*0.5 = 35 ; Divine x1.5 = 52.5 -> 53
            var r = HealCalculator.ComputeHeal(Healer(), ability, ExecutionResult.Divine);

            Assert.That(r.FinalHeal, Is.EqualTo(53));
        }

        [Test]
        public void Miss_HealsNothing()
        {
            var ability = new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, 20, 0.5f, 8, QteType.SwingMeter, QteDifficulty.Normal);

            var r = HealCalculator.ComputeHeal(Healer(), ability, ExecutionResult.Miss);

            Assert.That(r.FinalHeal, Is.EqualTo(0));
        }
    }

    public class AbilitySpecTests
    {
        [Test]
        public void BasicConstructor_DefaultsToFreeSwingMeterNormal()
        {
            var a = new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Fire, 10, 1.0f);

            Assert.That(a.MpCost, Is.EqualTo(0));
            Assert.That(a.QteType, Is.EqualTo(QteType.SwingMeter));
            Assert.That(a.QteDifficulty, Is.EqualTo(QteDifficulty.Normal));
        }

        [Test]
        public void FullConstructor_SetsMpAndQteFields()
        {
            var a = new AbilitySpec("Nova", AbilityKind.Special, ElementType.Fire, 30, 1.2f, 12, QteType.ButtonMash, QteDifficulty.Hard);

            Assert.That(a.MpCost, Is.EqualTo(12));
            Assert.That(a.QteType, Is.EqualTo(QteType.ButtonMash));
            Assert.That(a.QteDifficulty, Is.EqualTo(QteDifficulty.Hard));
        }
    }
}
