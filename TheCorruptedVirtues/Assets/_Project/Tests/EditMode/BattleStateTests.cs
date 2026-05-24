using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the roster / spatial / win queries extracted from the orchestrator
    // into BattleState.
    public class BattleStateTests
    {
        private static BattleState StateWith(params CombatUnit[] units)
        {
            BattleState state = new BattleState();
            state.SetRoster(units);
            return state;
        }

        [Test]
        public void FindById_ReturnsMatchingUnit()
        {
            CombatUnit a = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit b = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5));
            BattleState state = StateWith(a, b);

            Assert.That(state.FindById(new UnitId(2)), Is.SameAs(b));
        }

        [Test]
        public void FindById_UnknownId_ReturnsNull()
        {
            BattleState state = StateWith(BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0)));
            Assert.That(state.FindById(new UnitId(99)), Is.Null);
        }

        [Test]
        public void GetLivingUnitAt_ReturnsUnitOnTile()
        {
            CombatUnit a = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(3, 4));
            BattleState state = StateWith(a);

            Assert.That(state.GetLivingUnitAt(new GridCoord(3, 4)), Is.SameAs(a));
            Assert.That(state.GetLivingUnitAt(new GridCoord(0, 0)), Is.Null);
        }

        [Test]
        public void GetLivingUnitAt_DeadUnit_ReturnsNull()
        {
            CombatUnit dead = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(2, 2));
            dead.Hp = 0;
            BattleState state = StateWith(dead);

            Assert.That(state.GetLivingUnitAt(new GridCoord(2, 2)), Is.Null);
        }

        [Test]
        public void RebuildOccupancy_OccupiesLivingTilesOnly()
        {
            CombatUnit alive = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(1, 1));
            CombatUnit dead = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(2, 2));
            dead.Hp = 0;
            BattleState state = StateWith(alive, dead);

            Assert.That(state.Occupancy.IsOccupied(new GridCoord(1, 1)), Is.True);
            Assert.That(state.Occupancy.IsOccupied(new GridCoord(2, 2)), Is.False);
        }

        [Test]
        public void RebuildOccupancy_TracksMovedUnit()
        {
            CombatUnit u = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(1, 1));
            BattleState state = StateWith(u);

            u.Coord = new GridCoord(4, 1);
            state.RebuildOccupancy();

            Assert.That(state.Occupancy.IsOccupied(new GridCoord(1, 1)), Is.False);
            Assert.That(state.Occupancy.IsOccupied(new GridCoord(4, 1)), Is.True);
        }

        [Test]
        public void TryGetWinner_BothSidesAlive_NotDecided()
        {
            BattleState state = StateWith(
                BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0)),
                BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5)));

            Assert.That(state.TryGetWinner(Faction.Player, out _), Is.False);
        }

        [Test]
        public void TryGetWinner_OnlyPlayersAlive_PlayerWins()
        {
            CombatUnit enemy = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5));
            enemy.Hp = 0;
            BattleState state = StateWith(
                BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0)),
                enemy);

            Assert.That(state.TryGetWinner(Faction.Enemy, out Faction winner), Is.True);
            Assert.That(winner, Is.EqualTo(Faction.Player));
        }

        [Test]
        public void TryGetWinner_OnlyEnemiesAlive_EnemyWins()
        {
            CombatUnit player = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            player.Hp = 0;
            BattleState state = StateWith(
                player,
                BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5)));

            Assert.That(state.TryGetWinner(Faction.Player, out Faction winner), Is.True);
            Assert.That(winner, Is.EqualTo(Faction.Enemy));
        }

        [Test]
        public void TryGetWinner_NobodyAlive_FallsBackToTieBreak()
        {
            CombatUnit player = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit enemy = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5));
            player.Hp = 0;
            enemy.Hp = 0;
            BattleState state = StateWith(player, enemy);

            Assert.That(state.TryGetWinner(Faction.Enemy, out Faction winner), Is.True);
            Assert.That(winner, Is.EqualTo(Faction.Enemy));
        }

        [Test]
        public void GetLivingUnitAt_MultiTileUnit_FoundOnEveryCoveredCell()
        {
            CombatUnit beast = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(2, 2));
            beast.Footprint = new GridFootprint(2, 2);
            BattleState state = StateWith(beast);

            Assert.That(state.GetLivingUnitAt(new GridCoord(2, 2)), Is.SameAs(beast));
            Assert.That(state.GetLivingUnitAt(new GridCoord(3, 2)), Is.SameAs(beast));
            Assert.That(state.GetLivingUnitAt(new GridCoord(2, 3)), Is.SameAs(beast));
            Assert.That(state.GetLivingUnitAt(new GridCoord(3, 3)), Is.SameAs(beast));
            Assert.That(state.GetLivingUnitAt(new GridCoord(4, 4)), Is.Null);
        }

        [Test]
        public void RebuildOccupancy_MultiTileUnit_OccupiesEveryCoveredCell()
        {
            CombatUnit beast = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(2, 2));
            beast.Footprint = new GridFootprint(2, 2);
            BattleState state = StateWith(beast);

            Assert.That(state.Occupancy.IsOccupied(new GridCoord(2, 2)), Is.True);
            Assert.That(state.Occupancy.IsOccupied(new GridCoord(3, 3)), Is.True);
            Assert.That(state.Occupancy.IsOccupied(new GridCoord(2, 4)), Is.False);
        }
    }
}
