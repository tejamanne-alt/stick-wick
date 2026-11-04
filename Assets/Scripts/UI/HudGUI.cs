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
            scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = 720f;

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
            float bh = bars > 0.001f ? 70f * Mathf.SmoothStep(0f, 1f, bars) : 0f;
            if (cutscene) bh = Mathf.Max(bh, cine.BarsPixels);
            if (bh > 0.01f)
            {
                Fill(new Rect(0, 0, w, bh), Color.black);
                Fill(new Rect(0, h - bh, w, bh), Color.black);
            }
            if (cutscene) { OpeningCards(cine, w, h); GUI.matrix = Matrix4x4.identity; return; }

            DrawStick();
            PunchKickZones(w, h);
            if (hero != null) HeroPanel(w);
            if (director != null) DirectorPanel(w, h);
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
            if (c.TitleAlpha > 0.001f)
            {
                cineTitle.normal.textColor = new Color(0f, 0f, 0f, 0.55f * c.TitleAlpha);
                GUI.Label(new Rect(3, h * 0.40f + 3, w, 100), Spaced(c.title), cineTitle);
                cineTitle.normal.textColor = new Color(1f, 1f, 1f, c.TitleAlpha);
                GUI.Label(new Rect(0, h * 0.40f, w, 100), Spaced(c.title), cineTitle);
                Fill(new Rect(w * 0.5f - 160 * c.TitleAlpha, h * 0.40f + 104, 320 * c.TitleAlpha, 2), new Color(1f, 1f, 1f, 0.7f * c.TitleAlpha));
                cineTag.normal.textColor = new Color(1f, 1f, 1f, 0.8f * c.TitleAlpha);
                GUI.Label(new Rect(0, h * 0.40f + 112, w, 34), c.tagline, cineTag);
            }
            if (c.CaptionAlpha > 0.001f)
            {
                // the chapter card sits in the lower bar, left, like a lower third
                float slide = (1f - c.CaptionAlpha) * 30f;
                float y = h - 92f;
                cineCap.normal.textColor = new Color(1f, 1f, 1f, c.CaptionAlpha);
                GUI.Label(new Rect(70 - slide, y + 2, w, 46), c.chapter, cineCap);
                Fill(new Rect(72 - slide, y + 50, 50, 2), new Color(1f, 1f, 1f, 0.8f * c.CaptionAlpha));
                cineTag.alignment = TextAnchor.MiddleLeft;
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


        GUIStyle zoneStyle;

        /// <summary>Faint guides at the screen edges: tap above the line to punch, below to kick.</summary>
        void PunchKickZones(float w, float h)
        {
            var gi = GestureInput.Instance;
            if (gi == null) return;
            if (zoneStyle == null) zoneStyle = new GUIStyle { font = Body, fontSize = 17, alignment = TextAnchor.MiddleCenter };
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
                GUI.DrawTexture(new Rect(70, 720 - 230, 160, 160), ring);
                GUI.DrawTexture(new Rect(114, 720 - 186, 72, 72), knob);
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
                var lbl = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 18 };
                if (hero.InRampage) { lbl.normal.textColor = Color.white; Label(new Rect(24, 46, 400, 22), $"RAMPAGE  {hero.RampageLeft:0.0}", lbl); }
                else if (ready) { lbl.normal.textColor = new Color(1f, 1f, 1f, pulse); Label(new Rect(24, 46, 400, 22), "RAMPAGE READY   ·   two-finger tap", lbl); }
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
                var hs = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 16 };
                Label(new Rect(30 + 21 * hero.ComboCount.ToString().Length, 120, 200, 22), "H I T S", hs);
            }
        }

        void DirectorPanel(float w, float h)
        {
            Label(new Rect(w - 324, 8, 300, 48), director.Score.ToString("N0"), new GUIStyle(mid) { alignment = TextAnchor.UpperRight, fontSize = 44 });
            Label(new Rect(w - 324, 54, 300, 22), $"FLOOR {director.Floor}   ·   WAVE {Mathf.Min(director.WaveIndex + 1, director.WaveCount)} / {director.WaveCount}",
                  new GUIStyle(small) { alignment = TextAnchor.UpperRight, fontSize = 16 });

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

            // anyone the shot could not hold: a small red glow on the edge, at their height
            var cam = Camera.main;
            if (cam != null)
                foreach (var e in Enemy.All)
                {
                    if (!e.Alive) continue;
                    var vp = cam.WorldToViewportPoint(e.Chest);
                    if (vp.z > 0f && vp.x > -0.02f && vp.x < 1.02f && vp.y > -0.02f && vp.y < 1.02f) continue;
                    float ey = Mathf.Clamp(1f - vp.y, 0.12f, 0.88f) * h;
                    var oc = Enemy.TelegraphColor; oc.a = 0.5f;
                    if (vp.z <= 0f || vp.x < 0.5f) EdgeGlow(new Rect(0, ey - 50, 40, 100), oc, 0f);
                    else EdgeGlow(new Rect(w - 40, ey - 50, 40, 100), oc, 180f);
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

            if (director.CurrentPhase == WaveDirector.Phase.Intro && director.Floor == 1 || (director.Floor == 1 && director.WaveIndex == 0 && director.ElapsedTime < 9f))
                GUI.Label(new Rect(0, h - 56, w, 40), GestureInput.Instance != null && !GestureInput.Instance.Classic
                    ? "LEFT thumb: drag to run, flick up to jump   ·   RIGHT: tap = strike, swipe up = jump, sideways = dodge, down = duck/throw   ·   Two fingers: RAMPAGE"
                    : "TAP HIGH = punch combo, TAP LOW = kick combo (left / right side = that way)   ·   HOLD = run   ·   Swipe up = jump, sideways = dodge, down = throw   ·   Two fingers: RAMPAGE", new GUIStyle(small) { fontSize = 17 });

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
            var ws = new GUIStyle(small) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            ws.normal.textColor = new Color(1f, 1f, 1f, 0.75f * rankAlpha);
            Label(new Rect(center.x - 100, center.y + 36, 200, 16), Spaced(RankWords[ri]), ws);
            Fill(new Rect(center.x - 50, center.y + 56, 100, 1), new Color(1f, 1f, 1f, 0.15f * rankAlpha));
            Fill(new Rect(center.x - 50, center.y + 56, 100 * director.RankProgress, 1), new Color(1f, 1f, 1f, 0.8f * rankAlpha));
        }

        Texture2D glowTex, edgeTex;

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
