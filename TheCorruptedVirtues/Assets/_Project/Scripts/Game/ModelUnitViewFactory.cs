using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Spawns model-backed unit views, delegating to a fallback factory (the
    // primitives) for any unit that has no model yet — so models can be added
    // one at a time without breaking unmodelled units. The view-side routing
    // (which model for which unit) lives here only; gameplay just hands us
    // faction/element and never learns which prefab was chosen.
    public sealed class ModelUnitViewFactory : IUnitViewFactory
    {
        private const string ResourceRoot = "Units/";

        private readonly Transform parent;
        private readonly IUnitViewFactory fallback;
        // Caches hits and misses alike so a missing model isn't reloaded per spawn.
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        public ModelUnitViewFactory(Transform parent, IUnitViewFactory fallback)
        {
            this.parent = parent;
            this.fallback = fallback;
        }

        public IUnitView CreateUnit(Faction faction, ElementType element, GridFootprint footprint, bool isBoss)
        {
            GameObject prefab = ResolvePrefab(faction, element, isBoss);
            if (prefab == null)
            {
                return fallback.CreateUnit(faction, element, footprint, isBoss);
            }

            GameObject root = new GameObject(
                isBoss ? "BossUnitView" : (faction == Faction.Player ? "PlayerUnitView" : "EnemyUnitView"));
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            GameObject model = Object.Instantiate(prefab);
            model.transform.SetParent(root.transform, false);

            // Multi-tile units fill their footprint, matching the primitive path.
            int span = footprint.Width > footprint.Height ? footprint.Width : footprint.Height;
            if (span > 1)
            {
                model.transform.localScale *= span - 0.1f;
            }

            ModelUnitView view = root.AddComponent<ModelUnitView>();
            view.Init(model.transform, ElementPalette.For(element), isBoss);
            return view;
        }

        private GameObject ResolvePrefab(Faction faction, ElementType element, bool isBoss)
        {
            string key = ResolveKey(faction, element, isBoss);
            if (key == null)
            {
                return null;
            }

            if (!prefabCache.TryGetValue(key, out GameObject prefab))
            {
                prefab = Resources.Load<GameObject>(ResourceRoot + key);
                prefabCache[key] = prefab;
            }
            return prefab;
        }

        // The single seam that maps a unit to a model prefab (loaded from
        // Resources/Units/<key>). Returns null -> primitive fallback. Extend this
        // as more models arrive; it is the only place that grows for "many models".
        //
        // Player units route by ElementType to the matching Choir archangel
        // (see docs/LORE.md). The plain "Angel" key stays as a legacy fallback
        // in case a reskin variant isn't present for some element yet.
        private static string ResolveKey(Faction faction, ElementType element, bool isBoss)
        {
            if (faction != Faction.Player)
            {
                return null;
            }

            switch (element)
            {
                case ElementType.Light:       return "Uriel";
                case ElementType.Dark:        return "Umbriel";
                case ElementType.Fire:        return "Pyriel";
                case ElementType.Water:       return "Hydriel";
                case ElementType.Nature:      return "Arboriel";
                case ElementType.Earth:       return "Terriel";
                case ElementType.Electricity: return "Electriel";
                default:                      return "Angel";
            }
        }
    }
}
