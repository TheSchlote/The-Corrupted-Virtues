namespace TheCorruptedVirtues.Combat
{
    // Risk/reward gradient: stronger abilities are graded on a harder QTE.
    // Each meter interprets the level in its own units — the swing meter
    // narrows the Divine window, button mash raises the target press count.
    // Normal reproduces the M1/slice-1 swing tuning so the basic attack is
    // unchanged.
    public enum QteDifficulty
    {
        Normal,
        Hard,
        Brutal
    }
}
