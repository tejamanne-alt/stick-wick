# Chapter 1: The Hitman. The warehouse fight (design for review)

**Status:** built (first pass, not yet played in Unity).
- The hall: `tools/blender/story_hall.py`.
- The fight: `StoryHall.cs`.
- The boss: `StoryForeman.cs`.
- The ending: `StoryEnding.cs`.

The kit:
- **Oil drums:** `OilDrum.cs`. Kick one and it rolls, flattening everyone in its path. The Foreman's charge stuns him on one, and his hook sends them rolling.
- **Falling boxes:** `RackSpill.cs`. Fling a man through a rack and its stock rains down on the bay.
- **Office glass:** `OfficeWindow.cs`. Knock a man down hard on the mezzanine in front of the office and he goes back through the window, out of the fight.

It continues the prologue: wake-up → puddle → where am I → break-in → escape → knife → INSTINCT (the directed first fight) → *"YOU'VE DONE THIS BEFORE."*

Then, instead of the end card:

> **He lifts the shutter they came through. Behind it is the rest of the warehouse, and everyone in it is waiting for him.**

---

## 1. The idea in one line

The room he woke in is only the **back store** of a huge derelict warehouse. Through the shutter is **the main hall**: four zones of real fighting along its length, each built around things he can use, with waves that get harder zone by zone. At the far end is the **boss**, the foreman of this crew. The chapter ends not in triumph but in an **accident**: the one man who could have told him who he is dies by his own gun, in the hero's hands.

**Tone:** the fights are kinetic and fun; the ending is quiet, tragic and unresolved. It's the first real hook for the five-chapter story.

---

## 2. The building (built properly this time)

A **complete 3D warehouse**, enclosed on all four sides, at real scale, so every cinematic angle holds up. Built in Blender with baked night and storm lighting, like the current set.

| | |
|---|---|
| **Size** | Main hall about **48 m long × 22 m deep × 12 m to the roof ridge**. The back store (the existing room, rebuilt to match) sits behind the inner shutter. |
| **Shell** | Old brick and concrete walls, tall steel-framed windows (some panes broken, some boarded up), a pitched roof on steel trusses with skylights (one torn, letting the rain through), and a grid of steel columns. |
| **Outside** | A brick facade with faded company lettering, loading docks with big roll-up doors, a fence, floodlights, the yard and containers. The opening shot becomes a **big derelict warehouse**, not a low shed. |
| **The fourth wall** | Every wall is real and appears in every cinematic. **During gameplay** the front wall between the camera and the fight lane fades out (a common cutaway technique) and returns for cutscenes. *(Recommended. The alternative, a camera inside the building, makes the fight space cramped.)* |
| **Look** | The current baked, semi-stylised look, but with far more detail and clutter: graffiti, oil stains, puddles, cables, debris, hanging chains, light fittings. |

### The four zones (left to right along the fight lane)

The fight is side-on, as in fight mode, with platforms at three heights. Each zone is a self-contained arena with its own **kit**: the things the hero can use.

**Zone 1: THE LOADING BAY** (entering through the shutter)
- Pallets and **crates** to kick into enemies, roll cages, and a **forklift** with its forks raised, which is a ledge to climb onto.
- Truck bay doors, rain blowing in, **puddles** that splash when someone hits the ground.
- *It teaches the basics again, fast: crate kicks, edge throws off the forklift.*

**Zone 2: THE RACKING AISLE**
- Tall **pallet racks**, two climbable levels (ledges).
- A **chain hoist** hanging from the gantry: a rope swing across the aisle, kicking enemies as he passes.
- **New:** slam a man into a rack and boxes rain down on whoever is beneath it.

**Zone 3: THE MEZZANINE OFFICE**
- A steel mezzanine along the back wall, with stairs and a railing for **edge throws** down to the floor.
- The site office is up there behind **glass** for throws through the window.
- A **swing bar** (a pipe across the ceiling) for giant circles into a flying kick.

**Zone 4: THE CRANE BAY** (the boss arena)
- An open floor under the **overhead gantry crane**, with its hook and chain hanging.
- **Oil drums** (new: kick one and it rolls, knocking people down), the big main door to the yard at the far end, and rain through the torn skylight.

**Checkpoints:** at the start of each zone. A death replays only that zone.

---

## 3. The enemies

**Only bare hands and rods**, as you asked. No knives or guns among the crew; the only gun is the boss's, and it only appears in the ending.

| Type | Weapon | Role | New? |
|---|---|---|---|
| **Brawler** | Fists | The backbone: circles, waits his turn, combos. | existing |
| **Piper** | Steel rod / pipe | Long reach, heavy hits. Dodge the swing, then punish. Drops his rod when he falls, so the hero can pick it up and use it. | existing |
| **Runner** | Fists | Light and fast: darts in, one jab, darts out. Goes down in two hits. Keeps the tempo up and **lowers** the pressure (easy kills). | **new** |
| **Grabber** | Bare hands | Big and slow. If he gets close he **grabs the hero in a bear hug** and holds him for the others. Tap rapidly to break free. Teaches: don't let him close, kick him away. | **new** |
| **Door-man** | A sheet of corrugated iron as a shield, plus fists | Blocks strikes from the front. **Kick low, slide under, or go over him** (jump or swing); a crate kick or a thrown rod breaks his guard. | **new** |

### Waves (difficulty rises zone by zone; never more than 5 on screen)

| Zone | Wave | Enemies | What it asks of you |
|---|---|---|---|
| 1 Loading bay | 1 | 3 Brawlers (2 at a time) | Find your feet again. |
| | 2 | 2 Brawlers + 2 Runners | Pick targets; the runners are easy points. |
| 2 Racking aisle | 3 | 2 **Pipers** + 2 Brawlers | Rods appear: dodge, then punish; take a rod. |
| | 4 | **Grabber** + Piper + 2 Runners | Keep the grabber off you; use the rope swing. |
| 3 Mezzanine | 5 | **Door-man** + 2 Pipers + Runner | Break the shield: kick low, slide, or throw. |
| | 6 | 2 Brawlers + Grabber + Door-man, then 2 Pipers drop from the mezzanine | Everything at once; use the glass and the edges. |
| 4 Crane bay | 7 | The last of the crew (4 mixed) | They hold the floor while the boss arrives. |
| | **BOSS** | **The Foreman** | See below. |

---

## 4. The boss: THE FOREMAN

The crew's boss, built on the Brute boss system, with **a crowbar, then bare hands**. He has 1.4× the Brute's health and three phases, marked on his bar at 66% and 33%.

- **Arrival:** wave 7 thins out, the gantry crane groans into life, and he rides its hook down onto the floor. Name card: **THE FOREMAN.**

| Phase | Health | How he fights |
|---|---|---|
| **I · The crowbar** | 100–66% | **Guard up:** plain punches from the front bounce off while he is not attacking. Kick, hit him from behind, or dodge his swing and punish him. **Three-swing crowbar combo:** the third swing (red) floors you; dodge it and he is open for over a second. Also the bull charge, the ground slam and the bear hug. |
| **II · The crane** | at 66% | He breaks off and runs for the crane's hanging control box at the far end, and he can't be hurt there. **Two of his men** come in. The **hook swings every 3–5 s**, and the crane **drops loads of crates** where you stand: a red ring, a rattle of chain, then the crash. Reach him and he lets go and fights again, guard up (or after about 28 s on his own). |
| **III · Bare hands** | at 33% | He flings the crowbar away, enraged: no guard, **hits 20% harder**. Headbutt, bear hug, two-fist smash, double slams. **Winded:** after a slam he stands bent over for nearly 2 s, the opening. Hurt below 40%, he **limps** and you hear him breathing hard. |

- **Through every phase he reads you.** The same kind of strike (punch or kick) five times running gets caught and thrown back at you: *"HE READ YOU: MIX IT UP."*
- **The environment:** a charge into a standing drum stuns him; the hook sends drums rolling; crane loads crush his men too.
- **The last hit doesn't kill him.** He crashes down under the torn skylight, beaten but alive, and the drama begins.

---

## 5. The ending: a directed scene (no player input)

**Setting:** the crane bay, under the torn skylight, where the yard floodlight pours through the rain. From the kneel to the shot, everything is **side on, in silhouette**: two black shapes against a lit curtain of falling rain. Stick figures read best that way, so this replaces the earlier close-ups.

| # | Shot | What happens | Sound |
|---|---|---|---|
| 1 | Slow motion, wide | The final blow. He crashes down under the skylight. | The hit, then just rain. |
| 2 | Low and wide | The hero walks to him and drops to one knee, grabbing his collar. | Footsteps in water, breath. |
| 3 | **Silhouette**, side on | **YOU:** *"Why are you trying to kill me?"* **FOREMAN** (a weak laugh): *"You... really don't remember."* | Heartbeat. |
| 4 | Silhouette, closer | **YOU:** *"Tell me who I am. WHO SENT YOU?"* **FOREMAN:** *"Nobody had to send us. Five million reasons, friend."* | Tinnitus, faint. |
| 5a | Insert, very close on his hand | His hand slides under his jacket; the pistol comes out, low, where only we see it. | Cloth dragged, the grip scraping free. A slow heartbeat starts. |
| 5b | Close on the hero's face | He hasn't seen it. | A breath. |
| 5c | Silhouette, slow motion | The pistol comes up out of the dark into the hero's face, a black shape against the light. | Heartbeat faster. |
| 5d | Extreme close: the gun in profile against the light | The hammer goes back. **FOREMAN:** *"Nothing personal."* | The hammer: a click, then the heavy clack. |
| 5e | His eyes | The world closes in to the barrel. | Rain and room drop away, muffled; the heart races. |
| 6a | Whip to the two-shot | Instinct: he grabs the wrist. | A slap of hand on wrist, cloth, a grunt. |
| 6b | Fast cuts every half second | The two of them, the gun shaking between them, the Foreman's bared teeth, the hero's face. The barrel swings away, comes back at the hero, then turns, inch by inch. | Clothes wrenched, the gun's metal knocking, breath through teeth, grunts, the heart at full race. |
| 6c | Close on the gun, extreme slow motion | The barrel is on the Foreman's chest. | **The heart stops. Everything goes silent** but one breath. |
| 7 | **BANG** | A white frame, the backlight flares, the muzzle flash; smoke curls up out of the barrel through the light. | An indoor pistol shot: the crack, the thump in the chest, the hall answering with slap-back echoes and a long tail. Then a high ring, the world muffled. Through it, the brass casing bouncing on the concrete. |
| 8 | Silhouette, slow push in | His last breath out. The gun slips from his fingers into the water. The ink spreads out from under him into the rain water. | The death breath; the pistol hitting the puddle; the rain slowly coming back. |
| 9 | Push in | The hero backs away, staring at his hands, shaking his head: *"No... no... no..."* *"That wasn't... I didn't..."* | Breath. |
| 10 | Low, on the floor | **A phone buzzes** in the water beside the dead man, its screen lighting the wet concrete. | Vibration on concrete. |
| 11 | He picks it up: **the screen** | **CONTRACT UPDATE**, a grainy surveillance still of his own face (the wound on his temple): **THE FERRYMAN · $5,000,000 · OPEN · CONFIRM KILL.** UNKNOWN, incoming call. | |
| 12 | The call | **VOICE ON THE PHONE:** *"Is it done?"* Silence; he breathes. *"...Ferryman."* *"You were supposed to stay dead."* **YOU:** *"Who is this?"* **VOICE ON THE PHONE:** *"The contract is open. Every gun in this city is coming for you."* CALL ENDED. | A click; the line. |
| 13 | Wide, high | He lets the phone fall. Very small in the huge empty hall. | Rain. |
| 14 | From behind, wide | He walks out through the big door into the rain and the floodlights; the helicopter's searchlight crosses the yard. | Thunder, rotor. |
| 15 | Black | **CHAPTER 1 COMPLETE · THE HITMAN.** Memory restored: *"Someone put you in the ground. You climbed out."* | |

**What the player learns**
- **The wound:** it wasn't an accident. Someone tried to kill him; the warehouse was meant to be his grave.
- **What he was:** a man worth $5M, known by a codename, THE FERRYMAN.
- **What drives the next chapters:** the contract is open, so every chapter brings new hunters.
- **The man on the phone:** a voice only.

Nothing about his wife or his daughter is revealed here; that is for chapters 2–4.

---

## 6. What gets built (in order)

1. **The warehouse:** the full 3D model in Blender (shell, exterior, the four zones and their kit, clutter), baked lighting. The back store and the opening yard shot are rebuilt to match.
2. **The fight lane and its kit:** platforms, ledges, the rope and chain swings, the swing bar, crates, glass, plus the new **falling rack boxes** and **rolling oil drums**. The front wall fades during gameplay. Checkpoints per zone.
3. **The new enemies:** Runner, Grabber, Door-man (behaviour, animations, how they read on screen).
4. **The waves:** the seven waves above, tuned for rising difficulty.
5. **The boss:** the Foreman's arrival on the crane hook, the crowbar combo, the hook swing, phase 2.
6. **The ending:** new animations (kneeling over him, the gun struggle, the boss's death, the hero's "no... no...", walking out), the pistol and phone props, the silhouette backlight, shot-by-shot staging, sound and subtitles.
7. **Story wiring:** the prologue flows into the fight (through the shutter), and the chapter-complete card unlocks fight mode for this level.

---

## 7. Decisions for you

1. **Front wall:** fade it out during gameplay *(recommended)*, or put the camera inside the building?
2. **Look:** the current baked, semi-stylised look, much more detailed *(recommended)*, or a more realistic, textured style (more art work, a bigger app)?
3. ~~The boss's lines and the photo~~ **Decided:** no photo, no hints about "his son" or "her". The reveal is the phone call (the open contract, THE FERRYMAN, the voice).
