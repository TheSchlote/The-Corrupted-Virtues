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
    }
}
