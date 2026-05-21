using System.Collections.Generic;

namespace TheCorruptedVirtues.Combat
{
    // Pure-C# computation of Speed-based turn order for a round of combat.
    // The orchestrator owns round lifecycle (start round, dequeue, skip dead,
    // start next round); this helper just answers "given these units, what's
    // the order this round?" deterministically.
    public static class TurnOrder
    {
        // Highest Speed first; ties broken by lower Id (deterministic so
        // replays / tests don't shift on tie). Dead units are excluded.
        public static List<int> ComputeRound(IReadOnlyList<TurnOrderEntry> entries)
        {
            List<TurnOrderEntry> alive = new List<TurnOrderEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsAlive)
                {
                    alive.Add(entries[i]);
                }
            }

            // Stable sort by (Speed desc, Id asc). List<T>.Sort is unstable,
            // so we encode the tie-break explicitly in the comparison.
            alive.Sort((a, b) =>
            {
                if (a.Speed != b.Speed)
                {
                    return b.Speed.CompareTo(a.Speed);
                }
                return a.Id.CompareTo(b.Id);
            });

            List<int> order = new List<int>(alive.Count);
            for (int i = 0; i < alive.Count; i++)
            {
                order.Add(alive[i].Id);
            }
            return order;
        }
    }

    public readonly struct TurnOrderEntry
    {
        public readonly int Id;
        public readonly int Speed;
        public readonly bool IsAlive;

        public TurnOrderEntry(int id, int speed, bool isAlive)
        {
            Id = id;
            Speed = speed;
            IsAlive = isAlive;
        }
    }
}
