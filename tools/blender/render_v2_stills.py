"""Story v2 stills (docs/story/story_v2.md).

  vphoto   chapter 3: the old photo behind the family photo. Him at twenty in a boxing gym, Voss
           beside him (a long coat, his hair still dark, a hand on his shoulder, proud), and a sullen
           boy half hidden behind Voss: Silas. Soft and yellowed (an old print).

    /home/user/.venv-blender/bin/python tools/blender/render_v2_stills.py [names...]

Writes Assets/Resources/Story/Memories/<name>.png (960x540)."""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_memories as RM  # noqa: E402
import render_flashbacks as F  # noqa: E402
import render_maya as MY  # noqa: E402
import render_finale as RF  # noqa: E402
import build_stickman as B  # noqa: E402
import bpy  # noqa: E402


def young_voss(pose, at, facing=1, hair=0.12):
    arm = F.figure(pose, at=at, facing=facing, ink=0.09)
    RF.wear(arm, "head", (0, -0.06, 0.0), (0.27, 0.12, 0.27), hair)
    RF.wear(arm, "chest", (0, -0.02, 0.0), (0.36, 0.1, 0.27), 0.05)
    RF.wear(arm, "hips", (0, -0.34, 0.0), (0.34, 0.66, 0.26), 0.05)
    bpy.context.view_layer.update()
    return arm


def vphoto():
    F.floor(0.5, -3, 3, -4, 4, trim=False)
    F.wall(1.9, -6, 6, 4, g=0.55)
    F.box((-1.6, 1.85, 1.9), (1.0, 0.04, 0.9), F.halo(1.0, 0.8, "GymWindow"))
    # the gym: a heavy bag on its chain, a rope, a bench, gloves hanging
    F.box((1.7, 1.2, 2.6), (0.04, 0.04, 0.9), F.flat(0.2))
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=0.95, location=(1.7, 1.2, 1.7))
    bpy.context.object.data.materials.append(F.flat(0.12))
    F.box((0.0, 1.6, 0.35), (2.2, 0.35, 0.08), F.flat(0.3))
    F.box((-2.2, 1.0, 0.2), (0.06, 1.6, 0.06), F.flat(0.7))
    import math
    stand = B.pose(B.STRAIGHT, upperarm_L=12, forearm_L=20, upperarm_R=12, forearm_R=20)
    h = RM.him(stand, (-0.3, 0.55, 0), facing=1)
    proud = B.pose(B.STRAIGHT, upperarm_L=8, forearm_L=10, upperarm_R=8, forearm_R=15, head=182)
    v = young_voss(proud, (0.3, 0.6, 0), facing=1)
    boy = B.pose(B.STRAIGHT, upperarm_L=-10, forearm_L=40, upperarm_R=-10, forearm_R=40, head=170)
    b = F.figure(boy, at=(0.72, 1.2, 0), facing=1, ink=0.15)
    b.scale = (0.72, 0.72, 0.72)
    for ob, yaw in ((h, -78), (v, -100), (b, -115)):          # posed for the photo: towards the lens, the boy turning away
        ob.rotation_euler = (0, 0, math.radians(yaw))
    bpy.context.view_layer.update()
    F.camera((0.05, -2.9, 1.15), (0.15, 0.7, 0.95), 40)


SHOTS = {"vphoto": vphoto}


def aged(path):
    """An old print: soft, a little yellowed and faded, a vignette."""
    RM.soften(path)
    from PIL import Image, ImageEnhance
    im = Image.open(path).convert("RGB")
    im = ImageEnhance.Contrast(im).enhance(0.8)
    r, g, bl = im.split()
    r = r.point(lambda v: min(255, int(v * 1.05 + 10)))
    bl = bl.point(lambda v: int(v * 0.86))
    Image.merge("RGB", (r, g, bl)).save(path)


if __name__ == "__main__":
    names = [a for a in sys.argv[1:] if a in SHOTS] or list(SHOTS)
    os.makedirs(MY.MEM, exist_ok=True)
    for n in names:
        F.reset()
        SHOTS[n]()
        path = os.path.join(MY.MEM, f"{n}.png")
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        aged(path)
        print("rendered", path)
