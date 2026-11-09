"""Chapter 1's dressing kit: the small things that make the warehouse read as a real place, at
the scale of a bolt. Structural steel detail (gussets, cleats, rod bracing, haunches, base
guards), services (pipe runs with flanges, brackets and valves; conduit with saddles and boxes;
ladder cable trays with their cables), safety kit (extinguishers and their fire-point boards,
caged bulkhead lamps, call points, signs), stores (shelving, pallets, wrapped and tarped loads,
gas bottles), the crew's table, the chair he was tied to, the Foreman's office, catwalks with
grating and railings, and outside: container hardware, chain-link and razor wire, high-mast
floodlights, barriers, the skyline in three layers of haze.

Everything is plain boxes and cylinders merged into the existing groups (no new objects, no
new names for Unity to find), in the tower's black and white. A few procedural surfaces (wet
concrete, wet asphalt, grating, hazard bands, grime) bake into the lightmap with the light.

Coordinates as build_story_set.py: Blender X along the lane, Y depth (+ away from the camera),
Z up. The fight lane is |y| < 0.8 at floor level: nothing here stands on it.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

R = random.Random(1907)
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FONT_PATH = os.path.join(ROOT, "Assets", "Resources", "Fonts", "BebasNeue-Regular.ttf")
LANE = 0.85                        # half-width of the lane band kept clear at floor level


# ---------------------------------------------------------------- materials

def _grey(g):
    return (g, g, g * 1.02, 1)


def _node_mat(name):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    return m, nt, nt.nodes["Principled BSDF"]


def _xy(nt, plane="xy", scale=1.0):
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    comb = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    a, b, c = {"xy": ("X", "Y", "Z"), "xz": ("X", "Z", "Y"), "yz": ("Y", "Z", "X")}[plane]
    nt.links.new(sep.outputs[a], comb.inputs["X"])
    nt.links.new(sep.outputs[b], comb.inputs["Y"])
    nt.links.new(sep.outputs[c], comb.inputs["Z"])
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (scale, scale, scale)
    nt.links.new(comb.outputs[0], mp.inputs[0])
    return mp.outputs[0]


def wet_concrete(name, g=0.10, wet_dark=0.55, patch_scale=0.22, plane="xy"):
    """Power-floated concrete: a mottle of greys, trowel swirls, darker wet patches where the
    rain has come in (glossy in the preview; the bake keeps the darker tone)."""
    m, nt, bsdf = _node_mat(name)
    co = _xy(nt, plane)
    mot = nt.nodes.new("ShaderNodeTexNoise")
    mot.inputs["Scale"].default_value = 1.6
    mot.inputs["Detail"].default_value = 9.0
    mot.inputs["Roughness"].default_value = 0.62
    nt.links.new(co, mot.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.3
    ramp.color_ramp.elements[0].color = _grey(g * 0.72)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = _grey(g * 1.28)
    nt.links.new(mot.outputs["Fac"], ramp.inputs["Fac"])
    # fine grit
    grit = nt.nodes.new("ShaderNodeTexNoise")
    grit.inputs["Scale"].default_value = 60.0
    grit.inputs["Detail"].default_value = 2.0
    nt.links.new(co, grit.inputs["Vector"])
    mixg = nt.nodes.new("ShaderNodeMix")
    mixg.data_type = "RGBA"
    mixg.blend_type = "MULTIPLY"
    mixg.inputs["Factor"].default_value = 0.35
    nt.links.new(ramp.outputs["Color"], mixg.inputs["A"])
    nt.links.new(grit.outputs["Color"], mixg.inputs["B"])
    # the wet: big soft patches
    wet = nt.nodes.new("ShaderNodeTexNoise")
    wet.inputs["Scale"].default_value = patch_scale
    wet.inputs["Detail"].default_value = 3.0
    nt.links.new(co, wet.inputs["Vector"])
    wr = nt.nodes.new("ShaderNodeValToRGB")
    wr.color_ramp.elements[0].position = 0.52
    wr.color_ramp.elements[0].color = (0, 0, 0, 1)
    wr.color_ramp.elements[1].position = 0.6
    wr.color_ramp.elements[1].color = (1, 1, 1, 1)
    nt.links.new(wet.outputs["Fac"], wr.inputs["Fac"])
    dark = nt.nodes.new("ShaderNodeMix")
    dark.data_type = "RGBA"
    dark.blend_type = "MULTIPLY"
    nt.links.new(wr.outputs["Color"], dark.inputs["Factor"])
    nt.links.new(mixg.outputs["Result"], dark.inputs["A"])
    dark.inputs["B"].default_value = (wet_dark, wet_dark, wet_dark, 1)
    nt.links.new(dark.outputs["Result"], bsdf.inputs["Base Color"])
    rough = nt.nodes.new("ShaderNodeMapRange")
    rough.inputs["To Min"].default_value = 0.82
    rough.inputs["To Max"].default_value = 0.12
    nt.links.new(wr.outputs["Color"], rough.inputs["Value"])
    nt.links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.12
    nt.links.new(grit.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return m


def grime_mat(name, g, plane="xz", amount=0.45, streak=True):
    """A painted or sheet surface with grime: vertical rain streaks and soft dark patches."""
    m, nt, bsdf = _node_mat(name)
    co = _xy(nt, plane)
    nz = nt.nodes.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 0.8
    nz.inputs["Detail"].default_value = 6.0
    nt.links.new(co, nz.inputs["Vector"])
    out = nz.outputs["Color"]
    if streak:
        # stretch a second noise vertically: streaks running down
        mp = nt.nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (7.0, 0.25, 1.0)
        nt.links.new(co, mp.inputs[0])
        st = nt.nodes.new("ShaderNodeTexNoise")
        st.inputs["Scale"].default_value = 1.5
        st.inputs["Detail"].default_value = 4.0
        nt.links.new(mp.outputs[0], st.inputs["Vector"])
        mixs = nt.nodes.new("ShaderNodeMix")
        mixs.data_type = "RGBA"
        mixs.blend_type = "MULTIPLY"
        mixs.inputs["Factor"].default_value = 1.0
        nt.links.new(nz.outputs["Color"], mixs.inputs["A"])
        nt.links.new(st.outputs["Color"], mixs.inputs["B"])
        out = mixs.outputs["Result"]
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = amount
    mix.inputs["A"].default_value = _grey(g)
    nt.links.new(out, mix.inputs["B"])
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.75
    return m


def grating_mat(name, g=0.16, plane="xy"):
    """Open steel grating: thin bright bars over dark holes."""
    m, nt, bsdf = _node_mat(name)
    co = _xy(nt, plane)
    br = nt.nodes.new("ShaderNodeTexBrick")
    br.offset = 0.0
    br.inputs["Scale"].default_value = 1.0
    br.inputs["Brick Width"].default_value = 0.034
    br.inputs["Row Height"].default_value = 0.1
    br.inputs["Mortar Size"].default_value = 0.006
    br.inputs["Mortar Smooth"].default_value = 0.0
    br.inputs["Color1"].default_value = _grey(0.012)
    br.inputs["Color2"].default_value = _grey(0.02)
    br.inputs["Mortar"].default_value = _grey(g)
    nt.links.new(co, br.inputs["Vector"])
    nt.links.new(br.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.5
    bsdf.inputs["Metallic"].default_value = 0.4
    return m


def hazard_mat(name, light=0.62, dark=0.03, width=7.0):
    """Diagonal hazard bands (the tower's white edges on black), running in world x+z."""
    m, nt, bsdf = _node_mat(name)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    wave = nt.nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.bands_direction = "DIAGONAL"
    wave.wave_profile = "SAW"
    wave.inputs["Scale"].default_value = width / 10.0
    wave.inputs["Distortion"].default_value = 0.0
    nt.links.new(tc.outputs["Object"], wave.inputs["Vector"])
    gt = nt.nodes.new("ShaderNodeMath")
    gt.operation = "GREATER_THAN"
    gt.inputs[1].default_value = 0.5
    nt.links.new(wave.outputs["Fac"], gt.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    nt.links.new(gt.outputs[0], mix.inputs["Factor"])
    mix.inputs["A"].default_value = _grey(dark)
    mix.inputs["B"].default_value = _grey(light)
    nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6
    return m


def add_mats(S, M):
    """The kit's own materials, added to M once per build (existing names are left alone)."""
    if "k_floor" in M:
        return M
    if hasattr(S, "H"):
        S.H.hall_materials(S, M)                       # rack paint, cardboard, wrap, tarp: shared with the hall
    M["k_floor"] = wet_concrete("WetConcreteFloor", 0.10)
    M["k_floor_hall"] = wet_concrete("WetConcreteHall", 0.10, patch_scale=0.16)
    M["k_asphalt"] = wet_concrete("WetAsphalt", 0.05, wet_dark=0.45, patch_scale=0.09)
    M["k_grating"] = grating_mat("Grating", 0.2)
    M["k_hazard"] = hazard_mat("HazardBands")
    M["k_sheet_grime"] = grime_mat("SheetGrime", 0.3, "xz", 0.55)
    M["k_stain"] = S.mat("WetStain", (0.045, 0.045, 0.048), 0.3)
    M["k_puddle"] = S.mat("PuddleFlat", (0.06, 0.06, 0.062), 0.04)
    M["k_puddle_yard"] = S.mat("PuddleYard", (0.032, 0.032, 0.034), 0.04)
    M["k_stain_wall"] = S.mat("WallStain", (0.16, 0.16, 0.165), 0.9)
    M["k_stain_pale"] = S.mat("WallStainPale", (0.22, 0.22, 0.225), 0.9)
    M["k_white"] = S.mat("SignWhite", (0.66, 0.66, 0.66), 0.6)
    M["k_black"] = S.mat("SignBlack", (0.025, 0.025, 0.027), 0.6)
    M["k_mid"] = S.mat("KitGreyMid", (0.2, 0.2, 0.205), 0.6)
    M["k_paper"] = S.mat("Paper", (0.6, 0.6, 0.58), 0.95)
    M["k_card_white"] = S.mat("PlayingCard", (0.72, 0.72, 0.7), 0.6)
    M["k_lens"] = S.mat("LampLens", (0.9, 0.9, 0.9), 0.3, emit=(1, 1, 1), strength=14.0)
    M["k_lens_dim"] = S.mat("LampLensDim", (0.5, 0.5, 0.5), 0.3, emit=(1, 1, 1), strength=2.5)
    M["k_exit"] = S.mat("ExitSign", (0.8, 0.8, 0.8), 0.4, emit=(1, 1, 1), strength=4.0)
    M["k_screen"] = S.mat("CRTGlow", (0.3, 0.3, 0.3), 0.3, emit=(1, 1, 1), strength=0.8)
    M["k_led"] = S.mat("LedRed", (0.6, 0.6, 0.6), 0.4, emit=(1, 1, 1), strength=6.0)
    M["k_flood"] = S.mat("FloodHead", (1, 1, 1), 0.4, emit=(1, 1, 1), strength=45.0)
    M["k_zip"] = S.mat("ZipTie", (0.02, 0.02, 0.02), 0.4)
    M["k_blood"] = S.mat("DriedBlood", (0.03, 0.028, 0.028), 0.25)
    M["k_haze1"] = S.mat("CityHazeNear", (0.055, 0.056, 0.06), 0.8)
    M["k_haze2"] = S.mat("CityHazeMid", (0.075, 0.076, 0.08), 0.9)
    M["k_haze3"] = S.mat("CityHazeFar", (0.10, 0.10, 0.105), 1.0)
    M["k_city_win_dim"] = S.mat("CityWindowsDim", (0.3, 0.3, 0.3), 0.5, emit=(1, 1, 1), strength=1.2)
    return M


# ---------------------------------------------------------------- primitives

def bbox(g, c, s, m, rot=None, w=0.012):
    """A box with its edges bevelled (they catch the light in the bake)."""
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=1.0)
    bmesh.ops.transform(tmp, matrix=Matrix.Diagonal((*s, 1)), verts=tmp.verts)
    w = min(w, min(s) * 0.3)
    if w > 0.002:
        bmesh.ops.bevel(tmp, geom=tmp.edges[:] + tmp.verts[:], offset=w, segments=1, affect="EDGES", clamp_overlap=True)
    T = Matrix.Translation(Vector(c))
    if rot is not None:
        T = T @ (rot if isinstance(rot, Matrix) else Euler([math.radians(a) for a in rot]).to_matrix().to_4x4())
    bmesh.ops.transform(tmp, matrix=T, verts=tmp.verts)
    g._merge(tmp, m)


def place(g, sub, T):
    """Merge a sub-group built at the origin into g, through the matrix T."""
    me = bpy.data.meshes.new("sub")
    sub.bm.transform(T)
    sub.bm.to_mesh(me)
    sub.bm.free()
    remap = [g._mi(m) for m in sub.mats]
    for p in me.polygons:
        p.material_index = remap[p.material_index] if remap else 0
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def TR(x, y, z, yaw=0.0, pitch=0.0, roll=0.0):
    return Matrix.Translation((x, y, z)) @ Euler((math.radians(roll), math.radians(pitch), math.radians(yaw))).to_matrix().to_4x4()


_FONT = {}


def text(g, body, c, size, m, facing=-1, floor=False, rot=0.0, extrude=0.003, font=True):
    """Lettering: on a wall facing the camera (facing=-1, read from -y), on a wall facing the
    yard (facing=+1), or painted on the floor (read from above, along +x)."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = body
    cu.size = size
    cu.extrude = extrude
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    if font and os.path.exists(FONT_PATH):
        if "f" not in _FONT or _FONT["f"].name not in bpy.data.fonts:
            _FONT["f"] = bpy.data.fonts.load(FONT_PATH, check_existing=True)
        cu.font = _FONT["f"]
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    if floor:
        Rm = Matrix.Rotation(math.radians(rot), 4, "Z")
    else:
        Rm = Matrix.Rotation(math.radians(90), 4, "X")
        if facing > 0:
            Rm = Matrix.Rotation(math.radians(180), 4, "Z") @ Rm
        elif facing == 0:                                  # on a wall across the lane, read from -x
            Rm = Matrix.Rotation(math.radians(-90), 4, "Z") @ Rm
        Rm = Rm if not rot else Matrix.Rotation(math.radians(rot), 4, "Y") @ Rm
    me.transform(Matrix.Translation(Vector(c)) @ Rm)
    idx = g._mi(m)
    for p in me.polygons:
        p.material_index = idx
    g.bm.from_mesh(me)
    bpy.data.meshes.remove(me)


def sign(g, M, body, c, w, h, size=None, dark_plate=False, facing=-1, sub=None):
    """A sign board on a wall: the plate, a thin rim, the lettering, its screws."""
    x, y, z = c
    plate = M["k_black"] if dark_plate else M["k_white"]
    ink = M["k_white"] if dark_plate else M["k_black"]
    s = -1 if facing < 0 else 1
    bbox(g, (x, y, z), (w, 0.012, h), plate, w=0.004)
    if size is None:
        size = h * (0.42 if sub else 0.6)
    text(g, body, (x, y + s * 0.008, z + (h * 0.14 if sub else 0.0)), size, ink, facing=facing)
    if sub:
        text(g, sub, (x, y + s * 0.008, z - h * 0.24), size * 0.48, ink, facing=facing)
    for dx in (-w / 2 + 0.03, w / 2 - 0.03):
        for dz in (-h / 2 + 0.03, h / 2 - 0.03):
            g.cyl((x + dx, y + s * 0.008, z + dz), 0.006, 0.006, M["steel_mid"], rot=(90, 0, 0), n=6)


# ---------------------------------------------------------------- services on a wall (face at y, the room on -y)

def pipe_run(g, M, x0, x1, y, z, r, m, flange=3.0, bracket=1.5, wall_y=None):
    g.cyl(((x0 + x1) / 2, y, z), r, x1 - x0, m, rot=(0, 90, 0), n=12)
    x = x0 + flange * 0.5
    while x < x1:
        g.cyl((x, y, z), r + 0.025, 0.035, M["steel"], rot=(0, 90, 0), n=12)
        for a in range(6):                                      # flange bolts
            ang = a / 6 * math.tau
            g.cyl((x, y + math.cos(ang) * (r + 0.015), z + math.sin(ang) * (r + 0.015)), 0.008, 0.06, M["steel_mid"], rot=(0, 90, 0), n=5)
        x += flange
    if wall_y is not None:
        x = x0 + 0.4
        while x < x1:
            d = abs(wall_y - y)
            g.box((x, (y + wall_y) / 2, z - r - 0.02), (0.05, d, 0.03), M["steel"])            # bracket arm
            g.box((x, wall_y - 0.01, z - r - 0.02), (0.08, 0.02, 0.14), M["steel"])             # wall plate
            g.torus((x, y, z), r + 0.012, 0.007, M["steel"], rot=(0, 90, 0), n1=10, n2=4)       # U-clamp
            x += bracket


def valve(g, M, c, r=0.13, m=None, facing=-1):
    """A gate valve on a pipe: bonnet, stem, handwheel facing the room."""
    x, y, z = c
    s = facing
    g.cyl((x, y, z), 0.07, 0.16, M["steel_mid"], rot=(0, 90, 0), n=10)
    g.cyl((x, y + s * 0.1, z), 0.04, 0.2, M["steel"], rot=(90, 0, 0), n=8)
    g.cyl((x, y + s * 0.24, z), 0.012, 0.14, M["steel_mid"], rot=(90, 0, 0), n=6)
    m = m or M["paint_red"]
    g.torus((x, y + s * 0.3, z), r, 0.014, m, rot=(90, 0, 0), n1=16, n2=5)
    for a in range(3):
        ang = a / 3 * math.tau
        g.beam((x, y + s * 0.3, z), (x + r * math.cos(ang), y + s * 0.3, z + r * math.sin(ang)), 0.014, 0.014, m)


def gauge(g, M, c, facing=-1):
    x, y, z = c
    g.cyl((x, y + facing * 0.05, z), 0.012, 0.1, M["steel_mid"], rot=(90, 0, 0), n=6)
    g.cyl((x, y + facing * 0.11, z), 0.06, 0.03, M["steel"], rot=(90, 0, 0), n=14)
    g.cyl((x, y + facing * 0.127, z), 0.05, 0.004, M["k_white"], rot=(90, 0, 0), n=14)
    g.box((x + 0.012, y + facing * 0.131, z + 0.012), (0.004, 0.003, 0.04), M["k_black"], rot=(0, 40, 0))


def conduit(g, M, x0, x1, y, z, r=0.014, clip=0.7, boxes=()):
    g.cyl(((x0 + x1) / 2, y, z), r, x1 - x0, M["steel_mid"], rot=(0, 90, 0), n=6)
    x = x0 + 0.2
    while x < x1:
        g.box((x, y + 0.004, z), (0.025, r * 2 + 0.012, r * 2 + 0.03), M["steel"])
        x += clip
    for bx in boxes:
        bbox(g, (bx, y - 0.01, z), (0.12, 0.07, 0.12), M["steel_mid"], w=0.008)
        g.box((bx, y - 0.047, z), (0.1, 0.004, 0.1), M["steel"])


def conduit_drop(g, M, x, y, z_top, z_bot, r=0.014, box="switch"):
    g.cyl((x, y, (z_top + z_bot) / 2), r, z_top - z_bot, M["steel_mid"], n=6)
    z = z_bot + 0.3
    while z < z_top:
        g.box((x, y + 0.004, z), (r * 2 + 0.03, r * 2 + 0.012, 0.025), M["steel"])
        z += 0.7
    if box == "switch":
        bbox(g, (x, y - 0.01, z_bot - 0.07), (0.09, 0.05, 0.14), M["steel_mid"], w=0.006)
        g.box((x, y - 0.038, z_bot - 0.06), (0.025, 0.012, 0.04), M["k_black"])
    elif box == "socket":
        bbox(g, (x, y - 0.012, z_bot - 0.09), (0.16, 0.06, 0.16), M["steel_mid"], w=0.006)
        g.cyl((x - 0.035, y - 0.045, z_bot - 0.09), 0.03, 0.012, M["k_black"], rot=(90, 0, 0), n=10)
        g.cyl((x + 0.035, y - 0.045, z_bot - 0.09), 0.03, 0.012, M["k_black"], rot=(90, 0, 0), n=10)
    elif box == "isolator":
        bbox(g, (x, y - 0.03, z_bot - 0.14), (0.2, 0.12, 0.26), M["k_mid"], w=0.008)
        g.box((x, y - 0.095, z_bot - 0.14), (0.04, 0.02, 0.12), M["k_white"])


def cable_tray(g, M, x0, x1, y, z, wide=0.32, wall_y=None, cables=4):
    """A ladder cable tray: two side rails and rungs, its cables, hung off the wall on brackets."""
    for s in (-1, 1):
        g.box(((x0 + x1) / 2, y + s * wide / 2, z), (x1 - x0, 0.012, 0.08), M["steel_mid"])
    x = x0 + 0.15
    while x < x1:
        g.box((x, y, z - 0.035), (0.03, wide, 0.012), M["steel_mid"])
        x += 0.3
    for k in range(cables):
        yy = y - wide / 2 + 0.05 + k * (wide - 0.1) / max(1, cables - 1)
        rr = R.choice((0.012, 0.016, 0.022))
        g.cyl(((x0 + x1) / 2, yy, z - 0.02 + rr), rr, x1 - x0, M["rubber"], rot=(0, 90, 0), n=6)
    if wall_y is not None:
        x = x0 + 0.5
        while x < x1:
            g.box((x, (y + wall_y) / 2, z - 0.05), (0.04, abs(wall_y - y) + wide / 2, 0.04), M["steel"])
            g.beam((x, y - wide / 2, z - 0.05), (x, wall_y, z - 0.45), 0.03, 0.03, M["steel"])
            x += 1.5


def extinguisher(g, M, x, y_wall, z_floor=0.0, facing=-1, board=True):
    """A fire point: the bracket, the extinguisher (body, shoulder, valve, hose, horn), its
    ID sign board above, the floor marking."""
    s = facing
    y = y_wall + s * 0.12
    zb = z_floor + 0.45
    g.box((x, y_wall + s * 0.02, zb + 0.45), (0.08, 0.03, 0.18), M["steel"])                         # bracket
    g.cyl((x, y, zb + 0.25), 0.085, 0.5, M["paint_red"], n=14)
    g.cyl((x, y, zb + 0.52), 0.085, 0.05, M["paint_red"], n=14, r2=0.04)
    g.cyl((x, y, zb + 0.57), 0.02, 0.06, M["steel_mid"], n=8)
    g.box((x + 0.03, y, zb + 0.61), (0.11, 0.03, 0.02), M["k_black"], rot=(0, -12, 0))                  # lever
    g.box((x, y + s * 0.086, zb + 0.24), (0.1, 0.004, 0.14), M["k_white"])                              # label
    g.box((x, y + s * 0.088, zb + 0.32), (0.1, 0.004, 0.03), M["k_black"])                              # band
    g.beam((x - 0.02, y, zb + 0.57), (x - 0.11, y + s * 0.02, zb + 0.35), 0.016, 0.016, M["rubber"])    # hose
    g.beam((x - 0.11, y + s * 0.02, zb + 0.35), (x - 0.09, y + s * 0.05, zb + 0.1), 0.016, 0.016, M["rubber"])
    g.cyl((x - 0.09, y + s * 0.05, zb + 0.06), 0.022, 0.08, M["k_black"], n=8)
    if board:
        bbox(g, (x, y_wall + s * 0.012, zb + 1.05), (0.34, 0.012, 0.42), M["k_white"], w=0.004)
        text(g, "FIRE", (x, y_wall + s * 0.02, zb + 1.16), 0.11, M["k_black"], facing=facing)
        text(g, "POINT", (x, y_wall + s * 0.02, zb + 1.02), 0.075, M["k_black"], facing=facing)
        g.box((x, y_wall + s * 0.02, zb + 0.9), (0.26, 0.004, 0.02), M["k_black"])
    if z_floor < 0.01:                                                                                  # hatched floor box
        for k in range(5):
            g.box((x - 0.2 + k * 0.1, y_wall + s * 0.3, 0.003), (0.04, 0.5, 0.004), M["paint_yellow"], rot=(0, 0, 40))


def cage_lamp(g, M, x, y_wall, z, lit=True, facing=-1):
    """A caged bulkhead lamp on a wall or column: back plate, glass, wire cage, conduit stub."""
    s = facing
    bbox(g, (x, y_wall + s * 0.05, z), (0.3, 0.1, 0.2), M["steel"], w=0.012)
    g.cyl((x, y_wall + s * 0.11, z), 0.09, 0.04, M["k_lens"] if lit else M["bulb_dead"], rot=(90, 0, 0), n=14)
    g.box((x, y_wall + s * 0.12, z), (0.22, 0.04, 0.012), M["steel_mid"])
    g.box((x, y_wall + s * 0.12, z), (0.012, 0.04, 0.16), M["steel_mid"])
    g.torus((x, y_wall + s * 0.12, z), 0.1, 0.007, M["steel_mid"], rot=(90, 0, 0), n1=14, n2=4)
    g.cyl((x, y_wall + s * 0.03, z + 0.2), 0.014, 0.22, M["steel_mid"], n=6)


def elec_board(g, M, x, y_wall, z, w=0.8, h=1.0, facing=-1, label="DANGER  415 V"):
    """A distribution board: cabinet, door seam, hinges, a handle, the warning plate, conduits up."""
    s = facing
    bbox(g, (x, y_wall + s * 0.1, z), (w, 0.2, h), M["k_mid"], w=0.015)
    g.box((x, y_wall + s * 0.202, z), (0.006, 0.004, h - 0.06), M["k_black"])                          # door seam
    for dz in (-h / 2 + 0.1, h / 2 - 0.1):
        for dx in (-w / 2 + 0.01, w / 2 - 0.01):
            g.cyl((x + dx, y_wall + s * 0.19, z + dz), 0.012, 0.07, M["steel"], n=6)                    # hinges
    g.box((x - 0.07, y_wall + s * 0.215, z), (0.03, 0.03, 0.12), M["steel"])                           # handles
    g.box((x + 0.07, y_wall + s * 0.215, z), (0.03, 0.03, 0.12), M["steel"])
    bbox(g, (x, y_wall + s * 0.205, z + h / 2 - 0.16), (w * 0.6, 0.006, 0.12), M["k_white"], w=0.002)
    text(g, label, (x, y_wall + s * 0.21, z + h / 2 - 0.16), 0.065, M["k_black"], facing=facing)
    for k in range(3):                                                                                 # warning triangle
        a0 = math.radians(90 + k * 120)
        a1 = math.radians(90 + (k + 1) * 120)
        g.beam((x + 0.07 * math.cos(a0), y_wall + s * 0.207, z + 0.18 + 0.07 * math.sin(a0)),
               (x + 0.07 * math.cos(a1), y_wall + s * 0.207, z + 0.18 + 0.07 * math.sin(a1)), 0.016, 0.004, M["k_black"])
    for dx in (-w / 3, 0.0, w / 3):
        g.cyl((x + dx, y_wall + s * 0.08, z + h / 2 + 0.6), 0.025, 1.2, M["steel_mid"], n=8)
    g.box((x, y_wall + s * 0.1, z - h / 2 - 0.04), (w - 0.1, 0.12, 0.08), M["steel"])                  # gland plate


def call_point(g, M, x, y_wall, z, facing=-1):
    s = facing
    bbox(g, (x, y_wall + s * 0.03, z), (0.12, 0.06, 0.12), M["paint_red"], w=0.006)
    g.box((x, y_wall + s * 0.062, z), (0.07, 0.004, 0.07), M["k_white"])
    # the bell above
    g.cyl((x, y_wall + s * 0.05, z + 0.35), 0.1, 0.06, M["paint_red"], rot=(90, 0, 0), n=16)
    g.cyl((x, y_wall + s * 0.09, z + 0.35), 0.015, 0.03, M["steel"], rot=(90, 0, 0), n=6)


def streaks(g, M, x0, x1, z_top, y_face, n, m, length=(0.8, 2.6), width=(0.06, 0.35), z_min=0.2):
    """Rust and water bleeding down a wall: tapering layered strips."""
    for _ in range(n):
        x = R.uniform(x0, x1)
        L = R.uniform(*length)
        w = R.uniform(*width)
        zt = z_top - R.uniform(0, 0.15)
        zb = max(z_min, zt - L)
        steps = 3
        for k in range(steps):
            f0 = k / steps
            za = zt - (zt - zb) * f0
            zc = zt - (zt - zb) * (f0 + 1 / steps)
            g.box((x + R.uniform(-0.01, 0.01), y_face, (za + zc) / 2), (w * (1 - 0.28 * k), 0.004, za - zc), m)


# ---------------------------------------------------------------- structure

def column_detail(g, M, x, y, h, n=None, lamp_z=None, lit=True, guard=True, face=-1):
    """Dress an I-section column (S.ibeam: flanges at y +-0.11): cap plate, stiffeners, a
    hazard sleeve at the base, its grid number, a caged lamp."""
    s = face
    fy = y + s * 0.125                                  # front face of the camera-side flange
    g.box((x, y, h - 0.01), (0.36, 0.3, 0.02), M["steel_mid"])                                     # cap
    for zz in (h * 0.35, h * 0.7):
        for dx in (-0.07, 0.07):
            g.box((x + dx, y, zz), (0.012, 0.2, 0.25), M["steel_mid"])                                # stiffeners
    if guard:
        g.box((x, fy + s * 0.004, 0.55), (0.31, 0.008, 1.1), M["k_hazard"])
        g.box((x, y, 0.06), (0.4, 0.34, 0.12), M["concrete_dark"])                                    # plinth
    if n:
        bbox(g, (x, fy + s * 0.006, 2.2), (0.24, 0.006, 0.18), M["k_black"], w=0.002)
        text(g, n, (x, fy + s * 0.012, 2.2), 0.12, M["k_white"], facing=face)
    if lamp_z is not None:
        cage_lamp(g, M, x, fy, lamp_z, lit, facing=face)


def rod_bracing(g, M, x0, x1, y, z0, z1):
    """Cross rod bracing in a wall bay, a turnbuckle where the rods cross, cleats at the ends."""
    for (a, b) in (((x0, z0), (x1, z1)), ((x0, z1), (x1, z0))):
        g.beam((a[0], y, a[1]), (b[0], y, b[1]), 0.022, 0.022, M["steel"])
        for p in (a, b):
            g.box((p[0], y, p[1]), (0.12, 0.03, 0.12), M["steel_mid"])
    cx, cz = (x0 + x1) / 2, (z0 + z1) / 2
    ang = math.degrees(math.atan2(z1 - z0, x1 - x0))
    for a in (ang, -ang):
        g.cyl((cx + 0.35 * math.cos(math.radians(a)), y, cz + 0.35 * math.sin(math.radians(a))), 0.03, 0.18, M["steel_mid"], rot=(0, 90 - a, 0), n=8)


def gussets(g, M, x, nodes, size=0.24):
    """Gusset plates and bolts at a truss's panel points (the truss lies in the y-z plane at x)."""
    for (y, z) in nodes:
        g.box((x + 0.06, y, z), (0.012, size, size * 0.8), M["steel_mid"])
        g.box((x - 0.06, y, z), (0.012, size, size * 0.8), M["steel_mid"])
        for dy in (-size * 0.3, size * 0.3):
            for dz in (-size * 0.22, size * 0.22):
                g.cyl((x, y + dy, z + dz), 0.01, 0.15, M["steel"], rot=(0, 90, 0), n=6)


def railing(g, M, a, b, z, h=1.05, post=1.2, m=None, kick=True):
    """A railing from a to b (2D points on the floor at height z): posts, top and knee rails, toe board."""
    m = m or M["paint_yellow"]
    a, b = Vector((a[0], a[1], z)), Vector((b[0], b[1], z))
    d = b - a
    L = d.length
    n = max(1, round(L / post))
    for k in range(n + 1):
        p = a + d * (k / n)
        g.box((p.x, p.y, z + h / 2), (0.045, 0.045, h), m)
    for zz in (h, h * 0.52):
        g.beam((a.x, a.y, z + zz), (b.x, b.y, z + zz), 0.045, 0.045, m)
    if kick:
        g.beam((a.x, a.y, z + 0.06), (b.x, b.y, z + 0.06), 0.012, 0.12, M["steel"], up=Vector((0, 0, 1)) if abs(d.x) > abs(d.y) else Vector((0, 0, 1)))


def ladder(g, M, x, y, z0, z1, cage=True, facing=-1, m=None):
    """A fixed steel ladder up a wall: stiles, rungs, wall brackets, a safety cage above 2.4 m."""
    m = m or M["steel"]
    s = facing
    yl = y + s * 0.2
    for dx in (-0.22, 0.22):
        g.box((x + dx, yl, (z0 + z1) / 2 + 0.5), (0.05, 0.06, z1 - z0 + 1.0), m)
    z = z0 + 0.25
    while z < z1:
        g.cyl((x, yl, z), 0.014, 0.44, M["steel_mid"], rot=(0, 90, 0), n=6)
        z += 0.3
    z = z0 + 0.5
    while z < z1:
        for dx in (-0.22, 0.22):
            g.box((x + dx, (y + yl) / 2, z), (0.04, 0.2, 0.04), m)
        z += 1.8
    if cage:
        z = z0 + 2.4
        while z < z1 + 1.0:
            for a in range(9):
                t0, t1 = math.pi * a / 9, math.pi * (a + 1) / 9
                p0 = (x + 0.36 * math.cos(t0), yl + s * 0.36 * math.sin(t0), z)
                p1 = (x + 0.36 * math.cos(t1), yl + s * 0.36 * math.sin(t1), z)
                g.beam(p0, p1, 0.03, 0.03, m)
            z += 0.9
        for a in range(5):
            t = math.pi * a / 4
            g.box((x + 0.36 * math.cos(t), yl + s * 0.36 * math.sin(t), (z0 + 2.4 + z1 + 1.0) / 2), (0.03, 0.03, z1 + 1.0 - z0 - 2.4), m)


def catwalk(g, M, x0, x1, y0, y1, z, wall_y, rail_side=-1, brackets=2.0):
    """A grated walkway along a wall: the deck (grating), stringers, toe plates, a railing on
    the open side, knee brackets back to the wall every couple of metres."""
    g.box(((x0 + x1) / 2, (y0 + y1) / 2, z - 0.02), (x1 - x0, y1 - y0, 0.04), M["k_grating"])
    for yy in (y0, y1):
        g.box(((x0 + x1) / 2, yy, z - 0.1), (x1 - x0, 0.05, 0.2), M["steel"])                        # stringers
    ye = y0 if rail_side < 0 else y1
    railing(g, M, (x0, ye), (x1, ye), z, post=1.5, m=M["paint_yellow"])
    x = x0 + 0.3
    while x < x1:
        g.box((x, (y0 + y1) / 2, z - 0.12), (0.06, y1 - y0, 0.08), M["steel"])
        g.beam((x, ye, z - 0.15), (x, wall_y, z - 1.0), 0.06, 0.06, M["steel"])
        g.box((x, wall_y - 0.01, z - 1.0), (0.16, 0.02, 0.2), M["steel_mid"])
        x += brackets


# ---------------------------------------------------------------- loose kit

def chain_hook(S, g, M, top, length, hook=True):
    end = S.chain(g, top, length, M)
    if hook:
        x, y, _ = top
        g.torus((x, y, end - 0.05), 0.05, 0.014, M["steel_mid"], rot=(90, 0, 0), n1=10, n2=4)
        # an open crane hook: a C of short beams, the tip turned up
        cz = end - 0.25
        pts = [(math.cos(math.radians(a)) * 0.1, math.sin(math.radians(a)) * 0.1) for a in range(100, 400, 30)]
        for (ax, az), (bx, bz) in zip(pts, pts[1:]):
            g.beam((x + ax, y, cz + az), (x + bx, y, cz + bz), 0.035, 0.035, M["steel_mid"])
        g.beam((x, y, end - 0.1), (x + pts[0][0], y, cz + pts[0][1]), 0.04, 0.04, M["steel_mid"])


def carton(g, M, c, s, m=None, tape=True, label=True, rot=None):
    m = m or R.choice((M["card"], M["card"], M["card_dark"]))
    yaw = rot if rot is not None else R.uniform(-4, 4)
    Rm = Euler((0, 0, math.radians(yaw))).to_matrix().to_4x4()
    bbox(g, c, s, m, rot=Rm, w=0.008)
    x, y, z = c
    T = Matrix.Translation(c) @ Rm
    if tape:
        g.box((T @ Vector((0, 0, s[2] / 2 + 0.002)))[:], (s[0] + 0.004, 0.05, 0.004), M["wrap"], rot=Rm)
        g.box((T @ Vector((0, -s[1] / 2 - 0.002, s[2] / 2 - 0.06)))[:], (0.05, 0.004, 0.12), M["wrap"], rot=Rm)
    if label and R.random() < 0.6:
        g.box((T @ Vector((s[0] * 0.2, -s[1] / 2 - 0.003, 0)))[:], (0.12, 0.004, 0.08), M["k_paper"], rot=Rm)


def load_on_pallet(S, g, M, x, y, z=0.0, kind=None, h=None, yaw=0.0):
    """A pallet and its load: cartons stacked in courses, a wrapped load, a tarped one, or sacks."""
    S.pallet(g, (x, y, z), M)
    zt = z + 0.145
    kind = kind or R.choice(("cartons", "cartons", "wrap", "sacks", "drums"))
    h = h or R.uniform(0.5, 1.3)
    if kind == "cartons":
        zz = zt
        while zz < zt + h - 0.2:
            bh = R.uniform(0.25, 0.38)
            for dx in (-0.3, 0.3):
                for dy in (-0.25, 0.25):
                    if R.random() < 0.92:
                        carton(g, M, (x + dx, y + dy, zz + bh / 2), (0.58, 0.48, bh - 0.01))
            zz += bh
    elif kind == "wrap":
        bbox(g, (x, y, zt + h / 2), (1.18, 0.98, h), M["wrap"], w=0.05)
        for k in range(3):
            g.box((x, y, zt + 0.15 + k * h / 3), (1.2, 1.0, 0.02), M["k_white"])
    elif kind == "sacks":
        zz = zt
        for row in range(int(h / 0.16)):
            for dx in (-0.3, 0.3):
                g.cyl((x + dx, y, zz + 0.08), 0.42, 0.16, M["k_paper"] if row % 2 else M["card"], n=10, r2=0.38)
            zz += 0.16
    elif kind == "drums":
        for dx in (-0.3, 0.3):
            for dy in (-0.25, 0.25):
                S.drum(g, (x + dx, y + dy, zt + 0.44), M, R.choice((M["paint_blue"], M["paint_red"], M["steel_mid"])))


def tarped(g, M, c, s, m=None):
    """A load under a tarp: the tarp body with sag, folds hanging down the sides, a rope."""
    m = m or M["tarp"]
    x, y, z = c
    bbox(g, (x, y, z + s[2] / 2), s, m, w=0.08)
    bbox(g, (x, y, z + s[2] + 0.03), (s[0] * 0.8, s[1] * 0.75, 0.08), m, w=0.03)
    for k in range(int(s[0] / 0.35)):
        fx = x - s[0] / 2 + 0.2 + k * 0.35
        g.box((fx, y - s[1] / 2 - 0.03, z + s[2] * 0.55), (0.12, 0.06, s[2] * R.uniform(0.6, 1.0)), m, rot=(R.uniform(-4, 4), 0, R.uniform(-8, 8)))
    g.box((x, y - s[1] / 2 - 0.07, z + s[2] * 0.5), (s[0] + 0.02, 0.012, 0.012), M["rubber"])
    for dx in (-s[0] / 2 + 0.1, s[0] / 2 - 0.1):
        g.box((x + dx, y - s[1] / 2 - 0.07, z + s[2] * 0.25), (0.012, 0.012, s[2] * 0.5), M["rubber"])
    g.box((x + s[0] / 2 + 0.06, y, z + 0.05), (0.25, s[1] * 0.6, 0.06), m, rot=(0, 10, 15))


def shelving(S, g, M, x0, x1, y0, y1, h, levels=4, label=None):
    """Steel boltless shelving: angle uprights, shelf beams, decks, stock (cartons, tins, jerrycans)."""
    for x in (x0, x1):
        for y in (y0, y1):
            g.box((x, y, h / 2), (0.04, 0.04, h), M["steel_mid"])
    for k in range(levels):
        z = 0.12 + k * (h - 0.2) / (levels - 1)
        g.box(((x0 + x1) / 2, (y0 + y1) / 2, z), (x1 - x0, y1 - y0, 0.02), M["steel_mid"])
        for y in (y0, y1):
            g.box(((x0 + x1) / 2, y, z - 0.03), (x1 - x0, 0.03, 0.05), M["rack_orange"])
        if k == levels - 1 and h > 2.2:
            continue
        x = x0 + 0.06
        while x < x1 - 0.15:
            r = R.random()
            if r < 0.4:
                w = R.uniform(0.25, 0.45)
                bh = R.uniform(0.15, min(0.35, (h - 0.2) / (levels - 1) - 0.08))
                carton(g, M, (x + w / 2, (y0 + y1) / 2 + 0.04, z + 0.01 + bh / 2), (w, (y1 - y0) * 0.75, bh))
                x += w + 0.03
            elif r < 0.6:
                for k2 in range(R.randint(2, 4)):
                    g.cyl((x + 0.07, (y0 + y1) / 2 - 0.1 + k2 * 0.09, z + 0.1), 0.07, 0.19, R.choice((M["steel_mid"], M["paint_blue"], M["paint_red"])), n=12)
                    g.cyl((x + 0.07, (y0 + y1) / 2 - 0.1 + k2 * 0.09, z + 0.2), 0.055, 0.01, M["steel"], n=12)
                x += 0.17
            elif r < 0.72:
                bbox(g, (x + 0.09, (y0 + y1) / 2, z + 0.2), (0.16, 0.3, 0.36), R.choice((M["paint_red"], M["k_mid"])), w=0.02)
                g.box((x + 0.09, (y0 + y1) / 2 - 0.09, z + 0.41), (0.04, 0.1, 0.05), M["k_black"])
                x += 0.2
            elif r < 0.8:
                g.torus((x + 0.18, (y0 + y1) / 2, z + 0.05), 0.15, 0.04, M["rubber"], n1=14, n2=5)            # coiled cable
                x += 0.38
            else:
                x += R.uniform(0.1, 0.3)
    if label:
        sign(g, M, label, ((x0 + x1) / 2, y0 - 0.03, 0.12 + (h - 0.2) / (levels - 1) - 0.03), 0.34, 0.07, size=0.05)


def folding_chair(S, M, tipped=False):
    """A steel folding chair (built at the origin, facing -y): legs, seat, back."""
    c = S.Group("tmp")
    for dx in (-0.2, 0.2):
        c.beam((dx, -0.22, 0.0), (dx, 0.18, 0.86), 0.025, 0.025, M["steel_mid"])                   # front leg -> back post
        c.beam((dx, 0.2, 0.0), (dx, -0.12, 0.45), 0.025, 0.025, M["steel_mid"])                    # back leg
    c.box((0.0, -0.02, 0.45), (0.42, 0.4, 0.03), M["k_mid"])
    c.box((0.0, 0.15, 0.74), (0.4, 0.04, 0.22), M["k_mid"], rot=(-12, 0, 0))
    c.beam((-0.2, -0.2, 0.08), (0.2, -0.2, 0.08), 0.015, 0.015, M["steel_mid"])
    return c


def wood_chair(S, M):
    """A heavy wooden chair, the kind they tie a man to."""
    c = S.Group("tmp")
    for dx in (-0.21, 0.21):
        for dy in (-0.19, 0.19):
            c.box((dx, dy, 0.23), (0.045, 0.045, 0.46), M["wood_dark"])
        c.box((dx, 0.19, 0.72), (0.045, 0.045, 0.52), M["wood_dark"])
        c.box((dx, 0.0, 0.12), (0.03, 0.38, 0.03), M["wood_dark"])
    c.box((0.0, 0.0, 0.48), (0.48, 0.44, 0.04), M["wood"])
    for z in (0.66, 0.84, 0.97):
        c.box((0.0, 0.19, z), (0.44, 0.025, 0.07), M["wood"])
    c.box((0.0, -0.19, 0.12), (0.42, 0.03, 0.03), M["wood_dark"])
    return c


def zip_tie(g, M, c, yaw, cut=True):
    """A cut zip tie lying on the floor: an open loop of strap, its locking head."""
    x, y, z = c
    r = 0.04
    a0 = 30 if cut else 0
    steps = 9
    for k in range(steps):
        t0 = math.radians(a0 + yaw + (360 - 2 * a0) * k / steps)
        t1 = math.radians(a0 + yaw + (360 - 2 * a0) * (k + 1) / steps)
        g.beam((x + r * math.cos(t0), y + r * math.sin(t0), z + 0.003), (x + r * math.cos(t1), y + r * math.sin(t1), z + 0.003), 0.007, 0.002, M["k_zip"])
    g.box((x + r * math.cos(math.radians(yaw)), y + r * math.sin(math.radians(yaw)), z + 0.006), (0.014, 0.012, 0.008), M["k_zip"], rot=(0, 0, yaw))
    tail = math.radians(yaw)
    g.beam((x + r * math.cos(tail), y + r * math.sin(tail), z + 0.003), (x + r * math.cos(tail) + 0.08 * math.cos(tail + 1.2), y + r * math.sin(tail) + 0.08 * math.sin(tail + 1.2), z + 0.003), 0.007, 0.002, M["k_zip"])


def scatter_debris(g, M, x0, x1, y0, y1, n, kinds=("paper", "butt", "bolt", "chip", "glass")):
    """Litter on a floor: paper, cigarette ends, bolts, concrete chips, glass. Flat, never tall."""
    for _ in range(n):
        x, y = R.uniform(x0, x1), R.uniform(y0, y1)
        k = R.choice(kinds)
        a = R.uniform(0, 180)
        if k == "paper":
            g.box((x, y, 0.004), (R.uniform(0.06, 0.2), R.uniform(0.05, 0.14), 0.003), M["k_paper"] if R.random() < 0.35 else M["k_stain_pale"], rot=(R.uniform(-3, 3), R.uniform(-3, 3), a))
        elif k == "butt":
            g.cyl((x, y, 0.006), 0.004, 0.035, M["k_white"], rot=(90, 0, a), n=5)
        elif k == "bolt":
            g.cyl((x, y, 0.008), 0.008, 0.05, M["steel_mid"], rot=(90, 0, a), n=6)
        elif k == "chip":
            g.box((x, y, 0.01), (R.uniform(0.02, 0.07),) * 2 + (0.02,), M["concrete_dark"], rot=(R.uniform(0, 40), R.uniform(0, 40), a))
        elif k == "glass":
            g.box((x, y, 0.004), (R.uniform(0.02, 0.1), R.uniform(0.02, 0.07), 0.004), M["glass"], rot=(0, 0, a))


def floor_cracks(g, M, x0, x1, y0, y1, n, m):
    for _ in range(n):
        x, y = R.uniform(x0, x1), R.uniform(y0, y1)
        a = R.uniform(0, 360)
        for k in range(R.randint(3, 7)):
            L = R.uniform(0.15, 0.5)
            nx, ny = x + math.cos(math.radians(a)) * L, y + math.sin(math.radians(a)) * L
            g.beam((x, y, 0.0025), (nx, ny, 0.0025), 0.012, 0.003, m)
            x, y = nx, ny
            a += R.uniform(-45, 45)


def puddle(g, M, x, y, r, m=None, n=18):
    """An irregular puddle: a few overlapping flat discs, squashed, of different sizes."""
    m = m or M["k_puddle"]
    for k in range(5):
        a = R.uniform(0, math.tau)
        rr = r * (1.0 if k == 0 else R.uniform(0.35, 0.7))
        d = 0.0 if k == 0 else r * R.uniform(0.5, 0.9)
        tmp = bmesh.new()
        bmesh.ops.create_circle(tmp, cap_ends=True, segments=n, radius=rr)
        sx, sy = R.uniform(1.0, 1.6), R.uniform(0.5, 0.9)
        bmesh.ops.transform(tmp, matrix=Matrix.Translation((x + math.cos(a) * d, y + math.sin(a) * d * 0.6, 0.0022 + k * 0.0001))
                            @ Matrix.Rotation(R.uniform(-0.4, 0.4), 4, "Z") @ Matrix.Diagonal((sx, sy, 1, 1)), verts=tmp.verts)
        g._merge(tmp, m)


def gas_bottles(g, M, x, y, n=3, chained=True, wall_y=None):
    for k in range(n):
        bx = x + k * 0.26
        g.cyl((bx, y, 0.7), 0.11, 1.4, R.choice((M["k_mid"], M["steel_mid"], M["k_black"])), n=14)
        g.cyl((bx, y, 1.44), 0.11, 0.08, M["k_white"], n=14, r2=0.05)
        g.cyl((bx, y, 1.52), 0.03, 0.08, M["steel"], n=8)
        g.cyl((bx, y, 1.6), 0.05, 0.04, M["steel_mid"], n=10)
    if chained:
        g.box((x + (n - 1) * 0.13, y - 0.1, 1.05), ((n - 1) * 0.26 + 0.3, 0.012, 0.02), M["steel_mid"])
    if wall_y is not None:
        for dx in (-0.15, (n - 1) * 0.26 + 0.15):
            g.box((x + dx, (y + wall_y) / 2, 1.05), (0.04, abs(wall_y - y) + 0.1, 0.04), M["steel"])


def bollard(g, M, x, y, h=1.1, r=0.1):
    g.cyl((x, y, h / 2), r, h, M["k_black"], n=12)
    for z in (h - 0.15, h - 0.35):
        g.cyl((x, y, z), r + 0.004, 0.1, M["k_white"], n=12)
    g.cyl((x, y, h + 0.02), r * 0.85, 0.04, M["k_black"], n=12)


def pallet_stack(S, g, M, x, y, n, z=0.0):
    for k in range(n):
        S.pallet(g, (x + R.uniform(-0.03, 0.03), y + R.uniform(-0.03, 0.03), z + k * 0.145), M)


def pallet_jack(g, M, x, y, yaw):
    T = TR(x, y, 0.0, yaw)
    Rm = Euler((0, 0, math.radians(yaw))).to_matrix().to_4x4()
    for dy in (-0.27, 0.27):
        g.box((T @ Vector((0.6, dy, 0.06)))[:], (1.15, 0.16, 0.05), M["k_mid"], rot=Rm)
        g.cyl((T @ Vector((1.12, dy, 0.04)))[:], 0.04, 0.08, M["rubber"], rot=(90, 0, yaw), n=8)
    bbox(g, (T @ Vector((0.0, 0.0, 0.15)))[:], (0.25, 0.6, 0.22), M["k_mid"], rot=Rm, w=0.02)
    g.cyl((T @ Vector((-0.05, 0.0, 0.08)))[:], 0.09, 0.08, M["rubber"], rot=(90, 0, yaw), n=12)
    g.beam((T @ Vector((0.0, 0.0, 0.25)))[:], (T @ Vector((-0.45, 0.0, 1.15)))[:], 0.04, 0.04, M["steel"])
    g.beam((T @ Vector((-0.45, -0.15, 1.15)))[:], (T @ Vector((-0.45, 0.15, 1.15)))[:], 0.04, 0.04, M["k_black"])


def cone(g, M, x, y):
    g.box((x, y, 0.015), (0.36, 0.36, 0.03), M["k_black"])
    g.cyl((x, y, 0.36), 0.14, 0.66, M["k_white"], n=12, r2=0.025)
    for z, r in ((0.32, 0.105), (0.5, 0.07)):
        g.cyl((x, y, z), r + 0.004, 0.08, M["k_black"], n=12)


def jerrycan(g, M, x, y, z=0.0, yaw=0.0):
    bbox(g, (x, y, z + 0.24), (0.34, 0.17, 0.46), M["k_mid"], rot=(0, 0, yaw), w=0.025)
    g.box((x, y, z + 0.5), (0.18, 0.04, 0.04), M["k_mid"], rot=(0, 0, yaw))
    g.cyl((x + 0.12, y, z + 0.5), 0.025, 0.05, M["k_black"], n=8)


# ================================================================ THE STORE (the room he wakes in)

def shed_detail(S, g, M):
    """Dress the store: x -14..14, the lane at y 0, the back wall at y 2.6 (inner face ~2.58),
    the roof underside at 7.2. Everything stands at y >= 0.85, or is flat on the floor, or is
    high overhead."""
    add_mats(S, M)
    HW, ROOF, BACK = S.HALF_W, S.ROOF, S.BACK
    yw = BACK - 0.02                                    # the face things are fixed to
    cols = [(-HW + 3.5, "C2"), (-HW + 10.5, "C3"), (-HW + 17.5, "C4"), (S.DOOR_L - 0.25, "C5"), (S.DOOR_R + 0.25, "C6")]

    # ---------------------------------------------------- the floor: cracks, stains, puddles, litter
    floor_cracks(g, M, -13.5, 13.5, -0.9, 2.4, 26, M["concrete_dark"])
    for (x, y, r) in ((-12.0, 2.15, 0.5), (-5.0, 1.4, 0.35), (0.9, 1.3, 0.45), (4.4, 2.3, 0.3), (6.8, 1.15, 0.5), (12.6, 2.0, 0.7)):
        puddle(g, M, x, y, r)
    for (x, y, r) in ((-9.6, 1.2, 0.25), (-11.4, 1.0, 0.55), (-2.0, 1.25, 0.3), (2.0, 2.2, 0.35), (9.5, 1.2, 0.3)):
        g.cyl((x, y, 0.0022), r, 0.004, M["oil"], n=14)                                            # more oil
    # tyre tracks of the forklift, out from where it is parked
    for k in range(18):
        x = -10.0 + k * 0.45
        for dy in (-0.42, 0.42):
            g.box((x, 1.45 + dy - k * 0.01, 0.0021), (0.4, 0.14, 0.003), M["k_stain"], rot=(0, 0, -1.5))
    scatter_debris(g, M, -13.5, 13.0, 0.9, 2.45, 120)
    scatter_debris(g, M, -13.5, 13.0, -0.9, 0.9, 30, kinds=("paper", "butt", "chip"))
    # glass under the broken windows (they fell in)
    for cx in S.WINDOWS:
        for _ in range(28):
            x = cx + R.uniform(-1.4, 1.4)
            y = R.uniform(1.6, 2.5)
            g.box((x, y, 0.004), (R.uniform(0.02, 0.12), R.uniform(0.02, 0.08), 0.004), M["glass"], rot=(0, 0, R.uniform(0, 180)))
    # the walkway: pedestrian symbols and arrows on the line
    for x in (-10.5, -2.5, 6.0):
        text(g, "WALKWAY", (x, 2.1, 0.0045), 0.22, M["paint_yellow"], floor=True)
        for k in range(3):
            g.box((x + 0.9 + k * 0.18, 2.1, 0.0035), (0.1, 0.03, 0.003), M["paint_yellow"], rot=(0, 0, 45))
            g.box((x + 0.9 + k * 0.18, 2.06, 0.0035), (0.1, 0.03, 0.003), M["paint_yellow"], rot=(0, 0, -45))
    text(g, "KEEP  CLEAR", ((S.DOOR_L + S.DOOR_R) / 2, 1.0, 0.0045), 0.42, M["paint_yellow"], floor=True)

    # ---------------------------------------------------- the back wall: girts, bracing, stains
    for z in (1.25, 3.55):
        g.box((-HW + (S.DOOR_L + HW) / 2, yw + 0.04, z), (S.DOOR_L + HW, 0.06, 0.14), M["steel"])
    g.box(((S.DOOR_R + HW) / 2, yw + 0.04, 3.55), (HW - S.DOOR_R, 0.06, 0.14), M["steel"])
    rod_bracing(g, M, -HW + 17.65, S.DOOR_L - 0.4, yw + 0.02, 0.5, 4.5)
    for cx in S.WINDOWS:                                                                   # weather under each window
        streaks(g, M, cx - 1.3, cx + 1.3, S.WIN_Z0 - 0.1, yw + 0.075, 9, M["k_stain_wall"], length=(0.8, 3.0))
        streaks(g, M, cx - 1.2, cx + 1.2, S.WIN_Z0 - 0.1, yw + 0.074, 4, M["rust"], length=(0.4, 1.6), width=(0.03, 0.12))
    streaks(g, M, -13.5, 13.5, S.ROOF - 0.2, yw + 0.074, 26, M["k_stain_wall"], length=(0.6, 2.4), width=(0.1, 0.5), z_min=S.WIN_Z1 + 0.05)
    # a tide line along the block plinth, and rising damp
    g.box((-1.8, yw - 0.001, 0.12), (24.4, 0.004, 0.24), M["k_stain_wall"])
    for _ in range(14):
        x = R.uniform(-13.5, 9.5)
        g.box((x, yw - 0.002, 0.3), (R.uniform(0.3, 1.2), 0.004, R.uniform(0.2, 0.55)), M["k_stain_wall"])
    # dented sheets where the forklift has hit the wall
    for (x, z, a) in ((-11.7, 1.6, 8), (-10.9, 1.45, -6)):
        g.box((x, yw + 0.07, z), (0.5, 0.03, 0.4), M["iron_dark"], rot=(0, a, 0))

    # ---------------------------------------------------- columns
    for k, (cx, n) in enumerate(cols):
        column_detail(g, M, cx, BACK - 0.14, ROOF - 0.2, n=n, lamp_z=3.0 if k != 1 else None, lit=k != 2)
        g.box((cx, BACK - 0.14, ROOF - 0.5), (0.5, 0.3, 0.14), M["steel_mid"])                        # haunch
        g.beam((cx, BACK - 0.14, ROOF - 1.5), (cx + (0.8 if cx < 0 else -0.8), BACK - 0.14, ROOF - 0.35), 0.08, 0.08, M["steel"])

    # ---------------------------------------------------- services: conduit, cable tray, more pipe
    conduit(g, M, -HW + 0.2, S.DOOR_L - 0.5, yw - 0.02, 3.3, boxes=(-12.4, -9.4, -5.6, -3.5, 1.4, 6.0, 9.8))
    conduit(g, M, -HW + 0.2, S.DOOR_L - 0.5, yw - 0.02, 3.4, r=0.011, clip=0.9)
    for (x, z0, kind) in ((-5.0, 1.45, "socket"), (-6.2, 1.45, "socket"), (-3.0, 1.35, "switch"), (9.8, 1.4, "isolator"), (0.0, 1.3, "socket")):
        conduit_drop(g, M, x, yw - 0.02, 3.3, z0, box=kind)
    cable_tray(g, M, -HW + 0.3, S.DOOR_L - 0.3, yw - 0.32, 4.3, wall_y=yw)
    # the old pipes: brackets and a second valve, a gauge, a drain cock dripping
    for x in range(-13, 9, 2):
        g.box((x + 0.5, (2.4 + yw) / 2, 2.38), (0.05, yw - 2.4 + 0.1, 0.03), M["steel"])
        g.torus((x + 0.5, 2.4, 2.5), 0.1, 0.008, M["steel"], rot=(0, 90, 0), n1=10, n2=4)
    valve(g, M, (2.4, 2.4, 2.5), r=0.11, m=M["paint_red"])
    gauge(g, M, (-9.0, 2.4, 2.65))
    gauge(g, M, (4.6, 2.4, 2.95))
    g.cyl((-1.0, 2.4, 2.33), 0.015, 0.12, M["steel_mid"], n=6)
    puddle(g, M, -1.0, 2.25, 0.18)
    # an air line coiled on a hook by the press
    g.torus((0.15, yw - 0.12, 2.0), 0.22, 0.015, M["rubber"], rot=(90, 0, 0), n1=18, n2=4)
    g.torus((0.15, yw - 0.14, 2.0), 0.2, 0.015, M["rubber"], rot=(85, 0, 4), n1=18, n2=4)
    g.box((0.15, yw - 0.05, 2.2), (0.04, 0.12, 0.04), M["steel"])

    # ---------------------------------------------------- power, fire, signs
    elec_board(g, M, -9.4, yw, 1.95)
    g.box((-9.4, yw - 0.5, 0.002), (1.0, 0.8, 0.003), M["k_black"])                                 # rubber mat
    extinguisher(g, M, -HW + 10.5 + 0.38, yw)
    extinguisher(g, M, 8.9, yw)
    call_point(g, M, 9.85, yw, 1.4)
    sign(g, M, "HARD HATS", (9.3, yw - 0.01, 2.35), 0.5, 0.32, sub="MUST BE WORN", dark_plate=True)
    sign(g, M, "AUTHORISED", (-7.6, yw - 0.01, 2.95), 0.6, 0.3, sub="PERSONNEL ONLY")
    sign(g, M, "FORKLIFTS", (-12.0, yw - 0.01, 3.0), 0.5, 0.3, sub="OPERATING", dark_plate=True)
    sign(g, M, "BAY 7", (S.DOOR_L - 0.95, yw - 0.01, 3.95), 0.5, 0.22)
    text(g, "SWL  2 T", (-6.5, 2.05 - 0.14, 5.85), 0.16, M["k_black"])
    text(g, "SWL  2 T", (6.5, 2.05 - 0.14, 5.85), 0.16, M["k_black"])
    # a calendar and notices on the board by the bench
    g.box((-4.0, yw - 0.01, 1.7), (0.6, 0.02, 0.45), M["wood"])
    for (dx, dz, w, h) in ((-0.18, 0.06, 0.18, 0.25), (0.02, 0.1, 0.16, 0.2), (0.2, -0.02, 0.15, 0.22), (0.0, -0.13, 0.2, 0.1)):
        g.box((-4.0 + dx, yw - 0.025, 1.7 + dz), (w, 0.004, h), M["k_paper"], rot=(0, R.uniform(-6, 6), 0))

    # ---------------------------------------------------- the crew's table (they sat here and waited)
    tx, ty = -3.55, 1.45
    bbox(g, (tx, ty, 0.74), (1.3, 0.72, 0.035), M["wood"], w=0.006)
    for dx in (-0.6, 0.6):
        for dy in (-0.3, 0.3):
            g.box((tx + dx, ty + dy, 0.37), (0.035, 0.035, 0.74), M["steel_mid"])
        g.box((tx + dx, ty, 0.1), (0.03, 0.6, 0.03), M["steel_mid"])
    zt = 0.76
    # a game of cards left mid-hand: a deck, the hands face down, a few showing
    g.box((tx - 0.1, ty + 0.05, zt + 0.015), (0.065, 0.09, 0.03), M["k_card_white"], rot=(0, 0, 12))
    for k in range(9):
        a = R.uniform(0, 180)
        g.box((tx - 0.45 + R.uniform(0, 0.9), ty + R.uniform(-0.28, 0.28), zt + 0.002 + k * 0.0006), (0.065, 0.09, 0.0012),
              M["k_card_white"] if k % 3 else M["k_black"], rot=(0, 0, a))
    for k in range(5):
        g.box((tx + 0.25 + k * 0.03, ty - 0.2, zt + 0.003), (0.065, 0.09, 0.0012), M["k_card_white"], rot=(0, 0, -20 + k * 10))
    for k in range(4):                                                                              # the pot: notes
        g.box((tx + 0.05 + R.uniform(-0.05, 0.05), ty + 0.1 + R.uniform(-0.03, 0.03), zt + 0.003 + k * 0.002), (0.15, 0.07, 0.002), M["k_paper"], rot=(0, 0, R.uniform(0, 40)))
    # the thermos and its cup, a mug
    g.cyl((tx + 0.45, ty + 0.2, zt + 0.16), 0.045, 0.32, M["steel_mid"], n=14)
    g.cyl((tx + 0.45, ty + 0.2, zt + 0.33), 0.05, 0.03, M["k_black"], n=14)
    g.cyl((tx + 0.36, ty + 0.06, zt + 0.04), 0.04, 0.08, M["k_black"], n=12, r2=0.045)
    g.cyl((tx - 0.5, ty + 0.18, zt + 0.05), 0.04, 0.1, M["k_white"], n=12)
    g.torus((tx - 0.455, ty + 0.18, zt + 0.05), 0.025, 0.006, M["k_white"], rot=(90, 0, 0), n1=8, n2=4)
    # the ashtray, full, and the pack
    g.cyl((tx - 0.25, ty - 0.18, zt + 0.015), 0.07, 0.03, M["glass"], n=14)
    for k in range(6):
        a = R.uniform(0, math.tau)
        g.cyl((tx - 0.25 + math.cos(a) * 0.04, ty - 0.18 + math.sin(a) * 0.04, zt + 0.03), 0.004, 0.035, M["k_white"], rot=(90, 0, math.degrees(a)), n=5)
    g.box((tx - 0.05, ty - 0.24, zt + 0.012), (0.055, 0.09, 0.022), M["k_white"], rot=(0, 0, 25))
    # the radio: a black brick with an aerial, a dial, a speaker grille
    rx, ry = tx + 0.2, ty + 0.24
    bbox(g, (rx, ry, zt + 0.08), (0.26, 0.09, 0.16), M["k_black"], w=0.012)
    g.cyl((rx + 0.07, ry - 0.046, zt + 0.1), 0.025, 0.012, M["steel_mid"], rot=(90, 0, 0), n=10)
    for k in range(5):
        g.box((rx - 0.05, ry - 0.046, zt + 0.04 + k * 0.022), (0.1, 0.004, 0.008), M["steel_mid"])
    g.beam((rx + 0.1, ry, zt + 0.16), (rx + 0.22, ry + 0.05, zt + 0.62), 0.006, 0.006, M["steel_mid"])
    g.box((rx - 0.08, ry, zt + 0.17), (0.05, 0.04, 0.02), M["steel_mid"])
    # a crowbar left against the table, a torch on it
    g.beam((tx + 0.68, ty - 0.05, 0.02), (tx + 0.66, ty - 0.12, 0.75), 0.022, 0.022, M["steel"])
    g.cyl((tx - 0.3, ty + 0.25, zt + 0.025), 0.025, 0.2, M["k_black"], rot=(0, 90, 30), n=10)
    # chairs round it (never on the lane side)
    place(g, folding_chair(S, M), TR(tx - 0.95, ty + 0.05, 0.0, 95))
    place(g, folding_chair(S, M), TR(tx + 0.95, ty - 0.05, 0.0, -80))
    place(g, folding_chair(S, M), TR(tx + 0.1, ty + 0.75, 0.0, 180))
    scatter_debris(g, M, tx - 1.0, tx + 1.0, 1.0, 2.0, 18, kinds=("butt", "butt", "butt", "paper"))
    for k in range(3):                                                                              # cans
        g.cyl((tx - 0.8 + k * 0.5, ty + R.uniform(-0.3, 0.4), 0.035), 0.033, 0.12, M["steel_mid"], rot=(90, 0, R.uniform(0, 180)), n=10)

    # ---------------------------------------------------- the chair he was tied to, knocked over
    hx = S.HERO_X
    place(g, wood_chair(S, M), TR(hx - 1.45, 1.35, 0.23, 25, 0, -88))
    for (dx, dy, a) in ((-0.75, 1.05, 30), (-0.55, 0.92, 150), (-1.0, 1.18, 260), (-0.4, 1.12, 80)):
        zip_tie(g, M, (hx + dx, dy, 0.0), a)
    g.cyl((hx - 0.2, 1.25, 0.0025), 0.13, 0.004, M["k_blood"], n=14)                               # where his head bled
    for k in range(5):
        g.cyl((hx - 0.2 + R.uniform(-0.35, 0.35), 1.25 + R.uniform(-0.15, 0.25), 0.0026), R.uniform(0.02, 0.05), 0.004, M["k_blood"], n=8)
    # the bucket they threw over him, on its side, and a roll of tape
    g.cyl((hx - 2.0, 1.55, 0.15), 0.15, 0.32, M["steel_mid"], rot=(90, 0, 40), n=16, r2=0.12)
    puddle(g, M, hx - 1.75, 1.25, 0.32)
    g.torus((hx - 1.2, 1.75, 0.025), 0.045, 0.022, M["k_mid"], n1=12, n2=5)
    g.cyl((hx - 1.5, 1.95, 0.012), 0.005, 0.24, M["steel_mid"], rot=(90, 0, 35), n=6)               # the snips they cut a tie with

    # ---------------------------------------------------- the bench: tools out, the vice, a grinder
    bx, by = -5.6, 2.15
    zb = 0.91
    g.box((bx + 0.85, by - 0.36, 0.98), (0.03, 0.18, 0.03), M["steel"], rot=(0, 90, 0))              # vice bar
    for (dx, dy, kind) in ((-0.9, -0.1, "hammer"), (-0.65, -0.2, "driver"), (-0.55, -0.15, "driver"), (-0.25, -0.15, "drill"),
                           (0.25, -0.1, "spanner"), (0.4, -0.22, "rag"), (0.55, 0.05, "tin")):
        x, y = bx + dx, by + dy
        if kind == "hammer":
            g.cyl((x, y, zb + 0.015), 0.013, 0.3, M["wood"], rot=(90, 0, 70), n=6)
            g.box((x + 0.05, y + 0.14, zb + 0.02), (0.1, 0.035, 0.035), M["steel"], rot=(0, 0, 70))
        elif kind == "driver":
            g.cyl((x, y, zb + 0.01), 0.012, 0.11, M["k_black"], rot=(90, 0, 15), n=6)
            g.cyl((x + 0.03, y - 0.1, zb + 0.01), 0.004, 0.1, M["steel_mid"], rot=(90, 0, 15), n=5)
        elif kind == "drill":
            bbox(g, (x, y, zb + 0.07), (0.22, 0.07, 0.1), M["k_mid"], w=0.015)
            g.box((x - 0.05, y, zb + 0.0), (0.06, 0.06, 0.12), M["k_black"], rot=(0, -15, 0))
            g.cyl((x + 0.14, y, zb + 0.08), 0.02, 0.06, M["steel"], rot=(0, 90, 0), n=8)
        elif kind == "spanner":
            g.box((x, y, zb + 0.004), (0.22, 0.025, 0.006), M["steel_mid"], rot=(0, 0, 20))
        elif kind == "rag":
            g.box((x, y, zb + 0.012), (0.2, 0.16, 0.02), M["k_white"], rot=(3, 4, 30))
        elif kind == "tin":
            g.cyl((x, y, zb + 0.06), 0.05, 0.12, M["steel_mid"], n=12)
    # the bench grinder at its end
    g.box((bx - 1.25, by, 0.45), (0.2, 0.2, 0.9), M["steel"])
    g.box((bx - 1.25, by, 0.92), (0.25, 0.14, 0.12), M["k_mid"])
    for s in (-1, 1):
        g.cyl((bx - 1.25 + s * 0.17, by, 0.95), 0.08, 0.03, M["k_mid"], rot=(0, 90, 0), n=14)
    # more tools on the pegboard
    for k in range(10):
        x = bx - 0.95 + k * 0.2
        g.cyl((x, BACK - 0.05, 2.15), 0.006, 0.05, M["steel"], rot=(90, 0, 0), n=5)
        if k % 2:
            g.box((x, BACK - 0.065, 2.0), (0.025, 0.012, 0.28), M["steel_mid"], rot=(0, R.uniform(-8, 8), 0))
    g.torus((bx + 0.95, BACK - 0.065, 1.95), 0.12, 0.012, M["rubber"], rot=(90, 0, 0), n1=14, n2=4)
    # a lamp clamped to the shelf, its arm bent over the vice
    g.beam((bx + 0.6, by + 0.25, zb), (bx + 0.6, by + 0.25, zb + 0.5), 0.02, 0.02, M["steel_mid"])
    g.beam((bx + 0.6, by + 0.25, zb + 0.5), (bx + 0.85, by - 0.05, zb + 0.4), 0.02, 0.02, M["steel_mid"])
    g.cyl((bx + 0.88, by - 0.08, zb + 0.35), 0.06, 0.1, M["paint_green"], rot=(25, 0, 0), n=12, r2=0.025)

    # ---------------------------------------------------- shelving between the press and the ladder
    shelving(S, g, M, 0.35, 2.6, 2.1, 2.52, 2.3, levels=5, label="MAX  150 KG")
    # a roller cabinet beside the lathe, lockers past the drums
    bbox(g, (7.3, 2.28, 0.5), (0.62, 0.45, 0.9), M["paint_red"], w=0.02)
    for k in range(5):
        g.box((7.3, 2.05, 0.2 + k * 0.15), (0.56, 0.008, 0.012), M["k_black"])
        g.box((7.3, 2.04, 0.25 + k * 0.15), (0.2, 0.02, 0.02), M["steel_mid"])
    for dx in (-0.25, 0.25):
        g.cyl((7.3 + dx, 2.1, 0.04), 0.035, 0.03, M["rubber"], rot=(90, 0, 0), n=8)
    g.box((7.3, 2.28, 0.97), (0.64, 0.47, 0.04), M["k_black"])
    jerrycan(g, M, 6.95, 1.95, 0.0, 10)
    # traffic cones and a pallet jack by the door (the door's lane stays clear)
    cone(g, M, S.DOOR_L - 0.35, 1.3)
    cone(g, M, S.DOOR_R + 0.2, 1.8)
    pallet_jack(g, M, 10.2, 1.35, 15)

    # ---------------------------------------------------- tarped pallets by the door, more stock
    tarped(g, M, (9.85, 1.95, 0.6), (1.25, 1.0, 0.7))
    load_on_pallet(S, g, M, -12.6, 1.55, kind="sacks", h=0.6)

    # ---------------------------------------------------- overhead: truss gussets, roof crack, chains
    x = -HW + 1.2
    while x < HW:
        y0, y1 = S.FRONT, BACK
        zb_, zt_ = ROOF - 0.95, ROOF - 0.08
        nodes = []
        for i in range(6):
            yy = y0 + (y1 - y0) * i / 5
            nodes += [(yy, zb_), (yy, zt_)]
        gussets(g, M, x, nodes, size=0.2)
        x += 3.4
    # sag rods and bracing in the roof plane
    for k, (xa, xb) in enumerate(((-HW + 1.2, -HW + 4.6), (HW - 3.0, HW - 0.2))):
        g.beam((xa, S.FRONT, ROOF - 0.12), (xb, BACK, ROOF - 0.12), 0.02, 0.02, M["steel"])
        g.beam((xa, BACK, ROOF - 0.12), (xb, S.FRONT, ROOF - 0.12), 0.02, 0.02, M["steel"])
    # the roof round the crack: torn sheet hanging, rust and water rings on the underside
    cx = S.HERO_X - 0.6
    g.box((cx, 0.6, ROOF - 0.04), (2.2, 3.4, 0.006), M["rust"])
    g.box((cx, 0.6, ROOF - 0.045), (3.2, 4.0, 0.004), M["k_stain_wall"])
    for (dx, y, a, L) in ((-0.95, -0.4, 35, 0.22), (0.9, 0.3, -40, 0.26), (-0.95, 1.7, 30, 0.2), (0.95, 1.4, -35, 0.18)):
        g.box((cx + dx, y, ROOF - 0.05 - L * 0.4), (0.012, 0.22, L), M["rust"], rot=(0, a, 0))
    # chains and hooks hung from the crane rail and the trusses, behind the lane
    for (x, L) in ((-12.0, 2.8), (-5.0, 3.6), (1.4, 2.4), (6.6, 3.1)):
        chain_hook(S, g, M, (x, 1.75, ROOF - 0.95), L)
    # lamp flex sagging between the lamps, along the purlin
    for (xa, xb) in ((-6.0, -0.4), (-0.4, 5.6), (5.6, 10.2)):
        for k in range(6):
            a, b = k / 6, (k + 1) / 6
            za = ROOF - 0.2 - 0.25 * math.sin(a * math.pi)
            zb2 = ROOF - 0.2 - 0.25 * math.sin(b * math.pi)
            g.beam((xa + (xb - xa) * a, 0.55, za), (xa + (xb - xa) * b, 0.55, zb2), 0.012, 0.012, M["rubber"])

    # ---------------------------------------------------- the window sills: grime and a dead bird's worth of leaves
    for cx in S.WINDOWS:
        for _ in range(6):
            g.box((cx + R.uniform(-1.2, 1.2), BACK + 0.02, S.WIN_Z0 - 0.02), (R.uniform(0.05, 0.2), 0.1, 0.012), M["k_paper"], rot=(0, 0, R.uniform(0, 90)))


# ================================================================ THE YARD, THE GROUND, THE CITY

def jersey(g, M, x, y, yaw, L=3.0):
    """A concrete road barrier (the New Jersey profile), lifting slots, a hazard band."""
    prof = [(-0.3, 0.0), (0.3, 0.0), (0.3, 0.08), (0.18, 0.3), (0.1, 0.81), (-0.1, 0.81), (-0.18, 0.3), (-0.3, 0.08)]
    sub = _Sub(g)
    sub.prism([(px, pz) for px, pz in prof], -L / 2, L / 2, M["concrete"])
    sub.box((0.0, 0.0, 0.62), (0.22, L * 0.6, 0.12), M["k_white"])
    for dy in (-L / 3, L / 3):
        sub.box((0.0, dy, 0.03), (0.62, 0.2, 0.06), M["k_black"])
    place(g, sub, TR(x, y, 0.0, yaw))


def _Sub(g):
    return g.__class__("tmp")


def chainlink(g, M, x0, x1, y, h=2.6, spacing=0.16, post=3.0):
    """Chain-link mesh between the fence posts: a diamond lattice of wire."""
    n = int((x1 - x0) / spacing)
    for k in range(n):
        x = x0 + k * spacing
        g.beam((x, y, 0.06), (x + h, y, h), 0.008, 0.008, M["steel_mid"], up=Vector((0, 1, 0)))
        g.beam((x + h, y, 0.06), (x, y, h), 0.008, 0.008, M["steel_mid"], up=Vector((0, 1, 0)))


def razor_wire(g, M, x0, x1, y, z, r=0.32, pitch=0.45):
    x = x0
    while x < x1:
        g.torus((x, y, z), r, 0.008, M["steel_mid"], rot=(0, 90 + R.uniform(-15, 15), 0), n1=10, n2=3)
        x += pitch


def flood_mast(g, M, x, y, h=24.0, heads=6, face=-90):
    """A high-mast floodlight: a tapered pole, a ring of lamp heads, a ladder, a base."""
    g.cyl((x, y, h / 2), 0.38, h, M["steel_mid"], n=12, r2=0.16)
    g.cyl((x, y, 0.3), 0.7, 0.6, M["concrete"], n=12)
    for a in range(8):
        ang = a / 8 * math.tau
        g.cyl((x + math.cos(ang) * 0.5, y + math.sin(ang) * 0.5, 0.62), 0.03, 0.08, M["steel"], n=6)
    g.torus((x, y, h + 0.2), 1.1, 0.06, M["steel"], n1=24, n2=5)
    g.box((x, y, h + 0.2), (2.2, 0.08, 0.08), M["steel"])
    g.box((x, y, h + 0.2), (0.08, 2.2, 0.08), M["steel"])
    for k in range(heads):
        ang = math.radians(face) + (k - (heads - 1) / 2) * 0.5
        hx, hy = x + math.cos(ang) * 1.15, y + math.sin(ang) * 1.15
        yaw = math.degrees(ang)
        bbox(g, (hx, hy, h + 0.45), (0.7, 0.55, 0.28), M["steel"], rot=(0, -35, yaw), w=0.02)
        g.box((hx + math.cos(ang) * 0.12, hy + math.sin(ang) * 0.12, h + 0.36), (0.6, 0.48, 0.03), M["k_flood"], rot=(0, -35, yaw))
    g.cyl((x, y, h + 1.6), 0.03, 2.4, M["steel"], n=6)
    g.cyl((x, y, h + 2.85), 0.12, 0.2, M["aviation"], n=8)
    z = 0.8
    while z < h:
        g.box((x, y - 0.45, z), (0.4, 0.03, 0.03), M["steel"])
        z += 0.3
    for dx in (-0.2, 0.2):
        g.box((x + dx, y - 0.45, h / 2), (0.03, 0.03, h - 1.2), M["steel"])


def guard_hut(g, M, x, y):
    bbox(g, (x, y, 1.35), (3.0, 2.2, 2.7), M["k_white"], w=0.04)
    g.box((x, y, 2.78), (3.5, 2.7, 0.16), M["steel"])
    for (dx, w) in ((-0.6, 1.2), (0.75, 0.9)):
        g.box((x + dx, y - 1.11, 1.7), (w, 0.03, 0.9), M["cab_window"])
        g.box((x + dx, y - 1.13, 1.7), (w + 0.1, 0.02, 0.06), M["steel"])
        g.box((x + dx, y - 1.13, 1.2), (w + 0.2, 0.12, 0.05), M["steel"])
    g.box((x + 1.51, y + 0.3, 1.0), (0.03, 0.85, 2.0), M["steel_mid"])
    g.box((x - 1.4, y + 1.2, 2.2), (0.6, 0.35, 0.45), M["steel_mid"])                  # AC unit
    g.cyl((x - 1.4, y + 1.38, 2.2), 0.16, 0.02, M["steel"], rot=(90, 0, 0), n=12)
    # the barrier arm and its post
    g.box((x + 2.3, y - 1.3, 0.5), (0.3, 0.3, 1.0), M["k_white"])
    for k in range(8):
        g.box((x + 2.6 + k * 0.7, y - 1.3, 0.95), (0.7, 0.07, 0.07), M["k_white"] if k % 2 else M["k_black"])
    g.box((x, y - 1.25, 2.55), (1.2, 0.04, 0.28), M["k_black"])
    text(g, "SECURITY", (x, y - 1.28, 2.55), 0.2, M["k_white"])


def skip(g, M, x, y, yaw):
    sub = _Sub(g)
    prof = [(-1.9, 0.1), (1.9, 0.1), (2.4, 1.6), (-2.4, 1.6)]
    sub.prism(prof, -0.85, 0.85, M["iron_dark"])
    for dx in (-1.2, 0.0, 1.2):
        sub.box((dx, -0.87, 0.85), (0.08, 0.04, 1.4), M["iron_dark"])
        sub.box((dx, 0.87, 0.85), (0.08, 0.04, 1.4), M["iron_dark"])
    for k in range(9):                                                         # scrap heaped in it
        sub.box((R.uniform(-1.8, 1.8), R.uniform(-0.6, 0.6), 1.5 + R.uniform(0, 0.4)), (R.uniform(0.3, 1.2), R.uniform(0.1, 0.5), R.uniform(0.05, 0.3)),
                R.choice((M["rust"], M["steel_mid"], M["wood_dark"])), rot=(R.uniform(-30, 30), R.uniform(-30, 30), R.uniform(0, 180)))
    sub.box((0.0, -0.87, 1.0), (1.3, 0.02, 0.35), M["k_white"])
    place(g, sub, TR(x, y, 0.0, yaw))


def cable_drum(g, M, x, y, yaw, r=0.9):
    for s in (-0.45, 0.45):
        g.cyl((x + math.cos(math.radians(yaw)) * s, y + math.sin(math.radians(yaw)) * s, r), r, 0.06, M["wood"], rot=(90, 0, yaw + 90), n=20)
    g.cyl((x, y, r), r * 0.62, 0.84, M["rubber"], rot=(90, 0, yaw + 90), n=16)


def tractor_unit(g, M, x, y, yaw):
    """A truck's tractor unit: cab, sleeper, fuel tanks, wheels, the fifth wheel."""
    sub = _Sub(g)
    bbox(sub, (1.2, 0.0, 2.0), (1.9, 2.45, 2.5), M["k_mid"], w=0.08)
    sub.box((2.17, 0.0, 2.45), (0.03, 2.1, 0.9), M["glass"])
    sub.box((2.2, 0.0, 1.2), (0.08, 2.3, 0.5), M["steel"])                     # bumper/grille
    for dy in (-0.9, 0.9):
        sub.box((2.22, dy, 1.45), (0.04, 0.35, 0.15), M["bulb_dead"])
        sub.box((2.0, dy * 1.36, 2.6), (0.08, 0.06, 0.45), M["steel"])          # mirrors
    sub.box((-1.5, 0.0, 1.0), (4.5, 1.0, 0.25), M["steel"])                    # chassis
    sub.cyl((-2.4, 0.0, 1.2), 0.7, 0.12, M["steel_mid"], n=16)                 # fifth wheel
    for dy in (-0.95, 0.95):
        sub.cyl((-0.2, dy, 0.85), 0.32, 0.9, M["steel_mid"], rot=(0, 90, 0), n=14)
    for wx in (1.4, -2.0, -3.3):
        for dy in (-1.0, 1.0):
            sub.cyl((wx, dy, 0.52), 0.52, 0.32, M["rubber"], rot=(90, 0, 0), n=16)
            sub.cyl((wx, dy * 1.17, 0.52), 0.25, 0.02, M["steel_mid"], rot=(90, 0, 0), n=12)
    sub.cyl((0.1, 1.05, 2.6), 0.1, 2.2, M["steel_mid"], n=8)                    # exhaust stack
    place(g, sub, TR(x, y, 0.0, yaw))


def yard_detail(S, g, M):
    """The yard: the fence finished (mesh, razor wire, a gate and the hut), high-mast floods,
    barriers along the road, a parked truck, skips, cable drums, the shed's outside dressed."""
    HW, BACK, ROOF = S.HALF_W, S.BACK, S.ROOF
    # ---------------------------------------------------- the fence: mesh, arms, razor wire, the gate
    chainlink(g, M, -60.0, -9.0, -28.0)
    chainlink(g, M, -2.0, 60.0, -28.0)
    x = -60.0
    while x <= 60.0:
        g.beam((x, -28.0, 2.8), (x, -28.3, 3.3), 0.04, 0.04, M["steel_mid"])
        x += 3.0
    for dz, dy in ((2.95, -0.1), (3.1, -0.2), (3.25, -0.28)):
        g.box((0, -28.0 + dy, dz), (120.0, 0.012, 0.012), M["steel_mid"])
    razor_wire(g, M, -60.0, 60.0, -28.15, 3.05)
    for gx in (-9.0, -2.0):
        g.cyl((gx, -28.0, 1.6), 0.08, 3.2, M["steel"], n=10)
    for k, (ga, gb) in enumerate(((-9.0, -5.6), (-5.4, -2.0))):
        for z in (0.15, 2.4):
            g.box(((ga + gb) / 2, -28.0, z), (gb - ga, 0.05, 0.05), M["steel_mid"])
        for xx in (ga + 0.05, gb - 0.05):
            g.box((xx, -28.0, 1.28), (0.05, 0.05, 2.3), M["steel_mid"])
        g.beam((ga, -28.0, 0.15), (gb, -28.0, 2.4), 0.04, 0.04, M["steel_mid"], up=Vector((0, 1, 0)))
        chainlink(g, M, ga + 0.1, gb - 2.4, -27.98, h=2.2, spacing=0.2)
    sign(g, M, "PRIVATE PROPERTY", (-6.5, -28.05, 1.5), 1.4, 0.45, sub="NO ENTRY  -  DOGS ON PATROL", facing=-1, dark_plate=True)
    guard_hut(g, M, -12.0, -24.5)
    # ---------------------------------------------------- high masts
    for (mx, my, f) in ((-30.0, 11.25, -60), (-2.0, 37.3, -90), (48.0, 37.3, -120)):
        flood_mast(g, M, mx, my, face=f)
    # ---------------------------------------------------- the road to the shed: barriers, a truck, skips
    y = 6.0
    while y < 34.0:
        jersey(g, M, -17.2, y, 90 + R.uniform(-3, 3))
        if R.random() < 0.7:
            jersey(g, M, 11.2, y + 1.4, 90 + R.uniform(-3, 3))
        y += 3.2
    tractor_unit(g, M, -12.5, 24.0, -90)
    sub = _Sub(g)
    S.H.trailer(sub, M, 0.0, 0.0, 12.0)
    place(g, sub, TR(-12.5, 30.9, 0.0, 90))
    skip(g, M, 6.5, 9.5, 8)
    skip(g, M, -21.5, 1.0, 95)
    cable_drum(g, M, 3.0, 14.0, 20)
    cable_drum(g, M, 4.9, 15.2, 75, r=0.7)
    for (px, py, n) in ((-8.0, 5.5, 9), (-6.6, 5.6, 6), (-7.3, 6.9, 11), (8.5, 4.8, 7)):
        pallet_stack(S, g, M, px, py, n)
    for (dx, dy) in ((-15.0, 4.2), (-14.4, 4.6), (-14.7, 5.3), (9.0, 7.0)):
        S.drum(g, (dx, dy, 0.44), M, R.choice((M["paint_blue"], M["paint_red"], M["rust"])))
    for k in range(5):                                                         # tyres heaped by the skip
        g.torus((5.0 + R.uniform(-0.5, 0.5), 11.5 + R.uniform(-0.5, 0.5), 0.15 + k * 0.22), 0.45, 0.17, M["rubber"], rot=(R.uniform(-15, 15), R.uniform(-15, 15), 0), n1=16, n2=6)
    for (cx, cy) in ((-15.5, 6.5), (-15.5, 12.0), (9.8, 6.4)):
        cone(g, M, cx, cy)
    # ---------------------------------------------------- the shed from outside: letters, vents, roof ribs, a door
    for (x0, ch) in ((0.8, "B"), (-0.5, "A"), (-1.8, "Y"), (-4.0, "7")):        # read from the yard (+y)
        text(g, ch, (x0, BACK + 0.34, 5.15), 1.15, M["k_black"], facing=1)
    x = -HW
    while x < HW:
        if not (S.CRACK_X0 - 0.4 < x < S.CRACK_X1 + 0.6):
            g.box((x, (S.FRONT + BACK) / 2, ROOF + 0.37), (0.06, BACK - S.FRONT + 0.6, 0.04), M["rust"])
        x += 0.6
    for vx in (-11.0, -4.0, 3.0, 10.0):                                        # turbine vents
        g.cyl((vx, 1.2, ROOF + 0.5), 0.18, 0.3, M["steel_mid"], n=10)
        g.cyl((vx, 1.2, ROOF + 0.85), 0.32, 0.45, M["steel_mid"], n=16, r2=0.22)
        g.cyl((vx, 1.2, ROOF + 1.1), 0.08, 0.06, M["steel"], n=8)
    g.box((0, S.FRONT - 0.15, ROOF + 0.38), (HW * 2 + 0.6, 0.12, 0.16), M["steel_mid"])     # front gutter
    for (dx, z) in ((-HW + 0.4, 0.0), (HW - 0.4, 0.0)):
        for zz in (1.5, 3.5, 5.5):
            g.box((dx, BACK + 0.55, zz), (0.2, 0.2, 0.05), M["steel"])
    # a fire door in the back wall, its canopy, a lamp over it, steps
    fx = -8.0
    g.box((fx, BACK + 0.32, 1.05), (1.0, 0.05, 2.1), M["steel_mid"])
    g.box((fx + 0.3, BACK + 0.36, 1.0), (0.3, 0.04, 0.04), M["steel"])
    g.box((fx, BACK + 0.8, 2.45), (1.6, 1.0, 0.08), M["steel"])
    for dx in (-0.75, 0.75):
        g.beam((fx + dx, BACK + 0.3, 1.9), (fx + dx, BACK + 1.25, 2.42), 0.04, 0.04, M["steel"])
    cage_lamp(g, M, fx, BACK + 0.32, 2.75, True, facing=1)
    for k in range(3):
        g.box((fx, BACK + 0.5 + k * 0.3, 0.15 * (2 - k) + 0.075), (1.4, 0.3 + (2 - k) * 0.0, 0.15), M["concrete"])
    sign(g, M, "FIRE EXIT", (fx, BACK + 0.36, 2.25), 0.7, 0.18, facing=1, dark_plate=True)
    for (wx, wz) in ((-12.5, 3.2), (5.0, 3.2), (12.0, 4.0)):
        cage_lamp(g, M, wx, BACK + 0.3, wz, True, facing=1)
    # a tarp-covered stack and the bins against the shed
    tarped(g, M, (-11.6, BACK + 1.5, 0.0), (2.2, 1.4, 1.5))
    for k in range(3):
        bx = 4.0 + k * 0.85
        bbox(g, (bx, BACK + 0.85, 0.55), (0.75, 0.75, 1.1), M["k_mid"], w=0.03)
        g.box((bx, BACK + 0.85, 1.13), (0.8, 0.8, 0.05), M["k_black"], rot=(R.uniform(-8, 8), 0, 0))
        for dx in (-0.25, 0.25):
            g.cyl((bx + dx, BACK + 0.5, 0.08), 0.08, 0.05, M["rubber"], rot=(0, 90, 0), n=8)


def ground_detail(S, g, M):
    """The yard's ground: puddles everywhere the rain lies, drains, manholes, arrows, oil."""
    for _ in range(36):
        x, y = R.uniform(-58, 58), R.uniform(-27, 46)
        if -22 < x < 22 and y < 7:
            continue
        if 13.0 < x < 63.0 and y < 21.5:                    # under the hall, or between it and the camera
            continue
        puddle(g, M, x, y, R.uniform(1.0, 3.2), M["k_puddle_yard"], n=14)
    for _ in range(40):
        x, y = R.uniform(-58, 58), R.uniform(-27, 46)
        if 13.0 < x < 63.0 and y < 21.5:
            continue
        if -22 < x < 22 and y < 7:
            continue
        g.cyl((x, y, 0.0025), R.uniform(0.3, 1.2), 0.004, M["oil"], n=12)
    for (x, y) in ((-16.0, 3.5), (-16.0, 18.0), (10.5, 12.0), (-5.0, -14.0), (25.0, 26.0), (-40.0, 3.0)):
        g.box((x, y, 0.003), (0.6, 0.6, 0.006), M["steel"])
        for k in range(6):
            g.box((x - 0.22 + k * 0.088, y, 0.006), (0.04, 0.54, 0.006), M["k_black"])
    for (x, y) in ((-3.0, 18.0), (-8.0, 30.0), (30.0, 40.0), (-35.0, 25.0)):
        g.cyl((x, y, 0.004), 0.38, 0.008, M["steel"], n=18)
        for k in range(4):
            g.box((x, y, 0.009), (0.6, 0.03, 0.004), M["k_black"], rot=(0, 0, k * 45))
    for y in (10.0, 20.0, 30.0):                                                   # arrows down the road to the shed
        g.box((-3.0, y + 0.2, 0.004), (0.3, 2.4, 0.006), M["k_white"])
        g.box((-3.28, y - 1.3, 0.004), (0.9, 0.25, 0.006), M["k_white"], rot=(0, 0, -45))
        g.box((-2.72, y - 1.3, 0.004), (0.9, 0.25, 0.006), M["k_white"], rot=(0, 0, 45))
    text(g, "SLOW", (2.0, 16.0, 0.006), 1.6, M["k_white"], floor=True, rot=-90)
    for k in range(40):                                                            # tyre tracks through the wet
        x = -10.0 + R.uniform(-4, 4)
        y = 6.0 + k * 0.8
        g.box((x, y, 0.0022), (0.35, 0.75, 0.003), M["k_stain"], rot=(0, 0, R.uniform(-5, 5)))


def tower_dress(g, M, x, y, w, h, front):
    """One of the city's towers: a setback crown, facade fins, a plant room, masts, a crane
    on the odd one still going up."""
    yf = y + front + 0.1
    if R.random() < 0.55:
        g.box((x, y, h + 2.5), (w * 0.7, 6.0, 5.0), M["city"])
        if R.random() < 0.5:
            g.box((x, y, h + 6.2), (w * 0.4, 4.0, 2.5), M["city"])
    n = max(2, int(w / 3.0))
    for k in range(n + 1):
        g.box((x - w / 2 + k * w / n, yf + 0.15, h / 2), (0.25, 0.3, h), M["k_haze1"])
    for k in range(int(h / 12)):
        g.box((x, yf + 0.2, 6 + k * 12), (w, 0.3, 0.4), M["k_haze1"])
    if R.random() < 0.4:
        for dx in (-w * 0.25, w * 0.2):
            g.cyl((x + dx, y, h + 1.5), 1.3, 3.0, M["city"], n=10)
            g.cyl((x + dx, y, h + 3.2), 1.4, 0.6, M["city"], n=10, r2=0.2)
    if R.random() < 0.3:
        for k in range(3):
            g.cyl((x + R.uniform(-w / 3, w / 3), y + R.uniform(-2, 2), h + 4), 0.12, 8.0 + k * 3, M["steel"], n=5)
    if R.random() < 0.07:                                                         # a tower crane on top
        g.box((x, y, h + 14), (1.2, 1.2, 28), M["crane"])
        g.box((x + 10, y, h + 28.5), (32, 1.0, 1.0), M["crane"])
        g.box((x - 8, y, h + 27), (5, 2.0, 2.0), M["city"])
        g.cyl((x + 24, y, h + 28.5 - 8), 0.04, 16, M["steel"], n=4)
        g.cyl((x + 26, y, h + 29.2), 0.3, 0.3, M["aviation"], n=8)


def city_detail(S, g, M):
    """The skyline behind the near towers: two more layers, each paler in the haze and with
    fewer lit windows; and an elevated road across the front with its lamps."""
    for (y0, y1, hmax, m, lit) in ((-420.0, -360.0, 170.0, M["k_haze3"], 0.06), (-330.0, -260.0, 140.0, M["k_haze2"], 0.1)):
        x = -420.0
        while x < 420.0:
            w = R.uniform(12, 40)
            h = R.choice((R.uniform(20, 60), R.uniform(40, 100), R.uniform(70, hmax)))
            y = R.uniform(y0, y1)
            g.box((x, y, h / 2), (w, 20, h), m)
            if R.random() < 0.5:
                g.box((x, y, h + 3), (w * 0.6, 10, 6), m)
            cols = max(2, int(w / 4.0))
            for fl in range(int(h / 4.0)):
                for c in range(cols):
                    if R.random() < 1.0 - lit:
                        continue
                    g.box((x - w / 2 + (c + 0.5) * w / cols, y + 10.1, 3 + fl * 4.0), (w / cols * 0.6, 0.05, 1.6), M["k_city_win_dim"])
            if h > 110:
                g.cyl((x, y, h + 10), 0.6, 14, m, n=6)
                g.cyl((x, y, h + 17.5), 0.9, 0.8, M["aviation"], n=8)
            x += w + R.uniform(-4, 10)
    # the elevated road: deck, piers, lamp posts with their lamps, a few cars' lights
    yb = -120.0
    g.box((0.0, yb, 12.0), (560.0, 14.0, 1.6), M["k_haze1"])
    g.box((0.0, yb + 7.2, 13.2), (560.0, 0.4, 1.0), M["k_haze1"])
    x = -280.0
    while x < 280.0:
        g.box((x, yb, 5.6), (2.4, 6.0, 11.2), M["k_haze1"])
        x += 32.0
    x = -276.0
    while x < 280.0:
        g.cyl((x, yb + 6.8, 17.0), 0.15, 8.0, M["city"], n=5)
        g.box((x + 0.8, yb + 6.0, 20.9), (1.2, 0.4, 0.25), M["sodium"])
        x += 24.0
    for _ in range(26):
        cx = R.uniform(-270, 270)
        g.box((cx, yb + R.choice((-3.0, 3.0)), 13.4), (0.8, 1.8, 0.3), M["city_win"] if R.random() < 0.5 else M["aviation"])


# ================================================================ THE HALL

def bench_kit(g, M, x, y, w=2.2, back_panel=True):
    """A steel workbench with its tools out, a vice, a shelf of tins, a pegboard on its own frame."""
    bbox(g, (x, y, 0.88), (w, 0.75, 0.05), M["wood"], w=0.008)
    for dx in (-w / 2 + 0.05, w / 2 - 0.05):
        for dy in (-0.32, 0.32):
            g.box((x + dx, y + dy, 0.43), (0.05, 0.05, 0.86), M["steel"])
    g.box((x, y, 0.2), (w - 0.1, 0.65, 0.03), M["wood_dark"])
    for k in range(4):
        g.cyl((x - w / 2 + 0.3 + k * 0.22, y + 0.1, 0.3), 0.07, 0.17, R.choice((M["steel_mid"], M["paint_red"], M["paint_blue"])), n=12)
    bbox(g, (x + w / 2 - 0.3, y - 0.25, 0.99), (0.22, 0.16, 0.16), M["paint_blue"], w=0.015)
    g.box((x + w / 2 - 0.3, y - 0.38, 0.99), (0.03, 0.2, 0.03), M["steel"], rot=(0, 90, 0))
    for k in range(6):
        kx = x - w / 2 + 0.25 + k * (w - 0.7) / 5
        kind = k % 3
        if kind == 0:
            g.box((kx, y - 0.1, 0.915), (0.22, 0.03, 0.01), M["steel_mid"], rot=(0, 0, R.uniform(0, 60)))
        elif kind == 1:
            g.cyl((kx, y + 0.05, 0.92), 0.013, 0.28, M["wood"], rot=(90, 0, R.uniform(0, 180)), n=6)
        else:
            bbox(g, (kx, y + 0.1, 0.96), (0.25, 0.1, 0.12), M["k_mid"], w=0.01)
    if back_panel:
        yb = y + 0.45
        for dx in (-w / 2 + 0.05, w / 2 - 0.05):
            g.box((x + dx, yb, 1.25), (0.05, 0.05, 2.5), M["steel"])
        g.box((x, yb, 1.75), (w, 0.03, 1.0), M["wood_dark"])
        for k in range(12):
            tx = x - w / 2 + 0.2 + k * (w - 0.4) / 11
            h = R.uniform(0.15, 0.45)
            g.box((tx, yb - 0.025, 1.9 - h / 2), (R.uniform(0.02, 0.06), 0.015, h), M["steel_mid"], rot=(0, R.uniform(-10, 10), 0))
        g.box((x, yb - 0.05, 2.45), (w + 0.1, 0.25, 0.03), M["steel_mid"])
        for k in range(5):
            carton(g, M, (x - w / 2 + 0.3 + k * 0.4, yb - 0.05, 2.58), (0.3, 0.22, 0.22), label=False)


def drill_press(g, M, x, y):
    g.box((x, y, 0.05), (0.5, 0.6, 0.1), M["steel"])
    g.cyl((x, y + 0.15, 0.9), 0.05, 1.7, M["steel_mid"], n=10)
    g.box((x, y - 0.05, 0.85), (0.4, 0.4, 0.04), M["steel_mid"])
    bbox(g, (x, y + 0.05, 1.6), (0.3, 0.55, 0.32), M["paint_green"], w=0.03)
    g.cyl((x, y - 0.15, 1.35), 0.04, 0.2, M["steel"], n=8)
    for a in range(3):
        ang = math.radians(a * 120)
        g.beam((x + 0.16, y - 0.05, 1.5), (x + 0.16 + 0.2 * math.cos(ang) * 0.3, y - 0.05 + 0.2 * math.cos(ang), 1.5 + 0.2 * math.sin(ang)), 0.015, 0.015, M["steel"])
    g.box((x, y + 0.05, 1.82), (0.2, 0.4, 0.12), M["steel"])


def welding_cart(g, M, x, y):
    g.box((x, y, 0.35), (0.7, 0.45, 0.04), M["steel"])
    g.box((x, y, 0.7), (0.7, 0.45, 0.04), M["steel"])
    for dx in (-0.33, 0.33):
        for dy in (-0.2, 0.2):
            g.box((x + dx, y + dy, 0.45), (0.03, 0.03, 0.9), M["steel"])
    bbox(g, (x, y, 0.85), (0.45, 0.35, 0.26), M["k_mid"], w=0.02)
    g.box((x, y - 0.18, 0.88), (0.2, 0.01, 0.1), M["k_black"])
    for k in range(2):
        g.cyl((x - 0.12 + k * 0.24, y + 0.32, 0.75), 0.1, 1.4, (M["k_black"], M["k_mid"])[k], n=12)
        g.cyl((x - 0.12 + k * 0.24, y + 0.32, 1.5), 0.05, 0.1, M["steel_mid"], n=8)
    g.torus((x + 0.2, y - 0.3, 0.6), 0.2, 0.012, M["rubber"], rot=(90, 0, 0), n1=14, n2=4)
    for k in range(3):                                                         # a welding screen
        g.box((x + 1.2, y + k * 0.0, 0.9), (0.02, 1.0, 1.6), M["k_black"], rot=(0, 0, 15))


def lockers(g, M, x0, n, y, facing=-1):
    for k in range(n):
        x = x0 + k * 0.42
        bbox(g, (x, y, 0.95), (0.4, 0.5, 1.9), M["k_mid"], w=0.01)
        g.box((x, y + facing * 0.252, 0.95), (0.006, 0.004, 1.86), M["k_black"])
        for z in (1.6, 1.65, 1.7, 0.3, 0.35, 0.4):
            g.box((x, y + facing * 0.253, z), (0.25, 0.004, 0.012), M["k_black"])
        g.box((x + 0.13, y + facing * 0.26, 1.0), (0.03, 0.02, 0.1), M["steel"])
        bbox(g, (x, y + facing * 0.254, 1.3), (0.12, 0.004, 0.05), M["k_white"], w=0.001)
    g.box((x0 + (n - 1) * 0.21, y, 0.05), (n * 0.42, 0.5, 0.1), M["concrete_dark"])


def foreman_office(g, M, mz):
    """The Foreman's office on the mezzanine: his desk, the CRT and the phone, papers, the
    hard hat, the safe, a calendar, the duty board, a hi-vis on the hook, a clock."""
    ox0, ox1, oy0, oy1 = 45.5, 50.0, 0.9, 3.3
    z = mz
    # floor and skirting
    g.box(((ox0 + ox1) / 2, (oy0 + oy1) / 2, z + 0.004), (ox1 - ox0 - 0.12, oy1 - oy0 - 0.12, 0.008), M["k_mid"])
    # the desk (S builds the top and the pedestal; here the drawers and what is on it)
    for k in range(3):
        g.box((46.55, 1.99, z + 0.2 + k * 0.2), (0.4, 0.01, 0.012), M["k_black"])
        g.box((46.55, 1.985, z + 0.28 + k * 0.2), (0.12, 0.02, 0.02), M["steel_mid"])
    zt = z + 0.775
    bbox(g, (47.3, 2.55, zt + 0.17), (0.38, 0.36, 0.34), M["k_mid"], w=0.03)          # the CRT
    g.box((47.3, 2.37, zt + 0.19), (0.3, 0.01, 0.23), M["k_screen"])
    g.box((47.3, 2.7, zt + 0.14), (0.28, 0.2, 0.24), M["k_mid"])
    g.box((47.3, 2.25, zt + 0.015), (0.42, 0.14, 0.025), M["k_mid"])                   # keyboard
    bbox(g, (46.55, 2.3, zt + 0.04), (0.2, 0.22, 0.08), M["k_black"], w=0.01)          # the phone
    g.box((46.55, 2.3, zt + 0.1), (0.22, 0.06, 0.04), M["k_black"])
    for k in range(6):                                                                 # papers, a ledger
        g.box((46.9 + R.uniform(-0.3, 0.2), 2.15 + R.uniform(-0.1, 0.25), zt + 0.002 + k * 0.002), (0.21, 0.297, 0.002), M["k_paper"], rot=(0, 0, R.uniform(-25, 25)))
    bbox(g, (47.65, 2.15, zt + 0.02), (0.3, 0.22, 0.04), M["k_black"], w=0.005)
    g.cyl((46.3, 2.6, zt + 0.05), 0.04, 0.1, M["k_white"], n=12)                       # his mug
    g.cyl((47.75, 2.55, zt + 0.015), 0.07, 0.03, M["glass"], n=14)                     # ashtray
    # his hard hat, upside down on the desk
    g.cyl((46.3, 2.25, zt + 0.08), 0.15, 0.16, M["k_white"], n=16, r2=0.11)
    g.cyl((46.3, 2.25, zt + 0.005), 0.19, 0.01, M["k_white"], n=16)
    # the desk lamp
    g.cyl((47.75, 2.75, zt + 0.01), 0.08, 0.02, M["k_black"], n=12)
    g.beam((47.75, 2.75, zt), (47.7, 2.65, zt + 0.35), 0.02, 0.02, M["k_black"])
    g.cyl((47.62, 2.55, zt + 0.32), 0.07, 0.12, M["k_black"], rot=(40, 0, 0), n=12, r2=0.03)
    # his chair
    bbox(g, (47.0, 2.95, z + 0.48), (0.5, 0.48, 0.08), M["k_black"], w=0.02)
    bbox(g, (47.0, 3.18, z + 0.85), (0.48, 0.08, 0.6), M["k_black"], rot=(8, 0, 0), w=0.02)
    g.cyl((47.0, 2.95, z + 0.25), 0.03, 0.4, M["steel_mid"], n=8)
    for a in range(5):
        ang = a / 5 * math.tau
        g.beam((47.0, 2.95, z + 0.06), (47.0 + 0.28 * math.cos(ang), 2.95 + 0.28 * math.sin(ang), z + 0.04), 0.035, 0.03, M["k_black"])
    # filing cabinets (S builds three blocks at 49.4): their drawers and a box file on top
    for k in range(3):
        for d in range(4):
            g.box((49.17, 1.6 + k * 0.5, z + 0.15 + d * 0.3), (0.01, 0.4, 0.008), M["k_black"])
            g.box((49.165, 1.6 + k * 0.5, z + 0.25 + d * 0.3), (0.02, 0.12, 0.02), M["steel"])
    for k in range(4):
        bbox(g, (49.4, 1.5 + k * 0.08, z + 1.42), (0.3, 0.06, 0.24), R.choice((M["k_black"], M["k_mid"])), w=0.004)
    # the safe in the corner
    bbox(g, (45.95, 3.0, z + 0.4), (0.6, 0.5, 0.8), M["k_black"], w=0.03)
    g.cyl((45.95, 2.74, z + 0.5), 0.07, 0.03, M["steel_mid"], rot=(90, 0, 0), n=14)
    g.box((46.1, 2.74, z + 0.3), (0.12, 0.03, 0.03), M["steel_mid"])
    # on the back wall: a calendar, the duty board, a clock, a map of the yard
    yb = oy1 - 0.06
    g.box((46.6, yb, z + 1.75), (0.32, 0.01, 0.45), M["k_paper"])
    g.box((46.6, yb - 0.006, z + 1.92), (0.3, 0.004, 0.1), M["k_black"])
    for r in range(4):
        for c in range(6):
            g.box((46.47 + c * 0.05, yb - 0.006, z + 1.8 - r * 0.06), (0.03, 0.002, 0.03), M["k_black"] if (r * 6 + c) % 7 else M["k_paper"])
    bbox(g, (48.0, yb, z + 1.7), (1.1, 0.02, 0.7), M["k_white"], w=0.008)
    g.box((48.0, yb, z + 1.33), (1.1, 0.05, 0.03), M["steel"])
    for r in range(6):
        g.box((47.9 + R.uniform(-0.1, 0.1), yb - 0.012, z + 1.95 - r * 0.09), (R.uniform(0.4, 0.8), 0.002, 0.02), M["k_black"])
    text(g, "NIGHT  -  BAY 7", (48.0, yb - 0.014, z + 2.0), 0.07, M["k_black"])
    g.cyl((49.3, yb, z + 2.2), 0.13, 0.03, M["k_white"], rot=(90, 0, 0), n=18)
    g.torus((49.3, yb - 0.02, z + 2.2), 0.13, 0.012, M["k_black"], rot=(90, 0, 0), n1=18, n2=4)
    g.box((49.3, yb - 0.025, z + 2.24), (0.008, 0.004, 0.08), M["k_black"])
    g.box((49.33, yb - 0.025, z + 2.2), (0.06, 0.004, 0.008), M["k_black"], rot=(0, -30, 0))
    # the hi-vis on its hook by the door, and a jacket
    for (hx, m) in ((45.75, M["k_white"]), (45.95, M["k_black"])):
        g.box((hx, oy0 + 0.16, z + 1.9), (0.03, 0.06, 0.03), M["steel"])
        g.box((hx, oy0 + 0.25, z + 1.55), (0.34, 0.08, 0.65), m, rot=(0, 0, 90))
    for zz in (1.45, 1.6):
        g.box((45.75, oy0 + 0.2, z + zz), (0.02, 0.1, 0.03), M["k_black"], rot=(0, 0, 90))
    # the door in the end wall (to the stairs side) and the strip light's fitting
    g.box((ox0 - 0.06, 2.1, z + 1.0), (0.04, 0.85, 2.0), M["k_mid"])
    g.box((ox0 - 0.09, 1.8, z + 1.0), (0.03, 0.1, 0.03), M["steel"])
    text(g, "OFFICE", (ox0 - 0.09, 2.1, z + 1.7), 0.09, M["k_white"], facing=0)
    g.box((47.8, 2.0, z + 2.6 - 0.04), (1.5, 0.22, 0.04), M["steel_mid"])


def hall_detail(S, g, M):
    """Dress the hall: x 14.3..62, the lane at y 0, the back wall's inner face at y 20.4, the eaves
    at 9 and the ridge at 12 (over y 9.4). Fight-lane rules: nothing new at floor level in
    |y| < 0.85, nothing hanging over the lane, the background spawn corridors kept clear."""
    H = S.H
    X0, X1, F, B, EAVE, RIDGE = H.HALL_X0, H.HALL_X1, H.HALL_F, H.HALL_B, H.EAVE, H.RIDGE
    RY = H.RIDGE_Y
    yw = B - 0.06                                         # what is fixed to the back wall
    mz = H.MEZZ[2]
    spawn = [21.65, 33.5, 45.5, 57.5]                     # background spawns (x, y 3.5): keep a path to the lane
    cols = [X0 + 2.7 + 6.0 * k for k in range(8)]

    def free(x, y, r=0.8):
        if abs(y) < LANE + r:
            return False
        return not any(abs(x - sx) < 0.9 + r and y < 4.6 + r for sx in spawn)

    # ---------------------------------------------------- the floor
    floor_cracks(g, M, X0 + 0.5, X1 - 0.5, -1.4, B - 0.5, 60, M["concrete_dark"])
    for (sx0, sx1) in H.SKYLIGHTS:                                    # rain lies under every skylight
        for yy in (3.2, 4.8, 13.4, 15.2):
            if (sx0, sx1) == H.TORN or R.random() < 0.7:
                puddle(g, M, (sx0 + sx1) / 2 + R.uniform(-0.8, 0.8), yy + R.uniform(-0.5, 0.5), R.uniform(0.35, 0.8), M["puddle_hall"])
    for (px, py, r) in ((55.6, 2.6, 1.2), (57.0, 4.4, 0.9), (54.2, 5.2, 0.6), (61.0, 1.8, 0.9), (62.6, 1.8, 1.3)):
        puddle(g, M, px, py, r, M["puddle_hall"])
    scatter_debris(g, M, X0 + 0.5, X1 - 0.5, 1.0, B - 0.5, 260)
    scatter_debris(g, M, X0 + 0.5, X1 - 0.5, -1.4, 1.0, 60, kinds=("paper", "butt", "chip", "bolt"))
    for (sx0, sx1) in (H.TORN,):                                      # glass from the torn skylight, everywhere under it
        for _ in range(90):
            g.box((R.uniform(sx0 - 1, sx1 + 1), R.uniform(-1.2, 7.0), 0.004), (R.uniform(0.02, 0.14), R.uniform(0.02, 0.09), 0.004), M["glass"], rot=(0, 0, R.uniform(0, 180)))
    for k in range(60):                                                # forklift tyre tracks from the docks
        t = k / 60
        for dy in (-0.45, 0.45):
            x = 18.0 + t * 14.0
            y = 18.5 - t * 15.0 + math.sin(t * 3.0) * 1.2
            g.box((x + dy * 0.6, y + dy, 0.0021), (0.18, 0.5, 0.003), M["k_stain"], rot=(0, 0, -45 + t * 30))
    for k, cx in enumerate(cols):                                     # bay numbers painted by the wall
        text(g, "%02d" % (k + 1), (cx + 3.0, 18.6, 0.0045), 0.9, M["paint_yellow"], floor=True)
    for x in (24.0, 38.5, 52.0):                                      # forklift route arrows on the far aisle
        g.box((x, 2.9, 0.0035), (1.6, 0.12, 0.003), M["paint_yellow"])
        g.box((x + 0.75, 2.82, 0.0035), (0.45, 0.1, 0.003), M["paint_yellow"], rot=(0, 0, 40))
        g.box((x + 0.75, 2.98, 0.0035), (0.45, 0.1, 0.003), M["paint_yellow"], rot=(0, 0, -40))
    for gx in (20.0, 34.0, 48.0, 58.0):                               # trench drains along the back
        g.box((gx, 18.0, 0.003), (3.0, 0.3, 0.006), M["k_grating"])
        g.box((gx, 18.0, 0.002), (3.08, 0.36, 0.004), M["steel"])

    # ---------------------------------------------------- the back wall: weather, columns, services
    for k, cx in enumerate(cols):
        streaks(g, M, cx - 1.5, cx + 1.5, 5.1, B - 0.003, 7, M["k_stain_wall"], length=(1.0, 3.6), z_min=1.0)
        streaks(g, M, cx - 1.4, cx + 1.4, 5.1, B - 0.004, 3, M["rust"], length=(0.4, 1.5), width=(0.03, 0.1))
        streaks(g, M, cx - 3.0, cx + 3.0, EAVE - 0.05, B - 0.003, 4, M["k_stain_wall"], length=(0.5, 0.8), width=(0.2, 0.6), z_min=8.2)
        column_detail(g, M, cx, B - 0.2, EAVE, n="A%d" % (k + 1), lamp_z=4.3, lit=k not in (2, 6))
        g.beam((cx, B - 0.2, EAVE - 1.6), (cx, B - 1.3, EAVE - 0.05), 0.12, 0.12, M["steel"])          # knee brace to the truss
    g.box(((X0 + X1) / 2, B - 0.003, 0.95), (X1 - X0, 0.004, 0.1), M["k_stain_wall"])                  # tide line over the plinth
    # girts between the columns (the wall's rails), above the plinth and under the windows
    for z in (2.0, 4.85):
        g.box(((X0 + X1) / 2, B - 0.05, z), (X1 - X0, 0.08, 0.16), M["steel"])
    # the pipes: brackets, flanges, two valves, a gauge, a branch down to a hydrant
    for x in range(int(X0) + 1, int(X1), 2):
        g.box((x + 0.5, (B - 0.25 + B) / 2, 2.98), (0.05, 0.3, 0.03), M["steel"])
        g.torus((x + 0.5, B - 0.25, 3.1), 0.11, 0.009, M["steel"], rot=(0, 90, 0), n1=10, n2=4)
        if x % 4 == 0:
            g.cyl((x + 1.5, B - 0.25, 3.1), 0.135, 0.04, M["steel"], rot=(0, 90, 0), n=12)
    valve(g, M, (38.6, B - 0.25, 3.1), r=0.14)
    valve(g, M, (50.6, B - 0.25, 3.45), r=0.1, m=M["steel_mid"])
    gauge(g, M, (26.0, B - 0.25, 3.25))
    gauge(g, M, (44.6, B - 0.25, 3.25))
    for hx in (26.0, 50.0):                                            # risers down to fire hose reels
        g.cyl((hx, B - 0.25, 2.1), 0.05, 2.0, M["paint_red"], n=10)
        g.cyl((hx, B - 0.3, 1.2), 0.32, 0.12, M["paint_red"], rot=(90, 0, 0), n=20)
        g.torus((hx, B - 0.38, 1.2), 0.26, 0.04, M["rubber"], rot=(90, 0, 0), n1=18, n2=5)
        g.cyl((hx, B - 0.38, 1.2), 0.08, 0.1, M["steel_mid"], rot=(90, 0, 0), n=10)
        sign(g, M, "FIRE HOSE", (hx, yw, 1.85), 0.5, 0.14, dark_plate=True)
    # conduit and its boxes, drops to switches and sockets; the cable tray above the girt
    conduit(g, M, X0 + 0.5, X1 - 0.5, yw, 2.55, boxes=[x + 1.5 for x in cols])
    conduit(g, M, X0 + 0.5, X1 - 0.5, yw, 2.65, r=0.011, clip=0.9)
    for (x, kind, zb) in ((19.6, "switch", 1.35), (28.5, "socket", 0.6), (36.6, "isolator", 1.5), (40.6, "socket", 0.6), (55.5, "isolator", 1.5), (61.0, "switch", 1.35)):
        conduit_drop(g, M, x, yw, 2.55, zb, box=kind)
    cable_tray(g, M, X0 + 0.4, X1 - 0.4, B - 0.45, 4.55, wall_y=B - 0.05, cables=5)
    elec_board(g, M, 38.0, yw, 1.4, w=1.2, h=1.6, label="MCC 2  -  415 V")
    elec_board(g, M, 49.0, yw, 1.4, w=0.8, h=1.1)
    elec_board(g, M, 55.0, yw, 1.4, w=0.7, h=0.9, label="CRANE  ISOLATOR")
    # fire points, call points, signs
    for k, cx in enumerate(cols):
        if k % 2 == 0:
            extinguisher(g, M, cx + 0.55, yw)
        if k % 3 == 1:
            call_point(g, M, cx - 0.5, yw, 1.4)
    sign(g, M, "SPEED LIMIT", (27.5, yw, 3.9), 0.7, 0.42, sub="5 MPH", dark_plate=True)
    sign(g, M, "HI-VIS", (40.5, yw, 3.9), 0.6, 0.38, sub="BEYOND THIS POINT")
    sign(g, M, "DANGER", (59.0, yw, 3.9), 0.6, 0.38, sub="OVERHEAD CRANE", dark_plate=True)
    sign(g, M, "PEDESTRIANS", (16.0, yw, 3.9), 0.7, 0.38, sub="USE WALKWAY")
    sign(g, M, "GOODS IN", (21.0, yw, 3.0), 1.0, 0.3)
    # the catwalk along the back wall under the windows, ladders up at each end
    catwalk(g, M, X0 + 0.5, X1 - 0.5, B - 1.05, B - 0.12, 5.0, B - 0.05)
    ladder(g, M, X0 + 1.2, B - 1.05, 0.0, 5.0, facing=-1)
    ladder(g, M, X1 - 1.4, B - 1.05, 0.0, 5.0, facing=-1)

    # ---------------------------------------------------- the roof: purlins, ribs, gussets, ties, bracing
    for f in (0.3, 0.6, 0.9):
        for back in (False, True):
            ye = B if back else F
            y, z = ye + (RY - ye) * f, EAVE + (RIDGE - EAVE) * f + 0.02
            g.box(((X0 + X1) / 2, y, z), (X1 - X0, 0.08, 0.14), M["steel_mid"])
    x = X0 + 0.2
    while x < X1:
        if not any(a - 0.1 < x < b + 0.1 for a, b in H.SKYLIGHTS):
            for back in (False, True):
                ye = B + 0.35 if back else F - 0.35
                a = Vector((x, ye, EAVE + 0.12))
                b2 = Vector((x, RY, RIDGE + 0.12))
                g.beam(a[:], b2[:], 0.07, 0.04, M["iron"])                            # sheet ribs, underside
                g.beam((a + Vector((0, 0, 0.17)))[:], (b2 + Vector((0, 0, 0.17)))[:], 0.06, 0.04, M["iron_dark"])   # and on top
        x += 0.5
    for cx in cols:
        nodes = []
        for side in (0, 1):
            ya, yb = (F, RY) if side == 0 else (B, RY)
            for i in range(0, 6):
                f = i / 5
                nodes.append((ya + (yb - ya) * f, EAVE))
                nodes.append((ya + (yb - ya) * f, EAVE + (RIDGE - EAVE) * f - 0.05))
        gussets(g, M, cx, nodes, size=0.28)
    for y in (4.0, RY, 14.8):                                                         # bottom-chord ties
        g.box(((X0 + X1) / 2, y, EAVE - 0.02), (X1 - X0, 0.06, 0.06), M["steel"])
    for (xa, xb) in ((cols[0], cols[1]), (cols[-2], cols[-1])):                       # roof-plane bracing in the end bays
        for back in (False, True):
            ye = B if back else F
            pa = (xa, ye, EAVE + 0.1)
            pb = (xb, RY, RIDGE)
            g.beam(pa, pb, 0.03, 0.03, M["steel"])
            g.beam((xb, ye, EAVE + 0.1), (xa, RY, RIDGE), 0.03, 0.03, M["steel"])
    # more high-bay lamps over the back of the hall, hanging off the ties
    for lx in (20.0, 26.0, 32.0, 38.0, 44.0, 50.0, 56.0):
        H.lamp(g, M, lx, 9.4, 7.7, True, cold=(lx % 12 == 2))
        H.lamp(g, M, lx + 3.0, 15.0, 7.7, lx not in (32.0,))
    # the torn skylight: sheet torn back, a tarp hanging through, rain streaks under it
    tx0, tx1 = H.TORN
    for k in range(6):
        x = tx0 + 0.3 + k * (tx1 - tx0 - 0.6) / 5
        L = R.uniform(0.4, 1.0)
        g.box((x, 2.0 + R.uniform(-0.6, 0.6), 10.1 - L / 2), (0.25, 0.015, L), M["iron_dark"], rot=(R.uniform(-30, 30), R.uniform(-20, 20), R.uniform(-10, 10)))
    for k in range(5):
        a = k / 4
        g.box((tx0 + 0.9 + a * 0.2, 4.2 + a * 0.15, 10.3 - a * 1.6), (1.4 - a * 0.3, 0.02, 0.45), M["tarp"], rot=(70 - a * 50, 0, a * 8))
    # chains and hooks hanging from the trusses, behind the lane and clear of the spawns
    for (cx, cy, L) in ((28.6, 3.0, 3.2), (39.2, 6.5, 4.0), (51.2, 9.0, 3.6), (59.5, 7.0, 2.8), (18.2, 13.8, 3.0), (43.0, 12.0, 4.5)):
        chain_hook(S, g, M, (cx, cy, EAVE - 0.08), L)

    # ---------------------------------------------------- ZONE 1: the loading bay
    for dx in (18.0, 24.0):
        for s in (-1, 1):
            g.box((dx + s * 1.9, B - 0.08, 2.1), (0.12, 0.14, 4.2), M["steel"])                       # door tracks
            g.box((dx + s * 1.95, B - 0.15, 0.55), (0.12, 0.12, 1.1), M["k_hazard"])
        for k in range(4):                                                                             # vision panel
            g.box((dx - 1.2 + k * 0.8, B - 0.13, 2.3), (0.5, 0.012, 0.12), M["glass"])
        g.box((dx + 1.75, B - 0.2, 2.2), (0.012, 0.012, 3.8), M["steel_mid"])                        # pull chain
        g.box((dx, B - 0.9, 0.006), (3.2, 1.6, 0.012), M["checker"])                                   # dock leveller
        g.box((dx, B - 1.72, 0.008), (3.2, 0.1, 0.016), M["k_hazard"])
        bollard(g, M, dx - 2.1, B - 1.3)
        bollard(g, M, dx + 2.1, B - 1.3)
    pallet_jack(g, M, 22.6, 7.2, 70)
    pallet_stack(S, g, M, 15.2, 13.6, 14)
    pallet_stack(S, g, M, 15.2, 15.0, 9)
    pallet_stack(S, g, M, 26.0, 13.0, 11)
    tarped(g, M, (18.6, 11.4, 0.0), (2.4, 1.2, 1.6))
    for (lx, ly, kind) in ((19.0, 7.5, "wrap"), (20.3, 7.6, "cartons"), (17.4, 3.3, "drums"), (25.5, 6.8, "wrap"), (23.8, 10.0, "sacks")):
        if free(lx, ly):
            load_on_pallet(S, g, M, lx, ly, kind=kind)
    for (cx, cy) in ((23.0, 2.4), (23.4, 2.2), (17.0, 1.6)):
        cone(g, M, cx, cy)
    # the battery charger for the forklift on the west wall, its cable to the truck
    g.box((X0 + 0.45, 3.2, 1.3), (0.3, 0.7, 0.9), M["k_mid"])
    g.box((X0 + 0.29, 3.2, 1.45), (0.01, 0.4, 0.2), M["k_screen"])
    for k in range(10):
        a, b = k / 10, (k + 1) / 10
        g.beam((X0 + 0.6 + a * 4.6, 3.2 - a * 0.9, 0.04 + 0.8 * (1 - a) ** 3), (X0 + 0.6 + b * 4.6, 3.2 - b * 0.9, 0.04 + 0.8 * (1 - b) ** 3), 0.03, 0.03, M["rubber"])

    # ---------------------------------------------------- ZONE 2: the racking
    for ry in (4.0, 6.6, 9.8, 12.4, 15.6):
        for x in (26.6, 26.6 + 10.8 / 4, 26.6 + 10.8 / 2, 26.6 + 10.8 * 3 / 4, 37.4):
            g.box((x, ry - 0.08, 0.3), (0.16, 0.06, 0.6), M["k_hazard"])                               # upright guards
        for z in (2.2, 4.4, 6.6):
            for k in range(8):                                                                          # location labels
                lx = 26.9 + k * 1.33
                g.box((lx, ry - 0.035, z - 0.07), (0.16, 0.006, 0.07), M["k_white"])
                g.box((lx - 0.03, ry - 0.04, z - 0.07), (0.06, 0.004, 0.04), M["k_black"])
        text(g, "AISLE %s" % "CDEFG"[[4.0, 6.6, 9.8, 12.4, 15.6].index(ry)], (26.45, ry + 0.55, 3.0), 0.3, M["k_white"], facing=0)
    for (x0, x1) in H.RACKS:                                                                            # the lane racks: labels only
        for z in H.RACK_LEVELS:
            for k in range(3):
                lx = x0 + 0.6 + k * 1.3
                g.box((lx, -0.785, z - 0.07), (0.16, 0.006, 0.07), M["k_white"])
                g.box((lx - 0.03, -0.79, z - 0.07), (0.06, 0.004, 0.04), M["k_black"])
    # the monorail: end stops, the festoon of its cable on hangers
    for ex in (25.9, 38.1):
        g.box((ex, 0.0, 7.82), (0.1, 0.3, 0.2), M["paint_yellow"])
    for k in range(9):
        fx = 26.5 + k * 1.4
        g.box((fx, 0.22, 7.8), (0.05, 0.05, 0.12), M["steel"])
        if k < 8:
            for j in range(4):
                a, b = j / 4, (j + 1) / 4
                g.beam((fx + 1.4 * a, 0.22, 7.72 - 0.35 * math.sin(a * math.pi)), (fx + 1.4 * b, 0.22, 7.72 - 0.35 * math.sin(b * math.pi)), 0.02, 0.02, M["rubber"])

    # ---------------------------------------------------- ZONE 3: the mezzanine, the workshop, the office
    mx0, mx1 = H.MEZZ[0], H.MEZZ[1]
    x = mx0 + 0.6
    while x < mx1:
        g.box((x, (-0.85 + 3.4) / 2, mz - 0.2), (0.08, 4.2, 0.18), M["steel"])                           # deck joists
        x += 1.2
    g.box(((mx0 + mx1) / 2, -0.85, mz - 0.02), (mx1 - mx0, 0.02, 0.03), M["k_hazard"])                    # nosing
    sign(g, M, "MAX FLOOR LOAD", (43.0, -0.9, mz - 0.22), 0.7, 0.22, sub="5 KN / M2", dark_plate=True)
    foreman_office(g, M, mz)
    bench_kit(g, M, 46.5, 5.3)
    drill_press(g, M, 48.6, 5.2)
    welding_cart(g, M, 50.6, 5.6)
    lockers(g, M, 40.2, 6, 8.2)
    gas_bottles(g, M, 39.4, 6.0, 4)
    shelving(S, g, M, 40.0, 42.4, 10.0, 10.5, 2.4, levels=5, label="SPARES")
    shelving(S, g, M, 42.6, 45.0, 10.0, 10.5, 2.4, levels=5)
    jerrycan(g, M, 45.5, 7.0, 0.0, 20)
    jerrycan(g, M, 45.9, 7.1, 0.0, -10)

    # ---------------------------------------------------- ZONE 4: the crane bay
    gx = H.GANTRY_X
    for k in range(12):                                                                                  # girder stiffeners
        yy = F + 0.6 + k * (B - F - 1.2) / 11
        for s in (-1, 1):
            g.box((gx + s * 0.36, yy, 7.95), (0.02, 0.1, 0.65), M["crane"])
    for y in (F + 0.25, B - 0.3):
        for s in (-1, 1):
            g.cyl((gx + s * 0.9, y, 7.6), 0.18, 0.2, M["steel"], rot=(90, 0, 0), n=12)                    # end-truck wheels
        g.box((gx, y, 8.05), (2.4, 0.06, 0.04), M["k_hazard"])
    for k in range(8):                                                                                   # the bridge's festoon
        g.box((gx + 0.5, 19.6 - k * 0.5, 8.4), (0.04, 0.04, 0.1), M["steel"])
        g.beam((gx + 0.5, 19.6 - k * 0.5, 8.35), (gx + 0.5, 19.35 - k * 0.5, 8.0), 0.02, 0.02, M["rubber"])
    g.box((50.0 + (X1 - 50.0) / 2, B - 0.12, 8.3), (X1 - 50.0, 0.012, 0.012), M["steel_mid"])
    # steel stock: a cantilever rack of sections along the wall, plates, a skip of offcuts, a tarped machine
    for k in range(4):
        cx = 51.0 + k * 3.0
        g.box((cx, B - 0.6, 1.6), (0.2, 0.25, 3.2), M["steel"])
        g.box((cx, B - 0.6, 0.05), (0.3, 1.4, 0.1), M["steel"])
        for z in (0.6, 1.4, 2.2):
            g.beam((cx, B - 0.55, z), (cx, B - 1.6, z + 0.12), 0.08, 0.08, M["steel"])
    for z in (0.75, 1.55, 2.35):
        for j in range(4):
            g.box((55.5, B - 1.0 - j * 0.14, z + 0.06), (10.5, 0.1, 0.12), R.choice((M["steel_mid"], M["rust"], M["steel"])))
    for k in range(7):
        g.box((54.0 + R.uniform(-0.05, 0.05), 10.8, 0.05 + k * 0.06), (2.4, 1.2, 0.05), M["steel_mid"] if k % 2 else M["rust"], rot=(0, 0, R.uniform(-3, 3)))
    for (cx, cy) in ((52.0, 7.0), (53.0, 9.5), (60.0, 12.0)):                                            # coil cradles
        for s in (-1, 1):
            g.box((cx + s * 0.45, cy, 0.1), (0.25, 1.1, 0.2), M["wood_dark"])
    skip(g, M, 59.6, 16.0, 0)
    tarped(g, M, (55.4, 14.4, 0.0), (3.0, 1.8, 2.0))
    # the main door: hazard jambs, the EXIT sign lit over it, bollards outside, its frame
    ex = X1 + 0.02
    for yy in (H.EXIT[0], H.EXIT[1]):
        g.box((ex - 0.2, yy, 0.6), (0.28, 0.2, 1.2), M["k_hazard"])
    bbox(g, (X1 - 0.06, (H.EXIT[0] + H.EXIT[1]) / 2, H.EXIT[2] + 0.85), (0.08, 1.0, 0.32), M["k_exit"], w=0.01)
    text(g, "EXIT", (X1 - 0.11, (H.EXIT[0] + H.EXIT[1]) / 2, H.EXIT[2] + 0.85), 0.24, M["k_black"], facing=0)
    for (bx, by) in ((X1 + 0.9, H.EXIT[0] - 0.5), (X1 + 0.9, H.EXIT[1] + 0.5)):
        bollard(g, M, bx, by)
    g.box((X1 + 0.6, (H.EXIT[0] + H.EXIT[1]) / 2, H.EXIT[2] + 0.75), (1.2, H.EXIT[1] - H.EXIT[0] + 1.6, 0.1), M["steel"])   # canopy
    for k in range(4):
        g.box((X1 + 0.8, H.EXIT[0] - 0.5 + k * (H.EXIT[1] - H.EXIT[0] + 1.0) / 3, H.EXIT[2] + 0.62), (0.7, 0.3, 0.15), M["steel"])
    # the west end: an EXIT sign over the inner shutter, read from the hall
    bbox(g, (X0 + 0.42, 0.5, 4.75), (0.06, 0.9, 0.28), M["k_exit"], w=0.01)
    text(g, "STORE  7", (X0 + 0.46, 0.5, 4.75), 0.18, M["k_black"], facing=2)

    # ---------------------------------------------------- outside, the yard face
    yo = B + H.WALL_T
    for dx in (18.0, 24.0, 30.0):
        for s in (-1, 1):
            g.box((dx + s * 2.05, yo + 0.35, 2.3), (0.3, 0.7, 4.6), M["k_black"])                    # dock shelter pads
        g.box((dx, yo + 0.35, 4.75), (4.4, 0.7, 0.4), M["k_black"])
        for k in range(12):
            g.box((dx - 1.75 + k * 0.32, yo + 0.7, 4.2), (0.3, 0.02, 0.9), M["rubber"])               # curtain strips
        text(g, "%d" % (1 + [18.0, 24.0, 30.0].index(dx)), (dx, yo + 0.04, 5.6), 0.7, M["k_white"], facing=1)
        for s in (-1, 1):
            bollard(g, M, dx + s * 2.4, yo + 3.2)
    g.box((24.0, yo + 1.9, 5.9), (17.5, 3.8, 0.12), M["iron_dark"])                                 # the dock canopy
    for k in range(6):
        cx = 15.8 + k * 3.3
        g.beam((cx, yo + 0.05, 7.4), (cx, yo + 3.7, 5.95), 0.05, 0.05, M["steel"])                     # tie rods
        g.box((cx, yo + 1.9, 5.82), (0.1, 3.8, 0.12), M["steel"])
    for k in range(5):                                                                                # steps up to the dock
        h = (5 - k) * 0.24
        g.box((15.85 - k * 0.3, yo + 1.4, h / 2), (0.3, 1.2, h), M["concrete"])
    railing(g, M, (14.1, yo + 0.8), (16.0, yo + 0.8), 0.0, m=M["k_white"], kick=False)
    for (wx, wz) in ((21.0, 6.6), (27.0, 6.6), (33.0, 6.6), (44.0, 6.0), (52.0, 6.0), (60.0, 6.0)):
        cage_lamp(g, M, wx, yo + 0.02, wz, True, facing=1)
    streaks(g, M, X0, X1, EAVE + 0.05, yo + 0.004, 40, M["k_stain_wall"], length=(0.8, 3.5), width=(0.2, 0.7), z_min=0.3)
    for x in (X0 + 0.5, 38.0, X1 - 0.3):                                                              # downpipe brackets
        for z in (1.5, 3.5, 5.5, 7.5):
            g.box((x, yo + 0.1, z), (0.2, 0.15, 0.05), M["steel"])
    for vx in (20.0, 28.0, 36.0, 44.0, 52.0, 58.0):                                                   # ridge vents
        g.cyl((vx, RY, RIDGE + 0.6), 0.25, 0.4, M["steel_mid"], n=10)
        g.cyl((vx, RY, RIDGE + 1.0), 0.42, 0.5, M["steel_mid"], n=16, r2=0.3)
    g.box(((X0 + X1) / 2, RY, RIDGE + 0.42), (X1 - X0 + 0.7, 0.6, 0.1), M["steel_mid"])                # ridge cap
