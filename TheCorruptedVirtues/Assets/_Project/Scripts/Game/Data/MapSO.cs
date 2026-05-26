using System;
using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Editor-authorable battlefield data. Per-tile data uses Vector2Int (the pure
    // GridCoord is an immutable struct Unity won't serialize); ToSpec() builds the
    // pure BattleMapSpec the combat core reads.
    [CreateAssetMenu(fileName = "Map", menuName = "TCV/Map")]
    public sealed class MapSO : ScriptableObject
    {
        [Serializable]
        public struct ElevationCell
        {
            public Vector2Int coord;
            public int level;
        }

        [SerializeField] private string displayName = "New Map";
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private List<ElevationCell> elevation = new List<ElevationCell>();
        [SerializeField] private List<Vector2Int> obstacles = new List<Vector2Int>();

        public BattleMapSpec ToSpec()
        {
            ElevationTile[] tiles = new ElevationTile[elevation.Count];
            for (int i = 0; i < elevation.Count; i++)
            {
                tiles[i] = new ElevationTile(new GridCoord(elevation[i].coord.x, elevation[i].coord.y), elevation[i].level);
            }

            GridCoord[] blocks = new GridCoord[obstacles.Count];
            for (int i = 0; i < obstacles.Count; i++)
            {
                blocks[i] = new GridCoord(obstacles[i].x, obstacles[i].y);
            }

            return new BattleMapSpec(displayName, width, height, tiles, blocks);
        }

        // Stamp this asset from a pure spec — used by the Editor content generator.
        public void Configure(BattleMapSpec spec)
        {
            displayName = spec.Name;
            width = spec.Width;
            height = spec.Height;

            elevation = new List<ElevationCell>(spec.Elevation.Count);
            for (int i = 0; i < spec.Elevation.Count; i++)
            {
                elevation.Add(new ElevationCell
                {
                    coord = new Vector2Int(spec.Elevation[i].Coord.X, spec.Elevation[i].Coord.Y),
                    level = spec.Elevation[i].Level,
                });
            }

            obstacles = new List<Vector2Int>(spec.Obstacles.Count);
            for (int i = 0; i < spec.Obstacles.Count; i++)
            {
                obstacles.Add(new Vector2Int(spec.Obstacles[i].X, spec.Obstacles[i].Y));
            }
        }
    }
}
