using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The single home for a non-directional ability's shape -> tiles/targets,
    // dispatched by shape (burst or beam). One place so the live orchestrator's
    // hover preview, its resolve, and the headless BattleSimulator all agree on
    // both what lights up and who gets hit. Single-target abilities don't come
    // through here (they resolve against one faced unit).
    public static class MultiTargetSelection
    {
        // The tiles a shape lights up for the hover preview: a burst centred on
        // the aimed tile, or the beam from the attacker toward it.
        public static List<GridCoord> PreviewTiles(AbilitySpec ability, GridCoord attackerCoord, GridCoord aim, GridBounds bounds)
        {
            if (ability.IsAreaOfEffect)
            {
                return AreaOfEffect.BurstTiles(aim, ability.AoeRadius, bounds);
            }

            if (ability.IsLine)
            {
                GridCoord direction = LineOfEffect.Direction(attackerCoord, aim);
                return LineOfEffect.LineTiles(attackerCoord, direction, ability.LineLength, bounds);
            }

            return new List<GridCoord>();
        }

        // The living opponents a shape hits at resolve: the same tile set as the
        // preview, so what the player saw lit is exactly what takes damage.
        public static List<CombatUnit> Collect(AbilitySpec ability, CombatUnit attacker, GridCoord aim, BattleState state)
        {
            if (ability.IsAreaOfEffect)
            {
                return AreaOfEffect.CollectTargets(aim, ability.AoeRadius, attacker.Faction, state);
            }

            if (ability.IsLine)
            {
                GridCoord direction = LineOfEffect.Direction(attacker.Coord, aim);
                return LineOfEffect.CollectTargets(attacker.Coord, direction, ability.LineLength, attacker.Faction, state);
            }

            return new List<CombatUnit>();
        }
    }
}
