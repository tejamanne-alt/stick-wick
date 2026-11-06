"""Renders the hitman's flashback stills for the prologue (chapter 1): the fights he does not
remember, cut in for a few frames each when his hand closes on the knife.

They look like the game, in black and white: the fight mode's grey world (concrete, dark
steel, white trims, lamp cones, black windows pricked with white city lights) and its
figures exactly as the game builds them, flat and unlit, from the real rig in the game's
own clip poses: the hero ink-black with his two white eyes (no wound yet, this is before),
the men in flat greys (their fight-mode colours, desaturated), white slash trails on the
strikes. Rendered flat (emission only), then made pure greyscale.

    /home/user/.venv-blender/bin/python tools/blender/render_flashbacks.py [--quick] [1 2 ...]

Writes Assets/Resources/Story/Flash/flash_<n>.png (960x540; --quick renders small)."""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as B  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Story", "Flash")
QUICK = "--quick" in sys.argv
rnd = random.Random(5)

# the fight mode's palette (EnvironmentBuilder / WaveDirector), as greys
HERO = 0.02                       # Ink
EYE = 0.97                        # Eyes
CONCRETE = 0.58                   # Concrete
CONCRETE_DARK = 0.2
FRAME = 0.07                      # Frame / steel
METAL = 0.13
TRIM = 0.78                       # white edge highlights
CRATE = 0.22


def lum(r, g, b):
    return 0.299 * r + 0.587 * g + 0.114 * b


# enemy inks (WaveDirector) in grey: brawler teal / olive / amber / purple, sword red, gun blue
# (darkened: in fight mode their colour separates them from the grey walls; in black and white
# their value has to)
MEN = [0.08 + 0.42 * lum(*c) for c in ((0.10, 0.55, 0.55), (0.48, 0.22, 0.62), (0.72, 0.32, 0.14), (0.72, 0.07, 0.16), (0.18, 0.34, 0.72), (0.45, 0.52, 0.12))]


def lin(g):
    """A display grey (as the game's Unlit/Color shows it) to the linear value Blender wants."""
    return max(0.0, g) ** 2.2


# ------------------------------------------------------------------ materials (all flat)

_mats = {}


def flat(g, name=None):
    """Unlit flat grey, like the game's Unlit/Color."""
    key = ("flat", round(g, 4))
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new(name or f"Flat{g:.2f}")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (lin(g), lin(g), lin(g), 1)
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], o.inputs[0])
    _mats[key] = m
    return m


def concrete(g=CONCRETE, scale=1.0):
    """The fight mode's concrete: flat grey, mottled, with form-tie dots."""
    m = bpy.data.materials.new("Concrete")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.0 * scale
    noise.inputs["Detail"].default_value = 6
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[1].position = 0.75
    ramp.color_ramp.elements[0].color = (lin(g * 0.86), lin(g * 0.86), lin(g * 0.86), 1)
    ramp.color_ramp.elements[1].color = (lin(g * 1.06), lin(g * 1.06), lin(g * 1.06), 1)
    e = nt.nodes.new("ShaderNodeEmission")
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Object"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], e.inputs[0])
    nt.links.new(e.outputs[0], o.inputs[0])
    return m


def glow(g, alpha_top=0.0, alpha_bottom=0.35, name="Cone"):
    """A soft additive-looking light cone: white, fading from its source (top) to nothing."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (lin(g), lin(g), lin(g), 1)
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha_bottom
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z"], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], mul.inputs[0])
    nt.links.new(mul.outputs[0], mix.inputs[0])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(e.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], o.inputs[0])
    return m


def halo(g, strength, name="Halo"):
    """A round soft glow (a lamp, a moon's haze): bright in the middle, gone at the edge."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    grad = nt.nodes.new("ShaderNodeTexGradient")
    grad.gradient_type = "SPHERICAL"
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Location"].default_value = (0, 0, 0)
    mp.inputs["Scale"].default_value = (1.0, 1.0, 1.0)
    pw = nt.nodes.new("ShaderNodeMath")
    pw.operation = "POWER"
    pw.inputs[1].default_value = 2.0
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = strength
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (lin(g), lin(g), lin(g), 1)
    tr = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Object"], mp.inputs[0])
    nt.links.new(mp.outputs[0], grad.inputs[0])
    nt.links.new(grad.outputs["Fac"], pw.inputs[0])
    nt.links.new(pw.outputs[0], mul.inputs[0])
    nt.links.new(mul.outputs[0], mix.inputs[0])
    nt.links.new(tr.outputs[0], mix.inputs[1])
    nt.links.new(e.outputs[0], mix.inputs[2])
    nt.links.new(mix.outputs[0], o.inputs[0])
    return m


# ------------------------------------------------------------------ the scene

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 8 if QUICK else 32
    sc.cycles.use_denoising = False
    sc.render.resolution_x = 480 if QUICK else 960
    sc.render.resolution_y = 270 if QUICK else 540
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.render.filter_size = 1.2
    w = bpy.data.worlds.new("Night")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (lin(0.04), lin(0.04), lin(0.04), 1)
    sc.world = w
    return sc


def figure(pose_dict, at=(0, 0, 0), facing=1, tilt=0.0, prop=None, ink=HERO, hero=False, name="Fig"):
    """A stickman as the game builds him: the rig's body in one flat ink, two white eyes on the
    head (FighterFactory.BuildEyes: from the head's centre towards the face, one each side;
    the hero's narrowed), the pose from one of the game's clips. facing -1 turns him round;
    tilt (degrees, about the depth axis) throws the whole body (falls)."""
    arm = B.build_armature()
    body = B.build_mesh(arm)
    body.data.materials.clear()
    body.data.materials.append(flat(ink))
    arm.name = name
    bpy.context.view_layer.update()
    # the eyes, measured in the rest pose and carried by the head bone
    c = Vector((0, 0, B.HEAD_CENTER))
    for s in (1, -1):
        d = Vector((0.84, s * 0.52, 0.17)).normalized()
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, segments=16, ring_count=8)
        eye = bpy.context.object
        eye.data.materials.append(flat(EYE))
        eye.scale = (0.04, 0.04, 0.034 if hero else 0.07)
        eye.location = c + d * 0.2
        bpy.context.view_layer.update()
        mw = eye.matrix_world.copy()
        eye.parent = arm
        eye.parent_type = "BONE"
        eye.parent_bone = "head"
        bpy.context.view_layer.update()
        eye.matrix_world = mw
    B.apply_pose(arm, pose_dict)
    arm.location = at
    arm.rotation_euler = (0, math.radians(-tilt), 0 if facing > 0 else math.pi)
    if prop:
        p = B.add_prop(arm, "sword" if prop == "sword" else "gun")
        if prop == "knife":
            p.scale = (0.012, 0.13, 0.03)
            p.location = (0, 0.13 - 0.09, 0)
        p.data.materials.clear()
        p.data.materials.append(flat(ink))
    bpy.context.view_layer.update()
    return arm


def key(clip, i):
    """Key i of a hand-keyed clip, as a pose (negative i counts from the end)."""
    return dict(B.CLIPS[clip]["keys"][i][1])


def bone_tail(arm, bone):
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[bone].tail


def bone_head(arm, bone):
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[bone].head


def trail(arm, tip_bone, pivot_bone, sweep=-80, width=0.09, g=1.0):
    """The game's white slash trail: a tapered ribbon along the arc the limb tip just swept,
    about its joint, in the plane of the fight (thin at the tail, full at the tip)."""
    tip = bone_tail(arm, tip_bone)
    piv = bone_head(arm, pivot_bone)
    r = tip - piv
    facing = 1 if math.cos(arm.rotation_euler.z) > 0 else -1
    n = 18
    verts, faces = [], []
    for i in range(n):
        k = i / (n - 1)
        a = math.radians(sweep) * (1 - k) * facing
        p = piv + Matrix.Rotation(a, 3, "Y") @ r
        w = width * k ** 1.4
        verts += [p + Vector((0, -0.05, w * 0.5)), p + Vector((0, -0.05, -w * 0.5))]
        if i:
            j = 2 * i
            faces.append((j - 2, j - 1, j + 1, j))
    me = bpy.data.meshes.new("Trail")
    me.from_pydata([tuple(v) for v in verts], [], faces)
    ob = bpy.data.objects.new("Trail", me)
    ob.data.materials.append(flat(g))
    bpy.context.scene.collection.objects.link(ob)
    return ob


def box(loc, size, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = size
    o.data.materials.append(mat)
    return o


def plane(loc, size, mat, rot=(math.pi / 2, 0, 0)):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (size[0], size[1], 1)
    o.data.materials.append(mat)
    return o


def disc(loc, r, mat, rot=(math.pi / 2, 0, 0)):
    bpy.ops.mesh.primitive_circle_add(radius=1, vertices=48, fill_type="NGON", location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = (r, r, r)
    o.data.materials.append(mat)
    return o


def ball(loc, r, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=12, ring_count=8)
    o = bpy.context.object
    o.data.materials.append(mat)
    return o


def lamp(x, y, z, drop=2.4, spread=1.4):
    """A hanging lamp as in fight mode: a dark shade on a wire, a soft cone of light under it."""
    box((x, y, z + 0.6), (0.01, 0.01, 1.2), flat(FRAME))
    bpy.ops.mesh.primitive_cone_add(radius1=0.22, radius2=0.06, depth=0.14, location=(x, y, z))
    shade = bpy.context.object
    shade.data.materials.append(flat(0.05))
    ball((x, y, z - 0.07), 0.05, flat(1.0))
    bpy.ops.mesh.primitive_cone_add(radius1=spread, radius2=0.08, depth=drop, location=(x, y + 0.05, z - drop / 2 - 0.07), vertices=32)
    cone = bpy.context.object
    cone.data.materials.append(glow(0.9, 0.0, 0.3))
    disc((x, y - 0.2, z - 0.05), 0.5, halo(1.0, 0.8))


def night_window(cx, cz, wd, ht, y, lights=0.1, moon=None):
    """A black window in a steel frame, the city's lights in it as white points (and a moon)."""
    plane((cx, y, cz), (wd, ht, 1), flat(0.03))
    for i in range(int(wd * ht * 40 * lights)):
        x = cx + rnd.uniform(-wd / 2 + 0.05, wd / 2 - 0.05)
        z = cz - ht / 2 + ht * rnd.random() ** 1.6 * 0.75
        plane((x, y - 0.01, z), (0.035, 0.035, 1), flat(rnd.choice((0.9, 0.75, 1.0))))
    if moon:
        disc((cx + moon[0], y - 0.012, cz + moon[1]), moon[2], flat(0.96))
        disc((cx + moon[0], y - 0.011, cz + moon[1]), moon[2] * 3.2, halo(1.0, 0.25))
    fr = flat(FRAME)
    box((cx, y - 0.02, cz + ht / 2), (wd + 0.1, 0.06, 0.08), fr)
    box((cx, y - 0.02, cz - ht / 2), (wd + 0.1, 0.06, 0.1), fr)
    box((cx - wd / 2, y - 0.02, cz), (0.08, 0.06, ht), fr)
    box((cx + wd / 2, y - 0.02, cz), (0.08, 0.06, ht), fr)
    for k in (1, 2):
        box((cx - wd / 2 + wd * k / 3, y - 0.02, cz), (0.04, 0.06, ht), fr)


def wall(y=1.8, x0=-8, x1=8, height=6, g=CONCRETE):
    """The fight mode's back wall: concrete panels with seams and tie dots, a trim at the foot."""
    plane(((x0 + x1) / 2, y, height / 2), (x1 - x0, height, 1), concrete(g))
    seam = flat(g * 0.72)
    x = x0
    while x <= x1:
        box((x, y - 0.01, height / 2), (0.02, 0.01, height), seam)
        x += 2.0
    for z in (1.0, 2.0, 3.0):
        box(((x0 + x1) / 2, y - 0.01, z * 1.5), (x1 - x0, 0.01, 0.015), seam)


def floor(g=0.34, y0=-4, y1=4, x0=-10, x1=10, trim=True):
    plane(((x0 + x1) / 2, (y0 + y1) / 2, 0), (x1 - x0, y1 - y0, 1), concrete(g, 2.0), rot=(0, 0, 0))
    if trim:
        box(((x0 + x1) / 2, y1 - 0.02, 0.01), (x1 - x0, 0.04, 0.02), flat(TRIM))


def camera(loc, target, lens=35, roll=0.0):
    d = bpy.data.cameras.new("Cam")
    d.lens = lens
    o = bpy.data.objects.new("Cam", d)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)
    direction = Vector(target) - Vector(loc)
    o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    o.rotation_euler.rotate_axis("Z", math.radians(roll))
    bpy.context.scene.camera = o
    return o


def rain(n, x0, x1, y0, y1, z0, z1, g=0.72, slant=12):
    m = flat(g)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.0035, depth=0.45, vertices=4)
    base = bpy.context.object
    base.data.materials.append(m)
    base.rotation_euler = (0, math.radians(slant), 0)
    for _ in range(n):
        c = base.copy()
        c.location = (rnd.uniform(x0, x1), rnd.uniform(y0, y1), rnd.uniform(z0, z1))
        c.scale = (1, 1, rnd.uniform(0.6, 1.4))
        bpy.context.scene.collection.objects.link(c)
    base.location = (0, 0, -50)


def splatter(origin, direction, n, g=0.03, spread=0.5, reach=1.0):
    """Ink thrown off a hit (the game's ink spray): drops along an arc, bigger near the wound."""
    m = flat(g)
    d = Vector(direction).normalized()
    for i in range(n):
        k = rnd.random() ** 0.7
        p = Vector(origin) + d * (k * reach) + Vector((rnd.uniform(-1, 1), rnd.uniform(-0.3, 0.3), rnd.uniform(-1, 1))) * spread * k * 0.5
        p.z -= 0.25 * (k * reach) ** 2
        ball(p, 0.028 * (1.1 - k) + 0.006, m)


def skyline(y, n, x0, x1, hmin, hmax, g=0.1, lit=0.12):
    """Towers in flat grey with white window points, as through the fight mode's windows."""
    tower = flat(g)
    for i in range(n):
        w = rnd.uniform(2.0, 4.5)
        h = rnd.uniform(hmin, hmax)
        x = x0 + (x1 - x0) * (i + rnd.random() * 0.5) / n
        yy = y + rnd.uniform(0, 6)
        box((x, yy, h / 2 - 2), (w, 4, h), tower)
        cols, rows = int(w / 0.7), int(h / 1.0)
        for c in range(cols):
            for r in range(rows):
                if rnd.random() < lit:
                    plane((x - w / 2 + (c + 0.5) * w / cols, yy - 2.01, -2 + (r + 0.5) * h / rows), (0.28, 0.38, 1), flat(rnd.choice((0.85, 1.0))))


def render(n):
    os.makedirs(OUT, exist_ok=True)
    sc = bpy.context.scene
    path = os.path.join(OUT, f"flash_{n}.png")
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    # pure black and white
    from PIL import Image
    Image.open(path).convert("L").convert("RGB").save(path)
    print("rendered", path)


# ------------------------------------------------------------------ the memories

def shot_rooftop():
    """1. A rooftop in the rain, the city behind: his roundhouse takes a man over the edge."""
    plane((0, 90, 18), (260, 80, 1), flat(0.16))
    disc((14, 80, 30), 3.0, flat(0.95))
    disc((14, 79.9, 30), 10, halo(1.0, 0.25))
    skyline(45, 16, -40, 40, 2, 9, 0.09, 0.14)
    skyline(70, 14, -60, 60, 4, 16, 0.12, 0.08)
    floor(0.3, -3, 3.5, -6, 1.2, trim=False)
    box((1.05, 0.25, 0.18), (0.18, 6.5, 0.36), flat(0.26))
    box((1.05, 0.25, 0.37), (0.2, 6.5, 0.02), flat(TRIM))
    hero = figure(key("roundhouse", 2), at=(-0.3, 0, 0), hero=True, name="Hero")
    trail(hero, "shin.R", "thigh.R", sweep=-110, width=0.12)
    figure(key("knockdown", 1), at=(1.3, 0.1, 0.5), facing=-1, tilt=-38, ink=MEN[0], name="Man")
    splatter((1.2, 0, 1.5), (1, 0.1, 0.5), 18)
    rain(320, -4, 4, -2.5, 3, -0.5, 4)
    camera((0.5, -4.6, 0.55), (0.35, 0, 1.25), 30, roll=-4)


def shot_corridor():
    """2. A corridor under the lamps: he fires; a man is thrown back off his feet."""
    floor(0.32)
    wall(1.6, g=CONCRETE)
    box((2.6, 1.55, 2.4), (0.6, 0.05, 0.22), flat(0.93))           # the exit sign, white as in fight mode
    for x in (-3, 0, 3):
        box((x, 1.5, 1.5), (0.14, 0.2, 3), flat(FRAME))
    lamp(-1.5, 1.0, 3.0)
    lamp(1.6, 1.0, 3.0)
    hero = figure(key("gun_shoot", 1), at=(-1.2, 0, 0), prop="gun", hero=True, name="Hero")
    tip = bone_tail(hero, "hand.R") + Vector((0.28, 0, 0.02))
    ball(tip, 0.06, flat(1.0))
    disc(tip + Vector((0, -0.05, 0)), 0.35, halo(1.0, 1.0))
    box(tip + Vector((0.9, 0, 0.01)), (1.6, 0.006, 0.006), flat(1.0))
    figure(key("hit_back", 2), at=(1.3, 0.05, 0.1), facing=-1, tilt=-24, ink=MEN[4], name="Man")
    splatter((1.35, 0, 1.15), (1, 0.2, 0.15), 30, reach=1.0)
    box((-0.7, -0.2, 1.45), (0.012, 0.012, 0.03), flat(0.8), rot=(0.4, 0.8, 0))
    camera((0.1, -4.4, 1.1), (0.05, 0.6, 1.05), 28, roll=3)


def shot_knife():
    """3. The knife, close, in a rain-wet alley under one lamp."""
    floor(0.26)
    wall(1.4, g=0.46)
    night_window(1.6, 2.2, 1.6, 1.4, 1.38, 0.12)
    lamp(0.1, 0.9, 2.6, 2.4, 1.2)
    hero = figure(key("jab", 2), at=(-0.35, 0, 0), prop="knife", hero=True, name="Hero")
    trail(hero, "hand.R", "upperarm.R", sweep=-40, width=0.07)
    figure(key("hit_front", 1), at=(0.55, 0.05, 0), facing=-1, ink=MEN[1], name="Man")
    splatter((0.45, -0.05, 0.95), (0.5, -0.3, -0.6), 26, reach=0.7, spread=0.3)
    rain(150, -2, 2, -1.5, 1.2, 0, 3, 0.65)
    camera((0.05, -3.3, 1.0), (0.1, 0.2, 0.9), 34, roll=-6)


def shot_stairs():
    """4. A stairwell, the city in a tall window: he drops into two men with a flying kick."""
    wall(2.6, -6, 8, 6)
    night_window(1.4, 2.6, 2.6, 3.4, 2.5, 0.14, moon=(0.7, 1.1, 0.16))
    st = flat(0.3)
    for i in range(7):
        box((0.6 + i * 0.45, 0.5, 0.9 - i * 0.15), (0.45, 2.4, 0.15), st)
        box((0.6 + i * 0.45, -0.69, 0.98 - i * 0.15), (0.45, 0.02, 0.02), flat(TRIM))
    for i in range(8):                                               # the rail
        box((0.4 + i * 0.4, -0.7, 1.4 - i * 0.13), (0.015, 0.015, 0.9), flat(FRAME))
    plane((0, 0.5, -0.25), (14, 4, 1), concrete(0.3, 2.0), rot=(0, 0, 0))
    hero = figure(key("flying_kick", 2), at=(0.45, 0, 0.75), hero=True, name="Hero")
    trail(hero, "shin.R", "thigh.R", sweep=-70, width=0.11)
    figure(key("hit_front", 2), at=(1.55, 0.1, 0.42), facing=-1, tilt=-18, ink=MEN[2], name="Man1")
    figure(key("hit_back", 1), at=(2.4, 0.3, 0.15), facing=-1, ink=MEN[5], name="Man2")
    camera((1.2, -5.4, 1.35), (1.3, 0.5, 1.35), 28, roll=5)


def shot_headlights():
    """5. Headlights in the rain: a man on his knees, he stands over him, the pistol on him."""
    plane((0, 1, 0), (20, 14, 1), flat(0.12), rot=(0, 0, 0))
    plane((0, 14, 5), (40, 12, 1), flat(0.06))
    box((0, 6.6, 0.9), (2.6, 1.2, 1.0), flat(0.05))
    for x in (-0.9, 0.9):
        disc((x, 5.98, 0.75), 0.13, flat(1.0))
        disc((x, 5.9, 0.75), 1.6, halo(1.0, 1.0))
        plane((x * 0.6, 3.5, 0.005), (1.2, 5.0, 1), glow(0.9, 0, 0.25), rot=(0, 0, 0))   # their light on the wet road
    figure(key("gun_aim", 1), at=(-0.6, 0, 0), prop="gun", hero=True, name="Hero")
    kneel = B.pose(B.STRAIGHT, spine=168, chest=160, head=150, upperarm_L=20, forearm_L=150, upperarm_R=20, forearm_R=150,
                   thigh_L=84, shin_L=0, thigh_R=0, shin_R=-90)
    figure(kneel, at=(0.55, 0.2, 0), facing=-1, ink=MEN[3], name="Man")
    rain(420, -3, 3, -1, 6, -0.5, 4, 0.8)
    camera((0.15, -3.6, 0.45), (0.0, 1.5, 0.95), 32)


def shot_glass():
    """6. A bar at night: his kick sends a man out through the window in a burst of glass."""
    floor(0.3)
    wall(2.4, -6, 0.3, 5, g=0.5)
    night_window(2.4, 1.7, 3.6, 2.8, 2.5, 0.1)
    lamp(-1.6, 1.5, 3.0)
    box((-2.6, 1.9, 0.55), (2.0, 0.6, 1.1), flat(CRATE))             # the bar counter
    box((-2.6, 1.6, 1.1), (2.0, 0.02, 0.02), flat(TRIM))
    hero = figure(key("front_kick", 2), at=(-1.0, 0.4, 0), hero=True, name="Hero")
    trail(hero, "shin.R", "thigh.R", sweep=-60, width=0.1)
    figure(key("knockdown", 1), at=(0.65, 0.2, 0.55), facing=-1, tilt=-52, ink=MEN[0], name="Man")
    for i in range(90):
        p = Vector((0.4 + rnd.uniform(-0.2, 1.4), rnd.uniform(-0.6, 0.8), rnd.uniform(0.2, 2.0)))
        s = rnd.uniform(0.02, 0.09)
        box(p, (s, s * 0.1, s * rnd.uniform(0.6, 2)), flat(rnd.choice((0.95, 0.8, 0.65))),
            rot=(rnd.random() * 6, rnd.random() * 6, rnd.random() * 6))
    box((0.25, 0.2, 0.05), (0.06, 2.4, 0.1), flat(FRAME))
    camera((0.6, -4.0, 1.0), (0.1, 0.3, 1.0), 30, roll=-3)


def shot_iai():
    """7. A white moon over the yard: one draw-cut, and the two men behind him are falling."""
    plane((0, 50, 10), (200, 60, 1), flat(0.14))
    disc((2.5, 49, 11), 5, flat(0.96))
    disc((2.5, 48.9, 11), 16, halo(1.0, 0.3))
    skyline(30, 10, -30, 30, 1, 5, 0.08, 0.06)
    floor(0.24, -4, 6, -12, 12, trim=False)
    hero = figure(key("sword_iai", 1), at=(-0.4, 0, 0), prop="sword", hero=True, name="Hero")
    figure(key("hit_back", 2), at=(-1.6, 0.4, 0.05), facing=1, tilt=20, ink=MEN[3], name="Man1")
    figure(key("knockdown", 1), at=(1.4, 0.6, 0.3), facing=-1, tilt=-30, ink=MEN[1], name="Man2")
    slash = flat(1.0)
    for i in range(40):                                              # the cut: a long white arc
        k = i / 39
        a = math.radians(-60 + 120 * k)
        p = Vector((-0.3 + 3.2 * math.sin(a), -0.15, 3.6 - 3.2 * math.cos(a) * 0.82))
        w = 0.035 * math.sin(math.pi * k) + 0.004
        box(p, (0.17, 0.005, w), slash, rot=(0, -a * 0.82, 0))
    splatter((-1.5, 0.4, 1.1), (-1, 0.1, 0.4), 22, reach=0.8)
    camera((0.0, -5.0, 0.6), (0.0, 1.0, 1.25), 28, roll=2)


def shot_quiet():
    """8. After: alone on the end of a bed in a hotel room, rain on the window, the gun hanging."""
    floor(0.28)
    wall(2.2, -5, 5, 4, g=0.42)
    night_window(0.5, 1.7, 1.6, 1.9, 2.15, 0.12)
    box((0.1, 0.6, 0.25), (1.6, 2.0, 0.5), flat(0.2))
    box((0.1, -0.41, 0.5), (1.6, 0.02, 0.02), flat(TRIM))
    sit = B.pose(B.STRAIGHT, spine=160, chest=148, head=128, upperarm_L=40, forearm_L=80, upperarm_R=40, forearm_R=70,
                 thigh_L=90, shin_L=0, thigh_R=95, shin_R=4, dz=-0.2)
    figure(sit, at=(-0.55, 0, 0.12), prop="gun", hero=True, name="Hero")
    lamp(1.9, 1.4, 1.6, 1.5, 0.8)
    rain(140, -0.3, 1.3, 2.05, 2.1, 0.75, 2.65, 0.6, slant=4)
    camera((-0.1, -4.2, 1.05), (0.15, 1.0, 1.0), 32, roll=-2)


SHOTS = [shot_rooftop, shot_corridor, shot_knife, shot_stairs, shot_headlights, shot_glass, shot_iai, shot_quiet]

if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    for i, shot in enumerate(SHOTS, 1):
        if only and i not in only:
            continue
        reset()
        shot()
        render(i)
