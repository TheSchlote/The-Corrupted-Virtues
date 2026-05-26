using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

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

        public IUnitView CreateUnit(Faction faction, ElementType element, GridFootprint footprint, bool isBoss)
        {
            PrimitiveType shape = faction == Faction.Player
                ? PrimitiveType.Capsule
                : PrimitiveType.Cube;
            Color color = ElementPalette.For(element);

            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = isBoss
                ? "BossView"
                : (faction == Faction.Player ? "PlayerUnitView" : "EnemyUnitView");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            // Multi-tile units fill their footprint (small gap keeps the cell
            // grid legible underneath). 1x1 units keep the default unit scale.
            int span = footprint.Width > footprint.Height ? footprint.Width : footprint.Height;
            if (span > 1)
            {
                float scale = span - 0.1f;
                go.transform.localScale = new Vector3(scale, scale, scale);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.material = ViewMaterials.CreateColored(color);

            PrimitiveUnitView view = go.AddComponent<PrimitiveUnitView>();
            view.Configure(renderer, color, isBoss);
            return view;
        }
    }
}
