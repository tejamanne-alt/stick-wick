# DEVLOG

## 2026-11-03: Prototype 0.1 (week 1 goals)

**Goal:** one grey-box room where tapping left and right feels great.

Done:
- Blender pipeline, fully scripted: stickman mesh (1,222 vertices), a 12-bone rig and 13 clips (idle, run, jab, cross, front kick, roundhouse, back kick, flying kick, dodge, hit front, hit back, knockdown, windup). Poses are authored as side-on angles; the script grounds the feet automatically. Preview sheets are in `docs/previews/`.
- Unity runtime:
    - clip player built on Playables, so no Animator Controller asset is needed
    - gesture input with a 0.25 s input buffer
    - hero targeting, auto-dash and combo
    - context moves: back kick, flying kick at enemies above, finisher
    - counter and perfect dodge, both into bullet time
    - enemy AI with 2 attack tokens and red telegraphs
    - five spawn kinds
    - hit-stop and slow-mo controller
    - camera director with framing, shake and the orbit kill cam
    - ink splashes, trails, impact flash, synthesized hit sounds
    - IMGUI HUD with grade and restart
- Editor: FBX import rules; the whole scene (room, staircase, landing, doors, spawns, hero, systems) is built from code; Android build method.
- CI: GameCI workflow builds an APK on push.
- Verified: all scripts compile against Unity 6.3.27f2 assemblies.

Not verified yet (needs a Unity license):
- the in-editor run
- the FBX import facing (it is auto-calibrated at runtime from the front-kick clip)
- the device build

Next:
1. Unity license, then the first APK.
2. Tune dash speed, hit timings and the kill cam from the playtest video.
3. Style test: 3 moves in the final ink look.

## 2026-11-04: Look matched to the reference video, landscape only

The reference video set the look: solid black stickmen with thick limbs and big round heads, side-on on a soft grey backdrop. The ground is black and the fallen pile up on it. Hits flash with white slash arcs and light bursts, with black spatter.

- Stickman regenerated: limbs and torso about 60% thicker, head 0.13 → 0.165 m radius.
- Scene restyled: everything is unlit black silhouette. A grey gradient backdrop follows the camera. The lights are removed (cheaper on phones). Camera is lower and more side-on (FOV 32).
- FX:
    - white limb trails, which read as slash arcs
    - additive light bursts behind heavy hits and kills
    - round ink blots instead of square particles
- Defeated enemies stay on the ground; at most 30 bodies, oldest removed first.
- Landscape only: player settings are applied on editor open and on every build; the game also locks orientation at runtime.
- The scene is versioned (`SceneVersion = 2`), so an open editor rebuilds it automatically after a pull.
- Swords: later, as planned.

## 2026-11-04: Real martial arts from motion capture

Twelve of the 15 moves are now real human motion from the CMU motion-capture database, which is free for commercial use; the credit is in `tools/mocap/README.md`.

| Moves | Source |
| --- | --- |
| idle (boxing guard), jab, cross | boxing sessions |
| front kick, roundhouse, back kick, knee | kicking sessions |
| flying kick, spin kick | acrobatic jump kicks |
| dodge | a real backflip |
| knockdown | a real fall |
| run | a real run |

- `tools/blender/retarget.py` maps the actor onto the 12-bone stickman:
    - the hips and chest take full orientation, so twist and turns come through
    - the limbs follow the actor's limb directions
    - the hips height keeps the feet on the ground
    - each move is aimed along the screen, or backwards for the back kick
- Punches are straightened to full extension at impact, so they read side-on.
- Impact frames are set to each strike's maximum reach.
- New attacks:
    - knee (in the combo: jab, cross, knee, front kick, jab, cross, roundhouse)
    - spin kick, which alternates with the roundhouse as the cinematic finisher
- Dodge is a backflip, carried in the swiped direction; ceiling enemies flip down.
- Still hand-keyed: hit reactions and the enemy wind-up.

## 2026-11-04: Sword and Gun-Fu styles

You switch styles with a swipe down (Fists → Sword → Gun-Fu). Controls stay the same; the moves change.

- **Rig:** hand bones added. A hand's angle is its weapon's direction, so each pose aims the blade or barrel exactly. Motion-captured clips carry the hand rigidly with the forearm.
- **Sword (hand-keyed, reference style):**
    - idle; rising cut, overhead cut, lunging thrust as the finisher
    - back slash behind without turning; leaping overhead slam on swipe up
    - cuts cleave everyone in reach; a wide white tip trail draws the crescent slash arcs
- **Gun-Fu (hand-keyed):**
    - idle at a compressed ready position; shoot forward with recoil; shoot behind without turning
    - pistol-whip, then grapple and throw, followed automatically by a slow-mo finishing shot on the downed enemy
    - 12-round magazine with an automatic reload
    - each shot draws a white tracer, a muzzle flash and a generated gunshot sound
- **Enemies:** from wave 2, 30–45% carry swords and attack with telegraphed overhead cuts.
- **Mocap note:** CMU's swordplay (02_07–09) and shooting (79_96, 80_03) recordings were tried. They are slow demonstrations, so the weapon moves are hand-animated for big readable arcs instead. Their joint caches are kept in `tools/mocap/cache` for later use.
- Scene version 4.
