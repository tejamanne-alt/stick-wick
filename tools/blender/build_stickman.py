"""Generate the Project INK stickman: mesh, rig and animation clips.

Run with the Blender Python module (bpy):
    python tools/blender/build_stickman.py [--preview]

Outputs:
    Assets/Art/Stickman/Stickman.fbx       model + rig + every clip as an FBX take
    Assets/Art/Stickman/stickman_clips.json clip timing data read by the Unity setup
    docs/previews/<clip>.png                contact sheets of the hand-keyed clips (with --preview)

Motion: most moves are real motion capture (CMU database) retargeted by retarget.py, with
the windows chosen in mocap_clips.py; hit reactions and the enemy wind-up are hand-keyed
below. Pass --no-mocap to build only the hand-keyed set. Preview mocap clips with
preview_mocap.py.

Posing model
------------
The fight reads side-on, so every bone bends in the X/Z plane (character faces +X).
A pose gives each bone an ABSOLUTE angle in degrees, measured from straight down
(0) towards the front (+90); straight up is 180, pointing backwards is -90.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "Stickman")
PREVIEW_DIR = os.path.join(ROOT, "docs", "previews")
FPS = 30

# ---------------------------------------------------------------- skeleton
# name: (parent, head, tail, depth y, rest angle, radius of the limb mesh)
# Chibi proportions: about 1.45 m tall, a big round head, a round belly and chest, short
# thick limbs and little round feet. Poses are angles, so they all carry over; explicit
# hip offsets in the poses are written for the original 0.95 m hip height and scaled by S.
HIP_Z = 0.70
THIGH, SHIN = 0.34, 0.30
UPPER, FORE, HAND = 0.21, 0.19, 0.07
SHOULDER = 1.00
D = 0.07                                   # left/right depth offset of the limbs
BONES = {
    "hips":      (None,    (0, 0, HIP_Z),  (0, 0, 0.78), 0.0, 180, 0.11),
    "spine":     ("hips",  (0, 0, 0.78),   (0, 0, 0.92), 0.0, 180, 0.12),
    "chest":     ("spine", (0, 0, 0.92),   (0, 0, 1.04), 0.0, 180, 0.12),
    "head":      ("chest", (0, 0, 1.05),   (0, 0, 1.30), 0.0, 180, 0.0),
    "upperarm.L": ("chest", (0, 0, SHOULDER), (0, 0, SHOULDER - UPPER), D, 0, 0.060),
    "forearm.L": ("upperarm.L", (0, 0, SHOULDER - UPPER), (0, 0, SHOULDER - UPPER - FORE), D, 0, 0.058),
    "upperarm.R": ("chest", (0, 0, SHOULDER), (0, 0, SHOULDER - UPPER), -D, 0, 0.060),
    "forearm.R": ("upperarm.R", (0, 0, SHOULDER - UPPER), (0, 0, SHOULDER - UPPER - FORE), -D, 0, 0.058),
    # hands carry no mesh: their angle is the weapon's direction (blade or barrel)
    "hand.L":    ("forearm.L", (0, 0, SHOULDER - UPPER - FORE), (0, 0, SHOULDER - UPPER - FORE - HAND), D, 0, 0.0),
    "hand.R":    ("forearm.R", (0, 0, SHOULDER - UPPER - FORE), (0, 0, SHOULDER - UPPER - FORE - HAND), -D, 0, 0.0),
    "thigh.L":   ("hips",  (0, 0, HIP_Z), (0, 0, HIP_Z - THIGH), D, 0, 0.072),
    "shin.L":    ("thigh.L", (0, 0, HIP_Z - THIGH), (0, 0, HIP_Z - THIGH - SHIN), D, 0, 0.066),
    "thigh.R":   ("hips",  (0, 0, HIP_Z), (0, 0, HIP_Z - THIGH), -D, 0, 0.072),
    "shin.R":    ("thigh.R", (0, 0, HIP_Z - THIGH), (0, 0, HIP_Z - THIGH - SHIN), -D, 0, 0.066),
}
HEAD_RADIUS = 0.21
HEAD_CENTER = 1.24
FOOT_Z = HIP_Z - THIGH - SHIN  # shin tip height in the rest pose (0.06)
S = HIP_Z / 0.95               # scale for explicit hip offsets written for the original body
HANG_DROP = SHOULDER + UPPER + FORE + 0.03   # hands above the feet with the arms straight up (1.43)

# ---------------------------------------------------------------- poses
# L = far side (lead side in the guard), R = near side (rear / power side).
STANCE = dict(hips=180, spine=176, chest=172, head=178,
              **{"upperarm.L": 50, "forearm.L": 150, "upperarm.R": 25, "forearm.R": 160,
                 "thigh.L": 28, "shin.L": 2, "thigh.R": -18, "shin.R": -8})


def pose(base=STANCE, dz=None, dx=0.0, **kw):
    """A pose = base angles overridden by keyword angles. Dotted bone names use _L/_R."""
    p = dict(base)
    for k, v in kw.items():
        p[k.replace("_L", ".L").replace("_R", ".R")] = v
    p["_dz"] = dz  # None -> keep the lowest foot on the ground
    p["_dx"] = dx
    return p


def rotated(p, delta, dz=None):
    """Rotate the whole body rigidly (used for falls and flips)."""
    q = {k: (v + delta if not k.startswith("_") else v) for k, v in p.items()}
    q["_dz"] = dz
    q.setdefault("_dx", 0.0)
    return q


def mirror_legs_arms(p):
    q = dict(p)
    for a, b in (("upperarm.L", "upperarm.R"), ("forearm.L", "forearm.R"), ("hand.L", "hand.R"),
                 ("thigh.L", "thigh.R"), ("shin.L", "shin.R")):
        if a in p or b in p:
            q[a], q[b] = p.get(b, p.get("forearm" + b[-2:])), p.get(a, p.get("forearm" + a[-2:]))
    return q


RUN_A = pose(spine=166, chest=164, head=172,
             upperarm_L=-40, forearm_L=30, upperarm_R=45, forearm_R=125,
             thigh_L=36, shin_L=12, thigh_R=-26, shin_R=-52)
RUN_PASS_A = pose(spine=166, chest=164, head=172,
                  upperarm_L=0, forearm_L=70, upperarm_R=5, forearm_R=95,
                  thigh_L=-4, shin_L=-6, thigh_R=55, shin_R=-35, dz=-0.02)
RUN_B = mirror_legs_arms(RUN_A)
RUN_PASS_B = mirror_legs_arms(RUN_PASS_A)

STRAIGHT = pose(spine=180, chest=180, head=180, upperarm_L=10, forearm_L=10,
                upperarm_R=-10, forearm_R=-10, thigh_L=4, shin_L=2, thigh_R=-4, shin_R=-2)

# Each clip: frames, loop flag, hit frame (or None), and (frame, pose) keys.
CLIPS = {
    "idle": dict(frames=24, loop=True, hit=None, keys=[
        (0, STANCE),
        (12, pose(chest=169, spine=175, forearm_L=146, forearm_R=156, dz=-0.075)),
        (24, STANCE)]),
    "run": dict(frames=16, loop=True, hit=None, keys=[
        (0, RUN_A), (4, RUN_PASS_A), (8, RUN_B), (12, RUN_PASS_B), (16, RUN_A)]),
    "jab": dict(frames=12, loop=False, hit=4, keys=[
        (0, STANCE),
        (2, pose(chest=176, upperarm_L=60, forearm_L=140)),
        (4, pose(spine=172, chest=164, upperarm_L=90, forearm_L=91, thigh_L=34)),
        (7, pose(spine=172, chest=165, upperarm_L=88, forearm_L=96, thigh_L=34)),
        (12, STANCE)]),
    "cross": dict(frames=14, loop=False, hit=5, keys=[
        (0, STANCE),
        (2, pose(chest=184, upperarm_R=10, forearm_R=150)),
        (5, pose(spine=168, chest=158, head=170, upperarm_R=93, forearm_R=90,
                 upperarm_L=30, forearm_L=160, thigh_L=36, thigh_R=-34, shin_R=-20)),
        (8, pose(spine=168, chest=160, head=170, upperarm_R=90, forearm_R=95,
                 upperarm_L=30, forearm_L=160, thigh_L=36, thigh_R=-34, shin_R=-20)),
        (14, STANCE)]),
    "front_kick": dict(frames=16, loop=False, hit=6, keys=[
        (0, STANCE),
        (3, pose(chest=186, spine=184, thigh_R=95, shin_R=-5, thigh_L=6, shin_L=0)),
        (6, pose(chest=194, spine=188, head=184, thigh_R=92, shin_R=92, thigh_L=4, shin_L=0,
                 upperarm_L=70, forearm_L=150, upperarm_R=-20, forearm_R=60)),
        (9, pose(chest=188, spine=184, thigh_R=95, shin_R=10, thigh_L=6, shin_L=0)),
        (16, STANCE)]),
    "roundhouse": dict(frames=18, loop=False, hit=7, keys=[
        (0, STANCE),
        (3, pose(chest=192, spine=188, thigh_R=105, shin_R=15, thigh_L=-4, shin_L=0)),
        (7, pose(chest=205, spine=196, head=190, thigh_R=118, shin_R=122, thigh_L=-8, shin_L=0,
                 upperarm_L=115, forearm_L=135, upperarm_R=-45, forearm_R=-10)),
        (10, pose(chest=202, spine=195, head=188, thigh_R=116, shin_R=118, thigh_L=-8, shin_L=0,
                  upperarm_L=110, forearm_L=135, upperarm_R=-45, forearm_R=-10)),
        (13, pose(chest=190, spine=186, thigh_R=100, shin_R=20, thigh_L=-4, shin_L=0)),
        (18, STANCE)]),
    "back_kick": dict(frames=16, loop=False, hit=6, keys=[
        (0, STANCE),
        (3, pose(spine=160, chest=150, head=150, thigh_R=70, shin_R=-50, thigh_L=10, shin_L=0,
                 upperarm_L=80, forearm_L=120, upperarm_R=70, forearm_R=120)),
        (6, pose(spine=150, chest=138, head=135, thigh_R=-92, shin_R=-92, thigh_L=12, shin_L=0,
                 upperarm_L=95, forearm_L=110, upperarm_R=85, forearm_R=110)),
        (9, pose(spine=152, chest=140, head=138, thigh_R=-88, shin_R=-90, thigh_L=12, shin_L=0,
                 upperarm_L=95, forearm_L=110, upperarm_R=85, forearm_R=110)),
        (16, STANCE)]),
    "flying_kick": dict(frames=20, loop=False, hit=10, keys=[
        (0, STANCE),
        (3, pose(spine=165, chest=160, thigh_L=70, shin_L=-15, thigh_R=55, shin_R=-30,
                 upperarm_L=-30, upperarm_R=-40, forearm_L=20, forearm_R=20, dz=-0.22)),
        (6, pose(spine=178, chest=182, thigh_L=95, shin_L=-20, thigh_R=70, shin_R=-50,
                 upperarm_L=150, forearm_L=170, upperarm_R=120, forearm_R=160, dz=0.40)),
        (10, pose(spine=190, chest=198, head=188, thigh_L=96, shin_L=94, thigh_R=-30, shin_R=-110,
                  upperarm_L=155, forearm_L=175, upperarm_R=-60, forearm_R=-25, dz=0.50)),
        (13, pose(spine=188, chest=195, thigh_L=94, shin_L=90, thigh_R=-25, shin_R=-100,
                  upperarm_L=150, forearm_L=170, upperarm_R=-55, forearm_R=-20, dz=0.30)),
        (17, pose(spine=168, chest=164, thigh_L=60, shin_L=-10, thigh_R=-20, shin_R=-60,
                  upperarm_L=60, forearm_L=140, upperarm_R=10, forearm_R=120, dz=-0.18)),
        (20, STANCE)]),
    "dodge": dict(frames=14, loop=False, hit=None, keys=[
        (0, STANCE),
        (3, pose(spine=170, chest=168, thigh_L=60, shin_L=-10, thigh_R=40, shin_R=-40, dz=-0.20)),
        (7, rotated(pose(spine=180, chest=180, head=185, thigh_L=110, shin_L=-30, thigh_R=100,
                         shin_R=-40, upperarm_L=130, forearm_L=60, upperarm_R=120, forearm_R=50), 55,
                    dz=0.10)),
        (11, pose(spine=172, chest=170, thigh_L=50, shin_L=-10, thigh_R=-10, shin_R=-40, dz=-0.15)),
        (14, STANCE)]),
    "hit_front": dict(frames=12, loop=False, hit=None, keys=[
        (0, STANCE),
        (2, pose(spine=196, chest=210, head=222, upperarm_L=150, forearm_L=200,
                 upperarm_R=-35, forearm_R=20, thigh_L=10, thigh_R=-30, shin_R=-20)),
        (6, pose(spine=192, chest=200, head=205, upperarm_L=120, forearm_L=180,
                 upperarm_R=-20, forearm_R=40, thigh_L=12, thigh_R=-28, shin_R=-18)),
        (12, STANCE)]),
    "hit_back": dict(frames=12, loop=False, hit=None, keys=[
        (0, STANCE),
        (2, pose(spine=160, chest=146, head=135, upperarm_L=-60, forearm_L=-30,
                 upperarm_R=-75, forearm_R=-40, thigh_L=35, thigh_R=-10)),
        (6, pose(spine=166, chest=156, head=150, upperarm_L=-40, forearm_L=0,
                 upperarm_R=-50, forearm_R=-10, thigh_L=32, thigh_R=-12)),
        (12, STANCE)]),
    "knockdown": dict(frames=30, loop=False, hit=None, keys=[
        (0, STANCE),
        (3, pose(spine=200, chest=215, head=228, upperarm_L=160, forearm_L=210,
                 upperarm_R=150, forearm_R=200, thigh_L=30, thigh_R=10, dz=0.05)),
        (10, rotated(pose(STRAIGHT, upperarm_L=150, forearm_L=170, upperarm_R=140,
                          forearm_R=160, thigh_L=25, shin_L=0, thigh_R=5, shin_R=-10), 55, dz=-0.25)),
        (16, rotated(pose(STRAIGHT, upperarm_L=170, forearm_L=175, upperarm_R=160, forearm_R=165,
                          thigh_L=15, shin_L=-10, thigh_R=5, shin_R=-5), 90, dz=-0.84)),
        (30, rotated(pose(STRAIGHT, upperarm_L=165, forearm_L=170, upperarm_R=150, forearm_R=155,
                          thigh_L=20, shin_L=-15, thigh_R=5, shin_R=-5), 90, dz=-0.85))]),
    "windup": dict(frames=10, loop=False, hit=None, keys=[
        (0, STANCE),
        (6, pose(spine=184, chest=192, head=186, upperarm_R=-50, forearm_R=140,
                 upperarm_L=70, forearm_L=110, thigh_L=30, thigh_R=-25, shin_R=-15)),
        (10, pose(spine=185, chest=194, head=187, upperarm_R=-55, forearm_R=140,
                  upperarm_L=72, forearm_L=110, thigh_L=30, thigh_R=-25, shin_R=-15))]),
}


# ---------------------------------------------------------------- weapon clips
# hand_R is the weapon's direction (blade or barrel), absolute like every other angle:
# 0 = pointing down, 90 = forward, 180 = up, -90 = backwards.

SW_IDLE = pose(spine=174, chest=168, head=176,
               upperarm_R=38, forearm_R=72, hand_R=118,      # blade forward and up
               upperarm_L=-25, forearm_L=15,                 # free hand back
               thigh_L=36, shin_L=-4, thigh_R=-30, shin_R=-22)
SW_LUNGE = dict(thigh_L=52, shin_L=2, thigh_R=-48, shin_R=-14)

GUN_IDLE = pose(spine=176, chest=172, head=178,
                upperarm_R=40, forearm_R=118, hand_R=92,      # pistol close to the chest, level
                upperarm_L=46, forearm_L=122, hand_L=92,      # support hand on the grip
                thigh_L=26, shin_L=0, thigh_R=-20, shin_R=-10)

HANG = pose(STRAIGHT, spine=180, chest=180, head=182, upperarm_L=176, forearm_L=179, hand_L=179,
            upperarm_R=172, forearm_R=176, hand_R=176, thigh_L=8, shin_L=0, thigh_R=-5, shin_R=-8, dz=0.0)

WEAPON_CLIPS = {
    # ---- sword (all hand-keyed for big, readable arcs, like the reference)
    "sword_idle": dict(frames=30, loop=True, hit=None, keys=[
        (0, SW_IDLE),
        (15, pose(SW_IDLE, chest=165, forearm_R=76, hand_R=122, dz=-0.10)),
        (30, SW_IDLE)]),
    # Fast and snappy: a 1-2 frame anticipation, the cut on frame 3-5, a held follow-through.
    "sword_a": dict(frames=11, loop=False, hit=3, keys=[       # rising diagonal cut
        (0, SW_IDLE),
        (1, pose(SW_IDLE, spine=178, chest=184, upperarm_R=-35, forearm_R=-15, hand_R=-70)),
        (3, pose(SW_IDLE, spine=166, chest=158, head=170, upperarm_R=118, forearm_R=140, hand_R=172, **SW_LUNGE)),
        (5, pose(SW_IDLE, spine=166, chest=160, upperarm_R=162, forearm_R=182, hand_R=220, **SW_LUNGE)),
        (8, pose(SW_IDLE, spine=168, chest=162, upperarm_R=150, forearm_R=172, hand_R=208, **SW_LUNGE)),
        (11, SW_IDLE)]),
    "sword_b": dict(frames=12, loop=False, hit=4, keys=[       # overhead diagonal cut
        (0, SW_IDLE),
        (2, pose(SW_IDLE, spine=180, chest=188, upperarm_R=168, forearm_R=200, hand_R=232,
                 upperarm_L=120, forearm_L=160)),
        (4, pose(SW_IDLE, spine=160, chest=148, head=164, upperarm_R=72, forearm_R=58, hand_R=38,
                 upperarm_L=-30, forearm_L=0, thigh_L=58, shin_L=0, thigh_R=-52, shin_R=-10)),
        (6, pose(SW_IDLE, spine=158, chest=146, head=164, upperarm_R=18, forearm_R=2, hand_R=-32,
                 upperarm_L=-30, forearm_L=0, thigh_L=60, shin_L=0, thigh_R=-54, shin_R=-10)),
        (9, pose(SW_IDLE, spine=160, chest=150, head=166, upperarm_R=22, forearm_R=8, hand_R=-25,
                 upperarm_L=-30, forearm_L=0, thigh_L=58, shin_L=0, thigh_R=-52, shin_R=-10)),
        (12, SW_IDLE)]),
    "sword_c": dict(frames=15, loop=False, hit=5, keys=[       # lunging thrust (heavy)
        (0, SW_IDLE),
        (2, pose(SW_IDLE, spine=182, chest=188, upperarm_R=-25, forearm_R=62, hand_R=90,
                 thigh_L=20, thigh_R=-15, shin_R=-30)),
        (5, pose(SW_IDLE, spine=154, chest=146, head=158, upperarm_R=92, forearm_R=92, hand_R=92,
                 upperarm_L=-70, forearm_L=-50, thigh_L=76, shin_L=4, thigh_R=-66, shin_R=-28)),
        (10, pose(SW_IDLE, spine=156, chest=148, head=160, upperarm_R=90, forearm_R=90, hand_R=90,
                  upperarm_L=-65, forearm_L=-45, thigh_L=72, shin_L=4, thigh_R=-62, shin_R=-28)),
        (15, SW_IDLE)]),
    "sword_back": dict(frames=12, loop=False, hit=4, keys=[    # slash behind without turning
        (0, SW_IDLE),
        (2, pose(SW_IDLE, chest=164, upperarm_R=100, forearm_R=150, hand_R=185)),
        (4, pose(SW_IDLE, spine=186, chest=196, head=205, upperarm_R=-100, forearm_R=-112, hand_R=-128,
                 upperarm_L=60, forearm_L=90, thigh_L=20, thigh_R=-42, shin_R=-6)),
        (7, pose(SW_IDLE, spine=184, chest=192, head=200, upperarm_R=-120, forearm_R=-140, hand_R=-165,
                 upperarm_L=60, forearm_L=90, thigh_L=20, thigh_R=-42, shin_R=-6)),
        (12, SW_IDLE)]),
    "sword_leap": dict(frames=22, loop=False, hit=11, keys=[   # leaping overhead slam
        (0, SW_IDLE),
        (2, pose(SW_IDLE, spine=168, chest=164, thigh_L=70, shin_L=-10, thigh_R=50, shin_R=-40, dz=-0.25)),
        (7, pose(SW_IDLE, spine=184, chest=192, head=186, upperarm_R=180, forearm_R=205, hand_R=240,
                 upperarm_L=150, forearm_L=170, thigh_L=100, shin_L=-20, thigh_R=60, shin_R=-70, dz=0.65)),
        (11, pose(SW_IDLE, spine=150, chest=138, head=150, upperarm_R=62, forearm_R=40, hand_R=18,
                  upperarm_L=-40, forearm_L=-10, thigh_L=82, shin_L=8, thigh_R=-70, shin_R=-62, dz=-0.32)),
        (17, pose(SW_IDLE, spine=152, chest=140, head=152, upperarm_R=60, forearm_R=38, hand_R=15,
                  upperarm_L=-40, forearm_L=-10, thigh_L=82, shin_L=8, thigh_R=-70, shin_R=-62, dz=-0.30)),
        (22, SW_IDLE)]),
    "sword_rise": dict(frames=15, loop=False, hit=4, keys=[    # rising launcher: sends them up for the air chase
        (0, SW_IDLE),
        (2, pose(SW_IDLE, spine=164, chest=156, head=166, upperarm_R=-20, forearm_R=-40, hand_R=-78,
                 upperarm_L=40, forearm_L=60, thigh_L=64, shin_L=-20, thigh_R=-44, shin_R=-30, dz=-0.14)),
        (4, pose(SW_IDLE, spine=186, chest=194, head=190, upperarm_R=150, forearm_R=165, hand_R=172,
                 upperarm_L=-40, forearm_L=-20, thigh_L=24, shin_L=0, thigh_R=-14, shin_R=-36, dz=0.08)),
        (7, pose(SW_IDLE, spine=188, chest=198, head=194, upperarm_R=172, forearm_R=186, hand_R=196,
                 upperarm_L=-46, forearm_L=-24, thigh_L=20, shin_L=0, thigh_R=-10, shin_R=-40, dz=0.10)),
        (11, pose(SW_IDLE, spine=184, chest=190, head=188, upperarm_R=160, forearm_R=178, hand_R=188)),
        (15, SW_IDLE)]),
    "sword_iai": dict(frames=26, loop=False, hit=4, keys=[     # the draw-cut finisher: a blur through them, then stillness
        (0, SW_IDLE),
        (2, pose(SW_IDLE, spine=166, chest=158, head=168, upperarm_R=12, forearm_R=-22, hand_R=-82,
                 upperarm_L=34, forearm_L=44, thigh_L=62, shin_L=-32, thigh_R=-32, shin_R=-42, dz=-0.16)),
        (4, pose(SW_IDLE, spine=150, chest=140, head=150, upperarm_R=98, forearm_R=104, hand_R=110,
                 upperarm_L=-72, forearm_L=-62, thigh_L=82, shin_L=0, thigh_R=-76, shin_R=-14, dz=-0.30)),
        (7, pose(SW_IDLE, spine=150, chest=142, head=152, upperarm_R=128, forearm_R=160, hand_R=222,
                 upperarm_L=-72, forearm_L=-62, thigh_L=82, shin_L=0, thigh_R=-76, shin_R=-14, dz=-0.30)),
        (18, pose(SW_IDLE, spine=152, chest=146, head=156, upperarm_R=122, forearm_R=152, hand_R=214,
                  upperarm_L=-68, forearm_L=-58, thigh_L=78, shin_L=0, thigh_R=-72, shin_R=-14, dz=-0.28)),
        (26, SW_IDLE)]),

    # ---- gun-fu (pistol in the right hand)
    "gun_idle": dict(frames=30, loop=True, hit=None, keys=[
        (0, GUN_IDLE),
        (15, pose(GUN_IDLE, chest=169, dz=-0.07)),
        (30, GUN_IDLE)]),
    "gun_shoot": dict(frames=10, loop=False, hit=2, keys=[     # extend and fire forward
        (0, GUN_IDLE),
        (2, pose(GUN_IDLE, upperarm_R=88, forearm_R=90, hand_R=90, upperarm_L=80, forearm_L=92, hand_L=90)),
        (4, pose(GUN_IDLE, chest=175, head=176, upperarm_R=92, forearm_R=100, hand_R=112,
                 upperarm_L=82, forearm_L=96, hand_L=100)),
        (7, pose(GUN_IDLE, upperarm_R=88, forearm_R=90, hand_R=90, upperarm_L=80, forearm_L=92, hand_L=90)),
        (10, pose(GUN_IDLE, upperarm_R=88, forearm_R=90, hand_R=90, upperarm_L=80, forearm_L=92, hand_L=90))]),
    "gun_shoot_back": dict(frames=12, loop=False, hit=3, keys=[  # fire behind without turning
        (0, GUN_IDLE),
        (3, pose(GUN_IDLE, chest=178, head=205, upperarm_R=-88, forearm_R=-90, hand_R=-90,
                 upperarm_L=40, forearm_L=110)),
        (5, pose(GUN_IDLE, chest=180, head=205, upperarm_R=-92, forearm_R=-100, hand_R=-112,
                 upperarm_L=40, forearm_L=110)),
        (9, pose(GUN_IDLE, chest=178, head=205, upperarm_R=-88, forearm_R=-90, hand_R=-90,
                 upperarm_L=40, forearm_L=110)),
        (12, GUN_IDLE)]),
    "gun_shoot_down": dict(frames=14, loop=False, hit=4, keys=[  # finishing shot at a downed enemy
        (0, GUN_IDLE),
        (4, pose(GUN_IDLE, spine=166, chest=156, head=150, upperarm_R=52, forearm_R=44, hand_R=38,
                 upperarm_L=48, forearm_L=50, hand_L=40, thigh_L=40, thigh_R=-30)),
        (6, pose(GUN_IDLE, spine=166, chest=158, head=152, upperarm_R=58, forearm_R=56, hand_R=58,
                 upperarm_L=50, forearm_L=58, hand_L=52, thigh_L=40, thigh_R=-30)),
        (14, GUN_IDLE)]),
    "gun_whip": dict(frames=12, loop=False, hit=4, keys=[      # pistol-whip
        (0, GUN_IDLE),
        (2, pose(GUN_IDLE, chest=186, upperarm_R=150, forearm_R=200, hand_R=205, upperarm_L=60, forearm_L=130)),
        (4, pose(GUN_IDLE, spine=166, chest=154, head=164, upperarm_R=102, forearm_R=70, hand_R=38,
                 upperarm_L=40, forearm_L=100, thigh_L=42, thigh_R=-34, shin_R=-14)),
        (7, pose(GUN_IDLE, spine=168, chest=156, upperarm_R=70, forearm_R=30, hand_R=10,
                 thigh_L=42, thigh_R=-34, shin_R=-14)),
        (12, GUN_IDLE)]),
    "gun_throw": dict(frames=20, loop=False, hit=9, keys=[     # grapple and throw
        (0, GUN_IDLE),
        (3, pose(GUN_IDLE, spine=168, chest=162, upperarm_L=92, forearm_L=96, hand_L=96,
                 upperarm_R=86, forearm_R=92, hand_R=95, thigh_L=42, thigh_R=-26)),
        (6, pose(GUN_IDLE, spine=160, chest=150, head=160, upperarm_L=62, forearm_L=140, hand_L=140,
                 upperarm_R=58, forearm_R=135, hand_R=120, thigh_L=50, shin_L=-10, thigh_R=-40, shin_R=-30, dz=-0.2)),
        (9, pose(GUN_IDLE, spine=140, chest=128, head=136, upperarm_L=-40, forearm_L=-25, hand_L=-25,
                 upperarm_R=-30, forearm_R=0, hand_R=10, thigh_L=62, shin_L=0, thigh_R=-44, shin_R=-14, dz=-0.18)),
        (14, pose(GUN_IDLE, spine=160, chest=150, upperarm_R=40, forearm_R=80, hand_R=70)),
        (20, GUN_IDLE)]),
    "gun_reload": dict(frames=20, loop=False, hit=None, keys=[
        (0, GUN_IDLE),
        (4, pose(GUN_IDLE, upperarm_R=30, forearm_R=150, hand_R=165, upperarm_L=5, forearm_L=40, hand_L=40)),
        (9, pose(GUN_IDLE, upperarm_R=30, forearm_R=150, hand_R=165, upperarm_L=40, forearm_L=135, hand_L=150)),
        (12, pose(GUN_IDLE, upperarm_R=34, forearm_R=140, hand_R=150, upperarm_L=46, forearm_L=128, hand_L=120)),
        (20, GUN_IDLE)]),

    # ---- acrobatics and life between blows
    "front_flip": dict(frames=18, loop=False, hit=None, keys=[  # vault over an attacker
        (0, STANCE),
        (3, pose(spine=166, chest=160, thigh_L=62, shin_L=-12, thigh_R=48, shin_R=-36,
                 upperarm_L=-40, upperarm_R=-50, forearm_L=-10, forearm_R=-20, dz=-0.22)),
        (6, rotated(pose(STRAIGHT, thigh_L=110, shin_L=-20, thigh_R=100, shin_R=-30,
                         upperarm_L=100, forearm_L=150, upperarm_R=90, forearm_R=140), -110, dz=1.15)),
        (9, rotated(pose(STRAIGHT, thigh_L=120, shin_L=-30, thigh_R=115, shin_R=-35,
                         upperarm_L=60, forearm_L=140, upperarm_R=50, forearm_R=130), -220, dz=1.45)),
        (12, rotated(pose(STRAIGHT, thigh_L=80, shin_L=-10, thigh_R=60, shin_R=-20,
                          upperarm_L=140, forearm_L=160, upperarm_R=130, forearm_R=150), -320, dz=0.75)),
        (15, pose(spine=168, chest=162, thigh_L=58, shin_L=-8, thigh_R=-10, shin_R=-50,
                  upperarm_L=120, forearm_L=150, upperarm_R=-30, forearm_R=10, dz=-0.18)),
        (18, STANCE)]),
    "throw_weapon": dict(frames=14, loop=False, hit=5, keys=[
        (0, STANCE),
        (3, pose(spine=184, chest=194, head=186, upperarm_R=200, forearm_R=228, hand_R=240,
                 upperarm_L=80, forearm_L=110, thigh_L=30, thigh_R=-30)),
        (5, pose(spine=164, chest=150, head=160, upperarm_R=82, forearm_R=70, hand_R=60,
                 upperarm_L=-30, forearm_L=0, thigh_L=52, shin_L=0, thigh_R=-46, shin_R=-14)),
        (9, pose(spine=166, chest=154, upperarm_R=30, forearm_R=20, hand_R=10,
                 upperarm_L=-30, forearm_L=0, thigh_L=52, shin_L=0, thigh_R=-46, shin_R=-14)),
        (14, STANCE)]),
    "getup": dict(frames=20, loop=False, hit=None, keys=[        # from flat on the back to standing
        (0, rotated(pose(STRAIGHT, upperarm_L=165, forearm_L=170, upperarm_R=150, forearm_R=155,
                         thigh_L=20, shin_L=-15, thigh_R=5, shin_R=-5), 90, dz=-0.85)),
        (6, rotated(pose(STRAIGHT, spine=180, chest=180, upperarm_L=-30, forearm_L=-20, upperarm_R=-20,
                         forearm_R=-10, thigh_L=100, shin_L=-20, thigh_R=60, shin_R=-60), 45, dz=-0.55)),
        (12, pose(spine=150, chest=140, head=150, thigh_L=95, shin_L=0, thigh_R=-30, shin_R=-100,
                  upperarm_L=60, forearm_L=40, upperarm_R=30, forearm_R=20, dz=-0.42)),
        (16, pose(spine=166, chest=160, thigh_L=50, shin_L=-5, thigh_R=-20, shin_R=-40, dz=-0.15)),
        (20, STANCE)]),
    "taunt": dict(frames=32, loop=False, hit=None, keys=[        # beckoning: "come on"
        (0, STANCE),
        (6, pose(chest=176, head=182, upperarm_L=80, forearm_L=110, hand_L=110)),
        (11, pose(chest=176, head=182, upperarm_L=80, forearm_L=165, hand_L=175)),
        (16, pose(chest=176, head=182, upperarm_L=80, forearm_L=110, hand_L=110)),
        (21, pose(chest=176, head=182, upperarm_L=80, forearm_L=165, hand_L=175)),
        (26, pose(chest=176, head=182, upperarm_L=80, forearm_L=110, hand_L=110)),
        (32, STANCE)]),
    "gun_aim": dict(frames=10, loop=False, hit=None, keys=[      # a gunner holding aim
        (0, GUN_IDLE),
        (5, pose(GUN_IDLE, upperarm_R=88, forearm_R=90, hand_R=90, upperarm_L=80, forearm_L=92, hand_L=90)),
        (10, pose(GUN_IDLE, upperarm_R=88, forearm_R=90, hand_R=90, upperarm_L=80, forearm_L=92, hand_L=90))]),

    # ---- ledge work: hang by the hands below an edge, then pull up onto it.
    # Code places the root: "hang" at (edge, ledge height - 2.03); "climb_up" on the floor
    # just inside the edge, the clip starting from the hanging offset (dx back, dz down).
    "hang": dict(frames=24, loop=True, hit=None, keys=[
        (0, HANG),
        (12, pose(HANG, thigh_L=-6, shin_L=-10, thigh_R=10, shin_R=0, dz=0.0)),
        (24, HANG)]),
    "climb_up": dict(frames=16, loop=False, hit=None, keys=[
        (0, pose(HANG, dz=-HANG_DROP / S, dx=-0.6)),
        (5, pose(STRAIGHT, spine=168, chest=160, head=168, upperarm_L=-20, forearm_L=150, upperarm_R=-25, forearm_R=145,
                 thigh_L=5, shin_L=-5, thigh_R=-5, shin_R=-10, dz=-HANG_DROP * 0.62 / S, dx=-0.45)),
        (9, pose(STRAIGHT, spine=150, chest=145, head=155, upperarm_L=-30, forearm_L=40, upperarm_R=-30, forearm_R=40,
                 thigh_L=115, shin_L=-30, thigh_R=-10, shin_R=-30, dz=-HANG_DROP * 0.3 / S, dx=-0.25)),
        (13, pose(spine=165, chest=160, thigh_L=70, shin_L=-10, thigh_R=-10, shin_R=-60, dz=-0.28, dx=-0.05)),
        (16, STANCE)]),
    # ---- finishing and handling bodies
    "hook": dict(frames=12, loop=False, hit=4, keys=[           # lead-hand hook: hips and shoulders turn through
        (0, STANCE),
        (2, pose(spine=180, chest=188, head=180, upperarm_L=38, forearm_L=138, upperarm_R=30, forearm_R=165,
                 thigh_L=30, shin_L=0, thigh_R=-22, shin_R=-10)),
        (4, pose(spine=166, chest=152, head=166, upperarm_L=96, forearm_L=96, upperarm_R=22, forearm_R=160,
                 thigh_L=42, shin_L=0, thigh_R=-32, shin_R=-12)),
        (7, pose(spine=167, chest=154, head=167, upperarm_L=86, forearm_L=116, upperarm_R=22, forearm_R=160,
                 thigh_L=42, shin_L=0, thigh_R=-32, shin_R=-12)),
        (12, STANCE)]),
    "uppercut": dict(frames=15, loop=False, hit=5, keys=[       # rear-hand uppercut: dip, then drive up off the legs
        (0, STANCE),
        (2, pose(spine=168, chest=160, head=170, upperarm_R=8, forearm_R=42, upperarm_L=60, forearm_L=150,
                 thigh_L=46, shin_L=-34, thigh_R=-6, shin_R=-48, dz=-0.12)),
        (5, pose(spine=182, chest=188, head=186, upperarm_R=118, forearm_R=158, upperarm_L=40, forearm_L=140,
                 thigh_L=20, shin_L=0, thigh_R=-14, shin_R=-6, dz=0.07)),
        (9, pose(spine=181, chest=186, head=184, upperarm_R=124, forearm_R=164, upperarm_L=40, forearm_L=140,
                 thigh_L=20, shin_L=0, thigh_R=-14, shin_R=-6, dz=0.04)),
        (15, STANCE)]),
    "stomp": dict(frames=14, loop=False, hit=7, keys=[
        (0, STANCE),
        (4, pose(spine=184, chest=188, thigh_R=112, shin_R=-15, upperarm_L=120, forearm_L=150, upperarm_R=100, forearm_R=140)),
        (7, pose(spine=160, chest=152, head=150, thigh_R=40, shin_R=25, thigh_L=-10, shin_L=-5,
                 upperarm_L=-30, forearm_L=10, upperarm_R=-40, forearm_R=0, dz=-0.08)),
        (10, pose(spine=160, chest=152, head=150, thigh_R=40, shin_R=25, thigh_L=-10, shin_L=-5,
                  upperarm_L=-30, forearm_L=10, upperarm_R=-40, forearm_R=0, dz=-0.08)),
        (14, STANCE)]),
    "swing_hold": dict(frames=12, loop=True, hit=None, keys=[   # giant swing: leaning back, holding the legs
        (0, pose(spine=196, chest=202, head=196, upperarm_L=78, forearm_L=82, upperarm_R=72, forearm_R=78,
                 thigh_L=22, shin_L=-8, thigh_R=-26, shin_R=-16)),
        (6, pose(spine=198, chest=205, head=200, upperarm_L=82, forearm_L=86, upperarm_R=76, forearm_R=82,
                 thigh_L=-20, shin_L=-14, thigh_R=24, shin_R=-6)),
        (12, pose(spine=196, chest=202, head=196, upperarm_L=78, forearm_L=82, upperarm_R=72, forearm_R=78,
                  thigh_L=22, shin_L=-8, thigh_R=-26, shin_R=-16))]),
    "duck": dict(frames=10, loop=False, hit=None, keys=[        # drop under a knife or a shot
        (0, STANCE),
        (3, pose(spine=150, chest=138, head=140, thigh_L=88, shin_L=-18, thigh_R=42, shin_R=-100,
                 upperarm_L=60, forearm_L=140, upperarm_R=40, forearm_R=150, dz=-0.58)),
        (10, pose(spine=150, chest=138, head=140, thigh_L=88, shin_L=-18, thigh_R=42, shin_R=-100,
                  upperarm_L=60, forearm_L=140, upperarm_R=40, forearm_R=150, dz=-0.58))]),
    "slide_kick": dict(frames=16, loop=False, hit=5, keys=[     # slide in low, lead leg first
        (0, STANCE),
        (3, pose(spine=200, chest=208, head=200, thigh_L=86, shin_L=86, thigh_R=22, shin_R=-62,
                 upperarm_L=-45, forearm_L=-20, upperarm_R=-55, forearm_R=-30, dz=-0.62)),
        (9, pose(spine=200, chest=208, head=200, thigh_L=86, shin_L=86, thigh_R=22, shin_R=-62,
                 upperarm_L=-45, forearm_L=-20, upperarm_R=-55, forearm_R=-30, dz=-0.62)),
        (13, pose(spine=168, chest=160, thigh_L=60, shin_L=-10, thigh_R=-10, shin_R=-60, dz=-0.2)),
        (16, STANCE)]),
    # ---- grappling (enemy grabs the hero; the hero struggles free)
    "grab_attack": dict(frames=12, loop=False, hit=6, keys=[
        (0, STANCE),
        (3, pose(spine=172, chest=168, upperarm_L=20, forearm_L=60, upperarm_R=10, forearm_R=50, thigh_L=20)),
        (6, pose(spine=160, chest=150, head=160, upperarm_L=92, forearm_L=96, upperarm_R=88, forearm_R=94,
                 thigh_L=48, shin_L=0, thigh_R=-40, shin_R=-12)),
        (12, pose(spine=168, chest=164, upperarm_L=80, forearm_L=110, upperarm_R=76, forearm_R=108))]),
    "hold_victim": dict(frames=16, loop=True, hit=None, keys=[
        (0, pose(spine=184, chest=188, upperarm_L=86, forearm_L=110, upperarm_R=80, forearm_R=104, thigh_L=30, thigh_R=-30)),
        (8, pose(spine=186, chest=192, upperarm_L=84, forearm_L=118, upperarm_R=78, forearm_R=112, thigh_L=30, thigh_R=-30)),
        (16, pose(spine=184, chest=188, upperarm_L=86, forearm_L=110, upperarm_R=80, forearm_R=104, thigh_L=30, thigh_R=-30))]),
    "struggle": dict(frames=16, loop=True, hit=None, keys=[
        (0, pose(spine=166, chest=160, head=150, upperarm_L=120, forearm_L=170, upperarm_R=-30, forearm_R=20,
                 thigh_L=30, thigh_R=-30)),
        (8, pose(spine=170, chest=166, head=158, upperarm_L=-20, forearm_L=30, upperarm_R=110, forearm_R=160,
                 thigh_L=10, thigh_R=-10)),
        (16, pose(spine=166, chest=160, head=150, upperarm_L=120, forearm_L=170, upperarm_R=-30, forearm_R=20,
                  thigh_L=30, thigh_R=-30))]),

    # ---- free movement
    "jump": dict(frames=20, loop=False, hit=None, keys=[         # crouch, spring up arms high, tuck, open for landing
        (0, STANCE),
        (3, pose(spine=168, chest=162, thigh_L=62, shin_L=-10, thigh_R=50, shin_R=-30,
                 upperarm_L=-40, forearm_L=-20, upperarm_R=-45, forearm_R=-25, dz=-0.2)),
        (6, pose(STRAIGHT, spine=178, chest=182, upperarm_L=165, forearm_L=172, upperarm_R=160, forearm_R=168,
                 thigh_L=10, shin_L=-20, thigh_R=-5, shin_R=-30, dz=0.0)),
        (10, pose(spine=176, chest=178, thigh_L=100, shin_L=-30, thigh_R=95, shin_R=-40,
                  upperarm_L=120, forearm_L=160, upperarm_R=110, forearm_R=150, dz=0.0)),
        (16, pose(spine=176, chest=176, thigh_L=40, shin_L=0, thigh_R=20, shin_R=-20,
                  upperarm_L=80, forearm_L=110, upperarm_R=70, forearm_R=100, dz=0.0)),
        (20, pose(spine=176, chest=176, thigh_L=40, shin_L=0, thigh_R=20, shin_R=-20,
                  upperarm_L=80, forearm_L=110, upperarm_R=70, forearm_R=100, dz=0.0))]),
    "pike": dict(frames=12, loop=True, hit=None, keys=[         # hanging with straight legs kicked forward (bar circles)
        (0, pose(HANG, thigh_L=40, shin_L=40, thigh_R=34, shin_R=34, dz=0.0)),
        (6, pose(HANG, thigh_L=30, shin_L=30, thigh_R=24, shin_R=24, dz=0.0)),
        (12, pose(HANG, thigh_L=40, shin_L=40, thigh_R=34, shin_R=34, dz=0.0))]),
}
CLIPS.update(WEAPON_CLIPS)

# ---------------------------------------------------------------- story mode (the warehouse awakening)
# Angles are absolute, from straight down, positive towards the way he faces.
LOOSE = pose(STRAIGHT, spine=178, chest=176, head=176, upperarm_L=8, forearm_L=14, upperarm_R=-6, forearm_R=4,
             thigh_L=6, shin_L=2, thigh_R=-5, shin_R=-3)
LIE = rotated(pose(STRAIGHT, head=184, upperarm_L=8, forearm_L=12, upperarm_R=-6, forearm_R=-2,
                   thigh_L=10, shin_L=-6, thigh_R=-2, shin_R=-10), 90, dz=-0.85)
ALLFOURS = pose(STRAIGHT, spine=84, chest=80, head=58, upperarm_L=-4, forearm_L=-6, upperarm_R=6, forearm_R=4,
                thigh_L=2, shin_L=-88, thigh_R=-8, shin_R=-92)
KNEEL_PUSH = pose(STRAIGHT, spine=150, chest=142, head=150, upperarm_L=38, forearm_L=20, upperarm_R=10, forearm_R=0,
                  thigh_L=84, shin_L=0, thigh_R=0, shin_R=-90)
KNEEL = pose(STRAIGHT, spine=176, chest=172, head=168, upperarm_L=40, forearm_L=80, upperarm_R=6, forearm_R=10,
             thigh_L=84, shin_L=0, thigh_R=0, shin_R=-90)
HANDS_UP = pose(LOOSE, chest=172, head=150, upperarm_L=70, forearm_L=150, upperarm_R=64, forearm_R=142)
HANDS_TURN = pose(LOOSE, chest=172, head=148, upperarm_L=70, forearm_L=128, upperarm_R=64, forearm_R=120)
FISTS = pose(LOOSE, chest=170, head=150, upperarm_L=74, forearm_L=158, upperarm_R=68, forearm_R=152)
TOUCH = pose(LOOSE, chest=178, head=188, upperarm_R=120, forearm_R=215, hand_R=215)
FLINCH = pose(LOOSE, spine=181, chest=185, head=198, upperarm_R=126, forearm_R=222, hand_R=222,
              upperarm_L=16, forearm_L=30)
WALK_A = pose(LOOSE, upperarm_L=-18, forearm_L=-8, upperarm_R=20, forearm_R=38,
              thigh_L=22, shin_L=8, thigh_R=-16, shin_R=-26)
WALK_P = pose(LOOSE, upperarm_L=0, forearm_L=8, upperarm_R=2, forearm_R=14,
              thigh_L=-2, shin_L=-4, thigh_R=18, shin_R=-38)
STORY_CLIPS = {
    "lie": dict(frames=48, loop=True, hit=None, keys=[            # on his back, barely breathing
        (0, LIE), (24, rotated(pose(STRAIGHT, head=186, chest=183, upperarm_L=8, forearm_L=12, upperarm_R=-6,
                                    forearm_R=-2, thigh_L=10, shin_L=-6, thigh_R=-2, shin_R=-10), 90, dz=-0.85)),
        (48, LIE)]),
    "allfours": dict(frames=36, loop=True, hit=None, keys=[        # on hands and knees over the puddle, trembling
        (0, ALLFOURS), (18, pose(ALLFOURS, spine=82, chest=77, head=54)), (36, ALLFOURS)]),
    "rise": dict(frames=54, loop=False, hit=None, keys=[            # all fours -> one knee -> standing
        (0, ALLFOURS), (14, KNEEL_PUSH), (26, KNEEL), (40, KNEEL), (54, LOOSE)]),
    "idle_loose": dict(frames=48, loop=True, hit=None, keys=[       # standing, not in a guard: he does not know he fights
        (0, LOOSE), (24, pose(LOOSE, chest=174, head=174, dz=-0.01)), (48, LOOSE)]),
    "look_hands": dict(frames=48, loop=False, hit=None, keys=[      # hands up, palms turned, then fists
        (0, LOOSE), (12, HANDS_UP), (22, HANDS_TURN), (31, FISTS),
        (34, pose(FISTS, forearm_L=154, forearm_R=149)), (37, FISTS), (48, FISTS)]),
    "touch_temple": dict(frames=36, loop=False, hit=None, keys=[    # fingers to the scar, a flinch
        (0, LOOSE), (10, TOUCH), (16, FLINCH), (24, TOUCH), (36, TOUCH)]),
    "walk": dict(frames=24, loop=True, hit=None, keys=[
        (0, WALK_A), (6, WALK_P), (12, mirror_legs_arms(WALK_A)), (18, mirror_legs_arms(WALK_P)), (24, WALK_A)]),
}
# --- the escape (chapter 1): he runs, trips over a chain, falls on his face in a puddle, crawls
# to his knife, grips it, and gets up with it. Face down = the whole body rotated by -90.
TRIP_CATCH = pose(STRAIGHT, spine=150, chest=140, head=150, upperarm_L=125, forearm_L=160, upperarm_R=70, forearm_R=95,
                  thigh_L=40, shin_L=10, thigh_R=-35, shin_R=-70)
TRIP_FALL = pose(STRAIGHT, hips=120, spine=108, chest=102, head=110, upperarm_L=98, forearm_L=92, upperarm_R=92, forearm_R=84,
                 thigh_L=-30, shin_L=-55, thigh_R=-50, shin_R=-80, dz=-0.42)
PRONE = pose(STRAIGHT, hips=90, spine=90, chest=91, head=100, upperarm_L=12, forearm_L=92, upperarm_R=6, forearm_R=96,
             thigh_L=-88, shin_L=-92, thigh_R=-92, shin_R=-96, dz=-0.82)
PRONE_FLAT = pose(PRONE, head=86, dz=-0.84)                       # face in the water
CRAWL_A = pose(PRONE, chest=100, head=132, upperarm_R=84, forearm_R=86, upperarm_L=22, forearm_L=88,
               thigh_L=-92, shin_L=-90, thigh_R=-62, shin_R=-118, dz=-0.8)
CRAWL_B = mirror_legs_arms(CRAWL_A)
CRAWL_PULL = pose(PRONE, chest=102, head=130, upperarm_R=40, forearm_R=92, upperarm_L=40, forearm_L=92,
                  thigh_L=-75, shin_L=-108, thigh_R=-75, shin_R=-108, dz=-0.79)
REACH = pose(PRONE, chest=102, head=135, upperarm_R=84, forearm_R=86, hand_R=84, upperarm_L=24, forearm_L=90, dz=-0.81)
GRIP = pose(REACH, hand_R=60)
PRONE_KNIFE = pose(PRONE, chest=102, head=128, upperarm_R=30, forearm_R=104, hand_R=70, upperarm_L=20, forearm_L=88, dz=-0.79)
PUSH_UP = pose(STRAIGHT, hips=140, spine=128, chest=122, head=150, upperarm_L=4, forearm_L=-2, upperarm_R=50, forearm_R=115,
               thigh_L=40, shin_L=-70, thigh_R=-60, shin_R=-95, dz=-0.48)
KNEEL_KNIFE = pose(STRAIGHT, spine=170, chest=164, head=170, upperarm_L=45, forearm_L=120, upperarm_R=62, forearm_R=112,
                   thigh_L=84, shin_L=0, thigh_R=0, shin_R=-90)
KNIFE_STANCE = pose(STANCE, upperarm_R=48, forearm_R=118, hand_R=100)
ESCAPE_CLIPS = {
    "trip": dict(frames=22, loop=False, hit=None, keys=[            # the chain takes his foot; down he goes
        (0, RUN_A), (3, TRIP_CATCH), (8, TRIP_FALL), (12, PRONE), (16, pose(PRONE, head=92, dz=-0.85)), (22, PRONE_FLAT)]),
    "prone": dict(frames=48, loop=True, hit=None, keys=[            # face down in the water, breathing
        (0, PRONE_FLAT), (24, pose(PRONE_FLAT, head=92, chest=93)), (48, PRONE_FLAT)]),
    "crawl": dict(frames=32, loop=True, hit=None, keys=[            # dragging himself on his forearms
        (0, CRAWL_A), (8, CRAWL_PULL), (16, CRAWL_B), (24, CRAWL_PULL), (32, CRAWL_A)]),
    "reach_grab": dict(frames=26, loop=False, hit=None, keys=[      # the arm out, the fingers close on the handle
        (0, CRAWL_PULL), (9, REACH), (14, GRIP), (26, PRONE_KNIFE)]),
    "prone_knife": dict(frames=40, loop=True, hit=None, keys=[      # holding it, shaking
        (0, PRONE_KNIFE), (20, pose(PRONE_KNIFE, hand_R=64, head=116)), (40, PRONE_KNIFE)]),
    "rise_knife": dict(frames=44, loop=False, hit=None, keys=[      # up off the floor, the knife coming up with him
        (0, PRONE_KNIFE), (12, PUSH_UP), (24, KNEEL_KNIFE), (32, KNEEL_KNIFE), (44, KNIFE_STANCE)]),
}
STORY_CLIPS.update(ESCAPE_CLIPS)
CLIPS.update(STORY_CLIPS)


# ---------------------------------------------------------------- build
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.unit_settings.scale_length = 1.0


def build_armature():
    data = bpy.data.armatures.new("StickmanRig")
    arm = bpy.data.objects.new("Stickman", data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (parent, head, tail, y, _rest, _r) in BONES.items():
        b = data.edit_bones.new(name)
        b.head = Vector(head) + Vector((0, y, 0))
        b.tail = Vector(tail) + Vector((0, y, 0))
        b.align_roll(Vector((0, 1, 0)))  # local Z = world +Y: all bending is about one axis
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def build_mesh(arm):
    mat = bpy.data.materials.new("Ink")
    mat.diffuse_color = (0.02, 0.02, 0.02, 1)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.02, 0.02, 0.02, 1)
        bsdf.inputs["Roughness"].default_value = 0.6

    parts = []

    def add_part(obj, bone):
        obj.data.materials.append(mat)
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        parts.append(obj)

    for name, (_p, head, tail, y, _rest, radius) in BONES.items():
        h = Vector(head) + Vector((0, y, 0))
        t = Vector(tail) + Vector((0, y, 0))
        if name == "head":
            bpy.ops.mesh.primitive_uv_sphere_add(radius=HEAD_RADIUS, segments=24, ring_count=14,
                                                 location=(0, 0, HEAD_CENTER))
            add_part(bpy.context.object, name)
            continue
        if name == "spine":   # a round belly
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.165, segments=18, ring_count=10, location=(0, 0, 0.81))
            bpy.context.object.scale = (1.0, 1.05, 1.12)
            add_part(bpy.context.object, name)
        if name == "chest":   # a round chest under the head
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.158, segments=18, ring_count=10, location=(0, 0, 0.935))
            add_part(bpy.context.object, name)
        if name in ("shin.L", "shin.R"):  # little round feet, toes forward
            bpy.ops.mesh.primitive_uv_sphere_add(radius=0.075, segments=12, ring_count=8, location=t + Vector((0.035, 0, -0.005)))
            bpy.context.object.scale = (1.35, 1.0, 0.8)
            add_part(bpy.context.object, name)
        if radius <= 0:
            continue
        length = (t - h).length
        bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length, vertices=10,
                                            location=(h + t) / 2)
        add_part(bpy.context.object, name)
        # ball at the far joint so limbs read as one continuous ink line
        bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=t)
        add_part(bpy.context.object, name)
        if name in ("upperarm.L", "upperarm.R", "thigh.L", "thigh.R"):
            bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=10, ring_count=6, location=h)
            add_part(bpy.context.object, name)

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


def rest_angle(name):
    return BONES[name][4]


def ground_dz(p):
    """Lift or drop the hips so the lowest foot touches the floor."""
    ext = []
    for side in ("L", "R"):
        th, sh = math.radians(p["thigh." + side]), math.radians(p["shin." + side])
        ext.append(THIGH * math.cos(th) + SHIN * math.cos(sh))
    return max(ext) + FOOT_Z - HIP_Z


def apply_pose(arm, p):
    p = dict(p)
    for side in ("L", "R"):  # a hand not posed explicitly continues its forearm
        p.setdefault("hand." + side, p["forearm." + side])
    for name in BONES:
        pb = arm.pose.bones[name]
        parent = BONES[name][0]
        delta = p[name] - rest_angle(name)
        if parent:
            delta -= p[parent] - rest_angle(parent)
        rest_rot = pb.bone.matrix_local.to_3x3()
        axis = (rest_rot.inverted() @ Vector((0, 1, 0))).normalized()
        pb.rotation_mode = "QUATERNION"
        # rotating about world +Y moves a bone towards -X, so negate to make + mean "forward"
        pb.rotation_quaternion = Quaternion(axis, -math.radians(delta))
        if name == "hips":
            dz = p.get("_dz")
            dz = ground_dz(p) if dz is None else dz
            explicit = p.get("_dz") is not None
            pb.location = rest_rot.inverted() @ Vector((p.get("_dx", 0.0) * S, 0, dz * S if explicit else dz))


def build_actions(arm, skip=()):
    arm.animation_data_create()
    meta = []
    for clip, spec in CLIPS.items():
        if clip in skip:
            continue
        act = bpy.data.actions.new(clip)
        act.use_fake_user = True
        arm.animation_data.action = act
        for frame, p in spec["keys"]:
            apply_pose(arm, p)
            for name in BONES:
                pb = arm.pose.bones[name]
                pb.keyframe_insert("rotation_quaternion", frame=frame, group=name)
                if name == "hips":
                    pb.keyframe_insert("location", frame=frame, group=name)
        act.frame_range = (0, spec["frames"])
        meta.append(dict(name=clip, frames=spec["frames"], fps=FPS, loop=spec["loop"],
                         hitFrame=spec["hit"] if spec["hit"] is not None else -1))
    return meta


def export(arm, meta):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    path = os.path.join(OUT_DIR, "Stickman.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=False, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, mesh_smooth_type="FACE")
    with open(os.path.join(OUT_DIR, "stickman_clips.json"), "w") as f:
        json.dump(dict(clips=meta), f, indent=2)
    print("exported", path)


def add_prop(arm, kind):
    """A sword or pistol in the right hand, pointing where the hand bone points
    (the weapon direction the poses set), as in the game."""
    bpy.ops.mesh.primitive_cube_add(size=2)
    prop = bpy.context.object
    prop.name = kind
    prop.parent = arm
    prop.parent_type = "BONE"
    prop.parent_bone = "hand.R"
    prop.matrix_parent_inverse.identity()
    # bone-parented children sit at the bone's tail, Y along the bone: the weapon points
    # where the hand bone points, starting at the fist (HAND back from the tail)
    if kind == "sword":
        prop.location = (0.0, 0.5 - 0.09, 0.0)
        prop.scale = (0.02, 0.5, 0.04)
    else:
        prop.location = (0.0, 0.05 - 0.09, 0.0)
        prop.scale = (0.03, 0.1, 0.06)
    mat = bpy.data.materials.new("PropInk")
    mat.diffuse_color = (0.02, 0.02, 0.02, 1)
    bsdf = mat.node_tree.nodes.get("Principled BSDF") if mat.node_tree else None
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.02, 0.02, 0.02, 1)
    prop.data.materials.append(mat)
    return prop


def render_previews(arm):
    from PIL import Image
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 4
    scene.cycles.device = "CPU"
    scene.render.resolution_x = 220
    scene.render.resolution_y = 220
    world = bpy.data.worlds.new("Paper")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.62, 0.62, 1)
    scene.world = world
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.8
    cam = bpy.data.objects.new("Cam", cam_data)
    cam.location = (0, -6, 1.1)
    cam.rotation_euler = (math.radians(90), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    bpy.ops.mesh.primitive_plane_add(size=0.02, location=(0, 0, -5))  # keeps cycles happy
    tmp = os.path.join(PREVIEW_DIR, "_frame.png")
    props = {"sword": add_prop(arm, "sword"), "gun": add_prop(arm, "gun")}
    only = os.environ.get("PREVIEW_ONLY")          # e.g. PREVIEW_ONLY=sword_ to render just those sheets
    for clip, spec in CLIPS.items():
        if clip not in bpy.data.actions or "keys" not in spec:
            continue
        if only and not clip.startswith(only):
            continue
        for kind, obj in props.items():
            obj.hide_render = not clip.startswith(kind)
        if bpy.data.actions[clip].get("mocap"):
            continue
        arm.animation_data.action = bpy.data.actions[clip]
        frames = [k for k, _ in spec["keys"]]
        tiles = []
        for fr in frames:
            scene.frame_set(fr)
            scene.render.filepath = tmp
            bpy.ops.render.render(write_still=True)
            tiles.append(Image.open(tmp).convert("RGB").copy())
        sheet = Image.new("RGB", (220 * len(tiles), 220), "white")
        for i, t in enumerate(tiles):
            sheet.paste(t, (220 * i, 0))
        sheet.save(os.path.join(PREVIEW_DIR, clip + ".png"))
    os.remove(tmp)


def build_mocap_actions(arm):
    """Real motion: CMU motion capture retargeted onto the rig (see retarget.py)."""
    import retarget
    from mocap_clips import MOCAP_CLIPS
    cache = os.path.join(ROOT, "tools", "mocap", "cache")
    src = os.path.join(ROOT, "tools", "mocap", "bvh")  # only needed if a cache file is missing
    meta = []
    for name, spec in MOCAP_CLIPS.items():
        frames = retarget.prepare(spec, cache, src)
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        act["mocap"] = True
        arm.animation_data.action = act
        last = retarget.key_action(arm, frames, arm.pose.bones, FOOT_Z, BONES["hips"][1], (THIGH + SHIN) / 0.89)
        act.frame_range = (0, last)
        hit = -1
        if spec.get("hit") is not None:
            hit = int(round((spec["hit"] - spec["start"]) / spec.get("speed", 1.0)))
            hit = retarget.impact_frame(frames, spec, hit)
        meta.append(dict(name=name, frames=last, fps=FPS, loop=bool(spec.get("loop")), hitFrame=hit,
                         source="CMU " + spec["file"]))
    return meta


def main():
    reset_scene()
    arm = build_armature()
    build_mesh(arm)
    sys.path.insert(0, os.path.dirname(__file__))
    use_mocap = "--no-mocap" not in sys.argv
    mocap_names = ()
    if use_mocap:
        from mocap_clips import MOCAP_CLIPS
        mocap_names = tuple(MOCAP_CLIPS)
    meta = build_actions(arm, skip=mocap_names)
    if use_mocap:
        meta += build_mocap_actions(arm)
    arm.animation_data.action = None
    # the default pose the FBX carries is the rig's current one: make it the rest pose,
    # not whatever clip happened to be keyed last (eyes, mounts are measured on it)
    scn = bpy.context.scene
    scn.frame_set(0)
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    export(arm, meta)
    if "--preview" in sys.argv:
        render_previews(arm)


if __name__ == "__main__":
    main()
