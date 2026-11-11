"""Hanging from an edge (V4 body): lowering himself down, hanging, letting go, pulling back up,
swinging (to let go further out), dropping onto a man, pulling a man off by the ankle, losing his grip, and landing.

Space: the hanging frame. He faces +x (towards the platform he hangs from); the platform's edge is at
x = EDGE_X, its top at z = H_TOP. The game puts his root (the floor point under his feet) at the
edge, H_TOP below its top; the clips that start or end on the platform put him up there by the
hips' offset (lower_down starts standing on it, hang_climb ends standing on it)."""
from v4_pose import P

EDGE_X, H_TOP = 0.22, 1.6
GRIP = dict(hL=(EDGE_X - 0.03, H_TOP + 0.03), hR=(EDGE_X - 0.03, H_TOP + 0.02), fiL=110, fiR=110, yw_uaL=-8, yw_uaR=8)
HANG = P(hx=-0.04, hz=0.8, hips=180, spine=182, spine_mid=183, chest=184, neck=178, head=172,
         clL=(0, 30), clR=(0, 30), **GRIP,
         thL=4, shL=0, thR=-2, shR=-4, fpL=40, fpR=40, tpL=20, tpR=20)
TOP_X = EDGE_X + 0.26
STAND_TOP = P(hx=TOP_X, hz=H_TOP + 0.676, hips=180, spine=178, spine_mid=177, chest=176, neck=174, head=178,
              uaL=4, faL=12, uaR=-4, faR=6, fiL=55, fiR=55, fL=(TOP_X + 0.12, 0), fR=(TOP_X - 0.04, 0))


def _top(x, pitch=0.0):
    """A foot planted on the platform's top (the ankle given in the air form, at the top's height)."""
    import v4_pose as vp
    ax, az = vp.ankle_of(x, pitch)
    return (ax, az + H_TOP, pitch)


def clip(frames, keys, loop=False, hit=None, note="", shift=False, breath=False):
    return dict(frames=frames, keys=keys, hit=hit, load=None, loop=loop, aim=None, reach=None, react=None, note=note,
                striker=None, shift=shift, breath=breath, breaths=2, lag=True)


CLIPS = {}

# ---------------------------------------------------------------- lowering himself down off the edge
CLIPS["lower_down"] = clip(30, [
    (0, P(STAND_TOP, fL=_top(TOP_X + 0.12), fR=_top(TOP_X - 0.04)), "lin"),
    (5, P(STAND_TOP, hz=H_TOP + 0.42, spine=156, spine_mid=150, chest=146, neck=150, head=160, uaL=40, faL=40, uaR=30, faR=30,
          fL=_top(TOP_X + 0.14), fR=_top(TOP_X - 0.06, 30)), "smooth"),                                  # squats at the edge, looks down over the shoulder
    (9, P(STAND_TOP, hx=EDGE_X + 0.1, hz=H_TOP + 0.36, spine=150, spine_mid=144, chest=140, neck=146, head=156,
          hL=(EDGE_X + 0.04, H_TOP + 0.04), hR=(EDGE_X + 0.08, H_TOP + 0.04), fiL=110, fiR=110,
          fL=_top(TOP_X + 0.1), fR=(EDGE_X - 0.1, H_TOP - 0.1, -20)), "smooth"),                         # hands on the edge, one leg goes back over it
    (14, P(STAND_TOP, hx=EDGE_X - 0.08, hz=H_TOP - 0.05, spine=160, spine_mid=156, chest=152, neck=154, head=160,
           hL=(EDGE_X - 0.01, H_TOP + 0.03), hR=(EDGE_X - 0.01, H_TOP + 0.02), fiL=110, fiR=110,
           fL=(EDGE_X - 0.04, H_TOP - 0.36, 10), fR=(EDGE_X - 0.06, H_TOP - 0.5, 20)), "smooth"),        # both legs over, the belly on the edge
    (20, P(HANG, hz=1.08, spine=176, chest=174, neck=168, head=166, thL=30, shL=-20, thR=20, shR=-30), "smooth"),   # lowering on bent arms, toes on the wall
    (25, P(HANG, hz=0.82, hx=-0.06), "out"),                                                               # arms straight
    (30, HANG, "smooth"),                                                                                  # the swing settles
], note="lowering himself off the edge: squat, hands on the edge, a leg over, the other, down on bent arms, hang")

CLIPS["hang"] = clip(60, [
    (0, HANG, "smooth"),
    (15, P(HANG, hx=-0.07, thL=-6, thR=-10, head=168, tw_head=-14), "smooth"),                             # swinging gently, looking down
    (30, P(HANG, hx=-0.02, thL=8, thR=4, head=170, tw_head=6), "smooth"),
    (45, P(HANG, hx=-0.06, thL=0, thR=-6, head=164, tw_head=14), "smooth"),
    (60, HANG, "smooth"),
], loop=True, breath=True, note="hanging: a slow swing, the legs loose, looking down for where to land")

CLIPS["hang_swing"] = clip(36, [
    (0, HANG, "smooth"),
    (9, P(HANG, hx=-0.12, thL=-34, shL=-50, thR=-28, shR=-44, spine=188, chest=190, head=168), "smooth"),     # the legs swing back under the edge
    (18, P(HANG, hx=0.0, thL=0, shL=-8, thR=-4, shR=-10), "smooth"),
    (27, P(HANG, hx=-0.02, thL=40, shL=30, thR=34, shR=22, spine=172, chest=168, head=170), "smooth"),        # and out, the knees driving
    (36, HANG, "smooth"),
], loop=True, note="swinging from the edge: the legs pump back and out, to let go further out")

CLIPS["hang_climb"] = clip(26, [
    (0, HANG, "smooth"),
    (4, P(HANG, hz=0.76, thL=10, thR=6), "smooth"),                                                       # a small dip for the pull
    (9, P(HANG, hz=1.18, hx=-0.01, spine=172, chest=166, neck=164, head=170, uaL=150, faL=10, uaR=150, faR=10,
          hL=(EDGE_X - 0.03, H_TOP + 0.03), hR=(EDGE_X - 0.03, H_TOP + 0.02), thL=40, shL=-30, thR=20, shR=-50), "in"),   # pulled up, chest to the edge
    (14, P(HANG, hz=H_TOP + 0.12, hx=EDGE_X - 0.02, spine=150, spine_mid=140, chest=130, neck=136, head=156,
           hL=(EDGE_X + 0.16, H_TOP + 0.02), hR=(EDGE_X + 0.18, H_TOP + 0.02), fiL=20, fiR=20,
           thL=80, shL=-20, thR=-20, shR=-60), "smooth"),                                                  # pressing up over it, a knee comes onto the top
    (19, P(STAND_TOP, hx=EDGE_X + 0.16, hz=H_TOP + 0.42, spine=156, chest=146, neck=150, head=164, uaL=40, faL=50, uaR=30, faR=40,
           fL=_top(TOP_X + 0.12, 20), fR=_top(EDGE_X + 0.06, 40)), "smooth"),                             # crouched on the top
    (26, P(STAND_TOP, fL=_top(TOP_X + 0.12), fR=_top(TOP_X - 0.04)), "out"),                              # stands
], note="pulling back up: a dip, the pull, chest to the edge, a knee on top, stand")

CLIPS["hang_drop"] = clip(12, [
    (0, HANG, "lin"),
    (3, P(HANG, hz=0.74, hL=None, hR=None, uaL=170, faL=176, uaR=168, faR=172, fiL=40, fiR=40), "out"),    # the hands open
    (8, P(HANG, hz=0.7, hL=None, hR=None, uaL=120, faL=150, uaR=110, faR=140, fiL=60, fiR=60, thL=20, shL=-20, thR=10, shR=-30, head=164), "smooth"),
    (12, P(HANG, hz=0.68, hL=None, hR=None, uaL=60, faL=110, uaR=50, faR=100, fiL=80, fiR=80, thL=24, shL=-24, thR=14, shR=-34, head=162), "smooth"),   # arms coming down for balance, knees ready
], note="letting go: the hands open, the arms come down for balance, the knees get ready")

CLIPS["hang_fall"] = clip(12, [
    (0, HANG, "lin"),
    (2, P(HANG, hz=0.76, hL=None, hR=None, uaL=150, faL=120, uaR=110, faR=60, fiL=30, fiR=30, head=196, chest=190), "out3"),   # torn off: the arms fling
    (7, P(HANG, hz=0.72, hL=None, hR=None, uaL=100, faL=40, uaR=60, faR=10, thL=40, shL=0, thR=-20, shR=-40, head=190, chest=196, spine=192), "smooth"),
    (12, P(HANG, hz=0.7, hL=None, hR=None, uaL=80, faL=20, uaR=40, faR=0, thL=30, shL=-10, thR=-10, shR=-30, head=186, chest=192, spine=190), "smooth"),
], note="losing his grip: torn off the edge, the arms fling, flailing for balance")

CLIPS["drop_on"] = clip(12, [
    (0, HANG, "lin"),
    (3, P(HANG, hz=0.78, hL=None, hR=None, uaL=150, faL=170, uaR=140, faR=160, thL=80, shL=-10, thR=70, shR=-20, fpL=10, fpR=10), "out"),   # knees pulled up
    (8, P(HANG, hz=0.76, hL=None, hR=None, uaL=100, faL=130, uaR=90, faR=120, thL=20, shL=-4, thR=14, shR=-8, fpL=-10, fpR=-10, head=160), "in"),   # feet driven down at him
    (12, P(HANG, hz=0.74, hL=None, hR=None, uaL=90, faL=110, uaR=80, faR=100, thL=10, shL=0, thR=6, shR=-4, fpL=-10, fpR=-10, head=156), "smooth"),
], note="dropping on a man: let go, knees up, then both feet driven down onto him")

CLIPS["ankle_pull"] = clip(18, [
    (0, HANG, "smooth"),
    (4, P(HANG, hR=(EDGE_X + 0.2, H_TOP + 0.08), fiR=30, tw_chest=-14, head=186, neck=184), "in"),          # one hand leaves the edge and reaches up for the ankle
    (6, P(HANG, hR=(EDGE_X + 0.24, H_TOP + 0.06), fiR=140, tw_chest=-16, head=188), "in3"),                 # the grip
    (10, P(HANG, hR=(EDGE_X - 0.25, H_TOP - 0.45), fiR=140, tw_chest=10, hz=0.76, head=176, thL=20, thR=-10), "in3"),   # the yank: down and past him
    (14, P(HANG, hR=(EDGE_X - 0.1, H_TOP - 0.2), fiR=60, head=170), "out"),
    (18, HANG, "smooth"),
], hit=6, note="the ankle: one hand off the edge, round his ankle, and yanked down past him")

CLIPS["land_soft"] = clip(12, [
    (0, P(HANG, hz=0.7, hL=None, hR=None, uaL=60, faL=110, uaR=50, faR=100, fiL=80, fiR=80, thL=24, shL=-24, thR=14, shR=-34,
          head=162, hx=0.0), "lin"),
    (2, P(STAND_TOP, hx=0.0, hz=0.5, spine=164, chest=156, neck=156, head=166, uaL=40, faL=70, uaR=30, faR=60,
          fL=(0.12, 0), fR=(-0.06, 0)), "out3"),                                                         # the knees soak it
    (7, P(STAND_TOP, hx=0.0, hz=0.62, spine=174, chest=170, fL=(0.12, 0), fR=(-0.06, 0)), "out"),
    (12, P(STAND_TOP, hx=0.0, fL=(0.12, 0), fR=(-0.04, 0)), "smooth"),
], note="landing from a short drop: the knees soak it, up")

CLIPS["land_heavy"] = clip(26, [
    (0, P(HANG, hz=0.7, hL=None, hR=None, uaL=60, faL=110, uaR=50, faR=100, fiL=80, fiR=80, thL=24, shL=-24, thR=14, shR=-34,
          head=162, hx=0.0), "lin"),
    (3, P(STAND_TOP, hx=0.04, hz=0.36, spine=150, spine_mid=144, chest=138, neck=140, head=156, hR=(0.32, 0.06), fiR=20,
          uaL=40, faL=60, fL=(0.2, 0), fR=(-0.12, 50)), "out3"),                                          # a knee nearly down, a hand on the floor
    (10, P(STAND_TOP, hx=0.04, hz=0.38, spine=154, chest=146, neck=146, head=160, hR=(0.32, 0.06), fiR=20, uaL=40, faL=60,
           fL=(0.2, 0), fR=(-0.12, 50)), "smooth"),                                                      # a breath
    (18, P(STAND_TOP, hx=0.02, hz=0.6, spine=170, chest=164, head=170, uaR=20, faR=40, fL=(0.18, 0), fR=(-0.08, 20)), "out"),
    (26, P(STAND_TOP, hx=0.0, fL=(0.12, 0), fR=(-0.04, 0)), "smooth"),
], note="landing from high: down onto a knee and a hand, a breath, up")
