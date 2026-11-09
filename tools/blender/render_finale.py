"""The finale's stills (docs/story/chapter5_fortress.md, the last roof): what Voss tells him, and he
remembers. Broken, in the harsh look (render_maya.harsh):

  voss_1  "I'm out, Voss.": his gun laid on Voss's desk, the city through the window behind
  voss_2  Voss at the window, the phone at his ear: "Then finish it. All of them."
  voss_3  the warehouse: the blow from behind, the pipe coming down on his head

    /home/user/.venv-blender/bin/python tools/blender/render_finale.py

Writes Assets/Resources/Story/Flash/voss_<n>.png."""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_maya as MY  # noqa: E402
import render_flashbacks as F  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402

rnd = random.Random(83)


def wear(arm, bone, loc, size, g):
    """A piece of his look on a bone (bone parenting: loc is from the bone's tail, y along it)."""
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.object
    o.scale = size
    o.data.materials.append(F.flat(g))
    o.parent = arm
    o.parent_type = "BONE"
    o.parent_bone = bone
    o.location = loc
    return o


def voss(pose, at, facing=1):
    """VOSS (story_v2 §4): a long dark coat to the knees, silver hair, gloves."""
    arm = F.figure(pose, at=at, facing=facing, ink=0.09)
    wear(arm, "head", (0, -0.06, 0.0), (0.27, 0.12, 0.27), 0.78)          # the silver hair
    wear(arm, "chest", (0, -0.02, 0.0), (0.36, 0.1, 0.27), 0.04)           # the coat's shoulders
    wear(arm, "hips", (0, -0.34, 0.0), (0.34, 0.66, 0.26), 0.04)           # and its skirt, to the knees
    bpy.context.view_layer.update()
    return arm


def v1_out():
    F.floor(0.18, -3, 3, -4, 4, trim=False)
    F.wall(2.2, -5, 5, 4, g=0.16)
    F.box((0.0, 2.15, 1.7), (3.0, 0.05, 1.8), F.halo(1.0, 0.75, "CityWindow"))
    for _ in range(40):
        F.box((rnd.uniform(-1.4, 1.4), 2.12, rnd.uniform(0.9, 2.5)), (0.05, 0.02, 0.07), F.flat(1.0))
    F.box((0.2, 0.6, 0.75), (1.8, 0.8, 0.06), F.flat(0.1))                         # the desk
    F.box((0.15, 0.45, 0.8), (0.24, 0.08, 0.05), F.flat(0.03))                     # his gun on it
    sit = B.pose(B.STRAIGHT, thigh_L=90, shin_L=0, thigh_R=90, shin_R=0, upperarm_L=40, forearm_L=90, upperarm_R=40, forearm_R=90, dz=-0.35)
    voss(sit, (0.6, 1.2, 0), facing=-1)
    stand = B.pose(B.STRAIGHT, upperarm_R=60, forearm_R=70)
    F.figure(stand, at=(-0.55, 0.3, 0), facing=1, hero=True)
    F.camera((-0.6, -3.0, 1.05), (0.1, 0.8, 0.95), 50, roll=-4)


def v2_phone():
    F.wall(2.0, -5, 5, 4, g=0.12)
    F.box((0.3, 1.95, 1.6), (2.4, 0.05, 2.4), F.halo(1.0, 0.9, "CityWindow"))
    for _ in range(50):
        F.box((rnd.uniform(-0.8, 1.4), 1.92, rnd.uniform(0.6, 2.7)), (0.05, 0.02, 0.07), F.flat(1.0))
    phone = B.pose(B.STRAIGHT, upperarm_R=150, forearm_R=230, hand_R=230, head=176)
    voss(phone, (0.3, 1.2, 0), facing=1)
    F.camera((-0.6, -1.6, 1.15), (0.3, 1.2, 1.2), 46, roll=6)


def v3_blow():
    F.floor(0.16, -3, 3, -4, 4, trim=False)
    F.wall(2.2, -6, 6, 5, g=0.2)
    F.box((0.3, 2.15, 2.6), (1.6, 0.05, 1.0), F.halo(1.0, 0.9, "Skylight"))
    hit = B.pose(B.STRAIGHT, spine=160, chest=150, head=128, upperarm_L=-30, upperarm_R=-20, thigh_L=30, thigh_R=-10)
    F.figure(hit, at=(0.35, 0.5, 0), facing=1, hero=True)
    swing = B.pose(B.STRAIGHT, upperarm_R=150, forearm_R=120, hand_R=60, upperarm_L=60, forearm_L=90, chest=165)
    MY.thug(swing, (-0.45, 0.6, 0), facing=1)
    F.box((0.05, 0.55, 1.55), (0.7, 0.05, 0.05), F.flat(0.4), rot=(0, 0.7, 0))      # the pipe, coming down
    walk = B.pose(B.STRAIGHT, thigh_L=25, shin_L=10, thigh_R=-15, shin_R=20, upperarm_L=-15, upperarm_R=15)
    voss(walk, (1.6, 1.9, 0), facing=1)                                              # and him, walking away, not looking back
    F.camera((0.4, -2.8, 1.1), (0.0, 0.6, 1.05), 50, roll=-9)


SHOTS = [v1_out, v2_phone, v3_blow]

if __name__ == "__main__":
    os.makedirs(MY.FLASH, exist_ok=True)
    for i, shot in enumerate(SHOTS, 1):
        F.reset()
        shot()
        path = os.path.join(MY.FLASH, f"voss_{i}.png")
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        MY.harsh(path)
        print("rendered", path)
