"""The hero's walk from a real person walking (CMU motion capture, subject 16, recording 15: an ordinary,
unhurried walk), not keyed by hand.

Reviewed: the hand-keyed walks (the gait engine) never read as a person; the hero's own (walk_alone) was the
complaint every time he walked with nobody near. So the walk is taken from life:

  · the three clean cycles of the recording (left heel strike to left heel strike) are averaged into one,
    which removes the capture's jitter and gives a loop with no seam;
  · it's scaled to the stickman: lengths by his leg (hip to ankle, 0.64 m against the actor's 0.89) and time
    by the square root of that (dynamic similarity: a shorter body swings its legs faster), so it keeps the
    actor's rhythm, weight and arm swing at the stickman's size;
  · the feet are the actor's own paths (heel strike with the toes up, the roll, the heel lifting, the toe-off,
    the swing), held out of the floor and set down on it exactly while they carry weight; the hips keep the
    actor's rise and fall and the small surge and lag inside each step, relative to a root that moves at the
    constant average speed, so with the game carrying him at that speed the planted foot doesn't slide;
  · the trunk, neck and head keep the actor's lean and its small pitch; the pelvis and shoulders their counter-
    rotation; the arms their swing (further forward than back, the elbow bending as it comes forward); the
    hands relaxed and half open.

walk_speed: its ground speed (m/s), which the game must carry him at (HeroMovesV4.WalkSpeed)."""
import math
import os

import numpy as np

import build_stickman as bs
import v4_pose as vp
from v4_pose import P

ROOT = bs.ROOT
CACHE = os.path.join(ROOT, "tools", "mocap", "cache")
LEG = bs.THIGH + bs.SHIN                       # the stickman's hip to ankle (0.64)
ACTOR_LEG = 0.89                               # mocap_lib normalises every actor's leg to this

TAU = 2 * math.pi


def _load(name):
    d = np.load(os.path.join(CACHE, name + ".bvh.s4.npz"))
    return {k: d[k].astype(np.float64) for k in d.files if k != "_fps"}, float(d["_fps"])


def _strikes(pos, fps, side="Left"):
    """Frames where the foot sets down (its ankle stops moving over the ground)."""
    a = pos[side + "Foot"]
    v = np.r_[0.0, np.linalg.norm(np.diff(a[:, :2], axis=0), axis=1) * fps]
    still = v < 0.25
    return [i for i in range(2, len(a)) if still[i] and not still[i - 1] and not still[i - 2]]


def _cycle_samples(pos, a, b, m):
    """One cycle (frames a..b) resampled to m samples, in the cycle's own frame: x forward along the average
    travel, measured from a root moving at constant speed; y to the left; z up. Returns dict joint -> [m, 3],
    and the cycle's travel in metres."""
    hip = (pos["LeftUpLeg"] + pos["RightUpLeg"]) * 0.5
    d = hip[b, :2] - hip[a, :2]
    travel = float(np.linalg.norm(d))
    fwd = d / travel
    left = np.array([-fwd[1], fwd[0]])
    out = {}
    ts = np.linspace(a, b, m, endpoint=False)
    for j, arr in pos.items():
        s = np.zeros((m, 3))
        for i, t in enumerate(ts):
            i0 = int(math.floor(t)); u = t - i0
            p = arr[i0] * (1 - u) + arr[min(i0 + 1, len(arr) - 1)] * u
            rootxy = hip[a, :2] + d * (t - a) / (b - a)
            rel = p[:2] - rootxy
            s[i] = (rel @ fwd, rel @ left, p[2])
        out[j] = s
    return out, travel


def _side_angle(v):
    """The absolute side-plane angle of a vector (x forward, z up): 0 down, 90 forward, 180 up."""
    return math.degrees(math.atan2(v[0], -v[2])) % 360.0


def _foot_points(pitch):
    """The lowest points of the stickman's foot (heel sphere, the ball, the toe tip, radii included) relative
    to the ankle, for a foot pitched `pitch` degrees (+ toes down)."""
    base = vp.BASE
    s = base + math.radians(pitch)
    ball = (vp.FOOT_LEN * math.cos(s), -vp.FOOT_LEN * math.sin(s))
    toe = (ball[0] + 0.05 * math.cos(math.radians(pitch * 0.6)), ball[1] - 0.05 * math.sin(math.radians(pitch * 0.6)))
    r = math.radians(pitch)
    hx, hz = -0.022, -0.016                                     # the heel sphere's centre, rotated with the foot
    heel = (hx * math.cos(r) + hz * math.sin(r), -hx * math.sin(r) + hz * math.cos(r))
    return [(heel[0], heel[1] - 0.043), (ball[0], ball[1] - 0.038), (toe[0], toe[1] - 0.035)]


def _contact_z(pitch):
    """The ankle height at which this foot's lowest point just touches the floor."""
    return -min(p[1] for p in _foot_points(pitch))


def walk_from(name, n, cycles=None, lean=0.0, arm_gain=1.0, fist=55.0, time_scale=None, elbow_max=45.0):
    """Keys for a looping walk of n frames from recording `name`, and its ground speed."""
    pos, fps = _load(name)
    st = _strikes(pos, fps)
    pairs = [(st[i], st[i + 1]) for i in range(len(st) - 1)]
    if cycles:
        pairs = pairs[:cycles]
    m = 120
    acc = None
    travel = 0.0
    dur = 0.0
    for a, b in pairs:
        c, t = _cycle_samples(pos, a, b, m)
        acc = c if acc is None else {j: acc[j] + c[j] for j in acc}
        travel += t
        dur += (b - a) / fps
    k = len(pairs)
    avg = {j: v / k for j, v in acc.items()}
    travel /= k
    dur /= k
    s = LEG / ACTOR_LEG
    ts = math.sqrt(s) if time_scale is None else time_scale        # the stickman's cycle = the actor's x ts
    cycle_t = dur * ts
    speed = travel * s / cycle_t

    # the actor's floor: his ankle height while the foot is flat and carrying weight
    ank = np.r_[avg["LeftFoot"][:, 2], avg["RightFoot"][:, 2]]
    floor_ankle = float(np.percentile(ank, 8))

    def at(arr, u):
        x = u * m
        i0 = int(math.floor(x)) % m
        w = x - math.floor(x)
        return arr[i0] * (1 - w) + arr[(i0 + 1) % m] * w

    # each foot's speed over the ground (actor units, m/s): its speed in the cycle's frame plus the root's. A foot
    # carries weight only while it is (nearly) still on the ground; one swinging low just before it lands is not
    # set down on the floor (it would drag its heel), it keeps a small clearance that fades in as it leaves
    v_root = travel / dur
    ground_v = {}
    for side in ("Left", "Right"):
        x = avg[side + "Foot"][:, 0]
        dx = (np.roll(x, -1) - np.roll(x, 1)) * 0.5 * (m / dur)
        ground_v[side] = np.abs(dx + v_root)

    def side_ang(arr_a, arr_b):
        v = arr_b - arr_a
        return np.degrees(np.arctan2(v[:, 0], -v[:, 2])) % 360.0
    trunk_mean = float(np.mean(side_ang(avg["Hips"], avg["Neck"])))
    head_mean = float(np.mean(side_ang(avg["Neck"], avg["Head"])))
    arm_mean = {sk: float(np.mean(((side_ang(avg[sd + "Arm"], avg[sd + "ForeArm"]) + 180) % 360) - 180))
                for sd, sk in (("Left", "L"), ("Right", "R"))}

    keys = []
    for f in range(n + 1):
        u = (f % n) / n
        J = {j: at(v, u) for j, v in avg.items()}
        hipL, hipR = J["LeftUpLeg"], J["RightUpLeg"]
        hipc = (hipL + hipR) * 0.5
        p = {}
        p["hx"] = hipc[0] * s
        p["hz"] = vp.ANKLE_Z + (hipc[2] - floor_ankle) * s
        for side, sk in (("Left", "L"), ("Right", "R")):
            an, ball = J[side + "Foot"], J[side + "ToeBase"]
            fv = ball - an
            sl = math.atan2(-fv[2], fv[0])                                   # the foot's slope, heel to ball
            pitch = math.degrees(sl - vp.BASE)
            ax = an[0] * s
            az = vp.ANKLE_Z + (an[2] - floor_ankle) * s
            cz = _contact_z(pitch)
            gv = float(at(ground_v[side], u))
            swing = min(1.0, max(0.0, (gv - 0.25) / 0.45))                  # 0 still on the ground .. 1 swinging
            swing = swing * swing * (3 - 2 * swing)
            if az - cz < 0.012 and swing < 0.5:                              # carrying weight: on the floor exactly
                az = cz
            p["f" + sk] = (ax, max(az, cz + 0.012 * swing), pitch)
        # the trunk: his standing posture (LOOSE) leaned a degree into the walk, with the actor's own pitch through
        # each step (the trunk from the pelvis to the base of the neck, about its mean) spread up the spine; the head
        # steadier still (the eyes stay level)
        def ang(a, b):
            v = J[b] - J[a]
            return _side_angle((v[0], v[1], v[2]))
        dev = ang("Hips", "Neck") - trunk_mean
        hdev = (ang("Neck", "Head") - head_mean) * 0.5
        p["hips"] = 180.0
        p["spine"] = 177.0 + lean + dev
        p["spine_mid"] = 176.0 + lean + dev
        p["chest"] = 175.0 + lean + dev
        p["neck"] = 174.0 + dev * 0.5
        p["head"] = 178.0 + hdev
        # the counter-rotation of the pelvis and the shoulders (+ brings the near, right side forward)
        def yaw(lj, rj):
            v = J[lj] - J[rj]
            return -math.degrees(math.atan2(v[0], v[1]))
        p["tw_hips"] = yaw("LeftUpLeg", "RightUpLeg")
        p["tw_chest"] = yaw("LeftArm", "RightArm") - p["tw_hips"] * 0.5
        p["tw_spine"] = p["tw_hips"] * 0.5
        # the arms: the actor's swing (about the arm's mean, scaled by arm_gain), the elbow bending forward
        for side, sk in (("Left", "L"), ("Right", "R")):
            ua = ang(side + "Arm", side + "ForeArm")
            fa = ang(side + "ForeArm", side + "Hand")
            ua = ua if ua < 180 else ua - 360
            fa = fa if fa < 180 else fa - 360
            # the swing about the arm's mean, a little fuller (arm_gain) so it reads at the stickman's size; the
            # elbow's own bend kept as the actor had it
            p["ua" + sk] = arm_mean[sk] + (ua - arm_mean[sk]) * arm_gain
            p["fa" + sk] = p["ua" + sk] + max(-5.0, min(elbow_max, fa - ua))     # (a relaxed elbow: never a reach)
            p["ha" + sk] = p["fa" + sk] - 6                                  # the wrist a touch flexed, relaxed
            p["fi" + sk] = fist
        keys.append((f, p, "lin"))
    # the loop: the last frame is the first
    keys[-1] = (n, dict(keys[0][1]), "lin")
    return keys, speed, cycle_t


def clip(frames, keys, loop=True, note="", speed=0.0):
    """(v4_clips.clip's record: this module is imported by v4_clips)"""
    return dict(frames=frames, keys=keys, hit=None, load=None, loop=loop, aim=None, reach=None, react=None, note=note,
                striker=None, shift=False, breath=False, breaths=2, lag=False, speed=speed)


CLIPS = {}
# the hero's walk (HOLD with nobody near; on him the scenes' walk as well: ClipPlayer.Substitute)
_keys, WALK_ALONE_SPEED, _T = walk_from("16_15", 30, cycles=3, lean=-1.0, arm_gain=2.3)
CLIPS["walk_alone"] = clip(30, _keys, note="the walk, from life (CMU 16_15, three cycles averaged, scaled to him)",
                           speed=round(WALK_ALONE_SPEED, 3))

# the everyday walk (anyone walking in a scene: the men from the car, the guard, Pell, the Driver, Harrow), from a
# different person (CMU 02_01, two cycles averaged), so they don't all walk like him. The scenes' playback rates were
# tuned to the old walk's 1.18 m/s: the game plays this one at 1.18 / WALK_SPEED of the rate asked (Fighter.Awake),
# so every scene keeps its pace with the feet planted.
_wkeys, WALK_SPEED, _WT = walk_from("02_01", 28, cycles=2, lean=-0.5, arm_gain=1.0, fist=50.0, elbow_max=30.0)
CLIPS["walk"] = clip(28, _wkeys, note="the everyday walk, from life (CMU 02_01, two cycles averaged, scaled to him)",
                     speed=round(WALK_SPEED, 3))

