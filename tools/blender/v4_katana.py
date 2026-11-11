"""The katana on the V4 body (docs/design/backlog.md step 4). Two hands on the hilt; the blade lies
along the right hand (the game's sword mount points along the hand bone), so a pose sets the blade by
the hands' angle: 0 points down, 90 forward, 180 up, 270 back.

HERO (and the swordsmen: the same clips)
  katana_guard   chūdan: square-ish, the point at his eyes, breathing, the tip alive
  katana_walk    suri-ashi: sliding steps that never cross, the guard never moves (looped, in place)
  katana_retreat a sliding step back, out of distance
  kesa           ↘ the diagonal shoulder cut: up to jōdan, a stamping step, down through him
  gyaku          ↗ the rising diagonal from the hip
  oroshi         ↓ the straight overhead cut: the heaviest
  kiriage        ↑ the rising cut: lifts his blade and arms away
  yoko           → the level cut: the body turns through it
  ushiro         the turning cut behind
  tsuki / tsuki_low   the thrust: throat / belly, a lunge behind the point
  k_parry_high / k_parry_low / k_parry_rise   meeting his line (uke-nagashi); the rise lifts a thrust away
  iai_sheathe    chiburi (the flick) and nōtō (home into the scabbard at the left hip)
  iai_hold       low, still, the hand resting on the hilt (looped)
  iai_draw       nukitsuke: the draw is the cut, a lunge along it
  katana_pass    the pass: through two or three men, one cut each, the flick, sheathed; then they fall
  chiburi        the flick that cleans the blade
  katana_discard thrown aside, hands empty, back to the fists' guard (the duel)
SWORDSMAN
  k_windup_high / k_windup_low   the tells: up to jōdan, down to waki-gamae
  k_parried      his blade knocked wide, open
  cut_down       cut: he stands a beat, the knees go, down on his face"""
import math

from v4_pose import P
from v4_clips import GUARD
from v4_moves import clip, LOOSE
import v4_far
from v4_clinch import PRONE

CLIPS = {}


def grip(gx, gz, a, **kw):
    """The right hand at (gx, gz) on the hilt, the left one below it, the blade at angle a."""
    vx, vz = math.sin(math.radians(a)), -math.cos(math.radians(a))
    d = dict(hR=(gx, gz), hL=(round(gx - 0.09 * vx, 3), round(gz - 0.09 * vz, 3)), haR=a, haL=a, fiR=165, fiL=165)
    d.update(kw)
    return d


FL, FR = (0.26, 0), (-0.14, 34)
KG = P(LOOSE, hx=0.0, hz=0.62, hips=180, tw_hips=-8, spine=174, spine_mid=172, chest=170, tw_chest=-6, neck=166, head=172,
       clL=(10, 6), clR=(10, 6), fL=FL, fR=FR, **grip(0.3, 0.84, 135))


def K(**kw):
    """The guard, changed."""
    g = {k: v for k, v in kw.items() if k in ("gx", "gz", "a")}
    rest = {k: v for k, v in kw.items() if k not in g}
    p = P(KG, **rest)
    if g:
        p = P(p, **grip(g.get("gx", 0.3), g.get("gz", 0.84), g.get("a", 135)))
    return p


def at(p, dx, **kw):
    q = v4_far.at(p, dx)
    return P(q, **kw)


CLIPS["katana_guard"] = clip(60, [
    (0, KG, "smooth"),
    (15, K(hz=0.618, gx=0.31, gz=0.85, a=131, head=171), "smooth"),
    (30, K(hz=0.622, gx=0.3, gz=0.83, a=139, tw_chest=-4), "smooth"),
    (45, K(hz=0.619, gx=0.29, gz=0.845, a=134, head=173), "smooth"),
    (60, KG, "smooth"),
], loop=True, breath=True, breaths=3, note="chūdan: the point at his eyes, the tip alive, breathing")

# suri-ashi: the advance's step-drag, the arms holding the guard
CLIPS["katana_walk"] = clip(18, [(f, P(p, **grip(0.3, 0.84, 135), tw_chest=-6), e) for f, p, e in v4_far.CLIPS["advance"]["keys"]],
                            loop=True, speed=v4_far.CLIPS["advance"]["speed"], note="suri-ashi: sliding steps, the guard never moves")

CLIPS["katana_retreat"] = clip(12, [
    (0, KG, "lin"),
    (3, K(hx=-0.08, fR=(FR[0] - 0.24, 0.05, 10), hz=0.615), "out"),
    (6, K(hx=-0.2, fR=(FR[0] - 0.42, 30), fL=(FL[0], 20)), "in"),
    (9, K(hx=-0.36, fR=(FR[0] - 0.42, 30), fL=(FL[0] - 0.3, 0.04, 0)), "out"),
    (12, at(KG, -0.42), "smooth"),
], shift=True, note="out of distance: the back foot slides, the front follows, the point never leaves him")


def cut(name, frames, hit, reach, raised, strike, follow, note, step=0.2, react="hit_chin", extra_raise=None, extra_hit=None):
    """A cut, staged for the screen: a small dip before it (the anticipation), up to its start and held
    there a beat, a stamping step and the blade through him, the follow-through carrying on past, the
    body settling low and still (zanshin), then back up into the guard."""
    rs = dict(extra_raise or {})
    hs = dict(extra_hit or {})
    sx, sz, sa = strike
    fx, fz, fa = follow
    end = frames + 8
    keys = [
        (0, KG, "lin"),
        (max(1, hit - 6), K(hz=0.605, spine=171, chest=166, gx=0.27, gz=0.8, a=150, fR=(FR[0], 40)), "out"),          # the dip
        (hit - 4, K(hx=-0.03, hz=0.63, fR=(FR[0], 20), **{**dict(gx=raised[0], gz=raised[1], a=raised[2]), **rs}), "out3"),   # the peak
        (hit - 2, K(hx=-0.03, hz=0.632, fR=(FR[0], 22), **{**dict(gx=raised[0] - 0.01, gz=raised[1] + 0.015, a=raised[2] + 4), **rs}), "smooth"),  # held a beat
        (hit, K(hx=step * 0.6, hz=0.59, spine=166, chest=160, neck=158, head=166, fL=(FL[0] + step, 0), fR=(FR[0], 60),
                **{**dict(gx=sx, gz=sz, a=sa), **hs}), "in3"),
        (hit + 3, K(hx=step * 0.6, hz=0.585, spine=164, chest=158, fL=(FL[0] + step, 0), fR=(FR[0], 56), gx=fx, gz=fz, a=fa), "out"),
        (hit + 8, K(hx=step * 0.62, hz=0.56, spine=160, spine_mid=158, chest=154, neck=156, head=164, fL=(FL[0] + step, 0), fR=(FR[0], 50),
                    gx=fx - 0.04, gz=fz - 0.08, a=fa - 18), "out3"),                                                  # zanshin: low and still
        (hit + 14, K(hx=step * 0.62, hz=0.562, spine=161, chest=155, fL=(FL[0] + step, 0), fR=(FR[0], 50), gx=fx - 0.05, gz=fz - 0.09, a=fa - 20), "smooth"),
        (end, at(KG, step * 0.6, fL=(FL[0] + step, 0), fR=(FR[0] + step * 0.6, 34)), "smooth"),
    ]
    CLIPS[name] = clip(end, keys, hit=hit, reach=reach, react=react, striker="R", shift=True, note=note)


cut("kesa", 18, 8, 1.25, (0.06, 1.24, 212), (0.5, 0.96, 122), (0.44, 0.7, 62),
    "kesa-giri ↘: up to jōdan, a stamping step, down through his shoulder", extra_raise=dict(tw_chest=-22, tw_hips=-14))
cut("gyaku", 18, 8, 1.2, (0.1, 0.62, 330), (0.46, 1.02, 150), (0.4, 1.2, 190),
    "gyaku-kesa ↗: from the hip, rising through him", extra_raise=dict(tw_chest=-26))
cut("oroshi", 20, 10, 1.25, (0.08, 1.34, 196), (0.48, 0.86, 100), (0.45, 0.74, 80),
    "kiri-oroshi ↓: straight down from over the head, the heaviest", step=0.26, react="hit_body")
cut("kiriage", 18, 8, 1.15, (0.25, 0.55, 30), (0.38, 1.24, 172), (0.3, 1.32, 196),
    "kiri-age ↑: from low, rising, lifting his blade and arms away", react="head_back")
cut("yoko", 20, 9, 1.3, (-0.05, 0.95, 270), (0.46, 0.95, 90), (0.3, 0.95, 62),
    "yoko-giri →: the level cut, the whole body turning through it",
    extra_raise=dict(tw_chest=-60, tw_hips=-30), extra_hit=dict(tw_chest=34, tw_hips=20))

CLIPS["ushiro"] = clip(20, [
    (0, KG, "lin"),
    (4, K(head=168, tw_head=70, tw_chest=-30, gx=0.1, gz=1.2, a=200), "out"),
    (9, K(hz=0.6, spine=168, chest=164, head=164, tw_head=80, tw_chest=-70, tw_hips=-30, gx=-0.42, gz=0.95, a=268,
          fR=(FR[0] - 0.12, 0.05, 10)), "in3"),                                       # HIT: behind, without turning the feet
    (12, K(spine=170, tw_chest=-60, tw_head=70, gx=-0.36, gz=0.8, a=240, fR=(FR[0] - 0.14, 30)), "out"),
    (20, KG, "smooth"),
], hit=9, reach=1.2, react="hit_back", striker="R", note="the turning cut: the blade behind him without turning his feet")

for name, gz, a in (("tsuki", 1.0, 96), ("tsuki_low", 0.8, 92)):
    CLIPS[name] = clip(14, [
        (0, KG, "lin"),
        (2, K(hz=0.6, gx=0.26, gz=gz - 0.04, a=a + 20, fR=(FR[0], 50)), "in"),
        (6, K(hx=0.22, hz=0.58, spine=166, chest=160, head=164, fL=(FL[0] + 0.36, 0), fR=(FR[0], 70), gx=0.76, gz=gz, a=a), "in3"),
        (9, K(hx=0.22, hz=0.585, fL=(FL[0] + 0.36, 0), fR=(FR[0], 64), gx=0.7, gz=gz, a=a), "out"),
        (14, at(KG, 0.26, fL=(FL[0] + 0.36, 0), fR=(FR[0] + 0.26, 34)), "smooth"),
    ], hit=6, reach=1.5, react="fold" if name == "tsuki_low" else "head_back", striker="R", shift=True,
        note="tsuki: the point first, a lunge behind it" + (" (the belly)" if name == "tsuki_low" else " (the throat)"))

for name, g, note in (("k_parry_high", (0.22, 1.24, 98), "high: the blade across, over the head, his cut slides off it"),
                      ("k_parry_low", (0.3, 0.84, 18), "low: the blade point-down at the side, his cut slides off it"),
                      ("k_parry_rise", (0.34, 1.16, 152), "rising: lifting his thrust up and away")):
    CLIPS[name] = clip(12, [
        (0, KG, "lin"),
        (3, K(hz=0.61, gx=g[0], gz=g[1], a=g[2], tw_chest=-12, head=166), "in3"),
        (6, K(hz=0.61, gx=g[0] + 0.02, gz=g[1] + 0.02, a=g[2] + 6, tw_chest=-8), "out"),
        (12, KG, "smooth"),
    ], hit=3, striker="R", note="uke-nagashi, " + note)

SAYA = grip(0.06, 0.73, 252, hL=(0.02, 0.72))                         # home at the left hip, the left hand on the scabbard
IAI = P(KG, hz=0.55, spine=160, spine_mid=158, chest=154, neck=156, head=166, fL=(FL[0] + 0.06, 0), fR=(FR[0] - 0.08, 40), **SAYA)
CLIPS["chiburi"] = clip(12, [
    (0, KG, "lin"),
    (4, K(gx=0.42, gz=1.02, a=150), "in"),
    (6, K(gx=0.44, gz=0.78, a=58, tw_chest=10), "in3"),                 # the flick: the blood off the blade
    (12, KG, "smooth"),
], note="chiburi: one sharp flick cleans the blade")
CLIPS["iai_sheathe"] = clip(26, [
    (0, KG, "lin"),
    (4, K(gx=0.42, gz=1.02, a=150), "in"),
    (6, K(gx=0.44, gz=0.78, a=58), "in3"),                               # chiburi
    (14, K(gx=0.14, gz=0.8, a=240, hL=(0.02, 0.72), fiL=120), "smooth"),  # the back of the blade to the scabbard's mouth
    (22, IAI, "out"),                                                     # nōtō: home
    (26, IAI, "smooth"),
], hit=22, note="chiburi, then nōtō: the blade slid home along the left hand")
CLIPS["iai_hold"] = clip(40, [
    (0, IAI, "smooth"),
    (20, P(IAI, hz=0.545, head=164), "smooth"),
    (40, IAI, "smooth"),
], loop=True, breath=True, breaths=2, note="the iai stance: low, still, the hand resting on the hilt")
CLIPS["iai_draw"] = clip(18, [
    (0, IAI, "lin"),
    (2, P(IAI, hx=0.12, fL=(FL[0] + 0.3, 0.05, -8)), "in"),
    (5, P(KG, hx=0.42, hz=0.56, spine=162, chest=156, head=162, tw_chest=46, tw_hips=24, fL=(FL[0] + 0.6, 0), fR=(FR[0], 70),
          **grip(0.95, 0.96, 92, hL=(0.36, 0.8), haL=None, fiL=60)), "in3"),     # HIT: the draw is the cut, one hand, flat
    (9, P(KG, hx=0.48, hz=0.57, tw_chest=50, fL=(FL[0] + 0.6, 0), fR=(FR[0] + 0.1, 50), **grip(0.86, 0.88, 66, hL=(0.38, 0.8), haL=None, fiL=60)), "out"),
    (18, at(KG, 0.54, fL=(FL[0] + 0.6, 0), fR=(FR[0] + 0.5, 34)), "smooth"),
], hit=5, reach=1.45, react="hit_body", striker="R", shift=True, note="nukitsuke: the draw is the cut, a lunge along it")

PASS = 3.2
CLIPS["katana_pass"] = clip(40, [
    (0, IAI, "lin"),
    (3, P(IAI, hx=0.4, hz=0.5, fL=(FL[0] + 0.7, 0.08, -10), fR=(FR[0] + 0.1, 60)), "in"),
    (6, P(KG, hx=1.0, hz=0.5, spine=156, chest=150, tw_chest=40, fL=(FL[0] + 1.1, 0), fR=(FR[0] + 0.7, 0.1, 30),
          **grip(1.9, 0.98, 90, hL=(0.95, 0.8), haL=None)), "lin"),                          # the first: drawn through him
    (11, P(KG, hx=1.8, hz=0.52, spine=160, tw_chest=-20, fL=(FL[0] + 1.7, 0.1, 10), fR=(FR[0] + 1.9, 0), **grip(2.1, 0.72, 40)), "lin"),   # the second: down
    (16, P(KG, hx=2.6, hz=0.52, spine=164, tw_chest=30, fL=(FL[0] + 2.7, 0), fR=(FR[0] + 2.3, 0.1, 30), **grip(2.9, 1.18, 160)), "lin"),   # the third: up
    (20, P(KG, hx=PASS, hz=0.56, spine=170, tw_chest=10, fL=(FL[0] + PASS, 0), fR=(FR[0] + PASS - 0.1, 40), **grip(PASS + 0.3, 0.9, 110)), "out3"),   # stopped, the blade out behind the last
    (25, P(KG, hx=PASS, fL=(FL[0] + PASS, 0), fR=(FR[0] + PASS - 0.1, 40), **grip(PASS + 0.44, 0.78, 58)), "in3"),    # chiburi
    (33, P(IAI, hx=PASS, fL=(FL[0] + PASS + 0.06, 0), fR=(FR[0] + PASS - 0.08, 40), **grip(PASS + 0.14, 0.8, 240, hL=(PASS + 0.02, 0.72))), "smooth"),
    (38, at(IAI, PASS), "out"),                                                              # HIT: nōtō, the click: now they fall
    (40, at(IAI, PASS), "smooth"),
], hit=38, shift=True, note="the pass: through them, one cut each, the flick, home; on the click they fall")

CLIPS["katana_discard"] = clip(22, [
    (0, KG, "lin"),
    (5, K(gx=0.1, gz=1.15, a=210, tw_chest=-30, head=170), "in"),
    (8, P(GUARD, hx=0.0, uaR=-60, faR=-40, haR=-40, fiR=30, uaL=40, faL=100, fiL=120, tw_chest=24, head=176, tw_head=-30), "out3"),   # flung away, open-handed
    (14, P(GUARD, uaR=10, faR=60, fiR=90, head=174), "out"),
    (22, GUARD, "smooth"),
], hit=8, striker="R", note="thrown aside: the katana flung away, the hands empty, the fists come up")

CLIPS["k_windup_high"] = clip(16, [
    (0, KG, "lin"),
    (8, K(hz=0.63, gx=0.06, gz=1.26, a=205, tw_chest=-16, fR=(FR[0], 24)), "out"),
    (16, K(hz=0.632, gx=0.05, gz=1.28, a=208, tw_chest=-18, fR=(FR[0], 24)), "smooth"),
], note="the tell: up to jōdan, the blade back over the shoulder")
CLIPS["k_windup_low"] = clip(16, [
    (0, KG, "lin"),
    (8, K(hz=0.6, gx=0.1, gz=0.66, a=320, tw_chest=-24, fR=(FR[0], 24)), "out"),
    (16, K(hz=0.598, gx=0.09, gz=0.65, a=322, tw_chest=-26, fR=(FR[0], 24)), "smooth"),
], note="the tell: down to waki-gamae, the blade low behind the hip")
CLIPS["k_parried"] = clip(26, [
    (0, KG, "lin"),
    (2, K(hx=-0.08, gx=-0.06, gz=1.32, a=208, head=198, neck=184, chest=178, spine=180, fL=(FL[0] - 0.06, 0)), "out3"),   # knocked wide
    (16, K(hx=-0.1, gx=-0.04, gz=1.28, a=204, head=190, chest=176, spine=180, fL=(FL[0] - 0.06, 0)), "smooth"),           # OPEN
    (26, KG, "smooth"),
], note="parried: his blade knocked wide, arms up, open")
CLIPS["cut_down"] = clip(46, [
    (0, P(LOOSE, hz=0.66, head=176, uaL=10, faL=20, uaR=6, faR=14, fiL=50, fiR=50), "lin"),
    (12, P(LOOSE, hz=0.65, head=164, neck=166, uaL=8, faL=14, uaR=4, faR=10, fiL=30, fiR=30), "smooth"),          # he stands a beat
    (22, P(LOOSE, hz=0.4, spine=158, chest=150, head=150, thL=84, shL=4, fpL=0, thR=10, shR=-84, fpR=-70, uaL=0, faL=0, uaR=0, faR=0), "in"),   # the knees go
    (34, P(PRONE, hx=0.5, hz=0.14), "in"),                                                                          # down on his face
    (46, P(PRONE, hx=0.5), "smooth"),
], shift=True, note="cut: he stands a beat, the knees go, he falls on his face")
