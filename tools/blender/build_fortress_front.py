"""Kessel Hill's front (docs/story/walkout.md): the way out, used only in the chapter 6 walk-out
and baked only in dawn light. Imported by build_fortress_set.py, which adds these parts to the set.

    FortFacade     the great hall's front: plinth, string courses, stepped buttresses with gablets,
                   octagonal corner turrets with spirelets, traceried pointed windows (two rows),
                   machicolations under a crenellated parapet, gargoyle spouts, clustered chimneys,
                   drainpipes, ivy, wall lanterns, the deep portal of the great door
    FortPorch      the porch before the door: carved columns (spiral fluting, diaper bands),
                   a rib vault, a front arch, a balustraded balcony with urns, statues in niches,
                   the door leaves standing open
    FortForecourt  gravel and a paved path, a parterre of clipped hedges and topiary, the fountain
                   with its knight, benches, lamp posts, the west balustrade and its statues, the
                   east range (the stable block: slate roofs, dormers, chimneys, the clock turret),
                   the gate piers with their beasts and lanterns, the iron gates swung open
    FortBridge     the bridge over the gorge: setts, kerbs, brick parapets with coping, piers with
                   lanterns, four tall arches on slender piers with cutwaters
    FortGorge      the terrace's buttressed retaining wall, the cliffs under the whole hill, the
                   river, rocks, the far bank's moor, its road, dry-stone walls, a lone bent tree

Blender axes: x along the hall, y towards the back (the camera is at -y), z up."""
import math
import random

import bmesh
from mathutils import Matrix, Vector, noise

import detail as D

rnd = random.Random(1907)

UP = 6.0
HALL_X0, HALL_X1 = 128.0, 166.0
FACE_Y = -3.0                 # the hall's front face (the old cut-away wall's outer face)
SKIN = 0.35                   # the new facade's thickness, in front of it
FY = FACE_Y - SKIN            # the facade's outer face
DOOR_X = 147.0
DOOR_W = 3.0                  # the opening
DOOR_SPRING = UP + 4.0
PORCH_Y = -6.6                # the porch's front
COURT_Y1 = -28.8              # the gate piers
COURT_X0, COURT_X1 = 126.0, 178.0
BRIDGE_Y0, BRIDGE_Y1 = -29.6, -60.0
BRIDGE_W = 5.0
RIVER_Z = -12.0
RANGE_X0, RANGE_X1, RANGE_Y0, RANGE_Y1 = 158.0, 172.0, -4.6, -26.6
MAN_DOOR = (158.0, -11.0)
FOUNTAIN = (140.8, -15.0)
BAYS = [(128.0, 133.0), (133.0, 138.5), (138.5, 143.2), (150.8, 155.5), (155.5, 161.0), (161.0, 166.0)]
BUTTRESS_X = [133.0, 138.5, 155.5, 161.0]


def mats():
    M = {}
    M["ash"] = D.stone("FrontAshlar", 0.40, 0.16, "xz", block=(0.9, 0.38))
    M["ash_y"] = D.stone("FrontAshlar", 0.40, 0.16, "yz", block=(0.9, 0.38))
    M["ash_top"] = D.stone("FrontAshlar", 0.40, 0.16, "xy", block=(0.9, 0.38))
    M["dress"] = D.flat("FrontDressed", 0.5, rough=0.8)             # mouldings, carving, copings
    M["dress_dk"] = D.flat("FrontDressedDark", 0.3, rough=0.85)
    M["carve"] = D.flat("FrontCarving", 0.44, rough=0.9)
    M["brick"] = D.stone("FrontBrick", 0.3, 0.17, "yz", block=(0.23, 0.075))
    M["brick_x"] = D.stone("FrontBrick", 0.3, 0.17, "xz", block=(0.23, 0.075))
    M["glass"] = D.flat("FrontGlass", 0.05, rough=0.15)
    M["lead"] = D.flat("FrontLead", 0.12, rough=0.5, metal=0.5)
    M["iron"] = D.flat("FrontIron", 0.06, rough=0.45, metal=0.6)
    M["oak"] = D.planks("FrontOak", 0.12, "xz", board=0.16)
    M["oak_y"] = D.planks("FrontOak", 0.12, "yz", board=0.16)
    M["void"] = D.flat("FrontVoid", 0.01, rough=1.0)
    M["passage"] = D.flat("FrontPassage", 0.1, rough=1.0)
    M["hall_light"] = D.flat("FrontHallLight", 0.5, emit=0.55)
    M["lamp"] = D.flat("FrontLamp", 1.0, emit=7.0)
    M["slate"] = D.planks("FrontSlate", 0.11, "xz", board=0.14)
    M["slate_y"] = D.planks("FrontSlate", 0.11, "yz", board=0.14)
    M["setts"] = D.cobbles("FrontSetts", 0.3, wet=0.35)
    M["flags"] = D.stone("FrontFlags", 0.36, 0.14, "xy", block=(0.8, 0.6))
    M["gravel"] = gravel("FrontGravel", 0.42)
    M["hedge"] = foliage("FrontHedge", 0.11)
    M["ivy"] = foliage("FrontIvy", 0.08)
    M["grass"] = foliage("FrontMoor", 0.2, scale=0.9)
    M["heather"] = foliage("FrontHeather", 0.13, scale=3.0)
    M["water"] = D.flat("FrontWater", 0.035, rough=0.05)
    M["river"] = D.flat("FrontRiver", 0.18, rough=0.2)
    M["rock"] = D.rock("FrontRock", 0.24, "xz")
    M["rock_y"] = D.rock("FrontRock", 0.24, "yz")
    M["rock_top"] = D.rock("FrontRock", 0.22, "xy")
    M["bark"] = D.flat("FrontBark", 0.07)
    M["white"] = D.flat("FrontClockFace", 0.85, rough=0.6)
    return M


def gravel(name, g):
    key = (name, "xy")
    if key in D.MATS:
        return D.MATS[key]
    m, nt, bsdf = D._base(name)
    co = D._coords(nt, "xy", 14.0)
    vo = nt.nodes.new("ShaderNodeTexVoronoi")
    nt.links.new(co, vo.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = D._grey(g * 0.45)
    ramp.color_ramp.elements[1].position = 0.9
    ramp.color_ramp.elements[1].color = D._grey(g)
    nt.links.new(vo.outputs["Distance"], ramp.inputs["Fac"])
    big = nt.nodes.new("ShaderNodeTexNoise")
    big.inputs["Scale"].default_value = 0.05
    nt.links.new(co, big.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.45
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(big.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.4
    nt.links.new(vo.outputs["Distance"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.95
    D.MATS[key] = m
    return m


def foliage(name, g, scale=6.0):
    """Leaves: clumped noise light and dark, a deep bump (hedges, ivy, heather, moor grass)."""
    key = (name, "obj")
    if key in D.MATS:
        return D.MATS[key]
    m, nt, bsdf = D._base(name)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    nz = nt.nodes.new("ShaderNodeTexVoronoi")
    nz.inputs["Scale"].default_value = scale * 2.5
    nt.links.new(tc.outputs["Object"], nz.inputs["Vector"])
    n2 = nt.nodes.new("ShaderNodeTexNoise")
    n2.inputs["Scale"].default_value = scale
    n2.inputs["Detail"].default_value = 8.0
    nt.links.new(tc.outputs["Object"], n2.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.25
    ramp.color_ramp.elements[0].color = D._grey(g * 0.4)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = D._grey(g * 1.6)
    nt.links.new(n2.outputs["Fac"], ramp.inputs["Fac"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.6
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(nz.outputs["Distance"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.9
    nt.links.new(nz.outputs["Distance"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.95
    D.MATS[key] = m
    return m


# ---------------------------------------------------------------- shape helpers

def pointed(cx, zs, a, k=1.6, n=10):
    """A pointed arch over an opening 2a wide springing at zs: two arcs of radius k*a whose
    centres sit k*a - a inside the opposite jamb. Points from the left springing to the apex to
    the right springing, as (x, z)."""
    R = k * a
    left, right = [], []
    c = R - a
    top = math.acos(c / R)                 # the angle (from horizontal) at the apex
    for i in range(n + 1):
        t = top * i / n
        # the left arc is centred to the right of the centre line
        left.append((cx + c - R * math.cos(t), zs + R * math.sin(t)))
        right.append((cx - c + R * math.cos(t), zs + R * math.sin(t)))
    return left + list(reversed(right))[1:]


def ring(g, pts, y, depth, th, m, out=1.0, axis="x"):
    """Voussoirs along an arch outline: each pair of points a block th thick laid outwards.
    axis "x": the outline is (x, z) and the wall stands at y; "y": the outline is (y, z), the wall at x=y."""
    for i in range(len(pts) - 1):
        (x0, z0), (x1, z1) = pts[i], pts[i + 1]
        dx, dz = x1 - x0, z1 - z0
        L = math.hypot(dx, dz)
        if L < 1e-6:
            continue
        nx, nz = -dz / L, dx / L               # outward normal for a left-to-right walk over the top
        cx, cz = (x0 + x1) / 2 + nx * th * 0.5 * out, (z0 + z1) / 2 + nz * th * 0.5 * out
        ang = math.degrees(math.atan2(dz, dx))
        if axis == "x":
            g.box((cx, y, cz), (L * 1.04, depth, th), m, rot=(0, -ang, 0))
        else:
            g.box((y, cx, cz), (depth, L * 1.04, th), m, rot=(ang, 0, 0))


def fill_above(g, pts, y, depth, ztop, m, step=0.12, axis="x"):
    """Solid wall from an arch's outline up to ztop (the spandrels), in thin vertical slices."""
    xs = [p[0] for p in pts]
    x0, x1 = min(xs), max(xs)
    x = x0
    while x < x1 - 1e-4:
        xm = min(x + step, x1)
        xc = (x + xm) / 2
        zc = None
        for i in range(len(pts) - 1):
            (ax, az), (bx, bz) = pts[i], pts[i + 1]
            if min(ax, bx) - 1e-6 <= xc <= max(ax, bx) + 1e-6 and abs(bx - ax) > 1e-6:
                zc = az + (bz - az) * (xc - ax) / (bx - ax)
                break
        if zc is not None and zc < ztop:
            if axis == "x":
                g.box((xc, y, (zc + ztop) / 2), (xm - x + 0.01, depth, ztop - zc), m)
            else:
                g.box((y, xc, (zc + ztop) / 2), (depth, xm - x + 0.01, ztop - zc), m)
        x = xm


def window(g, M, x, z0, w, h, y, tracery=True, mullion=True):
    """A pointed window on a wall whose face is at y (the camera side is -y), built out from the
    face: the glass and its leading, a chamfered reveal, a mullion and transoms, tracery in the
    head (a circle and two trefoils), voussoirs, a hood mould with label stops, a sill."""
    a = w / 2
    zs = z0 + h - a * 1.25
    pts = pointed(x, zs, a, 1.5, 9)
    top = max(p[1] for p in pts)
    # the glass, its leading
    g.box((x, y - 0.04, (z0 + zs) / 2), (w, 0.04, zs - z0), M["glass"])
    for i in range(len(pts) - 1):
        (x0, zz0), (x1, zz1) = pts[i], pts[i + 1]
        hh = (zz0 + zz1) / 2 - zs
        if hh > 0.01:
            g.box(((x0 + x1) / 2, y - 0.04, zs + hh / 2), (abs(x1 - x0) + 0.01, 0.04, hh), M["glass"])
    for k in range(1, int(h / 0.32)):
        zz = z0 + k * 0.32
        if zz < zs:
            g.box((x, y - 0.07, zz), (w, 0.015, 0.012), M["lead"])
    for k in range(1, 4):
        g.box((x - a + k * w / 4, y - 0.07, (z0 + zs) / 2), (0.012, 0.015, zs - z0), M["lead"])
    # the reveal: jambs, their chamfers, the arch ring
    for s in (-1, 1):
        g.box((x + s * (a + 0.09), y - 0.12, (z0 + zs) / 2), (0.18, 0.2, zs - z0), M["dress"])
        g.box((x + s * (a + 0.01), y - 0.08, (z0 + zs) / 2), (0.06, 0.1, zs - z0), M["dress_dk"])
    ring(g, pts, y - 0.12, 0.2, 0.2, M["dress"])
    if mullion:
        g.box((x, y - 0.13, (z0 + zs) / 2), (0.1, 0.16, zs - z0), M["dress"])
        for tz in (z0 + (zs - z0) * 0.38, z0 + (zs - z0) * 0.72):
            g.box((x, y - 0.12, tz), (w, 0.14, 0.08), M["dress"])
    if tracery:
        r = min(a * 0.48, (top - zs) * 0.42)
        cz = zs + (top - zs) * 0.45
        g.torus((x, y - 0.12, cz), r, 0.04, M["dress"], rot=(90, 0, 0), n1=20, n2=6)
        for k in range(4):
            ang = k / 4 * math.tau
            g.box((x + math.cos(ang) * r * 0.5, y - 0.12, cz + math.sin(ang) * r * 0.5), (0.04, 0.04, r), M["dress"], rot=(0, math.degrees(ang) + 90, 0))
        for s in (-1, 1):
            cx = x + s * a * 0.5
            for k in range(3):
                ang = k / 3 * math.tau + math.pi / 2
                g.torus((cx + math.cos(ang) * 0.09, y - 0.12, zs + 0.18 + math.sin(ang) * 0.09), 0.08, 0.022, M["dress"], rot=(90, 0, 0), n1=12, n2=5)
    # the hood mould and its label stops, the sill
    hood = pointed(x, zs, a + 0.26, 1.5, 9)
    ring(g, hood, y - 0.28, 0.14, 0.1, M["dress"])
    for s in (-1, 1):
        g.box((x + s * (a + 0.34), y - 0.28, zs - 0.08), (0.18, 0.16, 0.22), M["carve"])
        g.box((x + s * (a + 0.34), y - 0.34, zs - 0.12), (0.12, 0.06, 0.12), M["dress_dk"])
    g.box((x, y - 0.18, z0 - 0.08), (w + 0.5, 0.36, 0.14), M["dress"])
    g.box((x, y - 0.14, z0 - 0.2), (w + 0.3, 0.2, 0.1), M["dress_dk"])


def lantern(g, M, c, s=1.0):
    """An iron lantern: a square cage of glass with a lamp inside, a roof, a finial."""
    x, y, z = c
    g.box((x, y, z - 0.02 * s), (0.34 * s, 0.34 * s, 0.05 * s), M["iron"])
    for sx in (-1, 1):
        for sy in (-1, 1):
            g.box((x + sx * 0.15 * s, y + sy * 0.15 * s, z + 0.24 * s), (0.03 * s, 0.03 * s, 0.48 * s), M["iron"])
    g.box((x, y, z + 0.22 * s), (0.27 * s, 0.27 * s, 0.4 * s), M["glass"])
    g.box((x, y, z + 0.2 * s), (0.1 * s, 0.1 * s, 0.22 * s), M["lamp"])
    g.cyl((x, y, z + 0.6 * s), 0.27 * s, 0.22 * s, M["iron"], n=4, r2=0.03 * s, rot=(0, 0, 45))
    g.cyl((x, y, z + 0.75 * s), 0.025 * s, 0.14 * s, M["iron"], n=6)
    g.cyl((x, y, z + 0.84 * s), 0.05 * s, 0.06 * s, M["iron"], n=8)


def lamp_post(g, M, x, y, z, h=3.4):
    """A cast-iron lamp post: a stepped base, a fluted shaft, collars, a lantern on top."""
    g.cyl((x, y, z + 0.15), 0.26, 0.3, M["iron"], n=8)
    g.cyl((x, y, z + 0.4), 0.2, 0.22, M["iron"], n=8, r2=0.12)
    g.cyl((x, y, z + h / 2), 0.075, h - 0.4, M["iron"], n=10)
    for k in range(8):
        a = k / 8 * math.tau
        g.box((x + math.cos(a) * 0.075, y + math.sin(a) * 0.075, z + h / 2 + 0.2), (0.025, 0.025, h - 1.0), M["iron"])
    for zz in (z + 0.9, z + h - 0.5):
        g.cyl((x, y, zz), 0.11, 0.08, M["iron"], n=10)
    for s in (-1, 1):                                      # the ladder bar
        g.beam((x, y, z + h - 0.35), (x + s * 0.3, y, z + h - 0.28), 0.03, 0.03, M["iron"])
    lantern(g, M, (x, y, z + h), 1.0)


def bench(g, M, x, y, z, along_x=True):
    """A stone bench: a moulded seat slab on two scrolled ends."""
    L = 1.9
    sx, sy = (L, 0.5) if along_x else (0.5, L)
    g.box((x, y, z + 0.44), (sx, sy, 0.1), M["dress"])
    g.box((x, y, z + 0.38), (sx - 0.08, sy - 0.08, 0.04), M["dress_dk"])
    for s in (-1, 1):
        ex, ey = (x + s * (L / 2 - 0.2), y) if along_x else (x, y + s * (L / 2 - 0.2))
        g.box((ex, ey, z + 0.2), (0.22, 0.22, 0.4), M["dress"])
        g.torus((ex, ey, z + 0.28), 0.14, 0.06, M["carve"], rot=(0, 90, 0) if along_x else (90, 0, 0), n1=12, n2=6)


def figure(g, M, x, y, z, s=1.0, sword=True):
    """A stone figure on its pedestal's top: legs in a robe, a cloak, arms on a sword, a helm."""
    g.cyl((x, y, z + 0.55 * s), 0.26 * s, 1.1 * s, M["carve"], n=12, r2=0.2 * s)        # the robe
    g.cyl((x, y + 0.04 * s, z + 0.9 * s), 0.3 * s, 1.0 * s, M["carve"], n=12, r2=0.12 * s)   # the cloak
    g.cyl((x, y, z + 1.32 * s), 0.18 * s, 0.4 * s, M["carve"], n=12)                       # chest
    g.cyl((x, y, z + 1.62 * s), 0.12 * s, 0.24 * s, M["carve"], n=12, r2=0.1 * s)          # head, the helm
    g.cyl((x, y, z + 1.75 * s), 0.13 * s, 0.04 * s, M["carve"], n=12)
    for k in (-1, 1):
        g.beam((x + k * 0.17 * s, y, z + 1.42 * s), (x + k * 0.06 * s, y - 0.18 * s, z + 1.02 * s), 0.07 * s, 0.07 * s, M["carve"])
    if sword:
        g.box((x, y - 0.22 * s, z + 0.6 * s), (0.04 * s, 0.02 * s, 1.0 * s), M["dress"])
        g.box((x, y - 0.22 * s, z + 1.04 * s), (0.28 * s, 0.03 * s, 0.04 * s), M["dress"])


def urn(g, M, x, y, z, s=1.0):
    g.cyl((x, y, z + 0.08 * s), 0.2 * s, 0.16 * s, M["dress"], n=12)
    g.cyl((x, y, z + 0.24 * s), 0.08 * s, 0.18 * s, M["dress"], n=12)
    g.cyl((x, y, z + 0.5 * s), 0.26 * s, 0.36 * s, M["carve"], n=14, r2=0.3 * s)
    g.cyl((x, y, z + 0.72 * s), 0.3 * s, 0.08 * s, M["dress"], n=14)
    g.cyl((x, y, z + 0.84 * s), 0.12 * s, 0.18 * s, M["carve"], n=10, r2=0.02 * s)


def beast(g, M, x, y, z, face=-1):
    """A heraldic beast sitting on a pier: haunches, a chest, a maned head, a shield at its paws."""
    g.box((x, y + 0.2, z + 0.35), (0.7, 0.9, 0.7), M["carve"])
    g.box((x, y - 0.15, z + 0.75), (0.55, 0.45, 0.8), M["carve"], rot=(-12, 0, 0))
    g.cyl((x, y - 0.3, z + 1.25), 0.3, 0.36, M["carve"], n=10)                     # the mane
    g.box((x, y - 0.5, z + 1.22), (0.26, 0.3, 0.24), M["carve"])                    # the muzzle
    for s in (-1, 1):
        g.box((x + s * 0.15, y - 0.32, z + 1.48), (0.08, 0.08, 0.14), M["carve"])   # ears
        g.box((x + s * 0.18, y - 0.45, z + 0.2), (0.14, 0.3, 0.4), M["carve"])      # forepaws
    g.box((x, y - 0.62, z + 0.42), (0.42, 0.06, 0.5), M["dress"])                   # the shield
    g.cyl((x, y - 0.66, z + 0.45), 0.12, 0.03, M["dress_dk"], rot=(90, 0, 0), n=12)
    g.beam((x + 0.3, y + 0.55, z + 0.2), (x + 0.42, y + 0.3, z + 0.9), 0.08, 0.08, M["carve"])   # the tail


def diaper(g, M, x0, x1, z0, z1, y, m, along="x", d=0.32):
    """A carved diaper panel: a frame and a lattice of raised diamonds."""
    w = x1 - x0
    if along == "x":
        g.box(((x0 + x1) / 2, y, (z0 + z1) / 2), (w, 0.06, z1 - z0), M["dress_dk"])
    else:
        g.box((y, (x0 + x1) / 2, (z0 + z1) / 2), (0.06, w, z1 - z0), M["dress_dk"])
    nx, nz = max(1, int(w / d)), max(1, int((z1 - z0) / d))
    for i in range(nx):
        for j in range(nz):
            cx = x0 + (i + 0.5) * w / nx
            cz = z0 + (j + 0.5) * (z1 - z0) / nz
            if along == "x":
                g.box((cx, y - 0.04, cz), (d * 0.5, 0.06, d * 0.5), m, rot=(0, 45, 0))
            else:
                g.box((y - 0.04 if y < 0 else y + 0.04, cx, cz), (0.06, d * 0.5, d * 0.5), m, rot=(45, 0, 0))


def ivy(g, M, x, y, z0, z1, spread=0.7, n=260):
    """Ivy climbing a wall face at y: stems and a clustered mass of leaves."""
    for k in range(n):
        u = rnd.random()
        zz = z0 + (z1 - z0) * u
        w = spread * (1.0 - 0.6 * u) * (0.6 + 0.4 * math.sin(u * 9 + x))
        xx = x + rnd.uniform(-w, w)
        g.box((xx, y - rnd.uniform(0.03, 0.14), zz), (rnd.uniform(0.1, 0.18), 0.04, rnd.uniform(0.09, 0.15)), M["ivy"],
              rot=(rnd.uniform(-30, 30), rnd.uniform(-40, 40), rnd.uniform(0, 90)))
    for k in range(5):
        xx = x + rnd.uniform(-spread * 0.6, spread * 0.6)
        g.beam((xx, y - 0.03, z0), (xx + rnd.uniform(-0.4, 0.4), y - 0.03, z0 + (z1 - z0) * rnd.uniform(0.5, 0.95)), 0.03, 0.03, M["bark"])


# ---------------------------------------------------------------- the facade

def build_facade(S, coll, M):
    g = S.Group("FortFacade")
    z0, z1 = UP, UP + 11.9
    # the wall skin, in pieces round the door's opening
    dx0, dx1 = DOOR_X - DOOR_W / 2 - 0.9, DOOR_X + DOOR_W / 2 + 0.9
    yc = (FY + FACE_Y) / 2
    g.box(((HALL_X0 + dx0) / 2, yc, (z0 + z1) / 2), (dx0 - HALL_X0, SKIN, z1 - z0), M["ash"])
    g.box(((dx1 + HALL_X1) / 2, yc, (z0 + z1) / 2), (HALL_X1 - dx1, SKIN, z1 - z0), M["ash"])
    pts = pointed(DOOR_X, DOOR_SPRING, DOOR_W / 2 + 0.9, 1.5, 12)
    fill_above(g, pts, yc, SKIN, z1, M["ash"])
    # the plinth: a battered base course and its moulded top
    g.box(((HALL_X0 + dx0) / 2, FY - 0.15, z0 + 0.45), (dx0 - HALL_X0, 0.3, 0.9), M["ash"])
    g.box(((dx1 + HALL_X1) / 2, FY - 0.15, z0 + 0.45), (HALL_X1 - dx1, 0.3, 0.9), M["ash"])
    for (a, b) in ((HALL_X0, dx0), (dx1, HALL_X1)):
        g.box(((a + b) / 2, FY - 0.3, z0 + 0.95), (b - a, 0.16, 0.12), M["dress"])
        g.box(((a + b) / 2, FY - 0.22, z0 + 1.05), (b - a, 0.1, 0.08), M["dress_dk"])
    # string courses
    for zz in (UP + 5.95, UP + 10.2):
        g.box(((HALL_X0 + dx0) / 2, FY - 0.12, zz), (dx0 - HALL_X0, 0.24, 0.2), M["dress"])
        g.box(((dx1 + HALL_X1) / 2, FY - 0.12, zz), (HALL_X1 - dx1, 0.24, 0.2), M["dress"])
        g.box(((HALL_X0 + dx0) / 2, FY - 0.18, zz - 0.14), (dx0 - HALL_X0, 0.12, 0.08), M["dress_dk"])
        g.box(((dx1 + HALL_X1) / 2, FY - 0.18, zz - 0.14), (HALL_X1 - dx1, 0.12, 0.08), M["dress_dk"])
    # buttresses: three stepped stages, each with a sloped weathering, a gablet at the top
    for bx in BUTTRESS_X:
        stages = [(z0, UP + 4.0, 1.4, 1.3), (UP + 4.0, UP + 8.0, 1.2, 1.0), (UP + 8.0, UP + 11.4, 1.0, 0.7)]
        for (za, zb, wd, dp) in stages:
            g.box((bx, FY - dp / 2, (za + zb) / 2), (wd, dp, zb - za), M["ash_y"])
            g.box((bx, FY - dp + 0.12, zb - 0.05), (wd + 0.06, 0.4, 0.2), M["dress"], rot=(28, 0, 0))   # its weathering
        for s in (-1, 1):
            g.beam((bx + s * 0.5, FY - 0.7, UP + 11.4), (bx, FY - 0.7, UP + 12.3), 0.14, 0.7, M["dress"])
        g.cyl((bx, FY - 0.7, UP + 12.6), 0.1, 0.6, M["carve"], n=8, r2=0.02)             # its finial
        for zz in (UP + 2.0, UP + 6.2, UP + 9.6):                                         # a blind trefoil panel on each stage
            g.torus((bx, FY - 1.3 + (0.3 if zz > UP + 4 else 0.0) + (0.3 if zz > UP + 8 else 0.0) - 0.02, zz), 0.18, 0.03, M["dress_dk"], rot=(90, 0, 0), n1=14, n2=5)
    # windows: tall pairs of lancets below, trefoil-headed pairs above
    for (a, b) in BAYS:
        cx = (a + b) / 2
        if a == HALL_X0:
            cx += 0.6
        if b == HALL_X1:
            cx -= 0.6
        for s in (-1, 1):
            window(g, M, cx + s * 0.9, UP + 1.5, 1.2, 3.9, FY)
            window(g, M, cx + s * 0.75, UP + 7.0, 0.9, 2.4, FY, tracery=False, mullion=False)
        g.box((cx, FY - 0.1, UP + 3.4), (0.32, 0.22, 3.8), M["dress"])                  # the pier between the lancets
        # gargoyle spouts under the parapet
        g.box((cx, FY - 0.6, UP + 11.1), (0.3, 1.0, 0.3), M["carve"])
        g.box((cx, FY - 1.15, UP + 11.05), (0.36, 0.3, 0.36), M["carve"])
        g.cyl((cx, FY - 1.32, UP + 11.0), 0.08, 0.2, M["dress_dk"], rot=(90, 0, 0), n=8)
    # machicolations: corbels in three steps, the parapet out over them, its merlons
    x = HALL_X0 + 0.6
    while x < HALL_X1 - 0.5:
        if abs(x - DOOR_X) > 0.1 or True:
            for k, (dz, dy) in enumerate(((0.0, 0.18), (0.25, 0.34), (0.5, 0.5))):
                g.box((x, FY - dy / 2, UP + 11.1 + dz), (0.3, dy, 0.25), M["dress"])
        x += 0.9
    g.box(((HALL_X0 + HALL_X1) / 2, FY - 0.42, UP + 12.15), (HALL_X1 - HALL_X0, 0.34, 0.6), M["ash"])
    g.box(((HALL_X0 + HALL_X1) / 2, FY - 0.6, UP + 11.85), (HALL_X1 - HALL_X0, 0.06, 0.08), M["dress_dk"])
    D.merlons(g, HALL_X0 + 0.3, HALL_X1 - 0.3, FY - 0.42, UP + 12.45, 0.34, M["ash"], w=0.8, gap=0.55, h=1.0)
    x = HALL_X0 + 0.3
    while x + 0.8 <= HALL_X1 - 0.3:                                                    # each merlon's coping and arrow slit
        g.box((x + 0.4, FY - 0.42, UP + 13.5), (0.86, 0.42, 0.1), M["dress"])
        g.box((x + 0.4, FY - 0.6, UP + 12.95), (0.06, 0.02, 0.5), M["void"])
        x += 1.35
    # corner turrets: octagonal, corbelled out, slit windows, a crenel ring, a spirelet with a vane
    for tx in (HALL_X0 - 0.2, HALL_X1 + 0.2):
        ty = FY - 0.7
        g.cyl((tx, ty, UP + 7.8), 1.55, 15.6, M["ash"], n=8, rot=(0, 0, 22.5))
        for zz in (UP + 5.95, UP + 10.2, UP + 14.0):
            g.cyl((tx, ty, zz), 1.68, 0.22, M["dress"], n=8, rot=(0, 0, 22.5))
        for k in range(4):
            for zz in (UP + 3.0, UP + 8.2, UP + 12.2):
                a = math.radians(-90 + (k - 1.5) * 45)
                g.box((tx + math.cos(a) * 1.5, ty + math.sin(a) * 1.5, zz), (0.16, 0.12, 1.0), M["void"], rot=(0, 0, math.degrees(a) + 90))
        for k in range(16):                                                            # corbels and the crenel ring
            a = k / 16 * math.tau
            g.box((tx + math.cos(a) * 1.75, ty + math.sin(a) * 1.75, UP + 15.4), (0.25, 0.3, 0.4), M["dress"], rot=(0, 0, math.degrees(a)))
        g.cyl((tx, ty, UP + 15.85), 1.95, 0.5, M["ash"], n=16)
        for k in range(10):
            a = k / 10 * math.tau
            g.box((tx + math.cos(a) * 1.85, ty + math.sin(a) * 1.85, UP + 16.45), (0.5, 0.3, 0.7), M["ash"], rot=(0, 0, math.degrees(a) + 90))
        g.cyl((tx, ty, UP + 18.6), 1.25, 4.4, M["lead"], n=8, r2=0.06, rot=(0, 0, 22.5))   # the spirelet, leaded
        for k in range(4):
            a = k / 4 * math.tau + 0.4
            g.box((tx + math.cos(a) * 0.85, ty + math.sin(a) * 0.85, UP + 17.6), (0.35, 0.35, 0.7), M["lead"], rot=(0, 0, math.degrees(a)))   # lucarnes
        g.cyl((tx, ty, UP + 21.6), 0.025, 1.6, M["iron"], n=6)
        g.box((tx + 0.25, ty, UP + 22.1), (0.5, 0.02, 0.22), M["iron"])               # the vane
        g.cyl((tx, ty, UP + 21.0), 0.08, 0.1, M["iron"], n=8)
    # clustered chimneys rising from behind the parapet
    for cxx in (131.0, 136.0, 158.0, 163.0):
        for k in range(3):
            px = cxx + (k - 1) * 0.55
            g.box((px, -1.2, UP + 14.5), (0.5, 0.6, 5.0), M["brick_x"])
            g.cyl((px, -1.2, UP + 16.6), 0.22, 0.6, M["dress_dk"], n=10, r2=0.18)     # its pot
            for zz in (UP + 13.6, UP + 15.8):
                g.box((px, -1.2, zz), (0.56, 0.66, 0.08), M["dress"])
        g.box((cxx, -1.2, UP + 16.1), (1.8, 0.75, 0.25), M["dress"])
    # drainpipes and hoppers down the buttress sides
    for bx in BUTTRESS_X:
        px = bx - 0.85
        g.cyl((px, FY - 0.12, UP + 5.6), 0.06, 11.0, M["lead"], n=8)
        g.box((px, FY - 0.15, UP + 11.2), (0.3, 0.3, 0.32), M["lead"])
        for zz in range(int(UP) + 1, int(UP + 11), 2):
            g.box((px, FY - 0.08, zz), (0.16, 0.06, 0.05), M["iron"])
        g.box((px, FY - 0.3, UP + 0.1), (0.22, 0.4, 0.06), M["lead"])
    # ivy up two buttresses and a corner
    ivy(g, M, 138.5, FY - 1.32, UP, UP + 7.5, spread=0.9, n=420)
    ivy(g, M, 161.0, FY - 1.32, UP, UP + 5.5, spread=0.8, n=320)
    ivy(g, M, HALL_X0 + 1.4, FY, UP, UP + 9.5, spread=1.1, n=420)
    # the great door's portal: four receding orders on jamb shafts, capitals, a carved tympanum
    a0 = DOOR_W / 2
    for o in range(4):
        a = a0 + o * 0.22
        y = FY + 0.35 - o * 0.12
        pts_o = pointed(DOOR_X, DOOR_SPRING, a + 0.11, 1.5, 12)
        ring(g, pts_o, y, 0.2, 0.22, M["dress"] if o % 2 == 0 else M["carve"])
        for s in (-1, 1):
            g.cyl((DOOR_X + s * (a + 0.11), y, (UP + DOOR_SPRING) / 2), 0.1, DOOR_SPRING - UP - 0.4, M["dress"], n=10)
            g.box((DOOR_X + s * (a + 0.11), y, DOOR_SPRING - 0.12), (0.3, 0.3, 0.24), M["carve"])        # its capital
            g.box((DOOR_X + s * (a + 0.11), y, UP + 0.2), (0.28, 0.28, 0.4), M["dress"])                 # its base
            g.torus((DOOR_X + s * (a + 0.11), y, UP + 0.44), 0.11, 0.035, M["dress"], n1=12, n2=5)
    for s in (-1, 1):
        g.box((DOOR_X + s * (a0 + 0.8), FY + 0.05, (UP + DOOR_SPRING) / 2), (0.3, 0.6, DOOR_SPRING - UP), M["ash_y"])
    window(g, M, DOOR_X, UP + 9.5, 1.4, 1.5, FY, tracery=False, mullion=True)
    # iron wall lanterns either side of the porch
    for s in (-1, 1):
        lx = DOOR_X + s * 5.2
        g.beam((lx, FY, UP + 3.4), (lx, FY - 0.5, UP + 3.4), 0.05, 0.05, M["iron"])
        g.beam((lx, FY, UP + 3.0), (lx, FY - 0.45, UP + 3.38), 0.03, 0.03, M["iron"])
        lantern(g, M, (lx, FY - 0.55, UP + 2.75), 0.9)
    return g.finish(coll)


# ---------------------------------------------------------------- the porch and the door

def carved_column(g, M, x, y, z0, h, r=0.38):
    """A carved column: a moulded base, a shaft of drums with spiral fluting and a diaper band,
    rings at the thirds, a capital with leaves and an abacus."""
    g.box((x, y, z0 + 0.25), (r * 2.6, r * 2.6, 0.5), M["dress"])
    g.cyl((x, y, z0 + 0.6), r * 1.25, 0.2, M["dress"], n=16)
    g.torus((x, y, z0 + 0.72), r * 1.08, 0.06, M["dress"], n1=20, n2=6)
    zs0, zs1 = z0 + 0.78, z0 + h - 0.7
    g.cyl((x, y, (zs0 + zs1) / 2), r * 0.92, zs1 - zs0, M["dress_dk"], n=16)
    n = int((zs1 - zs0) / 0.12)
    for i in range(n):
        zz = zs0 + (i + 0.5) * (zs1 - zs0) / n
        band = (zs1 - zs0) * 0.36 < zz - zs0 < (zs1 - zs0) * 0.64
        for k in range(10):
            a = k / 10 * math.tau + (0.0 if band else i * 0.16)
            if band and (i + k) % 2:
                continue
            g.box((x + math.cos(a) * r * 0.92, y + math.sin(a) * r * 0.92, zz), (0.1 if band else 0.07, 0.07, 0.11 if band else 0.13),
                  M["carve"], rot=(0, 45 if band else 0, math.degrees(a)))
    for zz in (zs0 + (zs1 - zs0) * 0.35, zs0 + (zs1 - zs0) * 0.65):
        g.torus((x, y, zz), r * 0.98, 0.05, M["dress"], n1=20, n2=6)
    g.cyl((x, y, zs1 + 0.25), r * 0.95, 0.5, M["carve"], n=16, r2=r * 1.35)            # the capital's bell
    for k in range(8):
        a = k / 8 * math.tau
        g.box((x + math.cos(a) * r * 1.15, y + math.sin(a) * r * 1.15, zs1 + 0.28), (0.12, 0.08, 0.4), M["carve"], rot=(-25, 0, math.degrees(a) + 90))
    g.box((x, y, zs1 + 0.6), (r * 3.0, r * 3.0, 0.2), M["dress"])


def build_porch(S, coll, M):
    g = S.Group("FortPorch")
    x0, x1 = DOOR_X - 3.8, DOOR_X + 3.8
    y0, y1 = FY, PORCH_Y
    # its floor: flags, a step at the front
    for i in range(int((x1 - x0) / 0.76)):
        for j in range(int((y0 - y1) / 0.6)):
            g.box((x0 + 0.38 + i * 0.76, y1 + 0.3 + j * 0.6, UP + 0.02), (0.74, 0.58, 0.06 + 0.01 * ((i + j) % 3)), M["flags"])
    g.box(((x0 + x1) / 2, y1 - 0.25, UP - 0.05), (x1 - x0 + 0.6, 0.5, 0.1), M["dress"])
    # the side walls, each with a niche and a statue, a blind arcade
    for s in (-1, 1):
        sx = DOOR_X + s * 3.5
        g.box((sx, (y0 + y1 + 1.2) / 2, UP + 3.4), (0.6, y0 - y1 - 1.2, 6.8), M["ash_y"])
        ny = (y0 + y1) / 2 + 0.6
        g.box((sx - s * 0.32, ny, UP + 2.4), (0.06, 1.1, 2.6), M["dress_dk"])           # the niche, its back
        for k in range(7):
            a = math.pi * k / 6
            g.box((sx - s * 0.33, ny + math.cos(a) * 0.55, UP + 3.7 + math.sin(a) * 0.55), (0.1, 0.24, 0.16), M["dress"], rot=(math.degrees(a) - 90, 0, 0))
        g.box((sx - s * 0.36, ny, UP + 1.05), (0.24, 1.2, 0.14), M["dress"])
        figure(g, M, sx - s * 0.5, ny, UP + 1.12, 1.25)
    # the two carved columns at the front corners, the front arch between them
    PS, BZ = UP + 4.6, UP + 8.0                   # the arch's springing, the balcony slab
    for s in (-1, 1):
        carved_column(g, M, DOOR_X + s * 3.1, y1 + 0.5, UP, 4.6)
    pts = pointed(DOOR_X, PS, 2.7, 1.15, 12)
    ring(g, pts, y1 + 0.5, 0.9, 0.32, M["dress"])
    ring(g, pointed(DOOR_X, PS, 2.36, 1.15, 12), y1 + 0.5, 0.6, 0.12, M["carve"])         # the inner roll
    fill_above(g, pointed(DOOR_X, PS, 3.02, 1.15, 12), y1 + 0.5, 0.9, BZ - 0.15, M["ash"])
    for s in (-1, 1):
        g.box((DOOR_X + s * 3.3, y1 + 0.5, (PS + BZ) / 2), (0.6, 0.9, BZ - PS), M["ash"])
    for k in range(10):                                                                # dogtooth round the arch
        p = pts[1 + k * 2] if 1 + k * 2 < len(pts) else pts[-2]
        g.box((p[0], y1 + 0.02, p[1] + 0.36), (0.12, 0.1, 0.12), M["carve"], rot=(0, 45, 0))
    # the rib vault inside: diagonal and ridge ribs, a carved boss
    zc = UP + 6.9
    for (ax, ay) in ((x0 + 0.5, y0 - 0.1), (x1 - 0.5, y0 - 0.1), (x0 + 0.5, y1 + 0.9), (x1 - 0.5, y1 + 0.9)):
        g.beam((ax, ay, PS), (DOOR_X, (y0 + y1) / 2 + 0.4, zc), 0.16, 0.2, M["dress"])
    g.beam((DOOR_X, y0 - 0.1, zc - 0.1), (DOOR_X, y1 + 0.9, zc - 0.1), 0.14, 0.16, M["dress"])
    g.box((DOOR_X, (y0 + y1) / 2 + 0.4, zc + 0.25), (x1 - x0 - 1.0, y0 - y1 - 1.2, 0.3), M["ash_top"])
    g.cyl((DOOR_X, (y0 + y1) / 2 + 0.4, zc - 0.05), 0.24, 0.24, M["carve"], n=12)
    # the balcony on top: a slab, a moulded edge, balusters, a rail, urns on the corners, a crest
    g.box((DOOR_X, (y0 + y1) / 2 + 0.1, BZ), (x1 - x0 + 0.4, y0 - y1 + 0.4, 0.3), M["dress"])
    g.box((DOOR_X, y1 - 0.08, BZ - 0.2), (x1 - x0 + 0.5, 0.2, 0.14), M["dress_dk"])
    xx = x0 + 0.3
    while xx < x1 - 0.2:
        g.cyl((xx, y1 + 0.1, BZ + 0.5), 0.07, 0.7, M["dress"], n=10, r2=0.05)
        g.cyl((xx, y1 + 0.1, BZ + 0.37), 0.09, 0.2, M["dress"], n=10)
        xx += 0.26
    for s in (-1, 1):
        yy = y0 - 0.4
        while yy > y1 + 0.3:
            g.cyl((DOOR_X + s * 3.75, yy, BZ + 0.5), 0.07, 0.7, M["dress"], n=10, r2=0.05)
            yy -= 0.26
    g.box((DOOR_X, y1 + 0.1, BZ + 0.91), (x1 - x0 + 0.3, 0.3, 0.12), M["dress"])
    for s in (-1, 1):
        g.box((DOOR_X + s * 3.75, (y0 + y1) / 2, BZ + 0.91), (0.3, y0 - y1, 0.12), M["dress"])
        g.box((DOOR_X + s * 3.75, y1 + 0.1, BZ + 0.65), (0.42, 0.42, 1.1), M["dress"])
        urn(g, M, DOOR_X + s * 3.75, y1 + 0.1, BZ + 1.2, 1.0)
    g.box((DOOR_X, y1 + 0.1, BZ + 1.25), (1.4, 0.3, 0.8), M["dress"])
    g.cyl((DOOR_X, y1 - 0.06, BZ + 1.25), 0.3, 0.06, M["carve"], rot=(90, 0, 0), n=20)
    for k in range(8):
        a = k / 8 * math.tau
        g.box((DOOR_X + math.cos(a) * 0.42, y1 - 0.06, BZ + 1.25 + math.sin(a) * 0.32), (0.08, 0.05, 0.08), M["carve"], rot=(0, math.degrees(a), 0))
    # the door leaves: oak planks, iron straps, studs, a ring handle, standing open into the dark
    for s in (-1, 1):
        hx = DOOR_X + s * DOOR_W / 2
        R = Matrix.Rotation(math.radians(-s * 105), 4, "Z")
        leaf = Matrix.Translation(Vector((hx, FY + 0.45, UP))) @ R
        def P(lx, lz, ly=0.0):
            v = leaf @ Vector((-s * lx, ly, lz))
            return (v.x, v.y, v.z)
        g.box(P(DOOR_W / 4, 2.2), (DOOR_W / 2, 0.12, 4.4), M["oak"], rot=R)
        for zz in (0.6, 1.8, 3.0, 4.0):
            g.box(P(DOOR_W / 4, zz, -0.08), (DOOR_W / 2 - 0.1, 0.03, 0.09), M["iron"], rot=R)
            for k in range(6):
                g.box(P(0.15 + k * 0.24, zz + 0.12, -0.09), (0.035, 0.03, 0.035), M["iron"], rot=R)
        g.box(P(DOOR_W / 2 - 0.25, 2.0, -0.12), (0.12, 0.05, 0.12), M["iron"], rot=R)
    # the threshold and, inside, the hall: a dim passage with the morning at its far end (the hall's
    # windows behind), so two figures come out of the dark as shapes against the light
    g.box((DOOR_X, FY + 0.15, UP + 0.03), (DOOR_W + 0.2, 0.7, 0.06), M["dress"])
    g.box((DOOR_X, FY + 4.0, UP + 3.0), (DOOR_W + 3.0, 0.1, 6.0), M["hall_light"])
    for k in range(5):                                                                # the far windows' mullions against it
        g.box((DOOR_X - 2.0 + k * 1.0, FY + 3.9, UP + 3.4), (0.12, 0.06, 5.0), M["passage"])
    g.box((DOOR_X, FY + 3.9, UP + 1.2), (DOOR_W + 3.0, 0.08, 0.3), M["passage"])
    for s in (-1, 1):
        g.box((DOOR_X + s * (DOOR_W / 2 + 1.4), FY + 2.0, UP + 3.0), (0.1, 4.0, 6.0), M["passage"])
    g.box((DOOR_X, FY + 2.0, UP + 6.0), (DOOR_W + 3.0, 4.0, 0.1), M["passage"])
    g.box((DOOR_X, FY + 2.0, UP + 0.01), (DOOR_W + 3.0, 4.0, 0.02), M["flags"])
    return g.finish(coll)


# ---------------------------------------------------------------- the forecourt

def hedge(g, M, x0, y0, x1, y1, h=0.55, w=0.45):
    """A clipped box hedge from one point to another: a block with a rounded top."""
    a, b = Vector((x0, y0, 0)), Vector((x1, y1, 0))
    L = (b - a).length
    ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
    c = (a + b) / 2
    g.box((c.x, c.y, UP + h / 2), (L + w, w, h), M["hedge"], rot=(0, 0, ang))
    g.cyl((c.x, c.y, UP + h), w / 2, L + w * 0.9, M["hedge"], rot=(0, 90, ang), n=8)


def build_forecourt(S, coll, M):
    g = S.Group("FortForecourt")
    # the ground: gravel; the paved path from the porch to the gate, its kerbs
    D.terrain(g, COURT_X0, COURT_X1, COURT_Y1 - 0.2, FY - 0.1, lambda x, y: UP - 0.01, M["gravel"], res=0.7, amp=0.015, seed=4.0)
    for j in range(int((PORCH_Y - COURT_Y1) / 0.62)):
        y = PORCH_Y - 0.31 - j * 0.62
        off = 0.0 if j % 2 == 0 else 0.4
        x = DOOR_X - 2.5 + off
        while x < DOOR_X + 2.5:
            w = min(0.8, DOOR_X + 2.5 - x)
            g.box((x + w / 2, y, UP + 0.02 + 0.006 * math.sin(x * 7 + y)), (w - 0.02, 0.6, 0.06), M["flags"])
            x += 0.8
    for s in (-1, 1):
        g.box((DOOR_X + s * 2.6, (PORCH_Y + COURT_Y1) / 2, UP + 0.04), (0.18, PORCH_Y - COURT_Y1, 0.1), M["dress"])
    # the parterre: hedged beds either side of the path, diagonal knots, topiary cones and balls
    for (bx0, bx1) in ((128.5, 140.0), (151.0, 157.2)):
        for (by0, by1) in ((-7.8, -12.0), (-18.2, -26.8)):
            hedge(g, M, bx0, by0, bx1, by0)
            hedge(g, M, bx0, by1, bx1, by1)
            hedge(g, M, bx0, by0, bx0, by1)
            hedge(g, M, bx1, by0, bx1, by1)
            hedge(g, M, bx0, by0, bx1, by1, h=0.35, w=0.3)
            hedge(g, M, bx0, by1, bx1, by0, h=0.35, w=0.3)
            for (cx, cy) in ((bx0, by0), (bx1, by0), (bx0, by1), (bx1, by1)):
                g.cyl((cx, cy, UP + 1.0), 0.42, 1.4, M["hedge"], n=10, r2=0.04)        # a cone
            tmp = bmesh.new()
            bmesh.ops.create_icosphere(tmp, subdivisions=2, radius=0.55)
            bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector(((bx0 + bx1) / 2, (by0 + by1) / 2, UP + 0.75))), verts=tmp.verts)
            g._merge(tmp, M["hedge"])
            g.cyl(((bx0 + bx1) / 2, (by0 + by1) / 2, UP + 0.2), 0.3, 0.4, M["dress"], n=10)   # its pot
    # the fountain: an octagonal basin with a moulded coping, the water, a pedestal of two bowls,
    # spouts, and on top a stone knight
    fx, fy = FOUNTAIN
    R = 2.9
    for k in range(8):
        a0, a1 = k / 8 * math.tau, (k + 1) / 8 * math.tau
        p0 = (fx + math.cos(a0) * R, fy + math.sin(a0) * R)
        p1 = (fx + math.cos(a1) * R, fy + math.sin(a1) * R)
        g.beam((p0[0], p0[1], UP + 0.35), (p1[0], p1[1], UP + 0.35), 0.35, 0.7, M["dress"])
        g.beam((p0[0], p0[1], UP + 0.74), (p1[0], p1[1], UP + 0.74), 0.5, 0.1, M["dress"])
        g.box(((p0[0] + p1[0]) / 2 - math.cos((a0 + a1) / 2) * 0.05, (p0[1] + p1[1]) / 2 - math.sin((a0 + a1) / 2) * 0.05, UP + 0.4),
              (0.6, 0.6, 0.3), M["carve"], rot=(0, 45, math.degrees((a0 + a1) / 2) + 90))
        g.cyl((p0[0], p0[1], UP + 0.85), 0.14, 0.24, M["carve"], n=8)
    g.cyl((fx, fy, UP + 0.55), R - 0.1, 0.04, M["water"], n=8, rot=(0, 0, 0))
    g.cyl((fx, fy, UP + 1.2), 0.4, 1.4, M["dress"], n=12)
    g.cyl((fx, fy, UP + 1.95), 1.4, 0.24, M["dress"], n=20, r2=0.6)
    g.cyl((fx, fy, UP + 2.08), 1.3, 0.05, M["water"], n=20)
    g.torus((fx, fy, UP + 2.07), 1.4, 0.06, M["dress"], n1=24, n2=6)
    g.cyl((fx, fy, UP + 2.6), 0.24, 0.9, M["dress"], n=12)
    g.cyl((fx, fy, UP + 3.1), 0.75, 0.18, M["dress"], n=16, r2=0.35)
    g.torus((fx, fy, UP + 3.18), 0.75, 0.04, M["dress"], n1=20, n2=5)
    for k in range(4):
        a = k / 4 * math.tau + 0.4
        g.box((fx + math.cos(a) * 0.62, fy + math.sin(a) * 0.62, UP + 2.4), (0.14, 0.14, 0.28), M["carve"])   # masks, spouting
        g.beam((fx + math.cos(a) * 0.7, fy + math.sin(a) * 0.7, UP + 2.4), (fx + math.cos(a) * 1.25, fy + math.sin(a) * 1.25, UP + 2.15), 0.02, 0.02, M["water"])
    g.cyl((fx, fy, UP + 3.4), 0.3, 0.4, M["dress"], n=12)
    figure(g, M, fx, fy, UP + 3.6, 1.15)
    # benches round the fountain and along the path, facing it
    for (bx, by, ax) in ((fx - 4.6, fy, False), (152.6, -13.6, False), (152.6, -16.6, False)):
        bench(g, M, bx, by, UP, ax)
    # lamp posts along the path
    for y in (-9.5, -21.5):
        for s in (-1, 1):
            lamp_post(g, M, DOOR_X + s * 3.4, y, UP, 3.6)
    # the west balustrade over the drop, with pedestals and statues
    wx = COURT_X0 + 0.4
    g.box((wx, (FY + COURT_Y1) / 2, UP + 0.15), (0.6, FY - COURT_Y1, 0.3), M["dress"])
    y = FY - 0.5
    while y > COURT_Y1 + 0.3:
        g.cyl((wx, y, UP + 0.6), 0.09, 0.62, M["dress"], n=10, r2=0.06)
        g.cyl((wx, y, UP + 0.45), 0.11, 0.24, M["dress"], n=10)
        y -= 0.3
    g.box((wx, (FY + COURT_Y1) / 2, UP + 1.0), (0.5, FY - COURT_Y1, 0.14), M["dress"])
    for py in (-6.0, -13.0, -20.0, -27.0):
        g.box((wx, py, UP + 0.7), (0.8, 0.8, 1.4), M["ash_y"])
        g.box((wx, py, UP + 1.45), (0.95, 0.95, 0.12), M["dress"])
        figure(g, M, wx, py, UP + 1.5, 1.0, sword=(py % 2 == 0))
    # the front balustrade either side of the gate
    for (a, b) in ((COURT_X0, DOOR_X - 4.6), (DOOR_X + 4.6, COURT_X1)):
        g.box(((a + b) / 2, COURT_Y1, UP + 0.15), (b - a, 0.6, 0.3), M["dress"])
        x = a + 0.3
        while x < b - 0.2:
            g.cyl((x, COURT_Y1, UP + 0.6), 0.09, 0.62, M["dress"], n=10, r2=0.06)
            x += 0.3
        g.box(((a + b) / 2, COURT_Y1, UP + 1.0), (b - a, 0.5, 0.14), M["dress"])
        for k in range(int((b - a) / 6)):
            px = a + 3 + k * 6
            g.box((px, COURT_Y1, UP + 0.7), (0.7, 0.7, 1.4), M["ash"])
            urn(g, M, px, COURT_Y1, UP + 1.4, 0.9)
    # the gate: two great piers (a rusticated base, diaper-carved panels, a cornice, a ball and a
    # beast on top), lanterns on brackets, and the wrought-iron gates swung open against them
    for s in (-1, 1):
        px = DOOR_X + s * 3.6
        py = COURT_Y1
        for k in range(4):                                                            # rustication
            g.box((px, py, UP + 0.25 + k * 0.5), (2.1 - (k % 2) * 0.06, 2.1 - (k % 2) * 0.06, 0.46), M["ash"])
        g.box((px, py, UP + 2.1), (2.2, 2.2, 0.2), M["dress"])
        g.box((px, py, UP + 3.6), (1.9, 1.9, 2.8), M["ash"])
        diaper(g, M, px - 0.75, px + 0.75, UP + 2.4, UP + 4.8, py - 0.95, M["carve"])
        diaper(g, M, py - 0.75, py + 0.75, UP + 2.4, UP + 4.8, px - s * 0.95, M["carve"], along="y")
        g.box((px, py, UP + 5.1), (2.2, 2.2, 0.22), M["dress"])
        g.box((px, py, UP + 5.3), (2.4, 2.4, 0.18), M["dress"])
        g.box((px, py - 1.12, UP + 5.05), (2.2, 0.06, 0.1), M["dress_dk"])
        g.box((px, py, UP + 5.55), (1.3, 1.3, 0.3), M["dress"])
        beast(g, M, px, py, UP + 5.7)
        for ly in (py - 1.1,):
            g.beam((px - s * 1.05, py - 0.6, UP + 4.2), (px - s * 1.6, py - 0.6, UP + 4.2), 0.05, 0.05, M["iron"])
            g.beam((px - s * 1.05, py - 0.6, UP + 3.7), (px - s * 1.55, py - 0.6, UP + 4.18), 0.03, 0.03, M["iron"])
            lantern(g, M, (px - s * 1.65, py - 0.6, UP + 3.55), 0.85)
        # the gate leaf, open back against the forecourt side of the pier
        hx = px - s * 1.1
        gx0, gx1 = hx, hx
        leafy0 = py + 1.1
        L = 2.4
        for k in range(12):
            by = leafy0 + 0.1 + k * L / 12
            g.box((hx, by, UP + 1.5), (0.03, 0.03, 2.8 + 0.25 * math.sin(k / 11 * math.pi)), M["iron"])
            g.cyl((hx, by, UP + 3.0 + 0.25 * math.sin(k / 11 * math.pi)), 0.045, 0.14, M["iron"], n=6, r2=0.0)
        for zz in (UP + 0.25, UP + 1.3, UP + 2.4):
            g.box((hx, leafy0 + L / 2, zz), (0.05, L, 0.06), M["iron"])
        for k in range(5):                                                            # scrolls
            g.torus((hx, leafy0 + 0.25 + k * 0.48, UP + 1.85), 0.16, 0.02, M["iron"], rot=(0, 90, 0), n1=14, n2=4)
            g.torus((hx, leafy0 + 0.25 + k * 0.48, UP + 0.8), 0.16, 0.02, M["iron"], rot=(0, 90, 0), n1=14, n2=4)
    # the east range: the stable block, two storeys, its windows and doors, a slate roof with
    # dormers, chimneys, and the clock turret over its gable
    x0, x1, y0, y1 = RANGE_X0, RANGE_X1, RANGE_Y0, RANGE_Y1
    h = 7.0
    g.box(((x0 + x1) / 2, (y0 + y1) / 2, UP + h / 2), (x1 - x0, y0 - y1, h), M["ash_y"])
    g.box((x0 - 0.1, (y0 + y1) / 2, UP + 3.5), (0.08, y0 - y1, 0.18), M["dress"])
    g.box((x0 - 0.1, (y0 + y1) / 2, UP + 0.35), (0.12, y0 - y1, 0.7), M["dress"])
    for k in range(9):
        wy = y0 - 1.2 - k * 2.4
        if abs(wy - MAN_DOOR[1]) < 1.0:
            # the man's door: an arched opening, its leaf open, dark beyond
            g.box((x0 - 0.02, wy, UP + 1.2), (0.06, 1.3, 2.4), M["void"])
            g.box((x0 - 0.1, wy, UP + 2.55), (0.24, 1.7, 0.28), M["dress"])
            for s in (-1, 1):
                g.box((x0 - 0.1, wy + s * 0.75, UP + 1.25), (0.24, 0.2, 2.5), M["dress"])
            g.box((x0 - 0.62, wy - 0.95, UP + 1.15), (1.15, 0.08, 2.3), M["oak"], rot=(0, 0, 70))
            continue
        for zz, hh in ((UP + 1.6, 1.5), (UP + 4.8, 1.3)):
            g.box((x0 - 0.02, wy, zz), (0.06, 0.9, hh), M["glass"])
            g.box((x0 - 0.1, wy, zz + hh / 2 + 0.1), (0.22, 1.2, 0.2), M["dress"])
            g.box((x0 - 0.12, wy, zz - hh / 2 - 0.06), (0.26, 1.1, 0.12), M["dress"])
            g.box((x0 - 0.06, wy, zz), (0.06, 0.06, hh), M["lead"])
            g.box((x0 - 0.06, wy, zz + 0.1), (0.06, 0.9, 0.05), M["lead"])
    # the gable end towards the camera (y1): a big arched carriage door, a round window, the clock turret
    gy = y1
    g.box(((x0 + x1) / 2, gy - 0.05, UP + 1.8), (3.4, 0.1, 3.6), M["oak"])
    ring(g, pointed((x0 + x1) / 2, UP + 3.0, 1.9, 1.2, 10), gy - 0.15, 0.3, 0.3, M["dress"])
    g.torus(((x0 + x1) / 2, gy - 0.12, UP + 5.4), 0.6, 0.1, M["dress"], rot=(90, 0, 0), n1=20, n2=6)
    g.cyl(((x0 + x1) / 2, gy - 0.05, UP + 5.4), 0.55, 0.05, M["glass"], rot=(90, 0, 0), n=20)
    # the roof: two slate slopes, a ridge, the gable copings
    rz = UP + h
    ridge = rz + 3.2
    mx = (x0 + x1) / 2
    for s in (-1, 1):
        ang = math.degrees(math.atan2(3.2, (x1 - x0) / 2))
        g.box((mx + s * (x1 - x0) / 4, (y0 + y1) / 2, rz + 1.6), (math.hypot((x1 - x0) / 2, 3.2) + 0.3, y0 - y1 + 0.4, 0.14), M["slate_y"], rot=(0, s * ang, 0))
    g.box((mx, (y0 + y1) / 2, ridge + 0.05), (0.3, y0 - y1 + 0.4, 0.2), M["dress_dk"])
    g.prism([(x0 - 0.2, rz), (x1 + 0.2, rz), (mx, ridge + 0.1)], gy - 0.1, gy + 0.3, M["ash"])
    for s in (-1, 1):
        g.beam((mx + s * ((x1 - x0) / 2 + 0.25), gy - 0.15, rz), (mx, gy - 0.15, ridge + 0.25), 0.35, 0.4, M["dress"])
    for k in range(3):                                                                # dormers on the forecourt slope
        dy = y0 - 4.0 - k * 7.0
        g.box((x0 + 1.4, dy, rz + 1.0), (1.4, 1.4, 1.6), M["ash_y"])
        g.box((x0 + 0.68, dy, rz + 1.0), (0.04, 0.8, 0.9), M["glass"])
        g.prism([(x0 + 0.6, rz + 1.8), (x0 + 2.2, rz + 1.8), (x0 + 1.4, rz + 2.5)], dy - 0.8, dy + 0.8, M["slate"], axis="y")
    for cy in (y0 - 2.0, (y0 + y1) / 2, y1 + 2.0):                                     # chimneys on the ridge
        g.box((mx, cy, ridge + 1.0), (1.0, 0.7, 2.2), M["brick_x"])
        g.box((mx, cy, ridge + 2.15), (1.15, 0.85, 0.12), M["dress"])
        for k in (-1, 1):
            g.cyl((mx + k * 0.25, cy, ridge + 2.45), 0.14, 0.5, M["dress_dk"], n=8, r2=0.11)
    # the clock turret: a square base on the ridge, the clock face to the camera, an open bell stage, a lead dome
    tx, ty = mx, y1 + 1.4
    g.box((tx, ty, ridge + 0.8), (1.6, 1.6, 1.8), M["ash"])
    g.cyl((tx, ty - 0.82, ridge + 1.0), 0.6, 0.04, M["white"], rot=(90, 0, 0), n=24)
    g.torus((tx, ty - 0.84, ridge + 1.0), 0.6, 0.04, M["iron"], rot=(90, 0, 0), n1=24, n2=5)
    for k in range(12):
        a = k / 12 * math.tau
        g.box((tx + math.cos(a) * 0.5, ty - 0.86, ridge + 1.0 + math.sin(a) * 0.5), (0.04, 0.02, 0.1), M["iron"], rot=(0, math.degrees(a) + 90, 0))
    g.box((tx + 0.12, ty - 0.88, ridge + 1.18), (0.03, 0.02, 0.38), M["iron"], rot=(0, -36, 0))   # its hands: twenty to five
    g.box((tx - 0.1, ty - 0.88, ridge + 0.88), (0.035, 0.02, 0.26), M["iron"], rot=(0, 120, 0))
    for sx in (-1, 1):
        for sy in (-1, 1):
            g.box((tx + sx * 0.65, ty + sy * 0.65, ridge + 2.4), (0.18, 0.18, 1.4), M["dress"])
    g.box((tx, ty, ridge + 3.15), (1.7, 1.7, 0.16), M["dress"])
    g.cyl((tx, ty, ridge + 3.7), 0.85, 1.0, M["lead"], n=16, r2=0.1)
    g.cyl((tx, ty, ridge + 4.6), 0.02, 1.2, M["iron"], n=6)
    g.cyl((tx, ty, ridge + 2.3), 0.35, 0.5, M["iron"], n=12, r2=0.2)                     # the bell
    return g.finish(coll)


# ---------------------------------------------------------------- the bridge

def build_bridge(S, coll, M):
    g = S.Group("FortBridge")
    cx = DOOR_X
    w = BRIDGE_W
    y0, y1 = BRIDGE_Y0, BRIDGE_Y1
    deck = UP
    # the deck: setts, the kerbs and gutters, the parapets in brick with a stone coping
    g.box((cx, (y0 + y1) / 2, deck - 0.4), (w + 1.2, y0 - y1, 0.8), M["ash_top"])
    D.terrain(g, cx - w / 2 + 0.25, cx + w / 2 - 0.25, y1, y0, lambda x, y: deck + 0.04 - 0.02 * ((x - cx) / (w / 2)) ** 2, M["setts"], res=0.4, amp=0.008, seed=9.0)
    for s in (-1, 1):
        g.box((cx + s * (w / 2 - 0.15), (y0 + y1) / 2, deck + 0.06), (0.3, y0 - y1, 0.12), M["dress"])
        px = cx + s * (w / 2 + 0.25)
        g.box((px, (y0 + y1) / 2, deck + 0.55), (0.5, y0 - y1, 1.1), M["brick"])
        g.box((px, (y0 + y1) / 2, deck + 1.17), (0.62, y0 - y1, 0.14), M["dress"])
        g.box((px, (y0 + y1) / 2, deck + 1.27), (0.46, y0 - y1, 0.08), M["dress"])
        g.box((px + s * 0.36, (y0 + y1) / 2, deck - 0.05), (0.12, y0 - y1, 0.2), M["dress"])   # the string course outside
    # piers along the parapet every 7.5 m, each with a lantern on a post; the end piers taller
    yy = y0 - 0.5
    while yy > y1 - 0.1:
        for s in (-1, 1):
            px = cx + s * (w / 2 + 0.3)
            g.box((px, yy, deck + 0.8), (0.9, 0.9, 1.6), M["ash"])
            g.box((px, yy, deck + 1.66), (1.05, 1.05, 0.14), M["dress"])
            g.box((px, yy, deck + 1.8), (0.8, 0.8, 0.14), M["dress"])
            g.cyl((px, yy, deck + 2.6), 0.07, 1.6, M["iron"], n=10)
            g.cyl((px, yy, deck + 1.95), 0.16, 0.18, M["iron"], n=10)
            for zz in (deck + 2.3, deck + 3.1):
                g.cyl((px, yy, zz), 0.1, 0.06, M["iron"], n=10)
            lantern(g, M, (px, yy, deck + 3.42), 1.05)
        yy -= 7.5
    for s in (-1, 1):                                                                  # the far end's piers and railings
        px = cx + s * (w / 2 + 0.6)
        g.box((px, y1 - 0.4, deck + 1.3), (1.3, 1.3, 2.6), M["ash"])
        diaper(g, M, px - 0.5, px + 0.5, deck + 0.6, deck + 2.2, y1 - 1.06, M["carve"])
        g.box((px, y1 - 0.4, deck + 2.7), (1.5, 1.5, 0.2), M["dress"])
        tmp = bmesh.new()
        bmesh.ops.create_icosphere(tmp, subdivisions=2, radius=0.38)
        bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector((px, y1 - 0.4, deck + 3.2))), verts=tmp.verts)
        g._merge(tmp, M["dress"])
        rx0 = px + s * 0.65
        for k in range(26):
            rx = rx0 + s * k * 0.15
            g.box((rx, y1 - 0.4, deck + 0.6), (0.025, 0.025, 1.2), M["iron"])
            g.cyl((rx, y1 - 0.4, deck + 1.25), 0.03, 0.1, M["iron"], n=6, r2=0.0)
        g.box((rx0 + s * 1.95, y1 - 0.4, deck + 1.1), (3.9, 0.04, 0.05), M["iron"])
        g.box((rx0 + s * 1.95, y1 - 0.4, deck + 0.2), (3.9, 0.04, 0.05), M["iron"])
    # under the deck: tall arches on slender piers down to the river, cutwaters, voussoirs, spandrels
    piers = [y0 - 0.2, -37.5, -45.0, -52.5, y1 + 0.2]
    spring = deck - 7.5
    for i in range(len(piers) - 1):
        a, b = piers[i], piers[i + 1]
        mid = (a + b) / 2
        half = (a - b) / 2 - 0.8
        pts = [(mid - math.cos(t) * half, spring + math.sin(t) * half) for t in [math.pi * k / 16 for k in range(17)]]
        ring(g, pts, cx, w + 0.8, 0.45, M["ash"], axis="y")
        fill_above(g, pts, cx, w + 0.8, deck - 0.8, M["ash_y"], step=0.25, axis="y")
    for (ay, ground) in ((y0 - 0.2, RIVER_Z), (y1 + 0.2, RIVER_Z)):                     # the abutments
        g.box((cx, ay, (ground + deck - 0.8) / 2), (w + 0.8, 1.6, deck - 0.8 - ground), M["ash"])
    for py in piers[1:-1]:
        g.box((cx, py, (RIVER_Z + spring) / 2), (w + 0.6, 1.6, spring - RIVER_Z), M["ash"])
        for zz in (RIVER_Z + 3.0, spring - 0.2):
            g.box((cx, py, zz), (w + 0.9, 1.9, 0.25), M["dress"])
        for s in (-1, 1):                                                              # cutwaters, up and down stream
            g.cyl((cx + s * (w / 2 + 0.3), py, RIVER_Z + 2.0), 0.95, 4.0, M["ash"], n=3, rot=(0, 0, 90 if s < 0 else -90))
            g.cyl((cx + s * (w / 2 + 0.3), py, RIVER_Z + 4.5), 0.95, 1.0, M["dress"], n=3, r2=0.1, rot=(0, 0, 90 if s < 0 else -90))
    return g.finish(coll)


# ---------------------------------------------------------------- the gorge and the far bank

def _ground(x, y):
    """The land in front of the hill: the cliffs under the lane and the terrace, the gorge's floor
    and its river, the far bank's cliff, the moor beyond."""
    base = UP - 6.0
    if y > -12.0:                                         # the cliff under the hill (in front of the lane)
        t = (y + 12.0) / 8.0
        h = RIVER_Z + 2.0 + (base - RIVER_Z - 2.0) * min(1.0, max(0.0, t)) ** 0.6
    elif y > -58.0:                                       # the gorge's floor
        h = RIVER_Z + 1.5 + 1.8 * noise.noise(Vector((x * 0.08, y * 0.08, 0.5)))
    elif y > -66.0:                                       # the far bank's cliff
        t = (-58.0 - y) / 8.0
        h = RIVER_Z + 1.5 + (UP - 0.1 - RIVER_Z - 1.5) * t ** 0.7
    else:                                                 # the moor
        h = UP - 0.1 + 0.8 * noise.noise(Vector((x * 0.03, y * 0.03, 1.7))) + max(0.0, (-y - 90.0) * 0.12)
        if abs(x - DOOR_X) < 3.4:
            h -= 0.45
    return h


def road_z(x, y):
    """The road on the far bank: level off the bridge, then up with the moor."""
    base = _ground(DOOR_X + 3.6, y) + 0.45 if y < -66.0 else UP
    return max(UP + 0.04, base + 0.06) if y > -90.0 else base + 0.06


def build_gorge(S, coll, M):
    g = S.Group("FortGorge")
    # the ground: cliffs (rock), the gorge floor, the moor (grass), the road over it
    D.terrain(g, -20, 320, -66.5, -4.0, _ground, M["rock_top"], res=1.6, amp=0.9, seed=13.0)
    D.terrain(g, -20, 320, -150, -66.0, _ground, M["grass"], res=1.8, amp=0.25, seed=17.0)
    D.terrain(g, DOOR_X - 2.8, DOOR_X + 2.8, -150, BRIDGE_Y1 - 0.8, road_z, M["gravel"], res=0.8, amp=0.02, seed=3.0)
    g.box((DOOR_X, (BRIDGE_Y1 - 66.6) / 2 + 0.0, (RIVER_Z + UP) / 2), (6.4, 66.6 + BRIDGE_Y1 + 0.2, UP - RIVER_Z), M["rock_y"])   # the embankment over the far cliff
    # the hill running on east of the hall (so nothing shows past its end)
    D.terrain(g, 166, 320, -4.5, 138, lambda x, y: UP - 3.0 + (y + 4.5) * 0.18 + 2.0 * noise.noise(Vector((x * 0.05, y * 0.05, 3.3))), M["rock_top"], res=3.0, amp=0.8, seed=21.0)
    # the river, braided round rocks, white where it breaks
    D.terrain(g, -20, 320, -50.0, -36.0, lambda x, y: RIVER_Z + 1.6 + 0.6 * math.sin(x * 0.07), M["river"], res=2.0, amp=0.2, seed=2.0)
    for _ in range(70):
        D.boulder(g, (rnd.uniform(60, 220), rnd.uniform(-55, -30), RIVER_Z + 1.6), rnd.uniform(0.6, 2.2), M["rock"])
    for _ in range(90):                                    # the cliff faces: big rocks
        x = rnd.uniform(44, 226)
        if COURT_X0 - 2 < x < COURT_X1 + 2:
            continue
        y = rnd.uniform(-11.5, -5.5)
        D.boulder(g, (x, y, _ground(x, y) + rnd.uniform(0, 2)), rnd.uniform(1.2, 3.0), M["rock"])
    for _ in range(60):
        x = rnd.uniform(44, 226)
        if abs(x - DOOR_X) < 6:
            continue
        y = rnd.uniform(-65.5, -58.5)
        D.boulder(g, (x, y, _ground(x, y)), rnd.uniform(1.0, 2.6), M["rock"])
    # the terrace's retaining wall: front and sides, battered, with buttresses and drain holes
    for (a, b) in ((COURT_X0, COURT_X1),):
        g.box(((a + b) / 2, COURT_Y1 + 0.2, (RIVER_Z + UP) / 2), (b - a, 1.2, UP - RIVER_Z), M["ash"])
        for k in range(int((b - a) / 5) + 1):
            bx = a + k * 5.0
            if abs(bx - DOOR_X) < 3.5:
                continue
            g.box((bx, COURT_Y1 - 0.6, (RIVER_Z + UP - 1.0) / 2), (1.3, 1.6, UP - 1.0 - RIVER_Z), M["ash_y"])
            g.box((bx, COURT_Y1 - 0.4, UP - 0.8), (1.4, 1.2, 0.5), M["dress"], rot=(30, 0, 0))
            g.cyl((bx + 2.5, COURT_Y1 - 0.42, UP - 4.0), 0.16, 0.2, M["void"], rot=(90, 0, 0), n=8)
        for zz in (UP - 0.2, UP - 10.0):
            g.box(((a + b) / 2, COURT_Y1 - 0.45, zz), (b - a, 0.3, 0.25), M["dress"])
    for sx in (COURT_X0 - 0.6, COURT_X1 + 0.6):
        g.box((sx, (FY + COURT_Y1) / 2, (RIVER_Z + UP) / 2), (1.2, FY - COURT_Y1 + 0.6, UP - RIVER_Z), M["ash_y"])
        for k in range(4):
            by = FY - 3.0 - k * 6.5
            g.box((sx - (0.7 if sx < DOOR_X else -0.7), by, (RIVER_Z + UP - 1.0) / 2), (1.4, 1.2, UP - 1.0 - RIVER_Z), M["ash"])
    # under the old lane: the hill's foot, a rough masonry base under the court's footing
    g.box((108.0, -4.9, UP - 9.0), (40.0, 0.9, 12.0), M["ash"])
    g.box((70.0, -4.9, UP - 9.0), (36.0, 0.9, 12.0), M["ash"])
    # the far bank: the road on, a dry-stone wall each side, a milestone, heather, a lone bent tree
    for s in (-1, 1):
        y = BRIDGE_Y1 - 6.5
        while y > -148:
            x = DOOR_X + s * 4.2 + 0.3 * math.sin(y * 0.3)
            z = _ground(x, y)
            for k in range(3):
                D.boulder(g, (x + rnd.uniform(-0.15, 0.15), y + rnd.uniform(-0.2, 0.2), z + 0.2 + k * 0.33), rnd.uniform(0.25, 0.36), M["rock"], squash=0.55)
            y -= 0.62
    g.box((DOOR_X + 3.3, -70.0, _ground(DOOR_X + 3.3, -70) + 0.45), (0.4, 0.3, 0.9), M["dress"])
    g.cyl((DOOR_X + 3.3, -70.0, _ground(DOOR_X + 3.3, -70) + 0.92), 0.2, 0.3, M["dress"], rot=(90, 0, 0), n=12)
    for _ in range(420):
        x = rnd.uniform(60, 230)
        y = rnd.uniform(-148, -67)
        if abs(x - DOOR_X) < 5.5:
            continue
        D.boulder(g, (x, y, _ground(x, y)), rnd.uniform(0.25, 0.7), M["heather"], squash=0.45)
    tx, ty = DOOR_X - 16.0, -74.0
    tz = _ground(tx, ty)
    g.beam((tx, ty, tz - 0.3), (tx + 0.8, ty, tz + 3.8), 0.32, 0.32, M["bark"])
    g.beam((tx + 0.8, ty, tz + 3.8), (tx + 2.6, ty - 0.2, tz + 5.4), 0.2, 0.2, M["bark"])
    for k in range(9):                                                                 # its wind-bent branches and leaves
        bx = tx + 0.5 + k * 0.35
        bz = tz + 2.8 + k * 0.35
        ex, ez = bx + 1.6 + rnd.uniform(0, 1.0), bz + rnd.uniform(0.2, 1.2)
        g.beam((bx, ty, bz), (ex, ty + rnd.uniform(-0.6, 0.6), ez), 0.07, 0.07, M["bark"])
        for j in range(4):
            D.boulder(g, (ex + rnd.uniform(-0.5, 0.5), ty + rnd.uniform(-0.6, 0.6), ez + rnd.uniform(-0.2, 0.4)), rnd.uniform(0.4, 0.75), M["hedge"], squash=0.6)
    return g.finish(coll)


def build_all(S, coll, M0):
    """The five front parts (all dawn-only)."""
    M = dict(M0)
    M.update(mats())
    parts = [build_facade(S, coll, M), build_porch(S, coll, M), build_forecourt(S, coll, M),
             build_bridge(S, coll, M), build_gorge(S, coll, M)]
    return parts
