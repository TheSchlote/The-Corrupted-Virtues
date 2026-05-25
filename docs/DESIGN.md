# Corrupted Virtues – Combat System Vision

## Purpose of This Document

This document defines the **end vision**, **current state**, and **next implementation steps** for the **Corrupted Virtues** combat system.

It is intended to:

* Preserve design intent across sessions
* Serve as a handoff reference for **ChatGPT Codex**
* Clearly separate prototype scaffolding from long-term architecture
* Prevent premature overengineering

### Instructions for AI Assistants (Codex)

* Prefer **pure C#**, Unity-agnostic logic where possible
* **Do NOT refactor existing systems** unless explicitly instructed
* Extend systems in a **modular, testable** way
* Favor **clarity over cleverness**
* Prototype with **simple primitives** (capsules/cubes) and minimal UI

---

## High-Level End Vision

**Corrupted Virtues** is a **turn-based, tactical RPG** inspired heavily by **LucasArts’ *Gladius***, with modern clarity and extensibility.

### Core Combat Pillars

1. **Execution Timing System** (skill-based, “attack performance”)
2. **Elemental Advantage System**
3. **Readable, deterministic damage math**
4. **Expandable ability and status framework**
5. **Strict separation of combat logic from Unity presentation**

### The Combat System Must Support (Eventually)

* AI combat
* Local multiplayer
* Online multiplayer (future)
* Spectator clarity

  * Damage breakdowns
  * Explainable outcomes

---

## Execution System (Timing Core)

### Concept

**Execution** represents how well the attacker performs the attack moment.

Mechanically, this is a **normalized value** in the range **`[0.0 – 1.0]`**, evaluated into outcome tiers.

### Execution Windows (swing meter, Normal difficulty)

| Range          | Result           | Multiplier |
| -------------- | ---------------- | ---------- |
| `[0.00, 0.20)` | Fumble           | `0.50x`    |
| `[0.20, 0.40)` | Miss             | `0.00x`    |
| `[0.40, 0.85)` | Hit              | `1.00x`    |
| `[0.85, 0.92]` | Divine           | `1.50x`    |
| `(0.92, 1.00]` | Miss (overshoot) | `0.00x`    |

#### Notes

* **LateHit was dropped (M2 slice 1).** Overshooting past Divine is now a
  **Miss**, not a forgiven Hit — that is what makes Divine a real risk/reward
  call instead of a free reward for holding late.
* **Difficulty narrows the Divine window.** `QteDifficulty` (Normal / Hard /
  Brutal) tightens the Divine band for stronger abilities; the Hit and
  overshoot-Miss zones grow to fill the gap. Normal is the table above.
* **Execution is a family, not one widget.** `IExecutionMeter` is the seam;
  concrete types are **swing meter** (above), **button mash**, **timed press**,
  and **matching**. Each grades on its own pure calculator but produces the same
  four tiers and multipliers, so downstream math is identical regardless of type.
* These thresholds are **design-tunable constants**, not final values.

### Current Implementation

* `ExecutionCalculator`

  * Evaluates normalized input
* `ExecutionResult`

  * Enum defining outcome tiers
* `ExecutionModifiers`

  * Maps result → damage multiplier
* `ButtonMashCalculator` / `TimedPressCalculator` / `MatchingCalculator`

  * Pure graders for the other QTE types (same four tiers)
* In-combat meters (`SwingMeterController`, `ButtonMashController`, …) paint the
  exact zones the grader uses; the standalone sandbox prototype was retired once
  the combat slice shipped

---

## Elemental System

### Elements

```text
Nature
Water
Earth
Fire
Electricity
Light
Dark
```

### Relationships

**Binary Pair**

```text
Light > Dark
Dark  > Light
```

**Circular Chain**

```text
Water > Fire > Nature > Earth > Electricity > Water
```

### Rules

* **Advantage:** `1.25x`
* **Disadvantage:** `0.80x`
* **Same Element:** Neutral (`1.00x`, tunable later)

### Current Implementation

* `ElementType` enum
* `ElementChart.GetMultiplier(attacker, defender)`
* Disadvantage inferred as inverse of advantage

---

## Damage Model

Damage is **deterministic and inspectable**.

### Conceptual Formula

```text
PreMitigation = AbilityPower + (AttackStat × Scaling)
Mitigated     = PreMitigation × (100 / (100 + DefenseStat))
FinalDamage   = round( Mitigated × ElementMultiplier × ExecutionMultiplier × SituationalProduct ),  min 0
```

* `AttackStat` / `DefenseStat` are chosen by ability kind (see below).
* `SituationalProduct = HighGround × Flanking` — position-derived terms, each
  `1.0` when absent (see **Situational Modifiers**). They multiply, so high
  ground *and* a rear hit stack.
* Rounding is half-away-from-zero; damage floors at `0`.
* Every term is echoed in `DamageBreakdown` so a hit is explainable line-by-line.

### Ability Types

* **Physical**

  * Uses `Attack` vs `Defense`
* **Special**

  * Uses `SpecialAttack` vs `SpecialDefense`
* **Support**

  * Deals no damage
  * Heal scales off `SpecialAttack` (`HealCalculator`), graded by the same execution tiers
  * Returns `0` damage but still reports multipliers

### Situational Modifiers

Position-derived terms folded into `SituationalProduct`. One source of truth
(`CombatSituation`) feeds both the on-screen forecast and the live resolve.

* **High ground:** attacking from a higher elevation than the target = `1.25x`.
* **Flanking** (single-target only): rear hit `1.50x`, side `1.25x`, front `1.00x`,
  read from the *target's* facing.
* **Area attacks** are non-directional: they take high ground but **never**
  flanking, and hit every opponent in a Chebyshev burst around the target tile
  (`AreaOfEffect` + `AbilityResolver.ResolveArea`, one breakdown per target).
* **Multi-tile units** (e.g. the 2×2 Great Beast) must keep their whole
  footprint on **one** elevation level — they can't stop straddling an edge
  (`ElevationMap.IsUniformUnder` gates footprint pathfinding). Asset-agnostic by
  design: a real model never has to bridge a ledge, and the anchor tile's level
  cleanly defines the unit's elevation for the high-ground term.

### Stat Semantics

The Digimon-Survive block; each stat has exactly one role in the math:

| Stat              | Role                                                            |
| ----------------- | --------------------------------------------------------------- |
| `MaxHP`           | Health pool; `0` = death (the Great Beast's pool = Corruption gauge). |
| `MaxMP`           | Ability resource; spent on commit, no passive regen; basic attack free. |
| `Attack`          | `AttackStat` for **Physical** abilities.                        |
| `Defense`         | `DefenseStat` mitigating **Physical** abilities.                |
| `SpecialAttack`   | `AttackStat` for **Special** abilities; also the heal-scaling stat. |
| `SpecialDefense`  | `DefenseStat` mitigating **Special** abilities.                 |
| `Speed`           | Turn order only (`TurnOrder`, lower `Id` breaks ties); **not** a damage term. |

### Key Design Rule

> Every damage result must be explainable line-by-line to a player, designer, or tester.

### Current Implementation

* `CombatStats`
* `AbilitySpec`
* `AbilityKind`
* `DamageCalculator`
* `DamageBreakdown` (returns intermediate values)

---

> **Status note (M2).** The sections below are **historical** — they captured the
> M0–M1 plan when the combat slice was still upcoming. That slice and the
> M1 / M1.5 / M2 work — squads + Speed turn order, abilities + MP, the QTE-type
> family, terrain, facing/flanking, the Great Beast boss, AoE, and smarter enemy
> AI — have all shipped. For live milestone status and scope, see **ROADMAP.md**.
> The combat **math** above is current; the planning notes below are kept for
> design history.

## Current Prototype State

### What Exists

* Fully playable **Combat Sandbox Scene**
* Execution meter featuring:

  * Animated cycling
  * Color-coded windows
  * LateHit visualization
* Input:

  * `Spacebar` / Gamepad South → Confirm
  * `R` → Reset
* Text output displaying:

  * Execution result
  * Element interaction
  * Pre-mitigation damage
  * Mitigation factor
  * Final damage

### What This Is

* A mechanical proving ground
* A tuning and feel-validation tool
* A safe place to experiment

### What This Is NOT

* Final UI
* Final animation system
* Final combat flow
* Network-safe logic (yet)

---

## New Direction: Combat Slice v0.1 (NEXT MILESTONE)

The Execution meter is “good enough” for now. The next priority is validating the **tactical combat loop**.

### Combat Slice Goal

> “I can select a character, move on a grid, attack an enemy, and see damage.”

### Scope Constraints (IMPORTANT)

Keep it minimal:

* Flat grid (no elevation yet)
* Basic movement rules (simple range)
* One player unit, one enemy unit
* One action per turn (move OR attack)
* Placeholder visuals (capsules/cubes)
* No status effects
* No complex AI
* No inventory/progression
* No overworld

### Why This Comes Next

This validates:

* Pacing (turns + downtime)
* Readability (camera + targeting)
* Fun (movement + attack + execution + damage)
* “Does this feel like Gladius?”

---

## Immediate Next Steps (Next Session)

### 1) Create a Combat Slice Scene (Unity)

* New scene: `CombatSlice.unity`
* Simple floor + grid visualization
* Spawn:

  * Player unit (capsule)
  * Enemy unit (capsule)
* Camera:

  * Simple tactical camera (temporary)

### 2) Minimal Grid + Movement (Code)

* Create pure C# types:

  * `GridCoord (x, y)`
  * `GridSize (width, height)`
  * `GridOccupancy` (tracks blocked/occupied)
  * `GridRange` helpers (Manhattan distance)
* Unity adapters:

  * Convert `GridCoord` ↔ `Vector3`
  * Click-to-select tile (temporary raycast)
* Movement rules:

  * Movement range N (e.g., 4)
  * Clamp to bounds
  * Prevent moving onto occupied tile

### 3) Turn Skeleton (Code)

* Two sides: Player then Enemy
* One action per turn:

  * Move OR Attack
* Enemy behavior:

  * Placeholder (wait, or step toward player)

### 4) Bind Attack to Execution (Code + Unity)

* When player chooses Attack:

  * choose target (closest enemy or clicked enemy)
  * run Execution meter (reuse existing logic/UI)
  * compute damage
  * apply to enemy HP
  * end turn
* Display:

  * current turn owner
  * selected unit
  * HP values (simple TMP text)

---

## What Codex Should Script vs What You Do in Unity

### Unity Editor Tasks (Manual)

These are faster/safer to do by hand:

1. **Create scene** `CombatSlice.unity`

2. **Place objects**

   * Plane/floor
   * Player capsule
   * Enemy capsule

3. **Add components / wire references**

   * Assign serialized fields in Inspector (TMP text, references)

4. **Create simple UI**

   * Canvas + TMP text labels (Turn/Selection/HP)
   * (Optional) reuse your Execution UI from Sandbox

5. **Tag/layer setup (optional)**

   * Layer “Grid” for raycasts
   * Layer “Units” for selection

### Codex Tasks (Automatable)

Codex should generate and iterate these:

1. **Pure C# grid logic**

   * `GridCoord`, `GridOccupancy`, `GridRange`
   * Unit tests for range + occupancy

2. **Turn skeleton**

   * `TurnState` / `TurnController` (simple)
   * `Faction` enum
   * Minimal “one action per turn” enforcement

3. **Movement controller (Unity orchestration layer)**

   * `UnitController` (holds current grid coord, faction, HP)
   * `GridPresenter` (coord ↔ world conversion, debug draw)
   * `SelectionController` (raycast selection)

4. **Combat glue**

   * `AttackController` that:

     * triggers Execution meter
     * calls `DamageCalculator`
     * applies HP changes
     * ends turn
   * Keep combat math Unity-agnostic

5. **Debug UI updates**

   * `CombatHudController` to update TMP fields

---

## Explicitly Deferred (DO NOT DO YET)

* Status effects
* Complex AI decision making
* Elevation, cover, terrain modifiers
* Turn order stats/speed systems
* Animation syncing
* ScriptableObject refactors
* Networking
* Overworld exploration

---

## Success Criteria for This Phase

You should be able to:

* Move a unit on a grid with clear selection feedback
* Perform an attack that uses Execution and deterministic damage
* End turns and repeat the loop
* Say with confidence:

  * “This is fun even with cubes/capsules.”

When that’s true, we expand.

---

## Notes to Future Self

* The Execution system already works. Do not tear it down.
* Validate the loop before adding more mechanics.
* Prototype ruthlessly. Pretty comes later.
