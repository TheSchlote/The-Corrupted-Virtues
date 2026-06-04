# CLAUDE.md — agent guide for The Corrupted Virtues

A turn-based tactical RPG: a grid SRPG where attacks resolve through skill-based timed **QTE
action commands**, and you play a Guardian purifying the seven corrupted **Virtues** of Areti.
Combat is a **pure-C# core** with a thin, code-driven Unity layer.

## ⚠️ Keep the GitHub Project board in sync with the repo

Work is tracked on **GitHub Project #8 — "The Corrupted Virtues"**
(https://github.com/users/TheSchlote/projects/8). It is the **source of truth for what the game
does and will do.** `ROADMAP.md` and `docs/` are design / vision / history reference; the
**board is the live tracker** — do not let them drift. `gh` is authed with the `project` scope.

**Whenever you change the repo, reflect it on the board in the same session, via `gh`:**
- **Start** work on a tracked item → set its **Status** to `In Progress`.
- **Ship** work (merge to `main` / tag a release) → set the item to `Done` and **close its
  issue**. If the shipped feature set changed, update the board README's "What the game does
  today" section so it stays true.
- **New** work not yet tracked → create an issue, add it to the project
  (`gh project item-add 8 --owner TheSchlote --url <issue-url>`), and set its **Status** + **Phase**.
- Keep **dependencies** honest with the `blocked` label + a "Blocked by #N" line (e.g. campaign
  #21 ← story #31; animations #26 / formalized-UI #27 ← the art decision #25).

Two board fields drive it:
- **Status** — workflow: `Backlog → Up Next → In Progress → Done`.
- **Phase** — the roadmap arc, ordered: `Shipped → M3: Campaign & Depth → Art & Polish →
  Release → Vision & Parked`. This is a side project worked **in bursts**, so the roadmap is
  **phase-ordered, not dated** — don't add target dates unless asked.

Look up field / option IDs with `gh project field-list 8 --owner TheSchlote --format json`, then
set with `gh project item-edit --id <item> --field-id <field> --project-id <proj>
--single-select-option-id <opt>`.

The older CV boards (projects #1 / #3 / #4) and repos (`CorruptedVirtues-TacticsRPG`,
`Corrupted-Virtues`, `CV_Idle`) are **abandoned prior attempts — leave them alone.**

## Working in this repo
- **Gameplay logic is pure C#** — the `Combat`, `GridCore`, and `Battle` asmdefs contain **no
  `UnityEngine`**. The Unity layer (`Game` asmdef, e.g. `CombatSliceOrchestrator`) is a thin
  adapter that drives those systems and raises `CombatEvents`. Put new combat logic in the pure
  core, pinned by EditMode tests — not in the MonoBehaviour.
- **Tests are the safety net** (250+ EditMode tests). CI is license-blocked, so run them
  **locally, headless** via Unity batchmode:
  `Unity.exe -batchmode -nographics -projectPath <repo>/TheCorruptedVirtues -runTests
  -testPlatform EditMode -testResults res.xml -logFile t.log`
  (Unity `6000.3.15f1`; on this machine the Editor is `T:\Unity\6000.3.15f1\Editor\Unity.exe`).
  Only one Editor may hold the project at once — close the GUI Editor first, and don't chain two
  batchmode runs (the first must fully release the project lock before the second starts).
- **Content is data**: Ability / Unit / Map / Encounter ScriptableObjects under
  `Assets/_Project/Resources/Encounters`, generated from the pure-C# `EncounterLibrary` by the
  `Tools/TCV/Generate Content Assets` editor tool. Edit the library, then re-run the generator;
  a deep equality test pins catalog == library.
- **Branching** (ROADMAP §3): short-lived `feature/<milestone>-<short>` off `main` → PR → merge
  → delete. `main` always compiles with tests green.
- **Author via CLI / batchmode** — do all authoring yourself; don't ask the user to wire things
  up in the Unity Editor by hand.
