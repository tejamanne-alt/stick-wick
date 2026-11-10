# Test checkpoints (for testing only)

**Main menu → TEST · CHECKPOINTS** opens a list of places in the story to jump straight to, with the world set up as it would be there. It is controlled by `MainMenu.DevShortcuts`; set it `false` for release.

| Chapter | Checkpoint | What it plays (story v2, docs/story/story_v2.md) |
|---|---|---|
| 1 · The Warehouse | Opening | A new run: HOLD to open his eyes while the voices beyond the shutter talk ("Boss wants him breathing. Dead, that box goes straight to the cops."), MASH to break the zip ties, flash 1 (the pipe, the coat walking away). The men: "Easy... boss wants him breathing." |
| | At the shutter, Hall 1–4 | The walk and the hall zones, as before. |
| | The Foreman | His hard hat; "Boss wants you breathing. For now."; phase III "You don't even know who you are. Do you?" |
| | Ending | "The old man wants his book back. Nine o'clock, you open a box for him." Then TAP his coat, TAP the inside pocket, DRAG to unfold the note: "Only his hand opens 417. Keep him alive till nine. Get the book. Then finish him. — V." |
| 2 · The Bank | The vault | Mr. Pell: six years, the standing instruction (the box goes to the lawyer); HOLD the palm on the glass. |
| | The box | The family's passports (him, Elena, Maya), the ring and the wedding memory, "I have a daughter", TAP the black book and DRAG to turn its page (41 names, every order signed V.), his own letter ("They'll come for E. and M. 14 Alder Lane. Get them out."). |
| | Walk out | Past the counter: "Yes, Mr. V. He took it with him." / "...Mr. V." |
| 2 · The Street | Start · after the Driver | "The book. Hand it over." The Driver drove him for eleven years; at the end, CHOOSE "Who is V?" or "What's waiting at home?" |
| 3 · Home | Arrival | TAP the hall table: Harrow's card and Elena's note. |
| | The study | DRAG to turn the family photo over: the old photo of V, the young hero and the boy; flash 2 "I'm out, Voss."; "You raised me." |
| | The kitchen | The bleach and plastic; HOLD to close her eyes; WHAT HAPPENED HERE; "Silas wants this place spotless." |
| | The house fight | ...then her room: TAP her shelf, one walkie gone, he keeps the other. |
| | Silas | "You're early. I wasn't finished." "They always come home." His lines in the fight. |
| | The kitchen chair | CHOOSE three of four questions; the phone rings (V.), SWIPE up to answer; Voss: "Hello, son."; SWIPE right for the reflex; "...Silas? Silas!" "Bring me the book. Then we're even." |
| 4 · The Backyard | Find her | Footprints; DRAG the ribbon free; the walkie crackles; HOLD to talk; Maya tells last night over the stills; CHOOSE what to ask; the van at the far end, her face in its window. |
| | Reach her, lose her | The torches; three stretches, her voice between them (count to three; "did you find Mama?" CHOOSE; "they're starting the van!"); the car park: MASH to run after the van; it gets away. |
| | The Handler · the address | The slow clap, his three phases, "He pulled you off the street at nineteen... and you just killed Silas." "I'm not his son. I'm hers." The car keys. |
| 5 · The Road | 0–4 | `RoadFrom`: the drive and her voice; counting the thunder (tap each flash); "Are you a bad man?" (CHOOSE) and HOLD to hum; the battery ("Over and out"); the last hairpin ("It was never a trade"). |
| 6 · The Fortress | 0–6 | As before, plus: the guards' bounty ("Whoever brings the boss that book gets the son's place!"), the barks, the trap's line, the Warden's chair, and at the tower the walkie: "Papa! I see you! You're tiny!" |
| | The last roof | Flash 3 ("Bring them to me. All of them. Alive."), six years of mercy, Silas ("Your boy killed Elena." "...I know. And I still loved him."), the trade: DRAG the book across to him, she runs, he draws the blade from his cane. |
| | The duel, the fall | The teacher, the gun, the edge; then CHOOSE REACH FOR HIM or LET GO; Maya; DRAG to tie her ribbon; the dawn, the book back in his coat. |
| | The walk-out | `FortressFrom = 10` (docs/story/walkout.md): the dawn wide, then the one take: out of the great door hand in hand, the walkies ("I can see you. Over."), the last man at the plunger ("...Then burn it."), "Over and out." and the hill goes up; the slow motion on the bridge, two clicks and two back; OVER AND OUT. CHAPTER 6 COMPLETE. |
| 7 · Home | 0–3 | `EpilogueFrom`: Harrow ("...Then I never saw you. Take her home."), the grave (HOLD your hand on the stone), the swing (SWIPE to push), the drawer (DRAG to close it), THE END, then the recap. |
| The whole story | Timeline | `EpilogueFrom = 5` (also the main menu's THE WHOLE STORY button): the recap alone. |

**For every new chapter:** add its checkpoints here and to `MainMenu.Checkpoints`. At least: its start, each fight zone, its boss fight, its ending after the boss, and every story beat worth replaying on its own. The entry points live in `StoryTest.cs` (and a `TestState` per chapter, like the bank's).

## CONTACT combat: what to try (any hall checkpoint in chapter 1)
Use **Hall 1** (knife in hand) or **Hall 2**. On first meeting, the fight shows a COMBOS card, and a hint appears the first couple of times a parry or a grab is possible.
- **Movement:** he jogs (no sprint), hops low, and never runs up walls or flips. A swipe away is a short step back.
- **Strings** (P = tap high, K = tap low; on keys, A/D punch and Z/X kick): `P P P P`, `K K K`, `P P K`, `K P P`, `P K P`, `P P P K`. The finisher's name shows when it lands, and it floors the man.
- **Parry:** tap at a bare-handed man just as he swings (red flash, close in). His punch goes aside, his head snaps back and he is dazed. Your next tap high is PALM TO THE CHIN; a tap low is a head scissors.
- **Grab:** swipe at a man in arm's reach (Q/E), or walk into him. In the clinch:
  - tap high: knee, knee, then headbutt
  - tap low: sweep
  - swipe at him: hip throw
  - swipe away: shoulder throw
  - swipe up (W): suplex, or a body slam if he is worn down
  - swipe down (S): DDT
  - tap behind you: shove him off
  - do nothing: after about 2.4 s he breaks free.
- **A big man (brute):** a lift is refused with TOO HEAVY until he's down to 40% health. Knees and the sweep always work.
- **From range:** swipe at a man 1.6–3 m away for a SPEAR; punch while running into him for a CLOTHESLINE.
- **Check:** thrown men land where the animation puts them (no snapping back), get up the way they fell (a DDT means face down), and a man you're holding is let go if someone else hits you.
