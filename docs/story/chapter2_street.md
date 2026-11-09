# Chapter 2, part two: THE STREET

This is the second half of chapter 2 (THE RING), not a chapter of its own. When the bank's last shot ends (the black car's door opens across the road), the screen fades and the street begins, with no chapter card in between. The scene is `Story_Street`. The set is `tools/blender/build_street_set.py`, the scene is built by `Editor/StreetBuilder.cs`, and the story is `Level/StoryStreet.cs`.

**Look:** the same black and white as the warehouse, the bank and the tower. It's the morning after the storm, and **no rain falls** (the rain ended with the warehouse). The street is wet: the pavement and the road reflect (one mirror render shared by both) and puddles lie everywhere. There's a low haze, and dust drifts under the railway. The sounds are the city by day, a train, car engines and car doors.

**Fighting:** CONTACT (docs/design/combat_v2.md): grounded strings, the parry, the clinch, throws and slams, and enemies who block, slip, feint and flank.

## The lane (about 140 m, one level)
| Zone | x | Place | What's there |
|---|---|---|---|
| S1 · Outside the bank | 0–30 | MERIDIAN TRUST's columns and steps, Cafe Noir's terrace (tables, chairs, awning), a pharmacy, a bus shelter, a newspaper box, the black car at the kerb | Bins to kick rolling |
| S2 · The market street | 30–62 | Stalls under striped awnings, fruit crates, hanging signs, a fish shop, a shopfront under repair (scaffold, ladder, cement mixer) | Fruit crates to kick |
| S3 · Under the railway | 62–92 | The elevated line overhead on its pillars, strip lamps under the deck, a chain-link fence, dumpsters, a fire escape | **The train** (see below), bins |
| S4 · The crossroads | 92–130 | Hotel Arcadia, a side street going back north (with a zebra crossing), traffic lights, a phone box, a newsstand, a stalled bus across the far end | The Driver's car |

## Shot by shot
| # | Shot | What happens | Sound |
|---|---|---|---|
| T1 | Fade up: low, by the bank's doors | He comes out and down the steps into the white morning. The four from the black car walk out across the pavement to meet him. | A door, the city, footsteps |
| T2 | Two-shot | **THUG:** *"The box. Hand it over."* | |
| T3 | On him | **YOU:** *"It's empty."* … his guard comes up … *"...Come and look."* | Cloth |
| T4 | Wide | **THUG:** *"Have it your way."* Banner: **THE STREET · 1 · OUTSIDE THE BANK**. The fight. | Tension drone |
| T5 | Play | The first swing within reach slows almost to a stop, so the parry can be learned ("TAP HIM NOW · PARRY"). | Slow-motion whoosh |
| T6 | Play | S1 cleared. **YOU:** *"They'll keep coming. Alder Lane is twenty minutes north."* GO. | His breath |
| T7 | Play | S2: a second car's men come in from **both ends**. | Car doors |
| T8 | Play | S3: **a train goes over.** The roar, the deck booms, and the light strobes through the sleepers. Everyone flinches for a moment (they're dazed: the context finishers work on them) while he moves first ("THEY FLINCH · MOVE FIRST"). It comes back every 22–28 s. Clearing the zone: *"Somebody's paying a lot of men to stop me getting home."* | The train pass-by |
| T9 | Play | S4: the last of them. | |
| T10 | Low, along the kerb | **The black car screeches in** and stops. The Driver gets out and walks onto the pavement. **THE DRIVER:** *"Kovac said you were dead. Kovac was always wrong about something."* *"Let's see what's left of you."* Banner: **THE DRIVER**. | Engine, door |
| T11 | Play | **I · THE BOXER** (100–60%): a real guard (he blocks almost half, and reads a move used twice) and real combinations, every one of them parryable. | |
| T12 | Play | **II · THE TIRE IRON** (60–30%): he backs off to the car's boot and comes back with a tire iron. *"Hold still."* Three swings at a time, which can't be parried; step out of the third and he's open. | Boot, iron |
| T13 | Play | **III · THE CAR** (at 30%): he flings the iron down, gets in, pulls onto the pavement and floors it straight at him. As it closes, time slows: **DIVE CLEAR** (any swipe). He dives into a shop doorway. If he doesn't, the car hits him (26 damage, knocked down). The car ends in the railway's last pillar: a boom, glass, metal, and a shake. | Engine, crash |
| T14 | Play | **IV · BARE HANDS:** he falls out of the wreck dazed, then comes on furious (he turns red): haymakers, the headbutt, a bear hug. **THE DRIVER:** *"You owe me a car."* | |
| T15 | Close, low | The last blow. He's down, propped on his elbows; the hero kneels with a fist in his collar. **YOU:** *"Who sent you?"* | Breath |
| T16 | On the Driver | He laughs through it. *"...You'll find out at home."* … *"We got there first."* | Laugh, heartbeat |
| T17 | On him | **YOU:** *"...Elena."* | Riser |
| T18 | Wide, from the road | He gets up and runs up the side street, north, home. | Footsteps |
| T19 | Black | **CHAPTER 2 COMPLETE · THE RING** · *"We got there first."* Next: **CHAPTER 3 · HOME · COMING SOON**. | |

**A defeat** restarts at the zone you were in. If it happens during the Driver fight, it restarts at his arrival.

## Test checkpoints
In the main menu, under TEST · CHECKPOINTS → CHAPTER 2 · THE STREET: the box (the opening), the market street, under the railway (the train), the crossroads, the Driver (the fight), and after the Driver ("We got there first"). The entry point is `StoryIntro.StreetFrom`, with 0–5 matching that list.
