"""Chapter 5: THE ROAD (docs/story/story_v2.md, CHAPTER 5). The drive up Kessel Hill at night, the
storm building. No fighting: a cinematic set.

  THE HILL ROAD   five legs climbing the hill's face in hairpins (x -260..-6, y -72..0, z -67..0):
                  rock cuttings on the uphill side, crash barriers on the drop, reflector posts,
                  chevron boards on every bend, the "KESSEL HILL 2" sign at the foot, storm-bent pines,
                  the valley's city lights 110 m below, low cloud
  THE TOP         the last leg runs straight into the fortress's cliff road (z 0, as build_fortress_set's
                  F1, x -8..52): the floodlit spot where he stops and gets out (x -9), the gang's truck,
                  the road's floodlight, the rock stair; the fortress above (its own builders, so it is
                  the same fortress), lit windows, floodlights on every wall, men on the battlements
  THE CAR         RoadCar: the Handler's black saloon (the Driver's model, detailed) with its lamps lit;
                  CarInterior: the cabin from inside (right-hand drive): glowing dials, the wheel, gear
                  stick, handbrake, stitched seats, the empty passenger seat, mirror, visors, door cards;
                  WiperL / WiperR (separate, their origins on their spindles: they sweep);
                  Windscreen (not baked: the game's rain runs on it)

Two bakes of every lit part: the storm night, and lit by a strike (_Lightning) for LightningSwap.
The car path (the left lane up the hairpins to the stop) goes to Assets/Scripts/Level/RoadPlan.cs.

    /home/user/.venv-blender/bin/python tools/blender/build_road_set.py                 # bake + export
    /home/user/.venv-blender/bin/python tools/blender/build_road_set.py --preview       # shots, no bake
    /home/user/.venv-blender/bin/python tools/blender/build_road_set.py --lightning-preview
    /home/user/.venv-blender/bin/python tools/blender/build_road_set.py --write-plan    # RoadPlan.cs only

Coordinates: Blender X = Unity x, Blender Y = Unity z (depth), Blender Z = Unity y (up). Metres."""
import math
import os
import random
import sys

import bpy  # noqa: E402
from mathutils import Matrix, Vector, kdtree, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402
import set_kit as K  # noqa: E402
import build_fortress_set as FB  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Road")
PLAN = os.path.join(ROOT, "Assets", "Scripts", "Level", "RoadPlan.cs")
rnd = random.Random(505)

# ---------------------------------------------------------------- the road's line
R_HP = 9.0                                  # hairpin radius (centreline)
LEG_Y = [-72.0, -54.0, -36.0, -18.0, 0.0]   # the five legs, bottom to top
XL, XR = -140.0, -20.0                      # hairpin centres: west and east
X_START, X_FOOT, X_TOP, X_END = -262.0, -200.0, -50.0, 52.0
Z_FOOT = -62.0                              # the road at the foot of the climb (x -200)
UP = FB.UP                                  # the fortress's level above the cliff road
X_STOP, Y_STOP = -9.0, 1.2                  # where the car stops (its middle, on the ground)
LANE = 1.6                                  # the car keeps left: this far left of the centre line
FLAT = 4.6                                  # the road and its shoulders, each side of the centre line
CAR_BAKE = Vector((900.0, 900.0, 0.0))      # the car is baked out here, alone, then parked at the stop
PATH_FROM_X = -240.0                        # the car's path starts here (on the first leg)
X_INT = -30.0                               # the interior is filmed here, parked on the top's level, facing +x:
INTERIOR_AT = Vector((X_INT, LANE, 0.0))    # the last of the climb ahead, the floodlight, the fortress filling the screen


def _centre_xy(step=1.0):
    pts = []

    def line(a, b):
        n = max(1, int(math.dist(a, b) / step))
        for i in range(n):
            t = i / n
            pts.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))

    def arc(c, a0, a1):
        n = max(2, int(abs(a1 - a0) * R_HP / step))
        for i in range(n):
            a = a0 + (a1 - a0) * i / n
            pts.append((c[0] + R_HP * math.cos(a), c[1] + R_HP * math.sin(a)))
    y1, y2, y3, y4, y5 = LEG_Y
    line((X_START, y1), (XR, y1))
    arc((XR, y1 + R_HP), -math.pi / 2, math.pi / 2)
    line((XR, y2), (XL, y2))
    arc((XL, y2 + R_HP), -math.pi / 2, -3 * math.pi / 2)
    line((XL, y3), (XR, y3))
    arc((XR, y3 + R_HP), -math.pi / 2, math.pi / 2)
    line((XR, y4), (XL, y4))
    arc((XL, y4 + R_HP), -math.pi / 2, -3 * math.pi / 2)
    line((XL, y5), (X_END, y5))
    pts.append((X_END, y5))
    return pts


def _arclen(pts):
    s = [0.0]
    for a, b in zip(pts, pts[1:]):
        s.append(s[-1] + math.dist(a, b))
    return s


CXY = _centre_xy()
CS = _arclen(CXY)
S_FOOT = min(range(len(CXY)), key=lambda i: abs(CXY[i][0] - X_FOOT) + abs(CXY[i][1] - LEG_Y[0]))
S_FOOT = CS[S_FOOT]
S_TOP = CS[max(i for i in range(len(CXY)) if abs(CXY[i][1]) < 1e-6 and CXY[i][0] <= X_TOP)]
GRADE = -Z_FOOT / (S_TOP - S_FOOT)
VC = 16.0                                   # the vertical curve onto the top's level


def z_of_s(s):
    """The road's height along its length: one steady grade, easing onto the level at the top."""
    a = S_TOP - VC
    if s <= a:
        return Z_FOOT + GRADE * (s - S_FOOT)
    if s >= S_TOP + VC:
        return 0.0
    return Z_FOOT + GRADE * (s - S_FOOT) - GRADE * (s - a) ** 2 / (4 * VC)


CZ = [z_of_s(s) for s in CS]


def frames_at(idx_from=0, idx_to=None, every=1):
    """(pos, left, up, s, i) along the centre line."""
    idx_to = len(CXY) - 1 if idx_to is None else idx_to
    out = []
    for i in range(idx_from, idx_to + 1, every):
        j0, j1 = max(0, i - 1), min(len(CXY) - 1, i + 1)
        t = Vector((CXY[j1][0] - CXY[j0][0], CXY[j1][1] - CXY[j0][1], 0)).normalized()
        left = Vector((-t.y, t.x, 0))
        out.append((Vector((CXY[i][0], CXY[i][1], CZ[i])), left, Vector((0, 0, 1)), CS[i], i))
    return out


def leg_z(k, x):
    """The height of leg k (0..4) where it passes x (its straight line extended)."""
    s0 = {0: S_FOOT + (x - X_FOOT)}
    y = LEG_Y
    # arc length at the start of each leg
    i_leg = []
    for kk in range(5):
        idx = [i for i in range(len(CXY)) if abs(CXY[i][1] - y[kk]) < 1e-6]
        i_leg.append(idx)
    i0 = i_leg[k][0]
    s_start, x_start = CS[i0], CXY[i0][0]
    heading = 1.0 if k % 2 == 0 else -1.0
    return z_of_s(s_start + (x - x_start) * heading)


_LEG_CACHE = {}


def leg_zc(k, x):
    key = (k, round(x, 1))
    if key not in _LEG_CACHE:
        _LEG_CACHE[key] = leg_z(k, x)
    return _LEG_CACHE[key]


# ---------------------------------------------------------------- the hill's shape

def _rock_rise(y):
    return 0.0 if y < 2.4 else (y - 2.4) ** 1.35 * 0.9


def natural(x, y):
    """The hillside before the road was cut: interpolated between the legs on the hairpin face, a
    plain slope beyond it, the cliff behind the top road (as the fortress set's), the valley floor,
    and the fortress's level from x 52."""
    simple = (-0.5 + 0.8 * y) if y < 0 else _rock_rise(y)
    if y < LEG_Y[0]:
        band = leg_zc(0, x) + (y - LEG_Y[0]) * 0.95
    elif y >= 0:
        band = leg_zc(4, x) + _rock_rise(y)
    else:
        k = int((y - LEG_Y[0]) // 18.0)
        k = min(3, max(0, k))
        t = (y - LEG_Y[k]) / 18.0
        za, zb = leg_zc(k, x), leg_zc(k + 1, x)
        band = za + (zb - za) * (t + 0.18 * math.sin(math.pi * t))
    w = 0.0
    if x > XR + R_HP:
        w = min(1.0, (x - (XR + R_HP)) / 26.0)
    elif x < XL - R_HP:
        w = min(1.0, ((XL - R_HP) - x) / 26.0)
    if y < LEG_Y[0] or y >= 0:
        w = max(w, 0.0)
    z = band + (simple - band) * w
    # the fortress's level, a cliff below its front
    if x > 44.0:
        top = UP - 0.35 if y < 12.0 else UP + 0.4 + (y - 12.0) * 0.9
        if y < -5.0:
            top = UP - 0.35 + (y + 5.0) * 1.7
        f = min(1.0, (x - 44.0) / 8.0)
        z = max(z, z + (top - z) * f) if y > -5 else max(z, top * f + z * (1 - f))
    z += 1.3 * noise.noise(Vector((x * 0.045, y * 0.045, 0.7))) + 0.35 * noise.noise(Vector((x * 0.21, y * 0.21, 2.3)))
    # the valley floor
    floor = -110.0 + 1.5 * noise.noise(Vector((x * 0.02, y * 0.02, 5.0)))
    if z < floor + 6.0:
        z = floor + (6.0 * math.exp((z - floor - 6.0) / 6.0))
    return z


KD = kdtree.KDTree(len(CXY))
for _i, (_x, _y) in enumerate(CXY):
    KD.insert((_x, _y, 0.0), _i)
KD.balance()


def terrain_z(x, y):
    """The hill with the road cut into it: flat under the road and its shoulders, cuttings up from
    the gutter on the high side, an embankment down on the low side."""
    z = natural(x, y)
    near = KD.find_range((x, y, 0.0), 22.0)
    if not near:
        return z
    hi, lo, best = 1e9, -1e9, None
    for co, i, d in near:
        zr = CZ[i]
        if d < FLAT:
            if best is None or d < best[0]:
                best = (d, zr)
            continue
        t = d - FLAT
        hi = min(hi, zr + 0.25 + t * 2.3)
        lo = max(lo, zr - 0.35 - t * 1.25)
    if best is not None:
        return best[1] - 0.14
    z = max(z, lo)
    z = min(z, hi)
    return z


# ---------------------------------------------------------------- materials

def mats():
    M = FB.M_()
    K.car_materials(M)
    S.TONE.update({"RoadLine": 0.62, "Barrier": 0.42, "BarrierPost": 0.12, "Delineator": 0.6, "SignWhite": 0.72,
                   "SignBlack": 0.015, "Reflector": 0.9, "HutConcrete": 0.3})
    M["asphalt"] = K.mottled("RoadAsphalt", 0.055, 0.28, scale=0.35, var=0.45)
    M["gravel"] = K.mottled("RoadGravel", 0.13, 0.95, scale=4.0, var=0.5)
    M["heath"] = K.mottled("HillHeath", 0.075, 0.95, scale=0.09, var=0.65)
    M["scree"] = D.rock("HillScree", 0.16, "xy")
    M["cut_x"] = D.rock("HillCut", 0.22, "xz")
    M["cut_y"] = D.rock("HillCut", 0.22, "yz")
    M["line"] = S.mat("RoadLine", (0.62, 0.62, 0.62), 0.5)
    M["barrier"] = S.mat("Barrier", (0.42, 0.42, 0.42), 0.32, metal=0.7)
    M["barrier_post"] = S.mat("BarrierPost", (0.12, 0.12, 0.12), 0.5, metal=0.5)
    M["delin"] = S.mat("Delineator", (0.6, 0.6, 0.6), 0.6)
    M["sign_white"] = S.mat("SignWhite", (0.72, 0.72, 0.72), 0.5)
    M["sign_black"] = S.mat("SignBlack", (0.015, 0.015, 0.015), 0.6)
    M["reflector"] = S.mat("Reflector", (1, 1, 1), 0.3, emit=(1, 1, 1), strength=6.0)
    M["needles"] = D.flat("PineNeedles", 0.05, rough=0.95)
    M["bark"] = D.flat("PineBark", 0.07)
    M["cloud"] = K.mottled("StormCloud", 0.11, 1.0, scale=0.04, var=0.55)
    M["cloud_low"] = K.mottled("ValleyCloud", 0.16, 1.0, scale=0.05, var=0.5)
    M["sky"] = K.grad_sky("RoadSkyGrad", -140.0, 160.0, [(0.0, 0.05), (0.18, 0.03), (0.45, 0.012), (1.0, 0.004)], 1.0)
    M["city_light"] = S.mat("CityLight", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=14.0)
    M["flood_lens"] = D.flat("RoadFlood", 1.0, emit=40.0)
    M["sentry"] = D.flat("Sentry", 0.01, rough=0.9)
    M["bake_ground"] = D.flat("BakeGround", 0.06)
    return M


# ---------------------------------------------------------------- RoadHill: the hill, the cuttings

def _axis(*a):
    return sorted(set(round(v, 3) for v in a))


def build_hill(coll, M):
    g = S.Group("RoadHill")
    xs = list(range(-320, -200, 10)) + [x * 2.5 for x in range(-80, 13)] + list(range(40, 201, 10))
    ys = list(range(-310, -110, 10)) + [y * 2.5 for y in range(-44, 13)] + list(range(35, 121, 6))
    xs, ys = _axis(*xs), _axis(*ys)
    Z = [[terrain_z(x, y) for x in xs] for y in ys]
    heath, scree = [], []
    verts = []
    for j, y in enumerate(ys):
        for i, x in enumerate(xs):
            verts.append((x, y, Z[j][i]))
    nx = len(xs)
    for j in range(len(ys) - 1):
        for i in range(nx - 1):
            a = j * nx + i
            f = (a, a + 1, a + nx + 1, a + nx)
            dz = max(abs(Z[j][i] - Z[j][i + 1]) / (xs[i + 1] - xs[i]), abs(Z[j][i] - Z[j + 1][i]) / (ys[j + 1] - ys[j]))
            (scree if dz > 0.9 else heath).append(f)
    up = lambda c, n: Vector((0, 0, 1))  # noqa: E731
    K.raw(g, verts, heath, M["heath"], hint=up)
    K.raw(g, verts, scree, M["scree"], hint=up)
    # the cuttings: a rock face up from the gutter on the high side wherever the road is cut in
    fr = frames_at(every=2)
    run, run_mat = [], None

    def flush():
        if len(run) < 2:
            return
        verts, faces = [], []
        nlev = len(run[0])
        for r in run:
            verts.extend(r)
        for i in range(len(run) - 1):
            for k in range(nlev - 1):
                a = i * nlev + k
                faces.append((a, a + 1, a + nlev + 1, a + nlev))
        cen = [f[0] for f in fr]
        K.raw(g, verts, faces, run_mat, hint=lambda c, n: _toward_road(c))
    for (p, left, up_, s, i) in fr:
        if p.x > -6.0 or s < 4.0:
            if run:
                flush()
                run = []
            continue
        hs = 1.0 if natural(*(p + left * 7.0).xy) > natural(*(p - left * 7.0).xy) else -1.0
        side = left * hs
        H = natural(*(p + side * 7.5).xy) - p.z - 0.4
        H = min(12.0, H)
        mat_ = M["cut_x"] if abs(left.y) > 0.7 else M["cut_y"]
        if H < 0.9 or (run and mat_ is not run_mat):
            if run:
                flush()
                run = []
            if H < 0.9:
                continue
        run_mat = mat_
        col = []
        for k in range(9):
            h = H * k / 8
            lat = FLAT - 0.3 + h / 3.6
            lat += 0.32 * noise.noise(Vector((p.x * 0.13, p.y * 0.13, h * 1.3))) + 0.14 * noise.noise(Vector((p.x * 0.5, p.y * 0.5, h * 4.0)))
            col.append(tuple(p + side * lat + Vector((0, 0, h - 0.12))))
        col.append(tuple(p + side * (FLAT + (H - 0.25) / 2.3 + 0.6) + Vector((0, 0, H - 0.15))))
        run.append(col)
    flush()
    # rubble at the foot of the cuttings, outcrops on the face
    for (p, left, up_, s, i) in frames_at(every=9):
        if p.x > -6.0 or rnd.random() < 0.35:
            continue
        hs = 1.0 if natural(*(p + left * 7.0).xy) > natural(*(p - left * 7.0).xy) else -1.0
        c = p + left * hs * (FLAT + rnd.uniform(-0.1, 0.4))
        K.ball(g, (c.x, c.y, p.z + 0.1), rnd.uniform(0.35, 0.8), M["cut_x"], sub=1, squash=(1.2, 1.0, 0.6), jitter=0.35, seed=rnd.uniform(0, 50))
    for _ in range(70):
        x, y = rnd.uniform(-230, 10), rnd.uniform(-100, 24)
        if KD.find((x, y, 0))[2] < FLAT + 6:
            continue
        r = rnd.uniform(1.0, 3.4)
        K.ball(g, (x, y, terrain_z(x, y) - r * 0.25), r, M["scree"], sub=1, squash=(1.3, 1.0, 0.6), jitter=0.4, seed=rnd.uniform(0, 50))
    return g.finish(coll)


def _toward_road(c):
    co, i, d = KD.find((c.x, c.y, 0.0))
    return Vector((CXY[i][0] - c.x, CXY[i][1] - c.y, 0.0))


# ---------------------------------------------------------------- RoadWay: the road and its furniture

def _edges(p):
    """The road's right and left edges (lateral offsets) here: 6.4 m wide on the hill, narrowing to
    the fortress cliff road's (y -2.8..2.4) over the top."""
    t = min(1.0, max(0.0, (p.x + 30.0) / 22.0)) if abs(p.y) < 1.0 else 0.0
    return -3.2 + 0.4 * t, 3.2 - 0.8 * t


def downhill(p, left):
    return -1.0 if natural(*(p + left * 7.0).xy) > natural(*(p - left * 7.0).xy) else 1.0


def build_way(coll, M):
    g = S.Group("RoadWay")
    up = lambda c, n: Vector((0, 0, 1))  # noqa: E731
    fr = frames_at(every=1)
    # asphalt (crowned), shoulders, a gutter on the high side
    rows_a, rows_r, rows_l = [], [], []
    for (p, left, u, s, i) in fr[::2] + [fr[-1]]:
        r, l = _edges(p)
        rows_a.append([tuple(p + left * o + Vector((0, 0, h))) for o, h in ((r, 0.0), (r * 0.5, 0.012), (0, 0.022), (l * 0.5, 0.012), (l, 0.0))])
        rows_r.append([tuple(p + left * o + Vector((0, 0, h))) for o, h in ((-FLAT, -0.12), (r - 0.25, -0.03), (r, 0.0))])
        rows_l.append([tuple(p + left * o + Vector((0, 0, h))) for o, h in ((l, 0.0), (l + 0.25, -0.03), (FLAT, -0.12))])

    def grid(rows, m):
        verts, faces = [], []
        n = len(rows[0])
        for r in rows:
            verts.extend(r)
        for i in range(len(rows) - 1):
            for j in range(n - 1):
                a = i * n + j
                faces.append((a, a + 1, a + n + 1, a + n))
        K.raw(g, verts, faces, m, hint=up)
    grid(rows_a, M["asphalt"])
    grid(rows_r, M["gravel"])
    grid(rows_l, M["gravel"])
    # markings: solid edge lines, the centre line dashed on the legs and double through the bends
    for side_i in (0, 1):
        rows = []
        for (p, left, u, s, i) in fr[::2] + [fr[-1]]:
            e = _edges(p)[side_i]
            o = e + (0.3 if side_i == 0 else -0.3)
            rows.append([tuple(p + left * (o + d) + Vector((0, 0, 0.012))) for d in (-0.06, 0.06)])
        grid(rows, M["line"])
    in_bend = lambda p: (p.x > XR - 1 or p.x < XL + 1) and p.y < -1  # noqa: E731
    for (p, left, u, s, i) in fr:
        if p.x > X_STOP + 6:
            break
        bend = in_bend(p)
        if bend:
            for off in (-0.09, 0.09):
                a = p + left * off
                g.box((a.x, a.y, p.z + 0.024), (0.5, 0.08, 0.004), M["line"], rot=(0, 0, math.degrees(math.atan2(left.x, -left.y))))
        elif s % 9.0 < 1.0:
            g.box((p.x, p.y, p.z + 0.024), (3.0, 0.12, 0.004), M["line"], rot=(0, 0, math.degrees(math.atan2(left.x, -left.y))))
    # SLOW painted before every bend, in the climbing lane
    for k in range(4):
        x_b = XR - 24.0 if k % 2 == 0 else XL + 24.0
        heading = 1.0 if k % 2 == 0 else -1.0
        T, L = Vector((heading, 0, 0)), Vector((0, heading, 0))
        y = LEG_Y[k]
        z = leg_z(k, x_b)
        Mt = K.frame_m((x_b, y + L.y * LANE, z + 0.026), -L, T)
        K.text(g, "SLOW", Mt @ Matrix.Diagonal((1.0, 2.2, 1.0, 1.0)), 1.1, M["line"], extrude=0.002)
    # crash barriers on the drop side: posts every 2 m, a W-beam rail, the ends turned down
    runs, cur = [], []
    for (p, left, u, s, i) in fr:
        if p.x > -7.0 or p.x < -235.0:
            if cur:
                runs.append(cur)
                cur = []
            continue
        cur.append((p, left, downhill(p, left)))
    if cur:
        runs.append(cur)
    for run in runs:
        rows_f = []
        prof = [(0.0, 0.46), (0.05, 0.51), (0.012, 0.565), (0.05, 0.62), (0.0, 0.675), (-0.025, 0.675), (-0.025, 0.46)]
        verts, faces = [], []
        for k, (p, left, ds) in enumerate(run):
            lat = ds * (FLAT - 0.15)
            inward = -left * ds                                                           # towards the road
            base = p + left * lat
            for (o, h) in prof:
                verts.append(tuple(base + inward * o + Vector((0, 0, h - 0.12))))
            if k % 2 == 0:
                pb = base - inward * 0.12 + Vector((0, 0, -0.12))
                g.box((pb.x, pb.y, pb.z + 0.38), (0.07, 0.12, 0.8), M["barrier_post"], rot=(0, 0, math.degrees(math.atan2(left.y, left.x))))
                ps = base - inward * 0.05 + Vector((0, 0, 0.44))
                g.box((ps.x, ps.y, ps.z), (0.06, 0.12, 0.28), M["barrier_post"], rot=(0, 0, math.degrees(math.atan2(left.y, left.x))))
                if k % 8 == 0:
                    pr = base + inward * 0.06 + Vector((0, 0, 0.7))
                    g.box((pr.x, pr.y, pr.z), (0.09, 0.09, 0.06), M["reflector"], rot=(0, 0, math.degrees(math.atan2(left.y, left.x))))
        n = len(prof)
        for k in range(len(run) - 1):
            for j in range(n):
                a, b = k * n + j, k * n + (j + 1) % n
                faces.append((a, b, b + n, a + n))
        faces.append(tuple(range(n)))
        faces.append(tuple((len(run) - 1) * n + j for j in range(n)))
        K.raw(g, verts, faces, M["barrier"], hint=lambda c, nr, _r=run: c - _nearest_base(_r, c))
    # reflector posts on the high side, every 24 m (a white post, a black band, the glass)
    for (p, left, u, s, i) in frames_at(every=24):
        if p.x > -4 or p.x < -250:
            continue
        ds = -downhill(p, left)
        b = p + left * ds * (FLAT - 0.55)
        yaw = math.degrees(math.atan2(left.y, left.x))
        g.box((b.x, b.y, p.z + 0.5), (0.12, 0.12, 1.05), M["delin"], rot=(0, 0, yaw))
        g.box((b.x, b.y, p.z + 0.85), (0.125, 0.125, 0.16), M["sign_black"], rot=(0, 0, yaw))
        for sgn in (-1, 1):
            q = b + left.cross(Vector((0, 0, 1))) * sgn * 0.063
            g.box((q.x, q.y, p.z + 0.85), (0.012, 0.06, 0.1), M["reflector"], rot=(0, 0, yaw))
    # chevron boards round the outside of every hairpin, facing the bend's centre
    for k, (cx, cy, a0, a1) in enumerate(((XR, LEG_Y[0] + R_HP, -1.2, 1.2), (XL, LEG_Y[1] + R_HP, math.pi - 1.2, math.pi + 1.2),
                                          (XR, LEG_Y[2] + R_HP, -1.2, 1.2), (XL, LEG_Y[3] + R_HP, math.pi - 1.2, math.pi + 1.2))):
        turn_left = k % 2 == 0
        zc = leg_z(k, cx) + 0.5 * (leg_z(k + 1, cx) - leg_z(k, cx))
        for j in range(6):
            a = a0 + (a1 - a0) * j / 5
            r = R_HP + FLAT + 0.9
            px, py = cx + math.cos(a) * r, cy + math.sin(a) * r
            pz = terrain_z(px, py)
            pz = max(pz, zc - 0.5) if pz < zc - 2 else pz
            inward = Vector((-math.cos(a), -math.sin(a), 0))
            tang = Vector((-math.sin(a), math.cos(a), 0)) * (1 if turn_left else -1)
            ctr = Vector((px, py, zc + 1.45))
            for sgn in (-1, 1):
                q = ctr + tang * sgn * 0.28
                g.box((q.x, q.y, (zc + 1.45 + pz) / 2 - 0.2), (0.07, 0.07, zc + 1.45 - pz + 0.4), M["barrier_post"])
            Mb = K.frame_m(ctr + inward * 0.04, tang, (0, 0, 1))
            yaw = math.degrees(math.atan2(tang.y, tang.x))
            g.box(tuple(ctr + inward * 0.02), (0.8, 0.04, 0.95), M["sign_black"], rot=(0, 0, yaw))
            for (dz, sgn) in ((0.18, 1), (-0.18, -1)):
                q = ctr + inward * 0.05 + Vector((0, 0, dz * 0.5))
                g.box(tuple(q), (0.42, 0.012, 0.11), M["sign_white"], rot=(0, -sgn * 38 * (1 if turn_left else -1), yaw) if False else None)
            for sgn in (-1, 1):
                q = ctr + inward * 0.05 + Vector((0, 0, sgn * 0.13))
                Mb2 = Matrix.Translation(q) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(sgn * 38), 4, "Y")
                g.box(tuple(q), (0.40, 0.012, 0.11), M["sign_white"], rot=Mb2.to_3x3().to_4x4())
    # the signs at the foot: KESSEL HILL 2, and the warning of bends ahead
    sx, sy = -214.0, LEG_Y[0] + FLAT + 0.6
    sz = leg_z(0, sx)
    for dy in (-0.85, 0.85):
        g.cyl((sx, sy + dy, sz + 1.0), 0.04, 2.6, M["barrier_post"], n=8)
    g.box((sx + 0.03, sy, sz + 1.95), (0.04, 2.3, 0.95), M["sign_white"])
    g.box((sx - 0.0, sy, sz + 1.95), (0.03, 2.38, 1.03), M["sign_black"])
    g.box((sx - 0.012, sy, sz + 1.95), (0.02, 2.22, 0.87), M["sign_white"])
    Mt = K.frame_m((sx - 0.03, sy + 0.1, sz + 2.0), (0, -1, 0), (0, 0, 1))
    K.text(g, "KESSEL HILL", Mt, 0.36, M["sign_black"], extrude=0.006)
    Mt = K.frame_m((sx - 0.03, sy - 0.88, sz + 2.0), (0, -1, 0), (0, 0, 1))
    K.text(g, "2", Mt, 0.5, M["sign_black"], extrude=0.006)
    g.box((sx - 0.03, sy + 0.1, sz + 1.66), (0.008, 1.6, 0.03), M["sign_black"])
    # the triangle: hairpins ahead
    tx, ty = -192.0, LEG_Y[0] + FLAT + 0.5
    tz = leg_z(0, tx)
    g.cyl((tx, ty, tz + 1.0), 0.04, 2.2, M["barrier_post"], n=8)
    tri = [(-0.48, 0.0), (0.48, 0.0), (0.0, 0.84)]
    tg = S.Group("tmp")
    tg.prism([(a, b) for a, b in tri], -0.015, 0.015, M["sign_black"])
    tg.prism([(a * 0.8, b * 0.8 + 0.07) for a, b in tri], -0.025, 0.025, M["sign_white"])
    for (a, b, c) in (((-0.12, 0.2), (0.0, 0.32), (0.06, 0.0)), ((0.0, 0.32), (0.08, 0.44), (0.06, 0.0)), ((0.08, 0.44), (-0.04, 0.56), (0.06, 0.0))):
        tg.beam((a[0], 0.03, a[1]), (b[0], 0.03, b[1]), 0.05, 0.01, M["sign_black"])
    Lt = K.Local(g, Matrix.Translation((tx - 0.06, ty, tz + 2.0)) @ Matrix.Rotation(math.radians(90), 4, "Z"))
    Lt.absorb(tg)
    return g.finish(coll)


def _nearest_base(run, c):
    best = min(run, key=lambda r: (r[0] - c).length_squared)
    p, left, ds = best
    return p + left * ds * (FLAT - 0.15) + Vector((0, 0, 0.45))


# ---------------------------------------------------------------- RoadTrees: storm-bent pines

def bent_pine(g, x, y, z, h, M, dead=False):
    """A pine bent by years of wind off the valley: the trunk bows, the tiers stream to one side."""
    wind = Vector((0.55, 0.85, 0)).normalized()
    lean = rnd.uniform(0.08, 0.22)
    pts = [Vector((x, y, z - 0.3))]
    for k in range(1, 4):
        t = k / 3
        pts.append(Vector((x, y, z)) + wind * (lean * h * t * t) + Vector((0, 0, h * t)))
    for a, b in zip(pts, pts[1:]):
        g.beam(a, b, 0.22 * h / 8 * (1.3 - (a.z - z) / h), 0.22 * h / 8 * (1.3 - (a.z - z) / h), M["bark"])
    if dead:
        for k in range(3):
            a = pts[1] + (pts[3] - pts[1]) * (k / 3)
            d = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), 0.4)).normalized()
            g.beam(a, a + d * h * 0.18, 0.05, 0.05, M["bark"])
        return
    tiers = 4
    for k in range(tiers):
        t = k / (tiers - 1)
        c = pts[0] + (pts[3] - pts[0]) * (0.35 + 0.62 * t) + wind * (0.9 + 0.5 * t) * h / 9
        r = (1.7 - 1.25 * t) * h / 7
        g.cyl(tuple(c), r, h * 0.24, M["needles"], n=7, r2=r * 0.15, rot=(rnd.uniform(-8, 8), rnd.uniform(-8, 8), rnd.uniform(0, 60)))


def build_trees(coll, M):
    g = S.Group("RoadTrees")
    n = 0
    tries = 0
    while n < 230 and tries < 4000:
        tries += 1
        x, y = rnd.uniform(-250, 40), rnd.uniform(-118, 34)
        if x > 40 and y > -6:
            continue
        d = KD.find((x, y, 0))[2]
        if d < FLAT + 3.2:
            continue
        if noise.noise(Vector((x * 0.03, y * 0.03, 9.0))) < -0.15 and d > 14:
            continue                                                                  # clearings
        z = terrain_z(x, y)
        bent_pine(g, x, y, z, rnd.uniform(6.0, 12.0), M, dead=rnd.random() < 0.06)
        n += 1
    return g.finish(coll)


# ---------------------------------------------------------------- RoadTop: the stop, the cliff road

def build_top(coll, M):
    """The fortress's cliff road (as build_fortress_set's F1) and the floodlit spot short of it where
    he stops: the road's floodlight, the truck, the milestone, the rock stair to the gate."""
    g = S.Group("RoadTop")
    rnd2 = random.Random(71)
    for x in range(-6, 52, 3):                                                         # the cliff's face
        K.ball(g, (x + rnd2.uniform(-1, 1), 3.9 + rnd2.uniform(0, 1), rnd2.uniform(0.3, 2.6)), rnd2.uniform(0.9, 1.6), M["rock"], sub=2, squash=(1, 1, 0.7), jitter=0.32, seed=x)
    for x in range(-60, -6, 3):
        K.ball(g, (x + rnd2.uniform(-1, 1), 4.6 + rnd2.uniform(0, 1.5), rnd2.uniform(0.0, 2.0)), rnd2.uniform(0.8, 1.5), M["rock"], sub=1, squash=(1, 1, 0.7), jitter=0.35, seed=x)
    for x in range(-6, 46, 2):                                                         # the low wall and its rail
        g.box((x, -2.95, 0.4), (0.18, 0.18, 0.8), M["stone_dark"])
        g.box((x, -2.95, 0.82), (0.24, 0.24, 0.06), M["stone"])
    g.beam((-6, -2.95, 0.74), (45, -2.95, 0.74), 0.08, 0.06, M["iron"])
    tx = 28.0                                                                          # the gang's truck
    g.box((tx, 1.6, 1.3), (5.5, 2.2, 2.2), M["truck"])
    g.box((tx + 3.4, 1.6, 0.95), (1.4, 2.1, 1.5), M["truck"])
    g.box((tx + 3.9, 0.52, 1.25), (0.5, 0.02, 0.5), M["glass"])
    for k in range(10):
        g.box((tx - 2.5 + k * 0.55, 0.49, 1.3), (0.04, 0.02, 2.0), M["iron"])
    for wx in (tx - 1.8, tx + 3.0):
        for wy in (0.5, 2.7):
            g.cyl((wx, wy, 0.42), 0.42, 0.3, M["tyre"], rot=(90, 0, 0), n=16)
            g.cyl((wx, wy + (-0.16 if wy < 1 else 0.16), 0.42), 0.22, 0.02, M["iron"], rot=(90, 0, 0), n=12)
    for (fx, fy, fz) in ((38.0, 2.2, 6.0), (-6.4, 3.6, 6.6)):                         # floodlights on poles
        g.cyl((fx, fy, fz / 2), 0.08, fz, M["iron"], n=8)
        g.cyl((fx, fy, 0.15), 0.18, 0.3, M["stone_dark"], n=10)
        g.box((fx + 0.12, fy, fz * 0.35), (0.2, 0.16, 0.3), M["iron"])                 # the junction box
        g.beam((fx + 0.12, fy, fz * 0.35 - 0.15), (fx + 0.06, fy, 0.05), 0.02, 0.02, M["iron"])
        g.box((fx, fy - 0.5, fz), (0.9, 0.5, 0.4), M["iron"], rot=(-25, 0, 0))
        g.box((fx, fy - 0.78, fz - 0.12), (0.7, 0.04, 0.28), M["flood"], rot=(-25, 0, 0))
        for k in range(4):
            g.box((fx - 0.27 + k * 0.18, fy - 0.8, fz - 0.12), (0.02, 0.05, 0.3), M["iron"], rot=(-25, 0, 0))
    g.box((12.0, 2.0, 0.5), (0.5, 0.35, 1.0), M["stone"])                              # the milestone
    g.cyl((12.0, 2.0, 1.0), 0.25, 0.35, M["stone"], rot=(90, 0, 0), n=12)
    n = 14                                                                             # the rock stair to the gate
    for i in range(n):
        x = 44.0 + 8.0 * i / n
        z = UP * (i + 1) / n
        g.box((x + 0.3, 0.4, z / 2), (8.0 / n, 3.4, z), M["stone_floor"])
    K.ball(g, (45.0, -2.4, 1.5), 1.6, M["rock"], sub=2, squash=(1, 1, 0.7), jitter=0.3, seed=3)
    K.ball(g, (49.5, -2.6, 4.0), 1.8, M["rock"], sub=2, squash=(1, 1, 0.7), jitter=0.3, seed=4)
    # the lay-by where he stops: gravel, a kerb, puddles; a road-closed trestle pushed aside
    g.box((X_STOP + 1.0, 2.75, -0.01), (12.0, 0.7, 0.04), M["gravel"] if "gravel" in M else M["road"])
    for k in range(6):
        g.box((X_STOP - 4.0 + k * 2.0, 3.12, 0.06), (1.9, 0.14, 0.14), M["stone"])
    for (px, py, r) in ((X_STOP - 1.5, 0.2, 0.9), (X_STOP + 3.4, -1.2, 0.7), (X_STOP - 4.2, -1.8, 1.1), (X_STOP + 7.0, 0.8, 0.6), (4.0, -0.6, 1.2), (16.0, 1.0, 0.8)):
        g.cyl((px, py, 0.016), r, 0.008, M["puddle"], n=18)
    bx, by = X_STOP + 5.5, 3.6
    for s in (-1, 1):
        g.beam((bx + s * 0.8, by - 0.3, 0.0), (bx + s * 0.8, by, 0.9), 0.05, 0.05, M["iron"])
        g.beam((bx + s * 0.8, by + 0.3, 0.0), (bx + s * 0.8, by, 0.9), 0.05, 0.05, M["iron"])
    for k in range(5):
        g.box((bx - 0.8 + k * 0.4, by, 0.92), (0.4, 0.05, 0.2), M["sign_white"] if k % 2 == 0 else M["sign_black"])
    return g.finish(coll)


# ---------------------------------------------------------------- RoadFortress: the fortress above

def build_fortress(coll, M):
    """The fortress itself, from build_fortress_set's own builders (so it is the same fortress the
    next chapter plays in), joined into one object, plus floodlights on every wall and the sentries."""
    parts = [FB.build_gate(coll, M), FB.build_court(coll, M), FB.build_hall(coll, M), FB.build_hall_front(coll, M),
             FB.build_roof(coll, M), FB.build_back(coll, M), FB.build_fore(coll, M), FB.build_portcullis(coll, M)]
    g = S.Group("RoadFortress")
    for ob in parts:
        me = ob.data.copy()
        me.transform(ob.matrix_world)
        remap = [g._mi(m) for m in ob.data.materials]
        for p in me.polygons:
            p.material_index = remap[p.material_index]
        g.bm.from_mesh(me)
        bpy.data.meshes.remove(me)
        old = ob.data
        bpy.data.objects.remove(ob)
        bpy.data.meshes.remove(old)
    # floodlights along the walls, aimed down the face at the road
    for (fx, fy, fz) in ((56.0, -3.0, UP + 3.6), (64.0, -1.2, UP + 11.4), (74.0, -1.2, UP + 11.4), (84.0, -3.0, UP + 3.6), (96.0, -5.2, UP + 0.6),
                         (112.0, -5.2, UP + 0.6), (126.0, -5.2, UP + 0.6), (134.0, -3.2, UP + 10.0), (160.0, -3.2, UP + 10.0),
                         (FB.TOWER_X - 4.0, 5.0, UP + 21.0), (115.0, 18.6, UP + 30.0)):
        g.box((fx, fy + 0.2, fz), (0.8, 0.45, 0.38), M["iron"], rot=(30, 0, 0))
        g.box((fx, fy - 0.04, fz - 0.12), (0.62, 0.04, 0.26), M["flood_lens"], rot=(30, 0, 0))
        g.beam((fx, fy + 0.5, fz + 0.3), (fx, fy + 0.25, fz), 0.05, 0.05, M["iron"])
    # men on the battlements: dark figures, rifles slung
    for (mx, my, mz) in ((60.5, 4.0, UP + 8.0), (66.0, 4.0, UP + 8.0), (79.0, 4.0, UP + 8.0), (86.5, 4.0, UP + 8.0),
                         (95.0, 4.0, UP + 12.0), (108.0, 4.0, UP + 12.0), (120.0, 4.0, UP + 12.0), (63.8, 1.6, UP + 11.3),
                         (74.2, 1.6, UP + 11.3), (140.0, -3.0, UP + 12.4), (152.0, -3.0, UP + 12.4)):
        g.cyl((mx, my, mz + 0.45), 0.13, 0.9, M["sentry"], n=8, r2=0.1)
        g.cyl((mx, my, mz + 1.15), 0.17, 0.55, M["sentry"], n=8, r2=0.13)
        K.ball(g, (mx, my, mz + 1.58), 0.12, M["sentry"], sub=1)
        g.beam((mx + 0.1, my, mz + 1.5), (mx - 0.15, my, mz + 0.8), 0.04, 0.04, M["sentry"])
    return g.finish(coll)


# ---------------------------------------------------------------- the sky, the clouds, the city

def build_sky(coll, M):
    g = S.Group("RoadSky")
    K.sky_band(g, (-60.0, -50.0), 262.0, -150.0, 170.0, M["sky"], n=64)
    # the far ridges round the valley, black against the city's glow
    for k in range(40):
        a = math.radians(180 + k * 9 + rnd.uniform(-3, 3))
        r = rnd.uniform(225, 245)
        cx, cy = -60 + math.cos(a) * r, -50 + math.sin(a) * r
        if cy > 40:
            continue
        g.box((cx, cy, -110 + rnd.uniform(10, 30) / 2), (rnd.uniform(30, 60), 10.0, rnd.uniform(14, 40)), M["sentry"], rot=(0, 0, math.degrees(a) + 90))
    return g.finish(coll)


def build_clouds(coll, M):
    g = S.Group("RoadClouds")
    for _ in range(26):                                                               # the storm over the hill
        c = (rnd.uniform(-200, 220), rnd.uniform(-20, 160), rnd.uniform(48, 95))
        K.ball(g, c, rnd.uniform(16, 34), M["cloud"], sub=2, squash=(1.6, 1.0, 0.32), jitter=0.4, seed=rnd.uniform(0, 99))
    for _ in range(14):                                                               # low cloud round the top
        c = (rnd.uniform(-120, 200), rnd.uniform(20, 70), rnd.uniform(14, 32))
        K.ball(g, c, rnd.uniform(9, 18), M["cloud"], sub=2, squash=(1.8, 1.0, 0.3), jitter=0.45, seed=rnd.uniform(0, 99))
    for _ in range(16):                                                               # a cloud deck over the valley
        c = (rnd.uniform(-300, 160), rnd.uniform(-260, -150), rnd.uniform(-92, -80))
        K.ball(g, c, rnd.uniform(14, 28), M["cloud_low"], sub=1, squash=(2.0, 1.2, 0.18), jitter=0.4, seed=rnd.uniform(0, 99))
    return g.finish(coll)


def build_city(coll, M):
    """The town in the valley: streets of lights in a grid that bends with the river, a ring road, a
    bridge, a lit centre. Quads facing up (one face each); not baked (the game shows them plain)."""
    g = S.Group("RoadCityLights")
    verts, faces = [], []

    def light(x, y, s=0.9):
        z = -109.4 + 1.5 * noise.noise(Vector((x * 0.02, y * 0.02, 5.0)))
        i = len(verts)
        verts.extend([(x - s, y - s * 0.5, z), (x + s, y - s * 0.5, z), (x + s, y + s * 0.5, z), (x - s, y + s * 0.5, z)])
        faces.append((i, i + 1, i + 2, i + 3))
    cx, cy = -40.0, -230.0
    for sx in range(-260, 200, 14):
        for sy in range(-300, -140, 4):
            x = sx + 6 * math.sin(sy * 0.02)
            d = math.hypot((x - cx) / 1.4, sy - cy)
            if d > 180 + 30 * noise.noise(Vector((x * 0.01, sy * 0.01, 1))):
                continue
            if abs(sy + 200 - 18 * math.sin(x * 0.012)) < 7:
                continue                                                               # the river
            if rnd.random() < 0.35 + d / 400:
                continue
            light(x, sy)
    for sy in range(-296, -144, 16):
        for sx in range(-250, 190, 4):
            if rnd.random() < 0.4:
                continue
            d = math.hypot((sx - cx) / 1.4, sy - cy)
            if d < 170 and abs(sy + 200 - 18 * math.sin(sx * 0.012)) > 7:
                light(sx, sy + 3 * math.sin(sx * 0.03))
    for k in range(160):                                                               # the ring road, a string of lights
        a = k / 160 * math.tau
        light(cx + math.cos(a) * 205, cy + math.sin(a) * 120 * 0.55 + 10, 1.1)
    for k in range(20):                                                                # the bridge
        light(-20 + k * 0.4, -200 + (k - 10) * 1.6, 0.7)
    for _ in range(120):                                                               # the centre: dense, bright
        light(cx + rnd.gauss(0, 30), cy + rnd.gauss(0, 18), 1.3)
    K.raw(g, verts, faces, M["city_light"], hint=lambda c, n: Vector((0, 0, 1)))
    return g.finish(coll)


# ---------------------------------------------------------------- the car

WIPER_PIVOTS = ((0.80, -0.50, 0.958), (0.80, 0.10, 0.958))      # WiperR (driver's), WiperL
WIPER_LEN = (0.56, 0.50)
SCREEN_UP = (Vector((K.SCREEN_TOP[0], 0, K.SCREEN_TOP[1])) - Vector((K.SCREEN_BASE[0], 0, K.SCREEN_BASE[1]))).normalized()
SCREEN_N = Vector((SCREEN_UP.z, 0, -SCREEN_UP.x))


def build_car(coll, M):
    g = S.Group("RoadCar")
    K.saloon(g, M, (0, 0, 0), 0.0, M["car_black"], M["head_lamp"])
    car = g.finish(coll)
    gi = S.Group("CarInterior")
    K.interior(gi, M, (0, 0, 0), 0.0)
    cab = gi.finish(coll)
    wipers = []
    for name, piv, ln in (("WiperR", WIPER_PIVOTS[0], WIPER_LEN[0]), ("WiperL", WIPER_PIVOTS[1], WIPER_LEN[1])):
        gw = S.Group(name)
        K.wiper(gw, M, piv, ln, (0, 1, 0), SCREEN_UP)
        w = K.finish_at(gw, coll, piv)
        w.parent = cab
        wipers.append(w)
    # the windscreen glass, just inside the outer glass (the game's rain runs on it)
    gs = S.Group("Windscreen")
    b0, b1 = Vector((K.SCREEN_BASE[0] - 0.012, 0, K.SCREEN_BASE[1] + 0.012)), Vector((K.SCREEN_TOP[0] + 0.012, 0, K.SCREEN_TOP[1] - 0.012))
    wb, wt = K._half_w(b0.x) - 0.09, K._half_w(b1.x) - 0.23
    inset = -SCREEN_N * 0.012
    vs = [tuple(b0 + Vector((0, -wb, 0)) + inset), tuple(b0 + Vector((0, wb, 0)) + inset),
          tuple(b1 + Vector((0, wt, 0)) + inset), tuple(b1 + Vector((0, -wt, 0)) + inset)]
    K.raw(gs, vs, [(0, 1, 2, 3)], M["car_glass"], hint=lambda c, n: -SCREEN_N)
    screen = gs.finish(coll)
    screen.parent = cab
    return car, cab, wipers, screen


def park(car, cab, at, yaw=0.0):
    for o in (car, cab):
        o.location = at
        o.rotation_euler = (0, 0, math.radians(yaw))


# ---------------------------------------------------------------- light

def lights():
    FB.lights()                                                                        # the fortress as its own chapter lights it
    for ob in list(bpy.context.scene.objects):
        if ob.type == "LIGHT" and ob.name in ("Fill", "RoadFlood"):
            bpy.data.objects.remove(ob)
    moon = bpy.context.scene.objects["Moon"]
    moon.data.energy = 0.42
    moon.rotation_euler = (math.radians(58), 0, math.radians(200))
    # the city's glow from below, on the hill's face and the clouds' bellies
    K.add_light("AREA", "CityGlow", (-60.0, -230.0, -60.0), 1.6e6, size=320.0, rot=(math.radians(-62), 0, 0))
    K.add_light("AREA", "FaceFill", (-80.0, -150.0, 20.0), 1.2e5, size=160.0, rot=(math.radians(68), 0, 0))
    # the floodlights at the top: the stop, and the cliff road's
    f = K.add_light("SPOT", "StopFlood", (-6.4, 2.8, 6.4), 9000, size=0.4, spot=80)
    K.aim(f, (X_STOP, Y_STOP - 0.6, 0.0))
    f = K.add_light("SPOT", "RoadFlood", (38.0, 1.4, 5.9), 7000, size=0.4, spot=80)
    K.aim(f, (34.0, -1.5, 0.0))
    for (fx, fy, fz) in ((56.0, -3.0, UP + 3.6), (64.0, -1.2, UP + 11.4), (74.0, -1.2, UP + 11.4), (84.0, -3.0, UP + 3.6),
                         (96.0, -5.2, UP + 0.6), (112.0, -5.2, UP + 0.6), (126.0, -5.2, UP + 0.6), (134.0, -3.2, UP + 10.0), (160.0, -3.2, UP + 10.0)):
        f = K.add_light("SPOT", "WallFlood", (fx, fy - 0.6, fz - 0.2), 26000, size=0.5, spot=95)
        K.aim(f, (fx, fy - 14.0, fz - 16.0))
    # the car, alone out of the way: moonlight on its roof, the night round it, the dials' own glow
    c = CAR_BAKE
    K.add_light("AREA", "CarTop", (c.x, c.y, c.z + 5.0), 220, size=6.0)
    K.add_light("AREA", "CarSideR", (c.x - 1.0, c.y - 6.0, c.z + 1.6), 160, size=6.0, rot=(math.radians(80), 0, 0))
    K.add_light("AREA", "CarSideL", (c.x + 1.0, c.y + 6.0, c.z + 1.6), 60, size=6.0, rot=(math.radians(-80), 0, 0))
    ci = INTERIOR_AT                                                                   # the cabin: the road's glow in the screen
    K.add_light("AREA", "CabinBounce", (ci.x + 3.2, ci.y, ci.z + 0.9), 30, size=2.0, rot=(0, math.radians(-80), 0))
    K.add_light("AREA", "CabinSide", (ci.x - 0.4, ci.y - 2.5, ci.z + 1.3), 10, size=1.5, rot=(math.radians(85), 0, 0))
    w = bpy.context.scene.world
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.018, 0.019, 0.024, 1)


def lightning_lights():
    FB.lightning_lights()
    for x in (-160.0, -40.0):
        K.add_light("AREA", "StrikeHill", (x, 40.0, 120.0), 2.4e6, size=60.0, rot=(math.radians(-150), 0, 0))
    K.add_light("AREA", "StrikeFace", (-90.0, -120.0, 90.0), 9e5, size=80.0, rot=(math.radians(40), 0, 0))
    c = CAR_BAKE
    K.add_light("AREA", "StrikeCar", (c.x + 6.0, c.y + 2.0, c.z + 8.0), 9000, size=6.0)
    K.aim(bpy.context.scene.objects["StrikeCar"], (c.x, c.y, c.z + 0.8))
    ci = INTERIOR_AT
    K.aim(K.add_light("AREA", "StrikeCabin", (ci.x + 8.0, ci.y + 1.0, ci.z + 7.0), 6000, size=5.0), (ci.x, ci.y, ci.z + 1.0))
    for ob in bpy.context.scene.objects:
        if ob.type == "LIGHT" and ob.name in ("CityGlow",):
            ob.data.energy *= 0.6


# ---------------------------------------------------------------- the plan for the game

def car_path(step=5.0):
    """The car's line: the left lane, from x -240 on the first leg to the stop; (Unity) points."""
    out = []
    fr = frames_at(every=1)
    acc = None
    for (p, left, u, s, i) in fr:
        if p.x < PATH_FROM_X and p.y < LEG_Y[0] + 1:
            continue
        lane = LANE
        if abs(p.y) < 1.0 and p.x > -40.0:
            lane = LANE + (Y_STOP - LANE) * min(1.0, (p.x + 40.0) / 24.0)
        q = p + left * lane
        if acc is None or (q - acc).length >= step - 1e-6:
            out.append(q.copy())
            acc = q
        if p.x >= X_STOP and abs(p.y) < 1.0:
            break
    end = Vector((X_STOP, Y_STOP, 0.0))
    if (out[-1] - end).length > 0.2:
        out.append(end)
    return out


def simplify(pts, tol):
    """Douglas-Peucker: the fewest points that stay within tol of the line."""
    if len(pts) < 3:
        return list(pts)
    a, b = pts[0], pts[-1]
    ab = b - a
    best, idx = -1.0, 0
    for i in range(1, len(pts) - 1):
        t = max(0.0, min(1.0, (pts[i] - a).dot(ab) / max(1e-9, ab.length_squared)))
        dd = (pts[i] - (a + ab * t)).length
        if dd > best:
            best, idx = dd, i
    if best <= tol:
        return [a, b]
    return simplify(pts[:idx + 1], tol)[:-1] + simplify(pts[idx:], tol)


def write_plan():
    pts = car_path()
    key = simplify(car_path(step=1.0), 0.9)
    length = sum((b - a).length for a, b in zip(pts, pts[1:]))

    def v3(p):
        return f"new Vector3({p.x:.2f}f, {p.z:.2f}f, {p.y:.2f}f)"

    def u3(x, y, z):
        return f"new Vector3({x:.3f}f, {z:.3f}f, {y:.3f}f)"
    # where the last hairpin lets go: the fortress fills the screen from here on
    reveal = next(i for i, p in enumerate(pts) if abs(p.y - LANE) < 0.3 and p.x > XL + 2)
    reveal_s = sum((b - a).length for a, b in zip(pts[:reveal], pts[1:reveal + 1]))
    seat_top = 0.488
    lines = [
        "using UnityEngine;",
        "",
        "namespace ProjectInk",
        "{",
        "    /// <summary>Kessel Hill's road at night (tools/blender/build_road_set.py; written by its --write-plan):",
        "    /// five legs climbing the hill's face in hairpins to the fortress's cliff road (y 0), where the car",
        "    /// stops in the floodlight short of x 0 (the fortress chapter starts the hero at x 2). The car keeps the",
        "    /// left lane. World coordinates (x along, y up, z depth). Right-hand drive: the driver sits at -z.</summary>",
        "    public static class RoadPlan",
        "    {",
        "        /// <summary>The car's line up the hill, every 5 m, from the foot (x -240) to the stop.</summary>",
        "        public static readonly Vector3[] Path =",
        "        {",
    ]
    for k in range(0, len(pts), 4):
        lines.append("            " + ", ".join(v3(p) for p in pts[k:k + 4]) + ",")
    lines += [
        "        };",
        "        /// <summary>The same route as key points (the ends of the legs, the hairpins round), bottom to the stop.</summary>",
        "        public static readonly Vector3[] CarPath =",
        "        {",
    ]
    for k in range(0, len(key), 4):
        lines.append("            " + ", ".join(v3(p) for p in key[k:k + 4]) + ",")
    lines += [
        "        };",
        f"        public const float PathLength = {length:.1f}f;",
        f"        /// <summary>Distance along Path where the car comes out of the last hairpin and the fortress fills the windscreen.</summary>",
        f"        public const float RevealDistance = {reveal_s:.1f}f;",
        f"        public static readonly Vector3 FinalHairpin = {u3(XL, LEG_Y[3] + R_HP, leg_z(3, XL) + 0.5 * (leg_z(4, XL) - leg_z(3, XL)))};",
        f"        public static readonly Vector3 CarStop = {u3(X_STOP, Y_STOP, 0.0)};",
        "        public const float CarStopYaw = 0f;   // the car faces +x at the stop",
        f"        public const float GetOutX = {X_STOP - 0.35:.2f}f;",
        f"        public static readonly Vector3 GetOutSpot = {u3(X_STOP - 0.35, Y_STOP - K.CAR_HALF_W - 0.55, 0.0)};",
        f"        public static readonly Vector3 StopFloodlight = {u3(-6.4, 2.8, 6.4)};",
        f"        public static readonly Vector3 KesselSign = {u3(-214.0, LEG_Y[0] + FLAT + 0.6, leg_z(0, -214.0) + 1.95)};",
        f"        public static readonly Vector3 FortressGate = {u3(69.0, 0.0, UP + 4.0)};",
        f"        public static readonly Vector3 Fortress = {u3(102.0, FB.WALL_Y - 0.1, UP + 6.5)};   // the keep's lit windows over the courtyard",
        f"        public static readonly Vector3 ObservatoryTop = {u3(FB.TOWER_X, 9.0, UP + 28.0)};",
        "        public const float CliffRoadY = 0f, FortressHeroStartX = 2f;",
        "",
        "",
        "        // ---- inside the car. The interior (CarInterior, with WiperL/WiperR and Windscreen under it) stays",
        "        // parked here, facing +x, and is filmed there: these are world points.",
        f"        public static readonly Vector3 InteriorOrigin = {u3(*INTERIOR_AT)};",
        f"        public static readonly Vector3 DriverHead = {u3(*(INTERIOR_AT + Vector((-0.62, K.DRIVER_Y, 1.14))))};",
        f"        public static readonly Vector3 DriverHands = {u3(*(INTERIOR_AT + Vector((0.13, K.DRIVER_Y, 0.80))))};   // the wheel's centre",
        "        /// <summary>The empty passenger seat's cushion, top centre: the walkie-talkie lies here.</summary>",
        f"        public static readonly Vector3 PassengerSeat = {u3(*(INTERIOR_AT + Vector((-0.42, -K.DRIVER_Y, seat_top))))};",
        f"        public static readonly Vector3 PassengerHead = {u3(*(INTERIOR_AT + Vector((-0.62, -K.DRIVER_Y, 1.14))))};",
        f"        public static readonly Vector3 Windscreen = {u3(*(INTERIOR_AT + Vector(((K.SCREEN_BASE[0] + K.SCREEN_TOP[0]) / 2, 0.0, (K.SCREEN_BASE[1] + K.SCREEN_TOP[1]) / 2))))};",
        "        /// <summary>Far ahead through the windscreen: the road's end and the fortress over it.</summary>",
        f"        public static readonly Vector3 OutsideAhead = {u3(*(INTERIOR_AT + Vector((70.0, -0.4, 3.0))))};",
        f"        public static readonly Vector3 RearViewMirror = {u3(*(INTERIOR_AT + Vector((K.SCREEN_TOP[0] - 0.094, 0.0, K.SCREEN_TOP[1] - 0.14))))};",
        f"        public static readonly Vector3 Gauges = {u3(*(INTERIOR_AT + Vector((0.462, K.DRIVER_Y, 0.865))))};",
        f"        public static readonly Vector3 WiperPivotR = {u3(*(INTERIOR_AT + Vector(WIPER_PIVOTS[0])))};",
        f"        public static readonly Vector3 WiperPivotL = {u3(*(INTERIOR_AT + Vector(WIPER_PIVOTS[1])))};",
        "        /// <summary>The wipers turn about this world axis (the screen's normal), from 0 (parked, lying along the",
        "        /// screen's foot towards +z) to WiperSweep degrees: w.localRotation = Quaternion.AngleAxis(a, WiperAxis).</summary>",
        f"        public static readonly Vector3 WiperAxis = {u3(SCREEN_N.x, SCREEN_N.y, SCREEN_N.z)};",
        "        public const float WiperSweep = -100f;",
        "        /// <summary>The exterior car's lamps, in its own frame (RoadCar: +x forward, +z its left).</summary>",
        f"        public static readonly Vector3 HeadlampLocal = {u3(2.37, 0.60, 0.645)};   // the left one; -z for the right",
        f"        public const float CarLength = {K.CAR_LEN:.2f}f, CarHalfWidth = {K.CAR_HALF_W:.2f}f, RoofY = {K.ROOF_Z:.2f}f;",
        "",
        "",
        "        /// <summary>The car's turn for a direction of travel (the model faces +x).</summary>",
        "        public static Quaternion CarRotation(Vector3 dir)",
        "        {",
        "            dir.y *= 0.6f;   // pitch with the hill, a little less than the road",
        "            return Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);",
        "        }",
        "",
        "        /// <summary>A point on the path `distance` metres from its start, and the way it is heading.</summary>",
        "        public static Vector3 Sample(float distance, out Vector3 dir)",
        "        {",
        "            distance = Mathf.Clamp(distance, 0f, PathLength);",
        "            for (int i = 0; i < Path.Length - 1; i++)",
        "            {",
        "                float seg = Vector3.Distance(Path[i], Path[i + 1]);",
        "                if (distance <= seg || i == Path.Length - 2)",
        "                {",
        "                    dir = (Path[i + 1] - Path[i]).normalized;",
        "                    return Vector3.Lerp(Path[i], Path[i + 1], seg > 0f ? Mathf.Clamp01(distance / seg) : 0f);",
        "                }",
        "                distance -= seg;",
        "            }",
        "            dir = Vector3.right;",
        "            return Path[Path.Length - 1];",
        "        }",
        "    }",
        "}",
        "",
    ]
    with open(PLAN, "w") as f:
        f.write("\n".join(lines))
    print("wrote", PLAN, len(pts), "points,", round(length, 1), "m")


# ---------------------------------------------------------------- main

def main():
    if "--write-plan" in sys.argv:
        write_plan()
        return
    preview_only = "--preview" in sys.argv
    lit = "--lightning-preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    hill, way, trees, top = build_hill(coll, M), build_way(coll, M), build_trees(coll, M), build_top(coll, M)
    fort, sky, clouds, city = build_fortress(coll, M), build_sky(coll, M), build_clouds(coll, M), build_city(coll, M)
    car, cab, wipers, screen = build_car(coll, M)
    D.bevel(top, 0.02)
    parts = [hill, way, trees, top, fort, sky, clouds, car, cab] + wipers
    for o in parts + [city, screen]:
        print(f"  {o.name:16s} {K.faces(o):7d} faces")
    print("  total", sum(K.faces(o) for o in parts + [city, screen]))
    lights()
    if preview_only or lit:
        if lit:
            lightning_lights()
        screen.hide_render = True
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        cams = preview_shots(car, cab, wipers)
        S.preview(cams, "road_set_lightning.png" if lit else "road_set.png")
        return
    for o in parts:
        S.unwrap(o)
    ground = bpy.data.objects.new("_BakeGround", bpy.data.meshes.new("_BakeGround"))
    gg = S.Group("_tmp")
    gg.box((0, 0, -0.05), (14, 14, 0.1), M["bake_ground"])
    ground = gg.finish(coll)
    ground.location = CAR_BAKE
    car.location = CAR_BAKE
    cab.location = INTERIOR_AT

    def bake_all(suffix, big, small, samples):
        S.bake(hill, big, samples, suffix)
        S.bake(way, big, samples, suffix)
        S.bake(trees, small, samples, suffix)
        S.bake(top, small, samples, suffix)
        S.bake(fort, big, samples, suffix)
        S.bake(sky, 1024, max(4, samples // 2), suffix)
        S.bake(clouds, 1024, samples, suffix)
        cab.hide_render = True
        S.bake(car, 1024, samples, suffix)
        cab.hide_render = False
        car.hide_render = True
        S.bake(cab, 2048, samples * 2, suffix)
        for w in wipers:
            S.bake(w, 256, samples, suffix)
        car.hide_render = False
    city.hide_render = False
    bake_all("", 4096, 2048, 16)
    lightning_lights()
    bake_all("_Lightning", 2048, 1024, 10)
    bpy.data.objects.remove(ground)
    car.location = Vector((X_STOP, Y_STOP, 0.0))
    export(parts + [city, screen] + K.marks(coll))


def export(objs):
    K.export(objs, os.path.join(OUT, "RoadSet.fbx"))


def preview_shots(car, cab, wipers):
    """Shots from the chapter's list. The interior stays parked at INTERIOR_AT (the story films it
    there); the exterior car is moved per shot (hidden for the inside shots: from inside, the game
    culls its back faces)."""
    pts = car_path(step=1.0)

    def at(x, y_leg):
        i = min(range(len(pts)), key=lambda k: abs(pts[k].x - x) + 3 * abs(pts[k].y - y_leg))
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        return pts[i], d
    ci = INTERIOR_AT
    shots, placements = [], []

    def inside(label, eye, look, fov):
        e, lk = ci + Vector(eye), ci + Vector(look)
        shots.append((label, S.shot_camera("C%d" % len(shots), (e.x, e.z, e.y), (lk.x, lk.z, lk.y), fov)))
        placements.append(None)

    def outside(label, p, d, pos, look, fov):
        shots.append((label, S.shot_camera("O%d" % len(shots), pos, look, fov)))
        placements.append((p, d))
    p, d = at(-80.0, LEG_Y[2] + LANE)
    outside("5.1  the hairpins from high, a car climbing", p, d, (-70.0, 30.0, -150.0), (-82.0, -28.0, -32.0), 44)
    inside("5.2  close on his hands on the wheel", (-0.30, -0.62, 1.02), (0.18, -0.30, 0.80), 50)
    inside("5.3  two-shot of nobody: the empty seat", (0.30, -0.70, 1.10), (-0.42, 0.37, 0.62), 62)
    inside("5.8  the fortress fills the windscreen", (-0.62, -0.37, 1.14), (20.0, 0.0, 3.8), 58)
    inside("5.6  the dials, the wipers, the mirror", (-0.75, 0.05, 1.20), (0.4, -0.25, 0.95), 70)
    outside("5.9  the floodlit stop (the car parked)", Vector((X_STOP, Y_STOP, 0.0)), Vector((1, 0, 0)), (-15.5, 1.5, -6.0), (-7.0, 1.0, 1.0), 48)
    p5, d5 = at(XR + 6.0, LEG_Y[2] + 6.0)
    outside("a hairpin close: chevrons, barrier, the cutting", p5, d5, (XR + 22.0, leg_z(2, XR) + 4.0, LEG_Y[2] - 8.0), (XR + 6.0, leg_z(2, XR) + 1.2, LEG_Y[2] + 8.0), 52)
    outside("the fortress from the cliff road", Vector((X_STOP, Y_STOP, 0.0)), Vector((1, 0, 0)), (-26.0, 1.6, -1.0), (60.0, 12.0, 6.0), 50)
    cab.location = ci

    def mover(scene, depsgraph=None):
        f = scene.frame_current - 1
        if 0 <= f < len(placements):
            pl = placements[f]
            car.hide_render = pl is None
            if pl:
                p, d = pl
                pitch = math.degrees(math.asin(max(-1, min(1, d.z))))
                car.location = p
                car.rotation_euler = (0, -math.radians(pitch), math.atan2(d.y, d.x))
    bpy.app.handlers.frame_change_pre.append(mover)
    return [(label, cam, k + 1) for k, (label, cam) in enumerate(shots)]


if __name__ == "__main__":
    main()
