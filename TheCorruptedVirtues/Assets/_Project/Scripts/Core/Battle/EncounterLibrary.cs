using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The playable encounters as plain-C# data, replacing the orchestrator's
    // hardcoded BuildSquads / BuildBossEncounter. F1 cycles All() in order.
    // The two player units are identical across encounters, so they're defined
    // once (PlayerSquad). Stats/positions are first-pass and tunable; this is the
    // seam SO-backed encounter assets plug into later (roadmap: data → SOs).
    public static class EncounterLibrary
    {
        // Cycle order. Index 0 loads first — the Boss, matching the old
        // default; F1 advances through the list and wraps.
        public static IReadOnlyList<EncounterSpec> All()
        {
            return new[]
            {
                Boss(),
                Squads(),
                PillarBoss(),
                HighlandSiege(),
                Narrows(),
                ElementalClash(),
            };
        }

        // The two player units both encounters share: a fast Light striker and a
        // sturdy Fire support. Rebuilt each call so encounters never alias lists.
        private static List<EncounterUnitSpec> PlayerSquad()
        {
            CombatStats lightFast = new CombatStats(
                maxHP: 90, maxMP: 20, attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70, speed: 14);
            CombatStats fireSturdy = new CombatStats(
                maxHP: 110, maxMP: 24, attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90, speed: 8);

            return new List<EncounterUnitSpec>
            {
                new EncounterUnitSpec(1, Faction.Player, new GridCoord(1, 1), lightFast, ElementType.Light, new List<AbilitySpec>
                {
                    new AbilitySpec("Radiant Cleave", AbilityKind.Physical, ElementType.Light, power: 10, scaling: 1.0f),
                    new AbilitySpec("Searing Lance", AbilityKind.Special, ElementType.Light, power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard),
                    new AbilitySpec("Flurry", AbilityKind.Physical, ElementType.Light, power: 7, scaling: 0.8f, mpCost: 8, qteType: QteType.ButtonMash, qteDifficulty: QteDifficulty.Normal),
                    new AbilitySpec("Lance of Dawn", AbilityKind.Special, ElementType.Light, power: 26, scaling: 1.3f, mpCost: 12, qteType: QteType.TimedPress, qteDifficulty: QteDifficulty.Hard),
                }),
                new EncounterUnitSpec(2, Faction.Player, new GridCoord(1, 3), fireSturdy, ElementType.Fire, new List<AbilitySpec>
                {
                    new AbilitySpec("Ember Strike", AbilityKind.Physical, ElementType.Fire, power: 10, scaling: 1.0f),
                    new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                    new AbilitySpec("Cinder Combo", AbilityKind.Physical, ElementType.Fire, power: 8, scaling: 0.7f, mpCost: 10, qteType: QteType.Matching, qteDifficulty: QteDifficulty.Normal),
                    new AbilitySpec("Flame Nova", AbilityKind.Special, ElementType.Fire, power: 18, scaling: 1.1f, mpCost: 16, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard, aoeRadius: 1),
                }),
            };
        }

        // The boss fight: the player squad vs one mobile 2x2 corrupted Virtue
        // with a deep Corruption pool (its HP) and no MP (basic attack only).
        private static EncounterSpec Boss()
        {
            List<EncounterUnitSpec> units = PlayerSquad();

            CombatStats beastStats = new CombatStats(
                maxHP: 400, maxMP: 0, attack: 22, defense: 80,
                specialAttack: 10, specialDefense: 80, speed: 6);
            units.Add(new EncounterUnitSpec(3, Faction.Enemy, new GridCoord(5, 4), beastStats, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Slam", AbilityKind.Physical, ElementType.Dark, power: 14, scaling: 1.0f),
            }, footprint: new GridFootprint(2, 2), isBoss: true));

            return new EncounterSpec("Boss", units, MapLibrary.Plateau());
        }

        // The 2v2 squad fight: four distinct elements so one battle surfaces
        // several matchups. Enemies carry an MP special so the AI's ability
        // choice is exercised.
        private static EncounterSpec Squads()
        {
            List<EncounterUnitSpec> units = PlayerSquad();

            CombatStats darkFast = new CombatStats(
                maxHP: 90, maxMP: 20, attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70, speed: 14);
            CombatStats waterSturdy = new CombatStats(
                maxHP: 110, maxMP: 24, attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90, speed: 8);

            units.Add(new EncounterUnitSpec(3, Faction.Enemy, new GridCoord(6, 6), darkFast, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Strike", AbilityKind.Physical, ElementType.Dark, power: 10, scaling: 1.0f),
                new AbilitySpec("Dark Pulse", AbilityKind.Special, ElementType.Dark, power: 20, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
            }));
            units.Add(new EncounterUnitSpec(4, Faction.Enemy, new GridCoord(6, 4), waterSturdy, ElementType.Water, new List<AbilitySpec>
            {
                new AbilitySpec("Tidal Slash", AbilityKind.Physical, ElementType.Water, power: 10, scaling: 1.0f),
                new AbilitySpec("Riptide", AbilityKind.Special, ElementType.Water, power: 18, scaling: 1.2f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
            }));

            return new EncounterSpec("Squads (2v2)", units, MapLibrary.RuinedHall());
        }

        // A second Boss fight, on the Pillared Hall — the 2x2 boss has to
        // weave through the pillars' 2-wide lanes to close on the squad.
        private static EncounterSpec PillarBoss()
        {
            List<EncounterUnitSpec> units = PlayerSquad();

            CombatStats beastStats = new CombatStats(
                maxHP: 400, maxMP: 0, attack: 22, defense: 80,
                specialAttack: 10, specialDefense: 80, speed: 6);
            units.Add(new EncounterUnitSpec(3, Faction.Enemy, new GridCoord(7, 4), beastStats, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Slam", AbilityKind.Physical, ElementType.Dark, power: 14, scaling: 1.0f),
            }, footprint: new GridFootprint(2, 2), isBoss: true));

            return new EncounterSpec("Boss (Pillars)", units, MapLibrary.PillaredHall());
        }

        // The squad assaults defenders dug in on the highland's high ground.
        private static EncounterSpec HighlandSiege()
        {
            List<EncounterUnitSpec> units = PlayerSquad();
            AddMinionPair(units, darkAt: new GridCoord(7, 3), waterAt: new GridCoord(6, 4));
            return new EncounterSpec("Highland Siege", units, MapLibrary.HighlandSiege());
        }

        // A funnel fight: the squad threads the serpentine corridor to reach the
        // minions holding the far end.
        private static EncounterSpec Narrows()
        {
            List<EncounterUnitSpec> units = PlayerSquad();
            AddMinionPair(units, darkAt: new GridCoord(9, 1), waterAt: new GridCoord(9, 2));
            return new EncounterSpec("The Narrows", units, MapLibrary.TheNarrows());
        }

        // A sandbox showcase: a mirror 7-vs-7 with one unit of every element on
        // each side, on Pillared Hall. Symmetric, so it's a fair brawl that puts
        // the whole matchup chart on the field at once — and the player drives a
        // full elemental roster. No boss (the boss has its own fights). Units are
        // built from per-element helpers, so each side shares one archetype per
        // element (the generator dedups them).
        private static EncounterSpec ElementalClash()
        {
            List<EncounterUnitSpec> units = new List<EncounterUnitSpec>(14)
            {
                // Players down the left column.
                LightUnit(1, Faction.Player, new GridCoord(1, 0)),
                FireUnit(2, Faction.Player, new GridCoord(1, 1)),
                NatureUnit(3, Faction.Player, new GridCoord(1, 2)),
                ElectricityUnit(4, Faction.Player, new GridCoord(1, 3)),
                WaterUnit(5, Faction.Player, new GridCoord(1, 4)),
                EarthUnit(6, Faction.Player, new GridCoord(1, 5)),
                DarkUnit(7, Faction.Player, new GridCoord(1, 6)),
                // Enemies down the right column — the same seven elements.
                LightUnit(8, Faction.Enemy, new GridCoord(8, 0)),
                FireUnit(9, Faction.Enemy, new GridCoord(8, 1)),
                NatureUnit(10, Faction.Enemy, new GridCoord(8, 2)),
                ElectricityUnit(11, Faction.Enemy, new GridCoord(8, 3)),
                WaterUnit(12, Faction.Enemy, new GridCoord(8, 4)),
                EarthUnit(13, Faction.Enemy, new GridCoord(8, 5)),
                DarkUnit(14, Faction.Enemy, new GridCoord(8, 6)),
            };

            return new EncounterSpec("Elemental Clash", units, MapLibrary.PillaredHall());
        }

        // Per-element unit templates for the Elemental Clash showcase. Faction /
        // id / coord are supplied by the caller so each can stand on either side.
        private static EncounterUnitSpec LightUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(90, 20, 16, 70, 14, 70, 14), ElementType.Light, new List<AbilitySpec>
                {
                    new AbilitySpec("Radiant Cleave", AbilityKind.Physical, ElementType.Light, power: 10, scaling: 1.0f),
                    new AbilitySpec("Searing Lance", AbilityKind.Special, ElementType.Light, power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard),
                });

        private static EncounterUnitSpec FireUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(110, 24, 14, 90, 16, 90, 8), ElementType.Fire, new List<AbilitySpec>
                {
                    new AbilitySpec("Ember Strike", AbilityKind.Physical, ElementType.Fire, power: 10, scaling: 1.0f),
                    new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                });

        private static EncounterUnitSpec NatureUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(95, 20, 15, 75, 15, 75, 12), ElementType.Nature, new List<AbilitySpec>
                {
                    new AbilitySpec("Vine Lash", AbilityKind.Physical, ElementType.Nature, power: 10, scaling: 1.0f),
                    new AbilitySpec("Thornburst", AbilityKind.Special, ElementType.Nature, power: 20, scaling: 1.2f, mpCost: 10, qteType: QteType.ButtonMash, qteDifficulty: QteDifficulty.Normal),
                });

        private static EncounterUnitSpec ElectricityUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(90, 20, 14, 70, 16, 70, 15), ElementType.Electricity, new List<AbilitySpec>
                {
                    new AbilitySpec("Spark", AbilityKind.Physical, ElementType.Electricity, power: 10, scaling: 1.0f),
                    new AbilitySpec("Chain Bolt", AbilityKind.Special, ElementType.Electricity, power: 18, scaling: 1.1f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal, aoeRadius: 1),
                });

        private static EncounterUnitSpec WaterUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(110, 24, 14, 90, 16, 90, 8), ElementType.Water, new List<AbilitySpec>
                {
                    new AbilitySpec("Tidal Slash", AbilityKind.Physical, ElementType.Water, power: 10, scaling: 1.0f),
                    new AbilitySpec("Riptide", AbilityKind.Special, ElementType.Water, power: 18, scaling: 1.2f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                });

        private static EncounterUnitSpec EarthUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(115, 20, 15, 95, 13, 95, 7), ElementType.Earth, new List<AbilitySpec>
                {
                    new AbilitySpec("Rock Throw", AbilityKind.Physical, ElementType.Earth, power: 10, scaling: 1.0f),
                    new AbilitySpec("Boulder", AbilityKind.Special, ElementType.Earth, power: 22, scaling: 1.3f, mpCost: 12, qteType: QteType.TimedPress, qteDifficulty: QteDifficulty.Hard),
                });

        private static EncounterUnitSpec DarkUnit(int id, Faction faction, GridCoord coord) =>
            new EncounterUnitSpec(id, faction, coord,
                new CombatStats(90, 20, 16, 70, 14, 70, 14), ElementType.Dark, new List<AbilitySpec>
                {
                    new AbilitySpec("Corruption Strike", AbilityKind.Physical, ElementType.Dark, power: 10, scaling: 1.0f),
                    new AbilitySpec("Dark Pulse", AbilityKind.Special, ElementType.Dark, power: 20, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                });

        // Two standard corrupted minions — a fast Dark striker and a sturdy Water
        // defender — at the given tiles. Ids 3/4 follow the player squad's 1/2.
        private static void AddMinionPair(List<EncounterUnitSpec> units, GridCoord darkAt, GridCoord waterAt)
        {
            CombatStats darkFast = new CombatStats(
                maxHP: 90, maxMP: 20, attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70, speed: 14);
            CombatStats waterSturdy = new CombatStats(
                maxHP: 110, maxMP: 24, attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90, speed: 8);

            units.Add(new EncounterUnitSpec(3, Faction.Enemy, darkAt, darkFast, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Strike", AbilityKind.Physical, ElementType.Dark, power: 10, scaling: 1.0f),
                new AbilitySpec("Dark Pulse", AbilityKind.Special, ElementType.Dark, power: 20, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
            }));
            units.Add(new EncounterUnitSpec(4, Faction.Enemy, waterAt, waterSturdy, ElementType.Water, new List<AbilitySpec>
            {
                new AbilitySpec("Tidal Slash", AbilityKind.Physical, ElementType.Water, power: 10, scaling: 1.0f),
                new AbilitySpec("Riptide", AbilityKind.Special, ElementType.Water, power: 18, scaling: 1.2f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
            }));
        }
    }
}
