using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Matching QTE: reproduce a short directional sequence in order before the
    // window closes. Fourth concrete IExecutionMeter type (M2 QTE-types slice).
    // Difficulty lengthens the sequence (MatchingCalculator); a wrong input ends
    // the run and grades on however many were matched in order.
    //
    // This is the first QTE that needs more than the Confirm edge, so it reads
    // directional intent from GameInput.Current directly (the same global the
    // orchestrator reads). The cursor is locked during a QTE, so there's no
    // contention over the move axis. The Confirm arg is unused here.
    public sealed class MatchingController : MonoBehaviour, IExecutionMeter
    {
        [Header("UI")]
        [SerializeField] private GameObject meterRoot;
        [SerializeField] private TMP_Text sequenceText;
        [SerializeField] private TMP_Text captionText;

        [Header("Timing")]
        // Whole-sequence window. Generous enough to read and input a 3-5 step
        // sequence; the pressure is memory + accuracy, not raw speed.
        [SerializeField] private float windowSeconds = 4.0f;
        [SerializeField] private float hideAfterStopSeconds = 0.5f;

        private static readonly Vector2Int Up = new Vector2Int(0, 1);
        private static readonly Vector2Int Down = new Vector2Int(0, -1);
        private static readonly Vector2Int Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int Right = new Vector2Int(1, 0);
        private static readonly Vector2Int[] Cardinals = { Up, Down, Left, Right };

        private readonly MatchingCalculator calculator = new MatchingCalculator();
        private Vector2Int[] sequence = new Vector2Int[0];
        private int matchedIndex;
        private float timeLeft;
        // The cardinal currently held/consumed (CardinalInput tracks it every
        // frame); a new press registers only when this changes (edge-on-change),
        // so rolling between directions works and a held direction doesn't repeat.
        private GridCoord lastCardinal;
        private bool isInitialized;
        private Coroutine hideLoop;

        public bool IsRunning { get; private set; }

        public bool IsAvailable => enabled;

        public string DisplayName => "Matching";

        private void Awake()
        {
            TryInitialize();
        }

        public void SetReferences(GameObject root, TMP_Text sequence, TMP_Text caption)
        {
            meterRoot = root;
            sequenceText = sequence;
            captionText = caption;
            TryInitialize();
        }

        public void Begin(QteDifficulty difficulty)
        {
            if (!EnsureReady())
            {
                return;
            }

            StopHideLoop();
            int length = Mathf.Max(1, MatchingCalculator.SequenceLength(difficulty));
            sequence = new Vector2Int[length];
            for (int i = 0; i < length; i++)
            {
                sequence[i] = Cardinals[Random.Range(0, Cardinals.Length)];
            }

            matchedIndex = 0;
            timeLeft = windowSeconds;
            IsRunning = true;
            // Seed with whatever direction is already held so it isn't counted
            // as a fresh press: a new input only registers when the cardinal
            // changes.
            Vector2Int held = GameInput.Current.MoveAxis;
            lastCardinal = CardinalInput.ToCardinal(held.x, held.y);

            Show();
            RenderSequence();
            if (captionText != null)
            {
                captionText.text = "Repeat the sequence!";
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

            timeLeft -= Time.deltaTime;

            // Clean-cardinal reduction + edge-on-change rule live in the pure,
            // tested CardinalInput (diagonals reduce to no-input; rolling between
            // directions registers each; a held direction doesn't repeat). Track
            // lastCardinal every frame — including releases to neutral — so a
            // re-press of the same direction registers again.
            Vector2Int axis = GameInput.Current.MoveAxis;
            GridCoord cardinal = CardinalInput.ToCardinal(axis.x, axis.y);
            bool registers = CardinalInput.RegistersPress(cardinal, lastCardinal);
            lastCardinal = cardinal;
            if (registers)
            {
                Vector2Int expected = sequence[matchedIndex];
                if (cardinal.X == expected.x && cardinal.Y == expected.y)
                {
                    matchedIndex++;
                    RenderSequence();
                    if (matchedIndex >= sequence.Length)
                    {
                        return Grade(out result, out multiplier);
                    }
                }
                else
                {
                    // Wrong input ends the run.
                    return Grade(out result, out multiplier);
                }
            }

            if (timeLeft <= 0.0f)
            {
                return Grade(out result, out multiplier);
            }

            if (captionText != null)
            {
                captionText.text = $"Repeat the sequence!   ({Mathf.Max(0f, timeLeft):0.0}s)";
            }

            return false;
        }

        public void Cancel()
        {
            IsRunning = false;
            StopHideLoop();
            HideImmediate();
        }

        private bool Grade(out ExecutionResult result, out float multiplier)
        {
            IsRunning = false;
            result = calculator.Evaluate(matchedIndex, sequence.Length);
            multiplier = ExecutionModifiers.GetDamageMultiplier(result);

            RenderSequence();
            if (captionText != null)
            {
                captionText.text = $"{matchedIndex}/{sequence.Length}  —  {result} (x{multiplier:0.00})";
            }

            StartHideAfterStop();
            return true;
        }

        private void RenderSequence()
        {
            if (sequenceText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < sequence.Length; i++)
            {
                string glyph = Glyph(sequence[i]);
                string colored;
                if (i < matchedIndex)
                {
                    colored = $"<color=#5BD16A>{glyph}</color>";        // matched
                }
                else if (i == matchedIndex && IsRunning)
                {
                    colored = $"<color=#FFD84D>{glyph}</color>";        // current
                }
                else
                {
                    colored = $"<color=#8A8A8A>{glyph}</color>";        // pending
                }

                builder.Append(colored);
                if (i < sequence.Length - 1)
                {
                    builder.Append("  ");
                }
            }

            sequenceText.text = builder.ToString();
        }

        private static string Glyph(Vector2Int dir)
        {
            // Arrow glyphs (UTF-8); they live in Unity's default TMP font
            // source (Liberation Sans), which the dynamic SDF atlas pulls from.
            if (dir == Up) return "↑";
            if (dir == Down) return "↓";
            if (dir == Left) return "←";
            return "→";
        }

        private void TryInitialize()
        {
            if (sequenceText == null)
            {
                return;
            }

            if (meterRoot == null)
            {
                meterRoot = sequenceText.gameObject;
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

            if (sequenceText == null)
            {
                Debug.LogWarning("MatchingController missing UI references.");
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
