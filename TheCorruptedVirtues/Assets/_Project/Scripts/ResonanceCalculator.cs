namespace TheCorruptedVirtues.Combat
{
    // Pure C# evaluator for resonance timing results.
    public class ResonanceCalculator
    {
        private const float DivineMin = 0.80f;
        private const float DivineMax = 0.95f;
        private const float HitMin = 0.40f;
        private const float HitMax = 0.80f;
        private const float MissMin = 0.20f;
        private const float MissMax = 0.40f;

        // Returns the resonance result for a normalized value in [0, 1].
        public ResonanceResult Evaluate(float normalizedValue)
        {
            float clampedValue = Clamp01(normalizedValue);

            if (clampedValue >= DivineMin && clampedValue <= DivineMax)
            {
                return ResonanceResult.Divine;
            }

            if ((clampedValue >= HitMin && clampedValue < HitMax) || clampedValue > DivineMax)
            {
                return ResonanceResult.Hit;
            }

            if (clampedValue >= MissMin && clampedValue < MissMax)
            {
                return ResonanceResult.Miss;
            }

            return ResonanceResult.Fumble;
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
