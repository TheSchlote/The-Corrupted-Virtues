using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the combined position modifiers: high ground and flanking multiply,
    // and either being absent leaves a 1.0 term.
    public class CombatSituationTests
    {
        private static CombatUnit UnitAt(int id, GridCoord coord, Facing facing)
        {
            CombatUnit u = BattleTestFactory.Unit(id, Faction.Player, coord);
            u.Facing = facing;
            return u;
        }

        [Test]
        public void HighGroundAndRear_Stack()
        {
            var elevation = new ElevationMap();
            elevation.SetLevel(new GridCoord(5, 4), 1); // attacker stands high

            CombatUnit attacker = UnitAt(1, new GridCoord(5, 4), Facing.North); // south of target = behind it
            CombatUnit target = UnitAt(2, new GridCoord(5, 5), Facing.North);

            SituationalModifiers mods = CombatSituation.For(attacker, target, elevation);

            Assert.That(mods.HighGround, Is.EqualTo(ElevationRules.HighGroundBonus).Within(1e-5f));
            Assert.That(mods.Flanking, Is.EqualTo(FlankingRules.BackBonus).Within(1e-5f));
            Assert.That(mods.Product, Is.EqualTo(ElevationRules.HighGroundBonus * FlankingRules.BackBonus).Within(1e-5f));
        }

        [Test]
        public void NullElevation_LeavesFlankingOnly()
        {
            CombatUnit attacker = UnitAt(1, new GridCoord(6, 5), Facing.North); // east of target = flank
            CombatUnit target = UnitAt(2, new GridCoord(5, 5), Facing.North);

            SituationalModifiers mods = CombatSituation.For(attacker, target, null);

            Assert.That(mods.HighGround, Is.EqualTo(1.0f).Within(1e-5f));
            Assert.That(mods.Flanking, Is.EqualTo(FlankingRules.SideBonus).Within(1e-5f));
        }

        [Test]
        public void FrontalLevelAttack_IsNeutral()
        {
            CombatUnit attacker = UnitAt(1, new GridCoord(5, 6), Facing.South); // north of a North-facer = front
            CombatUnit target = UnitAt(2, new GridCoord(5, 5), Facing.North);

            SituationalModifiers mods = CombatSituation.For(attacker, target, null);

            Assert.That(mods.Product, Is.EqualTo(1.0f).Within(1e-5f));
        }
    }
}
