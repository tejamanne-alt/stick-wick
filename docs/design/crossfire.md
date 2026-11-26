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

## Test

- **Main menu → test checkpoints → CHAPTER 1 · TEST · CROSSFIRE:** the first hall. Men come in pairs, one each side of him, and there is no cooldown.
- **The training ground:** four lessons, one for each move, each with a two-finger demonstration.
