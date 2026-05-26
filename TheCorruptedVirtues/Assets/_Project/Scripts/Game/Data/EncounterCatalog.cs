using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Loads encounter content from authored SO assets (Resources/Encounters),
    // converting each to the pure EncounterSpec the orchestrator drives. Falls
    // back to the code-defined EncounterLibrary when no assets are present, so
    // the game runs whether or not content has been stamped into assets yet —
    // a non-destructive migration seam.
    public static class EncounterCatalog
    {
        public const string ResourcePath = "Encounters";

        public static IReadOnlyList<EncounterSpec> Load()
        {
            EncounterSO[] assets = Resources.LoadAll<EncounterSO>(ResourcePath);
            if (assets == null || assets.Length == 0)
            {
                return EncounterLibrary.All();
            }

            List<EncounterSO> ordered = new List<EncounterSO>(assets);
            // Explicit cycle order; asset name breaks ties so the order is stable.
            ordered.Sort((a, b) =>
            {
                int byOrder = a.SortOrder.CompareTo(b.SortOrder);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.name, b.name);
            });

            List<EncounterSpec> specs = new List<EncounterSpec>(ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                specs.Add(ordered[i].ToSpec());
            }
            return specs;
        }
    }
}
