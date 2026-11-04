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

## 2026-11-04: Gameplay redesign (0.2)

Your feedback was that the gameplay wasn't good, along with a second reference: the early-2000s Flash stickman fights. I took the qualities of that reference rather than its content: vertical play in a building cut-away, colourful crowds, improvised weapons, acrobatics.

- **Arena:** a two-floor cut-away:
    - light interior back wall, black floors and outer walls
    - an open ledge on the upper floor
    - a staircase in a lane behind the fight lane, so ground fights pass in front of it
    - doors on both floors, a back door, a ceiling vent
- **Enemies** rewritten, with four colour-coded types: brawler, swordsman, gunner (laser sight, then a shot), and brute (every 5th wave).
    - Up to 3 attack at once (attack tokens).
    - They path between floors: up the stairs lane, or a flip off the ledge.
    - They get up after knockdowns and taunt while waiting.
    - Wall splats and drops off the ledge are kills.
    - The fallen leave colour stains on the floors and walls.
- **Weapons are earned:** swordsmen and gunners drop their weapons, and the hero grabs them on the run. A spent weapon is thrown as a homing, spinning kill; swipe down throws it early.
- **Vault:** a dodge into an attacker's red flash is a slow-mo front flip over them, landing at their back.
- **Floors:** the hero leaps up to the upper floor, or dives down from it, by striking at an enemy there.
- **Rampage:** the Flow meter fills with action. When it's full, a two-finger tap gives 5 s of slowed time where each tap blinks to an enemy for a one-hit finisher.
- **Hooks:**
    - a live style rank (D–SSS) that multiplies your score; variety counts
    - endless floors of 5 waves with rising difficulty
    - saved best score and floor
- **HUD:** health, Flow and Rampage prompt, weapon uses, rank letter, floor and wave, letterbox bars in slow-mo, results with bests.
- **New animations:** front-flip vault, weapon throw, get-up, taunt, gunner aim.
- Scene version 6.

## 2026-11-04: Abilities from the reference, chapters, premium building (0.3)

I re-studied the Xiao Xiao 3 reference frame by frame and built the abilities it shows, as original mechanics:
- **Ledge hang and climb:** fighters jump, hang by their hands under the upper floor's edge and pull up. Enemies use it too, and can be knocked off while hanging.
- **Grab and throw:** up close with fists. The thrown enemy becomes a projectile that knocks down others (human bowling).
- **On-floor finishers:** stomp an enemy lying on the floor, or giant-swing it by the legs, sweeping others before letting go.
- **Knife throwers:** knives in flight. Duck them, or tap as one arrives to catch it and throw it back.
- **Grappling:** brawlers can grab and hold the hero. Tap 4 times to break free with a reversal throw.
- **Wall flip and slide kick:** two more meanings for the dodge swipe.
- **Weapon progression:** knives and pipes join the sword and gun; each chapter's waves introduce them in turn.
- **Boss entrance:** a camera close-up and slow motion.

New hand-keyed clips: hang, climb_up, stomp, swing_hold, duck, slide_kick, grab_attack, hold_victim, struggle.

**Premium building:** `EnvironmentBuilder` generates and places everything in code:
- textures: architectural concrete (formwork seams, tie holes, drips, grime) and a night skyline with a moon and lit windows
- high clerestory windows, so fighters always stand against lit concrete
- swaying, flickering lamps with additive light cones
- pipes, balusters, trims, an exit sign, crates, haze and dust
- a vignette and film grain over the HUD

A design mock-up is in `docs/previews/building_mockup.png`. The first version of the mock-up showed the black hero disappearing against dark windows and the black back doorway. That led to raising the windows above head height and lighting the corridor.

Scene version 7.

## 2026-11-04: Chibi proportions and smoother animation

- **New body:** about 1.45 m tall instead of 1.85 m.
    - head radius 0.21 m
    - a round belly and chest
    - short, thick limbs
    - little round feet
    - two small white eyes that blink, and close when a fighter is down
- **What adapted to it:**
    - Poses are angles, so they carry over unchanged; explicit hip offsets in the hand-keyed poses are scaled.
    - Motion-capture hip height is scaled to the shorter legs.
    - In the game: melee reach, chest height, ledge hang height, swing and grab spacing, weapon size (80%), and camera framing.
- **Smoothness:**
    - The clip player blends up to 6 clips at once with eased crossfades, so rapid combos and interruptions never pop.
    - Turning around eases through the motion instead of snapping.
    - Motion capture gets a light 1-2-1 temporal filter to remove capture jitter.
- Scene version 8.

## 2026-11-04: Black-and-white premium look, free movement, ropes, bars, wall corkscrew, crates (0.4)

- **Look:** every environment colour is now monochrome:
    - neutral grey concrete and a silver skyline (white windows and moon)
    - white lamplight, glow, dust, haze and exit sign
    - brighter white edge trims and pipe rim-lights
    - moonlight shafts through the tall window

    Fighters and their ink keep their colour, so they pop.
- **Controls** (GestureInput rewritten):
    - the left 40% of the screen is a floating run stick; flick up to jump
    - right side: tap to strike toward the tapped side of the hero, swipes for jump, dodge and duck/throw, two fingers for Rampage
    - keyboard: A/D run, J strike, Space jump, L dodge, F/S throw or duck, E rampage

    The HUD draws the stick.
- **Hero:**
    - running with speed set by the stick
    - a plain jump (new clip) with air control and air kicks
    - **ropes:** a pendulum you pump; your feet sweep enemies; let go into a flying kick
    - **bars:** giant circles (new pike clip); feet sweep enemies; timed dismount into a flying kick
    - **wall corkscrew:** run up the wall, then a backflip with a full twist
    - **crate kicks:** crates fly, bowl enemies over and smash
- **Props** (`EnvironmentBuilder.Props`):
    - 3 bars hung from the ceiling, end-on to the camera
    - 2 ropes (LineRenderer)
    - 3 crates in the fight lane; they reset each chapter
- Scene version 9.

## 2026-11-04: 0.4.1 cinematic opening

- `OpeningCinematic` (added at runtime by WaveDirector, so the scene doesn't need rebuilding):
    - a scripted three-shot opening timed in real seconds: establishing crane, skylight crash and slow-mo landing, push-in with the chapter card
    - opening letterbox bars: from full black to 92 px, then away at the hand-off
    - tap to skip; restarts get a short version
- CameraDirector: `Scripted(pos, look, weight, fov)` blends cutscene shots over the gameplay camera, so the hand-off back to gameplay is a smooth move, not a cut.
- HeroController: cinematic lock (`CineHold`, `CineDrop`, `CineLock`, `CineRelease`). Input is ignored and flushed on release.
- HUD: bars take the larger of the slow-mo bars and the opening's bars. Title, tagline, lower-third chapter card and skip hint. The gameplay HUD is hidden until the opening ends.
- The first wave's banner is now "FIGHT!".

## 2026-11-04: 0.4.2 classic controls back as default

- `GestureInput.scheme`: **Classic** (default) or **TwoThumb**. C toggles; the choice is saved in PlayerPrefs (`controls`).
- Classic is the 0.1–0.3 scheme:
    - tap a screen half = strike that way
    - swipe up = jump kick, sideways = dodge, down = throw
    - two fingers = Rampage
    - keys A/D strike, W jump, Q/E dodge, Space Rampage
- New in Classic: a still press held for 0.22 s (or holding A/D) runs that way, so ropes, bars and walls are still reachable.
- HUD: the movement stick is drawn only in two-thumb mode; the hint line shows the active scheme.

## 2026-11-04: 0.4.3 real sound

- Replaced the synthesised blips with a bank of 143 recorded takes in 48 categories (6.3 MB of Ogg Vorbis). Sources:
    - Oculus Audio Pack 01 (CC BY 4.0, via facebookincubator/meta-open-sound)
    - SFX Kit (CC0, wyzowl/sfx-kit)
- `tools/audio/build_audio.py` builds the bank:
    - trims leading silence (hits play on the frame they land)
    - folds to mono and peak-normalises
    - layers sounds: punch + body thump, finisher + bone crack + sub boom, sword + flesh, pipe + metal
- `Sound` mixer:
    - creates itself and survives restarts
    - 24 voices; takes never repeat back to back
    - pitch and level variation, per-sound gaps, stereo pan by screen position
    - slow-mo treatment: pitch bend (on sounds already playing too), a low-pass on everything including the ambience, a reverse whoosh in and a breath out
    - ambience: room tone and city rain; a drone during the boss
- Sound by event:
    - hits by weapon and limb
    - voices for hurt, death and effort; the brute's roar and laugh
    - doors and the ceiling hatch on spawns
    - footsteps synced to run speed, and landings
    - gunshot: crack + body + tail, and a delayed shell casing
    - crates, glass, wall splats, pickups, knife catch
    - rank up, wave clear, floor clear and defeat stingers
    - cues for the opening cinematic
- `AudioImport` postprocessor: one-shots are Vorbis, decompressed on load, mono; loops stream.


## 2026-11-04: renamed to Stick Wick

- The title card, the app name on the phone, the Unity menu (**Stick Wick → …**), the APK file (`StickWick.apk`) and the Android package id (`com.stickwick.game`) all use the new name.
- The code namespace stays `ProjectInk` (internal only).
