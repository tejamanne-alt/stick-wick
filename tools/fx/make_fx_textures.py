"""Flipbook textures for the walk-out's blasts (docs/story/walkout.md), made from layered 3D noise:

    fx_fireball  8x8 frames: a turbulent fireball blooming, its core white-hot through yellow and
                 orange, cooling to deep red and then rolling black smoke at its edges
    fx_smoke     8x8 frames: a billowing smoke puff that rolls and spreads, lit from the upper right
                 (bright rims, dark bellies); greyscale, tinted per particle
    fx_flame     8x8 frames: tongues of flame licking upwards (a loop), for the fires that stay
    fx_dust      4x4 frames: a soft dust puff spreading and thinning
    fx_ring      a shockwave ring: a thin bright edge with a soft inside
    fx_glow      a soft round glow

Writes Assets/Resources/Fx/<name>.png.   python tools/fx/make_fx_textures.py"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Fx")
rng = np.random.default_rng(1907)
LAT = rng.random((64, 64, 64)).astype(np.float32)


def vnoise(x, y, z):
    """Smooth value noise on a 64^3 periodic lattice (trilinear, smoothstep)."""
    xi, yi, zi = np.floor(x).astype(int), np.floor(y).astype(int), np.floor(z).astype(int)
    xf, yf, zf = x - xi, y - yi, z - zi
    u, v, w = xf * xf * (3 - 2 * xf), yf * yf * (3 - 2 * yf), zf * zf * (3 - 2 * zf)
    def L(a, b, c):
        return LAT[(xi + a) % 64, (yi + b) % 64, (zi + c) % 64]
    x00 = L(0, 0, 0) * (1 - u) + L(1, 0, 0) * u
    x10 = L(0, 1, 0) * (1 - u) + L(1, 1, 0) * u
    x01 = L(0, 0, 1) * (1 - u) + L(1, 0, 1) * u
    x11 = L(0, 1, 1) * (1 - u) + L(1, 1, 1) * u
    return (x00 * (1 - v) + x10 * v) * (1 - w) + (x01 * (1 - v) + x11 * v) * w


def fbm(x, y, z, oct=6, lac=2.03, gain=0.52):
    s, a, f, tot = 0.0, 1.0, 1.0, 0.0
    for i in range(oct):
        s = s + a * vnoise(x * f + i * 7.3, y * f + i * 3.1, z * f + i * 5.7)
        tot += a
        a *= gain
        f *= lac
    return s / tot


def grid(n):
    c = (np.arange(n) + 0.5) / n * 2 - 1
    X, Y = np.meshgrid(c, -c)            # y up
    return X.astype(np.float32), Y.astype(np.float32)


def ramp(t, keys):
    """Colour ramp: keys [(pos, (r, g, b))] over t in [0, 1]."""
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,), np.float32)
    for i in range(len(keys) - 1):
        p0, c0 = keys[i]
        p1, c1 = keys[i + 1]
        m = (t >= p0) & (t <= p1)
        k = ((t - p0) / max(1e-6, p1 - p0))[m][:, None]
        out[m] = np.array(c0) * (1 - k) + np.array(c1) * k
    return out


def shade(dens, X, Y):
    """Fake lighting of a puff from the upper right: the density's gradient as a normal."""
    gy, gx = np.gradient(dens)
    nx, ny = -gx * 18, gy * 18
    nz = np.ones_like(nx)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly, lz = 0.55, 0.55, 0.63
    d = (nx * lx + ny * ly + nz * lz) / l
    rim = np.clip(X * 0.35 + Y * 0.45, -1, 1)
    return np.clip(0.55 + 0.6 * d + 0.25 * rim, 0.1, 1.3)


def sheet(frames, cols):
    n = frames[0].shape[0]
    rows = (len(frames) + cols - 1) // cols
    S = np.zeros((rows * n, cols * n, 4), np.float32)
    for i, f in enumerate(frames):
        r, c = i // cols, i % cols
        S[r * n:(r + 1) * n, c * n:(c + 1) * n] = f
    return S


def save(name, S):
    os.makedirs(OUT, exist_ok=True)
    img = Image.fromarray((np.clip(S, 0, 1) * 255).astype(np.uint8), "RGBA")
    img.save(os.path.join(OUT, name + ".png"))
    print("wrote", name, img.size)


def fireball(n=128, F=64):
    X, Y = grid(n)
    R = np.sqrt(X * X + Y * Y)
    frames = []
    for f in range(F):
        p = f / (F - 1)
        rad = 0.5 + 0.42 * (1 - (1 - p) ** 2.2)
        z = p * 3.0
        warp = fbm(X * 2.2 + 11, Y * 2.2 - p * 1.4, z, 4)
        Xw, Yw = X + (warp - 0.5) * 0.5, Y + (warp - 0.5) * 0.5 - p * 0.12
        Rw = np.sqrt(Xw * Xw + Yw * Yw)
        nz = fbm(Xw * 3.2 + 3, Yw * 3.2 - p * 2.2, z * 1.6, 6)
        dens = np.clip((rad - Rw + (nz - 0.5) * 0.75) / 0.12, 0, 1)
        dens = dens * (0.55 + 0.6 * nz)
        # heat: the core hottest, cooling with time and towards the edge; the billows' tops cooler
        heat = np.clip((1 - Rw / max(rad, 1e-3)) * 1.25 + (nz - 0.5) * 0.9, 0, 1) * (1 - p) ** 1.25
        heat = np.clip(heat * 1.35, 0, 1)
        fire = ramp(heat, [(0.0, (0.07, 0.06, 0.055)), (0.12, (0.22, 0.09, 0.04)), (0.3, (0.7, 0.2, 0.04)),
                           (0.5, (1.0, 0.45, 0.08)), (0.72, (1.0, 0.74, 0.3)), (0.9, (1.0, 0.93, 0.7)), (1.0, (1.0, 1.0, 0.95))])
        lit = shade(dens, X, Y)[..., None]
        cool = (heat < 0.15)[..., None]
        rgb = np.where(cool, fire * lit, fire * (0.85 + 0.25 * lit))
        a = np.clip(dens * (1.0 - 0.35 * p), 0, 1) * np.clip((1.0 - R) * 3, 0, 1)
        frames.append(np.dstack([rgb, a]))
    save("fx_fireball", sheet(frames, 8))


def blur(a, k=3):
    for _ in range(k):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


def smoke(n=128, F=64):
    """A cauliflower of billows (overlapping balls, each one roughened), rolling outwards."""
    X, Y = grid(n)
    R = np.sqrt(X * X + Y * Y)
    r2 = np.random.default_rng(77)
    blobs = [(r2.uniform(-0.38, 0.38), r2.uniform(-0.3, 0.36), r2.uniform(0.3, 0.52), r2.uniform(0, 6.28)) for _ in range(18)]
    frames = []
    for f in range(F):
        p = f / (F - 1)
        grow = 0.85 + 0.25 * (1 - (1 - p) ** 2)
        z = p * 1.6
        field = np.zeros_like(X)
        for (bx, by, br, ph) in blobs:
            ang = ph + p * 1.2
            cx, cy = bx * grow + 0.04 * np.cos(ang), by * grow + 0.04 * np.sin(ang) + p * 0.05
            d2 = ((X - cx) ** 2 + (Y - cy) ** 2) / (br * grow * 0.75) ** 2
            field = field + np.exp(-d2 * 1.6)
        nz = fbm(X * 3.0 + 8, Y * 3.0 - p, z, 6)
        det = fbm(X * 7 + 1, Y * 7 - p * 2, z + 9, 4)
        f2 = field + (nz - 0.5) * 0.9 + (det - 0.5) * 0.35
        t_ = np.clip((f2 - 0.25) / 0.7, 0, 1)
        a = t_ * t_ * (3 - 2 * t_)
        a *= np.clip((1.0 - R) * 4, 0, 1) * (1 - 0.5 * p ** 1.6)
        # light from the upper right: the field's slope, plus the billows' own relief
        bf = blur(field, 3)
        toward = np.roll(np.roll(bf, 5, axis=0), -5, axis=1)          # the field one step towards the light
        light = np.clip(0.55 + (bf - toward) / max(1e-3, bf.max()) * 3.5 + (det - 0.5) * 0.7, 0.12, 1.25)
        g = np.clip(0.36 * light + 0.1 * (nz - 0.5) + 0.04, 0, 1)
        frames.append(np.dstack([g, g, g * 0.97, a]))
    save("fx_smoke", sheet(frames, 8))


def flame(n=128, F=64):
    X, Y = grid(n)
    frames = []
    v = (Y + 1) / 2                                         # 0 at the bottom, 1 at the top
    for f in range(F):
        p = f / F
        ang = p * 2 * np.pi
        z0, z1 = np.cos(ang) * 1.5, np.sin(ang) * 1.5      # a loop: round a circle in the noise
        nz = fbm(X * 2.6 + z0, Y * 1.6 - p * 5.0, z1 + 3, 5)
        nz2 = fbm(X * 5.0 + z1, Y * 3.0 - p * 9.0, z0 + 7, 4)
        sway = (nz - 0.5) * 0.5 * v
        width = 0.5 * (1 - v) ** 0.55
        core = np.clip((width - np.abs(X + sway)) / (0.12 + 0.2 * v), 0, 1)
        tongue = np.clip(core * (nz2 * 1.7 - 0.35 + (1 - v) * 0.9), 0, 1)
        tongue *= np.clip(v * 6, 0, 1)                      # soft at its foot
        heat = np.clip(tongue * (1.25 - v * 0.7), 0, 1)
        rgb = ramp(heat, [(0.0, (0.3, 0.06, 0.01)), (0.3, (0.9, 0.28, 0.04)), (0.6, (1.0, 0.56, 0.12)), (0.85, (1.0, 0.84, 0.45)), (1.0, (1.0, 0.97, 0.85))])
        a = np.clip(tongue * 1.25, 0, 1) ** 1.2
        frames.append(np.dstack([rgb, a]))
    save("fx_flame", sheet(frames, 8))


def dust(n=128, F=16):
    X, Y = grid(n)
    R = np.sqrt(X * X + Y * Y)
    frames = []
    for f in range(F):
        p = f / (F - 1)
        nz = fbm(X * 2.4 + 2, Y * 2.4, p * 1.5, 5)
        dens = np.clip((0.55 + 0.35 * p - R + (nz - 0.5) * 0.7) / 0.3, 0, 1) * (1 - 0.6 * p)
        g = np.clip(0.65 + 0.3 * (nz - 0.5), 0, 1)
        frames.append(np.dstack([g, g * 0.98, g * 0.94, dens * 0.85]))
    save("fx_dust", sheet(frames, 4))


def ring(n=256):
    X, Y = grid(n)
    R = np.sqrt(X * X + Y * Y)
    nz = fbm(X * 6, Y * 6, 0.5, 4)
    edge = np.exp(-((R - 0.86) / 0.035) ** 2) + 0.35 * np.exp(-((R - 0.78) / 0.09) ** 2)
    a = np.clip(edge * (0.7 + 0.6 * nz), 0, 1) * (R < 0.99)
    S = np.dstack([np.ones_like(a), np.ones_like(a), np.ones_like(a), a])
    save("fx_ring", S)


def glow(n=128):
    X, Y = grid(n)
    R = np.sqrt(X * X + Y * Y)
    a = np.clip(1 - R, 0, 1) ** 2.2
    save("fx_glow", np.dstack([np.ones_like(a), np.ones_like(a), np.ones_like(a), a]))


if __name__ == "__main__":
    fireball()
    smoke()
    flame()
    dust()
    ring()
    glow()
