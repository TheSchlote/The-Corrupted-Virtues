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
        public event Action<SelectionChangedEvent> SelectionChanged;
        public event Action<IReadOnlyList<GridCoord>> PathPreviewChanged;
        public event Action<ExecutionGradedEvent> ExecutionGraded;
        public event Action CombatReset;

        public void RaiseGridBuilt(GridBuiltEvent e) => GridBuilt?.Invoke(e);
        public void RaiseUnitSpawned(UnitSpawnedEvent e) => UnitSpawned?.Invoke(e);
        public void RaiseUnitMoved(UnitMovedEvent e) => UnitMoved?.Invoke(e);
        public void RaiseUnitDamaged(UnitDamagedEvent e) => UnitDamaged?.Invoke(e);
        public void RaiseUnitDied(UnitId id) => UnitDied?.Invoke(id);
        public void RaiseTurnChanged(Faction active) => TurnChanged?.Invoke(active);
        public void RaiseSelectionChanged(SelectionChangedEvent e) => SelectionChanged?.Invoke(e);
        public void RaisePathPreviewChanged(IReadOnlyList<GridCoord> path) => PathPreviewChanged?.Invoke(path);
        public void RaiseExecutionGraded(ExecutionGradedEvent e) => ExecutionGraded?.Invoke(e);
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
        public readonly GridCoord Coord;
        public readonly int Hp;
        public readonly int MaxHp;

        public UnitSpawnedEvent(UnitId id, Faction faction, GridCoord coord, int hp, int maxHp)
        {
            Id = id;
            Faction = faction;
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
}
