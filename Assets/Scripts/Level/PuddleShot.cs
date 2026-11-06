using UnityEngine;

namespace ProjectInk
{
    /// <summary>The prologue's reflection shot, simulated: he is on hands and knees over a puddle.
    /// One drop falls in; ONE ripple spreads out in slow motion (a real wave packet: a few crests
    /// that bend the reflection and catch the light, dying as they travel); as the water calms his
    /// face surfaces in it, black against the light of the roof crack behind his head, eye slits and
    /// the scar at the temple white; then lightning blows the reflection stark white.
    ///
    /// Two textures: the wet concrete (static, detailed, made once) and the water (its reflection
    /// recomputed every frame on the CPU, at a modest size, drawn over it with a soft edge).
    /// The reflection itself is prepared once at double resolution, plain and in the lightning.</summary>
    public class PuddleShot
    {
        public const float DropAt = 0.6f, FlashAt = 3.8f;

        readonly int W, H, RW, RH;                     // water texture, reflection source (2x)
        readonly float asp;
        public Texture2D Concrete, Water;
        readonly Color32[] px;
        readonly float[] mask, dist, dxn, dyn;
        readonly float[] refR, refG, refB, flR, flG, flB;
        const float PY = 0.5f;
        readonly float PX;

        public PuddleShot(float screenAspect)
        {
            H = 216;
            W = Mathf.Clamp(Mathf.RoundToInt(H * screenAspect), 216, 520);
            asp = W / (float)H;
            PX = asp * 0.5f;
            RW = W * 2; RH = H * 2;
            px = new Color32[W * H];
            mask = new float[W * H]; dist = new float[W * H]; dxn = new float[W * H]; dyn = new float[W * H];
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    float x = i / (float)H, y = 1f - j / (float)H;           // texture rows run bottom-up
                    Mask(x, y, out float m, out float d, out _);
                    int k = j * W + i;
                    mask[k] = m; dist[k] = d;
                    float dx = x - PX, dy = (y - PY) * 1.25f, inv = 1f / Mathf.Max(d, 1e-4f);
                    dxn[k] = dx * inv; dyn[k] = dy * inv;
                }
            refR = new float[RW * RH]; refG = new float[RW * RH]; refB = new float[RW * RH];
            flR = new float[RW * RH]; flG = new float[RW * RH]; flB = new float[RW * RH];
            BuildReflection();
            Concrete = BuildConcrete(W * 2, H * 2);
            Water = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        }

        /// <summary>The puddle: an irregular blob, seen a little obliquely.</summary>
        void Mask(float x, float y, out float m, out float d, out float r)
        {
            float dx = x - PX, dy = (y - PY) * 1.25f;
            d = Mathf.Sqrt(dx * dx + dy * dy);
            float th = Mathf.Atan2(dy, dx);
            r = 0.40f + 0.045f * Mathf.Sin(3f * th + 1f) + 0.03f * Mathf.Sin(5f * th + 2f) + 0.018f * Mathf.Sin(9f * th + 0.5f);
            m = Mathf.Clamp01((r - d) / 0.012f * 0.5f + 0.5f);
        }

        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static float Noise(float x, float y, int s)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Mathf.Lerp(Hash(x0, y0, s), Hash(x0 + 1, y0, s), fx);
            float b = Mathf.Lerp(Hash(x0, y0 + 1, s), Hash(x0 + 1, y0 + 1, s), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>Wet concrete: mottled, aggregate specks, a few hairline cracks, darker where wet.</summary>
        Texture2D BuildConcrete(int cw, int ch)
        {
            var tex = new Texture2D(cw, ch, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var c = new Color32[cw * ch];
            for (int j = 0; j < ch; j++)
                for (int i = 0; i < cw; i++)
                {
                    float x = i / (float)ch, y = 1f - j / (float)ch;
                    float v = 0.115f + 0.05f * (Noise(x * 7f, y * 7f, 1) - 0.5f) + 0.03f * (Noise(x * 36f, y * 36f, 2) - 0.5f);
                    float sp = Hash(i, j, 9);
                    if (sp > 0.985f) v += 0.07f; else if (sp < 0.01f) v -= 0.05f;
                    Mask(x, y, out _, out float d, out float r);
                    float wet = Mathf.Clamp01(1f - (d - r) / 0.09f);
                    v *= 1f - 0.35f * wet;
                    c[j * cw + i] = new Color32((byte)(Mathf.Clamp01(v * 0.95f) * 255), (byte)(Mathf.Clamp01(v * 0.98f) * 255), (byte)(Mathf.Clamp01(v * 1.06f) * 255), 255);
                }
            // hairline cracks
            var rnd = new System.Random(3);
            for (int n = 0; n < 4; n++)
            {
                float x = (float)rnd.NextDouble() * cw, y = (float)rnd.NextDouble() * ch;
                for (int s = 0; s < 16; s++)
                {
                    float nx = x + (float)(rnd.NextDouble() - 0.5) * 70f, ny = y + (float)(rnd.NextDouble() - 0.5) * 70f;
                    for (int k = 0; k <= 30; k++)
                    {
                        int qx = Mathf.RoundToInt(Mathf.Lerp(x, nx, k / 30f)), qy = Mathf.RoundToInt(Mathf.Lerp(y, ny, k / 30f));
                        if (qx >= 0 && qy >= 0 && qx < cw && qy < ch) c[qy * cw + qx] = new Color32(14, 14, 16, 255);
                    }
                    x = nx; y = ny;
                }
            }
            tex.SetPixels32(c);
            tex.Apply();
            return tex;
        }

        /// <summary>What the still water shows: the ceiling above him (dark trusses, the bright jagged
        /// seam of the roof crack haloing his head) and his face. A plain version and a lightning one.</summary>
        void BuildReflection()
        {
            float cx = PX, cy = PY - 0.02f;
            for (int j = 0; j < RH; j++)
                for (int i = 0; i < RW; i++)
                {
                    float x = i / (float)RH, y = 1f - j / (float)RH;
                    float dx = x - cx, dy = y - cy, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float halo = Mathf.Exp(-(r / 0.26f) * (r / 0.26f));
                    float br = 0.05f + 0.26f * halo, bg = 0.06f + 0.32f * halo, bb = 0.09f + 0.46f * halo;
                    // trusses: dark diagonal beams across the ceiling
                    foreach (var (off, w) in new[] { (-0.22f, 0.02f), (0.05f, 0.016f), (0.3f, 0.02f) })
                    {
                        float dd = Mathf.Abs(dx * 0.55f + dy - off) / 1.14f;
                        float k = 1f - 0.75f * Mathf.Clamp01((w - dd) / 0.003f + 0.5f);
                        br *= k; bg *= k; bb *= k;
                    }
                    int idx = j * RW + i;
                    refR[idx] = br; refG[idx] = bg; refB[idx] = bb;
                }
            // the crack: a continuous jagged seam of storm light behind him, stamped as soft dots
            for (int n = 0; n <= 260; n++)
            {
                float t = n / 260f;
                float sx = cx - 0.5f + t, sy = cy - 0.06f + 0.05f * Mathf.Sin(t * 23f) * Mathf.Sin(t * 7f + 1f) + 0.012f * Mathf.Sin(t * 91f);
                int ci = Mathf.RoundToInt(sx * RH), cj = Mathf.RoundToInt((1f - sy) * RH);
                for (int dj = -6; dj <= 6; dj++)
                    for (int di = -6; di <= 6; di++)
                    {
                        int ii = ci + di, jj = cj + dj;
                        if (ii < 0 || jj < 0 || ii >= RW || jj >= RH) continue;
                        float g = Mathf.Exp(-(di * di + dj * dj) / 7f) * 0.55f;
                        int idx = jj * RW + ii;
                        refR[idx] += g * 0.9f; refG[idx] += g * 0.95f; refB[idx] += g;
                    }
            }
            // the lightning version of the ceiling, then the face on both
            for (int k = 0; k < refR.Length; k++)
            {
                flR[k] = refR[k] * 2.4f + 0.30f; flG[k] = refG[k] * 2.4f + 0.36f; flB[k] = refB[k] * 2.4f + 0.50f;
            }
            for (int j = 0; j < RH; j++)
                for (int i = 0; i < RW; i++)
                {
                    float x = i / (float)RH, y = 1f - j / (float)RH;
                    float dx = x - cx, dy = y - cy, r = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = j * RW + i;
                    float head = Mathf.Clamp01((0.155f - r) * RH + 0.5f);                 // anti-aliased edge
                    float sh = (dx / 0.36f) * (dx / 0.36f) + ((y - (cy + 0.33f)) / 0.2f) * ((y - (cy + 0.33f)) / 0.2f);
                    float body = Mathf.Max(head, Mathf.Clamp01((1f - sh) * 40f));
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.158f) * RH * 0.5f) * (1f - head);
                    // eye slits (slanted, a little angry) and the scar at the temple: stark white
                    float white = 0f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float ex = dx - s * 0.058f, ey = (y - (cy - 0.005f)) + s * ex * 0.22f;
                        white = Mathf.Max(white, Mathf.Clamp01((0.03f - Mathf.Abs(ex)) * RH) * Mathf.Clamp01((0.0055f - Mathf.Abs(ey)) * RH));
                    }
                    white = Mathf.Max(white, Scar(dx, dy) * head);
                    Shade(refR, refG, refB, idx, body, rim, white);
                    Shade(flR, flG, flB, idx, body, rim, white);
                }
        }

        float Scar(float dx, float dy)
        {
            var pts = new[] { new Vector2(-0.11f, -0.105f), new Vector2(-0.085f, -0.075f), new Vector2(-0.105f, -0.05f), new Vector2(-0.075f, -0.025f) };
            float best = 1f;
            var p = new Vector2(dx, -dy);
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (p - (a + ab * t)).magnitude);
            }
            return Mathf.Clamp01((0.0045f - best) * RH);
        }

        static void Shade(float[] r, float[] g, float[] b, int i, float body, float rim, float white)
        {
            r[i] = Mathf.Lerp(r[i] * (1f - 0.4f * rim) + 0.08f * rim, 0.015f, body);
            g[i] = Mathf.Lerp(g[i] * (1f - 0.4f * rim) + 0.08f * rim, 0.015f, body);
            b[i] = Mathf.Lerp(b[i] * (1f - 0.4f * rim) + 0.08f * rim, 0.02f, body);
            r[i] = Mathf.Lerp(r[i], 0.96f, white); g[i] = Mathf.Lerp(g[i], 0.96f, white); b[i] = Mathf.Lerp(b[i], 0.94f, white);
        }

        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>How much the lightning lights the water at time u (on, a stutter, fading).</summary>
        public static float Flash(float u)
        {
            if (u < FlashAt || u >= FlashAt + 0.7f) return 0f;
            float k = (u - FlashAt) / 0.7f;
            return (1f - k) * (k > 0.12f && k < 0.2f ? 0.4f : 1f);
        }

        /// <summary>Recompute the water for time u (story seconds into the shot).</summary>
        public void Update(float u)
        {
            float ti = u - DropAt;
            float clarity = u < DropAt ? 0.1f : 0.08f + 0.92f * Ease(0.9f, 3.4f, u);
            float fl = Flash(u);
            float A = ti > 0f ? Mathf.Exp(-ti * 0.85f) * Mathf.Min(1f, ti * 6f) : 0f;
            float rf0 = 0.035f + 0.12f * ti, rf1 = 0.02f + 0.085f * ti, rf2 = 0.01f + 0.055f * ti;
            const float wl = 0.034f;
            float kk = 2f * Mathf.PI / wl;
            float Lx = -0.3f, Ly = -0.95f;
            float wR = 0.035f, wG = 0.045f, wB = 0.065f;
            for (int k = 0; k < px.Length; k++)
            {
                float m = mask[k];
                if (m <= 0f) { px[k] = new Color32(0, 0, 0, 0); continue; }
                float d = dist[k];
                float s = 0f;
                if (A > 0.001f)
                {
                    s += Packet(d - rf0, 0.045f, kk) * 1.0f;
                    s += Packet(d - rf1, 0.06f, kk) * 0.55f;
                    s += Packet(d - rf2, 0.075f, kk) * 0.3f;
                    s *= A;
                }
                float disp = s * 0.00045f;
                int i = k % W, j = k / W;
                float sx = (i / (float)H + dxn[k] * disp) * RH;                        // reflection source (2x)
                float sy = (j / (float)H - dyn[k] * disp / 1.25f) * RH;
                Sample(sx, sy, fl, out float r, out float g, out float b);
                r = wR + (r - wR) * clarity; g = wG + (g - wG) * clarity; b = wB + (b - wB) * clarity;
                float lit = s * (dxn[k] * Lx + dyn[k] * Ly);
                if (lit > 0f) { float hl = lit * 0.0016f; r += hl * 0.75f; g += hl * 0.85f; b += hl; }
                else { float dk = 1f + lit * 0.0009f; r *= dk; g *= dk; b *= dk; }
                if (m > 0.25f && m < 0.75f) { r += 0.06f; g += 0.06f; b += 0.07f; }       // the meniscus at the edge
                px[k] = new Color32((byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255), (byte)(Mathf.Clamp01(b) * 255), (byte)(m * 255));
            }
            Water.SetPixels32(px);
            Water.Apply(false);
        }

        /// <summary>The slope of a wave packet (a few crests under a gaussian), at offset x from its front.</summary>
        static float Packet(float x, float sig, float kk)
        {
            if (x > sig * 3f || x < -sig * 3f) return 0f;
            float e = Mathf.Exp(-(x / sig) * (x / sig));
            return e * (-kk * Mathf.Sin(kk * x) - 2f * x / (sig * sig) * Mathf.Cos(kk * x));
        }

        void Sample(float sx, float sy, float fl, out float r, out float g, out float b)
        {
            sx = Mathf.Clamp(sx, 0f, RW - 1.001f); sy = Mathf.Clamp(sy, 0f, RH - 1.001f);
            int x0 = (int)sx, y0 = (int)sy;
            float fx = sx - x0, fy = sy - y0;
            int i00 = y0 * RW + x0, i10 = i00 + 1, i01 = i00 + RW, i11 = i01 + 1;
            float w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
            r = refR[i00] * w00 + refR[i10] * w10 + refR[i01] * w01 + refR[i11] * w11;
            g = refG[i00] * w00 + refG[i10] * w10 + refG[i01] * w01 + refG[i11] * w11;
            b = refB[i00] * w00 + refB[i10] * w10 + refB[i01] * w01 + refB[i11] * w11;
            if (fl > 0f)
            {
                float fr = flR[i00] * w00 + flR[i10] * w10 + flR[i01] * w01 + flR[i11] * w11;
                float fg = flG[i00] * w00 + flG[i10] * w10 + flG[i01] * w01 + flG[i11] * w11;
                float fb = flB[i00] * w00 + flB[i10] * w10 + flB[i01] * w01 + flB[i11] * w11;
                r = Mathf.Lerp(r, fr, fl); g = Mathf.Lerp(g, fg, fl); b = Mathf.Lerp(b, fb, fl);
            }
        }
    }
}
