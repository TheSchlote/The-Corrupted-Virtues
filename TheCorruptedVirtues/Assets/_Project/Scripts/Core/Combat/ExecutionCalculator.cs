namespace TheCorruptedVirtues.Combat
{
    // Pure C# evaluator for execution timing results.
    public class ExecutionCalculator
    {
        // Zone tuning after the M2 slice 1 playtest reported Divine was
        // still too easy at the tightened 1.0s cycle. Narrowed Divine from
        // 15% to 7% of the range (~70ms visible at 1.0s — reflex-level).
        // Hit picks up the freed range so the safe zone stays generous.
        private const float DivineMin = 0.85f;
        private const float DivineMax = 0.92f;
        private const float HitMin = 0.40f;
        private const float HitMax = 0.85f;
        private const float MissMin = 0.20f;
        private const float MissMax = 0.40f;

        // Returns the execution result for a normalized value in [0, 1].
        public ExecutionResult Evaluate(float normalizedValue)
        {
            float clampedValue = Clamp01(normalizedValue);

            if (clampedValue >= DivineMin && clampedValue <= DivineMax)
            {
                return ExecutionResult.Divine;
            }

            // Overshoot past Divine = Miss. This is what makes Divine a real
            // risk/reward call: you can't just hold late and drift into a
            // free Hit on the other side — overshoot whiffs entirely.
            if (clampedValue > DivineMax)
            {
                return ExecutionResult.Miss;
            }

            if (clampedValue >= HitMin && clampedValue < HitMax)
            {
                return ExecutionResult.Hit;
            }

            if (clampedValue >= MissMin && clampedValue < MissMax)
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
