using System;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Opaque handle to a logical combat unit. Events carry this instead of a
    // GameObject/MonoBehaviour so logic never reaches into the view layer.
    public readonly struct UnitId : IEquatable<UnitId>
    {
        public readonly int Value;

        public UnitId(int value)
        {
            Value = value;
        }

        // Typed Equals so UnitId-keyed dictionaries/sets (turn-order chips, view
        // lookups) compare without boxing through object.Equals.
        public bool Equals(UnitId other)
        {
            return other.Value == Value;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"Unit#{Value}";
        }
    }
}
