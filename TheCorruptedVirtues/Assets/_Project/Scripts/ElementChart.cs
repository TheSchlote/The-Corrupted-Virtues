namespace TheCorruptedVirtues.Combat
{
    // Elemental advantage chart with simplified relationships:
    // Light > Dark, Dark > Light
    // Water > Fire > Nature > Earth > Electricity > Water
    public static class ElementChart
    {
        private const float AdvantageMultiplier = 1.25f;
        private const float DisadvantageMultiplier = 0.8f;
        private const float NeutralMultiplier = 1.0f;

        // Can be tuned later (e.g. 0.9f) to make same-element hits less effective.
        private const float SameElementMultiplier = 1.0f;

        public static float GetMultiplier(ElementType attacker, ElementType defender)
        {
            if (attacker == defender)
                return SameElementMultiplier;

            if (IsAdvantage(attacker, defender))
                return AdvantageMultiplier;

            if (IsDisadvantage(attacker, defender))
                return DisadvantageMultiplier;

            return NeutralMultiplier;
        }

        private static bool IsAdvantage(ElementType attacker, ElementType defender)
        {
            switch (attacker)
            {
                case ElementType.Light:        return defender == ElementType.Dark;
                case ElementType.Dark:         return defender == ElementType.Light;

                case ElementType.Water:        return defender == ElementType.Fire;
                case ElementType.Fire:         return defender == ElementType.Nature;
                case ElementType.Nature:       return defender == ElementType.Earth;
                case ElementType.Earth:        return defender == ElementType.Electricity;
                case ElementType.Electricity:  return defender == ElementType.Water;

                default:                       return false;
            }
        }

        private static bool IsDisadvantage(ElementType attacker, ElementType defender)
        {
            // Disadvantage is simply the inverse of advantage.
            return IsAdvantage(defender, attacker);
        }
    }
}
