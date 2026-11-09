"""Detail kit for the story sets: surfaces with real texture (cut stone with mortar, cobbles,
weathered rock, planks, wet ground), bevelled edges that catch the light, noise terrain, rocks,
pines, towers with battlements and arrow slits, arches. All of it bakes, so the game stays one
unlit texture per object.

Materials are procedural (Blender shader nodes) and stay in the game's black and white: every
colour is a grey. They read object-space coordinates, so a merged group keeps its pattern scale
whatever it is attached to. `plane` picks which two axes the pattern runs along: "xz" for walls
facing the camera, "yz" for walls across the lane, "xy" for floors."""
import math
import random

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

rnd = random.Random(7)
MATS = {}


# ---------------------------------------------------------------- materials

def _base(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    return m, nt, nt.nodes["Principled BSDF"]


def _coords(nt, plane, scale):
    """Object coordinates, the two pattern axes picked out and scaled."""
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    comb = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    a, b = {"xz": ("X", "Z"), "yz": ("Y", "Z"), "xy": ("X", "Y")}[plane]
    nt.links.new(sep.outputs[a], comb.inputs["X"])
    nt.links.new(sep.outputs[b], comb.inputs["Y"])
    nt.links.new(sep.outputs["Z" if plane == "xy" else ("Y" if plane == "xz" else "X")], comb.inputs["Z"])
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (scale, scale, scale)
    nt.links.new(comb.outputs[0], mp.inputs[0])
    return mp.outputs[0]


def _grey(g):
    return (g, g, g * 1.02, 1)


def stone(name, g=0.34, mortar=0.12, plane="xz", block=(1.05, 0.46), rough=0.85, wet=0.0):
    """Cut stone blocks in courses, each block a slightly different grey, dark mortar between,
    a grain of noise over all of it. wet: darker, glossier (rain)."""
    key = (name, plane)
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _base(f"{name}_{plane}")
    co = _coords(nt, plane, 1.0)
    br = nt.nodes.new("ShaderNodeTexBrick")
    br.inputs["Color1"].default_value = _grey(g * (1.0 - 0.25 * wet))
    br.inputs["Color2"].default_value = _grey(g * 0.62 * (1.0 - 0.25 * wet))
    br.inputs["Bias"].default_value = 0.0
    br.inputs["Mortar"].default_value = _grey(mortar * (1.0 - 0.3 * wet))
    br.inputs["Scale"].default_value = 1.0
    br.inputs["Mortar Size"].default_value = 0.03
    br.inputs["Mortar Smooth"].default_value = 0.4
    br.inputs["Brick Width"].default_value = block[0]
    br.inputs["Row Height"].default_value = block[1]
    br.offset = 0.5
    nt.links.new(co, br.inputs["Vector"])
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 9.0
    nz.inputs["Detail"].default_value = 6.0
    nt.links.new(co, nz.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.6
    nt.links.new(br.outputs["Color"], mix.inputs["A"])
    nt.links.new(nz.outputs["Color"], mix.inputs["B"])
    # weathering: big soft patches of grime over the blocks
    grime = nt.nodes.new("ShaderNodeTexNoise")
    grime.inputs["Scale"].default_value = 0.6
    grime.inputs["Detail"].default_value = 4.0
    nt.links.new(co, grime.inputs["Vector"])
    mix2 = nt.nodes.new("ShaderNodeMix")
    mix2.data_type = "RGBA"
    mix2.blend_type = "MULTIPLY"
    mix2.inputs["Factor"].default_value = 0.5
    nt.links.new(mix.outputs["Result"], mix2.inputs["A"])
    nt.links.new(grime.outputs["Color"], mix2.inputs["B"])
    nt.links.new(mix2.outputs["Result"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.5
    nt.links.new(br.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = rough * (1.0 - 0.6 * wet)
    MATS[key] = m
    return m


def cobbles(name, g=0.24, wet=0.6):
    """Round-topped cobbles (a Voronoi of stones), dark joints, a wet sheen."""
    key = (name, "xy")
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _base(name)
    co = _coords(nt, "xy", 3.2)
    vo = nt.nodes.new("ShaderNodeTexVoronoi")
    vo.feature = "DISTANCE_TO_EDGE"
    nt.links.new(co, vo.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = _grey(g * 0.25)
    ramp.color_ramp.elements[1].position = 0.12
    ramp.color_ramp.elements[1].color = _grey(g)
    nt.links.new(vo.outputs["Distance"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.5
    nt.links.new(vo.outputs["Distance"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.85 - 0.6 * wet
    MATS[key] = m
    return m


def rock(name, g=0.2, plane="xz"):
    """Weathered rock: layered noise, cracks (a stretched Voronoi), lichen-pale patches."""
    key = (name, plane)
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _base(f"{name}_{plane}")
    co = _coords(nt, plane, 0.6)
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 2.5
    nz.inputs["Detail"].default_value = 10.0
    nz.inputs["Roughness"].default_value = 0.65
    nt.links.new(co, nz.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = _grey(g * 0.45)
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[1].color = _grey(g * 1.5)
    nt.links.new(nz.outputs["Fac"], ramp.inputs["Fac"])
    vo = nt.nodes.new("ShaderNodeTexVoronoi")
    vo.feature = "DISTANCE_TO_EDGE"
    vo.inputs["Scale"].default_value = 3.0
    nt.links.new(co, vo.inputs["Vector"])
    crack = nt.nodes.new("ShaderNodeMath")
    crack.operation = "GREATER_THAN"
    crack.inputs[1].default_value = 0.03
    nt.links.new(vo.outputs["Distance"], crack.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    nt.links.new(ramp.outputs["Color"], mix.inputs["A"])
    nt.links.new(crack.outputs[0], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.6
    nt.links.new(nz.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.9
    MATS[key] = m
    return m


def planks(name, g=0.16, plane="xz", board=0.22):
    """Timber: boards with dark seams and grain."""
    key = (name, plane)
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _base(f"{name}_{plane}")
    co = _coords(nt, plane, 1.0)
    br = nt.nodes.new("ShaderNodeTexBrick")
    br.inputs["Color1"].default_value = _grey(g)
    br.inputs["Color2"].default_value = _grey(g * 0.8)
    br.inputs["Mortar"].default_value = _grey(g * 0.3)
    br.inputs["Mortar Size"].default_value = 0.01
    br.inputs["Brick Width"].default_value = 2.4
    br.inputs["Row Height"].default_value = board
    nt.links.new(co, br.inputs["Vector"])
    wave = nt.nodes.new("ShaderNodeTexWave")
    wave.inputs["Scale"].default_value = 3.0
    wave.inputs["Distortion"].default_value = 6.0
    nt.links.new(co, wave.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 0.25
    nt.links.new(br.outputs["Color"], mix.inputs["A"])
    nt.links.new(wave.outputs["Color"], mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.7
    MATS[key] = m
    return m


def flat(name, g, rough=0.7, metal=0.0, emit=0.0):
    key = (name, "flat")
    if key in MATS:
        return MATS[key]
    m, nt, bsdf = _base(name)
    bsdf.inputs["Base Color"].default_value = _grey(g)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = (1, 0.97, 0.92, 1)
        bsdf.inputs["Emission Strength"].default_value = emit
    MATS[key] = m
    return m


# ---------------------------------------------------------------- shapes

def bevel(ob, width=0.025):
    """Round every hard edge a little (they catch the light in the bake), applied before unwrap."""
    for o in bpy.context.scene.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    mod = ob.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = 1
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(50)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def terrain(g, x0, x1, y0, y1, height, m, res=0.5, amp=0.35, seed=0.0):
    """A ground surface: a grid whose height is height(x, y) plus layered noise."""
    tmp = bmesh.new()
    nx, ny = max(2, int((x1 - x0) / res)), max(2, int((y1 - y0) / res))
    vs = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = x0 + (x1 - x0) * i / nx
            y = y0 + (y1 - y0) * j / ny
            z = height(x, y) + amp * noise.noise(Vector((x * 0.18 + seed, y * 0.18, 0.3)))
            row.append(tmp.verts.new((x, y, z)))
        vs.append(row)
    for j in range(ny):
        for i in range(nx):
            tmp.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
    g._merge(tmp, m)


def boulder(g, c, r, m, squash=0.7):
    """A rock: an icosphere pushed about by noise, flattened a little."""
    tmp = bmesh.new()
    bmesh.ops.create_icosphere(tmp, subdivisions=2, radius=r)
    s = rnd.uniform(0, 100)
    for v in tmp.verts:
        n = noise.noise(v.co * (1.6 / r) + Vector((s, s, s)))
        v.co *= 1.0 + 0.32 * n
        v.co.z *= squash
    bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector(c)), verts=tmp.verts)
    g._merge(tmp, m)


def pine(g, x, y, z, h, bark, needles):
    """A storm-bent pine: a trunk and five tiers of needles, leaning away from the wind."""
    lean = rnd.uniform(-0.12, 0.05)
    g.cyl((x, y, z + h * 0.35), 0.12 * h / 6, h * 0.7, bark, n=8)
    for k in range(5):
        t = k / 4
        r = (1.6 - 1.2 * t) * h / 7
        g.cyl((x + lean * h * t, y, z + h * (0.32 + 0.6 * t)), r, h * 0.26, needles, n=9, r2=r * 0.1)


def merlons(g, x0, x1, y, z, depth, m, w=0.5, gap=0.42, h=0.6):
    """Battlements along a wall top: the solid teeth, with gaps between."""
    x = x0
    while x + w <= x1 + 1e-3:
        g.box((x + w / 2, y, z + h / 2), (w, depth, h), m)
        x += w + gap


def tower(g, c, r, h, wall, roof, dark, slits=4, crenel=True):
    """A round tower: its wall, arrow slits, a corbelled top with battlements or a cone roof."""
    x, y, z = c
    g.cyl((x, y, z + h / 2), r, h, wall, n=24)
    g.cyl((x, y, z + h + 0.15), r + 0.2, 0.3, wall, n=24)
    for k in range(slits):
        a = math.pi + (k - (slits - 1) / 2) * 0.5
        sx, sy = x + math.sin(a) * (r + 0.02), y - abs(math.cos(a)) * (r + 0.02)
        for lvl in (0.35, 0.65):
            g.box((sx, sy, z + h * lvl), (0.1, 0.06, 0.9), dark)
    if crenel:
        for k in range(14):
            a = k / 14 * math.tau
            g.box((x + math.cos(a) * (r + 0.1), y + math.sin(a) * (r + 0.1), z + h + 0.6), (0.42, 0.42, 0.6), wall, rot=(0, 0, math.degrees(a)))
    else:
        g.cyl((x, y, z + h + 1.6), r + 0.35, 3.0, roof, n=24, r2=0.05)


def arch(g, x, y, w, h, depth, m, segs=9):
    """A round-headed arch: two jambs and a ring of voussoirs over an opening w wide, h to the spring."""
    t = 0.45
    for s in (-1, 1):
        g.box((x + s * (w / 2 + t / 2), y, h / 2), (t, depth, h), m)
    R = w / 2 + t / 2
    for k in range(segs):
        a0 = math.pi * k / segs
        a1 = math.pi * (k + 1) / segs
        am = (a0 + a1) / 2
        cx, cz = x + math.cos(am) * R, h + math.sin(am) * R
        g.box((cx, y, cz), (t * 1.05, depth, (a1 - a0) * R * 1.05), m, rot=(0, -math.degrees(am) + 90, 0))
