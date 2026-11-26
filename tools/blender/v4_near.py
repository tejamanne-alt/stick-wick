"""Near range, read the man (docs/design/combat_v3.md, "near range"): the V4 body.

HERO
  liver_hook     inside, tap low: the lead hook dug into the liver, the level dropped to throw it
  body_cross     the rear hand straight to the solar plexus, the knees bent to get under his guard
  rear_elbow     inside: the rear elbow, horizontal, the shoulder turned through it
  elbow_up       inside: the rising elbow under the chin (Close Quarters' last)
  rising_knee    he's folded: both hands on his head, pulled down onto the knee
  overhand       over his low guard, or after he misses: the rear hand looping over the top
  head_kick      his leg buckled: the round kick to the head, the hips turned all the way over
  crossfire      TWO MEN ON YOU (a three-body cinematic, crossfire_a_v in front, crossfire_b_v behind):
                 both swing; he drops under it and their punches cross into each other's faces;
                 he comes up between them with a palm in front and an elbow behind; the push kick
                 sends the front man flying; the back kick sends the other

THE MAN, READABLE (each held long enough to be read and answered; the opening is exaggerated)
  cover_high     hands glued to his head, elbows tight, chin down: the BODY is open
  cover_low      elbows down over his belly, hunched, the head up: the HEAD is open
  fold           doubled over a body shot, arms wrapped round it, face to the floor: KNEE him
  head_back      the head thrown back, arms flung wide, looking at the ceiling: the BODY is open
  overreach      his punch missed: stretched out, off balance, falling in: COUNTER over the top
  leg_buckle     the kicked leg gives, a hand on the thigh, the head dropped low: HEAD KICK
  wall_slump     driven into the wall, sliding down it, then dragging himself back up"""
from v4_pose import P
from v4_clips import GUARD, G, clip, REC
from v4_clinch import LIE

CLIPS = {}
GL, GR = GUARD["fL"], GUARD["fR"]

# ---------------------------------------------------------------- hero: inside work
CLIPS["liver_hook"] = clip(18, [
    (0, GUARD, "lin"),
    (3, G(hz=0.585, spine=170, spine_mid=164, chest=158, neck=150, head=164, tw_hips=-20, tw_chest=-22,
          uaL=40, faL=120, fL=(GL[0], 10), fR=(GR[0], 44)), "in"),                                     # the level drops, coiled to the lead side
    (6, G(hz=0.57, spine=166, spine_mid=160, chest=154, neck=148, head=162, tw_hips=12, tw_spine=10, tw_chest=26,
          hL=(0.6, 0.76), yw_uaL=-34, yw_faL=-50, twL=-10, fiL=170, fL=(GL[0], 34), fR=(GR[0], 40)), "in3"),   # HIT: the lead heel pivots, the fist digs in under the ribs
    (9, G(hz=0.57, spine=166, chest=154, tw_hips=10, tw_chest=22, hL=(0.56, 0.78), yw_uaL=-30, yw_faL=-40, fiL=170,
          fL=(GL[0], 30), fR=(GR[0], 40)), "out"),
    (18, GUARD, "smooth"),
], hit=6, reach=0.75, aim=("fistL", "body"), react="fold", striker="L",
   note="the liver hook: the level drops, the lead heel pivots, the fist digs in under the ribs")

CLIPS["body_cross"] = clip(18, [
    (0, GUARD, "lin"),
    (3, G(hz=0.575, spine=168, chest=158, neck=150, head=164, fR=(GR[0], 56), fL=(GL[0] + 0.06, 0.04, -6)), "in"),   # a little step in and down
    (6, G(hx=0.08, hz=0.56, spine=160, spine_mid=154, chest=148, neck=146, head=160, tw_hips=20, tw_spine=18, tw_chest=32,
          hR=(0.74, 0.8), twR=90, fiR=170, uaL=46, faL=166, fL=(GL[0] + 0.1, 0), fR=(GR[0], 76)), "in3"),       # HIT: the rear hand into the solar plexus, the rear heel up
    (9, G(hx=0.08, hz=0.56, spine=162, chest=150, tw_hips=16, tw_chest=26, hR=(0.7, 0.82), twR=90, fiR=170,
          fL=(GL[0] + 0.1, 0), fR=(GR[0], 70)), "out"),
    (18, P(GUARD, hx=0.08, fL=(GL[0] + 0.1, 0), fR=(GR[0] + 0.08, 38)), "smooth"),
], hit=6, reach=0.95, aim=("fistR", "body"), react="fold", striker="R", shift=True,
   note="the body cross: down and in, the rear hand into the solar plexus")

CLIPS["rear_elbow"] = clip(17, [
    (0, GUARD, "lin"),
    (3, G(tw_hips=-18, tw_chest=-26, uaR=60, faR=200, hz=0.62, fR=(GR[0], 46)), "in"),
    (6, G(tw_hips=24, tw_spine=20, tw_chest=40, spine=168, chest=160, uaR=110, faR=232, haR=236, yw_uaR=20, fiR=170,
          clR=(24, 12), fR=(GR[0], 74), hz=0.61), "in3"),                                                    # HIT: the elbow carried across his jaw
    (9, G(tw_hips=28, tw_chest=44, uaR=92, faR=236, yw_uaR=24, fR=(GR[0], 70)), "out"),
    (17, GUARD, "smooth"),
], hit=6, reach=0.62, aim=("elbowR", "chin"), react="hit_chin", striker="R",
   note="the rear elbow: the shoulder turned through, the elbow carried flat across his jaw")

CLIPS["elbow_up"] = clip(19, [
    (0, GUARD, "lin"),
    (3, G(hz=0.56, spine=168, chest=160, head=164, uaR=10, faR=150, fR=(GR[0], 50)), "in"),                  # sink
    (7, G(hx=0.08, hz=0.66, spine=182, spine_mid=184, chest=186, neck=172, head=170, tw_chest=24, uaR=104, faR=206, haR=210, fiR=170,
          clR=(10, 26), fR=(GR[0], 80), fL=(GL[0], 30)), "in3"),                                              # HIT: driven up off the legs, the point of the elbow under his chin
    (11, G(hx=0.06, hz=0.66, spine=184, chest=188, tw_chest=20, uaR=120, faR=214, fR=(GR[0], 70)), "out"),
    (19, GUARD, "smooth"),
], hit=7, reach=0.55, aim=("elbowR", "under_chin"), react="hit_up", striker="R",
   note="the rising elbow: sink, then drive up off the legs, the elbow's point under his chin")

CLIPS["rising_knee"] = clip(22, [
    (0, GUARD, "lin"),
    (3, G(hx=0.04, hz=0.63, spine=170, chest=162, hL=(0.5, 0.86), hR=(0.52, 0.84), fiL=60, fiR=60, fR=(GR[0], 40)), "in"),     # the hands find the back of his head
    (5, G(hx=0.04, hz=0.66, spine=176, chest=170, hL=(0.48, 0.8), hR=(0.5, 0.78), fiL=140, fiR=140,
          fL=(GL[0] + 0.04, 40), fR=(GR[0] + 0.04, 0.1, 30)), "in"),                                         # pulled down
    (8, G(hx=0.06, hz=0.7, spine=186, spine_mid=186, chest=180, neck=168, head=166, hL=(0.46, 0.74), hR=(0.48, 0.72), fiL=150, fiR=150,
          fL=(GL[0] + 0.04, 60), thR=118, shR=-12, fpR=40), "in3"),                                          # HIT: the hips thrust, the knee up into his face
    (12, G(hx=0.04, hz=0.66, spine=178, chest=172, hL=(0.5, 0.86), hR=(0.52, 0.84), fiL=60, fiR=60,
           fL=(GL[0] + 0.04, 30), fR=(GR[0] + 0.1, 0.12, 20)), "out"),                                       # letting go
    (22, P(GUARD, hx=0.04, fL=(GL[0] + 0.04, 0), fR=(GR[0] + 0.04, 38)), "smooth"),
], hit=8, reach=0.6, aim=("kneeR", "chin"), react="hit_up", striker="R", shift=True,
   note="the folded man: both hands on his head, pulled down onto the rising knee")

CLIPS["overhand"] = clip(22, [
    (0, GUARD, "lin"),
    (4, G(hz=0.6, spine=172, chest=164, head=162, tw_hips=-16, tw_chest=-30, clR=(-10, 20), uaR=40, faR=210, haR=210,
          fR=(GR[0], 30)), "in"),                                                                             # the rear shoulder drops, loading
    (7, G(hx=0.06, hz=0.62, spine=170, chest=166, tw_hips=0, tw_chest=-6, uaR=150, faR=172, haR=170, fiR=170, clR=(10, 30),
          fL=(GL[0] + 0.12, 0.05, -8), fR=(GR[0], 50)), "in"),                                                # the arm loops up and over
    (10, G(hx=0.14, hz=0.58, spine=156, spine_mid=150, chest=144, neck=146, head=158, tw_hips=26, tw_spine=22, tw_chest=40,
           hR=(0.86, 1.04), twR=70, fiR=170, clR=(30, 10), uaL=40, faL=168, fL=(GL[0] + 0.16, 0), fR=(GR[0], 78)), "in3"),   # HIT: down onto the temple, the whole body falling behind it
    (13, G(hx=0.16, hz=0.57, spine=154, chest=142, tw_hips=30, tw_chest=46, hR=(0.76, 0.86), fiR=170,
           fL=(GL[0] + 0.16, 0), fR=(GR[0] + 0.04, 70)), "out"),
    (22, P(GUARD, hx=0.16, fL=(GL[0] + 0.16, 0), fR=(GR[0] + 0.16, 38)), "smooth"),
], hit=10, reach=1.1, aim=("fistR", "chin"), react="hit_chin", striker="R", shift=True,
   note="the overhand: the rear shoulder drops, the arm loops over the top, the body falls in behind it")

CLIPS["head_kick"] = clip(26, [
    (0, GUARD, "lin"),
    (3, G(hz=0.63, fL=(GL[0] + 0.06, 0.05, -10), fR=(GR[0], 50), tw_hips=-10), "in"),                        # a step out with the lead foot
    (6, G(hz=0.64, spine=190, chest=186, neck=174, head=170, tw_hips=40, tw_spine=20, tw_chest=10, fL=(GL[0] + 0.08, 60),
          thR=74, shR=-50, fpR=40, uaL=80, faL=150, uaR=-20, faR=40), "in"),                                  # the lead heel turns, the knee chambers
    (9, G(hz=0.66, spine=208, spine_mid=210, chest=206, neck=184, head=168, tw_hips=80, tw_spine=40, tw_chest=20, fL=(GL[0] + 0.08, 80),
          thR=124, shR=120, fpR=60, uaL=90, faL=150, uaR=-60, faR=-20), "in3"),                               # HIT: the hips over, the shin through his head
    (13, G(hz=0.65, spine=200, chest=196, head=170, tw_hips=60, tw_chest=14, fL=(GL[0] + 0.08, 60),
           thR=96, shR=30, fpR=40, uaR=-30, faR=20), "out"),                                                  # follow-through, the leg folding
    (18, G(hz=0.62, tw_hips=0, fL=(GL[0] + 0.08, 0), fR=(GR[0] + 0.04, 0.1, 20)), "in"),
    (26, P(GUARD, hx=0.04, fL=(GL[0] + 0.08, 0), fR=(GR[0] + 0.04, 38)), "smooth"),
], hit=9, reach=0.8, aim=("shinR", "chin"), react="hit_chin", striker="R", shift=True,
   note="the head kick: a step out, the heel turns, the hips go over, the shin through his head")

# ---------------------------------------------------------------- the man: reactions that read
# held long and exaggerated: the opening has to be readable on a phone at a glance
COVER_HIGH = G(hz=0.6, spine=166, spine_mid=160, chest=154, neck=146, head=152, tw_hips=-4, tw_chest=-2,
               hL=(0.16, 1.14), hR=(0.13, 1.11), yw_uaL=-10, yw_uaR=10, fiL=170, fiR=170, clL=(20, 20), clR=(20, 20),
               fL=(GL[0], 0), fR=(GR[0], 30))
CLIPS["cover_high"] = clip(40, [
    (0, COVER_HIGH, "smooth"),
    (10, P(COVER_HIGH, hz=0.595, head=150, tw_chest=6), "smooth"),
    (20, P(COVER_HIGH, hz=0.602, tw_chest=-6), "smooth"),
    (30, P(COVER_HIGH, hz=0.597, head=154), "smooth"),
    (40, COVER_HIGH, "smooth"),
], loop=True, breath=True, breaths=2, note="the high shell: hands glued to his head, elbows tight: THE BODY IS OPEN")
COVER_LOW = G(hz=0.6, spine=160, spine_mid=152, chest=144, neck=168, head=184, tw_hips=-6,
              hL=(0.24, 0.74), hR=(0.2, 0.72), yw_uaL=-14, yw_uaR=14, fiL=170, fiR=170, clL=(16, -8), clR=(16, -8),
              fL=(GL[0], 0), fR=(GR[0], 30))
CLIPS["cover_low"] = clip(40, [
    (0, COVER_LOW, "smooth"),
    (10, P(COVER_LOW, hz=0.597, head=186, tw_head=6), "smooth"),
    (20, P(COVER_LOW, hz=0.603, head=182, tw_head=-6), "smooth"),
    (30, P(COVER_LOW, hz=0.598, head=185), "smooth"),
    (40, COVER_LOW, "smooth"),
], loop=True, breath=True, breaths=2, note="the low guard: elbows down over his belly, the chin up: THE HEAD IS OPEN")

FOLD = G(hx=-0.05, hz=0.52, spine=136, spine_mid=124, chest=112, neck=108, head=114,
         hL=(0.16, 0.6), hR=(0.18, 0.58), fiL=150, fiR=150, clL=(28, -10), clR=(28, -10),
         fL=(0.2, 0), fR=(-0.18, 20))
CLIPS["fold"] = clip(44, [
    (0, GUARD, "lin"),
    (2, P(FOLD, hz=0.56, spine=146, chest=126, head=126), "out3"),
    (5, FOLD, "out"),                                                                                          # doubled over, face to the floor
    (14, P(FOLD, hz=0.51, spine=134, chest=110, head=112, tw_chest=6), "smooth"),
    (24, P(FOLD, hz=0.52, spine=137, chest=113, head=116, tw_chest=-4), "smooth"),                             # gasping
    (32, P(FOLD, hz=0.54, spine=144, chest=124, head=130), "smooth"),
    (44, REC, "smooth"),
], breath=True, breaths=4, note="FOLDED over a body shot, arms round it, his face low: KNEE HIM")

HEADBACK = G(hx=-0.08, hz=0.63, spine=188, spine_mid=192, chest=198, neck=210, head=236, tw_head=10,
             uaL=104, faL=170, uaR=76, faR=150, fiL=30, fiR=30, clL=(-14, 24), clR=(-14, 24),
             fL=(0.16, 0), fR=(-0.26, 30))
CLIPS["head_back"] = clip(36, [
    (0, GUARD, "lin"),
    (2, P(HEADBACK, hx=-0.04, head=226), "out3"),
    (5, HEADBACK, "out"),                                                                                      # the head thrown back, arms wide
    (14, P(HEADBACK, hx=-0.1, head=232, tw_head=-8, uaL=96, uaR=70), "smooth"),
    (24, P(HEADBACK, hx=-0.1, head=214, chest=188, uaL=80, faL=150, uaR=50, faR=140), "smooth"),
    (36, REC, "smooth"),
], note="HEAD BACK, arms flung wide, looking at the ceiling: THE BODY IS OPEN")

OVER = G(hx=0.14, hz=0.58, spine=146, spine_mid=140, chest=134, neck=140, head=156, tw_chest=30, tw_hips=20,
         hR=(0.82, 0.96), twR=90, fiR=170, uaL=10, faL=60, fiL=60, fL=(0.5, 0), fR=(GR[0], 70))
CLIPS["overreach"] = clip(32, [
    (0, GUARD, "lin"),
    (3, OVER, "out3"),                                                                                         # the punch hits air, he falls in after it
    (12, P(OVER, hx=0.18, spine=142, chest=130, head=152, tw_chest=36, fR=(GR[0] + 0.06, 0.08, 40)), "smooth"),   # off balance, a foot lifting
    (20, P(OVER, hx=0.2, spine=150, chest=142, fR=(GR[0] + 0.12, 30)), "smooth"),
    (32, REC, "smooth"),
], note="HIS PUNCH MISSED: stretched out, falling in after it, off balance: COUNTER")

BUCKLE = G(hx=-0.02, hz=0.46, spine=160, spine_mid=154, chest=148, neck=146, head=150,
           hL=(0.24, 0.48), fiL=60, uaR=50, faR=120, fiR=90, fL=(0.18, 0), fR=(-0.2, 10), tw_hips=14)
CLIPS["leg_buckle"] = clip(40, [
    (0, GUARD, "lin"),
    (2, P(BUCKLE, hz=0.54, head=158), "out3"),
    (5, BUCKLE, "out"),                                                                                        # the leg gives, a hand to the thigh, the head low
    (16, P(BUCKLE, hz=0.45, head=148, tw_head=8), "smooth"),
    (28, P(BUCKLE, hz=0.5, head=154, spine=166), "smooth"),
    (40, REC, "smooth"),
], note="THE LEG GIVES: a hand on the thigh, the head dropped low: HEAD KICK")

CLIPS["wall_slump"] = clip(80, [
    (0, GUARD, "lin"),
    (3, G(hx=-0.24, hz=0.64, spine=192, spine_mid=196, chest=198, neck=200, head=210, uaL=90, faL=150, uaR=70, faR=140,
          fiL=30, fiR=30, fL=(0.0, 0), fR=(-0.3, 10)), "out3"),                                                # slammed into the wall
    (14, G(hx=-0.26, hz=0.42, spine=188, chest=190, neck=186, head=176, uaL=30, faL=40, uaR=20, faR=30,
           fL=(0.12, 0), fR=(-0.12, 0)), "smooth"),                                                            # sliding down it
    (26, G(hx=-0.26, hz=0.2, spine=196, spine_mid=196, chest=194, neck=170, head=150, uaL=10, faL=20, uaR=4, faR=14, fiL=30, fiR=30,
           thL=76, shL=88, fpL=-10, thR=70, shR=80, fpR=-10), "smooth"),                                       # sat, the head down
    (52, G(hx=-0.26, hz=0.21, spine=194, chest=192, neck=172, head=156, uaL=12, faL=24, uaR=6, faR=16, fiL=30, fiR=30,
           thL=78, shL=88, fpL=-10, thR=72, shR=80, fpR=-10), "smooth"),
    (64, G(hx=-0.2, hz=0.4, spine=170, chest=166, head=160, hL=(-0.18, 0.5), fiL=20, fL=(0.08, 20), fR=(-0.14, 40)), "in"),   # dragging himself up the wall
    (80, REC, "smooth"),
], note="PINNED: driven into the wall, slid down it, dragging himself back up")

# ---------------------------------------------------------------- CROSSFIRE (two men on you)
# The hero at 0; A in front at +D facing him, B behind at -D facing him. Each man's clip is in his
# own frame (he faces +x, the hero D in front of him, the other man 2D in front).
D = 0.6
DUCK = P(GUARD, hz=0.4, spine=150, spine_mid=144, chest=138, neck=140, head=150, hL=(0.22, 0.96), hR=(0.16, 0.92),
         fL=(0.3, 0), fR=(-0.3, 30))
CLIPS["crossfire"] = dict(frames=64, hit=7, load=None, loop=False, aim=None, reach=D, react=None, striker=None, shift=False,
                          breath=False, breaths=2, lag=True, victim="crossfire_a_v", vat=D,
                          note="CROSSFIRE: under both their punches, which land on each other; up between them, palm and elbow; "
                               "the push kick in front, the back kick behind",
                          keys=[
    (0, GUARD, "lin"),
    (3, P(DUCK, hz=0.46, head=156), "in"),
    (6, DUCK, "in3"),                                                                                           # under it: their punches cross over him
    (9, P(DUCK, hz=0.42, head=160), "smooth"),
    (13, G(hz=0.62, spine=176, chest=172, head=172, hL=(0.48, 1.0), fiL=20, haL=110, twL=-60,
           uaR=-84, faR=96, fiR=170, yw_uaR=10, fL=(0.3, 0), fR=(-0.3, 50)), "out3"),                          # IMPACT 2: up between them, the palm in front, the elbow behind
    (17, G(hz=0.62, spine=178, chest=174, hL=(0.44, 1.0), fiL=20, uaR=-80, faR=100, fiR=170, fL=(0.3, 0), fR=(-0.3, 46)), "smooth"),
    (22, G(hz=0.64, spine=186, chest=184, head=172, thL=82, shL=-40, fpL=30, fR=(-0.22, 30), uaL=60, faL=140, uaR=30, faR=150), "in"),   # chamber
    (26, G(hz=0.66, spine=196, spine_mid=198, chest=198, neck=178, head=170, thL=92, shL=92, fpL=-10, fR=(-0.22, 50),
           uaL=40, faL=120, uaR=50, faR=150), "in3"),                                                          # IMPACT 3: the push kick, the heel through his middle
    (30, G(hz=0.62, spine=180, chest=176, fL=(0.36, 0), fR=(-0.22, 30)), "out"),
    (34, G(hz=0.62, spine=160, chest=150, neck=152, head=156, tw_head=60, fL=(0.3, 30), thR=-10, shR=-80, fpR=20,
           uaL=50, faL=150, uaR=40, faR=150), "in"),                                                          # weight forward, eyes over the shoulder, the knee chambered back
    (38, G(hz=0.64, spine=138, spine_mid=134, chest=128, neck=136, head=146, tw_head=70, fL=(0.3, 60),
           thR=-94, shR=-92, fpR=-60, uaL=30, faL=120, uaR=60, faR=160), "in3"),                               # IMPACT 4: the back kick, the heel straight back into him
    (43, G(hz=0.62, spine=158, chest=150, head=160, tw_head=30, fL=(0.3, 30), fR=(-0.24, 0.1, 20)), "out"),
    (52, G(hz=0.64, uaL=40, faL=100, uaR=20, faR=90, fiL=90, fiR=90, head=176, fL=(0.3, 0), fR=(-0.2, 20)), "smooth"),   # standing between them, the hands low a beat
    (64, P(GUARD, fL=(0.3, 0), fR=(-0.2, 38)), "smooth"),
])


def _crossfire_man(kicked, flies):
    """One of the two men, in his own frame: wind-up, the lunging punch that lands on the other man,
    rocked by the other's, rocked again by the palm / elbow, then kicked away and down."""
    k = kicked
    return [
        (0, G(uaR=10, faR=200, haR=200, tw_chest=-24, tw_hips=-14, fR=(GR[0], 30)), "smooth"),                  # wound up
        (4, G(hx=0.1, spine=166, chest=156, head=164, tw_hips=24, tw_chest=36, hR=(1.06, 1.08), twR=90, fiR=170,
              fL=(GL[0] + 0.14, 0), fR=(GR[0], 70)), "in3"),                                                     # the punch: over the ducked hero ...
        (7, G(hx=0.12, spine=170, chest=166, neck=190, head=222, tw_hips=20, tw_chest=30, hR=(1.02, 1.12), fiR=170,
              uaL=70, faL=120, fL=(GL[0] + 0.14, 0), fR=(GR[0], 60)), "out3"),                                    # ... and into the other man's face as his comes in
        (11, G(hx=0.04, spine=184, chest=188, neck=196, head=214, uaL=90, faL=150, uaR=60, faR=130, fiL=40, fiR=40,
               fL=(GL[0] + 0.1, 0), fR=(GR[0] - 0.04, 30)), "out"),
        (13, G(hx=-0.02, spine=188, chest=194, neck=200, head=226, uaL=100, faL=160, uaR=70, faR=150, fiL=30, fiR=30,
               fL=(GL[0] + 0.06, 0), fR=(GR[0] - 0.08, 20)), "out3"),                                             # the palm / the elbow
        (k - 2, G(hx=-0.02, spine=176, chest=172, neck=168, head=176, uaL=60, faL=110, uaR=40, faR=100, fiL=60, fiR=60,
                  fL=(GL[0] + 0.06, 0), fR=(GR[0] - 0.08, 30)), "smooth"),                                        # staggering, trying to get his hands back up
        (k, G(hx=-0.12, hz=0.6, spine=150, spine_mid=140, chest=130, neck=130, head=140, uaL=40, faL=80, uaR=30, faR=70,
              fL=(GL[0] + 0.06, 0.1, 20), fR=(GR[0] - 0.08, 40)), "out3"),                                        # KICKED: folded round the heel ...
        (k + 6, G(hx=-flies * 0.6, hz=0.6, spine=196, chest=204, neck=206, head=214, uaL=120, faL=160, uaR=110, faR=150,
                  fiL=30, fiR=30, fL=(-flies * 0.6 + 0.3, 0.3, 30), fR=(-flies * 0.6 - 0.1, 0.25, 30)), "out"),   # ... and off his feet
        (k + 12, P(LIE, hx=-flies, hz=0.12), "in"),
        (64, P(LIE, hx=-flies), "smooth"),
    ]


CLIPS["crossfire_a_v"] = dict(frames=64, hit=None, load=None, loop=False, aim=None, reach=None, react=None, striker=None,
                              shift=True, breath=False, breaths=2, lag=True, note="crossfire: the man in front",
                              keys=_crossfire_man(26, 1.5))
CLIPS["crossfire_b_v"] = dict(frames=64, hit=None, load=None, loop=False, aim=None, reach=None, react=None, striker=None,
                              shift=True, breath=False, breaths=2, lag=True, note="crossfire: the man behind",
                              keys=_crossfire_man(38, 1.2))


# ---------------------------------------------------------------- CROSSFIRE, the four (docs/design/crossfire.md)
# Two men on him, one in front (A, +D) and one behind (B, -D). A two-thumb tap picks the move: each thumb
# on its man's side, HIGH = his head, LOW = his legs / body. The game mirrors front / back to the screen.
#   HIGH + HIGH          crossfire      THE CROSSING (above): they hit each other
#   LOW + LOW            cf_knees       BOTH KNEES: the stamp, the mule kick, the knee, the heel
#   front HIGH, back LOW cf_split       BOTH AT ONCE: the cross and the back kick on the same frame
#   front LOW, back HIGH cf_into        ONE INTO THE OTHER: the back elbow, the liver hook, over the hip into him
from v4_clinch import KNEEL, PRONE


def _cf(name, frames, hero, a, b, hit, note):
    """The hero's clip and his two men's (name_a_v in front, name_b_v behind), each in his own frame."""
    CLIPS[name] = dict(frames=frames, hit=hit, load=None, loop=False, aim=None, reach=D, react=None, striker=None, shift=False,
                       breath=False, breaths=2, lag=True, victim=name + "_a_v", vat=D, note=note, keys=hero)
    for tag, keys, who in (("_a_v", a, "the man in front"), ("_b_v", b, "the man behind")):
        CLIPS[name + tag] = dict(frames=frames, hit=None, load=None, loop=False, aim=None, reach=None, react=None, striker=None,
                                 shift=True, breath=False, breaths=2, lag=True, note=note.split(":")[0] + ": " + who, keys=keys)


WIND = G(uaR=10, faR=200, haR=200, tw_chest=-24, tw_hips=-14, fR=(GR[0], 30))          # a man winding up on him
LUNGE = G(hx=0.08, spine=168, chest=160, head=166, tw_hips=16, tw_chest=24, uaR=60, faR=110, fiR=170,
          fL=(GL[0] + 0.1, 0), fR=(GR[0], 60))                                          # coming in with it
KNEEL_L = P(KNEEL, hx=0.02, spine=160, spine_mid=150, chest=144, neck=142, head=150, hL=(0.24, 0.42), fiL=60, uaR=30, faR=70)


# ---- BOTH KNEES (LOW + LOW): Wick's stamp through the front man's knee; the same leg re-chambered and a
# mule kick back into the other's; both drop to a knee, their heads now at his hip; the front man's head
# pulled down onto the knee; and the back heel into the other one's face.
_cf("cf_knees", 54, [
    (0, GUARD, "lin"),
    (3, G(hz=0.63, spine=178, chest=172, thL=72, shL=8, fpL=10, fR=(GR[0], 24)), "in"),                         # the lead knee comes up
    (6, G(hx=0.06, hz=0.61, spine=184, chest=182, head=168, fL=(0.5, 0.26, -20), fR=(GR[0], 30)), "in3"),     # HIT 1: the stamp through his knee
    (9, G(hx=0.04, fL=(0.28, 0), fR=(GR[0], 34), spine=168, chest=160, head=160, tw_head=30), "out"),         # down, a look back over the shoulder
    (12, G(hx=0.06, hz=0.6, spine=146, spine_mid=140, chest=134, neck=136, head=144, tw_head=46, fL=(0.28, 20),
           thR=-52, shR=-64, fpR=-30, uaL=50, faL=140, uaR=40, faR=150), "in3"),                               # HIT 2: the mule kick into the other knee
    (15, G(hx=0.04, spine=166, chest=160, head=162, tw_head=20, fL=(0.28, 0), fR=(GR[0] + 0.02, 30)), "out"),
    (18, G(hx=0.1, hz=0.62, spine=160, chest=150, neck=148, head=156, fL=(0.32, 0), fR=(GR[0] + 0.04, 40),
           hL=(0.46, 0.74), hR=(0.44, 0.72), fiL=110, fiR=110), "in"),                                         # both hands on the kneeling man's head
    (20, G(hx=0.12, hz=0.63, spine=164, chest=154, neck=150, head=158, fL=(0.32, 10), thR=40, shR=-70, fpR=20,
           hL=(0.42, 0.66), hR=(0.4, 0.64), fiL=120, fiR=120), "in"),                                         # pulled down, the knee loading
    (23, G(hx=0.16, hz=0.65, spine=172, chest=166, neck=160, head=164, fL=(0.32, 34), thR=104, shR=-16, fpR=40,
           hL=(0.38, 0.62), hR=(0.36, 0.6), fiL=130, fiR=130), "in3"),                                         # HIT 3: the knee into his face
    (27, G(hx=0.1, spine=170, chest=164, fL=(0.32, 0), fR=(GR[0] + 0.04, 30), uaL=60, faL=140, uaR=30, faR=160), "out"),
    (30, G(hx=0.08, spine=162, chest=154, neck=150, head=150, tw_head=60, fL=(0.3, 30), thR=40, shR=-60, fpR=20), "in"),   # the other one: eyes back, the knee chambered
    (33, G(hx=0.08, hz=0.62, spine=138, spine_mid=132, chest=126, neck=128, head=138, tw_head=66, fL=(0.3, 50),
           thR=-76, shR=-86, fpR=-20, uaL=40, faL=120, uaR=60, faR=140), "in3"),                               # HIT 4: the heel straight back into his face
    (37, G(hx=0.06, spine=160, chest=152, head=160, tw_head=30, fL=(0.3, 30), fR=(GR[0] + 0.02, 0.1, 20)), "out"),
    (44, G(hx=0.04, uaL=40, faL=100, uaR=20, faR=90, fiL=90, fiR=90, head=174, fL=(0.28, 0), fR=(GR[0], 24)), "smooth"),   # between them, the hands low a beat
    (54, P(GUARD, fL=(0.28, 0), fR=(GR[0], 38)), "smooth"),
], [
    (0, WIND, "smooth"),
    (4, LUNGE, "in"),
    (6, G(hx=0.06, hz=0.52, spine=164, chest=158, neck=150, head=150, uaR=50, faR=90, fiR=60, uaL=60, faL=90,
          fL=(GL[0] + 0.08, 30), fR=(GR[0], 40)), "out3"),                                                    # STAMPED: the lead knee bends the wrong way
    (10, P(KNEEL_L, hx=0.0, head=144), "out"),                                                               # down on a knee
    (17, P(KNEEL_L, hx=0.0, head=156, uaL=60, faL=110, uaR=50, faR=100), "smooth"),                         # trying to get up ...
    (20, P(KNEEL_L, hx=0.05, spine=150, chest=136, neck=130, head=132, uaL=70, faL=120, uaR=60, faR=110), "in"),   # ... his head pulled down
    (23, P(KNEEL_L, hx=0.0, spine=186, spine_mid=192, chest=198, neck=210, head=232, uaL=120, faL=160, uaR=110, faR=150,
           fiL=30, fiR=30), "out3"),                                                                          # KNEED: the head snapped back
    (28, P(LIE, hx=-0.36, hz=0.3, hips=220, spine=228, chest=234, head=240, thL=60, shL=-20, thR=40, shR=-40), "in"),
    (32, P(LIE, hx=-0.7, hz=0.08), "in3"),                                                                   # on his back
    (35, P(LIE, hx=-0.72, hz=0.12, chest=262, head=258), "out"),
    (54, P(LIE, hx=-0.72), "smooth"),
], [
    (0, WIND, "smooth"),
    (6, LUNGE, "in"),
    (9, P(LUNGE, hx=0.1, spine=162, chest=152), "smooth"),
    (12, G(hx=0.08, hz=0.5, spine=160, chest=152, neck=148, head=150, uaR=40, faR=80, fiR=60, uaL=50, faL=80,
           fL=(GL[0] + 0.1, 30), fR=(GR[0], 40)), "out3"),                                                   # MULE-KICKED: the knee goes
    (16, P(KNEEL_L, hx=0.0, head=146), "out"),                                                                # down on a knee
    (28, P(KNEEL_L, hx=0.0, head=164, spine=168, chest=160, uaL=60, faL=120, uaR=50, faR=110), "smooth"),    # looking up, getting a foot under him
    (33, P(KNEEL_L, hx=-0.02, spine=190, spine_mid=196, chest=204, neck=214, head=236, uaL=120, faL=160, uaR=110, faR=150,
           fiL=30, fiR=30), "out3"),                                                                          # HEELED in the face
    (38, P(LIE, hx=-0.4, hz=0.3, hips=220, spine=228, chest=234, head=240, thL=60, shL=-20, thR=40, shR=-40), "in"),
    (42, P(LIE, hx=-0.75, hz=0.08), "in3"),
    (45, P(LIE, hx=-0.77, hz=0.12, chest=262, head=258), "out"),
    (54, P(LIE, hx=-0.77), "smooth"),
], 6, "BOTH KNEES: the stamp through the front man's knee, the mule kick into the other's; the knee to one face, the heel to the other")


# ---- BOTH AT ONCE (front HIGH, back LOW): the straight right to the front man's jaw and the back kick
# into the other man's gut on the same frame: a star frozen in the air. The front man goes over stiff like
# a felled tree; the other folds round the heel and goes down on his face.
_STAR = G(hx=0.06, hz=0.62, spine=146, spine_mid=140, chest=136, neck=140, head=154, tw_hips=16, tw_chest=24,
          hR=(0.64, 1.0), twR=90, fiR=170, uaL=44, faL=150, fiL=165,
          fL=(0.3, 40), thR=-82, shR=-88, fpR=-20)
_cf("cf_split", 50, [
    (0, GUARD, "lin"),
    (3, G(hx=0.04, hz=0.62, spine=170, chest=164, head=166, tw_head=24, fL=(0.3, 20), thR=30, shR=-70, fpR=20,
          tw_chest=-18, uaR=16, faR=170), "in"),                                                              # the weight onto the lead, the heel chambered, the right cocked
    (6, P(_STAR, spine=156, chest=150, thR=-50, shR=-70, hR=(0.5, 1.0)), "in"),
    (8, _STAR, "in3"),                                                                                         # HIT: both at once
    (13, P(_STAR, hR=(0.63, 1.0), thR=-80, shR=-87), "lin"),                                                   # held: the frame frozen on it
    (17, G(hx=0.04, spine=164, chest=158, head=164, fL=(0.3, 30), thR=30, shR=-70, fpR=20, uaR=40, faR=150), "out"),
    (21, G(hx=0.04, fL=(0.3, 10), fR=(GR[0] + 0.02, 30), head=170, tw_head=-30), "out"),                     # down, watching the one behind go
    (34, G(hx=0.04, fL=(0.3, 0), fR=(GR[0] + 0.02, 34), head=172, tw_head=10, uaL=50, faL=120, uaR=20, faR=120), "smooth"),
    (50, P(GUARD, fL=(0.3, 0), fR=(GR[0] + 0.02, 38)), "smooth"),
], [
    (0, WIND, "smooth"),
    (5, LUNGE, "in"),
    (8, G(hx=0.06, spine=180, spine_mid=186, chest=190, neck=204, head=226, uaL=80, faL=110, uaR=60, faR=100,
          fiL=60, fiR=60, fL=(GL[0] + 0.1, 0), fR=(GR[0], 40)), "out3"),                                    # ON THE JAW: the head snaps round
    (13, G(hx=0.04, spine=182, spine_mid=184, chest=186, neck=190, head=196, uaL=12, faL=14, uaR=8, faR=10,
           fiL=30, fiR=30, fL=(GL[0] + 0.1, 0), fR=(GR[0], 20)), "out"),                                    # out on his feet: the arms drop dead
    (20, G(hx=-0.08, hz=0.6, hips=204, spine=206, spine_mid=206, chest=206, neck=208, head=210, uaL=14, faL=16, uaR=10, faR=12,
           fiL=30, fiR=30, fL=(GL[0] + 0.1, 0), fR=(GR[0], 0)), "in"),                                       # and over, stiff, like a felled tree
    (26, P(LIE, hx=-0.4, hz=0.36, hips=236, spine=238, chest=240, neck=240, head=242, uaL=20, faL=24, uaR=14, faR=18,
           thL=120, shL=120, thR=116, shR=116), "in"),
    (30, P(LIE, hx=-0.66, hz=0.074, uaL=40, faL=44, uaR=30, faR=34, thL=94, shL=94, thR=90, shR=90), "in3"),   # flat out, the back of his head bouncing
    (33, P(LIE, hx=-0.66, hz=0.11, head=262, chest=264), "out"),
    (50, P(LIE, hx=-0.66), "smooth"),
], [
    (0, WIND, "smooth"),
    (5, LUNGE, "in"),
    (8, P(FOLD, hx=-0.06, hz=0.56, spine=140, chest=118, head=120), "out3"),                                  # THE HEEL IN HIS GUT: folded round it
    (14, P(FOLD, hx=-0.2, hz=0.5, fL=(0.08, 0), fR=(-0.34, 20)), "out"),                                      # staggering back, doubled
    (22, P(FOLD, hx=-0.2, hz=0.36, spine=136, chest=112, head=110, thL=40, shL=-90, fpL=-60, thR=20, shR=-100, fpR=-60), "in"),   # the knees go
    (28, P(PRONE, hx=-0.1, hz=0.2, hips=120, spine=110, chest=104, head=104, thL=-40, shL=-90, thR=-40, shR=-90), "in"),
    (31, P(PRONE, hx=-0.06, hz=0.08), "in3"),                                                                 # on his face
    (50, P(PRONE, hx=-0.06), "smooth"),
], 8, "BOTH AT ONCE: the cross to the front man's jaw and the back kick into the other's gut, on the same frame")


# ---- ONE INTO THE OTHER (front LOW, back HIGH): the back elbow into the face behind; the liver hook in
# front; the front man folds into him, loaded on the hip (o-goshi) and thrown backwards, over, into the
# other man: both down in a heap.
_cf("cf_into", 56, [
    (0, GUARD, "lin"),
    (3, G(hx=0.02, spine=172, chest=166, head=166, tw_head=40, tw_chest=12, uaR=30, faR=150, fR=(GR[0], 30)), "in"),   # eyes back
    (5, G(hx=-0.04, spine=176, chest=172, head=168, tw_head=46, tw_chest=-20, tw_hips=-10, uaR=-86, faR=96, fiR=170, yw_uaR=10,
          fL=(GL[0], 10), fR=(GR[0] - 0.04, 40)), "in3"),                                                      # HIT 1: the rear elbow back into his face
    (8, G(hz=0.585, spine=170, spine_mid=164, chest=158, neck=150, head=164, tw_hips=-20, tw_chest=-22, uaL=40, faL=120,
          fL=(GL[0], 10), fR=(GR[0], 44)), "in"),                                                              # round, the level drops
    (11, G(hz=0.57, spine=166, spine_mid=160, chest=154, neck=148, head=162, tw_hips=12, tw_spine=10, tw_chest=26,
           hL=(0.6, 0.76), yw_uaL=-34, yw_faL=-50, twL=-10, fiL=170, fL=(GL[0], 34), fR=(GR[0], 40)), "in3"),  # HIT 2: the liver hook
    (14, G(hx=0.1, hz=0.6, tw_hips=-60, tw_spine=-70, tw_chest=-80, chest=160, fL=(0.36, 20), fR=(0.16, 30), liftR=0.04,
           hL=(0.5, 0.86), hR=(0.46, 0.8), fiL=110, fiR=110), "in"),                                         # he folds into him: in, turning his back
    (17, G(hx=0.18, hz=0.56, tw_hips=-150, tw_spine=-160, tw_chest=-170, spine=150, spine_mid=140, chest=126, neck=130, head=140,
           fL=(0.32, 10), fR=(0.12, 10), hL=(0.44, 0.88), hR=(0.38, 0.78), fiL=120, fiR=120), "in"),         # loaded on the hip
    (20, G(hx=0.16, hz=0.52, tw_hips=-160, tw_chest=-178, spine=128, spine_mid=118, chest=104, neck=104, head=116,
           hL=(0.2, 0.6), hR=(0.14, 0.5), fL=(0.32, 0), fR=(0.12, 0)), "in3"),                                  # the pull over, bent double
    (23, G(hx=0.12, hz=0.56, tw_hips=-150, tw_chest=-170, spine=134, chest=112, neck=110, head=122, hL=(-0.24, 0.32), hR=(-0.28, 0.28),
           fiL=60, fiR=60, fL=(0.32, 0), fR=(0.12, 0)), "out"),                                                # HIT 3: down onto the other one
    (30, G(hx=0.1, hz=0.6, tw_hips=-90, tw_chest=-100, spine=160, chest=146, neck=144, head=152, uaL=40, faL=110, uaR=30, faR=100,
           fL=(0.32, 0), fR=(0.12, 0)), "out"),
    (42, G(hx=0.08, hz=0.63, tw_hips=-30, tw_chest=-20, head=170, tw_head=-20, uaL=50, faL=120, uaR=20, faR=110,
           fL=(0.3, 0), fR=(GR[0] + 0.04, 30)), "smooth"),                                                     # turning back round, looking down at them
    (56, P(GUARD, fL=(0.3, 0), fR=(GR[0] + 0.04, 38)), "smooth"),
], [
    (0, WIND, "smooth"),
    (6, LUNGE, "in"),
    (11, P(FOLD, hx=0.1, hz=0.56, spine=140, chest=118, head=118), "out3"),                                   # THE LIVER: folded, falling in on him
    (14, P(FOLD, hx=0.16, hz=0.58, spine=146, chest=126, head=126, uaL=60, faL=80, uaR=60, faR=80), "in"),
    (17, G(hx=0.26, hz=0.72, spine=150, spine_mid=140, chest=128, neck=124, head=130, fL=(0.44, 0.14, 40), fR=(0.04, 0.1, 40),
           uaL=60, faL=80, uaR=70, faR=90), "in"),                                                            # loaded, his feet leaving the floor
    (20, G(hx=0.72, hz=1.12, hips=60, spine=50, spine_mid=40, chest=30, neck=24, head=20, thL=190, shL=200, thR=170, shR=190,
           uaL=40, faL=20, uaR=20, faR=0, fiL=30, fiR=30), "in3"),                                            # over the top, upside down
    (22, G(hx=1.16, hz=0.62, hips=-40, spine=-48, spine_mid=-54, chest=-60, neck=-66, head=-70, thL=170, shL=160, thR=150, shR=140,
           uaL=150, faL=160, uaR=130, faR=150), "in"),                                                        # down onto the other man
    (24, P(LIE, hx=1.34, hz=0.12, thL=110, shL=100, thR=96, shR=90), "in3"),
    (27, P(LIE, hx=1.36, hz=0.16, chest=262, head=256, thL=100, shL=84), "out"),
    (56, P(LIE, hx=1.36), "smooth"),
], [
    (0, WIND, "smooth"),
    (3, LUNGE, "in"),
    (5, G(hx=0.02, spine=188, spine_mid=192, chest=198, neck=210, head=234, tw_head=10, uaL=104, faL=170, uaR=76, faR=150,
          fiL=30, fiR=30, fL=(GL[0] + 0.06, 0), fR=(GR[0], 30)), "out3"),                                    # ELBOWED: the head snapped back
    (10, G(hx=-0.1, spine=172, chest=166, neck=168, head=184, hL=(0.14, 1.0), hR=(0.16, 0.98), fiL=60, fiR=60,
           fL=(GL[0] - 0.02, 0), fR=(GR[0] - 0.08, 30)), "out"),                                             # reeling, the hands to his face
    (17, G(hx=-0.12, spine=170, chest=164, head=172, hL=(0.18, 1.0), hR=(0.2, 0.98), fiL=80, fiR=80,
           fL=(GL[0] - 0.04, 0), fR=(GR[0] - 0.1, 30)), "smooth"),
    (21, G(hx=-0.12, spine=182, chest=186, head=180, uaL=120, faL=150, uaR=110, faR=140, fiL=30, fiR=30,
           fL=(GL[0] - 0.04, 0), fR=(GR[0] - 0.1, 30)), "in"),                                               # the other man coming over at him: hands up
    (23, G(hx=-0.34, hz=0.42, hips=206, spine=214, chest=224, neck=226, head=232, uaL=130, faL=160, uaR=120, faR=150,
           fL=(GL[0] - 0.2, 0.1, 30), fR=(GR[0] - 0.3, 0.1, 30)), "out3"),                                   # FLATTENED under him
    (26, P(LIE, hx=-0.62, hz=0.074), "in3"),
    (29, P(LIE, hx=-0.64, hz=0.11, chest=262, head=258), "out"),
    (56, P(LIE, hx=-0.64), "smooth"),
], 5, "ONE INTO THE OTHER: the back elbow behind, the liver hook in front, over the hip and thrown into the other man")
