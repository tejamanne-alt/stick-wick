# Character prompts: ChatGPT → Meshy → Stick Wick

Goal: replace the 14-bone chibi stickman with a realistic, properly rigged mannequin. The new model has feet, neck, clavicles and wrists (see `docs/COMBAT_PROMPT_SHEET.md` B8).

The pipeline:
1. **ChatGPT, look.** Pick the hero's look (prompt 1).
2. **ChatGPT, turnaround.** Lock proportions in a turnaround (prompt 2).
3. **ChatGPT, Meshy input.** Make clean single images for Meshy (prompts 3a–3c).
4. **Meshy.** Image to 3D, then auto-rig, then export FBX (section 5).
5. **Claude.** Check proportions and bones in Blender, retarget the mocap onto the new skeleton, add the weapon and grab sockets, and import into Unity.

## Why the Meshy images are grey clay, not black
- **Image-to-3D reads shape from shading.** A matte-black figure is a flat silhouette: Meshy can't see where the knee bends or how deep the chest is. Grey clay with soft light gives it the form.
- **Colour doesn't need to come from the model.** The game tints the body at runtime (`Fighter.SetBaseColor`): black hero, coloured enemies. So **one neutral model can serve every colour**.
- **No eyes or face on the model.** The game adds the white eyes itself (`FighterFactory.BuildEyes`).

## Rules for every ChatGPT image
- Do it all in **one ChatGPT conversation**, so it keeps the same character and you can say "the character from image 2".
- **Reject and regenerate** if you see any of these:
  - fused or extra fingers or limbs
  - arms touching the body, or crossed legs
  - feet cut off
  - a strong floor shadow
  - a dramatic camera angle (a wide lens makes the head look bigger)
  - asymmetric shoulders in the front view

---

## 1. Pick the hero's look (landscape, 3:2)
```text
Concept sheet for an ORIGINAL hero of a 2.5D side-scrolling action game. Four design options
side by side on a plain light-grey background, all at the same height and scale, full body
from head to feet, each standing in a relaxed fighting guard in a 3/4 view.
Every option is a faceless stylized mannequin: smooth matte surface, tapered capsule-shaped
limbs, clearly visible rounded knee and elbow joints, simple mitten hands with a separate
thumb, simple rounded shoes, a smooth egg-shaped head with no face and no hair.
Stylized-realistic athletic proportions: 6.5 heads tall, broad shoulders, narrow waist,
long legs, lean and strong.
All four are solid matte black with a thin white rim light, so the silhouette reads.
Option 1: bare mannequin, no clothing.
Option 2: the same mannequin in a slim tailored three-piece suit shown only as clean shapes
(jacket hem, lapels, tie), still smooth and faceless.
Option 3: slim suit with a knee-length coat that flares out at the hem.
Option 4: fitted turtleneck and slim trousers.
Put a large number 1-4 under the feet of each. No other text, no logos, not a real person.
```
Tell ChatGPT which one you like, or mix them, e.g. "option 2 but without the tie".

## 2. Hero turnaround (landscape, 3:2)
```text
Make a character turnaround sheet of option [N] from the previous image.
Render it as a light-grey clay model (no black), soft even studio lighting, plain white
background, no floor shadow, orthographic camera (no perspective).
Top row, five views at exactly the same scale and height, standing in a relaxed A-pose
(arms 30-40 degrees away from the body, palms facing the thighs, feet shoulder-width
apart): front, 3/4 front, right side, 3/4 back, back.
Bottom row: the right-side view again with small dark dots on the joints (neck,
shoulders, elbows, wrists, hips, knees, ankles, toes), a vertical ruler marked in head
heights (6.5 heads in total), and next to it the same character in a side-view fighting
guard: chin tucked, lead hand at chin height, rear hand at the jaw, elbows down, knees
softly bent, weight mostly on the rear foot.
Keep the head blank with no eyes, nose, mouth or hair. No text except the ruler marks.
```
**Send me this image here.** I'll check the proportions and joint positions before you spend Meshy credits.

## 3. Meshy input images (portrait, 2:3, one figure per image)
Meshy needs one clean, centred, full-body figure. Make the front first. Side and back are optional extras if you use Meshy's multi-image input.

**3a. Front**
```text
Generate a single image of the same character from the turnaround: exact front view,
orthographic camera at chest height, full body centred with a little space above the head
and below the feet. A-pose: arms straight, held 35 degrees away from the body, palms facing
down/inward, fingers together, thumb separate; legs straight, feet shoulder-width apart,
toes pointing forward. Light-grey clay material, soft even lighting from the front, no cast
shadow, no floor, plain pure white background. Blank head with no face. Nothing in the hands.
```
**3b. Side (optional)**
```text
Same character, same pose, same scale and lighting, now an exact right-side orthographic view.
```
**3c. Back (optional)**
```text
Same character, same pose, same scale and lighting, now an exact back view.
```

## 4. Enemy bodies
The enemies don't each need their own model. Five body types cover all seven archetypes. The weapon, stance and colour come from the game.

| Body | Used by | Meshy model? |
|---|---|---|
| Lean | Knife fighter, Swordsman | yes |
| Average | Brawler, Stick fighter, Gunner | yes |
| Heavy | Brawler variant | yes |
| Armoured | Armoured tactical (helmet + vest) | yes |
| Brute | Boss (also scaled 1.4× in game) | yes |

**4a. Line-up for design** (landscape, 3:2)
```text
Line-up of five enemy body types for the same game, same style as the hero (faceless
stylized mannequins, tapered capsule limbs, visible knee and elbow joints, mitten hands,
blank egg-shaped heads), side by side on a plain light-grey background, same ground line,
a height ruler on the left, each in a front-facing relaxed A-pose, light-grey clay material.
1 Lean: wiry, narrow shoulders, 6.5 heads tall.
2 Average: medium build, 6.5 heads.
3 Heavy: thick torso, wide shoulders and neck, slightly shorter legs.
4 Armoured: average build wearing a simple rounded helmet and a bulky tactical vest with
   plates, knee pads, all as clean smooth shapes.
5 Brute: massive, very broad shoulders and chest, thick arms, small head relative to the body.
Number 1-5 under the feet. No other text, not real people.
```
**4b. Meshy input, one per body:** reuse prompt 3a, replacing "the same character from the turnaround" with "body type [N] from the line-up".

---

## 5. Meshy settings (when the images are approved)
1. **Image to 3D.**
   - Upload the front image. Add the side and back as multi-view if you made them.
   - Symmetry **on**, A-pose, **quad** topology.
   - Target about **8–12k triangles** for the hero and about **6–10k** for enemies (mobile).
   - **Texturing isn't needed**, since the game colours the body. Skip it to save credits.
2. **Auto-rig** as a **humanoid**. Mark the chin, wrists, elbows, knees and groin carefully if it asks.
3. **Export FBX** (rigged, in the A-pose). Don't include any of Meshy's animations: our moves come from the mocap pipeline.
4. **Upload the FBX here** (or put it in `docs/reference/characters/meshy/`).

What I do with it:
- Inspect the bone names and proportions.
- Add the sockets: `grip.L/R`, `holster`, `victim_neck`, `victim_back`.
- Retarget the CMU mocap onto it (`tools/blender/retarget.py`).
- Re-export to `Assets/Art/`, then update `Fighter.BodyScale` and the reach constants.
