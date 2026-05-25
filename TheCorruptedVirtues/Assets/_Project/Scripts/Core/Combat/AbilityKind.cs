namespace TheCorruptedVirtues.Combat
{
    // Ability category — selects the damage/heal formula only (Physical and
    // Special pick the attack/defense stat pair; Support heals). Targeting now
    // lives on AbilitySpec.Targeting, so Kind no longer doubles as the target
    // selector.
    public enum AbilityKind
    {
        Physical,
        Special,
        Support
    }
}
