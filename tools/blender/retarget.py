"""Retarget CMU motion capture onto the stickman rig built by build_stickman.py.

For each clip spec we take a time window of a CMU recording (joint positions from
mocap_lib), turn it to face +X (or aim the strike backwards), optionally mirror it,
optionally speed it up, and key the stickman's 12 bones:

* hips and chest take a full orientation from the actor (spine direction + hip/shoulder
  line), so torso twist and turns come through;
* every other bone swings from its parent to point where the actor's limb points;
* the hips height comes from the actor, relative to the actor's ankle height, so the
  feet stay on the ground; horizontal travel is removed (the game moves the fighter).
"""
import math

import numpy as np
from mathutils import Matrix, Quaternion, Vector

import mocap_lib

# stickman bone -> (start joint, end joint) in the CMU skeleton
SWING = {
    "spine": ("Spine", "Spine1"),
    "head": ("Neck", "Head"),
    "upperarm.L": ("LeftArm", "LeftForeArm"),
    "forearm.L": ("LeftForeArm", "LeftHand"),
    "upperarm.R": ("RightArm", "RightForeArm"),
    "forearm.R": ("RightForeArm", "RightHand"),
    "thigh.L": ("LeftUpLeg", "LeftLeg"),
    "shin.L": ("LeftLeg", "LeftFoot"),
    "thigh.R": ("RightUpLeg", "RightLeg"),
    "shin.R": ("RightLeg", "RightFoot"),
}
ORDER = ["hips", "spine", "chest", "head", "upperarm.L", "forearm.L", "upperarm.R", "forearm.R",
         "thigh.L", "shin.L", "thigh.R", "shin.R"]


def _interp(pos, t):
    """Joint positions at fractional frame t."""
    i0 = int(math.floor(t))
    i1 = min(i0 + 1, len(next(iter(pos.values()))) - 1)
    a = t - i0
    return {k: v[i0] * (1 - a) + v[i1] * a for k, v in pos.items()}


def _mirror(frame):
    """Swap left and right and reflect across the character's sagittal plane (y -> -y)."""
    out = {}
    for k, v in frame.items():
        k2 = k.replace("Left", "#").replace("Right", "Left").replace("#", "Right")
        out[k2] = np.array([v[0], -v[1], v[2]], np.float32)
    return out


def _frame_matrix(up, left):
    """Bone-space frame: Y along `up`, Z towards `left` (the rig's rest roll), X = Y x Z."""
    y = Vector(up).normalized()
    z = Vector(left)
    z = (z - y * z.dot(y)).normalized()
    x = y.cross(z).normalized()
    return Matrix((x, y, z)).transposed()


def _yaw_to(src_dir, dst_dir):
    a = math.atan2(src_dir[1], src_dir[0])
    b = math.atan2(dst_dir[1], dst_dir[0])
    d = b - a
    c, s = math.cos(d), math.sin(d)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], np.float32)


def prepare(spec, cache_dir, src_dir):
    """Return a list of per-frame joint dicts, normalised for the stickman."""
    pos, fps = mocap_lib.load_positions(f"{src_dir}/{spec['file']}.bvh", 4, cache_dir)
    start, end, speed = spec["start"], spec["end"], spec.get("speed", 1.0)
    times = list(np.arange(start, end + 1e-6, speed))
    frames = [_interp(pos, t) for t in times]
    if spec.get("mirror"):
        frames = [_mirror(f) for f in frames]

    # facing: pelvis forward at the reference frame -> +X, or the striking foot -> -X
    ref = frames[min(len(frames) - 1, int(round((spec["hit"] - start) / speed)))] if spec.get("hit") is not None else frames[0]
    if spec.get("aim") in ("joint_forward", "joint_back"):
        j = spec["joint"]
        base = (ref["LeftArm"] + ref["RightArm"]) * 0.5 if "Hand" in j else ref["Hips"]
        d = ref[j] - base
        rot = _yaw_to(d, (1.0, 0.0) if spec["aim"] == "joint_forward" else (-1.0, 0.0))
    elif spec.get("aim") == "foot_back":
        foot = spec["foot"]
        d = ref[foot] - ref["Hips"]
        rot = _yaw_to(d, (-1.0, 0.0))
    elif spec.get("aim") == "foot_forward":
        foot = spec["foot"]
        d = ref[foot] - ref["Hips"]
        rot = _yaw_to(d, (1.0, 0.0))
    else:
        f0 = frames[0]
        left = f0["LeftUpLeg"] - f0["RightUpLeg"]
        fwd = np.cross(left, np.array([0, 0, 1], np.float32))
        rot = _yaw_to(fwd, (1.0, 0.0))
    origin = frames[0]["Hips"].copy()
    origin[2] = 0.0
    out = []
    for f in frames:
        out.append({k: rot @ (v - origin) for k, v in f.items()})

    # punches: straighten the striking arm into full, level extension around the impact,
    # so it reads from the side (shadow-boxing punches are short and pulled)
    if spec.get("extend"):
        side = "Left" if spec["extend"].startswith("Left") else "Right"
        hit_i = int(round((spec["hit"] - start) / speed))
        for i, f in enumerate(out):
            w = max(0.0, 1.0 - abs(i - hit_i) / 4.0)
            if w <= 0:
                continue
            w = w * w * (3 - 2 * w)
            sh, el, ha = f[side + "Arm"], f[side + "ForeArm"], f[side + "Hand"]
            lu, lf = np.linalg.norm(el - sh), np.linalg.norm(ha - el)
            cur = (ha - sh) / (np.linalg.norm(ha - sh) + 1e-6)
            level = np.array([1.0, cur[1] * 0.3, 0.05], np.float32)
            level /= np.linalg.norm(level)
            d = cur * (1 - w) + level * w
            d /= np.linalg.norm(d)
            straight_el = sh + d * lu
            f[side + "ForeArm"] = el * (1 - w) + straight_el * w
            f[side + "Hand"] = f[side + "ForeArm"] + d * lf

    # loops: ease the last frames back onto the first so the cycle has no seam
    if spec.get("loop"):
        k = max(2, min(8, len(out) // 3))
        first = out[0]
        for i in range(len(out) - k, len(out)):
            w = (i - (len(out) - k) + 1) / k
            for j in list(first):
                out[i][j] = out[i][j] * (1 - w) + first[j] * w

    # ground: the actor's lowest ankle over the window (or the given value)
    ground = spec.get("ground")
    if ground is None:
        ground = min(min(f["LeftFoot"][2], f["RightFoot"][2]) for f in out)
    keep_travel = spec.get("travel", 0.0)  # 0 = in place, 1 = keep the actor's horizontal travel
    hip0 = (out[0]["LeftUpLeg"] + out[0]["RightUpLeg"]) * 0.5
    for f in out:
        mid = (f["LeftUpLeg"] + f["RightUpLeg"]) * 0.5
        f["_root"] = np.array([(mid[0] - hip0[0]) * keep_travel, 0.0, mid[2] - ground], np.float32)
    return out


def impact_frame(frames, spec, nominal, window=5):
    """The frame of greatest reach of the striking limb near the nominal hit frame."""
    joint = spec.get("joint") or spec.get("extend")
    if joint is None:
        joint = "RightFoot" if "kick" in spec.get("file", "") else None
    if joint is None:
        return nominal
    best, best_d = nominal, -1.0
    for i in range(max(0, nominal - window), min(len(frames), nominal + window + 1)):
        f = frames[i]
        base = (f["LeftArm"] + f["RightArm"]) * 0.5 if "Hand" in joint else f["Hips"]
        d = abs(float(f[joint][0] - base[0]))
        if d > best_d:
            best, best_d = i, d
    return best


def key_action(arm, frames, bones, foot_z, hip_rest):
    """Key one action on `arm` from prepared frames."""
    rest = {n: arm.data.bones[n].matrix_local.to_3x3() for n in ORDER}
    parent = {n: (arm.data.bones[n].parent.name if arm.data.bones[n].parent else None) for n in ORDER}
    for i, f in enumerate(frames):
        world = {}
        mid = (f["LeftUpLeg"] + f["RightUpLeg"]) * 0.5
        world["hips"] = _frame_matrix(f["Spine"] - mid, f["LeftUpLeg"] - f["RightUpLeg"])
        for n in ORDER[1:]:
            p = parent[n]
            unswung = world[p] @ (rest[p].inverted() @ rest[n])
            if n == "chest":
                # CMU's Neck often sits on Spine1; use the first joint above it that is distinct
                top = next((f[j] for j in ("Neck", "Neck1", "Head") if np.linalg.norm(f[j] - f["Spine1"]) > 0.03), f["Head"])
                world[n] = _frame_matrix(top - f["Spine1"], f["LeftArm"] - f["RightArm"])
                continue
            a, b = SWING[n]
            target = Vector(f[b] - f[a])
            if target.length < 1e-5:
                world[n] = unswung
                continue
            cur = unswung.col[1]
            q = cur.rotation_difference(target.normalized())
            world[n] = q.to_matrix() @ unswung
        for n in ORDER:
            pb = bones[n]
            p = parent[n]
            if p is None:
                basis = rest[n].inverted() @ world[n]
            else:
                basis = (rest[n].inverted() @ rest[p]) @ world[p].inverted() @ world[n]
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = basis.to_quaternion()
            pb.keyframe_insert("rotation_quaternion", frame=i, group=n)
        # hips: actor's pelvis height above its ankles -> stickman pelvis above its ankles
        root = f["_root"]
        target = Vector((root[0], 0.0, root[2] + foot_z))
        pb = bones["hips"]
        pb.location = rest["hips"].inverted() @ (target - Vector(hip_rest))
        pb.keyframe_insert("location", frame=i, group="hips")
    return len(frames) - 1
