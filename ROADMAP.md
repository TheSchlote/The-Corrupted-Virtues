# The Corrupted Virtues — Roadmap

> **Living document — design & scope intent.** Edit anything — scope, ordering,
> priorities, locked decisions. Claude reads this for design intent.
>
> **Work tracking now lives in [GitHub Issues & Projects](https://github.com/TheSchlote/The-Corrupted-Virtues/issues), not here.**
> The milestone checklists and backlog that used to live in this file have moved
> to issues so they can be triaged, ordered, and tracked on a project board.
> See [§5 Milestones & Backlog](#5-milestones--backlog-github-issues) for the index.
> World & story live in [docs/LORE.md](docs/LORE.md) and [docs/STORY.md](docs/STORY.md).
>
> _Last reviewed: 2026-06-02_

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

## 5. Milestones & Backlog → GitHub Issues

The detailed task checklists and functionality backlog that used to live here have
**moved to [GitHub Issues](https://github.com/TheSchlote/The-Corrupted-Virtues/issues)**
so they can be triaged, ordered, and tracked on a
[project board](https://github.com/TheSchlote/The-Corrupted-Virtues/projects).
Completed milestones are preserved as **closed** issues for history.

**Milestones**

| Milestone | Status | Issue |
|---|---|---|
| M0 — Foundation | ✅ complete (`v0.x` plumbing) | [#11](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/11) |
| M1 — Vertical Slice | ✅ complete — tagged `v0.1.0` | [#12](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/12) |
| M1.5 — Feel Pass | ✅ complete | [#13](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/13) |
| M2 — Real Combat | ✅ complete — tagged `v0.2.0` | [#14](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/14) |
| M3+ — Campaign | 🔜 open | [#10](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/10) |

**Backlog** _(unscheduled — triage on the board)_

| Area | Issue |
|---|---|
| Combat / QTE | [#15](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/15) |
| Systems | [#16](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/16) |
| Tech / Repo _(incl. `UNITY_LICENSE` CI secret — see [docs/CI.md](docs/CI.md))_ | [#17](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/17) |
| Parked _(out of scope until the loop is fun)_ | [#18](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/18) |
| Long-shot / Vision _(not committed)_ | [#19](https://github.com/TheSchlote/The-Corrupted-Virtues/issues/19) |

> Add new work as a GitHub issue and label it (`milestone`, `backlog`, `combat`,
> `systems`, `tech`, `campaign`, `parked`, `vision`). Keep this file for **design
> intent** (§0–§4); let the board carry the **task state**.
