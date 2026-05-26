using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins the encounter-as-data layer: EncounterSpec.BuildRoster construction
    // (lifted from the orchestrator's MakeUnit) and the EncounterLibrary content.
    public class EncounterSpecTests
    {
        private static EncounterUnitSpec PlayerSpec(int id, GridCoord coord)
        {
            return new EncounterUnitSpec(id, Faction.Player, coord,
                new CombatStats(100, 30, 20, 10, 20, 10, 12), ElementType.Light,
                new List<AbilitySpec> { new AbilitySpec("Basic", AbilityKind.Physical, ElementType.Light, 10, 1.0f) });
        }

        [Test]
        public void BuildRoster_BuildsOneUnitPerSpec()
        {
            var spec = new EncounterSpec("Test", new List<EncounterUnitSpec>
            {
                PlayerSpec(1, new GridCoord(1, 1)),
                PlayerSpec(2, new GridCoord(1, 2)),
            });

            List<CombatUnit> roster = spec.BuildRoster();

            Assert.That(roster.Count, Is.EqualTo(2));
            Assert.That(roster[0].Id.Value, Is.EqualTo(1));
            Assert.That(roster[1].Id.Value, Is.EqualTo(2));
        }

        [Test]
        public void BuildUnit_InitializesSpawnHpMpFacingAndMoveRange()
        {
            var spec = new EncounterSpec("Test", new List<EncounterUnitSpec> { PlayerSpec(1, new GridCoord(3, 4)) });

            CombatUnit unit = spec.BuildRoster()[0];

            Assert.That(unit.Coord, Is.EqualTo(new GridCoord(3, 4)));
            Assert.That(unit.SpawnCoord, Is.EqualTo(new GridCoord(3, 4)));
            Assert.That(unit.Facing, Is.EqualTo(Facing.East));       // player faces the enemy side
            Assert.That(unit.SpawnFacing, Is.EqualTo(Facing.East));
            Assert.That(unit.Hp, Is.EqualTo(unit.MaxHp));
            Assert.That(unit.Mp, Is.EqualTo(unit.MaxMp));
            Assert.That(unit.MoveRange, Is.EqualTo(4));              // per-unit default
            Assert.That(unit.AiBehavior, Is.EqualTo(AiBehavior.Aggressive));
            Assert.That(unit.Footprint.IsSingle, Is.True);           // default footprint
        }

        [Test]
        public void BuildUnit_AppliesPerUnitMoveRange()
        {
            var spec = new EncounterSpec("Test", new List<EncounterUnitSpec>
            {
                new EncounterUnitSpec(1, Faction.Enemy, new GridCoord(5, 5),
                    new CombatStats(100, 0, 20, 10, 20, 10, 8), ElementType.Dark,
                    new List<AbilitySpec> { new AbilitySpec("Bite", AbilityKind.Physical, ElementType.Dark, 10, 1.0f) },
                    moveRange: 6),
            });

            CombatUnit unit = spec.BuildRoster()[0];

            // A custom move range flows through instead of the global default.
            Assert.That(unit.MoveRange, Is.EqualTo(6));
        }

        [Test]
        public void EnemySpec_FacesWest()
        {
            var spec = new EncounterSpec("Test", new List<EncounterUnitSpec>
            {
                new EncounterUnitSpec(1, Faction.Enemy, new GridCoord(5, 5),
                    new CombatStats(100, 0, 20, 10, 20, 10, 8), ElementType.Dark,
                    new List<AbilitySpec> { new AbilitySpec("Bite", AbilityKind.Physical, ElementType.Dark, 10, 1.0f) }),
            });

            Assert.That(spec.BuildRoster()[0].Facing, Is.EqualTo(Facing.West));
        }

        [Test]
        public void BossSpec_KeepsFootprintAndFlag()
        {
            var spec = new EncounterSpec("Beast", new List<EncounterUnitSpec>
            {
                new EncounterUnitSpec(1, Faction.Enemy, new GridCoord(5, 4),
                    new CombatStats(400, 0, 22, 80, 10, 80, 6), ElementType.Dark,
                    new List<AbilitySpec> { new AbilitySpec("Slam", AbilityKind.Physical, ElementType.Dark, 14, 1.0f) },
                    footprint: new GridFootprint(2, 2), isBoss: true),
            });

            CombatUnit beast = spec.BuildRoster()[0];

            Assert.That(beast.Footprint.Width, Is.EqualTo(2));
            Assert.That(beast.Footprint.Height, Is.EqualTo(2));
            Assert.That(beast.IsBoss, Is.True);
        }

        [Test]
        public void BuildRoster_Twice_ProducesIndependentAbilityLists()
        {
            var spec = new EncounterSpec("Test", new List<EncounterUnitSpec> { PlayerSpec(1, new GridCoord(1, 1)) });

            CombatUnit a = spec.BuildRoster()[0];
            CombatUnit b = spec.BuildRoster()[0];

            // Rebuilding (e.g. an F1 encounter switch / combat reset) must not
            // alias ability lists between roster instances.
            Assert.That(a.Abilities, Is.Not.SameAs(b.Abilities));
        }
    }

    public class EncounterLibraryTests
    {
        [Test]
        public void All_HasAtLeastOneEncounter()
        {
            Assert.That(EncounterLibrary.All().Count, Is.GreaterThan(0));
        }

        [Test]
        public void FirstEncounter_IsBoss_WithA2x2Boss()
        {
            List<CombatUnit> roster = EncounterLibrary.All()[0].BuildRoster();

            CombatUnit beast = roster.Find(u => u.IsBoss);
            Assert.That(beast, Is.Not.Null);
            Assert.That(beast.Footprint.Width, Is.EqualTo(2));
            Assert.That(beast.Footprint.Height, Is.EqualTo(2));
            Assert.That(roster.Exists(u => u.Faction == Faction.Player), Is.True);
        }

        [Test]
        public void EveryEncounter_HasBothFactions_AndUniqueIds()
        {
            foreach (EncounterSpec spec in EncounterLibrary.All())
            {
                List<CombatUnit> roster = spec.BuildRoster();

                Assert.That(roster.Exists(u => u.Faction == Faction.Player), Is.True, $"{spec.Name} has no player");
                Assert.That(roster.Exists(u => u.Faction == Faction.Enemy), Is.True, $"{spec.Name} has no enemy");

                var ids = new HashSet<int>();
                foreach (CombatUnit u in roster)
                {
                    Assert.That(ids.Add(u.Id.Value), Is.True, $"{spec.Name} has duplicate unit id {u.Id.Value}");
                }
            }
        }
    }
}
