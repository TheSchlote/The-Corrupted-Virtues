using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the auto-facing direction math.
    public class FacingRulesTests
    {
        [Test]
        public void Toward_StepEast_FacesEast()
        {
            Assert.That(FacingRules.Toward(new GridCoord(1, 1), new GridCoord(2, 1)), Is.EqualTo(Facing.East));
        }

        [Test]
        public void Toward_StepWest_FacesWest()
        {
            Assert.That(FacingRules.Toward(new GridCoord(2, 1), new GridCoord(1, 1)), Is.EqualTo(Facing.West));
        }

        [Test]
        public void Toward_StepNorth_FacesNorth()
        {
            Assert.That(FacingRules.Toward(new GridCoord(1, 1), new GridCoord(1, 2)), Is.EqualTo(Facing.North));
        }

        [Test]
        public void Toward_StepSouth_FacesSouth()
        {
            Assert.That(FacingRules.Toward(new GridCoord(1, 2), new GridCoord(1, 1)), Is.EqualTo(Facing.South));
        }

        [TestCase(Facing.North, Facing.South)]
        [TestCase(Facing.South, Facing.North)]
        [TestCase(Facing.East, Facing.West)]
        [TestCase(Facing.West, Facing.East)]
        public void Opposite_Inverts(Facing facing, Facing expected)
        {
            Assert.That(FacingRules.Opposite(facing), Is.EqualTo(expected));
        }

        [Test]
        public void Toward_Diagonal_PicksDominantAxis()
        {
            // Mostly-east displacement resolves to East; mostly-north to North.
            Assert.That(FacingRules.Toward(new GridCoord(0, 0), new GridCoord(3, 1)), Is.EqualTo(Facing.East));
            Assert.That(FacingRules.Toward(new GridCoord(0, 0), new GridCoord(1, 3)), Is.EqualTo(Facing.North));
        }
    }
}
