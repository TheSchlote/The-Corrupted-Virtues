using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TheCorruptedVirtues.Combat
{
    // Simple harness to validate execution timing and damage in a Unity scene.
    public sealed class CombatSandboxController : MonoBehaviour
    {
        [Header("UI")]
        [FormerlySerializedAs("resonanceSlider")]
        [SerializeField] private Slider executionSlider;
        [SerializeField] private TMP_Text outputText;

        [Header("Timing")]
        [SerializeField] private float cycleDurationSeconds = 2.0f;

        private readonly ExecutionCalculator executionCalculator = new ExecutionCalculator();
        private Coroutine meterLoop;
        private Coroutine pulseLoop;
        private bool isRunning;
        private Vector3 baseSliderScale = Vector3.one;

        private readonly CombatStats attackerStats = new CombatStats(100, 30, 35, 20, 40, 22, 25);
        private readonly CombatStats defenderStats = new CombatStats(120, 10, 30, 30, 20, 28, 15);

        private readonly AbilitySpec testAbility =
            new AbilitySpec("Fire Bolt", AbilityKind.Special, ElementType.Fire, 20, 0.6f);

        private readonly ElementType attackerElement = ElementType.Fire;
        private readonly ElementType defenderElement = ElementType.Nature;

        private void Start()
        {
            if (executionSlider == null || outputText == null)
            {
                Debug.LogError("CombatSandboxController is missing UI references.");
                enabled = false;
                return;
            }

            baseSliderScale = executionSlider.transform.localScale;
            ResetMeter();
        }

        public void SetReferences(Slider slider, TMP_Text text)
        {
            executionSlider = slider;
            outputText = text;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetMeter();
                return;
            }

            if (isRunning && IsConfirmPressed())
            {
                StopMeterAndResolve();
            }
        }

        private bool IsConfirmPressed()
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool southPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return spacePressed || southPressed;
        }

        private void ResetMeter()
        {
            StopMeterLoop();
            StopPulse();
            executionSlider.transform.localScale = baseSliderScale;
            executionSlider.value = 0.0f;
            outputText.text = "Press Space or South button to stop the meter.";
            StartMeterLoop();
        }

        private void StartMeterLoop()
        {
            isRunning = true;
            meterLoop = StartCoroutine(AnimateMeter());
        }

        private void StopMeterLoop()
        {
            if (meterLoop != null)
            {
                StopCoroutine(meterLoop);
                meterLoop = null;
            }

            isRunning = false;
        }

        private IEnumerator AnimateMeter()
        {
            float elapsed = 0.0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                float t = (cycleDurationSeconds <= 0.0f)
                    ? 0.0f
                    : (elapsed % cycleDurationSeconds) / cycleDurationSeconds;
                executionSlider.value = t;
                yield return null;
            }
        }

        private void StopMeterAndResolve()
        {
            StopMeterLoop();
            StartPulse();

            float sliderValue = executionSlider.value;
            ExecutionResult executionResult = executionCalculator.Evaluate(sliderValue);
            float executionMultiplier = ExecutionModifiers.GetDamageMultiplier(executionResult);

            DamageBreakdown breakdown = DamageCalculator.ComputeDamage(
                attackerStats,
                attackerElement,
                defenderStats,
                defenderElement,
                testAbility,
                executionResult);

            StringBuilder builder = new StringBuilder(256);
            builder.AppendLine("=== Combat Sandbox ===");
            builder.Append("Slider: ").Append(sliderValue.ToString("0.000")).AppendLine();
            builder.Append("Execution: ").Append(executionResult)
                .Append(" (x").Append(executionMultiplier.ToString("0.00")).AppendLine(")");
            builder.Append("Element: ").Append(testAbility.Element)
                .Append(" vs ").Append(defenderElement)
                .Append(" (x").Append(breakdown.ElementMultiplier.ToString("0.00")).AppendLine(")");
            builder.Append("Pre-mitigation: ").Append(breakdown.PreMitigationDamage.ToString("0.00"))
                .Append(" | Mitigation: ").Append(breakdown.MitigationFactor.ToString("0.000")).AppendLine();
            builder.AppendLine("Windows: Fumble <0.20 | Miss 0.20-0.40 | Hit 0.40-0.80 | Divine 0.80-0.95 | LateHit 0.95-1.00");
            builder.Append("Final Damage: ").Append(breakdown.FinalDamage);

            outputText.text = builder.ToString();
            Debug.Log(outputText.text);
        }

        private void StartPulse()
        {
            StopPulse();
            pulseLoop = StartCoroutine(PulseSlider());
        }

        private void StopPulse()
        {
            if (pulseLoop != null)
            {
                StopCoroutine(pulseLoop);
                pulseLoop = null;
            }
        }

        private IEnumerator PulseSlider()
        {
            Transform target = executionSlider.transform;
            Vector3 upScale = baseSliderScale * 1.1f;

            yield return ScaleOverTime(target, baseSliderScale, upScale, 0.06f);
            yield return ScaleOverTime(target, upScale, baseSliderScale, 0.10f);
            target.localScale = baseSliderScale;
        }

        private IEnumerator ScaleOverTime(Transform target, Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0.0f ? 1.0f : Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(start, end, t);
                yield return null;
            }
        }
    }
}
