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
        // Chebyshev burst radius for area attacks: every enemy within this many
        // tiles of the targeted tile is hit. 0 = single target.
        public int AoeRadius { get; }

        // An area attack hits a burst rather than one faced tile, so it's
        // non-directional and exempt from the single-target facing/flank rule.
        // Derived from the radius (single source of truth) so the two can never
        // disagree: an ability is AoE iff it has a burst radius.
        public bool IsAreaOfEffect => AoeRadius > 0;

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
            QteDifficulty qteDifficulty,
            int aoeRadius = 0)
        {
            Name = name;
            Kind = kind;
            Element = element;
            Power = power;
            Scaling = scaling;
            MpCost = mpCost;
            QteType = qteType;
            QteDifficulty = qteDifficulty;
            AoeRadius = aoeRadius < 0 ? 0 : aoeRadius;
        }
    }
}
