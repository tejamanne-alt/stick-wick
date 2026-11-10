"""The V3 body and its pose engine (Combat 3.0, docs/design/combat_v3.md).

The body: the strike lab's (tools/blender/strike_lab.py): arms 25% longer (0.26 + 0.24 + a 0.08
fist), fists you can see, shoulders and hips set wider apart so the near and far limbs separate in a
side view. One ink material: the far-limb charcoal is drawn by the game's shader, by depth.

The pose engine goes past build_stickman's flat angles, because real blows are not flat:
- every bone still has an ABSOLUTE side-plane angle (0 hangs down, 90 forward, 180 up);
- the torso bones can TWIST about their own axis (hips and shoulders turn through a cross);
- the limbs can YAW out of the side plane (a hook's forearm comes across the face, a roundhouse's
  shin swings round from the side), so in the game's slightly turned camera they travel in depth;
- legs can be posed by FOOT TARGETS: two-bone IK with the knee forward, solved on every frame, so a
  planted foot stays planted while the hips move over it (no foot sliding);
- arms can be posed by FIST TARGETS at a key (two-bone IK, elbow down), so a punch lands exactly
  on the target man's chin at its reach distance;
- clips are baked on EVERY frame with per-segment easing (snap into an impact, ease out of a
  recovery), not left to Bezier handles, so a strike has a real acceleration and a real stop.
"""
import math

from mathutils import Quaternion, Vector

import build_stickman as bs

UP, FO, HA, DD = 0.26, 0.24, 0.08, 0.11
LEG_Y = DD * 0.8
FIST_R = 0.072
SH = bs.SHOULDER
THIGH, SHIN = bs.THIGH, bs.SHIN
FOOT_Z = bs.FOOT_Z
FOREARM_EFF = FO + HA * 0.5           # shoulder → fist centre, with the fist in line


def patch_bones():
    """Swap build_stickman's skeleton for the V3 one (in place: its functions read bs.BONES)."""
    B = bs.BONES
    for s, y in (("L", DD), ("R", -DD)):
        B["upperarm." + s] = ("chest", (0, 0, SH), (0, 0, SH - UP), y, 0, 0.058)
        B["forearm." + s] = ("upperarm." + s, (0, 0, SH - UP), (0, 0, SH - UP - FO), y, 0, 0.055)
        B["hand." + s] = ("forearm." + s, (0, 0, SH - UP - FO), (0, 0, SH - UP - FO - HA), y, 0, 0.0)
        B["thigh." + s] = ("hips", (0, 0, bs.HIP_Z), (0, 0, bs.HIP_Z - THIGH), y * 0.8, 0, 0.072)
        B["shin." + s] = ("thigh." + s, (0, 0, bs.HIP_Z - THIGH), (0, 0, bs.HIP_Z - THIGH - SHIN), y * 0.8, 0, 0.066)
    bs.HANG_DROP = SH + UP + FO + 0.03


def build_mesh_v3(arm, material_name="Ink", grey=0.02):
    """The ink body in one material: round head, belly and chest, thick limbs, fists, round feet."""
    import bpy
    mat = bpy.data.materials.new(material_name)
    mat.diffuse_color = (grey, grey, grey, 1)
    if mat.node_tree is None:
        mat.use_nodes = True
    b = mat.node_tree.nodes.get("Principled BSDF")
    if b:
        b.inputs["Base Color"].default_value = (grey, grey, grey, 1)
        b.inputs["Roughness"].default_value = 0.6
    parts = []

    def part(obj, bone):
        obj.data.materials.append(mat)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    for nm, (_p, head, tail, y, _rest, radius) in bs.BONES.items():
        h = Vector(head) + Vector((0, y, 0))
        t = Vector(tail) + Vector((0, y, 0))
        if nm == "head":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=bs.HEAD_RADIUS, segments=24, ring_count=14, location=(0, 0, bs.HEAD_CENTER))
            part(bpy.context.object, nm)
            continue
        if nm == "spine":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.165, segments=18, ring_count=10, location=(0, 0, 0.81))
            bpy.context.object.scale = (1.0, 1.05, 1.12)
            part(bpy.context.object, nm)
        if nm == "chest":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.158, segments=18, ring_count=10, location=(0, 0, 0.935))
            part(bpy.context.object, nm)
        if nm in ("shin.L", "shin.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.078, segments=12, ring_count=8, location=t + Vector((0.04, 0, -0.005)))
            bpy.context.object.scale = (1.45, 1.0, 0.8)
            part(bpy.context.object, nm)
        if nm in ("hand.L", "hand.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=FIST_R, segments=14, ring_count=10, location=(h + t) / 2)
            bpy.context.object.scale = (1.05, 1.0, 1.15)
            part(bpy.context.object, nm)
            continue
        if radius <= 0:
            continue
        bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=(t - h).length, vertices=12, location=(h + t) / 2)
        part(bpy.context.object, nm)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=t)
        part(bpy.context.object, nm)
        if nm in ("upperarm.L", "upperarm.R", "thigh.L", "thigh.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=h)
            part(bpy.context.object, nm)
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


# ---------------------------------------------------------------- pose description
# short names → bones
SHORT = {"hips": "hips", "spine": "spine", "chest": "chest", "head": "head",
         "uaL": "upperarm.L", "faL": "forearm.L", "haL": "hand.L", "uaR": "upperarm.R", "faR": "forearm.R", "haR": "hand.R",
         "thL": "thigh.L", "shL": "shin.L", "thR": "thigh.R", "shR": "shin.R"}
LONG = {v: k for k, v in SHORT.items()}
ORDER = ["hips", "spine", "chest", "head", "upperarm.L", "forearm.L", "hand.L", "upperarm.R", "forearm.R", "hand.R",
         "thigh.L", "shin.L", "thigh.R", "shin.R"]
TORSO = ("hips", "spine", "chest", "head")


def P(base=None, **kw):
    """A pose: base overridden by keywords.
      angles   hips spine chest head uaL faL haL uaR faR haR thL shL thR shR   (absolute, degrees)
      tw_<torso bone>   twist about the bone's own axis (+ brings the NEAR (R) shoulder/hip forward)
      yw_<limb short>   yaw out of the side plane (+ swings the limb towards the FAR side, +y)
      hx hz    the hips' head position (forward, up), metres
      fL fR    foot (ankle) targets (x, z): the leg is solved by IK.  None = use the leg angles
      hL hR    fist-centre targets (x, z) at this key: the arm is solved by IK (elbow down)
      liftL liftR   the foot's arc height on the way INTO this key
      dip      extra hip drop on the way into this key (a weight transfer through a bent knee)"""
    p = {} if base is None else {k: (dict(v) if isinstance(v, dict) else v) for k, v in base.items()}
    for k, v in kw.items():
        if k in ("hL", "hR") and v is not None:
            # a fist target replaces the arm's angles at this key
            side = k[1]
            for b in ("ua", "fa", "ha"):
                p.pop(b + side, None)
        if k in ("uaL", "faL", "uaR", "faR"):
            p.pop("h" + k[2], None)
        if k in ("thL", "shL", "thR", "shR"):
            p.pop("f" + k[2], None)
        if v is None:
            p.pop(k, None)
        else:
            p[k] = v
    for k in ("liftL", "liftR", "dip"):          # per-segment: never inherited from the base
        if k not in kw:
            p.pop(k, None)
    return p


def _deg(q):
    return math.radians(q)


def bone_q(angle, rest, yaw=0.0, twist=0.0):
    """World rotation of a bone from its rest orientation (rest bones are vertical)."""
    return (Quaternion((0, 0, 1), _deg(yaw)) @ Quaternion((0, 1, 0), -_deg(angle - rest)) @
            Quaternion((0, 0, 1), _deg(twist)))


def _dir(a):
    return Vector((math.sin(_deg(a)), 0.0, -math.cos(_deg(a))))


def _ang(v):
    """Side-plane angle of a vector (x, z)."""
    return math.degrees(math.atan2(v[0], -v[1]))


def two_bone(j, t, l1, l2, bend):
    """Planar two-bone IK from joint j to target t (x, z). bend +1: the middle joint ahead of the
    line (a knee), -1: behind/below it (an elbow for a straight punch). Returns (a1, a2, reached)."""
    vx, vz = t[0] - j[0], t[1] - j[1]
    d = math.hypot(vx, vz)
    reached = d <= l1 + l2 - 1e-4
    d = min(max(d, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
    av = _ang((vx, vz))
    ca = (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)
    al = math.degrees(math.acos(max(-1.0, min(1.0, ca))))
    a1 = av + bend * al
    kx = j[0] + l1 * math.sin(_deg(a1))
    kz = j[1] - l1 * math.cos(_deg(a1))
    a2 = _ang((t[0] - kx, t[1] - kz))
    return a1, a2, reached


class Rig:
    """Rest geometry of the V3 skeleton, for forward kinematics in plain Python."""

    def __init__(self):
        self.head, self.tail, self.parent, self.rest = {}, {}, {}, {}
        for n, (par, h, t, y, rest, _r) in bs.BONES.items():
            self.head[n] = Vector(h) + Vector((0, y, 0))
            self.tail[n] = Vector(t) + Vector((0, y, 0))
            self.parent[n] = par
            self.rest[n] = rest

    def world_q(self, p):
        q = {}
        for n in ORDER:
            short = LONG[n]
            a = p.get(short)
            if a is None and n.startswith("hand"):
                a = p.get("fa" + n[-1])
            q[n] = bone_q(a, self.rest[n], p.get("yw_" + short, p.get("yw_fa" + n[-1], 0.0) if n.startswith("hand") else 0.0),
                          p.get("tw_" + n, 0.0) if n in TORSO else 0.0)
        return q

    def fk(self, p, q=None):
        """Heads and tails of every bone in clip space."""
        q = q or self.world_q(p)
        pos, tip = {}, {}
        hips0 = self.head["hips"]
        for n in ORDER:
            par = self.parent[n]
            if par is None:
                pos[n] = Vector((p.get("hx", 0.0), 0.0, p["hz"]))
            else:
                pos[n] = pos[par] + q[par] @ (self.head[n] - self.head[par])
            tip[n] = pos[n] + q[n] @ (self.tail[n] - self.head[n])
        return pos, tip

    def joint(self, p, bone):
        """Where a limb's root joint is (shoulder or hip), from the torso alone."""
        q = {}
        for n in TORSO:
            short = n
            q[n] = bone_q(p[short], self.rest[n], 0.0, p.get("tw_" + n, 0.0))
        hips = Vector((p.get("hx", 0.0), 0.0, p["hz"]))
        if bone.startswith("thigh"):
            return hips + q["hips"] @ (self.head[bone] - self.head["hips"])
        spine = hips + q["hips"] @ (self.head["spine"] - self.head["hips"])
        chest = spine + q["spine"] @ (self.head["chest"] - self.head["spine"])
        return chest + q["chest"] @ (self.head[bone] - self.head["chest"])

    def solve_legs(self, p):
        for s in ("L", "R"):
            f = p.get("f" + s)
            if f is None:
                continue
            j = self.joint(p, "thigh." + s)
            a1, a2, _ = two_bone((j.x, j.z), f, THIGH, SHIN, +1)
            p["th" + s], p["sh" + s] = a1, a2
        return p

    def solve_arms(self, p):
        for s in ("L", "R"):
            h = p.get("h" + s)
            if h is None:
                continue
            j = self.joint(p, "upperarm." + s)
            bend = -1 if h[0] >= j.x - 0.05 else +1
            a1, a2, reached = two_bone((j.x, j.z), h, UP, FOREARM_EFF, bend)
            p["ua" + s], p["fa" + s] = a1, a2
            p.setdefault("ha" + s, a2)
            p["_reach" + s] = reached
        return p


RIG = None


def rig():
    global RIG
    if RIG is None:
        RIG = Rig()
    return RIG


# ---------------------------------------------------------------- easing and sampling
def ease(kind, u):
    if kind == "lin":
        return u
    if kind == "in":          # accelerating (a launch)
        return u * u
    if kind == "in3":         # hard acceleration into an impact
        return u * u * u
    if kind == "out":         # decelerating (a settle)
        return 1 - (1 - u) * (1 - u)
    if kind == "out3":
        return 1 - (1 - u) ** 3
    if kind == "hold":
        return 0.0 if u < 1 else 1.0
    return u * u * (3 - 2 * u)  # smooth


ANGLE_KEYS = tuple(SHORT.keys())


def resolve(p):
    """A key pose with every target turned into angles (feet targets are kept for per-frame IK)."""
    p = dict(p)
    p.setdefault("hx", 0.0)
    for b in TORSO:
        p.setdefault(b, 180.0)
    r = rig()
    r.solve_arms(p)
    r.solve_legs(p)
    for s in ("L", "R"):            # a fist not posed on its own continues its forearm
        p.setdefault("ha" + s, p["fa" + s])
        if "yw_fa" + s in p:
            p.setdefault("yw_ha" + s, p["yw_fa" + s])
    return p


def sample(keys, f):
    """The pose at frame f: keys = [(frame, pose, ease)], already resolved."""
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
                if k.startswith("_") or k in ("fL", "fR", "hL", "hR", "liftL", "liftR", "dip"):
                    continue
                z = 0.0 if k.startswith(("tw_", "yw_")) else None
                a = p0.get(k, p1.get(k, 0.0) if z is None else z)
                b = p1.get(k, p0.get(k, 0.0) if z is None else z)
                if isinstance(a, (int, float)) and isinstance(b, (int, float)):
                    out[k] = a + (b - a) * w
            # the hips sink through a weight transfer
            if p1.get("dip"):
                out["hz"] -= p1["dip"] * math.sin(math.pi * u)
            # feet: planted feet interpolate as targets and are solved on this frame
            for s in ("L", "R"):
                fa, fb = p0.get("f" + s), p1.get("f" + s)
                if fa is not None and fb is not None:
                    # a foot moves on its own timing: it leaves late and lands early (a real step)
                    uw = ease("smooth", min(1.0, max(0.0, (u - 0.1) / 0.75))) if (fa != fb) else 0.0
                    x = fa[0] + (fb[0] - fa[0]) * uw
                    z = fa[1] + (fb[1] - fa[1]) * uw + p1.get("lift" + s, 0.0) * math.sin(math.pi * min(1.0, max(0.0, (u - 0.1) / 0.75)))
                    out["f" + s] = (x, z)
            r = rig()
            r.solve_legs(out)
            return out
    return dict(keys[-1][1])


def ground_check(p):
    """Lowest point of the feet (ankle height minus the foot's thickness)."""
    _pos, tip = rig().fk(p)
    return min(tip["shin.L"].z, tip["shin.R"].z) - 0.062


# ---------------------------------------------------------------- keying onto a Blender armature
def key_clip(arm, name, spec, fps_frames=None):
    """Bake a clip on every frame into a new action."""
    import bpy
    keys = [(f, resolve(p), e) for f, p, e in spec["keys"]]
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    r = rig()
    rest_q = {pb.name: pb.bone.matrix_local.to_quaternion() for pb in arm.pose.bones}
    prev = {}
    for f in range(spec["frames"] + 1):
        p = sample(keys, f)
        q = r.world_q(p)
        for n in ORDER:
            pb = arm.pose.bones[n]
            par = r.parent[n]
            r0 = rest_q[n]
            rel = q[n] if par is None else q[par].inverted() @ q[n]
            loc = r0.inverted() @ rel @ r0
            if n in prev and prev[n].dot(loc) < 0:
                loc = -loc
            prev[n] = loc
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = loc
            pb.keyframe_insert("rotation_quaternion", frame=f, group=n)
            if n == "hips":
                off = Vector((p.get("hx", 0.0), 0.0, p["hz"])) - r.head["hips"]
                pb.location = r0.to_matrix().inverted() @ off
                pb.keyframe_insert("location", frame=f, group=n)
            pb.scale = (1, 1, 1)
    # every frame is a key: make the curves straight between them (Blender 4 and 5 layouts)
    curves = getattr(act, "fcurves", None)
    if curves is None:
        curves = [fc for layer in act.layers for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves]
    for fc in curves:
        for kp in fc.keyframe_points:
            kp.interpolation = "LINEAR"
    act.frame_range = (0, spec["frames"])
    return act


def apply_sampled(arm, p):
    """Pose an armature directly from a sampled pose (previews)."""
    r = rig()
    q = r.world_q(p)
    for n in ORDER:
        pb = arm.pose.bones[n]
        par = r.parent[n]
        r0 = pb.bone.matrix_local.to_quaternion()
        rel = q[n] if par is None else q[par].inverted() @ q[n]
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = r0.inverted() @ rel @ r0
        if n == "hips":
            off = Vector((p.get("hx", 0.0), 0.0, p["hz"])) - r.head["hips"]
            pb.location = r0.to_matrix().inverted() @ off
