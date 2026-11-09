"""Chapter 2, part two: THE STREET (docs/story/chapter2_street.md). The morning after the storm:
no rain falls (it ended with the warehouse) but the street is still wet. One continuous side-on lane,
the sidewalk (y = 0), about 130 m long, from the bank's steps to the crossroads, in four zones:

  S1  OUTSIDE THE BANK      x 0-30    MERIDIAN TRUST's corner, columns and steps, Cafe Noir's terrace
                                     (bistro tables, chairs, its striped awning), a pharmacy, a
                                     newspaper box, a bus shelter, the black car at the kerb.
  S2  THE MARKET STREET     x 30-62   stalls under striped awnings, crates of fruit with price cards,
                                     strings of bulbs, hanging signs, the fish shop, a shopfront under
                                     repair: scaffold with boards and couplers, a ladder, a mixer, sacks.
  S3  UNDER THE RAILWAY     x 62-92   the elevated line overhead on riveted steel girders and pillars,
                                     the brick arches behind with their shutters, a chain-link fence,
                                     dumpsters, a fire escape, strip lamps under the deck.
  S4  THE CROSSROADS        x 92-130  Hotel Arcadia with its canopy and revolving door, the side street
                                     going back north (a zebra across it), traffic lights, a phone box,
                                     a newsstand, the stalled bus across the far end.

Real detail from city_kit.py and detail.py: wet paving and tarmac, kerb stones, drains, manholes,
road markings, cornices, framed windows, lamp posts, bollards, benches, bins, bicycles, posters,
graffiti, AC units, pigeons. Same palette as everything else (the tower's: build_story_set's TONE):
pale stone, black steel, white trims, wet black tarmac. Moving parts are separate objects: the
Driver's car (DriverCar), the train (Train); their names and pivots are what StoryStreet expects.
Nothing new stands on the sidewalk between y -0.8 and 0.8 (the lane), and the crossroads keep the
pavement clear for the Driver's car (y -1.1..0.9) and the dive into the doorways.

    /home/user/.venv-blender/bin/python tools/blender/build_street_set.py               # build, bake, export
    /home/user/.venv-blender/bin/python tools/blender/build_street_set.py --preview     # the preview sheet only
    /home/user/.venv-blender/bin/python tools/blender/build_street_set.py --preview --shots=1,5

Writes Assets/Art/Street/StreetSet.fbx and its bakes; --preview renders docs/previews/street_set.png
(no bake, no export). Coordinates: Blender X = Unity x, Blender Y = Unity z (depth), Blender Z = Unity y."""
import math
import os
import random
import sys

import bpy  # noqa: E402  (bpy first: it provides mathutils)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import city_kit as K  # noqa: E402
import detail as D  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Street")
rnd = random.Random(23)

ZONES = (0.0, 30.0, 62.0, 92.0, 130.0)
KERB = -2.25               # the kerb: the sidewalk (the lane) behind it, the road in front, a step lower
SHOP_Y = 6.2               # the shop fronts along the back of the sidewalk
DECK_Z = 6.4               # the railway deck's underside
RAIL_X0, RAIL_X1 = 63.0, 91.0
CRASH_X = 92.6             # StreetPlan.CrashX: the Driver's car ends against the railway's last pier


def mats():
    M = S.materials()
    mat = S.mat
    M["stone"] = mat("Stone", (0.44, 0.44, 0.43), 0.8)
    M["stone_dark"] = mat("StoneDark", (0.2, 0.2, 0.2), 0.85)
    M["window_day"] = mat("ShopGlass", (0.22, 0.23, 0.25), 0.1, emit=(0.5, 0.52, 0.55), strength=0.35)
    M["sky_day"] = mat("SkyDay", (0.6, 0.62, 0.66), 0.5, emit=(0.8, 0.82, 0.86), strength=1.2)
    M["car_black"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["car_grey"] = mat("CarGreyDay", (0.2, 0.2, 0.2), 0.3, metal=0.5)
    M["car_pale"] = mat("CarPaleDay", (0.45, 0.45, 0.45), 0.3, metal=0.5)
    M["strip"] = mat("StripLight", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=10.0)
    M["signal"] = mat("Signal", (1, 1, 1), 0.3, emit=(1, 0.95, 0.9), strength=6.0)
    M["bulb"] = mat("MarketBulb", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=8.0)
    K.mats(M)
    M["glass_ext"] = K.grey("DayGlass", 0.3, 0.16)                       # windows by day: pale, catching the sky
    M["white_paint"] = K.grey("StreetWhitePaint", 0.72, 0.6)
    M["brass"] = K.grey("StreetBrass", 0.52, 0.28, metal=0.8)
    M["sf_paint"] = K.grey("StreetShopPaint", 0.05, 0.45)
    M["sf_pale"] = K.grey("StreetShopPale", 0.5, 0.5)
    M["frame_dark"] = K.grey("StreetFrameDark", 0.04, 0.5, metal=0.4)
    M["frame_pale"] = K.grey("StreetFramePale", 0.6, 0.5)
    M["chainlink"] = K.grey("StreetChainLink", 0.28, 0.5, metal=0.5)
    M["ice"] = K.grey("StreetIce", 0.7, 0.15)
    M["fish"] = K.grey("StreetFish", 0.45, 0.25, metal=0.4)
    M["sack"] = K.grey("StreetSack", 0.5, 1.0)
    M["bus"] = K.grey("StreetBus", 0.42, 0.35, metal=0.3)
    M["bus_band"] = K.grey("StreetBusBand", 0.06, 0.4)
    # surfaces with texture (object space)
    M["trim"] = K.courses("StreetTrim", 0.47, 0.4, "xz", (3.0, 1.5), joint_w=0.002, grain=0.25, grime=0.3, bump=0.1)
    M["trim_w"] = K.courses("StreetTrim", 0.47, 0.4, "yz", (3.0, 1.5), joint_w=0.002, grain=0.25, grime=0.3, bump=0.1)
    M["ashlar"] = K.courses("StreetAshlar", 0.4, 0.2, "xz", (1.1, 0.5), joint_w=0.008, grime=0.35)
    M["ashlar_dark"] = K.courses("StreetAshlarDark", 0.16, 0.08, "xz", (1.1, 0.5), joint_w=0.008, grime=0.35)
    M["rustic"] = K.courses("StreetRustic", 0.34, 0.1, "xz", (1.2, 0.5), joint_w=0.03, grime=0.5, bump=0.9)
    M["brick"] = K.courses("StreetBrick", 0.24, 0.12, "xz", (0.23, 0.075), joint_w=0.008, var=0.65, grime=0.5)
    M["brick_w"] = K.courses("StreetBrick", 0.24, 0.12, "yz", (0.23, 0.075), joint_w=0.008, var=0.65, grime=0.5)
    M["brick_dark"] = K.courses("StreetBrickDark", 0.12, 0.07, "xz", (0.23, 0.075), joint_w=0.008, var=0.7, grime=0.5)
    M["stucco"] = K.courses("StreetStucco", 0.46, 0.4, "xz", (4.0, 2.0), joint_w=0.002, grain=0.35, grime=0.55, bump=0.05)
    M["concrete_tex"] = K.courses("StreetConcrete", 0.3, 0.2, "xz", (2.0, 1.0), joint_w=0.004, grain=0.4, grime=0.6, bump=0.1)
    M["concrete_w"] = K.courses("StreetConcrete", 0.3, 0.2, "yz", (2.0, 1.0), joint_w=0.004, grain=0.4, grime=0.6, bump=0.1)
    M["paving"] = K.courses("StreetPaving", 0.26, 0.1, "xy", (0.9, 0.6), joint_w=0.012, grime=0.55, rough=0.35, bump=0.3)
    M["setts"] = D.cobbles("StreetSetts", 0.2, wet=0.7)
    M["kerb"] = K.courses("StreetKerb", 0.4, 0.12, "xz", (1.0, 1.0), joint_w=0.008, rough=0.5)
    M["road"] = K.tarmac("StreetTarmac", 0.06, 0.75)
    M["planks"] = D.planks("StreetBoards", 0.18, "xy", board=0.22)
    M["planks_w"] = D.planks("StreetBoards", 0.18, "xz", board=0.22)
    return M


S.TONE.update({"ShopGlass": 0.2, "Stone": 0.40, "StoneDark": 0.16, "CarBlack": 0.025, "CarGreyDay": 0.16,
               "CarPaleDay": 0.42, "ExtGlass": 0.2})


# ---------------------------------------------------------------- the ground

def ground(g, x0, x1, M):
    """Sidewalk (the lane, z = 0) in wet paving, kerb stones, a gutter of setts, the road (a step lower)
    with its markings, drains, manholes and the night's puddles."""
    f = K.L(g)
    cx, w = (x0 + x1) / 2, x1 - x0
    f.box((cx, (KERB + SHOP_Y) / 2, -0.05), (w, SHOP_Y - KERB, 0.1), M["paving"])
    f.box((cx, KERB, -0.08), (w, 0.3, 0.16), M["kerb"], bevel=0.03)
    x = x0 + 1.0
    while x < x1:
        f.box((x, KERB, -0.08), (0.012, 0.32, 0.17), M["reveal"])
        x += 1.0
    f.box((cx, KERB - 0.4, -0.152), (w, 0.5, 0.01), M["setts"])
    f.box((cx, -16.0, -0.2), (w, 27.5, 0.1), M["road"])
    x = x0 + 1.0
    while x < x1 - 1.0:
        f.box((x + 1.5, -11.0, -0.146), (3.0, 0.15, 0.008), M["white_paint"])
        x += 9.0
    f.box((cx, -3.3, -0.146), (w, 0.1, 0.008), M["white_paint"])
    x = x0 + 4.0
    while x < x1:
        K.drain(f, x, KERB - 0.38, M, w=0.8, d=0.36, z=-0.148)
        x += 12.0
    for k in range(int(w / 20)):
        K.manhole(f, x0 + 8 + k * 20 + rnd.uniform(-3, 3), rnd.choice((-7.5, -14.0)), M, z=-0.148)
        K.manhole(f, x0 + 14 + k * 20, rnd.uniform(4.2, 5.4), M, r=0.3, z=0.0)
    for _ in range(int(w / 6)):
        px = rnd.uniform(x0 + 1, x1 - 1)
        py = rnd.choice((rnd.uniform(-1.6, 1.8), rnd.uniform(-9, -2.8), rnd.uniform(-1.6, 1.8)))
        K.puddle(f, px, py, rnd.uniform(0.5, 1.7), M["puddle"], z=0.0 if py > KERB else -0.148, seed=px * 0.31, squash=0.6)


def street_lamp(f, x, M):
    K.lamp_post(f, x, KERB + 0.35, M, arm=-1.0, h=5.4)


def bistro(f, x, y, M):
    f.lathe((x, y, 0.0), [(0.0, 0.0), (0.24, 0.0), (0.24, 0.02), (0.04, 0.05), (0.03, 0.72), (0.0, 0.72)], M["iron"], n=12)
    f.lathe((x, y, 0.72), [(0.0, 0.0), (0.38, 0.0), (0.39, 0.025), (0.38, 0.04), (0.0, 0.04)], M["trim"], n=20)
    f.lathe((x + 0.1, y - 0.05, 0.76), [(0.0, 0.0), (0.04, 0.0), (0.035, 0.08), (0.0, 0.08)], M["paper"], n=8)
    f.lathe((x - 0.12, y + 0.06, 0.76), [(0.0, 0.0), (0.03, 0.0), (0.012, 0.05), (0.012, 0.16), (0.0, 0.17)], M["glass_ext"], n=8)


def bistro_chair(f, x, y, yaw, M):
    cs = f.sub((x, y, 0), yaw)
    cs.lathe((0, 0, 0.44), [(0.0, 0.0), (0.2, 0.0), (0.2, 0.04), (0.0, 0.04)], M["slat"], n=12)
    for q in range(4):
        a = q / 4 * math.tau + 0.78
        cs.rod((math.cos(a) * 0.16, math.sin(a) * 0.16, 0.44), (math.cos(a) * 0.2, math.sin(a) * 0.2, 0.0), 0.012, M["iron"], 4)
    cs.torus((0, 0.19, 0.68), 0.17, 0.012, M["iron"], axis="y", n1=12, n2=4)
    cs.rod((-0.17, 0.19, 0.44), (-0.17, 0.19, 0.68), 0.012, M["iron"], 4)
    cs.rod((0.17, 0.19, 0.44), (0.17, 0.19, 0.68), 0.012, M["iron"], 4)


def pigeons(f, pts, M):
    for (x, y, z) in pts:
        K.pigeon(f, x, y, z, rnd.uniform(-70, 70), M)


# ---------------------------------------------------------------- S1: outside the bank

def bank_corner(g, M):
    """MERIDIAN TRUST's corner: a rusticated stone front with its doors and the mark over them,
    four Ionic columns before it, the entablature with the name, steps, lanterns, brass plates."""
    f = K.L(g, (0, SHOP_Y, 0), 0)
    x0, x1, h = -14.0, 2.0, 12.0
    K.wall(f, x0, x1, 0.0, h, 0.4, M["ashlar"], [(-6.5, 2.6, 0.0, 4.0, False), (-11.5, 1.4, 1.5, 5.6, True), (-1.5, 1.4, 1.5, 5.6, True)], v=0.2)
    f.box((-6.5, 0.3, 2.0), (2.6, 0.2, 4.0), M["reveal"])
    for s in (-1, 1):                                                                              # the bronze doors
        f.box((-6.5 + s * 0.65, 0.18, 1.9), (1.26, 0.06, 3.8), M["iron"], bevel=0.01)
        f.box((-6.5 + s * 0.65, 0.14, 2.2), (0.9, 0.02, 2.6), M["glass_ext"])
        f.box((-6.5 + s * 0.15, 0.12, 1.2), (0.04, 0.05, 0.5), M["brass"])
    f.box((-6.5, 0.14, 3.85), (2.6, 0.06, 0.25), M["brass"])
    # rustication and plinth round the openings
    z, row = 0.0, 0
    while z < 4.6:
        x = x0 + (0.6 if row % 2 else 0.0)
        while x < x1:
            xb = min(x + 1.2, x1)
            if not (xb > -8.2 and x < -4.8 and z < 4.6) and not any(xb > c - 1.0 and x < c + 1.0 and z + 0.5 > 1.3 and z < 6.4 for c in (-11.5, -1.5)):
                f.box(((x + xb) / 2, -0.06, z + 0.24), (xb - x - 0.04, 0.12, 0.46), M["rustic"], bevel=0.04)
            x = xb
        z += 0.5
        row += 1
    K.string_course(f, x0, x1, 4.7, M["trim"], h=0.2, d=0.16)
    for c in (-11.5, -1.5):
        K.window(f, c, 1.5, 5.6, 1.4, M, M["frame_dark"], M["glass_ext"], M["trim"], head="arch", cols=2, rows=4, grille=2.0)
    # the mark over the doors in a wreath, the bronze name on the frieze over them
    fm = K.L(g, (0, SHOP_Y - 0.12, 0), 0)
    fm.box((-6.5, 0.06, 5.6), (2.3, 0.12, 2.3), M["ashlar_dark"])
    pts = [(-0.75, 0.85), (0.75, 0.85)] + [(0.75 * math.sqrt(max(0.0, (v + 0.95) / 1.05)), v) for v in [0.1 - i / 12 * 1.05 for i in range(1, 12)]] + [(0.0, -0.95)]
    pts += [(-0.75 * math.sqrt(max(0.0, (v + 0.95) / 1.05)), v) for v in [0.1 - i / 12 * 1.05 for i in range(11, 0, -1)]]
    fm.wallshape([(px * 0.9, pz * 0.9) for px, pz in pts], -0.06, 0.0, M["trim"], c=(-6.5, 0, 5.6))
    fm.cyl((-6.5, -0.07, 5.8), 0.17, 0.02, M["ashlar_dark"], axis="y", n=16)
    fm.wallshape([(-0.06, 0.0), (0.06, 0.0), (0.12, -0.6), (-0.12, -0.6)], -0.08, -0.06, M["ashlar_dark"], c=(-6.5, 0, 5.75))
    for s in (-1, 1):
        K.laurel(fm, -6.5 + s * 0.35, 4.75, -6.5 + s * 1.0, 6.2, M["trim"], n=8)
    # the portico: columns on the steps, the entablature, the name, the cornice
    for k in range(3):
        f.box((-6.5, -1.5 - k * 0.38, 0.03 + (2 - k) * 0.03), (10.4 - k * 0.6, 0.4, 0.06 + (2 - k) * 0.06), M["trim"], bevel=0.02)
    for x in (-10.0, -6.5 - 1.75, -6.5 + 1.75, -3.0):
        K.column(f, x, -0.9, 0.18, 6.62, 0.36, M["trim"], M["trim"])
    f.box((-6.5, -0.9, 7.05), (9.4, 1.9, 0.8), M["trim"])
    for k, z in enumerate((6.75, 6.88, 7.0)):
        f.box((-6.5, -1.86 - k * 0.02, z), (9.4, 0.04 + k * 0.02, 0.12), M["trim"])
    f.text("MERIDIAN   TRUST", (-6.5, -1.88, 7.22), 0.32, M["ashlar_dark"], depth=0.02, kind="serif", spacing=1.15)
    K.cornice(f.sub((0, -1.85, 0), 0), -11.2, -1.8, 7.45, M["trim"], depth=0.45, modillions=True)
    K.cornice(f, x0, x1, h - 0.6, M["trim"], depth=0.5, modillions=True)
    f.box((-6.5, -0.9, 8.25), (9.6, 1.8, 0.2), M["ashlar_dark"])
    for s in (-1, 1):                                                                              # lanterns, brass plates
        u = -6.5 + s * 1.65
        f.box((u, -0.04, 2.95), (0.14, 0.08, 0.4), M["iron"], bevel=0.02)
        f.beam((u, -0.04, 2.9), (u, -0.4, 3.0), 0.04, 0.04, M["iron"])
        f.lathe((u, -0.42, 2.66), [(0.0, 0.0), (0.05, 0.0), (0.12, 0.08), (0.14, 0.32), (0.1, 0.38), (0.0, 0.38)], M["lamp_glass"], n=6)
        f.lathe((u, -0.42, 3.03), [(0.0, 0.0), (0.17, 0.0), (0.06, 0.14), (0.0, 0.2)], M["iron"], n=6)
    f.box((-8.6, -0.02, 1.6), (0.5, 0.04, 0.7), M["brass"], bevel=0.02)
    for k, (t, sz) in enumerate((("MERIDIAN", 0.075), ("TRUST", 0.075), ("BANKERS", 0.045), ("EST.  1887", 0.045))):
        f.text(t, (-8.6, -0.045, 1.81 - k * 0.13), sz, M["ink"], kind="serif")
    # a balustrade along the roof, a flagpole with the flag hanging still
    y = x0 + 0.2
    while y < x1:
        f.lathe((y, 0.1, h + 0.2), [(0.09, 0.0), (0.09, 0.05), (0.06, 0.1), (0.11, 0.3), (0.07, 0.5), (0.05, 0.55), (0.08, 0.62), (0.0, 0.63)], M["trim"], n=8)
        y += 0.26
    f.box(((x0 + x1) / 2, 0.1, h + 0.1), (x1 - x0, 0.5, 0.2), M["trim"])
    f.box(((x0 + x1) / 2, 0.1, h + 0.9), (x1 - x0, 0.45, 0.14), M["trim"], bevel=0.02)


def bus_shelter(f, sx, M):
    """The bus shelter (its front glass is the game's): a steel frame, a roof with a gutter, the back
    glass, a slatted bench, the timetable, a lit advertisement on the end panel."""
    for xx in (sx - 1.75, sx + 1.75):
        for yy in (1.55, 2.9):
            f.box((xx, yy, 1.25), (0.08, 0.08, 2.5), M["iron"])
    f.box((sx, 2.2, 2.53), (3.7, 1.7, 0.08), M["iron"], bevel=0.02)
    f.box((sx, 2.2, 2.6), (3.5, 1.5, 0.05), M["glass_ext"])
    f.box((sx, 1.36, 2.5), (3.7, 0.06, 0.16), M["iron"])
    f.box((sx, 1.33, 2.5), (2.4, 0.01, 0.1), M["white_band"])
    f.text("BUS  STOP  ·  14  ·  22  ·  31", (sx, 1.325, 2.5), 0.07, M["ink"], kind="sign")
    f.box((sx, 2.95, 1.3), (3.4, 0.04, 2.2), M["glass_shop"])
    for zz in (0.2, 2.4):
        f.box((sx, 2.92, zz), (3.4, 0.04, 0.06), M["iron"])
    for k in range(4):                                                                            # bench slats
        f.box((sx, 2.5 + k * 0.08, 0.48), (2.8, 0.06, 0.03), M["slat"])
    for xx in (sx - 1.2, sx + 1.2):
        f.box((xx, 2.6, 0.24), (0.05, 0.3, 0.48), M["iron"])
    f.box((sx + 1.75, 2.22, 1.3), (0.06, 1.2, 1.7), M["iron"], bevel=0.01)                    # the ad panel
    f.box((sx + 1.71, 2.22, 1.3), (0.02, 1.08, 1.56), M["lamp_glass_lit"])
    ad = K.L(f.g, (sx + 1.69, 2.22, 1.3), -90)
    ad.text("HOTEL", (0, 0, 0.45), 0.14, M["ink"], kind="serif")
    ad.text("ARCADIA", (0, 0, 0.25), 0.2, M["ink"], kind="serif")
    ad.box((0, 0, -0.2), (0.6, 0.004, 0.5), M["paper_dark"])
    ad.text("ROOMS  BY  THE  NIGHT", (0, -0.004, -0.6), 0.06, M["ink"], kind="sign")
    f.box((sx - 1.6, 2.86, 1.5), (0.4, 0.03, 0.6), M["white_band"])                             # the timetable
    for k in range(6):
        f.box((sx - 1.6, 2.84, 1.7 - k * 0.07), (0.32, 0.005, 0.02), M["ink"])


def cafe_terrace(f, M):
    for k in range(3):
        tx = 11.6 + k * 3.3
        bistro(f, tx, 3.6, M)
        bistro_chair(f, tx - 0.6, 3.9, 90, M)
        bistro_chair(f, tx + 0.6, 3.9, -90, M)
    ab = f.sub((20.2, 3.2, 0), 10)                                                               # the chalkboard
    for s in (-1, 1):
        ab.box((0, s * 0.2, 0.5), (0.6, 0.03, 1.0), M["slat"], rot=(s * 12, 0, 0))
    ab.box((0, -0.235, 0.52), (0.52, 0.01, 0.8), M["ink"], rot=(-12, 0, 0))
    ab.text("SOUP", (0, -0.25, 0.75), 0.1, M["white_paint"], kind="marker")
    ab.text("of the day", (0, -0.25, 0.6), 0.07, M["white_paint"], kind="marker")
    for x in (9.6, 20.4):                                                                         # planters at the ends
        f.box((x, 4.4, 0.3), (0.5, 1.6, 0.6), M["slat_dark"], bevel=0.02)
        for k in range(4):
            f.lathe((x, 3.8 + k * 0.4, 0.55), [(0.0, 0.0), (0.22, 0.1), (0.24, 0.25), (0.15, 0.42), (0.0, 0.5)], M["leaf"], n=8)


def build_s1(coll, M):
    g = S.Group("StreetA")
    f = K.L(g)
    ground(g, -14.0, 46.0, M)
    bank_corner(g, M)
    fs = K.L(g, (0, SHOP_Y, 0), 0)
    # the shops: a shuttered stone front, Cafe Noir, the pharmacy, a shuttered row
    K.shop_building(g, 2.0, 9.0, M, 12.0, M["ashlar"], "TRAVEL  ·  TICKETS", "shop", face=SHOP_Y)
    K.roller_shutter(fs, 2.5, 6.9, 0.6, 3.05, M, graffiti=[("ZED", 0.5, 6, -0.4, 0.1), ("was here", 0.16, -4, 1.1, -0.5)])
    K.shop_building(g, 9.0, 21.0, M, 10.0, M["stucco"], "CAFE   NOIR", "cafe", floors_m=M["frame_dark"], face=SHOP_Y)
    K.awning(fs, 9.4, 20.6, 3.3, 2.7, M, "CAFE  NOIR  ·  ESPRESSO  ·  OPEN  LATE")
    fs.text("Espresso  ·  Croissants  ·  Wine", (14.0, 0.03, 2.3), 0.13, M["white_paint"], kind="body")
    fs.box((19.6, -0.1, 1.5), (0.4, 0.06, 0.6), M["ink"], bevel=0.01)                              # menu case
    fs.box((19.6, -0.135, 1.5), (0.34, 0.01, 0.52), M["paper"])
    cafe_terrace(f, M)
    K.shop_building(g, 21.0, 30.0, M, 14.0, M["ashlar_dark"], "PHARMACY", "shop", face=SHOP_Y)
    fs.box((29.4, -0.6, 4.6), (0.08, 0.9, 0.08), M["iron"])                                         # the lit cross
    for (sx, sz) in ((0.18, 0.55), (0.55, 0.18)):
        fs.box((29.4, -1.0, 4.4), (0.06, sx, sz), M["lamp_glass_lit"])
    K.fire_escape(fs, 21.8, 25.8, [4.7, 7.6, 10.5], M)
    K.shop_building(g, 30.0, 46.0, M, 9.0, M["brick"], "BAKERY  ·  SINCE  1931", "shop", face=SHOP_Y)
    K.awning(fs, 30.4, 37.0, 3.3, 1.6, M, "FRESH  BREAD", striped=True)
    K.roller_shutter(fs, 38.0, 43.9, 0.6, 2.2, M, graffiti=[("KRAK", 0.45, -5, 0.0, 0.1)])
    for (ax, az) in ((5.5, 6.0), (11.4, 6.0), (17.4, 8.9), (27.5, 9.6), (41.0, 6.0)):
        K.ac_unit(fs, ax, az, M)
    fp = K.L(g, (0, SHOP_Y - 0.165, 0), 0)
    posters = [(9.0, 1.4, [("CIRCUS  RIVOLI", 0.09, "ink"), ("TONIGHT", 0.07, "ink"), ("8 PM", 0.07, "ink")]),
               (21.0, 1.5, [("VOTE", 0.11, "ink"), ("CALLOWAY", 0.08, "ink"), ("FOR MAYOR", 0.06, "ink")]),
               (21.0, 2.45, [("LOST  DOG", 0.08, "ink"), ("REWARD", 0.07, "ink"), ("CALL  5-0144", 0.05, "ink")]),
               (30.0, 1.6, [("JAZZ", 0.12, "ink"), ("THE  BLUE  ROOM", 0.06, "ink"), ("FRIDAYS", 0.06, "ink")])]
    for (px, pz, lines) in posters:
        K.poster(fp, px, pz, 0.6, 0.85, M, lines, rot=rnd.uniform(-4, 4))
    pigeons(f, [(4.0, SHOP_Y - 0.3, 12.15), (4.4, SHOP_Y - 0.3, 12.15), (15.0, SHOP_Y - 0.3, 10.15), (24.0, SHOP_Y - 0.3, 14.15),
                (-9.0, SHOP_Y - 1.2, 8.5)], M)
    # the bus shelter and the newspaper box
    bus_shelter(f, 25.5, M)
    K.newspaper_box(f, 22.5, 2.0, 0, M, "THE MORNING STAR", "STORM BATTERS CITY")
    # lamp posts, bollards, a bench, bins, bicycles at the kerb, a hydrant, a bike rack
    for x in (-1.0, 12.0, 24.0, 38.0):
        street_lamp(f, x, M)
    for x in (4.0, 6.0, 8.0):
        K.bollard(f, x, KERB + 0.25, M)
    K.bicycle(f, 12.0, -1.5, 0, M, lean=0)
    K.hydrant(f, 33.0, -1.8, M)
    K.litter_bin(f, 8.6, 5.6, M)
    K.litter_bin(f, 29.6, 5.4, M)
    K.bench(f, 35.0, 5.5, 0, M)
    K.bike_rack(f, 39.5, 41.0, 5.5, M)
    K.bicycle(f, 39.55, 5.15, 90, M, lean=0)
    K.tree(f, 44.0, 4.8, M, h=6.5)
    return g.finish(coll)


# ---------------------------------------------------------------- S2: the market street

def stall(f, x, M, k):
    """A market stall: a trestle table, crates of fruit and veg with price cards, a striped awning on
    poles with a valance, a hanging scale."""
    y = 2.6
    f.box((x, y, 0.8), (2.6, 1.2, 0.04), M["planks"], bevel=0.01)
    for dx in (-1.1, 1.1):
        for s in (-1, 1):
            f.beam((x + dx - s * 0.2, y - 0.5, 0.0), (x + dx + s * 0.2, y + 0.5, 0.78), 0.05, 0.05, M["slat_dark"])
    f.box((x, y + 0.55, 0.4), (2.5, 0.02, 0.6), M["cloth_b"])                                       # the skirt at the back
    for i in range(4):
        cx = x - 0.95 + i * 0.63
        K.fruit_crate(f, (cx, y - 0.15, 0.82), M, fruit="round" if (i + k) % 3 else "long", n=(4, 3))
        f.box((cx, y - 0.37, 1.0), (0.18, 0.004, 0.11), M["white_band"], rot=(-20, 0, 0))
        f.text(("1.20", "0.90", "2 / KG", "0.60")[(i + k) % 4], (cx, y - 0.375, 1.0), 0.05, M["ink"], kind="marker")
        if i % 2 == 0:
            K.fruit_crate(f, (cx, y + 0.3, 1.02), M, fruit="round", n=(3, 2))
    for k2 in range(3):                                                                            # crates stacked below
        f.box((x - 0.8 + k2 * 0.8, y + 0.15, 0.13), (0.6, 0.4, 0.24), M["slat"], bevel=0.01)
    for dx in (-1.35, 1.35):
        f.cyl((x + dx, y - 0.9, 1.25), 0.03, 2.5, M["iron"], n=6)
        f.cyl((x + dx, y + 0.7, 1.35), 0.03, 2.7, M["iron"], n=6)
    for s in range(7):
        mm = M["cloth_a"] if (s + k) % 2 == 0 else M["cloth_b"]
        f.box((x - 1.4 + s * 0.4 + 0.2, y - 0.1, 2.55), (0.4, 1.9, 0.03), mm, rot=(-14, 0, 0))
        f.lathe((x - 1.4 + s * 0.4 + 0.2, y - 1.04, 2.2), [(0.0, 0.0), (0.19, 0.02), (0.2, 0.14), (0.0, 0.14)], mm, n=8, axis="y")
    f.rod((x + 0.9, y - 0.75, 2.3), (x + 0.9, y - 0.75, 1.75), 0.006, M["iron"], 3)               # a hanging scale
    f.lathe((x + 0.9, y - 0.75, 1.55), [(0.0, 0.0), (0.09, 0.0), (0.09, 0.2), (0.0, 0.2)], M["white_band"], n=12)
    f.lathe((x + 0.9, y - 0.75, 1.48), [(0.0, 0.0), (0.16, 0.06), (0.0, 0.07)], M["chrome"], n=12)


def scaffold(f, x0, x1, M):
    """Scaffold over a shopfront under repair: standards on base plates, ledgers with couplers,
    boards and toe boards at three lifts, guard rails, cross bracing, a ladder, the mixer, sacks."""
    ys = (SHOP_Y - 0.4, SHOP_Y - 1.6)
    xs = [x0 + i * 2.0 for i in range(int((x1 - x0) / 2.0) + 1)]
    for x in xs:
        for y in ys:
            f.cyl((x, y, 3.6), 0.035, 7.2, M["iron"], n=6)
            f.box((x, y, 0.01), (0.15, 0.15, 0.02), M["iron_mid"])
            f.box((x - 0.05, y + 0.1, 0.05), (0.2, 0.2, 0.1), M["planks"], rot=(0, 0, 10))       # sole board
            for z in (2.4, 4.8, 7.0):
                f.box((x, y, z), (0.09, 0.09, 0.1), M["iron_mid"])                                # couplers
    for z in (2.4, 4.8, 7.0):
        for y in ys:
            f.rod((x0, y, z), (x1, y, z), 0.03, M["iron"], 6)
            f.rod((x0, y, z + 1.0), (x1, y, z + 1.0), 0.025, M["iron"], 6)
        for k in range(5):
            f.box(((x0 + x1) / 2, SHOP_Y - 0.5 - k * 0.24, z + 0.07), (x1 - x0, 0.22, 0.04), M["planks"], bevel=0.005)
        f.box(((x0 + x1) / 2, SHOP_Y - 1.65, z + 0.17), (x1 - x0, 0.03, 0.15), M["planks_w"])
    for i in range(len(xs) - 1):
        f.rod((xs[i], ys[1], 0.1), (xs[i + 1], ys[1], 2.4), 0.03, M["iron"], 6)
        f.rod((xs[i + 1], ys[1], 2.4), (xs[i], ys[1], 4.8), 0.03, M["iron"], 6)
    f.box(((x0 + x1) / 2, ys[1] - 0.04, 5.9), (x1 - x0, 0.01, 2.2), M["chainlink"])                 # netting on the top lifts
    lx = x0 + 0.6
    for side in (-0.22, 0.22):
        f.box((lx + side, SHOP_Y - 1.9, 1.3), (0.05, 0.05, 2.8), M["slat_dark"], rot=(-8, 0, 0))
    for r in range(8):
        f.box((lx, SHOP_Y - 1.9 + r * 0.04, 0.2 + r * 0.33), (0.44, 0.04, 0.04), M["slat_dark"])
    fb = K.L(f.g, (0, SHOP_Y - 1.62, 0), 0)                                                       # the builder's banner
    fb.box(((x0 + x1) / 2, 0.0, 3.3), (4.2, 0.01, 0.8), M["white_band"])
    fb.text("HOLT  &  SONS  ·  BUILDERS", ((x0 + x1) / 2, -0.008, 3.42), 0.2, M["ink"], kind="sign")
    fb.text("RESTORATION  ·  ROOFING", ((x0 + x1) / 2, -0.008, 3.15), 0.13, M["ink"], kind="sign")
    mx = x1 - 2.0                                                                                   # the cement mixer
    f.cyl((mx, 3.4, 0.95), 0.5, 0.75, M["hydrant"], rot=(0, 55, 0), n=16, r2=0.32)
    f.torus((mx, 3.4, 0.95), 0.48, 0.03, M["iron"], rot=(0, 55, 0), n1=16, n2=4)
    f.box((mx, 3.4, 0.4), (1.0, 0.5, 0.1), M["iron"])
    f.box((mx - 0.35, 3.4, 0.55), (0.3, 0.4, 0.3), M["iron_mid"], bevel=0.02)
    for s in (-1, 1):
        f.cyl((mx, 3.4 + s * 0.3, 0.2), 0.2, 0.08, M["tyre"], axis="y", n=12)
    for i in range(4):
        f.box((mx - 1.4 + (i % 2) * 0.05, 3.6, 0.12 + i * 0.2), (0.7, 0.45, 0.18), M["sack"], bevel=0.05)
    f.box((mx - 2.6, 3.7, 0.35), (0.6, 0.9, 0.3), M["iron_mid"], rot=(0, 0, 20), bevel=0.04)      # a wheelbarrow
    f.cyl((mx - 2.75, 3.3, 0.18), 0.17, 0.06, M["tyre"], axis="x", n=10, rot=(0, 0, 20))
    for k in range(5):
        f.box((x0 + 2.5 + k * 0.1, 4.6, 0.02 + k * 0.04), (2.2, 0.22, 0.04), M["planks"], rot=(0, 0, 3 * k))


def market_lights(f, xs, z, y, M):
    """A string of bulbs sagging between poles over the stalls."""
    for a, b in zip(xs, xs[1:]):
        n = 10
        pts = [(a + (b - a) * t / n, y, z - math.sin(t / n * math.pi) * 0.35) for t in range(n + 1)]
        for p, q in zip(pts, pts[1:]):
            f.rod(p, q, 0.006, M["iron"], 3)
        for p in pts[1:-1:2]:
            f.lathe((p[0], p[1], p[2] - 0.14), [(0.0, 0.0), (0.035, 0.02), (0.04, 0.06), (0.015, 0.1), (0.0, 0.12)], M["bulb"], n=6)


def railway(f, M):
    """The elevated line: a deck on riveted plate girders with cross beams, steel pillars behind
    with knee braces, the last pier at the kerb (where the Driver's car ends), strip lamps in
    housings, cable trays, a chain-link fence with barbed wire, the dumpsters, a fire escape."""
    x0, x1 = RAIL_X0, RAIL_X1 + 1.5
    f.box(((x0 + x1) / 2, 1.0, DECK_Z + 0.75), (x1 - x0, 14.0, 0.5), M["concrete_dark"])
    for y in (-5.0, 7.0, 1.0):
        f.box(((x0 + x1) / 2, y, DECK_Z - 0.2 + 0.3), (x1 - x0, 0.03, 1.3), M["steel"])                 # webs
        for z in (DECK_Z - 0.53, DECK_Z + 0.48):
            f.box(((x0 + x1) / 2, y, z), (x1 - x0, 0.4, 0.04), M["steel"])                              # flanges
        x = x0 + 0.3
        while x < x1:
            for s in (-1, 1):
                f.box((x, y + s * 0.06, DECK_Z - 0.05), (0.08, 0.1, 0.95), M["steel"])                  # stiffeners
            x += 1.5
        x = x0 + 0.1
        while x < x1:
            for s in (-1, 1):
                f.cyl((x, y + s * 0.2, DECK_Z - 0.5), 0.018, 0.02, M["steel_mid"], n=6)                 # rivets
            x += 0.3
    k = 0
    x = x0 + 0.6
    while x < x1:
        f.box((x, 1.0, DECK_Z + 0.42), (0.16, 12.0, 0.08), M["steel"])
        f.box((x, 1.0, DECK_Z + 0.32), (0.02, 12.0, 0.2), M["steel"])
        if k % 3 == 0:
            f.beam((x, -5.0, DECK_Z - 0.5), (x + 1.2, 1.0, DECK_Z + 0.3), 0.06, 0.06, M["steel"])
            f.beam((x, 7.0, DECK_Z - 0.5), (x + 1.2, 1.0, DECK_Z + 0.3), 0.06, 0.06, M["steel"])
        x += 1.2
        k += 1
    for y in (2.6, 3.0):                                                                               # cable trays
        f.box(((x0 + x1) / 2, y, DECK_Z - 0.15), (x1 - x0, 0.3, 0.04), M["iron_mid"])
        f.cyl(((x0 + x1) / 2, y, DECK_Z - 0.1), 0.03, x1 - x0, M["tyre"], axis="x", n=6)
    # the pillars behind: built-up steel columns on base plates, knee braces up to the girder
    for x in (x0 + 1.0, x0 + 10.0, x0 + 19.0, RAIL_X1 - 1.0):
        f.box((x, 7.0, DECK_Z / 2), (0.5, 0.5, DECK_Z), M["steel"], bevel=0.02)
        for s in (-1, 1):
            f.box((x + s * 0.27, 7.0, DECK_Z / 2), (0.04, 0.7, DECK_Z), M["steel"])
        z = 0.4
        while z < DECK_Z - 0.3:
            for s in (-1, 1):
                f.cyl((x + s * 0.3, 6.75, z), 0.02, 0.02, M["steel_mid"], axis="y", n=6)
            z += 0.35
        f.box((x, 7.0, 0.04), (1.1, 1.1, 0.08), M["steel_mid"], bevel=0.01)
        f.box((x, 7.0, 0.15), (0.9, 0.9, 0.3), M["concrete_tex"], bevel=0.03)
        for s in (-1, 1):
            f.beam((x + s * 0.25, 7.0, DECK_Z - 1.6), (x + s * 1.4, 7.0, DECK_Z - 0.55), 0.12, 0.12, M["steel"])
    # the last pier, at the kerb by the crossroads (StreetPlan.CrashX: the car ends against it)
    px = CRASH_X - 0.55
    f.box((px, -1.75, DECK_Z / 2), (0.9, 0.8, DECK_Z), M["concrete_w"], bevel=0.04)
    for s in (-1, 1):
        f.box((px + s * 0.47, -1.75, DECK_Z / 2), (0.04, 0.84, DECK_Z), M["steel"])
    f.box((px, -1.75, 0.4), (1.05, 0.95, 0.8), M["concrete_w"], bevel=0.04)
    f.box((px, -1.75, DECK_Z - 0.2), (1.1, 1.0, 0.4), M["steel"], bevel=0.02)
    for k2 in range(5):
        f.box((px - 0.47, -1.75 - 0.3 + k2 * 0.15, 0.9), (0.02, 0.08, 0.6), M["white_band"] if k2 % 2 else M["ink"], rot=(35, 0, 0))
    # strip lamps under the deck in their housings
    for x in (x0 + 5.0, x0 + 14.0, x0 + 23.0):
        f.box((x, 1.0, DECK_Z - 0.17), (2.4, 0.3, 0.12), M["iron_mid"], bevel=0.01)
        f.box((x, 1.0, DECK_Z - 0.25), (2.2, 0.12, 0.08), M["strip"])
        f.rod((x, 1.0, DECK_Z - 0.1), (x, 1.0, DECK_Z + 0.4), 0.01, M["iron"], 4)
    # parapets and the rails on top (seen in the wide shots)
    for y in (-5.8, 7.8):
        f.box(((x0 + x1) / 2, y, DECK_Z + 1.4), (x1 - x0, 0.2, 0.8), M["concrete_tex"])
        x = x0
        while x < x1:
            f.box((x, y, DECK_Z + 1.4), (0.03, 0.22, 0.8), M["reveal"])
            x += 2.0
    for y in (0.25, 1.75):
        f.box(((x0 + x1) / 2, y, DECK_Z + 1.12), (x1 - x0, 0.08, 0.14), M["steel_mid"])
    # the fence: posts, a crosshatch of wire, barbed wire on arms
    fx0, fx1 = x0 + 0.5, RAIL_X1 - 0.5
    x = fx0
    while x <= fx1:
        f.cyl((x, 4.4, 1.3), 0.03, 2.6, M["steel"], n=6)
        f.beam((x, 4.4, 2.5), (x, 4.0, 2.85), 0.03, 0.03, M["steel"])
        x += 2.5
    for z in (0.05, 2.45):
        f.rod((fx0, 4.4, z), (fx1, 4.4, z), 0.02, M["steel"], 5)
    x = fx0
    while x < fx1:
        f.beam((x, 4.4, 0.05), (min(x + 2.4, fx1), 4.4, 2.45), 0.008, 0.008, M["chainlink"])
        f.beam((min(x + 2.4, fx1), 4.4, 0.05), (x, 4.4, 2.45), 0.008, 0.008, M["chainlink"])
        x += 0.12
    for z, yy in ((2.65, 4.25), (2.8, 4.12)):
        f.rod((fx0, yy, z), (fx1, yy, z), 0.008, M["iron"], 3)
    f.box(((fx0 + fx1) / 2, 4.36, 1.4), (1.6, 0.02, 0.5), M["white_band"])
    f.text("PRIVATE  ·  KEEP OUT", ((fx0 + fx1) / 2, 4.345, 1.4), 0.12, M["ink"], kind="sign")
    # the dumpsters, the fire escape on the arches
    K.bin_dumpster(f, x0 + 4.0, 3.2, M, M["paint_green"])
    K.bin_dumpster(f, x0 + 16.5, 3.2, M, M["iron_mid"])
    for k3 in range(5):
        f.box((x0 + 5.5 + k3 * 0.25, 3.4, 0.15 + (k3 % 2) * 0.1), (0.3, 0.3, 0.3), M["sack"], rot=(0, 10 * k3, 20 * k3), bevel=0.06)


def arches(g, M):
    """The railway arches behind the lane: brick bays with arched heads, each closed by a shutter,
    timber doors or a lit workshop; graffiti and posters on the piers."""
    f = K.L(g, (0, SHOP_Y, 0), 0)
    x0, x1, h = 63.0, 92.0, 6.0
    bays = [(65.5, "shutter"), (70.5, "doors"), (75.5, "shop"), (80.5, "shutter"), (85.5, "doors"), (90.0, "shutter")]
    K.wall(f, x0, x1, 0.0, h, 0.6, M["brick"], [(c, 3.4, 0.0, 2.6, True) for c, _ in bays], v=0.3)
    for c, kind in bays:
        R = 1.7
        for k in range(11):                                                                         # the brick arch ring
            a0, a1 = math.pi * k / 11, math.pi * (k + 1) / 11
            am = (a0 + a1) / 2
            f.box((c + math.cos(am) * (R + 0.15), -0.04, 2.6 + math.sin(am) * (R + 0.15)), (0.3, 0.08, (a1 - a0) * (R + 0.15) * 0.95), M["brick_dark"], rot=(0, -math.degrees(am) + 90, 0))
        if kind == "shutter":
            K.roller_shutter(f.sub((0, 0.4, 0), 0), c - 1.7, c + 1.7, 0.0, 2.6, M, graffiti=[("RAIL", 0.4, 7, -0.4, 0.2)] if c < 70 else None)
            f.wallshape([(math.cos(math.pi * k / 12) * R, math.sin(math.pi * k / 12) * R) for k in range(13)], 0.3, 0.34, M["iron_mid"], c=(c, 0, 2.6))
        elif kind == "doors":
            for s in (-1, 1):
                f.box((c + s * 0.85, 0.35, 1.3), (1.66, 0.08, 2.6), M["planks_w"], bevel=0.01)
                f.beam((c + s * 0.1, 0.3, 0.2), (c + s * 1.6, 0.3, 2.4), 0.12, 0.04, M["planks_w"])
            f.box((c, 0.3, 1.2), (0.12, 0.04, 0.3), M["iron"])
            pts = [(math.cos(math.pi * k / 12) * R, math.sin(math.pi * k / 12) * R) for k in range(13)]
            f.wallshape(pts, 0.34, 0.38, M["glass_ext"], c=(c, 0, 2.6))
            for k in range(1, 6):
                a = math.pi * k / 6
                f.beam((c, 0.33, 2.6), (c + math.cos(a) * R, 0.33, 2.6 + math.sin(a) * R), 0.04, 0.03, M["frame_dark"])
            f.text(("CARS  ·  REPAIRS", "STORAGE  12")[0 if c < 80 else 1], (c, 0.3, 1.9), 0.18, M["white_paint"], kind="sign")
        else:
            f.box((c, 0.45, 1.6), (3.4, 0.05, 3.2), M["lamp_glass_lit"])
            for xx in (c - 1.1, c, c + 1.1):
                f.box((xx, 0.4, 1.6), (0.06, 0.06, 3.2), M["frame_dark"])
            for zz in (0.9, 1.9):
                f.box((c, 0.4, zz), (3.4, 0.06, 0.05), M["frame_dark"])
            f.box((c, 0.42, 1.4), (1.2, 0.03, 0.4), M["ink"])
            f.text("WORKSHOP", (c, 0.4, 1.4), 0.2, M["white_paint"], kind="sign")
    pigeons(f, [(66.0, 0.0, DECK_Z - 0.48), (66.3, 0.0, DECK_Z - 0.48), (78.0, 0.0, DECK_Z - 0.48)], M)
    fp = K.L(g, (0, SHOP_Y - 0.004, 0), 0)
    for (px, pz, lines) in ((68.0, 1.6, [("BOXING", 0.12, "ink"), ("SAT  NIGHT", 0.07, "ink"), ("THE  ARMOURY", 0.06, "ink")]),
                            (73.0, 1.4, [("WANTED", 0.11, "ink"), ("DRIVERS", 0.08, "ink"), ("NO  QUESTIONS", 0.05, "ink")]),
                            (83.0, 1.7, [("CIRCUS  RIVOLI", 0.09, "ink"), ("LAST  WEEK", 0.07, "ink"), ("8 PM", 0.07, "ink")])):
        K.poster(fp, px, pz, 0.6, 0.85, M, lines, rot=rnd.uniform(-5, 5))
    fp.text("SNAKE", (78.0, -0.01, 1.4), 0.5, M["graffiti"], kind="marker", rot=5)
    fp.text("no way home", (88.0, -0.01, 3.3), 0.22, M["graffiti"], kind="marker", rot=-3)
    K.drainpipe(f, 63.4, 0.0, h, M["iron"])


def fish_display(f, x0, x1, M):
    """The fishmonger's slab under its awning: crushed ice sloping to the street, fish laid on it."""
    f.box(((x0 + x1) / 2, 5.6, 0.45), (x1 - x0, 0.9, 0.9), M["white_band"], bevel=0.02)
    f.box(((x0 + x1) / 2, 5.55, 0.95), (x1 - x0 - 0.1, 0.9, 0.08), M["ice"], rot=(-14, 0, 0))
    for k in range(14):
        x = x0 + 0.4 + (k % 7) * (x1 - x0 - 0.8) / 6
        y = 5.4 + (k // 7) * 0.35
        f.lathe((x, y, 1.0 + (k // 7) * 0.08), [(0.0, -0.22), (0.05, -0.12), (0.06, 0.05), (0.03, 0.18), (0.0, 0.2)], M["fish"], n=6, axis="x", rot=(0, 0, 80 + rnd.uniform(-15, 15)))
    for k in range(3):
        f.box((x0 + 1.0 + k * 1.6, 5.15, 1.0), (0.2, 0.004, 0.12), M["white_band"], rot=(-20, 0, 0))
        f.text(("COD", "SOLE", "EELS")[k], (x0 + 1.0 + k * 1.6, 5.145, 1.0), 0.05, M["ink"], kind="marker")


def build_s2(coll, M):
    g = S.Group("StreetB")
    f = K.L(g)
    ground(g, 46.0, 92.0, M)
    fs = K.L(g, (0, SHOP_Y, 0), 0)
    K.shop_building(g, 46.0, 52.0, M, 11.0, M["stucco"], "FISH", "shop", face=SHOP_Y)
    K.awning(fs, 46.3, 51.7, 3.3, 1.5, M, "FRESH  FISH  DAILY")
    fish_display(f, 46.6, 50.2, M)
    K.shop_building(g, 52.0, 60.0, M, 12.0, M["ashlar"], "", "shop", face=SHOP_Y)
    fs.box((56.0, -0.12, 1.85), (5.6, 0.04, 2.4), M["planks_w"])                                    # boarded up for the work
    fs.text("UNDER  REPAIR", (56.0, -0.15, 2.4), 0.24, M["ink"], kind="sign")
    K.shop_building(g, 60.0, 63.0, M, 10.0, M["brick"], "KEYS", "shop", face=SHOP_Y)
    # the market (its ground is StreetA's) and the lights strung over it
    for i, x in enumerate((33.0, 37.0, 41.0, 44.5)):
        stall(f, x, M, i)
    market_lights(f, [31.6, 35.0, 39.0, 43.0, 46.0], 3.1, 3.5, M)
    for x in (31.6, 46.0):
        f.cyl((x, 3.5, 1.6), 0.04, 3.2, M["iron"], n=6)
    scaffold(f, 52.0, 60.0, M)
    for x, word in ((34.0, "FRUIT  &  VEG"), (47.5, "OPEN")):                                       # hanging signs
        f.beam((x, 4.4, 3.6), (x, 4.4, 4.2), 0.03, 0.03, M["steel"])
        f.box((x, 4.4, 3.3), (1.4, 0.05, 0.55), M["stone_dark"], bevel=0.02)
        f.box((x, 4.37, 3.3), (1.3, 0.01, 0.45), M["ink"])
        f.text(word, (x, 4.36, 3.3), 0.2, M["white_paint"], kind="sign")
    railway(f, M)
    arches(g, M)
    street_lamp(f, 50.0, M)
    K.litter_bin(f, 51.2, 5.4, M)
    for (ax, az) in ((47.5, 6.0), (61.4, 6.0)):
        K.ac_unit(fs, ax, az, M)
    pigeons(f, [(47.0, SHOP_Y - 0.3, 11.15), (48.0, SHOP_Y - 0.3, 11.15), (55.0, SHOP_Y - 1.0, 7.1)], M)
    return g.finish(coll)


# ---------------------------------------------------------------- S3/S4: the crossroads

def hotel(g, M):
    f = K.L(g, (0, SHOP_Y, 0), 0)
    x0, x1, h = 92.0, 104.0, 13.0
    ups = [(x0 + 1.2 + c * 2.4, 1.2, 4.8 + r * 2.7, 6.5 + r * 2.7, r == 2) for r in range(3) for c in range(5)]
    K.wall(f, x0, x1, 0.0, h, 0.4, M["ashlar"], [(98.0, 4.0, 0.0, 3.4, False), (94.0, 2.0, 0.6, 3.2, False), (102.4, 2.4, 0.6, 3.2, False)] + ups, v=0.2)
    for c, w in ((94.0, 2.0), (102.4, 2.4)):
        f.box((c, 0.3, 1.9), (w, 0.2, 2.6), M["reveal"])
        f.box((c, 0.08, 1.9), (w - 0.06, 0.03, 2.6), M["glass_shop"])
        f.box((c, 0.06, 1.2), (w - 0.2, 0.01, 0.9), M["blind"])
    for (c, w0, w1, wd, arch) in ups:
        K.window(f, c, w0, w1, wd, M, M["frame_pale"], M["glass_ext"], M["trim"], head="arch" if arch else "pediment", cols=2, rows=3, blind=rnd.choice((None, 0.5)))
    for z in (4.1, 6.9):
        K.string_course(f, x0, x1, z, M["trim"], h=0.16)
    K.cornice(f, x0, x1, h - 0.6, M["trim"], depth=0.55, modillions=True)
    # the entrance: a revolving door in a bronze drum, side doors, the canopy on its chains
    f.box((98.0, 0.3, 1.7), (4.0, 0.2, 3.4), M["reveal"])
    f.lathe((98.0, -0.3, 0.0), [(0.0, 0.0), (1.15, 0.0), (1.15, 0.05), (0.0, 0.05)], M["brass"], n=24)
    f.lathe((98.0, -0.3, 2.6), [(0.0, 0.0), (1.2, 0.0), (1.2, 0.3), (0.0, 0.3)], M["brass"], n=24)
    for k in range(16):                                                                             # the drum's glazing bars
        a = k / 16 * math.tau
        if math.sin(a) < -0.5 or abs(math.cos(a)) < 0.2:
            continue
        f.box((98.0 + math.cos(a) * 1.15, -0.3 + math.sin(a) * 1.15, 1.3), (0.05, 0.05, 2.6), M["brass"])
    for k in range(4):                                                                              # the wings
        a = k / 4 * math.tau + 0.4
        f.box((98.0 + math.cos(a) * 0.55, -0.3 + math.sin(a) * 0.55, 1.3), (1.08, 0.05, 2.5), M["glass_ext"], rot=(0, 0, math.degrees(a)))
        f.box((98.0 + math.cos(a) * 0.55, -0.3 + math.sin(a) * 0.55, 1.0), (1.08, 0.06, 0.05), M["brass"], rot=(0, 0, math.degrees(a)))
    f.cyl((98.0, -0.3, 1.3), 0.05, 2.6, M["brass"], n=10)
    f.box((98.0, -1.2, 3.55), (5.4, 2.6, 0.28), M["sf_paint"], bevel=0.04)                         # the canopy
    f.box((98.0, -2.5, 3.55), (5.4, 0.04, 0.24), M["brass"])
    f.text("HOTEL   ARCADIA", (98.0, -2.53, 3.55), 0.17, M["sf_paint"], kind="serif")
    for s in (-1, 1):
        f.rod((98.0 + s * 2.5, -2.3, 3.6), (98.0 + s * 2.5, -0.05, 5.8), 0.02, M["iron"], 5)
    for k in range(8):
        f.box((96.0 + k * 0.6, -1.2, 3.39), (0.2, 0.2, 0.04), M["lamp_glass_lit"])
    for s in (-1, 1):                                                                               # potted bays by the door
        K.potted_plant(f, 98.0 + s * 2.2, -0.45, M, h=1.1, kind="ball")
    # the vertical sign and the flags
    f.box((x1 - 0.5, -0.6, 8.0), (0.18, 1.0, 4.6), M["sf_paint"], bevel=0.03)
    for k, ch in enumerate("HOTEL"):
        for s in (-1, 1):
            f.sub((x1 - 0.5 + s * 0.095, -0.6, 9.7 - k * 0.85), 90 * s).text(ch, (0, 0, 0), 0.6, M["lamp_glass_lit"], kind="serif")
    for xx in (94.0, 96.6):
        f.rod((xx, 0.0, 7.4), (xx, -1.6, 8.6), 0.025, M["iron"], 5)
        f.box((xx, -1.3, 7.6), (0.02, 0.9, 1.2), M["cloth_a"], rot=(37, 0, 0))
    f.box(((x0 + x1) / 2, 0.1, h + 0.3), (x1 - x0, 0.3, 0.6), M["ashlar"])
    f.text("ARCADIA", ((x0 + x1) / 2, -0.06, h + 0.3), 0.45, M["trim"], depth=0.03, kind="serif")


def side_street(g, M):
    """The side street going back north between the buildings (104-112): its tarmac, kerbs and
    pavements, brick and stone flanks with windows and a fire escape, a lamp, a dumpster, a line of
    washing across it high up."""
    f = K.L(g)
    f.box((108.0, 30.0, -0.05), (8.0, 50.0, 0.1), M["road"])
    for xx in (104.6, 111.4):
        f.box((xx, 30.0, 0.0), (1.2, 50.0, 0.1), M["paving"])
        f.box((xx + (0.6 if xx < 108 else -0.6), 30.0, 0.0), (0.2, 50.0, 0.14), M["kerb"])
    for y in range(10, 54, 6):
        f.box((108.0, y, 0.002), (0.12, 2.5, 0.006), M["white_paint"])
    K.wall(K.L(g, (103.8, 0, 0), 90), SHOP_Y, 54.0, 0.0, 12.0, 0.4, M["brick_w"],
           [(y, 1.0, z, z + 1.5, False) for y in (9.0, 12.0, 15.0, 18.0, 24.0, 30.0) for z in (4.6, 7.6)])
    K.wall(K.L(g, (112.2, 0, 0), 90), SHOP_Y, 54.0, 0.0, 14.0, 0.4, M["concrete_w"],
           [(y, 1.0, z, z + 1.5, False) for y in (10.0, 14.0, 20.0, 26.0) for z in (4.6, 7.6, 10.6)])
    fw2 = K.L(g, (104.0, 0, 0), 90)                                                                 # u = y, faces +x
    for y in (9.0, 12.0, 15.0, 18.0, 24.0, 30.0):
        for z in (4.6, 7.6):
            K.window(fw2, y, z, z + 1.5, 1.0, M, M["frame_dark"], M["glass_ext"], M["trim_w"], cols=2, rows=2, blind=rnd.choice((None, 0.6)))
    fe2 = K.L(g, (112.0, 0, 0), -90)                                                                # u = -y, faces -x
    for y in (10.0, 14.0, 20.0, 26.0):
        for z in (4.6, 7.6, 10.6):
            K.window(fe2, -y, z, z + 1.5, 1.0, M, M["frame_pale"], M["glass_ext"], M["trim_w"], cols=2, rows=2, blind=rnd.choice((None, 0.5)))
    K.fire_escape(fe2, -21.0, -16.0, [4.4, 7.4, 10.4], M)
    K.bin_dumpster(f, 105.2, 20.0, M, M["iron_mid"])
    for k in range(9):                                                                              # washing line high across
        x = 104.4 + k * 0.85
        z = 9.0 - math.sin(k / 8 * math.pi) * 0.5
        f.rod((x, 18.0, z), (x + 0.85, 18.0, 9.0 - math.sin((k + 1) / 8 * math.pi) * 0.5), 0.006, M["iron"], 3)
        if 0 < k < 8:
            f.box((x + 0.4, 18.0, z - 0.3), (0.45, 0.01, 0.5), M["cloth_a"] if k % 2 else M["paper"], rot=(0, rnd.uniform(-4, 4), 0))
    f.lathe((110.6, 12.0, 0.0), [(0.0, 0.0), (0.2, 0.0), (0.2, 0.1), (0.07, 0.2), (0.06, 4.6), (0.0, 4.6)], M["iron"], n=8)
    f.beam((110.6, 12.0, 4.5), (109.6, 12.0, 4.7), 0.05, 0.05, M["iron"])
    f.lathe((109.5, 12.0, 4.3), [(0.0, 0.0), (0.18, 0.0), (0.22, 0.3), (0.0, 0.35)], M["lamp_glass"], n=8)


def crossroads(g, M):
    """The junction: the zebra into the side street, signals with hoods and button boxes, the phone
    box, the newsstand with its papers, the stalled bus across the far end."""
    f = K.L(g)
    for k in range(8):
        f.box((104.4 + k * 1.0, 2.0, 0.003), (0.55, 7.6, 0.006), M["white_paint"])
    for x in (98.0, 121.0):
        f.lathe((x, KERB + 0.3, 0.0), [(0.0, 0.0), (0.15, 0.0), (0.15, 0.1), (0.07, 0.16), (0.06, 3.4), (0.0, 3.4)], M["iron"], n=10)
        f.box((x, KERB + 0.3, 3.85), (0.34, 0.3, 0.95), M["steel"], bevel=0.03)
        for i, m in enumerate((M["signal"], M["steel_mid"], M["steel_mid"])):
            f.cyl((x, KERB + 0.13, 4.15 - i * 0.3), 0.1, 0.03, m, axis="y", n=12)
            f.box((x, KERB + 0.06, 4.24 - i * 0.3), (0.24, 0.14, 0.02), M["steel"], rot=(-20, 0, 0))
        f.box((x, KERB + 0.12, 1.15), (0.16, 0.12, 0.28), M["steel"], bevel=0.02)
        f.cyl((x, KERB + 0.055, 1.1), 0.03, 0.02, M["white_band"], axis="y", n=8)
        f.box((x, KERB + 0.06, 1.24), (0.12, 0.01, 0.06), M["lamp_glass_lit"])
        f.box((x, KERB + 0.3, 2.6), (0.26, 0.24, 0.5), M["steel"], bevel=0.02)                     # the walking man
        f.box((x, KERB + 0.17, 2.6), (0.18, 0.01, 0.38), M["steel_mid"])
    # the phone box: glazing bars, a lit TELEPHONE sign, the phone inside
    px = 114.5
    for sx in (-0.48, 0.48):
        for sy in (-0.48, 0.48):
            f.box((px + sx, 3.4 + sy, 1.2), (0.08, 0.08, 2.4), M["steel"])
    f.box((px, 3.88, 1.2), (0.9, 0.04, 2.3), M["steel"])
    for (cx, cy, sx_, sy_) in ((px, 2.92, 0.9, 0.02), (px - 0.48, 3.4, 0.02, 0.9), (px + 0.48, 3.4, 0.02, 0.9)):
        f.box((cx, cy, 1.25), (sx_, sy_, 1.9), M["glass_shop"])
        for k in range(1, 6):
            f.box((cx, cy - 0.01 if sy_ < 0.05 else cy, 0.3 + k * 0.32), (sx_ if sx_ > 0.05 else 0.03, sy_ if sy_ > 0.05 else 0.03, 0.025), M["steel"])
    f.box((px, 3.4, 2.5), (1.1, 1.1, 0.2), M["stone_dark"], bevel=0.04)
    f.lathe((px, 3.4, 2.6), [(0.0, 0.0), (0.55, 0.0), (0.3, 0.18), (0.0, 0.22)], M["steel"], n=4, rot=(0, 0, 45))
    f.box((px, 2.88, 2.25), (0.7, 0.03, 0.16), M["lamp_glass_lit"])
    f.text("TELEPHONE", (px, 2.86, 2.25), 0.1, M["ink"], kind="sign")
    f.box((px, 3.8, 1.5), (0.24, 0.12, 0.36), M["iron"], bevel=0.02)
    f.box((px - 0.05, 3.72, 1.55), (0.06, 0.06, 0.24), M["iron"], bevel=0.02)
    f.box((px, 3.82, 1.0), (0.3, 0.1, 0.06), M["paper"])
    # the newsstand: a kiosk with an awning, racks of papers, magazines pegged on a line, the sign
    nx = 118.5
    f.box((nx, 3.8, 1.2), (2.6, 1.4, 2.4), M["stone_dark"], bevel=0.03)
    f.box((nx, 3.1, 1.1), (2.4, 0.08, 0.06), M["iron"])
    K.awning(K.L(g, (0, 3.1, 0), 0), nx - 1.4, nx + 1.4, 2.6, 0.7, M, "", striped=True)
    f.box((nx, 3.8, 2.65), (2.8, 1.6, 0.12), M["iron"], bevel=0.02)
    f.box((nx, 3.0, 2.85), (1.6, 0.04, 0.3), M["white_band"])
    f.text("NEWS  ·  TOBACCO", (nx, 2.97, 2.85), 0.15, M["ink"], kind="sign")
    heads = ["STORM BATTERS CITY", "BANK HOLDS FIRM", "TRAINS DELAYED", "FLOODS IN THE NORTH", "WHO IS V ?", "RACING RESULTS"]
    for i in range(6):                                                                              # the papers in the rack
        x = nx - 1.0 + i * 0.4
        f.box((x, 2.98, 0.85), (0.34, 0.12, 0.02), M["iron"], rot=(-25, 0, 0))
        f.box((x, 2.95, 1.0), (0.3, 0.02, 0.36), M["paper"], rot=(-15, 0, 0))
        f.text(heads[i].split()[0], (x, 2.93, 1.1), 0.05, M["ink"], kind="sign")
        f.box((x, 2.93, 0.98), (0.24, 0.005, 0.1), M["paper_dark"], rot=(-15, 0, 0))
    f.rod((nx - 1.2, 3.02, 2.2), (nx + 1.2, 3.02, 2.2), 0.008, M["iron"], 3)
    for i in range(7):
        f.box((nx - 1.1 + i * 0.36, 3.02, 1.95), (0.26, 0.01, 0.34), M["paper"] if i % 2 else M["paper_dark"])
        f.box((nx - 1.1 + i * 0.36, 3.01, 2.05), (0.18, 0.005, 0.05), M["ink"] if i % 2 else M["white_band"])
    f.box((nx + 1.5, 3.6, 0.5), (0.3, 0.6, 1.0), M["slat"], bevel=0.02)                             # bundles on a crate
    for k in range(3):
        f.box((nx + 1.5, 3.6, 1.05 + k * 0.1), (0.3, 0.45, 0.09), M["paper"], rot=(0, 0, 8 * k))
    # the stalled bus across the far end: a two-door saloon bus, its windows, livery, wheels, sign
    bx = 131.5
    bs = K.L(g, (bx, 0.5, 0), 0)
    prof = [(-6.0, 0.35), (6.0, 0.35), (6.0, 2.7), (5.9, 3.05), (5.6, 3.15), (-5.6, 3.15), (-5.9, 3.05), (-6.0, 2.7)]
    bs.wallshape(prof, -1.3, 1.3, M["bus"])
    bs.box((0, -1.31, 1.05), (12.0, 0.02, 0.26), M["bus_band"])
    bs.box((0, -1.31, 3.0), (11.6, 0.02, 0.06), M["bus_band"])
    for i in range(7):
        bs.box((-5.2 + i * 1.6, -1.31, 2.15), (1.36, 0.02, 1.0), M["glass_ext"])
        bs.box((-5.2 + i * 1.6 + 0.72, -1.32, 2.15), (0.08, 0.02, 1.1), M["bus"])
    for dxx in (-3.9, 4.3):
        bs.box((dxx, -1.31, 1.45), (1.1, 0.02, 2.2), M["glass_ext"])
        bs.box((dxx, -1.325, 1.45), (0.04, 0.02, 2.2), M["bus_band"])
    for s in (-1, 1):
        bs.box((s * 6.02, 0.0, 0.55), (0.1, 2.5, 0.3), M["chrome"], bevel=0.04)
        bs.box((s * 6.01, 0.0, 2.0), (0.02, 2.2, 1.2), M["glass_ext"])
        bs.box((s * 6.02, 0.0, 2.85), (0.03, 1.6, 0.26), M["lamp_glass_lit"])
        bs.sub((s * 6.04, 0.0, 2.85), -90 * s).text("14   NORTH  ·  ALDER LANE" if s < 0 else "NOT IN SERVICE", (0, 0, 0), 0.13, M["ink"], kind="sign")
    for wx in (-4.2, 4.2):
        for wy in (-1.25, 1.25):
            bs.cyl((wx, wy, 0.48), 0.48, 0.32, M["tyre"], axis="y", n=18)
            bs.cyl((wx, wy * 1.13, 0.48), 0.26, 0.02, M["chrome"], axis="y", n=14)
    bs.box((-1.0, -1.32, 1.05), (2.4, 0.01, 0.2), M["white_band"])
    bs.text("CITY  TRANSIT", (-1.0, -1.33, 1.05), 0.14, M["ink"], kind="sign")


def build_s3(coll, M):
    g = S.Group("StreetC")
    f = K.L(g)
    ground(g, 92.0, 142.0, M)
    hotel(g, M)
    fs = K.L(g, (0, SHOP_Y, 0), 0)
    K.shop_building(g, 112.0, 124.0, M, 11.0, M["brick"], "RADIO  &  TV", "shop", face=SHOP_Y)
    K.roller_shutter(fs, 112.5, 121.9, 0.6, 2.4, M, graffiti=[("V", 0.8, 0, 0.0, 0.2), ("is watching", 0.2, -3, 1.6, -0.3)])
    K.shop_building(g, 124.0, 142.0, M, 15.0, M["ashlar_dark"], "GRAND  DEPARTMENT  STORE", "shop", face=SHOP_Y)
    K.awning(fs, 124.4, 133.0, 3.3, 1.4, M, "", striped=False)
    side_street(g, M)
    crossroads(g, M)
    for x in (96.0, 126.0):
        street_lamp(f, x, M)
    for (ax, az) in ((113.5, 6.0), (119.0, 8.9), (126.0, 9.0)):
        K.ac_unit(fs, ax, az, M)
    K.litter_bin(f, 113.0, 5.6, M)
    pigeons(f, [(100.0, SHOP_Y - 0.3, 13.15), (100.4, SHOP_Y - 0.3, 13.15), (118.5, 3.8, 2.75), (131.0, 0.5, 3.17)], M)
    return g.finish(coll)


def build_backdrop(coll, M):
    """The far side of the road (seen in the reverse shots): shopfronts and windows, its kerb and
    pavement, parked cars, lamps; the city's towers behind the near side; the sky."""
    g = S.Group("StreetCity")
    f = K.L(g)
    f.box((60.0, -29.4, -0.08), (180.0, 2.8, 0.16), M["paving"])
    f.box((60.0, -28.05, -0.08), (180.0, 0.3, 0.16), M["kerb"])
    ff = K.L(g, (0, -30.0, 0), 180)
    names = ("HARDWARE", "BARBER", "TAILOR", "DELI", "BOOKS", "DINER", "LAUNDRY", "OPTICIAN", "TOBACCO", "RADIO",
             "SHOES", "BAKER", "CINEMA", "PAWN", "FLORIST", "BUTCHER", "GARAGE", "HOTEL")
    for k in range(18):
        x0 = -20.0 + k * 9.5
        h = 9.0 + (k * 37 % 9)
        wm = (M["brick"], M["stucco"], M["ashlar"], M["brick_dark"])[k % 4]
        u0, u1 = -(x0 + 9.2), -x0
        ups = [(u0 + 1.6 + c * 3.0, 1.1, 5.0 + r * 2.6, 6.5 + r * 2.6, False) for r in range(int((h - 4.5) / 2.6)) for c in range(3)]
        K.wall(ff, u0, u1, 0.0, h, 1.0, wm, ups + [((u0 + u1) / 2, 7.0, 0.6, 3.0, False)], v=0.5)
        for (c, w0, w1, wd, _a) in ups:
            K.window(ff, c, w0, w1, wd, M, M["frame_pale"], M["glass_ext"], M["trim"], cols=2, rows=2, blind=rnd.choice((None, 0.5)), keystone=False)
        ff.box(((u0 + u1) / 2, 0.15, 1.8), (7.0, 0.03, 2.4), M["glass_shop"])
        ff.box(((u0 + u1) / 2, -0.1, 3.4), (9.0, 0.2, 0.5), M["sf_paint"])
        ff.text(names[k], ((u0 + u1) / 2, -0.21, 3.4), 0.3, M["white_paint"], kind="serif")
        K.cornice(ff, u0, u1, h - 0.45, M["trim"], depth=0.35)
    for x in (-12.0, 8.0, 28.0, 48.0, 68.0, 88.0, 108.0, 128.0):
        K.lamp_post(f, x, -28.5, M, arm=1.0, h=5.4)
    for (x, m, yaw) in ((2.0, M["car_grey"], 0), (36.0, M["car_pale"], 180), (58.0, M["car_black"], 0), (97.0, M["car_grey"], 180)):
        K.sedan(g, (x, -26.6, -0.15), yaw, M, m, M["glass_ext"])
    for k in range(22):
        x = -30.0 + k * 8.5 + rnd.uniform(-2, 2)
        h = rnd.uniform(22.0, 60.0)
        yy = rnd.uniform(40.0, 70.0)
        w_, d_ = rnd.uniform(7, 12), rnd.uniform(7, 12)
        f.box((x, yy, h / 2), (w_, d_, h), M["city"])
        for r in range(int(h / 3.0)):
            f.box((x, yy - d_ / 2 - 0.02, 1.5 + r * 3.0), (w_ * 0.9, 0.05, 1.2), M["glass_ext"])
        if h > 45:
            f.cyl((x, yy, h + 2.0), 0.15, 4.0, M["steel"], n=6)
    f.box((60.0, 120.0, 40.0), (420.0, 1.0, 80.0), M["sky_day"])
    return g.finish(coll)


# ---------------------------------------------------------------- the moving parts

def black_car(name, c, M, coll, open_door=False):
    """The sedan: a separate object (the Driver's moves; the kerb one is static). Its origin stays at
    the world origin, as StoryStreet expects (it builds its own pivot at the car's middle)."""
    g = S.Group(name)
    K.sedan(g, c, 0, M, M["car_black"], M["glass_ext"])
    return g.finish(coll)


def train(coll, M):
    """Four carriages on the deck (the game runs them across): bodies with a curved roof, windows in
    frames, sliding doors, the livery band, roof vents, bogies with wheels, couplings."""
    g = S.Group("Train")
    f = K.L(g)
    for k in range(4):
        cx = 75.0 + k * 15.2
        body = [(-7.3, 0.0), (7.3, 0.0), (7.3, 2.7), (7.0, 3.05), (6.4, 3.2), (-6.4, 3.2), (-7.0, 3.05), (-7.3, 2.7)]
        K.L(g, (cx, 1.0, DECK_Z + 1.0), 0).wallshape(body, -1.4, 1.4, M["stone_dark"])
        for s in (-1, 1):
            yy = 1.0 + s * 1.41
            for i in range(8):
                x = cx - 6.2 + i * 1.75
                if i in (2, 5):
                    f.box((x, yy, DECK_Z + 2.2), (1.2, 0.02, 2.2), M["steel"])                          # doors
                    f.box((x, yy + s * 0.005, DECK_Z + 2.6), (0.9, 0.02, 0.9), M["window_day"])
                    f.box((x, yy + s * 0.01, DECK_Z + 2.2), (0.02, 0.02, 2.1), M["reveal"])
                    continue
                f.box((x, yy, DECK_Z + 3.0), (1.1, 0.02, 0.9), M["window_day"])
                f.box((x, yy + s * 0.008, DECK_Z + 3.0), (1.18, 0.01, 0.06), M["steel"])
            f.box((cx, yy + s * 0.005, DECK_Z + 1.65), (14.4, 0.02, 0.18), M["white_band"])
            if s < 0:
                f.text(f"0{k + 4}2", (cx + 6.4, yy + s * 0.012, DECK_Z + 1.4), 0.18, M["white_paint"], kind="sign")
        for i in range(4):
            f.box((cx - 4.5 + i * 3.0, 1.0, DECK_Z + 4.28), (1.0, 0.8, 0.16), M["steel"], bevel=0.03)
        for wx in (cx - 5.0, cx + 5.0):
            f.box((wx, 1.0, DECK_Z + 0.75), (2.4, 2.2, 0.4), M["steel"], bevel=0.03)
            for ax in (-0.75, 0.75):
                for wy in (0.0, 2.0):
                    f.cyl((wx + ax, wy, DECK_Z + 0.5), 0.42, 0.12, M["steel_mid"], axis="y", n=14)
        f.box((cx + 7.5, 1.0, DECK_Z + 1.2), (0.6, 0.3, 0.2), M["steel"])
    return g.finish(coll)


# ---------------------------------------------------------------- light, world, export

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
    D.MATS.clear()
    K.reset()
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
    objs = [a, b, c, city, kerb_car, driver_car, tr]
    total = sum(K.finalize(o) for o in objs)
    print(f"  street set: {total} triangles")
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
            ("T1  the bank's doors, low", S.shot_camera("P9", (-1.0, 1.0, -3.5), (-6.5, 2.4, 5.0), 50), None),
            ("S2  the stalls, close", S.shot_camera("P10", (38.0, 1.6, -2.5), (38.0, 1.1, 2.6), 46), None),
            ("S4  the phone box and the newsstand", S.shot_camera("P11", (113.0, 1.6, -1.5), (117.0, 1.5, 3.4), 50), None),
            ("T18  up the side street", S.shot_camera("P12", (106.5, 1.6, -8.0), (108.0, 1.2, 8.0), 38), None),
        ]
        sel = [x for x in sys.argv if x.startswith("--shots=")]
        if sel:
            keep = {int(v) for v in sel[0].split("=", 1)[1].split(",") if v}
            cams = [cm for i, cm in enumerate(cams, 1) if i in keep]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "street_set.png" if not sel else "street_set_part.png")
        return
    for o in objs:
        S.unwrap(o)
    S.bake(a, 4096, 20)
    S.bake(b, 4096, 20)
    S.bake(c, 4096, 20)
    S.bake(city, 2048, 8)
    for o in (kerb_car, driver_car):
        S.bake(o, 1024, 12)
    S.bake(tr, 1024, 12)
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(objs + marks)


if __name__ == "__main__":
    main()
