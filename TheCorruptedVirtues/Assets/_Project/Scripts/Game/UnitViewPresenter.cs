using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Turns unit events into unit visuals via the view factory. Holds no
    // gameplay state — purely reflects what logic announces.
    public sealed class UnitViewPresenter : MonoBehaviour
    {
        private readonly Dictionary<UnitId, IUnitView> views = new Dictionary<UnitId, IUnitView>();
        private CombatEvents events;
        private GridPresenter grid;
        private IUnitViewFactory factory;

        public void Initialize(CombatEvents combatEvents, GridPresenter gridPresenter, IUnitViewFactory viewFactory)
        {
            events = combatEvents;
            grid = gridPresenter;
            factory = viewFactory;

            events.UnitSpawned += OnUnitSpawned;
            events.UnitMoved += OnUnitMoved;
            events.UnitDamaged += OnUnitDamaged;
            events.UnitDied += OnUnitDied;
            events.CombatReset += OnCombatReset;
        }

        private void OnDestroy()
        {
            if (events == null)
            {
                return;
            }

            events.UnitSpawned -= OnUnitSpawned;
            events.UnitMoved -= OnUnitMoved;
            events.UnitDamaged -= OnUnitDamaged;
            events.UnitDied -= OnUnitDied;
            events.CombatReset -= OnCombatReset;
        }

        private void OnUnitSpawned(UnitSpawnedEvent e)
        {
            if (views.TryGetValue(e.Id, out IUnitView existing))
            {
                existing.SetVisible(true);
                existing.Warp(grid.GridToWorld(e.Coord, grid.UnitY));
                existing.UpdateHp(e.Hp, e.MaxHp);
                return;
            }

            IUnitView view = factory.CreateUnit(e.Faction);
            view.Warp(grid.GridToWorld(e.Coord, grid.UnitY));
            view.UpdateHp(e.Hp, e.MaxHp);
            views[e.Id] = view;
        }

        private void OnUnitMoved(UnitMovedEvent e)
        {
            if (views.TryGetValue(e.Id, out IUnitView view))
            {
                view.MoveTo(grid.GridToWorld(e.Coord, grid.UnitY));
            }
        }

        private void OnUnitDamaged(UnitDamagedEvent e)
        {
            if (views.TryGetValue(e.Id, out IUnitView view))
            {
                view.UpdateHp(e.Hp, e.MaxHp);
                view.PlayHitFlash();
            }
        }

        private void OnUnitDied(UnitId id)
        {
            if (views.TryGetValue(id, out IUnitView view))
            {
                view.SetVisible(false);
            }
        }

        private void OnCombatReset()
        {
            foreach (IUnitView view in views.Values)
            {
                view.Despawn();
            }

            views.Clear();
        }
    }
}
