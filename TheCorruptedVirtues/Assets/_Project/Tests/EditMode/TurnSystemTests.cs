using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the round-queue walking extracted from the orchestrator into
    // TurnSystem. (Speed/tie ordering itself is pinned by TurnOrderTests; this
    // covers the queue advancement, dead-skip, round refill, and upcoming
    // strip on top of it.)
    public class TurnSystemTests
    {
        private static TurnSystem Started(out BattleState state, params CombatUnit[] units)
        {
            state = new BattleState();
            state.SetRoster(units);
            TurnSystem turns = new TurnSystem(state);
            turns.StartNewRound();
            return turns;
        }

        private static List<int> AdvanceIds(TurnSystem turns, int times)
        {
            List<int> ids = new List<int>();
            for (int i = 0; i < times; i++)
            {
                CombatUnit u = turns.AdvanceToNextLivingUnit();
                ids.Add(u != null ? u.Id.Value : -1);
            }
            return ids;
        }

        [Test]
        public void Advance_WalksUnitsHighestSpeedFirst()
        {
            TurnSystem turns = Started(out _,
                BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), speed: 5),
                BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5), speed: 12),
                BattleTestFactory.Unit(3, Faction.Player, new GridCoord(0, 1), speed: 8));

            Assert.That(AdvanceIds(turns, 3), Is.EqualTo(new[] { 2, 3, 1 }));
        }

        [Test]
        public void Advance_EqualSpeed_TieBreaksByLowerId()
        {
            TurnSystem turns = Started(out _,
                BattleTestFactory.Unit(3, Faction.Player, new GridCoord(0, 0), speed: 10),
                BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), speed: 10),
                BattleTestFactory.Unit(2, Faction.Player, new GridCoord(0, 1), speed: 10));

            Assert.That(AdvanceIds(turns, 3), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Advance_PastEndOfRound_StartsNextRound()
        {
            TurnSystem turns = Started(out _,
                BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), speed: 12),
                BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5), speed: 8));

            // Round 1: 1, 2 — then round 2 begins: 1 again.
            Assert.That(AdvanceIds(turns, 3), Is.EqualTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public void Advance_SkipsUnitThatDiedAfterRoundBuilt()
        {
            CombatUnit fast = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), speed: 12);
            CombatUnit slow = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5), speed: 8);
            TurnSystem turns = Started(out _, fast, slow);

            // 1 is enqueued ahead of 2, but dies before its turn comes up.
            fast.Hp = 0;

            Assert.That(turns.AdvanceToNextLivingUnit().Id.Value, Is.EqualTo(2));
        }

        [Test]
        public void Advance_AllDead_ReturnsNull()
        {
            CombatUnit only = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            TurnSystem turns = Started(out _, only);
            only.Hp = 0;

            Assert.That(turns.AdvanceToNextLivingUnit(), Is.Null);
        }

        [Test]
        public void BuildUpcoming_ActiveFirstThenRoundThenNextRound()
        {
            TurnSystem turns = Started(out _,
                BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), speed: 5),
                BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5), speed: 12),
                BattleTestFactory.Unit(3, Faction.Player, new GridCoord(0, 1), speed: 8));

            turns.AdvanceToNextLivingUnit(); // active = 2; remaining round = 3, 1

            List<UnitId> upcoming = new List<UnitId>();
            turns.BuildUpcoming(6, upcoming);

            List<int> ids = upcoming.ConvertAll(u => u.Value);
            // active 2, rest of round 3 1, then start of next round 2 3 1.
            Assert.That(ids, Is.EqualTo(new[] { 2, 3, 1, 2, 3, 1 }));
        }

        [Test]
        public void BuildUpcoming_RespectsCountAndExcludesDead()
        {
            CombatUnit a = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0), speed: 5);
            CombatUnit b = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 5), speed: 12);
            CombatUnit c = BattleTestFactory.Unit(3, Faction.Player, new GridCoord(0, 1), speed: 8);
            TurnSystem turns = Started(out _, a, b, c);

            turns.AdvanceToNextLivingUnit(); // active = 2; remaining round = 3, 1
            c.Hp = 0;                          // unit 3 dies before being shown

            List<UnitId> upcoming = new List<UnitId>();
            turns.BuildUpcoming(3, upcoming);

            List<int> ids = upcoming.ConvertAll(u => u.Value);
            Assert.That(ids.Count, Is.EqualTo(3));
            Assert.That(ids[0], Is.EqualTo(2));            // active first
            Assert.That(ids, Has.None.EqualTo(3));         // dead excluded
        }
    }
}
