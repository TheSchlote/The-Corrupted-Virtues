using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;
using TheCorruptedVirtues.CombatSlice.Unity;

namespace TheCorruptedVirtues.Tests
{
    // Pins the ScriptableObject authoring seam: each *SO must Configure-from and
    // ToSpec-back-to the pure spec without losing data, and the runtime catalog
    // must surface the same encounters as the code library (assets are stamped
    // from it). The combat core only ever sees the pure specs these produce.
    public class SoConversionTests
    {
        [Test]
        public void AbilitySO_RoundTripsToSpec()
        {
            var original = new AbilitySpec("Test Bolt", AbilityKind.Special, ElementType.Fire,
                power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.ButtonMash,
                qteDifficulty: QteDifficulty.Hard, aoeRadius: 1);

            var so = ScriptableObject.CreateInstance<AbilitySO>();
            so.Configure(original);
            AbilitySpec result = so.ToSpec();

            Assert.That(result.Name, Is.EqualTo("Test Bolt"));
            Assert.That(result.Kind, Is.EqualTo(AbilityKind.Special));
            Assert.That(result.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(result.Power, Is.EqualTo(22));
            Assert.That(result.Scaling, Is.EqualTo(1.2f));
            Assert.That(result.MpCost, Is.EqualTo(10));
            Assert.That(result.QteType, Is.EqualTo(QteType.ButtonMash));
            Assert.That(result.QteDifficulty, Is.EqualTo(QteDifficulty.Hard));
            Assert.That(result.AoeRadius, Is.EqualTo(1));
            Assert.That(result.IsAreaOfEffect, Is.True);
        }

        [Test]
        public void MapSO_RoundTripsToSpec()
        {
            BattleMapSpec original = MapLibrary.RuinedHall();

            var so = ScriptableObject.CreateInstance<MapSO>();
            so.Configure(original);
            BattleMapSpec result = so.ToSpec();

            Assert.That(result.Name, Is.EqualTo(original.Name));
            Assert.That(result.Width, Is.EqualTo(original.Width));
            Assert.That(result.Height, Is.EqualTo(original.Height));
            Assert.That(result.Obstacles.Count, Is.EqualTo(original.Obstacles.Count));
            Assert.That(result.Elevation.Count, Is.EqualTo(original.Elevation.Count));

            var originalObstacles = new HashSet<GridCoord>(original.Obstacles);
            foreach (GridCoord c in result.Obstacles)
            {
                Assert.That(originalObstacles.Contains(c), Is.True, $"obstacle {c} lost in round trip");
            }

            // Elevation survives: a known raised tile keeps its level.
            Assert.That(result.BuildElevationMap().GetLevel(new GridCoord(6, 4)),
                Is.EqualTo(original.BuildElevationMap().GetLevel(new GridCoord(6, 4))));
        }

        [Test]
        public void UnitArchetypeSO_RoundTripsTemplate_PlacementFromArgs()
        {
            var ability = ScriptableObject.CreateInstance<AbilitySO>();
            ability.Configure(new AbilitySpec("Slash", AbilityKind.Physical, ElementType.Dark, 10, 1.0f));

            var template = new EncounterUnitSpec(
                7, Faction.Enemy, new GridCoord(0, 0),
                new CombatStats(120, 30, 18, 22, 15, 19, 11), ElementType.Dark,
                new List<AbilitySpec> { ability.ToSpec() },
                footprint: new GridFootprint(2, 2), isBoss: true, moveRange: 5);

            var so = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            so.Configure("Test Beast", template, new List<AbilitySO> { ability });

            // The template comes from the asset; id/faction/coord come from the call.
            EncounterUnitSpec result = so.ToSpec(3, Faction.Player, new GridCoord(4, 5));

            Assert.That(result.Id, Is.EqualTo(3));
            Assert.That(result.Faction, Is.EqualTo(Faction.Player));
            Assert.That(result.Coord, Is.EqualTo(new GridCoord(4, 5)));
            Assert.That(result.Element, Is.EqualTo(ElementType.Dark));
            Assert.That(result.Stats.MaxHP, Is.EqualTo(120));
            Assert.That(result.Stats.Speed, Is.EqualTo(11));
            Assert.That(result.Footprint.Width, Is.EqualTo(2));
            Assert.That(result.IsBoss, Is.True);
            Assert.That(result.MoveRange, Is.EqualTo(5));
            Assert.That(result.Abilities.Count, Is.EqualTo(1));
            Assert.That(result.Abilities[0].Name, Is.EqualTo("Slash"));
        }

        [Test]
        public void EncounterSO_RoundTripsToSpec()
        {
            var ability = ScriptableObject.CreateInstance<AbilitySO>();
            ability.Configure(new AbilitySpec("Basic", AbilityKind.Physical, ElementType.Light, 10, 1.0f));

            var unitAsset = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            unitAsset.Configure("U",
                new EncounterUnitSpec(1, Faction.Player, new GridCoord(0, 0),
                    new CombatStats(100, 10, 10, 10, 10, 10, 10), ElementType.Light,
                    new List<AbilitySpec> { ability.ToSpec() }),
                new List<AbilitySO> { ability });

            var mapAsset = ScriptableObject.CreateInstance<MapSO>();
            mapAsset.Configure(MapLibrary.Plateau());

            var so = ScriptableObject.CreateInstance<EncounterSO>();
            so.Configure("Test Fight", 3, mapAsset, new List<EncounterSO.Placement>
            {
                new EncounterSO.Placement { unit = unitAsset, faction = Faction.Player, id = 1, coord = new Vector2Int(1, 1) },
                new EncounterSO.Placement { unit = unitAsset, faction = Faction.Enemy, id = 2, coord = new Vector2Int(6, 6) },
            });

            EncounterSpec result = so.ToSpec();

            Assert.That(result.Name, Is.EqualTo("Test Fight"));
            Assert.That(so.SortOrder, Is.EqualTo(3));
            Assert.That(result.Units.Count, Is.EqualTo(2));
            Assert.That(result.Units[0].Coord, Is.EqualTo(new GridCoord(1, 1)));
            Assert.That(result.Units[1].Faction, Is.EqualTo(Faction.Enemy));
            Assert.That(result.Map, Is.Not.Null);
            Assert.That(result.Map.Name, Is.EqualTo("Plateau"));
        }

        [Test]
        public void EncounterCatalog_DeeplyMatchesLibrary()
        {
            IReadOnlyList<EncounterSpec> catalog = EncounterCatalog.Load();
            IReadOnlyList<EncounterSpec> library = EncounterLibrary.All();

            // The generated assets must reproduce the library exactly, field for
            // field. With this proven, the library's spawn/reachability/plays-to-
            // winner suites transitively validate the shipped asset content too.
            Assert.That(catalog.Count, Is.EqualTo(library.Count), "encounter count");
            for (int i = 0; i < library.Count; i++)
            {
                EncounterSpec c = catalog[i];
                EncounterSpec l = library[i];
                Assert.That(c.Name, Is.EqualTo(l.Name), $"encounter {i} name");
                Assert.That(c.Units.Count, Is.EqualTo(l.Units.Count), $"{l.Name} unit count");

                for (int j = 0; j < l.Units.Count; j++)
                {
                    EncounterUnitSpec cu = c.Units[j];
                    EncounterUnitSpec lu = l.Units[j];
                    string who = $"{l.Name} unit {j}";
                    Assert.That(cu.Id, Is.EqualTo(lu.Id), $"{who} id");
                    Assert.That(cu.Faction, Is.EqualTo(lu.Faction), $"{who} faction");
                    Assert.That(cu.Coord, Is.EqualTo(lu.Coord), $"{who} coord");
                    Assert.That(cu.Element, Is.EqualTo(lu.Element), $"{who} element");
                    Assert.That(cu.IsBoss, Is.EqualTo(lu.IsBoss), $"{who} isBoss");
                    Assert.That(cu.MoveRange, Is.EqualTo(lu.MoveRange), $"{who} moveRange");
                    Assert.That(cu.Footprint.Width, Is.EqualTo(lu.Footprint.Width), $"{who} footprint W");
                    Assert.That(cu.Footprint.Height, Is.EqualTo(lu.Footprint.Height), $"{who} footprint H");
                    Assert.That(cu.Stats.MaxHP, Is.EqualTo(lu.Stats.MaxHP), $"{who} HP");
                    Assert.That(cu.Stats.MaxMP, Is.EqualTo(lu.Stats.MaxMP), $"{who} MP");
                    Assert.That(cu.Stats.Attack, Is.EqualTo(lu.Stats.Attack), $"{who} ATK");
                    Assert.That(cu.Stats.Defense, Is.EqualTo(lu.Stats.Defense), $"{who} DEF");
                    Assert.That(cu.Stats.SpecialAttack, Is.EqualTo(lu.Stats.SpecialAttack), $"{who} SpATK");
                    Assert.That(cu.Stats.SpecialDefense, Is.EqualTo(lu.Stats.SpecialDefense), $"{who} SpDEF");
                    Assert.That(cu.Stats.Speed, Is.EqualTo(lu.Stats.Speed), $"{who} SPD");
                    Assert.That(cu.Abilities.Count, Is.EqualTo(lu.Abilities.Count), $"{who} ability count");
                    for (int k = 0; k < lu.Abilities.Count; k++)
                    {
                        Assert.That(cu.Abilities[k].Name, Is.EqualTo(lu.Abilities[k].Name), $"{who} ability {k} name");
                        Assert.That(cu.Abilities[k].Power, Is.EqualTo(lu.Abilities[k].Power), $"{who} ability {k} power");
                        Assert.That(cu.Abilities[k].MpCost, Is.EqualTo(lu.Abilities[k].MpCost), $"{who} ability {k} mp");
                        Assert.That(cu.Abilities[k].AoeRadius, Is.EqualTo(lu.Abilities[k].AoeRadius), $"{who} ability {k} aoe");
                    }
                }

                Assert.That(c.Map.Name, Is.EqualTo(l.Map.Name), $"{l.Name} map name");
                Assert.That(c.Map.Width, Is.EqualTo(l.Map.Width), $"{l.Name} map width");
                Assert.That(c.Map.Height, Is.EqualTo(l.Map.Height), $"{l.Name} map height");
                Assert.That(c.Map.Obstacles.Count, Is.EqualTo(l.Map.Obstacles.Count), $"{l.Name} obstacle count");
                Assert.That(c.Map.Elevation.Count, Is.EqualTo(l.Map.Elevation.Count), $"{l.Name} elevation count");

                var libraryObstacles = new HashSet<GridCoord>(l.Map.Obstacles);
                foreach (GridCoord ob in c.Map.Obstacles)
                {
                    Assert.That(libraryObstacles.Contains(ob), Is.True, $"{l.Name} obstacle {ob} mismatch");
                }
            }
        }
    }
}
