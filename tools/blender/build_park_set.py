"""Chapter 4: THE BACKYARD (docs/story/chapter4_backyard.md). Night falling behind No. 14 Alder Lane:
the back of the house, the garden and Maya's play area, through the broken back gate into the
neighbourhood park (the playground, the pond and its footbridge) and out to the car park, where
the white van waits. On the horizon, Kessel Hill and the fortress on it, lit.

  G1 the garden 8-34 | G2 the playground 34-70 | G3 the pond 70-100 | G4 the car park 100-130

The tower's palette (build_story_set TONE); moonlight, park lamps, the kitchen window. The swing seat
and the van's doors are separate objects (they move).

    /home/user/.venv-blender/bin/python tools/blender/build_park_set.py [--preview]

Writes Assets/Art/Park/ParkSet.fbx and its bakes."""
import math
import os
import random
import sys

import bpy  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Park")
rnd = random.Random(53)
SWING_X, SWING_Y = 27.0, 2.8
POND_X0, POND_X1 = 76.0, 94.0


def mats():
    M = S.materials()
    mat = S.mat
    M["grass"] = mat("Grass", (0.1, 0.11, 0.1), 1.0)
    M["grass_dark"] = mat("GrassDark", (0.06, 0.065, 0.06), 1.0)
    M["path"] = mat("Path", (0.3, 0.3, 0.29), 0.9)
    M["brick"] = mat("Brick", (0.3, 0.29, 0.28), 0.95)
    M["roof"] = mat("RoofTile", (0.1, 0.1, 0.11), 0.8)
    M["wood_furn"] = mat("FurnWood", (0.16, 0.12, 0.09), 0.6)
    M["fence"] = mat("Fence", (0.2, 0.17, 0.14), 0.9)
    M["window_warm"] = mat("WindowWarm", (0.8, 0.75, 0.66), 0.2, emit=(1.0, 0.92, 0.8), strength=3.0)
    M["globe"] = mat("Globe", (1, 1, 1), 0.3, emit=(1, 0.95, 0.88), strength=16.0)
    M["water"] = mat("Water", (0.02, 0.025, 0.03), 0.05)
    M["reed"] = mat("Reed", (0.08, 0.09, 0.08), 0.9)
    M["tree"] = mat("Tree", (0.07, 0.075, 0.07), 0.9)
    M["bark"] = mat("Bark", (0.1, 0.09, 0.08), 0.9)
    M["van"] = mat("VanWhite", (0.62, 0.62, 0.6), 0.4, metal=0.2)
    M["tyre"] = mat("Tyre", (0.02, 0.02, 0.02), 0.9)
    M["play_red"] = mat("PlayRed", (0.35, 0.34, 0.33), 0.6)
    M["play_pale"] = mat("PlayPale", (0.55, 0.55, 0.53), 0.6)
    M["sand"] = mat("Sand", (0.4, 0.39, 0.36), 1.0)
    M["sky_night"] = mat("SkyNight", (0.03, 0.035, 0.045), 0.5, emit=(0.08, 0.085, 0.1), strength=0.12)
    M["moon"] = mat("Moon", (1, 1, 1), 0.5, emit=(1, 1, 1), strength=6.0)
    M["hill"] = mat("Hill", (0.03, 0.035, 0.04), 0.9)
    M["fort_win"] = mat("FortWindow", (1, 0.9, 0.7), 0.3, emit=(1.0, 0.88, 0.62), strength=12.0)
    M["white_paint"] = mat("WhitePaint", (0.78, 0.78, 0.76), 0.7)
    return M


S.TONE.update({"Grass": 0.09, "GrassDark": 0.055, "Path": 0.27, "Brick": 0.26, "RoofTile": 0.07, "FurnWood": 0.11, "Fence": 0.15,
               "Water": 0.02, "Reed": 0.07, "Tree": 0.06, "Bark": 0.08, "VanWhite": 0.6, "PlayRed": 0.3, "PlayPale": 0.52,
               "Sand": 0.36, "Hill": 0.03, "WhitePaint": 0.72})


def tree(g, x, y, M, h=None):
    h = h or rnd.uniform(5.0, 8.0)
    g.cyl((x, y, h * 0.3), 0.18, h * 0.6, M["bark"], n=10)
    for k in range(5):
        S_ball(g, (x + rnd.uniform(-1, 1), y + rnd.uniform(-0.8, 0.8), h * 0.6 + rnd.uniform(-0.4, 1.4)), rnd.uniform(1.0, 1.7), M["tree"])


def S_ball(g, c, r, m):
    g.cyl(c, r, r * 1.5, m, n=12, r2=r * 0.55)


def lamp_post(g, x, y, M):
    g.cyl((x, y, 1.8), 0.06, 3.6, M["steel"], n=8)
    g.cyl((x, y, 3.75), 0.2, 0.3, M["globe"], n=12)
    g.cyl((x, y, 3.95), 0.24, 0.08, M["steel"], n=12)


def bench(g, x, y, M):
    g.box((x, y, 0.45), (1.6, 0.45, 0.06), M["wood_furn"])
    g.box((x, y + 0.22, 0.75), (1.6, 0.06, 0.4), M["wood_furn"])
    for dx in (-0.7, 0.7):
        g.box((x + dx, y, 0.22), (0.06, 0.4, 0.45), M["steel"])


def build_garden(coll, M):
    g = S.Group("ParkGarden")
    g.box((17.0, 2.0, -0.05), (34.0, 14.0, 0.1), M["grass"])
    # the back of No. 14: brick, the kitchen window lit, the back door open, the patio
    g.box((0.0, 4.0, 4.0), (0.4, 10.0, 8.0), M["brick"])
    g.box((-4.0, 4.0, 4.0), (8.0, 10.0, 8.0), M["brick"])
    g.box((0.25, 1.4, 1.6), (0.05, 1.4, 1.0), M["window_warm"])
    g.box((0.25, -0.3, 1.1), (0.05, 1.0, 2.2), M["window_warm"])                              # the open back door, light spilling
    g.box((3.0, 1.0, 0.02), (6.0, 6.0, 0.06), M["path"])
    g.box((-4.0, 4.0, 8.8), (8.6, 10.6, 1.6), M["roof"])
    # the washing line, a BBQ, the toy chest
    for x in (9.0, 15.0):
        g.cyl((x, 4.2, 1.0), 0.03, 2.0, M["steel"], n=6)
    g.beam((9.0, 4.2, 1.9), (15.0, 4.2, 1.85), 0.01, 0.01, M["white_paint"])
    for k in range(4):
        g.box((10.0 + k * 1.3, 4.2, 1.6), (0.5, 0.02, 0.55), M["play_pale"])
    g.cyl((12.5, 3.2, 0.5), 0.3, 0.1, M["steel"], n=14)
    g.cyl((12.5, 3.2, 0.25), 0.04, 0.5, M["steel"], n=6)
    g.box((19.0, 3.6, 0.3), (0.9, 0.5, 0.6), M["play_red"])
    # the play area: the slide, the swings (the seat is separate), a sandpit
    g.beam((21.5, 2.6, 0.0), (21.5, 2.6, 1.9), 0.06, 0.06, M["steel"])
    g.beam((22.1, 2.6, 0.0), (22.1, 2.6, 1.9), 0.06, 0.06, M["steel"])
    for k in range(6):
        g.box((21.8, 2.6, 0.3 + k * 0.3), (0.6, 0.06, 0.04), M["steel"])
    g.beam((21.8, 2.6, 1.85), (24.8, 2.6, 0.25), 0.55, 0.04, M["play_pale"])
    for x in (SWING_X - 1.6, SWING_X + 1.6):
        g.beam((x, SWING_Y - 0.8, 0.0), (x, SWING_Y, 2.4), 0.07, 0.07, M["steel"])
        g.beam((x, SWING_Y + 0.8, 0.0), (x, SWING_Y, 2.4), 0.07, 0.07, M["steel"])
    g.beam((SWING_X - 1.6, SWING_Y, 2.4), (SWING_X + 1.6, SWING_Y, 2.4), 0.08, 0.08, M["steel"])
    for dx in (-0.25, 0.25):                                                                 # the other swing, still
        g.beam((SWING_X + 0.8 + dx, SWING_Y, 2.38), (SWING_X + 0.8 + dx, SWING_Y, 0.55), 0.015, 0.015, M["steel"])
    g.box((SWING_X + 0.8, SWING_Y, 0.52), (0.6, 0.24, 0.05), M["wood_furn"])
    g.box((30.5, 3.0, 0.08), (2.4, 2.0, 0.16), M["wood_furn"])
    g.box((30.5, 3.0, 0.12), (2.2, 1.8, 0.1), M["sand"])
    # the back fence, its gate kicked in
    for k in range(int(12 / 0.18)):
        y = -3.5 + k * 0.18
        if -1.2 < y < 1.2:
            continue
        g.box((34.0, y, 0.9), (0.06, 0.16, 1.8), M["fence"])
    g.box((34.6, 0.4, 0.5), (0.06, 1.0, 1.6), M["fence"], rot=(0, 70, 30))                 # the gate, on the ground
    return g.finish(coll)


def build_park(coll, M):
    g = S.Group("ParkPark")
    g.box((82.0, 2.0, -0.05), (96.0, 18.0, 0.1), M["grass_dark"])
    g.box((82.0, 0.0, -0.02), (96.0, 2.4, 0.06), M["path"])                                   # the path along the lane
    # the playground: a climbing frame, a roundabout, a see-saw, benches
    cx = 44.0
    for dx in (0.0, 1.2, 2.4):
        for dy in (2.4, 3.6):
            g.cyl((cx + dx, dy, 1.1), 0.04, 2.2, M["steel"], n=6)
    for z in (0.7, 1.4, 2.1):
        g.beam((cx, 2.4, z), (cx + 2.4, 2.4, z), 0.03, 0.03, M["steel"])
        g.beam((cx, 3.6, z), (cx + 2.4, 3.6, z), 0.03, 0.03, M["steel"])
    g.cyl((53.0, 3.0, 0.25), 1.1, 0.08, M["play_pale"], n=24)
    g.cyl((53.0, 3.0, 0.6), 0.05, 0.8, M["steel"], n=6)
    g.box((59.0, 3.2, 0.45), (3.0, 0.25, 0.06), M["play_red"], rot=(0, 10, 0))
    g.box((59.0, 3.2, 0.2), (0.2, 0.2, 0.4), M["steel"])
    for x in (40.0, 63.0):
        bench(g, x, 4.2, M)
    for x in (37.0, 50.0, 66.0, 74.0, 98.0, 104.0):
        lamp_post(g, x, -1.5, M)
    for (x, y) in ((38, 7), (47, 8.5), (56, 7.5), (64, 9), (71, 6.5), (80, 9.5), (90, 9), (96, 7)):
        tree(g, x, y, M)
    # the pond, its reeds, the footbridge the lane crosses
    g.box(((POND_X0 + POND_X1) / 2, 3.2, -0.06), (POND_X1 - POND_X0, 6.0, 0.02), M["water"])
    for _ in range(60):
        x = rnd.uniform(POND_X0, POND_X1)
        y = rnd.choice((rnd.uniform(0.4, 0.9), rnd.uniform(5.6, 6.2)))
        g.cyl((x, y, 0.45), 0.012, 0.9 + rnd.uniform(-0.2, 0.3), M["reed"], n=4)
    bx0, bx1 = 82.0, 88.0
    for side in (-1.2, 1.2):
        g.beam((bx0, side, 0.9), (bx1, side, 0.9), 0.05, 0.05, M["wood_furn"])
        for k in range(7):
            g.cyl((bx0 + k, side, 0.45), 0.03, 0.9, M["wood_furn"], n=6)
    return g.finish(coll)


def build_carpark(coll, M):
    g = S.Group("ParkCarpark")
    g.box((116.0, 2.0, -0.05), (32.0, 18.0, 0.1), M["asphalt"])
    for k in range(5):
        g.box((106.0 + k * 4.0, 4.0, -0.0), (0.12, 4.0, 0.01), M["white_paint"])
    # the park gate and the railings
    for k in range(int(10 / 0.25)):
        g.cyl((100.0, -3.0 + k * 0.25, 0.9), 0.02, 1.8, M["steel"], n=5)
    for y in (-1.4, 1.4):
        g.box((100.0, y, 1.1), (0.3, 0.3, 2.2), M["brick"])
    g.cyl((124.0, -1.6, 2.5), 0.07, 5.0, M["steel"], n=8)
    g.box((124.0, -1.0, 5.0), (0.3, 1.2, 0.12), M["steel"])
    g.box((124.0, -0.5, 4.92), (0.3, 0.3, 0.06), M["globe"])
    # the white van (its back doors are separate: they open)
    vx, vy = 118.0, 3.0
    g.box((vx, vy, 1.25), (5.2, 2.0, 2.1), M["van"])
    g.box((vx + 2.85, vy, 0.95), (0.6, 2.0, 1.4), M["van"])
    g.box((vx + 2.55, vy - 1.01, 1.55), (0.7, 0.02, 0.6), M["glass"])
    for wx in (vx - 1.7, vx + 2.0):
        for wy in (vy - 1.0, vy + 1.0):
            g.cyl((wx, wy, 0.35), 0.35, 0.24, M["tyre"], rot=(90, 0, 0), n=16)
    g.box((vx - 2.62, vy, 1.25), (0.05, 1.9, 1.9), M["steel"])                              # its dark inside
    # the backs of the houses beyond, a low wall, and the night
    for k in range(10):
        x = 100.0 + k * 6.0
        if 106.0 < x < 136.0:
            continue                                                                             # an empty plot: the hill beyond
        g.box((x, 12.0, 3.0), (5.6, 4.0, 6.0), M["brick"])
        g.box((x, 12.0, 6.5), (5.8, 4.2, 1.2), M["roof"])
        if rnd.random() < 0.4:
            g.box((x + rnd.uniform(-1.5, 1.5), 9.95, 2.0 + rnd.choice((0.0, 2.6))), (1.0, 0.05, 1.0), M["window_warm"])
    return g.finish(coll)


def build_sky(coll, M):
    g = S.Group("ParkSky")
    g.box((60.0, 160.0, 50.0), (500.0, 1.0, 120.0), M["sky_night"])
    g.cyl((40.0, 159.0, 60.0), 4.0, 0.2, M["moon"], rot=(90, 0, 0), n=32)
    # Kessel Hill on the horizon, the fortress on top, its windows lit
    hx, hy = 128.0, 110.0
    for k in range(24):
        w = 140 - k * 5.3
        g.box((hx, hy, 1 + k * 1.45), (w, 10.0, 1.6), M["hill"])
    fx, fz = hx + 2, 36.0
    g.box((fx, hy - 2, fz + 4), (22.0, 6.0, 8.0), M["hill"])
    for tx in (fx - 10, fx + 10):
        g.box((tx, hy - 2, fz + 8), (4.0, 6.0, 16.0), M["hill"])
        for k in range(5):
            g.box((tx - 1.6 + k * 0.8, hy - 2, fz + 16.6), (0.5, 6.0, 1.2), M["hill"])          # battlements
    for _ in range(26):
        g.box((fx + rnd.uniform(-10, 10), hy - 5.1, fz + rnd.uniform(1.5, 13)), (0.7, 0.1, 1.0), M["fort_win"])
    g.box((fx, hy - 5.2, fz + 18.5), (0.6, 0.1, 0.6), M["fort_win"])                          # a light in the top
    # the town's roofs between, black, a few lit windows
    for k in range(30):
        x = 40 + k * 6.5
        if 104 < x < 150:
            continue
        h = rnd.uniform(6, 16)
        g.box((x, 60 + rnd.uniform(0, 20), h / 2), (5.5, 5.0, h), M["city"])
    return g.finish(coll)


def build_swing(coll, M):
    g = S.Group("Swing")
    x, y = SWING_X - 0.8, SWING_Y
    for dx in (-0.25, 0.25):
        g.beam((x + dx, y, 2.38), (x + dx, y, 0.55), 0.015, 0.015, M["steel"])
    g.box((x, y, 0.52), (0.62, 0.24, 0.05), M["wood_furn"])
    return g.finish(coll)


def build_van_doors(coll, M):
    g = S.Group("VanDoors")
    vx, vy = 118.0, 3.0
    for s in (-1, 1):
        g.box((vx - 2.66, vy + s * 0.48, 1.25), (0.05, 0.92, 1.9), M["van"])
    return g.finish(coll)


def lights():
    def add(kind, name, loc, energy, size=1.0, rot=None):
        ld = bpy.data.lights.new(name, kind)
        ld.energy = energy
        ld.color = S.NEUTRAL
        if kind == "AREA":
            ld.size = size
        if kind in ("POINT", "SPOT"):
            ld.shadow_soft_size = size
        ob = bpy.data.objects.new(name, ld)
        ob.location = loc
        if rot is not None:
            ob.rotation_euler = rot
        bpy.context.scene.collection.objects.link(ob)
        return ob
    moon = add("SUN", "Moon", (0, 0, 50), 0.35)
    moon.rotation_euler = (math.radians(50), 0, math.radians(160))
    for x in (37.0, 50.0, 66.0, 74.0, 98.0, 104.0):
        add("POINT", "ParkLamp", (x, -1.5, 3.6), 160, size=0.25)
    add("POINT", "CarparkLamp", (124.0, -0.5, 4.8), 260, size=0.25)
    add("AREA", "KitchenSpill", (1.2, 0.2, 1.4), 120, size=1.4, rot=(0, math.radians(90), 0))
    add("AREA", "Fill", (60.0, -14.0, 6.0), 900, size=60.0, rot=(math.radians(75), 0, 0))
    w = bpy.data.worlds.new("Night")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.055, 0.07, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "ParkSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported ParkSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    objs = [build_garden(coll, M), build_park(coll, M), build_carpark(coll, M), build_sky(coll, M),
            build_swing(coll, M), build_van_doors(coll, M)]
    lights()
    if preview_only:
        cams = [
            ("Y2  the garden at nightfall", S.shot_camera("P1", (16.0, 2.0, -11.0), (16.0, 1.2, 1.0), 42), None),
            ("Y3  the swing, the play area", S.shot_camera("P2", (25.0, 1.4, -3.0), (27.0, 0.6, 2.6), 46), None),
            ("Y8  the playground", S.shot_camera("P3", (50.0, 2.0, -11.0), (50.0, 1.2, 1.0), 42), None),
            ("Y9  the pond and the footbridge", S.shot_camera("P4", (85.0, 2.0, -11.0), (85.0, 1.0, 1.0), 42), None),
            ("Y10  the car park, the van", S.shot_camera("P5", (115.0, 2.0, -11.0), (115.0, 1.2, 1.0), 42), None),
            ("Y13  the fortress on the hill", S.shot_camera("P6", (118.0, 1.2, -4.0), (128.0, 26.0, 110.0), 34), None),
        ]
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        S.preview(cams, "park_set.png")
        return
    for o in objs:
        S.unwrap(o)
    S.bake(objs[0], 4096, 20)
    S.bake(objs[1], 4096, 20)
    S.bake(objs[2], 2048, 16)
    S.bake(objs[3], 2048, 8)
    S.bake(objs[4], 256, 8)
    S.bake(objs[5], 256, 8)
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(objs + marks)


if __name__ == "__main__":
    main()
