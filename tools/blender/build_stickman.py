"""Generate the Project INK stickman: mesh, rig and animation clips.

Run with the Blender Python module (bpy):
    python tools/blender/build_stickman.py [--preview]

Outputs:
    Assets/Art/Stickman/Stickman.fbx       model + rig + every clip as an FBX take
    Assets/Art/Stickman/stickman_clips.json clip timing data read by the Unity setup
    docs/previews/<clip>.png                contact sheets of the hand-keyed clips (with --preview)

Motion: most moves are real motion capture (CMU database) retargeted by retarget.py, with
the windows chosen in mocap_clips.py; hit reactions and the enemy wind-up are hand-keyed
below. Pass --no-mocap to build only the hand-keyed set. Preview mocap clips with
preview_mocap.py.

Posing model
------------
The fight reads side-on, so every bone bends in the X/Z plane (character faces +X).
A pose gives each bone an ABSOLUTE angle in degrees, measured from straight down
(0) towards the front (+90); straight up is 180, pointing backwards is -90.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "Stickman")
PREVIEW_DIR = os.path.join(ROOT, "docs", "previews")
FPS = 30

# ---------------------------------------------------------------- skeleton
# name: (parent, head, tail, depth y, rest angle, radius of the limb mesh)
HIP_Z = 0.95
THIGH, SHIN = 0.45, 0.44
UPPER, FORE = 0.28, 0.27
BONES = {
    "hips":      (None,    (0, 0, HIP_Z),  (0, 0, 1.05), 0.0, 180, 0.072),
    "spine":     ("hips",  (0, 0, 1.05),   (0, 0, 1.30), 0.0, 180, 0.070),
    "chest":     ("spine", (0, 0, 1.30),   (0, 0, 1.50), 0.0, 180, 0.074),
    "head":      ("chest", (0, 0, 1.52),   (0, 0, 1.80), 0.0, 180, 0.0),
    "upperarm.L": ("chest", (0, 0, 1.48), (0, 0, 1.48 - UPPER), 0.05, 0, 0.052),
    "forearm.L": ("upperarm.L", (0, 0, 1.48 - UPPER), (0, 0, 1.48 - UPPER - FORE), 0.05, 0, 0.047),
    "upperarm.R": ("chest", (0, 0, 1.48), (0, 0, 1.48 - UPPER), -0.05, 0, 0.052),
    "forearm.R": ("upperarm.R", (0, 0, 1.48 - UPPER), (0, 0, 1.48 - UPPER - FORE), -0.05, 0, 0.047),
    "thigh.L":   ("hips",  (0, 0, HIP_Z), (0, 0, HIP_Z - THIGH), 0.05, 0, 0.060),
    "shin.L":    ("thigh.L", (0, 0, HIP_Z - THIGH), (0, 0, HIP_Z - THIGH - SHIN), 0.05, 0, 0.054),
    "thigh.R":   ("hips",  (0, 0, HIP_Z), (0, 0, HIP_Z - THIGH), -0.05, 0, 0.060),
    "shin.R":    ("thigh.R", (0, 0, HIP_Z - THIGH), (0, 0, HIP_Z - THIGH - SHIN), -0.05, 0, 0.054),
}
HEAD_RADIUS = 0.165
FOOT_Z = HIP_Z - THIGH - SHIN  # shin tip height in the rest pose (0.06)

# ---------------------------------------------------------------- poses
# L = far side (lead side in the guard), R = near side (rear / power side).
STANCE = dict(hips=180, spine=176, chest=172, head=178,
              **{"upperarm.L": 50, "forearm.L": 150, "upperarm.R": 25, "forearm.R": 160,
                 "thigh.L": 28, "shin.L": 2, "thigh.R": -18, "shin.R": -8})


def pose(base=STANCE, dz=None, dx=0.0, **kw):
    """A pose = base angles overridden by keyword angles. Dotted bone names use _L/_R."""
    p = dict(base)
    for k, v in kw.items():
        p[k.replace("_L", ".L").replace("_R", ".R")] = v
    p["_dz"] = dz  # None -> keep the lowest foot on the ground
    p["_dx"] = dx
    return p


def rotated(p, delta, dz=None):
    """Rotate the whole body rigidly (used for falls and flips)."""
    q = {k: (v + delta if not k.startswith("_") else v) for k, v in p.items()}
    q["_dz"] = dz
    q.setdefault("_dx", 0.0)
    return q


def mirror_legs_arms(p):
    q = dict(p)
    for a, b in (("upperarm.L", "upperarm.R"), ("forearm.L", "forearm.R"),
                 ("thigh.L", "thigh.R"), ("shin.L", "shin.R")):
        q[a], q[b] = p[b], p[a]
    return q


RUN_A = pose(spine=166, chest=164, head=172,
             upperarm_L=-40, forearm_L=30, upperarm_R=45, forearm_R=125,
             thigh_L=36, shin_L=12, thigh_R=-26, shin_R=-52)
RUN_PASS_A = pose(spine=166, chest=164, head=172,
                  upperarm_L=0, forearm_L=70, upperarm_R=5, forearm_R=95,
                  thigh_L=-4, shin_L=-6, thigh_R=55, shin_R=-35, dz=-0.02)
RUN_B = mirror_legs_arms(RUN_A)
RUN_PASS_B = mirror_legs_arms(RUN_PASS_A)

STRAIGHT = pose(spine=180, chest=180, head=180, upperarm_L=10, forearm_L=10,
                upperarm_R=-10, forearm_R=-10, thigh_L=4, shin_L=2, thigh_R=-4, shin_R=-2)

# Each clip: frames, loop flag, hit frame (or None), and (frame, pose) keys.
CLIPS = {
    "idle": dict(frames=24, loop=True, hit=None, keys=[
        (0, STANCE),
        (12, pose(chest=169, spine=175, forearm_L=146, forearm_R=156, dz=-0.075)),
        (24, STANCE)]),
    "run": dict(frames=16, loop=True, hit=None, keys=[
        (0, RUN_A), (4, RUN_PASS_A), (8, RUN_B), (12, RUN_PASS_B), (16, RUN_A)]),
    "jab": dict(frames=12, loop=False, hit=4, keys=[
        (0, STANCE),
        (2, pose(chest=176, upperarm_L=60, forearm_L=140)),
        (4, pose(spine=172, chest=164, upperarm_L=90, forearm_L=91, thigh_L=34)),
        (7, pose(spine=172, chest=165, upperarm_L=88, forearm_L=96, thigh_L=34)),
        (12, STANCE)]),
    "cross": dict(frames=14, loop=False, hit=5, keys=[
        (0, STANCE),
        (2, pose(chest=184, upperarm_R=10, forearm_R=150)),
        (5, pose(spine=168, chest=158, head=170, upperarm_R=93, forearm_R=90,
                 upperarm_L=30, forearm_L=160, thigh_L=36, thigh_R=-34, shin_R=-20)),
        (8, pose(spine=168, chest=160, head=170, upperarm_R=90, forearm_R=95,
                 upperarm_L=30, forearm_L=160, thigh_L=36, thigh_R=-34, shin_R=-20)),
        (14, STANCE)]),
    "front_kick": dict(frames=16, loop=False, hit=6, keys=[
        (0, STANCE),
        (3, pose(chest=186, spine=184, thigh_R=95, shin_R=-5, thigh_L=6, shin_L=0)),
        (6, pose(chest=194, spine=188, head=184, thigh_R=92, shin_R=92, thigh_L=4, shin_L=0,
                 upperarm_L=70, forearm_L=150, upperarm_R=-20, forearm_R=60)),
        (9, pose(chest=188, spine=184, thigh_R=95, shin_R=10, thigh_L=6, shin_L=0)),
        (16, STANCE)]),
    "roundhouse": dict(frames=18, loop=False, hit=7, keys=[
        (0, STANCE),
        (3, pose(chest=192, spine=188, thigh_R=105, shin_R=15, thigh_L=-4, shin_L=0)),
        (7, pose(chest=205, spine=196, head=190, thigh_R=118, shin_R=122, thigh_L=-8, shin_L=0,
                 upperarm_L=115, forearm_L=135, upperarm_R=-45, forearm_R=-10)),
        (10, pose(chest=202, spine=195, head=188, thigh_R=116, shin_R=118, thigh_L=-8, shin_L=0,
                  upperarm_L=110, forearm_L=135, upperarm_R=-45, forearm_R=-10)),
        (13, pose(chest=190, spine=186, thigh_R=100, shin_R=20, thigh_L=-4, shin_L=0)),
        (18, STANCE)]),
    "back_kick": dict(frames=16, loop=False, hit=6, keys=[
        (0, STANCE),
        (3, pose(spine=160, chest=150, head=150, thigh_R=70, shin_R=-50, thigh_L=10, shin_L=0,
                 upperarm_L=80, forearm_L=120, upperarm_R=70, forearm_R=120)),
        (6, pose(spine=150, chest=138, head=135, thigh_R=-92, shin_R=-92, thigh_L=12, shin_L=0,
                 upperarm_L=95, forearm_L=110, upperarm_R=85, forearm_R=110)),
        (9, pose(spine=152, chest=140, head=138, thigh_R=-88, shin_R=-90, thigh_L=12, shin_L=0,
                 upperarm_L=95, forearm_L=110, upperarm_R=85, forearm_R=110)),
        (16, STANCE)]),
    "flying_kick": dict(frames=20, loop=False, hit=10, keys=[
        (0, STANCE),
        (3, pose(spine=165, chest=160, thigh_L=70, shin_L=-15, thigh_R=55, shin_R=-30,
                 upperarm_L=-30, upperarm_R=-40, forearm_L=20, forearm_R=20, dz=-0.22)),
        (6, pose(spine=178, chest=182, thigh_L=95, shin_L=-20, thigh_R=70, shin_R=-50,
                 upperarm_L=150, forearm_L=170, upperarm_R=120, forearm_R=160, dz=0.40)),
        (10, pose(spine=190, chest=198, head=188, thigh_L=96, shin_L=94, thigh_R=-30, shin_R=-110,
                  upperarm_L=155, forearm_L=175, upperarm_R=-60, forearm_R=-25, dz=0.50)),
        (13, pose(spine=188, chest=195, thigh_L=94, shin_L=90, thigh_R=-25, shin_R=-100,
                  upperarm_L=150, forearm_L=170, upperarm_R=-55, forearm_R=-20, dz=0.30)),
        (17, pose(spine=168, chest=164, thigh_L=60, shin_L=-10, thigh_R=-20, shin_R=-60,
                  upperarm_L=60, forearm_L=140, upperarm_R=10, forearm_R=120, dz=-0.18)),
        (20, STANCE)]),
    "dodge": dict(frames=14, loop=False, hit=None, keys=[
        (0, STANCE),
        (3, pose(spine=170, chest=168, thigh_L=60, shin_L=-10, thigh_R=40, shin_R=-40, dz=-0.20)),
        (7, rotated(pose(spine=180, chest=180, head=185, thigh_L=110, shin_L=-30, thigh_R=100,
                         shin_R=-40, upperarm_L=130, forearm_L=60, upperarm_R=120, forearm_R=50), 55,
                    dz=0.10)),
        (11, pose(spine=172, chest=170, thigh_L=50, shin_L=-10, thigh_R=-10, shin_R=-40, dz=-0.15)),
        (14, STANCE)]),
    "hit_front": dict(frames=12, loop=False, hit=None, keys=[
        (0, STANCE),
        (2, pose(spine=196, chest=210, head=222, upperarm_L=150, forearm_L=200,
                 upperarm_R=-35, forearm_R=20, thigh_L=10, thigh_R=-30, shin_R=-20)),
        (6, pose(spine=192, chest=200, head=205, upperarm_L=120, forearm_L=180,
                 upperarm_R=-20, forearm_R=40, thigh_L=12, thigh_R=-28, shin_R=-18)),
        (12, STANCE)]),
    "hit_back": dict(frames=12, loop=False, hit=None, keys=[
        (0, STANCE),
        (2, pose(spine=160, chest=146, head=135, upperarm_L=-60, forearm_L=-30,
                 upperarm_R=-75, forearm_R=-40, thigh_L=35, thigh_R=-10)),
        (6, pose(spine=166, chest=156, head=150, upperarm_L=-40, forearm_L=0,
                 upperarm_R=-50, forearm_R=-10, thigh_L=32, thigh_R=-12)),
        (12, STANCE)]),
    "knockdown": dict(frames=30, loop=False, hit=None, keys=[
        (0, STANCE),
        (3, pose(spine=200, chest=215, head=228, upperarm_L=160, forearm_L=210,
                 upperarm_R=150, forearm_R=200, thigh_L=30, thigh_R=10, dz=0.05)),
        (10, rotated(pose(STRAIGHT, upperarm_L=150, forearm_L=170, upperarm_R=140,
                          forearm_R=160, thigh_L=25, shin_L=0, thigh_R=5, shin_R=-10), 55, dz=-0.25)),
        (16, rotated(pose(STRAIGHT, upperarm_L=170, forearm_L=175, upperarm_R=160, forearm_R=165,
                          thigh_L=15, shin_L=-10, thigh_R=5, shin_R=-5), 90, dz=-0.84)),
        (30, rotated(pose(STRAIGHT, upperarm_L=165, forearm_L=170, upperarm_R=150, forearm_R=155,
                          thigh_L=20, shin_L=-15, thigh_R=5, shin_R=-5), 90, dz=-0.85))]),
    "windup": dict(frames=10, loop=False, hit=None, keys=[
        (0, STANCE),
        (6, pose(spine=184, chest=192, head=186, upperarm_R=-50, forearm_R=140,
                 upperarm_L=70, forearm_L=110, thigh_L=30, thigh_R=-25, shin_R=-15)),
        (10, pose(spine=185, chest=194, head=187, upperarm_R=-55, forearm_R=140,
                  upperarm_L=72, forearm_L=110, thigh_L=30, thigh_R=-25, shin_R=-15))]),
}


# ---------------------------------------------------------------- build
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.unit_settings.scale_length = 1.0


def build_armature():
    data = bpy.data.armatures.new("StickmanRig")
    arm = bpy.data.objects.new("Stickman", data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (parent, head, tail, y, _rest, _r) in BONES.items():
        b = data.edit_bones.new(name)
        b.head = Vector(head) + Vector((0, y, 0))
        b.tail = Vector(tail) + Vector((0, y, 0))
        b.align_roll(Vector((0, 1, 0)))  # local Z = world +Y: all bending is about one axis
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def build_mesh(arm):
    mat = bpy.data.materials.new("Ink")
    mat.diffuse_color = (0.02, 0.02, 0.02, 1)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.02, 0.02, 0.02, 1)
        bsdf.inputs["Roughness"].default_value = 0.6

    parts = []

    def add_part(obj, bone):
        obj.data.materials.append(mat)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    for name, (_p, head, tail, y, _rest, radius) in BONES.items():
        h = Vector(head) + Vector((0, y, 0))
        t = Vector(tail) + Vector((0, y, 0))
        if name == "head":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=HEAD_RADIUS, segments=20, ring_count=12,
                                                 location=(0, 0, 1.69))
            add_part(bpy.context.object, name)
            continue
        length = (t - h).length
        bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length, vertices=10,
                                            location=(h + t) / 2)
        add_part(bpy.context.object, name)
        # ball at the far joint so limbs read as one continuous ink line
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=t)
        add_part(bpy.context.object, name)
        if name in ("upperarm.L", "upperarm.R", "thigh.L", "thigh.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=h)
            add_part(bpy.context.object, name)

    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = "StickmanBody"
    bpy.ops.object.shade_smooth()
    body.parent = arm
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    return body


def rest_angle(name):
    return BONES[name][4]


def ground_dz(p):
    """Lift or drop the hips so the lowest foot touches the floor."""
    ext = []
    for side in ("L", "R"):
        th, sh = math.radians(p["thigh." + side]), math.radians(p["shin." + side])
        ext.append(THIGH * math.cos(th) + SHIN * math.cos(sh))
    return max(ext) + FOOT_Z - HIP_Z


def apply_pose(arm, p):
    for name in BONES:
        pb = arm.pose.bones[name]
        parent = BONES[name][0]
        delta = p[name] - rest_angle(name)
        if parent:
            delta -= p[parent] - rest_angle(parent)
        rest_rot = pb.bone.matrix_local.to_3x3()
        axis = (rest_rot.inverted() @ Vector((0, 1, 0))).normalized()
        pb.rotation_mode = "QUATERNION"
        # rotating about world +Y moves a bone towards -X, so negate to make + mean "forward"
        pb.rotation_quaternion = Quaternion(axis, -math.radians(delta))
        if name == "hips":
            dz = p.get("_dz")
            dz = ground_dz(p) if dz is None else dz
            pb.location = rest_rot.inverted() @ Vector((p.get("_dx", 0.0), 0, dz))


def build_actions(arm, skip=()):
    arm.animation_data_create()
    meta = []
    for clip, spec in CLIPS.items():
        if clip in skip:
            continue
        act = bpy.data.actions.new(clip)
        act.use_fake_user = True
        arm.animation_data.action = act
        for frame, p in spec["keys"]:
            apply_pose(arm, p)
            for name in BONES:
                pb = arm.pose.bones[name]
                pb.keyframe_insert("rotation_quaternion", frame=frame, group=name)
                if name == "hips":
                    pb.keyframe_insert("location", frame=frame, group=name)
        act.frame_range = (0, spec["frames"])
        meta.append(dict(name=clip, frames=spec["frames"], fps=FPS, loop=spec["loop"],
                         hitFrame=spec["hit"] if spec["hit"] is not None else -1))
    return meta


def export(arm, meta):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT_DIR, "Stickman.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=False, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, mesh_smooth_type="FACE")
    with open(os.path.join(OUT_DIR, "stickman_clips.json"), "w") as f:
        json.dump(dict(clips=meta), f, indent=2)
    print("exported", path)


def render_previews(arm):
    from PIL import Image
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 4
    scene.cycles.device = "CPU"
    scene.render.resolution_x = 220
    scene.render.resolution_y = 220
    world = bpy.data.worlds.new("Paper")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.62, 0.62, 1)
    scene.world = world
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.8
    cam = bpy.data.objects.new("Cam", cam_data)
    cam.location = (0, -6, 1.1)
    cam.rotation_euler = (math.radians(90), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    bpy.ops.mesh.primitive_plane_add(size=0.02, location=(0, 0, -5))  # keeps cycles happy
    tmp = os.path.join(PREVIEW_DIR, "_frame.png")
    for clip, spec in CLIPS.items():
        if clip not in bpy.data.actions or "keys" not in spec:
            continue
        if bpy.data.actions[clip].get("mocap"):
            continue
        arm.animation_data.action = bpy.data.actions[clip]
        frames = [k for k, _ in spec["keys"]]
        tiles = []
        for fr in frames:
            scene.frame_set(fr)
            scene.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            tiles.append(Image.open(tmp).convert("RGB").copy())
        sheet = Image.new("RGB", (220 * len(tiles), 220), "white")
        for i, t in enumerate(tiles):
            sheet.paste(t, (220 * i, 0))
        sheet.save(os.path.join(PREVIEW_DIR, clip + ".png"))
    os.remove(tmp)


def build_mocap_actions(arm):
    """Real motion: CMU motion capture retargeted onto the rig (see retarget.py)."""
    import retarget
    from mocap_clips import MOCAP_CLIPS
    cache = os.path.join(ROOT, "tools", "mocap", "cache")
    src = os.path.join(ROOT, "tools", "mocap", "bvh")  # only needed if a cache file is missing
    meta = []
    for name, spec in MOCAP_CLIPS.items():
        frames = retarget.prepare(spec, cache, src)
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        act["mocap"] = True
        arm.animation_data.action = act
        last = retarget.key_action(arm, frames, arm.pose.bones, FOOT_Z, BONES["hips"][1])
        act.frame_range = (0, last)
        hit = -1
        if spec.get("hit") is not None:
            hit = int(round((spec["hit"] - spec["start"]) / spec.get("speed", 1.0)))
            hit = retarget.impact_frame(frames, spec, hit)
        meta.append(dict(name=name, frames=last, fps=FPS, loop=bool(spec.get("loop")), hitFrame=hit,
                         source="CMU " + spec["file"]))
    return meta


def main():
    reset_scene()
    arm = build_armature()
    build_mesh(arm)
    sys.path.insert(0, os.path.dirname(__file__))
    use_mocap = "--no-mocap" not in sys.argv
    mocap_names = ()
    if use_mocap:
        from mocap_clips import MOCAP_CLIPS
        mocap_names = tuple(MOCAP_CLIPS)
    meta = build_actions(arm, skip=mocap_names)
    if use_mocap:
        meta += build_mocap_actions(arm)
    arm.animation_data.action = bpy.data.actions["idle"]
    export(arm, meta)
    if "--preview" in sys.argv:
        render_previews(arm)


if __name__ == "__main__":
    main()
