"""Renders the hitman's flashback stills for the prologue (chapter 1): the fights he does not
remember, cut in for a few frames each when his hand closes on the knife. Eight noir frames,
the real stickman rig posed with the game's own clip poses, black figures against hard
backlight, rain, one accent colour each.

    /home/user/.venv-blender/bin/python tools/blender/render_flashbacks.py [--quick]

Writes Assets/Resources/Story/Flash/flash_<n>.png (960x540; --quick renders small and fast
into the same place, for checking the staging)."""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as B  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Story", "Flash")
QUICK = "--quick" in sys.argv
rnd = random.Random(5)


# ------------------------------------------------------------------ materials

def mat_ink(name="Ink", rough=0.42):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.012, 0.012, 0.014, 1)
    b.inputs["Roughness"].default_value = rough
    return m


def mat_emit(color, strength, name="Emit"):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*color, 1)
    e.inputs["Strength"].default_value = strength
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], o.inputs[0])
    return m


def mat_wet(color=(0.02, 0.02, 0.025), rough=0.12):
    m = bpy.data.materials.new("Wet")
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Roughness"].default_value = rough
    return m


def mat_gradient(c0, c1, strength=1.0, axis="Z"):
    """An emissive backdrop shading from c0 (bottom) to c1 (top) in object space."""
    m = bpy.data.materials.new("Grad")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = -1
    mr.inputs["From Max"].default_value = 1
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (*c0, 1)
    ramp.color_ramp.elements[1].color = (*c1, 1)
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Strength"].default_value = strength
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z" if axis == "Z" else "X"], mr.inputs["Value"])
    mr.inputs["From Min"].default_value = 0
    mr.inputs["From Max"].default_value = 1
    nt.links.new(mr.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], e.inputs[0])
    nt.links.new(e.outputs[0], o.inputs[0])
    return m


def mat_windows(color=(1.0, 0.72, 0.4), strength=3.0, density=0.35):
    """Lit windows on a tower: a grid, a random few lit."""
    m = bpy.data.materials.new("Windows")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (14, 14, 30)
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.feature = "F1"
    vor.voronoi_dimensions = "3D"
    vor.inputs["Randomness"].default_value = 0.0
    gt = nt.nodes.new("ShaderNodeMath")
    gt.operation = "GREATER_THAN"
    gt.inputs[1].default_value = 1 - density
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*color, 1)
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = strength
    dark = nt.nodes.new("ShaderNodeBsdfDiffuse")
    dark.inputs["Color"].default_value = (0.01, 0.01, 0.012, 1)
    mixs = nt.nodes.new("ShaderNodeAddShader")
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(tc.outputs["Object"], mp.inputs[0])
    nt.links.new(mp.outputs[0], vor.inputs["Vector"])
    nt.links.new(vor.outputs["Color"], sep.inputs[0])
    nt.links.new(sep.outputs[0], gt.inputs[0])
    nt.links.new(gt.outputs[0], mul.inputs[0])
    nt.links.new(mul.outputs[0], em.inputs["Strength"])
    nt.links.new(em.outputs[0], mixs.inputs[0])
    nt.links.new(dark.outputs[0], mixs.inputs[1])
    nt.links.new(mixs.outputs[0], o.inputs[0])
    return m


# ------------------------------------------------------------------ the scene

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 12 if QUICK else 64
    sc.cycles.use_denoising = True
    sc.render.resolution_x = 480 if QUICK else 960
    sc.render.resolution_y = 270 if QUICK else 540
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Filmic" if "Filmic" in [i.identifier for i in sc.view_settings.bl_rna.properties["view_transform"].enum_items] else "AgX"
    sc.view_settings.look = "None"
    w = bpy.data.worlds.new("Night")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.004, 0.005, 0.008, 1)
    sc.world = w
    return sc


INK = None


def figure(pose_dict, at=(0, 0, 0), facing=1, tilt=0.0, prop=None, name="Fig"):
    """A posed stickman: the game's rig with one of its clip poses. facing -1 turns him round;
    tilt (degrees, about the depth axis) throws the whole body (falls)."""
    arm = B.build_armature()
    body = B.build_mesh(arm)
    body.data.materials.clear()
    body.data.materials.append(INK)
    arm.name = name
    B.apply_pose(arm, pose_dict)
    arm.location = at
    arm.rotation_euler = (0, math.radians(-tilt), 0 if facing > 0 else math.pi)
    if prop:
        p = B.add_prop(arm, "sword" if prop == "sword" else "gun")
        if prop == "knife":
            p.scale = (0.012, 0.13, 0.03)
            p.location = (0, 0.13 - 0.09, 0)
        p.data.materials.clear()
        p.data.materials.append(INK)
    bpy.context.view_layer.update()
    return arm


def key(clip, i):
    """Key i of a hand-keyed clip, as a pose (negative i counts from the end)."""
    return dict(B.CLIPS[clip]["keys"][i][1])


def bone_tail(arm, bone):
    bpy.context.view_layer.update()
    return arm.matrix_world @ arm.pose.bones[bone].tail


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


def ball(loc, r, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=12, ring_count=8)
    o = bpy.context.object
    o.data.materials.append(mat)
    return o


def area(loc, target, power, color, size=2.0):
    d = bpy.data.lights.new("Area", "AREA")
    d.energy = power
    d.color = color
    d.size = size
    o = bpy.data.objects.new("Area", d)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)
    direction = Vector(target) - Vector(loc)
    o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    return o


def point(loc, power, color, r=0.1):
    d = bpy.data.lights.new("Pt", "POINT")
    d.energy = power
    d.color = color
    d.shadow_soft_size = r
    o = bpy.data.objects.new("Pt", d)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)
    return o


def camera(loc, target, lens=35, roll=0.0):
    d = bpy.data.cameras.new("Cam")
    d.lens = lens
    o = bpy.data.objects.new("Cam", d)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)
    direction = Vector(target) - Vector(loc)
    q = direction.to_track_quat("-Z", "Y")
    o.rotation_euler = q.to_euler()
    o.rotation_euler.rotate_axis("Z", math.radians(roll))
    bpy.context.scene.camera = o
    return o


def rain(n, x0, x1, y0, y1, z0, z1, strength=0.8, slant=12):
    m = mat_emit((0.75, 0.82, 1.0), strength, "Rain")
    bpy.ops.mesh.primitive_cylinder_add(radius=0.004, depth=0.5, vertices=5)
    base = bpy.context.object
    base.data.materials.append(m)
    base.rotation_euler = (0, math.radians(slant), 0)
    for _ in range(n):
        c = base.copy()
        c.location = (rnd.uniform(x0, x1), rnd.uniform(y0, y1), rnd.uniform(z0, z1))
        c.scale = (1, 1, rnd.uniform(0.6, 1.4))
        bpy.context.scene.collection.objects.link(c)
    base.location = (0, 0, -50)


def spray(origin, direction, n, color=(0.75, 0.02, 0.02), spread=0.5, reach=1.0):
    """Blood thrown off a hit: droplets along an arc, bigger near the wound."""
    m = mat_emit(color, 1.6, "Blood")
    d = Vector(direction).normalized()
    for i in range(n):
        k = rnd.random() ** 0.7
        p = Vector(origin) + d * (k * reach) + Vector((rnd.uniform(-1, 1), rnd.uniform(-0.3, 0.3), rnd.uniform(-1, 1))) * spread * k * 0.5
        p.z -= 0.25 * (k * reach) ** 2
        ball(p, 0.03 * (1.1 - k) + 0.006, m)


def skyline(y, n, x0, x1, hmin, hmax, color, strength=3.0, lit=0.12):
    """Dark towers with a sparse grid of lit windows on their faces."""
    tower = mat_ink("Tower", 0.8)
    win = mat_emit(color, strength, "Win")
    bpy.ops.mesh.primitive_plane_add(size=1)
    pane = bpy.context.object
    pane.data.materials.append(win)
    pane.rotation_euler = (math.pi / 2, 0, 0)
    pane.location = (0, 0, -80)
    for i in range(n):
        w = rnd.uniform(2.0, 4.5)
        h = rnd.uniform(hmin, hmax)
        x = x0 + (x1 - x0) * (i + rnd.random() * 0.5) / n
        yy = y + rnd.uniform(0, 6)
        box((x, yy, h / 2 - 2), (w, 4, h), tower)
        cols, rows = int(w / 0.6), int(h / 0.9)
        for c in range(cols):
            for r in range(rows):
                if rnd.random() > lit:
                    continue
                wpn = pane.copy()
                wpn.location = (x - w / 2 + (c + 0.5) * w / cols, yy - 2.01, -2 + (r + 0.5) * h / rows)
                wpn.scale = (0.3, 0.42, 1)
                bpy.context.scene.collection.objects.link(wpn)


def render(n):
    os.makedirs(OUT, exist_ok=True)
    sc = bpy.context.scene
    sc.render.filepath = os.path.join(OUT, f"flash_{n}.png")
    bpy.ops.render.render(write_still=True)
    print("rendered", sc.render.filepath)


# ------------------------------------------------------------------ the memories

def shot_rooftop():
    """1. A rooftop in the rain, the city behind: his roundhouse takes a man over the edge."""
    plane((-2, 0.5, 0), (8, 4, 1), mat_wet((0.02, 0.022, 0.03), 0.18), rot=(0, 0, 0))
    box((1.05, 0.5, 0.18), (0.18, 4, 0.36), mat_ink("Ledge", 0.6))
    figure(key("roundhouse", -3 if len(B.CLIPS["roundhouse"]["keys"]) > 3 else 2), at=(-0.3, 0, 0), name="Hero")
    figure(key("knockdown", 1), at=(1.25, 0.1, 0.45), facing=-1, tilt=-38, name="Man")
    plane((0, 90, 18), (260, 80, 1), mat_gradient((0.2, 0.26, 0.45), (0.01, 0.015, 0.04), 1.0))
    skyline(45, 16, -40, 40, 2, 9, (1.0, 0.72, 0.4), 3.0, 0.14)
    skyline(70, 14, -60, 60, 4, 16, (0.6, 0.75, 1.0), 2.0, 0.1)
    area((1.5, 4, 3), (0, 0, 1), 900, (0.7, 0.8, 1.0), 4)            # the sky behind: rim light
    area((-3, -3, 4), (0, 0, 0.8), 60, (0.4, 0.5, 0.8), 3)
    rain(500, -4, 4, -2.5, 3, -0.5, 4)
    camera((0.5, -4.6, 0.55), (0.35, 0, 1.25), 30, roll=-4)


def shot_corridor():
    """2. A corridor under a red exit light: he fires; a man is thrown back off his feet."""
    plane((0, 0.6, 0), (12, 3, 1), mat_wet((0.03, 0.01, 0.01), 0.1), rot=(0, 0, 0))
    plane((0, 1.6, 1.5), (12, 3, 1), mat_ink("Wall", 0.7))
    box((2.6, 1.55, 2.35), (0.6, 0.05, 0.22), mat_emit((1.0, 0.08, 0.05), 6, "Exit"))
    for x in (-3, 0, 3):
        box((x, 1.5, 1.5), (0.1, 0.3, 3), mat_ink("Pillar", 0.6))
    hero = figure(key("gun_shoot", 1), at=(-1.2, 0, 0), prop="gun", name="Hero")
    tip = bone_tail(hero, "hand.R") + Vector((0.28, 0, 0.02))
    ball(tip, 0.07, mat_emit((1.0, 0.85, 0.5), 40, "Flash"))
    box(tip + Vector((0.9, 0, 0.01)), (1.6, 0.006, 0.006), mat_emit((1.0, 0.75, 0.35), 18, "Streak"))
    point(tip, 120, (1.0, 0.75, 0.4), 0.05)
    figure(key("hit_back", 2), at=(1.3, 0.05, 0.1), facing=-1, tilt=-24, name="Man")
    spray((1.35, 0, 1.15), (1, 0.2, 0.15), 40, reach=1.0)
    ball((-0.7, -0.2, 1.4), 0.015, mat_emit((1.0, 0.7, 0.2), 4, "Shell"))
    area((2.6, 1.0, 2.6), (0, 0, 1), 400, (1.0, 0.12, 0.08), 1.5)
    area((-4, -2, 2.5), (0, 0, 1), 40, (0.3, 0.35, 0.6), 2)
    camera((0.1, -4.2, 1.1), (0.05, 0.6, 1.0), 28, roll=3)


def shot_knife():
    """3. Close: the knife going in under the ribs, neon on the wet alley behind."""
    plane((0, 0.6, 0), (8, 4, 1), mat_wet((0.02, 0.02, 0.03), 0.08), rot=(0, 0, 0))
    plane((0, 3.0, 2), (10, 5, 1), mat_gradient((0.55, 0.05, 0.28), (0.03, 0.06, 0.25), 1.4, axis="X"))
    figure(key("jab", 2), at=(-0.35, 0, 0), prop="knife", name="Hero")
    figure(key("hit_front", 1), at=(0.55, 0.05, 0), facing=-1, name="Man")
    spray((0.45, -0.05, 0.95), (0.5, -0.3, -0.6), 30, reach=0.7, spread=0.3)
    area((0.5, 2.5, 2), (0, 0, 1), 500, (1.0, 0.3, 0.7), 3)
    area((-2, 2, 2), (0, 0, 1), 300, (0.3, 0.4, 1.0), 3)
    rain(250, -2, 2, -1.5, 2, 0, 3, 0.5)
    camera((0.05, -3.3, 1.0), (0.1, 0.2, 0.9), 34, roll=-6)


def shot_stairs():
    """4. A stairwell, moonlight through a tall window: he drops into two men with a flying kick."""
    ink = mat_ink("Steps", 0.5)
    for i in range(7):
        box((0.6 + i * 0.45, 0.5, 0.9 - i * 0.15), (0.45, 2.4, 0.15), ink)
    plane((0, 0.5, -0.25), (14, 4, 1), ink, rot=(0, 0, 0))
    win = plane((1.4, 2.4, 2.6), (2.6, 4.2, 1), mat_emit((0.45, 0.58, 1.0), 2.2, "Moon"))
    for x in (0.55, 1.4, 2.25):
        box((x, 2.35, 2.6), (0.06, 0.05, 4.2), ink)
    for z in (1.4, 2.6, 3.8):
        box((1.4, 2.35, z), (2.6, 0.05, 0.06), ink)
    figure(key("flying_kick", 2), at=(0.45, 0, 0.75), name="Hero")
    figure(key("hit_front", 2), at=(1.55, 0.1, 0.42), facing=-1, tilt=-18, name="Man1")
    figure(key("hit_back", 1), at=(2.4, 0.3, 0.15), facing=-1, name="Man2")
    area((1.4, 3.5, 2.6), (1.2, 0, 1.0), 700, (0.6, 0.72, 1.0), 3)
    camera((1.2, -5.4, 1.35), (1.3, 0.5, 1.35), 28, roll=5)


def shot_headlights():
    """5. Headlights in the rain: a man on his knees, he stands over him, the pistol down at him."""
    plane((0, 1, 0), (20, 14, 1), mat_wet((0.02, 0.02, 0.022), 0.05), rot=(0, 0, 0))
    for x in (-0.9, 0.9):
        ball((x, 6, 0.75), 0.13, mat_emit((1.0, 0.95, 0.85), 80, "Lamp"))
        area((x, 5.8, 0.75), (0, 0, 0.8), 1600, (1.0, 0.95, 0.85), 0.5)
    box((0, 6.6, 0.9), (2.6, 1.2, 1.0), mat_ink("Car", 0.3))
    figure(key("gun_aim", 1), at=(-0.6, 0, 0), prop="gun", name="Hero")
    kneel = B.pose(B.STRAIGHT, spine=168, chest=160, head=150, upperarm_L=20, forearm_L=150, upperarm_R=20, forearm_R=150,
                   thigh_L=84, shin_L=0, thigh_R=0, shin_R=-90)
    figure(kneel, at=(0.55, 0.2, 0), facing=-1, name="Man")
    rain(600, -3, 3, -1, 6, -0.5, 4, 1.2)
    camera((0.15, -3.6, 0.45), (0.0, 1.5, 0.95), 32)


def shot_glass():
    """6. Through the window of a bar: his kick sends a man through the glass in a burst of it."""
    plane((0, 1, 0), (12, 6, 1), mat_wet((0.03, 0.025, 0.02), 0.25), rot=(0, 0, 0))
    plane((-1.8, 2.5, 2), (6, 5, 1), mat_gradient((0.7, 0.35, 0.12), (0.12, 0.04, 0.01), 1.2))
    figure(key("front_kick", 2), at=(-1.0, 0.4, 0), name="Hero")
    figure(key("knockdown", 1), at=(0.65, 0.2, 0.55), facing=-1, tilt=-52, name="Man")
    glass = mat_emit((0.75, 0.88, 1.0), 2.5, "Glass")
    for i in range(80):
        p = Vector((0.4 + rnd.uniform(-0.2, 1.4), rnd.uniform(-0.6, 0.8), rnd.uniform(0.2, 2.0)))
        s = rnd.uniform(0.02, 0.09)
        box(p, (s, s * 0.1, s * rnd.uniform(0.6, 2)), glass, rot=(rnd.random() * 6, rnd.random() * 6, rnd.random() * 6))
    box((0.25, 0.2, 0.05), (0.05, 2.4, 0.1), mat_ink("Sill", 0.5))
    area((-2, 2, 2.5), (0, 0, 1), 500, (1.0, 0.6, 0.3), 4)
    area((3, -2, 3), (0, 0, 1), 120, (0.4, 0.55, 1.0), 3)
    rain(250, 0.3, 4, -2, 2, 0, 4, 0.7)
    camera((0.6, -4.0, 1.0), (0.1, 0.3, 1.0), 30, roll=-3)


def shot_iai():
    """7. A red moon: one draw-cut, and the two men behind him are already falling."""
    plane((0, 1, 0), (20, 10, 1), mat_wet((0.02, 0.01, 0.01), 0.3), rot=(0, 0, 0))
    plane((0, 50, 10), (200, 60, 1), mat_gradient((0.25, 0.02, 0.02), (0.02, 0.0, 0.0), 1.0))
    ball((2.5, 60, 12), 5, mat_emit((1.0, 0.12, 0.04), 9, "Moon"))
    figure(key("sword_iai", 1), at=(-0.4, 0, 0), prop="sword", name="Hero")
    figure(key("hit_back", 2), at=(-1.6, 0.4, 0.05), facing=1, tilt=20, name="Man1")
    figure(key("knockdown", 1), at=(1.4, 0.6, 0.3), facing=-1, tilt=-30, name="Man2")
    slash = mat_emit((1.0, 1.0, 1.0), 12, "Slash")
    # the cut: a long thin arc through the frame, thick in the middle, gone at the ends
    for i in range(40):
        k = i / 39
        a = math.radians(-60 + 120 * k)
        p = Vector((-0.3 + 3.2 * math.sin(a), -0.15, 3.6 - 3.2 * math.cos(a) * 0.82))
        w = 0.035 * math.sin(math.pi * k) + 0.004
        box(p, (0.17, 0.005, w), slash, rot=(0, -a * 0.82, 0))
    spray((-1.5, 0.4, 1.1), (-1, 0.1, 0.4), 25, reach=0.8)
    area((1.5, 6, 4), (0, 0, 1), 800, (1.0, 0.25, 0.15), 6)
    camera((0.0, -5.0, 0.6), (0.0, 1.0, 1.25), 28, roll=2)


def shot_quiet():
    """8. After: alone on the end of a bed in a hotel room, rain on the window, the gun hanging."""
    plane((0, 1, 0), (10, 6, 1), mat_ink("Floor", 0.6), rot=(0, 0, 0))
    plane((0.3, 2.2, 2), (6, 4, 1), mat_ink("WallR", 0.8))
    win = plane((0.5, 2.15, 1.7), (1.6, 1.9, 1), mat_gradient((0.25, 0.4, 0.8), (0.1, 0.18, 0.45), 2.5))
    ink = mat_ink("Frame", 0.5)
    box((0.5, 2.1, 1.7), (0.05, 0.05, 1.9), ink)
    box((0.5, 2.1, 1.7), (1.6, 0.05, 0.05), ink)
    box((0.1, 0.6, 0.25), (1.6, 2.0, 0.5), mat_ink("Bed", 0.8))
    sit = B.pose(B.STRAIGHT, spine=160, chest=148, head=128, upperarm_L=40, forearm_L=80, upperarm_R=40, forearm_R=70,
                 thigh_L=90, shin_L=0, thigh_R=95, shin_R=4, dz=-0.2)
    figure(sit, at=(-0.55, 0, 0.12), prop="gun", name="Hero")
    ball((1.9, 1.4, 1.1), 0.12, mat_emit((1.0, 0.6, 0.3), 8, "Lamp"))
    point((1.9, 1.4, 1.1), 40, (1.0, 0.6, 0.3), 0.2)
    area((0.5, 2.0, 1.7), (-0.5, 0, 1), 250, (0.4, 0.55, 1.0), 1.6)
    rain(140, -0.3, 1.3, 2.05, 2.1, 0.75, 2.65, 0.6, slant=4)
    camera((-0.1, -4.2, 1.05), (0.15, 1.0, 1.0), 32, roll=-2)


SHOTS = [shot_rooftop, shot_corridor, shot_knife, shot_stairs, shot_headlights, shot_glass, shot_iai, shot_quiet]

if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    for i, shot in enumerate(SHOTS, 1):
        if only and i not in only:
            continue
        reset()
        INK = mat_ink()
        shot()
        render(i)
