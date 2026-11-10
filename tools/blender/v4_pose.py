"""The V4 pose engine: hand-keyed poses for the 29-bone body, baked on every frame with the detail
layers that make motion read as real.

POSE LANGUAGE (a pose is a dict; angles are absolute in the side plane: 0 down, 90 forward, 180 up)
  hx hz                 the hips' head (forward, up), metres
  hips spine spine_mid chest neck head           angles; tw_<bone> twists it about its own axis
                                                  (+ brings the NEAR (R) side forward)
  clL clR = (protract, elevate)                  the collarbones, degrees: protract = the shoulder
                                                  forward (a reach), elevate = up (a shrug)
  uaL faL haL uaR faR haR                        arm angles (ha = the wrist; default: the forearm's)
  yw_uaL yw_faL ...                              yaw out of the side plane (+ towards the far side)
  twL twR                                        forearm twist: the fist turning over (−90 = a jab's
                                                  knuckles level for the lead hand; +90 for the rear)
  fiL fiR                                        finger curl: 165 a fist, 90 a grip, 20 open
  hL hR = (x, z)                                 fist-centre target at this key (arm IK, elbow down)
  fL fR = (x, pitch)                             a PLANTED foot: the ball of the foot at x, the heel
                                                  raised by `pitch` degrees (0 flat, 40 on the ball)
  fL fR = (x, z, pitch)                          a foot IN THE AIR: the ankle at (x, z), the foot
                                                  pitched (+ toes down, − toes up)
  thL shL ...                                    leg angles instead (kicks), with yw_thL etc.
  liftL liftR                                    a step into this key: the foot's arc height
  dip                                            the hips sink through the move into this key
EASING per key (the motion INTO it): in in3 out out3 smooth lin

THE DETAIL LAYERS (on every frame, after the keys)
  · steps: a foot that moves peels heel first (toe-off), swings with the toes down, lands heel
    first with the toes up, then rolls flat: four phases, never a slide
  · overlap: the head and neck lag the chest and settle with a small overshoot; the arm that is not
    striking lags the body (spring-damped), so nothing moves as one rigid block
  · impact: at the hit frame a short recoil runs back through the striker (chest, neck, head), the
    fist squeezes tight, and the shoulder pops forward a few degrees for two frames
  · breathing: loops can add a breath (chest and shoulders rise, hips sink)"""
import math

from mathutils import Quaternion, Vector

import v4_rig as r4
import build_stickman as bs

Y = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
THIGH, SHIN = bs.THIGH, bs.SHIN
FOOT_LEN, ANKLE_Z, BALL_Z = r4.FOOT_LEN, r4.ANKLE_Z, r4.BALL_Z
BASE = math.asin((ANKLE_Z - BALL_Z) / FOOT_LEN)        # the foot's slope at rest (heel to ball)
FIST = r4.FO + r4.HAND * 0.7                            # elbow → fist centre with the fist closed
TORSO = ("hips", "spine", "spine_mid", "chest", "neck", "head")
ARMS = ("ua", "fa", "ha")


def P(base=None, **kw):
    p = {} if base is None else dict(base)
    for k, v in kw.items():
        if k in ("hL", "hR") and v is not None:
            for b in ARMS:
                p.pop(b + k[1], None)
        if k[:2] in ("ua", "fa") and len(k) == 3:
            p.pop("h" + k[2], None)
        if k[:2] in ("th", "sh") and len(k) == 3:
            p.pop("f" + k[2], None)
        if k in ("fL", "fR"):
            for b in ("th", "sh"):
                p.pop(b + k[1], None)
        if v is None:
            p.pop(k, None)
        else:
            p[k] = v
    for k in ("liftL", "liftR", "dip"):
        if k not in kw:
            p.pop(k, None)
    return p


# ---------------------------------------------------------------- geometry
def side(a, yaw=0.0):
    r = math.radians(a)
    return Quaternion(Z, math.radians(yaw)) @ Vector((math.sin(r), 0, -math.cos(r)))


def q_torso(a, tw):
    return Quaternion(Y, -math.radians(a - 180)) @ Quaternion(Z, math.radians(tw))


def q_limb(a, yaw=0.0, tw=0.0):
    return Quaternion(Z, math.radians(yaw)) @ Quaternion(Y, -math.radians(a)) @ Quaternion(Z, math.radians(tw))


def ankle_of(ball_x, pitch):
    s = BASE + math.radians(pitch)
    return (ball_x - FOOT_LEN * math.cos(s), BALL_Z + FOOT_LEN * math.sin(s))


def two_bone(j, t, l1, l2, bend):
    vx, vz = t[0] - j[0], t[1] - j[1]
    d = math.hypot(vx, vz)
    reach = d <= l1 + l2 - 1e-4
    d = min(max(d, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
    av = math.degrees(math.atan2(vx, -vz))
    ca = (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)
    a1 = av + bend * math.degrees(math.acos(max(-1.0, min(1.0, ca))))
    kx = j[0] + l1 * math.sin(math.radians(a1))
    kz = j[1] - l1 * math.cos(math.radians(a1))
    a2 = math.degrees(math.atan2(t[0] - kx, -(t[1] - kz)))
    return a1, a2, reach


def head_pos(n):
    return Vector(r4.BONES[n][1])


def world(p):
    """World rotation deltas of every bone, and the heads of the joints IK needs."""
    q = {}
    for b in TORSO:
        q[b] = q_torso(p.get(b, 180.0), p.get("tw_" + b, 0.0))
    hips = Vector((p.get("hx", 0.0), 0.0, p["hz"]))
    pos = {"hips": hips}
    for b in TORSO[1:]:
        par = r4.BONES[b][0]
        pos[b] = pos[par] + q[par] @ (head_pos(b) - head_pos(par))
    for s in ("L", "R"):
        y = 1 if s == "L" else -1
        pro, ele = p.get("cl" + s, (0.0, 0.0))
        want = q["chest"] @ Vector((math.sin(math.radians(pro)), y * math.cos(math.radians(pro)), 0.12 + math.tan(math.radians(ele)))).normalized()
        q["clavicle." + s] = r4.REST_DIR["clavicle." + s].rotation_difference(want)
        pos["clavicle." + s] = pos["chest"] + q["chest"] @ (head_pos("clavicle." + s) - head_pos("chest"))
        sh = pos["clavicle." + s] + q["clavicle." + s] @ (Vector(r4.BONES["clavicle." + s][2]) - head_pos("clavicle." + s))
        pos["upperarm." + s] = sh
        tw = p.get("tw" + s, 0.0)
        q["upperarm." + s] = q_limb(p.get("ua" + s, 0.0), p.get("yw_ua" + s, 0.0))
        q["forearm." + s] = q_limb(p.get("fa" + s, 0.0), p.get("yw_fa" + s, 0.0), tw)
        q["hand." + s] = q_limb(p.get("ha" + s, p.get("fa" + s, 0.0)), p.get("yw_fa" + s, 0.0), tw)
        q["fingers." + s] = q["hand." + s] @ Quaternion(Y, -math.radians(p.get("fi" + s, 165.0)))
        pos["thigh." + s] = hips + q["hips"] @ (head_pos("thigh." + s) - head_pos("hips"))
        q["thigh." + s] = q_limb(p.get("th" + s, 0.0), p.get("yw_th" + s, 0.0))
        q["shin." + s] = q_limb(p.get("sh" + s, 0.0), p.get("yw_sh" + s, 0.0))
        fp = p.get("_fp" + s, 0.0)
        tp = p.get("_tp" + s, 0.0)
        fy = p.get("yw_ft" + s, 0.0)
        q["foot." + s] = Quaternion(Z, math.radians(fy)) @ Quaternion(Y, math.radians(fp))
        q["toes." + s] = Quaternion(Z, math.radians(fy)) @ Quaternion(Y, math.radians(tp))
    return q, pos


def solve(p):
    """Turn targets into angles: legs from the feet, arms from fist targets."""
    p = dict(p)
    for b in TORSO:
        p.setdefault(b, 180.0)
    q, pos = world(p)
    for s in ("L", "R"):
        f = p.get("f" + s)
        if f is not None:
            if len(f) == 2:                       # planted: ball at x, heel up by pitch; toes flat
                ax, az = ankle_of(f[0], f[1])
                p["_fp" + s], p["_tp" + s] = f[1], 0.0
            else:                                 # in the air: ankle at (x, z), the foot pitched
                ax, az = f[0], f[1]
                p["_fp" + s], p["_tp" + s] = f[2], f[2] * 0.6
            j = pos["thigh." + s]
            p["th" + s], p["sh" + s], _ = two_bone((j.x, j.z), (ax, az), THIGH, SHIN, +1)
        elif "_fp" + s not in p:
            # kicks: the foot points along the shin (toes extended), unless told
            p["_fp" + s] = p.get("fp" + s, 30.0)          # kicks: the foot extended (toes pointed)
            p["_tp" + s] = p.get("tp" + s, 15.0)
        h = p.get("h" + s)
        if h is not None:
            j = pos["upperarm." + s]
            a1, a2, ok = two_bone((j.x, j.z), h, r4.UP, FIST, -1)
            p["ua" + s], p["fa" + s] = a1, a2
            p.setdefault("ha" + s, a2)
            p["_reach" + s] = ok
        p.setdefault("ha" + s, p.get("fa" + s, 0.0))
    return p


# ---------------------------------------------------------------- easing, sampling, layers
def ease(kind, u):
    return {"lin": u, "in": u * u, "in3": u ** 3, "out": 1 - (1 - u) ** 2, "out3": 1 - (1 - u) ** 3,
            "hold": 0.0 if u < 1 else 1.0}.get(kind, u * u * (3 - 2 * u))


ZERO_DEFAULT = ("tw", "yw", "_fp", "_tp", "cl")


def _num(p, k, other):
    if k in p:
        return p[k]
    if k.startswith(ZERO_DEFAULT):
        return (0.0, 0.0) if k.startswith("cl") else 0.0
    return other.get(k, 0.0)


def _foot_step(fa, fb, u, lift):
    """A real step between two planted feet: heel peel (toe-off), swing, heel strike, roll flat.
    Returns (ankle_x, ankle_z, foot pitch, toe pitch)."""
    ax0, az0 = ankle_of(fa[0], fa[1])
    ax1, az1 = ankle_of(fb[0], fb[1])
    if u < 0.2:                                   # toe-off: the heel peels up, the ball stays
        k = u / 0.2
        pitch = fa[1] + (35 - fa[1]) * k * k
        x, z = ankle_of(fa[0], pitch)
        return x, z, pitch, 0.0
    if u < 0.78:                                  # swing: an arc, toes pointing then lifting
        k = (u - 0.2) / 0.58
        kk = k * k * (3 - 2 * k)
        sx, sz = ankle_of(fa[0], 35)
        ex, ez = ankle_of(fb[0] - 0.02, -14)
        x = sx + (ex - sx) * kk
        z = sz + (ez - sz) * kk + lift * math.sin(math.pi * k)
        pitch = 35 + (-14 - 35) * kk
        return x, z, pitch, pitch * 0.4
    k = (u - 0.78) / 0.22                         # heel strike, then the sole rolls down flat
    pitch = -14 + (fb[1] + 14) * (k * k * (3 - 2 * k))
    x, z = ankle_of(fb[0] - 0.02 * (1 - k), pitch)
    return x, z, pitch, 0.0


def sample(keys, f):
    if f <= keys[0][0]:
        return dict(keys[0][1])
    for i in range(1, len(keys)):
        f1, p1, e1 = keys[i]
        f0, p0, _ = keys[i - 1]
        if f <= f1:
            u = (f - f0) / float(max(1e-6, f1 - f0))
            w = ease(e1, u)
            out = {}
            for k in set(p0) | set(p1):
                if k in ("fL", "fR", "hL", "hR", "liftL", "liftR", "dip") or k.startswith("_reach"):
                    continue
                a, b = _num(p0, k, p1), _num(p1, k, p0)
                if isinstance(a, tuple):
                    out[k] = tuple(x + (y - x) * w for x, y in zip(a, b))
                elif isinstance(a, (int, float)) and isinstance(b, (int, float)):
                    out[k] = a + (b - a) * w
            if p1.get("dip"):
                out["hz"] -= p1["dip"] * math.sin(math.pi * u)
            for s in ("L", "R"):
                fa, fb = p0.get("f" + s), p1.get("f" + s)
                if fa is None or fb is None:
                    continue
                if len(fa) == 2 and len(fb) == 2:
                    if abs(fa[0] - fb[0]) > 0.015:
                        x, z, fp, tp = _foot_step(fa, fb, u, p1.get("lift" + s, 0.025))
                        out["f" + s] = (x, z, fp)
                        out["_tp" + s] = tp
                    else:
                        out["f" + s] = (fa[0] + (fb[0] - fa[0]) * w, fa[1] + (fb[1] - fa[1]) * w)
                else:
                    A = ankle_of(*fa) + (fa[1],) if len(fa) == 2 else fa
                    B = ankle_of(*fb) + (fb[1],) if len(fb) == 2 else fb
                    out["f" + s] = tuple(x + (y - x) * w for x, y in zip(A, B))
                    if len(fb) == 2 and w > 0.999:
                        out["f" + s] = fb
            return out
    return dict(keys[-1][1])


class Spring:
    """A damped spring following a target (per channel): lag and a small overshoot."""

    def __init__(self, hz, damp):
        self.w = 2 * math.pi * hz
        self.z = damp
        self.x = None
        self.v = 0.0

    def step(self, target, dt):
        if self.x is None:
            self.x = target
            return target
        # semi-implicit Euler in small sub-steps: stable at any frame rate
        n = 16
        h = dt / n
        for _ in range(n):
            acc = self.w * self.w * (target - self.x) - 2 * self.z * self.w * self.v
            self.v += acc * h
            self.x += self.v * h
        return self.x


def floor_guard(p):
    """A leg posed by angles (a kick going out or coming down) never puts its foot through the floor:
    the ankle is lifted onto it by IK, keeping the knee's side."""
    for s in ("L", "R"):
        if p.get("f" + s) is not None:
            continue
        f = fk(p)
        deficit = max(ANKLE_Z - f["ankle" + s].z, BALL_Z - f["ball" + s].z)
        if deficit <= 0.0005:
            continue
        _q, pos = world(p)
        j = pos["thigh." + s]
        a = f["ankle" + s]
        p["th" + s], p["sh" + s], _ = two_bone((j.x, j.z), (a.x, a.z + deficit), THIGH, SHIN, +1)


def frames(spec, fps=30):
    """Every frame of a clip, resolved, with the detail layers on."""
    keys = [(f, solve(p), e) for f, p, e in spec["keys"]]
    n = spec["frames"]
    hit = spec.get("hit")
    striker = spec.get("striker")              # "L"/"R": the striking arm (the other one lags)
    lag = spec.get("lag", True)
    springs = {}
    out = []
    passes = 2 if spec.get("loop") else 1
    dt = 1.0 / fps
    for pas in range(passes):
        out = []
        for f in range(n + 1):
            p = sample(keys, f)
            p = solve(p)
            floor_guard(p)
            if lag:
                # the head and neck ride on the chest, a beat late
                for ch, hz, dmp in (("neck", 9.0, 0.55), ("head", 7.0, 0.5), ("tw_head", 7.0, 0.5), ("tw_neck", 8.0, 0.55)):
                    if ch in p:
                        sp = springs.setdefault(ch, Spring(hz, dmp))
                        p[ch] = sp.step(p[ch], dt)
                # the off arm lags the body
                if striker in ("L", "R"):
                    off = "R" if striker == "L" else "L"
                    for b in ("ua", "fa"):
                        k = b + off
                        if k in p:
                            sp = springs.setdefault(k, Spring(8.0, 0.6))
                            p[k] = sp.step(p[k], dt)
                    p["ha" + off] = p.get("fa" + off, p.get("ha" + off, 0.0))
            if hit is not None and f >= hit:
                t = f - hit
                if t <= 6:
                    k = math.exp(-t / 1.8)
                    p["chest"] = p.get("chest", 180) + 3.0 * k * math.cos(t * 1.7)
                    p["neck"] = p.get("neck", 180) + 4.0 * k * math.cos(t * 1.7 - 0.6)
                    p["head"] = p.get("head", 180) + 5.0 * k * math.cos(t * 1.7 - 1.0)
                    if striker in ("L", "R"):
                        p["fi" + striker] = min(178.0, p.get("fi" + striker, 165.0) + 10.0 * k)
                        pro, ele = p.get("cl" + striker, (0.0, 0.0))
                        p["cl" + striker] = (pro + 8.0 * k, ele + 3.0 * k)
            if spec.get("breath"):
                b = math.sin(2 * math.pi * f / n * spec.get("breaths", 2))
                p["chest"] = p.get("chest", 180) + 1.6 * b
                p["hz"] = p["hz"] - 0.004 * b
                for s in ("L", "R"):
                    pro, ele = p.get("cl" + s, (0.0, 0.0))
                    p["cl" + s] = (pro, ele + 2.5 * b)
            out.append(p)
    return out


# ---------------------------------------------------------------- onto an armature
def locals_of(arm, p):
    rest_q = {pb.name: pb.bone.matrix_local.to_quaternion() for pb in arm.pose.bones}
    q, _pos = world(p)
    return r4.to_local(q, rest_q), rest_q


def apply(arm, p):
    loc, rest_q = locals_of(arm, p)
    for n in r4.ORDER:
        pb = arm.pose.bones[n]
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = loc[n]
        if n == "hips":
            off = Vector((p.get("hx", 0.0), 0.0, p["hz"])) - head_pos("hips")
            pb.location = rest_q["hips"].to_matrix().inverted() @ off


def key_clip(arm, name, spec):
    import bpy
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    prev = {}
    for f, p in enumerate(frames(spec)):
        loc, rest_q = locals_of(arm, p)
        for n in r4.ORDER:
            pb = arm.pose.bones[n]
            qq = loc[n]
            if n in prev and prev[n].dot(qq) < 0:
                qq = -qq
            prev[n] = qq
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = qq
            pb.keyframe_insert("rotation_quaternion", frame=f, group=n)
            if n == "hips":
                off = Vector((p.get("hx", 0.0), 0.0, p["hz"])) - head_pos("hips")
                pb.location = rest_q["hips"].to_matrix().inverted() @ off
                pb.keyframe_insert("location", frame=f, group=n)
    act.frame_range = (0, spec["frames"])
    return act


def fk(p):
    """Positions of the striking surfaces (for the aim report)."""
    q, pos = world(p)
    out = {}
    for s in ("L", "R"):
        sh = pos["upperarm." + s]
        el = sh + q["upperarm." + s] @ Vector((0, 0, -r4.UP))
        wr = el + q["forearm." + s] @ Vector((0, 0, -r4.FO))
        out["fist" + s] = wr + q["hand." + s] @ Vector((0, 0, -r4.HAND * 0.7))
        out["elbow" + s] = el
        j = pos["thigh." + s]
        kn = j + q["thigh." + s] @ Vector((0, 0, -THIGH))
        an = kn + q["shin." + s] @ Vector((0, 0, -SHIN))
        out["knee" + s] = kn
        out["shin" + s] = kn + (an - kn) * 0.65
        out["ankle" + s] = an
        out["ball" + s] = an + q["foot." + s] @ (Vector(r4.BONES["foot." + s][2]) - Vector(r4.BONES["foot." + s][1]))
    out["head"] = pos["head"] + q["head"] @ Vector((0, 0, bs.HEAD_CENTER - 1.05))
    return out
