using UnityEngine;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Single source of truth for the placeholder element -> colour mapping used
    // by every view that tints by element (unit primitives, turn-order chips).
    // Was duplicated verbatim in PrimitiveUnitViewFactory and TurnOrderPresenter,
    // where the two copies could silently drift; one table here keeps them in
    // step. Swapping in a real art-driven palette/theme is now a one-place change.
    public static class ElementPalette
    {
        public static Color For(ElementType element)
        {
            switch (element)
            {
                case ElementType.Light:       return new Color(0.98f, 0.92f, 0.62f); // pale gold
                case ElementType.Dark:        return new Color(0.32f, 0.18f, 0.42f); // deep violet
                case ElementType.Fire:        return new Color(0.95f, 0.40f, 0.20f);
                case ElementType.Water:       return new Color(0.30f, 0.55f, 0.95f);
                case ElementType.Nature:      return new Color(0.40f, 0.80f, 0.35f);
                case ElementType.Earth:       return new Color(0.65f, 0.50f, 0.30f);
                case ElementType.Electricity: return new Color(0.95f, 0.85f, 0.30f);
                default:                      return new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
