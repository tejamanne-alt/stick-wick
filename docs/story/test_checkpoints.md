# Test checkpoints (for testing only)

**Main menu → TEST · CHECKPOINTS** opens a list of places in the story to jump straight to, with the world set up as it would be there. It is controlled by `MainMenu.DevShortcuts`; set it `false` for release.

| Chapter | Checkpoint | How it is set up |
|---|---|---|
| 1 · The Warehouse | Opening | A new run. |
| | At the shutter (the walk) | `Retry`: the walk to the shutter, before the break-in. |
| | Hall 1–4 | `HallCheckpoint` = zone, the knife in hand. |
| | The Foreman (the fight) | Zone 4 with `BossReached`: he rides the hook down. |
| | **Ending · boss beaten** | `TestEnding = 1`: the Foreman is spawned on his last legs and the ending plays from the final blow, so you don't have to fight him. |
| | **Ending · the key on the body** | `TestEnding = 2`: the dead man under the skylight; the ending plays from the search of his pockets. |
| 2 · The Bank | Start · the street | The chapter from the top. |
| | At the counter | `BankFrom = 1`: the doors open, the guard at his post, the hero at the teller's window. |
| | The vault · two keys | `BankFrom = 2`: Harrow beside him at the vault. |
| | The box | `BankFrom = 3`: the box on the viewing table (passports, ring, memory, envelope). |
| | Walk out | `BankFrom = 4`: the ring on, back through the lobby. The bank's last shot fades straight into the street. |
| 2 · The Street | The box | `StreetFrom = 0`: the opening outside the bank (the car's four, "The box. Hand it over."), then S1. |
| | The market street | `StreetFrom = 1`: S2, a second crew from both ends. |
| | Under the railway | `StreetFrom = 2`: S3; the train comes over within a couple of seconds. |
| | The crossroads | `StreetFrom = 3`: S4's last wave, then the Driver arrives. |
| | **The Driver (the fight)** | `StreetFrom = 4`: his car screeches in; all four of his phases (boxer, tire iron, the car charge, bare hands). |
| | **After the Driver** | `StreetFrom = 5`: straight to the interrogation and the end of chapter 2. |
| 3 · Home | Alder Lane | `HomeFrom = 0`: the title, the street at dusk, the car, the chalk, the door. |
| | The study | `HomeFrom = 1`: the desk, the photo, Maya's memory. |
| | The kitchen | `HomeFrom = 2`: Elena; the broken memory; the voice upstairs; the rage. |
| | The house fight | `HomeFrom = 3`: the kitchen → living room → hall → up the stairs → the landing, then Maya's room. |
| | **The Cleaner** | `HomeFrom = 4`: the bedroom; his three phases (knife, choke, anything). |
| | **The kitchen chair** | `HomeFrom = 5`: the interrogation. |
| | The footprints | `HomeFrom = 6`: the footprints, the garden, the end of chapter 3. |

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
