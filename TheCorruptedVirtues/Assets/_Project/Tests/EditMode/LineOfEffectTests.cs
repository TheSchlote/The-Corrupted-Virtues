using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Line/beam slice: pins the pure line math — the direction picked, which
    // tiles light up, and which units a beam catches. Mirrors AreaOfEffectTests.
    public class LineOfEffectTests
    {
        private static readonly GridBounds Board = new GridBounds(8, 8);

        [Test]
        public void Direction_PicksDominantCardinalAxis()
        {
            Assert.That(LineOfEffect.Direction(new GridCoord(2, 2), new GridCoord(5, 3)), Is.EqualTo(new GridCoord(1, 0)));
            Assert.That(LineOfEffect.Direction(new GridCoord(2, 2), new GridCoord(2, 6)), Is.EqualTo(new GridCoord(0, 1)));
            Assert.That(LineOfEffect.Direction(new GridCoord(5, 5), new GridCoord(1, 5)), Is.EqualTo(new GridCoord(-1, 0)));
            Assert.That(LineOfEffect.Direction(new GridCoord(5, 5), new GridCoord(5, 1)), Is.EqualTo(new GridCoord(0, -1)));
        }

        [Test]
        public void Direction_SameTile_IsZero()
        {
            Assert.That(LineOfEffect.Direction(new GridCoord(3, 3), new GridCoord(3, 3)), Is.EqualTo(new GridCoord(0, 0)));
        }

        [Test]
        public void LineTiles_ExtendsFromInFront_ExcludesOrigin()
        {
            var tiles = LineOfEffect.LineTiles(new GridCoord(2, 2), new GridCoord(1, 0), 3, Board);
            Assert.That(tiles, Is.EquivalentTo(new[] { new GridCoord(3, 2), new GridCoord(4, 2), new GridCoord(5, 2) }));
            Assert.That(tiles, Has.No.Member(new GridCoord(2, 2))); // not the attacker's own tile
        }

        [Test]
        public void LineTiles_ClampsToGridEdge()
        {
            // From (6,0) heading +X on an 8-wide board: only (7,0) survives.
            var tiles = LineOfEffect.LineTiles(new GridCoord(6, 0), new GridCoord(1, 0), 3, Board);
            Assert.That(tiles, Is.EquivalentTo(new[] { new GridCoord(7, 0) }));
        }

        [Test]
        public void LineTiles_ZeroDirection_ReturnsNone()
        {
            var tiles = LineOfEffect.LineTiles(new GridCoord(4, 4), new GridCoord(0, 0), 3, Board);
            Assert.That(tiles, Is.Empty);
        }

        [Test]
        public void CollectTargets_HitsEnemiesOnLine_SkipsOffLineAndAllies()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit onLine = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(4, 2));   // on the +X beam
            CombatUnit offLine = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(4, 3));  // one row off
            CombatUnit ally = BattleTestFactory.Unit(4, Faction.Player, new GridCoord(3, 2));    // on line but friendly

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, onLine, offLine, ally });

            var targets = LineOfEffect.CollectTargets(new GridCoord(2, 2), new GridCoord(1, 0), 4, Faction.Player, state);

            Assert.That(targets, Has.Member(onLine));
            Assert.That(targets, Has.No.Member(offLine));
            Assert.That(targets, Has.No.Member(ally));
            Assert.That(targets, Has.No.Member(attacker));
        }

        [Test]
        public void CollectTargets_BeamPierces_HitsEveryEnemyAlongIt()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit near = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(1, 0));
            CombatUnit far = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(3, 0));

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, near, far });

            var targets = LineOfEffect.CollectTargets(new GridCoord(0, 0), new GridCoord(1, 0), 4, Faction.Player, state);

            Assert.That(targets.Count, Is.EqualTo(2));
            Assert.That(targets, Has.Member(near));
            Assert.That(targets, Has.Member(far));
        }

        [Test]
        public void CollectTargets_MultiTileUnit_CountedOnce_WhenAnyCellOnLine()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            // 2x2 boss anchored at (3,0) covers (3,0)(4,0)(3,1)(4,1); the +X beam
            // along row 0 clips (3,0) and (4,0) but the boss is one target.
            CombatUnit boss = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(3, 0));
            boss.Footprint = new GridFootprint(2, 2);

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, boss });

            var targets = LineOfEffect.CollectTargets(new GridCoord(0, 0), new GridCoord(1, 0), 6, Faction.Player, state);

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0], Is.SameAs(boss));
        }

        [Test]
        public void CollectTargets_ExcludesDeadEnemies()
        {
            CombatUnit attacker = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(0, 0));
            CombatUnit corpse = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(2, 0), hp: 10);
            corpse.Hp = 0;

            BattleState state = new BattleState();
            state.SetRoster(new[] { attacker, corpse });

            var targets = LineOfEffect.CollectTargets(new GridCoord(0, 0), new GridCoord(1, 0), 4, Faction.Player, state);

            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void AbilitySpec_IsLine_WhenLineLengthPositive_AndMutuallyExclusiveWithAoe()
        {
            AbilitySpec line = new AbilitySpec(
                "Beam", AbilityKind.Special, ElementType.Light, 10, 1f,
                mpCost: 3, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                aoeRadius: 0, targeting: null, range: 1, lineLength: 3);
            Assert.That(line.IsLine, Is.True);
            Assert.That(line.LineLength, Is.EqualTo(3));
            Assert.That(line.IsAreaOfEffect, Is.False);

            // Burst wins if both are set, so a spec is exactly one shape.
            AbilitySpec both = new AbilitySpec(
                "Both", AbilityKind.Special, ElementType.Light, 10, 1f,
                mpCost: 3, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                aoeRadius: 2, targeting: null, range: 1, lineLength: 3);
            Assert.That(both.IsAreaOfEffect, Is.True);
            Assert.That(both.IsLine, Is.False);
            Assert.That(both.LineLength, Is.EqualTo(0));
        }

        [Test]
        public void OnLine_TrueForCardinalEnemyInReach_FalseOffAxisOrTooFar()
        {
            GridCoord origin = new GridCoord(2, 2);
            CombatUnit onAxis = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 2));  // +X row 2
            CombatUnit offAxis = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(5, 3)); // one row off
            CombatUnit tooFar = BattleTestFactory.Unit(4, Faction.Enemy, new GridCoord(7, 2));  // 5 tiles out

            Assert.That(LineOfEffect.OnLine(origin, onAxis.Coord, 3, onAxis), Is.True);
            Assert.That(LineOfEffect.OnLine(origin, offAxis.Coord, 3, offAxis), Is.False);
            Assert.That(LineOfEffect.OnLine(origin, tooFar.Coord, 3, tooFar), Is.False);
        }

        [Test]
        public void IsValidTarget_LineAbility_AcceptsOnBeamEnemy_RejectsOffBeamAndAllies()
        {
            AbilitySpec beam = new AbilitySpec(
                "Beam", AbilityKind.Special, ElementType.Light, 10, 1f,
                mpCost: 3, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                aoeRadius: 0, targeting: null, range: 1, lineLength: 4);
            CombatUnit actor = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit onBeam = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(5, 2));
            CombatUnit offBeam = BattleTestFactory.Unit(3, Faction.Enemy, new GridCoord(4, 4));
            CombatUnit ally = BattleTestFactory.Unit(4, Faction.Player, new GridCoord(4, 2));

            Assert.That(AbilityTargeting.IsValidTarget(actor, beam, onBeam), Is.True);
            Assert.That(AbilityTargeting.IsValidTarget(actor, beam, offBeam), Is.False);
            Assert.That(AbilityTargeting.IsValidTarget(actor, beam, ally), Is.False);
        }
    }
}
