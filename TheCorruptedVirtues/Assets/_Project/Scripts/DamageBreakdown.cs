namespace TheCorruptedVirtues.Combat
{
    // Output bundle for damage calculations and tuning visibility.
    public sealed class DamageBreakdown
    {
        public int FinalDamage { get; }
        public float ElementMultiplier { get; }
        public float ExecutionMultiplier { get; }
        public float MitigationFactor { get; }
        public float PreMitigationDamage { get; }

        public DamageBreakdown(
            int finalDamage,
            float elementMultiplier,
            float executionMultiplier,
            float mitigationFactor,
            float preMitigationDamage)
        {
            FinalDamage = finalDamage;
            ElementMultiplier = elementMultiplier;
            ExecutionMultiplier = executionMultiplier;
            MitigationFactor = mitigationFactor;
            PreMitigationDamage = preMitigationDamage;
        }
    }
}
