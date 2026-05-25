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
        private static readonly Color HighGroundColor = new Color(0.30f, 0.34f, 0.40f);
        private static readonly Color ObstacleColor = new Color(0.40f, 0.32f, 0.26f);
        private static readonly Color AreaColor = new Color(0.95f, 0.55f, 0.25f);

        private CombatEvents events;
        private GridPresenter grid;
        private Renderer cursorRenderer;
        private PathPreviewRenderer pathPreview;

        // Pooled flat slabs lit on the tiles an AoE attack would hit. Grown on
        // demand; surplus markers are hidden, never destroyed.
        private readonly List<GameObject> areaMarkers = new List<GameObject>();

        // Terrain geometry (ground plane + elevation + obstacle blocks), rebuilt
        // on every GridBuilt. Tracked so an encounter switch tears the old map
        // down before the new one is raised (GridBuilt fires per encounter now).
        private readonly List<GameObject> terrainObjects = new List<GameObject>();

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
            events.AreaPreviewChanged += OnAreaPreviewChanged;
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
            events.AreaPreviewChanged -= OnAreaPreviewChanged;
            events.CombatReset -= OnCombatReset;
        }

        private void OnGridBuilt(GridBuiltEvent e)
        {
            // GridBuilt fires once per encounter load, so tear down the previous
            // map's geometry before building this one.
            ClearTerrain();

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

            Track(ground);
            BuildElevation(e.Bounds);
            BuildObstacles(e.Bounds);
        }

        // Raised blocks for elevated tiles: one cube per high tile spanning
        // from the base ground up to the tile surface (units stand on its top).
        // The ground stays a single flat plane; these are purely additive.
        private void BuildElevation(GridBounds bounds)
        {
            if (grid == null)
            {
                return;
            }

            for (int x = 0; x < bounds.Width; x++)
            {
                for (int y = 0; y < bounds.Height; y++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    float blockHeight = grid.WorldHeightAt(coord);
                    if (blockHeight <= 0f)
                    {
                        continue;
                    }

                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"HighGround_{x}_{y}";
                    block.transform.SetParent(transform, false);

                    Vector3 surface = grid.GridToWorld(coord);
                    block.transform.localScale = new Vector3(grid.CellSize, blockHeight, grid.CellSize);
                    block.transform.position = new Vector3(surface.x, surface.y - blockHeight * 0.5f, surface.z);

                    // Decorative only — keep cursor/mouse raycasts hitting the
                    // flat ground plane rather than these blocks.
                    Collider blockCollider = block.GetComponent<Collider>();
                    if (blockCollider != null)
                    {
                        Destroy(blockCollider);
                    }

                    Renderer blockRenderer = block.GetComponent<Renderer>();
                    if (blockRenderer != null)
                    {
                        blockRenderer.material = ViewMaterials.CreateColored(HighGroundColor);
                    }

                    Track(block);
                }
            }
        }

        // Solid blocks on impassable tiles — taller than elevation steps so they
        // read as walls rather than high ground. Decorative: the collider is
        // removed so the cursor's ground raycast is unaffected (obstacle tiles
        // already resolve to an Invalid selection via the pathfinding occupancy).
        private void BuildObstacles(GridBounds bounds)
        {
            if (grid == null)
            {
                return;
            }

            const float wallHeight = 1.0f;
            for (int x = 0; x < bounds.Width; x++)
            {
                for (int y = 0; y < bounds.Height; y++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    if (!grid.IsObstacle(coord))
                    {
                        continue;
                    }

                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"Obstacle_{x}_{y}";
                    block.transform.SetParent(transform, false);

                    Vector3 surface = grid.GridToWorld(coord);
                    block.transform.localScale = new Vector3(grid.CellSize, wallHeight, grid.CellSize);
                    block.transform.position = new Vector3(surface.x, surface.y + wallHeight * 0.5f, surface.z);

                    Collider blockCollider = block.GetComponent<Collider>();
                    if (blockCollider != null)
                    {
                        Destroy(blockCollider);
                    }

                    Renderer blockRenderer = block.GetComponent<Renderer>();
                    if (blockRenderer != null)
                    {
                        blockRenderer.material = ViewMaterials.CreateColored(ObstacleColor);
                    }

                    Track(block);
                }
            }
        }

        private void Track(GameObject go)
        {
            terrainObjects.Add(go);
        }

        private void ClearTerrain()
        {
            for (int i = 0; i < terrainObjects.Count; i++)
            {
                if (terrainObjects[i] != null)
                {
                    Destroy(terrainObjects[i]);
                }
            }

            terrainObjects.Clear();
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            if (cursorRenderer == null)
            {
                return;
            }

            ViewMaterials.SetColor(cursorRenderer, ColorFor(e.State));
        }

        private void OnPathPreviewChanged(PathPreviewEvent e)
        {
            if (pathPreview != null)
            {
                pathPreview.RenderPath(e.Path, e.ReachableSteps);
            }
        }

        // Light the burst tiles of a hovered AoE target; hide any surplus
        // markers from a previous, larger burst.
        private void OnAreaPreviewChanged(AreaPreviewEvent e)
        {
            if (grid == null)
            {
                return;
            }

            IReadOnlyList<GridCoord> tiles = e.Tiles;
            int count = tiles == null ? 0 : tiles.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject marker = GetOrCreateMarker(i);
                marker.transform.position = grid.GridToWorld(tiles[i], grid.CursorY);
                marker.SetActive(true);
            }

            for (int i = count; i < areaMarkers.Count; i++)
            {
                areaMarkers[i].SetActive(false);
            }
        }

        private GameObject GetOrCreateMarker(int index)
        {
            if (index < areaMarkers.Count)
            {
                return areaMarkers[index];
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"AreaMarker_{index}";
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = new Vector3(0.85f, 0.04f, 0.85f);

            // Decorative: don't let it catch the cursor's ground raycast.
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.material = ViewMaterials.CreateColored(AreaColor);
            }

            marker.SetActive(false);
            areaMarkers.Add(marker);
            return marker;
        }

        private void HideAllMarkers()
        {
            for (int i = 0; i < areaMarkers.Count; i++)
            {
                areaMarkers[i].SetActive(false);
            }
        }

        private void OnCombatReset()
        {
            if (pathPreview != null)
            {
                pathPreview.Clear();
            }

            HideAllMarkers();
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
