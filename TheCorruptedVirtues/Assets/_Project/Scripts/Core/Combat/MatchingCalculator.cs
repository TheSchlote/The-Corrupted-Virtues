namespace TheCorruptedVirtues.Combat
{
    // Pure C# grader for the matching QTE (M2 QTE-types slice). A short sequence
    // of directional prompts is shown; the player reproduces it in order. The
    // run ends at the first wrong input or when the window closes, and the
    // controller passes how many were matched (in order) out of the total here.
    //
    // Grades on the fraction matched, reusing the button-mash band layout so the
    // two count-based QTEs read the same:
    //   Fumble [0.00, 0.30) | Miss [0.30, 0.60) | Hit [0.60, 1.00) | Divine [1.00]
    // The whole sequence is the only path to Divine. Difficulty lengthens the
    // sequence (more to remember and input) rather than changing the bands, so
    // the risk/reward gradient holds.
    public class MatchingCalculator
    {
        private const float DivineMin = 1.0f;
        private const float HitMin = 0.60f;
        private const float MissMin = 0.30f;

        // How many prompts the player must reproduce. Rises with difficulty.
        public static int SequenceLength(QteDifficulty difficulty)
        {
            switch (difficulty)
            {
                case QteDifficulty.Hard:
                    return 4;
                case QteDifficulty.Brutal:
                    return 5;
                default:
                    return 3;
            }
        }

        // Grades matched-in-order count against the sequence length. A total of
        // zero (no sequence) grades Hit so a misconfigured ability can't whiff.
        public ExecutionResult Evaluate(int matched, int total)
        {
            if (total <= 0)
            {
                return ExecutionResult.Hit;
            }

            float clampedMatched = matched < 0 ? 0 : (matched > total ? total : matched);
            float ratio = clampedMatched / total;

            if (ratio >= DivineMin)
            {
                return ExecutionResult.Divine;
            }

            if (ratio >= HitMin)
            {
                return ExecutionResult.Hit;
            }

            if (ratio >= MissMin)
            {
                return ExecutionResult.Miss;
            }

            return ExecutionResult.Fumble;
        }
    }
}
