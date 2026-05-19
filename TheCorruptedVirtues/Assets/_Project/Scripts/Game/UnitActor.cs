using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Scene unit representation with grid stats.
    public sealed class UnitActor : MonoBehaviour
    {
        public enum Faction
        {
            Player,
            Enemy
        }

        [SerializeField] private Faction faction = Faction.Player;
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int currentHp = 100;
        [SerializeField] private int moveRange = 4;

        public Faction Team => faction;
        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public int MoveRange => moveRange;
        public GridCoord CurrentCoord { get; private set; }
        public GridCoord SpawnCoord { get; private set; }

        public void InitializeCoord(GridCoord coord)
        {
            CurrentCoord = coord;
            SpawnCoord = coord;
        }

        public void SetCoord(GridCoord coord)
        {
            CurrentCoord = coord;
        }

        public void ApplyDamage(int amount)
        {
            currentHp = Mathf.Max(0, currentHp - amount);
        }

        public void ResetHp()
        {
            currentHp = maxHp;
        }
    }
}
