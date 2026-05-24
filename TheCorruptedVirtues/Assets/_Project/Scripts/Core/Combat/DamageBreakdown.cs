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
        // Position-derived terms (high ground, later flanking). 1.0 = none.
        public SituationalModifiers Situational { get; }

        public DamageBreakdown(
            int finalDamage,
            float elementMultiplier,
            float executionMultiplier,
            float mitigationFactor,
            float preMitigationDamage,
            SituationalModifiers situational)
        {
            FinalDamage = finalDamage;
            ElementMultiplier = elementMultiplier;
            ExecutionMultiplier = executionMultiplier;
            MitigationFactor = mitigationFactor;
            PreMitigationDamage = preMitigationDamage;
            Situational = situational;
        }
    }
}
