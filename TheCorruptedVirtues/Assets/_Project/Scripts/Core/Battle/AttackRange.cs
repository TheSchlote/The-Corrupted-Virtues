using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The tiles a selected attack could reach from the attacker's current
    // position — the faint "range" overlay shown the moment an ability is
    // selected, so the player can read its reach before aiming. Geometry only:
    // no roster, no live targets (the bright per-aim preview shows exactly what
    // a given cursor hits). Empty for Support (non-offensive) abilities.
    public static class AttackRange
    {
        private static readonly GridCoord[] Cardinals =
        {
            new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1),
        };

        public static List<GridCoord> Tiles(AbilitySpec ability, CombatUnit attacker, GridBounds bounds)
        {
            HashSet<GridCoord> tiles = new HashSet<GridCoord>();
            if (ability == null || attacker == null || ability.Kind == AbilityKind.Support)
            {
                return new List<GridCoord>();
            }

            if (ability.IsLine)
            {
                // A beam reaches LineLength tiles in whichever cardinal you aim,
                // so show all four arms — the reach is legible before aiming.
                foreach (GridCoord dir in Cardinals)
                {
                    tiles.UnionWith(LineOfEffect.LineTiles(attacker.Coord, dir, ability.LineLength, bounds));
                }
            }
            else if (ability.IsAreaOfEffect)
            {
                // Anywhere a burst aimed within range could reach.
                foreach (GridCoord aim in InRange(attacker, ability.Range, bounds))
                {
                    tiles.UnionWith(AreaOfEffect.BurstTiles(aim, ability.AoeRadius, bounds));
                }
            }
            else
            {
                tiles.UnionWith(InRange(attacker, ability.Range, bounds));
            }

            // Never paint the attacker's own footprint.
            foreach (GridCoord cell in attacker.Footprint.Cells(attacker.Coord))
            {
                tiles.Remove(cell);
            }

            return new List<GridCoord>(tiles);
        }

        // In-bounds tiles within Manhattan 'range' of any footprint cell (the
        // attacker's own cells excluded by the caller).
        private static IEnumerable<GridCoord> InRange(CombatUnit attacker, int range, GridBounds bounds)
        {
            if (range < 1)
            {
                range = 1;
            }

            HashSet<GridCoord> tiles = new HashSet<GridCoord>();
            foreach (GridCoord cell in attacker.Footprint.Cells(attacker.Coord))
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    for (int dx = -range; dx <= range; dx++)
                    {
                        int manhattan = (dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy);
                        if (manhattan == 0 || manhattan > range)
                        {
                            continue;
                        }

                        GridCoord c = new GridCoord(cell.X + dx, cell.Y + dy);
                        if (bounds.Contains(c))
                        {
                            tiles.Add(c);
                        }
                    }
                }
            }

            return tiles;
        }
    }
}
