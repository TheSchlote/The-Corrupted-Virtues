using System;
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
    //
    // M2 slice 1: squads + Speed-based interleaved turn order. The single
    // player/enemy fields are gone; the orchestrator now owns a list of all
    // units, builds a per-round queue (highest Speed first, ties by lower
    // Id), and walks one unit's turn at a time. Combat ends when either
    // faction has no living units.
    public sealed class CombatSliceOrchestrator : MonoBehaviour
    {
        private sealed class CombatUnit
        {
            public UnitId Id;
            public Faction Faction;
            public GridCoord Coord;
            public GridCoord SpawnCoord;
            public int Hp;
            public int Mp;
            public CombatStats Stats;
            public ElementType Element;
            public List<AbilitySpec> Abilities;
            public int SelectedAbilityIndex;
            public int MoveRange;

            public int MaxHp => Stats.MaxHP;
            public int MaxMp => Stats.MaxMP;
            public bool IsAlive => Hp > 0;
            public AbilitySpec BasicAttack => Abilities[0];
            public AbilitySpec SelectedAbility => Abilities[SelectedAbilityIndex];
        }

        private readonly GridOccupancy occupancy = new GridOccupancy();
        private readonly List<GridCoord> currentPath = new List<GridCoord>();
        private readonly List<GridCoord> moveTrail = new List<GridCoord>();
        private readonly List<GridCoord> ghostPathBuffer = new List<GridCoord>();

        private readonly List<CombatUnit> allUnits = new List<CombatUnit>();
        private readonly Queue<CombatUnit> roundQueue = new Queue<CombatUnit>();
        private readonly List<TurnOrderEntry> turnOrderEntriesBuffer = new List<TurnOrderEntry>();
        private readonly List<UnitId> upcomingBuffer = new List<UnitId>();

        private CombatEvents events;
        private GridPresenter grid;
        private TacticalCursorController cursor;
        private IExecutionMeter swingMeter;

        private float moveStepDelaySeconds = 0.15f;

        private CombatUnit activeUnit;
        private CombatUnit currentAttackTarget;
        private AbilitySpec currentAbility;
        private bool isMoving;
        private bool isAwaitingSwingStop;
        private bool isCombatOver;
        private bool started;

        // Per-turn (per-unit, M2) flags.
        private bool hasMovedThisTurn;
        private bool hasAttackedThisTurn;

        // How far back in the upcoming-turns list to broadcast.
        private const int UpcomingTurnsToShow = 6;

        public void Initialize(
            CombatEvents combatEvents,
            GridPresenter gridPresenter,
            TacticalCursorController tacticalCursor,
            IExecutionMeter executionMeter,
            float moveStepDelay)
        {
            events = combatEvents;
            grid = gridPresenter;
            cursor = tacticalCursor;
            swingMeter = executionMeter;
            moveStepDelaySeconds = moveStepDelay;

            BuildSquads();

            events.RaiseGridBuilt(new GridBuiltEvent(grid.Bounds));

            started = true;
            ResetSliceState();
        }

        // 2v2 squads with four distinct elements so a single fight surfaces
        // multiple matchups (Light↔Dark mutual STRONG, Fire↔Water STRONG one
        // direction, plus the neutral cross-pairs). M2 slice 2: each unit now
        // carries an ability list (index 0 = the free basic attack) and MP.
        // Players get a second ability that costs MP and is graded harder
        // (the risk/reward gradient); one player unit's is a Support heal.
        // Spawn data stays hardcoded here; ScriptableObjects come later.
        private void BuildSquads()
        {
            allUnits.Clear();

            CombatStats lightFast = new CombatStats(
                maxHP: 90, maxMP: 20,
                attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70,
                speed: 14);
            CombatStats fireSturdy = new CombatStats(
                maxHP: 110, maxMP: 24,
                attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90,
                speed: 8);
            CombatStats darkFast = new CombatStats(
                maxHP: 90, maxMP: 20,
                attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70,
                speed: 14);
            CombatStats waterSturdy = new CombatStats(
                maxHP: 110, maxMP: 24,
                attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90,
                speed: 8);

            allUnits.Add(MakeUnit(1, Faction.Player, new GridCoord(1, 1), lightFast, ElementType.Light, new List<AbilitySpec>
            {
                new AbilitySpec("Radiant Cleave", AbilityKind.Physical, ElementType.Light, power: 10, scaling: 1.0f),
                new AbilitySpec("Searing Lance", AbilityKind.Special, ElementType.Light, power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard),
            }));
            allUnits.Add(MakeUnit(2, Faction.Player, new GridCoord(1, 3), fireSturdy, ElementType.Fire, new List<AbilitySpec>
            {
                new AbilitySpec("Ember Strike", AbilityKind.Physical, ElementType.Fire, power: 10, scaling: 1.0f),
                new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
            }));

            allUnits.Add(MakeUnit(3, Faction.Enemy, new GridCoord(6, 6), darkFast, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Strike", AbilityKind.Physical, ElementType.Dark, power: 10, scaling: 1.0f),
            }));
            allUnits.Add(MakeUnit(4, Faction.Enemy, new GridCoord(6, 4), waterSturdy, ElementType.Water, new List<AbilitySpec>
            {
                new AbilitySpec("Tidal Slash", AbilityKind.Physical, ElementType.Water, power: 10, scaling: 1.0f),
            }));
        }

        private static CombatUnit MakeUnit(int id, Faction faction, GridCoord coord, CombatStats stats, ElementType element, List<AbilitySpec> abilities)
        {
            CombatUnit unit = new CombatUnit
            {
                Id = new UnitId(id),
                Faction = faction,
                Coord = coord,
                SpawnCoord = coord,
                Stats = stats,
                Element = element,
                Abilities = abilities,
                SelectedAbilityIndex = 0,
                MoveRange = 4
            };
            unit.Hp = unit.MaxHp;
            unit.Mp = unit.MaxMp;
            return unit;
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

            if (isCombatOver)
            {
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

            // End Turn is per-unit now: the active unit's turn ends and the
            // next unit in Speed order acts.
            if (IsPlayerTurn && GameInput.Current.EndTurnPressed)
            {
                EndUnitTurn();
                return;
            }

            if (IsPlayerTurn && GameInput.Current.CycleAbilityPressed)
            {
                CycleSelectedAbility();
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

        private bool IsPlayerTurn => activeUnit != null && activeUnit.Faction == Faction.Player;

        private void ResetSliceState()
        {
            isMoving = false;
            isAwaitingSwingStop = false;
            isCombatOver = false;
            currentAttackTarget = null;
            swingMeter?.Cancel();

            foreach (CombatUnit unit in allUnits)
            {
                unit.Coord = unit.SpawnCoord;
                unit.Hp = unit.MaxHp;
                unit.Mp = unit.MaxMp;
                unit.SelectedAbilityIndex = 0;
            }

            events.RaiseCombatReset();
            foreach (CombatUnit unit in allUnits)
            {
                events.RaiseUnitSpawned(new UnitSpawnedEvent(unit.Id, unit.Faction, unit.Element, unit.Coord, unit.Hp, unit.MaxHp));
            }

            StartNewRound();
            BeginUnitTurn();
        }

        private void StartNewRound()
        {
            roundQueue.Clear();
            turnOrderEntriesBuffer.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnit u = allUnits[i];
                turnOrderEntriesBuffer.Add(new TurnOrderEntry(u.Id.Value, u.Stats.Speed, u.IsAlive));
            }

            List<int> order = TurnOrder.ComputeRound(turnOrderEntriesBuffer);
            for (int i = 0; i < order.Count; i++)
            {
                CombatUnit unit = FindUnitById(order[i]);
                if (unit != null)
                {
                    roundQueue.Enqueue(unit);
                }
            }
        }

        private CombatUnit FindUnitById(int id)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i].Id.Value == id)
                {
                    return allUnits[i];
                }
            }
            return null;
        }

        private CombatUnit GetUnitAt(GridCoord coord)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnit u = allUnits[i];
                if (u.IsAlive && u.Coord == coord)
                {
                    return u;
                }
            }
            return null;
        }

        private void UpdateOccupancy()
        {
            occupancy.Clear();
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnit u = allUnits[i];
                if (u.IsAlive)
                {
                    occupancy.Add(u.Coord);
                }
            }
        }

        // Per-unit turn lifecycle: pull the next living unit from the round
        // queue (rebuilding if empty), then start its turn.
        private void BeginUnitTurn()
        {
            if (isCombatOver)
            {
                return;
            }

            // Find next living unit; skip dead entries (they were alive when
            // the round was built but died before their turn came up).
            while (roundQueue.Count > 0 && !roundQueue.Peek().IsAlive)
            {
                roundQueue.Dequeue();
            }

            if (roundQueue.Count == 0)
            {
                StartNewRound();
                while (roundQueue.Count > 0 && !roundQueue.Peek().IsAlive)
                {
                    roundQueue.Dequeue();
                }

                if (roundQueue.Count == 0)
                {
                    // No living units of either faction — shouldn't normally
                    // happen since CheckWinCondition would have fired first.
                    return;
                }
            }

            activeUnit = roundQueue.Dequeue();
            activeUnit.SelectedAbilityIndex = 0;
            hasMovedThisTurn = false;
            hasAttackedThisTurn = false;
            moveTrail.Clear();

            events.RaiseActiveUnitChanged(activeUnit.Id);
            events.RaiseTurnChanged(activeUnit.Faction);
            RaiseTurnOrderUpdate();

            if (IsPlayerTurn)
            {
                if (cursor != null)
                {
                    cursor.IsLocked = false;
                    cursor.Initialize(activeUnit.Coord);
                }
                RaiseAbilitySelection();
                UpdatePreview();
            }
            else
            {
                if (cursor != null)
                {
                    cursor.IsLocked = true;
                }
                events.RaisePathPreviewChanged(PathPreviewEvent.Cleared);
                events.RaiseDamageEstimateChanged(DamageEstimateEvent.Cleared);
                events.RaiseAbilitySelectionChanged(AbilitySelectionEvent.Cleared);
                StartCoroutine(HandleEnemyTurn());
            }
        }

        private void RaiseTurnOrderUpdate()
        {
            upcomingBuffer.Clear();

            // Active unit first.
            if (activeUnit != null && activeUnit.IsAlive)
            {
                upcomingBuffer.Add(activeUnit.Id);
            }

            // Then the rest of the current round (living only).
            foreach (CombatUnit u in roundQueue)
            {
                if (upcomingBuffer.Count >= UpcomingTurnsToShow) break;
                if (u.IsAlive)
                {
                    upcomingBuffer.Add(u.Id);
                }
            }

            // Fill remaining slots from the start of the *next* round so the
            // strip doesn't shrink at the end of a round.
            if (upcomingBuffer.Count < UpcomingTurnsToShow)
            {
                turnOrderEntriesBuffer.Clear();
                for (int i = 0; i < allUnits.Count; i++)
                {
                    CombatUnit u = allUnits[i];
                    turnOrderEntriesBuffer.Add(new TurnOrderEntry(u.Id.Value, u.Stats.Speed, u.IsAlive));
                }
                List<int> nextRound = TurnOrder.ComputeRound(turnOrderEntriesBuffer);
                for (int i = 0; i < nextRound.Count; i++)
                {
                    if (upcomingBuffer.Count >= UpcomingTurnsToShow) break;
                    CombatUnit u = FindUnitById(nextRound[i]);
                    if (u != null) upcomingBuffer.Add(u.Id);
                }
            }

            events.RaiseTurnOrderChanged(upcomingBuffer);
        }

        private void EndUnitTurn()
        {
            if (isCombatOver)
            {
                return;
            }

            BeginUnitTurn();
        }

        private void CycleSelectedAbility()
        {
            if (activeUnit == null || activeUnit.Abilities.Count <= 1)
            {
                return;
            }

            activeUnit.SelectedAbilityIndex =
                (activeUnit.SelectedAbilityIndex + 1) % activeUnit.Abilities.Count;
            RaiseAbilitySelection();
            UpdatePreview();
        }

        private void RaiseAbilitySelection()
        {
            if (activeUnit == null || !IsPlayerTurn)
            {
                events.RaiseAbilitySelectionChanged(AbilitySelectionEvent.Cleared);
                return;
            }

            AbilitySpec a = activeUnit.SelectedAbility;
            bool canAfford = activeUnit.Mp >= a.MpCost;
            events.RaiseAbilitySelectionChanged(new AbilitySelectionEvent(
                a.Name, a.Kind, a.MpCost, activeUnit.Mp, activeUnit.MaxMp,
                QteDisplayName(a.QteType), a.QteDifficulty, canAfford,
                activeUnit.SelectedAbilityIndex, activeUnit.Abilities.Count));
        }

        // Slice 2 wires only the swing meter; the registry that maps each
        // QteType to its own meter arrives with the button-mash type.
        private string QteDisplayName(QteType type)
        {
            return swingMeter != null ? swingMeter.DisplayName : "QTE";
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

            int totalSteps = GridMath.StepCount(currentPath);
            int reachableSteps = ComputeReachableSteps(currentPath);

            IReadOnlyList<GridCoord> renderedPath = currentPath;
            if (hasMovedThisTurn)
            {
                reachableSteps = 0;
                renderedPath = BuildGhostPath();
            }

            events.RaisePathPreviewChanged(new PathPreviewEvent(renderedPath, reachableSteps));
            RaiseSelection(cursor.CursorCoord, totalSteps, reachableSteps);
        }

        private IReadOnlyList<GridCoord> BuildGhostPath()
        {
            if (moveTrail.Count == 0)
            {
                return currentPath;
            }

            ghostPathBuffer.Clear();
            ghostPathBuffer.AddRange(moveTrail);
            for (int i = 1; i < currentPath.Count; i++)
            {
                ghostPathBuffer.Add(currentPath[i]);
            }
            return ghostPathBuffer;
        }

        private int ComputeReachableSteps(IReadOnlyList<GridCoord> path)
        {
            if (path == null || path.Count <= 1)
            {
                return 0;
            }

            int totalSteps = GridMath.StepCount(path);
            int reachable = Mathf.Min(totalSteps, activeUnit.MoveRange);

            GridCoord destination = path[path.Count - 1];
            if (occupancy.IsOccupied(destination) && destination != activeUnit.Coord)
            {
                reachable = Mathf.Min(reachable, totalSteps - 1);
            }

            return Mathf.Max(0, reachable);
        }

        private void RaiseSelection(GridCoord cursorCoord, int totalSteps, int reachableSteps)
        {
            UpdateOccupancy();

            AbilitySpec ability = activeUnit.SelectedAbility;
            bool isSupport = ability.Kind == AbilityKind.Support;
            bool affordable = activeUnit.Mp >= ability.MpCost;

            CombatUnit targetUnit = GetUnitAt(cursorCoord);
            // Support can target the caster's own tile (self-heal).
            if (isSupport && cursorCoord == activeUnit.Coord)
            {
                targetUnit = activeUnit;
            }

            bool occupiedByOther = occupancy.IsOccupied(cursorCoord) && cursorCoord != activeUnit.Coord;
            bool reachable = reachableSteps > 0 && !hasMovedThisTurn;

            bool wantsTarget = false;
            if (targetUnit != null && targetUnit.IsAlive)
            {
                int dist = GridMath.ManhattanDistance(activeUnit.Coord, targetUnit.Coord);
                wantsTarget = isSupport
                    ? (targetUnit.Faction == activeUnit.Faction && dist <= 1)
                    : (targetUnit.Faction != activeUnit.Faction && dist == 1);
            }
            bool validAbilityTarget = wantsTarget && affordable && !hasAttackedThisTurn;

            SelectionState state;
            string hint;
            if (validAbilityTarget)
            {
                state = SelectionState.AttackValid;
                hint = isSupport ? "Confirm: Heal" : "Confirm: Attack";
            }
            else if (wantsTarget && !hasAttackedThisTurn && !affordable)
            {
                state = SelectionState.Invalid;
                hint = $"Not enough MP (need {ability.MpCost})";
            }
            else if (reachable && !occupiedByOther)
            {
                state = SelectionState.MoveValid;
                hint = totalSteps > reachableSteps
                    ? $"Confirm: Move ({reachableSteps}/{totalSteps} tiles)"
                    : "Confirm: Move";
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
            RaiseActionEstimate(state, targetUnit, ability);
        }

        private void RaiseActionEstimate(SelectionState state, CombatUnit targetUnit, AbilitySpec ability)
        {
            if (state != SelectionState.AttackValid || targetUnit == null)
            {
                events.RaiseDamageEstimateChanged(DamageEstimateEvent.Cleared);
                return;
            }

            string qteName = QteDisplayName(ability.QteType);

            if (ability.Kind == AbilityKind.Support)
            {
                HealBreakdown hitHeal = HealCalculator.ComputeHeal(activeUnit.Stats, ability, ExecutionResult.Hit);
                HealBreakdown divineHeal = HealCalculator.ComputeHeal(activeUnit.Stats, ability, ExecutionResult.Divine);
                events.RaiseDamageEstimateChanged(new DamageEstimateEvent(
                    targetUnit.Id,
                    hitHeal.FinalHeal,
                    divineHeal.FinalHeal,
                    ability.Element,
                    targetUnit.Element,
                    1.0f,
                    ability.Name,
                    qteName,
                    isHeal: true));
                return;
            }

            DamageBreakdown hit = DamageCalculator.ComputeDamage(
                activeUnit.Stats, activeUnit.Element,
                targetUnit.Stats, targetUnit.Element,
                ability, ExecutionResult.Hit);

            DamageBreakdown divine = DamageCalculator.ComputeDamage(
                activeUnit.Stats, activeUnit.Element,
                targetUnit.Stats, targetUnit.Element,
                ability, ExecutionResult.Divine);

            events.RaiseDamageEstimateChanged(new DamageEstimateEvent(
                targetUnit.Id,
                hit.FinalDamage,
                divine.FinalDamage,
                ability.Element,
                targetUnit.Element,
                hit.ElementMultiplier,
                ability.Name,
                qteName));
        }

        private void HandleConfirm()
        {
            if (activeUnit == null || cursor == null)
            {
                return;
            }

            GridCoord target = cursor.CursorCoord;
            AbilitySpec ability = activeUnit.SelectedAbility;
            bool isSupport = ability.Kind == AbilityKind.Support;
            bool affordable = activeUnit.Mp >= ability.MpCost;

            CombatUnit targetUnit = GetUnitAt(target);
            if (isSupport && target == activeUnit.Coord)
            {
                targetUnit = activeUnit;
            }

            bool validTarget = false;
            if (!hasAttackedThisTurn && affordable && targetUnit != null && targetUnit.IsAlive)
            {
                int dist = GridMath.ManhattanDistance(activeUnit.Coord, targetUnit.Coord);
                validTarget = isSupport
                    ? (targetUnit.Faction == activeUnit.Faction && dist <= 1)
                    : (targetUnit.Faction != activeUnit.Faction && dist == 1);
            }

            if (validTarget)
            {
                currentAttackTarget = targetUnit;
                currentAbility = ability;
                BeginAbilityExecution();
                return;
            }

            if (hasMovedThisTurn)
            {
                return;
            }

            UpdateOccupancy();
            if (currentPath.Count <= 1)
            {
                return;
            }

            int reachableSteps = ComputeReachableSteps(currentPath);
            if (reachableSteps <= 0)
            {
                return;
            }

            int reachablePoints = reachableSteps + 1;
            List<GridCoord> truncated = new List<GridCoord>(reachablePoints);
            for (int i = 0; i < reachablePoints; i++)
            {
                truncated.Add(currentPath[i]);
            }

            BeginMove(truncated, () => OnPlayerMoveComplete(truncated));
        }

        private void BeginAbilityExecution()
        {
            // Spend MP up front: committing to the action costs MP regardless
            // of how the QTE grades (a whiff wastes it — a real risk).
            activeUnit.Mp = Mathf.Max(0, activeUnit.Mp - currentAbility.MpCost);
            RaiseAbilitySelection();

            if (swingMeter == null || !swingMeter.IsAvailable)
            {
                ResolveAbility(activeUnit, currentAttackTarget, currentAbility, ExecutionResult.Hit);
                currentAttackTarget = null;
                currentAbility = null;
                hasAttackedThisTurn = true;
                EndUnitTurn();
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

            events.RaisePathPreviewChanged(PathPreviewEvent.Cleared);
            events.RaiseDamageEstimateChanged(DamageEstimateEvent.Cleared);
            swingMeter.Begin(currentAbility.QteDifficulty);
            string stopHint = currentAbility.Kind == AbilityKind.Support ? "Confirm: Stop (Heal)" : "Confirm: Stop Swing";
            events.RaiseSelectionChanged(new SelectionChangedEvent(cursor.CursorCoord, SelectionState.Neutral, stopHint));
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

            ResolveAbility(activeUnit, currentAttackTarget, currentAbility, tier);
            currentAttackTarget = null;
            currentAbility = null;
            hasAttackedThisTurn = true;

            if (cursor != null)
            {
                cursor.IsLocked = false;
            }

            EndUnitTurn();
        }

        private void BeginMove(List<GridCoord> path, Action onComplete)
        {
            if (path.Count <= 1)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(MoveAlongPath(new List<GridCoord>(path), onComplete));
        }

        private IEnumerator MoveAlongPath(List<GridCoord> path, Action onComplete)
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

            onComplete?.Invoke();
        }

        private void OnPlayerMoveComplete(IReadOnlyList<GridCoord> walkedPath)
        {
            hasMovedThisTurn = true;
            moveTrail.Clear();
            if (walkedPath != null)
            {
                for (int i = 0; i < walkedPath.Count; i++)
                {
                    moveTrail.Add(walkedPath[i]);
                }
            }
            UpdatePreview();
        }

        private void ResolveAbility(CombatUnit attacker, CombatUnit target, AbilitySpec ability, ExecutionResult execution)
        {
            if (attacker == null || target == null || ability == null)
            {
                return;
            }

            if (ability.Kind == AbilityKind.Support)
            {
                HealBreakdown heal = HealCalculator.ComputeHeal(attacker.Stats, ability, execution);
                int before = target.Hp;
                target.Hp = Mathf.Min(target.MaxHp, target.Hp + heal.FinalHeal);
                int applied = target.Hp - before;
                events.RaiseUnitHealed(new UnitHealedEvent(target.Id, applied, target.Hp, target.MaxHp));
                return;
            }

            DamageBreakdown bd = DamageCalculator.ComputeDamage(
                attacker.Stats, attacker.Element,
                target.Stats, target.Element,
                ability, execution);

            int damage = bd.FinalDamage;
            target.Hp = Mathf.Max(0, target.Hp - damage);
            events.RaiseUnitDamaged(new UnitDamagedEvent(target.Id, damage, target.Hp, target.MaxHp));

            if (target.Hp <= 0)
            {
                events.RaiseUnitDied(target.Id);
                CheckWinCondition(attacker.Faction);
            }
        }

        private void CheckWinCondition(Faction lastAttackerFaction)
        {
            bool anyPlayerAlive = false;
            bool anyEnemyAlive = false;
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (!allUnits[i].IsAlive) continue;
                if (allUnits[i].Faction == Faction.Player) anyPlayerAlive = true;
                else anyEnemyAlive = true;
            }

            if (anyPlayerAlive && anyEnemyAlive)
            {
                return;
            }

            isCombatOver = true;
            // Winner = whichever faction still has units. Use the last
            // attacker as a fallback if both sides simultaneously emptied
            // (shouldn't happen with current rules but defensive).
            Faction winner;
            if (anyPlayerAlive) winner = Faction.Player;
            else if (anyEnemyAlive) winner = Faction.Enemy;
            else winner = lastAttackerFaction;

            events.RaiseCombatEnded(winner);
        }

        // === Enemy AI (per-unit turn) ===

        private IEnumerator HandleEnemyTurn()
        {
            // Brief pause so player can read the turn transition.
            yield return new WaitForSeconds(0.25f);

            if (isCombatOver)
            {
                yield break;
            }

            // Find nearest living player unit (Manhattan distance).
            CombatUnit nearestPlayer = FindNearestEnemy(activeUnit);
            if (nearestPlayer == null)
            {
                EndUnitTurn();
                yield break;
            }

            int distance = GridMath.ManhattanDistance(activeUnit.Coord, nearestPlayer.Coord);
            if (distance == 1)
            {
                currentAttackTarget = nearestPlayer;
                ResolveEnemyAttackAndEnd();
                yield break;
            }

            UpdateOccupancy();
            List<GridCoord> approachPath = GridPathfinderBfs.FindPath(
                activeUnit.Coord,
                nearestPlayer.Coord,
                occupancy,
                grid.Bounds);

            int reachableSteps = ComputeReachableSteps(approachPath);
            if (reachableSteps <= 0)
            {
                EndUnitTurn();
                yield break;
            }

            int points = reachableSteps + 1;
            List<GridCoord> moveSegment = new List<GridCoord>(points);
            for (int i = 0; i < points; i++)
            {
                moveSegment.Add(approachPath[i]);
            }

            // Capture the target so OnEnemyMoveComplete knows who to attack
            // if it's adjacent after the move.
            CombatUnit intendedTarget = nearestPlayer;
            BeginMove(moveSegment, () => OnEnemyMoveComplete(intendedTarget));
        }

        private CombatUnit FindNearestEnemy(CombatUnit unit)
        {
            CombatUnit nearest = null;
            int nearestDistance = int.MaxValue;
            for (int i = 0; i < allUnits.Count; i++)
            {
                CombatUnit candidate = allUnits[i];
                if (!candidate.IsAlive) continue;
                if (candidate.Faction == unit.Faction) continue;
                int d = GridMath.ManhattanDistance(unit.Coord, candidate.Coord);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        private void OnEnemyMoveComplete(CombatUnit intendedTarget)
        {
            hasMovedThisTurn = true;

            if (intendedTarget != null && intendedTarget.IsAlive
                && GridMath.ManhattanDistance(activeUnit.Coord, intendedTarget.Coord) == 1)
            {
                currentAttackTarget = intendedTarget;
                ResolveEnemyAttackAndEnd();
                return;
            }

            EndUnitTurn();
        }

        private void ResolveEnemyAttackAndEnd()
        {
            ResolveAbility(activeUnit, currentAttackTarget, activeUnit.BasicAttack, ExecutionResult.Hit);
            currentAttackTarget = null;
            hasAttackedThisTurn = true;
            EndUnitTurn();
        }
    }
}
