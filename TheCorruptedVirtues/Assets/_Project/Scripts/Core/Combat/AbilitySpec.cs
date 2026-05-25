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
        // Single-target attacks are directional: the attacker turns to face its
        // target and strikes the faced tile. AoE attacks bypass that (they hit
        // an area, not one faced target). Default false = single-target.
        public bool IsAreaOfEffect { get; }
        // Chebyshev burst radius for area attacks: every enemy within this many
        // tiles of the targeted tile is hit. 0 = single target. >0 implies an
        // area attack (and pairs with IsAreaOfEffect for the facing exemption).
        public int AoeRadius { get; }

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
            bool isAreaOfEffect = false,
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
            IsAreaOfEffect = isAreaOfEffect;
            AoeRadius = aoeRadius < 0 ? 0 : aoeRadius;
        }
    }
}
