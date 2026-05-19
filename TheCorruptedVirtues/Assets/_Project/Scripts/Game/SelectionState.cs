namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // What the cursor is currently hovering, from the logic's point of view.
    // Presenters map this to colour / hint text.
    public enum SelectionState
    {
        Neutral,
        MoveValid,
        AttackValid,
        Invalid
    }
}
