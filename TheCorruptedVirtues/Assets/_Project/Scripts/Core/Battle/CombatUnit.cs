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
}
