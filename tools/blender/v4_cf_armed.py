"""CROSSFIRE with a weapon in hand (docs/design/crossfire.md, "Armed"): the same two-thumb tap, one thumb each
side of him, each HIGH (his head) or LOW (his legs / body); with the pipe or the katana the four become the
weapon's own. Each hero clip comes with name_a_v (the man in front, +D) and name_b_v (the man behind, -D),
each man in his own frame (facing +x, the hero D in front of him).

THE PIPE (weight and momentum: the swing carries, both ends are weapons; everyone goes down)
  rod_cf_ends      HIGH + HIGH            BOTH ENDS: the butt end straight back into the face behind; the forehand round into the front man's head
  rod_cf_scythe    LOW + LOW              THE SCYTHE: low through the front man's shins, over the top, low through the other's; down on one
  rod_cf_windup    front HIGH, back LOW   THE WIND-UP: the backswing chops the knee behind as it winds; over the top into the front man's head
  rod_cf_shoulder  front LOW, back HIGH   OVER THE SHOULDER: a rap on the front man's shins; whipped back over the shoulder into the face behind; down on the buckled man

THE KATANA (one line, precision, death: iaido's answers to men behind; ordinary men are cut down)
  katana_cf_line    HIGH + HIGH            ONE LINE, TWO MEN: the level cut through the front man, the turn carries it through the other; they stand a beat, then fall
  katana_cf_kneel   LOW + LOW              THE KNEELING CIRCLE: down on one knee, through the front man's legs, turning on the knee, through the other's
  katana_cf_thrust  front HIGH, back LOW   THE THRUST BEHIND: the point reversed under the arm, straight back into him; out and up, kesa down the front man
  katana_cf_rise    front LOW, back HIGH   RISE AND FALL: rising from the hip through the front man; carried overhead as he turns; straight down onto the other"""
from v4_pose import P
from v4_clips import GUARD, G
from v4_clinch import LIE, PRONE, KNEEL
from v4_katana import grip, KG, K, FL, FR
from v4_rod import RG, R

CLIPS = {}
D = 0.6
GL, GR = GUARD["fL"], GUARD["fR"]


def cf(name, frames, hero, a, b, hit, note):
    CLIPS[name] = dict(frames=frames, hit=hit, load=None, loop=False, aim=None, reach=D, react=None, striker="R", shift=False,
                       breath=False, breaths=2, lag=True, victim=name + "_a_v", vat=D, note=note, keys=hero)
    for tag, keys, who in (("_a_v", a, "the man in front"), ("_b_v", b, "the man behind")):
        CLIPS[name + tag] = dict(frames=frames, hit=None, load=None, loop=False, aim=None, reach=None, react=None, striker=None,
                                 shift=True, breath=False, breaths=2, lag=True, note=note.split(":")[0] + ": " + who, keys=keys)


# ---------------------------------------------------------------- the men's side of it
WIND = G(uaR=10, faR=200, haR=200, tw_chest=-24, tw_hips=-14, fR=(GR[0], 30))            # winding up on him
LUNGE = G(hx=0.08, spine=168, chest=160, head=166, tw_hips=16, tw_chest=24, uaR=60, faR=110, fiR=170,
          fL=(GL[0] + 0.1, 0), fR=(GR[0], 60))                                            # coming in with it
KNEEL_L = P(KNEEL, hx=0.0, spine=160, spine_mid=150, chest=144, neck=142, head=150, hL=(0.24, 0.42), fiL=60, uaR=30, faR=70)


def come(f):
    """Winding up, then coming in at him by frame f."""
    return [(0, WIND, "smooth"), (f, LUNGE, "in")]


def head_snap(x=0.0, side=0):
    """Hit in the head: snapped back (side: turned round with it)."""
    return G(hx=x, spine=186, spine_mid=192, chest=198, neck=206, head=230, tw_head=side, uaL=110, faL=160, uaR=100, faR=150,
             fiL=30, fiR=30, fL=(GL[0], 0), fR=(GR[0] - 0.04, 30))


def fall_back(f, x0, dist, end):
    """From frame f: over backwards and flat on his back, dist further back."""
    return [
        (f + 5, P(LIE, hx=x0 - dist * 0.5, hz=0.3, hips=220, spine=228, chest=234, head=240, thL=60, shL=-20, thR=40, shR=-40), "in"),
        (f + 9, P(LIE, hx=x0 - dist, hz=0.074), "in3"),
        (f + 12, P(LIE, hx=x0 - dist, hz=0.11, chest=262, head=258), "out"),
        (end, P(LIE, hx=x0 - dist), "smooth"),
    ]


def legs_taken(x=0.0):
    """Both legs swept from under him: the feet fly up, the body goes back in the air."""
    return G(hx=x, hz=0.52, hips=206, spine=212, spine_mid=214, chest=218, neck=220, head=222, uaL=120, faL=150, uaR=110, faR=150,
             fiL=30, fiR=30, thL=60, shL=40, thR=52, shR=30, fpL=20, fpR=20)


def slammed_down(f, x, end, extra=None):
    """Off his feet onto his back, hard."""
    k = [
        (f + 4, P(LIE, hx=x, hz=0.22, hips=240, spine=246, chest=250, head=252, thL=80, shL=50, thR=70, shR=40), "in3"),
        (f + 7, P(LIE, hx=x, hz=0.074), "in3"),
        (f + 10, P(LIE, hx=x, hz=0.11, chest=262, head=258), "out"),
    ]
    if extra:
        k += extra
    k.append((end, P(LIE, hx=x), "smooth"))
    return k


def struck_stands(x=0.0):
    """The cut has gone through him: he stops dead, the hands falling open."""
    return G(hx=x, spine=176, chest=174, neck=170, head=168, uaL=30, faL=44, uaR=40, faR=56, fiL=30, fiR=30,
             fL=(GL[0] + 0.06, 0), fR=(GR[0], 20))


def cut_down(f_stand, x, end, back=False):
    """He stands a beat after the cut, the knees go, and he falls: on his face (or, back=True, his back)."""
    k = [
        (f_stand, G(hx=x, hz=0.62, spine=172, chest=168, neck=164, head=160, uaL=10, faL=14, uaR=8, faR=12, fiL=30, fiR=30,
                    fL=(GL[0] + 0.06, 0), fR=(GR[0], 10)), "smooth"),                                    # a beat, standing
        (f_stand + 6, P(KNEEL_L, hx=x, hz=0.4, head=148 if not back else 190, spine=158 if not back else 186,
                        uaL=0, faL=0, uaR=0, faR=0, hL=None), "in"),                                       # the knees go
    ]
    if back:
        k += [(f_stand + 14, P(LIE, hx=x - 0.5, hz=0.1), "in3"), (end, P(LIE, hx=x - 0.5), "smooth")]
    else:
        # a step back as the knees go, then down on his face (clear of the hero's feet)
        k[-1] = (k[-1][0], P(k[-1][1], hx=x - 0.22), k[-1][2])
        k += [(f_stand + 14, P(PRONE, hx=x - 0.55, hz=0.12), "in3"), (end, P(PRONE, hx=x - 0.55), "smooth")]
    return k


def fold_on_blade(x=0.0):
    return G(hx=x, hz=0.6, spine=150, spine_mid=142, chest=136, neck=138, head=146, hL=(0.18, 0.74), hR=(0.2, 0.72),
             fiL=120, fiR=120, fL=(GL[0] + 0.04, 0), fR=(GR[0], 30))


BUCKLE = G(hx=0.0, hz=0.46, spine=160, spine_mid=154, chest=148, neck=146, head=150,
           hL=(0.24, 0.48), fiL=60, uaR=50, faR=120, fiR=90, fL=(0.18, 0), fR=(-0.2, 10), tw_hips=14)


# ================================================================ THE PIPE
# ---- BOTH ENDS (HIGH + HIGH)
cf("rod_cf_ends", 44, [
    (0, RG, "lin"),
    (3, R(hz=0.61, tw_chest=-10, head=168, tw_head=50, gx=0.12, gz=1.0, a=100), "in"),                       # level, a look back
    (6, R(hx=-0.08, hz=0.6, spine=184, chest=186, tw_head=55, tw_chest=10, gx=-0.22, gz=1.02, a=92,
          fR=(FR[0] - 0.1, 20)), "in3"),                                                                      # HIT 1: the butt end, straight back into his face
    (9, R(hx=-0.04, hz=0.6, tw_hips=-46, tw_spine=-40, tw_chest=-62, spine=180, chest=180, head=168, tw_head=0,
          gx=-0.18, gz=1.06, a=246, fR=(FR[0] - 0.06, 10)), "out"),                                          # off it, up and back: the load
    (14, R(hx=0.14, hz=0.58, spine=166, chest=160, neck=158, head=166, tw_hips=34, tw_spine=30, tw_chest=50, gx=0.56, gz=1.0, a=95,
           fL=(FL[0] + 0.22, 0), fR=(FR[0] - 0.06, 76)), "in3"),                                              # HIT 2: the forehand into his head
    (18, R(hx=0.16, hz=0.59, spine=168, chest=164, tw_hips=48, tw_chest=82, gx=0.1, gz=1.12, a=196,
           fL=(FL[0] + 0.22, 0), fR=(FR[0] + 0.04, 0.06, 40)), "out"),                                       # round over the lead shoulder
    (28, R(hx=0.12, hz=0.6, tw_hips=24, tw_chest=40, gx=0.0, gz=1.1, a=202, fL=(FL[0] + 0.16, 0), fR=(FR[0] + 0.06, 30)), "out"),
    (44, RG, "smooth"),
], come(8) + [
    (14, head_snap(0.02, 40), "out3"),                                                                        # THE FOREHAND
    (18, G(hx=-0.18, hz=0.56, spine=184, chest=190, head=206, uaL=90, faL=130, uaR=80, faR=120, fiL=30, fiR=30,
           fL=(GL[0] - 0.1, 0.06, 20), fR=(GR[0] - 0.14, 20)), "out"),
] + fall_back(18, -0.18, 0.8, 44), [
    (0, WIND, "smooth"),
    (4, LUNGE, "in"),
    (6, head_snap(-0.02), "out3"),                                                                            # THE BUTT END in his face
    (12, G(hx=-0.16, spine=166, chest=158, head=150, hL=(0.16, 0.98), hR=(0.18, 0.96), fiL=60, fiR=60,
           fL=(GL[0] - 0.06, 0), fR=(GR[0] - 0.12, 30)), "out"),                                             # the hands to his face, bent over it
    (18, P(KNEEL_L, hx=-0.16, head=140, hL=(0.2, 0.62), hR=(0.18, 0.6)), "in"),                              # down on a knee
    (24, P(PRONE, hx=-0.46, hz=0.2, hips=120, spine=110, chest=104, head=104, thL=-40, shL=-90, thR=-40, shR=-90), "in"),
    (27, P(PRONE, hx=-0.5, hz=0.08), "in3"),                                                                 # on his face
    (44, P(PRONE, hx=-0.5), "smooth"),
], 6, "BOTH ENDS: the butt end straight back into the face behind, the forehand round into the front man's head")

# ---- THE SCYTHE (LOW + LOW)
cf("rod_cf_scythe", 52, [
    (0, RG, "lin"),
    (5, R(hz=0.55, spine=170, tw_chest=-50, tw_hips=-36, gx=-0.12, gz=0.8, a=252, fR=(FR[0] - 0.06, 20)), "out"),   # loaded low
    (9, R(hx=0.06, hz=0.46, spine=152, chest=144, neck=148, head=158, tw_chest=40, tw_hips=30, gx=0.44, gz=0.4, a=95,
          fL=(FL[0] + 0.14, 0), fR=(FR[0] - 0.04, 60)), "in3"),                                              # HIT 1: low through his shins
    (12, R(hx=0.04, hz=0.58, spine=176, chest=178, tw_chest=0, tw_hips=0, tw_head=50, gx=0.04, gz=1.24, a=182,
           fL=(FL[0] + 0.12, 0), fR=(FR[0] - 0.04, 40)), "out"),                                             # carried on, up over the top
    (16, R(hx=0.0, hz=0.46, spine=156, chest=148, neck=150, head=160, tw_chest=-60, tw_hips=-40, tw_head=60, gx=-0.3, gz=0.42, a=266,
           fL=(FL[0] + 0.12, 30), fR=(FR[0] - 0.06, 10)), "in3"),                                            # HIT 2: and down, low through the other's
    (20, R(hx=0.0, hz=0.5, spine=160, chest=156, tw_chest=-40, tw_head=40, gx=-0.24, gz=0.5, a=240, fL=(FL[0] + 0.12, 10)), "out"),
    (25, R(hx=0.04, hz=0.65, spine=182, chest=186, neck=172, head=170, tw_chest=-6, gx=0.0, gz=1.42, a=200,
           fL=(FL[0] + 0.14, 0), fR=(FR[0] - 0.06, 20)), "out"),                                             # round to the front one, up high
    (28, R(hx=0.04, hz=0.66, spine=184, chest=188, gx=-0.02, gz=1.45, a=208, fL=(FL[0] + 0.14, 0), fR=(FR[0] - 0.06, 24)), "smooth"),
    (31, R(hx=0.16, hz=0.52, spine=148, spine_mid=144, chest=138, neck=146, head=158, gx=0.48, gz=0.5, a=60,
           fL=(FL[0] + 0.24, 0), fR=(FR[0] - 0.06, 70)), "in3"),                                              # HIT 3: down on him, on the floor
    (34, R(hx=0.16, hz=0.5, spine=146, chest=136, gx=0.46, gz=0.42, a=44, fL=(FL[0] + 0.24, 0), fR=(FR[0] - 0.06, 66)), "out3"),
    (40, R(hx=0.14, hz=0.54, spine=156, chest=150, gx=0.4, gz=0.52, a=48, fL=(FL[0] + 0.22, 0), fR=(FR[0] - 0.04, 50)), "smooth"),
    (52, RG, "smooth"),
], come(6) + [
    (9, legs_taken(0.02), "out3"),                                                                            # LEGS GONE
] + slammed_down(9, -0.42, 52, extra=[
    (31, P(LIE, hx=-0.42, hz=0.05, chest=250, head=250, uaL=60, faL=80, uaR=50, faR=70, thL=110, shL=90), "in3"),   # PUT DOWN
    (35, P(LIE, hx=-0.42, hz=0.074), "out"),
]), come(10) + [
    (16, legs_taken(0.02), "out3"),
] + slammed_down(16, -0.42, 52), 9,
    "THE SCYTHE: low through the front man's shins, over the top, low through the other's; down on one")

# ---- THE WIND-UP (front HIGH, back LOW)
cf("rod_cf_windup", 46, [
    (0, RG, "lin"),
    (3, R(hz=0.6, tw_chest=-40, tw_hips=-30, head=168, tw_head=50, gx=-0.1, gz=0.9, a=250), "in"),
    (6, R(hx=-0.04, hz=0.54, spine=168, chest=164, tw_chest=-70, tw_hips=-46, tw_head=60, gx=-0.32, gz=0.42, a=278,
          fR=(FR[0] - 0.08, 10)), "in3"),                                                                     # HIT 1: the backswing, low into his knee
    (10, R(hz=0.64, spine=182, chest=184, tw_chest=-40, tw_head=10, gx=-0.06, gz=1.3, a=212), "out"),        # on up, over the shoulder
    (13, R(hx=0.14, hz=0.56, spine=156, chest=150, neck=152, head=160, tw_chest=30, tw_hips=20, gx=0.48, gz=1.0, a=116,
           fL=(FL[0] + 0.22, 0), fR=(FR[0] - 0.06, 70)), "in3"),                                              # HIT 2: over the top into his head
    (17, R(hx=0.14, hz=0.54, spine=152, chest=146, gx=0.42, gz=0.62, a=62, fL=(FL[0] + 0.22, 0), fR=(FR[0] - 0.06, 66)), "out"),
    (28, R(hx=0.1, hz=0.58, spine=160, chest=156, gx=0.36, gz=0.62, a=58, fL=(FL[0] + 0.18, 0), fR=(FR[0], 50)), "smooth"),
    (46, RG, "smooth"),
], come(8) + [
    (13, G(hx=0.08, hz=0.54, spine=150, spine_mid=140, chest=132, neck=126, head=120, uaL=40, faL=60, uaR=30, faR=50,
           fiL=30, fiR=30, fL=(GL[0] + 0.1, 0), fR=(GR[0], 40)), "out3"),                                     # ON THE HEAD: driven down
    (17, P(KNEEL_L, hx=0.08, head=124, spine=150, chest=136, uaL=10, faL=10, uaR=10, faR=10, hL=None), "in"),
    (22, P(PRONE, hx=-0.46, hz=0.18, hips=120, spine=110, chest=104, head=104, thL=-40, shL=-90, thR=-40, shR=-90), "in"),
    (25, P(PRONE, hx=-0.5, hz=0.08), "in3"),                                                                 # on his face
    (46, P(PRONE, hx=-0.5), "smooth"),
], [
    (0, WIND, "smooth"),
    (4, LUNGE, "in"),
    (6, P(BUCKLE, hz=0.5, head=156), "out3"),                                                                 # THE KNEE goes
    (10, P(KNEEL_L, head=146), "out"),
    (18, P(KNEEL_L, head=150, hL=(0.22, 0.4)), "smooth"),                                                     # clutching it
    (24, P(LIE, hx=-0.2, hz=0.2, hips=230, spine=236, chest=240, head=244, thL=90, shL=40, thR=60, shR=20), "in"),
    (28, P(LIE, hx=-0.3, hz=0.074, thL=110, shL=60), "in3"),                                                  # over onto his back, the knee up
    (46, P(LIE, hx=-0.3, thL=110, shL=60), "smooth"),
], 6, "THE WIND-UP: the backswing chops the knee behind as it winds; over the top into the front man's head")

# ---- OVER THE SHOULDER (front LOW, back HIGH)
cf("rod_cf_shoulder", 50, [
    (0, RG, "lin"),
    (3, R(hz=0.6, gx=0.1, gz=0.9, a=110), "in"),
    (6, R(hx=0.08, hz=0.5, spine=156, chest=148, neck=150, head=160, gx=0.42, gz=0.44, a=98,
          fL=(FL[0] + 0.14, 0), fR=(FR[0] - 0.04, 50)), "in3"),                                              # HIT 1: a rap on his shins
    (9, R(hx=0.04, hz=0.6, spine=176, chest=178, tw_head=50, gx=0.04, gz=1.2, a=190, fL=(FL[0] + 0.12, 0)), "out"),   # whipped up and back
    (12, R(hx=0.0, hz=0.6, spine=184, chest=188, head=166, tw_head=60, tw_chest=-20, gx=-0.12, gz=1.04, a=262,
           fL=(FL[0] + 0.12, 0), fR=(FR[0] - 0.06, 20)), "in3"),                                              # HIT 2: over the shoulder, into the face behind
    (16, R(hx=0.02, hz=0.65, spine=182, chest=186, neck=172, head=170, tw_head=0, gx=0.0, gz=1.42, a=200, fL=(FL[0] + 0.12, 0)), "out"),   # up high
    (19, R(hx=0.02, hz=0.66, spine=184, chest=188, gx=-0.02, gz=1.45, a=208, fL=(FL[0] + 0.12, 0)), "smooth"),
    (22, R(hx=0.12, hz=0.54, spine=150, spine_mid=146, chest=140, neck=146, head=158, gx=0.46, gz=0.74, a=80,
           fL=(FL[0] + 0.2, 0), fR=(FR[0] - 0.06, 70)), "in3"),                                               # HIT 3: down on the buckled man
    (25, R(hx=0.12, hz=0.5, spine=146, chest=136, gx=0.44, gz=0.5, a=50, fL=(FL[0] + 0.2, 0), fR=(FR[0] - 0.06, 66)), "out3"),
    (32, R(hx=0.1, hz=0.55, spine=156, chest=150, gx=0.4, gz=0.54, a=48, fL=(FL[0] + 0.18, 0), fR=(FR[0], 50)), "smooth"),
    (50, RG, "smooth"),
], [
    (0, WIND, "smooth"),
    (4, LUNGE, "in"),
    (6, P(BUCKLE, hz=0.5, head=156), "out3"),                                                                 # THE SHINS: he buckles
    (10, P(KNEEL_L, head=146), "out"),
    (19, P(KNEEL_L, head=166, spine=168, chest=160), "smooth"),                                               # looking up
    (22, P(KNEEL_L, hz=0.32, spine=126, spine_mid=116, chest=104, neck=98, head=92, uaL=10, faL=10, uaR=10, faR=10, hL=None), "out3"),   # DRIVEN DOWN
    (26, P(PRONE, hx=-0.5, hz=0.1), "in3"),
    (50, P(PRONE, hx=-0.5), "smooth"),
], come(8) + [
    (12, head_snap(0.0, -30), "out3"),                                                                        # OVER THE SHOULDER, in the face
    (16, G(hx=-0.16, hz=0.58, spine=186, chest=192, head=210, uaL=100, faL=140, uaR=90, faR=130, fiL=30, fiR=30,
           fL=(GL[0] - 0.08, 0.05, 20), fR=(GR[0] - 0.12, 20)), "out"),
] + fall_back(16, -0.16, 0.7, 50), 6,
    "OVER THE SHOULDER: a rap on the front man's shins, the pipe whipped back over the shoulder into the face behind, down on the buckled man")


# ================================================================ THE KATANA
KN = P(KNEEL, hx=0.04, hz=0.4, spine=168, spine_mid=164, chest=160, neck=158, head=166)        # the hero on one knee


def zan(fx, fz, fa, **kw):
    return K(hz=0.57, spine=162, chest=156, neck=156, head=164, gx=fx, gz=fz, a=fa, **kw)


# ---- ONE LINE, TWO MEN (HIGH + HIGH)
cf("katana_cf_line", 58, [
    (0, KG, "lin"),
    (4, K(hz=0.62, tw_chest=-60, tw_hips=-30, gx=-0.05, gz=0.95, a=270), "out3"),                           # coiled, the blade back level
    (9, K(hx=0.12, hz=0.59, spine=166, chest=160, tw_chest=34, tw_hips=20, gx=0.46, gz=0.98, a=90,
          fL=(FL[0] + 0.16, 0), fR=(FR[0], 60)), "in3"),                                                     # HIT 1: level, through the front man
    (12, K(hx=0.1, hz=0.6, spine=172, tw_hips=-80, tw_chest=-110, tw_head=-20, gx=0.06, gz=1.14, a=180,
           fL=(FL[0] + 0.14, 0), fR=(FR[0] + 0.02, 40)), "lin"),                                             # the turn carries it
    (15, K(hx=0.08, hz=0.59, spine=168, tw_hips=-150, tw_chest=-170, tw_head=-10, gx=-0.46, gz=0.98, a=270,
           fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "in3"),                                             # HIT 2: the same line, through the man behind
    (19, zan(-0.38, 0.9, 246, hx=0.08, tw_hips=-150, tw_chest=-166, fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "out"),
    (30, zan(-0.36, 0.86, 240, hx=0.08, tw_hips=-148, tw_chest=-164, fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "smooth"),   # zanshin: still, while they stand
    (38, K(hx=0.06, tw_hips=-20, tw_chest=-10, gx=0.42, gz=1.02, a=150, fL=(FL[0] + 0.1, 0)), "smooth"),
    (41, K(hx=0.06, gx=0.44, gz=0.78, a=58, tw_chest=10, fL=(FL[0] + 0.1, 0)), "in3"),                       # chiburi
    (58, KG, "smooth"),
], come(6) + [(9, struck_stands(0.08), "out3")] + cut_down(24, 0.08, 58),
   [(0, WIND, "smooth"), (10, LUNGE, "in"), (15, struck_stands(0.08), "out3")] + cut_down(30, 0.08, 58),
   9, "ONE LINE, TWO MEN: the level cut through the front man, the turn carries it through the other; they stand a beat, then fall")

# ---- THE KNEELING CIRCLE (LOW + LOW)
cf("katana_cf_kneel", 56, [
    (0, KG, "lin"),
    (4, P(KN, tw_chest=-40, tw_hips=-20, **grip(-0.06, 0.42, 270)), "in"),                                   # down on the knee, the blade back low
    (8, P(KN, tw_chest=40, tw_hips=20, **grip(0.44, 0.34, 92)), "in3"),                                       # HIT 1: through the front man's legs
    (11, P(KN, tw_hips=-80, tw_chest=-110, tw_head=-20, **grip(0.04, 0.7, 180)), "lin"),                      # turning on the knee
    (15, P(KN, tw_hips=-150, tw_chest=-170, tw_head=-10, **grip(-0.44, 0.34, 268)), "in3"),                   # HIT 2: through the other's
    (19, P(KN, tw_hips=-150, tw_chest=-166, **grip(-0.36, 0.28, 244)), "out"),
    (30, P(KN, tw_hips=-148, tw_chest=-164, **grip(-0.35, 0.27, 242)), "smooth"),                             # zanshin, low
    (37, K(hz=0.6, tw_hips=-10, gx=0.3, gz=0.84, a=135), "in"),                                              # up
    (40, K(gx=0.44, gz=0.78, a=58, tw_chest=10), "in3"),                                                      # chiburi
    (56, KG, "smooth"),
], come(5) + [
    (8, P(KNEEL_L, hx=0.04, head=150, hL=None, uaL=40, faL=60), "out3"),                                      # THE LEGS: down onto his knees
] + cut_down(20, 0.04, 56),
   [(0, WIND, "smooth"), (10, LUNGE, "in"), (15, P(KNEEL_L, hx=0.04, head=150, hL=None, uaL=40, faL=60), "out3")] + cut_down(26, 0.04, 56),
   8, "THE KNEELING CIRCLE: down on one knee, through the front man's legs, turning on the knee, through the other's")

# ---- THE THRUST BEHIND (front HIGH, back LOW)
cf("katana_cf_thrust", 50, [
    (0, KG, "lin"),
    (3, K(hz=0.61, tw_head=60, head=168, gx=0.02, gz=0.86, a=268), "in"),                                    # the blade reversed, the point turned back
    (7, K(hx=-0.06, hz=0.6, spine=170, tw_head=70, tw_chest=-20, gx=-0.14, gz=0.86, a=272, fR=(FR[0] - 0.08, 20)), "in3"),   # HIT 1: straight back into him
    (10, K(hx=-0.06, hz=0.6, spine=170, tw_head=70, tw_chest=-20, gx=-0.12, gz=0.86, a=272, fR=(FR[0] - 0.08, 20)), "smooth"),  # in him, a beat
    (12, K(hz=0.63, tw_head=10, gx=0.06, gz=1.26, a=212), "out3"),                                           # out, and up
    (15, K(hx=0.12, hz=0.59, spine=166, chest=160, neck=158, head=166, fL=(FL[0] + 0.16, 0), fR=(FR[0], 60),
           gx=0.5, gz=0.96, a=122), "in3"),                                                                  # HIT 2: kesa, down through the front man
    (18, K(hx=0.12, hz=0.585, fL=(FL[0] + 0.16, 0), fR=(FR[0], 56), gx=0.44, gz=0.7, a=62), "out"),
    (28, zan(0.4, 0.62, 44, hx=0.12, fL=(FL[0] + 0.16, 0), fR=(FR[0], 50)), "out3"),                          # zanshin
    (34, K(hx=0.1, gx=0.42, gz=1.02, a=150, fL=(FL[0] + 0.14, 0)), "smooth"),
    (37, K(hx=0.1, gx=0.44, gz=0.78, a=58, tw_chest=10, fL=(FL[0] + 0.14, 0)), "in3"),                       # chiburi
    (50, KG, "smooth"),
], come(10) + [(15, struck_stands(0.08), "out3")] + cut_down(28, 0.08, 50),
   [(0, WIND, "smooth"), (4, LUNGE, "in"), (7, fold_on_blade(0.04), "out3"), (10, fold_on_blade(0.05), "smooth"),
    (13, P(fold_on_blade(-0.04), hz=0.56), "out")] + cut_down(18, -0.04, 50),
   7, "THE THRUST BEHIND: the point reversed under the arm, straight back into the man behind; out and up, kesa down the front man")

# ---- RISE AND FALL (front LOW, back HIGH)
cf("katana_cf_rise", 52, [
    (0, KG, "lin"),
    (4, K(hz=0.6, tw_chest=-26, gx=0.1, gz=0.62, a=330), "out3"),                                            # low at the hip
    (8, K(hx=0.12, hz=0.6, spine=172, chest=170, fL=(FL[0] + 0.16, 0), fR=(FR[0], 60), gx=0.42, gz=1.08, a=160), "in3"),   # HIT 1: rising through him
    (11, K(hx=0.1, hz=0.64, spine=182, chest=186, tw_hips=-100, tw_chest=-130, tw_head=-20, gx=0.0, gz=1.42, a=200,
           fL=(FL[0] + 0.14, 0), fR=(FR[0] + 0.02, 40)), "out"),                                             # carried overhead as he turns
    (13, K(hx=0.08, hz=0.65, spine=184, chest=188, tw_hips=-150, tw_chest=-170, gx=-0.06, gz=1.44, a=196,
           fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "smooth"),                                          # held, at the top
    (16, K(hx=0.06, hz=0.57, spine=164, chest=158, tw_hips=-155, tw_chest=-172, gx=-0.42, gz=1.0, a=292,
           fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "in3"),                                             # HIT 2: straight down onto the other
    (19, zan(-0.36, 0.72, 318, hx=0.06, tw_hips=-155, tw_chest=-170, fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "out"),
    (30, zan(-0.35, 0.7, 320, hx=0.06, tw_hips=-152, tw_chest=-168, fL=(FL[0] + 0.12, 0), fR=(FR[0] + 0.06, 40)), "smooth"),
    (38, K(hx=0.06, tw_hips=-20, tw_chest=-10, gx=0.42, gz=1.02, a=150, fL=(FL[0] + 0.1, 0)), "smooth"),
    (41, K(hx=0.06, gx=0.44, gz=0.78, a=58, tw_chest=10, fL=(FL[0] + 0.1, 0)), "in3"),                       # chiburi
    (52, KG, "smooth"),
], come(4) + [
    (8, G(hx=0.06, spine=184, spine_mid=190, chest=196, neck=200, head=206, uaL=60, faL=80, uaR=50, faR=70, fiL=30, fiR=30,
          fL=(GL[0] + 0.06, 0), fR=(GR[0], 20)), "out3"),                                                    # THE RISING LINE: lifted by it
] + cut_down(22, 0.06, 52, back=True),
   [(0, WIND, "smooth"), (10, LUNGE, "in"), (16, struck_stands(0.08), "out3")] + cut_down(30, 0.08, 52),
   8, "RISE AND FALL: rising from the hip through the front man; carried overhead as he turns; straight down onto the other")
