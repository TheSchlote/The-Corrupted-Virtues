using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Button-mash QTE: hammer Confirm to fill the bar before the window closes.
    // Second concrete IExecutionMeter type (M2 slice 2). Difficulty raises the
    // press target (ButtonMashCalculator). No overshoot penalty — extra presses
    // only ever help, capping at Divine once the target is reached.
    public sealed class ButtonMashController : MonoBehaviour, IExecutionMeter
    {
        [Header("UI")]
        [SerializeField] private GameObject meterRoot;
        [SerializeField] private Slider fillSlider;
        [SerializeField] private TMP_Text statusText;

        [Header("Timing")]
        [SerializeField] private float windowSeconds = 2.0f;
        [SerializeField] private float hideAfterStopSeconds = 0.35f;

        private readonly ButtonMashCalculator calculator = new ButtonMashCalculator();
        private int targetPresses = 1;
        private int presses;
        private float timeLeft;
        private bool isInitialized;
        private Coroutine hideLoop;

        public bool IsRunning { get; private set; }

        public bool IsAvailable => enabled;

        public string DisplayName => "Button Mash";

        private void Awake()
        {
            TryInitialize();
        }

        public void SetReferences(GameObject root, Slider slider, TMP_Text text)
        {
            meterRoot = root;
            fillSlider = slider;
            statusText = text;
            TryInitialize();
        }

        public void Begin(QteDifficulty difficulty)
        {
            if (!EnsureReady())
            {
                return;
            }

            StopHideLoop();
            targetPresses = Mathf.Max(1, ButtonMashCalculator.TargetPresses(difficulty));
            presses = 0;
            timeLeft = windowSeconds;
            IsRunning = true;
            Show();
            UpdateVisual();
        }

        public bool Tick(bool confirmPressed, out ExecutionResult result, out float multiplier)
        {
            result = ExecutionResult.Hit;
            multiplier = 1.0f;

            if (!IsRunning)
            {
                return false;
            }

            if (confirmPressed)
            {
                presses++;
            }

            timeLeft -= Time.deltaTime;
            UpdateVisual();

            if (timeLeft > 0.0f)
            {
                return false;
            }

            // Window closed — grade on presses / target.
            IsRunning = false;
            float ratio = targetPresses > 0 ? (float)presses / targetPresses : 0.0f;
            result = calculator.Evaluate(ratio);
            multiplier = ExecutionModifiers.GetDamageMultiplier(result);

            if (statusText != null)
            {
                statusText.text = $"{presses}/{targetPresses}  —  {result} (x{multiplier:0.00})";
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

        private void UpdateVisual()
        {
            if (fillSlider != null)
            {
                float ratio = targetPresses > 0 ? (float)presses / targetPresses : 0.0f;
                fillSlider.value = Mathf.Clamp01(ratio);
            }

            if (statusText != null && IsRunning)
            {
                statusText.text = $"MASH!  {presses}/{targetPresses}   ({Mathf.Max(0f, timeLeft):0.0}s)";
            }
        }

        private void TryInitialize()
        {
            if (fillSlider == null)
            {
                return;
            }

            if (meterRoot == null)
            {
                meterRoot = fillSlider.gameObject;
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

            if (fillSlider == null)
            {
                Debug.LogWarning("ButtonMashController missing UI references.");
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
