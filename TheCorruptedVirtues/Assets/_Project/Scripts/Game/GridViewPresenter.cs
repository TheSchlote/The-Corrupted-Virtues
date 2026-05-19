using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Grid-space visuals: the ground plane (built on GridBuilt), the cursor
    // tint (from SelectionChanged) and the path line (from PathPreviewChanged).
    public sealed class GridViewPresenter : MonoBehaviour
    {
        private static readonly Color NeutralColor = Color.white;
        private static readonly Color ValidColor = new Color(0.4f, 0.9f, 0.4f);
        private static readonly Color AttackColor = new Color(0.95f, 0.75f, 0.3f);
        private static readonly Color InvalidColor = new Color(0.9f, 0.35f, 0.35f);

        private CombatEvents events;
        private GridPresenter grid;
        private Renderer cursorRenderer;
        private PathPreviewRenderer pathPreview;

        public void Initialize(
            CombatEvents combatEvents,
            GridPresenter gridPresenter,
            Renderer cursor,
            PathPreviewRenderer preview)
        {
            events = combatEvents;
            grid = gridPresenter;
            cursorRenderer = cursor;
            pathPreview = preview;

            events.GridBuilt += OnGridBuilt;
            events.SelectionChanged += OnSelectionChanged;
            events.PathPreviewChanged += OnPathPreviewChanged;
            events.CombatReset += OnCombatReset;
        }

        private void OnDestroy()
        {
            if (events == null)
            {
                return;
            }

            events.GridBuilt -= OnGridBuilt;
            events.SelectionChanged -= OnSelectionChanged;
            events.PathPreviewChanged -= OnPathPreviewChanged;
            events.CombatReset -= OnCombatReset;
        }

        private void OnGridBuilt(GridBuiltEvent e)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GridGround";
            ground.transform.SetParent(transform, false);

            // Unity's Plane primitive is 10x10 units at scale 1.
            float w = e.Bounds.Width;
            float h = e.Bounds.Height;
            ground.transform.position = grid.GridToWorld(new GridCoord(0, 0))
                + new Vector3((w - 1) * 0.5f, -0.01f, (h - 1) * 0.5f);
            ground.transform.localScale = new Vector3(w / 10f, 1f, h / 10f);

            Renderer r = ground.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = ViewMaterials.CreateColored(new Color(0.18f, 0.2f, 0.24f));
            }
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            if (cursorRenderer == null)
            {
                return;
            }

            ViewMaterials.SetColor(cursorRenderer, ColorFor(e.State));
        }

        private void OnPathPreviewChanged(IReadOnlyList<GridCoord> path)
        {
            if (pathPreview != null)
            {
                pathPreview.RenderPath(path);
            }
        }

        private void OnCombatReset()
        {
            if (pathPreview != null)
            {
                pathPreview.Clear();
            }

            ViewMaterials.SetColor(cursorRenderer, NeutralColor);
        }

        private static Color ColorFor(SelectionState state)
        {
            switch (state)
            {
                case SelectionState.MoveValid:
                    return ValidColor;
                case SelectionState.AttackValid:
                    return AttackColor;
                case SelectionState.Invalid:
                    return InvalidColor;
                default:
                    return NeutralColor;
            }
        }
    }
}
