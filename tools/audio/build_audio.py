#!/usr/bin/env python3
"""Builds the game's sound bank into Assets/Resources/Audio/<category>/ from two free libraries:

  * Oculus Audio Pack 01 (real studio recordings, CC BY 4.0, credit required), as mirrored in
    https://github.com/facebookincubator/meta-open-sound  (corpora/oculus-audio-pack/ogg)
  * SFX Kit (CC0), https://github.com/wyzowl/sfx-kit  (sounds/)

Every one-shot is trimmed of leading silence (so a punch sounds on the frame it lands),
folded to mono, loudness-normalised, and some are layered (a recorded punch + a deep body
thump, a finisher + a bone crack + a sub boom). Loops stay stereo.

  python3 tools/audio/build_audio.py --oculus <meta-open-sound>/corpora/oculus-audio-pack/ogg \
                                     --kit <sfx-kit>/sounds
Needs ffmpeg.
"""
import argparse
import os
import shutil
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Audio")

# category -> list of entries. An entry is a source, or a tuple of layers [(source, gain dB, delay ms), ...].
# "o:" = Oculus pack file stem, "k:" = SFX Kit path (without .wav).
def o(stem): return "o:" + stem
def k(path): return "k:" + path

def rng(stem, n, start=1): return [o(f"{stem}_OS_{i:02d}") for i in range(start, start + n)]

BANK = {
    # --- fists and feet: real recorded punches, the heavy ones with a deep body thump under them
    "punch_light": rng("FGHTImpt_PunchRegular", 6),
    "punch_heavy": [[(o(f"FGHTImpt_PunchHarder_OS_{i:02d}"), 0, 0), (k("impacts/impact-deep-soft"), -9, 0)] for i in range(1, 5)],
    "kick": [[(o(f"FGHTImpt_Kick_OS_{i:02d}"), 0, 0), (k("impacts/impact-heavy-soft"), -12, 0)] for i in range(1, 6)],
    "finisher": [
        [(o("FGHTImpt_PunchHardest_OS_01"), 0, 0), (o("GOREBone_BonesBreaking_OS_01"), -6, 30), (k("booms-and-bass/boom-hit-medium"), -6, 0)],
        [(o("FGHTImpt_PunchHarder_OS_02"), 0, 0), (o("GOREBone_SkullCracking_OS_01"), -5, 20), (k("booms-and-bass/boom-hit-strong"), -8, 0)],
        [(o("FGHTImpt_Kick_OS_03"), 0, 0), (o("GOREBone_BonesBreaking_OS_02"), -6, 25), (k("booms-and-bass/boom-hit-medium"), -6, 0)],
    ],
    "bone": rng("GOREBone_BonesBreaking", 3) + rng("GOREBone_SkullCracking", 2),
    # --- movement
    "whoosh": rng("SWSH_Swoosh", 6) + rng("WHSH_WhooshSmall", 3),
    "whoosh_big": rng("WHSH_WhooshMedium", 3) + [k("whooshes/whoosh-fast-hard"), k("whooshes/whoosh-medium-hard")],
    "footstep": rng("FEETHmn_ShoeConcreteRun", 5),
    "land": rng("FEETHmn_ShoeConcreteLand", 2),
    "cloth": rng("CLOTHMvmt_MovementWithGear", 4) + rng("CLOTHMvmt_ClothingMovement", 4),
    "body_fall": [[(o(f"FEETHmn_ShoeConcreteLand_OS_0{i}"), -2, 0), (k(f"impacts/impact-heavy-{s}"), -3, 0), (o(f"CLOTHMvmt_MovementWithGear_OS_0{i}"), -8, 40)]
                  for i, s in ((1, "firm"), (2, "hard"))],
    # --- weapons
    "sword_swing": rng("WEAPSwrd_Sword", 2) + [k("swishes-and-zips/swish-hard")],
    "sword_hit": [[(o("WEAPSwrd_Sword_OS_01"), -4, 0), (o("GORESqsh_BloodSquishSlimy_OS_01"), -4, 30), (o("FGHTImpt_PunchHarder_OS_01"), -6, 30)],
                  [(o("WEAPSwrd_Sword_OS_02"), -4, 0), (o("GORESqsh_BloodSquishSlimy_OS_02"), -4, 30), (o("FGHTImpt_PunchHarder_OS_03"), -6, 30)],
                  [(o("WEAPKnif_KnifeScrape_OS_01"), -4, 0), (o("GORESqsh_BloodSquishSlimy_OS_03"), -4, 20), (o("FGHTImpt_PunchRegular_OS_02"), -6, 20)]],
    "sword_clash": [o("WEAPSwrd_SwordClash_OS_01")] + rng("WEAPSwrd_SwordScrape", 2),
    "pipe_hit": [[(o(f"METLImpt_MetalHitSurface_OS_{i:02d}"), 0, 0), (o(f"FGHTImpt_PunchHarder_OS_{1 + i % 4:02d}"), -5, 0)] for i in range(1, 6)],
    "knife": [o("WEAPKnif_KnifeScrape_OS_01"), k("swishes-and-zips/zip-in-hard")],
    "shell": rng("BLLTShel_ShellCasingConcrete", 3),
    "gun_body": [k("impacts/impact-light-hard"), k("impacts/impact-medium-hard")],
    "gun_tail": [k("booms-and-bass/boom-hit-soft"), k("cinematic-hits/cinematic-thunder-big")],
    "pickup": [k("mechanical/mechanical-latch-heavy"), k("mechanical/mechanical-ratchet-light"), o("MECHLvr_LeverPull_OS_01")],
    # --- the room
    "crate": [[(o("WOODImpt_LargeWoodDrop_OS_01"), 0, 0), (o("WEAPAxe_WoodCutAxe_OS_01"), -4, 0)], o("WOODImpt_LargeWoodDrop_OS_01")],
    "metal_clang": rng("METLImpt_MetalObjDrop", 4) + [o("METLImpt_BucketDrop_OS_01")],
    "glass": [o("GLASBrk_WindowPaneSmallBreak_OS_01"), o("GLASBrk_GlassBottleBreak_OS_01")],
    "door": [o("DOORWood_FrontDoorOpen_OS_01"), o("DOORSlid_SlidingDoorOpen_OS_01"), o("DOORMetl_HatchOpen_OS_01")],
    "wall_splat": [[(o("FGHTImpt_PunchHardest_OS_01"), -2, 0), (o("WOODImpt_LargeWoodDrop_OS_01"), -4, 0), (o("GOREBone_BonesBreaking_OS_03"), -7, 40)]],
    # --- voices
    "hero_hurt": rng("VOXMale_Male01Hurt", 3),
    "hero_effort": [o("VOXEfrt_Male01Effort_OS_01")],
    "hero_death": rng("VOXMale_Male01Death", 3),
    "enemy_hurt": rng("VOXMale_Male02Hurt", 3),
    "enemy_effort": [o("VOXEfrt_Male02Effort_OS_01")],
    "enemy_death": rng("VOXMale_Male02Death", 3) + [o("VOXMale_Male02DeathScream_OS_01")],
    "brute_hurt": rng("CREAHmn_OgreHurt", 3),
    "brute_death": [o("CREAHmn_OgreDeathScream_OS_01")] + rng("CREAHmn_OgreDeath", 2),
    "brute_laugh": rng("CREAHmn_OgreLaugh", 3),
    "fall_scream": rng("VOXScrm_MaleScream", 3),
    # --- cinematic layer
    "slowmo_in": [k("reverse-whooshes/reverse-whoosh-short-strong"), k("reverse-whooshes/reverse-whoosh-medium-strong")],
    "slowmo_out": [k("whooshes/whoosh-slow-soft"), k("whooshes/whoosh-slow-medium")],
    "cine_hit": [k("cinematic-hits/cinematic-slam-big"), k("cinematic-hits/cinematic-drum-big"), k("cinematic-hits/cinematic-metal-big")],
    "cine_massive": [k("cinematic-hits/cinematic-drum-massive"), k("cinematic-hits/cinematic-slam-massive")],
    "riser": [k("risers-and-fallers/riser-2s-strong")],
    "boom": [k("booms-and-bass/boom-drop-strong"), k("booms-and-bass/bass-sub-drop-medium")],
    "rank_up": [k("cinematic-hits/cinematic-metal-big")],
    "wave_clear": [o("MUSCStngr_SnareCrash_OS_01")],
    "floor_clear": rng("MUSCStngr_VictoryOrchestral", 3),
    "defeat": rng("MUSCStngr_LossOrchestral", 3),
    # --- loops (stereo, not trimmed)
    "amb_room": [o("AMBInd_RoomToneMachineRoom_LOOP_01")],
    "amb_rain": [o("RAIN_CityRain_LOOP_01")],
    "amb_tension": rng("DSGNDron_DroneCreepy", 1),
}
LOOPS = {"amb_room", "amb_rain", "amb_tension"}


def src_path(ref, oculus, kit):
    kind, name = ref.split(":", 1)
    p = os.path.join(oculus, name + ".ogg") if kind == "o" else os.path.join(kit, name + ".wav")
    if not os.path.exists(p):
        sys.exit(f"missing source: {p}")
    return p


def render(layers, dst, loop, oculus, kit):
    """Mix the layers to a temp WAV, then peak-normalise (one-shots to -1 dBFS, loops to -6)
    and encode to Ogg Vorbis."""
    tmp = dst + ".tmp.wav"
    cmd = ["ffmpeg", "-v", "error", "-y"]
    for ref, _, _ in layers:
        cmd += ["-i", src_path(ref, oculus, kit)]
    chains = []
    for i, (_, gain, delay) in enumerate(layers):
        f = f"[{i}:a]aresample=44100,aformat=channel_layouts={'stereo' if loop else 'mono'}"
        if not loop:
            f += ",silenceremove=start_periods=1:start_threshold=-48dB"
        if gain:
            f += f",volume={gain}dB"
        if delay:
            f += f",adelay={delay}:all=1"
        chains.append(f + f"[l{i}]")
    mix = "".join(f"[l{i}]" for i in range(len(layers)))
    graph = ";".join(chains) + f";{mix}amix=inputs={len(layers)}:normalize=0:duration=longest"
    if not loop:
        graph += ",areverse,afade=t=in:d=0.03,areverse"   # a short tail fade so nothing clicks
    cmd += ["-filter_complex", graph + "[out]", "-map", "[out]", tmp]
    subprocess.run(cmd, check=True)

    probe = subprocess.run(["ffmpeg", "-v", "info", "-i", tmp, "-af", "volumedetect", "-f", "null", "-"],
                           capture_output=True, text=True).stderr
    peak = 0.0
    for line in probe.splitlines():
        if "max_volume:" in line:
            peak = float(line.split("max_volume:")[1].split("dB")[0])
    gain = (-6.0 if loop else -1.0) - peak
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-af", f"volume={gain:.2f}dB",
                    "-c:a", "libvorbis", "-q:a", "6", dst], check=True)
    os.remove(tmp)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--oculus", required=True)
    ap.add_argument("--kit", required=True)
    a = ap.parse_args()
    if os.path.isdir(OUT):
        for name in os.listdir(OUT):
            p = os.path.join(OUT, name)
            if os.path.isdir(p):
                shutil.rmtree(p)
    n = 0
    for cat, entries in BANK.items():
        d = os.path.join(OUT, cat)
        os.makedirs(d, exist_ok=True)
        for i, e in enumerate(entries):
            layers = e if isinstance(e, list) else [(e, 0, 0)]
            render(layers, os.path.join(d, f"{cat}_{i + 1:02d}.ogg"), cat in LOOPS, a.oculus, a.kit)
            n += 1
    print(f"{n} clips in {len(BANK)} categories -> {OUT}")


if __name__ == "__main__":
    main()
