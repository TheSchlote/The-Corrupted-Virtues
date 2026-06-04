using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;

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

        // The attacker's swing / cast cue (one per ability use). Primitives can
        // fall back to a flash; rigged models trigger their Attack animation.
        void PlayAttack();

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

        // Orient the unit's facing indicator (auto-facing arrow). Logic owns
        // the canonical facing; the view just points the arrow.
        void SetFacing(Facing facing);

        void SetVisible(bool visible);

        // The unit died — view plays its dramatic exit (death animation, then
        // self-hide). Distinct from SetVisible(false), which is just generic
        // hiding (e.g. for the spawn-reuse path).
        void Die();

        void Despawn();
    }
}
