# Story mode: Prologue (the Warehouse)

The story is told in five chapters. In each, the hero recovers one memory, through one big fight and a boss:
1. He is a hitman.
2. He was married.
3. His wife is dead.
4. He has a daughter.
5. He killed the antagonist's son.

The finale is a one-on-one with the antagonist, on equal terms. This document is the shooting script of the **prologue** as built (`Assets/Scripts/Level/StoryIntro.cs`, set built by `Assets/Scripts/Editor/StoryBuilder.cs`). Changes from the first draft are marked *(changed)*.

| # | Shot | What we see and hear | Time (s) |
|---|------|----------------------|----------|
| 0 | **The Desolate Yard**, extreme wide, high over the yard | A storm over stacked containers and crushed cars. The iron shed sits in the middle; far off, a city under an amber and purple haze. A slow 2% push-in. Rain on the lens. Lightning: two white frames and a blue afterglow light the roof seam. A thunderclap, with a phone buzz on the flash. Lower third: *THE YARDS · 3:12 AM* *(added: gives the place and the hour)*. | 0–4.2 |
| 1 | **The Roof Fracture**, straight up into the dark girders | Storm water gathers at a jagged crack into a single drop, which swells, stretches, lets go and falls at the lens. The storm drops to a muffled interior; one distant drip; a whoosh as it falls. | 4.2–6.8 |
| 2 | **The Spark**, his eyes (first person, on his back) | Heavy, sluggish blinks behind soft-edged lids. The beams are out of focus, with heavy vignetting. A high tinnitus whine fades; a faint heartbeat; far-off lightning. **Splat** between the eyes: a white splash, the lens jolts, the eyes shut and snap open in sharp focus. Phone tick. | 6.8–10.8 |
| 3 | **The Fractured Reflection**, his view down into a puddle *(changed)* | He has rolled onto his hands and knees, his arms planted either side, trembling. The water is dark and unreadable. One drop falls from the roof crack straight into the middle: a crown of beads and a little rebound column, in slow motion. **One ripple** spreads out from it, a real wave packet that bends the reflection and catches the light, dying as it travels. As the water calms his face surfaces, slowly: a black head against the glow of the crack above, **white eye slits**, the **jagged white fracture at the temple**. When it is perfectly still and clear, lightning strikes outside: the reflection blows stark white and the thunder rolls. A groan, ragged breath, wet palms; he pushes up. | 10.8–15.6 |
| 4 | **The Lost Identity**, side-profile medium close-up, Dutch angle | The camera tilts up with him as he rises to one knee, then stands. He brings his hands up to his face, turns them, closes them into fists, with a double heartbeat and his knuckles cracking. *(Changed: a stickman has no palms or knuckles to scar, so the fists and the sound carry the threat.)* He touches the white scar: a burst of static and the picture tears. Half a second of black noise and reversed whispers, then back. | 15.6–22.2 |
| 5 | **Total Isolation**, high in the rafters, looking down | A small black figure alone in a dead hall: a press, a lathe, chains, scrap, long shadows. He turns slowly, all the way round. Wind in the vents, drips echoing. | 22.2–27.0 |
| 6 | **The Path to the Shutter**, low behind him, swinging into the side-on fight view | Moonlight spills under a rusted roll-up shutter 20 m away. He walks, splashing in shallow water; a cello drone swells. The camera arcs round to the side and the letterbox opens. | 27.0–30.6 |
| ▶ | **Play** | **HOLD THE RIGHT SIDE TO WALK.** Five metres from the door the scene takes him: two shadows cross the slit of light, footsteps outside, then a latch clanks twice and the shutter rattles. The shutter flies up (a clang, slow motion, a flood of moonlight) and three men walk in. *HIM: "Wait... who are you?"* *THUG: "He's awake. Finish it."* *(added: the first line of plot, they were sent to finish him)*. | — |
| ⇠ | **The escape** *(added)* | He doesn't know he can fight, so he **runs**: *"...No."*, he turns, *"Get him!"*. A rusted chain across the floor takes his foot and he goes down full length into the puddle in slow motion. On the ground, ears ringing, he hears them walk up, one laughing. Lightning, and in the flash **his knife glints** on the floor ahead. He crawls to it through the water (*"Look at him. Crawling."*) and his fingers close on the handle. **The past flashes in for 4.5 s**: rooftop rain, a red corridor, a neon alley, a stairwell, headlights, a bar window, a red moon, a hotel bed. These are noir stills of his rig in fights he doesn't remember, in a new order each time, over a racing heart, ending on a thunderclap. He gets up with the knife and turns to face them, and they step back: *"...My hands remember."* The fight, with the knife. | ~18 s |
| ✦ | **The first memory** *(added)* | When the last man falls: a tear of static, reversed whispers, one white frame, then **YOU'VE DONE THIS BEFORE.** over a heartbeat. This is the seed of chapter 1 (the hitman). | +1.8 after the last kill |
| ■ | **End card** | **PROLOGUE COMPLETE**, then CHAPTER 1 · THE HITMAN · COMING SOON. Buttons: PLAY AGAIN / MAIN MENU. | — |

**Rules**
- **SKIP** (top right) jumps from any shot to the walk.
- A defeat restarts at the door, not at the wake-up, with the knife already in his hand (the escape plays once per run).
- The fight HUD stays hidden during the cinematic and the breach.
- The menu offers STORY MODE (this prologue) and FIGHT MODE (the tower, unchanged).
- New animations: `lie`, `allfours`, `rise`, `idle_loose`, `look_hands`, `touch_temple`, `walk`, and for the escape `trip`, `prone`, `crawl`, `reach_grab`, `prone_knife` and `rise_knife` (`tools/blender/build_stickman.py`).
- Flashback stills: rendered with the real rig by `tools/blender/render_flashbacks.py` (preview: `docs/previews/flashbacks.png`).
- New sounds: built from real recordings and synthesis by `tools/audio/make_story_sfx.py`.
