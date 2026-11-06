"""Synthesises the story-mode sound effects (no recordings needed): thunder, roof drip, the
splat between the eyes, tinnitus, heartbeat, electrical static, reversed whispers, a cello
drone, the shutter latch, footsteps in shallow water, a ragged breath and wind through vents.

    python tools/audio/make_story_sfx.py

Writes 16-bit mono WAVs into Assets/Resources/Audio/<sound>/ (the Sound mixer loads every clip
in a sound's folder and picks a take at random). Deterministic: the same files every run."""
import os
import wave
import numpy as np
from scipy.signal import butter, lfilter

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio")
rng = np.random.default_rng(7)


def t(sec):
    return np.arange(int(sec * SR)) / SR


def bp(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), hi / (SR / 2)], btype="band")
    return lfilter(b, a, x)


def lp(x, hi, order=2):
    b, a = butter(order, hi / (SR / 2), btype="low")
    return lfilter(b, a, x)


def hp(x, lo, order=2):
    b, a = butter(order, lo / (SR / 2), btype="high")
    return lfilter(b, a, x)


def env(n, attack, release, total=None):
    """Attack then exponential-ish release over n samples."""
    a = max(1, int(attack * SR))
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a)
    k = np.arange(n - a) / SR
    e[a:] = np.exp(-k / max(release, 1e-4))
    return e


def echoes(x, delays=(0.09, 0.17, 0.29, 0.43), gain=0.45):
    """A big empty room: a few decaying reflections."""
    out = np.concatenate([x, np.zeros(int(0.8 * SR))])
    for i, d in enumerate(delays):
        n = int(d * SR)
        out[n:n + len(x)] += x * gain ** (i + 1)
    return out


def save(name, x, take=1, peak=0.9):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    fade = min(len(x) // 10, int(0.01 * SR))
    if fade > 0:
        x[-fade:] *= np.linspace(1, 0, fade)
    d = os.path.join(ROOT, name)
    os.makedirs(d, exist_ok=True)
    path = os.path.join(d, f"{name}_{take}.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print("wrote", os.path.relpath(path))


def thunder(take):
    n = int(5.5 * SR)
    crack = hp(rng.standard_normal(n), 1500) * env(n, 0.002, 0.08) * 1.4
    brown = np.cumsum(rng.standard_normal(n))
    brown = hp(brown, 20)
    rumble = lp(brown, 160 + 40 * take, 3)
    mod = 0.6 + 0.4 * np.abs(lp(rng.standard_normal(n), 3.0, 1)) * 30
    rumble *= env(n, 0.06, 1.6 + 0.4 * take) * np.clip(mod, 0.2, 1.6)
    rumble /= np.max(np.abs(rumble)) + 1e-9
    roll = lp(hp(rng.standard_normal(n), 60), 700) * env(n, 0.03, 0.9) * 0.5
    save("thunder", crack * 0.7 + rumble + roll, take)


def drip(take):
    x = t(0.16)
    f = (1300 + 150 * take) * np.exp(-x * 9) + 650
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(x), 0.001, 0.035)
    click = hp(rng.standard_normal(len(x)), 3000) * env(len(x), 0.0005, 0.004) * 0.4
    save("drip", echoes(tone + click, gain=0.5), take, peak=0.7)


def splat(take):
    n = int(0.35 * SR)
    wet = bp(rng.standard_normal(n), 250, 3500) * env(n, 0.001, 0.05 + 0.01 * take)
    thump = np.sin(2 * np.pi * 85 * t(0.35)) * env(n, 0.002, 0.06)
    spray = hp(rng.standard_normal(n), 4000) * env(n, 0.01, 0.12) * 0.25
    save("splat", wet + thump * 0.8 + spray, take)


def tinnitus(take):
    x = t(3.5)
    f = 7200 + 30 * np.sin(2 * np.pi * 0.7 * x)
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.3 * np.sin(2 * np.pi * np.cumsum(f * 1.007) / SR)
    e = np.minimum(1, x / 0.15) * np.exp(-x / 1.4)
    save("tinnitus", tone * e, take, peak=0.5)


def heartbeat(take):
    x = t(0.9)
    def beat(at, amp, f0):
        y = np.zeros_like(x)
        m = x >= at
        k = x[m] - at
        y[m] = amp * np.sin(2 * np.pi * (f0 - 20 * k) * k) * np.exp(-k / 0.07) * np.minimum(1, k / 0.004)
        return y
    y = beat(0.0, 1.0, 58) + beat(0.2, 0.7, 52)
    save("heartbeat", lp(y, 300), take)


def static(take):
    n = int(0.45 * SR)
    noise = bp(rng.standard_normal(n), 900, 9000)
    crackle = np.zeros(n)
    idx = rng.integers(0, n, 140)
    crackle[idx] = rng.choice([-1, 1], 140) * rng.uniform(1.5, 3, 140)
    gate = (lp(rng.standard_normal(n), 30) > -0.002).astype(float) * 0.6 + 0.4
    hum = np.sin(2 * np.pi * 120 * t(0.45)) * 0.3
    save("static", (noise * gate + crackle + hum) * env(n, 0.003, 0.25), take)


def whispers(take):
    n = int(0.9 * SR)
    y = np.zeros(n)
    for v in range(4):
        src = rng.standard_normal(n)
        f1 = 500 + 400 * rng.random()
        f2 = 1500 + 900 * rng.random()
        voice = bp(src, f1 * 0.8, f1 * 1.25) + 0.7 * bp(src, f2 * 0.85, f2 * 1.2)
        syll = np.clip(np.sin(2 * np.pi * (4 + 3 * rng.random()) * t(0.9) + rng.random() * 6), 0, 1) ** 2
        y += voice * syll * (0.6 + 0.4 * rng.random())
    y *= np.linspace(0.05, 1, n) ** 1.5          # reversed: swells up, then cuts
    save("whispers", echoes(y[::-1][::-1], gain=0.35), take, peak=0.6)


def drone(take):
    x = t(7.0)
    f0 = 65.41 * (1 + 0.004 * np.sin(2 * np.pi * 5.2 * x))       # C2 with a bowed vibrato
    saw = np.zeros_like(x)
    for h in range(1, 14):
        saw += np.sin(2 * np.pi * h * np.cumsum(f0) / SR) / h * (0.92 ** h)
    fifth = np.zeros_like(x)
    for h in range(1, 9):
        fifth += np.sin(2 * np.pi * h * np.cumsum(f0 * 1.5) / SR) / h * (0.9 ** h)
    bow = 1 + 0.08 * lp(rng.standard_normal(len(x)), 8) * 40
    y = lp(saw + 0.45 * fifth, 900, 2) * bow
    y *= np.minimum(1, x / 3.5) ** 2 * np.minimum(1, (7.0 - x) / 1.2)      # swells in
    save("drone", y, take, peak=0.8)


def latch(take):
    n = int(0.9 * SR)
    x = t(0.9)
    partials = [(620, 1.0, 0.18), (1370, 0.7, 0.12), (2210, 0.5, 0.09), (3170, 0.35, 0.06), (4630, 0.2, 0.04)]
    ring = sum(a * np.sin(2 * np.pi * f * (1 + 0.01 * take) * x) * np.exp(-x / d) for f, a, d in partials)
    click = hp(rng.standard_normal(n), 2000) * env(n, 0.0005, 0.01)
    second = np.zeros(n)
    k = int(0.09 * SR)
    second[k:] = 0.6 * ring[:n - k]
    save("latch", echoes(ring * 0.6 + click + second, delays=(0.12, 0.23, 0.37), gain=0.35), take)


def splash(take):
    n = int(0.3 * SR)
    hit = bp(rng.standard_normal(n), 700, 4500) * env(n, 0.002, 0.04 + 0.01 * take)
    slosh = bp(rng.standard_normal(n), 300, 1200) * env(n, 0.03, 0.08) * 0.5
    thud = np.sin(2 * np.pi * 70 * t(0.3)) * env(n, 0.002, 0.03) * 0.6
    save("splash", hit + slosh + thud, take, peak=0.7)


def breath(take):
    n = int(0.9 * SR)
    x = t(0.9)
    air = bp(rng.standard_normal(n), 450, 2400)
    shake = 1 + 0.35 * np.sin(2 * np.pi * 11 * x) * (x < 0.6)           # ragged
    e = np.minimum(1, x / 0.45) * np.exp(-np.maximum(0, x - 0.5) / 0.12)
    save("breath", air * shake * e, take, peak=0.6)


def wind(take):
    x = t(6.0)
    src = rng.standard_normal(len(x))
    centre = 520 + 260 * np.sin(2 * np.pi * 0.11 * x + take)
    y = np.zeros_like(x)
    block = 2048
    for s in range(0, len(x), block):
        c = centre[s]
        y[s:s + block] = bp(src[s:s + block + 0], c * 0.85, c * 1.15)[:len(x[s:s + block])]
    y = lp(y, 1400)
    y *= np.minimum(1, x / 1.5) * np.minimum(1, (6.0 - x) / 1.5)
    save("wind", y, take, peak=0.6)


if __name__ == "__main__":
    for take in (1, 2):
        thunder(take); drip(take); splat(take); latch(take); splash(take); breath(take)
    tinnitus(1); heartbeat(1); static(1); static(2); whispers(1); drone(1); wind(1)
