namespace TheCorruptedVirtues.Combat
{
    // Maps execution results to damage multipliers.
    public static class ExecutionModifiers
    {
        public static float GetDamageMultiplier(ExecutionResult result)
        {
            switch (result)
            {
                case ExecutionResult.Fumble:
                    return 0.5f;
                case ExecutionResult.Miss:
                    return 0.0f;
                case ExecutionResult.Hit:
                    return 1.0f;
                case ExecutionResult.Divine:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }
    }
}
