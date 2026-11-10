# Combat 3.0: "Touch is the fist" (design, for review)

Builds on `combat_v2.md`, which made the fighting grounded: human speeds, short heavy jumps, weight, breath,
strings, clinch, throws. This document is about **the hands**: how a thumb on glass becomes a punch, a
cut or a block, so that the player feels they are doing it, not ordering it.

---

## 1. The idea in one line

> **Where your finger lands says *what* you do. How it moves says *how*. When it lands says *how well*.**

A gamepad has buttons that mean nothing until you learn them. A touch screen has the scene itself: you
can touch *the man*, touch *yourself*, touch *the floor*, and drag your finger along the line a blade
should travel. No controller can do that. The whole system is built on that difference.

### WHERE: three zones, read from the picture (no buttons)

| Your finger starts on... | Meaning | Why it feels right |
|---|---|---|
| **an enemy** (his body, a generous margin around it) | **Attack him**: hit, cut, grab, aim | You touch the man you want to hit. Targeting is free: no lock button. |
| **the hero** (a band around him, about a fifth of the screen wide) | **Defend yourself**: guard, slip, duck, hop | You touch yourself to cover yourself. |
| **empty ground** | **Move there** | You point where you want to go. |

### HOW: four touches, each with one physical meaning

| Touch | Physical meaning | On an enemy | On the hero | On the ground |
|---|---|---|---|---|
| **Tap** | a quick, short action | a fast strike, **aimed at the height you touched**: head, body or legs | (nothing: avoids accidents) | step there |
| **Hold** | sustained effort | **grab him** (from a distance: walk in and take hold) | **guard up** | walk there; further = run |
| **Swipe** | motion through space | a **swinging strike along your finger's path** (hook, roundhouse, a sword cut on that exact line) | **evade** in that direction: back = step out, down = duck, up = hop, in = step inside his attack | a jump or vault that way |
| **Pull & release** (touch the enemy, drag back towards the hero, let go) | winding up | a **heavy blow**. The hero's body coils as you drag, in real time; how far you pull is how hard he hits. | | |

### WHEN: timing is the skill

Every enemy attack **telegraphs** as a short, readable wind-up. The striking limb flashes:
- **white** means you can block it;
- **red** means you can't (a blade against bare arms, a sledge, a grab or a tackle), so you must evade.

Its height shows what it is: a head-high arm, a body-level blow or a leg at the ankles.

Defend in the last **0.2 s** before it lands (a perfect guard or a perfect evade) and time slows. The man
is open, and for **0.6 s** your next touch on him is a **COUNTER**: a special move chosen by your weapon
and by how you defended (§4).

**That gives four touches times three places: about ten things to learn in total**, and every weapon
reuses them. The weapon in your hand changes what each touch does, not what you have to remember.

---

## 2. What I am NOT using, and why (debate)

| Proposed | Verdict | Reason |
|---|---|---|
| "Use every kind of touch event" | **No: one grammar instead.** | More gestures mean slower recognition (the game must wait to tell one from another), more mistakes mid-fight, and a game only experts enjoy. Four touches in three places already give dozens of moves, because *context* multiplies them: weapon, distance, his state, the setting. |
| Pinch to grab | **No.** | You fight holding the phone in both hands, landscape; pinching needs a free hand. *Hold on him* is a grab anyone can do with a thumb. |
| Two-finger gestures | **Only one, optional:** a two-thumb hold on both sides at once, to brace against two attackers (late chapters). | Two-thumb chords are hard to hit reliably under stress. |
| Tilt (gyro) to aim the gun | **No.** | It moves the camera you're looking at and fights the player's posture. Aim is your finger on the target. |
| 3D Touch / pressure | **No.** | Gone from most phones. |
| Tap left or right half to strike (the old scheme) | **Retire it.** | "Touch the man" is more direct and solves targeting when they surround you. |
| **Haptics** | **Yes, everywhere.** | The phone in your hands is the one thing a player physically feels. Each event gets its own pulse: a block is a dull thud, a parry a sharp tick, a counter a double knock, a heavy hit landing one strong jolt, and taking a cut a long buzz. This is the cheapest big step towards "it feels real". |

---

## 3. The body: making every move feel real

1. **The drag *is* the wind-up.** During a pull and release, his shoulder turns, his weight sinks onto the
   back foot and the arm or weapon draws back, all following your finger live. Let go and he unloads
   along the line you pulled. Release early for a quick, weak shot; full pull for a big one. You can
   hold a wind-up (breath drains), and a fast man may hit you out of it: real risk.
2. **Swipes leave a path the body follows.** The fist, foot or blade travels the arc you drew. A cut
   drawn diagonally down-left *is* a diagonal cut down-left. Arms are posed by two-bone reaches (as with
   the walk-out's hands), on top of the clip's legs and hips.
3. **Hit where you touch.** A tap is matched to his body, not the screen: on his head, a jab or cross;
   on his chest, a body shot or knee; on his legs, a low kick or a cut to the thigh. His reaction matches
   where he was hit: the head snaps, the body folds, the leg buckles.
4. **Weight and follow-through.** A heavy blow that misses carries him round; he recovers a step later.
   A blocked blow jars back up his arm. Feet plant, with no sliding: steps happen on the ground.
5. **Breath, without a bar.** A flurry, a long guard or a held wind-up all cost breath. You see it in his
   body: a lower stance, shoulders heaving, heavier breathing. Blows slow and a guard can be broken.
   Stop for a moment and it comes back. The enemies breathe too.
6. **The camera breathes with the fight**: a tiny push in on a counter, a slow-motion beat on a perfect
   defence, a hard cut-in on a finisher. All short; never taking the fight away from the player.

---

## 4. Defence and counters

All defence starts **on the hero**, so it never collides with attacking.

| Defence | Touch | Beats | Fails against | Perfect (last 0.2 s) gives |
|---|---|---|---|---|
| **Guard** | hold on the hero | white attacks; chip damage only | red attacks; a long guard drains breath; heavy blows break it | **Parry**: his arm or blade is knocked aside |
| **Step out** | swipe from the hero, away from him | anything, if you have room | a cornered or second attacker | **Whiff**: he overreaches and stumbles past |
| **Duck** | swipe down from the hero | head-high attacks (punches, horizontal cuts) | low and mid attacks | **Under**: you're inside, low |
| **Hop** | swipe up from the hero | sweeps, low cuts, leg kicks | high attacks | **Over**: you land on his flank |
| **Step inside** | swipe from the hero, towards him | long attacks (a swing, a pole, a chain), by getting inside their reach | quick jabs | **Inside**: chest to chest, his arm trapped |
| **Stop-hit** | tap him *early* in a slow wind-up | slow heavy attacks | quick attacks (you trade blows) | his attack is cancelled; a short stagger |

**The counter window.** After a perfect defence, time drops to a third for 0.6 s and he is outlined.
Your next touch on him performs a counter that depends on how you defended and on your weapon (see the
weapon tables in §5). If you miss the window, you just carry on fighting. A counter always ends in
something clean and visible: he's down, disarmed, thrown into the wall, or cut.

**Disarms** follow realistic technique: catch the wrist, twist, strip. They come from a perfect guard or
step-inside against an armed man, then a **hold** on him (catch) and a **swipe away** (strip). His weapon
becomes yours. This is how the hero moves up through the arsenal during a fight.

---

## 5. The four weapons (and bare hands)

The same grammar throughout; the verbs change meaning. Reach and speed differ realistically:
- **knife:** short and fast;
- **rod:** long and slow, uses both hands;
- **sword:** long and fast;
- **gun:** far, with a limited magazine.

### 5.0 Bare hands (his home: he was taught to kill with them)

| Touch | Move |
|---|---|
| Tap: head / body / legs | jab and cross / body hook or knee if close / low kick. Taps in rhythm chain into the named strings (combat_v2), with the target height of each tap picking the next blow. |
| Swipe across him | hook or spinning backfist (high), roundhouse (mid), sweep (low) |
| Swipe up through him | uppercut; if close, a rising knee |
| Swipe down on him | hammer fist; a stomp if he is on the floor |
| Pull & release | a heavy straight right; from far, a running superman punch |
| Hold on him | **clinch** (combat_v2's throws: knees, hip throw, shoulder throw, suplex, DDT) |
| Hold, then swipe towards a wall, table, rail, car or glass | **slam him into it** (the setting as a weapon, §6) |
| Counters | parry → arm trap and elbow · under → liver shot and uppercut · inside → headbutt and hip throw · whiff → rear naked choke · over → knee to the ribs |

### 5.1 Knife (Ch1 onwards: the one he grabs off the floor)

Carried in **reverse grip** (blade down along the forearm), the way a professional fights close.

| Touch | Move |
|---|---|
| Tap: head / body / legs | slash across / short stab to the body / cut to the thigh. Quick: three can fit in a second. |
| Swipe across him | a slash along your finger's path; the blade trail follows the swipe exactly |
| Pull & release | a lunging thrust (the longest reach a knife has) |
| Hold on him | grab and stab: a held hand and short stabs (tap); swipe to throw him off |
| Swipe on the ground, *away* from the hero, with no man near | **throw the knife** along the swipe (it's gone until you pick it up) |
| Guard | weak: the forearm and blade spine stop fists and clubs, not blades or heavies |
| Counters | parry → "pass and cut" (the attacking arm) · under → stab to the body · inside → arm pinned, three stabs · whiff → cut the back of his leg |

### 5.2 Rod / pipe (Ch1 Foreman's crowbar, the street, the park)

Two hands, long reach, slow and brutal. It controls space: the man who keeps them at bay.

| Touch | Move |
|---|---|
| Tap: head / body / legs | jabs with the end: straight to the face, gut poke, shin rap. Quick; keeps them off. |
| Swipe across him | a horizontal swing: **hits everyone along the arc** |
| Swipe down on him | an overhead smash (a downed man: a finish) |
| Swipe up through him | a rising strike under the chin |
| Swipe low *towards you* on his legs | **hook his leg** and pull: he goes down |
| Pull & release | a full home-run swing: huge knockback, a crowd-clearer; a long recovery if you miss |
| Hold on him | press him back with the bar across his chest; then swipe towards a wall to pin him, or down to drop him |
| Guard | **the best guard**: held crosswise in two hands, it stops everything except a tackle |
| Counters | parry → **shove** with the bar, then a smash · under → a jab to the ribs, then a sweep · inside → bar across his throat into the wall |

### 5.3 Sword / katana (Ch6: the fortress swordsmen; the duel with Voss's sword-cane)

The weapon the touch screen was made for: **the cut is the line you draw.**

| Touch | Move |
|---|---|
| Tap: head / body / legs | a quick cut on the nearest clean line: down at the head, across the body, low at the legs |
| **Swipe through him** | **the cut follows your swipe's angle**: vertical (the head), a diagonal from either side, horizontal (the waist), or rising from below. Eight clean lines, each its own move. |
| Pull & release | a thrust: the longest, fastest reach in the game |
| **Hold on the hero, then swipe through him** | a **quick-draw**: the blade drops to the hip, he waits (holding = stillness), and the swipe is a lightning cut. The cinematic duel move. |
| Hold on him | a bind: blade on blade, then swipe to wrench his blade aside |
| Guard | stops blades and clubs; a perfect guard is a ringing parry with sparks |
| Counters | parry → the return cut (your swipe) · under → a rising cut · whiff → a cut across his back · blade bind → disarm |

### 5.4 Gun (Ch6: Voss's silver pistol, taken off him; rare and heavy on purpose)

The hero is a former professional; guns are serious, loud and few. Ammo is limited and shown as rounds
in a small arc beside him. It is not a run-and-gun.

| Touch | Move |
|---|---|
| **Hold on a man** | **aim**: the gun comes up, a fine aim line wavers and **steadies the longer you hold** (realistic sway); breath drains slowly |
| **Release** | **fire**: a head-height hold that has steadied is a clean shot; released early, it may miss |
| Tap him | a snap shot from the hip: fast, inaccurate past a few metres |
| Tap him when he is at arm's length | **pistol-whip** (you never fire point-blank by accident) |
| Swipe down from the hero | **reload**: drop the magazine, slap in a new one; only if you have one |
| Guard | none: a gun can't block. Evade. |
| Counters | step inside → gun to his ribs, close · whiff → a shot in his back (one round) |

**Against a gun** (Voss phase II, and gunmen in the fortress): his aim is shown as a thin red line; a
perfect evade means the shot misses. Step inside, catch (hold on him) and swipe away to **take it off
him**. This is the disarm the story already uses.

---

## 6. Moving

| Touch | Movement |
|---|---|
| Hold on the ground | walks towards your finger; **further away means faster** (walk, jog, run); slide the finger to steer; let go to stop |
| Tap on the ground | a single step there (fine positioning) |
| Swipe on the ground | a short hop that way; with something there: **vault** (a crate, a car bonnet), **climb** (a ledge), **drop** (off an edge) |
| Hold far ahead, then swipe at a man in your path | **run attacks**: a spear tackle (straight), a flying knee (swipe up), a baseball slide into his legs (swipe down) |

His stance changes on its own, with no extra input:
- **enemies near:** a fighting stance, shuffling and facing the closest threat;
- **clear:** a natural walk or run;
- **hurt or out of breath:** a limp and laboured run.

On wet stone (the fortress) he slides a little further, and so do they.

---

## 7. The setting is a weapon (by chapter)

With a man held, **swipe towards an object** to use it; walk past a usable thing and **hold on it** to
pick it up. Each chapter has its own furniture, and each one teaches one new idea.

| Chapter | Place | New idea taught | Setting moves | Boss |
|---|---|---|---|---|
| 1 · The Warehouse | racking, chains, crates, the crane | **hands, guard, the knife** (his body remembers before he does: each first use of a move flashes a memory) | slam into racking · chain across his throat · kick a crate into his knees · the crane hook | **The Foreman** (crowbar). Teaches **red attacks**: evade them. Disarm him: the hero gets the **rod**. |
| 2 · The Bank / The Street | marble counter, revolving door, cars, lamp posts | **counters** | head into the counter · trap him in the revolving door · bonnet slide into a flying knee · wrap him round a lamp post | **The Driver** (knife): **knife against knife**, disarms |
| 3 · Home | kitchen, the chair, stairs, doorframes | **rage**: close quarters, improvised weapons (kitchen knife, frying pan as a rod) | slam into the worktop · down the stairs · through the doorframe | **The Cleaner** (scalpel, fast): **parries** under pressure |
| 4 · The Backyard | swings, slide, the pond bridge, bins, trees | **crowds** (rod sweeps, using space) | swing chain wrapped round a man · over the bridge rail into the pond · into the bins | **The Handler** (boxer, brass knuckles): a pure **boxing duel**: guard, slip, counter |
| 6 · The Fortress | rain, braziers, chandeliers, battlements, portcullis | **everything, plus the sword** | over the battlements · into a brazier · under the falling portcullis | **The Warden** (chain: long reach, **step inside**) · **Voss** (sword-cane duel with **blade parries**, then the **gun** phase and its **disarm**) |

---

## 8. Teaching it (no tutorial wall)

- He has lost his memory, so **learning is the story**. Each new move appears when the moment needs it,
  with a single ghost-finger hint *drawn on the scene*: a dotted swipe through the enemy, a pulsing hold
  on the hero. Then "his body remembers": a flash of a still from his past, and the move is his.
- **One idea per fight**, as in the chapter table. Never two new things at once.
- The pause card's **MOVES** page shows the grammar (where × how × when), not a list of 60 moves.

---

## 9. What changes in the code (for the implementation, later)

1. **TouchGrammar** (new, replacing the gesture classifier for story mode). Each touch is sorted by
   where it started (enemy, hero or ground, using screen projections of the bodies), what it is (tap,
   hold, swipe or pull and release), its path and speed, and how it is timed against the nearest
   telegraph. Taps fire on release (no waiting); holds at 0.2 s; swipes by speed; pull and release by
   starting on an enemy and moving back towards the hero.
2. **Telegraph and perfect windows** in `Enemy`: an attack's colour, height and impact time are exposed,
   so defence can be judged and the flash drawn.
3. **Counter state** in the hero: time scale, outline, the 0.6 s window, and the counter table.
4. **Procedural arm and weapon paths**: swipe paths turned into hand and blade IK targets (the walk-out's
   PuppetRig, grown up); the live wind-up coil driven by the drag.
5. **Weapon profiles** (knife, rod, sword, gun): each touch → move, reach, speed, damage, block rules.
6. **Breath** shown in the body (stance and animation speed), as designed in combat_v2.
7. **Haptic patterns** per event.
8. **The setting**: tagged objects (slam targets, pick-ups) per set, and the slam moves.
9. **New clips**: guard walk, duck, hop-over, step inside, the counters, the knife (reverse grip), rod,
   sword cut lines and quick-draw, gun aim and reload, the disarms, and the slams.

**Order of work** (each step is playable on its own):
1. TouchGrammar, bare hands, the three zones and movement.
2. Telegraphs, defence and counters.
3. Knife.
4. Rod.
5. The setting moves.
6. Sword.
7. Gun.
8. The chapter-by-chapter fights, bosses and teaching beats, starting with chapter 1.

---

## 10. Open questions for you

1. **Retire the old tap-left/right scheme in story mode** and use only "touch the man / yourself / the
   ground"? (Recommended. Fight Mode in the tower can keep the old scheme.)
2. **Breath**: shown only in his body (recommended), or also a small ring under him?
3. **Gun**: rare and limited (recommended), or a weapon he can keep for longer stretches?
4. **Failure**: should a fight be lost to a run of failed defences (a restart at the last zone), or should
   the story never fail you? (Recommended: you can be beaten, with a quick retry.)
