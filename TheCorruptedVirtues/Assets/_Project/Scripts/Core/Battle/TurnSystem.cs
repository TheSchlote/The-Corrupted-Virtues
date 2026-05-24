using System.Collections.Generic;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Owns the per-round turn queue and walks one unit's turn at a time in
    // Speed order (delegated to TurnOrder; lower Id breaks ties, dead units
    // excluded). When the round empties it builds the next one. Pure C#: the
    // Unity layer calls AdvanceToNextLivingUnit() and announces the active
    // unit / upcoming strip through CombatEvents.
    //
    // Lifted out of CombatSliceOrchestrator (was the roundQueue field +
    // StartNewRound / the queue-walking half of BeginUnitTurn /
    // RaiseTurnOrderUpdate's list building).
    public sealed class TurnSystem
    {
        private readonly BattleState state;
        private readonly Queue<CombatUnit> roundQueue = new Queue<CombatUnit>();
        private readonly List<TurnOrderEntry> entriesBuffer = new List<TurnOrderEntry>();
        private readonly List<int> orderBuffer = new List<int>();

        public TurnSystem(BattleState state)
        {
            this.state = state;
        }

        // The unit whose turn it currently is (null before the first advance).
        public CombatUnit Active { get; private set; }

        public void Reset()
        {
            roundQueue.Clear();
            Active = null;
        }

        public void StartNewRound()
        {
            roundQueue.Clear();
            ComputeOrder(orderBuffer);
            for (int i = 0; i < orderBuffer.Count; i++)
            {
                CombatUnit unit = state.FindById(new UnitId(orderBuffer[i]));
                if (unit != null)
                {
                    roundQueue.Enqueue(unit);
                }
            }
        }

        // Advance to the next living unit, starting a fresh round if the
        // current one is exhausted. Dead entries are skipped (a unit can die
        // after the round is built but before its turn). Returns null only
        // when no unit of either faction is alive (combat should already be
        // over by then).
        public CombatUnit AdvanceToNextLivingUnit()
        {
            SkipDeadFront();

            if (roundQueue.Count == 0)
            {
                StartNewRound();
                SkipDeadFront();

                if (roundQueue.Count == 0)
                {
                    Active = null;
                    return null;
                }
            }

            Active = roundQueue.Dequeue();
            return Active;
        }

        // Active first, then the rest of this round (living only), then the
        // head of the next round so the strip never shrinks at a round
        // boundary. Writes up to `count` ids into `into`.
        public void BuildUpcoming(int count, List<UnitId> into)
        {
            into.Clear();

            if (Active != null && Active.IsAlive)
            {
                into.Add(Active.Id);
            }

            foreach (CombatUnit u in roundQueue)
            {
                if (into.Count >= count) break;
                if (u.IsAlive) into.Add(u.Id);
            }

            if (into.Count < count)
            {
                ComputeOrder(orderBuffer);
                for (int i = 0; i < orderBuffer.Count; i++)
                {
                    if (into.Count >= count) break;
                    CombatUnit u = state.FindById(new UnitId(orderBuffer[i]));
                    if (u != null) into.Add(u.Id);
                }
            }
        }

        private void SkipDeadFront()
        {
            while (roundQueue.Count > 0 && !roundQueue.Peek().IsAlive)
            {
                roundQueue.Dequeue();
            }
        }

        // Speed-ordered living unit ids for the current roster (TurnOrder
        // already excludes the dead and is deterministic on ties).
        private void ComputeOrder(List<int> into)
        {
            entriesBuffer.Clear();
            IReadOnlyList<CombatUnit> units = state.Units;
            for (int i = 0; i < units.Count; i++)
            {
                CombatUnit u = units[i];
                entriesBuffer.Add(new TurnOrderEntry(u.Id.Value, u.Stats.Speed, u.IsAlive));
            }

            List<int> order = TurnOrder.ComputeRound(entriesBuffer);
            into.Clear();
            into.AddRange(order);
        }
    }
}
