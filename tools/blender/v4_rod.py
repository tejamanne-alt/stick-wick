"""The rod (a steel pipe) on the V4 body: not a sword. It's held like a bat, wound back over the rear
shoulder, and swung with the whole body: the hips first, the chest, the arms last, the weight of it
carrying the follow-through round. Heavy, slow to start, devastating when it lands.

  rod_guard     cocked on the rear shoulder, side-on, the weight back (looped)
  rod_walk      the guard while stepping (suri-ashi step-drag, the rod held still)
  rod_swing     → level: the big horizontal swing, round through him and on over the lead shoulder
  rod_overhead  ↓ the two-handed overhead smash, the pipe ringing off the floor after
  rod_rising    ↑ ↗ from low behind, up under his jaw
  rod_shin      ↘ / tap low: a low swing through his shins (his leg goes)
  rod_jab       tap: the butt end driven into his belly, short and quick (he folds)
  rod_spin      hold on yourself, let go: wound right round, the full turn: everyone in reach"""
from v4_pose import P
from v4_moves import clip
import v4_far
from v4_katana import grip, KG, FL, FR

CLIPS = {}

RG = P(KG, hz=0.63, tw_hips=-22, tw_chest=-24, spine=176, chest=174, head=172, fL=(FL[0] - 0.02, 0), fR=(FR[0] - 0.06, 30),
       **grip(-0.02, 1.04, 205))


def R(**kw):
    g = {k: kw.pop(k) for k in ("gx", "gz", "a") if k in kw}
    p = P(RG, **kw)
    if g:
        p = P(p, **grip(g.get("gx", -0.02), g.get("gz", 1.04), g.get("a", 205)))
    return p


def at(p, dx, **kw):
    return P(v4_far.at(p, dx), **kw)


CLIPS["rod_guard"] = clip(60, [
    (0, RG, "smooth"),
    (15, R(hz=0.627, gx=-0.03, gz=1.05, a=208, head=171), "smooth"),
    (30, R(hz=0.633, gx=-0.01, gz=1.03, a=202, tw_chest=-20), "smooth"),
    (45, R(hz=0.629, gx=-0.03, gz=1.045, a=207, head=173), "smooth"),
    (60, RG, "smooth"),
], loop=True, breath=True, breaths=3, note="the rod cocked on the rear shoulder, the weight back, breathing")

CLIPS["rod_walk"] = clip(18, [(f, P(p, **grip(-0.02, 1.04, 205), tw_chest=-24), e) for f, p, e in v4_far.CLIPS["advance"]["keys"]],
                         loop=True, speed=v4_far.CLIPS["advance"]["speed"], note="stepping in, the rod held cocked")

CLIPS["rod_swing"] = clip(28, [
    (0, RG, "lin"),
    (4, R(hx=-0.05, hz=0.6, tw_hips=-46, tw_spine=-40, tw_chest=-62, spine=180, chest=180, gx=-0.18, gz=1.06, a=246, fR=(FR[0] - 0.06, 10)), "out"),   # the load: right back
    (8, R(hx=0.06, hz=0.6, tw_hips=-30, tw_chest=-58, gx=-0.16, gz=1.04, a=250, fL=(FL[0] + 0.22, 0.06, -10), fR=(FR[0] - 0.06, 30)), "in"),          # the stride, the rod still back
    (11, R(hx=0.16, hz=0.58, spine=166, chest=160, neck=158, head=166, tw_hips=34, tw_spine=30, tw_chest=50, gx=0.56, gz=0.98, a=95,
           fL=(FL[0] + 0.26, 0), fR=(FR[0] - 0.06, 76)), "in3"),                                                                                      # HIT: hips, chest, arms
    (15, R(hx=0.18, hz=0.59, spine=168, chest=164, tw_hips=48, tw_chest=82, gx=0.1, gz=1.12, a=196, fL=(FL[0] + 0.26, 0), fR=(FR[0] + 0.04, 0.06, 40)), "out"),   # round over the lead shoulder
    (21, R(hx=0.18, hz=0.6, tw_hips=30, tw_chest=60, gx=0.0, gz=1.12, a=200, fL=(FL[0] + 0.26, 0), fR=(FR[0] + 0.14, 30)), "out"),
    (28, at(RG, 0.18, fL=(FL[0] + 0.24, 0), fR=(FR[0] + 0.12, 30)), "smooth"),
], hit=11, reach=1.35, react="hit_chin", striker="R", shift=True, note="the big swing: hips, chest, arms, round over the shoulder")

CLIPS["rod_overhead"] = clip(30, [
    (0, RG, "lin"),
    (5, R(hz=0.65, spine=182, chest=186, neck=172, head=170, tw_chest=-6, gx=0.0, gz=1.42, a=200, fR=(FR[0] - 0.06, 20)), "out"),        # up high
    (9, R(hz=0.66, spine=184, chest=188, gx=-0.02, gz=1.45, a=208, fR=(FR[0] - 0.06, 24)), "smooth"),                                        # held at the top
    (13, R(hx=0.16, hz=0.52, spine=150, spine_mid=146, chest=140, neck=146, head=158, gx=0.5, gz=0.82, a=88,
           fL=(FL[0] + 0.24, 0), fR=(FR[0] - 0.06, 70)), "in3"),                                                                               # HIT: brought down with the whole back
    (16, R(hx=0.16, hz=0.5, spine=146, chest=136, gx=0.46, gz=0.48, a=40, fL=(FL[0] + 0.24, 0), fR=(FR[0] - 0.06, 66)), "out3"),          # rung off the floor
    (22, R(hx=0.16, hz=0.54, spine=156, chest=150, gx=0.4, gz=0.52, a=48, fL=(FL[0] + 0.24, 0), fR=(FR[0] - 0.06, 50)), "smooth"),        # a beat, heavy
    (30, at(RG, 0.16, fL=(FL[0] + 0.22, 0), fR=(FR[0] + 0.1, 30)), "smooth"),
], hit=13, reach=1.2, react="hit_body", striker="R", shift=True, note="the overhead smash: up, held, down with the whole back, ringing off the floor")

CLIPS["rod_rising"] = clip(24, [
    (0, RG, "lin"),
    (5, R(hz=0.57, spine=164, chest=158, tw_chest=-40, tw_hips=-30, gx=-0.02, gz=0.58, a=296, fR=(FR[0] - 0.06, 20)), "out"),            # low behind, knees bent
    (10, R(hx=0.12, hz=0.64, spine=184, chest=186, tw_chest=30, tw_hips=24, gx=0.44, gz=1.12, a=150, fL=(FL[0] + 0.18, 0), fR=(FR[0] - 0.06, 74)), "in3"),   # HIT: up under the jaw, rising off the legs
    (14, R(hx=0.14, hz=0.66, spine=186, chest=190, tw_chest=40, gx=0.22, gz=1.36, a=192, fL=(FL[0] + 0.18, 0), fR=(FR[0], 56)), "out"),
    (24, at(RG, 0.14, fL=(FL[0] + 0.16, 0), fR=(FR[0] + 0.08, 30)), "smooth"),
], hit=10, reach=1.2, react="hit_up", striker="R", shift=True, note="the rising swing: from low behind, up under his jaw off the legs")

CLIPS["rod_shin"] = clip(24, [
    (0, RG, "lin"),
    (4, R(hz=0.58, tw_chest=-50, tw_hips=-36, gx=-0.12, gz=0.86, a=252, fR=(FR[0] - 0.06, 20)), "out"),
    (10, R(hx=0.1, hz=0.48, spine=156, chest=148, neck=150, head=160, tw_chest=40, tw_hips=30, gx=0.5, gz=0.5, a=118,
           fL=(FL[0] + 0.2, 0), fR=(FR[0] - 0.06, 60)), "in3"),                                                                                # HIT: low, through his shins
    (14, R(hx=0.1, hz=0.5, tw_chest=60, gx=0.2, gz=0.56, a=200, fL=(FL[0] + 0.2, 0), fR=(FR[0], 40)), "out"),
    (24, at(RG, 0.1, fL=(FL[0] + 0.18, 0), fR=(FR[0] + 0.06, 30)), "smooth"),
], hit=10, reach=1.3, react="leg_buckle", striker="R", shift=True, note="low through his shins: his leg goes")

CLIPS["rod_jab"] = clip(16, [
    (0, RG, "lin"),
    (3, R(hz=0.61, tw_chest=-10, gx=0.06, gz=0.88, a=96), "in"),                                                                         # brought down level, pulled back
    (6, R(hx=0.2, hz=0.59, spine=166, chest=160, tw_chest=10, gx=0.62, gz=0.9, a=92, fL=(FL[0] + 0.26, 0), fR=(FR[0] - 0.06, 66)), "in3"),   # HIT: the butt end, driven in
    (9, R(hx=0.2, hz=0.6, gx=0.54, gz=0.9, a=94, fL=(FL[0] + 0.26, 0), fR=(FR[0] - 0.06, 60)), "out"),
    (16, at(RG, 0.22, fL=(FL[0] + 0.24, 0), fR=(FR[0] + 0.2, 30)), "smooth"),
], hit=6, reach=1.2, react="fold", striker="R", shift=True, note="the butt end, short and quick, into his belly: he folds")

CLIPS["rod_spin"] = clip(36, [
    (0, RG, "lin"),
    (7, R(hz=0.56, spine=170, chest=166, tw_hips=-74, tw_spine=-70, tw_chest=-96, gx=-0.3, gz=1.0, a=270, fR=(FR[0] - 0.1, 10)), "out"),   # coiled right round
    (12, R(hx=0.1, hz=0.56, tw_hips=-30, tw_chest=-50, gx=-0.2, gz=1.0, a=262, fL=(FL[0] + 0.26, 0.06, -10)), "in"),
    (16, R(hx=0.22, hz=0.58, spine=168, chest=164, tw_hips=60, tw_spine=60, tw_chest=94, gx=0.62, gz=1.0, a=90, fL=(FL[0] + 0.32, 0), fR=(FR[0] - 0.06, 76)), "in3"),   # HIT: the full turn, level
    (21, R(hx=0.24, hz=0.58, tw_hips=110, tw_chest=150, gx=-0.12, gz=1.04, a=272, fL=(FL[0] + 0.32, 40), fR=(FR[0] + 0.1, 0.08, 30)), "out"),   # on round, the rod behind
    (28, R(hx=0.26, hz=0.6, tw_hips=40, tw_chest=40, gx=-0.02, gz=1.06, a=210, fL=(FL[0] + 0.32, 0), fR=(FR[0] + 0.2, 30)), "smooth"),
    (36, at(RG, 0.26, fL=(FL[0] + 0.3, 0), fR=(FR[0] + 0.2, 30)), "smooth"),
], hit=16, reach=1.6, react="stumble_back", striker="R", shift=True, note="the full turn: wound right round, everyone in reach")
