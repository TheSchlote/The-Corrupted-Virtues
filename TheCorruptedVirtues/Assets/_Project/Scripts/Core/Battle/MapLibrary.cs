using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Handcrafted battlefields as plain-C# data — the map counterpart to
    // EncounterLibrary. Each EncounterSpec references one of these so terrain
    // varies per fight instead of one shared hardcoded grid. Pure C#; the seam
    // an SO-backed map asset plugs into later (roadmap: data -> SOs).
    public static class MapLibrary
    {
        // The original 8x8 with a 2x2 high-ground plateau in the contested
        // midfield, preserved verbatim from the M2 terrain slice (was hardcoded
        // in CombatSliceOrchestrator.BuildTerrain). Both squads start
        // equidistant from the rise, so taking it is a real opening choice.
        public static BattleMapSpec Plateau()
        {
            return new BattleMapSpec("Plateau", 8, 8,
                elevation: new[]
                {
                    new ElevationTile(new GridCoord(3, 3), 1),
                    new ElevationTile(new GridCoord(4, 3), 1),
                    new ElevationTile(new GridCoord(3, 4), 1),
                    new ElevationTile(new GridCoord(4, 4), 1),
                });
        }

        // A wider 10x8 arena split by a central wall (x=4) with two gaps at
        // y=3 and y=5 — the only lanes between the sides, so engaging means
        // committing to a chokepoint. The enemy side holds a small rise the
        // attacker has to climb to. Demonstrates obstacles + variable size.
        public static BattleMapSpec RuinedHall()
        {
            return new BattleMapSpec("Ruined Hall", 10, 8,
                elevation: new[]
                {
                    new ElevationTile(new GridCoord(6, 4), 1),
                    new ElevationTile(new GridCoord(7, 4), 1),
                },
                obstacles: new[]
                {
                    new GridCoord(4, 0), new GridCoord(4, 1), new GridCoord(4, 2),
                    new GridCoord(4, 4),
                    new GridCoord(4, 6), new GridCoord(4, 7),
                });
        }

        // A 10x10 open arena broken by two 2x2 pillars, leaving 2-wide lanes so
        // the 2x2 Great Beast can navigate them (a 1-wide gap would trap it).
        // No elevation by design: a multi-tile unit can't straddle an elevation
        // edge, so raising tiles in its lanes would wall it off.
        public static BattleMapSpec PillaredHall()
        {
            return new BattleMapSpec("Pillared Hall", 10, 10,
                obstacles: new[]
                {
                    new GridCoord(4, 1), new GridCoord(5, 1),
                    new GridCoord(4, 2), new GridCoord(5, 2),
                    new GridCoord(4, 7), new GridCoord(5, 7),
                    new GridCoord(4, 8), new GridCoord(5, 8),
                });
        }

        // A 9x8 map with an asymmetric raised plateau on the enemy (right) flank,
        // topped by a level-2 peak — the attacker must climb to contest the
        // defenders' high ground. A 1x1 squad fight, so the multi-level terrain
        // is freely traversable.
        public static BattleMapSpec HighlandSiege()
        {
            return new BattleMapSpec("Highland Siege", 9, 8,
                elevation: new[]
                {
                    // Right plateau (level 1), wrapping a level-2 peak at its centre.
                    new ElevationTile(new GridCoord(6, 2), 1), new ElevationTile(new GridCoord(7, 2), 1), new ElevationTile(new GridCoord(8, 2), 1),
                    new ElevationTile(new GridCoord(6, 3), 1), new ElevationTile(new GridCoord(8, 3), 1),
                    new ElevationTile(new GridCoord(6, 4), 1), new ElevationTile(new GridCoord(8, 4), 1),
                    new ElevationTile(new GridCoord(6, 5), 1), new ElevationTile(new GridCoord(7, 5), 1), new ElevationTile(new GridCoord(8, 5), 1),
                    new ElevationTile(new GridCoord(7, 3), 2), new ElevationTile(new GridCoord(7, 4), 2),
                });
        }

        // An 11x7 corridor map: two offset walls force a serpentine, single-file
        // approach (up through the first gap, back down through the second), so
        // move range and chokepoint control matter more than raw positioning.
        public static BattleMapSpec TheNarrows()
        {
            return new BattleMapSpec("The Narrows", 11, 7,
                obstacles: new[]
                {
                    // Wall at x=4 sealing the lower rows; gap at the top (y=5,6).
                    new GridCoord(4, 0), new GridCoord(4, 1), new GridCoord(4, 2), new GridCoord(4, 3), new GridCoord(4, 4),
                    // Wall at x=7 sealing the upper rows; gap at the bottom (y=0,1).
                    new GridCoord(7, 2), new GridCoord(7, 3), new GridCoord(7, 4), new GridCoord(7, 5), new GridCoord(7, 6),
                });
        }
    }
}
