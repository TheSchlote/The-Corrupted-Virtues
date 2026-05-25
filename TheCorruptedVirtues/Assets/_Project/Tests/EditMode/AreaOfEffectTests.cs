using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // M2 AoE slice: pins the pure burst math — which tiles light up and which
    // units a burst catches.
    public class AreaOfEffectTests
    {
        private static readonly GridBounds Board = new GridBounds(8, 8);

        [Test]
        public void BurstTiles_Radius1_ReturnsThreeByThree()
        {
            var tiles = AreaOfEffect.BurstTiles(new GridCoord(4, 4), 1, Board);
            Assert.That(tiles.Count, Is.EqualTo(9));
            Assert.That(tiles.Contains(new GridCoord(4, 4)), Is.True); // includes centre
        }

        [Test]
        public void BurstTiles_Radius0_ReturnsOnlyCentre()
        {
            var tiles = AreaOfEffect.BurstTiles(new GridCoord(4, 4), 0, Board);
            Assert.That(tiles.Count, Is.EqualTo(1));
            Assert.That(tiles[0], Is.EqualTo(new GridCoord(4, 4)));
        }

        [Test]
        public void BurstTiles_ClampsToGridEdges()
        {
            // Corner centre: only the in-bounds quadrant of the 3x3 survives.
            var tiles = AreaOfEffect.BurstTiles(new GridCoord(0, 0), 1, Board);
            Assert.That(tiles.Count, Is.EqualTo(4)); // (0,0)(1,0)(0,1)(1,1)
        }

        [Test]
        public void CollectTargets_IncludesEnemiesInBurst_ExcludesAlliesAndOutOfRange()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit enemyIn = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(3, 2));   // chebyshev 1 of (2,2)
            CombatUnit enemyOut = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(5, 5));  // far
            CombatUnit allyIn = BattleTestFactory.Unit(4, Faction.Player, new GridCoord(1, 2));   // in range but friendly

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, enemyIn, enemyOut, allyIn });

            var targets = AreaOfEffect.CollectTargets(new GridCoord(2, 2), 1, Faction.Player, state);

            Assert.That(targets, Has.Member(enemyIn));
            Assert.That(targets, Has.No.Member(enemyOut));
            Assert.That(targets, Has.No.Member(allyIn));
            Assert.That(targets, Has.No.Member(attacker));
        }

        [Test]
        public void CollectTargets_MultiTileUnit_CountedOnce_WhenAnyCellInBurst()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            // 2x2 boss anchored at (3,3) covers (3,3)(4,3)(3,4)(4,4); only (3,3)
            // falls within radius 1 of centre (2,2).
            CombatUnit boss = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(3, 3));
            boss.Footprint = new GridFootprint(2, 2);

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, boss });

            var targets = AreaOfEffect.CollectTargets(new GridCoord(2, 2), 1, Faction.Player, state);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0], Is.SameAs(boss));
        }

        [Test]
        public void CollectTargets_ExcludesDeadEnemies()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit corpse = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(2, 2), hp: 10);
            corpse.Hp = 0;

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, corpse });

            var targets = AreaOfEffect.CollectTargets(new GridCoord(2, 2), 1, Faction.Player, state);

            Assert.That(targets, Is.Empty);
        }
    }
}
