namespace TheCorruptedVirtues.Combat
{
    // Pure C# evaluator for swing-meter timing results. The Divine window
    // narrows with QteDifficulty (M2 slice 2 risk/reward gradient: stronger
    // abilities are graded on a tighter band). Normal reproduces the slice-1
    // layout exactly so the basic attack is unchanged.
    //
    // Zone layout (Normal):
    //   Fumble [0.00, 0.20) | Miss [0.20, 0.40) | Hit [0.40, 0.85)
    //   Divine [0.85, 0.92] | overshoot Miss (0.92, 1.00]
    //
    // Harder settings only move the Divine edges inward; the Hit zone grows to
    // fill the gap below Divine and the overshoot-Miss zone grows above it.
    // The boundaries are exposed so the swing-meter UI can paint the exact
    // same zones the grader uses (they must never drift apart).
    public class ExecutionCalculator
    {
        // Lower bands are shared across all difficulties.
        public const float MissMin = 0.20f;
        public const float HitMin = 0.40f;

        private readonly float divineMin;
        private readonly float divineMax;

        public float DivineMin => divineMin;
        public float DivineMax => divineMax;

        public ExecutionCalculator() : this(QteDifficulty.Normal)
        {
        }

        public ExecutionCalculator(QteDifficulty difficulty)
        {
            switch (difficulty)
            {
                case QteDifficulty.Hard:
                    divineMin = 0.86f;
                    divineMax = 0.905f;
                    break;
                case QteDifficulty.Brutal:
                    divineMin = 0.875f;
                    divineMax = 0.90f;
                    break;
                default: // Normal — slice-1 tuning, unchanged.
                    divineMin = 0.85f;
                    divineMax = 0.92f;
                    break;
            }
        }

        // Returns the execution result for a normalized value in [0, 1].
        public ExecutionResult Evaluate(float normalizedValue)
        {
            float clampedValue = Clamp01(normalizedValue);

            if (clampedValue >= divineMin && clampedValue <= divineMax)
            {
                return ExecutionResult.Divine;
            }

            // Overshoot past Divine = Miss. This is what makes Divine a real
            // risk/reward call: you can't just hold late and drift into a
            // free Hit on the other side — overshoot whiffs entirely.
            if (clampedValue > divineMax)
            {
                return ExecutionResult.Miss;
            }

            if (clampedValue >= HitMin && clampedValue < divineMin)
            {
                return ExecutionResult.Hit;
            }

            if (clampedValue >= MissMin && clampedValue < HitMin)
            {
                return ExecutionResult.Miss;
            }

            return ExecutionResult.Fumble;
        }

        // Defensive clamp to keep values in the normalized range.
        private static float Clamp01(float value)
        {
            if (value < 0.0f)
            {
                return 0.0f;
            }

            if (value > 1.0f)
            {
                return 1.0f;
            }

            return value;
        }
    }
}
