namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Opaque handle to a logical combat unit. Events carry this instead of a
    // GameObject/MonoBehaviour so logic never reaches into the view layer.
    public readonly struct UnitId
    {
        public readonly int Value;

        public UnitId(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitId other && other.Value == Value;
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
