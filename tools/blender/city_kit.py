"""City kit for chapter 2's sets (the bank, build_bank_set.py, and the street, build_street_set.py):
the architecture and the street furniture of a stone-and-brick city the morning after the storm,
modelled in real detail (cornices with dentils, windows with frames, mullions, sills and keystones,
fluted columns, lamp posts, benches, bins, bicycles, cars with their trim, fire escapes, awnings,
posters, graffiti, puddles), and its surfaces (cut stone, brick, veined marble, wet paving and
tarmac). Everything bakes, so the unlit game shows it at no runtime cost.

Every piece is written once in a local frame, `L`: u along the wall (x), the wall plane at v = 0
with its outside towards -v (y), w up (z). `L(g, origin, yaw)` puts that frame anywhere: yaw 0 is a
wall facing the camera (-y), yaw -90 a wall facing -x (readable from the west), yaw 90 facing +x,
yaw 180 facing +y. Materials stay in the game's black and white: every colour is a grey."""
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector, noise

import detail as D

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
rnd = random.Random(29)

# ---------------------------------------------------------------- fonts

_FONT_FILES = {
    "sign": [os.path.join(ROOT, "Assets", "Resources", "Fonts", "BebasNeue-Regular.ttf")],
    "body": [os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf")],
    "marker": [os.path.join(ROOT, "Assets", "Resources", "Fonts", "PermanentMarker-Regular.ttf")],
    "serif": ["/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf",
              "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf",
              os.path.join(ROOT, "Assets", "Resources", "Fonts", "BebasNeue-Regular.ttf")],
}
_FONTS = {}


def font(kind):
    if kind in _FONTS:
        return _FONTS[kind]
    f = None
    for p in _FONT_FILES.get(kind, []):
        if os.path.exists(p):
            f = bpy.data.fonts.load(p, check_existing=True)
            break
    _FONTS[kind] = f
    return f


def reset():
    _FONTS.clear()
    MATS.clear()


# ---------------------------------------------------------------- materials

MATS = {}


def _grey(v):
    return (v, v, v * 1.02, 1)


def _node_mat(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    return m, nt, nt.nodes["Principled BSDF"]


def _mix(nt, a, b, fac, blend="MULTIPLY"):
    mx = nt.nodes.new("ShaderNodeMix")
    mx.data_type = "RGBA"
    mx.blend_type = blend
    mx.inputs["Factor"].default_value = fac
    nt.links.new(a, mx.inputs["A"])
    nt.links.new(b, mx.inputs["B"])
    return mx.outputs["Result"]


def courses(name, g, joint, plane="xz", size=(0.9, 0.42), joint_w=0.012, var=0.72, grain=0.45, grime=0.45,
            rough=0.85, bump=0.45, offset=0.5):
    """Units laid in courses (cut stone, brick, paving): each unit its own grey, thin joints, grain and
    weathering over it all. Object space, so a merged group keeps its scale."""
    key = (name, plane)
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _node_mat(f"{name}_{plane}")
    co = D._coords(nt, plane, 1.0)
    br = nt.nodes.new("ShaderNodeTexBrick")
    br.offset = offset
    br.inputs["Color1"].default_value = _grey(g)
    br.inputs["Color2"].default_value = _grey(g * var)
    br.inputs["Mortar"].default_value = _grey(joint)
    br.inputs["Scale"].default_value = 1.0
    br.inputs["Mortar Size"].default_value = joint_w
    br.inputs["Mortar Smooth"].default_value = 0.2
    br.inputs["Bias"].default_value = 0.0
    br.inputs["Brick Width"].default_value = size[0]
    br.inputs["Row Height"].default_value = size[1]
    nt.links.new(co, br.inputs["Vector"])
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 14.0
    nz.inputs["Detail"].default_value = 8.0
    nt.links.new(co, nz.inputs["Vector"])
    gr = nt.nodes.new("ShaderNodeTexNoise")
    gr.inputs["Scale"].default_value = 0.45
    gr.inputs["Detail"].default_value = 4.0
    nt.links.new(co, gr.inputs["Vector"])
    c = _mix(nt, br.outputs["Color"], nz.outputs["Color"], grain)
    c = _mix(nt, c, gr.outputs["Color"], grime)
    nt.links.new(c, bsdf.inputs["Base Color"])
    bp = nt.nodes.new("ShaderNodeBump")
    bp.inputs["Strength"].default_value = bump
    nt.links.new(br.outputs["Fac"], bp.inputs["Height"])
    nt.links.new(bp.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = rough
    MATS[key] = m
    return m


def marble(name, g, vein, plane="xy", scale=0.9, rough=0.12, warp=9.0):
    """Polished marble: a pale (or dark) field crossed by thin wandering veins, a soft cloud in it."""
    key = (name, plane)
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _node_mat(f"{name}_{plane}")
    co = D._coords(nt, plane, scale)
    wv = nt.nodes.new("ShaderNodeTexWave")
    wv.inputs["Scale"].default_value = 1.1
    wv.inputs["Distortion"].default_value = warp
    wv.inputs["Detail"].default_value = 6.0
    wv.inputs["Detail Scale"].default_value = 1.5
    nt.links.new(co, wv.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    els = ramp.color_ramp.elements
    soft = vein * 0.55 + g * 0.45                       # veins: thin, and only part-way to their grey
    els[0].position, els[0].color = 0.0, _grey(soft)
    els[1].position, els[1].color = 0.035, _grey(g)
    e = els.new(0.965)
    e.color = _grey(g)
    e = els.new(1.0)
    e.color = _grey(soft * 0.5 + g * 0.5)
    nt.links.new(wv.outputs["Fac"], ramp.inputs["Fac"])
    cl = nt.nodes.new("ShaderNodeTexNoise")
    cl.inputs["Scale"].default_value = 1.2
    cl.inputs["Detail"].default_value = 5.0
    nt.links.new(co, cl.inputs["Vector"])
    c = _mix(nt, ramp.outputs["Color"], cl.outputs["Color"], 0.12, "OVERLAY")
    nt.links.new(c, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    MATS[key] = m
    return m


def tarmac(name, g=0.06, wet=0.7):
    """Wet tarmac: fine aggregate, darker patches where the water lies, a little sheen."""
    key = (name, "xy")
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _node_mat(name)
    co = D._coords(nt, "xy", 1.0)
    ag = nt.nodes.new("ShaderNodeTexNoise")
    ag.inputs["Scale"].default_value = 60.0
    ag.inputs["Detail"].default_value = 2.0
    nt.links.new(co, ag.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = _grey(g * 0.6)
    ramp.color_ramp.elements[1].color = _grey(g * 1.6)
    nt.links.new(ag.outputs["Fac"], ramp.inputs["Fac"])
    pa = nt.nodes.new("ShaderNodeTexNoise")
    pa.inputs["Scale"].default_value = 0.35
    pa.inputs["Detail"].default_value = 3.0
    nt.links.new(co, pa.inputs["Vector"])
    c = _mix(nt, ramp.outputs["Color"], pa.outputs["Color"], 0.55 * wet)
    nt.links.new(c, bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6 - 0.4 * wet
    MATS[key] = m
    return m


def grey(name, g, rough=0.7, metal=0.0, emit=0.0):
    """A flat grey (neutral white light if it glows)."""
    key = (name, "grey")
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _node_mat(name)
    bsdf.inputs["Base Color"].default_value = _grey(g)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = (0.95, 0.96, 1.0, 1)
        bsdf.inputs["Emission Strength"].default_value = emit
    MATS[key] = m
    return m


# ---------------------------------------------------------------- primitives (a world matrix each)

def _rot(r):
    if r is None:
        return Matrix.Identity(4)
    if isinstance(r, Matrix):
        return r.to_4x4()
    return Euler([math.radians(a) for a in r]).to_matrix().to_4x4()


AXIS = {"z": Matrix.Identity(4), "y": Matrix.Rotation(math.radians(90), 4, "X"), "x": Matrix.Rotation(math.radians(90), 4, "Y")}


def _put(g, tmp, m, W):
    bmesh.ops.transform(tmp, matrix=W, verts=tmp.verts)
    g._merge(tmp, m)


def cube(g, W, s, m, bevel=0.0):
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=1.0)
    bmesh.ops.scale(tmp, vec=Vector(s), verts=tmp.verts)
    if bevel > 0:
        r = min(bevel, 0.45 * min(s))
        if r > 0.002:
            bmesh.ops.bevel(tmp, geom=list(tmp.edges) + list(tmp.verts), offset=r, segments=1, affect="EDGES",
                            profile=0.5, clamp_overlap=True)
    _put(g, tmp, m, W)


def cone(g, W, r1, r2, h, m, n=12):
    tmp = bmesh.new()
    bmesh.ops.create_cone(tmp, cap_ends=True, segments=n, radius1=r1, radius2=r2, depth=h)
    _put(g, tmp, m, W)


def revolve(g, W, prof, m, n=16):
    """A solid of revolution about z from a profile [(r, z), ...] (bottom to top)."""
    tmp = bmesh.new()
    rings = []
    for r, z in prof:
        if r < 1e-5:
            rings.append([tmp.verts.new((0, 0, z))])
        else:
            rings.append([tmp.verts.new((r * math.cos(i / n * math.tau), r * math.sin(i / n * math.tau), z)) for i in range(n)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        for i in range(n):
            j = (i + 1) % n
            if len(a) == 1:
                tmp.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1:
                tmp.faces.new((a[i], a[j], b[0]))
            else:
                tmp.faces.new((a[i], a[j], b[j], b[i]))
    if len(rings[0]) > 1:
        tmp.faces.new(list(reversed(rings[0])))
    if len(rings[-1]) > 1:
        tmp.faces.new(rings[-1])
    _put(g, tmp, m, W)


def ring(g, W, R, r, m, n1=18, n2=6):
    tmp = bmesh.new()
    rs = []
    for i in range(n1):
        a = i / n1 * math.tau
        rs.append([tmp.verts.new(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a), r * math.sin(b)))
                   for b in (j / n2 * math.tau for j in range(n2))])
    for i in range(n1):
        a, b2 = rs[i], rs[(i + 1) % n1]
        for j in range(n2):
            tmp.faces.new((a[j], b2[j], b2[(j + 1) % n2], a[(j + 1) % n2]))
    _put(g, tmp, m, W)


def slab(g, W, pts, z0, z1, m):
    """A 2D outline in the xy plane, given thickness z0..z1 (then placed by W)."""
    tmp = bmesh.new()
    f0 = [tmp.verts.new((x, y, z0)) for x, y in pts]
    f1 = [tmp.verts.new((x, y, z1)) for x, y in pts]
    tmp.faces.new(list(reversed(f0)))
    tmp.faces.new(f1)
    n = len(pts)
    for i in range(n):
        tmp.faces.new((f0[i], f0[(i + 1) % n], f1[(i + 1) % n], f1[i]))
    _put(g, tmp, m, W)


def letters(g, W, text, size, m, depth=0.0, kind="sign", align="CENTER", spacing=1.0, res=2):
    """Text standing in the local x-z plane, readable from local -y (W places it)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = depth
    cu.resolution_u = res
    cu.align_x = align
    cu.align_y = "CENTER"
    cu.space_character = spacing
    f = font(kind)
    if f is not None:
        cu.font = f
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    me.transform(W @ Matrix.Rotation(math.radians(90), 4, "X"))
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def fluted_shaft(g, W, r0, r1, h, m, flutes=20, depth=0.07):
    """A column shaft with flutes (each a groove between two arrises), tapering r0 -> r1 (entasis)."""
    tmp = bmesh.new()
    rings = []
    for k, t in enumerate((0.0, 0.33, 1.0)):
        r = r0 + (r1 - r0) * (t ** 1.4)
        ring_ = []
        for i in range(flutes * 2):
            a = i / (flutes * 2) * math.tau
            rr = r if i % 2 == 0 else r * (1.0 - depth)
            ring_.append(tmp.verts.new((rr * math.cos(a), rr * math.sin(a), t * h)))
        rings.append(ring_)
    n = flutes * 2
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            tmp.faces.new((a[i], a[j], b[j], b[i]))
    tmp.faces.new(list(reversed(rings[0])))
    tmp.faces.new(rings[-1])
    _put(g, tmp, m, W)


def tube(g, W, loop, m, n=24):
    """Revolve a closed profile loop [(r, z), ...] about z (a ring with a real section: a frame, a rim)."""
    tmp = bmesh.new()
    rings = [[tmp.verts.new((r * math.cos(i / n * math.tau), r * math.sin(i / n * math.tau), z)) for i in range(n)] for r, z in loop]
    k = len(rings)
    for p in range(k):
        a, b = rings[p], rings[(p + 1) % k]
        for i in range(n):
            j = (i + 1) % n
            tmp.faces.new((a[i], a[j], b[j], b[i]))
    _put(g, tmp, m, W)


def finalize(ob):
    """Triangulate (concave outlines export safely) and report the count."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    bm.to_mesh(ob.data)
    bm.free()
    print(f"  {ob.name}: {len(ob.data.polygons)} triangles, {len(ob.data.materials)} materials")
    return len(ob.data.polygons)


# ---------------------------------------------------------------- the local frame

class L:
    """A local frame on a group: u = x, v = y (the outside is -v), w = z."""

    def __init__(self, g, origin=(0.0, 0.0, 0.0), yaw=0.0):
        self.g = g
        self.T = Matrix.Translation(Vector(origin)) @ Matrix.Rotation(math.radians(yaw), 4, "Z")

    def at(self, c, rot=None):
        return self.T @ Matrix.Translation(Vector(c)) @ _rot(rot)

    def box(self, c, s, m, rot=None, bevel=0.0):
        cube(self.g, self.at(c, rot), s, m, bevel)

    def cyl(self, c, r, h, m, axis="z", n=12, r2=None, rot=None):
        cone(self.g, self.at(c, rot) @ AXIS[axis], r, r if r2 is None else r2, h, m, n)

    def lathe(self, c, prof, m, n=16, axis="z", rot=None):
        revolve(self.g, self.at(c, rot) @ AXIS[axis], prof, m, n)

    def torus(self, c, R, r, m, axis="z", n1=18, n2=6, rot=None):
        ring(self.g, self.at(c, rot) @ AXIS[axis], R, r, m, n1, n2)

    def wallshape(self, pts_uw, v0, v1, m, c=(0, 0, 0)):
        """An outline drawn on the wall (u, w), v0..v1 deep."""
        slab(self.g, self.at(c) @ Matrix.Rotation(math.radians(90), 4, "X") @ Matrix.Scale(-1, 4, Vector((0, 0, 1))),
             pts_uw, v0, v1, m)

    def plan(self, pts_uv, w0, w1, m, c=(0, 0, 0)):
        slab(self.g, self.at(c), pts_uv, w0, w1, m)

    def beam(self, a, b, w, h, m):
        a, b = Vector(a), Vector(b)
        d = b - a
        if d.length < 1e-4:
            return
        z = d.normalized()
        up = Vector((0, 0, 1)) if abs(z.z) < 0.95 else Vector((1, 0, 0))
        x = up.cross(z).normalized()
        y = z.cross(x)
        R = Matrix((x, y, z)).transposed().to_4x4()
        cube(self.g, self.T @ Matrix.Translation((a + b) * 0.5) @ R, (w, h, d.length), m)

    def rod(self, a, b, r, m, n=6):
        a, b = Vector(a), Vector(b)
        d = b - a
        if d.length < 1e-4:
            return
        q = d.normalized().to_track_quat("Z", "Y").to_matrix().to_4x4()
        cone(self.g, self.T @ Matrix.Translation((a + b) * 0.5) @ q, r, r, d.length, m, n)

    def text(self, s, c, size, m, depth=0.0, kind="sign", align="CENTER", rot=0.0, spacing=1.0, res=2):
        letters(self.g, self.at(c) @ Matrix.Rotation(math.radians(rot), 4, "Y"), s, size, m, depth, kind, align, spacing, res)

    def fluted(self, c, r0, r1, h, m, flutes=20, depth=0.07):
        fluted_shaft(self.g, self.at(c), r0, r1, h, m, flutes, depth)

    def tube(self, c, loop, m, n=24, axis="z"):
        tube(self.g, self.at(c) @ AXIS[axis], loop, m, n)

    def sub(self, origin, yaw=0.0):
        s = L(self.g)
        s.T = self.T @ Matrix.Translation(Vector(origin)) @ Matrix.Rotation(math.radians(yaw), 4, "Z")
        return s


# ---------------------------------------------------------------- architecture (local frame: the wall at v = 0)

def wall(f, u0, u1, w0, w1, t, m, openings=(), v=0.0, arch_steps=12):
    """A wall u0..u1 by w0..w1, t thick about v, with openings [(u centre, width, w0, w1, arched)]
    cut right through it (an arched opening rises a half-circle above its w1)."""
    u = u0
    for (uc, wd, ow0, ow1, arch) in sorted(openings):
        a, b = uc - wd / 2, uc + wd / 2
        if a > u + 1e-4:
            f.box(((u + a) / 2, v, (w0 + w1) / 2), (a - u, t, w1 - w0), m)
        if ow0 > w0 + 1e-4:
            f.box((uc, v, (w0 + ow0) / 2), (wd, t, ow0 - w0), m)
        if arch:
            R = wd / 2
            pts = [(R, 0.0), (R, w1 - ow1), (-R, w1 - ow1), (-R, 0.0)]
            pts += [(R * math.cos(math.pi - math.pi * k / arch_steps), R * math.sin(math.pi - math.pi * k / arch_steps)) for k in range(1, arch_steps)]
            f.wallshape(pts, v - t / 2, v + t / 2, m, c=(uc, 0, ow1))
        elif ow1 < w1 - 1e-4:
            f.box((uc, v, (ow1 + w1) / 2), (wd, t, w1 - ow1), m)
        u = b
    if u < u1 - 1e-4:
        f.box(((u + u1) / 2, v, (w0 + w1) / 2), (u1 - u, t, w1 - w0), m)


def cornice(f, u0, u1, w, m, depth=0.45, dentil_m=None, modillions=False, returns=True):
    """A classical cornice along the wall at height w: bed moulding, a row of dentils, the corona,
    the cymatium on top (each a step further out)."""
    L_ = u1 - u0
    cu = (u0 + u1) / 2
    steps = [(0.0, 0.10, 0.18 * depth), (0.10, 0.16, 0.34 * depth), (0.30, 0.18, 0.86 * depth), (0.48, 0.07, depth), (0.55, 0.06, depth * 0.94)]
    for z0, h, d in steps:
        ext = d if returns else 0.0
        f.box((cu, -d / 2, w + z0 + h / 2), (L_ + 2 * ext, d, h), m)
    dm = dentil_m or m
    u = u0 + 0.08
    while u < u1 - 0.08:
        f.box((u, -0.42 * depth, w + 0.22), (0.07, 0.16 * depth, 0.11), dm)
        u += 0.15
    if modillions:
        u = u0 + 0.3
        while u < u1 - 0.2:
            f.box((u, -0.62 * depth, w + 0.36), (0.1, 0.4 * depth, 0.09), m)
            u += 0.6


def string_course(f, u0, u1, w, m, h=0.16, d=0.08):
    f.box(((u0 + u1) / 2, -d / 2, w), (u1 - u0, d, h), m)
    f.box(((u0 + u1) / 2, -d * 0.75, w + h * 0.35), (u1 - u0, d * 0.5, h * 0.3), m)


def pilaster(f, u, w0, w1, wd, m, d=0.12, cap_m=None):
    """A flat pilaster: a moulded base, the shaft (with two sunk flutes), a capital."""
    f.box((u, -d / 2, w0 + 0.12), (wd + 0.12, d + 0.06, 0.24), m, bevel=0.015)
    f.box((u, -d / 2, (w0 + w1) / 2), (wd, d, w1 - w0), m)
    for s in (-0.22, 0.22):
        f.box((u + s * wd, -d - 0.004, (w0 + w1) / 2), (wd * 0.12, 0.012, (w1 - w0) - 0.7), cap_m or m)
    f.box((u, -d / 2 - 0.02, w1 - 0.15), (wd + 0.14, d + 0.08, 0.12), cap_m or m, bevel=0.015)
    f.box((u, -d / 2 - 0.03, w1 - 0.04), (wd + 0.2, d + 0.1, 0.08), m)


def window(f, u, w0, w1, wd, M, frame, glass, sill_m, head="flat", cols=2, rows=3, blind=None, keystone=True,
           reveal=0.14, grille=None, box=None, curtain=None):
    """A window in the wall: a dark reveal, the glass, a frame with mullions and glazing bars, the
    sill (a drip below), a head (flat lintel, keystone, a pediment or a round arch)."""
    h = w1 - w0
    cw = (w0 + w1) / 2
    f.box((u, reveal * 0.6 + 0.07, cw), (wd + 0.02, 0.1, h + 0.02), M["reveal"])               # the dark room behind
    f.box((u, reveal * 0.6, cw), (wd - 0.04, 0.02, h - 0.04), glass)                            # glass
    if blind is not None:                                                                        # a blind half down
        f.box((u, reveal * 0.6 - 0.012, w1 - blind * h / 2 - 0.02), (wd - 0.1, 0.01, blind * h - 0.04), M["blind"])
        f.box((u, reveal * 0.6 - 0.02, w1 - blind * h - 0.02), (wd - 0.08, 0.02, 0.03), frame)
    if curtain is not None:
        for s in (-1, 1):
            f.box((u + s * (wd / 2 - curtain * wd / 2), reveal * 0.6 - 0.012, cw), (curtain * wd, 0.01, h - 0.08), M["blind"])
    fy = reveal * 0.5
    fw = 0.06
    for uu in (u - wd / 2 + fw / 2, u + wd / 2 - fw / 2):
        f.box((uu, fy, cw), (fw, 0.06, h), frame)
    for ww in (w0 + fw / 2, w1 - fw / 2):
        f.box((u, fy, ww), (wd, 0.06, fw), frame)
    for i in range(1, cols):
        f.box((u - wd / 2 + i * wd / cols, fy - 0.005, cw), (0.035, 0.05, h - 0.06), frame)
    for j in range(1, rows):
        f.box((u, fy - 0.005, w0 + j * h / rows), (wd - 0.06, 0.05, 0.03 if j != rows - 1 or rows < 3 else 0.06), frame)
    # sill and drip
    f.box((u, -0.06, w0 - 0.05), (wd + 0.3, 0.2 + reveal, 0.08), sill_m, bevel=0.012)
    f.box((u, -0.1, w0 - 0.11), (wd + 0.24, 0.1, 0.04), sill_m)
    for s in (-1, 1):
        f.box((u + s * (wd / 2 + 0.11), -0.02, w0 - 0.18), (0.1, 0.1, 0.12), sill_m)              # corbels
    # surround (architrave)
    for s in (-1, 1):
        f.box((u + s * (wd / 2 + 0.06), -0.03, cw), (0.12, 0.06, h + 0.12), sill_m)
    if head == "flat":
        f.box((u, -0.04, w1 + 0.12), (wd + 0.36, 0.08, 0.24), sill_m)
        if keystone:
            f.wallshape([(-0.13, 0.0), (0.13, 0.0), (0.17, 0.36), (-0.17, 0.36)], -0.1, 0.0, sill_m, c=(u, 0, w1 - 0.02))
    elif head == "pediment":
        f.box((u, -0.06, w1 + 0.1), (wd + 0.44, 0.12, 0.16), sill_m)
        f.wallshape([(-wd / 2 - 0.26, 0.0), (wd / 2 + 0.26, 0.0), (0.0, 0.42)], -0.13, 0.0, sill_m, c=(u, 0, w1 + 0.18))
    elif head == "arch":
        R = wd / 2
        n = 9
        for k in range(n):                                                                       # the voussoirs
            a0, a1 = math.pi * k / n, math.pi * (k + 1) / n
            am = (a0 + a1) / 2
            f.box((u + math.cos(am) * (R + 0.08), -0.03, w1 + math.sin(am) * (R + 0.08)), (0.16, 0.06 if k != n // 2 else 0.12, (a1 - a0) * (R + 0.08) * 0.95),
                  sill_m, rot=(0, -math.degrees(am) + 90, 0))
        pts = [(math.cos(math.pi * k / 16) * R, math.sin(math.pi * k / 16) * R) for k in range(17)]
        f.wallshape(pts, reveal * 0.55, reveal * 0.65, glass, c=(u, 0, w1))                     # the fanlight
        f.wallshape(pts, reveal * 0.9, reveal, M["reveal"], c=(u, 0, w1))
        for k in range(1, 4):                                                                    # its radiating bars
            a = math.pi * k / 4
            f.beam((u, fy, w1), (u + math.cos(a) * R, fy, w1 + math.sin(a) * R), 0.03, 0.04, frame)
        f.box((u, fy, w1 + 0.02), (wd, 0.05, 0.05), frame)
    if grille is not None:                                                                       # a bank's bars
        k = 0
        uu = u - wd / 2 + 0.12
        while uu < u + wd / 2 - 0.08:
            f.box((uu, -0.03, w0 + grille / 2), (0.025, 0.025, grille), M["iron"])
            uu += 0.14
            k += 1
        for ww in (w0 + 0.1, w0 + grille - 0.06):
            f.box((u, -0.035, ww), (wd, 0.02, 0.04), M["iron"])
    if box is not None:                                                                          # a flower box
        f.box((u, -0.18, w0 + 0.08), (wd * 0.9, 0.22, 0.18), box)
        for i in range(int(wd / 0.2)):
            f.cyl((u - wd * 0.4 + i * 0.2 + 0.1, -0.18, w0 + 0.22), 0.08, 0.12, M["leaf"], n=6, r2=0.03)


def drainpipe(f, u, w0, w1, m, v=-0.1):
    f.cyl((u, v, (w0 + w1) / 2), 0.05, w1 - w0, m, n=8)
    f.box((u, v, w1 + 0.12), (0.22, 0.18, 0.24), m)                                          # hopper head
    f.cyl((u, v - 0.08, w0 + 0.08), 0.055, 0.2, m, axis="y", n=8)                            # the shoe
    w = w0 + 0.9
    while w < w1:
        f.box((u, v + 0.05, w), (0.13, 0.06, 0.04), m)                                         # brackets
        w += 1.8


def ac_unit(f, u, w, M, v=-0.25):
    f.box((u, v, w), (0.8, 0.45, 0.55), M["ac"], bevel=0.02)
    f.cyl((u + 0.12, v - 0.23, w), 0.2, 0.02, M["ac_dark"], axis="y", n=14)
    for k in range(4):
        f.box((u + 0.12, v - 0.245, w - 0.15 + k * 0.1), (0.42, 0.012, 0.012), M["ac"])
    for k in range(5):
        f.box((u - 0.28, v - 0.23, w - 0.18 + k * 0.09), (0.16, 0.01, 0.03), M["ac_dark"])
    for s in (-0.3, 0.3):
        f.beam((u + s, 0.0, w - 0.45), (u + s, v - 0.18, w - 0.28), 0.03, 0.03, M["iron"])      # brackets
    f.rod((u - 0.3, v + 0.1, w - 0.25), (u - 0.32, v + 0.1, w - 1.4), 0.012, M["iron"], 4)      # the drip pipe


def roller_shutter(f, u0, u1, w0, w1, M, graffiti=None, padlock=True):
    cu = (u0 + u1) / 2
    f.box((cu, -0.18, w1 + 0.2), (u1 - u0 + 0.2, 0.36, 0.4), M["shutter_box"], bevel=0.02)
    for uu in (u0 - 0.04, u1 + 0.04):
        f.box((uu, -0.04, (w0 + w1) / 2), (0.08, 0.1, w1 - w0), M["iron"])
    w = w0
    while w < w1 - 0.02:
        f.box((cu, -0.04, w + 0.04), (u1 - u0, 0.03, 0.075), M["shutter"])
        f.box((cu, -0.058, w + 0.079), (u1 - u0, 0.012, 0.012), M["shutter_dark"])
        w += 0.085
    f.box((cu, -0.06, w0 + 0.05), (u1 - u0, 0.06, 0.1), M["iron"])
    if padlock:
        for s in (-1, 1):
            f.box((cu + s * (u1 - u0) * 0.3, -0.11, w0 + 0.12), (0.06, 0.04, 0.08), M["brass"])
    if graffiti:
        for i, (txt, sz, rot, du, dw) in enumerate(graffiti):
            f.text(txt, (cu + du, -0.075 - i * 0.002, (w0 + w1) / 2 + dw), sz, M["graffiti" if i % 2 == 0 else "graffiti_dark"], kind="marker", rot=rot)


def poster(f, u, w, wd, ht, M, lines=(), rot=0.0, torn=True, m=None):
    """A paper poster pasted to the wall: the sheet, a headline and lines of print, a torn corner."""
    m = m or M["paper"]
    f.box((u, -0.004, w), (wd, 0.006, ht), m, rot=(0, rot, 0))
    R = Matrix.Rotation(math.radians(-rot), 2)
    for i, (txt, sz, ink) in enumerate(lines):
        off = R @ Vector((0.0, ht * 0.32 - i * ht * 0.22))
        f.text(txt, (u + off.x, -0.009, w + off.y), sz, M[ink], kind="sign", rot=rot)
    if torn:
        f.box((u + wd * 0.38, -0.006, w - ht * 0.4), (wd * 0.28, 0.006, ht * 0.22), M["reveal"], rot=(0, rot + 35, 0))


# ---------------------------------------------------------------- street furniture (world frame, standing on w = 0)

def lamp_post(f, u, v, M, arm=-1.0, h=5.4, lit=False, double=False):
    """A cast-iron lamp post: a moulded foot, a fluted shaft, collars, a scrolled arm, the lantern."""
    m = M["iron"]
    f.lathe((u, v, 0), [(0.2, 0.0), (0.2, 0.12), (0.16, 0.18), (0.16, 0.5), (0.12, 0.62), (0.1, 0.9), (0.075, 1.0), (0.0, 1.0)], m, n=12)
    f.cyl((u, v, (1.0 + h) / 2), 0.07, h - 1.0, m, n=10, r2=0.055)
    for z in (1.6, h - 0.6):
        f.torus((u, v, z), 0.07, 0.02, m, n1=12, n2=4)
    arms = (arm, -arm) if double else (arm,)
    for a in arms:
        f.beam((u, v, h - 0.1), (u, v + a * 0.9, h + 0.05), 0.05, 0.05, m)
        f.torus((u, v + a * 0.35, h - 0.28), 0.17, 0.018, m, axis="x", n1=12, n2=4)                 # the scroll
        lu, lv, lw = u, v + a * 1.0, h - 0.15
        f.rod((lu, lv, lw + 0.2), (lu, lv, h + 0.05), 0.015, m, 4)
        f.lathe((lu, lv, lw - 0.42), [(0.0, 0.0), (0.06, 0.0), (0.11, 0.08), (0.18, 0.34), (0.2, 0.4), (0.0, 0.4)],
                M["lamp_glass_lit"] if lit else M["lamp_glass"], n=8)
        f.lathe((lu, lv, lw - 0.03), [(0.0, 0.0), (0.24, 0.0), (0.22, 0.04), (0.08, 0.18), (0.04, 0.26), (0.0, 0.3)], m, n=8)
        f.cyl((lu, lv, lw - 0.46), 0.05, 0.06, m, n=8)


def bollard(f, u, v, M, h=0.95):
    f.lathe((u, v, 0), [(0.13, 0.0), (0.13, 0.06), (0.1, 0.12), (0.09, h - 0.2), (0.11, h - 0.14), (0.11, h - 0.08), (0.07, h - 0.02), (0.0, h + 0.04)], M["iron"], n=10)
    f.torus((u, v, h * 0.62), 0.095, 0.012, M["white_band"], n1=10, n2=4)


def bench(f, u, v, yaw, M):
    s = f.sub((u, v, 0), yaw)
    for x in (-0.85, 0.85):
        s.box((x, 0.0, 0.22), (0.06, 0.5, 0.06), M["iron"])
        s.beam((x, -0.22, 0.0), (x, -0.2, 0.42), 0.05, 0.05, M["iron"])
        s.beam((x, 0.22, 0.0), (x, 0.2, 0.42), 0.05, 0.05, M["iron"])
        s.beam((x, 0.2, 0.42), (x, 0.3, 0.86), 0.05, 0.05, M["iron"])
        s.box((x, -0.26, 0.6), (0.05, 0.05, 0.05), M["iron"])
        s.beam((x, -0.25, 0.42), (x, -0.27, 0.62), 0.04, 0.04, M["iron"])
        s.beam((x, -0.27, 0.62), (x, 0.1, 0.62), 0.04, 0.04, M["iron"])
    for k in range(4):                                                                         # seat slats
        s.box((0, -0.18 + k * 0.12, 0.44), (1.9, 0.09, 0.035), M["slat"], bevel=0.008)
    for k in range(3):                                                                         # back slats
        s.box((0, 0.24 + k * 0.03, 0.56 + k * 0.11), (1.9, 0.035, 0.08), M["slat"], rot=(-15, 0, 0), bevel=0.008)


def litter_bin(f, u, v, M):
    f.lathe((u, v, 0), [(0.0, 0.0), (0.2, 0.0), (0.21, 0.08), (0.24, 0.8), (0.26, 0.86), (0.26, 0.92), (0.0, 0.92)], M["bin"], n=14)
    for z in (0.2, 0.55, 0.84):
        f.torus((u, v, z), 0.23 + z * 0.03, 0.012, M["white_band"], n1=14, n2=4)
    f.box((u, v - 0.2, 0.86), (0.22, 0.08, 0.06), M["reveal"])                                 # the mouth
    f.cyl((u, v, 0.5), 0.03, 1.0, M["iron"], n=6)


def bicycle(f, u, v, yaw, M, lean=12.0):
    s = f.sub((u, v, 0), yaw)
    m = M["iron"]
    r = 0.33
    for x in (-0.52, 0.52):
        s.torus((x, 0, r), r, 0.018, M["tyre"], axis="y", n1=18, n2=4, rot=(lean, 0, 0))
        s.torus((x, 0, r), r - 0.03, 0.006, m, axis="y", n1=14, n2=3, rot=(lean, 0, 0))
        for k in range(4):
            a = k / 4 * math.pi
            s.beam((x - math.cos(a) * (r - 0.03), 0, r - math.sin(a) * (r - 0.03)), (x + math.cos(a) * (r - 0.03), 0, r + math.sin(a) * (r - 0.03)), 0.006, 0.006, m)
    pts = {"bb": (-0.05, 0, 0.32), "seat": (-0.18, 0, 0.82), "head": (0.38, 0, 0.8), "rear": (-0.52, 0, r), "front": (0.52, 0, r)}
    for a, b in (("bb", "seat"), ("bb", "head"), ("seat", "head"), ("bb", "rear"), ("seat", "rear"), ("head", "front")):
        s.rod(pts[a], pts[b], 0.016, m, 5)
    s.box((-0.2, 0, 0.88), (0.22, 0.09, 0.05), M["tyre"], bevel=0.01)                       # saddle
    s.rod((0.38, 0, 0.8), (0.34, 0, 0.98), 0.014, m, 5)
    s.rod((0.34, -0.25, 0.98), (0.34, 0.25, 0.98), 0.014, m, 5)                             # handlebar
    s.box((0.0, 0, 0.32), (0.06, 0.04, 0.06), m)
    s.box((0.55, 0.0, 0.72), (0.24, 0.3, 0.16), M["basket"])                                  # a basket on some


def bike_rack(f, u0, u1, v, M):
    u = u0
    while u <= u1 + 1e-3:
        for a in range(7):
            t0, t1 = a / 7 * math.pi, (a + 1) / 7 * math.pi
            f.rod((u + 0.0, v - 0.35 * math.cos(t0), 0.45 * math.sin(t0) + (0 if a else 0)),
                  (u, v - 0.35 * math.cos(t1), 0.45 * math.sin(t1)), 0.02, M["iron"], 5)
        f.rod((u, v - 0.35, 0.0), (u, v - 0.35, 0.02), 0.02, M["iron"], 5)
        u += 0.75


def hydrant(f, u, v, M):
    f.lathe((u, v, 0), [(0.14, 0.0), (0.14, 0.05), (0.1, 0.08), (0.1, 0.55), (0.13, 0.6), (0.11, 0.68), (0.06, 0.78), (0.0, 0.8)], M["hydrant"], n=10)
    for s in (-1, 1):
        f.cyl((u + s * 0.12, v, 0.45), 0.045, 0.1, M["hydrant"], axis="x", n=8)


def drain(f, u, v, M, w=0.9, d=0.4, z=0.0):
    f.box((u, v, z - 0.004), (w, d, 0.012), M["reveal"])
    k = 0
    while k * 0.06 < w - 0.06:
        f.box((u - w / 2 + 0.06 + k * 0.06, v, z + 0.003), (0.025, d - 0.04, 0.008), M["iron"])
        k += 1
    for s in (-1, 1):
        f.box((u, v + s * (d / 2 - 0.02), z + 0.003), (w, 0.03, 0.008), M["iron"])


def manhole(f, u, v, M, r=0.32, z=0.0):
    f.cyl((u, v, z), r + 0.03, 0.01, M["iron"], n=24)
    f.torus((u, v, z + 0.006), r * 0.8, 0.012, M["iron_mid"], n1=20, n2=4)
    f.torus((u, v, z + 0.006), r * 0.45, 0.012, M["iron_mid"], n1=16, n2=4)
    for k in range(6):
        a = k / 6 * math.pi
        f.box((u, v, z + 0.007), (2 * r * 0.95 * abs(math.cos(a)) + 0.02, 2 * r * 0.95 * abs(math.sin(a)) + 0.02, 0.006), M["iron_mid"], rot=(0, 0, 0))


def puddle(f, u, v, r, m, z=0.0, seed=0.0, n=20, squash=1.0):
    pts = []
    for k in range(n):
        a = k / n * math.tau
        rr = r * (0.75 + 0.45 * noise.noise(Vector((math.cos(a) * 1.3 + seed, math.sin(a) * 1.3, seed * 0.7))))
        pts.append((u + math.cos(a) * rr, v + math.sin(a) * rr * squash))
    f.plan(pts, z - 0.002, z + 0.0015, m)


def newspaper_box(f, u, v, yaw, M, title="THE MORNING STAR", headline="STORM HITS CITY"):
    s = f.sub((u, v, 0), yaw)
    s.box((0, 0, 0.62), (0.52, 0.48, 0.9), M["news_box"], bevel=0.025)
    s.box((0, 0, 1.1), (0.56, 0.52, 0.06), M["iron"], bevel=0.01)
    s.box((0, -0.245, 0.75), (0.4, 0.01, 0.34), M["glass_shop"])
    s.box((0, -0.25, 0.75), (0.36, 0.008, 0.28), M["paper"])
    s.text(title, (0, -0.256, 0.86), 0.045, M["ink"], kind="serif")
    s.text(headline, (0, -0.256, 0.73), 0.06, M["ink"], kind="sign")
    s.box((0, -0.245, 0.98), (0.44, 0.01, 0.08), M["white_band"])
    s.text("NEWS", (0, -0.252, 0.98), 0.06, M["ink"], kind="sign")
    s.box((0.15, -0.25, 0.48), (0.05, 0.02, 0.08), M["brass"])                                # the coin slot
    for x in (-0.2, 0.2):
        s.box((x, 0, 0.09), (0.05, 0.4, 0.18), M["iron"])


def bin_dumpster(f, u, v, M, m):
    f.box((u, v, 0.7), (1.9, 1.1, 1.1), m, bevel=0.03)
    f.box((u, v, 1.29), (1.96, 1.16, 0.06), M["iron"], rot=(-5, 0, 0))
    for x in (-0.9, 0.9):
        f.box((u + x, v - 0.56, 0.75), (0.06, 0.04, 1.0), M["iron"])
    f.box((u, v - 0.57, 1.05), (1.8, 0.04, 0.06), M["iron"])
    for x in (-0.7, 0.7):
        for y in (-0.4, 0.4):
            f.cyl((u + x, v + y, 0.08), 0.08, 0.06, M["tyre"], axis="y", n=8)


def sedan(g, c, yaw, M, body, glass, open_door=False):
    """A big old black saloon: body with its waist line and wheel arches, the glasshouse with its
    pillars, chrome bumpers, grille and lamps, hubcaps, mirrors, handles, plates."""
    f = L(g, c, yaw)
    side = [(-2.38, 0.36), (-2.42, 0.62), (-2.34, 0.86), (-1.9, 0.92), (1.3, 0.94), (2.3, 0.86), (2.42, 0.7), (2.4, 0.36)]
    f.wallshape(side, -0.86, 0.86, body)
    # the wheel arches (dark recesses) and the sills
    for x in (-1.5, 1.5):
        for s in (-1, 1):
            arch = [(x - 0.45, 0.30), (x + 0.45, 0.30)] + [(x + math.cos(t) * 0.46, 0.34 + math.sin(t) * 0.42) for t in (0.4, 0.9, 1.4, 1.9, 2.4, 2.75)]
            f.wallshape([(px, pz) for px, pz in arch], s * 0.865 - 0.01, s * 0.865 + 0.01, M["reveal"])
            f.cyl((x, s * 0.78, 0.36), 0.36, 0.22, M["tyre"], axis="y", n=18)
            f.cyl((x, s * 0.9, 0.36), 0.2, 0.02, M["chrome"], axis="y", n=14)
            f.cyl((x, s * 0.91, 0.36), 0.08, 0.02, M["iron"], axis="y", n=10)
    cab = [(-1.7, 0.92), (1.05, 0.92), (0.45, 1.42), (-1.1, 1.44), (-1.55, 1.18)]
    f.wallshape(cab, -0.74, 0.74, body)
    # glass: windscreen, back light, side windows split by the pillars
    f.box((0.76, 0, 1.17), (0.03, 1.36, 0.6), glass, rot=(0, -40, 0))
    f.box((-1.4, 0, 1.17), (0.03, 1.32, 0.5), glass, rot=(0, 50, 0))
    for s in (-1, 1):
        f.box((-0.35, s * 0.745, 1.17), (1.7, 0.02, 0.36), glass)
        f.box((-0.33, s * 0.755, 1.17), (0.06, 0.02, 0.42), body)                             # the B pillar
        for x in (-0.95, 0.25):                                                               # door seams, handles
            f.box((x + 0.62, s * 0.866, 0.62), (0.012, 0.01, 0.55), M["reveal"])
            f.box((x + 0.42, s * 0.88, 0.8), (0.14, 0.03, 0.03), M["chrome"])
        f.box((0.7, s * 0.95, 1.0), (0.12, 0.1, 0.08), body)                                   # mirrors
        f.box((-0.1, s * 0.868, 0.92), (4.4, 0.012, 0.025), M["chrome"])                      # waist trim
    for x, d in ((2.46, 1), (-2.46, -1)):                                                     # bumpers
        f.box((x, 0, 0.42), (0.1, 1.8, 0.12), M["chrome"], bevel=0.03)
        f.box((x - d * 0.06, 0, 0.62), (0.04, 1.2, 0.14), M["reveal"])                          # plate recess
        f.box((x + d * 0.002, 0, 0.6), (0.02, 0.5, 0.11), M["plate"])
    f.box((2.41, 0, 0.66), (0.04, 0.6, 0.18), M["chrome"])                                   # grille
    for k in range(6):
        f.box((2.43, -0.25 + k * 0.1, 0.66), (0.02, 0.02, 0.16), M["reveal"])
    for s in (-1, 1):
        f.cyl((2.39, s * 0.62, 0.7), 0.1, 0.06, M["lamp_glass"], axis="x", n=12)               # headlamps
        f.box((-2.42, s * 0.62, 0.72), (0.04, 0.18, 0.12), M["tail"])
    f.box((0, 0, 1.445), (1.3, 1.2, 0.02), body)                                             # roof skin


def potted_plant(f, u, v, M, h=1.6, kind="palm", urn=True, n=9):
    """A plant in an urn: a palm (fronds), a fern bush, or a clipped ball."""
    if urn:
        f.lathe((u, v, 0), [(0.0, 0.0), (0.2, 0.0), (0.2, 0.06), (0.14, 0.1), (0.16, 0.2), (0.32, 0.5), (0.34, 0.62), (0.3, 0.64), (0.0, 0.6)], M["urn"], n=16)
        top = 0.6
    else:
        top = 0.0
    if kind == "palm":
        f.cyl((u, v, top + h * 0.3), 0.04, h * 0.6, M["leaf"], n=6, r2=0.025)
        for k in range(n):
            a = k / n * math.tau + rnd.uniform(-0.2, 0.2)
            L_ = rnd.uniform(0.6, 0.9) * h * 0.6
            p0 = Vector((u, v, top + h * 0.58))
            mid = p0 + Vector((math.cos(a) * L_ * 0.5, math.sin(a) * L_ * 0.5, L_ * 0.25))
            tip = p0 + Vector((math.cos(a) * L_, math.sin(a) * L_, -L_ * 0.1))
            for a_, b_ in ((p0, mid), (mid, tip)):
                f.beam(a_[:], b_[:], 0.16, 0.012, M["leaf"])
    elif kind == "ball":
        f.lathe((u, v, top), [(0.0, 0.0)] + [(math.sin(t) * h * 0.3, h * 0.3 - math.cos(t) * h * 0.3) for t in (0.5, 1.0, 1.57, 2.1, 2.6)] + [(0.0, h * 0.6)], M["leaf"], n=12)
    else:
        for k in range(n * 2):
            a = k / (n * 2) * math.tau
            L_ = rnd.uniform(0.35, 0.55) * h
            p0 = Vector((u, v, top + 0.05))
            tip = p0 + Vector((math.cos(a) * L_ * 0.7, math.sin(a) * L_ * 0.7, L_ * rnd.uniform(0.4, 0.9)))
            f.beam(p0[:], tip[:], 0.1, 0.012, M["leaf"])


def cctv(f, c, yaw, M, pitch=-25.0):
    s = f.sub(c, yaw)
    s.box((0, 0.04, 0), (0.12, 0.08, 0.16), M["iron"])                                       # wall plate
    s.beam((0, 0.0, 0.0), (0, -0.25, 0.05), 0.04, 0.04, M["iron"])
    s.box((0, -0.38, 0.06), (0.14, 0.34, 0.14), M["cctv"], rot=(pitch, 0, 0), bevel=0.02)
    s.box((0, -0.38, 0.15), (0.18, 0.4, 0.02), M["cctv"], rot=(pitch, 0, 0))                # the hood
    s.cyl((0, -0.56, 0.0), 0.04, 0.03, M["reveal"], axis="y", n=10, rot=(pitch, 0, 0))
    s.cyl((0.04, -0.22, 0.13), 0.008, 0.008, M["led"], n=6)


def clock_face(f, c, r, M, hour, minute, numerals=True, face_m=None, rim_m=None, hand_m=None):
    """A clock on the wall (local frame: its face looking out of the wall, -v). Hands at hour:minute."""
    u, v, w = c
    f.cyl((u, v + 0.02, w), r, 0.04, face_m or M["clock_face"], axis="y", n=40)
    f.torus((u, v - 0.01, w), r + 0.03, 0.04, rim_m or M["brass"], axis="y", n1=40, n2=6)
    f.torus((u, v - 0.0, w), r * 0.86, 0.006, M["ink"], axis="y", n1=32, n2=3)
    for k in range(60):
        a = k / 60 * math.tau
        big = k % 5 == 0
        f.box((u + math.sin(a) * r * 0.9, v - 0.004, w + math.cos(a) * r * 0.9), (0.012 * (2 if big else 1) * r, 0.006, r * (0.09 if big else 0.04)), M["ink"], rot=(0, math.degrees(a), 0))
    if numerals:
        romans = ["XII", "I", "II", "III", "IIII", "V", "VI", "VII", "VIII", "IX", "X", "XI"]
        for k in range(12):
            a = k / 12 * math.tau
            f.text(romans[k], (u + math.sin(a) * r * 0.72, v - 0.004, w + math.cos(a) * r * 0.72), r * 0.13, M["ink"], kind="serif", rot=-math.degrees(a) * 0)
    hm = hand_m or M["ink"]
    am = (minute / 60) * math.tau
    ah = ((hour % 12) + minute / 60) / 12 * math.tau
    f.box((u + math.sin(ah) * r * 0.25, v - 0.012, w + math.cos(ah) * r * 0.25), (0.05 * r, 0.008, r * 0.62), hm, rot=(0, math.degrees(ah), 0))
    f.box((u + math.sin(am) * r * 0.38, v - 0.02, w + math.cos(am) * r * 0.38), (0.03 * r, 0.008, r * 0.9), hm, rot=(0, math.degrees(am), 0))
    f.cyl((u, v - 0.025, w), r * 0.05, 0.02, hm, axis="y", n=10)


def pigeon(f, u, v, w, yaw, M):
    s = f.sub((u, v, w), yaw)
    s.lathe((0, 0, 0.0), [(0.0, -0.09), (0.045, -0.05), (0.055, 0.0), (0.04, 0.06), (0.0, 0.1)], M["pigeon"], n=8, axis="x")
    s.lathe((0.1, 0, 0.06), [(0.0, -0.025), (0.025, 0.0), (0.0, 0.025)], M["pigeon"], n=6)
    s.box((-0.12, 0, 0.0), (0.08, 0.05, 0.01), M["pigeon"], rot=(0, -15, 0))
    s.rod((0.0, 0.015, -0.05), (0.0, 0.015, -0.1), 0.004, M["iron"], 3)
    s.rod((0.0, -0.015, -0.05), (0.0, -0.015, -0.1), 0.004, M["iron"], 3)


def fruit_crate(f, c, M, fruit="round", n=(4, 3)):
    u, v, w = c
    f.box((u, v, w + 0.11), (0.6, 0.4, 0.02), M["slat"])
    for s in (-1, 1):
        f.box((u, v + s * 0.19, w + 0.13), (0.6, 0.02, 0.24), M["slat_dark"])
        for k in range(2):
            f.box((u + s * 0.29, v, w + 0.06 + k * 0.12), (0.02, 0.4, 0.08), M["slat"])
    for i in range(n[0]):
        for j in range(n[1]):
            x = u - 0.22 + i * 0.44 / max(1, n[0] - 1)
            y = v - 0.12 + j * 0.24 / max(1, n[1] - 1)
            if fruit == "round":
                f.lathe((x, y, w + 0.2), [(0.0, -0.05), (0.04, -0.035), (0.055, 0.0), (0.04, 0.035), (0.0, 0.05)], M["fruit" if (i + j) % 3 else "fruit_dark"], n=7)
            else:
                f.box((x, y, w + 0.2), (0.12, 0.05, 0.05), M["fruit"], rot=(0, 0, 30 * ((i + j) % 2)), bevel=0.02)


def tree(f, u, v, M, h=6.0):
    """A plane tree in a pavement grate: trunk, three limbs, clumps of leaves."""
    f.box((u, v, 0.004), (1.2, 1.2, 0.01), M["iron"])
    for k in range(6):
        f.box((u - 0.5 + k * 0.2, v, 0.01), (0.03, 1.1, 0.01), M["reveal"])
    f.cyl((u, v, h * 0.25), 0.14, h * 0.5, M["bark"], n=8, r2=0.1)
    for k in range(3):
        a = k / 3 * math.tau + 0.4
        tip = (u + math.cos(a) * 1.0, v + math.sin(a) * 1.0, h * 0.72)
        f.rod((u, v, h * 0.48), tip, 0.06, M["bark"], 6)
        f.lathe((tip[0], tip[1], tip[2] - 0.6), [(0.0, 0.0), (0.8, 0.3), (1.1, 0.8), (0.9, 1.3), (0.0, 1.6)], M["foliage"], n=9)
    f.lathe((u, v, h * 0.7), [(0.0, 0.0), (1.0, 0.4), (1.3, 1.0), (1.0, 1.6), (0.0, 2.0)], M["foliage"], n=10)


def mats(M):
    """The kit's materials, added to a set's dict (greys; glows are neutral white)."""
    M["reveal"] = grey("CityReveal", 0.02, 0.8)
    M["blind"] = grey("CityBlind", 0.62, 0.9)
    M["iron"] = grey("CityIron", 0.035, 0.45, metal=0.5)
    M["iron_mid"] = grey("CityIronMid", 0.09, 0.5, metal=0.5)
    M["white_band"] = grey("CityWhiteBand", 0.7, 0.6)
    M["brass"] = grey("CityBrass", 0.5, 0.3, metal=0.75)
    M["chrome"] = grey("CityChrome", 0.75, 0.15, metal=0.9)
    M["ink"] = grey("CityInk", 0.015, 0.7)
    M["paper"] = grey("CityPaper", 0.66, 0.9)
    M["paper_dark"] = grey("CityPaperDark", 0.28, 0.9)
    M["slat"] = D.planks("CitySlat", 0.2, "xz", board=0.09)
    M["slat_dark"] = grey("CitySlatDark", 0.1, 0.8)
    M["tyre"] = grey("CityTyre", 0.018, 0.85)
    M["bin"] = grey("CityBin", 0.06, 0.5, metal=0.3)
    M["hydrant"] = grey("CityHydrant", 0.25, 0.5, metal=0.2)
    M["ac"] = grey("CityAC", 0.46, 0.6, metal=0.2)
    M["ac_dark"] = grey("CityACDark", 0.04, 0.6)
    M["shutter"] = grey("CityShutter", 0.3, 0.45, metal=0.5)
    M["shutter_dark"] = grey("CityShutterDark", 0.1, 0.6)
    M["shutter_box"] = grey("CityShutterBox", 0.2, 0.5, metal=0.4)
    M["graffiti"] = grey("CityGraffiti", 0.75, 0.7)
    M["graffiti_dark"] = grey("CityGraffitiDark", 0.02, 0.7)
    M["leaf"] = grey("CityLeaf", 0.05, 0.85)
    M["foliage"] = grey("CityFoliage", 0.07, 0.9)
    M["bark"] = grey("CityBark", 0.06, 0.9)
    M["urn"] = courses("CityUrn", 0.4, 0.3, "xz", (2.0, 0.12), joint_w=0.004, grime=0.2)
    M["basket"] = grey("CityBasket", 0.14, 0.9)
    M["news_box"] = grey("CityNewsBox", 0.12, 0.4, metal=0.3)
    M["glass_shop"] = grey("CityShopGlass", 0.05, 0.05)
    M["lamp_glass"] = grey("CityLampGlass", 0.55, 0.1)
    M["lamp_glass_lit"] = grey("CityLampLit", 0.9, 0.2, emit=6.0)
    M["led"] = grey("CityLed", 0.9, 0.3, emit=8.0)
    M["cctv"] = grey("CityCctv", 0.6, 0.4)
    M["clock_face"] = grey("CityClockFace", 0.85, 0.5, emit=0.25)
    M["pigeon"] = grey("CityPigeon", 0.22, 0.9)
    M["plate"] = grey("CityPlate", 0.75, 0.6)
    M["tail"] = grey("CityTail", 0.2, 0.3)
    M["fruit"] = grey("CityFruit", 0.36, 0.5)
    M["fruit_dark"] = grey("CityFruitDark", 0.12, 0.5)
    M["cloth_a"] = grey("CityCanvasPale", 0.6, 0.95)
    M["cloth_b"] = grey("CityCanvasDark", 0.05, 0.95)
    return M


# ---------------------------------------------------------------- buildings (shared by the bank and the street)

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
            f.box((x + math.cos(a) * r * 1.08, y + math.sin(a) * r * 1.08, top + 0.17), (0.07, 0.07, 0.1), mc, rot=(0, 0, math.degrees(a)))
        for s in (-1, 1):                                                                    # the volutes, on both faces
            for side in (-1, 1):
                c = (x + s * r * 1.12, y + side * r * 0.9, top + 0.2)
                f.torus(c, r * 0.32, 0.045, mc, axis="y", n1=14, n2=4)
                f.torus(c, r * 0.17, 0.04, mc, axis="y", n1=10, n2=4)
            f.cyl((x + s * r * 1.12, y, top + 0.2), r * 0.3, r * 1.8, mc, axis="y", n=14)      # the bolster
        f.box((x, y, top + 0.53), (r * 2.9, r * 2.9, 0.18), mc, bevel=0.025)                  # the abacus
    else:
        f.lathe((x, y, top), [(r * 0.88, 0.0), (r * 1.2, 0.2), (r * 1.2, 0.26), (0.0, 0.26)], mc, n=24)
        f.box((x, y, top + 0.33), (r * 2.7, r * 2.7, 0.14), mc, bevel=0.02)


def shop_building(g, x0, x1, M, h, wall_m, name, kind, floors_m=None, face=8.2):
    """A building on the street's far side of the sidewalk (its face at y 8.2, facing the lane):
    the shopfront below (pilasters, fascia with its name, a display window over a stall riser, the
    door), upper floors of framed windows, string courses, a cornice, the roofline."""
    f = L(g, (0, face, 0), 0)
    cx, w = (x0 + x1) / 2, x1 - x0
    sf = 3.8
    dwx = x1 - 1.4                                                                                  # the door
    win0, win1 = x0 + 0.5, dwx - 0.6
    ncol = max(1, int(w / 2.4))
    ups, z = [], sf + 0.9
    while z + 1.8 < h - 0.8:
        ups += [(x0 + (c + 0.5) * w / ncol, 1.1, z, z + 1.6, False) for c in range(ncol)]
        z += 2.9
    wall(f, x0, x1, 0.0, h, 0.4, wall_m, [((win0 + win1) / 2, win1 - win0, 0.6, 3.1, False), (dwx, 1.0, 0.0, 3.2, False)] + ups, v=0.2)
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
        string_course(f, x0, x1, z - 0.55, M["trim"], h=0.14, d=0.08)
        for c in range(ncol):
            wx = x0 + (c + 0.5) * w / ncol
            blind = rnd.choice((None, None, 0.4, 0.7))
            window(f, wx, z, z + 1.6, 1.1, M, fl, M["glass_ext"], M["trim"], head="pediment" if lvl == 0 and kind == "cafe" else "flat",
                     cols=2, rows=2, blind=blind, keystone=(lvl % 2 == 0), box=(M["urn"] if (kind == "florist" and lvl == 0) else None))
        z += 2.9
        lvl += 1
    cornice(f, x0, x1, h - 0.5, M["trim"], depth=0.45)
    f.box((cx, 0.1, h + 0.35), (w, 0.3, 0.7), wall_m)
    for k in range(2):                                                                              # chimneys
        cxx = x0 + 1.0 + k * (w - 2.0)
        f.box((cxx, 1.2, h + 1.2), (0.7, 0.5, 2.4), M["brick"])
        for q in range(2):
            f.cyl((cxx - 0.15 + q * 0.3, 1.2, h + 2.55), 0.08, 0.3, M["iron_mid"], n=8)
    drainpipe(f, x1 - 0.15, 0.0, h, M["iron"])
    return f


def awning(f, x0, x1, z, depth, M, text, striped=True):
    """A shop awning: canvas sloping out from the wall in stripes, a scalloped valance with lettering,
    the iron arms."""
    n = int((x1 - x0) / 0.35)
    sw = (x1 - x0) / n
    ang = math.degrees(math.atan2(0.7, depth))
    for k in range(n):
        m = (M["cloth_a"] if k % 2 == 0 else M["cloth_b"]) if striped else M["cloth_b"]
        f.box((x0 + (k + 0.5) * sw, -depth / 2, z - 0.35), (sw, math.hypot(depth, 0.7), 0.025), m, rot=(-ang, 0, 0))
        f.wallshape([(math.cos(t * math.pi / 6) * sw * 0.5, -math.sin(t * math.pi / 6) * sw * 0.32) for t in range(7)], -depth - 0.02, -depth, m,
                    c=(x0 + (k + 0.5) * sw, 0, z - 0.95))                                        # the scallops
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
