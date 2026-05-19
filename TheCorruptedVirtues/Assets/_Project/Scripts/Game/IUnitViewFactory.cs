namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Creates unit visuals. Swapping this implementation (primitives →
    // models) is the single-place change the asset-agnostic architecture
    // is built around.
    public interface IUnitViewFactory
    {
        IUnitView CreateUnit(Faction faction);
    }
}
