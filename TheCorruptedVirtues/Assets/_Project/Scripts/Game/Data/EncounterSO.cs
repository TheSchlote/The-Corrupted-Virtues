using System;
using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Editor-authorable encounter: a battlefield (MapSO) plus a list of unit
    // placements (which archetype, where, which side, what id). Converts to the
    // pure EncounterSpec the orchestrator already consumes.
    [CreateAssetMenu(fileName = "Encounter", menuName = "TCV/Encounter")]
    public sealed class EncounterSO : ScriptableObject
    {
        [Serializable]
        public struct Placement
        {
            public UnitArchetypeSO unit;
            public Faction faction;
            public int id;
            public Vector2Int coord;
        }

        [SerializeField] private string displayName = "New Encounter";
        [Tooltip("Cycle order for F1 (lower loads first).")]
        [SerializeField] private int sortOrder = 0;
        [SerializeField] private MapSO map;
        [SerializeField] private List<Placement> placements = new List<Placement>();

        public int SortOrder => sortOrder;

        public EncounterSpec ToSpec()
        {
            List<EncounterUnitSpec> units = new List<EncounterUnitSpec>(placements.Count);
            for (int i = 0; i < placements.Count; i++)
            {
                Placement p = placements[i];
                if (p.unit == null)
                {
                    continue;
                }

                units.Add(p.unit.ToSpec(p.id, p.faction, new GridCoord(p.coord.x, p.coord.y)));
            }

            BattleMapSpec mapSpec = map != null ? map.ToSpec() : null;
            return new EncounterSpec(displayName, units, mapSpec);
        }

        // Stamp this asset from library data — used by the Editor content generator.
        public void Configure(string name, int order, MapSO mapAsset, List<Placement> places)
        {
            displayName = name;
            sortOrder = order;
            map = mapAsset;
            placements = places;
        }
    }
}
