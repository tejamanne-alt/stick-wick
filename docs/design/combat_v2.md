# Combat 2.0 and the street fight (proposal, for review)

## Part 1: rethinking the combat

### What is wrong today

| Problem | Where it comes from | How it feels |
|---|---|---|
| **Mashing.** Taps chain into a fixed punch or kick string, and the hero auto-dashes up to 2 m to whoever is targeted. | `Strike` → `NextInString`, `UpdateDash` | You tap; the game fights. Every fight feels the same. |
| **The great moves are random.** The head scissors fires on a 30–60% dice roll; so do other context moves. | `TryHeadScissors` (`Random.value > chance`) | You never feel you *chose* it, so you can't learn it. |
| **Hits don't connect.** Most hits push a man back with a generic reaction; nobody is held, steered or driven into anything. | `ResolveHit`, knockback | Floaty, like shoving cardboard. |
| **The surroundings are side gimmicks.** Crate kicks, drums and racks are separate from your combos. | Props | The warehouse feels like a set, not a weapon. |
| **No defence but the dodge.** | `Dodge` | No skill expression in timing. |

**Why the head scissors works** (the model for everything new):
1. Two bodies move together: the enemy is part of the move.
2. Weight: the hero leaves the ground, there is a beat of slow motion, then the slam.
3. A clear, physical outcome: he ends up on the floor, and the camera sells it.

### Principle zero: grounded and realistic

Every fight should look like two men really fighting, in the style of a grounded action film, not a platformer.

| Today | Realistic |
|---|---|
| Runs are fast (and the hero dashes 2 m in a blink to reach a target). | **Human speeds.** A walk at 1.4 m/s, a run at about 4.5 m/s, a sprint only when chasing or fleeing. **No auto-dash:** he closes the gap with a step or two, and you see the steps. |
| Big jumps, flips, wall runs, flying kicks across the room. | **Short, heavy jumps** (about 0.5 m off the ground): enough to clear a sweep, climb a ledge or vault a car bonnet, not to fly. **No front flips or wall runs in the story.** Flying kicks only off something (a step, a car bonnet, a rail), short and committed. |
| Men are launched across the floor by ordinary hits. | **Weight.** A punch moves a man half a step; a heavy blow knocks him down where he stands; only a throw or a charge carries him a few metres. Falls land with a thud, and he gets up slowly. |
| Floaty gravity, long air time, freeze-frame hit-stops. | **Real gravity** (9.8 m/s² feel, short airtime), small hit-stops (2–4 frames) on heavy hits only. |
| Strike strings that never tire. | **Breath.** Long flurries slow down; he recovers between exchanges. So do they. |

**No RAMPAGE in story mode** (already switched off: the button never shows and the move can't start). The story's peak moments are its finishers and context moves instead.

Fight mode (the tower) keeps its bigger, arcade feel by default; the story uses the grounded settings. It's one combat system with two tunings, and I can bring the tower closer later if you want.

### The new system: CONTACT

One rule: **what you touch decides what happens.** It means the man, his state, and what is around you. The outcome is chosen by the situation, never by dice, and the game *shows* you what a tap will do before you do it.

#### 1. Four verbs (still one-thumb friendly on a phone)

| Input | Verb | What changed |
|---|---|---|
| **Tap** left or right half | **STRIKE** | Short, deliberate strikes. A small step-in only (no 2 m dash). The string depends on the man's state (see 3), not on a fixed list. |
| **Hold** on a man (0.25 s) | **GRAB**, the new clinch | You take hold of him. From the clinch:<br>• **tap**: knees or a headbutt<br>• **swipe left/right**: throw him that way (into men, walls, objects)<br>• **swipe up**: head scissors, a suplex or a vault over his shoulders<br>• **swipe down**: a slam into the floor |
| **Swipe** left or right | **EVADE** | As now, plus **PARRY**: a tap toward a man in the last 0.25 s of his red wind-up deflects him and opens him up (a white flash and a ring of sound). |
| **Swipe** up or down | **MOVE** | Jump, climb, slide, duck (as now). |

#### 2. Context moves: deterministic, and shown before you act

When a man is next to something usable, a small white **context mark** appears on it, the same family of marker as the speech tick (e.g. `SLAM`, `DOOR`, `RAIL`). Tap or grab and the move happens, every time. No dice. Examples:

| Situation | Tap does | Grab, then swipe toward it, does |
|---|---|---|
| He's staggered with a **wall** behind him | A palm strike that bounces his head off it | Wall run, then a kick off his chest |
| A **table / car bonnet** beside him | A face-plant into it | Roll him across it into the next man |
| An **edge / rail** behind him | A kick that takes him over | A throw over it (the edge throw) |
| A **door** (car door, house door) | A kick that slams the door into him | Slam the door on him, twice |
| A **pole / lamp post** in reach | A swing round it, both feet into him | |
| **Two men** close together | | Throw one into the other (human bowling, now chosen) |
| He's **down** | A stomp (as now) | Drag him up as a shield |

#### 3. Strikes that read the man

The string changes with **his state**, so you learn to read him:
- **Guarding:** strikes bounce off. Go low (kick) or grab.
- **Open** (after his swing, after a parry): heavy strikes land in full. Strike three ends in a **finisher of the chapter's kind** (see 5).
- **Staggered:** he's open to context moves (the marks light up).
- **Down:** stomp, or haul him up.

He also **reacts by where he's hit**: head snap, a fold at the body, legs buckling, a stumble backwards. A hit lands *on* him; it doesn't just push him. The reactions use the stickman rig's existing clips plus a few new ones (`hit_body`, `hit_legs`, `stumble_back`, `clinched`).

#### 4. Flow

- **Variety builds MOMENTUM** (the meter that today mostly scores). Repeating the same move drains it, and the Foreman's "he reads you" already punishes it.
- **Full momentum** unlocks one **Finisher**: a two-body cinematic like the head scissors, chosen by what's around.

#### 5. Unique per chapter: the kit is the story

Each chapter gives the hero **things that become moves**, so no two fights feel alike:

| Chapter | Its kit becomes |
|---|---|
| 1 · Warehouse | Crates, drums, racks, forklift forks, the crane hook, office glass (as now, re-wired as context moves). |
| **2 · The Street** | **Car doors** (door slam), **car bonnets** (roll over and face-plant), **lamp posts** (pole swing), **café chairs** (a breakable weapon, three hits), **café tables** (face-plant), **newspaper boxes and bins** (lids as shields), **the bus-stop glass** (through it), **puddles** (longer slides), **scaffold planks** (see-saw launch), **the market stall's awning** (bounce attack), **the train overhead** (its rumble and strobing light stun everyone for a moment, and you get the first move). |
| 4 · Home | Doors, a frying pan, the stairs, the sofa (vault), the glass table, the bookcase. |
| 5 · The backyard | The swings (ride-kick), the slide (slide-kick), the monkey bars, the roundabout (spin-throw). |
| 6 · The fortress | Weapons racks, battlements, a portcullis. |

The INSTINCT tutorial (chapter 1) is re-taught with the four verbs: strike, grab, parry, then one context move.

#### 6. What changes in code

- **`HeroController`:**
  - the dash range shrinks to a step-in
  - `TryHeadScissors` and the other random picks are replaced by a deterministic `ContextMove` picker
  - a new **Clinch** state (grab, hold, release) with throws, slams and the scissors as clinch exits
  - a **parry** window on enemy telegraphs
- **`GestureInput`:** a *hold on a man* becomes GRAB (today a hold is walk/hold-to-move). Holding on empty ground still walks.
- **`Enemy`:** a guard stance, `Open`/`Staggered` windows, hit-zone reactions, a `Clinched` state and being steered by the hero.
- **A `ContextProp` component:** every usable object (crate, door, bonnet, pole, rail…) declares what it lends to a tap and to a grab-throw. The hero queries nearby props, and the context mark draws over them.
- **Clips** (`build_stickman.py`): `clinch`, `knee_clinch`, `headbutt`, `throw_side`, `suplex`, `slam_down`, `parry`, `hit_body`, `hit_legs`, `stumble_back`, `door_slam`, `pole_swing`, `bonnet_roll`, `chair_swing`.
- **Fight mode (the tower)** gets the same system: it's one combat for the whole game.

### 7. Enemies that really fight

**Why bare-handed men barely fight today:**
- They wait in a ring 2.2–3.4 m away, out of reach.
- Only two may attack at once (`AttackTokens.Max = 2`).
- Each attack is one telegraphed swing and then a retreat.
- The rest circle, wait 0.3–1.2 s and taunt.

**What the new enemies do** (grounded, like men in a real street fight):

| Behaviour | What it looks like |
|---|---|
| **Close in and stay close** | They fight at arm's length (0.9–1.3 m), not from a ring. The ones not in the exchange **move to flank** (one behind you, one cutting off the way out) instead of standing still. |
| **Combinations** | Jab–cross, jab–jab–hook, a body shot then the head, a kick to the leg. Two to four hits, with real wind-ups you learn to read. The red telegraph is kept only for unblockable heavy blows. |
| **Guard and defence** | Hands up between exchanges: high strikes are blocked (go low, or grab). They **slip** and **step back** from your strings, and **block-and-counter** if you keep doing the same thing. |
| **Feints** | A wind-up that stops: bait for an early parry. |
| **Grabs** | They clinch you too: one holds you while another hits. Break free, or reverse it into your own throw. |
| **Use the place** | They pick up a café chair or a bottle, slam you into a car door or a wall, and kick you when you're down (roll away). |
| **Pressure that adapts** | Up to three can be on you at once, with the others always moving. Hurt men back off, catch their breath and come again; a man left alone grows bolder. |
| **Fear** | When their leader falls, or half of them are down, the rest may hesitate, or run. |

**The street's crew** (all bare hands or street objects; no guns):

| Type | Style | How to beat him |
|---|---|---|
| **Boxer** (the backbone) | Guard up, fast combos, slips your punches. | Go low, grab, or parry his cross. |
| **Brawler** | Wild swings, headbutts, throws whatever is near. | Dodge the haymaker; he's open after it. |
| **Grappler** | Clinches and holds you for the others; throws you into cars. | Keep him at a kick's length, or reverse his grab. |
| **Bat / chain** | Long reach, heavy, slow to recover. | Get inside the swing; take the bat. |
| **Runner** | In, a jab, out (as now). | Easy points; parry him. |

**The mini-boss, THE DRIVER** (a professional, built on the same system), in three phases:
1. **The boxer:** a tight guard, counters, and combos that punish mashing. Learn his rhythm; parry his cross.
2. **The tire iron:** he pulls it from the car's boot. Heavy swings, the car's door slammed on you, your head driven into the bonnet. Dodge, get inside, take the iron.
3. **The car:** beaten and desperate, he gets in and drives at you. Dodge at the last moment; the car hits a lamp post. He staggers out bloodied for a last, close, ugly exchange, then the directed beat.

The Foreman (chapter 1) is re-tuned on the same system: guard, combos and his clinch become real behaviour, not specials.

---

## Part 2: the street (the continuation of chapter 2, after the bank)

**Story beat:** he walks out of the bank into the white morning. The black car's door opens: the clerk's call has been answered. He has the ring, the address, and somewhere to be, and they are between him and it. Same look as the rest of the game: the tower's black and white, wet reflective streets.

### The street, block by block (one continuous side-on lane, about 120 m, 4 zones)

| Zone | Place | Its kit | Fight |
|---|---|---|---|
| **S1 · Outside the bank** | The bank's steps, Café Noir's terrace (tables, chairs, its awning), the black car at the kerb, a newspaper box, a bus stop with a glass shelter. | Car doors, the bonnet, café chairs and tables, the bus-stop glass. | The men from the black car (4) and the driver. |
| **S2 · The market street** | A narrow street of stalls under awnings: crates of fruit, hanging signs, a scaffold over a shopfront under repair (planks, a cement mixer, a ladder). | Awnings to bounce on, the scaffold to climb (2 levels), planks, crates, the hanging signs. | A second car's men pour in at both ends. |
| **S3 · Under the railway** | An underpass beneath the elevated line: chain-link fence, dumpsters, a fire-escape ladder, pillars. **A train passes overhead:** the roar, dust raining down, light strobing through the sleepers. | The train moment (everyone stunned, you move first), dumpster lids, the fence (climb and drop), pillars (wall moves). | Bat-carriers and grabbers in the half-light. |
| **S4 · The crossroads** | A wide junction near the residential streets: a stalled tram or bus, traffic lights, a phone box, a newsstand. The lieutenant who runs this crew. | The bus's doors and bonnet, the phone box glass, the traffic-light pole. | **The mini-boss: THE DRIVER** (the black car's driver, a car-door brawler). Then a short directed beat: he leaves him on the ground. |

### The drama around it (shot by shot, short)

1. **The door opens** (end of the bank). Four men get out of the black car and fan out across the street. Their leader: *"The box. Hand it over."*
2. **YOU:** *"It's empty."* (a beat) *"...Come and look."* The fight begins with a parry tutorial beat (the first man's swing, a frozen moment: **PARRY**).
3. **Between S1 and S2:** a hard-breathing walk; he flexes the hand with the ring on it. **YOU:** *"They'll keep coming. Alder Lane is twenty minutes north."*
4. **S3, the train:** directed. The lights strobe, he sees them in flashes, and the first context-mark lesson: **GRAB**.
5. **After the Driver:** he's on the ground, cornered.
   - **YOU** (kneeling, his fist in the man's collar): *"Who sent you?"*
   - **THE DRIVER** (laughing through blood): *"You'll find out at home. We got there first."*
   - The hero's face. He stands and runs. Cut to black: **CHAPTER 2 COMPLETE · THE RING**, with chapter 4's home ahead.

**Checkpoints for testing** (added to the test panel): the street start, S2, S3 (the train), S4, the Driver, and after the Driver.

---

## What I need from you

1. **The combat:** go with CONTACT (strike, grab, parry, deterministic context moves with marks, per-chapter kits), **grounded and realistic** (human speeds, short heavy jumps, no flips or wall runs, real weight)?
2. **The order:** build the new combat system first and re-tune chapter 1 on it, then the street. *(Recommended: the street is built around grabs, doors and bonnets, so the system comes first.)*
3. **The street:** the four zones and the Driver as its mini-boss: good?
4. **The enemies:** men who close in, combo, guard, feint, grab, flank and use the place (section 7), with the Driver in three phases?
