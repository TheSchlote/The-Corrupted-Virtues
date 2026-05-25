namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Per-unit AI policy, dispatched by EnemyTurnPlanner so enemies-as-data can
    // carry behaviour, not just stats. Only Aggressive ships today (the shipped
    // routine); the enum reserves the seam for defensive / support / skirmisher
    // archetypes added later without touching call sites.
    public enum AiBehavior
    {
        Aggressive, // focus-fire weakest adjacent, else approach nearest, else end
    }
}
