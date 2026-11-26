# CROSSFIRE: two men on you

Code: `Assets/Scripts/Combat/HeroNearV4.cs`; input in `Core/GestureInput.cs`. Clips: `tools/blender/v4_near.py` (`crossfire`, `cf_knees`, `cf_split`, `cf_into`, each with `_a_v` for the man in front and `_b_v` for the man behind).

## The control: both thumbs at once

When one man is close in front of him and one close behind, **tap both thumbs at once, one on each side of him**.
- **Each thumb picks the target on its side:** HIGH means that man's head, LOW means his legs or body.
- **A single two-thumb tap, not a double.** Crossfire is a reaction move, and a second pair would add about 0.3 s while they're already swinging. Two thumbs landing together on opposite sides never happens by accident, so it doesn't need doubling.
- **Detection:**
  - A tap counts when the finger lifts. When one thumb lifts while the other is still down, having landed within 0.15 s of it on his other side, the first tap is held until the second lifts, and the two become one CROSSFIRE.
  - A lone tap is never delayed.
  - If the other thumb turns into a swipe or a hold, the held tap goes through on its own.
- **Ordinary taps are just strikes now.** Before, any tap with two men on him fired the cinematic.
- **With only one man near:** a two-thumb tap is a strike at the nearer man, at that thumb's height.
- **Keys:** a key on each side pressed together: A/D (high) and Z/X (low). For example, A + X is left high, right low.
- **Cooldown:** 10 s (it was 16 s, but the move is now chosen deliberately).

## The four moves

**HIGH + HIGH: THE CROSSING** (`crossfire`, kept). The idea: they hit each other.
- He drops under both swings; their fists cross and land on each other.
- He comes up between them, a palm in front and an elbow behind.
- A push kick sends the front man flying; a back kick takes the other.

**LOW + LOW: BOTH KNEES** (`cf_knees`). The idea: Wick's knee stamps.
- A stamp through the front man's knee, then the same leg re-chambered in a mule kick into the other man's knee.
- Both drop to a knee, with their heads now at his hip.
- He pulls the front man's head down onto his knee, then drives the heel straight back into the other man's face.

**front HIGH + back LOW: BOTH AT ONCE** (`cf_split`). The idea: two blows on the same frame.
- A straight right to the front man's jaw and a back kick into the other man's gut, landing on the same frame.
- The frame freezes (a long hit-stop, deep slow motion, white ink at both impacts).
- The front man goes over stiff, like a felled tree. The other folds round the heel and goes down on his face.

**front LOW + back HIGH: ONE INTO THE OTHER** (`cf_into`). The idea: one man used as a weapon against the other.
- A back elbow into the face behind, then a liver hook in front.
- The front man folds into him, and the hero loads him on his hip (o-goshi) and throws him backwards, over, into the other man.
- Slow motion at the top of the throw; both go down in a heap.

## Why these four (the debate)

- **Replaced: a 360° floor sweep for LOW + LOW.** It's kung-fu cinema, he already has a sweep, and on short chibi legs it reads as a blur at phone size.
- **Considered and rejected:**
  - **Banging their heads together:** impossible with him standing between them.
  - **A fireman's-carry slam:** too slow mid-fight, and it repeats the one-into-the-other idea.
- **The rule:** each move rests on a different idea (they hit each other / precise brutality / both at once / one into the other) and has a distinct silhouette (a duck, kicks low then high, a frozen star, a man over the hip).
- **The taps say what happens to each man:** the thumb on a man's side picks where he's hit.

## In every chapter

**The move itself** works wherever the story's combat runs: every chapter runs on the V4 body and the story's combat, so CROSSFIRE is available in every fight while his hands are bare or he's holding the knife.

**The pincer** (`Level/StoryCrossfire.cs`):
- **The problem:** CROSSFIRE needs a man in front and one behind, but in most fights the men come at him from one side. Only about half of them ever go round on their own.
- **The fix:** in any fight, in any chapter, the men now box him in deliberately. With two or more on him, one is told to go round, and his mates call it: "Get behind him!", "Box him in!", "Take his back!"
- **How often:** the first time about 5 s into a fight, then every 25 s or so of fighting.
- **When:** only while CROSSFIRE is his to use (bare hands, the knife, the pipe, or the drawn katana).
- **The cue:** the "TWO ON YOU · BOTH THUMBS" cue over him shows in every chapter.

## Armed: the pipe and the katana

**The control is the same: both thumbs at once, each HIGH or LOW.** The weapon changes what they do. Clips: `tools/blender/v4_cf_armed.py`.

**THE PIPE** (weight and momentum: the swing carries on, and both ends are weapons). The whole sequence counts as one hit of wear, and everyone goes down.

| Thumbs | Move | |
|---|---|---|
| high + high | **BOTH ENDS** (`rod_cf_ends`) | The butt end straight back into the face behind; off that, the forehand round into the front man's head |
| low + low | **THE SCYTHE** (`rod_cf_scythe`) | Low through the front man's shins, over the top, low through the other man's; then the overhead PUT DOWN on one |
| front high, back low | **THE WIND-UP** (`rod_cf_windup`) | The backswing chops the knee behind as it winds up; over the top into the front man's head |
| front low, back high | **OVER THE SHOULDER** (`rod_cf_shoulder`) | A rap on the front man's shins (he buckles); whipped back over the shoulder into the face behind; down on the buckled man |

**THE KATANA** (one line, precision, death; based on iaido's answers to men behind). Every line cuts an ordinary man down; he stands a beat, then falls. Each ends with chiburi (the flick that cleans the blade).

| Thumbs | Move | |
|---|---|---|
| high + high | **ONE LINE, TWO MEN** (`katana_cf_line`) | A level cut through the front man; the turn carries the same line through the man behind |
| low + low | **THE KNEELING CIRCLE** (`katana_cf_kneel`) | Down on one knee: through the front man's legs, then, turning on the knee, through the other's |
| front high, back low | **THE THRUST BEHIND** (`katana_cf_thrust`) | The point reversed under the arm, straight back into the man behind; out and up, a diagonal cut down the front man |
| front low, back high | **RISE AND FALL** (`katana_cf_rise`) | Rising from the hip through the front man; carried overhead as he turns; straight down onto the other |

**Limits:**
- **Turning:** the pose engine turns his upper body about 180° with the feet planted, so the turns read as strong pivots, not free spins.
- **When the katana works:** only drawn. Sheathed for the draw, there's no CROSSFIRE.

## Test

- **Main menu → test checkpoints → CHAPTER 1 · TEST · CROSSFIRE:** the first hall. Men come in pairs, one each side of him, and there is no cooldown.
- **CHAPTER 1 · TEST · CROSSFIRE with the pipe / with the katana:** the same, with the weapon in hand (it never wears out there).
- **The training ground:** four lessons each for bare hands, the pipe and the sword, each with a two-finger demonstration.
