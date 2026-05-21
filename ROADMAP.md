# The Corrupted Virtues — Roadmap

> **Living document.** Edit anything — checkboxes, scope, ordering, priorities. This is
> yours to redirect as the game evolves. Claude reads this for design/scope intent.
> World & story live in [docs/LORE.md](docs/LORE.md) and [docs/STORY.md](docs/STORY.md).
>
> _Last reviewed: 2026-05-21_

---

## 0. Prime Directive

> Build a **love letter to *Gladius*** with a Corrupted Virtues spin — and treat this as
> a **multi-year** effort. The single biggest unknown is **art/assets**, so the current
> priority is a **modular, scalable, asset-agnostic architecture**: gameplay logic must
> never depend on what the art *is*. Placeholders now; whatever asset solution lands
> later (commission, store, generated) plugs in behind a presentation seam without
> touching combat/grid/turn code.

## 1. Vision (one paragraph)

A turn-based tactical RPG. Combat is a grid SRPG where attacks are resolved with
**skill-based timed QTE inputs** (a family of action commands — swing meter, button
mash, timed press, matching — not a single mechanic). Damage is deterministic and fully
explainable. You play a **Guardian** purging **corruption** from the seven fallen
**Virtues** of Areti. Runs on a pure-C# combat core with a code-driven Unity layer.

## 2. Design Pillars & Inspirations

| Source | What we take from it |
|---|---|
| **Gladius** (LucasArts) | Origin of the combat identity: timed **QTE action commands** on attacks (a *variety* of them) and large **Great Beast** bosses. |
| **Paper Mario: TTYD** | Action-command feel — the satisfying timed-input variety we want beyond just a swing meter. |
| **Digimon Survive** | Reference for game *feel* and the *stat model*: the familiar block (HP / MP / ATK / DEF / Sp.ATK / Sp.DEF / SPD). |
| **Digimon World: Next Order** | **Art-direction north star (future).** Stylized, readable 3D, Unity-made — the visual target *once assets exist*. Primitives until then. |
| Determinism | Every damage number explainable line-by-line (pre-mitigation → mitigation → element → QTE → final). |
| Logic / Unity separation | Pure C# core (no `UnityEngine`), code-driven Unity layer — **no manual Editor wiring**. |

**Locked design decisions (2026-05-18):**
- **QTE is a framework, not a widget.** A pluggable QTE abstraction; the swing meter is
  the *first* concrete type. Button-mash / timed-press / matching come later. Don't
  hard-code "swing meter" anywhere.
- **7-element chart stays** (current code). Thematically **Dark = Corruption,
  Light = Virtue/Purity**. Corruption is an **enemy/world state**, not a player
  resource. (Supersedes the old TDD "Alignment replaces elements" idea.)
- **Bosses = Great Beasts:** corrupted Virtues are **2×2 grid units** with a
  **Corruption gauge**. Win by depleting it = *purify, not kill*; the Virtue survives
  and aids the party. ⇒ **grid core must support multi-tile units from the start**, even
  though M1 only spawns 1×1.
- **Naming:** generic, clear engineering names now. Lore-flavored renames
  (Resonance / DivineStrike / Essence …) are a **late cosmetic pass**, just flavor.
- `CombatStats` already matches the Digimon-Survive block — confirms keeping the combat
  math; stat-*semantics* tuning is an M2 task, not a rebuild.
- **Asset-agnostic presentation seam (top priority).** Combat / grid / turn logic must
  never reference concrete visuals. Units, bosses, VFX, UI spawn through a
  presenter/factory so primitive → final model is a one-place swap. Assets are the open
  problem; the architecture must not bake in any assumption about them.
- **Story is minimal: "region → boss fight," repeated.** No tabletop player-characters;
  NPCs limited to the Choir + Royal Court; side quests / minigames / branching parked.
  Boss creatures are **original** designs (the D&D 5E monsters were placeholders).
- **Choir roster is canon** (7 named Archangels + signature weapons) — see
  [docs/LORE.md](docs/LORE.md). Lore is captured but does **not** expand build scope.

## 3. Branching & Release Strategy

- **`main`** = source of truth. Always compiles, tests green. Protected.
- **Feature branches**: `feature/<milestone>-<short>` → PR → merge to `main` → delete. Short-lived.
- **CI** (build + test) on every push/PR to `main`. _(continuous integration — safety net)_
- **CD** (player builds / deploy) triggers **only on version tags `vX.Y.Z`** — never on a plain `main` push. _(delivery is a deliberate tag, not a merge side effect)_
- **Milestone complete ⇒ annotated tag** (`v0.1.0` = M1, `v0.2.0` = M2, …).
- Archived prototypes preserved as tags: `archive/prototype-combat`, `archive/gridmovement`.
- _(Later, optional)_ `release/x.y` stabilization branch only if a release needs a freeze window. Not needed solo yet.

## 4. Narrative & World

Full bible: [docs/LORE.md](docs/LORE.md) · Campaign spine: [docs/STORY.md](docs/STORY.md).

- World **Areti**, seven regions each ruled by a **Virtue** + tied to an element; central
  tower **Paradeisos**; **The Choir** (7 Archangels); **Guardians** (player is one).
- Each Virtue is corrupted by a paired **Sin** into a **Great Beast** boss; the party
  **purifies** them, restoring region by region, then confronts **Chaos**, the Source of
  Corruption (with **Laylah / the Night Mother** as the manipulator behind the throne).
- **Scope (2026-05-18):** the campaign is just "**region → boss fight**," repeated,
  then the Paradeisos finale. Tabletop PCs cut; NPCs limited to Choir + Royal Court;
  side content/minigames parked. Boss creatures are original; the D&D monsters were
  placeholders. Lore is preserved in `docs/` but does not grow the build.

## 5. Milestones

### M0 — Foundation _(plumbing only, no gameplay change)_ ✅ **complete (2026-05-19)**
- [x] Archive tags `archive/prototype-combat`, `archive/gridmovement` (pushed)
- [x] Fast-forward `main` → `prototype-combat` (zero loss, full history)
- [x] `.gitattributes` — Git LFS for binary art, forced-text + `unityyamlmerge` for `.unity`/`.asset`/`.prefab`, normalized EOL
- [x] Consolidate `.gitignore` (single correct file, add `*.slnx`); add `.editorconfig`
- [x] Assembly definitions: `Combat` (pure C#), `GridCore` (pure C#), `Unity`, `Tests.EditMode`
- [x] Reorganize scripts into folders matching asmdef boundaries
- [x] EditMode tests pinning: `DamageCalculator`, `ElementChart`, `ExecutionCalculator`, `GridPathfinderBfs`, `GridOccupancy`
- [x] Commit `ROADMAP.md`, `docs/LORE.md`, `docs/STORY.md`; add `docs/DESIGN.md` (combat math spec); real root `README.md`
- [x] **Checkpoint:** open Unity → recompiles clean → Test Runner green
- [x] CI workflow: build + test on push/PR to `main` _(PR #1, merged 2026-05-19; `UNITY_LICENSE` secret setup still pending — see [docs/CI.md](docs/CI.md))_

### M1 — Vertical Slice _(prove the loop is fun with capsules)_
- [x] `CombatSliceBootstrap`: builds grid + units + camera + UI + input entirely in C# _(PR #5)_
- [x] Retire the hand-wired `CombatSlice.unity` (near-empty scene + bootstrap) _(PR #5)_
- [x] Input abstraction: keyboard/mouse **and** gamepad from day one _(PR #4)_
- [x] **QTE abstraction** (interface) with the existing **swing meter as the first concrete type** _(PR #3)_
- [x] **Grid core supports multi-tile occupancy** (design for N×N; M1 spawns only 1×1) _(PR #2)_
- [x] Loop: select unit → move → attack → QTE → deterministic damage → end turn → repeat
- [x] 1 player unit vs 1 enemy, one action/turn, placeholder primitives
- [x] Minimal HUD (turn owner, selection, HP) built in code
- [x] Win/loss terminal state (VICTORY/DEFEAT banner) _(originally scoped to M2; landed in M1)_
- **First playtest (2026-05-20):** wiring works, but the slice can't answer "is it fun?" — 1v1 / one ability / flat plane stripped out the depth needed to evaluate. Two findings:
  - The orchestrator was **bypassing the pure-C# combat math** (`DamageCalculator` / `ElementChart` / `ExecutionCalculator` existed but were never called). Re-routing through them unlocked elements + damage breakdown + damage preview in one change — see M1.5.
  - Most "tactics" feedback (squads, abilities, terrain, facing) is **M2-shaped** — no amount of polish on a 1v1 slice gets to fun. Pulled terrain forward; added facing as new backlog.
- [ ] **Success test:** "this is fun even with cubes." → tag `v0.1.0` _(deferred to post-M1.5 playtest)_

### M1.5 — Feel pass _(additive on top of M1; cheap wins from the playtest)_ ✅ **iteration 1 (2026-05-21)** · ✅ **iteration 2 (2026-05-21)**
**Iteration 1** — first response to the M1 playtest:
- [x] Orchestrator routes attacks through `DamageCalculator` + `ElementChart` (no more flat-damage bypass)
- [x] Units carry `CombatStats`, `ElementType`, `AbilitySpec` — Player = **Light**, Enemy = **Dark** (canonical Virtue ↔ Corruption matchup)
- [x] Gladius-style damage preview: HUD shows `DMG X (Divine Y)` + element matchup label (STRONG / WEAK / Neutral) on attack-valid hover
- [x] Floating HP bars over units (billboarded; drain right-to-left)
- [x] Path-preview visibility: brighter yellow line, floats above the ground plane

**Iteration 2** — second-playtest feedback (the 1v1 stayed boring, but specific Gladius affordances surfaced):
- [x] **Move + attack same turn** (Gladius pattern). Strict order: move (optional) → attack (optional) → end turn. Symmetric — enemy AI uses the same rule (also fixes the original AI bug where the enemy only stepped one tile per turn).
- [x] **End Turn input** — Tab (kb) / East button B/Circle (gp); persistent corner hint so it's discoverable.
- [x] **Path beyond range:** cursor stays MoveValid past `MoveRange`; clicking executes a partial move that stops at the reach limit. Path renders as bright yellow in-range + faded grey out-of-range so the truncation point is obvious. `ComputeReachableSteps` also handles "cursor on enemy from far away" — stops one tile short.
- [x] **HP-bar damage preview** — XCOM-style red overlay on the target's bar showing where HP would land after the forecasted Hit.
- [x] **Attack name + QTE type** in the top-right damage forecast (e.g. `Radiant Cleave · Swing Meter`).
- [x] **VICTORY/DEFEAT panel background** so the text reads against the battlefield.
- [x] **Element-coloured units** — `ElementType` plumbed through `UnitSpawnedEvent` → factory. Player Light = pale gold, Enemy Dark = deep violet. Shape still conveys faction (capsule/cube) for colour-blind readability.

- [ ] **Re-playtest** the second iteration. If fun → tag `v0.1.0`. If not → carry the punch-list into M2 (where squads / abilities / terrain / facing actually live).

### M2 — Real Combat _(depth, still placeholders)_
- [ ] Squads (multiple units/side); Speed-based turn order **+ turn-order UI** (so the player can plan around upcoming enemy turns — Gladius pattern)
- [ ] Abilities + MP cost; physical/special/support kinds; **risk/reward gradient** (stronger moves = harder QTE)
- [ ] **Pulled forward from M3:** grid elevation/terrain (high ground bonus). Identified in the M1 playtest as a fun-prerequisite — needed in M2 so positioning becomes a real choice.
- [ ] **Facing / flanking:** unit facing + back/side attack modifiers; facing arrow indicator. (New from M1 playtest — direct Gladius ask.)
- [ ] Win / loss / battle-end flow; basic enemy AI _(terminal state landed in M1; AI + multi-unit flow remains)_
- [ ] **Great Beast boss:** a 2×2 unit + **Corruption gauge** (purify-not-kill win condition)
- [ ] Additional QTE types (button mash / timed press / matching)
- [ ] Decompose god-controller → TurnSystem / AttackSystem / Hud (driven by need)
- [ ] Digimon-Survive stat-semantics pass _(element matchup UI shipped in M1.5)_
- [ ] → tag `v0.2.0`

### M3+ — Campaign _(after the loop is provably fun)_
- [ ] Region → boss template ([docs/STORY.md](docs/STORY.md)): traverse a region, then a
      Great Beast boss; purify → restore → next. Repeat ×7, then Paradeisos finale.
- [ ] Save / load; between-region progression
- [ ] Region restoration on purification (visual/state change of the region)
- [ ] **RPG customization layer (later):** items, equipment, skills, party building &
      recruitment. Explicitly deferred — design the data seams, build the UI later.
      _(Gladius-style move/equipment loadouts live here — direct M1 playtest ask.)_
- [ ] Audio + game juice; real art over placeholders (the art decision happens here)
- [ ] Menus, settings, build pipeline, release polish

## 6. Functionality Backlog _(unscheduled — reorder/triage freely)_

**Combat / QTE**
- [ ] QTE types: swing meter (have), button mash, timed press, matching/sequence
- [ ] Ability targeting (single / AoE / line); counterattacks / reactions
- [ ] "Execution" tier tuning (Fumble/Miss/Hit/Divine; optional 5th "LateHit" tier — deferred)
- [ ] Corruption-gauge variants per Great Beast (unique purify mechanics)

**Systems**
- [ ] Unit/ability data as ScriptableObjects (deferred until structure is proven)
- [ ] Command pattern for actions → enables **Undo Move** + future netcode ("every action is a Request")
- [ ] Damage-log / combat-report panel (spectator-clarity pillar)

**Parked — explicitly out of scope until the core loop is fun** _(see [docs/STORY.md](docs/STORY.md))_
- [ ] Side NPCs & questlines · branching allegiance choices · escape/riddle puzzles
- [ ] Fast-travel minecart survival minigame

**Tech / Repo**
- [ ] CI: build matrix, cache, test report
- [ ] CD: tagged player builds (Windows first)
- [ ] Automated formatting check (`.editorconfig` + `dotnet format` in CI)
- [ ] Lore-flavored rename pass (cosmetic, late)

**Long-shot / vision (not committed)**
- [ ] Local multiplayer · Online multiplayer (architecture kept request/command-friendly)
- [ ] Full overworld / meta layer
