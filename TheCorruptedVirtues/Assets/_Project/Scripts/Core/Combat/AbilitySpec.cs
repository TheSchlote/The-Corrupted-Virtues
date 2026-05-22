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

        // M2 slice 2: abilities cost MP and name which QTE (and how hard) the
        // attacker must clear to land them. A 0-cost SwingMeter/Normal ability
        // is the M1.5 "basic attack" — see the short constructor below.
        public int MpCost { get; }
        public QteType QteType { get; }
        public QteDifficulty QteDifficulty { get; }

        // Basic-attack shorthand: free, swing meter, normal difficulty. Keeps
        // the M1.5 call sites and characterization tests unchanged.
        public AbilitySpec(string name, AbilityKind kind, ElementType element, int power, float scaling)
            : this(name, kind, element, power, scaling, mpCost: 0, qteType: QteType.SwingMeter, qteDifficulty: QteDifficulty.Normal)
        {
        }

        public AbilitySpec(
            string name,
            AbilityKind kind,
            ElementType element,
            int power,
            float scaling,
            int mpCost,
            QteType qteType,
            QteDifficulty qteDifficulty)
        {
            Name = name;
            Kind = kind;
            Element = element;
            Power = power;
            Scaling = scaling;
            MpCost = mpCost;
            QteType = qteType;
            QteDifficulty = qteDifficulty;
        }
    }
}
