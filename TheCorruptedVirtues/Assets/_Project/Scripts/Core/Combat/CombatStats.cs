namespace TheCorruptedVirtues.Combat
{
    // Base combat stats for a unit — the Digimon-Survive block (HP / MP / ATK /
    // DEF / Sp.ATK / Sp.DEF / SPD). Each feeds the combat math in exactly one
    // place; see docs/DESIGN.md "Stat Semantics" for the canonical roles:
    //
    //   MaxHP          - health pool; reaching 0 is death (the Great Beast's
    //                    pool is its Corruption gauge: depleting it purifies).
    //   MaxMP          - ability resource; spent on commit, no passive regen.
    //   Attack/Defense - the Physical-ability attack/mitigation pair.
    //   Special*       - the Special-ability attack/mitigation pair.
    //   Speed          - turn order only (TurnOrder); not a damage term.
    //
    // Support abilities deal no damage; heals scale off Sp.ATK (HealCalculator).
    public readonly struct CombatStats
    {
        public int MaxHP { get; }
        public int MaxMP { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int SpecialAttack { get; }
        public int SpecialDefense { get; }
        public int Speed { get; }

        public CombatStats(
            int maxHP,
            int maxMP,
            int attack,
            int defense,
            int specialAttack,
            int specialDefense,
            int speed)
        {
            MaxHP = maxHP;
            MaxMP = maxMP;
            Attack = attack;
            Defense = defense;
            SpecialAttack = specialAttack;
            SpecialDefense = specialDefense;
            Speed = speed;
        }
    }
}
