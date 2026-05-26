# The Corrupted Virtues — Roadmap

> **Living document.** Edit anything — checkboxes, scope, ordering, priorities. This is
> yours to redirect as the game evolves. Claude reads this for design/scope intent.
> World & story live in [docs/LORE.md](docs/LORE.md) and [docs/STORY.md](docs/STORY.md).
>
> _Last reviewed: 2026-05-25_

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
- [x] **Success test:** "this is fun even with cubes." — signed off 2026-05-21 after M1.5 iteration 2. → ready to tag `v0.1.0`.

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

- [x] **Re-playtest** the second iteration — signed off 2026-05-21. Remaining wants (unit animations, formalized UI system) deferred to M3+ because both are asset-coupled and wait for the art decision.

### M2 — Real Combat _(depth, still placeholders)_ ✅ **complete — tagged `v0.2.0` (2026-05-25)**

**Slice 1 — Squads + Speed-based turn order + turn-order UI** ✅ _shipped on `feature/m2-squads-turn-order` (2026-05-21)_
- [x] 2v2 squads with four distinct elements (Player: Light + Fire / Enemy: Dark + Water — surfaces 4 matchups in one fight)
- [x] Pure-C# `TurnOrder` with deterministic Speed-based ordering (lower Id breaks ties); 7 EditMode tests pinning the rule
- [x] Per-unit turn lifecycle (move + attack + end, same as M1.5 but per unit instead of per side)
- [x] Round-robin queue with new round when current empties
- [x] Turn-order UI strip (top of screen, chips coloured by element with faction badge; active chip highlighted)
- [x] Active-unit indicator (faint coloured disc under the unit whose turn it is)
- [x] HUD refactor: per-side HP text replaced with squad-roster line; redundant "Turn: X" text dropped (the active chip carries it)
- [x] Enemy AI per unit: nearest-living-player heuristic, same move+attack rule as player
- [x] Win condition: faction wipe (all of one side dead → other side wins)

**Slice 2 — Abilities + MP + button-mash QTE** ✅ _shipped on `feature/m2-abilities-mp-qte` (2026-05-22)_
- [x] Per-unit ability list (index 0 = free basic attack) + MP; cycle the active ability with **C** / gamepad **North**; HUD selector shows the ability, MP cost/pool, and QTE type + difficulty
- [x] MP model: spent on commit (a whiffed QTE still costs it), gated when unaffordable; whole-battle budget (starts full, no passive regen), basic attack free
- [x] Physical / Special / Support kinds: Support heals self or an adjacent ally via pure-C# `HealCalculator`; green heal forecast (no red HP overlay)
- [x] **Risk/reward gradient:** stronger swing abilities graded on a tighter Divine window (`ExecutionCalculator` parameterized by `QteDifficulty`; the painted meter zones track the grader)
- [x] **QTE framework:** `IExecutionMeter` generalized to a Tick-driven seam + a `QteType`→meter registry so types with different interactions coexist; **button mash** is the first new concrete type (fill-bar, difficulty-scaled press target)
- [x] Pure-C# core (MP / QTE-difficulty / button-mash / heal calculators) with 19 EditMode tests pinning it (77 total, all green)
- [x] Enemy AI uses its basic attack this slice (enemy ability/MP use deferred)

**Slice 3 — God-controller decomposition** _(pay down debt before terrain/facing/boss pile on — all three touch turn + attack flow)_ ✅ _shipped on `feature/m2-decompose-orchestrator` (2026-05-22)_
- [x] New pure-C# **`Battle` assembly** (refs `Combat` + `GridCore`) — the home for combat-on-a-grid logic that needs both base cores, so each base core stays an independent zero-dependency leaf
- [x] `Faction` / `UnitId` relocated into it; `CombatUnit` lifted out of the orchestrator's private nested class into a shared pure model
- [x] Logic split into focused, unit-tested pure systems: `BattleState` (roster / occupancy / win), `TurnSystem` (round queue + upcoming strip), `EnemyTurnPlanner` (AI heuristic → plan), `AbilityResolver` (damage/heal → outcome struct), `MovementRules` (reach)
- [x] `CombatSliceOrchestrator` slimmed **1005 → ~815 lines**: now a Unity adapter (input, move/QTE coroutines, cursor, event-raising) that drives the systems — still announces everything through `CombatEvents`, behavior unchanged
- [x] 32 new EditMode tests pinning the extracted logic (**109 total, all green**)

- [x] Abilities + MP cost; physical/special/support kinds; **risk/reward gradient** (stronger moves = harder QTE) _(slice 2)_
- [x] **Pulled forward from M3:** grid elevation/terrain — high ground = ×1.25 damage; folded into the `SituationalModifiers` seam + shown in the forecast. _(shipped on `feature/m2-terrain-elevation`, 2026-05-24)_
- [x] **Facing / flanking:** auto-facing (face last step / face target on attack) + back ×1.5 / side ×1.25 / front ×1.0 + facing arrow. Single-target attacks are directional; AoE-exempt via `AbilitySpec.IsAreaOfEffect`. _(shipped on `feature/m2-facing-flanking`, 2026-05-24)_
- [x] **AoE targeting:** Chebyshev burst (`AbilitySpec.AoeRadius`); pure `AreaOfEffect` (burst tiles + target collection) + `AbilityResolver.ResolveArea` (high ground, no flanking — non-directional); affected tiles highlighted on hover, multi-target resolve in one win check. Flame Nova added. _(shipped on `feature/m2-finish`, 2026-05-24)_
- [x] Win / loss / battle-end flow; **smarter enemy AI** — ability/MP use (best affordable forecast damage) + focus-fire (weakest adjacent target). Terminal state landed in M1; per-unit multi-unit flow in slice 1. _(AI shipped on `feature/m2-finish`, 2026-05-24)_
- [x] **Great Beast boss:** mobile 2×2 unit (footprint pathfinding + adjacency) + **Corruption gauge** (its HP pool; purify-not-kill → PURIFIED banner) + purple corruption aura. _(shipped on `feature/m2-great-beast`, 2026-05-24; playtested — "barely won")_
- [x] Additional QTE types — framework + **button mash** _(slice 2)_
- [x] More QTE types: **timed press + matching** — new `IExecutionMeter` types + pure graders (`TimedPressCalculator` / `MatchingCalculator`); matching reads directional input. Lance of Dawn / Cinder Combo added. _(shipped on `feature/m2-finish`, 2026-05-24)_
- [x] Decompose god-controller → pure-C# `Battle` systems: `TurnSystem` + `AbilityResolver` (the "AttackSystem") + `BattleState`/`EnemyTurnPlanner`/`MovementRules`; HUD was already split into presenters _(slice 3)_
- [x] Digimon-Survive stat-semantics pass — **spec alignment**: `docs/DESIGN.md` brought in line with the shipped math (execution tiers, situational terms) + a canonical stat-role table; `CombatStats` documented. No balance change — constants kept at playtested values. _(shipped on `feature/m2-finish`, 2026-05-24)_
- [x] → tagged `v0.2.0` (2026-05-25) — annotated tag on the M2 merge `391fcd9`; 202 EditMode tests green at tag time, playtested.

### M3+ — Campaign _(after the loop is provably fun)_
- [ ] Region → boss template ([docs/STORY.md](docs/STORY.md)): traverse a region, then a
      Great Beast boss; purify → restore → next. Repeat ×7, then Paradeisos finale.
- [ ] Save / load; between-region progression
- [ ] Region restoration on purification (visual/state change of the region)
- [ ] **RPG customization layer (later):** items, equipment, skills, party building &
      recruitment. Explicitly deferred — design the data seams, build the UI later.
      _(Gladius-style move/equipment loadouts live here — direct M1 playtest ask.)_
- [ ] Audio + game juice; real art over placeholders (the art decision happens here)
- [ ] **Unit animations** (idle / walk / hit / attack / death). Deferred from M1.5 by design — animations are inherently asset-coupled, so they wait for the art decision rather than getting built against primitives and thrown away.
- [ ] **Formalized UI system** (consistent panels, layout grammar, visual hierarchy). The M1.5 HUD has elements scattered in screen corners because each was added in isolation; a real UI framework should match the eventual visual style, so it waits with the art decision.
- [ ] Menus, settings, build pipeline, release polish

## 6. Functionality Backlog _(unscheduled — reorder/triage freely)_

**Combat / QTE**
- [x] QTE types: swing meter, button mash, timed press, matching — all shipped _(line / sequence / charge variants later)_/sequence
- [ ] Ability targeting (single / AoE / line); counterattacks / reactions
- [ ] "Execution" tier tuning (Fumble/Miss/Hit/Divine; optional 5th "LateHit" tier — deferred)
- [ ] Corruption-gauge variants per Great Beast (unique purify mechanics)

**Systems**
- [x] **Data as ScriptableObjects** — Ability/UnitArchetype/Map/Encounter SOs (Unity layer) convert to the pure specs via `ToSpec`, so the combat core never sees an asset; loaded at runtime by `EncounterCatalog` (falls back to `EncounterLibrary`), seeded from the code library by an Editor generator, and pinned by a deep catalog-vs-library equality test. Content is Editor-authorable now. Also on that branch: `GreatBeast`→`Boss` code rename (lore keeps "Great Beast"); a mirror-7v7 "Elemental Clash" sandbox of all seven elements. _(shipped on `feature/m3-data-scriptableobjects`, 2026-05-25)_
- [x] **Varied battle maps** — maps as data (`BattleMapSpec`/`MapLibrary`, mirroring the encounter seam); per-encounter loading; **impassable obstacles** (a pure `ObstacleMap` folded into the pathfinding occupancy — no pathfinder signature change); variable grid sizes + obstacle rendering + per-encounter terrain rebuild. Five maps: Plateau (8×8, boss), Ruined Hall (10×8, wall + chokepoints), Pillared Hall (10×10, 2×2 pillars + beast), Highland Siege (9×8, multi-level incl. a level-2 peak), The Narrows (11×7 serpentine corridor). Also grounded unit views by mesh half-height (cube enemies had floated). _(shipped on `feature/m3-varied-maps` + `feature/m3-more-maps`, 2026-05-25)_
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
