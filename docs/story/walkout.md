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
- **One red.** Her ribbon is the only red. The fire is realistic fire colour (orange and white-hot)
  and the smoke black; the ribbon's tails whip in the blast wind during the slow motion. The
  walkie's battery pip is red too (set up in ch 5), but it dies after the blast.

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

**The take, one shot, 69 s.** The blast waits until they are near the far end of the bridge, so the
whole of Kessel Hill is in frame behind them when it goes, as the house is in the reference:

| t | Picture | Sound |
| --- | --- | --- |
| 0.0 | The great door, framed by the porch. The camera is still. | Wind, birds; the drone. |
| 2.0 | Two shapes against the light deep in the hall: a tall black one, a small white one, hand in hand. | Footsteps inside. |
| 4.3 | They step out. The camera backs away in front of them. | The pulse: a kick on the steps. |
| 4.3-16 | The forecourt at a walk. The fountain slides past on the left; she skips. | |
| 16 | They slow to a stroll. She raises her walkie: "Papa? ...I can see you. Over." | The music thins. |
| 19.4 | His walkie from his coat: "I see you too, sweetheart. Over." | |
| 24 | The stables' door bangs. The last man runs in from the right, past the lens, and kneels at a box by the gate. | The door, running feet. |
| 28.6 | He comes into frame low on the right, his hands on the plunger: "Boss said... if he falls, the hill burns." | The ratchet. |
| 31.3 | **YOU:** "...Then burn it." Through the gate onto the bridge. | The build begins. |
| 31-47 | **The reveal.** Down the bridge, the camera falls back, widens and tilts up until the whole fortress fills the frame behind them. | Strings and pulse, rising, timpani. |
| 41.5 | "Can we go home now? Over." | |
| 44.6 | "Over and out." The click. | *Click.* |
| 46.8 | Far behind them, small, the man pushes the plunger down. | The music cuts dead. |
| **47.2** | **The whole hill goes up, left to right, over five seconds:** the great door, the balcony, every window low then high, the corner turrets (their spirelets topple), the roof terrace, the chimneys falling, the stables, the courtyard, the towers behind, the keep (a pillar of fire), then the observatory dome blown off the tower and falling. | Each blast arrives late by its distance; the pressure wave hits them; glass rains; masonry collapses; a roar under it all. One huge chord. |
| 47.4 | She flinches; his hand on her head; she holds his leg. They falter, then walk on. | |
| 48-52 | Fire in every window: the fortress has burned through. Black smoke climbs and spreads across the sky; ash and embers drift past the lens. | Ringing. |
| 52.6 | **Slow motion.** The camera pulls further back and up over the burning hill. | The lullaby. |
| 54.4 | A last vast fireball rises slowly out of the roof. | Deep and slow. |
| 55.6-60 | She looks up at him; he looks down; his shoulders drop. She takes his hand again and swings it; the ribbon whips red. | |
| 62.0 | Two clicks on her dead walkie. | *click... click.* |
| 63.6 | Two back from his coat. | *click... click.* |
| 66.6-69 | Fade to black. **OVER AND OUT.** | |

**The blasts** (BlastFx), each built in layers:
- a white-hot flash and a shockwave ring;
- a fireball from an animated flipbook (white core through yellow, orange and deep red to black smoke at its edges), with a glow;
- stone blocks that tumble out trailing smoke, land and bounce; burning fragments streaking out; sparks and glass glinting;
- a wall of dust along the ground;
- a burst of black smoke and a column that keeps climbing and spreads over the sky;
- a fire that stays: flipbook flames, a glow, embers.

The biggest blasts add a pillar of fire and a smoke cap. Every fire also lights the fortress's
stone through the ProjectInk/FireLit shader: up to eight flickering fire lights, the flash, and
the fade into a burning bake (`_Burn`: windows full of fire, the morning dimmed by smoke).
The flipbooks come from `tools/fx/make_fx_textures.py`. The fire is real fire-coloured here,
because the user asked for realism; the ribbon stays the only red.

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
