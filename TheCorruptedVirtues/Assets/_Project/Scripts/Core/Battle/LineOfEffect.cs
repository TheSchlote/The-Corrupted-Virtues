using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Pure grid math for line attacks: a straight beam of 'length' tiles from
    // the attacker's front in a cardinal direction. The linear cousin of
    // AreaOfEffect — same "the tiles you see lit are exactly what gets hit"
    // guarantee and the same no-friendly-fire rule — differing only in the tile
    // set (a line, not a Chebyshev square).
    public static class LineOfEffect
    {
        // The cardinal step from 'origin' toward 'aim': the dominant axis, ties
        // broken toward X. Zero vector when origin == aim. Keeps beams axis-
        // aligned so the line is unambiguous from any aimed tile.
        public static GridCoord Direction(GridCoord origin, GridCoord aim)
        {
            int dx = aim.X - origin.X;
            int dy = aim.Y - origin.Y;
            int adx = dx < 0 ? -dx : dx;
            int ady = dy < 0 ? -dy : dy;
            if (adx == 0 && ady == 0)
            {
                return new GridCoord(0, 0);
            }

            if (adx >= ady)
            {
                return new GridCoord(dx < 0 ? -1 : 1, 0);
            }

            return new GridCoord(0, dy < 0 ? -1 : 1);
        }

        // The beam tiles for highlighting: 'length' steps out from the tile in
        // front of 'origin' along 'direction', clamped to the grid. Excludes the
        // attacker's own tile. A zero direction or non-positive length yields none.
        public static List<GridCoord> LineTiles(GridCoord origin, GridCoord direction, int length, GridBounds bounds)
        {
            List<GridCoord> tiles = new List<GridCoord>();
            if ((direction.X == 0 && direction.Y == 0) || length < 1)
            {
                return tiles;
            }

            for (int step = 1; step <= length; step++)
            {
                GridCoord coord = new GridCoord(origin.X + direction.X * step, origin.Y + direction.Y * step);
                if (bounds.Contains(coord))
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        // The living opponents a beam catches: any enemy of the attacker whose
        // footprint covers a beam tile. Mirrors AreaOfEffect.CollectTargets — no
        // friendly fire, each unit once. The beam pierces (hits everyone along
        // it); occlusion/blocking is a later refinement if wanted.
        public static List<CombatUnit> CollectTargets(
            GridCoord origin, GridCoord direction, int length, Faction attackerFaction, BattleState state)
        {
            List<CombatUnit> targets = new List<CombatUnit>();
            if ((direction.X == 0 && direction.Y == 0) || length < 1)
            {
                return targets;
            }

            IReadOnlyList<CombatUnit> units = state.Units;
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit unit = units[i];
                if (!unit.IsAlive || unit.Faction == attackerFaction)
                {
                    continue;
                }

                if (FootprintOnLine(unit, origin, direction, length))
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        // True if 'unit' sits on the beam of 'length' tiles from 'origin' toward
        // 'aim' (cardinal). Targeting validity uses this so the cursor only
        // validates enemies the beam would actually hit (preview == resolve).
        public static bool OnLine(GridCoord origin, GridCoord aim, int length, CombatUnit unit)
        {
            GridCoord direction = Direction(origin, aim);
            if (direction.X == 0 && direction.Y == 0)
            {
                return false;
            }

            return FootprintOnLine(unit, origin, direction, length);
        }

        private static bool FootprintOnLine(CombatUnit unit, GridCoord origin, GridCoord direction, int length)
        {
            for (int step = 1; step <= length; step++)
            {
                GridCoord coord = new GridCoord(origin.X + direction.X * step, origin.Y + direction.Y * step);
                if (unit.Footprint.Covers(unit.Coord, coord))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
