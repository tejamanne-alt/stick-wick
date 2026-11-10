"""The V4 skeleton (docs/previews/skeleton_lab.png): 29 bones.

    hips · spine · spine_mid* · chest · neck* · head
    clavicle.L/R* · upperarm · forearm · hand (wrist to knuckles) · fingers*
    thigh · shin · foot (ankle to ball)* · toes*                      (* new)

Every old bone keeps its name and its place (the game's code finds bones by name), so the old clips
carry over: `transfer_frame` turns a V3 pose (world rotations) into a V4 one and fills in the new
bones procedurally, which already adds detail to every old clip:
- the spine curves through two segments, the neck sits between chest and head;
- the shoulders follow the arms: they shrug as an arm rises over the shoulder, and reach forward
  when an arm drives out straight (the punch's last inches);
- the feet roll: flat when the ankle is down, heel lifting onto the ball as the ankle rises while
  the foot is still on the floor (a push-off, a pivot, the rear heel in a guard), pointing with the
  shin once the foot leaves the ground;
- the hands close into fists in fights and hang relaxed, half curled, everywhere else."""
import math

from mathutils import Quaternion, Vector

import build_stickman as bs

UP, FO = 0.26, 0.24
DD, LY = 0.11, 0.088
HAND, FING = 0.05, 0.045
FOOT_LEN, TOE_LEN = 0.10, 0.05
ANKLE_Z = bs.FOOT_Z                      # 0.06: the shin's tip in the rest pose
BALL_Z = 0.035
_fd = Vector((math.sqrt(max(0.0, FOOT_LEN ** 2 - (ANKLE_Z - BALL_Z) ** 2)), 0.0, -(ANKLE_Z - BALL_Z))).normalized()
FOOT_DIR = (_fd.x, 0.0, _fd.z)

# name: (parent, head, tail, mesh radius)
def bones():
    B = {
        "hips": (None, (0, 0, 0.70), (0, 0, 0.78), 0.11),
        "spine": ("hips", (0, 0, 0.78), (0, 0, 0.85), 0.12),
        "spine_mid": ("spine", (0, 0, 0.85), (0, 0, 0.92), 0.12),
        "chest": ("spine_mid", (0, 0, 0.92), (0, 0, 1.0), 0.12),
        "neck": ("chest", (0, 0, 1.0), (0, 0, 1.05), 0.045),
        "head": ("neck", (0, 0, 1.05), (0, 0, 1.30), 0.0),
    }
    for s, y in (("L", 1), ("R", -1)):
        sh = (0, DD * y, bs.SHOULDER)
        B["clavicle." + s] = ("chest", (0, 0.025 * y, 0.985), sh, 0.0)
        B["upperarm." + s] = ("clavicle." + s, sh, (0, DD * y, bs.SHOULDER - UP), 0.058)
        B["forearm." + s] = ("upperarm." + s, (0, DD * y, bs.SHOULDER - UP), (0, DD * y, bs.SHOULDER - UP - FO), 0.055)
        w = bs.SHOULDER - UP - FO
        B["hand." + s] = ("forearm." + s, (0, DD * y, w), (0, DD * y, w - HAND), 0.0)
        B["fingers." + s] = ("hand." + s, (0, DD * y, w - HAND), (0, DD * y, w - HAND - FING), 0.0)
        hz = bs.HIP_Z
        B["thigh." + s] = ("hips", (0, LY * y, hz), (0, LY * y, hz - bs.THIGH), 0.072)
        B["shin." + s] = ("thigh." + s, (0, LY * y, hz - bs.THIGH), (0, LY * y, ANKLE_Z), 0.066)
        ball = (FOOT_DIR[0] * FOOT_LEN, LY * y, ANKLE_Z + FOOT_DIR[2] * FOOT_LEN)
        B["foot." + s] = ("shin." + s, (0, LY * y, ANKLE_Z), ball, 0.0)
        B["toes." + s] = ("foot." + s, ball, (ball[0] + TOE_LEN, LY * y, ball[2]), 0.0)
    return B


BONES = bones()
ORDER = list(BONES.keys())
REST_DIR = {n: (Vector(t) - Vector(h)).normalized() for n, (_p, h, t, _r) in BONES.items()}


def build_armature(name="Stickman"):
    import bpy
    data = bpy.data.armatures.new(name + "Rig")
    arm = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for n, (par, h, t, _r) in BONES.items():
        b = data.edit_bones.new(n)
        b.head = Vector(h)
        b.tail = Vector(t)
        d = REST_DIR[n]
        # local Z = world +Y wherever the bone allows it (all side-plane bending about one axis)
        b.align_roll(Vector((0, 1, 0)) if abs(d.y) < 0.9 else Vector((0, 0, 1)))
        if par:
            b.parent = data.edit_bones[par]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def build_mesh(arm, grey=0.02, far_grey=None, name="StickmanBody"):
    """The ink body. One material unless far_grey is given (previews: far limbs a shade lighter)."""
    import bpy

    def mk(nm, g):
        m = bpy.data.materials.new(nm)
        m.diffuse_color = (g, g, g, 1)
        if m.node_tree is None:
            m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b:
            b.inputs["Base Color"].default_value = (g, g, g, 1)
            b.inputs["Roughness"].default_value = 0.65
        return m

    near = mk(name + "Ink", grey)
    far = mk(name + "Far", far_grey) if far_grey is not None else near
    parts = []

    def part(obj, bone, m):
        obj.data.materials.append(m)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    def sphere(r, at, bone, m, scale=(1, 1, 1), seg=16):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=seg, ring_count=max(8, seg // 2), location=at)
        bpy.context.object.scale = scale
        part(bpy.context.object, bone, m)

    def capsule(h, t, r, bone, m, cap=True):
        h, t = Vector(h), Vector(t)
        bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=(t - h).length, vertices=16, location=(h + t) / 2)
        o = bpy.context.object
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference((t - h).normalized())
        part(o, bone, m)
        if cap:
            sphere(r, t, bone, m, seg=16)

    for n, (par, h, t, r) in BONES.items():
        h, t = Vector(h), Vector(t)
        m = far if n.endswith(".L") else near
        d = REST_DIR[n]
        if n == "head":
            sphere(bs.HEAD_RADIUS, (0, 0, bs.HEAD_CENTER), n, near, seg=24)
        elif n == "spine":
            sphere(0.165, (0, 0, 0.81), n, near, (1.0, 1.05, 1.1), 18)
        elif n == "spine_mid":
            sphere(0.152, (0, 0, 0.875), n, near, (1.0, 1.04, 1.0), 18)
        elif n == "chest":
            sphere(0.155, (0, 0, 0.94), n, near, (1.0, 1.05, 0.97), 18)
        elif n == "neck":
            capsule(h, t, r, n, near)
        elif n.startswith("clavicle"):
            sphere(0.06, t, n, m)                                     # the shoulder ball rides the collarbone
        elif n.startswith("hand"):
            ys = -1 if n.endswith("R") else 1
            sphere(0.047, h + d * 0.026, n, m, (0.82, 1.25, 1.05))     # the palm, wide across the knuckles
            sphere(0.02, h + Vector((0.026, 0.03 * ys, -0.02)), n, m)  # the thumb's root
            sphere(0.018, h + Vector((0.042, 0.032 * ys, -0.042)), n, m)
        elif n.startswith("fingers"):
            for k in range(3):                                         # three knuckle segments: a mitten
                sphere(0.031, h + d * (0.008 + 0.017 * k), n, m, (0.82, 1.45, 1.0), 10)
        elif n.startswith("foot"):
            sphere(0.043, h + Vector((-0.022, 0, -0.016)), n, m, (1.1, 1.0, 0.9))   # the heel
            capsule(h + Vector((0, 0, -0.012)), t, 0.038, n, m)
        elif n.startswith("toes"):
            capsule(h, t, 0.035, n, m)
        elif r > 0:
            capsule(h, t, r, n, m)
            # a ball at the root of every limb segment too, a touch smaller, so a bent knee or
            # elbow is round on both sides and no cylinder's rim ever shows
            sphere(r * (1.0 if n.startswith(("upperarm", "thigh")) else 0.97), h, n, m, seg=14)
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = name
    bpy.ops.object.shade_smooth()
    body.parent = arm
    body.modifiers.new("Armature", "ARMATURE").object = arm
    return body


# ---------------------------------------------------------------- V3 → V4 (the old clips)
Y = Vector((0, 1, 0))


def _slerp(a, b, t):
    return a.slerp(b, t)


def curl_q(deg):
    """Bend about the side-plane axis (as the rig's angles do): + folds forward/under."""
    return Quaternion(Y, -math.radians(deg))


def transfer_frame(qv3, hips_pos, ankle, fist=True):
    """qv3: world rotation deltas of the V3 bones (from their rest), hips_pos: the hips' head,
    ankle: {"L": Vector, "R": Vector} the V3 shins' tips (world). Returns V4 world deltas."""
    q = {}
    q["hips"] = qv3["hips"]
    q["spine"] = _slerp(qv3["hips"], qv3["spine"], 0.8)
    q["spine_mid"] = _slerp(qv3["spine"], qv3["chest"], 0.2)
    q["chest"] = qv3["chest"]
    q["neck"] = _slerp(qv3["chest"], qv3["head"], 0.45)
    q["head"] = qv3["head"]
    for s in ("L", "R"):
        ua, fa = qv3["upperarm." + s], qv3["forearm." + s]
        q["upperarm." + s], q["forearm." + s] = ua, fa
        q["hand." + s] = qv3.get("hand." + s, fa)
        # the shoulder follows the arm: up over the shoulder = a shrug; driven out straight = a reach
        dua = ua @ Vector((0, 0, -1))
        dfa = fa @ Vector((0, 0, -1))
        lift = max(0.0, min(1.0, (dua.z + 0.05) / 0.9))                # 0 at horizontal, 1 straight up
        straight = max(0.0, (dua.dot(dfa) - 0.86) / 0.14)
        reach = max(0.0, min(1.0, dua.x * 1.2)) * straight            # an arm thrown out forward, straight
        chest = qv3["chest"]
        side = 1 if s == "L" else -1
        rest = Vector((0, side, 0.12)).normalized()
        want = chest @ (rest + Vector((0.0, 0.0, 0.55 * lift)) + Vector((0.95 * reach, 0.0, 0.08 * reach)))
        want.normalize()
        q["clavicle." + s] = REST_DIR["clavicle." + s].rotation_difference(want)
        q["fingers." + s] = q["hand." + s] @ curl_q(165 if fist else 70)
        # the foot rolls with the ankle's height
        sh = qv3["shin." + s]
        q["thigh." + s], q["shin." + s] = qv3["thigh." + s], sh
        a = ankle[s]
        lifted = a.z - ANKLE_Z
        flat = Quaternion()                                            # the rest foot: flat on the floor, pointing forward
        # a little yaw from the shin, so a turned leg turns its foot
        sd = sh @ Vector((0, 0, -1))
        yaw = math.atan2(sd.y, max(1e-4, abs(sd.x) + 0.6)) * 0.5 if abs(sd.x) + abs(sd.y) > 0.05 else 0.0
        yq = Quaternion((0, 0, 1), yaw)
        if lifted <= 0.012:
            foot = yq @ flat
            toes = foot
        else:
            # heel up, the ball still on the floor
            pitch = math.asin(max(-1.0, min(1.0, (a.z - BALL_Z) / FOOT_LEN))) - math.asin((ANKLE_Z - BALL_Z) / FOOT_LEN)
            pitch = max(0.0, min(math.radians(70), pitch))
            ball_on = yq @ Quaternion(Y, pitch)                        # + about Y tips the toes down
            # off the floor: the foot points with the shin (as at rest relative to it)
            air = sh @ Quaternion(Y, math.radians(25))
            k = max(0.0, min(1.0, (lifted - 0.08) / 0.12))
            foot = _slerp(ball_on, air, k)
            toes = _slerp(yq, air, k)
        q["foot." + s], q["toes." + s] = foot, toes
    return q


def to_local(qw, rest_q):
    """World deltas → pose-bone local rotations."""
    out = {}
    for n in ORDER:
        par = BONES[n][0]
        rel = qw[n] if par is None else qw[par].inverted() @ qw[n]
        out[n] = rest_q[n].inverted() @ rel @ rest_q[n]
    return out
