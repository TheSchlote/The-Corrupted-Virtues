namespace TheCorruptedVirtues.Combat
{
    // Pure C# grader for the timed-press QTE (M2 QTE-types slice). A marker
    // sweeps once across a [0, 1] track; the player presses Confirm once. The
    // controller passes the marker position at the press here, and we grade on
    // the distance from a fixed target centre — nail the centre for Divine, drift
    // out for Hit / Miss, miss the band entirely (or never press) for Fumble.
    //
    // Unlike the swing meter there is no overshoot side: the band is symmetric
    // about the centre, so early and late errors are punished the same. Harder
    // difficulties shrink the Divine and Hit half-widths so the risk/reward
    // gradient (stronger move = harder QTE) holds here too. The half-widths are
    // exposed so the UI paints the exact bands the grader uses.
    public class TimedPressCalculator
    {
        // Centre of the track. Fixed (not difficulty-dependent) so the painted
        // target sits in a stable, fair place; difficulty only narrows the bands.
        public const float TargetCenter = 0.5f;

        private readonly float divineHalf;
        private readonly float hitHalf;
        private readonly float missHalf;

        public float DivineHalf => divineHalf;
        public float HitHalf => hitHalf;
        public float MissHalf => missHalf;

        public TimedPressCalculator() : this(QteDifficulty.Normal)
        {
        }

        public TimedPressCalculator(QteDifficulty difficulty)
        {
            switch (difficulty)
            {
                case QteDifficulty.Hard:
                    divineHalf = 0.035f;
                    hitHalf = 0.16f;
                    missHalf = 0.30f;
                    break;
                case QteDifficulty.Brutal:
                    divineHalf = 0.025f;
                    hitHalf = 0.13f;
                    missHalf = 0.28f;
                    break;
                default: // Normal
                    divineHalf = 0.05f;
                    hitHalf = 0.20f;
                    missHalf = 0.34f;
                    break;
            }
        }

        // Grades the marker position at the moment of the press. A timeout with
        // no press should pass the marker's final position (far from centre),
        // which naturally grades Fumble.
        public ExecutionResult Evaluate(float pressValue)
        {
            float clamped = pressValue < 0.0f ? 0.0f : (pressValue > 1.0f ? 1.0f : pressValue);
            float distance = clamped - TargetCenter;
            if (distance < 0.0f)
            {
                distance = -distance;
            }

            if (distance <= divineHalf)
            {
                return ExecutionResult.Divine;
            }

            if (distance <= hitHalf)
            {
                return ExecutionResult.Hit;
            }

            if (distance <= missHalf)
            {
                return ExecutionResult.Miss;
            }

            return ExecutionResult.Fumble;
        }
    }
}
