# Project INK (working title)

A cinematic stickman fighting game for Android and iOS, built with Unity 6.6 and Blender.
The design is in the plan doc; this repo is the build.

## Prototype 0.3: what's in it

A film-noir cut-away of a two-floor building:
- architectural concrete walls
- high windows onto a night skyline
- hanging lamps casting warm cones of light that sway and flicker
- pipes, an exit sign, haze and drifting dust
- vignette and film grain over the picture

Each floor is a **chapter**. Each wave brings in a new armed opponent, whose weapon you take and use: brawlers → knife throwers → pipe fighters → swordsmen → gunners and the **Brute** (with a boss entrance).

| Input (touch) | Key | Action |
| --- | --- | --- |
| Tap left / right | A / D | Strike that side: dash, combo, and context moves (below) |
| Tap as a knife flies in | | **Catch it** and throw it back |
| Swipe up | W | Leap; **climb** the ledge (jump, hang by the hands, pull up) |
| Swipe sideways | Q / E | Dodge. Into a red flash: **vault** over the attacker. Towards an enemy: **slide kick**. Towards a wall: **wall flip**. Otherwise a backflip |
| Swipe down | S / F | Empty-handed: **duck** (knives and shots pass over). Armed: **throw** the weapon |
| Two-finger tap | Space | **Rampage** (when Flow is full) |
| Tap fast when grabbed | | **Break free**, ending in a reversal throw |
| | Tab | Testing: cycle weapons |

Context moves:
- An enemy on the floor next to you: **stomp** it, or **giant-swing** it by the legs into the others.
- Up close with fists: **grab and throw**. A thrown enemy knocks down whoever it hits (human bowling).
- An enemy on the upper floor: run to the ledge and **climb**, or leap.
- An enemy below: **dive** down.

Weapons:
- **Knives:** stab up close, throw at range; catch more.
- **Pipe:** heavy staff swings.
- **Sword:** cleaving cuts, back slash, leaping slam.
- **Gun:** shots, a shot behind without turning, pistol-whip, throw, finishing shot.

All of them wear out, and are then thrown as a spinning kill.

Enemies:
- Brawlers punch, kick and **grab**.
- Throwers hurl knives; pipers swing pipes.
- Swordsmen cut; gunners aim with a laser sight.
- The Brute shrugs off light hits.

Enemies climb the stairs, **hang from the ledge and pull up**, flip off the ledge, and get up after knockdowns.

The loop:
- Wall splats and drops off the ledge are kills.
- Every kill leaves the enemy's colour on the building.
- A **style rank** (D → SSS) multiplies your score.
- Endless chapters; best score and floor are saved.

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
