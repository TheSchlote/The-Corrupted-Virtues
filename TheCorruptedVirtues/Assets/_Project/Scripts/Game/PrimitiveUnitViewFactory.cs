using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The only place that knows units are primitives. Player = blue capsule,
    // enemy = red cube. Swap this class for a model-spawning factory and the
    // rest of the game is unaffected.
    public sealed class PrimitiveUnitViewFactory : IUnitViewFactory
    {
        private static readonly Color PlayerColor = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color EnemyColor = new Color(0.9f, 0.3f, 0.25f);

        private readonly Transform parent;

        public PrimitiveUnitViewFactory(Transform parent)
        {
            this.parent = parent;
        }

        public IUnitView CreateUnit(Faction faction)
        {
            PrimitiveType shape = faction == Faction.Player
                ? PrimitiveType.Capsule
                : PrimitiveType.Cube;
            Color color = faction == Faction.Player ? PlayerColor : EnemyColor;

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
    }
}
