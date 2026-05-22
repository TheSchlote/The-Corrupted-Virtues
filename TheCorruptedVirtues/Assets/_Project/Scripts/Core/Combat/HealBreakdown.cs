namespace TheCorruptedVirtues.Combat
{
    // Output bundle for Support heal calculations, mirroring DamageBreakdown
    // so the HUD can preview heals the same way it previews damage.
    public sealed class HealBreakdown
    {
        public int FinalHeal { get; }
        public float ExecutionMultiplier { get; }
        public float BaseHeal { get; }

        public HealBreakdown(int finalHeal, float executionMultiplier, float baseHeal)
        {
            FinalHeal = finalHeal;
            ExecutionMultiplier = executionMultiplier;
            BaseHeal = baseHeal;
        }
    }
}
