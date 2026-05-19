using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Drives the Gladius-style swing meter UI and timing evaluation.
    // First concrete IExecutionMeter (QTE) type.
    public sealed class SwingMeterController : MonoBehaviour, IExecutionMeter
    {
        [Header("UI")]
        [SerializeField] private GameObject meterRoot;
        [SerializeField] private Slider executionSlider;
        [SerializeField] private TMP_Text statusText;

        [Header("Timing")]
        [SerializeField] private float cycleDurationSeconds = 2.0f;
        [SerializeField] private float hideAfterStopSeconds = 0.35f;

        [Header("Text")]
        [SerializeField] private string promptText = "Press Confirm to stop the swing meter.";
        [SerializeField] private string resultFormat = "Timing: {0} (x{1:0.00})";

        private readonly ExecutionCalculator executionCalculator = new ExecutionCalculator();
        private Coroutine meterLoop;
        private Coroutine pulseLoop;
        private Coroutine hideLoop;
        private Vector3 baseSliderScale = Vector3.one;
        private bool isInitialized;

        public bool IsRunning { get; private set; }

        public bool IsAvailable => enabled;

        private void Awake()
        {
            TryInitialize();
        }

        public void SetReferences(GameObject root, Slider slider, TMP_Text text)
        {
            meterRoot = root;
            executionSlider = slider;
            statusText = text;
            TryInitialize();
        }

        public void Begin()
        {
            if (!EnsureReady())
            {
                return;
            }

            StopHideLoop();
            StopMeterLoop();
            StopPulse();

            Show();
            executionSlider.transform.localScale = baseSliderScale;
            executionSlider.value = 0.0f;

            if (statusText != null && !string.IsNullOrEmpty(promptText))
            {
                statusText.text = promptText;
            }

            StartMeterLoop();
        }

        public ExecutionResult StopAndEvaluate(out float multiplier, out float normalizedValue)
        {
            if (!EnsureReady())
            {
                multiplier = 1.0f;
                normalizedValue = 0.0f;
                return ExecutionResult.Hit;
            }

            StopMeterLoop();
            StartPulse();

            normalizedValue = executionSlider != null ? executionSlider.value : 0.0f;
            ExecutionResult result = executionCalculator.Evaluate(normalizedValue);
            multiplier = ExecutionModifiers.GetDamageMultiplier(result);

            if (statusText != null && !string.IsNullOrEmpty(resultFormat))
            {
                statusText.text = string.Format(resultFormat, result, multiplier);
            }

            StartHideAfterStop();
            return result;
        }

        public void Cancel()
        {
            StopHideLoop();
            StopMeterLoop();
            StopPulse();
            HideImmediate();
        }

        private void TryInitialize()
        {
            if (executionSlider == null)
            {
                return;
            }

            if (meterRoot == null)
            {
                meterRoot = executionSlider.gameObject;
            }

            baseSliderScale = executionSlider.transform.localScale;
            HideImmediate();
            isInitialized = true;
        }

        private bool EnsureReady()
        {
            if (!isInitialized)
            {
                TryInitialize();
            }

            if (executionSlider == null)
            {
                Debug.LogWarning("SwingMeterController missing UI references.");
                return false;
            }

            return true;
        }

        private void Show()
        {
            if (meterRoot != null)
            {
                meterRoot.SetActive(true);
            }
        }

        private void HideImmediate()
        {
            if (meterRoot != null)
            {
                meterRoot.SetActive(false);
            }
        }

        private void StartHideAfterStop()
        {
            if (hideAfterStopSeconds < 0.0f)
            {
                return;
            }

            StopHideLoop();
            hideLoop = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            if (hideAfterStopSeconds > 0.0f)
            {
                yield return new WaitForSeconds(hideAfterStopSeconds);
            }

            HideImmediate();
            hideLoop = null;
        }

        private void StopHideLoop()
        {
            if (hideLoop != null)
            {
                StopCoroutine(hideLoop);
                hideLoop = null;
            }
        }

        private void StartMeterLoop()
        {
            IsRunning = true;
            meterLoop = StartCoroutine(AnimateMeter());
        }

        private void StopMeterLoop()
        {
            if (meterLoop != null)
            {
                StopCoroutine(meterLoop);
                meterLoop = null;
            }

            IsRunning = false;
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
