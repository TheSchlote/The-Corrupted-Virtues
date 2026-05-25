using System.Collections;
using TMPro;
using UnityEngine;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Timed-press QTE: a marker sweeps across the track once; press Confirm when
    // it overlaps the target. Third concrete IExecutionMeter type (M2 QTE-types
    // slice). Difficulty narrows the Divine/Hit bands (TimedPressCalculator);
    // letting the marker run off the end without a press grades a Fumble.
    public sealed class TimedPressController : MonoBehaviour, IExecutionMeter
    {
        [Header("UI")]
        [SerializeField] private GameObject meterRoot;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform hitZone;
        [SerializeField] private RectTransform divineZone;
        [SerializeField] private TMP_Text statusText;

        [Header("Timing")]
        // One sweep across the track. Tight enough that hitting the centre is a
        // genuine reaction check, in the same spirit as the 1.0s swing cycle.
        [SerializeField] private float sweepSeconds = 1.1f;
        [SerializeField] private float hideAfterStopSeconds = 0.35f;

        private const float MarkerWidth = 0.012f;

        private TimedPressCalculator calculator = new TimedPressCalculator();
        private float markerValue;
        private bool isInitialized;
        private Coroutine hideLoop;

        public bool IsRunning { get; private set; }

        public bool IsAvailable => enabled;

        public string DisplayName => "Timed Press";

        private void Awake()
        {
            TryInitialize();
        }

        public void SetReferences(GameObject root, RectTransform markerRect, RectTransform hit, RectTransform divine, TMP_Text text)
        {
            meterRoot = root;
            marker = markerRect;
            hitZone = hit;
            divineZone = divine;
            statusText = text;
            TryInitialize();
        }

        public void Begin(QteDifficulty difficulty)
        {
            if (!EnsureReady())
            {
                return;
            }

            calculator = new TimedPressCalculator(difficulty);
            ApplyZoneLayout();

            StopHideLoop();
            markerValue = 0.0f;
            IsRunning = true;
            Show();
            UpdateMarker();

            if (statusText != null)
            {
                statusText.text = "Press Confirm on the target!";
            }
        }

        public bool Tick(bool confirmPressed, out ExecutionResult result, out float multiplier)
        {
            result = ExecutionResult.Hit;
            multiplier = 1.0f;

            if (!IsRunning)
            {
                return false;
            }

            markerValue += sweepSeconds > 0.0f ? Time.deltaTime / sweepSeconds : 1.0f;

            // A press grades at the current marker position; running off the end
            // without one grades at 1.0 (far from centre -> Fumble).
            bool timedOut = markerValue >= 1.0f;
            if (!confirmPressed && !timedOut)
            {
                UpdateMarker();
                return false;
            }

            float pressValue = Mathf.Clamp01(markerValue);
            markerValue = pressValue;
            UpdateMarker();

            IsRunning = false;
            result = calculator.Evaluate(pressValue);
            multiplier = ExecutionModifiers.GetDamageMultiplier(result);

            if (statusText != null)
            {
                statusText.text = $"{result} (x{multiplier:0.00})";
            }

            StartHideAfterStop();
            return true;
        }

        public void Cancel()
        {
            IsRunning = false;
            StopHideLoop();
            HideImmediate();
        }

        // Paint the Hit/Divine bands around the fixed centre so they match the
        // grader's windows for the current difficulty.
        private void ApplyZoneLayout()
        {
            float center = TimedPressCalculator.TargetCenter;
            SetZoneRange(hitZone, center - calculator.HitHalf, center + calculator.HitHalf);
            SetZoneRange(divineZone, center - calculator.DivineHalf, center + calculator.DivineHalf);
        }

        private static void SetZoneRange(RectTransform zone, float minX, float maxX)
        {
            if (zone == null)
            {
                return;
            }

            Vector2 min = zone.anchorMin;
            min.x = Mathf.Clamp01(minX);
            zone.anchorMin = min;

            Vector2 max = zone.anchorMax;
            max.x = Mathf.Clamp01(maxX);
            zone.anchorMax = max;
        }

        private void UpdateMarker()
        {
            if (marker == null)
            {
                return;
            }

            float x = Mathf.Clamp01(markerValue);
            Vector2 min = marker.anchorMin;
            Vector2 max = marker.anchorMax;
            min.x = Mathf.Clamp01(x - MarkerWidth * 0.5f);
            max.x = Mathf.Clamp01(x + MarkerWidth * 0.5f);
            marker.anchorMin = min;
            marker.anchorMax = max;
        }

        private void TryInitialize()
        {
            if (marker == null)
            {
                return;
            }

            if (meterRoot == null)
            {
                meterRoot = marker.gameObject;
            }

            HideImmediate();
            isInitialized = true;
        }

        private bool EnsureReady()
        {
            if (!isInitialized)
            {
                TryInitialize();
            }

            if (marker == null)
            {
                Debug.LogWarning("TimedPressController missing UI references.");
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
    }
}
