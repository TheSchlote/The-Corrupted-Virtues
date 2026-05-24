namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Cardinal facing for a unit. Grid axes: +Y = North, +X = East — matching
    // GridCoord and the view's mapping (grid X → world X, grid Y → world Z).
    public enum Facing
    {
        North,
        East,
        South,
        West,
    }
}
