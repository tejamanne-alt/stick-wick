"""The score for the final level (docs/story/chapter5_fortress.md): original cues, synthesised here
(drums, strings, brass, a choir pad, piano, the music box) and layered with a few real recordings
from the Oculus audio pack (cinematic impacts, the pipe organ, the creepy strings, the orchestral
stingers). Everything grows out of Maya's music-box motif (memory_maya): in D minor for the
storm and the fights, back in the major for the end.

  storm   48 s loop  the climb and the cutscenes: low strings, slow timpani, the motif high and far
  fight   24 s loop  120 bpm: taiko, a driving string ostinato, brass stabs
  boss    32 s loop  130 bpm: organ, choir, brass, the motif turned menacing
  dark    16 s loop  the blackout: a heartbeat pulse, glassy harmonics, a drone
  duel    32 s loop  140 bpm: the last fight: everything at once, the motif in the brass
  finale  56 s       the reunion and the dawn: piano, strings, the music box, the choir, a full swell

    python tools/audio/make_score.py [cue ...]

Writes Assets/Resources/Audio/music_<cue>/music_<cue>_1.wav."""
import os
import subprocess
import sys
import wave

import numpy as np
from scipy.signal import butter, sosfilt

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio")
O = "/home/user/facebookincubator/meta-open-sound/corpora/oculus-audio-pack/ogg"
rng = np.random.default_rng(5)


def hz(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def t(sec):
    return np.arange(int(sec * SR)) / SR


def lp(x, f, order=2):
    return sosfilt(butter(order, f / (SR / 2), btype="low", output="sos"), x)


def hp(x, f, order=2):
    return sosfilt(butter(order, f / (SR / 2), btype="high", output="sos"), x)


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo / (SR / 2), hi / (SR / 2)], btype="band", output="sos"), x)


def load(stem, start=0.0, dur=None):
    cmd = ["ffmpeg", "-v", "error", "-ss", f"{start}", "-i", os.path.join(O, stem + ".ogg")]
    if dur:
        cmd += ["-t", f"{dur}"]
    cmd += ["-ac", "1", "-ar", str(SR), "-f", "f32le", "-"]
    return np.frombuffer(subprocess.run(cmd, capture_output=True, check=True).stdout, np.float32).astype(np.float64)


def put(buf, x, at, gain=1.0):
    k = int(at * SR)
    if k >= len(buf):
        return
    n = min(len(x), len(buf) - k)
    buf[k:k + n] += x[:n] * gain


def env(n, a, r, sustain=1.0):
    e = np.ones(n) * sustain
    na, nr = int(a * SR), int(r * SR)
    if na:
        e[:na] = np.linspace(0, 1, na)
    if nr:
        e[-nr:] *= np.linspace(1, 0, nr)
    return e


def reverb(x, rt=2.4, wet=0.3):
    """A simple hall: a few comb filters into allpasses (Schroeder), dark."""
    out = np.zeros(len(x) + int(rt * SR))
    out[:len(x)] = x
    tail = np.zeros_like(out)
    for d in (0.0297, 0.0371, 0.0411, 0.0437):
        k = int(d * SR)
        g = 10 ** (-3 * d / rt)
        y = np.zeros_like(out)
        for i in range(k, len(out), k):
            n = min(k, len(out) - i)
            y[i:i + n] = out[i - k:i - k + n] + g * y[i - k:i - k + n]
        tail += y
    tail = lp(tail, 4000)
    return out * (1 - wet) + tail * wet * 0.25


# ---------------------------------------------------------------- instruments

def timpani(n=38, dur=2.2, vel=1.0):
    x = t(dur)
    f = hz(n) * (1 + 0.08 * np.exp(-x * 30))
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-x * 2.2)
    y += 0.5 * lp(rng.standard_normal(len(x)), 900) * np.exp(-x * 30)
    return y * vel


def taiko(vel=1.0, dur=1.2):
    x = t(dur)
    f = 62 * (1 + 0.6 * np.exp(-x * 40))
    y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-x * 4.5)
    y += 0.9 * lp(rng.standard_normal(len(x)), 500, 3) * np.exp(-x * 22)
    return np.tanh(1.6 * y) * vel


def rim(vel=0.5):
    x = t(0.15)
    return bp(rng.standard_normal(len(x)), 1800, 5000) * np.exp(-x * 60) * vel


def saw(f, x, voices=5, detune=0.008):
    y = np.zeros(len(x))
    for v in range(voices):
        d = 1 + detune * (v - (voices - 1) / 2) / max(1, voices - 1) * 2
        ph = (f * d * x + rng.random()) % 1.0
        y += 2 * ph - 1
    return y / voices


def strings(notes, dur, vel=0.5, bright=2600, attack=0.6, release=0.8):
    x = t(dur)
    y = sum(saw(hz(n), x, 7, 0.01) for n in notes) / max(1, len(notes))
    y = lp(y, bright, 2) * env(len(x), attack, release)
    return y * vel * (1 + 0.05 * np.sin(2 * np.pi * 5.2 * x))


def staccato(n, vel=0.5, dur=0.22):
    x = t(dur)
    y = lp(saw(hz(n), x, 4, 0.006), 1800, 2) * np.exp(-x * 9)
    return y * vel


def brass(notes, dur, vel=0.6):
    x = t(dur)
    y = sum(saw(hz(n), x, 3, 0.004) for n in notes) / len(notes)
    cutoff = 400 + 2600 * np.minimum(1, x / 0.12) * np.exp(-x * 1.2) + 500
    out = np.zeros(len(x))
    seg = int(0.01 * SR)
    for i in range(0, len(x), seg):                                       # a crude swept filter, block by block
        out[i:i + seg] = lp(y[max(0, i - 2000):i + seg], float(np.clip(cutoff[i], 200, 8000)))[-len(y[i:i + seg]):]
    return np.tanh(1.4 * out) * env(len(x), 0.03, 0.3) * vel


def choir(notes, dur, vel=0.4):
    """An 'ah' pad: buzzy voices through vowel formants."""
    x = t(dur)
    src = sum(saw(hz(n), x, 6, 0.012) for n in notes) / len(notes)
    y = bp(src, 650, 1000) * 1.0 + bp(src, 1050, 1350) * 0.6 + bp(src, 2500, 3100) * 0.25
    return y * env(len(x), 1.2, 1.4) * vel * 3


def piano(n, dur=2.6, vel=0.6):
    x = t(dur)
    f = hz(n)
    y = sum(np.sin(2 * np.pi * f * k * x * (1 + 0.0004 * k * k)) / k ** 1.3 * np.exp(-x * (1.2 + 0.6 * k)) for k in range(1, 8))
    y += 0.2 * lp(rng.standard_normal(len(x)), 3000) * np.exp(-x * 80)
    return y * vel * np.minimum(1, x / 0.003)


def box(n, dur=2.2, vel=0.6):
    x = t(dur)
    f = hz(n)
    y = (np.sin(2 * np.pi * f * x) + 0.35 * np.sin(2 * np.pi * f * 2 * x) * np.exp(-x * 5)
         + 0.18 * np.sin(2 * np.pi * f * 3.98 * x) * np.exp(-x * 12))
    return y * np.exp(-x * 1.8) * vel * np.minimum(1, x / 0.002)


def heartbeat(vel=0.8):
    x = t(0.9)
    th = np.sin(2 * np.pi * 48 * x) * np.exp(-x * 14)
    y = th.copy()
    put(y, th * 0.7, 0.24)
    return lp(y, 200) * vel


# Maya's motif (memory_maya, in D): the first phrase and its answer
MOTIF = [(0, 74), (1, 78), (2, 81), (3, 79), (4, 78)]
ANSWER = [(0, 76), (1, 79), (2, 83), (3.5, 81)]


def save(cue, y, peak=0.85, fade_out=0.05):
    y = np.asarray(y, dtype=np.float64)
    y -= np.mean(y)
    y = y / (np.max(np.abs(y)) or 1) * peak
    n = int(fade_out * SR)
    if n:
        y[-n:] *= np.linspace(1, 0, n)
    d = os.path.join(ROOT, f"music_{cue}")
    os.makedirs(d, exist_ok=True)
    for f in os.listdir(d):
        if f.endswith(".wav") or f.endswith(".wav.meta"):
            os.remove(os.path.join(d, f))
    path = os.path.join(d, f"music_{cue}_1.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(np.round(y * 32767).astype(np.int16).tobytes())
    print("wrote", os.path.relpath(path), f"{len(y) / SR:.1f}s")


def loop_tail(y, L):
    """Fold the tail past L back onto the start, so the loop joins without a click."""
    out = y[:L].copy()
    tail = y[L:]
    out[:len(tail)] += tail[:L]
    return out


# ---------------------------------------------------------------- the cues

def storm():
    L = 48.0
    buf = np.zeros(int((L + 4) * SR))
    prog = [(50, 57, 62), (46, 53, 62), (43, 50, 58), (45, 52, 61)]       # Dm  Bb  Gm  A
    for i in range(4):
        put(buf, strings(prog[i], 12.8, 0.55, 1600, 2.5, 2.5), i * 12.0)
        put(buf, strings([prog[i][0] - 12], 12.8, 0.5, 600, 2.0, 2.0), i * 12.0)
        for k in range(3):
            put(buf, timpani(38 if i != 3 else 33, 3.0, 0.7 - 0.15 * k), i * 12.0 + k * 4.0)
    # the motif, high and far, once
    for b, n in MOTIF:
        put(buf, strings([n + 12], 1.6, 0.12, 4000, 0.3, 0.8), 24.0 + b * 0.9)
    try:
        cs = load("MUSCStr_StringsCreepy_OS_01")
        put(buf, lp(cs, 3000) * 0.5, 6.0)
        put(buf, lp(load("DSGNDron_DroneCreepy_OS_01"), 1200) * 0.35, 30.0)
    except Exception:
        pass
    save("storm", loop_tail(reverb(buf, 3.0, 0.4), int(L * SR)), 0.8, 0.0)


def fight():
    bpm, bars = 120, 12
    beat = 60 / bpm
    L = bars * 4 * beat
    buf = np.zeros(int((L + 3) * SR))
    roots = [50, 50, 46, 46, 43, 43, 45, 45, 50, 50, 46, 45]
    ost = [0, 0, 3, 0, 2, 0, -2, 0]                                      # D D F D E D C D (over the root)
    for bar in range(bars):
        t0 = bar * 4 * beat
        r = roots[bar]
        for k in (0, 1.5, 2, 3, 3.5):                                     # the taiko pattern
            put(buf, taiko(1.0 if k == 0 else 0.7), t0 + k * beat)
        for k in (1, 3):
            put(buf, rim(0.5), t0 + k * beat)
        for i, step in enumerate(ost):                                    # the ostinato, in eighths
            put(buf, staccato(r + 12 + step, 0.42), t0 + i * beat / 2)
            put(buf, staccato(r + step, 0.3), t0 + i * beat / 2)
        if bar % 2 == 0:                                                  # brass stabs
            put(buf, brass([r + 12, r + 15, r + 19], 0.6, 0.5), t0)
            put(buf, brass([r + 12, r + 15, r + 19], 0.4, 0.4), t0 + 2.5 * beat)
        put(buf, strings([r + 24, r + 27], 4 * beat, 0.16, 3000, 0.4, 0.5), t0)
    try:
        hit = load("DSGNImpt_ImpactCreepyReverb_OS_01")
        put(buf, hit * 0.7, 0.0)
    except Exception:
        pass
    save("fight", loop_tail(reverb(buf, 1.6, 0.22), int(L * SR)), 0.86, 0.0)


def boss():
    bpm, bars = 130, 16
    beat = 60 / bpm
    L = bars * 4 * beat
    buf = np.zeros(int((L + 4) * SR))
    roots = [50, 50, 49, 49, 46, 46, 45, 45] * 2
    for bar in range(bars):
        t0 = bar * 4 * beat
        r = roots[bar]
        for k in range(8):                                                # driving eighths on the drums
            put(buf, taiko(0.95 if k % 4 == 0 else 0.45), t0 + k * beat / 2)
        if bar % 4 == 3:                                                  # a fill
            for k in range(8):
                put(buf, taiko(0.5 + k * 0.06), t0 + 2 * beat + k * beat / 4)
        for i in range(8):
            put(buf, staccato(r + (0 if i % 2 == 0 else 7), 0.4), t0 + i * beat / 2)
        if bar % 4 == 0:
            put(buf, choir([r + 12, r + 15, r + 19, r + 24], 8 * beat, 0.35), t0)
    # the motif, turned minor and slow, in the brass over it
    m = [(0, 74), (1, 77), (2, 81), (3, 79), (4, 77), (6, 76), (7, 74)]
    for rep in (0, 8):
        for b, n in m:
            put(buf, brass([n - 12, n - 24], beat * 1.8, 0.45), (rep * 4 + b) * beat * 1.0 + rep * 0)
    try:
        org = load("MUSCKeyd_PipeOrganCreepy_OS_01")
        put(buf, lp(org, 2500) * 0.6, 0.0)
        put(buf, lp(org, 2500) * 0.5, L / 2)
    except Exception:
        pass
    save("boss", loop_tail(reverb(buf, 2.4, 0.28), int(L * SR)), 0.88, 0.0)


def dark():
    L = 16.0
    buf = np.zeros(int((L + 3) * SR))
    for k in range(int(L / 0.95)):
        put(buf, heartbeat(0.8), k * 0.95)
    x = t(L)
    harm = (np.sin(2 * np.pi * hz(86) * x) * 0.05 + np.sin(2 * np.pi * hz(93) * x) * 0.03) * (0.6 + 0.4 * np.sin(2 * np.pi * 0.13 * x))
    put(buf, harm, 0.0)
    try:
        put(buf, lp(load("DSGNDron_DroneCreepy_OS_02"), 900) * 0.5, 0.0)
    except Exception:
        pass
    save("dark", loop_tail(reverb(buf, 2.8, 0.35), int(L * SR)), 0.75, 0.0)


def duel():
    bpm, bars = 140, 18
    beat = 60 / bpm
    L = bars * 4 * beat
    buf = np.zeros(int((L + 4) * SR))
    roots = [50, 50, 46, 46, 48, 48, 45, 45, 50, 50, 46, 46, 43, 43, 45, 45, 50, 45]
    for bar in range(bars):
        t0 = bar * 4 * beat
        r = roots[bar]
        for k in range(16):                                               # sixteenths: the strings, tremolo
            put(buf, staccato(r + 12 + (0, 7, 12, 7)[k % 4], 0.25, 0.12), t0 + k * beat / 4)
        for k in (0, 0.75, 1.5, 2, 3, 3.5):
            put(buf, taiko(1.0 if k in (0, 2) else 0.6), t0 + k * beat)
        put(buf, timpani(r - 12, 1.2, 0.6), t0)
        put(buf, piano(r + 24, 1.0, 0.25), t0)
        put(buf, piano(r + 27, 1.0, 0.2), t0 + 1.5 * beat)
        if bar % 2 == 1:
            put(buf, choir([r + 12, r + 15, r + 19], 8 * beat, 0.25), t0)
    for rep in (0, 9):                                                    # the motif, full, in the brass
        for b, n in MOTIF + [(b + 6, n) for b, n in ANSWER]:
            put(buf, brass([n - 12, n], beat * 1.6, 0.5), rep * 4 * beat + b * beat)
    save("duel", loop_tail(reverb(buf, 2.0, 0.25), int(L * SR)), 0.9, 0.0)


def finale():
    L = 56.0
    buf = np.zeros(int((L + 6) * SR))
    # 0-14: the music box alone, then the piano joins (D major)
    for i, (b, n) in enumerate(MOTIF + [(b + 6, n) for b, n in ANSWER]):
        put(buf, box(n + 12, 2.6, 0.5), 1.0 + b * 0.75)
    for k, n in enumerate([62, 66, 69, 66, 64, 67, 71, 67]):
        put(buf, piano(n, 3.0, 0.4), 7.0 + k * 0.9)
    # 14-34: strings come in under the motif, then the choir; the swell
    prog = [(50, 57, 62, 66), (47, 54, 62, 66), (43, 50, 59, 62), (45, 52, 57, 61)]
    for i, ch in enumerate(prog):
        put(buf, strings(ch, 5.6, 1.4, 2400, 1.5, 1.5), 14.0 + i * 5.0)
    for b, n in MOTIF + [(b + 6, n) for b, n in ANSWER]:
        put(buf, strings([n, n - 12], 1.8, 1.0, 3500, 0.15, 0.6), 18.0 + b * 1.0)
    put(buf, choir([50, 57, 62, 66, 69], 14.0, 1.2), 22.0)
    put(buf, timpani(38, 4.0, 0.9), 33.0)
    # 34: the climax, a full chord and an orchestral stinger, then it opens out
    put(buf, strings([38, 50, 57, 62, 66, 69, 74], 10.0, 0.9, 5000, 0.2, 4.0), 34.0)
    put(buf, brass([62, 66, 69], 6.0, 0.6), 34.0)
    try:
        put(buf, load("MUSCStngr_VictoryOrchestral_OS_02") * 0.8, 34.0)
    except Exception:
        pass
    # 44-56: the music box again, alone, ending on her note
    for b, n in MOTIF:
        put(buf, box(n + 12, 3.0, 0.45), 45.0 + b * 1.0)
    put(buf, box(74 + 12, 5.0, 0.4), 51.0)
    y = reverb(buf, 3.2, 0.35)[:int(L * SR)]
    save("finale", y, 0.88, 2.5)


CUES = {"storm": storm, "fight": fight, "boss": boss, "dark": dark, "duel": duel, "finale": finale}

if __name__ == "__main__":
    for name in (sys.argv[1:] or list(CUES)):
        CUES[name]()
