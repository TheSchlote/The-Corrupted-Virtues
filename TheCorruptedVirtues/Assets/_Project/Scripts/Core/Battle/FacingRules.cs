using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Pure direction math for auto-facing (M2 facing slice): a unit faces the
    // way it last stepped, and turns to face its target when it attacks — both
    // reduce to "which cardinal points from A toward B".
    public static class FacingRules
    {
        // The cardinal pointing from 'from' toward 'to'. Dominant axis wins;
        // an exact diagonal tie favours the X axis. Moves and melee are
        // cardinal/adjacent so ties are rare, but the fallback keeps it total.
        public static Facing Toward(GridCoord from, GridCoord to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (System.Math.Abs(dx) >= System.Math.Abs(dy) && dx != 0)
            {
                return dx > 0 ? Facing.East : Facing.West;
            }

            return dy >= 0 ? Facing.North : Facing.South;
        }

        public static Facing Opposite(Facing facing)
        {
            switch (facing)
            {
                case Facing.North: return Facing.South;
                case Facing.South: return Facing.North;
                case Facing.East: return Facing.West;
                default: return Facing.East; // West
            }
        }
    }
}
