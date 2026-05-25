using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // M2 QTE-types slice: pins the timed-press grader. The band is symmetric
    // about TargetCenter (0.5); harder difficulties shrink the Divine/Hit
    // half-widths.
    public class TimedPressCalculatorTests
    {
        // Normal half-widths: Divine 0.05, Hit 0.20, Miss 0.34 (distances from
        // 0.5). Cases sit clearly inside each band, not on the razor edge —
        // marker position is a continuous gameplay float, so exact-edge
        // classification is measure-zero and fragile in float arithmetic.
        [TestCase(0.50f, ExecutionResult.Divine)] // dead centre
        [TestCase(0.53f, ExecutionResult.Divine)] // dist 0.03, inside Divine
        [TestCase(0.47f, ExecutionResult.Divine)] // symmetric, inside Divine
        [TestCase(0.58f, ExecutionResult.Hit)]    // dist 0.08, inside Hit
        [TestCase(0.68f, ExecutionResult.Hit)]    // dist 0.18, inside Hit
        [TestCase(0.74f, ExecutionResult.Miss)]   // dist 0.24, inside Miss
        [TestCase(0.82f, ExecutionResult.Miss)]   // dist 0.32, inside Miss
        [TestCase(0.88f, ExecutionResult.Fumble)] // dist 0.38, past the band
        [TestCase(0.00f, ExecutionResult.Fumble)] // never pressed -> marker at start
        [TestCase(1.00f, ExecutionResult.Fumble)] // missed the press -> marker at end
        public void Evaluate_Normal_MapsDistanceFromCentreToTier(float pressValue, ExecutionResult expected)
        {
            var calc = new TimedPressCalculator(QteDifficulty.Normal);
            Assert.That(calc.Evaluate(pressValue), Is.EqualTo(expected));
        }

        [Test]
        public void DefaultConstructor_IsNormal()
        {
            var byDefault = new TimedPressCalculator();
            var normal = new TimedPressCalculator(QteDifficulty.Normal);
            Assert.That(byDefault.DivineHalf, Is.EqualTo(normal.DivineHalf).Within(1e-5f));
            Assert.That(byDefault.HitHalf, Is.EqualTo(normal.HitHalf).Within(1e-5f));
        }

        [Test]
        public void Band_IsSymmetricAboutCentre()
        {
            var calc = new TimedPressCalculator(QteDifficulty.Normal);
            // Equal distance either side of centre grades the same tier.
            Assert.That(calc.Evaluate(TimedPressCalculator.TargetCenter + 0.18f),
                Is.EqualTo(calc.Evaluate(TimedPressCalculator.TargetCenter - 0.18f)));
        }

        [Test]
        public void HarderDifficulty_NarrowsDivineAndHitWindows()
        {
            var normal = new TimedPressCalculator(QteDifficulty.Normal);
            var hard = new TimedPressCalculator(QteDifficulty.Hard);
            var brutal = new TimedPressCalculator(QteDifficulty.Brutal);

            Assert.That(hard.DivineHalf, Is.LessThan(normal.DivineHalf));
            Assert.That(brutal.DivineHalf, Is.LessThan(hard.DivineHalf));
            Assert.That(hard.HitHalf, Is.LessThan(normal.HitHalf));
            Assert.That(brutal.HitHalf, Is.LessThan(hard.HitHalf));
        }

        [Test]
        public void SamePress_DivineOnNormal_DropsToHitOnHarder()
        {
            // dist 0.043 from centre: inside Normal's Divine window (0.05) but
            // outside Hard's tighter one (0.035), so the same press downgrades.
            Assert.That(new TimedPressCalculator(QteDifficulty.Normal).Evaluate(0.543f), Is.EqualTo(ExecutionResult.Divine));
            Assert.That(new TimedPressCalculator(QteDifficulty.Hard).Evaluate(0.543f), Is.EqualTo(ExecutionResult.Hit));
        }
    }
}
