namespace TheCorruptedVirtues.Combat
{
    // Who an ability can target. Decoupled from AbilityKind so targeting is its
    // own data axis: a Physical ability can target self, an ally, or an enemy,
    // independent of the damage/heal formula its Kind selects.
    public enum TargetingMode
    {
        Enemy, // an opposing unit (default for Physical/Special)
        Ally,  // a friendly unit, the caster included (default for Support)
        Self,  // only the caster
    }
}
