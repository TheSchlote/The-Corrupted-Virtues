using NUnit.Framework;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Whole-loop integration tests. The pure systems (TurnSystem, EnemyTurnPlanner,
    // AbilityResolver, MovementRules, BattleState) are each unit-tested in
    // isolation; these prove they COMPOSE into a battle that actually progresses
    // from a starting roster to a decided winner. This is the EditMode stand-in
    // for "play a whole encounter" — the integration the MonoBehaviour
    // orchestrator otherwise only exercises under manual play.
    public class BattleSimulatorTests
    {
        // Default 8x8 playfield for the synthetic RunUnitTurn test below. Library
        // encounters now run on their own map's bounds/elevation/obstacles —
        // terrain is encounter data, so the simulator is fed the real battlefield.
        private static readonly GridBounds Bounds = new GridBounds(8, 8);

        private static BattleState RosterState(EncounterSpec encounter)
        {
            BattleState state = new BattleState();
            state.SetObstacles(encounter.Map.BuildObstacleMap());
            state.SetRoster(encounter.BuildRoster());
            return state;
        }

        [Test]
        public void EveryLibraryEncounter_PlaysToAWinner()
        {
            foreach (EncounterSpec encounter in EncounterLibrary.All())
            {
                BattleState state = RosterState(encounter);
                BattleSimulator sim = new BattleSimulator(state, encounter.Map.Bounds, encounter.Map.BuildElevationMap());

                bool decided = sim.RunToCompletion(maxTurns: 1000, out Faction winner);

                Assert.IsTrue(decided, $"'{encounter.Name}' never reached a winner within the turn cap.");
                Faction loser = winner == Faction.Player ? Faction.Enemy : Faction.Player;
                Assert.IsTrue(state.AnyAlive(winner), $"'{encounter.Name}': winner {winner} has no living units.");
                Assert.IsFalse(state.AnyAlive(loser), $"'{encounter.Name}': loser {loser} still has living units.");
            }
        }

        [Test]
        public void Resolution_KeepsHpAndMpInRange()
        {
            foreach (EncounterSpec encounter in EncounterLibrary.All())
            {
                BattleState state = RosterState(encounter);
                new BattleSimulator(state, encounter.Map.Bounds, encounter.Map.BuildElevationMap()).RunToCompletion(maxTurns: 1000, out _);

                foreach (CombatUnit unit in state.Units)
                {
                    Assert.GreaterOrEqual(unit.Hp, 0, $"{unit.Id} HP went negative.");
                    Assert.LessOrEqual(unit.Hp, unit.MaxHp, $"{unit.Id} HP exceeded MaxHp.");
                    Assert.GreaterOrEqual(unit.Mp, 0, $"{unit.Id} MP went negative.");
                }
            }
        }

        [Test]
        public void SameEncounter_RunTwice_IsDeterministic()
        {
            EncounterSpec encounter = EncounterLibrary.All()[0];

            BattleState a = RosterState(encounter);
            bool decidedA = new BattleSimulator(a, encounter.Map.Bounds, encounter.Map.BuildElevationMap()).RunToCompletion(1000, out Faction winnerA);

            BattleState b = RosterState(encounter);
            bool decidedB = new BattleSimulator(b, encounter.Map.Bounds, encounter.Map.BuildElevationMap()).RunToCompletion(1000, out Faction winnerB);

            Assert.AreEqual(decidedA, decidedB);
            Assert.AreEqual(winnerA, winnerB, "Same encounter produced different winners — the loop is non-deterministic.");
            for (int i = 0; i < a.Units.Count; i++)
            {
                Assert.AreEqual(a.Units[i].Hp, b.Units[i].Hp, $"Unit index {i} HP diverged between two identical runs.");
            }
        }

        [Test]
        public void AllAttacksMiss_NeverDeclaresAWinner()
        {
            // With every QTE Missing (0x damage) nobody can die, so the loop must
            // NOT declare a winner. Proves termination comes from real kills and
            // that the turn cap guards a genuine stalemate rather than hanging.
            EncounterSpec encounter = EncounterLibrary.All()[0];
            BattleState state = RosterState(encounter);
            BattleSimulator sim = new BattleSimulator(state, encounter.Map.Bounds, encounter.Map.BuildElevationMap(), ExecutionResult.Miss);

            bool decided = sim.RunToCompletion(maxTurns: 200, out _);

            Assert.IsFalse(decided, "A winner was declared even though every attack dealt 0 damage.");
            Assert.IsTrue(state.AnyAlive(Faction.Player) && state.AnyAlive(Faction.Enemy));
        }

        [Test]
        public void RunUnitTurn_FasterUnitStrikesAdjacentEnemy()
        {
            // A faster player next to an enemy should act first and damage it.
            CombatUnit player = BattleTestFactory.Unit(1, Faction.Player, new GridCoord(1, 1), speed: 20);
            CombatUnit enemy = BattleTestFactory.Unit(2, Faction.Enemy, new GridCoord(2, 1), speed: 5);
            BattleState state = new BattleState();
            state.SetRoster(new[] { player, enemy });

            BattleSimulator sim = new BattleSimulator(state, Bounds);
            CombatUnit acted = sim.RunUnitTurn();

            Assert.AreSame(player, acted, "The faster unit (player) should take the first turn.");
            Assert.Less(enemy.Hp, enemy.MaxHp, "The adjacent enemy should have taken damage.");
            Assert.IsTrue(enemy.IsAlive, "One Hit-tier basic attack shouldn't be lethal here.");
        }
    }
}
