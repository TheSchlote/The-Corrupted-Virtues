using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the enemy AI heuristic extracted into EnemyTurnPlanner: target the
    // nearest opponent, attack if already adjacent (focus-firing the weakest
    // when several are in reach), else approach and stop one tile short. Also
    // pins ability selection — strongest affordable offensive ability.
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

        // === Focus-fire + ability selection (M2 AI slice) ===

        [Test]
        public void Plan_MultipleAdjacent_FocusesWeakestAndCarriesAbility()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5));
            CombatUnit healthy = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(5, 4), hp: 100);
            CombatUnit wounded = BattleTestFactory.Unit(3, Faction.Player, new GridCoord(4, 5), hp: 100);
            wounded.Hp = 12; // both adjacent; the wounded one should be focused
            BattleState state = StateWith(actor, healthy, wounded);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, Bounds);

            Assert.That(plan.Target, Is.SameAs(wounded));
            Assert.That(plan.AttackAfterMove, Is.True);
            Assert.That(plan.HasMove, Is.False);
            Assert.That(plan.Ability, Is.Not.Null);
        }

        private static CombatUnit EnemyWithSpecial(int mp, out AbilitySpec special)
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), mp: mp, element: ElementType.Dark);
            special = new AbilitySpec("Dark Pulse", AbilityKind.Special, ElementType.Dark, power: 30, scaling: 1.2f,
                mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal);
            actor.Abilities.Add(special);
            return actor;
        }

        [Test]
        public void ChooseAbility_PicksHighestAffordableDamage()
        {
            CombatUnit actor = EnemyWithSpecial(mp: 20, out AbilitySpec special);
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(5, 4), element: ElementType.Dark);

            Assert.That(EnemyTurnPlanner.ChooseAbility(actor, target), Is.SameAs(special));
        }

        [Test]
        public void ChooseAbility_UnaffordableSpecial_FallsBackToBasic()
        {
            CombatUnit actor = EnemyWithSpecial(mp: 0, out AbilitySpec _); // can't pay the 10 MP
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(5, 4), element: ElementType.Dark);

            Assert.That(EnemyTurnPlanner.ChooseAbility(actor, target), Is.SameAs(actor.BasicAttack));
        }

        [Test]
        public void ChooseAbility_NeverPicksSupport()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), mp: 50, element: ElementType.Dark);
            // A wildly strong heal must never be selected as an attack.
            actor.Abilities.Add(new AbilitySpec("Mend", AbilityKind.Support, ElementType.Dark, power: 999, scaling: 9.9f,
                mpCost: 5, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal));
            CombatUnit target = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(5, 4), element: ElementType.Dark);

            AbilitySpec chosen = EnemyTurnPlanner.ChooseAbility(actor, target);

            Assert.That(chosen.Kind, Is.Not.EqualTo(AbilityKind.Support));
            Assert.That(chosen, Is.SameAs(actor.BasicAttack));
        }
    }
}
