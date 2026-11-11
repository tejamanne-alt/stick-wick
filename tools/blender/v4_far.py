"""Moves for when the men are FAR, for the V4 body (docs/design/combat_v3.md, "far range"):

  advance         tap at a man 3–6 m off: he comes in behind his guard (a boxer's step-drag, never
                  crossing his feet), then strikes when in reach
  rush_in         swipe towards a man 3–8 m off: two explosive step-drags, the chin down
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
from v4_clinch import HOLD, HELD, LIE, pair

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
    (5, P(LOOSE, hx=0.1, hz=0.62, spine=176, spine_mid=174, chest=170, neck=164, head=170, fL=(0.32, 0), fR=(0.0, 40),
          hR=(0.52, 1.02), hL=(0.4, 1.12), fiR=60, fiL=40), "out3"),                                             # up and in: the arm snakes round
    (9, P(LOOSE, hx=0.12, hz=0.63, spine=184, chest=186, neck=172, head=168, fL=(0.32, 0), fR=(0.0, 30),
          hR=(0.48, 1.0), hL=(0.36, 1.1), fiR=165, fiL=90, tw_chest=-6), "in3"),
    (12, P(LOOSE, hx=0.1, hz=0.62, spine=190, chest=194, neck=176, head=170, fL=(0.32, 0), fR=(-0.02, 20),
           hR=(0.44, 0.99), hL=(0.34, 1.08), fiR=165, fiL=120, clR=(10, 4), clL=(14, 8)), "in"),                  # LOCKED: the chest on his back, squeezing
    (22, P(LOOSE, hx=-0.02, hz=0.56, spine=198, spine_mid=200, chest=202, neck=180, head=172, fL=(0.34, 0), fR=(-0.26, 10),
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
    (5, P(LOOSE, hx=-0.02, spine=184, chest=186, neck=192, head=204, hL=(0.18, 0.98), hR=(0.2, 0.94), fiL=120, fiR=120,
          fL=(0.12, 20), fR=(-0.04, 30)), "out3"),                                                              # yanked back, the hands fly to the arm
    (12, P(LOOSE, hx=-0.04, hz=0.66, spine=188, chest=192, neck=196, head=208, hL=(0.1, 0.96), hR=(0.12, 0.92), fiL=140, fiR=140,
           fL=(0.3, 0.2, 20), fR=(-0.02, 40), tw_chest=12), "out"),                                              # a leg kicks up
    (18, P(LOOSE, hx=-0.06, hz=0.66, spine=190, chest=194, neck=196, head=206, hL=(0.08, 0.94), hR=(0.1, 0.9), fiL=150, fiR=150,
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
