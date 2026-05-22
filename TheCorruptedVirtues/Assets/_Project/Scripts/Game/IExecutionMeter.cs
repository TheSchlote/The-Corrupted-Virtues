using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The pluggable timed-input "Execution" challenge seam (the Gladius-style
    // QTE). Each concrete type owns its *whole* interaction — the swing meter
    // stops on a single confirm press; button mash counts presses until its
    // window closes — and the orchestrator drives them all uniformly through
    // Tick, so nothing downstream depends on a concrete meter. The grading
    // math stays in the pure core (ExecutionCalculator / ButtonMashCalculator).
    public interface IExecutionMeter
    {
        // True when this meter can run (e.g. its component is enabled). When
        // false, combat falls back to flat damage with no QTE.
        bool IsAvailable { get; }

        // True between Begin() and completion / Cancel().
        bool IsRunning { get; }

        // Player-facing label for the QTE variant (e.g. "Swing Meter",
        // "Button Mash"). Surfaced in the HUD so the player knows what kind of
        // timing check they're about to do — direct M1 playtest ask.
        string DisplayName { get; }

        // Start the challenge at the given difficulty: show it and begin the
        // timed input. The difficulty lets a meter scale its grading per
        // ability (the swing meter narrows its Divine window; button mash
        // raises its target press count).
        void Begin(QteDifficulty difficulty);

        // Advance one frame, consuming the confirm edge (true on the frame
        // Confirm was pressed). Returns true once the challenge has completed
        // and graded — result and multiplier are valid only then. Called every
        // frame by the orchestrator while a QTE is in progress.
        bool Tick(bool confirmPressed, out ExecutionResult result, out float multiplier);

        // Abort without grading (e.g. on combat reset).
        void Cancel();
    }
}
