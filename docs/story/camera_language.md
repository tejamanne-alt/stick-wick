# Camera language (the story's cutscenes)

The cutscene moves are in `Assets/Scripts/Core/CineCam.cs`: each is a pure function of progress `u` (0 to 1), returning the camera's position, its look-at point, field of view, roll and (for an overhead) its up direction. A scene applies one with `Cine(shot)` (StoryHome) and lets go with `CineOff()`.

**The rule: a move is chosen for what the beat means.** When no move fits, use a held frame, a slow creep or a cut. The fifteen reference moves are a toolbox, not a checklist.

| Move | Use it for | Chapter 3 |
|---|---|---|
| AerialOrbit | Arriving at a place; how small he is in it | H2: Alder Lane at dusk, as he runs in |
| ArcShot | A turn inside him, slow | H13: in Maya's room |
| DronePullAway | The aftermath; leaving him alone with it | H21: at the back door, the swing still moving |
| Truck | Walking, searching, climbing | The stairs |
| Roll | The world coming apart | H19: the second shock |
| Pan | He hears or sees something and turns to it | H2b: the black car; H4: the small raincoat |
| CraneDown | Coming down to a man who goes down (kneeling, lying) | H9: kneeling by Elena |
| DollyZoom | The floor dropping away: a realisation | H11: the voice upstairs, "the kid's here somewhere" |
| TiltUp | Following something up to what matters | H18: the cord, up to the bulb |
| DollyIn | Resolve; a decision; looking closer | H4: the door; H5: the desk; H9 and H18: his face |
| OverheadRotation | Seeing something whole and terrible, from above | H3: the chalk; H20: the footprints |
| DollyOut | Alone with it | H19: "Not yet!" |
| PedestalUp | Power coming back: he rises | H11: he stands, RAGE |
| LowAngleArc | A threat; a boss | H14: the Cleaner steps out |
| LateralReveal | What's behind the wall | H8: past the kitchen doorway: Elena |
| **Held frame** | Dialogue that needs stillness | H16–H18: the kitchen two-shot, over the shoulder, the man in the chair |
