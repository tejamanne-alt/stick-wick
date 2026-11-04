# Project INK (working title)

A cinematic stickman fighting game for Android and iOS, built with Unity 6.6 and Blender.
The design is in the plan doc; this repo is the build.

## Prototype 0.4: what's in it

**Cinematic opening** (0.4.1): the game starts on a black screen and the letterbox bars part like curtains.
1. A slow crane shot over the building, with the title card.
2. A hard cut to floor level: the hero crashes through the skylight and lands in slow motion. The lamps swing.
3. A push-in on the hero as he beckons, with the chapter card in the lower bar.
4. The bars slide away, **FIGHT!**, and you have control.

Tap anywhere to skip. A restart plays a short 1.5-second version. The code is in `Assets/Scripts/Level/OpeningCinematic.cs`; the title text is set there too.

A **black-and-white film-noir building**:
- grey architectural concrete and deep blacks
- white lamplight that sways and flickers
- moonlight shafts through the tall window
- a silver night skyline
- white edge highlights, haze, dust, vignette and grain

Only the fighters and their ink are in colour.

Each floor is a **chapter**. Each wave brings in a new armed opponent, whose weapon you take: brawlers → knife throwers → pipe fighters → swordsmen → gunners → the **Brute**.

### Sound

The sounds are real studio recordings:
- punches, kicks, swords, metal, bone cracks
- concrete footsteps
- grunts and death cries
- doors, glass, wood
- room tone and city rain

Cinematic hits, booms and risers sit on top for the big moments. In slow motion everything drops in pitch and is muffled, with a reverse whoosh going in. The boss brings a low drone. A 20-second demo is in `docs/previews/sound_demo.mp3`.

How it's built:
- `Assets/Scripts/Core/Sound.cs` is the mixer.
- The bank is built by `tools/audio/build_audio.py`, which trims, layers and normalises the clips into `Assets/Resources/Audio/`.
- Credits are in `Assets/Resources/Audio/CREDITS.txt`. The Oculus Audio Pack is CC BY 4.0, so its credit line must go in the game's credits.

### Controls

**Classic** (the default, as in the first prototypes). Press **C** to switch to two-thumb; your choice is saved.

| Touch | Keys | Action |
| --- | --- | --- |
| Tap the left / right half | A / D, ← / → | Strike that way; the hero dashes in to the nearest enemy |
| Press and **hold** a half | Hold A / D | Run that way (to reach ropes, bars, walls and pickups) |
| Swipe up | W / ↑ | Jump kick. Context: grab a rope or bar, run up a wall into a corkscrew, climb the ledge |
| Swipe left / right | Q / E | Dodge that way. Into a red flash: vault. Toward an enemy: slide kick. Toward a wall: wall flip |
| Swipe down | S / F | Duck, or throw the weapon in hand |
| Two-finger tap | Space | Rampage (when Flow is full) |
| Tap as a knife flies in | | Catch it and throw it back |
| Tap fast when grabbed | | Break free with a reversal throw |
| | Tab, 1/2/3 | Testing: give a weapon |

**Two-thumb** (C): a floating stick on the left 40% of the screen runs (flick up to jump); on the right, tap = strike, swipes as above. Keys: A/D run, J strike, Space/W jump, L/Shift dodge, E Rampage.

### Things to use

- **Ropes** (one over the hall, one over the upper floor): jump to grab. Hold the stick to pump the swing; your feet knock down whoever they pass. Tap or swipe to let go into a flying kick.
- **Steel bars** (three): grab one and do giant circles round it, kicking everyone in reach. You dismount into a flying kick.
- **Walls:** run at one and jump. The hero runs up it and spins off in a corkscrew flip.
- **Crates:** stand on them, or strike one to kick it into enemies like a bowling ball.

Also:
- **In the air:** air control, air kicks.
- **Ledge:** hang and climb.
- **Enemies:** grab-and-throw (human bowling), stomp and giant swing on the floor.
- **Weapons:** knives, pipe, sword, gun. All wear out and are then thrown as spinning kills.

The loop:
- Style rank (D → SSS) multiplies your score.
- Endless chapters; saved bests.

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
