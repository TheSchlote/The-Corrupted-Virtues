using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the flanking tiers: front (looking at you) is neutral, a perpendicular
    // hit is a side flank, and a hit from directly behind is the rear bonus.
    // Constants are referenced (not literals) so playtest tuning keeps tests green.
    public class FlankingRulesTests
    {
        // Target at (5,5). Attacker steps in from each cardinal side.
        private static readonly GridCoord Target = new GridCoord(5, 5);

        [TestCase(5, 6, FlankingRules.FrontBonus)] // north of a North-facer = in front
        [TestCase(5, 4, FlankingRules.BackBonus)]  // south = directly behind
        [TestCase(6, 5, FlankingRules.SideBonus)]  // east = flank
        [TestCase(4, 5, FlankingRules.SideBonus)]  // west = flank
        public void NorthFacingTarget_TierByAttackerSide(int ax, int ay, float expected)
        {
            float m = FlankingRules.Multiplier(new GridCoord(ax, ay), Target, Facing.North);
            Assert.That(m, Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(6, 5, FlankingRules.FrontBonus)] // east of an East-facer = in front
        [TestCase(4, 5, FlankingRules.BackBonus)]  // west = behind
        [TestCase(5, 6, FlankingRules.SideBonus)]  // north = flank
        [TestCase(5, 4, FlankingRules.SideBonus)]  // south = flank
        public void EastFacingTarget_TierByAttackerSide(int ax, int ay, float expected)
        {
            float m = FlankingRules.Multiplier(new GridCoord(ax, ay), Target, Facing.East);
            Assert.That(m, Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void BackExceedsSideExceedsFront()
        {
            Assert.That(FlankingRules.BackBonus, Is.GreaterThan(FlankingRules.SideBonus));
            Assert.That(FlankingRules.SideBonus, Is.GreaterThan(FlankingRules.FrontBonus));
        }
    }
}
