"""Renders chapter 2's memory stills (docs/story/chapter2_bank.md, shot B16): the wedding ring
in box 417 gives him back his wife. Five soft stills, in the game's black and white, made from
the game's own figures (the rig in hand-set poses): him ink-black, Elena white in her dress.
Unlike the fight flashbacks (hard, grainy, harsh) these are memories he wants: high-key, a
glow round the lights, lifted blacks, a soft vignette.

    /home/user/.venv-blender/bin/python tools/blender/render_memories.py [--quick] [1 2 ...]

Writes Assets/Resources/Story/Memories/wed_<n>.png (960x540)."""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_flashbacks as F  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402

F.OUT = os.path.join(F.ROOT, "Assets", "Resources", "Story", "Memories")
rnd = random.Random(21)
HER = 0.76                         # Elena: a white figure (her dress a touch brighter)
HIM = F.HERO


def her(pose, at, facing=1, tilt=0.0, veil=False, dress_flare=0.0, name="Her"):
    """Elena: a white figure with dark hair to her shoulders, in a long white dress (and a veil)."""
    arm = F.figure(pose, at=at, facing=facing, tilt=tilt, ink=HER, name=name)
    bpy.context.view_layer.update()
    # on a white face the eyes are dark
    for o in bpy.data.objects:
        if o.parent == arm and o.type == "MESH" and o.parent_type == "BONE":
            o.data.materials.clear()
            o.data.materials.append(F.flat(0.08))
    head = arm.matrix_world @ arm.pose.bones["head"].head
    hips = arm.matrix_world @ arm.pose.bones["hips"].head
    ax, ay, az = at
    # hair: dark, behind the head and down to the shoulders
    F.ball((head.x - 0.09 * facing, head.y + 0.02, head.z + 0.17), 0.15, F.flat(0.16))
    F.box((head.x - 0.12 * facing, head.y + 0.03, head.z + 0.02), (0.12, 0.18, 0.28), F.flat(0.16), rot=(0, math.radians(-8 * facing), 0))
    # the dress: a long cone from the waist
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.34 + dress_flare, radius2=0.11, depth=0.66, location=(hips.x, hips.y, hips.z - 0.3))
    d = bpy.context.object
    d.data.materials.append(F.flat(0.84))
    d.rotation_euler = (0, math.radians(-tilt * 0.6), 0)
    if veil:
        F.box((head.x - 0.18 * facing, head.y + 0.05, head.z - 0.25), (0.05, 0.42, 0.85), F.halo(0.95, 0.5, "Veil"))
    return arm


def petals(n, x0, x1, z0, z1, y=0.4, g=0.95):
    for _ in range(n):
        F.disc((rnd.uniform(x0, x1), y + rnd.uniform(-0.4, 0.4), rnd.uniform(z0, z1)), rnd.uniform(0.012, 0.025), F.flat(g),
               rot=(rnd.uniform(0, 3), rnd.uniform(0, 3), rnd.uniform(0, 3)))


def bokeh(n, x0, x1, z0, z1, y, r=0.06):
    for _ in range(n):
        F.disc((rnd.uniform(x0, x1), y, rnd.uniform(z0, z1)), rnd.uniform(r * 0.6, r * 1.4), F.halo(1.0, 0.7, "Bokeh"))


# ------------------------------------------------------------------ the stills

def wed_altar():
    """1. The altar: under an arch of flowers they face each other, her hands in his; a veil; petals."""
    F.floor(0.42, -3, 4, -6, 6, trim=False)
    F.wall(2.6, -8, 8, 6, g=0.5)
    for x in (-2.6, 2.6):                                                  # tall bright windows
        F.box((x, 2.55, 2.4), (1.1, 0.05, 2.8), F.halo(1.0, 0.9, "Window"))
    for i in range(26):                                                    # the arch
        a = math.pi * i / 25
        x, z = math.cos(a) * 1.05, 1.0 + math.sin(a) * 1.35
        F.ball((x, 0.55, z), 0.1 + 0.03 * math.sin(i), F.flat(0.82 if i % 2 else 0.68))
    for x in (-1.05, 1.05):
        F.box((x, 0.55, 0.5), (0.06, 0.06, 1.0), F.flat(0.3))
    pose_him = B.pose(B.STRAIGHT, upperarm_L=60, forearm_L=88, upperarm_R=55, forearm_R=85, head=172)
    pose_her = B.pose(B.STRAIGHT, upperarm_L=58, forearm_L=86, upperarm_R=52, forearm_R=84, head=176)
    F.figure(pose_him, at=(-0.36, 0, 0), hero=False, name="Him")
    her(pose_her, (0.36, 0, 0), facing=-1, veil=True)
    petals(60, -1.4, 1.4, 0.4, 2.6)
    F.camera((0.0, -3.8, 1.0), (0.0, 0.3, 1.05), 40)


def wed_dance():
    """2. The first dance: he dips her, her dress flaring, string lights blurred behind."""
    F.floor(0.3, -3, 4, -6, 6, trim=False)
    F.wall(3.5, -8, 8, 6, g=0.18)
    for k in range(5):                                                     # strings of lights, sagging
        for i in range(30):
            t = i / 29
            x = -3.2 + t * 6.4
            z = 2.7 + k * 0.25 - math.sin(t * math.pi) * 0.45
            F.ball((x, 3.2, z), 0.035, F.flat(1.0))
    bokeh(40, -3, 3, 1.4, 3.2, 2.6, 0.09)
    pose_him = B.pose(B.STRAIGHT, spine=168, chest=160, head=150, upperarm_L=80, forearm_L=100, upperarm_R=95, forearm_R=95,
                      thigh_L=20, shin_L=-5, thigh_R=-14, shin_R=-10)
    pose_her = B.pose(B.STRAIGHT, spine=210, chest=225, head=235, upperarm_L=200, forearm_L=210, upperarm_R=110, forearm_R=120,
                      thigh_L=40, shin_L=20, thigh_R=-10, shin_R=-6)
    F.figure(pose_him, at=(-0.25, 0, 0), name="Him")
    her(pose_her, (0.28, 0, 0.02), facing=-1, tilt=-18, dress_flare=0.18)
    F.camera((0.4, -3.3, 0.8), (0.05, 0.3, 0.95), 38, roll=-6)


def wed_kitchen():
    """3. A kitchen on a Sunday: she laughs, flour on her cheek; his arms round her from behind."""
    F.floor(0.36, -3, 3, -5, 5, trim=False)
    F.wall(1.6, -6, 6, 4, g=0.6)
    F.box((1.6, 1.55, 1.55), (1.3, 0.05, 1.2), F.halo(1.0, 0.95, "Window"))   # the window, blown white
    F.box((0.0, 1.1, 0.45), (4.0, 0.7, 0.9), F.flat(0.24))                   # the counter
    F.box((0.0, 0.76, 0.9), (4.0, 0.04, 0.04), F.flat(F.TRIM))
    for x in (-1.4, -1.0, -0.6):                                             # pans on the wall
        F.disc((x, 1.53, 1.75), 0.14, F.flat(0.14), rot=(math.pi / 2, 0, 0))
    F.box((-0.2, 1.0, 0.95), (0.35, 0.3, 0.08), F.flat(0.92))                # a bowl, flour
    pose_her = B.pose(B.STRAIGHT, head=200, chest=186, upperarm_L=40, forearm_L=150, upperarm_R=20, forearm_R=60)
    pose_him = B.pose(B.STRAIGHT, head=165, chest=172, upperarm_L=75, forearm_L=95, upperarm_R=70, forearm_R=92)
    her(pose_her, (0.15, 0, 0), facing=1)
    F.figure(pose_him, at=(-0.34, 0.12, 0), facing=1, name="Him")
    F.ball((0.28, -0.12, 1.16), 0.035, F.flat(1.0))                         # flour on her cheek
    petals(40, -0.3, 0.6, 0.8, 1.5, y=0.6, g=0.98)                           # flour in the air
    F.camera((0.6, -2.7, 1.2), (0.1, 0.3, 1.0), 36, roll=3)


def wed_sea():
    """4. By the sea at sundown: they walk away hand in hand, footprints behind them."""
    F.plane((0, 40, 10), (200, 30, 1), F.flat(0.62))                         # the sky, pale
    F.disc((2.5, 39, 2.0), 1.4, F.flat(1.0), rot=(math.pi / 2, 0, 0))         # the sun on the horizon
    F.disc((2.5, 38.9, 2.0), 5.0, F.halo(1.0, 0.35), rot=(math.pi / 2, 0, 0))
    F.box((0, 20, -0.02), (200, 40, 0.04), F.flat(0.3))                       # the sea
    for i in range(14):                                                      # light on the water
        F.box((2.5 + rnd.uniform(-1.4, 1.4), 18 - i * 1.2, 0.01), (rnd.uniform(0.3, 1.2), 0.08, 0.01), F.flat(0.95))
    F.box((0, -2, -0.01), (40, 8, 0.02), F.flat(0.52))                       # wet sand
    walk_him = B.pose(B.STRAIGHT, thigh_L=25, shin_L=-5, thigh_R=-20, shin_R=-25, upperarm_L=-15, upperarm_R=20, forearm_R=25)
    walk_her = B.pose(B.STRAIGHT, thigh_L=-20, shin_L=-25, thigh_R=22, shin_R=-4, upperarm_L=20, forearm_L=25, upperarm_R=-12)
    F.figure(walk_him, at=(-0.25, 1.2, 0), facing=1, name="Him")
    her(walk_her, (0.22, 1.25, 0), facing=1)
    for i in range(8):                                                       # their footprints
        F.disc((-0.4 - i * 0.45, 1.2 - i * 0.2, 0.005), 0.05, F.flat(0.4))
        F.disc((0.1 - i * 0.45, 1.3 - i * 0.2, 0.005), 0.045, F.flat(0.4))
    F.camera((-1.2, -3.4, 1.05), (0.4, 4.0, 1.1), 34)


def wed_face():
    """5. Her, close: looking at him, his hand on her cheek. "Promise me you'll always come home." """
    F.wall(1.2, -4, 4, 4, g=0.38)
    bokeh(30, -1.6, 1.6, 0.7, 2.2, 1.1, 0.12)
    pose_her = B.pose(B.STRAIGHT, head=172, chest=178)
    her(pose_her, (0.0, 0, 0), facing=-1)
    hand = B.pose(B.STRAIGHT, upperarm_R=118, forearm_R=104, upperarm_L=20, forearm_L=20)
    F.figure(hand, at=(0.98, -0.3, -0.02), facing=-1, name="Him")
    F.camera((-0.32, -1.25, 1.22), (0.02, 0.0, 1.22), 30, roll=-3)


SHOTS = [wed_altar, wed_dance, wed_kitchen, wed_sea, wed_face]


def soften(path):
    """The look of remembering: greyscale, lifted blacks, a bloom round the lights, a soft
    vignette, a little grain."""
    from PIL import Image, ImageFilter, ImageChops
    im = Image.open(path).convert("L")
    bright = im.point(lambda v: max(0, v - 215) * 4)
    glow = bright.filter(ImageFilter.GaussianBlur(16))
    im = ImageChops.add(im, glow)
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    im = im.point(lambda v: int(26 + v * (238 - 26) / 255))
    w, h = im.size
    vig = Image.radial_gradient("L").resize((w, h)).point(lambda v: int(255 - v * 0.55))
    im = ImageChops.multiply(im, vig)
    noise = Image.effect_noise((w, h), 10).point(lambda v: v - 128 + 128)
    im = Image.blend(im, noise, 0.04)
    im.convert("RGB").save(path)


if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    os.makedirs(F.OUT, exist_ok=True)
    for i, shot in enumerate(SHOTS, 1):
        if only and i not in only:
            continue
        F.reset()
        shot()
        sc = bpy.context.scene
        path = os.path.join(F.OUT, f"wed_{i}.png")
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True)
        soften(path)
        print("rendered", path)
