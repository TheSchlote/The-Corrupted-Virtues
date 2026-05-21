using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Line-based preview renderer for grid paths. Splits the rendered path
    // into a bright in-range segment (what this turn's move can actually
    // reach) and a faded out-of-range continuation (what was targeted past
    // the reach), so the player can target far and see where the move will
    // truncate.
    public sealed class PathPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private GridPresenter gridPresenter;
        [SerializeField] private LineRenderer reachableLine;
        [SerializeField] private LineRenderer outOfRangeLine;

        // Code-driven wiring (no serialized scene refs needed).
        public void Configure(GridPresenter presenter, LineRenderer reachable, LineRenderer outOfRange)
        {
            gridPresenter = presenter;
            reachableLine = reachable;
            outOfRangeLine = outOfRange;
        }

        public void RenderPath(IReadOnlyList<GridCoord> path, int reachableSteps)
        {
            if (gridPresenter == null)
            {
                return;
            }

            if (path == null || path.Count == 0)
            {
                Clear();
                return;
            }

            // Float the line slightly above the cursor's hover height so the
            // path reads cleanly over the ground plane instead of z-fighting it.
            float lineY = gridPresenter.CursorY + 0.18f;

            // reachableSteps is the number of *edges* the unit can traverse;
            // it consumes reachableSteps+1 path points. Clamp into the path.
            int reachablePoints = Mathf.Clamp(reachableSteps + 1, 0, path.Count);

            FillLine(reachableLine, gridPresenter, path, 0, reachablePoints, lineY);

            // Out-of-range continuation: from the last in-range point onward,
            // so the two segments visually connect.
            if (reachablePoints > 0 && reachablePoints < path.Count)
            {
                FillLine(outOfRangeLine, gridPresenter, path, reachablePoints - 1, path.Count - reachablePoints + 1, lineY);
            }
            else
            {
                ClearLine(outOfRangeLine);
            }
        }

        public void Clear()
        {
            ClearLine(reachableLine);
            ClearLine(outOfRangeLine);
        }

        private static void FillLine(LineRenderer line, GridPresenter grid, IReadOnlyList<GridCoord> path, int startIndex, int count, float y)
        {
            if (line == null)
            {
                return;
            }

            if (count <= 1)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                line.SetPosition(i, grid.GridToWorld(path[startIndex + i], y));
            }
        }

        private static void ClearLine(LineRenderer line)
        {
            if (line != null)
            {
                line.positionCount = 0;
            }
        }
    }
}
