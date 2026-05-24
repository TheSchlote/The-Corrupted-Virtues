using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Battle;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Builds the combat HUD entirely in code and keeps it in sync from
    // events. No hand-wired canvas, no serialized text references.
    //
    // M2 slice 1: the per-side HP text rows were redundant with floating
    // per-unit HP bars and didn't scale to squads. Replaced with a squad
    // roster line (Player 2/2 · Enemy 1/2) computed from per-unit damage and
    // death events.
    public sealed class HudPresenter : MonoBehaviour
    {
        private sealed class UnitState
        {
            public Faction Faction;
            public bool IsAlive;
        }

        private readonly Dictionary<UnitId, UnitState> unitStates = new Dictionary<UnitId, UnitState>();

        private CombatEvents events;
        private TMP_Text squadText;
        private TMP_Text hintText;
        private TMP_Text outcomeText;
        private Image outcomePanel;
        private TMP_Text damageInfoText;
        private TMP_Text endTurnHintText;
        private TMP_Text abilityText;

        public void Initialize(CombatEvents combatEvents)
        {
            events = combatEvents;
            BuildCanvas();

            // "Whose turn is it?" is carried by the TurnOrderPresenter strip
            // (highlighted active chip + faction badge), so HudPresenter no
            // longer renders a "Turn: X" text — would be redundant.
            events.UnitSpawned += OnUnitSpawned;
            events.UnitDamaged += OnUnitDamaged;
            events.UnitDied += OnUnitDied;
            events.SelectionChanged += OnSelectionChanged;
            events.DamageEstimateChanged += OnDamageEstimateChanged;
            events.AbilitySelectionChanged += OnAbilitySelectionChanged;
            events.CombatEnded += OnCombatEnded;
            events.CombatReset += OnCombatReset;
        }

        private void OnDestroy()
        {
            if (events == null)
            {
                return;
            }

            events.UnitSpawned -= OnUnitSpawned;
            events.UnitDamaged -= OnUnitDamaged;
            events.UnitDied -= OnUnitDied;
            events.SelectionChanged -= OnSelectionChanged;
            events.DamageEstimateChanged -= OnDamageEstimateChanged;
            events.AbilitySelectionChanged -= OnAbilitySelectionChanged;
            events.CombatEnded -= OnCombatEnded;
            events.CombatReset -= OnCombatReset;
        }

        private void OnUnitSpawned(UnitSpawnedEvent e)
        {
            unitStates[e.Id] = new UnitState { Faction = e.Faction, IsAlive = e.Hp > 0 };
            RefreshSquadCounts();
        }

        private void OnUnitDamaged(UnitDamagedEvent e)
        {
            // Death detection happens via UnitDied; UnitDamaged only updates
            // alive status when hp hits zero (the orchestrator fires UnitDied
            // right after, but updating here is harmless and idempotent).
            if (unitStates.TryGetValue(e.Id, out UnitState state))
            {
                state.IsAlive = e.Hp > 0;
                RefreshSquadCounts();
            }
        }

        private void OnUnitDied(UnitId id)
        {
            if (unitStates.TryGetValue(id, out UnitState state))
            {
                state.IsAlive = false;
                RefreshSquadCounts();
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

            // Support forecast: a heal, not damage — green, no element matchup.
            if (e.IsHeal)
            {
                string healHeader = string.IsNullOrEmpty(e.AttackName)
                    ? string.Empty
                    : $"<size=85%>{e.AttackName}  ·  {e.QteName}</size>\n";
                damageInfoText.text =
                    healHeader +
                    $"<color=#7FE0A0>HEAL {e.HitDamage}</color>  <size=80%>(Divine {e.DivineDamage})</size>";
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

        private void OnAbilitySelectionChanged(AbilitySelectionEvent e)
        {
            if (abilityText == null)
            {
                return;
            }

            if (!e.HasSelection)
            {
                abilityText.text = string.Empty;
                return;
            }

            string mp = e.MpCost > 0
                ? $"{e.CurrentMp}/{e.MaxMp} MP · cost {e.MpCost}"
                : $"{e.CurrentMp}/{e.MaxMp} MP · free";
            string qte = e.Difficulty == QteDifficulty.Normal
                ? e.QteName
                : $"{e.QteName} ({e.Difficulty})";
            string afford = e.CanAfford ? string.Empty : "  <color=#E06666>need MP</color>";

            abilityText.text =
                $"{e.AbilityName}  <size=75%>({e.Index + 1}/{e.Count})  [C] cycle</size>\n" +
                $"<size=78%>{mp}  ·  {qte}</size>{afford}";
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

            if (abilityText != null)
            {
                abilityText.text = string.Empty;
            }
        }

        private void OnCombatReset()
        {
            unitStates.Clear();
            RefreshSquadCounts();
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

            if (abilityText != null)
            {
                abilityText.text = string.Empty;
            }
        }

        private void RefreshSquadCounts()
        {
            if (squadText == null)
            {
                return;
            }

            int playerAlive = 0;
            int playerTotal = 0;
            int enemyAlive = 0;
            int enemyTotal = 0;
            foreach (UnitState state in unitStates.Values)
            {
                if (state.Faction == Faction.Player)
                {
                    playerTotal++;
                    if (state.IsAlive) playerAlive++;
                }
                else
                {
                    enemyTotal++;
                    if (state.IsAlive) enemyAlive++;
                }
            }

            squadText.text = $"Player {playerAlive}/{playerTotal}  ·  Enemy {enemyAlive}/{enemyTotal}";
        }

        private void BuildCanvas()
        {
            Canvas canvas = UiCanvas.CreateOverlay("HudCanvas", transform);

            squadText = CreateText(canvas.transform, "SquadText", new Vector2(0.02f, 0.92f), new Vector2(0.4f, 0.99f), "Player -/-  ·  Enemy -/-");
            hintText = CreateText(canvas.transform, "HintText", new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.1f), string.Empty);
            hintText.alignment = TextAlignmentOptions.Center;

            endTurnHintText = CreateText(canvas.transform, "EndTurnHintText", new Vector2(0.75f, 0.02f), new Vector2(0.98f, 0.07f), "Tab: End Turn");
            endTurnHintText.alignment = TextAlignmentOptions.BottomRight;
            endTurnHintText.fontSize = 18;
            endTurnHintText.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);

            // Ability selector line, just under the squad roster. Shows the
            // active player unit's chosen ability + MP + QTE; empty otherwise.
            abilityText = CreateText(canvas.transform, "AbilityText", new Vector2(0.02f, 0.83f), new Vector2(0.5f, 0.915f), string.Empty);
            abilityText.fontSize = 20;

            // Damage forecast lives in the top-right, below the turn-order
            // strip that sits at the very top (built by TurnOrderPresenter).
            damageInfoText = CreateText(canvas.transform, "DamageInfoText", new Vector2(0.55f, 0.72f), new Vector2(0.98f, 0.86f), string.Empty);
            damageInfoText.alignment = TextAlignmentOptions.TopRight;

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
