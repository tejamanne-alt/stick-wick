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

## 2026-11-04: 0.9 the building's moves

Situational cinematic moves built on the level and its objects. Nothing new to learn: they're chosen when you strike, drop or run in the right place.
- **DEATH FROM ABOVE:** drop through a floor, run off an edge, strike at an enemy ≥ 1.8 m below, or strike in mid-air above one.
    - The hero hops into a spinning front-flip and falls on him, homing in. Slow-mo is ×0.25 for 0.8 s, with the camera framing both. The dive passes through any floors in between and lands on the target's.
    - The last part of the fall is a stamp, or a downward plunge with a sword or pipe.
    - Impact: 45 damage (55 with a blade) to him, 22 to anyone within 1.9 m, all knocked down. A dust shockwave rolls both ways, with a boom, cinematic hit, flash, hit-stop, shake and zoom punch, and the lamps swing. "DEATH FROM ABOVE!" (×n for several).
    - Diving onto the catwalk glass smashes straight through and carries on down.
- **HERO LANDING:** a fall of ≥ 3.2 m with nobody to land on: a crouched landing with dust, a thud and a shake; anyone right beside him is knocked off his feet.
- **OVER THE EDGE:** strike an enemy (not the Brute) within 1.8 m of a mezzanine or catwalk edge with a real drop (≥ 2.2 m) beyond. The hero grabs him and heaves him up and out into the drop in slow motion, with a scream, the camera watching him fall. FLOOR DROP finishes him.
- **HAND STOMP:** strike toward an enemy hanging from a ledge below you: a stamp on his hands, a bone crack, and he falls with a scream. Enemies now hang 0.45–0.9 s on climbs, so it's catchable.
- **SPRINGBOARD:** a crate 0.3–1.6 m ahead and an enemy 0.6–4.5 m beyond it: the hero runs up the crate, flips off it and goes straight into a flying kick.
- Strike decision order: hand stomp, over the edge, springboard, death from above, head scissors, then the normal strings.

## 2026-11-04: 1.0 the guide

Three layers, learning by doing.

**1. The Training Reel** (`TutorialDirector`). On the first launch, after the opening, eight lessons in the lobby against pale-grey **stunt doubles** (`Enemy.Passive`: they walk up and take hits but never attack).
- A **ghost finger** performs each gesture where it's made: taps ripple, holds fill a ring with a direction arrow, swipes leave a trail and an arrowhead, two fingers pair up.
- A lesson completes when the player does it for real (via `HeroController.Did` events): "NICE." with a chime, then the next.
- The lessons:
    1. TAP HIGH, punch ×3
    2. TAP LOW, kick ×2
    3. KEEP TAPPING, a full combo
    4. HOLD, run to the striped ledge (a pulsing "HERE" ring on the floor)
    5. SWIPE UP, climb
    6. SWIPE DOWN, drop through
    7. SWIPE SIDEWAYS, dodge
    8. TWO FINGERS, rampage (the Flow bar is filled for it, three doubles to wreck)
- Finale: "THE BUILDING IS YOUR WEAPON / look for the chalk marks". Then the stunt doubles vanish and FIGHT! starts wave 1 (the director holds the first wave until then).
- SKIP TRAINING at any time. Completion is saved (`tutorial_done`).

**2. Chalk marks** (`HintDirector`). Hand-chalked words (Permanent Marker) with drawn arrows appear in the world, by the thing, when it's useful:
- CLIMB by a reachable striped ledge
- GRAB under a rope, trapeze or bar
- TAP - KICK IT by a crate with an enemy beyond
- DIVE ON HIM when standing above an enemy
- DODGE over an enemy flashing red beside you
- TAKE IT by a weapon on the floor

At most two show at once, most urgent first. Each retires for good after the move has been done three times (saved).

**3. The ? button** (top centre, or **H**) **pauses** the game (`TimeController.Paused`) and opens the **MOVES** card: every gesture and what it does, plus a note on the moves that happen by themselves. Its buttons:
- **REPLAY TRAINING**: resets the tutorial and chalk marks, then restarts.
- **BACK TO THE FIGHT** (or Esc).

On-screen buttons own their touches (`GestureInput.Blocked`), so pressing them never throws a punch, and a paused game ignores fight input. The old one-line hint is hidden during training.

Mock-ups: `docs/previews/guide_mockups.png`.

## 2026-11-04: 1.0.1 pause

- A **pause button** (II) beside the ? at the top centre; on the keyboard, **P** or **Esc**.
- The **PAUSED** screen: floor, wave and score, then RESUME, MOVES (opens the help card; closing it returns here), SOUND ON/OFF (saved) and RESTART.
- The world stops (`TimeController.Paused`) and so does all sound (`AudioListener.pause`).
- On phones the game pauses by itself when the app loses focus (a call, a notification, the home button).
- Touches on the pause screen never reach the fight.

## 2026-11-04: 1.0.2 weapons in the guide

- The Training Reel has three new lessons after the combo (11 in all):
    - **TAKE HIS WEAPON**: a sword lands on the floor, marked HERE; walk over it.
    - **TAP TO SWING**: two swings.
    - **SWIPE DOWN, THROW IT**: at the stunt double.
- New chalk mark **THROW IT** (weapon in hand, an enemy 3–10 m away on your level). It retires after three throws. "TAKE IT" by dropped weapons was already there.
- **Fix:** on a mezzanine or the catwalk, swipe down used to always drop you through the floor, so you couldn't throw from up there. Now, with a weapon in hand and an enemy on your level, swipe down throws; otherwise it drops.
- The MOVES card wording is updated for both.

## 2026-11-04: 1.0.3 bigger text and buttons

- The whole interface is drawn larger than the 720-line design: **×1.35 on phones**, ×1.2 on desktop (`HudGUI.UiBoost`). Layouts now use the real height instead of a fixed 720, and letterbox bars keep their share of the screen.
- Buttons:
    - pause and ? are 62×48 (II icon 7×24, ? at 32 pt)
    - SKIP TRAINING is 230×48 at 21 pt
    - the pause screen is a 2×2 grid of 300×58 buttons at 24–28 pt
    - the MOVES card buttons are 270×52 at 21 pt
- Small text raised: weapon, floor/wave, hits, rank word, lesson header, chalk marks (27 pt), training lines (22 pt), punch/kick guides.
- The MOVES card is re-fitted to the shorter layout space (rows size to fit).
- Checked at 2340×1080 (19.5:9): HUD, pause screen and MOVES card all fit with no overlap (`docs/previews/ui_sizes_phone.png`).

## 2026-11-04: 1.1 Rampage, made special

**Knowing it's ready:**
- A big round **RAMPAGE button** in the upper left, under the Flow bar it belongs to: dark disc, white rim, blood-red title, two rings rippling out, a red glow. One tap triggers it; two fingers and Space still work. It sits clear of the hero (the camera keeps him mid-screen) and of the usual thumb area.
- The **Flow bar burns** red-white with "R A M P A G E   R E A D Y" beneath it.
- The moment it fills: a "RAMPAGE READY" call-out, a cinematic hit, and three low heartbeats.
- **On the hero:** white sparks rise off him, and his light pool brightens and pulses.

**Activation, a 1.1 s cinematic** (the hero is untouchable and holds still):
- time drops to ×0.04
- the screen flashes; ink explodes, red splash included; 20 white speed lines burst outward
- the camera pushes in low and close (fov 32 → 26)
- black bars snap in, and **RAMPAGE** slams onto the screen in blood-red brush letters (scale 2.2 → 1 with a shudder), a white swipe drawn under it
- reverse whoosh, massive cinematic hit, boom, a shout
- the hero crouches, then rises in an uppercut
- at 72%, a **shockwave**: everyone within 3.8 m is interrupted and thrown back (those within 2.2 m off their feet), with a dust ring, a flash, a heavy shake, a boom, and every lamp swinging
- then the camera hands back and the rampage runs (×0.4 time, 5 s)

**While it runs:** a red frame breathes at the screen's edge, and a big draining RAMPAGE bar sits at the top centre.

The training lesson is now "RAMPAGE: tap the RAMPAGE button", with the ghost finger on the button. The MOVES card is updated. Storyboard: `docs/previews/rampage_storyboard.png`.

## 2026-11-04: 1.1.1 Rampage as a rare power for when it matters

Feedback: the RAMPAGE prompt appeared too often and distracted. Redesigned:
- **Rare:** the bar fills about 1–2 times per floor (≈180 points over a first floor's ~30 enemies, against a 100-point bar):
    - hit +0.5, kill +2.5, special kill +4, environment kill +3.5, perfect dodge +4
    - **damage taken ×0.45** (it used to drain 25): pain feeds the rage, a comeback mechanic
- **Quiet when charged:** the bar turns solid red with a small "R A G E   C H A R G E D". No button, no sparks, no heartbeat, no call-out.
- **Appears in trouble** (`HeroController.InTrouble`): health ≤ 35%, three or more enemies within 3.2 m, or the Brute within 7 m. Then the round RAMPAGE button appears with one hit and one heartbeat, the sparks and the pulsing light pool return, and the bar pulses. The button lingers 3 s after the danger so it never flickers. Space and two fingers still work any time it's charged.
- **Bonuses for using it at the right moment:**
    - **SECOND WIND:** used at ≤ 35% health, it restores a third of max health.
    - **Surrounded:** a 5.2 m shockwave (was 3.8) that knocks everyone down for 12 damage.
- Training lesson and MOVES card text updated (the training lesson still always shows the button).

## 2026-11-04: The boss fight, shot like the end of a film

The Brute felt like a big enemy dropped into the middle of a wave. Now the last wave of every floor is his, and it plays as a duel with an entrance and a finale (`BossDirector`).

- **Arrival:** the wave opens with his men. Once half are down (or the room is nearly empty, or 18 s pass), the lights stutter, the floor shakes and he walks in through the ground-floor door nearer the hero.
- **His men flee:** they flinch and run for the exits, then vanish. This is not a kill. Hitting one on the way out scores **COWARD!** The fight is one on one.
- **Entrance cinematic** (tap to skip):
    - letterbox bars
    - a low tracking shot as he walks in, each step shaking the room
    - a hard cut to his face as he roars, and the name card slams in: THE BRUTE / HE OWNS THIS BUILDING
    - an over-the-shoulder two-shot as the hero beckons and he beckons back
    - FIGHT!
- **Boss health bar,** top centre: a white "damage taken" segment that drains after a beat, a phase mark at half, and ENRAGED pulsing in phase two.
- **Special moves,** all telegraphed in red:
    - **Bull charge:** he paws the floor, then thunders across. Dodge through him (MATADOR!) and he hits the wall and is **stunned**: stars round his head, 1.5× damage, light-hit armour gone. If he charges off a gallery he lands hard and is stunned too. He skids to a stop at the foot of a stair.
    - **Ground slam:** he crouches, leaps, and a red ring shows where he will land. The shockwave runs along the floor, so be in the air (JUMPED IT!). Afterwards he is slow to rise, which is your opening. He breaks the catwalk glass if he lands on it.
    - **Bear hug:** he uses the grab; tap to break it.
    - Super-armour while charging, slamming and stunned.
- **Phase two** at half health: a roar that blows the hero back, blood red, faster, more charges, double slams, and he powers through half of all counters.
- **No cheap kills:**
    - Rampage strikes take 12% instead of killing.
    - The gun-fu execution does 35.
    - Falling off a floor takes 12%.
- **Finale:** the killing blow is caught; he staggers at 1 HP in slow motion, and the scene takes both fighters:
    1. a three-hit flurry, each hit a hard cut: over the shoulder, reverse angle, profile
    2. an uppercut launch, shot from the floor looking up
    3. the hero leaps after him, in slow motion, with the camera circling them in the air
    4. a freeze-frame spinning kick, or a leaping cut if he has a blade
    5. driven into the floor: a shockwave ring, his colour across the floor, the lamps swinging
    6. the hero lands crouched beside him and rises, under the card BRUTE DEFEATED / FLOOR n IS YOURS
    7. a hard cut into the floor-clear victory
  The sequence keeps both fighters on their platform and under the floor above. It uses the hero's weapon: fists, sword or pipe.
- Brute health 220 → 300 (×floor scale), size 1.35 → 1.45.
- Verified:
    - Compiles (input system and legacy).
    - Finale framing checked numerically: both fighters stay in every shot, apart from the intended close-up crops.
    - HUD cards mocked at screen size: `docs/previews/boss_cards.png`.
- Not playtested in Unity yet. Timings to tune on device:
    - charge speed
    - slam radius
    - the arrival threshold

## 2026-11-04: A cinematic title sequence

The STICK WICK title used to fade in over the crane shot. It is now its own beat on black, about 3.4 s long, drawn by `HudGUI.Logo` and cued by `OpeningCinematic`:
1. **0.2 s:** a blade's glint, a hairline drawn across the middle of the black.
2. **0.58 s:** a slash of light. A dry-brush red ink stroke is painted left to right. The stroke is a procedural texture: ragged edges and bristle streaks that run out of ink.
3. **0.72 s:** STICK slams in letter by letter, each letter dropping from 2.4× with a motion trail. A white flash, a red chromatic split that settles, and an impact shake.
4. **1.02 s:** WICK lands the same way, a beat later.
5. Ash drifts up behind the title. A light sweep passes across the letters (1.45 s). The title leans in slowly.
6. **1.7 s:** the tagline lands in three beats: ONE MAN. / ONE BUILDING. / NO WAY DOWN.
7. **3.0 s:** a sword cut flashes along the same middle line. The title is sliced in two and the halves slide apart and fade. The black splits open from that line into the letterbox, onto the crane shot over the building.
- Sound design on every beat: glint whoosh, sword swing, a massive hit on STICK, a hit on WICK, three soft hits for the tagline, and sword swing plus hit for the cut.
- The establishing shot is now 6.0 s, so the crane still plays after the title. Skip still works. Restarts play the short opening without the title.
- Storyboard: `docs/previews/title_sequence.png`.

## 2026-11-04: A cinematic floor-clear victory

The victory was one camera arc with a text fade. `VictoryCinematic` is now an edited sequence of about 8 s, played after the kill-cam or the boss finale:
1. **The walk.** The victory sting starts. The hero turns his back on the bodies and walks away at a slow, heavy stride (the run cycle slowed right down), easing to a stop. The camera stays low in front of him, dollying back at his pace and pulling away from walls if it has to. He walks towards the side of his floor with more room. In the classic hall, whose floor is not flat, he does not walk.
2. **Hard cut to the hero shot.** He turns and beckons as the low camera arcs round him.
3. **The card,** drawn by `HudGUI.VictoryCards` and timed from `CardT`:
    - FLOOR n, then CLEAR slamming in letter by letter over the red ink stroke, with the same trail, red split and shake as the title
    - **the tally** along the bottom: kills, score and time count up one after another, with a thud as each lands and ticks while it counts (this floor's kills and time)
    - the style grade **stamped** in brush lettering on a red ink splat, landing big and tilted with a heavy thud and a shake
    - **NEW HIGH SCORE** in a red tag, if it is one
4. **The crane** up and back over the floor while the card fades.
5. The bars slide away. Tap to skip once the card is up.
- The floor-clear sting moved from a fixed delay to the start of the walk.
- The boss finale now hands the camera over when the walk starts.
- The floor-clear timeout went from 12 s to 16 s.
- Storyboard of the card: `docs/previews/victory_card.png`.

## 2026-11-04: No controls line along the bottom

The one-line controls cheat-sheet at the bottom of the screen during the start of floor 1 has been removed. It repeated the training reel and the ? card.

## 2026-11-04: HINTS option (AUTO / ALWAYS)

The pause menu has a third row: **HINTS AUTO / HINTS ALWAYS** (saved).
- **AUTO** (the default) is the existing behaviour: each chalk mark retires once its move has been done three times.
- **ALWAYS** shows every chalk mark whenever it applies and never retires them.

Switching back to AUTO resumes the counts where they were. Set in `HintDirector.AlwaysShow`, which uses the PlayerPrefs key `hints_always`. The pause buttons shrink a little on short screens so all three rows fit.

## 2026-11-04: Chapter card rule

The rule under CHAPTER n on the opening's lower third was a 50 px dash, which read as cut off. It now spans the wider of CHAPTER n and the chapter name, and draws out from the left as the card fades in. It is led by a short red stroke in the same ink red as the title and victory cards.

## 2026-11-04: Training reel fixes and SKIP THIS MOVE

Reported: after three stunt doubles died, the training gave no way forward. Each lesson was reviewed against the events it waits for.

**Why it stalled**
- Doubles were spawned only when a lesson started. Once one died mid-lesson (sword cuts, a thrown sword, wall splats, falls, throws, rampage hits), a punch, kick, combo or swing lesson had nobody left to land a hit on.
- A double launched across the room or off a floor stayed there.
- The throw lesson needs a weapon in hand. If the sword had been used up or thrown during the swing lesson, a swipe down just ducked.
- The pickup lesson waited forever if the hero already held a weapon.
- The run, climb and drop lessons could never finish in the classic hall, which has no ledges.

**Fixes**
- **Stunt doubles cannot die.** Every damage path (hits, kills, executions, wall splats, falls) stops at 1 health. They go down and get up again. Their "kills" are not counted, so there is no kill-cam and no WAVE CLEAR.
- **Every lesson starts clean.** The doubles are put back on fresh marks on the hero's own floor (never past its ends), on their feet and healed, with a puff:
    - one double in front for punch, kick, combo, the pickup/swing pair and dodge
    - one double 5 m away for the throw
    - three doubles round him for rampage
    - none for run, climb and drop
- **Doubles are tended during a lesson.** One more than 8 m away, on another floor, or lying down too long is put back on his mark. A missing one is respawned.
- **Throw lesson:** if he has nothing to throw, he is handed a sword.
- **Pickup lesson:** skipped if he is already armed. The sword is placed on his own floor.
- **Climb lesson:** now also shows the marker at the ledge.
- **Classic hall:** the run, climb and drop lessons are skipped.
- **SKIP THIS MOVE** button under SKIP TRAINING skips just the current lesson. If a lesson runs past 20 s, its outline pulses.

## 2026-11-04: Training: taps on the right side, skip buttons at the top

- **Bug:** in the first lesson the stunt double stood on the hero's left, but the ghost finger showed a tap on the upper right. In the classic scheme the screen half picks the direction, so the hero struck the empty air.
    - The ghost finger now taps on the side the nearest double is on (classic: that half of the screen; stick scheme: just beside the hero, clear of the stick zone).
    - Doubles are placed to the hero's right when there is room.
    - The hero turns to face the double at the start of each lesson.
    - The lesson lines now say "on the side of the screen he is on".
- **SKIP THIS MOVE** and **SKIP TRAINING** moved from the right edge, where they covered the tap zone, to the top middle under the pause and ? buttons.

## 2026-11-04: No chalk marks during training

Chalk-mark hints are hidden while the training reel runs (and while it is waiting to start), even with HINTS set to ALWAYS. The reel teaches one move at a time, and hints for other moves on top of it were confusing. They come back when the fight starts.

## 2026-11-05: Fewer at once, and pick your fight

Reported: fights felt overwhelming with many enemies arriving together, and there was no way to go after a chosen enemy (the armed one first, say).

**Never more than five on screen**
- A wave is now a queue at the doors. Only as many get their door warning as there are free places (`ActiveCap`); the rest wait outside.
    - Floor 1 starts at 3 (wave 1), then 4, then 5.
    - Later floors: 5.
- If the room drops below 3, the next man comes straight in.
- Arrivals are at least 0.45 s apart. Runners and stunt doubles do not count.
- At most 2 attack at once (`AttackTokens.Max` 3 → 2).
- Simulated (`spawn_sim.py`): never above the cap, about 3–4 on screen on average, waves of 14–34 s.

**Target lock**
- **Tap (or click) an enemy** to make him the target (`HeroController.Locked`). Red corner brackets go round him, with his health bar above.
- Every strike or kick on his side goes to him (`PickTarget` prefers him within reach). Tapping the other side still fights there.
- **T** on the keyboard cycles targets, the most dangerous first (`Enemy.Threat`: gun > sword > pipe > knives > fists).
- The lock clears when he dies, runs or is far away.

**The duel**
- While you have a target, he may always come at you, but only one other may cut in (`AttackTokens`).
- The other melee enemies circle 1.7 m wider and wait their turn. Ranged enemies keep their distance as before.
- The fight plays as one-on-one with interruptions, like a film fight.

**Reading the threats**
- A small drawn weapon icon sits over every armed enemy: sword, pistol, knives or pipe. It turns red while he winds up.
- New chalk hint: **TAP HIM: TARGET** over the most dangerous enemy while nobody is targeted. It retires after 3 locks.
- The help card has a new row: TAP AN ENEMY.
- New training lesson after the combo lesson: **TAP HIM**. There are two stunt doubles; the ghost finger taps the one behind you.

## 2026-11-05: The hero's look: steel shades and a red scarf

The hero was a plain black stickman, the same ink as the building's shadows, and could blend into the fight. He now has a signature silhouette that reads at any size:
- **Mirrored steel shades** (`FighterFactory.BuildShades`) replace his cartoon eyes. He is the only one without eyes, which reads as cool rather than cute.
    - a flat, wide steel-silver lens on each side of the face (whichever way he faces, one is towards the camera)
    - a white glint on each lens
    - a thin temple arm back to the ear and a bridge across the front
- **An ink-red scarf:** the wrap and knot are built at the neck, and two tails are simulated as light cloth at runtime (`HeroScarf`, a verlet chain drawn as camera-facing ribbons just behind the body).
    - They hang and sway when he stands, stream out behind him when he runs, whip round on a dodge and lift in a fall, so his direction of travel always reads.
    - They float in slow motion (scaled time), reset on cuts and teleports, and never pass through the floor.
    - They are hidden while the opening holds him out of sight.
    - It is the same ink red as the title, the victory card and the chapter rule. He is the only fighter with a single bright accent, so he is found at a glance among the coloured enemies.
- **A glint runs across his shades** whenever he beckons (taunt): in the opening, in the victory hero shot, and before the boss.
- Scene version 19 → 20 (the scene rebuilds on open).
- Preview: `docs/previews/hero_look.png`, made by `tools/blender/preview_hero_look.py` with the same geometry as the game.

## 2026-11-05: Scene build fix after a clean checkout

Reported: after switching to `story-mode` with "discard local changes", the game would not run. The scene was "Untitled", holding only an empty Building and SpawnPoints, and the camera showed "No cameras rendering".
- **Cause:** discarding also removed the untracked generated files (`Assets/Generated/…`, `Assets/Scenes/Prototype.unity`). The tower writes its textures to `Assets/Generated/Textures` but never created that folder; it only existed because an older classic-hall build had made it. The automatic rebuild threw at the first texture and stopped.
- **Fix:**
    - `SaveTex` creates its folder itself.
    - The scene build creates `Assets/Generated`, its `Textures` folder and `Assets/Scenes` up front, and refreshes the asset database.
    - A failed texture import gives a clear error instead of a null reference.
- `HeroScarf.Reset(Vector3)` renamed to `ResetChain`, so it is not confused with Unity's `Reset()` message.
- Also: `FirstOpenSetup` treated a missing scene as up to date once it had run in an editor session, so after the failed attempt it never retried. A missing scene or version file now always triggers a rebuild.
- **Stick Wick → Build Prototype Scene** pressed during Play mode threw "This cannot be used during play mode". It now stops Play mode first and builds as soon as the editor is back in edit mode.

## 2026-11-05: Hero look reverted

The steel shades and red scarf were not liked and are removed. The hero is back to the plain black stickman with his eyes. Scene version 21, so the scene rebuilds without the accessories. The build fixes from the same day stay.

## 2026-11-06: Story mode, the prologue (the warehouse awakening)

The story begins. He wakes in a warehouse with no memory; across five chapters he recovers one memory each. The tower stays as **Fight Mode**.

- **Main menu** (`MainMenu`, scene `Menu`): STICK WICK over a stormy night. The choices are **STORY MODE** (the prologue) and **FIGHT MODE** (the tower, unchanged). The pause menu has **MAIN MENU**.
- **The set** (`StoryBuilder`, scene `Story_Warehouse`).
    - **Inside:** a 28 m corrugated-iron shed, cut away to the camera.
        - Roof and walls: a cracked roof with a seam of storm light over the spot where he lies; girders, two long beams, columns, hanging chains with hooks.
        - Props: a dead press and lathe, a bench, a scrap pile, barrels.
        - Floor: puddles, and long fake shadows.
        - The way out: a rusted roll-up shutter with moonlight spilling under it.
    - **Outside**, seen only in the opening wide shot: a labyrinth of stacked containers in five dark colours, piles of crushed cars, sodium lamps with pools of light, neon puddles. Far off, a city in two skyline layers under an amber and purple haze.
    - Props are visual only (no colliders).
- **The intro** (`StoryIntro`): seven shots, then the play hand-off, as written in `docs/story/prologue.md`.
    - The revised script fixes three things for a stickman:
        - the reflection shot is now his view down into the puddle on hands and knees
        - the scarred knuckles become fists and a knuckle crack
        - a location card, the thugs' line and the first memory flash ("YOU'VE DONE THIS BEFORE.") are added
    - The walk to the door is played. The breach has crossing shadows, two latch clanks, the shutter flying up and slow motion, then three brawlers and the fight.
    - It ends with the memory flash and a PROLOGUE COMPLETE card (PLAY AGAIN / MAIN MENU).
    - SKIP jumps to the walk. A defeat retries at the door.
    - Lightning means two white frames and a blue afterglow, the roof crack flaring, and phone vibration on mobile.
- **New lens effects** (`StoryCameraFX`), with no custom shaders: a downsample blur for waking eyes and slice glitches for the fractured memory. Also a Dutch-angle roll for scripted shots (`CameraDirector.ScriptRoll`).
- **New animations** in the Blender pipeline: `lie`, `allfours`, `rise`, `idle_loose`, `look_hands`, `touch_temple`, `walk`. `Stickman.fbx` and the clip data were re-exported; the 48 existing clips are unchanged.
- **New sounds**, synthesised by `tools/audio/make_story_sfx.py`: thunder, drip, splat, tinnitus, heartbeat, static, reversed whispers, a cello drone, latch, water footsteps, breath, wind. There are also storm ambience controls (`Sound.Ambience`): the storm is loud outside and muffled inside.
- **The hero in the story:** narrowed eye slits (`Fighter.eyeOpenY`) and a white scar at the temple.
- `WaveDirector.scripted`: no waves, floor or score, and no opening, tutorial, victory or boss; the story spawns its own (`SpawnScripted`).
- Scene version 22: **Build All Scenes** (also the old Build Prototype Scene menu item) builds the menu, the story and the tower, sets the build list, and opens the menu.
- Not playtested in Unity yet. Timings, camera positions and the puddle art will want tuning on device.

## 2026-11-06: The warehouse, modelled and lit like a film set

The prologue's set was Unity boxes. It is now modelled in detail in Blender (`tools/blender/build_story_set.py`), lit for night, and **baked**: the light is painted into textures. The unlit game shows a lit film set at no runtime cost.

**Inside the shed**
- The building:
    - a concrete-block plinth with mortar joints, corrugated iron above (ribs, lap seams, rust bleeding from the bolts)
    - four steel-framed clerestory windows with broken panes
    - side walls with rails
    - a corrugated roof torn open at the crack, with flaps hanging
- The structure: Warren roof trusses every 3.4 m with purlins, I-beam columns with base plates and bolts, a yellow crane rail with a trolley and a chain of real links down to a hook block.
- On the walls:
    - pipes with flanges, brackets and a red valve wheel
    - a fuse box with conduit and a DANGER HIGH VOLTAGE plate
    - painted BAY 7 and NO SMOKING stencils
    - a wall fan (it turns)
- Lamps: four enamel shades on cords; one still burns.
- Machinery: a green hydraulic press (control box with lit buttons, sagging hydraulic hose, warning stripes) and a blue lathe (chuck and jaws, carriage, tailstock, motor).
- The workbench: a vise, a toolbox, a pegboard of tools.
- On the floor:
    - a forklift parked crooked (cage, mast, forks)
    - scrap and pipes, oil drums with rims (one lying, one stacked), a pallet stack
    - crates with battens, a tyre stack, a leaning ladder
    - the slab: joints, a yellow walkway line, hazard stripes at the door, a drain, oil stains
- The shutter: real slats, a bottom bar, guide rails, the drum housing, a chain hoist, NO ENTRY.

**Outside** (the opening shot)
- Six rows of containers, stacked 1 to 4 high, with corrugated sides, corner posts and door lock bars. A road through the stacks leads the eye to the shed.
- Two gantry cranes with lit cabs and red aviation lights.
- Stacks of crushed cars, sodium light poles, a chain-link fence with barbed wire.
- The shed's exterior: gutters, downpipes, the painted bay letters under their lamp.
- A city skyline with lit windows, neon signs and aviation lights, under the night sky.

**The light** (baked with Cycles)
- Cold moonlight through the windows and the open front.
- A storm seam through the roof crack onto where he lies.
- Moonlight under the shutter.
- One warm sodium spot inside (a single warm pool in a cold blue room).
- The bay lamp and the pole lamps outside.
- A cold fill tuned so black fighters always read against the back wall.

**In Unity** (`StoryBuilder`)
- The set FBX goes in with one baked texture per part.
- Its axes are aligned from marker empties exported with it.
- Unity adds:
    - moonbeam shafts (additive)
    - the flaring crack seam and the light under the door
    - puddle sheen, droplets, the night sky
    - rain outside (seen through the windows, the door and in the wide shot), dust drifting in the moonlight
    - the turning fan
- Import rules: `StorySetImport`. The shutter disappears into its drum after the breach.
- The opening wide shot was re-framed down the road to the shed. Scene version 23.
- Previews:
    - `docs/previews/story_set_baked.png`: the exported set re-imported and rendered through its baked textures only, as Unity shows it
    - `docs/previews/story_set.png`: the lit preview

## 2026-11-06: Richer, slower, immersive opening shots

Feedback: the shots should be richer and premium, mixing emotion, thriller and action. These are the beginning scenes, so they should be slower, making the player feel he is there.

- **Slower.** The whole opening plays at 1.5× its written length (`StoryIntro.Pace`): about 46 s instead of 31. The written beat times stay as they were; the cinematic clock runs slower. Blinks are heavier, the drop hangs before it falls, and camera moves are slower. His rise, look at his hands, scar touch and walk are slowed to match.
- **Alive between the beats.**
    - Every few seconds: distant thunder, iron creaking, a chain chinking, drips in the dark (`Ambient`).
    - Every shot breathes with a slow handheld drift (`Sway`).
- **Real lightning.**
    - The set is baked a second time, lit by a strike: windows, roof crack and door blazing, the sky white-blue (`*_Lightning.png`).
    - `LightningSwap` flickers the set's materials to that bake (on, off, on, gone) with every strike.
    - In the opening a forked bolt (three jagged lines) splits the sky over the city.
- **Shot 0:** a slow crane down the road to the shed. A helicopter searches the yard: its beam combs the stacks, its pool of light slides over the containers, its red light blinks, and the rotor thumps far off. Someone is hunting him.
- **Each shot moves.**
    - Shot 1 turns slowly as it looks up into the crack.
    - Shot 2's eyes drift as he breathes.
    - Shot 4 creeps in on the Dutch angle.
    - Shot 5 cranes down out of the rafters and drifts round him as he turns.
- **Emotion.** A sparse piano motif in A minor plays when he sees his own face in the puddle, and again at the memory (`Sfx.Piano`, synthesised).
- **The walk.** Water splashes at his feet (cinematic and play).
- **The breach.**
    - A low camera over his shoulder.
    - Sparks fly from the latch at each clank; rust and dust shake off the drum while it rattles.
    - The breach itself is a full lightning strike with a beam of moonlight through the doorway. The three men stand **black against it** and only get their colours as they step into the room.
- **The memory flash** (about 5 s). White, then split-second inserts, each with a stab of static:
    1. **through a rifle scope** onto a man, at 450 m
    2. **a muzzle flash** with a gunshot
    3. **a target photo**: a red cross through the face, TARGET stamped on it

  Then the piano and **YOU'VE DONE THIS BEFORE.**
- Moonbeams halved, so they never wash a fighter out. Scene version 24.
- Storyboard on the real baked set: `docs/previews/story_storyboard.png`.

### Puddle reveal, redone (story-mode)
- Shot 3 is now a simulated puddle (`PuddleShot.cs`), not stacked 2D shapes. Detailed wet concrete with a darker wet halo, an irregular puddle with a meniscus, and a reflection at 2x resolution: the roof crack, the trusses, his head, eye slits and scar.
- **One drop, one ripple.** The drop falls into the centre, with a crown of beads and a rebound column. A single wave packet spreads in slow motion, refracting the reflection and catching highlights, then dies away.
- **The face surfaces as the water calms**, unreadable before the drop and fully clear by about 3.4. Then lightning at 3.8 blows the reflection white, with the thunder close behind. The push-up follows at 4.4.
- Sound re-timed: breath, groan, a slowed plop as the drop lands, piano as the face emerges, then lightning and thunder.

### Story sound pass, clean walk, a real drop (story-mode)
- **Walking no longer splashes.** The ink splash particles and the splash sound on every step through a puddle are gone, both in the walk to the shutter and in the shot-6 hand-off. Plain footsteps only.
- **Story SFX rebuilt from real recordings** (`tools/audio/make_story_sfx.py`, now taking the same two libraries as `build_audio.py`):
  - real male breathing, real whispers, a recorded drone under the cello, real metal for the latch, recorded thunder, drips and a splash
  - all of it in one consistent warehouse convolution reverb
  - **no broadband hiss anywhere:** noisy material is band-limited and darkened, and the electrical "static" is a buzz and glitch held below 6 kHz instead of white noise
  - the wind uses a smooth time-varying filter, so the old block clicks are gone
- **The drop in shot 1** is now a clear water lens instead of an opaque ball, turned to the camera: almost transparent, a thin bright rim, the dark inner ring, a glint, and the roof crack refracted upside down inside it. It wobbles as it gathers and rings as it lets go. Scene version 25.

### Floating eyes and scar fixed
- The eyes and scar were floating off the head in story close-ups. Cause: they, the weapon mounts and the limb trails are placed by measuring the bones when the fighter is built, assuming the model is in its rest pose. But the model's default pose is whatever frame it was exported on. Since the story clips were added, that frame is a crouched story pose: the head sits 0.27 m forward and 0.29 m low.
- `FighterFactory.ToBindPose` now puts the model into its true bind pose (from the skinned mesh's bind poses) for those measurements, then puts it back. Forward is measured the way `ClipPlayer.CalibrateFacing` does at runtime, instead of being assumed to be +X. The story scar uses the same rules. Scene version 26.

### One wound, not two scars
- The two mirrored white zigzag "scars" are replaced by a single fresh head wound on his **right temple**: the side his right hand touches in `touch_temple`, and the side the camera sees in the prologue. It's a short, slightly curved, tapered gash (a dark lip round a wet red cut) with a thin trickle of dried blood running down past the eye, ending in a bead.
- It's built as ribbon meshes laid on the head sphere, parented to the head bone and measured in the bind pose like the eyes.
- The puddle reflection shows the same single red gash and trickle instead of the white zigzag. Scene version 27.

### Fix: running on the spot at the shutter
- Walking up to the shutter starts the break-in, which locks the hero with `CineLock`. The lock left his run state and run animation on, so he kept running on the spot after the arrow was released, sliding to a stop.
- `CineLock` now stops a run: horizontal velocity is zeroed and he settles into his stance (`IdleClip`). This also covers every other cinematic that locks a standing hero.

### The escape (prologue, between the breach and the fight)
- Story beats:
  - The men walk in and he runs.
  - A chain across the floor trips him into the puddle, in slow motion.
  - He lies dazed while they come, laughing.
  - Lightning shows his knife glinting on the floor ahead, and he crawls to it.
  - He grips it, and 4.5 s of past fights flash in.
  - He rises with the knife, turns to face them, and they step back: *"...My hands remember."* Then the knife fight.
- `StoryEscape.cs` (a partial of `StoryIntro`):
  - the timeline, with hand-cut camera shots
  - the men walked up as puppets (`Enemy.PuppetRelease` added)
  - the chain jumping and dragging where his foot caught it
  - the knife glint
  - the montage: stills pushed in and shaken, split at the edges, torn, grained, with white and black frames and cuts back to him bled red, in a new random order each time
- New clips `trip`, `prone`, `crawl`, `reach_grab`, `prone_knife` and `rise_knife` in `build_stickman.py`. The export now resets the rig to rest before writing.
- Set: `Chain` (30 real ring links in an S across the lane at x 3.3) and `FloorKnife` (steel blade, glint sprite) in `StoryBuilder`.
- Flashback stills: `tools/blender/render_flashbacks.py` renders 8 noir frames with the real rig and clip poses into `Resources/Story/Flash`. Preview: `docs/previews/flashbacks.png`.
- New recorded SFX: chain, body splash, crawl, glint, knife grab (a real knife scrape) and flash cuts.
- A retry skips the escape, with the knife already in hand. Scene version 29.

### Flashbacks redone in the fight mode's look, black and white
- The stills used shaded, glossy mannequins and colour, so they read as other characters.
- They are now rendered flat and unlit, exactly as the game draws its fighters:
  - the hero ink-black with his two white eyes (narrowed, as in story mode, and no wound: this is before)
  - the men in flat greys taken from their fight-mode colours, darkened so they separate from the walls as their colours do in fight mode
  - the game's white slash trails on the strikes
- The world is the fight mode's grayscale: concrete panels with seams, dark steel, white trims, hanging lamps with soft cones, black windows with white city lights, a white moon.
- Every frame is finished in pure greyscale, and the montage's colour split is now a grey double image. The cuts back to the present stay in colour.

### INSTINCT: the first fight, directed (replaces the training-reel version)
- `StoryDuel.cs` (a partial of `StoryIntro`): the prologue's first fight is choreographed. The three men are puppets on their marks, and every attack, hit, fall and camera cut is scripted. The player only gives the input at each beat's moment of decision.
- **The prompt:**
  - Time crawls (`TimeController.CineScale`, a new story-held scale under everything else) to about 3%.
  - The edges darken and a heartbeat plays.
  - The move appears: its name, how to make it, the key on desktop, and combo pips.
  - A ghost finger makes the gesture at the right place: a high or low tap on the target's half of the screen (with the high/low line), a tap on the man, or a swipe.
  - A red ring pulses on the man it's for.
  - It accepts only the real `GestureInput` command. A wrong one shakes the prompt. You can't lose.
- **Beats:**
  1. PUNCH (tap high)
  2. KICK (tap low, under his guard)
  3. COMBO (three taps: jab, cross, uppercut)
  4. SLIDE (swipe toward him, under the haymaker)
  5. The knife comes out of his belt
  6. STAB (tap high on the left, the man behind him)
  7. THROW (swipe down, a flying knife that stays in him)
  8. TAP HIM (a tap on the man picks him; `Enemy.PuppetPickable` lets a puppet be picked)
- Hit weight: reaction clips, ink burst and splash in the man's colour, hit-stop, shake, slow-motion beats, cinematic kill shots and lightning.
- After the last falls, the existing memory ("YOU'VE DONE THIS BEFORE.") follows.

### Shot 5 rethought: THE LIGHT
- The overhead crane with the hero turning 360° on the spot didn't belong in the scene. Replaced:
  - The helicopter from shot 0 returns, rotor rising.
  - Its searchlight drops through the roof crack as a hard white curtain: two slanted additive sheets, rain flicker, the crack blazing.
  - The curtain's strip sweeps across the floor towards him. He crouches (`duck`) and it stops at his feet for three heartbeats.
  - It slides away and fades. He rises, and the camera turns with his eyes to the shutter, leading into shot 6.
- `StoryIntro.SearchCurtain` builds the light at runtime from the searchlight's own material, with its floor strip a clone of the search pool.

### The drop, realistic
- The drop's texture had the roof crack drawn inside it as a sharp zigzag ("refracted"), so the drop itself looked cracked. It's rebuilt as a ball lens seen from below against the light, with nothing in it a hard line:
  - nearly clear water
  - the light above as a soft, straight, inverted band low across it
  - an edge darkening where the water bends the dark room in
  - a hairline crescent of light on the lit side only
  - one small sharp highlight
  - a soft caustic at the bottom
- Scene version 30.

### The drop comes from the crack
- The drop used to form at the head's position under the roof, which could be up to about 18 cm off the crack's jagged line, so it seemed to appear out of the dark.
- It now hangs from the crack itself. `CrackPoint(z)` reads the crack's own mesh for its centre line at that depth, and both drops (shot 1's and the one that lands between his eyes in shot 2) form exactly on it, at its underside.
- Water on the seam:
  - five small beads cling along the crack (they stay through shot 2, while the camera still looks up)
  - one bead runs down the seam to where the drop gathers as it begins to swell, feeding it

### The roof crack, one and real
- Looking up in shot 1 there were two different "cracks" that didn't line up:
  - the Blender set's rectangular hole in the roof sheet, with seven flat rusty "flaps" at random angles inside it (a dark box of tilted orange cards)
  - beside it, the game's glowing jagged seam (the one the drop falls from)
- The set's roof is now whole (`build_story_set.py`): no hole, no flaps, no ribs across the seam. The storm light that came through the hole is now a narrow light just under the roof, so the pool on the floor where he lies stays.
- The glowing seam is the only crack. `StoryBuilder.TornLips` gives it torn metal, built on the seam's own jagged outline so it follows it exactly:
  - a ragged lip of iron bent down into the room on each side, with a pale worn edge where it tore
  - rust bled out round it
  - all children of the crack, so they move with it
- Scene version 31.

### The drop falls on you
- In shot 1 the camera now sits exactly beneath the drop's point on the crack (`CrackPoint`) and looks straight up the line it falls. Before, it sat under his head, a few centimetres off, and looked slightly off vertical, so the drop slid sideways out of frame as it came.
- Now it stays dead centre and only grows.
- It lets go with a tiny tick (`DropRelease`, timed so it arrives just before the cut), and its air is heard close.
- At 45 cm it hits the lens (`LensHit`), forced just before the cut if the timing runs short:
  - a wet tap and a jolt, the picture blurring
  - on the glass: the burst blob with its bright edge, beads thrown round it, runs sliding down, fading as his eyes start to open in shot 2

### Shot 5: WHERE AM I (replaces THE LIGHT)
- After the wound and the static he doesn't know who or where he is, so the shot is him looking round, cut between his face and what he sees:
  1. close on his face as he lifts his head and turns past us
  2. his POV, the hall to the left, drifting and soft
  3. under his chin as he tips his head back against the rafters
  4. his POV, up: the rafters, the fan, the crack, the room rolling over a heartbeat
  5. a chain clinks behind him; his POV whips round in a blur to nothing
  6. a wide low shot from far across the floor as he turns to the moonlight under the shutter, into shot 6
- His facing follows a keyed path (`GazeYaw`). His head tips back via the head bone after the animator (`GazePitch`, `LateUpdate`).
- The searchlight curtain is removed.

### The second drop is real; the drop is visible
- After the blinks a second drop lands between his eyes. It was drawn as a cartoon burst: a white disc and twelve dots flying out like a firework. Removed.
- Now his eyes, drifting, find the drop gathering on the crack and hold on it, straight up its fall line. He comes into focus on it, and it falls into his eye: the same lens hit as shot 1 (`LensSplash`). His lids snap shut and reopen on water on the glass.
- The drop was nearly invisible against the dark roof. It now has more body, a brighter lit edge, a stronger highlight, a clearer refracted band, and a little more size, so it can be followed all the way down.
- Scene version 32.

### No second drop
- Shot 2 is just his eyes: heavy blinks, then they open steadily and the picture clears into focus (the blur goes to nothing by 10.5), with a breath as he comes to. Then the cut to the face in the puddle.
- The second drop, its lens hit and the snap-shut blink are gone. The eyelid keys now end fully open at 10.45.

### Chapter 1: the warehouse fight, the Foreman, the ending (first pass)
- **The hall** (`tools/blender/story_hall.py`): a complete building through the inner shutter, 48 × 22 m, 12 m to the ridge.
  - Brick walls with high steel windows (some broken or boarded), a truss roof with skylights (one torn), columns, pipes, graffiti.
  - Outside: the yard face with its lettering, docks and floodlights, and the open main door.
  - Four zones:
    1. the loading bay: dock doors, trailer, pallets, roll cages, the forklift with its forks raised over the lane
    2. the racking aisle: two racks across the lane, more receding, the monorail hoist
    3. the mezzanine: steel deck, stairs, the lit site office, the workshop under it
    4. the crane bay: gantry crane, presses, coils, drums, the puddle under the torn skylight
  - Lit for fighting: a lane fill and a wall wash per zone. Baked with a lightning version.
  - The store got its missing front wall. Front walls (`HallFront`) are cut away by `CutawayWall` whenever the camera is outside them.
- **Navigation** (`LevelNav.WarehousePlan`):
  - the forks (1.62 m), both racks (2.2 m) and the mezzanine (3.4 m)
  - stairs up at 37.6, a climb ledge at the mezzanine's far end
  - the hoist chain as a rope swing, the swing bar over the mezzanine, crates in every zone
- **Zones** (`StoryHall`):
  - The arena, the enemies' walls and the camera lock to the zone (`CameraDirector.SetBounds`: asymmetric bounds), and the doors move for each zone.
  - Seven waves with caps. **GO →** when a zone is clear; the next zone closes behind him.
  - A death restarts at the zone (`HallCheckpoint`).
  - The way in is a cinematic: voices beyond the shutter, he heaves it up, the hall opens and the first of them come.
- **New enemies:**
  - **Runner:** small and fast, one jab and he darts out.
  - **Grabber:** big, mostly grabs; tap to break free.
  - **Door-man:** a corrugated sheet that blocks high strikes from the front. Kicks, slides, hits from behind and heavy blows get through; four blocks or one heavy blow tear it away.
  - Hero hits carry `low` for kicks.
- **The Foreman** (`StoryForeman`):
  - Rides the crane hook down out of the dark and lands with a crash; roar and name card.
  - The brute's specials with a crowbar, flung away when enraged.
  - The hook swings across the floor every so often, telegraphed in red.
  - His own health bar.
  - His last blow goes to the ending (`Enemy.StoryFinale`). A defeat by him restarts at him.
- **The ending** (`StoryEnding`): the 15 shots of the design, with new clips `kneel_hold`, `kneel_grip`, `lie_up`, `lie_gun`. His head shakes "no" via a head-bone override. The photo is drawn at runtime.
  - Then CHAPTER 1 COMPLETE · THE HITMAN · MEMORY RESTORED: "You kill people. You are good at it."
  - The end card now says CHAPTER 2 · THE WIFE · COMING SOON.
- The menu starts a fresh run (`StoryIntro.NewRun`). Scene version 34.

### The hall's lighting and kit

- **Lighting fix:** the hall was dark because the cut-away front walls were shading the lane in the bake. They now stay out of the lighting bake and only switch on for their own bake.
- **Oil drums** (`OilDrum`):
  - There are four: one in the loading bay, one under the mezzanine and two in the crane bay.
  - Any strike with no one in the way kicks a drum over. It rolls along the floor, knocking down everyone in its path (DRUM ROLL!).
  - It slows with each man it hits, slams off the zone's walls, then lies still until it is kicked again.
  - A Foreman bull charge into a standing drum stuns him and sends the drum rolling on. His swinging hook knocks drums rolling too.
- **Rack spills** (`RackSpill`, one per pallet rack):
  - A man knocked flying low through a rack bay rattles the frame (RACK SLAM!). A moment later, 4–6 boxes tumble off its decks into the lane.
  - Any man underneath is knocked flat (INCOMING!).
  - Three spills per rack, with a settle time between them.
- **Crate fix:** kicked crates no longer smash at once in the hall. They used the old room's ±12.6 m walls; they now use the arena they were kicked in.
- Scene version 35.
- **Office glass** (`OfficeWindow`):
  - Knock a man down hard on the mezzanine in front of the site office, and he is thrown back over the sill and through the window (THROUGH THE GLASS!).
  - 34 shards blow out over the deck and down to the floor; he lands among the desks inside, dead.
  - Jagged teeth stay in the frame. The baked glass was removed from the set: the pane is the game's own.
- **Fix: the raised floors were not solid.** The forks, rack decks and mezzanine are now collider slabs; the mezzanine's runs back over the office. The stairs are a walkable slope. Before this, anyone stepping or climbing onto them dropped to the ground.
- Scene version 36.

### The Foreman in three phases; the ending redone with the phone call

- **The Foreman** (`StoryForeman`, `Enemy` Foreman habits): 1.4× health, three phases marked at 66% and 33% on his bar.
  - **I · The crowbar:** a guard (plain front punches bounce off when he isn't attacking) and three-swing crowbar combos. The third swing is red and floors you; if it misses he is open for 1.15 s.
  - **II · The crane:** he runs for the crane's hanging control box and can't be hurt there. Two men come in, the hook swings every 3–5 s, and the crane drops loads of crates on red rings. Reach him and he lets go.
  - **III · Bare hands:** enraged, no guard, 1.2× damage: headbutts and double slams. He is winded for 1.8 s after a slam, limps below 40% health and breathes hard.
  - **Throughout:** he catches the fifth strike of the same kind in a row and throws you ("HE READ YOU: MIX IT UP").
- **The ending** (`StoryEnding`):
  - From the kneel on it is side on, in silhouette, against a lit curtain of rain under the torn skylight (`BuildBacklight`, `Rain`).
  - The pistol is posed in world space (`PoseGun`), so in the struggle its barrel visibly turns from the hero to the Foreman.
  - Removed: the photo of the girl, "his son" and "say hello to her". The Foreman's line is now "Five million reasons, friend."
  - **New: the phone call.** The dead man's phone buzzes in the water. Its screen (`PhoneGUI`) shows a surveillance still of the hero: THE FERRYMAN · $5,000,000 · OPEN · CONFIRM KILL. The voice: "Is it done?" … "Ferryman. You were supposed to stay dead." … "The contract is open. Every gun in this city is coming for you."
  - The memory line is now "Someone put you in the ground. You climbed out."
- **New sounds:** `phone_buzz` (motor in bursts, case rattling on concrete) and `phone_click` (pick-up, and the hang-up tones).
- **Vibration setting:** the pause menu now has a VIBRATION ON/OFF button next to SOUND, saved like sound (`Haptics`). Every phone buzz goes through `Haptics.Vibrate()`, and turning it back on gives one buzz to confirm. The pause menu is now four rows: RESUME | MOVES, SOUND | VIBRATION, RESTART | HINTS, MAIN MENU.
- **Temporary test shortcut:** the main menu has a TEST · SKIP TO THE BOSS button under the two modes. It starts story mode straight in the crane bay with the Foreman arriving (knife in hand), using the same checkpoint path as a defeat by him. It is controlled by `MainMenu.DevShortcuts`; set that false before release.
- **The gun scene, shot by shot.** Ten beats now:
  - the draw (an insert on his hand going under the jacket)
  - the hero's face, unaware
  - the gun rising in slow motion
  - an extreme close-up of the hammer in profile ("Nothing personal")
  - the hero's eyes, as the world goes muffled
  - the grab, with a whip pan
  - the struggle in fast cuts: two-shot, gun, the Foreman's teeth, the hero (`TurnCurve`: the barrel swings away, back at the hero, then over)
  - the turn: his heart stops, total silence
  - the shot: muzzle flash and smoke (`MuzzleFlash`)
  - a slow death in the smoke: his last breath, the gun into the water, the ink spreading
  - A heartbeat (`Heart`) climbs from 62 to 175 bpm and then stops.
- **New sounds, from real recordings where they exist:**
  - `gunshot_indoor`: a synthesised crack, real impact and boom layers, slap-back echoes and a 3.4 s warehouse tail
  - `gun_draw`, `gun_cock`, `struggle`, `death_breath`
  - `shell_casing`: real brass on concrete
  - `gun_drop_water`
  - `phone_buzz` now uses a real phone vibrating on a table

### Who is speaking

- **One subtitle system for the whole story** (`StoryDialogue`, `docs/story/dialogue.md`):
  - plain names from the player's side: YOU (was "HIM"), THUG, THE FOREMAN, VOICE ON THE PHONE, VOICE BEYOND THE SHUTTER
  - one colour per kind of speaker: YOU is white with the red bar, enemies amber, a boss blood orange, off-screen voices cold blue with a signal mark
  - a marker over the speaker's head, in his colour
  - the subtitle panel slides towards the speaker
  - every line sits on a dark panel with a coloured edge
- The speaker is found automatically (the hero, the boss, or the nearest enemy) unless a line passes one.
- **Subtitles moved into the black.** The bottom letterbox bar grows to hold each line; in gameplay a dark fade rises from the bottom instead. The name and line are centred in two rows, leaning towards the speaker, and drawn last so nothing covers them.
  - The phone screen now fits between the top bar and the words, laid out as fractions of its own height. Before, it covered the call's lines.
  - The fight's controls hint moves above a line of dialogue.
  - A layout mock is in `docs/previews/subtitles_layout.png`.

### The story set in the fight-mode tower's look

- **Palette** (`build_story_set.py`: `TOWER`, `tone`, `TONE`): the warehouse and the yard now use the tower's black-and-white world, and only the fighters carry colour.
  - The hall's brick is pale cast concrete in formwork panels.
  - The safety paint and the rack beams are the tower's white edges; the rack uprights and the steel are near-black.
  - Containers, cars, drums, cardboard and stock are greys at their own brightness.
  - Lamp shades are black.
  - Every light is neutral white at the same strength (`NEUTRAL`), and the night sky is neutral.
- **Light like the tower** (`story_hall.TOWER`): all six hall lamps are lit and stronger, so each zone has a pool of light. The fills stay at full strength (the hall's baked brightness is back to before: 90th percentile 13, previously 14), and the white light no longer loses half its energy to the old blue tints.
- **Reflections:** the floor really reflects.
  - `PlanarReflection` renders the scene mirrored in the floor's plane into a half-resolution texture.
  - The `ProjectInk/WetFloor` shader lays it over the floor: faint on damp concrete, strong in the puddles (`StoryWetMask`), strongest at grazing angles, shivering with the rain.
  - One plane covers the store, the hall and the yard.
- **The tower's light effects** (`StoryBuilder.TowerFeel`): its additive cones of light and glows under every working lamp, a low haze over the hall, the mezzanine and the store, and dust drifting in the light.
- **Props in greys:**
  - crates use the tower's crate colours
  - drums are dark grey with pale hoops
  - fallen boxes, crane loads, the hook, the pendant and the chain are grey
  - moonbeams, searchlight, crack light, puddles and office glass are neutral
  - red stays only for warnings (telegraphs) and the helicopter's light
- Scene version 37.
- **Rain only outside.** The one 70 m rain emitter predated the hall, which now stood under it. Rain now falls only in open yard, in three patches at the same density:
  - behind the store (what the shutter opens onto)
  - past the hall's far end (the walk-out)
  - behind the hall
  - The lit rain in the ending's backlight is gone; the light stays, as the floodlight through the torn skylight in the haze.
  - Indoors the puddles stay, with only a faint stir on the water (`_Ripple` 0.0007).
  - The drip from the roof crack in the prologue stays.
  - Scene version 38.

## Chapter 1's new last beat, and Chapter 2: The Bank

- **Story arc** (`docs/story/story_arc.md`): the seven levels and what each reveals. **Shot list** (`docs/story/chapter2_bank.md`).
- **Chapter 1 now ends on his coat** (`StoryEnding`, `StoryKey`).
  - In a lightning flash he sees his long black coat over the drums. He lifts it; a flash cut tells him it's his.
  - In the inside pocket is a key on a leather tag, shown full screen: MERIDIAN TRUST · SAFE DEPOSIT · Nº 417. He says "Meridian Trust...".
  - The coat goes over his arm and he walks out.
  - The card's memory line is back to "You kill people. You are good at it.", with **CONTINUE → CHAPTER 2**.
  - The phone call is kept (`PhoneGUI`) for a later chapter.
- **Chapter 2: The Bank** (scene `Story_Bank`, `LayoutKind.Bank`, `BankBuilder`, `StoryBank.cs` as StoryIntro's chapter 2):
  - **The set** (`tools/blender/build_bank_set.py`, in the tower palette): a wet morning street with shops, the black car at the far curb, and the bank.
    - The front: a stone portico with MERIDIAN TRUST cut in the entablature and the bank's mark (a keyhole in a shield) over the doors.
    - Inside: a marble lobby (clock at nine, pendant globes, teller windows behind glass, a velvet rope, benches), a corridor through a barred gate, the round vault door standing open, and the wall of numbered boxes.
    - The front wall is cut away while the camera is outside it. The marble and the wet sidewalk reflect, sharing one mirror render (`PlanarReflection.shareWith`). Lamp cones, morning shafts, haze and dust as in the tower.
  - **B1–B22:**
    - the title over the street
    - a match cut from the key to the mark in stone
    - the clock strikes nine
    - you walk to the doors, then to the counter
    - "Box four-seventeen", and the clerk's screen flags the box
    - Harrow leads him through the gate and the round door
    - two keys turn and the box comes out
    - the clerk's call ("He's here.")
    - you open the box: three passports (three names, his face), the ring ("E. — always"), the memory, "...Elena.", the ring put on
    - the envelope in her hand ("For when you need to come home. — E. · 14 ALDER LANE"), "...Home."
    - you walk out, and across the road the black car's door opens
    - CHAPTER 2 COMPLETE · THE RING
  - The bank's people are grey figures (puppets): the clerk, Harrow, the guard and two waiting customers.
- **Memory stills** (`tools/blender/render_memories.py`, in `Resources/Story/Memories`): the altar, the first dance, the kitchen, the sea, her face. They're soft high-key black and white, made with the game's own figures (Elena a white figure with dark eyes, hair, a dress and a veil).
- **Subtitles:** HER · A MEMORY is an off-screen voice.
- **Menu:** a test button for CHAPTER 2 · THE BANK, next to SKIP TO THE BOSS.
- Scene version 39.
- **Elena, and her memories, in detail** (`render_memories.py`):
  - **Elena is built as her own character** on the game's rig: slimmer limbs, big dark eyes with catchlights, lashes, blush, a smile (or a laugh, or an "oh"), and dark hair with bangs plus long waves, a bun with a ribbon, or a ponytail. Outfits: a dress with a bowed belt, a gown with a lace hem, sash, crown of flowers and veil, a polka-dot sundress with a sun hat, and a jumper and apron with a heart pocket. His wedding suit gets a shirt front, bow tie and flower.
  - **Eight memories** (24 s): the rainy cafe where they met, the proposal on a bridge, the altar, the first dance, moving day at 14 Alder Lane, the floury kitchen, the sea at sundown, her face.
  - **Her lines** run across the montage (`MemoryLines`).
  - **Its own sound** (`memory_elena`): a piano waltz with a chord per memory and a string pad, over each memory's real sounds (rain, river, applause and bell, chimes, birds and keys and a door, the kitchen, the beach, her breath).
- **The coat and the key, as a scene with his thoughts out loud** (`StoryEnding` beats 9–15, `MatchGUI`):
  - "Who am I?"; he searches the hall.
  - Lightning shows the coat. "...What's that?"
  - He walks to it. "A coat... Blood on the collar. Mine." He lifts it: "It looks like mine." A flash cut: "It IS mine. They took it off me."
  - "Let me check what I can find." The pockets: "Empty...", then a CAFE NOIR matchbook (the cafe on the bank's street), then "something in the lining".
  - The key: "What is this...? A key. Too small for a door... It's a locker key. Meridian Trust. A bank."
  - The decision: "Whoever I was... I locked something away there... Maybe the reason they want me dead. Then that's where I'm going."
- **More of his lines through chapter 2:** at the mark, the street, Harrow ("Eleven years, sir. Paid in cash."), the passports, the ring ("Who is E...?"), the envelope, and walking out ("Elena... I'm coming home.").
- **The bank, fixed after a look in the editor:**
  - **The clerk was hidden.** The counter's glass had been baked as an opaque dark pane. It's now real clear glass (`CounterGlass`) with glints, and the bank's people are darker greys so they read against the pale stone.
  - **The front doors:** the baked revolving door (an opaque block) is gone. Two glass leaves in bronze frames are locked overnight. The guard walks to them, turns the key, drops the floor bolt, pushes both leaves open and holds one. "Morning, sir." He follows the hero in to his post.
  - **Box 417, by dual control, as a real bank does it:** Harrow's guard key in first, then the hero's key, "Together"; both keys are shown turning. Harrow draws the box out (the slot left empty), carries it in both hands to the viewing table and sets it down beside a bell. "Ring the bell when you're finished." Then he leaves him alone.
  - The bank set was rebaked without the counter glass and the revolving door.
- **The key is found on the Foreman's body** (`StoryEnding` beats 9–14, `NoteGUI`). The crew searched him when they dumped him, and the Foreman kept the key for his boss.
  - He kneels and goes through the dead man's pockets: cigarettes, then the CAFE NOIR matchbook, then (in a lightning flash) the inside pocket.
  - His key: "No... that's MY key. They searched me when they dumped me here."
  - The boss's note, torn and handwritten: "Get what's in 417 before the contract closes. Then finish him."
  - The deduction: "So that's why I'm still breathing... They wanted what's in that box." The decision: "Whoever sent you is going after it. Then I'll get there first."
  - Chapter 2 opens on the race ("I have to get to that box before they do.").
  - The coat beat is gone.
- **Test checkpoints** (`docs/story/test_checkpoints.md`): the menu's single test button now opens a list of checkpoints.
  - Chapter 1: opening, the shutter, each hall zone, the Foreman fight, the ending with the boss already beaten (no fight), and the ending from the search of the body.
  - Chapter 2: start, counter, vault, box, walk out.
  - The pieces: `StoryTest.JumpToEnding`, `StoryEnding.EndingFrom`, and the bank's `BankFrom`/`TestState` (the bank's sequence is now in blocks that can be entered mid-way).

## Fight move library (CONTACT kit, part 1)
- 14 new mocap clips from the CMU database. Karate: oi-zuki, gyaku-zuki, mae-geri, mawashi-geri, side kick, gedan-barai. Boxing: lead hook, jab-hook, one-two-three. Also a slip, a fall on the face, and get-ups from face down, side and back.
- 38 hand-keyed clips: strikes (elbow, back fist, palm heel, low kick, hammer fist, headbutt, clinch knee, clothesline), parries and blocks, the clinch, judo throws (hip throw, shoulder throw, sweep), pro-wrestling slams (suplex, body slam, DDT, spear), and new hit reactions. Paired moves come as attacker + `_v` victim clips; `endShift` tells the game where the thrown man lands.
- No rain after the warehouse: chapter 2's rain sound is now silent (the puddles stay), and the docs record the rule.
- See docs/design/move_library.md.

## CONTACT combat (story mode), part 2: the hero
- Grounded story movement: a 3.3 m/s jog, a short hop, a quick step-in instead of a dash, a step back instead of flips, no wall runs, no springboard or death-from-above dives, no giant swing.
- Named strings from the new clips (boxing, karate, muay thai), each finisher labelled. A dazed man or one bent double gets a context finisher, and a man behind you gets a back kick.
- Parry: a tap into a bare-handed swing knocks it aside and leaves him dazed for the follow-up. The head scissors now happen only on a dazed man (no more dice roll).
- Clinch: swipe at a man or walk into him. From there, knees and a headbutt, a leg sweep, hip and shoulder throws, a suplex or body slam, a DDT, a shove; brutes are too heavy until worn down. Spear and clothesline work from range. Paired clips put the thrown man where he lands (endShift), and he gets up the way he fell.
- Hit reactions match the blow: body shots fold him, low kicks buckle the leg, chin shots snap the head back, heavy ones stagger him back.
- ContactHints teach each move at the moment it's possible, until it's learned.

## CONTACT combat, part 3: enemies that fight (story mode)
- Closer rings (1.4–2.2 m) and up to three attackers at once.
- Strings, not single swings: jab-cross, low kick-hook, front kick, jab-jab-cross, lunge-hook; the doorman throws a three-piece, the grabber mixes a bear hug with a haymaker and an elbow. Every strike of a string gets a short red beat, so it can be parried.
- Guard: a man standing ready may block (block_hit, 20% damage) and then counter at once. A move used twice in a row is read ("HE READ IT: MIX IT UP"). Two blocks in a row and the guard opens; finishers and dazed men never block.
- Runners slip light punches and come straight back. Feints: some pull the wind-up halfway and come again.
- Flanking: two on the same side and one walks round (a step out of line) to the hero's other side.
- Fear: below 30% health a man stands further off and waits longer.
- The hero's hit reactions match the blow too. The Foreman's bare-handed phase can be parried (a shorter daze for him); his headbutt uses the real headbutt clip.

## Chapter 2, part two: the street
- A new set (`tools/blender/build_street_set.py`, baked): about 140 m of the morning-after street in the tower palette. MERIDIAN TRUST's columns and steps, Cafe Noir's terrace, a bus shelter, the market with its stalls and a scaffold, the elevated railway on its pillars, and the crossroads with a hotel, a side street, a phone box, a newsstand and a stalled bus. The Driver's car and a four-carriage train are separate objects the story moves. No rain: wet, reflective pavement and road, puddles, haze.
- `StreetBuilder` builds `Story_Street` (wet sidewalk and road sharing one reflection, lamp cones under the railway, haze, dust, fruit crates and bins to kick). Added to Build All and the build settings; SceneVersion 41.
- `StoryStreet`: the bank's last shot now fades straight into the street (the chapter goes on, with no card). The opening is "The box. Hand it over." / "It's empty... Come and look.", and the first swing slows down to teach the parry. Four zones of waves, a walking line, GO between zones. The train overhead strobes the light and startles every enemy (so they're dazed for context finishers) every 22–28 s.
- THE DRIVER, the street's boss, in four phases: a boxer (guard, combinations, all parryable), a tire iron from the boot (three-swing strings, open after the third), the car (he drives along the pavement at you; swipe to DIVE CLEAR; it ends in the railway pillar), then bare hands and furious. His health bar marks the phase changes at 60% and 30%.
- Finale: a fist in his collar, "Who sent you?" / "You'll find out at home. We got there first." / "...Elena.", then he runs north up the side street. CHAPTER 2 COMPLETE · THE RING; next, CHAPTER 3 · HOME.
- New sounds: a train pass-by, car revs, car doors (from the Oculus audio pack).
- Test checkpoints: the box, the market, the railway, the crossroads, the Driver, after the Driver. The test panel's rows now shrink to fit.

## Chapter 3: HOME
- A camera language for the cutscenes (`CineCam`): fifteen moves (orbit, arc, pull-away, truck, roll, pan, crane, dolly zoom, tilt, dolly in and out, overhead rotation, pedestal, low arc, lateral reveal), each chosen for what its beat means, with held frames where stillness is better (docs/story/camera_language.md). The camera director gained a scripted up direction for true overheads.
- The house (`build_home_set.py`, baked): Alder Lane at dusk, No. 14 cut away side-on over two floors (the hall and stairs, study, living room, kitchen; the landing, Maya's room, the bedroom), the garden and the play area. It has its chalk drawing, the black car, a small raincoat, Maya's fairy lights and drawings, and a low sun through the back windows.
- Maya's memories (`render_maya.py`): a child figure in Elena's figure language (her bob and pigtails, the ribbon). Six soft stills and three broken ones of how Elena died. Her own music-box theme, and new sounds: creaks, a buzz, a fridge hum, a swing.
- `StoryHome`, H1–H22: the arrival, the car, the chalk ("...Three."), the photo ("...I have a daughter."), the memory, Elena in the kitchen, the broken flash, the voice upstairs, RAGE (red edges, heavier blows, enemies hang back), four rooms of fighting right to left and up the stairs, Maya's room, THE CLEANER (knife / choke / anything, with a disarm), the kitchen chair (restrained: light, sound, silhouette), the small footprints, the moving swing. CHAPTER 3 COMPLETE · THE DAUGHTER.
- New props: the glass coffee table a man goes through, the kitchen chair, the bulb, the footprints. New rig poses: seated and bound, the shock, the slump, holding the cord, kneeling to hold her hand.
- The street's end card now continues to chapter 3. Test checkpoints for every beat. SceneVersion 42.

## Chapter 4: THE BACKYARD
- The night set (`build_park_set.py`, baked): the back of No. 14 with the kitchen window lit, the garden and Maya's play area (slide, swings, sandpit), the broken back gate, the park's playground, the pond and its footbridge, the car park and the white van. On the horizon, Kessel Hill and the fortress on it, its windows lit. Moonlight, park lamps with cones, mist, the pond reflecting.
- Stills (`render_backyard.py`): her ribbon in the grass, the game's one red, the rest black and white; four broken stills of how she was taken (under the slide, the hand, carried off reaching back, a small hand on the van's glass). A crickets night sound.
- `StoryBackyard`, Y1–Y15: her footprints (a low truck beside him), the swing stopping, the ribbon (a crane down), "PAPA!", the vow with the ribbon tied round his wrist (a slow arc), torches at the fence (a pan), four stretches of fighting with torch-carrying men, THE HANDLER (the grip, the knuckles, cornered), the address ("Kessel Hill. The old fort... He knows you. He said you'd come."), a tilt from the ribbon on his wrist up to the fortress, a drone pull-away as he walks towards it. CHAPTER 4 COMPLETE · THE PROMISE.
- From here on the hero wears the red ribbon on his wrist. Home's end card continues to chapter 4. Test checkpoints for every beat. SceneVersion 43.

## Chapter 5: THE FORTRESS (the storm)
- A detail kit for the sets (`tools/blender/detail.py`): procedural surfaces that bake (weathered cut stone in courses with mortar and grime, cobbles, layered rock with cracks, planks, wet ground), bevelled edges, noise terrain, rocks, storm-bent pines, round towers with arrow slits and battlements, arches.
- The fortress (`build_fortress_set.py`): the cliff road (a rock face, pines, the drop to the valley's lights, the gang's truck, a floodlight), the rock stair, the gate (the drawbridge on chains, twin towers, the arched gatehouse with its portcullis, braziers, banners), the courtyard (cobbles, the well, carts, barrels, the generator shed and floodlights, the keep's windows) and the great hall (pillars, the long table, the fireplace, chandeliers, tall windows); the observatory tower above. Two bakes: the storm night, and lit by a strike, flickered between by LightningSwap.
- The storm: rain that follows the camera outdoors, wind and rain sound (muffled inside), strikes every 6–12 s with forked bolts and thunder, rain on the lens, rippling wet stone and road.
- `StoryFortress`, T1–T16, cut as coverage: the title on a flash; the fortress from the air; a boot in a puddle, him walking into the lens, his profile, the soaked ribbon; the reverse from the battlements and the guards; the cliff road fight; the stair climb with a lightning silhouette; the gate fight; the trap (the portcullis slams down in slow motion); the courtyard; the blackout (a strike on the floodlight pole, the generator bursts, then the fight lit only by lightning); the great hall reveal and THE WARDEN (the chain, the chandeliers cut loose and crashing down, bare hands); "PAPA!" from above; the man and the child on the tower's gallery, "Come up, old friend. She's been asking for you."; a drone pull-away. CHAPTER 5 COMPLETE · THE STORM.
- Big hits in this chapter sometimes get a strike, slow motion and a kill-cam arc. Test checkpoints for every beat. SceneVersion 44.
- The finale, on the same night in the same level (`StoryFinale`): up the outside of the tower in the strikes; the last roof, a terrace over the great hall with gargoyles, lightning rods, a broken end over the drop, and the observatory's open dome and telescope above; VOSS at the edge holding Maya. He tells him over three broken stills (`render_finale.py`: "I'm out, Voss.", the phone at the window, the pipe in the warehouse), and he remembers. The duel: I the teacher, II the gun (a red aim line; slip the shot or get in close for a slow-motion disarm that sends it off the roof), III the edge in constant lightning. The fall from the broken parapet, Maya, the ribbon back in her hair, the storm passing into dawn, the road home, THE END. The end card offers to play from chapter 1.
- A score (`tools/audio/make_score.py`): six synthesised cues (storm, fight, dark, boss, duel, finale) and a music channel in `Sound` that crossfades them and bends them in slow motion.
- Test checkpoints for the last roof, the duel and the end.

## Story v2: the dots connected (docs/story/story_v2.md)
- The story was rebuilt around one object and one man. THE BOOK: his record of 41 killings ordered by "V.", locked in box 417, opened only by his living hand, sent to the police if he dies. That dead-man's switch kept his family safe for six years. VOSS, who raised him: when Detective Harrow came asking, Voss struck first. SILAS, Voss's real son and the Cleaner, killed Elena against his father's order. Every chapter now answers one question and asks the next.
- Seven chapters: 1 The Warehouse, 2 The Bank and the Street, 3 Home, 4 The Backyard, 5 THE ROAD (new), 6 The Fortress, 7 HOME (new epilogue). Then THE WHOLE STORY: a timeline recap with its own score.
- Story controls (StoryControls): tap, hold, mash, swipe, choose, drag, one look all game, never a fail screen. Used for opening his eyes, breaking the zip ties, the search, unfolding the note, the palm scanner, turning the book's page, turning the photo over, closing Elena's eyes, three questions for Silas, answering V's call, the reflex, pulling the ribbon free, talking on the walkie, the chase, counting the thunder, humming, the trade, reaching for Voss or letting go, tying the ribbon, the stone, the swing and the drawer.
- One silhouette per character (Dress): the Foreman's hard hat, the clerk's glasses, the Driver's cap, Silas's apron and gloves, the Handler's tracksuit and knuckles, the Warden's chain, Voss's long coat, silver hair, gloves and cane, Harrow's hat, Maya's pigtails.
- Maya's walkie-talkie: missing from her shelf (ch3), her voice from the van (ch4), the whole drive (ch5), "I see you!" from the tower (ch6), left on Elena's stone (ch7).
- New stills: the old photo of V with the young hero and the boy Silas; the blow redrawn with Voss walking away into the light. New rig poses for driving, the walkie, the grave, the swing.

## Chapter 6: the walk-out ("Over and out", docs/story/walkout.md)

The game's highlight: one long take of the father and daughter walking out of Kessel Hill at dawn while it blows up behind them, with slow motion on the bridge at the end. It follows the camera work and timing of a famous TV walk-out; nothing else is taken from it.

- **The set.** `tools/blender/build_fortress_front.py`, imported by `build_fortress_set.py`, builds the fortress's front: facade, porch, forecourt, gate, bridge, gorge and moor.
  - The front exists only in this scene. FortressBuilder gives it to LightningSwap as `dawnOnly` renderers.
  - Every part seen from it gets a third bake, `Story_<part>_Dawn.png`. `LightningSwap.Dawn` swaps the whole set to that bake; the roof's dawn uses it too.
  - The script has three modes:
    - `--walkout-preview`: the set's shots;
    - `--walkout-board`: the take's key frames with stand-ins;
    - `--dawn [--parts=A,B] [--export]`: the dawn bakes only. UV unwraps are deterministic (checked against the old FBX), so the night and lightning bakes stay valid.
- **The scene.** `StoryWalkout.cs` keeps a clock for the take and an event list. The camera is a reverse dolly that waits at the porch, backs away in front of them, creeps in while they talk, and pulls out, up and wide in the slow motion.
- **PuppetRig** (Core) adds hand business on top of any clip: a two-bone reach with a pole and blend weights, and head nod and turn. The hands are joined, the walkies raised, a hand put on her head, she hugs his leg, the man grips the plunger.
- **BlastFx**: flash, fireball, black smoke burst and column, stone blocks thrown out that bounce on a ground plane, sparks, a dust ring, a fire that stays, and ash drifting round the lens. It runs on scaled time, so the slow motion floats it.
- **Sound:** explosion, debris, fire roar, walkie click, plunger, birds and embers, plus the `walkout` score cut to the take. `Sound.PlayTake` picks a take; `Sound.KeepClear` keeps a directed slow motion's sound unmuffled and at true pitch.
- **Setups:**
  - ch5: two clicks means you're okay;
  - the Warden: "Boss falls... hill burns.";
  - Voss: "If I fall, Kessel falls with me.";
  - ch7, Harrow: "Kessel Hill's still burning."
- **Checkpoint:** chapter 6, "The walk-out" (`FortressFrom = 10`).

### The walk-out, the blast rebuilt (whole fortress in frame, realistic)
- **Timing.** The blast now comes when they are near the far end of the bridge (t 47.2 of a 69 s take). The camera falls back, widens to 42° and tilts up down the bridge, so the whole of Kessel Hill is behind them when it goes.
- **The chain.** It runs over ~5 s: the door, the balcony, every window low and high, the turrets, the roof, the stables, the courtyard, the towers, the keep, the dome.
- **Pieces that fall:** the dome (`FortDome`, split off `FortRoof`; its night, lightning, dawn and burn bakes are redone), the two spirelets and four chimney stacks.
- **Burning bake.** Every part seen in the walk-out gets a `_Burn` bake (`--bake=burn`): windows full of fire, the morning dimmed. `build_fortress_set.py --bake=night,lightning,dawn,burn --parts=...` bakes any mix.
- **ProjectInk/FireLit** (Assets/Shaders/FireLit.shader) draws the dawn set. It fades into the burn bake (`_FortBurn`), takes up to eight flickering fire lights from the blasts, and lifts with the flash.
- **BlastFx v2.** Flipbooks from `tools/fx/make_fx_textures.py`: fireball, smoke, flame, dust, a shockwave ring. Stone blocks trail smoke (a sub-emitter), burning fragments leave fiery trails, there's a dust wall along the ground, pillars and smoke caps for the biggest blasts, and haze drifting past the bridge.
- **Sound.** Five explosion takes, ExplosionBig, Collapse, GlassRain, Shockwave, Rumble and new debris; each blast is heard late by its distance. The score is re-timed to the 69 s take.

## Combat 3.0, step 1: the experiment in chapter 1 (docs/design/combat_v3.md)

The old controls are kept on the branch `backup/combat-v2`. Chapter 1 now runs on a new body, and its pause card has **COMBAT NEW / CLASSIC** (saved; NEW by default). Both modes use the new body; every other chapter is unchanged.

- **The V3 body and moves** (`tools/blender/build_stickman_v3.py`, `v3_rig.py`, `v3_clips.py` → `Assets/Art/StickmanV3/`).
  - The strike lab's body: arms 25% longer, fists, shoulders and hips wider. Every old clip (story, mocap, throws) is carried over onto it.
  - A new pose engine: absolute side-plane angles, plus **torso twist** (the hips and shoulders turn through a cross or a hook) and **limb yaw** (a hook's forearm comes across the face, a roundhouse's shin swings round from the side).
  - Legs posed by **foot targets** with IK solved on every frame (planted feet never slide; a step lifts a foot through an arc). Fists posed by **fist targets** at the impact key, so each blow lands on the target man's chin, body or thigh at its own reach (`--report` prints the error: within a few centimetres for all of them).
  - Baked on every frame with per-key easing: accelerating into the impact, a hard stop, one frame of penetration, a return faster than the way out, a small settle.
  - 38 hand-keyed clips: the live guard (breathing, weight rocking, hands alive), jab, cross, lead hook, rear hook, uppercut, liver hook, low kick, roundhouse, push kick, knee, elbow, hammer fist, the coil and the power right, guard up/hold/hit, parry, duck, step out, step in, hop, the counters (trap and elbow, liver shot, counter right), the reactions (jabbed, rocked, uppercut, folded, leg buckled, shoved, launched, elbowed down, parried, overreaching) and the boxer's step forward and back.
  - Contact sheets: `docs/previews/v3/<clip>.png`, all on one sheet in `docs/previews/v3_moves.png` (`--preview`).
- **The touch grammar** (`GestureInputV3.cs`): where the finger starts (on a man, on the hero, on the ground) and how it moves (tap, hold, swipe, pull and release). Every touch is also read the classic way, so the prologue's directed duel still works.
- **The hero** (`HeroV3.cs`):
  - strikes by the height touched, taps in rhythm making strings;
  - swipes for the big shots; the pull and release scrubs the coil live with the finger;
  - hold on yourself to guard; swipe from yourself to duck, hop, step out or step in.
  - **Perfect defence** (the last 0.2 s) slows time and opens a 0.6 s **counter** window.
  - The clinch, rope, bar and air states read the new touches as their old gestures.
- **The enemies** (`EnemyV3.cs`): the wind-up is the blow's own clip played slowly to its loaded pose, flashing **white** (block it) or **red** (evade it); `V3Defend` judges every blow as it lands. Hand-keyed clips carry their reach, so fighters stand at the distance the blow was keyed for.
- **Feel:** `LimbStretch` (the striking limb stretches through the impact, in real time, so it shows in the freeze); `Haptics` patterns (block thud, parry tick, counter double knock, heavy jolt, hurt buzz; Android amplitude control).
- **Shading option C:** `ProjectInk/InkDepth` draws the far limbs a charcoal by depth behind the hips, in fights only; it fades to pure ink in cutscenes (`CombatV3.Shade`).
- **Teaching:** a card at the first fight, GUARD and RED · EVADE calls at the first telegraphs, and a MOVES page for the new grammar.

### Combat 3.0's touch grammar: dropped, the body kept
Playing it on the phone: touching the man to hit him puts your thumb over the man, so you can't see his wind-up, the flash or his reaction. The controls go back to classic everywhere. Chapter 1 keeps:
- the V3 body and its hand-keyed clips, which the classic strings now play;
- each clip's keyed reach;
- the impact stretch (`LimbStretch`);
- the far-limb shade in fights (`ProjectInk/InkDepth`).

Removed:
- `GestureInputV3`, `HeroV3`, `EnemyV3`;
- the COMBAT NEW / CLASSIC button;
- the V3 hint cards and MOVES page;
- the new haptic patterns.

The classic controls are the base for the next improvements.

## The V4 body: 29 bones, chapter 1's moves re-keyed, the clinch rebuilt

- **Skeleton** (`v4_rig.py`, `docs/previews/skeleton_lab.png`):
  - New bones: spine in three, neck, collarbones, wrists and fingers (mitten hands with a thumb), feet and toes. Every old bone keeps its name.
  - Every old clip is carried over by `build_stickman_v4.py`, which fills in the new bones on every frame: a curved spine, shoulders that shrug and reach with the arms, feet that roll heel to ball, fists in fights and relaxed hands in the story.
- **Pose engine** (`v4_pose.py`):
  - What a pose can set: torso twist per segment, collarbone protraction and shrug, the fist turning over, finger curl, planted feet given by the ball of the foot with the heel pitch, feet in the air, and arm and leg IK.
  - Detail layers baked on every frame:
    - real steps: heel peel, swing, heel strike, roll flat;
    - head, neck and off-arm overlap on damped springs;
    - an impact recoil, with the fist squeezing and the shoulder popping;
    - breathing on loops;
    - a floor guard for kicks.
- **Chapter 1's moves** (`v4_clips.py`): 33 clips keyed through the whole chain of events. They are the guard, every punch and kick, the knee, elbow and hammer fist, parry, slip, block, the enemies' wind-up, and every reaction. `v4_lab.py --report` checks each blow lands at its reach and every foot stays on the floor.
- **The clinch** (`v4_clinch.py` + `HeroClinchV4.cs`): 24 moves, each keyed as a pair, the hero and the held man together.
  - New: knee to the thigh, the plum, the headbutt, a sweep, o-soto-gari, the guillotine (hold high), the body lock (hold low, then swipe where he lands), and a belly-to-belly suplex. The hip and shoulder throws now correctly land him behind you.
  - Combos: plum, dead leg, knee + headbutt, bent-over DDT, knees to suplex, knee into guillotine, off balance, shove + push kick. Inputs made in the second half of a knee are buffered.
  - `GestureInput` now reports a swipe's height and a held press's height.
  - The old clinch stays for chapters 2–6, which keep the old body.
- Contact sheets: `docs/previews/v4_moves.png` (every move), `docs/previews/v4_clinch.png`.

## Moving on his own (chapter 1, the V4 body)

- **Gaits** (`v4_moves.py`): walk, jog and run cycles plus a sneak, generated from gait physics.
  - Each foot's stance slides back at exactly the body's speed; the landing is heel first (walking) or midfoot (running), rolling flat and peeling up to push off.
  - The swing arcs forward.
  - The hips bob on the gait's rhythm, the pelvis turns against the chest, and the arms swing against the legs.
- **Hand-keyed moves:**
  - stopping from a run (the heel plants, he leans back, takes a braking step and settles);
  - the running turn (plant, pivot, push off);
  - the running jump;
  - vaulting a drum (both hands stay planted on the lid while he goes over);
  - picking up (a squat, the hand closes);
  - pushing a drum over;
  - a relaxed idle when alone (weight shifting, looking around) and a crouched idle.
- **Controls** (`HeroMovesV4.cs`, `GestureInput`):
  - Holding builds walk → jog → run (near men, no faster than a jog). Letting go from speed plays the stop; holding the other way while running plays the turn.
  - Swipe down and hold: crouch; slide that finger to creep (no footsteps). Keyboard: hold C.
  - Swipe up at a drum: vault it. While running: the running jump. Up-left / up-right: jump that way (keyboard: W with A/D).
  - One swipe down throws the weapon in hand (bare hands: duck); two drop through the floor (keyboard: S twice).
  - A tap with nobody within 6 m takes a step instead of punching the air. A tap on a weapon or a drum walks to it and uses it, and walking over a weapon bends down to pick it up.
  - Relaxed when alone, the guard when a man comes within 6 m.

### Hanging from an edge (chapter 1, V4 body)
Walking (not sprinting) off the forklift, a rack or the mezzanine, he lowers himself down by the hands and hangs (`v4_hang.py`, `HeroHangV4.cs`). While hanging:
- **Swipe down** (or do nothing for about 1.6 s): let go. The knees soak a short drop; from high, he lands on a knee and a hand.
- **Swipe up:** pull back up (a dip, the pull, chest to the edge, a knee on top, stand).
- **Hold left or right:** swing the legs, and let go on the outward swing to land further out. This replaced the planned shimmy: in the side view the edge is a single point, so there's nowhere to shimmy along.
- **Tap a man below:** drop onto him feet first; he goes down.
- **Tap a man at the edge above:** grab his ankle and pull him off past you.
- **Hit while hanging:** torn off the edge.

Every fall of a metre or more on the new body now lands with the soft or the heavy landing.

## When the men are far (chapter 1's V4 body; props in every chapter)

**Detail on every action** (`tools/blender/v4_pose.py`, `build_stickman_v4.py`):
- **Torso wave:** the waist and chest twist trail the hips by a beat.
- **Follow-through:** the off hand's wrist and fingers spring on after the arm stops.
- **Shoulders:** a raised arm lifts its shoulder.
- **Impacts:** the knees give a little at every hit.
- **Loops:** a small drift in the head and forearms, so he never holds perfectly still.
- **Carried-over clips:** the old clips get the same lag on the neck, head, wrists and fingers.

**New clips** (`tools/blender/v4_far.py`): advance, rush_in, flying_knee, slide_tackle, shoulder_barge, throw_object, sidestep, swat, chain_lash, chain_drag (+_v), takedown_choke (+_v), unaware. Contact sheets are in docs/previews/v4/.

**Controls** (`HeroFarV4.cs`):

| Input | What he does |
|---|---|
| Tap a man 2.2–6.5 m off | Closes in behind his guard (a boxer's step-drag), then strikes. |
| Swipe towards a man 3–8 m off | The rush: step-drag bursts until he's in front of him. |
| Running, tap | Any tap counts as forward, with height picking high or low. A tap within 0.35 s of letting go cuts the stop short. High = the flying knee (the clothesline when closer), low = the baseball slide. |
| Sprinting into a man | The shoulder barge. Jogging into him still takes hold. |
| Swipe as a charge comes | The sidestep, with a hammer on his head as he passes. |
| Crouched, tap a man's back | The silent choke. Others don't hear it. |
| A prop in hand, tap a man | Throws it at him, wherever he is on screen. Within arm's reach he smashes it on him instead. |
| Something thrown at you, tap | The swat. |
| Chain in hand, tap | The lash, out to 2.6 m. |
| Chain in hand, hold on a man | Hauls him in, into the clinch. |

**Stealth** (`EnemyFar.cs`):
- A man on watch smokes and looks about.
- He sees you in front within 7 m; crouched, you can get within 4 m.
- He hears you standing behind him within 1.6 m, running within 5 m, or a fight within 9 m.
- Chapter 1's mezzanine starts with two men on watch, backs to the stair. The waves wait until they know.

**Props by chapter** (`Props/Throwable.cs`): chosen from the place and the story.

| Chapter | Props |
|---|---|
| Warehouse | The night crew's bottles, the forklift wrench, the crane's hoist chain. |
| Street | Café and market bottles, bricks loose from the railway arches. |
| Home | The Cleaner's bleach bottle (it blinds), a wine bottle. Nothing of Elena's or Maya's. |
| Backyard | Flower pots, pond stones. |
| Fortress | Battlement rubble, the guards' torches (in the blackout the beam spins and blinds). |

The men pick props up and throw them at you too, so the swat has a job.

## Near range: reading the man, and CROSSFIRE (chapter 1's V4 body)

**Distance decides what a tap throws** (`HeroNearV4.cs`):

| How close | Tap high | Tap low |
|---|---|---|
| Inside 0.9 m | elbows, hooks, the uppercut | to the body: liver hook, body cross |
| Further out | jab, cross | kicks |

**Strings:**
- Boxer's Four (now ends on the uppercut)
- Dutch Two-and-Low
- One-Two-Liver
- Up-Down-Up
- Liver and Lights
- Dig and Lift
- Close Quarters
- Kick String (calf, body, head)
- Karate
- Off-book taps throw the natural next shot instead of a dead end.

**Read the man** (`EnemyRead.cs`): every opening is a pose held long enough to read, and it is spelled out over him (`OpeningCue`).

| What his body shows | Answer |
|---|---|
| High shell (`cover_high`) | tap low: UNDER THE GUARD |
| Low guard (`cover_low`) | tap high: OVER THE TOP |
| Folded (`fold`) | tap high: KNEE TO THE FACE |
| Head back (`head_back`) | tap low: HE FORGOT HIS BODY |
| Stumbling | tap low: SENT OVER |
| His punch missed after your slip (`overreach`) | tap high: COUNTER (overhand) |
| Leg gone after two calf kicks (`leg_buckle`) | tap high: TIMBER (head kick) |
| Back to the wall | everything lands 25% harder; a finisher slams him down it (`wall_slump`) |

Men waiting their turn now hold a visible high or low guard, changed every few seconds. It blocks that level and leaves the other open.

**CROSSFIRE:** one man close in front and one close behind, and a tap does this:
1. He drops under their swings, and their punches land on each other.
2. He comes up between them: a palm in front, an elbow behind.
3. A push kick sends the front man flying.
4. A back kick sends the other.

It runs as a three-body paired clip with slow motion, camera focus, shakes and the banner. It recharges in 16 s.

## Backlog step 1: the new body in every story chapter but the fortress
- **What now builds on the 29-bone body:** the bank and the street, home, the backyard, the road and the
  epilogue, as chapter 1 already did. Enemies, Maya and Voss get it too, since every chapter builds its
  figures from one model.
- **Not the fortress:** it is rebuilt on the new body from scratch (step 7).
- **Not the tower and the classic hall:** they are fight mode.
- **Every old clip carries over:** all 130 of the classic clips are in the V4 set (checked by name).
- **Props still attach:** props on the bones (hand.R, hand.L, head, forearm) keep their bone names.
- **Build check:** `BuildScenes.CheckCombatV3` now fails the build if any of those scenes isn't on the new body.
- **What comes with it in each chapter:**
  - the walk, jog and run speeds, the relaxed idle
  - the near and far moves, the clinch, the hanging moves (where the level has raised floors)
  - the chapter's props
- **To check in Unity (Build All Scenes):** every cutscene's poses, props in the hands and on the heads
  (Maya's ribbon, the Cleaner's apron, Voss's cane), the Driver's car, the Home chair scene, the backyard swing.

## The katana (backlog steps 2–6)

**Step 2, the controls change with the weapon** (`GestureInput`):
- `Grammar` is Fists or Katana, set by the hero every frame.
- With the katana, every swipe is `Command.Cut` along its own line (`CutStart`, `CutEnd`, `CutVector`, `CutSpeed`).
- A press on the hero himself held for 0.22 s is `IaiHold`. Dragging out and letting go is `IaiDraw`; letting go without a drag is `IaiCancel`.
- The crouch swipe is off while the katana is out.
- Keys for testing: U ↘, I ↓, O ↗, J →, K ↑, L retreat, hold SPACE for the draw.

**Step 3, the ink stroke and the red line** (`Core/InkStroke.cs`):
- The swipe is drawn into the fighting plane as one tapered, bowed brushstroke, pale so it reads on the dark sets.
- A killing cut's stroke lingers; a kata holds all its strokes together.
- `RedLine` is a swordsman's coming cut, through the hero, firming up as the wind-up ends. A thrust shows as a red ring.

**Step 4, the clips** (`tools/blender/v4_katana.py`, 24 clips):
- **The hold:** both hands on the hilt; the blade lies along the right hand, so a pose sets the blade by its angle.
- **The hero's clips:**
  - katana_guard (chūdan), katana_walk (suri-ashi), katana_retreat
  - the cuts: kesa, gyaku, oroshi, kiriage, yoko, ushiro; the thrusts: tsuki, tsuki_low
  - three parries
  - chiburi, iai_sheathe (nōtō), iai_hold, iai_draw (nukitsuke), katana_pass
  - katana_discard
- **The swordsman's clips:** the two wind-up tells, k_parried, cut_down.
- **The lab:** `v4_lab` now draws the blade on the hero.

**Step 5, katana play** (`HeroKatanaV4.cs`):

| Gesture | What it does |
|---|---|
| A swipe's angle, relative to the man faced | the cut: ↘ kesa, ↗ gyaku, ↓ oroshi, ↑ kiriage, → yoko |
| ← away | the retreat; with a man behind, the turning cut |
| A tap | the thrust |
| A man out of reach | the cut becomes a stepping cut that slides in |
| The same line as a swordsman's red line | the mirror parry: within 20° PERFECT (slow motion, zoom), within 45° a parry; then a free KAESHI. A thrust is answered by a rising swipe. |
| Line sequences | the kata: TSUBAME-GAESHI, JŪJI, KATA OF THREE |
| The iai draw | NUKITSUKE |
| Two or three men in a line along the drag | THE PASS: frozen, he goes through them, flick, sheathe; on the click they fall in order |

- **A killing cut:** he stands a beat, then falls on his face (`cut_down`).
- **A katana doesn't wear out.**

**Voss's lessons:** remembered lines (`VOSS · A MEMORY`), each the first time it's needed.
- *"The line comes before the blade."*
- *"Point first. The edge is for after."*
- *"Don't meet his sword. Meet his line."*
- *"Draw only once. Make it enough."*

**Step 6, swordsmen who fence** (`EnemyKatana.cs`):
- **His tell:** up to jōdan or down to waki-gamae, with the red line, for about 0.6 s.
- **His attacks:** cuts, and the thrust from further off.
- **Feints:** 18% of the time.
- **Reading you:** the same line three times running and he parries it.
- **Parried,** he's knocked wide and open.
- **He drops his katana** when he falls.

**Test checkpoint:** chapter 1, TEST · the katana (the crane bay: a katana on the floor, every wave swordsmen).

## Chapter 6 rebuilt: Kessel Hill v3 (backlog steps 7–11)

**One layout, one source of truth** (`tools/blender/kessel_plan.py`):
- It holds the floors, stairs, ledges, levels and set-piece marks.
- It checks itself (stairs meet their floors, no overlapping floors, every mark stands on a floor).
- It generates `Assets/Scripts/Level/KesselPlan.cs`, which `LevelNav` uses for the fortress.

**The set** (`tools/blender/build_kessel_v3.py` → `Assets/Art/FortressV3/KesselSet.fbx`):
- **Built on the plan:** eleven spaces up to 33 m, baked twice (night and lightning) like every story set.
- **The rule:** only floors and steps are in the lane. Walls, pillars, statues and furniture stand behind it, or low in front. Interiors have no front wall.
- **The moving parts:** the truck, the two portcullises, the chandeliers and the telescope are separate parts the game moves.

**In Unity** (`Editor/KesselBuilder.cs`):
- The set goes on the LightningSwap.
- The floors and stairs become solid colliders from the plan.
- **A self-test:** every floor, probed every 0.5 m, must be solid where the plan says, or the build fails.
- **Each level's acoustic** is an AudioReverbZone: open air, the passage's stone corridor, the tower's cave, the gallery's room, the hall's auditorium, the dome's hangar.
- **The fortress is on the V4 body now.**

**The ascent** (`Level/StoryKessel.cs`): the old opening is kept; then the levels.

| # | Level | What happens |
|---|---|---|
| 1 | The switchback road | Two on watch by the truck: the stealth start, or tap the cab and the handbrake rolls the truck through them. Men on the upper road throw stones down. |
| 2 | The great stair | The crane-back reveal. Barrels down the flights: jump them, or shelter by a lion. |
| 3 | The barbican | Stones through the murder holes, pale circles first. Then hold to climb the drawbridge chain. |
| 4 | The gate passage | Down through the murder hole. Both portcullises slam: the killing ground. Mash the windlass to get out. |
| 5 | The wall walk | Gusts of wind push everyone. The first swordsman: take his katana; the drums come in. |
| 6 | The wall tower | The passage down: "Take the high step. Always." One ambush. |
| 7 | The courtyard | Two on watch. Cut the bell rope, or they wake the bailey. The hay hoist. The blackout, then swordsmen in the dark (the pass). |
| 8 | Voss's gallery | Paper screens men are shadows behind; they tear under a cut. The photograph: the Kata of Three remembered, its three lines drawn in ink before him. |
| 9 | The great hall | THE WARDEN, his three phases kept. His chain takes your katana: fists, TIMBER. He drops his chain. |
| 10 | The tower stair | (After the tower cutscene.) Two swordsmen. The broken stair: tap in a lightning flash to jump it, or catch the edge and pull up. The walkie. |
| 11 | The observatory | Two of Voss's best (masters: faster lines, more feints). The telescope sweeps them. Hold the crank. |

Then the roof, as built.

**Checkpoints:** `FortressFrom = 20 + level`.

**The duel** (`StoryFinale`, `EnemyKatana`), step 10:
- **Phase I:** Voss fences with the cane's hidden blade: red lines, feints, two taps of the cane before a thrust.
- **At 75%, the Kata of Three:** three red lines on a slow clock. Mirror them all (PERFECT, "...Well taught.").
- **Then the hero throws the katana aside:** "...What are you doing?" / "Not yours."
- **The bare-handed close:** a wrist, a twist, and the cane's blade rings off the stones.
- **Then the gun and the edge,** as built.

**Sound** (step 11):
- The wind rises with height.
- The thunder comes sooner after each flash as he climbs.
- Each room has its own reverb.
- The drums (the duel cue) arrive with the first katana.

## Backlog steps 12 and 15
- **Step 12, each chapter's named combo** (`HeroController.ChapterCombo`):
  - It is taught once, by a banner at the chapter's first fight.
  - It lands 50% harder on that chapter's boss, marked ★.

  | Chapter | Combo |
  |---|---|
  | Warehouse | BOXER'S FOUR |
  | Street | DUTCH TWO-AND-LOW (the Driver is a boxer: chop his legs) |
  | Home | CLOSE QUARTERS (the narrow rooms) |
  | Backyard | COUNTER (make the Handler miss, then the overhand) |
  | Fortress | TIMBER (the Warden) |

- **Step 15, tuning that needs no phone:**
  - The read words fade to a mark after three uses of that read.
  - PINNED counts an oil drum behind him.
  - A prop in hand is put down when he walks onto a weapon.
- **Parked:** step 13 (knife and rod controls) and step 14 (the book / evidence story, which needs your brief).
