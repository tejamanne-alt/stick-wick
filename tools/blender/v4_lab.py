"""Check the V4 clips before they go into the game.

    /home/user/.venv-blender/bin/python tools/blender/v4_lab.py --report [--only=a,b]
    /home/user/.venv-blender/bin/python tools/blender/v4_lab.py --preview [--only=a,b] [--every=2]

--report   for each blow: where the striking surface is at the hit frame against the target man's
           chin / body / thigh at the clip's reach, and the feet against the floor
--preview  docs/previews/v4/<clip>.png: every Nth frame (default: every 2nd, plus the hit), the hero
           in two greys (far limbs lighter), the target man reacting from the hit frame on;
           docs/previews/v4_moves.png: all of them"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_stickman as bs  # noqa: E402
import v4_rig as r4  # noqa: E402
import v4_pose as vp  # noqa: E402
import v4_clips as c4  # noqa: E402

ROOT = bs.ROOT
PREV = os.path.join(ROOT, "docs", "previews", "v4")
FONT = os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf")


def target_points(D):
    g = vp.solve(c4.GUARD)
    hc = vp.fk(g)["head"]
    hx = D - hc.x
    chin_z = 1.07
    chin_x = hx - math.sqrt(max(0.0, bs.HEAD_RADIUS ** 2 - (hc.z - chin_z) ** 2))
    return {"chin": (chin_x, chin_z), "under_chin": (hx - 0.1, hc.z - 0.19), "body": (D - 0.17, 0.84),
            "ribs": (D - 0.15, 0.92), "thigh": (D - 0.14, 0.44), "back": (D - 0.02, 0.86)}


def report(only):
    for name, spec in c4.CLIPS.items():
        if only and name not in only:
            continue
        fr = vp.frames(spec)
        low = min(min(vp.fk(p)["ball" + s].z, vp.fk(p)["ankle" + s].z) for p in fr for s in ("L", "R"))
        line = f"{name:16s} {spec['frames']:3d}f  lowest foot point {low:+.3f}"
        if spec.get("aim") and spec.get("hit") is not None:
            surf, point = spec["aim"]
            p = fr[spec["hit"]]
            c = vp.fk(p)[surf]
            tp = target_points(spec["reach"])[point]
            r = 0.07 if surf.startswith("fist") else 0.06
            line += f"  hit f{spec['hit']}: {surf} ({c.x:.2f},{c.z:.2f}) vs {point} ({tp[0]:.2f},{tp[1]:.2f}) dx {c.x + r - tp[0]:+.2f} dz {c.z - tp[1]:+.2f}"
        print(line)


def man(name, near, far, x=0.0, facing=1, blade=False):
    arm = r4.build_armature(name)
    r4.build_mesh(arm, near, far, name=name + "Body")
    if blade:
        # the katana, as the game mounts it: along the right hand bone (FighterFactory.Mount)
        bpy.ops.mesh.primitive_cube_add(size=1)
        b = bpy.context.active_object
        b.name = name + "Blade"
        b.scale = (0.012, 0.75, 0.03)
        b.parent = arm
        b.parent_type = "BONE"
        b.parent_bone = "hand.R"
        b.location = (0, 0.3, 0)
        b.data.materials.append(bpy.data.objects[name + "Body"].data.materials[0])
    arm.location = (x, 0, 0)
    arm.rotation_euler = (0, 0, 0 if facing > 0 else math.pi)
    return arm


def preview(only, every):
    import strike_lab as sl
    from PIL import Image, ImageDraw, ImageFont
    bs.reset_scene()
    cam = sl.setup()
    sc = bpy.context.scene
    W, H = 250, 270
    sc.render.resolution_x, sc.render.resolution_y = W, H
    cam.data.ortho_scale = 2.3
    cam.location = (0.5, -8, 0.8)
    hero = man("Hero", 0.02, 0.10, blade=True)
    tgt = man("Target", 0.55, 0.72, x=1.0, facing=-1)
    f1 = ImageFont.truetype(FONT, 15)
    f2 = ImageFont.truetype(FONT, 20)
    os.makedirs(PREV, exist_ok=True)
    tmp = os.path.join(PREV, "_t.png")
    rows = []
    guard = vp.solve(c4.GUARD)
    for name, spec in c4.CLIPS.items():
        if only and name not in only:
            continue
        fr = vp.frames(spec)
        n = spec["frames"]
        show = sorted(set(range(0, n + 1, every)) | {n} | ({spec["hit"]} if spec.get("hit") is not None else set()))
        D = spec.get("reach")
        vic = c4.CLIPS.get(spec.get("victim") or "")
        vfr = vp.frames(vic) if vic else None
        for o in (tgt, bpy.data.objects["TargetBody"]):
            o.hide_render = D is None and vfr is None
        tgt.location.x = spec.get("vat", 0.62) if vfr else (D or 1.0)
        tgt.rotation_euler = (0, 0, 0 if spec.get("vface", -1) > 0 else math.pi)
        wide = max(tgt.location.x, D or 0.0) > 1.6 or spec.get("shift")
        cam.data.ortho_scale = 4.0 if wide else 2.3
        cam.location = (1.3 if wide else 0.5, -8, 1.0 if wide else 0.8)
        react = c4.CLIPS.get(spec.get("react") or "")
        rfr = vp.frames(react) if react else None
        if name.endswith("_v") and name[:-2] in c4.CLIPS:
            continue                                   # drawn with its pair
        tiles = []
        for f in show:
            vp.apply(hero, fr[f])
            if vfr:
                vp.apply(tgt, vfr[min(len(vfr) - 1, f)])
            elif rfr and spec.get("hit") is not None and f >= spec["hit"]:
                vp.apply(tgt, rfr[min(len(rfr) - 1, f - spec["hit"])])
            else:
                vp.apply(tgt, guard)
            bpy.context.view_layer.update()
            sc.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            img = Image.open(tmp).convert("RGB").copy()
            hit = f == spec.get("hit")
            ImageDraw.Draw(img).text((5, 3), f"f{f}" + ("  IMPACT" if hit else ""), fill=(200, 20, 20) if hit else (40, 40, 40), font=f1)
            tiles.append(img)
        per = 10
        lines = [tiles[i:i + per] for i in range(0, len(tiles), per)]
        row = Image.new("RGB", (W * per, 30 + H * len(lines)), (24, 24, 24))
        ImageDraw.Draw(row).text((8, 4), f"{name}  ·  {n} frames  ·  {spec.get('note', '')}", fill=(255, 210, 0), font=f2)
        for li, line in enumerate(lines):
            for i, t in enumerate(line):
                row.paste(t, (i * W, 30 + li * H))
        row.save(os.path.join(PREV, name + ".png"))
        rows.append(row)
        print("preview", name)
    if os.path.exists(tmp):
        os.remove(tmp)
    if rows and not only:
        sheet = Image.new("RGB", (max(r.width for r in rows), sum(r.height for r in rows)), (24, 24, 24))
        y = 0
        for r in rows:
            sheet.paste(r, (0, y))
            y += r.height
        sheet.save(os.path.join(ROOT, "docs", "previews", "v4_moves.png"))


if __name__ == "__main__":
    only = None
    every = 2
    for a in sys.argv:
        if a.startswith("--only="):
            only = set(a.split("=", 1)[1].split(","))
        if a.startswith("--every="):
            every = int(a.split("=", 1)[1])
    if "--report" in sys.argv:
        report(only)
    else:
        preview(only, every)
