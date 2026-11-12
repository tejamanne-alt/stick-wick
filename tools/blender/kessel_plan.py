"""Kessel Hill v3: THE layout (docs/story/chapter6_fortress_v3.md), the one source of truth.

The Blender set (build_kessel_v3.py) is modelled on these numbers, and the game's navigation is
generated from them (Assets/Scripts/Level/KesselPlan.cs, by `python3 tools/blender/kessel_plan.py`),
so the stone you see and the floor you stand on can't disagree.

The lane runs along x (metres) and climbs; y is height (Unity's y, Blender's z). Everything solid is
either a floor below here, or sits off the lane (behind it at depth > 1.4, or in front at < -1.6).
The new levels start at x 300, clear of the old fortress set (kept for the roof duel and the walk-out)."""
import json
import os
import sys

# ---------------------------------------------------------------- floors (x0, x1, y, one-way, name)
P = [
    (300.0, 344.0, 0.0, False, "road_low"),         # 0  L1 the switchback road, lower
    (322.0, 380.0, 3.4, True, "road_up"),           # 1  L1 the upper road (over the lower one)
    (390.0, 396.0, 7.4, False, "stair_landing1"),   # 2  L2 the great stair's first landing
    (406.0, 412.0, 11.4, False, "stair_landing2"),  # 3  L2 the second landing
    (420.0, 444.0, 15.4, False, "barbican"),        # 4  L3 the barbican's forecourt (the moat beyond)
    (448.0, 458.0, 21.0, True, "gatehouse_room"),   # 5  L3/L4 the room over the passage (the chain leads here)
    (448.0, 472.0, 15.4, False, "gate_passage"),    # 6  L4 the gate passage (the killing ground)
    (480.0, 520.0, 22.4, False, "wall_walk"),       # 7  L5 the wall walk
    (528.0, 584.0, 15.4, False, "courtyard"),       # 8  L7 the courtyard
    (548.0, 560.0, 18.6, True, "hay_loft"),         # 9  L7 the stable's loft
    (584.0, 612.0, 15.4, False, "gallery"),         # 10 L8 Voss's gallery
    (612.0, 660.0, 15.4, False, "great_hall"),      # 11 L9 the great hall
    (644.0, 658.0, 19.6, True, "minstrel_gallery"),  # 12 L9 the minstrels' gallery (the long table stands behind the lane: no walking through it)
    (672.0, 676.0, 21.4, False, "tower_landing"),   # 13 L10 a landing on the tower stair
    (684.0, 688.0, 24.4, False, "tower_ledge"),     # 14 L10 beyond the broken step
    (698.0, 716.0, 30.4, False, "observatory"),     # 15 L11 the observatory
    (681.4, 682.4, 24.4, False, "broken_end"),      # 16 L10 the last whole step before the gap
    # the old fortress set, kept: the cliff road and the keep's level (the arrival, the walk-out) and the
    # roof terrace (the duel with Voss)
    (0.0, 44.0, 0.0, False, "old_road"),            # 18
    (52.0, 166.0, 6.0, False, "old_keep"),          # 19
    (128.8, 161.4, 18.4, False, "roof_terrace"),    # 20
]

# ---------------------------------------------------------------- stairs (bottom x, y, top x, y, bottom floor, top floor)
S = [
    (344.0, 0.0, 352.0, 3.4, 0, 1),        # L1 the bend of the switchback
    (380.0, 3.4, 390.0, 7.4, 1, 2),        # L2 flight one
    (396.0, 7.4, 406.0, 11.4, 2, 3),       # L2 flight two
    (412.0, 11.4, 420.0, 15.4, 3, 4),      # L2 flight three, up to the barbican
    (472.0, 15.4, 480.0, 22.4, 6, 7),      # L4 → L5 the passage's stair to the wall
    (528.0, 15.4, 520.0, 22.4, 8, 7),      # L6 the wall tower's stair (down to the courtyard)
    (566.0, 15.4, 560.0, 18.6, 8, 9),      # L7 the stable roof (slid down)
    (640.0, 15.4, 644.0, 19.6, 11, 12),    # L9 the gallery stair
    (660.0, 15.4, 672.0, 21.4, 11, 13),    # L10 the tower stair, first flight
    (676.0, 21.4, 681.4, 24.4, 13, 16),    # L10 the second flight, to the last whole step before the gap
    (688.0, 24.4, 698.0, 30.4, 14, 15),    # L10 the last flight to the observatory
]

# ---------------------------------------------------------------- ledges (x, y, dir): hang and pull up
L = [
    (322.0, 3.4, 1),          # L1 the retaining wall: up onto the upper road
    (548.0, 18.6, 1),         # L7 the hay loft's edge (a ladder)
]

# ---------------------------------------------------------------- levels: (name, x0, x1, floor y, checkpoint label)
LEVELS = [
    ("THE SWITCHBACK ROAD", 300.0, 380.0, 0.0),
    ("THE GREAT STAIR", 380.0, 420.0, 3.4),
    ("THE BARBICAN", 420.0, 443.8, 15.4),       # stops at the moat's edge
    ("THE GATE PASSAGE", 448.0, 472.0, 15.4),
    ("THE WALL WALK", 472.0, 520.0, 22.4),
    ("THE WALL TOWER", 520.0, 528.0, 22.4),
    ("THE COURTYARD", 528.0, 584.0, 15.4),
    ("VOSS'S GALLERY", 584.0, 612.0, 15.4),
    ("THE GREAT HALL", 612.0, 660.0, 15.4),
    ("THE TOWER STAIR", 660.0, 698.0, 15.4),
    ("THE OBSERVATORY", 698.0, 716.0, 30.4),
]

# ---------------------------------------------------------------- props and set pieces (x, y): where the game finds them
MARKS = {
    "truck": (330.0, 0.0),
    "floodlight_road": (338.0, 0.0),
    "lion": [(391.5, 7.4), (394.5, 7.4), (407.5, 11.4), (410.5, 11.4)],
    "barrel_top": (421.0, 15.4),
    "murder_holes": [(428.0, 15.4), (434.0, 15.4), (440.0, 15.4)],
    "drawbridge_chain": (443.2, 15.4),
    "chain_top": (448.6, 21.0),
    "murder_hole_drop": (452.0, 21.0),
    "portcullis_front": (449.0, 15.4),
    "portcullis_back": (471.0, 15.4),
    "windlass": (469.8, 15.4),
    "banner": (517.0, 22.4),
    "well": (534.0, 15.4),
    "cart": (541.0, 15.4),
    "hay_hoist": (556.0, 18.6),
    "generator": (570.0, 15.4),
    "flood_court": [(538.0, 15.4), (576.0, 15.4)],
    "bell_rope": (581.0, 15.4),
    "shoji": [(590.0, 15.4), (596.0, 15.4), (602.0, 15.4)],
    "photo": (608.5, 15.4),
    "chandeliers": [(622.0, 15.4), (634.0, 15.4), (648.0, 15.4)],
    "fireplace": (658.0, 15.4),
    "broken_step": (683.0, 24.4),
    "telescope": (707.0, 30.4),
    "dome_crank": (714.0, 30.4),
    "tower_window": (707.0, 36.0),     # Maya's lit window (the goal, seen from everywhere)
}


def cs():
    """KesselPlan.cs: the floors, stairs, ledges, levels and marks, as the game's LevelNav wants them."""
    out = ["// GENERATED by tools/blender/kessel_plan.py: edit the plan there, then run it.",
           "using UnityEngine;", "", "namespace ProjectInk", "{",
           "    /// <summary>Kessel Hill v3 (docs/story/chapter6_fortress_v3.md): floors, stairs, ledges, the levels and",
           "    /// the set pieces' marks. The Blender set is modelled on the same numbers.</summary>",
           "    public static class KesselPlan", "    {",
           "        public static readonly Platform[] Platforms =", "        {"]
    for x0, x1, y, ow, n in P:
        out.append(f"            new Platform({x0}f, {x1}f, {y}f, {'true' if ow else 'false'}),   // {n}")
    out += ["        };", "", "        public static readonly Stair[] Stairs =", "        {"]
    for x0, y0, x1, y1, b, t in S:
        out.append(f"            new Stair({x0}f, {y0}f, {x1}f, {y1}f, {b}, {t}),")
    out += ["        };", "", "        public static readonly Ledge[] Ledges =", "        {"]
    for x, y, d in L:
        out.append(f"            new Ledge({x}f, {y}f, {d}),")
    out += ["        };", "", "        public static readonly (int from, int to, float x)[] ExtraWalks = { };", "",
            "        public static readonly (string name, float x0, float x1, float y)[] Levels =", "        {"]
    for n, x0, x1, y in LEVELS:
        out.append(f"            (\"{n}\", {x0}f, {x1}f, {y}f),")
    out += ["        };", ""]
    for k, v in MARKS.items():
        name = "".join(w.capitalize() for w in k.split("_"))
        if isinstance(v, list):
            items = ", ".join(f"new Vector2({a}f, {b}f)" for a, b in v)
            out.append(f"        public static readonly Vector2[] {name} = {{ {items} }};")
        else:
            out.append(f"        public static readonly Vector2 {name} = new Vector2({v[0]}f, {v[1]}f);")
    out += ["    }", "}", ""]
    return "\n".join(out)


def check():
    """The self-test: every stair joins the floors it claims, at their ends; no floor overlaps another at
    the same height; every mark stands on a floor or a stair. Fails loudly."""
    errs = []
    for i, (x0, y0, x1, y1, b, t) in enumerate(S):
        for fi, x, y in ((b, x0, y0), (t, x1, y1)):
            if fi < 0:
                continue
            fx0, fx1, fy = P[fi][0], P[fi][1], P[fi][2]
            if abs(fy - y) > 0.01 or not (fx0 - 0.6 <= x <= fx1 + 0.6):
                errs.append(f"stair {i} doesn't meet floor {P[fi][4]} at ({x}, {y})")
    for i, a in enumerate(P):
        for j, b in enumerate(P):
            if j <= i or abs(a[2] - b[2]) > 0.01:
                continue
            if min(a[1], b[1]) - max(a[0], b[0]) > 0.01:
                errs.append(f"floors {a[4]} and {b[4]} overlap")

    def stands(x, y):
        for x0, x1, fy, _, _ in P:
            if abs(fy - y) < 0.05 and x0 - 0.3 <= x <= x1 + 0.3:
                return True
        return False
    for k, v in MARKS.items():
        if k in ("tower_window", "broken_step"):
            continue
        for x, y in (v if isinstance(v, list) else [v]):
            if not stands(x, y):
                errs.append(f"mark {k} at ({x}, {y}) isn't on a floor")
    if errs:
        raise SystemExit("KESSEL PLAN CHECK FAILED:\n  " + "\n  ".join(errs))
    print("kessel plan check OK:", len(P), "floors,", len(S), "stairs,", len(L), "ledges,", len(LEVELS), "levels")


if __name__ == "__main__":
    check()
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    path = os.path.join(root, "Assets", "Scripts", "Level", "KesselPlan.cs")
    with open(path, "w") as f:
        f.write(cs())
    with open(os.path.join(root, "Assets", "Art", "FortressV3", "kessel_plan.json") if os.path.isdir(os.path.join(root, "Assets", "Art", "FortressV3")) else os.devnull, "w") as f:
        json.dump(dict(platforms=P, stairs=S, ledges=L, levels=LEVELS), f)
    print("wrote", path)
