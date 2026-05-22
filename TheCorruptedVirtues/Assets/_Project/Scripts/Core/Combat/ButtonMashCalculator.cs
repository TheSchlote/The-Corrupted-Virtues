namespace TheCorruptedVirtues.Combat
{
    // Pure C# grader for the button-mash QTE (M2 slice 2). The Unity meter
    // counts presses against a difficulty-scaled target and passes the [0, 1]
    // fill ratio (presses / target) here. Unlike the swing meter there is no
    // overshoot penalty — more presses only ever helps, capping at Divine once
    // the target is reached.
    //
    //   Fumble [0.00, 0.30) | Miss [0.30, 0.60) | Hit [0.60, 1.00) | Divine [1.00, +inf)
    public class ButtonMashCalculator
    {
        private const float DivineMin = 1.0f;
        private const float HitMin = 0.60f;
        private const float MissMin = 0.30f;

        // Presses needed to reach Divine. Rises with difficulty so the
        // risk/reward gradient (stronger move = harder QTE) holds for mashing
        // too. Within the controller's fixed mash window, a higher target is a
        // faster required cadence.
        public static int TargetPresses(QteDifficulty difficulty)
        {
            switch (difficulty)
            {
                case QteDifficulty.Hard:
                    return 10;
                case QteDifficulty.Brutal:
                    return 14;
                default:
                    return 6;
            }
        }

        // Grades a fill ratio (presses / target). Values above 1 cap at Divine.
        public ExecutionResult Evaluate(float fillRatio)
        {
            float clamped = fillRatio < 0.0f ? 0.0f : fillRatio;

            if (clamped >= DivineMin)
            {
                return ExecutionResult.Divine;
            }

            if (clamped >= HitMin)
            {
                return ExecutionResult.Hit;
            }

            if (clamped >= MissMin)
            {
                return ExecutionResult.Miss;
            }

            return ExecutionResult.Fumble;
        }
    }
}
