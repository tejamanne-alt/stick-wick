"""Shared pieces for the chapter 5 (the road) and chapter 7 (the epilogue) sets: raw meshes with an
explicit facing, ribbons swept along a path, lofted solids, text on any plane, objects whose origin
is a pivot (wipers, the swing seat, a door, a drawer), a gradient sky, inward-facing sky bands, and
the saloon car (exterior and interior) that both chapters drive.

Built on build_story_set's Group (one bmesh per baked object) and its materials; every colour stays
in the game's black and white (TONE)."""
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector, noise

import build_story_set as S
import detail as D

rnd = random.Random(5)


# ---------------------------------------------------------------- raw geometry

def raw(g, verts, faces, m, hint=None):
    """Merge faces exactly as wound (no normal guessing). hint(face_centre, normal) -> Vector: when
    given, a face whose normal points away from the hint is turned round."""
    if hint is not None:
        out = []
        for f in faces:
            a, b, c = Vector(verts[f[0]]), Vector(verts[f[1]]), Vector(verts[f[2]])
            n = (b - a).cross(c - a)
            if n.length < 1e-12 and len(f) > 3:
                n = (c - a).cross(Vector(verts[f[3]]) - a)
            ctr = sum((Vector(verts[i]) for i in f), Vector()) / len(f)
            out.append(tuple(reversed(f)) if n.dot(hint(ctr, n)) < 0 else tuple(f))
        faces = out
    me = bpy.data.meshes.new("raw")
    me.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
    me.update()
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def sweep(g, frames, profile, m, closed=False, hint=None, caps=False):
    """Sweep a cross-section along a path. frames: [(pos, side, up)] (side = the path's left);
    profile: [(s, u)] offsets along side and up. A ribbon (open) or a tube (closed)."""
    verts, faces = [], []
    n = len(profile)
    for (p, side, up) in frames:
        for (s, u) in profile:
            verts.append(tuple(p + side * s + up * u))
    rows = len(frames)
    seg = n if closed else n - 1
    for i in range(rows - 1):
        for j in range(seg):
            a, b = i * n + j, i * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    if caps and closed:
        faces.append(tuple(range(n - 1, -1, -1)))
        faces.append(tuple((rows - 1) * n + j for j in range(n)))
    if hint is None and closed:
        def hint(c, nrm, _f=frames):
            # outward from the nearest frame's centre
            best = min(_f, key=lambda fr: (fr[0] - c).length_squared)
            return c - best[0]
    raw(g, verts, faces, m, hint)


def loft(g, stations, m, cap=True):
    """A closed solid through cross-sections [(x, [(y, z), ...])], all with the same point count,
    each section wound the same way. Used for car bodies (x along the car)."""
    verts, faces = [], []
    n = len(stations[0][1])
    for x, sec in stations:
        for (y, z) in sec:
            verts.append((x, y, z))
    for i in range(len(stations) - 1):
        for j in range(n):
            a, b = i * n + j, i * n + (j + 1) % n
            faces.append((a, b, b + n, a + n))
    if cap:
        faces.append(tuple(range(n)))
        faces.append(tuple((len(stations) - 1) * n + j for j in range(n)))
    cx = sum(v[0] for v in verts) / len(verts)
    cy = sum(v[1] for v in verts) / len(verts)
    cz = sum(v[2] for v in verts) / len(verts)

    def hint(c, nrm):
        # outward from the section's own centre (the body is long: use the axis, not one point)
        sx = min(stations, key=lambda st: abs(st[0] - c.x))
        sy = sum(p[0] for p in sx[1]) / n
        sz = sum(p[1] for p in sx[1]) / n
        d = Vector((0.0, c.y - sy, c.z - sz))
        if abs(nrm.normalized().x) > 0.8:                         # an end cap
            d = Vector((c.x - cx, 0, 0))
        return d
    raw(g, verts, faces, m, hint)


def ball(g, c, r, m, sub=1, squash=(1, 1, 1), jitter=0.0, seed=0.0):
    tmp = bmesh.new()
    bmesh.ops.create_icosphere(tmp, subdivisions=sub, radius=r)
    for v in tmp.verts:
        if jitter:
            v.co *= 1.0 + jitter * noise.noise(v.co * (1.7 / r) + Vector((seed, seed * 0.7, seed * 1.3)))
        v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
    bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector(c)), verts=tmp.verts)
    g._merge(tmp, m)


def text(g, body, M4, size, m, extrude=0.004, align="CENTER", font=None):
    """Text on any plane: M4 maps the text's own frame (x along the line, y up, z out of the face)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = body
    cu.size = size
    cu.extrude = extrude
    cu.align_x = align
    cu.align_y = "CENTER"
    if font:
        cu.font = font
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    me.transform(M4)
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def frame_m(origin, right, up):
    """A 4x4 placing a local frame: x = right, y = up, z = right x up (out of the face)."""
    r, u = Vector(right).normalized(), Vector(up).normalized()
    f = r.cross(u).normalized()
    M = Matrix((r, u, f)).transposed().to_4x4()
    M.translation = Vector(origin)
    return M


def finish_at(g, coll, pivot):
    """Finish a group with its object origin at `pivot` (it turns or slides about it in the game)."""
    ob = g.finish(coll)
    pv = Vector(pivot)
    ob.data.transform(Matrix.Translation(-pv))
    ob.location = pv
    return ob


class Local:
    """Wraps a Group so primitives are given in a local frame (a car's), placed by a matrix."""

    def __init__(self, g, M4):
        self.g, self.M = g, M4
        self.R = M4.to_3x3()
        e = M4.to_euler()
        self.rot_deg = Vector((math.degrees(e.x), math.degrees(e.y), math.degrees(e.z)))

    def p(self, c):
        return self.M @ Vector(c)

    def _rot(self, rot):
        R = Euler([math.radians(a) for a in rot]).to_matrix() if rot is not None else Matrix.Identity(3)
        return (self.R @ R).to_4x4()

    def box(self, c, s, m, rot=None):
        self.g.box(self.p(c), s, m, rot=self._rot(rot))

    def beam(self, a, b, w, h, m, up=(0, 0, 1)):
        self.g.beam(self.p(a), self.p(b), w, h, m, up=self.R @ Vector(up))

    def cyl(self, c, r, h, m, rot=None, n=16, r2=None):
        tmp = bmesh.new()
        bmesh.ops.create_cone(tmp, cap_ends=True, segments=n, radius1=r, radius2=r if r2 is None else r2, depth=h)
        M = Matrix.Translation(self.p(c)) @ self._rot(rot)
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self.g._merge(tmp, m)

    def torus(self, c, R, r, m, rot=None, n1=18, n2=8, arc=1.0):
        tmp = bmesh.new()
        rings = []
        k1 = n1 if arc >= 1.0 else n1 + 1
        for i in range(k1):
            a = i / n1 * math.tau * arc
            ring = []
            for j in range(n2):
                b = j / n2 * math.tau
                ring.append(tmp.verts.new(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a), r * math.sin(b))))
            rings.append(ring)
        for i in range(n1 if arc >= 1.0 else n1):
            a, b2 = rings[i], rings[(i + 1) % k1]
            for j in range(n2):
                tmp.faces.new((a[j], b2[j], b2[(j + 1) % n2], a[(j + 1) % n2]))
        if arc < 1.0:
            tmp.faces.new(list(reversed(rings[0])))
            tmp.faces.new(rings[-1])
        M = Matrix.Translation(self.p(c)) @ self._rot(rot)
        bmesh.ops.transform(tmp, matrix=M, verts=tmp.verts)
        self.g._merge(tmp, m)

    def ball(self, c, r, m, sub=1, squash=(1, 1, 1)):
        tmp = bmesh.new()
        bmesh.ops.create_icosphere(tmp, subdivisions=sub, radius=r)
        for v in tmp.verts:
            v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
        bmesh.ops.transform(tmp, matrix=self.M.to_3x3().to_4x4(), verts=tmp.verts)
        bmesh.ops.transform(tmp, matrix=Matrix.Translation(self.p(c)), verts=tmp.verts)
        self.g._merge(tmp, m)

    def loft(self, stations, m):
        tmp = S.Group("tmp")
        loft(tmp, stations, m)
        self.absorb(tmp)

    def absorb(self, tmp):
        """Bring a temp group (built in local coordinates) in, placed by the matrix."""
        tmp.bm.transform(self.M)
        me = bpy.data.meshes.new("tmp")
        tmp.bm.to_mesh(me)
        tmp.bm.free()
        remap = [self.g._mi(x) for x in tmp.mats]
        for p in me.polygons:
            p.material_index = remap[p.material_index]
        self.g.bm.from_mesh(me)
        bpy.data.meshes.remove(me)


def place(at, yaw=0.0, pitch=0.0):
    """A local frame at `at`, turned by yaw (degrees, about z) and pitched (about the local y)."""
    return Matrix.Translation(Vector(at)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(-pitch), 4, "Y")


# ---------------------------------------------------------------- materials

def grad_sky(name, z0, z1, stops, strength=1.0):
    """An emissive sky whose grey runs with height: stops = [(t, grey)] from z0 (t 0) to z1 (t 1)."""
    key = ("sky", name)
    if key in D.MATS:
        return D.MATS[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(n)
    out = nt.nodes["Material Output"]
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = z0
    mr.inputs["From Max"].default_value = z1
    nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = stops[0][0], (*[stops[0][1]] * 2, stops[0][1] * 0.98, 1)
    els[1].position, els[1].color = stops[-1][0], (*[stops[-1][1]] * 2, stops[-1][1] * 0.98, 1)
    for t, gv in stops[1:-1]:
        e = els.new(t)
        e.color = (gv, gv, gv * 0.98, 1)
    nt.links.new(mr.outputs["Result"], ramp.inputs["Fac"])
    # a soft cloud mottling over the gradient
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 0.012
    nz.inputs["Detail"].default_value = 5.0
    nt.links.new(tc.outputs["Object"], nz.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.35
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(nz.outputs["Color"], mix.inputs["B"])
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = strength
    nt.links.new(mix.outputs["Result"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs[0])
    D.MATS[key] = m
    return m


def mottled(name, g, rough=0.8, scale=1.5, var=0.5, metal=0.0):
    """A plain grey with a soft blotch of darker and lighter (grass, carpet, paint that has lived)."""
    key = (name, "mottled")
    if key in D.MATS:
        return D.MATS[key]
    m, nt, bsdf = D._base(name)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = scale
    nz.inputs["Detail"].default_value = 8.0
    nt.links.new(tc.outputs["Object"], nz.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[0].color = D._grey(g * (1 - var))
    ramp.color_ramp.elements[1].position = 0.7
    ramp.color_ramp.elements[1].color = D._grey(g * (1 + var * 0.6))
    nt.links.new(nz.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.3
    nt.links.new(nz.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    D.MATS[key] = m
    return m


def sky_band(g, centre, radius, z0, z1, m, n=72, cap=True):
    """An open cylinder facing inward (the camera is inside it), and a lid over it."""
    cx, cy = centre
    verts, faces = [], []
    for i in range(n):
        a = i / n * math.tau
        verts.append((cx + math.cos(a) * radius, cy + math.sin(a) * radius, z0))
        verts.append((cx + math.cos(a) * radius, cy + math.sin(a) * radius, z1))
    for i in range(n):
        a0, a1 = 2 * i, 2 * ((i + 1) % n)
        faces.append((a0, a0 + 1, a1 + 1, a1))
    raw(g, verts, faces, m, hint=lambda c, nrm: Vector((cx - c.x, cy - c.y, 0)))
    if cap:
        lid = [(cx + math.cos(i / n * math.tau) * radius, cy + math.sin(i / n * math.tau) * radius, z1) for i in range(n)]
        raw(g, lid, [tuple(range(n))], m, hint=lambda c, nrm: Vector((0, 0, -1)))


def sheet(g, x0, x1, y0, y1, height, m, res=1.0):
    """A ground surface facing up: a grid whose height is height(x, y)."""
    nx, ny = max(1, int(round((x1 - x0) / res))), max(1, int(round((y1 - y0) / res)))
    verts, faces = [], []
    for j in range(ny + 1):
        for i in range(nx + 1):
            x = x0 + (x1 - x0) * i / nx
            y = y0 + (y1 - y0) * j / ny
            verts.append((x, y, height(x, y)))
    for j in range(ny):
        for i in range(nx):
            a = j * (nx + 1) + i
            faces.append((a, a + 1, a + nx + 2, a + nx + 1))
    raw(g, verts, faces, m, hint=lambda c, nrm: Vector((0, 0, 1)))


def add_light(kind, name, loc, energy, size=1.0, rot=None, color=None, spot=None):
    ld = bpy.data.lights.new(name, kind)
    ld.energy = energy
    ld.color = color or S.NEUTRAL
    if kind == "AREA":
        ld.size = size
    if kind in ("POINT", "SPOT"):
        ld.shadow_soft_size = size
    if kind == "SPOT" and spot:
        ld.spot_size = math.radians(spot)
        ld.spot_blend = 0.5
    ob = bpy.data.objects.new(name, ld)
    ob.location = loc
    if rot is not None:
        ob.rotation_euler = rot
    bpy.context.scene.collection.objects.link(ob)
    return ob


def aim(ob, target):
    """Point a light (or camera) at a point."""
    d = Vector(target) - ob.location
    ob.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    return ob


def marks(coll):
    out = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        out.append(e)
    return out


def export(objs, path):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported", path)


def faces(ob):
    return len(ob.data.polygons)


# ---------------------------------------------------------------- the saloon

# Car frame: x forward, y to the car's left, z up; the origin on the ground under the middle.
CAR_LEN, CAR_HALF_W = 4.72, 0.89
WHEEL_X, WHEEL_Y, WHEEL_R = 1.38, 0.775, 0.32
BELT = 0.95                                   # the waist line (the bottom of the side windows)
SCREEN_BASE = (0.76, 0.95)                    # windscreen base (x, z) and top
SCREEN_TOP = (-0.06, 1.385)
ROOF_Z = 1.42
REAR_TOP, REAR_BASE = (-1.10, 1.385), (-1.76, 0.985)
DRIVER_Y = -0.37                              # right-hand drive: the driver sits on the right (-y)


def _top(x):
    """The body's upper line (bonnet, waist, boot) along the car."""
    pts = [(-2.36, 0.80), (-2.30, 0.92), (-2.0, 0.975), (-1.76, 0.985), (0.76, 0.95), (1.2, 0.90), (2.05, 0.84), (2.30, 0.78), (2.36, 0.66)]
    for (xa, za), (xb, zb) in zip(pts, pts[1:]):
        if xa <= x <= xb:
            t = (x - xa) / (xb - xa)
            return za + (zb - za) * t
    return pts[0][1] if x < pts[0][0] else pts[-1][1]


def _half_w(x):
    t = abs(x) / (CAR_LEN / 2)
    return CAR_HALF_W * (1.0 - 0.10 * max(0.0, t - 0.6) ** 2 / 0.16) - (0.03 if x > 2.25 else 0.0)


def _bottom(x):
    return 0.30 if abs(x) < 1.9 else 0.30 + (abs(x) - 1.9) * 0.25


def car_body(L, M, body):
    """The lower body as a loft: shoulders, a little tumble under to the sills."""
    xs = [-2.36, -2.33, -2.25, -2.1, -1.9, -1.6, -1.2, -0.6, 0.0, 0.6, 1.0, 1.4, 1.8, 2.1, 2.27, 2.34, 2.37]
    st = []
    for x in xs:
        w, zt, zb = _half_w(x), _top(x), _bottom(x)
        sh = zt - 0.06
        sec = [(-w * 0.62, zt), (-w * 0.92, zt - 0.015), (-w, sh - 0.03), (-w * 1.0, (sh + zb) * 0.5 + 0.08),
               (-w * 0.985, zb + 0.10), (-w * 0.94, zb), (w * 0.94, zb), (w * 0.985, zb + 0.10),
               (w * 1.0, (sh + zb) * 0.5 + 0.08), (w, sh - 0.03), (w * 0.92, zt - 0.015), (w * 0.62, zt)]
        st.append((x, sec))
    L.loft(st, body)


def car_cabin(L, M, body, glass):
    """The glasshouse: tumblehome sides, the windscreen and the rear window raked, as glass; then the
    pillars, the roof skin and the frames over it in the body colour and chrome."""
    xs = [REAR_BASE[0], -1.6, -1.4, REAR_TOP[0], -0.8, -0.5, -0.2, SCREEN_TOP[0], 0.2, 0.5, SCREEN_BASE[0]]

    def roof(x):
        if x <= REAR_TOP[0]:
            t = (x - REAR_BASE[0]) / (REAR_TOP[0] - REAR_BASE[0])
            return REAR_BASE[1] + (REAR_TOP[1] - REAR_BASE[1]) * math.sin(t * math.pi / 2)
        if x >= SCREEN_TOP[0]:
            t = (x - SCREEN_TOP[0]) / (SCREEN_BASE[0] - SCREEN_TOP[0])
            return SCREEN_TOP[1] + (SCREEN_BASE[1] - SCREEN_TOP[1]) * (t ** 1.15)
        return ROOF_Z - 0.035 * (abs(x - (REAR_TOP[0] + SCREEN_TOP[0]) / 2) / 0.52) ** 2
    st = []
    for x in xs:
        zr = max(roof(x), BELT + 0.012)
        wb, wr = _half_w(x) - 0.06, _half_w(x) - 0.20
        st.append((x, [(-wb, BELT), (wb, BELT), (wr, zr), (-wr, zr)]))
    L.loft(st, glass)
    # the roof skin
    rs = []
    for x in [REAR_TOP[0] + 0.02, -0.8, -0.5, -0.2, SCREEN_TOP[0] - 0.02]:
        zr = roof(x)
        wr = _half_w(x) - 0.20
        rs.append((x, [(-wr - 0.01, zr - 0.01), (wr + 0.01, zr - 0.01), (wr - 0.06, zr + 0.012), (-wr + 0.06, zr + 0.012)]))
    L.loft(rs, body)
    for s in (-1, 1):
        wb, wr = _half_w(0.7) - 0.06, _half_w(0) - 0.20
        # A pillars, B pillars, C pillars (the saloon's broad sail panels)
        L.beam((SCREEN_BASE[0] - 0.02, s * (wb - 0.01), BELT + 0.01), (SCREEN_TOP[0] + 0.02, s * (wr + 0.005), SCREEN_TOP[1] - 0.005), 0.07, 0.06, body)
        L.beam((-0.40, s * (wb - 0.02), BELT), (-0.44, s * (wr + 0.01), ROOF_Z - 0.02), 0.09, 0.04, body)
        for k in range(4):
            t = k / 3
            x = REAR_TOP[0] + (REAR_BASE[0] - REAR_TOP[0]) * t
            L.beam((x + 0.36, s * (wb - 0.01 - 0.0 * t), BELT + 0.005), (REAR_TOP[0] + 0.08 - 0.05 * t, s * (wr + 0.01), REAR_TOP[1] - 0.02 - 0.05 * t), 0.08, 0.035, body)
        # the drip rails and the window frames (chrome)
        L.beam((SCREEN_TOP[0], s * (wr + 0.012), ROOF_Z - 0.03), (REAR_TOP[0], s * (wr + 0.012), REAR_TOP[1] - 0.005), 0.012, 0.016, M["chrome"])
        L.beam((SCREEN_BASE[0] - 0.05, s * (wb + 0.003), BELT + 0.005), (REAR_BASE[0] + 0.25, s * (wb + 0.003), BELT + 0.005), 0.012, 0.014, M["chrome"])
    # windscreen and rear-window rubbers
    for (a, b) in ((SCREEN_BASE, SCREEN_TOP), (REAR_BASE, REAR_TOP)):
        for zz, xx in ((a[1] + 0.012, a[0]), (b[1] - 0.012, b[0])):
            w = _half_w(xx) - (0.08 if zz < 1.1 else 0.21)
            L.beam((xx, -w, zz), (xx, w, zz), 0.022, 0.02, M["rubber"])


def car_wheel(L, M, x, y, tyre, rim):
    s = 1 if y > 0 else -1
    L.cyl((x, y, WHEEL_R), WHEEL_R, 0.205, tyre, rot=(90, 0, 0), n=22)
    L.cyl((x, y + s * 0.095, WHEEL_R), WHEEL_R * 0.84, 0.02, tyre, rot=(90, 0, 0), n=22)       # sidewall bulge
    L.cyl((x, y + s * 0.104, WHEEL_R), 0.19, 0.016, rim, rot=(90, 0, 0), n=20)               # the wheel trim
    L.cyl((x, y + s * 0.112, WHEEL_R), 0.065, 0.02, M["chrome"], rot=(90, 0, 0), n=12)        # centre cap
    for k in range(8):                                                                        # trim slots
        a = k / 8 * math.tau
        L.box((x + math.cos(a) * 0.135, y + s * 0.113, WHEEL_R + math.sin(a) * 0.135), (0.05, 0.006, 0.022), M["rubber"], rot=(0, -math.degrees(a), 0))
    # the arch: a dark well behind the tyre, a lip round it
    L.cyl((x, s * (CAR_HALF_W - 0.02), WHEEL_R + 0.02), WHEEL_R + 0.06, 0.05, M["rubber"], rot=(90, 0, 0), n=22)
    L.torus((x, s * (CAR_HALF_W + 0.004), WHEEL_R + 0.02), WHEEL_R + 0.065, 0.012, M["rubber"], rot=(90, 0, 0), n1=22, n2=4, arc=0.5)


def saloon(g, M, at, yaw, body, lamp, tail=None, glass=None, extras=None):
    """The saloon (the Handler's, the Driver's, the police's): body, glasshouse, wheels, lamps,
    bumpers, grille, handles, mirrors, plates. Returns the Local frame (for extras)."""
    L = Local(g, place(at, yaw))
    glass = glass or M["car_glass"]
    tail = tail or M["tail_lamp"]
    car_body(L, M, body)
    car_cabin(L, M, body, glass)
    for x in (-WHEEL_X, WHEEL_X):
        for y in (-WHEEL_Y, WHEEL_Y):
            car_wheel(L, M, x, y, M["tyre"], M["wheel_trim"])
    fx, rx = 2.36, -2.36
    # front: the grille (chrome surround, slats), the headlamps (glass over a bright lamp), indicators
    L.box((fx + 0.005, 0, 0.62), (0.03, 0.66, 0.16), M["chrome"])
    for k in range(5):
        L.box((fx + 0.02, 0, 0.565 + k * 0.028), (0.012, 0.62, 0.008), M["grille"])
    for s in (-1, 1):
        L.box((fx - 0.005, s * 0.60, 0.645), (0.04, 0.34, 0.14), M["chrome"])
        L.box((fx + 0.014, s * 0.60, 0.645), (0.012, 0.30, 0.11), lamp)
        L.cyl((fx + 0.02, s * 0.53, 0.645), 0.045, 0.01, M["chrome"], rot=(0, 90, 0), n=12)    # the main beam's reflector
        L.box((fx - 0.02, s * 0.80, 0.645), (0.06, 0.08, 0.1), M["indicator"])
        L.box((fx + 0.01, s * 0.55, 0.37), (0.03, 0.12, 0.05), M["indicator"])                 # fog lamps in the apron
    # bumpers: chrome blades on black rubber, the plates
    for x0, sgn in ((fx, 1), (rx, -1)):
        L.box((x0 + sgn * 0.035, 0, 0.44), (0.08, 1.72, 0.12), M["rubber"])
        L.box((x0 + sgn * 0.075, 0, 0.47), (0.02, 1.70, 0.035), M["chrome"])
        for s in (-1, 1):
            L.box((x0 - sgn * 0.12, s * 0.86, 0.44), (0.3, 0.06, 0.12), M["rubber"], rot=(0, 0, s * sgn * 18))
        L.box((x0 + sgn * 0.08, 0, 0.36 if sgn > 0 else 0.62), (0.012, 0.52, 0.12), M["plate"])
    # rear: the tail lamp clusters, the boot lid's shut line and its chrome handle, the badge
    for s in (-1, 1):
        L.box((rx - 0.005, s * 0.64, 0.80), (0.04, 0.36, 0.15), M["chrome"])
        L.box((rx - 0.02, s * 0.64, 0.80), (0.012, 0.32, 0.12), tail)
        L.box((rx - 0.024, s * 0.52, 0.80), (0.006, 0.06, 0.12), M["indicator"])
    L.box((-2.05, 0, _top(-2.05) + 0.004), (0.01, 1.30, 0.004), M["shut"])
    L.box((rx - 0.012, 0, 0.73), (0.02, 0.30, 0.03), M["chrome"])
    L.box((-1.78, 0, _top(-1.78) + 0.006), (0.02, 1.36, 0.004), M["shut"])
    # the bonnet: shut line, a centre crease, the scuttle vents, the badge
    L.box((0.80, 0, _top(0.8) + 0.004), (0.01, 1.42, 0.004), M["shut"])
    L.box((1.55, 0, _top(1.55) + 0.003), (1.5, 0.006, 0.004), M["shut"], rot=(0, -4, 0))
    for k in range(9):
        L.box((0.83, -0.4 + k * 0.1, _top(0.83) + 0.006), (0.04, 0.06, 0.004), M["grille"])
    L.cyl((fx + 0.03, 0, 0.72), 0.03, 0.012, M["chrome"], rot=(0, 90, 0), n=12)
    # sides: door shut lines, handles, the rubbing strip, mirrors, the fuel flap, the aerial
    for s in (-1, 1):
        y = s * (CAR_HALF_W + 0.002)
        for xd in (0.78, -0.40, -1.38):
            L.box((xd, y, 0.62), (0.006, 0.004, 0.62), M["shut"])
        L.box((-0.30, y, 0.33), (2.15, 0.004, 0.006), M["shut"])
        for xh in (0.10, -0.95):
            L.box((xh, y + s * 0.012, 0.86), (0.13, 0.02, 0.025), M["chrome"])
        L.box((-0.30, y + s * 0.006, 0.60), (3.7, 0.012, 0.03), M["chrome"])
        L.beam((0.62, s * 0.84, BELT + 0.04), (0.56, s * 1.02, BELT + 0.07), 0.04, 0.05, body)
        L.box((0.56, s * 1.03, BELT + 0.09), (0.09, 0.16, 0.11), body)
        L.box((0.515, s * 1.03, BELT + 0.09), (0.006, 0.13, 0.085), M["mirror"])
        if s > 0:
            L.cyl((-1.95, y + 0.004, 0.84), 0.05, 0.008, M["shut"], rot=(90, 0, 0), n=12)
    L.cyl((-1.98, -0.70, _top(-1.98) + 0.4), 0.004, 0.8, M["chrome"], rot=(-14, 0, 0), n=4)
    if extras:
        extras(L)
    return L


# ---------------------------------------------------------------- the saloon's interior

def seat(L, M, x, y, cloth, stitch, rear=False, width=0.52):
    """A front seat: runners, a cushion with side bolsters and pleated centre, a raked backrest with
    the same pleats and its piping, the headrest on its posts."""
    cz = 0.48
    L.box((x + 0.05, y, 0.33), (0.46, 0.38, 0.06), M["metal_dark"])                           # the frame, the runners
    for s in (-1, 1):
        L.box((x + 0.05, y + s * 0.17, 0.315), (0.6, 0.03, 0.025), M["metal_dark"])
    L.box((x + 0.05, y, cz - 0.045), (0.50, width, 0.09), cloth)
    for s in (-1, 1):
        L.box((x + 0.05, y + s * (width / 2 - 0.05), cz + 0.03), (0.48, 0.10, 0.07), cloth, rot=(s * 6, 0, 0))
    for k in range(5):                                                                         # pleats and stitching
        L.box((x + 0.05, y - 0.13 + k * 0.065, cz + 0.002), (0.44, 0.006, 0.006), stitch)
    L.box((x + 0.05, y, cz + 0.0), (0.46, width - 0.22, 0.012), cloth)
    L.box((x + 0.29, y, cz - 0.01), (0.03, width - 0.02, 0.07), cloth)                         # the cushion's front roll
    # the backrest, reclined
    bx, bz = x - 0.24, cz + 0.30
    L.box((bx, y, bz), (0.10, width, 0.62), cloth, rot=(0, -14, 0))
    for s in (-1, 1):
        L.box((bx + 0.03, y + s * (width / 2 - 0.04), bz), (0.10, 0.08, 0.58), cloth, rot=(0, -14, 0))
    for k in range(5):
        L.box((bx + 0.062 + 0.0, y - 0.13 + k * 0.065, bz - 0.02), (0.006, 0.006, 0.5), stitch, rot=(0, -14, 0))
    L.box((bx + 0.055, y, bz + 0.29), (0.008, width - 0.06, 0.006), stitch, rot=(0, -14, 0))     # the piping across its top
    L.box((bx - 0.052, y, bz - 0.05), (0.01, width - 0.08, 0.36), M["trim"], rot=(0, -14, 0))   # its back panel
    if not rear:
        hx, hz = bx - 0.10, bz + 0.48
        for s in (-1, 1):
            L.cyl((hx + 0.02, y + s * 0.07, hz - 0.12), 0.007, 0.16, M["chrome"], rot=(0, -14, 0), n=6)
        L.box((hx, y, hz), (0.09, 0.26, 0.17), cloth, rot=(0, -14, 0))
        L.box((hx + 0.046, y, hz), (0.004, 0.2, 0.004), stitch, rot=(0, -14, 0))
    L.box((x + 0.28, y + 0.2 * (1 if y < 0 else -1), 0.40), (0.04, 0.06, 0.03), M["metal_dark"])   # the recline lever


def gauge(L, M, c, r, ticks, lo=-130, hi=130, needle=40):
    """A dial facing the driver (-x), tipped back: its bezel, a dark face, glowing ticks, the needle."""
    c = Vector(c)
    t = math.radians(18)
    n = Vector((-math.cos(t), 0, math.sin(t)))            # the face's normal, towards the driver's eyes
    u = Vector((math.sin(t), 0, math.cos(t)))             # the dial's "up", leaning away from him
    yv = Vector((0, 1, 0))
    rot = (0, -72, 0)
    L.cyl(tuple(c - n * 0.004), r + 0.008, 0.012, M["chrome"], rot=rot, n=24)
    L.cyl(tuple(c), r, 0.012, M["dial"], rot=rot, n=24)

    def at(a, rr, lift):
        return c + n * lift + yv * (-math.sin(a) * rr) + u * (math.cos(a) * rr)
    for k in range(ticks):
        a = math.radians(lo + (hi - lo) * k / (ticks - 1))
        major = k % 2 == 0
        L.beam(tuple(at(a, r * (0.70 if major else 0.78), 0.0075)), tuple(at(a, r * 0.90, 0.0075)), 0.0035, 0.002, M["gauge_glow"], up=tuple(n))
    a = math.radians(lo + (hi - lo) * needle / 100)
    L.beam(tuple(at(a + math.pi, r * 0.18, 0.011)), tuple(at(a, r * 0.80, 0.011)), 0.003, 0.002, M["needle"], up=tuple(n))
    L.cyl(tuple(c + n * 0.012), r * 0.12, 0.01, M["metal_dark"], rot=rot, n=10)
    L.torus(tuple(c + n * 0.007), r * 0.95, 0.0025, M["gauge_ring"], rot=rot, n1=24, n2=4)


def interior(g, M, at, yaw=0.0):
    """The cabin of the saloon from inside (right-hand drive): what the driver's camera sees."""
    L = Local(g, place(at, yaw))
    cloth, stitch, trim, carpet, dash = M["seat_cloth"], M["stitch"], M["trim"], M["carpet"], M["dash"]
    W = CAR_HALF_W - 0.09                                                                    # the inner side walls
    # the floor pan and the carpet, the tunnel, the footwells, the bulkhead
    L.box((-0.35, 0, 0.27), (2.9, 2 * W, 0.04), M["metal_dark"])
    L.box((-0.35, 0, 0.30), (2.8, 2 * W - 0.02, 0.02), carpet)
    L.box((-0.15, 0, 0.36), (2.1, 0.24, 0.12), carpet)
    L.box((-0.15, 0, 0.43), (2.0, 0.16, 0.04), carpet)
    for s in (-1, 1):
        L.box((0.20, s * 0.40, 0.315), (0.5, 0.42, 0.012), M["rubber"])                      # front mats
        L.box((-0.95, s * 0.40, 0.315), (0.35, 0.42, 0.012), M["rubber"])
    L.box((0.92, 0, 0.55), (0.04, 2 * W, 0.5), M["metal_dark"], rot=(0, -30, 0))               # the toe board
    # pedals (the driver's side): clutch, brake, accelerator
    for k, (py, w) in enumerate(((DRIVER_Y + 0.13, 0.07), (DRIVER_Y + 0.02, 0.08), (DRIVER_Y - 0.12, 0.05))):
        L.beam((0.62, py, 0.62), (0.70 if k < 2 else 0.73, py, 0.42), 0.015, 0.012, M["metal_dark"])
        L.box((0.70 if k < 2 else 0.74, py, 0.42), (0.015, w, 0.07 if k < 2 else 0.12), M["rubber"], rot=(0, -20, 0))
    # ---- the dashboard: the padded top, its face, the knee rolls, the binnacle over the dials
    dz = 0.93
    L.box((0.60, 0, dz - 0.02), (0.32, 2 * W, 0.06), dash)
    L.box((0.62, 0, dz + 0.012), (0.26, 2 * W - 0.04, 0.012), M["dash_top"])
    L.box((0.44, 0, dz - 0.03), (0.06, 2 * W - 0.02, 0.06), dash, rot=(0, 30, 0))              # the rolled front edge
    L.box((0.50, 0, 0.74), (0.12, 2 * W, 0.34), dash)
    L.box((0.44, 0, 0.56), (0.12, 2 * W, 0.08), dash, rot=(0, -20, 0))                       # the knee roll
    L.box((0.43, 0, 0.88), (0.004, 2 * W - 0.06, 0.006), stitch)
    L.box((0.47, 0, 0.60), (0.004, 2 * W - 0.06, 0.006), stitch, rot=(0, -20, 0))
    by = DRIVER_Y
    L.box((0.46, by, 0.97), (0.16, 0.44, 0.03), dash, rot=(0, -10, 0))                       # the binnacle's hood
    L.box((0.40, by, 0.955), (0.04, 0.44, 0.03), dash, rot=(0, 25, 0))
    for s in (-1, 1):
        L.box((0.46, by + s * 0.215, 0.90), (0.14, 0.02, 0.12), dash)
    L.box((0.475, by, 0.86), (0.01, 0.42, 0.15), M["dial"], rot=(0, 18, 0))                 # the recess behind the dials
    gauge(L, M, (0.462, by - 0.085, 0.865), 0.058, 13, needle=34)                            # speedometer
    gauge(L, M, (0.462, by + 0.085, 0.865), 0.058, 9, needle=28)                             # rev counter
    gauge(L, M, (0.466, by - 0.18, 0.855), 0.028, 5, lo=-60, hi=60, needle=70)              # fuel
    gauge(L, M, (0.466, by + 0.18, 0.855), 0.028, 5, lo=-60, hi=60, needle=45)              # temperature
    for k in range(5):                                                                        # warning pips, a strip of them
        L.box((0.466, by - 0.06 + k * 0.03, 0.80), (0.004, 0.012, 0.008), M["gauge_glow"] if k in (1, 3) else M["dial_off"])
    # the centre stack: vents, the radio with its glowing dial, the heater slides, the clock
    for s in (-1, 1):
        L.box((0.445, s * 0.09, 0.86), (0.02, 0.15, 0.07), M["metal_dark"])
        for k in range(4):
            L.box((0.438, s * 0.09, 0.835 + k * 0.016), (0.012, 0.14, 0.004), dash)
        L.cyl((0.445, s * (W - 0.10), 0.86), 0.04, 0.03, M["metal_dark"], rot=(0, 90, 0), n=14)    # the round end vents
        L.torus((0.43, s * (W - 0.10), 0.86), 0.04, 0.006, M["chrome"], rot=(0, 90, 0), n1=14, n2=4)
        for k in range(3):
            L.box((0.428, s * (W - 0.10), 0.845 + k * 0.015), (0.006, 0.065, 0.004), dash)
    L.box((0.452, 0, 0.74), (0.02, 0.20, 0.06), M["metal_dark"])                             # the radio
    L.box((0.441, 0, 0.75), (0.004, 0.11, 0.02), M["radio_glow"])
    for s in (-1, 1):
        L.cyl((0.438, s * 0.08, 0.735), 0.012, 0.02, M["chrome"], rot=(0, 90, 0), n=10)
    for k in range(5):
        L.box((0.441, -0.05 + k * 0.025, 0.722), (0.006, 0.016, 0.008), M["dial_off"])
    L.box((0.452, 0, 0.66), (0.02, 0.20, 0.06), M["metal_dark"])                             # the heater
    for k in range(3):
        L.box((0.441, -0.06 + k * 0.06, 0.66), (0.006, 0.008, 0.04), M["gauge_glow"] if k == 1 else M["dial_off"])
        L.box((0.438, -0.06 + k * 0.06, 0.668 - k * 0.006), (0.01, 0.018, 0.01), M["chrome"])
    L.cyl((0.445, 0, 0.94), 0.022, 0.012, M["dial"], rot=(0, 72, 0), n=16)                   # the clock on the top
    L.box((0.44, 0, 0.94), (0.003, 0.003, 0.016), M["needle"], rot=(40, -18, 0))
    # the glovebox on the passenger's side: its lid and catch; a grab handle
    L.box((0.445, 0.40, 0.73), (0.008, 0.40, 0.16), M["shut"])
    L.box((0.441, 0.40, 0.73), (0.008, 0.38, 0.14), dash)
    L.box((0.436, 0.40, 0.79), (0.01, 0.07, 0.018), M["chrome"])
    L.beam((0.44, 0.22, 0.90), (0.44, 0.58, 0.90), 0.02, 0.02, dash)
    # ---- the steering wheel: rim, three spokes, the horn pad; the column, its shroud and stalks
    wc = Vector((0.13, DRIVER_Y, 0.80))
    tilt = -67
    L.torus(tuple(wc), 0.19, 0.016, M["wheel_rim"], rot=(0, tilt, 0), n1=36, n2=8)
    Rw = Euler((0, math.radians(tilt), 0)).to_matrix()
    for k in range(24):                                                                       # the rim's stitched grip
        a = k / 24 * math.tau
        L.box(tuple(wc + Rw @ Vector((math.sin(a) * 0.19, math.cos(a) * 0.19, 0.0165))), (0.005, 0.005, 0.003), stitch)
    for a in (0, 180, 270):                                                                   # spokes at 3, 9 and 6 o'clock
        d = Vector((math.sin(math.radians(a)), math.cos(math.radians(a)), 0))
        L.beam(tuple(wc + Rw @ (d * 0.05)), tuple(wc + Rw @ (d * 0.18)), 0.03, 0.012, M["wheel_spoke"], up=tuple(Rw @ Vector((0, 0, 1))))
    L.cyl(tuple(wc + Rw @ Vector((0, 0, -0.005))), 0.055, 0.035, M["wheel_spoke"], rot=(0, tilt, 0), n=16)
    L.cyl(tuple(wc + Rw @ Vector((0, 0, 0.016))), 0.042, 0.006, M["wheel_rim"], rot=(0, tilt, 0), n=16)
    col0 = wc + Rw @ Vector((0, 0, -0.03))
    col1 = wc + Vector((0.38, 0, -0.17))
    L.beam(tuple(col0), tuple(col1), 0.035, 0.035, M["metal_dark"])
    L.beam(tuple(col0 + (col1 - col0) * 0.18), tuple(col0 + (col1 - col0) * 0.7), 0.075, 0.08, dash)
    for s in (-1, 1):
        sp = col0 + (col1 - col0) * 0.25
        L.beam(tuple(sp), tuple(sp + Vector((-0.04, s * 0.12, 0.01))), 0.01, 0.01, M["metal_dark"])
        L.box(tuple(sp + Vector((-0.045, s * 0.125, 0.012))), (0.02, 0.025, 0.014), M["rubber"])
    L.box(tuple(col0 + (col1 - col0) * 0.45 + Vector((0, 0.05, -0.04))), (0.01, 0.02, 0.03), M["chrome"])   # the key
    # ---- the gear stick on the tunnel (a gaiter, the lever, the knob), the handbrake between the seats
    gb = Vector((0.08, 0.0, 0.46))
    L.cyl(tuple(gb + Vector((0, 0, 0.03))), 0.06, 0.06, M["leather"], n=12, r2=0.025)
    L.box(tuple(gb), (0.16, 0.13, 0.012), trim)
    L.beam(tuple(gb), tuple(gb + Vector((-0.06, 0, 0.22))), 0.012, 0.012, M["chrome"])
    L.ball(tuple(gb + Vector((-0.062, 0, 0.24))), 0.032, M["leather"], sub=2, squash=(1, 1, 0.9))
    hb = Vector((-0.20, 0.0, 0.47))
    L.box(tuple(hb + Vector((0.02, 0, -0.01))), (0.30, 0.10, 0.02), trim)
    L.beam(tuple(hb + Vector((0.12, 0, 0))), tuple(hb + Vector((-0.16, 0, 0.07))), 0.035, 0.04, M["leather"])
    L.cyl(tuple(hb + Vector((-0.17, 0, 0.072))), 0.012, 0.02, M["chrome"], rot=(0, 75, 0), n=8)
    L.box((-0.55, 0, 0.52), (0.22, 0.20, 0.12), trim)                                        # a cubby between the seats
    L.box((-0.55, 0, 0.585), (0.2, 0.18, 0.012), M["leather"])
    # ---- seats
    seat(L, M, -0.52, DRIVER_Y, cloth, stitch)
    seat(L, M, -0.52, -DRIVER_Y, cloth, stitch)
    for s in (-1, 1):                                                                           # the belts' buckles by the tunnel
        L.box((-0.38, s * 0.15, 0.56), (0.04, 0.02, 0.08), M["metal_dark"])
    # the rear bench: cushion, backrest in three panels, the centre armrest, the parcel shelf
    rb = -1.32
    L.box((rb + 0.12, 0, 0.47), (0.52, 2 * W - 0.06, 0.12), cloth)
    for k in range(9):
        L.box((rb + 0.12, -0.6 + k * 0.15, 0.532), (0.48, 0.006, 0.006), stitch)
    L.box((rb - 0.22, 0, 0.78), (0.12, 2 * W - 0.06, 0.52), cloth, rot=(0, -18, 0))
    for k in range(9):
        L.box((rb - 0.16, -0.6 + k * 0.15, 0.77), (0.006, 0.006, 0.44), stitch, rot=(0, -18, 0))
    for s in (-1, 1):
        L.box((rb - 0.25, s * 0.40, 1.07), (0.08, 0.24, 0.12), cloth, rot=(0, -18, 0))
    L.box((rb - 0.42, 0, 0.98), (0.40, 2 * W - 0.04, 0.025), trim)
    for s in (-1, 1):
        L.cyl((rb - 0.42, s * 0.50, 0.995), 0.06, 0.008, M["metal_dark"], n=16)
        L.torus((rb - 0.42, s * 0.50, 0.998), 0.06, 0.006, M["chrome"], n1=16, n2=4)
    # ---- the sides: door cards front and rear, the sills, the kick panels, the B pillars
    for s in (-1, 1):
        y = s * W
        L.box((-0.45, y + s * 0.02, 0.62), (2.5, 0.04, 0.66), trim)                             # the shell behind them
        for (x0, x1) in ((-0.38, 0.72), (-1.38, -0.42)):
            cx, w = (x0 + x1) / 2, x1 - x0
            L.box((cx, y, 0.64), (w - 0.02, 0.03, 0.60), M["door_card"])
            L.box((cx, y - s * 0.012, 0.93), (w - 0.02, 0.05, 0.05), dash)                       # the padded sill capping
            L.box((cx, y - s * 0.017, 0.80), (w - 0.10, 0.006, 0.006), stitch)
            L.box((cx - 0.05, y - s * 0.04, 0.70), (w * 0.55, 0.07, 0.04), dash)               # the armrest
            L.box((cx - 0.05, y - s * 0.055, 0.67), (w * 0.4, 0.05, 0.03), M["door_card"], rot=(s * 25, 0, 0))
            L.box((x1 - 0.18, y - s * 0.02, 0.82), (0.10, 0.012, 0.035), M["chrome"])           # the door handle
            L.box((x1 - 0.18, y - s * 0.014, 0.82), (0.14, 0.006, 0.06), M["metal_dark"])
            wx = cx + 0.12                                                                       # the window winder
            L.cyl((wx, y - s * 0.02, 0.76), 0.02, 0.01, M["chrome"], rot=(90, 0, 0), n=10)
            L.beam((wx, y - s * 0.03, 0.76), (wx - 0.07, y - s * 0.03, 0.73), 0.012, 0.006, M["chrome"])
            L.cyl((wx - 0.07, y - s * 0.045, 0.73), 0.008, 0.03, M["leather"], rot=(90, 0, 0), n=8)
            L.cyl((cx - 0.12, y - s * 0.017, 0.44), 0.07, 0.008, M["metal_dark"], rot=(90, 0, 0), n=18)   # speaker
            L.torus((cx - 0.12, y - s * 0.02, 0.44), 0.07, 0.006, M["chrome"], rot=(90, 0, 0), n1=18, n2=4)
            for k in range(3):
                L.box((cx - 0.12, y - s * 0.022, 0.415 + k * 0.025), (0.11, 0.004, 0.004), M["dial"])
            L.box((cx + 0.15, y - s * 0.035, 0.45), (w * 0.35, 0.05, 0.12), M["door_card"])     # the map pocket
            L.box((cx + 0.15, y - s * 0.06, 0.505), (w * 0.35, 0.008, 0.01), stitch)
            L.cyl((x0 + 0.12, y - s * 0.02, 0.965), 0.006, 0.04, M["chrome"], n=6)            # the lock pin
        L.box((-0.45, y - s * 0.02, 0.32), (2.4, 0.08, 0.06), trim)                             # the sill tread
        L.box((-0.45, y - s * 0.03, 0.355), (2.3, 0.02, 0.008), M["chrome"])
        L.box((0.82, y - s * 0.01, 0.55), (0.18, 0.04, 0.45), trim)                             # the kick panel
        L.box((-0.40, y + s * 0.01, 1.16), (0.11, 0.06, 0.46), trim)                            # the B pillar trim
        L.box((-0.40, y - s * 0.03, 1.33), (0.05, 0.03, 0.06), M["metal_dark"])               # the belt's upper anchor
        L.beam((-0.40, y - s * 0.035, 1.31), (-0.44, s * 0.15, 0.58), 0.045, 0.004, M["belt"])     # the belt, hung up
        L.beam((-0.40, y - s * 0.035, 1.31), (-0.42, y - s * 0.06, 0.40), 0.045, 0.004, M["belt"])
    # ---- the windscreen's frame inside: A-pillar trims, the header; the headliner, visors, mirror, lamp
    for s in (-1, 1):
        a = Vector((SCREEN_BASE[0] - 0.06, s * (W - 0.03), BELT + 0.02))
        b = Vector((SCREEN_TOP[0] - 0.02, s * (W - 0.13), SCREEN_TOP[1] - 0.04))
        L.beam(tuple(a), tuple(b), 0.08, 0.05, trim)
        L.beam(tuple(a + Vector((0, -s * 0.03, 0))), tuple(b + Vector((0, -s * 0.03, 0))), 0.012, 0.03, M["rubber"])
        L.beam((REAR_BASE[0] + 0.32, s * (W - 0.03), BELT + 0.02), (REAR_TOP[0] - 0.02, s * (W - 0.14), REAR_TOP[1] - 0.03), 0.16, 0.05, trim)
    L.box((SCREEN_TOP[0] - 0.05, 0, SCREEN_TOP[1] - 0.025), (0.10, 2 * W - 0.24, 0.05), trim)  # the header rail
    L.box((-0.58, 0, ROOF_Z - 0.035), (1.06, 2 * W - 0.18, 0.03), M["headliner"])            # the headliner
    for k in range(6):
        L.box((-0.12 - k * 0.18, 0, ROOF_Z - 0.052), (0.006, 2 * W - 0.26, 0.004), M["shut"])
    for s in (-1, 1):
        L.box((-0.58, s * (W - 0.07), ROOF_Z - 0.07), (1.05, 0.10, 0.06), M["headliner"], rot=(s * 40, 0, 0))
        L.beam((-0.95, s * (W - 0.09), ROOF_Z - 0.10), (-0.70, s * (W - 0.09), ROOF_Z - 0.10), 0.02, 0.02, trim)   # grab handles
        L.box((-0.95, s * (W - 0.07), ROOF_Z - 0.08), (0.02, 0.03, 0.05), trim)
        L.box((-0.70, s * (W - 0.07), ROOF_Z - 0.08), (0.02, 0.03, 0.05), trim)
        vy = s * 0.36                                                                           # sun visors, folded up
        L.box((SCREEN_TOP[0] - 0.13, vy, ROOF_Z - 0.065), (0.16, 0.36, 0.022), M["headliner"], rot=(0, 8, 0))
        L.box((SCREEN_TOP[0] - 0.13, vy, ROOF_Z - 0.078), (0.15, 0.35, 0.004), stitch, rot=(0, 8, 0))
        L.cyl((SCREEN_TOP[0] - 0.05, vy + s * 0.17, ROOF_Z - 0.055), 0.008, 0.05, M["chrome"], rot=(0, 90, 0), n=6)
        L.box((SCREEN_TOP[0] - 0.12, -s * 0.0 + vy - s * 0.18, ROOF_Z - 0.062), (0.03, 0.02, 0.02), trim)    # its clip
    L.box((SCREEN_TOP[0] - 0.13, 0.36, ROOF_Z - 0.081), (0.08, 0.14, 0.002), M["mirror"], rot=(0, 8, 0))   # the vanity mirror
    mb = Vector((SCREEN_TOP[0] - 0.02, 0.0, SCREEN_TOP[1] - 0.04))                              # the rear-view mirror
    L.box(tuple(mb), (0.05, 0.05, 0.02), M["metal_dark"])
    L.beam(tuple(mb), tuple(mb + Vector((-0.04, 0, -0.07))), 0.014, 0.014, M["metal_dark"])
    L.box(tuple(mb + Vector((-0.055, 0, -0.10))), (0.035, 0.24, 0.065), M["metal_dark"], rot=(0, 0, -8))
    L.box(tuple(mb + Vector((-0.074, 0, -0.10))), (0.004, 0.225, 0.052), M["mirror"], rot=(0, 0, -8))
    L.box((-0.75, 0, ROOF_Z - 0.06), (0.10, 0.18, 0.02), trim)                                  # the dome lamp
    L.box((-0.75, 0, ROOF_Z - 0.071), (0.08, 0.15, 0.004), M["dome"])
    L.box((-0.40, 0, ROOF_Z - 0.06), (0.06, 2 * W - 0.2, 0.04), M["headliner"])                 # the roof bow over the B pillars
    # the scuttle outside, under the wipers (seen through the screen), and the bonnet's back edge
    L.box((SCREEN_BASE[0] + 0.07, 0, BELT - 0.005), (0.14, 2 * W - 0.1, 0.02), M["grille"])
    return L


def wiper(g, M, pivot, length, side_dir, screen_up):
    """A wiper at rest along the base of the screen: the spindle cap, the arm (with its spring), the
    blade and rubber. Built in the car's frame at its pivot."""
    p = Vector(pivot)
    d = Vector(side_dir).normalized()
    u = Vector(screen_up).normalized()
    n = d.cross(u).normalized()
    if n.z < 0:
        n = -n
    g.cyl(tuple(p), 0.022, 0.03, M["wiper"], n=12)
    g.cyl(tuple(p + n * 0.018), 0.012, 0.02, M["chrome"], n=8)
    tip = p + d * length + u * 0.03
    g.beam(tuple(p + n * 0.02), tuple(p + d * 0.12 + n * 0.035), 0.018, 0.012, M["wiper"], up=n)
    g.beam(tuple(p + d * 0.12 + n * 0.035), tuple(tip + n * 0.03), 0.012, 0.008, M["wiper"], up=n)
    g.beam(tuple(p + d * 0.16 + n * 0.04), tuple(p + d * 0.30 + n * 0.035), 0.008, 0.006, M["chrome"], up=n)   # the spring
    mid = p + d * (length * 0.62) + u * 0.02
    a, b = mid - d * length * 0.42 + n * 0.012, mid + d * length * 0.42 + n * 0.012
    g.beam(tuple(a), tuple(b), 0.012, 0.016, M["wiper"], up=n)
    g.beam(tuple(a - n * 0.008), tuple(b - n * 0.008), 0.006, 0.006, M["rubber"], up=n)
    g.beam(tuple(tip + n * 0.026), tuple(mid + n * 0.02), 0.008, 0.01, M["wiper"], up=n)


def car_materials(M):
    mat = S.mat
    S.TONE.update({"CarHandler": 0.03, "CarGlassDark": 0.02, "Chrome": 0.55, "TyreBlack": 0.02, "WheelTrim": 0.35,
                   "Grille": 0.03, "ShutLine": 0.005, "PlateWhite": 0.7, "SeatCloth": 0.075, "Stitch": 0.32, "InteriorTrim": 0.05,
                   "Carpet": 0.035, "Dash": 0.03, "DashTop": 0.02, "DoorCard": 0.06, "Headliner": 0.36, "Leather": 0.03,
                   "MetalDark": 0.02, "Dial": 0.008, "WheelRim": 0.025, "WheelSpoke": 0.04, "SeatBelt": 0.02,
                   "WiperBlack": 0.015, "MirrorGlass": 0.4})
    M["car_black"] = mat("CarHandler", (0.03, 0.03, 0.035), 0.22, metal=0.6)
    M["car_glass"] = mat("CarGlassDark", (0.02, 0.022, 0.025), 0.05, metal=0.2)
    M["chrome"] = mat("Chrome", (0.6, 0.6, 0.6), 0.15, metal=0.9)
    M["tyre"] = mat("TyreBlack", (0.02, 0.02, 0.02), 0.85)
    M["wheel_trim"] = mat("WheelTrim", (0.35, 0.35, 0.35), 0.3, metal=0.8)
    M["grille"] = mat("Grille", (0.03, 0.03, 0.03), 0.6)
    M["shut"] = mat("ShutLine", (0.005, 0.005, 0.005), 0.9)
    M["plate"] = mat("PlateWhite", (0.7, 0.7, 0.7), 0.5)
    M["head_lamp"] = mat("HeadLamp", (1, 1, 1), 0.2, emit=(1, 1, 1), strength=40.0)
    M["head_lamp_off"] = mat("HeadLampOff", (0.6, 0.6, 0.6), 0.1, metal=0.5)
    M["tail_lamp"] = mat("TailLamp", (0.3, 0.3, 0.3), 0.3, emit=(1, 1, 1), strength=1.2)
    M["indicator"] = mat("Indicator", (0.45, 0.45, 0.45), 0.2)
    M["mirror"] = mat("MirrorGlass", (0.4, 0.4, 0.42), 0.05, metal=1.0)
    M["seat_cloth"] = mottled("SeatCloth", 0.075, 0.95, scale=40.0, var=0.25)
    M["stitch"] = mat("Stitch", (0.32, 0.32, 0.32), 0.9)
    M["trim"] = mat("InteriorTrim", (0.05, 0.05, 0.05), 0.7)
    M["carpet"] = mottled("Carpet", 0.035, 1.0, scale=60.0, var=0.3)
    M["dash"] = mottled("Dash", 0.03, 0.75, scale=30.0, var=0.15)
    M["dash_top"] = mat("DashTop", (0.02, 0.02, 0.02), 0.9)
    M["door_card"] = mottled("DoorCard", 0.06, 0.8, scale=35.0, var=0.2)
    M["headliner"] = mottled("Headliner", 0.36, 0.95, scale=50.0, var=0.1)
    M["leather"] = mat("Leather", (0.03, 0.03, 0.03), 0.45)
    M["metal_dark"] = mat("MetalDark", (0.02, 0.02, 0.02), 0.5, metal=0.5)
    M["dial"] = mat("Dial", (0.008, 0.008, 0.008), 0.3)
    M["dial_off"] = mat("DialOff", (0.05, 0.05, 0.05), 0.3)
    M["gauge_glow"] = mat("GaugeGlow", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=6.0)
    M["gauge_ring"] = mat("GaugeRing", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=1.4)
    M["needle"] = mat("Needle", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=9.0)
    M["radio_glow"] = mat("RadioGlow", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=4.0)
    M["dome"] = mat("DomeLens", (0.6, 0.6, 0.6), 0.3)
    M["wheel_rim"] = mat("WheelRim", (0.025, 0.025, 0.025), 0.5)
    M["wheel_spoke"] = mat("WheelSpoke", (0.04, 0.04, 0.04), 0.4, metal=0.4)
    M["belt"] = mat("SeatBelt", (0.02, 0.02, 0.02), 0.8)
    M["wiper"] = mat("WiperBlack", (0.015, 0.015, 0.015), 0.6)
    M["rubber"] = mat("Rubber", (0.05, 0.05, 0.055), 0.9)
    return M
