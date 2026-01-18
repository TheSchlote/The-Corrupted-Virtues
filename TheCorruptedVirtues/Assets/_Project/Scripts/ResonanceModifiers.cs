namespace TheCorruptedVirtues.Combat
{
    // Maps resonance results to damage multipliers.
    public static class ResonanceModifiers
    {
        public static float GetDamageMultiplier(ResonanceResult result)
        {
            switch (result)
            {
                case ResonanceResult.Fumble:
                    return 0.5f;
                case ResonanceResult.Miss:
                    return 0.0f;
                case ResonanceResult.Hit:
                    return 1.0f;
                case ResonanceResult.Divine:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }
    }
}
