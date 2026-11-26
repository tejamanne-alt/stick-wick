# The pipe, v2

A heavy steel pipe in two hands. Code: `Assets/Scripts/Combat/HeroPipeV4.cs`. Clips: `tools/blender/v4_rod.py`.

## What was wrong with v1

- **The swipes were the sword's, renamed.** Each angle picked a swing, but nothing felt like swinging something heavy. Every swing started from the shoulder, so nothing carried from one to the next.
- **"Low and away" for the legs** was arbitrary: nobody would guess it.
- **No defence against ordinary men.** The only parry was the sword's mirror parry, which works only on swordsmen with a red attack line.
- **Swipe away used a sword clip:** the katana's back-step, so his hands jumped out of the pipe stance.
- **Misses wore it out.** Swinging at air cost one of the 14 uses.
- **The wind-up didn't wind.** The hold played the spin frozen on its first frame (the plain stance), and the length of the hold changed nothing.

## The three ideas

### 1. The swipe is the pipe's path

Draw the line the pipe should travel, where it should land.

- **Where:** a swipe started in the upper part of the screen goes for the head and body. One started in the lower part goes for the legs. This is the same high/low line as every tap in the game.
- **How fast:** swipe speed sets power (×0.85 to ×1.2 on damage, and on swing speed).

| Swipe | Swing | Damage | After |
|---|---|---|---|
| → level | **Forehand**: the big swing, through everyone in its arc | 22 | knockback |
| ← right after a swing | **Backhand**: back from where the forehand left the pipe | 20 | arc |
| ↘ | **Chop**: short and quick, down across the collarbone | 18 | his guard arm drops (1.2 s) |
| ↓ | **Overhead**: two hands, the whole back | 30 | he goes down. With a man on the floor and no one standing: **PUT DOWN** |
| ↑ ↗ | **Rising**: up under the jaw | 18 | launched |
| low → / ↘ | **Leg sweep** through his shins | 14 | he buckles (0.8 s) |
| ← with nothing to return to | **Back-hop**: out of reach, the pipe kept cocked | — | brief invulnerability |
| ← with a man behind | Turn and swing at him | 22 | |
| tap | **Butt-end** into the belly | 12 | he folds (0.6 s) |
| tap low | A rap on the shins (quicker) | 14 | he buckles |
| hold on the hero | **Wind up**: the longer, the harder (full at 1 s: a spark and a creak). Let go, dragged or not: **the full turn**, everyone around him | 22 → 37 | full charge knocks down |

### 2. Keep it swinging (the flow)

**The window.** Every swing ends somewhere, and the next one starts from there. A swipe made as the pipe comes through **flows**: anywhere from 0.1 s after the impact to 0.22 s after the follow-through ends.

- **No reset:** the follow-through is cut short.
- **Stacking:** each flow step adds +15% damage and +7% swing speed, up to four deep. The whoosh climbs, the splash grows, and the hit-stop and shake get heavier. The style label reads **FLOW ×n**.
- **Buffered:** a swipe made too early is held and fires the moment it can, so fast thumbs never feel ignored.
- **The cost of a miss:** a big swing at nothing, outside the flow, slows his recovery (×0.78). The weight carries him round. Whiffing is a choice with a price.

**Combos.** Swings that belong together pay more:

| Chain | Name | Damage |
|---|---|---|
| → then ← | **FIGURE EIGHT** | ×1.3 |
| → ← → ← (third swing on) | **WINDMILL** | ×1.4–1.45 |
| low sweep then ↓ | **SWEEP AND SMASH** (he's falling: the overhead meets him on the way down) | ×1.5 |
| ↘ chop then ↑ | **UP AND UNDER** (his guard dropped: up under it) | ×1.4 |
| tap then ↓ | **FOLD AND DROP** (he folds forward into it) | ×1.4 |
| back-hop then any swing within 0.6 s | **STEP AND SWING** | ×1.3 |

The backhand clip starts exactly where the forehand's follow-through leaves the pipe. It ends cocked on the rear shoulder, where the forehand starts. So → ← → ← really is one unbroken figure eight on screen.

### 3. Hit the hand that hits you

A pipe doesn't parry; it breaks.

- **INTERCEPTED:** any swing that lands on a man while he's winding up, or swinging but not yet landed, intercepts him. His blow is gone, he reels (1.4 s), and it does ×1.3 damage, with a crack, a longer hit-stop, slow motion and a zoom.
- **Why:** this is the pipe's defence. Watch his wind-up and beat him to it. The butt-end tap is the quickest interceptor.
- **Swordsmen:** the mirror parry still works against their red line.

## In the air

**The leap:** swipe up while running. Standing still, swipe up stays the rising swing. Both hands keep the pipe cocked on the shoulder through the leap.

**FROM ABOVE:** in the air, swipe down (or tap). The pipe goes up overhead and comes down with the fall as he lands: 34 damage, always a knockdown, ringing off the floor.

**Landing:** the knees take it with the pipe still in hand.

## Wear

**14 hits**, counted once per swing however many it catches. Misses don't count.

- **At 4 left:** **THE PIPE IS BENDING** (a creak).
- **The last blow:** the hardest it ever lands (×1.5, knocks down, slow motion, **LAST SWING**). Then **THE PIPE GIVES OUT**.

## Why these choices (the debate)

**Swipe angle vs. where the swipe is drawn.**
- **Angle alone** can't tell a head swing from a leg sweep without odd rules ("low and away").
- **Drawing on the man** hides him under the thumb.
- **The chosen rule:** the screen's high/low line is already how every tap in the game works, so legs = draw it low costs nothing to learn.

**A block gesture vs. intercepts.**
- **Against a block:** a held block would fight with walking (hold = walk), and with the wind-up (hold on the hero).
- **For intercepts:** they need no new gesture. They turn the enemies' existing wind-up tells into the pipe's skill, and they suit a club: offence is the defence.

**Combos as fixed strings vs. physics.**
- **Fixed strings** like "→ ← ↓" are a lot to memorise.
- **Chains that follow the pipe's own momentum** are found by feel: where the pipe ends, swing back from there. The names are the reward for discovering them.

## Test

Main menu → test checkpoints → **CHAPTER 1 · TEST · the pipe** (crane bay, three pipes on the floor, ordinary men).
