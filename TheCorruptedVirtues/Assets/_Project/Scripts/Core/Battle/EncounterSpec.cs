using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Immutable data for one combatant in an encounter — everything needed to
    // spawn a fresh CombatUnit. Pure C#; the same shape a ScriptableObject would
    // carry later (data is deferred to SOs per the roadmap, but the seam is here).
    public sealed class EncounterUnitSpec
    {
        // Standard move range when a spec doesn't override it. Was MakeUnit's
        // constant, then EncounterSpec's; now a per-unit default so a fast scout
        // and a slow tank can differ in reach.
        public const int DefaultMoveRange = 4;

        public readonly int Id;
        public readonly Faction Faction;
        public readonly GridCoord Coord;
        public readonly CombatStats Stats;
        public readonly ElementType Element;
        public readonly IReadOnlyList<AbilitySpec> Abilities;
        public readonly GridFootprint Footprint;
        public readonly bool IsBoss;
        public readonly int MoveRange;
        public readonly AiBehavior AiBehavior;

        public EncounterUnitSpec(
            int id,
            Faction faction,
            GridCoord coord,
            CombatStats stats,
            ElementType element,
            IReadOnlyList<AbilitySpec> abilities,
            GridFootprint footprint = default,
            bool isBoss = false,
            int moveRange = DefaultMoveRange,
            AiBehavior aiBehavior = AiBehavior.Aggressive)
        {
            Id = id;
            Faction = faction;
            Coord = coord;
            Stats = stats;
            Element = element;
            Abilities = abilities;
            // default(GridFootprint) is (0,0); normalise to 1x1 for normal units.
            Footprint = footprint.Width < 1 || footprint.Height < 1 ? GridFootprint.Single : footprint;
            IsBoss = isBoss;
            MoveRange = moveRange < 1 ? DefaultMoveRange : moveRange;
            AiBehavior = aiBehavior;
        }
    }

    // A whole battle's roster as data. BuildRoster spawns fresh, fully-initialised
    // CombatUnits (the construction lifted out of CombatSliceOrchestrator.MakeUnit),
    // so a new encounter is "load data → build roster" rather than a hardcoded method.
    public sealed class EncounterSpec
    {
        public readonly string Name;
        public readonly IReadOnlyList<EncounterUnitSpec> Units;
        public readonly BattleMapSpec Map;

        public EncounterSpec(string name, IReadOnlyList<EncounterUnitSpec> units, BattleMapSpec map = null)
        {
            Name = name;
            Units = units;
            // A null map (e.g. roster-only unit tests) falls back to a flat 8x8
            // so the orchestrator always has bounds/terrain to apply.
            Map = map ?? new BattleMapSpec("Default", 8, 8);
        }

        public List<CombatUnit> BuildRoster()
        {
            List<CombatUnit> roster = new List<CombatUnit>(Units.Count);
            for (int i = 0; i < Units.Count; i++)
            {
                roster.Add(BuildUnit(Units[i]));
            }
            return roster;
        }

        private static CombatUnit BuildUnit(EncounterUnitSpec spec)
        {
            // Auto-facing starts pointed at the opposing side (players sit at
            // low X, enemies high), so opening shots are frontal until someone
            // maneuvers around a flank.
            Facing facing = spec.Faction == Faction.Player ? Facing.East : Facing.West;
            CombatUnit unit = new CombatUnit
            {
                Id = new UnitId(spec.Id),
                Faction = spec.Faction,
                Coord = spec.Coord,
                SpawnCoord = spec.Coord,
                Facing = facing,
                SpawnFacing = facing,
                Footprint = spec.Footprint,
                IsBoss = spec.IsBoss,
                Stats = spec.Stats,
                Element = spec.Element,
                // Fresh list per build so rebuilt rosters never alias one another.
                Abilities = new List<AbilitySpec>(spec.Abilities),
                SelectedAbilityIndex = 0,
                MoveRange = spec.MoveRange,
                AiBehavior = spec.AiBehavior
            };
            unit.Hp = unit.MaxHp;
            unit.Mp = unit.MaxMp;
            return unit;
        }
    }
}
