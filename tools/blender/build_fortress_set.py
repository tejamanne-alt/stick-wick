"""Chapter 5: THE FORTRESS (docs/story/chapter5_fortress.md). Kessel Hill in a storm at night. One
side-on lane climbing the hill in four stretches, each flat, at two heights (the game walks him up
between them):

  F1 THE CLIFF ROAD   x 0-44,   z 0   a road cut into the hillside: the rock face, pines bent by the
                                      wind, a guard rail and the drop to the valley's lights, the
                                      gang's truck, a floodlight
     the rock stair   x 44-52         cut into the cliff, up to the gate
  F2 THE GATE         x 52-88,  z 6   the drawbridge on its chains over the ditch, twin towers, the
                                      gatehouse arch and its portcullis (separate: it falls), braziers
  F3 THE COURTYARD    x 88-128, z 6   cobbles, a well, carts, barrels, the generator shed and its
                                      floodlights, the keep's lit windows; the observatory tower above
  F4 THE GREAT HALL   x 128-166, z 6  inside: stone pillars, a long table, the fireplace, chandeliers,
                                      banners, the tall windows the lightning comes through; the stair
                                      door to the tower at the far end

Real surfaces from detail.py (cut stone, cobbles, rock, planks), bevelled edges, noise terrain.
Two bakes of every part: the storm-dark night, and lit by a lightning strike (_Lightning), which the
game flickers between (LightningSwap). The hall's front wall is cut away while the camera is out.

    /home/user/.venv-blender/bin/python tools/blender/build_fortress_set.py [--preview]"""
import math
import os
import random
import sys

import bpy  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402
import build_fortress_front as F  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Fortress")
rnd = random.Random(71)
UP = 6.0                       # the gate, courtyard and hall stand this much above the road
HALL_X0, HALL_X1 = 128.0, 166.0
HALL_TOP = UP + 12.0          # a hall as tall as a church
ROOF = HALL_TOP + 0.4         # the roof terrace (the last fight)
WALL_Y = 4.2                   # the curtain wall behind the lane
TOWER_X = 150.0                # the observatory tower behind the hall


def M_():
    M = S.materials()
    M["stone"] = D.stone("FortStone", 0.34, 0.1, "xz")
    M["stone_y"] = D.stone("FortStone", 0.34, 0.1, "yz")
    M["stone_dark"] = D.stone("FortStoneDark", 0.2, 0.07, "xz")
    M["stone_floor"] = D.stone("FortFlags", 0.26, 0.09, "xy", block=(0.9, 0.9))
    M["cobbles"] = D.cobbles("FortCobbles", 0.22, wet=0.7)
    M["rock"] = D.rock("FortRock", 0.2, "xz")
    M["rock_top"] = D.rock("FortRock", 0.18, "xy")
    M["road"] = D.flat("FortRoad", 0.07, rough=0.25)
    M["planks"] = D.planks("FortPlanks", 0.14, "xy")
    M["planks_wall"] = D.planks("FortPlanks", 0.14, "xz")
    M["iron"] = D.flat("FortIron", 0.06, rough=0.45, metal=0.6)
    M["bark"] = D.flat("FortBark", 0.07)
    M["needles"] = D.flat("FortNeedles", 0.045, rough=0.95)
    M["cloth"] = D.flat("FortBanner", 0.09, rough=1.0)
    M["cloth_pale"] = D.flat("FortBannerMark", 0.6, rough=1.0)
    M["fire"] = D.flat("FortFire", 1.0, emit=22.0)
    M["window"] = D.flat("FortWindow", 0.6, emit=1.4)
    M["candle"] = D.flat("FortCandle", 1.0, emit=12.0)
    M["flood"] = D.flat("FortFloodlight", 1.0, emit=30.0)
    M["town"] = D.flat("FortTownLights", 1.0, emit=8.0)
    M["sky"] = D.flat("FortStormSky", 0.06, emit=0.0)
    M["cloud"] = D.flat("FortCloud", 0.1)
    M["truck"] = D.flat("FortTruck", 0.05, rough=0.35, metal=0.4)
    M["glass"] = D.flat("FortGlass", 0.04, rough=0.1)
    return M


# ---------------------------------------------------------------- F1: the cliff road

def build_road(coll, M):
    g = S.Group("FortRoad")
    # the road surface (a little uneven), the verge, the rock face rising behind
    D.terrain(g, -8, 52, -2.8, 2.4, lambda x, y: 0.0, M["road"], res=0.8, amp=0.03)
    D.terrain(g, -8, 52, 2.4, 14.0, lambda x, y: (y - 2.4) ** 1.35 * 0.9, M["rock_top"], res=0.9, amp=0.8, seed=3.0)
    for x in range(-6, 52, 3):                                   # the cliff's face: boulders stacked into it
        D.boulder(g, (x + rnd.uniform(-1, 1), 3.4 + rnd.uniform(0, 1), rnd.uniform(0.3, 2.6)), rnd.uniform(0.9, 1.6), M["rock"])
    # the edge: a low wall and posts with a rail, the drop beyond
    for x in range(-6, 46, 2):
        g.box((x, -2.95, 0.4), (0.18, 0.18, 0.8), M["stone_dark"])
    g.beam((-6, -2.95, 0.78), (45, -2.95, 0.78), 0.08, 0.06, M["iron"])
    D.terrain(g, -8, 52, -14.0, -3.1, lambda x, y: -(abs(y) - 3.1) * 1.6, M["rock_top"], res=1.2, amp=1.0, seed=8.0)
    # pines up the slope, leaning with the wind
    for x in range(-4, 50, 5):
        y = rnd.uniform(5, 11)
        D.pine(g, x + rnd.uniform(-1.5, 1.5), y, (y - 2.4) ** 1.35 * 0.9 - 0.4, rnd.uniform(6, 10), M["bark"], M["needles"])
    # the gang's truck, a floodlight on a pole, a milestone
    tx = 28.0
    g.box((tx, 1.6, 1.3), (5.5, 2.2, 2.2), M["truck"])
    g.box((tx + 3.4, 1.6, 0.95), (1.4, 2.1, 1.5), M["truck"])
    g.box((tx + 3.9, 0.52, 1.25), (0.5, 0.02, 0.5), M["glass"])
    for wx in (tx - 1.8, tx + 3.0):
        for wy in (0.5, 2.7):
            g.cyl((wx, wy, 0.42), 0.42, 0.3, M["iron"], rot=(90, 0, 0), n=16)
    g.cyl((38.0, 2.2, 3.0), 0.08, 6.0, M["iron"], n=8)
    g.box((38.0, 1.7, 6.0), (0.9, 0.5, 0.4), M["iron"])
    g.box((38.0, 1.42, 5.95), (0.7, 0.04, 0.28), M["flood"])
    g.box((12.0, 2.0, 0.5), (0.5, 0.35, 1.0), M["stone"])
    # the rock stair up to the gate (x 44-52), cut into the cliff, its steps worn
    n = 14
    for i in range(n):
        x = 44.0 + 8.0 * i / n
        z = UP * (i + 1) / n
        g.box((x + 0.3, 0.4, z / 2), (8.0 / n, 3.4, z), M["stone_floor"])
    D.boulder(g, (45.0, -2.4, 1.5), 1.6, M["rock"])
    D.boulder(g, (49.5, -2.6, 4.0), 1.8, M["rock"])
    # the valley far below: the town's lights in the rain
    for _ in range(160):
        g.box((rnd.uniform(-60, 220), rnd.uniform(-70, -40), rnd.uniform(-34, -26)), (0.25, 0.1, 0.18), M["town"])
    return g.finish(coll)


# ---------------------------------------------------------------- F2: the gate

def build_gate(coll, M):
    g = S.Group("FortGate")
    z = UP
    # the ground before the gate and the ditch the drawbridge crosses
    g.box((54.5, 0.0, z - 0.5), (5.0, 7.0, 1.0), M["stone_floor"])
    g.box((61.0, 0.0, z - 5.0), (8.0, 7.0, 0.5), M["rock_top"])                      # the ditch floor, far down
    for x in (57.0, 65.0):
        g.box((x, 0.0, z - 2.5), (0.6, 7.0, 5.0), M["stone_y"])
    g.box((61.0, 0.0, z - 0.12), (8.0, 2.6, 0.24), M["planks"])                     # the drawbridge
    for s in (-1, 1):
        g.beam((57.2, s * 1.2, z + 0.05), (64.8, s * 1.2, z + 0.05), 0.2, 0.2, M["planks_wall"])
        g.beam((57.4, s * 1.2, z + 0.2), (66.0, s * 1.6, z + 6.2), 0.05, 0.05, M["iron"])   # its chains up to the gatehouse
    g.box((76.5, 0.0, z - 0.5), (23.0, 7.0, 1.0), M["stone_floor"])                 # the barbican floor
    # the curtain wall behind the lane, with battlements and a wall walk
    g.box(((52 + 88) / 2, WALL_Y + 0.6, z + 4.0), (88 - 52, 1.2, 8.0), M["stone"])
    D.merlons(g, 52, 88, WALL_Y + 0.1, z + 8.0, 0.5, M["stone"])
    # the gatehouse: an arch over the lane, its portcullis slot, two round towers either side
    gx = 69.0
    g.box((gx, -2.0, z + 6.0), (6.0, 1.2, 4.0), M["stone"])                          # the wall over the arch (front)
    for s in (-1, 1):
        g.box((gx + s * 2.6, -2.0, z + 2.0), (0.8, 1.2, 4.0), M["stone"])            # its jambs
    for k in range(9):                                                              # the voussoirs of the arch
        a0, a1 = math.pi * k / 9, math.pi * (k + 1) / 9
        am = (a0 + a1) / 2
        g.box((gx + math.cos(am) * 2.2, -2.0, z + 4.0 + math.sin(am) * 2.2 - 0.6), (0.5, 1.25, 0.82), M["stone_dark"], rot=(0, 90 - math.degrees(am), 0))
    for s in (-1, 1):
        D.tower(g, (gx + s * 5.2, 1.6, z), 2.4, 11.0, M["stone"], M["stone_dark"], M["iron"])
    # braziers either side of the gate, and the banners
    for bx in (gx - 3.4, gx + 3.4):
        g.cyl((bx, -1.0, z + 0.6), 0.08, 1.2, M["iron"], n=8)
        g.cyl((bx, -1.0, z + 1.3), 0.35, 0.3, M["iron"], n=12, r2=0.25)
        g.cyl((bx, -1.0, z + 1.5), 0.25, 0.25, M["fire"], n=10, r2=0.05)
    for bx in (60.0, 80.0, 86.0):
        g.box((bx, WALL_Y - 0.05, z + 5.0), (1.2, 0.04, 3.4), M["cloth"])
        g.box((bx, WALL_Y - 0.08, z + 5.4), (0.5, 0.02, 0.5), M["cloth_pale"])        # the mark: a pale disc on black
    return g.finish(coll)


def build_portcullis(coll, M):
    g = S.Group("Portcullis")
    gx, z = 69.0, UP
    for k in range(9):
        g.box((gx - 2.0 + k * 0.5, -1.4, z + 2.4), (0.08, 0.08, 4.6), M["iron"])
    for k in range(6):
        g.box((gx, -1.4, z + 0.3 + k * 0.8), (4.2, 0.08, 0.08), M["iron"])
    for k in range(9):                                                              # the spikes at its foot
        g.cyl((gx - 2.0 + k * 0.5, -1.4, z + 0.08), 0.05, 0.2, M["iron"], n=6, r2=0.0)
    return g.finish(coll)


# ---------------------------------------------------------------- F3: the courtyard

def build_court(coll, M):
    g = S.Group("FortCourt")
    z = UP
    D.terrain(g, 88, HALL_X0, -3.0, WALL_Y, lambda x, y: z, M["cobbles"], res=0.7, amp=0.04)
    g.box((108.0, -0.5, z - 3.0), (40.0, 8.0, 6.0), M["stone"])                      # the courtyard's footing
    # the well
    g.cyl((97.0, 2.6, z + 0.45), 0.9, 0.9, M["stone"], n=20)
    g.cyl((97.0, 2.6, z + 0.92), 0.7, 0.06, M["road"], n=20)
    for s in (-1, 1):
        g.box((97.0 + s * 0.8, 2.6, z + 1.6), (0.12, 0.12, 1.4), M["planks_wall"])
    g.beam((96.2, 2.6, z + 2.2), (97.8, 2.6, z + 2.2), 0.1, 0.1, M["planks_wall"])
    # carts, barrels, crates under a lean-to
    g.box((104.0, 3.0, z + 0.7), (2.8, 1.4, 0.5), M["planks_wall"])
    for wx in (102.9, 105.1):
        g.cyl((wx, 2.2, z + 0.6), 0.6, 0.1, M["planks"], rot=(90, 0, 0), n=16)
    for k in range(5):
        g.cyl((109.0 + k * 0.75, 3.4, z + 0.45), 0.34, 0.9, M["planks_wall"], n=14)
    g.box((112.0, 3.5, z + 2.6), (6.0, 1.6, 0.1), M["planks"], rot=(-14, 0, 0))
    for k in range(3):
        S.crate(g, (114.0 + k * 0.9, 3.3, z + 0.4), (0.8, 0.8, 0.8), M)
    # the founder's statue: a knight on a plinth, his sword point-down before him
    kx = 124.5
    g.box((kx, 2.6, z + 0.8), (1.6, 1.6, 1.6), M["stone_dark"])
    g.box((kx, 2.6, z + 1.7), (1.8, 1.8, 0.2), M["stone"])
    g.cyl((kx, 2.6, z + 2.6), 0.32, 1.6, M["stone"], n=12, r2=0.42)
    g.cyl((kx, 2.6, z + 3.8), 0.36, 0.9, M["stone"], n=12, r2=0.3)
    g.cyl((kx, 2.6, z + 4.5), 0.22, 0.45, M["stone"], n=12)
    g.box((kx - 0.45, 2.3, z + 2.9), (0.06, 0.06, 1.8), M["iron"])
    g.box((kx - 0.45, 2.3, z + 3.7), (0.4, 0.06, 0.06), M["iron"])
    for s_ in (-1, 1):
        g.beam((kx + s_ * 0.36, 2.6, z + 4.1), (kx - 0.4, 2.35, z + 3.8), 0.12, 0.12, M["stone"])
    # the generator shed, its exhaust, the cables, and two floodlights on poles
    sx = 121.0
    g.box((sx, 2.8, z + 1.3), (4.0, 2.4, 2.6), M["stone_dark"])
    g.box((sx, 1.58, z + 1.3), (3.6, 0.05, 2.2), M["iron"])
    for k in range(10):
        g.box((sx - 1.6 + k * 0.36, 1.55, z + 1.3), (0.05, 0.05, 2.0), M["road"])
    g.cyl((sx + 1.5, 3.2, z + 3.4), 0.12, 1.8, M["iron"], n=8)
    for fx in (99.0, 116.0):
        g.cyl((fx, 3.6, z + 3.2), 0.08, 6.4, M["iron"], n=8)
        g.box((fx, 3.2, z + 6.3), (0.9, 0.5, 0.4), M["iron"])
        g.box((fx, 2.93, z + 6.25), (0.7, 0.04, 0.28), M["flood"])
        g.beam((fx, 3.6, z + 0.05), (sx - 2.0, 3.0, z + 0.05), 0.04, 0.04, M["road"])
    # the keep's face behind the courtyard: tall lit windows, a doorway into the hall
    g.box(((88 + HALL_X0) / 2, WALL_Y + 0.6, z + 6.0), (HALL_X0 - 88, 1.2, 12.0), M["stone"])
    for wx in (94.0, 102.0, 110.0, 118.0):
        g.box((wx, WALL_Y - 0.02, z + 6.5), (1.0, 0.05, 2.4), M["window"])
        g.box((wx, WALL_Y - 0.05, z + 7.75), (1.3, 0.1, 0.18), M["stone_dark"])
    D.merlons(g, 88, HALL_X0, WALL_Y + 0.1, z + 12.0, 0.5, M["stone"])
    return g.finish(coll)


# ---------------------------------------------------------------- F4: the great hall

def build_hall(coll, M):
    g = S.Group("FortHall")
    z = UP
    x0, x1 = HALL_X0, HALL_X1
    g.box(((x0 + x1) / 2, 0.6, z - 0.1), (x1 - x0, 7.6, 0.2), M["stone_floor"])
    g.box(((x0 + x1) / 2, WALL_Y + 0.6, z + 6.0), (x1 - x0, 0.6, 12.0), M["stone"])      # the back wall
    g.box(((x0 + x1) / 2, 0.6, HALL_TOP + 0.2), (x1 - x0, 7.6, 0.4), M["planks"])        # the ceiling
    for k in range(int((x1 - x0) / 3)):                                                 # its beams
        g.box((x0 + 1.5 + k * 3.0, 0.6, HALL_TOP - 0.15), (0.35, 7.6, 0.4), M["planks_wall"])
    for x in (x0, x1):
        g.box((x, 0.6, z + 6.0), (0.6, 7.6, 12.0), M["stone_y"])
    # the upper gallery along the back wall, its balustrade on corbels
    g.box(((x0 + x1) / 2, WALL_Y - 0.8, z + 6.6), (x1 - x0 - 1.2, 1.6, 0.3), M["stone_dark"])
    for k in range(int((x1 - x0 - 2) / 0.45)):
        g.cyl((x0 + 1.2 + k * 0.45, WALL_Y - 1.5, z + 7.2), 0.08, 0.9, M["stone"], n=8, r2=0.05)
    g.box(((x0 + x1) / 2, WALL_Y - 1.5, z + 7.7), (x1 - x0 - 1.2, 0.25, 0.12), M["stone"])
    for k in range(int((x1 - x0) / 3)):
        g.box((x0 + 1.5 + k * 3.0, WALL_Y - 0.2, z + 6.2), (0.4, 0.8, 0.6), M["stone_dark"])
    # the rose window high in the back wall: a ring of lights round a centre, its tracery
    rx, rz = (x0 + x1) / 2, z + 9.6
    g.cyl((rx, WALL_Y + 0.28, rz), 2.2, 0.06, M["glass"], rot=(90, 0, 0), n=40)
    for k in range(16):
        a = k / 16 * math.tau
        g.beam((rx, WALL_Y + 0.22, rz), (rx + math.cos(a) * 2.2, WALL_Y + 0.22, rz + math.sin(a) * 2.2), 0.07, 0.05, M["iron"])
    for ring in (0.8, 1.6, 2.2):
        g.torus((rx, WALL_Y + 0.22, rz), ring, 0.05, M["iron"], rot=(90, 0, 0), n1=40, n2=6)
    g.cyl((rx, WALL_Y + 0.25, rz), 0.5, 0.06, M["window"], rot=(90, 0, 0), n=20)
    # the throne on its dais at the far end
    g.box((163.0, 2.4, z + 0.25), (3.0, 3.0, 0.5), M["stone_dark"])
    g.box((163.0, 2.4, z + 0.65), (2.2, 2.2, 0.3), M["stone"])
    g.box((163.4, 2.6, z + 1.2), (0.9, 0.9, 0.12), M["planks"])
    g.box((163.8, 2.6, z + 2.3), (0.14, 1.0, 2.6), M["planks"])
    for s_ in (-1, 1):
        g.box((163.4, 2.6 + s_ * 0.5, z + 1.6), (0.9, 0.1, 0.7), M["planks"])
    # the doorway in from the courtyard (x0), the stair door to the tower (x1)
    # tall windows in the back wall: the lightning comes through them
    for wx in (133.0, 139.0, 145.0, 151.0, 157.0):
        if abs(wx - 147.0) < 2.0:
            continue                                                                   # the rose window's place
        g.box((wx, WALL_Y + 0.25, z + 3.4), (1.4, 0.05, 4.4), M["glass"])
        for k in range(4):
            g.box((wx, WALL_Y + 0.2, z + 1.6 + k * 1.2), (1.4, 0.06, 0.05), M["iron"])
        g.box((wx, WALL_Y + 0.2, z + 3.4), (0.05, 0.06, 4.4), M["iron"])
        for k in range(5):                                                              # its pointed head
            g.box((wx + (k - 2) * 0.16, WALL_Y + 0.2, z + 5.7 + (2 - abs(k - 2)) * 0.18), (0.18, 0.06, 0.1), M["stone_dark"])
    # pillars down the front and the back of the hall
    for px in range(int(x0) + 3, int(x1) - 1, 6):
        for py in (-1.6, 3.4):
            g.box((px, py, z + 5.8), (0.7, 0.7, 11.6), M["stone"])
            g.box((px, py, z + 0.2), (0.95, 0.95, 0.4), M["stone_dark"])
            g.box((px, py, z + 11.5), (0.95, 0.95, 0.3), M["stone_dark"])
    # the long table and its chairs, the fireplace, the chandeliers, banners
    g.box((146.0, 2.6, z + 0.82), (12.0, 1.3, 0.1), M["planks"])
    for k in range(6):
        g.box((141.0 + k * 2.0, 2.6, z + 0.4), (0.1, 1.1, 0.8), M["planks_wall"])
    for k in range(10):
        cx = 141.0 + k * 1.1
        for s in (-1, 1):
            g.box((cx, 2.6 + s * 0.95, z + 0.45), (0.45, 0.45, 0.06), M["planks_wall"])
            g.box((cx, 2.6 + s * 1.15, z + 0.9), (0.45, 0.06, 0.9), M["planks_wall"])
    fx = 160.0
    g.box((fx, WALL_Y - 0.4, z + 1.6), (3.6, 1.2, 3.2), M["stone_dark"])
    g.box((fx, WALL_Y - 1.02, z + 1.0), (2.4, 0.05, 1.8), M["road"])
    g.box((fx, WALL_Y - 0.9, z + 0.5), (1.8, 0.4, 0.5), M["fire"])
    g.box((fx, WALL_Y - 0.6, z + 3.6), (4.2, 1.8, 0.4), M["stone"])
    for cx in (136.0, 146.0, 156.0):
        g.beam((cx, 0.8, HALL_TOP), (cx, 0.8, HALL_TOP - 2.0), 0.03, 0.03, M["iron"])
        g.torus((cx, 0.8, HALL_TOP - 2.1), 1.1, 0.05, M["iron"], n1=24, n2=6)
        for k in range(10):
            a = k / 10 * math.tau
            g.cyl((cx + math.cos(a) * 1.1, 0.8 + math.sin(a) * 1.1, HALL_TOP - 1.95), 0.04, 0.18, M["candle"], n=6)
    for bx in (136.0, 148.0, 154.0):
        g.box((bx, WALL_Y + 0.26, z + 5.6), (1.4, 0.04, 3.6), M["cloth"])
        g.box((bx, WALL_Y + 0.22, z + 6.0), (0.6, 0.02, 0.6), M["cloth_pale"])
    # the observatory tower rising behind the hall, its dome, its lit gallery (chapter 6 is up there)
    D.tower(g, (TOWER_X, 9.0, z), 4.2, 24.0, M["stone"], M["stone_dark"], M["iron"], slits=5, crenel=True)
    g.cyl((TOWER_X, 9.0, z + 24.4), 4.7, 0.5, M["stone_dark"], n=32)
    for k in range(20):                                                                # the gallery's rail
        a = k / 20 * math.tau
        g.cyl((TOWER_X + math.cos(a) * 4.6, 9.0 + math.sin(a) * 4.6, z + 25.1), 0.04, 1.0, M["iron"], n=6)
    for k in range(6):
        a = math.pi + (k - 2.5) * 0.35
        g.box((TOWER_X + math.sin(a) * 4.25, 9.0 - abs(math.cos(a)) * 4.25, z + 20.0), (0.7, 0.06, 1.4), M["window"])
    return g.finish(coll)


def build_hall_front(coll, M):
    g = S.Group("FortHallFront")
    z = UP
    g.box(((HALL_X0 + HALL_X1) / 2, -2.75, z + 6.0), (HALL_X1 - HALL_X0, 0.5, 12.0), M["stone"])
    for wx in (134.0, 142.0, 150.0, 158.0):
        g.box((wx, -3.02, z + 4.6), (1.2, 0.05, 3.2), M["window"])
    return g.finish(coll)


def build_sky(coll, M):
    g = S.Group("FortSky")
    g.box((80.0, 140.0, 40.0), (600.0, 1.0, 140.0), M["sky"])
    for _ in range(40):                                                               # the storm's clouds, dark masses
        c = (rnd.uniform(-150, 320), rnd.uniform(90, 135), rnd.uniform(25, 70))
        g.cyl(c, rnd.uniform(14, 30), rnd.uniform(4, 9), M["cloud"], rot=(rnd.uniform(-8, 8), 0, 0), n=14)
    for k in range(30):                                                               # the far hills
        x = -150 + k * 16
        g.box((x, 110.0, rnd.uniform(4, 16)), (18.0, 6.0, rnd.uniform(14, 34)), M["cloud"])
    return g.finish(coll)


# ---------------------------------------------------------------- the final level's grandeur

def build_roof(coll, M):
    """The last roof: a terrace over the great hall in the storm, its battlements, gargoyles at the
    corners, lightning rods, a broken parapet over the drop at the far end; behind it the
    observatory's dome on the tower, open, a great telescope pointing into the storm."""
    g = S.Group("FortRoof")
    z = ROOF
    D.terrain(g, HALL_X0 - 0.4, HALL_X1 + 0.4, -3.2, 5.0, lambda x, y: z, M["stone_floor"], res=0.8, amp=0.02)
    # the parapet behind (the tower side) and the low battlements in front
    g.box(((HALL_X0 + HALL_X1) / 2, 5.2, z + 0.7), (HALL_X1 - HALL_X0, 0.5, 1.4), M["stone"])
    D.merlons(g, HALL_X0, HALL_X1 - 4.0, 5.2, z + 1.4, 0.5, M["stone"])
    D.merlons(g, HALL_X0, HALL_X1 - 4.0, -3.6, z, 0.4, M["stone"], w=0.6, gap=0.6, h=0.35)
    g.box(((HALL_X0 + HALL_X1 - 4.0) / 2, -3.6, z - 0.2), (HALL_X1 - HALL_X0 - 4.0, 0.5, 0.4), M["stone_dark"])
    # the broken end: the parapet gone, rubble, the drop
    for k in range(9):
        D.boulder(g, (HALL_X1 - 3.2 + rnd.uniform(0, 2.4), rnd.uniform(-3.0, -1.0), z + 0.12), rnd.uniform(0.15, 0.35), M["stone"])
    # gargoyles on the corners: a crouched beast, wings, a gaping head
    for (gx, gy) in ((HALL_X0 + 0.4, -3.4), (HALL_X1 - 4.4, -3.4), (HALL_X0 + 0.4, 5.3), (HALL_X1 - 0.4, 5.3)):
        g.box((gx, gy - 0.1, z + 1.2), (0.6, 0.9, 0.5), M["stone_dark"])
        g.box((gx, gy - 0.6, z + 1.5), (0.4, 0.5, 0.4), M["stone_dark"])
        g.cyl((gx, gy - 0.95, z + 1.45), 0.08, 0.3, M["stone_dark"], rot=(90, 0, 0), n=8)
        for s_ in (-1, 1):
            g.box((gx + s_ * 0.45, gy, z + 1.55), (0.5, 0.6, 0.08), M["stone_dark"], rot=(0, s_ * 35, 0))
    # lightning rods on the parapet
    for lx in (HALL_X0 + 6.0, HALL_X0 + 18.0, HALL_X0 + 30.0):
        g.cyl((lx, 5.2, z + 3.6), 0.04, 4.6, M["iron"], n=6)
        g.cyl((lx, 5.2, z + 6.0), 0.12, 0.3, M["iron"], n=8, r2=0.0)
    # the door from the stair onto the terrace, at the tower's foot
    g.box((TOWER_X, 4.75, z + 1.3), (1.4, 0.2, 2.6), M["road"])
    g.box((TOWER_X, 4.7, z + 2.8), (1.9, 0.3, 0.4), M["stone_dark"])
    return g.finish(coll)


def build_dome(coll, M):
    """The observatory's dome on the tower, its open slit and the great telescope: its own part, so
    the walk-out can blow it off the tower (docs/story/walkout.md)."""
    g = S.Group("FortDome")
    # the observatory: the tower's top above the terrace, the dome and its open slit, the telescope
    tz = UP + 24.4
    for k in range(10):                                                             # the dome, ring by ring
        a0, a1 = k / 10 * math.pi / 2, (k + 1) / 10 * math.pi / 2
        r0, r1 = 4.6 * math.cos(a0), 4.6 * math.cos(a1)
        g.cyl((TOWER_X, 9.0, tz + 0.5 + 4.6 * (math.sin(a0) + math.sin(a1)) / 2), (r0 + r1) / 2, 4.6 * (math.sin(a1) - math.sin(a0)) + 0.02, M["iron"], n=32)
    g.box((TOWER_X, 9.0 - 2.4, tz + 3.0), (1.2, 2.0, 5.0), M["road"])               # the slit, open
    g.cyl((TOWER_X - 0.6, 6.0, tz + 5.4), 0.55, 6.5, M["iron"], rot=(-32, 18, 0), n=16, r2=0.4)   # the telescope
    g.cyl((TOWER_X - 2.0, 3.4, tz + 7.5), 0.6, 0.4, M["iron"], rot=(-32, 18, 0), n=16)
    return g.finish(coll)


def build_back(coll, M):
    """The fortress beyond the lane: a second, higher wall along the cliff with towers of every
    height and arched bridges between them, the great keep, a waterfall off the cliff, lit windows
    everywhere, outer bastions going away into the storm."""
    g = S.Group("FortBack")
    by = 18.0
    D.terrain(g, -10, 200, 12.0, 30.0, lambda x, y: (y - 12.0) * 0.9 + 2.0 + (UP if x > 50 else 0.0), M["rock_top"], res=2.0, amp=1.6, seed=11.0)
    g.box((95.0, by + 1.0, UP + 9.0), (150.0, 2.0, 10.0), M["stone"])               # the high wall
    D.merlons(g, 20.0, 170.0, by + 0.2, UP + 14.0, 0.6, M["stone"], w=0.7, gap=0.6, h=0.9)
    towers = [(24, 3.2, 17), (46, 2.6, 21), (78, 3.6, 25), (102, 2.8, 19), (138, 3.0, 23), (172, 3.4, 27)]
    for (tx, r, h) in towers:
        D.tower(g, (tx, by + 1.0, UP + 4.0), r, h, M["stone"], M["stone_dark"], M["iron"], slits=3, crenel=(h % 2 == 1))
        for k in range(4):
            g.box((tx + rnd.uniform(-1, 1), by + 1.0 - r - 0.02, UP + 8.0 + k * 4.0), (0.6, 0.05, 1.1), M["window"] if rnd.random() < 0.5 else M["road"])
    for (a, b) in ((24, 46), (78, 102), (138, 172)):                               # bridges between towers
        z0 = UP + 15.0
        g.box(((a + b) / 2, by + 1.0, z0), (b - a, 2.2, 0.8), M["stone"])
        for k in range(int((b - a) / 3)):
            cx = a + 1.5 + k * 3
            g.box((cx, by + 1.0, z0 - 1.6), (0.6, 2.0, 2.4), M["stone"])
        D.merlons(g, a, b, by, z0 + 0.4, 0.4, M["stone"], w=0.45, gap=0.4, h=0.5)
    # the great keep: a square tower with buttresses, pinnacles and tall lit windows
    kx, ky = 115.0, 26.0
    g.box((kx, ky, UP + 16.0), (14.0, 14.0, 32.0), M["stone"])
    for s1 in (-1, 1):
        for s2 in (-1, 1):
            g.box((kx + s1 * 7.4, ky + s2 * 7.4, UP + 14.0), (2.0, 2.0, 28.0), M["stone_dark"])
            g.cyl((kx + s1 * 7.4, ky + s2 * 7.4, UP + 30.0), 0.9, 4.0, M["stone_dark"], n=8, r2=0.05)   # pinnacles
    for k in range(5):
        for j in range(4):
            g.box((kx - 5.0 + k * 2.5, ky - 7.05, UP + 6.0 + j * 6.0), (0.9, 0.05, 2.6), M["window"] if rnd.random() < 0.55 else M["road"])
    D.merlons(g, kx - 7, kx + 7, ky - 7.0, UP + 32.0, 0.6, M["stone"], w=0.8, gap=0.7, h=1.2)
    g.cyl((kx, ky, UP + 33.0), 0.06, 6.0, M["iron"], n=6)
    g.box((kx + 0.9, ky, UP + 35.4), (1.6, 0.04, 1.0), M["cloth"])                 # a flag in the gale
    # the waterfall off the cliff by the road
    for k in range(40):
        x = 14.0 + rnd.uniform(-1.2, 1.2)
        g.box((x, 13.0 + rnd.uniform(-0.3, 0.3), rnd.uniform(-2, 20)), (0.05, 0.05, rnd.uniform(2, 6)), D.flat("FortSpray", 0.8, emit=0.6))
    # outer bastions far away, fading into the storm
    for k in range(8):
        x = -40 + k * 34 + rnd.uniform(-6, 6)
        h = rnd.uniform(16, 30)
        g.box((x, 60.0, h / 2), (rnd.uniform(8, 14), 6.0, h), M["stone_dark"])
        D.merlons(g, x - 5, x + 5, 57.0, h, 0.5, M["stone_dark"], w=0.9, gap=0.8, h=1.0)
    return g.finish(coll)


def build_fore(coll, M):
    """In front of the lane, below it: the outer parapet's teeth, framing the bottom of the shot."""
    g = S.Group("FortFore")
    for x0, x1, z in ((52.0, 88.0, UP), (88.0, 128.0, UP)):
        g.box(((x0 + x1) / 2, -4.8, z - 2.3), (x1 - x0, 0.7, 2.4), M["stone"])
        D.merlons(g, x0, x1, -4.8, z - 1.1, 0.7, M["stone"], w=0.7, gap=0.7, h=0.6)
    return g.finish(coll)


# ---------------------------------------------------------------- light

def add_light(kind, name, loc, energy, size=1.0, rot=None):
    ld = bpy.data.lights.new(name, kind)
    ld.energy = energy
    ld.color = S.NEUTRAL
    if kind == "AREA":
        ld.size = size
    if kind in ("POINT", "SPOT"):
        ld.shadow_soft_size = size
    ob = bpy.data.objects.new(name, ld)
    ob.location = loc
    if rot is not None:
        ob.rotation_euler = rot
    bpy.context.scene.collection.objects.link(ob)
    return ob


def lights():
    """The storm at night: a dim moon through cloud, fire, floodlights, candles, lit windows."""
    moon = add_light("SUN", "Moon", (0, 0, 50), 0.32)
    moon.rotation_euler = (math.radians(55), 0, math.radians(160))
    add_light("AREA", "Fill", (80.0, -18.0, 10.0), 1100, size=120.0, rot=(math.radians(72), 0, 0))
    add_light("POINT", "RoadFlood", (38.0, 1.2, 5.8), 900, size=0.3)
    for bx in (65.6, 72.4):
        add_light("POINT", "Brazier", (bx, -1.0, UP + 1.8), 220, size=0.3)
    for fx in (99.0, 116.0):
        add_light("SPOT", "CourtFlood", (fx, 2.6, UP + 6.2), 2400, size=0.3, rot=(math.radians(35), 0, 0))
    for wx in (94.0, 102.0, 110.0, 118.0):
        add_light("AREA", "KeepWindow", (wx, WALL_Y - 0.4, UP + 6.5), 60, size=1.0, rot=(math.radians(90), 0, 0))
    add_light("AREA", "CourtFill", (108.0, -6.0, UP + 5.0), 900, size=40.0, rot=(math.radians(70), 0, 0))
    add_light("AREA", "GateFill", (70.0, -7.0, UP + 4.0), 400, size=24.0, rot=(math.radians(70), 0, 0))
    add_light("POINT", "Fireplace", (160.0, WALL_Y - 1.6, UP + 1.0), 650, size=0.6)
    for cx in (136.0, 146.0, 156.0):
        add_light("POINT", "Chandelier", (cx, 0.8, HALL_TOP - 2.3), 260, size=0.8)
    add_light("AREA", "HallFill", (147.0, -2.0, UP + 6.0), 420, size=24.0, rot=(math.radians(80), 0, 0))
    add_light("AREA", "RoofFill", (147.0, -10.0, ROOF + 4.0), 900, size=40.0, rot=(math.radians(72), 0, 0))
    add_light("AREA", "BackFill", (95.0, -10.0, UP + 18.0), 2400, size=120.0, rot=(math.radians(62), 0, 0))
    w = bpy.data.worlds.new("Storm")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.03, 0.032, 0.04, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def lightning_lights():
    """A strike: the sky behind the hill blazes, everything is thrown into hard white and black."""
    for ob in bpy.context.scene.objects:
        if ob.type != "LIGHT":
            continue
        if ob.name == "Moon":
            ob.data.energy = 6.0
            ob.rotation_euler = (math.radians(35), 0, math.radians(170))
        elif ob.name.startswith("Fill"):
            ob.data.energy *= 2.6
    for x in (10.0, 60.0, 110.0, 160.0):
        add_light("AREA", "Strike", (x, 40.0, 60.0), 90000, size=40.0, rot=(math.radians(-140), 0, 0))
    for wx in (133.0, 139.0, 145.0, 151.0, 157.0):
        add_light("AREA", "StrikeWindow", (wx, WALL_Y + 1.6, UP + 5.0), 2600, size=1.6, rot=(math.radians(-90), 0, 0))
    bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.2, 0.21, 0.24, 1)


def dawn_lights():
    """The morning after (docs/story/walkout.md): the storm gone over, a pale sky behind the hill,
    a low sun from the back right rimming every edge, a soft cool light on the faces towards us.
    The lanterns and the windows still burn, small now."""
    from mathutils import Vector as V
    for ob in list(bpy.context.scene.objects):
        if ob.type == "LIGHT":
            bpy.data.objects.remove(ob, do_unlink=True)
    sun = add_light("SUN", "DawnSun", (0, 0, 60), 4.2)
    sun.data.angle = math.radians(3.0)
    sun.data.color = (1.0, 0.97, 0.93)
    d = V((-0.55, -0.8, -0.2)).normalized()
    sun.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    add_light("AREA", "DawnFront", (147.0, -120.0, 40.0), 60000, size=160.0, rot=(math.radians(68), 0, 0)).data.color = (0.9, 0.93, 1.0)
    add_light("AREA", "DawnCourtFill", (147.0, -40.0, UP + 14.0), 9000, size=60.0, rot=(math.radians(64), 0, 0)).data.color = (0.92, 0.94, 1.0)
    w = bpy.context.scene.world
    if w is None:
        w = bpy.data.worlds.new("Dawn")
        w.use_nodes = True
        bpy.context.scene.world = w
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.36, 0.38, 0.42, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    # the sky panel: bright and pale low down behind the hill, grey-blue above
    for m in bpy.data.materials:
        if m.name.startswith("FortStormSky"):
            nt = m.node_tree
            bsdf = nt.nodes["Principled BSDF"]
            geo = nt.nodes.new("ShaderNodeNewGeometry")
            sep = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(geo.outputs["Position"], sep.inputs[0])
            mr = nt.nodes.new("ShaderNodeMapRange")
            mr.inputs["From Min"].default_value = -10.0
            mr.inputs["From Max"].default_value = 90.0
            nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
            ramp = nt.nodes.new("ShaderNodeValToRGB")
            e = ramp.color_ramp.elements
            e[0].position, e[0].color = 0.0, (1.0, 0.98, 0.95, 1)
            e[1].position, e[1].color = 1.0, (0.3, 0.33, 0.39, 1)
            mid = e.new(0.3)
            mid.color = (0.72, 0.73, 0.75, 1)
            nt.links.new(mr.outputs["Result"], ramp.inputs["Fac"])
            nt.links.new(ramp.outputs["Color"], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 1.5
    # the storm's clouds: thinned to faint shapes against the morning (the same sky, a breath darker)
    for m in bpy.data.materials:
        if m.name.startswith("FortCloud"):
            nt = m.node_tree
            bsdf = nt.nodes["Principled BSDF"]
            bsdf.inputs["Base Color"].default_value = (0.0, 0.0, 0.0, 1)
            geo = nt.nodes.new("ShaderNodeNewGeometry")
            sep = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(geo.outputs["Position"], sep.inputs[0])
            mr = nt.nodes.new("ShaderNodeMapRange")
            mr.inputs["From Min"].default_value = -10.0
            mr.inputs["From Max"].default_value = 90.0
            nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
            ramp = nt.nodes.new("ShaderNodeValToRGB")
            e = ramp.color_ramp.elements
            e[0].position, e[0].color = 0.0, (0.9, 0.89, 0.87, 1)
            e[1].position, e[1].color = 1.0, (0.26, 0.28, 0.33, 1)
            mid = e.new(0.3)
            mid.color = (0.6, 0.61, 0.63, 1)
            nt.links.new(mr.outputs["Result"], ramp.inputs["Fac"])
            nt.links.new(ramp.outputs["Color"], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 1.5


def burn_lights():
    """After the blasts (the walk-out): every window and door full of fire, the roof and the tower
    burning, the morning dimmed by the smoke. The fire is the only warm thing in the game's grey."""
    for ob in bpy.context.scene.objects:
        if ob.type != "LIGHT":
            continue
        if ob.name == "DawnSun":
            ob.data.energy = 1.6
        elif ob.name.startswith("Dawn"):
            ob.data.energy *= 0.45
    bpy.context.scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.55
    fire = (1.0, 0.42, 0.1)
    def L(x, y, z, e, size=1.5):
        o = add_light("POINT", "Fire", (x, y, z), e, size=size)
        o.data.color = fire
    for bx in (131.1, 135.75, 140.85, 153.15, 158.25, 162.9):
        L(bx, F.FY - 1.4, UP + 3.4, 1400)
        L(bx, F.FY - 1.0, UP + 8.2, 700)
    L(F.DOOR_X, F.FY - 0.6, UP + 1.6, 5000, 2.0)
    L(F.DOOR_X, F.FY - 3.2, UP + 8.6, 1600)
    for rx in (134.0, 147.0, 160.0):
        L(rx, 0.5, ROOF + 2.5, 6000, 3.0)
    L(TOWER_X, 9.0 - 5.0, UP + 26.0, 9000, 3.0)
    L(115.0, 18.0, UP + 21.0, 12000, 4.0)
    L(124.0, -1.0, UP + 7.0, 4000, 2.0)
    L(165.0, -16.0, UP + 5.0, 6000, 3.0)
    L(165.0, -16.0, UP + 10.0, 4000, 3.0)
    L(102.0, 14.0, UP + 18.0, 6000, 3.0)
    L(138.0, 14.0, UP + 22.0, 6000, 3.0)
    for m in bpy.data.materials:
        if m.name.startswith(("FrontGlass", "FortWindow", "FortGlass", "FrontHallLight")) and m.use_nodes:
            b = m.node_tree.nodes.get("Principled BSDF")
            if b is not None:
                b.inputs["Emission Color"].default_value = (1.0, 0.5, 0.16, 1)
                b.inputs["Emission Strength"].default_value = 5.0


DAWN_PARTS = {          # part: (size, samples) of its dawn bake
    "FortFacade": (4096, 16), "FortPorch": (4096, 16), "FortForecourt": (4096, 16),
    "FortBridge": (4096, 16), "FortGorge": (4096, 12),
    "FortHall": (2048, 10), "FortRoof": (2048, 10), "FortBack": (2048, 8), "FortSky": (1024, 8),
    "FortCourt": (2048, 10), "FortGate": (1024, 8), "FortFore": (1024, 8),
    "FortDome": (1024, 10), "FortSpireW": (512, 8), "FortSpireE": (512, 8),
    "FortChimney0": (512, 8), "FortChimney1": (512, 8), "FortChimney2": (512, 8), "FortChimney3": (512, 8),
}
BURN_PARTS = {          # the fortress on fire (the walk-out, after the blasts)
    "FortFacade": (4096, 12), "FortPorch": (2048, 10), "FortForecourt": (4096, 12), "FortHall": (2048, 10),
    "FortRoof": (2048, 10), "FortCourt": (2048, 8), "FortBack": (2048, 8), "FortDome": (1024, 8),
    "FortBridge": (2048, 8), "FortSky": (1024, 6),
    "FortSpireW": (512, 6), "FortSpireE": (512, 6),
    "FortChimney0": (512, 6), "FortChimney1": (512, 6), "FortChimney2": (512, 6), "FortChimney3": (512, 6),
}
NIGHT_PARTS = {"FortRoof": (4096, 16), "FortDome": (1024, 12)}           # (re)baked in the storm too
LIGHTNING_PARTS = {"FortRoof": (2048, 10), "FortDome": (512, 8)}


def walkout_board(name):
    """The take's key frames as the game frames them (StoryWalkout.WoCamera), with stand-ins: him
    (black) and her (white, 0.58 his size), walking at the camera."""
    U = UP
    frames = [   # label, hero z, cam (x, y, z), look (x, y, z), fov
        ("1.5 s  the doorway, two shapes in it", -0.98, (147.05, U + 0.92, -8.6), (147.0, U + 1.55, -3.35), 36),
        ("8 s  out of the porch", -7.05, (147.53, U + 0.92, -10.45), (147.03, U + 0.81, -6.45), 36),
        ("22 s  stopped: the walkies", -18.7, (147.05, U + 0.92, -21.98), (147.03, U + 0.81, -18.1), 36),
        ("35 s  'Over and out.'", -23.25, (147.05, U + 0.92, -26.55), (147.03, U + 0.81, -22.65), 36),
        ("40 s  through the gate, the hill going up", -27.95, (147.05, U + 0.92, -31.25), (147.03, U + 0.81, -27.35), 36),
        ("52 s  slow motion: two clicks", -35.35, (146.97, U + 1.14, -39.4), (147.03, U + 1.75, -34.75), 40.4),
        ("60 s  the last frame", -37.9, (146.9, U + 1.32, -42.6), (147.03, U + 2.51, -37.3), 44),
    ]
    white = S.mat("MayaInk", (0.85, 0.85, 0.84), 0.9)
    shots = []
    arms = []
    for i, (label, hz, cp, lk, fov) in enumerate(frames):
        h, fr = S.stickman("walk", 6, 0.0, -90.0)
        h.location = (147.3, hz, UP)
        m, _ = S.stickman("walk", 18, 0.0, -90.0)
        m.location = (146.75, hz + 0.04, UP)
        m.scale = (0.58, 0.58, 0.58)
        for o in bpy.data.objects:
            if o.type == "MESH" and o.parent == m:
                o.data.materials.clear()
                o.data.materials.append(white)
        arms.append((h, m))
        shots.append((label, S.shot_camera("B%d" % i, cp, lk, fov), fr))
    sc = bpy.context.scene
    # one pair visible per shot
    import types
    cams = []
    for i, (label, cam, fr) in enumerate(shots):
        cams.append((label, cam, fr, i))
    S.PREVIEW = os.path.join(ROOT, "docs", "previews")
    def show(k):
        for j, (h, m) in enumerate(arms):
            for a in (h, m):
                hide = j != k
                a.hide_render = hide
                for o in bpy.data.objects:
                    if o.parent == a:
                        o.hide_render = hide
    orig = S.preview
    tiles = []
    for (label, cam, fr, i) in cams:
        show(i)
        tiles.append((label, cam, fr))
    # render each with its pair shown
    sc.render.engine = "CYCLES"
    from PIL import Image, ImageDraw, ImageFont
    out = []
    tmp = os.path.join(OUT, "_board.png")
    sc.cycles.samples = 10
    sc.render.resolution_x, sc.render.resolution_y = 768, 432
    sc.view_settings.view_transform = "Standard"
    for (label, cam, fr, i) in cams:
        show(i)
        sc.camera = cam
        sc.frame_set(fr)
        sc.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        out.append((label, Image.open(tmp).convert("RGB").copy()))
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 26)
    rows = (len(out) + 1) // 2
    sheet = Image.new("RGB", (778 * 2, 472 * rows), (20, 20, 20))
    d = ImageDraw.Draw(sheet)
    for i, (label, img) in enumerate(out):
        x, y = (i % 2) * 778, (i // 2) * 472
        sheet.paste(img, (x, y + 36))
        d.text((x + 8, y + 4), label, fill=(255, 210, 0), font=font)
    sheet.save(os.path.join(ROOT, "docs", "previews", name))


def walkout_preview(name):
    cams = [
        ("W0  establishing: Kessel Hill at dawn", S.shot_camera("W0", (147.0, UP + 3.0, -104.0), (140.0, UP + 10.0, 0.0), 40), None),
        ("W1  the doorway", S.shot_camera("W1", (147.0, UP + 1.35, -8.6), (147.0, UP + 1.7, -3.0), 36), None),
        ("W2  the forecourt (they stop here)", S.shot_camera("W2", (147.0, UP + 1.25, -21.4), (147.0, UP + 1.7, -17.0), 36), None),
        ("W3  the gate", S.shot_camera("W3", (147.0, UP + 1.3, -31.0), (147.0, UP + 1.5, -26.5), 36), None),
        ("W4  the bridge, the end of the slow motion", S.shot_camera("W4", (147.0, UP + 1.8, -48.0), (147.0, UP + 2.8, -40.0), 41), None),
        ("W5  overview", S.shot_camera("W5", (180.0, UP + 10.0, -75.0), (147.0, UP + 2.0, -22.0), 42), None),
    ]
    S.PREVIEW = os.path.join(ROOT, "docs", "previews")
    S.preview(cams, name)


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "FortressSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported FortressSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    lit = "--lightning-preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = M_()
    road, gate, court, hall = build_road(coll, M), build_gate(coll, M), build_court(coll, M), build_hall(coll, M)
    front, sky, port = build_hall_front(coll, M), build_sky(coll, M), build_portcullis(coll, M)
    roof, back, fore = build_roof(coll, M), build_back(coll, M), build_fore(coll, M)
    dome = build_dome(coll, M)
    for o in (road, gate, court, hall, front, port, roof, fore, dome):
        D.bevel(o, 0.03)
    fparts = F.build_all(S, coll, M)
    for o in fparts[:4]:
        D.bevel(o, 0.02)
    if "--walkout-board" in sys.argv:
        front.hide_render = True
        dawn_lights()
        walkout_board("walkout_board.png")
        return
    if "--walkout-preview" in sys.argv:
        front.hide_render = True
        dawn_lights()
        if "--fast" in sys.argv:
            S.PREVIEW_SAMPLES = 8
        walkout_preview("fortress_walkout.png")
        return
    modes = [x.split("=", 1)[1].split(",") for x in sys.argv if x.startswith("--bake=")]
    if "--dawn" in sys.argv:
        modes = [["dawn"]]
    if modes:
        # chosen bakes only (the others are kept: unwraps are deterministic)
        allp = [road, gate, court, hall, front, sky, port, roof, back, fore, dome] + fparts
        for o in allp:
            S.unwrap(o)
        sel = [x.split("=", 1)[1].split(",") for x in sys.argv if x.startswith("--parts=")]
        tables = {"night": (NIGHT_PARTS, ""), "lightning": (LIGHTNING_PARTS, "_Lightning"), "dawn": (DAWN_PARTS, "_Dawn"), "burn": (BURN_PARTS, "_Burn")}
        front.hide_render = True
        for mode in modes[0]:
            for ob in list(bpy.context.scene.objects):
                if ob.type == "LIGHT":
                    bpy.data.objects.remove(ob, do_unlink=True)
            night = mode in ("night", "lightning")
            for o in fparts:
                o.hide_render = night                     # the way out the front isn't there in the storm
            if night:
                lights()
                if mode == "lightning":
                    lightning_lights()
            else:
                dawn_lights()
                if mode == "burn":
                    burn_lights()
            table, suffix = tables[mode]
            want = sel[0] if sel else list(table)
            for o in allp:
                if o.name in want and o.name in table:
                    size, smp = table[o.name]
                    S.bake(o, size, smp, suffix)
        if "--export" in sys.argv:
            marks = []
            for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
                e = bpy.data.objects.new(n, None)
                e.location = p
                coll.objects.link(e)
                marks.append(e)
            export(allp + marks)
        return
    lights()
    if preview_only or lit:
        if lit:
            lightning_lights()
        front.hide_render = True
        cams = [
            ("F2  Kessel Hill in the storm", S.shot_camera("P1", (20.0, 6.0, -40.0), (120.0, 14.0, 6.0), 40), None),
            ("F5  the cliff road (gameplay)", S.shot_camera("P2", (22.0, 2.2, -11.0), (22.0, 1.3, 1.0), 42), None),
            ("F6  the gatehouse, the drawbridge", S.shot_camera("P3", (60.0, UP + 1.4, -8.0), (69.0, UP + 4.0, 0.0), 46), None),
            ("F8  the courtyard (gameplay)", S.shot_camera("P4", (108.0, UP + 2.2, -12.0), (108.0, UP + 1.6, 1.0), 42), None),
            ("F10  the great hall (gameplay)", S.shot_camera("P5", (147.0, UP + 2.2, -11.0), (147.0, UP + 2.0, 1.0), 42), None),
            ("F12  the observatory tower, from the courtyard", S.shot_camera("P6", (118.0, UP + 1.0, -8.0), (TOWER_X, UP + 22.0, 9.0), 50), None),
            ("F16  the last roof (gameplay)", S.shot_camera("P7", (147.0, ROOF + 2.4, -12.0), (147.0, ROOF + 2.0, 1.0), 44), None),
            ("F0  the whole fortress", S.shot_camera("P8", (60.0, 14.0, -70.0), (100.0, 14.0, 10.0), 44), None),
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "fortress_set_lightning.png" if lit else "fortress_set.png")
        return
    parts = [road, gate, court, hall, front, sky, port, roof, back, fore, dome] + fparts
    for o in parts:
        S.unwrap(o)
    front.hide_render = True
    S.bake(road, 4096, 16)
    S.bake(gate, 4096, 16)
    S.bake(court, 4096, 16)
    S.bake(hall, 4096, 20)
    S.bake(sky, 1024, 8)
    S.bake(port, 512, 8)
    S.bake(roof, 4096, 16)
    S.bake(dome, 1024, 12)
    S.bake(back, 4096, 12)
    S.bake(fore, 2048, 10)
    front.hide_render = False
    S.bake(front, 2048, 12)
    # the same set, lit by a strike
    lightning_lights()
    front.hide_render = True
    S.bake(road, 2048, 10, "_Lightning")
    S.bake(gate, 2048, 10, "_Lightning")
    S.bake(court, 2048, 10, "_Lightning")
    S.bake(hall, 2048, 12, "_Lightning")
    S.bake(sky, 512, 6, "_Lightning")
    S.bake(roof, 2048, 10, "_Lightning")
    S.bake(dome, 512, 8, "_Lightning")
    S.bake(back, 2048, 8, "_Lightning")
    S.bake(fore, 1024, 6, "_Lightning")
    front.hide_render = False
    S.bake(front, 1024, 8, "_Lightning")
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(parts + marks)


if __name__ == "__main__":
    main()
