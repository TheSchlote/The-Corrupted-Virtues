using System.Collections.Generic;
using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Ambient procedural wing flap for a winged model. The wing bones are extra,
    // non-humanoid bones no retargeted clip touches, so this drives them in
    // LateUpdate (after the Animator writes the body pose). Self-contained and
    // gameplay-blind; lives on the model prefab and auto-discovers the
    // wing_left_<n> / wing_right_<n> chains if not wired in the Inspector.
    //
    // Axis/amplitude are first-guess defaults — tune them in the Inspector once
    // visible, since they depend on how the bones' local axes are oriented.
    [DefaultExecutionOrder(10000)]
    public sealed class WingFlap : MonoBehaviour
    {
        [SerializeField] private Transform[] wingBones;
        [SerializeField] private float flapDegrees = 10f;
        [SerializeField] private float flapSpeed = 2.2f;
        [SerializeField] private Vector3 flapAxis = Vector3.forward;
        [Tooltip("Phase lag from shoulder to tip; 0 flaps each wing as one piece.")]
        [SerializeField] private float segmentPhase = 0.1f;
        [Tooltip("Negate the right wing so both sweep symmetrically; flip if they splay the same way instead.")]
        [SerializeField] private bool mirrorRightWing = true;
        // Baked by the BuildFoldedPrefab editor tool — when populated, used as
        // restRotations at Awake instead of capturing live. Lets the fold survive
        // the Humanoid Animator snapping non-humanoid bones to the avatar's bind
        // pose at init (which would otherwise make live-captured rest = spread).
        [SerializeField] private Quaternion[] bakedRestRotations;

        private Quaternion[] restRotations;
        private float[] sideSigns;
        private int[] segmentIndices;

        private void Awake()
        {
            if (wingBones == null || wingBones.Length == 0)
            {
                wingBones = DiscoverWingBones();
            }

            int count = wingBones.Length;
            restRotations = new Quaternion[count];
            sideSigns = new float[count];
            segmentIndices = new int[count];

            bool useBaked = bakedRestRotations != null && bakedRestRotations.Length == count;
            for (int i = 0; i < count; i++)
            {
                Transform bone = wingBones[i];
                if (bone == null)
                {
                    continue;
                }

                restRotations[i] = useBaked ? bakedRestRotations[i] : bone.localRotation;
                bool isLeft = bone.name.ToLowerInvariant().StartsWith("wing_left_");
                sideSigns[i] = (isLeft || !mirrorRightWing) ? 1f : -1f;
                segmentIndices[i] = SegmentIndexOf(bone.name);
            }
        }

        private Transform[] DiscoverWingBones()
        {
            var found = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (n.StartsWith("wing_left_") || n.StartsWith("wing_right_"))
                {
                    found.Add(t);
                }
            }
            return found.ToArray();
        }

        // Trailing number of "wing_left_3" -> 3 — the phase key that keeps the
        // matching segment on each wing in step. Defaults to 1 if unparseable.
        private static int SegmentIndexOf(string boneName)
        {
            int underscore = boneName.LastIndexOf('_');
            if (underscore >= 0 && int.TryParse(boneName.Substring(underscore + 1), out int n))
            {
                return n;
            }
            return 1;
        }

        private void LateUpdate()
        {
            if (wingBones == null)
            {
                return;
            }

            for (int i = 0; i < wingBones.Length; i++)
            {
                Transform bone = wingBones[i];
                if (bone == null)
                {
                    continue;
                }

                // Phase by segment number (not array order) so the same segment
                // on each wing shares a clock — left and right stay in sync.
                float phase = (segmentIndices[i] - 1) * segmentPhase;
                float angle = Mathf.Sin(Time.time * flapSpeed - phase) * flapDegrees * sideSigns[i];
                bone.localRotation = restRotations[i] * Quaternion.AngleAxis(angle, flapAxis);
            }
        }
    }
}
