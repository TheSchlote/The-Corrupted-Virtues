using System.Collections.Generic;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Battle
{
    // Headless, deterministic turn runner that composes the pure combat systems
    // (TurnSystem + EnemyTurnPlanner + AbilityResolver + MovementRules over a
    // BattleState) into a complete battle loop with no Unity layer.
    //
    // The in-game CombatSliceOrchestrator drives these same systems with
    // coroutines, player input and live QTE meters; this is their pure
    // equivalent. It powers AI-vs-AI simulation and — the reason it exists now —
    // lets the whole turn loop be exercised in EditMode, which the MonoBehaviour
    // orchestrator cannot be. (As the orchestrator is decomposed per the roadmap,
    // it can delegate its turn-stepping here so the two share one tested loop.)
    //
    // Every actor of either faction is planned by EnemyTurnPlanner, which always
    // targets the faction opposing the actor, so it runs symmetrically. QTE
    // execution is supplied as a fixed tier since there is no live meter.
    public sealed class BattleSimulator
    {
        private readonly BattleState state;
        private readonly TurnSystem turns;
        private readonly GridBounds bounds;
        private readonly ElevationMap elevation;
        private readonly ExecutionResult execution;

        // Faction of the unit that acted most recently — the win-check tie-break
        // (mirrors the orchestrator passing "the last attacker"). Only consulted
        // in the can't-happen both-sides-empty case, so its initial value is moot.
        private Faction lastActorFaction;

        public BattleSimulator(
            BattleState state,
            GridBounds bounds,
            ElevationMap elevation = null,
            ExecutionResult execution = ExecutionResult.Hit)
        {
            this.state = state;
            this.bounds = bounds;
            this.elevation = elevation;
            this.execution = execution;
            turns = new TurnSystem(state);
        }

        public TurnSystem Turns => turns;

        public bool TryGetWinner(out Faction winner)
        {
            return state.TryGetWinner(lastActorFaction, out winner);
        }

        // Advance to the next living unit and play its whole turn: walk the
        // planned path, then resolve the planned attack (single-target or burst)
        // if one lands and the actor can afford it. Returns the unit that acted,
        // or null when no unit is alive to act.
        public CombatUnit RunUnitTurn()
        {
            CombatUnit actor = turns.AdvanceToNextLivingUnit();
            if (actor == null)
            {
                return null;
            }

            lastActorFaction = actor.Faction;

            EnemyTurnPlan plan = EnemyTurnPlanner.Plan(actor, state, bounds, elevation);
            ApplyMove(actor, plan);
            ApplyAttack(actor, plan);
            return actor;
        }

        // Play turns until one faction is wiped or `maxTurns` unit-turns elapse.
        // The cap guards a genuine non-terminating stalemate (e.g. every QTE
        // missing for 0 damage); it is not expected to trigger in a real fight.
        // Returns true if a winner was decided.
        public bool RunToCompletion(int maxTurns, out Faction winner)
        {
            for (int i = 0; i < maxTurns; i++)
            {
                if (state.TryGetWinner(lastActorFaction, out winner))
                {
                    return true;
                }

                if (RunUnitTurn() == null)
                {
                    break;
                }
            }

            return state.TryGetWinner(lastActorFaction, out winner);
        }

        private void ApplyMove(CombatUnit actor, EnemyTurnPlan plan)
        {
            if (!plan.HasMove)
            {
                return;
            }

            IReadOnlyList<GridCoord> path = plan.MovePath;
            GridCoord destination = path[path.Count - 1];
            GridCoord prior = path[path.Count - 2];
            actor.Coord = destination;
            actor.Facing = FacingRules.Toward(prior, destination);
            state.RebuildOccupancy();
        }

        private void ApplyAttack(CombatUnit actor, EnemyTurnPlan plan)
        {
            if (!plan.AttackAfterMove || plan.Ability == null || plan.Target == null || !plan.Target.IsAlive)
            {
                return;
            }

            AbilitySpec ability = plan.Ability;
            if (actor.Mp < ability.MpCost)
            {
                return;
            }

            // MP is spent on commit, win or lose the QTE — matches the orchestrator.
            actor.Mp -= ability.MpCost;
            // Face the target before striking, as auto-facing does in the live game.
            actor.Facing = FacingRules.Toward(actor.Coord, plan.Target.Coord);

            if (ability.IsAreaOfEffect)
            {
                List<CombatUnit> targets = AreaOfEffect.CollectTargets(
                    plan.Target.Coord, ability.AoeRadius, actor.Faction, state);
                AbilityResolver.ResolveArea(actor, targets, ability, execution, elevation);
            }
            else
            {
                SituationalModifiers mods = CombatSituation.For(actor, plan.Target, elevation);
                AbilityResolver.Resolve(actor, plan.Target, ability, execution, mods);
            }
        }
    }
}
