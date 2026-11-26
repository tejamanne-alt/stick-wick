"""Moves for when the men are FAR, for the V4 body (docs/design/combat_v3.md, "far range"):

  advance         tap at a man 3–6 m off: he comes in behind his guard (a boxer's step-drag, never
                  crossing his feet), then strikes when in reach
  rush_in         (old) two explosive step-drags, the chin down
  rush_burst      swipe towards a man 3–8 m off: the blitz, looped explosive bounds (the game carries him)
  rush_strike     the blitz lands: a cross with the whole run behind it
  flying_knee     running, tap high: off one foot, the knee driven up into his face, hands pulling
                  his head down onto it
  slide_tackle    running, tap low: a baseball slide, the lead leg through his ankles
  shoulder_barge  running flat out into him: dip, drive the lead shoulder through his chest
  throw_object    tap a man with a bottle / wrench / brick in the hand: an overhand throw from
                  the legs up (step, hips, chest, the arm last, the wrist snapping through)
  sidestep        swipe away as a charge arrives: the matador's pivot off the line, and a hammer
                  on the back of his head as he goes past
  swat            tap towards something thrown at him: the lead forearm sweeps it aside
  chain_lash      a chain in the hand, tap: wound back over the shoulder, whipped through
  chain_drag      hold on the man: the chain round him, hauled in hand over hand into the clinch
  takedown_choke  crouched, from behind, tap: the rear naked choke, sat back, laid down
  unaware         a man on watch who hasn't seen him: bored, a cigarette, looking about

Clip frame and pose language: v4_pose.py. Feet and hands in shift clips are where they are in
the clip's own space (the hips move away from them), so a planted foot stays planted."""
from v4_pose import P
import v4_pose as vp
from v4_clips import GUARD, G
from v4_moves import CLIPS as MOVES, LOOSE, CROUCH, clip
from v4_clinch import HOLD, HELD, LIE, PRONE, KNEEL, pair

CLIPS = {}
RUN0 = vp.solve(MOVES["run"]["keys"][4][1])
SPRINT0 = vp.solve(MOVES["sprint"]["keys"][3][1])


def at(pose, dx, **kw):
    """The pose moved dx forward: the hips, the planted / flying feet and the hand targets with it."""
    p = dict(pose)
    p["hx"] = p.get("hx", 0.0) + dx
    for k in ("fL", "fR", "hL", "hR"):
        v = p.get(k)
        if v is not None:
            p[k] = (v[0] + dx,) + tuple(v[1:])
    return P(p, **kw)


GL, GR = GUARD["fL"], GUARD["fR"]

# ---------------------------------------------------------------- the advance (a looped step-drag)
# The lead foot steps (a low, quick slide of a step), the rear foot drags up after it on its ball,
# the guard never drops; the hips dip as the weight lands on the lead and rise as the rear arrives;
# the fists draw small circles, the head bobs off the line once per cycle. 0.42 m a cycle, 18
# frames: 0.7 m/s of ground, the speed the game moves him at (meta speed).
STEP = 0.21


def _advance():
    k = []
    # in the cycle's own space: start on the guard, end on the guard STEP*2 further on, then the
    # clip is played as a shift-free loop by subtracting the travel (the game moves the root)
    seq = [
        (0, G(), "lin"),
        (3, G(hx=0.03, fL=(GL[0] + 0.12, 0.05, -8), hz=0.635, head=170, uaL=64, faL=148), "out"),                  # the lead foot slides out
        (5, G(hx=0.08, fL=(GL[0] + STEP, 0), hz=0.628, chest=164, head=169, fR=(GR[0], 46)), "in"),             # lands, the weight onto it
        (7, G(hx=0.12, fL=(GL[0] + STEP, 0), fR=(GR[0] + 0.1, 0.04, 30), hz=0.632, head=171, tw_head=-4), "out"),  # the rear foot drags up on its ball
        (9, G(hx=STEP, fL=(GL[0] + STEP, 0), fR=(GR[0] + STEP, 40), hz=0.64, head=172, tw_head=0), "smooth"),
        (12, G(hx=STEP + 0.03, fL=(GL[0] + STEP + 0.12, 0.05, -8), fR=(GR[0] + STEP, 42), hz=0.636, uaR=24, faR=164, head=171, tw_head=4), "out"),
        (14, G(hx=STEP + 0.08, fL=(GL[0] + 2 * STEP, 0), fR=(GR[0] + STEP, 48), hz=0.627, chest=164, head=169), "in"),
        (16, G(hx=STEP + 0.12, fL=(GL[0] + 2 * STEP, 0), fR=(GR[0] + STEP + 0.1, 0.04, 30), hz=0.632), "out"),
        (18, at(GUARD, 2 * STEP), "smooth"),
    ]
    # to loop in place: subtract the travel, frame-proportionally, from every x (the root carries it)
    n = 18
    for f, p, e in seq:
        k.append((f, at(p, -2 * STEP * f / n), e))
    return k


CLIPS["advance"] = clip(18, _advance(), loop=True, speed=round(2 * STEP * 30 / 18, 3),
                        note="closing in behind the guard: step-drag, step-drag, the guard never drops")

# ---------------------------------------------------------------- the retreat (the advance's mirror, a looped back-step)
# Backing off behind the guard, still facing the man: the REAR foot reaches back first (low, on its
# ball), the weight settles back onto it, then the lead foot draws back after it, heel-light; the feet
# never cross or come together, the hips sink a touch as the weight lands, the chin stays down, the
# guard never drops (the fists ride up a little as he gives ground). 0.42 m a cycle, like the advance.
def _retreat():
    k = []
    S = -STEP
    seq = [
        (0, G(), "lin"),
        (3, G(hx=-0.03, fR=(GR[0] - 0.12, 0.05, 20), hz=0.635, head=170, uaL=62, faL=150), "out"),                # the rear foot slides back
        (5, G(hx=-0.08, fR=(GR[0] + S, 40), hz=0.628, chest=165, head=169, fL=(GL[0], 0)), "in"),                # lands on its ball, the weight back onto it
        (7, G(hx=-0.12, fR=(GR[0] + S, 40), fL=(GL[0] - 0.1, 0.035, -4), hz=0.632, head=171, tw_head=3), "out"),   # the lead draws back, heel-light
        (9, G(hx=S, fR=(GR[0] + S, 40), fL=(GL[0] + S, 0), hz=0.64, head=172, tw_head=0), "smooth"),
        (12, G(hx=S - 0.03, fR=(GR[0] + S - 0.12, 0.05, 20), fL=(GL[0] + S, 0), hz=0.636, uaR=24, faR=164, head=171, tw_head=-3), "out"),
        (14, G(hx=S - 0.08, fR=(GR[0] + 2 * S, 40), fL=(GL[0] + S, 0), hz=0.627, chest=165, head=169), "in"),
        (16, G(hx=S - 0.12, fR=(GR[0] + 2 * S, 40), fL=(GL[0] + S - 0.1, 0.035, -4), hz=0.632), "out"),
        (18, at(GUARD, 2 * S), "smooth"),
    ]
    n = 18
    for f, p, e in seq:
        k.append((f, at(p, -2 * S * f / n), e))       # loop in place: the game carries the root
    return k


CLIPS["retreat"] = clip(18, _retreat(), loop=True, speed=round(2 * STEP * 30 / 18, 3),
                        note="backing off behind the guard: the rear foot back, the lead draws after it, still facing him")

# ---------------------------------------------------------------- moving on guard (the hero's footwork near a man)
# Reviewed: the step-drag loops above, played at twice their speed, were seven twitchy shuffles a second,
# the hips lurching 12 cm inside every step (backing off it read as being knocked back), and the fists
# frozen at the face. A fighter moving on a man does two different things:
#
#   THE STALK (towards him): he walks his man down. A real walk, shortened and low (knees bent, no bounce,
#   the head level), the chest leading a touch, chin down; the guard held on the line: the lead fist out
#   at chin height a forearm in front, the rear at the jaw, elbows in; the shoulders turn a little with the
#   stride but the fists stay on him; every second step the lead hand paws out a few centimetres (feeling
#   for the range). 1.0 m/s.
#
#   GIVING GROUND (away, still facing him): the back-step. The body GLIDES back at an even speed (the
#   hips never lurch), only the feet work: the rear foot reaches back low and short on its ball, the lead
#   draws back after it the same distance, heel-light; the stance never splays; the lead hand a little
#   further out to keep him off. 0.75 m/s.
import math as _m
from v4_clips import GUARD as _G
from v4_moves import gait as _gait

STALK_SPEED = 1.0
_GARM = dict((k, _G[k]) for k in ("clL", "clR", "uaL", "faL", "haL", "yw_uaL", "yw_faL", "twL", "fiL",
                                  "uaR", "faR", "haR", "yw_uaR", "yw_faR", "twR", "fiR") if k in _G)


def _guard_arms(lead_out=0.0, paw=0.0, sway=0.0):
    """The guard's arms, the lead fist `lead_out` further from the face (0..1), pawing out by `paw` (0..1)."""
    a = dict(_GARM)
    lerp = lambda x, y, k: x + (y - x) * k
    # the lead fist out in front at chin height (a forearm ahead of the face), not jammed against it
    a["uaL"] = lerp(_G["uaL"], 44, lead_out) + 10 * paw
    a["faL"] = lerp(_G["faL"], 104, lead_out) - 12 * paw
    a["haL"] = a["faL"] + 6
    a["twL"] = -40
    # the rear fist at the jaw, the elbow in over the ribs
    a["uaR"] = 20 + 2 * sway
    a["faR"] = 158
    a["haR"] = 164
    return a


def _stalk():
    n = 24
    keys = []
    for f, p, e in _gait(n, STALK_SPEED, duty=0.62, lift=0.04, hz=0.64, bounce=0.005, bounce_low_at_mid=False,
                         spine=173, chest=165, neck=158, head=171, arm_base=0, arm_amp=0, elbow=0, fist=165,
                         twist=6, strike=-10, push=30):
        ph = f / n
        # the paw: on the second step (the lead foot landing), out and back
        paw = max(0.0, _m.sin(_m.pi * (ph - 0.5) / 0.3)) if 0.5 <= ph <= 0.8 else 0.0
        sway = _m.sin(2 * _m.pi * ph)
        q = P(p, tw_hips=p["tw_hips"] - 8, tw_spine=p["tw_spine"] - 6, spine_mid=169, tw_chest=p["tw_chest"] * 0.5 - 6,
              tw_head=-p["tw_chest"] * 0.4, **_guard_arms(0.85, paw, sway))
        keys.append((f, q, e))
    return keys


CLIPS["guard_stalk"] = clip(24, _stalk(), loop=True, speed=STALK_SPEED,
                            note="walking his man down: a low real walk behind the guard, head level, the lead hand pawing for range")

GIVE_STEP = 0.15


def _give_ground():
    n, S = 14, -GIVE_STEP
    GL0, GR0 = _G["fL"][0], _G["fR"][0]
    keys = []

    def move(f, a, b):
        return min(1.0, max(0.0, (f - a) / (b - a)))

    for f in range(n + 1):
        # the rear foot: back over frames 1-4 and 9-12; the lead after it: 5-8 and 13-16
        kr1, kr2 = move(f, 0.5, 3.5), move(f, 7.5, 10.5)      # the lead follows as the rear lands: one flowing step
        kl1, kl2 = move(f, 3.0, 6.5), move(f, 10.0, 13.5)
        sm = lambda k: k * k * (3 - 2 * k)
        rx = GR0 + S * (sm(kr1) + sm(kr2))
        lx = GL0 + S * (sm(kl1) + sm(kl2))
        r_air = (0 < kr1 < 1) or (0 < kr2 < 1)
        l_air = (0 < kl1 < 1) or (0 < kl2 < 1)
        kr = kr1 if 0 < kr1 < 1 else kr2
        kl = kl1 if 0 < kl1 < 1 else kl2
        fR = (rx, 0.022 * _m.sin(_m.pi * kr), 26) if r_air else (rx, 40)
        fL = (lx, 0.016 * _m.sin(_m.pi * kl), -3) if l_air else (lx, 0)
        travel = 2 * S * f / n                                     # the hips glide back evenly
        step = (f % 7) / 7.0
        hx = travel - 0.006 * _m.sin(_m.pi * step)                # (a few millimetres of give as the rear foot takes him)
        hz = 0.628 - 0.003 * _m.sin(_m.pi * step)                 # knees kept bent: never a straight leg reaching back
        p = P(_G, hx=hx, hz=hz, fL=fL, fR=fR, head=171, tw_head=0.0, **_guard_arms(1.0, 0.0, 0.0))
        keys.append((f, at(p, -travel), "lin"))                    # in place: the game carries the root
    return keys


CLIPS["guard_back"] = clip(14, _give_ground(), loop=True, speed=round(2 * GIVE_STEP * 30 / 14, 3),
                           note="giving ground: the body glides back, the rear foot back low, the lead after it; the lead hand out")

# ---------------------------------------------------------------- the rush in (two explosive step-drags)
CLIPS["rush_in"] = clip(18, [
    (0, GUARD, "lin"),
    (2, G(hx=-0.02, hz=0.615, spine=172, chest=160, neck=152, head=166, fR=(GR[0], 56)), "in"),          # the load: sink, the rear heel up
    (5, G(hx=0.3, hz=0.6, spine=168, chest=158, neck=150, head=164, fL=(GL[0] + 0.48, 0.07, -12), fR=(GR[0], 70),
          uaL=66, faL=152, uaR=26, faR=168, clL=(20, 14), clR=(10, 12)), "out3"),                            # the rear leg fires him forward
    (7, G(hx=0.44, hz=0.61, spine=170, chest=160, neck=154, head=166, fL=(GL[0] + 0.56, 0), fR=(GR[0] + 0.12, 0.05, 30)), "in"),
    (9, G(hx=0.56, hz=0.625, fL=(GL[0] + 0.56, 0), fR=(GR[0] + 0.56, 44), head=168), "out"),                    # the rear arrives: back on guard
    (12, G(hx=0.86, hz=0.6, spine=168, chest=158, neck=150, head=164, fL=(GL[0] + 1.04, 0.07, -12), fR=(GR[0] + 0.56, 70),
           clL=(20, 14), clR=(10, 12)), "out3"),                                                              # and again
    (14, G(hx=1.0, hz=0.612, fL=(GL[0] + 1.1, 0), fR=(GR[0] + 0.7, 0.05, 30), head=167), "in"),
    (18, at(GUARD, 1.1), "smooth"),
], shift=True, note="the rush: two explosive step-drags, chin down behind the shoulder, landing on guard")

# ---------------------------------------------------------------- the rush, v2 (the blitz)
# One unbroken burst instead of hops: a looped pair of explosive bounds (the rear leg fires him, both
# feet leave the floor for a frame, the lead lands heel-light and the rear snaps in behind it, never
# crossing), chin buried behind the lead shoulder, the guard tight and riding the bounds; the game
# carries the root at the clip's speed until he is a stride out, then the momentum goes straight into
# rush_strike. 10 frames, two bounds of 0.8 m: 4.8 m/s.
BOUND = 0.8
RUSH_SPEED = round(2 * BOUND * 30 / 10, 3)
_RB = dict(hz=0.6, spine=168, spine_mid=164, chest=156, neck=148, head=162, tw_chest=-10,
           uaL=70, faL=140, haL=150, uaR=26, faR=164, haR=170, clL=(18, 12), clR=(6, 8))


def _bound(x0):
    """One bound starting on the crouched guard at x0 (clip space), as (frame offset, pose, ease)."""
    return [
        (1, G(**dict(_RB, hx=x0 + 0.18, hz=0.615, spine=166, fL=(GL[0] + x0 + 0.22, 0.05, -12), fR=(GR[0] + x0, 72),
                     uaL=74, faL=136, clL=(22, 14))), "out3"),                                     # the rear leg fires
        (2, G(**dict(_RB, hx=x0 + 0.4, hz=0.635, fL=(GL[0] + x0 + 0.6, 0.06, -10), fR=(GR[0] + x0 + 0.2, 0.09, 46),
                     head=160, tw_head=-3)), "lin"),                                                # both feet off: the skip
        (3, G(**dict(_RB, hx=x0 + 0.6, hz=0.592, fL=(GL[0] + x0 + BOUND, 0), fR=(GR[0] + x0 + 0.5, 0.06, 40),
                     chest=154, head=161)), "in"),                                                  # the lead lands, the knee gives
        (5, G(**dict(_RB, hx=x0 + BOUND, fL=(GL[0] + x0 + BOUND, 0), fR=(GR[0] + x0 + BOUND, 64), tw_head=0)), "smooth"),  # the rear snaps in
    ]


def _rush_burst():
    seq = [(0, G(**dict(_RB, hx=0.0, fL=(GL[0], 0), fR=(GR[0], 64))), "smooth")]
    for f, p, e in _bound(0.0):
        seq.append((f, p, e))
    for f, p, e in _bound(BOUND):
        seq.append((f + 5, p, e))
    # in place: the root carries the travel (as the advance)
    return [(f, at(p, -2 * BOUND * f / 10), e) for f, p, e in seq]


CLIPS["rush_burst"] = clip(10, _rush_burst(), loop=True, speed=RUSH_SPEED,
                           note="the blitz: explosive bounds behind the guard, chin down, never crossing the feet")

# the blitz lands: the last bound becomes the punch. The rear foot fires one more time, the lead lands
# as the hips whip round, and the cross arrives with the whole run behind it; the rear foot slides
# through on its ball, then he settles on guard where the momentum left him (0.5 m on).
CLIPS["rush_strike"] = clip(18, [
    (0, G(**dict(_RB, hx=0.0, fL=(GL[0], 0), fR=(GR[0], 64))), "lin"),
    (2, G(**dict(_RB, hx=0.2, hz=0.61, fL=(GL[0] + 0.3, 0.05, -12), fR=(GR[0], 74), tw_hips=-20, tw_spine=-16,
                 tw_chest=-18, uaR=18, faR=168, clR=(-4, 6))), "out3"),                               # the last push, the shoulders wound
    (4, G(**dict(_RB, hx=0.38, hz=0.6, fL=(GL[0] + 0.48, 0), fR=(GR[0] + 0.12, 0.05, 40), tw_hips=20, tw_spine=14,
                 tw_chest=6, uaR=58, faR=128, haR=128, twR=50, clR=(24, 10), uaL=46, faL=160, haL=166)), "in"),   # lead lands, hips turning
    (5, G(**dict(_RB, hR=(1.02, 1.06), hx=0.46, hz=0.598, tw_hips=34, tw_spine=34, tw_spine_mid=36, tw_chest=42, chest=148,
                 spine_mid=162, neck=146, head=162, fL=(GL[0] + 0.5, 0), fR=(GR[0] + 0.24, 76), twR=94, clR=(58, 16),
                 uaL=38, faL=167, haL=173)), "in3"),                                                   # HIT: the run behind the fist
    (7, G(**dict(_RB, hR=(1.04, 1.055), hx=0.48, hz=0.6, tw_hips=35, tw_spine=35, tw_spine_mid=37, tw_chest=43, chest=148,
                 neck=146, head=162, fL=(GL[0] + 0.5, 0), fR=(GR[0] + 0.32, 76), twR=95, clR=(60, 16),
                 uaL=38, faL=167, haL=173)), "lin"),                                                   # through him, the rear foot sliding in
    (10, G(hx=0.5, hz=0.615, uaR=60, faR=130, haR=132, twR=56, clR=(28, 10), tw_hips=20, tw_chest=12, chest=156,
           fL=(GL[0] + 0.5, 0), fR=(GR[0] + 0.42, 60)), "out"),
    (14, G(hx=0.5, uaR=26, faR=162, haR=166, twR=24, tw_chest=-4, fL=(GL[0] + 0.5, 0), fR=(GR[0] + 0.5, 44)), "out"),
    (18, at(GUARD, 0.5), "smooth"),
], shift=True, hit=5, reach=1.02, striker="R", react="stumble_back",
    note="the blitz lands: the last bound becomes a cross with the whole run behind it")

# ---------------------------------------------------------------- the spear and the head scissors (paired, from range)
# Both are taken from a step or two away: the game slides the hero's root in to the clinch gap (0.62) by
# the moment of contact (GrappleMove.closeAt), the bodies keyed as a pair in the clinch's space (v4_clinch:
# the hero at 0 facing +x, the man GAP in front facing him, his clip in his own frame).
TORSO = ("hips", "spine", "spine_mid", "chest", "neck", "head")


def turned(pose, deg):
    """The same pose with the whole torso chain wound on by deg (a full flip keeps its numbers climbing)."""
    q = dict(pose)
    for k in TORSO:
        if k in q:
            q[k] = q[k] + deg
    return q


GAP = 0.62
_GUARD_V = G()                       # the man, on guard, before it starts

# SPEAR (swipe flat at a man 1.5-3 m off): the level change, the shoulder through his waist, arms round
# his thighs, the legs driving him off his feet, and both come down, the hero on top of him.
CLIPS.update(pair("spear", 30, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.02, hz=0.44, spine=142, spine_mid=136, chest=128, neck=136, head=158, tw_chest=0,
          fL=(GL[0] + 0.24, 0), fR=(GR[0] - 0.2, 66), uaL=70, faL=96, uaR=60, faR=90, clL=(10, 6), clR=(10, 6)), "in"),      # the level change: under his hands
    (6, G(hx=0.16, hz=0.47, spine=122, spine_mid=118, chest=112, neck=126, head=150, tw_chest=0,
          hL=(0.78, 0.56), hR=(0.74, 0.5), fL=(GL[0] + 0.3, 0), fR=(GR[0] + 0.0, 76), clL=(30, 10), clR=(30, 10)), "in3"),   # CONTACT: shoulder in his middle
    (9, G(hx=0.48, hz=0.46, spine=112, spine_mid=110, chest=104, neck=118, head=144, tw_chest=0,
          hL=(1.04, 0.62), hR=(1.0, 0.58), fL=(GL[0] + 0.62, 0), fR=(GR[0] + 0.4, 0.06, 40)), "lin"),                       # driving, his feet off the floor
    (13, G(hx=0.92, hz=0.3, spine=96, spine_mid=95, chest=93, neck=104, head=120, tw_chest=0,
           thL=24, shL=-84, fpL=-60, thR=10, shR=-92, fpR=-60, hL=(1.38, 0.06), hR=(1.34, 0.08), fiL=40, fiR=40), "in3"),     # SLAM: down on top of him
    (18, G(hx=0.95, hz=0.4, spine=140, spine_mid=134, chest=128, neck=134, head=150, tw_chest=0,
           thL=84, shL=0, fpL=0, thR=4, shR=-90, fpR=-70, uaL=50, faL=80, uaR=40, faR=70), "out"),                            # up onto a knee over him
    (24, G(hx=0.92, hz=0.58, spine=168, chest=158, fL=(GL[0] + 0.9, 0), fR=(GR[0] + 0.86, 40)), "out"),
    (30, at(GUARD, 0.9), "smooth"),
], [
    (0, _GUARD_V, "smooth"),
    (4, G(hx=-0.02, hz=0.62, spine=170, chest=162, head=168, uaL=90, faL=120, uaR=70, faR=110), "in"),                      # sees it coming, too late
    (6, G(hx=-0.06, hz=0.7, hips=176, spine=146, spine_mid=134, chest=122, neck=118, head=126,
          uaL=110, faL=150, uaR=100, faR=140, fL=(GL[0] - 0.06, 0.06, 20), fR=(GR[0] - 0.1, 40)), "in3"),                  # CONTACT: folded over the shoulder
    (9, G(hx=-0.42, hz=0.86, hips=214, spine=222, spine_mid=228, chest=232, neck=236, head=240,
          thL=72, shL=40, fpL=40, thR=58, shR=26, fpR=40, uaL=160, faL=176, uaR=150, faR=170), "in"),                     # lifted, going over backwards
    (13, P(LIE, hx=-0.72, hz=0.074, thL=80, shL=60, thR=70, shR=50, uaL=150, faL=170, uaR=140, faR=160), "in3"),           # SLAM: flat on his back
    (15, P(LIE, hx=-0.72, hz=0.12, chest=262, head=256, thL=90, shL=70, thR=84, shR=66), "out"),
    (30, P(LIE, hx=-0.72), "smooth"),
], hit=13, vshift=True, hshift=True, note="the spear: level change, shoulder through his middle, driven off his feet and slammed"))

# HEAD SCISSORS (swipe up and across at a man 1-2 m off): the leap, sat on his shoulders with the legs
# locked round his head, then thrown backwards in a walkover: the legs whip his head down and round, he
# somersaults forward over the hero and is slammed on his back behind him; the hero comes over onto his
# feet. (The torso numbers climb past 360: a full turn, not a spin back.)
_HS_HERO = [
    (0, GUARD, "smooth"),
    (3, G(hx=0.02, hz=0.52, spine=166, spine_mid=160, chest=152, neck=150, head=164, uaL=-20, faL=-10, uaR=-30, faR=-20,
          fL=(GL[0] + 0.04, 0), fR=(GR[0], 60)), "in"),                                                                   # the load: arms swung back
    (5, G(hx=0.2, hz=0.92, spine=176, spine_mid=176, chest=176, neck=172, head=174, uaL=160, faL=170, uaR=150, faR=160,
          thL=96, shL=10, fpL=30, thR=40, shR=-40, fpR=50), "out3"),                                                     # off the floor, knees coming up
    (7, G(hx=0.44, hz=1.38, hips=182, spine=184, spine_mid=184, chest=182, neck=178, head=176,
          thL=94, shL=-4, fpL=40, thR=86, shR=2, fpR=40, hL=(0.66, 1.36), hR=(0.62, 1.32), fiL=90, fiR=90), "in3"),       # ON HIS SHOULDERS: legs locked round his head
    (9, G(hx=0.45, hz=1.36, hips=186, spine=190, spine_mid=192, chest=194, neck=192, head=190,
          thL=94, shL=-4, fpL=40, thR=86, shR=2, fpR=40, hL=(0.6, 1.4), hR=(0.58, 1.36), fiL=90, fiR=90), "smooth"),
    (12, G(hx=0.32, hz=1.12, hips=238, spine=244, spine_mid=248, chest=252, neck=256, head=262,
           thL=112, shL=30, fpL=40, thR=104, shR=36, fpR=40, uaL=230, faL=236, uaR=224, faR=230), "in"),                   # thrown backwards, arms reaching for the floor
    (14, G(hx=0.04, hz=0.74, hips=300, spine=306, spine_mid=310, chest=314, neck=318, head=322,
           thL=160, shL=120, fpL=40, thR=150, shR=110, fpR=40, hL=(-0.36, 0.04), hR=(-0.32, 0.04), fiL=20, fiR=20), "lin"),  # upside down, hands planted, his head whipped down
    (16, G(hx=-0.24, hz=0.62, hips=350, spine=354, spine_mid=356, chest=358, neck=360, head=362,
           thL=210, shL=200, fpL=30, thR=196, shR=188, fpR=30, hL=(-0.36, 0.04), hR=(-0.32, 0.04)), "lin"),                 # legs over the top
    (19, G(hx=-0.42, hz=0.72, hips=420, spine=422, spine_mid=422, chest=420, neck=422, head=428,
           thL=150, shL=110, fpL=20, thR=136, shR=96, fpR=20, uaL=150, faL=160, uaR=140, faR=150), "lin"),                  # coming over, the feet reaching for the floor
    (22, G(hx=-0.52, hz=0.46, hips=500, spine=500, spine_mid=496, chest=492, neck=496, head=508,
           thL=80, shL=-10, fpL=0, thR=70, shR=-20, fpR=0, fL=(-0.38, 0), fR=(-0.72, 20), uaL=40, faL=70, uaR=30, faR=60), "in3"),  # lands, crouched over
    (27, turned(G(hx=-0.58, hz=0.56, spine=166, spine_mid=160, chest=154, neck=152, head=166,
                  fL=(GL[0] - 0.58, 0), fR=(GR[0] - 0.58, 40)), 360), "out"),
    (34, turned(at(GUARD, -0.6), 360), "smooth"),
]
_HS_MAN = [
    (0, _GUARD_V, "smooth"),
    (5, G(hx=0.0, hz=0.62, spine=172, chest=166, head=170, uaL=120, faL=150, uaR=110, faR=140), "in"),                 # arms up at the leap
    (7, G(hx=0.0, hz=0.58, spine=168, spine_mid=164, chest=158, neck=150, head=150, hL=(0.26, 1.22), hR=(0.22, 1.18),
          fL=(GL[0], 0), fR=(GR[0] - 0.06, 30)), "in3"),                                                                 # the weight lands on his shoulders
    (9, G(hx=0.02, hz=0.56, spine=164, spine_mid=158, chest=150, neck=142, head=140, hL=(0.24, 1.2), hR=(0.2, 1.16)), "smooth"),  # clawing at the legs
    (12, G(hx=0.18, hz=0.66, hips=150, spine=132, spine_mid=118, chest=104, neck=90, head=80,
           fL=(GL[0] + 0.1, 0.06, 20), fR=(GR[0], 0.1, 40), uaL=90, faL=60, uaR=80, faR=50), "in"),                     # yanked forward by the head, feet leaving
    (14, G(hx=0.62, hz=1.1, hips=56, spine=46, spine_mid=40, chest=32, neck=24, head=18,
           thL=196, shL=200, fpL=30, thR=180, shR=184, fpR=30, uaL=40, faL=20, uaR=30, faR=10), "in"),                   # over the top, upside down
    (16, G(hx=1.36, hz=0.62, hips=-34, spine=-40, spine_mid=-46, chest=-52, neck=-58, head=-62,
           thL=166, shL=150, fpL=30, thR=150, shR=136, fpR=30, uaL=150, faL=160, uaR=136, faR=150), "in"),
    (17, P(LIE, hx=GAP + 1.3, hz=0.074, hips=-90, spine=-90, spine_mid=-90, chest=-90, neck=-88, head=-86,
           thL=110, shL=100, thR=96, shR=90), "in3"),                                                                     # SLAM: flat on his back, behind the hero
    (19, P(LIE, hx=GAP + 1.32, hz=0.12, hips=-90, spine=-90, spine_mid=-90, chest=-98, neck=-102, head=-104, thL=100, shL=84), "out"),
    (34, P(LIE, hx=GAP + 1.32, hips=-90, spine=-90, spine_mid=-90, chest=-90, neck=-88, head=-86), "smooth"),
]
CLIPS.update(pair("head_scissors", 34, _HS_HERO, _HS_MAN, hit=17, vshift=True, hshift=True,
                  note="head scissors: onto his shoulders, legs locked, the walkover whips him over and down behind"))

# ---------------------------------------------------------------- ARM BREAKER (catch his punch) and TAKE HIS BACK
# Both start from his swing. The arm breaker is in the clinch's space (the man GAP in front, facing the
# hero). Taking his back ends with the hero behind him: the back hold's space has the man BG in front,
# FACING AWAY (his clips keyed in his own frame, the hero BG behind him).
BG = 0.5

# --- the wrist hold: turned side-on, his wrist in the lead hand, the rear hand on his elbow, the man bent
ARM_H = G(hx=0.0, hz=0.6, chest=164, spine=170, tw_hips=-50, tw_spine=-55, tw_chest=-60, neck=158, head=166,
          hL=(0.3, 0.95), hR=(0.42, 1.0), fiL=150, fiR=110, fL=(GL[0], 0), fR=(GR[0] - 0.05, 40))
ARM_V = P(HELD, hx=-0.04, hz=0.6, spine=160, spine_mid=148, chest=136, neck=128, head=132,
          hR=(GAP - 0.3, 0.95), hL=(0.22, 0.88), fiR=60, fiL=90, fL=(0.2, 0), fR=(-0.24, 26))

CLIPS.update(pair("arm_catch", 12, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.02, tw_hips=-20, tw_chest=-30, hL=(0.32, 1.12), hR=(0.24, 1.06), fiL=40, fiR=40, fL=(GL[0] + 0.04, 0)), "in"),   # hands shoot out, open
    (5, G(hx=0.04, hz=0.62, tw_hips=-35, tw_spine=-35, tw_chest=-45, chest=168, hL=(0.15, 1.1), hR=(0.3, 1.14),
          fiL=150, fiR=150, fR=(GR[0] - 0.05, 30)), "in3"),                                                                   # CAUGHT: the wrist, the elbow
    (8, P(ARM_H, hx=0.01, tw_chest=-66), "in"),                                                                               # turned, the arm pulled across
    (12, ARM_H, "smooth"),
], [
    (0, G(), "smooth"),
    (3, G(hx=0.06, tw_hips=24, tw_chest=30, uaR=60, faR=120, hR=(0.35, 1.12)), "in"),                                         # the punch comes
    (5, G(hx=0.1, tw_hips=32, tw_chest=40, chest=154, neck=150, head=162, hR=(GAP - 0.15, 1.1)), "in3"),                       # into his hands
    (8, P(ARM_V, hx=0.0, chest=142, head=140), "in"),                                                                         # jerked forward and down
    (12, ARM_V, "smooth"),
], hit=5, note="the catch: his punch taken at the wrist mid-flight, the arm pulled across, him folded over it"))

CLIPS.update(pair("arm_hold", 24, [
    (0, ARM_H, "smooth"),
    (8, P(ARM_H, hx=0.02, tw_chest=-66, hR=(0.43, 0.97)), "smooth"),
    (16, P(ARM_H, hx=-0.01, tw_chest=-56, hL=(0.29, 0.97)), "smooth"),
    (24, ARM_H, "smooth"),
], [
    (0, ARM_V, "smooth"),
    (8, P(ARM_V, hx=-0.06, chest=140, hL=(0.3, 0.92)), "smooth"),                                                              # pulling, prying at the hand
    (16, P(ARM_V, hx=-0.02, chest=132, head=126, hL=(0.18, 0.84)), "smooth"),
    (24, ARM_V, "smooth"),
], loop=True, breath=True, note="the wrist hold: his arm locked straight, him fighting it"))

CLIPS.update(pair("armbar_drop", 26, [
    (0, ARM_H, "smooth"),
    (4, P(ARM_H, hx=0.12, hz=0.55, spine=160, chest=150, hL=(0.38, 0.75), hR=(0.46, 0.72), fL=(GL[0] + 0.2, 0)), "in"),     # the weight onto his elbow
    (8, P(ARM_H, hx=0.22, hz=0.42, spine=140, chest=130, neck=140, head=150, hL=(0.5, 0.42), hR=(0.55, 0.44),
          fL=(GL[0] + 0.3, 0), fR=(GR[0] + 0.1, 70)), "in"),                                                                   # driving him down
    (12, P(ARM_H, hx=0.3, hz=0.36, spine=150, chest=140, neck=146, head=155, thL=84, shL=4, fpL=0, thR=4, shR=-86, fpR=-70,
           hL=(0.62, 0.12), hR=(0.7, 0.14), fiL=150, fiR=150), "in3"),                                                        # SLAM: a knee on him, the arm pinned straight
    (18, P(ARM_H, hx=0.3, hz=0.38, spine=152, chest=142, head=150, thL=84, shL=4, fpL=0, thR=4, shR=-86, fpR=-70,
           hL=(0.64, 0.12), hR=(0.68, 0.18)), "out"),
    (26, at(GUARD, 0.3), "smooth"),
], [
    (0, ARM_V, "smooth"),
    (4, P(ARM_V, hx=0.0, hz=0.5, spine=140, chest=120, neck=110, head=112, hR=(GAP - 0.38, 0.75)), "in"),
    (8, P(ARM_V, hx=0.06, hz=0.34, hips=124, spine=112, spine_mid=106, chest=100, neck=98, head=98,
          thL=60, shL=-40, thR=50, shR=-50, hR=(GAP - 0.5, 0.45)), "in"),                                                     # folding, face first
    (12, P(PRONE, hx=-0.15, hz=0.08, uaR=170, faR=176), "in3"),                                                              # SLAM: flat on his face, the arm up straight
    (14, P(PRONE, hx=-0.15, hz=0.12, head=94, uaR=168, faR=174), "out"),
    (26, P(PRONE, hx=-0.15), "smooth"),
], hit=12, vshift=True, hshift=True, note="the arm bar: the weight on his locked elbow drives him face first; a knee on his back"))

CLIPS.update(pair("arm_break", 16, [
    (0, ARM_H, "smooth"),
    (3, P(ARM_H, hR=(0.38, 0.7), tw_chest=-72, chest=158), "in"),                                                              # the rear forearm drops under his elbow
    (5, P(ARM_H, hR=(0.44, 1.14), tw_chest=-40, chest=166, spine=176, fiR=165), "in3"),                                        # CRACK: driven up through it
    (8, P(ARM_H, hR=(0.44, 1.1), tw_chest=-36, fiL=60), "out"),
    (16, GUARD, "smooth"),
], [
    (0, ARM_V, "smooth"),
    (5, P(ARM_V, hR=(GAP - 0.44, 1.2), chest=150, neck=160, head=172), "in3"),                                                 # the elbow goes the wrong way
    (9, G(hx=-0.25, hz=0.58, spine=166, chest=150, neck=148, head=150, hL=(0.12, 0.92), hR=(0.08, 0.82), fiL=140, fiR=40), "out"),  # clutching it, reeling
    (16, G(hx=-0.32, hz=0.6, spine=168, chest=156, head=152, hL=(0.12, 0.95), hR=(0.1, 0.86), fiL=140, fiR=40), "smooth"),
], hit=5, vshift=True, note="the arm break: the forearm driven up through his locked elbow; he reels away clutching it"))

# --- taking his back: under the swing, past his armpit, out behind him, turning to face his back
BACK_H = G(hx=0.08, hz=0.6, chest=164, spine=172, neck=160, head=166, hL=(BG + 0.12, 0.82), hR=(BG + 0.1, 0.8),
           fiL=130, fiR=130, fL=(0.26, 0), fR=(-0.1, 40))
BACK_V = G(hx=0.0, hz=0.62, spine=172, chest=164, neck=160, head=166, hL=(0.12, 0.82), hR=(0.1, 0.8), fiL=100, fiR=100,
           fL=(0.2, 0), fR=(-0.2, 20))

_db_h = [
    (0, GUARD, "smooth"),
    (3, G(hx=0.18, hz=0.45, spine=150, chest=140, neck=150, head=160, fL=(GL[0] + 0.3, 0), fR=(GR[0], 60)), "in"),               # ducks under the hook
    (6, G(hx=0.55, hz=0.42, spine=146, chest=138, neck=146, head=156, tw_hips=30, tw_chest=40,
          fL=(0.85, 0), fR=(GR[0] + 0.3, 0.05, 30)), "lin"),                                                                   # under his arm
    (9, G(hx=0.95, hz=0.5, spine=160, chest=152, tw_hips=-50, tw_spine=-60, tw_chest=-70, fL=(1.15, 0), fR=(0.8, 40)), "out"),  # through, turning
    (12, G(hx=1.08, hz=0.58, spine=168, chest=160, tw_hips=-80, tw_spine=-85, tw_chest=-90, neck=156, head=164,
           fL=(1.2, 0), fR=(1.0, 30)), "smooth"),
    (16, at(G(hz=0.6, spine=170, chest=162, tw_hips=-90, tw_spine=-90, tw_chest=-90, neck=158, head=164), GAP + BG), "smooth"),  # facing the camera: the pivot
]
_db_v = [
    (0, G(), "smooth"),
    (3, G(hx=0.1, tw_hips=30, tw_chest=45, uaR=100, faR=110, hR=(0.55, 1.15)), "in"),                                         # the big hook
    (6, G(hx=0.2, hz=0.6, spine=164, chest=150, neck=150, head=160, tw_chest=70, hR=(0.62, 1.0)), "in3"),                      # through air: he ducked
    (9, G(hx=0.22, spine=160, chest=148, head=160, fL=(GL[0] + 0.26, 0), fR=(GR[0] + 0.1, 0.06, 30)), "out"),                 # stumbling after it
    (16, G(hx=0.05, hz=0.62, spine=170, chest=160, head=164), "smooth"),
]
CLIPS.update(pair("duck_behind", 16, _db_h, _db_v, hshift=True,
                  note="take his back: under his hook, past his armpit, out behind him, turning to face his back"))

_bh = pair("back_hold", 24, [
    (0, BACK_H, "smooth"),
    (8, P(BACK_H, hx=0.1, chest=160, hR=(BG + 0.13, 0.84)), "smooth"),
    (16, P(BACK_H, hx=0.06, chest=166, hL=(BG + 0.1, 0.8)), "smooth"),
    (24, BACK_H, "smooth"),
], [
    (0, BACK_V, "smooth"),
    (8, P(BACK_V, hx=0.04, chest=158, tw_chest=12, hL=(0.16, 0.84)), "smooth"),                                                # prying at the hands
    (16, P(BACK_V, hx=-0.02, chest=168, tw_chest=-10, head=160), "smooth"),
    (24, BACK_V, "smooth"),
], loop=True, breath=True, note="the rear waist lock: his back to the hero, prying at the hands")
_bh["back_hold"].update(vat=BG, vface=1)
CLIPS.update(_bh)

_bk = pair("back_kidney", 14, [
    (0, BACK_H, "smooth"),
    (3, P(BACK_H, hR=(0.3, 0.95), tw_chest=-25), "in"),
    (5, P(BACK_H, hR=(BG + 0.05, 0.84), tw_chest=15, chest=160), "in3"),                                                       # into his kidney
    (9, P(BACK_H, hR=(BG + 0.08, 0.82)), "out"),
    (14, BACK_H, "smooth"),
], [
    (0, BACK_V, "smooth"),
    (5, P(BACK_V, hx=0.05, spine=160, chest=150, head=158, tw_chest=20, hL=(0.04, 0.76)), "in3"),
    (14, BACK_V, "smooth"),
], hit=5, note="from behind: a short hook into his kidney, the hold kept")
_bk["back_kidney"].update(vat=BG, vface=1)
CLIPS.update(_bk)

_gs = pair("german_suplex", 40, [
    (0, BACK_H, "smooth"),
    (4, P(BACK_H, hx=0.06, hz=0.5, spine=168, chest=160, hL=(BG + 0.08, 0.7), hR=(BG + 0.06, 0.68)), "in"),                    # hips drop under him
    (8, P(BACK_H, hx=0.02, hz=0.66, hips=190, spine=196, spine_mid=200, chest=204, neck=206, head=212,
          hL=(0.4, 1.1), hR=(0.38, 1.08), fL=(0.26, 30), fR=(-0.1, 50)), "in"),                                                # up on his toes, lifting
    (12, P(BACK_H, hx=-0.1, hz=0.6, hips=220, spine=232, spine_mid=238, chest=246, neck=252, head=262,
           hL=(0.05, 1.35), hR=(0.03, 1.32), fL=(0.3, 0), fR=(0.0, 0)), "lin"),                                                 # arching back, him over the top
    (16, P(BACK_H, hx=-0.12, hz=0.45, hips=250, spine=266, spine_mid=280, chest=300, neck=318, head=330,
           hL=(-0.35, 0.3), hR=(-0.37, 0.28), fL=(0.32, 0), fR=(0.02, 0)), "in3"),                                             # SLAM: the bridge
    (21, P(BACK_H, hx=-0.12, hz=0.44, hips=252, spine=268, spine_mid=282, chest=302, neck=320, head=332,
           hL=(-0.4, 0.2), hR=(-0.42, 0.18), fL=(0.32, 0), fR=(0.02, 0)), "out"),                                              # held, the bridge
    (27, P(KNEEL, hx=-0.06, hz=0.34, hips=200, spine=196, spine_mid=190, chest=180, neck=172, head=168,
           thL=90, shL=10, thR=40, shR=-60), "in"),                                                                            # rolls up off it
    (33, P(KNEEL, hx=-0.06), "smooth"),
    (40, at(GUARD, -0.06), "smooth"),
], [
    (0, BACK_V, "smooth"),
    (4, P(BACK_V, hx=-0.02, hz=0.6, spine=176, chest=172), "in"),
    (8, P(BACK_V, hx=-0.15, hz=0.95, hips=200, spine=205, spine_mid=207, chest=210, neck=212, head=215,
          thL=30, shL=20, thR=20, shR=10, uaL=100, faL=120, uaR=110, faR=130), "in"),                                         # lifted off his feet
    (12, P(BACK_V, hx=-0.5, hz=1.25, hips=260, spine=270, spine_mid=274, chest=280, neck=285, head=290,
           thL=150, shL=160, thR=140, shR=150, uaL=200, faL=210, uaR=190, faR=200), "lin"),                                    # over the top, upside down
    (16, P(BACK_V, hx=-0.9, hz=0.35, hips=300, spine=310, spine_mid=316, chest=320, neck=325, head=330,
           thL=200, shL=210, thR=190, shR=200, uaL=240, faL=250, uaR=230, faR=240), "in3"),                                    # SLAM: onto his shoulders and neck
    (20, P(BACK_V, hx=-1.0, hz=0.12, hips=280, spine=285, spine_mid=286, chest=288, neck=289, head=290,
           thL=120, shL=100, thR=110, shR=90), "out"),                                                                         # the legs come over
    (26, P(LIE, hx=-1.05), "in"),
    (40, P(LIE, hx=-1.05), "smooth"),
], hit=16, vshift=True, hshift=True, note="the German suplex: lifted from the waist lock, the bridge, him over the top onto his neck behind")
_gs["german_suplex"].update(vat=BG, vface=1)
CLIPS.update(_gs)

# ---------------------------------------------------------------- running attacks
CLIPS["flying_knee"] = clip(24, [
    (0, RUN0, "lin"),
    (3, P(LOOSE, hx=0.22, hz=0.62, spine=166, chest=160, neck=158, head=170, fL=(0.3, 30), fR=(0.12, 0.2, 30),
          uaL=70, faL=130, uaR=50, faR=120, fiL=90, fiR=90), "in"),                                         # the take-off foot plants, everything loads
    (6, P(LOOSE, hx=0.42, hz=0.82, spine=186, spine_mid=184, chest=180, neck=170, head=172,
          thL=-28, shL=-70, fpL=40, thR=96, shR=-10, fpR=40,
          hL=(0.82, 1.06), hR=(0.84, 1.02), fiL=60, fiR=60, clL=(30, 20), clR=(30, 20)), "out3"),          # off the ground: the hands reach for his head, the knee rising
    (8, P(LOOSE, hx=0.56, hz=0.9, spine=192, spine_mid=190, chest=184, neck=170, head=170,
          thL=-30, shL=-90, fpL=40, thR=142, shR=-6, fpR=50,
          hL=(0.82, 0.98), hR=(0.84, 0.95), fiL=120, fiR=120, clL=(24, 6), clR=(24, 6)), "in3"),          # HIT: the head pulled down onto the rising knee, hips thrust
    (11, P(LOOSE, hx=0.7, hz=0.82, spine=180, chest=172, neck=164, head=170, thL=10, shL=-30, fpL=30, thR=70, shR=10,
           hL=(0.86, 0.86), hR=(0.88, 0.84), fiL=110, fiR=110), "out"),                                       # dropping, still holding on
    (14, P(LOOSE, hx=0.8, hz=0.54, spine=162, chest=152, neck=150, head=164, fL=(0.98, 0), fR=(0.86, 0.12, 0),
           uaL=60, faL=110, uaR=50, faR=100, fiL=140, fiR=140), "in3"),                                        # lands on the trail foot, soaks it
    (18, P(GUARD, hx=0.82, hz=0.6, fL=(GL[0] + 0.86, 0), fR=(GR[0] + 0.86, 0.05, 20)), "out"),
    (24, at(GUARD, 0.86), "smooth"),
], shift=True, hit=8, reach=1.15, striker="R", react="hit_chin", note="the flying knee off the run: plant, leap, his head pulled onto the knee, land on guard")

CLIPS["slide_tackle"] = clip(30, [
    (0, RUN0, "lin"),
    (3, P(LOOSE, hx=0.36, hz=0.52, spine=196, spine_mid=198, chest=200, neck=186, head=176, fL=(0.42, 20),
          fR=(0.82, 0.16, -20), uaL=-30, faL=-10, uaR=60, faR=90, fiL=40, fiR=120), "in"),                   # the drop: leaning back, the lead leg shooting out low
    (6, P(LOOSE, hx=0.86, hz=0.2, spine=214, spine_mid=212, chest=208, neck=192, head=176,
          thR=80, shR=84, fpR=-6, thL=48, shL=-56, fpL=20, hL=(0.6, 0.06), fiL=20, uaR=60, faR=96, fiR=150), "out"),     # on the floor, sliding: the near hand trails on the ground
    (10, P(LOOSE, hx=1.5, hz=0.2, spine=212, spine_mid=210, chest=206, neck=188, head=174,
           thR=82, shR=86, fpR=-14, thL=46, shL=-60, fpL=20, hL=(1.22, 0.06), fiL=20, uaR=64, faR=104, fiR=160), "lin"),  # HIT: through his ankles
    (15, P(LOOSE, hx=1.92, hz=0.24, spine=204, chest=196, neck=182, head=172,
           thR=74, shR=76, thL=60, shL=-40, fpL=10, hL=(1.7, 0.06), fiL=20, uaR=50, faR=90), "out"),          # slowing
    (20, P(LOOSE, hx=2.02, hz=0.4, spine=164, spine_mid=160, chest=156, neck=152, head=166, fL=(2.12, 30), fR=(2.3, 0.12, 0),
           hL=(1.9, 0.06), fiL=20, uaR=60, faR=110), "in"),                                                    # up off the bent knee, a hand pushing off the floor
    (25, P(GUARD, hx=2.1, hz=0.6, fL=(GL[0] + 2.12, 0), fR=(GR[0] + 2.12, 30), spine=172, chest=162), "out"),
    (30, at(GUARD, 2.12), "smooth"),
], shift=True, hit=10, reach=2.1, striker="R", react="hit_legs", note="the baseball slide: lean back, drop, through his ankles, up off the bent knee")

CLIPS["shoulder_barge"] = clip(20, [
    (0, SPRINT0, "lin"),
    (3, P(LOOSE, hx=0.4, hz=0.56, spine=150, spine_mid=146, chest=140, neck=150, head=166, tw_hips=-26, tw_chest=-48,
          fL=(0.62, 0), fR=(0.24, 0.12, 30), uaL=24, faL=120, uaR=40, faR=134, fiL=160, fiR=160, clL=(26, 18)), "in"),   # dip: the lead shoulder turned in, the arm tucked
    (6, P(LOOSE, hx=0.66, hz=0.62, spine=160, spine_mid=158, chest=156, neck=156, head=164, tw_hips=-34, tw_chest=-58,
          fL=(0.72, 0), fR=(0.32, 70), uaL=20, faL=124, uaR=36, faR=140, fiL=165, fiR=165, clL=(36, 24)), "out3"),         # HIT: the legs drive up through the shoulder
    (10, P(LOOSE, hx=0.82, hz=0.64, spine=172, chest=170, neck=164, head=172, tw_hips=-20, tw_chest=-26,
           fL=(0.86, 0), fR=(0.5, 0.1, 20), uaL=40, faL=100, uaR=30, faR=110), "out"),                                    # recoil, rebalancing
    (14, P(GUARD, hx=0.86, fL=(GL[0] + 0.9, 0), fR=(GR[0] + 0.9, 0.05, 20)), "in"),
    (20, at(GUARD, 0.9), "smooth"),
], shift=True, hit=6, reach=0.95, striker="L", react="stumble_back", note="the shoulder barge: dip, the shoulder turned in, the legs drive up through it")

# ---------------------------------------------------------------- the throw (an object in the rear hand)
CLIPS["throw_object"] = clip(24, [
    (0, G(fiR=140), "lin"),
    (5, G(hx=-0.05, hz=0.62, tw_hips=-34, tw_spine=-30, tw_chest=-44, spine=184, chest=188, neck=170, head=172,
          uaR=262, faR=196, haR=210, fiR=140, uaL=96, faL=104, fiL=60, fR=(GR[0], 10), fL=(GL[0], 0)), "out"),          # cocked: weight back, the arm high behind, the lead hand aims
    (9, G(hx=0.08, hz=0.62, tw_hips=-10, tw_chest=-34, spine=180, chest=182, uaR=250, faR=176, fiR=140,
          uaL=92, faL=100, fiL=60, fL=(GL[0] + 0.3, 0.06, -10), fR=(GR[0], 30)), "in"),                                   # the stride: lead foot reaching, the hips open first
    (11, G(hx=0.16, hz=0.6, tw_hips=24, tw_chest=10, spine=170, chest=162, neck=158, head=168,
           uaR=168, faR=150, haR=140, fiR=110, uaL=30, faL=60, fiL=120, fL=(GL[0] + 0.34, 0), fR=(GR[0], 60)), "in3"),    # RELEASE: the chest whips round, the arm last
    (14, G(hx=0.2, hz=0.58, tw_hips=36, tw_chest=40, spine=156, chest=146, neck=150, head=164,
           uaR=40, faR=20, haR=0, fiR=40, uaL=-10, faL=40, fiL=120, fL=(GL[0] + 0.34, 0), fR=(GR[0] + 0.1, 0.08, 40)), "out"),  # follow-through across the body, the back foot coming up
    (19, G(hx=0.18, fL=(GL[0] + 0.34, 0), fR=(GR[0] + 0.24, 30), fiR=150), "out"),
    (24, at(GUARD, 0.2), "smooth"),
], shift=True, hit=11, striker="R", note="the overhand throw: weight back, step, the hips, the chest, the arm last, follow through")

# ---------------------------------------------------------------- off the line
CLIPS["sidestep"] = clip(22, [
    (0, GUARD, "lin"),
    (4, G(hx=-0.04, hz=0.62, tw_hips=60, tw_spine=50, tw_chest=70, spine=184, spine_mid=186, chest=188, neck=170, head=168,
          fR=(-0.44, 0.06, 0), uaL=80, faL=120, uaR=30, faR=150), "out3"),                                    # the pivot: the rear foot swings away, the body turned out of his path
    (7, G(hx=-0.08, hz=0.62, tw_hips=72, tw_chest=82, spine=186, chest=190, neck=172, head=166, fR=(-0.46, 20),
          uaL=96, faL=126, uaR=40, faR=160), "out"),                                                         # he goes past (the belly sucked in)
    (11, G(hx=-0.06, hz=0.6, tw_hips=40, tw_chest=10, spine=168, chest=160, neck=158, head=160, fR=(-0.46, 30),
           uaR=-70, faR=-100, fiR=165, uaL=50, faL=130), "in3"),                                               # HIT: the hammer down on the back of his head
    (15, G(hx=-0.08, tw_hips=10, tw_chest=0, fR=(-0.46, 30), uaR=-30, faR=-20), "out"),
    (22, P(GUARD, hx=-0.1, fL=(GL[0] - 0.1, 0), fR=(GR[0] - 0.1, 38)), "smooth"),
], hit=11, striker="R", note="the matador: pivot off the line, he charges past, the hammer on the back of his head")

CLIPS["swat"] = clip(12, [
    (0, GUARD, "lin"),
    (3, G(hz=0.62, tw_chest=-22, tw_hips=-14, uaL=126, faL=196, haL=206, fiL=90, head=160, neck=152,
          clL=(24, 18), fR=(GR[0], 46)), "out3"),                                                               # HIT: the forearm sweeps up and out
    (6, G(hz=0.63, tw_chest=-4, uaL=96, faL=60, haL=40, fiL=110, head=166), "out"),                            # the hand carries on through, aside
    (12, GUARD, "smooth"),
], hit=3, striker="L", note="swatting something thrown: the lead forearm up and out, the hand carries it aside")

# ---------------------------------------------------------------- the chain
CLIPS["chain_lash"] = clip(24, [
    (0, G(fiR=150), "lin"),
    (5, G(hx=-0.05, hz=0.62, tw_hips=-26, tw_chest=-46, spine=182, chest=184, uaR=232, faR=206, haR=220, fiR=150,
          uaL=84, faL=112, fiL=90, fR=(GR[0], 20)), "out"),                                                     # wound back over the shoulder
    (9, G(hx=0.04, tw_hips=-6, tw_chest=-16, spine=176, chest=174, uaR=178, faR=168, haR=160, fiR=150,
          fL=(GL[0] + 0.22, 0.06, -8), fR=(GR[0], 40)), "in"),                                                  # over the top, stepping in
    (12, G(hx=0.12, hz=0.6, tw_hips=18, tw_chest=24, spine=164, chest=154, neck=152, head=164,
           uaR=104, faR=76, haR=60, fiR=165, uaL=30, faL=70, fL=(GL[0] + 0.24, 0), fR=(GR[0], 60)), "in3"),      # HIT: whipped down through him, the wrist snapping
    (16, G(hx=0.14, hz=0.6, tw_hips=26, tw_chest=36, spine=160, chest=150, uaR=30, faR=20, haR=10, fiR=150,
           fL=(GL[0] + 0.24, 0), fR=(GR[0] + 0.06, 0.06, 40)), "out"),                                           # follow-through, the chain swinging back
    (24, P(GUARD, hx=0.14, fL=(GL[0] + 0.24, 0), fR=(GR[0] + 0.14, 38), fiR=150), "smooth"),
], shift=True, hit=12, reach=2.6, striker="R", react="hit_front", note="the chain: wound back over the shoulder, a step, whipped down through him")

DRAG = 1.8      # how far the chain hauls him (his clip ends DRAG nearer, the hero's GAP in front)
CLIPS.update(pair("chain_drag", 24, [
    (0, P(GUARD, hL=(0.36, 0.88), hR=(0.3, 0.84), fiL=150, fiR=150, tw_chest=-10), "lin"),                      # the chain taut, both hands on it
    (4, P(GUARD, hx=-0.08, hz=0.62, spine=194, spine_mid=196, chest=198, neck=176, head=170, hL=(0.1, 0.84), hR=(0.04, 0.8),
          fiL=165, fiR=165, fL=(GL[0], 0), fR=(GR[0] - 0.08, 0)), "out3"),                                      # the first haul: leaning back, the back foot flat
    (8, P(GUARD, hx=-0.06, hz=0.62, spine=188, chest=190, neck=174, head=170, hL=(0.08, 0.84), hR=(0.42, 0.9),
          fiL=165, fiR=60, fL=(GL[0], 0), fR=(GR[0] - 0.08, 0)), "out"),                                         # hand over hand: the rear hand reaches along it
    (12, P(GUARD, hx=-0.1, hz=0.61, spine=198, spine_mid=198, chest=200, neck=178, head=170, hL=(0.36, 0.88), hR=(0.0, 0.8),
           fiL=60, fiR=165, fL=(GL[0], 0), fR=(GR[0] - 0.08, 0)), "out3"),                                       # the second haul
    (17, P(HOLD, hx=0.0, hz=0.63, hL=(0.44, 1.0), hR=(0.44, 0.98), fiL=60, fiR=60), "in"),                       # dropping the chain, the hands going for his neck
    (20, HOLD, "in3"),                                                                                           # HIT: the clinch locked
    (24, HOLD, "out"),
], [
    (0, P(LOOSE, hz=0.66, spine=172, chest=168, neck=168, head=160, hL=(0.16, 1.0), hR=(0.14, 0.98), fiL=120, fiR=120,
          fL=(0.12, 0), fR=(-0.06, 20)), "lin"),                                                                # the chain round his neck, clawing at it
    (4, P(LOOSE, hx=0.42, hz=0.62, spine=150, spine_mid=148, chest=140, neck=136, head=140, hL=(0.6, 0.98), hR=(0.58, 0.94),
          fiL=130, fiR=130, fL=(0.12, 40), fR=(0.56, 0.16, 10)), "out3"),                                       # yanked off his feet, falling forward
    (8, P(LOOSE, hx=0.84, hz=0.6, spine=154, chest=146, neck=140, head=142, hL=(1.02, 0.94), hR=(1.0, 0.92),
          fiL=130, fiR=130, fL=(0.44, 0.14, 20), fR=(0.86, 0)), "in"),                                          # a stumbling step
    (12, P(LOOSE, hx=1.3, hz=0.6, spine=152, chest=144, neck=138, head=140, hL=(1.48, 0.94), hR=(1.46, 0.92),
           fiL=130, fiR=130, fL=(1.4, 0), fR=(0.94, 0.12, 30)), "out3"),                                        # hauled again
    (17, P(HELD, hx=DRAG - 0.08, fL=(HELD["fL"][0] + DRAG - 0.04, 0), fR=(HELD["fR"][0] + DRAG - 0.1, 0.1, 20)), "in"),
    (20, at(HELD, DRAG), "in3"),
    (24, at(HELD, DRAG), "out"),
], hit=20, vshift=True, note="the chain round him: hauled in hand over hand, stumbling, into the clinch"))
CLIPS["chain_drag"]["vat"] = 0.62 + DRAG

# ---------------------------------------------------------------- the takedown from behind
# He is crouched 0.42 behind the man, both facing +x. The man's clip is in HIS frame: the hero is
# 0.42 behind him (-x). Rear naked choke: the near arm round the throat, the other hand on the back
# of his head, the chest against his back; sat back so his heels come off, down to a knee, laid down.
BEHIND = 0.42
CLIPS["takedown_choke"] = dict(frames=64, hit=12, load=None, loop=False, aim=None, reach=BEHIND + 0.1, react=None,
                               striker=None, shift=False, breath=False, breaths=2, lag=True, victim="takedown_choke_v", vat=BEHIND, vface=1,
                               note="the rear naked choke from a crouch: up, the arm round the throat, sat back, laid down",
                               keys=[
    (0, CROUCH, "lin"),
    (5, P(LOOSE, hx=0.1, hz=0.56, spine=176, spine_mid=174, chest=170, neck=156, head=164, fL=(0.32, 0), fR=(0.0, 40),
          hR=(0.52, 1.02), hL=(0.4, 1.12), fiR=60, fiL=40), "out3"),                                             # up and in: the arm snakes round
    (9, P(LOOSE, hx=0.12, hz=0.56, spine=180, chest=180, neck=158, head=162, fL=(0.32, 0), fR=(0.0, 30),
          hR=(0.48, 1.0), hL=(0.36, 1.1), fiR=165, fiL=90, tw_chest=-6), "in3"),
    (12, P(LOOSE, hx=0.1, hz=0.56, spine=186, chest=188, neck=160, head=162, fL=(0.32, 0), fR=(-0.02, 20),
           hR=(0.44, 0.99), hL=(0.34, 1.08), fiR=165, fiL=120, clR=(10, 4), clL=(14, 8)), "in"),                  # LOCKED: the chest on his back, squeezing
    (22, P(LOOSE, hx=-0.02, hz=0.52, spine=196, spine_mid=198, chest=198, neck=164, head=164, fL=(0.34, 0), fR=(-0.26, 10),
           hR=(0.36, 0.92), hL=(0.26, 1.0), fiR=165, fiL=120, tw_chest=8), "smooth"),                            # sitting back: his heels come off
    (30, P(LOOSE, hx=-0.06, hz=0.44, spine=192, chest=194, neck=178, head=170, thL=84, shL=4, fpL=0, thR=10, shR=-82, fpR=-70,
           hR=(0.3, 0.76), hL=(0.22, 0.84), fiR=165, fiL=120, tw_chest=-6), "smooth"),                            # down onto a knee with him; he's fading
    (40, P(LOOSE, hx=-0.06, hz=0.42, spine=186, chest=188, neck=176, head=168, thL=84, shL=4, fpL=0, thR=8, shR=-84, fpR=-70,
           hR=(0.3, 0.7), hL=(0.22, 0.78), fiR=165, fiL=110), "smooth"),                                         # held a beat after he goes limp
    (50, P(LOOSE, hx=0.0, hz=0.4, spine=150, spine_mid=150, chest=146, neck=150, head=158, thL=84, shL=4, fpL=0, thR=8, shR=-84, fpR=-70,
           hR=(0.36, 0.2), hL=(0.26, 0.26), fiR=40, fiL=40), "smooth"),                                          # laid down, the hands sliding out from under him
    (64, CROUCH, "smooth"),
])
CLIPS["takedown_choke_v"] = dict(frames=64, hit=None, load=None, loop=False, aim=None, reach=None, react=None,
                                 striker=None, shift=True, breath=False, breaths=2, lag=True, note="choked from behind (held)",
                                 keys=[
    (0, P(LOOSE), "lin"),
    (5, P(LOOSE, hx=-0.02, spine=184, chest=186, neck=188, head=196, hL=(0.18, 0.98), hR=(0.2, 0.94), fiL=120, fiR=120,
          fL=(0.12, 20), fR=(-0.04, 30)), "out3"),                                                              # yanked back, the hands fly to the arm
    (12, P(LOOSE, hx=-0.04, hz=0.66, spine=188, chest=192, neck=190, head=198, hL=(0.1, 0.96), hR=(0.12, 0.92), fiL=140, fiR=140,
           fL=(0.3, 0.2, 20), fR=(-0.02, 40), tw_chest=12), "out"),                                              # a leg kicks up
    (18, P(LOOSE, hx=-0.06, hz=0.66, spine=190, chest=194, neck=190, head=198, hL=(0.08, 0.94), hR=(0.1, 0.9), fiL=150, fiR=150,
           fL=(0.12, 30), fR=(0.24, 0.16, 20), tw_chest=-10), "out"),                                            # the other
    (22, P(LOOSE, hx=-0.18, hz=0.56, spine=200, spine_mid=200, chest=202, neck=200, head=210, hL=(-0.02, 0.86), hR=(0.0, 0.82),
           fiL=120, fiR=120, fL=(0.34, -10), fR=(0.22, 0.12, -10)), "smooth"),                                   # the heels off: the weight on the hero
    (30, P(LOOSE, hx=-0.22, hz=0.36, spine=208, spine_mid=208, chest=210, neck=206, head=214, thL=76, shL=70, fpL=-10, thR=70, shR=60, fpR=-10,
           hL=(-0.08, 0.66), hR=(-0.04, 0.62), fiL=70, fiR=70), "smooth"),                                        # dragged down, the legs out in front
    (40, P(LOOSE, hx=-0.22, hz=0.34, spine=210, spine_mid=210, chest=212, neck=212, head=226, thL=80, shL=80, fpL=-20, thR=74, shR=72, fpR=-20,
           uaL=200, faL=200, uaR=196, faR=196, fiL=30, fiR=30), "smooth"),                                        # limp: the arms fall, the head lolls
    (50, P(LIE, hx=0.5, hz=0.09), "in"),                                                                        # laid down in front of him
    (64, P(LIE, hx=0.55), "smooth"),
])

# ---------------------------------------------------------------- a man on watch (unaware)
LOOK = P(LOOSE, hz=0.674, uaL=6, faL=24, haL=30, fiL=60, fL=(0.1, 0), fR=(-0.06, 0), tw_hips=6)
CLIPS["unaware"] = clip(150, [
    (0, LOOK, "smooth"),
    (20, P(LOOK, hR=(0.14, 1.08), fiR=40, head=176, tw_head=4), "smooth"),                                       # the cigarette up to the mouth
    (34, P(LOOK, hR=(0.14, 1.1), fiR=40, head=182, neck=176, chest=178), "smooth"),                              # a drag (the chest rises)
    (48, P(LOOK, hR=(0.22, 0.9), fiR=40, head=186, neck=178, tw_head=-4), "smooth"),                             # out: the smoke goes up, the head back
    (66, P(LOOK, uaR=10, faR=40, haR=50, fiR=40, head=178, tw_head=-30, tw_hips=10, fR=(-0.06, 10)), "smooth"),  # looks off along the wall
    (90, P(LOOK, uaR=10, faR=40, haR=50, fiR=40, head=170, tw_head=-20, neck=170), "smooth"),                   # at his feet, scuffs one
    (100, P(LOOK, uaR=10, faR=40, haR=50, fiR=40, head=172, fR=(0.0, 0.05, 20), liftR=0.03), "smooth"),
    (112, P(LOOK, uaR=8, faR=30, fiR=40, head=176, tw_head=18, fR=(-0.04, 0)), "smooth"),                      # the other way
    (132, P(LOOK, hR=(0.14, 1.06), fiR=40, head=176, tw_head=6), "smooth"),
    (150, LOOK, "smooth"),
], loop=True, breath=True, breaths=5, note="a man on watch who hasn't seen you: a cigarette, bored, looking about")
