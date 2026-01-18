namespace TheCorruptedVirtues.Combat
{
    // Base combat stats for a unit.
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
