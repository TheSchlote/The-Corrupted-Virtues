using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Simple line-based preview renderer for grid paths.
    public sealed class PathPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private GridPresenter gridPresenter;
        [SerializeField] private LineRenderer lineRenderer;

        // Code-driven wiring (no serialized scene refs needed).
        public void Configure(GridPresenter presenter, LineRenderer line)
        {
            gridPresenter = presenter;
            lineRenderer = line;
        }

        public void RenderPath(IReadOnlyList<GridCoord> path)
        {
            if (lineRenderer == null || gridPresenter == null)
            {
                return;
            }

            if (path == null || path.Count == 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            // Float the line slightly above the cursor's hover height so the
            // path reads cleanly over the ground plane instead of z-fighting it.
            float lineY = gridPresenter.CursorY + 0.18f;
            lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 world = gridPresenter.GridToWorld(path[i], lineY);
                lineRenderer.SetPosition(i, world);
            }
        }

        public void Clear()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }
    }
}
