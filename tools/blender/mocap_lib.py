"""Read CMU motion-capture BVH files (Bruce Hahne's MotionBuilder-friendly conversion)
and return world-space joint positions per frame, Z up, in metres.

Used by retarget.py to drive the stickman rig with real martial-arts motion.
"""
import os

import bpy
import numpy as np

# CMU joint names used by the retargeter
JOINTS = ["Hips", "LowerBack", "Spine", "Spine1", "Neck", "Neck1", "Head",
          "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
          "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
          "LeftArm", "LeftForeArm", "LeftHand",
          "RightArm", "RightForeArm", "RightHand"]


def load_positions(path, step=4, cache_dir=None):
    """Joint head positions for every `step`-th frame (CMU is 120 fps, step 4 = 30 fps).
    Returns (dict joint -> float32 array [frames, 3], fps). Results are cached as .npz."""
    if cache_dir:
        os.makedirs(cache_dir, exist_ok=True)
        cache = os.path.join(cache_dir, os.path.basename(path) + f".s{step}.npz")
        if os.path.exists(cache):
            data = np.load(cache)
            return {k: data[k] for k in data.files if k != "_fps"}, float(data["_fps"])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_anim.bvh(filepath=path, global_scale=1.0, frame_start=0, use_fps_scale=False,
                            update_scene_fps=False, update_scene_duration=True,
                            rotate_mode="NATIVE", axis_forward="-Z", axis_up="Y")
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    scene = bpy.context.scene
    frame_time = 1.0 / 120.0
    with open(path) as f:
        for line in f:
            if line.startswith("Frame Time"):
                frame_time = float(line.split(":")[1])
                break
    end = scene.frame_end
    frames = list(range(0, end + 1, step))
    names = [j for j in JOINTS if j in arm.pose.bones]
    out = {j: np.zeros((len(frames), 3), np.float32) for j in names}
    mw = arm.matrix_world
    for i, fr in enumerate(frames):
        scene.frame_set(fr)
        for j in names:
            out[j][i] = mw @ arm.pose.bones[j].head
    # CMU units are inches-ish; normalise so the leg (hip -> ankle at rest, frame 0 T-pose) is 0.89 m
    leg = np.linalg.norm(out["LeftUpLeg"][0] - out["LeftLeg"][0]) + np.linalg.norm(out["LeftLeg"][0] - out["LeftFoot"][0])
    scale = 0.89 / leg
    for j in names:
        out[j] *= scale
    fps = 1.0 / (frame_time * step)
    if cache_dir:
        np.savez_compressed(cache, _fps=fps, **out)
    return out, fps
