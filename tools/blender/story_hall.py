"""Chapter 1's main hall: the rest of the warehouse, through the inner shutter of the room he
wakes in. A complete building (four walls, a pitched truss roof, an exterior), 48 m along the
fight lane, 22 m deep, 12 m to the ridge, laid out in four fight zones, each with its kit:

  ZONE 1  x 14.3-26  THE LOADING BAY      dock doors, pallet stacks, a trailer, a forklift with
                                          its forks raised over the lane (a platform, 1.62 m)
  ZONE 2  x 26-38    THE RACKING AISLE    two pallet racks across the lane (platforms at 2.2 and
                                          4.4 m), more racks receding, a monorail chain hoist
  ZONE 3  x 38-50    THE MEZZANINE        a steel deck over the lane (3.4 m), stairs up at 37.6,
                                          the site office behind its glass, workshop below
  ZONE 4  x 50-62    THE CRANE BAY        the gantry crane, a torn skylight over a puddle, the big
                                          main door to the yard (open), presses, drums, coils

Blender X = Unity X (along the lane), Blender Y = Unity Z (depth), Blender Z = Unity Y (up). The
lane is y = 0. Called from build_story_set.py, which owns the materials, helpers and bake.

Objects:
  SetHall       the hall, inside and out (one bake)
  HallFront     the walls on the camera side (the hall's and the store's): hidden by the game
                whenever the camera is outside them, so the side-on fight sees in
  InnerShutter  the roll-up door between the store and the hall (moves)
"""
import math
import random

from mathutils import Vector

HALL_X0, HALL_X1 = 14.3, 62.0
HALL_F, HALL_B = -1.6, 20.4          # inner faces of the front (camera side) and back (yard side) walls
WALL_T = 0.35
EAVE, RIDGE = 9.0, 12.0
RIDGE_Y = (HALL_F + HALL_B) / 2
SKYLIGHTS = [(19.5, 22.0), (31.0, 33.5), (43.0, 45.5), (54.5, 57.5)]   # the last one is torn
TORN = (54.5, 57.5)
FORK = (19.6, 21.2, 1.62)            # the forklift's raised forks: a platform (x0, x1, top)
RACKS = [(27.0, 31.0), (33.0, 37.0)]
RACK_LEVELS = (2.2, 4.4)
MEZZ = (40.0, 50.0, 3.4)
STAIR_X0 = 37.6
GANTRY_X = 56.0
EXIT = (-1.0, 4.6, 6.5)              # the main door in the east wall: y0, y1, height
INNER = (-0.9, 1.9, 4.0)             # the inner shutter in the store's end wall: y0, y1, height
STORE_HALF, STORE_FRONT, STORE_BACK, STORE_ROOF = 14.0, -1.0, 2.6, 7.2

rnd = random.Random(23)


# ---------------------------------------------------------------- materials of its own

def hall_materials(S, M):
    """Brick, painted rack steel, shrink wrap, office light, cardboard (procedural brick bakes
    into the lightmap with the light)."""
    import bpy
    if "brick" not in M:
        m = bpy.data.materials.new("Brick")
        m.use_nodes = True
        nt = m.node_tree
        bsdf = nt.nodes["Principled BSDF"]
        bsdf.inputs["Roughness"].default_value = 0.95
        tc = nt.nodes.new("ShaderNodeTexCoord")
        sep = nt.nodes.new("ShaderNodeSeparateXYZ")
        add = nt.nodes.new("ShaderNodeMath")
        add.operation = "ADD"
        comb = nt.nodes.new("ShaderNodeCombineXYZ")
        br = nt.nodes.new("ShaderNodeTexBrick")
        br.inputs["Scale"].default_value = 1.0
        if getattr(S, "TOWER", False):
            # the tower's walls: pale cast concrete in formwork panels, thin dark seams
            br.offset = 0.0
            br.inputs["Mortar Size"].default_value = 0.012
            br.inputs["Brick Width"].default_value = 2.4
            br.inputs["Row Height"].default_value = 1.2
            br.inputs["Color1"].default_value = (0.40, 0.40, 0.41, 1)
            br.inputs["Color2"].default_value = (0.35, 0.35, 0.36, 1)
            br.inputs["Mortar"].default_value = (0.14, 0.14, 0.15, 1)
        else:
            br.inputs["Mortar Size"].default_value = 0.012
            br.inputs["Brick Width"].default_value = 0.25
            br.inputs["Row Height"].default_value = 0.075
            br.inputs["Color1"].default_value = (0.30, 0.13, 0.08, 1)
            br.inputs["Color2"].default_value = (0.22, 0.10, 0.07, 1)
            br.inputs["Mortar"].default_value = (0.22, 0.21, 0.2, 1)
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 0.6
        mixc = nt.nodes.new("ShaderNodeMixRGB")
        mixc.blend_type = "MULTIPLY"
        mixc.inputs["Fac"].default_value = 0.35 if getattr(S, "TOWER", False) else 0.55
        nt.links.new(tc.outputs["Object"], sep.inputs[0])
        nt.links.new(sep.outputs["X"], add.inputs[0])
        nt.links.new(sep.outputs["Y"], add.inputs[1])
        nt.links.new(add.outputs[0], comb.inputs["X"])
        nt.links.new(sep.outputs["Z"], comb.inputs["Y"])
        nt.links.new(comb.outputs[0], br.inputs["Vector"])
        nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
        nt.links.new(br.outputs["Color"], mixc.inputs["Color1"])
        nt.links.new(noise.outputs["Color"], mixc.inputs["Color2"])
        nt.links.new(mixc.outputs[0], bsdf.inputs["Base Color"])
        M["brick"] = m
    M.setdefault("rack_blue", S.mat("RackBlue", (0.06, 0.14, 0.32), 0.6, metal=0.2))
    M.setdefault("rack_orange", S.mat("RackOrange", (0.62, 0.22, 0.04), 0.6, metal=0.2))
    M.setdefault("wrap", S.mat("ShrinkWrap", (0.5, 0.52, 0.55), 0.25))
    M.setdefault("card", S.mat("Cardboard", (0.42, 0.30, 0.17), 0.95))
    M.setdefault("card_dark", S.mat("CardboardDark", (0.30, 0.21, 0.12), 0.95))
    M.setdefault("tube_cold", S.mat("TubeCold", (0.9, 0.95, 1.0), 0.4, emit=(0.75, 0.88, 1.0), strength=18.0))
    M.setdefault("checker", S.mat("CheckerPlate", (0.26, 0.27, 0.29), 0.35, metal=0.6))
    M.setdefault("tarp", S.mat("Tarp", (0.10, 0.18, 0.28), 0.8))
    M.setdefault("graffiti_red", S.mat("GraffitiRed", (0.55, 0.06, 0.06), 0.8))
    M.setdefault("graffiti_white", S.mat("GraffitiWhite", (0.62, 0.62, 0.6), 0.8))
    M.setdefault("puddle_hall", S.mat("PuddleHall", (0.11, 0.12, 0.15), 0.08))


# ---------------------------------------------------------------- pieces

def gable(g, x0, x1, m):
    """The triangle of wall under the roof at an end of the hall (between eave and ridge)."""
    import bmesh
    import bpy
    tmp = bmesh.new()
    y0, y1 = HALL_F - WALL_T, HALL_B + WALL_T
    pts = [(y0, EAVE), (y1, EAVE), (RIDGE_Y, RIDGE + 0.3)]
    f0 = [tmp.verts.new((x0, y, z)) for y, z in pts]
    f1 = [tmp.verts.new((x1, y, z)) for y, z in pts]
    tmp.faces.new(f0)
    tmp.faces.new(list(reversed(f1)))
    for i in range(3):
        tmp.faces.new((f0[i], f0[(i + 1) % 3], f1[(i + 1) % 3], f1[i]))
    g._merge(tmp, m)


def slope(g, M, x0, x1, back, frac0, frac1, m=None):
    """A strip of one roof slope between x0 and x1, from frac0 to frac1 of the way up it."""
    ye = HALL_B + WALL_T if back else HALL_F - WALL_T
    a = Vector((0, ye + (RIDGE_Y - ye) * frac0, EAVE + (RIDGE - EAVE) * frac0 + 0.2))
    b = Vector((0, ye + (RIDGE_Y - ye) * frac1, EAVE + (RIDGE - EAVE) * frac1 + 0.2))
    a.x = b.x = (x0 + x1) / 2
    g.beam(a[:], b[:], x1 - x0, 0.12, m or M["iron_dark"])


def roof(g, M):
    """Two corrugated slopes on the trusses, broken by skylight bands; one skylight torn open."""
    cuts = sorted(SKYLIGHTS)
    edges = [HALL_X0 - 0.3]
    for a, b in cuts:
        edges += [a, b]
    edges.append(HALL_X1 + WALL_T + 0.3)
    for back in (False, True):
        for i in range(0, len(edges), 2):
            slope(g, M, edges[i], edges[i + 1], back, 0.0, 1.0)
        for a, b in cuts:
            slope(g, M, a, b, back, 0.0, 0.38)
            slope(g, M, a, b, back, 0.72, 1.0)
            ye = HALL_B + WALL_T if back else HALL_F - WALL_T
            za, zb = EAVE + (RIDGE - EAVE) * 0.38 + 0.2, EAVE + (RIDGE - EAVE) * 0.72 + 0.2
            ya, yb = ye + (RIDGE_Y - ye) * 0.38, ye + (RIDGE_Y - ye) * 0.72
            torn = (a, b) == TORN and not back
            # the frame: glazing bars along and across
            for k in range(4):
                xx = a + (b - a) * k / 3
                g.beam((xx, ya, za), (xx, yb, zb), 0.06, 0.08, M["steel"])
            for f in (0.0, 0.5, 1.0):
                g.beam((a, ya + (yb - ya) * f, za + (zb - za) * f), (b, ya + (yb - ya) * f, za + (zb - za) * f), 0.06, 0.06, M["steel"])
            if not torn:
                g.beam(((a + b) / 2, ya, za - 0.02), ((a + b) / 2, yb, zb - 0.02), b - a, 0.012, M["glass"])
            else:
                # torn: the glass gone but for teeth along the frame, one pane hanging by a corner
                for k in range(14):
                    xx = rnd.uniform(a + 0.1, b - 0.1)
                    f = rnd.choice((0.02, 0.48, 0.52, 0.98))
                    py, pz = ya + (yb - ya) * f, za + (zb - za) * f
                    s = rnd.uniform(0.12, 0.28)
                    g.box((xx, py, pz - 0.03), (s, s * 0.6, 0.008), M["glass"], rot=(rnd.uniform(-25, 25), rnd.uniform(-20, 20), rnd.uniform(0, 90)))
                g.box((a + 0.6, ya + 0.4, za - 0.55), (0.9, 0.012, 0.75), M["glass"], rot=(70, 8, 4))


def truss(g, M, x):
    """A pitched steel truss across the depth (bottom chord at the eaves, top chords under the
    slopes), with verticals and diagonals."""
    y0, y1 = HALL_F, HALL_B
    g.beam((x, y0, EAVE), (x, y1, EAVE), 0.12, 0.14, M["steel"])
    n = 10
    for side in (0, 1):
        ya, yb = (y0, RIDGE_Y) if side == 0 else (y1, RIDGE_Y)
        g.beam((x, ya, EAVE + 0.05), (x, yb, RIDGE - 0.05), 0.12, 0.14, M["steel"])
        for i in range(1, n // 2 + 1):
            f = i / (n // 2)
            y = ya + (yb - ya) * f
            zt = EAVE + (RIDGE - EAVE) * f - 0.05
            g.beam((x, y, EAVE), (x, y, zt), 0.07, 0.07, M["steel"])
            fp = (i - 1) / (n // 2)
            yp = ya + (yb - ya) * fp
            g.beam((x, yp, EAVE), (x, y, zt), 0.06, 0.06, M["steel"])


def column(S, g, M, x, y, h=EAVE):
    S.ibeam(g, (x, y, 0.0), h, M)


TOWER = False          # set by build_story_set: the fight-mode tower's palette and light


def lamp(g, M, x, y, z, working, cold=False):
    working = working or TOWER                 # the tower's look: every lamp lit, a pool under each
    g.beam((x, y, EAVE), (x, y, z + 0.18), 0.015, 0.015, M["steel"])
    g.cyl((x, y, z + 0.08), 0.06, 0.18, M["steel_mid"], n=10)
    g.cyl((x, y, z), 0.42, 0.24, M["paint_green"], n=20, r2=0.07)
    bulb = (M["tube_cold"] if cold else M["bulb_warm"]) if working else M["bulb_dead"]
    g.cyl((x, y, z - 0.13), 0.07, 0.1, bulb, n=10)


def boxes_on(g, M, x0, x1, y0, y1, z, fill=0.7, hmax=0.9):
    """Stock on a shelf or a pallet: cartons and wrapped loads, leaving gaps."""
    x = x0 + 0.05
    while x < x1 - 0.3:
        w = rnd.uniform(0.6, 1.1)
        if x + w > x1:
            w = x1 - x - 0.05
        if rnd.random() < fill and w > 0.3:
            h = rnd.uniform(0.35, hmax)
            if rnd.random() < 0.35:
                g.box((x + w / 2, (y0 + y1) / 2, z + h / 2), (w, y1 - y0, h), rnd.choice((M["wrap"], M["tarp"])))
            else:
                k = 0
                zz = z
                while zz < z + h - 0.2 and k < 3:
                    bh = rnd.uniform(0.25, 0.4)
                    g.box((x + w / 2 + rnd.uniform(-0.03, 0.03), (y0 + y1) / 2, zz + bh / 2), (w - 0.04, (y1 - y0) - 0.04, bh), rnd.choice((M["card"], M["card"], M["card_dark"])))
                    zz += bh
                    k += 1
        x += w + 0.08


def rack(g, M, x0, x1, y0, y1, levels, stock_front=False):
    """A pallet rack: blue uprights with bracing, orange beams, decks, stock."""
    xs = [x0, x1] if x1 - x0 < 5 else [x0 + i * (x1 - x0) / round((x1 - x0) / 2.7) for i in range(round((x1 - x0) / 2.7) + 1)]
    top = levels[-1] + 1.4
    for x in xs:
        for y in (y0, y1):
            g.box((x, y, top / 2), (0.09, 0.09, top), M["rack_blue"])
        z = 0.3
        while z < top - 0.4:                                                                 # frame bracing
            g.beam((x, y0, z), (x, y1, z + 0.55), 0.04, 0.04, M["rack_blue"])
            z += 0.55
        g.box((x, (y0 + y1) / 2, 0.02), (0.2, y1 - y0 + 0.2, 0.04), M["steel_mid"])          # foot plates
    for z in levels:
        for y in (y0, y1):
            g.box(((x0 + x1) / 2, y, z - 0.07), (x1 - x0, 0.06, 0.12), M["rack_orange"])
        g.box(((x0 + x1) / 2, (y0 + y1) / 2, z - 0.02), (x1 - x0, y1 - y0, 0.03), M["steel_mid"])   # deck
        if stock_front:
            boxes_on(g, M, x0, x1, y0 + 0.05, y1 - 0.05, z, 0.75)
        else:
            boxes_on(g, M, x0, x1, y0 + (y1 - y0) * 0.62, y1 - 0.05, z, 0.85)               # stock at the back: room to stand
    boxes_on(g, M, x0, x1, y0 + (y1 - y0) * 0.55, y1 - 0.05, 0.0, 0.6, 1.2)


def trailer(g, M, x, y, length=11.0):
    """A box trailer backed up to the dock: body, ribs, wheels, landing legs."""
    g.box((x, y, 2.35), (length, 2.5, 2.7), M["iron"])
    for k in range(int(length / 0.6)):
        g.box((x - length / 2 + 0.3 + k * 0.6, y - 1.27, 2.35), (0.05, 0.03, 2.7), M["iron_dark"])
    g.box((x, y, 0.9), (length, 2.2, 0.2), M["steel"])
    for dx in (length / 2 - 1.4, length / 2 - 2.6):
        for dy in (-1.0, 1.0):
            g.cyl((x + dx, y + dy, 0.5), 0.5, 0.3, M["rubber"], rot=(90, 0, 0), n=16)
    for dy in (-0.9, 0.9):
        g.box((x - length / 2 + 1.5, y + dy, 0.5), (0.1, 0.1, 0.8), M["steel_mid"])


# ---------------------------------------------------------------- the hall

def build_hall(S, coll, M):
    hall_materials(S, M)
    g = S.Group("SetHall")
    # floor: slab, joints, painted lines and bays, oil, puddles, debris
    g.box(((HALL_X0 + HALL_X1) / 2, (HALL_F + HALL_B) / 2, -0.1), (HALL_X1 - HALL_X0 + 0.8, HALL_B - HALL_F + 0.9, 0.2), M["concrete"])
    x = HALL_X0 + 4
    while x < HALL_X1:
        g.box((x, (HALL_F + HALL_B) / 2, 0.002), (0.03, HALL_B - HALL_F, 0.004), M["concrete_dark"])
        x += 4
    for y in (4.0, 8.0, 12.0, 16.0):
        g.box(((HALL_X0 + HALL_X1) / 2, y, 0.002), (HALL_X1 - HALL_X0, 0.03, 0.004), M["concrete_dark"])
    g.box(((HALL_X0 + HALL_X1) / 2, 2.0, 0.003), (HALL_X1 - HALL_X0 - 1, 0.1, 0.006), M["paint_yellow"])     # walkway line
    g.box(((HALL_X0 + HALL_X1) / 2, -1.2, 0.003), (HALL_X1 - HALL_X0 - 1, 0.1, 0.006), M["paint_yellow"])
    for zx in (26.0, 38.0, 50.0):                                                                 # zone hatching
        for i in range(8):
            g.box((zx - 0.9 + i * 0.26, 0.4, 0.003), (0.1, 1.6, 0.006), M["paint_yellow"], rot=(0, 0, 35))
    for _ in range(22):
        g.cyl((rnd.uniform(HALL_X0 + 1, HALL_X1 - 1), rnd.uniform(1.0, HALL_B - 1), 0.002), rnd.uniform(0.3, 1.0), 0.004, M["oil"], n=16)
    for (px, py, r) in ((23.0, 0.3, 0.7), (35.0, -0.2, 0.55), (47.0, 0.4, 0.6), (56.0, 0.1, 1.3), (59.5, 0.5, 0.6)):
        g.cyl((px, py, 0.003), r, 0.006, M["puddle_hall"], n=22)                                  # water from the roof
    for _ in range(40):                                                                           # debris: paper, splinters
        g.box((rnd.uniform(HALL_X0 + 1, HALL_X1 - 1), rnd.uniform(-1.0, HALL_B - 1), 0.006), (rnd.uniform(0.1, 0.35), rnd.uniform(0.05, 0.25), 0.008),
              rnd.choice((M["card"], M["wood"], M["graffiti_white"])), rot=(0, 0, rnd.uniform(0, 180)))

    # walls: the back (yard side) and the east end, brick on a concrete plinth; the gables
    g.box(((HALL_X0 + HALL_X1) / 2, HALL_B + WALL_T / 2, EAVE / 2), (HALL_X1 - HALL_X0 + WALL_T, WALL_T, EAVE), M["brick"])
    g.box(((HALL_X0 + HALL_X1) / 2, HALL_B - 0.02, 0.45), (HALL_X1 - HALL_X0, 0.08, 0.9), M["concrete_dark"])
    ex = HALL_X1 + WALL_T / 2
    y0w, y1w = HALL_F - WALL_T, HALL_B + WALL_T
    g.box((ex, (y0w + EXIT[0]) / 2, EAVE / 2), (WALL_T, EXIT[0] - y0w, EAVE), M["brick"])
    g.box((ex, (EXIT[1] + y1w) / 2, EAVE / 2), (WALL_T, y1w - EXIT[1], EAVE), M["brick"])
    g.box((ex, (EXIT[0] + EXIT[1]) / 2, (EXIT[2] + EAVE) / 2), (WALL_T, EXIT[1] - EXIT[0], EAVE - EXIT[2]), M["brick"])
    g.box((ex - 0.2, (EXIT[0] + EXIT[1]) / 2, EXIT[2] + 0.3), (0.4, EXIT[1] - EXIT[0] + 0.4, 0.6), M["steel"])          # the door drum
    for yy in (EXIT[0], EXIT[1]):
        g.box((ex - 0.2, yy, EXIT[2] / 2), (0.25, 0.15, EXIT[2]), M["steel"])                    # guides
    # the west wall (shared with the store's end): above the store's roof, and behind it
    wx = HALL_X0 + WALL_T / 2
    g.box((wx, (STORE_BACK + 0.4 + y1w) / 2, EAVE / 2), (WALL_T, y1w - STORE_BACK - 0.4, EAVE), M["brick"])
    g.box((wx, (y0w + STORE_BACK + 0.4) / 2, (STORE_ROOF + 0.45 + EAVE) / 2), (WALL_T, STORE_BACK + 0.4 - y0w, EAVE - STORE_ROOF - 0.45), M["brick"])
    for gx0, gx1 in ((HALL_X0, HALL_X0 + WALL_T), (HALL_X1, HALL_X1 + WALL_T)):
        gable(g, gx0, gx1, M["brick"])
    # the back wall from inside: steel columns, high windows (some broken), the dock doors
    x = HALL_X0 + 2.7
    while x < HALL_X1:
        column(S, g, M, x, HALL_B - 0.2)
        x += 6.0
    for k, cx in enumerate((17.0, 23.0, 29.0, 35.0, 41.0, 47.0, 53.0, 59.0)):
        z0, z1, w = 5.2, 8.2, 3.2
        for zz in (z0, z1):
            g.box((cx, HALL_B - 0.05, zz), (w + 0.14, 0.16, 0.12), M["steel"])
        for xx in (cx - w / 2, cx + w / 2):
            g.box((xx, HALL_B - 0.05, (z0 + z1) / 2), (0.12, 0.16, z1 - z0), M["steel"])
        for i in range(4):
            for j in range(3):
                px, pz = cx - w / 2 + (i + 0.5) * w / 4, z0 + (j + 0.5) * (z1 - z0) / 3
                if rnd.random() < 0.18:
                    g.box((px, HALL_B - 0.06, pz), (w / 4 - 0.08, 0.02, (z1 - z0) / 3 - 0.08), M["wood_dark"])          # boarded
                elif rnd.random() < 0.2:
                    continue                                                                                          # broken out
                else:
                    g.box((px, HALL_B - 0.06, pz), (w / 4 - 0.06, 0.012, (z1 - z0) / 3 - 0.06), M["glass"])
        for i in range(1, 4):
            g.box((cx - w / 2 + i * w / 4, HALL_B - 0.07, (z0 + z1) / 2), (0.04, 0.05, z1 - z0), M["steel"])
        for j in range(1, 3):
            g.box((cx, HALL_B - 0.07, z0 + j * (z1 - z0) / 3), (w, 0.05, 0.04), M["steel"])
    for dx in (18.0, 24.0):                                                                       # dock doors, closed
        z = 0.1
        while z < 4.2:
            g.box((dx, HALL_B - 0.1, z + 0.05), (3.6, 0.05, 0.09), M["iron"] if rnd.random() > 0.15 else M["rust"])
            z += 0.1
        g.box((dx, HALL_B - 0.3, 4.5), (4.2, 0.5, 0.5), M["steel"])
        S.stencil(g, "DOCK %d" % (1 if dx < 20 else 2), (dx, HALL_B - 0.14, 4.9), 0.4, M["paint_yellow"])
    # graffiti and stencils inside
    S.stencil(g, "BAY 7", (31.0, HALL_B - 0.05, 3.4), 1.2, M["paint_yellow"])
    S.stencil(g, "NO SMOKING", (44.0, HALL_B - 0.05, 2.2), 0.42, M["paint_red"])
    S.stencil(g, "THEY KNOW", (52.0, HALL_B - 0.05, 1.6), 0.6, M["graffiti_red"], rot_z=-4)
    S.stencil(g, "CRANE BAY  -  SWL 10T", (56.0, HALL_B - 0.05, 4.4), 0.38, M["paint_yellow"])
    # pipes along the back wall
    for z, r in ((3.1, 0.1), (3.45, 0.06)):
        g.cyl(((HALL_X0 + HALL_X1) / 2, HALL_B - 0.25, z), r, HALL_X1 - HALL_X0 - 0.5, M["steel_mid"] if r > 0.07 else M["rust"], rot=(0, 90, 0), n=12)
    # roof and its structure
    roof(g, M)
    x = HALL_X0 + 2.7
    while x < HALL_X1:
        truss(g, M, x)
        x += 6.0
    for f in (0.15, 0.45, 0.75):                                                                  # purlins
        for back in (False, True):
            ye = HALL_B if back else HALL_F
            y, z = ye + (RIDGE_Y - ye) * f, EAVE + (RIDGE - EAVE) * f + 0.02
            g.box(((HALL_X0 + HALL_X1) / 2, y, z), (HALL_X1 - HALL_X0, 0.1, 0.16), M["steel_mid"])

    # ------------------------------------------------------------ ZONE 1: the loading bay
    S.forklift(g, (FORK[0] + FORK[1]) / 2, 2.1, M, -90, fork_z=FORK[2] - 0.14)
    S.pallet(g, ((FORK[0] + FORK[1]) / 2, 0.55, FORK[2] - 0.12), M)                              # the pallet on the forks
    trailer(g, M, 20.5, 17.4)
    for (px, py, n) in ((16.2, 5.0, 3), (17.6, 5.2, 2), (24.5, 4.8, 4), (16.0, 9.0, 2), (25.0, 9.5, 3), (22.5, 12.0, 2)):
        for k in range(n):
            S.pallet(g, (px, py, k * 0.15), M)
        boxes_on(g, M, px - 0.6, px + 0.6, py - 0.5, py + 0.5, n * 0.15, 0.9, 1.3)
    for (rx, ry) in ((15.6, 2.4), (16.4, 2.5), (25.2, 2.6)):                                       # roll cages
        g.box((rx, ry, 0.9), (0.7, 0.8, 0.02), M["steel_mid"])
        for dz in (0.1, 1.8):
            g.box((rx, ry, dz), (0.72, 0.82, 0.03), M["steel_mid"])
        for dx in (-0.35, 0.35):
            for dy in (-0.4, 0.4):
                g.box((rx + dx, ry + dy, 0.95), (0.03, 0.03, 1.8), M["steel_mid"])
        boxes_on(g, M, rx - 0.33, rx + 0.33, ry - 0.36, ry + 0.36, 0.12, 0.8, 1.4)
    lamp(g, M, 18.0, 6.0, 6.2, True)
    lamp(g, M, 24.0, 1.2, 6.2, False)

    # ------------------------------------------------------------ ZONE 2: the racking aisle
    for (x0, x1) in RACKS:
        rack(g, M, x0, x1, -0.75, 1.15, RACK_LEVELS)
    for ry in (4.0, 6.6, 9.8, 12.4, 15.6):                                                         # racks receding
        rack(g, M, 26.6, 37.4, ry, ry + 1.1, (2.2, 4.4, 6.6), stock_front=True)
    g.box((32.0, 0.0, 8.0), (12.4, 0.12, 0.3), M["steel"])                                          # monorail I-beam
    for hx in (27.0, 32.0, 37.0):
        g.beam((hx, 0.0, 8.15), (hx, 0.0, EAVE), 0.05, 0.05, M["steel"])
    g.box((32.0, 0.0, 7.72), (0.5, 0.35, 0.3), M["paint_yellow"])                                  # hoist trolley
    S.stencil(g, "AISLE B", (32.0, HALL_B - 0.05, 2.6), 0.5, M["paint_yellow"])
    lamp(g, M, 32.0, 2.6, 6.4, True)

    # ------------------------------------------------------------ ZONE 3: the mezzanine office
    mx0, mx1, mz = MEZZ
    md0, md1 = -0.85, 3.4                                                                           # deck depth (over the lane)
    g.box(((mx0 + mx1) / 2, (md0 + md1) / 2, mz - 0.06), (mx1 - mx0, md1 - md0, 0.12), M["checker"])
    g.box(((mx0 + mx1) / 2, md0, mz - 0.22), (mx1 - mx0, 0.12, 0.32), M["steel"])                   # edge beam
    for x in (mx0 + 0.2, (mx0 + mx1) / 2, mx1 - 0.2):
        column(S, g, M, x, md1 - 0.2, mz - 0.2)
        g.beam((x, md1 - 0.2, mz - 1.4), (x, md0 + 0.4, mz - 0.25), 0.1, 0.1, M["steel"])            # cantilever braces
    x = mx0
    while x <= mx1 + 0.01:                                                                          # railing, front edge
        g.box((x, md0 + 0.05, mz + 0.5), (0.05, 0.05, 1.0), M["paint_yellow"])
        x += 1.25
    for zz in (mz + 0.55, mz + 1.0):
        g.box(((mx0 + mx1) / 2, md0 + 0.05, zz), (mx1 - mx0, 0.04, 0.04), M["paint_yellow"])
    n = 12                                                                                          # stairs up from the floor
    for i in range(n):
        f = (i + 0.5) / n
        g.box((STAIR_X0 + (mx0 - STAIR_X0) * f, 0.2, mz * f), ((mx0 - STAIR_X0) / n + 0.05, 1.2, 0.05), M["checker"])
    for sy in (-0.42, 0.82):
        g.beam((STAIR_X0, sy, 0.0), (mx0, sy, mz), 0.06, 0.22, M["steel"])
        g.beam((STAIR_X0, sy, 1.0), (mx0, sy, mz + 1.0), 0.04, 0.04, M["paint_yellow"])
    # the office: walls behind the lane, windows onto it, a desk, cabinets, the strip light on
    ox0, ox1, oy0, oy1, oh = 45.5, 50.0, 0.9, 3.3, 2.6
    for xx, w in ((ox0 + 0.4, 0.8), (ox1 - 0.4, 0.8)):
        g.box((xx, oy0, mz + oh / 2), (w, 0.1, oh), M["block"])
    g.box(((ox0 + ox1) / 2, oy0, mz + 0.45), (ox1 - ox0, 0.1, 0.9), M["block"])
    g.box(((ox0 + ox1) / 2, oy0, mz + oh - 0.15), (ox1 - ox0, 0.1, 0.3), M["block"])
    # (the window's glass is the game's own, breakable: OfficeWindow. The opening is left clear here)
    g.box(((ox0 + ox1) / 2, oy1, mz + oh / 2), (ox1 - ox0, 0.1, oh), M["block"])
    for xx in (ox0, ox1):
        g.box((xx, (oy0 + oy1) / 2, mz + oh / 2), (0.1, oy1 - oy0, oh), M["block"])
    g.box(((ox0 + ox1) / 2, (oy0 + oy1) / 2, mz + oh), (ox1 - ox0, oy1 - oy0, 0.08), M["iron_dark"])
    g.box((47.0, 2.4, mz + 0.75), (1.6, 0.8, 0.05), M["wood"])
    g.box((47.0, 2.4, mz + 0.37), (1.5, 0.7, 0.74), M["wood_dark"])
    for k in range(3):
        g.box((49.4, 1.6 + k * 0.5, mz + 0.65), (0.45, 0.45, 1.3), M["steel_mid"])
    g.box((47.8, 2.0, mz + oh - 0.08), (1.4, 0.15, 0.06), M["tube_cold"])
    S.stencil(g, "SITE OFFICE", ((ox0 + ox1) / 2, oy0 - 0.06, mz + oh - 0.15), 0.22, M["graffiti_white"])
    # the workshop under the deck
    S.lathe(g, 42.0, 3.0, M)
    for k in range(4):
        S.drum(g, (43.6 + k * 0.62, 6.0, 0.44), M, M["paint_blue"] if k % 2 else M["paint_red"])
    lamp(g, M, 44.0, 1.4, 6.6, False)

    # ------------------------------------------------------------ ZONE 4: the crane bay
    for y in (HALL_F + 0.25, HALL_B - 0.3):                                                         # crane rails on the columns
        g.box((50.0 + (HALL_X1 - 50.0) / 2, y, 7.55), (HALL_X1 - 50.0, 0.3, 0.3), M["steel"])
    g.box((GANTRY_X, (HALL_F + HALL_B) / 2, 7.95), (0.7, HALL_B - HALL_F - 0.4, 0.7), M["crane"])   # the bridge girder
    for y in (HALL_F + 0.25, HALL_B - 0.3):
        g.box((GANTRY_X, y, 7.75), (2.4, 0.5, 0.45), M["crane"])                                  # end trucks
    g.box((GANTRY_X, 0.0, 7.45), (1.0, 1.2, 0.5), M["crane"])                                      # trolley over the lane
    g.cyl((GANTRY_X, 0.0, 7.2), 0.25, 0.9, M["steel_mid"], rot=(0, 90, 0), n=14)                   # hoist drum
    S.stencil(g, "10 T", (GANTRY_X, HALL_F + 3.0, 7.95), 0.45, M["steel"])
    S.press(g, 58.5, 6.0, M)
    for (cx, cy) in ((52.0, 7.0), (53.0, 9.5), (60.0, 12.0)):                                       # steel coils
        g.cyl((cx, cy, 0.7), 0.7, 1.0, M["steel_mid"], rot=(90, 0, 0), n=20)
        g.cyl((cx, cy, 0.7), 0.32, 1.02, M["steel"], rot=(90, 0, 0), n=14)
    for (dx, dy) in ((51.0, 3.0), (51.7, 3.2), (51.3, 3.8), (60.8, 2.6), (61.2, 3.4)):
        S.drum(g, (dx, dy, 0.44), M, rnd.choice((M["paint_blue"], M["paint_red"], M["steel_mid"])))
    S.drum(g, (52.4, 3.6, 0.29), M, M["paint_red"], lying=True)
    lamp(g, M, 53.0, 1.0, 6.5, True)
    lamp(g, M, 59.5, 2.0, 6.5, False)

    # ------------------------------------------------------------ outside: the yard face of the hall
    yo = HALL_B + WALL_T
    S.stencil(g, "MERIDIAN  COLD  STORAGE", ((HALL_X0 + HALL_X1) / 2, yo + 0.02, 7.3), 1.5, M["graffiti_white"], facing=1)
    S.stencil(g, "UNIT 7", (57.0, yo + 0.02, 5.6), 0.9, M["paint_yellow"], facing=1)
    g.box((24.0, yo + 1.4, 0.6), (16.0, 2.8, 1.2), M["concrete"])                                  # the dock
    for dx in (18.0, 24.0, 30.0):
        g.box((dx, yo + 0.06, 2.2), (3.8, 0.1, 4.4), M["iron_dark"])                               # dock doors, outside
        for side in (-1, 1):
            g.box((dx + side * 1.6, yo + 2.85, 0.75), (0.3, 0.12, 0.4), M["rubber"])                 # bumpers
        g.box((dx, yo + 0.5, 5.0), (0.5, 0.5, 0.15), M["steel"])
        g.box((dx, yo + 0.5, 4.92), (0.4, 0.4, 0.04), M["sodium"])                                  # floodlight
    g.box(((HALL_X0 + HALL_X1) / 2, yo + 0.15, EAVE + 0.2), (HALL_X1 - HALL_X0 + 1, 0.25, 0.2), M["steel_mid"])   # gutter
    for x in (HALL_X0 + 0.5, 38.0, HALL_X1 - 0.3):
        g.cyl((x, yo + 0.15, EAVE / 2), 0.08, EAVE, M["steel_mid"], n=10)
    trailer(g, M, 36.5, yo + 5.5)
    return g.finish(coll)


def build_hall_front(S, coll, M):
    """The camera-side walls: the hall's front (brick, high windows, a door) and the store's
    front (block and corrugated iron). The game hides them while the camera is outside."""
    hall_materials(S, M)
    g = S.Group("HallFront")
    yf = HALL_F - WALL_T / 2
    g.box(((HALL_X0 + HALL_X1) / 2, yf, EAVE / 2), (HALL_X1 - HALL_X0 + WALL_T, WALL_T, EAVE), M["brick"])
    g.box(((HALL_X0 + HALL_X1) / 2, HALL_F + 0.02, 0.45), (HALL_X1 - HALL_X0, 0.08, 0.9), M["concrete_dark"])
    x = HALL_X0 + 2.7
    while x < HALL_X1:
        S.ibeam(g, (x, HALL_F + 0.2, 0.0), EAVE, M)
        x += 6.0
    for cx in (17.0, 23.0, 29.0, 35.0, 41.0, 47.0, 53.0, 59.0):
        g.box((cx, HALL_F + 0.03, 6.7), (3.2, 0.03, 3.0), M["glass"])
        for xx in (cx - 1.6, cx - 0.53, cx + 0.53, cx + 1.6):
            g.box((xx, HALL_F + 0.05, 6.7), (0.07, 0.07, 3.0), M["steel"])
        for zz in (5.2, 6.2, 7.2, 8.2):
            g.box((cx, HALL_F + 0.05, zz), (3.2, 0.07, 0.07), M["steel"])
    S.stencil(g, "FIRE EXIT", (33.0, HALL_F + 0.05, 2.8), 0.32, M["paint_red"], facing=1)
    # the store's front wall (it had none: the old set was open on the camera side)
    ys = STORE_FRONT - 0.15
    S.block_wall(g, -STORE_HALF, STORE_HALF, 0.0, 1.2, ys, M)
    S.corrugated(g, -STORE_HALF, STORE_HALF, 1.2, STORE_ROOF, ys - 0.05, 1, M)
    return g.finish(coll)


def build_inner_shutter(S, coll, M):
    """The roll-up door in the store's end wall, into the hall: slats across the opening (it
    rolls up into its drum when the scene lifts it)."""
    g = S.Group("InnerShutter")
    x = STORE_HALF + 0.2
    y0, y1, h = INNER
    z = 0.1
    while z < h - 0.02:
        g.box((x, (y0 + y1) / 2, z + 0.045), (0.04, y1 - y0 - 0.08, 0.08), M["iron"] if rnd.random() > 0.15 else M["rust"])
        g.box((x - 0.02, (y0 + y1) / 2, z + 0.088), (0.045, y1 - y0 - 0.08, 0.012), M["iron_dark"])
        z += 0.1
    g.box((x, (y0 + y1) / 2, 0.08), (0.07, y1 - y0 - 0.06, 0.1), M["steel"])
    return g.finish(coll)


def hall_lights(add):
    """The hall's light: moon through the windows and the torn skylight, a few lamps still on,
    the office strip light, floodlights outside the dock, a cold fill so fights read."""
    # the tower's light is pools under lamps with dusk between them: the fills come down, the lamps go up
    k = 1.0                                    # (kept at full: readability first; the lamps add the pools)
    add("AREA", "HallFill", (38.0, -5.0, 5.0), 2400 * k, (0.32, 0.42, 0.8), size=30.0, rot=(math.radians(80), 0, 0))
    add("AREA", "HallRoofFill", (38.0, 9.4, 8.5), 900 * k, (0.45, 0.55, 0.75), size=30.0, rot=(math.radians(180), 0, 0))
    for zx in (20.0, 32.0, 44.0, 56.0):                                                     # each zone's lane, readable
        add("AREA", "HallLaneFill", (zx, -5.5, 5.5), 900 * k, (0.36, 0.46, 0.82), size=10.0, rot=(math.radians(75), 0, 0))
        # and what stands behind it (racks, deck, back wall) lifted off black, so a black figure
        # up on a rack or the mezzanine still reads against it
        add("AREA", "HallWallWash", (zx, 11.0, 6.5), 1600, (0.42, 0.5, 0.78), size=11.0, rot=(math.radians(100), 0, 0))
    add("AREA", "SkylightTorn", ((TORN[0] + TORN[1]) / 2, 0.4, RIDGE - 0.4), 1400, (0.62, 0.72, 1.0), size=3.0)
    lamps = ((18.0, 6.0, 6.0, True), (32.0, 2.6, 6.2, True), (53.0, 1.0, 6.3, True))
    if TOWER:
        lamps += ((24.0, 1.2, 6.0, True), (44.0, 1.4, 6.4, True), (59.5, 2.0, 6.3, True))
    for (x, y, z, warm) in lamps:
        s = add("SPOT", "HallLamp", (x, y, z), 2600 if TOWER else 1700, (1.0, 0.58, 0.24), size=0.15)
        s.data.spot_size = math.radians(95)
        s.data.spot_blend = 0.7
    add("AREA", "OfficeLight", (47.8, 2.1, MEZZ[2] + 2.4), 300, (0.75, 0.88, 1.0), size=1.4, rot=(math.radians(180), 0, 0))
    add("AREA", "ExitYard", (HALL_X1 + 6.0, 1.8, 4.0), 1200, (1.0, 0.62, 0.3), size=6.0, rot=(0, math.radians(-90), 0))
    for dx in (18.0, 24.0, 30.0):
        add("POINT", "DockFlood", (dx, HALL_B + WALL_T + 0.6, 4.8), 1500, (1.0, 0.56, 0.2), size=0.3)
