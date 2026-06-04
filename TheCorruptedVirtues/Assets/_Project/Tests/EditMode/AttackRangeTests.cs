using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the "reach" overlay geometry: which tiles a selected attack can
    // affect from the unit's position, by shape (melee / beam / burst / support).
    public class AttackRangeTests
    {
        private static readonly GridBounds Board = new GridBounds(10, 10);

        private static AbilitySpec Melee() =>
            new AbilitySpec("Jab", AbilityKind.Physical, ElementType.Light, 10, 1f);

        private static AbilitySpec Beam(int len) => new AbilitySpec(
            "Beam", AbilityKind.Special, ElementType.Light, 10, 1f,
            mpCost: 5, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
            aoeRadius: 0, targeting: null, range: 1, lineLength: len);

        private static AbilitySpec Burst(int radius) => new AbilitySpec(
            "Burst", AbilityKind.Special, ElementType.Fire, 10, 1f,
            mpCost: 5, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal, aoeRadius: radius);

        private static AbilitySpec Heal() =>
            new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, 10, 1f);

        [Test]
        public void Melee_IsTheFourAdjacentTiles()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(5, 5));
            var tiles = AttackRange.Tiles(Melee(), u, Board);
            Assert.That(tiles.Count, Is.EqualTo(4));
            Assert.That(tiles, Has.Member(new GridCoord(6, 5)));
            Assert.That(tiles, Has.Member(new GridCoord(4, 5)));
            Assert.That(tiles, Has.Member(new GridCoord(5, 6)));
            Assert.That(tiles, Has.Member(new GridCoord(5, 4)));
            Assert.That(tiles, Has.No.Member(new GridCoord(5, 5))); // not the unit's own tile
        }

        [Test]
        public void Beam_IsTheFourCardinalArms()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(5, 5));
            var tiles = AttackRange.Tiles(Beam(3), u, Board);
            Assert.That(tiles.Count, Is.EqualTo(12)); // 3 tiles x 4 arms, own tile excluded
            Assert.That(tiles, Has.Member(new GridCoord(8, 5)));   // +X tip
            Assert.That(tiles, Has.Member(new GridCoord(5, 2)));   // -Y tip
            Assert.That(tiles, Has.No.Member(new GridCoord(6, 6))); // diagonals not covered
        }

        [Test]
        public void Beam_ClampsAtBoardEdge()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(1, 1));
            var tiles = AttackRange.Tiles(Beam(3), u, Board);
            Assert.That(tiles, Has.Member(new GridCoord(0, 1)));    // -X arm reaches the edge
            Assert.That(tiles, Has.No.Member(new GridCoord(-1, 1))); // and stops there
        }

        [Test]
        public void Burst_CoversTheRangePlusRadiusEnvelope()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(5, 5));
            // Range 1, radius 1: aim centres are the 4 adjacent tiles, each
            // bursting 3x3 — so a tile two out (7,5) is reachable.
            var tiles = AttackRange.Tiles(Burst(1), u, Board);
            Assert.That(tiles, Has.Member(new GridCoord(7, 5)));
            Assert.That(tiles, Has.Member(new GridCoord(6, 6)));
            Assert.That(tiles, Has.No.Member(new GridCoord(5, 5))); // own tile excluded
        }

        [Test]
        public void Support_HasNoRange()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(5, 5));
            Assert.That(AttackRange.Tiles(Heal(), u, Board), Is.Empty);
        }
    }
}
