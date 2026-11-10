"""STRIKE LAB: the reference sheet for the new body and the new hand-keyed strikes (for review
before anything changes in the game; docs/design/combat_v3.md).

    /home/user/.venv-blender/bin/python tools/blender/strike_lab.py

Writes docs/previews/strike_lab.png:
  row 0      the old body and the new body, side by side, in their guards
  rows 1-6   jab, cross, hook, uppercut, low kick, roundhouse: STANCE, LOAD, LAUNCH, IMPACT, FOLLOW,
             RECOVER, with frame counts at 60 fps, a target man reacting where he is hit, and at
             impact the limb stretched along its path (a smear of the frame before behind it)

The new body: arms 25% longer (0.26 + 0.24 + a 0.08 fist), visible fists, the shoulders and hips
set wider apart (0.11) so the near and far limbs separate in a side view, and the far-side limbs a
shade lighter than the near ones (the old 2D trick: you always see which hand is punching).
Angles are absolute, in the side plane: 0 hangs down, 90 points forward, 180 points up."""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as bs  # noqa: E402

ROOT = bs.ROOT
OUT = os.path.join(ROOT, "docs", "previews", "strike_lab.png")
TMP = os.path.join(ROOT, "docs", "previews", "_lab.png")
OLD_BONES = dict(bs.BONES)

# ---------------------------------------------------------------- the new body
UP, FO, HA, DD = 0.26, 0.24, 0.08, 0.11
SH = bs.SHOULDER


def new_bones():
    B = dict(OLD_BONES)
    for s, y in (("L", DD), ("R", -DD)):
        B["upperarm." + s] = ("chest", (0, 0, SH), (0, 0, SH - UP), y, 0, 0.058)
        B["forearm." + s] = ("upperarm." + s, (0, 0, SH - UP), (0, 0, SH - UP - FO), y, 0, 0.055)
        B["hand." + s] = ("forearm." + s, (0, 0, SH - UP - FO), (0, 0, SH - UP - FO - HA), y, 0, 0.0)
        B["thigh." + s] = ("hips", (0, 0, bs.HIP_Z), (0, 0, bs.HIP_Z - bs.THIGH), y * 0.8, 0, 0.072)
        B["shin." + s] = ("thigh." + s, (0, 0, bs.HIP_Z - bs.THIGH), (0, 0, bs.HIP_Z - bs.THIGH - bs.SHIN), y * 0.8, 0, 0.066)
    return B


def mat(name, g):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (g, g, g * 1.02, 1)
    b.inputs["Roughness"].default_value = 0.7
    return m


def build_body(arm, near_g, far_g, fists=True, name="Body"):
    """The ink body: one mesh, the far-side limbs a lighter grey, fists on the hands."""
    near, far = mat(name + "Near", near_g), mat(name + "Far", far_g)
    parts = []

    def part(obj, bone, m):
        obj.data.materials.append(m)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    for nm, (_p, head, tail, y, _rest, radius) in bs.BONES.items():
        h = Vector(head) + Vector((0, y, 0))
        t = Vector(tail) + Vector((0, y, 0))
        m = far if nm.endswith(".L") else near
        if nm == "head":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=bs.HEAD_RADIUS, segments=24, ring_count=14, location=(0, 0, bs.HEAD_CENTER))
            part(bpy.context.object, nm, near)
            continue
        if nm == "spine":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.165, segments=18, ring_count=10, location=(0, 0, 0.81))
            bpy.context.object.scale = (1.0, 1.05, 1.12)
            part(bpy.context.object, nm, near)
        if nm == "chest":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.158, segments=18, ring_count=10, location=(0, 0, 0.935))
            part(bpy.context.object, nm, near)
        if nm in ("shin.L", "shin.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.078, segments=12, ring_count=8, location=t + Vector((0.04, 0, -0.005)))
            bpy.context.object.scale = (1.45, 1.0, 0.8)
            part(bpy.context.object, nm, m)
        if fists and nm in ("hand.L", "hand.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.072, segments=14, ring_count=10, location=(h + t) / 2)
            bpy.context.object.scale = (1.05, 1.0, 1.15)
            part(bpy.context.object, nm, m)
            continue
        if radius <= 0:
            continue
        bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=(t - h).length, vertices=12, location=(h + t) / 2)
        part(bpy.context.object, nm, m)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=t)
        part(bpy.context.object, nm, m)
        if nm in ("upperarm.L", "upperarm.R", "thigh.L", "thigh.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=h)
            part(bpy.context.object, nm, m)
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = name
    bpy.ops.object.shade_smooth()
    body.parent = arm
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    return body


def rig(name, near_g, far_g, x=0.0, facing=1, fists=True):
    arm = bs.build_armature()
    arm.name = name
    build_body(arm, near_g, far_g, fists, name + "Body")
    arm.location = (x, 0, 0)
    arm.rotation_euler = (0, 0, 0 if facing > 0 else math.pi)
    return arm


def put(arm, p, stretch=None):
    """Pose it; `stretch` = {bone: factor} lengthens limbs along their path (the impact smear)."""
    bs.apply_pose(arm, p)
    for pb in arm.pose.bones:
        pb.scale = (1, 1, 1)
    for b, f in (stretch or {}).items():
        arm.pose.bones[b].scale = (1, f, 1)
    bpy.context.view_layer.update()


# ---------------------------------------------------------------- the poses (hero faces +x)
P = bs.pose
# the guard: side-on, chin down, lead (far, L) fist forward at chin height, rear (near, R) fist at
# the cheek, elbows in, knees soft, weight 60/40 on the back foot
STANCE = P(bs.STANCE, hips=180, spine=172, chest=166, head=170,
           upperarm_L=62, forearm_L=150, upperarm_R=22, forearm_R=168,
           thigh_L=24, shin_L=-2, thigh_R=-14, shin_R=-30)
# the old guard, for the comparison
OLD = bs.STANCE

TARGET = P(bs.STANCE, spine=176, chest=172, head=176, upperarm_L=55, forearm_L=150, upperarm_R=25, forearm_R=165,
           thigh_L=20, shin_L=0, thigh_R=-12, shin_R=-26)
T_HEAD = P(TARGET, spine=188, chest=196, head=214, upperarm_L=40, forearm_L=110, upperarm_R=10, forearm_R=120)    # snapped back
T_TURN = P(TARGET, spine=186, chest=192, head=226, upperarm_L=70, forearm_L=60, upperarm_R=-10, forearm_R=90)      # hook: spun
T_LIFT = P(TARGET, spine=192, chest=204, head=232, upperarm_L=100, forearm_L=150, upperarm_R=60, forearm_R=140, dz=0.06)   # uppercut: off his heels
T_LEG = P(TARGET, spine=170, chest=166, head=164, thigh_L=50, shin_L=-36, thigh_R=-30, shin_R=-40, dz=-0.1)       # leg kicked out
T_FOLD = P(TARGET, spine=150, chest=128, head=140, upperarm_L=20, forearm_L=110, upperarm_R=10, forearm_R=104, dz=-0.06)   # body: folded

ARM_L = {"upperarm.L": 1.07, "forearm.L": 1.07}
ARM_R = {"upperarm.R": 1.07, "forearm.R": 1.07}
LEG_R = {"thigh.R": 1.06, "shin.R": 1.06}

STRIKES = [
    ("JAB", "lead hand, to the head · 15 frames (0.25 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 2, P(STANCE, chest=170, upperarm_L=56, forearm_L=140, dz=-0.02), None, TARGET),
        ("LAUNCH", 4, P(STANCE, spine=168, chest=160, upperarm_L=78, forearm_L=100, thigh_L=30, shin_L=-4, dx=0.12), None, TARGET),
        ("IMPACT", 5, P(STANCE, spine=166, chest=154, head=166, upperarm_L=76, forearm_L=74, thigh_L=32, shin_L=-4, thigh_R=-20, shin_R=-36, dx=0.2), ARM_L, T_HEAD),
        ("FOLLOW", 9, P(STANCE, chest=160, upperarm_L=72, forearm_L=128, dx=0.1), None, T_HEAD),
        ("RECOVER", 15, STANCE, None, TARGET),
    ]),
    ("CROSS", "rear hand, the power: the rear heel turns, the hips drive · 20 frames (0.33 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 3, P(STANCE, chest=176, spine=176, upperarm_R=12, forearm_R=164, thigh_R=-24, shin_R=-34, dz=-0.03), None, TARGET),
        ("LAUNCH", 6, P(STANCE, spine=166, chest=156, upperarm_R=66, forearm_R=108, upperarm_L=40, forearm_L=162, thigh_R=-26, shin_R=-50, dx=0.1), None, TARGET),
        ("IMPACT", 7, P(STANCE, spine=160, chest=146, head=162, upperarm_R=80, forearm_R=78, upperarm_L=36, forearm_L=164,
                        thigh_L=34, shin_L=-2, thigh_R=-34, shin_R=-46, dx=0.18), ARM_R, T_HEAD),
        ("FOLLOW", 12, P(STANCE, spine=166, chest=156, upperarm_R=60, forearm_R=140, dx=0.1), None, T_HEAD),
        ("RECOVER", 20, STANCE, None, TARGET),
    ]),
    ("HOOK", "lead hand, round the side: elbow up at shoulder height, the body turns through it · 21 frames (0.35 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 3, P(STANCE, chest=178, spine=176, upperarm_L=46, forearm_L=128, dz=-0.04), None, TARGET),
        ("LAUNCH", 6, P(STANCE, chest=160, spine=168, upperarm_L=100, forearm_L=150, dx=0.1), None, TARGET),
        ("IMPACT", 7, P(STANCE, chest=144, spine=162, head=156, upperarm_L=96, forearm_L=88, thigh_L=34, shin_L=-6, thigh_R=-22, shin_R=-40, dx=0.2), {"upperarm.L": 1.04, "forearm.L": 0.8}, T_TURN),
        ("FOLLOW", 12, P(STANCE, chest=150, spine=166, upperarm_L=88, forearm_L=30, dx=0.14), {"forearm.L": 0.8}, T_TURN),
        ("RECOVER", 21, STANCE, None, TARGET),
    ]),
    ("UPPERCUT", "rear hand, from below: dip, then drive up off the legs · 24 frames (0.4 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 4, P(STANCE, spine=160, chest=150, head=160, upperarm_R=-6, forearm_R=110, thigh_L=46, shin_L=-10, thigh_R=-4, shin_R=-46, dz=-0.12, dx=0.12), None, TARGET),
        ("LAUNCH", 7, P(STANCE, spine=172, chest=168, upperarm_R=60, forearm_R=150, dz=-0.05, dx=0.24), None, TARGET),
        ("IMPACT", 8, P(STANCE, spine=180, chest=180, head=176, upperarm_R=96, forearm_R=142, upperarm_L=40, forearm_L=160,
                        thigh_L=16, shin_L=4, thigh_R=-10, shin_R=-24, dx=0.34), ARM_R, T_LIFT),
        ("FOLLOW", 14, P(STANCE, spine=180, chest=182, upperarm_R=118, forearm_R=172, dx=0.26), None, T_LIFT),
        ("RECOVER", 24, STANCE, None, TARGET),
    ]),
    ("LOW KICK", "rear leg, the shin into his thigh: the hips turn over, the arms balance · 24 frames (0.4 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 4, P(STANCE, thigh_L=30, shin_L=4, spine=178, chest=176, thigh_R=-30, shin_R=-48), None, TARGET),
        ("LAUNCH", 7, P(STANCE, spine=186, chest=190, thigh_R=34, shin_R=-24, upperarm_R=-30, forearm_R=20, thigh_L=6, shin_L=2), None, TARGET),
        ("IMPACT", 9, P(STANCE, spine=192, chest=198, head=186, thigh_R=62, shin_R=68, upperarm_R=-44, forearm_R=-10, upperarm_L=70, forearm_L=150,
                        thigh_L=-2, shin_L=0, dx=0.12), LEG_R, T_LEG),
        ("FOLLOW", 15, P(STANCE, spine=184, chest=188, thigh_R=30, shin_R=-10), None, T_LEG),
        ("RECOVER", 24, STANCE, None, TARGET),
    ]),
    ("ROUNDHOUSE", "rear leg to the body: the knee chambers high, the standing foot pivots, the body leans away · 33 frames (0.55 s)", [
        ("STANCE", 0, STANCE, None, TARGET),
        ("LOAD", 5, P(STANCE, spine=182, chest=184, thigh_R=78, shin_R=-18, thigh_L=4, shin_L=0, upperarm_R=10, forearm_R=150), None, TARGET),
        ("LAUNCH", 9, P(STANCE, spine=198, chest=204, head=192, thigh_R=96, shin_R=40), None, TARGET),
        ("IMPACT", 11, P(STANCE, spine=206, chest=214, head=198, thigh_R=100, shin_R=96, upperarm_R=-40, forearm_R=-6, upperarm_L=80, forearm_L=150,
                         thigh_L=-6, shin_L=-4, dx=0.1), LEG_R, T_FOLD),
        ("FOLLOW", 18, P(STANCE, spine=194, chest=198, thigh_R=80, shin_R=-14), None, T_FOLD),
        ("RECOVER", 33, STANCE, None, TARGET),
    ]),
]


# ---------------------------------------------------------------- render

def setup():
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 6
    sc.cycles.device = "CPU"
    sc.render.resolution_x, sc.render.resolution_y = 300, 300
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.88, 0.88, 0.87, 1)
    sc.world = w
    cd = bpy.data.cameras.new("c")
    cd.type = "ORTHO"
    cd.ortho_scale = 2.5
    cam = bpy.data.objects.new("c", cd)
    sc.collection.objects.link(cam)
    cam.location = (0.45, -8, 0.82)
    cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 2.5
    sun.rotation_euler = (math.radians(50), math.radians(-20), 0)
    sc.collection.objects.link(sun)
    return cam


def shot(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    from PIL import Image
    return Image.open(path).convert("RGB").copy()


def to_px(cam, p):
    s = cam.data.ortho_scale
    return (150 + (p.x - cam.location.x) / s * 300, 150 - (p.z - cam.location.z) / s * 300)


def main():
    from PIL import Image, ImageDraw, ImageFont
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 20)
    small = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 15)
    bs.reset_scene()
    cam = setup()

    # row 0: the old body and the new one
    bs.BONES.clear(); bs.BONES.update(OLD_BONES)
    old = rig("Old", 0.02, 0.02, fists=False)
    put(old, OLD)
    img_old = shot(TMP)
    for o in [o for o in bpy.data.objects if o.name.startswith("Old")]:
        bpy.data.objects.remove(o, do_unlink=True)
    bs.BONES.clear(); bs.BONES.update(new_bones())
    hero = rig("Hero", 0.02, 0.10)          # far limbs: near-black charcoal (fights only; pure ink in cutscenes)
    put(hero, STANCE)
    img_new = shot(TMP)
    tgt = rig("Target", 0.55, 0.72, x=0.98, facing=-1)

    cols = 6
    W, H = 300, 300
    head_h = 36
    rows = []
    # the comparison row
    r0 = Image.new("RGB", (W * cols, H + head_h), (24, 24, 24))
    d0 = ImageDraw.Draw(r0)
    r0.paste(img_old, (0, head_h)); r0.paste(img_new, (W, head_h))
    d0.text((8, 8), "OLD BODY  ·  old guard", fill=(255, 210, 0), font=font)
    d0.text((W + 8, 8), "NEW BODY  ·  new guard", fill=(255, 210, 0), font=font)
    notes = ["Arms +25%, fists you can see, shoulders and hips wider apart.",
             "Far limbs a touch lighter (charcoal) IN FIGHTS ONLY, by depth",
             "from the camera, so it is right facing either way; it fades",
             "to pure ink in every cutscene, close-up and silhouette shot.",
             "",
             "Guard: side-on, chin down, lead fist forward,",
             "rear fist at the cheek, knees soft."]
    for i, line in enumerate(notes):
        d0.text((2 * W + 16, head_h + 14 + i * 26), line, fill=(230, 230, 230), font=font)
    rows.append(r0)

    for name, desc, keys in STRIKES:
        tiles = []
        prev = None
        for label, frame, p, stretch, tpose in keys:
            put(hero, p, stretch)
            put(tgt, tpose)
            img = shot(TMP)
            if label == "IMPACT" and prev is not None:
                # the smear: the frame before, ghosted behind the impact
                img = Image.blend(prev, img, 0.72)
                # the contact: a burst where the fist or foot lands
                tip = None
                for b in (stretch or {}):
                    if b.startswith(("forearm", "shin")):
                        pb = hero.pose.bones[b.replace("forearm", "hand") if b.startswith("forearm") else b]
                        tip = hero.matrix_world @ pb.tail
                if tip is not None:
                    x, y = to_px(cam, tip)
                    dr = ImageDraw.Draw(img)
                    if name == "HOOK":
                        dr.arc([x - 120, y - 40, x + 10, y + 70], start=200, end=330, fill=(150, 150, 150), width=6)
                    for k in range(10):
                        a = k / 10 * math.tau
                        r1, r2 = 10, 26 + (k % 2) * 10
                        dr.line([(x + math.cos(a) * r1, y + math.sin(a) * r1), (x + math.cos(a) * r2, y + math.sin(a) * r2)], fill=(200, 20, 20), width=3)
            if label != "IMPACT":
                prev = img
            tiles.append((label, frame, img))
        row = Image.new("RGB", (W * cols, H + head_h), (24, 24, 24))
        dr = ImageDraw.Draw(row)
        dr.text((8, 6), name, fill=(255, 210, 0), font=font)
        dr.text((8 + 14 * len(name) + 20, 9), desc, fill=(200, 200, 200), font=small)
        for i, (label, frame, img) in enumerate(tiles):
            row.paste(img, (i * W, head_h))
            dd = ImageDraw.Draw(row)
            col = (200, 20, 20) if label == "IMPACT" else (40, 40, 40)
            dd.text((i * W + 8, head_h + 6), f"{label}  ·  f{frame}", fill=col, font=small)
        rows.append(row)
    sheet = Image.new("RGB", (W * cols, sum(r.height for r in rows)), (24, 24, 24))
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(OUT)
    os.remove(TMP)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
