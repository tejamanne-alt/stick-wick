"""Combat 3.0's body and moves: the V3 stickman (docs/design/combat_v3.md, step 1).

    /home/user/.venv-blender/bin/python tools/blender/build_stickman_v3.py [--preview] [--only=jab,cross] [--report]

Writes
    Assets/Art/StickmanV3/StickmanV3.fbx            the V3 body + rig + every clip
    Assets/Art/StickmanV3/stickman_v3_clips.json     timing (hit and load frames)
    docs/previews/v3/<clip>.png                      contact sheets with a target man (--preview)
    docs/previews/v3_moves.png                       all of them on one sheet (--preview)

Every clip of build_stickman.py is carried over onto the new skeleton (the story's clips, the
mocap, the throws), so the same game logic runs on this body. The clips in v3_clips.CLIPS replace
the old ones of the same name (the strikes, the guard, the reactions) and add the new ones (the
guard, the evasions, the counters, the pull-and-release power shot)."""
import json
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as bs  # noqa: E402
import v3_rig as vr  # noqa: E402

vr.patch_bones()
import v3_clips as vc  # noqa: E402

ROOT = bs.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "StickmanV3")
PREV_DIR = os.path.join(ROOT, "docs", "previews", "v3")


def export(arm, meta):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT_DIR, "StickmanV3.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=False, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, mesh_smooth_type="FACE")
    with open(os.path.join(OUT_DIR, "stickman_v3_clips.json"), "w") as f:
        json.dump(dict(clips=meta), f, indent=2)
    print("exported", path, len(meta), "clips")


def build():
    bs.reset_scene()
    arm = bs.build_armature()
    vr.build_mesh_v3(arm)
    import mocap_clips
    for n in vc.CLIPS:
        mocap_clips.MOCAP_CLIPS.pop(n, None)
    meta = bs.build_actions(arm, skip=tuple(mocap_clips.MOCAP_CLIPS) + tuple(vc.CLIPS))
    meta += bs.build_mocap_actions(arm)
    for name, spec in vc.CLIPS.items():
        vr.key_clip(arm, name, spec)
        meta.append(dict(name=name, frames=spec["frames"], fps=bs.FPS, loop=spec.get("loop", False),
                         hitFrame=spec["hit"] if spec.get("hit") is not None else -1,
                         loadFrame=spec.get("load") if spec.get("load") is not None else -1, v3=True,
                         reach=spec.get("reach") or 0.0,
                         endShift=round(spec["keys"][-1][1].get("hx", 0.0), 4) if spec.get("shift") else 0.0))
    arm.animation_data.action = None
    bpy.context.scene.frame_set(0)
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    export(arm, meta)


# ---------------------------------------------------------------- the aim report
def target_points(D):
    """Where a man in the guard, D metres away and facing back, can be hit (clip space)."""
    r = vr.rig()
    p = vr.resolve(vc.ST)
    pos, tip = r.fk(p)
    hc = (pos["head"] + (tip["head"] - pos["head"]).normalized() * (bs.HEAD_CENTER - 1.05))
    hcx = D - hc.x
    chin_z = 1.07
    chin_x = hcx - math.sqrt(max(0.0, bs.HEAD_RADIUS ** 2 - (hc.z - chin_z) ** 2))
    return {"head_c": (hcx, hc.z), "chin": (chin_x, chin_z),
            "under_chin": (hcx - 0.1, hc.z - 0.19),
            "body": (D - 0.17, 0.84), "back": (D - 0.02, 0.86), "ribs": (D - 0.15, 0.92), "thigh": (D - 0.14, 0.44)}


def report(only=None):
    r = vr.rig()
    for name, spec in vc.CLIPS.items():
        if only and name not in only:
            continue
        aim = spec.get("aim")
        if not aim or spec.get("hit") is None:
            continue
        keys = [(f, vr.resolve(p), e) for f, p, e in spec["keys"]]
        p = vr.sample(keys, spec["hit"])
        pos, tip = r.fk(p)
        bone, point = aim
        tp = target_points(spec["reach"])[point]
        if bone.startswith("hand"):
            c = (pos[bone] + tip[bone]) / 2
            surf = vr.FIST_R
        elif bone.startswith("upperarm"):
            c = tip[bone]
            surf = 0.058
        else:
            c = pos[bone] + (tip[bone] - pos[bone]) * 0.7
            surf = 0.066
        dx, dz = c.x + surf - tp[0], c.z - tp[1]
        gnd = vr.ground_check(p)
        print(f"{name:16s} hit f{spec['hit']:2d}  {bone:10s} at ({c.x:5.2f},{c.y:5.2f},{c.z:5.2f})  "
              f"target {point} ({tp[0]:.2f},{tp[1]:.2f})  dx {dx:+.2f} dz {dz:+.2f}  feet {gnd:+.3f}")


# ---------------------------------------------------------------- previews
def preview(only=None):
    import strike_lab as sl
    from PIL import Image, ImageDraw, ImageFont
    bs.reset_scene()
    cam = sl.setup()
    bpy.context.scene.render.resolution_x = 260
    bpy.context.scene.render.resolution_y = 260
    cam.data.ortho_scale = 2.4
    cam.location = (0.5, -8, 0.8)
    hero = sl.rig("Hero", 0.02, 0.10)
    tgt = sl.rig("Target", 0.55, 0.72, x=1.0, facing=-1)
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 15)
    big = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf"), 22)
    os.makedirs(PREV_DIR, exist_ok=True)
    tmp = os.path.join(PREV_DIR, "_t.png")
    rows = []
    for name, spec in vc.CLIPS.items():
        if only and name not in only:
            continue
        keys = [(f, vr.resolve(p), e) for f, p, e in spec["keys"]]
        frames = spec.get("show") or sorted({k[0] for k in keys} | ({spec["hit"]} if spec.get("hit") is not None else set()))
        D = spec.get("reach")
        tgt.hide_render = D is None and not spec.get("victim")
        tgt.location.x = D if D else spec.get("victim_at", 1.0)
        react = vc.CLIPS.get(spec.get("react", ""))
        rkeys = [(f, vr.resolve(p), e) for f, p, e in react["keys"]] if react else None
        vkeys = [(f, vr.resolve(p), e) for f, p, e in vc.CLIPS[spec["victim"]]["keys"]] if spec.get("victim") else None
        tiles = []
        for fr in frames:
            vr.apply_sampled(hero, vr.sample(keys, fr))
            if vkeys:
                vr.apply_sampled(tgt, vr.sample(vkeys, fr))
            elif rkeys and spec.get("hit") is not None and fr >= spec["hit"]:
                vr.apply_sampled(tgt, vr.sample(rkeys, fr - spec["hit"]))
            else:
                vr.apply_sampled(tgt, vr.sample([(0, vr.resolve(vc.ST), "lin")], 0))
            bpy.context.view_layer.update()
            bpy.context.scene.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            img = Image.open(tmp).convert("RGB").copy()
            d = ImageDraw.Draw(img)
            hit = spec.get("hit") == fr
            d.text((6, 4), f"f{fr}" + ("  IMPACT" if hit else "") + (f"  load" if spec.get("load") == fr else ""),
                   fill=(200, 20, 20) if hit else (40, 40, 40), font=font)
            tiles.append(img)
        W = 260
        row = Image.new("RGB", (W * max(1, len(tiles)), 260 + 30), (24, 24, 24))
        ImageDraw.Draw(row).text((8, 4), f"{name}  ·  {spec['frames']} frames at 30 fps  ·  {spec.get('note', '')}", fill=(255, 210, 0), font=big)
        for i, t in enumerate(tiles):
            row.paste(t, (i * W, 30))
        row.save(os.path.join(PREV_DIR, name + ".png"))
        rows.append(row)
        print("preview", name)
    if os.path.exists(tmp):
        os.remove(tmp)
    if rows and not only:
        wmax = max(r.width for r in rows)
        sheet = Image.new("RGB", (wmax, sum(r.height for r in rows)), (24, 24, 24))
        y = 0
        for r in rows:
            sheet.paste(r, (0, y))
            y += r.height
        sheet.save(os.path.join(ROOT, "docs", "previews", "v3_moves.png"))


def main():
    only = None
    for a in sys.argv:
        if a.startswith("--only="):
            only = set(a.split("=", 1)[1].split(","))
    if "--report" in sys.argv:
        report(only)
        return
    if "--preview" in sys.argv:
        preview(only)
        return
    build()


if __name__ == "__main__":
    main()
