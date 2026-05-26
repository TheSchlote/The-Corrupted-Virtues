using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // A reusable unit template — stats / element / abilities / footprint — minus
    // its battlefield placement (id / faction / coord come from the encounter).
    // Converts to an EncounterUnitSpec; the combat core never sees this asset.
    [CreateAssetMenu(fileName = "Unit", menuName = "TCV/Unit Archetype")]
    public sealed class UnitArchetypeSO : ScriptableObject
    {
        [SerializeField] private string displayName = "New Unit";
        [SerializeField] private ElementType element = ElementType.Light;

        [Header("Stats")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int maxMP = 20;
        [SerializeField] private int attack = 10;
        [SerializeField] private int defense = 10;
        [SerializeField] private int specialAttack = 10;
        [SerializeField] private int specialDefense = 10;
        [SerializeField] private int speed = 10;

        [Header("Battlefield")]
        [SerializeField] private int moveRange = 4;
        [SerializeField] private AiBehavior aiBehavior = AiBehavior.Aggressive;
        [SerializeField] private bool isBoss = false;
        [SerializeField] private int footprintWidth = 1;
        [SerializeField] private int footprintHeight = 1;

        [Header("Abilities (index 0 = free basic attack)")]
        [SerializeField] private List<AbilitySO> abilities = new List<AbilitySO>();

        public EncounterUnitSpec ToSpec(int id, Faction faction, GridCoord coord)
        {
            CombatStats stats = new CombatStats(maxHP, maxMP, attack, defense, specialAttack, specialDefense, speed);

            List<AbilitySpec> abilitySpecs = new List<AbilitySpec>(abilities.Count);
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] != null)
                {
                    abilitySpecs.Add(abilities[i].ToSpec());
                }
            }

            return new EncounterUnitSpec(
                id, faction, coord, stats, element, abilitySpecs,
                new GridFootprint(footprintWidth, footprintHeight),
                isBoss, moveRange, aiBehavior);
        }

        // Stamp this archetype from a pure spec + the asset references for its
        // abilities — used by the Editor content generator.
        public void Configure(string name, EncounterUnitSpec spec, IReadOnlyList<AbilitySO> abilityAssets)
        {
            displayName = name;
            element = spec.Element;
            maxHP = spec.Stats.MaxHP;
            maxMP = spec.Stats.MaxMP;
            attack = spec.Stats.Attack;
            defense = spec.Stats.Defense;
            specialAttack = spec.Stats.SpecialAttack;
            specialDefense = spec.Stats.SpecialDefense;
            speed = spec.Stats.Speed;
            moveRange = spec.MoveRange;
            aiBehavior = spec.AiBehavior;
            isBoss = spec.IsBoss;
            footprintWidth = spec.Footprint.Width;
            footprintHeight = spec.Footprint.Height;
            abilities = new List<AbilitySO>(abilityAssets);
        }
    }
}
