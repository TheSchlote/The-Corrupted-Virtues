using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // The mutable logical state of one combatant: where it stands, what it can
    // do, and how hurt it is. Pure C# — no UnityEngine, no view concerns. The
    // Unity layer reads/writes these fields and announces the changes through
    // CombatEvents; it never stores gameplay state of its own.
    //
    // Was a private nested class inside CombatSliceOrchestrator; lifted out so
    // the pure systems (BattleState / TurnSystem / EnemyTurnPlanner /
    // AbilityResolver) can all share one model and be unit-tested.
    public sealed class CombatUnit
    {
        public UnitId Id;
        public Faction Faction;
        public GridCoord Coord;
        public GridCoord SpawnCoord;
        public Facing Facing;
        public Facing SpawnFacing;
        // Tiles occupied, anchored at Coord. 1x1 for normal units; the 2x2
        // Boss bosses set this larger. Defaults to Single so existing
        // single-tile units need no change.
        public GridFootprint Footprint = GridFootprint.Single;
        // The corrupted-Virtue boss: a multi-tile unit whose HP pool reads as a
        // Corruption gauge and whose defeat is a "purify", not a kill.
        public bool IsBoss;
        public int Hp;
        public int Mp;
        public CombatStats Stats;
        public ElementType Element;
        public List<AbilitySpec> Abilities;
        public int SelectedAbilityIndex;
        public int MoveRange;
        // Per-unit AI policy (EnemyTurnPlanner dispatches on it). Defaults to
        // Aggressive — the shipped routine — so units built without setting it
        // behave exactly as before.
        public AiBehavior AiBehavior;

        public int MaxHp => Stats.MaxHP;
        public int MaxMp => Stats.MaxMP;
        public bool IsAlive => Hp > 0;
        public AbilitySpec BasicAttack => Abilities[0];
        public AbilitySpec SelectedAbility => Abilities[SelectedAbilityIndex];
    }
}
