"""The V4 stickman (29 bones: docs/previews/skeleton_lab.png) with every clip.

    /home/user/.venv-blender/bin/python tools/blender/build_stickman_v4.py [--preview] [--only=a,b]

Writes
    Assets/Art/StickmanV4/StickmanV4.fbx           body + rig + every clip
    Assets/Art/StickmanV4/stickman_v4_clips.json    timing (hit, load, reach, endShift)
    docs/previews/v4/<clip>.png                     contact sheets (--preview)

How: every clip is first made as before on the V3 skeleton (the hand-keyed set, the mocap, the
V3 strikes), then carried onto the V4 skeleton frame by frame by v4_rig.transfer_frame, which fills
in the new bones (spine curve, neck, shoulders following the arms, rolling feet, fists or relaxed
hands). Clips written for V4 itself (v4_clips.CLIPS, keyed with every bone) replace them."""
import json
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as bs  # noqa: E402
import v3_rig as vr  # noqa: E402

vr.patch_bones()
import v3_clips as vc  # noqa: E402
import v4_rig as r4  # noqa: E402

try:
    import v4_clips as c4  # noqa: E402
    V4_CLIPS = c4.CLIPS
except ImportError:
    c4 = None
    V4_CLIPS = {}

ROOT = bs.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "StickmanV4")
PREV_DIR = os.path.join(ROOT, "docs", "previews", "v4")
RELAXED = set(bs.STORY_CLIPS) | set(getattr(bs, "HOME_CLIPS", {})) | set(getattr(bs, "V2_CLIPS", {})) | {"walk", "idle_loose"}


def source_actions(src):
    """Every clip on the V3 skeleton, as the V3 build makes them."""
    import mocap_clips
    for n in list(vc.CLIPS) + list(V4_CLIPS):
        mocap_clips.MOCAP_CLIPS.pop(n, None)
    skip = tuple(mocap_clips.MOCAP_CLIPS) + tuple(vc.CLIPS) + tuple(V4_CLIPS)
    meta = bs.build_actions(src, skip=skip)
    meta += bs.build_mocap_actions(src)
    for name, spec in vc.CLIPS.items():
        if name in V4_CLIPS:
            continue
        vr.key_clip(src, name, spec)
        meta.append(dict(name=name, frames=spec["frames"], fps=bs.FPS, loop=spec.get("loop", False),
                         hitFrame=spec["hit"] if spec.get("hit") is not None else -1,
                         loadFrame=spec.get("load") if spec.get("load") is not None else -1,
                         reach=spec.get("reach") or 0.0,
                         endShift=round(spec["keys"][-1][1].get("hx", 0.0), 4) if spec.get("shift") else 0.0))
    return meta


def transfer(src, dst, name, frames, fist):
    """Carry one clip from the V3 armature onto the V4 one, every frame."""
    sact = bpy.data.actions[name]
    sact.name = name + "__v3"
    src.animation_data.action = sact
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    dst.animation_data.action = act
    s_rest = {pb.name: pb.bone.matrix_local.to_quaternion() for pb in src.pose.bones}
    d_rest = {pb.name: pb.bone.matrix_local.to_quaternion() for pb in dst.pose.bones}
    hips_rest = Vector(r4.BONES["hips"][1])
    prev = {}
    lagged = {}
    scn = bpy.context.scene
    for f in range(frames + 1):
        scn.frame_set(f)
        qv3 = {pb.name: pb.matrix.to_quaternion() @ s_rest[pb.name].inverted() for pb in src.pose.bones}
        hips = src.pose.bones["hips"].head.copy()
        ankle = {s: src.pose.bones["shin." + s].tail.copy() for s in ("L", "R")}
        qw = r4.transfer_frame(qv3, hips, ankle, fist)
        # overlap on the carried-over clips too: the neck and head a beat behind the chest, the
        # wrists and fingers a beat behind the forearms (the parent's motion carries them, late)
        for n, k in (("neck", 0.3), ("head", 0.42), ("hand.L", 0.38), ("hand.R", 0.38), ("fingers.L", 0.45), ("fingers.R", 0.45)):
            par = r4.BONES[n][0]
            rel = qw[par].inverted() @ qw[n]
            if n in lagged:
                rel = rel.slerp(lagged[n], k) if f > 0 else rel
            lagged[n] = rel
            qw[n] = qw[par] @ rel
        loc = r4.to_local(qw, d_rest)
        for n in r4.ORDER:
            pb = dst.pose.bones[n]
            q = loc[n]
            if n in prev and prev[n].dot(q) < 0:
                q = -q
            prev[n] = q
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = q
            pb.keyframe_insert("rotation_quaternion", frame=f, group=n)
            if n == "hips":
                pb.location = d_rest["hips"].to_matrix().inverted() @ (hips - hips_rest)
                pb.keyframe_insert("location", frame=f, group=n)
    act.frame_range = (0, frames)
    return act


def export(dst, body, meta):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    dst.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = dst
    path = os.path.join(OUT_DIR, "StickmanV4.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, mesh_smooth_type="FACE")
    with open(os.path.join(OUT_DIR, "stickman_v4_clips.json"), "w") as f:
        json.dump(dict(clips=meta), f, indent=2)
    print("exported", path, len(meta), "clips")


def build(preview=False):
    bs.reset_scene()
    src = bs.build_armature()
    src.name = "V3src"
    src.animation_data_create()
    meta = source_actions(src)
    dst = r4.build_armature("Stickman")
    body = r4.build_mesh(dst, far_grey=0.10 if preview else None)
    dst.animation_data_create()
    n = 0
    for m in meta:
        name = m["name"]
        frames = int(m["frames"])
        transfer(src, dst, name, frames, name not in RELAXED)
        n += 1
    # clips written for V4 itself
    if c4 is not None:
        for name, spec in V4_CLIPS.items():
            c4.key_clip(dst, name, spec)
            meta.append(c4.meta(name, spec))
    # the source rig and its actions go: only the V4 one is exported
    for a in [a for a in bpy.data.actions if a.name.endswith("__v3")]:
        bpy.data.actions.remove(a)
    bpy.data.objects.remove(src, do_unlink=True)
    dst.animation_data.action = None
    bpy.context.scene.frame_set(0)
    for pb in dst.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    print("transferred", n, "clips")
    if not preview:
        export(dst, body, meta)
    return dst, body, meta


def render_previews(dst, meta, only):
    """Contact sheets of clips on the V4 body: 8 evenly spaced frames (and the hit frame)."""
    import strike_lab as sl
    from PIL import Image, ImageDraw, ImageFont
    cam = sl.setup()
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = 240, 260
    cam.data.ortho_scale = 2.1
    cam.location = (0.25, -8, 0.78)
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 15)
    big = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 20)
    os.makedirs(PREV_DIR, exist_ok=True)
    tmp = os.path.join(PREV_DIR, "_t.png")
    rows = []
    for m in meta:
        if only and m["name"] not in only:
            continue
        dst.animation_data.action = bpy.data.actions[m["name"]]
        n = int(m["frames"])
        frames = sorted({round(i * n / 7) for i in range(8)} | ({m["hitFrame"]} if m.get("hitFrame", -1) >= 0 else set()))
        tiles = []
        for f in frames:
            sc.frame_set(f)
            sc.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            img = Image.open(tmp).convert("RGB").copy()
            ImageDraw.Draw(img).text((5, 3), f"f{f}" + ("  HIT" if f == m.get("hitFrame") else ""), fill=(200, 20, 20) if f == m.get("hitFrame") else (40, 40, 40), font=font)
            tiles.append(img)
        row = Image.new("RGB", (240 * len(tiles), 260 + 28), (24, 24, 24))
        ImageDraw.Draw(row).text((8, 3), m["name"], fill=(255, 210, 0), font=big)
        for i, t in enumerate(tiles):
            row.paste(t, (i * 240, 28))
        row.save(os.path.join(PREV_DIR, m["name"] + ".png"))
        rows.append(row)
    if os.path.exists(tmp):
        os.remove(tmp)
    return rows


if __name__ == "__main__":
    if "--preview" in sys.argv:
        only = None
        for a in sys.argv:
            if a.startswith("--only="):
                only = set(a.split("=", 1)[1].split(","))
        d, _b, mt = build(preview=True)
        render_previews(d, mt, only)
    else:
        build()
