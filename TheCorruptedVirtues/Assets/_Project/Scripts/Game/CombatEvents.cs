using System;
using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // The one-way channel from combat logic to the presentation layer. The
    // orchestrator only ever calls Raise*; presenters only ever subscribe.
    // Nothing in logic touches a GameObject — that is the asset-agnostic seam.
    public sealed class CombatEvents
    {
        public event Action<GridBuiltEvent> GridBuilt;
        public event Action<UnitSpawnedEvent> UnitSpawned;
        public event Action<UnitMovedEvent> UnitMoved;
        public event Action<UnitDamagedEvent> UnitDamaged;
        public event Action<UnitId> UnitDied;
        public event Action<Faction> TurnChanged;
        // ActiveUnitChanged fires every time the active unit changes (M2:
        // multiple units interleave per Speed). Separate from TurnChanged so
        // per-unit views can light up an "it's your turn" indicator while
        // faction-level HUD only needs the faction signal.
        public event Action<UnitId> ActiveUnitChanged;
        // TurnOrderChanged carries the upcoming queue of unit ids (active
        // first, then the next units in the current round, then the start of
        // the next round if there's room). UI uses this to draw the strip.
        public event Action<IReadOnlyList<UnitId>> TurnOrderChanged;
        public event Action<SelectionChangedEvent> SelectionChanged;
        public event Action<PathPreviewEvent> PathPreviewChanged;
        public event Action<DamageEstimateEvent> DamageEstimateChanged;
        public event Action<ExecutionGradedEvent> ExecutionGraded;
        public event Action<Faction> CombatEnded;
        public event Action CombatReset;

        public void RaiseGridBuilt(GridBuiltEvent e) => GridBuilt?.Invoke(e);
        public void RaiseUnitSpawned(UnitSpawnedEvent e) => UnitSpawned?.Invoke(e);
        public void RaiseUnitMoved(UnitMovedEvent e) => UnitMoved?.Invoke(e);
        public void RaiseUnitDamaged(UnitDamagedEvent e) => UnitDamaged?.Invoke(e);
        public void RaiseUnitDied(UnitId id) => UnitDied?.Invoke(id);
        public void RaiseTurnChanged(Faction active) => TurnChanged?.Invoke(active);
        public void RaiseActiveUnitChanged(UnitId id) => ActiveUnitChanged?.Invoke(id);
        public void RaiseTurnOrderChanged(IReadOnlyList<UnitId> upcoming) => TurnOrderChanged?.Invoke(upcoming);
        public void RaiseSelectionChanged(SelectionChangedEvent e) => SelectionChanged?.Invoke(e);
        public void RaisePathPreviewChanged(PathPreviewEvent e) => PathPreviewChanged?.Invoke(e);
        public void RaiseDamageEstimateChanged(DamageEstimateEvent e) => DamageEstimateChanged?.Invoke(e);
        public void RaiseExecutionGraded(ExecutionGradedEvent e) => ExecutionGraded?.Invoke(e);
        public void RaiseCombatEnded(Faction winner) => CombatEnded?.Invoke(winner);
        public void RaiseCombatReset() => CombatReset?.Invoke();
    }

    public readonly struct GridBuiltEvent
    {
        public readonly GridBounds Bounds;

        public GridBuiltEvent(GridBounds bounds)
        {
            Bounds = bounds;
        }
    }

    public readonly struct UnitSpawnedEvent
    {
        public readonly UnitId Id;
        public readonly Faction Faction;
        public readonly ElementType Element;
        public readonly GridCoord Coord;
        public readonly int Hp;
        public readonly int MaxHp;

        public UnitSpawnedEvent(UnitId id, Faction faction, ElementType element, GridCoord coord, int hp, int maxHp)
        {
            Id = id;
            Faction = faction;
            Element = element;
            Coord = coord;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    public readonly struct UnitMovedEvent
    {
        public readonly UnitId Id;
        public readonly GridCoord Coord;

        public UnitMovedEvent(UnitId id, GridCoord coord)
        {
            Id = id;
            Coord = coord;
        }
    }

    public readonly struct UnitDamagedEvent
    {
        public readonly UnitId Id;
        public readonly int Amount;
        public readonly int Hp;
        public readonly int MaxHp;

        public UnitDamagedEvent(UnitId id, int amount, int hp, int maxHp)
        {
            Id = id;
            Amount = amount;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    // Carries the full path plus the count of *step edges* that are within
    // the active unit's MoveRange. The view splits the line into a bright
    // in-range segment and a faded out-of-range continuation so the player
    // can target far and see exactly where the move will truncate.
    public readonly struct PathPreviewEvent
    {
        public readonly IReadOnlyList<GridCoord> Path;
        public readonly int ReachableSteps;

        public PathPreviewEvent(IReadOnlyList<GridCoord> path, int reachableSteps)
        {
            Path = path;
            ReachableSteps = reachableSteps;
        }

        public static PathPreviewEvent Cleared => new PathPreviewEvent(System.Array.Empty<GridCoord>(), 0);
    }

    public readonly struct SelectionChangedEvent
    {
        public readonly GridCoord Cursor;
        public readonly SelectionState State;
        public readonly string Hint;

        public SelectionChangedEvent(GridCoord cursor, SelectionState state, string hint)
        {
            Cursor = cursor;
            State = state;
            Hint = hint;
        }
    }

    public readonly struct ExecutionGradedEvent
    {
        public readonly ExecutionResult Tier;
        public readonly float Multiplier;

        public ExecutionGradedEvent(ExecutionResult tier, float multiplier)
        {
            Tier = tier;
            Multiplier = multiplier;
        }
    }

    // Gladius-style damage forecast surfaced when the cursor is on a valid
    // attack target. HasEstimate=false means "no attack hovered, clear the
    // readout"; the default(struct) value is the cleared form by design.
    // TargetId lets per-unit views (e.g. an HP-bar damage overlay) respond.
    public readonly struct DamageEstimateEvent
    {
        public readonly bool HasEstimate;
        public readonly UnitId TargetId;
        public readonly int HitDamage;
        public readonly int DivineDamage;
        public readonly ElementType AttackerElement;
        public readonly ElementType DefenderElement;
        public readonly float ElementMultiplier;
        public readonly string AttackName;
        public readonly string QteName;

        public DamageEstimateEvent(
            UnitId targetId,
            int hitDamage,
            int divineDamage,
            ElementType attackerElement,
            ElementType defenderElement,
            float elementMultiplier,
            string attackName,
            string qteName)
        {
            HasEstimate = true;
            TargetId = targetId;
            HitDamage = hitDamage;
            DivineDamage = divineDamage;
            AttackerElement = attackerElement;
            DefenderElement = defenderElement;
            ElementMultiplier = elementMultiplier;
            AttackName = attackName;
            QteName = qteName;
        }

        public static DamageEstimateEvent Cleared => default;
    }
}
