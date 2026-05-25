using System;
using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // One raised tile in a map's terrain: a coordinate and how many levels up
    // it sits (1 = one step, a x1.25 hit against a lower target).
    public readonly struct ElevationTile
    {
        public readonly GridCoord Coord;
        public readonly int Level;

        public ElevationTile(GridCoord coord, int level)
        {
            Coord = coord;
            Level = level;
        }
    }

    // Immutable data for one battlefield: dimensions, raised tiles, and
    // impassable obstacle tiles. Pure C#; the same shape a ScriptableObject map
    // asset would carry later (data is deferred to SOs per the roadmap, but the
    // seam is here, mirroring EncounterSpec). Build* materialises the grid-core
    // models the runtime reads.
    public sealed class BattleMapSpec
    {
        public readonly string Name;
        public readonly int Width;
        public readonly int Height;
        public readonly IReadOnlyList<ElevationTile> Elevation;
        public readonly IReadOnlyList<GridCoord> Obstacles;

        public BattleMapSpec(
            string name,
            int width,
            int height,
            IReadOnlyList<ElevationTile> elevation = null,
            IReadOnlyList<GridCoord> obstacles = null)
        {
            Name = name;
            Width = width;
            Height = height;
            Elevation = elevation ?? Array.Empty<ElevationTile>();
            Obstacles = obstacles ?? Array.Empty<GridCoord>();
        }

        public GridBounds Bounds => new GridBounds(Width, Height);

        public ElevationMap BuildElevationMap()
        {
            ElevationMap map = new ElevationMap();
            for (int i = 0; i < Elevation.Count; i++)
            {
                map.SetLevel(Elevation[i].Coord, Elevation[i].Level);
            }
            return map;
        }

        public ObstacleMap BuildObstacleMap()
        {
            ObstacleMap map = new ObstacleMap();
            for (int i = 0; i < Obstacles.Count; i++)
            {
                map.Add(Obstacles[i]);
            }
            return map;
        }
    }
}
