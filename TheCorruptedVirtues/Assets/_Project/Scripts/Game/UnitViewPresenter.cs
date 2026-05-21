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
        private UnitId activeDamagePreviewTarget;
        private bool damagePreviewActive;

        public void Initialize(CombatEvents combatEvents, GridPresenter gridPresenter, IUnitViewFactory viewFactory)
        {
            events = combatEvents;
            grid = gridPresenter;
            factory = viewFactory;

            events.UnitSpawned += OnUnitSpawned;
            events.UnitMoved += OnUnitMoved;
            events.UnitDamaged += OnUnitDamaged;
            events.UnitDied += OnUnitDied;
            events.DamageEstimateChanged += OnDamageEstimateChanged;
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
            events.DamageEstimateChanged -= OnDamageEstimateChanged;
            events.CombatReset -= OnCombatReset;
        }

        private void OnUnitSpawned(UnitSpawnedEvent e)
        {
            if (views.TryGetValue(e.Id, out IUnitView existing))
            {
                existing.SetVisible(true);
                existing.Warp(grid.GridToWorld(e.Coord, grid.UnitY));
                existing.UpdateHp(e.Hp, e.MaxHp);
                existing.ClearDamagePreview();
                return;
            }

            IUnitView view = factory.CreateUnit(e.Faction, e.Element);
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

            // The damage just resolved — the forecast no longer applies to
            // the current HP value, so clear any leftover preview.
            ClearActiveDamagePreview();
        }

        private void OnUnitDied(UnitId id)
        {
            if (views.TryGetValue(id, out IUnitView view))
            {
                view.SetVisible(false);
            }

            ClearActiveDamagePreview();
        }

        private void OnDamageEstimateChanged(DamageEstimateEvent e)
        {
            // Clear any previous overlay first so the preview can't get stuck
            // on a unit the cursor moved off of.
            ClearActiveDamagePreview();

            if (!e.HasEstimate)
            {
                return;
            }

            if (views.TryGetValue(e.TargetId, out IUnitView view))
            {
                view.ShowDamagePreview(e.HitDamage);
                activeDamagePreviewTarget = e.TargetId;
                damagePreviewActive = true;
            }
        }

        private void OnCombatReset()
        {
            foreach (IUnitView view in views.Values)
            {
                view.Despawn();
            }

            views.Clear();
            damagePreviewActive = false;
        }

        private void ClearActiveDamagePreview()
        {
            if (!damagePreviewActive)
            {
                return;
            }

            if (views.TryGetValue(activeDamagePreviewTarget, out IUnitView previousView))
            {
                previousView.ClearDamagePreview();
            }

            damagePreviewActive = false;
        }
    }
}
