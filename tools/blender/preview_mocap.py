"""Render contact sheets of the retargeted mocap clips (for choosing windows).
    python tools/blender/preview_mocap.py <cmu_bvh_dir> [clip ...]
Sheets go to docs/previews/mocap_<clip>.png."""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import build_stickman as bs  # noqa: E402
import retarget  # noqa: E402
from mocap_clips import MOCAP_CLIPS  # noqa: E402

ROOT = bs.ROOT
CACHE = os.path.join(ROOT, "tools", "mocap", "cache")


def main():
    src = sys.argv[1]
    names = sys.argv[2:] or list(MOCAP_CLIPS)
    extra = {}
    if os.environ.get("CANDIDATES"):
        import json
        extra = json.loads(os.environ["CANDIDATES"])
        names = list(extra)
    clips = dict(MOCAP_CLIPS, **extra)
    from PIL import Image, ImageDraw
    for name in names:
        spec = clips[name]
        frames = retarget.prepare(spec, CACHE, src)
        bs.reset_scene()
        arm = bs.build_armature()
        bs.build_mesh(arm)
        if spec.get("prop"):
            bs.add_prop(arm, spec["prop"])
        arm.animation_data_create()
        act = bpy.data.actions.new(name)
        arm.animation_data.action = act
        last = retarget.key_action(arm, frames, arm.pose.bones, bs.FOOT_Z, bs.BONES["hips"][1])
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 2
        scene.render.resolution_x = scene.render.resolution_y = 200
        world = bpy.data.worlds.new("W")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.62, 0.62, 1)
        scene.world = world
        cam_data = bpy.data.cameras.new("Cam")
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = 3.4
        cam = bpy.data.objects.new("Cam", cam_data)
        cam.location = (0.3, -6, 1.2)
        cam.rotation_euler = (math.radians(90), 0, 0)
        scene.collection.objects.link(cam)
        scene.camera = cam
        n = 10
        picks = sorted(set(int(round(i * last / (n - 1))) for i in range(n)))
        tiles = []
        tmp = os.path.join(bs.PREVIEW_DIR, "_frame.png")
        for fr in picks:
            scene.frame_set(fr)
            scene.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            tiles.append((fr, Image.open(tmp).convert("RGB").copy()))
        sheet = Image.new("RGB", (200 * len(tiles), 200), "white")
        d = ImageDraw.Draw(sheet)
        for i, (fr, t) in enumerate(tiles):
            sheet.paste(t, (200 * i, 0))
            d.text((200 * i + 6, 4), f"{name} {fr}", fill=(150, 0, 0))
        sheet.save(os.path.join(bs.PREVIEW_DIR, f"mocap_{name}.png"))
        os.remove(tmp)
        print("preview", name, len(frames), "frames")


if __name__ == "__main__":
    main()
