using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // A unit's visual. The asset swap point: cubes today, rigged models
    // later — only the IUnitViewFactory implementation changes, never the
    // presenters or logic.
    public interface IUnitView
    {
        // Teleport with no interpolation (spawn / reset).
        void Warp(Vector3 world);

        // Smoothly travel to a new cell (the view owns the easing).
        void MoveTo(Vector3 world);

        // Brief "I got hit" feedback.
        void PlayHitFlash();

        // Update any view-side HP display (floating bar, ring, etc). Logic
        // still owns the canonical HP — this is purely presentation.
        void UpdateHp(int current, int max);

        // XCOM-style preview: show how much of this unit's HP would be lost
        // by an incoming hit (typically the 1.0x "Hit" tier). Cleared when the
        // attack hover ends. View decides how to render it.
        void ShowDamagePreview(int previewDamage);

        void ClearDamagePreview();

        // "It's this unit's turn" affordance — the view picks how to render
        // (ring under the unit, glow, arrow, etc). M2 squads need this since
        // multiple units belong to each side.
        void SetActiveIndicator(bool active);

        void SetVisible(bool visible);

        void Despawn();
    }
}
