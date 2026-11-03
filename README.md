# Project INK (working title)

A cinematic stickman fighting game for Android and iOS, built with Unity 6.6 and Blender.
The design is in the plan doc; this repo is the build.

## Prototype 0.2: what's in it

A cut-away of a two-floor building, seen side-on:
- a ground floor and an upper floor with an open ledge
- a staircase, a back door, a ceiling vent
- doors on both floors

Enemies come in colours, so you can read a crowd at a glance. They pour in from every door, climb the stairs, leap off the ledge and drop from the vent. Every fight leaves its colours on the floors and walls.

| Input (touch) | Key | Action |
| --- | --- | --- |
| Tap left / right half | A / D or arrows | Dash (or leap or dive to another floor) to the best enemy on that side and strike |
| Swipe up | W / Up | Leap attack |
| Swipe left / right | Q / E | Dodge. Into an attacker's red flash, it becomes a slow-mo vault over them, landing at their back |
| Swipe down | S / Down / F | Throw the weapon in hand (a spinning kill) |
| Two-finger tap (Flow full) | Space | RAMPAGE: 5 s of slowed time; every tap blinks to an enemy for a one-hit finisher |
| Tap while an enemy glows red | | Counter |
| | Tab, 1/2/3 | Testing: give the hero a weapon |
| | R | Restart |

| Enemy | Colour | What it does | What it leaves |
| --- | --- | --- | --- |
| Brawler | teal, olive, ochre, violet, rust, green | Punches and kicks | |
| Swordsman | crimson | Overhead cuts | A sword (10 cuts) |
| Gunner | steel blue | Keeps its distance; a red laser sight, then a shot (dodge it) | A pistol (8 shots) |
| Brute (every 5th wave) | deep red | A giant; shrugs off light hits | |

Weapons wear out, and then the hero throws them.

The loop:
- Hits fill the **Flow** meter, which powers Rampage.
- A **style rank** (D → C → B → A → S → SS → SSS) rises with varied moves and environment kills (wall splats, drops off the ledge), and falls when you're hit. Your rank multiplies your score.
- **Endless floors:** 5 waves each, getting harder.
- Best score and floor are saved.

Cinematic layer:
- Motion-captured martial arts, hand-keyed weapon moves, hit-stop, white slash and limb trails, light bursts, coloured ink.
- Letterbox bars slide in during every slow-mo.
- Every wave ends with a slow-mo orbit "kill cam".

## Layout

| Path | What |
| --- | --- |
| `tools/blender/build_stickman.py` | Generates the stickman mesh, rig and all 13 animation clips → `Assets/Art/Stickman/Stickman.fbx` (+ `stickman_clips.json` timing, + preview sheets in `docs/previews/`) |
| `Assets/Scripts/Core` | Clip player (Playables), time controller (hit-stop / slow-mo), camera director, gesture input, ink FX + synthesized hit sounds |
| `Assets/Scripts/Combat` | Fighter base, hero controller, enemy AI + attack tokens, attack tuning |
| `Assets/Scripts/Level` | Wave director (spawns, kill cam, score, grade), fighter factory |
| `Assets/Scripts/UI` | IMGUI prototype HUD |
| `Assets/Scripts/Editor` | FBX import rules; `PrototypeBuilder` builds the scene, materials and the Android APK from code |
| `tools/compile_check.sh` | Compiles all scripts against Unity 6.6's assemblies (no license needed) |
| `.github/workflows/android.yml` | Builds the APK on every push to `main` |

## Regenerate the art

```bash
pip install bpy==5.2.3 pillow
python tools/blender/build_stickman.py --preview
```

## Open and play on a Mac (or PC)

1. Clone the repo, then in Unity Hub choose **Add → Add project from disk** and pick the repo folder. Use Unity **6000.6.8f1** (6.6).
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

Code compiles against Unity 6000.6.8f1 (`tools/compile_check.sh`). Input System 1.20.1 and the built-in shaders the prototype uses ship with that version. The first in-editor run is next.
