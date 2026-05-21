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

        void SetVisible(bool visible);

        void Despawn();
    }
}
