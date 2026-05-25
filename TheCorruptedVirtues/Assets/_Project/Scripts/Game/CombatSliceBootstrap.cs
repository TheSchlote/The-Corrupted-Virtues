using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The entire combat slice, constructed in code. The scene contains only
    // this component — no hand-wired GameObjects, no serialized references.
    // Build order: services → events → presenters (subscribe) → orchestrator
    // (emits the opening GridBuilt / UnitSpawned).
    public sealed class CombatSliceBootstrap : MonoBehaviour
    {
        [SerializeField] private float moveStepDelaySeconds = 0.15f;

        private void Awake()
        {
            CreateDirectionalLight();
            GridPresenter grid = CreateGrid();
            Transform cameraTransform = CreateCamera();
            Renderer cursorRenderer = CreateCursor(grid, cameraTransform, out TacticalCursorController cursor);
            PathPreviewRenderer pathPreview = CreatePathPreview(grid);
            IExecutionMeter swingMeter = CreateSwingMeter();
            IExecutionMeter buttonMashMeter = CreateButtonMashMeter();
            IExecutionMeter timedPressMeter = CreateTimedPressMeter();
            IExecutionMeter matchingMeter = CreateMatchingMeter();
            Dictionary<QteType, IExecutionMeter> meters = new Dictionary<QteType, IExecutionMeter>
            {
                { QteType.SwingMeter, swingMeter },
                { QteType.ButtonMash, buttonMashMeter },
                { QteType.TimedPress, timedPressMeter },
                { QteType.Matching, matchingMeter },
            };

            CombatEvents events = new CombatEvents();
            IUnitViewFactory unitFactory = new PrimitiveUnitViewFactory(CreateChild("UnitViews").transform);

            GameObject presentation = CreateChild("Presentation");
            presentation.AddComponent<UnitViewPresenter>().Initialize(events, grid, unitFactory);
            presentation.AddComponent<GridViewPresenter>().Initialize(events, grid, cursorRenderer, pathPreview);
            presentation.AddComponent<HudPresenter>().Initialize(events);
            presentation.AddComponent<TurnOrderPresenter>().Initialize(events);
            presentation.AddComponent<VfxPresenter>().Initialize(events);

            CombatSliceOrchestrator orchestrator =
                CreateChild("Orchestrator").AddComponent<CombatSliceOrchestrator>();
            orchestrator.Initialize(events, grid, cursor, meters, moveStepDelaySeconds);
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

            Renderer cursorRenderer = cursorObject.GetComponent<Renderer>();
            cursorRenderer.material = ViewMaterials.CreateColored(Color.white);
            return cursorRenderer;
        }

        private PathPreviewRenderer CreatePathPreview(GridPresenter grid)
        {
            GameObject pathObject = CreateChild("PathPreview");

            // Two lines: bright yellow within MoveRange, faded grey for the
            // out-of-range continuation so the truncation point reads at a
            // glance. Both have to be separate components because LineRenderer
            // is one-per-GameObject; we parent each under a holder.
            LineRenderer reachable = CreateLine(
                pathObject.transform,
                name: "ReachableLine",
                color: new Color(1f, 0.92f, 0.35f),
                width: 0.14f);

            LineRenderer outOfRange = CreateLine(
                pathObject.transform,
                name: "OutOfRangeLine",
                color: new Color(0.55f, 0.55f, 0.55f),
                width: 0.10f);

            PathPreviewRenderer preview = pathObject.AddComponent<PathPreviewRenderer>();
            preview.Configure(grid, reachable, outOfRange);
            return preview;
        }

        private static LineRenderer CreateLine(Transform parent, string name, Color color, float width)
        {
            GameObject host = new GameObject(name);
            host.transform.SetParent(parent, false);

            LineRenderer line = host.AddComponent<LineRenderer>();
            line.widthMultiplier = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.positionCount = 0;
            return line;
        }

        private IExecutionMeter CreateSwingMeter()
        {
            SwingMeterUiFactory.SwingMeterUi ui = SwingMeterUiFactory.Build();
            SwingMeterController meter = ui.Root.AddComponent<SwingMeterController>();
            meter.SetReferences(ui.Root, ui.Slider, ui.Text, ui.HitZone, ui.DivineZone, ui.OvershootZone);
            return meter;
        }

        private IExecutionMeter CreateButtonMashMeter()
        {
            ButtonMashUiFactory.ButtonMashUi ui = ButtonMashUiFactory.Build();
            ButtonMashController meter = ui.Root.AddComponent<ButtonMashController>();
            meter.SetReferences(ui.Root, ui.Slider, ui.Text);
            return meter;
        }

        private IExecutionMeter CreateTimedPressMeter()
        {
            TimedPressUiFactory.TimedPressUi ui = TimedPressUiFactory.Build();
            TimedPressController meter = ui.Root.AddComponent<TimedPressController>();
            meter.SetReferences(ui.Root, ui.Marker, ui.HitZone, ui.DivineZone, ui.Text);
            return meter;
        }

        private IExecutionMeter CreateMatchingMeter()
        {
            MatchingUiFactory.MatchingUi ui = MatchingUiFactory.Build();
            MatchingController meter = ui.Root.AddComponent<MatchingController>();
            meter.SetReferences(ui.Root, ui.SequenceText, ui.CaptionText);
            return meter;
        }
    }
}
