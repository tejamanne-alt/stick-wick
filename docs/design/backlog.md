# Backlog: in build order

Each step is built on the ones before it, so nothing is redone. Every step ends with:
- its test checkpoint
- the contact sheets for its clips
- a compile check, a commit and a push
- a note in the DEVLOG

| # | Step | What it delivers | Depends on |
|---|---|---|---|
| 1 | **The V4 body in chapters 2, 3, 4, the walk-out and the epilogue** | One switch, then each chapter checked: cutscene clips, props on the bones, bosses, Maya and Voss. (The fortress is not converted: it is rebuilt in step 7.) | – |
| 2 | **Weapon control grammar** | A weapon in hand swaps what swipes mean (fists keep today's controls). Swipe angle in 8 directions relative to the man faced. A press on the hero, then drag and release. The swipes the katana replaces are dropped while it's in hand. | – |
| 3 | **Ink stroke and red line** | The swipe drawn as a sumi-e brushstroke that the blade follows: a killing stroke stays a beat, a kata's strokes show together. The enemy's red ghost line for his coming cut, a red point for a thrust. | 2 |
| 4 | **Katana animations (about 22)** | Hero: the guard and footwork, the five cuts, the turning cut, the thrust, the retreat, the mirror parries, the return cut, the iai draw, the pass, cleaning and sheathing, tsubame-gaeshi, jūji, the Kata of Three, picking it up, throwing it aside. Swordsman: his cuts and thrust with tells, parried open, cut, the fall after the pass, disarmed. | 1 |
| 5 | **Katana play, at a chapter 1 test checkpoint** | Cuts by swipe line, the thrust by tap, the stepping cut. The mirror parry (20° PERFECT / 45° parry / a wrong line trades blows) and the free return cut after it. The draw and the pass. Kata from line sequences. Men without blades fear the sword. | 2, 3, 4 |
| 6 | **Swordsmen who fence** | Red lines with tells, feints, they read a repeated line, lunge, bind. A beaten swordsman drops his katana. Voss's lessons as memory lines, each the first time it's needed. | 5 |
| 7 | **Chapter 6 rebuilt: the set and the levels** (docs/story/chapter6_fortress_v3.md) | The Blender set and real collision with a self-test that fails the build on overlaps. The twelve spaces: 1 the switchback road · 2 the great stair · 3 the barbican · 4 the gate passage · 5 the wall walk · 6 the wall tower passage · 7 the courtyard · 8 Voss's gallery · 9 the great hall · 10 the tower stair · 11 the observatory · 12 the roof. The enemies, the stealth openings and the alarm bell. | 1, 6 |
| 8 | **The traversal set pieces** | The new clips and their gestures: climbing the retaining wall, the truck's handbrake, barrels on the stair (jump, cover), tumbling down stairs, the drawbridge chain, the windlass, riding the banner, sliding on the slates, the bell rope, the paper screens, the broken stair in the lightning, the telescope, the dome crank. | 7 |
| 9 | **The Warden, rebuilt** | The chain wraps the katana; the chandeliers; TIMBER (calf, calf, head kick); he drops his chain. | 7 |
| 10 | **The Voss duel, rebuilt** | His shikomizue: faster red lines, feints, two taps of the cane before a thrust. The Kata of Three, mirrored. The katana thrown aside, bare hands, the cane blade taken. Then the gun and the edge (as built). | 6, 7 |
| 11 | **The fortress sound** | The storm with height (the thunder getting closer). Each room's acoustic. Surfaces underfoot. The katana's sounds. The score stems by act: storm, then drums, then the choir, then silence for Voss. | 7–10 |
| 12 | **Each chapter's named combo** | 2 the street: Dutch Two-and-Low against the Driver · 3 home: Close Quarters · 4 the backyard: slip and overhand against the Handler's knuckles. | 1 |
| 13 | **Knife and rod controls, new enemy types** (parked) | Their gesture maps on the step 2 grammar. | 2 |
| 14 | **The book / evidence story logic** (parked) | Needs a brief from you. | – |
| 15 | **Tuning after phone testing** | Fade the read words once learned. Crates and racks count for PINNED. A prop in hand vs a weapon pickup. The advance speed. The distances at which guards on watch notice you. How long the readable poses are held. Previews that draw the second man in three-body clips. | testing |
