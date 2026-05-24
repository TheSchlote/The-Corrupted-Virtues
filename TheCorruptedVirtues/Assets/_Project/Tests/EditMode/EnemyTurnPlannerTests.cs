using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the enemy AI heuristic extracted into EnemyTurnPlanner: target the
    // nearest opponent, attack if already adjacent, else approach and stop one
    // tile short (attacking if that lands adjacent).
    public class EnemyTurnPlannerTests
    {
        private static readonly GridBounds Bounds = new GridBounds(8, 8);

        private static BattleState StateWith(params CombatUnit[] units)
        {
            BattleState state = new BattleState();
            state.SetRoster(units);
            return state;
        }

        [Test]
        public void FindNearestEnemy_PicksClosestOpponent()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5));
            CombatUnit far = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(1, 1));
            CombatUnit near = BattleTestFactory.Unit(3, Faction.Player, new GridCoord(5, 3));
            BattleState state = StateWith(actor, far, near);

            Assert.That(EnemyTurnPlanner.FindNearestEnemy(actor, state), Is.SameAs(near));
        }

        [Test]
        public void FindNearestEnemy_IgnoresAlliesAndDead()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5));
            CombatUnit ally = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 4));
            CombatUnit deadFoe = BattleTestFactory.Unit(3, Faction.Player, new GridCoord(5, 6));
            deadFoe.Hp = 0;
            CombatUnit liveFoe = BattleTestFactory.Unit(4, Faction.Player, new GridCoord(0, 5));
            BattleState state = StateWith(actor, ally, deadFoe, liveFoe);

            Assert.That(EnemyTurnPlanner.FindNearestEnemy(actor, state), Is.SameAs(liveFoe));
        }

        [Test]
        public void Plan_AdjacentTarget_AttacksInPlace()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5));
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(5, 4));
            BattleState state = StateWith(actor, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.Target, Is.SameAs(foe));
            Assert.That(plan.HasMove, Is.False);
            Assert.That(plan.AttackAfterMove, Is.True);
        }

        [Test]
        public void Plan_ReachableTarget_ApproachesStopsAdjacentAndAttacks()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), moveRange: 4);
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(0, 5));
            BattleState state = StateWith(actor, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.Target, Is.SameAs(foe));
            Assert.That(plan.HasMove, Is.True);
            // Stops one tile short of the occupied target tile, i.e. adjacent.
            GridCoord finalCoord = plan.MovePath[plan.MovePath.Count - 1];
            Assert.That(GridMath.ManhattanDistance(finalCoord, foe.Coord), Is.EqualTo(1));
            Assert.That(plan.AttackAfterMove, Is.True);
        }

        [Test]
        public void Plan_TargetTooFar_MovesWithoutAttacking()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(7, 0), moveRange: 1);
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(0, 0));
            BattleState state = StateWith(actor, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.HasMove, Is.True);
            Assert.That(plan.AttackAfterMove, Is.False);
        }

        [Test]
        public void Plan_NoOpponents_EndsTurn()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5));
            CombatUnit ally = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 4));
            BattleState state = StateWith(actor, ally);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.Target, Is.Null);
            Assert.That(plan.HasMove, Is.False);
            Assert.That(plan.AttackAfterMove, Is.False);
        }

        [Test]
        public void Plan_NoMovementBudget_EndsTurn()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), moveRange: 0);
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(0, 5));
            BattleState state = StateWith(actor, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.HasMove, Is.False);
            Assert.That(plan.AttackAfterMove, Is.False);
        }
    }
}
