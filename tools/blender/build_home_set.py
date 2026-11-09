"""Chapter 3: HOME (docs/story/chapter3_home.md). 14 Alder Lane at dusk: a quiet suburban road, the
gate and the garden path, and the house cut away side-on like the warehouse. Two storeys:

  ground (z 0)    porch 0-3 | hall 3-9 (the stairs up the back wall) | study 9-15 |
                  living room 15-25 | kitchen 25-34 | back door -> the garden and the play area
  upstairs (3.2)  landing 3-14 | Maya's room 14-22 | the bedroom 22-34

The same palette as the rest of the game (the tower's: build_story_set's TONE). A low sun comes in
through the back windows in long shafts; a few lamps are on. The front wall (the camera side) is its
own object, cut away by the game; the swing seat is separate (it moves).

    /home/user/.venv-blender/bin/python tools/blender/build_home_set.py [--preview]

Writes Assets/Art/Home/HomeSet.fbx and its bakes. Coordinates: Blender X = Unity x, Y = Unity z
(depth), Z = Unity y."""
import math
import os
import random
import sys

import bpy  # noqa: E402
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Home")
rnd = random.Random(31)

X0, X1 = 0.0, 34.0          # the house's ends (the porch is outside x0)
YF, YB = -2.0, 4.2          # front (cut away) and back walls
FLOOR2 = 3.2                # the upstairs floor
H1, H2 = 3.0, 6.0           # ceiling heights
DOORS1 = (3.0, 9.0, 15.0, 25.0)     # ground-floor walls with doorways (3 = the front door)
DOORS2 = (14.0, 22.0)
STAIR_X0, STAIR_X1 = 4.2, 9.4


def mats():
    M = S.materials()
    mat = S.mat
    M["plaster"] = mat("Plaster", (0.5, 0.5, 0.49), 0.95)
    M["plaster_kid"] = mat("PlasterKid", (0.56, 0.56, 0.55), 0.95)
    M["floorboard"] = mat("Floorboard", (0.22, 0.2, 0.18), 0.6)
    M["tile"] = mat("Tile", (0.42, 0.42, 0.42), 0.3)
    M["skirting"] = mat("Skirting", (0.75, 0.75, 0.73), 0.6)
    M["fabric"] = mat("Fabric", (0.18, 0.18, 0.19), 1.0)
    M["fabric_pale"] = mat("FabricPale", (0.62, 0.62, 0.6), 1.0)
    M["wood_furn"] = mat("FurnWood", (0.16, 0.12, 0.09), 0.6)
    M["brick"] = mat("Brick", (0.3, 0.29, 0.28), 0.95)
    M["roof"] = mat("RoofTile", (0.1, 0.1, 0.11), 0.8)
    M["grass"] = mat("Grass", (0.12, 0.13, 0.12), 1.0)
    M["window_dusk"] = mat("WindowDusk", (0.7, 0.68, 0.64), 0.2, emit=(1.0, 0.95, 0.88), strength=0.9)
    M["lamp"] = mat("LampShade", (1, 1, 1), 0.4, emit=(1, 0.95, 0.88), strength=6.0)
    M["fairy"] = mat("FairyLight", (1, 1, 1), 0.3, emit=(1, 0.95, 0.85), strength=14.0)
    M["sky_dusk"] = mat("SkyDusk", (0.5, 0.5, 0.52), 0.5, emit=(0.72, 0.7, 0.68), strength=1.0)
    M["white_paint"] = mat("WhitePaint", (0.78, 0.78, 0.76), 0.7)
    M["chalk"] = mat("Chalk", (0.85, 0.85, 0.83), 0.9)
    M["car_black"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["tyre"] = mat("Tyre", (0.02, 0.02, 0.02), 0.9)
    M["paper"] = mat("Paper", (0.82, 0.82, 0.8), 0.9)
    return M


S.TONE.update({"Plaster": 0.46, "PlasterKid": 0.52, "Floorboard": 0.18, "Tile": 0.38, "Skirting": 0.72, "Fabric": 0.14,
               "FabricPale": 0.58, "FurnWood": 0.11, "Brick": 0.26, "RoofTile": 0.07, "Grass": 0.11, "CarBlack": 0.025,
               "Chalk": 0.82, "Paper": 0.8, "WhitePaint": 0.72})


# ---------------------------------------------------------------- pieces

def wall_with_door(g, x, z0, z1, m, door_w=1.3, door_h=2.2, y0=YF, y1=YB):
    """A cross wall at x from the front to the back, a doorway in the lane (y ~ 0)."""
    t = 0.14
    g.box((x, (y0 + (-door_w / 2)) / 2, (z0 + z1) / 2), (t, (-door_w / 2) - y0, z1 - z0), m)
    g.box((x, ((door_w / 2) + y1) / 2, (z0 + z1) / 2), (t, y1 - door_w / 2, z1 - z0), m)
    g.box((x, 0.0, (z0 + door_h + z1) / 2), (t, door_w, z1 - z0 - door_h), m)


def back_window(g, x, z0, w, h, M):
    g.box((x, YB - 0.02, z0 + h / 2), (w, 0.05, h), M["window_dusk"])
    g.box((x, YB - 0.08, z0 - 0.05), (w + 0.2, 0.2, 0.08), M["skirting"])
    g.box((x, YB - 0.06, z0 + h / 2), (0.06, 0.06, h), M["skirting"])
    g.box((x, YB - 0.06, z0 + h / 2), (w, 0.06, 0.06), M["skirting"])


def frame(g, x, z, w, h, M, y=YB - 0.05):
    g.box((x, y, z), (w + 0.08, 0.04, h + 0.08), M["wood_furn"])
    g.box((x, y - 0.025, z), (w, 0.02, h), M["paper"])


def sofa(g, x, y, M):
    g.box((x, y, 0.25), (2.2, 0.9, 0.5), M["fabric"])
    g.box((x, y + 0.38, 0.65), (2.2, 0.2, 0.7), M["fabric"])
    for dx in (-1.05, 1.05):
        g.box((x + dx, y, 0.45), (0.2, 0.9, 0.5), M["fabric"])
    for dx in (-0.5, 0.5):
        g.box((x + dx, y - 0.05, 0.55), (0.9, 0.7, 0.12), M["fabric_pale"])


def bed(g, x, y, z, w, l, M, kid=False):
    g.box((x, y, z + 0.25), (l, w, 0.3), M["wood_furn"])
    g.box((x, y, z + 0.48), (l - 0.1, w - 0.1, 0.18), M["fabric_pale"])
    g.box((x - l / 2 + 0.05, y, z + 0.6), (0.08, w, 0.9 if not kid else 0.7), M["wood_furn"])
    g.box((x - l / 2 + 0.35, y, z + 0.62), (0.4, w * 0.6, 0.12), M["paper"])                 # pillow
    g.box((x + 0.15, y - 0.05, z + 0.6), (l * 0.65, w + 0.04, 0.06), M["fabric"], rot=(0, 0, rnd.uniform(-8, 8)))


def stairs(g, M):
    """Up the back wall from the hall to the landing (the game walks him up them)."""
    n = 16
    for i in range(n):
        x = STAIR_X0 + (STAIR_X1 - STAIR_X0) * i / n
        z = FLOOR2 * (i + 1) / n
        g.box((x + 0.16, 3.3, z / 2), ((STAIR_X1 - STAIR_X0) / n, 1.4, z), M["floorboard"])
    for i in range(0, n, 2):                                                              # the banister
        x = STAIR_X0 + (STAIR_X1 - STAIR_X0) * i / n
        z = FLOOR2 * (i + 1) / n
        g.cyl((x, 2.62, z + 0.45), 0.025, 0.9, M["skirting"], n=6)
    g.beam((STAIR_X0, 2.62, 0.95), (STAIR_X1, 2.62, FLOOR2 + 0.9), 0.06, 0.06, M["wood_furn"])


def build_shell(coll, M):
    g = S.Group("HomeShell")
    # floors, ceilings, back wall, end walls
    g.box(((X0 + X1) / 2, (YF + YB) / 2, -0.05), (X1 - X0, YB - YF, 0.1), M["floorboard"])
    g.box((29.5, (YF + YB) / 2, 0.003), (9.0, YB - YF, 0.006), M["tile"])                 # the kitchen tiles
    g.box(((X0 + X1) / 2, (YF + 2.4) / 2, FLOOR2 - 0.1), (X1 - X0, 2.4 - YF, 0.2), M["floorboard"])   # upstairs (open over the stairs)
    g.box(((STAIR_X1 + 0.2 + X1) / 2, 3.3, FLOOR2 - 0.1), (X1 - STAIR_X1 - 0.2, 1.8, 0.2), M["floorboard"])
    g.box(((X0 + X1) / 2, (YF + YB) / 2, H2 + 0.05), (X1 - X0, YB - YF, 0.1), M["plaster"])
    g.box(((X0 + X1) / 2, YB + 0.1, H2 / 2), (X1 - X0, 0.2, H2), M["plaster"])
    for x in (X0, X1):
        g.box((x, (YF + YB) / 2, H2 / 2 + 0.0), (0.2, YB - YF, H2), M["brick"])
    # the front door in the porch wall, the back door in the kitchen's end wall: openings
    g.box((X0, 0.0, 1.1), (0.22, 1.2, 2.2), M["steel"])                                    # (filled: the game opens it)
    # the roof
    for side in (-1, 1):
        g.box(((X0 + X1) / 2, (YF + YB) / 2 + side * 1.6, H2 + 1.25), (X1 - X0 + 0.8, 3.6, 0.15), M["roof"], rot=(side * 38, 0, 0))
    # cross walls with doorways
    for x in DOORS1[1:]:
        wall_with_door(g, x, 0.0, FLOOR2 - 0.2, M["plaster"])
    for x in DOORS2:
        wall_with_door(g, x, FLOOR2, H2, M["plaster_kid"] if x == 22.0 else M["plaster"])
    stairs(g, M)
    # skirting and the picture rail along the back
    for z in (0.06, FLOOR2 + 0.06):
        g.box(((X0 + X1) / 2, YB - 0.02, z), (X1 - X0, 0.04, 0.12), M["skirting"])
    # ---- the hall: coat rack (a small yellow raincoat), the table knocked over, shoes
    g.cyl((5.0, YB - 0.4, 0.95), 0.03, 1.9, M["wood_furn"], n=8)
    g.box((5.0, YB - 0.55, 1.35), (0.34, 0.12, 0.5), M["fabric_pale"])                        # her raincoat
    g.box((5.2, YB - 0.55, 1.55), (0.5, 0.15, 0.9), M["fabric"])
    g.box((7.4, 2.4, 0.15), (1.0, 0.45, 0.06), M["wood_furn"], rot=(0, 80, 20))               # the overturned table
    for i, (sx, w) in enumerate(((6.1, 0.3), (6.5, 0.18), (6.75, 0.18))):
        g.box((sx, 3.6, 0.05), (w, 0.12, 0.08), M["fabric"])
    # ---- the study: the desk (the photo is the game's), shelves, a lamp, the chair
    g.box((12.0, 3.4, 0.75), (1.8, 0.8, 0.06), M["wood_furn"])
    for dx in (-0.8, 0.8):
        g.box((12.0 + dx, 3.4, 0.37), (0.08, 0.7, 0.74), M["wood_furn"])
    g.cyl((12.7, 3.6, 0.98), 0.12, 0.4, M["lamp"], n=12, r2=0.06)
    g.box((11.2, 2.8, 0.45), (0.5, 0.5, 0.06), M["fabric"])
    for k in range(4):
        g.box((14.0, 3.9, 0.4 + k * 0.5), (1.4, 0.4, 0.04), M["wood_furn"])
        for b in range(9):
            g.box((13.4 + b * 0.13, 3.9, 0.55 + k * 0.5), (0.09, 0.25, 0.26 + rnd.uniform(-0.04, 0.04)), M["fabric_pale"] if b % 3 else M["fabric"])
    back_window(g, 10.4, 0.9, 1.4, 1.5, M)
    # ---- the living room: sofa, TV, fireplace, a floor lamp, pictures
    sofa(g, 19.0, 3.3, M)
    g.box((22.6, 3.7, 0.3), (1.6, 0.45, 0.6), M["wood_furn"])
    g.box((22.6, 3.75, 0.95), (1.4, 0.06, 0.8), M["steel"])
    g.box((16.6, 3.9, 0.7), (1.4, 0.5, 1.4), M["brick"])
    g.box((16.6, 3.62, 0.45), (0.8, 0.06, 0.7), M["steel"])
    g.cyl((24.2, 3.4, 0.8), 0.02, 1.6, M["steel"], n=6)
    g.cyl((24.2, 3.4, 1.65), 0.2, 0.3, M["lamp"], n=12, r2=0.12)
    frame(g, 19.0, 1.9, 1.0, 0.7, M)
    frame(g, 20.4, 1.85, 0.5, 0.6, M)
    back_window(g, 21.6, 0.9, 1.6, 1.6, M)
    # ---- the kitchen: counters, the sink and tap, a fridge, the island, pans, the knife block, kettle, table
    g.box((29.0, 3.85, 0.45), (7.0, 0.65, 0.9), M["wood_furn"])
    g.box((29.0, 3.85, 0.92), (7.0, 0.7, 0.05), M["tile"])
    g.box((27.4, 3.85, 0.9), (0.8, 0.5, 0.06), M["steel"])                                   # the sink
    g.beam((27.4, 4.05, 0.95), (27.4, 3.9, 1.25), 0.03, 0.03, M["steel"])
    g.box((33.2, 3.7, 1.0), (0.8, 0.8, 2.0), M["fabric_pale"])                                # fridge
    g.box((29.4, 2.2, 0.47), (2.4, 0.9, 0.94), M["wood_furn"])                                # island
    g.box((29.4, 2.2, 0.96), (2.5, 1.0, 0.05), M["tile"])
    g.beam((27.0, 3.8, 2.2), (31.0, 3.8, 2.2), 0.02, 0.02, M["steel"])                        # pan rail
    for k in range(5):
        g.cyl((27.4 + k * 0.85, 3.8, 1.9), 0.17, 0.04, M["steel"], rot=(90, 0, 0), n=14)
    g.box((30.6, 3.85, 1.07), (0.2, 0.15, 0.26), M["wood_furn"])                              # knife block
    g.cyl((31.4, 3.85, 1.05), 0.1, 0.22, M["steel"], n=12)                                    # kettle
    back_window(g, 28.6, 1.3, 1.6, 1.1, M)
    # the back door (its frame; the opening and the light through it)
    g.box((X1, 0.0, 1.1), (0.24, 1.25, 2.2), M["window_dusk"])
    # ---- upstairs: the landing, Maya's room, the bedroom
    frame(g, 10.0, FLOOR2 + 1.5, 0.8, 0.6, M)
    frame(g, 11.4, FLOOR2 + 1.6, 0.6, 0.8, M)
    back_window(g, 12.4, FLOOR2 + 0.9, 1.2, 1.4, M)
    # Maya's room: her bed, the toy chest, a rocking horse, fairy lights, her drawings, stars
    bed(g, 19.8, 3.4, FLOOR2, 1.0, 1.7, M, kid=True)
    g.box((15.6, 3.7, FLOOR2 + 0.3), (0.9, 0.5, 0.6), M["fabric_pale"])
    rx = 17.2
    g.box((rx, 2.8, FLOOR2 + 0.55), (0.6, 0.18, 0.35), M["wood_furn"])
    g.box((rx + 0.32, 2.8, FLOOR2 + 0.78), (0.2, 0.16, 0.3), M["wood_furn"])
    for dx in (-0.22, 0.22):
        g.beam((rx + dx, 2.8, FLOOR2 + 0.42), (rx + dx * 1.6, 2.8, FLOOR2 + 0.1), 0.04, 0.04, M["wood_furn"])
    for k in range(18):
        g.cyl((14.3 + k * 0.42, YB - 0.06, FLOOR2 + 2.35 - 0.12 * math.sin(k * 0.8)), 0.035, 0.03, M["fairy"], rot=(90, 0, 0), n=8)
    for k, (dx, dz) in enumerate(((15.2, 1.3), (16.1, 1.6), (17.0, 1.25), (18.0, 1.55))):
        g.box((dx, YB - 0.05, FLOOR2 + dz), (0.6, 0.02, 0.45), M["paper"], rot=(0, rnd.uniform(-6, 6), 0))
    for _ in range(14):
        g.cyl((rnd.uniform(14.3, 21.7), YB - 0.04, FLOOR2 + rnd.uniform(1.9, 2.7)), 0.05, 0.01, M["white_paint"], rot=(90, 0, 0), n=5)
    for _ in range(10):                                                                     # toys on the floor (they searched it)
        g.box((rnd.uniform(14.6, 21.4), rnd.uniform(0.6, 3.0), FLOOR2 + 0.05), (rnd.uniform(0.1, 0.25),) * 2 + (0.1,), M["fabric_pale"] if rnd.random() < 0.5 else M["fabric"], rot=(0, 0, rnd.uniform(0, 90)))
    back_window(g, 18.0, FLOOR2 + 1.0, 1.0, 1.0, M)
    # the bedroom: the double bed, wardrobe, dresser and mirror, a lamp
    bed(g, 28.5, 3.0, FLOOR2, 1.8, 2.1, M)
    g.box((23.6, 3.7, FLOOR2 + 1.1), (1.2, 0.6, 2.2), M["wood_furn"])
    g.box((32.6, 3.8, FLOOR2 + 0.45), (1.2, 0.5, 0.9), M["wood_furn"])
    g.box((32.6, 4.0, FLOOR2 + 1.4), (0.8, 0.03, 0.9), M["window_dusk"])
    g.cyl((26.8, 3.8, FLOOR2 + 0.75), 0.15, 0.3, M["lamp"], n=12, r2=0.08)
    back_window(g, 30.8, FLOOR2 + 1.0, 1.4, 1.5, M)
    return g.finish(coll)


def build_front(coll, M):
    """The front wall (the camera side): brick, windows, the porch roof. The game cuts it away."""
    g = S.Group("HomeFront")
    y = YF - 0.1
    g.box(((X0 + X1) / 2, y, H2 / 2), (X1 - X0, 0.2, H2), M["brick"])
    for x in (6.0, 12.0, 20.0, 29.0):
        for z in (1.6, FLOOR2 + 1.5):
            g.box((x, y - 0.12, z), (1.4, 0.05, 1.3), M["glass"])
    return g.finish(coll)


def build_outside(coll, M):
    """Alder Lane: the road, the pavement, No. 12 and No. 16, the gate and path with her chalk
    drawing, the black car two doors down; behind the house, the garden and the play area."""
    g = S.Group("HomeOutside")
    g.box((10.0, -11.0, -0.16), (120.0, 14.0, 0.1), M["asphalt"])
    g.box((10.0, -3.4, -0.05), (120.0, 1.6, 0.1), M["concrete"])                              # the pavement
    g.box((-14.0, 1.0, -0.06), (28.0, 8.4, 0.1), M["grass"])                                 # front gardens
    g.box((-2.0, 0.0, -0.02), (4.0, 1.4, 0.06), M["concrete"])                               # No. 14's path to the porch
    # the gate and the low wall
    g.box((-14.0, -2.4, 0.4), (24.0, 0.25, 0.8), M["brick"])
    g.box((-3.4, -2.3, 0.55), (0.06, 1.2, 1.0), M["steel"], rot=(0, 0, 50))                  # the gate, swung open
    g.box((-2.4, -2.4, 0.9), (0.25, 0.3, 0.6), M["brick"])
    g.box((-2.4, -2.55, 0.85), (0.2, 0.02, 0.15), M["white_paint"])                          # "14"
    # her chalk on the path: three stick figures holding hands, a sun
    for k, (fx, sz) in enumerate(((-2.9, 1.0), (-2.2, 1.0), (-1.6, 0.62))):
        g.box((fx, 0.0, 0.012), (0.02, 0.4 * sz, 0.004), M["chalk"])                         # body (lying on the path)
        g.cyl((fx, 0.27 * sz, 0.012), 0.07 * sz, 0.004, M["chalk"], n=12)
        g.box((fx, -0.05 * sz, 0.012), (0.3 * sz, 0.02, 0.004), M["chalk"])
    g.cyl((-1.0, 0.45, 0.012), 0.12, 0.004, M["chalk"], n=14)
    # the porch: steps and a little roof
    g.box((X0 - 0.8, 0.0, 0.08), (1.6, 1.8, 0.16), M["concrete"])
    g.box((X0 - 0.8, 0.0, 2.5), (1.8, 2.0, 0.1), M["roof"])
    # No. 12 and No. 16 (simple, the same build) behind the gardens either side
    for (x0, x1) in ((-26.0, -8.0), (40.0, 58.0)):
        g.box(((x0 + x1) / 2, 7.5, 3.0), (x1 - x0, 4.0, 6.0), M["brick"])
        g.box(((x0 + x1) / 2, 7.5, 6.8), (x1 - x0, 4.2, 1.6), M["roof"])
        for k in range(int((x1 - x0) / 4)):
            for z in (1.6, 4.4):
                g.box((x0 + 2 + k * 4, 5.45, z), (1.2, 0.05, 1.2), M["window_dusk"] if rnd.random() < 0.3 else M["glass"])
    # the black car two doors down
    cx, cy = -18.0, -5.2
    prof = [(-2.35, 0.32), (2.35, 0.32), (2.4, 0.85), (1.25, 0.95), (0.55, 1.45), (-1.05, 1.45), (-1.85, 0.95), (-2.4, 0.9)]
    g.prism([(cx + px, pz - 0.15) for px, pz in prof], cy - 0.85, cy + 0.85, M["car_black"])
    for wx in (-1.5, 1.5):
        for wy in (cy - 0.9, cy + 0.9):
            g.cyl((cx + wx, wy, 0.19), 0.34, 0.22, M["tyre"], rot=(90, 0, 0), n=16)
    # street lamps
    for x in (-22.0, -6.0):
        g.cyl((x, -2.9, 2.6), 0.06, 5.2, M["steel"], n=8)
        g.box((x, -2.6, 5.2), (0.36, 0.5, 0.12), M["steel_mid"])
    # the garden behind (the back door opens onto it) and the play area at its end
    g.box((47.0, 1.0, -0.06), (26.0, 12.0, 0.1), M["grass"])
    g.box((47.0, 7.0, 0.8), (26.0, 0.1, 1.6), M["wood_furn"])                                 # the fence
    for k in range(27):
        g.box((34.5 + k * 1.0, 7.0, 0.85), (0.08, 0.14, 1.7), M["wood_furn"])
    # the slide
    g.beam((47.0, 3.2, 0.0), (47.0, 3.2, 1.9), 0.06, 0.06, M["steel"])
    g.beam((47.6, 3.2, 0.0), (47.6, 3.2, 1.9), 0.06, 0.06, M["steel"])
    g.beam((47.3, 3.2, 1.85), (50.2, 3.2, 0.25), 0.55, 0.04, M["fabric_pale"])
    # the swing frame (the seat is separate: it moves)
    for x in (52.0, 55.2):
        g.beam((x, 2.4, 0.0), (x, 3.2, 2.4), 0.07, 0.07, M["steel"])
        g.beam((x, 4.0, 0.0), (x, 3.2, 2.4), 0.07, 0.07, M["steel"])
    g.beam((52.0, 3.2, 2.4), (55.2, 3.2, 2.4), 0.08, 0.08, M["steel"])
    # the city behind, a dusk sky
    for k in range(16):
        x = -30.0 + k * 7.0 + rnd.uniform(-2, 2)
        h = rnd.uniform(10.0, 26.0)
        g.box((x, rnd.uniform(35.0, 55.0), h / 2), (rnd.uniform(6, 9), 6.0, h), M["city"])
    g.box((15.0, 90.0, 30.0), (300.0, 1.0, 60.0), M["sky_dusk"])
    return g.finish(coll)


def build_swing(coll, M):
    g = S.Group("Swing")
    x, y = 53.6, 3.2
    for dx in (-0.25, 0.25):
        g.beam((x + dx, y, 2.38), (x + dx, y, 0.55), 0.015, 0.015, M["steel"])
    g.box((x, y, 0.52), (0.62, 0.24, 0.05), M["wood_furn"])
    return g.finish(coll)


def lights():
    def add(kind, name, loc, energy, size=1.0, rot=None):
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
    # the low sun from behind the house, through the back windows: long shafts across the floors
    sun = add("SUN", "Sun", (0, 0, 50), 2.6)
    sun.rotation_euler = (math.radians(74), 0, math.radians(180 + 20))
    sun.data.angle = math.radians(3)
    for x in (10.4, 21.6, 28.6):
        add("AREA", "WindowGlow", (x, YB - 0.5, 1.6), 45, size=1.6, rot=(math.radians(90), 0, 0))
    for x in (12.4, 18.0, 30.8):
        add("AREA", "WindowGlow2", (x, YB - 0.5, FLOOR2 + 1.6), 38, size=1.4, rot=(math.radians(90), 0, 0))
    # the lamps that are on: the study's, the living room's, Maya's fairy lights, the bedside
    for (p, e) in (((12.7, 3.4, 1.1), 30), ((24.2, 3.2, 1.7), 45), ((18.0, 3.5, FLOOR2 + 2.3), 20), ((26.8, 3.6, FLOOR2 + 0.9), 25)):
        add("POINT", "Lamp", p, e, size=0.2)
    # a soft fill from the camera side so the lane reads
    add("AREA", "Fill", (17.0, -9.0, 3.5), 1500, size=26.0, rot=(math.radians(78), 0, 0))
    add("AREA", "StreetFill", (-12.0, -10.0, 4.0), 1600, size=24.0, rot=(math.radians(75), 0, 0))
    add("AREA", "GardenFill", (47.0, -8.0, 4.0), 1300, size=22.0, rot=(math.radians(75), 0, 0))
    w = bpy.data.worlds.new("Dusk")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.26, 0.26, 0.27, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "HomeSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported HomeSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    shell = build_shell(coll, M)
    front = build_front(coll, M)
    outside = build_outside(coll, M)
    swing = build_swing(coll, M)
    lights()
    if preview_only:
        front.hide_render = True
        cams = [
            ("H2  Alder Lane at dusk", S.shot_camera("P1", (-24.0, 2.6, -14.0), (-4.0, 1.4, 2.0), 46), None),
            ("H3  the path, the chalk", S.shot_camera("P2", (-2.4, 1.6, -2.2), (-2.2, 0.0, 0.2), 50), None),
            ("H5  the study (gameplay)", S.shot_camera("P3", (12.0, 1.8, -10.5), (12.0, 1.4, 1.0), 38), None),
            ("H12  the living room (gameplay)", S.shot_camera("P4", (20.0, 1.8, -10.5), (20.0, 1.4, 1.0), 38), None),
            ("H8  the kitchen", S.shot_camera("P5", (29.5, 1.8, -10.5), (29.5, 1.3, 1.0), 38), None),
            ("H13  Maya's room", S.shot_camera("P6", (18.0, FLOOR2 + 1.8, -9.0), (18.0, FLOOR2 + 1.3, 2.0), 40), None),
            ("H14  the bedroom", S.shot_camera("P7", (28.0, FLOOR2 + 1.8, -10.0), (28.0, FLOOR2 + 1.2, 1.0), 40), None),
            ("H21  the garden, the swings", S.shot_camera("P8", (34.6, 1.4, 0.0), (52.0, 1.0, 3.0), 44), None),
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "home_set.png")
        return
    for o in (shell, front, outside, swing):
        S.unwrap(o)
    front.hide_render = True
    S.bake(shell, 4096, 24)
    S.bake(outside, 4096, 16)
    S.bake(swing, 256, 8)
    front.hide_render = False
    S.bake(front, 2048, 12)
    front.hide_render = True
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export([shell, front, outside, swing] + marks)


if __name__ == "__main__":
    main()
