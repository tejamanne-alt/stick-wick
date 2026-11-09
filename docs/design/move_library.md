# The fight move library (CONTACT kit)

Every move is a clip on the one stickman rig (`Assets/Art/Stickman/Stickman.fbx`). Clips are either real motion capture or keyed by hand, and none of them is a copy of another game's moves. Contact sheets for each clip are in `docs/previews/<clip>.png`; mocap clips are under `mocap_<clip>.png`.

## Real motion capture (CMU database, `tools/blender/mocap_clips.py`)
| Style | Clips |
|---|---|
| Karate | `oi_zuki` (lunge punch), `gyaku_zuki` (reverse punch), `mae_geri` (front kick), `mawashi_geri` (roundhouse), `side_kick`, `gedan_barai` (low sweeping block) |
| Boxing | `jab`, `cross`, `lead_hook`, `jab_hook`, `one_two_three` (jab, cross, hook) |
| Evasion | `slip` (duck under and come back up), `dodge` |
| Falls and get-ups | `fall_face`, `getup_face`, `getup_side`, `getup_back`, `knockdown` |

## Hand-keyed (`CONTACT_CLIPS` in `tools/blender/build_stickman.py`)
| Group | Clips |
|---|---|
| Strikes (karate, boxing, muay thai) | `elbow`, `back_fist`, `palm_strike`, `low_kick`, `hammer_fist`, `headbutt`, `clinch_knee`, `clothesline` |
| Defence | `parry`, `block_high`, `block_hold`, `block_hit`, `block_low`, `catch_kick` |
| Clinch | `clinch_enter`, `clinch` / `clinch_v`, `shove` / `shoved_v` |
| Throws (judo) | `hip_throw` / `hip_throw_v` (o-goshi), `shoulder_throw` / `shoulder_throw_v` (seoi-nage), `sweep` / `sweep_v` (o-soto-gari) |
| Slams (pro wrestling) | `suplex` / `suplex_v`, `body_slam` / `body_slam_v`, `ddt` / `ddt_v`, `spear` / `spear_v`, `clothesline` / `clothesline_v` |
| Reactions | `hit_body`, `hit_legs`, `hit_chin`, `stumble_back`, `dazed`, `kneel_hurt` |

## Paired moves
`X` is the attacker's clip and `X_v` is the clip of the man it is done to. Both have the same length and the same hit frame. The victim stands `VictimGap` (0.62 m) in front of the attacker, facing him. Where the victim's body ends up is exported as `endShift` (metres, in the victim's own forward direction) in `stickman_clips.json` and in `MoveSet.Entry.endShift`. When the clip ends, the game moves his transform by that amount and he goes on into `lie` or `prone` from the spot where he landed.

## Next
The hero's CONTACT controls (tap to strike, hold to grab, parry, context moves) and enemies that use the same kit (task 15/16).
