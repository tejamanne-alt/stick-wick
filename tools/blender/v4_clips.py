"""Chapter 1's fight moves, hand-keyed for the 29-bone V4 body (pose language: v4_pose.py).

The hero faces +x. L = far side = LEAD (left foot and fist forward), R = near side = REAR (power).
Frames at 30 fps. Planted feet are given by the BALL of the foot: fL=(x, heel pitch).

Every strike is keyed through its whole chain of events, not just its end pose:
  ready → the trigger (the first thing that moves, usually a foot or the hips) → the drive (hips,
  then spine, then shoulder, then arm, each a beat after the last) → the turnover (the fist rotates
  in the last third) → IMPACT (a hard stop, the shoulder up to cover the chin) → penetration (one more
  centimetre) → the snap back (faster than it went) → the recovery (feet re-set, the guard re-forms,
  a small overshoot) → settled.
and the engine adds the overlap (head and off arm lag), the step mechanics (heel peel, swing, heel
strike, roll) and the impact recoil on every frame."""
import v4_pose as vp
from v4_pose import P

# ---------------------------------------------------------------- the guard
# Side-on, the lead shoulder forward and up, the chin down behind it; the lead fist at chin height
# out in front, turned a little, the rear fist at the cheek; elbows in over the ribs; the spine curved
# forward; knees soft; the weight 55/45 on the back foot; the rear heel up on the ball.
GUARD = P(hx=0.0, hz=0.64,
          hips=180, tw_hips=-12, spine=176, tw_spine=-8, spine_mid=171, tw_spine_mid=-6, chest=166, tw_chest=-6,
          neck=160, head=172,
          clL=(14, 10), clR=(2, 6),
          uaL=62, faL=150, haL=156, yw_uaL=-6, yw_faL=-10, twL=-30, fiL=165,
          uaR=22, faR=166, haR=172, yw_uaR=4, yw_faR=6, twR=20, fiR=165,
          fL=(0.24, 0), fR=(-0.13, 38))
FL, FR = GUARD["fL"], GUARD["fR"]


def G(**kw):
    return P(GUARD, **kw)


def clip(frames, keys, hit=None, load=None, loop=False, aim=None, reach=None, react=None, note="",
         striker=None, shift=False, breath=False, breaths=2, lag=True):
    return dict(frames=frames, keys=keys, hit=hit, load=load, loop=loop, aim=aim, reach=reach, react=react,
                note=note, striker=striker, shift=shift, breath=breath, breaths=breaths, lag=lag)


CLIPS = {}

# ---------------------------------------------------------------- IDLE: a live guard (3 s loop)
# Two breaths (the engine's breath layer) on top of: the weight rocking from the back foot to the
# front and back, the rear heel bouncing a little with it, the lead fist drawing a slow circle, the
# rear fist re-settling at the cheek once, the head making small checks, the shoulders loosening.
CLIPS["idle"] = clip(90, [
    (0, GUARD, "smooth"),
    (15, G(hx=0.008, fR=(-0.13, 34), uaL=60, faL=153, twL=-26, head=171, tw_head=-3, clL=(15, 11)), "smooth"),
    (30, G(hx=0.016, hz=0.637, fR=(-0.13, 30), uaL=63, faL=149, twL=-33, chest=167, tw_chest=-8, head=173, tw_head=-1), "smooth"),
    (44, G(hx=0.01, fR=(-0.13, 36), uaL=64, faL=147, uaR=24, faR=163, haR=168, head=172, tw_head=2, clL=(13, 9)), "smooth"),
    (52, G(hx=0.006, uaR=21, faR=168, haR=174, fiR=170, clR=(1, 9)), "smooth"),          # the rear fist re-settles at the cheek
    (66, G(hx=-0.004, hz=0.642, fR=(-0.13, 41), uaL=61, faL=151, twL=-29, chest=165, tw_chest=-5, head=171, tw_head=-2), "smooth"),
    (78, G(hx=-0.006, fR=(-0.13, 40), head=173, tw_head=1, clL=(15, 11)), "smooth"),
    (90, GUARD, "smooth"),
], loop=True, breath=True, breaths=3, note="a live guard: breathing, weight rocking, rear heel bouncing, hands alive")

# ---------------------------------------------------------------- JAB (lead hand → chin)
# No wind-up: the fist leaves first, the elbow stays down behind it, the lead foot slides in as the
# rear foot pushes off its ball, the lead shoulder rolls up to cover the chin, the fist turns over in
# the last third. Snapped back on the same line, the step recovered, a tiny settle.
CLIPS["jab"] = clip(13, [
    (0, GUARD, "smooth"),
    (1, G(uaL=66, faL=140, haL=146, twL=-38, clL=(18, 10), tw_chest=-9, fR=(-0.13, 44), hx=0.012), "in"),
    (2, G(uaL=78, faL=118, haL=120, twL=-55, clL=(30, 12), chest=163, tw_chest=-18, spine_mid=170, tw_spine_mid=-12, tw_hips=-15,
          hx=0.05, fR=(-0.13, 50)), "in"),
    (3, G(uaL=86, faL=100, haL=100, twL=-78, clL=(44, 15), chest=159, tw_chest=-28, spine_mid=169, tw_spine_mid=-20, tw_hips=-17,
          neck=156, head=170, hx=0.09, fL=(0.36, 0), liftL=0.018, fR=(-0.13, 52)), "in"),
    (4, G(hL=(0.63, 1.07), twL=-92, clL=(56, 19), chest=156, tw_chest=-36, spine_mid=168, tw_spine_mid=-26, spine=175, tw_spine=-14,
          tw_hips=-18, neck=152, head=168, hx=0.12, fL=(0.36, 0), fR=(-0.12, 50), uaR=24, faR=168, haR=174), "in3"),
    (5, G(hL=(0.645, 1.07), twL=-94, clL=(58, 19), chest=156, tw_chest=-37, spine_mid=168, tw_spine_mid=-27, spine=175, tw_spine=-14,
          tw_hips=-18, neck=152, head=168, hx=0.122, fL=(0.36, 0), fR=(-0.12, 50), uaR=24, faR=168, haR=174), "lin"),
    (7, G(uaL=76, faL=124, haL=128, twL=-60, clL=(34, 14), chest=160, tw_chest=-24, tw_spine_mid=-16, tw_hips=-15,
          hx=0.1, fL=(0.36, 0), fR=(-0.12, 46)), "out"),
    (9, G(uaL=64, faL=146, haL=152, twL=-34, clL=(18, 11), chest=164, tw_chest=-10, hx=0.06, fL=(0.36, 0), fR=(-0.12, 40)), "out"),
    (11, G(uaL=61, faL=153, haL=158, twL=-28, hx=0.02, fL=(0.25, 0), liftL=0.015), "out"),
    (13, GUARD, "smooth"),
], hit=4, load=1, aim=("fistL", "chin"), reach=0.95, react="hit_front", striker="L",
    note="jab: fist first, foot slides in, shoulder covers the chin, fist turns over, snaps back")

# ---------------------------------------------------------------- CROSS (rear hand → chin)
# A short load (the weight settles into the back leg, the shoulders turn away a few degrees, the rear
# heel drops), then in order: the rear heel spins up and out, the hips drive round and over the front
# knee, the spine and then the chest follow, the rear shoulder is thrown forward, and the arm goes
# last, straight down the middle, turning over at the end. The lead fist comes home to the chin.
CLIPS["cross"] = clip(16, [
    (0, GUARD, "smooth"),
    (2, G(hx=-0.02, hz=0.632, tw_hips=-18, tw_spine=-14, tw_spine_mid=-12, tw_chest=-14, chest=168, fR=(-0.13, 24),
          uaR=18, faR=168, haR=174, clR=(-4, 6)), "smooth"),
    (3, G(hx=0.0, hz=0.632, tw_hips=0, tw_spine=-8, tw_spine_mid=-10, tw_chest=-12, chest=166, fR=(-0.13, 48),
          uaR=22, faR=164, haR=168, clR=(0, 6)), "in"),
    (4, G(hx=0.06, hz=0.63, tw_hips=18, tw_spine=10, tw_spine_mid=2, tw_chest=-2, chest=160, spine_mid=168, fR=(-0.13, 62),
          uaR=46, faR=146, haR=140, twR=40, clR=(14, 8), uaL=50, faL=160, haL=166), "in"),
    (5, G(hx=0.12, hz=0.628, tw_hips=28, tw_spine=24, tw_spine_mid=20, tw_chest=18, chest=154, spine_mid=165, spine=174,
          neck=152, head=166, fR=(-0.13, 70), uaR=72, faR=112, haR=112, twR=70, clR=(32, 12),
          uaL=40, faL=166, haL=172), "in"),
    (6, G(hR=(0.68, 1.08), hx=0.15, hz=0.626, tw_hips=32, tw_spine=32, tw_spine_mid=34, tw_chest=38, chest=149, spine_mid=163,
          spine=173, neck=148, head=164, fR=(-0.13, 74), twR=92, clR=(54, 16), uaL=38, faL=167, haL=173, fL=(0.24, 0)), "in3"),
    (7, G(hR=(0.695, 1.075), hx=0.155, hz=0.625, tw_hips=33, tw_spine=33, tw_spine_mid=35, tw_chest=40, chest=148, spine_mid=163,
          spine=173, neck=148, head=164, fR=(-0.13, 74), twR=94, clR=(57, 16), uaL=38, faL=167, haL=173), "lin"),
    (9, G(uaR=64, faR=128, haR=130, twR=60, clR=(30, 10), hx=0.12, tw_hips=22, tw_spine=20, tw_spine_mid=16, tw_chest=14,
          chest=155, fR=(-0.13, 66)), "out"),
    (12, G(uaR=26, faR=162, haR=166, twR=26, clR=(4, 7), hx=0.04, tw_hips=-4, tw_chest=-4, fR=(-0.13, 46)), "out"),
    (14, G(uaR=21, faR=168, haR=174, twR=18, hx=0.005, tw_chest=-8, fR=(-0.13, 36)), "out"),
    (16, GUARD, "smooth"),
], hit=6, load=2, aim=("fistR", "chin"), reach=1.0, react="hit_chin", striker="R",
    note="cross: rear heel spins, hips, spine, chest, shoulder, then the arm; fist turns over; lead fist home")



# ---------------------------------------------------------------- shared pieces
HANDS_UP = dict(uaL=50, faL=162, haL=168, twL=-20, uaR=22, faR=168, haR=174, twR=18)      # both fists home
LEAD_HOME = dict(uaL=44, faL=164, haL=170, twL=-18)
REAR_HOME = dict(uaR=22, faR=168, haR=174, twR=18)


# ---------------------------------------------------------------- LEAD HOOK (lead hand → jaw)
# The weight rocks onto the lead leg; the lead heel turns out (the foot pivots on its ball); hips,
# spine, chest turn through hard; the elbow comes up level with the fist, the forearm swings ACROSS
# (yaw) and the palm turns down; the rear fist glued to the cheek. A tiny dip loads it.
CLIPS["lead_hook"] = clip(17, [
    (0, GUARD, "smooth"),
    (2, G(hz=0.626, hx=0.02, tw_hips=-4, tw_spine=0, tw_chest=2, chest=165, uaL=58, faL=142, yw_uaL=16, yw_faL=10, clL=(8, 8)), "smooth"),
    (4, G(hz=0.628, hx=0.06, tw_hips=-20, tw_spine=-22, tw_spine_mid=-24, tw_chest=-22, chest=160, uaL=84, faL=120, yw_uaL=32, yw_faL=-12,
          twL=-60, clL=(22, 16), fL=(0.24, 18), fiL=170), "in"),
    (5, G(hz=0.63, hx=0.08, tw_hips=-32, tw_spine=-36, tw_spine_mid=-40, tw_chest=-42, chest=156, uaL=104, faL=110, haL=110,
          yw_uaL=30, yw_faL=-46, twL=-82, clL=(34, 22), fL=(0.24, 26), neck=154, head=166), "in"),
    (6, G(hz=0.63, hx=0.1, tw_hips=-40, tw_spine=-46, tw_spine_mid=-50, tw_chest=-54, chest=154, uaL=114, faL=110, haL=110,
          yw_uaL=22, yw_faL=-74, twL=-92, clL=(42, 26), fL=(0.24, 30), neck=152, head=164, **REAR_HOME), "in3"),
    (7, G(hz=0.63, hx=0.1, tw_hips=-43, tw_spine=-49, tw_spine_mid=-54, tw_chest=-58, chest=154, uaL=114, faL=110, haL=110,
          yw_uaL=12, yw_faL=-86, twL=-92, clL=(44, 26), fL=(0.24, 30), neck=152, head=164, **REAR_HOME), "lin"),
    (9, G(hz=0.632, hx=0.08, tw_hips=-30, tw_spine=-32, tw_chest=-36, chest=158, uaL=94, faL=126, yw_uaL=12, yw_faL=-50, twL=-60,
          clL=(26, 18), fL=(0.24, 18)), "out"),
    (12, G(hx=0.03, tw_hips=-16, tw_chest=-14, uaL=66, faL=148, yw_uaL=0, yw_faL=-14, twL=-34, clL=(16, 11), fL=(0.24, 4)), "out"),
    (14, G(uaL=61, faL=153, twL=-28, tw_chest=-5), "out"),
    (17, GUARD, "smooth"),
], hit=6, load=2, aim=("fistL", "chin"), reach=0.9, react="hit_chin", striker="L",
    note="lead hook: lead heel turns out, the body turns through, forearm comes across level")

# ---------------------------------------------------------------- HOOK (rear hand, the big one; also the enemies' haymaker)
CLIPS["hook"] = clip(21, [
    (0, GUARD, "smooth"),
    (3, G(hz=0.622, hx=-0.03, tw_hips=-28, tw_spine=-34, tw_spine_mid=-38, tw_chest=-40, chest=170, spine_mid=174,
          uaR=46, faR=150, haR=150, yw_uaR=-42, yw_faR=-12, twR=40, clR=(-14, 12), fR=(-0.13, 22)), "smooth"),
    (5, G(hz=0.622, hx=-0.03, tw_hips=-30, tw_spine=-36, tw_spine_mid=-40, tw_chest=-44, chest=171,
          uaR=50, faR=150, yw_uaR=-48, yw_faR=-16, twR=44, clR=(-18, 14), fR=(-0.13, 22)), "smooth"),
    (7, G(hz=0.626, hx=0.05, tw_hips=10, tw_spine=4, tw_spine_mid=-4, tw_chest=-10, chest=162, uaR=86, faR=128,
          yw_uaR=-46, yw_faR=24, twR=70, clR=(12, 18), fR=(-0.13, 60), **LEAD_HOME), "in"),
    (9, G(hR=None, hz=0.62, hx=0.14, tw_hips=40, tw_spine=46, tw_spine_mid=52, tw_chest=56, chest=150, spine_mid=163, neck=150, head=164,
          uaR=114, faR=110, haR=110, yw_uaR=-20, yw_faR=74, twR=92, clR=(44, 24), fR=(-0.13, 74), **LEAD_HOME), "in3"),
    (10, G(hz=0.618, hx=0.15, tw_hips=46, tw_spine=54, tw_spine_mid=60, tw_chest=66, chest=148, neck=150, head=164,
           uaR=114, faR=110, haR=110, yw_uaR=-6, yw_faR=88, twR=92, clR=(48, 24), fR=(-0.13, 76), **LEAD_HOME), "lin"),
    (12, G(hz=0.618, hx=0.15, tw_hips=50, tw_spine=58, tw_chest=70, chest=150, uaR=100, faR=112, yw_uaR=6, yw_faR=96, twR=80,
           clR=(36, 18), fR=(-0.12, 72)), "out"),                                  # the follow-through carries him round
    (15, G(hz=0.626, hx=0.09, tw_hips=24, tw_chest=26, chest=158, uaR=60, faR=140, yw_uaR=0, yw_faR=40, twR=50, clR=(14, 10),
           fR=(-0.12, 56)), "out"),
    (18, G(hx=0.02, tw_chest=-4, uaR=24, faR=166, yw_faR=6, twR=22, fR=(-0.13, 42)), "out"),
    (21, GUARD, "smooth"),
], hit=9, load=5, aim=("fistR", "chin"), reach=0.95, react="hit_chin", striker="R",
    note="rear hook: cocked wide, rear heel spins, the body whips round, carries through")

# ---------------------------------------------------------------- UPPERCUT (rear hand, under the chin)
CLIPS["uppercut"] = clip(20, [
    (0, GUARD, "smooth"),
    (2, G(hz=0.6, hx=0.03, spine=170, spine_mid=164, chest=156, neck=150, head=160, tw_hips=-14, tw_spine=-18, tw_chest=-22,
          uaR=8, faR=120, haR=130, twR=60, clR=(-6, -6), fL=(0.26, 0), fR=(-0.13, 26)), "smooth"),
    (4, G(hz=0.565, hx=0.06, spine=164, spine_mid=158, chest=150, neck=146, head=158, tw_hips=-18, tw_spine=-24, tw_chest=-28,
          uaR=-6, faR=98, haR=118, twR=88, clR=(-10, -10), fiR=170, fL=(0.27, 0), fR=(-0.13, 24)), "smooth"),
    (5, G(hz=0.585, hx=0.09, spine=170, spine_mid=166, chest=160, tw_hips=4, tw_spine=0, tw_chest=-4,
          uaR=24, faR=132, haR=146, twR=88, clR=(4, 0), fR=(-0.13, 56)), "in"),
    (6, G(hR=(0.72, 0.95), hz=0.645, hx=0.14, spine=178, spine_mid=176, chest=174, neck=166, head=170, tw_hips=28, tw_spine=30,
          tw_spine_mid=34, tw_chest=38, twR=88, clR=(30, 20), fL=(0.27, 0), fR=(-0.13, 72), **LEAD_HOME), "in3"),
    (7, G(hR=(0.74, 1.02), hz=0.665, hx=0.15, spine=180, spine_mid=180, chest=178, neck=170, head=172, tw_hips=31, tw_spine=34,
          tw_spine_mid=38, tw_chest=42, twR=88, clR=(32, 28), fL=(0.27, 12), fR=(-0.13, 80), **LEAD_HOME), "lin"),
    (9, G(hz=0.66, hx=0.13, spine=178, chest=174, uaR=74, faR=172, haR=176, twR=70, clR=(18, 22), tw_chest=26, fL=(0.27, 6),
          fR=(-0.13, 66)), "out"),
    (13, G(hz=0.645, hx=0.06, uaR=30, faR=166, twR=30, clR=(4, 9), tw_chest=-2, fL=(0.25, 0), fR=(-0.13, 46)), "out"),
    (16, G(uaR=21, faR=169, twR=18, tw_chest=-7), "out"),
    (20, GUARD, "smooth"),
], hit=6, load=4, aim=("fistR", "under_chin"), reach=0.95, react="hit_up", striker="R",
    note="uppercut: dip, palm up at the hip, legs drive up, hips turn, the fist rises up the middle")

# ---------------------------------------------------------------- ELBOW (rear horizontal elbow, close)
CLIPS["elbow"] = clip(17, [
    (0, GUARD, "smooth"),
    (2, G(tw_hips=-20, tw_spine=-24, tw_chest=-28, hx=0.0, uaR=40, faR=170, haR=176, clR=(-8, 10), uaL=70, faL=112, yw_faL=-20,
          fiL=60, clL=(22, 10)), "smooth"),                                      # the lead hand opens to frame his shoulder
    (4, G(tw_hips=10, tw_spine=6, tw_chest=4, hx=0.04, chest=160, uaR=96, faR=182, haR=186, yw_uaR=-50, yw_faR=-10, clR=(10, 22),
          uaL=84, faL=96, fiL=60, fR=(-0.13, 60)), "in"),
    (5, G(tw_hips=38, tw_spine=44, tw_spine_mid=50, tw_chest=56, hx=0.065, chest=152, spine_mid=164, neck=148, head=162,
          uaR=118, faR=200, haR=200, yw_uaR=10, yw_faR=60, twR=60, clR=(40, 28), uaL=52, faL=150, fiL=120, fR=(-0.13, 72)), "in3"),
    (6, G(tw_hips=42, tw_spine=48, tw_spine_mid=55, tw_chest=62, hx=0.07, chest=152, neck=148, head=162,
          uaR=118, faR=202, haR=202, yw_uaR=22, yw_faR=72, twR=60, clR=(44, 28), uaL=50, faL=152, fiL=150, fR=(-0.13, 74)), "lin"),
    (9, G(tw_hips=24, tw_chest=26, hx=0.05, uaR=70, faR=176, yw_uaR=10, yw_faR=30, clR=(16, 14), fR=(-0.13, 58)), "out"),
    (13, G(uaR=26, faR=168, clR=(2, 7), tw_chest=-4, fR=(-0.13, 42)), "out"),
    (17, GUARD, "smooth"),
], hit=5, load=2, aim=("elbowR", "chin"), reach=0.75, react="hit_chin", striker="R",
    note="rear elbow: up level, the whole turning body behind it; the lead hand frames his shoulder")

# ---------------------------------------------------------------- BACK FIST (lead, a whip of the forearm)
CLIPS["back_fist"] = clip(14, [
    (0, GUARD, "smooth"),
    (2, G(uaL=88, faL=196, haL=206, twL=0, clL=(20, 18), tw_chest=-2, chest=166), "smooth"),     # the elbow up, the fist cocked back by the ear
    (3, G(uaL=94, faL=170, haL=170, twL=-10, clL=(30, 20), tw_chest=-16, hx=0.03), "in"),
    (4, G(uaL=96, faL=104, haL=96, twL=0, clL=(44, 20), tw_chest=-28, tw_hips=-16, hx=0.07, neck=154, head=168, **REAR_HOME), "in3"),
    (5, G(uaL=96, faL=100, haL=88, twL=0, clL=(45, 20), tw_chest=-29, tw_hips=-16, hx=0.07, neck=154, head=168, **REAR_HOME), "lin"),
    (7, G(uaL=86, faL=150, haL=160, clL=(28, 16), tw_chest=-18, hx=0.05), "out"),       # it whips back the way it came
    (10, G(uaL=64, faL=152, clL=(16, 11), hx=0.02), "out"),
    (14, GUARD, "smooth"),
], hit=4, load=2, aim=("fistL", "chin"), reach=1.05, react="hit_front", striker="L",
    note="back fist: the elbow points, the forearm whips out and back like a towel")

# ---------------------------------------------------------------- PALM STRIKE (rear palm heel up under the chin)
CLIPS["palm_strike"] = clip(18, [
    (0, GUARD, "smooth"),
    (3, G(hz=0.61, hx=0.02, tw_hips=-14, tw_chest=-20, uaR=14, faR=120, haR=180, fiR=20, twR=60, clR=(-6, 2), fR=(-0.13, 28)), "smooth"),
    (5, G(hz=0.62, hx=0.08, tw_hips=14, tw_chest=10, chest=162, uaR=56, faR=130, haR=210, fiR=10, twR=80, clR=(16, 12), fR=(-0.13, 62)), "in"),
    (6, G(hR=(0.73, 1.0), hz=0.63, hx=0.15, tw_hips=32, tw_spine=34, tw_chest=40, chest=152, haR=212, fiR=10, twR=90, clR=(50, 22),
          neck=150, head=166, fR=(-0.13, 74), **LEAD_HOME), "in3"),
    (7, G(hR=(0.75, 1.03), hz=0.635, hx=0.155, tw_hips=33, tw_spine=35, tw_chest=42, chest=151, haR=214, fiR=10, twR=90, clR=(52, 24),
          neck=150, head=166, fR=(-0.13, 74), **LEAD_HOME), "lin"),
    (10, G(hx=0.1, uaR=60, faR=140, haR=170, fiR=90, twR=50, clR=(20, 12), tw_chest=18, fR=(-0.13, 60)), "out"),
    (14, G(hx=0.03, uaR=24, faR=166, fiR=160, twR=22, clR=(2, 7), fR=(-0.13, 44)), "out"),
    (18, GUARD, "smooth"),
], hit=6, load=3, aim=("fistR", "under_chin"), reach=0.95, react="stumble_back", striker="R",
    note="palm heel: the hand opens, the wrist bends back, driven up under the chin")

# ---------------------------------------------------------------- OI ZUKI (stepping lunge punch, lead hand)
CLIPS["oi_zuki"] = clip(22, [
    (0, GUARD, "smooth"),
    (3, G(hz=0.61, hx=0.02, chest=168, tw_chest=-10, fR=(-0.13, 44), uaL=56, faL=150), "smooth"),
    (6, G(hz=0.6, hx=0.16, chest=164, tw_hips=-20, tw_chest=-24, uaL=74, faL=126, twL=-50, clL=(24, 12), fL=(0.44, 0), liftL=0.03,
          fR=(-0.1, 50)), "in"),
    (8, G(hL=(0.86, 1.02), hz=0.595, hx=0.26, chest=158, tw_hips=-26, tw_spine=-28, tw_chest=-36, twL=-92, clL=(54, 18), neck=152,
          head=166, fL=(0.48, 0), fR=(-0.06, 56), **REAR_HOME), "in3"),
    (9, G(hL=(0.875, 1.02), hz=0.595, hx=0.265, chest=158, tw_hips=-27, tw_spine=-29, tw_chest=-37, twL=-92, clL=(56, 18), neck=152,
          head=166, fL=(0.48, 0), fR=(-0.06, 56), **REAR_HOME), "lin"),
    (12, G(hz=0.61, hx=0.24, uaL=72, faL=130, twL=-50, clL=(26, 12), tw_chest=-22, fL=(0.48, 0), fR=(-0.06, 46)), "out"),
    (16, G(hx=0.1, fL=(0.36, 0), liftL=0.03, fR=(-0.1, 40)), "out"),
    (22, GUARD, "smooth"),
], hit=8, load=3, aim=("fistL", "chin"), reach=1.15, react="hit_chin", striker="L",
    note="lunge punch: a long step in, the fist arriving as the foot lands, then back out")

# ---------------------------------------------------------------- GYAKU ZUKI (reverse punch to the body, planted)
CLIPS["gyaku_zuki"] = clip(18, [
    (0, GUARD, "smooth"),
    (2, G(hz=0.6, hx=0.0, tw_hips=-20, tw_chest=-22, uaR=4, faR=120, haR=126, twR=0, clR=(-8, 2), fR=(-0.13, 20)), "smooth"),
    (4, G(hz=0.595, hx=0.07, tw_hips=12, tw_chest=0, uaR=40, faR=118, twR=50, clR=(10, 6), fR=(-0.13, 62), uaL=30, faL=150, haL=150), "in"),
    (5, G(hR=(0.74, 0.86), hz=0.59, hx=0.14, tw_hips=34, tw_spine=36, tw_spine_mid=38, tw_chest=40, chest=154, neck=150, head=166,
          twR=92, clR=(52, 8), fR=(-0.13, 74), uaL=22, faL=140, haL=140), "in3"),               # the lead hand pulls back to the hip (hikite)
    (6, G(hR=(0.755, 0.86), hz=0.59, hx=0.145, tw_hips=35, tw_spine=37, tw_spine_mid=39, tw_chest=42, chest=154, neck=150, head=166,
          twR=92, clR=(54, 8), fR=(-0.13, 74), uaL=20, faL=138, haL=138), "lin"),
    (9, G(hz=0.605, hx=0.1, uaR=50, faR=120, twR=60, clR=(20, 6), tw_chest=18, fR=(-0.13, 60)), "out"),
    (13, G(hx=0.03, uaR=24, faR=164, twR=24, clR=(2, 6), tw_chest=-4, fR=(-0.13, 44)), "out"),
    (18, GUARD, "smooth"),
], hit=5, load=2, aim=("fistR", "body"), reach=1.0, react="hit_body", striker="R",
    note="reverse punch: rooted, hips snap round, the other hand pulls back to the hip")

# ---------------------------------------------------------------- LOW KICK (rear shin into his thigh)
CLIPS["low_kick"] = clip(22, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.08, hz=0.63, fL=(0.36, 0), liftL=0.03, tw_hips=-6, tw_chest=-12, chest=168), "smooth"),     # step out and turn the lead toes
    (5, G(hx=0.14, hz=0.64, fL=(0.37, 28), yw_ftL=-30, tw_hips=18, tw_spine=8, tw_chest=0, spine=182, chest=184, head=174,
          thR=24, shR=-26, yw_thR=-52, yw_shR=-44, fpR=40, uaR=-22, faR=40, clR=(-10, 4), **LEAD_HOME), "in"),
    (7, G(hx=0.18, hz=0.645, fL=(0.37, 34), yw_ftL=-50, tw_hips=56, tw_spine=50, tw_spine_mid=40, tw_chest=30, spine=192, spine_mid=194,
          chest=198, neck=186, head=180, thR=62, shR=68, yw_thR=-6, yw_shR=10, fpR=50, uaR=-44, faR=-18, haR=-18, clR=(-20, -4),
          **LEAD_HOME), "in3"),
    (8, G(hx=0.18, hz=0.645, fL=(0.37, 34), yw_ftL=-52, tw_hips=60, tw_spine=54, tw_chest=34, spine=193, chest=199, neck=186, head=180,
          thR=62, shR=72, yw_thR=4, yw_shR=22, fpR=50, uaR=-46, faR=-22, haR=-22, clR=(-22, -4), **LEAD_HOME), "lin"),
    (11, G(hx=0.15, hz=0.64, fL=(0.37, 20), yw_ftL=-30, tw_hips=26, tw_chest=8, spine=186, chest=186, thR=24, shR=-34, yw_thR=-24,
           yw_shR=-20, fpR=30, uaR=-6, faR=110), "out"),                          # the leg rebounds off him
    (14, G(hx=0.1, fL=(0.36, 0), yw_ftL=0, fR=(-0.04, 30), tw_chest=-12), "out"),
    (18, G(hx=0.04, fL=(0.26, 0), liftL=0.025, fR=(-0.12, 36)), "out"),
    (22, GUARD, "smooth"),
], hit=7, load=3, aim=("shinR", "thigh"), reach=0.95, react="hit_legs", striker=None,
    note="low kick: step out, lead foot pivots, hips turn over, the shin swings round into his thigh")

# ---------------------------------------------------------------- MAWASHI GERI (rear roundhouse to the ribs)
CLIPS["mawashi_geri"] = clip(26, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.04, hz=0.63, tw_hips=-2, tw_chest=-10, spine=176, chest=172, fL=(0.25, 6)), "smooth"),
    (6, G(hx=0.08, hz=0.645, tw_hips=26, tw_chest=0, spine=186, chest=186, head=178, fL=(0.25, 30), yw_ftL=-40,
          thR=96, shR=-8, yw_thR=-52, yw_shR=-60, fpR=40, uaR=10, faR=124, uaL=62, faL=154), "in"),
    (8, G(hx=0.1, hz=0.655, tw_hips=60, tw_chest=24, spine=200, chest=208, neck=196, head=190, fL=(0.25, 38), yw_ftL=-80,
          thR=104, shR=58, yw_thR=-26, yw_shR=-36, fpR=55, uaR=-36, faR=-10, uaL=70, faL=150), "in"),
    (9, G(hx=0.12, hz=0.66, tw_hips=78, tw_spine=60, tw_chest=34, spine=206, chest=216, neck=200, head=196, fL=(0.25, 40), yw_ftL=-100,
          thR=116, shR=118, yw_thR=-4, yw_shR=4, fpR=60, uaR=-48, faR=-20, haR=-20, clR=(-24, -2), uaL=76, faL=150), "in3"),
    (10, G(hx=0.12, hz=0.66, tw_hips=82, tw_spine=64, tw_chest=38, spine=207, chest=217, neck=200, head=196, fL=(0.25, 40), yw_ftL=-104,
           thR=116, shR=120, yw_thR=4, yw_shR=16, fpR=60, uaR=-50, faR=-22, haR=-22, clR=(-24, -2), uaL=76, faL=150), "lin"),
    (13, G(hx=0.1, hz=0.65, tw_hips=50, tw_chest=20, spine=196, chest=200, head=188, fL=(0.25, 30), yw_ftL=-60,
           thR=98, shR=-6, yw_thR=-20, yw_shR=-30, fpR=40, uaR=-10, faR=100), "out"),
    (17, G(hx=0.05, tw_hips=4, tw_chest=-8, spine=178, chest=174, fL=(0.25, 8), yw_ftL=-10, thR=24, shR=-40, fpR=30, uaR=16, faR=160), "out"),
    (21, G(hx=0.02, fR=(-0.12, 30), liftR=0.03, fL=(0.25, 0), yw_ftL=0), "smooth"),
    (26, GUARD, "smooth"),
], hit=9, load=6, aim=("shinR", "ribs"), reach=0.95, react="hit_body", striker=None,
    note="roundhouse: chamber, the standing foot pivots on its ball, lean away, the shin snaps round")

# ---------------------------------------------------------------- MAE GERI (front push kick)
CLIPS["mae_geri"] = clip(22, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.03, spine=178, chest=176, tw_hips=0, tw_chest=-10, thR=60, shR=-24, fpR=20, uaR=26, faR=164), "smooth"),
    (5, G(hx=0.06, hz=0.65, spine=184, chest=186, head=176, tw_hips=10, thR=106, shR=8, fpR=-10, tpR=-30, fL=(0.25, 8), uaL=58, faL=152), "in"),
    (7, G(hx=0.14, hz=0.66, spine=196, chest=200, head=184, tw_hips=24, thR=108, shR=104, fpR=-20, tpR=-40, fL=(0.25, 14),
          uaR=-14, faR=60, uaL=56, faL=150), "in3"),                               # the heel drives, the toes pulled back
    (8, G(hx=0.16, hz=0.66, spine=198, chest=202, head=185, tw_hips=26, thR=106, shR=106, fpR=-20, tpR=-40, fL=(0.25, 14),
          uaR=-16, faR=56, uaL=56, faL=150), "lin"),
    (11, G(hx=0.08, hz=0.65, spine=186, chest=188, tw_hips=10, thR=104, shR=4, fpR=10, fL=(0.25, 6)), "out"),
    (15, G(hx=0.03, thR=10, shR=-30, fpR=30, tw_hips=-6, tw_chest=-14), "out"),
    (18, G(fR=(-0.13, 30), liftR=0.03), "smooth"),
    (22, GUARD, "smooth"),
], hit=7, load=5, aim=("shinR", "body"), reach=0.95, react="hit_shoved", striker=None,
    note="push kick: knee to the chest, the heel driven through the belly, recoil before it lands")

# ---------------------------------------------------------------- SIDE KICK (lead leg, the hips turned side-on, a driving heel)
CLIPS["side_kick"] = clip(24, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.04, fR=(0.02, 30), liftR=0.03, tw_hips=-20, tw_chest=-16), "smooth"),            # the rear foot steps up behind (a skip)
    (6, G(hx=0.06, hz=0.66, fR=(0.02, 0), yw_ftR=60, tw_hips=-50, tw_chest=-40, spine=190, chest=196, head=184,
          thL=100, shL=-10, fpL=-10, uaL=60, faL=110, uaR=30, faR=150), "in"),             # chamber: knee to the chest, the foot turned
    (8, G(hx=0.12, hz=0.665, fR=(0.02, 10), yw_ftR=90, tw_hips=-70, tw_chest=-60, spine=208, chest=216, neck=206, head=198,
          thL=106, shL=104, fpL=-20, tpL=-30, uaL=40, faL=60, uaR=-30, faR=10), "in3"),    # driven out flat, the body leaning back over the hip
    (9, G(hx=0.13, hz=0.665, fR=(0.02, 10), yw_ftR=90, tw_hips=-72, tw_chest=-62, spine=210, chest=218, neck=206, head=198,
          thL=106, shL=106, fpL=-20, tpL=-30, uaL=40, faL=60, uaR=-30, faR=10), "lin"),
    (12, G(hx=0.08, hz=0.66, fR=(0.02, 4), yw_ftR=60, tw_hips=-50, tw_chest=-40, spine=192, chest=198, thL=100, shL=-6, fpL=0), "out"),
    (16, G(hx=0.04, fL=(0.26, 0), fR=(0.02, 10), yw_ftR=20, tw_hips=-20, tw_chest=-14), "out"),
    (20, G(fR=(-0.13, 38), liftR=0.03), "smooth"),
    (24, GUARD, "smooth"),
], hit=8, load=6, aim=("shinL", "body"), reach=1.0, react="stumble_back", striker=None,
    note="side kick: skip up, chamber, the heel driven out flat with the hips side-on")

# ---------------------------------------------------------------- BACK KICK (behind him: a donkey kick without turning round)
CLIPS["back_kick"] = clip(20, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.02, spine=164, chest=154, neck=150, head=150, fR=(-0.13, 20), thR=40, shR=-60, fpR=20), "smooth"),   # a look back over the shoulder
    (6, G(hx=0.05, hz=0.62, spine=140, spine_mid=134, chest=128, neck=124, head=130, tw_head=40, thR=-70, shR=-84, fpR=-20, tpR=-20,
          uaL=40, faL=120, uaR=60, faR=140), "in3"),
    (7, G(hx=0.05, hz=0.62, spine=138, spine_mid=132, chest=126, neck=124, head=130, tw_head=40, thR=-74, shR=-86, fpR=-20, tpR=-20,
          uaL=40, faL=120, uaR=60, faR=140), "lin"),
    (11, G(hx=0.03, spine=160, chest=152, head=160, tw_head=20, thR=30, shR=-60, fpR=20), "out"),
    (15, G(fR=(-0.13, 30), liftR=0.02), "out"),
    (20, GUARD, "smooth"),
], hit=6, load=3, aim=None, reach=None, striker=None, note="back kick: a look over the shoulder, folding forward, the heel driven straight back")

# ---------------------------------------------------------------- HAMMER FIST (down on a man bent double)
CLIPS["hammer_fist"] = clip(19, [
    (0, GUARD, "smooth"),
    (4, G(hx=0.03, hz=0.665, spine=184, spine_mid=186, chest=190, neck=180, head=176, uaR=170, faR=200, haR=210, twR=0, clR=(4, 40),
          uaL=70, faL=130, tw_hips=-4, tw_chest=-6, fR=(-0.13, 50)), "smooth"),            # up on the toes, the fist high, the shoulder shrugged
    (7, G(hx=0.11, hz=0.58, spine=152, spine_mid=144, chest=134, neck=130, head=146, uaR=96, faR=110, haR=120, clR=(30, -10),
          uaL=50, faL=110, tw_hips=26, tw_chest=30, fR=(-0.13, 62), dip=0.02), "in3"),
    (8, G(hx=0.115, hz=0.575, spine=150, spine_mid=142, chest=132, neck=128, head=146, uaR=88, faR=96, haR=104, clR=(32, -12),
          uaL=50, faL=110, tw_hips=26, tw_chest=30, fR=(-0.13, 62)), "lin"),
    (12, G(hx=0.07, hz=0.62, spine=166, chest=156, uaR=40, faR=150, clR=(8, 4), tw_hips=6, fR=(-0.13, 46)), "out"),
    (19, GUARD, "smooth"),
], hit=7, load=4, aim=("fistR", "back"), reach=0.9, react="hit_body", striker="R",
    note="hammer fist: up on the toes, the shoulder shrugged, the whole body dropping with it")

# ---------------------------------------------------------------- KNEE (rising rear knee, hands pulling his head down)
CLIPS["knee"] = clip(18, [
    (0, GUARD, "smooth"),
    (3, G(hx=0.06, spine=168, chest=160, head=164, uaL=96, faL=120, uaR=86, faR=118, fiL=90, fiR=90, clL=(30, 16), clR=(30, 16),
          tw_chest=0, tw_hips=0), "smooth"),
    (5, G(hx=0.12, hz=0.66, spine=176, chest=170, head=166, uaL=86, faL=70, uaR=80, faR=66, fiL=110, fiR=110, clL=(20, 10), clR=(20, 10),
          tw_hips=12, thR=60, shR=-40, fpR=30, fL=(0.25, 18)), "in"),
    (6, G(hx=0.16, hz=0.675, spine=186, chest=176, head=168, uaL=76, faL=50, uaR=70, faR=46, fiL=120, fiR=120, tw_hips=20,
          thR=118, shR=-4, fpR=40, fL=(0.25, 40)), "in3"),
    (7, G(hx=0.17, hz=0.675, spine=188, chest=178, head=168, uaL=74, faL=46, uaR=68, faR=42, fiL=120, fiR=120, tw_hips=22,
          thR=122, shR=-2, fpR=40, fL=(0.25, 42)), "lin"),
    (10, G(hx=0.1, hz=0.65, spine=176, chest=168, uaL=70, faL=130, uaR=40, faR=150, thR=40, shR=-40, fpR=30, fL=(0.25, 10)), "out"),
    (13, G(fR=(-0.13, 34), liftR=0.03), "out"),
    (18, GUARD, "smooth"),
], hit=6, load=3, aim=("kneeR", "body"), reach=0.75, react="hit_body", striker=None,
    note="rising knee: hands cup his head and pull it down, up on the toes, the knee drives up")

# ---------------------------------------------------------------- PARRY, SLIP and the guard
CLIPS["parry"] = clip(11, [
    (0, GUARD, "lin"),
    (2, G(hz=0.61, head=164, chest=162, tw_chest=-26, uaL=78, faL=150, haL=160, yw_faL=-30, fiL=40, twL=-20, clL=(24, 12), **REAR_HOME), "in"),
    (3, G(hz=0.61, hx=-0.02, head=160, tw_head=10, chest=160, tw_chest=-34, uaL=60, faL=100, haL=80, yw_faL=-62, fiL=30, twL=-40,
          clL=(30, 10), **REAR_HOME), "in3"),
    (6, G(hz=0.62, head=166, tw_chest=-26, uaL=50, faL=130, haL=140, yw_faL=-20, fiL=120, clL=(18, 10)), "out"),
    (11, GUARD, "smooth"),
], hit=3, striker="L", note="parry: the open lead hand slaps the punch across and down, the head slips the other way")

CLIPS["slip"] = clip(16, [
    (0, GUARD, "smooth"),
    (3, G(hz=0.6, hx=-0.03, spine=168, spine_mid=162, chest=156, neck=150, head=160, tw_hips=-20, tw_spine=-24, tw_chest=-30, tw_head=-10,
          fR=(-0.13, 20)), "in"),                                                   # the head off the line, the weight onto the back leg
    (6, G(hz=0.605, hx=-0.08, spine=170, chest=160, head=164, tw_chest=-26, fR=(-0.2, 22), liftR=0.02), "out"),
    (10, G(hz=0.62, hx=-0.05, tw_chest=-14, fR=(-0.18, 32)), "out"),
    (16, GUARD, "smooth"),
], note="slip: the head off the line first, a half step back, the guard kept (the game carries him back)")

GUARD_UP = G(hz=0.6, spine=168, spine_mid=164, chest=158, neck=148, head=154, tw_hips=-6, tw_chest=-8,
             uaL=40, faL=166, haL=172, uaR=34, faR=170, haR=176, yw_uaL=-8, yw_uaR=8, twL=0, twR=0, clL=(16, 26), clR=(16, 24))
CLIPS["block_high"] = clip(6, [(0, GUARD, "lin"), (2, P(GUARD_UP, hz=0.605, head=160), "in"), (6, GUARD_UP, "out")],
                           note="guard up: forearms together in front of the face, shoulders up, chin down, knees sink")
CLIPS["block_hold"] = clip(40, [
    (0, GUARD_UP, "smooth"), (12, P(GUARD_UP, hz=0.604, chest=160, head=157, uaL=42, faL=164), "smooth"),
    (20, P(GUARD_UP, hz=0.598, chest=157, head=152), "smooth"), (30, P(GUARD_UP, hz=0.603, chest=159, head=156, uaR=36, faR=168), "smooth"),
    (40, GUARD_UP, "smooth")], loop=True, breath=True, breaths=4, note="held guard: quick breaths, peeking between the forearms")
CLIPS["block_hit"] = clip(12, [
    (0, GUARD_UP, "lin"),
    (1, P(GUARD_UP, hx=-0.04, spine=174, chest=168, head=166, uaL=30, faL=172, uaR=26, faR=176, clL=(4, 30), clR=(4, 28)), "out3"),
    (3, P(GUARD_UP, hx=-0.07, spine=176, chest=172, head=170, fR=(-0.13, 14)), "out"),
    (7, P(GUARD_UP, hx=-0.03, chest=162, head=156), "out"),
    (12, GUARD_UP, "smooth"),
], note="a blow on the forearms: the arms driven into the head, rocked onto the back foot, re-set")

# the enemies' wind-up (the classic one before any strike): the weight back, the rear hand cocked
CLIPS["windup"] = clip(10, [
    (0, GUARD, "smooth"),
    (5, G(hx=-0.04, hz=0.63, tw_hips=-22, tw_chest=-30, chest=170, uaR=40, faR=170, haR=176, yw_uaR=-30, clR=(-14, 14),
          uaL=70, faL=140, fR=(-0.13, 20)), "smooth"),
    (10, G(hx=-0.05, hz=0.628, tw_hips=-24, tw_chest=-33, chest=171, uaR=44, faR=172, yw_uaR=-34, clR=(-16, 16), uaL=72, faL=138,
           fR=(-0.13, 18)), "smooth"),
], striker="R", note="the wind-up: weight back onto the rear foot, the shoulders turned away, the right cocked")

# ---------------------------------------------------------------- REACTIONS (frame 0 = the impact)
# The head first (it was hit), then the neck and chest a frame later, the arms flung by it, the feet
# last (a step back to catch the weight), then back to a guard that is a little lower and slower.
REC = G(head=166, chest=162, uaL=56, faL=150, uaR=20, faR=160, clL=(10, 6), clR=(0, 3))
CLIPS["hit_front"] = clip(16, [
    (0, GUARD, "lin"),
    (1, G(head=200, neck=176, chest=170, spine_mid=172, hx=-0.01, uaL=52, faL=136, uaR=18, faR=152, fiL=130, fiR=130), "out3"),
    (3, G(head=206, neck=184, chest=178, spine_mid=176, spine=178, hx=-0.05, uaL=46, faL=126, uaR=12, faR=140, fL=(0.18, 0), liftL=0.012), "out"),
    (8, G(head=182, neck=168, chest=170, hx=-0.04, fL=(0.18, 0)), "smooth"),
    (16, REC, "smooth"),
], lag=True, note="jabbed: the head snaps back, the body follows a beat late, half a step")
CLIPS["hit_chin"] = clip(22, [
    (0, GUARD, "lin"),
    (1, G(head=218, tw_head=20, neck=184, chest=178, spine_mid=176, hx=-0.02, uaL=48, faL=122, uaR=10, faR=132, tw_chest=8, fiL=120, fiR=120), "out3"),
    (3, G(head=228, tw_head=28, neck=196, chest=192, spine_mid=186, spine=184, hx=-0.08, hz=0.63, uaL=40, faL=96, uaR=0, faR=110,
          tw_chest=22, tw_hips=10, clL=(-10, 20), clR=(-10, 20), fL=(0.16, 20)), "out"),
    (6, G(head=212, tw_head=18, neck=190, chest=188, spine=182, hx=-0.15, uaL=30, faL=70, uaR=-6, faR=60, tw_chest=14,
          fL=(0.06, 0), liftL=0.03, fR=(-0.3, 30), liftR=0.03), "out"),
    (12, G(head=180, neck=168, chest=170, hx=-0.16, uaL=50, faL=130, uaR=16, faR=150, fL=(0.06, 0), fR=(-0.31, 34)), "smooth"),
    (22, REC, "smooth"),
], note="rocked: the head turned and thrown back, arms flung, two steps to catch the weight")
CLIPS["hit_up"] = clip(22, [
    (0, GUARD, "lin"),
    (1, G(head=232, neck=200, chest=190, spine=184, hz=0.67, uaL=70, faL=120, uaR=40, faR=110, fL=(0.24, 30), fR=(-0.13, 60)), "out3"),
    (4, G(head=236, neck=208, chest=204, spine=192, hz=0.7, hx=-0.06, uaL=90, faL=140, uaR=60, faR=130, fiL=40, fiR=40,
          fL=(0.22, 0.12, 30), fR=(-0.15, 0.12, 40)), "out"),
    (8, G(head=210, neck=194, chest=196, spine=188, hz=0.63, hx=-0.14, uaL=40, faL=80, uaR=10, faR=60, fL=(0.1, 0), fR=(-0.27, 30)), "in"),
    (14, G(head=180, chest=172, hz=0.62, hx=-0.16, fL=(0.08, 0), fR=(-0.29, 34)), "out"),
    (22, REC, "smooth"),
], note="uppercut: lifted off his heels, the head thrown up, the hands opening, dropping back")
CLIPS["hit_body"] = clip(24, [
    (0, GUARD, "lin"),
    (1, G(spine=166, spine_mid=156, chest=146, neck=146, head=152, hz=0.625, hx=-0.02, uaL=24, faL=110, uaR=12, faR=104), "out3"),
    (4, G(spine=152, spine_mid=140, chest=126, neck=126, head=138, hz=0.56, hx=-0.06, uaL=10, faL=96, uaR=4, faR=90,
          clL=(26, -6), clR=(26, -6), fiL=120, fiR=120, fL=(0.2, 0), fR=(-0.15, 24)), "out"),
    (12, G(spine=154, spine_mid=142, chest=128, neck=128, head=140, hz=0.56, hx=-0.06, uaL=12, faL=100, uaR=6, faR=94,
           clL=(24, -4), clR=(24, -4), fL=(0.2, 0), fR=(-0.15, 24)), "smooth"),
    (24, REC, "smooth"),
], breath=True, breaths=3, note="to the body: folds round it, the shoulders round, arms in, breath gone")
CLIPS["hit_legs"] = clip(18, [
    (0, GUARD, "lin"),
    (1, G(fL=(0.3, 0.1, -10), hz=0.6, spine=176, chest=172, head=176, uaL=50, faL=120), "out3"),
    (4, G(fL=(0.16, 30), hz=0.5, spine=168, spine_mid=164, chest=160, head=158, uaL=30, faL=100, uaR=40, faR=100, tw_hips=10, fiL=60), "out"),
    (10, G(fL=(0.2, 6), hz=0.56, spine=170, chest=164, head=164), "smooth"),
    (18, GUARD, "smooth"),
], note="the kicked leg swept out from under him, buckles, a hand drops to it")
CLIPS["hit_shoved"] = clip(20, [
    (0, GUARD, "lin"),
    (1, G(spine=160, spine_mid=150, chest=140, head=150, hz=0.62, hx=-0.06, uaL=70, faL=90, uaR=60, faR=80, fiL=40, fiR=40), "out3"),
    (5, G(spine=162, chest=148, head=156, hx=-0.22, uaL=80, faL=110, uaR=70, faR=100, fL=(0.0, 0), fR=(-0.38, 20), liftL=0.05, liftR=0.02), "out"),
    (10, G(hx=-0.24, spine=168, chest=160, fL=(0.0, 0), fR=(-0.38, 30)), "out"),
    (20, REC, "smooth"),
], note="kicked in the belly: folded and driven back, arms flung forward")
CLIPS["stumble_back"] = clip(30, [
    (0, GUARD, "lin"),
    (1, G(head=210, neck=190, chest=184, hx=-0.04, uaL=80, faL=110, uaR=60, faR=100, fiL=40, fiR=40), "out3"),
    (5, G(head=214, neck=194, chest=192, spine=186, hx=-0.2, hz=0.62, uaL=110, faL=140, uaR=90, faR=130, fL=(0.04, 0), liftL=0.05, fR=(-0.36, 20)), "out"),
    (11, G(head=196, chest=180, hx=-0.34, fL=(-0.12, 0), liftL=0.04, fR=(-0.5, 24), liftR=0.03), "out"),
    (17, G(head=180, chest=170, hx=-0.4, fL=(-0.16, 0), fR=(-0.53, 34)), "out"),
    (24, G(hx=-0.2, fL=(0.04, 0), fR=(-0.33, 34), liftL=0.03, liftR=0.03), "smooth"),
    (30, REC, "smooth"),
], note="rocked: three staggering steps back, arms out for balance")
CLIPS["hit_back"] = clip(14, [
    (0, GUARD, "lin"),
    (1, G(spine=186, spine_mid=190, chest=194, neck=176, head=170, hx=0.03, uaL=30, faL=60, uaR=-20, faR=20), "out3"),
    (4, G(spine=180, chest=184, head=168, hx=0.06, fL=(0.32, 0), liftL=0.02), "out"),
    (14, REC, "smooth"),
], note="hit from behind: arched forward, a stumble step")
DAZED = G(hz=0.62, spine=174, chest=170, head=160, neck=164, uaL=24, faL=70, haL=60, uaR=12, faR=40, haR=30, fiL=40, fiR=40,
          clL=(0, -6), clR=(0, -6), fR=(-0.13, 10))
CLIPS["dazed"] = clip(48, [
    (0, DAZED, "smooth"),
    (12, P(DAZED, hx=0.02, spine=178, chest=176, head=172, tw_head=10, fR=(-0.13, 20)), "smooth"),
    (24, P(DAZED, hx=-0.01, spine=172, chest=166, head=156, tw_head=-6), "smooth"),
    (36, P(DAZED, hx=0.015, spine=180, chest=178, head=176, tw_head=12, fL=(0.24, 14)), "smooth"),
    (48, DAZED, "smooth"),
], loop=True, breath=True, breaths=3, note="out on his feet: hands low and open, the head lolling, swaying")


# ---------------------------------------------------------------- the clinch (paired clips)
import v4_clinch  # noqa: E402

CLIPS.update(v4_clinch.CLIPS)


def key_clip(arm, name, spec):
    return vp.key_clip(arm, name, spec)


def meta(name, spec):
    end = spec["keys"][-1][1]
    return dict(name=name, frames=spec["frames"], fps=30, loop=spec.get("loop", False),
                hitFrame=spec["hit"] if spec.get("hit") is not None else -1,
                loadFrame=spec["load"] if spec.get("load") is not None else -1,
                reach=spec.get("reach") or 0.0,
                endShift=round(end.get("hx", 0.0), 4) if spec.get("shift") else 0.0, v4=True)
