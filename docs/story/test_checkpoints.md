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
| | Walk out | `BankFrom = 4`: the ring on, back through the lobby. |

**For every new chapter:** add its checkpoints here and to `MainMenu.Checkpoints`. At least: its start, each fight zone, its boss fight, its ending after the boss, and every story beat worth replaying on its own. The entry points live in `StoryTest.cs` (and a `TestState` per chapter, like the bank's).
