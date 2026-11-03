"""Render docs/previews/moves_overview.png: every motion-captured move at its key moment,
straight from the exported FBX.  python tools/blender/render_overview.py"""
import json
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import build_stickman as bs  # noqa: E402

SHOW = [("idle", 0), ("jab", None), ("cross", None), ("knee", None), ("front_kick", None),
        ("roundhouse", None), ("back_kick", None), ("flying_kick", None), ("spin_kick", None),
        ("dodge", 12), ("knockdown", 41)]


def main():
    from PIL import Image, ImageDraw
    meta = {c["name"]: c for c in json.load(open(os.path.join(bs.OUT_DIR, "stickman_clips.json")))["clips"]}
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(bs.OUT_DIR, "Stickman.fbx"))
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    body = next(o for o in bpy.context.scene.objects if o.type == "MESH")
    mat = bpy.data.materials.new("Ink")
    mat.diffuse_color = (0.01, 0.01, 0.01, 1)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.01, 0.01, 0.01, 1)
    body.data.materials.clear()
    body.data.materials.append(mat)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 4
    scene.render.resolution_x, scene.render.resolution_y = 240, 260
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.62, 0.62, 1)
    scene.world = world
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.9
    cam = bpy.data.objects.new("Cam", cam_data)
    # FBX import turns the rig; look along whichever horizontal axis is side-on
    cam.location = (0.2, -6, 1.15)
    cam.rotation_euler = (math.radians(90), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    tiles = []
    tmp = os.path.join(bs.PREVIEW_DIR, "_frame.png")
    for name, frame in SHOW:
        act = next(a for a in bpy.data.actions if a.name.split("|")[-1] == name)
        arm.animation_data.action = act
        if frame is None:
            frame = meta[name]["hitFrame"]
        start = int(act.frame_range[0])
        scene.frame_set(start + frame)
        scene.render.filepath = tmp
        bpy.ops.render.render(write_still=True)
        tiles.append((name, Image.open(tmp).convert("RGB").copy()))
    cols = 6
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (240 * cols, 260 * rows), (158, 158, 158))
    d = ImageDraw.Draw(sheet)
    for i, (name, t) in enumerate(tiles):
        x, y = (i % cols) * 240, (i // cols) * 260
        sheet.paste(t, (x, y))
        d.text((x + 8, y + 6), name.replace("_", " "), fill=(255, 255, 255))
    sheet.save(os.path.join(bs.PREVIEW_DIR, "moves_overview.png"))
    os.remove(tmp)


if __name__ == "__main__":
    main()
