# Project INK (working title)

A cinematic stickman fighting game for Android and iOS, built with Unity 6.3 LTS and Blender.
The design is in the plan doc; this repo is the build.

## Prototype 0.1: what's in it

One grey-box room with a staircase and landing. Three waves of grunts come from the left
door, the right door (under the landing), down the stairs, out of the background, and
drop from the ceiling.

| Input (touch) | Key | Action |
| --- | --- | --- |
| Tap left / right half | A / D or arrows | Dash to the best enemy on that side and strike (combo: jab, cross, front kick, jab, cross, roundhouse) |
| Swipe up | W / Up | Flying kick |
| Swipe left / right | Q / E | Dodge (just before a hit = bullet time) |
| Tap while an enemy glows red | | Counter (slow-mo, knockdown) |
| | R | Restart |

The game picks the move for you:
- an enemy close behind gets a back kick, without turning around
- an enemy above you on the stairs gets a flying kick
- a blow that will finish an enemy becomes a roundhouse

Cinematic layer:
- hit-stop of 2–8 frames on every hit
- camera shake and a zoom punch on heavy hits
- a one-frame impact flash, ink splashes and limb motion trails
- the last enemy of each wave triggers a slow-mo orbit "kill cam" that returns to the gameplay side

Only 2 enemies may attack at once (attack tokens). The rest circle and wait.

## Layout

| Path | What |
| --- | --- |
| `tools/blender/build_stickman.py` | Generates the stickman mesh, rig and all 13 animation clips → `Assets/Art/Stickman/Stickman.fbx` (+ `stickman_clips.json` timing, + preview sheets in `docs/previews/`) |
| `Assets/Scripts/Core` | Clip player (Playables), time controller (hit-stop / slow-mo), camera director, gesture input, ink FX + synthesized hit sounds |
| `Assets/Scripts/Combat` | Fighter base, hero controller, enemy AI + attack tokens, attack tuning |
| `Assets/Scripts/Level` | Wave director (spawns, kill cam, score, grade), fighter factory |
| `Assets/Scripts/UI` | IMGUI prototype HUD |
| `Assets/Scripts/Editor` | FBX import rules; `PrototypeBuilder` builds the scene, materials and the Android APK from code |
| `tools/compile_check.sh` | Compiles all scripts against Unity 6.3's assemblies (no license needed) |
| `.github/workflows/android.yml` | Builds the APK on every push to `main` |

## Regenerate the art

```bash
pip install bpy==5.2.3 pillow
python tools/blender/build_stickman.py --preview
```

## Open and play on a Mac (or PC)

1. Clone the repo, then in Unity Hub choose **Add → Add project from disk** and pick the repo folder. Use Unity **6000.3.27f2** (6.3 LTS). A newer 6.3 patch also works: let Hub upgrade it.
2. The first import takes a few minutes. If a dialog asks to enable the Input System backends, either answer works; the code supports both.
3. When the import finishes, the prototype scene is built and opened automatically (`Assets/Scenes/Prototype.unity`). If it isn't, run **Project INK → Build Prototype Scene**.
4. Set the Game view to a landscape size (for example 1920×1080), then press **Play**. Use A/D or click the left/right half to strike, W for the flying kick, Q/E to dodge, and R to restart.
5. Commit and push what Unity generated (`*.meta` files, `ProjectSettings/`, `Packages/packages-lock.json`), so the asset IDs stay stable for everyone.

## Build

In the Unity editor: **Project INK → Build Prototype Scene**, then press Play. Or use **Build Android APK**.

Headless:

```bash
unity-editor -batchmode -nographics -projectPath . -executeMethod ProjectInk.EditorTools.PrototypeBuilder.CI_BuildAndroid -logFile -
```

The APK lands in `build/Android/ProjectInk.apk`.

### CI secrets (one-time, done by the repo owner)

The workflow needs a Unity license. In the GitHub repo, go to **Settings → Secrets and variables → Actions** and add:
- `UNITY_EMAIL` and `UNITY_PASSWORD`: your Unity account
- `UNITY_LICENSE`: the contents of your activated `.ulf` license file. See the GameCI activation guide: https://game.ci/docs/github/activation

## Status

Code compiles against Unity 6.3.27f2 (`tools/compile_check.sh`). The first in-editor run and device build are waiting on the Unity license.
