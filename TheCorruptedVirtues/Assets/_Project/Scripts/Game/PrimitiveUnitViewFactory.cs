using UnityEngine;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The only place that knows units are primitives. Shape conveys faction
    // (player capsule, enemy cube) so even greyscale colour-blind viewers can
    // read sides; colour conveys element (Light = pale gold, Dark = deep
    // violet, etc). Swap this class for a model-spawning factory and the
    // rest of the game is unaffected.
    public sealed class PrimitiveUnitViewFactory : IUnitViewFactory
    {
        private readonly Transform parent;

        public PrimitiveUnitViewFactory(Transform parent)
        {
            this.parent = parent;
        }

        public IUnitView CreateUnit(Faction faction, ElementType element)
        {
            PrimitiveType shape = faction == Faction.Player
                ? PrimitiveType.Capsule
                : PrimitiveType.Cube;
            Color color = ColorForElement(element);

            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = faction == Faction.Player ? "PlayerUnitView" : "EnemyUnitView";
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.material = ViewMaterials.CreateColored(color);

            PrimitiveUnitView view = go.AddComponent<PrimitiveUnitView>();
            view.Configure(renderer, color);
            return view;
        }

        private static Color ColorForElement(ElementType element)
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
