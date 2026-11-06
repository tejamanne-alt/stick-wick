"""Builds the story-mode sound effects: real recordings where a real sound exists (breathing,
drips, a splash, metal, whispers, thunder, a creepy drone), designed and layered, placed in
the warehouse with a convolution reverb; synthesis only where synthesis is the right tool
(the ringing in the ears, the heartbeat, the wind in the vents, the far-off rotor, the piano,
the bowed cello under the drone).

    python tools/audio/make_story_sfx.py --oculus <meta-open-sound>/corpora/oculus-audio-pack/ogg \\
                                         --kit <sfx-kit>/sounds

The two libraries are the same ones build_audio.py uses (Oculus Audio Pack 01, CC BY 4.0;
SFX Kit, CC0). Needs numpy, scipy and ffmpeg. Writes 16-bit mono WAVs into
Assets/Resources/Audio/<sound>/<sound>_<take>.wav (the Sound mixer loads every clip in a
sound's folder and picks a take at random). Deterministic: the same files every run.

Rules that keep it clean: no broadband hiss (everything noisy is band-limited and darkened,
there is no "tssshh"), every one-shot starts on its transient and ends in a fade, and the
room is one consistent space (a big concrete warehouse, about 2 s of decay)."""
import argparse
import os
import subprocess
import sys
import wave
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve, resample_poly

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio")
rng = np.random.default_rng(7)
O = K = ""


# ------------------------------------------------------------------ plumbing

def load(path, start=0.0, dur=None):
    """A recording as mono float at SR, optionally a slice of it."""
    cmd = ["ffmpeg", "-v", "error", "-ss", f"{start}", "-i", path]
    if dur is not None:
        cmd += ["-t", f"{dur}"]
    cmd += ["-ac", "1", "-ar", str(SR), "-f", "f32le", "-"]
    raw = subprocess.run(cmd, capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).astype(np.float64)


def oc(stem, start=0.0, dur=None):
    return load(os.path.join(O, stem + ".ogg"), start, dur)


def kit(path, start=0.0, dur=None):
    return load(os.path.join(K, path + ".wav"), start, dur)


def t(sec):
    return np.arange(int(sec * SR)) / SR


def _sos(kind, f, order):
    f = np.atleast_1d(f) / (SR / 2)
    return butter(order, f if len(f) > 1 else f[0], btype=kind, output="sos")


def lp(x, f, order=2): return sosfilt(_sos("low", f, order), x)
def hp(x, f, order=2): return sosfilt(_sos("high", f, order), x)
def bp(x, lo, hi, order=2): return sosfilt(_sos("band", [lo, hi], order), x)


def shelf_dark(x, f=3500, amount=0.6):
    """Take the top end down by `amount` above f (keeps the body, loses the fizz)."""
    return x - amount * hp(x, f, 2)


def pitch(x, ratio):
    """Play at `ratio` speed (tape-style: pitch and length together)."""
    up, down = int(round(1000 / ratio)), 1000
    g = np.gcd(up, down)
    return resample_poly(x, up // g, down // g)


def trim(x, thresh_db=-45):
    """Start on the transient."""
    a = np.abs(x)
    i = np.argmax(a > a.max() * 10 ** (thresh_db / 20))
    return x[max(0, i - int(0.002 * SR)):]


def fades(x, fin=0.003, fout=0.04):
    x = x.copy()
    a, b = min(len(x) // 4, int(fin * SR)), min(len(x) // 3, int(fout * SR))
    if a > 0: x[:a] *= np.linspace(0, 1, a) ** 2
    if b > 0: x[-b:] *= np.linspace(1, 0, b) ** 2
    return x


def pad(x, n):
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])


def mix(*parts):
    """Sum (signal, gain, delay seconds) parts."""
    n = max(int(d * SR) + len(s) for s, g, d in parts)
    out = np.zeros(n)
    for s, g, d in parts:
        k = int(d * SR)
        out[k:k + len(s)] += s * g
    return out


def env(n, attack, release):
    a = max(1, int(attack * SR))
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a)
    e[a:] = np.exp(-np.arange(n - a) / SR / max(release, 1e-4))
    return e


_irs = {}


def room(x, rt60=2.0, wet=0.3, predelay=0.018, dark=2800, size="hall"):
    """Convolution reverb with a synthesised impulse response: a few early reflections off
    concrete, then a dense exponentially decaying tail that darkens as it decays."""
    key = (rt60, predelay, dark, size)
    if key not in _irs:
        r = np.random.default_rng(11)
        n = int(rt60 * 1.1 * SR)
        tt = np.arange(n) / SR
        tail = r.standard_normal(n) * np.exp(-6.9 * tt / rt60)
        early = lp(tail, dark * 1.6, 2)
        late = lp(tail, dark * 0.55, 2)
        k = np.clip(tt / (rt60 * 0.5), 0, 1)
        ir = early * (1 - k) + late * k
        ir[:int(predelay * SR)] = 0
        for d, g in ((0.011, 0.5), (0.019, 0.38), (0.031, 0.3), (0.047, 0.22), (0.066, 0.16)):
            ir[int((predelay + d) * SR)] += g * (1 if r.random() > 0.5 else -1)
        ir = hp(ir, 90, 2)
        _irs[key] = ir / np.sqrt(np.sum(ir ** 2))
    ir = _irs[key]
    w = fftconvolve(x, ir)[:len(x) + len(ir)]
    dry = pad(x, len(w))
    return dry * (1 - wet) + w * wet * 0.9


def save(name, x, take=1, peak=0.89):
    x = np.asarray(x, dtype=np.float64)
    x = x - np.mean(x)
    # drop the inaudible tail
    a = np.abs(x)
    end = len(x) - np.argmax(a[::-1] > a.max() * 10 ** (-60 / 20))
    x = fades(x[:min(len(x), end + int(0.02 * SR))], 0.0005, 0.06)
    x = x / (np.max(np.abs(x)) or 1.0) * peak
    d = os.path.join(ROOT, name)
    os.makedirs(d, exist_ok=True)
    path = os.path.join(d, f"{name}_{take}.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(np.round(x * 32767).astype(np.int16).tobytes())
    print("wrote", os.path.relpath(path), f"{len(x) / SR:.2f}s")


def clear(name):
    d = os.path.join(ROOT, name)
    if os.path.isdir(d):
        for f in os.listdir(d):
            if f.endswith(".wav") or f.endswith(".wav.meta"):
                os.remove(os.path.join(d, f))


# ------------------------------------------------------------------ the sounds

def thunder():
    """Outside, beyond the walls: the crack softened by distance, a long rolling body and a
    sub you feel more than hear."""
    for take, src in ((1, "cinematic-hits/cinematic-thunder-big"), (2, "cinematic-hits/cinematic-thunder-massive")):
        x = trim(kit(src))
        x = lp(shelf_dark(x, 2500, 0.7), 7000, 2)
        n = len(x) + int(2.5 * SR)
        brown = np.cumsum(rng.standard_normal(n)); brown = hp(brown, 18, 2)
        sub = lp(brown, 75, 4)
        sub = sub / (np.max(np.abs(sub)) + 1e-9) * env(n, 0.08, 1.8 + 0.5 * take)
        roll = lp(hp(rng.standard_normal(n), 40), 260, 3)
        roll *= np.clip(lp(np.abs(rng.standard_normal(n)), 2.5, 1) * 9, 0.15, 1.5) * env(n, 0.25, 2.2)
        roll /= np.max(np.abs(roll)) + 1e-9
        y = mix((x, 1.0, 0.0), (sub, 0.3, 0.02), (roll, 0.25, 0.35))
        save("thunder", room(y, 2.6, 0.22, 0.03, 2000), take)


def drip():
    """Single drops from the roof into standing water, in the big room."""
    src = kit("everyday-props/props-drip")
    for take, at in ((1, 0.0), (2, 0.66)):
        x = trim(src[int(at * SR):int((at + 0.34) * SR)])
        x = fades(x, 0.0005, 0.08)
        x = shelf_dark(x, 6000, 0.4)
        save("drip", room(x, 2.0, 0.34, 0.02, 3000), take, peak=0.75)


def splat():
    """The drop landing between his eyes, right against the skin: a heavy, close, wet tap
    with weight (a big drop slowed down) and a tiny thump through the skull."""
    src = kit("everyday-props/props-drip")
    for take, (at, ratio) in ((1, (0.0, 0.62)), (2, (0.34, 0.56))):
        x = trim(src[int(at * SR):int((at + 0.3) * SR)])
        x = fades(pitch(x, ratio), 0.0005, 0.12)
        x = shelf_dark(x, 3000, 0.5)
        n = int(0.4 * SR)
        thump = np.sin(2 * np.pi * (70 - 25 * t(0.4)) * t(0.4)) * env(n, 0.002, 0.05)
        body = lp(rng.standard_normal(n), 500, 2) * env(n, 0.001, 0.025) * 0.5
        y = mix((x, 1.0, 0.0), (thump, 0.3, 0.0), (body, 0.25, 0.0))
        save("splat", room(y, 0.5, 0.12, 0.004, 3500), take)


def splash():
    """Palms coming up out of a shallow puddle: a soft slosh and drips running off, no hiss."""
    sp = trim(kit("everyday-props/props-splash"))
    for take, ratio in ((1, 0.92), (2, 0.8)):
        x = fades(pitch(sp, ratio), 0.001, 0.15)
        x = lp(shelf_dark(x, 2500, 0.65), 6000, 2)
        d = trim(kit("everyday-props/props-drip")[int(0.66 * SR):int(1.0 * SR)])
        d = shelf_dark(fades(pitch(d, 1.15), 0.0005, 0.05), 5000, 0.5)
        y = mix((x, 1.0, 0.0), (d, 0.25, 0.32), (d, 0.15, 0.55))
        save("splash", room(y, 1.8, 0.26, 0.015, 2600), take, peak=0.7)


def breath():
    """Real breathing (a man, winded), single breaths sliced at their natural gaps and
    darkened so they sit close and warm instead of hissing."""
    src = oc("HMNBrth_MaleBreathing_OS_01")
    for take, (a, b) in ((1, (0.0, 1.02)), (2, (0.52, 1.58))):
        x = src[int(a * SR):int(b * SR)]
        x = fades(x, 0.04, 0.18)
        x = lp(shelf_dark(x, 2200, 0.7), 5200, 2)
        x = x + 0.25 * lp(x, 400, 2)                                  # a little chest
        save("breath", room(x, 1.6, 0.14, 0.01, 2400), take, peak=0.6)


def tinnitus():
    """The ringing in the ears: a pure high tone with a slow beat and its quieter twin,
    fading in fast and away slowly. Nothing noisy."""
    x = t(3.8)
    f = 6800 + 18 * np.sin(2 * np.pi * 0.5 * x)
    tone = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.25 * np.sin(2 * np.pi * np.cumsum(f * 1.0035) / SR)
    tone += 0.12 * np.sin(2 * np.pi * np.cumsum(f * 0.5) / SR)
    e = np.minimum(1, x / 0.25) ** 2 * np.exp(-x / 1.5)
    save("tinnitus", tone * e, 1, peak=0.5)


def heartbeat():
    """Lub-dub, heard from inside: a low damped knock with a falling pitch, a softer second
    sound, and the chest's own resonance."""
    x = t(1.0)

    def beat(at, amp, f0):
        y = np.zeros_like(x)
        m = x >= at
        k = x[m] - at
        f = f0 * (1 + 0.6 * np.exp(-k / 0.02))
        ph = 2 * np.pi * np.cumsum(f) / SR
        body = np.sin(ph) * np.exp(-k / 0.085) + 0.35 * np.sin(2 * ph) * np.exp(-k / 0.04)
        y[m] = amp * body * np.minimum(1, k / 0.006)
        return y
    y = beat(0.0, 1.0, 48) + beat(0.24, 0.65, 42)
    knock = lp(rng.standard_normal(len(x)), 180, 2) * (env(len(x), 0.002, 0.03) + 0.6 * np.roll(env(len(x), 0.002, 0.025), int(0.24 * SR)))
    y = lp(y + knock * 0.35, 220, 2)
    save("heartbeat", room(y, 0.6, 0.1, 0.005, 800), 1)


def static():
    """The wound touched, the memory breaking through: electrical, not hiss. A dying signal,
    a glitch, a mains buzz with its harmonics stuttering, all held below 6 kHz."""
    n = int(0.7 * SR)
    x = t(0.7)
    buzz = sum(np.sin(2 * np.pi * 50 * h * x) / h ** 0.8 for h in (1, 2, 3, 5, 7, 9, 11))
    gate = (np.sin(2 * np.pi * 23 * x) + 0.6 * np.sin(2 * np.pi * 37 * x + 1) > 0.2).astype(float)
    gate = lp(gate, 300, 2)
    buzz = bp(buzz * gate, 70, 3000, 2) * env(n, 0.002, 0.25)
    for take in (1, 2):
        g = trim(kit("digital/digital-signal-drop" if take == 1 else "digital/digital-glitch-clear"))
        g = lp(shelf_dark(g, 3000, 0.5), 6000, 2)
        y = mix((g, 1.0, 0.0), (buzz, 0.32, 0.0))
        if take == 2:
            g2 = lp(trim(kit("digital/digital-glitch-subtle")), 6000, 2)
            y = mix((y, 1.0, 0.0), (g2, 0.7, 0.21))
        save("static", room(y, 0.9, 0.12, 0.008, 3000), take, peak=0.75)


def whispers():
    """Voices from a life he does not remember: real whispers, one take forwards in a
    big space, one reversed so it swells and cuts."""
    a = oc("VOXWhsp_WhispersCreepyReverb_OS_01", 0.6, 2.6)
    a = fades(lp(shelf_dark(a, 3000, 0.45), 7000, 2), 0.25, 0.6)
    save("whispers", room(a, 2.4, 0.3, 0.03, 2400), 1, peak=0.6)
    b = oc("VOXWhsp_WhispersCreepy_OS_01", 1.0, 2.2)
    b = lp(shelf_dark(b, 3000, 0.45), 7000, 2)
    b = room(b, 2.4, 0.45, 0.03, 2400)[::-1]
    b = fades(b * np.linspace(0.2, 1, len(b)) ** 1.5, 0.3, 0.01)
    save("whispers", b, 2, peak=0.6)


def drone():
    """The walk to the shutter: a recorded creepy drone underneath a bowed cello C with its
    fifth, swelling in."""
    x = t(8.0)
    f0 = 65.41 * (1 + 0.003 * np.sin(2 * np.pi * 5.0 * x))
    saw = sum(np.sin(2 * np.pi * h * np.cumsum(f0) / SR) / h * (0.9 ** h) for h in range(1, 16))
    fifth = sum(np.sin(2 * np.pi * h * np.cumsum(f0 * 1.5) / SR) / h * (0.88 ** h) for h in range(1, 10))
    bow = 1 + 0.06 * lp(rng.standard_normal(len(x)), 6, 1) * 40
    cello = lp(saw + 0.4 * fifth, 1100, 2) * bow
    cello /= np.max(np.abs(cello))
    bed = oc("DSGNDron_DroneCreepy_OS_01", 4.0, 8.0)
    bed = lp(bed, 3000, 2)
    bed /= np.max(np.abs(bed)) + 1e-9
    y = cello * 0.8 + pad(bed, len(cello))[:len(cello)] * 0.45
    y *= np.minimum(1, x / 3.5) ** 2 * np.minimum(1, (8.0 - x) / 1.5)
    save("drone", room(y, 3.0, 0.3, 0.03, 2000), 1, peak=0.8)


def latch():
    """The shutter's latch tried from outside: a heavy metal clunk and the sheet ringing in
    the big room."""
    box = trim(oc("METLHndl_ElectricBoxOpen_OS_01", 0.3))
    for take, hit in ((1, "METLImpt_MetalHitSurface_OS_01"), (2, "METLImpt_MetalHitSurface_OS_02")):
        h = trim(oc(hit))
        h = lp(shelf_dark(h, 3000, 0.4), 8000, 2)
        b = lp(box, 7000, 2)
        y = mix((h, 1.0, 0.0), (b, 0.6, 0.03 if take == 1 else 0.05))
        save("latch", room(y, 2.4, 0.32, 0.02, 2800), take)


def wind():
    """Wind through the roof vents: a moaning resonance that glides with the gusts (a
    smoothly time-varying filter, so no clicks), over a dark rush. No high end at all."""
    dur = 7.0
    n = int(dur * SR)
    x = t(dur)
    src = lp(rng.standard_normal(n), 2500, 2)
    gust = 0.55 + 0.45 * np.sin(2 * np.pi * 0.09 * x + 0.5) * np.sin(2 * np.pi * 0.23 * x + 2)
    fc = 380 + 260 * gust
    q = 9.0
    y = np.zeros(n)
    low = band = 0.0
    for i in range(n):                                   # Chamberlin state-variable filter
        f = 2 * np.sin(np.pi * fc[i] / SR)
        high = src[i] - low - band / q
        band += f * high
        low += f * band
        y[i] = band
    rush = lp(hp(rng.standard_normal(n), 120, 2), 700, 2) * (0.4 + 0.6 * gust)
    y = y / np.max(np.abs(y)) * gust + rush / np.max(np.abs(rush)) * 0.35
    y *= np.minimum(1, x / 1.5) * np.minimum(1, (dur - x) / 1.5)
    save("wind", room(y, 2.0, 0.25, 0.02, 1500), 1, peak=0.6)


def rotor():
    """A helicopter far off over the yard: blade slap and turbine, small with distance."""
    dur = 6.5
    x = t(dur)
    rate = 10.5 * (1 + 0.02 * np.sin(2 * np.pi * 0.2 * x))
    ph = np.cumsum(rate) / SR
    pulse = np.exp(-((ph % 1.0) / 0.06)) * (0.85 + 0.15 * np.sin(2 * np.pi * 0.37 * x))
    slap = lp(pulse * (0.6 + 0.4 * lp(rng.standard_normal(len(x)), 300, 2) * 8), 320, 3)
    whine = 0.05 * np.sin(2 * np.pi * np.cumsum(780 + 12 * np.sin(2 * np.pi * 0.3 * x)) / SR)
    y = slap / np.max(np.abs(slap)) + whine
    y *= np.minimum(1, x / 1.8) * np.minimum(1, (dur - x) / 1.8)
    save("rotor", room(y, 2.5, 0.35, 0.05, 1200), 1, peak=0.6)


def piano_note(f, dur, vel=1.0):
    x = t(dur)
    y = np.zeros_like(x)
    for h, a in ((1, 1.0), (2, 0.45), (3, 0.22), (4, 0.12), (5, 0.07), (6, 0.035), (7, 0.02)):
        fh = f * h * (1 + 0.0004 * h * h)
        det = 1 + 0.0007 * (h % 2)
        y += a * (np.sin(2 * np.pi * fh * x) + 0.5 * np.sin(2 * np.pi * fh * det * x)) * np.exp(-x * (0.9 + 0.5 * h))
    hammer = lp(rng.standard_normal(len(x)), 2500, 2) * np.exp(-x / 0.006) * 0.08
    return (y * np.minimum(1, x / 0.003) + hammer) * vel


def piano():
    """A sparse, melancholic motif in A minor, in the empty building (who am I?)."""
    for take, notes in ((1, [(220.0, 0.0, 0.9), (261.63, 0.9, 0.7), (329.63, 1.8, 0.75), (246.94, 2.9, 0.85)]),
                        (2, [(329.63, 0.0, 0.8), (293.66, 0.8, 0.7), (261.63, 1.6, 0.7), (220.0, 2.6, 0.9), (110.0, 2.6, 0.5)])):
        total = 6.5
        y = np.zeros(int(total * SR))
        for f, at, v in notes:
            nn = piano_note(f, total - at, v)
            k = int(at * SR)
            y[k:k + len(nn)] += nn[:len(y) - k]
        save("piano", room(lp(y, 5000, 2), 3.2, 0.42, 0.03, 2200), take, peak=0.7)


# ------------------------------------------------------------------ the escape (chapter 1)

def chain():
    """His foot catching a heavy chain lying across the concrete: links dragged and slapping
    down, keys-on-a-ring jingle for the small links, a heavy metal body for the big ones."""
    keys = [trim(oc("OBJKey_KeyDrop_OS_01")), trim(oc("OBJKey_KeyDrop_OS_02"))]
    drops = [trim(oc(f"METLImpt_MetalObjDrop_OS_0{i}")) for i in (1, 2, 3, 4)]
    for take in (1, 2):
        k1 = shelf_dark(pitch(keys[take - 1], 0.62), 3500, 0.5)
        k2 = shelf_dark(pitch(keys[2 - take], 0.55), 3500, 0.5)
        d1 = lp(pitch(drops[take - 1], 0.8), 6000, 2)
        d2 = lp(pitch(drops[take + 1], 0.72), 5000, 2)
        y = mix((d1, 1.0, 0.0), (k1, 0.8, 0.02), (k2, 0.55, 0.11), (d2, 0.6, 0.16), (k1[::1], 0.3, 0.27))
        save("chain", room(y, 2.0, 0.28, 0.015, 2600), take)


def body_splash():
    """A man going down full length on wet concrete, into shallow water: the slap of the body,
    a heavy splash, the water settling."""
    fall = load(os.path.join(ROOT, "body_fall", "body_fall_01.ogg"))
    sp = trim(kit("everyday-props/props-splash"))
    for take, r in ((1, 0.62), (2, 0.55)):
        s1 = lp(shelf_dark(fades(pitch(sp, r), 0.001, 0.25), 2200, 0.6), 5000, 2)
        s2 = lp(shelf_dark(fades(pitch(sp, r * 1.25), 0.001, 0.2), 2500, 0.6), 5500, 2)
        n = int(0.5 * SR)
        thump = np.sin(2 * np.pi * (62 - 20 * t(0.5)) * t(0.5)) * env(n, 0.002, 0.08)
        lap = oc("WATRLap_WaterLapping_LOOP_01", 12.0 + take * 3, 1.2)
        lap = fades(lp(shelf_dark(lap, 2000, 0.7), 3000, 2), 0.2, 0.5)
        y = mix((trim(fall), 0.9, 0.0), (s1, 1.0, 0.012), (s2, 0.5, 0.09), (thump, 0.45, 0.0), (lap, 0.35, 0.25))
        save("body_splash", room(y, 1.9, 0.24, 0.015, 2400), take)


def crawl():
    """Dragging himself through the wet on his forearms: cloth on concrete, a little water."""
    for take in (1, 2):
        c = oc(f"CLOTHMvmt_ClothingMovement_OS_0{take + 1}")
        c = fades(lp(shelf_dark(trim(c), 2500, 0.6), 5000, 2)[:int(0.9 * SR)], 0.02, 0.2)
        w = oc("WATRLap_WaterLapping_LOOP_01", 20.0 + 4 * take, 0.8)
        w = fades(lp(shelf_dark(w, 2000, 0.7), 2600, 2), 0.1, 0.3)
        save("crawl", room(mix((c, 1.0, 0.0), (w, 0.45, 0.05)), 1.6, 0.18, 0.012, 2400), take, peak=0.7)


def glint():
    """The lightning catching the blade on the floor: a cold, quiet ring."""
    for take, src in ((1, "shimmers/shimmer-short-soft"), (2, "shimmers/shimmer-medium-soft")):
        x = trim(kit(src))
        x = lp(shelf_dark(x, 5000, 0.5), 9000, 2)
        ring = np.sin(2 * np.pi * 2350 * t(1.4)) * np.exp(-t(1.4) / 0.35) * 0.25
        save("glint", room(mix((x, 1.0, 0.0), (ring, 1.0, 0.0)), 2.2, 0.35, 0.02, 3000), take, peak=0.55)


def knife_grab():
    """The handle dragged across the concrete into his fist: a real knife scrape, close."""
    sc = trim(oc("WEAPKnif_KnifeScrape_OS_01"))
    for take, (r, cut) in ((1, (1.0, 0.7)), (2, (0.9, 0.85))):
        x = fades(pitch(sc, r)[:int(cut * SR)], 0.003, 0.15)
        x = lp(shelf_dark(x, 4500, 0.4), 9000, 2)
        grip = lp(trim(oc("CLOTHMvmt_MovementWithGear_OS_01"))[:int(0.4 * SR)], 3000, 2)
        save("knife_grab", room(mix((x, 1.0, 0.0), (fades(grip, 0.01, 0.1), 0.5, cut * 0.85)), 1.2, 0.15, 0.01, 3000), take)


def flash_cut():
    """The memory cuts: each one sucks in (a reversed whoosh) and lands (a hit), some with
    metal, some with a drum. Short, hard, different every time."""
    rev = [trim(kit("reverse-whooshes/reverse-whoosh-short-strong")), trim(kit("reverse-whooshes/reverse-whoosh-short-soft"))]
    hits = ["cinematic-hits/cinematic-metal-big", "cinematic-hits/cinematic-drum-big", "impacts/impact-deep-hard", "cinematic-hits/cinematic-slam-big"]
    for take, h in enumerate(hits, 1):
        r = rev[take % 2]
        r = r[-int(min(len(r), 0.35 * SR)):]
        hit = lp(shelf_dark(trim(kit(h)), 3500, 0.5), 8000, 2)
        y = mix((r, 0.7, 0.0), (hit, 1.0, len(r) / SR - 0.01))
        save("flash_cut", room(y, 1.4, 0.2, 0.01, 2500), take)


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--oculus", default="/home/user/facebookincubator/meta-open-sound/corpora/oculus-audio-pack/ogg")
    ap.add_argument("--kit", default="/home/user/wyzowl/sfx-kit/sounds")
    a = ap.parse_args()
    O, K = a.oculus, a.kit
    if not os.path.isdir(O) or not os.path.isdir(K):
        sys.exit("need the two sound libraries: --oculus <...>/oculus-audio-pack/ogg --kit <...>/sounds")
    for name in ("thunder", "drip", "splat", "splash", "breath", "tinnitus", "heartbeat", "static",
                 "whispers", "drone", "latch", "wind", "rotor", "piano",
                 "chain", "body_splash", "crawl", "glint", "knife_grab", "flash_cut"):
        clear(name)
        globals()[name]()
