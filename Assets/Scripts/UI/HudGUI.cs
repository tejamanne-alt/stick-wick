using UnityEngine;

namespace ProjectInk
{
    /// <summary>Prototype HUD drawn with IMGUI (no UI assets): health, Flow meter and Rampage
    /// prompt, weapon in hand, live style rank, floor and wave, banners, spawn warnings,
    /// cinematic letterbox bars in slow-mo, the hero marker, results with saved bests,
    /// and a diagnostics readout (F1).</summary>
    public class HudGUI : MonoBehaviour
    {
        public HeroController hero;
        public WaveDirector director;
        public bool showDiagnostics = false;   // F1 toggles

        GUIStyle big, mid, small;
        /// <summary>How much larger than the 720-line design the interface is drawn.</summary>
        static float UiBoost => Application.isMobilePlatform ? 1.35f : 1.2f;
        Texture2D white;
        float scale, bars;

        void Build()
        {
            white = Texture2D.whiteTexture;
            // white type with a soft shadow, the way film titles sit over a black-and-white picture
            LoadFonts();
            big = new GUIStyle { font = Display, fontSize = 64, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            mid = new GUIStyle(big) { fontSize = 34 };
            small = new GUIStyle(big) { font = Body, fontSize = 18, fontStyle = FontStyle.Normal };
            small.normal.textColor = new Color(0.86f, 0.86f, 0.86f);
            glowTex = RadialTexture(64);
            edgeTex = EdgeTexture(64);
        }

        // ------------------------------------------------------------------ type
        // Display: Bebas Neue, tall condensed capitals, the film-poster face, for titles, banners,
        //          the score, the rank letter and the hit counter.
        // Body:    Barlow Condensed SemiBold, a clean sporty condensed face for small HUD text.
        // Brush:   Permanent Marker, an ink-brush hand, for the shouted callouts (DOUBLE CUT!).
        public static Font Display, Body, Brush;

        static void LoadFonts()
        {
            if (Display != null) return;
            Display = Resources.Load<Font>("Fonts/BebasNeue-Regular");
            Body = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
            Brush = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) showDiagnostics = !showDiagnostics;
#else
            if (Input.GetKeyDown(KeyCode.F1)) showDiagnostics = !showDiagnostics;
#endif
            // letterbox bars slide in whenever time slows down
            float want = Time.timeScale < 0.7f ? 1f : 0f;
            bars = Mathf.MoveTowards(bars, want, Time.unscaledDeltaTime * 5f);
        }

        void OnGUI()
        {
            if (big == null) Build();
            // the whole interface is drawn larger than the 720-line design (phones most of all), so
            // text and buttons are comfortable to read and press; layouts use w and h, not 720
            scale = Screen.height / 720f * UiBoost;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            float barK = h / 720f;            // letterbox bars keep their share of the screen

            // film look: vignette and moving grain over everything
            if (vignette == null) BuildFilmTextures();
            GUI.DrawTexture(new Rect(0, 0, w, h), vignette);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.06f);
            float ox = Random.value, oy = Random.value;
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), grain, new Rect(ox, oy, w / 256f, h / 256f));
            GUI.color = old;

            // cinematic bars: slow-mo letterbox, or the opening's (which starts as a black screen)
            var cine = OpeningCinematic.Instance;
            bool cutscene = cine != null && cine.Active;
            float bh = bars > 0.001f ? 70f * barK * Mathf.SmoothStep(0f, 1f, bars) : 0f;
            if (cutscene) bh = Mathf.Max(bh, cine.BarsPixels * barK);
            if (bh > 0.01f)
            {
                Fill(new Rect(0, 0, w, bh), Color.black);
                Fill(new Rect(0, h - bh, w, bh), Color.black);
            }
            if (cutscene) { OpeningCards(cine, w, h); GUI.matrix = Matrix4x4.identity; return; }
            var bd = BossDirector.Instance;
            if (bd != null && bd.Cinematic)
            {
                // the boss's entrance and finale: bars, his cards, nothing else
                float bb = Mathf.Max(bh, bd.BarsPixels * barK);
                if (bb > 0.01f)
                {
                    Fill(new Rect(0, 0, w, bb), Color.black);
                    Fill(new Rect(0, h - bb, w, bb), Color.black);
                }
                BossCards(bd, w, h);
                GUI.matrix = Matrix4x4.identity;
                return;
            }
            if (hero != null && hero.RampageIntro) { RampageIntroOverlay(w, h, barK); GUI.matrix = Matrix4x4.identity; return; }
            var win = VictoryCinematic.Instance;
            if (win != null && win.Active)
            {
                float vb = Mathf.Max(win.BarsPixels, bd != null ? bd.BarsPixels : 0f) * barK;
                if (vb > 0.01f)
                {
                    Fill(new Rect(0, 0, w, vb), Color.black);
                    Fill(new Rect(0, h - vb, w, vb), Color.black);
                }
                if (bd != null && bd.DefeatAlpha > 0.001f) BossCards(bd, w, h);
                else VictoryCards(win, w, h);
                GUI.matrix = Matrix4x4.identity;
                return;
            }

            RampageFrame(w, h);
            DrawStick();
            PunchKickZones(w, h);
            if (hero != null) HeroPanel(w);
            if (director != null) DirectorPanel(w, h);
            if (bd != null) BossBar(bd, w);
            GuideLayer(w, h);
            if (showDiagnostics) Diagnostics();
            GUI.matrix = Matrix4x4.identity;
        }

        GUIStyle cineTitle, cineTag, cineCap, cineSkip;

        /// <summary>Title, tagline, chapter card and skip hint, white on the film.</summary>
        void OpeningCards(OpeningCinematic c, float w, float h)
        {
            if (cineTitle == null)
            {
                LoadFonts();
                cineTitle = new GUIStyle { font = Display, fontSize = 104, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                cineTag = new GUIStyle(cineTitle) { font = Body, fontSize = 22, fontStyle = FontStyle.Normal };
                cineCap = new GUIStyle(cineTitle) { fontSize = 42, alignment = TextAnchor.MiddleLeft };
                cineSkip = new GUIStyle(cineTag) { fontSize = 16, alignment = TextAnchor.MiddleRight };
            }
            var white = Color.white;
            if (c.LogoT >= 0f) Logo(c, w, h);
            if (c.CaptionAlpha > 0.001f)
            {
                // the chapter card sits in the lower bar, left, like a lower third
                float slide = (1f - c.CaptionAlpha) * 30f;
                float y = h - 92f;
                cineCap.normal.textColor = new Color(1f, 1f, 1f, c.CaptionAlpha);
                GUI.Label(new Rect(70 - slide, y + 2, w, 46), c.chapter, cineCap);
                // the rule under it spans the wider of the two lines, drawing out from the left,
                // led by a short red stroke (the ink red of the title and the victory card)
                cineTag.alignment = TextAnchor.MiddleLeft;
                float ruleW = Mathf.Max(cineCap.CalcSize(new GUIContent(c.chapter)).x, cineTag.CalcSize(new GUIContent(Spaced(c.chapterName))).x);
                float draw = ruleW * Mathf.SmoothStep(0f, 1f, c.CaptionAlpha);
                Fill(new Rect(72 - slide, y + 50, draw, 2), new Color(1f, 1f, 1f, 0.8f * c.CaptionAlpha));
                Fill(new Rect(72 - slide, y + 49, Mathf.Min(draw, 36f), 4), new Color(0.78f, 0.06f, 0.07f, c.CaptionAlpha));
                cineTag.normal.textColor = new Color(1f, 1f, 1f, 0.75f * c.CaptionAlpha);
                GUI.Label(new Rect(72 - slide, y + 56, w, 26), Spaced(c.chapterName), cineTag);
                cineTag.alignment = TextAnchor.MiddleCenter;
            }
            if (c.SkipAlpha > 0.001f)
            {
                cineSkip.normal.textColor = new Color(1f, 1f, 1f, c.SkipAlpha);
                GUI.Label(new Rect(0, 24, w - 40, 30), "TAP TO SKIP", cineSkip);
            }
        }


        // ================================================================== the title sequence
        // On black: a blade's glint runs across the middle of the screen; a sword stroke paints a
        // red ink smear; STICK slams in letter by letter (each dropping from large, with a flash, a
        // red split that settles and a shake), then WICK; ash drifts up; a light sweeps the letters;
        // the tagline lands in three beats. Then a cut along the same line slices the title in two,
        // the halves slide apart, and the black splits open onto the building.

        GUIStyle logoSt, logoTag;
        Texture2D inkStroke;
        float[] logoW;
        float logoTotal;
        const string LogoText = "STICK WICK";
        const float LogoGap = 10f, LogoSpace = 46f;
        static readonly float[] Ash = BuildAsh(40);

        static float[] BuildAsh(int n)
        {
            var r = new System.Random(11);
            var a = new float[n * 4];
            for (int i = 0; i < n; i++)
            {
                a[i * 4] = (float)r.NextDouble();               // x
                a[i * 4 + 1] = (float)r.NextDouble();           // phase
                a[i * 4 + 2] = 0.04f + 0.08f * (float)r.NextDouble();   // rise speed (screens/s)
                a[i * 4 + 3] = 1.5f + 2.5f * (float)r.NextDouble();     // size
            }
            return a;
        }

        static float E(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        /// <summary>When each letter lands (the space never does).</summary>
        static float LandAt(int i) => i < 5 ? 0.72f + i * 0.04f : 1.02f + (i - 6) * 0.04f;

        void Logo(OpeningCinematic c, float w, float h)
        {
            float t = c.LogoT;
            EnsureLogo();
            float cx = w * 0.5f, cy = h * 0.5f;             // the cut runs exactly through the middle
            float slice = OpeningCinematic.LogoSlice;
            float sliceK = E(slice, slice + 0.45f, t);       // the halves part
            float fade = 1f - sliceK;

            // ash rising behind it all
            float ashA = E(0.7f, 1.4f, t) * fade;
            if (ashA > 0.01f)
                for (int i = 0; i < Ash.Length / 4; i++)
                {
                    float y = h + 20f - Mathf.Repeat(Ash[i * 4 + 1] * (h + 40f) + t * Ash[i * 4 + 2] * h, h + 40f);
                    float x = Ash[i * 4] * w + Mathf.Sin(t * 1.3f + i) * 12f;
                    float sz = Ash[i * 4 + 3];
                    float tw = 0.45f + 0.55f * Mathf.Sin(t * 3f + i * 1.7f);
                    var col = i % 5 == 0 ? new Color(0.9f, 0.25f, 0.18f) : new Color(0.85f, 0.85f, 0.82f);
                    col.a = ashA * 0.55f * tw;
                    Fill(new Rect(x, y, sz, sz), col);
                }

            // 1. the glint: a hairline drawn across the middle of the black
            if (t < 1.1f)
            {
                float k = E(0.2f, 0.6f, t), a = 1f - E(0.75f, 1.1f, t);
                Fill(new Rect(cx - w * 0.5f * k, cy - 1f, w * k, 2f), new Color(1f, 1f, 1f, 0.9f * a));
                if (glowTex != null) { var o = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.35f * a * k); GUI.DrawTexture(new Rect(cx - 160, cy - 30, 320, 60), glowTex); GUI.color = o; }
            }

            // the whole title leans in very slowly
            float push = 1f + 0.05f * E(0.9f, slice, t);
            var m0 = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(push, push), new Vector2(cx, cy) * scale);
            if (sliceK <= 0f) LogoBody(t, w, cx, cy, 0f, 1f);
            else
            {
                // sliced: the top half slides one way, the bottom the other, both fading
                float dx = 70f * sliceK, dy = 26f * sliceK;
                GUI.BeginGroup(new Rect(-200f, -200f, w + 400f, cy + 200f));
                LogoBody(t, w, cx + 200f - dx, cy + 200f - dy, 0f, fade);
                GUI.EndGroup();
                GUI.BeginGroup(new Rect(-200f, cy, w + 400f, h - cy + 200f));
                LogoBody(t, w, cx + 200f + dx, 0f + dy, 0f, fade);
                GUI.EndGroup();
            }
            GUI.matrix = m0;

            // the stroke that paints the ink: a diagonal slash of light
            SlashLight(t, 0.58f, 0.8f, new Vector2(cx, cy), -14f, w * 1.1f);
            // the cut: a flash along the middle, full width
            if (t > slice - 0.06f && t < slice + 0.35f)
            {
                float k = E(slice - 0.06f, slice + 0.02f, t), a = 1f - E(slice + 0.05f, slice + 0.35f, t);
                Fill(new Rect(cx - w * 0.5f * k, cy - 1.5f, w * k, 3f), new Color(1f, 1f, 1f, a));
                Fill(new Rect(cx - w * 0.5f * k, cy - 6f, w * k, 12f), new Color(1f, 0.3f, 0.2f, 0.25f * a));
            }
            // flashes on each slam and on the cut
            float flash = Mathf.Max(Pulse(t, 0.72f, 0.16f) * 0.55f, Mathf.Max(Pulse(t, 1.02f, 0.12f) * 0.3f, Pulse(t, slice, 0.14f) * 0.45f));
            if (flash > 0.01f) Fill(new Rect(0, 0, w, h), new Color(1f, 1f, 1f, flash));
        }

        void EnsureLogo()
        {
            if (logoSt != null) return;
            LoadFonts();
            logoSt = new GUIStyle { font = Display, fontSize = 150, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            logoTag = new GUIStyle(logoSt) { font = Body, fontSize = 22, fontStyle = FontStyle.Normal };
            logoW = new float[LogoText.Length];
            logoTotal = 0f;
            for (int i = 0; i < LogoText.Length; i++)
            {
                logoW[i] = LogoText[i] == ' ' ? LogoSpace : logoSt.CalcSize(new GUIContent(LogoText[i].ToString())).x;
                logoTotal += logoW[i] + (i > 0 ? LogoGap : 0f);
            }
            inkStroke = InkStrokeTexture(512, 128);
        }

        static float Pulse(float t, float at, float len) => t < at || t > at + len ? 0f : 1f - (t - at) / len;

        /// <summary>The stroke, the letters, the light sweep and the tagline, at (cx, cy).</summary>
        void LogoBody(float t, float w, float cx, float cy, float unused, float alpha)
        {
            // impact shake after each slam
            float shake = 7f * Mathf.Exp(-Mathf.Max(0f, t - 0.72f) * 14f) * (t > 0.72f ? 1f : 0f)
                        + 4f * Mathf.Exp(-Mathf.Max(0f, t - 1.02f) * 14f) * (t > 1.02f ? 1f : 0f);
            float sx = Mathf.Sin(t * 90f) * shake, sy = Mathf.Cos(t * 77f) * shake * 0.6f;
            cx += sx; cy += sy;

            // the red ink stroke, painted left to right behind the letters
            float reveal = E(0.62f, 0.84f, t);
            if (reveal > 0f && inkStroke != null)
            {
                float sw = logoTotal + 150f, shh = 150f;
                var r = new Rect(cx - sw * 0.5f, cy - shh * 0.5f + 6f, sw * reveal, shh);
                var o = GUI.color;
                GUI.color = new Color(0.72f, 0.05f, 0.06f, 0.95f * alpha);
                GUI.DrawTextureWithTexCoords(r, inkStroke, new Rect(0f, 0f, reveal, 1f));
                GUI.color = o;
            }

            // the letters
            float x = cx - logoTotal * 0.5f;
            float split = Mathf.Exp(-Mathf.Max(0f, t - 0.72f) * 7f) * (t > 0.7f ? 1f : 0f);
            for (int i = 0; i < LogoText.Length; i++)
            {
                float lw = logoW[i];
                float lc = x + lw * 0.5f;
                x += lw + LogoGap;
                if (LogoText[i] == ' ') continue;
                float land = LandAt(i);
                float k = Mathf.Clamp01((t - (land - 0.12f)) / 0.12f);
                if (k <= 0f) continue;
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                float sc = Mathf.Lerp(2.4f, 1f, e);
                string ch = LogoText[i].ToString();
                // a motion trail while it drops in
                if (k < 1f)
                    for (int g = 1; g <= 3; g++)
                        Letter(ch, lc, cy, sc * (1f + g * 0.18f), new Color(1f, 1f, 1f, 0.12f * k * alpha));
                // the red split, settling
                float sp = split * 9f;
                if (sp > 0.3f) Letter(ch, lc + sp, cy, sc, new Color(1f, 0.15f, 0.1f, 0.7f * alpha * k));
                logoSt.normal.textColor = new Color(0f, 0f, 0f, 0.6f * k * alpha);
                Letter(ch, lc + 3f, cy + 4f, sc, logoSt.normal.textColor);           // shadow
                Letter(ch, lc, cy, sc, new Color(0.93f, 0.93f, 0.91f, k * alpha));
            }

            // the light sweep: the letters, pure white, seen through a moving slit
            float gk = Mathf.InverseLerp(1.45f, 1.95f, t);
            if (gk > 0f && gk < 1f)
            {
                float left = cx - logoTotal * 0.5f - 60f;
                float stripX = left + (logoTotal + 120f) * gk;
                if (glowTex != null)
                {
                    var o = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.35f * alpha);
                    GUI.DrawTexture(new Rect(stripX - 70f, cy - 90f, 140f, 180f), glowTex); GUI.color = o;
                }
                GUI.BeginGroup(new Rect(stripX - 16f, cy - 120f, 32f, 240f));
                float xx = cx - logoTotal * 0.5f - (stripX - 16f);
                for (int i = 0; i < LogoText.Length; i++)
                {
                    float lw = logoW[i];
                    if (LogoText[i] != ' ') Letter(LogoText[i].ToString(), xx + lw * 0.5f, 120f, 1f, new Color(1f, 1f, 1f, alpha), false);
                    xx += lw + LogoGap;
                }
                GUI.EndGroup();
            }

            // the tagline, in three beats
            string[] parts = { "ONE MAN.", "ONE BUILDING.", "NO WAY DOWN." };
            float[] at = { 1.7f, 1.95f, 2.2f };
            float total = 0f;
            var pw = new float[3];
            for (int i = 0; i < 3; i++) { pw[i] = logoTag.CalcSize(new GUIContent(Spaced(parts[i]))).x; total += pw[i] + (i > 0 ? 42f : 0f); }
            float tx = cx - total * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float k = E(at[i], at[i] + 0.25f, t);
                if (k > 0f)
                {
                    logoTag.normal.textColor = new Color(1f, 1f, 1f, 0.82f * k * alpha);
                    Label(new Rect(tx, cy + 92f + (1f - k) * 8f, pw[i], 30f), Spaced(parts[i]), logoTag);
                }
                tx += pw[i] + 42f;
            }
        }

        /// <summary>One letter, centred at (x, y), scaled about its centre.</summary>
        void Letter(string ch, float x, float y, float s, Color col, bool scaled = true)
        {
            var m = GUI.matrix;
            if (scaled && Mathf.Abs(s - 1f) > 0.001f) GUIUtility.ScaleAroundPivot(new Vector2(s, s), new Vector2(x, y) * scale);
            logoSt.normal.textColor = col;
            GUI.Label(new Rect(x - 80f, y - 100f, 160f, 200f), ch, logoSt);
            GUI.matrix = m;
        }

        /// <summary>A bright slash of light across the screen that shoots out and thins away.</summary>
        void SlashLight(float t, float a, float b, Vector2 c, float angle, float length)
        {
            if (t < a || t > b) return;
            float u = (t - a) / (b - a);
            float len = length * Mathf.Clamp01(u / 0.35f), th = 6f * (1f - u);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, c * scale);
            Fill(new Rect(c.x - len * 0.5f, c.y - th * 0.5f, len, th), new Color(1f, 1f, 1f, 1f - u * u));
            Fill(new Rect(c.x - len * 0.5f, c.y - th * 2f, len, th * 4f), new Color(1f, 1f, 1f, 0.2f * (1f - u)));
            GUI.matrix = m;
        }

        /// <summary>A dry-brush ink smear: a ragged-edged horizontal stroke, streaked by the
        /// bristles, heavy at the start and breaking up towards the end.</summary>
        static Texture2D InkStrokeTexture(int W, int H)
        {
            var r = new System.Random(5);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            var bristle = new float[H];
            for (int y = 0; y < H; y++) bristle[y] = 0.55f + 0.45f * (float)r.NextDouble();
            var gapStart = new float[H];
            for (int y = 0; y < H; y++) gapStart[y] = 0.55f + 0.45f * (float)r.NextDouble();
            float n1 = (float)r.NextDouble() * 10f, n2 = (float)r.NextDouble() * 10f;
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                // the stroke's body: fat after the touch-down, tapering to the lift-off
                float half = 0.40f * Mathf.Sqrt(Mathf.Clamp01(u / 0.06f)) * Mathf.Lerp(1f, 0.55f, u * u);
                float wob = 0.05f * (Mathf.PerlinNoise(u * 6f, n1) - 0.5f);
                float top = 0.5f - half + wob + 0.05f * (Mathf.PerlinNoise(u * 22f, n2) - 0.5f);
                float bot = 0.5f + half + wob + 0.05f * (Mathf.PerlinNoise(u * 22f, n2 + 3f) - 0.5f);
                for (int y = 0; y < H; y++)
                {
                    float v = y / (float)(H - 1);
                    float a = Mathf.Clamp01(Mathf.Min(v - top, bot - v) * 60f);
                    // dry brush: bristles run out of ink towards the end
                    if (u > gapStart[y]) a *= Mathf.Clamp01(1f - (u - gapStart[y]) * 5f);
                    a *= bristle[y];
                    a *= 0.85f + 0.15f * Mathf.PerlinNoise(u * 40f, v * 40f);
                    px[y * W + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        GUIStyle zoneStyle;

        /// <summary>Faint guides at the screen edges: tap above the line to punch, below to kick.</summary>
        void PunchKickZones(float w, float h)
        {
            var gi = GestureInput.Instance;
            if (gi == null) return;
            if (zoneStyle == null) zoneStyle = new GUIStyle { font = Body, fontSize = 20, alignment = TextAnchor.MiddleCenter };
            float y = h * (1f - gi.kickLine);
            // stronger for the first floor, then barely there
            // only while learning: the first wave of the first floor, fading out
            float a = director != null && director.Floor == 1 && director.WaveIndex == 0
                ? 0.3f * Mathf.Clamp01(1f - (director.ElapsedTime - 10f) / 3f) : 0f;
            if (a <= 0.01f) return;
            float left = gi.Classic ? 0f : w * gi.stickZone;
            foreach (float x in new[] { left + 8f, w - 72f })
            {
                if (!gi.Classic && x < w * 0.5f) continue;
                Fill(new Rect(x, y, 64f, 1.5f), new Color(1f, 1f, 1f, a));
                zoneStyle.normal.textColor = new Color(1f, 1f, 1f, a * 1.4f);
                GUI.Label(new Rect(x, y - 28f, 64f, 22f), "PUNCH", zoneStyle);
                GUI.Label(new Rect(x, y + 6f, 64f, 22f), "KICK", zoneStyle);
            }
        }

        Texture2D ring, knob;

        /// <summary>The floating movement stick, drawn where the left thumb is.</summary>
        void DrawStick()
        {
            var gi = GestureInput.Instance;
            if (gi == null || gi.Classic) return;
            if (ring == null) { ring = Disc(96, true); knob = Disc(64, false); }
            var old = GUI.color;
            if (gi.StickActive)
            {
                Vector2 o = new Vector2(gi.StickOrigin.x / scale, (Screen.height - gi.StickOrigin.y) / scale);
                Vector2 n = new Vector2(gi.StickNow.x / scale, (Screen.height - gi.StickNow.y) / scale);
                Vector2 d = n - o;
                if (d.magnitude > 70f) d = d.normalized * 70f;
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(new Rect(o.x - 80, o.y - 80, 160, 160), ring);
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.DrawTexture(new Rect(o.x + d.x - 36, o.y + d.y - 36, 72, 72), knob);
            }
            else
            {
                // a faint resting stick in the corner, so players know the left side moves
                GUI.color = new Color(1f, 1f, 1f, 0.12f);
                float hh = Screen.height / scale;
                GUI.DrawTexture(new Rect(70, hh - 230, 160, 160), ring);
                GUI.DrawTexture(new Rect(114, hh - 186, 72, 72), knob);
            }
            GUI.color = old;
        }

        static Texture2D Disc(int size, bool hollow)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = hollow ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) * 18f) : Mathf.Clamp01((1f - d) * 12f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        Texture2D vignette, grain;

        void BuildFilmTextures()
        {
            vignette = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var v = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    v[y * 128 + x] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01((d - 0.7f) / 0.55f) * 105));   // soft: never hides a fighter at the edge
                }
            vignette.SetPixels32(v);
            vignette.Apply();
            grain = new Texture2D(256, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var g = new Color32[256 * 256];
            var rng = new System.Random(3);
            for (int i = 0; i < g.Length; i++) { byte b = (byte)rng.Next(0, 256); g[i] = new Color32(b, b, b, 255); }
            grain.SetPixels32(g);
            grain.Apply();
        }

        static readonly Color RampageRed = new Color(0.86f, 0.08f, 0.1f);
        bool wasReady;

        /// <summary>While the rampage runs: a red frame breathing at the screen's edge, and a big
        /// draining RAMPAGE bar at the top.</summary>
        void RampageFrame(float w, float h)
        {
            if (hero == null || !hero.InRampage || hero.RampageIntro) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            var old = GUI.color;
            GUI.color = new Color(RampageRed.r, RampageRed.g, RampageRed.b, 0.35f + 0.2f * pulse);
            if (vignette != null) GUI.DrawTexture(new Rect(-40, -40, w + 80, h + 80), vignette);
            GUI.color = old;
            float frac = Mathf.Clamp01(hero.RampageLeft / Mathf.Max(0.1f, hero.rampageSeconds));
            var bar = new Rect(w * 0.5f - 230, 66, 460, 10);
            Fill(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(bar.center.x - bar.width * frac * 0.5f, bar.y, bar.width * frac, bar.height), Color.Lerp(RampageRed, Color.white, pulse * 0.4f));
            var st = new GUIStyle(big) { fontSize = 30 };
            st.normal.textColor = new Color(1f, 1f, 1f, 0.95f);
            Label(new Rect(0, 78, w, 40), Spaced("RAMPAGE"), st);
        }

        /// <summary>The activation: bars snap in, RAMPAGE slams onto the screen in blood red brush
        /// letters with a white swipe under it, and fades as the shockwave hits.</summary>
        void RampageIntroOverlay(float w, float h, float barK)
        {
            float t = hero.RampageIntroT;
            float bars = 96f * barK * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.08f)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.85f) / 0.15f)));
            Fill(new Rect(0, 0, w, bars), Color.black);
            Fill(new Rect(0, h - bars, w, bars), Color.black);
            LoadFonts();
            float slam = Mathf.Clamp01(t / 0.16f);
            float size = Mathf.Lerp(2.2f, 1f, 1f - (1f - slam) * (1f - slam));
            float alpha = t < 0.8f ? 1f : Mathf.Clamp01(1f - (t - 0.8f) / 0.2f);
            var st = new GUIStyle { font = Brush != null ? Brush : Display, fontSize = Mathf.RoundToInt(118 * size), alignment = TextAnchor.MiddleCenter };
            float shake = slam < 1f ? 0f : Mathf.Sin(Time.unscaledTime * 90f) * 3f * Mathf.Clamp01(1f - (t - 0.16f) / 0.3f);
            var r = new Rect(shake, h * 0.30f, w, 160);
            st.normal.textColor = new Color(0f, 0f, 0f, 0.7f * alpha);
            GUI.Label(new Rect(r.x + 5, r.y + 6, r.width, r.height), "RAMPAGE", st);
            st.normal.textColor = new Color(RampageRed.r, RampageRed.g, RampageRed.b, alpha);
            GUI.Label(r, "RAMPAGE", st);
            // the white swipe under it, drawn out left to right
            float sw = Mathf.Clamp01((t - 0.1f) / 0.2f) * 560f;
            Fill(new Rect(w * 0.5f - 280, h * 0.30f + 150, sw, 4), new Color(1f, 1f, 1f, 0.9f * alpha));
        }

        void HeroPanel(float w)
        {
            // health
            // a hairline health bar: white, turning red and pulsing when low
            float hp = Mathf.Clamp01(hero.Health / hero.maxHealth);
            Rect bar = new Rect(24, 24, 300, 6);
            Fill(new Rect(bar.x - 1, bar.y - 1, bar.width + 2, bar.height + 2), new Color(0f, 0f, 0f, 0.45f));
            Fill(bar, new Color(1f, 1f, 1f, 0.12f));
            Color hc = hp > 0.3f ? new Color(0.95f, 0.95f, 0.95f) : Color.Lerp(new Color(0.85f, 0.1f, 0.12f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f));
            Fill(new Rect(bar.x, bar.y, bar.width * hp, bar.height), hc);
            // flow
            if (director != null)
            {
                Rect fb = new Rect(24, 36, 300, 3);
                Fill(fb, new Color(1f, 1f, 1f, 0.08f));
                bool ready = director.FlowReady;
                float pulse = ready ? 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
                Fill(new Rect(fb.x, fb.y, fb.width * director.Flow / 100f, fb.height), new Color(1f, 1f, 1f, pulse));
                var lbl = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 20 };
                if (ready && !hero.InRampage)
                {
                    // charged: quietly, a solid red bar and a small label; it only calls out in trouble
                    bool trouble = hero.InTrouble;
                    float k = trouble ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f) : 0f;
                    Fill(new Rect(fb.x, fb.y - 1, fb.width, fb.height + 2), Color.Lerp(RampageRed, Color.white, k));
                    lbl.normal.textColor = trouble ? Color.Lerp(RampageRed, Color.white, 0.4f * k) : new Color(RampageRed.r, RampageRed.g, RampageRed.b, 0.85f);
                    var small2 = new GUIStyle(lbl) { fontSize = 15 };
                    Label(new Rect(24, 44, 400, 22), "R A G E   C H A R G E D", small2);
                }
            }
            // weapon in hand
            string weapon = hero.Style switch
            {
                FightStyle.Sword => $"SWORD  {hero.SwordUses}",
                FightStyle.Gun => $"GUN  {hero.Ammo}",
                FightStyle.Pipe => $"PIPE  {hero.PipeUses}",
                FightStyle.Knife => $"KNIVES  {hero.Knives}",
                _ => "FISTS",
            };
            if (hero.IsGrabbed)
            {
                var g = new GUIStyle(big) { fontSize = 62 };
                g.normal.textColor = new Color(0.9f, 0.1f, 0.1f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 18f));
                GUI.Label(new Rect(0, 200, w, 60), $"TAP TAP TAP!  {hero.GrabTaps}/4", g);
            }
            Label(new Rect(24, 70, 300, 30), Spaced(weapon), new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 20 });
            if (hero.ComboCount >= 2)
            {
                var cs = new GUIStyle(mid) { alignment = TextAnchor.UpperLeft, fontSize = 52 };
                Label(new Rect(24, 92, 300, 56), hero.ComboCount.ToString(), cs);
                var hs = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 18 };
                Label(new Rect(30 + 21 * hero.ComboCount.ToString().Length, 120, 200, 22), "H I T S", hs);
            }
        }

        void DirectorPanel(float w, float h)
        {
            Label(new Rect(w - 324, 8, 300, 48), director.Score.ToString("N0"), new GUIStyle(mid) { alignment = TextAnchor.UpperRight, fontSize = 44 });
            Label(new Rect(w - 324, 54, 300, 22), $"FLOOR {director.Floor}   ·   WAVE {Mathf.Min(director.WaveIndex + 1, director.WaveCount)} / {director.WaveCount}",
                  new GUIStyle(small) { alignment = TextAnchor.UpperRight, fontSize = 18 });

            StyleRank(w);

            if (Time.unscaledTime < director.BannerUntil && !string.IsNullOrEmpty(director.Banner))
                Label(new Rect(0, h * 0.2f, w, 90), Spaced(director.Banner), new GUIStyle(big) { fontSize = 72 });
            if (Time.unscaledTime < director.StyleUntil && !string.IsNullOrEmpty(director.StyleText))
            {
                float k = Mathf.Clamp01((director.StyleUntil - Time.unscaledTime) / 0.3f);
                var st = new GUIStyle(mid) { font = Brush != null ? Brush : Display, fontSize = Brush != null ? 30 : 34 };
                st.normal.textColor = new Color(1f, 1f, 1f, k);
                Label(new Rect(0, h * 0.32f, w, 50), Brush != null ? director.StyleText : Spaced(director.StyleText), st);
            }

            // anyone the shot could not hold: an arrow in their colour on the screen edge, pointing at them
            var cam = Camera.main;
            if (cam != null)
            {
                if (chevron == null) chevron = ChevronTexture(64);
                foreach (var e in Enemy.All)
                {
                    if (!e.Alive || e.Fleeing) continue;
                    var vp = cam.WorldToViewportPoint(e.Chest);
                    if (vp.z > 0f && vp.x > -0.01f && vp.x < 1.01f && vp.y > -0.01f && vp.y < 1.01f) continue;
                    if (vp.z <= 0f) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }
                    // the point on a slightly inset screen rectangle in their direction
                    Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                    float m = Mathf.Max(Mathf.Abs(d.x) / 0.46f, Mathf.Abs(d.y) / 0.40f);
                    Vector2 q = new Vector2(0.5f, 0.5f) + d / Mathf.Max(m, 1e-3f);
                    float px = q.x * w, py = (1f - q.y) * h;
                    float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;          // GUI y points down
                    float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f + e.transform.position.x);
                    var oldC = GUI.color; var oldM = GUI.matrix;
                    GUI.color = new Color(1f, 1f, 1f, 0.35f * pulse);
                    GUI.DrawTexture(new Rect(px - 34, py - 34, 68, 68), glowTex);
                    GUIUtility.RotateAroundPivot(ang, new Vector2(px, py) * scale);
                    GUI.color = new Color(1f, 1f, 1f, 0.9f);
                    GUI.DrawTexture(new Rect(px - 17, py - 17, 34, 34), chevron);      // white rim
                    var ec = e.InkColor; ec.a = pulse;
                    GUI.color = ec;
                    GUI.DrawTexture(new Rect(px - 13, py - 13, 26, 26), chevron);      // their colour
                    GUI.matrix = oldM; GUI.color = oldC;
                }
            }

            // incoming enemies: a soft red glow breathing where they will come in: round the doorway
            // if it is in shot, otherwise on the screen edge nearest it
            if (cam != null)
                foreach (var inc in director.Upcoming)
                {
                    if (Time.time < inc.time) continue;
                    float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f);
                    var c = Enemy.TelegraphColor; c.a = 0.55f * pulse;
                    var sp = director.spawnPoints[(int)inc.kind];
                    if (sp == null) continue;
                    Vector3 wp = sp.position + Vector3.up * 1.0f;
                    if (inc.kind == SpawnKind.Ceiling && hero != null) wp = new Vector3(hero.transform.position.x, sp.position.y, 0f);
                    var vp = cam.WorldToViewportPoint(wp);
                    if (vp.z > 0f && vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.08f && vp.y < 0.92f)
                    {
                        var old = GUI.color;
                        GUI.color = c;
                        float gx = vp.x * w, gy = (1f - vp.y) * h;
                        GUI.DrawTexture(new Rect(gx - 70, gy - 90, 140, 180), glowTex);
                        GUI.color = old;
                    }
                    else
                    {
                        float ex = Mathf.Clamp01(vp.x), ey = Mathf.Clamp(1f - vp.y, 0.1f, 0.9f) * h;
                        if (vp.z <= 0f || ex <= 0.06f) EdgeGlow(new Rect(0, ey - 90, 70, 180), c, 0f);
                        else if (ex >= 0.94f) EdgeGlow(new Rect(w - 70, ey - 90, 70, 180), c, 180f);
                        else if (vp.y >= 0.92f) EdgeGlow(new Rect(ex * w - 120, 0, 240, 60), c, 90f);
                        else EdgeGlow(new Rect(ex * w - 120, h - 60, 240, 60), c, 270f);
                    }
                }

            // (the controls are taught by the training reel and listed on the ? card, not along the bottom)
            if (director.CurrentPhase == WaveDirector.Phase.Dead) Results(w, h);
        }

        void Results(float w, float h)
        {
            Rect panel = new Rect(w * 0.5f - 280, h * 0.26f, 560, 320);
            Fill(panel, new Color(0f, 0f, 0f, 0.82f));
            Fill(new Rect(panel.x, panel.y, panel.width, 1), new Color(1f, 1f, 1f, 0.5f));
            Fill(new Rect(panel.x, panel.yMax - 1, panel.width, 1), new Color(1f, 1f, 1f, 0.5f));
            int best = Mathf.Clamp(director.BestRankIndex, 0, RankWords.Length - 1);
            Label(new Rect(panel.x, panel.y + 18, panel.width, 20), "B E S T   S T Y L E", new GUIStyle(small) { fontSize = 15 });
            Label(new Rect(panel.x, panel.y + 36, panel.width, 100), director.Grade(), new GUIStyle(big) { fontSize = 124 });
            Label(new Rect(panel.x, panel.y + 132, panel.width, 22), Spaced(RankWords[best]), new GUIStyle(small) { fontSize = 17 });
            Fill(new Rect(w * 0.5f - 40, panel.y + 164, 80, 1), new Color(1f, 1f, 1f, 0.4f));
            Label(new Rect(panel.x, panel.y + 176, panel.width, 40), director.Score.ToString("N0") + (director.NewBest ? "   ·   NEW BEST" : ""), mid);
            Label(new Rect(panel.x, panel.y + 220, panel.width, 26),
                $"Floor {director.Floor}   ·   {director.Kills} kills   ·   best {director.BestScore:N0} (floor {director.BestFloor})", new GUIStyle(small) { fontSize = 18 });
            var tap = new GUIStyle(small) { fontSize = 17 };
            tap.normal.textColor = new Color(1f, 1f, 1f, 0.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f));
            GUI.Label(new Rect(panel.x, panel.y + 276, panel.width, 26), "T A P   T O   F I G H T   A G A I N", tap);
        }

        // ------------------------------------------------------------------ the premium bits

        static readonly string[] RankWords = { "", "CLEAN", "BRUTAL", "ARTFUL", "SAVAGE", "SUPREME", "STICK WICK" };
        int shownRank = -1;
        float rankChangedAt, rankAlpha;

        /// <summary>The style rank: hidden at D; from C up, a white letter with a soft glow,
        /// its word in spaced capitals, and a hairline that fills towards the next rank.
        /// A new rank arrives with a quick scale-in.</summary>
        void StyleRank(float w)
        {
            int ri = director.RankIndex;
            if (ri != shownRank) { if (ri > shownRank) rankChangedAt = Time.unscaledTime; shownRank = ri; }
            rankAlpha = Mathf.MoveTowards(rankAlpha, ri >= 1 ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            if (rankAlpha <= 0.01f) return;
            float since = Time.unscaledTime - rankChangedAt;
            float pop = since < 0.25f ? Mathf.Lerp(1.6f, 1f, Mathf.SmoothStep(0f, 1f, since / 0.25f)) : 1f;
            var center = new Vector2(w - 84, 120);
            float size = 84 + ri * 4;
            // glow behind the letter, brighter at the top ranks
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, rankAlpha * (0.08f + 0.05f * ri) * pop);
            float gs = 150 * pop;
            GUI.DrawTexture(new Rect(center.x - gs * 0.5f, center.y - gs * 0.5f, gs, gs), glowTex);
            GUI.color = old;
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), center * scale);
            var ls = new GUIStyle(big) { fontSize = (int)size, alignment = TextAnchor.MiddleCenter };
            ls.normal.textColor = new Color(1f, 1f, 1f, rankAlpha);
            Label(new Rect(center.x - 100, center.y - 44, 200, 80), director.Rank, ls);
            GUI.matrix = m;
            var ws = new GUIStyle(small) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            ws.normal.textColor = new Color(1f, 1f, 1f, 0.75f * rankAlpha);
            Label(new Rect(center.x - 100, center.y + 36, 200, 16), Spaced(RankWords[ri]), ws);
            Fill(new Rect(center.x - 50, center.y + 56, 100, 1), new Color(1f, 1f, 1f, 0.15f * rankAlpha));
            Fill(new Rect(center.x - 50, center.y + 56, 100 * director.RankProgress, 1), new Color(1f, 1f, 1f, 0.8f * rankAlpha));
        }

        Texture2D glowTex, edgeTex, chevron;

        /// <summary>A soft-edged arrow pointing right (rotated into place when drawn).</summary>
        static Texture2D ChevronTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)(n - 1), v = y / (float)(n - 1) - 0.5f;
                    // a solid triangle: tip at the right, base at u = 0.15
                    float halfH = (0.92f - u) * 0.62f;
                    float a = Mathf.Clamp01(Mathf.Min((u - 0.12f) * 30f, (halfH - Mathf.Abs(v)) * 30f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>A soft glow on a screen edge; `angle` turns the gradient (0 = fades rightwards from the left edge).</summary>
        void EdgeGlow(Rect r, Color c, float angle)
        {
            var old = GUI.color;
            var m = GUI.matrix;
            GUI.color = c;
            if (angle == 0f) GUI.DrawTexture(r, edgeTex);
            else
            {
                // draw the left-edge gradient rotated into place
                Vector2 pivot = r.center;
                bool sideways = Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) < 1f;
                Rect rr = sideways ? new Rect(pivot.x - r.height * 0.5f, pivot.y - r.width * 0.5f, r.height, r.width) : r;
                GUIUtility.RotateAroundPivot(angle, pivot * scale);
                GUI.DrawTexture(rr, edgeTex);
            }
            GUI.matrix = m;
            GUI.color = old;
        }

        /// <summary>Text with a soft drop shadow.</summary>
        static void Label(Rect r, string text, GUIStyle st)
        {
            var col = st.normal.textColor;
            st.normal.textColor = new Color(0f, 0f, 0f, 0.6f * col.a);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, st);
            st.normal.textColor = col;
            GUI.Label(r, text, st);
        }

        static string Spaced(string s) => string.IsNullOrEmpty(s) ? s : string.Join(" ", s.ToCharArray());

        // ================================================================== the guide
        // The training reel's lesson card and ghost finger, the chalk marks in the world, the
        // SKIP and ? buttons, and the help card (paused) with REPLAY TRAINING.

        GUIStyle gTitle, gLine, gNice, gChalk, gBtn;
        Texture2D finger, gRing;
        bool helpOpen, pauseOpen;
        Rect helpBtn, skipBtn, skipMoveBtn, replayBtn, closeBtn, pauseBtn, resumeBtn, movesBtn, soundBtn, restartBtn, hintsBtn;

        /// <summary>Open or close the pause screen (and stop the world, and its sound).</summary>
        void SetPause(bool on)
        {
            pauseOpen = on;
            if (!on) helpOpen = false;
            TimeController.Paused = pauseOpen || helpOpen;
            AudioListener.pause = TimeController.Paused;
        }

        // on a phone, leaving the app (a call, a notification) pauses the fight
        void OnApplicationPause(bool paused) { if (paused && Application.isMobilePlatform && !TimeController.Paused) SetPause(true); }
        void OnApplicationFocus(bool focus) { if (!focus && Application.isMobilePlatform && !TimeController.Paused) SetPause(true); }
        void OnDestroy() { TimeController.Paused = false; AudioListener.pause = false; }

        void GuideStyles()
        {
            if (gTitle != null) return;
            LoadFonts();
            gTitle = new GUIStyle { font = Display, fontSize = 40, alignment = TextAnchor.MiddleCenter };
            gTitle.normal.textColor = Color.white;
            gLine = new GUIStyle(gTitle) { font = Body, fontSize = 22 };
            gNice = new GUIStyle(gTitle) { font = Brush != null ? Brush : Display, fontSize = 46 };
            gChalk = new GUIStyle(gTitle) { font = Brush != null ? Brush : Body, fontSize = 24 };
            gBtn = new GUIStyle(gTitle) { font = Body, fontSize = 17 };
            finger = Disc(64, false);
            gRing = Disc(96, true);
        }

        /// <summary>A GUI rect (720-high space) as a screen-pixel rect, origin bottom-left (touch space).</summary>
        Rect ToScreen(Rect r) => new Rect(r.x * scale, Screen.height - (r.y + r.height) * scale, r.width * scale, r.height * scale);

        bool Pressed(Rect r)
        {
            var t = GestureInput.LastButtonTap;
            if (!t.HasValue || !ToScreen(r).Contains(t.Value)) return false;
            GestureInput.LastButtonTap = null;
            return true;
        }

        void GuideLayer(float w, float h)
        {
            GuideStyles();
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) GestureInput.Blocked.Clear();
            var tut = TutorialDirector.Instance;
            bool training = tut != null && tut.Active;

            ChalkMarks(w, h);
            if (training) TrainingCard(tut, w, h);
            RampageButton(w, h, repaint);

            // the pause and ? buttons (top centre), and SKIP during training
            pauseBtn = new Rect(w * 0.5f - 70, 8, 62, 48);
            helpBtn = new Rect(w * 0.5f + 8, 8, 62, 48);
            DrawButton(pauseBtn, "", 26);
            Fill(new Rect(pauseBtn.center.x - 10, pauseBtn.y + 12, 7, 24), new Color(1f, 1f, 1f, 0.92f));    // the II icon
            Fill(new Rect(pauseBtn.center.x + 3, pauseBtn.y + 12, 7, 24), new Color(1f, 1f, 1f, 0.92f));
            DrawButton(helpBtn, "?", 32);
            if (repaint) { GestureInput.Blocked.Add(ToScreen(pauseBtn)); GestureInput.Blocked.Add(ToScreen(helpBtn)); }
            if (training && !tut.Finale)
            {
                // top middle, under pause and ?, clear of the tap zones at the sides
                skipMoveBtn = new Rect(w * 0.5f - 212, 64, 202, 42);
                skipBtn = new Rect(w * 0.5f + 10, 64, 202, 42);
                DrawButton(skipMoveBtn, "SKIP THIS MOVE  >", 19);
                DrawButton(skipBtn, "SKIP TRAINING  >>", 19);
                if (tut.Stuck)
                {
                    // taking a while: the way on lights up
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                    var r = skipMoveBtn;
                    var c = new Color(1f, 1f, 1f, 0.35f + 0.5f * pulse);
                    Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, 2), c); Fill(new Rect(r.x - 2, r.yMax, r.width + 4, 2), c);
                    Fill(new Rect(r.x - 2, r.y, 2, r.height), c); Fill(new Rect(r.xMax, r.y, 2, r.height), c);
                }
                if (repaint) { GestureInput.Blocked.Add(ToScreen(skipBtn)); GestureInput.Blocked.Add(ToScreen(skipMoveBtn)); }
                if (repaint && Pressed(skipBtn)) tut.End(false);
                else if (repaint && Pressed(skipMoveBtn)) tut.SkipLesson();
            }
            bool hKey = false, pKey = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (repaint && kb != null)
            {
                hKey = kb.hKey.wasPressedThisFrame || (helpOpen && kb.escapeKey.wasPressedThisFrame);
                pKey = kb.pKey.wasPressedThisFrame || (!helpOpen && kb.escapeKey.wasPressedThisFrame);
            }
#else
            if (repaint)
            {
                hKey = Input.GetKeyDown(KeyCode.H) || (helpOpen && Input.GetKeyDown(KeyCode.Escape));
                pKey = Input.GetKeyDown(KeyCode.P) || (!helpOpen && Input.GetKeyDown(KeyCode.Escape));
            }
#endif
            if (repaint)
            {
                if (Pressed(pauseBtn) || pKey) SetPause(!pauseOpen);
                else if (Pressed(helpBtn) || hKey)
                {
                    helpOpen = !helpOpen;
                    TimeController.Paused = pauseOpen || helpOpen;
                    AudioListener.pause = TimeController.Paused;
                }
            }
            if (helpOpen) HelpCard(w, h, repaint);
            else if (pauseOpen) PauseCard(w, h, repaint);
        }

        Rect rampageBtn;
        float readySince, troubleUntil;

        /// <summary>Upper left, just under the Flow bar it belongs to: clear of the hero (the camera
        /// keeps him mid-screen) and of the thumbs' usual fighting area.</summary>
        Vector2 RampageButtonCentre(float w, float h) => new Vector2(96f, 236f);

        /// <summary>When the Flow bar is full: a big round RAMPAGE button at the bottom centre,
        /// pulsing, with rings rippling out; one tap and the rampage starts. The moment it becomes
        /// ready there is a call-out, a hit, and a few heartbeats.</summary>
        void RampageButton(float w, float h, bool repaint)
        {
            if (director == null || hero == null) return;
            // the button shows only when it is charged AND the moment calls for it (or in training),
            // and lingers 3 s after the danger, so it never flickers
            var tut = TutorialDirector.Instance;
            bool training = tut != null && tut.Active;
            bool charged = director.FlowReady && !hero.InRampage && !hero.IsDead && !TimeController.Paused;
            if (repaint && charged && (hero.InTrouble || training)) troubleUntil = Time.unscaledTime + 3f;
            bool ready = charged && Time.unscaledTime < troubleUntil;
            if (repaint && ready && !wasReady)
            {
                readySince = Time.unscaledTime;
                Sound.Play(Sfx.CineHit, 0.5f);
                Sound.PlayLater(Sfx.Boom, 0.35f, 0.35f);         // one heartbeat
            }
            if (repaint) wasReady = ready;
            if (!ready) return;
            float t = Time.unscaledTime;
            float pop = Mathf.Clamp01((t - readySince) / 0.3f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 6f);
            float R = 58f * Mathf.Lerp(1.4f, 1f, pop) * (1f + 0.04f * pulse);
            var c = RampageButtonCentre(w, h);
            var old = GUI.color;
            GUI.color = new Color(RampageRed.r, RampageRed.g, RampageRed.b, 0.45f + 0.25f * pulse);
            GUI.DrawTexture(new Rect(c.x - R * 2.1f, c.y - R * 2.1f, R * 4.2f, R * 4.2f), glowTex);
            for (int i = 0; i < 2; i++)
            {
                float u = Mathf.Repeat(t * 0.9f + i * 0.5f, 1f);
                GUI.color = new Color(1f, 1f, 1f, 0.5f * (1f - u));
                float rr = R * Mathf.Lerp(1f, 1.9f, u);
                GUI.DrawTexture(new Rect(c.x - rr, c.y - rr, rr * 2, rr * 2), gRing);
            }
            GUI.color = new Color(0.06f, 0.06f, 0.06f, 0.92f);
            GUI.DrawTexture(new Rect(c.x - R, c.y - R, R * 2, R * 2), finger);
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(c.x - R - 3, c.y - R - 3, R * 2 + 6, R * 2 + 6), gRing);
            GUI.color = old;
            var st = new GUIStyle(gTitle) { fontSize = 30 };
            st.normal.textColor = Color.Lerp(RampageRed, Color.white, 0.35f + 0.35f * pulse);
            GUI.Label(new Rect(c.x - R, c.y - 22, R * 2, 34), "RAMPAGE", st);
            var sub = new GUIStyle(gLine) { fontSize = 15 };
            sub.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(c.x - R, c.y + 10, R * 2, 22), "TAP", sub);
            rampageBtn = new Rect(c.x - R, c.y - R, R * 2, R * 2);
            if (repaint)
            {
                GestureInput.Blocked.Add(ToScreen(rampageBtn));
                if (Pressed(rampageBtn)) hero.TriggerRampage();
            }
        }

        void DrawButton(Rect r, string text, int size)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(1f, 1f, 1f, 0.5f));
            var st = new GUIStyle(gBtn) { fontSize = size };
            st.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            GUI.Label(r, text, st);
        }

        /// <summary>The lesson card in the lower band, the ghost finger showing the gesture where
        /// it is made, the run marker on the floor, NICE. on success, and the finale card.</summary>
        void TrainingCard(TutorialDirector tut, float w, float h)
        {
            if (tut.Finale)
            {
                Fill(new Rect(0, h * 0.30f, w, 170), new Color(0f, 0f, 0f, 0.6f));
                var t1 = new GUIStyle(gTitle) { fontSize = 64 };
                Label(new Rect(0, h * 0.30f + 14, w, 80), Spaced("THE BUILDING IS YOUR WEAPON"), t1);
                Label(new Rect(0, h * 0.30f + 98, w, 40), "ledges, ropes, crates, edges, glass... look for the chalk marks", gChalk);
                return;
            }
            var L = tut.Current;
            // lower band card
            var card = new Rect(w * 0.5f - 330, h - 150, 660, 112);
            Fill(card, new Color(0f, 0f, 0f, 0.62f));
            Fill(new Rect(card.x, card.y, card.width, 1), new Color(1f, 1f, 1f, 0.4f));
            var head = new GUIStyle(gLine) { fontSize = 17 };
            head.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(card.x, card.y + 6, card.width, 20), $"T R A I N I N G   ·   {tut.Index + 1} / {tut.Count}", head);
            Label(new Rect(card.x, card.y + 22, card.width, 48), L.title, gTitle);
            Label(new Rect(card.x, card.y + 66, card.width, 28), L.line, gLine);
            // progress pips
            for (int i = 0; i < L.goal && L.goal > 1; i++)
                Fill(new Rect(card.center.x - L.goal * 9 + i * 18, card.yMax - 12, 12, 4), i < tut.Progress ? Color.white : new Color(1f, 1f, 1f, 0.25f));

            if (Time.unscaledTime < tut.NiceUntil)
            {
                float k = Mathf.Clamp01((tut.NiceUntil - Time.unscaledTime) / 0.9f);
                gNice.normal.textColor = new Color(1f, 1f, 1f, k);
                Label(new Rect(0, h * 0.38f - (1f - k) * 20f, w, 70), "NICE.", gNice);
                return;
            }
            GhostFinger(L.gesture, w, h);

            // the run lesson's mark on the floor
            if (tut.Marker.HasValue && Camera.main != null)
            {
                var vp = Camera.main.WorldToViewportPoint(tut.Marker.Value);
                if (vp.z > 0f)
                {
                    float x = vp.x * w, y = (1f - vp.y) * h;
                    float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
                    var old = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.8f * pulse);
                    GUI.DrawTexture(new Rect(x - 40, y - 14, 80, 28), gRing);
                    GUI.color = old;
                    Label(new Rect(x - 80, y - 60, 160, 36), "HERE", gChalk);
                }
            }
        }

        /// <summary>A translucent fingertip performing the gesture, in the part of the screen
        /// where it is made: taps ripple, holds fill a ring, swipes trail, two fingers pair up.</summary>
        void GhostFinger(Gesture g, float w, float h)
        {
            if (g == Gesture.None) return;
            float t = Time.unscaledTime;
            float cyc = Mathf.Repeat(t, 1.4f) / 1.4f;            // one demonstration every 1.4 s
            Vector2 a, b;
            // taps are shown on the side the stunt double is on, relative to the hero (the side of
            // the hero you touch picks the direction, not the half of the screen)
            var tutor = TutorialDirector.Instance;
            int tside = tutor != null ? tutor.TargetSide : 1;
            float tapX = tside > 0 ? w * 0.76f : w * 0.24f;
            if (hero != null && Camera.main != null)
            {
                float hx = Camera.main.WorldToScreenPoint(hero.Chest).x / scale;
                tapX = tside > 0 ? Mathf.Min(w - 40f, Mathf.Max(hx + 90f, w * 0.62f))
                                 : Mathf.Max(40f, Mathf.Min(hx - 90f, w * 0.38f));
            }
            switch (g)
            {
                case Gesture.TapHigh: a = b = new Vector2(tapX, h * 0.30f); break;
                case Gesture.TapLow: a = b = new Vector2(tapX, h * 0.70f); break;
                case Gesture.TapAgain: a = b = new Vector2(tapX, Mathf.Repeat(t, 0.7f) < 0.35f ? h * 0.30f : h * 0.70f); cyc = Mathf.Repeat(t, 0.35f) / 0.35f; break;
                case Gesture.Hold: a = b = new Vector2(w * 0.22f, h * 0.55f); break;
                case Gesture.SwipeUp: a = new Vector2(w * 0.78f, h * 0.66f); b = a + new Vector2(0f, -170f); break;
                case Gesture.SwipeDown: a = new Vector2(w * 0.78f, h * 0.34f); b = a + new Vector2(0f, 170f); break;
                case Gesture.SwipeSide: a = new Vector2(w * 0.64f, h * 0.5f); b = a + new Vector2(200f, 0f); break;
                case Gesture.TapRampage: a = b = RampageButtonCentre(w, h); break;
                default: a = new Vector2(w * 0.66f, h * 0.48f); b = new Vector2(w * 0.80f, h * 0.48f); break;   // two fingers
            }
            var old = GUI.color;
            bool swipe = g == Gesture.SwipeUp || g == Gesture.SwipeDown || g == Gesture.SwipeSide;
            if (swipe)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cyc / 0.6f));
                float alpha = cyc < 0.75f ? 0.85f : Mathf.Lerp(0.85f, 0f, (cyc - 0.75f) / 0.25f);
                for (int i = 0; i < 8; i++)                      // the trail
                {
                    float u = Mathf.Max(0f, k - i * 0.05f);
                    var q = Vector2.Lerp(a, b, u);
                    GUI.color = new Color(1f, 1f, 1f, alpha * 0.35f * (1f - i / 8f));
                    GUI.DrawTexture(new Rect(q.x - 18, q.y - 18, 36, 36), finger);
                }
                var p = Vector2.Lerp(a, b, k);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(p.x - 24, p.y - 24, 48, 48), finger);
                // the arrowhead at the end
                if (chevron == null) chevron = ChevronTexture(64);
                float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, b * scale);
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTexture(new Rect(b.x - 16, b.y - 16, 32, 32), chevron);
                GUI.matrix = m;
            }
            else if (g == Gesture.Hold)
            {
                float k = Mathf.Clamp01(cyc / 0.8f);
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.DrawTexture(new Rect(a.x - 24, a.y - 24, 48, 48), finger);
                GUI.color = new Color(1f, 1f, 1f, 0.5f * (1f - k) + 0.2f);
                float r = Mathf.Lerp(30f, 70f, k);
                GUI.DrawTexture(new Rect(a.x - r, a.y - r, r * 2, r * 2), gRing);
                // a direction arrow: hold a side to run that way
                if (chevron == null) chevron = ChevronTexture(64);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(180f, new Vector2(a.x - 90, a.y) * scale);
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.DrawTexture(new Rect(a.x - 90 - 16, a.y - 16, 32, 32), chevron);
                GUI.matrix = m;
            }
            else
            {
                // taps (and two-finger taps): press, then a ripple
                float press = cyc < 0.25f ? 1f - cyc / 0.25f * 0.25f : 1f;
                float rip = Mathf.Clamp01((cyc - 0.15f) / 0.5f);
                foreach (var q in g == Gesture.TwoFingers ? new[] { a, b } : new[] { a })
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.85f);
                    float s = 48f * press;
                    GUI.DrawTexture(new Rect(q.x - s / 2, q.y - s / 2, s, s), finger);
                    if (rip > 0f && rip < 1f)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.6f * (1f - rip));
                        float r = Mathf.Lerp(26f, 80f, rip);
                        GUI.DrawTexture(new Rect(q.x - r, q.y - r, r * 2, r * 2), gRing);
                    }
                }
            }
            GUI.color = old;
        }

        /// <summary>The chalk marks: a hand-written word by the object, a small gesture glyph and a
        /// chalk line to the spot, wobbling slightly like chalk on concrete.</summary>
        void ChalkMarks(float w, float h)
        {
            var hd = HintDirector.Instance;
            var cam = Camera.main;
            if (hd == null || cam == null || hd.Shown.Count == 0) return;
            foreach (var hint in hd.Shown)
            {
                var vp = cam.WorldToViewportPoint(hint.world);
                if (vp.z <= 0f || vp.x < 0.03f || vp.x > 0.97f || vp.y < 0.05f || vp.y > 0.95f) continue;
                float x = vp.x * w, y = (1f - vp.y) * h;
                float wob = Mathf.Sin(Time.unscaledTime * 3f + x) * 1.5f;
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
                string glyph = hint.gesture switch
                {
                    Gesture.TapAgain or Gesture.TapHigh or Gesture.TapLow => "TAP - ", Gesture.Hold => "HOLD - ", _ => "",
                };
                var st = new GUIStyle(gChalk) { fontSize = 27 };
                st.normal.textColor = new Color(0.97f, 0.97f, 0.95f, pulse);
                // a soft dark smudge behind for legibility on light walls
                var old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.35f);
                GUI.DrawTexture(new Rect(x - 95, y - 58 + wob, 190, 44), glowTex);
                GUI.color = old;
                Label(new Rect(x - 120, y - 58 + wob, 240, 40), glyph + hint.text, st);
                // the swipe drawn as chalk arrows beside the word
                if (hint.gesture == Gesture.SwipeUp || hint.gesture == Gesture.SwipeDown || hint.gesture == Gesture.SwipeSide)
                {
                    if (chevron == null) chevron = ChevronTexture(64);
                    float textHalf = st.CalcSize(new GUIContent(hint.text)).x * 0.5f;
                    var m0 = GUI.matrix;
                    var oc = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, pulse);
                    if (hint.gesture == Gesture.SwipeSide)
                    {
                        foreach (var (cx2, ang2) in new[] { (x - textHalf - 22f, 180f), (x + textHalf + 22f, 0f) })
                        {
                            GUIUtility.RotateAroundPivot(ang2, new Vector2(cx2, y - 38 + wob) * scale);
                            GUI.DrawTexture(new Rect(cx2 - 12, y - 50 + wob, 24, 24), chevron);
                            GUI.matrix = m0;
                        }
                    }
                    else
                    {
                        float cx2 = x - textHalf - 22f;
                        GUIUtility.RotateAroundPivot(hint.gesture == Gesture.SwipeUp ? -90f : 90f, new Vector2(cx2, y - 38 + wob) * scale);
                        GUI.DrawTexture(new Rect(cx2 - 12, y - 50 + wob, 24, 24), chevron);
                        GUI.matrix = m0;
                    }
                    GUI.color = oc;
                }
                // a chalk tick down to the spot
                Fill(new Rect(x - 1, y - 18 + wob, 2, 14), new Color(1f, 1f, 1f, 0.6f * pulse));
            }
        }

        /// <summary>The pause screen: RESUME, MOVES (the help card), SOUND on/off, RESTART.</summary>
        void PauseCard(float w, float h, bool repaint)
        {
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.7f));
            Label(new Rect(0, h * 0.12f, w, 100), Spaced("PAUSED"), new GUIStyle(gTitle) { fontSize = 84 });
            Fill(new Rect(w * 0.5f - 150, h * 0.12f + 104, 300, 1), new Color(1f, 1f, 1f, 0.5f));
            if (director != null)
                Label(new Rect(0, h * 0.12f + 112, w, 30), $"FLOOR {director.Floor}   ·   WAVE {Mathf.Min(director.WaveIndex + 1, director.WaveCount)} / {director.WaveCount}   ·   {director.Score:N0}", new GUIStyle(gLine) { fontSize = 18 });
            // three rows of big buttons, sized to fit whatever height is left
            float bw = 300, gap = 16;
            float y = h * 0.12f + 150;
            float bh = Mathf.Clamp((h - 16f - y - gap * 2f) / 3f, 44f, 58f);
            resumeBtn = new Rect(w * 0.5f - bw - gap * 0.5f, y, bw, bh);
            movesBtn = new Rect(w * 0.5f + gap * 0.5f, y, bw, bh);
            soundBtn = new Rect(w * 0.5f - bw - gap * 0.5f, y + bh + gap, bw, bh);
            restartBtn = new Rect(w * 0.5f + gap * 0.5f, y + bh + gap, bw, bh);
            DrawButton(resumeBtn, "RESUME", 28);
            DrawButton(movesBtn, "MOVES", 24);
            DrawButton(soundBtn, Sound.Master > 0f ? "SOUND  ON" : "SOUND  OFF", 24);
            DrawButton(restartBtn, "RESTART", 24);
            // chalk-mark hints: AUTO retires each one once learned; ALWAYS keeps them all on
            hintsBtn = new Rect(w * 0.5f - bw * 0.5f, y + (bh + gap) * 2f, bw, bh);
            DrawButton(hintsBtn, HintDirector.AlwaysShow ? "HINTS  ALWAYS" : "HINTS  AUTO", 24);
            if (!repaint) return;
            GestureInput.Blocked.Add(ToScreen(new Rect(0, 0, w, h)));         // the screen belongs to the pause card
            if (Pressed(resumeBtn)) SetPause(false);
            else if (Pressed(movesBtn)) { helpOpen = true; TimeController.Paused = true; }
            else if (Pressed(soundBtn)) Sound.SetMuted(Sound.Master > 0f);
            else if (Pressed(restartBtn)) { SetPause(false); if (director != null) director.Restart(); }
            else if (Pressed(hintsBtn)) HintDirector.AlwaysShow = !HintDirector.AlwaysShow;
            else GestureInput.LastButtonTap = null;
        }

        /// <summary>The help card (game paused): every move with its gesture, and REPLAY TRAINING.</summary>
        void HelpCard(float w, float h, bool repaint)
        {
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.72f));
            var panel = new Rect(w * 0.5f - Mathf.Min(520f, w * 0.5f - 20f), 24, Mathf.Min(1040f, w - 40f), h - 48);
            Fill(new Rect(panel.x, panel.y, panel.width, 1), new Color(1f, 1f, 1f, 0.5f));
            Fill(new Rect(panel.x, panel.yMax - 1, panel.width, 1), new Color(1f, 1f, 1f, 0.5f));
            Label(new Rect(panel.x, panel.y + 4, panel.width, 56), Spaced("MOVES"), new GUIStyle(gTitle) { fontSize = 50 });
            string[,] rows =
            {
                { "TAP HIGH", "Punch combo  (left / right side = that way)" },
                { "TAP LOW", "Kick combo" },
                { "HOLD", "Run that way" },
                { "SWIPE UP", "Jump · climb a striped ledge · grab a rope, trapeze or bar" },
                { "SWIPE DOWN", "Throw your weapon at someone ahead · or drop through the floor (onto anyone below)" },
                { "SWIPE SIDEWAYS", "Dodge · vault · slide kick · wall flip" },
                { "RAMPAGE", "A rare power for when you're in trouble: the round button appears (or two fingers / Space when charged)" },
                { "WALK OVER", "Pick up a dropped weapon (sword, pipe, gun, knives): your taps use it" },
            };
            var k1 = new GUIStyle(gTitle) { fontSize = 28, alignment = TextAnchor.MiddleRight };
            var k2 = new GUIStyle(gLine) { fontSize = 21, alignment = TextAnchor.MiddleLeft };
            float rowH = Mathf.Min(42f, (panel.height - 190f) / 8f);
            float y = panel.y + 66;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Label(new Rect(panel.x + 20, y, 250, 34), rows[i, 0], k1);
                Label(new Rect(panel.x + 300, y, panel.width - 320, 34), rows[i, 1], k2);
                y += rowH;
            }
            var hint = new GUIStyle(gChalk) { fontSize = 19, wordWrap = true };
            Label(new Rect(panel.x + 20, y + 4, panel.width - 40, 50), "Some moves come from the situation: dive from above, over the edge, hand stomp, springboard. Head scissors: tap low as he winds up, or end the kick string close", hint);
            replayBtn = new Rect(w * 0.5f - 290, panel.yMax - 62, 270, 52);
            closeBtn = new Rect(w * 0.5f + 20, panel.yMax - 62, 270, 52);
            DrawButton(replayBtn, "REPLAY TRAINING", 21);
            DrawButton(closeBtn, "BACK TO THE FIGHT", 21);
            if (repaint)
            {
                GestureInput.Blocked.Add(ToScreen(replayBtn));
                GestureInput.Blocked.Add(ToScreen(closeBtn));
                GestureInput.Blocked.Add(ToScreen(new Rect(0, 0, w, h)));     // the whole screen is the card's while open
                if (Pressed(closeBtn)) { helpOpen = false; TimeController.Paused = pauseOpen; AudioListener.pause = pauseOpen; }
                else if (Pressed(replayBtn))
                {
                    helpOpen = false;
                    pauseOpen = false;
                    TimeController.Paused = false;
                    AudioListener.pause = false;
                    TutorialDirector.ResetDone();
                    HintDirector.ResetAll();
                    if (director != null) director.Restart();
                }
                else GestureInput.LastButtonTap = null;     // a tap anywhere else on the card does nothing
            }
        }

        GUIStyle bossName, bossTag, bossSmall, bossDefeat;
        static readonly Color BossRed = new Color(0.78f, 0.06f, 0.07f);

        /// <summary>The boss's name card (a lower third, sliding in on his roar) and, at the end,
        /// BRUTE DEFEATED.</summary>
        void BossCards(BossDirector bd, float w, float h)
        {
            LoadFonts();
            if (bossName == null)
            {
                bossName = new GUIStyle { font = Display, fontSize = 128, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                bossTag = new GUIStyle(bossName) { font = Body, fontSize = 24, fontStyle = FontStyle.Normal };
                bossSmall = new GUIStyle(bossTag) { fontSize = 18 };
                bossDefeat = new GUIStyle(bossName) { fontSize = 88, alignment = TextAnchor.MiddleCenter };
            }
            if (bd.NameAlpha > 0.001f)
            {
                float a = bd.NameAlpha;
                float x = w * 0.08f + bd.NameSlide * 160f;
                float y = h * 0.47f;
                bossSmall.normal.textColor = new Color(1f, 1f, 1f, 0.75f * a);
                Label(new Rect(x + 4, y - 30, w, 26), Spaced($"FLOOR {(director != null ? director.Floor : 1)}") + "      " + Spaced("FINAL OPPONENT"), bossSmall);
                bossName.normal.textColor = new Color(1f, 1f, 1f, a);
                Label(new Rect(x, y, w, 132), Spaced(bd.bossName), bossName);
                Fill(new Rect(x + 4, y + 132, 460f * a, 6), new Color(BossRed.r, BossRed.g, BossRed.b, a));
                if (bd.TagAlpha > 0.001f)
                {
                    bossTag.normal.textColor = new Color(1f, 1f, 1f, 0.85f * bd.TagAlpha * a);
                    Label(new Rect(x + 4 + (1f - bd.TagAlpha) * 20f, y + 146, w, 32), Spaced(bd.tagline), bossTag);
                }
            }
            if (bd.DefeatAlpha > 0.001f)
            {
                float a = bd.DefeatAlpha;
                float rise = (1f - a) * 18f;
                bossDefeat.normal.textColor = new Color(1f, 1f, 1f, a);
                Label(new Rect(0, h * 0.24f + rise, w, 100), Spaced("BRUTE DEFEATED"), bossDefeat);
                float lw = 420f * a;
                Fill(new Rect(w * 0.5f - lw * 0.5f, h * 0.24f + 104 + rise, lw, 4), new Color(BossRed.r, BossRed.g, BossRed.b, a));
                if (!string.IsNullOrEmpty(bd.DefeatSub))
                {
                    bossTag.alignment = TextAnchor.MiddleCenter;
                    bossTag.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    Label(new Rect(0, h * 0.24f + 116 + rise, w, 32), Spaced(bd.DefeatSub), bossTag);
                    bossTag.alignment = TextAnchor.MiddleLeft;
                }
            }
        }

        /// <summary>The boss health bar, top centre under the pause button: his name, a white
        /// "damage taken" segment that drains after a beat, the half-way phase mark, and
        /// ENRAGED pulsing once he passes it.</summary>
        void BossBar(BossDirector bd, float w)
        {
            float a = bd.HpAlpha;
            if (a < 0.01f || bd.Current != BossDirector.Stage.Fight) return;
            LoadFonts();
            if (bossSmall == null) BossCards(bd, w, 720f);
            float bw = Mathf.Min(560f, w * 0.48f), x = (w - bw) * 0.5f, y = 96f, bh = 14f;
            var nameSt = new GUIStyle(mid) { fontSize = 28, alignment = TextAnchor.LowerLeft };
            nameSt.normal.textColor = new Color(1f, 1f, 1f, a);
            Label(new Rect(x, y - 34, bw, 32), Spaced(bd.bossName), nameSt);
            if (bd.HpEnraged)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f);
                var en = new GUIStyle(nameSt) { alignment = TextAnchor.LowerRight, fontSize = 24 };
                en.normal.textColor = new Color(1f, 0.25f, 0.2f, a * pulse);
                Label(new Rect(x, y - 33, bw, 30), Spaced("ENRAGED"), en);
            }
            Fill(new Rect(x - 2, y - 2, bw + 4, bh + 4), new Color(0f, 0f, 0f, 0.6f * a));
            Fill(new Rect(x, y, bw, bh), new Color(0.2f, 0.02f, 0.03f, 0.75f * a));
            Fill(new Rect(x, y, bw * bd.HpLag, bh), new Color(1f, 1f, 1f, 0.85f * a));
            var red = bd.HpEnraged ? Color.Lerp(BossRed, new Color(1f, 0.2f, 0.1f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)) : BossRed;
            Fill(new Rect(x, y, bw * bd.Hp, bh), new Color(red.r, red.g, red.b, a));
            Fill(new Rect(x, y, bw * bd.Hp, 3), new Color(1f, 1f, 1f, 0.25f * a));          // a sheen along the top
            Fill(new Rect(x + bw * 0.5f - 1.5f, y - 4, 3, bh + 8), new Color(1f, 1f, 1f, 0.9f * a));   // phase two starts here
            float flash = 1f - Mathf.Clamp01((Time.unscaledTime - bd.EnragedAt) / 0.6f);
            if (flash > 0f) Fill(new Rect(x - 2, y - 2, bw + 4, bh + 4), new Color(1f, 1f, 1f, 0.8f * flash * a));
        }

        GUIStyle winSmall, winNum, winStamp, winTag;
        Texture2D inkSplat;
        static readonly Color InkRed = new Color(0.72f, 0.05f, 0.06f);

        /// <summary>The victory card, timed from CardT: FLOOR n over CLEAR, which slams in letter
        /// by letter over a red ink stroke; then the tally along the bottom (kills, score and time
        /// counting up, one after another) and the style grade stamped in red ink; NEW HIGH SCORE
        /// if it is.</summary>
        void VictoryCards(VictoryCinematic v, float w, float h)
        {
            float t = v.CardT, A = v.CardAlpha;
            if (t < 0f || A < 0.01f) return;
            EnsureLogo();
            if (winSmall == null)
            {
                winSmall = new GUIStyle { font = Body, fontSize = 20, alignment = TextAnchor.MiddleCenter };
                winNum = new GUIStyle { font = Display, fontSize = 54, alignment = TextAnchor.MiddleCenter, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold };
                winStamp = new GUIStyle { font = Brush != null ? Brush : Display, fontSize = 76, alignment = TextAnchor.MiddleCenter };
                winTag = new GUIStyle(winSmall) { fontSize = 18 };
                inkSplat = InkSplatTexture(128);
            }
            float barK = h / 720f;
            float top = 92f * barK, bottom = h - 92f * barK;
            float cx = w * 0.5f;

            // FLOOR n
            float fk = Mathf.Clamp01((t - 0.1f) / 0.3f);
            winSmall.normal.textColor = new Color(1f, 1f, 1f, 0.8f * fk * A);
            Label(new Rect(0, top + 4f + (1f - fk) * 6f, w, 26), Spaced($"FLOOR {v.FloorNo}"), winSmall);

            // CLEAR, slammed over the ink
            const string word = "CLEAR";
            float cy = top + 124f;
            float ww = 0f;
            var lw = new float[word.Length];
            for (int i = 0; i < word.Length; i++) { lw[i] = logoSt.CalcSize(new GUIContent(word[i].ToString())).x; ww += lw[i] + (i > 0 ? LogoGap : 0f); }
            float shake = 6f * Mathf.Exp(-t * 14f);
            float sx = Mathf.Sin(t * 90f) * shake, sy = Mathf.Cos(t * 77f) * shake * 0.6f;
            float reveal = E(0f, 0.2f, t);
            if (reveal > 0f)
            {
                float sw = ww + 170f, shh = 150f;
                var o = GUI.color;
                GUI.color = new Color(InkRed.r, InkRed.g, InkRed.b, 0.95f * A);
                GUI.DrawTextureWithTexCoords(new Rect(cx - sw * 0.5f + sx, cy - shh * 0.5f + 6f + sy, sw * reveal, shh), inkStroke, new Rect(0f, 0f, reveal, 1f));
                GUI.color = o;
            }
            float x = cx - ww * 0.5f + sx;
            float split = Mathf.Exp(-t * 7f);
            for (int i = 0; i < word.Length; i++)
            {
                float lc = x + lw[i] * 0.5f;
                x += lw[i] + LogoGap;
                float k = Mathf.Clamp01((t - i * 0.05f) / 0.12f);
                if (k <= 0f) continue;
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                float sc = Mathf.Lerp(2.4f, 1f, e);
                string ch = word[i].ToString();
                if (k < 1f)
                    for (int g = 1; g <= 3; g++) Letter(ch, lc, cy + sy, sc * (1f + g * 0.18f), new Color(1f, 1f, 1f, 0.12f * k * A));
                if (split * 9f > 0.3f) Letter(ch, lc + split * 9f, cy + sy, sc, new Color(1f, 0.15f, 0.1f, 0.7f * k * A));
                Letter(ch, lc + 3f, cy + sy + 4f, sc, new Color(0f, 0f, 0f, 0.6f * k * A));
                Letter(ch, lc, cy + sy, sc, new Color(0.95f, 0.95f, 0.93f, k * A));
            }

            // the tally along the bottom: three columns and the grade stamp
            float rowY = bottom - 96f;
            float colW = Mathf.Min(190f, w * 0.18f);
            float[] at = { VictoryCinematic.KillsAt, VictoryCinematic.ScoreAt, VictoryCinematic.TimeAt };
            string[] labels = { "KILLS", "SCORE", "TIME" };
            float startX = cx - colW * 2f;
            for (int i = 0; i < 3; i++)
            {
                float k = E(at[i], at[i] + 0.2f, t);
                if (k <= 0f) continue;
                float count = E(at[i], at[i] + VictoryCinematic.CountLen, t);
                string val = i == 0 ? Mathf.RoundToInt(v.Kills * count).ToString()
                           : i == 1 ? Mathf.RoundToInt(v.Score * count).ToString("N0")
                           : Clock(v.Seconds * count);
                float colX = startX + colW * i;
                float rise = (1f - k) * 12f;
                winTag.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.9f * k * A);
                Label(new Rect(colX, rowY + rise, colW, 22), Spaced(labels[i]), winTag);
                bool counting = count < 1f;
                winNum.normal.textColor = counting ? new Color(1f, 1f, 1f, 0.85f * k * A) : new Color(1f, 1f, 1f, k * A);
                Label(new Rect(colX, rowY + 20f + rise, colW, 60), val, winNum);
                if (i > 0) Fill(new Rect(colX, rowY + 8f, 1f, 64f), new Color(1f, 1f, 1f, 0.3f * k * A));
            }
            // a hairline under the row, drawn out as the first column lands
            float hl = E(VictoryCinematic.KillsAt, VictoryCinematic.KillsAt + 0.5f, t);
            if (hl > 0f) Fill(new Rect(cx - colW * 2f, rowY + 84f, colW * 4f * hl, 2f), new Color(1f, 1f, 1f, 0.5f * A));

            // the grade, stamped in red ink at the end of the row
            float gk = Mathf.Clamp01((t - VictoryCinematic.GradeAt) / 0.1f);
            if (gk > 0f)
            {
                float stampX = startX + colW * 3f + colW * 0.5f, stampY = rowY + 40f;
                float sc = gk < 1f ? Mathf.Lerp(2.6f, 1f, gk * gk) : 1f + 0.06f * Mathf.Exp(-(t - VictoryCinematic.GradeAt - 0.1f) * 9f) * Mathf.Sin((t - VictoryCinematic.GradeAt) * 40f);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(-8f, new Vector2(stampX, stampY) * scale);
                GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), new Vector2(stampX, stampY) * scale);
                var o = GUI.color;
                GUI.color = new Color(InkRed.r, InkRed.g, InkRed.b, 0.95f * gk * A);
                GUI.DrawTexture(new Rect(stampX - 88f, stampY - 88f, 176f, 176f), inkSplat);
                GUI.color = o;
                winStamp.normal.textColor = new Color(0f, 0f, 0f, 0.5f * gk * A);
                GUI.Label(new Rect(stampX - 100f + 3f, stampY - 60f + 4f, 200f, 120f), v.Grade, winStamp);
                winStamp.normal.textColor = new Color(1f, 1f, 1f, gk * A);
                GUI.Label(new Rect(stampX - 100f, stampY - 60f, 200f, 120f), v.Grade, winStamp);
                GUI.matrix = m;
                winTag.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.9f * gk * A);
                Label(new Rect(stampX - colW * 0.5f, rowY - 46f, colW, 22), Spaced("STYLE"), winTag);
            }

            // NEW HIGH SCORE: a red tag above the score
            if (v.NewBest && t > VictoryCinematic.BestAt)
            {
                float k = Mathf.Clamp01((t - VictoryCinematic.BestAt) / 0.15f);
                float pulse = 0.85f + 0.15f * Mathf.Sin(t * 7f);
                float tw = 190f * (0.6f + 0.4f * k), th = 28f;
                var r = new Rect(startX + colW * 1.5f - tw * 0.5f, rowY - 36f, tw, th);
                Fill(r, new Color(InkRed.r, InkRed.g, InkRed.b, 0.95f * k * A));
                winTag.normal.textColor = new Color(1f, 1f, 1f, k * A * pulse);
                GUI.Label(r, Spaced("NEW HIGH SCORE"), winTag);
            }
        }

        static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>A red ink splat for the grade stamp: a ragged blob with flung droplets.</summary>
        static Texture2D InkSplatTexture(int n)
        {
            var r = new System.Random(9);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            var ph = new float[4];
            for (int i = 0; i < 4; i++) ph[i] = (float)r.NextDouble() * 6.28f;
            var drops = new Vector3[9];
            for (int i = 0; i < drops.Length; i++)
            {
                float a = (float)r.NextDouble() * 6.28f, d = 0.72f + 0.22f * (float)r.NextDouble();
                drops[i] = new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.03f + 0.05f * (float)r.NextDouble());
            }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x - c) / c, v = (y - c) / c;
                    float rad = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                    float edge = 0.6f + 0.06f * Mathf.Sin(5f * ang + ph[0]) + 0.04f * Mathf.Sin(9f * ang + ph[1]) + 0.03f * Mathf.Sin(17f * ang + ph[2]);
                    float a = Mathf.Clamp01((edge - rad) * n * 0.5f);
                    foreach (var d in drops)
                    {
                        float dd = Mathf.Sqrt((u - d.x) * (u - d.x) + (v - d.y) * (v - d.y));
                        a = Mathf.Max(a, Mathf.Clamp01((d.z - dd) * n * 0.5f));
                    }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D RadialTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>Bright at the left edge fading right, soft at the top and bottom ends.</summary>
        static Texture2D EdgeTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)(n - 1), v = y / (float)(n - 1);
                    float a = Mathf.Pow(1f - u, 2.2f) * Mathf.Sin(v * Mathf.PI);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        void Diagnostics()
        {
            var cam = Camera.main;
            var smr = hero != null ? hero.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("DIAGNOSTICS (F1 to hide)");
            if (hero != null)
            {
                lines.AppendLine($"hero pos={hero.transform.position:F2} level={hero.Level} health={hero.Health:F0} style={hero.Style}");
                if (hero.body != null) lines.AppendLine($"clip={hero.body.CurrentName} t={hero.body.Time:F2}");
            }
            if (smr != null) lines.AppendLine($"body bounds center={smr.bounds.center:F2} size={smr.bounds.size:F2} visible={smr.isVisible}");
            if (cam != null) lines.AppendLine($"camera pos={cam.transform.position:F2} fov={cam.fieldOfView:F0}  timeScale={Time.timeScale:F2}");
            lines.AppendLine($"enemies={Enemy.All.Count} pickups={WeaponPickup.All.Count} fps={1f / Mathf.Max(0.001f, Time.unscaledDeltaTime):F0}");
            var st = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 15, wordWrap = false };
            st.normal.textColor = new Color(0.05f, 0.05f, 0.05f);
            Fill(new Rect(20, 160, 640, 120), new Color(1f, 1f, 1f, 0.75f));
            GUI.Label(new Rect(28, 164, 630, 116), lines.ToString(), st);
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }
    }
}
