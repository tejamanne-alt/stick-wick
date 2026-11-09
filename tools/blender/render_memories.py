"""Renders chapter 2's memory stills (docs/story/chapter2_bank.md, shot B16): the wedding ring
in box 417 gives him back his wife, Elena, and their life together. Eight soft stills in the
game's black and white, with him as the game's ink-black figure and her as a character of her
own in the same figure language: a white figure, slimmer, with big dark eyes that catch the
light, lashes, a blush and a small smile, dark hair (bangs, long waves, a bun or a ponytail as the
day asks), and her clothes for each moment (a raincoat dress, a gown and veil, a sundress and sun
hat, an apron). These are memories he wants: high-key, a glow round the lights, lifted blacks,
a soft vignette.

    /home/user/.venv-blender/bin/python tools/blender/render_memories.py [--quick] [1 2 ...]

Writes Assets/Resources/Story/Memories/wed_<n>.png (960x540). The montage times and its sound
(tools/audio/make_story_sfx.py: memory_elena) follow MEMORY_SECONDS."""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_flashbacks as F  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

F.OUT = os.path.join(F.ROOT, "Assets", "Resources", "Story", "Memories")
rnd = random.Random(21)

# how long each still holds in the montage (StoryBank.MemoryDur and the sound use the same)
MEMORY_SECONDS = [2.8, 2.8, 2.6, 2.6, 2.6, 2.4, 2.6, 5.6]

SKIN = 0.8        # Elena: a white figure
HAIR = 0.12
EYES = 0.06
BLUSH = 0.62
LIPS = 0.22


# ------------------------------------------------------------------ building blocks

def sph(c, r, g, scale=(1, 1, 1), mat=None):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=c, segments=24, ring_count=12)
    o = bpy.context.object
    o.scale = scale
    o.data.materials.append(mat or F.flat(g))
    return o


def rod(a, b, r, g, r2=None, mat=None, verts=16):
    """A cylinder (or a cone, with r2) from a to b."""
    a, b = Vector(a), Vector(b)
    d = b - a
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=d.length, vertices=verts, location=(a + b) / 2)
    else:
        bpy.ops.mesh.primitive_cone_add(radius1=r, radius2=r2, depth=d.length, vertices=verts, location=(a + b) / 2)
    o = bpy.context.object
    o.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    o.data.materials.append(mat or F.flat(g))
    return o


def to_bone(objs, arm, bone):
    """Hang parts on a bone (in the rest pose), so they follow the pose."""
    bpy.context.view_layer.update()
    for o in objs:
        mw = o.matrix_world.copy()
        o.parent = arm
        o.parent_type = "BONE"
        o.parent_bone = bone
        bpy.context.view_layer.update()
        o.matrix_world = mw


def rest(bone, end="head"):
    """A bone's head (or tail) in the rest pose (the armature at the origin)."""
    p, head, tail, y, _r, _rad = B.BONES[bone]
    v = Vector(head if end == "head" else tail) + Vector((0, y, 0))
    return v


# ------------------------------------------------------------------ Elena

def elena(pose, at, facing=1, tilt=0.0, outfit="dress", hair="down", smile="smile", flare=0.0, veil=False, hat=False, name="Elena"):
    """Elena, built on the game's rig: slimmer limbs, a big round head (a touch smaller than his),
    a face (eyes with a sparkle, lashes, blush, a smile), hair, and an outfit."""
    arm = B.build_armature()
    arm.name = name
    skin = F.flat(SKIN)
    # limbs: slender, with round joints so they read as one line
    for side in ("L", "R"):
        parts = [rod(rest(f"upperarm.{side}"), rest(f"upperarm.{side}", "tail"), 0.034, SKIN, mat=skin),
                 sph(rest(f"upperarm.{side}"), 0.036, SKIN, mat=skin)]
        to_bone(parts, arm, f"upperarm.{side}")
        fore = rest(f"forearm.{side}", "tail")
        parts = [rod(rest(f"forearm.{side}"), fore, 0.03, SKIN, mat=skin), sph(rest(f"forearm.{side}"), 0.032, SKIN, mat=skin),
                 sph(fore + Vector((0, 0, -0.02)), 0.042, SKIN, mat=skin)]                       # her hand
        to_bone(parts, arm, f"forearm.{side}")
        parts = [rod(rest(f"thigh.{side}"), rest(f"thigh.{side}", "tail"), 0.045, SKIN, mat=skin),
                 sph(rest(f"thigh.{side}"), 0.047, SKIN, mat=skin)]
        to_bone(parts, arm, f"thigh.{side}")
        foot = rest(f"shin.{side}", "tail")
        parts = [rod(rest(f"shin.{side}"), foot, 0.038, SKIN, mat=skin), sph(rest(f"shin.{side}"), 0.042, SKIN, mat=skin),
                 sph(foot + Vector((0.04, 0, -0.005)), 0.05, 0.3, scale=(1.5, 0.8, 0.7))]        # little shoes
        to_bone(parts, arm, f"shin.{side}")
    # the body: a slim waist between hips and chest, a neck
    to_bone([rod((0, 0, 0.66), (0, 0, 0.84), 0.13, SKIN, r2=0.085, mat=skin)], arm, "hips")
    to_bone([rod((0, 0, 0.84), (0, 0, 1.02), 0.085, SKIN, r2=0.12, mat=skin), rod((0, 0, 1.0), (0, 0, 1.08), 0.034, SKIN, mat=skin)], arm, "chest")
    # the head and the face (it looks along +x)
    hc = Vector((0, 0, B.HEAD_CENTER))
    face = [sph(hc, 0.205, SKIN, mat=skin)]
    for s in (1, -1):
        eye = hc + Vector((0.168, s * 0.072, 0.012))
        face.append(sph(eye, 1.0, EYES, scale=(0.022, 0.034, 0.05)))
        face.append(sph(eye + Vector((0.016, s * 0.006, 0.018)), 0.011, 1.0))                     # the catchlight
        face.append(sph(eye + Vector((0.017, -s * 0.008, -0.016)), 0.006, 1.0))
        for k in range(3):                                                                         # lashes
            a = math.radians(30 + k * 22)
            p0 = eye + Vector((0.006, s * (0.012 + 0.012 * k), 0.044 - 0.004 * k))
            face.append(rod(p0, p0 + Vector((0.008, s * 0.016 * math.cos(a), 0.022 * math.sin(a))), 0.0045, EYES, verts=6))
        face.append(sph(hc + Vector((0.165, s * 0.112, -0.05)), 1.0, BLUSH, scale=(0.012, 0.032, 0.018)))   # blush
    # the mouth: a small smile (or an open laugh, or an "oh")
    if smile == "laugh":
        face.append(sph(hc + Vector((0.196, 0, -0.07)), 1.0, LIPS, scale=(0.012, 0.03, 0.022)))
    elif smile == "oh":
        face.append(sph(hc + Vector((0.198, 0, -0.07)), 1.0, LIPS, scale=(0.01, 0.014, 0.018)))
    else:
        for k in range(7):
            u = (k - 3) / 3
            face.append(sph(hc + Vector((0.198 - 0.004 * u * u, u * 0.03, -0.075 + 0.012 * u * u)), 0.0055, LIPS))
    # hair: a cap over the back of the head, bangs over the brow, then the style
    hm = F.flat(HAIR)
    face.append(sph(hc + Vector((-0.075, 0, 0.035)), 0.21, HAIR, scale=(0.9, 1.04, 1.04), mat=hm))
    face.append(sph(hc + Vector((0.1, 0.035, 0.135)), 1.0, HAIR, scale=(0.115, 0.13, 0.06), mat=hm))
    face.append(sph(hc + Vector((0.11, -0.07, 0.115)), 1.0, HAIR, scale=(0.1, 0.1, 0.055), mat=hm))
    if hair == "down":
        for s in (1, -1):                                                                          # long waves past her shoulders
            for k in range(6):
                t = k / 5
                face.append(sph(hc + Vector((-0.02 - 0.05 * t + 0.02 * math.sin(t * 9), s * (0.17 - 0.01 * t), -0.04 - 0.36 * t)), 0.075 - 0.025 * t, HAIR, mat=hm))
        for k in range(6):
            t = k / 5
            face.append(sph(hc + Vector((-0.15 - 0.02 * t, 0, -0.06 - 0.36 * t)), 0.11 - 0.035 * t, HAIR, mat=hm))
    elif hair == "bun":
        face.append(sph(hc + Vector((-0.14, 0, 0.17)), 0.095, HAIR, mat=hm))
        for s in (1, -1):                                                                          # a ribbon on it
            face.append(rod(hc + Vector((-0.2, 0, 0.2)), hc + Vector((-0.24, s * 0.08, 0.24)), 0.025, 0.85, r2=0.005, verts=8))
        for s in (1, -1):                                                                          # two loose strands
            face.append(rod(hc + Vector((0.12, s * 0.16, 0.06)), hc + Vector((0.14, s * 0.17, -0.12)), 0.012, HAIR, mat=hm, verts=6))
    elif hair == "pony":
        for k in range(6):
            t = k / 5
            face.append(sph(hc + Vector((-0.2 - 0.07 * math.sin(t * 2.4), 0, 0.15 - 0.3 * t)), 0.07 - 0.02 * t, HAIR, mat=hm))
        face.append(sph(hc + Vector((-0.2, 0, 0.17)), 0.03, 0.85))                                  # the band
    if hat:                                                                                        # a wide sun hat with a ribbon
        face.append(rod(hc + Vector((0, 0, 0.13)), hc + Vector((0, 0, 0.145)), 0.36, 0.86, verts=40))
        face.append(sph(hc + Vector((-0.01, 0, 0.16)), 0.19, 0.86, scale=(1, 1, 0.7)))
        face.append(rod(hc + Vector((-0.01, 0, 0.15)), hc + Vector((-0.01, 0, 0.2)), 0.18, 0.1, verts=32))
    if veil:
        face.append(sph(hc + Vector((-0.02, 0, 0.19)), 1.0, 0.95, scale=(0.13, 0.17, 0.035)))       # a little crown of flowers
        for k in range(7):
            a = k / 6 * math.pi
            face.append(sph(hc + Vector((0.12 * math.cos(a) - 0.03, 0.17 * math.sin(a) - 0.085, 0.19)), 0.026, 1.0))
        face.append(rod(hc + Vector((-0.12, 0, 0.17)), hc + Vector((-0.3, 0, -0.75)), 0.05, 0.95, r2=0.3,
                        mat=F.halo(0.95, 0.55, "Veil"), verts=32))
    to_bone(face, arm, "head")
    # the outfit
    if outfit == "gown":
        to_bone([rod((0, 0, 0.84), (0, 0, 1.0), 0.09, 0.93, r2=0.125)], arm, "chest")
        skirt = [rod((0, 0, 0.84), (0, 0, 0.04), 0.09, 0.93, r2=0.5 + flare, verts=48),
                 rod((0, 0, 0.85), (0, 0, 0.83), 0.1, 0.62, verts=24)]                              # the sash
        for k in range(36):                                                                         # lace along the hem
            a = k / 36 * math.tau
            skirt.append(sph((math.cos(a) * (0.5 + flare), math.sin(a) * (0.5 + flare), 0.05), 0.022, 1.0))
        to_bone(skirt, arm, "hips")
    elif outfit in ("dress", "sundress"):
        g = 0.6 if outfit == "dress" else 0.9
        to_bone([rod((0, 0, 0.84), (0, 0, 1.0), 0.088, g, r2=0.122)], arm, "chest")
        skirt = [rod((0, 0, 0.84), (0, 0, 0.36), 0.088, g, r2=0.3 + flare, verts=40)]
        if outfit == "sundress":
            for k in range(26):                                                                    # polka dots
                z = rnd.uniform(0.42, 0.8)
                r = 0.088 + (0.84 - z) / 0.48 * (0.3 + flare - 0.088) + 0.003
                a = rnd.uniform(-1.6, 1.6)
                skirt.append(sph((math.cos(a) * r, math.sin(a) * r, z), 0.016, 0.2))
        else:
            skirt.append(rod((0, 0, 0.86), (0, 0, 0.82), 0.093, 0.12, verts=24))                    # a dark belt, a bow behind
            for s in (1, -1):
                skirt.append(rod((-0.09, 0, 0.84), (-0.15, s * 0.06, 0.88), 0.03, 0.12, r2=0.006, verts=8))
        to_bone(skirt, arm, "hips")
    elif outfit == "apron":
        to_bone([rod((0, 0, 0.84), (0, 0, 1.0), 0.09, 0.42, r2=0.13)], arm, "chest")              # a soft grey jumper
        to_bone([rod((0, 0, 0.84), (0, 0, 0.42), 0.09, 0.42, r2=0.24, verts=32),
                 F.box((0.11, 0, 0.66), (0.03, 0.2, 0.42), F.flat(0.95)),                            # the apron
                 sph((0.13, 0.0, 0.62), 1.0, 0.3, scale=(0.01, 0.035, 0.03))], arm, "hips")         # its heart pocket
        to_bone([F.box((0.115, 0, 0.94), (0.025, 0.14, 0.14), F.flat(0.95))], arm, "chest")
    B.apply_pose(arm, pose)
    arm.location = at
    arm.rotation_euler = (0, math.radians(-tilt), 0 if facing > 0 else math.pi)
    bpy.context.view_layer.update()
    return arm


def him(pose, at, facing=1, tilt=0.0, suit=False, name="Him"):
    """Him, as the game builds him (ink black, white eyes), in a suit for the wedding: a white
    shirt front, a bow tie, a flower in his lapel."""
    arm = F.figure(pose, at=at, facing=facing, tilt=tilt, name=name)
    if suit:
        parts = [F.box((0.15, 0, 0.95), (0.02, 0.09, 0.13), F.flat(0.95))]
        for s in (1, -1):
            parts.append(rod((0.16, 0, 1.0), (0.165, s * 0.045, 1.0), 0.022, 0.08, r2=0.005, verts=8))
        parts.append(sph((0.14, -0.08, 0.98), 0.024, 1.0))
        bpy.context.view_layer.update()
        pb = arm.pose.bones["chest"]
        M = arm.matrix_world @ pb.matrix @ pb.bone.matrix_local.inverted()      # rest space -> posed world
        for o in parts:
            o.matrix_world = M @ o.matrix_world
    return arm


def held(arm, bone, obj_fn):
    """A thing in a hand: built at the posed hand's position."""
    bpy.context.view_layer.update()
    p = arm.matrix_world @ arm.pose.bones[bone].tail
    return obj_fn(p)


# ------------------------------------------------------------------ scenery

def petals(n, x0, x1, z0, z1, y=0.4, g=0.95):
    for _ in range(n):
        F.disc((rnd.uniform(x0, x1), y + rnd.uniform(-0.4, 0.4), rnd.uniform(z0, z1)), rnd.uniform(0.012, 0.025), F.flat(g),
               rot=(rnd.uniform(0, 3), rnd.uniform(0, 3), rnd.uniform(0, 3)))


def bokeh(n, x0, x1, z0, z1, y, r=0.06):
    for _ in range(n):
        F.disc((rnd.uniform(x0, x1), y, rnd.uniform(z0, z1)), rnd.uniform(r * 0.6, r * 1.4), F.halo(1.0, 0.7, "Bokeh"))


def rain_streaks(n, x0, x1, z0, z1, y0, y1, g=0.85):
    for _ in range(n):
        x, y, z = rnd.uniform(x0, x1), rnd.uniform(y0, y1), rnd.uniform(z0, z1)
        rod((x, y, z), (x + 0.02, y, z - 0.16), 0.003, g, verts=4)


def string_lights(x0, x1, z, y, sag, n=30):
    for i in range(n):
        t = i / (n - 1)
        F.ball((x0 + t * (x1 - x0), y, z - math.sin(t * math.pi) * sag), 0.03, F.flat(1.0))


# ------------------------------------------------------------------ the eight memories

def m1_rain():
    """1. How they met: a night of rain outside a cafe. She is soaked and laughing; he holds his
    umbrella over her and lets the rain have him."""
    F.floor(0.22, -3, 4, -6, 6, trim=False)
    F.wall(2.2, -8, 8, 6, g=0.3)
    F.box((0.9, 2.1, 1.25), (2.6, 0.05, 1.6), F.halo(1.0, 0.85, "CafeWindow"))       # the cafe's lit window
    F.box((0.9, 1.6, 2.35), (3.2, 1.2, 0.05), F.flat(0.16))                          # its awning
    for x in (-0.6, -0.1, 0.4, 0.9, 1.4, 1.9, 2.4):
        F.box((x, 1.0, 2.29), (0.25, 0.02, 0.12), F.flat(0.9 if int(x * 10) % 10 == 0 else 0.16))
    bokeh(18, -3, 3, 0.6, 2.4, 3.0, 0.1)
    her_pose = B.pose(B.STRAIGHT, head=190, chest=182, upperarm_L=30, forearm_L=150, upperarm_R=-20, forearm_R=20)
    elena(her_pose, (0.25, 0, 0), facing=-1, outfit="dress", hair="down", smile="laugh")
    him_pose = B.pose(B.STRAIGHT, head=168, upperarm_R=150, forearm_R=165, upperarm_L=10, forearm_L=10)
    h = him(him_pose, (-0.32, 0.05, 0), facing=1)
    # the umbrella, over her
    top = Vector((0.12, 0.05, 1.82))
    sph(top, 0.55, 0.06, scale=(1, 1, 0.35))
    rod(top, top + Vector((-0.12, 0, -0.62)), 0.012, 0.06)
    rain_streaks(260, -2.6, 2.6, 0.0, 2.8, -0.6, 1.2)
    for k in range(12):                                                               # water dripping off its edge
        a = k / 12 * math.tau
        F.ball((top.x + math.cos(a) * 0.55, top.y + math.sin(a) * 0.55, top.z - 0.2 - rnd.uniform(0, 0.25)), 0.012, F.flat(0.95))
    F.camera((0.0, -3.4, 1.05), (0.0, 0.3, 1.15), 40)


def m2_proposal():
    """2. On a bridge at night, the city lit along the water: he is on one knee with a ring; her
    hands are at her mouth."""
    F.floor(0.26, -3, 2, -6, 6, trim=False)
    for x in range(-6, 7):                                                            # the bridge's railing
        F.box((x * 0.5, 0.8, 0.55), (0.03, 0.03, 1.1), F.flat(0.1))
    F.box((0, 0.8, 1.1), (7, 0.05, 0.05), F.flat(0.1))
    F.plane((0, 12, 3), (60, 14, 1), F.flat(0.14))                                    # the night
    F.disc((-2.2, 11.8, 4.2), 0.5, F.flat(1.0))                                       # the moon
    F.disc((-2.2, 11.7, 4.2), 2.2, F.halo(1.0, 0.25))
    for k in range(60):                                                               # the city across the water
        F.box((rnd.uniform(-8, 8), 10, rnd.uniform(0.3, 2.4)), (0.06, 0.02, 0.08), F.flat(rnd.choice((0.9, 1.0))))
    for k in range(40):                                                               # and its reflection
        F.box((rnd.uniform(-8, 8), 6, rnd.uniform(-0.4, -0.05)), (0.06, 0.02, 0.18), F.flat(0.55))
    bokeh(24, -3, 3, 0.4, 2.6, 2.4, 0.11)
    her_pose = B.pose(B.STRAIGHT, head=178, upperarm_L=150, forearm_L=200, upperarm_R=148, forearm_R=196)
    elena(her_pose, (0.45, 0, 0), facing=-1, outfit="dress", hair="bun", smile="oh")
    kneel = B.pose(B.STRAIGHT, thigh_L=90, shin_L=0, thigh_R=-10, shin_R=-95, upperarm_R=110, forearm_R=110, head=200, chest=190, dz=-0.3)
    h = him(kneel, (-0.32, 0.02, 0), facing=1)

    def ring_box(p):
        F.box((p.x + 0.04, p.y, p.z + 0.02), (0.06, 0.06, 0.045), F.flat(0.08))
        F.ball((p.x + 0.04, p.y, p.z + 0.06), 0.018, F.flat(1.0))
        F.disc((p.x + 0.04, p.y - 0.01, p.z + 0.06), 0.12, F.halo(1.0, 0.8))
    held(h, "hand.R", ring_box)
    F.camera((0.1, -3.6, 1.0), (0.1, 0.3, 1.05), 36)


def m3_altar():
    """3. The altar: under an arch of flowers, her hands in his, rings; their people behind; petals."""
    F.floor(0.42, -3, 6, -6, 6, trim=False)
    F.wall(5.6, -8, 8, 6, g=0.52)
    for x in (-2.6, 2.6):
        F.box((x, 5.55, 2.4), (1.1, 0.05, 2.8), F.halo(1.0, 0.9, "Window"))
    for i in range(30):                                                               # the arch of flowers
        a = math.pi * i / 29
        F.ball((math.cos(a) * 1.1, 0.55, 1.0 + math.sin(a) * 1.45), 0.1 + 0.03 * math.sin(i * 1.7), F.flat(0.86 if i % 2 else 0.68))
    for x in (-1.1, 1.1):
        F.box((x, 0.55, 0.5), (0.06, 0.06, 1.0), F.flat(0.3))
    for row in range(3):                                                              # the guests, soft grey, out of focus
        for k in range(8):
            x = -3.0 + k * 0.85 + row * 0.2
            if abs(x) < 1.2:
                continue
            F.ball((x, 2.4 + row * 0.9, 1.0), 0.17, F.flat(0.62 - row * 0.06))
            F.box((x, 2.4 + row * 0.9, 0.55), (0.3, 0.2, 0.55), F.flat(0.62 - row * 0.06))
    her_pose = B.pose(B.STRAIGHT, upperarm_L=60, forearm_L=86, upperarm_R=55, forearm_R=84, head=176)
    elena(her_pose, (0.34, 0, 0), facing=-1, outfit="gown", hair="down", veil=True, smile="smile")
    him_pose = B.pose(B.STRAIGHT, upperarm_L=60, forearm_L=88, upperarm_R=55, forearm_R=85, head=172)
    him(him_pose, (-0.36, 0, 0), suit=True)
    petals(80, -1.6, 1.6, 0.3, 2.8)
    F.camera((0.0, -3.6, 1.0), (0.0, 0.3, 1.05), 42)


def m4_dance():
    """4. The first dance: he dips her; her gown flares; string lights blur above them."""
    F.floor(0.3, -3, 4, -6, 6, trim=False)
    F.wall(3.6, -8, 8, 6, g=0.16)
    for k in range(5):
        string_lights(-3.4, 3.4, 2.7 + k * 0.25, 3.3, 0.45)
    bokeh(46, -3, 3, 1.4, 3.2, 2.6, 0.1)
    him_pose = B.pose(B.STRAIGHT, spine=168, chest=160, head=150, upperarm_L=80, forearm_L=100, upperarm_R=95, forearm_R=95,
                      thigh_L=20, shin_L=-5, thigh_R=-14, shin_R=-10)
    her_pose = B.pose(B.STRAIGHT, spine=212, chest=228, head=238, upperarm_L=200, forearm_L=215, upperarm_R=110, forearm_R=120,
                      thigh_L=40, shin_L=20, thigh_R=-10, shin_R=-6)
    him(him_pose, (-0.25, 0, 0), suit=True)
    elena(her_pose, (0.3, 0, 0.04), facing=-1, tilt=-18, outfit="gown", hair="down", flare=0.12, smile="laugh")
    F.camera((0.45, -3.2, 0.8), (0.05, 0.3, 0.95), 40, roll=-6)


def m5_home():
    """5. The house on Alder Lane: moving day. She rides on his back, laughing, the new keys held
    up; the door with its number, boxes on the porch, a tree."""
    F.floor(0.4, -3, 3, -6, 6, trim=False)
    F.wall(2.4, -6, 6, 5, g=0.62)                                                     # the house front, pale weatherboard
    for z in range(16):
        F.box((0, 2.35, 0.2 + z * 0.28), (12, 0.02, 0.02), F.flat(0.48))
    F.box((1.2, 2.33, 1.05), (0.95, 0.04, 2.1), F.flat(0.14))                         # the front door
    F.ball((1.55, 2.3, 1.0), 0.03, F.flat(0.9))
    F.box((1.2, 2.3, 2.3), (0.4, 0.03, 0.2), F.flat(0.92))                            # its number plate: 14
    F.box((-1.4, 2.32, 1.5), (1.0, 0.03, 0.9), F.halo(1.0, 0.75, "Window"))           # a window, morning light
    for k, (x, z) in enumerate(((2.4, 0.2), (2.6, 0.55), (2.1, 0.2))):                # boxes on the porch
        F.box((x, 1.6, z), (0.5, 0.45, 0.35), F.flat(0.5 + 0.06 * k))
    F.ball((-2.8, 1.6, 2.4), 1.0, F.flat(0.3))                                        # a tree
    F.box((-2.8, 1.6, 1.0), (0.12, 0.12, 2.0), F.flat(0.18))
    carry = B.pose(B.STRAIGHT, chest=172, head=176, upperarm_L=82, forearm_L=96, upperarm_R=82, forearm_R=96,
                   thigh_L=14, shin_L=-4, thigh_R=-10, shin_R=-14)
    him(carry, (-0.55, 0, 0), facing=1)
    F.box((-0.2, 0, 0.98), (0.46, 0.42, 0.4), F.flat(0.28))                          # the box he carries in
    F.box((-0.2, -0.212, 0.98), (0.46, 0.005, 0.06), F.flat(0.85))                    # its tape
    keys_up = B.pose(B.STRAIGHT, head=186, chest=184, upperarm_L=175, forearm_L=185, upperarm_R=-25, forearm_R=10,
                     thigh_L=6, shin_L=-2, thigh_R=-8, shin_R=-24)
    e = elena(keys_up, (0.35, 0.05, 0), facing=-1, outfit="dress", hair="pony", smile="laugh")

    def keys(p):
        F.ball((p.x, p.y, p.z + 0.04), 0.022, F.flat(0.95))
        F.box((p.x + 0.03, p.y, p.z + 0.08), (0.012, 0.012, 0.07), F.flat(0.95))
        F.disc((p.x + 0.02, p.y - 0.01, p.z + 0.08), 0.14, F.halo(1.0, 0.7))
    held(e, "hand.L", keys)
    petals(14, -2.5, 2.5, 1.6, 3.0, y=1.2, g=0.4)                                     # leaves falling
    F.camera((0.2, -3.9, 1.35), (0.15, 0.4, 1.35), 34)


def m6_kitchen():
    """6. A Sunday in their kitchen: flour everywhere. She laughs; there is a white handprint of
    flour on his black chest."""
    F.floor(0.36, -3, 3, -5, 5, trim=False)
    F.wall(1.6, -6, 6, 4, g=0.6)
    F.box((1.6, 1.55, 1.55), (1.3, 0.05, 1.2), F.halo(1.0, 0.95, "Window"))
    F.box((0.0, 1.1, 0.45), (4.0, 0.7, 0.9), F.flat(0.24))
    F.box((0.0, 0.76, 0.9), (4.0, 0.04, 0.04), F.flat(F.TRIM))
    for x in (-1.6, -1.2, -0.8):
        F.disc((x, 1.53, 1.75), 0.14, F.flat(0.14), rot=(math.pi / 2, 0, 0))
    F.box((-0.3, 1.0, 0.98), (0.35, 0.3, 0.12), F.flat(0.92))                         # the bowl of flour
    F.box((0.6, 1.0, 0.95), (0.25, 0.2, 0.1), F.flat(0.3))                            # a rolling pin
    her_pose = B.pose(B.STRAIGHT, head=198, chest=186, upperarm_L=60, forearm_L=150, upperarm_R=95, forearm_R=95)
    elena(her_pose, (0.32, 0, 0), facing=-1, outfit="apron", hair="bun", smile="laugh")
    him_pose = B.pose(B.STRAIGHT, head=176, chest=176, upperarm_L=30, forearm_L=120, upperarm_R=15, forearm_R=40)
    him(him_pose, (-0.3, 0.05, 0), facing=1)
    for k in range(5):                                                                # her floury handprint on his chest
        F.ball((-0.15, -0.12 + k * 0.012, 0.98 + (k - 2) * 0.03), 0.018, F.flat(0.98))
    F.ball((-0.15, -0.1, 0.94), 0.035, F.flat(0.98))
    F.ball((0.48, -0.16, 1.2), 0.03, F.flat(0.98))                                    # and a dab on her nose
    petals(60, -0.6, 0.9, 0.8, 1.7, y=0.5, g=0.98)                                    # flour in the air
    F.camera((0.0, -2.8, 1.15), (0.05, 0.3, 1.05), 38)


def m7_sea():
    """7. The sea at sundown: they walk away hand in hand; her sun hat; footprints; gulls."""
    F.plane((0, 40, 10), (200, 30, 1), F.flat(0.62))
    F.disc((2.5, 39, 2.0), 1.4, F.flat(1.0), rot=(math.pi / 2, 0, 0))
    F.disc((2.5, 38.9, 2.0), 5.0, F.halo(1.0, 0.35), rot=(math.pi / 2, 0, 0))
    F.box((0, 20, -0.02), (200, 40, 0.04), F.flat(0.3))
    for i in range(16):
        F.box((2.5 + rnd.uniform(-1.4, 1.4), 18 - i * 1.1, 0.01), (rnd.uniform(0.3, 1.2), 0.08, 0.01), F.flat(0.95))
    F.box((0, -2, -0.01), (40, 8, 0.02), F.flat(0.52))
    for k in range(5):                                                                # gulls
        x, z = rnd.uniform(-2, 3), rnd.uniform(2.4, 3.4)
        for s in (1, -1):
            rod((x, 12, z), (x + s * 0.18, 12, z + 0.06), 0.012, 0.15, verts=6)
    walk_him = B.pose(B.STRAIGHT, thigh_L=25, shin_L=-5, thigh_R=-20, shin_R=-25, upperarm_L=-15, upperarm_R=20, forearm_R=25)
    walk_her = B.pose(B.STRAIGHT, thigh_L=-20, shin_L=-25, thigh_R=22, shin_R=-4, upperarm_L=20, forearm_L=25, upperarm_R=-12)
    him(walk_him, (-0.25, 1.2, 0), facing=1)
    elena(walk_her, (0.22, 1.25, 0), facing=1, outfit="sundress", hair="down", hat=True, flare=0.05)
    for i in range(8):
        F.disc((-0.4 - i * 0.45, 1.2 - i * 0.2, 0.005), 0.05, F.flat(0.4))
        F.disc((0.1 - i * 0.45, 1.3 - i * 0.2, 0.005), 0.045, F.flat(0.4))
    F.camera((-1.2, -3.4, 1.05), (0.4, 4.0, 1.1), 36)


def m8_face():
    """8. Her, close: looking at him, his hand at her cheek. "Promise me you'll always come home." """
    F.wall(1.4, -4, 4, 4, g=0.36)
    bokeh(34, -1.6, 1.6, 0.7, 2.2, 1.2, 0.12)
    pose_her = B.pose(B.STRAIGHT, head=172, chest=178)
    elena(pose_her, (0.0, 0, 0), facing=-1, outfit="dress", hair="down", smile="smile")
    hand = B.pose(B.STRAIGHT, upperarm_R=118, forearm_R=104, upperarm_L=20, forearm_L=20)
    him(hand, (0.8, -0.3, -0.02), facing=-1)
    F.camera((-0.75, -1.75, 1.2), (-0.05, 0.0, 1.12), 50, roll=-3)


SHOTS = [m1_rain, m2_proposal, m3_altar, m4_dance, m5_home, m6_kitchen, m7_sea, m8_face]


def soften(path):
    """The look of remembering: greyscale, lifted blacks, a bloom round the lights only, a soft
    vignette, a little grain."""
    from PIL import Image, ImageFilter, ImageChops
    im = Image.open(path).convert("L")
    bright = im.point(lambda v: max(0, v - 215) * 4)
    glow = bright.filter(ImageFilter.GaussianBlur(16))
    im = ImageChops.add(im, glow)
    im = im.filter(ImageFilter.GaussianBlur(0.5))
    im = im.point(lambda v: int(24 + v * (240 - 24) / 255))
    w, h = im.size
    vig = Image.radial_gradient("L").resize((w, h)).point(lambda v: int(255 - v * 0.5))
    im = ImageChops.multiply(im, vig)
    noise = Image.effect_noise((w, h), 10)
    im = Image.blend(im, noise, 0.035)
    im.convert("RGB").save(path)


if __name__ == "__main__":
    only = [int(a) for a in sys.argv[1:] if a.isdigit()]
    os.makedirs(F.OUT, exist_ok=True)
    for f in os.listdir(F.OUT):
        if f.startswith("wed_") and f.endswith(".png") and int(f[4:-4]) > len(SHOTS):
            os.remove(os.path.join(F.OUT, f))
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
