using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Orchestrates a minimal Gladius-style combat slice.
    public sealed class CombatSliceController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private GridPresenter gridPresenter;
        [SerializeField] private TacticalCursorController tacticalCursor;
        [SerializeField] private UnitActor playerUnit;
        [SerializeField] private UnitActor enemyUnit;
        [SerializeField] private Transform intentMarkerTemplate;
        [SerializeField] private PathPreviewRenderer pathPreview;

        [Header("UI")]
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text selectedText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private TMP_Text actionHintText;

        [Header("Tuning")]
        [SerializeField] private float moveStepDelaySeconds = 0.15f;
        [SerializeField] private Color cursorNeutralColor = Color.white;
        [SerializeField] private Color cursorValidColor = Color.green;
        [SerializeField] private Color cursorInvalidColor = Color.red;

        private readonly GridOccupancy occupancy = new GridOccupancy();
        private readonly List<GridCoord> currentPath = new List<GridCoord>();
        private UnitActor activeUnit;
        private UnitActor otherUnit;
        private bool isPlayerTurn = true;
        private bool isMoving;
        private Transform activeIntentMarker;
        private Renderer cursorRenderer;

        private void Start()
        {
            InitializeUnits();
            ResetSliceState();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetSliceState();
                return;
            }

            if (isMoving)
            {
                return;
            }

            bool cursorMoved = tacticalCursor != null && tacticalCursor.TryMoveCursor();
            if (cursorMoved)
            {
                UpdatePreview();
            }

            if (IsConfirmPressed())
            {
                HandleConfirm();
            }
        }

        private void InitializeUnits()
        {
            if (gridPresenter == null || playerUnit == null || enemyUnit == null)
            {
                Debug.LogError("CombatSliceController missing references.");
                enabled = false;
                return;
            }

            playerUnit.InitializeCoord(gridPresenter.WorldToGrid(playerUnit.transform.position));
            enemyUnit.InitializeCoord(gridPresenter.WorldToGrid(enemyUnit.transform.position));

            if (tacticalCursor != null)
            {
                cursorRenderer = tacticalCursor.GetComponent<Renderer>();
            }
        }

        private void ResetSliceState()
        {
            isMoving = false;
            ClearIntentMarker();
            UpdateOccupancy();

            playerUnit.ResetHp();
            enemyUnit.ResetHp();

            playerUnit.SetCoord(playerUnit.SpawnCoord);
            enemyUnit.SetCoord(enemyUnit.SpawnCoord);
            SetUnitWorldPosition(playerUnit, playerUnit.SpawnCoord);
            SetUnitWorldPosition(enemyUnit, enemyUnit.SpawnCoord);

            isPlayerTurn = true;
            SetActiveUnit(playerUnit, enemyUnit);

            if (tacticalCursor != null)
            {
                tacticalCursor.IsLocked = false;
                tacticalCursor.Initialize(activeUnit.CurrentCoord);
            }

            UpdatePreview();
            UpdateDebugAndCursor(tacticalCursor != null ? tacticalCursor.CursorCoord : activeUnit.CurrentCoord);
            UpdateUi();
        }

        private void SetActiveUnit(UnitActor active, UnitActor other)
        {
            activeUnit = active;
            otherUnit = other;
        }

        private void UpdateOccupancy()
        {
            occupancy.Clear();
            occupancy.Add(playerUnit.CurrentCoord);
            occupancy.Add(enemyUnit.CurrentCoord);
        }

        private void UpdatePreview()
        {
            if (activeUnit == null || tacticalCursor == null || gridPresenter == null)
            {
                return;
            }

            UpdateOccupancy();
            currentPath.Clear();

            List<GridCoord> path = GridPathfinderBfs.FindPath(
                activeUnit.CurrentCoord,
                tacticalCursor.CursorCoord,
                occupancy,
                gridPresenter.Bounds);

            currentPath.AddRange(path);
            pathPreview?.RenderPath(currentPath);
            UpdateDebugAndCursor(tacticalCursor.CursorCoord);
        }

        private void HandleConfirm()
        {
            if (activeUnit == null || otherUnit == null || tacticalCursor == null)
            {
                return;
            }

            GridCoord target = tacticalCursor.CursorCoord;
            int distance = GridMath.ManhattanDistance(activeUnit.CurrentCoord, otherUnit.CurrentCoord);

            if (target == otherUnit.CurrentCoord && distance == 1)
            {
                ResolveAttack(activeUnit, otherUnit);
                EndTurn();
                return;
            }

            UpdateOccupancy();
            bool occupied = occupancy.IsOccupied(target) && target != activeUnit.CurrentCoord;
            if (occupied)
            {
                return;
            }

            int steps = GridMath.StepCount(currentPath);
            if (steps == 0 || steps > activeUnit.MoveRange)
            {
                return;
            }

            BeginMove(currentPath);
        }

        private void BeginMove(List<GridCoord> path)
        {
            if (path.Count == 0)
            {
                return;
            }

            PlaceIntentMarker(path[path.Count - 1]);
            StartCoroutine(MoveAlongPath(path));
        }

        private IEnumerator MoveAlongPath(List<GridCoord> path)
        {
            isMoving = true;
            tacticalCursor.IsLocked = true;

            for (int i = 1; i < path.Count; i++)
            {
                GridCoord coord = path[i];
                activeUnit.SetCoord(coord);
                SetUnitWorldPosition(activeUnit, coord);
                UpdateOccupancy();
                yield return new WaitForSeconds(moveStepDelaySeconds);
            }

            ClearIntentMarker();
            isMoving = false;
            tacticalCursor.IsLocked = false;

            EndTurn();
        }

        private void ResolveAttack(UnitActor attacker, UnitActor defender)
        {
            defender.ApplyDamage(10);
            if (defender.CurrentHp <= 0)
            {
                Debug.Log("Unit defeated");
                defender.gameObject.SetActive(false);
            }

            UpdateUi();
            UpdateDebugAndCursor(tacticalCursor != null ? tacticalCursor.CursorCoord : attacker.CurrentCoord);
        }

        private void EndTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            SetActiveUnit(isPlayerTurn ? playerUnit : enemyUnit, isPlayerTurn ? enemyUnit : playerUnit);

            if (isPlayerTurn)
            {
                if (tacticalCursor != null)
                {
                    tacticalCursor.IsLocked = false;
                    tacticalCursor.Initialize(activeUnit.CurrentCoord);
                }

                UpdatePreview();
                UpdateDebugAndCursor(tacticalCursor != null ? tacticalCursor.CursorCoord : activeUnit.CurrentCoord);
                UpdateUi();
                return;
            }

            StartCoroutine(HandleEnemyTurn());
        }

        private IEnumerator HandleEnemyTurn()
        {
            yield return new WaitForSeconds(0.1f);

            int distance = GridMath.ManhattanDistance(activeUnit.CurrentCoord, otherUnit.CurrentCoord);
            if (distance == 1)
            {
                ResolveAttack(activeUnit, otherUnit);
                EndTurn();
                yield break;
            }

            GridCoord step = GetStepToward(activeUnit.CurrentCoord, otherUnit.CurrentCoord);
            UpdateOccupancy();
            if (occupancy.IsOccupied(step) && step != activeUnit.CurrentCoord)
            {
                EndTurn();
                yield break;
            }

            List<GridCoord> path = GridPathfinderBfs.FindPath(
                activeUnit.CurrentCoord,
                step,
                occupancy,
                gridPresenter.Bounds);

            if (path.Count == 0 || GridMath.StepCount(path) > activeUnit.MoveRange)
            {
                EndTurn();
                yield break;
            }

            BeginMove(path);
        }

        private GridCoord GetStepToward(GridCoord from, GridCoord to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                return new GridCoord(from.X + (dx == 0 ? 0 : (dx > 0 ? 1 : -1)), from.Y);
            }

            return new GridCoord(from.X, from.Y + (dy == 0 ? 0 : (dy > 0 ? 1 : -1)));
        }

        private void PlaceIntentMarker(GridCoord coord)
        {
            if (intentMarkerTemplate == null)
            {
                return;
            }

            ClearIntentMarker();
            activeIntentMarker = Instantiate(intentMarkerTemplate, transform);
            activeIntentMarker.gameObject.SetActive(true);
            activeIntentMarker.position = gridPresenter.GridToWorld(coord, gridPresenter.CursorY);
        }

        private void ClearIntentMarker()
        {
            if (activeIntentMarker != null)
            {
                Destroy(activeIntentMarker.gameObject);
                activeIntentMarker = null;
            }
        }

        private void SetUnitWorldPosition(UnitActor unit, GridCoord coord)
        {
            unit.transform.position = gridPresenter.GridToWorld(coord, gridPresenter.UnitY);
        }

        private bool IsConfirmPressed()
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            return Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
        }

        private void UpdateUi()
        {
            if (turnText != null)
            {
                turnText.text = isPlayerTurn ? "Turn: Player" : "Turn: Enemy";
            }

            if (selectedText != null)
            {
                selectedText.text = isPlayerTurn ? "Active: Player" : "Active: Enemy";
            }

            if (playerHpText != null)
            {
                playerHpText.text = $"Player HP: {playerUnit.CurrentHp}";
            }

            if (enemyHpText != null)
            {
                enemyHpText.text = $"Enemy HP: {enemyUnit.CurrentHp}";
            }
        }

        private void UpdateDebugAndCursor(GridCoord cursorCoord)
        {
            if (activeUnit == null)
            {
                return;
            }

            UpdateOccupancy();
            int stepCount = GridMath.StepCount(currentPath);
            bool occupied = occupancy.IsOccupied(cursorCoord) && cursorCoord != activeUnit.CurrentCoord;
            bool reachable = stepCount > 0 && stepCount <= activeUnit.MoveRange;
            bool canAttack = otherUnit != null
                && cursorCoord == otherUnit.CurrentCoord
                && GridMath.ManhattanDistance(activeUnit.CurrentCoord, otherUnit.CurrentCoord) == 1;

            if (debugText != null)
            {
                debugText.text = $"Active: {activeUnit.Team} | Unit: {activeUnit.CurrentCoord} | Cursor: {cursorCoord} | Steps: {stepCount} | Reachable: {reachable} | Occupied: {occupied}";
            }

            if (actionHintText != null)
            {
                if (canAttack)
                {
                    actionHintText.text = "Confirm: Attack";
                }
                else if (reachable && !occupied)
                {
                    actionHintText.text = "Confirm: Move";
                }
                else
                {
                    actionHintText.text = "Confirm: -";
                }
            }

            UpdateCursorColor(cursorCoord, reachable, occupied);
        }

        private void UpdateCursorColor(GridCoord cursorCoord, bool reachable, bool occupied)
        {
            if (cursorRenderer == null)
            {
                return;
            }

            if (cursorCoord == activeUnit.CurrentCoord)
            {
                cursorRenderer.material.color = cursorNeutralColor;
                return;
            }

            cursorRenderer.material.color = reachable && !occupied ? cursorValidColor : cursorInvalidColor;
        }
    }
}
