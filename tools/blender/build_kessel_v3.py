"""Chapter 6 v3: KESSEL HILL, the ascent (docs/story/chapter6_fortress_v3.md). Modelled on the layout in
kessel_plan.py (the same numbers drive the game's floors), baked twice like every story set: the storm
night, and lit by a lightning strike (_Lightning), which the game flickers between.

    /home/user/.venv-blender/bin/python tools/blender/build_kessel_v3.py [--preview] [--bake] [--export]

Rules the set keeps (so nothing is walked through):
  · the lane is y −1.3 … 1.3 (Blender y = depth): only floors, steps and the edges of things live in it
  · walls, pillars, statues, furniture stand behind it (y > 1.6) or are low and in front (y < −1.8)
  · interiors have no front wall: the camera looks in, the way a stage set is built"""
import math
import os
import random
import sys

import bpy  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402
import kessel_plan as K  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "FortressV3")
rnd = random.Random(61)
BACK = 2.2          # where the back of the lane starts (y): every wall is at least this far behind


def M_():
    M = S.materials()
    M["stone"] = D.stone("KStone", 0.34, 0.1, "xz")
    M["stone_y"] = D.stone("KStone", 0.34, 0.1, "yz")
    M["stone_dark"] = D.stone("KStoneDark", 0.2, 0.07, "xz")
    M["flags"] = D.stone("KFlags", 0.26, 0.09, "xy", block=(0.9, 0.9))
    M["steps"] = D.stone("KSteps", 0.3, 0.08, "xy", block=(0.5, 1.2))
    M["cobbles"] = D.cobbles("KCobbles", 0.22, wet=0.7)
    M["rock"] = D.rock("KRock", 0.2, "xz")
    M["rock_top"] = D.rock("KRock", 0.18, "xy")
    M["road"] = D.flat("KRoad", 0.07, rough=0.25)
    M["planks"] = D.planks("KPlanks", 0.14, "xy")
    M["planks_wall"] = D.planks("KPlanks", 0.14, "xz")
    M["iron"] = D.flat("KIron", 0.06, rough=0.45, metal=0.6)
    M["bark"] = D.flat("KBark", 0.07)
    M["needles"] = D.flat("KNeedles", 0.045, rough=0.95)
    M["cloth"] = D.flat("KBanner", 0.09, rough=1.0)
    M["cloth_pale"] = D.flat("KBannerMark", 0.6, rough=1.0)
    M["paper"] = D.flat("KPaper", 0.75, rough=1.0)
    M["lacquer"] = D.flat("KLacquer", 0.05, rough=0.2)
    M["fire"] = D.flat("KFire", 1.0, emit=22.0)
    M["window"] = D.flat("KWindow", 0.6, emit=1.6)
    M["maya"] = D.flat("KMayaWindow", 1.0, emit=6.0)
    M["candle"] = D.flat("KCandle", 1.0, emit=12.0)
    M["flood"] = D.flat("KFlood", 1.0, emit=30.0)
    M["town"] = D.flat("KTown", 1.0, emit=8.0)
    M["truck"] = D.flat("KTruck", 0.05, rough=0.35, metal=0.4)
    M["glass"] = D.flat("KGlass", 0.04, rough=0.1)
    M["copper"] = D.flat("KCopper", 0.22, rough=0.4, metal=0.7)
    BODY["stone"] = M["stone"]
    return M


BODY = {}


def floor_slab(g, x0, x1, z, m, front=-1.6, back=4.0, thick=1.0):
    """A floor of the plan: a cut-stone body (its face to the camera is masonry), the surface on top."""
    if thick > 0.3:
        g.box(((x0 + x1) / 2, (front + back) / 2, z - 0.04 - (thick - 0.08) / 2), (x1 - x0, back - front, thick - 0.08), BODY["stone"])
        g.box(((x0 + x1) / 2, (front + back) / 2, z - 0.04), (x1 - x0, back - front, 0.08), m)
    else:
        g.box(((x0 + x1) / 2, (front + back) / 2, z - thick / 2), (x1 - x0, back - front, thick), m)


def open_steps(g, x0, z0, x1, z1, m, front=-1.3, back=0.6, n=None):
    """An open flight (treads on a stringer, nothing under it): a floor runs on below it, and a man
    walking there must not walk through a solid block."""
    n = n or max(4, int(abs(z1 - z0) / 0.2))
    for i in range(n):
        u = (i + 1) / n
        xa, xb = x0 + (x1 - x0) * i / n, x0 + (x1 - x0) * u
        z = z0 + (z1 - z0) * u
        g.box(((xa + xb) / 2, (front + back) / 2, z - 0.04), (abs(xb - xa) + 0.04, back - front, 0.08), m)
    g.beam((x0, back + 0.06, z0), (x1, back + 0.06, z1), 0.08, 0.3, m)       # the stringer, behind the lane


def steps(g, x0, z0, x1, z1, m, front=-1.4, back=2.0, n=None, base=None):
    """A flight of real steps from (x0, z0) to (x1, z1) (either direction)."""
    n = n or max(4, int(abs(z1 - z0) / 0.2))
    for i in range(n):
        u0, u1 = i / n, (i + 1) / n
        xa, xb = x0 + (x1 - x0) * u0, x0 + (x1 - x0) * u1
        z = z0 + (z1 - z0) * u1
        lo = min(z0, z1) - 1.0 if base is None else base
        g.box(((xa + xb) / 2, (front + back) / 2, (z - 0.06 + lo) / 2), (abs(xb - xa) + 0.02, back - front, z - 0.06 - lo), BODY["stone"])
        g.box(((xa + xb) / 2, (front + back) / 2, z - 0.03), (abs(xb - xa) + 0.04, back - front + 0.04, 0.06), m)   # the tread


def lion(g, x, z, M):
    """A stone lion on a plinth (behind the lane): the great stair's guardians."""
    y = BACK + 0.6
    g.box((x, y, z + 0.6), (1.2, 1.2, 1.2), M["stone_dark"])
    g.box((x, y, z + 1.3), (1.0, 1.4, 0.4), M["stone"])                     # the body, couchant
    g.box((x + 0.42, y, z + 1.78), (0.5, 0.55, 0.6), M["stone"])            # the head and mane
    g.cyl((x - 0.5, y + 0.3, z + 1.5), 0.08, 0.6, M["stone"], rot=(0, 60, 0), n=8)   # the tail


def lantern(g, x, y, z, M):
    g.cyl((x, y, z), 0.05, 2.2, M["iron"], n=6)
    g.box((x, y, z + 1.25), (0.3, 0.3, 0.4), M["iron"])
    g.box((x, y, z + 1.25), (0.2, 0.2, 0.3), M["fire"])


def banner(g, x, y, z, h, M):
    g.box((x, y, z - h / 2), (1.1, 0.04, h), M["cloth"])
    g.box((x, y - 0.03, z - h * 0.35), (0.46, 0.02, 0.46), M["cloth_pale"])


# ---------------------------------------------------------------- L1 the switchback road
def build_road(coll, M):
    g = S.Group("KRoad")
    x0, x1 = 296.0, 384.0
    floor_slab(g, 296.0, 344.0, 0.0, M["road"], front=-3.2, back=BACK)          # the lower road
    D.terrain(g, x0, 352, -16, -3.2, lambda x, y: -(abs(y) - 3.2) * 1.4, M["rock_top"], res=1.2, amp=1.0, seed=2.0)
    for x in range(298, 344, 2):                                                # the edge: posts and a rail, the drop beyond
        g.box((x, -3.0, 0.4), (0.16, 0.16, 0.8), M["stone_dark"])
    g.beam((298, -3.0, 0.76), (343, -3.0, 0.76), 0.07, 0.05, M["iron"])
    # the retaining wall under the upper road (behind the lower one): cut stone, a drain, ivy dark
    g.box(((322 + 352) / 2, BACK + 0.4, 1.7), (30.0, 0.8, 3.4), M["stone"])
    g.box((322.0, BACK + 0.4, 1.7), (0.6, 0.8, 3.4), M["stone"])                  # its end (behind the lane: the lower road runs on)
    floor_slab(g, 322.0, 384.0, 3.4, M["road"], front=-1.4, back=BACK + 2.4, thick=0.5)   # the upper road over it
    for x in range(323, 380, 2):
        g.box((x, -1.5, 3.8), (0.16, 0.16, 0.8), M["stone_dark"])
    g.beam((323, -1.5, 4.16), (379, -1.5, 4.16), 0.07, 0.05, M["iron"])
    steps(g, 344.0, 0.0, 352.0, 3.4, M["road"], front=-3.0, back=-1.45, n=12)    # the bend (seen as a ramp)
    # the rock face above the upper road, pines on the slope
    D.terrain(g, 318, x1, BACK + 2.4, 18, lambda x, y: 3.4 + max(0.0, y - BACK - 2.4) ** 1.3 * 0.9, M["rock_top"], res=1.0, amp=0.8, seed=5.0)
    for x in range(320, 382, 5):
        y = rnd.uniform(6, 12)
        D.pine(g, x + rnd.uniform(-1.5, 1.5), y, 3.4 + max(0.0, y - BACK - 2.4) ** 1.3 * 0.9 - 0.4, rnd.uniform(6, 10), M["bark"], M["needles"])
    for x in range(298, 322, 4):                                                # the cliff behind the start
        D.boulder(g, (x + rnd.uniform(-1, 1), BACK + 1.2 + rnd.uniform(0, 1), rnd.uniform(0.4, 2.4)), rnd.uniform(0.9, 1.6), M["rock"])
    # the floodlight on its pole, the milestone
    fx, _ = K.MARKS["floodlight_road"]
    g.cyl((fx, BACK + 0.2, 3.0), 0.08, 6.0, M["iron"], n=8)
    g.box((fx, BACK - 0.1, 6.0), (0.9, 0.5, 0.4), M["iron"])
    g.box((fx, BACK - 0.36, 5.95), (0.7, 0.04, 0.28), M["flood"])
    g.box((306.0, BACK + 0.3, 0.5), (0.5, 0.35, 1.0), M["stone"])
    # the valley far below: the town's lights
    for _ in range(220):
        g.box((rnd.uniform(240, 760), rnd.uniform(-90, -50), rnd.uniform(-40, -30)), (0.25, 0.1, 0.18), M["town"])
    return g.finish(coll)


def build_truck(coll, M):
    """The gang's truck (it rolls): its own part, so the game can move it."""
    g = S.Group("KTruck")
    tx = 0.0
    g.box((tx, 1.0, 1.3), (5.5, 2.2, 2.2), M["truck"])
    g.box((tx + 3.4, 1.0, 0.95), (1.4, 2.1, 1.5), M["truck"])
    g.box((tx + 3.9, -0.08, 1.25), (0.5, 0.02, 0.5), M["glass"])
    for wx in (tx - 1.8, tx + 3.0):
        for wy in (-0.1, 2.1):
            g.cyl((wx, wy, 0.42), 0.42, 0.3, M["iron"], rot=(90, 0, 0), n=16)
    ob = g.finish(coll)
    ob.location = (K.MARKS["truck"][0], BACK + 0.3, 0.0)
    return ob


# ---------------------------------------------------------------- L2 the great stair
def build_stair(coll, M):
    g = S.Group("KStair")
    for (sx0, sy0, sx1, sy1, b, t) in K.S[1:4]:
        steps(g, sx0, sy0, sx1, sy1, M["steps"], front=-1.6, back=BACK + 0.4, base=2.0)
        # the balustrade behind, rising with the flight
        g.beam((sx0, BACK + 0.6, sy0 + 1.0), (sx1, BACK + 0.6, sy1 + 1.0), 0.18, 0.18, M["stone"])
        for k in range(6):
            u = k / 5
            g.box((sx0 + (sx1 - sx0) * u, BACK + 0.6, sy0 + (sy1 - sy0) * u + 0.5), (0.14, 0.14, 1.0), M["stone"])
    for i in (2, 3):
        x0, x1, z = K.P[i][0], K.P[i][1], K.P[i][2]
        floor_slab(g, x0, x1, z, M["flags"], back=BACK + 1.4, thick=z + 1)
    for lx, lz in K.MARKS["lion"]:
        lion(g, lx, lz, M)
    for lx, lz in ((385.0, 5.4), (401.0, 9.4), (416.0, 13.4)):
        lantern(g, lx, BACK + 0.6, lz, M)
    # the hill rising behind, the fortress's walls high above (the goal in view)
    D.terrain(g, 378, 424, BACK + 2.0, 20, lambda x, y: 3.4 + (x - 378) * 0.27 + (y - BACK - 2) * 0.6, M["rock_top"], res=1.2, amp=0.9, seed=7.0)
    g.box((432.0, 16.0, 22.0), (60.0, 2.0, 14.0), M["stone"])                     # the curtain wall, far up
    D.merlons(g, 402, 462, 15.2, 29.0, 1.0, M["stone"])
    return g.finish(coll)


# ---------------------------------------------------------------- L3 the barbican, and the gatehouse
def build_barbican(coll, M):
    g = S.Group("KBarbican")
    x0, x1, z = K.P[4][0], K.P[4][1], K.P[4][2]
    floor_slab(g, x0, x1, z, M["flags"], back=BACK + 3, thick=4.0)
    g.box((446.0, 0.5, z - 6.0), (4.0, 6.0, 0.6), M["rock_top"])                # the moat's floor, far down
    for x in (444.2, 447.8):
        g.box((x, 0.5, z - 3.0), (0.4, 6.0, 6.0), M["stone_y"])
    # the drawbridge, UP: standing against the gatehouse, its chains
    g.box((447.4, 0.0, z + 3.8), (0.3, 2.6, 7.4), M["planks_wall"])
    cx, cz = K.MARKS["drawbridge_chain"]
    g.beam((cx, -0.4, cz + 0.2), (448.0, -0.4, 21.6), 0.07, 0.07, M["iron"])
    g.beam((cx, 0.6, cz + 0.2), (448.0, 0.6, 21.6), 0.07, 0.07, M["iron"])
    # the gatehouse front: two towers, the wall between with machicolations over the forecourt
    for tx in (446.0, 470.0):
        D.tower(g, (tx, BACK + 2.4, z), 2.6, 12.0, M["stone"], M["stone_dark"], M["iron"])
    g.box((458.0, BACK + 0.6, z + 9.0), (24.0, 1.2, 18.0), M["stone"])
    g.box((448.4, 1.6, z + 5.6), (0.8, 2.2, 1.2), M["window"])                     # the window the chain leads to
    for hx, hz in K.MARKS["murder_holes"]:                                         # the corbelled gallery over the forecourt
        g.box((hx, -0.2, z + 6.6), (1.6, 3.0, 0.4), M["stone_dark"])
        g.box((hx, -0.2, z + 6.35), (0.6, 0.6, 0.1), M["iron"])
    g.box(((x0 + x1) / 2, -0.2, z + 7.2), (x1 - x0, 3.2, 0.9), M["stone"])
    for bx in (424.0, 438.0):                                                      # braziers
        g.cyl((bx, BACK + 0.4, z + 0.6), 0.08, 1.2, M["iron"], n=8)
        g.cyl((bx, BACK + 0.4, z + 1.3), 0.35, 0.3, M["iron"], n=12, r2=0.25)
        g.cyl((bx, BACK + 0.4, z + 1.5), 0.25, 0.25, M["fire"], n=10, r2=0.05)
    for bx in (428.0, 436.0):
        banner(g, bx, BACK + 1.2, z + 6.0, 3.2, M)
    return g.finish(coll)


# ---------------------------------------------------------------- L4 the gate passage (inside)
def build_passage(coll, M):
    g = S.Group("KPassage")
    x0, x1, z = K.P[6][0], K.P[6][1], K.P[6][2]
    floor_slab(g, x0, x1, z, M["flags"], back=BACK + 0.6, thick=4.0)
    g.box(((x0 + x1) / 2, BACK + 0.8, z + 3.0), (x1 - x0, 0.6, 6.0), M["stone_dark"])     # the passage's back wall
    for k in range(8):                                                                   # its vault, in ribs
        xx = x0 + 1.5 + k * 3.0
        g.box((xx, 0.4, z + 5.4), (0.4, 3.6, 0.4), M["stone"])
    g.box(((x0 + x1) / 2, 0.4, z + 5.8), (x1 - x0, 3.6, 0.4), M["stone"])               # the ceiling (the room above)
    # the room above (the chain's window, the murder holes in its floor)
    rx0, rx1, rz = K.P[5][0], K.P[5][1], K.P[5][2]
    floor_slab(g, rx0, rx1, rz, M["planks"], back=BACK + 0.6, thick=0.3)
    g.box(((rx0 + rx1) / 2, BACK + 0.8, rz + 2.0), (rx1 - rx0, 0.6, 4.0), M["stone_dark"])
    hx, _ = K.MARKS["murder_hole_drop"]
    g.box((hx, 0.0, rz - 0.15), (1.0, 1.0, 0.32), M["iron"])                              # the hole's grate
    for tx in (rx0 + 2, rx1 - 2):
        g.box((tx, BACK + 0.2, rz + 1.6), (0.2, 0.2, 0.5), M["iron"])
        g.box((tx, BACK + 0.2, rz + 1.95), (0.15, 0.15, 0.2), M["fire"])
    # the bend: the passage turns (a pier juts from the back wall), torches, the windlass
    g.box((460.0, BACK + 0.2, z + 2.5), (1.6, 1.0, 5.0), M["stone"])
    for tx in (452.0, 464.0, 468.0):
        g.box((tx, BACK + 0.5, z + 2.2), (0.2, 0.3, 0.5), M["iron"])
        g.box((tx, BACK + 0.4, z + 2.55), (0.16, 0.16, 0.24), M["fire"])
    wx, _ = K.MARKS["windlass"]
    g.cyl((wx, BACK + 0.2, z + 1.0), 0.45, 1.4, M["planks_wall"], rot=(90, 0, 0), n=16)
    for k in range(4):
        a = k * math.pi / 2
        g.beam((wx, BACK - 0.6, z + 1.0), (wx + math.cos(a) * 0.9, BACK - 0.6, z + 1.0 + math.sin(a) * 0.9), 0.08, 0.08, M["planks_wall"])
    # the stair up to the wall walk, at the end
    steps(g, K.S[4][0], K.S[4][1], K.S[4][2], K.S[4][3], M["steps"], front=-1.4, back=BACK + 0.4)
    return g.finish(coll)


def build_portcullis(coll, M, name, x):
    g = S.Group(name)
    z = 15.4
    for k in range(7):
        g.box((-1.5 + k * 0.5, 0.0, 2.4), (0.08, 0.08, 4.6), M["iron"])
    for k in range(6):
        g.box((0.0, 0.0, 0.3 + k * 0.8), (3.2, 0.08, 0.08), M["iron"])
    for k in range(7):
        g.cyl((-1.5 + k * 0.5, 0.0, 0.08), 0.05, 0.2, M["iron"], n=6, r2=0.0)
    ob = g.finish(coll)
    ob.location = (x, 0.9, z)
    ob.rotation_euler = (0, 0, math.radians(90))      # across the lane, seen edge-on: its bars face the camera side-on
    return ob


# ---------------------------------------------------------------- L5 the wall walk, L6 the wall tower
def build_wall(coll, M):
    g = S.Group("KWall")
    x0, x1, z = K.P[7][0], K.P[7][1], K.P[7][2]
    # the curtain wall: its walk, the parapet with merlons on the cliff side (behind), the low parapet
    # on the courtyard side (in front: below the camera's eye, a drop beyond)
    g.box(((x0 + x1) / 2, 0.8, z - 3.6), (x1 - x0 + 2, 5.0, 7.2), M["stone"])
    floor_slab(g, x0, x1, z, M["flags"], front=-1.7, back=BACK + 0.4, thick=0.4)
    g.box(((x0 + x1) / 2, BACK + 0.8, z + 0.55), (x1 - x0, 0.6, 1.1), M["stone"])
    D.merlons(g, x0, x1, BACK + 0.8, z + 1.1, 0.6, M["stone"], w=0.9, gap=0.6, h=1.0)
    g.box(((x0 + x1) / 2, -1.95, z + 0.3), (x1 - x0, 0.4, 0.6), M["stone"])
    # hoardings: wooden galleries hung off the wall's outer face, with their beams
    for hx in (488.0, 504.0):
        g.box((hx, BACK + 1.6, z + 2.2), (6.0, 1.4, 2.6), M["planks_wall"])
        g.box((hx, BACK + 1.6, z + 3.7), (6.4, 1.8, 0.2), M["planks"])
    # the towers at its ends (the doors men come out of)
    for tx in (478.0, 522.0):
        D.tower(g, (tx, BACK + 2.4, z - 8.0), 3.0, 14.0, M["stone"], M["stone_dark"], M["iron"])
        g.box((tx + (2 if tx < 500 else -2), BACK + 0.2, z + 1.2), (1.2, 0.3, 2.4), M["stone_dark"])   # its door
    for bx in (492.0, 500.0, 512.0):
        banner(g, bx, BACK + 1.15, z + 3.4, 3.0, M)
    # L6 the wall tower's stair down to the courtyard, inside the tower's cutaway
    sx0, sy0, sx1, sy1 = K.S[5][0], K.S[5][1], K.S[5][2], K.S[5][3]
    steps(g, sx1, sy1, sx0, sy0, M["steps"], front=-1.4, back=BACK + 0.4)
    g.box(((sx0 + sx1) / 2, BACK + 0.8, (sy0 + sy1) / 2 + 2.0), (abs(sx1 - sx0) + 1.0, 0.6, abs(sy1 - sy0) + 5.0), M["stone_dark"])
    g.box((sx0 - 4.0, BACK + 0.4, 18.5), (0.2, 0.3, 0.5), M["iron"])
    g.box((sx0 - 4.0, BACK + 0.3, 18.85), (0.16, 0.16, 0.24), M["fire"])
    return g.finish(coll)


# ---------------------------------------------------------------- L7 the courtyard
def build_court(coll, M):
    g = S.Group("KCourt")
    x0, x1, z = K.P[8][0], K.P[8][1], K.P[8][2]
    D.terrain(g, x0, x1, -1.8, BACK + 3.0, lambda x, y: z, M["cobbles"], res=0.7, amp=0.04)
    g.box(((x0 + x1) / 2, 0.6, z - 2.0), (x1 - x0, 6.0, 4.0), M["stone"])
    # the well
    wx, _ = K.MARKS["well"]
    g.cyl((wx, BACK + 0.9, z + 0.45), 0.9, 0.9, M["stone"], n=20)
    for s in (-1, 1):
        g.box((wx + s * 0.8, BACK + 0.9, z + 1.6), (0.12, 0.12, 1.4), M["planks_wall"])
    g.beam((wx - 0.8, BACK + 0.9, z + 2.2), (wx + 0.8, BACK + 0.9, z + 2.2), 0.1, 0.1, M["planks_wall"])
    # the stable and its loft (the loft floor is a plan floor; the roof slopes down to the yard)
    lx0, lx1, lz = K.P[9][0], K.P[9][1], K.P[9][2]
    floor_slab(g, lx0, lx1, lz, M["planks"], front=-1.4, back=BACK + 1.6, thick=0.25)
    for px in (lx0 + 0.2, (lx0 + lx1) / 2, lx1 - 0.2):
        g.box((px, BACK + 1.4, (z + lz) / 2), (0.25, 0.25, lz - z), M["planks_wall"])
    g.box(((lx0 + lx1) / 2, BACK + 2.0, (z + lz) / 2 + 1.5), (lx1 - lx0, 0.3, lz - z + 3.0), M["planks_wall"])
    open_steps(g, K.S[6][2], K.S[6][3], K.S[6][0], K.S[6][1], M["planks"], front=-1.3, back=0.8, n=10)   # the roof's slope (the yard runs on under it)
    for k in range(9):                                                     # the ladder at the loft's edge
        g.box((lx0 - 0.15, -0.6, z + 0.3 + k * 0.36), (0.1, 0.5, 0.05), M["planks_wall"])
    for s in (-1, 1):
        g.box((lx0 - 0.15, -0.6 + s * 0.25, (z + lz) / 2), (0.06, 0.06, lz - z + 0.4), M["planks_wall"])
    hx, _ = K.MARKS["hay_hoist"]
    g.beam((hx, -0.2, lz + 3.2), (hx, BACK + 2.0, lz + 3.2), 0.16, 0.16, M["planks_wall"])
    g.box((hx, -0.2, lz + 2.7), (0.9, 0.7, 0.7), M["planks"])                # the bale on its rope (above head height on the loft)
    # the cart, carts and barrels
    cx, _ = K.MARKS["cart"]
    g.box((cx, BACK + 0.8, z + 0.7), (2.8, 1.4, 0.5), M["planks_wall"])
    for wx2 in (cx - 1.1, cx + 1.1):
        g.cyl((wx2, BACK + 0.0, z + 0.6), 0.6, 0.1, M["planks"], rot=(90, 0, 0), n=16)
    for k in range(4):
        g.cyl((565.0 + k * 0.75, BACK + 0.8, z + 0.45), 0.34, 0.9, M["planks_wall"], n=14)
    # the generator shed, the floodlights on poles
    gx, _ = K.MARKS["generator"]
    g.box((gx, BACK + 1.4, z + 1.3), (4.0, 2.4, 2.6), M["stone_dark"])
    g.cyl((gx + 1.5, BACK + 1.8, z + 3.4), 0.12, 1.8, M["iron"], n=8)
    for fx, _ in K.MARKS["flood_court"]:
        g.cyl((fx, BACK + 0.4, z + 3.2), 0.08, 6.4, M["iron"], n=8)
        g.box((fx, BACK + 0.0, z + 6.3), (0.9, 0.5, 0.4), M["iron"])
        g.box((fx, BACK - 0.27, z + 6.25), (0.7, 0.04, 0.28), M["flood"])
    # the chapel: its steps, its door, its bell in the gable, the rope down to the yard
    bx, _ = K.MARKS["bell_rope"]
    g.box((bx - 1.0, BACK + 2.4, z + 4.5), (6.0, 2.4, 9.0), M["stone"])
    g.box((bx - 1.0, BACK + 1.15, z + 1.4), (1.4, 0.1, 2.8), M["planks_wall"])
    g.box((bx - 1.0, BACK + 1.6, z + 10.0), (1.6, 1.4, 2.0), M["stone"])
    g.cyl((bx - 1.0, BACK + 1.6, z + 9.6), 0.45, 0.7, M["copper"], n=14, r2=0.25)
    g.beam((bx, -0.2, z + 9.4), (bx, -0.2, z + 1.0), 0.03, 0.03, M["planks_wall"])
    # the keep's face beyond: lit windows, the doorway into the gallery
    g.box((x1 + 0.2, BACK + 0.6, z + 9.0), (0.6, 4.0, 18.0), M["stone"])
    for wx3 in (538.0, 552.0, 566.0):
        g.box((wx3, BACK + 3.9, z + 7.0), (1.0, 0.05, 2.4), M["window"])
    D.merlons(g, x0, x1, BACK + 3.4, z + 12.0, 0.6, M["stone"])
    g.box(((x0 + x1) / 2, BACK + 3.4, z + 6.0), (x1 - x0, 0.6, 12.0), M["stone"])
    return g.finish(coll)


# ---------------------------------------------------------------- L8 Voss's gallery
def build_gallery(coll, M):
    g = S.Group("KGallery")
    x0, x1, z = K.P[10][0], K.P[10][1], K.P[10][2]
    floor_slab(g, x0, x1, z, M["planks"], back=BACK + 2.0, thick=4.0)
    g.box(((x0 + x1) / 2, BACK + 2.2, z + 3.5), (x1 - x0, 0.4, 7.0), M["planks_wall"])          # dark wood panelling
    g.box(((x0 + x1) / 2, 0.6, z + 7.2), (x1 - x0, 4.0, 0.4), M["planks"])                       # the ceiling
    # the wall of katanas: rack after rack, blades in their scabbards, edges up
    for k in range(18):
        kx = x0 + 2.0 + k * 1.3
        for r in range(3):
            g.beam((kx - 0.45, BACK + 1.95, z + 2.4 + r * 0.9), (kx + 0.45, BACK + 1.95, z + 2.55 + r * 0.9), 0.03, 0.05, M["lacquer"])
    # suits of armour on stands, behind the lane
    for ax in (588.0, 600.0):
        g.box((ax, BACK + 1.1, z + 0.4), (0.8, 0.8, 0.8), M["planks_wall"])
        g.box((ax, BACK + 1.1, z + 1.4), (0.7, 0.45, 1.0), M["lacquer"])
        for s in (-1, 1):
            g.box((ax + s * 0.45, BACK + 1.1, z + 1.6), (0.3, 0.3, 0.5), M["lacquer"])
        g.cyl((ax, BACK + 1.1, z + 2.15), 0.26, 0.4, M["lacquer"], n=12, r2=0.18)
        g.box((ax, BACK + 1.1, z + 2.4), (0.8, 0.1, 0.18), M["copper"])           # the helmet's crest
    # glass cases, low, behind
    for cx in (594.0, 606.0):
        g.box((cx, BACK + 0.8, z + 0.5), (1.6, 0.7, 1.0), M["planks_wall"])
        g.box((cx, BACK + 0.8, z + 1.2), (1.5, 0.6, 0.4), M["glass"])
    # the photograph (the hero at twenty, kneeling, the wooden sword): a frame on an easel
    px, _ = K.MARKS["photo"]
    g.box((px, BACK + 0.6, z + 1.6), (0.7, 0.05, 0.5), M["window"])
    g.box((px, BACK + 0.62, z + 1.6), (0.8, 0.04, 0.6), M["lacquer"])
    for s in (-1, 1):
        g.beam((px + s * 0.3, BACK + 0.4, z), (px, BACK + 0.65, z + 1.9), 0.04, 0.04, M["planks_wall"])
    # lanterns, the paper screens' frames behind (the screens themselves are the game's: they tear)
    for lx in (590.0, 598.0, 606.0):
        g.box((lx, 0.6, z + 6.2), (0.5, 0.5, 0.7), M["paper"])
        g.box((lx, 0.6, z + 6.2), (0.3, 0.3, 0.5), M["candle"])
    return g.finish(coll)


# ---------------------------------------------------------------- L9 the great hall
def build_hall(coll, M):
    g = S.Group("KHall")
    x0, x1, z = K.P[11][0], K.P[11][1], K.P[11][2]
    top = z + 12.0
    floor_slab(g, x0, x1, z, M["flags"], back=BACK + 4.0, thick=4.0)
    g.box(((x0 + x1) / 2, BACK + 4.2, z + 6.0), (x1 - x0, 0.6, 12.0), M["stone"])           # the back wall
    for wx in (618.0, 630.0, 642.0, 654.0):                                                   # tall windows (the lightning comes through)
        g.box((wx, BACK + 3.85, z + 6.5), (1.6, 0.05, 5.0), M["window"])
        g.box((wx, BACK + 3.8, z + 9.2), (2.0, 0.1, 0.4), M["stone_dark"])
    # pillars: a row BEHIND the lane only (none on it): the old fortress's glitch was pillars in the lane
    for px in range(616, 660, 6):
        g.cyl((px, BACK + 1.6, z + 6.0), 0.55, 12.0, M["stone"], n=16)
        g.box((px, BACK + 1.6, z + 0.25), (1.4, 1.4, 0.5), M["stone_dark"])
    g.box(((x0 + x1) / 2, 0.8, top), (x1 - x0, 7.0, 0.6), M["planks"])                        # the roof beams' ceiling
    for bx in range(614, 662, 4):
        g.box((bx, 0.8, top - 0.4), (0.3, 7.0, 0.5), M["planks_wall"])
    # the long table, its benches and the Warden's chair: behind the lane (nobody walks through them)
    tx0, tx1, ty = 626.0, 640.0, BACK + 0.9
    g.box(((tx0 + tx1) / 2, ty, z + 0.86), (tx1 - tx0, 1.4, 0.12), M["planks"])
    for lx in (tx0 + 0.6, (tx0 + tx1) / 2, tx1 - 0.6):
        for s in (-1, 1):
            g.box((lx, ty + s * 0.55, z + 0.4), (0.14, 0.14, 0.8), M["planks_wall"])
    g.box(((tx0 + tx1) / 2, ty + 1.0, z + 0.45), (tx1 - tx0, 0.4, 0.1), M["planks"])
    g.box((tx1 + 0.8, ty, z + 0.8), (0.8, 0.8, 1.6), M["planks_wall"])
    g.box((tx1 + 1.1, ty, z + 2.0), (0.2, 0.8, 1.6), M["planks_wall"])
    # the minstrels' gallery and its stair
    mx0, mx1, mz = K.P[12][0], K.P[12][1], K.P[12][2]
    floor_slab(g, mx0, mx1, mz, M["planks"], front=-1.4, back=BACK + 4.0, thick=0.4)
    g.box(((mx0 + mx1) / 2, -1.45, mz + 0.5), (mx1 - mx0, 0.1, 1.0), M["planks_wall"])     # its balustrade (low, in front)
    for bx in range(int(mx0), int(mx1), 1):
        g.box((bx + 0.5, -1.45, mz + 0.5), (0.06, 0.06, 1.0), M["planks_wall"])
    open_steps(g, K.S[7][0], K.S[7][1], K.S[7][2], K.S[7][3], M["planks"], front=-1.3, back=0.6, n=12)   # open: the hall's floor runs on under it
    # the fireplace at the far end, big enough to stand in
    fx, _ = K.MARKS["fireplace"]
    g.box((fx, BACK + 3.6, z + 2.0), (3.6, 1.2, 4.0), M["stone_dark"])
    g.box((fx, BACK + 3.0, z + 0.5), (2.4, 0.4, 0.8), M["fire"])
    g.box((fx, BACK + 3.0, z + 4.2), (4.2, 1.6, 0.5), M["stone"])
    # banners
    for bx in (620.0, 636.0, 652.0):
        banner(g, bx, BACK + 3.8, z + 10.0, 4.0, M)
    return g.finish(coll)


def build_chandelier(coll, M, name, x):
    g = S.Group(name)
    g.torus((0, 0, 0), 1.0, 0.06, M["iron"], n1=24, n2=6)
    for k in range(10):
        a = k / 10 * math.tau
        g.cyl((math.cos(a), math.sin(a), 0.16), 0.035, 0.2, M["candle"], n=6)
    for k in range(3):
        a = k / 3 * math.tau
        g.beam((math.cos(a), math.sin(a), 0.0), (0, 0, 2.0), 0.03, 0.03, M["iron"])
    g.beam((0, 0, 2.0), (0, 0, 6.0), 0.04, 0.04, M["iron"])
    ob = g.finish(coll)
    ob.location = (x, 0.4, 15.4 + 5.8)
    return ob


# ---------------------------------------------------------------- L10 the tower stair, L11 the observatory
def build_tower(coll, M):
    g = S.Group("KTower")
    # the tower's drum, behind the lane: the stair winds up its face
    g.cyl((690.0, BACK + 5.0, 23.0), 6.0, 16.0, M["stone"], n=40)
    for k in range(12):
        a = k / 12 * math.pi + math.pi
        g.box((690.0 + math.cos(a) * 6.02, BACK + 5.0 + math.sin(a) * 6.02, 18.0 + (k % 3) * 4.0), (0.12, 0.06, 1.0), M["stone_dark"])
    for st in (K.S[8], K.S[9], K.S[10]):
        steps(g, st[0], st[1], st[2], st[3], M["steps"], front=-1.4, back=BACK + 0.6)
    for i in (13, 14, 16):
        x0, x1, z = K.P[i][0], K.P[i][1], K.P[i][2]
        floor_slab(g, x0, x1, z, M["steps"], back=BACK + 0.6, thick=1.0)
    # the gap: broken steps fallen away, their stubs, the drop
    bx, bz = K.MARKS["broken_step"]
    g.box((bx - 0.3, BACK + 0.2, bz - 1.4), (0.5, 1.2, 0.5), M["stone_dark"])
    for k in range(5):
        D.boulder(g, (bx + rnd.uniform(-2, 2), rnd.uniform(-1, 2), 16.0 + rnd.uniform(0, 1)), rnd.uniform(0.3, 0.6), M["stone"])
    # the outer rail (low, in front), the lightning rod at the top
    g.beam((660.0, -1.5, 16.2), (698.0, -1.5, 31.2), 0.06, 0.06, M["iron"])
    g.cyl((707.0, BACK + 5.0, 46.0), 0.06, 6.0, M["iron"], n=6)
    return g.finish(coll)


def build_dome(coll, M):
    g = S.Group("KDome")
    x0, x1, z = K.P[15][0], K.P[15][1], K.P[15][2]
    floor_slab(g, x0, x1, z, M["planks"], back=BACK + 4.0, thick=1.0)
    # the dome (behind and over): copper ribs, the open slot to the storm
    cx = (x0 + x1) / 2
    for k in range(16):
        a = k / 16 * math.pi
        g.beam((cx + math.cos(a) * 9.0, BACK + 3.0, z), (cx + math.cos(a) * 2.0, BACK + 3.0, z + 8.5 + math.sin(a) * 1.0), 0.18, 0.18, M["copper"])
    g.box((cx, BACK + 3.6, z + 4.0), (x1 - x0, 0.4, 8.0), M["copper"])
    g.box((cx + 1.5, BACK + 3.3, z + 6.0), (2.4, 0.2, 6.0), M["glass"])                     # the slot, open (rain beyond)
    # the gears and the crank by the wall
    kx, _ = K.MARKS["dome_crank"]
    g.cyl((kx, BACK + 0.8, z + 1.2), 0.9, 0.2, M["iron"], rot=(90, 0, 0), n=24)
    g.cyl((kx - 1.2, BACK + 0.8, z + 1.6), 0.5, 0.2, M["iron"], rot=(90, 0, 0), n=16)
    g.beam((kx, BACK + 0.6, z + 1.2), (kx + 0.6, BACK + 0.3, z + 1.2), 0.06, 0.06, M["iron"])
    # Maya's room: the lit window high in the tower (the goal, seen from below all chapter)
    wx, wz = K.MARKS["tower_window"]
    g.box((wx, BACK + 2.9, wz), (1.2, 0.1, 1.6), M["maya"])
    return g.finish(coll)


def build_telescope(coll, M):
    g = S.Group("KTelescope")
    g.cyl((0, 0, 0.9), 0.35, 1.8, M["iron"], n=12, r2=0.5)                  # the pier
    g.cyl((0, 0, 2.0), 0.4, 4.2, M["copper"], rot=(0, 55, 0), n=20, r2=0.32)  # the tube, tilted to the slot
    ob = g.finish(coll)
    ob.location = (K.MARKS["telescope"][0], BACK + 0.8, 30.4)          # behind the lane
    return ob


# ---------------------------------------------------------------- the backdrop: the storm, the hills, the tower
def build_back(coll, M):
    g = S.Group("KBack")
    D.terrain(g, 240, 780, 40, 140, lambda x, y: (y - 40) * 0.45 + math.sin(x * 0.02) * 8 + 4, M["rock_top"], res=6.0, amp=4.0, seed=11.0)
    # the fortress's skyline, far behind: walls, towers, and the observatory tower with HER window
    g.box((560.0, 60.0, 22.0), (240.0, 3.0, 20.0), M["stone_dark"])
    for tx in (440.0, 500.0, 580.0, 640.0):
        D.tower(g, (tx, 62.0, 12.0), 5.0, 30.0, M["stone_dark"], M["stone_dark"], M["iron"])
    g.cyl((707.0, 58.0, 30.0), 7.0, 60.0, M["stone_dark"], n=32)
    g.cyl((707.0, 58.0, 62.0), 8.0, 6.0, M["copper"], n=32, r2=2.0)
    g.box((707.0, 50.9, 52.0), (2.0, 0.2, 2.8), M["maya"])
    return g.finish(coll)


# ---------------------------------------------------------------- lights
def lights(lightning=False):
    def add(kind, name, loc, energy, size=1.0, rot=None, color=(1, 1, 1)):
        ld = bpy.data.lights.new(name, kind)
        ld.energy = energy
        ld.color = color
        if kind == "AREA":
            ld.size = size
        elif kind == "POINT":
            ld.shadow_soft_size = size
        ob = bpy.data.objects.new(name, ld)
        ob.location = loc
        if rot:
            ob.rotation_euler = [math.radians(a) for a in rot]
        bpy.context.scene.collection.objects.link(ob)
    add("SUN", "Moon", (0, 0, 0), 1.1 if not lightning else 7.0, rot=(52, 14, -30) if not lightning else (35, -25, 160),
        color=(0.78, 0.82, 0.9))
    # fires, torches, braziers, lanterns, floodlights, windows: every warm source is a point light
    for x, y, z, e in ((424.0, BACK + 0.4, 17.2, 400), (438.0, BACK + 0.4, 17.2, 400), (385.0, BACK + 0.6, 6.7, 200),
                       (401.0, BACK + 0.6, 10.7, 200), (416.0, BACK + 0.6, 14.7, 200), (452.0, BACK, 17.8, 220),
                       (464.0, BACK, 17.8, 220), (468.0, BACK, 17.8, 220), (450.0, BACK, 23.2, 160), (456.0, BACK, 23.2, 160),
                       (516.0, BACK, 18.9, 200), (658.0, BACK + 2.4, 16.2, 900), (590.0, 0.6, 21.6, 120), (598.0, 0.6, 21.6, 120),
                       (606.0, 0.6, 21.6, 120), (622.0, 0.4, 21.4, 260), (634.0, 0.4, 21.4, 260), (648.0, 0.4, 21.4, 260)):
        add("POINT", "Fire", (x, y, z), e, size=0.4, color=(1.0, 0.95, 0.88))
    for x, y, z in ((338.0, BACK - 0.6, 5.9), (538.0, BACK - 0.6, 21.6), (576.0, BACK - 0.6, 21.6)):
        add("SPOT", "Flood", (x, y, z), 1800, rot=(20, 0, 0))
    add("AREA", "Dome", (707.0, BACK + 2.0, 40.0), 600, size=6.0, rot=(180, 0, 0))
    # the storm's sky light from the camera's side: a soft fill over every level (the stage's front light)
    for (n, x0, x1, y) in K.LEVELS:
        add("AREA", "Fill", ((x0 + x1) / 2, -14.0, y + 9.0), 2600 if not lightning else 9000, size=max(14.0, x1 - x0),
            rot=(55, 0, 0), color=(0.8, 0.84, 0.92))


PARTS = {"KRoad": (2048, 12), "KStair": (2048, 12), "KBarbican": (2048, 12), "KPassage": (2048, 12), "KWall": (2048, 12),
         "KCourt": (2048, 12), "KGallery": (2048, 12), "KHall": (2048, 12), "KTower": (2048, 12), "KDome": (1024, 12),
         "KBack": (1024, 8), "KTruck": (512, 8), "KPortcullisA": (256, 8), "KPortcullisB": (256, 8),
         "KChandelier1": (256, 8), "KChandelier2": (256, 8), "KChandelier3": (256, 8), "KTelescope": (512, 8)}


def build_all(coll, M):
    parts = [build_road(coll, M), build_truck(coll, M), build_stair(coll, M), build_barbican(coll, M), build_passage(coll, M),
             build_portcullis(coll, M, "KPortcullisA", K.MARKS["portcullis_front"][0]),
             build_portcullis(coll, M, "KPortcullisB", K.MARKS["portcullis_back"][0]),
             build_wall(coll, M), build_court(coll, M), build_gallery(coll, M), build_hall(coll, M),
             build_chandelier(coll, M, "KChandelier1", K.MARKS["chandeliers"][0][0]),
             build_chandelier(coll, M, "KChandelier2", K.MARKS["chandeliers"][1][0]),
             build_chandelier(coll, M, "KChandelier3", K.MARKS["chandeliers"][2][0]),
             build_tower(coll, M), build_dome(coll, M), build_telescope(coll, M), build_back(coll, M)]
    for o in parts:
        if o.name not in ("KBack",):
            D.bevel(o, 0.025)
    return parts


def preview(name):
    """Contact frames: a camera along the lane at each level (side-on, as the game sees it)."""
    from PIL import Image
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 10
    sc.cycles.device = "CPU"
    sc.render.resolution_x, sc.render.resolution_y = 640, 360
    cam_d = bpy.data.cameras.new("Cam")
    cam_d.lens = 35
    cam = bpy.data.objects.new("Cam", cam_d)
    bpy.context.scene.collection.objects.link(cam)
    sc.camera = cam
    tiles = []
    for (n, x0, x1, y) in K.LEVELS:
        cx = (x0 + x1) / 2
        cam.location = (cx, -17.0, y + 3.2)
        cam.rotation_euler = (math.radians(86), 0, 0)
        tmp = os.path.join(OUT, "_prev.png")
        sc.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        tiles.append(Image.open(tmp).convert("RGB").copy())
    W = 640 * 3
    rows = (len(tiles) + 2) // 3
    sheet = Image.new("RGB", (W, 360 * rows), (20, 20, 20))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % 3) * 640, (i // 3) * 360))
    path = os.path.join(ROOT, "docs", "previews", name)
    sheet.save(path)
    os.remove(tmp)
    print("preview", path)


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "KesselSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported KesselSet.fbx")


def main():
    os.makedirs(OUT, exist_ok=True)
    K.check()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = M_()
    parts = build_all(coll, M)
    if "--preview" in sys.argv:
        lights()
        preview("kessel_v3_levels.png")
        return
    if "--bake" in sys.argv:
        only = None
        for a in sys.argv:
            if a.startswith("--parts="):
                only = set(a.split("=", 1)[1].split(","))
        for o in parts:
            S.unwrap(o)
        for mode, suffix in (("night", ""), ("lightning", "_Lightning")):
            for ob in list(bpy.context.scene.objects):
                if ob.type == "LIGHT":
                    bpy.data.objects.remove(ob, do_unlink=True)
            lights(lightning=mode == "lightning")
            for o in parts:
                if only and o.name not in only:
                    continue
                size, smp = PARTS.get(o.name, (1024, 8))
                if mode == "lightning":
                    size, smp = max(256, size // 2), max(6, smp // 2)
                S.bake(o, size, smp, suffix)
    if "--export" in sys.argv:
        marks = []
        for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
            e = bpy.data.objects.new(n, None)
            e.location = p
            coll.objects.link(e)
            marks.append(e)
        export(parts + marks)


if __name__ == "__main__":
    main()
