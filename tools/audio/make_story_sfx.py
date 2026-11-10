"""Builds the story-mode sound effects: real recordings where a real sound exists (breathing,
drips, a splash, metal, whispers, thunder, a creepy drone), designed and layered, placed in
the warehouse with a convolution reverb; synthesis only where synthesis is the right tool
(the ringing in the ears, the heartbeat, the wind in the vents, the far-off rotor, the piano,
the bowed cello under the drone).

    python tools/audio/make_story_sfx.py --oculus <meta-open-sound>/corpora/oculus-audio-pack/ogg \\
                                         --kit <sfx-kit>/sounds [sound ...]

With sound names (e.g. `explosion walkie_click`), builds only those; with none, all of them.

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


def save(name, x, take=1, peak=0.89, loop=False):
    x = np.asarray(x, dtype=np.float64)
    x = x - np.mean(x)
    if not loop:                                   # a loop keeps every sample: no trim, no fades
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


def gunshot_indoor():
    """One pistol shot in a big concrete warehouse, close: the supersonic crack (a synthesised
    N-wave, since the libraries have no gun), a hard real impact for the body, a low thump in
    the chest, then the building answering: slap-back off the far walls and a long dark tail."""
    for take, (body, rr) in ((1, ("impacts/impact-medium-hard", 1.0)), (2, ("impacts/impact-heavy-hard", 0.94))):
        n = int(0.012 * SR)
        nw = np.zeros(n)
        a = int(0.0004 * SR)
        nw[:a] = 1.0
        nw[a:2 * a] = -0.9
        burst = rng.standard_normal(n) * np.exp(-np.arange(n) / (0.0018 * SR))
        crack = hp(nw * 0.9 + burst * 0.7, 700, 2)
        crack = lp(crack, 12000, 2)
        b = lp(shelf_dark(pitch(trim(kit(body)), rr), 4500, 0.4), 7000, 2)
        snap = hp(trim(oc("METLImpt_MetalHitSurface_OS_04")), 2500, 2)[:int(0.08 * SR)]
        thump = lp(trim(kit("booms-and-bass/boom-hit-strong")), 170, 2)
        rumble = lp(trim(kit("cinematic-hits/cinematic-thunder-big")), 320, 2)
        dry = mix((crack, 1.0, 0.0), (b, 0.75, 0.001), (fades(snap, 0.0005, 0.03), 0.3, 0.0), (thump, 0.9, 0.0), (rumble, 0.22, 0.04))
        dry = np.tanh(dry * 1.6) / np.tanh(1.6)                              # driven hard: the air itself clips
        echoes = mix((dry, 1.0, 0.0), (lp(dry, 2200, 2), 0.32, 0.11), (lp(dry, 1400, 2), 0.2, 0.26), (lp(dry, 900, 2), 0.12, 0.43), (lp(dry, 700, 2), 0.07, 0.62))
        save("gunshot_indoor", room(echoes, 3.4, 0.5, 0.02, 2200), take, peak=0.95)


def gun_draw():
    """The pistol coming out from under his jacket: cloth dragged, the grip scraping free, a
    small knock of metal against a button."""
    for take, c in ((1, "CLOTHMvmt_ClothingMovement_OS_02"), (2, "CLOTHMvmt_MovementWithGear_OS_03")):
        cl = fades(lp(shelf_dark(trim(oc(c))[:int(0.7 * SR)], 3000, 0.5), 7000, 2), 0.01, 0.2)
        sl = fades(lp(trim(kit("foley/foley-plastic-slide")), 6000, 2)[:int(0.5 * SR)], 0.01, 0.12)
        tick = lp(pitch(trim(oc("METLImpt_MetalObjDrop_OS_02")), 1.4), 7000, 2)[:int(0.12 * SR)]
        y = mix((cl, 1.0, 0.0), (sl, 0.6, 0.18), (fades(tick, 0.0005, 0.05), 0.25, 0.55))
        save("gun_draw", room(y, 1.6, 0.14, 0.01, 2800), take, peak=0.7)


def gun_cock():
    """The hammer drawn back, very close: a first click as it starts, the heavy clack as it
    sets."""
    first = [trim(kit("mechanical/mechanical-latch-light")), trim(kit("mechanical/mechanical-switch-light"))]
    second = [trim(kit("mechanical/mechanical-ratchet-light")), trim(kit("mechanical/mechanical-latch-heavy"))]
    for take in (1, 2):
        c1 = fades(lp(pitch(first[take - 1], 1.25), 9000, 2)[:int(0.1 * SR)], 0.0005, 0.04)
        c2 = fades(lp(pitch(second[take - 1], 1.1), 8000, 2)[:int(0.16 * SR)], 0.0005, 0.06)
        y = mix((c1, 0.6, 0.0), (c2, 1.0, 0.13 if take == 1 else 0.17))
        save("gun_cock", room(y, 1.4, 0.1, 0.008, 3500), take, peak=0.8)


def shell_casing():
    """The spent brass bouncing on the concrete afterwards (real casings), heard through the
    ringing: bright, small, a long way down."""
    for take in (1, 2, 3):
        x = trim(oc(f"BLLTShel_ShellCasingConcrete_OS_0{take}"))
        x = shelf_dark(x, 6000, 0.3)
        save("shell_casing", room(x, 2.0, 0.24, 0.015, 3500), take, peak=0.6)


def gun_drop_water():
    """The pistol slipping out of a dead hand into the puddle: metal on concrete under water."""
    sp = trim(kit("everyday-props/props-splash"))
    for take, d in ((1, "METLImpt_MetalObjDrop_OS_01"), (2, "METLImpt_MetalObjDrop_OS_03")):
        m = lp(pitch(trim(oc(d)), 0.9), 5000, 2)
        w = lp(shelf_dark(fades(pitch(sp, 1.15), 0.001, 0.15), 2500, 0.6), 5000, 2)
        y = mix((m, 0.8, 0.0), (w, 0.7, 0.004))
        save("gun_drop_water", room(y, 1.8, 0.22, 0.015, 2600), take, peak=0.7)


def struggle():
    """Two men locked over a gun: clothes wrenched, the gun's metal knocking, breath through
    teeth. Low and close, a bed under the grunts."""
    src = oc("HMNBrth_MaleBreathing_OS_01")
    for take in (1, 2):
        parts = []
        for i in range(6):
            c = trim(oc(f"CLOTHMvmt_MovementWithGear_OS_0{(i + take) % 4 + 1}"))[:int(0.45 * SR)]
            parts.append((fades(lp(shelf_dark(c, 2800, 0.5), 6500, 2), 0.01, 0.12), 0.8, 0.32 * i + rng.uniform(0, 0.08)))
        for i in range(3):
            m = hp(trim(oc(f"METLImpt_MetalHitSurface_OS_0{i + 1}")), 1800, 2)[:int(0.06 * SR)]
            parts.append((fades(m, 0.0005, 0.03), 0.18, 0.4 + 0.55 * i + rng.uniform(0, 0.1)))
        b = src[int((0.1 + 0.4 * take) * SR):int((0.7 + 0.4 * take) * SR)]
        b = lp(shelf_dark(fades(pitch(b, 1.25), 0.02, 0.1), 2200, 0.7), 4500, 2)
        parts += [(b, 0.6, 0.2), (b, 0.5, 1.1)]
        save("struggle", room(mix(*parts), 1.4, 0.12, 0.01, 2600), take, peak=0.75)


def death_breath():
    """His last breath out: long, slow, wet in the throat, falling away."""
    src = oc("HMNBrth_MaleBreathing_OS_01")
    for take, (a, b) in ((1, (0.0, 1.02)), (2, (0.52, 1.58))):
        x = fades(src[int(a * SR):int(b * SR)], 0.05, 0.4)
        x = lp(shelf_dark(pitch(x, 0.72), 1800, 0.75), 3200, 2)
        x = x * np.linspace(1.0, 0.25, len(x))
        save("death_breath", room(x, 1.6, 0.16, 0.01, 2200), take, peak=0.55)


MEMORY_SECONDS = [2.8, 2.8, 2.6, 2.6, 2.6, 2.4, 2.6, 5.6]     # render_memories.py and StoryBank use the same


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def memory_elena():
    """Chapter 2's memory of Elena (render_memories.py): one take timed to the eight stills. A
    gentle waltz on the piano in C (a chord for each memory, a melody over it, the last two bars
    resolving home), a soft string pad under it, and under that each memory's own sound, real
    recordings, faded in and out with its picture: rain and a cafe's door bell; a river and a
    city at night, a ring box opening, her gasp; applause, confetti, a church bell; wind chimes
    while they dance; birds on moving day, keys, a front door; the kitchen (a spoon, a tap); the
    beach (waves on stones, gulls); and on her face, only her breath and the piano."""
    D = MEMORY_SECONDS
    starts = np.concatenate([[0.0], np.cumsum(D)[:-1]])
    total = sum(D) + 1.5
    n = int(total * SR)
    music = np.zeros(n)
    pad = np.zeros(n)
    foley = np.zeros(n)

    def put(buf, x, at, gain=1.0):
        k = int(at * SR)
        if k >= len(buf):
            return
        m = min(len(x), len(buf) - k)
        buf[k:k + m] += x[:m] * gain

    # one bar of three beats per memory (the last memory has two)
    chords = [(48, (64, 67)), (45, (60, 64)), (41, (57, 60)), (43, (59, 62)), (48, (64, 67)), (45, (60, 64)), (41, (57, 60))]
    melody = [((76, 0), (79, 2)), ((81, 0), (79, 1), (76, 2)), ((77, 0), (76, 1), (72, 2)), ((74, 0), (71, 2)),
              ((76, 0), (79, 1), (84, 2)), ((83, 0), (81, 1), (76, 2)), ((81, 0), (79, 1), (77, 2))]
    for i, (root, tones) in enumerate(chords):
        beat = D[i] / 3
        t0 = starts[i]
        put(music, piano_note(midi(root), beat * 3 + 0.6, 0.55), t0)
        for b in (1, 2):
            for tn in tones:
                put(music, piano_note(midi(tn), beat * 1.2, 0.28), t0 + b * beat)
        for note, b in melody[i]:
            put(music, piano_note(midi(note), beat * 2.2, 0.62), t0 + b * beat)
        # the strings: the chord held, swelling in and out
        dur = D[i] + 0.6
        tt = t(dur)
        env = np.minimum(1, tt / 0.6) * np.minimum(1, (dur - tt) / 0.5)
        chord = sum(np.sign(np.sin(2 * np.pi * midi(m) * tt)) * 0.3 + np.sin(2 * np.pi * midi(m) * 1.003 * tt) for m in (root + 12,) + tones)
        put(pad, lp(chord, 1100, 2) * env * 0.11, t0)
    # the last memory, her face: G, then home to C, the melody climbing and coming to rest
    t0, beat = starts[7], D[7] / 6
    put(music, piano_note(midi(43), beat * 3 + 0.4, 0.5), t0)
    for b in (1, 2):
        for tn in (59, 62, 65):
            put(music, piano_note(midi(tn), beat * 1.2, 0.26), t0 + b * beat)
    for note, b in ((74, 0), (76, 1), (77, 2)):
        put(music, piano_note(midi(note), beat * 2.0, 0.6), t0 + b * beat)
    put(music, piano_note(midi(36), beat * 3 + 2.0, 0.55), t0 + 3 * beat)
    for tn in (60, 64, 67, 72):
        put(music, piano_note(midi(tn), beat * 3 + 2.0, 0.3 if tn < 72 else 0.62), t0 + 3 * beat)
    tt = t(D[7] + 1.4)
    env = np.minimum(1, tt / 0.8) * np.minimum(1, (D[7] + 1.4 - tt) / 1.2)
    put(pad, lp(sum(np.sin(2 * np.pi * midi(m) * tt) + 0.3 * np.sign(np.sin(2 * np.pi * midi(m) * 1.002 * tt)) for m in (48, 64, 67)), 900, 2) * env * 0.12, t0)

    # each memory's own sound, faded with its picture
    def bed(x, i, gain, lo=60, hi=9000, delay=0.0, length=None):
        L = (length or D[i] + 0.5)
        x = x[:int(L * SR)]
        x = bp(x, lo, hi, 2) if lo > 0 else lp(x, hi, 2)
        x = fades(x, 0.25, 0.45)
        put(foley, x, starts[i] + delay, gain)

    rain = load(os.path.join(ROOT, "amb_rain", "amb_rain_01.ogg"), 3.0, 4.0)
    bed(shelf_dark(rain, 3000, 0.6), 0, 0.5, 80, 7000)
    bed(trim(kit("everyday-props/props-shop-bell")), 0, 0.35, 200, 9000, 0.25, 1.8)
    bed(oc("WATRFlow_River_LOOP_01", 5.0, 4.0), 1, 0.35, 80, 6000)
    bed(oc("AMBUrbn_CityAmerican_LOOP_01", 10.0, 4.0), 1, 0.25, 80, 4000)
    bed(trim(kit("everyday-props/props-box-open")), 1, 0.4, 150, 8000, 0.9, 1.0)
    gasp = oc("HMNBrth_FemaleBreathing_OS_01")[int(0.0 * SR):int(0.7 * SR)]
    bed(gasp, 1, 0.45, 150, 6000, 1.35, 0.8)
    bed(trim(kit("celebration/celebration-applause-small")), 2, 0.4, 100, 8000)
    bed(trim(kit("celebration/celebration-confetti")), 2, 0.25, 200, 9000, 0.3)
    tb = t(2.6)
    bell = sum(a * np.sin(2 * np.pi * 523.25 * r * tb) * np.exp(-tb * d) for r, a, d in ((1, 1.0, 1.2), (2.0, 0.5, 1.8), (2.4, 0.4, 2.2), (3.0, 0.25, 2.6), (4.2, 0.15, 3.5)))
    bed(bell * 0.4, 2, 0.35, 100, 8000, 0.6, 2.6)
    bed(oc("MUSCChim_WindChimes_LOOP_01", 2.0, 3.2), 3, 0.25, 300, 9000)
    bed(trim(oc("CLOTHMvmt_ClothingMovement_OS_03"))[:int(0.8 * SR)], 3, 0.3, 150, 6000, 0.5, 0.9)
    bed(oc("AMBForst_ForestBirds_OS_01", 0.0, 3.2), 4, 0.35, 300, 9000)
    bed(trim(oc("OBJKey_KeyDrop_OS_01")), 4, 0.45, 300, 9000, 0.5, 1.2)
    bed(trim(oc("DOORWood_FrontDoorOpen_OS_01")), 4, 0.35, 100, 7000, 1.5, 1.4)
    bed(oc("AMBHome_RoomTone_LOOP_01", 2.0, 3.0), 5, 0.25, 60, 4000)
    bed(oc("WATRPlmb_FaucetWaterRun_LOOP_01", 1.0, 1.4), 5, 0.18, 200, 6000, 0.2, 1.4)
    bed(trim(oc("FOODTware_SpoonDrop_OS_01")), 5, 0.35, 200, 9000, 1.0, 1.2)
    bed(oc("AMBSea_BeachWavesStonesBirds_LOOP_01", 6.0, 3.4), 6, 0.5, 60, 8000)
    breath = oc("HMNBrth_FemaleBreathing_OS_01")[int(0.9 * SR):int(2.4 * SR)]
    bed(shelf_dark(breath, 2500, 0.6), 7, 0.4, 100, 5000, 0.8, 1.6)

    L = n + int(3.0 * SR)
    mix = pad_to(room(lp(music, 5200, 2), 2.6, 0.32, 0.03, 2600), L) + pad_to(room(pad, 2.2, 0.25, 0.02, 1800), L)
    mix = mix + pad_to(room(foley, 1.4, 0.14, 0.01, 3000), L) * 0.9
    save("memory_elena", fades(mix, 0.05, 1.2), 1, peak=0.8)


def pad_to(x, n):
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n]


def phone_buzz():
    """The dead man's phone vibrating on the wet concrete: a real phone on a hard surface,
    darkened and placed in the room, with a motor layer under it to carry the low end."""
    real = trim(kit("phone/phone-vibrate-table"))
    for take, (f, bursts) in ((1, (162.0, 2)), (2, (171.0, 3))):
        seg = []
        for b in range(bursts):
            n = int(0.42 * SR)
            tt = np.arange(n) / SR
            motor = np.sign(np.sin(2 * np.pi * f * tt)) * 0.5 + np.sin(2 * np.pi * f * 2 * tt) * 0.3
            motor = bp(motor, 120, 1800, 2)
            rattle = bp(rng.standard_normal(n), 900, 3200, 2) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * f / 2 * tt)))
            env = np.minimum(1.0, tt / 0.02) * np.minimum(1.0, (0.42 - tt) / 0.03)
            seg.append((motor * 0.8 + rattle * 0.35) * env)
            seg.append(np.zeros(int(0.22 * SR)))
        y = lp(np.concatenate(seg), 4500, 2)
        r = lp(shelf_dark(pitch(real, 1.0 if take == 1 else 0.95), 4000, 0.4), 7000, 2)
        y = mix((r, 1.0, 0.0), (y, 0.35, 0.0))
        save("phone_buzz", room(y, 1.6, 0.18, 0.01, 3000), take, peak=0.7)


def phone_click():
    """The call picking up, and ending: a soft plastic click, then the line's tiny double tone."""
    for take, tone in ((1, 0.0), (2, 1.0)):
        n = int(0.5 * SR)
        x = np.zeros(n)
        k = bp(rng.standard_normal(int(0.012 * SR)), 1500, 6000, 2) * np.exp(-np.arange(int(0.012 * SR)) / (0.002 * SR))
        x[:len(k)] += k
        if tone:
            for i, st in enumerate((0.08, 0.24)):
                tt = np.arange(int(0.11 * SR)) / SR
                beep = np.sin(2 * np.pi * 425 * tt) * np.minimum(1.0, tt / 0.005) * np.minimum(1.0, (0.11 - tt) / 0.01) * 0.25
                a = int(st * SR)
                x[a:a + len(beep)] += bp(beep, 300, 3400, 2)
        save("phone_click", x, take, peak=0.5)


def train_pass():
    """The elevated train going over: the long roar of a pass-by, heavy under the deck."""
    x = oc("TRNElec_TrainPassby_OS_01", 6.0, 14.0)
    x = mix((x, 1.0, 0.0), (lp(x, 180, 2), 0.5, 0.0))           # the deck booms with it
    x *= np.minimum(1, t(len(x) / SR) / 1.5) * np.minimum(1, (len(x) / SR - t(len(x) / SR)) / 2.0)
    save("train_pass", room(x, 1.8, 0.3, 0.02, 3000), 1, peak=0.85)


def car_rev():
    """The black car: the engine caught, revved hard, then the run at him."""
    a = oc("VEHCar_CarStartRunStop_OS_01", 0.0, 4.5)
    b = oc("VEHCar_CarEngineHigh_LOOP_01")
    save("car_rev", a, 1, peak=0.8)
    save("car_rev", mix((b, 1.0, 0.0), (b, 0.8, len(b) / SR - 0.05)), 2, peak=0.8)


def car_door():
    """Car doors opening, slamming, the boot."""
    save("car_door", oc("VEHDoor_CarDoorOpen_OS_01"), 1, peak=0.7)
    save("car_door", oc("VEHDoor_CarDoorClose_OS_01"), 2, peak=0.8)
    save("car_door", oc("VEHDoor_CarTrunkClose_OS_01"), 3, peak=0.8)


MAYA_SECONDS = [2.8, 2.6, 2.6, 2.6, 2.6, 3.4]    # render_maya.py and StoryHome.MayaDur use the same


def box_note(f, dur=1.6, vel=1.0):
    """One tine of a music box: a bright pluck, metallic overtones, a long soft ring."""
    x = t(dur)
    e = np.exp(-x / 0.55) * np.minimum(1.0, x / 0.002)
    tone = (np.sin(2 * np.pi * f * x) + 0.35 * np.sin(2 * np.pi * f * 2.0 * x) * np.exp(-x / 0.2)
            + 0.18 * np.sin(2 * np.pi * f * 3.98 * x) * np.exp(-x / 0.08) + 0.08 * np.sin(2 * np.pi * f * 6.1 * x) * np.exp(-x / 0.04))
    return tone * e * vel * (1 + 0.04 * np.sin(2 * np.pi * 5.5 * x))


def memory_maya():
    """Maya's theme: a music-box lullaby (original), slow, in E major, one phrase per memory, the
    last one resolving and left ringing; a soft string pad under it."""
    L = int(sum(MAYA_SECONDS) * SR)
    out = np.zeros(L + int(2.0 * SR))
    hz = lambda n: 440.0 * 2 ** ((n - 69) / 12)
    # (beat, midi) per phrase; a beat is 0.43 s; each phrase sits in its memory's window
    phrases = [
        [(0, 76), (1, 80), (2, 83), (3, 81), (4, 80)],
        [(0, 78), (1, 81), (2, 85), (3.5, 83)],
        [(0, 76), (1, 80), (2, 83), (2.5, 88), (3.5, 85)],
        [(0, 81), (1, 80), (2, 78), (3, 76)],
        [(0, 73), (1, 76), (2, 80), (3, 81), (4, 83)],
        [(0, 85), (1, 83), (2, 80), (3, 78), (4.5, 76), (4.5, 68), (4.5, 71)],
    ]
    start = 0.0
    for i, ph in enumerate(phrases):
        for b, n in ph:
            k = int((start + 0.15 + b * 0.43) * SR)
            note = box_note(hz(n), 2.2 if i == 5 else 1.5, 0.8 if n > 70 else 0.5)
            out[k:k + len(note)] += note[:max(0, len(out) - k)]
        start += MAYA_SECONDS[i]
    # a soft pad: the chords under it
    x = t(len(out) / SR)
    chords = [(64, 68, 71), (66, 69, 73), (64, 68, 71), (69, 73, 76), (61, 64, 68), (64, 68, 71)]
    pad = np.zeros(len(out))
    start = 0.0
    for i, ch in enumerate(chords):
        a, b = int(start * SR), int((start + MAYA_SECONDS[i] + 0.6) * SR)
        b = min(b, len(out))
        seg = x[a:b] - x[a]
        env_ = np.minimum(1, seg / 0.6) * np.minimum(1, (seg[-1] - seg + 1e-3) / 0.6)
        for n in ch:
            f = hz(n - 12)
            pad[a:b] += (np.sin(2 * np.pi * f * seg) + 0.3 * np.sin(2 * np.pi * f * 2 * seg)) * env_ * 0.06
        start += MAYA_SECONDS[i]
    pad = lp(pad, 1800, 2)
    y = out + pad
    y = room(y, 2.4, 0.28, 0.02, 5000)
    save("memory_maya", pad_to(y, L + int(1.5 * SR)), 1, peak=0.8)


def music_note():
    """A single music-box note: the photo, her room."""
    for take, n in ((1, 83), (2, 88), (3, 80)):
        f = 440.0 * 2 ** ((n - 69) / 12)
        save("music_note", room(box_note(f, 2.4), 2.2, 0.3, 0.02, 5000), take, peak=0.6)


def creak():
    """A floorboard upstairs: stick-slip friction, a slow groan of wood."""
    for take in (1, 2, 3):
        dur = rng.uniform(0.6, 1.1)
        x = t(dur)
        rate = rng.uniform(28, 55) * (1 + 0.5 * np.sin(np.pi * x / dur))
        ph = np.cumsum(rate) / SR
        pulses = np.exp(-((ph % 1.0) / 0.08)) * np.sin(np.pi * x / dur) ** 0.7
        noise = rng.standard_normal(len(x))
        y = bp(pulses * (0.5 + 0.5 * noise), 180, 1400, 2)
        save("creak", room(y, 1.4, 0.25, 0.02, 2500), take, peak=0.7)


def buzz():
    """The lamp flex at the socket: mains hum, crackle, a hard fizz."""
    dur = 1.4
    x = t(dur)
    hum = sum(np.sin(2 * np.pi * 50 * k * x) / k for k in (1, 2, 3, 5, 7))
    crack = (rng.random(len(x)) < 0.002) * rng.standard_normal(len(x)) * 6
    fizz = hp(rng.standard_normal(len(x)), 3000, 2) * (0.5 + 0.5 * np.sin(2 * np.pi * 9 * x))
    y = np.tanh(1.8 * (hum * 0.4 + lp(crack, 6000, 2) + fizz * 0.25)) * np.minimum(1, x / 0.01) * np.minimum(1, (dur - x) / 0.08)
    save("buzz", y, 1, peak=0.75)


def fridge_hum():
    """The kitchen at rest: a fridge's low hum, its compressor ticking."""
    dur = 5.0
    x = t(dur)
    y = 0.6 * np.sin(2 * np.pi * 60 * x) + 0.25 * np.sin(2 * np.pi * 120 * x) + 0.08 * lp(rng.standard_normal(len(x)), 400, 2)
    y *= 0.85 + 0.15 * np.sin(2 * np.pi * 0.3 * x)
    y *= np.minimum(1, x / 0.6) * np.minimum(1, (dur - x) / 0.6)
    save("fridge_hum", y, 1, peak=0.35)


def swing_creak():
    """An empty swing still moving: the chains' squeak at each end of the arc."""
    dur = 3.2
    x = t(dur)
    y = np.zeros(len(x))
    for k, at in enumerate((0.2, 1.0, 1.8, 2.6)):
        a = int(at * SR)
        n = int(0.32 * SR)
        tt = np.arange(n) / SR
        f = 620 + 260 * np.sin(np.pi * tt / 0.32) + (40 if k % 2 else 0)
        sq = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * tt / 0.32) ** 2 * (0.9 - 0.15 * k)
        y[a:a + n] += sq + 0.2 * bp(rng.standard_normal(n), 1500, 4000, 2) * np.sin(np.pi * tt / 0.32)
    save("swing_creak", room(y, 1.6, 0.3, 0.03, 4000), 1, peak=0.55)


def crickets():
    """A summer night in the gardens: crickets near and far, a breath of wind."""
    dur = 8.0
    x = t(dur)
    y = np.zeros(len(x))
    for k in range(7):
        f = rng.uniform(3800, 5200)
        rate = rng.uniform(2.2, 3.4)
        phase = rng.uniform(0, 1)
        gate = (np.mod(x * rate + phase, 1.0) < 0.32).astype(float)
        trill = (np.sin(2 * np.pi * 38 * x) > 0).astype(float)
        amp = rng.uniform(0.15, 1.0)
        y += np.sin(2 * np.pi * f * x) * gate * trill * amp * 0.12
    y = lp(y, 7000, 2) + 0.04 * lp(rng.standard_normal(len(x)), 300, 2)
    y *= np.minimum(1, x / 1.0) * np.minimum(1, (dur - x) / 1.0)
    save("crickets", room(y, 1.2, 0.2, 0.02, 7000), 1, peak=0.35)


# ------------------------------------------------------------------ the walk-out (chapter 6, docs/story/walkout.md)

def outdoors(x, wet=0.16, slap=0.0, slap_gain=0.0, dark=2400):
    """Open air at dawn: no room, only a short diffuse spread off the forecourt's stone and,
    for the big ones, a slap-back off the hill's walls a fraction of a second later."""
    y = room(x, 1.1, wet, 0.012, dark)
    if slap > 0:
        y = mix((y, 1.0, 0.0), (lp(y, dark * 0.5, 2), slap_gain, slap), (lp(y, dark * 0.3, 2), slap_gain * 0.45, slap * 2.3))
    return y


def loopable(y, L, xf):
    """Fold a render of L + xf seconds into an L-second loop: the overhang crossfades
    (equal power, for noise) into the start, so the seam is inaudible."""
    n, k = int(L * SR), int(xf * SR)
    out = y[:n].copy()
    w = np.linspace(0, 1, k)
    out[:k] = y[:k] * np.sqrt(w) + y[n:n + k] * np.sqrt(1 - w)
    return out


def crackle(n, rate, lo=1200, hi=6000, decay=0.0015, amp_env=None, r=None):
    """Sparse little snaps: Poisson-timed clicks of random size, each a decaying burst."""
    r = r or rng
    y = np.zeros(n)
    k = int(decay * SR * 8)
    for i in np.flatnonzero(r.random(n) < rate / SR):
        a = r.pareto(2.2) * 0.3
        if amp_env is not None:
            a *= amp_env[i]
        m = min(k, n - i)
        y[i:i + m] += r.standard_normal(m) * np.exp(-np.arange(m) / (decay * SR * r.uniform(0.5, 1.6))) * a
    return bp(y, lo, hi, 2)


def explosion():
    """A great building going up, heard from 30-80 m away across the forecourt: the crack
    (the shock wave, already dulled by distance), the deep boom with its sub that you feel in
    the chest, then the long rumble of the masonry going with stone and glass crackling in it.
    Three sizes: the door, a wing, the dome."""
    thunder = trim(kit("cinematic-hits/cinematic-thunder-massive"))
    slam = trim(kit("cinematic-hits/cinematic-slam-massive"))
    drum = trim(kit("cinematic-hits/cinematic-drum-massive"))
    hit = trim(kit("booms-and-bass/boom-hit-strong"))
    rumble = trim(kit("booms-and-bass/bass-rumble-long"))
    glass = trim(oc("GLASBrk_WindowPaneSmallBreak_OS_01"))
    for take, (size, dist, rr) in ((1, (1.0, 40, 0.92)), (2, (1.15, 60, 0.85)), (3, (1.35, 75, 0.78))):
        r = np.random.default_rng(600 + take)                            # its own seed: the same takes however it is built
        L = 6.0
        n = int(L * SR)
        x = t(L)
        air = 9000 - dist * 55                                            # air eats the top with distance
        # the crack: an N-wave and a burst of torn air
        cn = int(0.03 * SR)
        nw = np.zeros(cn)
        a = int(0.0009 * size * SR)
        nw[:a] = np.linspace(1, -1, a)
        burst = r.standard_normal(cn) * np.exp(-np.arange(cn) / (0.006 * SR))
        crack = lp(hp(nw + burst * 0.8, 300, 2), air, 2)
        s = lp(shelf_dark(pitch(slam, rr), 3000, 0.5), air, 2)
        # the boom: real hits pitched down, and a synthesised sub sweeping from 60 Hz into the floor
        b1 = lp(pitch(drum, rr * 0.8), 1800, 2)
        b2 = lp(pitch(hit, rr * 0.9), 400, 2)
        f = 26 + 34 * np.exp(-x / (0.35 * size))
        sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(n, 0.006, 0.9 * size)
        # the rumble: thunder and a bass rumble, darkened, and a moving brown-noise body
        th = lp(shelf_dark(pitch(thunder, rr), 1500, 0.7), 2200, 2)
        ru = lp(pitch(rumble, rr), 300, 2)
        brown = hp(np.cumsum(r.standard_normal(n)), 20, 2)
        body = lp(brown, 180, 3)
        body /= np.max(np.abs(body)) + 1e-9
        surge = np.clip(lp(np.abs(r.standard_normal(n)), 3, 1) * 10, 0.3, 1.6)
        body *= surge * np.minimum(1, x / 0.15) * np.exp(-x / (1.6 * size))
        # the debris in the tail: crackle thinning out, a little glass
        deb = crackle(n, 70 * size, 900, 5000, 0.002, np.minimum(1, x / 0.3) * np.exp(-x / 1.4), r)
        deb = lp(deb, air * 0.7, 2)
        gl = lp(shelf_dark(pitch(glass, 0.9 + 0.05 * take), 5000, 0.4), air, 2)
        dry = mix((crack, 0.9, 0.0), (s, 0.7, 0.0), (b1, 0.8, 0.004), (b2, 0.8, 0.0), (sub, 0.85 * size, 0.0),
                  (th, 0.65, 0.05), (ru, 0.55, 0.12), (body, 0.55, 0.08), (deb, 0.6, 0.0), (gl, 0.12, 0.5 + 0.2 * take))
        dry = np.tanh(dry * 1.3) / np.tanh(1.3)                           # weight, without hard clipping
        y = outdoors(dry, 0.2, 0.32 + 0.04 * take, 0.28, 2200)[:n]
        y *= np.minimum(1, (L - x) / 1.2) ** 1.5                          # the tail sinks away by ~6 s
        save("explosion", y, take, peak=0.93)


def debris():
    """Stone coming down after a blast, on the gravel and the paving: a few heavy chunks
    (thuds with a stony clack on top), a rain of smaller stones and grit, a little glass."""
    wood = trim(oc("WOODImpt_LargeWoodDrop_OS_01"))
    cer = trim(oc("CERMImpt_CeramicSmallImpact_OS_01"))
    dirt = [trim(oc(s)) for s in ("FEETHmn_ShoeDirtLand_OS_01", "FEETHmn_ShoeDirtLand_OS_02", "FEETHmn_ShoeConcreteLand_OS_01")]
    glass = [trim(oc("GLASBrk_WindowPaneSmallBreak_OS_01")), trim(oc("GLASBrk_GlassBottleBreak_OS_01"))]

    def clack(r):
        """One stone striking another: a burst through two or three stony resonances."""
        m = int(0.08 * SR)
        nz = r.standard_normal(m) * np.exp(-np.arange(m) / (0.004 * SR))
        y = sum(bp(nz, f * 0.85, f * 1.15, 2) * g for f, g in ((r.uniform(700, 1400), 1.0), (r.uniform(1800, 3200), 0.7), (r.uniform(3500, 5500), 0.3)))
        return y * np.exp(-np.arange(m) / (0.012 * SR))

    for take in (1, 2, 3):
        r = np.random.default_rng(100 + take)
        L = 3.2
        n = int(L * SR)
        parts = []
        for i in range(3 + take % 2):                                     # the big chunks
            at = r.uniform(0.0, 1.8) if i else 0.0
            ch = lp(pitch(wood, r.uniform(0.45, 0.6)), 2500, 2)
            parts += [(ch, r.uniform(0.6, 1.0), at), (clack(r), r.uniform(0.35, 0.6), at + 0.002),
                      (lp(pitch(dirt[i % 3], r.uniform(0.7, 0.9)), 4000, 2), 0.5, at)]
        for i in range(int(40 + 10 * take)):                              # the rain of smaller stones
            at = min(L - 0.4, r.gamma(1.8, 0.7))
            g = 0.7 * np.exp(-at / 2.0) * r.uniform(0.3, 1.0)
            if r.random() < 0.3:
                c = lp(pitch(cer, r.uniform(0.55, 0.9)), 6000, 2)[:int(0.25 * SR)]
                parts.append((fades(c, 0.0005, 0.08), g * 0.5, at))
            else:
                parts.append((clack(r), g * 0.55, at))
        grit = crackle(n, 260, 1500, 6500, 0.0008, np.exp(-t(L) / 1.6), r)  # grit and gravel hiss-free pitter
        gl = shelf_dark(pitch(glass[take % 2], r.uniform(1.0, 1.15)), 6000, 0.4)
        parts += [(grit, 0.5, 0.05), (gl, 0.18, r.uniform(0.2, 0.6))]
        y = mix(*parts)[:n]
        y = lp(y, 7500, 2)
        y *= np.minimum(1, (L - t(len(y) / SR)) / 0.8)
        save("debris", outdoors(y, 0.18, 0.0, 0.0, 2600), take, peak=0.8)


def fire_roar():
    """A big fire going in the ruins, from across the forecourt: a low broadband roar that
    breathes with the draught, the flames' flutter, wood snapping. Loops (6 s)."""
    for take in (1, 2):
        r = np.random.default_rng(200 + take)
        L, xf = 6.0, 1.2
        n = int((L + xf + 1.2) * SR)
        x = np.arange(n) / SR
        fall = oc("WATRFall_Waterfall_LOOP_01", 5.0 + 11.0 * take, n / SR)  # water's roar, darkened, reads as flame
        fall = pad(fall, n)[:n]
        roar = lp(hp(fall, 40, 2), 650, 3)
        roar /= np.max(np.abs(roar)) + 1e-9
        breath = 0.75 + 0.25 * np.sin(2 * np.pi * 0.31 * x + r.uniform(0, 6)) * np.sin(2 * np.pi * 0.13 * x + 1)
        brown = hp(np.cumsum(r.standard_normal(n)), 25, 2)
        low = lp(brown, 220, 3)
        low /= np.max(np.abs(low)) + 1e-9
        flut = bp(r.standard_normal(n), 120, 900, 2)
        flut *= np.clip(lp(r.standard_normal(n), 9, 1) * 25 + 0.5, 0, 2)  # flames licking
        flut /= np.max(np.abs(flut)) + 1e-9
        cr = crackle(n, 40, 900, 5500, 0.0015, None, r)
        snaps = crackle(n, 2.5, 500, 3500, 0.006, None, r)
        y = roar * breath * 0.8 + low * 0.55 + flut * 0.3 + cr * 0.8 + snaps * 1.2
        y = lp(y, 6000, 2)
        y = outdoors(y, 0.22, 0.0, 0.0, 2000)[int(1.0 * SR):]           # drop the reverb's run-in
        y = loopable(y, L, xf)
        save("fire_roar", y, take, peak=0.75, loop=True)


def walkie_click():
    """A toy walkie-talkie's talk button, close in the hush: a crisp little plastic click, then
    the briefest burst of squelch as the radio keys (band-limited like a tiny speaker)."""
    clicks = [trim(kit("interface/click-rocker-firm")), trim(oc("MECHSwtch_SwitchSmallOn_OS_01"))[:int(0.05 * SR)],
              trim(kit("interface/click-toggle-on-firm"))]
    body = trim(kit("foley/foley-plastic-touch"))
    for take in (1, 2, 3):
        r = np.random.default_rng(300 + take)
        c = fades(hp(clicks[take - 1], 400, 2), 0.0003, 0.012)
        b = lp(fades(pitch(body, 1.3), 0.0003, 0.02), 5000, 2)
        sn = int(r.uniform(0.11, 0.15) * SR)
        sq = bp(r.standard_normal(sn), 700, 3200, 3)                    # the squelch, through a tiny speaker
        sq = bp(sq + 0.4 * np.sign(np.sin(2 * np.pi * r.uniform(900, 1300) * np.arange(sn) / SR)) * 0.2, 500, 3500, 2)
        e = np.minimum(1, np.arange(sn) / (0.006 * SR)) * np.exp(-np.arange(sn) / (0.06 * SR))
        e[-int(0.01 * SR):] *= np.linspace(1, 0, int(0.01 * SR))
        sq *= e * (0.85 + 0.15 * np.sign(np.sin(2 * np.pi * 60 * np.arange(sn) / SR)))
        sq /= np.max(np.abs(sq)) + 1e-9
        y = mix((c, 1.0, 0.0), (b, 0.35, 0.0), (sq, 0.22, r.uniform(0.035, 0.05)))
        y = pad(y, int(0.25 * SR))
        save("walkie_click", room(y, 0.25, 0.06, 0.003, 5000), take, peak=0.7)


def plunger():
    """The detonator's T-handle. Take 1: pulled up, the rod ratcheting out of the box, a
    catch at the top. Take 2: rammed down, a heavy hollow thunk in the box."""
    lever = oc("MECHLvr_LeverPull_OS_01")
    ratchet = trim(kit("mechanical/mechanical-ratchet-heavy"))
    latch = trim(kit("mechanical/mechanical-latch-heavy"))
    box = trim(oc("METLHndl_ElectricBoxOpen_OS_01", 0.3))
    # 1: up
    lv = fades(lp(lever[int(0.22 * SR):int(0.9 * SR)], 7000, 2), 0.01, 0.08)
    ra = lp(pitch(ratchet, 1.05), 7000, 2)
    top = fades(lp(pitch(latch, 1.15), 7000, 2), 0.0005, 0.05)
    y = mix((ra, 1.0, 0.0), (lv, 0.35, 0.0), (top, 0.7, 0.52))
    save("plunger", outdoors(y, 0.1), 1, peak=0.75)
    # 2: down
    slide = fades(lp(pitch(ratchet[:int(0.2 * SR)], 1.6), 6000, 2), 0.002, 0.03)
    thunk = lp(trim(kit("impacts/impact-deep-firm")), 1800, 2)
    m = lp(pitch(box, 0.85), 4000, 2)
    n = int(0.4 * SR)
    body = np.sin(2 * np.pi * (95 - 30 * t(0.4)) * t(0.4)) * env(n, 0.002, 0.07)
    y = mix((slide, 0.5, 0.0), (thunk, 1.0, 0.11), (fades(pitch(latch, 0.8), 0.0005, 0.04), 0.8, 0.11), (m, 0.35, 0.115), (body, 0.6, 0.11))
    save("plunger", outdoors(y, 0.12), 2, peak=0.8)


def birds():
    """Dawn over the moor, small and far away. Takes 1-2: a few birds trying their first
    phrases, sparse chirps and trills (synthesised, every one a little different) over a whisper
    of a real chorus a long way off. Take 3: crows cawing far off over the tower."""
    chorus = oc("AMBForst_ForestBirds_OS_02", 0.3, 6.6)

    def chirp(r, kind):
        if kind == 0:                                                     # a down-sweep "tsiu"
            d = r.uniform(0.05, 0.12)
            x = t(d)
            f = r.uniform(4500, 6500) - r.uniform(1500, 2800) * (x / d)
        elif kind == 1:                                                   # an up-slur "wheet"
            d = r.uniform(0.1, 0.18)
            x = t(d)
            f = r.uniform(2600, 3400) + r.uniform(800, 1800) * (x / d) ** 2
        else:                                                             # a trill
            d = r.uniform(0.25, 0.5)
            x = t(d)
            f = r.uniform(3800, 5200) + 700 * np.sin(2 * np.pi * r.uniform(18, 28) * x)
        y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * x / d) ** 1.5
        return y

    for take in (1, 2):
        r = np.random.default_rng(400 + take)
        L = 6.0
        parts = [(fades(lp(hp(chorus, 1500, 2), 6000, 2), 0.8, 1.2), 0.12, 0.0)]
        at = r.uniform(0.2, 0.6)
        while at < L - 0.6:
            g = r.uniform(0.25, 0.7)
            kind = int(r.integers(0, 3))
            for k in range(int(r.integers(1, 4)) if kind < 2 else 1):    # a phrase of one to three notes
                c = chirp(r, kind)
                parts.append((c, g * (0.9 ** k), at + k * r.uniform(0.12, 0.2)))
            at += r.uniform(0.5, 1.4)
        y = mix(*parts)[:int(L * SR)]
        y = lp(y, 6500, 2)                                                # far away: the top goes
        y *= np.minimum(1, t(len(y) / SR) / 0.5) * np.minimum(1, (L - t(len(y) / SR)) / 0.8)
        save("birds", outdoors(y, 0.3, 0.0, 0.0, 4000), take, peak=0.45)

    # crows: a harsh nasal caw, a buzzy tone through the beak's formants, in twos and threes
    r = np.random.default_rng(410)
    L = 6.0
    parts = []
    for at, count in ((0.3, 3), (2.6, 2), (4.4, 3)):
        f0 = r.uniform(430, 520)
        for k in range(count):
            d = r.uniform(0.28, 0.4)
            x = t(d)
            f = f0 * (1.05 - 0.15 * x / d) * (1 + 0.03 * r.standard_normal(len(x)).cumsum() / np.sqrt(len(x)))
            src = sum(np.sin(2 * h * np.pi * np.cumsum(f) / SR) / h ** 0.7 for h in range(1, 14))
            src += 0.3 * r.standard_normal(len(x))                       # the rasp
            v = bp(src, 1000, 1700, 2) + 0.7 * bp(src, 1900, 2600, 2) + 0.3 * bp(src, 600, 900, 2)
            v *= np.minimum(1, x / 0.025) * np.minimum(1, (d - x) / 0.08) * (0.8 + 0.2 * np.sin(2 * np.pi * 28 * x))
            parts.append((v, r.uniform(0.7, 1.0) * (1 if at < 4 else 0.7), at + k * r.uniform(0.45, 0.6)))
    y = mix(*parts)
    y = lp(y, 3200, 2)
    y = pad(y, int(L * SR))[:int(L * SR)]
    save("birds", outdoors(y, 0.35, 0.28, 0.2, 2500), 3, peak=0.45)


def ember():
    """The slow motion: hot wind off the fire, embers drifting past. A soft airy swell, never
    a hiss, with the faintest ticks of burning flakes in it. Very quiet."""
    wh = trim(kit("whooshes/whoosh-slow-soft"))
    for take in (1, 2):
        r = np.random.default_rng(500 + take)
        L = 3.2
        n = int(L * SR)
        x = t(L)
        src = r.standard_normal(n)
        sw = np.sin(np.pi * x / L) ** 2
        air = bp(src, 250, 1600, 2) * sw + 0.4 * bp(src, 1600, 3200, 2) * sw ** 2
        air *= 0.8 + 0.2 * np.sin(2 * np.pi * 0.7 * x + r.uniform(0, 6))
        air /= np.max(np.abs(air)) + 1e-9
        w = fades(lp(pitch(wh, 0.55 + 0.05 * take), 2500, 2), 0.3, 0.6)
        ticks = crackle(n, 6, 2000, 5000, 0.0007, sw, r)
        y = mix((air, 0.6, 0.0), (w, 0.6, 0.4 + 0.2 * take), (ticks, 0.35, 0.0))[:n]
        y = lp(y, 4500, 2) * np.minimum(1, x / 0.6) * np.minimum(1, (L - x) / 0.8)
        save("ember", outdoors(y, 0.25), take, peak=0.35)


ALL = ("thunder", "drip", "splat", "splash", "breath", "tinnitus", "heartbeat", "static",
       "whispers", "drone", "latch", "wind", "rotor", "piano",
       "chain", "body_splash", "crawl", "glint", "knife_grab", "flash_cut", "phone_buzz", "phone_click", "gunshot_indoor", "gun_draw", "gun_cock", "shell_casing",
       "gun_drop_water", "struggle", "death_breath", "memory_elena", "train_pass", "car_rev", "car_door",
       "memory_maya", "music_note", "creak", "buzz", "fridge_hum", "swing_creak", "crickets",
       "explosion", "debris", "fire_roar", "walkie_click", "plunger", "birds", "ember")

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("sounds", nargs="*", help="only these (default: all)")
    ap.add_argument("--oculus", default="/home/user/facebookincubator/meta-open-sound/corpora/oculus-audio-pack/ogg")
    ap.add_argument("--kit", default="/home/user/wyzowl/sfx-kit/sounds")
    a = ap.parse_args()
    O, K = a.oculus, a.kit
    if not os.path.isdir(O) or not os.path.isdir(K):
        sys.exit("need the two sound libraries: --oculus <...>/oculus-audio-pack/ogg --kit <...>/sounds")
    bad = [s for s in a.sounds if s not in ALL]
    if bad:
        sys.exit("unknown sound(s): " + ", ".join(bad))
    for name in (a.sounds or ALL):
        clear(name)
        globals()[name]()
