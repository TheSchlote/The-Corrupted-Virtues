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
        private Image outcomePanel;
        private TMP_Text damageInfoText;
        private TMP_Text endTurnHintText;

        public void Initialize(CombatEvents combatEvents)
        {
            events = combatEvents;
            BuildCanvas();

            events.TurnChanged += OnTurnChanged;
            events.UnitSpawned += OnUnitSpawned;
            events.UnitDamaged += OnUnitDamaged;
            events.SelectionChanged += OnSelectionChanged;
            events.DamageEstimateChanged += OnDamageEstimateChanged;
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
            events.DamageEstimateChanged -= OnDamageEstimateChanged;
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

        private void OnDamageEstimateChanged(DamageEstimateEvent e)
        {
            if (damageInfoText == null)
            {
                return;
            }

            if (!e.HasEstimate)
            {
                damageInfoText.text = string.Empty;
                return;
            }

            string matchupLabel;
            Color matchupColor;
            if (e.ElementMultiplier > 1.01f)
            {
                matchupLabel = "STRONG";
                matchupColor = new Color(0.5f, 0.95f, 0.5f);
            }
            else if (e.ElementMultiplier < 0.99f)
            {
                matchupLabel = "WEAK";
                matchupColor = new Color(0.95f, 0.55f, 0.5f);
            }
            else
            {
                matchupLabel = "Neutral";
                matchupColor = new Color(0.85f, 0.85f, 0.85f);
            }

            string hex = ColorUtility.ToHtmlStringRGB(matchupColor);

            // Three-line readout: which attack, which QTE type, then the
            // damage + element matchup. The attack/QTE line was the M1.5
            // playtest gap — players couldn't tell what they were committing to.
            string attackLine = string.IsNullOrEmpty(e.AttackName) ? string.Empty : e.AttackName;
            string qteLine = string.IsNullOrEmpty(e.QteName) ? string.Empty : e.QteName;
            string header = !string.IsNullOrEmpty(attackLine) && !string.IsNullOrEmpty(qteLine)
                ? $"<size=85%>{attackLine}  ·  {qteLine}</size>\n"
                : (string.IsNullOrEmpty(attackLine) ? string.Empty : $"<size=85%>{attackLine}</size>\n");

            damageInfoText.text =
                header +
                $"DMG {e.HitDamage}  <size=80%>(Divine {e.DivineDamage})</size>\n" +
                $"<size=80%>{e.AttackerElement} → {e.DefenderElement}</size>  <color=#{hex}>{matchupLabel}</color>";
        }

        private void OnCombatEnded(Faction winner)
        {
            if (outcomeText != null)
            {
                outcomeText.text = winner == Faction.Player
                    ? "VICTORY\n<size=60%>Press R to fight again</size>"
                    : "DEFEAT\n<size=60%>Press R to try again</size>";
            }

            if (outcomePanel != null)
            {
                outcomePanel.gameObject.SetActive(true);
            }

            // No more selection or estimate events fire once combat is over,
            // so the last action hint ("Confirm: Stop Swing") and the last
            // damage forecast would linger otherwise. Same for the persistent
            // End Turn hint — Tab is meaningless once combat is over.
            if (hintText != null)
            {
                hintText.text = string.Empty;
            }

            if (damageInfoText != null)
            {
                damageInfoText.text = string.Empty;
            }

            if (endTurnHintText != null)
            {
                endTurnHintText.text = string.Empty;
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

            if (outcomePanel != null)
            {
                outcomePanel.gameObject.SetActive(false);
            }

            if (damageInfoText != null)
            {
                damageInfoText.text = string.Empty;
            }

            if (endTurnHintText != null)
            {
                endTurnHintText.text = "Tab: End Turn";
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

            // Persistent corner hint so End Turn is discoverable without
            // hunting through a menu. Muted; not meant to grab focus.
            endTurnHintText = CreateText(canvas.transform, "EndTurnHintText", new Vector2(0.75f, 0.02f), new Vector2(0.98f, 0.07f), "Tab: End Turn");
            endTurnHintText.alignment = TextAlignmentOptions.BottomRight;
            endTurnHintText.fontSize = 18;
            endTurnHintText.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);

            // Damage forecast lives in the top-right — the Gladius-style
            // "1.0x estimate + critical upside" readout fires only when the
            // cursor is over a valid attack target. Taller now to fit the
            // attack name + QTE-type line above the damage.
            damageInfoText = CreateText(canvas.transform, "DamageInfoText", new Vector2(0.55f, 0.79f), new Vector2(0.98f, 0.99f), string.Empty);
            damageInfoText.alignment = TextAlignmentOptions.TopRight;

            // VICTORY/DEFEAT outcome — panel + text together, panel created
            // first so it draws behind the text. Hidden until CombatEnded.
            outcomePanel = CreatePanel(canvas.transform, "OutcomePanel", new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.62f), new Color(0f, 0f, 0f, 0.7f));
            outcomePanel.gameObject.SetActive(false);

            outcomeText = CreateText(canvas.transform, "OutcomeText", new Vector2(0.25f, 0.42f), new Vector2(0.75f, 0.60f), string.Empty);
            outcomeText.alignment = TextAlignmentOptions.Center;
            outcomeText.fontSize = 52;
            outcomeText.fontStyle = FontStyles.Bold;
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
