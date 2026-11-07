"""Story mode, prologue: the warehouse set and the container yard, modelled in detail, lit for
night and BAKED, so the unlit game shows a lit film set at no runtime cost.

    python tools/blender/build_story_set.py              # build, bake, export
    python tools/blender/build_story_set.py --preview    # build and render the shots (no bake)

Coordinates: Blender X = Unity X (along the fight lane), Blender Y = Unity Z (depth: the back
wall is at +2.6, the camera side is negative), Blender Z = Unity Y (up). Units are metres.

Outputs (Assets/Art/Story/):
  StorySet.fbx       SetInterior, SetYard, SetGround, SetCity (static, one baked texture each),
                     Shutter and Fan (moving parts, their own small bakes), Shafts (moonlight
                     beams, not baked: an additive material in Unity), Mark_X/Y/Z (axis markers).
  Story_<object>.png the baked light (Unity: Unlit/Texture on UV0).
"""
import math
import os
import random
import sys

import bpy  # noqa: E402  (bpy first: it provides bmesh and mathutils)
import bmesh
from mathutils import Euler, Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import story_hall as H  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Story")
PREVIEW = os.path.join(ROOT, "docs", "previews")

HALF_W, ROOF, BACK, FRONT = 14.0, 7.2, 2.6, -1.0       # shed inner extents (Blender coords)
DOOR_L, DOOR_R, DOOR_H = 10.4, 13.6, 3.6
HERO_X = -8.0
CRACK_X0, CRACK_X1, CRACK_Y0, CRACK_Y1 = -9.05, -8.15, -0.75, 1.95
WINDOWS = [-10.0, -4.2, 1.6, 7.0]                     # clerestory window centres (x)
WIN_Z0, WIN_Z1, WIN_W = 4.7, 6.2, 2.6
MOON_DIR = Vector((0.28, -0.62, -0.73)).normalized()  # moonlight travels this way (from behind, high)

rnd = random.Random(7)

# ---------------------------------------------------------------- materials

MATS = {}


def mat(name, color, rough=0.8, emit=None, strength=0.0, metal=0.0, transparent=0.0):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emit is not None:
        bsdf.inputs["Emission Color"].default_value = (*emit, 1)
        bsdf.inputs["Emission Strength"].default_value = strength
    if transparent > 0:
        out = nt.nodes["Material Output"]
        tr = nt.nodes.new("ShaderNodeBsdfTransparent")
        mix = nt.nodes.new("ShaderNodeMixShader")
        mix.inputs[0].default_value = transparent
        nt.links.new(bsdf.outputs[0], mix.inputs[1])
        nt.links.new(tr.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], out.inputs[0])
    MATS[name] = m
    return m


def materials():
    M = {}
    M["concrete"] = mat("Concrete", (0.20, 0.205, 0.215), 0.9)
    M["concrete_dark"] = mat("ConcreteDark", (0.16, 0.16, 0.17), 0.9)
    M["block"] = mat("Block", (0.36, 0.35, 0.34), 0.95)
    M["mortar"] = mat("Mortar", (0.20, 0.20, 0.20), 1.0)
    M["iron"] = mat("Iron", (0.42, 0.45, 0.50), 0.6, metal=0.3)
    M["iron_dark"] = mat("IronDark", (0.20, 0.22, 0.25), 0.6, metal=0.3)
    M["rust"] = mat("Rust", (0.32, 0.14, 0.07), 0.9)
    M["steel"] = mat("Steel", (0.10, 0.105, 0.12), 0.5, metal=0.5)
    M["steel_mid"] = mat("SteelMid", (0.22, 0.23, 0.25), 0.5, metal=0.5)
    M["paint_yellow"] = mat("PaintYellow", (0.80, 0.62, 0.10), 0.7)
    M["paint_red"] = mat("PaintRed", (0.45, 0.06, 0.05), 0.7)
    M["paint_green"] = mat("PaintGreen", (0.10, 0.26, 0.18), 0.7)
    M["paint_blue"] = mat("PaintBlue", (0.08, 0.16, 0.34), 0.7)
    M["oil"] = mat("Oil", (0.10, 0.10, 0.11), 0.15)
    M["wood"] = mat("Wood", (0.30, 0.20, 0.11), 0.9)
    M["wood_dark"] = mat("WoodDark", (0.18, 0.12, 0.07), 0.9)
    M["rubber"] = mat("Rubber", (0.05, 0.05, 0.055), 0.9)
    M["glass"] = mat("NightGlass", (0.05, 0.07, 0.10), 0.1, emit=(0.10, 0.14, 0.24), strength=0.6, transparent=0.75)
    M["bulb_warm"] = mat("BulbWarm", (1, 0.7, 0.35), 0.5, emit=(1.0, 0.62, 0.28), strength=25.0)
    M["bulb_dead"] = mat("BulbDead", (0.3, 0.3, 0.28), 0.4)
    M["button_red"] = mat("ButtonRed", (0.6, 0.05, 0.04), 0.4, emit=(1, 0.08, 0.05), strength=3.0)
    M["button_green"] = mat("ButtonGreen", (0.05, 0.4, 0.1), 0.4, emit=(0.1, 1, 0.2), strength=1.2)
    M["sky_glow"] = mat("SkyGlow", (0.05, 0.06, 0.1), 0.5, emit=(0.16, 0.20, 0.34), strength=1.0)
    M["sodium"] = mat("Sodium", (1, 0.6, 0.25), 0.5, emit=(1.0, 0.55, 0.18), strength=40.0)
    M["aviation"] = mat("Aviation", (1, 0.1, 0.05), 0.5, emit=(1.0, 0.05, 0.03), strength=30.0)
    M["cab_window"] = mat("CabWindow", (0.6, 0.5, 0.3), 0.5, emit=(1.0, 0.75, 0.4), strength=6.0)
    M["asphalt"] = mat("Asphalt", (0.07, 0.075, 0.085), 0.35)
    M["puddle"] = mat("Puddle", (0.02, 0.025, 0.035), 0.05)
    M["container"] = [mat("ContRust", (0.36, 0.09, 0.06), 0.7), mat("ContTeal", (0.06, 0.24, 0.26), 0.7),
                      mat("ContOchre", (0.38, 0.26, 0.07), 0.7), mat("ContGrey", (0.24, 0.25, 0.27), 0.7),
                      mat("ContBlue", (0.07, 0.12, 0.30), 0.7), mat("ContWhite", (0.48, 0.48, 0.46), 0.7)]
    M["car"] = [mat("CarRed", (0.30, 0.05, 0.04), 0.5, metal=0.4), mat("CarBlue", (0.06, 0.10, 0.22), 0.5, metal=0.4),
                mat("CarGrey", (0.18, 0.18, 0.19), 0.5, metal=0.4), mat("CarBeige", (0.36, 0.31, 0.22), 0.5, metal=0.4)]
    M["crane"] = mat("CraneYellow", (0.55, 0.40, 0.05), 0.6, metal=0.3)
    M["city"] = mat("CityTower", (0.04, 0.045, 0.06), 0.7)
    M["city_win"] = mat("CityWindows", (0.6, 0.5, 0.3), 0.5, emit=(1.0, 0.72, 0.42), strength=3.0)
    M["city_win_cold"] = mat("CityWindowsCold", (0.4, 0.5, 0.6), 0.5, emit=(0.55, 0.7, 1.0), strength=2.5)
    M["neon_pink"] = mat("NeonPink", (0.8, 0.2, 0.6), 0.5, emit=(1.0, 0.15, 0.65), strength=10.0)
    M["neon_cyan"] = mat("NeonCyan", (0.2, 0.7, 0.8), 0.5, emit=(0.15, 0.85, 1.0), strength=8.0)
    return M


# ---------------------------------------------------------------- geometry accumulation
# Each group gathers closed solids (normals fixed per piece) into one bmesh, so a few objects
# carry thousands of parts.

class Group:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []

    def _mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def _merge(self, tmp, m):
        bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces[:])
        me = bpy.data.meshes.new("tmp")
        tmp.to_mesh(me)
        tmp.free()
        idx = self._mi(m)
        for p in me.polygons:
            p.material_index = idx
        off = len(self.bm.verts)
        self.bm.from_mesh(me)
        bpy.data.meshes.remove(me)
        # from_mesh appends; material indices come with the polygons
        return off

    def box(self, c, s, m, rot=None):
        tmp = bmesh.new()
        bmesh.ops.create_cube(tmp, size=1.0)
        M = Matrix.Translation(Vector(c))
        if rot is not None:
            M = M @ (rot if isinstance(rot, Matrix) else Euler([math.radians(a) for a in rot]).to_matrix().to_4x4())
        M = M @ Matrix.Diagonal((*s, 1))
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self._merge(tmp, m)

    def beam(self, a, b, w, h, m, up=Vector((0, 0, 1))):
        a, b = Vector(a), Vector(b)
        d = b - a
        L = d.length
        if L < 1e-4:
            return
        z = d.normalized()
        x = up.cross(z)
        if x.length < 1e-3:
            x = Vector((1, 0, 0)).cross(z)
        x.normalize()
        y = z.cross(x)
        R = Matrix((x, y, z)).transposed().to_4x4()
        tmp = bmesh.new()
        bmesh.ops.create_cube(tmp, size=1.0)
        M = Matrix.Translation((a + b) * 0.5) @ R @ Matrix.Diagonal((w, h, L, 1))
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self._merge(tmp, m)

    def cyl(self, c, r, h, m, rot=None, n=16, r2=None):
        tmp = bmesh.new()
        bmesh.ops.create_cone(tmp, cap_ends=True, segments=n, radius1=r, radius2=r if r2 is None else r2, depth=h)
        M = Matrix.Translation(Vector(c))
        if rot is not None:
            M = M @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self._merge(tmp, m)

    def torus(self, c, R, r, m, rot=None, n1=18, n2=8):
        tmp = bmesh.new()
        rings = []
        for i in range(n1):
            a = i / n1 * math.tau
            ring = []
            for j in range(n2):
                b = j / n2 * math.tau
                p = Vector(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a), r * math.sin(b)))
                ring.append(tmp.verts.new(p))
            rings.append(ring)
        for i in range(n1):
            for j in range(n2):
                a, b2 = rings[i], rings[(i + 1) % n1]
                tmp.faces.new((a[j], b2[j], b2[(j + 1) % n2], a[(j + 1) % n2]))
        M = Matrix.Translation(Vector(c))
        if rot is not None:
            M = M @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self._merge(tmp, m)

    def prism(self, pts2d, y0, y1, m, axis="y"):
        """Extrude a 2D outline (x, z) along y (a car body profile, a sign)."""
        tmp = bmesh.new()
        f0 = [tmp.verts.new((x, y0, z)) for x, z in pts2d]
        f1 = [tmp.verts.new((x, y1, z)) for x, z in pts2d]
        tmp.faces.new(f0)
        tmp.faces.new(list(reversed(f1)))
        n = len(pts2d)
        for i in range(n):
            tmp.faces.new((f0[i], f0[(i + 1) % n], f1[(i + 1) % n], f1[i]))
        self._merge(tmp, m)
        return

    def finish(self, coll):
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        ob = bpy.data.objects.new(self.name, me)
        for m in self.mats:
            me.materials.append(m)
        coll.objects.link(ob)
        return ob


# ---------------------------------------------------------------- the shed

def stencil(g, text, c, size, m, facing=-1, rot_z=0.0):
    """Painted stencil letters on a wall (a text object turned into a thin slab)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.004
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    R = Matrix.Rotation(math.radians(90), 4, "X")                # text stands up in the x-z plane
    if facing > 0:
        R = Matrix.Rotation(math.radians(180), 4, "Z") @ R
    me.transform(Matrix.Translation(Vector(c)) @ Matrix.Rotation(math.radians(rot_z), 4, "Y") @ R)
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def corrugated(g, x0, x1, z0, z1, y, side, M, ribs=True, rust_chance=0.12):
    """A corrugated iron sheet in the x-z plane at depth y; ribs stand out towards `side`
    (-1 = towards the camera, +1 = away). Seams every 3 m; rust on some sheets."""
    if x1 - x0 < 0.05 or z1 - z0 < 0.05:
        return
    g.box(((x0 + x1) * 0.5, y, (z0 + z1) * 0.5), (x1 - x0, 0.03, z1 - z0), M["iron"])
    if not ribs:
        return
    x = x0 + 0.1
    while x < x1 - 0.05:
        m = M["rust"] if rnd.random() < rust_chance * 0.3 else M["iron"]
        for s in (side, -side):
            g.box((x, y + s * 0.03, (z0 + z1) * 0.5), (0.07, 0.035, z1 - z0), m if s == side else M["iron_dark"])
        x += 0.2
    # sheet seams (lap joints) and rust bleeding from the bolts
    sx = x0 + 3.0
    while sx < x1:
        g.box((sx, y + side * 0.06, (z0 + z1) * 0.5), (0.05, 0.03, z1 - z0), M["iron_dark"])
        if rnd.random() < 0.5:
            hz = rnd.uniform(0.6, 1.8)
            g.box((sx + 0.15, y + side * 0.052, z1 - hz * 0.5 - 0.1), (0.18, 0.012, hz), M["rust"])
        sx += 3.0


def block_wall(g, x0, x1, z0, z1, y, M):
    """Concrete block plinth: blocks proud of the mortar."""
    g.box(((x0 + x1) * 0.5, y + 0.02, (z0 + z1) * 0.5), (x1 - x0, 0.2, z1 - z0), M["mortar"])
    row = 0
    z = z0
    while z < z1 - 0.01:
        x = x0 + (0.2 if row % 2 else 0.0)
        while x < x1 - 0.02:
            w = min(0.39, x1 - x - 0.01)
            if w > 0.05:
                g.box((x + w * 0.5, y - 0.1, z + 0.095), (w, 0.04, 0.18), M["block"] if rnd.random() > 0.08 else M["concrete_dark"])
            x += 0.4
        z += 0.2
        row += 1


def window(g, cx, M, broken):
    """A steel-framed clerestory window: frame, 3x2 mullions, glass in the intact panes."""
    y = BACK + 0.12
    x0, x1 = cx - WIN_W / 2, cx + WIN_W / 2
    for z in (WIN_Z0, WIN_Z1):
        g.box((cx, y, z), (WIN_W + 0.12, 0.14, 0.1), M["steel"])
    for x in (x0, x1):
        g.box((x, y, (WIN_Z0 + WIN_Z1) / 2), (0.1, 0.14, WIN_Z1 - WIN_Z0), M["steel"])
    for i in (1, 2):
        g.box((x0 + i * WIN_W / 3, y, (WIN_Z0 + WIN_Z1) / 2), (0.05, 0.08, WIN_Z1 - WIN_Z0), M["steel"])
    g.box((cx, y, (WIN_Z0 + WIN_Z1) / 2), (WIN_W, 0.08, 0.05), M["steel"])
    g.box((cx, y - 0.1, WIN_Z0 - 0.06), (WIN_W + 0.3, 0.22, 0.06), M["concrete_dark"])     # sill
    for i in range(3):
        for j in range(2):
            if (i, j) in broken:
                # a few teeth of glass left in the frame
                px = x0 + (i + 0.5) * WIN_W / 3
                pz = WIN_Z0 + (j + 0.5) * (WIN_Z1 - WIN_Z0) / 2
                g.box((px - 0.25, y + 0.02, pz - 0.25), (0.22, 0.01, 0.18), M["glass"], rot=(0, 25, 0))
                continue
            px = x0 + (i + 0.5) * WIN_W / 3
            pz = WIN_Z0 + (j + 0.5) * (WIN_Z1 - WIN_Z0) / 2
            g.box((px, y + 0.02, pz), (WIN_W / 3 - 0.06, 0.012, (WIN_Z1 - WIN_Z0) / 2 - 0.06), M["glass"])


def chain(g, top, length, M, link=0.07):
    n = int(length / (link * 1.4))
    for i in range(n):
        z = top[2] - i * link * 1.4
        g.torus((top[0], top[1], z), link * 0.5, 0.012, M["steel_mid"], rot=(0, 0, 90 * (i % 2)) if False else ((90, 0, 0) if i % 2 == 0 else (90, 0, 90)), n1=10, n2=5)
    return top[2] - n * link * 1.4


def hook(g, c, M):
    g.box((c[0], c[1], c[2] + 0.12), (0.22, 0.14, 0.26), M["paint_yellow"])                  # block
    g.torus((c[0], c[1], c[2] - 0.12), 0.09, 0.025, M["steel_mid"], rot=(90, 0, 0), n1=14, n2=6)


def ibeam(g, c, h, M, m=None):
    m = m or M["steel"]
    g.box((c[0], c[1], c[2] + h / 2), (0.26, 0.03, h), m)                                    # web
    for s in (-1, 1):
        g.box((c[0], c[1] + s * 0.11, c[2] + h / 2), (0.3, 0.025, h), m)                     # flanges
    g.box((c[0], c[1], c[2] + 0.015), (0.5, 0.42, 0.03), M["steel_mid"])                   # base plate
    for dx in (-0.18, 0.18):
        for dy in (-0.15, 0.15):
            g.cyl((c[0] + dx, c[1] + dy, c[2] + 0.05), 0.02, 0.05, M["steel_mid"], n=6)


def truss(g, x, M):
    """A Warren roof truss across the depth, with its purlins."""
    y0, y1 = FRONT, BACK
    zb, zt = ROOF - 0.95, ROOF - 0.08
    g.beam((x, y0, zb), (x, y1, zb), 0.1, 0.12, M["steel"])
    g.beam((x, y0, zt), (x, y1, zt), 0.1, 0.12, M["steel"])
    n = 5
    for i in range(n + 1):
        y = y0 + (y1 - y0) * i / n
        g.beam((x, y, zb), (x, y, zt), 0.07, 0.07, M["steel"])
        if i < n:
            ya, yb = y, y0 + (y1 - y0) * (i + 1) / n
            g.beam((x, ya, zb if i % 2 == 0 else zt), (x, yb, zt if i % 2 == 0 else zb), 0.06, 0.06, M["steel"])


def lamp(g, c, M, working):
    g.beam((c[0], c[1], ROOF - 0.1), (c[0], c[1], c[2] + 0.18), 0.015, 0.015, M["steel"])   # cord
    g.cyl((c[0], c[1], c[2] + 0.08), 0.06, 0.18, M["steel_mid"], n=10)
    g.cyl((c[0], c[1], c[2]), 0.38, 0.22, M["paint_green"], n=20, r2=0.07)                  # enamel dish shade
    g.cyl((c[0], c[1], c[2] - 0.12), 0.07, 0.1, M["bulb_warm"] if working else M["bulb_dead"], n=10)


def press(g, x, y, M):
    g.box((x, y, 0.5), (2.4, 1.0, 1.0), M["paint_green"])
    g.box((x, y, 1.05), (2.2, 0.8, 0.1), M["steel_mid"])                                     # bolster
    for s in (-1, 1):
        g.box((x + s * 0.95, y, 2.1), (0.3, 0.32, 2.1), M["paint_green"])
    g.box((x, y, 3.35), (2.6, 0.9, 0.7), M["paint_green"])                                   # crown
    g.cyl((x, y, 2.75), 0.24, 0.6, M["steel_mid"])                                           # ram cylinder
    g.cyl((x, y, 2.3), 0.12, 0.4, M["steel"])
    g.box((x, y, 2.05), (1.9, 0.7, 0.15), M["steel_mid"])                                    # platen
    g.box((x + 1.35, y - 0.3, 1.6), (0.36, 0.22, 0.5), M["paint_yellow"])                    # control box
    g.cyl((x + 1.3, y - 0.42, 1.7), 0.04, 0.04, M["button_red"], rot=(90, 0, 0), n=8)
    g.cyl((x + 1.42, y - 0.42, 1.7), 0.035, 0.04, M["button_green"], rot=(90, 0, 0), n=8)
    for i in range(6):                                                                       # hydraulic hose sagging
        a, b = i / 6, (i + 1) / 6
        pa = (x + 1.1 + 0.4 * a, y + 0.3, 3.0 - 1.4 * a + 0.5 * math.sin(a * math.pi))
        pb = (x + 1.1 + 0.4 * b, y + 0.3, 3.0 - 1.4 * b + 0.5 * math.sin(b * math.pi))
        g.beam(pa, pb, 0.05, 0.05, M["rubber"])
    for dx in (-0.6, 0.0, 0.6):                                                              # warning stripes
        g.box((x + dx, y - 0.505, 0.85), (0.22, 0.01, 0.12), M["paint_yellow"], rot=(0, 30, 0))


def lathe(g, x, y, M):
    for s in (-1, 1):
        g.box((x + s * 1.3, y, 0.42), (0.5, 0.55, 0.84), M["paint_blue"])                   # cabinets
    g.box((x, y, 0.82), (3.4, 0.5, 0.06), M["steel_mid"])                                    # chip tray
    g.box((x, y, 0.98), (3.2, 0.32, 0.22), M["steel"])                                       # bed
    g.box((x - 1.2, y, 1.32), (0.7, 0.55, 0.55), M["paint_blue"])                            # headstock
    g.cyl((x - 0.78, y, 1.3), 0.17, 0.16, M["steel_mid"], rot=(0, 90, 0))                   # chuck
    for a in range(3):
        g.box((x - 0.68, y + 0.13 * math.cos(a * 2.09), 1.3 + 0.13 * math.sin(a * 2.09)), (0.05, 0.04, 0.04), M["steel"])
    g.box((x + 0.2, y, 1.2), (0.35, 0.4, 0.2), M["paint_blue"])                              # carriage
    g.box((x + 1.25, y, 1.25), (0.35, 0.3, 0.32), M["paint_blue"])                           # tailstock
    g.cyl((x + 1.0, y, 1.3), 0.03, 0.4, M["steel_mid"], rot=(0, 90, 0), n=8)
    g.box((x - 1.3, y + 0.33, 1.45), (0.4, 0.12, 0.5), M["steel"])                           # motor


def bench(g, x, y, M):
    g.box((x, y, 0.88), (2.2, 0.7, 0.06), M["wood"])
    for dx in (-1.0, 1.0):
        for dy in (-0.3, 0.3):
            g.box((x + dx, y + dy, 0.43), (0.06, 0.06, 0.86), M["steel"])
    g.box((x, y, 0.2), (2.0, 0.6, 0.03), M["wood_dark"])                                     # shelf
    g.box((x + 0.85, y - 0.2, 0.98), (0.2, 0.14, 0.14), M["paint_blue"])                     # vise
    g.box((x + 0.85, y - 0.31, 0.98), (0.04, 0.08, 0.02), M["steel"])
    g.box((x - 0.5, y + 0.05, 1.0), (0.5, 0.26, 0.2), M["paint_red"])                        # toolbox
    g.box((x, BACK - 0.03, 1.75), (2.2, 0.03, 1.0), M["wood_dark"])                          # pegboard
    tools = [(-0.8, 0.5, 0.05, "wrench"), (-0.55, 0.42, 0.05, "wrench"), (-0.2, 0.36, 0.08, "hammer"),
             (0.15, 0.5, 0.06, "saw"), (0.55, 0.3, 0.04, "file"), (0.8, 0.4, 0.06, "wrench")]
    for dx, h, w, kind in tools:
        g.box((x + dx, BACK - 0.06, 1.75), (w, 0.02, h), M["steel_mid"])
        if kind == "hammer":
            g.box((x + dx, BACK - 0.06, 1.75 + h / 2), (0.16, 0.03, 0.06), M["steel"])
        if kind == "saw":
            g.box((x + dx + 0.12, BACK - 0.06, 1.7), (0.22, 0.015, 0.3), M["steel_mid"])


def forklift(g, x, y, M, yaw, fork_z=0.1):
    rot = Matrix.Rotation(math.radians(yaw), 4, "Z")
    def P(dx, dy, dz):
        return (Matrix.Translation((x, y, 0)) @ rot @ Vector((dx, dy, dz)))[:]
    R = Euler((0, 0, math.radians(yaw))).to_matrix().to_4x4()
    g.box(P(0, 0, 0.55), (1.8, 1.05, 0.6), M["paint_yellow"], rot=R)                         # body
    g.box(P(-0.75, 0, 0.75), (0.45, 1.05, 0.9), M["steel"], rot=R)                            # counterweight
    g.box(P(0.0, 0, 0.95), (0.45, 0.5, 0.2), M["rubber"], rot=R)                              # seat
    for dx in (-0.55, 0.45):
        for dy in (-0.45, 0.45):
            g.box(P(dx, dy, 1.55), (0.06, 0.06, 1.2), M["steel"], rot=R)                     # cage posts
            g.cyl(P(dx * 1.05, dy * 1.18, 0.28), 0.28, 0.2, M["rubber"], rot=(90, 0, yaw), n=14)
    g.box(P(-0.05, 0, 2.15), (1.1, 1.0, 0.06), M["steel"], rot=R)                             # roof guard
    for dy in (-0.32, 0.32):
        g.box(P(1.05, dy, 1.35), (0.1, 0.1, 2.6), M["steel_mid"], rot=R)                      # mast rails
        g.box(P(1.55, dy, fork_z), (1.1, 0.12, 0.05), M["steel_mid"], rot=R)                  # forks
    g.box(P(1.12, 0, fork_z + 0.5), (0.06, 0.8, 0.5), M["steel"], rot=R)                      # carriage


def drum(g, c, M, m, lying=False):
    rot = (90, 0, rnd.uniform(-30, 30)) if lying else None
    g.cyl(c, 0.29, 0.88, m, rot=rot, n=20)
    for dz in (-0.29, 0.0, 0.29):
        off = Vector((0, 0, dz))
        if lying:
            off = Euler([math.radians(a) for a in rot]).to_matrix() @ off
        g.torus((Vector(c) + off)[:], 0.29, 0.018, m, rot=rot if lying else None, n1=20, n2=5)


def pallet(g, c, M):
    x, y, z = c
    for dy in (-0.45, 0.0, 0.45):
        g.box((x, y + dy, z + 0.05), (1.2, 0.1, 0.1), M["wood_dark"])
    for i in range(7):
        g.box((x - 0.54 + i * 0.18, y, z + 0.12), (0.12, 1.0, 0.025), M["wood"])


def crate(g, c, s, M):
    g.box(c, s, M["wood"])
    x, y, z = c
    for dz in (-s[2] / 2 + 0.04, s[2] / 2 - 0.04):
        g.box((x, y - s[1] / 2 - 0.01, z + dz), (s[0], 0.02, 0.07), M["wood_dark"])
    for dx in (-s[0] / 2 + 0.04, s[0] / 2 - 0.04):
        g.box((x + dx, y - s[1] / 2 - 0.01, z), (0.07, 0.02, s[2]), M["wood_dark"])
    g.beam((x - s[0] / 2, y - s[1] / 2 - 0.015, z - s[2] / 2), (x + s[0] / 2, y - s[1] / 2 - 0.015, z + s[2] / 2), 0.07, 0.02, M["wood_dark"], up=Vector((0, 1, 0)))


def pipes(g, M):
    for z, r in ((2.5, 0.09), (2.85, 0.06)):
        g.cyl((-2.5, BACK - 0.2, z), r, 23.0, M["steel_mid"] if r > 0.07 else M["rust"], rot=(0, 90, 0), n=12)
        x = -13.5
        while x < 9.0:
            g.cyl((x, BACK - 0.2, z), r + 0.03, 0.06, M["steel"], rot=(0, 90, 0), n=12)      # flanges
            x += 2.0
        for s in (-1, 1):                                                                    # brackets
            pass
    for x in (-13.0, -7.0, -1.0, 5.0):
        g.box((x, BACK - 0.11, 2.68), (0.05, 0.18, 0.55), M["steel"])
    # a valve wheel
    g.torus((-6.0, BACK - 0.36, 2.5), 0.16, 0.02, M["paint_red"], rot=(90, 0, 0), n1=18, n2=6)
    for a in range(4):
        g.beam((-6.0, BACK - 0.36, 2.5), (-6.0 + 0.16 * math.cos(a * 1.57), BACK - 0.36, 2.5 + 0.16 * math.sin(a * 1.57)), 0.02, 0.02, M["paint_red"])
    g.cyl((-6.0, BACK - 0.3, 2.5), 0.05, 0.12, M["steel"], rot=(90, 0, 0), n=8)
    # down into the floor at the left end
    g.cyl((-13.5, BACK - 0.2, 1.25), 0.09, 2.5, M["steel_mid"], n=12)
    # fuse box, conduit, a sign
    g.box((-12.4, BACK - 0.08, 1.7), (0.6, 0.16, 0.8), M["steel_mid"])
    g.box((-12.4, BACK - 0.17, 1.7), (0.55, 0.02, 0.74), M["iron_dark"])
    g.cyl((-12.4, BACK - 0.1, 4.2), 0.025, 4.2, M["steel"], n=6)
    g.box((-12.4, BACK - 0.18, 2.25), (0.4, 0.01, 0.12), M["paint_yellow"])


def build_shed(coll, M):
    g = Group("SetInterior")
    # floor: slab, joints, paint, oil, a drain
    g.box((0, (FRONT + BACK) / 2 - 0.4, -0.1), (HALF_W * 2 + 0.6, BACK - FRONT + 1.4, 0.2), M["concrete"])
    for x in range(-12, 14, 4):
        g.box((x, 0.6, 0.002), (0.03, BACK - FRONT + 1.0, 0.004), M["concrete_dark"])
    g.box((0, 1.0, 0.002), (HALF_W * 2, 0.03, 0.004), M["concrete_dark"])
    g.box((-1.5, 1.85, 0.003), (21.0, 0.08, 0.006), M["paint_yellow"])                       # walkway line
    for i in range(12):                                                                      # hazard stripes at the door
        g.box((DOOR_L + 0.15 + i * 0.27, 1.6, 0.003), (0.12, 1.6, 0.006), M["paint_yellow"], rot=(0, 0, 35))
    for x, y, s in ((-6.5, 1.7, 0.55), (3.4, 1.6, 0.7), (-2.0, 2.0, 0.45), (8.0, 1.5, 0.6), (11.0, 1.2, 0.5)):
        g.cyl((x, y, 0.002), s, 0.004, M["oil"], n=18)                                      # oil stains, by the wall
    g.box((3.0, 1.2, 0.003), (0.6, 0.6, 0.006), M["steel"])
    for i in range(6):
        g.box((2.78 + i * 0.09, 1.2, 0.006), (0.04, 0.56, 0.006), M["concrete_dark"])
    # back wall: block plinth, corrugated iron above, windows, the door opening
    yW = BACK + 0.12
    for xa, xb in ((-HALF_W, DOOR_L), (DOOR_R, HALF_W)):
        block_wall(g, xa, xb, 0.0, 1.2, BACK + 0.1, M)
        corrugated(g, xa, xb, 1.2, WIN_Z0 if xb <= DOOR_L else ROOF, yW, -1, M)
    corrugated(g, DOOR_L, DOOR_R, DOOR_H + 0.3, ROOF, yW, -1, M)
    # the window band between the windows, and the band above
    edges = [-HALF_W] + sum([[c - WIN_W / 2 - 0.06, c + WIN_W / 2 + 0.06] for c in WINDOWS], []) + [DOOR_L]
    for i in range(0, len(edges), 2):
        corrugated(g, edges[i], edges[i + 1], WIN_Z0, WIN_Z1, yW, -1, M)
    corrugated(g, -HALF_W, DOOR_L, WIN_Z1, ROOF, yW, -1, M)
    broken = [{(0, 0), (1, 1)}, {(2, 0), (0, 1), (1, 0)}, {(1, 1)}, {(0, 0), (2, 1), (1, 0)}]
    for c, b in zip(WINDOWS, broken):
        window(g, c, M, b)
    # side walls
    for s in (-1, 1):
        if s < 0:
            g.box((s * (HALF_W + 0.15), (FRONT + BACK) / 2 + 0.2, ROOF / 2), (0.3, BACK - FRONT + 0.8, ROOF), M["iron_dark"])
        else:
            # the east end: the opening of the inner shutter into the main hall (story_hall.INNER)
            yo0, yo1, ho = H.INNER
            ya, yb = FRONT - 0.2, BACK + 0.6
            g.box((HALF_W + 0.15, (ya + yo0) / 2, ROOF / 2), (0.3, yo0 - ya, ROOF), M["iron_dark"])
            g.box((HALF_W + 0.15, (yo1 + yb) / 2, ROOF / 2), (0.3, yb - yo1, ROOF), M["iron_dark"])
            g.box((HALF_W + 0.15, (yo0 + yo1) / 2, (ho + ROOF) / 2), (0.3, yo1 - yo0, ROOF - ho), M["iron_dark"])
            g.box((HALF_W - 0.1, (yo0 + yo1) / 2, ho + 0.25), (0.4, yo1 - yo0 + 0.4, 0.5), M["steel"])          # the drum
            for yy in (yo0, yo1):
                g.box((HALF_W - 0.05, yy, ho / 2), (0.15, 0.12, ho), M["steel"])
        for z in (0.6, 3.6):
            if s > 0:
                for (ya, yb) in ((FRONT - 0.1, H.INNER[0]), (H.INNER[1], BACK + 0.5)):
                    g.box((HALF_W - 0.02, (ya + yb) / 2, z), (0.06, yb - ya, 0.12), M["steel"])
                continue
            g.box((s * (HALF_W - 0.02), (FRONT + BACK) / 2 + 0.2, z), (0.06, BACK - FRONT + 0.6, 0.12), M["steel"])
    # roof: the underside sheet and the roof top, whole (the crack, its torn lips and the storm light
    # through it are added in the game, on the line the drop falls from)
    g.box((0, (FRONT + BACK) / 2, ROOF + 0.02), (HALF_W * 2 + 0.6, BACK - FRONT + 0.6, 0.04), M["iron_dark"])
    g.box((0, (FRONT + BACK) / 2, ROOF + 0.32), (HALF_W * 2 + 0.6, BACK - FRONT + 0.6, 0.06), M["rust"])
    x = -HALF_W
    while x < HALF_W:                                                                        # roof ribs (underside)
        if not (CRACK_X0 - 0.4 < x < CRACK_X1 + 0.6):                                         # none across the crack
            g.box((x, (FRONT + BACK) / 2, ROOF - 0.01), (0.08, BACK - FRONT + 0.6, 0.03), M["iron"])
        x += 0.3
    # structure
    x = -HALF_W + 1.2
    while x < HALF_W:
        truss(g, x, M)
        x += 3.4
    for y in (FRONT + 0.2, 0.4, 1.4, BACK - 0.2):
        g.box((0, y, ROOF - 0.15), (HALF_W * 2, 0.08, 0.14), M["steel"])                     # purlins
    for x in (-HALF_W + 3.5, -HALF_W + 10.5, -HALF_W + 17.5):
        ibeam(g, (x, BACK - 0.14, 0.0), ROOF - 0.2, M)
    ibeam(g, (DOOR_L - 0.25, BACK - 0.14, 0.0), ROOF - 0.2, M)
    ibeam(g, (DOOR_R + 0.25, BACK - 0.14, 0.0), ROOF - 0.2, M)
    # a crane rail along the back with a trolley, chain and hook block
    g.box((0, 2.05, 5.85), (HALF_W * 2, 0.26, 0.3), M["paint_yellow"])
    g.box((-2.2, 2.05, 5.55), (0.6, 0.5, 0.35), M["steel_mid"])
    end = chain(g, (-2.2, 2.05, 5.35), 2.1, M)
    hook(g, (-2.2, 2.05, end - 0.2), M)
    # chains hanging from the front purlin
    for cx in (-3.2, 2.6, 7.4):
        end = chain(g, (cx, FRONT + 0.2, ROOF - 0.25), 1.7 + abs(cx) * 0.05, M)
        g.torus((cx, FRONT + 0.2, end - 0.06), 0.08, 0.02, M["steel_mid"], rot=(90, 0, 0), n1=12, n2=5)
    # lamps: one still burning (sodium warm), the rest dead
    lamp(g, (-6.0, 0.4, 5.4), M, False)
    lamp(g, (-0.4, 0.4, 5.6), M, True)
    lamp(g, (5.6, 0.4, 5.4), M, False)
    lamp(g, (10.2, 0.4, 5.5), M, False)
    # machines, bench, pipes
    press(g, -1.4, 1.95, M)
    lathe(g, 5.2, 2.0, M)
    bench(g, -5.6, 2.15, M)
    pipes(g, M)
    # forklift parked crooked, scrap, drums, pallets, crates, tyres, a ladder
    forklift(g, -11.0, 1.55, M, 18)
    for i in range(10):
        s = (rnd.uniform(0.4, 1.3), rnd.uniform(0.3, 0.8), rnd.uniform(0.2, 0.7))
        g.box((rnd.uniform(-13.4, -12.2), rnd.uniform(1.9, 2.4), s[2] / 2 + rnd.uniform(0, 0.4)), s,
              M["rust"] if i % 3 == 0 else M["steel_mid"], rot=(rnd.uniform(-20, 20), rnd.uniform(-20, 20), rnd.uniform(0, 90)))
    for i in range(4):
        g.cyl((rnd.uniform(-13.4, -12.0), rnd.uniform(1.8, 2.3), rnd.uniform(0.3, 0.9)), 0.05, rnd.uniform(1.2, 2.4),
              M["rust"], rot=(rnd.uniform(40, 90), 0, rnd.uniform(0, 180)), n=8)
    drum(g, (8.45, 2.1, 0.44), M, M["paint_blue"])
    drum(g, (9.08, 2.15, 0.44), M, M["rust"])
    drum(g, (8.78, 2.12, 1.32), M, M["paint_red"])
    drum(g, (7.6, 1.5, 0.29), M, M["rust"], lying=True)
    for k in range(4):
        pallet(g, (9.85, 1.95, k * 0.15), M)
    crate(g, (-7.3, 2.1, 0.45), (0.9, 0.8, 0.9), M)
    crate(g, (-7.25, 2.1, 1.2), (0.7, 0.7, 0.6), M)
    crate(g, (-8.25, 2.15, 0.35), (0.7, 0.6, 0.7), M)
    for k in range(4):
        g.torus((-12.9, 0.9, 0.12 + k * 0.24), 0.32, 0.12, M["rubber"], n1=18, n2=8)
    g.torus((-12.2, 0.7, 0.36), 0.32, 0.12, M["rubber"], rot=(70, 0, 20), n1=18, n2=8)
    for s in (-1, 1):                                                                        # ladder
        g.beam((3.1 + s * 0.22, 2.2, 0.0), (3.1 + s * 0.22, 2.48, 3.4), 0.05, 0.05, M["paint_yellow"])
    for k in range(1, 11):
        z = k * 0.31
        g.beam((2.88, 2.2 + z * 0.082, z), (3.32, 2.2 + z * 0.082, z), 0.03, 0.03, M["steel_mid"])
    # the door: guide rails, the drum housing, a chain hoist
    for x in (DOOR_L + 0.05, DOOR_R - 0.05):
        g.box((x, BACK + 0.05, DOOR_H / 2), (0.1, 0.14, DOOR_H), M["steel"])
    g.cyl(((DOOR_L + DOOR_R) / 2, BACK - 0.05, DOOR_H + 0.25), 0.28, DOOR_R - DOOR_L + 0.3, M["steel_mid"], rot=(0, 90, 0), n=18)
    end = chain(g, (DOOR_R + 0.2, BACK - 0.25, DOOR_H + 0.2), 2.2, M, link=0.05)
    # stencils: the bay number, a warning, and the shutter's NO ENTRY
    stencil(g, "BAY 7", (3.4, BACK + 0.06, 3.15), 0.75, M["paint_yellow"])
    stencil(g, "DANGER  HIGH VOLTAGE", (-12.4, BACK + 0.05, 2.75), 0.11, M["paint_red"])
    stencil(g, "NO SMOKING", (8.2, BACK + 0.06, 3.0), 0.28, M["paint_red"])
    # the fan housing on the back wall (the blades turn in Unity)
    g.box((-12.6, BACK + 0.0, 5.4), (1.3, 0.3, 1.3), M["steel"])
    g.torus((-12.6, BACK - 0.16, 5.4), 0.55, 0.05, M["steel_mid"], rot=(90, 0, 0), n1=24, n2=6)
    return g.finish(coll)


def build_shutter(coll, M):
    g = Group("Shutter")
    y = BACK + 0.14
    z = 0.1
    while z < DOOR_H - 0.02:
        g.box(((DOOR_L + DOOR_R) / 2, y, z + 0.045), (DOOR_R - DOOR_L - 0.1, 0.04, 0.08), M["iron"] if rnd.random() > 0.15 else M["rust"])
        g.box(((DOOR_L + DOOR_R) / 2, y - 0.02, z + 0.088), (DOOR_R - DOOR_L - 0.1, 0.045, 0.012), M["iron_dark"])
        z += 0.1
    g.box(((DOOR_L + DOOR_R) / 2, y, 0.08), (DOOR_R - DOOR_L - 0.08, 0.07, 0.1), M["steel"])          # bottom bar
    g.box((DOOR_L + 0.6, y - 0.05, 1.0), (0.2, 0.05, 0.08), M["steel_mid"])                           # handle
    stencil(g, "NO ENTRY", ((DOOR_L + DOOR_R) / 2, y - 0.03, 2.1), 0.42, M["paint_red"])
    return g.finish(coll)


def build_fan(coll, M):
    g = Group("Fan")
    c = Vector((-12.6, BACK - 0.2, 5.4))
    g.cyl(c[:], 0.1, 0.08, M["steel"], rot=(90, 0, 0), n=12)
    for a in range(5):
        ang = a * 72
        R = Matrix.Rotation(math.radians(ang), 4, "Y")
        p = c + (R @ Vector((0, 0, 0.27)))
        g.box(p[:], (0.16, 0.02, 0.46), M["steel_mid"], rot=R @ Matrix.Rotation(math.radians(18), 4, "Z"))
    return g.finish(coll)


def build_shafts(coll):
    """Moonlight beams through the broken panes: long tapered quads (two crossed per window)
    with v running along the beam, for an additive material in Unity (not baked)."""
    me = bpy.data.meshes.new("Shafts")
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    for cx in WINDOWS:
        top = Vector((cx, BACK + 0.12, (WIN_Z0 + WIN_Z1) / 2))
        L = (top.z - 0.02) / -MOON_DIR.z
        end = top + MOON_DIR * L
        for side in (Vector((1, 0, 0)), Vector((0.0, 0.6, 0.8)).normalized()):
            w0, w1 = WIN_W * 0.42, WIN_W * 0.62
            a0, a1 = top - side * w0, top + side * w0
            b0, b1 = end - side * w1, end + side * w1
            vs = [bm.verts.new(p) for p in (a0, a1, b1, b0)]
            f = bm.faces.new(vs)
            for loop, (u, v) in zip(f.loops, ((0, 1), (1, 1), (1, 0), (0, 0))):
                loop[uv].uv = (u, v)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("Shafts", me)
    coll.objects.link(ob)
    return ob


# ---------------------------------------------------------------- the yard

def container(g, c, yaw, m, M):
    R = Euler((0, 0, math.radians(yaw))).to_matrix().to_4x4()
    T = Matrix.Translation(c) @ R
    def P(dx, dy, dz):
        return (T @ Vector((dx, dy, dz)))[:]
    L, W, H = 6.06, 2.44, 2.59
    g.box(P(0, 0, H / 2), (L, W, H), m, rot=R)
    for i in range(18):                                                                      # side corrugation
        dx = -L / 2 + 0.25 + i * (L - 0.5) / 17
        for s in (-1, 1):
            g.box(P(dx, s * (W / 2 + 0.02), H / 2), (0.12, 0.04, H - 0.25), m, rot=R)
    for sx in (-1, 1):                                                                       # corner posts / castings
        for sy in (-1, 1):
            g.box(P(sx * (L / 2 - 0.08), sy * (W / 2 - 0.08), H / 2), (0.18, 0.18, H + 0.02), M["steel"], rot=R)
    for dy in (-0.75, -0.3, 0.3, 0.75):                                                      # door lock bars
        g.box(P(L / 2 + 0.03, dy, H / 2), (0.04, 0.04, H - 0.3), M["steel_mid"], rot=R)
    g.box(P(L / 2 + 0.02, 0, H / 2), (0.02, 0.03, H - 0.2), M["steel"], rot=R)


def crane(g, x, y, M):
    """A rubber-tyred gantry crane straddling the stacks."""
    span, h, L = 23.0, 21.0, 9.0
    for sx in (-1, 1):
        for sy in (-1, 1):
            g.box((x + sx * span / 2, y + sy * L / 2, h / 2), (0.9, 0.9, h), M["crane"])
            g.box((x + sx * span / 2, y + sy * L / 2, 0.6), (1.6, 2.2, 1.2), M["steel"])
        g.box((x + sx * span / 2, y, 2.2), (0.6, L, 0.7), M["crane"])                        # sill beams
    for sy in (-1, 1):
        g.box((x, y + sy * L / 2, h + 0.8), (span + 1.6, 0.9, 1.6), M["crane"])              # girders
    g.box((x + 3.0, y, h + 1.9), (3.0, L - 1.0, 1.4), M["steel_mid"])                        # trolley
    g.box((x + 8.0, y - L / 2 - 0.9, h - 1.0), (2.2, 1.6, 1.8), M["crane"])                  # cab
    g.box((x + 8.0, y - L / 2 - 1.71, h - 0.9), (1.9, 0.02, 0.9), M["cab_window"])
    for sx in (-1, 1):
        g.cyl((x + sx * (span / 2 + 0.3), y - L / 2, h + 1.9), 0.18, 0.3, M["aviation"], n=10)
    for k in range(10):                                                                      # diagonal bracing
        a = k / 10
        for sx in (-1, 1):
            g.beam((x + sx * span / 2, y - L / 2, 3 + a * (h - 4)), (x + sx * span / 2, y + L / 2, 3 + (a + 0.1) * (h - 4)), 0.18, 0.18, M["crane"])


def car(g, c, yaw, m, M, squash=1.0):
    R = Euler((0, 0, math.radians(yaw))).to_matrix().to_4x4()
    T = Matrix.Translation(c) @ R @ Matrix.Diagonal((1, 1, squash, 1))
    tmp_g = Group("tmp")
    prof = [(-2.1, 0.25), (2.1, 0.25), (2.15, 0.7), (1.2, 0.8), (0.6, 1.3), (-0.9, 1.3), (-1.6, 0.85), (-2.15, 0.75)]
    tmp_g.prism(prof, -0.85, 0.85, m)
    for sx in (-1.35, 1.35):
        for sy in (-0.82, 0.82):
            tmp_g.cyl((sx, sy, 0.32), 0.32, 0.22, M["rubber"], rot=(90, 0, 0), n=12)
    tmp_g.box((0.85, 0, 1.06), (0.5, 1.5, 0.4), M["glass"], rot=(0, 30, 0))
    me = bpy.data.meshes.new("car")
    tmp_g.bm.transform(T)
    tmp_g.bm.to_mesh(me)
    tmp_g.bm.free()
    off = g._mi(m)
    remap = [g._mi(x) for x in tmp_g.mats]
    for p in me.polygons:
        p.material_index = remap[p.material_index]
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def build_yard(coll, M):
    g = Group("SetYard")
    # the shed's exterior: gutters, downpipes, a painted bay number
    g.box((0, BACK + 0.45, ROOF + 0.3), (HALF_W * 2 + 0.6, 0.25, 0.2), M["steel_mid"])
    for x in (-HALF_W + 0.4, HALF_W - 0.4):
        g.cyl((x, BACK + 0.45, ROOF / 2), 0.07, ROOF, M["steel_mid"], n=10)
    for i, (x0, ch) in enumerate(((-4.0, "B"), (-2.7, "A"), (-1.4, "Y"), (0.8, "7"))):
        g.box((x0, BACK + 0.32, 5.2), (0.95, 0.02, 1.4), M["paint_yellow"])
    # an exterior sodium lamp over the bay letters
    g.box((-1.5, BACK + 0.9, 6.4), (0.5, 0.6, 0.12), M["steel"])
    g.box((-1.5, BACK + 0.9, 6.32), (0.4, 0.5, 0.04), M["sodium"])
    # containers: a labyrinth of stacks behind the shed and off to the sides
    rows = [8.0, 14.5, 21.0, 27.5, 34.0, 40.5]
    for y in rows:
        x = -60.0
        while x < 60.0:
            if -18.0 < x < 12.0 and y < 36:            # a road through the stacks, leading the eye to the shed
                x += 6.6
                continue
            if 10.0 < x < 70.0 and y < 32:              # the main hall and its dock
                x += 6.6
                continue
            if rnd.random() > 0.25:
                stack = 1 + rnd.randrange(4)
                for k in range(stack):
                    container(g, Vector((x + rnd.uniform(-0.3, 0.3), y + rnd.uniform(-0.3, 0.3), k * 2.6)), rnd.uniform(-3, 3),
                              rnd.choice(M["container"]), M)
            x += 6.6
    for y in (-14.0, -21.0):
        x = -60.0
        while x < 60.0:
            if abs(x) > 22 and not (10.0 < x < 70.0 and y > -16) and rnd.random() > 0.3:
                for k in range(1 + rnd.randrange(3)):
                    container(g, Vector((x, y, k * 2.6)), rnd.uniform(-3, 3), rnd.choice(M["container"]), M)
            x += 6.6
    crane(g, -36.0, 21.0, M)
    crane(g, 34.0, 34.0, M)
    # crushed cars, stacked
    for (bx, by) in ((-20.0, 3.5), (-44.0, 8.0), (-30.0, -5.0)):
        for k in range(4):
            for j in range(2):
                car(g, Vector((bx + j * 4.6 + rnd.uniform(-0.4, 0.4), by + rnd.uniform(-0.5, 0.5), k * 0.85)), rnd.uniform(-8, 8),
                    rnd.choice(M["car"]), M, squash=0.55)
    # light poles
    for (lx, ly) in ((-14.0, 6.0), (8.0, 26.0), (-6.0, 24.0), (72.0, 6.0), (-40.0, 30.0), (4.0, -12.0), (-18.0, -10.0)):
        g.cyl((lx, ly, 5.0), 0.12, 10.0, M["steel_mid"], n=8)
        g.beam((lx, ly, 9.9), (lx + 1.2, ly, 10.1), 0.1, 0.1, M["steel_mid"])
        g.box((lx + 1.3, ly, 9.95), (0.7, 0.35, 0.18), M["steel"])
        g.box((lx + 1.3, ly, 9.85), (0.6, 0.3, 0.03), M["sodium"])
    # a chain-link fence in front of the shed, with barbed wire
    x = -60.0
    while x < 60.0:
        g.cyl((x, -28.0, 1.4), 0.04, 2.8, M["steel_mid"], n=6)
        x += 3.0
    for z in (0.3, 1.4, 2.6, 2.75, 2.9):
        g.box((0, -28.0, z), (120.0, 0.02, 0.02), M["steel_mid"])
    return g.finish(coll)


def build_ground(coll, M):
    g = Group("SetGround")
    g.box((0, 0, -0.08), (240, 240, 0.1), M["asphalt"])
    for i in range(26):
        x, y = rnd.uniform(-50, 50), rnd.uniform(-25, 45)
        if -22 < x < 22 and y < 7:                    # never in front of (or under) the shed: the fight's foreground
            continue
        g.cyl((x, y, 0.003), rnd.uniform(1.0, 3.5), 0.006, M["puddle"], n=20)
    for k in range(8):                                                                       # painted bay lines
        g.box((-56 + k * 15, 4.0, 0.004), (0.15, 50, 0.008), M["paint_yellow"])
    return g.finish(coll)


def build_city(coll, M):
    g = Group("SetCity")
    x = -260.0
    while x < 260.0:
        w = rnd.uniform(8, 26)
        h = rnd.choice([rnd.uniform(15, 40), rnd.uniform(30, 70), rnd.uniform(60, 120)])
        y = rnd.uniform(-240, -150)
        g.box((x, y, h / 2), (w, rnd.uniform(8, 20), h), M["city"])
        # window bands facing the yard: lit floors, some dark
        cols = max(2, int(w / 2.2))
        for fl in range(int(h / 3.2)):
            for cidx in range(cols):
                if rnd.random() < 0.82:
                    continue
                wx = x - w / 2 + (cidx + 0.5) * w / cols
                g.box((wx, y + 10.1, 2 + fl * 3.2), (w / cols * 0.55, 0.05, 1.1), M["city_win"] if rnd.random() < 0.75 else M["city_win_cold"])
        if h > 80:
            g.cyl((x, y, h + 3), 0.4, 6, M["steel"], n=6)
            g.cyl((x, y, h + 6.2), 0.6, 0.5, M["aviation"], n=8)
        if rnd.random() < 0.12:
            g.box((x, y + 10.2, h * 0.6), (w * 0.6, 0.05, 2.0), rnd.choice([M["neon_pink"], M["neon_cyan"]]))
        x += w + rnd.uniform(0, 6)
    return g.finish(coll)


# ---------------------------------------------------------------- lights and world

def lights():
    def add(kind, name, loc, energy, color, size=1.0, rot=None):
        ld = bpy.data.lights.new(name, kind)
        ld.energy = energy
        ld.color = color
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
    sun = add("SUN", "Moon", (0, 0, 50), 1.5, (0.50, 0.62, 1.0))
    sun.rotation_euler = MOON_DIR.to_track_quat("-Z", "Y").to_euler()
    sun.data.angle = math.radians(1.5)
    # storm light through the roof crack, straight down onto where he lies
    add("AREA", "CrackLight", ((CRACK_X0 + CRACK_X1) / 2, (CRACK_Y0 + CRACK_Y1) / 2, ROOF - 0.12), 260, (0.62, 0.72, 1.0), size=0.6)
    # moonlight under the shutter (from outside, low)
    add("AREA", "DoorGap", ((DOOR_L + DOOR_R) / 2, BACK + 0.9, 0.25), 220, (0.62, 0.72, 1.0), size=2.6,
        rot=(math.radians(-92), 0, 0))
    # the one lamp still burning, warm
    sod = add("SPOT", "Sodium", (-0.4, 0.4, 5.3), 320, (1.0, 0.55, 0.22), size=0.15)
    sod.data.spot_size = math.radians(80)
    sod.data.spot_blend = 0.6
    # a cold fill so the back wall reads behind the fighters
    add("AREA", "Fill", (0, -6.0, 4.0), 150, (0.30, 0.42, 0.88), size=14.0, rot=(math.radians(80), 0, 0))
    add("AREA", "FillRoof", (0, 0.8, 3.0), 30, (0.45, 0.55, 0.75), size=10.0, rot=(math.radians(180), 0, 0))
    # outside: the bay lamp and the pole lamps
    add("POINT", "BayLamp", (-1.5, BACK + 0.9, 6.1), 900, (1.0, 0.58, 0.22), size=0.3)
    for (lx, ly) in ((-14.0, 6.0), (8.0, 26.0), (-6.0, 24.0), (72.0, 6.0), (-40.0, 30.0), (4.0, -12.0), (-18.0, -10.0)):
        add("POINT", "Pole", (lx + 1.3, ly, 9.6), 6000, (1.0, 0.56, 0.2), size=0.4)
    H.hall_lights(add)
    w = bpy.data.worlds.new("Night")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.014, 0.028, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def lightning_lights():
    """A lightning strike: the sky goes white-blue, the moon becomes a blinding flash from
    above and behind, and the windows, the roof crack and the gap under the door blaze."""
    for ob in bpy.context.scene.objects:
        if ob.type == "LIGHT":
            if ob.name.startswith(("Sodium", "Pole", "BayLamp", "HallLamp", "DockFlood", "OfficeLight", "ExitYard")):
                ob.data.energy *= 0.5
            elif ob.name in ("SkylightTorn",):
                ob.data.energy *= 5.0
            elif ob.name.startswith("Hall"):
                ob.data.energy *= 2.4
                ob.data.color = (0.7, 0.8, 1.0)
            elif ob.name == "Moon":
                ob.data.energy = 9.0
                ob.data.color = (0.78, 0.86, 1.0)
            elif ob.name in ("CrackLight", "DoorGap"):
                ob.data.energy *= 6.0
            elif ob.name.startswith("Fill"):
                ob.data.energy *= 2.2
                ob.data.color = (0.7, 0.8, 1.0)
    for cx in WINDOWS:
        ld = bpy.data.lights.new("Strike", "AREA")
        ld.energy = 1600
        ld.color = (0.8, 0.88, 1.0)
        ld.size = WIN_W
        ob = bpy.data.objects.new("Strike", ld)
        ob.location = (cx, BACK + 1.2, (WIN_Z0 + WIN_Z1) / 2 + 0.6)
        ob.rotation_euler = (math.radians(-60), 0, 0)
        bpy.context.scene.collection.objects.link(ob)
    for cx in (17.0, 23.0, 29.0, 35.0, 41.0, 47.0, 53.0, 59.0):                          # the hall's high windows
        ld = bpy.data.lights.new("StrikeHall", "AREA")
        ld.energy = 2600
        ld.color = (0.8, 0.88, 1.0)
        ld.size = 3.2
        ob = bpy.data.objects.new("StrikeHall", ld)
        ob.location = (cx, H.HALL_B + 1.4, 7.4)
        ob.rotation_euler = (math.radians(-60), 0, 0)
        bpy.context.scene.collection.objects.link(ob)
    w = bpy.context.scene.world
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.22, 0.26, 0.40, 1)


# ---------------------------------------------------------------- preview shots (Unity camera -> Blender)

def U(x, y, z):
    return Vector((x, z, y))


def shot_camera(name, pos, look, fov, roll=0.0):
    cd = bpy.data.cameras.new(name)
    cd.sensor_fit = "VERTICAL"
    cd.angle = math.radians(fov)
    cd.clip_end = 600
    cam = bpy.data.objects.new(name, cd)
    bpy.context.scene.collection.objects.link(cam)
    p, l = U(*pos), U(*look)
    cam.location = p
    q = (l - p).to_track_quat("-Z", "Y")
    cam.rotation_euler = q.to_euler()
    if roll:
        cam.rotation_euler.rotate_axis("Z", math.radians(-roll))
    return cam


def stickman(pose_clip, frame, x, rot_z=0.0):
    """A posed stand-in hero (black), from the stickman pipeline."""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import build_stickman as S
    from mocap_clips import MOCAP_CLIPS
    arm = S.build_armature()
    S.build_mesh(arm)
    if "lie" not in bpy.data.actions:
        S.build_actions(arm, skip=tuple(MOCAP_CLIPS))
    black = mat("HeroInk", (0.01, 0.01, 0.01), 0.9)
    for o in bpy.data.objects:
        if o.type == "MESH" and o.parent == arm:
            o.data.materials.clear()
            o.data.materials.append(black)
    arm.animation_data_create()
    arm.animation_data.action = bpy.data.actions[pose_clip]
    arm.location = (x, 0, 0)
    arm.rotation_euler = (0, 0, math.radians(rot_z))
    return arm, frame


def preview(shots, name="story_set.png"):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 24
    sc.cycles.device = "CPU"
    try:
        sc.cycles.use_denoising = True
    except Exception:
        pass
    sc.render.resolution_x, sc.render.resolution_y = 768, 432
    sc.view_settings.view_transform = "Standard"
    from PIL import Image, ImageDraw, ImageFont
    tiles = []
    tmp = os.path.join(OUT, "_shot.png")
    for label, cam, arm_frame in shots:
        sc.camera = cam
        if arm_frame is not None:
            sc.frame_set(arm_frame)
        sc.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        tiles.append((label, Image.open(tmp).convert("RGB").copy()))
    os.remove(tmp)
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 26)
    cols = 2
    rows = (len(tiles) + 1) // 2
    sheet = Image.new("RGB", (768 * cols + 10, (432 + 40) * rows), (20, 20, 20))
    d = ImageDraw.Draw(sheet)
    for i, (label, img) in enumerate(tiles):
        x, y = (i % cols) * 778, (i // cols) * 472
        sheet.paste(img, (x, y + 36))
        d.text((x + 8, y + 4), label, fill=(255, 210, 0), font=font)
    sheet.save(os.path.join(PREVIEW, name))


# ---------------------------------------------------------------- bake and export

def unwrap(ob):
    """Lightmap UVs, once per object (every bake of it shares them)."""
    sc = bpy.context.scene
    for o in sc.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003, area_weight=0.0, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(margin=0.003)
    bpy.ops.object.mode_set(mode="OBJECT")


def bake(ob, size, samples, suffix=""):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = samples
    sc.cycles.device = "CPU"
    for o in sc.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    img = bpy.data.images.new("Story_" + ob.name + suffix, size, size, alpha=False)
    for m in ob.data.materials:
        nt = m.node_tree
        node = nt.nodes.get("BakeTarget") or nt.nodes.new("ShaderNodeTexImage")
        node.name = "BakeTarget"
        node.image = img
        for n in nt.nodes:
            n.select = False
        node.select = True
        nt.nodes.active = node
    rb = sc.render.bake
    rb.margin = 8
    rb.use_pass_direct = True
    rb.use_pass_indirect = True
    rb.use_pass_diffuse = True
    rb.use_pass_glossy = False
    rb.use_pass_transmission = True
    rb.use_pass_emit = True
    bpy.ops.object.bake(type="COMBINED")
    path = os.path.join(OUT, "Story_" + ob.name + suffix + ".png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    # soften the bake noise a touch
    from PIL import Image, ImageFilter
    Image.open(path).convert("RGB").filter(ImageFilter.GaussianBlur(0.8)).save(path)
    print("baked", ob.name, path)


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "StorySet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported StorySet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    coll = bpy.context.scene.collection
    M = materials()
    interior = build_shed(coll, M)
    shutter = build_shutter(coll, M)
    fan = build_fan(coll, M)
    shafts = build_shafts(coll)
    yard = build_yard(coll, M)
    ground = build_ground(coll, M)
    city = build_city(coll, M)
    hall = H.build_hall(sys.modules[__name__], coll, M)
    front = H.build_hall_front(sys.modules[__name__], coll, M)
    inner = H.build_inner_shutter(sys.modules[__name__], coll, M)
    lights()
    if preview_only:
        shafts.hide_render = True
        cams = []
        cams.append(("SHOT 0  the yard", shot_camera("S0", (-10, 18, 40), (-2, 3, -60), 40), None))
        arm, _ = stickman("lie", 0, HERO_X)
        cams.append(("SHOT 1  the roof fracture", shot_camera("S1", (HERO_X - 0.6, 0.55, 0), (HERO_X - 0.6, 7.2, 0.25), 24), 0))
        cams.append(("SHOT 5  isolation (rafters)", shot_camera("S5", (HERO_X + 3, 6.2, -2.4), (HERO_X, 0.5, 0.4), 52), 0))
        cams.append(("GAMEPLAY  lane, left", shot_camera("G1", (-6, 2.4, -10.5), (-6, 1.1, 0), 32), 0))
        cams.append(("GAMEPLAY  lane, centre", shot_camera("G2", (2, 2.4, -10.5), (2, 1.1, 0), 32), 0))
        cams.append(("THE DOOR  breach angle", shot_camera("B", (5.5, 1.35, -5.4), (10.8, 1.1, 1.6), 38), 0))
        if "--hall" in sys.argv:
            front.hide_render = True                    # the game hides it from outside
            cams = [
                ("HALL  zone 1 loading bay (gameplay)", shot_camera("H1", (20.0, 2.4, -10.5), (20.0, 1.6, 0), 34), None),
                ("HALL  zone 2 racking (gameplay)", shot_camera("H2", (32.0, 2.6, -10.5), (32.0, 2.0, 0), 34), None),
                ("HALL  zone 3 mezzanine (gameplay)", shot_camera("H3", (44.0, 2.8, -10.5), (44.0, 2.2, 0), 34), None),
                ("HALL  zone 4 crane bay (gameplay)", shot_camera("H4", (56.0, 2.6, -10.5), (56.0, 2.0, 0), 34), None),
                ("HALL  inside, wide, from the shutter", shot_camera("H5", (15.5, 3.2, 0.5), (40.0, 4.0, 10.0), 50), None),
                ("HALL  crane bay, low (the ending)", shot_camera("H6", (52.0, 0.6, -0.8), (58.0, 2.5, 6.0), 44), None),
                ("OUTSIDE  the yard face", shot_camera("H7", (30.0, 6.0, 52.0), (36.0, 5.0, 10.0), 40), None),
                ("OUTSIDE  the main door", shot_camera("H8", (72.0, 2.0, -6.0), (60.0, 3.0, 2.0), 40), None),
            ]
            preview(cams, "story_hall.png")
            return
        preview(cams)
        return
    for o in (interior, yard, ground, city, shutter, fan, hall, front, inner):
        o.select_set(False)
    for o in (interior, shutter, fan, yard, ground, city, hall, front, inner):
        unwrap(o)
    hall_only = "--hall-only" in sys.argv          # re-bake just the hall (the other bakes are kept; unwraps are deterministic)
    # the camera-side walls are cut away whenever the lane is seen, so they must not shade it:
    # bake everything as the old open set was lit, and only switch them on for their own bake
    front.hide_render = True
    if not hall_only:
        bake(interior, 2048, 48)
        bake(shutter, 512, 32)
        bake(fan, 256, 16)
        bake(yard, 2048, 24)
        bake(ground, 1024, 16)
        bake(city, 1024, 8)
    bake(hall, 4096, 24)
    front.hide_render = False
    bake(front, 2048, 16)
    front.hide_render = True
    bake(inner, 512, 24)
    # the same set in a lightning strike: everything blown cold white for a few frames
    lightning_lights()
    if not hall_only:
        bake(interior, 2048, 32, "_Lightning")
        bake(shutter, 512, 16, "_Lightning")
        bake(yard, 2048, 16, "_Lightning")
        bake(ground, 1024, 12, "_Lightning")
    bake(hall, 4096, 16, "_Lightning")
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export([interior, shutter, fan, shafts, yard, ground, city, hall, front, inner] + marks)


if __name__ == "__main__":
    main()
