using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Builds the combat HUD entirely in code and keeps it in sync from
    // events. No hand-wired canvas, no serialized text references.
    public sealed class HudPresenter : MonoBehaviour
    {
        private readonly Dictionary<UnitId, Faction> unitFactions = new Dictionary<UnitId, Faction>();
        private readonly Dictionary<Faction, int> hp = new Dictionary<Faction, int>();
        private readonly Dictionary<Faction, int> maxHp = new Dictionary<Faction, int>();

        private CombatEvents events;
        private TMP_Text turnText;
        private TMP_Text playerHpText;
        private TMP_Text enemyHpText;
        private TMP_Text hintText;
        private TMP_Text outcomeText;

        public void Initialize(CombatEvents combatEvents)
        {
            events = combatEvents;
            BuildCanvas();

            events.TurnChanged += OnTurnChanged;
            events.UnitSpawned += OnUnitSpawned;
            events.UnitDamaged += OnUnitDamaged;
            events.SelectionChanged += OnSelectionChanged;
            events.CombatEnded += OnCombatEnded;
            events.CombatReset += OnCombatReset;
        }

        private void OnDestroy()
        {
            if (events == null)
            {
                return;
            }

            events.TurnChanged -= OnTurnChanged;
            events.UnitSpawned -= OnUnitSpawned;
            events.UnitDamaged -= OnUnitDamaged;
            events.SelectionChanged -= OnSelectionChanged;
            events.CombatEnded -= OnCombatEnded;
            events.CombatReset -= OnCombatReset;
        }

        private void OnTurnChanged(Faction active)
        {
            if (turnText != null)
            {
                turnText.text = active == Faction.Player ? "Turn: Player" : "Turn: Enemy";
            }
        }

        private void OnUnitSpawned(UnitSpawnedEvent e)
        {
            unitFactions[e.Id] = e.Faction;
            hp[e.Faction] = e.Hp;
            maxHp[e.Faction] = e.MaxHp;
            RefreshHp();
        }

        private void OnUnitDamaged(UnitDamagedEvent e)
        {
            if (unitFactions.TryGetValue(e.Id, out Faction faction))
            {
                hp[faction] = e.Hp;
                maxHp[faction] = e.MaxHp;
                RefreshHp();
            }
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            if (hintText != null)
            {
                hintText.text = string.IsNullOrEmpty(e.Hint) ? string.Empty : e.Hint;
            }
        }

        private void OnCombatEnded(Faction winner)
        {
            if (outcomeText != null)
            {
                outcomeText.text = winner == Faction.Player
                    ? "VICTORY\n<size=60%>Press R to fight again</size>"
                    : "DEFEAT\n<size=60%>Press R to try again</size>";
            }

            // No more selection events fire once combat is over, so the last
            // action hint ("Confirm: Stop Swing") would linger otherwise.
            if (hintText != null)
            {
                hintText.text = string.Empty;
            }
        }

        private void OnCombatReset()
        {
            unitFactions.Clear();
            hp.Clear();
            maxHp.Clear();
            RefreshHp();
            if (hintText != null)
            {
                hintText.text = string.Empty;
            }

            if (outcomeText != null)
            {
                outcomeText.text = string.Empty;
            }
        }

        private void RefreshHp()
        {
            if (playerHpText != null)
            {
                playerHpText.text = $"Player HP: {Hp(Faction.Player)}/{MaxHp(Faction.Player)}";
            }

            if (enemyHpText != null)
            {
                enemyHpText.text = $"Enemy HP: {Hp(Faction.Enemy)}/{MaxHp(Faction.Enemy)}";
            }
        }

        private int Hp(Faction f) => hp.TryGetValue(f, out int v) ? v : 0;
        private int MaxHp(Faction f) => maxHp.TryGetValue(f, out int v) ? v : 0;

        private void BuildCanvas()
        {
            Canvas canvas = UiCanvas.CreateOverlay("HudCanvas", transform);

            turnText = CreateText(canvas.transform, "TurnText", new Vector2(0.02f, 0.92f), new Vector2(0.4f, 0.99f), "Turn: Player");
            playerHpText = CreateText(canvas.transform, "PlayerHpText", new Vector2(0.02f, 0.85f), new Vector2(0.4f, 0.92f), "Player HP: -");
            enemyHpText = CreateText(canvas.transform, "EnemyHpText", new Vector2(0.02f, 0.78f), new Vector2(0.4f, 0.85f), "Enemy HP: -");
            hintText = CreateText(canvas.transform, "HintText", new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.1f), string.Empty);
            hintText.alignment = TextAlignmentOptions.Center;

            // Mid-screen, clear of the VFX callout (y 0.70-0.82) and the
            // swing meter (y 0.15-0.235) so end-of-combat reads cleanly.
            outcomeText = CreateText(canvas.transform, "OutcomeText", new Vector2(0.25f, 0.42f), new Vector2(0.75f, 0.60f), string.Empty);
            outcomeText.alignment = TextAlignmentOptions.Center;
            outcomeText.fontSize = 52;
            outcomeText.fontStyle = FontStyles.Bold;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string content)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = content;
            return text;
        }
    }
}
