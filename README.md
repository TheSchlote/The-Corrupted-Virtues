# The Corrupted Virtues

A turn-based tactical RPG — a love letter to LucasArts' *Gladius* with an original
"Corrupted Virtues" spin. Grid combat resolved with skill-based timed **QTE** action
commands; deterministic, explainable damage. Built in Unity with a strict pure-C# core
separated from the Unity presentation layer.

> Status: **early foundation.** Placeholder primitives only — the priority is a modular,
> asset-agnostic architecture, not art. See [ROADMAP.md](ROADMAP.md).

## Tech

- **Unity** 6000.3.15f1 · Universal Render Pipeline · new Input System
- **Language** C# — pure logic assemblies have no `UnityEngine` dependency
- Targets PC (keyboard/mouse + gamepad)

## Repository layout

```
TheCorruptedVirtues/              Unity project
  Assets/_Project/
    Scripts/Core/Combat/          pure C# — damage/element/execution math   (asmdef: TheCorruptedVirtues.Combat)
    Scripts/Core/Grid/            pure C# — grid + pathfinding              (asmdef: TheCorruptedVirtues.GridCore)
    Scripts/Core/Battle/          pure C# — combat-on-a-grid systems        (asmdef: TheCorruptedVirtues.Battle)
    Scripts/Game/                 MonoBehaviours / Unity layer             (asmdef: TheCorruptedVirtues.Unity)
    Scripts/Game/Data/            ScriptableObject content types (convert to the pure specs)
    Scripts/Editor/               Editor-only content asset generator      (asmdef: TheCorruptedVirtues.EditorTools)
    Resources/Encounters/         authored SO content (encounters · maps · units · abilities)
    Tests/EditMode/               NUnit characterization tests             (asmdef: TheCorruptedVirtues.Tests.EditMode)
docs/                             DESIGN.md · LORE.md · STORY.md
ROADMAP.md                        living plan & milestones (edit freely)
```

## Getting started

1. Install **Unity 6000.3.15f1** (via Unity Hub) and **Git LFS** (`git lfs install`).
2. Clone, then open the `TheCorruptedVirtues/` folder in Unity Hub.
3. Let Unity import/recompile. Run tests via **Window → General → Test Runner → EditMode**.

## Documentation

- [ROADMAP.md](ROADMAP.md) — milestones, locked design decisions, backlog (the living plan)
- [docs/DESIGN.md](docs/DESIGN.md) — combat-system design vision (Execution, elements, damage)
- [docs/LORE.md](docs/LORE.md) — world & lore bible
- [docs/STORY.md](docs/STORY.md) — minimal campaign spine

## Branching & releases

`main` is the always-green source of truth. Feature branches → PR → merge → delete.
CI builds/tests every push to `main`; player builds ship only from version tags
`vX.Y.Z`. Prior prototypes are preserved as `archive/*` tags.

## Credits

Moral Support Studios — Joey Schlote. Originated as a homebrew tabletop campaign.
