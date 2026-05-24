using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // The multi-tile movement layer for the mobile Great Beast: footprint
    // adjacency, footprint-aware approach pathfinding, lift-and-place occupancy,
    // and the planner's multi-tile branch.
    public class FootprintAdjacencyTests
    {
        [Test]
        public void SingleVsSingle_MatchesManhattanOne()
        {
            var s = GridFootprint.Single;
            Assert.That(FootprintAdjacency.AreAdjacent(s, new GridCoord(2, 2), s, new GridCoord(2, 3)), Is.True);
            Assert.That(FootprintAdjacency.AreAdjacent(s, new GridCoord(2, 2), s, new GridCoord(3, 3)), Is.False); // diagonal
            Assert.That(FootprintAdjacency.AreAdjacent(s, new GridCoord(2, 2), s, new GridCoord(2, 2)), Is.False); // same cell
        }

        [Test]
        public void TwoByTwo_AdjacentOnAnySide_NotDiagonalOrOverlap()
        {
            var beast = new GridFootprint(2, 2);   // cells (2,2)(3,2)(2,3)(3,3)
            var anchor = new GridCoord(2, 2);
            var single = GridFootprint.Single;

            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(1, 2), beast, anchor), Is.True);  // left
            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(4, 3), beast, anchor), Is.True);  // right
            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(3, 4), beast, anchor), Is.True);  // above
            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(1, 1), beast, anchor), Is.False); // diagonal off corner
            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(3, 3), beast, anchor), Is.False); // inside (overlap)
            Assert.That(FootprintAdjacency.AreAdjacent(single, new GridCoord(0, 2), beast, anchor), Is.False); // two away
        }
    }

    public class FootprintApproachTests
    {
        private static readonly GridBounds Board = new GridBounds(8, 8);

        [Test]
        public void TwoByTwo_ApproachesUntilAdjacent_NeverOverlapsTarget()
        {
            var beast = new GridFootprint(2, 2);
            var target = new GridCoord(0, 0);
            var blocked = new GridOccupancy();
            blocked.Add(target); // the player sits on its cell

            var path = GridPathfinderBfs.FindFootprintApproach(new GridCoord(5, 5), beast, target, blocked, Board);

            Assert.That(path, Is.Not.Empty);
            GridCoord finalAnchor = path[path.Count - 1];
            Assert.That(FootprintAdjacency.AreAdjacent(beast, finalAnchor, GridFootprint.Single, target), Is.True);
            foreach (GridCoord anchor in path)
            {
                Assert.That(beast.Covers(anchor, target), Is.False);
            }
        }

        [Test]
        public void AlreadyAdjacent_ReturnsStartOnly()
        {
            var beast = new GridFootprint(2, 2);
            var target = new GridCoord(0, 0);
            var blocked = new GridOccupancy();
            blocked.Add(target);

            // Beast cell (1,0) is orthogonally adjacent to (0,0).
            var path = GridPathfinderBfs.FindFootprintApproach(new GridCoord(1, 0), beast, target, blocked, Board);

            Assert.That(path.Count, Is.EqualTo(1));
            Assert.That(path[0], Is.EqualTo(new GridCoord(1, 0)));
        }

        [Test]
        public void NoValidAdjacentPlacement_ReturnsEmpty()
        {
            // A 2x2 only fits at (0,0) on a 2x2 board, which overlaps the target.
            var tiny = new GridBounds(2, 2);
            var beast = new GridFootprint(2, 2);
            var target = new GridCoord(0, 0);
            var blocked = new GridOccupancy();
            blocked.Add(target);

            var path = GridPathfinderBfs.FindFootprintApproach(new GridCoord(0, 0), beast, target, blocked, tiny);

            Assert.That(path, Is.Empty);
        }
    }

    public class MultiTilePlannerTests
    {
        private static readonly GridBounds Board = new GridBounds(8, 8);

        private static BattleState StateWith(params CombatUnit[] units)
        {
            BattleState state = new BattleState();
            state.SetRoster(units);
            return state;
        }

        [Test]
        public void BuildOccupancyExcluding_OmitsActorKeepsOthers()
        {
            CombatUnit beast = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(2, 2));
            beast.Footprint = new GridFootprint(2, 2);
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(6, 6));
            BattleState state = StateWith(beast, foe);

            GridOccupancy occ = state.BuildOccupancyExcluding(beast);

            Assert.That(occ.IsOccupied(new GridCoord(2, 2)), Is.False); // beast's own cells omitted
            Assert.That(occ.IsOccupied(new GridCoord(3, 3)), Is.False);
            Assert.That(occ.IsOccupied(new GridCoord(6, 6)), Is.True);  // other unit kept
        }

        [Test]
        public void Plan_MobileBeast_ApproachesAndStopsAdjacent()
        {
            CombatUnit beast = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(5, 5), moveRange: 12);
            beast.Footprint = new GridFootprint(2, 2);
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(0, 0));
            BattleState state = StateWith(beast, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(beast, state, Board);

            Assert.That(plan.Target, Is.SameAs(foe));
            Assert.That(plan.HasMove, Is.True);
            GridCoord finalAnchor = plan.MovePath[plan.MovePath.Count - 1];
            Assert.That(FootprintAdjacency.AreAdjacent(beast.Footprint, finalAnchor, foe.Footprint, foe.Coord), Is.True);
            Assert.That(plan.AttackAfterMove, Is.True);
        }

        [Test]
        public void Plan_MobileBeastAlreadyAdjacent_AttacksInPlace()
        {
            CombatUnit beast = BattleTestFactory.Unit(1, Faction.Enemy, new GridCoord(2, 2));
            beast.Footprint = new GridFootprint(2, 2); // cells (2,2)-(3,3)
            CombatUnit foe = BattleTestFactory.Unit(2, Faction.Player, new GridCoord(4, 2)); // adjacent to (3,2)
            BattleState state = StateWith(beast, foe);

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(beast, state, Board);

            Assert.That(plan.HasMove, Is.False);
            Assert.That(plan.AttackAfterMove, Is.True);
        }
    }
}
