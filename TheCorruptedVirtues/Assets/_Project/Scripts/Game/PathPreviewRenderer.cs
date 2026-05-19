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

            lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 world = gridPresenter.GridToWorld(path[i], gridPresenter.CursorY);
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
