using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the high-ground rule (M2 terrain slice): binary, bonus-only. The
    // magnitude is intentionally a single named constant so playtest tuning is
    // a one-line change the tests track.
    public class ElevationRulesTests
    {
        [Test]
        public void HigherGround_GrantsBonus()
        {
            Assert.That(
                ElevationRules.HighGroundMultiplier(1, 0),
                Is.EqualTo(ElevationRules.HighGroundBonus).Within(1e-5f));
        }

        [Test]
        public void EqualGround_IsNeutral()
        {
            Assert.That(ElevationRules.HighGroundMultiplier(1, 1), Is.EqualTo(1.0f).Within(1e-5f));
        }

        [Test]
        public void LowerGround_IsNeutral_NoUphillPenalty()
        {
            Assert.That(ElevationRules.HighGroundMultiplier(0, 1), Is.EqualTo(1.0f).Within(1e-5f));
        }

        [Test]
        public void BinaryRule_AnyHeightAdvantage_IsTheSameBonus()
        {
            // A 2-level edge is no bigger than a 1-level edge — the rule is
            // binary, not graded (that was a deliberate tuning choice).
            Assert.That(
                ElevationRules.HighGroundMultiplier(3, 1),
                Is.EqualTo(ElevationRules.HighGroundMultiplier(2, 1)).Within(1e-5f));
        }

        [Test]
        public void ModifiersFor_NullMap_IsNone()
        {
            SituationalModifiers mods = ElevationRules.ModifiersFor(new GridCoord(0, 0), new GridCoord(1, 0), null);
            Assert.That(mods.HighGround, Is.EqualTo(1.0f).Within(1e-5f));
            Assert.That(mods.Flanking, Is.EqualTo(1.0f).Within(1e-5f));
            Assert.That(mods.Product, Is.EqualTo(1.0f).Within(1e-5f));
        }

        [Test]
        public void ModifiersFor_AttackerOnHighGround_SetsHighGroundOnly()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(0, 0), 1);
            SituationalModifiers mods = ElevationRules.ModifiersFor(new GridCoord(0, 0), new GridCoord(1, 0), map);

            Assert.That(mods.HighGround, Is.EqualTo(ElevationRules.HighGroundBonus).Within(1e-5f));
            // Flanking is the facing slice's term; the terrain slice never sets it.
            Assert.That(mods.Flanking, Is.EqualTo(1.0f).Within(1e-5f));
        }

        [Test]
        public void ModifiersFor_AttackerOnLowGround_IsNone()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(1, 0), 1);
            SituationalModifiers mods = ElevationRules.ModifiersFor(new GridCoord(0, 0), new GridCoord(1, 0), map);

            Assert.That(mods.HighGround, Is.EqualTo(1.0f).Within(1e-5f));
        }
    }
}
