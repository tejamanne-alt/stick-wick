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

## 4. Where the words go: in the black, under the picture

- **Bottom band:** subtitles live in the bottom band of the screen, never on top of the picture.
  - In cinematics the letterbox bar grows to hold the line, then shrinks back.
  - In gameplay (no bars), a soft dark fade rises from the bottom edge instead.
- **Two rows, centred:**
  - the speaker's name in their colour, with a short dash before it (a signal mark for off-screen voices)
  - the line in white, wrapping onto two rows if long
- **Leaning:** the two rows lean slightly towards the speaker (within the middle quarter of the screen).
- **Drawn last:** nothing in the story can cover the subtitles.
- **Everything else moves out of the way:**
  - The controls hint in a fight moves up above the band.
  - The phone screen in the ending sits between the top bar and the words, and is sized to fit.

A mock of the layout (desktop, a 20:9 phone and a 4:3 tablet) is in `docs/previews/subtitles_layout.png`.

## Adding a new speaker

- Call `Say("NAME", "line", seconds)`.
- To pin a line to a particular body, use `Say("NAME", "line", seconds, transform)`. Otherwise the speaker is found automatically:
  - YOU: the hero
  - THE FOREMAN: the boss
  - any other name: the nearest enemy
- Names starting with `VOICE` are treated as off screen.
- A new boss: add his name next to THE FOREMAN in `Say` so he gets the boss colour.
