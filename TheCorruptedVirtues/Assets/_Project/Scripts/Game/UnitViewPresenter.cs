using System.Collections.Generic;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;
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
        private UnitId activeUnit;
        private bool hasActiveUnit;

        public void Initialize(CombatEvents combatEvents, GridPresenter gridPresenter, IUnitViewFactory viewFactory)
        {
            events = combatEvents;
            grid = gridPresenter;
            factory = viewFactory;

            events.UnitSpawned += OnUnitSpawned;
            events.UnitMoved += OnUnitMoved;
            events.UnitDamaged += OnUnitDamaged;
            events.UnitHealed += OnUnitHealed;
            events.UnitDied += OnUnitDied;
            events.DamageEstimateChanged += OnDamageEstimateChanged;
            events.ActiveUnitChanged += OnActiveUnitChanged;
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
            events.UnitHealed -= OnUnitHealed;
            events.UnitDied -= OnUnitDied;
            events.DamageEstimateChanged -= OnDamageEstimateChanged;
            events.ActiveUnitChanged -= OnActiveUnitChanged;
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

        private void OnUnitHealed(UnitHealedEvent e)
        {
            if (views.TryGetValue(e.Id, out IUnitView view))
            {
                // HP bar rises; no hit flash — this is friendly feedback.
                view.UpdateHp(e.Hp, e.MaxHp);
            }

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

            // Heal forecasts don't paint the red "HP you'd lose" overlay.
            if (e.IsHeal)
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

        private void OnActiveUnitChanged(UnitId id)
        {
            // Clear the previous active indicator first; toggle the new one.
            // Works even if a unit died on its own turn — its view is hidden,
            // but turning off the indicator is harmless.
            if (hasActiveUnit && views.TryGetValue(activeUnit, out IUnitView previousActive))
            {
                previousActive.SetActiveIndicator(false);
            }

            activeUnit = id;
            hasActiveUnit = true;

            if (views.TryGetValue(id, out IUnitView newActive))
            {
                newActive.SetActiveIndicator(true);
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
            hasActiveUnit = false;
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
