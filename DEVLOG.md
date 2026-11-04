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

## 2026-11-04: 0.5 the sword, rebuilt

- **Look:**
    - `SwordFx` replaces the box blade with a mesh katana: a curved, tapered diamond-section blade with a white cutting edge, a round guard, a gold collar and a banded grip.
    - The thin trail is replaced by a solid swept ribbon from mid-blade to tip, Catmull-Rom smoothed, bright at the tip and fading inwards. It lingers in slow motion.
- **Hit effects:**
    - `StreakFx` draws the white slice line across the body along the blade's path, with a glow echo, plus steel sparks. Clashes with a winding-up swordsman throw a big spark spray with a "CLASH!" sound.
    - Ink sprays the way the blade travelled.
- **Speed:**
    - Every sword clip is re-keyed shorter: the hit lands 3–5 frames in (was 4–6), and the follow-through is held.
    - Sword clips play at 1.2× with tighter crossfades.
    - The dash-in is 35% faster and leaves white afterimages (`Afterimage`, baked from the skinned mesh).
- **New moves:**
    - **Rising cut** (`sword_rise`), 4th hit of the combo: launches enemies into the air.
    - **Iai draw-cut** (`sword_iai`): the hero blurs through everyone in line in a trail of ghosts. Time nearly stops (×0.1) and the camera closes in. The hero holds the follow-through; 0.38 s later the cuts land on every victim at once. Then the blade clicks home.
    - The iai triggers on a counter, on 2+ enemies in a line, or as the kill finisher.
- **Slow motion, kept rare:** the iai, cutting two or more at once, and heavy killing blows. Never more often than every 2.5 s.
- `SceneVersion` → 10 (the move set must pick up the two new clips).

## 2026-11-04: 0.5.1 punch and kick combos

- Taps split by height (`GestureInput.kickLine` = 45% of the screen): **high = punch**, **low = kick**. The side still picks the direction.
    - Keys: A/D punch, Z/X kick (classic); J punch, K kick (two-thumb).
    - HUD: faint PUNCH / KICK guides at the screen edges.
- **Punch string:** jab → cross → jab → **hook** → **uppercut** (launches). After a full string, the next tap grabs and throws.
- **Kick string:** front kick → roundhouse → knee → spinning kick (knockdown). Kicks work with any weapon in hand, gun included.
- Each string continues while taps keep coming and resets after a 0.9 s pause; switching limbs starts the other string.
- A killing blow always becomes the string's finisher.
- When a finisher lands: "PUNCH COMBO!" / "KICK COMBO!". If it kills, there's a short slow-mo (shares the 2.5 s slow-mo budget).
- Jab, cross and hook play at 1.15× with tighter blends, for fast chains.
- New hand-keyed clips `hook` and `uppercut`. `SceneVersion` → 11.


## 2026-11-04: 0.5.2 premium HUD, no more text marks

- **Hero:** the white "v" over the head is gone. `HeroSpot` puts a soft pool of light on the floor under him, like a following spotlight: it breathes gently, brightens when enemies crowd in, shrinks while he's airborne and fades when he dies.
- **Incoming enemies:** the text arrows (<, >, v, o) are gone. A soft red glow breathes on the edge they'll come through (left, right, upper left, top, bottom).
- **Style rank:**
    - hidden at D
    - from C up: a white letter with a soft glow, its word in spaced capitals (CLEAN, BRUTAL, ARTFUL, SAVAGE, SUPREME, STICK WICK) and a hairline progress bar
    - a new rank scales in
- **Type:** all HUD text is white with a soft drop shadow; the dark text was unreadable on the noir building.
    - hairline health bar (red pulse when low) and Flow line
    - letter-spaced weapon label, banners and style callouts
    - combo count shown as a big number with "H I T S"
- **Results:** a black panel with hairline rules, the best grade and its word, and a pulsing "tap to fight again".
- **PUNCH / KICK guides:** only during the first wave of the first floor; they fade out after 10 s.

## 2026-11-04: 0.5.3 everyone in frame; upper-floor enemies with a plan

- **Camera** (`CameraDirector.Frame`):
    - Builds the smallest shot holding the hero, always, plus every living enemy on both floors, nearest first, with margins for heads, feet and the HUD.
    - Slides to keep the side walls at the frame edge, then pulls back if that would cut anyone off.
    - `maxDistance` raised to 22, enough for the whole hall on a 19.5:9 phone.
    - Checked numerically on five layouts (both corners, both floors, a full-room spread, a close fight): everyone fits.
    - Anyone still out of shot gets a small red glow on that screen edge at their height.
- **Enemies track the hero's floor, not his height:** `HeroController.Floor` changes only when he lands. Jumps, ropes and bars no longer send everyone running for the stairs and flipping back down.
- **Upper floor, hero below** (replaces the "run off the edge and flip" loop):
    - **Shooters and knife throwers** take a spot by the rail and fire down at him.
    - **Fighters** queue along the rail, taunting, and take turns (attack tokens) to **pounce**: a red-flashing crouch at the edge (counterable), then a diving kick that lands beside him. It hits only if he's still there.
    - The rest, and anyone when the hero is under the upper floor or far away, **walk down the stairs**.

## 2026-11-04: 0.5.4 type

- Three open-licence faces in `Assets/Resources/Fonts/` (licences in `FONT_LICENSES.txt`):
    - **Bebas Neue** (OFL): tall condensed capitals, the film-poster face. Used for the title, banners, chapter card, score, rank letter, hit counter and results grade.
    - **Barlow Condensed SemiBold** (OFL): small HUD text, tagline, labels, hints.
    - **Permanent Marker** (Apache 2.0): ink-brush shouts for the style callouts (DOUBLE CUT!, KICK COMBO!, IAI!), matching the ink theme.
- Sizes retuned for the new faces; the chapter card is re-laid out so its two lines no longer overlap.
- Mock-ups: `docs/previews/hud_fonts.png`, `docs/previews/title_fonts.png`.

## 2026-11-04: 0.5.5 classic controls locked in

- Removed the C-key scheme toggle. It sat next to the new Z/X kick keys, so a stray press switched to the two-thumb stick and saved that choice.
- The game now always starts in classic controls and deletes any saved `controls` preference.

## 2026-11-04: 0.6 the hall, redesigned for movement

Feedback (video): enemies couldn't reach the upper floor (they jammed at the stair top and the ledge), and moving up and down was a struggle. The old layout had the upper floor reachable only by a stair lane behind the fight (enemies only) or a ledge climb.

New layout: **one continuous walking surface**. Every x has exactly one floor height, so going up or down is just running left or right, for the player and the AI alike.
- `ground (x -13..-7) | stairs up (-7..-3) | raised deck 2.6 m (-3..3) on a solid base | stairs down (3..7) | ground (7..13)`.
- Stairs are smooth ramp colliders under visible steps, with white nosings and handrails along the back.
- Ramp endpoints verified numerically: they meet the deck edge exactly and run under the ground.
- Doors:
    - side doors at both ends
    - the back door moved to the right ground (x 11)
    - a new **deck doorway** at the back of the deck; `UpperDoor` spawns step out of it like the back door
- Props:
    - bars over each ground area and over the deck
    - **ropes over each flight of stairs**, to swing between the ground and the deck
    - crates on both grounds and on the deck
- Removed: the upper floor slab, the stair lane, the ledge climb triggers (jump/strike near the old ledge), the drop-through dive (it could sink a fighter into the deck's base), the old low lamps, pipes and balusters.
- Enemy brain: always "same level", just walk to the hero. Gunners fire up and down the stairs.
- The hero starts at x 9. `SceneVersion` → 12, so the scene rebuilds on pull.
- Layout diagram: `docs/previews/hall_layout.png`.

## 2026-11-04: 0.6.1 nobody on the roof

- The floor probe started above high-flying fighters and found the roof slab's top, so a big launch (a rope or bar dismount, the wall corkscrew) or a ceiling spawn could leave someone standing on the roof with no way down.
- `Fighter.GroundY` now never starts above y 6.5 (under the roof), and airborne fighters bump their heads at feet height 5.2.
- The roof has no collider. `SceneVersion` → 13.
- Anyone already up there falls back into the hall.

## 2026-11-04: 0.7 the tower

The hall still felt basic. The new main building is **the tower**, a three-level cut-away high-rise lobby built around movement. The single-path hall is kept as a backup: branch `backup/classic-hall`, plus the menu **Stick Wick → Build Classic Hall (backup)**, which builds `Assets/Scenes/ClassicHall.unity`.

**Layout** (`TowerPlan`, shared by the builder and the AI, so they always agree):
- Level 01: the lobby (ground).
- Level 02: two mezzanines (x ≤ −4 and x ≥ 3, y 3.0), with an open atrium between them.
- Level 03: a steel catwalk (x −7.5..7.5, y 6.0) across the atrium, with a **glass panel** in the middle. Roof at 9.4.
- Ledges, marked with hazard stripes: the inner edge of each mezzanine and each end of the catwalk.

**Movement:**
- **Climb:** swipe up near a striped ledge. The hero runs to it, jumps, hangs by the hands and pulls up.
- **Drop:** swipe down on a mezzanine or the catwalk to drop through it.
- **Fall:** walk or get knocked off an edge. Falls of more than 2.3 m finish enemies (FLOOR DROP).
- **Ropes:** hang from the catwalk truss into the atrium, to swing from one mezzanine to the other.
- **Bars:** under and over each mezzanine, and high over the catwalk.
- **Glass:** anyone landing hard on it (thrown, knocked down from above, the Brute, a long fall) smashes through, 6 m to the lobby ("THROUGH THE GLASS!"). It is repaired each floor.
- **Flying kicks** leap up a level at enemies above, and dive through the floor at enemies below.
- The opening cinematic drops the hero from the roof **through the glass catwalk** into the lobby.

**Physics:**
- One-way platforms: an airborne fighter only lands on surfaces below where their feet were, so you jump up through floors and land on them from above.
- Ledges are per fighter (any ledge, either direction).
- The roof height comes from `LevelLayout`.

**Enemy AI** (`LevelNav`): breadth-first route-finding over platforms (walk, climb a ledge, drop through), tested on 10 cases including the broken-glass detour.
- Fighters with an attack token **pounce** off a level onto the hero below.
- Shooters and throwers fire across levels.

**Look:**
- concrete back wall with lobby clerestories and a **tall atrium window** with mullions onto the city skyline
- **lit offices behind venetian blinds** on the mezzanines, with back doors and corridors
- a **skylight band** and a **giant ventilation fan** turning in the top wall (it slows in slow motion)
- **searchlights sweeping** through the skylight, moonlight shafts
- I-beam columns, a steel truss under the catwalk with hangers, slabs with bright lips and dark beams, handrails
- big stencilled level numbers 01/02/03, pendant lamps, haze on every level, drifting dust

**HUD:** incoming-enemy glows now sit around the actual doorway when it's in shot, or on the nearest screen edge.

`SceneVersion` → 14. Diagram: `docs/previews/tower_layout.png`.

## 2026-11-04: 0.7.1 the hero never leaves the shot

- `CameraDirector.KeepHeroInShot`: after every camera move (follow, boss close-up, kill-cam orbit, shake excluded), the hero's feet and head are measured on screen.
    - If either is outside a safe frame (14% from the sides, clear of the HUD at the top), the camera slides in its own right/up axes to bring him back.
    - If he is too tall for the frame, or behind the camera, it backs away instead.
    - Simulated on six cases (far side, high on the catwalk, mid-orbit, behind the camera, very close, below the frame): all end in frame.
- The boss close-up and the last-kill orbit now frame the subject **and** the hero together (centred between them, pulled back by their separation) whenever he is within ~8 m.
- The opening cinematic is exempt: it frames the hero itself.

## 2026-11-04: 0.7.2 fighters always readable

Feedback: the hero and enemies were hard to see in the tower. The cause was contrast and scale, not framing:
- the black hero stood in front of the tower's dark atrium window and skylight
- wide shots of the tall building made everyone small
- a heavy vignette darkened the screen edges

Fixes:
- **Outline on every fighter** (`InkOutline` + `Resources/Shaders/InkOutline.shader`): an inverted-hull rim, inflated along averaged normals, sharing the body's bones.
    - Near-white for the hero (3.4 cm), light grey for enemies (2.6 cm).
    - Its own colour property, so hit tints don't touch it.
    - Built at runtime in Awake, so the opening cinematic hides it with the body. The model is now imported read/write for this.
    - Before/after render of the real model: `docs/previews/outline_before_after.png`.
- **The atrium window is backlit:** pale moonlit fog with the city in light grey and a soft moon, instead of black glass.
- The **skylight band** was raised above head height on the catwalk.
- **Door corridors** are lit (0.62).
- **Camera** `maxDistance` 22 → 17: fighters stay big; anyone outside the shot gets the red edge marker.
- **Vignette** softened (starts further out, 105 alpha at the corners instead of 170).
- `SceneVersion` → 15 (re-imports the model, rebuilds the tower).

## 2026-11-04: 0.7.3 everyone in shot, always

Feedback: the hero and enemies still left the screen; the camera should zoom out more and show everyone. (The 0.7.2 cap of 17 m traded visibility for size, the wrong way round.)

- `Frame` now includes the hero and **every living enemy, with no exclusions**. It sizes the shot to the safe part of the screen (sides ±0.84; top 0.70 below the HUD; bottom −0.86) and aims slightly above the group so the HUD never covers heads.
- `maxDistance` is 30 (the whole tower fits on any aspect ratio). `minDistance` rises to 10.5 for a slightly wider standard shot.
- New last-word pass `KeepEveryoneInShot`: after every camera move (follow, close-ups, orbits), if any fighter's feet or head would fall outside the safe frame, the camera pulls straight back along its view until they're all inside. It pulls back instantly and eases in gently.
- Zooms out fast (0.16 s) and in slowly (0.5 s); the follow is a little quicker (0.14 s).
- Simulated six tower fights on 19.5:9, 16:9 and a 1.5 Game view: everyone visible in all 18. The hero is ~237 px tall on 1080p in close fights and ~80–100 px in full-tower spreads.

## 2026-11-04: 0.7.4 a calmer camera that stays in the building

Feedback: zoomed out, enemies were too small; the camera showed past the building; transitions were rough.

- **The building is the frame.** The shot never shows past the side walls. At most it shows a strip of floor below and roof above, and it can pull back at most to the full building, wall to wall. That caps fighter size at ≈135–155 px tall on a 1080p phone (≈240 px in close fights), against ≈80 px before.
- **Who's framed:** the hero always (he's clamped inside the safe area), then enemies nearest first, with other levels counting 1.8× further, while they fit within that limit. Simulated six tower fights on a 19.5:9 phone: everyone in shot in four; one or two left out only in extreme spreads (opposite lobby corners while the hero is on the catwalk).
- **Off-screen enemies** get an arrow in their own colour with a white rim and a soft glow. It slides along the screen edge pointing at them (it replaces the red edge glow).
- **Transitions:**
    - zoom hysteresis (the target only changes when the spread grows more than 0.4 m or shrinks more than 1.2 m), so no breathing
    - smoother follow (0.22 s); zoom out 0.45 s, in 0.9 s, with speed caps
    - the instant pull-back pass is removed; the hero slide is only an emergency net at the screen edge
- **Close-ups** (the boss entrance, the iai) now ease in over 0.35 s as well as out.
- **Kill-cam orbits** are clamped to ±38°: big swings showed the cut-away building's open side.

## 2026-11-04: 0.7.5 outline removed

- Removed the fighter outline at the user's request: `InkOutline`, its shader, the read/write model import and the preview image.
- The other readability fixes stay: the backlit atrium window, the skylight above head height, lit door corridors and the softer vignette.
- `SceneVersion` → 16, so the scene rebuilds without it.

## 2026-11-04: 0.7.6 stairs on the right side of the tower

Feedback: instead of climbing on both sides, use steps on one side.
- **Left side:** climbing, as before (lobby → left mezzanine edge, left mezzanine → catwalk end).
- **Right side:** two flights of open steel stairs.
    - A grand flight from the lobby (x 1.5) up to the right mezzanine's inner edge (x 4.5, y 3). It rises in front of the backlit atrium window; the mezzanine's inner edge moved from 3.0 to 4.5 to meet it.
    - A flight from the right mezzanine (x 11.5) up to the catwalk's right end (x 7.5, y 6).
    - Each flight is a thin one-way slope collider with open treads, white nosings, two stringers, a handrail and posts.
- **Walking under stairs:** while walking, fighters now step up at most 0.35 m (in the tower), so they pass under the stairs and slabs instead of being lifted onto them. Walking right in the left lobby takes you up the grand stairs; from under them you can walk out to the left.
- **Route-finding:** new `Stairs` steps. Enemies walk to the foot (or head) of a flight and keep going until they're off it. The lobby is split in two for routing, with a walk-under link. Ten routes checked.
- The right mezzanine door moved to x 5.4–7.0, the right office window to x 8.4–11.8 (behind the upper flight), and the crate to x 12.4.
- `SceneVersion` → 17. Diagram updated: `docs/previews/tower_layout.png`.

## 2026-11-04: 0.7.7 trapezes

- New prop **trapeze**: a steel rod (bright top edge, grip tape) hung from a roof fixing by two thin threads. It swings rigidly like a swing seat; the threads stay parallel and the rod tilts with the swing.
- It uses the rope's pendulum: jump to grab the rod, hold a direction to pump, feet sweep enemies ("TRAPEZE KICK!"), any tap or swipe lets go into a flying kick. Grabbing makes a light metal clink and shows "TRAPEZE!".
- Swings now have their own angle limits (`minAngle`/`maxAngle`), set at build time so the body never passes the walls. Hitting a limit bounces gently.
- In the tower:
    - a long trapeze from the roof over the **left mezzanine** (x −10.4, rod 2.1 m above the floor; replaces the bar there; swings −28°…+80°)
    - a short one over the **catwalk** (x 4.6, rod 1.35 m above the grating)
- `SceneVersion` → 18.

## 2026-11-04: 0.7.8 crates where they matter

Feedback: keep the boxes in useful places. A kicked crate flies on until it hits someone or lands, so a crate kicked off an edge drops onto whoever is below. The six tower crates are now:
- **lobby, by the left door** (x −10.6) and **by the right door** (x 10.8): bowl them into enemies walking in
- **left mezzanine, by its door** (x −8.0) and **right mezzanine, by its door** (x 8.4): into enemies stepping out
- **left mezzanine, at the climbing edge** (x −4.9): kick it off onto climbers or the crowd below
- **catwalk, near its left end** (x −6.3): kick it off to drop 3 m onto the left mezzanine

The two corner crates that did nothing are gone. All positions were checked against the bars' giant circles, the trapeze's swing and the ledge climb landings.

Walking fighters can **step up onto crates** again in the tower (a crate-only step-up on top of the 0.35 m stair step), so they stand on them instead of walking through them. `SceneVersion` → 19.

## 2026-11-04: 0.7.9 victory cinematic

Clearing a floor (the fifth wave, after the Brute) now plays `VictoryCinematic` (real-time, ~6 s):
1. The final blow's slow-motion kill-cam plays out while letterbox bars slide in; the gameplay HUD steps aside.
2. **Hero shot:** once he's on his feet, the hero is locked and gives his beckoning taunt. A low, close camera looks up at him and arcs ~30° round him, rising slightly (2.6 s), from the side with more room in the building.
3. **Title card:** FLOOR n CLEAR in Bebas Neue with a hairline, landing with a white flash, a small shake and a cinematic hit. Beneath it: style grade, score and kills. The victory sting is timed to the shot.
4. **Crane:** the camera rises and pulls back over the floor (1.5 s).
5. The bars slide away, the camera blends back to gameplay (1 s), control returns and the next chapter starts. The director waits for it, with a 12 s safety timeout.

## 2026-11-04: 0.8 head scissors takedown

A new signature move, chosen by situation (never on every hit):
- **Choreography** (scripted like the giant swing, 0.88 s of game time):
    1. the hero leaps onto the enemy's shoulders, legs locked round his head
    2. a beat leaning back
    3. the whip: he throws himself into a back-flip, rolling about the camera axis, while the enemy is dragged over in a forward somersault along an arc
    4. the slam, 2.1 m behind the start: 34 damage, knockdown, a bounce, bone crack, body fall, cinematic hit, ink splash, flash, hit-stop and shake. Anyone standing there is bowled over ("COLLATERAL!")
    5. the hero rolls out onto his feet
    - Over the top of the somersault: a short slow-mo and zoom, sharing the 2.5 s slow-mo budget.
    - "HEAD SCISSORS!" (+220 style).
- **When it happens** (all need: not the Brute, same level, close, solid floor 2.1 m behind, 4 s cooldown):
    - **counter** to a close wind-up: 60%
    - **another enemy right behind you**, who the victim is thrown onto: 45%
    - **end of the kick string**, in place of the spinning kick: 50%
    - **finishing blow** on an enemy with ≤ 34 HP: 30%
    - **from the air**, striking near an enemy at head height: 65%
- The hero is invulnerable during the move. If the victim vanishes mid-move, the hero drops him and recovers.
- Motion check: `docs/previews/head_scissors.png` (sampled from the real timings); both bodies stay above the floor.
