"""Chapter 7: HOME, the epilogue (docs/story/story_v2.md, CHAPTER 7). The morning after the storm. No
fighting: three places in one scene, laid out along x with wide gaps (each in its own sky):

  A  THE ROAD     x 0     the bottom of Kessel Hill's cliff road at dawn: police cars with their light
                          bars (the lenses are separate: PoliceLight_0..5, white, they flash), tape,
                          cones, a forensic tent at the foot of the cliff where Voss fell, Harrow's car;
                          the staging for the three of them (him, Maya, Harrow) on the road
  B  THE GRAVE    x 500   a hillside cemetery at dawn: rows of headstones of every kind, angels and
                          crosses, a stone wall, the iron gate, yews, a bench, wet grass, white flowers;
                          ElenaStone (separate, a flat top for the walkie-talkie, room to kneel before it)
  C  HOME         x 1000  the back of No. 14 in the morning: the garden, the swing (SwingSeat separate,
                          its origin on the top bar: it swings about the bar), the back door (BackDoor,
                          its origin on the hinge), and inside, the hall: the drawer cabinet under Elena's
                          photo (HallDrawer, HallDrawerFront slides out along +x, HallPhoto), the stairs,
                          the front door; the kitchen through its doorway

Dawn: a low sun, a bright sky, soft and a little warm, in the game's black and white. One bake.

    /home/user/.venv-blender/bin/python tools/blender/build_epilogue_set.py               # bake + export
    /home/user/.venv-blender/bin/python tools/blender/build_epilogue_set.py --preview     # shots, no bake
    /home/user/.venv-blender/bin/python tools/blender/build_epilogue_set.py --write-plan  # EpiloguePlan.cs

Writes Assets/Art/Epilogue/EpilogueSet.fbx and its bakes. Blender X = Unity x, Y = Unity z, Z = Unity y."""
import math
import os
import random
import sys

import bpy  # noqa: E402
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402
import set_kit as K  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Epilogue")
PLAN = os.path.join(ROOT, "Assets", "Scripts", "Level", "EpiloguePlan.cs")
rnd = random.Random(707)

A = Vector((0.0, 0.0, 0.0))          # the road
B = Vector((500.0, 0.0, 0.0))        # the cemetery
C = Vector((1000.0, 0.0, 0.0))       # the backyard and the hall
SKY_R = 235.0

# A: the road
CLIFF_Y = 12.0
DET_CAR = (-2.4, -2.35, 0.0)                       # Harrow's car (its middle), facing +x
POLICE = [((-10.5, -0.6, 0.0), 18.0), ((12.0, 0.9, 0.0), 166.0), ((-21.0, 1.6, 0.0), -4.0)]
VAN = ((24.5, 1.7, 0.0), 180.0)
TENT = (15.0, 9.2)
# B: the grave
ELENA = Vector((0.0, 1.6, 0.0))                    # the stone's foot, middle (B-local); its face looks to -y
STONE_W, STONE_H, STONE_T = 0.66, 0.92, 0.14
GATE_X, PATH_Y = -32.0, -1.3
# C: home
HX0, HX1, HY0, HY1 = -6.0, 10.0, 10.0, 20.0        # the house (the back wall at HY0 faces the garden)
HALL_X0, HALL_X1 = 0.0, 2.0
FLOOR = 0.12                                       # the hall's floor (a step up from the patio)
CEIL = 2.6
BACK_DOOR = (0.55, 1.45)                           # its opening along x
FRONT_DOOR = (0.45, 1.35)
CAB_Y = 16.6                                       # the drawer cabinet against the hall's left wall (x 0)
SWING = Vector((8.5, -2.0, 2.25))                  # the swing's pivot (on the top bar)
SEAT_Z = 0.46


def mats():
    M = S.materials()
    K.car_materials(M)
    S.TONE.update({"PoliceWhite": 0.62, "PoliceStripe": 0.03, "DetGrey": 0.16, "VanWhite": 0.6, "TentWhite": 0.78,
                   "TapeWhite": 0.75, "TapeBlack": 0.02, "ConeWhite": 0.7, "Headstone": 0.42, "HeadstoneDark": 0.24,
                   "Marble": 0.7, "Inscription": 0.06, "Petal": 0.85, "Stem": 0.05, "IronBlack": 0.02, "BenchWood": 0.2,
                   "Brick": 0.3, "WinGlass": 0.55, "DoorPaint": 0.08, "Plaster": 0.62, "Skirting": 0.75, "Runner": 0.12,
                   "PhotoPaper": 0.6, "Frame": 0.03, "FencePanel": 0.2, "ShedWood": 0.14, "Felt": 0.04, "Gravel": 0.4,
                   "Tile": 0.55, "Counter": 0.7, "SwingRope": 0.05, "Sand": 0.45, "ToyPale": 0.6})
    mat = S.mat
    M["police"] = mat("PoliceWhite", (0.62, 0.62, 0.62), 0.25, metal=0.4)
    M["stripe"] = mat("PoliceStripe", (0.03, 0.03, 0.03), 0.4)
    M["det"] = mat("DetGrey", (0.16, 0.16, 0.16), 0.3, metal=0.5)
    M["van"] = mat("VanWhite", (0.6, 0.6, 0.6), 0.35, metal=0.3)
    M["bar"] = mat("LightBar", (0.05, 0.05, 0.05), 0.4)
    M["lens"] = mat("PoliceLens", (1, 1, 1), 0.2, emit=(1, 1, 1), strength=30.0)
    M["tent"] = mat("TentWhite", (0.78, 0.78, 0.78), 0.9)
    M["tape_w"] = mat("TapeWhite", (0.75, 0.75, 0.75), 0.6)
    M["tape_b"] = mat("TapeBlack", (0.02, 0.02, 0.02), 0.6)
    M["cone"] = mat("ConeWhite", (0.7, 0.7, 0.7), 0.5)
    M["road"] = K.mottled("DawnAsphalt", 0.075, 0.3, scale=0.4, var=0.4)
    M["verge"] = K.mottled("DawnVerge", 0.12, 0.7, scale=0.3, var=0.6)
    M["cliff"] = K.strata_rock("DawnCliff", 0.32, "xz", 0.5)
    M["scree"] = K.strata_rock("DawnScree", 0.28, "xy", 0.8)
    M["line"] = mat("RoadLine", (0.62, 0.62, 0.62), 0.5)
    M["grass"] = K.mottled("WetGrass", 0.14, 0.45, scale=0.5, var=0.55)
    M["grass_far"] = K.mottled("FarGrass", 0.17, 0.8, scale=0.05, var=0.5)
    M["tuft"] = mat("Tuft", (0.1, 0.1, 0.1), 0.6)
    M["stone"] = D.stone("HeadstoneGrain", 0.42, 0.3, "xz", block=(3.0, 3.0), rough=0.8, wet=0.3)
    M["stone_old"] = D.stone("HeadstoneOld", 0.26, 0.2, "xz", block=(3.0, 3.0), rough=0.9, wet=0.3)
    M["marble"] = mat("Marble", (0.7, 0.7, 0.7), 0.25)
    M["inscr"] = mat("Inscription", (0.06, 0.06, 0.06), 0.8)
    M["wall_x"] = D.stone("DryStone", 0.3, 0.1, "xz", block=(0.42, 0.2), wet=0.4)
    M["wall_y"] = D.stone("DryStone", 0.3, 0.1, "yz", block=(0.42, 0.2), wet=0.4)
    M["coping"] = D.stone("Coping", 0.36, 0.12, "xz", block=(0.22, 0.4))
    M["gravel"] = K.mottled("Gravel", 0.4, 0.9, scale=6.0, var=0.4)
    M["iron"] = mat("IronBlack", (0.02, 0.02, 0.02), 0.4, metal=0.6)
    M["yew"] = K.mottled("Yew", 0.035, 0.95, scale=2.0, var=0.6)
    M["bark"] = D.flat("YewBark", 0.08)
    M["bench"] = D.planks("BenchWood", 0.2, "xy", board=0.09)
    M["petal"] = mat("Petal", (0.85, 0.85, 0.85), 0.6)
    M["stem"] = mat("Stem", (0.05, 0.05, 0.05), 0.8)
    M["brick_x"] = D.stone("HouseBrick", 0.3, 0.18, "xz", block=(0.225, 0.075), wet=0.2)
    M["brick_y"] = D.stone("HouseBrick", 0.3, 0.18, "yz", block=(0.225, 0.075), wet=0.2)
    M["roof"] = D.stone("RoofTiles", 0.1, 0.03, "xz", block=(0.3, 0.2))
    M["roof_y"] = D.stone("RoofTiles", 0.1, 0.03, "yz", block=(0.3, 0.2))
    M["win"] = mat("WinGlass", (0.55, 0.55, 0.56), 0.05, emit=(1, 1, 1), strength=0.5)
    M["frame"] = mat("WinFrame", (0.8, 0.8, 0.8), 0.4)
    M["door"] = mat("DoorPaint", (0.08, 0.08, 0.08), 0.35)
    M["plaster"] = K.mottled("Plaster", 0.62, 0.85, scale=3.0, var=0.12)
    M["paper"] = D.planks("Wallpaper", 0.52, "yz", board=0.53)
    M["skirting"] = mat("Skirting", (0.75, 0.75, 0.75), 0.4)
    M["boards"] = D.planks("HallBoards", 0.22, "xy", board=0.14)
    M["runner"] = K.mottled("Runner", 0.12, 0.95, scale=30.0, var=0.4)
    M["photo"] = mat("PhotoPaper", (0.6, 0.6, 0.6), 0.3)
    M["frame_blk"] = mat("Frame", (0.03, 0.03, 0.03), 0.3)
    M["oak"] = D.planks("Oak", 0.24, "xz", board=0.12)
    M["oak_y"] = D.planks("Oak", 0.24, "yz", board=0.12)
    M["fence"] = D.planks("FencePanel", 0.2, "yz", board=0.15)
    M["fence_x"] = D.planks("FencePanel", 0.2, "xz", board=0.15)
    M["shed"] = D.planks("ShedWood", 0.14, "xz", board=0.15)
    M["felt"] = mat("Felt", (0.04, 0.04, 0.04), 0.9)
    M["paving"] = D.stone("Paving", 0.42, 0.2, "xy", block=(0.6, 0.6), wet=0.5)
    M["tile"] = D.stone("KitchenTile", 0.55, 0.3, "xy", block=(0.3, 0.3))
    M["counter"] = mat("Counter", (0.7, 0.7, 0.7), 0.3)
    M["rope"] = mat("SwingRope", (0.05, 0.05, 0.05), 0.6, metal=0.5)
    M["sand"] = K.mottled("Sand", 0.45, 1.0, scale=8.0, var=0.2)
    M["toy"] = mat("ToyPale", (0.6, 0.6, 0.6), 0.4)
    M["shrub"] = K.mottled("Shrub", 0.06, 0.9, scale=3.0, var=0.5)
    M["leaf"] = K.mottled("Leaf", 0.09, 0.9, scale=1.5, var=0.5)
    M["sky_a"] = K.grad_sky("DawnSkyA", -30.0, 180.0, [(0.0, 0.72), (0.12, 0.9), (0.3, 0.62), (1.0, 0.32)], 1.0)
    M["sun"] = mat("DawnSun", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=12.0)
    M["far"] = mat("FarHill", (0.32, 0.32, 0.33), 0.9)
    M["far2"] = mat("FarHill2", (0.42, 0.42, 0.43), 0.9)
    M["far_dark"] = mat("FarDark", (0.16, 0.16, 0.17), 0.9)
    return M


# ---------------------------------------------------------------- helpers

def wall(g, axis, at, a0, a1, z0, z1, t, holes, m):
    """A wall along x (axis "x", in the plane y=at) or along y (axis "y", plane x=at) with
    rectangular holes [(a0, a1, z0, z1)], built as solid strips round them."""
    cuts = sorted(set([a0, a1] + [h[0] for h in holes] + [h[1] for h in holes]))
    cuts = [c for c in cuts if a0 <= c <= a1]
    for u0, u1 in zip(cuts, cuts[1:]):
        if u1 - u0 < 1e-4:
            continue
        mid = (u0 + u1) / 2
        spans = [(z0, z1)]
        for (h0, h1, hz0, hz1) in holes:
            if h0 < mid < h1:
                nxt = []
                for (s0, s1) in spans:
                    if hz1 <= s0 or hz0 >= s1:
                        nxt.append((s0, s1))
                        continue
                    if hz0 > s0:
                        nxt.append((s0, hz0))
                    if hz1 < s1:
                        nxt.append((hz1, s1))
                spans = nxt
        for (s0, s1) in spans:
            if s1 - s0 < 1e-4:
                continue
            if axis == "x":
                g.box((mid, at, (s0 + s1) / 2), (u1 - u0, t, s1 - s0), m)
            else:
                g.box((at, mid, (s0 + s1) / 2), (t, u1 - u0, s1 - s0), m)


def P(o, x, y, z=0.0):
    return (o.x + x, o.y + y, o.z + z)


def flowers(g, M, c, n=9, r=0.12, h=0.35, wrap=True):
    """A bunch of white flowers: stems, leaves, open blooms (petals round a centre), in paper."""
    cx, cy, cz = c
    for k in range(n):
        a = k / n * math.tau + rnd.uniform(-0.3, 0.3)
        rr = r * math.sqrt(rnd.random())
        top = Vector((cx + math.cos(a) * rr * 1.6, cy + math.sin(a) * rr * 1.6, cz + h * rnd.uniform(0.75, 1.1)))
        g.beam((cx + math.cos(a) * rr * 0.3, cy + math.sin(a) * rr * 0.3, cz), tuple(top), 0.006, 0.006, M["stem"])
        g.cyl(tuple(top), 0.022, 0.03, M["petal"], n=6, r2=0.012)
        for p in range(5):
            b = p / 5 * math.tau + k
            g.box((top.x + math.cos(b) * 0.028, top.y + math.sin(b) * 0.028, top.z - 0.004), (0.04, 0.026, 0.006), M["petal"], rot=(0, 18, math.degrees(b)))
        if k % 3 == 0:
            g.box((top.x * 0.5 + cx * 0.5, top.y * 0.5 + cy * 0.5, cz + h * 0.45), (0.06, 0.012, 0.025), M["stem"], rot=(0, 30, math.degrees(a)))
    if wrap:
        g.cyl((cx, cy, cz + h * 0.25), r * 0.7, h * 0.5, M["tape_w"], n=8, r2=r * 1.3)


def tufts(g, M, centre, rx, ry, n, zf):
    for _ in range(n):
        x, y = centre[0] + rnd.uniform(-rx, rx), centre[1] + rnd.uniform(-ry, ry)
        z = zf(x, y)
        g.cyl((x, y, z + 0.06), rnd.uniform(0.05, 0.1), rnd.uniform(0.1, 0.2), M["tuft"], n=4, r2=0.005, rot=(rnd.uniform(-15, 15), rnd.uniform(-15, 15), rnd.uniform(0, 90)))


# ---------------------------------------------------------------- A: the bottom of the cliff road

def cliff_face(g, o, x0, x1, y, z1, m):
    """A rock wall facing -y: a grid in x-z pushed about by strata and ledges, leaning back."""
    xs = [x0 + (x1 - x0) * i / 70 for i in range(71)]
    zs = [z1 * j / 26 for j in range(27)]
    verts, faces = [], []
    for z in zs:
        for x in xs:
            d = 1.6 * noise.noise(Vector((x * 0.08, z * 0.05, 1.0))) + 0.5 * noise.noise(Vector((x * 0.3, z * 0.9, 2.0)))
            d += 0.8 * math.sin(z * 0.9 + noise.noise(Vector((x * 0.05, 0, 3))) * 2.0)
            verts.append((o.x + x, o.y + y + z * 0.22 + d, o.z + z))
    n = len(xs)
    for j in range(len(zs) - 1):
        for i in range(n - 1):
            a = j * n + i
            faces.append((a, a + 1, a + n + 1, a + n))
    K.raw(g, verts, faces, m, hint=lambda c, nr: Vector((0, -1, 0)))


def police_car(g, M, lights, at, yaw, idx):
    def extras(L):
        for s in (-1, 1):
            y = s * (K.CAR_HALF_W + 0.006)
            L.box((-0.3, y, 0.72), (3.9, 0.008, 0.14), M["stripe"])
            L.box((-0.3, y, 0.80), (3.9, 0.008, 0.025), M["stripe"])
            Mt = L.M @ K.frame_m((0.05, y + s * 0.006, 0.72), (s * -1, 0, 0) if s < 0 else (-1, 0, 0), (0, 0, 1))
            if s < 0:
                Mt = L.M @ K.frame_m((0.05, y - 0.006, 0.72), (1, 0, 0), (0, 0, 1))
            K.text(g, "POLICE", Mt, 0.11, M["police"], extrude=0.003)
        L.box((-0.5, 0, K.ROOF_Z + 0.05), (0.24, 1.15, 0.07), M["bar"])
        for s in (-1, 1):
            L.box((-0.5, s * 0.5, K.ROOF_Z + 0.02), (0.06, 0.04, 0.05), M["bar"])
        L.box((-0.5, 0, K.ROOF_Z + 0.105), (0.2, 0.25, 0.04), M["police"])
        L.box((2.38, 0, 0.36), (0.03, 0.5, 0.1), M["stripe"])
    L = K.saloon(g, M, at, yaw, M["police"], M["head_lamp_off"], extras=extras)
    for s in (-1, 1):
        gl = S.Group("PoliceLight_%d" % (2 * idx + (0 if s > 0 else 1)))
        p = L.p((-0.5, s * 0.30, K.ROOF_Z + 0.115))
        gl.box(tuple(p), (0.2, 0.32, 0.075), M["lens"], rot=(0, 0, yaw))
        lights.append(K.finish_at(gl, bpy.context.scene.collection, p))


def build_road(coll, M):
    g = S.Group("EpiRoad")
    o = A
    K.sheet(g, o.x - 80, o.x + 80, o.y - 3.2, o.y + 3.2, lambda x, y: 0.02 - 0.012 * abs(y) / 3.2, M["road"], res=2.0)
    K.sheet(g, o.x - 80, o.x + 80, o.y + 3.2, o.y + 6.4, lambda x, y: -0.04 + 0.2 * noise.noise(Vector((x * 0.3, y * 0.3, 1))) * (y - 3.2) / 3.2, M["verge"], res=1.0)
    # the scree rising to the cliff, and the cliff
    K.sheet(g, o.x - 80, o.x + 80, o.y + 6.4, o.y + CLIFF_Y + 2.0,
            lambda x, y: (y - 6.4) ** 1.6 * 0.25 + 0.35 * noise.noise(Vector((x * 0.25, y * 0.25, 4))), M["scree"], res=1.0)
    cliff_face(g, o, -80.0, 80.0, CLIFF_Y, 52.0, M["cliff"])
    for _ in range(46):
        x, y = rnd.uniform(-70, 70), rnd.uniform(6.6, 11.4)
        r = rnd.uniform(0.3, 1.4)
        K.ball(g, (x, y, (y - 6.4) ** 1.6 * 0.25 + r * 0.3), r, M["cliff"], sub=1, squash=(1.2, 1, 0.6), jitter=0.4, seed=x)
    # the camera's side: a verge, a stone wall on the drop, the valley falling away
    K.sheet(g, o.x - 80, o.x + 80, o.y - 7.0, o.y - 3.2, lambda x, y: -0.06 + 0.15 * noise.noise(Vector((x * 0.3, y * 0.3, 7))), M["verge"], res=1.0)
    K.sheet(g, o.x - 90, o.x + 90, o.y - 70.0, o.y - 7.0, lambda x, y: -(abs(y) - 7.0) * 0.55 + 1.5 * noise.noise(Vector((x * 0.05, y * 0.05, 8))), M["grass_far"], res=5.0)
    for x in range(-78, 79, 2):
        g.box((x, -6.4, 0.3), (2.0, 0.45, 0.65), M["wall_x"])
        g.box((x, -6.4, 0.68), (2.02, 0.5, 0.1), M["coping"])
    # markings, kerbs, drain grates, a manhole, the puddles of the night
    for x in range(-78, 79, 9):
        g.box((x, 0.0, 0.026), (3.0, 0.12, 0.004), M["line"])
    for s in (-1, 1):
        g.box((0, s * 3.05, 0.024), (160.0, 0.12, 0.004), M["line"])
    for x in range(-70, 71, 14):
        g.box((x, 3.15, 0.02), (0.6, 0.35, 0.02), M["iron"])
    g.cyl((5.0, -1.4, 0.022), 0.35, 0.01, M["iron"], n=16)
    for (px, py, r) in ((-6.0, 1.2, 1.4), (3.8, -2.1, 0.9), (8.5, 1.8, 1.6), (-15.0, -1.0, 1.1), (19.0, -0.4, 1.2)):
        g.cyl((px, py, 0.024), r, 0.006, M["puddle"], n=20)
    # the storm's leavings: branches across the verge, leaves, a fallen pine against the cliff
    for _ in range(10):
        x, y = rnd.uniform(-40, 40), rnd.choice((rnd.uniform(3.4, 6.0), rnd.uniform(-6.0, -3.4)))
        a = rnd.uniform(0, math.tau)
        L = rnd.uniform(0.8, 2.4)
        g.beam((x, y, 0.05), (x + math.cos(a) * L, y + math.sin(a) * L, 0.08), 0.05, 0.05, M["bark"])
        for k in range(3):
            t = rnd.uniform(0.2, 0.9)
            bx, by = x + math.cos(a) * L * t, y + math.sin(a) * L * t
            g.beam((bx, by, 0.07), (bx + rnd.uniform(-0.5, 0.5), by + rnd.uniform(-0.5, 0.5), 0.1), 0.02, 0.02, M["bark"])
    g.beam((-30.0, 7.0, 0.2), (-24.0, 11.5, 6.0), 0.3, 0.3, M["bark"])
    for k in range(4):
        c = Vector((-30.0, 7.0, 0.2)) + (Vector((6.0, 4.5, 5.8))) * (0.3 + 0.18 * k)
        g.cyl(tuple(c), 1.0 - 0.18 * k, 0.9, M["yew"], n=7, r2=0.1, rot=(-40, 30, 0))
    # KESSEL HILL 2: the sign at the foot of the hill road, turned towards the town
    sx, sy = -18.0, 4.0
    for dy in (-0.85, 0.85):
        g.cyl((sx, sy + dy, 1.0), 0.04, 2.6, M["steel_mid"], n=8)
    g.box((sx, sy, 1.95), (0.03, 2.38, 1.03), M["tape_b"])
    g.box((sx - 0.012, sy, 1.95), (0.02, 2.22, 0.87), M["tape_w"])
    K.text(g, "KESSEL HILL", K.frame_m((sx - 0.03, sy + 0.1, 2.0), (0, -1, 0), (0, 0, 1)), 0.36, M["tape_b"], extrude=0.006)
    K.text(g, "2", K.frame_m((sx - 0.03, sy - 0.88, 2.0), (0, -1, 0), (0, 0, 1)), 0.5, M["tape_b"], extrude=0.006)
    # a lamp post, a grit bin, the road going up the hill out of the shot (a turn into the cliff)
    g.cyl((-4.0, 4.2, 3.5), 0.08, 7.0, M["steel_mid"], n=10)
    g.beam((-4.0, 4.2, 7.0), (-4.0, 3.0, 7.2), 0.06, 0.06, M["steel_mid"])
    g.box((-4.0, 2.8, 7.15), (0.5, 0.3, 0.12), M["steel_mid"])
    g.box((-8.0, 4.3, 0.4), (0.8, 0.6, 0.8), M["concrete"])
    # high on the clifftop, the fortress against the morning: its walls, the towers, a thread of smoke
    for (x0, x1, z0, h) in ((-60.0, 60.0, 52.0, 9.0),):
        g.box(((x0 + x1) / 2, CLIFF_Y + 18.0, z0 + h / 2), (x1 - x0, 2.0, h), M["far_dark"])
        D.merlons(g, x0, x1, CLIFF_Y + 17.0, z0 + h, 0.8, M["far_dark"], w=0.9, gap=0.8, h=1.0)
    for (tx, r, h) in ((-42.0, 3.4, 18.0), (-14.0, 2.8, 14.0), (16.0, 3.8, 22.0), (44.0, 3.0, 16.0)):
        D.tower(g, (tx, CLIFF_Y + 19.0, 52.0), r, h, M["far_dark"], M["far_dark"], M["far_dark"], slits=3, crenel=True)
    for k in range(10):
        g.cyl((18.0 + k * 1.2, CLIFF_Y + 20.0, 78.0 + k * 3.0), 1.5 + k * 0.6, 2.5, M["far2"], n=10, r2=1.2 + k * 0.6)
    return g.finish(coll)


def build_police(coll, M):
    g = S.Group("EpiPolice")
    lights = []
    for k, (at, yaw) in enumerate(POLICE):
        police_car(g, M, lights, at, yaw, k)
    # the van: a box van in the police's white, its own bar
    (vx, vy, vz), vyaw = VAN
    L = K.Local(g, K.place((vx, vy, vz), vyaw))
    L.box((-0.4, 0, 1.35), (4.2, 1.95, 2.1), M["van"])
    L.box((2.05, 0, 0.95), (0.9, 1.9, 1.3), M["van"])
    L.box((2.0, 0, 1.75), (0.7, 1.7, 0.6), M["car_glass"], rot=(0, -28, 0))
    for s in (-1, 1):
        L.box((1.95, s * 0.98, 1.55), (0.6, 0.02, 0.5), M["car_glass"])
        L.box((-0.4, s * 0.985, 0.95), (4.2, 0.012, 0.2), M["stripe"])
        for wx in (-1.6, 1.7):
            L.cyl((wx, s * 0.86, 0.36), 0.36, 0.25, M["tyre"], rot=(90, 0, 0), n=18)
            L.cyl((wx, s * 0.99, 0.36), 0.2, 0.02, M["wheel_trim"], rot=(90, 0, 0), n=14)
    L.box((2.52, 0, 0.6), (0.06, 1.8, 0.2), M["rubber"])
    L.box((-2.52, 0, 1.3), (0.02, 1.9, 1.9), M["shut"])
    L.box((0.3, 0, 2.45), (0.3, 1.3, 0.09), M["bar"])
    for s in (-1, 1):
        gl = S.Group("PoliceLight_%d" % (6 + (0 if s > 0 else 1)))
        p = L.p((0.3, s * 0.35, 2.52))
        gl.box(tuple(p), (0.22, 0.36, 0.08), M["lens"], rot=(0, 0, vyaw))
        lights.append(K.finish_at(gl, coll, p))
    # Harrow's car: an old grey saloon, its lamps off
    K.saloon(g, M, DET_CAR, 0.0, M["det"], M["head_lamp_off"])
    # the tent at the cliff's foot, the tape round it and across the verge, cones, a lamp on a tripod
    tx, ty = TENT
    tz = (ty - 6.4) ** 1.6 * 0.25
    g.box((tx, ty, tz + 1.0), (2.6, 2.6, 2.0), M["tent"])
    g.prism([(tx - 1.32, tz + 2.0), (tx + 1.32, tz + 2.0), (tx, tz + 2.7)], ty - 1.32, ty + 1.32, M["tent"])
    g.box((tx, ty - 1.31, tz + 0.95), (0.9, 0.02, 1.85), M["shut"])
    for s in (-1, 1):
        g.beam((tx + s * 1.3, ty - 1.3, tz + 2.0), (tx + s * 2.2, ty - 2.4, tz), 0.01, 0.01, M["rope"])
    posts = [(8.0, 4.2), (8.0, 9.0), (11.0, 12.0), (20.0, 12.0), (23.0, 9.0), (23.0, 4.2), (15.5, 3.6)]
    for (px, py) in posts:
        pz = (py - 6.4) ** 1.6 * 0.25 if py > 6.4 else 0.0
        g.cyl((px, py, pz + 0.55), 0.03, 1.1, M["tape_w"], n=6)
        g.cyl((px, py, pz + 0.02), 0.12, 0.04, M["tape_b"], n=8)
    for (a, b) in zip(posts, posts[1:] + posts[:1]):
        za = ((a[1] - 6.4) ** 1.6 * 0.25 if a[1] > 6.4 else 0.0) + 0.95
        zb = ((b[1] - 6.4) ** 1.6 * 0.25 if b[1] > 6.4 else 0.0) + 0.95
        A_, B_ = Vector((a[0], a[1], za)), Vector((b[0], b[1], zb))
        n = int((B_ - A_).length / 0.4)
        for k in range(n):
            p0 = A_ + (B_ - A_) * (k / n)
            p1 = A_ + (B_ - A_) * ((k + 1) / n)
            sag = -0.08 * math.sin(math.pi * (k + 0.5) / n)
            g.beam(tuple(p0 + Vector((0, 0, sag))), tuple(p1 + Vector((0, 0, sag))), 0.005, 0.07, M["tape_w"] if k % 2 == 0 else M["tape_b"])
    for (cx, cy) in ((6.0, 2.6), (8.0, 2.4), (10.0, 2.7), (-6.0, -2.6), (-8.0, -2.2), (26.0, -1.0), (28.0, -1.8)):
        g.box((cx, cy, 0.02), (0.38, 0.38, 0.04), M["cone"])
        g.cyl((cx, cy, 0.37), 0.15, 0.66, M["cone"], n=12, r2=0.025)
        g.cyl((cx, cy, 0.42), 0.105, 0.12, M["tape_b"], n=12, r2=0.09)
    lx, ly = 11.5, 7.8
    lz = (ly - 6.4) ** 1.6 * 0.25
    for k in range(3):
        a = k / 3 * math.tau
        g.beam((lx + math.cos(a) * 0.5, ly + math.sin(a) * 0.5, lz), (lx, ly, lz + 1.6), 0.03, 0.03, M["steel_mid"])
    g.beam((lx, ly, lz + 1.6), (lx, ly, lz + 2.6), 0.04, 0.04, M["steel_mid"])
    g.box((lx + 0.1, ly + 0.15, lz + 2.65), (0.5, 0.25, 0.35), M["steel_mid"], rot=(0, 0, 50))
    g.box((lx + 0.24, ly + 0.3, lz + 2.65), (0.4, 0.02, 0.28), M["tape_w"], rot=(0, 0, 50))
    # an A-frame barrier closing the hill road's lane, an evidence marker or two, a blanket on the bonnet
    for k, (bx, by) in enumerate(((-16.0, 1.6), (-16.0, -1.6))):
        for s in (-1, 1):
            g.beam((bx + s * 0.6, by - 0.3, 0.0), (bx + s * 0.6, by, 1.0), 0.04, 0.04, M["steel_mid"])
            g.beam((bx + s * 0.6, by + 0.3, 0.0), (bx + s * 0.6, by, 1.0), 0.04, 0.04, M["steel_mid"])
        for j in range(4):
            g.box((bx - 0.6 + j * 0.4 + 0.2, by, 0.95), (0.4, 0.04, 0.22), M["tape_w"] if j % 2 == 0 else M["tape_b"])
    for (ex, ey) in ((13.2, 6.0), (17.4, 7.0), (14.0, 3.4)):
        ez = (ey - 6.4) ** 1.6 * 0.25 if ey > 6.4 else 0.0
        g.prism([(ex - 0.08, ez), (ex + 0.08, ez), (ex, ez + 0.14)], ey - 0.05, ey + 0.05, M["tape_w"])
    return g.finish(coll), lights


# ---------------------------------------------------------------- B: the cemetery

def ground_b(x, y):
    """The cemetery's slope: level along the path and Elena's row, rising up the hill behind."""
    lx, ly = x - B.x, y - B.y
    z = max(0.0, ly - 2.6) * 0.17 + 0.0012 * max(0.0, ly - 2.6) ** 2 * 6
    z += 0.08 * noise.noise(Vector((lx * 0.4, ly * 0.4, 3.0)))
    if ly < -4.0:
        z -= (-4.0 - ly) ** 1.3 * 0.35
    return z


def headstone(g, M, c, kind, scale=1.0, lean=0.0, old=False, yaw=0.0):
    """One grave's stone of a given kind, its face to -y."""
    m = M["stone_old"] if old else (M["marble"] if kind in (2, 8) and rnd.random() < 0.6 else M["stone"])
    x, y, z = c
    L = K.Local(g, Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(lean), 4, "X"))
    s = scale
    w, h, t = 0.58 * s, 0.8 * s, 0.12 * s

    def inscription(top, lines=4):
        for k in range(lines):
            ww = w * (0.7 if k == 0 else rnd.uniform(0.35, 0.62))
            L.box((0, -t / 2 - 0.002, top - k * 0.07 * s), (ww, 0.004, 0.028 * s), M["inscr"])
    if kind in (0, 1, 2):
        L.box((0, 0, 0.05), (w + 0.14, t + 0.14, 0.1), m)                            # the base
    if kind == 0:                                                                     # rounded top
        L.box((0, 0, 0.1 + (h - w / 2) / 2), (w, t, h - w / 2), m)
        L.cyl((0, 0, 0.1 + h - w / 2), w / 2, t, m, rot=(90, 0, 0), n=18)
        inscription(0.1 + h - w * 0.55)
    elif kind == 1:                                                                   # gothic, pointed
        L.box((0, 0, 0.1 + (h - 0.25 * s) / 2), (w, t, h - 0.25 * s), m)
        tg = S.Group("tmp")
        tg.prism([(-w / 2, h - 0.25 * s + 0.1), (w / 2, h - 0.25 * s + 0.1), (0.0, h + 0.18 * s)], -t / 2, t / 2, m)
        L.absorb(tg)
        inscription(0.1 + h - 0.35 * s)
    elif kind == 2:                                                                   # shouldered, flat top
        L.box((0, 0, 0.1 + h / 2 - 0.05), (w, t, h - 0.1), m)
        L.box((0, 0, 0.1 + h - 0.02), (w * 0.62, t, 0.1), m)
        for sg in (-1, 1):
            L.box((sg * w * 0.38, 0, 0.1 + h - 0.08), (w * 0.3, t, 0.08), m, rot=(0, sg * 25, 0))
        inscription(0.1 + h - 0.2 * s)
    elif kind == 3:                                                                   # a cross on steps
        L.box((0, 0, 0.08), (0.6 * s, 0.6 * s, 0.16), m)
        L.box((0, 0, 0.22), (0.42 * s, 0.42 * s, 0.12), m)
        L.box((0, 0, 0.28 + 0.55 * s), (0.1 * s, 0.1 * s, 1.1 * s), m)
        L.box((0, 0, 0.28 + 0.8 * s), (0.5 * s, 0.1 * s, 0.1 * s), m)
    elif kind == 4:                                                                   # a Celtic cross
        L.box((0, 0, 0.15), (0.5 * s, 0.3 * s, 0.3), m)
        L.box((0, 0, 0.3 + 0.6 * s), (0.12 * s, 0.1 * s, 1.2 * s), m, rot=None)
        L.box((0, 0, 0.3 + 0.9 * s), (0.6 * s, 0.1 * s, 0.12 * s), m)
        L.torus((0, 0, 0.3 + 0.9 * s), 0.19 * s, 0.03 * s, m, rot=(90, 0, 0), n1=18, n2=5)
        for k in range(3):
            L.box((0, -0.05 * s - 0.004, 0.45 + k * 0.12 * s), (0.08 * s, 0.004, 0.08 * s), M["inscr"], rot=(0, 45, 0))
    elif kind == 5:                                                                   # an obelisk
        L.box((0, 0, 0.12), (0.7 * s, 0.7 * s, 0.24), m)
        L.box((0, 0, 0.36), (0.55 * s, 0.55 * s, 0.24), m)
        L.cyl((0, 0, 0.48 + 0.9 * s), 0.26 * s, 1.8 * s, m, n=4, r2=0.16 * s, rot=(0, 0, 45))
        L.cyl((0, 0, 0.48 + 1.8 * s + 0.12 * s), 0.16 * s, 0.24 * s, m, n=4, r2=0.0, rot=(0, 0, 45))
        inscription(0.42, 2)
    elif kind == 6:                                                                   # a ledger slab, kerbed
        L.box((0, -0.9 * s, 0.06), (0.9 * s, 1.9 * s, 0.12), m)
        L.box((0, -0.9 * s, 0.13), (0.8 * s, 1.8 * s, 0.04), M["stone_old"] if not old else m)
        for k in range(3):
            L.box((0, -0.3 * s - k * 0.12, 0.152), (0.4 * s, 0.05, 0.004), M["inscr"])
    elif kind == 7:                                                                   # an open book on a plinth
        L.box((0, 0, 0.12), (0.6 * s, 0.4 * s, 0.24), m)
        for sg in (-1, 1):
            L.box((sg * 0.15 * s, -0.02, 0.32), (0.3 * s, 0.32 * s, 0.05), m, rot=(-30, sg * 8, 0))
    elif kind == 8:                                                                   # kerbed grave with a small upright
        for sg in (-1, 1):
            L.box((sg * 0.4 * s, -0.9 * s, 0.06), (0.1, 1.9 * s, 0.12), m)
        L.box((0, -1.8 * s, 0.06), (0.9 * s, 0.1, 0.12), m)
        L.box((0, -0.9 * s, 0.03), (0.7 * s, 1.7 * s, 0.06), M["gravel"])
        L.box((0, 0, 0.1 + h * 0.35), (w, t, h * 0.7), m)
        L.cyl((0, 0, 0.1 + h * 0.7), w / 2, t, m, rot=(90, 0, 0), n=14)
        inscription(0.1 + h * 0.6, 3)


def angel(g, M, c, yaw=0.0, s=1.0):
    """A mourning angel on a plinth: robe, folded wings, the head bowed, hands together."""
    m = M["stone_old"]
    L = K.Local(g, Matrix.Translation(c) @ Matrix.Rotation(math.radians(yaw), 4, "Z"))
    L.box((0, 0, 0.35), (0.8 * s, 0.6 * s, 0.7), m)
    L.box((0, 0, 0.74), (0.9 * s, 0.7 * s, 0.08), m)
    L.cyl((0, 0, 0.78 + 0.55 * s), 0.3 * s, 1.1 * s, m, n=14, r2=0.17 * s)            # the robe
    for k in range(5):                                                               # the robe's folds
        L.box(((k - 2) * 0.09 * s, -0.2 * s - 0.03 * (2 - abs(k - 2)), 0.78 + 0.5 * s), (0.04 * s, 0.04 * s, 0.95 * s), m, rot=(0, (k - 2) * 3, 0))
    L.cyl((0, 0, 0.78 + 1.25 * s), 0.17 * s, 0.35 * s, m, n=12, r2=0.13 * s)            # the chest
    L.ball((0, -0.06 * s, 0.78 + 1.55 * s), 0.11 * s, m, sub=2, squash=(0.9, 1.0, 1.15))  # the head, bowed
    L.box((0, -0.18 * s, 0.78 + 1.18 * s), (0.1 * s, 0.1 * s, 0.18 * s), m, rot=(30, 0, 0))   # the hands
    for sg in (-1, 1):
        L.beam((sg * 0.14 * s, -0.05 * s, 0.78 + 1.38 * s), (sg * 0.06 * s, -0.17 * s, 0.78 + 1.15 * s), 0.07 * s, 0.07 * s, m)
        L.ball((sg * 0.22 * s, 0.14 * s, 0.78 + 1.2 * s), 0.26 * s, m, sub=2, squash=(0.35, 0.6, 2.2))   # the wings, folded
        L.ball((sg * 0.2 * s, 0.18 * s, 0.78 + 1.62 * s), 0.13 * s, m, sub=1, squash=(0.4, 0.8, 1.3))
    L.cyl((0, 0, 0.78 + 0.03), 0.34 * s, 0.06, m, n=14)


def yew(g, M, c, h):
    x, y, z = c
    g.cyl((x, y, z + 0.6), 0.25, 1.2, M["bark"], n=8)
    for k in range(6):
        t = k / 5
        r = h * (0.38 - 0.22 * t) * rnd.uniform(0.85, 1.1)
        K.ball(g, (x + rnd.uniform(-0.4, 0.4), y + rnd.uniform(-0.4, 0.4), z + h * (0.28 + 0.62 * t)), r, M["yew"], sub=1, squash=(1, 1, 0.9), jitter=0.3, seed=x + k)


def dry_wall(g, M, pts, h, m):
    """A coursed stone wall along a polyline on the ground, with its coping stones set on edge."""
    for (a, b) in zip(pts, pts[1:]):
        a, b = Vector(a), Vector(b)
        L = (b - a).length
        n = max(1, int(L / 0.9))
        d = (b - a).normalized()
        yaw = math.degrees(math.atan2(d.y, d.x))
        for k in range(n):
            p = a + d * ((k + 0.5) * L / n)
            z = ground_b(p.x, p.y)
            g.box((p.x, p.y, z + h / 2 - 0.1), (L / n + 0.02, 0.5, h + 0.2), m, rot=(0, 0, yaw))
            for j in range(4):
                q = p + d * ((j - 1.5) * L / n / 4)
                g.box((q.x, q.y, z + h + 0.1), (L / n / 4 - 0.03, 0.42, 0.22), M["coping"], rot=(rnd.uniform(-4, 4), 0, yaw))


def build_cemetery(coll, M):
    g = S.Group("EpiCemetery")
    o = B
    K.sheet(g, o.x - 40, o.x + 40, o.y - 7.5, o.y + 32, ground_b, M["grass"], res=1.0)
    K.sheet(g, o.x - 140, o.x + 140, o.y - 90, o.y - 7.5, lambda x, y: ground_b(x, y) - 0.05, M["grass_far"], res=6.0)
    for side in (-1, 1):
        K.sheet(g, o.x + side * 40 - (100 if side < 0 else 0), o.x + side * 40 + (100 if side > 0 else 0), o.y - 7.5, o.y + 90,
                lambda x, y: ground_b(x, y) - 0.05, M["grass_far"], res=6.0)
    K.sheet(g, o.x - 40, o.x + 40, o.y + 32, o.y + 90, lambda x, y: ground_b(x, y) - 0.05, M["grass_far"], res=4.0)
    # the paths: gravel from the gate along the front, and one up the hill
    K.sheet(g, o.x - 32, o.x + 30, o.y + PATH_Y - 1.0, o.y + PATH_Y + 1.0, lambda x, y: ground_b(x, y) + 0.02, M["gravel"], res=1.0)
    K.sheet(g, o.x + 7.2, o.x + 8.8, o.y + PATH_Y + 1.0, o.y + 24.0, lambda x, y: ground_b(x, y) + 0.02, M["gravel"], res=0.8)
    for x in range(-32, 31, 1):                                                       # path edging
        for sy in (-1, 1):
            p = (o.x + x + 0.5, o.y + PATH_Y + sy * 1.02)
            g.box((p[0], p[1], ground_b(*p) + 0.03), (0.98, 0.06, 0.08), M["coping"])
    # the walls round it: high at the back, low in front, down the sides; the gate piers
    W0, W1, F, Bk = o.x - 33.0, o.x + 33.0, o.y - 6.8, o.y + 26.0
    dry_wall(g, M, [(W0, Bk), (W1, Bk)], 1.5, M["wall_x"])
    dry_wall(g, M, [(W0, F), (o.x + GATE_X - 1.5, F)], 0.85, M["wall_x"])
    dry_wall(g, M, [(o.x + GATE_X - 1.5, F), (W1, F)], 0.85, M["wall_x"])
    dry_wall(g, M, [(W1, F), (W1, Bk)], 1.2, M["wall_y"])
    dry_wall(g, M, [(W0, F), (W0, o.y + PATH_Y - 1.5)], 1.2, M["wall_y"])
    dry_wall(g, M, [(W0, o.y + PATH_Y + 1.5), (W0, Bk)], 1.2, M["wall_y"])
    for sy in (-1, 1):                                                                # the gate's piers
        py = o.y + PATH_Y + sy * 1.5
        z = ground_b(W0, py)
        g.box((W0, py, z + 1.0), (0.7, 0.7, 2.0), M["wall_y"])
        g.box((W0, py, z + 2.07), (0.82, 0.82, 0.14), M["coping"])
        g.cyl((W0, py, z + 2.35), 0.32, 0.42, M["coping"], n=4, r2=0.02, rot=(0, 0, 45))
        K.ball(g, (W0, py, z + 2.62), 0.12, M["coping"], sub=1)
    # yews, dark and old; a holly; the chapel's gable behind the wall
    for (x, y, h) in ((-22, 22, 7.5), (-8, 23.5, 9.0), (13, 22.5, 8.0), (27, 20, 6.5), (-28, 4, 5.5), (26, 4.5, 6.0),
                      (-16, 9.5, 5.0), (18, 12.5, 5.5), (-30, -4.5, 4.5), (30, -4.5, 4.0)):
        yew(g, M, (o.x + x, o.y + y, ground_b(o.x + x, o.y + y)), h)
    cx, cy = o.x - 12.0, o.y + 31.0
    cz = ground_b(cx, cy)
    g.box((cx, cy, cz + 3.0), (7.0, 10.0, 6.0), M["wall_x"])
    g.prism([(cx - 3.8, cz + 6.0), (cx + 3.8, cz + 6.0), (cx, cz + 9.4)], cy - 5.2, cy + 5.0, M["roof"])
    g.box((cx, cy - 5.05, cz + 3.0), (1.2, 0.1, 2.6), M["door"])
    g.cyl((cx, cy - 5.06, cz + 4.3), 0.6, 0.1, M["door"], rot=(90, 0, 0), n=14)
    g.box((cx, cy - 5.1, cz + 9.6), (0.9, 0.5, 1.2), M["wall_x"])                     # the bell-cote
    g.box((cx, cy - 5.36, cz + 9.5), (0.45, 0.04, 0.6), M["door"])
    g.cyl((cx, cy - 5.1, cz + 10.5), 0.08, 0.6, M["iron"], n=6)
    g.box((cx, cy - 5.1, cz + 10.6), (0.4, 0.06, 0.06), M["iron"])
    # the bench on the slope, looking out over the valley, and its plaque; a water tap, a bin, a rake
    bx, by = o.x + 19.0, o.y + 9.0
    bz = ground_b(bx, by)
    for k in range(4):
        g.box((bx, by - 0.2 + k * 0.12, bz + 0.45), (1.7, 0.1, 0.04), M["bench"])
    for k in range(3):
        g.box((bx, by + 0.26, bz + 0.6 + k * 0.12), (1.7, 0.04, 0.09), M["bench"], rot=(10, 0, 0))
    for dx in (-0.75, 0.75):
        g.box((bx + dx, by, bz + 0.22), (0.06, 0.5, 0.44), M["iron"])
        g.box((bx + dx, by + 0.28, bz + 0.62), (0.06, 0.05, 0.5), M["iron"], rot=(10, 0, 0))
        g.box((bx + dx, by - 0.05, bz + 0.62), (0.06, 0.45, 0.04), M["iron"])
    g.box((bx, by + 0.31, bz + 0.92), (0.2, 0.01, 0.06), M["marble"], rot=(10, 0, 0))
    g.cyl((o.x + 9.6, o.y + PATH_Y + 1.4, 0.45), 0.04, 0.9, M["iron"], n=6)
    g.box((o.x + 9.6, o.y + PATH_Y + 1.32, 0.86), (0.12, 0.08, 0.04), M["iron"])
    g.cyl((o.x + 10.6, o.y + PATH_Y + 1.5, 0.4), 0.25, 0.8, M["iron"], n=12)
    g.beam((o.x + 11.4, o.y + PATH_Y + 1.6, 0.0), (o.x + 11.5, o.y + PATH_Y + 1.7, 1.6), 0.03, 0.03, M["bench"])
    # wet grass: tufts round everything
    tufts(g, M, (o.x, o.y + 8.0), 32.0, 15.0, 700, ground_b)
    # white flowers on a few graves, and the bunch at Elena's
    for (fx, fy) in ((-6.6, 4.0), (3.4, 6.6), (11.6, 1.2), (-14.0, 9.2)):
        flowers(g, M, (o.x + fx, o.y + fy - 0.3, ground_b(o.x + fx, o.y + fy - 0.3)), n=5, h=0.25)
    ex, ey = o.x + ELENA.x, o.y + ELENA.y
    flowers(g, M, (ex - 0.2, ey - 0.26, 0.1), n=9, h=0.38, wrap=False)
    g.cyl((ex - 0.2, ey - 0.26, 0.08), 0.075, 0.16, M["marble"], n=12, r2=0.06)       # the vase on the base
    for k in range(3):                                                               # a few petals fallen on the grass
        g.cyl((ex + rnd.uniform(-0.4, 0.4), ey - 0.5 - rnd.uniform(0, 0.4), 0.012), 0.02, 0.004, M["petal"], n=6)
    return g.finish(coll)


def build_stones(coll, M):
    g = S.Group("EpiStones")
    o = B
    rows = [1.6 + k * 2.6 for k in range(8)]
    for ri, ry in enumerate(rows):
        x = -29.0 + rnd.uniform(0, 0.8)
        while x < 29.0:
            if ri == 0 and abs(x - ELENA.x) < 2.2:
                x += 1.7
                continue
            if 6.4 < x < 9.6:
                x += 1.7
                continue
            if rnd.random() < 0.12:
                x += 1.7
                continue
            old = ri >= 4 or rnd.random() < 0.15
            kind = rnd.choices(range(9), weights=(6, 3, 4, 2, 1, 0.6, 1.4, 1, 1.6))[0]
            y = ry + rnd.uniform(-0.12, 0.12)
            p = (o.x + x, o.y + y, ground_b(o.x + x, o.y + y) - 0.03)
            lean = rnd.uniform(-7, 7) if old else rnd.uniform(-1.5, 1.5)
            if kind in (6, 8):                                                        # lying graves follow the slope
                slope = (ground_b(o.x + x, o.y + y) - ground_b(o.x + x, o.y + y - 1.9)) / 1.9
                lean = math.degrees(math.atan(slope))
                p = (p[0], p[1], p[2] - 0.04)
            headstone(g, M, p, kind, scale=rnd.uniform(0.85, 1.15), lean=lean, old=old, yaw=rnd.uniform(-4, 4))
            x += rnd.uniform(1.5, 2.1)
    for (ax, ay, yaw) in ((-12.4, 4.2, 6.0), (15.2, 11.9, -8.0), (-21.0, 14.6, 4.0)):
        angel(g, M, (o.x + ax, o.y + ay, ground_b(o.x + ax, o.y + ay) - 0.05), yaw=yaw)
    return g.finish(coll)


def build_elena(coll, M):
    """Elena's stone: pale, new, a flat top (a walkie-talkie stands on it), her name cut in it."""
    g = S.Group("ElenaStone")
    o = B + ELENA
    m = M["marble"]
    w, h, t = STONE_W, STONE_H, STONE_T
    g.box((o.x, o.y, 0.06), (w + 0.22, t + 0.3, 0.12), m)                              # the base
    g.box((o.x, o.y, 0.13), (w + 0.12, t + 0.18, 0.02), m)
    g.box((o.x, o.y, 0.12 + (h - 0.12) / 2), (w, t, h - 0.12), m)
    g.box((o.x, o.y, h - 0.05), (w * 0.58, t, 0.1), m)                                 # the flat top
    for sg in (-1, 1):
        g.box((o.x + sg * w * 0.36, o.y, h - 0.105), (w * 0.33, t, 0.06), m, rot=(0, sg * 22, 0))
    g.box((o.x, o.y - t / 2 - 0.003, h - 0.24), (w * 0.86, 0.004, h * 0.62), M["stone"])   # the panel, honed
    face = K.frame_m((o.x, o.y - t / 2 - 0.006, h - 0.3), (1, 0, 0), (0, 0, 1))
    K.text(g, "ELENA", face, 0.105, M["inscr"], extrude=0.002)
    K.text(g, "BELOVED WIFE AND MOTHER", K.frame_m((o.x, o.y - t / 2 - 0.006, h - 0.42), (1, 0, 0), (0, 0, 1)), 0.034, M["inscr"], extrude=0.0015)
    g.box((o.x, o.y - t / 2 - 0.006, h - 0.36), (0.22, 0.004, 0.004), M["inscr"])
    for sg in (-1, 1):                                                                 # a cut border
        g.box((o.x + sg * w * 0.43, o.y - t / 2 - 0.005, h - 0.4), (0.006, 0.004, h * 0.5), M["inscr"])
    return K.finish_at(g, coll, (o.x, o.y, 0.0))


# ---------------------------------------------------------------- C: home

def build_house(coll, M):
    g = S.Group("EpiHouse")
    o = C
    X0, X1, Y0, Y1 = o.x + HX0, o.x + HX1, o.y + HY0, o.y + HY1
    eave, ridge = 5.6, 8.3
    hx0, hx1 = o.x + HALL_X0, o.x + HALL_X1
    bd0, bd1 = o.x + BACK_DOOR[0], o.x + BACK_DOOR[1]
    # the back wall, its openings: the back door, the kitchen window, the French doors, upstairs
    holes = [(bd0, bd1, 0.15, 2.18), (o.x + 3.2, o.x + 4.8, 1.0, 2.2), (o.x - 4.3, o.x - 2.5, 0.15, 2.18),
             (o.x - 3.8, o.x - 2.4, 3.3, 4.6), (o.x + 2.6, o.x + 4.1, 3.3, 4.6), (o.x + 6.8, o.x + 7.6, 3.6, 4.4)]
    wall(g, "x", Y0 + 0.15, X0, X1, 0.0, eave, 0.3, holes, M["brick_x"])
    wall(g, "x", Y1 - 0.15, X0, X1, 0.0, eave, 0.3, [(o.x + FRONT_DOOR[0], o.x + FRONT_DOOR[1], FLOOR, 2.2), (o.x + FRONT_DOOR[0], o.x + FRONT_DOOR[1], 2.3, 2.55)], M["brick_x"])
    for x in (X0, X1):
        wall(g, "y", x, Y0, Y1, 0.0, eave, 0.3, [], M["brick_y"])
        for k in range(12):                                                            # the gable, course by course
            z = eave + k * (ridge - eave) / 12
            half = (Y1 - Y0) / 2 * (1 - k / 12)
            g.box((x, (Y0 + Y1) / 2, z + (ridge - eave) / 24), (0.3, 2 * half, (ridge - eave) / 12), M["brick_y"])
    # the roof: two tiled pitches, the ridge, the barge boards, the gutters and the downpipe, the chimney
    pitch = math.degrees(math.atan2(ridge - eave, (Y1 - Y0) / 2))
    run = math.hypot(ridge - eave, (Y1 - Y0) / 2) + 0.4
    for sg in (-1, 1):
        cy = (Y0 + Y1) / 2 - sg * ((Y1 - Y0) / 4 + 0.08)
        g.box(((X0 + X1) / 2, cy, (eave + ridge) / 2 + 0.12), (X1 - X0 + 0.5, run, 0.12), M["roof"], rot=(-sg * pitch, 0, 0))
        for k in range(14):
            yy = (Y0 + Y1) / 2 - sg * (0.25 + k * 0.38)
            zz = ridge + 0.18 - (0.25 + k * 0.38) * math.tan(math.radians(pitch))
            g.box(((X0 + X1) / 2, yy, zz), (X1 - X0 + 0.5, 0.03, 0.035), M["felt"])
        gy = Y0 - 0.25 if sg > 0 else Y1 + 0.25
        g.box(((X0 + X1) / 2, gy, eave - 0.05), (X1 - X0 + 0.5, 0.14, 0.12), M["frame"])
    g.box(((X0 + X1) / 2, (Y0 + Y1) / 2, ridge + 0.25), (X1 - X0 + 0.5, 0.3, 0.2), M["roof"])
    g.cyl((X1 - 0.3, Y0 - 0.28, eave / 2), 0.05, eave, M["frame"], n=8)
    g.box((X0 + 2.0, (Y0 + Y1) / 2 + 1.0, ridge + 0.6), (0.9, 0.6, 1.8), M["brick_x"])
    g.box((X0 + 2.0, (Y0 + Y1) / 2 + 1.0, ridge + 1.55), (1.0, 0.7, 0.12), M["coping"])
    for dx in (-0.2, 0.2):
        g.cyl((X0 + 2.0 + dx, (Y0 + Y1) / 2 + 1.0, ridge + 1.8), 0.1, 0.4, M["concrete"], n=10, r2=0.08)
    # window joinery, sills and lintels; the glass shows the morning
    for (a, b, z0, z1) in holes[1:]:
        g.box(((a + b) / 2, Y0 - 0.05, z0 - 0.04), (b - a + 0.2, 0.2, 0.08), M["coping"])
        g.box(((a + b) / 2, Y0, z1 + 0.08), (b - a + 0.2, 0.32, 0.16), M["coping"])
        if z0 > 0.5:
            g.box(((a + b) / 2, Y0 + 0.08, (z0 + z1) / 2), (b - a, 0.02, z1 - z0), M["win"])
            for xm in (a + 0.03, b - 0.03, (a + b) / 2):
                g.box((xm, Y0 + 0.06, (z0 + z1) / 2), (0.06, 0.07, z1 - z0), M["frame"])
            for zm in (z0 + 0.03, z1 - 0.03, z0 + (z1 - z0) * 0.62):
                g.box(((a + b) / 2, Y0 + 0.06, zm), (b - a, 0.07, 0.06), M["frame"])
        else:                                                                         # the French doors, shut
            for k, (xa, xb) in enumerate(((a, (a + b) / 2), ((a + b) / 2, b))):
                g.box(((xa + xb) / 2, Y0 + 0.08, (z0 + z1) / 2), (xb - xa, 0.02, z1 - z0), M["win"])
                for xm in (xa + 0.04, xb - 0.04):
                    g.box((xm, Y0 + 0.06, (z0 + z1) / 2), (0.08, 0.08, z1 - z0), M["frame"])
                for zm in (z0 + 0.06, z1 - 0.04, z0 + 0.5, z0 + 1.2):
                    g.box(((xa + xb) / 2, Y0 + 0.06, zm), (xb - xa, 0.08, 0.08 if zm > z0 + 0.1 else 0.14), M["frame"])
    # the back door's frame, the step, the lamp over it, a boot scraper and a mat
    g.box(((bd0 + bd1) / 2, Y0 - 0.25, 0.06), (1.4, 0.5, 0.12), M["coping"])
    for x in (bd0 - 0.04, bd1 + 0.04):
        g.box((x, Y0 + 0.02, 1.1), (0.08, 0.12, 2.2), M["frame"])
    g.box(((bd0 + bd1) / 2, Y0 + 0.02, 2.2), (bd1 - bd0 + 0.16, 0.12, 0.08), M["frame"])
    g.box((bd1 + 0.35, Y0 - 0.08, 2.35), (0.12, 0.14, 0.2), M["iron"])
    g.box((bd1 + 0.35, Y0 - 0.14, 2.3), (0.1, 0.02, 0.12), M["win"])
    g.box(((bd0 + bd1) / 2, Y0 - 0.65, 0.01), (0.9, 0.55, 0.02), M["runner"])
    # ---- inside: the hall (boards, a runner, the walls papered over a dado, skirting, the ceiling)
    iy0, iy1 = Y0 + 0.3, Y1 - 0.3
    g.box(((hx0 + hx1) / 2, (iy0 + iy1) / 2, FLOOR - 0.06), (hx1 - hx0 + 0.3, iy1 - iy0, 0.12), M["boards"])
    g.box(((hx0 + hx1) / 2 + 0.05, (iy0 + iy1) / 2 + 0.3, FLOOR + 0.004), (0.8, iy1 - iy0 - 1.6, 0.008), M["runner"])
    for sy in (iy0 + 0.8, iy1 - 0.2):
        g.box(((hx0 + hx1) / 2 + 0.05, sy, FLOOR + 0.006), (0.84, 0.03, 0.01), M["skirting"])
    g.box(((hx0 + hx1) / 2, (iy0 + iy1) / 2, CEIL + 0.06), (hx1 - hx0 + 0.3, iy1 - iy0, 0.12), M["plaster"])
    kd0, kd1 = iy0 + 0.5, iy0 + 1.4                                                   # the kitchen's doorway (right wall)
    ld0, ld1 = iy0 + 2.6, iy0 + 3.5                                                   # the living room's (left wall)
    wall(g, "y", hx0 - 0.06, iy0, iy1, FLOOR, CEIL, 0.12, [(ld0, ld1, FLOOR, 2.1)], M["paper"])
    wall(g, "y", hx1 + 0.06, iy0, iy1, FLOOR, CEIL, 0.12, [(kd0, kd1, FLOOR, 2.1)], M["paper"])
    for (x, sgn, d0, d1) in ((hx0, 1, ld0, ld1), (hx1, -1, kd0, kd1)):
        for (a, b) in ((iy0, d0), (d1, iy1)):
            g.box((x + sgn * 0.01, (a + b) / 2, FLOOR + 0.08), (0.03, b - a, 0.16), M["skirting"])
            g.box((x + sgn * 0.01, (a + b) / 2, 0.95), (0.025, b - a, 0.05), M["skirting"])   # the dado rail
            g.box((x + sgn * 0.005, (a + b) / 2, (FLOOR + 0.95) / 2), (0.01, b - a, 0.95 - FLOOR - 0.2), M["plaster"])
        for yy in (d0 - 0.04, d1 + 0.04):                                            # door architraves
            g.box((x + sgn * 0.02, yy, (FLOOR + 2.15) / 2), (0.03, 0.08, 2.15 - FLOOR), M["skirting"])
        g.box((x + sgn * 0.02, (d0 + d1) / 2, 2.15), (0.03, d1 - d0 + 0.16, 0.08), M["skirting"])
    # the living room's door, ajar, and a glimpse of the room
    g.box((hx0 - 0.42, ld0 + 0.05, (FLOOR + 2.1) / 2), (0.8, 0.04, 2.0), M["door"], rot=(0, 0, 70))
    g.box((hx0 - 2.0, ld0 + 0.5, FLOOR - 0.05), (3.8, 3.0, 0.1), M["boards"])
    g.box((hx0 - 3.8, ld0 + 0.5, 1.3), (0.1, 3.0, 2.6), M["paper"])
    g.box((hx0 - 2.0, ld0 + 2.0, 1.3), (3.8, 0.1, 2.6), M["paper"])
    g.box((hx0 - 2.0, ld0 - 1.0, 1.3), (3.8, 0.1, 2.6), M["paper"])
    g.box((hx0 - 2.0, ld0 + 0.5, 2.66), (3.8, 3.0, 0.1), M["plaster"])
    g.box((hx0 - 2.6, ld0 + 1.5, 0.45), (2.0, 0.8, 0.5), M["runner"])                 # the back of a sofa
    # the kitchen through its doorway: units under the window, the table, the fridge, the light on
    kx0, kx1, ky0, ky1 = hx1 + 0.12, o.x + 6.2, iy0, iy0 + 4.0
    g.box(((kx0 + kx1) / 2, (ky0 + ky1) / 2, FLOOR - 0.05), (kx1 - kx0, ky1 - ky0, 0.1), M["tile"])
    g.box(((kx0 + kx1) / 2, (ky0 + ky1) / 2, CEIL + 0.05), (kx1 - kx0, ky1 - ky0, 0.1), M["plaster"])
    wall(g, "x", ky1 + 0.06, kx0, kx1, FLOOR, CEIL, 0.12, [], M["plaster"])
    wall(g, "y", kx1 + 0.06, ky0, ky1, FLOOR, CEIL, 0.12, [], M["plaster"])
    g.box((o.x + 4.4, ky0 + 0.32, 0.5), (3.4, 0.6, 0.86), M["frame"])
    g.box((o.x + 4.4, ky0 + 0.32, 0.95), (3.5, 0.64, 0.04), M["counter"])
    for k in range(5):
        g.box((o.x + 3.0 + k * 0.68, ky0 + 0.63, 0.5), (0.005, 0.01, 0.82), M["shut"])
    g.box((o.x + 4.0, ky0 + 0.35, 0.95), (0.55, 0.4, 0.05), M["steel_mid"])          # the sink, the tap
    g.beam((o.x + 4.0, ky0 + 0.55, 0.97), (o.x + 4.0, ky0 + 0.4, 1.2), 0.02, 0.02, M["chrome"])
    g.box((o.x + 5.7, ky1 - 0.4, 0.9), (0.7, 0.65, 1.8), M["frame"])                  # the fridge
    g.box((o.x + 4.0, ky0 + 2.4, 0.74), (1.2, 0.8, 0.04), M["oak"])                  # the table, two chairs
    for (dx, dy) in ((-0.55, -0.35), (0.55, -0.35), (-0.55, 0.35), (0.55, 0.35)):
        g.box((o.x + 4.0 + dx, ky0 + 2.4 + dy, 0.37), (0.04, 0.04, 0.74), M["oak"])
    for dy in (-0.7, 0.7):
        g.box((o.x + 4.0, ky0 + 2.4 + dy, 0.45), (0.42, 0.42, 0.04), M["oak"])
        g.box((o.x + 4.0, ky0 + 2.4 + dy * 1.27, 0.75), (0.42, 0.03, 0.6), M["oak"])
    g.cyl((o.x + 4.0, ky0 + 2.4, CEIL - 0.3), 0.18, 0.12, M["win"], n=14)
    g.box((o.x + 4.3, ky0 + 2.3, 0.8), (0.12, 0.12, 0.1), M["toy"])                   # a cereal box
    g.cyl((o.x + 3.8, ky0 + 2.5, 0.78), 0.08, 0.04, M["counter"], n=12)              # a bowl
    # the stairs up the right wall from the front door, their balusters and rail
    sx0, sx1 = hx1 - 0.92, hx1
    nst = 13
    for k in range(nst):
        y = iy1 - 1.0 - k * 0.26
        z = FLOOR + (k + 1) * (CEIL - FLOOR) / nst
        g.box(((sx0 + sx1) / 2, y, z / 2 + FLOOR / 2), (sx1 - sx0, 0.27, z - FLOOR), M["oak_y"])
        g.box(((sx0 + sx1) / 2, y - 0.13, z + 0.01), (sx1 - sx0 + 0.02, 0.04, 0.025), M["skirting"])
        g.cyl((sx0 + 0.03, y, z + 0.45), 0.018, 0.9, M["skirting"], n=6)
    g.beam((sx0 + 0.03, iy1 - 0.8, FLOOR + 1.0), (sx0 + 0.03, iy1 - 1.0 - (nst - 1) * 0.26, CEIL + 0.9), 0.06, 0.05, M["oak_y"])
    g.box((sx0 + 0.03, iy1 - 0.85, FLOOR + 0.55), (0.1, 0.1, 1.1), M["oak_y"])       # the newel post
    K.ball(g, (sx0 + 0.03, iy1 - 0.85, FLOOR + 1.15), 0.065, M["oak_y"], sub=1)
    g.box((sx0 - 0.005, (iy1 - 1.0 + iy1 - 1.0 - (nst - 1) * 0.26) / 2, FLOOR + 0.6), (0.01, (nst - 1) * 0.26, 0.8), M["skirting"])
    # the front door from inside: panels, the fanlight, the letterbox, the knob, the chain; the mat
    fd0, fd1 = o.x + FRONT_DOOR[0], o.x + FRONT_DOOR[1]
    g.box(((fd0 + fd1) / 2, iy1 + 0.01, (FLOOR + 2.2) / 2), (fd1 - fd0, 0.05, 2.2 - FLOOR), M["door"])
    for (zz, hh) in ((0.55, 0.7), (1.45, 0.8)):
        for dx in (-0.2, 0.2):
            g.box(((fd0 + fd1) / 2 + dx, iy1 - 0.02, zz), (0.3, 0.02, hh), M["door"])
            g.box(((fd0 + fd1) / 2 + dx, iy1 - 0.025, zz + hh / 2), (0.32, 0.02, 0.02), M["shut"])
    g.box(((fd0 + fd1) / 2, iy1 - 0.02, 1.02), (0.3, 0.02, 0.06), M["chrome"])
    g.cyl((fd1 - 0.12, iy1 - 0.04, 1.0), 0.03, 0.04, M["chrome"], rot=(90, 0, 0), n=10)
    g.beam((fd1 - 0.1, iy1 - 0.03, 1.5), (fd1 + 0.1, iy1 - 0.03, 1.42), 0.006, 0.006, M["chrome"])
    g.box(((fd0 + fd1) / 2, iy1 + 0.01, 2.43), (fd1 - fd0, 0.02, 0.24), M["win"])
    g.box(((fd0 + fd1) / 2, iy1 - 0.4, FLOOR + 0.008), (0.8, 0.5, 0.016), M["runner"])
    # by the door: coats on hooks, a small raincoat among them, wellies, an umbrella
    for k in range(4):
        y = iy1 - 0.55 - k * 0.24
        g.box((hx0 + 0.02, y, 1.7), (0.03, 0.03, 0.05), M["chrome"])
        g.box((hx0 + 0.09, y, 1.25 if k != 2 else 1.42), (0.14, 0.22, 0.9 if k != 2 else 0.5), M["door"] if k != 2 else M["toy"])
    for dy in (0.0, 0.16):
        g.box((hx0 + 0.15, iy1 - 0.6 + dy, FLOOR + 0.14), (0.24, 0.1, 0.28), M["rubber"])
    g.beam((hx0 + 0.1, iy1 - 1.5, FLOOR), (hx0 + 0.05, iy1 - 1.4, FLOOR + 0.85), 0.03, 0.03, M["iron"])
    # pictures down the hall (blank: the game's), a mirror, the radiator, the pendant lamp, the switch
    for (y, z, w, h) in ((CAB_Y - 1.4, 1.62, 0.3, 0.38), (CAB_Y + 1.25, 1.5, 0.26, 0.32), (HY0 + 1.9, 1.55, 0.36, 0.28)):
        yy = o.y + y
        g.box((hx0 + 0.015, yy, z), (0.02, w + 0.05, h + 0.05), M["frame_blk"])
        g.box((hx0 + 0.027, yy, z), (0.006, w, h), M["photo"])
    g.box((hx1 - 0.02, iy0 + 2.4, 1.5), (0.02, 0.5, 0.7), M["frame"])
    g.box((hx1 - 0.032, iy0 + 2.4, 1.5), (0.006, 0.44, 0.64), M["mirror"])
    g.box((hx0 + 0.06, iy0 + 1.0, 0.45), (0.08, 0.9, 0.55), M["frame"])
    for k in range(10):
        g.box((hx0 + 0.105, iy0 + 0.6 + k * 0.09, 0.45), (0.01, 0.02, 0.52), M["skirting"])
    g.cyl(((hx0 + hx1) / 2, CAB_Y + o.y, CEIL - 0.22), 0.006, 0.4, M["rope"], n=4)
    g.cyl(((hx0 + hx1) / 2, CAB_Y + o.y, CEIL - 0.48), 0.2, 0.2, M["felt"], n=16, r2=0.08)
    g.cyl(((hx0 + hx1) / 2, CAB_Y + o.y, CEIL - 0.52), 0.07, 0.08, M["win"], n=10)
    g.box((hx1 - 0.01, iy0 + 0.2, 1.3), (0.01, 0.08, 0.12), M["skirting"])
    return g.finish(coll)


def build_cabinet(coll, M):
    """The cabinet under Elena's photo: its body (HallDrawer), its drawer (HallDrawerFront: out
    along +x into the hall), and the photo (HallPhoto, blank paper: the game can show her)."""
    o = C
    x0 = o.x + HALL_X0
    cy = o.y + CAB_Y
    w, d, h = 0.9, 0.36, 0.8
    g = S.Group("HallDrawer")
    g.box((x0 + d / 2, cy, h - 0.015), (d + 0.03, w + 0.04, 0.03), M["oak"])            # the top
    for sy in (-1, 1):
        g.box((x0 + d / 2, cy + sy * (w / 2 - 0.02), h / 2), (d, 0.04, h - 0.03), M["oak_y"])
    g.box((x0 + 0.01, cy, h / 2), (0.02, w, h - 0.03), M["oak_y"])
    g.box((x0 + d / 2, cy, 0.12), (d, w - 0.04, 0.02), M["oak"])                        # the shelf below
    g.box((x0 + d / 2, cy, h - 0.2), (d, w - 0.04, 0.02), M["oak"])                     # the drawer's rails
    for sy in (-1, 1):
        g.box((x0 + d / 2, cy + sy * (w / 2 - 0.02), 0.06), (d + 0.02, 0.06, 0.12), M["oak"])   # feet
    # on top: a dish for keys, a small vase of white flowers, a lamp
    g.cyl((x0 + 0.2, cy - 0.25, h + 0.015), 0.08, 0.02, M["counter"], n=14, r2=0.06)
    g.cyl((x0 + 0.18, cy + 0.22, h + 0.08), 0.04, 0.16, M["win"], n=12, r2=0.03)
    flowers(g, M, (x0 + 0.18, cy + 0.22, h + 0.12), n=5, r=0.04, h=0.18, wrap=False)
    g.cyl((x0 + 0.17, cy + 0.0, h + 0.012), 0.07, 0.024, M["iron"], n=12)
    g.cyl((x0 + 0.17, cy + 0.0, h + 0.18), 0.012, 0.32, M["iron"], n=6)
    g.cyl((x0 + 0.17, cy + 0.0, h + 0.36), 0.12, 0.14, M["tent"], n=14, r2=0.08)
    # the photo's frame on the wall above
    g.box((x0 + 0.015, cy, 1.55), (0.025, 0.46, 0.56), M["frame_blk"])
    g.box((x0 + 0.03, cy, 1.55), (0.006, 0.4, 0.5), M["photo"])
    body = g.finish(coll)
    gd = S.Group("HallDrawerFront")
    fz = h - 0.12
    gd.box((x0 + d - 0.01, cy, fz), (0.02, w - 0.1, 0.13), M["oak_y"])
    gd.box((x0 + d / 2 + 0.02, cy, fz - 0.055), (d - 0.06, w - 0.14, 0.012), M["oak"])
    for sy in (-1, 1):
        gd.box((x0 + d / 2 + 0.02, cy + sy * (w / 2 - 0.075), fz - 0.01), (d - 0.06, 0.012, 0.1), M["oak"])
    gd.box((x0 + 0.06, cy, fz - 0.01), (0.012, w - 0.14, 0.1), M["oak"])
    gd.cyl((x0 + d + 0.015, cy, fz), 0.018, 0.03, M["chrome"], rot=(0, 90, 0), n=10)
    gd.box((x0 + d - 0.002, cy, fz), (0.004, 0.08, 0.04), M["chrome"])
    front = K.finish_at(gd, coll, (x0 + d, cy, fz))
    gp = S.Group("HallPhoto")
    gp.box((x0 + 0.034, cy, 1.55), (0.004, 0.36, 0.46), M["photo"])
    photo = K.finish_at(gp, coll, (x0 + 0.036, cy, 1.55))
    return body, front, photo


def build_back_door(coll, M):
    """The back door: half-glazed, its hinge at the doorway's -x side; baked standing open into the hall."""
    o = C
    a, b = o.x + BACK_DOOR[0], o.x + BACK_DOOR[1]
    hy = o.y + HY0 + 0.3
    g = S.Group("BackDoor")
    w = b - a
    g.box((a + w / 2, hy, 1.15), (w, 0.045, 2.0), M["door"])
    g.box((a + w / 2, hy - 0.025, 1.6), (w - 0.24, 0.01, 0.8), M["win"])
    for k in (-1, 1):
        g.box((a + w / 2 + k * 0.12, hy - 0.03, 1.6), (0.03, 0.012, 0.8), M["door"])
    g.box((a + w / 2, hy - 0.03, 0.65), (w - 0.2, 0.012, 0.6), M["door"])
    g.cyl((b - 0.08, hy - 0.04, 1.05), 0.025, 0.04, M["chrome"], rot=(90, 0, 0), n=10)
    g.cyl((b - 0.08, hy + 0.04, 1.05), 0.025, 0.04, M["chrome"], rot=(90, 0, 0), n=10)
    ob = K.finish_at(g, coll, (a, hy, 0.15))
    ob.rotation_euler = (0, 0, math.radians(BACK_DOOR_OPEN))
    return ob


BACK_DOOR_OPEN = 75.0                                # degrees about z: open into the hall


def build_garden(coll, M):
    g = S.Group("EpiGarden")
    o = C
    lawn = lambda x, y: 0.0 + 0.05 * noise.noise(Vector((x * 0.3, y * 0.3, 9.0)))  # noqa: E731
    K.sheet(g, o.x - 8.0, o.x + 20.0, o.y - 9.5, o.y + 6.6, lawn, M["grass"], res=0.7)
    K.sheet(g, o.x + HX0, o.x + 10.0, o.y + 6.6, o.y + HY0, lambda x, y: 0.05, M["paving"], res=2.0)
    g.box((o.x + 2.0, o.y + 6.6, 0.03), (16.0, 0.12, 0.08), M["coping"])
    for k in range(6):                                                                # stepping stones to the swing
        t = (k + 0.5) / 6
        x, y = o.x + 1.2 + (SWING.x - 1.2) * t, o.y + 6.4 + (SWING.y + 1.6 - 6.4) * t
        g.cyl((x, y, 0.02), 0.25, 0.04, M["paving"], n=12)
    # chalk on the patio: a sun, a hopscotch (Maya's)
    g.torus((o.x + 5.0, o.y + 8.2, 0.054), 0.35, 0.02, M["tape_w"], n1=20, n2=3)
    for k in range(10):
        a = k / 10 * math.tau
        g.box((o.x + 5.0 + math.cos(a) * 0.55, o.y + 8.2 + math.sin(a) * 0.55, 0.054), (0.2, 0.03, 0.004), M["tape_w"], rot=(0, 0, math.degrees(a)))
    for k in range(5):
        g.box((o.x - 1.5 + 0.0, o.y + 7.0 + k * 0.5, 0.054), (0.45, 0.45, 0.003), M["tape_w"])
        g.box((o.x - 1.5, o.y + 7.0 + k * 0.5, 0.0545), (0.41, 0.41, 0.003), M["paving"])
    # fences: close-boarded down the sides, a low picket across the end with the back gate hanging
    for x in (o.x - 8.0, o.x + 20.0):
        g.box((x, o.y - 1.5, 0.9), (0.03, 16.0, 1.8), M["fence"])
        for k in range(9):
            g.box((x, o.y - 9.5 + k * 2.0, 0.95), (0.1, 0.1, 1.9), M["fence"])
        for z in (0.3, 1.5):
            g.box((x + (0.04 if x > o.x else -0.04), o.y - 1.5, z), (0.04, 16.0, 0.08), M["fence"])
    for k in range(int(28 / 0.14)):
        x = o.x - 8.0 + k * 0.14
        if o.x + 3.6 < x < o.x + 4.8:
            continue
        g.box((x, o.y - 9.5, 0.5), (0.08, 0.02, 1.0), M["fence_x"])
        g.cyl((x, o.y - 9.5, 1.02), 0.04, 0.06, M["fence_x"], n=4, r2=0.0, rot=(0, 0, 45))
    for z in (0.3, 0.8):
        g.box((o.x + 6.0, o.y - 9.47, z), (28.0, 0.03, 0.07), M["fence_x"])
    g.box((o.x + 4.3, o.y - 10.0, 0.5), (1.1, 0.04, 0.9), M["fence_x"], rot=(0, 18, 38))    # the gate, off a hinge
    for k in range(6):
        g.box((o.x + 3.85 + k * 0.17, o.y - 10.1 + k * 0.1, 0.55 + k * 0.06), (0.06, 0.02, 0.95), M["fence_x"], rot=(0, 18, 38))
    # the flower beds along the sides: earth, shrubs, white roses
    for x in (o.x - 7.2, o.x + 19.2):
        g.box((x, o.y - 1.5, 0.02), (1.4, 14.0, 0.06), M["verge"])
        for k in range(10):
            y = o.y - 8.0 + k * 1.45
            K.ball(g, (x, y, 0.35), rnd.uniform(0.35, 0.55), M["shrub"], sub=2, squash=(1, 1.2, 0.8), jitter=0.35, seed=k + x)
            for j in range(4):
                K.ball(g, (x + rnd.uniform(-0.45, 0.45), y + rnd.uniform(-0.5, 0.5), rnd.uniform(0.45, 0.75)), 0.06, M["petal"], sub=1)
    # the swing: A-frames, the top bar along y (the seat swings along x), its hooks
    sx, sy, sz = o.x + SWING.x, o.y + SWING.y, SWING.z
    for yy in (sy - 1.15, sy + 1.15):
        for sg in (-1, 1):
            g.beam((sx + sg * 0.95, yy, 0.0), (sx, yy, sz + 0.06), 0.07, 0.07, M["steel_mid"])
        g.beam((sx - 0.5, yy, 1.0), (sx + 0.5, yy, 1.0), 0.04, 0.04, M["steel_mid"])
        for sg in (-1, 1):
            g.cyl((sx + sg * 0.95, yy, 0.05), 0.07, 0.1, M["concrete"], n=8)
    g.cyl((sx, sy, sz + 0.06), 0.045, 2.46, M["steel_mid"], rot=(90, 0, 0), n=12)
    for dy in (-0.22, 0.22):
        g.torus((sx, sy + dy, sz), 0.035, 0.01, M["steel_mid"], rot=(90, 0, 0), n1=10, n2=4)
    g.box((sx, sy, 0.01), (1.2, 0.8, 0.02), M["sand"])                                 # the worn patch under it
    # a slide, the sandpit and its toys, a ball, the rotary washing line, the bird bath, the table on the patio
    lx, ly = o.x + 13.0, o.y - 5.0
    for dy in (-0.25, 0.25):
        g.beam((lx, ly + dy, 0.0), (lx, ly + dy, 1.3), 0.04, 0.04, M["toy"])
    for k in range(4):
        g.box((lx, ly, 0.3 + k * 0.3), (0.04, 0.5, 0.04), M["toy"])
    g.box((lx + 0.15, ly, 1.3), (0.4, 0.55, 0.04), M["toy"])
    g.beam((lx + 0.3, ly, 1.28), (lx + 2.1, ly, 0.15), 0.45, 0.03, M["toy"])
    for dy in (-0.24, 0.24):
        g.beam((lx + 0.3, ly + dy, 1.36), (lx + 2.1, ly + dy, 0.23), 0.02, 0.08, M["toy"])
    px, py = o.x + 14.0, o.y + 2.0
    g.box((px, py, 0.1), (2.0, 1.6, 0.2), M["oak"])
    g.box((px, py, 0.16), (1.85, 1.45, 0.1), M["sand"])
    g.cyl((px - 0.4, py + 0.2, 0.3), 0.1, 0.18, M["toy"], n=10, r2=0.13)
    g.beam((px + 0.3, py - 0.2, 0.22), (px + 0.6, py - 0.4, 0.4), 0.04, 0.02, M["toy"])
    K.ball(g, (o.x + 11.0, o.y - 1.0, 0.11), 0.11, M["toy"], sub=2)
    wx, wy = o.x - 3.0, o.y - 1.0
    g.cyl((wx, wy, 1.0), 0.03, 2.0, M["steel_mid"], n=6)
    for k in range(4):
        a = k / 4 * math.tau + 0.3
        g.beam((wx, wy, 1.6), (wx + math.cos(a) * 1.4, wy + math.sin(a) * 1.4, 1.75), 0.02, 0.02, M["steel_mid"])
        for r in (0.6, 1.0, 1.35):
            b = a + math.pi / 4
            g.beam((wx + math.cos(a) * r, wy + math.sin(a) * r, 1.75), (wx + math.cos(a + math.pi / 2) * r, wy + math.sin(a + math.pi / 2) * r, 1.75), 0.006, 0.006, M["rope"])
    for k in range(3):                                                                 # a few things pegged out
        a = 0.3 + k * 0.9
        g.box((wx + math.cos(a) * 1.0, wy + math.sin(a) * 1.0, 1.45), (0.4, 0.02, 0.55), M["tent"], rot=(0, 0, math.degrees(a) + 45))
    bx, by = o.x + 5.5, o.y + 2.5
    g.cyl((bx, by, 0.4), 0.1, 0.8, M["coping"], n=10)
    g.cyl((bx, by, 0.85), 0.35, 0.1, M["coping"], n=16, r2=0.3)
    g.cyl((bx, by, 0.9), 0.28, 0.01, M["water"] if "water" in M else M["oil"], n=16)
    tx, ty = o.x - 2.6, o.y + 8.3
    g.cyl((tx, ty, 0.72), 0.5, 0.03, M["fence_x"], n=18)
    g.cyl((tx, ty, 0.36), 0.03, 0.72, M["steel_mid"], n=6)
    for dx in (-0.8, 0.8):
        g.box((tx + dx, ty, 0.42), (0.4, 0.4, 0.04), M["fence_x"])
        g.box((tx + dx * 1.2, ty, 0.68), (0.04, 0.4, 0.5), M["fence_x"])
    for (ppx, ppy, r) in ((o.x - 5.4, o.y + 9.4, 0.25), (o.x + 9.2, o.y + 9.3, 0.3), (o.x + 2.0, o.y + 9.6, 0.18)):
        g.cyl((ppx, ppy, r * 0.8), r, r * 1.6, M["concrete"], n=12, r2=r * 0.8)
        K.ball(g, (ppx, ppy, r * 1.9), r * 1.2, M["shrub"], sub=1, jitter=0.3, seed=ppx)
    # the shed at the end of the garden, the apple tree, the bins by the side passage
    shx, shy = o.x + 17.0, o.y - 6.6
    g.box((shx, shy, 1.0), (2.4, 1.8, 2.0), M["shed"])
    g.prism([(shx - 1.35, 2.0), (shx + 1.35, 2.0), (shx, 2.55)], shy - 1.0, shy + 1.0, M["felt"])
    g.box((shx - 0.4, shy - 0.91, 0.95), (0.8, 0.04, 1.8), M["shed"])
    g.box((shx + 0.65, shy - 0.91, 1.4), (0.6, 0.03, 0.5), M["win"])
    tx, ty = o.x - 5.0, o.y - 6.0
    g.beam((tx, ty, 0.0), (tx + 0.3, ty + 0.2, 1.8), 0.22, 0.22, M["bark"])
    for k in range(5):
        a = k / 5 * math.tau
        g.beam((tx + 0.3, ty + 0.2, 1.8), (tx + 0.3 + math.cos(a) * 1.4, ty + 0.2 + math.sin(a) * 1.4, 2.6 + rnd.uniform(0, 0.8)), 0.08, 0.08, M["bark"])
    for k in range(8):
        K.ball(g, (tx + rnd.uniform(-1.6, 1.8), ty + rnd.uniform(-1.4, 1.6), rnd.uniform(2.4, 3.6)), rnd.uniform(0.7, 1.1), M["leaf"], sub=2, jitter=0.35, seed=k)
    for k in range(2):
        g.box((o.x + 10.9, o.y + 11.0 + k * 0.8, 0.55), (0.6, 0.7, 1.1), M["rubber"])
    # the neighbours: the backs of the houses either side and across the gardens; their fences
    for (x0, x1, y0) in ((-24.0, -8.3, HY0), (20.3, 36.0, HY0)):
        wall(g, "x", o.y + y0 + 0.15, o.x + x0, o.x + x1, 0.0, 5.6, 0.3, [(o.x + x0 + 2.0, o.x + x0 + 3.4, 1.0, 2.2), (o.x + x0 + 6.0, o.x + x0 + 6.9, 0.15, 2.2),
                                                                          (o.x + x0 + 2.0, o.x + x0 + 3.4, 3.3, 4.5), (o.x + x0 + 9.0, o.x + x0 + 10.4, 3.3, 4.5)], M["brick_x"])
        for (a, b, z0, z1) in ((o.x + x0 + 2.0, o.x + x0 + 3.4, 1.0, 2.2), (o.x + x0 + 6.0, o.x + x0 + 6.9, 0.15, 2.2), (o.x + x0 + 2.0, o.x + x0 + 3.4, 3.3, 4.5), (o.x + x0 + 9.0, o.x + x0 + 10.4, 3.3, 4.5)):
            g.box(((a + b) / 2, o.y + y0 + 0.2, (z0 + z1) / 2), (b - a, 0.1, z1 - z0), M["win"] if z0 > 0.5 else M["door"])
        for sg in (-1, 1):
            g.box(((o.x * 2 + x0 + x1) / 2, o.y + 15.0 - sg * 2.6, 6.95), (x1 - x0, 5.9, 0.14), M["roof"], rot=(-sg * 27.5, 0, 0))
        g.box(((o.x * 2 + x0 + x1) / 2, o.y + 15.0, 2.8), (x1 - x0, 9.6, 5.6), M["brick_x"])
        K.sheet(g, o.x + x0, o.x + x1, o.y - 9.5, o.y + y0, lambda x, y: -0.02, M["grass_far"], res=4.0)
    for k in range(9):                                                                 # the houses across the back
        x = o.x - 30.0 + k * 8.0
        g.box((x, o.y - 30.0, 3.0), (7.4, 8.0, 6.0), M["brick_x"])
        g.box((x, o.y - 27.5, 7.0), (7.6, 4.6, 0.15), M["roof"], rot=(-38, 0, 0))
        g.box((x, o.y - 32.5, 7.0), (7.6, 4.6, 0.15), M["roof"], rot=(38, 0, 0))
        for j in range(3):
            g.box((x - 2.4 + j * 2.4, o.y - 25.98, 1.6 + (j % 2) * 2.4), (1.0, 0.05, 1.1), M["win"])
        g.box((x + 3.7, o.y - 22.0, 0.9), (0.04, 8.0, 1.8), M["fence"])
    K.sheet(g, o.x - 40, o.x + 60, o.y - 26.0, o.y - 9.5, lambda x, y: -0.03, M["grass_far"], res=5.0)
    for k in range(12):                                                                # trees between
        x, y = o.x - 34.0 + k * 7.5 + rnd.uniform(-2, 2), o.y - 18.0 + rnd.uniform(-4, 3)
        g.cyl((x, y, 1.6), 0.2, 3.2, M["bark"], n=8)
        for j in range(4):
            K.ball(g, (x + rnd.uniform(-1, 1), y + rnd.uniform(-1, 1), 3.5 + rnd.uniform(0, 2)), rnd.uniform(1.2, 2.0), M["leaf"], sub=1, jitter=0.3, seed=x + j)
    for k in range(8):                                                                 # the street's roofs behind the house
        x = o.x - 28.0 + k * 8.0
        g.box((x, o.y + 34.0, 3.5), (7.4, 8.0, 7.0), M["brick_x"])
        g.box((x, o.y + 32.0, 8.2), (7.6, 4.6, 0.15), M["roof"], rot=(-38, 0, 0))
    return g.finish(coll)


def build_swing(coll, M):
    """The swing seat on its two chains, its origin on the top bar: it swings about the bar (y)."""
    g = S.Group("SwingSeat")
    sx, sy, sz = C.x + SWING.x, C.y + SWING.y, SWING.z
    for dy in (-0.22, 0.22):
        z = sz - 0.04
        k = 0
        while z > SEAT_Z + 0.12:
            g.torus((sx, sy + dy, z), 0.03, 0.007, M["rope"], rot=(90, 0, 0) if k % 2 == 0 else (90, 0, 90), n1=8, n2=4)
            z -= 0.06
            k += 1
        g.beam((sx, sy + dy, SEAT_Z + 0.13), (sx, sy + dy * 0.95, SEAT_Z + 0.02), 0.012, 0.012, M["rope"])
    # the seat: a curved rubber strap
    for k in range(7):
        t = (k - 3) / 3
        g.box((sx, sy + t * 0.22, SEAT_Z - 0.02 * (1 - t * t) * 2), (0.17, 0.075, 0.018), M["rubber"])
    return K.finish_at(g, coll, (sx, sy, sz))


# ---------------------------------------------------------------- the skies

def build_sky(coll, M):
    g = S.Group("EpiSky")
    for (o, sun_az, sun_el) in ((A, -30.0, 9.0), (B, -20.0, 8.0), (C, -95.0, 12.0)):
        K.sky_band(g, (o.x, o.y), SKY_R, -40.0, 200.0, M["sky_a"], n=64)
        a, e = math.radians(sun_az), math.radians(sun_el)
        r = SKY_R - 4.0
        sp = Vector((o.x + math.cos(a) * math.cos(e) * r, o.y + math.sin(a) * math.cos(e) * r, math.sin(e) * r))
        n = (Vector((o.x, o.y, sp.z)) - sp).normalized()
        g.cyl(tuple(sp), 7.0, 0.5, M["sun"], rot=(90, 0, math.degrees(a) + 90), n=24)
        # far hills round the horizon, two layers of grey (the haze of a morning)
        for k in range(36):
            b = k / 36 * math.tau
            for (rr, mm, hh) in ((SKY_R - 20.0, M["far2"], 14.0), (SKY_R - 45.0, M["far"], 9.0)):
                cx, cy = o.x + math.cos(b) * rr, o.y + math.sin(b) * rr
                g.box((cx, cy, -8.0 + rnd.uniform(0.5, 1.0) * hh / 2), (rr * math.tau / 36 * 1.3, 8.0, rnd.uniform(0.6, 1.3) * hh), mm, rot=(0, 0, math.degrees(b) + 90))
    # from the cemetery: the town in the valley and Kessel Hill with the fortress on it, far off
    hx, hy = B.x - 150.0, B.y - 140.0
    for k in range(16):
        w = 90 - k * 5.0
        g.box((hx, hy, -30 + k * 4.0), (w, 30.0, 4.4), M["far"])
    for (tx, h) in ((-14, 14), (-6, 10), (4, 20), (14, 12)):
        g.box((hx + tx, hy, 34 + h / 2), (5.0, 8.0, h), M["far_dark"])
    g.box((hx, hy, 38), (34.0, 8.0, 8.0), M["far_dark"])
    for k in range(60):
        x, y = B.x - 120 + rnd.uniform(-60, 140), B.y - 110 + rnd.uniform(-40, 30)
        h = rnd.uniform(3, 12)
        g.box((x, y, -30 + h / 2), (rnd.uniform(3, 8), rnd.uniform(3, 8), h), M["far"])
    return g.finish(coll)


# ---------------------------------------------------------------- light

def lights():
    warm = (1.0, 0.955, 0.9)
    sun = K.add_light("SUN", "Sun", (0, 0, 50), 3.6, color=warm)
    sun.data.angle = math.radians(3.0)
    # the sun low in the -x/-y quarter, raking across everything
    sun.rotation_euler = (math.radians(78), 0, math.radians(-125))
    # the bright sky's fill, from the camera side and from above
    for o in (A, B, C):
        K.add_light("AREA", "SkyFill", (o.x, o.y - 40.0, 30.0), 2.2e4, size=80.0, rot=(math.radians(55), 0, 0), color=(0.96, 0.97, 1.0))
    # the hall: the morning through the back door, the kitchen's lamp, the fanlight
    K.aim(K.add_light("AREA", "DoorLight", (C.x + 1.0, C.y + HY0 - 2.5, 2.0), 220, size=1.2, color=warm), (C.x + 1.0, C.y + HY0 + 4.0, 0.6))
    K.add_light("AREA", "HallFill", (C.x + 1.0, C.y + HY0 + 4.5, CEIL - 0.05), 30, size=1.5, color=warm)
    K.add_light("POINT", "KitchenLamp", (C.x + 4.0, C.y + HY0 + 2.7, CEIL - 0.6), 70, size=0.3, color=warm)
    K.add_light("POINT", "HallLamp", (C.x + 1.0, C.y + CAB_Y, CEIL - 0.6), 45, size=0.3, color=warm)
    K.add_light("AREA", "Fanlight", (C.x + 0.9, C.y + HY1 - 0.5, 2.4), 25, size=0.8, rot=(math.radians(80), 0, 0))
    w = bpy.data.worlds.new("Dawn")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.43, 0.45, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


# ---------------------------------------------------------------- the plan for the game

def write_plan():
    def u3(p):
        return f"new Vector3({p[0]:.3f}f, {p[2]:.3f}f, {p[1]:.3f}f)"
    e = B + ELENA
    sx, sy = C.x + SWING.x, C.y + SWING.y
    lines = [
        "using UnityEngine;",
        "",
        "namespace ProjectInk",
        "{",
        "    /// <summary>The epilogue's three places (tools/blender/build_epilogue_set.py; written by its --write-plan),",
        "    /// laid out along x with wide gaps, each in its own sky: A the bottom of the cliff road (x 0), B the",
        "    /// cemetery (x 500), C the backyard of No. 14 and the hall inside (x 1000). World coordinates.</summary>",
        "    public static class EpiloguePlan",
        "    {",
        f"        public static readonly Vector3 RoadOrigin = {u3(A)}, CemeteryOrigin = {u3(B)}, HomeOrigin = {u3(C)};",
        "",
        "        // ---- A: the road. Harrow stands by his car; he and Maya face him from the +x side.",
        f"        public static readonly Vector3 RoadSpot = {u3(A + Vector((2.1, -0.45, 0)))};",
        f"        public static readonly Vector3 MayaRoadSpot = {u3(A + Vector((2.55, -0.9, 0)))};",
        f"        public static readonly Vector3 HarrowSpot = {u3(A + Vector((0.2, -0.6, 0)))};",
        f"        public static readonly Vector3 HarrowCar = {u3(A + Vector(DET_CAR))};   // its middle; faces +x",
        f"        public static readonly Vector3 ForensicTent = {u3(A + Vector((TENT[0], TENT[1], (TENT[1] - 6.4) ** 1.6 * 0.25)))};",
        f"        public static readonly Vector3 CliffTop = {u3(A + Vector((16.0, CLIFF_Y + 19.0, 60.0)))};",
        "        /// <summary>PoliceLight_0.._7: two lenses per car (0/1, 2/3, 4/5) and the van's (6/7).</summary>",
        "        public const int PoliceLightCount = 8;",
        "        public const float RoadX0 = -60f, RoadX1 = 60f;",
        "",
        "        // ---- B: the grave. Elena's stone faces -z (towards the path); he kneels before it.",
        f"        public static readonly Vector3 CemeteryGate = {u3(B + Vector((GATE_X, PATH_Y, 0)))};",
        f"        /// <summary>The stone's flat top, centre (the walkie-talkie stands here).</summary>",
        f"        public static readonly Vector3 ElenaStone = {u3(e + Vector((0, 0, STONE_H)))};",
        f"        public static readonly Vector3 ElenaStoneFace = {u3(e + Vector((0, -STONE_T / 2, STONE_H * 0.62)))};",
        f"        public static readonly Vector3 KneelSpot = {u3(e + Vector((0, -0.78, 0)))};",
        f"        public static readonly Vector3 MayaGraveSpot = {u3(e + Vector((0.62, -0.72, 0)))};",
        f"        public static readonly Vector3 GraveFlowers = {u3(e + Vector((-0.2, -0.26, 0.1)))};",
        f"        public static readonly Vector3 CemeteryPath = {u3(B + Vector((0, PATH_Y, 0)))};   // the gravel path runs along x here",
        "",
        "        // ---- C: home. SwingSeat's origin is on the top bar; it swings about SwingAxis (the seat moves along x).",
        f"        public static readonly Vector3 SwingPivot = {u3((sx, sy, SWING.z))};",
        f"        public static readonly Vector3 SwingSeat = {u3((sx, sy, SEAT_Z + 0.01))};   // the seat's top at rest",
        "        public static readonly Vector3 SwingAxis = Vector3.forward;",
        f"        public static readonly Vector3 PushSpot = {u3((sx - 0.95, sy, 0.0))};   // behind the seat, pushing towards +x",
        f"        public static readonly Vector3 HouseView = {u3((C.x + 1.0, C.y + HY0 - 0.6, 1.6))};   // wide from the house: at the back door",
        f"        public static readonly Vector3 BackDoor = {u3((C.x + (BACK_DOOR[0] + BACK_DOOR[1]) / 2, C.y + HY0 + 0.3, FLOOR))};",
        f"        /// <summary>BackDoor (a pivot on its hinge) is built standing open; closed: localRotation = Euler(0, BackDoorOpen, 0).</summary>",
        f"        public const float BackDoorOpen = {BACK_DOOR_OPEN:.0f}f;",
        f"        public const float HallFloorY = {FLOOR:.2f}f;",
        f"        /// <summary>HallDrawerFront's origin: its front face. It slides along +x (world) to open, HallDrawerTravel metres.</summary>",
        f"        public static readonly Vector3 HallDrawer = {u3((C.x + HALL_X0 + 0.36, C.y + CAB_Y, 0.68))};",
        "        public static readonly Vector3 HallDrawerSlide = Vector3.right;",
        "        public const float HallDrawerTravel = 0.28f;",
        f"        public static readonly Vector3 HallPhoto = {u3((C.x + HALL_X0 + 0.036, C.y + CAB_Y, 1.55))};",
        f"        public static readonly Vector3 HallSpot = {u3((C.x + HALL_X0 + 0.95, C.y + CAB_Y, FLOOR))};   // facing -x, at the drawer",
        f"        public static readonly Vector3 FrontDoor = {u3((C.x + (FRONT_DOOR[0] + FRONT_DOOR[1]) / 2, C.y + HY1 - 0.35, 1.1))};   // its inside face",
        f"        public static readonly Vector3 KitchenDoor = {u3((C.x + HALL_X1, C.y + HY0 + 0.3 + 0.95, FLOOR))};",
        f"        public const float HallX0 = {C.x + HALL_X0:.2f}f, HallX1 = {C.x + HALL_X1:.2f}f, HallZ0 = {C.y + HY0 + 0.3:.2f}f, HallZ1 = {C.y + HY1 - 0.3:.2f}f;",
        "    }",
        "}",
        "",
    ]
    with open(PLAN, "w") as f:
        f.write("\n".join(lines))
    print("wrote", PLAN)


# ---------------------------------------------------------------- main

def main():
    if "--write-plan" in sys.argv:
        write_plan()
        return
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    road = build_road(coll, M)
    police, plights = build_police(coll, M)
    cem, stones, elena = build_cemetery(coll, M), build_stones(coll, M), build_elena(coll, M)
    house, garden = build_house(coll, M), build_garden(coll, M)
    cab, drawer, photo = build_cabinet(coll, M)
    door, swing, sky = build_back_door(coll, M), build_swing(coll, M), build_sky(coll, M)
    for o in (elena, cab, drawer, door):
        D.bevel(o, 0.006)
    baked = [road, police, cem, stones, elena, house, garden, cab, drawer, photo, door, swing, sky]
    for o in baked + plights:
        print(f"  {o.name:16s} {K.faces(o):7d} faces")
    print("  total", sum(K.faces(o) for o in baked + plights))
    lights()
    if preview_only:
        cams = [
            ("7.1  the cliff road at dawn, the police lights", S.shot_camera("P1", (-14.0, 2.2, -11.0), (6.0, 2.4, 4.0), 46), None),
            ("7.2  Harrow, him and Maya (staging)", S.shot_camera("P2", (1.2, 1.5, -5.0), (1.2, 1.2, 0.0), 44), None),
            ("7.3  the cemetery, wide", S.shot_camera("P3", (B.x - 6.0, 3.0, -9.0), (B.x + 2.0, 1.6, 8.0), 50), None),
            ("7.3  Elena's stone, kneeling height (locked off)", S.shot_camera("P4", (B.x - 0.5, 0.75, -1.6), (B.x, 0.6, 1.6), 40), None),
            ("7.4  the swing in the morning", S.shot_camera("P5", (C.x + 3.0, 1.4, -8.5), (C.x + 7.5, 1.2, 1.0), 46), None),
            ("7.5  wide from the house", S.shot_camera("P6", (C.x + 1.0, 1.7, C.y + HY0 - 0.8), (C.x + 7.0, 0.9, -6.0), 54), None),
            ("7.5  the hall: the drawer under her photo", S.shot_camera("P7", (C.x + 1.75, 1.55, C.y + HY0 + 0.9), (C.x + 0.2, 1.1, C.y + CAB_Y), 52), None),
            ("7.5  the back of No. 14", S.shot_camera("P8", (C.x + 4.0, 2.0, -10.0), (C.x + 2.0, 3.0, 10.0), 52), None),
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "epilogue_set.png")
        return
    for o in baked:
        S.unwrap(o)
    S.bake(road, 4096, 16)
    S.bake(police, 2048, 16)
    S.bake(cem, 4096, 16)
    S.bake(stones, 4096, 16)
    S.bake(elena, 1024, 32)
    S.bake(house, 4096, 24)
    S.bake(garden, 4096, 16)
    S.bake(cab, 1024, 32)
    S.bake(drawer, 512, 32)
    S.bake(photo, 256, 16)
    S.bake(door, 512, 24)
    S.bake(swing, 512, 16)
    S.bake(sky, 2048, 6)
    K.export(baked + plights + K.marks(coll), os.path.join(OUT, "EpilogueSet.fbx"))


if __name__ == "__main__":
    main()
