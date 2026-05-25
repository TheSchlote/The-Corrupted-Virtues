using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the matching QTE's input rule (extracted from MatchingController,
    // which once collapsed diagonals to a wrong cardinal and dropped inputs
    // rolled between directions without passing through neutral).
    public class CardinalInputTests
    {
        [TestCase(1, 0, 1, 0)]    // right
        [TestCase(-1, 0, -1, 0)]  // left
        [TestCase(0, 1, 0, 1)]    // up
        [TestCase(0, -1, 0, -1)]  // down
        [TestCase(0, 0, 0, 0)]    // neutral
        [TestCase(1, 1, 0, 0)]    // diagonal -> no input (not forced to a cardinal)
        [TestCase(-1, 1, 0, 0)]   // diagonal
        [TestCase(1, -1, 0, 0)]   // diagonal
        [TestCase(-1, -1, 0, 0)]  // diagonal
        public void ToCardinal_ReducesToCleanCardinalOrZero(int ax, int ay, int ex, int ey)
        {
            Assert.That(CardinalInput.ToCardinal(ax, ay), Is.EqualTo(new GridCoord(ex, ey)));
        }

        [Test]
        public void RegistersPress_FreshPressFromNeutral_True()
        {
            Assert.That(CardinalInput.RegistersPress(new GridCoord(1, 0), new GridCoord(0, 0)), Is.True);
        }

        [Test]
        public void RegistersPress_HeldDirection_DoesNotRepeat()
        {
            Assert.That(CardinalInput.RegistersPress(new GridCoord(1, 0), new GridCoord(1, 0)), Is.False);
        }

        [Test]
        public void RegistersPress_RollBetweenDirections_RegistersWithoutNeutral()
        {
            // Right -> Up directly (never passing through (0,0)) still registers Up.
            Assert.That(CardinalInput.RegistersPress(new GridCoord(0, 1), new GridCoord(1, 0)), Is.True);
        }

        [Test]
        public void RegistersPress_ReleaseToNeutral_DoesNotRegister()
        {
            Assert.That(CardinalInput.RegistersPress(new GridCoord(0, 0), new GridCoord(1, 0)), Is.False);
        }
    }
}
