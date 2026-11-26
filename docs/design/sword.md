# The katana, v2

Code: `HeroKatanaV4.cs` (the cuts, the thrust, the draw, the pass, the mirror parry) and `HeroSwordV4.cs` (v2's additions). The swordsman's side is in `EnemyKatana.cs`. Clips: `tools/blender/v4_katana.py`.

## The identity

| | The pipe | The katana |
|---|---|---|
| Idea | Momentum and wear: keep it swinging | **Precision and lethality**: one clean line at the right moment ends a man |
| Reward | Chains (flow ×4, figure eight) | Timing and line quality (debana, clean cut, in one breath) |
| Defence | Intercept: hit the arm that hits you | Debana (cut as he decides), the mirror parry, the draw as a counter |

## What v1 already did well (kept)

- **Every swipe is a cut along its own line**, drawn anywhere: ↘ kesa, ↗ gyaku, ↓ oroshi, ↑ kiriage, → yoko. ← cuts a man behind (ushiro); with no one behind, it steps out of distance. A tap is the thrust (high: the throat, low: the belly).
- **The mirror parry** against swordsmen's red lines: within 20° is PERFECT, within 45° a parry, then a free return cut (kaeshi).
- **Combos:** ↓ then ↑ is tsubame-gaeshi; ↘ then ↗ is jūji.
- **The draw:** hold on the hero to sheathe (the world slows), drag out and let go for the draw cut, or THE PASS through two or three men in a line.
- **Swordsmen read you:** use the same line three times and they parry the third.
- **Cut down:** a killing cut is a stillness, then the knees go; the flick (chiburi) cleans the blade.

## What was missing

- **Against ordinary men there was no sword skill**, only cutting and stepping back. The parry works only on swordsmen.
- **Nothing rewarded a good cut over a bad one.** A sloppy curved swipe cut exactly like a perfect one.
- **The Kata of Three paid ×2 for any three quick cuts.** Mashing the same line earned it, and the same line is exactly what swordsmen are meant to punish.
- **Swipes made mid-cut were lost** if they came before the blade landed.
- **One way to fall:** every man cut down fell on his face, whatever the line.

## v2

### Hasuji: the edge

A cut is one line, and the swipe's straightness is the edge's alignment. The input measures how far the stroke bulged off its own straight line (`GestureInput.CutWobble`).

- **Straight and fast** (under 12% wobble, over 1.6 screen-heights a second): **CLEAN**, ×1.25. A brighter splash, the stroke lingers, a cleaner ring. A kill reads **CLEAN CUT**.
- **Wobbly** (over 30%): **THE EDGE TURNED**. It glances (×0.6, a dull clank) and never cuts a man down. Voss's line the first time: "Your edge wandered. A cut is one line."
- **Slow** (under 0.7 screen-heights a second): a weak cut, ×0.85.
- **Exempt:** the thrust and the draw. The point has no edge to turn.

### Debana: the moment he decides

Cut a man in the first 45% of his wind-up and the cut lands before his blow exists. **DEBANA**:
- **An ordinary man** is cut down outright.
- **A boss** takes ×2 and reels.

Later in the wind-up, or mid-swing, it's **CUT HIM OFF**: his blow is gone, ×1.3, he reels. Debana also beats a swordsman's read: he's attacking, not waiting.

Voss: "Cut him as he decides. Not after."

### Saya no uchi: victory in the scabbard

Sheathed, wait for him to come. If you draw as he winds up or swings at you, the draw wins before his blade is out: he's cut down (a boss takes ×3), in slow motion. This makes the draw a counter-stance, not just an opener.

Voss: "Win before the blade is out."

### In one breath

Each man cut down within 1.3 s of the last builds a chain: **TWO IN ONE BREATH**, **THREE IN ONE BREATH**, and on.
- **Feel:** more points and deeper slow motion each time, and a buzz on phones.
- **Why:** this is the sword's addictive loop, and the lethality makes it reachable.

### Also

- **The Kata of Three needs three different lines** (still ×2). Repeating a line is what swordsmen read; the kata now rewards the opposite.
- **Early swipes are held** and taken the moment the blade lands, so they're never lost (same as the pipe).
- **A man cut by a rising line** (kiriage, gyaku) falls on his back (`cut_down_back`); the rest fall on their face.

## Picking it up

**On the floor:** a katana lies flat and still; a glint runs down the blade now and then. One dropped by a swordsman falls, rings off the floor, bounces once and settles.

**Standing** (`katana_pick_up`): walk over it.
- Always by the hilt: he goes to the hilt end, even if he came from the point.
- He goes down on his right knee, and his right hand closes on the hilt.
- The blade rises across him with the edge turning up, and a glint runs down it.
- He stands, the left hand meets the end of the hilt, and he is in chudan.
- Calm and deliberate: the opposite of the pipe's heave.

**On the run** (`katana_scoop`): run at it. He dips on the stride and takes the hilt, and the blade wheels up past his face and over into the trailing carry of the sword run. He doesn't break stride.

**The lift:** the blade in his hand starts exactly where it lay and comes up into his grip. A blade lying with its point towards him comes up and over in an arc.

## Evading his swing (NUKI-WAZA)

- **The retreat (hiki):** swipe away with nobody behind, or Q / E on the keys. Out of distance in one gliding step, the hands and the point travelling with him (they used to stay behind, stretching both arms dead straight).
- **NUKI-WAZA:** retreat as he winds up or swings, and his blade hits nothing (slow motion, OUT OF DISTANCE). Your next cut within 1.1 s comes as his blow falls through empty air: ×1.5 damage, and an ordinary man it would finish is cut down.
- **Not DEBANA:** DEBANA is cutting as he starts; NUKI-WAZA is cutting after he has missed.

## The block

On guard (standing or stepping, facing him, not mid-swing, not running, crouched or sheathed), a blow from the front lands on the weapon.
- **High** (a punch, a swing): jōdan uke: the blade raised before his face, its back taking the blow.
- **Low** (a kick, a low swing): gedan barai: the blade swept down, knocking the kick aside.
- **A fist or a shin into steel hurts the man:** HE HIT THE BLADE. He shakes out his hand or hops on one foot, and takes a little damage.
- **A pipe or a blade on it:** a clang and sparks, and both are pushed apart.
- **Not a free shield:**
  - A third block within 2 s is **GUARD BROKEN**: he staggers, and the next blow lands.
  - A Brute's blow drives 40% through.
  - Timing still pays far more (DEBANA, NUKI-WAZA); the block only saves him when he does nothing.
- **When a blow does land:** he's rocked back with both hands kept on the weapon (`katana_hit`), not the bare-hand flail.

## In the air

**The leap:** swipe up while running. Standing still, swipe up stays the rising cut, and the swallow combo is untouched. The blade is raised overhead (jodan) for the leap.

**THE FALLING CUT:** in the air, swipe down (or tap). The blade comes down through him with the fall as he lands: 30 damage, and an ordinary man it would finish is cut down.

**Landing:** the knees take it, the point rising back into the guard.

## Test

Main menu → test checkpoints:
- **TEST · the katana** (swordsmen): the parry, the read, the kata.
- **TEST · picking up the pipe and the sword**: the kneel, the scoop, swordsmen who drop theirs.
- **TEST · the katana vs ordinary men**: debana, cut him off, saya no uchi, in one breath, the edge.
