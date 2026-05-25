namespace TheCorruptedVirtues.CombatSlice.Core
{
    // Pure interpretation of a raw two-axis input as a single cardinal step, plus
    // the edge rule for when a fresh directional press should register. Extracted
    // from the matching QTE so the fiddly, once-buggy logic (diagonal collapse,
    // roll-through drops) is testable without the Unity input layer. GridCoord
    // doubles as the direction here — its X/Y are the step — matching how
    // GridPathfinderBfs already uses GridCoord as a delta.
    public static class CardinalInput
    {
        // Exactly one axis nonzero -> that cardinal; zero OR a diagonal (both
        // axes nonzero) -> (0,0), so an ambiguous diagonal is treated as "no
        // input" instead of being forced to one direction.
        public static GridCoord ToCardinal(int axisX, int axisY)
        {
            if (axisX != 0 && axisY == 0)
            {
                return new GridCoord(axisX > 0 ? 1 : -1, 0);
            }

            if (axisY != 0 && axisX == 0)
            {
                return new GridCoord(0, axisY > 0 ? 1 : -1);
            }

            return new GridCoord(0, 0);
        }

        // A fresh press registers when the clean cardinal changed from last frame
        // and is non-neutral. Edge-on-change (not on a release to neutral) so
        // rolling straight between directions registers each, and a held
        // direction doesn't repeat. Callers should track lastCardinal every
        // frame (including releases to (0,0)) so a re-press of the same direction
        // registers again.
        public static bool RegistersPress(GridCoord cardinal, GridCoord lastCardinal)
        {
            return cardinal != lastCardinal && (cardinal.X != 0 || cardinal.Y != 0);
        }
    }
}
