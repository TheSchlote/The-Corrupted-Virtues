using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The entire combat slice, constructed in code. The scene contains only
    // this component — no hand-wired GameObjects, no serialized references.
    // Build order: services → events → presenters (subscribe) → orchestrator
    // (emits the opening GridBuilt / UnitSpawned).
    public sealed class CombatSliceBootstrap : MonoBehaviour
    {
        [SerializeField] private int baseAttackDamage = 10;
        [SerializeField] private float moveStepDelaySeconds = 0.15f;

        private void Awake()
        {
            CreateDirectionalLight();
            GridPresenter grid = CreateGrid();
            Transform cameraTransform = CreateCamera();
            Renderer cursorRenderer = CreateCursor(grid, cameraTransform, out TacticalCursorController cursor);
            PathPreviewRenderer pathPreview = CreatePathPreview(grid);
            IExecutionMeter swingMeter = CreateSwingMeter();

            CombatEvents events = new CombatEvents();
            IUnitViewFactory unitFactory = new PrimitiveUnitViewFactory(CreateChild("UnitViews").transform);

            GameObject presentation = CreateChild("Presentation");
            presentation.AddComponent<UnitViewPresenter>().Initialize(events, grid, unitFactory);
            presentation.AddComponent<GridViewPresenter>().Initialize(events, grid, cursorRenderer, pathPreview);
            presentation.AddComponent<HudPresenter>().Initialize(events);
            presentation.AddComponent<VfxPresenter>().Initialize(events);

            CombatSliceOrchestrator orchestrator =
                CreateChild("Orchestrator").AddComponent<CombatSliceOrchestrator>();
            orchestrator.Initialize(events, grid, cursor, swingMeter, baseAttackDamage, moveStepDelaySeconds);
        }

        private GameObject CreateChild(string childName)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go;
        }

        private void CreateDirectionalLight()
        {
            GameObject lightObject = CreateChild("DirectionalLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private GridPresenter CreateGrid()
        {
            return CreateChild("Grid").AddComponent<GridPresenter>();
        }

        private Transform CreateCamera()
        {
            GameObject cameraObject = CreateChild("MainCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(1f, 12f, -7f);
            cameraObject.AddComponent<TacticalCameraController>();
            return cameraObject.transform;
        }

        private Renderer CreateCursor(GridPresenter grid, Transform cameraTransform, out TacticalCursorController cursor)
        {
            GameObject cursorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cursorObject.name = "TacticalCursor";
            cursorObject.transform.SetParent(transform, false);
            cursorObject.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            Collider collider = cursorObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            cursor = cursorObject.AddComponent<TacticalCursorController>();
            cursor.Configure(grid, cameraTransform);
            return cursorObject.GetComponent<Renderer>();
        }

        private PathPreviewRenderer CreatePathPreview(GridPresenter grid)
        {
            GameObject pathObject = CreateChild("PathPreview");
            LineRenderer line = pathObject.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.08f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.4f, 0.85f, 1f);
            line.endColor = new Color(0.4f, 0.85f, 1f);
            line.numCornerVertices = 2;
            line.positionCount = 0;

            PathPreviewRenderer preview = pathObject.AddComponent<PathPreviewRenderer>();
            preview.Configure(grid, line);
            return preview;
        }

        private IExecutionMeter CreateSwingMeter()
        {
            SwingMeterUiFactory.SwingMeterUi ui = SwingMeterUiFactory.Build();
            SwingMeterController meter = ui.Root.AddComponent<SwingMeterController>();
            meter.SetReferences(ui.Root, ui.Slider, ui.Text);
            return meter;
        }
    }
}
