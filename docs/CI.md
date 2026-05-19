# CI Setup (GameCI)

CI compiles the Unity project and runs the EditMode tests on every push/PR to
`main` via [`.github/workflows/ci.yml`](../.github/workflows/ci.yml).

It needs **one secret** — a Unity license — set up once.

## One-time: get the Unity license into a repo secret

We use the standard GameCI **Personal license** activation flow.

1. **Generate an activation file.** GitHub repo → **Actions** tab → **Unity
   Activation (one-time)** workflow → **Run workflow**. When it finishes,
   download the **`Manual Activation File`** artifact and unzip it — you get a
   `.alf` file.
2. **Convert it to a license.** Go to <https://license.unity3d.com/manual>,
   sign in with your Unity account, upload the `.alf`, pick **Unity Personal**,
   and download the resulting **`.ulf`** license file.
3. **Store it as a secret.** GitHub repo → **Settings → Secrets and variables →
   Actions → New repository secret**:
   - Name: `UNITY_LICENSE`
   - Value: the **entire contents** of the `.ulf` file (open it in a text
     editor, copy everything including the XML).
4. Re-run CI (push, or **Actions → CI → Run workflow**). It should go green.

> Never commit `.alf` / `.ulf` files — they're git-ignored for safety.

## Version caveat

`ci.yml` uses `unityVersion: auto`, which reads `6000.3.15f1` from
`ProjectSettings/ProjectVersion.txt`. GameCI runs the Editor from prebuilt
`unityci/editor` Docker images; a very new Unity 6 patch may not have an image
yet. If a run fails with an "image not found / unknown tag" error, set
`unityVersion:` in `ci.yml` to the nearest available `6000.3.x` from the GameCI
versions list and re-run. (EditMode tests are pure logic, so a nearby 6000.3
patch is fine for CI even though local dev stays on 6000.3.15f1.)

## What CI does / doesn't cover

- ✅ Compiles all four assemblies; runs the EditMode characterization tests.
- ❌ Does not (yet) run PlayMode tests or make player builds — add when needed.
- ❌ Cannot judge "is it fun" or visuals — that stays a human checkpoint.
