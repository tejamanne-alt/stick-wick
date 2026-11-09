"""Chapter 4's stills (docs/story/chapter4_backyard.md).

Y3: her ribbon in the wet grass under the swing, close: the one thing in the game in colour, the
game's red; everything else black and white.
Y4: how she was taken, broken (the same harsh look as Elena's): hiding under the slide, the hand
reaching in, carried off over a shoulder reaching back ("PAPA!"), a small hand on the van's glass.

    /home/user/.venv-blender/bin/python tools/blender/render_backyard.py [1 2 ...]

Writes Assets/Resources/Story/Memories/ribbon.png and Assets/Resources/Story/Flash/taken_<n>.png."""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_maya as MY  # noqa: E402
import render_memories as RM  # noqa: E402
import render_flashbacks as F  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

rnd = random.Random(61)
RED = (0.62, 0.02, 0.03)


def red_mat():
    m = bpy.data.materials.new("Ribbon")
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*RED, 1)
    p.inputs["Roughness"].default_value = 0.55
    for name in ("Specular IOR Level", "Specular"):
        if name in p.inputs:
            p.inputs[name].default_value = 0.15
    try:
        p.inputs["Emission Color"].default_value = (*RED, 1)
        p.inputs["Emission Strength"].default_value = 0.35
    except KeyError:
        pass
    return m


def ribbon():
    """Her ribbon, in the wet grass under the swing: a satin bow, two tails, drops of rain on it."""
    F.floor(0.07, -3, 3, -4, 4, trim=False)
    for _ in range(420):                                                             # blades of grass
        x, y = rnd.uniform(-1.4, 1.4), rnd.uniform(-0.6, 1.6)
        RM.rod((x, y, 0), (x + rnd.uniform(-0.03, 0.03), y + rnd.uniform(-0.02, 0.02), rnd.uniform(0.03, 0.09)), 0.004, 0.1, verts=4)
    m = red_mat()
    c = Vector((0.0, 0.2, 0.03))
    for s in (1, -1):                                                                # the two loops
        RM.sph(c + Vector((s * 0.09, 0, 0.012)), 0.07, 0.5, scale=(1.3, 0.75, 0.32), mat=m)
    RM.sph(c + Vector((0, 0, 0.02)), 0.028, 0.5, mat=m)                              # the knot
    for s, a in ((1, -0.6), (-1, 0.5)):                                              # the tails
        RM.rod(c + Vector((s * 0.02, -0.01, 0.01)), c + Vector((s * 0.06 + a * 0.1, -0.16, 0.004)), 0.022, 0.5, r2=0.016, mat=m, verts=10)
    for _ in range(6):                                                               # drops
        F.ball((c.x + rnd.uniform(-0.16, 0.16), c.y + rnd.uniform(-0.1, 0.06), c.z + 0.04), rnd.uniform(0.003, 0.005), F.flat(0.9))
    for (loc, e, size) in (((0.2, -0.3, 0.9), 60, 0.6), ((-0.5, 0.6, 0.5), 25, 0.8)):  # the moon on it, a rim from behind
        ld = bpy.data.lights.new("RibbonLight", "AREA")
        ld.energy = e
        ld.size = size
        ob = bpy.data.objects.new("RibbonLight", ld)
        ob.location = loc
        ob.rotation_euler = (Vector((0.0, 0.2, 0.03)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.collection.objects.link(ob)
    F.box((0.1, 0.5, 0.62), (0.62, 0.24, 0.05), F.flat(0.1))                        # the swing seat above, out of focus
    F.camera((0.0, -0.42, 0.34), (0.0, 0.2, 0.03), 36)


def y1_hide():
    """She hid under the slide, as her mother said."""
    F.floor(0.18, -3, 3, -4, 4, trim=False)
    F.wall(2.0, -5, 5, 4, g=0.2)
    F.box((0.2, 0.6, 0.8), (2.2, 0.6, 0.04), F.flat(0.45), rot=(0, 0.55, 0))       # the slide over her
    crouch = B.pose(B.STRAIGHT, spine=150, chest=140, head=170, thigh_L=110, shin_L=-30, thigh_R=105, shin_R=-35,
                    upperarm_L=60, forearm_L=150, upperarm_R=55, forearm_R=145, dz=-0.35)
    MY.maya(crouch, (0.1, 0.62, 0.0), facing=-1, size=0.6, smile="blow")
    F.camera((-0.9, -1.7, 0.5), (0.1, 0.6, 0.45), 44, roll=-6)


def y2_hand():
    """A man's hand reaching in under the slide; she screams."""
    F.floor(0.16, -3, 3, -4, 4, trim=False)
    F.wall(2.0, -5, 5, 4, g=0.18)
    F.box((0.2, 0.6, 0.8), (2.2, 0.6, 0.04), F.flat(0.4), rot=(0, 0.55, 0))
    scream = B.pose(B.STRAIGHT, spine=196, chest=200, head=205, thigh_L=100, shin_L=-20, thigh_R=95, shin_R=-30,
                    upperarm_L=120, forearm_L=170, upperarm_R=110, forearm_R=160, dz=-0.35)
    MY.maya(scream, (0.45, 0.62, 0.0), facing=-1, size=0.6, smile="laugh")
    reach = B.pose(B.STRAIGHT, spine=130, chest=110, head=120, upperarm_R=95, forearm_R=92, thigh_L=40, thigh_R=-20)
    MY.thug(reach, (-0.85, 0.55, 0), facing=1)
    F.camera((0.6, -1.6, 0.55), (-0.1, 0.6, 0.6), 46, roll=7)


def y3_taken():
    """Over his shoulder, carried off, reaching back for her father: "PAPA!" """
    F.floor(0.14, -3, 3, -4, 4, trim=False)
    F.wall(3.0, -6, 6, 5, g=0.15)
    walk = B.pose(B.STRAIGHT, thigh_L=25, shin_L=-5, thigh_R=-20, shin_R=-25, upperarm_L=100, forearm_L=150, upperarm_R=-10)
    MY.thug(walk, (0.3, 0.4, 0), facing=1)
    carried = B.rotated(B.pose(B.STRAIGHT, upperarm_L=200, forearm_L=200, upperarm_R=190, forearm_R=200, head=200, thigh_L=10, thigh_R=-10), -100, dz=0.0)
    MY.maya(carried, (0.15, 0.32, 0.95), facing=-1, size=0.6, smile="laugh")
    F.camera((-1.4, -2.2, 1.1), (0.0, 0.4, 1.15), 42, roll=-8)


def y4_glass():
    """The van's back doors slamming; a small hand pressed on the glass."""
    F.box((0, 0.6, 1.2), (3.0, 0.1, 2.4), F.flat(0.55))                              # the doors
    F.box((0.0, 0.54, 1.2), (0.03, 0.02, 2.4), F.flat(0.1))
    F.box((0.45, 0.54, 1.75), (0.6, 0.02, 0.45), F.flat(0.06))                       # the glass, dark inside
    for k in range(5):                                                               # her hand on it, pale
        F.ball((0.42 + (k - 2) * 0.03, 0.52, 1.8 + (0.05 if 0 < k < 4 else 0.02)), 0.012, F.flat(0.9))
    F.ball((0.42, 0.52, 1.74), 0.035, F.flat(0.9))
    F.camera((0.2, -1.2, 1.6), (0.35, 0.5, 1.7), 44, roll=4)


SHOTS = [y1_hide, y2_hand, y3_taken, y4_glass]


def keep_red(path):
    """Black and white, except for the red."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            lum = int(0.3 * r + 0.59 * g + 0.11 * b)
            if r > 45 and r > g * 1.25 and r > b * 1.25:
                px[x, y] = (min(255, int(40 + lum * 1.6)), int(lum * 0.22), int(lum * 0.24))
            else:
                px[x, y] = (lum, lum, lum)
    im.save(path)


if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    os.makedirs(MY.MEM, exist_ok=True)
    os.makedirs(MY.FLASH, exist_ok=True)
    jobs = [(0, ribbon, os.path.join(MY.MEM, "ribbon.png"), keep_red)]
    jobs += [(i, s, os.path.join(MY.FLASH, f"taken_{i}.png"), MY.harsh) for i, s in enumerate(SHOTS, 1)]
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
