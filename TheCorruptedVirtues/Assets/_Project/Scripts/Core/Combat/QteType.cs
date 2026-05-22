namespace TheCorruptedVirtues.Combat
{
    // The family of timed-input "Execution" challenges (Gladius-style QTEs).
    // An ability names which one it uses; the Unity layer maps each value to a
    // concrete IExecutionMeter, so nothing in the core depends on a widget.
    // SwingMeter is M1's original; ButtonMash is the M2 slice 2 addition.
    // Timed-press / matching variants come in a later slice.
    public enum QteType
    {
        SwingMeter,
        ButtonMash
    }
}
