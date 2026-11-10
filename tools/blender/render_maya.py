"""Chapter 3's stills (docs/story/chapter3_home.md).

H6, the family photo gives him back his daughter: six soft memories of Maya, in the same figure
language as Elena (render_memories.py): a little white figure with a big round head, big dark
eyes with a sparkle, a blush, a wide smile, a short bob with two pigtails and a ribbon tied in a
bow on top (the ribbon is her motif from here on).

H10, how Elena died: three broken stills, harsher (cold, crushed blacks, grain): the men in the
hall, Elena pushing Maya out of the back door, a hand on her shoulder and her face set.

    /home/user/.venv-blender/bin/python tools/blender/render_maya.py [1 2 ...]

Writes Assets/Resources/Story/Memories/maya_<n>.png and Assets/Resources/Story/Flash/elena_<n>.png
(960x540). StoryHome.MayaDur / the music box follow MAYA_SECONDS."""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_memories as RM  # noqa: E402
import render_flashbacks as F  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

MEM = os.path.join(F.ROOT, "Assets", "Resources", "Story", "Memories")
FLASH = os.path.join(F.ROOT, "Assets", "Resources", "Story", "Flash")
rnd = random.Random(41)

MAYA_SECONDS = [2.8, 2.6, 2.6, 2.6, 2.6, 3.4]
sph, rod, to_bone, rest = RM.sph, RM.rod, RM.to_bone, RM.rest


def satin(pts, nrms, width, thick, mat, name="Ribbon"):
    """A ribbon strip as a real mesh: through points, its face along the normals, a width and a thickness,
    the edges turned (a slightly thinner rim) and a stitch line along each side."""
    verts, faces = [], []
    n = len(pts)
    for i in range(n):
        p = Vector(pts[i])
        d = (Vector(pts[min(i + 1, n - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
        nr = Vector(nrms[i]).normalized()
        side = d.cross(nr).normalized()
        for sx, sz in ((-0.5, -0.35), (-0.42, 0.5), (0.42, 0.5), (0.5, -0.35), (0.42, -0.5), (-0.42, -0.5)):
            verts.append(p + side * width * sx + nr * thick * sz)
    k = 6
    for i in range(n - 1):
        for j in range(k):
            a, b = i * k + j, i * k + (j + 1) % k
            faces.append((a, b, b + k, a + k))
    faces.append(tuple(range(k))[::-1])
    faces.append(tuple(range((n - 1) * k, n * k)))
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    for poly in me.polygons:
        poly.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def ribbon_bow(c, size, face, up, mat, tails=True):
    """A bow: two teardrop loops that twist as they turn, a gathered knot with wrinkles, two tails with
    V-cut ends. c: the knot; face: where the bow looks; up: its up."""
    c, face, up = Vector(c), Vector(face).normalized(), Vector(up).normalized()
    side = up.cross(face).normalized()
    w, t = size * 0.34, size * 0.05
    obs = []
    for s in (1, -1):
        pts, nrs = [], []
        for i in range(25):
            th = i / 24 * 2 * math.pi
            out = size * (0.5 - 0.5 * math.cos(th))
            hgt = size * 0.38 * math.sin(th)
            pts.append(c + side * s * out + up * hgt + face * 0.15 * size * math.sin(th / 2))
            ang = math.sin(th) * 0.4 * s
            nrs.append(face * math.cos(ang) + up * math.sin(ang))
        obs.append(satin(pts, nrs, w, t, mat))
    knot = sph(c + face * w * 0.2, w * 0.55, 0.97, scale=(1.0, 0.8, 1.15), mat=mat)
    obs.append(knot)
    if tails:
        for s, ang in ((1, -0.35), (-1, 0.42)):
            d = (-up * math.cos(ang) + side * math.sin(ang)).normalized()
            pts, nrs = [], []
            for i in range(11):
                u = i / 10
                pts.append(c + d * size * 0.95 * u + side * s * math.sin(u * 4.0) * size * 0.06 + face * 0.02 * size * u)
                nrs.append(face)
            obs.append(satin(pts, nrs, w * 0.9, t, mat))
            end = pts[-1]
            for v in (1, -1):                                               # the V-cut
                tip = end + d * w * 0.45 + side * v * w * 0.25
                obs.append(satin([end + side * v * w * 0.05, tip], [face, face], w * 0.42, t, mat))
    return obs


def maya(pose, at, facing=1, size=0.6, smile="smile", eyes="open", hair="pigtails", name="Maya"):
    """Maya: the rig at a child's size, her head a size up, a short dress, the ribbon."""
    arm = B.build_armature()
    arm.name = name
    skin = F.flat(RM.SKIN)
    for side in ("L", "R"):
        parts = [rod(rest(f"upperarm.{side}"), rest(f"upperarm.{side}", "tail"), 0.042, RM.SKIN, mat=skin),
                 sph(rest(f"upperarm.{side}"), 0.044, RM.SKIN, mat=skin)]
        to_bone(parts, arm, f"upperarm.{side}")
        fore = rest(f"forearm.{side}", "tail")
        parts = [rod(rest(f"forearm.{side}"), fore, 0.038, RM.SKIN, mat=skin), sph(rest(f"forearm.{side}"), 0.04, RM.SKIN, mat=skin),
                 sph(fore + Vector((0, 0, -0.02)), 0.05, RM.SKIN, mat=skin)]
        to_bone(parts, arm, f"forearm.{side}")
        parts = [rod(rest(f"thigh.{side}"), rest(f"thigh.{side}", "tail"), 0.05, RM.SKIN, mat=skin),
                 sph(rest(f"thigh.{side}"), 0.052, RM.SKIN, mat=skin)]
        to_bone(parts, arm, f"thigh.{side}")
        foot = rest(f"shin.{side}", "tail")
        parts = [rod(rest(f"shin.{side}"), foot, 0.045, RM.SKIN, mat=skin), sph(rest(f"shin.{side}"), 0.048, RM.SKIN, mat=skin),
                 sph(foot + Vector((0.04, 0, -0.005)), 0.06, 0.25, scale=(1.4, 0.85, 0.75))]
        to_bone(parts, arm, f"shin.{side}")
    to_bone([rod((0, 0, 0.66), (0, 0, 0.84), 0.12, RM.SKIN, r2=0.11, mat=skin)], arm, "hips")
    to_bone([rod((0, 0, 0.84), (0, 0, 1.02), 0.11, RM.SKIN, r2=0.12, mat=skin)], arm, "chest")
    # a little dress
    to_bone([rod((0, 0, 0.86), (0, 0, 1.0), 0.115, 0.7, r2=0.125)], arm, "chest")
    to_bone([rod((0, 0, 0.86), (0, 0, 0.5), 0.115, 0.7, r2=0.27, verts=36)], arm, "hips")
    # the head: bigger for her size, the face looking along +x
    hc = Vector((0, 0, B.HEAD_CENTER + 0.02))
    face = [sph(hc, 0.25, RM.SKIN, mat=skin)]
    for s in (1, -1):
        eye = hc + Vector((0.2, s * 0.085, 0.0))
        if eyes == "closed":
            face.append(rod(eye + Vector((0.01, -s * 0.025, 0)), eye + Vector((0.01, s * 0.025, -0.008)), 0.006, RM.EYES, verts=6))
        else:
            face.append(sph(eye, 1.0, RM.EYES, scale=(0.026, 0.042, 0.06)))
            face.append(sph(eye + Vector((0.02, s * 0.008, 0.022)), 0.014, 1.0))
            face.append(sph(eye + Vector((0.021, -s * 0.01, -0.02)), 0.007, 1.0))
        face.append(sph(hc + Vector((0.2, s * 0.14, -0.07)), 1.0, RM.BLUSH, scale=(0.014, 0.04, 0.024)))
    if smile == "laugh":
        face.append(sph(hc + Vector((0.238, 0, -0.085)), 1.0, RM.LIPS, scale=(0.014, 0.04, 0.03)))
    elif smile == "blow":
        face.append(sph(hc + Vector((0.242, 0, -0.08)), 1.0, RM.LIPS, scale=(0.012, 0.016, 0.016)))
    else:
        for k in range(7):
            u = (k - 3) / 3
            face.append(sph(hc + Vector((0.24 - 0.004 * u * u, u * 0.04, -0.09 + 0.016 * u * u)), 0.0065, RM.LIPS))
    hm = F.flat(RM.HAIR)
    face.append(sph(hc + Vector((-0.06, 0, 0.04)), 0.255, RM.HAIR, scale=(0.92, 1.05, 1.02), mat=hm))         # the bob
    face.append(sph(hc + Vector((0.13, 0, 0.16)), 1.0, RM.HAIR, scale=(0.12, 0.2, 0.06), mat=hm))             # her fringe
    for s in (1, -1):
        face.append(sph(hc + Vector((-0.02, s * 0.21, -0.12)), 0.07, RM.HAIR, mat=hm))                         # the bob's ends
    if hair == "pigtails":
        for s in (1, -1):
            for k in range(4):
                t = k / 3
                face.append(sph(hc + Vector((-0.08, s * (0.27 + 0.04 * t), 0.06 - 0.18 * t)), 0.06 - 0.012 * t, RM.HAIR, mat=hm))
            face.append(sph(hc + Vector((-0.08, s * 0.25, 0.08)), 0.025, 0.95))                                 # bobbles
    # the ribbon, a satin bow on top (the loops twist, the knot gathers, the tails end in a V)
    face.extend(ribbon_bow(hc + Vector((-0.06, 0, 0.255)), 0.26, (0, -1, 0.25), (0, 0, 1), F.flat(0.97)))
    to_bone(face, arm, "head")
    B.apply_pose(arm, pose)
    arm.location = at
    arm.scale = (size, size, size)
    arm.rotation_euler = (0, 0, 0 if facing > 0 else math.pi)
    bpy.context.view_layer.update()
    return arm


def bundle(p, s=1.0):
    """A newborn swaddled: a soft white parcel and a tiny face."""
    RM.sph(p, 0.14 * s, 0.94, scale=(1.5, 1, 0.9))
    RM.sph(p + Vector((0.16 * s, 0, 0.04 * s)), 0.07 * s, RM.SKIN)
    RM.sph(p + Vector((0.2 * s, 0.02 * s, 0.05 * s)), 0.008 * s, RM.EYES)


# ------------------------------------------------------------------ the memories (soft)

def k1_born():
    """1. The hospital: Elena in bed, the newborn in her arms; he leans in. "She has your frown." """
    F.floor(0.42, -3, 3, -5, 5, trim=False)
    F.wall(1.8, -5, 5, 4, g=0.66)
    F.box((1.8, 1.75, 1.6), (1.2, 0.04, 1.0), F.halo(1.0, 0.9, "Window"))
    F.box((0.2, 0.5, 0.45), (2.2, 1.0, 0.25), F.flat(0.9))                                          # the bed
    F.box((-0.85, 0.5, 0.75), (0.12, 1.0, 0.6), F.flat(0.3))
    sit = B.pose(B.STRAIGHT, spine=200, chest=196, head=188, upperarm_L=70, forearm_L=100, upperarm_R=60, forearm_R=110,
                 thigh_L=90, shin_L=90, thigh_R=90, shin_R=90, dz=0.0)
    RM.elena(sit, (-0.35, 0.5, 0.35), facing=1, outfit="dress", hair="down", smile="smile")
    bundle(Vector((-0.12, 0.42, 1.12)))
    lean = B.pose(B.STRAIGHT, spine=160, chest=150, head=140, upperarm_L=60, forearm_L=90)
    RM.him(lean, (0.85, 0.2, 0), facing=-1)
    RM.bokeh(14, -2, 2, 1.4, 2.4, 1.6, 0.1)
    F.camera((0.2, -2.8, 1.3), (0.1, 0.4, 1.0), 36)


def k2_steps():
    """2. Her first steps: across the kitchen floor towards him, his arms open."""
    F.floor(0.4, -3, 3, -5, 5, trim=False)
    F.wall(1.6, -6, 6, 4, g=0.6)
    F.box((0.0, 1.2, 0.45), (4.0, 0.6, 0.9), F.flat(0.26))
    F.box((1.6, 1.55, 1.55), (1.3, 0.05, 1.2), F.halo(1.0, 0.95, "Window"))
    step = B.pose(B.STRAIGHT, upperarm_L=100, forearm_L=110, upperarm_R=95, forearm_R=105, thigh_L=22, shin_L=-4, thigh_R=-12, shin_R=-14, head=186)
    maya(step, (0.35, 0, 0), facing=-1, size=0.42, smile="laugh", hair="tuft")
    kneel = B.pose(B.STRAIGHT, thigh_L=90, shin_L=0, thigh_R=-10, shin_R=-95, upperarm_L=110, forearm_L=120, upperarm_R=105, forearm_R=115,
                   chest=170, head=176, dz=-0.3)
    RM.him(kneel, (-0.5, 0, 0), facing=1)
    F.camera((0.0, -2.6, 0.7), (-0.05, 0.3, 0.55), 40)


def k3_shoulders():
    """3. On his shoulders at the sea, her arms up; Elena beside them, laughing."""
    F.plane((0, 40, 10), (200, 30, 1), F.flat(0.66))
    F.disc((-2.5, 39, 2.6), 1.2, F.flat(1.0), rot=(math.pi / 2, 0, 0))
    F.disc((-2.5, 38.9, 2.6), 5.0, F.halo(1.0, 0.35), rot=(math.pi / 2, 0, 0))
    F.box((0, 20, -0.02), (200, 40, 0.04), F.flat(0.34))
    F.box((0, -2, -0.01), (40, 8, 0.02), F.flat(0.55))
    stand = B.pose(B.STRAIGHT, upperarm_L=150, forearm_L=200, upperarm_R=150, forearm_R=200, head=176)
    RM.him(stand, (-0.25, 0.5, 0), facing=1)
    ride = B.pose(B.STRAIGHT, thigh_L=80, shin_L=10, thigh_R=80, shin_R=10, upperarm_L=170, forearm_L=180, upperarm_R=165, forearm_R=175, head=188)
    maya(ride, (-0.3, 0.62, 0.98), facing=1, size=0.62, smile="laugh")
    walk = B.pose(B.STRAIGHT, head=190, upperarm_L=30, forearm_L=60)
    RM.elena(walk, (0.55, 0.55, 0), facing=-1, outfit="sundress", hair="down", hat=True, smile="laugh")
    for k in range(4):
        x, z = rnd.uniform(-2, 2), rnd.uniform(2.4, 3.2)
        for s in (1, -1):
            rod((x, 12, z), (x + s * 0.18, 12, z + 0.06), 0.012, 0.15, verts=6)
    F.camera((0.15, -3.2, 1.35), (0.1, 0.6, 1.45), 38)


def k4_story():
    """4. A bedtime story: she is asleep, the book open on his knee, a night light."""
    F.floor(0.3, -3, 3, -5, 5, trim=False)
    F.wall(1.5, -5, 5, 4, g=0.4)
    for _ in range(18):
        F.disc((rnd.uniform(-2, 2), 1.48, rnd.uniform(1.2, 2.6)), 0.03, F.flat(0.95))                 # stars on the wall
    F.box((0.6, 0.7, 0.3), (1.5, 0.8, 0.3), F.flat(0.86))                                           # her bed
    F.ball((1.6, 0.9, 0.8), 0.12, F.halo(1.0, 1.0, "NightLight"))
    F.disc((1.6, 0.85, 0.8), 0.7, F.halo(1.0, 0.3))
    lie = B.rotated(B.pose(B.STRAIGHT, head=186, upperarm_L=20, forearm_L=60), 90, dz=-0.85)
    maya(lie, (0.45, 0.7, 0.46), facing=1, size=0.55, eyes="closed")
    F.box((0.95, 0.68, 0.53), (0.95, 0.82, 0.08), F.flat(0.75))                                     # the blanket over her legs
    sit = B.pose(B.STRAIGHT, thigh_L=90, shin_L=0, thigh_R=90, shin_R=0, upperarm_L=40, forearm_L=90, upperarm_R=40, forearm_R=90,
                 chest=170, head=150, dz=-0.35)
    RM.him(sit, (-0.55, 0.5, 0), facing=1)
    F.box((-0.28, 0.45, 0.62), (0.3, 0.22, 0.02), F.flat(0.95), rot=(0, 0.3, 0))                   # the open book
    F.camera((0.2, -2.6, 1.0), (0.2, 0.6, 0.65), 40)


def k5_cake():
    """5. Her sixth birthday: six candles, she leans in to blow; Elena behind her, the paper hats."""
    F.floor(0.4, -3, 3, -5, 5, trim=False)
    F.wall(1.8, -6, 6, 4, g=0.5)
    for k in range(5):
        RM.string_lights(-3, 3, 2.4 + k * 0.1, 1.7, 0.3, n=20)
    F.box((0.1, 0.4, 0.4), (1.4, 0.8, 0.8), F.flat(0.3))                                            # the table
    F.box((0.1, 0.25, 0.9), (0.5, 0.5, 0.2), F.flat(0.94))                                          # the cake
    for k in range(6):
        a = k / 6 * math.tau
        x, y = 0.1 + math.cos(a) * 0.15, 0.25 + math.sin(a) * 0.15
        F.box((x, y, 1.06), (0.02, 0.02, 0.12), F.flat(0.9))
        F.ball((x, y, 1.15), 0.016, F.flat(1.0))
        F.disc((x, y - 0.02, 1.15), 0.09, F.halo(1.0, 0.8))
    lean = B.pose(B.STRAIGHT, spine=165, chest=150, head=140, upperarm_L=40, forearm_L=60, upperarm_R=40, forearm_R=60)
    maya(lean, (-0.5, 0.25, 0), facing=1, size=0.6, smile="blow")
    clap = B.pose(B.STRAIGHT, upperarm_L=80, forearm_L=150, upperarm_R=80, forearm_R=150, head=184)
    RM.elena(clap, (-0.7, 0.9, 0), facing=1, outfit="dress", hair="bun", smile="laugh")
    RM.bokeh(20, -2.5, 2.5, 1.4, 2.6, 1.6, 0.1)
    F.camera((0.9, -2.4, 1.05), (-0.1, 0.4, 0.95), 38)


def k6_ribbon():
    """6. Elena tying the ribbon in her hair; Maya beaming at the camera (at him): "Papa! Look!" """
    F.wall(1.4, -4, 4, 4, g=0.42)
    RM.bokeh(30, -1.8, 1.8, 0.6, 2.2, 1.3, 0.12)
    beam = B.pose(B.STRAIGHT, upperarm_L=20, forearm_L=40, upperarm_R=110, forearm_R=160, head=186)
    maya(beam, (0.0, 0, 0), facing=-1, size=0.62, smile="laugh")
    tie = B.pose(B.STRAIGHT, spine=168, chest=160, head=150, upperarm_L=120, forearm_L=110, upperarm_R=115, forearm_R=105)
    RM.elena(tie, (0.42, 0.25, 0), facing=-1, outfit="dress", hair="down", smile="smile")
    F.camera((-0.9, -2.7, 0.95), (0.15, 0.1, 0.9), 40, roll=-3)


MEMORIES = [k1_born, k2_steps, k3_shoulders, k4_story, k5_cake, k6_ribbon]


# ------------------------------------------------------------------ how she died (broken)

def thug(pose, at, facing=1):
    return F.figure(pose, at=at, facing=facing, ink=0.2)


def f1_hall():
    """1. The doorbell: the front door open, three men in the hall, against the light."""
    F.floor(0.2, -3, 3, -5, 5, trim=False)
    F.wall(1.6, -5, 5, 4, g=0.22)
    F.box((0.0, 1.55, 1.1), (1.0, 0.05, 2.2), F.halo(1.0, 1.0, "Door"))
    stand = B.pose(B.STRAIGHT, upperarm_L=10, forearm_L=20, upperarm_R=-10, forearm_R=10)
    for x in (-0.5, 0.05, 0.6):
        thug(stand, (x, 0.9 - abs(x) * 0.3, 0), facing=-1)
    F.camera((0.0, -2.2, 0.8), (0.0, 1.0, 1.1), 44, roll=5)


def f2_run():
    """2. Elena pushing Maya out of the back door: "Run, baby." """
    F.floor(0.24, -3, 3, -5, 5, trim=False)
    F.wall(1.6, -5, 5, 4, g=0.26)
    F.box((0.9, 1.55, 1.1), (1.0, 0.05, 2.2), F.halo(1.0, 0.9, "BackDoor"))
    push = B.pose(B.STRAIGHT, spine=165, chest=155, head=160, upperarm_L=95, forearm_L=95, upperarm_R=90, forearm_R=95, thigh_L=30, thigh_R=-20)
    RM.elena(push, (0.1, 0.6, 0), facing=1, outfit="dress", hair="down", smile="oh")
    run = B.pose(B.STRAIGHT, thigh_L=50, shin_L=-10, thigh_R=-30, shin_R=-70, upperarm_L=-40, upperarm_R=60, head=200)
    maya(run, (0.75, 0.9, 0.02), facing=1, size=0.6, smile="blow")
    F.camera((-0.6, -2.0, 0.9), (0.5, 0.8, 0.8), 42, roll=-7)


def f3_hand():
    """3. A man's hand on her shoulder; her face set: "He'll come for you." """
    F.wall(1.4, -4, 4, 4, g=0.18)
    stand = B.pose(B.STRAIGHT, head=176, chest=178)
    RM.elena(stand, (0.15, 0, 0), facing=-1, outfit="dress", hair="down", smile="smile")
    grab = B.pose(B.STRAIGHT, upperarm_R=100, forearm_R=90)
    thug(grab, (-0.55, 0.3, 0), facing=1)
    F.camera((-0.4, -1.5, 1.2), (0.1, 0.0, 1.15), 48, roll=8)


FLASHES = [f1_hall, f2_run, f3_hand]


def harsh(path):
    """The broken memory: cold, crushed blacks, hard grain, a torn line or two."""
    from PIL import Image, ImageChops, ImageDraw
    im = Image.open(path).convert("L")
    im = im.point(lambda v: max(0, min(255, int((v - 40) * 1.5))))
    w, h = im.size
    noise = Image.effect_noise((w, h), 40)
    im = Image.blend(im, noise, 0.12)
    d = ImageDraw.Draw(im)
    for _ in range(3):
        y = rnd.randint(0, h - 1)
        band = im.crop((0, y, w, min(h, y + rnd.randint(4, 14))))
        im.paste(band, (rnd.randint(-40, 40), y))
    vig = Image.radial_gradient("L").resize((w, h)).point(lambda v: int(255 - v * 0.7))
    im = ImageChops.multiply(im, vig)
    im.convert("RGB").save(path)


if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    os.makedirs(MEM, exist_ok=True)
    os.makedirs(FLASH, exist_ok=True)
    jobs = [(i, s, os.path.join(MEM, f"maya_{i}.png"), RM.soften) for i, s in enumerate(MEMORIES, 1)]
    jobs += [(10 + i, s, os.path.join(FLASH, f"elena_{i}.png"), harsh) for i, s in enumerate(FLASHES, 1)]
    for n, shot, path, post in jobs:
        if only and n not in only:
            continue
        F.reset()
        shot()
        sc = bpy.context.scene
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True)
        post(path)
        print("rendered", path)
