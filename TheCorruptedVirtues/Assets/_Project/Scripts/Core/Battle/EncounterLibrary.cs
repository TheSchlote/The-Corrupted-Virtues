using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The playable encounters as plain-C# data, replacing the orchestrator's
    // hardcoded BuildSquads / BuildGreatBeastEncounter. F1 cycles All() in order.
    // The two player units are identical across encounters, so they're defined
    // once (PlayerSquad). Stats/positions are first-pass and tunable; this is the
    // seam SO-backed encounter assets plug into later (roadmap: data → SOs).
    public static class EncounterLibrary
    {
        // Cycle order. Index 0 loads first — the Great Beast, matching the old
        // default; F1 advances through the list and wraps.
        public static IReadOnlyList<EncounterSpec> All()
        {
            return new[]
            {
                GreatBeast(),
                Squads(),
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
        private static EncounterSpec GreatBeast()
        {
            List<EncounterUnitSpec> units = PlayerSquad();

            CombatStats beastStats = new CombatStats(
                maxHP: 400, maxMP: 0, attack: 22, defense: 80,
                specialAttack: 10, specialDefense: 80, speed: 6);
            units.Add(new EncounterUnitSpec(3, Faction.Enemy, new GridCoord(5, 4), beastStats, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Slam", AbilityKind.Physical, ElementType.Dark, power: 14, scaling: 1.0f),
            }, footprint: new GridFootprint(2, 2), isGreatBeast: true));

            return new EncounterSpec("Great Beast", units);
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

            return new EncounterSpec("Squads (2v2)", units);
        }
    }
}
