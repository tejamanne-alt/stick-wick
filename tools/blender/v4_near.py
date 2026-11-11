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
