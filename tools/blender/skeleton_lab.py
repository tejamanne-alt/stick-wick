"""SKELETON LAB: the proposed V4 skeleton, for review before anything changes in the game.

    /home/user/.venv-blender/bin/python tools/blender/skeleton_lab.py

Writes docs/previews/skeleton_lab.png:
  row 1  the current skeleton (14 bones) and the proposed one (29 bones), side and front, every bone
         drawn over the body and named
  row 2  the same blows on both: the guard, the jab and the cross at impact. The new bones give the
         heel lift and the pivot on the ball of the foot, the shoulder reaching through the punch,
         the chin tucked behind the shoulder, the fist turned over, and the spine coiled
  row 3  close-ups of what the new bones can do: the foot's roll (flat, heel up, on the toes), the
         hand (fist, open, gripping), the shoulder (down, shrugged, reaching), and the clinch: two
         men, his hands cupped round the other man's neck

The proposed bones (new ones marked *):
  hips · spine_low* · spine_mid* · chest · neck* · head
  clavicle.L/R* · upperarm · forearm · hand (wrist)* · fingers*
  thigh · shin · foot (heel to ball)* · toes*
Angles are absolute in the side plane (0 down, 90 forward, 180 up), with yaw (+ towards the far
side) and twist about the bone where it matters."""
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as bs  # noqa: E402
import v3_rig as vr  # noqa: E402

vr.patch_bones()
import strike_lab as sl  # noqa: E402
import v3_clips as vc  # noqa: E402

ROOT = bs.ROOT
OUT = os.path.join(ROOT, "docs", "previews", "skeleton_lab.png")
TMP = os.path.join(ROOT, "docs", "previews", "_skel.png")
FONT = os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf")

# ---------------------------------------------------------------- the proposed skeleton
# name: (parent, head (x, y, z), rest direction, length, mesh radius)
DD, LY = 0.11, 0.088
UP, DN, FW = (0, 0, 1), (0, 0, -1), (1, 0, 0)
B = {
    "hips":       (None, (0, 0, 0.70), UP, 0.07, 0.11),
    "spine_low":  ("hips", (0, 0, 0.77), UP, 0.09, 0.12),
    "spine_mid":  ("spine_low", (0, 0, 0.86), UP, 0.09, 0.12),
    "chest":      ("spine_mid", (0, 0, 0.95), UP, 0.08, 0.12),
    "neck":       ("chest", (0, 0, 1.03), UP, 0.05, 0.045),
    "head":       ("neck", (0, 0, 1.08), UP, 0.24, 0.0),
}
for s, y in (("L", 1), ("R", -1)):
    B["clavicle." + s] = ("chest", (0.0, 0.025 * y, 0.995), (0, y, 0), DD - 0.025, 0.0)
    B["upperarm." + s] = ("clavicle." + s, (0, DD * y, 0.995), DN, 0.26, 0.058)
    B["forearm." + s] = ("upperarm." + s, (0, DD * y, 0.735), DN, 0.24, 0.055)
    B["hand." + s] = ("forearm." + s, (0, DD * y, 0.495), DN, 0.055, 0.0)
    B["fingers." + s] = ("hand." + s, (0, DD * y, 0.44), DN, 0.055, 0.0)
    B["thigh." + s] = ("hips", (0, LY * y, 0.70), DN, 0.34, 0.072)
    B["shin." + s] = ("thigh." + s, (0, LY * y, 0.36), DN, 0.30, 0.066)
    B["foot." + s] = ("shin." + s, (0, LY * y, 0.06), (0.92, 0, -0.39), 0.105, 0.0)
    B["toes." + s] = ("foot." + s, (0.097, LY * y, 0.019), FW, 0.055, 0.0)
ORDER = list(B.keys())
SIDE_ANGLE_REST = {"up": 180, "down": 0}


def build_rig(name):
    data = bpy.data.armatures.new(name + "Rig")
    arm = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for n, (par, head, d, ln, _r) in B.items():
        b = data.edit_bones.new(n)
        b.head = Vector(head)
        b.tail = Vector(head) + Vector(d).normalized() * ln
        b.align_roll(Vector((0, 1, 0)) if abs(Vector(d).normalized().y) < 0.9 else Vector((0, 0, 1)))
        if par:
            b.parent = data.edit_bones[par]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def build_body(arm, near_g, far_g):
    near, far = sl.mat(arm.name + "Near", near_g), sl.mat(arm.name + "Far", far_g)
    parts = []

    def part(obj, bone, m):
        obj.data.materials.append(m)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    def sphere(r, at, bone, m, scale=(1, 1, 1), seg=14):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=seg, ring_count=max(6, seg // 2), location=at)
        bpy.context.object.scale = scale
        part(bpy.context.object, bone, m)

    def capsule(h, t, r, bone, m):
        h, t = Vector(h), Vector(t)
        bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=(t - h).length, vertices=12, location=(h + t) / 2)
        o = bpy.context.object
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference((t - h).normalized())
        part(o, bone, m)
        sphere(r, t, bone, m, seg=10)

    for n, (par, head, d, ln, r) in B.items():
        h = Vector(head)
        t = h + Vector(d).normalized() * ln
        m = far if n.endswith(".L") else near
        if n == "head":
            sphere(bs.HEAD_RADIUS, (0, 0, 1.25), n, near, seg=24)
        elif n == "spine_low":
            sphere(0.165, (0, 0, 0.81), n, near, (1.0, 1.05, 1.12), 18)
        elif n == "spine_mid":
            sphere(0.15, (0, 0, 0.9), n, near, (1.0, 1.04, 1.0), 18)
        elif n == "chest":
            sphere(0.152, (0, 0, 0.955), n, near, (1.0, 1.06, 0.95), 18)
        elif n == "neck":
            capsule(h, t, r, n, near)
        elif n.startswith("clavicle"):
            sphere(0.062, t, n, m)                                    # the shoulder ball rides the clavicle
        elif n.startswith("hand"):
            sphere(0.046, h + Vector(d) * 0.028, n, m, (0.8, 1.25, 1.05))     # the palm, wide across the knuckles
            ys = -1 if n.endswith("R") else 1
            sphere(0.02, h + Vector((0.028, 0.03 * ys, -0.022)), n, m)       # the thumb's root
            sphere(0.018, h + Vector((0.045, 0.034 * ys, -0.045)), n, m)     # the thumb's tip
        elif n.startswith("fingers"):
            for k in range(3):                                        # three knuckle segments, a mitten
                sphere(0.03, h + Vector(d) * (0.012 + 0.02 * k), n, m, (0.8, 1.45, 1.0), 10)
        elif n.startswith("foot"):
            sphere(0.042, h + Vector((-0.02, 0, -0.012)), n, m)       # the heel
            capsule(h + Vector((0, 0, -0.01)), t, 0.04, n, m)
        elif n.startswith("toes"):
            capsule(h, t, 0.036, n, m)
        elif r > 0:
            capsule(h, t, r, n, m)
            if n.startswith(("upperarm", "thigh")):
                sphere(r, h, n, m, seg=10)
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = arm.name + "Body"
    bpy.ops.object.shade_smooth()
    body.parent = arm
    body.modifiers.new("Armature", "ARMATURE").object = arm
    return body


def new_man(name, near_g, far_g, x=0.0, facing=1):
    arm = build_rig(name)
    build_body(arm, near_g, far_g)
    arm.location = (x, 0, 0)
    arm.rotation_euler = (0, 0, 0 if facing > 0 else math.pi)
    return arm


# ---------------------------------------------------------------- posing
def side(a, yaw=0.0):
    """A direction from a side-plane angle (0 down, 90 forward, 180 up) and a yaw (+ far side)."""
    r = math.radians(a)
    return Quaternion((0, 0, 1), math.radians(yaw)) @ Vector((math.sin(r), 0, -math.cos(r)))


def pose(arm, spec, hips=(0.0, 0.66), ground=True):
    """spec: bone -> (angle, yaw, twist) | ("dir", vector, twist). Bones not given keep their rest
    direction relative to the parent's change. Feet are put on the ground (the lowest point of
    either foot at the floor) unless ground=False."""
    rest_q = {pb.name: pb.bone.matrix_local.to_quaternion() for pb in arm.pose.bones}
    world = {}
    for n in ORDER:
        par, head, d, ln, _r = B[n]
        d0 = Vector(d).normalized()
        if n in spec:
            v = spec[n]
            if v[0] == "dir":
                want, tw = Vector(v[1]).normalized(), v[2]
            else:
                want, tw = side(v[0], v[1] if len(v) > 1 else 0.0), v[2] if len(v) > 2 else 0.0
            q = Quaternion(want, math.radians(tw)) @ d0.rotation_difference(want)
        else:
            q = world[par] if par else Quaternion()
        world[n] = q
    for n in ORDER:
        pb = arm.pose.bones[n]
        par = B[n][0]
        rel = world[n] if par is None else world[par].inverted() @ world[n]
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = rest_q[n].inverted() @ rel @ rest_q[n]
        pb.location = (0, 0, 0)
    root = arm.pose.bones["hips"]
    off = Vector((hips[0], 0, hips[1] - 0.70))
    root.location = rest_q["hips"].to_matrix().inverted() @ off
    bpy.context.view_layer.update()
    if ground:
        low = min((arm.matrix_world @ arm.pose.bones[b].tail).z for b in ("toes.L", "toes.R", "foot.L", "foot.R"))
        heel = min((arm.matrix_world @ arm.pose.bones[b].head).z for b in ("foot.L", "foot.R"))
        drop = min(low - 0.035, heel - 0.055)
        off.z -= drop
        root.location = rest_q["hips"].to_matrix().inverted() @ off
        bpy.context.view_layer.update()


def P(base, **kw):
    p = dict(base)
    for k, v in kw.items():
        p[k.replace("_L", ".L").replace("_R", ".R")] = v
    return p


# the guard, on the new bones: the shoulders slightly hunched and forward (the chin behind the lead
# one), the spine curled a little, the fists closed, the rear heel up off the floor
GUARD = {
    "hips": (180, 0, -10), "spine_low": (176, 0, -6), "spine_mid": (170, 0, -6), "chest": (164, 0, -6),
    "neck": (158,), "head": (174,),
    "clavicle.L": ("dir", (0.25, 1, 0.12), 0), "clavicle.R": ("dir", (0.05, -1, 0.14), 0),
    "upperarm.L": (62, -6), "forearm.L": (150, -8), "hand.L": (156, -8), "fingers.L": (250, -8),
    "upperarm.R": (22, 4), "forearm.R": (166, 6), "hand.R": (172, 6), "fingers.R": (262, 6),
    "thigh.L": (24,), "shin.L": (-2,), "foot.L": (92,), "toes.L": (92,),
    "thigh.R": (-14,), "shin.R": (-30,), "foot.R": (58,), "toes.R": (90,),
}
# the jab at impact: the lead shoulder REACHES (clavicle forward), the chin hides behind it, the
# fist has turned over (knuckles level), the lead foot has stepped and landed heel first
JAB = P(GUARD, hips=(180, 0, -16), spine_low=(176, 0, -12), spine_mid=(170, 0, -14), chest=(160, 0, -16),
        neck=(150,), head=(170,),
        clavicle_L=("dir", (0.75, 0.6, 0.2), 0),
        upperarm_L=(96, -4), forearm_L=(94, -4, -80), hand_L=(94, -4, -80), fingers_L=(190, -4, -80),
        thigh_L=(30,), shin_L=(-6,), foot_L=(98,), toes_L=(90,))
# the cross at impact: rear heel up and turned, pushing off the ball of the foot (toes flat), the
# hips and spine uncoiled in turn, the rear shoulder thrown forward, the fist turned over, the lead
# fist home at the chin
CROSS = P(GUARD, hips=(178, 0, 24), spine_low=(172, 0, 30), spine_mid=(162, 0, 34), chest=(150, 0, 38),
          neck=(144,), head=(166,),
          clavicle_R=("dir", (0.85, -0.45, 0.22), 0), clavicle_L=("dir", (-0.25, 1, 0.1), 0),
          upperarm_R=(97, 2), forearm_R=(93, 2, 80), hand_R=(93, 2, 80), fingers_R=(188, 2, 80),
          upperarm_L=(36, -4), forearm_L=(166, -6), hand_L=(172, -6),
          thigh_L=(34,), shin_L=(-2,), foot_L=(92,),
          thigh_R=(-30,), shin_R=(-48,), foot_R=(28,), toes_R=(90,))
OLD_JAB = vc.CLIPS["jab"]
OLD_CROSS = vc.CLIPS["cross"]


def old_pose(arm, spec, frame):
    keys = [(f, vr.resolve(p), e) for f, p, e in spec["keys"]]
    vr.apply_sampled(arm, vr.sample(keys, frame))
    bpy.context.view_layer.update()


# ---------------------------------------------------------------- render
def bones_px(arm, cam, size):
    out = []
    for pb in arm.pose.bones:
        h = arm.matrix_world @ pb.head
        t = arm.matrix_world @ pb.tail
        out.append((pb.name, to_px(cam, h, size), to_px(cam, t, size), h.y))
    return out


def to_px(cam, p, size):
    from bpy_extras.object_utils import world_to_camera_view
    v = world_to_camera_view(bpy.context.scene, cam, p)
    return (v.x * size[0], (1 - v.y) * size[1])


def shot(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    from PIL import Image
    return Image.open(path).convert("RGB").copy()


def overlay(img, bones, labels=True, new=()):
    from PIL import ImageDraw, ImageFont
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(FONT, 13)
    for name, a, b, y in sorted(bones, key=lambda x: -x[3]):
        far = name.endswith(".L")
        col = (40, 120, 255) if far else (230, 40, 30)
        if name in new or any(name.startswith(n) for n in new):
            col = (255, 170, 0)
        d.line([a, b], fill=col, width=3)
        d.ellipse([a[0] - 3, a[1] - 3, a[0] + 3, a[1] + 3], fill=col)
        if labels and not far:
            torso = name in ("hips", "spine_low", "spine_mid", "chest", "neck", "head", "spine") or name.startswith("clavicle")
            if torso:
                d.text((a[0] - 8 - f.getlength(name.replace(".R", "")), a[1] - 7), name.replace(".R", ""), fill=col, font=f)
                d.line([(a[0] - 6, a[1]), a], fill=col, width=1)
            else:
                d.text((b[0] + 5, b[1] - 7), name.replace(".R", ""), fill=col, font=f)
    return img


def setup(res=(320, 360)):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 8
    sc.cycles.device = "CPU"
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.9, 0.9, 0.89, 1)
    sc.world = w
    cd = bpy.data.cameras.new("c")
    cd.type = "ORTHO"
    cd.ortho_scale = 1.8
    cam = bpy.data.objects.new("c", cd)
    sc.collection.objects.link(cam)
    sc.camera = cam
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 2.6
    sun.rotation_euler = (math.radians(50), math.radians(-20), 0)
    sc.collection.objects.link(sun)
    return cam


def aim(cam, at, scale, view="side"):
    cam.data.ortho_scale = scale
    cam.rotation_mode = "XYZ"
    if view == "side":
        cam.location = (at[0], -8, at[1])
        cam.rotation_euler = (math.radians(90), 0, 0)
    else:                                                   # from the front (the man faces the camera)
        cam.location = (8 + at[0], 0, at[1])
        cam.rotation_euler = (math.radians(90), 0, math.radians(90))


def look(cam, frm, at, scale):
    cam.data.ortho_scale = scale
    cam.location = frm
    cam.rotation_mode = "QUATERNION"
    cam.rotation_quaternion = (Vector(at) - Vector(frm)).to_track_quat("-Z", "Y")


def blend(a, b, k=0.5):
    from PIL import Image
    return Image.blend(a, b, k)


def only(*objs):
    keep = set()
    for o in objs:
        keep.add(o.name)
        keep.update(c.name for c in o.children)
    for o in bpy.data.objects:
        if o.type in ("ARMATURE", "MESH"):
            o.hide_render = o.name not in keep


def main():
    from PIL import Image, ImageDraw, ImageFont
    big = ImageFont.truetype(FONT, 24)
    small = ImageFont.truetype(FONT, 16)
    bs.reset_scene()
    cam = setup()
    size = (320, 360)
    old = sl.rig("Old", 0.02, 0.10)
    new = new_man("New", 0.02, 0.10)
    foe = new_man("Foe", 0.55, 0.72, x=0.62, facing=-1)
    NEW_BONES = ("spine_low", "spine_mid", "neck", "clavicle", "hand", "fingers", "foot", "toes")

    rows = []

    def row(title, tiles, note=""):
        W = size[0]
        r = Image.new("RGB", (W * len(tiles), size[1] + 66), (24, 24, 24))
        dr = ImageDraw.Draw(r)
        dr.text((10, 6), title, fill=(255, 210, 0), font=big)
        if note:
            dr.text((10, 36), note, fill=(205, 205, 205), font=small)
        for i, (cap, img) in enumerate(tiles):
            r.paste(img, (i * W, 66))
            ImageDraw.Draw(r).text((i * W + 8, 70), cap, fill=(30, 30, 30), font=small)
        rows.append(r)

    # ---- row 1: the skeletons
    tiles = []
    only(old)
    vr.apply_sampled(old, vr.sample([(0, vr.resolve(vc.ST), "lin")], 0))
    bpy.context.view_layer.update()
    aim(cam, (0.1, 0.72), 1.75)
    tiles.append(("NOW · 14 bones", overlay(shot(TMP), bones_px(old, cam, size))))
    only(new)
    pose(new, GUARD)
    aim(cam, (0.1, 0.72), 1.75)
    tiles.append(("PROPOSED · 29 bones (new in orange)", overlay(shot(TMP), bones_px(new, cam, size), new=NEW_BONES)))
    pose(new, {}, hips=(0, 0.70))
    aim(cam, (0.0, 0.72), 1.75)
    tiles.append(("PROPOSED · rest, side", overlay(shot(TMP), bones_px(new, cam, size), new=NEW_BONES)))
    aim(cam, (0.0, 0.72), 1.75, "front")
    tiles.append(("PROPOSED · rest, front", overlay(shot(TMP), bones_px(new, cam, size), labels=False, new=NEW_BONES)))
    row("THE SKELETON", tiles, "red = near side · blue = far side · orange = new bones: spine x3, neck, collarbones, wrists, fingers, feet, toes")

    # ---- row 2: the same blows, now and proposed
    tiles = []
    for cap, oldspec, of, newpose in (("GUARD", vc.CLIPS["idle"], 0, GUARD), ("JAB · impact", OLD_JAB, 3, JAB), ("CROSS · impact", OLD_CROSS, 5, CROSS)):
        only(old)
        old_pose(old, oldspec, of)
        aim(cam, (0.25, 0.72), 1.75)
        tiles.append(("NOW · " + cap, shot(TMP)))
        only(new)
        pose(new, newpose)
        aim(cam, (0.25, 0.72), 1.75)
        tiles.append(("PROPOSED · " + cap, shot(TMP)))
    row("THE SAME BLOWS", tiles, "proposed: heel up and pivot on the ball of the foot · shoulder reaching through · chin behind the shoulder · fist turned over · spine coiled")

    # ---- row 3: what the new bones do, close up
    tiles = []
    only(new)
    for cap, foot, toes, hz in (("FOOT · flat", 92, 92, 0.66), ("FOOT · heel up, on the ball", 40, 90, 0.66), ("FOOT · up on the toes", 10, 40, 0.72)):
        pose(new, P(GUARD, foot_R=(foot,), toes_R=(toes,), thigh_R=(-6,), shin_R=(-10,)), hips=(0, hz))
        aim(cam, (-0.1, 0.12), 0.55)
        tiles.append((cap, overlay(shot(TMP), [b for b in bones_px(new, cam, size) if "R" in b[0] and ("foot" in b[0] or "toes" in b[0] or "shin" in b[0])], new=NEW_BONES)))
    for cap, hand, fing, tw in (("HAND · fist (knuckles forward)", 90, 260, 0), ("HAND · open", 90, 90, 0),
                                ("HAND · gripping (round a neck)", 70, 165, 0)):
        pose(new, P(GUARD, upperarm_R=(40,), forearm_R=(90,), hand_R=(hand, 0, tw), fingers_R=(fing, 0, tw)))
        hp = new.matrix_world @ new.pose.bones["hand.R"].head
        at = (hp.x + 0.04, hp.y, hp.z - 0.01)
        look(cam, (at[0] + 0.08, at[1] - 0.7, at[2] + 0.16), at, 0.34)
        tiles.append((cap, shot(TMP)))
    row("WHAT THE NEW BONES DO (1)", tiles, "the foot rolls heel to toe, so steps push off and pivots turn on the ball · the hand opens, grips, turns over")

    tiles = []
    only(new)
    STRAIGHT_ARMS = dict(upperarm_L=(8,), forearm_L=(8,), hand_L=(8,), fingers_L=(8,), upperarm_R=(-8,), forearm_R=(-8,), hand_R=(-8,), fingers_R=(-8,))
    # shrug: from the front, shoulders down, then hunched up round the chin
    pose(new, P(GUARD, **STRAIGHT_ARMS, clavicle_L=("dir", (0, 1, -0.08), 0), clavicle_R=("dir", (0, -1, -0.08), 0), neck=(178,), head=(180,)))
    aim(cam, (0.0, 0.85), 1.3, "front")
    a = shot(TMP)
    pose(new, P(GUARD, **STRAIGHT_ARMS, clavicle_L=("dir", (0.1, 1, 0.9), 0), clavicle_R=("dir", (0.1, -1, 0.9), 0), neck=(160,), head=(172,)))
    tiles.append(("SHOULDERS · down / shrugged (front)", blend(a, shot(TMP), 0.6)))
    # reach: the same straight arm without and with the collarbone thrown forward
    ARM = dict(upperarm_R=(92,), forearm_R=(92,), hand_R=(92,), fingers_R=(182,))
    pose(new, P(GUARD, **ARM, clavicle_R=("dir", (0.0, -1, 0.1), 0)))
    aim(cam, (0.35, 0.95), 1.0)
    a = shot(TMP)
    pose(new, P(GUARD, **ARM, clavicle_R=("dir", (0.9, -0.42, 0.15), 0), chest=(156, 0, 20), neck=(148,), head=(168,)))
    tiles.append(("SHOULDER · reach: +9 cm (ghosted)", blend(a, shot(TMP), 0.6)))
    # the clinch: hands cupped round his neck, his head pulled down
    only(new, foe)
    CL = P(GUARD, hips=(180, 0, 0), spine_low=(172,), spine_mid=(164,), chest=(156,), neck=(150,), head=(160,),
           clavicle_L=("dir", (0.35, 1, 0.1), 0), clavicle_R=("dir", (0.35, -1, 0.1), 0),
           upperarm_L=(100, -14), forearm_L=(84, -40), hand_L=(60, -60), fingers_L=(10, -80),
           upperarm_R=(96, 14), forearm_R=(82, 40), hand_R=(60, 60), fingers_R=(10, 80),
           thigh_L=(20,), shin_L=(-4,), foot_L=(92,), thigh_R=(-16,), shin_R=(-24,), foot_R=(70,), toes_R=(90,))
    pose(new, CL)
    pose(foe, P(GUARD, spine_low=(160,), spine_mid=(146,), chest=(132,), neck=(124,), head=(130,),
                upperarm_L=(40, -10), forearm_L=(110, -20), upperarm_R=(30, 10), forearm_R=(100, 20),
                thigh_L=(30,), shin_L=(0,), thigh_R=(-10,), shin_R=(-24,)), hips=(0, 0.62))
    aim(cam, (0.32, 0.8), 1.6)
    tiles.append(("CLINCH · hands cupped round his neck", shot(TMP)))
    look(cam, (2.2, -3.0, 1.7), (0.31, 0, 0.85), 1.7)
    tiles.append(("CLINCH · three-quarter view", shot(TMP)))
    look(cam, (0.5, -1.4, 1.45), (0.38, 0, 1.02), 0.7)
    tiles.append(("CLINCH · close: fingers round his neck", shot(TMP)))
    row("WHAT THE NEW BONES DO (2)", tiles, "the collarbone shrugs (chin tucked) and reaches (the punch's last inches) · the clinch: real grips on the neck")

    width = max(r.width for r in rows)
    sheet = Image.new("RGB", (width, sum(r.height for r in rows)), (24, 24, 24))
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(OUT)
    os.remove(TMP)
    print("wrote", OUT, sheet.size)


if __name__ == "__main__":
    main()
