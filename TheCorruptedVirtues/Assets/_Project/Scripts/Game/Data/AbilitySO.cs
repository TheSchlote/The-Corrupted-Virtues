using UnityEngine;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Editor-authorable ability data. Mirrors the pure AbilitySpec's fields with
    // Unity-serializable types and converts to it via ToSpec(); the combat core
    // only ever sees AbilitySpec, never this asset. This is the asset-authoring
    // seam — the runtime data is still the pure spec.
    [CreateAssetMenu(fileName = "Ability", menuName = "TCV/Ability")]
    public sealed class AbilitySO : ScriptableObject
    {
        [SerializeField] private string displayName = "New Ability";
        [SerializeField] private AbilityKind kind = AbilityKind.Physical;
        [SerializeField] private ElementType element = ElementType.Light;
        [SerializeField] private int power = 10;
        [SerializeField] private float scaling = 1.0f;

        [Header("QTE / cost")]
        [SerializeField] private int mpCost = 0;
        [SerializeField] private QteType qteType = QteType.SwingMeter;
        [SerializeField] private QteDifficulty qteDifficulty = QteDifficulty.Normal;
        [Tooltip("Chebyshev burst radius; 0 = single target.")]
        [SerializeField] private int aoeRadius = 0;

        [Header("Targeting")]
        [Tooltip("Off = derive from Kind (Support -> Ally, else Enemy), matching AbilitySpec's default.")]
        [SerializeField] private bool overrideTargeting = false;
        [SerializeField] private TargetingMode targeting = TargetingMode.Enemy;
        [SerializeField] private int range = 1;

        public AbilitySpec ToSpec()
        {
            return new AbilitySpec(
                displayName, kind, element, power, scaling,
                mpCost, qteType, qteDifficulty, aoeRadius,
                overrideTargeting ? (TargetingMode?)targeting : null,
                range);
        }

        // Stamp this asset from a pure spec — used by the Editor content generator.
        public void Configure(AbilitySpec spec)
        {
            displayName = spec.Name;
            kind = spec.Kind;
            element = spec.Element;
            power = spec.Power;
            scaling = spec.Scaling;
            mpCost = spec.MpCost;
            qteType = spec.QteType;
            qteDifficulty = spec.QteDifficulty;
            aoeRadius = spec.AoeRadius;
            overrideTargeting = true;
            targeting = spec.Targeting;
            range = spec.Range;
        }
    }
}
