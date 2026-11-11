# Chapter 6 · KESSEL HILL (v3): the ascent

> **Status: approved.** It replaces the fortress fight (`chapter5_fortress.md`, `StoryFortress.cs`,
> `build_fortress_set.py`) from the cliff road up to Voss's fall. The walk-out and the epilogue are kept.

## 0. Why rebuild, and the rules that fix the glitches

### What's wrong now
- **One flat lane:** the fortress is one flat lane with a painted set around it. Pillars, walls and
  furniture are only pictures, so fighters walk through them.
- **No use of the fortress:** nothing up, nothing down, no reason to be in a fortress.
- **Uneven grandeur:** it comes from the cutscenes, not from the play.

### Rules for the new build
1. **Everything solid is real.**
   - A thing is either behind the fighting plane (background), or it is **in** the level as nav
     geometry: a floor, a wall, a step, cover, a ledge.
   - Nothing solid sits on the fighting plane as a picture.
2. **The builder tests itself.** It exports every solid's footprint, and a check fails the build if
   any walkable span crosses a solid. The same goes for any spawn point, or any clip's travel at a
   level's edges.
3. **Vertical, like the warehouse:**
   - several floors per level, joined by stairs, ladders, ledges and chains
   - one-way platforms where it makes sense: a gallery, a scaffold
4. **The tower is always in sight.** Maya's lit window at the top of the observatory tower is in the
   background of every outdoor level, coming closer, so the player always sees the goal.

## 1. The shape: four acts, twelve levels, one climb

The whole chapter goes **up**: from the valley road to the roof, about 140 m of height. Each act
ends higher than it began. You feel the climb in:
- the camera: more sky, more drop below
- the wind: louder with height
- the score: one more layer each act

| Act | Levels | Feeling | Score layer added |
|---|---|---|---|
| I · THE APPROACH | 1 road · 2 the great stair · 3 the barbican | outside, exposed, the size of the place | storm + low strings |
| II · THE BREACH | 4 gate passage · 5 the wall walk · 6 the wall tower | trapped, high, the wind | taiko drums: the katana arrives |
| III · THE BAILEY | 7 the courtyard · 8 Voss's gallery | dark, the blackout, Voss's world | choir |
| IV · THE KEEP | 9 the great hall · 10 the tower stair · 11 the observatory · 12 the roof | the climax coming | all of it, then silence for Voss |

### Pacing (decided: eleven levels and one passage, about 25–28 minutes)
- **The road** is a short warm-up, about 90 s.
- **The wall tower (6)** is a 30-second descent with one ambush on a turn, not a full level.

The rest are about 2–3 minutes each:
1. **Fight** (the main thing)
2. **Traversal** (a set piece you play between levels)
3. **A story beat** (short)

Then the next level. There is a checkpoint at every level.

## 2. Level by level

### ACT I · THE APPROACH

**1 · THE SWITCHBACK ROAD.** The cliff road climbs in two switchbacks that you see one above the other.
- **Layout:** the lower road (fight) with the upper road above it (enemies). A retaining wall joins them.
  - You can climb it (swipe up at the wall) to cut the corner.
  - Or take the long way round past the truck.
- **Opening:** the gang's truck parked at the bend, two men on watch smoking by it.
  - The stealth start: crouch and take them, or don't.
  - If the alarm goes, the men on the upper road throw stones and torches down.
- **Interactive:**
  - **Tap the truck's cab:** he releases the handbrake. The truck rolls back down the road and takes the men with it (a slow cinematic).
  - **Kick the floodlight:** it topples into the dark.
- **Sound:**
  - wind in the pines, the valley far below (a distant dog, a train)
  - gravel and puddle footsteps, the truck's idling engine

**2 · THE GREAT STAIR.** A monumental stair cut into the hill: three flights, two landings, stone lions on
the plinths, iron lanterns.
- **Fighting on stairs, as it really works:**
  - **The man above has the reach and the weight.** Fighting uphill is harder; the game is pushing up.
  - **Kicked or thrown down the stairs,** a man tumbles down several steps (a tumble clip that follows the steps).
- **Defenders roll barrels down the flights:**
  - swipe up to jump one
  - hold to drop behind a lion plinth
  - or time it so the barrel takes the men climbing behind you
- **The landings are the arenas;** the flights between are a running fight.
- **The reveal:** at the top of flight two, the camera cranes back and up to show the whole stair, the
  tiny figure, the gate above and the tower above that, in one lightning flash.
- **Sound:** boots echoing on stone, barrels thundering down the steps, a lion's iron ring clanging when hit.

**3 · THE BARBICAN AND THE DRAWBRIDGE.** The drawbridge is **up**, over the dry moat (a chasm).
- **The fight:** on the barbican's forecourt.
  - Men on the gatehouse above drop stones through the **machicolations** (the murder holes).
  - Each drop is telegraphed by a pale circle on the floor and grit falling.
  - You can lure men under them.
- **The way in: the drawbridge chain.**
  - Hold to climb it hand over hand (the hanging system, extended).
  - Swipe left or right to swing past a stone dropped on you.
  - At the top, through the gatehouse window.
- **Sound:**
  - the chain's links creaking under his weight
  - the wind louder up the chain
  - the stones whistling down

### ACT II · THE BREACH

**4 · THE GATE PASSAGE (the killing ground).** The historical bent entrance: a narrow passage with a turn, lit by torches.
- **Both portcullises slam down** (front and back): trapped.
  - Men in the passage, more above the murder holes.
- **Close quarters only:** the clinch, elbows, walls for PINNED, CROSSFIRE in the narrows.
- **Out:** the inner windlass. **Mash** to heave it while a man tries to stop you (tap him off between heaves).
  The portcullis grinds up.
- **Sound:** the portcullis's iron crash, the passage's tight echo, torches roaring in the draught.

**5 · THE WALL WALK.** The curtain wall's battlements:
- **The layout:** a long narrow walk high above the courtyard on one side and the cliff on the other.
  - Merlons give cover from the stones thrown from the next tower.
  - Hoardings (wooden galleries) hang off the wall.
- **Men come from both towers at the ends:** CROSSFIRE moments by design.
- **Throws over the parapet** (the existing over-the-edge throws).
- **Wind gusts:** telegraphed by flapping banners; they stagger everyone (you can use it).
- **THE FIRST SWORDSMAN:** he comes out of the far tower. Beat him and take his **katana**.
  - Voss's voice, the first lesson: *"The line comes before the blade."*
- **From here the men are mostly swordsmen.**
- **Interactive:** cut a banner's rope and ride the banner down (swipe at the banner, hold on).
  This is the shortcut into the tower below.
- **Sound:**
  - the wind loudest yet, banners cracking
  - the first katana draw (sayabiki: the scrape)
  - **taiko drums enter**

**6 · THE WALL TOWER.** A spiral stair down inside the tower. It's **clockwise**, as real castle stairs are, so
the defender coming down has room for his sword arm and the man going up doesn't.
- **The reversal:** you are going down, so here the advantage is **yours**. The game says so (Voss's voice:
  *"Take the high step. Always."*).
- **The fights:** short, on the turns. The camera is in the tower's cutaway.
- **At the bottom:** a door out into the courtyard.
- **Sound:** the tight stone echo, his breathing loud, steel scraping the walls.

### ACT III · THE BAILEY

**7 · THE COURTYARD.** The inner bailey, on three levels:
- **Ground:** cobbles, the well, carts.
- **The stable roofs and the hay loft:** reached by a ladder and a slope.
- **The chapel steps.**

**The generator and the floodlights.** They sweep like searchlights. In their beams you're seen;
out of them, with the lightning the only light, you can do stealth.

**THE BLACKOUT** (kept: the best idea of the old chapter):
- Lightning hits the floodlight pole, the generator bursts, the dark.
- The katana in the dark: **the pass lit by a strike.** Three men in a line, black. Your drag, the flash.
  They stand. The sheathing click. They fall.

**Interactive:**
- **The hay hoist:** cut its rope, the bale drops on the man below.
- **The cart:** kick it down the slope.
- **The stable roof:** slide down the slates (automatic). Swipe at the end to come off it onto a man.
- **THE CHAPEL BELL** is the alarm:
  - If they see you before the blackout, a man runs for the bell rope and the whole bailey wakes.
  - **Cut the rope first** (a cut at it) and they don't.

**Sound:**
- wind through the stable roofs
- the bell, the generator's hum then its death
- the dark's silence between thunder

**8 · VOSS'S GALLERY.** The keep's lower floor: Voss's collection, his private world. The fortress is
European; this one room is Japanese.
- **The room:**
  - a wall of katanas
  - suits of armour on stands
  - glass cases
  - **paper screens (shoji)**
- **THE SILHOUETTE FIGHT:** men behind the lit screens are shadows on the paper. Cut through the
  screen at a shadow (an ink cut through ink). The ink style was made for this.
- **The story beat:** among his trophies, a photograph. Voss, and the hero at twenty, kneeling, a
  bokken (a wooden sword) across his knees.
  - A memory, a short flash: Voss teaching the Kata of Three in a dojo.
  - *"You will use this on me one day. That's how I'll know I taught you well."*
  - The player learns the kata here (its three lines shown in ink), the exact thing Voss will use on the roof.
- **Sound:**
  - inside, dry, muffled thunder
  - paper tearing
  - armour toppling
  - the hiss of a blade drawn from a display

### ACT IV · THE KEEP

**9 · THE GREAT HALL: THE WARDEN.** Rebuilt as a real room:
- **The pillars are solid** (fighters go round them, cover from the chain).
- **The long table:** slide along it, swipe up to jump onto it.
- **The minstrels' gallery:** a balcony above, reached by its stair. Escape the chain up there, or drop
  from it onto him.
- **The fireplace:** big enough to stand in.

**THE WARDEN, three phases:**
1. **THE CHAIN:**
   - long swings in strings of three
   - his chain **wraps your katana**: you must let it go (swipe down) or be dragged
   - the katana skids away across the floor. Bare hands now; you can get it back later.
2. **THE CHANDELIERS:** he cuts them down where you stand (move!). Their crash breaks the table.
3. **TIMBER:** furious, bare-handed. **Calf kick, calf kick: his leg goes. Head kick.** He falls like a tree.
   - He drops his chain; take the katana back from the floor.

**Sound:**
- the hall's long reverb
- the fire roaring
- the chain whistling, its crack on stone
- the chandeliers' chains snapping, the crash

**10 · THE TOWER STAIR.** Outside, up the observatory tower in the storm: a stair spiralling round the outside.
- **Still dry:** the rain is held for the roof, as built. The first drop falls between him and Voss after
  "So here we are"; that beat is too good to spend earlier. Here it's the wind at its height and the lightning
  close enough to taste.
- **Men above on the stair:** fight upwards, the reverse of the wall tower (now *you* are cramped).
- **A broken section of the stair:**
  - You can only see the gap in a lightning flash.
  - **Tap with the flash** to make the jump.
  - Miss the timing and you're hanging from the edge (the hanging system), pull up.
- **The walkie clicks on:** *"Papa! I see you! You're tiny!"*
- **Sound:** the full storm, the wind at its height, the walkie's crackle, her voice clear through it.

**11 · THE OBSERVATORY.** Inside the dome at the top: the giant telescope she talked about.
- **The dome is open:** its slot is open to the storm and turning.
- **Sound:** wind, not rain: the rain is held for the roof.
- **The last guards:** Voss's best, two swordsmen together.
- **Interactive:** **drag to swing the telescope** across the floor. It sweeps a man off his feet.
- **The way out:** the dome's crank turns the slot to the roof terrace's door.
- **Sound:**
  - the dome's great gears turning
  - the wind through the open slot
  - **then the music stops**

**12 · THE ROOF: VOSS.** The terrace, the broken parapet, Maya held at the edge.
- **The talk and the trade:** as built, including the first drop of rain between them.
- **I · THE TEACHER:** his shikomizue against your katana.
  - **His lines:** red lines, faster than any swordsman's, and feints.
  - **His tell:** two taps of the cane before each thrust.
  - **The climax:** the **Kata of Three**, the three lines you learned in his gallery. Mirror all three:
    three rings, three sparks, slow motion. You have beaten the teacher at his own lesson.
  - **Then:** the hero **throws the katana aside.**
    - *VOSS:* "...What are you doing?"
    - *YOU:* "Not yours."
  - Bare hands from here. The clinch disarm takes the cane blade from him.
- **II · THE GUN · III · THE EDGE:** as built, then REACH or LET GO, the fall.
- **Sound:**
  - silence under the rain for the talk
  - the duel's score: the taiko alone
  - the katana's clatter across the stones when he throws it (it should hurt)

## 3. The enemies

| Who | Where | What they do |
|---|---|---|
| Men on watch (unaware) | road, courtyard | the stealth openings |
| Brawlers | acts I–II | bare-handed; fear the katana once you have it; try to grab the sword arm |
| Stone-throwers | upper road, gatehouse, wall towers | from above: telegraphed drops and throws (swat them) |
| **Swordsmen** | from the wall walk on: most of the fortress | fence: red lines with tells, feints, they read a repeated line, lunge, bind. Their katanas drop when they fall. |
| Two masters | the observatory | swordsmen who work together: one feints while the other cuts |
| The Warden | the great hall | chain, chandeliers, TIMBER |
| Voss | the roof | the teacher |

## 4. Sound design for the whole chapter
- **The storm has height:**
  - The wind's level and pitch rise with his height.
  - Thunder's delay after a flash shortens as the storm comes over: the thunder count from chapter 5, paid off.
- **Rooms have their own acoustic:**
  - open air (no reverb, wind)
  - the passage (tight slap echo)
  - the tower (stone spiral)
  - the gallery (dry, close)
  - the hall (long reverb)
  - the dome (metallic ring)
- **Surfaces:** stone, wet stone, wood (hoardings, the loft), gravel, paper, iron, each with its own footsteps and body falls.
- **The katana's sound set:**
  - the draw's scrape
  - the cut's whoosh, shaped by the swipe's speed
  - the ringing clash with a long decay
  - the sheathing click as punctuation after the pass
- **The adaptive score:**
  - Stems add up act by act (storm → drums → choir).
  - In slow motion the score drops an octave and slows (as now).
  - On the roof it stops for the talk.
- **Voss's lessons:** spoken close and dry, as a memory, under the storm.

## 5. The interactive moves between levels (gesture → automatic move)

| Moment | Gesture | What he does |
|---|---|---|
| Climb the retaining wall | swipe up at it | hands, a heave, up |
| The truck's handbrake | tap the cab | it rolls |
| A barrel coming | swipe up / hold | jump it / drop behind cover |
| The drawbridge chain | hold, swipe left / right | climb hand over hand, swing aside |
| The windlass | mash | heave the portcullis up |
| A banner | swipe at it, hold | cut its rope, ride it down |
| The stable roof | (automatic), then swipe | slide, then jump off onto a man |
| The bell rope | a cut at it | no alarm |
| The broken stair | tap with the lightning | the jump across |
| The telescope | drag | swing it |
| The dome crank | drag in circles | turn the slot to the door |

## 6. What it needs (and the order, merged into docs/design/backlog.md)

| Step | What |
|---|---|
| 1 | The V4 body for the fortress (built on V4 from the start: the old fortress is **not** converted) |
| 2–6 | The weapon grammar, the ink, the katana clips and play, and the swordsmen: as in the backlog |
| 7 | **This rebuild:** the set (Blender), the level geometry with real collision and the self-test, and the twelve levels |
| 8 | The traversal set pieces and the new clips |
| 9 | Sound: the per-room acoustics, the surfaces, the katana set, the score stems |
| 10 | The duel |

New traversal and reaction clips include:
- the stair tumble and fighting on stairs
- climbing the chain and the wall
- riding the banner
- sliding on the slates
- heaving the windlass
- swinging the telescope
- the jump at the broken stair
- the barrel jump
- cutting through the screens

### Decided
- **Length:** eleven levels plus the wall tower passage, about 25–28 minutes, a checkpoint at each.
- **Voss's gallery:** kept.
