using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Shared builder for the pure-C# battle systems' tests. Keeps the unit
    // construction noise (stats block, ability list) out of the individual
    // test bodies so they read as behaviour, not setup.
    internal static class BattleTestFactory
    {
        public static CombatUnit Unit(
            int id,
            Faction faction,
            GridCoord coord,
            int speed = 10,
            int hp = 100,
            int mp = 0,
            int moveRange = 4,
            ElementType element = ElementType.Light)
        {
            CombatStats stats = new CombatStats(
                maxHP: hp, maxMP: mp,
                attack: 20, defense: 10,
                specialAttack: 20, specialDefense: 10,
                speed: speed);

            CombatUnit unit = new CombatUnit
            {
                Id = new UnitId(id),
                Faction = faction,
                Coord = coord,
                SpawnCoord = coord,
                Stats = stats,
                Element = element,
                Abilities = new List<AbilitySpec>
                {
                    new AbilitySpec("Basic", AbilityKind.Physical, element, power: 10, scaling: 1.0f),
                },
                SelectedAbilityIndex = 0,
                MoveRange = moveRange,
            };
            unit.Hp = hp;
            unit.Mp = mp;
            return unit;
        }
    }
}
