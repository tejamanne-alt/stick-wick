"""Chapter 2, part two: THE STREET (docs/design/combat_v2.md, part 2). The morning after the storm:
no rain falls (it ended with the warehouse) but the street is still wet. One continuous side-on lane,
the sidewalk (y = 0), about 130 m long, from the bank's steps to the crossroads, in four zones:

  S1  OUTSIDE THE BANK      x 0-30    MERIDIAN TRUST's corner and steps, Cafe Noir's terrace
                                     (tables, chairs, its awning), a newspaper box, a bus shelter,
                                     the black car at the kerb.
  S2  THE MARKET STREET     x 30-62   stalls under striped awnings, fruit crates, hanging signs,
                                     a shopfront under repair: scaffold, planks, a cement mixer.
  S3  UNDER THE RAILWAY     x 62-92   the elevated line overhead on its pillars, a chain-link
                                     fence, dumpsters, a fire escape, strip lamps under the deck.
  S4  THE CROSSROADS        x 92-130  a wide junction: zebra crossing, traffic lights, a phone
                                     box, a newsstand, a stalled bus across the far end.

Same palette as everything else (the tower's: build_story_set's TONE): pale stone, black steel,
white trims, wet black tarmac. Moving parts are separate objects: the Driver's car (DriverCar),
the train (Train).

    /home/user/.venv-blender/bin/python tools/blender/build_street_set.py [--preview]

Writes Assets/Art/Street/StreetSet.fbx and its bakes; --preview renders docs/previews/street_set.png.
Coordinates: Blender X = Unity x, Blender Y = Unity z (depth), Blender Z = Unity y."""
import math
import os
import random
import sys

import bpy  # noqa: E402  (bpy first: it provides bmesh and mathutils)
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Street")
rnd = random.Random(23)

ZONES = (0.0, 30.0, 62.0, 92.0, 130.0)
KERB = -2.25               # the kerb: the sidewalk (the lane) behind it, the road in front, a step lower
SHOP_Y = 6.2               # the shop fronts along the back of the sidewalk
DECK_Z = 6.4               # the railway deck's underside
RAIL_X0, RAIL_X1 = 63.0, 91.0


def mats():
    M = S.materials()
    mat = S.mat
    M["stone"] = mat("Stone", (0.44, 0.44, 0.43), 0.8)
    M["stone_dark"] = mat("StoneDark", (0.2, 0.2, 0.2), 0.85)
    M["brick"] = mat("Brick", (0.3, 0.29, 0.28), 0.95)
    M["window_day"] = mat("ShopGlass", (0.22, 0.23, 0.25), 0.1, emit=(0.5, 0.52, 0.55), strength=0.35)
    M["sky_day"] = mat("SkyDay", (0.6, 0.62, 0.66), 0.5, emit=(0.8, 0.82, 0.86), strength=1.2)
    M["car_black"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["tyre"] = mat("Tyre", (0.02, 0.02, 0.02), 0.9)
    M["awning_a"] = mat("AwningA", (0.42, 0.42, 0.42), 0.9)
    M["awning_b"] = mat("AwningB", (0.12, 0.12, 0.12), 0.9)
    M["fruit"] = mat("Fruit", (0.36, 0.34, 0.3), 0.7)
    M["strip"] = mat("StripLight", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=10.0)
    M["signal"] = mat("Signal", (1, 1, 1), 0.3, emit=(1, 0.95, 0.9), strength=6.0)
    M["white_paint"] = mat("WhitePaint", (0.75, 0.75, 0.73), 0.7)
    M["chainlink"] = mat("ChainLink", (0.3, 0.31, 0.32), 0.6, metal=0.4)
    return M


S.TONE.update({"ShopGlass": 0.2, "Stone": 0.40, "StoneDark": 0.16, "Brick": 0.26, "CarBlack": 0.025, "AwningA": 0.38, "AwningB": 0.08,
               "Fruit": 0.3, "WhitePaint": 0.7, "ChainLink": 0.28})


# ---------------------------------------------------------------- pieces

def ground(g, x0, x1, M):
    """Sidewalk (the lane, z = 0), kerb, road (a step lower), centre line."""
    cx, w = (x0 + x1) / 2, x1 - x0
    g.box((cx, (KERB + SHOP_Y) / 2, -0.05), (w, SHOP_Y - KERB, 0.1), M["concrete"])
    g.box((cx, KERB, -0.08), (w, 0.3, 0.16), M["stone"])
    g.box((cx, -16.0, -0.2), (w, 27.5, 0.1), M["asphalt"])
    x = x0 + 1.0
    while x < x1 - 1.0:
        g.box((x + 1.5, -11.0, -0.145), (3.0, 0.15, 0.01), M["white_paint"])
        x += 9.0
    # paving joints and puddles, the night's rain still lying
    for k in range(int(w / 1.6)):
        g.box((x0 + 0.8 + k * 1.6, (KERB + SHOP_Y) / 2, 0.001), (0.02, SHOP_Y - KERB, 0.002), M["concrete_dark"])
    for _ in range(int(w / 7)):
        px, py = rnd.uniform(x0 + 1, x1 - 1), rnd.choice((rnd.uniform(-1.8, 1.5), rnd.uniform(-8, -3)))
        g.cyl((px, py, 0.003 if py > KERB else -0.148), rnd.uniform(0.6, 1.9), 0.004, M["puddle"], n=22)


def frontage(g, x0, x1, M, h=10.0, m=None, shop=None, sign=None):
    """A building along the back of the sidewalk: a shop at the bottom, windows above."""
    m = m or M["brick"]
    cx, w = (x0 + x1) / 2, x1 - x0
    g.box((cx, SHOP_Y + 0.2, h / 2), (w, 0.4, h), m)
    g.box((cx, SHOP_Y - 0.05, 3.1), (w, 0.2, 0.25), M["stone_dark"])                 # the fascia
    if shop == "glass":
        g.box((cx, SHOP_Y - 0.02, 1.45), (w - 1.2, 0.05, 2.5), M["window_day"])
        for xx in (x0 + 0.6, x1 - 0.6):
            g.box((xx, SHOP_Y - 0.1, 1.45), (0.12, 0.12, 2.6), M["steel"])
    elif shop == "shutter":
        S.corrugated(g, x0 + 0.6, x1 - 0.6, 0.0, 2.8, SHOP_Y - 0.08, -1, M, rust_chance=0.0)
    if sign:
        S.stencil(g, sign, (cx, SHOP_Y - 0.18, 3.1), 0.42, M["white_paint"])
    for r in range(int((h - 4.0) / 2.4)):
        for c in range(max(1, int(w / 2.6))):
            g.box((x0 + 1.3 + c * 2.6, SHOP_Y - 0.01, 4.6 + r * 2.4), (1.2, 0.05, 1.5), M["glass"])
            g.box((x0 + 1.3 + c * 2.6, SHOP_Y - 0.05, 3.8 + r * 2.4), (1.4, 0.12, 0.08), M["stone"])


def chair(g, x, y, M, yaw=0.0):
    g.box((x, y, 0.45), (0.42, 0.42, 0.04), M["steel"])
    for dx in (-0.18, 0.18):
        for dy in (-0.18, 0.18):
            g.box((x + dx, y + dy, 0.22), (0.03, 0.03, 0.45), M["steel"])
    g.box((x, y + 0.2, 0.7), (0.42, 0.03, 0.46), M["steel"])


def table(g, x, y, M):
    g.cyl((x, y, 0.74), 0.38, 0.03, M["steel"], n=18)
    g.cyl((x, y, 0.37), 0.035, 0.74, M["steel"], n=8)
    g.cyl((x, y, 0.02), 0.22, 0.03, M["steel"], n=12)


def lamp_post(g, x, M):
    g.cyl((x, KERB + 0.35, 2.7), 0.07, 5.4, M["steel"], n=10)
    g.beam((x, KERB + 0.35, 5.3), (x, KERB + 1.2, 5.5), 0.06, 0.06, M["steel"])
    g.box((x, KERB + 1.3, 5.42), (0.42, 0.3, 0.14), M["steel_mid"])


def stall(g, x, M, k):
    """A market stall: a trestle of crates, a striped awning on poles."""
    y = 2.6
    g.box((x, y, 0.8), (2.6, 1.2, 0.06), M["wood"])
    for dx in (-1.2, 1.2):
        for dy in (-0.5, 0.5):
            g.box((x + dx, y + dy, 0.4), (0.06, 0.06, 0.8), M["wood_dark"])
    for i in range(4):
        S.crate(g, (x - 0.95 + i * 0.63, y - 0.15, 0.98), (0.55, 0.4, 0.3), M)
        for j in range(5):
            g.cyl((x - 1.15 + i * 0.63 + (j % 3) * 0.13, y - 0.2 + (j // 3) * 0.12, 1.17), 0.06, 0.08, M["fruit"], n=8)
    for dx in (-1.35, 1.35):
        g.cyl((x + dx, y - 0.9, 1.25), 0.03, 2.5, M["steel"], n=6)
        g.cyl((x + dx, y + 0.7, 1.35), 0.03, 2.7, M["steel"], n=6)
    for s in range(7):                                          # the stripes
        mm = M["awning_a"] if (s + k) % 2 == 0 else M["awning_b"]
        g.box((x - 1.4 + s * 0.4 + 0.2, y - 0.1, 2.55), (0.4, 1.9, 0.03), mm, rot=(-14, 0, 0))


def scaffold(g, x0, x1, M):
    """Scaffold over a shopfront under repair: tubes, boards at two lifts, a ladder."""
    for x in [x0 + i * 2.0 for i in range(int((x1 - x0) / 2.0) + 1)]:
        for y in (SHOP_Y - 0.4, SHOP_Y - 1.6):
            g.cyl((x, y, 3.6), 0.035, 7.2, M["iron"], n=6)
    for z in (2.4, 4.8, 7.0):
        for y in (SHOP_Y - 0.4, SHOP_Y - 1.6):
            g.beam((x0, y, z), (x1, y, z), 0.05, 0.05, M["iron"])
        g.box(((x0 + x1) / 2, SHOP_Y - 1.0, z - 0.05), (x1 - x0, 1.3, 0.05), M["wood"])
    for i in range(int((x1 - x0) / 2.0)):                     # cross bracing
        g.beam((x0 + i * 2.0, SHOP_Y - 1.6, 0.1), (x0 + i * 2.0 + 2.0, SHOP_Y - 1.6, 2.4), 0.035, 0.035, M["iron"])
    lx = x0 + 0.6
    for side in (-0.22, 0.22):
        g.box((lx + side, SHOP_Y - 1.9, 1.3), (0.05, 0.05, 2.8), M["wood_dark"], rot=(-8, 0, 0))
    for r in range(8):
        g.box((lx, SHOP_Y - 1.9 + r * 0.04, 0.2 + r * 0.33), (0.44, 0.04, 0.04), M["wood_dark"])
    # the cement mixer and sacks
    mx = x1 - 2.0
    g.cyl((mx, 3.4, 0.9), 0.55, 0.9, M["paint_red"], rot=(0, 55, 0), n=16, r2=0.3)
    g.box((mx, 3.4, 0.35), (0.9, 0.5, 0.1), M["steel"])
    for i in range(3):
        g.box((mx - 1.4, 3.6, 0.12 + i * 0.22), (0.7, 0.45, 0.2), M["stone"])


def railway(g, M):
    """The elevated line: a deck on steel girders over the lane, pillars behind and in front."""
    x0, x1 = RAIL_X0, RAIL_X1
    g.box(((x0 + x1) / 2, 1.0, DECK_Z + 0.5), (x1 - x0, 14.0, 1.0), M["concrete_dark"])         # the deck
    for y in (-5.0, 7.0):
        g.box(((x0 + x1) / 2, y, DECK_Z - 0.4), (x1 - x0, 0.4, 0.8), M["steel"])               # girders
    for k in range(int((x1 - x0) / 1.2)):
        g.box((x0 + 0.6 + k * 1.2, 1.0, DECK_Z - 0.1), (0.16, 12.0, 0.2), M["steel"])          # the sleepers' undersides
    for x in (x0 + 1.0, x0 + 10.0, x0 + 19.0, x1 - 1.0):
        g.box((x, 7.0, DECK_Z / 2), (0.9, 0.9, DECK_Z), M["concrete"])                          # pillars behind
    for x in (x0 + 5.0, x0 + 14.0, x0 + 23.0):
        g.box((x, 1.0, DECK_Z - 0.25), (2.2, 0.12, 0.08), M["strip"])                           # strip lamps under it
    # parapet and rails on top
    for y in (-5.8, 7.8):
        g.box(((x0 + x1) / 2, y, DECK_Z + 1.4), (x1 - x0, 0.2, 0.8), M["concrete"])
    # the fence, the dumpsters, a fire escape
    for k in range(int((x1 - x0) / 2.5)):
        g.cyl((x0 + 1.2 + k * 2.5, 4.4, 1.25), 0.03, 2.5, M["steel"], n=6)
    g.box(((x0 + x1) / 2, 4.4, 1.25), (x1 - x0 - 2.0, 0.02, 2.4), M["chainlink"])
    for x in (x0 + 4.0, x0 + 16.5):
        g.box((x, 3.2, 0.65), (1.9, 1.1, 1.3), M["paint_green"])
        g.box((x, 3.2, 1.33), (1.95, 1.15, 0.06), M["steel"], rot=(-6, 0, 0))
    fx = x1 - 6.0
    for z in (3.2, 5.6):
        g.box((fx, SHOP_Y - 0.7, z), (3.0, 1.2, 0.06), M["steel"])
        for xx in (fx - 1.5, fx + 1.5):
            g.cyl((xx, SHOP_Y - 1.3, z + 0.45), 0.02, 0.9, M["steel"], n=5)
    g.beam((fx - 1.2, SHOP_Y - 0.5, 3.2), (fx + 1.2, SHOP_Y - 0.5, 5.6), 0.5, 0.05, M["steel"])


def crossroads(g, M):
    """The junction: zebra crossing, signals, the phone box, the newsstand, the stalled bus."""
    x0, x1 = ZONES[3], ZONES[4]
    # the side street runs back between the buildings at 104-112 (the zebra across it)
    for k in range(8):
        g.box((104.4 + k * 1.0, 2.0, 0.003), (0.55, 7.6, 0.004), M["white_paint"])
    for x in (98.0, 121.0):
        g.cyl((x, KERB + 0.3, 1.7), 0.06, 3.4, M["steel"], n=8)
        g.box((x, KERB + 0.3, 3.7), (0.32, 0.3, 0.9), M["steel"])
        for i, m in enumerate((M["signal"], M["steel_mid"], M["steel_mid"])):
            g.cyl((x, KERB + 0.12, 4.0 - i * 0.28), 0.09, 0.03, m, rot=(90, 0, 0), n=12)
    # the phone box
    px = 114.5
    g.box((px, 3.4, 1.2), (1.0, 1.0, 2.4), M["steel"])
    g.box((px, 2.88, 1.2), (0.8, 0.04, 1.9), M["glass"])
    g.box((px, 3.4, 2.5), (1.1, 1.1, 0.2), M["stone_dark"])
    # the newsstand
    nx = 118.5
    g.box((nx, 3.6, 1.2), (2.6, 1.6, 2.4), M["stone_dark"])
    g.box((nx, 2.6, 2.5), (3.0, 1.0, 0.08), M["awning_b"], rot=(-12, 0, 0))
    for i in range(6):
        g.box((nx - 1.0 + i * 0.4, 2.78, 1.3), (0.3, 0.04, 0.42), M["white_paint"])
    # the stalled bus across the far end
    bx = 131.5
    g.box((bx, 0.5, 1.7), (12.0, 2.6, 2.9), M["stone"])
    g.box((bx, 0.5, 3.25), (12.0, 2.6, 0.2), M["stone_dark"])
    for i in range(7):
        g.box((bx - 5.2 + i * 1.6, -0.82, 2.2), (1.3, 0.04, 1.0), M["glass"])
    for wx in (bx - 4.2, bx + 4.2):
        for wy in (-0.8, 1.8):
            g.cyl((wx, wy, 0.48), 0.48, 0.32, M["tyre"], rot=(90, 0, 0), n=16)


def black_car(name, c, M, coll, open_door=False):
    """The sedan: a separate object (the Driver's moves; the kerb one is static)."""
    g = S.Group(name)
    x, y, z = c
    prof = [(-2.35, 0.32), (2.35, 0.32), (2.4, 0.85), (1.25, 0.95), (0.55, 1.45), (-1.05, 1.45), (-1.85, 0.95), (-2.4, 0.9)]
    g.prism([(x + px, z + pz) for px, pz in prof], y - 0.85, y + 0.85, M["car_black"])
    for wx in (-1.5, 1.5):
        for wy in (y - 0.9, y + 0.9):
            g.cyl((x + wx, wy, z + 0.34), 0.34, 0.22, M["tyre"], rot=(90, 0, 0), n=16)
    g.box((x - 0.25, y - 0.86, z + 1.15), (1.9, 0.02, 0.4), M["glass"])
    g.box((x + 0.85, y, z + 1.18), (0.05, 1.6, 0.45), M["glass"], rot=(0, 34, 0))
    g.box((x + 2.38, y, z + 0.6), (0.06, 1.5, 0.12), M["steel_mid"])                         # the grille
    return g.finish(coll)


def train(coll, M):
    """Four carriages on the deck (the game runs them across)."""
    g = S.Group("Train")
    for k in range(4):
        cx = 75.0 + k * 15.2
        g.box((cx, 1.0, DECK_Z + 2.6), (14.6, 2.8, 3.2), M["stone_dark"])
        for i in range(8):
            g.box((cx - 6.2 + i * 1.75, -0.42, DECK_Z + 3.0), (1.1, 0.04, 0.9), M["window_day"])
        for wx in (cx - 5.0, cx + 5.0):
            g.box((wx, 1.0, DECK_Z + 1.0), (2.0, 2.0, 0.5), M["steel"])
    return g.finish(coll)


# ---------------------------------------------------------------- the zones

def build_s1(coll, M):
    g = S.Group("StreetA")
    ground(g, -14.0, 46.0, M)
    # MERIDIAN TRUST's corner: the stone front, a column and the steps up to its doors
    g.box((-6.0, SHOP_Y + 0.2, 6.0), (16.0, 0.4, 12.0), M["stone"])
    for x in (-10.0, -6.5, -3.0):
        g.cyl((x, SHOP_Y - 0.9, 3.4), 0.4, 6.8, M["stone"], n=20)
    g.box((-6.5, SHOP_Y - 0.9, 7.0), (9.0, 1.8, 0.5), M["stone"])
    g.box((-6.5, SHOP_Y - 0.1, 2.0), (2.6, 0.06, 4.0), M["steel"])                            # the bronze doors
    for k in range(3):
        g.box((-6.5, SHOP_Y - 1.9 - k * 0.35, 0.08 + k * 0.0), (10.0 - k * 0.6, 0.4, 0.04), M["stone"])
    S.stencil(g, "MERIDIAN  TRUST", (-6.5, SHOP_Y - 1.82, 7.0), 0.34, M["white_paint"])
    frontage(g, 2.0, 9.0, M, h=12.0, m=M["stone"], shop="shutter")
    frontage(g, 9.0, 21.0, M, h=10.0, shop="glass", sign="CAFE  NOIR")
    g.box((15.0, 4.6, 3.0), (11.6, 3.2, 0.06), M["awning_b"], rot=(-11, 0, 0))                # the awning
    for k in range(3):
        table(g, 11.6 + k * 3.3, 3.6, M)
        chair(g, 11.0 + k * 3.3, 3.9, M)
        chair(g, 12.2 + k * 3.3, 3.9, M)
    frontage(g, 21.0, 30.0, M, h=14.0, m=M["stone_dark"], shop="glass", sign="PHARMACY")
    frontage(g, 30.0, 46.0, M, h=9.0, shop="shutter")
    # the bus shelter (its front glass is the game's) and the newspaper box
    sx = 25.5
    g.box((sx, 2.2, 2.5), (3.6, 1.6, 0.08), M["steel"])
    for xx in (sx - 1.75, sx + 1.75):
        g.box((xx, 2.2, 1.25), (0.08, 1.4, 2.5), M["steel"])
    g.box((sx, 2.95, 1.3), (3.4, 0.04, 2.2), M["glass"])
    g.box((sx, 2.6, 0.45), (3.0, 0.4, 0.06), M["wood"])
    g.box((22.5, 2.0, 0.55), (0.5, 0.45, 1.1), M["paint_blue"])
    for x in (-1.0, 12.0, 24.0, 38.0):
        lamp_post(g, x, M)
    for x in (4.0, 6.0, 8.0):
        g.cyl((x, KERB + 0.25, 0.45), 0.11, 0.9, M["steel"], n=10)
    return g.finish(coll)


def build_s2(coll, M):
    g = S.Group("StreetB")
    ground(g, 46.0, 92.0, M)
    frontage(g, 46.0, 52.0, M, h=11.0, shop="glass", sign="FISH")
    frontage(g, 52.0, 60.0, M, h=12.0, m=M["stone"], shop="shutter")
    frontage(g, 60.0, 63.0, M, h=10.0, shop="glass")
    for i, x in enumerate((33.0, 37.0, 41.0, 44.5)):
        stall(g, x, M, i)
    scaffold(g, 52.0, 60.0, M)
    for x, word in ((34.0, "FRUIT"), (47.5, "OPEN")):                                          # hanging signs
        g.beam((x, 4.4, 3.6), (x, 4.4, 4.2), 0.03, 0.03, M["steel"])
        g.box((x, 4.4, 3.3), (1.4, 0.05, 0.55), M["stone_dark"])
        S.stencil(g, word, (x, 4.36, 3.3), 0.24, M["white_paint"])
    railway(g, M)
    frontage(g, 63.0, 92.0, M, h=6.0, m=M["brick"], shop="shutter")                           # the arches' back wall
    lamp_post(g, 50.0, M)
    return g.finish(coll)


def build_s3(coll, M):
    g = S.Group("StreetC")
    ground(g, 92.0, 142.0, M)
    frontage(g, 92.0, 104.0, M, h=13.0, m=M["stone"], shop="glass", sign="HOTEL  ARCADIA")
    frontage(g, 112.0, 124.0, M, h=11.0, shop="shutter")
    frontage(g, 124.0, 142.0, M, h=15.0, m=M["stone_dark"], shop="glass")
    # the side street between them, going back into the city
    g.box((108.0, 30.0, -0.05), (8.0, 50.0, 0.1), M["asphalt"])
    for y in (12.0, 22.0):
        g.box((103.6, y, 6.0), (0.4, 8.0, 12.0), M["brick"])
        g.box((112.4, y, 7.0), (0.4, 8.0, 14.0), M["stone"])
    crossroads(g, M)
    for x in (96.0, 126.0):
        lamp_post(g, x, M)
    return g.finish(coll)


def build_backdrop(coll, M):
    g = S.Group("StreetCity")
    for k in range(18):
        x0 = -20.0 + k * 9.5
        h = rnd.uniform(9.0, 18.0)
        g.box((x0 + 4.5, -30.5, h / 2), (9.2, 1.0, h), M["block"] if k % 2 else M["stone"])
        for r in range(int(h / 2.6)):
            for c in range(3):
                g.box((x0 + 1.5 + c * 3.0, -29.95, 1.6 + r * 2.6), (1.5, 0.05, 1.5), M["glass"])
    for k in range(22):
        x = -30.0 + k * 8.5 + rnd.uniform(-2, 2)
        h = rnd.uniform(22.0, 60.0)
        g.box((x, rnd.uniform(40.0, 70.0), h / 2), (rnd.uniform(7, 12), rnd.uniform(7, 12), h), M["city"])
    g.box((60.0, 120.0, 40.0), (420.0, 1.0, 80.0), M["sky_day"])
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
    sun = add("SUN", "Sun", (0, 0, 50), 2.2)
    sun.rotation_euler = (math.radians(55), 0, math.radians(-35))
    sun.data.angle = math.radians(12)
    for x in (10.0, 40.0, 110.0):
        add("AREA", "StreetFill", (x, -10.0, 4.0), 2600, size=30.0, rot=(math.radians(75), 0, 0))
    # under the railway: dim, the strip lamps and a cold spill from each end
    for x in (RAIL_X0 + 5.0, RAIL_X0 + 14.0, RAIL_X0 + 23.0):
        add("AREA", "Strip", (x, 1.0, DECK_Z - 0.35), 380, size=2.2, rot=(0, 0, 0))
    add("AREA", "RailFill", (77.0, -9.0, 2.6), 700, size=14.0, rot=(math.radians(80), 0, 0))
    w = bpy.data.worlds.new("Morning")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.32, 0.33, 0.35, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "StreetSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported StreetSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    a = build_s1(coll, M)
    b = build_s2(coll, M)
    c = build_s3(coll, M)
    city = build_backdrop(coll, M)
    kerb_car = black_car("BlackCar", (16.0, -3.3, -0.15), M, coll)
    driver_car = black_car("DriverCar", (122.0, -3.3, -0.15), M, coll)
    tr = train(coll, M)
    lights()
    if preview_only:
        cams = [
            ("S1  outside the bank (gameplay)", S.shot_camera("P1", (12.0, 2.2, -12.0), (12.0, 1.5, 0.0), 40), None),
            ("S1  wide: the bank, the cafe, the car", S.shot_camera("P2", (-4.0, 1.8, -8.0), (16.0, 2.4, 2.0), 50), None),
            ("S2  the market street", S.shot_camera("P3", (44.0, 2.2, -12.0), (44.0, 1.6, 0.0), 40), None),
            ("S2  the scaffold", S.shot_camera("P4", (56.0, 2.2, -12.0), (56.0, 2.2, 0.0), 40), None),
            ("S3  under the railway", S.shot_camera("P5", (76.0, 2.2, -12.0), (76.0, 2.0, 0.0), 40), None),
            ("S3  the train overhead", S.shot_camera("P6", (78.0, 0.8, -6.0), (78.0, 7.0, 1.0), 56), None),
            ("S4  the crossroads", S.shot_camera("P7", (110.0, 2.4, -13.0), (110.0, 1.6, 0.0), 42), None),
            ("S4  the Driver's car and the bus", S.shot_camera("P8", (114.0, 1.4, -7.0), (126.0, 1.4, 0.0), 46), None),
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "street_set.png")
        return
    objs = [a, b, c, city, kerb_car, driver_car, tr]
    for o in objs:
        S.unwrap(o)
    S.bake(a, 4096, 20)
    S.bake(b, 4096, 20)
    S.bake(c, 4096, 20)
    S.bake(city, 1024, 8)
    for o in (kerb_car, driver_car, tr):
        S.bake(o, 512, 12)
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(objs + marks)


if __name__ == "__main__":
    main()
