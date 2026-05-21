using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Creates unit visuals. Swapping this implementation (primitives →
    // models) is the single-place change the asset-agnostic architecture
    // is built around. Takes both faction and element so visuals can convey
    // either dimension — current primitive impl uses shape for faction and
    // colour for element.
    public interface IUnitViewFactory
    {
        IUnitView CreateUnit(Faction faction, ElementType element);
    }
}
