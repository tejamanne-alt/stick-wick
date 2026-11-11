"""Moving on his own, for the V4 body: walking, jogging, running, stopping, turning, sneaking, the
running jump, vaulting, picking things up, pushing, and the relaxed idle (docs: the "hero alone" set).

The gaits are generated, not keyed by hand pose by pose, because a gait is a cycle of exact physics:
  · each foot has a stance phase (on the ground, sliding back under the body at exactly the body's
    speed, so nothing skates once the game moves him at that speed) and a swing phase (an arc forward);
  · in stance the foot lands heel first with the toes up (walking) or on the midfoot (running),
    rolls flat, then peels up onto the ball and pushes off; in swing the toes clear the floor and
    reach for the next heel strike;
  · the hips rise and fall on the gait's own rhythm (highest over the planted foot when walking,
    lowest under load when running), the pelvis turns with the stride and the chest turns against it,
    the arms swing against the legs, the head stays level (the engine's springs);
the clip's speed is written with it (metres per second at 1x), so the game plays it at the speed
that matches how fast he is really going."""
import math

from v4_pose import P
import v4_pose as vp

TAU = 2 * math.pi
BASE = vp.BASE
FOOT_LEN, BALL_Z, ANKLE_Z = vp.FOOT_LEN, vp.BALL_Z, vp.ANKLE_Z


def _stance_foot(u, span, strike, push):
    """A foot on the ground at stance progress u (0 strike .. 1 toe-off): (ankle x, ankle z, pitch)."""
    ball_x = span * (1 - 2 * u)                     # slides from +span to -span under the body
    if u < 0.16:
        pitch = strike * (1 - u / 0.16)             # heel strike (toes up, negative) rolling flat
    elif u < 0.55:
        pitch = 0.0
    else:
        k = (u - 0.55) / 0.45
        pitch = push * k * k                        # the heel peels up onto the ball
    if pitch >= 0:
        s = BASE + math.radians(pitch)
        ax = ball_x + 0.03 - FOOT_LEN * math.cos(s)
        az = BALL_Z + FOOT_LEN * math.sin(s)
    else:                                           # on the heel: the ankle just above it
        ax = ball_x + 0.03 - FOOT_LEN * math.cos(BASE)
        az = ANKLE_Z + 0.035 * math.sin(math.radians(-pitch))
    return ax, az, pitch


def gait(n, speed, duty, lift, hz, bounce, bounce_low_at_mid, spine, chest, neck, head,
         arm_base, arm_amp, elbow, fist, twist, lean_pump=0.0, strike=-14.0, push=40.0, hands_relaxed=False):
    """A locomotion cycle of n frames at `speed` m/s (at 30 fps): keys on every frame."""
    stride = speed * n / 30.0                        # metres the body travels in one cycle
    span = stride * duty / 2.0
    keys = []
    for f in range(n + 1):
        ph = f / n
        p = {}
        for s, off in (("L", 0.0), ("R", 0.5)):
            q = (ph + off) % 1.0
            if q < duty:
                ax, az, pitch = _stance_foot(q / duty, span, strike, push)
            else:
                k = (q - duty) / (1 - duty)
                kk = k * k * (3 - 2 * k)
                sx, sz, sp = _stance_foot(1.0, span, strike, push)
                ex, ez, ep = _stance_foot(0.0, span, strike, push)
                ax = sx + (ex - sx) * kk
                az = sz + (ez - sz) * kk + lift * math.sin(math.pi * k) ** 0.8
                pitch = sp + (ep - sp) * kk + 18 * math.sin(math.pi * k)     # the toes point, then lift to clear
            p["f" + s] = (ax, az, pitch)
            # the arm on the other side swings with this leg
            arm = "R" if s == "L" else "L"
            swing = math.cos(TAU * (q - duty * 0.5))
            p["ua" + arm] = arm_base + arm_amp * swing
            p["fa" + arm] = p["ua" + arm] + elbow + elbow * 0.25 * swing
            p["fi" + arm] = fist
            if hands_relaxed:
                p["ha" + arm] = p["fa" + arm] - 8
        # hips: two bounces per cycle (one per step)
        b = math.cos(TAU * 2 * ph)
        p["hz"] = hz + (bounce * (-b) if bounce_low_at_mid else bounce * b)
        p["hx"] = 0.0
        tw = twist * math.sin(TAU * ph)
        p.update(hips=180, tw_hips=tw, spine=spine + lean_pump * b, tw_spine=tw * 0.4, spine_mid=(spine + chest) / 2,
                 chest=chest + lean_pump * b, tw_chest=-tw * 0.7, neck=neck, head=head, tw_head=tw * 0.2)
        keys.append((f, p, "lin"))
    return keys


def clip(frames, keys, loop=False, hit=None, note="", speed=0.0, shift=False, striker=None, breath=False, breaths=2, reach=None, react=None):
    return dict(frames=frames, keys=keys, hit=hit, load=None, loop=loop, aim=None, reach=reach, react=react, note=note,
                striker=striker, shift=shift, breath=breath, breaths=breaths, lag=True, speed=speed)


CLIPS = {}

# ---------------------------------------------------------------- the gaits
# walking alone: loose, the hands relaxed and swinging low, heel first, a gentle rise over each foot
WALK_SPEED, JOG_SPEED, RUN_SPEED, SNEAK_SPEED = 1.1, 3.3, 4.8, 0.55
CLIPS["walk_alone"] = clip(28, gait(28, WALK_SPEED, duty=0.62, lift=0.05, hz=0.672, bounce=0.012, bounce_low_at_mid=False,
                                    spine=178, chest=176, neck=172, head=178, arm_base=0, arm_amp=16, elbow=14, fist=60,
                                    twist=8, strike=-16, push=34, hands_relaxed=True),
                           loop=True, speed=WALK_SPEED, note="walking alone: heel first, rolling through, a loose arm swing")
# the jog (the classic 'run'): a flight phase, midfoot landing, the arms bent and pumping
CLIPS["run"] = clip(18, gait(18, JOG_SPEED, duty=0.38, lift=0.13, hz=0.65, bounce=0.03, bounce_low_at_mid=True,
                             spine=172, chest=168, neck=164, head=174, arm_base=14, arm_amp=38, elbow=80, fist=120,
                             twist=12, lean_pump=1.5, strike=4, push=48),
                    loop=True, speed=JOG_SPEED, note="the jog: a flight phase, midfoot landing, arms pumping")
# running flat out: leaning in, high knees, the arms driving hard
CLIPS["sprint"] = clip(16, gait(16, RUN_SPEED, duty=0.31, lift=0.2, hz=0.635, bounce=0.035, bounce_low_at_mid=True,
                                spine=166, chest=160, neck=158, head=170, arm_base=18, arm_amp=58, elbow=84, fist=140,
                                twist=16, lean_pump=2.5, strike=8, push=58),
                       loop=True, speed=RUN_SPEED, note="flat out: leaning in, knees high, arms driving")
# sneaking: crouched low, long slow steps, the feet placed (rolled heel to toe without a sound)
CLIPS["sneak"] = clip(40, gait(40, SNEAK_SPEED, duty=0.72, lift=0.06, hz=0.47, bounce=0.006, bounce_low_at_mid=False,
                               spine=158, chest=150, neck=148, head=168, arm_base=40, arm_amp=8, elbow=70, fist=90,
                               twist=6, strike=-10, push=26),
                      loop=True, speed=SNEAK_SPEED, note="sneaking: crouched, slow placed steps, hands ready")

# ---------------------------------------------------------------- standing alone
# loose, the weight on one leg then the other, the hands hanging (half open), the shoulders loose,
# looking around: left, the floor, right, behind; a breath that shows
LOOSE = P(hx=0.0, hz=0.676, hips=180, spine=178, spine_mid=177, chest=176, neck=174, head=178,
          clL=(0, 0), clR=(0, 0), uaL=4, faL=12, haL=8, uaR=-4, faR=6, haR=2, fiL=55, fiR=55,
          fL=(0.12, 0), fR=(-0.04, 0))
CLIPS["idle_alone"] = clip(150, [
    (0, LOOSE, "smooth"),
    (24, P(LOOSE, hx=0.012, tw_hips=4, fR=(-0.04, 8), head=176, tw_head=-24), "smooth"),            # looks away (over the far shoulder)
    (44, P(LOOSE, hx=0.014, tw_hips=5, fR=(-0.04, 10), head=176, tw_head=-28, fiR=80), "smooth"),
    (62, P(LOOSE, hx=0.0, head=168, tw_head=0, neck=170), "smooth"),                                # down at the floor
    (80, P(LOOSE, hx=-0.012, tw_hips=-4, fL=(0.12, 8), head=180, tw_head=22, clL=(4, 6), clR=(4, 6)), "smooth"),   # the other way, a shoulder roll
    (96, P(LOOSE, hx=-0.012, tw_hips=-4, fL=(0.12, 8), head=180, tw_head=26, fiL=85), "smooth"),
    (116, P(LOOSE, hx=0.0, head=178, tw_head=-6, uaR=-2, faR=20, haR=30), "smooth"),               # a hand flexes
    (134, P(LOOSE, hx=0.004, head=177, tw_head=4), "smooth"),
    (150, LOOSE, "smooth"),
], loop=True, breath=True, breaths=4, note="alone: weight shifting, hands loose, looking around, breathing")

CROUCH = P(hx=0.0, hz=0.47, hips=180, spine=158, spine_mid=154, chest=150, neck=148, head=168,
           clL=(10, 6), clR=(10, 6), uaL=44, faL=110, uaR=36, faR=104, fiL=90, fiR=90,
           fL=(0.2, 0), fR=(-0.12, 30))
CLIPS["crouch_idle"] = clip(60, [
    (0, CROUCH, "smooth"),
    (20, P(CROUCH, hz=0.466, head=170, tw_head=-12), "smooth"),
    (40, P(CROUCH, hz=0.472, head=166, tw_head=10), "smooth"),
    (60, CROUCH, "smooth"),
], loop=True, breath=True, breaths=3, note="crouched still: low, ready, listening")

# ---------------------------------------------------------------- stopping, turning
RUN0 = vp.solve(CLIPS["run"]["keys"][4][1])
CLIPS["run_stop"] = clip(16, [
    (0, P(RUN0), "lin"),
    (3, P(LOOSE, hz=0.6, spine=190, spine_mid=192, chest=194, neck=184, head=180, uaL=40, faL=80, uaR=50, faR=90, fiL=90, fiR=90,
          fL=(0.4, 0.07, -16), fR=(-0.08, 30), hx=0.13), "out"),                                      # the lead heel plants far ahead, leaning back
    (6, P(LOOSE, hz=0.58, spine=186, chest=186, head=178, uaL=30, faL=60, uaR=36, faR=70,
          fL=(0.36, 0), fR=(0.1, 0.1, 10), hx=0.2), "out"),                                           # a short braking step
    (9, P(LOOSE, hz=0.6, spine=174, chest=170, head=172, uaL=14, faL=30, uaR=10, faR=26, fL=(0.36, 0), fR=(0.16, 0), hx=0.23), "out"),
    (12, P(LOOSE, hz=0.668, spine=179, chest=177, fL=(0.36, 0), fR=(0.18, 0), hx=0.24), "out"),     # the weight settles
    (16, P(LOOSE, fL=(0.36, 0), fR=(0.18, 0), hx=0.24), "smooth"),
], shift=True, note="stopping from a jog: the heel plants ahead, leaning back, one braking step, the weight settles")
CLIPS["run_turn"] = clip(14, [
    (0, P(LOOSE, hz=0.6, spine=196, spine_mid=200, chest=204, neck=190, head=184, uaL=60, faL=100, uaR=40, faR=80, fiL=110, fiR=110,
          fL=(-0.25, 30), fR=(-0.42, 0.08, -10)), "lin"),                                               # still carried backwards: braking
    (4, P(LOOSE, hz=0.57, spine=170, chest=162, neck=160, head=172, uaL=30, faL=110, uaR=60, faR=130,
          fL=(-0.2, 40), fR=(-0.36, 10)), "in"),                                                       # pivot: weight over the planted feet, leaning into the new way
    (8, P(LOOSE, hz=0.6, spine=166, chest=158, neck=158, head=172, uaL=-10, faL=60, uaR=70, faR=150,
          fL=(0.14, 0.12, 30), fR=(-0.3, 50)), "in"),                                                  # the push-off: the first stride out
    (14, P(RUN0), "smooth"),
], note="turning while running: plant, pivot, push off the other way")

# ---------------------------------------------------------------- the running jump
CLIPS["jump_run"] = clip(20, [
    (0, P(RUN0, fR=(-0.2, 60), fL=(0.3, 0.3, 20)), "lin"),                                              # take-off off the ball of the back foot
    (3, P(LOOSE, hz=0.7, spine=170, chest=164, head=172, uaL=-30, faL=40, uaR=80, faR=150, fiL=120, fiR=120,
          fL=(0.38, 0.36, 10), fR=(-0.36, 0.2, 60)), "out"),                                            # the lead knee driven up, the trail leg long
    (8, P(LOOSE, hz=0.7, spine=172, chest=168, head=174, uaL=20, faL=80, uaR=60, faR=120,
          fL=(0.42, 0.3, 0), fR=(-0.2, 0.3, 40)), "smooth"),                                            # hanging in the air
    (13, P(LOOSE, hz=0.68, spine=176, chest=172, uaL=50, faL=90, uaR=40, faR=90,
           fL=(0.46, 0.14, -14), fR=(0.1, 0.2, 10)), "smooth"),                                         # legs reach for the floor
    (15, P(LOOSE, hz=0.52, spine=160, chest=150, neck=150, head=166, uaL=60, faL=100, uaR=50, faR=96,
           fL=(0.46, 0), fR=(0.12, 20)), "in3"),                                                        # landing: the knees soak it up
    (20, P(RUN0), "smooth"),
], note="a running jump: off the back foot, the lead knee driven, hang, land soft and run on")

# ---------------------------------------------------------------- the vault (an oil drum, 0.88 m)
# the drum stands 0.55 ahead of where he starts. Both hands plant on its lid and stay there while the
# body goes over (the hips swing up and past the hands, the legs tucked to the side), then he drops
# off the far side, absorbs, and is moving again 1.2 m on.
DRUM_X, LID = 0.55, 0.9
CLIPS["vault"] = clip(22, [
    (0, P(LOOSE, hz=0.66, spine=170, chest=164, head=172, uaL=40, faL=90, uaR=30, faR=80, fL=(0.14, 0), fR=(-0.1, 40)), "lin"),
    (3, P(LOOSE, hx=0.12, hz=0.62, spine=150, chest=136, neck=140, head=160, hL=(DRUM_X - 0.02, LID + 0.03), hR=(DRUM_X + 0.02, LID + 0.03),
          fiL=30, fiR=30, fL=(0.3, 30), fR=(0.04, 60)), "in"),                                         # reach, the hands land on the lid
    (6, P(LOOSE, hx=0.34, hz=0.92, spine=144, spine_mid=140, chest=132, neck=136, head=158, hL=(DRUM_X - 0.02, LID + 0.03),
          hR=(DRUM_X + 0.02, LID + 0.03), fiL=20, fiR=20, thL=88, shL=-30, yw_thL=-30, thR=70, shR=-50, yw_thR=-40, fpL=30, fpR=30), "in"),   # hips up over the hands, legs tucked aside
    (9, P(LOOSE, hx=0.62, hz=1.0, spine=160, chest=152, neck=150, head=166, hL=(DRUM_X - 0.02, LID + 0.03), hR=(DRUM_X + 0.02, LID + 0.03),
          fiL=20, fiR=20, thL=50, shL=-10, yw_thL=-24, thR=30, shR=-30, yw_thR=-30, fpL=20, fpR=20), "smooth"),        # over the top, pushing off the lid
    (12, P(LOOSE, hx=0.9, hz=0.74, spine=170, chest=166, head=172, uaL=40, faL=70, uaR=50, faR=80, fiL=60, fiR=60,
           fL=(1.06, 0.12, -10), fR=(0.86, 0.18, 0)), "in"),
    (14, P(LOOSE, hx=1.0, hz=0.55, spine=158, chest=150, neck=150, head=166, uaL=50, faL=90, uaR=40, faR=90,
           fL=(1.12, 0), fR=(0.92, 20)), "in3"),                                                       # landing, the knees soak it
    (18, P(LOOSE, hx=1.12, hz=0.64, spine=172, chest=168, head=174, fL=(1.24, 0), fR=(1.0, 20)), "out"),
    (22, P(LOOSE, hx=1.16, fL=(1.28, 0), fR=(1.12, 0)), "smooth"),
], shift=True, hit=6, note="vaulting a drum: both hands plant on the lid and stay, the body swings over, a soft landing")

# ---------------------------------------------------------------- picking something up
CLIPS["pick_up"] = clip(24, [
    (0, LOOSE, "smooth"),
    (4, P(LOOSE, hx=0.06, fL=(0.26, 0), liftL=0.03, head=170, neck=166), "smooth"),                    # a step towards it, eyes on it
    (9, P(LOOSE, hx=0.08, hz=0.42, spine=150, spine_mid=144, chest=140, neck=140, head=150, hR=(0.38, 0.1), fiR=20,
          uaL=40, faL=60, fL=(0.26, 0), fR=(-0.02, 40)), "in"),                                       # down into a squat, the hand reaching
    (11, P(LOOSE, hx=0.08, hz=0.41, spine=148, spine_mid=142, chest=138, neck=138, head=150, hR=(0.4, 0.08), fiR=150,
           uaL=40, faL=60, fL=(0.26, 0), fR=(-0.02, 40)), "out"),                                      # the hand closes on it
    (17, P(LOOSE, hx=0.06, hz=0.62, spine=172, chest=168, neck=166, head=172, uaR=30, faR=90, fiR=150, fL=(0.26, 0), fR=(-0.02, 10)), "out"),
    (24, P(LOOSE, hx=0.0, uaR=20, faR=70, fiR=150, fL=(0.12, 0), fR=(-0.04, 0), liftL=0.02), "smooth"),
], hit=11, striker="R", note="picking it up: a step, a squat with the back straight, the hand closes, up")

# ---------------------------------------------------------------- pushing (a drum over, into rolling)
CLIPS["push"] = clip(20, [
    (0, LOOSE, "smooth"),
    (4, P(LOOSE, hx=0.08, hz=0.6, spine=160, chest=150, neck=150, head=164, hL=(0.5, 0.78), hR=(0.48, 0.74), fiL=20, fiR=20,
          fL=(0.3, 0), liftL=0.03, fR=(-0.12, 30)), "in"),                                             # both hands on it, set
    (8, P(LOOSE, hx=0.18, hz=0.58, spine=152, chest=140, neck=142, head=160, hL=(0.6, 0.76), hR=(0.58, 0.72), fiL=20, fiR=20,
          fL=(0.3, 20), fR=(-0.12, 64)), "in3"),                                                       # the legs drive, the back flat
    (10, P(LOOSE, hx=0.24, hz=0.6, spine=154, chest=144, hL=(0.76, 0.74), hR=(0.74, 0.7), fiL=20, fiR=20, fL=(0.3, 30), fR=(-0.1, 70)), "lin"),
    (15, P(LOOSE, hx=0.14, hz=0.64, spine=170, chest=164, uaL=40, faL=70, uaR=30, faR=60, fiL=60, fiR=60, fL=(0.3, 0), fR=(0.0, 20), liftR=0.03), "out"),
    (20, P(LOOSE, hx=0.0, fL=(0.12, 0), fR=(-0.04, 0), liftL=0.02), "smooth"),
], hit=9, note="pushing: both hands on it, the legs drive with the back flat, it goes")
