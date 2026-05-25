using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Pure grid math for area attacks (M2 AoE slice). A burst is a Chebyshev
    // square: every tile within 'radius' of the centre (so radius 1 = a 3x3
    // block). One place for both the on-screen area preview (BurstTiles) and the
    // resolve-time target set (CollectTargets), so what the player sees lit up is
    // exactly what gets hit.
    public static class AreaOfEffect
    {
        // The burst tiles, clamped to the grid, for highlighting. Includes the
        // centre tile.
        public static List<GridCoord> BurstTiles(GridCoord center, int radius, GridBounds bounds)
        {
            List<GridCoord> tiles = new List<GridCoord>();
            if (radius < 0)
            {
                radius = 0;
            }

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    GridCoord coord = new GridCoord(center.X + dx, center.Y + dy);
                    if (bounds.Contains(coord))
                    {
                        tiles.Add(coord);
                    }
                }
            }

            return tiles;
        }

        // The living units an area attack hits: opponents of the attacker whose
        // footprint touches any tile within the burst. No friendly fire — damage
        // AoE never catches the caster's own side. Each unit appears once even if
        // several of its tiles fall inside the burst (e.g. a 2x2 boss).
        public static List<CombatUnit> CollectTargets(GridCoord center, int radius, Faction attackerFaction, BattleState state)
        {
            List<CombatUnit> targets = new List<CombatUnit>();
            IReadOnlyList<CombatUnit> units = state.Units;
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit unit = units[i];
                if (!unit.IsAlive || unit.Faction == attackerFaction)
                {
                    continue;
                }

                if (FootprintInBurst(unit, center, radius))
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        private static bool FootprintInBurst(CombatUnit unit, GridCoord center, int radius)
        {
            foreach (GridCoord cell in unit.Footprint.Cells(unit.Coord))
            {
                int dx = cell.X - center.X;
                if (dx < 0) dx = -dx;
                int dy = cell.Y - center.Y;
                if (dy < 0) dy = -dy;

                int chebyshev = dx > dy ? dx : dy;
                if (chebyshev <= radius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
