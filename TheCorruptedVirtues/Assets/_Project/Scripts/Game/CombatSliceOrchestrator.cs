using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The combat loop's logic owner. It manipulates only logical state and
    // announces what happened through CombatEvents — it never touches a
    // GameObject, transform, renderer or UI element. Presenters do that.
    public sealed class CombatSliceOrchestrator : MonoBehaviour
    {
        private sealed class CombatUnit
        {
            public UnitId Id;
            public Faction Faction;
            public GridCoord Coord;
            public GridCoord SpawnCoord;
            public int Hp;
            public int MaxHp;
            public int MoveRange;
        }

        private readonly GridOccupancy occupancy = new GridOccupancy();
        private readonly List<GridCoord> currentPath = new List<GridCoord>();

        private CombatEvents events;
        private GridPresenter grid;
        private TacticalCursorController cursor;
        private IExecutionMeter swingMeter;

        private int baseAttackDamage = 10;
        private float moveStepDelaySeconds = 0.15f;

        private CombatUnit player;
        private CombatUnit enemy;
        private CombatUnit activeUnit;
        private CombatUnit otherUnit;
        private bool isPlayerTurn = true;
        private bool isMoving;
        private bool isAwaitingSwingStop;
        private bool started;

        public void Initialize(
            CombatEvents combatEvents,
            GridPresenter gridPresenter,
            TacticalCursorController tacticalCursor,
            IExecutionMeter executionMeter,
            int attackDamage,
            float moveStepDelay)
        {
            events = combatEvents;
            grid = gridPresenter;
            cursor = tacticalCursor;
            swingMeter = executionMeter;
            baseAttackDamage = attackDamage;
            moveStepDelaySeconds = moveStepDelay;

            player = new CombatUnit
            {
                Id = new UnitId(1),
                Faction = Faction.Player,
                Coord = new GridCoord(1, 1),
                SpawnCoord = new GridCoord(1, 1),
                Hp = 100,
                MaxHp = 100,
                MoveRange = 4
            };
            enemy = new CombatUnit
            {
                Id = new UnitId(2),
                Faction = Faction.Enemy,
                Coord = new GridCoord(6, 6),
                SpawnCoord = new GridCoord(6, 6),
                Hp = 100,
                MaxHp = 100,
                MoveRange = 4
            };

            events.RaiseGridBuilt(new GridBuiltEvent(grid.Bounds));
            events.RaiseUnitSpawned(new UnitSpawnedEvent(player.Id, player.Faction, player.Coord, player.Hp, player.MaxHp));
            events.RaiseUnitSpawned(new UnitSpawnedEvent(enemy.Id, enemy.Faction, enemy.Coord, enemy.Hp, enemy.MaxHp));

            started = true;
            ResetSliceState();
        }

        private void Update()
        {
            if (!started)
            {
                return;
            }

            if (GameInput.Current.ResetPressed)
            {
                ResetSliceState();
                return;
            }

            if (isMoving)
            {
                return;
            }

            if (isAwaitingSwingStop)
            {
                if (GameInput.Current.ConfirmPressed)
                {
                    ResolvePlayerSwingStop();
                }

                return;
            }

            if (cursor != null && cursor.TryMoveCursor())
            {
                UpdatePreview();
            }

            if (GameInput.Current.ConfirmPressed)
            {
                HandleConfirm();
            }
        }

        private void ResetSliceState()
        {
            isMoving = false;
            isAwaitingSwingStop = false;
            swingMeter?.Cancel();

            player.Coord = player.SpawnCoord;
            enemy.Coord = enemy.SpawnCoord;
            player.Hp = player.MaxHp;
            enemy.Hp = enemy.MaxHp;

            events.RaiseCombatReset();
            events.RaiseUnitSpawned(new UnitSpawnedEvent(player.Id, player.Faction, player.Coord, player.Hp, player.MaxHp));
            events.RaiseUnitSpawned(new UnitSpawnedEvent(enemy.Id, enemy.Faction, enemy.Coord, enemy.Hp, enemy.MaxHp));

            isPlayerTurn = true;
            SetActiveUnit(player, enemy);

            if (cursor != null)
            {
                cursor.IsLocked = false;
                cursor.Initialize(activeUnit.Coord);
            }

            events.RaiseTurnChanged(Faction.Player);
            UpdatePreview();
        }

        private void SetActiveUnit(CombatUnit active, CombatUnit other)
        {
            activeUnit = active;
            otherUnit = other;
        }

        private void UpdateOccupancy()
        {
            occupancy.Clear();
            occupancy.Add(player.Coord);
            occupancy.Add(enemy.Coord);
        }

        private void UpdatePreview()
        {
            if (activeUnit == null || cursor == null)
            {
                return;
            }

            UpdateOccupancy();
            currentPath.Clear();
            List<GridCoord> path = GridPathfinderBfs.FindPath(
                activeUnit.Coord,
                cursor.CursorCoord,
                occupancy,
                grid.Bounds);
            currentPath.AddRange(path);

            events.RaisePathPreviewChanged(currentPath);
            RaiseSelection(cursor.CursorCoord);
        }

        private void RaiseSelection(GridCoord cursorCoord)
        {
            UpdateOccupancy();
            int steps = GridMath.StepCount(currentPath);
            bool occupied = occupancy.IsOccupied(cursorCoord) && cursorCoord != activeUnit.Coord;
            bool reachable = steps > 0 && steps <= activeUnit.MoveRange;
            bool canAttack = cursorCoord == otherUnit.Coord
                && GridMath.ManhattanDistance(activeUnit.Coord, otherUnit.Coord) == 1;

            SelectionState state;
            string hint;
            if (canAttack)
            {
                state = SelectionState.AttackValid;
                hint = "Confirm: Attack";
            }
            else if (reachable && !occupied)
            {
                state = SelectionState.MoveValid;
                hint = "Confirm: Move";
            }
            else if (cursorCoord == activeUnit.Coord)
            {
                state = SelectionState.Neutral;
                hint = string.Empty;
            }
            else
            {
                state = SelectionState.Invalid;
                hint = string.Empty;
            }

            events.RaiseSelectionChanged(new SelectionChangedEvent(cursorCoord, state, hint));
        }

        private void HandleConfirm()
        {
            if (activeUnit == null || otherUnit == null || cursor == null)
            {
                return;
            }

            GridCoord target = cursor.CursorCoord;
            int distance = GridMath.ManhattanDistance(activeUnit.Coord, otherUnit.Coord);

            if (target == otherUnit.Coord && distance == 1)
            {
                if (isPlayerTurn)
                {
                    BeginPlayerAttack();
                }
                else
                {
                    ResolveAttack(activeUnit, otherUnit, baseAttackDamage);
                    EndTurn();
                }

                return;
            }

            UpdateOccupancy();
            if (occupancy.IsOccupied(target) && target != activeUnit.Coord)
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

        private void BeginPlayerAttack()
        {
            if (swingMeter == null || !swingMeter.IsAvailable)
            {
                ResolveAttack(activeUnit, otherUnit, baseAttackDamage);
                EndTurn();
                return;
            }

            if (isAwaitingSwingStop)
            {
                return;
            }

            isAwaitingSwingStop = true;
            if (cursor != null)
            {
                cursor.IsLocked = true;
            }

            events.RaisePathPreviewChanged(System.Array.Empty<GridCoord>());
            swingMeter.Begin();
            events.RaiseSelectionChanged(new SelectionChangedEvent(cursor.CursorCoord, SelectionState.Neutral, "Confirm: Stop Swing"));
        }

        private void ResolvePlayerSwingStop()
        {
            if (!isAwaitingSwingStop)
            {
                return;
            }

            isAwaitingSwingStop = false;
            if (swingMeter == null || !swingMeter.IsAvailable)
            {
                if (cursor != null)
                {
                    cursor.IsLocked = false;
                }

                return;
            }

            ExecutionResult tier = swingMeter.StopAndEvaluate(out float multiplier, out _);
            events.RaiseExecutionGraded(new ExecutionGradedEvent(tier, multiplier));

            int damage = Mathf.Max(0, Mathf.RoundToInt(baseAttackDamage * multiplier));
            ResolveAttack(activeUnit, otherUnit, damage);

            if (cursor != null)
            {
                cursor.IsLocked = false;
            }

            EndTurn();
        }

        private void BeginMove(List<GridCoord> path)
        {
            if (path.Count == 0)
            {
                return;
            }

            StartCoroutine(MoveAlongPath(new List<GridCoord>(path)));
        }

        private IEnumerator MoveAlongPath(List<GridCoord> path)
        {
            isMoving = true;
            if (cursor != null)
            {
                cursor.IsLocked = true;
            }

            for (int i = 1; i < path.Count; i++)
            {
                activeUnit.Coord = path[i];
                events.RaiseUnitMoved(new UnitMovedEvent(activeUnit.Id, activeUnit.Coord));
                UpdateOccupancy();
                yield return new WaitForSeconds(moveStepDelaySeconds);
            }

            isMoving = false;
            if (cursor != null)
            {
                cursor.IsLocked = false;
            }

            EndTurn();
        }

        private void ResolveAttack(CombatUnit attacker, CombatUnit defender, int damage)
        {
            defender.Hp = Mathf.Max(0, defender.Hp - damage);
            events.RaiseUnitDamaged(new UnitDamagedEvent(defender.Id, damage, defender.Hp, defender.MaxHp));

            if (defender.Hp <= 0)
            {
                events.RaiseUnitDied(defender.Id);
            }
        }

        private void EndTurn()
        {
            isPlayerTurn = !isPlayerTurn;
            SetActiveUnit(isPlayerTurn ? player : enemy, isPlayerTurn ? enemy : player);
            events.RaiseTurnChanged(isPlayerTurn ? Faction.Player : Faction.Enemy);

            if (isPlayerTurn)
            {
                if (cursor != null)
                {
                    cursor.IsLocked = false;
                    cursor.Initialize(activeUnit.Coord);
                }

                UpdatePreview();
                return;
            }

            StartCoroutine(HandleEnemyTurn());
        }

        private IEnumerator HandleEnemyTurn()
        {
            yield return new WaitForSeconds(0.1f);

            int distance = GridMath.ManhattanDistance(activeUnit.Coord, otherUnit.Coord);
            if (distance == 1)
            {
                ResolveAttack(activeUnit, otherUnit, baseAttackDamage);
                EndTurn();
                yield break;
            }

            GridCoord step = GetStepToward(activeUnit.Coord, otherUnit.Coord);
            UpdateOccupancy();
            if (occupancy.IsOccupied(step) && step != activeUnit.Coord)
            {
                EndTurn();
                yield break;
            }

            List<GridCoord> path = GridPathfinderBfs.FindPath(
                activeUnit.Coord,
                step,
                occupancy,
                grid.Bounds);

            if (path.Count == 0 || GridMath.StepCount(path) > activeUnit.MoveRange)
            {
                EndTurn();
                yield break;
            }

            BeginMove(path);
        }

        private static GridCoord GetStepToward(GridCoord from, GridCoord to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                return new GridCoord(from.X + (dx == 0 ? 0 : (dx > 0 ? 1 : -1)), from.Y);
            }

            return new GridCoord(from.X, from.Y + (dy == 0 ? 0 : (dy > 0 ? 1 : -1)));
        }
    }
}
