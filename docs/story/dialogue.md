# Who is speaking: the subtitle rule

One system for every line of dialogue in story mode, in `StoryDialogue.cs`. Every scene calls `Say(who, line, seconds)`.

## 1. A plain name, from the player's side

| Name on screen | Who it is |
|---|---|
| **YOU** | The hero. He has no name of his own yet; the player *is* him. |
| **THUG** | One of the crew, whoever is in the hero's face. |
| **THE FOREMAN** | The chapter's boss. Later bosses use their own titles. |
| **VOICE ON THE PHONE** | The man behind the contract. Heard, never seen. |
| **VOICE BEYOND THE SHUTTER** | Someone speaking from off screen. |

## 2. A colour for each kind of speaker, the same all game

| Kind | Name and bar colour | The line |
|---|---|---|
| **YOU** (the hero) | White name, the game's **red** bar (the red of the title) | White |
| **Enemies** | **Amber** | White |
| **A boss** | **Blood orange**; the name one size up | White |
| **Off screen** (phone, door, radio) | **Cold blue**, with a small signal mark (three rising bars) by the name | Tinted pale blue |

## 3. A marker over the speaker's head

While someone is speaking, a small triangle in their colour floats over their head. It pulses once as each new line starts. In the silhouette scenes, where both men are black shapes, the marker is what tells them apart. Off-screen voices have no marker.

## 4. The subtitle leans towards the speaker

The subtitle's panel slides left or right to sit under the speaker (within the middle third of the screen). The eye goes from the words straight to the mouth. A speaker off screen, or out of shot, gets a centred panel.

Every line sits on a dark panel, with a coloured bar down its left edge, so white text stays readable over rain, lightning and the bright backlight. Long lines wrap onto two rows.

## Adding a new speaker

- Call `Say("NAME", "line", seconds)`.
- To pin a line to a particular body, use `Say("NAME", "line", seconds, transform)`. Otherwise the speaker is found automatically:
  - YOU: the hero
  - THE FOREMAN: the boss
  - any other name: the nearest enemy
- Names starting with `VOICE` are treated as off screen.
- A new boss: add his name next to THE FOREMAN in `Say` so he gets the boss colour.
