using NUnit.Framework;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.Tests
{
    // M2 QTE-types slice: pins the matching grader. Grades matched-in-order
    // count over the sequence length on the button-mash band layout; the whole
    // sequence is the only Divine. Difficulty lengthens the sequence.
    public class MatchingCalculatorTests
    {
        private readonly MatchingCalculator calc = new MatchingCalculator();

        // Length 3 maps cleanly onto the bands: 0->Fumble, 1->Miss, 2->Hit, 3->Divine.
        [TestCase(0, 3, ExecutionResult.Fumble)]
        [TestCase(1, 3, ExecutionResult.Miss)]
        [TestCase(2, 3, ExecutionResult.Hit)]
        [TestCase(3, 3, ExecutionResult.Divine)]
        public void Evaluate_LengthThree_MapsMatchedToTier(int matched, int total, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(matched, total), Is.EqualTo(expected));
        }

        // Length 5 (Brutal): partial credit still grades on the fraction.
        [TestCase(0, 5, ExecutionResult.Fumble)]
        [TestCase(1, 5, ExecutionResult.Fumble)] // 0.2
        [TestCase(2, 5, ExecutionResult.Miss)]   // 0.4
        [TestCase(3, 5, ExecutionResult.Hit)]    // 0.6
        [TestCase(4, 5, ExecutionResult.Hit)]    // 0.8
        [TestCase(5, 5, ExecutionResult.Divine)] // 1.0
        public void Evaluate_LengthFive_GradesOnFraction(int matched, int total, ExecutionResult expected)
        {
            Assert.That(calc.Evaluate(matched, total), Is.EqualTo(expected));
        }

        [Test]
        public void Evaluate_OnlyFullSequenceIsDivine()
        {
            Assert.That(calc.Evaluate(3, 4), Is.Not.EqualTo(ExecutionResult.Divine));
            Assert.That(calc.Evaluate(4, 4), Is.EqualTo(ExecutionResult.Divine));
        }

        [Test]
        public void Evaluate_ClampsOutOfRangeMatched()
        {
            Assert.That(calc.Evaluate(-2, 3), Is.EqualTo(ExecutionResult.Fumble));
            Assert.That(calc.Evaluate(9, 3), Is.EqualTo(ExecutionResult.Divine));
        }

        [Test]
        public void Evaluate_ZeroLengthSequence_DoesNotWhiff()
        {
            // A misconfigured (empty) sequence can't punish the player.
            Assert.That(calc.Evaluate(0, 0), Is.EqualTo(ExecutionResult.Hit));
        }

        [Test]
        public void SequenceLength_RisesWithDifficulty()
        {
            int normal = MatchingCalculator.SequenceLength(QteDifficulty.Normal);
            int hard = MatchingCalculator.SequenceLength(QteDifficulty.Hard);
            int brutal = MatchingCalculator.SequenceLength(QteDifficulty.Brutal);

            Assert.That(normal, Is.EqualTo(3));
            Assert.That(hard, Is.GreaterThan(normal));
            Assert.That(brutal, Is.GreaterThan(hard));
        }
    }
}
