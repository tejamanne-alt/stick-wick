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


def scoop(base_keys, frames, table, run_keys):
    """A pick-up on the run: the legs keep the run's own cycle, the body dips on the stride, the hand
    takes it off the floor, and the last key is the armed run's first frame."""
    keys = []
    last = frames[-1]
    for f in frames:
        if f == last:
            keys.append((f, run_keys[0][1], "smooth"))
            continue
        legs = dict(base_keys[f][1])
        over = dict(table[f])
        legs["hz"] = legs["hz"] + over.pop("dip_hz", 0.0)
        keys.append((f, P(legs, **over), "smooth"))
    return keys

# ---------------------------------------------------------------- the gaits
# walking alone: loose, the hands relaxed and swinging low, heel first, a gentle rise over each foot
WALK_SPEED, JOG_SPEED, RUN_SPEED, SNEAK_SPEED = 1.25, 4.0, 4.8, 0.8
# THE WICK WALK (the hero's walk, HOLD): composed and purposeful, made for the speed it's played at.
# Brisk, never hurried (a step every 0.43 s on his short legs); the heel planted firmly, rolling through
# to a push off the toes; a visible rise over each foot; the chest leading a touch, the chin a little down,
# eyes front; the hands loosely closed, soft elbows, a natural swing that goes further forward than back.
CLIPS["walk_alone"] = clip(26, gait(26, WALK_SPEED, duty=0.6, lift=0.06, hz=0.664, bounce=0.02, bounce_low_at_mid=False,
                                    spine=177, chest=172, neck=168, head=173, arm_base=1, arm_amp=19, elbow=11, fist=90,
                                    twist=7, strike=-18, push=38),
                           loop=True, speed=WALK_SPEED, note="the Wick walk: composed, purposeful; heel planted, chest leading, chin down, loose hands")
# THE EVERYDAY WALK (anyone walking in a scene: the men from the car, the guard, Pell, the Driver, Harrow; on the
# hero his own walk stands in for it, ClipPlayer.Substitute). It replaces the first body's four-pose walk (short flat-
# footed steps, a stiff trunk, one arm swinging bent). An ordinary man's unhurried walk, made at that walk's ground
# speed (1.18 m/s: what every scene's playback rate was tuned to, so no scene's feet skate): heel first, rolling
# through to the toes; a rise over each foot; upright, the head level and eyes front (not the hero's chest-led lean);
# open relaxed hands and an easy swing from the shoulder, a little further forward than back; the shoulders turning
# a touch against the hips.
EVERYDAY_SPEED = 1.18
CLIPS["walk"] = clip(24, gait(24, EVERYDAY_SPEED, duty=0.61, lift=0.055, hz=0.666, bounce=0.018, bounce_low_at_mid=False,
                              spine=179, chest=176, neck=172, head=176, arm_base=3, arm_amp=24, elbow=14, fist=60,
                              twist=8, strike=-15, push=34, hands_relaxed=True),
                     loop=True, speed=EVERYDAY_SPEED, note="the everyday walk: unhurried, upright, heel to toe, an easy arm swing")
# THE RUN (TAP, THEN HOLD): one run for every pace from the first stride (3.4 m/s) to flat out (4.6), made
# at 4 m/s so the feet stay planted across the range. A flight phase, a midfoot landing under the hips,
# leaning in from the ankles, the head steady and level, the arms bent near ninety and driving forward and
# back at chest height (not up at the chin: he's running somewhere, not squaring up).
CLIPS["run"] = clip(16, gait(16, JOG_SPEED, duty=0.36, lift=0.15, hz=0.648, bounce=0.03, bounce_low_at_mid=True,
                             spine=170, chest=166, neck=164, head=172, arm_base=-3, arm_amp=44, elbow=70, fist=125,
                             twist=11, lean_pump=1.4, strike=6, push=50),
                    loop=True, speed=JOG_SPEED, note="the run: leaning in, midfoot under the hips, arms driving at chest height")
# the legs of a pick-up on the run (rod_scoop, katana_scoop): the run's cycle, 14 frames
SCOOP_LEGS = gait(14, JOG_SPEED, duty=0.4, lift=0.1, hz=0.64, bounce=0.02, bounce_low_at_mid=True,
                  spine=170, chest=166, neck=164, head=172, arm_base=-3, arm_amp=40, elbow=70, fist=125,
                  twist=8, lean_pump=1.0, strike=6, push=48)
# running flat out: leaning in, high knees, the arms driving hard
CLIPS["sprint"] = clip(16, gait(16, RUN_SPEED, duty=0.31, lift=0.2, hz=0.635, bounce=0.035, bounce_low_at_mid=True,
                                spine=166, chest=160, neck=158, head=170, arm_base=18, arm_amp=58, elbow=84, fist=140,
                                twist=16, lean_pump=2.5, strike=8, push=58),
                       loop=True, speed=RUN_SPEED, note="flat out: leaning in, knees high, arms driving")
# sneaking: crouched low, long slow steps, the feet placed (rolled heel to toe without a sound)
CLIPS["sneak"] = clip(30, gait(30, SNEAK_SPEED, duty=0.7, lift=0.07, hz=0.48, bounce=0.004, bounce_low_at_mid=False,
                               spine=160, chest=152, neck=150, head=170, arm_base=28, arm_amp=9, elbow=42, fist=80,
                               twist=5, strike=8, push=20),
                      loop=True, speed=SNEAK_SPEED, note="creeping: low and level, each foot set down toes first, the hands low and close")

# ---------------------------------------------------------------- standing alone
# simply standing: both feet planted, the weight even, the hands hanging still (half open), the
# shoulders down. Nothing walks or fidgets: only the breath, and the head now and then looking a
# little one way, then the other (a story stand, not a fight)
LOOSE = P(hx=0.0, hz=0.676, hips=180, spine=178, spine_mid=177, chest=176, neck=174, head=178,
          clL=(0, 0), clR=(0, 0), uaL=4, faL=12, haL=8, uaR=-4, faR=6, haR=2, fiL=55, fiR=55,
          fL=(0.12, 0), fR=(-0.04, 0))
CLIPS["idle_alone"] = clip(150, [
    (0, LOOSE, "smooth"),
    (30, P(LOOSE, head=177, tw_head=-7), "smooth"),           # a glance one way (the head only)
    (60, P(LOOSE, head=177, tw_head=-7), "smooth"),
    (90, P(LOOSE, head=178, tw_head=6), "smooth"),            # and the other
    (120, P(LOOSE, head=178, tw_head=6), "smooth"),
    (150, LOOSE, "smooth"),
], loop=True, breath=True, breaths=4, note="standing alone: feet planted, hands still, breathing, the head looking about a little")

CROUCH = P(hx=0.0, hz=0.47, hips=180, spine=158, spine_mid=154, chest=150, neck=148, head=168,
           clL=(8, 4), clR=(8, 4), uaL=34, faL=70, uaR=18, faR=98, fiL=70, fiR=90,     # one hand low for balance, one close
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
# ---------------------------------------------------------------- jumping (swipe up), timed to the physics
# The game launches him at once (5.6 m/s up: 0.25 s to the top, 0.51 s in the air), so the clip is the
# body's part of it: a quick dip and drive (the arms swinging up to the chest, never flung overhead),
# a compact tuck at the top, the legs reaching for the floor as he comes down. It holds its last frame if
# he falls further; the landing is its own clip (land_light, or land_soft / land_heavy from higher).
JUMP_REACH = P(LOOSE, hz=0.70, spine=176, chest=174, neck=170, head=174, uaL=22, faL=58, uaR=18, faR=52, fiL=100, fiR=100,
               fL=(0.12, 0.035, -12), fR=(-0.06, 0.025, -4))
CLIPS["jump"] = clip(15, [
    (0, P(LOOSE), "lin"),
    (1, P(LOOSE, hz=0.58, spine=168, chest=160, neck=160, head=168, uaL=-22, faL=-8, uaR=-18, faR=-6,
          fL=(0.1, 0), fR=(-0.06, 0)), "out"),                                                          # the dip, the arms back
    (3, P(LOOSE, hz=0.71, spine=178, chest=177, neck=172, head=175, uaL=44, faL=128, uaR=38, faR=122, fiL=120, fiR=120,
          fL=(0.05, 0.03, 42), fR=(-0.05, 0.02, 46)), "out"),                                           # the drive: up off the toes, the arms up to the chest
    (7, P(LOOSE, hz=0.70, spine=174, chest=170, neck=168, head=172, uaL=38, faL=118, uaR=34, faR=112, fiL=120, fiR=120,
          fL=(0.13, 0.27, 6), fR=(-0.02, 0.21, 14)), "smooth"),                                         # the top: knees up, compact
    (11, P(LOOSE, hz=0.70, spine=175, chest=172, neck=169, head=173, uaL=28, faL=80, uaR=24, faR=74, fiL=110, fiR=110,
           fL=(0.12, 0.09, -8), fR=(-0.06, 0.07, 2)), "smooth"),                                        # coming down: the legs open
    (15, JUMP_REACH, "smooth"),                                                                         # reaching for the floor
], note="the jump: a quick dip, the drive (arms to the chest), a compact tuck, the legs reaching down")

# a running jump: off the back foot, the lead knee driven, a short hang, the legs reaching (0.43 s in the air)
RUN_REACH = P(LOOSE, hz=0.68, spine=176, chest=172, uaL=32, faL=104, uaR=-10, faR=56, fiL=120, fiR=120,
              fL=(0.46, 0.06, -14), fR=(0.1, 0.1, 10))
CLIPS["jump_run"] = clip(13, [
    (0, P(RUN0, fR=(-0.2, 60), fL=(0.3, 0.3, 20)), "lin"),                                              # take-off off the ball of the back foot
    (2, P(LOOSE, hz=0.7, spine=170, chest=164, head=172, uaL=-30, faL=40, uaR=80, faR=150, fiL=120, fiR=120,
          fL=(0.38, 0.36, 10), fR=(-0.36, 0.2, 60)), "out"),                                            # the lead knee driven up, the trail leg long
    (6, P(LOOSE, hz=0.7, spine=172, chest=168, head=174, uaL=20, faL=80, uaR=60, faR=120,
          fL=(0.42, 0.3, 0), fR=(-0.2, 0.3, 40)), "smooth"),                                            # hanging in the air
    (10, P(LOOSE, hz=0.69, spine=175, chest=171, uaL=36, faL=104, uaR=-4, faR=62,
           fL=(0.46, 0.12, -14), fR=(0.06, 0.16, 12)), "smooth"),                                       # the legs reach for the floor
    (13, RUN_REACH, "smooth"),
], note="a running jump: off the back foot, the lead knee driven, a hang, the legs reaching")

# landing a jump: the knees take it (a short soak), then up (standing) or straight on (running)
CLIPS["land_light"] = clip(8, [
    (0, JUMP_REACH, "lin"),
    (2, P(LOOSE, hz=0.55, spine=164, chest=156, neck=156, head=166, uaL=30, faL=52, uaR=26, faR=48, fiL=100, fiR=100,
          fL=(0.12, 0), fR=(-0.06, 0)), "out3"),                                                        # the knees take it
    (5, P(LOOSE, hz=0.64, spine=174, chest=170, neck=168, head=174, fL=(0.12, 0), fR=(-0.06, 0)), "out"),
    (8, P(LOOSE), "smooth"),
], note="landing a jump: the knees take it, up")
CLIPS["land_run"] = clip(6, [
    (0, RUN_REACH, "lin"),
    (2, P(RUN0, hz=0.56, spine=162, chest=156, neck=156, head=166, fL=(0.3, 0), fR=(-0.08, 0.05, 40)), "out3"),   # the lead foot takes it, knees soak
    (6, P(RUN0), "smooth"),                                                                              # and straight on into the run
], note="landing a running jump: the lead foot takes it and he runs straight on")

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


# ---------------------------------------------------------------- the people who aren't fighting (story)
# A security guard on his post: feet a shoulder apart, weight even, the hands clasped in front at the belt,
# the chin up, the eyes moving; breathing. Not a fighter's guard (that was what he used to stand in).
SENTRY = P(LOOSE, hz=0.672, spine=180, spine_mid=180, chest=180, neck=176, head=180, clL=(6, 0), clR=(6, 0),
           hL=(0.13, 0.66), hR=(0.15, 0.68), fiL=90, fiR=120, fL=(0.13, 0), fR=(-0.07, 0))
CLIPS["sentry"] = clip(150, [
    (0, SENTRY, "smooth"),
    (40, P(SENTRY, head=179, tw_head=-12), "smooth"),           # a look along the street
    (75, P(SENTRY, head=180, tw_head=-12), "smooth"),
    (110, P(SENTRY, head=181, tw_head=8), "smooth"),            # and back the other way
    (150, SENTRY, "smooth"),
], loop=True, breath=True, breaths=4, note="a security guard on his post: hands clasped at the belt, chin up, watching")

# a key in a lock at hand height: the hand goes to it, the wrist turns twice (the lock, the bolt), stays
CLIPS["unlock"] = clip(40, [
    (0, LOOSE, "smooth"),
    (8, P(LOOSE, spine=176, chest=174, head=172, hR=(0.32, 1.0), haR=90, fiR=150, twR=0), "out"),         # the key into the lock
    (14, P(LOOSE, spine=176, chest=174, head=172, hR=(0.33, 1.0), haR=90, fiR=150, twR=80), "in"),        # turned
    (18, P(LOOSE, spine=176, chest=174, head=172, hR=(0.33, 1.0), haR=90, fiR=150, twR=0), "out"),
    (24, P(LOOSE, spine=176, chest=174, head=172, hR=(0.33, 1.0), haR=90, fiR=150, twR=85), "in"),        # and again: the bolt
    (32, P(LOOSE, spine=177, chest=175, head=174, hR=(0.3, 0.98), haR=90, fiR=150, twR=85), "smooth"),
    (40, P(LOOSE, spine=177, chest=175, head=174, hR=(0.3, 0.98), haR=90, fiR=150, twR=85), "smooth"),
], note="a key in a lock at hand height: in, turned twice, held")

# carrying a box in both hands before him, at the waist (StoryBank.CarriedBy: 0.36 forward, 0.95 up)
CARRY = dict(hL=(0.3, 0.93), hR=(0.33, 0.95), fiL=110, fiR=110, haL=96, haR=96, clL=(10, 2), clR=(10, 2))
CLIPS["carry_stand"] = clip(60, [
    (0, P(LOOSE, spine=178, chest=176, **CARRY), "smooth"),
    (30, P(LOOSE, spine=178, chest=176, hz=0.674, **CARRY), "smooth"),
    (60, P(LOOSE, spine=178, chest=176, **CARRY), "smooth"),
], loop=True, breath=True, breaths=2, note="holding a box in both hands before him")


def _carry_walk():
    keys = []
    for f, p, e in gait(26, WALK_SPEED, duty=0.6, lift=0.05, hz=0.664, bounce=0.016, bounce_low_at_mid=False,
                        spine=178, chest=176, neck=170, head=174, arm_base=0, arm_amp=0, elbow=10, fist=110,
                        twist=4, strike=-16, push=34):
        bob = p["hz"] - 0.664
        c = dict(CARRY)
        c["hL"] = (0.3, 0.93 + bob)
        c["hR"] = (0.33, 0.95 + bob)
        keys.append((f, P(p, **c), e))
    return keys


CLIPS["carry_walk"] = clip(26, _carry_walk(), loop=True, speed=WALK_SPEED, note="walking with a box held in both hands before him")
