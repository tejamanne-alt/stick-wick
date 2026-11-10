# Chapter 6 · The walk-out ("OVER AND OUT")

The highlight of the game. One long take of a father and his daughter walking out of Kessel
Hill at dawn, the fortress blowing up behind them, ending in slow motion on the bridge. The
reference is a famous TV scene: a man walks out of a stately house, the camera backing away in
front of him, explosions behind him, slow motion on the bridge at the end. We follow its camera
work and timing. Nothing else is taken from it: no cigarette, no characters, no house.
In its place: two stick figures hand in hand, two toy walkie-talkies, and the red ribbon.

## Why it works with stick figures

- **Two shapes, linked by one line.** A tall black figure and a small white one, with their hands
  joined between them. Against white fire and black smoke, that's the clearest silhouette the
  rig can make. Carrying her on his hip would merge them into one blob.
- **Everything is a big gesture.** The walkie raised to the head. A hand on her head. Her head
  tilted right up to look at him. Their joined hands swung forward. No faces needed.
- **One red.** The fire is white and the smoke black, as all fire in the game is. Her
  ribbon is the only red. Its tails whip in the blast wind during the slow motion. The walkie's
  battery pip is red too (set up in ch 5); it dies after the blast, so the slow motion has only
  the ribbon.

## Where it sits

Chapter 6, after the roof: the fall, the reunion, the ribbon tied, the storm passing, the book
picked up ("...Forty-one names."). Then this. Then the chapter card **THE STORM**, then
chapter 7 (Harrow at the bottom of the hill: "Kessel Hill's still burning. Was that you?").

## Setups paid off here

| Where | Line |
| --- | --- |
| Ch5, the drive (5.7) | **YOU:** "And if you can't talk, click it twice. Two clicks means you're okay." **MAYA:** "Two clicks. Okay." *click-click* |
| Ch6, the Warden, beaten (6.3) | **WARDEN:** "Boss falls... hill burns." "Nothing left... for Harrow." |
| Ch6, the roof, Voss | **VOSS:** "If I fall, Kessel falls with me. Every page of it." |

Voss's dead-man's switch: the whole hill is rigged, so if he dies, everything that could hang
him burns. It's the same trick the hero played with box 417. The last of his men carries out the
order. The hero lets him.

## The set: Kessel Hill's front (tools/blender/build_fortress_front.py)

The fortress was built side-on for the fight. Its front (the camera side, -z) now has a way out,
used only in this scene and baked in dawn light:

- **The great hall's facade** (`FortFacade`), x 128-166 on the hall's front wall:
  - a plinth and string courses;
  - stepped buttresses with gablets, and octagonal corner turrets with spirelets;
  - pointed windows with tracery, mullions, transoms, hood moulds and label stops;
  - an upper row of trefoil windows;
  - machicolations under a crenellated parapet, gargoyle spouts;
  - clustered chimneys, drainpipes and hoppers, ivy up the buttresses, iron wall lanterns.
- **The great door and its porch** (`FortPorch`), x 147:
  - a deep pointed portal with four receding orders on jamb shafts with capitals;
  - a carved tympanum, and plank doors with strap hinges and studs, standing open on the dark;
  - a porch in front: two carved columns (spiral fluting, diaper bands, capitals);
  - a rib-vaulted ceiling, and a balustraded balcony with urns on top.
- **The forecourt** (`FortForecourt`), z -3 to -29:
  - gravel with a paved path to the gate;
  - a parterre of clipped box hedges, topiary cones and balls;
  - an octagonal fountain at x 142.5 z -15 (two bowls, a stone knight on top, still water);
  - stone benches and iron lamp posts;
  - the west balustrade with statues on pedestals over the drop;
  - the east range: a two-storey stable block with slate roofs, dormers, chimneys, a clock
    turret, and the door the last man runs out of.
- **The gate** at z -28.8: two great piers (rusticated base, diaper-carved panels, cornice,
  urn finials, a carved beast on each), lanterns on brackets, wrought-iron gates swung open.
- **The bridge** (`FortBridge`), z -29.5 to -60, 5 m wide:
  - a setts deck with kerbs, and brick parapets with stone coping;
  - piers every 7.5 m, each carrying an iron lantern on a post;
  - four arches over the gorge, with spandrels, voussoirs and cutwaters;
  - a smaller pair of piers and railings at the far end.
- **The gorge and the far bank** (`FortGorge`):
  - the forecourt's buttressed retaining wall dropping 30 m to a river;
  - cliffs and rocks, and mist in the gorge;
  - the far bank a moor of heather and grass, with a dry-stone wall, a lone bent tree, a
    milestone, and the road down.

All of the front plus every part seen from it (hall, roof, keep, back wall, sky, court) gets a
third bake, `_Dawn`: the storm has passed and the sky behind the hill is pale, under a low sun
from the right. LightningSwap gains a dawn set; the scene swaps to it when the walk-out starts.

## The take (StoryWalkout.cs)

The clock runs in real seconds. Positions are Unity metres: x along the hall, z towards the camera
(negative), y up from the hill's level.

**0 · The establishing wide (5.5 s, before the take).** From the far end of the bridge: the whole of
Kessel Hill at dawn. The bridge is empty and its lanterns still burn. The night's smoke drifts off
the roof, crows circle the tower, and the wind blows. Very slow push in. Cut to the doorway.

**The take, one shot, 61 s:**

| t | Picture | Sound |
| --- | --- | --- |
| 0.0 | The great door, black inside, framed by the porch columns. The camera is still, chest-high. | Wind, birds. A low drone starts the score. |
| 2.0 | Two shapes take form in the dark: a tall one, a small one beside it, hand in hand. | Footsteps on stone, inside, echoing. |
| 4.3 | They step into the light. The camera starts to back away in front of them. | The score's slow pulse: a kick on each of his steps. |
| 5-11 | Out of the porch. The camera drifts right, then settles centred. The carved columns pass the lens. | |
| 11-19 | The forecourt. The fountain slides past on the left; the facade fills the frame behind them. She skips; he shortens his stride. | |
| 19.5 | She stops, pulling on his hand. He stops. | The pulse holds. |
| 19.8 | She raises her walkie (its red pip blinking): **MAYA (walkie):** "Papa? ...I can see you. Over." | |
| 21.2 | Far right: the east range door bangs open. A man runs in through the gravel. | The door, running feet. |
| 22.8 | He tilts his head down to her, takes his walkie from his coat and raises it: **YOU (walkie):** "I see you too, sweetheart. Over." | |
| 24.6 | The man drops to his knees at a box behind them on the right, lifts the plunger's handle and looks at them. | Clatter, the ratchet. |
| 26.0 | **MAN:** "Boss said... if he falls, the hill burns." They both turn their heads to him. She presses against his leg. | |
| 28.8 | He turns back to the road. **YOU:** "...Then burn it." | |
| 30.0 | They walk on. | The pulse comes back, stronger: strings. |
| 31.5 | **MAYA (walkie):** "Can we go home now? Over." | |
| 34.4 | **YOU (walkie):** "Over and out." He clicks off on "out" (35.6) and puts the walkie in his coat. | *Click.* |
| 36.0 | Behind them the man pushes the plunger down. | Silence for half a second. |
| 36.3 | **The great door blows out:** a white fireball through the doorway they walked out of. | The blast: a crack, a deep boom. The score hits one huge chord and stops. |
| 36.6-40 | The chain, left to right: the facade's windows one after another, the porch, the roof terrace, the east range, the tower's dome (the biggest), the keep, the gate towers. | Boom after boom; stone raining down, glass. |
| 36.5 | She flinches and starts to turn. He lets go of her hand, puts it on the back of her head and draws her against his leg. They keep walking. | |
| 38-44 | Between the gate piers onto the bridge. Black smoke climbs and fills the sky behind them. Embers drift down, stones land in the forecourt. Her walkie's pip stutters and goes out. | The fire roars; a high ringing comes in over everything. |
| 44.5 | **Slow motion** sets in over 1.5 s. The camera backs away a little faster, rises and widens: the whole burning hill behind them. | The ringing; the fire far off; the music-box theme comes in softly. |
| 47 | She looks up at him, her head tilted right back. He looks down at her. His shoulders drop: he can stand easy for the first time in the game. | |
| 50 | She reaches up and takes his hand again, and swings their joined hands forward: a skip. The ribbon's tails whip red in the hot wind. Embers float past the lens. | |
| 53 | She holds up her dead walkie and clicks it twice. | *click... click.* Loud in the hush. |
| 55 | He presses the walkie in his coat twice. | *click... click.* |
| 57-61 | Hold on them walking. Smoke fills the sky, white fire beneath it. A slow fade to black. | The theme's last note, left unresolved. |

**After:** black. "OVER AND OUT" in small spaced letters. Then the chapter card **THE STORM**
(its line depends on whether he reached for Voss).

## The camera

One unbroken move. Until 4.3 s it's a locked-off shot at the porch, 5.6 m from the door. Then a
reverse dolly at their walking speed:
- 3.0-3.4 m in front of him, lens at his chest height;
- framed from his knees up, with her whole figure beside him;
- 36° vertical field of view;
- a slight sideways arc at the start;
- a creep in to 2.6 m while they stand talking.

In the slow motion it pulls out to 5.5 m, rises 0.5 m and widens to 41°, so the whole burning
fortress gets into the frame. A handheld breath stays on the whole time.
