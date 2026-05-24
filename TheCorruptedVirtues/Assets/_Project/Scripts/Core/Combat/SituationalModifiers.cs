namespace TheCorruptedVirtues.Combat
{
    // Position-derived damage multipliers — the "where you're standing" terms
    // that aren't intrinsic to stats, element, or the QTE. Each defaults to 1.0
    // (no effect). DamageCalculator multiplies final damage by Product, and
    // DamageBreakdown echoes the individual terms so the forecast can explain
    // each one line-by-line (the determinism pillar).
    //
    // HighGround ships in the M2 terrain slice. Flanking is reserved for the
    // facing slice — fill it there with zero churn to this signature; that is
    // the shared modifier seam designed for both up front.
    public readonly struct SituationalModifiers
    {
        public readonly float HighGround;
        public readonly float Flanking;

        public SituationalModifiers(float highGround, float flanking)
        {
            HighGround = highGround;
            Flanking = flanking;
        }

        public float Product => HighGround * Flanking;

        // The no-effect value. Use this instead of default(SituationalModifiers),
        // whose zeroed fields would multiply all damage down to nothing.
        public static SituationalModifiers None => new SituationalModifiers(1.0f, 1.0f);

        public static SituationalModifiers FromHighGround(float highGround)
        {
            return new SituationalModifiers(highGround, 1.0f);
        }
    }
}
