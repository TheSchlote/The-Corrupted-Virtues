using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public sealed class CombatSliceController : MonoBehaviour
    {
        [SerializeField] private Color walkableColor = new(0.24f, 0.56f, 0.34f, 1f);
        [SerializeField] private Color nonWalkableColor = new(0.55f, 0.21f, 0.18f, 1f);
        [SerializeField] private Color unitColor = new(0.87f, 0.87f, 0.93f, 1f);
        [SerializeField] private Color cursorColor = new(0.12f, 0.9f, 0.97f, 1f);

        private TacticalPathfinding _pathfinding;
        private SelectionCursorActor _cursor;
        private CombatCameraGimbal _cameraGimbal;
        private UnitActor _unit;
        private Coroutine _moveRoutine;

        private Material _walkableMaterial;
        private Material _nonWalkableMaterial;
        private Material _unitMaterial;
        private Material _cursorMaterial;

        private void Start()
        {
            BuildCombatSlice();
        }

        private void OnDestroy()
        {
            ReleaseMaterial(_walkableMaterial);
            ReleaseMaterial(_nonWalkableMaterial);
            ReleaseMaterial(_unitMaterial);
            ReleaseMaterial(_cursorMaterial);
        }

        private void Update()
        {
            if (_pathfinding == null || _cursor == null || _unit == null || _moveRoutine != null)
            {
                return;
            }

            if (PlayerInputBridge.IsConfirmPressedDown())
            {
                _moveRoutine = StartCoroutine(MoveUnitToCursor());
            }
        }

        public UnitActor SpawnUnit(Vector3 spawnPosition)
        {
            Vector3Int gridPosition = _pathfinding.WorldToCell(spawnPosition);
            if (!_pathfinding.IsWalkableCell(gridPosition))
            {
                Debug.Log($"Cannot spawn unit at {spawnPosition}. Cell is not walkable or is occupied.");
                return null;
            }

            var unitRoot = new GameObject("Unit");
            unitRoot.transform.SetParent(transform, true);
            unitRoot.transform.position = spawnPosition;

            UnitActor unitActor = unitRoot.AddComponent<UnitActor>();

            GameObject unitVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitVisual.name = "Visual";
            unitVisual.transform.SetParent(unitRoot.transform, false);
            unitVisual.transform.localPosition = new Vector3(0f, 2f, 0f);
            unitVisual.transform.localScale = new Vector3(1f, 2f, 1f);
            unitVisual.GetComponent<Renderer>().sharedMaterial = _unitMaterial;

            _pathfinding.MarkCellOccupied(gridPosition);
            Debug.Log($"Unit spawned at {spawnPosition}.");
            return unitActor;
        }

        private void BuildCombatSlice()
        {
            IReadOnlyList<GridCellData> cells = CombatMapData.DecodeGridCells();

            _walkableMaterial = CreateMaterial(walkableColor);
            _nonWalkableMaterial = CreateMaterial(nonWalkableColor);
            _unitMaterial = CreateMaterial(unitColor);
            _cursorMaterial = CreateMaterial(cursorColor);

            var pathfindingObject = new GameObject("Pathfinding");
            pathfindingObject.transform.SetParent(transform, false);
            _pathfinding = pathfindingObject.AddComponent<TacticalPathfinding>();
            _pathfinding.SetupGrid(cells, CombatMapData.CellSize, CombatMapData.WalkableTileId);

            BuildMapVisuals(cells);

            _cursor = BuildCursor(CombatMapData.PlayerSpawnWorldPosition);
            _cameraGimbal = BuildCameraGimbal(_cursor.transform);
            _cursor.Setup(_pathfinding, _cameraGimbal, CombatMapData.PlayerSpawnWorldPosition);

            _unit = SpawnUnit(CombatMapData.PlayerSpawnWorldPosition);
        }

        private void BuildMapVisuals(IReadOnlyList<GridCellData> cells)
        {
            var mapRoot = new GameObject("Map");
            mapRoot.transform.SetParent(transform, false);

            for (int i = 0; i < cells.Count; i++)
            {
                GridCellData cell = cells[i];

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Cell_{cell.Coordinate.x}_{cell.Coordinate.y}_{cell.Coordinate.z}_Tile_{cell.TileId}";
                tile.transform.SetParent(mapRoot.transform, false);
                tile.transform.position = _pathfinding.CellToWorld(cell.Coordinate) +
                                          (Vector3.up * (CombatMapData.CellSize * 0.5f));
                tile.transform.localScale = Vector3.one * CombatMapData.CellSize;

                Renderer renderer = tile.GetComponent<Renderer>();
                renderer.sharedMaterial = cell.TileId == CombatMapData.WalkableTileId
                    ? _walkableMaterial
                    : _nonWalkableMaterial;
            }
        }

        private SelectionCursorActor BuildCursor(Vector3 startPosition)
        {
            var cursorObject = new GameObject("SelectionCursor");
            cursorObject.transform.SetParent(transform, false);
            cursorObject.transform.position = startPosition;

            SelectionCursorActor cursorActor = cursorObject.AddComponent<SelectionCursorActor>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(cursorObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 2f, 0f);
            visual.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            visual.GetComponent<Renderer>().sharedMaterial = _cursorMaterial;

            return cursorActor;
        }

        private CombatCameraGimbal BuildCameraGimbal(Transform cursorTransform)
        {
            var gimbalObject = new GameObject("CameraGimbal");
            gimbalObject.transform.SetParent(cursorTransform, false);
            gimbalObject.transform.localPosition = new Vector3(0f, 2f, 0f);

            CombatCameraGimbal gimbal = gimbalObject.AddComponent<CombatCameraGimbal>();
            gimbal.Setup(GetOrCreateMainCamera());
            return gimbal;
        }

        private IEnumerator MoveUnitToCursor()
        {
            if (_unit == null || _cursor == null || _pathfinding == null)
            {
                Debug.LogError("Unit, Cursor, or Pathfinding not found.");
                _moveRoutine = null;
                yield break;
            }

            Vector3 targetPosition = _cursor.GetSelectedTile();
            Vector3Int gridTarget = _pathfinding.WorldToCell(targetPosition);

            if (!_pathfinding.IsWalkableCell(gridTarget))
            {
                Debug.Log($"Blocked: Cannot move to {gridTarget}.");
                _moveRoutine = null;
                yield break;
            }

            yield return _unit.MoveTo(targetPosition, _pathfinding);
            Debug.Log($"Unit moved to {targetPosition}.");

            _moveRoutine = null;
        }

        private static Camera GetOrCreateMainCamera()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                return main;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                color = color
            };

            return material;
        }

        private static void ReleaseMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
                return;
            }

            DestroyImmediate(material);
        }
    }
}
