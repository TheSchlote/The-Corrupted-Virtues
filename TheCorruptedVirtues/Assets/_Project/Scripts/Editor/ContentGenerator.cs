using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Unity;

namespace TheCorruptedVirtues.EditorTools
{
    // One-shot migration / re-seed tool: stamps the code-defined EncounterLibrary
    // (and the maps its encounters reference) into authored SO assets under
    // Resources/Encounters, deduping shared abilities (by name) and unit
    // archetypes (by value). Re-runnable — it wipes and regenerates the folder,
    // so EncounterLibrary stays the source of truth until you choose to edit the
    // assets directly (the runtime EncounterCatalog prefers assets when present).
    public static class ContentGenerator
    {
        private const string Root = "Assets/_Project/Resources/Encounters";
        private const string AbilitiesDir = Root + "/Abilities";
        private const string UnitsDir = Root + "/Units";
        private const string MapsDir = Root + "/Maps";

        [MenuItem("Tools/TCV/Generate Content Assets")]
        public static void Generate()
        {
            // Wipe any prior generation so re-running is idempotent.
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }
            EnsureFolder(Root);
            EnsureFolder(AbilitiesDir);
            EnsureFolder(UnitsDir);
            EnsureFolder(MapsDir);

            var abilityByName = new Dictionary<string, AbilitySO>();
            var mapByName = new Dictionary<string, MapSO>();
            var archetypeBySignature = new Dictionary<string, UnitArchetypeSO>();
            int archetypeIndex = 0;

            IReadOnlyList<EncounterSpec> library = EncounterLibrary.All();
            for (int e = 0; e < library.Count; e++)
            {
                EncounterSpec encounter = library[e];
                MapSO mapAsset = GetOrCreateMap(encounter.Map, mapByName);

                var placements = new List<EncounterSO.Placement>(encounter.Units.Count);
                foreach (EncounterUnitSpec unit in encounter.Units)
                {
                    UnitArchetypeSO archetype = GetOrCreateArchetype(unit, abilityByName, archetypeBySignature, ref archetypeIndex);
                    placements.Add(new EncounterSO.Placement
                    {
                        unit = archetype,
                        faction = unit.Faction,
                        id = unit.Id,
                        coord = new Vector2Int(unit.Coord.X, unit.Coord.Y),
                    });
                }

                EncounterSO encounterAsset = ScriptableObject.CreateInstance<EncounterSO>();
                encounterAsset.Configure(encounter.Name, e, mapAsset, placements);
                AssetDatabase.CreateAsset(encounterAsset, $"{Root}/{Sanitize(encounter.Name)}.asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TCV] Generated {library.Count} encounters, {archetypeBySignature.Count} archetypes, " +
                      $"{abilityByName.Count} abilities, {mapByName.Count} maps under {Root}.");
        }

        private static MapSO GetOrCreateMap(BattleMapSpec map, Dictionary<string, MapSO> cache)
        {
            if (map == null)
            {
                return null;
            }
            if (cache.TryGetValue(map.Name, out MapSO existing))
            {
                return existing;
            }

            MapSO so = ScriptableObject.CreateInstance<MapSO>();
            so.Configure(map);
            AssetDatabase.CreateAsset(so, $"{MapsDir}/{Sanitize(map.Name)}.asset");
            cache[map.Name] = so;
            return so;
        }

        private static UnitArchetypeSO GetOrCreateArchetype(
            EncounterUnitSpec unit,
            Dictionary<string, AbilitySO> abilityCache,
            Dictionary<string, UnitArchetypeSO> archetypeCache,
            ref int index)
        {
            string signature = Signature(unit);
            if (archetypeCache.TryGetValue(signature, out UnitArchetypeSO existing))
            {
                return existing;
            }

            var abilityAssets = new List<AbilitySO>(unit.Abilities.Count);
            foreach (AbilitySpec ability in unit.Abilities)
            {
                abilityAssets.Add(GetOrCreateAbility(ability, abilityCache));
            }

            index++;
            string name = $"{unit.Element}{(unit.IsBoss ? " Boss" : " Unit")} {index}";
            UnitArchetypeSO so = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            so.Configure(name, unit, abilityAssets);
            AssetDatabase.CreateAsset(so, $"{UnitsDir}/{Sanitize(name)}.asset");
            archetypeCache[signature] = so;
            return so;
        }

        private static AbilitySO GetOrCreateAbility(AbilitySpec ability, Dictionary<string, AbilitySO> cache)
        {
            if (cache.TryGetValue(ability.Name, out AbilitySO existing))
            {
                return existing;
            }

            AbilitySO so = ScriptableObject.CreateInstance<AbilitySO>();
            so.Configure(ability);
            AssetDatabase.CreateAsset(so, $"{AbilitiesDir}/{Sanitize(ability.Name)}.asset");
            cache[ability.Name] = so;
            return so;
        }

        // A value key so two units with identical templates share one archetype
        // asset (e.g. the same player squad reused across every encounter).
        private static string Signature(EncounterUnitSpec u)
        {
            var sb = new StringBuilder();
            CombatStats s = u.Stats;
            sb.Append(u.Element).Append('|')
              .Append(s.MaxHP).Append(',').Append(s.MaxMP).Append(',').Append(s.Attack).Append(',')
              .Append(s.Defense).Append(',').Append(s.SpecialAttack).Append(',').Append(s.SpecialDefense).Append(',').Append(s.Speed).Append('|')
              .Append(u.Footprint.Width).Append('x').Append(u.Footprint.Height).Append('|')
              .Append(u.IsBoss).Append('|').Append(u.MoveRange).Append('|').Append(u.AiBehavior).Append('|');
            foreach (AbilitySpec a in u.Abilities)
            {
                sb.Append(a.Name).Append(',');
            }
            return sb.ToString();
        }

        // Keep only filename-safe characters (Windows + AssetDatabase). Parens
        // are allowed, so "Boss (Pillars)" stays readable.
        private static string Sanitize(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                bool ok = char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '(' || c == ')';
                sb.Append(ok ? c : '_');
            }
            return sb.ToString().Trim();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
