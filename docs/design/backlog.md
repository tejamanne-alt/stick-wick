# Backlog: discussed, not yet built (in build order)

The order avoids rework: shared foundations first, then the features that sit on them, then
tuning. Every step adds its test checkpoint (docs/story/test_checkpoints.md).

## 1. The V4 body in every chapter
Most later features need it, so it comes first.
- One switch in PrototypeBuilder: any chapter can run on the V4 body, enemies included.
- Every classic clip is already carried onto V4 by build_stickman_v4.py. Each chapter still needs checking:
  - its cutscene clips
  - the props attached to bones
  - its bosses
  - Maya, Voss and the other characters
- Order: chapters 2, 3, 4, then the walk-out and the epilogue.
- **Not the fortress:** it is rebuilt on V4 from scratch (step 7), so the old one is not converted.

## 2. Weapon control grammar (shared by the katana, later the knife and the rod)
- **Per-weapon gesture map:** a weapon in hand swaps what swipes mean. Fists keep today's controls.
- **Swipe angle:** classified relative to the man faced, in 8 directions.
- **Press on the hero himself:** a hold there, then drag and release. Used for the iai draw.
- **Swipes the katana replaces while it is in hand:** dodge, jump, throw, and the double-swipe drop.

## 3. Ink renderer and red telegraph line
- **The ink brushstroke:** the finger's path drawn as a sumi-e stroke that the blade follows.
  - A killing cut's stroke stays a beat.
  - A kata's strokes show together, then wash away.
- **The enemy's red ghost line:** his coming cut, or a red point for a thrust.

## 4. Katana clips (V4, about 22)
- **Hero, guard and footwork:** guard and fencing footwork.
- **Hero, the cuts:** kesa-giri, gyaku-kesa, kiri-oroshi, kiri-age, yoko-giri, the turning cut behind, tsuki (high and low).
- **Hero, defence and recovery:** retreat, mirror parry (high, low, against a thrust), kaeshi return cut.
- **Hero, the draw:** iai sheathe-and-hold, nukitsuke draw, the pass (multi-body), chiburi and nōtō.
- **Hero, the kata:** tsubame-gaeshi, jūji, the Kata of Three.
- **Hero, katana handling:** picking a katana up from a downed man; throwing the katana aside (for the duel).
- **Swordsman, attacks:** guard, his cuts and thrust with tells.
- **Swordsman, reactions:** parried open, cut (no gore: ink), the fall after the pass, disarmed.

## 5. Katana gameplay (tested at a chapter 1 checkpoint first)
- **Cuts:** cuts by swipe line; tsuki by tap; a stepping cut when out of reach.
- **The mirror parry:** swipe his red line.
  - Within 20°: PERFECT, with slow motion.
  - Within 45°: a parry.
  - A wrong line: blows are traded.
- **After a parry:** the kaeshi window.
- **Thrusts:** answered with a rising cut or the retreat.
- **The iai draw:** hold on the hero, drag, release. The pass through 2-3 men in a line.
- **Kata:** recognised from line sequences.
- **Men without blades fear the katana:** they keep distance, try to grab the sword arm, some run.
- **The test checkpoint:** a chapter 1 checkpoint with two swordsmen. The katana comes from beating one.

## 6. Swordsmen AI
- They fence: lines with tells, feints, they read a repeated line, lunge, bind.
- **Drops on defeat:** a beaten swordsman drops his katana and the hero picks it up.
- **The first pickup:** a memory of Voss's voice gives the first lesson ("The line comes before the blade").
- **Later lessons:** they arrive the first time each new skill is needed:
  - the parry: "Don't meet his sword. Meet his line."
  - the draw: "Draw only once. Make it enough."

## 7. Chapter 6, the fortress, rebuilt from the road to Voss's fall
**The full design is in docs/story/chapter6_fortress_v3.md**: twelve levels in four acts, real collision with a
self-test, the traversal set pieces, per-room sound and the score stems. It takes in all of this step and step 8.
- **Enemies:** mostly swordsmen. The hero starts bare-handed and earns the katana from the first one he beats.
- **Voss's collection:** the katana wall in the great hall, Voss's collection (dressing and story).
- **The Warden:**
  - His chain wraps the blade. Drop the sword and fight bare-handed.
  - He drops his chain on defeat: chain against chain.
  - **TIMBER:** calf kicks until his leg buckles, then the head kick (the chapter's named combo).

## 8. The Voss duel, reworked
- **Phase I, his shikomizue (cane blade):**
  - faster red lines, and feints;
  - his thrust tell: two taps of the cane;
  - the Kata of Three as three red strokes. Mirror all three: three rings, slow motion.
- **Then:** the hero throws the katana aside and finishes bare-handed ("What am I now?").
- **Phases II (the gun) and III (the edge):** as built, on the V4 body.

## 9. Each chapter's named combo (needs step 1)
| Chapter | Named combo | Against |
|---|---|---|
| 2 · The Street | Dutch Two-and-Low | the Driver, a boxer |
| 3 · Home | Close Quarters | in the narrow rooms |
| 4 · The Backyard | the slip and overhand counter | the Handler's brass knuckles |

## 10. Knife and rod profiles, new enemy types (parked)
- The knife in reverse grip.
- The rod's sweeps.
- Their gesture maps on the step 2 grammar.
- New enemy types to suit them.

## 11. Story: the book / evidence logic (parked)
- Still needs a brief from you.

## 12. Tuning and polish (after phone testing)
- **The read words:** fade the words over the men after each read has been used a few times.
- **Pinned:** also counts crates, racks and real walls, not only the edge of the fight area.
- **A prop in hand:** blocks walking onto a weapon pickup (or swaps them).
- **Tuning:**
  - the advance speed (1.7 m/s)
  - the distances at which a guard on watch notices you
  - how long the readable poses are held
- **Previews:** v4_lab draws the second man in three-body clips (CROSSFIRE, the pass).
