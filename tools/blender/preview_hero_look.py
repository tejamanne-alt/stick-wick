import sys, os, math
sys.argv = ["x"]
sys.path.insert(0, "/home/user/stick-wick/tools/blender")
import bpy
from mathutils import Vector, Matrix, Quaternion
import build_stickman as B
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "docs", "previews")
B.reset_scene()
arm = B.build_armature()
B.build_mesh(arm)
B.build_actions(arm)        # hand-keyed clips (mocap ones are keyed too, as poses)
scene = bpy.context.scene
scene.render.engine = "CYCLES"; scene.cycles.samples = 16; scene.cycles.device = "CPU"
scene.view_settings.view_transform = "Standard"; scene.render.resolution_x = 520; scene.render.resolution_y = 520
world = bpy.data.worlds.new("Paper"); world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.58, 0.57, 0.55, 1)
scene.world = world
cam_data = bpy.data.cameras.new("Cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = 2.3
cam = bpy.data.objects.new("Cam", cam_data); cam.location = (0, -6, 0.85); cam.rotation_euler = (math.radians(90), 0, 0)
scene.collection.objects.link(cam); scene.camera = cam

def emit(name, rgb):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    e = nt.nodes.new("ShaderNodeEmission"); e.inputs[0].default_value = (*rgb, 1); e.inputs[1].default_value = 1.0
    o = nt.nodes.new("ShaderNodeOutputMaterial"); nt.links.new(e.outputs[0], o.inputs[0])
    return m
lensM = emit("lens", (0.50, 0.55, 0.63)); scarfM = emit("scarf", (0.80, 0.06, 0.07)); white = emit("w", (0.97, 0.96, 0.92))
# the body: flat ink
for o in bpy.data.objects:
    if o.type == "MESH":
        o.data.materials.clear(); o.data.materials.append(emit("ink", (0.02, 0.02, 0.02)))

U = lambda x, y, z: Vector((x, z, y))     # Unity (x, y, z) -> Blender (x, y=z, z=y)

def part(kind, pos, rot, scale, mat, bone):
    if kind == "sphere": bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=24, ring_count=12)
    else: bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.data.materials.append(mat)
    o.matrix_world = Matrix.Translation(pos) @ rot.to_matrix().to_4x4() @ Matrix.Diagonal((*scale, 1))
    mw = o.matrix_world.copy()
    o.parent = arm; o.parent_type = "BONE"; o.parent_bone = bone
    bpy.context.view_layer.update()
    o.matrix_world = mw
    return o

def look_rot(fwd, up):
    # Unity LookRotation in Blender space: local Z -> fwd, local Y -> up
    f = fwd.normalized(); r = up.cross(f).normalized(); u = f.cross(r)
    return Matrix(((r.x, u.x, f.x), (r.y, u.y, f.y), (r.z, u.z, f.z))).to_quaternion()

arm.data.pose_position = "REST"; bpy.context.view_layer.update()
headb = arm.data.bones["head"]
hpos = arm.matrix_world @ headb.head_local            # Blender coords
c = hpos + Vector((0, 0, 0.19)); R = 0.214
upB = Vector((0, 0, 1))
for s in (1, -1):
    n = U(0.60, 0.13, 0.79 * s).normalized()
    rot = look_rot(n, upB) @ Quaternion(Vector((0, 0, 1)), math.radians(-8 * s))
    part("sphere", c + n * R, rot, (0.165 / 2, 0.062 / 2, 0.026 / 2), lensM, "head")
    part("cube", c + n * (R + 0.014) + upB * 0.017 + Vector((0.014, 0, 0)), rot @ Quaternion(Vector((0, 0, 1)), math.radians(25)), (0.045, 0.011, 0.004), white, "head")
    side = U(0.05, 0.16, 0.99 * s).normalized()
    back = (Vector((-1, 0, 0)) - side * Vector((-1, 0, 0)).dot(side)).normalized()
    part("cube", c + side * (R + 0.004) + back * 0.02, look_rot(back, upB), (0.012, 0.012, 0.15), lensM, "head")
part("cube", c + Vector((R - 0.004, 0, 0.03)), Quaternion(), (0.016, 0.14, 0.018), lensM, "head")
neck = hpos + Vector((0, 0, 0.02))
part("sphere", neck, Quaternion(), (0.12, 0.12, 0.05), scarfM, "chest")
knot = part("sphere", neck + Vector((-0.11, 0, -0.02)), Quaternion(), (0.045, 0.045, 0.045), scarfM, "chest")
arm.data.pose_position = "POSE"

def tails(wind):
    """Two tails from the knot, shaped by a steady 'wind' (0 = hanging, 1 = running)."""
    objs = []
    bpy.context.view_layer.update()
    k0 = knot.matrix_world.translation
    for i, (L, ph) in enumerate(((0.75, 0.0), (0.62, 1.7))):
        pts = []
        for j in range(10):
            u = j / 9
            x = -L * u * (0.25 + 0.75 * wind)
            y = -L * u * (1 - 0.8 * wind) + 0.05 * math.sin(u * 7 + ph) * wind * u - (0.03 * i)
            pts.append(k0 + Vector((x, 0.06, y)))
        cu = bpy.data.curves.new("t%d" % i, "CURVE"); cu.dimensions = "3D"
        sp = cu.splines.new("POLY"); sp.points.add(len(pts) - 1)
        for p, q in zip(sp.points, pts): p.co = (*q, 1)
        for j, p in enumerate(sp.points): p.radius = 1.0 - 0.55 * j / 9
        cu.bevel_depth = 0.04 if i == 0 else 0.034
        ob = bpy.data.objects.new("t%d" % i, cu); ob.data.materials.append(scarfM)
        scene.collection.objects.link(ob); objs.append(ob)
    return objs

tmp = os.path.join(OUT, "_f.png")
shots = [("idle", 8, 0.15, "STANDING"), ("run", 6, 1.0, "RUNNING"), ("taunt", 14, 0.35, "BECKONING")]
tiles = []
accessories = [o for o in bpy.data.objects if o.parent == arm and o.type == "MESH" and o.name != "Stickman"]
for clip, fr, wind, label in shots:
    if clip in bpy.data.actions: arm.animation_data.action = bpy.data.actions[clip]
    scene.frame_set(fr)
    t = tails(wind)
    scene.render.filepath = tmp; bpy.ops.render.render(write_still=True)
    tiles.append((Image.open(tmp).convert("RGB").copy(), label))
    for o in t: bpy.data.objects.remove(o)
# before: plain (accessories hidden), idle
for o in bpy.data.objects:
    if o.parent == arm and o.type == "MESH" and o.data.materials and o.data.materials[0].name.split(".")[0] in ("lens", "scarf", "w"):
        o.hide_render = True
arm.animation_data.action = bpy.data.actions["idle"]; scene.frame_set(8)
bpy.ops.render.render(write_still=True)
before = Image.open(tmp).convert("RGB").copy()
from PIL import ImageDraw, ImageFont
F = "/home/user/stick-wick/Assets/Resources/Fonts/BarlowCondensed-SemiBold.ttf"
font = ImageFont.truetype(F, 30)
sheet = Image.new("RGB", (520 * 4, 520 + 50), (25, 25, 25)); d = ImageDraw.Draw(sheet)
for i, (img, lab) in enumerate([(before, "BEFORE")] + tiles):
    sheet.paste(img, (520 * i, 50)); d.text((520 * i + 14, 10), lab, fill=(255, 210, 0), font=font)
sheet.save("/home/user/stick-wick/docs/previews/hero_look.png")
print("ok")
