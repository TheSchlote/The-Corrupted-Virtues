using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the shared target-validity rule used by both the preview
    // (RaiseSelection) and the commit (HandleConfirm), so they can't diverge as
    // they once did for the 2x2 boss.
    public class AbilityTargetingTests
    {
        private static readonly AbilitySpec Attack =
            new AbilitySpec("Strike", AbilityKind.Physical, ElementType.Light, 10, 1.0f);
        private static readonly AbilitySpec Heal =
            new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, 10, 1.0f);

        private static CombatUnit Unit(int id, Faction faction, GridCoord coord)
        {
            return BattleTestFactory.Unit(id, faction, coord);
        }

        [Test]
        public void Attack_AdjacentEnemy_IsValid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit enemy = Unit(2, Faction.Enemy, new GridCoord(2, 3));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, enemy), Is.True);
        }

        [Test]
        public void Attack_NonAdjacentEnemy_IsInvalid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit enemy = Unit(2, Faction.Enemy, new GridCoord(5, 5));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, enemy), Is.False);
        }

        [Test]
        public void Attack_Ally_IsInvalid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit ally = Unit(2, Faction.Player, new GridCoord(2, 3));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, ally), Is.False);
        }

        [Test]
        public void Attack_DeadOrNull_IsInvalid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit corpse = Unit(2, Faction.Enemy, new GridCoord(2, 3));
            corpse.Hp = 0;
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, corpse), Is.False);
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, null), Is.False);
        }

        [Test]
        public void Attack_AdjacentToNonAnchorCellOf2x2Boss_IsValid()
        {
            // The regression case: actor next to a boss cell that isn't the
            // anchor. Footprint-aware adjacency must accept it even though the
            // old Manhattan-to-anchor check (== distance 3 here) rejected it.
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(7, 5));
            CombatUnit boss = Unit(2, Faction.Enemy, new GridCoord(5, 4)); // anchor (5,4)
            boss.Footprint = new GridFootprint(2, 2);                      // covers (5,4)(6,4)(5,5)(6,5)

            Assert.That(GridMath.ManhattanDistance(actor.Coord, boss.Coord), Is.EqualTo(3));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Attack, boss), Is.True);
        }

        [Test]
        public void Support_Self_IsValid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Heal, actor), Is.True);
        }

        [Test]
        public void Support_AdjacentAlly_Valid_FarAlly_Invalid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit nearAlly = Unit(2, Faction.Player, new GridCoord(2, 3));
            CombatUnit farAlly = Unit(3, Faction.Player, new GridCoord(5, 5));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Heal, nearAlly), Is.True);
            Assert.That(AbilityTargeting.IsValidTarget(actor, Heal, farAlly), Is.False);
        }

        [Test]
        public void Support_Enemy_IsInvalid()
        {
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit enemy = Unit(2, Faction.Enemy, new GridCoord(2, 3));
            Assert.That(AbilityTargeting.IsValidTarget(actor, Heal, enemy), Is.False);
        }

        [Test]
        public void DefaultTargeting_DerivesFromKind()
        {
            // The decoupled axes default to the old Kind-derived rule.
            Assert.That(Attack.Targeting, Is.EqualTo(TargetingMode.Enemy));
            Assert.That(Attack.Range, Is.EqualTo(1));
            Assert.That(Heal.Targeting, Is.EqualTo(TargetingMode.Ally));
        }

        [Test]
        public void SelfTargeting_MatchesOnlyTheCaster()
        {
            AbilitySpec selfBuff = new AbilitySpec(
                "Guard", AbilityKind.Support, ElementType.Light, 0, 1.0f,
                mpCost: 0, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                targeting: TargetingMode.Self);
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit ally = Unit(2, Faction.Player, new GridCoord(2, 3));

            Assert.That(AbilityTargeting.IsValidTarget(actor, selfBuff, actor), Is.True);
            Assert.That(AbilityTargeting.IsValidTarget(actor, selfBuff, ally), Is.False);
        }

        [Test]
        public void RangedEnemyTargeting_ReachesBeyondAdjacency()
        {
            AbilitySpec ranged = new AbilitySpec(
                "Snipe", AbilityKind.Special, ElementType.Light, 10, 1.0f,
                mpCost: 0, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal,
                targeting: TargetingMode.Enemy, range: 3);
            CombatUnit actor = Unit(1, Faction.Player, new GridCoord(2, 2));
            CombatUnit inRange = Unit(2, Faction.Enemy, new GridCoord(2, 5));    // Manhattan 3
            CombatUnit outOfRange = Unit(3, Faction.Enemy, new GridCoord(2, 6)); // Manhattan 4

            Assert.That(AbilityTargeting.IsValidTarget(actor, ranged, inRange), Is.True);
            Assert.That(AbilityTargeting.IsValidTarget(actor, ranged, outOfRange), Is.False);
        }
    }
}
