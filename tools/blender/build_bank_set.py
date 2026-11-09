"""Chapter 2: THE BANK (docs/story/chapter2_bank.md). The morning after the storm: a wet city
street, MERIDIAN TRUST's stone front with its portico and its mark carved over the doors, the
marble lobby (teller windows behind glass, a clock at 8:41, pendant lamps, a velvet rope), a corridor
through a barred gate to the round vault door, and the safe-deposit room with its walls of numbered
boxes. Built in the fight-mode tower's palette, like the warehouse (build_story_set's materials and
light go through its TOWER tone): pale stone and marble, black steel, white trims.

Real surfaces and real detail (city_kit.py, detail.py): rusticated and coursed stone, veined marble
in a patterned floor, fluted columns with their bases and capitals, a cornice with dentils and
modillions, a pediment with the mark in it, arched windows with frames and grilles; inside a
coffered ceiling with a laylight, a panelled teller counter with speaking grilles, bells, pens and
the glowing palm scanner, the tellers' desks, a wall of pigeonholes, check-writing desks, rope
posts, CCTV, plants, paintings; the vault door with its locking bolts, wheel and dial; the wall of
boxes numbered round 417, each with its two keyholes. Outside: paving, kerbs, drains, lamp posts,
bollards, a bench, bins, bicycles, the cafe, the pawnbroker and the florist under their awnings,
fire escapes, posters, parked cars, puddles.

    /home/user/.venv-blender/bin/python tools/blender/build_bank_set.py               # build, bake, export
    /home/user/.venv-blender/bin/python tools/blender/build_bank_set.py --preview     # the preview sheet only
    /home/user/.venv-blender/bin/python tools/blender/build_bank_set.py --preview --shots=1,4   # some shots

Writes Assets/Art/Bank/BankSet.fbx and its bakes (Story_BankShell.png, Story_BankStreet.png,
Story_BankFront.png); --preview renders docs/previews/bank_set.png instead (no bake, no export).

Coordinates as in the warehouse: Blender X = Unity x (along the lane), Blender Y = Unity z
(depth, away from the camera), Blender Z = Unity y (up). The lane is y = 0, the floor z = 0
everywhere (a level entrance), the camera side y < -2.2 is the road. Nothing new stands on the
floor between y -0.8 and 0.8 where people walk."""
import math
import os
import random
import sys

import bpy  # noqa: E402  (bpy first: it provides bmesh and mathutils)
import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import city_kit as K  # noqa: E402
import detail as D  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Bank")
rnd = random.Random(11)

# the plan (metres)
X0, X1 = 0.0, 46.0                 # the bank's west (entrance) and east walls
YF, YB = -2.0, 16.0                # its front (camera side, cut away) and back walls
H = 11.0                           # the hall's height
LOBBY_X1 = 26.0                    # the lobby ends, the corridor begins
GATE_X = 28.5                      # the barred gate
VAULT_X = 33.0                     # the vault's wall and round door
VAULT_TOP = 3.8                    # the low ceiling over corridor and vault
VAULT_BACK = 2.8                   # the vault room's back wall (the wall of boxes)
COUNTER = (12.0, 23.0, 1.7, 2.5)   # the teller counter: x0, x1, y0, y1
BOX417 = (37.25, 1.45)             # box 417 on the wall of boxes: x, z
TELLER_X = 16.1                    # the clerk's window (StoryBank.TellerX): the palm scanner is here
TABLE = (39.5, 0.75, 0.905)        # the viewing table (StoryBank.TableX, TableZ; its top)
DOOR_Y, DOOR_H = 1.2, 3.6          # the entrance: y -1.2..1.2, 3.6 high
WEST_WIN = (3.8, 7.0, 10.2, 13.4)  # the windows between the portico's columns (y)
BACK_WIN = (4.0, 10.0, 16.0, 22.0)  # the lobby's tall back windows (x)
CLOCK = (13.0, 9.6)                # the lobby clock (x, z) on the back wall: 8:41
TELLERS = [COUNTER[0] + (k + 0.5) * (COUNTER[1] - COUNTER[0]) / 4 for k in range(4)]


def mats():
    M = S.materials()
    mat = S.mat
    M["stone"] = mat("Stone", (0.44, 0.44, 0.43), 0.8)               # tone() keeps it: chosen grey below
    M["stone_dark"] = mat("StoneDark", (0.2, 0.2, 0.2), 0.85)
    M["marble_pale"] = mat("MarblePale", (0.5, 0.5, 0.5), 0.15)
    M["marble_dark"] = mat("MarbleDark", (0.06, 0.06, 0.065), 0.15)
    M["wood_dark"] = mat("CounterWood", (0.08, 0.06, 0.05), 0.5)
    M["velvet"] = mat("Velvet", (0.05, 0.02, 0.02), 0.95)
    M["box_steel"] = mat("BoxSteel", (0.32, 0.33, 0.34), 0.35, metal=0.6)
    M["vault_steel"] = mat("VaultSteel", (0.26, 0.27, 0.28), 0.3, metal=0.7)
    M["keyhole"] = mat("Keyhole", (0.02, 0.02, 0.02), 0.6)
    M["window_day"] = mat("WindowDay", (0.8, 0.82, 0.85), 0.2, emit=(0.9, 0.92, 0.95), strength=2.2)
    M["globe"] = mat("Globe", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=18.0)
    M["strip"] = mat("StripLight", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=14.0)
    M["laylight"] = mat("Laylight", (0.9, 0.9, 0.9), 0.3, emit=(1, 1, 1), strength=3.0)
    M["sky_day"] = mat("SkyDay", (0.6, 0.62, 0.66), 0.5, emit=(0.8, 0.82, 0.86), strength=1.2)
    M["car"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["car_grey"] = mat("CarGreyDay", (0.2, 0.2, 0.2), 0.3, metal=0.5)
    M["car_pale"] = mat("CarPaleDay", (0.45, 0.45, 0.45), 0.3, metal=0.5)
    M["glass_ext"] = mat("ExtGlass", (0.06, 0.065, 0.07), 0.05)
    M["scanner"] = mat("ScannerGlow", (0.9, 0.95, 1.0), 0.2, emit=(1, 1, 1), strength=5.0)
    M["screen"] = mat("ScreenGlow", (0.7, 0.72, 0.75), 0.3, emit=(1, 1, 1), strength=1.4)
    K.mats(M)
    M["brass"] = K.grey("BankBrass", 0.52, 0.28, metal=0.8)              # in black and white: a pale bright trim
    # surfaces with texture (object space: walls facing the camera "xz", across the lane "yz", floors "xy")
    M["stone_w"] = K.courses("BankAshlar", 0.4, 0.2, "yz", (1.1, 0.5), joint_w=0.008, grime=0.35)
    M["stone_y"] = K.courses("BankAshlar", 0.4, 0.2, "xz", (1.1, 0.5), joint_w=0.008, grime=0.35)
    M["stone_in"] = K.courses("BankInnerStone", 0.44, 0.3, "xz", (1.4, 0.7), joint_w=0.004, grain=0.25, grime=0.2, rough=0.6)
    M["stone_in_w"] = K.courses("BankInnerStone", 0.44, 0.3, "yz", (1.4, 0.7), joint_w=0.004, grain=0.25, grime=0.2, rough=0.6)
    M["trim"] = K.courses("BankTrim", 0.47, 0.4, "xz", (3.0, 1.5), joint_w=0.002, grain=0.25, grime=0.3, bump=0.1)
    M["trim_w"] = K.courses("BankTrim", 0.47, 0.4, "yz", (3.0, 1.5), joint_w=0.002, grain=0.25, grime=0.3, bump=0.1)
    M["rustic_w"] = K.courses("BankRustic", 0.34, 0.1, "yz", (1.2, 0.5), joint_w=0.03, grime=0.5, bump=0.9)
    M["plinth"] = K.courses("BankPlinth", 0.18, 0.08, "yz", (1.4, 0.5), joint_w=0.01)
    M["m_pale"] = K.marble("BankMarblePale", 0.52, 0.2, "xy")
    M["m_dark"] = K.marble("BankMarbleDark", 0.045, 0.3, "xy")
    M["m_pale_wall"] = K.marble("BankMarblePale", 0.52, 0.2, "xz")
    M["m_dark_wall"] = K.marble("BankMarbleDark", 0.045, 0.3, "xz")
    M["m_dark_w"] = K.marble("BankMarbleDark", 0.045, 0.3, "yz")
    M["m_pale_w"] = K.marble("BankMarblePale", 0.52, 0.2, "yz")
    M["m_column"] = K.marble("BankMarbleColumn", 0.5, 0.25, "xz", scale=1.6)
    M["wood"] = D.planks("BankWalnut", 0.06, "xz", board=0.32)
    M["wood_w"] = D.planks("BankWalnut", 0.06, "yz", board=0.32)
    M["wood_top"] = D.planks("BankWalnut", 0.06, "xy", board=0.32)
    M["paving"] = K.courses("BankPaving", 0.26, 0.1, "xy", (0.9, 0.6), joint_w=0.012, grime=0.55, rough=0.35, bump=0.3)
    M["setts"] = D.cobbles("BankSetts", 0.2, wet=0.7)
    M["kerb"] = K.courses("BankKerb", 0.4, 0.12, "xz", (1.0, 1.0), joint_w=0.008, rough=0.5)
    M["road"] = K.tarmac("BankTarmac", 0.06, 0.75)
    M["vault_floor"] = K.courses("BankVaultFloor", 0.12, 0.05, "xy", (0.8, 0.8), joint_w=0.006, grain=0.3, rough=0.25)
    M["brick"] = K.courses("BankBrick", 0.24, 0.12, "xz", (0.23, 0.075), joint_w=0.008, var=0.65, grime=0.5)
    M["brick_dark"] = K.courses("BankBrickDark", 0.12, 0.07, "xz", (0.23, 0.075), joint_w=0.008, var=0.7, grime=0.5)
    M["stucco"] = K.courses("BankStucco", 0.46, 0.4, "xz", (4.0, 2.0), joint_w=0.002, grain=0.35, grime=0.55, bump=0.05)
    M["stucco_dark"] = K.courses("BankStuccoDark", 0.16, 0.14, "xz", (4.0, 2.0), joint_w=0.002, grain=0.35, grime=0.5, bump=0.05)
    M["felt"] = K.grey("BankFelt", 0.03, 0.95)
    M["leather"] = K.grey("BankLeather", 0.035, 0.55)
    M["frame_dark"] = K.grey("BankWindowFrame", 0.04, 0.5, metal=0.4)
    M["frame_pale"] = K.grey("BankWindowFramePale", 0.6, 0.5)
    M["canvas"] = K.grey("BankPainting", 0.12, 0.9)
    M["canvas_pale"] = K.grey("BankPaintingPale", 0.42, 0.9)
    M["white_paint"] = K.grey("BankWhitePaint", 0.72, 0.6)
    return M


S.TONE.update({"Stone": 0.40, "StoneDark": 0.16, "MarblePale": 0.42, "MarbleDark": 0.05, "CounterWood": 0.06,
               "Velvet": 0.035, "BoxSteel": 0.28, "VaultSteel": 0.2, "CarBlack": 0.025, "CarGreyDay": 0.16,
               "CarPaleDay": 0.42, "ExtGlass": 0.05})


# ---------------------------------------------------------------- shared pieces

def prism_x(g, pts_yz, x0, x1, m):
    """Extrude a 2D outline (y, z) along x (an emblem on the west face)."""
    tmp = bmesh.new()
    f0 = [tmp.verts.new((x0, y, z)) for y, z in pts_yz]
    f1 = [tmp.verts.new((x1, y, z)) for y, z in pts_yz]
    tmp.faces.new(f0)
    tmp.faces.new(list(reversed(f1)))
    n = len(pts_yz)
    for i in range(n):
        tmp.faces.new((f0[i], f0[(i + 1) % n], f1[(i + 1) % n], f1[i]))
    g._merge(tmp, m)


def shield(g, cy, cz, s, x, m, hole=None):
    """The bank's mark: a shield with a keyhole through it (the keyhole in a darker inlay)."""
    pts = [(cy - 0.82 * s, cz + 0.88 * s), (cy + 0.82 * s, cz + 0.88 * s)]
    for i in range(1, 12):
        v = 0.1 - i / 12 * 1.05
        pts.append((cy + 0.82 * math.sqrt(max(0.0, (v + 0.95) / 1.05)) * s, cz + v * s))
    pts.append((cy, cz - 0.95 * s))
    for i in range(11, 0, -1):
        v = 0.1 - i / 12 * 1.05
        pts.append((cy - 0.82 * math.sqrt(max(0.0, (v + 0.95) / 1.05)) * s, cz + v * s))
    prism_x(g, pts, x - 0.06, x, m)
    if hole is not None:
        g.cyl((x - 0.07, cy, cz + 0.22 * s), 0.2 * s, 0.02, hole, rot=(0, 90, 0), n=20)
        prism_x(g, [(cy - 0.07 * s, cz + 0.18 * s), (cy + 0.07 * s, cz + 0.18 * s), (cy + 0.13 * s, cz - 0.55 * s), (cy - 0.13 * s, cz - 0.55 * s)], x - 0.075, x - 0.065, hole)


def laurel(f, u0, w0, u1, w1, m, n=9, side=1):
    """A laurel spray carved in relief: a stem and pairs of leaves along it (local frame)."""
    f.beam((u0, -0.03, w0), (u1, -0.03, w1), 0.04, 0.04, m)
    for k in range(n):
        t = (k + 0.5) / n
        u, w = u0 + (u1 - u0) * t, w0 + (w1 - w0) * t
        ang = math.degrees(math.atan2(w1 - w0, u1 - u0))
        for s in (-1, 1):
            f.box((u, -0.05, w + s * 0.07), (0.16 * (1.1 - t * 0.4), 0.04, 0.06), m, rot=(0, -(ang + s * 40), 0))


def column(f, x, y, z0, h, r, m, mcap=None, flutes=20, ionic=True):
    """A stone (or marble) column: an Attic base on a square plinth, a fluted shaft with entasis,
    an Ionic capital (necking, echinus with egg-and-dart, volutes on both faces, the abacus)."""
    mc = mcap or m
    f.box((x, y, z0 + 0.09), (r * 2.7, r * 2.7, 0.18), m, bevel=0.02)
    f.lathe((x, y, z0 + 0.18), [(r * 1.3, 0.0), (r * 1.32, 0.06), (r * 1.22, 0.12), (r * 1.06, 0.15), (r * 1.04, 0.22),
                                (r * 1.16, 0.27), (r * 1.12, 0.33), (r * 1.02, 0.36), (0.0, 0.36)], m, n=24)
    zs = z0 + 0.54
    top = z0 + h - (0.62 if ionic else 0.4)
    f.fluted((x, y, zs), r, r * 0.86, top - zs, m, flutes=flutes)
    f.torus((x, y, zs + 0.02), r * 1.0, 0.035, m, n1=24, n2=5)
    f.torus((x, y, top - 0.02), r * 0.88, 0.03, mc, n1=24, n2=5)
    if ionic:
        f.lathe((x, y, top), [(r * 0.88, 0.0), (r * 0.9, 0.06), (r * 1.12, 0.22), (r * 1.18, 0.3), (0.0, 0.3)], mc, n=24)
        for k in range(16):                                                                  # egg and dart
            a = k / 16 * math.tau
            f.box((x + math.cos(a) * r * 1.08, y + math.sin(a) * r * 1.08, top + 0.17), (0.07, 0.07, 0.1), mc, rot=(0, 0, math.degrees(a)), bevel=0.02)
        for s in (-1, 1):                                                                    # the volutes, on both faces
            for side in (-1, 1):
                c = (x + s * r * 1.12, y + side * r * 0.9, top + 0.2)
                f.torus(c, r * 0.32, 0.045, mc, axis="y", n1=16, n2=5)
                f.torus(c, r * 0.17, 0.04, mc, axis="y", n1=12, n2=5)
                f.cyl(c, r * 0.07, 0.1, mc, axis="y", n=8)
            f.cyl((x + s * r * 1.12, y, top + 0.2), r * 0.3, r * 1.8, mc, axis="y", n=14)      # the bolster
        f.box((x, y, top + 0.53), (r * 2.9, r * 2.9, 0.18), mc, bevel=0.025)                  # the abacus
    else:
        f.lathe((x, y, top), [(r * 0.88, 0.0), (r * 1.2, 0.2), (r * 1.2, 0.26), (0.0, 0.26)], mc, n=24)
        f.box((x, y, top + 0.33), (r * 2.7, r * 2.7, 0.14), mc, bevel=0.02)


def pendant(f, x, y, zc, M, top=None):
    """A pendant globe on a rod: a ceiling rose, a rod with collars, a brass gallery, the glowing globe."""
    top = top if top is not None else H - 0.32
    f.lathe((x, y, top - 0.06), [(0.0, 0.0), (0.12, 0.0), (0.22, 0.03), (0.3, 0.06), (0.0, 0.06)], M["trim"], n=16)
    f.cyl((x, y, (top + zc + 0.5) / 2), 0.018, top - zc - 0.5, M["brass"], n=8)
    for z in (top - 0.5, zc + 1.2):
        f.lathe((x, y, z), [(0.0, -0.04), (0.04, -0.04), (0.05, 0.0), (0.04, 0.04), (0.0, 0.04)], M["brass"], n=10)
    f.lathe((x, y, zc + 0.24), [(0.0, 0.26), (0.06, 0.26), (0.08, 0.2), (0.16, 0.12), (0.17, 0.06), (0.12, 0.0), (0.0, 0.0)], M["brass"], n=16)
    f.lathe((x, y, zc - 0.32), [(0.0, 0.0), (0.12, 0.03), (0.26, 0.15), (0.33, 0.32), (0.3, 0.48), (0.19, 0.58), (0.12, 0.6), (0.0, 0.6)], M["globe"], n=20)
    f.lathe((x, y, zc - 0.38), [(0.0, 0.0), (0.04, 0.0), (0.03, 0.08), (0.0, 0.08)], M["brass"], n=8)


def chair(f, x, y, yaw, M, office=False):
    s = f.sub((x, y, 0), yaw)
    if office:
        for k in range(5):
            a = k / 5 * math.tau
            s.beam((0, 0, 0.08), (math.cos(a) * 0.28, math.sin(a) * 0.28, 0.04), 0.04, 0.03, M["iron"])
            s.cyl((math.cos(a) * 0.28, math.sin(a) * 0.28, 0.025), 0.025, 0.05, M["tyre"], n=6)
        s.cyl((0, 0, 0.27), 0.025, 0.4, M["chrome"], n=8)
        s.box((0, 0, 0.48), (0.46, 0.44, 0.08), M["leather"], bevel=0.03)
        s.box((0, 0.22, 0.8), (0.42, 0.06, 0.5), M["leather"], rot=(-8, 0, 0), bevel=0.03)
        s.beam((0, 0.18, 0.48), (0, 0.24, 0.6), 0.04, 0.02, M["iron"])
        return
    s.box((0, 0, 0.46), (0.44, 0.42, 0.06), M["leather"], bevel=0.02)
    for dx in (-0.18, 0.18):
        for dy in (-0.17, 0.17):
            s.lathe((dx, dy, 0), [(0.02, 0.0), (0.025, 0.2), (0.02, 0.43), (0.0, 0.43)], M["wood"], n=6)
    for dx in (-0.18, 0.18):
        s.box((dx, 0.19, 0.72), (0.04, 0.04, 0.52), M["wood"])
    s.box((0, 0.2, 0.86), (0.42, 0.04, 0.2), M["wood"], bevel=0.01)
    s.box((0, 0.2, 0.68), (0.34, 0.03, 0.24), M["leather"])


def painting(f, u, w, wd, ht, M, kind):
    """A framed painting (local frame): a gilt frame of four mouldings, the canvas, a picture light."""
    f.box((u, 0.0, w), (wd + 0.24, 0.04, ht + 0.24), M["brass"], bevel=0.03)
    f.box((u, -0.035, w), (wd + 0.06, 0.04, ht + 0.06), M["frame_dark"])
    f.box((u, -0.05, w), (wd, 0.02, ht), M["canvas"])
    if kind == "portrait":                                                                    # the founder
        f.lathe((u, -0.065, w - ht * 0.5), [(0.0, 0.0), (wd * 0.38, 0.0), (wd * 0.4, ht * 0.2), (wd * 0.3, ht * 0.42), (wd * 0.12, ht * 0.5), (0.0, ht * 0.5)], M["canvas_pale"], n=12, axis="z")
        f.lathe((u, -0.065, w + ht * 0.08), [(0.0, 0.0), (wd * 0.14, 0.03), (wd * 0.17, ht * 0.14), (wd * 0.12, ht * 0.28), (0.0, ht * 0.3)], M["canvas_pale"], n=12)
        f.box((u, -0.07, w - ht * 0.06), (wd * 0.08, 0.01, ht * 0.18), M["canvas"])
        f.box((u, -0.06, w - ht * 0.5 + 0.06), (wd + 0.04, 0.02, 0.12), M["canvas"])
    else:                                                                                     # the harbour, a landscape
        f.box((u, -0.062, w - ht * 0.18), (wd * 0.98, 0.01, ht * 0.3), M["canvas_pale"])
        for k in range(5):
            f.box((u - wd * 0.4 + k * wd * 0.2, -0.065, w - ht * 0.04 + (k % 2) * 0.08), (wd * 0.12, 0.01, ht * 0.12 + (k % 3) * 0.06), M["canvas"])
        f.wallshape([(-0.35, 0.0), (0.35, 0.0), (0.0, 0.5)], -0.07, -0.06, M["canvas_pale"], c=(u + wd * 0.2, 0, w - ht * 0.02))
        f.box((u + wd * 0.2, -0.066, w - ht * 0.05), (0.03, 0.01, ht * 0.4), M["canvas"])
    f.box((u, -0.25, w + ht / 2 + 0.22), (wd * 0.6, 0.1, 0.06), M["brass"], bevel=0.02)     # the picture light
    f.beam((u, 0.0, w + ht / 2 + 0.16), (u, -0.22, w + ht / 2 + 0.22), 0.02, 0.02, M["brass"])
    f.box((u, -0.045, w - ht / 2 - 0.2), (0.4, 0.02, 0.1), M["brass"])                       # the name plate


def tall_window_in(f, u, wd, w0, w1, M, t=0.4):
    """The inside of an arched window cut through the wall (local frame on the wall's inside face):
    daylight glass at mid-depth, a steel frame with mullions, transoms and fanlight bars, a marble
    sill, a stone architrave with a keystone."""
    R = wd / 2
    gl = t * 0.5
    f.box((u, gl, (w0 + w1) / 2), (wd, 0.04, w1 - w0), M["window_day"])
    pts = [(math.cos(math.pi * k / 16) * R, math.sin(math.pi * k / 16) * R) for k in range(17)]
    f.wallshape(pts, gl - 0.02, gl + 0.02, M["window_day"], c=(u, 0, w1))
    fr = M["frame_dark"]
    for uu in (u - wd / 2 + 0.04, u - wd / 6, u + wd / 6, u + wd / 2 - 0.04):
        f.box((uu, gl - 0.04, (w0 + w1) / 2), (0.05, 0.05, w1 - w0), fr)
    k = 0
    ww = w0
    while ww <= w1 + 1e-3:
        f.box((u, gl - 0.04, ww), (wd, 0.05, 0.05 if k % 3 else 0.08), fr)
        ww += (w1 - w0) / 6
        k += 1
    for j in range(1, 4):
        a = math.pi * j / 4
        f.beam((u, gl - 0.04, w1), (u + math.cos(a) * R, gl - 0.04, w1 + math.sin(a) * R), 0.04, 0.05, fr)
    f.torus((u, gl - 0.04, w1), R * 0.4, 0.025, fr, axis="y", n1=16, n2=4)
    f.box((u, -0.1, w0 - 0.05), (wd + 0.3, 0.32, 0.1), M["m_dark_wall"], bevel=0.015)      # sill
    for s in (-1, 1):
        f.box((u + s * (R + 0.12), -0.04, (w0 + w1) / 2), (0.24, 0.08, w1 - w0 + 0.1), M["trim"], bevel=0.01)
    for kk in range(11):                                                                     # the arch's architrave
        a0, a1 = math.pi * kk / 11, math.pi * (kk + 1) / 11
        am = (a0 + a1) / 2
        f.box((u + math.cos(am) * (R + 0.12), -0.04, w1 + math.sin(am) * (R + 0.12)), (0.24, 0.08, (a1 - a0) * (R + 0.12) * 1.05), M["trim"], rot=(0, -math.degrees(am) + 90, 0))
    f.wallshape([(-0.16, 0.0), (0.16, 0.0), (0.22, 0.5), (-0.22, 0.5)], -0.14, 0.0, M["trim"], c=(u, 0, w1 + R - 0.1))


# ---------------------------------------------------------------- the lobby

def build_floor(g, M):
    """The marble: a pale veined field in 1.2 m tiles, dark cabochons at the corners, a dark border
    band round the room, and the bank's mark inlaid as a compass medallion before the counter."""
    f = K.L(g)
    f.box((LOBBY_X1 / 2, (YF + YB) / 2, -0.06), (LOBBY_X1, YB - YF, 0.12), M["m_pale"])
    for i in range(1, int(LOBBY_X1 / 1.2) + 1):
        f.box((i * 1.2, (YF + YB) / 2, 0.0004), (0.012, YB - YF, 0.002), M["m_dark"])
    for j in range(1, int((YB - YF) / 1.2) + 1):
        f.box((LOBBY_X1 / 2, YF + j * 1.2, 0.0004), (LOBBY_X1, 0.012, 0.002), M["m_dark"])
    for i in range(1, int(LOBBY_X1 / 1.2) + 1):
        for j in range(1, int((YB - YF) / 1.2) + 1):
            f.box((i * 1.2, YF + j * 1.2, 0.0008), (0.17, 0.17, 0.003), M["m_dark"], rot=(0, 0, 45))
    # the border band (0.35 wide, 0.6 in from the walls) and a thin pale fillet inside it
    for (cx, cy, sx, sy) in ((LOBBY_X1 / 2, YB - 0.6, LOBBY_X1 - 1.2, 0.35), (LOBBY_X1 / 2, YF + 0.6, LOBBY_X1 - 1.2, 0.35),
                             (0.6, (YF + YB) / 2, 0.35, YB - YF - 1.2), (LOBBY_X1 - 0.6, (YF + YB) / 2, 0.35, YB - YF - 1.2)):
        f.box((cx, cy, 0.001), (sx, sy, 0.004), M["m_dark"])
    # the medallion: rings, a sixteen-point star, the keyhole
    mx, my = 2.9, 4.6
    for r, m in ((2.2, M["m_dark"]), (2.05, M["m_pale"]), (1.75, M["m_dark"]), (1.62, M["m_pale"])):
        f.cyl((mx, my, 0.0012 + (2.3 - r) * 0.001), r, 0.003, m, n=48)
    pts = []
    for k in range(32):
        a = k / 32 * math.tau
        rr = 1.58 if k % 4 == 0 else (1.0 if k % 2 == 0 else 0.55)
        pts.append((mx + math.cos(a) * rr, my + math.sin(a) * rr))
    f.plan(pts, 0.0015, 0.0025, M["m_dark"])
    f.cyl((mx, my, 0.0028), 0.42, 0.002, M["brass"], n=24)
    f.cyl((mx, my + 0.06, 0.0034), 0.12, 0.002, M["m_dark"], n=16)
    f.plan([(mx - 0.05, my + 0.04), (mx + 0.05, my + 0.04), (mx + 0.09, my - 0.25), (mx - 0.09, my - 0.25)], 0.0028, 0.0036, M["m_dark"])
    # the entrance mat, its brass edge, inside the doors
    f.box((1.0, 0.0, 0.004), (1.4, 2.1, 0.008), M["felt"])
    for k in range(12):
        f.box((0.4 + k * 0.11, 0.0, 0.0085), (0.03, 1.95, 0.002), M["iron_mid"])
    for s in (-1, 1):
        f.box((1.0, s * 1.06, 0.006), (1.44, 0.03, 0.012), M["brass"])
    for xx in (0.29, 1.71):
        f.box((xx, 0.0, 0.006), (0.03, 2.14, 0.012), M["brass"])
    # the corridor (pale and dark stone in a running border) and the vault (dark polished stone)
    f.box(((LOBBY_X1 + VAULT_X) / 2, (YF + VAULT_BACK) / 2, -0.06), (VAULT_X - LOBBY_X1, VAULT_BACK - YF, 0.12), M["m_pale"])
    for yy in (YF + 0.35, VAULT_BACK - 0.35):
        f.box(((LOBBY_X1 + VAULT_X) / 2, yy, 0.001), (VAULT_X - LOBBY_X1, 0.25, 0.004), M["m_dark"])
    x = LOBBY_X1 + 0.6
    while x < VAULT_X - 0.3:
        f.box((x, 0.4, 0.001), (0.3, 0.3, 0.004), M["m_dark"], rot=(0, 0, 45))
        x += 1.2
    f.box(((VAULT_X + X1) / 2, (YF + VAULT_BACK) / 2, -0.06), (X1 - VAULT_X, VAULT_BACK - YF, 0.12), M["vault_floor"])
    f.box((VAULT_X, 0.3, 0.004), (0.7, 3.1, 0.01), M["brass"])                                  # the threshold


def build_lobby_walls(g, M):
    # the back wall, cut by four arched windows (2.6 wide, 2.6..8.8 and the arch), coursed stone
    fb = K.L(g, (0, YB + 0.2, 0), 0)
    K.wall(fb, X0 - 0.4, LOBBY_X1, 0.0, H, 0.4, M["stone_y"], [(x, 2.6, 2.6, 8.8, True) for x in BACK_WIN])
    K.wall(fb, LOBBY_X1, X1 + 0.4, 0.0, H, 0.4, M["stone_y"])
    fi = K.L(g, (0, YB, 0), 0)                                                                  # its inside face
    for x in BACK_WIN:
        tall_window_in(fi, x, 2.6, 2.6, 8.8, M)
    # a dark marble dado with pale panels, a skirting and a rail
    fi.box((LOBBY_X1 / 2, -0.03, 0.62), (LOBBY_X1, 0.06, 1.24), M["m_dark_wall"])
    fi.box((LOBBY_X1 / 2, -0.06, 0.09), (LOBBY_X1, 0.06, 0.18), M["m_dark_wall"], bevel=0.01)
    fi.box((LOBBY_X1 / 2, -0.07, 1.26), (LOBBY_X1, 0.1, 0.07), M["trim"], bevel=0.015)
    x = 0.9
    while x < LOBBY_X1 - 0.6:
        fi.box((x, -0.065, 0.7), (1.1, 0.012, 0.7), M["m_pale_wall"])
        fi.box((x, -0.072, 0.7), (1.0, 0.004, 0.6), M["m_dark_wall"])
        x += 1.5
    # pilasters on the piers, paintings between them, the crown cornice under the coffers
    for x in (1.3, 5.9, 8.1, 11.9, 14.1, 17.9, 20.1, 24.7):
        K.pilaster(fi, x, 1.3, 10.1, 0.5, M["trim"], d=0.12)
    painting(fi, 7.0, 4.6, 1.3, 1.8, M, "portrait")
    painting(fi, 19.0, 4.6, 1.3, 1.0, M, "landscape")
    K.cornice(fi, X0, LOBBY_X1, 10.1, M["trim"], depth=0.5, modillions=True)
    # under the clock: the founder's brass plaque with the mark
    fi.box((13.0, -0.03, 4.9), (1.5, 0.05, 1.0), M["brass"], bevel=0.03)
    fi.box((13.0, -0.055, 4.9), (1.36, 0.01, 0.86), M["frame_dark"])
    fi.text("MERIDIAN  TRUST", (13.0, -0.066, 5.15), 0.15, M["brass"], kind="serif")
    fi.text("FOUNDED  MDCCCLXXXVII", (13.0, -0.066, 4.92), 0.08, M["brass"], kind="serif")
    fi.text("FIDES  ET  CUSTODIA", (13.0, -0.066, 4.68), 0.08, M["brass"], kind="serif")
    # the clock, high on the back wall, in a carved stone surround: 8:41
    fi.lathe((CLOCK[0], 0.0, CLOCK[1]), [(0.0, 0.0), (1.3, 0.0), (1.3, 0.08), (1.18, 0.14), (1.12, 0.2), (0.0, 0.2)], M["trim"], n=40, axis="y")
    K.clock_face(fi, (CLOCK[0], -0.24, CLOCK[1]), 0.95, M, 8, 41, rim_m=M["brass"])
    fi.box((CLOCK[0], -0.12, CLOCK[1] - 1.42), (0.7, 0.24, 0.16), M["trim"], bevel=0.03)
    for s in (-1, 1):
        laurel(fi, CLOCK[0] + s * 0.4, CLOCK[1] - 1.3, CLOCK[0] + s * 1.35, CLOCK[1] - 0.6, M["trim"], n=6)

    # the west wall (the entrance), cut by the door and the four windows between the columns
    fw = K.L(g, (X0 - 0.2, 0, 0), 90)
    K.wall(fw, YF, YB + 0.4, 0.0, H, 0.4, M["stone_w"], [(0.0, 2 * DOOR_Y, 0.0, DOOR_H, False)] + [(y, 1.5, 1.6, 6.4, True) for y in WEST_WIN])
    fwi = K.L(g, (X0, 0, 0), 90)                                                                # inside face (u = y, out = +x)
    for y in WEST_WIN:
        tall_window_in(fwi, y, 1.5, 1.6, 6.4, M)
    for (ya, yb) in ((YF, -DOOR_Y - 0.4), (DOOR_Y + 0.4, YB)):
        fwi.box(((ya + yb) / 2, -0.03, 0.62), (yb - ya, 0.06, 1.24), M["m_dark_w"])
        fwi.box(((ya + yb) / 2, -0.07, 1.26), (yb - ya, 0.1, 0.07), M["trim_w"])
    for s in (-1, 1):                                                                           # the door's marble surround
        fwi.box((s * (DOOR_Y + 0.2), -0.05, DOOR_H / 2 + 0.1), (0.4, 0.1, DOOR_H + 0.2), M["m_dark_w"], bevel=0.02)
    fwi.box((0.0, -0.06, DOOR_H + 0.3), (2 * DOOR_Y + 0.8, 0.12, 0.4), M["m_dark_w"], bevel=0.02)
    fwi.box((0.0, -0.1, DOOR_H + 0.55), (2 * DOOR_Y + 1.0, 0.2, 0.1), M["trim_w"], bevel=0.02)
    fwi.text("WELCOME", (0.0, -0.125, DOOR_H + 0.3), 0.16, M["brass"], kind="serif")
    for y in (2.4, 8.6, 15.0):                                                                  # wall sconces
        fwi.box((y, -0.04, 4.0), (0.16, 0.08, 0.3), M["brass"], bevel=0.02)
        fwi.beam((y, -0.04, 4.0), (y, -0.3, 4.2), 0.03, 0.03, M["brass"])
        fwi.lathe((y, -0.34, 4.18), [(0.0, 0.0), (0.06, 0.0), (0.12, 0.18), (0.1, 0.26), (0.0, 0.26)], M["globe"], n=12)
    K.cornice(fwi, YF, YB, 10.1, M["trim_w"], depth=0.5, modillions=True)
    cctv_pts = [((0.25, 14.6, 7.2), 45), ((0.25, -1.6, 5.2), 135)]
    for c, yaw in cctv_pts:
        K.cctv(K.L(g), c, yaw, M)

    # the east end of the lobby (x = 26): over the corridor's mouth, a rate board, the manager's door
    fe = K.L(g, (LOBBY_X1, 0, 0), -90)                                                          # u = -y, out = -x
    fe.box((-(YB + 2.6) / 2, -0.03, 0.62), (YB - 2.6, 0.06, 1.24), M["m_dark_w"])
    fe.box((-(YB + 2.6) / 2, -0.07, 1.26), (YB - 2.6, 0.1, 0.07), M["trim_w"])
    K.cornice(fe, -YB, -YF, 10.1, M["trim_w"], depth=0.5, modillions=True)
    # the corridor mouth: a stone architrave, the sign over it
    for u in (-(2.6 - 0.15), -(YF - 0.15)):
        fe.box((u, -0.06, VAULT_TOP / 2), (0.3, 0.12, VAULT_TOP), M["trim_w"], bevel=0.02)
    fe.box((-0.3, -0.07, VAULT_TOP + 0.2), (5.2, 0.14, 0.4), M["trim_w"], bevel=0.02)
    fe.box((-0.3, -0.09, VAULT_TOP + 0.9), (3.6, 0.06, 0.7), M["brass"], bevel=0.02)
    fe.box((-0.3, -0.12, VAULT_TOP + 0.9), (3.4, 0.01, 0.56), M["frame_dark"])
    fe.text("SAFE  DEPOSIT", (-0.3, -0.13, VAULT_TOP + 0.95), 0.3, M["brass"], kind="serif")
    fe.text("& VAULTS", (-0.3, -0.13, VAULT_TOP + 0.7), 0.14, M["brass"], kind="serif")
    # the manager's door (Harrow comes from here): panelled walnut, brass plate and handle
    du = -6.0
    fe.box((du, -0.03, 1.3), (1.3, 0.06, 2.6), M["wood_w"])
    for s in (-1, 1):
        fe.box((du + s * 0.75, -0.06, 1.38), (0.2, 0.12, 2.76), M["trim_w"], bevel=0.02)
    fe.box((du, -0.07, 2.86), (1.8, 0.14, 0.2), M["trim_w"], bevel=0.02)
    fe.box((du, -0.12, 3.06), (2.0, 0.16, 0.14), M["trim_w"], bevel=0.02)
    for w in (0.75, 1.85):
        for s in (-1, 1):
            fe.box((du + s * 0.3, -0.07, w), (0.5, 0.03, 0.8 if w < 1 else 1.0), M["wood_w"], bevel=0.02)
    fe.box((du + 0.5, -0.1, 1.1), (0.04, 0.06, 0.14), M["brass"])
    fe.lathe((du + 0.5, -0.12, 1.1), [(0.0, 0.0), (0.04, 0.02), (0.035, 0.06), (0.0, 0.07)], M["brass"], n=8, axis="y")
    fe.box((du, -0.07, 1.95), (0.5, 0.01, 0.14), M["brass"])
    fe.text("MANAGER", (du, -0.078, 1.95), 0.08, M["ink"], kind="serif")
    # the rate board: brass framed, white letters on black
    ru = -11.6
    fe.box((ru, -0.03, 4.6), (3.0, 0.06, 2.4), M["brass"], bevel=0.03)
    fe.box((ru, -0.06, 4.6), (2.8, 0.01, 2.2), M["ink"])
    fe.text("RATES  OF  INTEREST", (ru, -0.07, 5.5), 0.16, M["white_paint"], kind="serif")
    rows = [("SAVINGS", "2.75 %"), ("DEPOSIT  3 MONTHS", "3.10 %"), ("DEPOSIT  12 MONTHS", "3.85 %"), ("BONDS", "4.20 %"), ("MORTGAGE", "5.60 %")]
    for k, (a, b) in enumerate(rows):
        fe.text(a, (ru - 1.25, -0.07, 5.1 - k * 0.3), 0.12, M["white_paint"], kind="body", align="LEFT")
        fe.text(b, (ru + 1.25, -0.07, 5.1 - k * 0.3), 0.12, M["white_paint"], kind="body", align="RIGHT")
    K.cctv(K.L(g), (LOBBY_X1 - 0.1, 14.8, 6.4), -45, M)
    K.cctv(K.L(g), (LOBBY_X1 - 0.1, 3.0, 5.0), -100, M)


def build_ceiling(g, M):
    """The coffered ceiling: deep moulded beams on the 3 m grid, each coffer with an inner frame and
    a rosette; over the middle of the hall a glazed laylight in a leaded grid."""
    f = K.L(g)
    f.box((X1 / 2, (YF + YB) / 2, H + 0.2), (X1 + 0.8, YB - YF + 0.8, 0.4), M["stone_dark"])
    f.box((LOBBY_X1 / 2, (YF + YB) / 2, H - 0.02), (LOBBY_X1, YB - YF, 0.04), M["stone_in"])
    xs = [1.5 + 3 * i for i in range(9)]
    ys = [-0.5 + 3 * j for j in range(6)]
    for x in xs:
        if x > LOBBY_X1:
            continue
        f.box((x, (YF + YB) / 2, H - 0.25), (0.36, YB - YF, 0.5), M["trim"], bevel=0.02)
        f.box((x, (YF + YB) / 2, H - 0.52), (0.24, YB - YF, 0.06), M["trim"], bevel=0.01)
    for y in ys:
        f.box((LOBBY_X1 / 2, y, H - 0.25), (LOBBY_X1, 0.36, 0.5), M["trim"], bevel=0.02)
        f.box((LOBBY_X1 / 2, y, H - 0.52), (LOBBY_X1, 0.24, 0.06), M["trim"], bevel=0.01)
    for x in xs:
        for y in ys:
            if x < LOBBY_X1:
                f.lathe((x, y, H - 0.62), [(0.0, 0.0), (0.12, 0.02), (0.2, 0.06), (0.22, 0.1), (0.0, 0.1)], M["brass"], n=12)
    for i in range(len(xs) - 1):
        for j in range(len(ys) - 1):
            cx, cy = xs[i] + 1.5, ys[j] + 1.5
            if cx > LOBBY_X1:
                continue
            if 7.0 < cx < 20.0 and 3.5 < cy < 11.0:                                            # the laylight
                f.box((cx, cy, H - 0.08), (2.64, 2.64, 0.04), M["laylight"])
                for k in (-0.66, 0.0, 0.66):
                    f.box((cx + k, cy, H - 0.12), (0.03, 2.64, 0.04), M["frame_dark"])
                    f.box((cx, cy + k, H - 0.12), (2.64, 0.03, 0.04), M["frame_dark"])
                continue
            for s in (-1, 1):
                f.box((cx + s * 1.1, cy, H - 0.12), (0.12, 2.3, 0.16), M["trim"])
                f.box((cx, cy + s * 1.1, H - 0.12), (2.3, 0.12, 0.16), M["trim"])
            f.lathe((cx, cy, H - 0.26), [(0.0, 0.0), (0.1, 0.0), (0.22, 0.08), (0.3, 0.18), (0.0, 0.2)], M["trim"], n=12)


def build_lobby_furniture(g, M):
    f = K.L(g)
    # columns, two rows behind the lane (Ionic, pale marble)
    for x in (5.0, 11.0, 17.0, 23.0):
        for y in (5.0, 11.0):
            column(f, x, y, 0.0, H - 0.62, 0.42, M["m_column"], M["trim"])
    # pendant globes on long rods (the three over the lane stay where the game's cones hang)
    for x in (6.0, 13.0, 20.0):
        pendant(f, x, 1.2, 5.6, M)
        pendant(f, x, 8.0, 6.0, M)
        pendant(f, x, 14.0, 6.4, M)
    # the velvet rope: weighted brass posts, ball finials, the rope hanging between their hooks
    for k in range(5):
        px = 7.0 + k * 1.1
        f.lathe((px, 0.95, 0.0), [(0.0, 0.0), (0.17, 0.0), (0.17, 0.02), (0.13, 0.06), (0.05, 0.1), (0.03, 0.14), (0.025, 0.9),
                                   (0.04, 0.92), (0.04, 0.96), (0.03, 0.97), (0.0, 0.97)], M["brass"], n=14)
        f.lathe((px, 0.95, 0.97), [(0.0, 0.0), (0.04, 0.01), (0.055, 0.05), (0.04, 0.09), (0.0, 0.1)], M["brass"], n=12)
        f.torus((px + 0.04, 0.95, 0.9), 0.025, 0.007, M["brass"], axis="y", n1=8, n2=4)
        if k < 4:
            pts = []
            for s in range(11):
                t = s / 10
                pts.append((px + 0.06 + t * 0.98, 0.95, 0.88 - math.sin(t * math.pi) * 0.2))
            for a, b in zip(pts, pts[1:]):
                f.rod(a, b, 0.024, M["velvet"], 8)
    f.lathe((6.25, 1.2, 0.0), [(0.0, 0.0), (0.16, 0.0), (0.16, 0.02), (0.05, 0.08), (0.025, 0.1), (0.025, 1.1), (0.0, 1.1)], M["brass"], n=12)
    f.box((6.25, 1.2, 1.25), (0.5, 0.03, 0.32), M["brass"], bevel=0.01)
    f.box((6.25, 1.18, 1.25), (0.44, 0.01, 0.26), M["ink"])
    f.text("PLEASE  WAIT", (6.25, 1.17, 1.3), 0.07, M["white_paint"], kind="serif")
    f.text("TO BE CALLED", (6.25, 1.17, 1.19), 0.05, M["white_paint"], kind="serif")
    # benches along the back for the waiting (walnut, brass legs, a back of slats)
    for x in (3.0, 8.0):
        f.box((x, 8.0, 0.45), (2.2, 0.5, 0.06), M["wood_top"], bevel=0.015)
        for k in range(5):
            f.box((x, 8.24 + k * 0.004, 0.6 + k * 0.1), (2.16, 0.04, 0.07), M["wood"], rot=(-8, 0, 0), bevel=0.01)
        for dx in (-0.95, 0.0, 0.95):
            f.box((x + dx, 8.0, 0.21), (0.06, 0.42, 0.42), M["brass"], bevel=0.01)
            f.box((x + dx, 8.24, 0.7), (0.06, 0.06, 0.6), M["brass"])
        for dx in (-1.1, 1.1):
            f.box((x + dx, 8.0, 0.62), (0.06, 0.5, 0.05), M["brass"], bevel=0.01)
    # the check-writing desks: marble tops sloped to both sides, slips in pockets, pens on chains, a lamp
    for (dx, dy) in ((8.2, 3.7), (2.6, 11.4)):
        f.box((dx, dy, 0.48), (1.5, 0.5, 0.96), M["wood"], bevel=0.02)
        for s in (-1, 1):
            f.box((dx, dy + s * 0.252, 0.5), (1.3, 0.01, 0.7), M["wood"], bevel=0.0)
            f.box((dx, dy + s * 0.2, 1.02), (1.8, 0.42, 0.04), M["m_dark"], rot=(s * 10, 0, 0), bevel=0.01)
            for k in range(3):
                f.box((dx - 0.55 + k * 0.55, dy + s * 0.32, 1.0), (0.22, 0.03, 0.12), M["brass"], rot=(s * 10, 0, 0))
                f.box((dx - 0.55 + k * 0.55, dy + s * 0.3, 1.05), (0.18, 0.02, 0.1), M["paper"], rot=(s * 10, 0, 0))
            f.box((dx - 0.25, dy + s * 0.22, 1.06), (0.12, 0.012, 0.012), M["iron"], rot=(0, 0, 20))
            f.box((dx + 0.35, dy + s * 0.16, 1.06), (0.21, 0.15, 0.003), M["paper"], rot=(s * 10, 0, 8 * s))
        f.box((dx, dy, 1.15), (1.6, 0.04, 0.14), M["brass"])
        f.lathe((dx, dy, 1.17), [(0.0, 0.0), (0.06, 0.0), (0.02, 0.04), (0.015, 0.3), (0.0, 0.3)], M["brass"], n=8)
        f.box((dx, dy, 1.5), (0.5, 0.18, 0.1), M["frame_dark"], bevel=0.03)
        f.box((dx, dy, 1.44), (0.44, 0.12, 0.02), M["globe"])
        f.text("DEPOSIT SLIPS", (dx, dy - 0.03, 1.15), 0.05, M["ink"], kind="serif")
    # the guard's lectern by the door: a sloped top, the visitors' book open on it
    lx, ly = 2.0, 2.4
    f.box((lx, ly, 0.5), (0.5, 0.4, 1.0), M["wood"], bevel=0.02)
    f.box((lx, ly, 1.08), (0.64, 0.5, 0.05), M["wood_top"], rot=(-14, 0, 0), bevel=0.01)
    f.box((lx, ly - 0.02, 1.13), (0.42, 0.3, 0.02), M["paper"], rot=(-14, 0, 0))
    f.box((lx, ly - 0.02, 1.135), (0.01, 0.3, 0.02), M["ink"], rot=(-14, 0, 0))
    # plants in urns: palms by the door, at the corners, by the benches
    for (px, py, kind) in ((0.9, 3.3, "palm"), (0.9, 14.9, "palm"), (25.2, 14.8, "palm"), (25.2, 3.4, "fern"), (5.6, 8.0, "ball"), (10.6, 8.0, "fern")):
        K.potted_plant(f, px, py, M, h=1.8 if kind == "palm" else 1.0, kind=kind)
    # a brass stand with the bank's leaflets
    f.box((24.6, 9.0, 0.7), (0.5, 0.35, 1.4), M["brass"], bevel=0.02)
    for k in range(4):
        f.box((24.6, 8.8, 0.45 + k * 0.28), (0.42, 0.06, 0.2), M["paper"], rot=(-15, 0, 0))


def build_counter(g, M):
    """The teller counter: a walnut front in raised panels between pilasters, a marble top with a
    rounded nosing, brass glazing frames (the glass itself is the game's), speaking grilles, the
    deal trays, each window's bell, pens and slips; the palm scanner at the clerk's window. Behind:
    the tellers' desks with screens, phones, lamps and chairs, and a wall of pigeonholes."""
    f = K.L(g)
    cx0, cx1, cy0, cy1 = COUNTER
    cxm, cym = (cx0 + cx1) / 2, (cy0 + cy1) / 2
    f.box((cxm, cym, 0.08), (cx1 - cx0 + 0.04, cy1 - cy0 + 0.04, 0.16), M["m_dark"], bevel=0.01)   # the kick
    f.box((cxm, cym, 0.6), (cx1 - cx0, cy1 - cy0, 1.0), M["wood"])
    bays = 8
    bw = (cx1 - cx0) / bays
    for k in range(bays + 1):                                                                   # pilasters
        x = cx0 + k * bw
        f.box((x, cy0 - 0.04, 0.62), (0.12, 0.08, 0.92), M["wood"], bevel=0.015)
        f.box((x, cy0 - 0.06, 1.02), (0.16, 0.1, 0.08), M["brass"], bevel=0.01)
        f.box((x, cy0 - 0.06, 0.2), (0.16, 0.1, 0.08), M["wood"], bevel=0.01)
    for k in range(bays):                                                                       # raised panels
        x = cx0 + (k + 0.5) * bw
        for (w, h) in ((0.42, 0.3), (0.8, 0.36)):
            f.box((x, cy0 - 0.02, w), (bw - 0.32, 0.04, h), M["wood"], bevel=0.02)
            f.box((x, cy0 - 0.045, w), (bw - 0.46, 0.02, h - 0.12), M["wood"], bevel=0.01)
    f.box((cxm, cym - 0.04, 1.13), (cx1 - cx0 + 0.16, cy1 - cy0 + 0.2, 0.06), M["m_dark"], bevel=0.008)
    f.cyl((cxm, cy0 - 0.13, 1.12), 0.04, cx1 - cx0 + 0.16, M["m_dark"], axis="x", n=10)
    f.box((cxm, cy0 - 0.06, 1.06), (cx1 - cx0 + 0.1, 0.06, 0.04), M["brass"])
    # the glazing: brass posts and rails (the glass is StoryBank's), a crest over the windows
    x = cx0
    while x <= cx1 + 0.01:
        f.box((x, cy0 + 0.25, 1.85), (0.07, 0.07, 1.4), M["brass"], bevel=0.01)
        f.lathe((x, cy0 + 0.25, 2.6), [(0.0, 0.0), (0.05, 0.0), (0.06, 0.04), (0.03, 0.1), (0.04, 0.14), (0.0, 0.18)], M["brass"], n=10)
        x += (cx1 - cx0) / 4
    for z in (1.17, 2.58):
        f.box((cxm, cy0 + 0.25, z), (cx1 - cx0, 0.08, 0.06), M["brass"], bevel=0.01)
    f.box((cxm, cy0 + 0.25, 2.66), (cx1 - cx0, 0.04, 0.1), M["brass"])
    for k, tx in enumerate(TELLERS):
        # the number over each window
        f.box((tx, cy0 + 0.23, 2.86), (0.8, 0.05, 0.3), M["frame_dark"], bevel=0.02)
        f.box((tx, cy0 + 0.2, 2.86), (0.74, 0.01, 0.24), M["brass"])
        f.text(f"TELLER  {k + 1}", (tx, cy0 + 0.193, 2.86), 0.1, M["ink"], kind="serif")
        f.wallshape([(-0.42, 0.0), (0.42, 0.0), (0.0, 0.16)], cy0 + 0.21, cy0 + 0.25, M["brass"], c=(tx, 0, 3.01))
        # the speaking grille: a brass ring on the glass, rings of holes
        gz = 1.64
        f.torus((tx, cy0 + 0.245, gz), 0.1, 0.012, M["brass"], axis="y", n1=18, n2=4)
        f.cyl((tx, cy0 + 0.25, gz), 0.09, 0.006, M["brass"], axis="y", n=18)
        for rr, nn in ((0.0, 1), (0.035, 6), (0.065, 12)):
            for q in range(nn):
                a = q / nn * math.tau
                f.cyl((tx + math.cos(a) * rr, cy0 + 0.244, gz + math.sin(a) * rr), 0.008, 0.004, M["ink"], axis="y", n=6)
        # the deal tray under the glass
        f.box((tx, cy0 + 0.25, 1.165), (0.56, 0.42, 0.02), M["brass"], bevel=0.005)
        f.box((tx, cy0 + 0.25, 1.172), (0.48, 0.34, 0.012), M["frame_dark"])
        # the customer's side: a service bell, a pen on its chain, slips
        f.lathe((tx + 0.45, cy0 + 0.02, 1.16), [(0.0, 0.0), (0.06, 0.0), (0.06, 0.012), (0.05, 0.02), (0.045, 0.05), (0.03, 0.07), (0.0, 0.08)], M["brass"], n=12)
        f.cyl((tx + 0.45, cy0 + 0.02, 1.25), 0.006, 0.02, M["brass"], n=6)
        f.lathe((tx - 0.45, cy0 + 0.0, 1.16), [(0.0, 0.0), (0.04, 0.0), (0.03, 0.03), (0.0, 0.03)], M["brass"], n=8)
        f.box((tx - 0.45, cy0 + 0.0, 1.22), (0.01, 0.01, 0.12), M["ink"], rot=(25, 0, 30))
        f.box((tx - 0.3, cy0 + 0.05, 1.163), (0.15, 0.21, 0.004), M["paper"], rot=(0, 0, 12))
        if k == 3:
            f.box((tx, cy0 + 0.0, 1.24), (0.4, 0.008, 0.14), M["paper"], rot=(-12, 0, 0))
            f.text("NEXT  WINDOW", (tx, cy0 - 0.01, 1.26), 0.045, M["ink"], kind="serif")
            f.text("PLEASE", (tx, cy0 - 0.01, 1.21), 0.045, M["ink"], kind="serif")
    # the palm scanner at the clerk's window (StoryBank 2.2): a tilted glass panel on a black steel
    # base, a glowing frame and the outline of a hand in it, a ready light, its label
    sx, sy = TELLER_X + 0.42, cy0 + 0.02
    sc = f.sub((sx, sy, 1.16), 0)
    sc.box((0, 0, 0.03), (0.34, 0.3, 0.06), M["iron"], bevel=0.015)
    pn = sc.sub((0, 0.02, 0.1), 0)
    tilt = (-62, 0, 0)
    pn.box((0, 0, 0), (0.3, 0.24, 0.025), M["iron"], rot=tilt, bevel=0.01)
    pn.box((0, -0.004, 0.009), (0.26, 0.2, 0.01), M["glass_shop"], rot=tilt)
    for (u, v, su, sv) in ((0, 0.096, 0.26, 0.012), (0, -0.096, 0.26, 0.012), (0.125, 0, 0.012, 0.2), (-0.125, 0, 0.012, 0.2)):
        q = Matrix.Rotation(math.radians(-62), 3, "X") @ Vector((u, v, 0.016))
        pn.box(q[:], (su, sv, 0.004), M["scanner"], rot=tilt)
    hand = [(0.0, -0.03, 0.09, 0.09), (-0.032, 0.045, 0.018, 0.07), (-0.011, 0.055, 0.018, 0.08), (0.011, 0.052, 0.018, 0.075),
            (0.032, 0.04, 0.016, 0.06), (0.058, -0.02, 0.016, 0.06)]
    for (u, v, su, sv) in hand:
        q = Matrix.Rotation(math.radians(-62), 3, "X") @ Vector((u, v, 0.016))
        pn.box(q[:], (su, sv, 0.003), M["scanner"], rot=(-62, 0, -28 if su < 0.02 and u > 0.05 else 0))
    sc.cyl((0.13, -0.13, 0.065), 0.01, 0.01, M["led"], n=6)
    sc.box((0, -0.152, 0.03), (0.2, 0.004, 0.03), M["white_paint"])
    sc.text("PLACE  PALM", (0, -0.156, 0.03), 0.018, M["ink"], kind="body")
    # behind the counter: the tellers' side (the clerk stands at y 3.25)
    for k, tx in enumerate(TELLERS):
        f.box((tx, cy1 + 0.12, 1.0), (2.6, 0.24, 0.04), M["wood_top"])                         # a shelf under the top
        f.box((tx + 0.55, cy1 + 0.06, 1.18), (0.36, 0.06, 0.28), M["iron"], rot=(8, 0, 0), bevel=0.01)   # the clerk's screen
        f.box((tx + 0.55, cy1 + 0.095, 1.18), (0.32, 0.01, 0.22), M["screen"], rot=(8, 0, 0))
        f.box((tx + 0.55, cy1 + 0.0, 1.05), (0.08, 0.08, 0.12), M["iron"])
        f.box((tx - 0.6, cy1 + 0.08, 1.06), (0.2, 0.22, 0.07), M["iron"], bevel=0.02)          # the desk phone
        f.box((tx - 0.6, cy1 + 0.06, 1.12), (0.22, 0.06, 0.05), M["iron"], bevel=0.02)
        f.box((tx - 0.6, cy1 + 0.13, 1.0), (0.008, 0.008, 0.3), M["ink"], rot=(0, 70, 30))
        # the desk, its chair, a banker's lamp, the cash drawer, rubber stamps, papers
        dy = cy1 + 1.4
        f.box((tx, dy, 0.76), (1.6, 0.7, 0.04), M["wood_top"], bevel=0.01)
        for s in (-1, 1):
            f.box((tx + s * 0.6, dy, 0.37), (0.4, 0.66, 0.74), M["wood"], bevel=0.015)
            for kk in range(3):
                f.box((tx + s * 0.6, dy - 0.335, 0.15 + kk * 0.23), (0.34, 0.01, 0.18), M["wood"], bevel=0.01)
                f.box((tx + s * 0.6, dy - 0.345, 0.17 + kk * 0.23), (0.08, 0.012, 0.012), M["brass"])
        f.box((tx + 0.4, dy + 0.15, 0.82), (0.05, 0.05, 0.08), M["brass"])
        f.lathe((tx + 0.4, dy + 0.15, 0.78), [(0.0, 0.0), (0.08, 0.0), (0.07, 0.02), (0.015, 0.03), (0.015, 0.36), (0.0, 0.36)], M["brass"], n=10)
        f.box((tx + 0.4, dy + 0.1, 1.14), (0.36, 0.14, 0.08), M["lamp_glass_lit"], bevel=0.03)
        f.box((tx - 0.2, dy - 0.1, 0.79), (0.42, 0.3, 0.02), M["paper"], rot=(0, 0, rnd.uniform(-10, 10)))
        f.box((tx - 0.25, dy + 0.18, 0.8), (0.3, 0.2, 0.04), M["iron"])
        for q in range(3):
            f.lathe((tx + 0.05 + q * 0.06, dy + 0.2, 0.78), [(0.0, 0.0), (0.02, 0.0), (0.012, 0.02), (0.008, 0.07), (0.015, 0.09), (0.0, 0.1)], M["wood"], n=6)
        chair(f, tx - 0.95, cy1 + 0.75, 35, M, office=True)
    # the pigeonhole wall behind the desks, with three clocks over it (the world's markets)
    py = 4.3
    f.box((cxm, py + 0.06, 1.25), (cx1 - cx0, 0.16, 2.5), M["wood"])
    for i in range(int((cx1 - cx0) / 0.32)):
        for j in range(6):
            x = cx0 + 0.16 + i * 0.32
            z = 1.0 + j * 0.24
            f.box((x, py - 0.03, z), (0.28, 0.04, 0.2), M["reveal"])
            if rnd.random() < 0.6:
                f.box((x + rnd.uniform(-0.04, 0.04), py - 0.035, z - 0.06), (0.22, 0.03, rnd.uniform(0.03, 0.1)), M["paper"])
    for i in range(int((cx1 - cx0) / 0.32) + 1):
        f.box((cx0 + i * 0.32, py - 0.04, 1.6), (0.03, 0.06, 1.48), M["wood"])
    for j in range(7):
        f.box((cxm, py - 0.04, 0.88 + j * 0.24), (cx1 - cx0, 0.06, 0.03), M["wood"])
    f.box((cxm, py - 0.05, 0.42), (cx1 - cx0, 0.04, 0.84), M["wood"])
    for i in range(10):                                                                           # drawers below
        f.box((cx0 + 0.55 + i * 1.1, py - 0.08, 0.42), (1.0, 0.03, 0.7), M["wood"], bevel=0.015)
        f.box((cx0 + 0.55 + i * 1.1, py - 0.1, 0.62), (0.16, 0.02, 0.03), M["brass"])
    f.box((cxm, py - 0.02, 2.58), (cx1 - cx0 + 0.1, 0.24, 0.16), M["wood"], bevel=0.02)
    for k, (city, hh) in enumerate((("LONDON", 1), ("NEW  YORK", 20), ("TOKYO", 10))):
        x = cxm - 3.6 + k * 3.6
        K.clock_face(f.sub((x, py - 0.1, 3.05), 0), (0, 0, 0), 0.24, M, hh, 41, numerals=False, rim_m=M["brass"])
        f.text(city, (x, py - 0.12, 2.68), 0.08, M["brass"], kind="serif")


def build_back_office(g, M):
    """Behind the pigeonholes, where the camera sees over them: the managers' desks, filing cabinets,
    the big wall safe, bookcases along the back wall."""
    f = K.L(g)
    for (x, y) in ((14.5, 7.8), (20.0, 7.8)):
        f.box((x, y, 0.76), (1.8, 0.9, 0.05), M["wood_top"], bevel=0.015)
        f.box((x, y, 0.38), (1.7, 0.85, 0.72), M["wood"], bevel=0.02)
        f.lathe((x + 0.6, y + 0.2, 0.78), [(0.0, 0.0), (0.08, 0.0), (0.015, 0.03), (0.015, 0.4), (0.0, 0.4)], M["brass"], n=8)
        f.box((x + 0.6, y + 0.15, 1.18), (0.4, 0.16, 0.08), M["lamp_glass_lit"], bevel=0.03)
        f.box((x - 0.3, y, 0.8), (0.5, 0.35, 0.02), M["felt"])
        chair(f, x, y + 0.75, 0, M)
    for k in range(5):                                                                              # filing cabinets
        x = 13.0 + k * 0.6
        f.box((x, 14.9, 0.7), (0.56, 0.7, 1.4), M["iron_mid"], bevel=0.015)
        for d in range(4):
            f.box((x, 14.54, 0.2 + d * 0.34), (0.5, 0.02, 0.3), M["iron_mid"], bevel=0.01)
            f.box((x, 14.52, 0.3 + d * 0.34), (0.14, 0.02, 0.03), M["chrome"])
    # the wall safe: a steel door in a frame, a wheel and a dial
    sx = 21.6
    f.box((sx, 15.55, 1.2), (1.6, 0.5, 2.4), M["vault_steel"], bevel=0.04)
    f.box((sx, 15.28, 1.2), (1.3, 0.06, 2.0), M["vault_steel"], bevel=0.03)
    f.torus((sx + 0.25, 15.22, 1.25), 0.18, 0.02, M["chrome"], axis="y", n1=16, n2=4)
    for a in range(3):
        f.beam((sx + 0.25, 15.22, 1.25), (sx + 0.25 + 0.18 * math.cos(a * 2.09), 15.22, 1.25 + 0.18 * math.sin(a * 2.09)), 0.02, 0.02, M["chrome"])
    f.cyl((sx - 0.25, 15.22, 1.5), 0.08, 0.04, M["chrome"], axis="y", n=16)
    for z in (0.5, 1.9):
        f.box((sx - 0.68, 15.25, z), (0.06, 0.1, 0.3), M["iron"])
    # bookcases with ledgers along the back wall's dado
    for bx in (2.0, 25.0 - 1.2):
        if bx > 20:
            continue
        f.box((bx, 15.55, 1.1), (1.8, 0.4, 2.2), M["wood"])
        for j in range(5):
            z = 0.2 + j * 0.42
            f.box((bx, 15.38, z), (1.7, 0.36, 0.03), M["wood"])
            x = bx - 0.8
            while x < bx + 0.78:
                w = rnd.uniform(0.04, 0.08)
                h = rnd.uniform(0.26, 0.36)
                f.box((x + w / 2, 15.42, z + h / 2 + 0.02), (w, 0.26, h), M["leather"] if rnd.random() < 0.6 else M["paper_dark"])
                x += w + 0.006


def build_corridor_and_vault(g, M):
    f = K.L(g)
    # the low ceiling, the walls of the corridor
    f.box(((LOBBY_X1 + X1) / 2, (YF + YB) / 2, VAULT_TOP + 0.1), (X1 - LOBBY_X1, YB - YF, 0.2), M["stone_dark"])
    f.box((LOBBY_X1 + 0.2, (2.6 + YB) / 2, VAULT_TOP / 2), (0.4, YB - 2.6, VAULT_TOP), M["stone_w"])
    f.box(((LOBBY_X1 + VAULT_X) / 2, 2.95, VAULT_TOP / 2), (VAULT_X - LOBBY_X1, 0.3, VAULT_TOP), M["stone_in"])
    f.box((LOBBY_X1 + 0.2, YF + 0.2, (VAULT_TOP + H) / 2), (0.4, 0.4, H - VAULT_TOP), M["stone_w"])
    f.box((LOBBY_X1 + 0.2, (YF + 2.6) / 2, (VAULT_TOP + H) / 2), (0.4, 2.6 - YF, H - VAULT_TOP), M["stone_w"])
    fc = K.L(g, (0, 2.8, 0), 0)                                                                    # the corridor's back wall, inside
    fc.box(((LOBBY_X1 + VAULT_X) / 2 + 0.2, -0.03, 0.6), (VAULT_X - LOBBY_X1 - 0.4, 0.06, 1.2), M["m_dark_wall"])
    fc.box(((LOBBY_X1 + VAULT_X) / 2 + 0.2, -0.07, 1.22), (VAULT_X - LOBBY_X1 - 0.4, 0.1, 0.06), M["brass"])
    K.cornice(fc, LOBBY_X1 + 0.4, VAULT_X - 0.35, VAULT_TOP - 0.42, M["trim"], depth=0.22, returns=False)
    fc.box((30.95, -0.02, 2.75), (2.8, 0.04, 0.34), M["brass"], bevel=0.01)
    fc.box((30.95, -0.04, 2.75), (2.7, 0.01, 0.26), M["ink"])
    fc.text("AUTHORISED  PERSONS  ONLY", (30.95, -0.05, 2.75), 0.12, M["brass"], kind="serif")
    # the gate's reader and buzzer: a glowing panel on a steel plate, a keypad, a lamp
    fc.box((27.9, -0.03, 1.35), (0.22, 0.06, 0.34), M["iron"], bevel=0.015)
    fc.box((27.9, -0.065, 1.42), (0.15, 0.01, 0.13), M["scanner"])
    for r_ in range(3):
        for c_ in range(3):
            fc.box((27.85 + c_ * 0.05, -0.065, 1.27 - r_ * 0.04), (0.035, 0.012, 0.03), M["chrome"])
    fc.cyl((27.9, -0.07, 1.56), 0.012, 0.012, M["led"], axis="y", n=6)
    fc.lathe((27.9, -0.06, 1.75), [(0.0, 0.0), (0.06, 0.0), (0.06, 0.04), (0.0, 0.08)], M["iron"], n=10, axis="y")   # the buzzer
    # a fire extinguisher on its bracket, an alarm bell, conduit along the top
    fc.lathe((27.0, -0.12, 0.3), [(0.0, 0.0), (0.08, 0.0), (0.08, 0.5), (0.06, 0.56), (0.02, 0.6), (0.0, 0.6)], M["hydrant"], n=12)
    fc.box((27.0, -0.13, 0.95), (0.06, 0.1, 0.06), M["iron"])
    fc.beam((27.03, -0.14, 0.92), (27.12, -0.22, 0.6), 0.02, 0.02, M["tyre"])
    fc.box((27.0, -0.02, 1.6), (0.3, 0.02, 0.4), M["white_paint"])
    fc.text("FIRE", (27.0, -0.035, 1.66), 0.08, M["ink"], kind="sign")
    fc.lathe((31.6, -0.06, 3.2), [(0.0, 0.0), (0.14, 0.0), (0.13, 0.06), (0.0, 0.08)], M["hydrant"], n=16, axis="y")
    fc.box((31.6, -0.02, 3.2), (0.08, 0.04, 0.08), M["iron"])
    for z in (3.48, 3.56):
        fc.cyl(((LOBBY_X1 + VAULT_X) / 2, -0.06, z), 0.02, VAULT_X - LOBBY_X1 - 0.6, M["iron_mid"], axis="x", n=6)
    fc.box((29.6, -0.04, 3.1), (0.6, 0.04, 0.3), M["iron_mid"])                                     # a vent
    for k in range(6):
        fc.box((29.6, -0.065, 2.98 + k * 0.045), (0.56, 0.01, 0.015), M["reveal"])
    K.cctv(K.L(g), (VAULT_X - 0.45, 2.6, 3.4), -60, M)
    # ceiling strips with their diffusers
    for x in (27.5, 31.0):
        f.box((x, 0.3, VAULT_TOP - 0.04), (1.6, 0.3, 0.08), M["iron_mid"], bevel=0.01)
        f.box((x, 0.3, VAULT_TOP - 0.085), (1.4, 0.18, 0.02), M["strip"])

    # the barred gate: posts, head, a transom grille above; the leaf swung open against the wall
    for y in (YF + 0.15, 2.55):
        f.box((GATE_X, y, VAULT_TOP / 2 - 0.1), (0.14, 0.14, VAULT_TOP - 0.2), M["steel"], bevel=0.015)
        for z in (0.4, 1.6, 2.8):
            f.cyl((GATE_X + 0.1, y, z), 0.035, 0.14, M["steel_mid"], n=8)                           # hinge knuckles
    f.box((GATE_X, (YF + 2.6) / 2, VAULT_TOP - 0.25), (0.14, 2.6 - YF, 0.14), M["steel"], bevel=0.015)
    y = YF + 0.3
    while y < 2.5:
        f.cyl((GATE_X, y, (VAULT_TOP - 0.18 + VAULT_TOP) / 2 - 0.05), 0.015, 0.2, M["steel"], n=6)
        y += 0.16
    f.box((GATE_X, 0.3, 0.01), (0.2, 4.4, 0.02), M["steel_mid"])                                    # the floor track
    lx0 = GATE_X + 0.15
    k = 0
    x = lx0
    while x < GATE_X + 2.1:                                                                         # the open leaf
        f.cyl((x, 2.45, 1.6), 0.018, 3.0, M["steel"], n=6)
        f.lathe((x, 2.45, 3.1), [(0.0, 0.0), (0.03, 0.0), (0.0, 0.1)], M["steel"], n=6)
        x += 0.15
        k += 1
    for z in (0.15, 1.2, 2.2, 3.05):
        f.box((lx0 + 0.95, 2.45, z), (1.95, 0.06, 0.05), M["steel"])
        xx = lx0
        while xx < lx0 + 1.9:
            f.cyl((xx, 2.41, z), 0.012, 0.01, M["steel_mid"], axis="y", n=6)
            xx += 0.15
    f.box((lx0 + 1.95, 2.42, 1.25), (0.12, 0.14, 0.3), M["steel_mid"], bevel=0.015)               # the lock box
    f.cyl((lx0 + 1.95, 2.34, 1.28), 0.02, 0.02, M["keyhole"], axis="y", n=8)
    f.beam((lx0 + 0.05, 2.47, 0.2), (lx0 + 1.9, 2.47, 3.0), 0.04, 0.03, M["steel"])                # a diagonal brace

    # the vault wall: steel cladding, a round opening the door fits
    fv = K.L(g, (VAULT_X, 0, 0), 90)                                                                # u = y, through the wall
    R, cu, cw = 1.55, 0.3, 1.55
    K.wall(fv, YF, VAULT_BACK, 0.0, VAULT_TOP, 0.7, M["vault_steel"], [(cu, 2 * R, 0.0, 2 * cw, False)])
    for qs, qw in ((-1, 1), (1, 1), (-1, -1), (1, -1)):
        a0 = 0.0 if qs > 0 else math.pi
        a1 = (math.pi / 2 if qw > 0 else -math.pi / 2) if qs > 0 else (math.pi / 2 if qw > 0 else 1.5 * math.pi)
        pts = [(cu + qs * R, cw + qw * cw)] + [(cu + math.cos(a0 + (a1 - a0) * k / 10) * R, cw + math.sin(a0 + (a1 - a0) * k / 10) * R) for k in range(11)]
        fv.wallshape(pts, -0.35, 0.35, M["vault_steel"])
    # the frame: a massive ring through the wall, its lip on the corridor face, the bolt sockets
    fv.tube((cu, 0.0, cw), [(R, -0.37), (R + 0.28, -0.37), (R + 0.28, -0.32), (R + 0.06, -0.3), (R + 0.06, 0.37), (R, 0.37)], M["steel_mid"], n=48, axis="y")
    for k in range(24):
        a = k / 24 * math.tau
        fv.cyl((cu + math.cos(a) * (R + 0.18), 0.38, cw + math.sin(a) * (R + 0.18)), 0.03, 0.03, M["chrome"], axis="y", n=6)
    for k in range(16):
        a = k / 16 * math.tau
        fv.cyl((cu + math.cos(a) * (R + 0.02), -0.2, cw + math.sin(a) * (R + 0.02)), 0.065, 0.06, M["keyhole"], axis="y", n=8)
    # the corridor face: a riveted steel surround, the maker's plate
    ff = K.L(g, (VAULT_X - 0.35, 0, 0), -90)                                                        # u = -y, out = -x
    for k in range(9):
        ff.box((-(YF + 0.25 + k * 0.32), -0.02, VAULT_TOP - 0.25), (0.28, 0.04, 0.4), M["vault_steel"], bevel=0.01)
    for u in (-(YF + 0.2), -(VAULT_BACK - 0.2)):
        z = 0.2
        while z < VAULT_TOP - 0.1:
            ff.cyl((u, -0.04, z), 0.02, 0.02, M["chrome"], axis="y", n=6)
            z += 0.25
    ff.box((-(cu), -0.05, VAULT_TOP - 0.25), (1.4, 0.04, 0.26), M["brass"], bevel=0.01)
    ff.text("HALLAM  &  STERLING  ·  1911", (-cu, -0.074, VAULT_TOP - 0.25), 0.07, M["ink"], kind="serif")

    # the round door, standing open into the vault, hinged at the back: a stepped steel drum, its
    # locking bolts out of the rim, the spoked wheel, the dial, the time-lock plate
    dx, dy, dz = VAULT_X + 1.3, 1.95, 1.55
    fd = K.L(g, (dx, dy, dz), 0)                                                                    # its face looks at the camera (-y)
    fd.lathe((0, 0, 0), [(0.0, -0.3), (1.38, -0.3), (1.38, 0.2), (1.5, 0.22), (1.5, 0.3), (0.0, 0.3)], M["vault_steel"], n=48, axis="y")
    for rr in (1.32, 1.18):
        fd.torus((0, -0.305, 0), rr, 0.022, M["steel_mid"], axis="y", n1=40, n2=4)
    fd.lathe((0, 0, 0), [(0.0, 0.3), (1.05, 0.3), (0.9, 0.36), (0.0, 0.36)], M["vault_steel"], n=40, axis="y")
    fd.torus((0, -0.365, 0), 0.62, 0.02, M["steel_mid"], axis="y", n1=32, n2=4)
    for k in range(16):                                                                             # the locking bolts
        a = k / 16 * math.tau
        for yy in (-0.12, 0.1):
            fd.cyl((math.cos(a) * 1.5, yy, math.sin(a) * 1.5), 0.065, 0.32, M["chrome"], rot=(0, 90 - math.degrees(a), 0), n=10)
    # the wheel: hub, five spokes with handles, the rim
    fd.cyl((0, -0.4, 0), 0.12, 0.1, M["chrome"], axis="y", n=16)
    fd.torus((0, -0.46, 0), 0.5, 0.035, M["chrome"], axis="y", n1=32, n2=6)
    for k in range(5):
        a = k / 5 * math.tau + 0.3
        fd.beam((0, -0.46, 0), (math.cos(a) * 0.5, -0.46, math.sin(a) * 0.5), 0.04, 0.04, M["chrome"])
        fd.lathe((math.cos(a) * 0.68, -0.46, math.sin(a) * 0.68), [(0.0, -0.1), (0.03, -0.1), (0.04, 0.05), (0.0, 0.1)], M["chrome"], n=8, rot=(0, 90 - math.degrees(a), 0))
    # the combination dial and its ring of ticks, above the wheel
    fd.lathe((0, -0.36, 0.72), [(0.0, 0.0), (0.16, 0.0), (0.15, 0.05), (0.06, 0.08), (0.0, 0.08)], M["chrome"], n=24, axis="y")
    for k in range(40):
        a = k / 40 * math.tau
        fd.box((math.sin(a) * 0.2, -0.37, 0.72 + math.cos(a) * 0.2), (0.008, 0.006, 0.03 if k % 5 else 0.05), M["ink"], rot=(0, math.degrees(a), 0))
    # the time-lock plate: three small clocks behind glass, and the maker's name in an arc
    fd.box((0, -0.38, -0.7), (0.8, 0.04, 0.28), M["chrome"], bevel=0.02)
    for k in range(3):
        fd.cyl((-0.25 + k * 0.25, -0.405, -0.7), 0.09, 0.01, M["clock_face"], axis="y", n=16)
        fd.box((-0.25 + k * 0.25, -0.412, -0.67), (0.008, 0.004, 0.07), M["ink"], rot=(0, 40 * k, 0))
    fd.text("MERIDIAN  TRUST", (0, -0.31, 1.2), 0.1, M["brass"], kind="serif")
    # the hinge: a barrel on the frame, two arms to the door
    f.cyl((VAULT_X + 0.5, 2.15, 1.55), 0.2, 2.6, M["steel_mid"], n=16)
    for z in (0.7, 2.4):
        f.box((VAULT_X + 0.95, 2.1, z), (1.0, 0.3, 0.32), M["vault_steel"], bevel=0.03)
        f.cyl((VAULT_X + 0.5, 2.15, z), 0.24, 0.36, M["vault_steel"], n=16)
    # a CCTV dome on the vault ceiling
    f.lathe((VAULT_X + 1.2, -1.4, VAULT_TOP - 0.02), [(0.0, -0.14), (0.1, -0.12), (0.14, -0.05), (0.15, 0.0), (0.0, 0.0)], M["reveal"], n=16)


def box_door(f, u, w, wd, ht, M, num, front=-0.0, proud=False, two_keys=True):
    """One deposit box's door (local frame on the wall's face): the door, its hinges, the two
    keyholes (the bank's guard key and the renter's), a brass plate with the number."""
    v = front - (0.012 if proud else 0.0)
    f.box((u, v + 0.015, w), (wd - 0.02, 0.03, ht - 0.02), M["box_steel"], bevel=0.006)
    f.box((u - wd / 2 + 0.02, v - 0.004, w + ht * 0.28), (0.012, 0.012, 0.05), M["chrome"])
    f.box((u - wd / 2 + 0.02, v - 0.004, w - ht * 0.28), (0.012, 0.012, 0.05), M["chrome"])
    for ku in ((0.08, 0.17) if two_keys else (0.13,)):
        f.cyl((u + ku, v - 0.003, w), 0.022, 0.008, M["brass"], axis="y", n=8)
        f.box((u + ku, v - 0.008, w), (0.008, 0.004, 0.026), M["keyhole"])
    f.box((u - 0.08, v - 0.003, w + 0.02), (0.15, 0.008, 0.065), M["brass"])
    f.text(str(num), (u - 0.08, v - 0.009, w + 0.02), 0.05, M["ink"], kind="body")


def build_box_room(g, M):
    """The safe-deposit room: the wall of boxes (eleven rows of twenty-three, numbered so that 417 is
    where the story needs it), more boxes on the end wall, big lockers below, the viewing table, a
    library ladder on its rail, the cold strip lights in steel housings."""
    f = K.L(g)
    f.box(((VAULT_X + X1) / 2, VAULT_BACK + 0.15, VAULT_TOP / 2), (X1 - VAULT_X, 0.3, VAULT_TOP), M["stone_dark"])
    f.box((X1 - 0.2, (YF + VAULT_BACK) / 2, VAULT_TOP / 2), (0.2, VAULT_BACK - YF, VAULT_TOP), M["stone_dark"])
    fw = K.L(g, (0, VAULT_BACK, 0), 0)                                                              # the wall of boxes, its face
    xa, xb = VAULT_X + 1.0, X1 - 0.5
    fw.box(((xa + xb) / 2, -0.01, VAULT_TOP / 2), (xb - xa + 0.1, 0.02, VAULT_TOP), M["steel_mid"])
    cols = int(round((xb - xa) / 0.5))
    for r in range(11):                                                                            # row 6 sits at z 1.45
        zc = 3.25 - 0.3 * r
        for c in range(cols):
            num = 272 + r * cols + c + 1
            if r >= 9 and c % 2 == 1:
                continue
            if r >= 9:                                                                             # big lockers below
                if r == 10:
                    continue
                box_door(fw, xa + c * 0.5 + 0.5, 0.4, 1.0, 0.6, M, f"L{c // 2 + 1}", front=-0.055)
                continue
            box_door(fw, xa + c * 0.5 + 0.25, zc, 0.5, 0.3, M, num, front=-0.055, proud=(num == 417))
    for c in range(cols + 1):
        fw.box((xa + c * 0.5, -0.06, 1.75), (0.012, 0.012, 3.1), M["iron"])
    fw.box(((xa + xb) / 2, -0.07, 3.48), (xb - xa + 0.2, 0.14, 0.12), M["steel_mid"], bevel=0.02)
    fw.box(((xa + xb) / 2, -0.06, 0.05), (xb - xa + 0.2, 0.12, 0.1), M["steel_mid"])
    # the end wall: more boxes (seen down the room)
    fe = K.L(g, (X1 - 0.3, 0, 0), -90)                                                              # u = -y, out = -x
    for r in range(11):
        zc = 3.25 - 0.3 * r
        for c in range(8):
            u = -(VAULT_BACK - 0.6 - c * 0.5)
            if u > -(YF + 0.6):
                continue
            box_door(fe, u, zc, 0.5, 0.3, M, 525 + r * 8 + c + 1, front=-0.05)
    # the library ladder on its rail
    fw.cyl(((xa + xb) / 2, -0.25, 3.3), 0.02, xb - xa, M["brass"], axis="x", n=8)
    lx = 43.6
    for s in (-0.22, 0.22):
        fw.beam((lx + s, -0.26, 3.3), (lx + s, -0.9, 0.0), 0.05, 0.03, M["wood"])
    for k in range(1, 9):
        t = k / 9
        fw.box((lx, -0.26 - 0.64 * t, 3.3 - 3.3 * t), (0.44, 0.08, 0.025), M["wood"])
    # the viewing table (where the box is opened): steel frame, felt top in a brass edge, a chair
    tx, ty, tz = TABLE
    f.box((tx, ty, tz - 0.025), (2.0, 0.9, 0.05), M["iron_mid"], bevel=0.01)
    f.box((tx, ty, tz + 0.002), (1.86, 0.76, 0.006), M["felt"])
    for s in (-1, 1):
        f.box((tx, ty + s * 0.45, tz - 0.01), (2.02, 0.02, 0.04), M["brass"])
    for ddx in (-0.9, 0.9):
        for ddy in (-0.38, 0.38):
            f.box((tx + ddx, ty + ddy, (tz - 0.05) / 2), (0.05, 0.05, tz - 0.05), M["iron"])
        f.box((tx + ddx, ty, 0.15), (0.04, 0.76, 0.04), M["iron"])
    f.box((tx, ty, 0.15), (1.8, 0.04, 0.04), M["iron"])
    chair(f, tx + 0.2, ty + 0.85, 0, M)
    f.lathe((tx - 0.75, ty + 0.3, tz), [(0.0, 0.0), (0.08, 0.0), (0.015, 0.03), (0.015, 0.36), (0.0, 0.36)], M["chrome"], n=8)
    f.box((tx - 0.75, ty + 0.24, tz + 0.36), (0.34, 0.16, 0.08), M["lamp_glass_lit"], bevel=0.03)
    f.box((tx + 0.55, ty - 0.25, tz + 0.005), (0.24, 0.16, 0.004), M["paper"], rot=(0, 0, 10))
    # the strip lights in steel housings, a vent, the room's clock
    for x in (36.0, 40.5, 44.5):
        f.box((x, 0.4, VAULT_TOP - 0.04), (2.2, 0.32, 0.08), M["iron_mid"], bevel=0.01)
        f.box((x, 0.4, VAULT_TOP - 0.085), (2.0, 0.2, 0.02), M["strip"])
    for x in (35.0, 42.5):
        f.box((x, -1.2, VAULT_TOP - 0.01), (0.8, 0.5, 0.02), M["iron_mid"])
        for k in range(8):
            f.box((x - 0.35 + k * 0.1, -1.2, VAULT_TOP - 0.025), (0.03, 0.46, 0.01), M["reveal"])


# ---------------------------------------------------------------- the stone front (west) and its portico

def build_facade(g, M):
    fo = K.L(g, (X0 - 0.4, 0, 0), -90)                                                              # u = -y, out = -x
    # the dark plinth (not across the doorway: a level entrance)
    for (ya, yb) in ((YF, -DOOR_Y), (DOOR_Y, YB + 0.4)):
        fo.box((-(ya + yb) / 2, -0.08, 0.28), (yb - ya, 0.16, 0.56), M["plinth"], bevel=0.02)
    # the rusticated ground storey: chamfered blocks in courses, round the openings
    opens = [(-DOOR_Y - 0.35, DOOR_Y + 0.35, 0.0, DOOR_H + 0.75)] + [(y - 0.95, y + 0.95, 1.3, 7.4) for y in WEST_WIN]
    z, row = 0.56, 0
    while z < 4.4:
        y = YF + (0.6 if row % 2 else 0.0)
        while y < YB + 0.4:
            y1 = min(y + 1.2, YB + 0.4)
            ok = all(not (y1 > a and y < b and z + 0.5 > c and z < d) for a, b, c, d in opens)
            if ok and y1 - y > 0.15:
                fo.box((-(y + y1) / 2, -0.06, z + 0.24), (y1 - y - 0.04, 0.12, 0.46), M["rustic_w"], bevel=0.04)
            y = y1
        z += 0.5
        row += 1
    K.string_course(fo, -(YB + 0.4), -YF, 4.48, M["trim_w"], h=0.2, d=0.16)
    # the door: a moulded architrave with ears, a frieze with the bronze name, a cornice on consoles,
    # the transom grille above the game's glass doors
    for s in (-1, 1):
        fo.box((s * (DOOR_Y + 0.17), -0.1, DOOR_H / 2 + 0.05), (0.34, 0.2, DOOR_H + 0.1), M["trim_w"], bevel=0.02)
        fo.box((s * (DOOR_Y + 0.17), -0.16, DOOR_H / 2), (0.14, 0.06, DOOR_H), M["trim_w"])
        fo.box((s * (DOOR_Y + 0.42), -0.22, DOOR_H + 0.35), (0.2, 0.32, 0.6), M["trim_w"], bevel=0.04)   # consoles
        fo.cyl((s * (DOOR_Y + 0.42), -0.32, DOOR_H + 0.5), 0.1, 0.22, M["trim_w"], axis="x", n=10)
    fo.box((0.0, -0.1, DOOR_H + 0.3), (2 * DOOR_Y + 0.7, 0.2, 0.6), M["trim_w"], bevel=0.02)
    fo.text("MERIDIAN  TRUST", (0.0, -0.205, DOOR_H + 0.3), 0.3, M["brass"], depth=0.01, kind="serif")
    K.cornice(fo, -DOOR_Y - 0.6, DOOR_Y + 0.6, DOOR_H + 0.6, M["trim_w"], depth=0.42)
    tg = K.L(g, (X0 - 0.25, 0, 0), -90)
    tg.box((0.0, 0.0, 3.4), (2 * DOOR_Y, 0.04, 0.05), M["brass"])
    for k in range(7):
        u = -DOOR_Y + (k + 0.5) * (2 * DOOR_Y) / 7
        tg.torus((u, 0.0, 3.4 + 0.1), 0.08, 0.012, M["brass"], axis="y", n1=12, n2=4)
    tg.box((0.0, 0.0, DOOR_H - 0.02), (2 * DOOR_Y, 0.06, 0.06), M["brass"])
    # the mark carved over the doors (StoryBank's match cut): a recessed panel, a wreath, a ribbon
    shield(g, 0.0, 5.3, 0.95, X0 - 0.42, M["stone"], hole=M["stone_dark"])
    g.box((X0 - 0.43, 0.0, 5.3), (0.02, 2.4, 2.4), M["stone_dark"])
    fm = K.L(g, (X0 - 0.42, 0, 0), -90)
    for s in (-1, 1):
        fm.box((s * 1.28, -0.04, 5.3), (0.16, 0.1, 2.72), M["trim_w"], bevel=0.02)
        fm.box((0.0, -0.04, 5.3 + s * 1.28), (2.72, 0.1, 0.16), M["trim_w"], bevel=0.02)
        laurel(fm, s * 0.45, 4.25, s * 1.05, 5.9, M["trim_w"], n=8)
    fm.box((0.0, -0.06, 4.28), (1.4, 0.06, 0.22), M["trim_w"], bevel=0.02)
    fm.text("EST.  1887", (0.0, -0.095, 4.28), 0.13, M["stone_dark"], kind="serif")
    # windows between the columns: arched, framed, barred below, keystones
    for y in WEST_WIN:
        K.window(fo, -y, 1.6, 6.4, 1.5, M, M["frame_dark"], M["glass_ext"], M["trim_w"], head="arch", cols=2, rows=4, grille=2.2)
    # lanterns either side of the door, the brass plates
    for s in (-1, 1):
        u = s * 1.72
        fo.box((u, -0.04, 2.95), (0.14, 0.08, 0.4), M["iron"], bevel=0.02)
        fo.beam((u, -0.04, 2.9), (u, -0.4, 3.0), 0.04, 0.04, M["iron"])
        fo.lathe((u, -0.42, 2.66), [(0.0, 0.0), (0.05, 0.0), (0.12, 0.08), (0.14, 0.32), (0.1, 0.38), (0.0, 0.38)], M["lamp_glass"], n=6)
        fo.lathe((u, -0.42, 3.03), [(0.0, 0.0), (0.17, 0.0), (0.06, 0.14), (0.0, 0.2)], M["iron"], n=6)
    fo.box((-1.68, -0.02, 1.55), (0.5, 0.04, 0.7), M["brass"], bevel=0.02)
    for k, (t, sz) in enumerate((("MERIDIAN", 0.075), ("TRUST", 0.075), ("BANKERS", 0.045), ("EST.  1887", 0.045))):
        fo.text(t, (-1.68, -0.045, 1.76 - k * 0.13), sz, M["ink"], kind="serif")
    fo.box((1.65, -0.02, 1.55), (0.4, 0.04, 0.55), M["brass"], bevel=0.02)
    for k, (t, sz) in enumerate((("HOURS", 0.06), ("MON - FRI", 0.045), ("9:00 - 16:00", 0.045))):
        fo.text(t, (1.65, -0.045, 1.7 - k * 0.12), sz, M["ink"], kind="serif")
    # the upper wall: a band, the main cornice over the portico roof line, a balustrade on the roof
    K.cornice(fo, -(YB + 0.4), -(YF - 0.4), H - 0.4, M["trim_w"], depth=0.55, modillions=True)
    rb = K.L(g, (X0 - 0.6, 0, H + 0.4), -90)
    rb.box((-(YF + YB) / 2, 0.0, 0.1), (YB - YF + 0.8, 0.5, 0.2), M["trim_w"])
    rb.box((-(YF + YB) / 2, 0.0, 0.9), (YB - YF + 0.8, 0.45, 0.14), M["trim_w"], bevel=0.02)
    y = YF + 0.2
    while y < YB + 0.2:
        rb.lathe((-y, 0.0, 0.2), [(0.09, 0.0), (0.09, 0.05), (0.06, 0.1), (0.11, 0.3), (0.07, 0.5), (0.05, 0.55), (0.08, 0.62), (0.08, 0.63), (0.0, 0.63)], M["trim_w"], n=8)
        y += 0.24
    rf = K.L(g, (0, YF - 0.4, H + 0.4), 0)                                                          # along the front's roof edge
    rf.box((X1 / 2, 0.0, 0.45), (X1 + 0.4, 0.4, 0.9), M["trim"], bevel=0.02)
    rf.box((X1 / 2, -0.02, 0.92), (X1 + 0.5, 0.48, 0.1), M["trim"], bevel=0.02)
    # a flagpole on the roof, the flag hanging still in the calm after the storm
    f = K.L(g)
    f.lathe((1.6, 6.0, H + 0.4), [(0.0, 0.0), (0.3, 0.0), (0.3, 0.3), (0.06, 0.4), (0.05, 6.0), (0.08, 6.05), (0.0, 6.15)], M["iron"], n=10)
    for k in range(6):
        f.box((1.6, 6.0 - 0.06 - k * 0.003, H + 5.7 - k * 0.02), (0.02, 0.02 + k * 0.004, 0.04), M["white_band"])
    for k in range(7):                                                                              # the flag hangs limp, in folds
        f.box((1.6 - 0.1 - k * 0.09, 6.0 + (0.04 if k % 2 else -0.04), H + 5.4 - k * 0.04), (0.11, 0.015, 1.6 - k * 0.08), M["cloth_a"] if k != 3 else M["cloth_b"],
              rot=(0, 0, 40 if k % 2 else -40))


def build_portico(g, M):
    f = K.L(g)
    # the stylobate (behind the lane: y from 1.6) with a step along its front
    f.box((-2.15, 7.2, 0.15), (3.5, 11.2, 0.3), M["trim"], bevel=0.02)
    f.box((-3.95, 7.2, 0.075), (0.3, 11.2, 0.15), M["trim"], bevel=0.02)
    for y in (2.2, 5.4, 8.6, 11.8):
        column(f, X0 - 3.0, y, 0.3, 7.9, 0.48, M["trim"], M["trim"], flutes=24)
    # the entablature: three fasciae, the frieze with the name, the cornice with dentils and modillions
    ey0, ey1 = YF - 1.6, YB - 0.4
    ex0, ex1 = X0 - 3.4, X0 + 0.2
    f.box(((ex0 + ex1) / 2, (ey0 + ey1) / 2, 8.75), (ex1 - ex0, ey1 - ey0, 1.1), M["trim"])
    fe = K.L(g, (ex0, 0, 0), -90)
    fs = K.L(g, (0, ey0, 0), 0)                                                                    # its end, facing the road
    for fr, a, b in ((fe, -ey1, -ey0), (fs, ex0, ex1)):
        for k, (z, d) in enumerate(((8.3, 0.02), (8.44, 0.04), (8.58, 0.06))):
            fr.box(((a + b) / 2, -d / 2, z), (b - a + 2 * d, d, 0.14), M["trim"])
        fr.box(((a + b) / 2, -0.07, 8.68), (b - a + 0.14, 0.07, 0.06), M["trim"])
        K.cornice(fr, a, b, 9.3, M["trim"], depth=0.5, modillions=True)
    fe.text("MERIDIAN   TRUST", (-(ey0 + ey1) / 2, -0.03, 9.0), 0.5, M["stone_dark"], depth=0.02, kind="serif", spacing=1.2)
    for u in (-(ey0 + 1.2), -(ey1 - 1.2)):
        fe.lathe((u, 0.0, 9.0), [(0.0, 0.0), (0.18, 0.0), (0.2, 0.03), (0.12, 0.06), (0.0, 0.07)], M["trim"], n=12, axis="y")
    # the soffit under the portico: coffers between the beams over the columns
    for y in (2.2, 5.4, 8.6, 11.8):
        f.box((-1.6, y, 8.12), (3.6, 0.5, 0.16), M["trim"])
    y = ey0 + 0.6
    while y < ey1:
        f.box((-1.6, y, 8.1), (2.4, 0.9, 0.04), M["stone_dark"])
        y += 1.6
    # the pediment: the raking cornices, the tympanum with the mark in a wreath, acroteria
    py0, py1, pz0, pz1 = ey0, ey1, 9.86, 12.6
    pc = (py0 + py1) / 2
    fp = K.L(g, (ex0, 0, 0), -90)
    fp.wallshape([(-py1, 0.0), (-py0, 0.0), (-pc, pz1 - pz0)], -0.02, 3.4, M["trim_w"], c=(0, 0, pz0))
    fp.wallshape([(-py1 + 0.6, 0.12), (-py0 - 0.6, 0.12), (-pc, pz1 - pz0 - 0.4)], -0.06, 0.0, M["stone_dark"], c=(0, 0, pz0))
    slope = math.atan2(pz1 - pz0, (py1 - py0) / 2)
    for s in (-1, 1):
        a = (-pc + s * (py1 - py0) / 2, pz0)
        b = (-pc, pz1)
        for k, (dz, dd, hh) in enumerate(((0.05, 0.25, 0.14), (0.2, 0.4, 0.12), (0.32, 0.5, 0.08))):
            fp.beam((a[0], -dd / 2, a[1] + dz), (b[0], -dd / 2, b[1] + dz), hh, dd, M["trim_w"])
    shield(g, -0.0 + pc, pz0 + 1.0, 0.75, ex0 - 0.04, M["trim"], hole=M["stone_dark"])
    fpm = K.L(g, (ex0 - 0.05, 0, 0), -90)
    for s in (-1, 1):
        laurel(fpm, -pc + s * 0.75, pz0 + 0.3, -pc + s * 3.6, pz0 + 0.55, M["trim_w"], n=12)
    fpm.box((-pc, -0.03, pz0 + 0.18), (2.2, 0.06, 0.18), M["trim_w"])
    fpm.text("MDCCCLXXXVII", (-pc, -0.065, pz0 + 0.18), 0.12, M["stone_dark"], kind="serif")
    for (yy, zz) in ((py0, pz0 + 0.4), (py1, pz0 + 0.4), (pc, pz1 + 0.35)):
        f.lathe((ex0 + 0.2, yy, zz), [(0.0, 0.0), (0.22, 0.0), (0.22, 0.08), (0.12, 0.14), (0.24, 0.42), (0.16, 0.6), (0.06, 0.66), (0.08, 0.72), (0.0, 0.8)], M["trim"], n=12)
    # its roof, pitched back to the wall
    for s in (-1, 1):
        a = Vector((ex0 - 0.3, pc + s * (py1 - py0) / 2 + s * 0.3, pz0 + 0.3))
        b = Vector((ex0 - 0.3, pc, pz1 + 0.35))
        mid = (a + b) / 2
        ang = math.degrees(math.atan2(b.z - a.z, (b.y - a.y)))
        f.box((X0 - 1.6, mid.y, mid.z), (3.9, (a - b).length, 0.16), M["stone_dark"], rot=(ang, 0, 0))


# ---------------------------------------------------------------- the bank's long side (cut away)

def build_front(coll, M):
    """The bank's long side facing the road (y = YF): stone with tall windows, a rusticated base,
    pilasters and a cornice outside; inside a dado and the windows' frames. The game cuts it away
    whenever the camera is outside it."""
    g = S.Group("BankFront")
    yf = YF - 0.2
    fw = K.L(g, (0, yf, 0), 0)
    K.wall(fw, X0, X1, 0.0, H, 0.4, M["stone_y"], [(x, 2.6, 2.4, 8.8, False) for x in BACK_WIN])
    fo = K.L(g, (0, yf - 0.2, 0), 0)                                                                # outside face
    fo.box((X1 / 2, -0.06, 1.2), (X1, 0.12, 2.4), M["stone_y"], bevel=0.02)
    for z in (0.6, 1.2, 1.8):
        fo.box((X1 / 2, -0.125, z), (X1, 0.012, 0.03), M["reveal"])
    for x in BACK_WIN:
        K.window(fo, x, 2.4, 8.8, 2.6, M, M["frame_dark"], M["glass_ext"], M["trim"], head="flat", cols=3, rows=5, grille=2.0, reveal=0.2)
    for x in (1.0, 7.0, 13.0, 19.0, 25.0):
        K.pilaster(fo, x, 2.4, 9.8, 0.6, M["trim"])
    K.cornice(fo, X0, X1, H - 0.9, M["trim"], depth=0.5, modillions=True)
    fi = K.L(g, (0, YF, 0), 180)                                                                    # inside face (u = -x)
    fi.box((-LOBBY_X1 / 2, -0.03, 0.62), (LOBBY_X1, 0.06, 1.24), M["m_dark_wall"])
    fi.box((-LOBBY_X1 / 2, -0.07, 1.26), (LOBBY_X1, 0.1, 0.07), M["trim"])
    for x in BACK_WIN:
        fi.box((-x, 0.2, 5.6), (2.6, 0.04, 6.4), M["window_day"])
        for xx in (x - 0.65, x, x + 0.65):
            fi.box((-xx, 0.15, 5.6), (0.07, 0.07, 6.4), M["frame_dark"])
        for z in (3.5, 5.6, 7.7):
            fi.box((-x, 0.15, z), (2.6, 0.06, 0.05), M["frame_dark"])
        fi.box((-x, -0.1, 2.35), (2.9, 0.3, 0.1), M["m_dark_wall"], bevel=0.015)
    K.cornice(fi, -LOBBY_X1, X0, 10.1, M["trim"], depth=0.5)
    fi.box((-(LOBBY_X1 + X1) / 2, -0.03, 0.5), (X1 - LOBBY_X1 - 0.4, 0.06, 1.0), M["m_dark_wall"])
    return g.finish(coll)


def build_shell(coll, M):
    g = S.Group("BankShell")
    build_floor(g, M)
    build_lobby_walls(g, M)
    build_ceiling(g, M)
    build_lobby_furniture(g, M)
    build_counter(g, M)
    build_back_office(g, M)
    build_corridor_and_vault(g, M)
    build_box_room(g, M)
    build_facade(g, M)
    build_portico(g, M)
    return g.finish(coll)


# ---------------------------------------------------------------- the street outside

def shop_building(g, x0, x1, M, h, wall_m, name, kind, floors_m=None):
    """A building on the street's far side of the sidewalk (its face at y 8.2, facing the lane):
    the shopfront below (pilasters, fascia with its name, a display window over a stall riser, the
    door), upper floors of framed windows, string courses, a cornice, the roofline."""
    f = K.L(g, (0, 8.2, 0), 0)
    cx, w = (x0 + x1) / 2, x1 - x0
    sf = 3.8
    dwx = x1 - 1.4                                                                                  # the door
    win0, win1 = x0 + 0.5, dwx - 0.6
    ncol = max(1, int(w / 2.4))
    ups, z = [], sf + 0.9
    while z + 1.8 < h - 0.8:
        ups += [(x0 + (c + 0.5) * w / ncol, 1.1, z, z + 1.6, False) for c in range(ncol)]
        z += 2.9
    K.wall(f, x0, x1, 0.0, h, 0.4, wall_m, [((win0 + win1) / 2, win1 - win0, 0.6, 3.1, False), (dwx, 1.0, 0.0, 3.2, False)] + ups, v=0.2)
    f.box(((win0 + win1) / 2, 0.3, 1.85), (win1 - win0, 0.2, 2.5), M["reveal"])                       # the shop, dark inside
    f.box((dwx, 0.3, 1.6), (1.0, 0.2, 3.2), M["reveal"])
    # the shopfront
    for xx in (x0 + 0.2, x1 - 0.2):
        f.box((xx, -0.08, sf / 2), (0.4, 0.16, sf), M["sf_paint"], bevel=0.02)
        f.box((xx, -0.12, sf - 0.25), (0.5, 0.24, 0.3), M["sf_paint"], bevel=0.03)
    f.box((cx, -0.1, sf - 0.3), (w - 0.6, 0.2, 0.6), M["sf_paint"], bevel=0.02)                  # fascia
    f.box((cx, -0.21, sf - 0.3), (w - 1.0, 0.02, 0.44), M["ink"])
    f.text(name, (cx, -0.225, sf - 0.3), 0.34, M["white_paint"], depth=0.008, kind="serif" if kind != "pawn" else "sign")
    f.box((cx, -0.18, sf + 0.03), (w - 0.4, 0.3, 0.06), M["sf_paint"])
    f.box(((win0 + win1) / 2, -0.04, 0.3), (win1 - win0, 0.08, 0.6), M["sf_paint"], bevel=0.02)      # stall riser
    f.box(((win0 + win1) / 2, 0.06, 1.85), (win1 - win0, 0.03, 2.5), M["glass_shop"])
    for k in range(int((win1 - win0) / 1.2) + 1):
        f.box((win0 + k * (win1 - win0) / max(1, int((win1 - win0) / 1.2)), 0.0, 1.85), (0.06, 0.08, 2.5), M["sf_paint"])
    f.box(((win0 + win1) / 2, 0.0, 2.75), (win1 - win0, 0.08, 0.05), M["sf_paint"])
    f.box((dwx, 0.15, 1.3), (1.0, 0.04, 2.6), M["sf_paint"])
    f.box((dwx, 0.12, 1.55), (0.7, 0.02, 1.6), M["glass_shop"])
    f.box((dwx + 0.38, 0.1, 1.2), (0.03, 0.06, 0.25), M["brass"])
    f.box((dwx, 0.12, 3.0), (1.0, 0.02, 0.4), M["glass_shop"])
    for xx in (dwx - 0.55, dwx + 0.55):
        f.box((xx, 0.0, 1.5), (0.1, 0.12, 3.0), M["sf_paint"])
    # upper floors
    fl = floors_m or M["frame_pale"]
    z = sf + 0.9
    lvl = 0
    while z + 1.8 < h - 0.8:
        K.string_course(f, x0, x1, z - 0.55, M["trim"], h=0.14, d=0.08)
        for c in range(ncol):
            wx = x0 + (c + 0.5) * w / ncol
            blind = rnd.choice((None, None, 0.4, 0.7))
            K.window(f, wx, z, z + 1.6, 1.1, M, fl, M["glass_ext"], M["trim"], head="pediment" if lvl == 0 and kind == "cafe" else "flat",
                     cols=2, rows=2, blind=blind, keystone=(lvl % 2 == 0), box=(M["urn"] if (kind == "florist" and lvl == 0) else None))
        z += 2.9
        lvl += 1
    K.cornice(f, x0, x1, h - 0.5, M["trim"], depth=0.45)
    f.box((cx, 0.1, h + 0.35), (w, 0.3, 0.7), wall_m)
    for k in range(2):                                                                              # chimneys
        cxx = x0 + 1.0 + k * (w - 2.0)
        f.box((cxx, 1.2, h + 1.2), (0.7, 0.5, 2.4), M["brick"])
        for q in range(2):
            f.cyl((cxx - 0.15 + q * 0.3, 1.2, h + 2.55), 0.08, 0.3, M["iron_mid"], n=8)
    K.drainpipe(f, x1 - 0.15, 0.0, h, M["iron"])
    return f


def build_street(coll, M):
    """The street outside: the road, the sidewalk (level with the bank's floor), the shops up the
    block, lamp posts and street furniture, the parked black car, puddles, the far side, the city."""
    g = S.Group("BankStreet")
    f = K.L(g)
    # the road, the gutter of setts, the kerb (kerb stones with joints), the sidewalk's paving
    f.box((20.0, -16.0, -0.2), (140.0, 28.0, 0.1), M["road"])
    f.box((20.0, -2.65, -0.152), (140.0, 0.5, 0.01), M["setts"])
    f.box((20.0, -2.25, -0.08), (140.0, 0.3, 0.16), M["kerb"], bevel=0.03)
    x = -50.0
    while x < 90.0:
        f.box((x, -2.25, -0.08), (0.012, 0.32, 0.17), M["reveal"])
        x += 1.0
    f.box((-15.0, 3.0, -0.05), (32.0, 10.4, 0.1), M["paving"])
    for xx in (-29.0, -11.0, 7.0, 25.0, 43.0, 61.0):
        K.drain(f, xx, -2.6, M, w=0.8, d=0.36, z=-0.148)
    for (mx, my) in ((-20.0, -9.0), (2.0, -14.0), (-36.0, -15.5), (14.0, -8.5)):
        K.manhole(f, mx, my, M, z=-0.148)
    # road markings: a broken centre line, the edge lines, a zebra at the corner, a bus stop box
    for x in range(-58, 80, 6):
        f.box((x, -12.0, -0.146), (3.0, 0.15, 0.008), M["white_paint"])
    for yy in (-3.3, -20.7):
        f.box((10.0, yy, -0.146), (140.0, 0.1, 0.008), M["white_paint"])
    for k in range(26):                                                                             # the zebra
        f.box((-34.0, -3.0 - k * 1.0, -0.146), (3.2, 0.5, 0.008), M["white_paint"])
    for k in range(5):
        f.box((-24.0 + k * 1.2, -3.0, -0.146), (0.6, 0.1, 0.008), M["white_paint"])
    # puddles, irregular, the night's rain lying in the low places
    for (px, py, r) in ((-6.0, -1.0, 0.9), (-17.0, 0.6, 1.3), (-11.0, -6.0, 2.2), (4.0, -8.0, 1.8), (-25.0, -4.0, 1.6),
                        (-28.0, 2.4, 0.8), (-2.5, 5.5, 0.7), (-21.0, -10.5, 2.6), (8.0, -4.5, 1.2), (-14.0, -2.75, 0.9)):
        K.puddle(f, px, py, r, M["puddle"], z=(0.0 if py > -2.2 else -0.148), seed=px * 0.37)
    # the shops up the block
    cafe = shop_building(g, -30.0, -21.0, M, 14.0, M["stucco"], "CAFE  NOIR", "cafe", floors_m=M["frame_dark"])
    pawn = shop_building(g, -21.0, -13.5, M, 12.5, M["brick"], "PAWN  &  LOANS", "pawn")
    flor = shop_building(g, -13.5, -2.5, M, 16.0, M["brick_dark"], "LILY'S  FLOWERS", "florist")
    fs = K.L(g, (0, 8.2, 0), 0)
    # Cafe Noir: a striped scalloped awning with its name on the valance, letters on the glass, the terrace
    K_awning(fs, -29.6, -21.4, 3.3, 2.6, M, "CAFE  NOIR  ·  ESPRESSO")
    fs.text("OPEN  LATE", (-26.2, 0.035, 2.4), 0.2, M["white_paint"], kind="serif")
    fs.text("Espresso  ·  Croissants  ·  Wine", (-26.2, 0.035, 2.1), 0.12, M["white_paint"], kind="body")
    for k in range(3):
        tx = -28.5 + k * 2.8
        f.lathe((tx, 6.0, 0.0), [(0.0, 0.0), (0.24, 0.0), (0.24, 0.02), (0.04, 0.05), (0.03, 0.7), (0.0, 0.7)], M["iron"], n=12)
        f.lathe((tx, 6.0, 0.7), [(0.0, 0.0), (0.36, 0.0), (0.37, 0.025), (0.36, 0.04), (0.0, 0.04)], M["m_dark"], n=20)
        f.lathe((tx + 0.1, 6.05, 0.74), [(0.0, 0.0), (0.04, 0.0), (0.035, 0.08), (0.0, 0.08)], M["paper"], n=8)
        for s in (-1, 1):
            cs = f.sub((tx + s * 0.62, 6.0, 0), 90 * s)
            cs.lathe((0, 0, 0.44), [(0.0, 0.0), (0.2, 0.0), (0.2, 0.04), (0.0, 0.04)], M["slat"], n=12)
            for q in range(4):
                a = q / 4 * math.tau + 0.78
                cs.rod((math.cos(a) * 0.16, math.sin(a) * 0.16, 0.44), (math.cos(a) * 0.2, math.sin(a) * 0.2, 0.0), 0.012, M["iron"], 4)
            cs.torus((0, 0.19, 0.68), 0.17, 0.012, M["iron"], axis="y", n1=12, n2=4)
            cs.rod((-0.17, 0.19, 0.44), (-0.17, 0.19, 0.68), 0.012, M["iron"], 4)
            cs.rod((0.17, 0.19, 0.44), (0.17, 0.19, 0.68), 0.012, M["iron"], 4)
    ab = f.sub((-22.0, 5.6, 0), 15)                                                                 # the chalkboard
    for s in (-1, 1):
        ab.box((0, s * 0.2, 0.5), (0.6, 0.03, 1.0), M["slat"], rot=(s * 12, 0, 0))
    ab.box((0, -0.235, 0.52), (0.52, 0.01, 0.8), M["ink"], rot=(-12, 0, 0))
    ab.text("SOUP", (0, -0.25, 0.75), 0.1, M["white_paint"], kind="marker", rot=0)
    ab.text("of the day", (0, -0.25, 0.6), 0.07, M["white_paint"], kind="marker")
    # the pawnbroker: a shutter half down with graffiti, the three balls on a bracket, a barred window
    K.roller_shutter(fs, -20.4, -15.6, 1.4, 2.95, M, graffiti=[("VEX", 0.5, 8, -0.6, 0.0), ("no future", 0.22, -6, 1.2, -0.4)])
    fs.beam((-14.2, 0.0, 4.6), (-14.2, -1.1, 4.6), 0.06, 0.06, M["iron"])
    for k in range(3):
        a = k / 3 * math.tau + 1.57
        fs.lathe((-14.2 + math.cos(a) * 0.17, -1.0, 4.15 + math.sin(a) * 0.17), [(0.0, -0.12), (0.08, -0.09), (0.12, 0.0), (0.08, 0.09), (0.0, 0.12)], M["brass"], n=10)
    fs.rod((-14.2, -1.0, 4.6), (-14.2, -1.0, 4.3), 0.01, M["iron"], 4)
    # the florist: buckets of flowers on a stepped stand, a plain awning
    K_awning(fs, -12.9, -3.1, 3.25, 1.8, M, "", striped=False)
    for k in range(3):
        f.box((-8.0, 7.6 - k * 0.35, 0.15 + k * 0.25), (4.0, 0.34, 0.04), M["slat"])
        for q in range(9):
            bx = -9.8 + q * 0.45
            by = 7.6 - k * 0.35
            bz = 0.17 + k * 0.25
            f.lathe((bx, by, bz), [(0.0, 0.0), (0.1, 0.0), (0.13, 0.26), (0.0, 0.26)], M["iron_mid"], n=8)
            for p in range(5):
                a = p / 5 * math.tau
                f.rod((bx, by, bz + 0.2), (bx + math.cos(a) * 0.12, by + math.sin(a) * 0.12, bz + 0.6), 0.008, M["leaf"], 3)
                f.lathe((bx + math.cos(a) * 0.12, by + math.sin(a) * 0.12, bz + 0.58), [(0.0, 0.0), (0.05, 0.03), (0.0, 0.08)], M["fruit" if (p + q) % 2 else "white_band"], n=6)
    # upper storey dressing: a fire escape on the cafe, AC units, posters, graffiti, pigeons
    fire_escape(fs, -20.6, -16.9, [4.7, 7.6], M)
    for (ax, az) in ((-16.0, 6.0), (-27.0, 6.0), (-27.0, 8.9), (-5.25, 6.0), (-10.75, 11.8)):
        K.ac_unit(fs, ax, az, M)
    fp_ = K.L(g, (0, 8.2 - 0.165, 0), 0)
    for (px, pz, kind_) in ((-21.0, 1.6, 0), (-21.0, 2.5, 1), (-13.5, 1.5, 2)):
        lines = [[("CIRCUS  RIVOLI", 0.09, "ink"), ("TONIGHT", 0.07, "ink"), ("8 PM", 0.07, "ink")],
                 [("VOTE", 0.11, "ink"), ("CALLOWAY", 0.08, "ink"), ("FOR MAYOR", 0.06, "ink")],
                 [("LOST  DOG", 0.08, "ink"), ("REWARD", 0.07, "ink"), ("CALL  5-0144", 0.05, "ink")]][kind_]
        K.poster(fp_, px, pz, 0.6, 0.85, M, lines, rot=rnd.uniform(-4, 4))
    fp_.text("KILROY", (-2.7, -0.006, 1.6), 0.2, M["graffiti"], kind="marker", rot=80)
    for (px, pz) in ((-27.5, 14.15), (-27.0, 14.15), (-18.0, 12.65), (-8.0, 16.15), (-7.4, 16.15), (-6.6, 16.15)):
        K.pigeon(fs, px, -0.3, pz, rnd.uniform(-60, 60), M)
    # lamp posts along the kerb (unlit by day), a bench, a pillar box, bollards, bins, bicycles, news
    for x in (-24.0, -12.0, -1.0):
        K.lamp_post(f, x, -1.8, M, arm=-1.0, h=5.4)
    K.bench(f, -16.5, 2.6, 0, M)
    f.lathe((-8.0, 2.2, 0.0), [(0.0, 0.0), (0.3, 0.0), (0.3, 0.1), (0.25, 0.14), (0.25, 1.3), (0.3, 1.36), (0.3, 1.45), (0.2, 1.6), (0.0, 1.66)], M["hydrant"], n=16)
    f.box((-8.0, 1.94, 1.15), (0.3, 0.04, 0.05), M["reveal"])
    f.box((-8.0, 1.95, 0.9), (0.24, 0.02, 0.14), M["white_band"])
    f.text("POST", (-8.0, 1.94, 0.9), 0.08, M["ink"], kind="serif")
    for x in (-4.0, -3.0, 46.5, 47.5):
        K.bollard(f, x, -1.9, M)
    K.litter_bin(f, -11.2, 2.2, M)
    K.litter_bin(f, -25.6, 2.0, M)
    K.newspaper_box(f, -18.4, 2.4, 0, M, "THE MORNING STAR", "STORM BATTERS CITY")
    K.newspaper_box(f, -17.8, 2.4, 0, M, "EVENING CHRONICLE", "NIGHT OF FLOODS")
    K.bike_rack(f, -6.4, -4.4, 4.0, M)
    K.bicycle(f, -5.65, 4.0, 90, M, lean=0)
    K.bicycle(f, -4.9, 4.0, -90, M, lean=0)
    K.hydrant(f, -19.6, -1.7, M)
    K.potted_plant(f, -0.9, 2.4, M, h=1.2, kind="ball")
    K.potted_plant(f, -0.9, 13.2, M, h=1.2, kind="ball")
    # the parked black car across the road (its door is StoryBank's), and others at the far kerb
    K.sedan(g, (-6.0, -26.63, -0.15), 0, M, M["car"], M["glass_ext"])
    K.sedan(g, (-17.5, -26.6, -0.15), 180, M, M["car_grey"], M["glass_ext"])
    K.sedan(g, (7.5, -26.6, -0.15), 0, M, M["car_pale"], M["glass_ext"])
    # the far side of the road (seen from the bank's steps): its kerb, buildings with shopfronts
    f.box((20.0, -29.4, -0.08), (140.0, 2.8, 0.16), M["paving"])
    f.box((20.0, -28.05, -0.08), (140.0, 0.3, 0.16), M["kerb"], bevel=0.03)
    ff = K.L(g, (0, -30.0, 0), 180)                                                                 # u = -x, out = +y
    for k in range(10):
        x0 = -40.0 + k * 11.0
        h = 9.0 + (k * 37 % 7)
        wm = (M["brick"], M["stucco"], M["stone_y"], M["brick_dark"])[k % 4]
        u0, u1 = -(x0 + 10.6), -x0
        ups = [(u0 + 2.0 + c * 3.3, 1.1, 5.0 + r * 2.6, 6.5 + r * 2.6, False) for r in range(int((h - 4.5) / 2.6)) for c in range(3)]
        K.wall(ff, u0, u1, 0.0, h, 1.0, wm, ups, v=0.5)
        ff.box(((u0 + u1) / 2, -0.1, 3.5), (10.4, 0.2, 0.5), M["sf_paint"])
        ff.box(((u0 + u1) / 2, 0.02, 1.6), (8.5, 0.04, 2.6), M["glass_shop"])
        ff.text(("HARDWARE", "BARBER", "TAILOR", "DELI", "BOOKS", "DINER", "LAUNDRY", "OPTICIAN", "TOBACCO", "RADIO")[k], ((u0 + u1) / 2, -0.21, 3.5), 0.3, M["white_paint"], kind="serif")
        for r in range(int((h - 4.5) / 2.6)):
            for c in range(3):
                K.window(ff, u0 + 2.0 + c * 3.3, 5.0 + r * 2.6, 6.5 + r * 2.6, 1.1, M, M["frame_pale"], M["glass_ext"], M["trim"], cols=2, rows=2, blind=rnd.choice((None, 0.5)))
        K.cornice(ff, u0, u1, h - 0.45, M["trim"], depth=0.35)
    for x in (-30.0, -12.0, 6.0, 24.0):
        K.lamp_post(f, x, -28.5, M, arm=1.0, h=5.4)
    # the city behind everything
    for k in range(14):
        x = -60.0 + k * 11.0 + rnd.uniform(-2, 2)
        h = rnd.uniform(20.0, 55.0)
        yy = rnd.uniform(40.0, 70.0)
        w_, d_ = rnd.uniform(7, 12), rnd.uniform(7, 12)
        f.box((x, yy, h / 2), (w_, d_, h), M["city"])
        for r in range(int(h / 3.0)):
            f.box((x, yy - d_ / 2 - 0.02, 1.5 + r * 3.0), (w_ * 0.9, 0.05, 1.2), M["glass_ext"])
    f.box((10.0, 120.0, 40.0), (400.0, 1.0, 80.0), M["sky_day"])
    return g.finish(coll)


def K_awning(f, x0, x1, z, depth, M, text, striped=True):
    """A shop awning: canvas sloping out from the wall in stripes, a scalloped valance with lettering,
    the iron arms."""
    n = int((x1 - x0) / 0.35)
    sw = (x1 - x0) / n
    ang = math.degrees(math.atan2(0.7, depth))
    for k in range(n):
        m = (M["cloth_a"] if k % 2 == 0 else M["cloth_b"]) if striped else M["cloth_b"]
        f.box((x0 + (k + 0.5) * sw, -depth / 2, z - 0.35), (sw, math.hypot(depth, 0.7), 0.025), m, rot=(-ang, 0, 0))
        f.lathe((x0 + (k + 0.5) * sw, -depth - 0.01, z - 0.95), [(0.0, 0.0), (sw * 0.48, 0.02), (sw * 0.5, 0.18), (0.0, 0.18)], m, n=8, axis="y")
    f.box(((x0 + x1) / 2, -depth - 0.01, z - 0.82), (x1 - x0, 0.02, 0.26), M["cloth_b"])
    if text:
        f.text(text, ((x0 + x1) / 2, -depth - 0.025, z - 0.82), 0.16, M["white_paint"], kind="serif")
    for xx in (x0 + 0.1, x1 - 0.1):
        f.beam((xx, 0.0, z - 1.0), (xx, -depth, z - 0.72), 0.03, 0.03, M["iron"])
        f.box((xx, -depth / 2, z - 0.35), (0.04, math.hypot(depth, 0.7) + 0.04, 0.04), M["iron"], rot=(-ang, 0, 0))
    f.box(((x0 + x1) / 2, -0.12, z + 0.05), (x1 - x0 + 0.1, 0.24, 0.18), M["iron"], bevel=0.02)


def fire_escape(f, x0, x1, levels, M):
    """A fire escape on the wall (local frame): landings of slatted grating with railings and
    balusters, stairs between them, the drop ladder hanging over the street."""
    d = 1.1
    for z in levels:
        f.box(((x0 + x1) / 2, -d / 2, z), (x1 - x0, d, 0.04), M["iron"])
        k = x0 + 0.05
        while k < x1:
            f.box((k, -d / 2, z + 0.025), (0.02, d - 0.05, 0.012), M["iron_mid"])
            k += 0.08
        for xx in (x0, x1):
            f.box((xx, -d / 2, z + 0.5), (0.03, d, 0.03), M["iron"])
        f.box(((x0 + x1) / 2, -d, z + 1.0), (x1 - x0, 0.04, 0.04), M["iron"])
        f.box(((x0 + x1) / 2, -d, z + 0.5), (x1 - x0, 0.02, 0.02), M["iron"])
        xx = x0
        while xx <= x1 + 1e-3:
            f.box((xx, -d, z + 0.5), (0.02, 0.02, 1.0), M["iron"])
            xx += 0.25
        for xx in (x0 + 0.4, x1 - 0.4):
            f.beam((xx, 0.0, z - 0.6), (xx, -d + 0.1, z - 0.02), 0.04, 0.04, M["iron"])
    for a, b in zip(levels, levels[1:]):
        for s in (0.15, 0.85):
            f.beam((x0 + 0.6, -d * s, a), (x0 + 2.6, -d * s, b), 0.04, 0.12, M["iron"])
        for q in range(1, 10):
            t = q / 10
            f.box((x0 + 0.6 + 2.0 * t, -d / 2, a + (b - a) * t), (0.2, d * 0.7, 0.02), M["iron_mid"])
        f.beam((x0 + 0.6, -d * 0.85, a + 0.9), (x0 + 2.6, -d * 0.85, b + 0.9), 0.03, 0.03, M["iron"])
    for s in (-0.22, 0.22):
        f.box((x1 - 0.6 + s, -d + 0.05, levels[0] - 1.2), (0.03, 0.03, 2.3), M["iron"])
    for q in range(8):
        f.box((x1 - 0.6, -d + 0.05, levels[0] - 2.2 + q * 0.28), (0.44, 0.025, 0.025), M["iron"])


# ---------------------------------------------------------------- light, world, export

def lights():
    def add(kind, name, loc, energy, color=(1, 1, 1), size=1.0, rot=None):
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
    # a white, overcast morning: a soft low sun and a bright sky
    sun = add("SUN", "Sun", (0, 0, 50), 2.2)
    sun.rotation_euler = (math.radians(55), 0, math.radians(-35))
    sun.data.angle = math.radians(12)
    # daylight through the back windows (and the west ones) into the lobby
    for x in BACK_WIN:
        add("AREA", "WindowLight", (x, YB - 0.6, 5.6), 900, size=3.0, rot=(math.radians(90), 0, 0))
    for y in WEST_WIN:
        add("AREA", "WestWindow", (0.5, y, 4.4), 220, size=1.6, rot=(0, math.radians(-90), 0))
    add("AREA", "Laylight", (13.5, 7.0, H - 0.3), 900, size=9.0)
    # the lobby's lamps and a soft fill so the lane reads
    for x in (6.0, 13.0, 20.0):
        add("POINT", "Globe", (x, 1.2, 5.5), 700, size=0.3)
    add("AREA", "LobbyFill", (13.0, -6.0, 5.0), 1400, size=20.0, rot=(math.radians(80), 0, 0))
    add("AREA", "CounterFill", (17.5, 3.4, 2.9), 120, size=10.0)
    # the corridor and the vault: cold strips
    for x in (27.5, 31.0, 36.0, 40.5, 44.5):
        add("AREA", "Strip", (x, 0.4, VAULT_TOP - 0.12), 260, size=1.6, rot=(0, 0, 0))
    add("AREA", "VaultFill", (39.0, -5.0, 2.2), 500, size=8.0, rot=(math.radians(80), 0, 0))
    # outside: the street lamps are off in daylight; a fill off the road
    add("AREA", "StreetFill", (-12.0, -10.0, 4.0), 2600, size=30.0, rot=(math.radians(75), 0, 0))
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
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "BankSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported BankSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    K.reset()
    S.OUT = OUT                                   # the bakes land in Assets/Art/Bank
    coll = bpy.context.scene.collection
    M = mats()
    M["sf_paint"] = K.grey("BankShopPaint", 0.05, 0.45)
    shell = build_shell(coll, M)
    front = build_front(coll, M)
    street = build_street(coll, M)
    total = sum(K.finalize(o) for o in (shell, front, street))
    print(f"  bank set: {total} triangles")
    lights()
    if preview_only:
        front.hide_render = True
        cams = [
            ("B1  the street at dawn", S.shot_camera("P1", (-30.0, 9.0, -22.0), (-6.0, 3.0, 4.0), 46), None),
            ("B2  the mark over the doors", S.shot_camera("P2", (-9.0, 4.2, 0.0), (0.0, 5.3, 0.0), 40), None),
            ("B4  gameplay: the street", S.shot_camera("P3", (-14.0, 2.4, -11.0), (-14.0, 1.6, 0.0), 34), None),
            ("B5  the lobby, wide", S.shot_camera("P4", (2.0, 2.2, -1.2), (16.0, 3.4, 8.0), 54), None),
            ("B6  gameplay: the counter", S.shot_camera("P5", (16.0, 2.4, -10.5), (16.0, 1.7, 0.0), 34), None),
            ("B10  the corridor and the vault door", S.shot_camera("P6", (26.5, 1.6, -0.8), (34.0, 1.5, 1.2), 50), None),
            ("B11  the wall of boxes", S.shot_camera("P7", (38.0, 1.5, -1.2), (38.0, 1.6, 2.8), 52), None),
            ("B21  outside, wide", S.shot_camera("P8", (-20.0, 1.2, -7.0), (-4.0, 2.4, 0.0), 44), None),
            ("2.2  the palm scanner, over his shoulder", S.shot_camera("P9", (15.2, 1.75, -0.9), (16.4, 1.25, 1.75), 32), None),
            ("B11  box 417, close", S.shot_camera("P10", (37.25, 1.65, 0.4), (37.25, 1.35, 2.6), 40), None),
            ("B21  from the steps: the car across the road", S.shot_camera("P11", (-1.4, 1.55, 1.2), (-6.0, 1.0, -26.0), 38), None),
            ("2.1  the clock: 8:41", S.shot_camera("P12", (9.0, 3.5, 4.0), (13.0, 8.6, 15.8), 40), None),
        ]
        sel = [a for a in sys.argv if a.startswith("--shots=")]
        if sel:
            keep = {int(v) for v in sel[0].split("=", 1)[1].split(",") if v}
            cams = [c for i, c in enumerate(cams, 1) if i in keep]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "bank_set.png" if not sel else "bank_set_part.png")
        return
    for o in (shell, front, street):
        o.select_set(False)
    for o in (shell, front, street):
        S.unwrap(o)
    front.hide_render = True                      # the cut-away front never shades the lane
    S.bake(shell, 4096, 32)
    S.bake(street, 4096, 24)
    front.hide_render = False
    S.bake(front, 2048, 16)
    front.hide_render = True
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export([shell, front, street] + marks)


if __name__ == "__main__":
    main()
