using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The combat loop's Unity-side conductor. It no longer owns the rules: the
    // pure-C# Battle systems decide them — BattleState (roster / occupancy /
    // win), TurnSystem (round order), EnemyTurnPlanner (AI), AbilityResolver
    // (damage / heal), MovementRules (reach). This class drives input, runs the
    // move and QTE coroutines, moves the cursor, and translates every state
    // change into CombatEvents. As before, it never touches a GameObject,
    // transform, renderer or UI element — presenters do that.
    //
    // M2: squads + Speed-based interleaved turn order; one unit acts per turn.
    // Combat ends when either faction has no living units.
    public sealed class CombatSliceOrchestrator : MonoBehaviour
    {
        private readonly List<GridCoord> currentPath = new List<GridCoord>();
        private readonly List<GridCoord> moveTrail = new List<GridCoord>();
        private readonly List<GridCoord> ghostPathBuffer = new List<GridCoord>();
        private readonly List<UnitId> upcomingBuffer = new List<UnitId>();

        // Pure-C# combat state + turn queue. This MonoBehaviour mutates them
        // and announces the results; it keeps no gameplay rules of its own.
        private BattleState battle;
        private TurnSystem turns;

        private CombatEvents events;
        private GridPresenter grid;
        private ElevationMap elevation;
        // M2: this branch loads the Great Beast boss fight (2 players vs one 2x2
        // boss) instead of the 2v2 squad fight. Flip to playtest the squads.
        [SerializeField] private bool greatBeastEncounter = true;
        private TacticalCursorController cursor;
        private readonly Dictionary<QteType, IExecutionMeter> meters = new Dictionary<QteType, IExecutionMeter>();
        private IExecutionMeter currentMeter;

        private float moveStepDelaySeconds = 0.15f;

        private CombatUnit activeUnit;
        private CombatUnit currentAttackTarget;
        // The tile an area attack bursts from (the cursor tile at confirm). The
        // forecast highlights and the resolve both centre here, so what lit up
        // is exactly what gets hit. Unused for single-target abilities.
        private GridCoord currentAttackCenter;
        private AbilitySpec currentAbility;
        private bool isMoving;
        private bool isAwaitingQte;
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
            IReadOnlyDictionary<QteType, IExecutionMeter> executionMeters,
            float moveStepDelay)
        {
            events = combatEvents;
            grid = gridPresenter;
            cursor = tacticalCursor;
            meters.Clear();
            if (executionMeters != null)
            {
                foreach (KeyValuePair<QteType, IExecutionMeter> entry in executionMeters)
                {
                    meters[entry.Key] = entry.Value;
                }
            }
            moveStepDelaySeconds = moveStepDelay;

            battle = new BattleState();
            turns = new TurnSystem(battle);

            if (greatBeastEncounter)
            {
                BuildGreatBeastEncounter();
            }
            else
            {
                BuildSquads();
            }
            BuildTerrain();

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
            List<CombatUnit> roster = new List<CombatUnit>();

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

            roster.Add(MakeUnit(1, Faction.Player, new GridCoord(1, 1), lightFast, ElementType.Light, new List<AbilitySpec>
            {
                new AbilitySpec("Radiant Cleave", AbilityKind.Physical, ElementType.Light, power: 10, scaling: 1.0f),
                new AbilitySpec("Searing Lance", AbilityKind.Special, ElementType.Light, power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard),
                new AbilitySpec("Flurry", AbilityKind.Physical, ElementType.Light, power: 7, scaling: 0.8f, mpCost: 8, qteType: QteType.ButtonMash, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Lance of Dawn", AbilityKind.Special, ElementType.Light, power: 26, scaling: 1.3f, mpCost: 12, qteType: QteType.TimedPress, qteDifficulty: QteDifficulty.Hard),
            }));
            roster.Add(MakeUnit(2, Faction.Player, new GridCoord(1, 3), fireSturdy, ElementType.Fire, new List<AbilitySpec>
            {
                new AbilitySpec("Ember Strike", AbilityKind.Physical, ElementType.Fire, power: 10, scaling: 1.0f),
                new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Cinder Combo", AbilityKind.Physical, ElementType.Fire, power: 8, scaling: 0.7f, mpCost: 10, qteType: QteType.Matching, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Flame Nova", AbilityKind.Special, ElementType.Fire, power: 18, scaling: 1.1f, mpCost: 16, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard, isAreaOfEffect: true, aoeRadius: 1),
            }));

            roster.Add(MakeUnit(3, Faction.Enemy, new GridCoord(6, 6), darkFast, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Strike", AbilityKind.Physical, ElementType.Dark, power: 10, scaling: 1.0f),
            }));
            roster.Add(MakeUnit(4, Faction.Enemy, new GridCoord(6, 4), waterSturdy, ElementType.Water, new List<AbilitySpec>
            {
                new AbilitySpec("Tidal Slash", AbilityKind.Physical, ElementType.Water, power: 10, scaling: 1.0f),
            }));

            battle.SetRoster(roster);
        }

        // M2 Great Beast slice: a boss fight - the same two player units versus
        // one 2x2 corrupted Virtue. Its deep HP pool is the Corruption gauge;
        // depleting it purifies (wins). Hardcoded like the squads; encounter
        // data comes later. Stats/positions are first-pass, tunable in playtest.
        private void BuildGreatBeastEncounter()
        {
            List<CombatUnit> roster = new List<CombatUnit>();

            CombatStats lightFast = new CombatStats(
                maxHP: 90, maxMP: 20, attack: 16, defense: 70,
                specialAttack: 14, specialDefense: 70, speed: 14);
            CombatStats fireSturdy = new CombatStats(
                maxHP: 110, maxMP: 24, attack: 14, defense: 90,
                specialAttack: 16, specialDefense: 90, speed: 8);

            roster.Add(MakeUnit(1, Faction.Player, new GridCoord(1, 1), lightFast, ElementType.Light, new List<AbilitySpec>
            {
                new AbilitySpec("Radiant Cleave", AbilityKind.Physical, ElementType.Light, power: 10, scaling: 1.0f),
                new AbilitySpec("Searing Lance", AbilityKind.Special, ElementType.Light, power: 22, scaling: 1.2f, mpCost: 10, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard),
                new AbilitySpec("Flurry", AbilityKind.Physical, ElementType.Light, power: 7, scaling: 0.8f, mpCost: 8, qteType: QteType.ButtonMash, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Lance of Dawn", AbilityKind.Special, ElementType.Light, power: 26, scaling: 1.3f, mpCost: 12, qteType: QteType.TimedPress, qteDifficulty: QteDifficulty.Hard),
            }));
            roster.Add(MakeUnit(2, Faction.Player, new GridCoord(1, 3), fireSturdy, ElementType.Fire, new List<AbilitySpec>
            {
                new AbilitySpec("Ember Strike", AbilityKind.Physical, ElementType.Fire, power: 10, scaling: 1.0f),
                new AbilitySpec("Mend", AbilityKind.Support, ElementType.Light, power: 24, scaling: 0.6f, mpCost: 12, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Cinder Combo", AbilityKind.Physical, ElementType.Fire, power: 8, scaling: 0.7f, mpCost: 10, qteType: QteType.Matching, qteDifficulty: QteDifficulty.Normal),
                new AbilitySpec("Flame Nova", AbilityKind.Special, ElementType.Fire, power: 18, scaling: 1.1f, mpCost: 16, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Hard, isAreaOfEffect: true, aoeRadius: 1),
            }));

            // The corrupted Virtue: a slow, hard-hitting 2x2 with a deep
            // Corruption pool (its HP). MakeUnit gives it West facing + the
            // standard move range; footprint + boss flag set after.
            CombatStats beastStats = new CombatStats(
                maxHP: 400, maxMP: 0, attack: 22, defense: 80,
                specialAttack: 10, specialDefense: 80, speed: 6);
            CombatUnit beast = MakeUnit(3, Faction.Enemy, new GridCoord(5, 4), beastStats, ElementType.Dark, new List<AbilitySpec>
            {
                new AbilitySpec("Corruption Slam", AbilityKind.Physical, ElementType.Dark, power: 14, scaling: 1.0f),
            });
            beast.Footprint = new GridFootprint(2, 2);
            beast.IsGreatBeast = true;
            roster.Add(beast);

            battle.SetRoster(roster);
        }

        // M2 terrain slice: a small high-ground plateau in the contested mid-
        // field. Both squads start equidistant from it, so taking the high
        // ground is a real opening choice. Level 1 = one step up (a ×1.25 hit
        // against a lower target). Hardcoded like the roster; data-driven later.
        private void BuildTerrain()
        {
            elevation = new ElevationMap();
            GridCoord[] plateau =
            {
                new GridCoord(3, 3), new GridCoord(4, 3),
                new GridCoord(3, 4), new GridCoord(4, 4),
            };
            for (int i = 0; i < plateau.Length; i++)
            {
                elevation.SetLevel(plateau[i], 1);
            }

            grid.SetElevation(elevation);
        }

        private static CombatUnit MakeUnit(int id, Faction faction, GridCoord coord, CombatStats stats, ElementType element, List<AbilitySpec> abilities)
        {
            // Auto-facing starts pointed at the opposing side (players sit at
            // low X, enemies high), so opening shots are frontal until someone
            // maneuvers around a flank.
            Facing facing = faction == Faction.Player ? Facing.East : Facing.West;
            CombatUnit unit = new CombatUnit
            {
                Id = new UnitId(id),
                Faction = faction,
                Coord = coord,
                SpawnCoord = coord,
                Facing = facing,
                SpawnFacing = facing,
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

            if (isAwaitingQte)
            {
                if (currentMeter != null
                    && currentMeter.Tick(GameInput.Current.ConfirmPressed, out ExecutionResult qteTier, out float qteMultiplier))
                {
                    OnQteComplete(qteTier, qteMultiplier);
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
            isAwaitingQte = false;
            isCombatOver = false;
            currentAttackTarget = null;
            currentAbility = null;
            currentMeter = null;
            foreach (IExecutionMeter meter in meters.Values)
            {
                meter?.Cancel();
            }

            foreach (CombatUnit unit in battle.Units)
            {
                unit.Coord = unit.SpawnCoord;
                unit.Facing = unit.SpawnFacing;
                unit.Hp = unit.MaxHp;
                unit.Mp = unit.MaxMp;
                unit.SelectedAbilityIndex = 0;
            }
            battle.RebuildOccupancy();

            events.RaiseCombatReset();
            foreach (CombatUnit unit in battle.Units)
            {
                events.RaiseUnitSpawned(new UnitSpawnedEvent(unit.Id, unit.Faction, unit.Element, unit.Coord, unit.Hp, unit.MaxHp, unit.Footprint, unit.IsGreatBeast));
                events.RaiseUnitFacingChanged(new UnitFacingChangedEvent(unit.Id, unit.Facing));
            }

            turns.Reset();
            turns.StartNewRound();
            BeginUnitTurn();
        }

        // Per-unit turn lifecycle: pull the next living unit from the turn
        // system (it rebuilds the round when empty), then start its turn.
        private void BeginUnitTurn()
        {
            if (isCombatOver)
            {
                return;
            }

            activeUnit = turns.AdvanceToNextLivingUnit();
            if (activeUnit == null)
            {
                // No living units of either faction — shouldn't normally
                // happen since CheckWinCondition would have fired first.
                return;
            }

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
                events.RaiseAreaPreviewChanged(AreaPreviewEvent.Cleared);
                events.RaiseAbilitySelectionChanged(AbilitySelectionEvent.Cleared);
                StartCoroutine(HandleEnemyTurn());
            }
        }

        private void RaiseTurnOrderUpdate()
        {
            turns.BuildUpcoming(UpcomingTurnsToShow, upcomingBuffer);
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

        private string QteDisplayName(QteType type)
        {
            return meters.TryGetValue(type, out IExecutionMeter meter) && meter != null
                ? meter.DisplayName
                : "QTE";
        }

        // The on-screen prompt for the QTE the player is about to perform.
        // Each type drives a different interaction, so each gets its own verb.
        private static string QteHint(AbilitySpec ability)
        {
            switch (ability.QteType)
            {
                case QteType.ButtonMash:
                    return "Mash Confirm!";
                case QteType.TimedPress:
                    return "Confirm: press on the target!";
                case QteType.Matching:
                    return "Repeat the sequence (arrows / D-pad)!";
                default:
                    return ability.Kind == AbilityKind.Support ? "Confirm: Stop (Heal)" : "Confirm: Stop Swing";
            }
        }

        private void UpdatePreview()
        {
            if (activeUnit == null || cursor == null)
            {
                return;
            }

            battle.RebuildOccupancy();
            currentPath.Clear();
            List<GridCoord> path = GridPathfinderBfs.FindPath(
                activeUnit.Coord,
                cursor.CursorCoord,
                battle.Occupancy,
                grid.Bounds);
            currentPath.AddRange(path);

            int totalSteps = GridMath.StepCount(currentPath);
            int reachableSteps = ReachableSteps(currentPath);

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

        // The active unit's reachable step count along a path — MovementRules
        // does the capping; this just supplies the unit's range / position.
        private int ReachableSteps(IReadOnlyList<GridCoord> path)
        {
            return MovementRules.ComputeReachableSteps(
                path, activeUnit.MoveRange, battle.Occupancy, activeUnit.Coord);
        }

        private void RaiseSelection(GridCoord cursorCoord, int totalSteps, int reachableSteps)
        {
            battle.RebuildOccupancy();

            AbilitySpec ability = activeUnit.SelectedAbility;
            bool isSupport = ability.Kind == AbilityKind.Support;
            bool affordable = activeUnit.Mp >= ability.MpCost;

            CombatUnit targetUnit = battle.GetLivingUnitAt(cursorCoord);
            // Support can target the caster's own tile (self-heal).
            if (isSupport && cursorCoord == activeUnit.Coord)
            {
                targetUnit = activeUnit;
            }

            bool occupiedByOther = battle.Occupancy.IsOccupied(cursorCoord) && cursorCoord != activeUnit.Coord;
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
            RaiseActionEstimate(state, cursorCoord, targetUnit, ability);
        }

        private void RaiseActionEstimate(SelectionState state, GridCoord cursorCoord, CombatUnit targetUnit, AbilitySpec ability)
        {
            if (state != SelectionState.AttackValid || targetUnit == null)
            {
                events.RaiseDamageEstimateChanged(DamageEstimateEvent.Cleared);
                events.RaiseAreaPreviewChanged(AreaPreviewEvent.Cleared);
                return;
            }

            // AoE: light the burst tiles centred on the cursor so the player
            // sees the whole area before committing. Single-target clears it.
            if (ability.AoeRadius > 0 && ability.Kind != AbilityKind.Support)
            {
                events.RaiseAreaPreviewChanged(new AreaPreviewEvent(
                    AreaOfEffect.BurstTiles(cursorCoord, ability.AoeRadius, grid.Bounds)));
            }
            else
            {
                events.RaiseAreaPreviewChanged(AreaPreviewEvent.Cleared);
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

            SituationalModifiers mods = CombatSituation.For(activeUnit, targetUnit, elevation);

            DamageBreakdown hit = DamageCalculator.ComputeDamage(
                activeUnit.Stats, activeUnit.Element,
                targetUnit.Stats, targetUnit.Element,
                ability, ExecutionResult.Hit, mods);

            DamageBreakdown divine = DamageCalculator.ComputeDamage(
                activeUnit.Stats, activeUnit.Element,
                targetUnit.Stats, targetUnit.Element,
                ability, ExecutionResult.Divine, mods);

            events.RaiseDamageEstimateChanged(new DamageEstimateEvent(
                targetUnit.Id,
                hit.FinalDamage,
                divine.FinalDamage,
                ability.Element,
                targetUnit.Element,
                hit.ElementMultiplier,
                ability.Name,
                qteName,
                highGroundMultiplier: mods.HighGround,
                flankingMultiplier: mods.Flanking));
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

            CombatUnit targetUnit = battle.GetLivingUnitAt(target);
            if (isSupport && target == activeUnit.Coord)
            {
                targetUnit = activeUnit;
            }

            bool validTarget = false;
            if (!hasAttackedThisTurn && affordable && targetUnit != null && targetUnit.IsAlive)
            {
                int dist = GridMath.ManhattanDistance(activeUnit.Coord, targetUnit.Coord);
                bool adjacent = FootprintAdjacency.AreAdjacent(
                    activeUnit.Footprint, activeUnit.Coord, targetUnit.Footprint, targetUnit.Coord);
                validTarget = isSupport
                    ? (targetUnit.Faction == activeUnit.Faction && dist <= 1)
                    : (targetUnit.Faction != activeUnit.Faction && adjacent);
            }

            if (validTarget)
            {
                currentAttackTarget = targetUnit;
                currentAttackCenter = target;
                currentAbility = ability;
                BeginAbilityExecution();
                return;
            }

            if (hasMovedThisTurn)
            {
                return;
            }

            battle.RebuildOccupancy();
            if (currentPath.Count <= 1)
            {
                return;
            }

            int reachableSteps = ReachableSteps(currentPath);
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

            currentMeter = ResolveMeter(currentAbility.QteType);

            if (currentMeter == null || !currentMeter.IsAvailable)
            {
                ResolveAbility(activeUnit, currentAttackTarget, currentAbility, ExecutionResult.Hit);
                currentAttackTarget = null;
                currentAbility = null;
                currentMeter = null;
                hasAttackedThisTurn = true;
                EndUnitTurn();
                return;
            }

            if (isAwaitingQte)
            {
                return;
            }

            isAwaitingQte = true;
            if (cursor != null)
            {
                cursor.IsLocked = true;
            }

            events.RaisePathPreviewChanged(PathPreviewEvent.Cleared);
            events.RaiseDamageEstimateChanged(DamageEstimateEvent.Cleared);
            events.RaiseAreaPreviewChanged(AreaPreviewEvent.Cleared);
            currentMeter.Begin(currentAbility.QteDifficulty);

            string hint = QteHint(currentAbility);
            events.RaiseSelectionChanged(new SelectionChangedEvent(cursor.CursorCoord, SelectionState.Neutral, hint));
        }

        private void OnQteComplete(ExecutionResult tier, float multiplier)
        {
            isAwaitingQte = false;
            events.RaiseExecutionGraded(new ExecutionGradedEvent(tier, multiplier));

            ResolveAbility(activeUnit, currentAttackTarget, currentAbility, tier);
            currentAttackTarget = null;
            currentAbility = null;
            currentMeter = null;
            hasAttackedThisTurn = true;

            if (cursor != null)
            {
                cursor.IsLocked = false;
            }

            EndUnitTurn();
        }

        // Pick the meter for an ability's QTE type, falling back to any
        // available meter so a missing registration can never softlock a turn.
        private IExecutionMeter ResolveMeter(QteType type)
        {
            if (meters.TryGetValue(type, out IExecutionMeter meter) && meter != null)
            {
                return meter;
            }

            foreach (IExecutionMeter fallback in meters.Values)
            {
                if (fallback != null && fallback.IsAvailable)
                {
                    return fallback;
                }
            }

            return null;
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
                activeUnit.Facing = FacingRules.Toward(path[i - 1], path[i]);
                activeUnit.Coord = path[i];
                events.RaiseUnitMoved(new UnitMovedEvent(activeUnit.Id, activeUnit.Coord));
                events.RaiseUnitFacingChanged(new UnitFacingChangedEvent(activeUnit.Id, activeUnit.Facing));
                battle.RebuildOccupancy();
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

        // Apply an ability through the pure resolver, then announce the result.
        // The HP math + lethality live in AbilityResolver; raising events and
        // checking the win condition stay here (they touch CombatEvents and the
        // combat-over flag).
        private void ResolveAbility(CombatUnit attacker, CombatUnit target, AbilitySpec ability, ExecutionResult execution)
        {
            if (attacker == null || target == null || ability == null)
            {
                return;
            }

            // Area attacks hit everyone in the burst around the targeted tile;
            // resolve them together (one win check) and skip the single-target
            // facing turn (they're non-directional).
            if (ability.AoeRadius > 0 && ability.Kind != AbilityKind.Support)
            {
                ResolveAreaAbility(attacker, currentAttackCenter, ability, execution);
                return;
            }

            SituationalModifiers mods = CombatSituation.For(attacker, target, elevation);

            // Single-target attacks are directional: the attacker turns to face
            // its target and strikes the faced tile (movement sets facing
            // otherwise; there is no standalone turn action). Support heals and
            // AoE attacks don't turn to a single target. Kept out of the flank
            // math above, which reads the *target's* facing.
            if (ability.Kind != AbilityKind.Support && !ability.IsAreaOfEffect)
            {
                attacker.Facing = FacingRules.Toward(attacker.Coord, target.Coord);
                events.RaiseUnitFacingChanged(new UnitFacingChangedEvent(attacker.Id, attacker.Facing));
            }

            AbilityOutcome outcome = AbilityResolver.Resolve(attacker, target, ability, execution, mods);

            if (outcome.IsHeal)
            {
                events.RaiseUnitHealed(new UnitHealedEvent(outcome.TargetId, outcome.Amount, outcome.TargetHp, outcome.TargetMaxHp));
                return;
            }

            events.RaiseUnitDamaged(new UnitDamagedEvent(outcome.TargetId, outcome.Amount, outcome.TargetHp, outcome.TargetMaxHp));

            if (outcome.TargetDied)
            {
                events.RaiseUnitDied(outcome.TargetId);
                CheckWinCondition(attacker.Faction);
            }
        }

        // Resolve one area attack: gather every enemy in the burst, apply the
        // ability to each (AbilityResolver.ResolveArea — high ground but no
        // flanking), announce per target, then check the win once at the end.
        private void ResolveAreaAbility(CombatUnit attacker, GridCoord center, AbilitySpec ability, ExecutionResult execution)
        {
            List<CombatUnit> targets = AreaOfEffect.CollectTargets(center, ability.AoeRadius, attacker.Faction, battle);
            if (targets.Count == 0)
            {
                return;
            }

            IReadOnlyList<AbilityOutcome> outcomes =
                AbilityResolver.ResolveArea(attacker, targets, ability, execution, elevation);

            bool anyDied = false;
            for (int i = 0; i < outcomes.Count; i++)
            {
                AbilityOutcome outcome = outcomes[i];
                events.RaiseUnitDamaged(new UnitDamagedEvent(
                    outcome.TargetId, outcome.Amount, outcome.TargetHp, outcome.TargetMaxHp));
                if (outcome.TargetDied)
                {
                    events.RaiseUnitDied(outcome.TargetId);
                    anyDied = true;
                }
            }

            if (anyDied)
            {
                CheckWinCondition(attacker.Faction);
            }
        }

        private void CheckWinCondition(Faction lastAttackerFaction)
        {
            if (battle.TryGetWinner(lastAttackerFaction, out Faction winner))
            {
                isCombatOver = true;
                events.RaiseCombatEnded(winner);
            }
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

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(activeUnit, battle, grid.Bounds);

            if (plan.Target == null)
            {
                EndUnitTurn();
                yield break;
            }

            // Already adjacent (or otherwise no move): attack in place or end.
            if (!plan.HasMove)
            {
                if (plan.AttackAfterMove)
                {
                    currentAttackTarget = plan.Target;
                    ResolveEnemyAttackAndEnd();
                }
                else
                {
                    EndUnitTurn();
                }
                yield break;
            }

            // Walk the planned segment; OnEnemyMoveComplete re-checks adjacency
            // and attacks if the move landed next to the target.
            CombatUnit intendedTarget = plan.Target;
            List<GridCoord> moveSegment = new List<GridCoord>(plan.MovePath);
            BeginMove(moveSegment, () => OnEnemyMoveComplete(intendedTarget));
        }

        private void OnEnemyMoveComplete(CombatUnit intendedTarget)
        {
            hasMovedThisTurn = true;

            if (intendedTarget != null && intendedTarget.IsAlive
                && FootprintAdjacency.AreAdjacent(activeUnit.Footprint, activeUnit.Coord, intendedTarget.Footprint, intendedTarget.Coord))
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
