using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TheCorruptedVirtues.Combat;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Top-of-screen strip showing the next several unit turns in order. The
    // first chip is the currently-active unit (highlighted); the rest are
    // the upcoming queue. Each chip is coloured by the unit's element and
    // shaped/badged by faction so it matches the unit on the field.
    public sealed class TurnOrderPresenter : MonoBehaviour
    {
        private sealed class UnitMeta
        {
            public Faction Faction;
            public ElementType Element;
        }

        private readonly Dictionary<UnitId, UnitMeta> unitMeta = new Dictionary<UnitId, UnitMeta>();
        private readonly List<GameObject> chipObjects = new List<GameObject>();

        private CombatEvents events;
        private Canvas canvas;
        private RectTransform stripRoot;

        public void Initialize(CombatEvents combatEvents)
        {
            events = combatEvents;
            BuildCanvas();

            events.UnitSpawned += OnUnitSpawned;
            events.TurnOrderChanged += OnTurnOrderChanged;
            events.CombatReset += OnCombatReset;
            events.CombatEnded += OnCombatEnded;
        }

        private void OnDestroy()
        {
            if (events == null)
            {
                return;
            }

            events.UnitSpawned -= OnUnitSpawned;
            events.TurnOrderChanged -= OnTurnOrderChanged;
            events.CombatReset -= OnCombatReset;
            events.CombatEnded -= OnCombatEnded;
        }

        private void OnUnitSpawned(UnitSpawnedEvent e)
        {
            unitMeta[e.Id] = new UnitMeta { Faction = e.Faction, Element = e.Element };
        }

        private void OnTurnOrderChanged(IReadOnlyList<UnitId> upcoming)
        {
            ClearChips();
            if (upcoming == null || upcoming.Count == 0)
            {
                return;
            }

            for (int i = 0; i < upcoming.Count; i++)
            {
                BuildChip(upcoming[i], isActive: i == 0, sequenceIndex: i);
            }
        }

        private void OnCombatReset()
        {
            unitMeta.Clear();
            ClearChips();
        }

        private void OnCombatEnded(Faction winner)
        {
            // Once combat is over the strip is just clutter; hide until reset.
            ClearChips();
        }

        private void ClearChips()
        {
            for (int i = 0; i < chipObjects.Count; i++)
            {
                if (chipObjects[i] != null)
                {
                    Destroy(chipObjects[i]);
                }
            }
            chipObjects.Clear();
        }

        private void BuildChip(UnitId id, bool isActive, int sequenceIndex)
        {
            if (!unitMeta.TryGetValue(id, out UnitMeta meta))
            {
                return;
            }

            // Chip layout in stripRoot-local space (0..1 across strip width).
            // Sized for ~6 chips with spacing — fills the strip horizontally.
            const float chipWidthFraction = 0.13f;
            const float chipSpacing = 0.01f;
            float minX = sequenceIndex * (chipWidthFraction + chipSpacing);
            float maxX = minX + chipWidthFraction;

            GameObject chip = new GameObject(isActive ? "Chip_Active" : $"Chip_{sequenceIndex}");
            chip.transform.SetParent(stripRoot, false);
            RectTransform chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(minX, 0.05f);
            chipRect.anchorMax = new Vector2(maxX, 0.95f);
            chipRect.offsetMin = Vector2.zero;
            chipRect.offsetMax = Vector2.zero;

            // Active-unit border behind the body so the body sits on top.
            if (isActive)
            {
                GameObject border = new GameObject("Border");
                border.transform.SetParent(chip.transform, false);
                RectTransform borderRect = border.AddComponent<RectTransform>();
                borderRect.anchorMin = new Vector2(-0.08f, -0.08f);
                borderRect.anchorMax = new Vector2(1.08f, 1.08f);
                borderRect.offsetMin = Vector2.zero;
                borderRect.offsetMax = Vector2.zero;
                Image borderImg = border.AddComponent<Image>();
                borderImg.color = new Color(1f, 0.95f, 0.4f, 0.95f);
                borderImg.raycastTarget = false;
            }

            // Chip body — element colour.
            GameObject body = new GameObject("Body");
            body.transform.SetParent(chip.transform, false);
            RectTransform bodyRect = body.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            Image bodyImg = body.AddComponent<Image>();
            bodyImg.color = ColorForElement(meta.Element);
            bodyImg.raycastTarget = false;

            // Faction badge — a thin strip across the bottom of the chip in
            // player-blue or enemy-red, so element colour and faction read
            // independently.
            GameObject badge = new GameObject("FactionBadge");
            badge.transform.SetParent(chip.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0.18f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;
            Image badgeImg = badge.AddComponent<Image>();
            badgeImg.color = meta.Faction == Faction.Player
                ? new Color(0.25f, 0.55f, 0.95f, 1f)
                : new Color(0.9f, 0.3f, 0.25f, 1f);
            badgeImg.raycastTarget = false;

            chipObjects.Add(chip);
        }

        private static Color ColorForElement(ElementType element)
        {
            switch (element)
            {
                case ElementType.Light:       return new Color(0.98f, 0.92f, 0.62f);
                case ElementType.Dark:        return new Color(0.32f, 0.18f, 0.42f);
                case ElementType.Fire:        return new Color(0.95f, 0.40f, 0.20f);
                case ElementType.Water:       return new Color(0.30f, 0.55f, 0.95f);
                case ElementType.Nature:      return new Color(0.40f, 0.80f, 0.35f);
                case ElementType.Earth:       return new Color(0.65f, 0.50f, 0.30f);
                case ElementType.Electricity: return new Color(0.95f, 0.85f, 0.30f);
                default:                      return new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void BuildCanvas()
        {
            canvas = UiCanvas.CreateOverlay("TurnOrderCanvas", transform);

            // Top strip — sits to the right of the squad-count text in the
            // HUD so the two don't fight for space. Wide enough for ~6 chips.
            GameObject root = new GameObject("TurnOrderStrip");
            root.transform.SetParent(canvas.transform, false);
            stripRoot = root.AddComponent<RectTransform>();
            stripRoot.anchorMin = new Vector2(0.42f, 0.92f);
            stripRoot.anchorMax = new Vector2(0.98f, 0.99f);
            stripRoot.offsetMin = Vector2.zero;
            stripRoot.offsetMax = Vector2.zero;
        }
    }
}
