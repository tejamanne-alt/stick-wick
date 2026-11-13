# Stick Wick: Combat System Prompt Sheet

A step-by-step plan for rebuilding Stick Wick's combat so it is **deterministic, readable and realistic**, in the gun-fu style of the *John Wick* films. There are no on-screen buttons.

How to use this sheet:
- **Part A** says what is wrong today, with file and line references, from the two gameplay clips and the code.
- **Part B** is the design spec (the "combat bible"). Every build prompt points back to a section of it, so the spec lives in one place.
- **Part C** has the build prompts. Paste them one at a time into a Claude Code session on this repo, in order, and don't start a step until the previous step's "Done when" check passes.
- **Part D** has the image and reference prompts for ChatGPT and Gemini, plus what to do with the results.
- **Part E** is the test and tuning checklist.

> **Keep the hero original.** Take the *choreography principles* from the films, not the actor, character names, logos or exact shots. That matters for store approval and IP, and image models usually refuse a real actor's likeness anyway.

---

## Part A: Diagnosis

### A1. What the clips show

| Time | What happens | Why it feels wrong |
|---|---|---|
| Clip 1, 0:01–0:07 | Hero, orange, green, black and purple stand on the same spot, and the bodies merge into one blob | Nobody can tell who is hitting whom. Nothing pushes bodies apart, and 3 enemies may attack at once |
| Clip 1, 0:15–0:21 | Hero and purple stand face to face, about 0.5 m apart, for 6 s. "PARRY!" flashes but nothing visibly changes | No hit-to-stagger-to-takedown progression. The exchange has no readable rhythm |
| Clip 1, 0:21–0:23 | The camera cuts wide → close → extreme close and back | Kill-cam hard cuts in the middle of an exchange make positions *look* like jumps |
| Clip 1, 0:25–0:31 | The black hero vanishes against the black racking | No rim light or outline on the hero |
| Both clips | Fighters are about 10–12% of screen height in wide shots | Too small to read limbs and telegraphs on a phone |
| Clip 2, 0:24–0:29 | Hero sandwiched between purple and orange while doing a "KICK STRING" | Two melee attackers at once, and the string is not aimed at a readable target |

### A2. Root causes in the code

**Teleporting** (the body moves without the animation earning it):
1. **Long dash.** The dash runs at **20 m/s for up to 0.6 s** (`HeroController.cs:32`, `UpdateDash` at `:1936-1946`) toward a target picked up to **9 m away** (`targetRange`, `:29`) and **on any floor** (`PickTarget(..., anyLevel = true)`, `:1096`). The run clip at 1.7× speed cannot sell 9 m in under half a second, so it reads as a blink.
2. **Glides.** Flying, spin and sword-leap kicks slide the body toward the target with `MoveTowards` (`:1987-1993`).
3. **Rampage strike.** It literally teleports next to any enemy within 30 m on any floor (`RampageStrike`, `:1272-1280`).
4. **Position snaps:**
   - when grabbed (`:2341-2344`)
   - at the end of the scissors (`:514`)
   - at the hang-to-climb switch (`Fighter.cs:259`)
   - during the iai dash: 5 frames lerped (`:658-668`)

**Same input, different result** (non-determinism):
5. **Random rolls in move selection:**
   - head-scissors fires on a **30–60% chance** (`:408-409`), and **65%** in the air (`:1568`)
   - the Rampage finisher is a coin flip (`:1286`)
   - enemy attack choice is random (`Enemy.cs:665`, `:678`)
   - the boss ignores interrupts 50% of the time (`Enemy.cs:755`)
6. **Hidden context checks.** `Strike()` runs about 10 situational checks before the normal string (`:1323-1365`): crate kick, stomp/giant-swing (which alternates on `comboStep++ % 2`), knife throw beyond 1.8 m, hand stomp, over-the-edge, springboard, death-from-above, scissors… The player can't see which one will fire.
7. **Target switching.** Target scoring favours telegraphing enemies (−2.5 m) and token holders (−0.8 m) (`:1109`), so the target jumps to a farther enemy mid-string.
8. **Wrong side from screen halves.** In Classic mode, which way a tap strikes is decided by **which half of the screen** was touched, not where the hero is (`GestureInput.cs:148`; hold-to-run at `:199`). When the camera isn't centred on the hero, tapping *on* an enemy can strike the other way. This alone explains much of "I pressed right and he did the wrong thing".
9. **Invisible high/low line.** Tap high = punch and tap low = kick, split at a fixed line at 45% of screen height (`kickLine`, `GestureInput.cs:41`), not relative to the hero.
10. **Unsynced animation and logic clocks.** Animation runs on the PlayableGraph's GameTime (`ClipPlayer.cs:88`) while hit checks read `body.Time` in `Update` (`:1999`). With hit-stop, slow-mo and a variable frame rate, the hit frame lands ±1–2 frames differently each time.
11. **Up to 3 simultaneous attackers** (`AttackTokens.Max = 3`, `Enemy.cs:1353`), and no body separation.

**Naive bodies:**
12. **The rig is 14 planar bones** (`build_stickman.py:45-60`):
    - no feet, toes, neck, clavicles or wrists
    - every joint bends in one plane, so there is no hip or shoulder rotation; punches and kicks have no kinetic chain
13. **No root motion and no foot IK** (`ClipPlayer.cs:84`, `:133`). Code moves the body while the clip plays in place, so feet slide.
14. **Mushy blends.** 40–125 ms crossfades between unrelated poses smear them. The hit reactions are 3 hand-keyed clips (front, back, knockdown), with no zone, direction or strength variants.
15. **Chibi proportions** (1.45 m tall, head radius 0.21 m, about 3.5 heads tall) read as toy-like. The level geometry was built for a ~1.9 m body (`BodyScale = 0.76`, `Fighter.cs:31`).

---

## Part B: Combat bible (the spec)

### B1. The determinism contract
1. **Same state + same input = same outcome, every time.** There is no `Random` anywhere in hero move selection, hit resolution or damage.
2. **Fixed combat clock.** All combat logic runs at **60 ticks/s** on an accumulator. Slow-mo scales the accumulator, not the tick length. All move timings are **integer frames**.
3. **Animation is slaved to the clock.** The PlayableGraph runs in `Manual` update mode and is evaluated by the combat tick, so the hit frame is always the same frame.
4. **Randomness only where it is harmless:**
   - `AiRng`: a seeded xorshift per wave, so the same seed gives the same wave
   - `FxRng`: free, for cosmetics only (sparks, ink, sound variants)
5. **Inputs are stamped with the tick** they were consumed on. Input log + seed = exact replay.
6. **Every context-sensitive move is shown before it happens.** A small *intent glyph* over the target shows what a tap will do right now (see B4).

### B2. The no-teleport contract
1. **The animation earns every metre.** Displacement comes from the clip's **root motion**, or from a visible locomotion clip moving at its authored speed.
2. **Speed caps:**
   - walk 1.6 m/s
   - run 6.5 m/s
   - sprint 8.5 m/s
   - authored dash moves at most 12 m/s and at most 3 m, always with a blur trail
3. **Motion warping.** At move start, `warp = contactDistance / clipRootDistance`, clamped to **[0.75, 1.30]**. Warping applies only during the startup frames, never after contact. Outside the clamp, pick the step-in variant or run first. Never stretch further.
4. **Distance bands** decide what a tap does. They are deterministic and shown by the target marker:

| Band | Distance (hero chest to enemy chest) | Tap does |
|---|---|---|
| Clinch | 0 – 0.8 m | Clinch moves (knee, elbow, grab) |
| Strike | 0.8 – 1.6 m | Next move in the string, in place |
| Lunge | 1.6 – 3.0 m | Step-in version of the next move (root motion up to 1.4 m, warped) |
| Run | 3.0 – 9.0 m, same floor only | Hero runs (visible cycle), the tap stays queued and fires a **running attack** on arrival |
| Out | > 9 m or another floor | Hero turns and faces; no attack (guns excepted) |

5. **Paired moves** (grabs, scissors, throws, being grabbed) start with an **alignment phase**: 6–10 frames in which both actors walk or blend to the sync point. After that, the victim's root follows a socket on the attacker. Nothing snaps.
6. **Teleport guard.** In development builds, any fighter whose position changes more than `capSpeed × dt × 1.5` in one tick, outside a move flagged `allowsWarp`, logs an error and draws a red line. PlayMode tests fail on any violation.

### B3. Input grammar: no buttons, hero-relative
Every gesture is judged **relative to the hero's on-screen position**, never the screen halves.

| Gesture | Meaning |
|---|---|
| **Tap** left or right of the hero, **above** the hero's waist line | **High** strike that way (punch string, or a shot with a gun) |
| **Tap** left or right of the hero, **below** the waist line | **Low** strike that way (kick string, sweeps, leg shot) |
| **Hold** (≥ 180 ms, still) on a side | **Run** that way. If an enemy is in the Clinch band on that side: **grab** |
| **Swipe toward** an enemy | **Lunge attack** (dashing knee or superman punch). Low swipe = **slide kick** |
| **Swipe away** from enemies | **Evade** (backstep). Twice in 300 ms = **roll** |
| **Swipe up** | **Jump**. Toward a *staggered or telegraphing* enemy 1.0–2.4 m away: **leg-scissor takedown** (B6.4) |
| **Swipe down** | **Duck**. Holding a weapon with a target: **throw it**. Over a downed enemy: **finisher** |
| **Tap a highlighted prop** (within 2 m, no enemy in the Strike band) | Walk to it and **pick it up** |
| **Tap during an enemy's red flash** (counter window) | **Counter** for the current weapon |
| **Two-finger tap** | **Rampage** (when full) |

Rules:
- **Buffer.** One command is buffered for **10 ticks** (≈166 ms, real time) and a newer command replaces it.
- **Acceptance cue.** Every accepted tap shows a response within **1 tick**, either the move starting or a small flash on the hero's hand or foot meaning "queued". No tap vanishes silently.
- **Cancel rules:**
  - string moves cancel into the next string move **on hit**, from the last active frame
  - **evade** cancels any recovery after its first **8** frames, so there is always a readable way out
  - startup frames are never cancelled
- **Haptics.** Light tick on a hit, medium on stagger, heavy on a takedown or finisher.

### B4. Targeting and the intent glyph
- **Same floor only** for melee. The target is the nearest enemy on the tapped side.
- **Sticky lock.** Keep the current target until it dies or is downed, or until the player taps the other side. There is no switching for "telegraphing" bias.
- **Target marker.** A thin chalk underline under the target, styled by band (solid = Strike, dashed = Lunge, arrow = Run).
- **Intent glyph.** A tiny chalk icon over the target saying what *this* tap will do when it's not the plain string: ✋ grab, ↺ takedown, ✕ counter, ⊙ headshot, ⇩ finisher, ⟲ scissors. It uses the existing chalk-mark hint system (`HintDirector.cs`). This is what makes context moves **predictable**.

### B5. Rhythm: strike → stagger → takedown → finish
The core loop of gun-fu choreography, and what replaces the random rolls:
1. **Poise.** Every enemy has a poise meter. Hits remove poise; at 0 the enemy is **STAGGERED** for a fixed **45 frames**: a visible wobble, guard dropped, ↺ glyph shown.
2. **Takedown.** A takedown is only available on a STAGGERED or telegraphing (counter) enemy. **Which** takedown fires is chosen from a fixed priority table by the environment (B6.3), never by chance.
3. **Finish.** A downed enemy takes a finisher (stomp, stab, headshot) while downed, or gets up after a fixed **70 frames**.
4. **The hero gets hurt too.** Being hit means a flinch (light) or a stagger of 1–2 real steps (heavy), never a slide. An evade cancel is available from frame 8.

### B6. Move list with frame data
All values are at 60 Hz. "Lunge" is maximum root motion before warping.

#### B6.1 Fists

| Move | Input | Band | Startup | Active | Recovery | Lunge | On hit |
|---|---|---|---|---|---|---|---|
| Jab | high #1 | Strike | 5 | 2 | 10 | 0.30 m | flinch 12f, poise −10 |
| Cross | high #2 | Strike | 7 | 2 | 12 | 0.45 m | flinch 16f, poise −15 |
| Lead hook | high #3 | Strike | 8 | 3 | 13 | 0.25 m | head snap 18f, poise −20 |
| Rear uppercut | high #4 | Strike | 9 | 3 | 16 | 0.20 m | **STAGGER** |
| Teep (push kick) | low #1 | Strike | 10 | 3 | 15 | 0.50 m | push 1.2 m, poise −15 |
| Low kick (thigh) | low #2 | Strike | 9 | 3 | 14 | 0.30 m | leg buckle, poise −20 |
| Body roundhouse | low #3 | Strike | 12 | 3 | 18 | 0.35 m | **STAGGER** |
| Spinning back kick | low #4 | Strike | 14 | 3 | 22 | 0.40 m | knockdown |
| Clinch elbow | high | Clinch | 6 | 2 | 12 | 0 | poise −20 |
| Clinch knee | low | Clinch | 7 | 3 | 14 | 0 | **STAGGER** |
| Step-in variants | any | Lunge | +6 | = | = | 1.4 m | as the base move |
| Running superman punch | tap on arrival | Run | 6 | 3 | 18 | from momentum | knockdown |
| Running jump knee | swipe toward | Run/Lunge | 8 | 4 | 20 | 2.5 m | **STAGGER** + knockdown |
| Slide kick | low swipe toward | Lunge | 6 | 8 | 16 | 3.0 m | knockdown, passes under |
| Counter | tap in counter window | Strike | 3 | 3 | 14 | 0.40 m | **STAGGER**, guaranteed |

#### B6.2 Gun-fu (pistol, held close to the chest)
- **Stance.** Pistol held close in, elbows bent about 90°, gun at chest height. It is never at full arm's length in melee, so the grapple-and-shoot reads.
- **High tap** = a **double tap** to the chest (2 rounds, 4f + 4f, 12f recovery). Poise −35.
- **The third high tap on the same STAGGERED target** = **headshot** (kill).
- **Hold on a target side** (≥ 12 frames, still) = aim. An aim line converges over 12 frames, and release = **headshot** at any range up to 14 m on the same floor. Releasing early = body shots.
- **Low tap** = a leg shot. The enemy drops to a knee (poise to 0), ready for a headshot.
- **Clinch band with a gun:**
  - high = **arm drag + point-blank body shots**
  - low = **knee + shot**
  - STAGGERED = **grapple-throw + headshot on the floor** (the signature loop: strike → throw → shoot)
- **Shoot behind without turning.** A tap on the side behind the hero while he holds a target in front.
- **Ammo is diegetic:**
  - small tick marks along the gun hand (12 rounds)
  - empty: a swipe down **throws the gun** (stun 40f)
  - auto-reload when no enemy is within 2.5 m (a 36-frame animation, vulnerable)
- **Disarm.** Counter a gunner's telegraph in the Clinch or Strike band to strip the gun; the hero keeps it.
- **Headshots are never random.** They come from: aim-hold, the third tap on a STAGGERED enemy, point-blank on a downed enemy, or the counter-disarm follow-up.
- **Armoured enemies** (helmet and vest) ignore body shots. They need a headshot after a stagger, or a melee takedown.

#### B6.3 Takedowns (target STAGGERED or in the counter window, tap high)
The first rule that matches fires:

| Priority | Environment check | Takedown |
|---|---|---|
| 1 | Edge, rail or glass ≤ 2 m behind the enemy | Over-the-edge throw / through-the-glass |
| 2 | Wall ≤ 1.5 m behind | Wall smash (head into the wall), then the enemy drops |
| 3 | Table, desk or crate adjacent | Table slam |
| 4 | Another enemy in line ≤ 3 m behind | Hip throw *into* him (both down) |
| 5 | Hero holding a prop | The prop's takedown (B7) |
| 6 | Hero holding a gun | Arm drag → throw → headshot |
| 7 | Otherwise | Leg reap (osoto-gari) to the floor |

**Tap low** on a STAGGERED enemy = leg sweep (downed).

#### B6.4 Leg-scissor takedown ("jump on him, legs round the neck, throw the body")
**Trigger:** swipe up toward an enemy 1.0–2.4 m away who is STAGGERED or telegraphing. In the air: high tap within 1.9 m. It's deterministic: the ⟲ glyph shows whenever it's available.

| Frames | Phase | Hero | Victim |
|---|---|---|---|
| 0–8 | Plant | Skip step; lead foot planted (foot IK lock) | Reaches forward (telegraph frozen) |
| 8–18 | Climb | Jumps; hips arc up to the victim's shoulder height +0.2 m; hands on the head (hand IK) | Head pulled down 10° |
| 18–26 | Wrap | Thighs close on the neck; the victim's neck socket is pinned to the hero's crotch socket | Hands go up to the hero's legs |
| 26–44 | Swing | Hero swings under and around the victim, torso inverted, hands reaching for the floor | Pulled off balance, feet leave the floor at f36 |
| 44–52 | Throw | Hero releases. The victim flies along the **throw vector**: toward another enemy (bowling), an edge or glass (fall), a wall (splat), otherwise to the floor | Ballistic arc, then ragdoll on impact |
| 52–70 | Recover | Hero lands in a handstand, rolls out to a crouch, then back to guard | Downed (70f) |

- Hero is invulnerable on frames 8–52 and **vulnerable on 52–70**, so the move has a risk.
- Variant at full Rampage: no release. A twisting neck-break, and the body drops limp.

#### B6.5 Weapon throws (swipe down with a weapon and a target on the same floor)
Throws are aim-assisted to the target's head or chest. They fly on a real ballistic arc and hit geometry if it's in the way.

| Thrown | Effect |
|---|---|
| Knife, axe, hatchet | Lethal, sticks in the target |
| Pistol (empty) | Stun 40f, bounces |
| Book, phone, ashtray | Stun 25f, poise −30 |
| Stick, pool cue | Spinning, knockdown |
| Chair, stool | Knockdown; breaks into a chair leg (blunt-long) |
| Bottle | Stun, breaks; leaves glass on the floor |

**Catch-and-return:** a tap toward a knife in flight within 2.4 m catches it (keep; see `IncomingKnife`).

### B7. Props: everyday, natural and relic objects
Every prop is a data asset:
- archetype, mass, grip point, reach
- damage and poise damage
- durability: a fixed number of uses, never random
- what it breaks into
- throw profile, takedown and finisher
- pickup radius
- outline highlight when it's pickable

| Archetype | Everyday | Natural | Relic | Signature moves | Breaks into |
|---|---|---|---|---|---|
| **Stab-short** | **pencil**, pen, fork, screwdriver, glass shard | sharp stone, thorn branch | dagger, arrowhead | stab ×3 string; takedown = neck stab; pin the hand to a table | nothing (pencil: 2 uses) |
| **Blunt-short** | **book** (hardcover), phone, ashtray, stapler, brick, plate | rock, log end | candlestick, idol, bronze bust | strike the head; **block a knife stab** (book = parry tool); takedown = throat chop with the spine of the book, then a slam | bottle → glass shard |
| **Blunt-long** | **stick**/cane, broom, mop, **pool cue**, bat, pipe, crowbar | branch, staff | quarterstaff, mace, flail | thrust, overhead, **leg sweep**; takedown = choke from behind with the stick; trip and pin | pool cue → 2 halves (stab-short + blunt-short) |
| **Bladed-long** | machete, kitchen knife | — | **katana, display sword, halberd, spear** | cuts with cleave (keep the sword set); spear = long thrust that pins | — |
| **Furniture** | chair, stool, table, door, trash-can lid | log, crate | shield, suit of armour | chair = swing, then it breaks into a leg; lid/shield = block projectiles; **shove the armour onto an enemy** | chair → chair leg |
| **Flexible** | belt, necktie, rope, chain, jacket | vine | chain-flail | choke from behind; **jacket over the head** = blind 60f | — |
| **Spray/blind** | fire extinguisher, sand bag | **sand/dirt** (a fistful), snow | incense urn | blind the enemies in a cone for 90f | empty → blunt-short |
| **Environmental** | glass panel, railing, bar counter, shelving | puddle (slip), mud, ice, river edge | **chandelier** (shoot its chain), statue (topple), display cases (smash through), knife cabinet (throwing knives) | kills by placement; all fixed triggers | — |

**Chapters that host them:**
- Warehouse / Racking Aisle (current): pipes, crates, forklift, pallets
- **Library**: books, pencils, ladders, toppling shelves
- **Museum / relic hall**: armour, displays, chandelier, statues
- **Night club / bar**: bottles, glass, pool cues, chairs
- **Construction yard / natural** (rain or night): rocks, sand, rebar, puddles, mud, planks
- **Stables / barn**: pitchfork, rope, hay, horse kick as a hazard
- **Rooftop in rain**: puddles, antenna, fall kills

### B8. Body mechanics (heroes and NPCs)

#### B8.1 Skeleton v2 (replaces the 14 planar bones)
`root` (floor projection, carries root motion) → `pelvis` → `spine_01` → `spine_02` → `chest` → `neck` → `head`.

Arms and legs:
- `clavicle.L/R` → `upperarm` → `forearm` → `hand`, with sockets `grip.L/R`
- `thigh.L/R` → `shin` → `foot` → `toe`

Sockets: `holster`, `victim_neck` (at the crotch, for scissors and chokes), `victim_back` (for the human shield).

#### B8.2 Proportions
Pick a target: **stylized-realistic, 6.5 heads tall, 1.80 m**. That is realistic, yet the head still reads on a phone. Approximate lengths (m):

| Part | Length |
|---|---|
| head | 0.27 |
| neck | 0.09 |
| shoulder width | 0.42 |
| pelvis width | 0.30 |
| upper arm | 0.30 |
| forearm | 0.27 |
| hand | 0.17 |
| thigh | 0.45 |
| shin | 0.43 |
| foot | 0.25 |
| hip height | 0.93 |

- `BodyScale` goes from 0.76 to ≈0.95, so recheck the reach, ledge hang drop and headroom constants.
- Limbs become **tapered capsules** (thicker at the thigh and upper arm) with soft visible joints, so knees and elbows read.

#### B8.3 Joint limits (degrees)

| Joint | Limits |
|---|---|
| Neck | flex 50 / ext 60 / turn ±70 / tilt ±40 |
| Spine (3 segments in total) | flex 80 / ext 30 / **twist ±45** / side ±35 |
| Pelvis yaw vs root | ±60 |
| Shoulder | flex 180 / ext 50 / abduction 180 / rotation ±90 |
| Elbow | 0–145, **never hyperextends** |
| Forearm | pronation and supination ±80 |
| Wrist | ±70 flex, ±20 deviation |
| Hip | flex 120 (130 with pelvis tilt for kicks) / ext 20 / abduction 45 (90 with pelvis tilt for a roundhouse) / rotation ±40 |
| Knee | 0–150, **never hyperextends**, 5–15° bend at rest |
| Ankle | dorsiflexion 20 / plantar flexion 50 |

#### B8.4 How each part should move

| Part | Rules |
|---|---|
| **Legs and knees** | Knees soft, never locked. Knees track over the toes. The **planted foot never slides** (foot IK lock during contact). The pelvis shifts 5–8 cm over the support foot *before* a kick. Front kick: chamber the knee to hip height, then extend. Roundhouse: pivot on the support foot 90–180° and turn the hip over. Steps are real steps: a stagger is 1–2 footfalls, not a slide |
| **Torso** | **Hips start, chest follows 2–4 frames later, arm last** (kinetic chain). Counter-rotation between shoulders and hips. Brace (spine flexes 10°) when hit. Idle breathing ±1.5° chest rotation |
| **Arms** | Punches travel straight down the centreline from the chin. The non-punching hand guards the chin. Elbows stay down. The fist turns over (pronates) in the last third. Retraction is as fast as extension. Hook: elbow at shoulder height, 90° bend. Gun: close to the chest (B6.2) |
| **Forearms and hands** | Pronation sells punches and the gun grip. Hand poses: fist, open blade, grip (per prop size: pinch for a pencil, reverse grip for a stab, two hands on the edge of a book, mid-grip on a stick), point (gun) |
| **Head** | Chin tucked. **Eyes on the target** (look-at IK, ±70°, weight 0.6–0.9). It lags 2–3 frames on fast moves (overlap) and snaps on head hits, its speed driven by the hit |
| **Whole body** | Anticipation (load) → action → follow-through → recover to guard. Arcs, not straight lerps, for hands and feet. Weight is always over a foot or visibly falling |

#### B8.5 Runtime animation stack (Unity)
1. **Root motion** from the `root` bone, applied by the fighter controller with motion warping (B2.3).
2. **Inertialization** blends replace crossfades (the "inertialization" technique from the GDC 2018 *Gears of War* animation talk). Impacts get 2–4 frame transitions with no smearing.
3. **Analytic two-bone IK** in `LateUpdate`, run after the graph is evaluated:
   - feet: lock planted feet, adapt to stairs and crates
   - hands: grabs, scissors, prop grips
   - gun arm: aim at the target
4. **Look-at** for the head and upper spine.
5. **Hit reactions** form a matrix, played **additively** on top of whatever the fighter is doing:

   | Dimension | Values |
   |---|---|
   | Zone | head, torso, legs |
   | Direction | front, back |
   | Strength | light flinch (additive), stagger (full body, steps), knockdown, launch |
   | Special | headshot (limp drop), choke, scissors victim, wall splat |

6. **Ragdoll on death and big knockdowns**, seeded with the bone velocities from the animation. On get-up, pick `getup_front` or `getup_back` from the pelvis orientation, then blend from the ragdoll pose.
7. **Locomotion set:**
   - guard idle with weight shift
   - guard shuffle forward and back
   - run 6.5 m/s and sprint 8.5 m/s, with arm swing
   - start, stop and **180° plant-and-pivot turn** (replaces the eased spin)
   - crouch and land (light and heavy)

#### B8.6 NPC archetypes (distinct bodies and movement)
Each archetype has fixed personality values: aggression, ring distance, telegraph length and poise.

| Archetype | Body | Stance and movement | Telegraph | Poise |
|---|---|---|---|---|
| Brawler | average, heavy shoulders | square stance, plodding steps | 18f light / 30f heavy | 60 |
| Knife fighter | lean | low bladed stance, fast shuffles | 14f | 40 |
| Stick/pipe fighter | average | two-hand guard, wide arcs | 24f | 60 |
| Swordsman | lean, upright | sword high, measured steps | 22f | 50 |
| Gunner | average | gun close in, keeps 4.5–7 m, sidesteps | 36f laser | 45 |
| Armoured tactical | bulky (helmet, vest) | slow, ignores body shots | 30f | 90 |
| Brute (boss) | 1.4× scale | lumbering, ground shakes | 40f / charge 50f | 200 |

**Attack scheduler.** At most **1 melee attacker** at a time (2 on Hard) and 1 ranged. The rest circle at their ring distance. Attackers take turns round-robin by distance, with a fixed **20-tick gap** between attacks. Everything is deterministic from `AiRng`.

**Body separation.** Fighters' capsules push apart, so no two bodies overlap more than 25% of their width.

### B9. Camera and readability
- **Melee framing.** Fighters are **≥ 20% of screen height**, and the camera keeps the fight's bounding box at about 60% of screen width.
- **No hard cuts** mid-exchange. The kill cam (`CameraDirector.KillCam`) plays only on the **last** enemy of a wave, or on a Rampage finisher.
- **Hero visibility.** A 1–2 px white rim or outline on the hero, so black never vanishes into black. Enemy colours are reserved for enemies.
- **Telegraph readability.** A red flash on the attacking limb, plus a sound cue, with the same duration for each archetype every time.

---

## Part C: Build prompts (paste into Claude Code, in order)

Each prompt assumes the session is on this repo. They share these rules (repeat them if a session starts fresh):

```text
Shared rules: Unity 6.6 C# project "stick-wick". Read docs/COMBAT_PROMPT_SHEET.md first; the
section numbers below refer to it. Keep changes focused on the step. After coding, run
tools/compile_check.sh and fix every error. Add a short DEVLOG.md entry. Commit with a clear
message and push to the working branch. Report what changed, what you could not verify (the
editor cannot run here), and exactly how I should test it in the editor.
```

### Phase 0: Measure first

**C0.1 Teleport guard and combat debug overlay**
```text
Implement the teleport guard from B2.6 and a combat debug overlay.
1. Add Core/CombatDebug.cs: each frame, for every Fighter, compare position to last frame; if
   the step exceeds capSpeed*dt*1.5 (caps from B2.2) and the current move is not flagged
   allowsWarp, Debug.LogError with fighter name, state, clip, delta and file/method that moved it
   (add a lightweight "MovedBy(string)" breadcrumb at every transform.position write in
   HeroController, Enemy and Fighter).
2. Overlay (toggled by a 3-finger tap or F1 in editor): per fighter its state name, clip,
   clip frame, poise (stub 0 for now), current target line, distance band, and a red trail
   where violations happened.
3. List every current violation source you find statically (Part A2 items 1-4) in DEVLOG.
Done when: compiles; overlay toggles; walking around without fighting logs zero violations;
the dash/rampage/grab cases log violations (proving the guard works).
```

**C0.2 Deterministic replay recorder**
```text
Add a replay recorder: record the wave seed and every Command with the frame it was consumed;
a "Replay last fight" debug menu item replays it. Hash all fighter positions + health every
second and print the hashes. Replaying twice must give identical hash sequences once Phase 1 is
done; for now just report how far they diverge and why (list the Random calls hit).
Done when: recorder works and the divergence report lists concrete sources.
```

### Phase 1: Determinism and no teleporting

**C1.1 Fixed combat clock and frame data**
```text
Implement B1.2 and B1.3.
- Add Core/CombatClock.cs: 60 Hz accumulator fed by Time.deltaTime (so slow-mo still works),
  raising OnTick(int tick). Move HeroController/Enemy combat Update logic into the tick.
- ClipPlayer: switch the graph to DirectorUpdateMode.Manual and evaluate it from the tick.
- Extend AttackData with integer frames: startup, active, recovery, cancelFrom, lungeMeters,
  poiseDamage, allowsWarp. Fill the fist values from B6.1; derive the others from the current
  clip hit times so nothing plays slower. Hit resolution happens on the first active frame,
  not when body.Time passes hitTime.
Done when: compiles; the same jab always connects on frame 5 (log it); slow-mo still slows.
```

**C1.2 Remove randomness from the hero, add the intent glyph**
```text
Implement B1.1, B1.6, B4 and B5 for the hero.
- Delete every Random call in hero move selection (HeroController: TryHeadScissors chance,
  air scissors 65%, RampageStrike coin flip). Replace with the rules in B5/B6.3/B6.4.
- Add poise to Enemy (B5.1) with a STAGGERED state (45 frames, wobble, guard down).
- Rewrite Strike() as one explicit priority table (B6.3 + B3), documented in a comment block,
  computed by a pure function DecideMove(heroState, target, environment) -> MoveId so it can be
  unit-tested.
- Intent glyph (B4) through HintDirector: show the glyph for whatever DecideMove would return
  for a high tap and a low tap toward the current target, every frame.
- Add EditMode tests (Unity Test Framework) for DecideMove covering each row of B6.3.
Done when: tests pass in the editor; no Random remains in Combat/HeroController.cs except FX.
```

**C1.3 Targeting, distance bands, hero-relative input**
```text
Implement B2.4, B3 and B4.
- GestureInput: side and high/low are decided relative to the hero's on-screen position and
  waist line (fix GestureInput.cs:148 and :199 which use Screen.width*0.5). Add the B3 gestures
  (hold-in-clinch = grab, swipe toward = lunge attack, swipe away = evade, tap on prop =
  pick up). Buffer = 10 ticks, newest wins, acceptance cue within 1 tick.
- PickTarget: same floor only for melee, sticky lock, no telegraph/token bias.
- Bands: Clinch/Strike/Lunge/Run/Out exactly as the B2.4 table; Run band makes the hero
  visibly run (run clip at authored speed) and fires the queued running attack on arrival.
- Target marker styled by band.
Done when: tapping on an enemy always strikes toward that enemy even when the hero is near the
screen edge; no attack ever targets another floor (except the gun).
```

**C1.4 Kill the teleports: root motion, warping, paired alignment**
```text
Implement B2.1-B2.5 and remove every violation the C0.1 guard reports.
- Replace UpdateDash's 20 m/s MoveTowards with: Lunge-band step-in using clip root motion +
  motion warping clamped [0.75,1.30] during startup only; Run band uses the run state.
- Remove the glide MoveTowards in flying/spin/sword-leap; use warped root motion instead.
- RampageStrike: no blink. Replace with a visible dash (<=12 m/s, <=3 m, blur trail) to a
  target in the Lunge band only; farther targets: run.
- Paired moves (grabbed, scissors start/end, giant swing, hold): 6-10 frame alignment phase,
  victim follows an attacker socket; no position snaps (GetGrabbed, EndScissors, ledge climb
  hang->climb should use the climb clip's root motion).
- Until the v2 rig exists (Phase 3), extract root motion procedurally: per clip, a curve of
  forward displacement authored in AttackData.lungeMeters over the startup frames.
Done when: a 3-minute fight logs zero teleport-guard violations.
```

**C1.5 Bodies don't overlap; one attacker at a time**
```text
Implement B8.6's attack scheduler and body separation.
- AttackTokens: Max melee = 1 (2 on Hard), ranged = 1; round-robin by distance; 20-tick gap.
- Capsule separation between all fighters on the same floor (push apart over a few ticks,
  never more than 25% overlap); enemies circle at ring distance while waiting.
- Enemy RNG: replace UnityEngine.Random in Enemy.cs and WaveDirector.cs with a seeded AiRng
  (per wave); cosmetic randomness moves to FxRng. Boss no longer ignores interrupts by chance:
  enraged boss has a fixed super-armour window instead (state it in the telegraph).
- Fixed telegraph durations per archetype from B8.6.
Done when: the C0.2 replay gives identical hashes twice in a row; clip-1-style pile-ups
cannot happen (4 enemies spawned on top of the hero spread out within 0.5 s).
```

**C1.6 Camera readability**
```text
Implement B9: melee framing (fighters >= 20% screen height), no hard cuts mid-exchange (kill
cam only on the last enemy of a wave or Rampage finisher), white rim/outline on the hero,
fixed red telegraph flash on the attacking limb.
Done when: in a 4-enemy fight the hero is always distinguishable and the camera never cuts.
```

### Phase 2: Rig v2 and body realism

**C2.1 Skeleton v2 and proportions**
```text
Rebuild the stickman in tools/blender/build_stickman.py per B8.1-B8.2 (6.5 heads, 1.80 m,
tapered capsule limbs, soft joints, feet with toes, neck, clavicles, wrists, sockets). The
reference images are in docs/reference/characters/ (see Part D). Keep the art style: matte
black hero, flat coloured enemies, round head, small white oval eyes.
- Poses stay angle-based but add a twist channel for pelvis, spine segments, chest, neck and
  forearms (B8.3 limits enforced by clamping when posing).
- Update retarget.py so the CMU mocap maps onto the new bones, including foot/toe and twist.
- Update Fighter.BodyScale and every reach/hang/headroom constant that depends on the body.
- Render docs/previews/rig_v2_turnaround.png and before/after sheets for idle, jab, front_kick,
  roundhouse, run.
Done when: FBX rebuilds; previews show knees, elbows, feet and hip turn clearly.
```

**C2.2 Re-author the clips with real mechanics**
```text
Using B8.4 as the rules, rework every clip in build_stickman.py / mocap_clips.py:
kinetic chain (hips -> chest 2-4 frames later -> arm), weight shift before kicks, chambered
kicks, pivoting roundhouse, guard hand at the chin, pronation on punches, soft knees, head
lag. Retime to the B6 frame data. Add the missing clips: hook, uppercut, teep, low kick,
clinch elbow, clinch knee, superman punch, running jump knee, evade, roll, stagger (front/back),
flinch additive (head/torso/legs), getup_front, getup_back, 180 pivot turn, run start/stop,
guard shuffle, plus gun-fu clips from B6.2 and the leg-scissor pair from B6.4 (hero and victim
clips authored together on one timeline). Use the pose sheets in docs/reference/moves/.
Done when: preview sheets for every new clip are in docs/previews/; no knee or elbow
hyperextends (add an automated check in the build script that fails on violations).
```

**C2.3 Runtime animation stack**
```text
Implement B8.5 in Unity: root motion from the root bone (feeding C1.4's warping), inertial
blending replacing crossfades in ClipPlayer, analytic two-bone IK for feet (planted-foot lock,
stairs/crates) and hands (grabs, props, scissors), aim IK for the gun arm, head/spine look-at,
additive hit-reaction layer, ragdoll on death/big knockdowns with get-up selection.
Done when: no foot slides during strings (verify with the debug overlay drawing foot
positions), and getting hit mid-punch blends the flinch on top instead of restarting.
```

**C2.4 NPC archetype bodies and behaviour**
```text
Implement B8.6: per-archetype body variants (scale/width/silhouette props: helmet+vest for the
armoured type), stance and locomotion personalities, fixed telegraph lengths, poise values,
ring distances. Add the Armoured tactical enemy (needs headshot after stagger or a takedown).
Done when: each archetype is recognisable by silhouette alone in a black-on-black screenshot.
```

### Phase 3: The move set

**C3.1 Fists, clinch and running attacks**
```text
Wire the full B6.1 table (strings, step-ins, clinch, running superman punch, running jump knee,
slide kick, counter) through DecideMove. Strings reset after 54 frames without input.
Done when: every row is reachable by the B3 gestures and listed in the in-game Moves card.
```

**C3.2 Gun-fu**
```text
Implement B6.2 completely: close-in stance, double tap, third-tap headshot on STAGGERED,
aim-hold headshot with converging line, leg shot, clinch arm-drag shots, grapple-throw +
headshot, shoot behind, diegetic ammo ticks, auto-reload rules, throw empty gun, disarm counter
on gunners, armoured enemy rules. Headshots get their own reaction (limp drop + ragdoll).
Done when: headshots happen only under the four listed conditions (unit-test DecideMove).
```

**C3.3 Takedown table and leg-scissor rebuild**
```text
Implement the B6.3 priority table (edge/glass, wall smash, table slam, hip throw into an
enemy, prop takedown, gun takedown, leg reap) and rebuild the head scissors per B6.4 frame by
frame with the alignment phase, IK hands/feet, victim socket, throw vector chosen by the
environment, invulnerable 8-52 and vulnerable 52-70. Remove the old normalized-t scissors code.
Done when: the scissors look identical every time from the same spot, and the thrown body
lands where the glyph's throw-arrow pointed.
```

**C3.4 Weapon throws and catches**
```text
Implement B6.5 for every weapon/prop type: swipe down with a target, aim-assist to head/chest,
ballistic arc, geometry blocks, effects per the table, catch-and-return knives.
```

### Phase 4: Props and environments

**C4.1 Prop framework**
```text
Create Props/PropData (ScriptableObject) and Props/Prop.cs per B7: archetype, mass, grip,
reach, damage, poise damage, durability (fixed uses), breaks-into, throw profile, takedown and
finisher move ids, pickup radius, outline when pickable. Migrate knife, pipe, sword, gun to it.
Pickup = tap on a highlighted prop (B3); swapping drops/throws the current one.
```

**C4.2 Improvised weapons: pencil, book, stick first**
```text
Add pencil (stab-short, 2 uses, neck-stab takedown, hand-pin onto a table), hardcover book
(blunt-short, blocks a knife stab as a parry, throat-chop + slam takedown, throwable stun),
stick/cane (blunt-long, thrust/overhead/leg sweep, choke-from-behind takedown). Then pool cue
(breaks into two halves), bottle (breaks into a shard), chair (breaks into a chair leg),
jacket (blind 60f), fire extinguisher (blind cone 90f). Author the grip poses and clips.
```

**C4.3 Natural and relic sets plus new chapters**
```text
Add the natural set (rock, branch, sand/dirt blind, puddle slip, mud) and relic set (armour
with a halberd to pull out and armour to shove, display sword, shield, mace, chandelier on a
chain to shoot down, toppling statue, smashable display cases, knife cabinet of throwing
knives). Build two new chapters in EnvironmentBuilder/TowerBuilder using the concept frames in
docs/reference/environments/: the Library and the Relic Hall, keeping the noir B&W look.
All environmental kills are fixed triggers with a visible glyph.
```

### Phase 5: Feel and tuning

**C5.1 Feel pass**
```text
Per-move hit-stop (frames from AttackData), shake scaled by poise damage, haptics (B3),
consistent SFX per hit class, a short 3-frame impact freeze on STAGGER, slow-mo only on
takedowns/finishers (never on plain strikes, so timing stays learnable).
```

**C5.2 Training reel update**
```text
Update TutorialDirector lessons for the new grammar: hero-relative tap high/low, distance
bands, stagger -> takedown, intent glyphs, gun-fu loop, scissors, props, throws.
```

---

## Part D: Images and references (ChatGPT and Gemini)

### D1. Do you need to generate images?
**Not to make the game run.** The fighters are generated in code (`tools/blender/build_stickman.py`) and the levels are built in code (`EnvironmentBuilder`, `TowerBuilder`). Images are a **design aid** that makes Phase 2 and Phase 4 much better. Generate these, in priority order:

| # | Set | Needed before | Best tool |
|---|---|---|---|
| 1 | Hero model sheet (turnaround and proportions) | C2.1 | ChatGPT (follows layout and labels well) |
| 2 | Enemy archetype line-up (7 silhouettes) | C2.4 | ChatGPT |
| 3 | Key-pose storyboards for the signature moves | C2.2, C3.x | ChatGPT for multi-panel sheets; Gemini to fix or iterate one panel while keeping the character consistent |
| 4 | Prop sheet with grip points | C4.x | ChatGPT |
| 5 | Environment mood frames (Library, Relic Hall, Club, Construction yard, Stables, Rooftop) | C4.3 | Gemini or ChatGPT |
| 6 | Store key art and icon | launch | either |

**For motion, real footage beats generated images.** The best reference for realistic legs, knees and torso is:
1. **Film yourself.** Side view, phone at hip height, 60 fps, full body, plain wall. Perform each move slowly, then at speed, with a broom handle as the stick and a book as the book. Upload the clips; I can extract key poses frame by frame (as I did with your gameplay clips).
2. **Mocap libraries.** CMU (already used) and Mixamo (free; check its licence terms for your use). Video-to-mocap services such as Rokoko Vision, DeepMotion or Move.ai can turn your phone clips into BVH. Check each one's current pricing and licence.
3. **AI video** (Gemini's Veo, or ChatGPT's video model) is useful only for **timing and staging previz**, not for joint accuracy. Generated anatomy drifts.

**Where to put the results** (commit them, then run the matching Part C prompt):

| Folder | What goes in it |
|---|---|
| `docs/reference/characters/` | hero sheet, enemy line-up |
| `docs/reference/moves/<move_name>/` | pose panels, your own footage |
| `docs/reference/props/` | prop sheet |
| `docs/reference/environments/<chapter>/` | mood frames |

### D2. Shared style block (paste at the start of every image prompt)
```text
STYLE: 2.5D side-view action game concept art, orthographic side view, clean flat shapes.
Film-noir black-and-white world: grey architectural concrete, deep blacks, white lamplight,
haze, light rain streaks, high contrast. Characters are smooth faceless stick-figure
mannequins: tapered capsule limbs, visible rounded knee and elbow joints, small feet, round
head with two small white oval eyes, no clothes detail, no mouth. The hero is solid matte
black with a thin white rim light. Enemies are flat saturated single colours (orange, purple,
green, yellow, red). Only the fighters and ink splashes are in colour. Original characters,
no real person's likeness, no film logos, no text unless asked.
```

### D3. Prompts

**D3.1 Hero model sheet** (ChatGPT)
```text
[STYLE block]
Character model sheet of the hero on a plain light-grey background with a faint 10 cm grid.
Stylized-realistic proportions, 6.5 heads tall, 1.80 m: broad shoulders, narrow waist, long
legs. Five views in a row at the same scale: front, 3/4 front, side (facing right), 3/4 back,
back. Below them: the side view repeated with joint markers (neck, shoulders, elbows, wrists,
hips, knees, ankles, toes) as small white circles, and a height ruler in heads. Neutral
A-pose for the turnaround, plus one fighting-guard pose (side view): chin tucked, lead hand at
chin height, rear hand at the jaw, elbows down, knees soft, weight 60% on the rear foot.
```

**D3.2 Enemy line-up** (ChatGPT)
```text
[STYLE block]
Line-up of 7 enemy archetypes side by side, side view facing left, same ground line, plain
light-grey background, height ruler on the left. Each in its own flat colour and fighting
stance: 1 Brawler (heavy shoulders, square stance), 2 Knife fighter (lean, low bladed stance,
knife reverse grip), 3 Stick fighter (two-hand staff guard), 4 Swordsman (upright, sword high),
5 Gunner (pistol held close to the chest, elbows bent), 6 Armoured tactical (helmet and vest
silhouettes, bulky), 7 Brute boss (1.4x size, lumbering). Each must be recognisable by
silhouette alone. Small caption under each with its name.
```

**D3.3 Key-pose storyboards** (ChatGPT; one sheet per move, then fix single panels in Gemini)

Template:
```text
[STYLE block]
Six-panel storyboard sheet, 3x2 grid, each panel a side view of the same two characters at the
same scale and camera, white panel numbers 1-6 in the corner. Hero = matte black, enemy =
[colour]. Show anatomically correct joints: soft knees, no hyperextension, hips leading the
shoulders, planted feet clearly on the floor line. MOVE: [move description and the six beats].
```

Beats to drop in:

| Move | Panels 1 → 6 |
|---|---|
| Leg-scissor takedown | 1 hero plants lead foot, enemy staggered · 2 hero leaps, hands on enemy's head · 3 thighs locked round enemy's neck · 4 hero swings under, body inverted, hands reaching for the floor · 5 enemy flipped over, flying · 6 hero in a low crouch, enemy slammed on the floor |
| Pencil takedown | 1 enemy swings a punch · 2 hero parries the forearm with his left · 3 hero's right hand drives a pencil (reverse grip) toward the enemy's neck · 4 hero steps through and turns him · 5 enemy collapsing · 6 hero back in guard, pencil gone |
| Book takedown | 1 enemy stabs with a knife · 2 hero blocks the blade with a hardcover book · 3 hero strikes the throat with the spine of the book · 4 two-hand book strike to the head · 5 enemy dropping · 6 hero in guard holding the book |
| Stick fight | 1 thrust to the stomach · 2 overhead strike · 3 low leg sweep, enemy airborne · 4 enemy falling flat · 5 hero steps behind a second enemy and chokes him with the stick across the throat · 6 drops him |
| Gun-fu loop | 1 hero traps the enemy's arm (arm drag), pistol close to his own chest · 2 two body shots at point blank · 3 hip throw, enemy over the hero's hip · 4 enemy on his back · 5 hero aims down · 6 headshot finish (no gore, ink splash only) |
| Weapon throw | 1 empty pistol, slide locked back · 2 wind-up, weight on the rear foot · 3 release, hips turned through · 4 gun spinning through the air · 5 hits the enemy's head, his head snaps back · 6 hero already moving forward |
| Running attacks | 1 sprinting (correct arm swing) · 2 jump knee take-off · 3 knee into the enemy's chest · 4 landing · 5 slide kick under a second enemy · 6 enemy falling forward over the hero |

**D3.4 Prop sheet** (ChatGPT)
```text
[STYLE block, but props in neutral greys on white]
Prop reference sheet, side views at real scale against a 10 cm grid, each with a small red dot
at the grip point and an arrow for the striking end: pencil, pen, hardcover book, phone,
ashtray, glass bottle and broken bottle neck, cane/stick, broom, pool cue and its two broken
halves, baseball bat, steel pipe, crowbar, chair and broken chair leg, jacket, fire
extinguisher, rock, tree branch, handful of sand, halberd, display sword, round shield,
mace, candlestick, chandelier on a chain. Small caption under each.
```

**D3.5 Environment mood frames** (Gemini or ChatGPT; one per chapter)
```text
[STYLE block]
Wide side-view game level concept, 21:9, two floors connected by a staircase on the right and a
ledge on the left, a fighting floor running the full width. LOCATION: [Library: tall shelves,
ladders, reading tables, scattered books and pencils / Relic Hall: suits of armour, glass
display cases, weapon racks, a chandelier on a chain, statues / Night club: bar counter,
bottles, pool table, glass partitions / Construction yard at night in rain: rebar, sand piles,
puddles, planks, rocks / Stables: stalls, hay bales, pitchforks, ropes / Rooftop in rain:
water tanks, antennas, puddles, a drop to the street]. Highlight interactive objects with a
thin white outline. No characters.
```

**D3.6 Optional motion previz** (Gemini Veo or ChatGPT video; timing only)
```text
Side-view, locked camera, 5 seconds, plain grey studio, two faceless black and orange
mannequins, realistic martial-arts timing: [move]. Real-time speed, full bodies in frame,
feet visible. No cuts.
```

---

## Part E: Test and tuning checklist

**Determinism**
- [ ] Replaying the same seed and inputs twice gives identical state hashes (C0.2).
- [ ] No `UnityEngine.Random` in combat logic. `grep -n "Random\." Assets/Scripts/Combat` shows only `FxRng` or cosmetics.
- [ ] The same jab connects on the same frame 20 times in a row, including under slow-mo.

**No teleports**
- [ ] A 3-minute fight with 6+ enemies produces zero teleport-guard violations.
- [ ] Every paired move starts with a visible alignment.

**Input**
- [ ] With the hero at the far left of the screen, tapping on an enemy to his right always strikes right.
- [ ] Every row of the B6 tables is reachable, and the intent glyph matches what fires 100% of the time.
- [ ] Every accepted tap gives visible feedback within 1 tick.

**Body**
- [ ] No knee or elbow hyperextension in any clip (the build-script check passes).
- [ ] Planted feet never slide during strings (overlay foot markers stay put).
- [ ] Each archetype is recognisable from its silhouette alone.

**Readability**
- [ ] Fighters are ≥ 20% of screen height in melee; the black hero is visible on black sets; no pile-ups.

**Playtest questions** (5 players, 10 minutes each, no explanation beyond the Training Reel)
1. "Did the hero ever do something you didn't ask for?" The target is no.
2. "Could you tell what a tap would do before you tapped?"
3. "Which move felt fake?" Fix the top 3 in a C2.2-style animation pass.
