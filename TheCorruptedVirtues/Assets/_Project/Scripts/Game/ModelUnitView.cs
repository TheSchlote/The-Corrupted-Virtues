using System.Collections;
using UnityEngine;
using TheCorruptedVirtues.CombatSlice.Battle;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // IUnitView backed by a rigged model + Animator. The only class that knows a
    // unit is an animated model: it translates the view's semantic calls (move /
    // hit / face) into Animator parameters and transform motion. Drives a tiny
    // fixed contract — "Speed" (float) and "Hit" (trigger) — and silently skips
    // any parameter a given controller doesn't declare, so every model can ship
    // its own controller and clips behind the same contract. Gameplay logic and
    // CombatEvents never reference any of this.
    public sealed class ModelUnitView : MonoBehaviour, IUnitView
    {
        private const float MoveLerp = 14f;
        private const float DeathHideDelay = 1.5f;
        private const string SpeedParam = "Speed";
        private const string HitTrigger = "Hit";
        private const string AttackTrigger = "Attack";
        private const string DeathTrigger = "Death";

        private Transform modelRoot;     // the rigged model — rotated to face
        private Animator animator;
        private UnitViewOverlays overlays;

        private Vector3 target;
        private float yaw;

        private bool hasSpeed;
        private bool hasHit;
        private bool hasAttack;
        private bool hasDeath;
        private int speedId;
        private int hitId;
        private int attackId;
        private int deathId;

        // model is the already-parented prefab instance; overlays attach to this
        // (unrotated) root so billboarding never fights the model's facing turn.
        public void Init(Transform model, Color elementColor, bool isBoss)
        {
            modelRoot = model;
            animator = model.GetComponentInChildren<Animator>();
            CacheParameters();

            GameObject overlayHost = new GameObject("Overlays");
            overlayHost.transform.SetParent(transform, false);
            overlays = overlayHost.AddComponent<UnitViewOverlays>();
            overlays.Init(elementColor, isBoss);
        }

        private void CacheParameters()
        {
            speedId = Animator.StringToHash(SpeedParam);
            hitId = Animator.StringToHash(HitTrigger);
            attackId = Animator.StringToHash(AttackTrigger);
            deathId = Animator.StringToHash(DeathTrigger);

            // No controller yet is the normal "model imported, clips not wired"
            // state; accessing parameters in that case logs a noisy Unity warning.
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Float && p.name == SpeedParam) hasSpeed = true;
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == HitTrigger) hasHit = true;
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == AttackTrigger) hasAttack = true;
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == DeathTrigger) hasDeath = true;
            }
        }

        public void Warp(Vector3 world)
        {
            target = world;
            transform.position = world;
        }

        public void MoveTo(Vector3 world)
        {
            target = world;
        }

        public void PlayHitFlash()
        {
            if (hasHit && animator != null)
            {
                animator.SetTrigger(hitId);
            }
        }

        public void PlayAttack()
        {
            if (hasAttack && animator != null)
            {
                animator.SetTrigger(attackId);
            }
        }

        public void Die()
        {
            // Play the death animation then hide; without a Death trigger we
            // hide outright so the dead body doesn't linger in T-pose.
            if (hasDeath && animator != null && isActiveAndEnabled)
            {
                animator.SetTrigger(deathId);
                StartCoroutine(HideAfter(DeathHideDelay));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            gameObject.SetActive(false);
        }

        public void UpdateHp(int current, int max)
        {
            if (overlays != null) overlays.UpdateHp(current, max);
        }

        public void ShowDamagePreview(int previewDamage)
        {
            if (overlays != null) overlays.ShowDamagePreview(previewDamage);
        }

        public void ClearDamagePreview()
        {
            if (overlays != null) overlays.ClearDamagePreview();
        }

        public void SetActiveIndicator(bool active)
        {
            if (overlays != null) overlays.SetActiveIndicator(active);
        }

        public void SetFacing(Facing facing)
        {
            // Logic owns the canonical facing; the model itself turns to show it
            // (no separate arrow needed on the model path).
            yaw = YawFor(facing);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Despawn()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            if (modelRoot == null)
            {
                return;
            }

            Vector3 previous = transform.position;
            float t = 1f - Mathf.Exp(-MoveLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(previous, target, t);

            // Planar travel speed feeds the locomotion blend; settles to idle on
            // arrival. Models lacking a Speed param just don't react.
            if (hasSpeed && animator != null)
            {
                Vector3 delta = transform.position - previous;
                delta.y = 0f;
                float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
                animator.SetFloat(speedId, speed);
            }

            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, Quaternion.Euler(0f, yaw, 0f), t);
        }

        private static float YawFor(Facing facing)
        {
            switch (facing)
            {
                case Facing.East: return 90f;
                case Facing.South: return 180f;
                case Facing.West: return 270f;
                default: return 0f; // North
            }
        }
    }
}
