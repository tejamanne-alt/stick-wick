"""Chapter 4: THE BACKYARD (docs/story/story_v2.md, CHAPTER 4: FIND HER, REACH HER, LOSE HER).
Night behind No. 14 Alder Lane, after rain. One flat side-on lane along x (z = 0, kept clear between
y -0.8 and 0.8) through four fight stretches:

  G1 THE GARDEN      x 4-34     the back of No. 14 (brick, the kitchen window lit, the back door open,
                                gutters, drainpipes), the patio and Maya's chalk hopscotch, her small
                                wellies, the wet lawn, the stepping-stone path, the rotary washing line,
                                the shed, flower beds, Maya's play area: the swing set (its seat is a
                                separate object, "Swing"), a small slide, the sandpit; the low picket
                                fence and the broken back gate (x 34) into the park
  G2 THE PLAYGROUND  x 34-66    the park: the path, a big climbing frame, the TALL SLIDE with the dark
                                gap under it (where she hid), the roundabout, the see-saw, benches,
                                bins, lamp posts, signs (NO DOGS, the park map), the railings at the
                                back the men with torches climb over
  G3 THE POND        x 66-98    the pond, its reeds and lilies, the wooden footbridge the lane crosses,
                                a willow, the lifebuoy
  G4 THE CAR PARK    x 98-131   tarmac, painted bays, kerbs, the pay machine, the lamps, the barrier
                                (its arm is separate, "BarrierArm"), the WHITE VAN at the exit ("Van",
                                its back doors "VanDoorL", "VanDoorR", each with a small window), and
                                the hill road leaving it, rising towards Kessel Hill

THE SIGHTLINE: the swings, the gate, the park, the pond and the car park are lined up along y = 2.9
(SIGHT_Y): nothing taller than 0.9 m stands in that strip, so a long lens from the swings sees down
the whole park to the van's back window, with Kessel Hill and the lit fortress under the storm
clouds behind it. The preview checks it with a ray cast.

The tower's palette (every surface a grey); moonlight, the park lamps, the kitchen window. Real
surfaces from detail.py (brick, slabs, planks, rock), bevelled edges, noise ground. Grounds are their
own objects so the props keep their texels. Two bakes for the horizon (the night, and lit by a far
strike: _Lightning), the rest one.

    /home/user/.venv-blender/bin/python tools/blender/build_park_set.py [--preview] [--lightning-preview]
                                                                       [--only=1,4,5]

Writes Assets/Art/Park/ParkSet.fbx and its bakes (Assets/Art/Park/Story_<object>.png)."""
import math
import os
import random
import sys

import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Matrix, Vector, noise  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Park")
rnd = random.Random(53)

# ---------------------------------------------------------------- the plan (mirrored in Assets/Scripts/Level/ParkPlan.cs)
LANE_X0, LANE_X1 = 1.0, 131.0
ZONE_X = (4.0, 34.0, 66.0, 98.0, 131.0)
GATE_X = 34.0                                  # the garden's end fence, the broken back gate
SWING_X, SWING_Y, SWING_TOP = 27.0, 2.9, 2.4   # the swing frame's centre; the moving seat hangs at SWING_X - 0.8
SIGHT_Y = 2.9                                  # the long sightline from the swings to the van
SLIDE_X, SLIDE_Y = 52.0, 7.0                   # the tall slide's platform (the gap under it)
POND_C, POND_R = (82.0, 1.6), (12.0, 7.2)       # the pond: centre, half extents
WATER_Z = -0.24
BRIDGE_X0, BRIDGE_X1 = 71.5, 92.5
VAN_X, VAN_Y, VAN_L = 128.2, 2.9, 5.4          # the van: centre on the ground, length (its back faces the park)
VAN_BACK = VAN_X - VAN_L / 2
BARRIER_X = 134.6
LAMPS = [(37.0, -1.75), (49.0, -1.75), (61.0, -1.75), (69.0, -1.75), (82.0, -1.6), (96.0, -1.75), (56.5, 4.7), (88.0, 9.6)]
LAMP_H = 3.7
CAR_LAMPS = [(110.5, 10.0), (124.0, 10.0)]
CAR_LAMP_H = 6.4
HILL_C, HILL_H, HILL_R = (250.0, 40.0), 30.0, (68.0, 56.0)   # Kessel Hill: its crown, height, half extents
FORT_Z = HILL_H - 1.0


def sight_clear(x, y, r):
    """True when something of radius r at (x, y) stays out of the sightline's strip."""
    return not (SWING_X - 2.0 < x < VAN_BACK + 0.5 and abs(y - SIGHT_Y) < 0.75 + r)


# ---------------------------------------------------------------- materials

def speckle(name, g, scale=4.0, grain=60.0, contrast=0.45, rough=0.8, plane="xy"):
    """A ground surface: soft patches and a fine grain (grass, tarmac, gravel, sand, soil)."""
    key = (name, plane)
    if key in D.MATS:
        return D.MATS[key]
    m, nt, bsdf = D._base(f"{name}_{plane}")
    co = D._coords(nt, plane, 1.0)
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = scale * 0.1
    nz.inputs["Detail"].default_value = 6.0
    nt.links.new(co, nz.inputs["Vector"])
    fine = nt.nodes.new("ShaderNodeTexNoise")
    fine.inputs["Scale"].default_value = grain
    fine.inputs["Detail"].default_value = 2.0
    nt.links.new(co, fine.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[0].color = D._grey(g * (1 - contrast))
    ramp.color_ramp.elements[1].position = 0.7
    ramp.color_ramp.elements[1].color = D._grey(g * (1 + contrast))
    nt.links.new(nz.outputs["Fac"], ramp.inputs["Fac"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.55
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(fine.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    nt.links.new(fine.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = rough
    D.MATS[key] = m
    return m


def mats():
    M = S.materials()
    M["brick"] = D.stone("ParkBrick", 0.2, 0.3, "xz", block=(0.225, 0.075), rough=0.9)
    M["brick_y"] = D.stone("ParkBrick", 0.2, 0.3, "yz", block=(0.225, 0.075), rough=0.9)
    M["brick_dark"] = D.stone("ParkBrickDark", 0.12, 0.2, "xz", block=(0.225, 0.075), rough=0.9)
    M["roof"] = D.stone("ParkRoofTile", 0.07, 0.03, "xz", block=(0.3, 0.2), rough=0.6)
    M["roof_y"] = D.stone("ParkRoofTile", 0.07, 0.03, "yz", block=(0.3, 0.2), rough=0.6)
    M["slab"] = D.stone("ParkSlab", 0.3, 0.12, "xy", block=(0.6, 0.6), rough=0.5)
    M["kerb"] = D.stone("ParkKerb", 0.32, 0.14, "xz", block=(0.9, 0.3))
    M["coping"] = D.flat("ParkCoping", 0.36)
    M["lawn"] = speckle("ParkLawn", 0.085, scale=3.0, grain=90.0, contrast=0.5, rough=0.45)
    M["lawn_park"] = speckle("ParkGrass", 0.07, scale=2.0, grain=90.0, contrast=0.5, rough=0.45)
    M["soil"] = speckle("ParkSoil", 0.045, scale=6.0, grain=40.0, contrast=0.4, rough=0.9)
    M["path"] = speckle("ParkTarmacPath", 0.12, scale=5.0, grain=120.0, contrast=0.25, rough=0.35)
    M["tarmac"] = speckle("ParkTarmac", 0.075, scale=4.0, grain=140.0, contrast=0.3, rough=0.3)
    M["gravel"] = speckle("ParkGravel", 0.2, scale=8.0, grain=70.0, contrast=0.4, rough=0.9)
    M["sand"] = speckle("ParkSand", 0.34, scale=6.0, grain=50.0, contrast=0.25, rough=1.0)
    M["rubber"] = D.stone("ParkSafetyTiles", 0.075, 0.035, "xy", block=(0.5, 0.5), rough=0.9)
    M["timber"] = D.planks("ParkTimber", 0.15, "xz", board=0.15)
    M["timber_y"] = D.planks("ParkTimber", 0.15, "yz", board=0.15)
    M["deck"] = D.planks("ParkDeck", 0.13, "xy", board=0.14)
    M["post"] = D.flat("ParkPost", 0.11, rough=0.9)
    M["fence"] = D.planks("ParkFence", 0.12, "xz", board=0.1)
    M["picket"] = D.flat("ParkPicket", 0.42, rough=0.8)
    M["concrete"] = D.flat("ParkConcrete", 0.26, rough=0.9)
    M["steel"] = D.flat("ParkSteel", 0.04, rough=0.45, metal=0.6)
    M["steel_mid"] = D.flat("ParkSteelMid", 0.16, rough=0.4, metal=0.5)
    M["play_pale"] = D.flat("ParkPlayPale", 0.5, rough=0.5)
    M["play_dark"] = D.flat("ParkPlayDark", 0.1, rough=0.5)
    M["play_mid"] = D.flat("ParkPlayMid", 0.26, rough=0.5)
    M["chute"] = D.flat("ParkChute", 0.42, rough=0.25, metal=0.3)
    M["rope"] = D.flat("ParkRope", 0.22, rough=1.0)
    M["upvc"] = D.flat("ParkUPVC", 0.55, rough=0.5)
    M["gutter"] = D.flat("ParkGutter", 0.03, rough=0.4)
    M["glass"] = D.flat("ParkGlass", 0.025, rough=0.05)
    M["frost"] = D.flat("ParkFrostedGlass", 0.12, rough=0.3)
    M["lit"] = D.flat("ParkWindowLit", 0.85, emit=4.0)
    M["lit_dim"] = D.flat("ParkWindowDim", 0.5, emit=0.9)
    M["curtain"] = D.flat("ParkCurtain", 0.3, emit=1.2)
    M["globe"] = D.flat("ParkLampGlobe", 1.0, emit=18.0)
    M["sodium"] = D.flat("ParkSodium", 1.0, emit=24.0)
    M["fairy"] = D.flat("ParkFairyLight", 1.0, emit=14.0)
    M["water"] = D.flat("ParkWater", 0.012, rough=0.04)
    M["puddle"] = D.flat("ParkPuddle", 0.012, rough=0.03)
    M["reed"] = D.flat("ParkReed", 0.07, rough=0.9)
    M["reed_head"] = D.flat("ParkReedHead", 0.03, rough=1.0)
    M["lily"] = D.flat("ParkLily", 0.09, rough=0.6)
    M["bark"] = D.rock("ParkBark", 0.1, "xz")
    M["foliage"] = speckle("ParkFoliage", 0.05, scale=30.0, grain=20.0, contrast=0.5, rough=0.95, plane="xz")
    M["hedge"] = speckle("ParkHedge", 0.06, scale=30.0, grain=25.0, contrast=0.5, rough=0.95, plane="xz")
    M["leaf"] = D.flat("ParkFallenLeaf", 0.17, rough=0.7)
    M["leaf_dark"] = D.flat("ParkFallenLeafDark", 0.08, rough=0.7)
    M["flower"] = D.flat("ParkFlower", 0.45, rough=0.8)
    M["chalk"] = D.flat("ParkChalk", 0.62, rough=1.0)
    M["line"] = D.flat("ParkLinePaint", 0.55, rough=0.6)
    M["sign"] = D.flat("ParkSignPlate", 0.62, rough=0.5)
    M["sign_ink"] = D.flat("ParkSignInk", 0.03, rough=0.5)
    M["cloth"] = D.flat("ParkWashing", 0.5, rough=1.0)
    M["cloth_dark"] = D.flat("ParkWashingDark", 0.2, rough=1.0)
    M["rubber_boot"] = D.flat("ParkWellies", 0.07, rough=0.4)
    M["van"] = D.flat("ParkVanWhite", 0.6, rough=0.35)
    M["van_trim"] = D.flat("ParkVanTrim", 0.05, rough=0.6)
    M["tyre"] = D.flat("ParkTyre", 0.02, rough=0.9)
    M["chrome"] = D.flat("ParkChrome", 0.3, rough=0.2, metal=0.8)
    M["van_inside"] = D.flat("ParkVanInside", 0.08, emit=0.25)
    M["car_a"] = D.flat("ParkCarDark", 0.05, rough=0.3, metal=0.4)
    M["car_b"] = D.flat("ParkCarMid", 0.18, rough=0.3, metal=0.4)
    M["sky"] = D.flat("ParkNightSky", 0.02, emit=0.06)
    M["moon"] = D.flat("ParkMoon", 1.0, emit=7.0)
    M["cloud"] = speckle("ParkStormCloud", 0.05, scale=1.0, grain=4.0, contrast=0.6, rough=1.0, plane="xz")
    M["cloud_lit"] = D.flat("ParkCloudGlow", 0.1, emit=0.16)
    M["hill"] = D.rock("ParkHill", 0.045, "xy")
    M["hill_side"] = D.rock("ParkHill", 0.045, "xz")
    M["fort"] = D.stone("ParkFortStone", 0.1, 0.05, "xz", block=(1.6, 0.7))
    M["fort_y"] = D.stone("ParkFortStone", 0.1, 0.05, "yz", block=(1.6, 0.7))
    M["fort_win"] = D.flat("ParkFortWindow", 1.0, emit=9.0)
    M["town"] = D.flat("ParkTownDark", 0.035, rough=0.8)
    M["town_win"] = D.flat("ParkTownWindow", 0.9, emit=3.0)
    M["far"] = D.flat("ParkFarHills", 0.03, rough=1.0)
    return M


# ---------------------------------------------------------------- building blocks

def sub(g, fn, at=(0, 0, 0), yaw=0.0, scale=1.0, tilt=None):
    """Build a prop in its own frame (fn(tmp)), then set it into g: turned by yaw about z (degrees),
    optionally tilted (an Euler in degrees, applied first), moved to `at`."""
    tmp = S.Group("tmp")
    fn(tmp)
    T = Matrix.Translation(Vector(at)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Scale(scale, 4)
    if tilt is not None:
        from mathutils import Euler
        T = T @ Euler([math.radians(a) for a in tilt]).to_matrix().to_4x4()
    tmp.bm.transform(T)
    me = bpy.data.meshes.new("sub")
    tmp.bm.to_mesh(me)
    tmp.bm.free()
    remap = [g._mi(m) for m in tmp.mats]
    for p in me.polygons:
        p.material_index = remap[p.material_index]
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def limb(g, a, b, r1, r2, m, n=6):
    """A tapered round piece from a to b (a trunk, a branch, a tube)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    L = d.length
    if L < 1e-4:
        return
    tmp = bmesh.new()
    bmesh.ops.create_cone(tmp, cap_ends=True, segments=n, radius1=r1, radius2=r2, depth=L)
    q = d.to_track_quat("Z", "Y")
    bmesh.ops.transform(tmp, matrix=Matrix.Translation((a + b) * 0.5) @ q.to_matrix().to_4x4(), verts=tmp.verts)
    g._merge(tmp, m)


def blob(g, c, r, m, s=(1.0, 1.0, 1.0), subdiv=1, rough=0.35):
    """A lumpy mass (a clump of leaves, a bush, a hedge, a cloud): an icosphere pushed about by noise."""
    tmp = bmesh.new()
    bmesh.ops.create_icosphere(tmp, subdivisions=subdiv, radius=r)
    o = Vector((rnd.uniform(0, 99), rnd.uniform(0, 99), rnd.uniform(0, 99)))
    for v in tmp.verts:
        v.co *= 1.0 + rough * noise.noise(v.co * (1.3 / r) + o)
        v.co.x *= s[0]
        v.co.y *= s[1]
        v.co.z *= s[2]
    bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector(c)), verts=tmp.verts)
    g._merge(tmp, m)


def slab(g, pts, z0, z1, m):
    """A flat outline (x, y points, counter-clockwise) extruded from z0 to z1 (a pond's water, a puddle)."""
    tmp = bmesh.new()
    bot = [tmp.verts.new((x, y, z0)) for x, y in pts]
    top = [tmp.verts.new((x, y, z1)) for x, y in pts]
    tmp.faces.new(top)
    tmp.faces.new(list(reversed(bot)))
    n = len(pts)
    for i in range(n):
        tmp.faces.new((bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i]))
    g._merge(tmp, m)


def flecks(g, items, m):
    """Many small flat pieces in one go (fallen leaves, chalk, petals): (x, y, z, w, l, yaw) each."""
    tmp = bmesh.new()
    for (x, y, z, w, l, yaw) in items:
        c, s = math.cos(yaw), math.sin(yaw)
        vs = []
        for (u, v) in ((-l, -w), (l, -w), (l, w), (-l, w)):
            vs.append(tmp.verts.new((x + u * c - v * s, y + u * s + v * c, z)))
        tmp.faces.new(vs)
    me = bpy.data.meshes.new("flecks")
    tmp.to_mesh(me)
    tmp.free()
    for p in me.polygons:
        p.material_index = g._mi(m)
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def flat_text(g, text, c, size, m, yaw=0.0):
    """Letters lying flat on the ground (chalk, road paint), reading from the -y side."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.0015
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    me.transform(Matrix.Translation(Vector(c)) @ Matrix.Rotation(math.radians(yaw), 4, "Z"))
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def holed_wall(g, axis, at, a0, a1, z0, z1, t, holes, m):
    """A wall with openings. axis "x": the wall runs along x at y = at; "y": along y at x = at.
    holes: (a0, a1, z0, z1) each, along the wall's run."""
    cuts = sorted({a0, a1, *[h[0] for h in holes], *[h[1] for h in holes]})
    for i in range(len(cuts) - 1):
        u0, u1 = cuts[i], cuts[i + 1]
        if u1 - u0 < 1e-4 or u0 < a0 - 1e-4 or u1 > a1 + 1e-4:
            continue
        mid = (u0 + u1) / 2
        spans = [(h[2], h[3]) for h in holes if h[0] <= mid <= h[1]]
        zs = [z0]
        for (h0, h1) in sorted(spans):
            zs += [h0, h1]
        zs.append(z1)
        for k in range(0, len(zs), 2):
            za, zb = zs[k], zs[k + 1]
            if zb - za < 1e-4:
                continue
            if axis == "x":
                g.box((mid, at, (za + zb) / 2), (u1 - u0, t, zb - za), m)
            else:
                g.box((at, mid, (za + zb) / 2), (t, u1 - u0, zb - za), m)


def chain(g, top, bottom, m, link=0.055):
    """A chain of round links (alternating), hanging from top to bottom."""
    top, bottom = Vector(top), Vector(bottom)
    L = (top - bottom).length
    n = max(2, int(L / link))
    for k in range(n):
        p = top.lerp(bottom, (k + 0.5) / n)
        g.torus(p[:], link * 0.42, link * 0.1, m, rot=(90, 0, 90 if k % 2 else 0), n1=6, n2=4)


def bolt(g, c, m, axis="y", r=0.022):
    rot = {"x": (0, 90, 0), "y": (90, 0, 0), "z": None}[axis]
    g.cyl(c, r, 0.03, m, rot=rot, n=6)


def tree(g, x, y, h, M, z=0.0, spread=1.0, leafy=0.55, mains=5, lean=0.25):
    """A broadleaf tree in autumn: a trunk with a root flare, main limbs, branches and twigs, and
    clumps of what leaves are left at the ends."""
    base = Vector((x, y, z))
    top = base + Vector((rnd.uniform(-lean, lean), rnd.uniform(-lean, lean), h * rnd.uniform(0.38, 0.48)))
    tr = 0.028 * h
    limb(g, base - Vector((0, 0, 0.3)), top, tr * 1.25, tr * 0.72, M["bark"], n=9)
    for k in range(4):                                                         # the root flare
        a = k / 4 * math.tau + rnd.uniform(-0.3, 0.3)
        limb(g, base + Vector((0, 0, 0.6)), base + Vector((math.cos(a) * tr * 3.2, math.sin(a) * tr * 3.2, -0.1)), tr * 0.55, tr * 0.2, M["bark"], n=5)
    a0 = rnd.uniform(0, math.tau)
    for k in range(mains):
        a = a0 + k / mains * math.tau + rnd.uniform(-0.35, 0.35)
        el = math.radians(rnd.uniform(30, 62))
        L = h * rnd.uniform(0.3, 0.42) * spread
        d = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el) * 0.75, math.sin(el)))
        start = top - Vector((0, 0, rnd.uniform(0, h * 0.08)))
        mid = start + d * L * 0.55 + Vector((0, 0, rnd.uniform(-0.2, 0.5)))
        end = mid + (d + Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 0.25))).normalized() * L * 0.5
        limb(g, start, mid, tr * 0.62, tr * 0.42, M["bark"], n=6)
        limb(g, mid, end, tr * 0.42, tr * 0.14, M["bark"], n=5)
        for j in range(3):
            p = start.lerp(end, rnd.uniform(0.35, 0.95))
            d2 = (d + Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(0.1, 0.9)))).normalized()
            e2 = p + d2 * L * rnd.uniform(0.25, 0.45)
            limb(g, p, e2, tr * 0.18, tr * 0.04, M["bark"], n=4)
            if rnd.random() < leafy:
                blob(g, e2[:], rnd.uniform(0.7, 1.25) * h / 9, M["foliage"], s=(1.2, 1.0, 0.75))
        if rnd.random() < leafy + 0.2:
            blob(g, end[:], rnd.uniform(1.0, 1.6) * h / 9, M["foliage"], s=(1.25, 1.05, 0.8))


def pine(g, x, y, h, M, z=0.0):
    D.pine(g, x, y, z, h, M["bark"], M["foliage"])


def hedge(g, x0, x1, y, h, d, M, z=0.0):
    """A clipped hedge: a run of overlapping lumpy masses."""
    g.box(((x0 + x1) / 2, y, z + h * 0.42), (x1 - x0 - h * 0.3, d * 0.8, h * 0.84), M["hedge"])
    n = max(2, int((x1 - x0) / (h * 0.55)))
    for k in range(n):
        x = x0 + (x1 - x0) * (k + rnd.uniform(0.2, 0.8)) / n
        r = h * rnd.uniform(0.32, 0.5)
        blob(g, (x, y + rnd.uniform(-0.15, 0.15) * d, z + h - r * rnd.uniform(0.6, 1.0)), r, M["hedge"], s=(rnd.uniform(1.1, 1.6), d / h * 1.1, rnd.uniform(0.7, 1.0)), rough=0.4)


def bush(g, c, r, M):
    blob(g, (c[0], c[1], c[2] + r * 0.6), r, M["hedge"], s=(1.2, 1.0, 0.8), rough=0.3)


def tufts(g, x0, x1, y0, y1, n, M, h=0.25):
    """Long grass at the edges: thin blades in clumps."""
    for _ in range(n):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        for k in range(5):
            a = rnd.uniform(0, math.tau)
            limb(g, (x, y, 0.0), (x + math.cos(a) * h * 0.4, y + math.sin(a) * h * 0.4, h * rnd.uniform(0.6, 1.1)), 0.012, 0.002, M["reed"], n=3)


def ground(g, x0, x1, y0, y1, m, res, h=None, amp=0.05, seed=0.0, flat=1.4):
    """A ground surface: h(x, y) plus noise, the noise fading out over the lane (it stays flat)."""
    tmp = bmesh.new()
    nx, ny = max(2, int((x1 - x0) / res)), max(2, int((y1 - y0) / res))
    rows = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = x0 + (x1 - x0) * i / nx
            y = y0 + (y1 - y0) * j / ny
            fade = min(1.0, max(0.0, (abs(y) - 0.9) / max(0.01, flat - 0.9)))
            z = (h(x, y) if h else 0.0) + amp * fade * noise.noise(Vector((x * 0.35 + seed, y * 0.35, 0.7)))
            row.append(tmp.verts.new((x, y, z)))
        rows.append(row)
    for j in range(ny):
        for i in range(nx):
            tmp.faces.new((rows[j][i], rows[j][i + 1], rows[j + 1][i + 1], rows[j + 1][i]))
    me = bpy.data.meshes.new("ground")
    tmp.to_mesh(me)
    tmp.free()
    for p in me.polygons:
        p.material_index = g._mi(m)
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def pond_depth(x, y):
    u = ((x - POND_C[0]) / POND_R[0]) ** 2 + ((y - POND_C[1]) / POND_R[1]) ** 2
    if u >= 1.0:
        return 0.0
    return -0.75 * (1.0 - u) ** 0.8


def pond_outline(scale, n=56, wob=0.06):
    pts = []
    for k in range(n):
        a = k / n * math.tau
        w = 1.0 + wob * noise.noise(Vector((math.cos(a) * 2.0, math.sin(a) * 2.0, 3.3)))
        pts.append((POND_C[0] + math.cos(a) * POND_R[0] * scale * w, POND_C[1] + math.sin(a) * POND_R[1] * scale * w))
    return pts


def leaves(g, x0, x1, y0, y1, n, M, zf=None, avoid=None):
    """Fallen leaves, pale and dark, thicker at the edges of things."""
    items, dark = [], []
    for _ in range(n):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        if avoid and avoid(x, y):
            continue
        z = (zf(x, y) if zf else 0.0) + 0.012
        it = (x, y, z, rnd.uniform(0.025, 0.04), rnd.uniform(0.04, 0.065), rnd.uniform(0, math.tau))
        (items if rnd.random() < 0.55 else dark).append(it)
    flecks(g, items, M["leaf"])
    flecks(g, dark, M["leaf_dark"])


def puddle(g, x, y, rx, ry, M, z=0.006):
    pts = []
    for k in range(18):
        a = k / 18 * math.tau
        w = 1.0 + 0.25 * noise.noise(Vector((math.cos(a) * 1.5 + x, math.sin(a) * 1.5 + y, 0.5)))
        pts.append((x + math.cos(a) * rx * w, y + math.sin(a) * ry * w))
    slab(g, pts, z - 0.004, z, M["puddle"])


def sign_board(g, x, y, text, M, h=1.6, w=0.7, ht=0.45, size=0.13, lines=None, two_legs=False):
    """A sign on a post (or two): the plate, a dark edge, the words."""
    if two_legs:
        for s in (-1, 1):
            g.box((x + s * (w / 2 - 0.05), y + 0.03, (h - ht / 2) / 2), (0.07, 0.07, h - ht / 2), M["steel"])
    else:
        g.cyl((x, y + 0.04, h / 2), 0.035, h, M["steel"], n=8)
    g.box((x, y, h), (w + 0.04, 0.03, ht + 0.04), M["sign_ink"])
    g.box((x, y - 0.006, h), (w, 0.03, ht), M["sign"])
    rows = lines or [text]
    for i, t in enumerate(rows):
        S.stencil(g, t, (x, y - 0.03, h + (len(rows) - 1) * size * 0.65 - i * size * 1.3 - size * 0.35), size, M["sign_ink"])


# ---------------------------------------------------------------- the house: the back of No. 14

def window(g, plane, c, w, h, M, glass, sill=True, panes=(2, 1), curtains=False):
    """A window in a wall facing -y (plane "xz", c = (x, y_face, z_centre)) or +x (plane "yz",
    c = (x_face, y, z)): the frame, the glass, the bars, a stone sill, a lintel of soldier bricks."""
    x, y, z = c
    fr = 0.06
    if plane == "xz":
        g.box((x, y + 0.06, z), (w, 0.04, h), glass)
        for s in (-1, 1):
            g.box((x + s * (w / 2 - fr / 2), y + 0.02, z), (fr, 0.08, h), M["upvc"])
            g.box((x, y + 0.02, z + s * (h / 2 - fr / 2)), (w, 0.08, fr), M["upvc"])
        for k in range(1, panes[0]):
            g.box((x - w / 2 + w * k / panes[0], y + 0.03, z), (0.045, 0.06, h - fr), M["upvc"])
        for k in range(1, panes[1]):
            g.box((x, y + 0.03, z - h / 2 + h * k / panes[1]), (w - fr, 0.06, 0.045), M["upvc"])
        if sill:
            g.box((x, y - 0.06, z - h / 2 - 0.04), (w + 0.2, 0.16, 0.07), M["coping"])
        g.box((x, y - 0.01, z + h / 2 + 0.12), (w + 0.25, 0.04, 0.22), M["brick_dark"])
        if curtains:
            for s in (-1, 1):
                g.box((x + s * (w / 2 - w * 0.16), y + 0.12, z), (w * 0.3, 0.03, h - 0.1), M["curtain"])
    else:
        g.box((x - 0.06, y, z), (0.04, w, h), glass)
        for s in (-1, 1):
            g.box((x - 0.02, y + s * (w / 2 - fr / 2), z), (0.08, fr, h), M["upvc"])
            g.box((x - 0.02, y, z + s * (h / 2 - fr / 2)), (0.08, w, fr), M["upvc"])
        for k in range(1, panes[0]):
            g.box((x - 0.03, y - w / 2 + w * k / panes[0], z), (0.06, 0.045, h - fr), M["upvc"])
        for k in range(1, panes[1]):
            g.box((x - 0.03, y, z - h / 2 + h * k / panes[1]), (0.06, w - fr, 0.045), M["upvc"])
        if sill:
            g.box((x + 0.06, y, z - h / 2 - 0.04), (0.16, w + 0.2, 0.07), M["coping"])
        g.box((x + 0.01, y, z + h / 2 + 0.12), (0.04, w + 0.25, 0.22), M["brick_dark"])


def downpipe(g, x, y, z0, z1, M, shoe_dir=(0, -1)):
    g.cyl((x, y, (z0 + z1) / 2), 0.034, z1 - z0, M["gutter"], n=8)
    for k in range(int((z1 - z0) / 1.3) + 1):
        g.box((x, y + 0.03, z0 + 0.4 + k * 1.3), (0.09, 0.03, 0.04), M["gutter"])
    limb(g, (x, y, z0 + 0.15), (x + shoe_dir[0] * 0.14, y + shoe_dir[1] * 0.14, z0 + 0.05), 0.034, 0.034, M["gutter"], n=8)


def gutter(g, a, b, M):
    """A half-round gutter along an eave from a to b, its brackets."""
    limb(g, a, b, 0.065, 0.065, M["gutter"], n=8)
    a, b = Vector(a), Vector(b)
    n = int((b - a).length / 0.8) + 1
    for k in range(n + 1):
        p = a.lerp(b, k / n)
        g.box(p[:], (0.04, 0.04, 0.15), M["gutter"])


def build_house(coll, M):
    g = S.Group("ParkHouse")
    # the main house: two storeys, its rear face at y = 4 (towards the garden), x -9.5..7.5
    hx0, hx1, hy0, hy1, eave, ridge = -9.5, 7.5, 4.0, 11.0, 5.7, 8.8
    holes = [(-8.2, -7.2, 0.9, 2.1), (-7.4, -6.2, 3.3, 4.6), (-3.2, -2.4, 3.6, 4.6), (0.8, 2.2, 3.3, 4.6), (4.2, 5.6, 3.3, 4.6),
             (2.4, 4.4, 0.0, 2.1), (5.6, 6.8, 0.9, 2.1)]
    _house_body(g, M, hx0, hx1, hy0, hy1, eave, ridge, holes)
    return g.finish(coll)


def gable(g, x, y0, y1, z0, z1, t, m):
    """A triangular gable end in the y-z plane at x."""
    tmp = bmesh.new()
    pts = [(y0, z0), (y1, z0), ((y0 + y1) / 2, z1)]
    a = [tmp.verts.new((x - t / 2, y, z)) for y, z in pts]
    b = [tmp.verts.new((x + t / 2, y, z)) for y, z in pts]
    tmp.faces.new(a)
    tmp.faces.new(list(reversed(b)))
    for i in range(3):
        tmp.faces.new((a[i], a[(i + 1) % 3], b[(i + 1) % 3], b[i]))
    g._merge(tmp, m)


def gable_x(g, y, x0, x1, z0, z1, t, m):
    """A triangular gable end in the x-z plane at y (facing the camera)."""
    tmp = bmesh.new()
    pts = [(x0, z0), (x1, z0), ((x0 + x1) / 2, z1)]
    a = [tmp.verts.new((x, y - t / 2, z)) for x, z in pts]
    b = [tmp.verts.new((x, y + t / 2, z)) for x, z in pts]
    tmp.faces.new(a)
    tmp.faces.new(list(reversed(b)))
    for i in range(3):
        tmp.faces.new((a[i], a[(i + 1) % 3], b[(i + 1) % 3], b[i]))
    g._merge(tmp, m)


def pitched_roof(g, x0, x1, y0, y1, eave, ridge, M, along="x", courses=True, over=0.35):
    """A pitched tile roof over the box x0..x1, y0..y1; its ridge runs along `along`."""
    if along == "x":
        half = (y1 - y0) / 2 + over
        rise = ridge - eave
        ang = math.degrees(math.atan2(rise, half))
        L = math.hypot(half, rise)
        for s in (-1, 1):
            yc = (y0 + y1) / 2 + s * half / 2
            g.box(((x0 + x1) / 2, yc, eave + rise / 2 + 0.06), (x1 - x0 + 2 * over, L, 0.1), M["roof"], rot=(-s * ang, 0, 0))
            if courses:
                n = int(L / 0.24)
                for k in range(n):
                    t = (k + 0.5) / n
                    yy = (y0 + y1) / 2 + s * half * (1 - t)
                    zz = eave + rise * t + 0.13
                    g.box(((x0 + x1) / 2, yy, zz), (x1 - x0 + 2 * over, 0.025, 0.035), M["steel"], rot=(-s * ang, 0, 0))
        g.box(((x0 + x1) / 2, (y0 + y1) / 2, ridge + 0.12), (x1 - x0 + 2 * over + 0.05, 0.28, 0.14), M["roof"])
        for k in range(int((x1 - x0 + 2 * over) / 0.45)):
            g.cyl((x0 - over + 0.22 + k * 0.45, (y0 + y1) / 2, ridge + 0.2), 0.12, 0.42, M["roof"], rot=(0, 90, 0), n=8)
    else:
        half = (x1 - x0) / 2 + over
        rise = ridge - eave
        ang = math.degrees(math.atan2(rise, half))
        L = math.hypot(half, rise)
        for s in (-1, 1):
            xc = (x0 + x1) / 2 + s * half / 2
            g.box((xc, (y0 + y1) / 2, eave + rise / 2 + 0.06), (L, y1 - y0 + 2 * over, 0.1), M["roof_y"], rot=(0, s * ang, 0))
            if courses:
                n = int(L / 0.24)
                for k in range(n):
                    t = (k + 0.5) / n
                    xx = (x0 + x1) / 2 + s * half * (1 - t)
                    zz = eave + rise * t + 0.13
                    g.box((xx, (y0 + y1) / 2, zz), (0.025, y1 - y0 + 2 * over, 0.035), M["steel"], rot=(0, s * ang, 0))
        g.box(((x0 + x1) / 2, (y0 + y1) / 2, ridge + 0.12), (0.28, y1 - y0 + 2 * over + 0.05, 0.14), M["roof"])
        for k in range(int((y1 - y0 + 2 * over) / 0.45)):
            g.cyl(((x0 + x1) / 2, y0 - over + 0.22 + k * 0.45, ridge + 0.2), 0.12, 0.42, M["roof"], rot=(90, 0, 0), n=8)


def _house_body(g, M, hx0, hx1, hy0, hy1, eave, ridge, holes):
    holed_wall(g, "x", hy0 + 0.15, hx0, hx1, 0.0, eave, 0.3, holes, M["brick"])
    g.box(((hx0 + hx1) / 2, hy1 - 0.15, eave / 2), (hx1 - hx0, 0.3, eave), M["brick"])
    for x in (hx0 + 0.15, hx1 - 0.15):
        g.box((x, (hy0 + hy1) / 2, eave / 2), (0.3, hy1 - hy0, eave), M["brick_y"])
        gable(g, x, hy0, hy1, eave, ridge - 0.1, 0.3, M["brick_y"])
    g.box(((hx0 + hx1) / 2, hy0 - 0.005, 0.15), (hx1 - hx0, 0.02, 0.04), M["brick_dark"])          # the damp course
    for x in (-8.9, -4.6, 0.4, 6.9):                                                             # air bricks
        g.box((x, hy0 - 0.01, 0.3), (0.22, 0.03, 0.08), M["steel"])
    window(g, "xz", (-7.7, hy0, 1.5), 1.0, 1.2, M, M["glass"])
    window(g, "xz", (-6.8, hy0, 3.95), 1.2, 1.3, M, M["glass"])
    window(g, "xz", (-2.8, hy0, 4.1), 0.8, 1.0, M, M["frost"], panes=(1, 1))
    window(g, "xz", (1.5, hy0, 3.95), 1.4, 1.3, M, M["lit_dim"], curtains=True)
    window(g, "xz", (4.9, hy0, 3.95), 1.4, 1.3, M, M["glass"], curtains=True)
    window(g, "xz", (6.2, hy0, 1.5), 1.2, 1.2, M, M["glass"])
    # Maya's window: paper stars stuck to the glass, a night light's glow left on
    for (dx, dz) in ((-0.3, 0.2), (0.15, 0.35), (0.35, -0.1), (-0.1, -0.25)):
        g.cyl((4.9 + dx, hy0 + 0.03, 3.95 + dz), 0.06, 0.01, M["chalk"], rot=(90, 0, 0), n=5)
    for s in (-1, 1):                                                                           # the French doors
        g.box((3.4 + s * 0.5, hy0 + 0.1, 1.05), (0.98, 0.04, 2.1), M["glass"])
        g.box((3.4 + s * 0.97, hy0 + 0.05, 1.05), (0.06, 0.08, 2.1), M["upvc"])
        g.box((3.4 + s * 0.5, hy0 + 0.05, 0.06), (0.98, 0.08, 0.12), M["upvc"])
        g.box((3.4 + s * 0.06, hy0 + 0.05, 1.05), (0.08, 0.08, 2.1), M["upvc"])
        g.box((3.4 + s * 0.16, hy0 - 0.0, 1.05), (0.03, 0.04, 0.18), M["chrome"])
    g.box((3.4, hy0 + 0.05, 2.13), (2.1, 0.08, 0.06), M["upvc"])
    g.box((3.4, hy0 - 0.01, 2.27), (2.3, 0.04, 0.22), M["brick_dark"])
    g.box((3.4, hy0 - 0.25, 0.05), (2.3, 0.5, 0.1), M["coping"])                                    # the step
    g.box((3.4, hy0 + 2.0, 1.05), (2.0, 0.1, 2.1), M["lit_dim"])                                    # the dim room beyond
    pitched_roof(g, hx0, hx1, hy0, hy1, eave, ridge, M, along="x")
    g.box(((hx0 + hx1) / 2, hy0 - 0.33, eave - 0.05), (hx1 - hx0 + 0.7, 0.04, 0.22), M["upvc"])      # the fascia
    g.box(((hx0 + hx1) / 2, hy0 - 0.15, eave - 0.15), (hx1 - hx0 + 0.7, 0.36, 0.03), M["upvc"])      # the soffit
    gutter(g, (hx0 - 0.3, hy0 - 0.42, eave - 0.12), (hx1 + 0.3, hy0 - 0.42, eave - 0.12), M)
    downpipe(g, hx1 + 0.1, hy0 - 0.42, 0.0, eave - 0.12, M)
    downpipe(g, -4.9, hy0 - 0.12, 2.9, eave - 0.12, M)                                           # onto the kitchen roof
    # the soil stack, the outside tap, a satellite dish, the security light (off)
    g.cyl((-0.5, hy0 - 0.12, eave / 2 + 0.3), 0.055, eave + 0.6, M["gutter"], n=8)
    for k in range(4):
        g.box((-0.5, hy0 - 0.05, 0.8 + k * 1.4), (0.14, 0.05, 0.05), M["gutter"])
    g.cyl((-0.5, hy0 - 0.12, eave + 0.75), 0.07, 0.15, M["gutter"], n=8)
    g.cyl((6.9, hy0 - 0.08, 0.7), 0.03, 0.1, M["chrome"], rot=(90, 0, 0), n=6)
    g.box((6.9, hy0 - 0.12, 0.62), (0.05, 0.05, 0.08), M["chrome"])
    g.cyl((-4.0, hy0 - 0.35, 5.0), 0.3, 0.04, M["upvc"], rot=(70, 0, 20), n=14)
    g.beam((-4.0, hy0, 5.0), (-4.0, hy0 - 0.35, 5.0), 0.03, 0.03, M["steel"])
    # the chimney stack on the ridge, its pots, the TV aerial
    cx = -7.6
    g.box((cx, (hy0 + hy1) / 2, ridge + 0.7), (1.0, 0.6, 2.0), M["brick"])
    g.box((cx, (hy0 + hy1) / 2, ridge + 1.75), (1.15, 0.75, 0.12), M["coping"])
    for dx in (-0.25, 0.25):
        g.cyl((cx + dx, (hy0 + hy1) / 2, ridge + 2.05), 0.13, 0.5, M["concrete"], n=10, r2=0.1)
    g.cyl((cx + 0.6, (hy0 + hy1) / 2, ridge + 2.3), 0.02, 1.8, M["steel"], n=5)
    for k in range(6):
        g.box((cx + 0.6, (hy0 + hy1) / 2 - 0.1, ridge + 2.9 + k * 0.04), (0.04, 0.5 - k * 0.06, 0.012), M["steel"])
    g.box((cx + 0.6, (hy0 + hy1) / 2 - 0.1, ridge + 2.95), (0.03, 0.03, 0.9), M["steel"], rot=(90, 0, 0))

    # the kitchen extension: single storey, projecting from the house towards the garden's front;
    # its side wall (x = 0) faces the garden: the kitchen window lit, the back door open
    ex0, ex1, ey0, ey1, ee, er = -5.6, 0.0, -3.4, 4.0, 2.85, 4.2
    holed_wall(g, "y", ex1 - 0.15, ey0, ey1, 0.0, ee, 0.3, [(-0.75, 0.15, 0.0, 2.05), (1.15, 2.45, 0.95, 2.05)], M["brick_y"])
    holed_wall(g, "x", ey0 + 0.15, ex0, ex1, 0.0, ee, 0.3, [(-3.5, -2.3, 1.0, 2.0)], M["brick"])
    g.box((ex0 + 0.15, (ey0 + ey1) / 2, ee / 2), (0.3, ey1 - ey0, ee), M["brick_y"])
    gable_x(g, ey0 + 0.15, ex0, ex1, ee, er - 0.1, 0.3, M["brick"])
    pitched_roof(g, ex0, ex1, ey0, ey1, ee, er, M, along="y", over=0.3)
    for s in (-1, 1):
        x = (ex0 + ex1) / 2 + s * ((ex1 - ex0) / 2 + 0.38)
        gutter(g, (x, ey0 - 0.3, ee - 0.08), (x, ey1, ee - 0.08), M)
        g.box((x - s * 0.06, (ey0 + ey1) / 2 - 0.15, ee), (0.04, ey1 - ey0 + 0.3, 0.2), M["upvc"])
    downpipe(g, ex1 + 0.38, ey0 - 0.25, 0.0, ee - 0.1, M, shoe_dir=(1, 0))
    g.cyl((ex1 + 0.75, ey0 - 0.3, 0.45), 0.32, 0.9, M["play_dark"], n=14)                      # the water butt under it
    g.cyl((ex1 + 0.75, ey0 - 0.3, 0.92), 0.34, 0.05, M["play_dark"], n=14)
    g.cyl((ex1 + 0.75, ey0 - 0.62, 0.18), 0.025, 0.08, M["chrome"], rot=(90, 0, 0), n=6)
    window(g, "xz", (-2.9, ey0, 1.5), 1.2, 1.0, M, M["glass"])
    # the kitchen window, lit: the frame, a blind half down, a plant on the sill inside
    window(g, "yz", (ex1, 1.8, 1.5), 1.3, 1.1, M, M["lit"], panes=(2, 1))
    for k in range(9):
        g.box((ex1 - 0.12, 1.8, 1.98 - k * 0.045), (0.02, 1.2, 0.03), M["upvc"])                 # the blind's slats
    g.cyl((ex1 - 0.14, 2.2, 1.05), 0.08, 0.14, M["play_dark"], n=8)
    blob(g, (ex1 - 0.14, 2.2, 1.22), 0.13, M["foliage"])
    g.box((ex1 - 1.5, 1.8, 0.9), (0.4, 1.8, 0.9), M["lit_dim"])                                   # the worktop under it
    # the back door, open into the lit kitchen: frame, the door leaf swung in, the step, the mat
    for s in (-1, 1):
        g.box((ex1 - 0.02, -0.3 + s * 0.48, 1.05), (0.1, 0.06, 2.1), M["upvc"])
    g.box((ex1 - 0.02, -0.3, 2.07), (0.1, 1.02, 0.06), M["upvc"])
    g.box((ex1 - 0.55, 0.14, 1.03), (0.9, 0.05, 2.0), M["upvc"], rot=(0, 0, -62))
    g.box((ex1 - 0.55 - 0.18, 0.14 - 0.35, 1.0), (0.04, 0.1, 0.18), M["chrome"], rot=(0, 0, -62))
    g.box((ex1 - 2.2, -0.3, 1.3), (0.1, 2.2, 2.6), M["lit"])                                      # the kitchen's far wall
    g.box((ex1 - 1.1, -0.3, 0.02), (2.2, 2.2, 0.04), M["slab"])
    g.box((ex1 - 1.6, -1.25, 0.45), (0.9, 0.6, 0.9), M["upvc"])                                   # a cupboard
    g.box((ex1 + 0.2, -0.3, 0.05), (0.4, 1.1, 0.1), M["coping"])
    g.box((ex1 + 0.62, -0.3, 0.012), (0.45, 0.75, 0.02), M["play_dark"])
    g.box((ex1 + 0.02, 0.45, 2.35), (0.18, 0.22, 0.14), M["steel"])                             # the security light
    g.box((ex1 + 0.06, -0.3, 2.3), (0.06, 1.3, 0.05), M["coping"])                               # the lintel
    # her small wellies by the door, one fallen over; Papa's big boots
    for (dx, dy, fallen) in ((0.55, -1.15, False), (0.75, -1.3, True)):
        x, y = ex1 + dx, dy
        if fallen:
            g.box((x, y, 0.06), (0.24, 0.1, 0.1), M["rubber_boot"], rot=(0, 0, 30))
            g.box((x + 0.1, y + 0.05, 0.05), (0.1, 0.11, 0.08), M["rubber_boot"], rot=(0, 0, 30))
        else:
            g.box((x, y, 0.11), (0.09, 0.08, 0.22), M["rubber_boot"])
            g.box((x + 0.05, y, 0.03), (0.17, 0.08, 0.06), M["rubber_boot"])
            g.box((x, y, 0.22), (0.1, 0.09, 0.02), M["chalk"])
    g.box((ex1 + 0.32, -1.6, 0.17), (0.12, 0.12, 0.34), M["steel"])
    g.box((ex1 + 0.4, -1.6, 0.04), (0.32, 0.12, 0.08), M["steel"])

    # the neighbours' house (No. 12) joined on beyond our gable: a dark copy, a light upstairs
    nx0, nx1 = -26.5, -9.5
    g.box(((nx0 + nx1) / 2, hy0 + 0.15 + 0.05, eave / 2), (nx1 - nx0, 0.3, eave), M["brick"])
    pitched_roof(g, nx0, nx1, hy0, hy1, eave, ridge, M, along="x", courses=False)
    for (x, z, lit) in ((-12.0, 1.5, False), (-16.0, 1.5, False), (-12.0, 3.95, True), (-16.0, 3.95, False), (-21.0, 3.95, False), (-21.0, 1.5, False)):
        window(g, "xz", (x, hy0 - 0.0, z), 1.2, 1.2, M, M["lit_dim"] if lit else M["glass"], curtains=lit)
    g.box((-18.0, -2.0, 1.4), (5.0, 0.3, 2.8), M["brick"])
    holed_wall(g, "y", -9.7, -12.5, 4.0, 0.0, 1.85, 0.12, [], M["fence"])                          # the fence between
    return g


# ---------------------------------------------------------------- G1: the garden

def close_board(g, a, b, y_or_x, M, h=1.8, axis="x", post_every=1.83):
    """A close-board fence: concrete posts, gravel boards, rails, feather-edge boards."""
    L = b - a
    n = max(1, int(L / post_every + 0.5))
    step = L / n
    for k in range(n + 1):
        u = a + k * step
        c = (u, y_or_x, h / 2 + 0.05) if axis == "x" else (y_or_x, u, h / 2 + 0.05)
        g.box(c, (0.1, 0.1, h + 0.1), M["concrete"])
    for k in range(n):
        u0 = a + k * step + 0.05
        u1 = u0 + step - 0.1
        boards = int((u1 - u0) / 0.1)
        for j in range(boards):
            u = u0 + (j + 0.5) * (u1 - u0) / boards
            c = (u, y_or_x, 0.15 + h / 2) if axis == "x" else (y_or_x, u, 0.15 + h / 2)
            s = (0.1, 0.02, h - 0.15) if axis == "x" else (0.02, 0.1, h - 0.15)
            g.box(c, s, M["post"] if j % 7 == 3 else M["fence"])
        for z in (0.12, h * 0.35, h - 0.25):
            c = ((u0 + u1) / 2, y_or_x + 0.04, z) if axis == "x" else (y_or_x + 0.04, (u0 + u1) / 2, z)
            s = (u1 - u0, 0.04, 0.24 if z < 0.2 else 0.08) if axis == "x" else (0.04, u1 - u0, 0.24 if z < 0.2 else 0.08)
            g.box(c, s, M["concrete"] if z < 0.2 else M["fence"])


def picket(g, x, y0, y1, M, h=1.05, skip=None):
    """A painted picket fence across the garden's end (y0..y1 at x): pointed pickets, two rails."""
    n = int((y1 - y0) / 0.13)
    for k in range(n):
        y = y0 + (k + 0.5) * (y1 - y0) / n
        if skip and skip(y):
            continue
        hh = h + 0.03 * math.sin(k * 1.7)
        g.box((x, y, hh / 2), (0.02, 0.085, hh), M["picket"])
        g.cyl((x, y, hh + 0.04), 0.06, 0.08, M["picket"], rot=(0, 90, 0), n=4, r2=0.0)
    for z in (0.25, h - 0.2):
        for (a, b) in ((y0, -1.0), (1.0, y1)) if skip else ((y0, y1),):
            g.box((x + 0.03, (a + b) / 2, z), (0.03, b - a, 0.07), M["picket"])
    for y in [y0 + k * 2.4 for k in range(int((y1 - y0) / 2.4) + 1)]:
        if skip and skip(y):
            continue
        g.box((x + 0.05, y, h / 2 + 0.05), (0.08, 0.08, h + 0.1), M["post"])


def swing_frame(g, M):
    """Maya's swing set: two A-frames of steel tube, the top bar, hangers and bolts, ground anchors,
    the still seat on its chains (the moving seat is the Swing object)."""
    x0, x1, y, top = SWING_X - 1.7, SWING_X + 1.7, SWING_Y, SWING_TOP
    for x in (x0, x1):
        for s in (-1, 1):
            limb(g, (x, y + s * 0.95, -0.05), (x, y + s * 0.03, top), 0.032, 0.032, M["play_pale"], n=8)
            g.box((x, y + s * 0.98, 0.01), (0.12, 0.22, 0.03), M["steel"])                     # the anchor plate
            bolt(g, (x, y + s * 0.98, 0.03), M["steel"], axis="z")
        g.box((x, y, top - 0.02), (0.12, 0.16, 0.14), M["play_dark"])                          # the corner clamp
        for s in (-1, 1):
            bolt(g, (x, y + s * 0.09, top - 0.02), M["steel"], axis="y")
        limb(g, (x, y - 0.52, 1.0), (x, y + 0.52, 1.0), 0.018, 0.018, M["play_pale"], n=6)      # the brace
    limb(g, (x0 - 0.1, y, top + 0.02), (x1 + 0.1, y, top + 0.02), 0.04, 0.04, M["play_pale"], n=10)
    for hx in (SWING_X - 0.8 - 0.22, SWING_X - 0.8 + 0.22, SWING_X + 0.8 - 0.22, SWING_X + 0.8 + 0.22):
        g.box((hx, y, top - 0.04), (0.05, 0.1, 0.07), M["steel"])
    # the other swing: still
    sx = SWING_X + 0.8
    for dx in (-0.22, 0.22):
        chain(g, (sx + dx, y, top - 0.07), (sx + dx, y, 0.5), M["steel_mid"])
    g.box((sx, y, 0.47), (0.46, 0.17, 0.05), M["play_dark"])
    for dx in (-0.21, 0.21):
        g.box((sx + dx, y, 0.51), (0.04, 0.05, 0.05), M["steel"])
    # worn hollows in the grass under the seats
    for hx in (SWING_X - 0.8, sx):
        slab(g, [(hx + math.cos(a) * 0.45, y + math.sin(a) * 0.3) for a in [k / 12 * math.tau for k in range(12)]], -0.004, 0.004, M["soil"])


def build_swing(coll, M):
    """The swing seat Maya was on: its chains and the seat, hanging from (SWING_X - 0.8, SWING_Y,
    SWING_TOP). Its origin is that pivot: the game swings it."""
    g = S.Group("Swing")
    x, y = SWING_X - 0.8, SWING_Y
    for dx in (-0.22, 0.22):
        chain(g, (x + dx, y, SWING_TOP - 0.07), (x + dx, y, 0.52), M["steel_mid"])
        g.box((x + dx, y, 0.53), (0.04, 0.05, 0.06), M["steel"])
    g.box((x, y, 0.49), (0.46, 0.17, 0.045), M["play_dark"])
    g.box((x, y - 0.085, 0.49), (0.46, 0.01, 0.05), M["play_pale"])
    ob = g.finish(coll)
    set_origin(ob, (x, y, SWING_TOP))
    return ob


def set_origin(ob, p):
    """Move an object's origin to p without moving its mesh (a pivot for the game)."""
    p = Vector(p)
    ob.data.transform(Matrix.Translation(-p))
    ob.location = p


def small_slide(g, M):
    """Maya's own little slide, behind the swings: a ladder, a platform with rails, a plastic chute."""
    x, y = 21.8, 6.6
    for (dx, dy) in ((-0.3, -0.3), (0.3, -0.3), (-0.3, 0.3), (0.3, 0.3)):
        g.box((x + dx, y + dy, 0.65), (0.06, 0.06, 1.3), M["play_pale"])
    g.box((x, y, 1.15), (0.7, 0.7, 0.05), M["play_dark"])
    for s in (-1, 1):
        limb(g, (x + s * 0.3, y + 0.3, 1.15), (x + s * 0.3, y + 0.3, 1.75), 0.02, 0.02, M["play_pale"], n=6)
        limb(g, (x + s * 0.3, y + 0.3, 1.75), (x + s * 0.3, y - 0.3, 1.75), 0.02, 0.02, M["play_pale"], n=6)
        limb(g, (x + s * 0.3, y + 0.3, 1.15), (x + s * 0.32, y + 1.25, 0.0), 0.025, 0.025, M["play_pale"], n=6)
    for k in range(4):
        t = (k + 1) / 5
        g.box((x, y + 0.3 + 0.95 * t, 1.15 * (1 - t)), (0.6, 0.05, 0.03), M["play_dark"])
    # the chute: down towards the camera
    a, b = Vector((x, y - 0.35, 1.15)), Vector((x, y - 2.4, 0.2))
    L = (b - a).length
    ang = math.degrees(math.atan2(a.z - b.z, a.y - b.y))
    g.box(((a + b) / 2)[:], (0.46, L, 0.03), M["chute"], rot=(ang, 0, 0))
    for s in (-1, 1):
        g.box(((a + b) / 2 + Vector((s * 0.24, 0, 0.08)))[:], (0.03, L, 0.18), M["play_pale"], rot=(ang, 0, 0))
    g.box((x, y - 2.65, 0.18), (0.46, 0.4, 0.03), M["chute"])


def sandpit(g, M):
    x, y = 31.0, 6.0
    for s in (-1, 1):
        g.box((x + s * 1.0, y, 0.12), (0.08, 2.08, 0.24), M["timber_y"])
        g.box((x, y + s * 1.0, 0.12), (2.0, 0.08, 0.24), M["timber"])
    for (dx, dy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        g.box((x + dx, y + dy, 0.15), (0.14, 0.14, 0.3), M["timber"])                          # the corner seats
    ground(g, x - 0.96, x + 0.96, y - 0.96, y + 0.96, M["sand"], 0.12, h=lambda a, b: 0.16, amp=0.05, flat=0.0)
    blob(g, (x - 0.3, y + 0.2, 0.2), 0.22, M["sand"], s=(1, 1, 0.8), rough=0.2)                 # her sandcastle, half kicked
    blob(g, (x - 0.1, y + 0.45, 0.18), 0.15, M["sand"], s=(1, 1, 0.6), rough=0.4)
    g.cyl((x + 0.45, y - 0.3, 0.26), 0.1, 0.16, M["play_mid"], n=10, r2=0.13)                  # the bucket, the spade
    g.torus((x + 0.45, y - 0.3, 0.34), 0.13, 0.008, M["play_dark"], rot=(90, 0, 0), n1=10, n2=4)
    g.box((x + 0.1, y - 0.6, 0.2), (0.3, 0.06, 0.012), M["play_pale"], rot=(0, 15, 30))
    g.box((x - 0.15, y - 0.68, 0.22), (0.02, 0.02, 0.25), M["play_pale"], rot=(0, 65, 30))
    g.box((x + 1.6, y + 0.4, 0.5), (0.04, 2.0, 1.0), M["timber_y"], rot=(0, 18, 0))           # the lid, propped on the fence


def washing_line(g, M):
    """A rotary washing line: the pole, four arms, the lines in squares, her clothes pegged out."""
    x, y = 10.5, 6.8
    g.cyl((x, y, 0.95), 0.025, 1.9, M["steel_mid"], n=8)
    g.cyl((x, y, 0.05), 0.06, 0.1, M["concrete"], n=8)
    for k in range(4):
        a = k / 4 * math.tau + 0.3
        limb(g, (x, y, 1.65), (x + math.cos(a) * 1.4, y + math.sin(a) * 1.4, 1.9), 0.012, 0.012, M["steel_mid"], n=4)
        limb(g, (x, y, 1.3), (x + math.cos(a) * 0.5, y + math.sin(a) * 0.5, 1.7), 0.01, 0.01, M["steel_mid"], n=4)
    for r in (0.6, 1.0, 1.35):
        z = 1.7 + 0.18 * r
        for k in range(4):
            a0, a1 = k / 4 * math.tau + 0.3, (k + 1) / 4 * math.tau + 0.3
            g.beam((x + math.cos(a0) * r, y + math.sin(a0) * r, z), (x + math.cos(a1) * r, y + math.sin(a1) * r, z), 0.006, 0.006, M["cloth"])
    # the washing: a small dress, socks, a towel, a T-shirt (his)
    r = 1.35
    a0, a1 = 0.3 + math.pi, 0.3 + 1.5 * math.pi
    pa, pb = Vector((x + math.cos(a0) * r, y + math.sin(a0) * r, 1.94)), Vector((x + math.cos(a1) * r, y + math.sin(a1) * r, 1.94))
    d = (pb - pa)
    yaw = math.degrees(math.atan2(d.y, d.x))
    for (t, w, h, m) in ((0.2, 0.36, 0.5, M["cloth"]), (0.42, 0.1, 0.18, M["cloth_dark"]), (0.5, 0.1, 0.18, M["cloth_dark"]), (0.72, 0.55, 0.62, M["cloth_dark"])):
        p = pa.lerp(pb, t)
        g.box((p.x, p.y, p.z - h / 2), (w, 0.015, h), m, rot=(0, 0, yaw))
        g.box((p.x, p.y, p.z), (0.02, 0.03, 0.05), M["play_pale"], rot=(0, 0, yaw))
    p = pa.lerp(pb, 0.2)
    g.box((p.x, p.y, p.z - 0.62), (0.5, 0.016, 0.22), M["cloth"], rot=(0, 0, yaw))               # the dress's skirt
    r = 1.0
    a0, a1 = 0.3, 0.3 + 0.5 * math.pi
    pa, pb = Vector((x + math.cos(a0) * r, y + math.sin(a0) * r, 1.88)), Vector((x + math.cos(a1) * r, y + math.sin(a1) * r, 1.88))
    d = (pb - pa)
    yaw = math.degrees(math.atan2(d.y, d.x))
    p = pa.lerp(pb, 0.5)
    g.box((p.x, p.y, p.z - 0.3), (0.7, 0.015, 0.6), M["cloth"], rot=(0, 0, yaw))
    g.box((10.0, 5.2, 0.18), (0.55, 0.38, 0.36), M["play_pale"])                                 # the peg basket
    g.box((10.0, 5.2, 0.37), (0.5, 0.33, 0.02), M["cloth_dark"])


def shed(g, M):
    """The garden shed: weatherboard walls on a base, a pent felt roof, the ledged-and-braced door
    with its hasp and padlock, a four-pane window, the water butt, tools against it."""
    x0, x1, y0, y1 = 14.4, 18.4, 8.6, 11.4
    hf, hb = 2.3, 2.0
    g.box(((x0 + x1) / 2, (y0 + y1) / 2, 0.07), (x1 - x0 + 0.1, y1 - y0 + 0.1, 0.14), M["concrete"])
    door = (16.9, 17.75, 0.15, 2.0)
    win = (14.9, 16.2, 1.1, 1.8)
    holed_wall(g, "x", y0, x0, x1, 0.14, hf, 0.06, [door, win], M["timber"])
    g.box(((x0 + x1) / 2, y1, (0.14 + hb) / 2), (x1 - x0, 0.06, hb - 0.14), M["timber"])
    for x in (x0, x1):
        g.box((x, (y0 + y1) / 2, (0.14 + hb) / 2), (0.06, y1 - y0, hb - 0.14), M["timber_y"])
    for x in (x0, x1):                                                                           # the sloping tops of the sides
        tmp = bmesh.new()
        pts = [(y0, hb), (y1, hb), (y0, hf)]
        a = [tmp.verts.new((x - 0.03, yy, zz)) for yy, zz in pts]
        b = [tmp.verts.new((x + 0.03, yy, zz)) for yy, zz in pts]
        tmp.faces.new(a)
        tmp.faces.new(list(reversed(b)))
        for i in range(3):
            tmp.faces.new((a[i], a[(i + 1) % 3], b[(i + 1) % 3], b[i]))
        g._merge(tmp, M["timber_y"])
    for (cx, cz) in ((x0, 0), (x1, 0)):
        for yy in (y0, y1):
            g.box((cx, yy, 1.1), (0.08, 0.08, 2.2 if yy == y0 else 1.9), M["post"])              # the corner trims
    ang = math.degrees(math.atan2(hf - hb, y1 - y0))
    L = math.hypot(y1 - y0 + 0.5, hf - hb)
    g.box(((x0 + x1) / 2, (y0 + y1) / 2 - 0.0, (hf + hb) / 2 + 0.06), (x1 - x0 + 0.4, L, 0.05), M["gutter"], rot=(-ang, 0, 0))
    for k in range(5):                                                                           # the felt's battens
        x = x0 - 0.1 + k * (x1 - x0 + 0.2) / 4
        g.box((x, (y0 + y1) / 2, (hf + hb) / 2 + 0.1), (0.05, L, 0.04), M["gutter"], rot=(-ang, 0, 0))
    g.box(((x0 + x1) / 2, y0 - 0.22, hf + 0.03), (x1 - x0 + 0.4, 0.04, 0.16), M["post"])           # the fascia board
    # the door: boards, ledges, the brace, hinges, the hasp and padlock
    dx0, dx1, _, dz1 = door
    for k in range(6):
        g.box((dx0 + (k + 0.5) * (dx1 - dx0) / 6, y0 - 0.02, 1.08), ((dx1 - dx0) / 6 - 0.008, 0.03, 1.85), M["timber"])
    for z in (0.45, 1.75):
        g.box(((dx0 + dx1) / 2, y0 - 0.05, z), (dx1 - dx0 - 0.06, 0.03, 0.12), M["post"])
        g.box((dx0 + 0.08, y0 - 0.07, z), (0.3, 0.02, 0.04), M["steel"])
    g.beam((dx0 + 0.08, y0 - 0.05, 0.5), (dx1 - 0.08, y0 - 0.05, 1.7), 0.1, 0.03, M["post"], up=Vector((0, 1, 0)))
    g.box((dx1 - 0.06, y0 - 0.07, 1.1), (0.16, 0.02, 0.04), M["steel"])
    g.box((dx1 - 0.02, y0 - 0.1, 1.04), (0.05, 0.03, 0.07), M["chrome"])
    # the window: four panes, cobwebbed dark, a sill
    wx0, wx1, wz0, wz1 = win
    g.box(((wx0 + wx1) / 2, y0 + 0.01, (wz0 + wz1) / 2), (wx1 - wx0, 0.02, wz1 - wz0), M["glass"])
    for u in (wx0, (wx0 + wx1) / 2, wx1):
        g.box((u, y0 - 0.02, (wz0 + wz1) / 2), (0.05, 0.04, wz1 - wz0 + 0.05), M["post"])
    for w in (wz0, (wz0 + wz1) / 2, wz1):
        g.box(((wx0 + wx1) / 2, y0 - 0.02, w), (wx1 - wx0 + 0.05, 0.04, 0.05), M["post"])
    g.box(((wx0 + wx1) / 2, y0 - 0.08, wz0 - 0.04), (wx1 - wx0 + 0.15, 0.12, 0.04), M["post"])
    # the water butt on blocks, its gutter; a rake and a spade, flower pots stacked
    g.box((x1 - 0.2, y0 - 0.24, hb + 0.1), (0.04, 0.1, 0.1), M["gutter"])
    g.box((x1 + 0.4, y0 + 0.5, 0.12), (0.5, 0.5, 0.24), M["concrete"])
    g.cyl((x1 + 0.4, y0 + 0.5, 0.67), 0.28, 0.86, M["play_dark"], n=14)
    g.cyl((x1 + 0.4, y0 + 0.5, 1.12), 0.3, 0.04, M["play_dark"], n=14)
    limb(g, (x0 - 0.12, y0 + 0.2, 0.0), (x0 - 0.06, y0 + 0.1, 1.6), 0.015, 0.015, M["post"], n=5)
    g.box((x0 - 0.06, y0 + 0.1, 1.62), (0.05, 0.4, 0.04), M["steel"])
    for k in range(9):
        g.box((x0 - 0.06, y0 - 0.08 + k * 0.045, 1.57), (0.012, 0.012, 0.09), M["steel"])
    limb(g, (x0 - 0.15, y0 + 1.0, 0.0), (x0 - 0.08, y0 + 0.9, 1.0), 0.016, 0.016, M["post"], n=5)
    g.box((x0 - 0.15, y0 + 1.0, 0.15), (0.03, 0.2, 0.28), M["steel_mid"], rot=(6, 0, 0))
    g.box((x0 - 0.08, y0 + 0.9, 1.04), (0.03, 0.14, 0.03), M["post"])
    for k in range(4):
        g.cyl((x1 + 0.35, y0 - 0.45, 0.08 + k * 0.07), 0.12, 0.16, M["play_mid"], n=10, r2=0.09)
    g.cyl((x1 + 0.8, y0 - 0.3, 0.1), 0.15, 0.2, M["play_mid"], n=10, r2=0.11)
    bush(g, (x1 + 0.8, y0 - 0.3, 0.15), 0.18, M)


def flower_bed(g, x0, x1, y0, y1, M, n=None, edging=True):
    """A bed of dark soil, edged with timber: low shrubs, tall stalks, a few pale flower heads left."""
    ground(g, x0, x1, y0, y1, M["soil"], 0.35, h=lambda a, b: 0.04, amp=0.04, flat=0.0, seed=x0)
    if edging:
        g.box(((x0 + x1) / 2, y0, 0.06), (x1 - x0, 0.04, 0.12), M["timber"])
    n = n or int((x1 - x0) * 1.4)
    for _ in range(n):
        x, y = rnd.uniform(x0 + 0.2, x1 - 0.2), rnd.uniform(y0 + 0.15, y1 - 0.15)
        kind = rnd.random()
        if kind < 0.45:
            bush(g, (x, y, 0.02), rnd.uniform(0.18, 0.38), M)
        elif kind < 0.8:
            for k in range(3):
                tx, ty = x + rnd.uniform(-0.12, 0.12), y + rnd.uniform(-0.12, 0.12)
                hh = rnd.uniform(0.5, 1.0)
                limb(g, (tx, ty, 0.0), (tx + rnd.uniform(-0.08, 0.08), ty, hh), 0.008, 0.005, M["reed"], n=3)
                g.cyl((tx, ty, hh), 0.035, 0.05, M["flower"], n=6)
        else:
            for k in range(4):
                a = rnd.uniform(0, math.tau)
                g.box((x + math.cos(a) * 0.08, y + math.sin(a) * 0.08, 0.12), (0.2, 0.03, 0.006), M["foliage"], rot=(rnd.uniform(-30, 30), 20, math.degrees(a)))
            g.cyl((x, y, 0.3), 0.05, 0.04, M["flower"], n=6)


def build_garden(coll, M):
    g = S.Group("ParkGarden")
    # the patio: Maya's chalk on it (the hopscotch, a family of three under a sun)
    hop = [(4.8, -2.15, "1"), (5.4, -2.15, "2"), (6.0, -2.45, "3"), (6.0, -1.85, "4"), (6.6, -2.15, "5"), (7.2, -2.45, "6"), (7.2, -1.85, "7"), (7.8, -2.15, "8")]
    lines = []
    for (x, y, t) in hop:
        h = 0.3 if t in "3467" else 0.6
        for (a, b) in (((x - 0.3, y - h / 2), (x + 0.3, y - h / 2)), ((x - 0.3, y + h / 2), (x + 0.3, y + h / 2)),
                       ((x - 0.3, y - h / 2), (x - 0.3, y + h / 2)), ((x + 0.3, y - h / 2), (x + 0.3, y + h / 2))):
            mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            L = math.hypot(b[0] - a[0], b[1] - a[1]) / 2
            lines.append((mx, my, 0.024, 0.012, L, math.atan2(b[1] - a[1], b[0] - a[0])))
        flat_text(g, t, (x, y - 0.1, 0.024), 0.22, M["chalk"], yaw=90)
    # the drawing: a sun with rays, three stick figures holding hands (a tall one, a middle, a small one)
    sx, sy = 3.0, 2.6
    for k in range(10):
        a = k / 10 * math.tau
        lines.append((sx + math.cos(a) * 0.32, sy + math.sin(a) * 0.32, 0.024, 0.012, 0.09, a))
    for k in range(16):
        a = k / 16 * math.tau
        lines.append((sx + math.cos(a) * 0.17, sy + math.sin(a) * 0.17, 0.024, 0.01, 0.035, a + math.pi / 2))
    for (fx, fh) in ((1.6, 0.7), (2.1, 0.62), (2.5, 0.4)):
        fy = 1.6
        lines.append((fx, fy + fh * 0.45, 0.024, 0.01, fh * 0.3, math.pi / 2))
        g.torus((fx, fy + fh * 0.85, 0.024), fh * 0.12, 0.01, M["chalk"], n1=10, n2=3)
        lines.append((fx - 0.06, fy + 0.07, 0.024, 0.01, fh * 0.17, math.pi / 2 - 0.35))
        lines.append((fx + 0.06, fy + 0.07, 0.024, 0.01, fh * 0.17, math.pi / 2 + 0.35))
    lines.append((2.05, 1.6 + 0.48, 0.024, 0.01, 0.5, 0.0))
    flecks(g, [(x, y, z, w, l, a) for (x, y, z, w, l, a) in lines], M["chalk"])
    flat_text(g, "MAYA", (2.1, 1.15, 0.024), 0.2, M["chalk"])
    g.box((2.9, 1.0, 0.03), (0.08, 0.02, 0.02), M["chalk"], rot=(0, 0, 25))                      # the stub of chalk
    g.box((3.1, 0.95, 0.03), (0.06, 0.02, 0.02), M["play_mid"], rot=(0, 0, -40))
    # the patio furniture: a table under its folded parasol, two chairs, the barbecue under its cover
    tx, ty = 5.6, 2.6
    g.cyl((tx, ty, 0.72), 0.5, 0.03, M["steel_mid"], n=16)
    for k in range(3):
        a = k / 3 * math.tau
        limb(g, (tx, ty, 0.7), (tx + math.cos(a) * 0.45, ty + math.sin(a) * 0.45, 0.0), 0.015, 0.015, M["steel"], n=4)
    g.cyl((tx, ty, 1.5), 0.02, 2.4, M["steel_mid"], n=6)
    g.cyl((tx, ty, 2.05), 0.13, 1.1, M["cloth_dark"], n=8, r2=0.05)
    for (cx, cy, yaw) in ((tx - 0.8, ty - 0.1, 20), (tx + 0.75, ty + 0.2, 200)):
        def chair(t):
            t.box((0, 0, 0.45), (0.45, 0.45, 0.03), M["steel_mid"])
            t.box((-0.22, 0, 0.75), (0.03, 0.45, 0.6), M["steel_mid"], rot=(0, -10, 0))
            for (dx, dy) in ((-0.2, -0.2), (0.2, -0.2), (-0.2, 0.2), (0.2, 0.2)):
                t.box((dx, dy, 0.22), (0.025, 0.025, 0.45), M["steel"])
        sub(g, chair, (cx, cy, 0), yaw)
    g.cyl((1.4, 3.3, 0.55), 0.3, 0.6, M["cloth_dark"], n=14, r2=0.22)
    g.cyl((1.4, 3.3, 0.9), 0.22, 0.12, M["cloth_dark"], n=14, r2=0.05)
    for k in range(3):
        a = k / 3 * math.tau
        limb(g, (1.4, 3.3, 0.3), (1.4 + math.cos(a) * 0.32, 3.3 + math.sin(a) * 0.32, 0.0), 0.012, 0.012, M["steel"], n=4)
    # the wheelie bins at the side of the house
    for k, bx in enumerate((6.7, 7.4)):
        g.box((bx, 3.25, 0.5), (0.58, 0.72, 1.0), M["play_dark"] if k else M["steel_mid"])
        g.box((bx, 3.22, 1.03), (0.62, 0.78, 0.05), M["play_dark"] if k else M["steel_mid"])
        g.cyl((bx, 3.65, 0.12), 0.1, 0.06, M["tyre"], rot=(0, 90, 0), n=10)
    # the stepping-stone path from the patio across the lawn to the shed
    for k in range(13):
        t = k / 12
        x, y = 8.6 + t * 8.6, 2.0 + t * 5.8 + 0.4 * math.sin(t * 3.0)
        if abs(y) < 1.0:
            continue
        g.cyl((x, y, 0.012), rnd.uniform(0.24, 0.3), 0.03, M["slab"], n=10)
    # the lawn's edge: a mown strip by the fence, edging stones round the beds
    washing_line(g, M)
    shed(g, M)
    swing_frame(g, M)
    small_slide(g, M)
    sandpit(g, M)
    # her toys: a ball in the long grass, a small bike on its side, a skipping rope, a toy pram
    g.cyl((32.2, -2.8, 0.11), 0.11, 0.2, M["play_pale"], n=12)
    g.torus((32.2, -2.8, 0.11), 0.095, 0.02, M["play_mid"], n1=12, n2=4)
    for (wx, wy) in ((23.4, -2.6), (24.1, -2.75)):
        g.torus((wx, wy, 0.04), 0.17, 0.025, M["tyre"], n1=16, n2=5)
        g.cyl((wx, wy, 0.045), 0.02, 0.05, M["steel"], n=6)
    limb(g, (23.4, -2.6, 0.05), (24.1, -2.75, 0.06), 0.02, 0.02, M["play_mid"], n=5)
    limb(g, (23.6, -2.65, 0.06), (23.65, -2.4, 0.12), 0.015, 0.015, M["play_mid"], n=4)
    g.box((23.65, -2.38, 0.13), (0.22, 0.03, 0.03), M["play_dark"])
    for k in range(14):
        a = k / 14 * math.pi * 1.6
        g.box((19.5 + math.cos(a) * 0.5, -2.9 + math.sin(a) * 0.3, 0.012), (0.08, 0.015, 0.015), M["play_pale"], rot=(0, 0, math.degrees(a) + 90))
    # fairy lights strung along the back fence over her play area, sagging between the posts;
    # solar lights on stakes along the stepping stones
    for k in range(9):
        xa, xb = 18.0 + k * 1.83, 18.0 + (k + 1) * 1.83
        prev = None
        for j in range(9):
            t = j / 8
            p = Vector((xa + (xb - xa) * t, 12.45, 1.95 - 0.28 * math.sin(t * math.pi)))
            if prev is not None:
                g.beam(prev[:], p[:], 0.006, 0.006, M["steel"])
            if 0 < j < 8 and j % 2 == 0:
                g.cyl((p.x, p.y - 0.02, p.z - 0.04), 0.022, 0.05, M["fairy"], n=6)
            prev = p
    for k in range(6):
        t = (k + 0.5) / 6
        x, y = 8.6 + t * 8.6 + 0.5, 2.0 + t * 5.8 + 0.4 * math.sin(t * 3.0) + 0.45
        g.cyl((x, y, 0.15), 0.012, 0.3, M["steel"], n=4)
        g.cyl((x, y, 0.33), 0.045, 0.06, M["fairy"], n=8)
        g.cyl((x, y, 0.37), 0.05, 0.02, M["steel"], n=8)
    # the beds: along the back fence, the house's side, the front
    flower_bed(g, 8.0, 14.0, 11.4, 12.3, M)
    flower_bed(g, 19.0, 33.7, 11.2, 12.3, M)
    flower_bed(g, 1.0, 6.2, 3.6, 3.95, M, n=4, edging=False)
    flower_bed(g, 9.0, 21.0, -4.6, -3.4, M)
    flower_bed(g, 25.5, 33.5, -4.6, -3.4, M)
    # an apple tree by the shed, a small cherry by the fence end, a rose arch on the path
    tree(g, 11.8, 10.2, 6.8, M, leafy=0.4, mains=5)
    tree(g, 32.0, 9.8, 5.2, M, leafy=0.5, mains=4)
    for k in range(5):                                                                           # windfall apples
        g.cyl((11.8 + rnd.uniform(-1.6, 1.6), 9.4 + rnd.uniform(-1.0, 1.0), 0.04), 0.04, 0.07, M["play_mid"], n=8)
    for s in (-1, 1):
        g.box((8.9 + s * 0.55, 3.0, 1.1), (0.06, 0.06, 2.2), M["steel"])
    for k in range(9):
        a = k / 8 * math.pi
        g.box((8.9 + math.cos(a) * 0.55, 3.0, 2.2 + math.sin(a) * 0.35), (0.12, 0.05, 0.04), M["steel"], rot=(0, -math.degrees(a) + 90, 0))
    for k in range(10):
        bush(g, (8.9 + rnd.uniform(-0.65, 0.65), 3.0 + rnd.uniform(-0.1, 0.1), rnd.uniform(0.3, 2.3)), rnd.uniform(0.1, 0.18), M)
    # the fences: close-board along the back, the low picket fence across the garden's end, its
    # gate broken (one leaf torn off and thrown on the grass, the other hanging from one hinge)
    close_board(g, 7.6, GATE_X, 12.6, M)
    picket(g, GATE_X, -12.0, 12.6, M, skip=lambda y: -1.0 < y < 1.0)
    for s in (-1, 1):
        g.box((GATE_X, s * 1.05, 0.65), (0.12, 0.12, 1.3), M["post"])
        g.cyl((GATE_X, s * 1.05, 1.33), 0.08, 0.06, M["post"], n=4, r2=0.0)
    def gate_leaf(t):
        for k in range(7):
            t.box((0, -0.42 + k * 0.14, 0.52), (0.02, 0.085, 1.0), M["picket"])
            t.cyl((0, -0.42 + k * 0.14, 1.06), 0.055, 0.08, M["picket"], rot=(0, 90, 0), n=4, r2=0.0)
        for z in (0.25, 0.85):
            t.box((0.03, 0, z), (0.03, 0.95, 0.07), M["picket"])
        t.beam((0.04, -0.45, 0.25), (0.04, 0.45, 0.85), 0.03, 0.06, M["picket"], up=Vector((1, 0, 0)))
        t.box((0.05, 0.4, 0.85), (0.03, 0.2, 0.03), M["steel"])
    sub(g, gate_leaf, (GATE_X + 0.4, 1.35, 0.05), yaw=-58, tilt=(0, 0, 0))                       # hanging open, askew
    sub(g, gate_leaf, (GATE_X + 1.6, -2.3, 0.04), yaw=25, tilt=(0, 86, 0))                        # torn off, in the grass
    g.box((GATE_X + 0.03, -1.0, 0.85), (0.04, 0.2, 0.05), M["steel"], rot=(0, 30, 0))           # the broken latch
    for k in range(6):                                                                           # splinters
        g.box((GATE_X + 0.5 + rnd.uniform(0, 1.5), rnd.uniform(-2.6, -1.2), 0.01), (0.12, 0.02, 0.01), M["picket"], rot=(0, 0, rnd.uniform(0, 180)))
    # long grass along the fences, fallen leaves, worm casts and puddles on the wet lawn
    tufts(g, 7.6, 33.8, 12.1, 12.5, 40, M)
    tufts(g, 33.3, 33.9, -6.0, 12.4, 25, M)
    for (x, y, rx, ry) in ((13.5, -1.6, 0.6, 0.35), (21.0, 1.6, 0.45, 0.3), (29.5, -1.5, 0.7, 0.4), (16.5, 4.4, 0.5, 0.3), (3.0, -1.4, 0.5, 0.3)):
        puddle(g, x, y, rx, ry, M, z=0.008 if x > 7 else 0.03)
    leaves(g, 7.0, 34.0, -6.0, 12.4, 700, M)
    leaves(g, 0.3, 7.0, -3.0, 3.8, 120, M, zf=lambda x, y: 0.02)
    return g.finish(coll)


def build_garden_ground(coll, M):
    g = S.Group("ParkGardenGround")
    g.box((3.6, 0.3, 0.0), (7.2, 7.4, 0.04), M["slab"])                                          # the patio
    g.box((7.25, 0.3, 0.0), (0.12, 7.4, 0.06), M["kerb"])
    ground(g, 7.3, GATE_X, -12.0, 12.7, M["lawn"], 0.5, amp=0.05, seed=1.0)
    ground(g, -9.0, 7.3, -12.0, -3.4, M["lawn"], 0.8, amp=0.05, seed=2.0)
    ground(g, -9.6, 0.0, -3.4, 4.0, M["lawn"], 1.0, amp=0.0)
    return g.finish(coll)


# ---------------------------------------------------------------- G2: the playground

def park_lamp(g, x, y, M, h=LAMP_H):
    """A park lamp: a fluted iron post on a base, a crossbar, a lantern with a lit globe."""
    g.cyl((x, y, 0.25), 0.12, 0.5, M["steel"], n=10, r2=0.08)
    g.cyl((x, y, h / 2), 0.055, h, M["steel"], n=8, r2=0.04)
    g.torus((x, y, 0.55), 0.075, 0.018, M["steel"], n1=10, n2=4)
    g.box((x, y, h - 0.75), (0.5, 0.03, 0.03), M["steel"])                                       # the ladder bar
    for s in (-1, 1):
        g.cyl((x + s * 0.25, y, h - 0.75), 0.025, 0.05, M["steel"], n=6, r2=0.0)
    g.cyl((x, y, h + 0.02), 0.09, 0.08, M["steel"], n=8)
    g.cyl((x, y, h + 0.25), 0.16, 0.4, M["globe"], n=12, r2=0.21)
    for k in range(4):
        a = k / 4 * math.tau + math.pi / 4
        limb(g, (x + math.cos(a) * 0.15, y + math.sin(a) * 0.15, h + 0.05), (x + math.cos(a) * 0.22, y + math.sin(a) * 0.22, h + 0.46), 0.012, 0.012, M["steel"], n=4)
    g.cyl((x, y, h + 0.52), 0.28, 0.14, M["steel"], n=12, r2=0.06)
    g.cyl((x, y, h + 0.66), 0.03, 0.12, M["steel"], n=6, r2=0.0)


def park_bench(g, x, y, yaw, M, plaque=True):
    def b(t):
        for k in range(3):                                                                       # seat slats
            t.box((0, -0.18 + k * 0.15, 0.45), (1.8, 0.11, 0.04), M["timber"])
        for k in range(3):                                                                       # back slats
            t.box((0, 0.25, 0.62 + k * 0.13), (1.8, 0.035, 0.1), M["timber"], rot=(-12, 0, 0))
        for s in (-1, 1):                                                                        # the cast iron ends
            t.box((s * 0.78, 0.0, 0.22), (0.05, 0.08, 0.44), M["steel"])
            t.box((s * 0.78, 0.0, 0.43), (0.05, 0.52, 0.04), M["steel"])
            t.box((s * 0.78, -0.2, 0.2), (0.05, 0.06, 0.4), M["steel"])
            t.box((s * 0.78, 0.26, 0.6), (0.05, 0.05, 0.7), M["steel"], rot=(-12, 0, 0))
            t.box((s * 0.82, -0.04, 0.66), (0.05, 0.42, 0.04), M["steel"])                    # the arm
            for z in (0.45, 0.66):
                bolt(t, (s * 0.78, -0.18, z), M["steel"], axis="x", r=0.015)
        if plaque:
            t.box((0, 0.21, 0.83), (0.18, 0.01, 0.05), M["chrome"], rot=(-12, 0, 0))
    sub(g, b, (x, y, 0), yaw)


def litter_bin(g, x, y, M):
    g.cyl((x, y, 0.45), 0.24, 0.9, M["steel"], n=14)
    for z in (0.15, 0.75):
        g.torus((x, y, z), 0.245, 0.012, M["steel_mid"], n1=14, n2=4)
    g.cyl((x, y, 0.95), 0.27, 0.1, M["steel_mid"], n=14, r2=0.2)
    g.box((x, y - 0.22, 0.8), (0.22, 0.06, 0.1), M["sign_ink"])
    g.box((x, y - 0.24, 0.55), (0.18, 0.01, 0.1), M["sign"])


def railings(g, x0, x1, y, M, h=1.8, spear=True, gap=0.14, axis="x", skip=None):
    """Steel railings: square posts, top and bottom rails, round bars with spear tops."""
    L = x1 - x0
    n = int(L / gap)
    for k in range(n + 1):
        u = x0 + k * L / n
        if skip and skip(u):
            continue
        c = (u, y, h / 2) if axis == "x" else (y, u, h / 2)
        g.cyl(c, 0.011, h, M["steel"], n=4)
        if spear:
            cc = (c[0], c[1], h + 0.05)
            g.cyl(cc, 0.022, 0.1, M["steel"], n=4, r2=0.0)
    posts = int(L / 2.5)
    for k in range(posts + 1):
        u = x0 + k * L / posts
        if skip and skip(u):
            continue
        c = (u, y, (h + 0.12) / 2) if axis == "x" else (y, u, (h + 0.12) / 2)
        g.box(c, (0.06, 0.06, h + 0.12), M["steel"])
        cc = (c[0], c[1], h + 0.15)
        g.cyl(cc, 0.045, 0.08, M["steel"], n=8)
    for z in (0.12, h - 0.15):
        if skip:
            u = x0
            while u < x1:
                v = u
                while v < x1 and not skip(v):
                    v += 0.1
                if v - u > 0.2:
                    c = ((u + v) / 2, y, z) if axis == "x" else (y, (u + v) / 2, z)
                    s = (v - u, 0.03, 0.04) if axis == "x" else (0.03, v - u, 0.04)
                    g.box(c, s, M["steel"])
                u = v + 0.1
        else:
            c = ((x0 + x1) / 2, y, z) if axis == "x" else (y, (x0 + x1) / 2, z)
            s = (L, 0.03, 0.04) if axis == "x" else (0.03, L, 0.04)
            g.box(c, s, M["steel"])


def climbing_frame(g, M):
    """The big climbing frame: two towers of square timber posts at two heights, joined by a chain
    bridge; a pitched roof on the tall tower, a rope net up one side, a climbing wall with holds up
    another, a fireman's pole, a ladder with rungs, rails and panels, bolts at every joint."""
    def post(x, y, h):
        g.box((x, y, h / 2), (0.11, 0.11, h), M["post"])
        g.box((x, y, h + 0.03), (0.13, 0.13, 0.06), M["play_dark"])
        for z in (0.5, h - 0.4):
            bolt(g, (x, y - 0.06, z), M["steel"], axis="y")
    def deck(x, y, w, d, z):
        g.box((x, y, z), (w, d, 0.05), M["deck"])
        for s in (-1, 1):
            g.box((x, y + s * (d / 2 - 0.05), z - 0.1), (w, 0.06, 0.16), M["post"])
            g.box((x + s * (w / 2 - 0.05), y, z - 0.1), (0.06, d, 0.16), M["post"])
    def rail(x0, y0, x1, y1, z, bars=True):
        g.beam((x0, y0, z + 0.9), (x1, y1, z + 0.9), 0.06, 0.06, M["play_pale"])
        g.beam((x0, y0, z + 0.45), (x1, y1, z + 0.45), 0.04, 0.04, M["play_pale"])
        if bars:
            n = int(math.hypot(x1 - x0, y1 - y0) / 0.12)
            for k in range(1, n):
                t = k / n
                g.cyl((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, z + 0.45), 0.012, 0.9, M["play_pale"], n=4)
    # tower A (tall, roofed): x 41.4..42.8, y 6.0..7.4, deck at 1.8
    ax0, ax1, ay0, ay1, az = 41.4, 42.8, 6.0, 7.4, 1.8
    for (x, y) in ((ax0, ay0), (ax1, ay0), (ax0, ay1), (ax1, ay1)):
        post(x, y, az + 1.9)
    deck((ax0 + ax1) / 2, (ay0 + ay1) / 2, ax1 - ax0, ay1 - ay0, az)
    rail(ax0, ay1, ax1, ay1, az)
    rail(ax0, ay0, ax0, ay1, az)
    # its roof: a pitched canopy, ridge along x, panels
    for s in (-1, 1):
        g.box(((ax0 + ax1) / 2, (ay0 + ay1) / 2 + s * 0.42, az + 2.15), (1.7, 0.95, 0.04), M["play_mid"], rot=(-s * 32, 0, 0))
    g.box(((ax0 + ax1) / 2, (ay0 + ay1) / 2, az + 2.42), (1.8, 0.08, 0.08), M["play_dark"])
    # a climbing wall up its front (-y): a sloping panel with holds
    g.box(((ax0 + ax1) / 2, ay0 - 0.55, az / 2), (1.3, 0.05, math.hypot(az, 1.0)), M["play_mid"], rot=(-29, 0, 0))
    for _ in range(14):
        t = rnd.uniform(0.1, 0.9)
        g.box(((ax0 + ax1) / 2 + rnd.uniform(-0.5, 0.5), ay0 - 1.0 + t * 0.95 - 0.04, az * t), (0.1, 0.06, 0.08), M["play_pale"], rot=(0, 0, rnd.uniform(0, 90)))
    # tower B (lower, open): x 44.6..46.0, deck at 1.2; the chain bridge between them
    bx0, bx1, by0, by1, bz = 44.6, 46.0, 6.0, 7.4, 1.2
    for (x, y) in ((bx0, by0), (bx1, by0), (bx0, by1), (bx1, by1)):
        post(x, y, bz + 1.1)
    deck((bx0 + bx1) / 2, (by0 + by1) / 2, bx1 - bx0, by1 - by0, bz)
    rail(bx1, by0, bx1, by1, bz)
    rail(bx0, by1, bx1, by1, bz)
    for k in range(9):                                                                           # the bridge's planks, hung on chains
        t = (k + 0.5) / 9
        x = ax1 + (bx0 - ax1) * t
        z = az + (bz - az) * t - 0.12 * math.sin(t * math.pi)
        g.box((x, (ay0 + ay1) / 2, z), (0.16, 0.9, 0.04), M["deck"])
    for s in (-1, 1):
        yy = (ay0 + ay1) / 2 + s * 0.45
        for k in range(10):
            t = k / 10
            p0 = Vector((ax1 + (bx0 - ax1) * t, yy, az + (bz - az) * t - 0.12 * math.sin(t * math.pi) + 0.02))
            g.torus(p0[:], 0.025, 0.006, M["steel_mid"], rot=(90, 0, 90 * (k % 2)), n1=6, n2=4)
        g.beam((ax1, yy, az + 0.9), (bx0, yy, bz + 0.9), 0.025, 0.025, M["rope"])
        for k in range(1, 5):
            t = k / 5
            x = ax1 + (bx0 - ax1) * t
            g.beam((x, yy, az + (bz - az) * t), (x, yy, az + (bz - az) * t + 0.85), 0.015, 0.015, M["rope"])
    # the rope net up the back of tower B
    for k in range(7):
        t = k / 6
        g.beam((bx0 + (bx1 - bx0) * t, by1 + 1.3, 0.0), (bx0 + (bx1 - bx0) * t, by1, bz), 0.02, 0.02, M["rope"])
    for k in range(6):
        t = k / 6
        g.beam((bx0, by1 + 1.3 * (1 - t), bz * t), (bx1, by1 + 1.3 * (1 - t), bz * t), 0.02, 0.02, M["rope"])
    g.box(((bx0 + bx1) / 2, by1 + 1.3, 0.05), (1.5, 0.1, 0.1), M["post"])
    # the fireman's pole off tower B, a ladder up tower A's side
    g.cyl((bx1 + 0.5, by0 + 0.3, 1.25), 0.025, 2.5, M["chrome"], n=8)
    g.beam((bx1, by0 + 0.3, 2.3), (bx1 + 0.5, by0 + 0.3, 2.45), 0.03, 0.03, M["chrome"])
    for s in (-1, 1):
        limb(g, (ax0 - 0.9, ay0 + 0.7 + s * 0.25, 0.0), (ax0, ay0 + 0.7 + s * 0.25, az), 0.03, 0.03, M["post"], n=6)
    for k in range(6):
        t = (k + 1) / 7
        g.cyl((ax0 - 0.9 * (1 - t), ay0 + 0.7, az * t), 0.018, 0.5, M["play_pale"], rot=(90, 0, 0), n=6)
    # a tube slide off tower A's back, curling round to the ground
    for k in range(12):
        t = k / 11
        a = t * math.pi * 1.1
        c = (ax0 + 0.7 - 1.1 * math.sin(a), ay1 + 0.8 + 1.1 * (1 - math.cos(a)) * 0.6, az - t * (az - 0.35))
        g.cyl(c, 0.38, 0.32, M["play_mid"], n=12, rot=(math.degrees(0.4), 0, math.degrees(a)))
    # the bark under it all, a sign: AGES 5-12
    sign_board(g, 40.4, 4.4, "AGES 5-12", M, h=1.2, w=0.6, ht=0.25, size=0.1)


def tall_slide(g, M):
    """The tall slide (where she hid): a tower of steel posts up to a platform 2.6 m up, a steep
    ladder behind with handrails, a hooped cage, a long steel chute coming down towards x+, and
    under the platform the dark gap: boarded on three sides, open to the front, a child's den."""
    x, y, z = SLIDE_X, SLIDE_Y, 2.6
    w = 1.2
    for (dx, dy) in ((-w / 2, -w / 2), (w / 2, -w / 2), (-w / 2, w / 2), (w / 2, w / 2)):
        limb(g, (x + dx, y + dy, -0.1), (x + dx, y + dy, z + 1.2), 0.05, 0.05, M["play_pale"], n=8)
        g.cyl((x + dx, y + dy, z + 1.22), 0.06, 0.05, M["play_dark"], n=8)
        for zz in (0.5, 1.5, z - 0.1):
            bolt(g, (x + dx, y + dy - 0.06, zz), M["steel"])
    g.box((x, y, z), (w + 0.1, w + 0.1, 0.06), M["steel_mid"])
    for k in range(int((w + 0.1) / 0.05)):                                                      # the grating
        g.box((x - (w + 0.1) / 2 + k * 0.05, y, z + 0.035), (0.012, w + 0.1, 0.01), M["steel"])
    # the platform's guard: hoops and bars, the roof
    for (a, b) in (((x - w / 2, y - w / 2), (x - w / 2, y + w / 2)), ((x - w / 2, y + w / 2), (x + w / 2, y + w / 2)), ((x - w / 2, y - w / 2), (x + w / 2, y - w / 2))):
        for zz in (z + 0.5, z + 1.0):
            limb(g, (a[0], a[1], zz), (b[0], b[1], zz), 0.022, 0.022, M["play_pale"], n=6)
        n = 8
        for k in range(1, n):
            t = k / n
            g.cyl((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, z + 0.5), 0.012, 1.0, M["play_pale"], n=4)
    for s in (-1, 1):
        g.box((x, y + s * 0.38, z + 1.5), (w + 0.4, 0.9, 0.04), M["play_dark"], rot=(-s * 25, 0, 0))
    # the den under the platform: boards on three sides, open at the front (-y); dark inside
    for k in range(9):
        g.box((x - w / 2 + 0.07 + k * (w - 0.14) / 8, y + w / 2 + 0.03, 0.75), (0.12, 0.03, 1.5), M["timber"])
        g.box((x - w / 2 - 0.03, y - w / 2 + 0.07 + k * (w - 0.14) / 8, 0.75), (0.03, 0.12, 1.5), M["timber_y"])
    for k in range(5):
        g.box((x + w / 2 + 0.03, y - w / 2 + 0.07 + k * (w - 0.14) / 4, 0.55), (0.03, 0.22, 1.1), M["timber_y"])
    g.box((x, y, 1.5), (w, w, 0.04), M["timber"])                                                  # its ceiling
    g.box((x, y + 0.1, 0.02), (w - 0.1, w - 0.2, 0.02), M["soil"])
    g.cyl((x + 0.2, y + 0.3, 0.05), 0.06, 0.05, M["play_mid"], n=8)                             # a lost hair clip, a sweet wrapper
    g.box((x - 0.25, y + 0.2, 0.025), (0.08, 0.05, 0.01), M["chalk"], rot=(0, 0, 30))
    # the ladder up the back (+y), steep, with handrails and treads
    ly0, ly1 = y + w / 2, y + w / 2 + 1.2
    for s in (-1, 1):
        limb(g, (x + s * 0.3, ly1, 0.0), (x + s * 0.3, ly0, z), 0.03, 0.03, M["play_pale"], n=6)
        limb(g, (x + s * 0.36, ly1 + 0.05, 0.9), (x + s * 0.36, ly0, z + 1.0), 0.022, 0.022, M["play_pale"], n=6)
    for k in range(9):
        t = (k + 1) / 10
        g.box((x, ly1 - (ly1 - ly0) * t, z * t), (0.58, 0.1, 0.03), M["steel_mid"])
    # the chute: long, steep then levelling out, towards x+; its raised sides
    a, b = Vector((x + w / 2, y, z + 0.05)), Vector((x + w / 2 + 4.8, y, 0.45))
    segs = 10
    prev = None
    for k in range(segs + 1):
        t = k / segs
        p = a.lerp(b, t)
        p.z = a.z + (b.z - a.z) * (t ** 0.75)
        if prev is not None:
            d = p - prev
            ang = math.degrees(math.atan2(-d.z, d.x))
            mid = (p + prev) / 2
            g.box(mid[:], (d.length + 0.02, 0.56, 0.03), M["chute"], rot=(0, ang, 0))
            for s in (-1, 1):
                g.box((mid + Vector((0, s * 0.29, 0.12)))[:], (d.length + 0.02, 0.03, 0.26), M["chute"], rot=(0, ang, 0))
        prev = p
    g.box((b.x + 0.35, y, 0.43), (0.7, 0.56, 0.03), M["chute"])
    for s in (-1, 1):
        limb(g, (b.x + 0.6, y + s * 0.3, 0.0), (b.x + 0.6, y + s * 0.3, 0.43), 0.03, 0.03, M["play_pale"], n=6)
        limb(g, (x + w / 2 + 2.0, y + s * 0.3, 0.0), (x + w / 2 + 2.0, y + s * 0.3, 1.22), 0.03, 0.03, M["play_pale"], n=6)


def roundabout(g, M):
    x, y = 60.4, 5.6
    g.cyl((x, y, 0.08), 0.35, 0.16, M["concrete"], n=12)
    g.cyl((x, y, 0.24), 1.0, 0.06, M["deck"], n=28)
    g.torus((x, y, 0.24), 1.0, 0.03, M["steel"], n1=28, n2=5)
    for k in range(6):
        a = k / 6 * math.tau
        g.beam((x, y, 0.28), (x + math.cos(a) * 0.98, y + math.sin(a) * 0.98, 0.28), 0.04, 0.02, M["steel"])
        limb(g, (x + math.cos(a) * 0.85, y + math.sin(a) * 0.85, 0.27), (x + math.cos(a) * 0.25, y + math.sin(a) * 0.25, 0.85), 0.022, 0.022, M["play_pale"], n=6)
    g.torus((x, y, 0.85), 0.25, 0.025, M["play_pale"], n1=14, n2=5)
    g.cyl((x, y, 0.6), 0.05, 0.7, M["steel_mid"], n=8)
    g.cyl((x, y, 0.97), 0.08, 0.06, M["play_dark"], n=8)
    slab(g, [(x + math.cos(t) * 1.6, y + math.sin(t) * 1.6) for t in [k / 20 * math.tau for k in range(20)]], -0.004, 0.012, M["rubber"])


def seesaw(g, M):
    x, y = 38.4, 6.8
    g.box((x, y, 0.3), (0.12, 0.5, 0.6), M["steel"])
    g.cyl((x, y, 0.6), 0.06, 0.6, M["steel_mid"], rot=(90, 0, 0), n=10)
    g.box((x, y, 0.62), (3.6, 0.22, 0.06), M["deck"], rot=(0, 9, 0))
    for s in (-1, 1):
        hx = x + s * 1.5
        hz = 0.62 - s * 1.5 * math.tan(math.radians(9))
        g.box((hx, y, hz + 0.04), (0.4, 0.3, 0.05), M["play_dark"], rot=(0, 9, 0))
        limb(g, (hx - s * 0.22, y - 0.12, hz + 0.05), (hx - s * 0.22, y - 0.12, hz + 0.4), 0.018, 0.018, M["play_pale"], n=5)
        limb(g, (hx - s * 0.22, y + 0.12, hz + 0.05), (hx - s * 0.22, y + 0.12, hz + 0.4), 0.018, 0.018, M["play_pale"], n=5)
        limb(g, (hx - s * 0.22, y - 0.14, hz + 0.4), (hx - s * 0.22, y + 0.14, hz + 0.4), 0.02, 0.02, M["play_pale"], n=5)
        g.cyl((hx, y, 0.06 if s > 0 else 0.07), 0.12, 0.1, M["tyre"], n=10)                     # the tyre it bumps on


def spring_rider(g, x, y, M):
    g.box((x, y, 0.02), (0.4, 0.4, 0.04), M["steel"])
    for k in range(6):
        g.torus((x, y, 0.08 + k * 0.06), 0.09, 0.015, M["steel_mid"], n1=10, n2=4)
    g.box((x, y, 0.55), (0.75, 0.22, 0.28), M["play_mid"])
    g.box((x + 0.36, y, 0.78), (0.24, 0.18, 0.3), M["play_mid"], rot=(0, -20, 0))
    g.box((x + 0.45, y, 0.92), (0.2, 0.16, 0.14), M["play_mid"])
    limb(g, (x + 0.28, y - 0.15, 0.82), (x + 0.28, y + 0.15, 0.82), 0.015, 0.015, M["play_pale"], n=5)


def build_play(coll, M):
    g = S.Group("ParkPlay")
    climbing_frame(g, M)
    tall_slide(g, M)
    roundabout(g, M)
    seesaw(g, M)
    spring_rider(g, 47.6, 4.8, M)
    spring_rider(g, 63.6, 7.6, M)
    # the playground's own low railing (gate by the NO DOGS sign), its safety surface
    def opening(u):
        return 39.2 < u < 40.6
    railings(g, 37.0, 64.6, 4.0, M, h=0.85, spear=False, gap=0.13, skip=opening)
    for x in (37.0, 64.6):
        railings(g, 4.0, 10.6, x, M, h=0.85, spear=False, gap=0.13, axis="y")
    for s in (-1, 1):
        g.box((39.9 + s * 0.72, 4.0, 0.5), (0.08, 0.08, 1.0), M["steel"])
    sign_board(g, 41.2, 3.85, "NO DOGS", M, h=1.05, w=0.62, ht=0.42, size=0.11, lines=["NO DOGS", "IN THE", "PLAY AREA"])
    # the dog in a red circle, drawn in greys: a ring and a bar
    g.torus((41.0 + 0.0, 3.8, 1.55), 0.12, 0.018, M["sign_ink"], rot=(90, 0, 0), n1=14, n2=4)
    # benches round the play area (parents' benches, facing it), bins beside them
    park_bench(g, 50.0, 10.4, 180, M)
    park_bench(g, 57.0, 10.4, 180, M)
    litter_bin(g, 53.5, 10.5, M)
    # the park map board by the entrance
    mx, my = 36.4, 4.6
    for s in (-1, 1):
        g.box((mx + s * 0.6, my + 0.05, 0.85), (0.08, 0.08, 1.7), M["post"])
    g.box((mx, my, 1.4), (1.3, 0.05, 0.9), M["sign_ink"])
    g.box((mx, my - 0.03, 1.4), (1.2, 0.03, 0.8), M["sign"])
    g.box((mx, my - 0.02, 1.9), (1.45, 0.25, 0.06), M["post"])
    S.stencil(g, "ALDER PARK", (mx, my - 0.06, 1.68), 0.09, M["sign_ink"])
    pm = []                                                                                      # the map: paths, the pond, the play area
    pm.append((mx, my - 0.05, 1.35, 0.008, 0.5, 0.0))
    for k in range(8):
        a = k / 8 * math.tau
        pm.append((mx + 0.3 + math.cos(a) * 0.12, my - 0.05, 1.3 + math.sin(a) * 0.08, 0.006, 0.05, a + math.pi / 2))
    for (dx, dz) in ((-0.35, 1.5), (-0.1, 1.55), (0.35, 1.5)):
        g.box((mx + dx, my - 0.05, dz), (0.08, 0.008, 0.06), M["sign_ink"])
    g.box((mx + 0.3, my - 0.05, 1.3), (0.24, 0.008, 0.16), M["glass"])
    g.box((mx - 0.2, my - 0.05, 1.25), (0.04, 0.012, 0.04), M["sign_ink"])
    S.stencil(g, "YOU ARE HERE", (mx - 0.2, my - 0.06, 1.1), 0.04, M["sign_ink"])
    return g.finish(coll)


def build_lawn(coll, M):
    """The park's ground from the gate to the car park: the grass, the path the lane runs along,
    its edging, the pond's basin, the safety surface."""
    g = S.Group("ParkLawn")
    ground(g, GATE_X, 98.0, -14.0, 16.0, M["lawn_park"], 0.5, h=pond_depth, amp=0.06, seed=4.0)
    for (a, b) in ((GATE_X, BRIDGE_X0 + 0.3), (BRIDGE_X1 - 0.3, 98.0)):
        g.box(((a + b) / 2, 0.0, 0.0), (b - a, 2.6, 0.04), M["path"])
        for s in (-1, 1):
            g.box(((a + b) / 2, s * 1.33, 0.01), (b - a, 0.07, 0.06), M["kerb"])
    g.box((50.8, 7.3, 0.0), (27.6, 6.6, 0.04), M["rubber"])
    return g.finish(coll)


# ---------------------------------------------------------------- G3: the pond and the footbridge

def footbridge(g, M):
    """The footbridge the lane crosses: piles in the water, stringers, the deck's boards across,
    posts with a top rail, a mid rail and balusters both sides; steps of the bank at each end."""
    x0, x1 = BRIDGE_X0, BRIDGE_X1
    g.box(((x0 + x1) / 2, 0.0, -0.035), (x1 - x0, 2.4, 0.03), M["deck"])
    for k in range(int((x1 - x0) / 0.16)):                                                       # the boards' gaps
        g.box((x0 + 0.08 + k * 0.16, 0.0, -0.018), (0.015, 2.4, 0.008), M["sign_ink"])
    for s in (-1, 1):
        g.box(((x0 + x1) / 2, s * 1.1, -0.17), (x1 - x0, 0.1, 0.24), M["post"])                    # stringers
        g.box(((x0 + x1) / 2, s * 1.22, -0.05), (x1 - x0, 0.04, 0.12), M["timber"])                 # the fascia
    n = int((x1 - x0) / 1.5)
    for k in range(n + 1):
        x = x0 + k * (x1 - x0) / n
        for s in (-1, 1):
            g.box((x, s * 1.25, 0.5), (0.12, 0.12, 1.1), M["post"])
            g.cyl((x, s * 1.25, 1.07), 0.075, 0.05, M["post"], n=4, r2=0.02)
            bolt(g, (x, s * 1.32, -0.12), M["steel"])
            bolt(g, (x, s * 1.32, 0.1), M["steel"])
            g.cyl((x, s * 1.1, -0.6), 0.1, 1.0, M["post"], n=8)                                   # the pile
        if k < n:
            xa, xb = x, x0 + (k + 1) * (x1 - x0) / n
            for s in (-1, 1):
                g.box(((xa + xb) / 2, s * 1.25, 1.0), (xb - xa, 0.14, 0.06), M["timber"])
                g.box(((xa + xb) / 2, s * 1.25, 0.55), (xb - xa, 0.05, 0.08), M["timber"])
                for j in range(1, 6):
                    g.box((xa + (xb - xa) * j / 6, s * 1.25, 0.28), (0.04, 0.04, 0.5), M["timber"])
            g.box(((xa + xb) / 2, 0.0, -0.3), (0.1, 2.3, 0.1), M["post"])                         # the cross beams
    # the lamp at the middle: its own pile, braced to the parapet
    g.cyl((82.0, -1.6, -0.45), 0.1, 0.9, M["post"], n=8)
    g.beam((82.0, -1.6, 0.9), (82.0, -1.25, 0.95), 0.05, 0.05, M["steel"])
    # a plaque: ALDER PARK FOOTBRIDGE 1911
    g.box(((x0 + x1) / 2, -1.33, 0.78), (0.4, 0.02, 0.14), M["chrome"])


def build_pond(coll, M):
    g = S.Group("ParkPond")
    slab(g, pond_outline(0.84), WATER_Z - 0.03, WATER_Z, M["water"])
    footbridge(g, M)
    # the reeds: dense at the edges, bulrush heads, none in the sightline's strip
    for _ in range(80):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(0.74, 0.9)
        cx, cy = POND_C[0] + math.cos(a) * POND_R[0] * r, POND_C[1] + math.sin(a) * POND_R[1] * r
        if abs(cy) < 1.6 or not sight_clear(cx, cy, 0.4):
            continue
        for k in range(rnd.randint(6, 12)):
            x, y = cx + rnd.uniform(-0.35, 0.35), cy + rnd.uniform(-0.25, 0.25)
            h = rnd.uniform(0.9, 1.7)
            tip = (x + rnd.uniform(-0.15, 0.15), y + rnd.uniform(-0.1, 0.1), WATER_Z + h)
            limb(g, (x, y, WATER_Z - 0.1), tip, 0.012, 0.004, M["reed"], n=3)
            if rnd.random() < 0.3:
                g.cyl((tip[0], tip[1], tip[2] - 0.2), 0.022, 0.18, M["reed_head"], n=6)
        for k in range(4):                                                                       # flag leaves
            x, y = cx + rnd.uniform(-0.3, 0.3), cy + rnd.uniform(-0.2, 0.2)
            limb(g, (x, y, WATER_Z), (x + rnd.uniform(-0.4, 0.4), y + rnd.uniform(-0.2, 0.2), WATER_Z + rnd.uniform(0.5, 0.9)), 0.025, 0.003, M["foliage"], n=3)
    # lily pads on the water, a few flowers shut for the night
    pads = []
    for _ in range(70):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(0.2, 0.7)
        x, y = POND_C[0] + math.cos(a) * POND_R[0] * r, POND_C[1] + math.sin(a) * POND_R[1] * r
        if abs(y) < 1.5:
            continue
        pads.append((x, y))
        g.cyl((x, y, WATER_Z + 0.004), rnd.uniform(0.12, 0.22), 0.008, M["lily"], n=9)
    for (x, y) in pads[:6]:
        g.cyl((x + 0.05, y, WATER_Z + 0.04), 0.04, 0.06, M["flower"], n=6, r2=0.015)
    # stones at the water's edge, the bank's edging
    for k in range(60):
        a = k / 60 * math.tau
        x, y = POND_C[0] + math.cos(a) * POND_R[0] * 0.86, POND_C[1] + math.sin(a) * POND_R[1] * 0.86
        if abs(y) < 1.5:
            continue
        blob(g, (x, y, WATER_Z + 0.06), rnd.uniform(0.12, 0.26), M["concrete"], s=(1.0, 1.0, 0.6), rough=0.3)
    # the willow on the far bank, its curtain of hanging strands; the lifebuoy on its post
    wx, wy = 84.5, 11.2
    tree(g, wx, wy, 8.5, M, leafy=0.2, mains=6)
    for _ in range(90):
        a = rnd.uniform(0, math.tau)
        r = rnd.uniform(1.2, 3.6)
        x, y = wx + math.cos(a) * r, wy + math.sin(a) * r * 0.8
        top = rnd.uniform(5.0, 7.2)
        g.beam((x, y, top), (x + rnd.uniform(-0.1, 0.1), y, top - rnd.uniform(2.5, 4.5)), 0.03, 0.03, M["foliage"])
    lx, ly = 93.3, 4.4
    g.box((lx, ly, 0.9), (0.12, 0.12, 1.8), M["post"])
    g.box((lx, ly - 0.08, 1.75), (0.5, 0.06, 0.12), M["post"])
    g.torus((lx, ly - 0.14, 1.25), 0.3, 0.07, M["play_pale"], rot=(90, 0, 0), n1=20, n2=8)
    for k in range(4):
        a = k / 4 * math.tau + math.pi / 4
        g.box((lx + math.cos(a) * 0.3, ly - 0.14, 1.25 + math.sin(a) * 0.3), (0.12, 0.15, 0.15), M["play_dark"], rot=(0, -math.degrees(a), 0))
    sign_board(g, 69.5, 4.6, "DEEP WATER", M, h=1.0, w=0.62, ht=0.36, size=0.09, lines=["DANGER", "DEEP WATER", "NO SWIMMING"])
    # ducks asleep on the water, a duck house on its pole
    for (x, y) in ((77.0, 5.2), (77.6, 5.5), (87.5, -3.8)):
        g.box((x, y, WATER_Z + 0.06), (0.28, 0.14, 0.1), M["play_mid"])
        g.box((x + 0.1, y, WATER_Z + 0.12), (0.1, 0.09, 0.08), M["play_dark"])
    g.cyl((90.0, 6.4, 0.2), 0.05, 1.3, M["post"], n=6)
    g.box((90.0, 6.4, 0.95), (0.7, 0.5, 0.4), M["timber"])
    for s in (-1, 1):
        g.box((90.0, 6.4 + s * 0.17, 1.24), (0.8, 0.38, 0.03), M["post"], rot=(-s * 30, 0, 0))
    g.box((90.0, 6.14, 0.9), (0.18, 0.02, 0.2), M["sign_ink"])
    # benches looking at the water from the far bank, a bin
    park_bench(g, 75.0, 9.4, 180, M)
    park_bench(g, 92.0, 9.6, 200, M)
    litter_bin(g, 77.0, 9.6, M)
    return g.finish(coll)


# ---------------------------------------------------------------- G4: the car park, the van, the hill road

def bay_lines(g, x0, x1, y0, y1, w, M):
    n = int((x1 - x0) / w)
    for k in range(n + 1):
        g.box((x0 + k * w, (y0 + y1) / 2, 0.012), (0.1, abs(y1 - y0), 0.008), M["line"])
    g.box(((x0 + x0 + n * w) / 2, y0 if abs(y0) < abs(y1) else y1, 0.012), (n * w, 0.1, 0.008), M["line"])


def car(g, x, y, yaw, m, M):
    """A parked hatchback: the body, its glass, wheels, lights, mirrors, a wet sheen."""
    def c(t):
        prof = [(-2.0, 0.3), (2.0, 0.3), (2.05, 0.75), (1.4, 0.88), (0.65, 1.42), (-1.2, 1.45), (-1.95, 0.95), (-2.05, 0.6)]
        t.prism(prof, -0.86, 0.86, m)
        t.prism([(0.72, 0.9), (1.33, 0.9), (0.64, 1.38)], -0.8, 0.8, M["glass"])
        t.box((-0.25, 0.0, 1.15), (1.6, 1.74, 0.42), M["glass"])
        t.box((-1.97, 0, 1.15), (0.08, 1.4, 0.35), M["glass"], rot=(0, 18, 0))
        for sx in (-1.32, 1.3):
            for sy in (-0.82, 0.82):
                t.cyl((sx, sy, 0.32), 0.32, 0.22, M["tyre"], rot=(90, 0, 0), n=14)
                t.cyl((sx, sy * 1.02, 0.32), 0.18, 0.2, M["chrome"], rot=(90, 0, 0), n=10)
        for sy in (-0.6, 0.6):
            t.box((2.04, sy, 0.68), (0.04, 0.3, 0.12), M["sign"])
            t.box((-2.06, sy, 0.8), (0.04, 0.25, 0.14), M["play_dark"])
        for sy in (-0.95, 0.95):
            t.box((0.55, sy, 1.0), (0.12, 0.08, 0.1), m)
    sub(g, c, (x, y, 0), yaw)


def build_carpark(coll, M):
    g = S.Group("ParkCarpark")
    # the park's end: a low wall with railings on it, brick gate piers with stone caps, the gates open
    ex = 98.0
    def gap(u):
        return -1.7 < u < 4.4
    for (a, b) in ((-14.0, -1.7), (4.4, 16.0)):
        g.box((ex, (a + b) / 2, 0.3), (0.35, b - a, 0.6), M["brick_y"])
        g.box((ex, (a + b) / 2, 0.63), (0.45, b - a, 0.06), M["coping"])
        railings(g, a, b, ex, M, h=1.15, gap=0.13, axis="y")
    for yy in (-1.9, 4.6):
        g.box((ex, yy, 0.9), (0.55, 0.55, 1.8), M["brick_y"])
        g.box((ex, yy, 1.85), (0.68, 0.68, 0.1), M["coping"])
        g.cyl((ex, yy, 2.0), 0.18, 0.22, M["coping"], n=12, r2=0.06)
    def gate_leaf(t, L):
        for k in range(int(L / 0.13)):
            t.cyl((0, k * 0.13 + 0.1, 0.6), 0.011, 1.15, M["steel"], n=4)
            t.cyl((0, k * 0.13 + 0.1, 1.22), 0.02, 0.08, M["steel"], n=4, r2=0.0)
        for z in (0.12, 0.6, 1.1):
            t.box((0, L / 2, z), (0.03, L, 0.04), M["steel"])
    sub(g, lambda t: gate_leaf(t, 2.2), (ex + 0.2, -1.65, 0), yaw=-80)                            # swung back
    sub(g, lambda t: gate_leaf(t, 2.2), (ex + 0.2, 4.35, 0), yaw=-100)
    sign_board(g, ex + 0.6, 5.4, "ALDER PARK", M, h=1.0, w=1.0, ht=0.32, size=0.1, lines=["ALDER PARK", "GATES LOCKED AT DUSK"], two_legs=True)
    # the kerbs round the tarmac, the painted bays (both sides of the aisle), the exit lane
    for (a, b, y) in ((99.0, 140.0, -9.0), (99.0, 122.0, 9.5)):
        g.box(((a + b) / 2, y, 0.06), (b - a, 0.15, 0.14), M["kerb"])
    bay_lines(g, 100.0, 122.5, 4.4, 9.4, 2.5, M)
    bay_lines(g, 100.0, 132.5, -3.2, -8.2, 2.5, M)
    for k in range(9):                                                                           # the bay numbers
        S_x = 101.25 + k * 2.5
        flat_text(g, str(k + 1), (S_x, 8.6, 0.016), 0.35, M["line"])
    for k in range(13):
        flat_text(g, str(k + 10), (101.25 + k * 2.5, -7.7, 0.016), 0.35, M["line"], yaw=180)
    # the exit: hatching, an arrow, EXIT painted on the tarmac; NO PARKING by the gate
    for k in range(10):
        g.box((123.2 + k * 0.42, 6.9, 0.012), (0.08, 3.6, 0.008), M["line"], rot=(0, 0, 35))
    flat_text(g, "EXIT", (131.6, SIGHT_Y - 1.6, 0.016), 0.7, M["line"], yaw=90)
    for (x, l) in ((121.0, 1.6),):
        g.box((x, -0.0 + SIGHT_Y + 1.5, 0.012), (l, 0.16, 0.008), M["line"])
        flecks(g, [(x + l / 2 + 0.2, SIGHT_Y + 1.5, 0.017, 0.35, 0.3, 0.0)], M["line"])
    g.box((134.0, 0.0, 0.012), (0.2, 0.16, 0.008), M["line"])
    for k in range(6):                                                                           # the give-way line
        g.box((BARRIER_X + 1.2, -0.2 + k * 1.1, 0.012), (0.3, 0.6, 0.008), M["line"])
    # the barrier: its housing, the post the arm rests on (the arm is the BarrierArm object)
    bx, by = BARRIER_X, SIGHT_Y + 3.6
    g.box((bx, by, 0.55), (0.45, 0.45, 1.1), M["steel_mid"])
    g.box((bx, by, 1.12), (0.5, 0.5, 0.05), M["steel"])
    g.box((bx, by - 0.24, 0.8), (0.3, 0.02, 0.2), M["sign"])
    g.box((bx, SIGHT_Y - 3.2, 0.45), (0.12, 0.12, 0.9), M["steel_mid"])
    g.box((bx, SIGHT_Y - 3.2, 0.95), (0.18, 0.12, 0.1), M["steel"])
    g.box((bx - 1.6, by + 0.2, 0.55), (0.4, 0.3, 1.1), M["steel_mid"])                         # the ticket machine
    g.box((bx - 1.6, by + 0.04, 0.85), (0.24, 0.02, 0.3), M["lit_dim"])
    # the pay-and-display machine and its sign, a bottle bank, a grit bin, bollards
    g.box((106.0, 10.2, 0.75), (0.5, 0.35, 1.5), M["steel_mid"])
    g.box((106.0, 10.0, 1.15), (0.35, 0.02, 0.25), M["lit_dim"])
    g.box((106.0, 10.0, 0.75), (0.2, 0.02, 0.06), M["steel"])
    sign_board(g, 107.4, 10.0, "PAY AND DISPLAY", M, h=2.0, w=0.9, ht=0.6, size=0.11, lines=["PAY AND", "DISPLAY", "8AM - 6PM"])
    for k, x in enumerate((112.0, 114.0)):
        g.box((x, 11.0, 0.8), (1.8, 1.6, 1.6), M["steel_mid"] if k else M["play_dark"])
        g.cyl((x, 11.0, 1.65), 0.5, 0.15, M["steel"], n=12)
        for s in (-1, 1):
            g.cyl((x + s * 0.5, 10.2, 1.2), 0.1, 0.02, M["steel"], rot=(90, 0, 0), n=10)
    g.box((117.0, 10.6, 0.4), (0.9, 0.6, 0.8), M["play_mid"])
    g.box((117.0, 10.55, 0.84), (1.0, 0.7, 0.1), M["play_mid"], rot=(8, 0, 0))
    for x in range(100, 140, 2):
        g.cyl((x + 0.5, -9.4, 0.4), 0.08, 0.8, M["steel"], n=8)
        g.torus((x + 0.5, -9.4, 0.7), 0.08, 0.015, M["sign"], n1=8, n2=4)
    # the parked cars: two in the bays behind (dark), one at the end in front
    car(g, 103.75, 6.9, 92, M["car_a"], M)
    car(g, 111.25, 7.0, 88, M["car_b"], M)
    car(g, 126.25, -5.9, 271, M["car_a"], M)
    # the car park lamps: tall columns with a double head
    for (x, y) in CAR_LAMPS:
        g.cyl((x, y, CAR_LAMP_H / 2), 0.09, CAR_LAMP_H, M["steel_mid"], n=10, r2=0.06)
        g.box((x, y, 0.4), (0.24, 0.24, 0.8), M["steel_mid"])
        g.box((x, y - 0.13, 0.5), (0.12, 0.02, 0.2), M["steel"])
        for s in (-1, 1):
            g.beam((x, y, CAR_LAMP_H - 0.1), (x + s * 0.8, y, CAR_LAMP_H + 0.05), 0.05, 0.05, M["steel_mid"])
            g.box((x + s * 1.0, y, CAR_LAMP_H + 0.05), (0.6, 0.3, 0.1), M["steel_mid"])
            g.box((x + s * 1.0, y, CAR_LAMP_H - 0.01), (0.5, 0.22, 0.03), M["sodium"])
    # a hedge and trees round the car park; CCTV on a pole
    hedge(g, 99.5, 122.0, 12.4, 1.6, 1.0, M)
    hedge(g, 99.5, 140.0, -11.0, 1.2, 1.0, M)
    for (x, y, h) in ((102.0, 14.5, 8.5), (119.0, 14.0, 9.5), (137.5, 12.6, 8.0), (104.0, -13.0, 8.0), (131.0, -12.8, 9.0)):
        tree(g, x, y, h, M, leafy=0.45)
    g.cyl((120.5, 11.0, 2.5), 0.06, 5.0, M["steel_mid"], n=8)
    g.box((120.5, 10.7, 5.0), (0.16, 0.45, 0.16), M["steel"], rot=(25, 0, 30))
    sign_board(g, 121.2, 10.6, "CCTV", M, h=2.2, w=0.5, ht=0.4, size=0.1, lines=["CCTV", "IN OPERATION"])
    # puddles on the tarmac, leaves blown into the corners
    for (x, y, rx, ry) in ((104.5, -1.9, 1.1, 0.5), (113.0, 2.4, 0.9, 0.6), (120.0, -1.5, 1.4, 0.6), (127.0, 0.9, 0.8, 0.4), (108.0, 4.0, 0.7, 0.4), (133.0, 5.0, 1.2, 0.6)):
        puddle(g, x, y, rx, ry, M, z=0.008)
    leaves(g, 99.0, 140.0, -9.0, 9.5, 450, M, avoid=lambda x, y: abs(y) < 1.0 and x < 131)
    leaves(g, 99.0, 122.0, 8.6, 9.4, 120, M)
    # the hill road: out through the barrier, curving round and up towards Kessel Hill
    road_curve(g, M)
    return g.finish(coll)


def ramp(x):
    """The land beyond the car park rising towards Kessel Hill (the hill road climbs with it)."""
    return min(4.0, 7.0 * max(0.0, (x - BARRIER_X - 1.0) / 70.0 - 0.15) ** 1.5)


def hill_h(x, y):
    """Kessel Hill: broad, steep-sided, a flat crown for the fortress, its flanks broken by noise."""
    cx, cy = HILL_C
    r = math.hypot((x - cx) / HILL_R[0], (y - cy) / HILL_R[1])
    t = min(1.0, max(0.0, 1.0 - r) / 0.55)
    base = HILL_H * t * t * (3 - 2 * t)
    rough = 3.0 * noise.noise(Vector((x * 0.035, y * 0.035, 2.0))) * min(1.0, 4.0 * t * (1 - t) + 0.15)
    return max(ramp(x), base + rough - 0.3)


def road_points():
    """The hill road's centre line: (x, y, z) from the barrier, curving round towards the hill."""
    pts = []
    for k in range(41):
        t = k / 40
        x = BARRIER_X + 1.0 + t * 38.0
        y = SIGHT_Y + 12.0 * (t ** 1.8)
        pts.append(Vector((x, y, ramp(x) + 0.02)))
    return pts


def road_curve(g, M):
    pts = road_points()
    tmp = bmesh.new()
    prev = None
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        n = Vector((-d.y, d.x, 0)).normalized()
        a, b = p + n * 3.2, p - n * 3.2
        va, vb = tmp.verts.new(a), tmp.verts.new(b)
        if prev:
            tmp.faces.new((prev[1], vb, va, prev[0]))
        prev = (va, vb)
        if i % 2 == 0 and i < len(pts) - 1:
            q = pts[i + 1]
            g.beam((p + d * 0.2 + Vector((0, 0, 0.02)))[:], ((p + q) / 2 + Vector((0, 0, 0.02)))[:], 0.12, 0.01, M["line"])
        for s in (-1, 1):                                                                       # the kerbs
            if i < len(pts) - 1:
                q = pts[i + 1]
                qd = (pts[min(i + 2, len(pts) - 1)] - pts[i]).normalized()
                qn = Vector((-qd.y, qd.x, 0)).normalized()
                g.beam((p + n * s * 3.3 + Vector((0, 0, 0.06)))[:], (q + qn * s * 3.3 + Vector((0, 0, 0.06)))[:], 0.15, 0.14, M["kerb"])
        if i % 4 == 2:                                                                           # crash barrier posts on the outer side
            c = p - n * 4.0
            g.box((c.x, c.y, c.z + 0.35), (0.1, 0.1, 0.7), M["steel_mid"])
    me = bpy.data.meshes.new("road")
    tmp.to_mesh(me)
    tmp.free()
    for poly in me.polygons:
        poly.material_index = g._mi(M["tarmac"])
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    for i in range(0, len(pts) - 4, 4):
        p, q = pts[i + 2], pts[i + 6] if i + 6 < len(pts) else pts[-1]
        dp = (pts[i + 3] - pts[i + 1]).normalized()
        dq = (pts[min(i + 7, len(pts) - 1)] - pts[min(i + 5, len(pts) - 1)]).normalized()
        np_, nq = Vector((-dp.y, dp.x, 0)), Vector((-dq.y, dq.x, 0))
        g.beam((p - np_ * 4.0 + Vector((0, 0, 0.6)))[:], (q - nq * 4.0 + Vector((0, 0, 0.6)))[:], 0.04, 0.3, M["steel_mid"])
    # road lamps along it, a sign: KESSEL HILL, the fort's crest
    for i in (6, 18, 30):
        p = pts[i]
        d = (pts[i + 1] - pts[i - 1]).normalized()
        n = Vector((-d.y, d.x, 0))
        c = p + n * 4.2
        g.cyl((c.x, c.y, c.z + 3.0), 0.07, 6.0, M["steel_mid"], n=8)
        e = c - n * 1.4
        g.beam((c.x, c.y, c.z + 5.9), (e.x, e.y, e.z + 6.0), 0.05, 0.05, M["steel_mid"])
        g.box((e.x, e.y, e.z + 5.95), (0.6, 0.3, 0.12), M["steel_mid"])
        g.box((e.x, e.y, e.z + 5.88), (0.5, 0.22, 0.03), M["sodium"])
    p = pts[3]
    sx, sy = p.x + 1.0, p.y - 4.4
    for s in (-1, 1):
        g.box((sx + s * 0.8, sy, 1.1), (0.08, 0.08, 2.2), M["steel_mid"])
    g.box((sx, sy - 0.04, 1.9), (2.2, 0.04, 0.9), M["sign_ink"])
    g.box((sx, sy - 0.07, 1.9), (2.1, 0.03, 0.8), M["sign"])
    S.stencil(g, "KESSEL HILL  3", (sx - 0.1, sy - 0.1, 1.95), 0.18, M["sign_ink"])
    S.stencil(g, "OBSERVATORY", (sx - 0.1, sy - 0.1, 1.68), 0.13, M["sign_ink"])
    g.box((sx + 0.9, sy - 0.1, 1.9), (0.3, 0.02, 0.06), M["sign_ink"])
    g.cyl((sx + 1.07, sy - 0.1, 1.9), 0.1, 0.02, M["sign_ink"], rot=(90, 0, 0), n=3)


def build_carpark_ground(coll, M):
    g = S.Group("ParkCarparkGround")
    g.box((119.5, 0.25, -0.02), (41.0, 18.5, 0.04), M["tarmac"])
    ground(g, 98.0, 141.0, -14.0, -9.1, M["lawn_park"], 1.0, amp=0.08, seed=6.0)
    ground(g, 98.0, 140.0, 9.6, 16.0, M["lawn_park"], 1.0, amp=0.08, seed=7.0)
    ground(g, 140.0, 167.0, -20.0, 60.0, M["lawn_park"], 1.5, h=lambda x, y: ramp(x) - 0.08, amp=0.25, flat=0.0, seed=8.0)
    return g.finish(coll)


def build_van(coll, M):
    """The white van: a panel van, its nose to the hill road, its back to the park. The body, cab
    glass, wheels and arches, lights, mirrors, the roof rack; the back doors are VanDoorL and
    VanDoorR (each with a small window, hinged at the outer edge). The origin is on the ground under
    its centre: the game drives it away."""
    g = S.Group("Van")
    x0, x1 = VAN_BACK, VAN_BACK + VAN_L
    y, w = VAN_Y, 2.0
    prof = [(x0, 0.42), (x1 - 0.1, 0.42), (x1, 0.55), (x1, 1.0), (x1 - 0.5, 1.25), (x1 - 1.25, 2.05), (x1 - 1.5, 2.35),
            (x0 + 0.05, 2.38), (x0, 2.3)]
    tmp = S.Group("tmp")
    tmp.prism([(px, pz) for px, pz in prof], y - w / 2, y + w / 2, M["van"])
    me = bpy.data.meshes.new("vanbody")
    tmp.bm.to_mesh(me)
    tmp.bm.free()
    for p in me.polygons:
        p.material_index = g._mi(M["van"])
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    # the cab's windscreen and side windows, the bulkhead; the load area's dark inside (seen when the doors open)
    g.box((x1 - 0.85, y, 1.68), (0.05, w - 0.12, 0.7), M["glass"], rot=(0, -47, 0))
    for s in (-1, 1):
        g.prism([(x1 - 1.65, 1.35), (x1 - 0.75, 1.35), (x1 - 1.25, 1.98), (x1 - 1.55, 2.12)], y + s * (w / 2 + 0.005) - 0.01, y + s * (w / 2 + 0.005) + 0.01, M["glass"])
        g.box((x1 - 1.7, y + s * (w / 2 + 0.01), 1.25), (0.02, 0.01, 0.95), M["van_trim"])               # the cab door's seam
        g.box((x1 - 2.75, y + s * (w / 2 + 0.01), 1.25), (0.02, 0.01, 1.6), M["van_trim"])               # the side door's seam
        g.box((x1 - 2.2, y + s * (w / 2 + 0.03), 1.1), (0.22, 0.03, 0.04), M["van_trim"])
        g.box((x1 - 1.55, y + s * (w / 2 + 0.25), 1.6), (0.06, 0.28, 0.2), M["van_trim"])               # mirrors
        g.box((x1 - 1.5, y + s * (w / 2 + 0.12), 1.6), (0.04, 0.2, 0.04), M["van_trim"])
        g.box(((x0 + x1) / 2, y + s * (w / 2 + 0.01), 0.55), (x1 - x0 - 0.2, 0.02, 0.12), M["van_trim"])   # the rubbing strip
    g.box((x0 + 0.1, y, 1.4), (0.1, w - 0.16, 1.85), M["van_inside"])                                    # behind the doors
    g.box((x1 - 1.9, y, 1.4), (0.06, w - 0.1, 1.85), M["van_inside"])
    g.box((x0 + 0.4, y, 0.48), (0.6, w - 0.2, 0.04), M["van_inside"])
    # the front: grille, bumper, headlights (off), number plate
    g.box((x1 + 0.02, y, 0.8), (0.04, 1.3, 0.35), M["van_trim"])
    for k in range(4):
        g.box((x1 + 0.04, y, 0.68 + k * 0.08), (0.02, 1.2, 0.02), M["steel_mid"])
    g.box((x1 + 0.05, y, 0.5), (0.12, w + 0.04, 0.2), M["van_trim"])
    for s in (-1, 1):
        g.box((x1 - 0.02, y + s * 0.78, 0.95), (0.06, 0.32, 0.18), M["sign"])
    g.box((x1 + 0.12, y, 0.5), (0.02, 0.52, 0.11), M["sign"])
    # the back: the bumper and step, the tail lights, the number plate; the roof rack and a ladder
    g.box((x0 - 0.05, y, 0.5), (0.14, w + 0.02, 0.2), M["van_trim"])
    g.box((x0 - 0.15, y, 0.42), (0.2, 0.8, 0.04), M["steel_mid"])
    for s in (-1, 1):
        g.box((x0 - 0.01, y + s * 0.94, 1.0), (0.04, 0.1, 0.45), M["play_mid"])
    g.box((x0 - 0.13, y, 0.6), (0.02, 0.52, 0.11), M["sign"])
    for s in (-1, 1):
        g.box(((x0 + x1 - 1.4) / 2, y + s * 0.8, 2.45), (x1 - x0 - 1.6, 0.05, 0.05), M["steel_mid"])
    for k in range(6):
        xx = x0 + 0.3 + k * (x1 - x0 - 1.9) / 5
        g.box((xx, y, 2.45), (0.05, 1.7, 0.04), M["steel_mid"])
        g.box((xx, y + 0.8, 2.42), (0.05, 0.05, 0.08), M["steel_mid"])
        g.box((xx, y - 0.8, 2.42), (0.05, 0.05, 0.08), M["steel_mid"])
    g.box((x0 + 1.6, y + 0.3, 2.52), (2.6, 0.38, 0.04), M["steel"])
    # wheels and arches
    for wx in (x0 + 0.95, x1 - 1.05):
        for s in (-1, 1):
            g.cyl((wx, y + s * 0.88, 0.36), 0.36, 0.24, M["tyre"], rot=(90, 0, 0), n=16)
            g.cyl((wx, y + s * 0.99, 0.36), 0.2, 0.04, M["chrome"], rot=(90, 0, 0), n=10)
            for k in range(5):
                a = k / 5 * math.tau
                g.cyl((wx + math.cos(a) * 0.12, y + s * 1.01, 0.36 + math.sin(a) * 0.12), 0.02, 0.02, M["steel"], rot=(90, 0, 0), n=6)
            for k in range(7):
                a = math.pi * k / 6
                g.box((wx + math.cos(a) * 0.46, y + s * 1.0, 0.36 + math.sin(a) * 0.46), (0.24, 0.04, 0.07), M["van_trim"], rot=(0, -math.degrees(a) + 90, 0))
    # muck on the sills and the back from the wet lanes
    for k in range(12):
        g.box((x0 + rnd.uniform(0.3, VAN_L - 0.3), y + rnd.choice((-1, 1)) * (w / 2 + 0.008), rnd.uniform(0.45, 0.7)), (rnd.uniform(0.2, 0.6), 0.01, rnd.uniform(0.04, 0.12)), M["concrete"])
    ob = g.finish(coll)
    set_origin(ob, (VAN_X, VAN_Y, 0.0))
    return ob


def build_van_door(coll, M, side):
    """A back door of the van: L (to the left seen from behind, +y) or R. Hinged at its outer edge:
    the origin is on the hinge line, at the ground. A small window in its upper half."""
    name = "VanDoorL" if side > 0 else "VanDoorR"
    g = S.Group(name)
    x = VAN_BACK - 0.03
    y0, y1 = VAN_Y, VAN_Y + side * 0.99
    yc = (y0 + y1) / 2
    w = abs(y1 - y0) - 0.01
    z0, z1 = 0.62, 2.28
    wz0, wz1 = 1.5, 2.05
    # the panel around the window, the window, its rubber seal
    g.box((x, yc, (z0 + wz0) / 2), (0.05, w, wz0 - z0), M["van"])
    g.box((x, yc, (wz1 + z1) / 2), (0.05, w, z1 - wz1), M["van"])
    wy0, wy1 = sorted((y0 + side * 0.12, y1 - side * 0.12))
    for (a, b) in ((min(y0, y1), wy0), (wy1, max(y0, y1))):
        if b - a > 0.005:
            g.box((x, (a + b) / 2, (wz0 + wz1) / 2), (0.05, b - a, wz1 - wz0), M["van"])
    g.box((x + 0.01, (wy0 + wy1) / 2, (wz0 + wz1) / 2), (0.02, wy1 - wy0, wz1 - wz0), M["glass"])
    for (a, b, c, d) in (((wy0 + wy1) / 2, wz0, wy1 - wy0 + 0.04, 0.025), ((wy0 + wy1) / 2, wz1, wy1 - wy0 + 0.04, 0.025)):
        g.box((x - 0.03, a, b), (0.01, c, d), M["van_trim"])
    for yy in (wy0, wy1):
        g.box((x - 0.03, yy, (wz0 + wz1) / 2), (0.01, 0.025, wz1 - wz0 + 0.04), M["van_trim"])
    # pressed ribs, the handle (on the right door), hinges, a dent, the door's seam
    for zz in (0.9, 1.2):
        g.box((x - 0.03, yc, zz), (0.01, w - 0.15, 0.04), M["van"])
    if side < 0:
        g.box((x - 0.05, y0 - 0.12, 1.2), (0.04, 0.05, 0.22), M["van_trim"])
    for zz in (0.85, 1.55, 2.1):
        g.box((x - 0.02, y1 - side * 0.03, zz), (0.06, 0.06, 0.14), M["van_trim"])
    g.box((x - 0.035, y0, (z0 + z1) / 2), (0.01, 0.015, z1 - z0), M["van_trim"])
    ob = g.finish(coll)
    set_origin(ob, (VAN_BACK - 0.03, y1, 0.0))
    return ob


def build_barrier_arm(coll, M):
    """The barrier's arm, raised: striped (pale and dark), its counterweight; its origin is the hinge."""
    g = S.Group("BarrierArm")
    hx, hy, hz = BARRIER_X, SIGHT_Y + 3.6, 1.0
    L = 6.6
    ang = math.radians(78)
    d = Vector((0, -math.cos(ang), math.sin(ang)))
    n = 8
    for k in range(n):
        a = Vector((hx, hy, hz)) + d * (L * k / n + 0.1)
        b = Vector((hx, hy, hz)) + d * (L * (k + 1) / n + 0.1)
        g.beam(a[:], b[:], 0.08, 0.1, M["play_pale"] if k % 2 == 0 else M["play_dark"], up=Vector((1, 0, 0)))
    g.box((hx, hy, hz), (0.3, 0.3, 0.3), M["steel"])
    g.beam((hx, hy, hz), (Vector((hx, hy, hz)) - d * 0.7)[:], 0.18, 0.18, M["steel_mid"], up=Vector((1, 0, 0)))
    ob = g.finish(coll)
    set_origin(ob, (hx, hy, hz))
    return ob


# ---------------------------------------------------------------- around: the foreground, the backs of the houses, the town

def build_fore(coll, M):
    """In front of the lane (towards the camera): low things only near the lane (it must not hide the
    fighters' feet): bed edges, plant pots, the park lamps' row, benches facing the path, bins, a low
    hedge, tufts; further off (beyond the camera, for the reverse shots), fences, trees, houses."""
    g = S.Group("ParkFore")
    # the park lamps (their light comes from the Unity cones and the bake)
    for (x, y) in LAMPS:
        park_lamp(g, x, y, M)
    litter_bin(g, 37.9, -1.75, M)
    litter_bin(g, 61.9, -1.75, M)
    litter_bin(g, 95.1, -1.75, M)
    g.cyl((49.9, -1.75, 0.45), 0.16, 0.5, M["play_dark"], n=10)                                  # the dog bin
    g.cyl((49.9, -1.75, 0.15), 0.03, 0.3, M["steel"], n=6)
    g.box((49.9, -1.9, 0.6), (0.2, 0.02, 0.12), M["sign"])
    park_bench(g, 44.0, -2.6, 0, M)
    park_bench(g, 64.5, -2.6, 0, M)
    park_bench(g, 96.8, -2.8, 10, M)
    # low hedges and beds along the park path's near side, gaps where the benches are
    for (a, b) in ((35.5, 42.6), (45.6, 63.0), (66.0, 70.5), (93.5, 95.6)):
        hedge(g, a, b, -4.2, 0.55, 0.7, M)
    tufts(g, 34.5, 98.0, -3.6, -2.0, 120, M, h=0.2)
    leaves(g, GATE_X, 98.0, -14.0, -1.4, 700, M, zf=lambda x, y: pond_depth(x, y) + (0.02 if abs(y) < 1.35 else 0.0),
           avoid=lambda x, y: pond_depth(x, y) < -0.2)
    leaves(g, GATE_X, 98.0, 1.4, 16.0, 700, M, zf=pond_depth, avoid=lambda x, y: pond_depth(x, y) < -0.2 or (37 < x < 64.6 and 4 < y < 10.6))
    leaves(g, GATE_X, 98.0, -1.3, 1.3, 260, M, zf=lambda x, y: 0.02, avoid=lambda x, y: BRIDGE_X0 < x < BRIDGE_X1)
    for (x, y, rx, ry) in ((40.5, -0.4, 0.9, 0.4), (55.0, 0.5, 1.2, 0.45), (63.5, -0.6, 0.7, 0.35), (96.0, 0.3, 0.9, 0.4), (47.0, 2.4, 0.8, 0.5), (59.0, 3.0, 1.0, 0.4)):
        puddle(g, x, y, rx, ry, M, z=0.03 if abs(y) < 1.3 else 0.01)
    # the garden's front: plant pots on the patio's edge, a wheelbarrow, a watering can
    for (x, y, r) in ((1.0, -3.0, 0.22), (1.6, -3.1, 0.16), (6.6, -3.0, 0.2)):
        g.cyl((x, y, r * 0.8), r, r * 1.6, M["concrete"], n=12, r2=r * 0.75)
        bush(g, (x, y, r * 1.5), r * 1.1, M)
    def barrow(t):
        t.prism([(-0.5, 0.25), (0.45, 0.25), (0.65, 0.6), (-0.7, 0.6)], -0.32, 0.32, M["steel_mid"])
        t.cyl((0.75, 0, 0.18), 0.18, 0.08, M["tyre"], rot=(90, 0, 0), n=12)
        for s in (-1, 1):
            t.beam((0.75, s * 0.05, 0.18), (-1.2, s * 0.3, 0.45), 0.03, 0.03, M["steel"])
            t.box((-0.4, s * 0.28, 0.13), (0.03, 0.03, 0.26), M["steel"])
    sub(g, barrow, (14.5, -3.0, 0), yaw=-20)
    g.cyl((18.2, -3.2, 0.14), 0.12, 0.28, M["steel_mid"], n=10)
    limb(g, (18.3, -3.2, 0.2), (18.6, -3.2, 0.33), 0.02, 0.012, M["steel_mid"], n=5)
    tufts(g, 7.5, 33.8, -6.0, -3.0, 70, M, h=0.22)
    # beyond the camera: the garden's front fence, the park's railings, trees, the houses behind them
    close_board(g, -9.0, GATE_X, -24.0, M)
    railings(g, GATE_X, 98.0, -26.0, M, h=1.6, gap=0.15)
    for k in range(9):
        tree(g, 38.0 + k * 7.0 + rnd.uniform(-2, 2), rnd.uniform(-22.0, -17.0), rnd.uniform(8.0, 11.0), M, leafy=0.5)
    for k in range(4):
        tree(g, 4.0 + k * 8.0, rnd.uniform(-22.0, -19.0), rnd.uniform(6.0, 8.0), M, leafy=0.5)
    for k in range(14):
        x = -10.0 + k * 10.0
        houses_row(g, x, x + 9.6, -36.0, -1, M)
    return g.finish(coll)


def houses_row(g, x0, x1, y, face, M):
    """A pair of terraced houses seen from behind (face -1: facing +y; +1: facing -y): brick, a
    pitched roof, chimneys, a lit window or two, a fence in front."""
    d = 7.0
    yb = y - face * d
    y0, y1 = min(y, yb), max(y, yb)
    g.box(((x0 + x1) / 2, (y0 + y1) / 2, 2.9), (x1 - x0, d, 5.8), M["brick"])
    pitched_roof(g, x0, x1, y0, y1, 5.8, 8.6, M, along="x", courses=False, over=0.25)
    for cx in (x0 + 0.5, (x0 + x1) / 2):
        g.box((cx, (y0 + y1) / 2, 9.2), (0.9, 0.6, 1.6), M["brick"])
        g.cyl((cx - 0.2, (y0 + y1) / 2, 10.15), 0.12, 0.35, M["concrete"], n=8)
    yf = y if face < 0 else y
    for k, wx in enumerate((x0 + 1.4, x0 + 3.4, x0 + 6.2, x0 + 8.2)):
        for wz in (1.5, 4.0):
            lit = rnd.random() < 0.18
            g.box((wx, yf + face * 0.0 - (-face) * 0.02, wz), (1.0, 0.06, 1.15), M["lit_dim"] if lit else M["glass"])
            g.box((wx, yf - (-face) * 0.05, wz - 0.62), (1.2, 0.1, 0.06), M["coping"])
    # an extension on one, a shed and a fence in front of each
    g.box((x0 + 2.4, y - face * -2.0, 1.4), (3.0, 4.0, 2.8), M["brick"])
    g.box((x0 + 2.4, y - face * -2.0, 2.9), (3.2, 4.2, 0.2), M["gutter"])


def build_back(coll, M):
    """Behind the lane: the gardens' back fences and trees beyond, the park's railings and its tall
    trees, a row of houses backing onto it (lit windows), the town's roofs further back."""
    g = S.Group("ParkBack")
    # the park's back railings, the ones the men climb over, and a hedge behind them
    railings(g, GATE_X, 98.0, 12.6, M, h=1.85, gap=0.14)
    hedge(g, GATE_X, 98.0, 14.2, 2.2, 1.6, M)
    # the park's big trees, behind the play area and round the pond
    for (x, y, h) in ((37.5, 11.8, 10.5), (46.0, 15.5, 12.0), (55.0, 12.0, 9.5), (63.5, 15.5, 11.5), (70.0, 12.5, 10.0), (78.0, 15.8, 12.5),
                      (95.5, 12.0, 10.0), (97.0, 17.0, 11.0)):
        tree(g, x, y, h, M, leafy=0.5, spread=1.1)
    # behind our garden: the neighbours' gardens, their sheds and trees, then their houses
    close_board(g, -9.6, GATE_X, 21.0, M)
    for (x, y, h) in ((2.0, 17.5, 7.0), (14.0, 18.5, 8.5), (26.0, 16.5, 6.0)):
        tree(g, x, y, h, M, leafy=0.6)
    for x in (5.0, 21.0):
        g.box((x, 18.0, 1.1), (2.4, 2.0, 2.2), M["timber"])
        g.box((x, 18.0, 2.3), (2.7, 2.3, 0.1), M["gutter"], rot=(6, 0, 0))
    for k in range(13):
        x = -26.0 + k * 10.0
        houses_row(g, x, x + 9.6, 24.0, 1, M)
    for k in range(5):
        x = 100.0 + k * 10.0
        houses_row(g, x, x + 9.6, 26.0, 1, M)
    # the town further back: rooftops, chimneys, a church spire, lit windows
    for k in range(46):
        x = -40.0 + k * 6.5 + rnd.uniform(-1, 1)
        y = rnd.uniform(48, 80)
        h = rnd.uniform(6, 13)
        w = rnd.uniform(5.0, 7.5)
        g.box((x, y, h / 2), (w, 6.0, h), M["town"])
        g.cyl((x, y, h + 1.2), w * 0.55, 2.4, M["town"], rot=(90, 0, 0), n=3)
        for _ in range(rnd.randint(0, 3)):
            g.box((x + rnd.uniform(-w / 3, w / 3), y - 3.02, rnd.uniform(1.5, h - 1)), (0.7, 0.05, 0.9), M["town_win"])
    sx, sy = 66.0, 70.0
    g.box((sx, sy, 9.0), (5.0, 5.0, 18.0), M["town"])
    g.cyl((sx, sy, 24.0), 2.6, 12.0, M["town"], n=4, r2=0.05, rot=(0, 0, 45))
    g.box((sx, sy - 2.52, 13.0), (0.9, 0.05, 1.8), M["town_win"])
    g.cyl((sx, sy - 2.55, 16.0), 0.6, 0.05, M["sign"], rot=(90, 0, 0), n=16)
    return g.finish(coll)


def build_hill(coll, M):
    """Kessel Hill beyond the car park, at the end of the long sightline: the hill rising out of
    the dark, the road's hairpins up it with a few lamps, and on its crown the fortress, lit: the
    curtain wall with battlements, towers, the keep's windows, the observatory's dome."""
    g = S.Group("ParkHill")
    cx, cy = HILL_C
    h = hill_h
    tmp = bmesh.new()
    x0, x1, y0, y1, res = 166.0, 322.0, -30.0, 100.0, 3.0
    nx, ny = int((x1 - x0) / res), int((y1 - y0) / res)
    rows = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = x0 + (x1 - x0) * i / nx
            y = y0 + (y1 - y0) * j / ny
            row.append(tmp.verts.new((x, y, h(x, y))))
        rows.append(row)
    for j in range(ny):
        for i in range(nx):
            tmp.faces.new((rows[j][i], rows[j][i + 1], rows[j + 1][i + 1], rows[j + 1][i]))
    me = bpy.data.meshes.new("hill")
    tmp.to_mesh(me)
    tmp.free()
    for p in me.polygons:
        p.material_index = g._mi(M["hill"])
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    # the lower slopes between: dark fields, hedgerows, a farm's lit window
    ground(g, 140.0, 167.0, 60.0, 120.0, M["far"], 4.0, h=lambda x, y: ramp(x) - 0.1, amp=1.0, flat=0.0, seed=9.0)
    ground(g, 166.0, 322.0, 100.0, 140.0, M["far"], 5.0, h=lambda x, y: ramp(x) - 0.1, amp=1.5, flat=0.0, seed=10.0)
    for k in range(30):
        x, y = rnd.uniform(172, 320), rnd.uniform(-5, 110)
        if math.hypot((x - cx) / HILL_R[0], (y - cy) / HILL_R[1]) < 0.5:
            continue
        z = h(x, y)
        D.pine(g, x, y, z - 0.3, rnd.uniform(6, 10), M["far"], M["far"])
    # the road's hairpins up the hill, its lamps
    road = []
    for k in range(60):
        t = k / 59
        a = t * math.pi * 4.0
        x = 174.0 + t * 38.0 + 7.0 * math.sin(a)
        y = 14.9 + t * 14.0 - 5.0 * (1 - math.cos(a))
        road.append(Vector((x, y, h(x, y) + 0.4)))
    for i in range(len(road) - 1):
        g.beam(road[i][:], road[i + 1][:], 5.0, 0.3, M["tarmac"])
        if i % 9 == 4:
            p = road[i]
            g.cyl((p.x, p.y + 3.0, p.z + 3.0), 0.12, 6.0, M["steel_mid"], n=6)
            g.box((p.x, p.y + 2.5, p.z + 6.0), (0.8, 1.0, 0.2), M["sodium"])
    # the fortress on the crown (built at its own origin, set on the crown at three-quarter scale)
    def fortress(g):
        fx, fy, fz = 0.0, 0.0, 0.0
        wall = 26.0
        for s in (-1, 1):
            g.box((fx, fy + s * wall / 2, fz + 6.0), (wall, 2.0, 12.0), M["fort"])
            g.box((fx + s * wall / 2, fy, fz + 6.0), (2.0, wall, 12.0), M["fort_y"])
            D.merlons(g, fx - wall / 2, fx + wall / 2, fy + s * wall / 2, fz + 12.0, 2.0, M["fort"], w=0.9, gap=0.8, h=1.2)
        for (dx, dy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            D.tower(g, (fx + dx * wall / 2, fy + dy * wall / 2, fz), 3.2, 17.0, M["fort"], M["fort"], M["steel"], slits=3, crenel=True)
        g.box((fx + 2.0, fy + 4.0, fz + 14.0), (12.0, 10.0, 28.0), M["fort"])                          # the keep
        D.merlons(g, fx - 4.0, fx + 8.0, fy - 1.0, fz + 28.0, 1.0, M["fort"], w=0.9, gap=0.8, h=1.2)
        tx, ty = fx - 6.0, fy + 6.0                                                                    # the observatory tower and dome
        g.cyl((tx, ty, fz + 17.0), 3.6, 34.0, M["fort"], n=20)
        for k in range(8):
            a0, a1 = k / 8 * math.pi / 2, (k + 1) / 8 * math.pi / 2
            r0, r1 = 4.0 * math.cos(a0), 4.0 * math.cos(a1)
            g.cyl((tx, ty, fz + 34.2 + 4.0 * (math.sin(a0) + math.sin(a1)) / 2), (r0 + r1) / 2, 4.0 * (math.sin(a1) - math.sin(a0)) + 0.05, M["steel_mid"], n=20)
        g.box((tx, ty - 3.4, fz + 36.0), (1.0, 1.0, 3.6), M["fort_win"])                               # the slit, lit
        g.cyl((tx, ty, fz + 34.3), 4.3, 0.5, M["fort"], n=20)
        # the windows: the keep's rows, the towers' slits, the gatehouse; all facing the town (-y and -x)
        for j in range(5):
            for k in range(4):
                if rnd.random() < 0.7:
                    g.box((fx - 2.5 + k * 3.0, fy - 1.05, fz + 8.0 + j * 4.0), (0.9, 0.1, 1.8), M["fort_win"])
            if rnd.random() < 0.7:
                g.box((fx - 4.05, fy + 1.0 + (j % 3) * 3.0, fz + 8.0 + j * 4.0), (0.1, 0.9, 1.8), M["fort_win"])
        for k in range(8):
            g.box((fx - wall / 2 + 2.0 + k * 3.0, fy - wall / 2 - 1.05, fz + 7.5), (0.6, 0.1, 1.2), M["fort_win"])
            g.box((fx - wall / 2 - 1.05, fy - wall / 2 + 2.0 + k * 3.0, fz + 7.5), (0.1, 0.6, 1.2), M["fort_win"])
        for k in range(10):
            a = k / 10 * math.tau
            g.box((tx + math.cos(a) * 3.62, ty + math.sin(a) * 3.62, fz + 28.0), (0.6, 0.6, 1.4), M["fort_win"], rot=(0, 0, math.degrees(a)))
        g.box((fx - wall / 2 - 1.1, fy, fz + 3.0), (0.3, 4.0, 6.0), M["fort_win"])                    # the gate, lit

    sub(g, fortress, (cx, cy, h(cx, cy) - 0.8), scale=0.75)
    return g.finish(coll)


def build_sky(coll, M):
    """The night sky as a wall of panels round the set (inside faces), the moon, the storm gathering
    over Kessel Hill: masses of cloud, lit from inside here and there; the far hills' line."""
    g = S.Group("ParkSky")
    # the panels: behind (y 175), at the far end (x 330), and round the corners
    g.box((80.0, 175.0, 60.0), (520.0, 1.0, 180.0), M["sky"])
    g.box((324.0, 60.0, 60.0), (1.0, 240.0, 180.0), M["sky"])
    g.box((-100.0, 40.0, 60.0), (1.0, 280.0, 180.0), M["sky"])
    g.box((80.0, -80.0, 60.0), (520.0, 1.0, 180.0), M["sky"])
    g.box((300.0, -60.0, 60.0), (80.0, 1.0, 180.0), M["sky"], rot=(0, 0, 45))
    # the moon, high over the town, and its halo
    g.cyl((22.0, 172.0, 50.0), 4.2, 0.3, M["moon"], rot=(90, 0, 0), n=32)
    g.cyl((22.0, 173.0, 50.0), 8.0, 0.2, M["cloud_lit"], rot=(90, 0, 0), n=32)
    # the far hills along the horizon
    for k in range(40):
        x = -100 + k * 11.0
        if 200 < x < 310:
            continue
        hh = rnd.uniform(10, 24)
        blob(g, (x, rnd.uniform(130, 160), hh * 0.2), hh, M["far"], s=(1.6, 0.6, 0.5), subdiv=1, rough=0.2)
    # the storm gathering over the fortress: banks of heavy cloud, lit from within here and there
    cx, cy = HILL_C
    for (bx, by, bz, n) in ((-35, 30, 46, 9), (5, 40, 52, 10), (40, 25, 46, 8), (-10, 70, 58, 8), (30, 65, 62, 7)):
        for k in range(n):
            c = (cx + bx + rnd.uniform(-22, 22), cy + by + rnd.uniform(-10, 10), HILL_H + bz + rnd.uniform(-4, 6))
            blob(g, c, rnd.uniform(7, 13), M["cloud"], s=(1.7, 1.2, 0.6), subdiv=2, rough=0.45)
    for (bx, by, bz) in ((5, 44, 49), (-30, 34, 44)):                                              # lit from within, low in the banks
        blob(g, (cx + bx, cy + by, HILL_H + bz), 9.0, M["cloud_lit"], s=(1.6, 1.2, 0.5), subdiv=2, rough=0.4)
    # thinner cloud over the town, drifting
    for k in range(18):
        c = (rnd.uniform(-80, 200), rnd.uniform(110, 165), rnd.uniform(60, 95))
        blob(g, c, rnd.uniform(8, 16), M["cloud"], s=(2.4, 1.0, 0.3), subdiv=1, rough=0.3)
    # stars
    st = []
    for _ in range(400):
        x, z = rnd.uniform(-160, 320), rnd.uniform(40, 145)
        st.append((x, 174.4, z))
    tmp = bmesh.new()
    for (x, y, z) in st:
        s = rnd.uniform(0.08, 0.2)
        vs = [tmp.verts.new((x - s, y, z - s)), tmp.verts.new((x + s, y, z - s)), tmp.verts.new((x + s, y, z + s)), tmp.verts.new((x - s, y, z + s))]
        tmp.faces.new(vs)
    me = bpy.data.meshes.new("stars")
    tmp.to_mesh(me)
    tmp.free()
    for p in me.polygons:
        p.material_index = g._mi(M["moon"])
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    return g.finish(coll)


# ---------------------------------------------------------------- light

def add_light(kind, name, loc, energy, size=1.0, rot=None):
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


def lights():
    """Night after rain: a low moon behind the houses, the park lamps, the car park's sodium heads,
    the kitchen's light spilling out of the window and the open door."""
    moon = add_light("SUN", "Moon", (0, 0, 50), 0.6)
    moon.rotation_euler = (math.radians(58), 0, math.radians(165))
    for (x, y) in LAMPS:
        add_light("POINT", "ParkLamp", (x, y, LAMP_H + 0.25), 170, size=0.2)
    for (x, y) in CAR_LAMPS:
        for s in (-1, 1):
            add_light("POINT", "CarparkLamp", (x + s * 1.0, y, CAR_LAMP_H - 0.2), 380, size=0.2)
    for p in (road_points()[6], road_points()[18], road_points()[30]):
        add_light("POINT", "RoadLamp", (p.x, p.y + 1.6, p.z + 5.7), 260, size=0.2)
    add_light("AREA", "KitchenWindow", (0.25, 1.8, 1.5), 140, size=1.2, rot=(0, math.radians(-90), 0))
    add_light("AREA", "KitchenDoor", (0.25, -0.3, 1.1), 220, size=1.0, rot=(0, math.radians(-90), 0))
    add_light("POINT", "Landing", (1.5, 5.2, 3.9), 30, size=0.4)
    for x in (20.0, 24.5, 29.0, 33.0):
        add_light("POINT", "FairyLights", (x, 12.2, 1.8), 14, size=0.6)
    for k in range(6):
        t = (k + 0.5) / 6
        add_light("POINT", "SolarLight", (8.6 + t * 8.6 + 0.5, 2.0 + t * 5.8 + 0.4 * math.sin(t * 3.0) + 0.45, 0.4), 1.2, size=0.05)
    add_light("POINT", "BarrierBox", (BARRIER_X - 1.6, SIGHT_Y + 3.3, 1.0), 12, size=0.2)
    add_light("POINT", "VanInside", (VAN_BACK + 0.9, VAN_Y, 1.1), 2.5, size=0.2)
    for x in (20.0, 65.0, 115.0):
        add_light("AREA", "Fill", (x, -16.0, 7.0), 900, size=50.0, rot=(math.radians(74), 0, 0))
    add_light("AREA", "BackFill", (60.0, -20.0, 24.0), 1600, size=140.0, rot=(math.radians(60), 0, 0))
    add_light("AREA", "HillFill", (200.0, -20.0, 40.0), 2200, size=120.0, rot=(math.radians(55), 0, math.radians(-30)))
    w = bpy.data.worlds.new("Night")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.035, 0.038, 0.05, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def lightning_lights():
    """A far strike behind Kessel Hill: the storm clouds and the hill's crown thrown into white."""
    cx, cy = HILL_C
    for (dx, dy) in ((-20, 30), (25, 50)):
        add_light("AREA", "Strike", (cx + dx, cy + dy, HILL_H + 40.0), 160000, size=30.0, rot=(math.radians(-150), 0, 0))
    add_light("POINT", "StrikeCore", (cx - 10, cy + 20, HILL_H + 32.0), 400000, size=8.0)


# ---------------------------------------------------------------- the sightline

def check_sightline(cam_pos, target):
    """Ray cast from the camera at the swings to the van's back window: report what (if anything)
    stands in the way."""
    dg = bpy.context.evaluated_depsgraph_get()
    a, b = Vector(cam_pos), Vector(target)
    d = (b - a)
    L = d.length
    hit, loc, nrm, idx, ob, mat = bpy.context.scene.ray_cast(dg, a, d.normalized(), distance=L - 0.15)
    if hit:
        print(f"SIGHTLINE BLOCKED by {ob.name} at {tuple(round(v, 2) for v in loc)}")
    else:
        print(f"SIGHTLINE CLEAR: {tuple(round(v, 2) for v in a)} -> {tuple(round(v, 2) for v in b)} ({L:.1f} m)")
    return not hit


# ---------------------------------------------------------------- bake and export

def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "ParkSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported ParkSet.fbx")


def main():
    args = sys.argv
    preview_only = "--preview" in args
    lit = "--lightning-preview" in args
    only = None
    for a in args:
        if a.startswith("--only="):
            only = {int(v) for v in a[7:].split(",") if v}
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    house = build_house(coll, M)
    garden, garden_g = build_garden(coll, M), build_garden_ground(coll, M)
    play, lawn, pond = build_play(coll, M), build_lawn(coll, M), build_pond(coll, M)
    carpark, carpark_g = build_carpark(coll, M), build_carpark_ground(coll, M)
    fore, back, hill, sky = build_fore(coll, M), build_back(coll, M), build_hill(coll, M), build_sky(coll, M)
    swing = build_swing(coll, M)
    van = build_van(coll, M)
    door_l, door_r = build_van_door(coll, M, 1), build_van_door(coll, M, -1)
    for d in (door_l, door_r):                                                                    # the doors ride with the van
        d.parent = van
        d.matrix_parent_inverse = Matrix.Translation(-van.location)
    arm = build_barrier_arm(coll, M)
    for o in (house, van, door_l, door_r):                                                         # the big hard edges catch the light
        D.bevel(o, 0.012)
    parts = [house, garden, garden_g, play, lawn, pond, carpark, carpark_g, fore, back, hill, sky, swing, van, door_l, door_r, arm]
    tris = 0
    for o in parts:
        n = sum(len(p.vertices) - 2 for p in o.data.polygons)
        tris += n
        print(f"  {o.name:20s} {n:8d} tris")
    print(f"  {'total':20s} {tris:8d} tris")
    lights()
    win = (VAN_BACK - 0.06, VAN_Y + 0.5, 1.78)
    if preview_only or lit:
        bpy.context.view_layer.update()
        check_sightline((SWING_X + 2.6, SWING_Y, 1.45), win)
        check_sightline((SWING_X - 0.8, SWING_Y - 0.6, 1.2), win)
        if lit:
            lightning_lights()
        U = S.U
        fort = Vector((HILL_C[0], HILL_C[1], HILL_H + 14.0))
        cams = [
            ("4.1  the small footprints across the wet lawn", S.shot_camera("P1", (5.0, 0.55, -2.6), (13.0, 0.35, 2.2), 46), None),
            ("Y7  the garden (gameplay)", S.shot_camera("P2", (17.0, 2.0, -12.0), (17.0, 1.3, 1.0), 42), None),
            ("4.2  the swings, Maya's play area", S.shot_camera("P3", (23.6, 1.3, -2.4), (27.4, 1.0, 3.2), 50), None),
            ("4.7  long lens: down the park to the van's window", S.shot_camera("P4", (SWING_X + 2.6, 1.45, SWING_Y), (VAN_BACK, 1.75, VAN_Y), 3.2), None),
            ("4.7  wide: the park, the van, Kessel Hill", S.shot_camera("P5", (SWING_X + 1.0, 1.6, SWING_Y), (150.0, 9.0, 12.0), 20), None),
            ("Y8  the playground (gameplay)", S.shot_camera("P6", (50.0, 2.0, -12.0), (50.0, 1.3, 1.0), 42), None),
            ("4.5  the tall slide, the gap under it", S.shot_camera("P7", (SLIDE_X - 1.2, 0.6, SLIDE_Y - 2.4), (SLIDE_X + 0.1, 0.7, SLIDE_Y), 56), None),
            ("Y9  the pond, the footbridge (gameplay)", S.shot_camera("P8", (82.0, 2.0, -12.0), (82.0, 1.0, 1.0), 42), None),
            ("Y10  the car park, the van (gameplay)", S.shot_camera("P9", (118.0, 2.0, -12.0), (118.0, 1.3, 1.0), 42), None),
            ("4.11  the hill road, the van gone", S.shot_camera("P10", (122.0, 1.6, 0.2), (175.0, 6.0, 14.0), 30), None),
            ("4.15  the fortress on Kessel Hill, the storm", S.shot_camera("P11", (126.0, 1.2, -1.0), (fort.x, fort.z, fort.y), 24), None),
            ("Y2  the back of No. 14", S.shot_camera("P12", (9.0, 1.7, -5.5), (0.5, 2.2, 1.8), 54), None),
            ("(overview)", S.shot_camera("P13", (60.0, 60.0, -70.0), (130.0, 0.0, 30.0), 60), None),
            ("(down the set, high)", S.shot_camera("P14", (10.0, 25.0, -20.0), (220.0, 20.0, 50.0), 50), None),
        ]
        cd = bpy.data.cameras.new("Top")
        cd.type = "ORTHO"
        cd.ortho_scale = 380.0
        cd.clip_end = 1000
        top = bpy.data.objects.new("Top", cd)
        top.location = (150.0, 45.0, 400.0)
        bpy.context.scene.collection.objects.link(top)
        cams.append(("(top, debug)", top, None))
        if only:
            cams = [c for i, c in enumerate(cams, 1) if i in only]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        name = "park_set_lightning.png" if lit else ("park_set.png" if not only else "park_set_part.png")
        for a in args:
            if a.startswith("--sheet="):
                name = a[8:]
        S.preview(cams, name)
        return
    for o in parts:
        S.unwrap(o)
    S.bake(house, 4096, 20)
    S.bake(garden, 4096, 20)
    S.bake(garden_g, 2048, 16)
    S.bake(play, 4096, 20)
    S.bake(lawn, 4096, 16)
    S.bake(pond, 4096, 16)
    S.bake(carpark, 4096, 16)
    S.bake(carpark_g, 4096, 16)
    S.bake(fore, 4096, 16)
    S.bake(back, 4096, 12)
    S.bake(hill, 2048, 12)
    S.bake(sky, 2048, 8)
    S.bake(swing, 512, 12)
    S.bake(van, 2048, 16)
    S.bake(door_l, 512, 12)
    S.bake(door_r, 512, 12)
    S.bake(arm, 256, 8)
    # the horizon lit by a far strike (the game flickers to it: LightningSwap)
    lightning_lights()
    S.bake(hill, 1024, 8, "_Lightning")
    S.bake(sky, 1024, 6, "_Lightning")
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(parts + marks)


if __name__ == "__main__":
    main()
