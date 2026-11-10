"""Which part of which CMU recording becomes which stickman move.

Frames are at 30 fps (every 4th frame of the 120 fps CMU data).
start/end/hit: window and impact frame in the recording.
speed:  >1 plays the move faster than the actor did (game timing).
mirror: swap the actor's left and right (choose the striking side).
aim:    "foot_back" turns the move so that `foot` strikes towards -X (behind the hero);
        default faces the actor's pelvis towards +X.
travel: 0 = in place (the game moves the fighter), 1 = keep the actor's travel.

Source: CMU Graphics Lab Motion Capture Database (mocap.cs.cmu.edu), BVH conversion by
B. Hahne. "Free for use in research and commercial projects worldwide."
"""

MOCAP_CLIPS = {
    # fighting stance (boxing guard), looped
    # (the standing "idle" is hand-keyed in build_stickman.py: a still guard with a slow breath. The
    #  boxer's bouncing stance that was here read as dancing whenever anyone stood still.)
    # boxing: lead-hand jab, rear-hand cross (arm straightened at impact)
    "jab":        dict(file="14_01", start=64, end=84, hit=73, speed=1.1, aim="joint_forward", joint="LeftHand", extend="LeftHand"),
    "cross":      dict(file="14_01", start=33, end=55, hit=43, speed=1.1, aim="joint_forward", joint="RightHand", extend="RightHand"),
    # kicks (subject 86): chambered front kick, leaning roundhouse, the same kick driven backwards
    "front_kick": dict(file="86_06", start=860, end=897, hit=877, speed=1.3, aim="joint_forward", joint="RightFoot"),
    "roundhouse": dict(file="86_08", start=997, end=1035, hit=1015, speed=1.3, aim="joint_forward", joint="RightFoot"),
    "back_kick":  dict(file="86_08", start=1063, end=1100, hit=1080, speed=1.3, aim="joint_back", joint="RightFoot"),
    "knee":       dict(file="86_06", start=1555, end=1585, hit=1568, speed=1.2),
    # acrobatic kicks (subjects 90, 88)
    "flying_kick": dict(file="90_05", start=40, end=95, hit=66, speed=1.1, aim="joint_forward", joint="RightFoot"),
    "spin_kick":  dict(file="88_06", start=3, end=44, hit=19, speed=1.1, aim="joint_forward", joint="LeftFoot"),
    # evasion and reactions
    "dodge":      dict(file="88_01", start=3, end=40, hit=None, speed=1.3),
    "knockdown":  dict(file="90_18", start=12, end=60, hit=None, speed=1.15),
    # locomotion
    "run":        dict(file="09_02", start=11, end=33, hit=None, loop=True),

    # ---- CONTACT kit: karate (subject 135), boxing (14), evasion (76), falls and get-ups (90, 139, 140)
    "oi_zuki":      dict(file="135_09", start=228, end=268, hit=251, speed=1.3, aim="joint_forward", joint="LeftHand", extend="LeftHand"),
    "gyaku_zuki":   dict(file="135_09", start=314, end=352, hit=334, speed=1.3, aim="joint_forward", joint="RightHand", extend="RightHand"),
    "side_kick":    dict(file="135_11", start=64, end=110, hit=87, speed=1.3, aim="joint_forward", joint="RightFoot"),
    "mawashi_geri": dict(file="135_07", start=10, end=54, hit=30, speed=1.3, aim="joint_forward", joint="RightFoot"),
    "mae_geri":     dict(file="135_04", start=72, end=116, hit=94, speed=1.3, aim="joint_forward", joint="RightFoot"),
    "gedan_barai":  dict(file="135_05", start=72, end=110, hit=92, speed=1.2),
    "one_two_three": dict(file="14_02", start=378, end=420, hit=398, speed=1.15, aim="joint_forward", joint="LeftHand"),
    "jab_hook":     dict(file="14_03", start=696, end=730, hit=712, speed=1.15, aim="joint_forward", joint="LeftHand"),
    "lead_hook":    dict(file="14_02", start=176, end=210, hit=189, speed=1.15, aim="joint_forward", joint="LeftHand"),
    "slip":         dict(file="76_03", start=92, end=160, hit=None, speed=1.7),
    "fall_face":    dict(file="90_16", start=84, end=130, hit=None, speed=1.1),
    "getup_face":   dict(file="140_01", start=30, end=150, hit=None, speed=1.4),
    "getup_side":   dict(file="140_03", start=40, end=215, hit=None, speed=1.6),
    "getup_back":   dict(file="139_18", start=26, end=200, hit=None, speed=1.6),
}
