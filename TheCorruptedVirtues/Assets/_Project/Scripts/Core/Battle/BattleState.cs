using System.Collections.Generic;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Owns the unit roster and the spatial occupancy derived from it, and
    // answers the spatial / liveness / win questions the combat loop asks:
    // "who is at this tile", "find unit by id", "has a side won". Pure C# —
    // no UnityEngine, no events. The Unity layer mutates units, asks this what
    // changed, and announces results through CombatEvents.
    //
    // Lifted out of CombatSliceOrchestrator (was the allUnits list + occupancy
    // field + FindUnitById / GetUnitAt / UpdateOccupancy / CheckWinCondition).
    public sealed class BattleState
    {
        private readonly List<CombatUnit> units = new List<CombatUnit>();
        private readonly GridOccupancy occupancy = new GridOccupancy();

        public IReadOnlyList<CombatUnit> Units => units;

        // Living-unit occupancy, rebuilt from unit coords. Pathfinding and
        // reach checks read this; it is only as current as the last
        // RebuildOccupancy call (the orchestrator rebuilds after each step).
        public GridOccupancy Occupancy => occupancy;

        public void SetRoster(IEnumerable<CombatUnit> roster)
        {
            units.Clear();
            units.AddRange(roster);
            RebuildOccupancy();
        }

        public CombatUnit FindById(UnitId id)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Id.Value == id.Value)
                {
                    return units[i];
                }
            }
            return null;
        }

        public CombatUnit GetLivingUnitAt(GridCoord coord)
        {
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit u = units[i];
                if (u.IsAlive && u.Footprint.Covers(u.Coord, coord))
                {
                    return u;
                }
            }
            return null;
        }

        public void RebuildOccupancy()
        {
            occupancy.Clear();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive)
                {
                    occupancy.AddFootprint(units[i].Footprint, units[i].Coord);
                }
            }
        }

        // Occupancy of every OTHER living unit's footprint. Used for
        // lift-and-place pathfinding so a multi-tile unit doesn't collide with
        // the cells it is currently standing on.
        public GridOccupancy BuildOccupancyExcluding(CombatUnit actor)
        {
            GridOccupancy result = new GridOccupancy();
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit u = units[i];
                if (u.IsAlive && !ReferenceEquals(u, actor))
                {
                    result.AddFootprint(u.Footprint, u.Coord);
                }
            }
            return result;
        }

        public bool AnyAlive(Faction faction)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive && units[i].Faction == faction)
                {
                    return true;
                }
            }
            return false;
        }

        // Combat is decided when at most one faction has living units. Mirrors
        // the original CheckWinCondition: the winner is whichever side still
        // stands; if both emptied at once, fall back to tieBreakWinner (the
        // last attacker) — defensive, can't happen under current rules.
        // Returns false (not decided) while both sides have living units.
        public bool TryGetWinner(Faction tieBreakWinner, out Faction winner)
        {
            bool anyPlayer = AnyAlive(Faction.Player);
            bool anyEnemy = AnyAlive(Faction.Enemy);

            if (anyPlayer && anyEnemy)
            {
                winner = tieBreakWinner;
                return false;
            }

            if (anyPlayer) winner = Faction.Player;
            else if (anyEnemy) winner = Faction.Enemy;
            else winner = tieBreakWinner;
            return true;
        }
    }
}
