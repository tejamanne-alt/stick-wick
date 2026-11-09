"""Chapter 2: THE BANK (docs/story/chapter2_bank.md). The morning after the storm: a wet city
street, MERIDIAN TRUST's stone front with its portico and its mark carved over the doors, the
marble lobby (teller windows behind glass, a clock, pendant lamps, a velvet rope), a corridor
through a barred gate to the round vault door, and the safe-deposit room with its wall of
numbered boxes. Built in the fight-mode tower's palette, like the warehouse (build_story_set's
materials and light go through its TOWER tone): pale stone and marble, black steel, white trims.

    /home/user/.venv-blender/bin/python tools/blender/build_bank_set.py [--preview]

Writes Assets/Art/Bank/BankSet.fbx and its bakes (Story_BankShell.png, Story_BankStreet.png,
Story_BankFront.png); --preview renders docs/previews/bank_set.png instead.

Coordinates as in the warehouse: Blender X = Unity x (along the lane), Blender Y = Unity z
(depth, away from the camera), Blender Z = Unity y (up). The lane is y = 0, the floor z = 0
everywhere (a level entrance), the camera side y < -2.2 is the road."""
import math
import os
import random
import sys

import bpy  # noqa: E402  (bpy first: it provides bmesh and mathutils)
import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402

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


def mats():
    M = S.materials()
    mat = S.mat
    M["stone"] = mat("Stone", (0.44, 0.44, 0.43), 0.8)               # tone() keeps it: chosen grey below
    M["stone_dark"] = mat("StoneDark", (0.2, 0.2, 0.2), 0.85)
    M["marble_pale"] = mat("MarblePale", (0.5, 0.5, 0.5), 0.15)
    M["marble_dark"] = mat("MarbleDark", (0.06, 0.06, 0.065), 0.15)
    M["wood_dark"] = mat("CounterWood", (0.08, 0.06, 0.05), 0.5)
    M["brass"] = mat("Brass", (0.62, 0.55, 0.35), 0.35, metal=0.6)  # in black and white: a pale bright trim
    M["velvet"] = mat("Velvet", (0.05, 0.02, 0.02), 0.95)
    M["box_steel"] = mat("BoxSteel", (0.32, 0.33, 0.34), 0.35, metal=0.6)
    M["vault_steel"] = mat("VaultSteel", (0.26, 0.27, 0.28), 0.3, metal=0.7)
    M["keyhole"] = mat("Keyhole", (0.02, 0.02, 0.02), 0.6)
    M["clock_face"] = mat("ClockFace", (0.85, 0.85, 0.82), 0.6, emit=(1, 1, 1), strength=0.4)
    M["window_day"] = mat("WindowDay", (0.8, 0.82, 0.85), 0.2, emit=(0.9, 0.92, 0.95), strength=2.2)
    M["globe"] = mat("Globe", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=18.0)
    M["strip"] = mat("StripLight", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=14.0)
    M["sky_day"] = mat("SkyDay", (0.6, 0.62, 0.66), 0.5, emit=(0.8, 0.82, 0.86), strength=1.2)
    M["car"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["tyre"] = mat("Tyre", (0.02, 0.02, 0.02), 0.9)
    return M


S.TONE.update({"Stone": 0.40, "StoneDark": 0.16, "MarblePale": 0.42, "MarbleDark": 0.05, "CounterWood": 0.06,
               "Brass": 0.55, "Velvet": 0.035, "BoxSteel": 0.28, "VaultSteel": 0.2, "CarBlack": 0.025})


# ---------------------------------------------------------------- pieces

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


def text_x(g, text, c, size, m):
    """Letters cut in stone on the west face, readable from the street (-x)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.02
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    R = Matrix.Rotation(math.radians(-90), 4, "Z") @ Matrix.Rotation(math.radians(90), 4, "X")
    me.transform(Matrix.Translation(Vector(c)) @ R)
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def shield(g, cy, cz, s, x, m, hole=None):
    """The bank's mark: a shield with a keyhole through it (the keyhole in a darker inlay)."""
    # outline: flat top, curved sides to a point
    pts = [(cy - 0.82 * s, cz + 0.88 * s), (cy + 0.82 * s, cz + 0.88 * s)]
    for i in range(1, 12):
        t = i / 12
        v = 0.1 - t * 1.05
        half = 0.82 * math.sqrt(max(0.0, (v + 0.95) / 1.05))
        pts.append((cy + half * s, cz + v * s))
    pts.append((cy, cz - 0.95 * s))
    for i in range(11, 0, -1):
        t = i / 12
        v = 0.1 - t * 1.05
        half = 0.82 * math.sqrt(max(0.0, (v + 0.95) / 1.05))
        pts.append((cy - half * s, cz + v * s))
    prism_x(g, pts, x - 0.06, x, m)
    if hole is not None:
        # the keyhole, set into the face
        g.cyl((x - 0.07, cy, cz + 0.22 * s), 0.2 * s, 0.02, hole, rot=(0, 90, 0), n=20)
        prism_x(g, [(cy - 0.07 * s, cz + 0.18 * s), (cy + 0.07 * s, cz + 0.18 * s), (cy + 0.13 * s, cz - 0.55 * s), (cy - 0.13 * s, cz - 0.55 * s)], x - 0.075, x - 0.065, hole)


def column(g, x, y, h, r, M, base=0.0):
    """A stone column: plinth, fluted shaft (a 20-sided cylinder), capital."""
    g.box((x, y, base + 0.15), (r * 2.6, r * 2.6, 0.3), M["stone"])
    g.cyl((x, y, base + 0.3 + (h - 0.7) / 2), r, h - 0.7, M["stone"], n=20)
    g.box((x, y, base + h - 0.2), (r * 2.8, r * 2.8, 0.4), M["stone"])


def window_west_free(g, x, y, z0, z1, w, M):
    """A tall window in the back wall (y = YB): frame, mullions, glowing daylight glass."""
    g.box((x, YB - 0.05, (z0 + z1) / 2), (w, 0.04, z1 - z0), M["window_day"])
    for xx in (x - w / 2, x, x + w / 2):
        g.box((xx, YB - 0.1, (z0 + z1) / 2), (0.08, 0.08, z1 - z0), M["steel"])
    zz = z0
    while zz <= z1 + 0.01:
        g.box((x, YB - 0.1, zz), (w, 0.08, 0.06), M["steel"])
        zz += (z1 - z0) / 4


# ---------------------------------------------------------------- the bank

def build_shell(coll, M):
    g = S.Group("BankShell")
    # floors: marble in the lobby (big pale tiles with dark joints), stone in the vault
    for i in range(int(LOBBY_X1 / 1.2) + 1):
        for j in range(int((YB - YF) / 1.2) + 1):
            x, y = X0 + 0.6 + i * 1.2, YF + 0.6 + j * 1.2
            if x > LOBBY_X1:
                continue
            g.box((x, y, -0.05), (1.19, 1.19, 0.1), M["marble_pale"] if (i + j) % 2 == 0 else M["marble_dark"])
    g.box(((LOBBY_X1 + X1) / 2, (YF + VAULT_BACK) / 2, -0.05), (X1 - LOBBY_X1, VAULT_BACK - YF, 0.1), M["stone_dark"])
    # walls (the front, y = YF, is its own object: it is cut away)
    g.box((X1 / 2, YB + 0.2, H / 2), (X1 + 0.8, 0.4, H), M["stone"])
    g.box((X1 + 0.2, (YF + YB) / 2, H / 2), (0.4, YB - YF, H), M["stone"])
    # the west (entrance) wall with its door opening (y -1.2..1.2, 3.6 high)
    g.box((X0 - 0.2, (YF + -1.2) / 2, H / 2), (0.4, -1.2 - YF, H), M["stone"])
    g.box((X0 - 0.2, (1.2 + YB) / 2, H / 2), (0.4, YB - 1.2, H), M["stone"])
    g.box((X0 - 0.2, 0.0, (3.6 + H) / 2), (0.4, 2.4, H - 3.6), M["stone"])
    # the roof and the lobby's coffered ceiling
    g.box((X1 / 2, (YF + YB) / 2, H + 0.2), (X1 + 0.8, YB - YF + 0.8, 0.4), M["stone_dark"])
    for i in range(int(LOBBY_X1 / 3)):
        g.box((1.5 + i * 3, (YF + YB) / 2, H - 0.15), (0.18, YB - YF, 0.3), M["stone"])
    for j in range(int((YB - YF) / 3)):
        g.box((LOBBY_X1 / 2, YF + 1.5 + j * 3, H - 0.15), (LOBBY_X1, 0.18, 0.3), M["stone"])
    # tall windows in the back wall: the white morning comes in
    for x in (4.0, 10.0, 16.0, 22.0):
        window_west_free(g, x, None, 2.6, 8.8, 2.6, M)
    # the clock, high on the back wall: nine o'clock
    g.cyl((13.0, YB - 0.08, 9.6), 0.95, 0.06, M["clock_face"], rot=(90, 0, 0), n=40)
    g.torus((13.0, YB - 0.12, 9.6), 0.98, 0.06, M["brass"], rot=(90, 0, 0), n1=40, n2=6)
    g.box((13.0, YB - 0.16, 9.6 + 0.32), (0.06, 0.02, 0.64), M["steel"])          # 12
    g.box((13.0 - 0.25, YB - 0.16, 9.6), (0.5, 0.02, 0.07), M["steel"])           # 9
    for k in range(12):
        a = k / 12 * math.tau
        g.box((13.0 + math.sin(a) * 0.82, YB - 0.15, 9.6 + math.cos(a) * 0.82), (0.05, 0.02, 0.12), M["steel"])
    # columns in the lobby, two rows behind the lane
    for x in (5.0, 11.0, 17.0, 23.0):
        for y in (5.0, 11.0):
            column(g, x, y, H - 0.3, 0.42, M)
    # the teller counter: dark wood, a pale stone top, glass to head height in brass frames
    cx0, cx1, cy0, cy1 = COUNTER
    g.box(((cx0 + cx1) / 2, (cy0 + cy1) / 2, 0.55), (cx1 - cx0, cy1 - cy0, 1.1), M["wood_dark"])
    g.box(((cx0 + cx1) / 2, (cy0 + cy1) / 2, 1.13), (cx1 - cx0 + 0.1, cy1 - cy0 + 0.12, 0.06), M["stone"])
    # (the glass itself is the game's own, transparent: StoryBank.BuildBankProps; the clerk is seen through it)
    x = cx0
    while x <= cx1 + 0.01:
        g.box((x, cy0 + 0.25, 1.85), (0.06, 0.06, 1.4), M["brass"])
        x += (cx1 - cx0) / 4
    g.box(((cx0 + cx1) / 2, cy0 + 0.25, 2.58), (cx1 - cx0, 0.08, 0.08), M["brass"])
    for k, label in enumerate(("1", "2", "3", "4")):
        tx = cx0 + (k + 0.5) * (cx1 - cx0) / 4
        g.box((tx, cy0 + 0.22, 2.8), (0.7, 0.04, 0.3), M["marble_dark"])
        S.stencil(g, "TELLER " + label, (tx, cy0 + 0.19, 2.74), 0.12, M["brass"])
        g.box((tx, cy0 + 0.2, 1.17), (0.5, 0.3, 0.02), M["marble_dark"])     # the slot under the glass
    # behind the counter: the tellers' desks, a wall of pigeonholes
    for k in range(4):
        tx = cx0 + (k + 0.5) * (cx1 - cx0) / 4
        g.box((tx, cy1 + 1.4, 0.4), (1.6, 0.7, 0.8), M["wood_dark"])
    # a velvet rope: brass posts and a sagging red rope (dark in black and white)
    for k in range(5):
        g.cyl((7.0 + k * 1.1, 0.95, 0.5), 0.04, 1.0, M["brass"], n=10)
        g.cyl((7.0 + k * 1.1, 0.95, 0.02), 0.16, 0.04, M["brass"], n=14)
        g.cyl((7.0 + k * 1.1, 0.95, 1.02), 0.07, 0.06, M["brass"], n=10)
        if k < 4:
            for s in range(6):
                t0, t1 = s / 6, (s + 1) / 6
                z0 = 0.92 - math.sin(t0 * math.pi) * 0.18
                z1 = 0.92 - math.sin(t1 * math.pi) * 0.18
                g.beam((7.0 + k * 1.1 + t0 * 1.1, 0.95, z0), (7.0 + k * 1.1 + t1 * 1.1, 0.95, z1), 0.04, 0.04, M["velvet"])
    # benches along the back for the waiting
    for x in (3.0, 8.0):
        g.box((x, 8.0, 0.45), (2.2, 0.5, 0.08), M["wood_dark"])
        g.box((x, 8.22, 0.75), (2.2, 0.06, 0.6), M["wood_dark"])
        for dx in (-0.95, 0.95):
            g.box((x + dx, 8.0, 0.22), (0.08, 0.45, 0.44), M["steel"])
    # pendant globes on long rods
    for x in (6.0, 13.0, 20.0):
        g.beam((x, 1.2, H - 0.3), (x, 1.2, 5.8), 0.02, 0.02, M["steel"])
        g.cyl((x, 1.2, 5.6), 0.32, 0.5, M["globe"], n=20)
    # the corridor: a low ceiling, its back wall, the barred gate (swung open against the wall)
    g.box(((LOBBY_X1 + X1) / 2, (YF + YB) / 2, VAULT_TOP + 0.1), (X1 - LOBBY_X1, YB - YF, 0.2), M["stone_dark"])
    g.box((LOBBY_X1 + 0.2, (2.6 + YB) / 2, VAULT_TOP / 2), (0.4, YB - 2.6, VAULT_TOP), M["stone"])       # closes the lobby off behind
    g.box(((LOBBY_X1 + VAULT_X) / 2, 2.8, VAULT_TOP / 2), (VAULT_X - LOBBY_X1, 0.3, VAULT_TOP), M["stone"])
    g.box((LOBBY_X1 + 0.2, YF + 0.2, (VAULT_TOP + H) / 2), (0.4, 0.4, H - VAULT_TOP), M["stone"])
    g.box((LOBBY_X1 + 0.2, (YF + 2.6) / 2, (VAULT_TOP + H) / 2), (0.4, 2.6 - YF, H - VAULT_TOP), M["stone"])   # the wall over the corridor mouth
    for y in (YF + 0.15, 2.55):
        g.box((GATE_X, y, VAULT_TOP / 2 - 0.1), (0.14, 0.14, VAULT_TOP - 0.2), M["steel"])
    g.box((GATE_X, (YF + 2.6) / 2, VAULT_TOP - 0.25), (0.14, 2.6 - YF, 0.14), M["steel"])
    for k in range(9):                                                                         # the gate leaf, open
        g.box((GATE_X + 0.15 + k * 0.22, 2.45, 1.6), (0.035, 0.035, 3.0), M["steel"])
    for z in (0.15, 1.6, 3.05):
        g.box((GATE_X + 1.03, 2.45, z), (1.9, 0.06, 0.06), M["steel"])
    S.stencil(g, "AUTHORISED PERSONS ONLY", ((GATE_X + VAULT_X) / 2, 2.62, 2.9), 0.16, M["paint_yellow"])
    for x in (27.5, 31.0):
        g.box((x, 0.3, VAULT_TOP - 0.03), (1.4, 0.18, 0.05), M["strip"])
    # the vault wall, an opening for the round door, the ring of the frame
    g.box((VAULT_X, (YF + -1.1) / 2, VAULT_TOP / 2), (0.7, -1.1 - YF, VAULT_TOP), M["vault_steel"])
    g.box((VAULT_X, (1.7 + VAULT_BACK) / 2, VAULT_TOP / 2), (0.7, VAULT_BACK - 1.7, VAULT_TOP), M["vault_steel"])
    g.box((VAULT_X, 0.3, (3.1 + VAULT_TOP) / 2), (0.7, 2.8, VAULT_TOP - 3.1), M["vault_steel"])
    g.torus((VAULT_X - 0.36, 0.3, 1.55), 1.52, 0.12, M["steel_mid"], rot=(0, 90, 0), n1=40, n2=8)
    # the round door, standing open into the vault, hinged at the back
    dx, dy = VAULT_X + 1.3, 1.95
    g.cyl((dx, dy, 1.55), 1.45, 0.55, M["vault_steel"], rot=(90, 0, 0), n=40)
    g.torus((dx, dy - 0.3, 1.55), 1.25, 0.06, M["steel_mid"], rot=(90, 0, 0), n1=40, n2=6)
    g.torus((dx, dy - 0.32, 1.55), 0.42, 0.05, M["brass"], rot=(90, 0, 0), n1=24, n2=6)     # the wheel
    for k in range(4):
        a = k / 4 * math.pi
        g.box((dx, dy - 0.32, 1.55), (0.84 * abs(math.cos(a)) + 0.04, 0.04, 0.84 * abs(math.sin(a)) + 0.04), M["brass"])
    for k in range(12):                                                                        # the bolts round its rim
        a = k / 12 * math.tau
        g.cyl((dx + math.cos(a) * 1.4, dy - 0.15, 1.55 + math.sin(a) * 1.4), 0.07, 0.5, M["brass"], rot=(90, 0, 0), n=10)
    g.box((VAULT_X + 0.2, 1.75, 1.55), (0.3, 0.3, 1.8), M["vault_steel"])                     # the hinge
    # the safe-deposit room: the wall of boxes, a table, cold strip lights
    g.box(((VAULT_X + X1) / 2, VAULT_BACK + 0.15, VAULT_TOP / 2), (X1 - VAULT_X, 0.3, VAULT_TOP), M["stone_dark"])
    n = 0
    z = 0.25
    row = 0
    while z + 0.3 < VAULT_TOP - 0.1:
        x = VAULT_X + 1.0
        col = 0
        while x + 0.5 < X1 - 0.3:
            num = 400 + row * 1 + col * 9
            big = (row % 3 == 0)
            h = 0.36 if big else 0.28
            g.box((x + 0.25, VAULT_BACK - 0.02, z + h / 2), (0.48, 0.06, h - 0.03), M["box_steel"])
            g.cyl((x + 0.37, VAULT_BACK - 0.06, z + h / 2), 0.025, 0.02, M["keyhole"], rot=(90, 0, 0), n=8)
            g.box((x + 0.13, VAULT_BACK - 0.055, z + h / 2), (0.14, 0.01, 0.05), M["brass"])  # number plate
            x += 0.5
            col += 1
            n += 1
        z += 0.36 if row % 3 == 0 else 0.28
        row += 1
    # box 417, a hair proud of the rest (the eye finds it)
    g.box((BOX417[0], VAULT_BACK - 0.06, BOX417[1]), (0.48, 0.05, 0.3), M["box_steel"])
    g.box(((VAULT_X + X1) / 2 + 1.5, 0.75, 0.88), (2.0, 0.9, 0.05), M["steel_mid"])          # the table
    for ddx in (-0.9, 0.9):
        for ddy in (-0.38, 0.38):
            g.box(((VAULT_X + X1) / 2 + 1.5 + ddx, 0.75 + ddy, 0.43), (0.05, 0.05, 0.86), M["steel"])
    for x in (36.0, 40.5, 44.5):
        g.box((x, 0.4, VAULT_TOP - 0.03), (2.0, 0.16, 0.05), M["strip"])
    g.box((X1 - 0.3, (YF + VAULT_BACK) / 2, VAULT_TOP / 2), (0.2, VAULT_BACK - YF, VAULT_TOP), M["stone_dark"])

    # ---------------------------------------------------------------- the stone front (west)
    # a plinth course, the portico of four columns behind the lane, its entablature and pediment
    g.box((X0 - 0.45, (YF + YB) / 2, 0.25), (0.5, YB - YF, 0.5), M["stone"])
    for y in (2.2, 5.4, 8.6, 11.8):
        column(g, X0 - 3.0, y, 8.2, 0.48, M)
    g.box((X0 - 1.6, (YF + YB) / 2 - 1.0, 8.75), (3.6, YB - YF + 1.2, 1.1), M["stone"])        # entablature
    g.box((X0 - 1.6, (YF + YB) / 2 - 1.0, 9.42), (3.8, YB - YF + 1.4, 0.24), M["stone"])        # cornice
    text_x(g, "MERIDIAN  TRUST", (X0 - 3.42, (YF + YB) / 2 - 1.0, 8.55), 0.72, M["stone_dark"])
    # the mark carved over the doors
    shield(g, 0.0, 5.3, 0.95, X0 - 0.42, M["stone"], hole=M["stone_dark"])
    g.box((X0 - 0.43, 0.0, 5.3), (0.02, 2.4, 2.4), M["stone_dark"])                           # its recessed panel
    # the doors: a bronze frame and a revolving door's drum (glass) with its wings
    for y in (-1.25, 1.25):
        g.box((X0 - 0.42, y, 1.8), (0.12, 0.12, 3.6), M["brass"])
    g.box((X0 - 0.42, 0.0, 3.62), (0.12, 2.6, 0.12), M["brass"])
    # (the doors are the game's own: glass double doors the guard unlocks and opens, StoryBank)
    return g.finish(coll)


def build_front(coll, M):
    """The bank's long side facing the road (y = YF): stone with tall arched windows. The game
    cuts it away whenever the camera is outside it."""
    g = S.Group("BankFront")
    yf = YF - 0.2
    xs = [X0, 2.0]
    for x in (4.0, 10.0, 16.0, 22.0):
        xs += [x - 1.3, x + 1.3]
    xs += [LOBBY_X1, X1]
    # pillars between the windows, the band under them and the wall over them
    g.box((X1 / 2, yf, 1.2), (X1, 0.4, 2.4), M["stone"])
    g.box((X1 / 2, yf, (8.8 + H) / 2), (X1, 0.4, H - 8.8), M["stone"])
    for a, b in zip(xs[0::2], xs[1::2]):
        g.box(((a + b) / 2, yf, (2.4 + 8.8) / 2), (b - a, 0.4, 6.4), M["stone"])
    for x in (4.0, 10.0, 16.0, 22.0):
        g.box((x, yf + 0.1, 5.6), (2.6, 0.04, 6.4), M["window_day"])
        for xx in (x - 0.65, x, x + 0.65):
            g.box((xx, yf - 0.05, 5.6), (0.07, 0.07, 6.4), M["steel"])
    g.box(((LOBBY_X1 + X1) / 2, yf, (2.4 + 8.8) / 2), (X1 - LOBBY_X1, 0.4, 6.4), M["stone"])
    return g.finish(coll)


def build_street(coll, M):
    """The street outside: the road, the sidewalk (level with the bank's floor), the shops up the
    block, a lamp or two, a parked black car, puddles, the far side, the city."""
    g = S.Group("BankStreet")
    g.box((20.0, -16.0, -0.2), (140.0, 28.0, 0.1), M["asphalt"])                          # the road (a curb lower)
    g.box((-15.0, 3.0, -0.05), (32.0, 10.4, 0.1), M["concrete"])                          # the sidewalk and the plaza
    g.box((20.0, -2.25, -0.08), (140.0, 0.3, 0.16), M["stone"])                            # the curb
    for x in range(-58, 80, 9):
        g.box((x, -12.0, -0.145), (3.0, 0.15, 0.01), M["paint_yellow"])                    # the centre line
    for (px, py, r) in ((-6.0, -1.0, 0.9), (-17.0, 0.6, 1.3), (-11.0, -6.0, 2.2), (4.0, -8.0, 1.8), (-25.0, -4.0, 1.6)):
        g.cyl((px, py, (0.002 if py > -2.2 else -0.148)), r, 0.004, M["puddle"], n=24)
    # the shops up the block: a cafe with an awning, a shuttered pawnbroker's, a narrow door
    for (x0, x1, name) in ((-30.0, -21.0, "CAFE  NOIR"), (-21.0, -13.5, "PAWN"), (-13.5, -2.5, "")):
        g.box(((x0 + x1) / 2, 8.4, 4.5), (x1 - x0, 0.4, 9.0), M["block"])
        if name == "CAFE  NOIR":
            g.box(((x0 + x1) / 2, 8.15, 1.6), (x1 - x0 - 2.0, 0.06, 2.6), M["window_day"])
            g.box(((x0 + x1) / 2, 6.9, 3.25), (x1 - x0 - 1.0, 2.6, 0.08), M["stone_dark"], rot=(-12, 0, 0))   # awning
            for k in range(2):
                g.cyl((x0 + 2.5 + k * 2.8, 6.0, 0.37), 0.35, 0.03, M["steel"], n=16)       # cafe tables
                g.cyl((x0 + 2.5 + k * 2.8, 6.0, 0.18), 0.03, 0.36, M["steel"], n=8)
        elif name == "PAWN":
            S.corrugated(g, x0 + 1.0, x1 - 1.0, 0.0, 2.8, 8.12, -1, M, rust_chance=0.0)
        S.stencil(g, name, ((x0 + x1) / 2, 8.1, 3.6), 0.5, M["paint_yellow"])
        for k in range(int((x1 - x0) / 2.4)):
            for row in range(2):
                g.box((x0 + 1.2 + k * 2.4, 8.15, 5.5 + row * 2.0), (1.2, 0.05, 1.4), M["glass"])
    # lamp posts along the curb and a bench, a postbox, bollards
    for x in (-24.0, -12.0, -1.0):
        g.cyl((x, -1.8, 2.6), 0.07, 5.2, M["steel"], n=10)
        g.beam((x, -1.8, 5.1), (x, -0.9, 5.3), 0.06, 0.06, M["steel"])
        g.box((x, -0.8, 5.2), (0.4, 0.3, 0.14), M["globe"])
    g.box((-16.5, 2.6, 0.45), (2.2, 0.5, 0.08), M["wood_dark"])
    g.box((-16.5, 2.82, 0.75), (2.2, 0.06, 0.6), M["wood_dark"])
    g.box((-8.0, 2.2, 0.6), (0.5, 0.45, 1.2), M["paint_red"])
    for x in (-4.0, -3.0, 46.5, 47.5):
        g.cyl((x, -1.9, 0.45), 0.12, 0.9, M["steel"], n=12)
    # the parked black car at the curb (its door will open)
    # (across the road, at the far curb: out of the lane's way, in the shot from the bank's doors)
    cx, cy0, cy1 = -6.0, -27.4, -25.8
    prof = [(-2.3, 0.35), (2.3, 0.35), (2.3, 0.85), (1.2, 0.95), (0.5, 1.45), (-1.0, 1.45), (-1.8, 0.95), (-2.3, 0.9)]
    g.prism([(cx + px, pz - 0.15) for px, pz in prof], cy0, cy1, M["car"])
    for wx in (-1.45, 1.45):
        for wy in (cy0 - 0.05, cy1 + 0.05):
            g.cyl((cx + wx, wy, 0.17), 0.34, 0.24, M["tyre"], rot=(90, 0, 0), n=16)
    g.box((cx - 0.3, cy1 + 0.02, 1.05), (1.9, 0.02, 0.38), M["glass"])
    # the far side of the road and the city behind everything
    for k in range(10):
        x0 = -40.0 + k * 11.0
        h = rnd.uniform(9.0, 16.0)
        g.box((x0 + 5.0, -30.5, h / 2), (10.6, 1.0, h), M["block"] if k % 2 else M["stone"])
        for r in range(int(h / 2.6)):
            for c in range(3):
                g.box((x0 + 2.0 + c * 3.0, -29.95, 1.6 + r * 2.6), (1.5, 0.05, 1.5), M["glass"])
    for k in range(14):
        x = -60.0 + k * 11.0 + rnd.uniform(-2, 2)
        h = rnd.uniform(20.0, 55.0)
        g.box((x, rnd.uniform(40.0, 70.0), h / 2), (rnd.uniform(7, 12), rnd.uniform(7, 12), h), M["city"])
    g.box((10.0, 120.0, 40.0), (400.0, 1.0, 80.0), M["sky_day"])
    return g.finish(coll)


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
    # daylight through the back windows into the lobby
    for x in (4.0, 10.0, 16.0, 22.0):
        add("AREA", "WindowLight", (x, YB - 0.6, 5.6), 900, size=3.0, rot=(math.radians(90), 0, 0))
    # the lobby's lamps and a soft fill so the lane reads
    for x in (6.0, 13.0, 20.0):
        add("POINT", "Globe", (x, 1.2, 5.5), 700, size=0.3)
    add("AREA", "LobbyFill", (13.0, -6.0, 5.0), 1400, size=20.0, rot=(math.radians(80), 0, 0))
    # the corridor and the vault: cold strips
    for x in (27.5, 31.0, 36.0, 40.5, 44.5):
        add("AREA", "Strip", (x, 0.4, VAULT_TOP - 0.1), 260, size=1.6, rot=(0, 0, 0))
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
    S.OUT = OUT                                   # the bakes land in Assets/Art/Bank
    coll = bpy.context.scene.collection
    M = mats()
    shell = build_shell(coll, M)
    front = build_front(coll, M)
    street = build_street(coll, M)
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
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "bank_set.png")
        return
    for o in (shell, front, street):
        o.select_set(False)
    for o in (shell, front, street):
        S.unwrap(o)
    front.hide_render = True                      # the cut-away front never shades the lane
    S.bake(shell, 4096, 32)
    S.bake(street, 2048, 24)
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
