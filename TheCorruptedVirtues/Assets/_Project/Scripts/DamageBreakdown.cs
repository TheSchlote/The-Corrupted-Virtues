namespace TheCorruptedVirtues.Combat
{
    // Output bundle for damage calculations and tuning visibility.
    public sealed class DamageBreakdown
    {
        public int FinalDamage { get; }
        public float ElementMultiplier { get; }
        public float ResonanceMultiplier { get; }
        public float MitigationFactor { get; }
        public float PreMitigationDamage { get; }

        public DamageBreakdown(
            int finalDamage,
            float elementMultiplier,
            float resonanceMultiplier,
            float mitigationFactor,
            float preMitigationDamage)
        {
            FinalDamage = finalDamage;
            ElementMultiplier = elementMultiplier;
            ResonanceMultiplier = resonanceMultiplier;
            MitigationFactor = mitigationFactor;
            PreMitigationDamage = preMitigationDamage;
        }
    }
}
