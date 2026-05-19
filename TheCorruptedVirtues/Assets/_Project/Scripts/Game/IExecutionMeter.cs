using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The pluggable timed-input "Execution" challenge seam (the Gladius-style
    // QTE). The swing meter is the first concrete type; button-mash /
    // timed-press / matching variants implement this same contract later, so
    // nothing downstream should depend on a concrete meter. The QTE grading
    // math stays in the pure core (ExecutionCalculator / ExecutionModifiers);
    // this interface is only the runtime lifecycle of presenting one.
    public interface IExecutionMeter
    {
        // True when this meter can run (e.g. its component is enabled). When
        // false, combat falls back to flat damage with no QTE.
        bool IsAvailable { get; }

        // True between Begin() and StopAndEvaluate()/Cancel().
        bool IsRunning { get; }

        // Start the challenge: show it and begin the timed input.
        void Begin();

        // Stop and grade the current input. Returns the discrete tier and
        // outputs the damage multiplier plus the raw [0,1] input value.
        ExecutionResult StopAndEvaluate(out float multiplier, out float normalizedValue);

        // Abort without grading (e.g. on combat reset).
        void Cancel();
    }
}
