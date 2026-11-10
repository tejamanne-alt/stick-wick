"""Combat 3.0's hand-keyed moves (V3 body). See v3_rig.py for the pose language.

The hero faces +x. L = the far side = the LEAD side in the guard (left foot and fist forward);
R = the near side = the REAR, the power side. Frames are at 30 fps; every clip is baked on every
frame with the easing named on each key (the easing of the motion INTO that key):
    in / in3   accelerating: a launch, a strike driving into the impact
    out / out3 decelerating: a settle, a recovery
    smooth     ease in and out
    lin        constant speed

What makes them read as real, clip by clip:
- the blow starts in the FEET and HIPS: weight shifts, the hips turn (tw_hips), then the shoulders
  (tw_chest), then the arm. Every power shot has its rear heel turning (the rear ankle lifts) and
  the hips moving over the front foot;
- the off hand stays home: it guards the chin while the other hand works;
- the chin stays down behind the lead shoulder;
- impact is a hard stop (in3 into the impact key), with one frame of penetration after it;
- the return is faster than the way out and settles with a tiny overshoot;
- the feet are planted by IK: they never slide; a step lifts a foot through an arc.
Reaction clips (hit_*) start at the impact frame of the blow that causes them.
`aim` and `reach` are checked by build_stickman_v3.py --report: the fist or shin must land on the
target man's chin, body or thigh at the AttackData reach (gap between the two men's roots)."""
from v3_rig import P

# ---------------------------------------------------------------- the guard
# side-on, chin down behind the lead shoulder, lead fist forward at chin height, rear fist at the
# cheek, elbows in over the ribs, knees soft, weight a little more on the back foot, rear heel up
ST = P(hips=180, spine=172, chest=166, head=170, hx=0.0, hz=0.64,
       uaL=62, faL=150, uaR=22, faR=168, haR=172,
       tw_hips=-10, tw_chest=-16,
       fL=(0.14, 0.06), fR=(-0.23, 0.085))
FL, FR = ST["fL"], ST["fR"]


def G(**kw):
    """The guard with changes."""
    return P(ST, **kw)


def clip(frames, keys, hit=None, load=None, loop=False, aim=None, reach=None, react=None, note="", show=None, victim=None,
         shift=False):
    """shift: the clip travels (its last pose is the guard moved by hx): the game moves the man by
    that much when it ends (MoveSet endShift), so the feet never slide."""
    return dict(frames=frames, keys=keys, hit=hit, load=load, loop=loop, aim=aim, reach=reach, react=react,
                note=note, show=show, victim=victim, shift=shift)


CLIPS = {}

# ---------------------------------------------------------------- idle: a live guard
# 3 s loop. Breathing (the chest lifts and the shoulders rise on the in-breath, the hips sink on
# the out-breath), a slow weight rock between the feet, and the hands never quite still: the lead
# fist draws a small circle, the rear fist taps the cheek once.
CLIPS["idle"] = clip(90, [
    (0, ST, "smooth"),
    (22, G(chest=168.5, spine=173, head=171, hz=0.646, hx=0.006, uaL=60, faL=153, uaR=20, faR=170, tw_chest=-15), "smooth"),
    (45, G(chest=165, head=169, hz=0.636, hx=0.012, uaL=63, faL=147, uaR=23, faR=166, tw_chest=-17), "smooth"),
    (58, G(chest=166, hz=0.638, hx=0.01, uaL=64, faL=150, uaR=26, faR=162, haR=168), "smooth"),     # the rear fist taps the cheek
    (66, G(chest=167.5, spine=172.5, hz=0.643, hx=0.004, uaL=61, faL=152, uaR=21, faR=169), "smooth"),
    (90, ST, "smooth"),
], loop=True, note="breathing, weight rocking, hands alive")

# ---------------------------------------------------------------- JAB (lead hand, head)
# The fastest punch: no wind-up at all. The fist leaves first (the elbow follows), the lead foot
# slides forward as the rear foot pushes, the lead shoulder rolls up to cover the chin. The fist
# turns over at the end (knuckles level), snaps back on the same line, faster than it went out.
CLIPS["jab"] = clip(10, [
    (0, ST, "smooth"),
    (1, G(uaL=66, faL=140, tw_chest=-19, chest=165, hx=0.02, fL=(0.16, 0.06)), "in"),
    (2, G(uaL=82, faL=112, tw_chest=-26, tw_hips=-12, chest=161, spine=170, head=166, hx=0.07, fL=(0.21, 0.075), liftL=0.02,
          uaR=24, faR=170), "in"),
    (3, G(hL=(0.63, 1.07), tw_chest=-36, tw_hips=-15, chest=156, spine=168, head=163, hx=0.12, fL=(0.26, 0.06),
          fR=(-0.20, 0.09), uaR=25, faR=171), "in3"),
    (4, G(uaL=86, faL=100, tw_chest=-32, tw_hips=-14, chest=158, spine=169, head=164, hx=0.115, fL=(0.26, 0.06), fR=(-0.20, 0.09),
          uaR=25, faR=171), "out"),
    (6, G(uaL=68, faL=136, tw_chest=-22, chest=163, hx=0.07, fL=(0.24, 0.06), fR=(-0.21, 0.088)), "out"),
    (8, G(uaL=61, faL=153, tw_chest=-14, hx=0.02, fL=(0.16, 0.06)), "out3"),
    (10, ST, "smooth"),
], hit=3, load=1, aim=("hand.L", "chin"), reach=0.95, react="hit_front", note="lead straight: foot slides in, shoulder covers the chin")

# ---------------------------------------------------------------- CROSS (rear hand, head)
# The power straight. A short load (the weight settles on the rear leg, the shoulders turn away a
# few degrees), then the rear heel turns out and up, the hips drive round and forward over the
# front knee, the shoulders follow, and the arm goes last, straight down the middle. The lead fist
# comes back to the chin as the rear one goes. Penetration: the shoulder keeps going one frame.
CLIPS["cross"] = clip(14, [
    (0, ST, "smooth"),
    (2, G(tw_hips=-14, tw_chest=-22, hx=-0.02, hz=0.632, chest=168, uaR=18, faR=166, haR=170), "smooth"),
    (4, G(tw_hips=10, tw_chest=6, hx=0.06, chest=160, spine=168, head=166, uaR=48, faR=150, haR=140,
          uaL=48, faL=160, fR=(-0.21, 0.11)), "in"),
    (5, G(hR=(0.68, 1.08), tw_hips=28, tw_chest=34, hx=0.15, hz=0.63, chest=150, spine=162, head=160,
          uaL=40, faL=166, haL=170, fR=(-0.19, 0.13)), "in3"),
    (6, G(hR=(0.70, 1.07), tw_hips=30, tw_chest=38, hx=0.165, hz=0.628, chest=148, spine=161, head=159,
          uaL=40, faL=166, haL=170, fR=(-0.19, 0.13)), "lin"),
    (8, G(uaR=60, faR=128, haR=128, tw_hips=16, tw_chest=16, hx=0.12, chest=156, spine=165, head=163, uaL=46, faL=160,
          fR=(-0.2, 0.12)), "out"),
    (11, G(uaR=20, faR=172, haR=174, tw_hips=-8, tw_chest=-13, hx=0.02, fR=(-0.225, 0.09)), "out3"),
    (14, ST, "smooth"),
], hit=5, load=2, aim=("hand.R", "chin"), reach=1.0, react="hit_chin", note="rear straight: heel turns, hips drive, lead fist home")


# ---------------------------------------------------------------- LEAD HOOK (lead hand, jaw)
# Round the side. The weight rocks onto the lead leg, the lead heel turns out, the hips and the
# shoulders turn through hard (lead shoulder forward and round), the elbow comes up level with the
# fist and the forearm swings ACROSS the line (yaw), arriving at his jaw from the side. The rear fist
# stays glued to the cheek. A short load: the lead shoulder dips and opens first.
CLIPS["lead_hook"] = clip(15, [
    (0, ST, "smooth"),
    (2, G(tw_hips=0, tw_chest=4, hz=0.625, hx=0.03, chest=164, uaL=58, faL=140, yw_uaL=20, yw_faL=10), "smooth"),
    (4, G(tw_hips=-26, tw_chest=-30, hz=0.628, hx=0.08, chest=158, spine=168, head=164, uaL=100, faL=118, yw_uaL=34, yw_faL=-20,
          fL=(0.14, 0.075)), "in"),
    (6, G(tw_hips=-42, tw_chest=-50, hz=0.63, hx=0.1, chest=154, spine=166, head=162, uaL=114, faL=110, haL=110, yw_uaL=24, yw_faL=-72,
          fL=(0.14, 0.085), uaR=24, faR=170), "in3"),
    (7, G(tw_hips=-46, tw_chest=-56, hz=0.63, hx=0.1, chest=154, spine=166, head=162, uaL=114, faL=110, haL=110, yw_uaL=14, yw_faL=-84,
          fL=(0.14, 0.085), uaR=24, faR=170), "lin"),
    (9, G(tw_hips=-30, tw_chest=-36, hx=0.07, chest=158, uaL=78, faL=124, yw_uaL=12, yw_faL=-40, fL=(0.14, 0.07)), "out"),
    (12, G(tw_hips=-12, tw_chest=-18, hx=0.02, uaL=60, faL=152, yw_uaL=4, yw_faL=-4), "out3"),
    (15, ST, "smooth"),
], hit=6, load=2, aim=("hand.L", "chin"), reach=0.9, react="hit_chin", note="lead hook: hips turn through, forearm comes across")

# ---------------------------------------------------------------- HOOK (rear hand, jaw: the big one)
# Also the enemies' haymaker. A visible load (the rear shoulder drops back, the fist cocks wide by
# the ear), then the rear heel spins, the hips and shoulders whip round and the arm comes in a wide
# flat arc across, the body weight falling in behind it; the follow-through carries him round.
CLIPS["hook"] = clip(18, [
    (0, ST, "smooth"),
    (3, G(tw_hips=-26, tw_chest=-34, hz=0.625, hx=-0.03, chest=170, spine=175, uaR=40, faR=150, yw_uaR=-40, yw_faR=-10,
          fR=(-0.23, 0.07)), "smooth"),
    (6, G(tw_hips=14, tw_chest=10, hx=0.06, chest=160, spine=168, uaR=100, faR=124, yw_uaR=-46, yw_faR=30, uaL=48, faL=160,
          fR=(-0.21, 0.11)), "in"),
    (8, G(tw_hips=40, tw_chest=50, hx=0.14, hz=0.62, chest=150, spine=162, head=160, uaR=114, faR=110, haR=110, yw_uaR=-20, yw_faR=72,
          uaL=42, faL=164, haL=170, fR=(-0.19, 0.13)), "in3"),
    (9, G(tw_hips=46, tw_chest=60, hx=0.15, hz=0.618, chest=148, spine=161, head=160, uaR=114, faR=110, haR=110, yw_uaR=-8, yw_faR=86,
          uaL=42, faL=164, haL=170, fR=(-0.19, 0.13)), "lin"),
    (12, G(tw_hips=34, tw_chest=40, hx=0.1, chest=154, uaR=70, faR=130, yw_uaR=-4, yw_faR=50, fR=(-0.2, 0.12)), "out"),
    (15, G(tw_hips=0, tw_chest=-6, hx=0.03, uaR=26, faR=166, yw_faR=6), "out3"),
    (18, ST, "smooth"),
], hit=8, load=3, aim=("hand.R", "chin"), reach=0.95, react="hit_chin", note="rear hook / haymaker: cocked wide, the body falls in behind it")

# ---------------------------------------------------------------- UPPERCUT (rear hand, under the chin)
# Dip: the knees bend, the rear shoulder drops and the fist sinks to the hip, palm up. Then the legs
# drive up, the hips turn, and the fist rises straight up the middle under his chin, the elbow
# under it. He ends up tall on his toes; the fist overshoots up a little and comes back to the cheek.
CLIPS["uppercut"] = clip(18, [
    (0, ST, "smooth"),
    (3, G(hz=0.565, hx=0.05, spine=162, chest=150, head=158, tw_hips=-18, tw_chest=-26, uaR=-6, faR=100, haR=120,
          fL=(0.15, 0.06), dip=0.0), "smooth"),
    (5, G(hz=0.6, hx=0.11, spine=170, chest=164, head=166, tw_hips=12, tw_chest=10, uaR=40, faR=150, haR=160, fR=(-0.21, 0.11)), "in"),
    (6, G(hR=(0.72, 0.95), hz=0.655, hx=0.15, spine=178, chest=176, head=172, tw_hips=30, tw_chest=36, uaL=44, faL=164,
          fR=(-0.19, 0.14), fL=(0.15, 0.065)), "in3"),
    (7, G(hR=(0.74, 1.02), hz=0.665, hx=0.155, spine=180, chest=180, head=174, tw_hips=32, tw_chest=40, uaL=44, faL=164,
          fR=(-0.19, 0.145), fL=(0.15, 0.07)), "lin"),
    (10, G(uaR=70, faR=170, hz=0.65, hx=0.1, spine=176, chest=172, tw_hips=18, tw_chest=20, fR=(-0.2, 0.12)), "out"),
    (14, G(uaR=24, faR=170, hx=0.03, tw_hips=-6, tw_chest=-12), "out3"),
    (18, ST, "smooth"),
], hit=6, load=3, aim=("hand.R", "under_chin"), reach=0.95, react="hit_up", note="dip, then drive up off the legs")

# ---------------------------------------------------------------- BODY SHOT (lead hook to the liver)
# The level change: he drops his hips (the head goes off the centre line, to the outside), and the
# lead hand hooks in low under the elbow; the rear hand covers the chin. Then back up behind it.
CLIPS["body_shot"] = clip(16, [
    (0, ST, "smooth"),
    (3, G(hz=0.56, hx=0.07, spine=158, chest=146, head=150, tw_hips=0, tw_chest=2, uaL=40, faL=110, yw_uaL=24, yw_faL=14,
          fL=(0.2, 0.065), liftL=0.015, uaR=30, faR=168), "smooth"),
    (5, G(hz=0.55, hx=0.1, spine=156, chest=142, head=148, tw_hips=-34, tw_chest=-42, uaL=94, faL=104, haL=104, yw_uaL=22, yw_faL=-62,
          fL=(0.2, 0.08), uaR=34, faR=166), "in3"),
    (6, G(hz=0.55, hx=0.1, spine=156, chest=142, head=148, tw_hips=-38, tw_chest=-48, uaL=94, faL=104, haL=104, yw_uaL=12, yw_faL=-76,
          fL=(0.2, 0.08), uaR=34, faR=166), "lin"),
    (9, G(hz=0.59, hx=0.07, spine=164, chest=156, head=160, tw_hips=-20, tw_chest=-24, uaL=60, faL=130, yw_uaL=8, yw_faL=-20,
          fL=(0.18, 0.065)), "out"),
    (13, G(hx=0.02, tw_chest=-15, uaL=61, faL=151, fL=(0.15, 0.06)), "out3"),
    (16, ST, "smooth"),
], hit=5, load=3, aim=("hand.L", "body"), reach=0.85, react="hit_body", note="lead hook to the liver: level change, head off the line")

# ---------------------------------------------------------------- LOW KICK (rear shin into his thigh)
# A step out with the lead foot (toes turned out, so the hips can open), the hips turn over, the
# rear leg swings round from the side (yaw) with the knee slightly bent and the SHIN lands across
# his thigh; the rear arm swings down and back to balance, the lead hand guards. Then the leg
# rebounds back to its place.
CLIPS["low_kick"] = clip(20, [
    (0, ST, "smooth"),
    (3, G(hx=0.1, hz=0.63, fL=(0.26, 0.06), liftL=0.03, tw_hips=-4, tw_chest=-12, chest=168, spine=174), "smooth"),
    (5, G(hx=0.16, hz=0.64, fL=(0.27, 0.06), tw_hips=20, tw_chest=2, spine=184, chest=186, head=176,
          thR=20, shR=-30, yw_thR=-50, yw_shR=-40, uaR=-20, faR=40, uaL=60, faL=156), "in"),
    (7, G(hx=0.2, hz=0.645, fL=(0.27, 0.065), tw_hips=58, tw_chest=30, spine=194, chest=198, head=182,
          thR=62, shR=70, yw_thR=-6, yw_shR=10, uaR=-44, faR=-20, haR=-20, uaL=66, faL=152), "in3"),
    (8, G(hx=0.2, hz=0.645, fL=(0.27, 0.065), tw_hips=62, tw_chest=34, spine=195, chest=199, head=182,
          thR=62, shR=72, yw_thR=4, yw_shR=22, uaR=-46, faR=-22, haR=-22, uaL=66, faL=152), "lin"),
    (11, G(hx=0.17, hz=0.64, fL=(0.27, 0.06), tw_hips=26, tw_chest=8, spine=186, chest=186, head=178,
           thR=20, shR=-36, yw_thR=-24, yw_shR=-20, uaR=-6, faR=110), "out"),
    (14, G(hx=0.1, fL=(0.26, 0.06), fR=(-0.12, 0.08), tw_chest=-14, uaR=20, faR=166), "out"),
    (20, ST, "smooth"),
], hit=7, load=3, aim=("shin.R", "thigh"), reach=0.95, react="hit_legs", note="rear low kick: shin across the thigh, hips turned over")

# ---------------------------------------------------------------- MAWASHI GERI (rear roundhouse to the body/head)
# The knee chambers high and out to the side, the standing foot pivots on the ball (the ankle
# rises), the body leans away to counterweight, the hips roll over and the shin snaps round into his
# ribs, the foot extended. The rear arm whips down and back; the lead arm stays up.
CLIPS["mawashi_geri"] = clip(24, [
    (0, ST, "smooth"),
    (3, G(hx=0.04, hz=0.63, tw_hips=0, tw_chest=-10, spine=176, chest=172, fL=(0.15, 0.06)), "smooth"),
    (6, G(hx=0.08, hz=0.645, tw_hips=26, tw_chest=0, spine=186, chest=186, head=178, fL=(0.15, 0.075),
          thR=96, shR=-6, yw_thR=-52, yw_shR=-60, uaR=10, faR=120, uaL=62, faL=154), "in"),
    (8, G(hx=0.1, hz=0.655, tw_hips=60, tw_chest=24, spine=200, chest=208, head=190, fL=(0.15, 0.09),
          thR=100, shR=60, yw_thR=-26, yw_shR=-36, uaR=-36, faR=-10, uaL=70, faL=150), "in"),
    (9, G(hx=0.12, hz=0.66, tw_hips=78, tw_chest=34, spine=206, chest=216, head=196, fL=(0.15, 0.095),
          thR=110, shR=112, yw_thR=-4, yw_shR=4, uaR=-48, faR=-20, haR=-20, uaL=76, faL=150), "in3"),
    (10, G(hx=0.12, hz=0.66, tw_hips=82, tw_chest=38, spine=207, chest=217, head=196, fL=(0.15, 0.095),
           thR=110, shR=114, yw_thR=4, yw_shR=16, uaR=-50, faR=-22, haR=-22, uaL=76, faL=150), "lin"),
    (13, G(hx=0.1, hz=0.65, tw_hips=50, tw_chest=20, spine=196, chest=200, head=188, fL=(0.15, 0.085),
           thR=96, shR=-4, yw_thR=-20, yw_shR=-30, uaR=-10, faR=100), "out"),
    (17, G(hx=0.05, tw_hips=4, tw_chest=-8, spine=178, chest=174, fL=(0.15, 0.065),
           thR=20, shR=-40, uaR=16, faR=160), "out"),
    (20, G(hx=0.02, fR=(-0.21, 0.09), liftR=0.03), "smooth"),
    (24, ST, "smooth"),
], hit=9, load=6, aim=("shin.R", "ribs"), reach=0.95, react="hit_body", note="roundhouse: chamber, pivot, lean away, shin round into the ribs")

# ---------------------------------------------------------------- MAE GERI (front push kick)
# The lead... no: the rear knee comes straight up to the chest, then the heel drives straight out
# through his belly as the hips push forward and the body leans back; the leg recoils to the chamber
# before it is put down: a kick that shoves him away.
CLIPS["mae_geri"] = clip(20, [
    (0, ST, "smooth"),
    (3, G(hx=0.03, spine=178, chest=176, tw_hips=0, tw_chest=-10, thR=60, shR=-20, uaR=26, faR=164), "smooth"),
    (5, G(hx=0.06, hz=0.65, spine=184, chest=186, head=176, tw_hips=10, thR=104, shR=10, fL=(0.14, 0.07), uaL=58, faL=152), "in"),
    (7, G(hx=0.14, hz=0.66, spine=196, chest=200, head=184, tw_hips=24, thR=108, shR=104, fL=(0.14, 0.08),
          uaR=-14, faR=60, uaL=56, faL=150), "in3"),
    (8, G(hx=0.16, hz=0.66, spine=198, chest=202, head=185, tw_hips=26, thR=106, shR=106, fL=(0.14, 0.08),
          uaR=-16, faR=56, uaL=56, faL=150), "lin"),
    (11, G(hx=0.08, hz=0.65, spine=186, chest=188, tw_hips=10, thR=100, shR=4, fL=(0.14, 0.07)), "out"),
    (15, G(hx=0.03, thR=10, shR=-30, tw_hips=-6, tw_chest=-14), "out"),
    (20, ST, "smooth"),
], hit=7, load=5, aim=("shin.R", "body"), reach=0.95, react="hit_shoved", note="push kick: knee up, heel through the belly, recoil")

# ---------------------------------------------------------------- KNEE (rising rear knee, close)
# Both hands reach and pull his head down (a short collar tie), the hips drive forward and the rear
# knee comes straight up into his body; up on the toes of the standing foot.
CLIPS["knee"] = clip(16, [
    (0, ST, "smooth"),
    (3, G(hx=0.06, spine=168, chest=160, head=164, uaL=96, faL=120, uaR=86, faR=118, tw_chest=0, tw_hips=0), "smooth"),
    (5, G(hx=0.12, hz=0.66, spine=176, chest=170, head=166, uaL=86, faL=70, uaR=80, faR=66, tw_hips=12,
          thR=60, shR=-40, fL=(0.15, 0.08)), "in"),
    (6, G(hx=0.16, hz=0.675, spine=186, chest=176, head=168, uaL=76, faL=50, uaR=70, faR=46, tw_hips=20,
          thR=118, shR=-4, fL=(0.15, 0.1)), "in3"),
    (7, G(hx=0.17, hz=0.675, spine=188, chest=178, head=168, uaL=74, faL=46, uaR=68, faR=42, tw_hips=22,
          thR=122, shR=-2, fL=(0.15, 0.1)), "lin"),
    (10, G(hx=0.1, hz=0.65, spine=176, chest=168, uaL=70, faL=130, uaR=40, faR=150, thR=40, shR=-40, fL=(0.15, 0.065)), "out"),
    (16, ST, "smooth"),
], hit=6, load=3, aim=("thigh.R", "body"), reach=0.6, react="hit_body", note="rising knee: hands pull his head down onto it")

# ---------------------------------------------------------------- ELBOW (rear horizontal elbow)
# In close: the rear elbow comes up level, the fist by the ear, and the whole turning body swings it
# across his jaw; the lead hand frames his shoulder.
CLIPS["elbow"] = clip(15, [
    (0, ST, "smooth"),
    (2, G(tw_hips=-20, tw_chest=-28, hx=0.02, uaR=40, faR=170, haR=176, uaL=70, faL=110), "smooth"),
    (4, G(tw_hips=10, tw_chest=8, hx=0.08, chest=160, uaR=88, faR=176, yw_uaR=-50, yw_faR=-10, uaL=84, faL=96, fR=(-0.21, 0.11)), "in"),
    (5, G(tw_hips=40, tw_chest=54, hx=0.06, chest=152, spine=164, head=160, uaR=118, faR=200, haR=200, yw_uaR=10, yw_faR=60,
          uaL=50, faL=150, fR=(-0.19, 0.13)), "in3"),
    (6, G(tw_hips=44, tw_chest=60, hx=0.065, chest=152, spine=164, head=160, uaR=118, faR=202, haR=202, yw_uaR=22, yw_faR=72,
          uaL=50, faL=150, fR=(-0.19, 0.13)), "lin"),
    (9, G(tw_hips=24, tw_chest=24, hx=0.09, uaR=60, faR=168, yw_uaR=10, yw_faR=30, fR=(-0.2, 0.11)), "out"),
    (15, ST, "smooth"),
], hit=5, load=2, aim=("upperarm.R", "chin"), reach=0.75, react="hit_chin", note="rear elbow across the jaw, the body turning behind it")

# ---------------------------------------------------------------- HAMMER FIST (down on a man bent double)
CLIPS["hammer_fist"] = clip(18, [
    (0, ST, "smooth"),
    (4, G(hx=0.04, hz=0.66, spine=184, chest=190, head=176, uaR=170, faR=200, haR=210, uaL=70, faL=130, tw_hips=0, tw_chest=0,
          fR=(-0.22, 0.1)), "smooth"),
    (7, G(hx=0.12, hz=0.58, spine=150, chest=134, head=146, uaR=96, faR=110, haR=120, uaL=50, faL=110, tw_hips=26, tw_chest=30,
          fR=(-0.2, 0.12)), "in3"),
    (8, G(hx=0.12, hz=0.575, spine=148, chest=132, head=146, uaR=88, faR=96, haR=104, uaL=50, faL=110, tw_hips=26, tw_chest=30,
          fR=(-0.2, 0.12)), "lin"),
    (12, G(hx=0.07, hz=0.62, spine=166, chest=156, uaR=40, faR=150, tw_hips=6), "out"),
    (18, ST, "smooth"),
], hit=7, load=4, aim=("hand.R", "back"), reach=0.8, react="hit_body", note="hammer fist down, the whole body dropping with it")

# ---------------------------------------------------------------- the PULL & RELEASE power shot
# `coil` is scrubbed by the game while the finger drags back (0 = guard, the end = a full coil):
# the weight sinks onto the back leg, the hips and shoulders turn away, the rear fist draws back by
# the ear, the chin tucks, the lead hand reaches out to measure. `power_cross` is the release, from
# the full coil: the rear foot pushes off, he travels in a long step and the right lands with all of
# him behind it; the rear foot drags up after.
COIL = G(hx=-0.08, hz=0.6, spine=176, chest=174, head=164, tw_hips=-38, tw_chest=-54,
         uaR=30, faR=176, haR=184, yw_uaR=-30, uaL=84, faL=120, haL=110,
         fL=(0.15, 0.06), fR=(-0.23, 0.07))
CLIPS["coil"] = clip(20, [
    (0, ST, "lin"),
    (8, G(hx=-0.04, hz=0.62, spine=174, chest=170, head=167, tw_hips=-22, tw_chest=-32, uaR=26, faR=174, haR=178, yw_uaR=-14,
          uaL=72, faL=136), "out"),
    (20, COIL, "out"),
], note="the drag: scrubbed live by the pull (0 = guard, 20 = full coil)", show=[0, 5, 10, 15, 20])
CLIPS["power_cross"] = clip(22, [
    (0, COIL, "lin"),
    (3, P(COIL, hx=0.06, hz=0.6, tw_hips=0, tw_chest=-10, chest=164, spine=170, uaR=50, faR=150, haR=150, yw_uaR=-10,
          fL=(0.3, 0.07), liftL=0.04, fR=(-0.2, 0.11)), "in"),
    (5, P(COIL, hR=(0.83, 1.06), hx=0.3, hz=0.6, tw_hips=36, tw_chest=46, chest=144, spine=158, head=156, yw_uaR=0,
          uaL=40, faL=166, haL=170, fL=(0.42, 0.06), fR=(-0.08, 0.14)), "in3"),
    (6, P(COIL, hR=(0.86, 1.05), hx=0.32, hz=0.595, tw_hips=40, tw_chest=50, chest=142, spine=157, head=156, yw_uaR=0,
          uaL=40, faL=166, haL=170, fL=(0.42, 0.06), fR=(-0.06, 0.14)), "lin"),
    (10, P(ST, hx=0.26, hz=0.62, uaR=56, faR=130, tw_hips=24, tw_chest=24, chest=154, spine=164,
           fL=(0.42, 0.06), fR=(0.02, 0.1), liftR=0.04), "out"),
    (16, P(ST, hx=0.3, hz=0.63, fL=(0.44, 0.06), fR=(0.07, 0.085)), "smooth"),
    (22, P(ST, hx=0.3, fL=(FL[0] + 0.3, FL[1]), fR=(FR[0] + 0.3, FR[1])), "smooth"),
], shift=True, hit=5, load=0, aim=("hand.R", "chin"), reach=1.15, react="hit_launch", note="the release: a long step, all of him behind the right hand")

# ---------------------------------------------------------------- GUARD (hold on yourself)
# Up: forearms come together in front of the face, elbows in over the ribs, chin down into the
# chest, the knees sink. Hold: the same, breathing quickly behind the gloves, peeking. Hit: a blow
# lands on the forearms: the arms are driven back into the head, the body rocks back on the rear
# leg, then he re-sets.
GUARD = G(hz=0.6, spine=166, chest=158, head=150, tw_hips=-6, tw_chest=-8,
          uaL=40, faL=166, haL=172, uaR=34, faR=170, haR=176, yw_uaL=-8, yw_uaR=8)
CLIPS["guard_up"] = clip(5, [(0, ST, "lin"), (2, P(GUARD, hz=0.605, head=156), "in"), (5, GUARD, "out")],
                         note="forearms up, chin down, knees sink")
CLIPS["guard_hold"] = clip(40, [
    (0, GUARD, "smooth"),
    (12, P(GUARD, hz=0.604, chest=160, head=153, uaL=42, faL=164), "smooth"),
    (20, P(GUARD, hz=0.598, chest=157, head=148), "smooth"),
    (30, P(GUARD, hz=0.603, chest=159, head=152, uaR=36, faR=168), "smooth"),
    (40, GUARD, "smooth"),
], loop=True, note="held: quick breaths, peeking over the forearms")
CLIPS["guard_hit"] = clip(12, [
    (0, GUARD, "lin"),
    (2, P(GUARD, hx=-0.07, hz=0.595, spine=176, chest=172, head=168, uaL=30, faL=172, uaR=26, faR=176,
          fL=(0.14, 0.06), fR=(-0.23, 0.07)), "out3"),
    (6, P(GUARD, hx=-0.03, spine=170, chest=164, head=156), "out"),
    (12, GUARD, "smooth"),
], note="a blow on the forearms: driven back, re-set")

# ---------------------------------------------------------------- PARRY (perfect guard)
# The lead hand slaps his punch across and down at the last instant, the head slips off the line
# the other way, and the rear hand is cocked for the counter.
CLIPS["parry"] = clip(10, [
    (0, GUARD, "lin"),
    (2, G(hz=0.61, head=164, chest=160, tw_chest=-28, uaL=78, faL=150, haL=150, yw_faL=-30, uaR=30, faR=168), "in"),
    (3, G(hz=0.61, head=160, chest=158, tw_chest=-34, uaL=60, faL=100, haL=96, yw_faL=-60, uaR=34, faR=166, hx=-0.02), "in3"),
    (6, G(hz=0.62, head=166, tw_chest=-26, uaL=50, faL=130, yw_faL=-20, uaR=36, faR=164), "out"),
    (10, ST, "smooth"),
], hit=3, note="lead hand slaps it across and down, head off the line")

# ---------------------------------------------------------------- DUCK (swipe down from yourself)
# Bob under: the knees bend, the hips drop, the chest folds and the head goes down and slightly
# forward (under a hook, the head draws a U: down, under, up on the other side); the hands stay up.
CLIPS["duck"] = clip(14, [
    (0, ST, "smooth"),
    (3, G(hz=0.52, hx=-0.01, spine=150, chest=134, head=138, tw_chest=-4, uaL=70, faL=170, uaR=50, faR=176,
          fL=(0.17, 0.06), fR=(-0.25, 0.08)), "in"),
    (6, G(hz=0.5, hx=0.02, spine=148, chest=130, head=136, tw_chest=10, uaL=72, faL=172, uaR=54, faR=178,
          fL=(0.17, 0.06), fR=(-0.25, 0.08)), "smooth"),
    (10, G(hz=0.57, hx=0.04, spine=162, chest=150, head=156, tw_chest=-8, fL=(0.16, 0.06)), "out"),
    (14, ST, "smooth"),
], note="bob under: head draws a U, hands stay up")

# ---------------------------------------------------------------- STEP OUT (swipe back from yourself)
# The lead foot pushes, the rear foot steps back and the lead foot slides after it: a whole stance
# back, the upper body leaning away from the blow, the guard kept.
CLIPS["step_out"] = clip(12, [
    (0, ST, "smooth"),
    (2, G(hx=-0.04, spine=176, chest=178, head=178, fR=(-0.3, 0.09), liftR=0.02), "in"),
    (5, G(hx=-0.2, hz=0.62, spine=182, chest=186, head=184, fR=(-0.45, 0.08), liftR=0.03, fL=(0.05, 0.06), uaL=56, faL=156), "out"),
    (8, G(hx=-0.3, spine=176, chest=174, head=174, fR=(-0.47, 0.085), fL=(-0.14, 0.06), liftL=0.02), "out"),
    (12, P(ST, hx=-0.32, fL=(FL[0] - 0.32, FL[1]), fR=(FR[0] - 0.32, FR[1])), "smooth"),
], note="a stance back, leaning away, guard kept", shift=True)

# ---------------------------------------------------------------- STEP IN (swipe towards him)
# Inside a long swing: a fast step forward and off the line, the forearms up and the head tucked
# against his chest.
CLIPS["step_in"] = clip(10, [
    (0, ST, "smooth"),
    (2, G(hx=0.08, hz=0.6, spine=164, chest=154, head=150, uaL=80, faL=170, uaR=60, faR=172, fL=(0.3, 0.07), liftL=0.03), "in"),
    (5, G(hx=0.3, hz=0.59, spine=160, chest=148, head=146, uaL=86, faL=172, uaR=66, faR=174, fL=(0.46, 0.06), fR=(-0.06, 0.09), liftR=0.03), "out"),
    (10, P(ST, hx=0.32, fL=(FL[0] + 0.32, FL[1]), fR=(FR[0] + 0.32, FR[1])), "smooth"),
], note="a step inside his swing, forearms up, head on his chest", shift=True)

# ---------------------------------------------------------------- HOP (swipe up from yourself)
# Over a sweep: a small two-footed hop, the knees tucked up, the guard kept, a soft landing that
# sinks through the knees.
CLIPS["hop"] = clip(18, [
    (0, ST, "smooth"),
    (3, G(hz=0.55, spine=166, chest=158, fL=FL, fR=FR), "smooth"),
    (6, G(hz=0.78, hx=0.02, spine=174, chest=168, thL=70, shL=-20, thR=50, shR=-50), "out"),
    (9, G(hz=0.84, hx=0.03, spine=172, chest=166, thL=86, shL=-10, thR=70, shR=-50), "out"),
    (12, G(hz=0.69, hx=0.03, thL=30, shL=-6, thR=-8, shR=-30), "in"),
    (14, G(hz=0.54, hx=0.03, spine=164, chest=154, fL=(0.17, 0.06), fR=(-0.2, 0.085)), "out"),
    (18, ST, "smooth"),
], note="a hop over the sweep, knees tucked, soft landing")

# ---------------------------------------------------------------- COUNTERS (after a perfect defence)
# parry → TRAP AND ELBOW: the lead hand pins his punching arm down against his chest, the rear elbow
# comes over the top into his jaw.
CLIPS["trap_elbow"] = clip(20, [
    (0, CLIPS["parry"]["keys"][-2][1], "lin"),
    (3, G(hx=0.1, hz=0.62, tw_hips=-20, tw_chest=-30, uaL=90, faL=70, haL=60, yw_faL=-20, uaR=60, faR=176, haR=180, yw_uaR=-40,
          fL=(0.26, 0.06), liftL=0.02), "in"),
    (6, G(hx=0.1, hz=0.62, tw_hips=36, tw_chest=52, chest=150, spine=162, head=158, uaL=70, faL=40, haL=30, yw_faL=-10,
          uaR=114, faR=204, haR=200, yw_uaR=14, yw_faR=50, fL=(0.27, 0.06), fR=(-0.16, 0.13)), "in3"),
    (7, G(hx=0.105, hz=0.615, tw_hips=40, tw_chest=58, chest=148, spine=161, head=158, uaL=70, faL=40, haL=30,
          uaR=114, faR=206, haR=204, yw_uaR=24, yw_faR=62, fL=(0.27, 0.06), fR=(-0.16, 0.13)), "lin"),
    (12, G(hx=0.12, tw_hips=10, tw_chest=0, uaR=40, faR=170, uaL=64, faL=140, fL=(0.27, 0.06), fR=(-0.18, 0.09)), "out"),
    (20, P(ST, hx=0.12, fL=(FL[0] + 0.12, FL[1]), fR=(FR[0] + 0.12, FR[1])), "smooth"),
], hit=6, aim=("upperarm.R", "chin"), reach=0.8, react="hit_down", note="COUNTER (parry): pin his arm, elbow over the top", shift=True)

# under (a perfect duck) → LIVER SHOT, then the uppercut: from the bottom of the duck the lead hand
# digs into his liver and, as he folds, the rear hand comes up the middle.
CLIPS["liver_shot"] = clip(12, [
    (0, CLIPS["duck"]["keys"][2][1], "lin"),
    (3, G(hz=0.53, hx=0.0, spine=150, chest=134, head=138, tw_hips=-36, tw_chest=-44, uaL=96, faL=102, haL=102, yw_uaL=18, yw_faL=-64,
          uaR=46, faR=170, fL=(0.2, 0.08), fR=(-0.25, 0.08)), "in3"),
    (4, G(hz=0.53, hx=0.0, spine=150, chest=134, head=138, tw_hips=-40, tw_chest=-50, uaL=96, faL=102, haL=102, yw_uaL=8, yw_faL=-78,
          uaR=46, faR=170, fL=(0.2, 0.08), fR=(-0.25, 0.08)), "lin"),
    (8, G(hz=0.54, hx=0.08, spine=156, chest=144, head=148, tw_hips=-20, tw_chest=-28, uaL=50, faL=140, uaR=-4, faR=104, haR=120,
          fL=(0.2, 0.065), fR=(-0.25, 0.08)), "out"),
    (12, G(hz=0.565, hx=0.05, spine=162, chest=150, head=158, tw_hips=-18, tw_chest=-26, uaR=-6, faR=100, haR=120,
           fL=(0.15, 0.06)), "smooth"),
], hit=3, aim=("hand.L", "body"), reach=0.75, react="hit_body", note="COUNTER (duck): lead hand into the liver, then the uppercut")

# a perfect step-out (he overreaches) → COUNTER CROSS: back in on the rear foot's push with the
# right, into him as he stumbles past.
CLIPS["counter_cross"] = clip(16, [
    (0, P(ST, hx=0.0), "lin"),
    (2, G(hx=0.08, hz=0.62, tw_hips=-14, tw_chest=-20, fL=(0.3, 0.07), liftL=0.03, uaL=56, faL=158), "in"),
    (4, G(hR=(0.81, 1.05), hx=0.28, hz=0.61, tw_hips=34, tw_chest=44, chest=146, spine=160, head=156,
          uaL=40, faL=166, haL=170, fL=(0.4, 0.06), fR=(-0.1, 0.13)), "in3"),
    (5, G(hR=(0.83, 1.04), hx=0.3, hz=0.605, tw_hips=36, tw_chest=48, chest=144, spine=159, head=156,
          uaL=40, faL=166, haL=170, fL=(0.4, 0.06), fR=(-0.08, 0.13)), "lin"),
    (9, G(hx=0.26, uaR=56, faR=132, tw_hips=20, tw_chest=18, chest=156, fL=(0.4, 0.06), fR=(-0.02, 0.1), liftR=0.03), "out"),
    (16, P(ST, hx=0.26, fL=(FL[0] + 0.26, FL[1]), fR=(FR[0] + 0.26, FR[1])), "smooth"),
], hit=4, aim=("hand.R", "chin"), reach=1.1, react="hit_launch", note="COUNTER (step out): back in with the right as he overreaches", shift=True)

# ---------------------------------------------------------------- REACTIONS (frame 0 = the impact)
# Each one moves the body the way the blow pushed it: the head first, the torso after it, the feet
# last (a step to catch the weight), then he comes back to his guard, shaken.
REC = G(head=166, chest=162)        # shaken: chin a little lower
# (the game's knockback moves the man; the clip itself keeps him over his feet and ends in place)
CLIPS["hit_front"] = clip(14, [
    (0, ST, "lin"),
    (1, G(head=196, chest=172, spine=174, hx=-0.02, uaL=50, faL=136, uaR=16, faR=150), "out3"),
    (3, G(head=204, chest=180, spine=178, hx=-0.06, uaL=44, faL=126, uaR=12, faR=140, fL=(0.1, 0.06)), "out"),
    (7, G(head=180, chest=170, hx=-0.04, fL=(0.1, 0.06)), "smooth"),
    (14, P(REC), "smooth"),
], note="jabbed: head snaps back, a half step", )
CLIPS["hit_chin"] = clip(20, [
    (0, ST, "lin"),
    (1, G(head=214, chest=180, spine=176, hx=-0.03, uaL=46, faL=120, uaR=10, faR=130, tw_chest=10), "out3"),
    (3, G(head=226, chest=194, spine=184, hx=-0.09, hz=0.63, uaL=40, faL=96, uaR=0, faR=110, tw_chest=24, tw_hips=10, fL=(0.08, 0.07), liftL=0.03), "out"),
    (7, G(head=204, chest=186, spine=182, hx=-0.16, uaL=30, faL=70, uaR=-6, faR=60, tw_chest=14, fL=(-0.04, 0.06), fR=(-0.36, 0.08), liftR=0.03), "out"),
    (12, G(head=178, chest=170, hx=-0.16, uaL=50, faL=130, uaR=16, faR=150, fL=(-0.02, 0.06), fR=(-0.38, 0.085)), "smooth"),
    (20, P(REC), "smooth"),
], note="rocked: the head turned and thrown back, arms fly, two steps to catch it", )
CLIPS["hit_up"] = clip(22, [
    (0, ST, "lin"),
    (1, G(head=230, chest=196, spine=186, hz=0.67, uaL=70, faL=120, uaR=40, faR=110, fL=(0.14, 0.07), fR=(-0.23, 0.1)), "out3"),
    (4, G(head=236, chest=204, spine=192, hz=0.7, hx=-0.06, uaL=90, faL=140, uaR=60, faR=130, fL=(0.12, 0.1), fR=(-0.25, 0.12)), "out"),
    (8, G(head=210, chest=196, spine=188, hz=0.64, hx=-0.14, uaL=40, faL=80, uaR=10, faR=60, fL=(0.0, 0.06), fR=(-0.36, 0.08), liftL=0.05), "in"),
    (14, G(head=180, chest=172, hz=0.62, hx=-0.16, fL=(0.0, 0.06), fR=(-0.38, 0.08)), "out"),
    (22, P(REC), "smooth"),
], note="uppercut: lifted onto his toes, head thrown up, drops back", )
CLIPS["hit_body"] = clip(22, [
    (0, ST, "lin"),
    (1, G(spine=160, chest=142, head=150, hz=0.62, hx=-0.03, uaL=20, faL=110, uaR=10, faR=104), "out3"),
    (4, G(spine=148, chest=124, head=136, hz=0.56, hx=-0.07, uaL=10, faL=96, uaR=4, faR=90, fL=(0.1, 0.06), fR=(-0.25, 0.08)), "out"),
    (12, G(spine=150, chest=128, head=140, hz=0.56, hx=-0.07, uaL=12, faL=100, uaR=6, faR=94, fL=(0.1, 0.06), fR=(-0.25, 0.08)), "smooth"),
    (22, P(REC), "smooth"),
], note="to the body: folds over it, arms in, breath gone", )
CLIPS["hit_legs"] = clip(18, [
    (0, ST, "lin"),
    (1, G(fL=(0.2, 0.09), hz=0.6, spine=176, chest=172, head=176, uaL=50, faL=120), "out3"),
    (4, G(fL=(0.04, 0.06), hz=0.5, spine=168, chest=160, head=160, uaL=30, faL=100, uaR=40, faR=100, tw_hips=10), "out"),
    (10, G(fL=(0.08, 0.06), hz=0.56, spine=170, chest=164, head=164), "smooth"),
    (18, ST, "smooth"),
], note="the kicked leg swept out and buckles, a hand drops")
CLIPS["hit_shoved"] = clip(20, [
    (0, ST, "lin"),
    (1, G(spine=156, chest=140, head=150, hz=0.62, hx=-0.06, uaL=70, faL=90, uaR=60, faR=80), "out3"),
    (5, G(spine=160, chest=148, head=156, hx=-0.3, uaL=80, faL=110, uaR=70, faR=100, fL=(-0.1, 0.07), fR=(-0.5, 0.08), liftL=0.05, liftR=0.02), "out"),
    (10, G(hx=-0.42, spine=168, chest=160, fL=(-0.26, 0.06), fR=(-0.62, 0.08), liftL=0.04), "out"),
    (20, P(REC), "smooth"),
], note="kicked in the belly: folded and driven back three steps", )
CLIPS["hit_launch"] = clip(24, [
    (0, ST, "lin"),
    (1, G(head=220, chest=190, spine=184, hx=-0.06, uaL=70, faL=100, uaR=50, faR=90), "out3"),
    (4, G(head=230, chest=204, spine=194, hx=-0.24, hz=0.62, uaL=110, faL=140, uaR=90, faR=130, fL=(-0.02, 0.1), fR=(-0.44, 0.08), liftL=0.06), "out"),
    (9, G(head=214, chest=196, spine=190, hx=-0.44, hz=0.6, uaL=80, faL=110, uaR=60, faR=100, fL=(-0.32, 0.06), fR=(-0.62, 0.08), liftL=0.05, liftR=0.03), "out"),
    (15, G(head=186, chest=176, hx=-0.52, fL=(-0.38, 0.06), fR=(-0.72, 0.085)), "out"),
    (24, P(REC), "smooth"),
], note="the big right: thrown back three staggering steps", )
CLIPS["hit_down"] = clip(20, [
    (0, ST, "lin"),
    (1, G(head=150, chest=150, spine=164, hz=0.6, uaL=30, faL=60, uaR=0, faR=40, tw_chest=20), "out3"),
    (5, G(head=126, chest=124, spine=148, hz=0.48, hx=0.02, uaL=10, faL=30, uaR=-10, faR=20, fL=(0.18, 0.06), fR=(-0.27, 0.08), tw_chest=24), "out"),
    (12, G(head=140, chest=130, spine=152, hz=0.5, hx=0.0, uaL=30, faL=90, uaR=10, faR=80, fL=(0.18, 0.06), fR=(-0.27, 0.08)), "smooth"),
    (20, ST, "smooth"),
], note="elbowed over the top: driven down, nearly to a knee")
# an enemy whose punch was slapped aside: his arm thrown across, the body turned open
CLIPS["parried_v"] = clip(18, [
    (0, CLIPS["jab"]["keys"][3][1], "lin"),
    (2, G(hx=0.1, tw_chest=20, tw_hips=6, chest=158, head=168, uaL=40, faL=60, yw_uaL=-40, yw_faL=-60, uaR=40, faR=110,
          fL=(0.26, 0.06)), "out3"),
    (8, G(hx=0.08, tw_chest=26, chest=160, head=172, uaL=30, faL=50, yw_uaL=-30, yw_faL=-40, uaR=30, faR=90, fL=(0.26, 0.06)), "out"),
    (18, P(ST, hx=0.12, fL=(FL[0] + 0.12, FL[1]), fR=(FR[0] + 0.12, FR[1])), "smooth"),
], note="parried: his arm knocked across, turned open", shift=True)
# an enemy whose blow found nothing: over-reaching, he stumbles a step past
CLIPS["whiff_stumble"] = clip(22, [
    (0, CLIPS["cross"]["keys"][4][1], "lin"),
    (4, G(hx=0.3, hz=0.6, spine=150, chest=136, head=146, uaR=90, faR=96, uaL=60, faL=110, tw_chest=40, tw_hips=30,
          fL=(0.34, 0.06), fR=(0.0, 0.12), liftL=0.04, liftR=0.03), "out"),
    (10, G(hx=0.42, hz=0.6, spine=156, chest=146, head=154, uaR=40, faR=90, uaL=40, faL=110, tw_chest=20,
           fL=(0.56, 0.06), fR=(0.12, 0.08), liftL=0.05), "out"),
    (22, P(ST, hx=0.42, fL=(FL[0] + 0.42, FL[1]), fR=(FR[0] + 0.42, FR[1])), "smooth"),
], note="his blow found nothing: overreaches and stumbles past", shift=True)

# ---------------------------------------------------------------- moving in the guard
# The boxer's step: the foot nearest where he goes moves first, the other follows the same
# distance; the hips glide level, the hands stay up. One cycle = one step of 0.24 m (the game plays
# it at the speed that matches his ground speed).
STEP = 0.24
CLIPS["shuffle_fwd"] = clip(16, [
    (0, ST, "lin"),
    (5, G(hx=STEP * 0.55, hz=0.63, fL=(FL[0] + STEP, FL[1]), liftL=0.035, spine=170, chest=164), "smooth"),
    (11, G(hx=STEP * 0.95, fL=(FL[0] + STEP, FL[1]), fR=(FR[0] + STEP, FR[1]), liftR=0.03, hz=0.635), "smooth"),
    (16, P(ST, hx=STEP, fL=(FL[0] + STEP, FL[1]), fR=(FR[0] + STEP, FR[1])), "smooth"),
], note="step-drag forward in the guard (one 0.24 m step)", shift=True)
CLIPS["shuffle_back"] = clip(16, [
    (0, ST, "lin"),
    (5, G(hx=-STEP * 0.55, hz=0.63, fR=(FR[0] - STEP, FR[1]), liftR=0.035, spine=174, chest=168), "smooth"),
    (11, G(hx=-STEP * 0.95, fL=(FL[0] - STEP, FL[1]), fR=(FR[0] - STEP, FR[1]), liftL=0.03, hz=0.635), "smooth"),
    (16, P(ST, hx=-STEP, fL=(FL[0] - STEP, FL[1]), fR=(FR[0] - STEP, FR[1])), "smooth"),
], note="step-drag back in the guard (one 0.24 m step)", shift=True)
