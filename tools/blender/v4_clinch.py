"""The clinch, hand-keyed for the V4 body: every move as a PAIR (the hero's clip and the held man's
clip, same length, same timing), so the two bodies stay together frame by frame.

Space: the hero at x 0 facing +x. The held man stands GAP (0.62) in front, facing the hero, and his
clip is keyed in HIS OWN frame (he faces +x in it too; the hero is GAP in front of him). Where he
ends up after a throw is his clip's last hx (the game moves his root there: endShift); "behind the
hero" is about GAP + 0.7 in his frame.

Moves (gestures in the game, docs/design/combat_v3.md and the clinch table):
  tap high          clinch_knee        knee to the body (alternating legs)
  tap low           thigh_knee         a dead leg: knee into the side of his thigh
  3rd tap high      plum_knee          head pulled down onto a knee to the face (THE PLUM)
  low, high         headbutt           his hands drop to the leg; forehead into his nose
  low, low          sweep              the dead leg swept out (de-ashi-barai)
  swipe towards     reap               o-soto-gari: driven back, the leg reaped, slammed down in front
  swipe away, low   hip_throw          o-goshi: over the hip, lands behind you
  swipe away, high  shoulder_throw     seoi-nage: over the shoulder, lands behind you
  swipe up          suplex / body_slam over and behind you / (worn down) lifted and slammed in front
  swipe down        ddt                front facelock, fall back, his head planted
  hold high         guillotine_in / guillotine (loop) / guillotine_drop / guillotine_let
  hold low          lock_lift / lock_hold (loop) / lock_slam (towards) / lock_suplex (away) / lock_drop (down)
  tap behind        shove              both palms into his chest: off you go
  (idle)            clinch_strain      he fights the hold"""
from v4_pose import P

GAP = 0.62

# ---------------------------------------------------------------- base poses
# the hero holding: hands cupped round the back of his neck (fingers gripping), elbows in on his
# collarbones, forehead close, hips in, the weight on the balls of the feet
HOLD = P(hx=0.04, hz=0.635, hips=180, tw_hips=-4, spine=174, spine_mid=168, chest=163, neck=168, head=174, tw_chest=-4,
         clL=(30, 14), clR=(30, 14),
         hL=(0.5, 1.07), hR=(0.5, 1.04), yw_uaL=-12, yw_uaR=12, yw_faL=-30, yw_faR=30, fiL=95, fiR=95, twL=-60, twR=60,
         fL=(0.28, 8), fR=(-0.1, 40))
# the held man: head pulled down and forward, his hands on the hero's forearms, his feet braced back
HELD = P(hx=-0.02, hz=0.59, hips=180, spine=162, spine_mid=150, chest=140, neck=126, head=130,
         clL=(26, 18), clR=(26, 18),
         hL=(0.3, 0.98), hR=(0.3, 0.95), yw_uaL=-8, yw_uaR=8, fiL=100, fiR=100,
         fL=(0.2, 0), fR=(-0.24, 26))
# on his back (the game's "lie"): head towards his own back (-x), legs towards his front
LIE = P(hx=0.0, hz=0.074, hips=270, spine=270, spine_mid=270, chest=270, neck=272, head=274,
        clL=(0, 0), clR=(0, 0), uaL=98, faL=102, uaR=84, faR=88, fiL=40, fiR=40,
        thL=100, shL=84, thR=88, shR=80, fpL=60, fpR=60)
# face down (the game's "prone"): head towards his front, arms under him
PRONE = P(hx=0.0, hz=0.08, hips=90, spine=90, spine_mid=90, chest=91, neck=95, head=100,
          uaL=12, faL=92, uaR=6, faR=96, fiL=40, fiR=40, thL=-88, shL=-90, thR=-92, shR=-90, fpL=-60, fpR=-60)
KNEEL = P(hx=0.0, hz=0.4, spine=168, spine_mid=160, chest=154, neck=148, head=150,
          uaL=20, faL=60, uaR=10, faR=50, fiL=40, fiR=40,
          thL=84, shL=4, fpL=0, thR=4, shR=-86, fpR=-70)


def H(**kw):
    return P(HOLD, **kw)


def V(**kw):
    return P(HELD, **kw)


def L(hx, **kw):
    return P(LIE, hx=hx, **kw)


def pair(name, frames, hero, victim, hit=None, note="", vshift=False, hshift=False, loop=False, breath=False):
    """Two clips: name (the hero) and name_v (the held man)."""
    h = dict(frames=frames, keys=hero, hit=hit, load=None, loop=loop, aim=None, reach=None, react=None, note=note,
             striker=None, shift=hshift, breath=breath, breaths=2, lag=True, victim=name + "_v")
    v = dict(frames=frames, keys=victim, hit=None, load=None, loop=loop, aim=None, reach=None, react=None,
             note=note + " (held)", striker=None, shift=vshift, breath=breath, breaths=2, lag=True)
    return {name: h, name + "_v": v}


CLIPS = {}

# ---------------------------------------------------------------- taking hold, holding, the struggle
CLIPS.update(pair("clinch_enter", 9, [
    (0, P(hx=0.0, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fL=(0.24, 0), fR=(-0.13, 38)), "lin"),
    (3, H(hx=0.03, hL=(0.55, 1.12), hR=(0.55, 1.1), fiL=40, fiR=40, chest=162), "in"),        # hands shoot in, open
    (5, H(hx=0.06, fiL=110, fiR=110, chest=158, head=162), "in3"),                           # they lock behind his neck
    (9, HOLD, "out"),
], [
    (0, P(HELD, spine=176, spine_mid=172, chest=166, neck=160, head=170, hz=0.64, uaL=62, faL=150, uaR=22, faR=166), "lin"),
    (4, V(spine=172, chest=162, neck=158, head=166, hz=0.63), "in"),
    (5, V(spine=166, chest=150, neck=140, head=148, hz=0.61), "in3"),                        # yanked down by the neck
    (9, HELD, "out"),
], hit=4, note="taking hold: the hands shoot in and lock behind his neck, his head comes down"))

CLIPS.update(pair("clinch", 32, [
    (0, HOLD, "smooth"),
    (8, H(hx=0.055, fR=(-0.1, 46), chest=158, tw_chest=-8), "smooth"),
    (16, H(hx=0.045, chest=161, tw_chest=2, head=166), "smooth"),
    (24, H(hx=0.05, fR=(-0.1, 36), tw_chest=-2), "smooth"),
    (32, HOLD, "smooth"),
], [
    (0, HELD, "smooth"),
    (8, V(hx=-0.01, chest=144, head=144, tw_chest=6), "smooth"),
    (16, V(hx=0.005, chest=148, head=148, tw_chest=-4, fR=(-0.24, 32)), "smooth"),
    (24, V(chest=145, tw_chest=2), "smooth"),
    (32, HELD, "smooth"),
], loop=True, breath=True, note="the hold: leaning on each other, weight shifting, both breathing hard"))

CLIPS.update(pair("clinch_strain", 24, [
    (0, HOLD, "smooth"),
    (6, H(hx=0.0, chest=166, tw_chest=10, fL=(0.28, 0), fR=(-0.1, 20), hL=(0.47, 1.09), hR=(0.47, 1.06)), "smooth"),   # dragged about
    (12, H(hx=0.07, chest=156, tw_chest=-12, fR=(-0.1, 52)), "smooth"),
    (18, H(hx=0.02, chest=164, tw_chest=8, fL=(0.28, 14)), "smooth"),
    (24, HOLD, "smooth"),
], [
    (0, HELD, "smooth"),
    (6, V(hx=-0.05, chest=154, spine=168, tw_chest=18, hL=(0.28, 1.0), hR=(0.32, 0.92), fR=(-0.28, 20)), "smooth"),    # pulling back, prying
    (12, V(hx=0.03, chest=140, tw_chest=-14, hL=(0.33, 0.95), hR=(0.28, 0.99)), "smooth"),
    (18, V(hx=-0.04, chest=152, tw_chest=10, fL=(0.18, 10)), "smooth"),
    (24, HELD, "smooth"),
], loop=True, note="he fights the hold: pulling back, prying at the arms"))

# ---------------------------------------------------------------- the knees
CLIPS.update(pair("clinch_knee", 15, [
    (0, HOLD, "smooth"),
    (2, H(hx=0.03, chest=164, hL=(0.48, 1.04), hR=(0.48, 1.01), fR=(-0.1, 52)), "in"),          # the pull: his head comes down
    (4, H(hx=0.08, hz=0.65, chest=166, spine=178, tw_hips=14, thR=70, shR=-30, fpR=30, fL=(0.28, 22),
          hL=(0.47, 0.98), hR=(0.47, 0.95)), "in"),
    (5, H(hx=0.11, hz=0.665, chest=168, spine=184, tw_hips=22, thR=114, shR=0, fpR=40, fL=(0.28, 38),
          hL=(0.46, 0.94), hR=(0.46, 0.91)), "in3"),                                              # the knee drives up, up on the toes
    (6, H(hx=0.115, hz=0.665, chest=168, spine=185, tw_hips=24, thR=118, shR=2, fpR=40, fL=(0.28, 40),
          hL=(0.46, 0.93), hR=(0.46, 0.9)), "lin"),
    (9, H(hx=0.07, hz=0.645, thR=40, shR=-40, fpR=30, fL=(0.28, 14)), "out"),
    (12, H(fR=(-0.1, 40), liftR=0.02), "out"),
    (15, HOLD, "smooth"),
], [
    (0, HELD, "smooth"),
    (2, V(head=140, neck=132, chest=142), "in"),
    (4, V(head=136, neck=128, chest=138, hz=0.595), "in"),
    (5, V(hx=-0.02, spine=158, spine_mid=144, chest=126, neck=122, head=134, hz=0.58, fiL=60, fiR=60,
          hL=(0.18, 0.78), hR=(0.18, 0.75)), "in3"),                                            # folded round the knee, hands drop
    (8, V(hx=-0.04, spine=156, spine_mid=142, chest=124, neck=120, head=132, hz=0.57, hL=(0.16, 0.76), hR=(0.16, 0.73)), "out"),
    (15, V(chest=140, neck=134, head=142), "smooth"),
], hit=5, note="knee to the body: his head pulled down, up on the toes, he folds round it"))

CLIPS.update(pair("thigh_knee", 14, [
    (0, HOLD, "smooth"),
    (2, H(hx=0.03, fR=(-0.1, 52), tw_hips=-14), "in"),
    (4, H(hx=0.07, hz=0.63, tw_hips=26, tw_chest=10, thR=58, shR=-20, yw_thR=24, yw_shR=10, fpR=30, fL=(0.28, 20)), "in3"),   # round into the side of his thigh
    (5, H(hx=0.08, hz=0.63, tw_hips=30, tw_chest=12, thR=60, shR=-18, yw_thR=30, yw_shR=16, fpR=30, fL=(0.28, 22)), "lin"),
    (8, H(hx=0.05, thR=20, shR=-30, fpR=20), "out"),
    (11, H(fR=(-0.1, 40), liftR=0.02), "out"),
    (14, HOLD, "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(fL=(0.2, 0)), "smooth"),
    (4, V(hz=0.56, fL=(0.14, 30), tw_hips=14, chest=150, head=150, fiL=60), "in3"),               # the leg buckles, the knee gives
    (7, V(hz=0.54, fL=(0.12, 30), tw_hips=12, spine=166, chest=152, hL=(0.24, 0.8), fiL=40), "out"),   # a hand drops to the thigh
    (14, V(hz=0.57, fL=(0.16, 10)), "smooth"),
], hit=4, note="the dead leg: the knee round into the side of his thigh, his leg buckles"))

CLIPS.update(pair("plum_knee", 26, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.02, hz=0.645, hL=(0.44, 0.92), hR=(0.44, 0.89), chest=168, neck=162, fR=(-0.1, 54), fiL=120, fiR=120), "in"),   # both hands drag his head down
    (5, H(hx=0.09, hz=0.665, spine=184, chest=170, tw_hips=20, thR=96, shR=-24, fpR=40, fL=(0.28, 34), hL=(0.43, 0.8), hR=(0.43, 0.77)), "in"),
    (6, H(hx=0.12, hz=0.68, spine=188, chest=172, tw_hips=26, thR=126, shR=10, fpR=45, fL=(0.28, 44), hL=(0.43, 0.78), hR=(0.43, 0.75)), "in3"),
    (7, H(hx=0.12, hz=0.68, spine=188, chest=172, tw_hips=26, thR=128, shR=12, fpR=45, fL=(0.28, 44), hL=(0.5, 0.86), hR=(0.5, 0.84),
          fiL=40, fiR=40), "lin"),                                                                    # the hands let go as he's knocked up
    (11, P(HOLD, hx=0.09, hz=0.65, spine=176, chest=166, neck=160, head=170, uaL=60, faL=150, uaR=24, faR=164, fiL=165, fiR=165,
           thR=40, shR=-40, fpR=30, fL=(0.28, 12)), "out"),
    (16, P(HOLD, hx=0.05, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.28, 0), fR=(-0.1, 40), liftR=0.03), "out"),
    (26, P(HOLD, hx=0.0, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.24, 0), fR=(-0.13, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hz=0.57, spine=158, spine_mid=144, chest=128, neck=118, head=124, hL=(0.24, 0.84), hR=(0.24, 0.81)), "in"),
    (5, V(hz=0.55, hx=0.02, spine=152, spine_mid=136, chest=118, neck=108, head=112), "in"),
    (6, V(hz=0.6, hx=-0.02, spine=170, spine_mid=170, chest=176, neck=196, head=222, uaL=120, faL=150, uaR=100, faR=140, fiL=30, fiR=30), "in3"),   # the knee snaps his head up
    (9, V(hz=0.62, hx=-0.12, spine=190, spine_mid=196, chest=204, neck=214, head=232, uaL=150, faL=170, uaR=130, faR=160,
          fL=(0.12, 0.1, 20), fR=(-0.3, 0.1, 10)), "out"),
    (14, L(-0.3, hz=0.22, hips=230, spine=236, spine_mid=240, chest=244, neck=246, head=250, thL=60, shL=40, thR=50, shR=30), "in"),   # falling back
    (17, L(-0.4, hz=0.074), "in3"),                                                                  # flat on his back
    (20, L(-0.42, hz=0.09, chest=262, head=262), "out"),
    (26, L(-0.42), "smooth"),
], hit=6, vshift=True, note="THE PLUM: his head dragged down onto the knee; it snaps up and he goes over backwards"))

CLIPS.update(pair("headbutt", 18, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.0, neck=176, head=184, chest=166, hL=(0.5, 1.1), hR=(0.5, 1.07)), "in"),            # the head draws back
    (5, H(hx=0.1, neck=136, head=128, chest=152, spine=170, hL=(0.5, 1.02), hR=(0.5, 0.99), fR=(-0.1, 58)), "in3"),   # forehead into his face
    (6, H(hx=0.1, neck=134, head=126, chest=151, hL=(0.52, 1.04), hR=(0.52, 1.01), fiL=40, fiR=40, fR=(-0.1, 58)), "lin"),
    (10, P(HOLD, hx=0.06, neck=158, head=168, uaL=62, faL=150, uaR=22, faR=164, fiL=165, fiR=165), "out"),
    (18, P(HOLD, hx=0.0, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.24, 0), fR=(-0.13, 38), liftL=0.02), "smooth"),
], [
    (0, P(HELD, hL=(0.24, 0.84), hR=(0.24, 0.81), spine=166, chest=150, head=152), "smooth"),        # his hands down at his thigh
    (5, P(HELD, hL=(0.24, 0.84), hR=(0.24, 0.81), spine=168, chest=152, head=156), "in3"),
    (6, V(hx=-0.04, spine=180, spine_mid=184, chest=190, neck=206, head=226, uaL=60, faL=110, uaR=40, faR=100, fiL=30, fiR=30), "out3"),
    (10, V(hx=-0.14, spine=184, chest=192, neck=200, head=210, uaL=70, faL=150, uaR=50, faR=140, fL=(0.08, 0), fR=(-0.36, 20), liftL=0.03), "out"),
    (18, V(hx=-0.2, spine=176, chest=168, neck=162, head=172, uaL=60, faL=150, uaR=30, faR=150, fL=(0.04, 0), fR=(-0.4, 30)), "smooth"),
], hit=5, vshift=True, note="headbutt: his hands at his leg, the forehead drives into his face, he reels back"))

# ---------------------------------------------------------------- the trips and the reap (he lands IN FRONT)
CLIPS.update(pair("sweep", 22, [
    (0, HOLD, "smooth"),
    (3, H(hx=-0.02, tw_chest=16, tw_hips=10, chest=166, hL=(0.46, 1.08), hR=(0.46, 1.05)), "in"),          # pull him onto his lead leg
    (5, H(hx=0.0, tw_chest=-10, fL=(0.36, 0.06, -10), tw_hips=-20), "in"),                                  # the sole sweeps in low
    (6, H(hx=0.0, tw_chest=-20, fL=(0.46, 0.07, -20), tw_hips=-28, hL=(0.5, 1.0), hR=(0.5, 0.97)), "in3"),   # takes his foot
    (9, H(hx=0.04, hz=0.6, spine=164, chest=150, neck=150, head=154, fL=(0.36, 0), hL=(0.62, 0.78), hR=(0.62, 0.75)), "out"),   # following him down
    (14, H(hx=0.06, hz=0.58, spine=160, chest=146, fL=(0.36, 0), hL=(0.7, 0.5), hR=(0.7, 0.47), fiL=40, fiR=40), "out"),
    (22, P(HOLD, hx=0.06, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.36, 0), fR=(-0.05, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hx=0.02, tw_chest=-16, fL=(0.22, 0), fR=(-0.2, 40)), "in"),
    (6, V(hx=0.0, hz=0.58, fL=(0.3, 0.12, -30), thL=None, tw_hips=20, chest=160, uaL=120, faL=150, fiL=30), "in3"),   # the foot whipped away
    (9, V(hx=-0.06, hz=0.44, hips=210, spine=214, spine_mid=218, chest=222, neck=226, head=232, uaL=150, faL=170, uaR=130, faR=160,
          fL=(0.36, 0.3, -20), fR=(-0.16, 0.06, 10)), "in"),
    (12, L(-0.38, hz=0.16, hips=248, spine=252, chest=256, head=262, thL=70, shL=60, thR=60, shR=50), "in3"),
    (14, L(-0.43, hz=0.08), "out"),
    (22, L(-0.45), "smooth"),
], hit=6, vshift=True, note="DEAD LEG: his weight pulled onto the hurt leg, the foot swept, he drops onto his back"))

CLIPS.update(pair("reap", 26, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.12, hz=0.62, fL=(0.5, 0), liftL=0.03, chest=156, tw_chest=-16, hL=(0.6, 1.08), hR=(0.6, 1.05)), "in"),   # deep step in alongside him
    (5, H(hx=0.2, hz=0.63, fL=(0.52, 20), chest=150, spine=168, thR=60, shR=-10, fpR=40, hL=(0.66, 1.02), hR=(0.66, 0.99)), "in"),  # chest on his chest, the leg swings through
    (7, H(hx=0.24, hz=0.6, fL=(0.52, 26), spine=150, spine_mid=140, chest=128, neck=128, head=138, thR=-60, shR=-60, fpR=60, tpR=40,
          hL=(0.76, 0.86), hR=(0.76, 0.83)), "in3"),                                                       # reap: the leg swept back hard, the head goes down
    (10, H(hx=0.28, hz=0.5, fL=(0.52, 10), spine=146, chest=126, neck=124, head=134, thR=-40, shR=-80, hL=(0.86, 0.5), hR=(0.86, 0.47)), "out"),
    (14, H(hx=0.3, hz=0.48, fL=(0.52, 0), fR=(0.1, 30), spine=150, chest=134, head=140, hL=(0.9, 0.3), hR=(0.9, 0.28), fiL=60, fiR=60), "out"),
    (20, P(HOLD, hx=0.3, hz=0.62, spine=172, chest=162, neck=158, head=168, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.52, 0), fR=(0.15, 38)), "out"),
    (26, P(HOLD, hx=0.3, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.54, 0), fR=(0.17, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hx=-0.04, spine=176, chest=170, neck=168, head=174, fR=(-0.28, 20)), "in"),                    # driven upright, back onto his heels
    (5, V(hx=-0.08, hz=0.62, spine=188, spine_mid=192, chest=198, neck=200, head=206, fR=(-0.3, 0), fL=(0.16, 0)), "in"),
    (7, V(hx=-0.12, hz=0.56, hips=200, spine=210, spine_mid=214, chest=220, neck=224, head=232, uaL=120, faL=160, uaR=110, faR=150,
          fL=(0.4, 0.3, -10), fR=(-0.3, 0.06, 0)), "in3"),                                                 # the leg reaped from under him
    (10, L(-0.48, hz=0.3, hips=228, spine=232, chest=238, head=246, thL=80, shL=70, thR=40, shR=30), "in"),
    (12, L(-0.6, hz=0.074), "in3"),                                                                       # slammed down flat
    (14, L(-0.62, hz=0.1, chest=262, head=258), "out"),
    (26, L(-0.62), "smooth"),
], hit=7, vshift=True, hshift=True, note="o-soto-gari: driven back onto his heels, the leg reaped, slammed down in front"))

# ---------------------------------------------------------------- over you: he lands BEHIND
def _over(name, frames, high, note):
    """Hip throw (low) or shoulder throw (high): turn in, load him, pull him over, he lands behind."""
    hz_load = 0.5 if high else 0.56
    peak = 1.45 if high else 1.15
    hero = [
        (0, HOLD, "smooth"),
        (3, H(hx=0.1, hz=0.6, tw_hips=-60, tw_spine=-70, tw_chest=-80, chest=160, fL=(0.36, 20), fR=(0.16, 30), liftR=0.04,
              hL=(0.56, 1.06), hR=(0.5, 1.0)), "in"),                                                       # step in, turning his back into him
        (6, H(hx=0.2, hz=hz_load, tw_hips=-150, tw_spine=-160, tw_chest=-170, spine=150, spine_mid=140, chest=126, neck=130, head=140,
              fL=(0.32, 10), fR=(0.12, 10), hL=(0.5, 0.92), hR=(0.42, 0.8)), "in"),                         # loaded: under his hips / shoulder
        (9, H(hx=0.18, hz=hz_load - 0.04, tw_hips=-160, tw_chest=-178, spine=128, spine_mid=118, chest=104, neck=104, head=116,
              hL=(0.3, 0.6), hR=(0.24, 0.5), fL=(0.32, 0), fR=(0.12, 0)), "in3"),                           # the pull over: bent double
        (12, H(hx=0.14, hz=hz_load, tw_hips=-150, tw_chest=-170, spine=134, chest=112, neck=110, head=122, hL=(-0.2, 0.3), hR=(-0.24, 0.26),
               fiL=60, fiR=60), "out"),                                                                     # following him down behind
        (18, H(hx=0.1, hz=0.6, tw_hips=-90, tw_chest=-100, spine=160, chest=146, neck=144, head=152, uaL=40, faL=110, uaR=30, faR=100), "out"),
        (frames, P(HOLD, hx=0.08, hz=0.64, spine=176, chest=166, neck=160, head=172, tw_hips=-12, tw_chest=-6,
                   uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165, fL=(0.32, 0), fR=(-0.05, 38)), "smooth"),
    ]
    victim = [
        (0, HELD, "smooth"),
        (3, V(hx=0.04, spine=170, chest=160, neck=156, head=160, fL=(0.24, 10)), "in"),
        (6, V(hx=0.14, hz=0.7 if not high else 0.78, spine=150, spine_mid=140, chest=128, neck=124, head=130, fL=(0.4, 0.14, 40), fR=(0.0, 0.1, 40),
              uaL=60, faL=80, uaR=70, faR=90), "in"),                                                       # loaded, feet leaving the floor
        (9, V(hx=0.55, hz=peak, hips=60, spine=50, spine_mid=40, chest=30, neck=24, head=20, thL=190, shL=200, thR=170, shR=190,
              uaL=40, faL=20, uaR=20, faR=0, fiL=30, fiR=30), "in3"),                                       # over the top, upside down
        (11, V(hx=1.2, hz=0.55, hips=-40, spine=-48, spine_mid=-54, chest=-60, neck=-66, head=-70, thL=170, shL=160, thR=150, shR=140,
               uaL=150, faL=160, uaR=130, faR=150), "in"),
        (13, L(GAP + 0.86, hz=0.074, thL=110, shL=100, thR=96, shR=90), "in3"),                             # flat on his back, behind the hero
        (15, L(GAP + 0.88, hz=0.12, chest=262, head=256, thL=100, shL=84), "out"),
        (frames, L(GAP + 0.88), "smooth"),
    ]
    return pair(name, frames, hero, victim, hit=13, vshift=True, hshift=True, note=note)


CLIPS.update(_over("hip_throw", 28, False, "o-goshi: turn in, his hips loaded on yours, pulled over: he lands behind you"))
CLIPS.update(_over("shoulder_throw", 30, True, "seoi-nage: drop under him, his arm over your shoulder, over the top: he lands behind you"))

# ---------------------------------------------------------------- lifts
CLIPS.update(pair("suplex", 44, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.12, hz=0.56, spine=160, chest=146, neck=140, head=146, hL=(0.6, 0.86), hR=(0.6, 0.83), fL=(0.36, 0), liftL=0.03), "in"),   # in under, arms round him
    (7, H(hx=0.1, hz=0.62, spine=184, spine_mid=190, chest=196, neck=200, head=204, hL=(0.28, 1.4), hR=(0.28, 1.37), fL=(0.36, 20)), "in"),  # lifting, leaning back
    (12, H(hx=0.04, hz=0.66, spine=196, spine_mid=206, chest=214, neck=218, head=226, hL=(0.1, 1.6), hR=(0.1, 1.57), fL=(0.36, 40), fR=(-0.1, 60)), "smooth"),  # held up there
    (16, H(hx=-0.04, hz=0.5, hips=210, spine=226, spine_mid=236, chest=246, neck=250, head=256, hL=(-0.3, 1.0), hR=(-0.3, 0.97),
           fL=(0.36, 0.1, -20), fR=(-0.1, 0.1, -20)), "in"),                                              # falling back with him
    (20, P(LIE, hx=-0.06, hz=0.08, hips=262, spine=266, chest=268, neck=270, head=268, uaL=170, faL=180, uaR=160, faR=170,
           thL=70, shL=40, thR=60, shR=30), "in3"),                                                       # both crash down
    (26, P(LIE, hx=-0.04, hz=0.12, hips=220, spine=210, spine_mid=200, chest=190, neck=186, head=184, uaL=20, faL=30, uaR=10, faR=20,
           thL=96, shL=10, thR=80, shR=0), "out"),                                                        # sits up
    (33, P(HOLD, hx=-0.05, hz=0.42, spine=166, chest=152, neck=150, head=160, uaL=40, faL=60, uaR=20, faR=40, fiL=60, fiR=60,
           thL=86, shL=4, fpL=0, thR=10, shR=-86, fpR=-60), "smooth"),                                    # onto a knee
    (44, P(HOLD, hx=-0.08, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.16, 0), fR=(-0.21, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hx=0.02, hz=0.6, spine=170, chest=158), "in"),
    (7, V(hx=0.3, hz=0.95, hips=110, spine=104, spine_mid=98, chest=92, neck=88, head=86, fL=(0.4, 0.4, 30), fR=(0.2, 0.36, 30),
          uaL=60, faL=40, uaR=50, faR=30), "in"),                                                         # lifted, tipping over
    (12, V(hx=0.5, hz=1.55, hips=10, spine=4, spine_mid=0, chest=-4, neck=-6, head=-8, thL=180, shL=180, thR=176, shR=178,
           uaL=20, faL=10, uaR=10, faR=0, fiL=30, fiR=30), "smooth"),                                      # held upside down, straight up
    (16, V(hx=1.3, hz=0.9, hips=-50, spine=-56, spine_mid=-60, chest=-64, neck=-66, head=-68, thL=150, shL=140, thR=140, shR=130), "in"),
    (20, L(GAP + 1.12, hz=0.074, thL=130, shL=120, thR=120, shR=110), "in3"),                             # down on his back behind
    (24, L(GAP + 1.15, hz=0.1, chest=262, head=258), "out"),
    (44, L(GAP + 1.15), "smooth"),
], hit=20, vshift=True, hshift=True, note="suplex: lifted upside down, held, both fall back: he crashes down behind"))

CLIPS.update(pair("body_slam", 34, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.1, hz=0.54, spine=156, chest=140, neck=136, head=144, hL=(0.56, 0.72), hR=(0.66, 0.62), fL=(0.36, 0), liftL=0.03), "in"),   # one arm between his legs, one round his shoulder
    (8, H(hx=0.08, hz=0.62, spine=178, chest=180, neck=176, head=180, hL=(0.36, 1.24), hR=(0.5, 1.2), fL=(0.36, 30)), "in"),           # scooped up across the chest
    (13, H(hx=0.06, hz=0.66, spine=184, chest=188, neck=184, head=186, hL=(0.3, 1.5), hR=(0.46, 1.46), fL=(0.36, 40), fR=(-0.1, 60)), "smooth"),   # high
    (16, H(hx=0.14, hz=0.56, spine=158, spine_mid=150, chest=140, neck=138, head=146, hL=(0.6, 0.66), hR=(0.74, 0.6), fL=(0.4, 0), fR=(-0.06, 40)), "in3"),   # slammed down
    (17, H(hx=0.15, hz=0.55, spine=154, chest=136, hL=(0.64, 0.4), hR=(0.78, 0.36), fiL=40, fiR=40), "lin"),
    (24, H(hx=0.12, hz=0.6, spine=166, chest=154, uaL=40, faL=100, uaR=30, faR=90), "out"),
    (34, P(HOLD, hx=0.1, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.34, 0), fR=(-0.03, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hz=0.62, spine=170, chest=160, fL=(0.2, 20)), "in"),
    (8, V(hx=0.24, hz=1.0, hips=260, spine=262, spine_mid=264, chest=266, neck=268, head=270, thL=70, shL=50, thR=80, shR=60,
          uaL=150, faL=170, uaR=140, faR=160, fiL=30, fiR=30), "in"),                                       # flat across his chest
    (13, V(hx=0.26, hz=1.3, hips=264, spine=266, chest=268, neck=270, head=272, thL=76, shL=60, thR=86, shR=70), "smooth"),
    (16, L(-0.38, hz=0.074), "in3"),                                                                       # slammed flat
    (18, L(-0.4, hz=0.11, chest=262, head=258, uaL=130, faL=150), "out"),
    (34, L(-0.4), "smooth"),
], hit=16, vshift=True, hshift=True, note="body slam (a worn-down man): scooped up across the chest, held high, slammed down in front"))

CLIPS.update(pair("ddt", 32, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.04, hz=0.63, chest=158, neck=158, head=162, hL=(0.36, 0.92), hR=(0.3, 0.98), yw_uaL=-30, fiL=110, fiR=110), "in"),   # front facelock: his head under the arm
    (7, H(hx=0.0, hz=0.66, spine=184, spine_mid=188, chest=192, neck=190, head=196, hL=(0.26, 1.0), hR=(0.2, 1.06), fR=(-0.1, 60)), "in"),   # up on the toes, falling back
    (10, P(LIE, hx=-0.22, hz=0.16, hips=244, spine=250, spine_mid=254, chest=258, neck=262, head=264, uaL=140, faL=110, uaR=140, faR=100,
           thL=70, shL=40, thR=60, shR=30), "in3"),                                                        # dropped on his back: the head planted
    (11, P(LIE, hx=-0.25, hz=0.09, hips=262, spine=264, chest=266, neck=268, head=266, uaL=130, faL=100, uaR=130, faR=96,
           thL=80, shL=50, thR=70, shR=40), "lin"),
    (17, P(LIE, hx=-0.27, hz=0.12, hips=220, spine=210, spine_mid=200, chest=192, neck=186, head=184, uaL=20, faL=30, uaR=10, faR=20,
           thL=96, shL=10, thR=80, shR=0), "out"),
    (24, P(HOLD, hx=-0.3, hz=0.42, spine=166, chest=152, neck=150, head=160, uaL=40, faL=60, uaR=20, faR=40, fiL=60, fiR=60,
           thL=86, shL=4, fpL=0, thR=10, shR=-86, fpR=-60), "smooth"),
    (32, P(HOLD, hx=-0.42, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(-0.18, 0), fR=(-0.55, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (3, V(hz=0.56, spine=150, spine_mid=134, chest=118, neck=108, head=112, hL=(0.3, 0.7), hR=(0.3, 0.67)), "in"),          # bent double, head trapped
    (7, V(hx=0.18, hz=0.66, spine=140, spine_mid=124, chest=110, neck=104, head=100, fL=(0.3, 30), fR=(-0.1, 50)), "in"),  # dragged forward off his feet
    (10, V(hx=0.12, hz=0.5, hips=70, spine=58, spine_mid=46, chest=36, neck=28, head=20, thL=140, shL=150, thR=120, shR=130,
           uaL=60, faL=40, uaR=50, faR=30, fiL=30, fiR=30), "in3"),                                         # head driven into the floor
    (12, P(PRONE, hx=0.08, hz=0.2, hips=80, spine=74, chest=70, head=72, thL=-50, shL=-60, thR=-40, shR=-50), "in"),
    (15, P(PRONE, hx=0.05), "in3"),
    (32, P(PRONE, hx=0.05), "smooth"),
], hit=10, vshift=True, hshift=True, note="DDT: front facelock, he falls back and drives the head into the floor, face down"))

# ---------------------------------------------------------------- the guillotine (hold high)
CHOKE = H(hx=0.0, hz=0.62, spine=186, spine_mid=190, chest=194, neck=188, head=190, tw_chest=10,
          hL=(0.3, 0.98), hR=(0.26, 1.02), yw_uaL=-34, yw_faL=-50, fiL=150, fiR=110, fL=(0.28, 0), fR=(-0.14, 30))
CHOKED = V(hx=0.12, hz=0.6, spine=146, spine_mid=128, chest=110, neck=100, head=102, hL=(0.3, 0.86), hR=(0.34, 0.92), fiL=100, fiR=100,
           fL=(0.3, 10), fR=(-0.1, 30))
CLIPS.update(pair("guillotine_in", 12, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.02, chest=164, hL=(0.42, 0.96), hR=(0.46, 1.0), yw_uaL=-26), "in"),                     # the arm snakes round his neck
    (7, H(hx=0.0, hz=0.63, spine=180, chest=180, neck=176, head=178, hL=(0.34, 0.96), hR=(0.3, 1.0), yw_uaL=-32, fiL=140, fiR=110), "in"),
    (12, CHOKE, "out"),
], [
    (0, HELD, "smooth"),
    (4, V(hx=0.05, chest=130, neck=118, head=118), "in"),
    (8, V(hx=0.1, spine=150, spine_mid=132, chest=114, neck=104, head=104, hL=(0.32, 0.88), hR=(0.36, 0.94)), "in"),
    (12, CHOKED, "out"),
], hit=7, note="the guillotine: the arm snakes round his neck, the hand locks"))
CLIPS.update(pair("guillotine", 30, [
    (0, CHOKE, "smooth"),
    (8, P(CHOKE, spine=190, spine_mid=196, chest=202, neck=196, head=196, hz=0.625, fR=(-0.14, 22)), "smooth"),     # arching, hips forward, squeezing up
    (15, P(CHOKE, spine=184, chest=190, tw_chest=16, fL=(0.28, 8)), "smooth"),
    (22, P(CHOKE, spine=192, spine_mid=198, chest=204, neck=198, head=198, tw_chest=6, hz=0.627), "smooth"),
    (30, CHOKE, "smooth"),
], [
    (0, CHOKED, "smooth"),
    (5, P(CHOKED, hx=0.14, hL=(0.26, 0.98), hR=(0.3, 0.9), fiL=150, fR=(-0.06, 50), liftR=0.02, tw_chest=12), "smooth"),   # prying at the arm, feet scrambling
    (10, P(CHOKED, hx=0.1, hL=(0.34, 0.84), hR=(0.24, 1.0), fL=(0.24, 20), tw_chest=-10), "smooth"),
    (15, P(CHOKED, hx=0.15, hz=0.61, fR=(-0.12, 20), liftR=0.02), "smooth"),
    (20, P(CHOKED, hx=0.11, hL=(0.28, 0.94), hR=(0.3, 0.9), fL=(0.3, 30), tw_chest=8), "smooth"),
    (25, P(CHOKED, hx=0.13, hz=0.6, tw_chest=-6), "smooth"),
    (30, CHOKED, "smooth"),
], loop=True, breath=True, note="the choke held: he arches and squeezes; the man pries at the arm, his feet scrambling"))
CLIPS.update(pair("guillotine_drop", 24, [
    (0, CHOKE, "smooth"),
    (4, P(CHOKE, spine=196, spine_mid=204, chest=212, neck=206, head=204, hz=0.63), "in"),               # one last squeeze
    (8, H(hx=0.0, hz=0.6, spine=166, chest=152, neck=150, head=158, hL=(0.4, 0.66), hR=(0.36, 0.7), fiL=40, fiR=40), "in3"),   # lets him drop
    (14, P(HOLD, hx=-0.2, hz=0.62, spine=172, chest=162, neck=156, head=166, uaL=40, faL=110, uaR=24, faR=120, fiL=120, fiR=120,
           fL=(0.04, 0), fR=(-0.33, 30), liftL=0.03, liftR=0.03), "out"),
    (24, P(HOLD, hx=-0.34, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(-0.1, 0), fR=(-0.47, 38)), "smooth"),
], [
    (0, CHOKED, "smooth"),
    (4, P(CHOKED, hL=(0.3, 0.7), hR=(0.3, 0.66), fiL=30, fiR=30), "in"),                                 # the hands fall off the arm
    (8, V(hx=0.14, hz=0.42, spine=140, spine_mid=124, chest=106, neck=96, head=94, uaL=10, faL=10, uaR=4, faR=6, fiL=20, fiR=20,
          thL=80, shL=-10, fpL=0, thR=60, shR=-40, fpR=-20), "in3"),                                       # the knees go
    (12, P(PRONE, hx=0.04, hz=0.22, hips=100, spine=94, chest=90, head=86, thL=10, shL=-80, thR=0, shR=-90), "in"),
    (16, P(PRONE, hx=0.0), "in3"),                                                                         # face down, out
    (24, P(PRONE, hx=0.0), "smooth"),
], hit=8, vshift=True, hshift=True, note="the choke finished: one last squeeze, the hands fall away, he crumples face down"))
CLIPS.update(pair("guillotine_let", 18, [
    (0, CHOKE, "smooth"),
    (6, P(HOLD, hx=-0.04, hz=0.63, spine=176, chest=166, neck=160, head=170, uaL=50, faL=130, uaR=24, faR=150, fiL=120, fiR=120,
          fL=(0.24, 0), fR=(-0.13, 30)), "out"),
    (18, P(HOLD, hx=0.0, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.24, 0), fR=(-0.13, 38)), "smooth"),
], [
    (0, CHOKED, "smooth"),
    (6, V(hx=-0.06, hz=0.6, spine=168, chest=160, neck=156, head=150, uaL=40, faL=110, uaR=60, faR=150, hR=None, fL=(0.14, 0), fR=(-0.3, 20)), "out"),  # staggers back, a hand at his throat
    (12, V(hx=-0.14, spine=170, chest=162, head=156, uaL=30, faL=80, uaR=70, faR=170, fL=(0.04, 0), fR=(-0.38, 26), liftL=0.03), "out"),
    (18, V(hx=-0.18, hz=0.62, spine=172, chest=164, head=160, uaL=40, faL=110, uaR=60, faR=160, fL=(0.04, 0), fR=(-0.4, 30)), "smooth"),
], vshift=True, note="let go before he's out: he staggers back, a hand at his throat, dazed"))

# ---------------------------------------------------------------- the body lock (hold low)
LOCK = H(hx=0.1, hz=0.62, spine=190, spine_mid=196, chest=200, neck=190, head=186, hL=(0.5, 0.84), hR=(0.5, 0.8),
         yw_uaL=-24, yw_uaR=24, yw_faL=-50, yw_faR=50, fiL=110, fiR=110, fL=(0.34, 10), fR=(-0.06, 40))
LIFTED = V(hx=0.02, hz=0.86, spine=172, spine_mid=168, chest=160, neck=150, head=150, uaL=70, faL=120, uaR=60, faR=110, fiL=60, fiR=60,
           fL=(0.24, 0.24, 40), fR=(-0.16, 0.22, 50))
CLIPS.update(pair("lock_lift", 14, [
    (0, HOLD, "smooth"),
    (3, H(hx=0.12, hz=0.56, spine=164, chest=150, neck=148, head=156, hL=(0.56, 0.86), hR=(0.56, 0.82), fL=(0.36, 0), liftL=0.03), "in"),   # in low, arms round his waist
    (6, H(hx=0.12, hz=0.54, spine=168, chest=156, hL=(0.54, 0.8), hR=(0.54, 0.76), fiL=120, fiR=120), "smooth"),                           # the grip locks, the knees bend
    (10, P(LOCK, hz=0.6, fR=(-0.06, 50)), "in"),                                                                                           # the legs drive, he leans back
    (14, LOCK, "out"),
], [
    (0, HELD, "smooth"),
    (3, V(hx=0.02, spine=170, chest=162, neck=156, head=158), "in"),
    (6, V(hx=0.02, spine=172, chest=166, uaL=70, faL=120, uaR=60, faR=110), "smooth"),
    (10, P(LIFTED, hz=0.78, fL=(0.24, 0.16, 40), fR=(-0.18, 0.14, 50)), "in"),
    (14, LIFTED, "out"),
], hit=10, note="the body lock: arms round his waist, the knees bend, he's lifted off his feet"))
CLIPS.update(pair("lock_hold", 24, [
    (0, LOCK, "smooth"),
    (6, P(LOCK, hx=0.12, fL=(0.36, 6), liftL=0.02, chest=204, tw_chest=8), "smooth"),            # small steps to keep his balance under the weight
    (12, P(LOCK, hx=0.1, fR=(-0.08, 44), liftR=0.02, chest=198, tw_chest=-6), "smooth"),
    (18, P(LOCK, hx=0.11, chest=202, tw_chest=4), "smooth"),
    (24, LOCK, "smooth"),
], [
    (0, LIFTED, "smooth"),
    (4, P(LIFTED, fL=(0.3, 0.3, 10), fR=(-0.12, 0.2, 60), uaL=90, faL=140, tw_chest=10), "smooth"),     # kicking, hammering at his back
    (8, P(LIFTED, fL=(0.2, 0.2, 60), fR=(-0.1, 0.3, 20), uaR=100, faR=150, uaL=50, faL=90, tw_chest=-8), "smooth"),
    (12, P(LIFTED, fL=(0.32, 0.28, 20), fR=(-0.2, 0.2, 50), uaL=96, faL=150, tw_chest=6), "smooth"),
    (16, P(LIFTED, fL=(0.22, 0.22, 50), fR=(-0.08, 0.3, 30), uaR=96, faR=146, tw_chest=-6), "smooth"),
    (20, P(LIFTED, fL=(0.28, 0.26, 30), uaL=80, faL=130), "smooth"),
    (24, LIFTED, "smooth"),
], loop=True, breath=True, note="held off the ground: he kicks and hammers at the hero's back, the hero steps to stay under him"))
CLIPS.update(pair("lock_slam", 22, [
    (0, LOCK, "smooth"),
    (3, P(LOCK, hz=0.66, spine=196, chest=210, neck=200, head=196, fR=(-0.06, 60)), "in"),        # up higher
    (7, H(hx=0.22, hz=0.5, spine=150, spine_mid=140, chest=128, neck=128, head=138, hL=(0.72, 0.4), hR=(0.72, 0.36),
          fL=(0.44, 0), fR=(0.04, 30), liftL=0.03), "in3"),                                       # driven forward and down on top of him
    (8, H(hx=0.23, hz=0.48, spine=148, chest=126, hL=(0.74, 0.32), hR=(0.74, 0.29), fL=(0.44, 0), fR=(0.04, 30)), "lin"),
    (14, H(hx=0.2, hz=0.58, spine=164, chest=150, uaL=40, faL=100, uaR=30, faR=90, fiL=60, fiR=60, fL=(0.44, 0), fR=(0.04, 34)), "out"),
    (22, P(HOLD, hx=0.2, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.44, 0), fR=(0.07, 38)), "smooth"),
], [
    (0, LIFTED, "smooth"),
    (3, P(LIFTED, hz=0.94), "in"),
    (6, L(-0.26, hz=0.5, hips=230, spine=236, chest=242, head=250, thL=60, shL=40, thR=50, shR=30), "in"),
    (7, L(-0.42, hz=0.074), "in3"),                                                               # driven into the floor on his back
    (9, L(-0.44, hz=0.1, chest=262, head=258), "out"),
    (22, L(-0.44), "smooth"),
], hit=7, vshift=True, hshift=True, note="body lock, swipe towards: driven forward and down onto his back"))
CLIPS.update(pair("lock_suplex", 40, [
    (0, LOCK, "smooth"),
    (4, P(LOCK, hz=0.6, spine=200, spine_mid=210, chest=220, neck=220, head=224, fL=(0.34, 30)), "in"),      # the bridge begins
    (8, P(LIE, hx=0.0, hz=0.3, hips=228, spine=240, spine_mid=252, chest=262, neck=268, head=276, uaL=170, faL=180, uaR=160, faR=170,
          thL=30, shL=-10, thR=20, shR=-20), "in3"),                                              # thrown over: a belly-to-belly arch
    (9, P(LIE, hx=-0.02, hz=0.1, hips=258, spine=264, chest=268, neck=270, head=268, uaL=160, faL=176, uaR=150, faR=166,
          thL=70, shL=40, thR=60, shR=30), "lin"),
    (16, P(LIE, hx=-0.02, hz=0.12, hips=220, spine=210, spine_mid=200, chest=190, neck=186, head=184, uaL=20, faL=30, uaR=10, faR=20,
           thL=96, shL=10, thR=80, shR=0), "out"),
    (26, P(HOLD, hx=-0.04, hz=0.42, spine=166, chest=152, neck=150, head=160, uaL=40, faL=60, uaR=20, faR=40, fiL=60, fiR=60,
           thL=86, shL=4, fpL=0, thR=10, shR=-86, fpR=-60), "smooth"),
    (40, P(HOLD, hx=-0.06, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.18, 0), fR=(-0.19, 38)), "smooth"),
], [
    (0, LIFTED, "smooth"),
    (4, P(LIFTED, hx=0.1, hz=1.05, spine=196, chest=210, head=220), "in"),
    (7, V(hx=1.0, hz=1.1, hips=-20, spine=-30, spine_mid=-40, chest=-50, neck=-56, head=-60, thL=160, shL=170, thR=150, shR=160,
          uaL=160, faL=170, uaR=150, faR=160, fiL=30, fiR=30), "in"),
    (9, L(GAP + 1.08, hz=0.074, thL=120, shL=110, thR=110, shR=100), "in3"),                                  # over and down behind
    (11, L(GAP + 1.1, hz=0.1, chest=262, head=258), "out"),
    (40, L(GAP + 1.1), "smooth"),
], hit=9, vshift=True, hshift=True, note="body lock, swipe away: arched back and thrown over behind you"))
CLIPS.update(pair("lock_drop", 26, [
    (0, LOCK, "smooth"),
    (6, H(hx=0.1, hz=0.5, spine=160, chest=146, neck=144, head=150, hL=(0.5, 0.6), hR=(0.5, 0.56), fL=(0.34, 0), fR=(-0.06, 20)), "in3"),   # dropped hard onto his knees
    (12, H(hx=0.06, hz=0.58, spine=168, chest=156, uaL=50, faL=120, uaR=30, faR=110, fiL=100, fiR=100), "out"),
    (26, P(HOLD, hx=0.04, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.28, 0), fR=(-0.09, 38)), "smooth"),
], [
    (0, LIFTED, "smooth"),
    (6, P(KNEEL, hx=0.02, hz=0.38, chest=150, head=144), "in3"),                                   # onto his knees
    (12, P(KNEEL, hx=0.02, hz=0.4), "out"),
    (20, P(KNEEL, hx=0.0, hz=0.42, chest=160, head=158, uaL=40, faL=100), "smooth"),
    (26, P(HELD, hx=-0.06, hz=0.56, spine=166, chest=156, neck=150, head=156, uaL=50, faL=130, uaR=30, faR=140, hL=None, hR=None,
           fL=(0.16, 0), fR=(-0.3, 40)), "smooth"),                                                 # struggling back up
], hit=6, vshift=True, note="body lock, swipe down (or let go): dropped hard onto his knees"))

# ---------------------------------------------------------------- the shove
CLIPS.update(pair("shove", 14, [
    (0, HOLD, "smooth"),
    (2, H(hx=0.0, hz=0.62, hL=(0.42, 0.98), hR=(0.42, 0.94), fiL=20, fiR=20, twL=0, twR=0, chest=166), "in"),     # the palms come onto his chest
    (4, H(hx=0.12, hz=0.63, hL=(0.7, 1.0), hR=(0.7, 0.96), fiL=10, fiR=10, chest=156, spine=170, fR=(-0.1, 60)), "in3"),   # both arms drive out
    (9, P(HOLD, hx=0.08, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165), "out"),
    (14, P(HOLD, hx=0.0, hz=0.64, spine=176, chest=166, neck=160, head=172, uaL=62, faL=150, uaR=22, faR=166, fiL=165, fiR=165,
           fL=(0.24, 0), fR=(-0.13, 38)), "smooth"),
], [
    (0, HELD, "smooth"),
    (4, V(hx=-0.04, spine=180, chest=184, neck=180, head=186, uaL=90, faL=110, uaR=80, faR=100, hL=None, hR=None, fiL=30, fiR=30), "in3"),
    (8, V(hx=-0.08, hz=0.62, spine=176, chest=176, head=180, uaL=100, faL=130, uaR=90, faR=120, hL=None, hR=None, fL=(0.14, 0), fR=(-0.3, 20),
          liftL=0.03), "out"),
    (14, V(hx=0.0, hz=0.63, spine=174, chest=166, neck=160, head=170, uaL=60, faL=150, uaR=24, faR=164, hL=None, hR=None,
           fL=(0.24, 0), fR=(-0.13, 34)), "smooth"),
], hit=4, note="the shove: both palms drive into his chest, he's sent reeling back (the game carries him)"))
CLIPS["shoved_v"] = CLIPS.pop("shove_v")
CLIPS["shove"]["victim"] = "shoved_v"
