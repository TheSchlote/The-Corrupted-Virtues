using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the shape dispatcher: an ability's shape (burst / beam / single)
    // selects the right preview tiles and the right victims, so the live
    // orchestrator and the headless simulator never diverge on either.
    public class MultiTargetSelectionTests
    {
        private static readonly GridBounds Board = new GridBounds(8, 8);

        private static AbilitySpec Burst(int radius) => new AbilitySpec(
            "Burst", AbilityKind.Special, ElementType.Fire, 10, 1f,
            mpCost: 5, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal, aoeRadius: radius);

        private static AbilitySpec Beam(int length) => new AbilitySpec(
            "Beam", AbilityKind.Special, ElementType.Light, 10, 1f,
            mpCost: 5, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
            aoeRadius: 0, targeting: null, range: 1, lineLength: length);

        private static AbilitySpec Single() => new AbilitySpec(
            "Jab", AbilityKind.Physical, ElementType.Light, 10, 1f);

        [Test]
        public void PreviewTiles_DispatchesByShape()
        {
            GridCoord attacker = new GridCoord(2, 2);
            // Burst radius 1 around the aim = a 3x3 of 9; beam length 3 ahead = 3; single = none.
            Assert.That(MultiTargetSelection.PreviewTiles(Burst(1), attacker, new GridCoord(4, 4), Board).Count, Is.EqualTo(9));
            Assert.That(MultiTargetSelection.PreviewTiles(Beam(3), attacker, new GridCoord(5, 2), Board).Count, Is.EqualTo(3));
            Assert.That(MultiTargetSelection.PreviewTiles(Single(), attacker, new GridCoord(3, 2), Board), Is.Empty);
        }

        [Test]
        public void Collect_Beam_HitsOnlyTheBeamRow()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit onBeam = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(4, 2)); // on the +X beam
            CombatUnit offBeam = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(5, 3)); // off row 2

            BattleState state = new BattleState();
            state.SetRoster(new[] { actor, onBeam, offBeam });

            var targets = MultiTargetSelection.Collect(Beam(4), actor, new GridCoord(4, 2), state);
            Assert.That(targets, Has.Member(onBeam));
            Assert.That(targets, Has.No.Member(offBeam));
        }

        [Test]
        public void Collect_Burst_CatchesEveryoneInRadius()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit a = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(4, 2)); // chebyshev 1 of (5,3)
            CombatUnit b = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(5, 3)); // the centre

            BattleState state = new BattleState();
            state.SetRoster(new[] { actor, a, b });

            var targets = MultiTargetSelection.Collect(Burst(1), actor, new GridCoord(5, 3), state);
            Assert.That(targets, Has.Member(a));
            Assert.That(targets, Has.Member(b));
        }

        [Test]
        public void Collect_SingleTarget_ReturnsNothing()
        {
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit enemy = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(3, 2));

            BattleState state = new BattleState();
            state.SetRoster(new[] { actor, enemy });

            // A single-target ability isn't a multi-target shape — the dispatcher
            // yields nobody (it resolves against one faced unit elsewhere).
            Assert.That(MultiTargetSelection.Collect(Single(), actor, new GridCoord(3, 2), state), Is.Empty);
        }
    }
}
