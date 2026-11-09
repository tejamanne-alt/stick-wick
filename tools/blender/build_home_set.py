"""Chapter 3: HOME (docs/story/story_v2.md, chapter 3; docs/story/chapter3_home.md). 14 Alder Lane at
dusk: a quiet suburban road, the gate and the garden path with her chalk hopscotch, and the house cut
away side-on like the warehouse. Two storeys:

  ground (z 0)    porch 0-3 | hall 3-9 (the stairs up the back wall) | study 9-15 |
                  living room 15-25 | kitchen 25-34 | back door -> the garden and the play area
  upstairs (3.2)  landing 3-14 | Maya's room 14-22 | the bedroom 22-34

A lived-in family home, dressed room by room: papered walls, skirting, cornices, framed doors and
open doors with handles, windows with frames, sills, curtains and radiators, switches and sockets,
pendant lamps, a staircase with a runner and balusters, and each room's furniture and clutter.
Outside: the road with its kerbs and markings, paving, low walls and hedges, the neighbours'
houses, street lamps, trees, the garden with its fence, bins, a washing line, a shed, the play area.

The same palette as the rest of the game (the tower's: build_story_set's TONE and detail.py's
greys). A low sun comes in through the back windows; a few lamps are on.

Parts (each its own baked texture, Story_<name>.png):
  HomeShell     the house's structure and furniture (bevelled)
  HomeDetail    the small things: books, toys, mugs, switches, shoes, handles (not bevelled)
  HomeFront     the front wall (the camera side), cut away by the game
  HomeOutside   Alder Lane, the neighbours, the garden and the play area
  Swing         the swing seat (it moves)
  Story props, separate so the story can find and frame them (origin at each one's base centre):
    HallTable, HarrowCard, ElenaNote     the hall table, Harrow's card and Elena's note on it
    FamilyPhoto                          the photo frame on the study desk
    ShelfWalkie, MayaDrawing             Maya's room: the one walkie-talkie left on her shelf (a
                                         clean gap beside it where its twin stood), her drawing
    CleanSheeting, Bleach                the kitchen: plastic sheeting on the floor; the bleach
                                         bottle and bucket by the back door
    KitchenChair                         the chair in the middle of the kitchen (the interrogation)

    /home/user/.venv-blender/bin/python tools/blender/build_home_set.py [--preview]

--preview builds the set and renders docs/previews/home_set.png and home_set_props.png (no bake,
no export). Without it: bakes every part and writes Assets/Art/Home/HomeSet.fbx.
Coordinates: Blender X = Unity x, Y = Unity z (depth), Z = Unity y."""
import math
import os
import random
import sys

import bpy  # noqa: E402
import bmesh
from mathutils import Matrix, Vector, noise

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_story_set as S  # noqa: E402
import detail as D  # noqa: E402

ROOT = S.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Home")
rnd = random.Random(31)

X0, X1 = 0.0, 34.0          # the house's ends (the porch is outside x0)
YF, YB = -2.0, 4.2          # front (cut away) and back walls
FLOOR2 = 3.2                # the upstairs floor
F = FLOOR2
H1, H2 = 3.0, 6.0           # ceiling heights
DOORS1 = (3.0, 9.0, 15.0, 25.0)     # ground-floor walls with doorways (3 = the front door)
DOORS2 = (14.0, 22.0)
STAIR_X0, STAIR_X1 = 4.2, 9.4
WALL = YB - 0.02            # the papered face of the back wall
LANE = 0.8                  # the fight lane: |y| < LANE kept clear at floor level

# the back windows (x, sill z, width, height): the sun shafts in HomeBuilder sit on these
WIN1 = ((10.4, 0.9, 1.4, 1.5), (21.6, 0.9, 1.6, 1.6), (28.6, 1.3, 1.6, 1.1))
WIN2 = ((12.4, F + 0.9, 1.2, 1.4), (18.0, F + 1.0, 1.0, 1.0), (30.8, F + 1.0, 1.4, 1.5))


# ---------------------------------------------------------------- materials

def pmat(name, plane, kind, g, g2=None, scale=1.0, rough=0.85, bump=0.12, grime=0.22, p=None):
    """A patterned grey surface (object coordinates, like detail.py's): wallpaper stripes, dots or
    a trellis, tiles, a checker floor, wood grain, fabric or plain noise. p: the pattern's size."""
    key = (name, plane)
    if key in D.MATS:
        return D.MATS[key]
    m, nt, bsdf = D._base(f"{name}_{plane}")
    co = D._coords(nt, plane, scale)
    g2 = g * 0.8 if g2 is None else g2
    N = nt.nodes
    L = nt.links
    fac = None
    col = None

    def ramp(src, a, b, pa=0.0, pb=1.0, const=False):
        r = N.new("ShaderNodeValToRGB")
        if const:
            r.color_ramp.interpolation = "CONSTANT"
        r.color_ramp.elements[0].position = pa
        r.color_ramp.elements[0].color = D._grey(a)
        r.color_ramp.elements[1].position = pb
        r.color_ramp.elements[1].color = D._grey(b)
        L.new(src, r.inputs["Fac"])
        return r.outputs["Color"]

    if kind == "stripe":
        w = N.new("ShaderNodeTexWave")
        w.wave_type = "BANDS"
        w.bands_direction = "X"
        w.inputs["Scale"].default_value = 0.628 / (p or 0.2)
        w.inputs["Distortion"].default_value = 0.0
        L.new(co, w.inputs["Vector"])
        fac = w.outputs["Fac"]
        col = ramp(fac, g2, g, 0.0, 0.62, const=True)
    elif kind == "dots":
        v = N.new("ShaderNodeTexVoronoi")
        v.inputs["Scale"].default_value = 1.0 / (p or 0.2)
        v.inputs["Randomness"].default_value = 0.0
        L.new(co, v.inputs["Vector"])
        fac = v.outputs["Distance"]
        col = ramp(fac, g2, g, 0.0, 0.2, const=True)
    elif kind in ("trellis", "tile"):
        b = N.new("ShaderNodeTexBrick")
        b.inputs["Color1"].default_value = D._grey(g)
        b.inputs["Color2"].default_value = D._grey(g * (0.97 if kind == "trellis" else 0.9))
        b.inputs["Mortar"].default_value = D._grey(g2)
        b.inputs["Scale"].default_value = 1.0
        bw, bh = p or ((0.32, 0.16) if kind == "trellis" else (0.15, 0.075))
        b.inputs["Brick Width"].default_value = bw
        b.inputs["Row Height"].default_value = bh
        b.inputs["Mortar Size"].default_value = 0.012 if kind == "trellis" else 0.006
        b.offset = 0.5
        L.new(co, b.inputs["Vector"])
        fac = b.outputs["Fac"]
        col = b.outputs["Color"]
    elif kind == "check":
        c = N.new("ShaderNodeTexChecker")
        c.inputs["Color1"].default_value = D._grey(g)
        c.inputs["Color2"].default_value = D._grey(g2)
        c.inputs["Scale"].default_value = 1.0 / (p or 0.3)
        L.new(co, c.inputs["Vector"])
        fac = c.outputs["Fac"]
        col = c.outputs["Color"]
    elif kind == "grain":
        w = N.new("ShaderNodeTexWave")
        w.wave_type = "BANDS"
        w.bands_direction = "Z"
        w.inputs["Scale"].default_value = 1.5
        w.inputs["Distortion"].default_value = 7.0
        w.inputs["Detail"].default_value = 3.0
        L.new(co, w.inputs["Vector"])
        fac = w.outputs["Fac"]
        col = ramp(fac, g2, g)
    else:                                   # "noise" (plaster, fabric, grass, asphalt)
        nz = N.new("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = 1.0 / (p or 0.1)
        nz.inputs["Detail"].default_value = 8.0
        L.new(co, nz.inputs["Vector"])
        fac = nz.outputs["Fac"]
        col = ramp(fac, g2, g, 0.3, 0.7)
    gr = N.new("ShaderNodeTexNoise")
    gr.inputs["Scale"].default_value = 0.7
    gr.inputs["Detail"].default_value = 4.0
    L.new(co, gr.inputs["Vector"])
    mix = N.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = grime
    L.new(col, mix.inputs["A"])
    L.new(gr.outputs["Color"], mix.inputs["B"])
    L.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    if bump > 0:
        bp = N.new("ShaderNodeBump")
        bp.inputs["Strength"].default_value = bump
        L.new(fac, bp.inputs["Height"])
        L.new(bp.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = rough
    D.MATS[key] = m
    return m


def mats():
    M = S.materials()
    mat = S.mat
    fl = D.flat
    # emissive (through the tower palette: their light is neutral)
    M["window_dusk"] = mat("WindowDusk", (0.7, 0.68, 0.64), 0.2, emit=(1.0, 0.95, 0.88), strength=0.9)
    M["lamp"] = mat("LampShade", (1, 1, 1), 0.4, emit=(1, 0.95, 0.88), strength=6.0)
    M["fairy"] = mat("FairyLight", (1, 1, 1), 0.3, emit=(1, 0.95, 0.85), strength=14.0)
    M["sky_dusk"] = mat("SkyDusk", (0.5, 0.5, 0.52), 0.5, emit=(0.72, 0.7, 0.68), strength=1.0)
    M["lantern"] = mat("Lantern", (1, 1, 1), 0.4, emit=(1, 0.95, 0.88), strength=8.0)
    M["win_lit"] = mat("WindowLit", (0.7, 0.68, 0.64), 0.3, emit=(1.0, 0.95, 0.88), strength=1.6)
    M["chalk"] = mat("Chalk", (0.85, 0.85, 0.83), 0.9)
    M["car_black"] = mat("CarBlack", (0.03, 0.03, 0.035), 0.25, metal=0.5)
    M["tyre"] = mat("Tyre", (0.02, 0.02, 0.02), 0.9)
    M["paper"] = mat("Paper", (0.82, 0.82, 0.8), 0.9)
    M["white_paint"] = mat("WhitePaint", (0.78, 0.78, 0.76), 0.5)
    # surfaces
    M["boards"] = D.planks("HomeBoards", 0.2, "xy", board=0.14)
    M["boards_up"] = D.planks("HomeBoardsUp", 0.22, "xy", board=0.14)
    M["kitchen_floor"] = pmat("KitchenCheck", "xy", "check", 0.5, 0.07, p=0.3, rough=0.35, bump=0.0)
    M["hall_tiles"] = pmat("HallTiles", "xy", "check", 0.42, 0.12, p=0.15, rough=0.4, bump=0.0)
    M["plaster"] = pmat("Plaster", "xz", "noise", 0.5, 0.45, p=0.5, bump=0.05)
    M["plaster_y"] = pmat("Plaster", "yz", "noise", 0.5, 0.45, p=0.5, bump=0.05)
    M["ceiling"] = pmat("Ceiling", "xy", "noise", 0.55, 0.5, p=0.6, bump=0.0)
    M["paper_hall"] = pmat("PaperHall", "xz", "stripe", 0.5, 0.41, p=0.16, bump=0.0)
    M["paper_study"] = pmat("PaperStudy", "xz", "trellis", 0.27, 0.19, p=(0.24, 0.24), bump=0.05)
    M["paper_living"] = pmat("PaperLiving", "xz", "stripe", 0.5, 0.44, p=0.32, bump=0.0)
    M["paper_kitchen"] = pmat("PaperKitchen", "xz", "noise", 0.6, 0.55, p=0.4, bump=0.0)
    M["paper_maya"] = pmat("PaperMaya", "xz", "dots", 0.62, 0.45, p=0.16, bump=0.0)
    M["paper_bed"] = pmat("PaperBed", "xz", "trellis", 0.45, 0.36, p=(0.3, 0.3), bump=0.04)
    M["wainscot"] = pmat("Wainscot", "xz", "tile", 0.6, 0.42, p=(0.45, 0.8), rough=0.5, bump=0.3)
    M["splash"] = pmat("Splashback", "xz", "tile", 0.68, 0.36, p=(0.15, 0.075), rough=0.2, bump=0.2)
    M["skirting"] = mat("Skirting", (0.75, 0.75, 0.73), 0.5)
    M["wood_furn"] = pmat("FurnWood", "xz", "grain", 0.13, 0.08, scale=3.0, rough=0.45, bump=0.05)
    M["wood_top"] = pmat("FurnWood", "xy", "grain", 0.13, 0.08, scale=3.0, rough=0.45, bump=0.05)
    M["oak"] = pmat("Oak", "xz", "grain", 0.34, 0.25, scale=3.0, rough=0.5, bump=0.05)
    M["oak_top"] = pmat("Oak", "xy", "grain", 0.34, 0.25, scale=3.0, rough=0.5, bump=0.05)
    M["cabinet"] = fl("CabinetPaint", 0.56, rough=0.4)
    M["worktop"] = pmat("Worktop", "xy", "noise", 0.11, 0.06, p=0.04, rough=0.2, bump=0.0)
    M["fabric"] = pmat("SofaFabric", "xz", "noise", 0.16, 0.11, p=0.01, rough=1.0, bump=0.15)
    M["fabric_pale"] = pmat("Linen", "xz", "noise", 0.66, 0.57, p=0.012, rough=1.0, bump=0.1)
    M["curtain"] = pmat("Curtain", "xz", "stripe", 0.34, 0.27, p=0.09, rough=1.0, bump=0.0)
    M["curtain_kid"] = pmat("CurtainKid", "xz", "dots", 0.7, 0.5, p=0.1, rough=1.0, bump=0.0)
    M["duvet_kid"] = pmat("DuvetKid", "xy", "dots", 0.72, 0.5, p=0.12, rough=1.0, bump=0.0)
    M["rug"] = pmat("Rug", "xy", "stripe", 0.42, 0.12, p=0.2, rough=1.0, bump=0.1)
    M["rug_kid"] = pmat("RugKid", "xy", "dots", 0.75, 0.55, p=0.25, rough=1.0, bump=0.1)
    M["carpet"] = pmat("Carpet", "xy", "noise", 0.3, 0.24, p=0.01, rough=1.0, bump=0.15)
    M["carpet_kid"] = pmat("CarpetKid", "xy", "noise", 0.5, 0.44, p=0.01, rough=1.0, bump=0.15)
    M["runner"] = pmat("Runner", "xy", "stripe", 0.15, 0.06, p=0.08, rough=1.0, bump=0.1)
    M["runner_x"] = pmat("Runner", "xz", "stripe", 0.15, 0.06, p=0.08, rough=1.0, bump=0.1)
    M["coir"] = pmat("Coir", "xy", "noise", 0.24, 0.15, p=0.006, rough=1.0, bump=0.3)
    M["mirror"] = fl("Mirror", 0.55, rough=0.04, metal=1.0)
    M["ink"] = fl("HomeInk", 0.03, rough=0.5)
    M["screen"] = fl("HomeScreen", 0.015, rough=0.1)
    M["chrome"] = fl("HomeChrome", 0.5, rough=0.2, metal=1.0)
    M["brass"] = fl("HomeBrass", 0.42, rough=0.3, metal=1.0)
    M["ceramic"] = fl("HomeCeramic", 0.72, rough=0.2)
    M["plastic_w"] = fl("PlasticWhite", 0.66, rough=0.35)
    M["plastic_d"] = fl("PlasticDark", 0.07, rough=0.4)
    M["toy"] = [fl("ToyA", 0.7, rough=0.4), fl("ToyB", 0.38, rough=0.4), fl("ToyC", 0.12, rough=0.4), fl("ToyD", 0.55, rough=0.4)]
    M["books"] = [fl("BookA", 0.09), fl("BookB", 0.24), fl("BookC", 0.45), fl("BookD", 0.16), fl("BookE", 0.62), fl("BookF", 0.32)]
    M["fruit"] = [fl("FruitA", 0.42, rough=0.3), fl("FruitB", 0.22, rough=0.3), fl("FruitC", 0.6, rough=0.3)]
    M["leaf"] = pmat("Leaves", "xz", "noise", 0.1, 0.045, p=0.05, rough=0.9, bump=0.5)
    M["leaf_top"] = pmat("Leaves", "xy", "noise", 0.1, 0.045, p=0.05, rough=0.9, bump=0.5)
    M["soil"] = fl("Soil", 0.045, rough=1.0)
    M["sheeting"] = pmat("Sheeting", "xy", "noise", 0.7, 0.56, p=0.08, rough=0.12, bump=0.25, grime=0.08)
    M["bark"] = pmat("Bark", "xz", "noise", 0.08, 0.04, p=0.03, rough=1.0, bump=0.4)
    # outside
    M["brick"] = D.stone("HomeBrick", 0.28, 0.13, "xz", block=(0.225, 0.075))
    M["brick_y"] = D.stone("HomeBrick", 0.28, 0.13, "yz", block=(0.225, 0.075))
    M["brick_n"] = D.stone("NeighbourBrick", 0.22, 0.11, "xz", block=(0.225, 0.075))
    M["brick_ny"] = D.stone("NeighbourBrick", 0.22, 0.11, "yz", block=(0.225, 0.075))
    M["roof"] = D.stone("HomeRoof", 0.085, 0.03, "xy", block=(0.3, 0.22))
    M["roof_y"] = D.stone("HomeRoof", 0.085, 0.03, "yz", block=(0.3, 0.22))
    M["paving"] = D.stone("HomePaving", 0.22, 0.09, "xy", block=(0.6, 0.6))
    M["path"] = D.stone("HomePath", 0.24, 0.1, "xy", block=(0.45, 0.45))
    M["coping"] = D.stone("Coping", 0.36, 0.15, "xz", block=(0.6, 0.12))
    M["kerb"] = D.stone("Kerb", 0.3, 0.1, "xz", block=(0.9, 0.3))
    M["asphalt"] = pmat("Asphalt", "xy", "noise", 0.065, 0.04, p=0.05, rough=0.45, bump=0.2)
    M["grass"] = pmat("Lawn", "xy", "noise", 0.13, 0.07, p=0.25, rough=0.95, bump=0.4)
    M["fence"] = D.planks("FenceBoard", 0.13, "xz", board=0.15)
    M["fence_y"] = D.planks("FenceBoard", 0.13, "yz", board=0.15)
    M["shed"] = D.planks("ShedBoard", 0.1, "xz", board=0.12)
    M["shed_y"] = D.planks("ShedBoard", 0.1, "yz", board=0.12)
    M["sand"] = pmat("Sand", "xy", "noise", 0.5, 0.42, p=0.03, rough=1.0, bump=0.3)
    return M


S.TONE.update({"Skirting": 0.72, "CarBlack": 0.025, "Chalk": 0.82, "Paper": 0.8, "WhitePaint": 0.72})


# ---------------------------------------------------------------- small geometry helpers

class Loc:
    """Parts placed in a local frame (origin, yaw about z): a chair, a sofa, a bike turned in the room."""

    def __init__(self, g, o, yaw=0.0):
        self.g, self.o, self.yaw = g, Vector(o), yaw
        self.R = Matrix.Rotation(math.radians(yaw), 4, "Z")

    def p(self, x, y, z):
        return tuple(self.o + (self.R @ Vector((x, y, z))))

    def box(self, c, s, m, rot=(0, 0, 0)):
        self.g.box(self.p(*c), s, m, rot=(rot[0], rot[1], rot[2] + self.yaw))

    def cyl(self, c, r, h, m, rot=(0, 0, 0), n=12, r2=None):
        self.g.cyl(self.p(*c), r, h, m, rot=(rot[0], rot[1], rot[2] + self.yaw), n=n, r2=r2)

    def beam(self, a, b, w, h, m):
        self.g.beam(self.p(*a), self.p(*b), w, h, m)

    def ball(self, c, r, m, n=8, squash=(1, 1, 1)):
        ball(self.g, self.p(*c), r, m, n, squash)


def ball(g, c, r, m, n=8, squash=(1, 1, 1)):
    tmp = bmesh.new()
    bmesh.ops.create_uvsphere(tmp, u_segments=n, v_segments=max(4, n // 2 + 1), radius=r)
    bmesh.ops.transform(tmp, matrix=Matrix.Translation(Vector(c)) @ Matrix.Diagonal((*squash, 1)), verts=tmp.verts)
    g._merge(tmp, m)


def flat_text(g, text, c, size, m, rot_z=0.0):
    """Letters lying flat on the ground (chalk)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.0012
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    me.transform(Matrix.Translation(Vector(c)) @ Matrix.Rotation(math.radians(rot_z), 4, "Z"))
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def prism_x(g, pts_yz, x0, x1, m):
    """Extrude a (y, z) outline along x (a gable, a roof end)."""
    tmp = bmesh.new()
    f0 = [tmp.verts.new((x0, y, z)) for y, z in pts_yz]
    f1 = [tmp.verts.new((x1, y, z)) for y, z in pts_yz]
    tmp.faces.new(f0)
    tmp.faces.new(list(reversed(f1)))
    n = len(pts_yz)
    for i in range(n):
        tmp.faces.new((f0[i], f0[(i + 1) % n], f1[(i + 1) % n], f1[i]))
    g._merge(tmp, m)


def sheet(g, x0, x1, y0, y1, z, m, res=0.12, amp=0.014, seed=0.0):
    """A crumpled sheet lying on the floor (plastic sheeting, a dropped duvet)."""
    tmp = bmesh.new()
    nx, ny = max(2, int((x1 - x0) / res)), max(2, int((y1 - y0) / res))
    vs = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = x0 + (x1 - x0) * i / nx
            y = y0 + (y1 - y0) * j / ny
            h = noise.noise(Vector((x * 2.6 + seed, y * 2.6, 0.5))) + 0.5 * noise.noise(Vector((x * 7.0, y * 7.0 + seed, 1.5)))
            row.append(tmp.verts.new((x, y, z + amp * max(0.0, 0.6 + h))))
        vs.append(row)
    for j in range(ny):
        for i in range(nx):
            tmp.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
    g._merge(tmp, m)


def rects_minus(x0, x1, z0, z1, holes):
    """The rectangle (x0..x1, z0..z1) minus the holes, as a list of rectangles."""
    hs = [h for h in holes if h[1] > x0 and h[0] < x1 and h[3] > z0 and h[2] < z1]
    xs = sorted({x0, x1} | {h[0] for h in hs if x0 < h[0] < x1} | {h[1] for h in hs if x0 < h[1] < x1})
    out = []
    for a, b in zip(xs, xs[1:]):
        mid = (a + b) / 2
        cur = z0
        for hz0, hz1 in sorted((h[2], h[3]) for h in hs if h[0] <= mid <= h[1]):
            if hz0 > cur:
                out.append((a, b, cur, min(hz0, z1)))
            cur = max(cur, hz1)
        if cur < z1:
            out.append((a, b, cur, z1))
    return out


def paper_wall(g, x0, x1, z0, z1, m, holes=(), y=YB, t=0.02):
    for a, b, c, d in rects_minus(x0, x1, z0, z1, holes):
        g.box(((a + b) / 2, y - t / 2, (c + d) / 2), (b - a, t, d - c), m)


def win_hole(w):
    x, z0, ww, h = w
    return (x - ww / 2 - 0.06, x + ww / 2 + 0.06, z0 - 0.06, z0 + h + 0.06)


# ---------------------------------------------------------------- fittings

def window(gs, gd, x, z0, w, h, M, curtains="curtain", radiator=True, floor_len=False, blind=False, floor=0.0):
    """A back window: the glowing pane (the low sun), its casing, mullion and transom, the sill,
    curtains on a rod (or a blind), a radiator under it."""
    zc = z0 + h / 2
    gs.box((x, YB - 0.01, zc), (w + 0.14, 0.02, h + 0.14), M["window_dusk"])
    y = WALL - 0.03
    for s in (-1, 1):
        gs.box((x + s * (w / 2 + 0.04), y, zc), (0.09, 0.07, h + 0.18), M["white_paint"])
    gs.box((x, y, z0 + h + 0.045), (w + 0.17, 0.07, 0.09), M["white_paint"])
    gs.box((x, WALL - 0.1, z0 - 0.04), (w + 0.3, 0.2, 0.045), M["white_paint"])          # the sill
    gs.box((x, WALL - 0.03, z0 - 0.1), (w + 0.18, 0.05, 0.08), M["white_paint"])
    gd.box((x, YB - 0.03, zc), (0.045, 0.04, h), M["white_paint"])                         # mullion
    gd.box((x, YB - 0.03, z0 + h * 0.72), (w, 0.04, 0.045), M["white_paint"])             # transom
    for s in (-1, 1):                                                                     # glazing bars over the top lights
        gd.box((x + s * w / 4, YB - 0.025, z0 + h * 0.86), (0.018, 0.03, h * 0.28), M["white_paint"])
    gd.box((x - w / 4, YB - 0.04, z0 + h * 0.36), (0.1, 0.02, 0.02), M["chrome"])          # catch
    if curtains:
        cm = M[curtains]
        top = z0 + h + 0.2
        gd.cyl((x, WALL - 0.13, top), 0.014, w + 0.9, M["wood_furn"], rot=(0, 90, 0), n=8)
        for s in (-1, 1):
            gd.cyl((x + s * (w / 2 + 0.45), WALL - 0.13, top), 0.03, 0.06, M["wood_furn"], rot=(0, 90, 0), n=8)
            gd.box((x + s * (w / 2 + 0.43), WALL - 0.04, top), (0.02, 0.1, 0.03), M["wood_furn"])
        bot = floor + 0.02 if floor_len else z0 - 0.18
        hh = top - 0.03 - bot
        for s in (-1, 1):
            cx = x + s * (w / 2 + 0.2)
            for k in range(5):
                fx = cx + s * (k - 2) * 0.075
                gs.cyl((fx, WALL - 0.13 + (0.025 if k % 2 else -0.02), bot + hh / 2), 0.045, hh, cm, n=7)
            gd.box((cx, WALL - 0.15, bot + hh * 0.42), (0.36, 0.13, 0.04), cm)            # the tie-back
    if blind:
        gs.box((x, WALL - 0.06, z0 + h - 0.12), (w + 0.04, 0.03, 0.3), M["fabric_pale"])
        gd.cyl((x, WALL - 0.06, z0 + h + 0.04), 0.035, w + 0.08, M["white_paint"], rot=(0, 90, 0), n=8)
        gd.box((x, WALL - 0.08, z0 + h - 0.28), (w + 0.02, 0.02, 0.025), M["white_paint"])
        gd.beam((x + w * 0.35, WALL - 0.08, z0 + h - 0.28), (x + w * 0.35, WALL - 0.08, z0 + h - 0.45), 0.004, 0.004, M["white_paint"])
    if radiator:
        radiator_unit(gs, gd, x, floor + 0.14, min(w, 1.3), M)


def radiator_unit(gs, gd, x, z0, w, M, h=0.55):
    y = WALL - 0.07
    gs.box((x, y, z0 + h / 2), (w, 0.07, h), M["white_paint"])
    n = int(w / 0.065)
    for k in range(n):                                                                    # the columns
        gd.box((x - w / 2 + 0.035 + k * (w - 0.07) / max(1, n - 1), y - 0.04, z0 + h / 2), (0.028, 0.02, h - 0.06), M["white_paint"])
    gd.box((x, y - 0.03, z0 + h - 0.01), (w, 0.06, 0.02), M["white_paint"])
    for s in (-1, 1):                                                                     # valves and pipes
        gd.cyl((x + s * (w / 2 + 0.06), y, z0 + 0.06), 0.022, 0.08, M["chrome"], n=8)
        gd.cyl((x + s * (w / 2 + 0.06), y, z0 + 0.13), 0.012, 0.05, M["chrome"], n=6, rot=(0, 0, 0))
        gd.cyl((x + s * (w / 2 + 0.06), y, z0 / 2), 0.011, z0, M["white_paint"], n=6)


def switch(g, x, y, z, M, face="y"):
    """A light switch plate on a wall (face y: the back wall; face x+/x-: a cross wall)."""
    if face == "y":
        g.box((x, y - 0.006, z), (0.085, 0.012, 0.085), M["white_paint"])
        g.box((x, y - 0.014, z), (0.02, 0.008, 0.03), M["plastic_w"])
    else:
        s = 1 if face == "x+" else -1
        g.box((x + s * 0.006, y, z), (0.012, 0.085, 0.085), M["white_paint"])
        g.box((x + s * 0.014, y, z), (0.008, 0.02, 0.03), M["plastic_w"])


def socket(g, x, z, M, y=WALL):
    g.box((x, y - 0.006, z), (0.15, 0.012, 0.085), M["white_paint"])
    for dx in (-0.035, 0.035):
        g.box((x + dx, y - 0.013, z), (0.016, 0.004, 0.022), M["ink"])


def pendant(gs, gd, x, y, ceil, M, kind="cone", lit=False):
    gd.cyl((x, y, ceil - 0.012), 0.07, 0.024, M["white_paint"], n=12)
    gd.cyl((x, y, ceil - 0.24), 0.006, 0.45, M["ink"], n=5)
    sm = M["lamp"] if lit else M["fabric_pale"]
    if kind == "cone":
        gs.cyl((x, y, ceil - 0.56), 0.21, 0.24, sm, n=16, r2=0.08)
    elif kind == "drum":
        gs.cyl((x, y, ceil - 0.56), 0.24, 0.22, sm, n=18)
    elif kind == "star":
        for k in range(5):
            a = k / 5 * 360
            gs.box((x, y, ceil - 0.58), (0.36, 0.05, 0.12), sm, rot=(0, 0, a))
        ball(gs, (x, y, ceil - 0.58), 0.1, sm, n=8)
    else:                                                                                 # a glass globe
        ball(gs, (x, y, ceil - 0.6), 0.16, sm, n=12)
    gd.cyl((x, y, ceil - 0.5), 0.03, 0.06, M["chrome"], n=8)


def frame_pic(gs, gd, x, z, w, h, M, art="photo", y=WALL, seed=0):
    """A framed picture on the back wall: frame, mount, and a picture (silhouettes in a photo, a
    horizon and a sun for a print, blocks for an abstract)."""
    r = random.Random(seed + int(x * 100))
    gs.box((x, y - 0.015, z), (w + 0.07, 0.03, h + 0.07), M["wood_furn"] if r.random() < 0.6 else M["ink"])
    gd.box((x, y - 0.032, z), (w, 0.004, h), M["paper"])
    iw, ih = w * 0.78, h * 0.78
    if art == "photo":
        gd.box((x, y - 0.035, z), (iw, 0.003, ih), M["books"][2])
        n = r.choice((2, 3, 3, 4))
        for k in range(n):
            fx = x - iw / 2 + iw * (k + 0.5) / n
            hh = ih * r.uniform(0.45, 0.7)
            gd.box((fx, y - 0.038, z - ih / 2 + hh / 2), (iw / n * 0.42, 0.003, hh), M["books"][0])
            gd.cyl((fx, y - 0.038, z - ih / 2 + hh + iw / n * 0.16), iw / n * 0.17, 0.003, M["books"][0], rot=(90, 0, 0), n=10)
    elif art == "print":
        gd.box((x, y - 0.035, z + ih * 0.2), (iw, 0.003, ih * 0.6), M["books"][4])
        gd.box((x, y - 0.035, z - ih * 0.3), (iw, 0.003, ih * 0.4), M["books"][1])
        gd.cyl((x + iw * 0.2, y - 0.038, z + ih * 0.18), ih * 0.12, 0.003, M["paper"], rot=(90, 0, 0), n=12)
        for k in range(3):
            gd.box((x - iw * 0.3 + k * 0.08 * w, y - 0.038, z - ih * 0.12), (iw * 0.25, 0.003, ih * 0.06), M["books"][0], rot=(0, r.uniform(-25, 25), 0))
    else:
        for k in range(4):
            gd.box((x + r.uniform(-iw, iw) * 0.3, y - 0.036 - k * 0.001, z + r.uniform(-ih, ih) * 0.3),
                   (iw * r.uniform(0.2, 0.5), 0.003, ih * r.uniform(0.2, 0.5)), M["books"][r.randrange(6)])


def standing_frame(g, gd, c, w, h, M, yaw=0.0, art=True, m=None):
    """A photo frame standing on a table, leaning back on its strut."""
    L = Loc(g, c, yaw)
    L.box((0, 0, h / 2 + 0.005), (w, 0.015, h), m or M["wood_furn"], rot=(-10, 0, 0))
    L.box((0, 0.05, h * 0.4), (0.02, 0.012, h * 0.8), m or M["wood_furn"], rot=(25, 0, 0))
    if art:
        Ld = Loc(gd, c, yaw)
        Ld.box((0, -0.011, h / 2 + 0.006), (w * 0.8, 0.003, h * 0.8), M["paper"], rot=(-10, 0, 0))
        Ld.box((0, -0.0125, h / 2 - 0.01), (w * 0.5, 0.003, h * 0.45), M["books"][3], rot=(-10, 0, 0))


def books_row(g, x0, x1, y, z, depth, M, r, lean=True):
    """A shelf's worth of books, each its own height, thickness and grey; now and then a gap, a book
    leaning into it, a little stack lying flat."""
    x = x0
    while x < x1 - 0.03:
        if r.random() < 0.07 and x1 - x > 0.25:
            for k in range(r.randint(2, 4)):                                              # a stack lying flat
                t = r.uniform(0.025, 0.045)
                g.box((x + 0.11, y, z + t / 2 + k * t), (0.2, depth * 0.85, t), M["books"][r.randrange(6)], rot=(0, 0, r.uniform(-6, 6)))
            x += 0.24
            continue
        if lean and r.random() < 0.05 and x1 - x > 0.2:
            h = r.uniform(0.18, 0.24)
            g.box((x + 0.07, y, z + h * 0.47), (0.035, depth * 0.85, h), M["books"][r.randrange(6)], rot=(0, 18, 0))
            x += 0.15
            continue
        t = r.uniform(0.022, 0.05)
        h = r.uniform(0.16, 0.27)
        if x + t > x1:
            break
        m = M["books"][r.randrange(6)]
        g.box((x + t / 2, y + r.uniform(-0.015, 0.015), z + h / 2), (t, depth * r.uniform(0.78, 0.95), h), m)
        if r.random() < 0.35:                                                             # a spine band
            g.box((x + t / 2, y - depth * 0.44, z + h * 0.8), (t * 1.02, 0.004, 0.012), M["paper"])
        x += t + 0.002


def shoe(g, x, y, z, M, yaw=0.0, L=0.27, m=None):
    lo = Loc(g, (x, y, z), yaw)
    m = m or M["ink"]
    lo.box((0, 0, 0.012), (L, L * 0.36, 0.024), M["plastic_d"])
    lo.box((-L * 0.12, 0, 0.05), (L * 0.72, L * 0.32, L * 0.24), m)
    lo.cyl((L * 0.3, 0, 0.035), L * 0.16, L * 0.32, m, rot=(90, 0, 0), n=8)


def mug(g, c, M, m=None, yaw=0.0):
    L = Loc(g, c, yaw)
    L.cyl((0, 0, 0.048), 0.04, 0.095, m or M["ceramic"], n=10)
    L.box((0.05, 0, 0.05), (0.02, 0.012, 0.06), m or M["ceramic"])


def plant(gs, gd, x, y, z, M, h=0.9, pot=0.14, seed=0):
    r = random.Random(seed)
    gs.cyl((x, y, z + pot * 1.1 / 2), pot, pot * 1.1, M["ceramic"], n=12, r2=pot * 0.8)
    gd.cyl((x, y, z + pot * 1.1), pot * 0.92, 0.01, M["soil"], n=12)
    for k in range(11):
        a = k * 137.5
        tilt = r.uniform(15, 50)
        ln = h * r.uniform(0.5, 0.95)
        rad = math.radians(a)
        cx = x + math.cos(rad) * math.sin(math.radians(tilt)) * ln / 2
        cy = y + math.sin(rad) * math.sin(math.radians(tilt)) * ln / 2
        gd.box((cx, cy, z + pot * 1.1 + math.cos(math.radians(tilt)) * ln / 2), (0.07, 0.008, ln), M["leaf"], rot=(0, tilt, a + 90))


def door_leaf(g, gd, hinge, ang, M, w=1.22, h=2.16, z0=0.0, glazed=False, m=None, t=0.045):
    """A panelled door on its hinge, opened to angle ang (degrees: the direction the leaf points)."""
    L = Loc(g, hinge, ang)
    Ld = Loc(gd, hinge, ang)
    m = m or M["white_paint"]
    L.box((w / 2, 0, z0 + h / 2), (w, t, h), m)
    for s in (-1, 1):                                                                     # raised panels, both faces
        for (pz, ph) in ((0.32, 0.5), (0.95, 0.62)) if glazed else ((0.3, 0.42), (0.75, 0.42), (1.2, 0.42), (1.65, 0.42)):
            for px in (0.27, 0.73) if not glazed or pz < 0.9 else ():
                Ld.box((w * px, s * (t / 2 + 0.006), z0 + h * pz), (w * 0.36, 0.012, h * ph * 0.42 if not glazed else h * 0.2), m)
        if glazed:
            Ld.box((w / 2, s * (t / 2 + 0.004), z0 + h * 0.72), (w * 0.72, 0.008, h * 0.42), M["glass"])
            Ld.box((w / 2, s * (t / 2 + 0.008), z0 + h * 0.72), (0.03, 0.01, h * 0.42), m)
        Ld.cyl((w - 0.08, s * (t / 2 + 0.03), z0 + 1.0), 0.008, 0.06, M["brass"], rot=(90, 0, 0), n=6)
        Ld.box((w - 0.08 - 0.05, s * (t / 2 + 0.06), z0 + 1.0), (0.12, 0.018, 0.018), M["brass"])
        Ld.box((w - 0.08, s * (t / 2 + 0.004), z0 + 1.0), (0.04, 0.008, 0.18), M["brass"])
    for k in (0.15, 0.85):                                                                # hinges
        Ld.box((0.0, 0, z0 + h * k), (0.02, t + 0.01, 0.09), M["brass"])


def architrave(g, x, z0, M, dw=1.3, dh=2.2, faces=(-1, 1), t=0.14, y0=0.0):
    """The moulded frame round a doorway in a cross wall at x (both faces)."""
    for s in faces:
        fx = x + s * (t / 2 + 0.012)
        for sy in (-1, 1):
            g.box((fx, y0 + sy * (dw / 2 + 0.045), z0 + (dh + 0.09) / 2), (0.024, 0.09, dh + 0.09), M["white_paint"])
        g.box((fx, y0, z0 + dh + 0.045), (0.024, dw + 0.18, 0.09), M["white_paint"])
    g.box((x, y0, z0 + dh - 0.012), (t + 0.02, dw, 0.024), M["white_paint"])                # the lining
    for sy in (-1, 1):
        g.box((x, y0 + sy * (dw / 2 - 0.012), z0 + dh / 2), (t + 0.02, 0.024, dh), M["white_paint"])


def wall_with_door(g, x, z0, z1, m, door_w=1.3, door_h=2.2, y0=YF, y1=YB):
    """A cross wall at x from the front to the back, a doorway in the lane (y ~ 0)."""
    t = 0.14
    g.box((x, (y0 + (-door_w / 2)) / 2, (z0 + z1) / 2), (t, (-door_w / 2) - y0, z1 - z0), m)
    g.box((x, ((door_w / 2) + y1) / 2, (z0 + z1) / 2), (t, y1 - door_w / 2, z1 - z0), m)
    g.box((x, 0.0, (z0 + door_h + z1) / 2), (t, door_w, z1 - z0 - door_h), m)


# ---------------------------------------------------------------- furniture

def chair(g, c, yaw, M, m=None, back=True):
    """A plain wooden kitchen chair; its back on local +x (sitting faces -x)."""
    L = Loc(g, c, yaw)
    m = m or M["oak"]
    L.box((0, 0, 0.44), (0.44, 0.42, 0.04), m)
    for dx in (-0.19, 0.19):
        for dy in (-0.18, 0.18):
            L.box((dx, dy, 0.21), (0.035, 0.035, 0.42), m)
    for dy in (-0.18, 0.18):
        L.box((0, dy, 0.12), (0.38, 0.02, 0.02), m)
    if back:
        for dy in (-0.18, 0.18):
            L.box((0.2, dy, 0.7), (0.035, 0.035, 0.52), m, rot=(0, -5, 0))
        L.box((0.215, 0, 0.92), (0.04, 0.42, 0.08), m, rot=(0, -5, 0))
        for dy in (-0.09, 0.0, 0.09):
            L.box((0.205, dy, 0.7), (0.02, 0.025, 0.36), m, rot=(0, -5, 0))
        L.box((0.2, 0, 0.53), (0.035, 0.4, 0.035), m, rot=(0, -5, 0))


def sofa(gs, gd, x, y, M, w=2.2):
    L = Loc(gs, (x, y, 0))
    fab = M["fabric"]
    L.box((0, 0, 0.24), (w, 0.88, 0.3), fab)                                               # base
    L.box((0, 0.36, 0.62), (w - 0.1, 0.18, 0.62), fab)                                    # back
    for dx in (-w / 2 + 0.1, w / 2 - 0.1):                                                # rolled arms
        L.box((dx, 0.0, 0.42), (0.2, 0.88, 0.36), fab)
        L.cyl((dx, 0.0, 0.62), 0.11, 0.88, fab, rot=(90, 0, 0), n=10)
    for dx in (-w / 4 + 0.05, w / 4 - 0.05):                                              # seat and back cushions
        L.box((dx, -0.06, 0.45), (w / 2 - 0.22, 0.7, 0.14), fab)
        L.box((dx, 0.22, 0.72), (w / 2 - 0.24, 0.16, 0.48), fab, rot=(-8, 0, 0))
    for dx in (-w / 2 + 0.12, w / 2 - 0.12):                                              # little legs
        for dy in (-0.36, 0.36):
            gd.cyl((x + dx, y + dy, 0.045), 0.025, 0.09, M["wood_furn"], n=8)
    Ld = Loc(gd, (x, y, 0))
    for dx, ang, m in ((-0.75, 12, M["fabric_pale"]), (-0.45, -8, M["curtain"]), (0.72, -14, M["fabric_pale"])):
        Ld.box((dx, 0.1, 0.68), (0.38, 0.12, 0.36), m, rot=(-12, ang, 0))
    Ld.box((w / 2 - 0.1, -0.05, 0.75), (0.26, 0.92, 0.02), M["curtain"], rot=(0, 0, 0))   # a throw over the arm
    Ld.box((w / 2 - 0.1, -0.5, 0.5), (0.26, 0.02, 0.5), M["curtain"])


def armchair(gs, gd, c, yaw, M):
    L = Loc(gs, c, yaw)
    fab = M["fabric"]
    L.box((0, 0, 0.24), (0.8, 0.8, 0.3), fab)
    L.box((0, 0.32, 0.65), (0.78, 0.16, 0.68), fab)
    for dx in (-0.34, 0.34):
        L.box((dx, -0.02, 0.48), (0.14, 0.76, 0.3), fab)
    L.box((0, -0.06, 0.45), (0.54, 0.64, 0.13), fab)
    Loc(gd, c, yaw).box((0.0, 0.18, 0.7), (0.34, 0.12, 0.32), M["fabric_pale"], rot=(-14, 6, 0))
    for dx in (-0.34, 0.34):
        for dy in (-0.34, 0.34):
            Loc(gd, c, yaw).cyl((dx, dy, 0.045), 0.025, 0.09, M["wood_furn"], n=8)


def drawers(gs, gd, x, y, z, w, d, h, M, rows=3, m=None, knobs=True, cols=1):
    """A chest of drawers against the back wall: carcass, drawer fronts, knobs."""
    m = m or M["wood_furn"]
    gs.box((x, y, z + h / 2), (w, d, h), m)
    gs.box((x, y - 0.01, z + h + 0.012), (w + 0.03, d + 0.03, 0.024), m)
    fy = y - d / 2 - 0.006
    dh = (h - 0.08) / rows
    for r in range(rows):
        for c in range(cols):
            cw = (w - 0.06) / cols
            cx = x - w / 2 + 0.03 + cw * (c + 0.5)
            cz = z + 0.06 + dh * (r + 0.5)
            gd.box((cx, fy, cz), (cw - 0.02, 0.012, dh - 0.02), m)
            if knobs:
                gd.cyl((cx, fy - 0.015, cz), 0.014, 0.02, M["brass"], rot=(90, 0, 0), n=8)


def bed_double(gs, gd, x, M):
    """The double bed, its padded headboard against the back wall, made, with its pillows and a throw."""
    z = F
    y0, y1 = 2.0, WALL - 0.04                    # foot, head
    yc = (y0 + y1) / 2
    ln = y1 - y0
    gs.box((x, yc, z + 0.2), (1.8, ln, 0.22), M["wood_furn"])                               # base
    for dx in (-0.82, 0.82):
        gd.box((x + dx, y0 + 0.08, z + 0.05), (0.07, 0.07, 0.1), M["wood_furn"])
    gs.box((x, yc - 0.02, z + 0.42), (1.74, ln - 0.08, 0.22), M["fabric_pale"])            # mattress
    for k in range(3):                                                                    # the padded headboard
        gs.box((x - 0.62 + k * 0.62, y1 - 0.04, z + 0.85), (0.58, 0.08, 0.9), M["fabric"])
    gs.box((x, y1 - 0.03, z + 1.32), (1.9, 0.1, 0.06), M["fabric"])
    for dx in (-0.42, 0.42):                                                              # pillows
        gs.box((x + dx, y1 - 0.32, z + 0.6), (0.72, 0.42, 0.16), M["ceramic"], rot=(-14, 0, 0))
    gd.box((x - 0.15, y1 - 0.55, z + 0.62), (0.4, 0.14, 0.32), M["curtain"], rot=(-20, 0, 5))
    gs.box((x, (y0 + y1 - 0.7) / 2 - 0.02, z + 0.56), (1.86, ln - 0.7, 0.06), M["fabric_pale"])   # the duvet
    gs.box((x, y1 - 0.72, z + 0.58), (1.86, 0.16, 0.08), M["fabric_pale"])                  # folded back
    for s in (-1, 1):
        gs.box((x + s * 0.94, (y0 + y1 - 0.7) / 2, z + 0.4), (0.04, ln - 0.7, 0.36), M["fabric_pale"])
    gs.box((x, y0 + 0.02, z + 0.4), (1.86, 0.04, 0.36), M["fabric_pale"])
    gd.box((x, y0 + 0.32, z + 0.6), (1.9, 0.5, 0.03), M["curtain"])                        # the throw at the foot
    gd.box((x, y0 - 0.01, z + 0.47), (1.9, 0.02, 0.26), M["curtain"])


def bed_kid(gs, gd, x, y, M):
    """Maya's bed along the back wall, torn up: the duvet half off, the pillow on the floor."""
    z = F
    l, w = 1.7, 1.0
    gs.box((x, y, z + 0.2), (l, w, 0.2), M["oak"])
    gs.box((x, y, z + 0.37), (l - 0.08, w - 0.08, 0.15), M["fabric_pale"])
    hx = x - l / 2 + 0.03
    gs.box((hx, y, z + 0.45), (0.06, w + 0.04, 0.9), M["oak"])                                 # headboard, a round top
    gs.cyl((hx, y, z + 0.9), w / 2 + 0.02, 0.06, M["oak"], rot=(0, 90, 0), n=16)
    gs.box((x + l / 2 - 0.03, y, z + 0.35), (0.06, w + 0.04, 0.7), M["oak"])
    for k in range(5):                                                                    # cut-out stars on the footboard
        gd.cyl((x + l / 2 - 0.065, y - 0.36 + k * 0.18, z + 0.5), 0.035, 0.01, M["white_paint"], rot=(0, 90, 0), n=5)
    gs.box((x + 0.3, y - 0.2, z + 0.47), (1.0, 0.8, 0.05), M["duvet_kid"], rot=(8, 0, 14))      # the duvet, dragged
    gs.box((x + 0.45, y - 0.64, z + 0.24), (0.9, 0.04, 0.42), M["duvet_kid"], rot=(0, 0, 14))
    sheet(gs, x - 0.3, x + 0.6, y - 1.25, y - 0.6, z + 0.01, M["duvet_kid"], res=0.1, amp=0.05, seed=4.0)
    gs.box((hx + 0.35, y + 0.2, z + 0.5), (0.36, 0.5, 0.1), M["ceramic"], rot=(0, 0, 20))
    gs.box((x - 0.95, y - 1.0, z + 0.06), (0.5, 0.34, 0.1), M["ceramic"], rot=(0, 0, 35))     # the pillow on the floor
    # her teddy, face down at the foot of the bed
    T = Loc(gd, (x + 0.5, y + 0.2, z + 0.5), 30)
    T.ball((0, 0, 0.06), 0.09, M["toy"][3], n=8, squash=(1.2, 0.9, 0.7))
    T.ball((0.15, 0, 0.06), 0.065, M["toy"][3], n=8)
    for dx, dy in ((0.16, 0.05), (0.16, -0.05)):
        T.ball((dx + 0.02, dy, 0.12), 0.025, M["toy"][3], n=6)
    for dx, dy in ((-0.05, 0.09), (-0.05, -0.09), (0.08, 0.09), (0.08, -0.09)):
        T.ball((dx, dy, 0.03), 0.035, M["toy"][3], n=6)


def rocking_horse(gs, gd, x, y, M):
    z = F
    L = Loc(gs, (x, y, z), 8)
    for s in (-1, 1):                                                                     # the bow rockers
        prev = None
        for k in range(7):
            a = math.radians(-36 + k * 12)
            p = (math.sin(a) * 0.7, s * 0.14, 0.72 - math.cos(a) * 0.7 + 0.02)
            if prev:
                L.beam(prev, p, 0.04, 0.03, M["oak"])
            prev = p
        for dx in (-0.22, 0.22):
            L.beam((dx, s * 0.14, 0.05), (dx * 0.6, s * 0.06, 0.38), 0.035, 0.035, M["oak"])
    L.box((0, 0, 0.45), (0.5, 0.18, 0.18), M["fabric_pale"])                               # body
    L.box((0.3, 0, 0.62), (0.16, 0.13, 0.3), M["fabric_pale"], rot=(0, -25, 0))           # neck
    L.box((0.38, 0, 0.74), (0.22, 0.11, 0.12), M["fabric_pale"], rot=(0, 18, 0))          # head
    L.box((0.24, 0, 0.7), (0.08, 0.05, 0.26), M["ink"], rot=(0, -25, 0))                  # mane
    L.box((-0.29, 0, 0.42), (0.06, 0.05, 0.22), M["ink"], rot=(0, 30, 0))                 # tail
    L.box((0.0, 0, 0.555), (0.18, 0.2, 0.03), M["fabric"])                                # saddle
    Loc(gd, (x, y, z), 8).cyl((0.36, 0, 0.72), 0.012, 0.3, M["chrome"], rot=(90, 0, 0), n=6)


def wheelie_bin(g, x, y, M, yaw=0.0):
    L = Loc(g, (x, y, 0), yaw)
    L.box((0, 0, 0.5), (0.58, 0.72, 0.96), M["plastic_d"])
    L.box((0, -0.02, 1.0), (0.62, 0.8, 0.05), M["plastic_d"])
    L.box((0, 0.4, 0.95), (0.5, 0.06, 0.05), M["plastic_d"])
    for dx in (-0.25, 0.25):
        L.cyl((dx, 0.34, 0.1), 0.1, 0.05, M["tyre"], rot=(0, 90, 0), n=10)


def tree(g, x, y, M, h=6.0, r=1.8, seed=0):
    rr = random.Random(seed)
    g.cyl((x, y, h * 0.3), 0.16, h * 0.6, M["bark"], n=8, r2=0.1)
    for k in range(3):
        a = rr.uniform(0, math.tau)
        g.beam((x, y, h * 0.4), (x + math.cos(a) * r * 0.6, y + math.sin(a) * r * 0.6, h * 0.7), 0.08, 0.08, M["bark"])
    for k in range(5):
        D.boulder(g, (x + rr.uniform(-r, r) * 0.55, y + rr.uniform(-r, r) * 0.55, h * rr.uniform(0.62, 0.85)), r * rr.uniform(0.55, 0.8), M["leaf"], squash=0.8)


def lamp_post(g, x, y, M, toward=-1):
    g.cyl((x, y, 0.3), 0.13, 0.6, M["steel"], n=10, r2=0.08)
    g.cyl((x, y, 2.9), 0.065, 5.2, M["steel"], n=10, r2=0.045)
    g.beam((x, y, 5.3), (x, y + toward * 0.7, 5.45), 0.05, 0.05, M["steel"])
    g.box((x, y + toward * 0.82, 5.42), (0.28, 0.42, 0.12), M["steel_mid"])
    g.box((x, y + toward * 0.82, 5.35), (0.22, 0.34, 0.03), M["lantern"])
    g.cyl((x, y, 0.75), 0.075, 0.12, M["steel_mid"], n=10)


def house(g, x0, x1, yf, depth, M, seed, facing=-1, door=0.3, chimney=True):
    """A neighbour's house: brick, a pitched roof, gable ends, chimneys, windows with frames (some
    lit, curtains half drawn), a front door with a canopy and step, a gutter and downpipe."""
    r = random.Random(seed)
    yb = yf + depth
    yc = (yf + yb) / 2
    w = x1 - x0
    cx = (x0 + x1) / 2
    hh = 5.6
    g.box((cx, yc, hh / 2), (w, depth, hh), M["brick_n"])
    a = 35.0
    hd = depth / 2 + 0.35
    sl = hd / math.cos(math.radians(a))
    rise = hd * math.tan(math.radians(a))
    for side in (-1, 1):
        g.box((cx, yc + side * hd / 2, hh + rise / 2 - 0.15), (w + 0.4, sl, 0.14), M["roof"], rot=(-side * a, 0, 0))
    for gx in (x0, x1):
        prism_x(g, [(yf, hh), (yb, hh), (yc, hh + depth / 2 * math.tan(math.radians(a)))], gx - 0.1, gx + 0.1, M["brick_ny"])
    g.cyl((cx, yc, hh + rise - 0.1), 0.09, w + 0.4, M["roof"], rot=(0, 90, 0), n=8)
    if chimney:
        for chx in (x0 + 0.8, x1 - 0.8):
            g.box((chx, yc, hh + rise + 0.4), (0.7, 0.5, 1.4), M["brick_n"])
            g.box((chx, yc, hh + rise + 1.12), (0.8, 0.6, 0.08), M["concrete"])
            for k in (-0.15, 0.15):
                g.cyl((chx + k, yc, hh + rise + 1.3), 0.08, 0.3, M["roof"], n=8)
    fy = yf - 0.02 if facing < 0 else yb + 0.02
    fs = facing
    g.box((cx, fy + fs * 0.08, hh - 0.05), (w + 0.4, 0.14, 0.1), M["plastic_d"])          # gutter
    g.cyl((x1 - 0.3, fy + fs * 0.08, hh / 2), 0.04, hh, M["plastic_d"], n=6)                # downpipe
    cols = max(2, int(w / 2.8))
    dc = int(door * cols)
    for k in range(cols):
        wx = x0 + (k + 0.5) * w / cols
        for z, wh in ((1.55, 1.3), (4.2, 1.2)):
            if k == dc and z < 2:
                g.box((wx, fy, 1.05), (1.0, 0.06, 2.1), M["wood_furn"])                     # the front door
                g.box((wx, fy + fs * 0.04, 1.05), (1.14, 0.04, 2.2), M["white_paint"])
                g.box((wx, fy + fs * 0.01, 1.05), (0.98, 0.04, 2.08), M["ink"] if r.random() < 0.5 else M["fabric"])
                g.box((wx, fy + fs * 0.06, 1.85), (0.7, 0.03, 0.3), M["win_lit"] if r.random() < 0.6 else M["glass"])
                g.box((wx, fy + fs * 0.5, 2.4), (1.5, 0.9, 0.08), M["roof"])
                g.box((wx, fy + fs * 0.3, 0.06), (1.4, 0.6, 0.12), M["concrete"])
                g.cyl((wx + 0.32, fy + fs * 0.08, 1.0), 0.03, 0.04, M["brass"], rot=(90, 0, 0), n=6)
                g.box((wx - 0.75, fy + fs * 0.07, 2.1), (0.12, 0.08, 0.18), M["lantern"])
                continue
            lit = r.random() < 0.35
            g.box((wx, fy, z), (1.2, 0.04, wh), M["win_lit"] if lit else M["glass"])
            g.box((wx, fy + fs * 0.03, z + wh / 2 + 0.04), (1.32, 0.06, 0.08), M["white_paint"])
            g.box((wx, fy + fs * 0.03, z - wh / 2 - 0.04), (1.32, 0.06, 0.08), M["white_paint"])
            for s in (-1, 0, 1):
                g.box((wx + s * 0.62, fy + fs * 0.03, z), (0.06 if s else 0.04, 0.06, wh), M["white_paint"])
            g.box((wx, fy + fs * 0.08, z - wh / 2 - 0.1), (1.4, 0.16, 0.06), M["concrete"])
            g.box((wx, fy + fs * 0.04, z + wh / 2 + 0.16), (1.4, 0.1, 0.16), M["brick_n"])       # lintel
            if r.random() < 0.7:                                                             # curtains inside
                cw = r.uniform(0.15, 0.4)
                for s in (-1, 1):
                    g.box((wx + s * (0.6 - cw / 2), fy - fs * 0.04, z), (cw, 0.02, wh - 0.05), M["curtain"])


# ---------------------------------------------------------------- the house

def structure(gs, gd, M):
    # ---- floors and ceilings
    gs.box(((X0 + X1) / 2, (YF + YB) / 2, -0.05), (X1 - X0, YB - YF, 0.1), M["boards"])
    gs.box((29.5, (YF + YB) / 2, 0.003), (9.0, YB - YF, 0.006), M["kitchen_floor"])
    gs.box((1.55, (YF + YB) / 2, 0.003), (2.9, YB - YF, 0.006), M["hall_tiles"])
    gs.box(((X0 + X1) / 2, (YF + 2.4) / 2, FLOOR2 - 0.1), (X1 - X0, 2.4 - YF, 0.2), M["boards_up"])   # upstairs (open over the stairs)
    gs.box(((STAIR_X1 + 0.2 + X1) / 2, 3.3, FLOOR2 - 0.1), (X1 - STAIR_X1 - 0.2, 1.8, 0.2), M["boards_up"])
    gs.box(((X0 + X1) / 2, (YF + 2.4) / 2, H1 - 0.01), (X1 - X0 - 0.2, 2.4 - YF, 0.02), M["ceiling"])
    gs.box(((STAIR_X1 + 0.2 + X1) / 2, 3.3, H1 - 0.01), (X1 - STAIR_X1 - 0.4, 1.8, 0.02), M["ceiling"])
    gs.box(((X0 + X1) / 2, (YF + YB) / 2, H2 + 0.05), (X1 - X0, YB - YF, 0.1), M["ceiling"])
    gs.box((4.8, 2.41, FLOOR2 - 0.1), (9.6, 0.03, 0.22), M["white_paint"])                  # the fascia over the stairwell
    gs.box((9.61, 3.3, FLOOR2 - 0.1), (0.03, 1.8, 0.22), M["white_paint"])
    # ---- the back wall: plaster inside, brick outside, the papered rooms
    gs.box(((X0 + X1) / 2, YB + 0.1, H2 / 2), (X1 - X0, 0.2, H2), M["plaster"])
    gs.box(((X0 + X1) / 2, YB + 0.22, H2 / 2), (X1 - X0 + 0.4, 0.04, H2), M["brick"])
    holes1 = [win_hole(w) for w in WIN1]
    holes2 = [win_hole(w) for w in WIN2]
    paper_wall(gs, 0.1, 9.0, 0.0, H1, M["paper_hall"], [(0.1, 4.2, 0.0, 0.95)])
    paper_wall(gs, 0.1, 4.2, 0.0, 0.95, M["wainscot"])
    gs.box((2.15, WALL - 0.02, 0.96), (4.1, 0.04, 0.04), M["white_paint"])                  # dado rail
    paper_wall(gs, 0.1, 9.6, H1, H2, M["paper_hall"])                                      # the stairwell, up to the roof
    paper_wall(gs, 9.07, 14.93, 0.0, H1, M["paper_study"], holes1)
    paper_wall(gs, 15.07, 24.93, 0.0, H1, M["paper_living"], holes1)
    splash = (25.5, 32.5, 0.94, 1.6)
    paper_wall(gs, 25.07, 33.9, 0.0, H1, M["paper_kitchen"], holes1 + [splash])
    paper_wall(gs, 25.5, 32.5, 0.94, 1.6, M["splash"], holes1)
    paper_wall(gs, 9.6, 13.93, F, H2, M["paper_hall"], holes2)
    paper_wall(gs, 14.07, 21.93, F, H2, M["paper_maya"], holes2)
    paper_wall(gs, 22.07, 33.9, F, H2, M["paper_bed"], holes2)
    # outside: the back windows' glass and frames, a gutter
    for (x, z0, w, h) in WIN1 + WIN2:
        gs.box((x, YB + 0.25, z0 + h / 2), (w, 0.03, h), M["glass"])
        gs.box((x, YB + 0.26, z0 - 0.05), (w + 0.2, 0.12, 0.06), M["concrete"])
        for s in (-1, 1):
            gs.box((x + s * w / 2, YB + 0.26, z0 + h / 2), (0.06, 0.04, h), M["white_paint"])
    gs.box(((X0 + X1) / 2, YB + 0.36, H2 - 0.05), (X1 - X0 + 0.4, 0.14, 0.1), M["plastic_d"])
    # ---- the end walls, with the front door (x0) and the back door (x1) as real openings
    for x, s in ((X0, 1), (X1, -1)):
        for (ya, yb) in ((YF, -0.62), (0.62, YB)):
            gs.box((x, (ya + yb) / 2, H2 / 2), (0.2, yb - ya, H2), M["brick_y"])
            gs.box((x + s * 0.11, (ya + yb) / 2, H2 / 2), (0.02, yb - ya, H2), M["plaster_y"])
        gs.box((x, 0.0, (2.2 + H2) / 2), (0.2, 1.24, H2 - 2.2), M["brick_y"])
        gs.box((x + s * 0.11, 0.0, (2.2 + H2) / 2), (0.02, 1.24, H2 - 2.2), M["plaster_y"])
        architrave(gs, x, 0.0, M, dw=1.24, dh=2.2, t=0.22)
        gs.box((x, 0.0, 0.01), (0.3, 1.24, 0.02), M["concrete"])                           # threshold
    for x in (X0, X1):                                                                     # the gables
        prism_x(gs, [(YF - 0.05, H2), (YB + 0.25, H2), ((YF + YB) / 2 + 0.1, 8.3)], x - 0.1, x + 0.1, M["brick_y"])
    # ---- the roof (two slopes meeting at the ridge), its ridge, the chimneys, fascias
    for side in (-1, 1):
        gs.box(((X0 + X1) / 2, (YF + YB) / 2 + side * 1.6, H2 + 1.25), (X1 - X0 + 0.8, 3.6, 0.15), M["roof"], rot=(-side * 38, 0, 0))
    gs.cyl(((X0 + X1) / 2, (YF + YB) / 2 + 0.02, 8.36), 0.12, X1 - X0 + 0.8, M["roof"], rot=(0, 90, 0), n=8)
    for chx in (3.0, 16.6, 31.0):
        gs.box((chx, 2.6, 8.6), (0.9, 0.6, 2.4), M["brick"])
        gs.box((chx, 2.6, 9.85), (1.0, 0.7, 0.1), M["concrete"])
        for k in (-0.22, 0.0, 0.22):
            gs.cyl((chx + k, 2.6, 10.05), 0.08, 0.32, M["roof"], n=8, r2=0.06)
    for y in (YF - 0.75, YB + 0.95):
        gs.box(((X0 + X1) / 2, y, 6.05), (X1 - X0 + 0.8, 0.04, 0.22), M["white_paint"])
    # ---- cross walls with doorways, their frames, the doors (open, back against the wall)
    for x in DOORS1[1:]:
        wall_with_door(gs, x, 0.0, FLOOR2 - 0.2, M["plaster_y"])
        architrave(gs, x, 0.0, M)
        door_leaf(gs, gd, (x + 0.1, 0.66, 0.0), 86 + rnd.uniform(-4, 2), M, w=1.24, h=2.16)
    for x in DOORS2:
        wall_with_door(gs, x, FLOOR2, H2, M["plaster_y"])
        architrave(gs, x, F, M)
        door_leaf(gs, gd, (x + 0.1, 0.66, F), 84 + rnd.uniform(-4, 4), M, w=1.24, h=2.16)
    # ---- skirting, coving, switches and sockets
    for (a, b, z) in ((0.1, 33.9, 0.0), (9.6, 33.9, F)):
        gs.box(((a + b) / 2, WALL - 0.025, z + 0.075), (b - a, 0.05, 0.15), M["skirting"])
    for (a, b, z) in ((0.1, 33.9, H1), (0.1, 33.9, H2)):
        gs.box(((a + b) / 2, WALL - 0.04, z - 0.05), (b - a, 0.08, 0.1), M["white_paint"], rot=(0, 0, 0))
    for xs, z0 in ((DOORS1[1:], 0.0), (DOORS2, F)):
        for x in xs:
            for s in (-1, 1):
                for (ya, yb) in ((YF, -0.74), (0.74, WALL)):
                    gs.box((x + s * 0.095, (ya + yb) / 2, z0 + 0.075), (0.05, yb - ya, 0.15), M["skirting"])
            switch(gd, x - 0.071, 0.92, z0 + 1.2, M, face="x-")
            switch(gd, x + 0.071, -0.92, z0 + 1.2, M, face="x+")
    for x, s in ((X0 + 0.12, 1), (X1 - 0.12, -1)):
        for z0 in (0.0, F):
            for (ya, yb) in ((YF, -0.74), (0.74, WALL)):
                gs.box((x + s * 0.025, (ya + yb) / 2, z0 + 0.075), (0.05, yb - ya, 0.15), M["skirting"])
    switch(gd, X0 + 0.121, 0.92, 1.2, M, face="x+")
    switch(gd, X1 - 0.121, -0.92, 1.2, M, face="x-")
    for x in (2.0, 13.2, 16.2, 24.4, 25.8, 33.0):
        socket(gd, x, 0.3, M)
    for x in (11.7, 15.6, 21.2, 24.0, 26.0, 33.0):
        socket(gd, x, F + 0.3, M)
    for x in (26.2, 31.0):
        socket(gd, x, 1.15, M, y=WALL - 0.02)


def stairs(gs, gd, M):
    """Up the back wall from the hall to the landing (the game walks him up them): treads with a
    carpet runner held by rods, nosings, the stringer, a baluster on every step, the handrail and
    newels, the panelled side with a cupboard under it, a rail on the wall."""
    n = 16
    run = (STAIR_X1 - STAIR_X0) / n
    for i in range(n):
        x = STAIR_X0 + run * i
        z = FLOOR2 * (i + 1) / n
        gs.box((x + 0.16, 3.3, z / 2), (run, 1.4, z), M["boards"])
        gs.box((x - 0.012, 3.3, z - 0.016), (0.05, 1.42, 0.032), M["white_paint"])           # nosing
        gd.box((x + 0.16, 3.3, z + 0.005), (run, 0.86, 0.01), M["runner"])                  # the runner
        gd.box((x - 0.04, 3.3, z - 0.1), (0.01, 0.86, 0.2), M["runner_x"])
        gd.cyl((x - 0.05, 3.3, z - 0.18), 0.008, 0.92, M["brass"], rot=(90, 0, 0), n=6)
    rail = lambda x: 0.95 + (x - STAIR_X0) * (3.15 / (STAIR_X1 - STAIR_X0))
    for i in range(n):                                                                   # a baluster on every tread
        x = STAIR_X0 + run * i + 0.16
        z = FLOOR2 * (i + 1) / n
        gs.box((x, 2.66, (z + rail(x)) / 2), (0.034, 0.034, rail(x) - z), M["white_paint"])
    gs.beam((STAIR_X0, 2.66, 0.95), (STAIR_X1, 2.66, FLOOR2 + 0.9), 0.075, 0.07, M["wood_furn"])
    gs.beam((STAIR_X0 - 0.1, 2.58, 0.04), (STAIR_X1 + 0.05, 2.58, FLOOR2 + 0.06), 0.05, 0.28, M["white_paint"])   # stringer
    for (nx, nz0, nz1) in ((STAIR_X0 - 0.05, 0.0, 1.15), (STAIR_X1 + 0.12, F - 0.3, F + 1.05)):
        gs.box((nx, 2.66, (nz0 + nz1) / 2), (0.11, 0.11, nz1 - nz0), M["wood_furn"])
        gs.box((nx, 2.66, nz1 + 0.03), (0.15, 0.15, 0.05), M["wood_furn"])
        ball(gs, (nx, 2.66, nz1 + 0.1), 0.055, M["wood_furn"], n=8)
    # the side panel under the flight, and the cupboard door in it
    prism = [(STAIR_X0 + 0.15, 0.0), (STAIR_X1, 0.0), (STAIR_X1, FLOOR2 - 0.15)]
    tmp = bmesh.new()
    f0 = [tmp.verts.new((x, 2.585, z)) for x, z in prism]
    f1 = [tmp.verts.new((x, 2.6, z)) for x, z in prism]
    tmp.faces.new(f0)
    tmp.faces.new(list(reversed(f1)))
    for i in range(3):
        tmp.faces.new((f0[i], f0[(i + 1) % 3], f1[(i + 1) % 3], f1[i]))
    gs._merge(tmp, M["white_paint"])
    gs.box((8.15, 2.575, 0.8), (0.75, 0.02, 1.55), M["white_paint"])
    for z in (0.45, 1.15):
        gd.box((8.15, 2.563, z), (0.55, 0.008, 0.5), M["white_paint"])
    gd.cyl((7.88, 2.55, 0.85), 0.018, 0.03, M["brass"], rot=(90, 0, 0), n=8)
    # a rail on the wall side
    gd.beam((STAIR_X0 + 0.3, WALL - 0.07, 1.15), (STAIR_X1, WALL - 0.07, FLOOR2 + 1.05), 0.045, 0.045, M["wood_furn"])
    for k in range(4):
        x = STAIR_X0 + 0.6 + k * 1.4
        gd.box((x, WALL - 0.035, rail(x) + 0.12), (0.03, 0.07, 0.03), M["brass"])
    # the landing balustrade along the stairwell's edge
    gs.box((4.85, 2.36, F + 0.95), (9.5, 0.08, 0.06), M["wood_furn"])
    gs.box((4.85, 2.36, F + 0.05), (9.5, 0.06, 0.06), M["white_paint"])
    x = 0.25
    while x < 9.45:
        gs.box((x, 2.36, F + 0.5), (0.032, 0.032, 0.86), M["white_paint"])
        x += 0.14
    for nx in (0.15, 9.55):
        gs.box((nx, 2.36, F + 0.55), (0.1, 0.1, 1.1), M["wood_furn"])


def hall(gs, gd, M):
    r = random.Random(1)
    # inside the front door: the mat, the post on it
    gs.box((0.65, 0.0, 0.009), (0.6, 0.95, 0.012), M["coir"])
    for k in range(3):
        gd.box((0.5 + k * 0.12, -0.1 + k * 0.17, 0.018), (0.22, 0.11, 0.003), M["paper"], rot=(0, 0, r.uniform(-30, 30)))
    gs.box((5.9, 0.0, 0.008), (5.2, 0.9, 0.012), M["runner"])                               # the hall runner
    # the shoe rack and the shoes (hers, his, Maya's), the little wellies
    sx, sy = 0.6, WALL - 0.17
    for z in (0.02, 0.22, 0.42):
        gs.box((sx, sy, z), (0.8, 0.32, 0.025), M["oak_top"])
    for dx in (-0.39, 0.39):
        gs.box((sx + dx, sy, 0.22), (0.025, 0.32, 0.44), M["oak"])
    for k, (dx, z, L) in enumerate(((-0.25, 0.035, 0.28), (-0.13, 0.035, 0.28), (0.1, 0.035, 0.26), (0.22, 0.035, 0.26),
                                    (-0.25, 0.235, 0.17), (-0.15, 0.235, 0.17), (0.12, 0.235, 0.25), (0.24, 0.235, 0.25))):
        shoe(gd, sx + dx, sy, z, M, yaw=90 + r.uniform(-6, 6), L=L, m=M["ink"] if k % 3 else M["fabric"])
    for dx in (0.95, 1.05):
        gd.cyl((dx, 3.6, 0.13), 0.045, 0.26, M["toy"][1], n=10)
        gd.box((dx + 0.04, 3.6, 0.03), (0.1, 0.08, 0.06), M["toy"][1])
    shoe(gd, 1.3, 1.9, 0.0, M, yaw=30, L=0.17, m=M["toy"][3])
    shoe(gd, 1.5, 2.15, 0.0, M, yaw=-50, L=0.17, m=M["toy"][3])
    shoe(gd, 0.9, -1.4, 0.0, M, yaw=70, L=0.29)                                              # his, kicked off
    radiator_unit(gs, gd, 1.55, 0.14, 0.6, M, h=0.6)
    # the umbrella stand
    gs.cyl((3.65, WALL - 0.17, 0.25), 0.12, 0.5, M["ceramic"], n=14)
    for k, a in enumerate((8, -10)):
        gd.beam((3.65 + k * 0.05, WALL - 0.17, 0.1), (3.65 + k * 0.05 + math.sin(math.radians(a)) * 0.9, WALL - 0.2, 0.95), 0.03, 0.03, M["ink"])
    # the mirror over the hall table, a photo of the three of them beside it
    gs.box((2.6, WALL - 0.02, 1.55), (0.7, 0.04, 0.85), M["wood_furn"])
    gs.box((2.6, WALL - 0.045, 1.55), (0.58, 0.01, 0.73), M["mirror"])
    frame_pic(gs, gd, 3.55, 1.6, 0.3, 0.4, M, art="photo", seed=3)
    # coats on hooks above the bottom of the stairs (her small raincoat among them), a satchel
    gs.box((5.25, WALL - 0.015, 1.8), (1.4, 0.03, 0.12), M["oak"])
    for k in range(5):
        gd.beam((4.7 + k * 0.28, WALL - 0.03, 1.78), (4.7 + k * 0.28, WALL - 0.1, 1.83), 0.02, 0.02, M["brass"])
    gs.box((4.75, WALL - 0.12, 1.33), (0.42, 0.16, 0.85), M["fabric"])                      # his long coat
    gs.box((4.75, WALL - 0.12, 1.74), (0.2, 0.18, 0.08), M["fabric"])
    gs.box((5.1, WALL - 0.12, 1.42), (0.34, 0.12, 0.5), M["fabric_pale"])                   # her raincoat
    gs.cyl((5.1, WALL - 0.12, 1.72), 0.1, 0.14, M["fabric_pale"], n=10)
    for s in (-1, 1):
        gs.box((5.1 + s * 0.19, WALL - 0.12, 1.4), (0.08, 0.1, 0.38), M["fabric_pale"], rot=(0, s * 8, 0))
    gs.box((5.55, WALL - 0.13, 1.3), (0.4, 0.18, 0.95), M["curtain"])                       # Elena's coat
    gd.box((5.85, WALL - 0.1, 1.5), (0.3, 0.1, 0.24), M["fabric"])                          # a bag
    gd.beam((5.85, WALL - 0.1, 1.62), (5.82, WALL - 0.04, 1.8), 0.02, 0.01, M["fabric"])
    gd.box((5.35, WALL - 0.09, 1.7), (0.1, 0.06, 0.5), M["toy"][2])                         # a scarf
    # the gallery of photos climbing the stairwell wall
    for k, (x, z, w, h) in enumerate(((4.6, 2.35, 0.4, 0.5), (5.6, 2.6, 0.5, 0.38), (6.6, 3.15, 0.38, 0.5), (7.6, 3.6, 0.5, 0.38),
                                      (8.6, 4.2, 0.4, 0.5), (6.2, 4.3, 0.6, 0.45), (2.2, 3.6, 0.7, 0.5), (1.1, 3.75, 0.4, 0.55))):
        frame_pic(gs, gd, x, z, w, h, M, art="photo" if k % 3 else "print", seed=10 + k)
    # an intercom and the alarm panel by the door, a hall clock
    gd.box((X0 + 0.12, 1.2, 1.45), (0.03, 0.14, 0.2), M["plastic_w"])
    gs.cyl((7.2, WALL - 0.03, 2.4), 0.16, 0.05, M["wood_furn"], rot=(90, 0, 0), n=16)
    gd.cyl((7.2, WALL - 0.06, 2.4), 0.13, 0.01, M["paper"], rot=(90, 0, 0), n=16)
    gd.box((7.2, WALL - 0.07, 2.44), (0.012, 0.006, 0.09), M["ink"])
    gd.box((7.23, WALL - 0.07, 2.4), (0.07, 0.006, 0.012), M["ink"])
    pendant(gs, gd, 2.0, 0.9, H1, M, kind="globe")
    pendant(gs, gd, 6.5, 0.6, H2, M, kind="drum")


def study(gs, gd, M):
    r = random.Random(2)
    gs.box((12.0, 2.6, 0.007), (3.2, 2.6, 0.012), M["rug"])
    # the desk: a pedestal of drawers, a leg end, the modesty panel, a leather top
    gs.box((12.0, 3.4, 0.75), (1.8, 0.8, 0.05), M["wood_top"])
    gs.box((11.35, 3.4, 0.36), (0.48, 0.74, 0.72), M["wood_furn"])
    for k in range(3):
        gd.box((11.35, 3.025, 0.13 + k * 0.23), (0.42, 0.012, 0.2), M["wood_furn"])
        gd.box((11.35, 3.012, 0.17 + k * 0.23), (0.12, 0.014, 0.016), M["brass"])
    for dy in (-0.33, 0.33):
        gs.box((12.83, 3.4 + dy, 0.36), (0.06, 0.06, 0.72), M["wood_furn"])
    gs.box((12.1, 3.75, 0.45), (1.4, 0.03, 0.5), M["wood_furn"])
    gd.box((12.15, 3.32, 0.78), (0.62, 0.42, 0.006), M["fabric"])
    # on it: a laptop (shut), papers, a pen pot, a mug gone cold, books, the lamp (on)
    gd.box((12.25, 3.4, 0.79), (0.34, 0.24, 0.018), M["steel_mid"], rot=(0, 0, -6))
    for k in range(5):
        gd.box((12.6 + r.uniform(-0.02, 0.02), 3.2, 0.785 + k * 0.004), (0.21, 0.29, 0.003), M["paper"], rot=(0, 0, r.uniform(-12, 12)))
    gd.cyl((11.5, 3.62, 0.83), 0.04, 0.11, M["ceramic"], n=10)
    for k in range(4):
        gd.beam((11.5 + r.uniform(-0.02, 0.02), 3.62, 0.8), (11.5 + r.uniform(-0.04, 0.04), 3.62 + r.uniform(-0.03, 0.03), 0.95), 0.008, 0.008, M["books"][k])
    mug(gd, (12.75, 3.05, 0.775), M)
    for k in range(3):
        gd.box((11.3, 3.25, 0.79 + k * 0.035), (0.22, 0.16, 0.034), M["books"][k * 2], rot=(0, 0, r.uniform(-10, 10)))
    gs.cyl((12.7, 3.6, 0.79), 0.08, 0.025, M["brass"], n=12)                                 # the banker's lamp
    gs.cyl((12.7, 3.6, 0.9), 0.012, 0.22, M["brass"], n=6)
    gs.cyl((12.7, 3.6, 1.04), 0.12, 0.1, M["lamp"], n=12, r2=0.06)
    # his chair
    L = Loc(gs, (12.05, 2.72, 0.0), 8)
    for k in range(5):
        a = math.radians(k * 72)
        L.beam((0, 0, 0.07), (math.cos(a) * 0.3, math.sin(a) * 0.3, 0.05), 0.04, 0.03, M["ink"])
        L.ball((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.03), 0.03, M["ink"], n=6)
    L.cyl((0, 0, 0.25), 0.025, 0.36, M["chrome"], n=8)
    L.box((0, 0, 0.47), (0.5, 0.48, 0.08), M["fabric"])
    L.box((0, -0.25, 0.8), (0.46, 0.07, 0.56), M["fabric"], rot=(-8, 0, 0))
    for s in (-1, 1):
        L.box((s * 0.25, -0.02, 0.62), (0.04, 0.3, 0.03), M["ink"])
        L.box((s * 0.25, -0.1, 0.55), (0.03, 0.03, 0.14), M["ink"])
    # the bookcase, full
    bx, by, bw, bd = 14.12, WALL - 0.17, 1.4, 0.32
    gs.box((bx, WALL - 0.01, 1.1), (bw, 0.02, 2.2), M["wood_furn"])
    for s in (-1, 1):
        gs.box((bx + s * (bw / 2 - 0.015), by, 1.1), (0.03, bd, 2.2), M["wood_furn"])
    gs.box((bx, by, 2.21), (bw + 0.04, bd + 0.03, 0.04), M["wood_furn"])
    gs.box((bx, by - 0.01, 0.05), (bw, bd, 0.1), M["wood_furn"])
    for k in range(5):
        z = 0.1 + k * 0.42
        gs.box((bx, by, z), (bw - 0.06, bd, 0.025), M["wood_top"])
        books_row(gd, bx - bw / 2 + 0.04, bx + bw / 2 - 0.04, by, z + 0.013, 0.24, M, r)
    gs.cyl((bx - 0.35, by, 2.28), 0.06, 0.1, M["wood_furn"], n=10)                          # a globe on top
    ball(gs, (bx - 0.35, by, 2.47), 0.14, M["books"][2], n=12)
    gd.box((bx + 0.3, by, 2.3), (0.3, 0.24, 0.14), M["fabric"])                              # a box file
    # the wall: a certificate, a clock, a cork board with pinned notes
    frame_pic(gs, gd, 11.75, 1.75, 0.42, 0.3, M, art="print", seed=21)
    gs.box((12.75, WALL - 0.015, 1.68), (0.6, 0.03, 0.45), M["oak"])
    for k in range(6):
        gd.box((12.55 + (k % 3) * 0.19, WALL - 0.033, 1.58 + (k // 3) * 0.2), (0.13, 0.004, 0.11), M["paper"], rot=(0, r.uniform(-10, 10), 0))
        gd.cyl((12.55 + (k % 3) * 0.19, WALL - 0.038, 1.62 + (k // 3) * 0.2), 0.008, 0.01, M["toy"][k % 4], rot=(90, 0, 0), n=6)
    gd.box((12.75, WALL - 0.05, 2.25), (0.3, 0.02, 0.2), M["ink"])
    gd.box((12.75, WALL - 0.062, 2.25), (0.26, 0.004, 0.16), M["paper"])
    # under the desk: the waste bin and a crumpled sheet
    gs.cyl((13.08, 3.4, 0.16), 0.12, 0.32, M["steel_mid"], n=12, r2=0.1)
    ball(gd, (13.2, 2.95, 0.04), 0.04, M["paper"], n=6)
    window(gs, gd, *WIN1[0], M, curtains="curtain")
    plant(gs, gd, 9.45, 1.55, 0.0, M, h=0.9, seed=5)
    pendant(gs, gd, 12.0, 1.1, H1, M, kind="cone")


def living(gs, gd, M):
    r = random.Random(3)
    # the chimney breast and the fireplace: surround, mantel, hearth, grate and logs, tools
    fx = 16.6
    gs.box((fx, WALL - 0.17, H1 / 2), (1.6, 0.34, H1), M["paper_living"])
    fy = WALL - 0.34
    for s in (-1, 1):
        gs.box((fx + s * 0.62, fy - 0.05, 0.6), (0.18, 0.12, 1.2), M["white_paint"])
    gs.box((fx, fy - 0.05, 1.08), (1.42, 0.12, 0.24), M["white_paint"])
    gs.box((fx, fy - 0.1, 1.215), (1.7, 0.26, 0.05), M["white_paint"])
    gs.box((fx, fy - 0.01, 0.5), (0.95, 0.02, 0.9), M["ink"])
    gs.box((fx, fy - 0.3, 0.025), (1.6, 0.6, 0.05), M["worktop"])
    gs.box((fx, fy - 0.12, 0.17), (0.6, 0.2, 0.18), M["steel"])
    gd.box((fx, fy - 0.22, 0.21), (0.6, 0.02, 0.02), M["steel_mid"])
    for k in range(4):
        gd.cyl((fx - 0.2 + k * 0.13, fy - 0.12, 0.31), 0.045, 0.45, M["bark"], rot=(0, 90, 20 * (k - 1.5)), n=7)
    for k in range(3):
        gd.beam((fx + 0.92, fy - 0.2, 0.05), (fx + 0.9 + (k - 1) * 0.03, fy - 0.2 + (k - 1) * 0.02, 0.75), 0.015, 0.015, M["steel"])
    gd.cyl((fx + 0.92, fy - 0.2, 0.03), 0.08, 0.04, M["steel"], n=10)
    gs.cyl((fx - 0.95, fy - 0.15, 0.18), 0.2, 0.36, M["fabric"], n=12)                        # the log basket
    for k in range(3):
        gd.cyl((fx - 0.95, fy - 0.15 + (k - 1) * 0.08, 0.4), 0.04, 0.38, M["bark"], rot=(0, 90, 10 * k), n=7)
    # on the mantel: a clock, candlesticks, photos, a vase; above it, the mirror
    mz = 1.24
    gs.box((fx, fy - 0.08, mz + 0.11), (0.26, 0.1, 0.22), M["wood_furn"])
    gd.cyl((fx, fy - 0.14, mz + 0.12), 0.07, 0.01, M["paper"], rot=(90, 0, 0), n=14)
    for s in (-1, 1):
        gd.cyl((fx + s * 0.7, fy - 0.08, mz + 0.12), 0.025, 0.24, M["brass"], n=8)
        gd.cyl((fx + s * 0.7, fy - 0.08, mz + 0.3), 0.012, 0.12, M["paper"], n=6)
    standing_frame(gs, gd, (fx - 0.38, fy - 0.08, mz), 0.16, 0.2, M, yaw=8)
    standing_frame(gs, gd, (fx + 0.38, fy - 0.08, mz), 0.14, 0.18, M, yaw=-6, m=M["brass"])
    gd.cyl((fx + 0.55, fy - 0.1, mz + 0.1), 0.045, 0.2, M["ceramic"], n=10, r2=0.03)
    gs.box((fx, WALL - 0.36, 1.9), (0.9, 0.04, 0.75), M["brass"])
    gs.box((fx, WALL - 0.385, 1.9), (0.8, 0.01, 0.65), M["mirror"])
    # the sofa, its cushions and throw; pictures over it; a side table with a lamp
    sofa(gs, gd, 19.0, 3.3, M)
    frame_pic(gs, gd, 19.0, 1.9, 1.0, 0.7, M, art="print", seed=31)
    frame_pic(gs, gd, 20.35, 1.85, 0.42, 0.55, M, art="photo", seed=32)
    gs.box((20.42, 3.75, 0.27), (0.36, 0.36, 0.03), M["wood_top"])
    gs.box((20.42, 3.75, 0.52), (0.38, 0.38, 0.03), M["wood_top"])
    for dx in (-0.16, 0.16):
        for dy in (-0.16, 0.16):
            gs.box((20.42 + dx, 3.75 + dy, 0.26), (0.03, 0.03, 0.52), M["wood_furn"])
    gs.cyl((20.42, 3.8, 0.62), 0.07, 0.16, M["ceramic"], n=12)
    gs.cyl((20.42, 3.8, 0.8), 0.13, 0.18, M["fabric_pale"], n=12, r2=0.1)
    mug(gd, (20.38, 3.62, 0.535), M, m=M["books"][1])
    # the TV on its unit: the screen, the soundbar, a console, the shelves below, a plant
    tx = 23.15
    gs.box((tx, WALL - 0.21, 0.27), (1.35, 0.4, 0.5), M["wood_furn"])
    gs.box((tx, WALL - 0.21, 0.53), (1.4, 0.42, 0.03), M["wood_top"])
    for k in (-0.34, 0.34):
        gd.box((tx + k, WALL - 0.41, 0.3), (0.6, 0.012, 0.4), M["wood_furn"])
        gd.box((tx + k, WALL - 0.42, 0.42), (0.12, 0.012, 0.012), M["brass"])
    gs.box((tx, WALL - 0.22, 0.56), (0.32, 0.2, 0.02), M["ink"])
    gs.box((tx, WALL - 0.2, 0.62), (0.05, 0.04, 0.12), M["ink"])
    gs.box((tx, WALL - 0.2, 0.98), (1.15, 0.04, 0.66), M["ink"])
    gd.box((tx, WALL - 0.225, 0.98), (1.1, 0.006, 0.61), M["screen"])
    gd.box((tx, WALL - 0.33, 0.585), (0.8, 0.08, 0.06), M["ink"])
    gd.box((tx + 0.5, WALL - 0.25, 0.575), (0.26, 0.18, 0.05), M["plastic_w"])
    plant(gs, gd, tx - 0.55, WALL - 0.25, 0.545, M, h=0.35, pot=0.06, seed=7)
    gs.box((tx, WALL - 0.1, 1.72), (1.1, 0.2, 0.03), M["wood_top"])                          # a shelf over it
    books_row(gd, tx - 0.5, tx + 0.15, WALL - 0.1, 1.735, 0.16, M, r, lean=False)
    standing_frame(gs, gd, (tx + 0.35, WALL - 0.1, 1.735), 0.16, 0.2, M)
    # the floor lamp (on), the rug, her toys
    gs.cyl((24.2, 3.4, 0.02), 0.16, 0.04, M["steel"], n=14)
    gs.cyl((24.2, 3.4, 0.8), 0.02, 1.6, M["steel"], n=6)
    gs.cyl((24.2, 3.4, 1.65), 0.2, 0.3, M["lamp"], n=14, r2=0.12)
    gs.box((20.0, 1.0, 0.008), (4.0, 3.2, 0.012), M["rug"])
    armchair(gs, gd, (15.85, 2.45, 0.0), 160, M)
    plant(gs, gd, 15.42, 3.6, 0.0, M, h=1.2, pot=0.16, seed=8)
    gs.box((17.65, 2.55, 0.15), (0.45, 0.32, 0.3), M["fabric_pale"])                          # a toy basket
    for k in range(4):
        ball(gd, (17.55 + k * 0.07, 2.55 + (k % 2) * 0.06, 0.33), 0.05, M["toy"][k], n=8)
    D_ = Loc(gd, (18.3, 2.35, 0.012), 25)                                                     # a doll on the rug
    D_.ball((0, 0, 0.03), 0.04, M["toy"][0], n=8)
    D_.box((0.12, 0, 0.025), (0.16, 0.08, 0.04), M["toy"][3])
    gd.box((21.3, 2.25, 0.015), (0.3, 0.21, 0.003), M["paper"], rot=(0, 0, 15))             # a drawing, crayons
    for k in range(4):
        gd.cyl((21.55 + k * 0.03, 2.1, 0.02), 0.006, 0.09, M["toy"][k], rot=(0, 90, 30 + k * 20), n=6)
    window(gs, gd, *WIN1[1], M, curtains="curtain", floor_len=True)
    pendant(gs, gd, 20.0, 1.3, H1, M, kind="drum")
    frame_pic(gs, gd, 24.55, 2.1, 0.4, 0.5, M, art="abstract", seed=33)


def kitchen(gs, gd, M):
    r = random.Random(4)
    # the base units: plinth, carcass, doors and drawers with bar handles, the dishwasher, the oven
    x0, x1 = 25.5, 32.5
    gs.box(((x0 + x1) / 2, 3.88, 0.05), (x1 - x0, 0.55, 0.1), M["ink"])
    gs.box(((x0 + x1) / 2, 3.86, 0.49), (x1 - x0, 0.6, 0.78), M["cabinet"])
    gs.box(((x0 + x1) / 2, 3.85, 0.915), (x1 - x0 + 0.04, 0.66, 0.05), M["worktop"])
    nmod = 12
    mw = (x1 - x0) / nmod
    fy = 3.555
    for k in range(nmod):
        cx = x0 + mw * (k + 0.5)
        if k == 7:                                                                        # the oven
            gd.box((cx, fy, 0.42), (mw - 0.02, 0.012, 0.58), M["ink"])
            gd.box((cx, fy - 0.006, 0.4), (mw - 0.14, 0.006, 0.36), M["screen"])
            gd.box((cx, fy - 0.03, 0.66), (mw - 0.12, 0.02, 0.02), M["chrome"])
            gd.box((cx, fy, 0.8), (mw - 0.02, 0.012, 0.12), M["steel_mid"])
            for j in range(4):
                gd.cyl((cx - 0.2 + j * 0.13, fy - 0.015, 0.8), 0.018, 0.02, M["chrome"], rot=(90, 0, 0), n=8)
            continue
        if k == 2:                                                                        # the dishwasher
            gd.box((cx, fy, 0.48), (mw - 0.02, 0.012, 0.74), M["cabinet"])
            gd.box((cx, fy - 0.02, 0.8), (mw - 0.16, 0.02, 0.02), M["chrome"])
            continue
        if k in (4, 9, 10):
            gd.box((cx, fy, 0.79), (mw - 0.02, 0.012, 0.15), M["cabinet"])
            gd.box((cx, fy - 0.02, 0.79), (0.16, 0.02, 0.015), M["chrome"])
            gd.box((cx, fy, 0.4), (mw - 0.02, 0.012, 0.58), M["cabinet"])
            gd.box((cx, fy - 0.02, 0.64), (0.16, 0.02, 0.015), M["chrome"])
        else:
            gd.box((cx, fy, 0.48), (mw - 0.02, 0.012, 0.74), M["cabinet"])
            gd.box((cx + (0.2 if k % 2 else -0.2), fy - 0.02, 0.72), (0.015, 0.02, 0.16), M["chrome"])
    # the sink, the drainer, the tap; the hob; the window blind and the sill
    gs.box((27.4, 3.85, 0.945), (0.8, 0.5, 0.012), M["chrome"])
    gs.box((27.25, 3.83, 0.948), (0.42, 0.38, 0.01), M["steel"])
    gd.cyl((27.25, 3.83, 0.955), 0.03, 0.004, M["chrome"], n=10)
    for k in range(6):
        gd.box((27.66, 3.68 + k * 0.06, 0.952), (0.22, 0.012, 0.003), M["steel_mid"])
    gs.cyl((27.4, 4.08, 1.07), 0.022, 0.24, M["chrome"], n=8)
    gs.beam((27.4, 4.08, 1.18), (27.4, 3.92, 1.16), 0.03, 0.03, M["chrome"])
    gs.beam((27.4, 3.92, 1.165), (27.4, 3.88, 1.08), 0.025, 0.025, M["chrome"])
    gd.beam((27.4, 4.08, 1.2), (27.44, 4.0, 1.26), 0.018, 0.018, M["chrome"])
    gd.cyl((27.4, 3.88, 1.02), 0.005, 0.12, M["water"], n=5)  # the tap, running
    gs.box((29.875, 3.85, 0.945), (0.56, 0.5, 0.01), M["screen"])
    for dx, dy, rr in ((-0.13, -0.11, 0.08), (0.13, -0.11, 0.06), (-0.13, 0.11, 0.06), (0.13, 0.11, 0.09)):
        gd.cyl((29.875 + dx, 3.85 + dy, 0.952), rr, 0.003, M["steel_mid"], n=14)
    gd.cyl((29.75, 3.74, 0.965), 0.1, 0.03, M["steel"], n=14)                                 # a pan left on it
    gd.beam((29.65, 3.74, 0.98), (29.38, 3.6, 0.99), 0.025, 0.012, M["steel"])
    window(gs, gd, *WIN1[2], M, curtains=None, radiator=False, blind=True)
    for k in range(3):                                                                    # herbs on the sill
        px = 28.05 + k * 0.22
        gd.cyl((px, WALL - 0.12, 1.32), 0.045, 0.09, M["ceramic"], n=8, r2=0.035)
        ball(gd, (px, WALL - 0.12, 1.42), 0.06, M["leaf"], n=6)
    gd.cyl((29.25, WALL - 0.12, 1.36), 0.03, 0.16, M["plastic_w"], n=8)                     # washing-up liquid
    # the upper cabinets, either side of the window
    for (a, b) in ((25.5, 26.95), (31.15, 32.5)):
        gs.box(((a + b) / 2, WALL - 0.16, 2.15), (b - a, 0.32, 0.8), M["cabinet"])
        nd = 2
        dw = (b - a) / nd
        for k in range(nd):
            cx = a + dw * (k + 0.5)
            gd.box((cx, WALL - 0.325, 2.15), (dw - 0.02, 0.012, 0.78), M["cabinet"])
            gd.box((cx + (0.22 if k else -0.22) * dw / 0.72, WALL - 0.345, 1.82), (0.015, 0.02, 0.14), M["chrome"])
        gs.box(((a + b) / 2, WALL - 0.16, 2.56), (b - a + 0.03, 0.34, 0.03), M["cabinet"])
    # the pan rail and the pans on it (the game knocks them off)
    gs.beam((27.0, 3.8, 2.2), (31.0, 3.8, 2.2), 0.02, 0.02, M["steel"])
    for x in (27.0, 31.0):
        gs.beam((x, 3.8, 2.2), (x, WALL, 2.2), 0.02, 0.02, M["steel"])
    for k in range(5):
        px = 27.4 + k * 0.85
        rr = 0.17 - 0.015 * (k % 3)
        gs.cyl((px, 3.8, 1.9), rr, 0.04, M["steel"], rot=(90, 0, 0), n=16)
        gd.cyl((px, 3.775, 1.9), rr * 0.85, 0.004, M["steel_mid"], rot=(90, 0, 0), n=16)
        gd.beam((px, 3.8, 1.9 + rr), (px, 3.8, 2.14), 0.025, 0.02, M["ink"])
        gd.torus((px, 3.8, 2.17), 0.03, 0.006, M["chrome"], rot=(90, 0, 0), n1=8, n2=4)
    # on the worktop: a microwave, a bread bin, the knife block, the kettle, a toaster, the mug
    # tree, a utensil pot, a radio, rubber gloves, a roll of bin bags
    wz = 0.94
    gs.box((25.95, 3.9, wz + 0.15), (0.5, 0.36, 0.3), M["plastic_w"])
    gd.box((25.9, 3.715, wz + 0.15), (0.32, 0.01, 0.22), M["screen"])
    gd.box((26.16, 3.715, wz + 0.15), (0.08, 0.01, 0.22), M["ink"])
    gs.box((26.55, 3.95, wz + 0.11), (0.38, 0.25, 0.22), M["steel_mid"])
    gs.cyl((26.55, 3.95, wz + 0.22), 0.12, 0.38, M["steel_mid"], rot=(0, 90, 0), n=12)
    gs.box((30.6, 3.85, wz + 0.13), (0.2, 0.15, 0.26), M["wood_furn"])
    for k in range(5):
        gd.box((30.55 + (k % 3) * 0.04, 3.85 + (k // 3 - 0.5) * 0.05, wz + 0.31), (0.025, 0.018, 0.1), M["ink"], rot=(0, -20, 0))
    gs.cyl((31.4, 3.85, wz + 0.012), 0.11, 0.025, M["ink"], n=14)                             # the kettle, boiled dry
    gs.cyl((31.4, 3.85, wz + 0.13), 0.1, 0.21, M["chrome"], n=14, r2=0.085)
    gd.cyl((31.4, 3.85, wz + 0.245), 0.04, 0.02, M["ink"], n=10)
    gd.beam((31.33, 3.85, wz + 0.25), (31.47, 3.85, wz + 0.25), 0.03, 0.03, M["ink"])
    gd.beam((31.5, 3.85, wz + 0.18), (31.6, 3.85, wz + 0.24), 0.04, 0.04, M["chrome"])
    gs.box((31.9, 3.88, wz + 0.1), (0.3, 0.17, 0.2), M["chrome"])
    for dy in (-0.03, 0.03):
        gd.box((31.9, 3.88 + dy, wz + 0.2), (0.2, 0.025, 0.004), M["ink"])
    gd.cyl((32.3, 3.88, wz + 0.17), 0.012, 0.34, M["wood_furn"], n=6)
    gd.cyl((32.3, 3.88, wz + 0.01), 0.07, 0.02, M["wood_furn"], n=10)
    for k in range(3):
        a = math.radians(90 + k * 120)
        mug(gd, (32.3 + math.cos(a) * 0.1, 3.88 + math.sin(a) * 0.1, wz + 0.15 + k * 0.05), M, m=M["books"][k + 2], yaw=math.degrees(a))
    gd.cyl((30.95, 3.98, wz + 0.08), 0.06, 0.16, M["ceramic"], n=10)
    for k in range(4):
        gd.beam((30.95, 3.98, wz + 0.1), (30.95 + r.uniform(-0.05, 0.05), 3.98 + r.uniform(-0.04, 0.04), wz + 0.36), 0.012, 0.012, M["wood_furn"])
    gs.box((28.15, 4.02, wz + 0.09), (0.3, 0.12, 0.18), M["plastic_d"])
    gd.cyl((28.08, 3.959, wz + 0.09), 0.05, 0.004, M["steel_mid"], rot=(90, 0, 0), n=10)
    for k, dx in enumerate((0.0, 0.12)):                                                    # yellow gloves, on the edge
        gd.box((32.3 + dx, 3.62, wz + 0.02), (0.1, 0.22, 0.03), M["toy"][0], rot=(0, 0, 20 + k * 30))
    gd.cyl((26.2, 3.66, wz + 0.05), 0.05, 0.1, M["plastic_d"], rot=(0, 90, 0), n=10)
    # the fridge: two doors, handles, her drawings and the magnets on it
    gs.box((33.2, 3.72, 1.0), (0.78, 0.76, 2.0), M["plastic_w"])
    gd.box((33.2, 3.33, 1.24), (0.76, 0.02, 0.012), M["ink"])
    for z0, z1 in ((0.05, 1.22), (1.27, 1.97)):
        gd.box((33.2, 3.335, (z0 + z1) / 2), (0.76, 0.012, z1 - z0), M["plastic_w"])
        gd.box((32.88, 3.3, (z0 + z1) / 2 + (0.2 if z0 < 1 else -0.15)), (0.025, 0.04, 0.4), M["chrome"])
    for k, (dx, z, w, h) in enumerate(((0.05, 1.62, 0.26, 0.2), (0.16, 1.38, 0.2, 0.26), (0.02, 0.98, 0.28, 0.2), (0.18, 0.72, 0.22, 0.2), (-0.02, 0.6, 0.16, 0.12))):
        gd.box((33.2 + dx, 3.324, z), (w, 0.003, h), M["paper"], rot=(0, r.uniform(-6, 6), 0))
        for j in range(3):                                                               # crayon scribbles
            gd.box((33.2 + dx + r.uniform(-w, w) * 0.3, 3.321, z + r.uniform(-h, h) * 0.3), (w * r.uniform(0.2, 0.5), 0.002, 0.012),
                   M["toy"][(k + j) % 4], rot=(0, r.uniform(-40, 40), 0))
        gd.cyl((33.2 + dx, 3.318, z + h / 2 - 0.02), 0.018, 0.012, M["toy"][k % 4], rot=(90, 0, 0), n=8)
    for k in range(6):
        gd.box((33.0 + r.uniform(0, 0.35), 3.318, r.uniform(1.0, 1.9)), (0.04, 0.012, 0.05), M["toy"][k % 4], rot=(0, r.uniform(-20, 20), 0))
    # the pedal bin, the mop against the end wall, black sacks by the fridge
    gs.cyl((33.2, 2.95, 0.24), 0.15, 0.48, M["chrome"], n=14)
    gd.cyl((33.2, 2.95, 0.49), 0.155, 0.025, M["chrome"], n=14)
    gs.beam((33.45, 2.15, 0.05), (33.86, 2.2, 1.35), 0.025, 0.025, M["wood_furn"])
    D.boulder(gs, (33.43, 2.15, 0.07), 0.1, M["fabric_pale"], squash=0.6)
    for k, (bx, by) in enumerate(((33.55, 2.55), (33.3, 2.45))):
        ball(gs, (bx, by, 0.24), 0.24, M["plastic_d"], n=10, squash=(1.0, 0.9, 1.1))
        gd.cyl((bx, by, 0.52), 0.035, 0.1, M["plastic_d"], n=8)
    gs.cyl((31.6, 3.28, 0.45), 0.075, 0.9, M["sheeting"], n=12)                               # the spare roll of sheeting
    gd.torus((29.7, 2.0, 1.0), 0.05, 0.02, M["steel_mid"], n1=10, n2=5)                       # duct tape on the island
    # the island (the game face-plants a man on it): panelled doors, worktop, two stools
    gs.box((29.4, 2.2, 0.05), (2.3, 0.8, 0.1), M["ink"])
    gs.box((29.4, 2.2, 0.49), (2.4, 0.9, 0.78), M["cabinet"])
    gs.box((29.4, 2.2, 0.96), (2.5, 1.0, 0.05), M["worktop"])
    for k in range(3):
        cx = 28.6 + k * 0.8
        gd.box((cx, 1.744, 0.49), (0.76, 0.012, 0.74), M["cabinet"])
        gd.box((cx, 1.735, 0.5), (0.6, 0.006, 0.58), M["plastic_w"])
        gd.box((cx + 0.3, 1.725, 0.75), (0.015, 0.02, 0.14), M["chrome"])
    for sx in (28.45, 30.35):
        gs.cyl((sx, 1.52, 0.66), 0.17, 0.05, M["fabric"], n=14)
        for k in range(4):
            a = math.radians(45 + k * 90)
            gd.beam((sx + math.cos(a) * 0.1, 1.52 + math.sin(a) * 0.1, 0.64), (sx + math.cos(a) * 0.18, 1.52 + math.sin(a) * 0.18, 0.0), 0.022, 0.022, M["steel"])
        gd.torus((sx, 1.52, 0.25), 0.15, 0.01, M["steel"], n1=12, n2=4)
    # on the island: the fruit bowl, mugs, a chopping board, a cookbook, her plastic cup
    tz = 0.985
    gs.cyl((29.05, 2.25, tz + 0.04), 0.18, 0.08, M["ceramic"], n=16, r2=0.11)
    for k in range(7):
        a = k / 7 * math.tau
        ball(gd, (29.05 + math.cos(a) * 0.09, 2.25 + math.sin(a) * 0.09, tz + 0.1 + (k % 2) * 0.02), 0.045, M["fruit"][k % 3], n=8)
    gd.beam((29.0, 2.3, tz + 0.12), (29.18, 2.18, tz + 0.18), 0.035, 0.025, M["fruit"][2])  # bananas
    mug(gd, (28.45, 2.05, tz), M, yaw=30)
    mug(gd, (28.6, 2.35, tz), M, m=M["books"][3], yaw=-60)
    gd.box((30.15, 2.25, tz + 0.012), (0.42, 0.28, 0.024), M["oak_top"], rot=(0, 0, 8))
    gd.box((30.2, 2.24, tz + 0.03), (0.22, 0.02, 0.006), M["chrome"], rot=(0, 0, 30))
    for k in range(3):
        ball(gd, (30.05 + k * 0.05, 2.3, tz + 0.04), 0.022, M["fruit"][1], n=6)
    gd.box((29.55, 2.45, tz + 0.012), (0.3, 0.22, 0.02), M["books"][1], rot=(0, 0, -10))
    gd.cyl((28.85, 1.9, tz + 0.05), 0.035, 0.1, M["toy"][2], n=10, r2=0.03)
    # the little table where the three of them ate; one chair is gone (it's in the middle of the room)
    tx, ty = 26.35, 2.05
    gs.cyl((tx, ty, 0.74), 0.5, 0.035, M["oak_top"], n=24)
    gs.cyl((tx, ty, 0.37), 0.05, 0.72, M["oak"], n=10)
    gs.cyl((tx, ty, 0.025), 0.28, 0.05, M["oak"], n=14)
    chair(gs, (tx - 0.62, ty, 0.0), 180, M)
    chair(gs, (tx + 0.05, ty + 0.66, 0.0), 95, M)
    for (px, py) in ((tx - 0.25, ty), (tx, ty + 0.25)):
        gd.cyl((px, py, 0.76), 0.12, 0.004, M["fabric"], n=14)
    gd.cyl((tx - 0.25, ty, 0.77), 0.1, 0.015, M["ceramic"], n=14)
    gd.box((tx + 0.15, ty - 0.2, 0.761), (0.3, 0.21, 0.003), M["paper"], rot=(0, 0, -20))
    for k in range(3):
        gd.cyl((tx + 0.28, ty - 0.1 + k * 0.03, 0.765), 0.006, 0.09, M["toy"][k], rot=(0, 90, 70), n=6)
    gd.cyl((tx + 0.25, ty + 0.2, 0.79), 0.018, 0.06, M["ceramic"], n=8)
    # the calendar and a clock on the end of the wall
    gd.box((25.08, 2.65, 1.55), (0.006, 0.32, 0.46), M["paper"])
    for k in range(5):
        gd.box((25.085, 2.65, 1.42 + k * 0.05), (0.004, 0.28, 0.004), M["books"][0])
    gd.box((25.085, 2.65, 1.7), (0.004, 0.28, 0.12), M["books"][2])
    gs.cyl((25.1, 1.4, 2.3), 0.15, 0.04, M["plastic_w"], rot=(0, 90, 0), n=16)
    gd.box((25.125, 1.4, 2.33), (0.006, 0.012, 0.09), M["ink"])
    gd.box((25.125, 1.43, 2.3), (0.006, 0.07, 0.012), M["ink"])
    for (sx, sy) in ((26.6, 0.0), (31.6, 0.0), (26.6, 2.6), (31.6, 2.6)):                       # ceiling spots (off)
        gd.cyl((sx, sy, H1 - 0.015), 0.05, 0.012, M["chrome"], n=10)
    # outside the back door: the step
    gs.box((X1 + 0.35, 0.0, 0.03), (0.5, 1.5, 0.06), M["concrete"])


def landing(gs, gd, M):
    r = random.Random(5)
    gs.box((11.8, 0.0, F + 0.008), (4.2, 0.9, 0.012), M["runner"])
    frame_pic(gs, gd, 10.0, F + 1.5, 0.8, 0.6, M, art="print", seed=41)
    frame_pic(gs, gd, 11.4, F + 1.6, 0.6, 0.8, M, art="photo", seed=42)
    drawers(gs, gd, 10.7, WALL - 0.16, F, 1.0, 0.3, 0.78, M, rows=2, cols=2)
    gd.cyl((10.4, WALL - 0.16, F + 0.92), 0.06, 0.24, M["ceramic"], n=10, r2=0.04)
    for k in range(3):
        gd.beam((10.4, WALL - 0.16, F + 1.0), (10.4 + (k - 1) * 0.08, WALL - 0.16 + r.uniform(-0.04, 0.04), F + 1.28), 0.008, 0.008, M["leaf"])
        ball(gd, (10.4 + (k - 1) * 0.08, WALL - 0.16, F + 1.3), 0.03, M["paper"], n=6)
    gs.cyl((11.0, WALL - 0.16, F + 0.84), 0.12, 0.05, M["ceramic"], n=14)
    window(gs, gd, *WIN2[0], M, curtains="curtain", floor=F)
    gs.box((13.55, 3.6, F + 0.22), (0.5, 0.36, 0.44), M["fabric_pale"])                        # the laundry basket
    D.boulder(gd, (13.55, 3.6, F + 0.44), 0.18, M["curtain"], squash=0.5)
    gd.box((6.5, 0.6, H2 - 0.006), (0.8, 0.8, 0.012), M["white_paint"])                         # the loft hatch
    gd.box((6.5, 0.6, H2 - 0.014), (0.7, 0.7, 0.006), M["ceiling"])
    gd.cyl((6.5, 0.25, H2 - 0.03), 0.02, 0.04, M["chrome"], n=6)
    gd.cyl((10.0, -0.6, H2 - 0.02), 0.06, 0.03, M["plastic_w"], n=12)                           # the smoke alarm
    switch(gd, 9.8, WALL, F + 1.2, M)
    pendant(gs, gd, 11.6, 0.9, H2, M, kind="globe")


def maya(gs, gd, M):
    r = random.Random(6)
    gs.box((18.0, (YF + YB) / 2, F + 0.006), (7.86, YB - YF, 0.012), M["carpet_kid"])
    gs.cyl((17.0, 1.1, F + 0.016), 1.25, 0.012, M["rug_kid"], n=28)
    bed_kid(gs, gd, 19.8, 3.4, M)
    # the bedside table, a night light, a book
    gs.box((18.68, WALL - 0.2, F + 0.22), (0.34, 0.34, 0.44), M["oak"])
    gd.box((18.68, WALL - 0.375, F + 0.3), (0.28, 0.012, 0.18), M["oak"])
    gd.cyl((18.68, WALL - 0.39, F + 0.3), 0.014, 0.02, M["toy"][1], rot=(90, 0, 0), n=8)
    gs.cyl((18.63, WALL - 0.22, F + 0.52), 0.07, 0.15, M["plastic_w"], n=12)
    gd.cyl((18.63, WALL - 0.22, F + 0.62), 0.05, 0.04, M["toy"][0], rot=(90, 0, 0), n=5)
    gd.box((18.75, WALL - 0.12, F + 0.455), (0.16, 0.12, 0.025), M["books"][4], rot=(0, 0, 12))
    # the toy chest, its lid thrown back, toys spilling over the edge
    tcx = 15.6
    gs.box((tcx, 3.7, F + 0.28), (0.9, 0.5, 0.56), M["oak"])
    gs.box((tcx, 3.98, F + 0.78), (0.92, 0.04, 0.5), M["oak"], rot=(-12, 0, 0))
    for k in range(5):
        gd.cyl((tcx - 0.32 + k * 0.16, 3.445, F + 0.3), 0.04, 0.01, M["white_paint"], rot=(90, 0, 0), n=5)
    for k in range(5):
        ball(gd, (tcx - 0.3 + k * 0.14, 3.7 + r.uniform(-0.1, 0.1), F + 0.58), 0.06, M["toy"][k % 4], n=8)
    gd.box((tcx + 0.3, 3.5, F + 0.6), (0.06, 0.06, 0.25), M["toy"][2], rot=(0, -40, 10))
    # the shelf over it: books, a music box, a rabbit; the walkie-talkie and its gap are story props
    gs.box((15.65, WALL - 0.11, F + 1.25), (1.3, 0.22, 0.025), M["white_paint"])
    for dx in (-0.5, 0.5):
        gd.box((15.65 + dx, WALL - 0.04, F + 1.18), (0.02, 0.08, 0.12), M["white_paint"])
    books_row(gd, 15.02, 15.3, WALL - 0.12, F + 1.263, 0.15, M, r, lean=False)
    gs.box((15.92, WALL - 0.12, F + 1.31), (0.15, 0.1, 0.09), M["oak"])                        # the music box
    gs.box((15.92, WALL - 0.14, F + 1.37), (0.15, 0.1, 0.02), M["oak"], rot=(-55, 0, 0))
    ball(gd, (15.92, WALL - 0.12, F + 1.375), 0.025, M["ceramic"], n=6)
    R = Loc(gd, (16.18, WALL - 0.12, F + 1.263), -10)
    R.ball((0, 0, 0.07), 0.06, M["toy"][0], n=8, squash=(0.9, 0.8, 1.1))
    R.ball((0, 0, 0.16), 0.045, M["toy"][0], n=8)
    for s in (-1, 1):
        R.box((s * 0.02, 0, 0.24), (0.018, 0.012, 0.1), M["toy"][0], rot=(0, s * 10, 0))
    # the dolls' house
    hx = 14.55
    gs.box((hx, WALL - 0.18, F + 0.32), (0.56, 0.3, 0.64), M["white_paint"])
    prism_x(gs, [(WALL - 0.34, F + 0.64), (WALL - 0.02, F + 0.64), (WALL - 0.18, F + 0.86)], hx - 0.3, hx + 0.3, M["toy"][1])
    for k in range(4):
        gd.box((hx - 0.13 + (k % 2) * 0.26, WALL - 0.336, F + 0.2 + (k // 2) * 0.28), (0.12, 0.01, 0.12), M["toy"][2])
    gd.box((hx, WALL - 0.336, F + 0.1), (0.1, 0.01, 0.18), M["toy"][0])
    # her drawings on the wall (the one with the three of them is a story prop), her name
    for k, (x, z, w, h) in enumerate(((16.42, F + 1.95, 0.3, 0.38), (17.25, F + 1.98, 0.3, 0.3), (17.32, F + 1.2, 0.28, 0.36),
                                      (19.3, F + 1.38, 0.36, 0.28), (20.25, F + 1.5, 0.3, 0.4), (21.3, F + 1.8, 0.34, 0.26))):
        rot = r.uniform(-7, 7)
        gd.box((x, WALL - 0.004, z), (w, 0.004, h), M["paper"], rot=(0, rot, 0))
        for j in range(4):
            gd.box((x + r.uniform(-w, w) * 0.3, WALL - 0.008, z + r.uniform(-h, h) * 0.3), (w * r.uniform(0.15, 0.5), 0.002, 0.014),
                   M["toy"][(k + j) % 4], rot=(0, r.uniform(-60, 60), 0))
        gd.cyl((x + w * 0.25, WALL - 0.009, z - h * 0.1), w * 0.12, 0.002, M["toy"][k % 4], rot=(90, 0, 0), n=10)
        for sx in (-1, 1):
            gd.box((x + sx * w / 2, WALL - 0.007, z + h / 2), (0.05, 0.002, 0.02), M["plastic_w"], rot=(0, sx * 40, 0))
    S.stencil(gd, "MAYA", (19.8, WALL - 0.01, F + 1.92), 0.26, M["toy"][1])
    for k, x in enumerate((19.38, 19.62, 19.9, 20.17)):
        gd.cyl((x + 0.09, WALL - 0.02, F + 2.05), 0.02, 0.01, M["toy"][k % 4], rot=(90, 0, 0), n=5)
    # the fairy lights along the wall, on their wire; stars on the wall and ceiling
    prev = None
    for k in range(18):
        p = (14.3 + k * 0.42, YB - 0.06, F + 2.35 - 0.12 * math.sin(k * 0.8))
        gs.cyl(p, 0.035, 0.03, M["fairy"], rot=(90, 0, 0), n=8)
        if prev:
            gd.beam(prev, p, 0.006, 0.006, M["ink"])
        prev = p
    for _ in range(14):
        gd.cyl((rnd.uniform(14.3, 21.7), YB - 0.04, F + rnd.uniform(2.55, 2.75)), 0.05, 0.01, M["white_paint"], rot=(90, 0, 0), n=5)
    for _ in range(10):
        gd.cyl((rnd.uniform(14.5, 21.5), rnd.uniform(-1.6, 3.8), H2 - 0.006), 0.05, 0.008, M["white_paint"], n=5)
    rocking_horse(gs, gd, 17.15, 2.8, M)
    # the chest of drawers by the far wall, a lamp and a framed photo on it, a hairbrush
    drawers(gs, gd, 21.35, WALL - 0.21, F, 0.9, 0.42, 0.8, M, rows=3, m=M["oak"])
    gs.cyl((21.1, WALL - 0.2, F + 0.86), 0.06, 0.1, M["ceramic"], n=10)
    gs.cyl((21.1, WALL - 0.2, F + 1.02), 0.12, 0.16, M["curtain_kid"], n=12, r2=0.08)
    standing_frame(gs, gd, (21.5, WALL - 0.22, F + 0.81), 0.16, 0.2, M, yaw=-10, m=M["white_paint"])
    gd.box((21.6, WALL - 0.36, F + 0.83), (0.18, 0.05, 0.03), M["toy"][3], rot=(0, 0, 25))
    # the toys on the floor: they turned the room over looking for her
    for k in range(26):
        kind = k % 6
        x = r.uniform(14.5, 21.5)
        y = r.uniform(-0.6, 3.0) if kind in (2, 3) else r.uniform(0.95, 3.0)     # only flat things in the lane
        if 18.9 < x < 20.7 and y > 2.8:
            continue
        m = M["toy"][r.randrange(4)]
        if kind == 0:                                                                     # letter blocks
            s = 0.06
            gd.box((x, y, F + 0.012 + s / 2), (s, s, s), m, rot=(0, 0, r.uniform(0, 90)))
        elif kind == 1:                                                                   # a ball
            ball(gd, (x, y, F + 0.11), 0.1, m, n=10)
        elif kind == 2:                                                                   # a picture book, open
            gd.box((x, y, F + 0.02), (0.24, 0.18, 0.01), m, rot=(0, 6, r.uniform(0, 90)))
            gd.box((x, y, F + 0.026), (0.22, 0.16, 0.004), M["paper"], rot=(0, 6, r.uniform(0, 90)))
        elif kind == 3:                                                                   # crayons
            for j in range(3):
                gd.cyl((x + j * 0.03, y, F + 0.02), 0.006, 0.09, M["toy"][j], rot=(0, 90, r.uniform(0, 180)), n=6)
        elif kind == 4:                                                                   # a toy car
            T = Loc(gd, (x, y, F + 0.012), r.uniform(0, 360))
            T.box((0, 0, 0.04), (0.16, 0.08, 0.05), m)
            T.box((-0.01, 0, 0.08), (0.08, 0.07, 0.04), m)
            for dx in (-0.05, 0.05):
                for dy in (-0.045, 0.045):
                    T.cyl((dx, dy, 0.02), 0.02, 0.015, M["ink"], rot=(90, 0, 0), n=8)
        else:                                                                             # a stuffed toy on its side
            ball(gd, (x, y, F + 0.07), 0.07, m, n=8, squash=(1.3, 0.9, 0.8))
            ball(gd, (x + 0.1, y, F + 0.07), 0.05, m, n=8)
    gd.box((17.9, 1.6, F + 0.015), (0.03, 0.02, 0.01), M["toy"][0], rot=(0, 0, 30))            # a small hair clip
    window(gs, gd, *WIN2[1], M, curtains="curtain_kid", floor=F)
    pendant(gs, gd, 18.0, 1.0, H2, M, kind="star")


def bedroom(gs, gd, M):
    r = random.Random(7)
    gs.box((28.0, (YF + YB) / 2, F + 0.006), (11.8, YB - YF, 0.012), M["carpet"])
    gs.box((28.05, 1.45, F + 0.016), (2.2, 1.1, 0.012), M["rug"])
    bed_double(gs, gd, 28.05, M)
    for nx in (26.75, 29.35):                                                              # the bedside tables
        drawers(gs, gd, nx, WALL - 0.2, F, 0.46, 0.4, 0.52, M, rows=2)
    gs.cyl((26.8, 3.8, F + 0.57), 0.07, 0.04, M["ceramic"], n=10)                            # her lamp (on)
    gs.cyl((26.8, 3.8, F + 0.62), 0.012, 0.1, M["brass"], n=6)
    gs.cyl((26.8, 3.8, F + 0.75), 0.15, 0.3, M["lamp"], n=12, r2=0.08)
    gd.box((26.6, 3.7, F + 0.545), (0.15, 0.2, 0.03), M["books"][2], rot=(0, 0, -10))
    gs.cyl((29.45, 3.85, F + 0.57), 0.06, 0.04, M["ceramic"], n=10)                          # his lamp (off)
    gs.cyl((29.45, 3.85, F + 0.64), 0.012, 0.12, M["brass"], n=6)
    gs.cyl((29.45, 3.85, F + 0.77), 0.14, 0.26, M["fabric_pale"], n=12, r2=0.09)
    gd.box((29.2, 3.75, F + 0.565), (0.11, 0.07, 0.08), M["ink"], rot=(0, 0, 15))           # the alarm clock
    gd.cyl((29.5, 3.62, F + 0.585), 0.03, 0.09, M["glass"], n=8)
    gd.box((29.3, 3.62, F + 0.535), (0.14, 0.05, 0.015), M["ink"], rot=(0, 0, -20))         # his glasses
    # their wedding photo over the bed, two small frames
    frame_pic(gs, gd, 28.05, F + 1.75, 0.7, 0.5, M, art="photo", seed=51)
    frame_pic(gs, gd, 26.75, F + 1.45, 0.25, 0.32, M, art="photo", seed=52)
    frame_pic(gs, gd, 29.35, F + 1.45, 0.25, 0.32, M, art="print", seed=53)
    # the wardrobe (the game slams its door on a man), suitcases on top
    wx = 23.6
    gs.box((wx, 3.7, F + 1.1), (1.2, 0.6, 2.2), M["wood_furn"])
    gs.box((wx, 3.68, F + 2.23), (1.28, 0.66, 0.06), M["wood_furn"])
    gs.box((wx, 3.7, F + 0.04), (1.22, 0.58, 0.08), M["ink"])
    for s in (-1, 1):
        gd.box((wx + s * 0.3, 3.395, F + 1.12), (0.58, 0.012, 2.0), M["wood_furn"])
        gd.box((wx + s * 0.3, 3.39, F + 1.12), (0.44, 0.006, 1.8), M["oak"])
        gd.box((wx + s * 0.04, 3.375, F + 1.15), (0.018, 0.025, 0.2), M["brass"])
    gs.box((wx - 0.1, 3.72, F + 2.38), (0.8, 0.5, 0.24), M["fabric"])
    gs.box((wx + 0.2, 3.72, F + 2.58), (0.6, 0.42, 0.18), M["steel_mid"])
    # her chair with a cardigan on it, the laundry basket
    chair(gs, (25.15, 3.6, F), 90, M, m=M["white_paint"])
    gd.box((25.15, 3.83, F + 0.85), (0.4, 0.06, 0.42), M["curtain"], rot=(8, 0, 0))
    gd.box((25.15, 3.6, F + 0.47), (0.36, 0.38, 0.02), M["curtain"])
    gs.cyl((25.8, 3.85, F + 0.25), 0.2, 0.5, M["fabric_pale"], n=12, r2=0.17)
    # the dresser and its mirror; perfume, a jewellery box, a hairbrush, a photo of Maya
    drawers(gs, gd, 32.6, 3.82, F, 1.2, 0.5, 0.9, M, rows=3, cols=2)
    gs.box((32.6, WALL - 0.03, F + 1.45), (0.84, 0.05, 0.95), M["wood_furn"])
    gs.box((32.6, WALL - 0.06, F + 1.45), (0.74, 0.01, 0.85), M["mirror"])
    for k in range(3):
        gd.cyl((32.15 + k * 0.08, 3.85, F + 0.97), 0.025 + 0.005 * k, 0.1 + 0.02 * k, M["glass"], n=8)
    gs.box((32.75, 3.9, F + 0.97), (0.22, 0.14, 0.1), M["fabric"])
    gd.box((33.0, 3.75, F + 0.935), (0.2, 0.05, 0.03), M["ink"], rot=(0, 0, -15))
    standing_frame(gs, gd, (32.45, 3.9, F + 0.925), 0.14, 0.18, M, yaw=10)
    window(gs, gd, *WIN2[2], M, curtains="curtain", floor_len=True, floor=F)
    pendant(gs, gd, 28.0, 1.2, H2, M, kind="drum")


def build_house(coll, M):
    gs = S.Group("HomeShell")
    gd = S.Group("HomeDetail")
    structure(gs, gd, M)
    stairs(gs, gd, M)
    hall(gs, gd, M)
    study(gs, gd, M)
    living(gs, gd, M)
    kitchen(gs, gd, M)
    landing(gs, gd, M)
    maya(gs, gd, M)
    bedroom(gs, gd, M)
    # the front door (open, swung out onto the porch) and the back door (open onto the garden)
    door_leaf(gs, gd, (X0 - 0.11, 0.62, 0.0), 168, M, w=1.2, h=2.16, m=M["ink"], t=0.055)
    gd.box((X0 - 0.3, 0.68, 1.15), (0.25, 0.012, 0.035), M["brass"], rot=(0, 0, 168))
    gd.cyl((X0 - 0.62, 0.66, 1.55), 0.035, 0.012, M["brass"], rot=(90, 0, 168), n=10)
    gd.box((X0 - 0.12, -0.55, 1.12), (0.06, 0.04, 0.16), M["steel_mid"])                      # the splintered lock
    for k in range(4):
        gd.box((X0 + 0.02, -0.6 + k * 0.03, 1.0 + k * 0.08), (0.06, 0.012, 0.1), M["wood_furn"], rot=(0, 30 * (k - 1.5), 0))
    door_leaf(gs, gd, (X1 + 0.11, 0.62, 0.0), 12, M, w=1.2, h=2.16, glazed=True, t=0.05)
    return gs.finish(coll), gd.finish(coll)


def build_front(coll, M):
    """The front wall (the camera side): brick, windows with frames and sills, lintels, a string
    course, the gutter and downpipes, the meter box. The game cuts it away."""
    g = S.Group("HomeFront")
    y = YF - 0.1
    g.box(((X0 + X1) / 2, y, H2 / 2), (X1 - X0, 0.2, H2), M["brick"])
    g.box(((X0 + X1) / 2, y - 0.11, 0.2), (X1 - X0 + 0.02, 0.04, 0.4), M["brick_n"])           # plinth
    g.box(((X0 + X1) / 2, y - 0.12, FLOOR2 - 0.05), (X1 - X0 + 0.02, 0.05, 0.15), M["coping"])  # string course
    g.box(((X0 + X1) / 2, y - 0.2, H2 - 0.05), (X1 - X0 + 0.8, 0.16, 0.12), M["plastic_d"])
    for x in (0.3, 33.7):
        g.cyl((x, y - 0.2, H2 / 2), 0.045, H2, M["plastic_d"], n=8)
        g.box((x, y - 0.2, 0.1), (0.2, 0.12, 0.06), M["plastic_d"])
    for x in (6.0, 12.0, 20.0, 29.0):
        for z in (1.6, FLOOR2 + 1.5):
            g.box((x, y - 0.12, z), (1.4, 0.05, 1.3), M["glass"])
            for s in (-1, 1):
                g.box((x + s * 0.72, y - 0.14, z), (0.08, 0.06, 1.42), M["white_paint"])
            for dz in (-0.68, 0.68):
                g.box((x, y - 0.14, z + dz), (1.5, 0.06, 0.08), M["white_paint"])
            g.box((x, y - 0.14, z), (0.05, 0.06, 1.3), M["white_paint"])
            g.box((x, y - 0.14, z + 0.25), (1.4, 0.06, 0.04), M["white_paint"])
            g.box((x, y - 0.2, z - 0.74), (1.6, 0.2, 0.07), M["coping"])
            g.box((x, y - 0.12, z + 0.8), (1.64, 0.06, 0.18), M["brick_n"])
    g.box((24.5, y - 0.14, 0.75), (0.4, 0.08, 0.5), M["plastic_w"])                            # the meter box
    g.box((3.0, y - 0.12, 2.3), (0.2, 0.1, 0.3), M["lantern"])
    return g.finish(coll)


def build_outside(coll, M):
    """Alder Lane: the road, kerbs and markings, the pavement, the front gardens and their low
    walls, the shared path, No. 14's gate and path with her chalk hopscotch, the porch, the
    neighbours, street lamps, trees, the black car; behind the house, the garden and the play area."""
    g = S.Group("HomeOutside")
    r = random.Random(9)
    # the road
    g.box((10.0, -11.0, -0.16), (160.0, 14.0, 0.1), M["asphalt"])
    for k in range(32):
        g.box((-60.0 + k * 5.0, -11.0, -0.105), (2.4, 0.12, 0.01), M["white_paint"])
    for yy in (-4.6, -17.4):
        g.box((10.0, yy, -0.105), (160.0, 0.1, 0.01), M["white_paint"])
    for x in (-15.0, 9.0, 33.0, 57.0):
        g.box((x, -4.5, -0.1), (0.5, 0.3, 0.02), M["steel"])                                    # drains
        for k in range(5):
            g.box((x - 0.2 + k * 0.1, -4.5, -0.09), (0.03, 0.28, 0.01), M["steel_mid"])
    g.cyl((-2.0, -9.0, -0.105), 0.35, 0.02, M["steel_mid"], n=16)
    g.box((10.0, -4.25, -0.03), (160.0, 0.18, 0.16), M["kerb"])                                 # kerbs and pavements
    g.box((10.0, -3.4, -0.05), (160.0, 1.6, 0.1), M["paving"])
    g.box((10.0, -17.75, -0.03), (160.0, 0.18, 0.16), M["kerb"])
    g.box((10.0, -18.6, -0.05), (160.0, 1.6, 0.1), M["paving"])
    # the front gardens (the hero comes along the shared path at y 0) and their low walls
    g.box((-24.0, 1.0, -0.06), (48.0, 8.4, 0.1), M["grass"])
    g.box((-15.0, 0.0, -0.01), (22.0, 1.2, 0.04), M["path"])                                  # the shared path
    g.box((-2.0, 0.0, -0.01), (4.0, 1.4, 0.04), M["path"])                                    # No. 14's path to the porch
    g.box((-3.05, -1.55, -0.01), (1.1, 1.7, 0.04), M["path"])                                 # and from the gate
    for (a, b) in ((-48.0, -3.7), (-2.35, -0.25)):
        g.box(((a + b) / 2, -2.4, 0.38), (b - a, 0.25, 0.76), M["brick"])
        g.box(((a + b) / 2, -2.4, 0.8), (b - a, 0.34, 0.08), M["coping"])
    for px in (-3.85, -2.2, -8.6, -25.4):
        g.box((px, -2.4, 0.5), (0.34, 0.34, 1.0), M["brick"])
        g.box((px, -2.4, 1.04), (0.42, 0.42, 0.08), M["coping"])
    g.box((-2.38, -2.58, 0.75), (0.22, 0.02, 0.15), M["white_paint"])                         # "14"
    S.stencil(g, "14", (-2.38, -2.6, 0.7), 0.12, M["ink"])
    # the gate, a metal one, swung open into the garden
    G = Loc(g, (-3.65, -2.4, 0.0), 62)
    for z in (0.12, 0.92):
        G.box((0.5, 0, z), (1.0, 0.04, 0.04), M["steel"])
    for k in range(9):
        G.box((0.05 + k * 0.112, 0, 0.52), (0.022, 0.022, 0.84), M["steel"])
    G.box((0.85, 0, 0.6), (0.06, 0.03, 0.06), M["steel_mid"])
    # her chalk on the path: a hopscotch, 1 2 3 (and on to "HOME"), a sun, the chalk itself
    hx = -3.35
    for k, num in enumerate("123"):
        cx = hx + k * 0.5
        for (dx, dy, sx, sy) in ((0, -0.23, 0.48, 0.022), (0, 0.23, 0.48, 0.022), (-0.24, 0, 0.022, 0.48), (0.24, 0, 0.022, 0.48)):
            g.box((cx + dx, dy, 0.032), (sx, sy, 0.003), M["chalk"])
        flat_text(g, num, (cx, 0.0, 0.031), 0.28, M["chalk"], rot_z=-90)
    for dy in (-0.25, 0.25):                                                                   # 4 | 5
        for (dx, ddy, sx, sy) in ((0, -0.2, 0.42, 0.02), (0, 0.2, 0.42, 0.02), (0.22, 0, 0.02, 0.42)):
            g.box((hx + 1.5 + dx, dy + ddy, 0.032), (sx, sy, 0.003), M["chalk"])
    flat_text(g, "4", (hx + 1.5, -0.25, 0.031), 0.22, M["chalk"], rot_z=-90)
    flat_text(g, "5", (hx + 1.5, 0.25, 0.031), 0.22, M["chalk"], rot_z=-90)
    g.cyl((-0.85, 0.45, 0.031), 0.13, 0.003, M["chalk"], n=16)
    for k in range(8):
        a = k / 8 * 360
        g.box((-0.85 + math.cos(math.radians(a)) * 0.22, 0.45 + math.sin(math.radians(a)) * 0.22, 0.031), (0.1, 0.018, 0.003), M["chalk"], rot=(0, 0, a))
    g.cyl((-1.3, -0.5, 0.035), 0.012, 0.07, M["chalk"], rot=(0, 90, 30), n=6)
    # the porch: steps, the canopy on brackets, the lantern, plants in pots, the boot scraper
    g.box((X0 - 0.8, 0.0, 0.08), (1.6, 1.8, 0.16), M["concrete"])
    g.box((X0 - 1.75, 0.0, 0.04), (0.3, 1.6, 0.08), M["concrete"])
    g.box((X0 - 0.8, 0.0, 2.5), (1.8, 2.0, 0.1), M["roof"])
    g.box((X0 - 0.8, 0.0, 2.44), (1.84, 2.04, 0.04), M["white_paint"])
    for s in (-1, 1):
        g.beam((X0 - 0.12, s * 0.95, 1.9), (X0 - 1.4, s * 0.95, 2.42), 0.06, 0.06, M["white_paint"])
    g.box((X0 - 0.14, -0.9, 2.05), (0.1, 0.16, 0.26), M["lantern"])
    g.box((X0 - 0.14, -0.9, 2.2), (0.12, 0.18, 0.04), M["steel"])
    for s in (-1, 1):
        g.cyl((X0 - 1.3, s * 0.72, 0.36), 0.16, 0.4, M["ceramic"], n=12, r2=0.12)
        D.boulder(g, (X0 - 1.3, s * 0.72, 0.68), 0.22, M["leaf"], squash=0.9)
    for k in range(3):
        g.cyl((X0 - 0.35 + k * 0.08, -0.75, 0.25), 0.03, 0.18, M["glass"], n=8)                # milk bottles
    # the neighbours: No. 12 and No. 10 along the lane, No. 16 past the garden, the houses opposite
    # and the backs of the next street over the garden fence
    house(g, -24.0, -9.0, 3.0, 7.0, M, 1, door=0.75)
    house(g, -40.0, -25.0, 3.0, 7.0, M, 2, door=0.2)
    house(g, 62.0, 76.0, -1.8, 7.0, M, 3, door=0.2)
    for k in range(6):
        house(g, -40.0 + k * 17.0, -26.0 + k * 17.0, -33.0, 7.0, M, 10 + k, facing=1, door=0.5)
    for k in range(5):
        house(g, 24.0 + k * 14.0, 36.0 + k * 14.0, 16.0, 6.0, M, 20 + k, door=0.5)
    for (a, b, hy) in ((-24.0, -9.0, -1.6), (-40.0, -25.0, -1.6)):                             # their hedges
        g.box(((a + b) / 2, hy, 0.45), (b - a - 1.6, 0.6, 0.9), M["leaf"])
    for k in range(12):                                                                        # shrubs by No. 12
        D.boulder(g, (-23.0 + k * 1.2, 2.4, 0.3), r.uniform(0.35, 0.5), M["leaf"], squash=0.8)
    # street lamps, trees
    for x in (-38.0, -22.0, -6.0, 66.0, 82.0):
        lamp_post(g, x, -3.0, M)
    for x in (-30.0, 10.0, 50.0):
        lamp_post(g, x, -19.0, M, toward=1)
    for (tx, ty, h, rad) in ((-31.0, 1.2, 7.0, 2.0), (-12.0, 9.0, 8.0, 2.4), (-44.0, 9.5, 7.5, 2.2), (20.0, 12.0, 8.5, 2.6),
                             (44.0, 12.5, 7.0, 2.2), (58.0, 11.0, 7.5, 2.0), (-20.0, -22.0, 6.5, 2.0), (16.0, -22.0, 7.0, 2.1),
                             (40.0, -22.0, 6.5, 2.0), (57.6, -1.4, 5.5, 1.8)):
        tree(g, tx, ty, M, h=h, r=rad, seed=int(tx * 3 + ty))
    # the black car two doors down
    cx, cy = -18.0, -5.2
    prof = [(-2.35, 0.32), (2.35, 0.32), (2.4, 0.85), (1.25, 0.95), (0.55, 1.45), (-1.05, 1.45), (-1.85, 0.95), (-2.4, 0.9)]
    g.prism([(cx + px, pz - 0.15) for px, pz in prof], cy - 0.85, cy + 0.85, M["car_black"])
    for wx in (-1.5, 1.5):
        for wy in (cy - 0.9, cy + 0.9):
            g.cyl((cx + wx, wy, 0.19), 0.34, 0.22, M["tyre"], rot=(90, 0, 0), n=16)
    # ---- the garden behind (the back door opens onto it) and the play area at its end
    g.box((47.0, 2.2, -0.06), (26.0, 9.6, 0.1), M["grass"])
    g.box((35.8, 1.1, -0.005), (3.4, 5.0, 0.03), M["paving"])                                 # the patio
    for k in range(8):                                                                         # stepping stones
        g.cyl((38.2 + k * 1.15, 1.0 + 0.35 * math.sin(k * 0.9), 0.0), 0.24, 0.03, M["paving"], n=10)
    wheelie_bin(g, 35.0, 3.55, M)
    wheelie_bin(g, 35.75, 3.55, M)
    g.box((X1 + 0.13, -0.95, 2.1), (0.1, 0.16, 0.26), M["lantern"])                             # the light by the back door
    # the fences: panels between posts (back and far side), the house side; a hedge along the front
    for (a, b, y, axis) in ((34.0, 60.0, 7.0, "x"), (-2.6, 7.0, 60.0, "y"), (4.45, 7.0, 34.05, "y")):
        n = max(1, int(round((b - a) / 1.8)))
        for k in range(n + 1):
            p = a + (b - a) * k / n
            pos = (p, y, 0.9) if axis == "x" else (y, p, 0.9)
            g.box(pos, (0.1, 0.1, 1.8), M["wood_furn"])
            g.box((pos[0], pos[1], 1.84), (0.14, 0.14, 0.04), M["wood_furn"])
        for k in range(n):
            p = a + (b - a) * (k + 0.5) / n
            if axis == "x":
                g.box((p, y, 0.92), ((b - a) / n - 0.1, 0.04, 1.6), M["fence"])
            else:
                g.box((y, p, 0.92), (0.04, (b - a) / n - 0.1, 1.6), M["fence_y"])
    g.box((47.1, -2.55, 0.45), (25.8, 0.6, 0.9), M["leaf"])
    for k in range(20):
        D.boulder(g, (34.6 + k * 1.3, -2.55, 0.85), r.uniform(0.3, 0.42), M["leaf"], squash=0.7)
    # the flower beds along the back fence
    g.box((47.0, 6.6, -0.02), (25.6, 0.7, 0.04), M["soil"])
    for k in range(18):
        D.boulder(g, (35.0 + k * 1.4 + r.uniform(-0.3, 0.3), 6.6, 0.25), r.uniform(0.25, 0.45), M["leaf"], squash=0.85)
        if k % 2:
            for j in range(3):
                g.cyl((35.5 + k * 1.4 + j * 0.15, 6.35, 0.25), 0.008, 0.5, M["leaf"], n=4)
                ball(g, (35.5 + k * 1.4 + j * 0.15, 6.35, 0.52), 0.05, M["paper"], n=6)
    # the washing line and her clothes on it
    for px in (38.0, 45.0):
        g.cyl((px, 5.7, 1.0), 0.04, 2.0, M["steel_mid"], n=8)
    pts = [(38.0 + k * 1.0, 5.7, 1.92 - 0.12 * math.sin(math.pi * k / 7)) for k in range(8)]
    for a, b in zip(pts, pts[1:]):
        g.beam(a, b, 0.008, 0.008, M["paper"])
    for k, (px, w, h, m) in enumerate(((38.8, 0.32, 0.42, M["fabric_pale"]), (39.6, 0.5, 0.6, M["curtain"]), (40.5, 0.26, 0.34, M["toy"][0]),
                                       (41.3, 0.6, 0.45, M["fabric_pale"]), (42.3, 0.22, 0.4, M["toy"][3]), (43.1, 0.36, 0.5, M["curtain_kid"]),
                                       (43.9, 0.12, 0.2, M["toy"][1]), (44.25, 0.12, 0.2, M["toy"][1]))):
        zt = 1.92 - 0.12 * math.sin(math.pi * (px - 38.0) / 7.0)
        g.box((px, 5.7, zt - h / 2), (w, 0.02, h), m, rot=(0, r.uniform(-3, 3), 0))
        if w > 0.3:
            for s in (-1, 1):
                g.box((px + s * (w / 2 + 0.06), 5.7, zt - 0.08), (0.14, 0.02, 0.12), m, rot=(0, s * 30, 0))
        for s in (-1, 1):
            g.box((px + s * w * 0.4, 5.7, zt), (0.012, 0.03, 0.06), M["toy"][2])
    g.box((40.8, 5.7, 0.2), (0.4, 0.3, 0.4), M["fabric_pale"], rot=(0, 0, 15))                 # the peg basket
    # the shed in the corner
    sx, sy = 57.4, 5.4
    g.box((sx, sy, 1.0), (2.4, 1.8, 2.0), M["shed"])
    g.box((sx, sy, 2.08), (2.6, 2.1, 0.08), M["roof"], rot=(-8, 0, 0))
    g.box((sx - 0.3, sy - 0.91, 0.9), (0.75, 0.04, 1.8), M["shed"])
    g.box((sx + 0.05, sy - 0.94, 1.0), (0.04, 0.03, 0.12), M["steel"])
    g.box((sx + 0.75, sy - 0.91, 1.3), (0.6, 0.04, 0.5), M["glass"])
    g.box((sx, sy - 0.6, 0.05), (2.4, 0.4, 0.1), M["concrete"])
    # the play area: the slide (ladder, platform, rails), the swings, a sandpit, a bike, a ball
    g.beam((47.0, 3.2, 0.0), (47.0, 3.2, 1.9), 0.06, 0.06, M["steel"])
    g.beam((47.6, 3.2, 0.0), (47.6, 3.2, 1.9), 0.06, 0.06, M["steel"])
    g.beam((47.3, 3.2, 1.85), (50.2, 3.2, 0.25), 0.55, 0.04, M["fabric_pale"])
    for s in (-1, 1):
        g.beam((47.3, 3.2 + s * 0.29, 1.95), (50.2, 3.2 + s * 0.29, 0.35), 0.03, 0.08, M["fabric_pale"])
        g.beam((46.4, 3.2 + s * 0.25, 0.0), (47.0, 3.2 + s * 0.25, 1.85), 0.05, 0.05, M["steel"])
        g.beam((47.0, 3.2 + s * 0.3, 1.85), (47.0, 3.2 + s * 0.3, 2.6), 0.04, 0.04, M["steel"])
        g.beam((47.6, 3.2 + s * 0.3, 1.85), (47.6, 3.2 + s * 0.3, 2.6), 0.04, 0.04, M["steel"])
        g.beam((47.0, 3.2 + s * 0.3, 2.6), (47.6, 3.2 + s * 0.3, 2.6), 0.04, 0.04, M["steel"])
    g.box((47.3, 3.2, 1.86), (0.64, 0.64, 0.05), M["oak_top"])
    for k in range(6):
        t = (k + 0.5) / 6
        g.beam((46.4 + 0.6 * t, 2.95, 1.85 * t), (46.4 + 0.6 * t, 3.45, 1.85 * t), 0.035, 0.035, M["steel_mid"])
    for x in (52.0, 55.2):                                                                     # the swing frame (the seat is separate)
        g.beam((x, 2.4, 0.0), (x, 3.2, 2.4), 0.07, 0.07, M["steel"])
        g.beam((x, 4.0, 0.0), (x, 3.2, 2.4), 0.07, 0.07, M["steel"])
        g.beam((x, 2.6, 0.6), (x, 3.8, 0.6), 0.04, 0.04, M["steel"])
    g.beam((52.0, 3.2, 2.4), (55.2, 3.2, 2.4), 0.08, 0.08, M["steel"])
    for dx in (-0.25, 0.25):
        g.box((53.6 + dx, 3.2, 2.36), (0.04, 0.06, 0.06), M["steel_mid"])
    g.box((53.6, 3.2, 0.005), (1.0, 0.8, 0.02), M["soil"])                                     # worn grass under it
    sp = (43.8, 4.7)
    for s in (-1, 1):
        g.box((sp[0] + s * 0.78, sp[1], 0.12), (0.08, 1.6, 0.24), M["oak"])
        g.box((sp[0], sp[1] + s * 0.78, 0.12), (1.6, 0.08, 0.24), M["oak"])
    g.box((sp[0], sp[1], 0.08), (1.5, 1.5, 0.16), M["sand"])
    g.cyl((sp[0] + 0.3, sp[1] - 0.2, 0.24), 0.08, 0.14, M["toy"][0], n=10, r2=0.06)
    g.beam((sp[0] - 0.3, sp[1] + 0.2, 0.18), (sp[0] - 0.1, sp[1] + 0.35, 0.3), 0.06, 0.02, M["toy"][1])
    B = Loc(g, (41.2, 3.0, 0.0), 25)                                                            # her bike, on its side
    for dx in (-0.35, 0.35):
        B.cyl((dx, 0, 0.05), 0.22, 0.04, M["tyre"], n=14)
        B.cyl((dx, 0, 0.06), 0.18, 0.045, M["grass"], n=14)
    B.beam((-0.35, 0, 0.08), (0.05, 0, 0.09), 0.03, 0.03, M["toy"][0])
    B.beam((0.05, 0, 0.09), (0.35, 0, 0.08), 0.03, 0.03, M["toy"][0])
    B.beam((0.3, 0.05, 0.08), (0.3, 0.32, 0.1), 0.025, 0.025, M["steel_mid"])
    B.box((-0.1, -0.02, 0.1), (0.2, 0.1, 0.04), M["ink"])
    ball(g, (49.2, 0.4, 0.11), 0.11, M["paper"], n=10)
    # the city behind, a dusk sky
    for k in range(16):
        x = -30.0 + k * 7.0 + rnd.uniform(-2, 2)
        h = rnd.uniform(10.0, 26.0)
        y = rnd.uniform(35.0, 55.0)
        w = rnd.uniform(6, 9)
        g.box((x, y, h / 2), (w, 6.0, h), M["city"])
        for j in range(int(h / 3)):
            if rnd.random() < 0.45:
                g.box((x + rnd.uniform(-w, w) * 0.3, y - 3.02, 1.5 + j * 3.0), (rnd.uniform(0.6, 2.0), 0.04, 0.5), M["win_lit"])
    g.box((15.0, 90.0, 30.0), (300.0, 1.0, 60.0), M["sky_dusk"])
    return g.finish(coll)


def build_swing(coll, M):
    g = S.Group("Swing")
    x, y = 53.6, 3.2
    for dx in (-0.25, 0.25):
        g.beam((x + dx, y, 2.38), (x + dx, y, 0.55), 0.015, 0.015, M["steel"])
    g.box((x, y, 0.52), (0.62, 0.24, 0.05), M["wood_furn"])
    return g.finish(coll)


# ---------------------------------------------------------------- the story props (separate objects)

def finish_prop(g, coll):
    """Finish a prop group with its origin at its base centre, so the game can frame it."""
    ob = g.finish(coll)
    vs = [v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    ob.data.transform(Matrix.Translation(-c))
    ob.location = c
    return ob


HALL_TABLE = (2.6, WALL - 0.2)      # x, y of the hall table (Blender)
TABLE_TOP = 0.82


def build_props(coll, M):
    props = []
    # ---- the hall table: a console with a drawer, a lamp, the key bowl, Elena's photo
    g = S.Group("HallTable")
    tx, ty = HALL_TABLE
    g.box((tx, ty, TABLE_TOP - 0.02), (1.0, 0.38, 0.04), M["wood_top"])
    g.box((tx, ty, TABLE_TOP - 0.1), (0.92, 0.32, 0.12), M["wood_furn"])
    g.box((tx, ty - 0.165, TABLE_TOP - 0.1), (0.5, 0.012, 0.09), M["wood_furn"])
    g.cyl((tx, ty - 0.18, TABLE_TOP - 0.1), 0.016, 0.025, M["brass"], rot=(90, 0, 0), n=8)
    for dx in (-0.45, 0.45):
        for dy in (-0.15, 0.15):
            g.box((tx + dx, ty + dy, (TABLE_TOP - 0.16) / 2), (0.04, 0.04, TABLE_TOP - 0.16), M["wood_furn"])
    g.box((tx, ty, 0.12), (0.88, 0.28, 0.02), M["wood_furn"])
    g.box((tx + 0.25, ty, 0.17), (0.3, 0.2, 0.08), M["fabric"])                                 # a basket of gloves underneath
    g.cyl((tx - 0.36, ty + 0.05, TABLE_TOP + 0.02), 0.07, 0.04, M["ceramic"], n=12)           # the lamp
    g.cyl((tx - 0.36, ty + 0.05, TABLE_TOP + 0.18), 0.012, 0.28, M["brass"], n=6)
    g.cyl((tx - 0.36, ty + 0.05, TABLE_TOP + 0.36), 0.13, 0.16, M["fabric_pale"], n=12, r2=0.09)
    g.cyl((tx + 0.33, ty + 0.03, TABLE_TOP + 0.025), 0.1, 0.05, M["ceramic"], n=14, r2=0.07)   # the key bowl, keys
    g.torus((tx + 0.33, ty + 0.03, TABLE_TOP + 0.05), 0.02, 0.004, M["chrome"], n1=8, n2=4)
    g.box((tx + 0.36, ty + 0.01, TABLE_TOP + 0.052), (0.05, 0.012, 0.004), M["chrome"], rot=(0, 0, 30))
    standing_frame(g, g, (tx + 0.08, ty + 0.1, TABLE_TOP), 0.18, 0.23, M, yaw=-6)              # Elena's photo
    props.append(finish_prop(g, coll))
    # ---- Harrow's card: a white business card, the print on it
    g = S.Group("HarrowCard")
    cx, cy = tx - 0.12, ty - 0.08
    L = Loc(g, (cx, cy, TABLE_TOP), -14)
    L.box((0, 0, 0.0012), (0.09, 0.052, 0.0024), M["paper"])
    L.box((-0.012, 0.01, 0.0026), (0.05, 0.007, 0.0006), M["ink"])                              # the name
    for k in range(3):
        L.box((-0.02, -0.006 - k * 0.007, 0.0026), (0.034 - k * 0.006, 0.0025, 0.0006), M["ink"])
    L.box((0.03, 0.012, 0.0026), (0.014, 0.014, 0.0006), M["ink"])                              # the crest
    props.append(finish_prop(g, coll))
    # ---- Elena's note: folded once, standing open like a tent against the card, written on
    g = S.Group("ElenaNote")
    nx, ny = tx - 0.02, ty - 0.02
    L = Loc(g, (nx, ny, TABLE_TOP), 10)
    for s in (-1, 1):
        L.box((0, s * 0.032, 0.026), (0.15, 0.002, 0.07), M["paper"], rot=(s * 25, 0, 0))
    for k in range(4):
        L.box((-0.005 + (k % 2) * 0.008, -0.048, 0.04 - k * 0.011), (0.11 - (k == 3) * 0.05, 0.0006, 0.0025), M["books"][0], rot=(-25, 0, 0))
    props.append(finish_prop(g, coll))
    # ---- the family photo on the study desk: a silver frame, the three of them (her on his shoulders)
    g = S.Group("FamilyPhoto")
    L = Loc(g, (11.72, 3.5, 0.775), 14)
    fw, fh = 0.22, 0.27
    L.box((0, 0, fh / 2 + 0.005), (fw, 0.016, fh), M["chrome"], rot=(-10, 0, 0))
    L.box((0, 0.055, fh * 0.4), (0.02, 0.012, fh * 0.8), M["chrome"], rot=(25, 0, 0))
    up = Vector((0.0, math.sin(math.radians(10)), math.cos(math.radians(10))))       # the frame leans back 10 degrees
    nrm = Vector((0.0, -math.cos(math.radians(10)), math.sin(math.radians(10))))
    layer = [0]

    def P(dx, dz, s, m):
        layer[0] += 1
        q = Vector((dx, 0.0, fh / 2 + 0.005)) + up * dz + nrm * (0.009 + 0.0012 * layer[0])
        L.box(tuple(q), s, m, rot=(-10, 0, 0))
    P(0, 0, (fw * 0.84, 0.002, fh * 0.84), M["paper"])
    P(0, 0.02, (fw * 0.76, 0.002, fh * 0.6), M["books"][4])                                    # the sky
    P(0, -0.085, (fw * 0.76, 0.002, 0.05), M["books"][1])                                      # the sea wall
    P(-0.04, -0.03, (0.032, 0.002, 0.11), M["books"][0])                                       # him
    P(-0.04, 0.04, (0.026, 0.002, 0.028), M["books"][0])
    P(0.035, -0.04, (0.03, 0.002, 0.1), M["books"][3])                                         # Elena
    P(0.035, 0.025, (0.024, 0.002, 0.026), M["books"][3])
    P(-0.04, 0.08, (0.022, 0.002, 0.045), M["toy"][3])                                         # Maya on his shoulders
    P(-0.04, 0.112, (0.018, 0.002, 0.018), M["toy"][3])
    P(-0.03, 0.125, (0.012, 0.002, 0.006), M["toy"][2])                                        # her ribbon
    props.append(finish_prop(g, coll))
    # ---- the walkie-talkie left on Maya's shelf (its twin is gone: a clean gap in the dust beside it)
    g = S.Group("ShelfWalkie")
    wx, wy, wz = 15.42, WALL - 0.12, F + 1.263
    L = Loc(g, (wx, wy, wz), -8)
    L.box((0, 0, 0.065), (0.07, 0.04, 0.13), M["toy"][0])
    L.box((0, -0.021, 0.085), (0.05, 0.004, 0.04), M["toy"][2])                                 # the speaker grille
    for k in range(4):
        L.box((0, -0.0235, 0.07 + k * 0.01), (0.044, 0.002, 0.003), M["ink"])
    L.cyl((0, -0.022, 0.035), 0.01, 0.006, M["toy"][1], rot=(90, 0, 0), n=8)                  # the talk button
    L.box((0.0, 0.0, 0.003), (0.074, 0.044, 0.006), M["toy"][2])
    L.cyl((0.022, 0.0, 0.17), 0.006, 0.09, M["ink"], n=6)                                      # the aerial
    L.cyl((0.022, 0.0, 0.218), 0.009, 0.012, M["ink"], n=6)
    L.cyl((-0.02, 0.0, 0.136), 0.007, 0.012, M["toy"][1], n=6)
    for (ddx, ddy, sx, sy) in ((0, -0.024, 0.08, 0.004), (0, 0.024, 0.08, 0.004), (-0.04, 0, 0.004, 0.052), (0.04, 0, 0.004, 0.052)):
        g.box((wx + 0.13 + ddx, wy + ddy, wz + 0.0008), (sx, sy, 0.0016), M["books"][2])        # where the other one stood: its outline in the dust
    props.append(finish_prop(g, coll))
    # ---- her drawing on the wall: the three of them holding hands under a sun
    g = S.Group("MayaDrawing")
    dx0, dz0 = 16.85, F + 1.45
    dy = WALL - 0.004
    g.box((dx0, dy, dz0), (0.44, 0.004, 0.32), M["paper"], rot=(0, 3, 0))
    g.box((dx0, dy - 0.003, dz0 - 0.12), (0.4, 0.002, 0.04), M["toy"][1])                      # the ground
    for k, (fx, s) in enumerate(((-0.1, 1.0), (0.0, 1.0), (0.09, 0.65))):
        bx = dx0 + fx
        g.box((bx, dy - 0.004, dz0 - 0.05 * s), (0.008, 0.002, 0.1 * s), M["ink"])              # body
        g.cyl((bx, dy - 0.004, dz0 + 0.02 * s), 0.022 * s, 0.002, M["ink"], rot=(90, 0, 0), n=10)   # head
        for sl in (-1, 1):
            g.box((bx + sl * 0.012, dy - 0.004, dz0 - 0.125 * s), (0.006, 0.002, 0.06 * s), M["ink"], rot=(0, sl * 20, 0))
        g.box((bx, dy - 0.004, dz0 - 0.03 * s), (0.1 if k < 2 else 0.06, 0.002, 0.006), M["ink"])   # arms, holding hands
    g.cyl((dx0 + 0.15, dy - 0.004, dz0 + 0.1), 0.035, 0.002, M["toy"][0], rot=(90, 0, 0), n=12)   # the sun
    for k in range(8):
        a = k / 8 * 360
        g.box((dx0 + 0.15 + math.cos(math.radians(a)) * 0.055, dy - 0.004, dz0 + 0.1 + math.sin(math.radians(a)) * 0.055),
              (0.025, 0.002, 0.005), M["toy"][0], rot=(0, -a, 0))
    for sx in (-1, 1):
        g.box((dx0 + sx * 0.21, dy - 0.006, dz0 + 0.15), (0.05, 0.002, 0.02), M["plastic_w"], rot=(0, sx * 40, 0))
    props.append(finish_prop(g, coll))
    # ---- the plastic sheeting on the kitchen floor: spread out, taped down at one edge, half done
    g = S.Group("CleanSheeting")
    sheet(g, 30.4, 33.75, -1.35, 1.65, 0.012, M["sheeting"], res=0.12, amp=0.02, seed=2.0)
    sheet(g, 30.6, 31.3, 1.65, 2.25, 0.012, M["sheeting"], res=0.1, amp=0.05, seed=7.0)         # a fold lifted
    for (a, b, y) in ((30.45, 33.7, -1.33),):
        g.box(((a + b) / 2, y, 0.016), (b - a, 0.05, 0.003), M["steel_mid"])                    # the tape
    for k in range(3):
        g.box((30.42, -1.0 + k * 1.0, 0.016), (0.05, 0.22, 0.003), M["steel_mid"])
    props.append(finish_prop(g, coll))
    # ---- the bleach, and the bucket by the back door, a brush across it
    g = S.Group("Bleach")
    bx, by = 33.05, 1.18
    g.cyl((bx, by, 0.13), 0.065, 0.26, M["plastic_w"], n=14)
    g.cyl((bx, by, 0.285), 0.06, 0.05, M["plastic_w"], n=14, r2=0.03)
    g.cyl((bx, by, 0.325), 0.022, 0.035, M["toy"][1], n=10)
    g.box((bx, by - 0.064, 0.13), (0.09, 0.004, 0.12), M["paper"])                               # the label
    g.box((bx, by - 0.067, 0.16), (0.07, 0.002, 0.018), M["ink"])
    g.box((bx, by - 0.067, 0.11), (0.05, 0.002, 0.03), M["ink"])
    ux, uy = 33.38, 1.45
    g.cyl((ux, uy, 0.15), 0.16, 0.3, M["plastic_d"], n=16, r2=0.135)
    g.cyl((ux, uy, 0.28), 0.15, 0.01, M["sheeting"], n=16)                                      # the water, grey
    g.torus((ux, uy, 0.3), 0.165, 0.01, M["plastic_d"], n1=16, n2=4)
    for k in range(7):                                                                          # the handle, a half hoop
        a0 = math.radians(k * 180 / 7)
        a1 = math.radians((k + 1) * 180 / 7)
        g.beam((ux + math.cos(a0) * 0.16, uy, 0.3 + math.sin(a0) * 0.12), (ux + math.cos(a1) * 0.16, uy, 0.3 + math.sin(a1) * 0.12), 0.008, 0.008, M["steel_mid"])
    g.box((ux, uy, 0.33), (0.22, 0.06, 0.04), M["oak"], rot=(0, 0, 30))                         # the scrubbing brush
    props.append(finish_prop(g, coll))
    # ---- the kitchen chair, set in the middle of the kitchen (where the story ties the Cleaner)
    g = S.Group("KitchenChair")
    chair(g, (29.2, 0.9, 0.0), 0, M)
    props.append(finish_prop(g, coll))
    return props


# ---------------------------------------------------------------- lights, export

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
    # the low sun from behind the house, through the back windows: long shafts across the floors
    sun = add("SUN", "Sun", (0, 0, 50), 2.6)
    sun.rotation_euler = (math.radians(74), 0, math.radians(180 + 20))
    sun.data.angle = math.radians(3)
    for (x, z0, w, h) in WIN1:
        add("AREA", "WindowGlow", (x, YB - 0.5, z0 + h / 2), 45, size=1.6, rot=(math.radians(90), 0, 0))
    for (x, z0, w, h) in WIN2:
        add("AREA", "WindowGlow2", (x, YB - 0.5, z0 + h / 2), 38, size=1.4, rot=(math.radians(90), 0, 0))
    # the lamps that are on: the study's, the living room's, Maya's fairy lights, the bedside
    for (p, e) in (((12.7, 3.4, 1.1), 30), ((24.2, 3.2, 1.7), 45), ((18.0, 3.5, FLOOR2 + 2.3), 20), ((26.8, 3.6, FLOOR2 + 0.9), 25)):
        add("POINT", "Lamp", p, e, size=0.2)
    # outside: the porch lantern, the back door's light, the street lamps
    add("POINT", "Porch", (X0 - 0.35, -0.9, 2.0), 40, size=0.1)
    add("POINT", "BackLight", (X1 + 0.35, -0.95, 2.0), 30, size=0.1)
    for x in (-38.0, -22.0, -6.0, 66.0, 82.0):
        add("POINT", "StreetLamp", (x, -3.8, 5.2), 220, size=0.2)
    # a soft fill from the camera side so the lane reads
    add("AREA", "Fill", (17.0, -9.0, 3.5), 1500, size=26.0, rot=(math.radians(78), 0, 0))
    add("AREA", "StreetFill", (-12.0, -10.0, 4.0), 1600, size=24.0, rot=(math.radians(75), 0, 0))
    add("AREA", "GardenFill", (47.0, -8.0, 4.0), 1300, size=22.0, rot=(math.radians(75), 0, 0))
    w = bpy.data.worlds.new("Dusk")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.26, 0.26, 0.27, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    bpy.context.scene.world = w


def lane_check(objs):
    """Report any geometry standing in the fight lane inside the rooms (|y| < LANE, between 3 cm and
    1.9 m above a floor), away from the doorways in the cross walls."""
    walls = list(DOORS1[1:]) + list(DOORS2) + [X0, X1]
    bad = []
    for ob in objs:
        mw = ob.matrix_world
        for v in ob.data.vertices:
            p = mw @ v.co
            if not (X0 + 0.2 < p.x < X1 - 0.2) or abs(p.y) >= LANE:
                continue
            for fz in (0.0, F):
                if fz + 0.035 < p.z < fz + 1.9 and min(abs(p.x - w) for w in walls) > 0.2:
                    bad.append((ob.name, round(p.x, 2), round(p.y, 2), round(p.z, 2)))
                    break
    print("lane check:", len(bad), "vertices in the lane")
    seen = {}
    for b in bad:
        seen.setdefault((b[0], round(b[1]), round(b[3])), b)
    for v in seen.values():
        print("   ", v)


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def export(objs):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "HomeSet.fbx"), use_selection=True,
                             object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             path_mode="STRIP", embed_textures=False)
    print("exported HomeSet.fbx")


def main():
    preview_only = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    S._tower_to_hall()
    S.MATS.clear()
    D.MATS.clear()
    S.OUT = OUT
    coll = bpy.context.scene.collection
    M = mats()
    M["water"] = D.flat("TapWater", 0.6, rough=0.05)
    shell, det = build_house(coll, M)
    front = build_front(coll, M)
    outside = build_outside(coll, M)
    swing = build_swing(coll, M)
    props = build_props(coll, M)
    for o in (shell, front):
        D.bevel(o, 0.012)
    # (the street is left unbevelled: on its rocks and hedges a bevel triples the count for nothing)
    lights()
    parts = [shell, det, front, outside, swing] + props
    for o in parts:
        print(f"  {o.name:14s} {tri_count(o):7d} tris  at {tuple(round(c, 2) for c in o.location)}")
    print("  total", sum(tri_count(o) for o in parts), "tris")
    lane_check([shell, det] + [p for p in props if p.name not in ("CleanSheeting", "KitchenChair")])
    if preview_only:
        front.hide_render = True
        S.PREVIEW = os.path.join(ROOT, "docs", "previews")
        only = [a for a in sys.argv if a.startswith("--sheet=")]
        sheets = [s.split("=", 1)[1] for s in only] or ["set", "props"]
        if "set" in sheets:
            cams = [
                ("H2  Alder Lane at dusk", S.shot_camera("P1", (-24.0, 2.6, -14.0), (-4.0, 1.4, 2.0), 46), None),
                ("H3  the path, the hopscotch", S.shot_camera("P2", (-2.4, 1.6, -2.2), (-2.2, 0.0, 0.2), 50), None),
                ("H4  the hall (gameplay)", S.shot_camera("P3", (5.0, 1.8, -10.5), (5.0, 1.4, 1.0), 38), None),
                ("H5  the study (gameplay)", S.shot_camera("P4", (12.0, 1.8, -10.5), (12.0, 1.4, 1.0), 38), None),
                ("H12  the living room (gameplay)", S.shot_camera("P5", (20.0, 1.8, -10.5), (20.0, 1.4, 1.0), 38), None),
                ("H8  the kitchen", S.shot_camera("P6", (29.5, 1.8, -10.5), (29.5, 1.3, 1.0), 38), None),
                ("the landing (gameplay)", S.shot_camera("P7", (8.0, FLOOR2 + 1.8, -10.0), (8.0, FLOOR2 + 1.3, 1.0), 38), None),
                ("H13  Maya's room", S.shot_camera("P8", (18.0, FLOOR2 + 1.8, -9.0), (18.0, FLOOR2 + 1.3, 2.0), 40), None),
                ("H14  the bedroom", S.shot_camera("P9", (28.0, FLOOR2 + 1.8, -10.0), (28.0, FLOOR2 + 1.2, 1.0), 40), None),
                ("H21  the garden, the swings", S.shot_camera("P10", (34.6, 1.4, 0.0), (52.0, 1.0, 3.0), 44), None),
            ]
            S.preview(cams, "home_set.png")
        if "props" in sheets:
            tz = TABLE_TOP
            cams = [
                ("3.1b  the hall table", S.shot_camera("Q1", (2.6, 1.35, 2.2), (2.6, 0.9, 3.9), 42), None),
                ("3.1b  HarrowCard, ElenaNote", S.shot_camera("Q2", (2.5, tz + 0.45, 3.45), (2.5, tz, 3.9), 34), None),
                ("3.2  FamilyPhoto on the desk", S.shot_camera("Q3", (11.85, 1.15, 2.6), (11.75, 0.88, 3.5), 30), None),
                ("3.3  ShelfWalkie, MayaDrawing", S.shot_camera("Q4", (16.0, FLOOR2 + 1.5, 2.2), (16.1, FLOOR2 + 1.35, 4.1), 40), None),
                ("3.4  CleanSheeting, Bleach (high wide)", S.shot_camera("Q5", (30.0, 2.7, -1.6), (32.4, 0.1, 0.9), 50), None),
                ("3.7  KitchenChair", S.shot_camera("Q6", (27.0, 1.5, -1.2), (29.3, 0.6, 1.2), 50), None),
            ]
            S.preview(cams, "home_set_props.png")
        return
    for o in parts:
        S.unwrap(o)
    front.hide_render = True
    S.bake(shell, 4096, 24)
    S.bake(det, 4096, 16)
    S.bake(outside, 4096, 16)
    S.bake(swing, 256, 8)
    for p in props:
        S.bake(p, 1024 if p.name in ("CleanSheeting", "HallTable") else 512, 12)
    front.hide_render = False
    S.bake(front, 2048, 12)
    front.hide_render = True
    marks = []
    for n, p in (("Mark_X", (10, 0, 0)), ("Mark_Y", (0, 10, 0)), ("Mark_Z", (0, 0, 10))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        coll.objects.link(e)
        marks.append(e)
    export(parts + marks)


if __name__ == "__main__":
    main()
