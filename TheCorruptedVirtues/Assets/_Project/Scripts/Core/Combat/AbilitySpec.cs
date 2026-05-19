namespace TheCorruptedVirtues.Combat
{
    // Immutable definition for a combat ability.
    public sealed class AbilitySpec
    {
        public string Name { get; }
        public AbilityKind Kind { get; }
        public ElementType Element { get; }
        public int Power { get; }
        public float Scaling { get; }

        public AbilitySpec(string name, AbilityKind kind, ElementType element, int power, float scaling)
        {
            Name = name;
            Kind = kind;
            Element = element;
            Power = power;
            Scaling = scaling;
        }
    }
}
