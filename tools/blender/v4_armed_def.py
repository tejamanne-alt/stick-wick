"""The weapon's own defence (docs/design/pipe.md, sword.md, "The block"): on guard, facing him, a blow from the
front lands on the weapon. And when one does get through, he's rocked with both hands still on it.

  rod_block_high     the pipe stood up in front of his face, both hands, the blow stopped on it
  rod_block_low      the pipe dropped point-down in front of his shins
  katana_block_high  jodan uke: the blade raised before his face, its back (mune) taking the blow
  katana_block_low   gedan barai: the blade swept down, knocking the kick aside
  rod_hit            rocked back by a blow, the pipe kept in both hands, back into the guard
  katana_hit         the same with the blade, the point coming back onto his eyes
  hand_shake         (a man) his fist into steel: the hand shaken out, turning away from it
  shin_hurt          (a man) his shin into steel: the foot up, a hand to it, a hop"""
from v4_pose import P
from v4_clips import GUARD, G
from v4_moves import clip
from v4_katana import grip, KG, K, FL, FR
from v4_rod import RG, R

CLIPS = {}
GL, GR = GUARD["fL"], GUARD["fR"]

# ---------------------------------------------------------------- the pipe
CLIPS["rod_block_high"] = clip(16, [
    (0, RG, "lin"),
    (2, R(hz=0.61, spine=172, chest=168, head=166, tw_chest=-4, gx=0.22, gz=0.92, a=172), "in"),             # up in front of his face
    (4, R(hx=-0.06, hz=0.6, spine=178, chest=176, head=168, gx=0.18, gz=0.94, a=176, fR=(FR[0] - 0.08, 20)), "out3"),   # THE BLOW: stopped on it, rocked back
    (8, R(hx=-0.06, hz=0.61, spine=174, chest=170, gx=0.2, gz=0.93, a=174, fR=(FR[0] - 0.08, 26)), "smooth"),
    (16, RG, "smooth"),
], hit=4, striker="R", note="the pipe stood up in front of his face, both hands: the blow stopped on it")

CLIPS["rod_block_low"] = clip(16, [
    (0, RG, "lin"),
    (2, R(hz=0.57, spine=164, chest=158, head=162, gx=0.24, gz=0.82, a=18, fL=(FL[0] - 0.02, 0)), "in"),     # dropped point-down before his shins
    (4, R(hx=-0.05, hz=0.56, spine=166, chest=160, head=164, gx=0.2, gz=0.8, a=14, fR=(FR[0] - 0.06, 24)), "out3"),   # THE KICK: on the steel
    (8, R(hx=-0.05, hz=0.57, spine=165, chest=159, gx=0.22, gz=0.81, a=16, fR=(FR[0] - 0.06, 28)), "smooth"),
    (16, RG, "smooth"),
], hit=4, striker="R", note="the pipe dropped point-down in front of his shins: the kick lands on steel")

CLIPS["rod_hit"] = clip(18, [
    (0, RG, "lin"),
    (2, R(hx=-0.08, hz=0.6, spine=188, spine_mid=190, chest=194, neck=196, head=206, tw_chest=-20, gx=-0.08, gz=1.02, a=220,
          fR=(FR[0] - 0.1, 10)), "out3"),                                                                     # rocked back, the pipe kept in both hands
    (6, R(hx=-0.1, hz=0.58, spine=176, chest=172, head=176, gx=-0.04, gz=1.0, a=214, fR=(FR[0] - 0.12, 20)), "out"),
    (18, RG, "smooth"),
], note="hit: rocked back with the pipe kept in both hands, back into the guard")

# ---------------------------------------------------------------- the katana
CLIPS["katana_block_high"] = clip(16, [
    (0, KG, "lin"),
    (2, K(hz=0.61, spine=172, chest=168, head=166, gx=0.24, gz=1.0, a=166, tw_chest=-6), "in"),             # raised before his face, the back of the blade out
    (4, K(hx=-0.06, hz=0.6, spine=178, chest=176, head=168, gx=0.2, gz=1.02, a=170, fR=(FR[0] - 0.08, 20)), "out3"),  # THE BLOW: on the mune
    (8, K(hx=-0.06, hz=0.61, spine=174, chest=170, gx=0.22, gz=1.01, a=168, fR=(FR[0] - 0.08, 26)), "smooth"),
    (16, KG, "smooth"),
], hit=4, striker="R", note="jodan uke: the blade raised before his face, its back taking the blow")

CLIPS["katana_block_low"] = clip(16, [
    (0, KG, "lin"),
    (2, K(hz=0.58, spine=166, chest=160, head=164, gx=0.34, gz=0.8, a=60), "in"),
    (4, K(hx=-0.03, hz=0.57, spine=164, chest=158, head=164, gx=0.3, gz=0.76, a=16, tw_chest=12, fR=(FR[0] - 0.04, 26)), "in3"),   # THE KICK: swept aside
    (8, K(hx=-0.03, hz=0.58, gx=0.3, gz=0.78, a=20, fR=(FR[0] - 0.04, 30)), "out"),
    (16, KG, "smooth"),
], hit=4, striker="R", note="gedan barai: the blade swept down across, knocking the kick aside")

CLIPS["katana_hit"] = clip(18, [
    (0, KG, "lin"),
    (2, K(hx=-0.08, hz=0.6, spine=188, spine_mid=190, chest=194, neck=196, head=206, gx=0.16, gz=1.0, a=160,
          fR=(FR[0] - 0.1, 10)), "out3"),                                                                     # rocked back, the blade kept
    (6, K(hx=-0.1, hz=0.58, spine=176, chest=172, head=176, gx=0.22, gz=0.9, a=148, fR=(FR[0] - 0.12, 20)), "out"),
    (18, KG, "smooth"),
], note="hit: rocked back with the blade kept in both hands, the point back onto his eyes")

# ---------------------------------------------------------------- the man who hit steel
CLIPS["hand_shake"] = clip(36, [
    (0, GUARD, "lin"),
    (3, G(hx=-0.06, spine=170, chest=164, neck=158, head=150, tw_chest=-30, tw_hips=-14, hR=(0.12, 0.86), fiR=60,
          uaL=40, faL=90, fiL=90, fR=(GR[0] - 0.04, 20)), "out3"),                                          # the fist pulled in to him
    (7, G(hx=-0.08, spine=166, chest=160, head=146, tw_chest=-40, tw_hips=-20, uaR=20, faR=40, haR=10, fiR=30,
          uaL=40, faL=90, fiL=90), "out"),
    (10, G(hx=-0.08, spine=166, chest=160, head=146, tw_chest=-40, uaR=26, faR=70, haR=80, fiR=30, uaL=40, faL=90), "smooth"),  # shaking it out
    (13, G(hx=-0.08, spine=166, chest=160, head=148, tw_chest=-40, uaR=20, faR=40, haR=0, fiR=30, uaL=40, faL=90), "smooth"),
    (16, G(hx=-0.08, spine=166, chest=160, head=148, tw_chest=-38, uaR=26, faR=70, haR=80, fiR=30, uaL=40, faL=90), "smooth"),
    (20, G(hx=-0.08, spine=168, chest=162, head=154, tw_chest=-30, uaR=20, faR=46, haR=20, fiR=40, uaL=50, faL=110), "smooth"),
    (36, GUARD, "smooth"),
], note="his fist into steel: the hand pulled in, shaken out, turning away from it")

CLIPS["shin_hurt"] = clip(38, [
    (0, GUARD, "lin"),
    (3, G(hx=-0.04, hz=0.6, spine=160, chest=152, head=150, fL=(GL[0] - 0.06, 0.16, 20), hL=(0.18, 0.46), fiL=60), "out3"),   # the foot up, a hand to the shin
    (10, G(hx=-0.08, hz=0.62, spine=162, chest=154, head=152, fL=(GL[0] - 0.1, 0.2, 30), hL=(0.14, 0.5), fiL=60,
           fR=(GR[0] - 0.04, 30), liftR=0.04), "smooth"),                                                     # a hop
    (17, G(hx=-0.12, hz=0.6, spine=162, chest=154, head=152, fL=(GL[0] - 0.12, 0.16, 30), hL=(0.12, 0.46), fiL=60,
           fR=(GR[0] - 0.08, 30), liftR=0.04), "smooth"),
    (24, G(hx=-0.12, hz=0.62, spine=168, chest=160, fL=(GL[0] - 0.12, 0), fR=(GR[0] - 0.08, 30)), "out"),  # the foot down, gingerly
    (38, GUARD, "smooth"),
], note="his shin into steel: the foot up, a hand to it, a hop")
