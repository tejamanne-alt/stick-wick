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
            if (hero != null && hero.RampageIntro) { RampageIntroOverlay(w, h, barK); GUI.matrix = Matrix4x4.identity; return; }
            var win = VictoryCinematic.Instance;
            if (win != null && win.Active)
            {
                float vb = win.BarsPixels * barK;
                if (vb > 0.01f)
                {
                    Fill(new Rect(0, 0, w, vb), Color.black);
                    Fill(new Rect(0, h - vb, w, vb), Color.black);
                }
                VictoryCards(win, w, h);
                GUI.matrix = Matrix4x4.identity;
                return;
            }

            RampageFrame(w, h);
            DrawStick();
            PunchKickZones(w, h);
            if (hero != null) HeroPanel(w);
            if (director != null) DirectorPanel(w, h);
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
                    // the bar is charged: it burns red-white
                    Fill(new Rect(fb.x, fb.y - 1, fb.width, fb.height + 2), Color.Lerp(RampageRed, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f)));
                    lbl.normal.textColor = Color.Lerp(RampageRed, Color.white, 0.4f + 0.4f * Mathf.Sin(Time.unscaledTime * 10f));
                    Label(new Rect(24, 44, 400, 26), "R A M P A G E   R E A D Y", lbl);
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
                    if (!e.Alive) continue;
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

            bool trainingNow = TutorialDirector.Instance != null && TutorialDirector.Instance.Active;
            if (!trainingNow && (director.CurrentPhase == WaveDirector.Phase.Intro && director.Floor == 1 || (director.Floor == 1 && director.WaveIndex == 0 && director.ElapsedTime < 9f)))
                GUI.Label(new Rect(0, h - 56, w, 40), GestureInput.Instance != null && !GestureInput.Instance.Classic
                    ? "LEFT thumb: drag to run, flick up to jump   ·   RIGHT: tap = strike, swipe up = jump, sideways = dodge, down = duck/throw   ·   Two fingers: RAMPAGE"
                    : "TAP HIGH = punch combo, TAP LOW = kick combo (left / right side = that way)   ·   HOLD = run   ·   Swipe up = jump, sideways = dodge, down = throw   ·   Two fingers: RAMPAGE", new GUIStyle(small) { fontSize = 18 });

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
        Rect helpBtn, skipBtn, replayBtn, closeBtn, pauseBtn, resumeBtn, movesBtn, soundBtn, restartBtn;

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
                skipBtn = new Rect(w - 250, 200, 230, 48);
                DrawButton(skipBtn, "SKIP TRAINING  >", 21);
                if (repaint) GestureInput.Blocked.Add(ToScreen(skipBtn));
                if (repaint && Pressed(skipBtn)) tut.End(false);
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
        float readySince;

        /// <summary>Upper left, just under the Flow bar it belongs to: clear of the hero (the camera
        /// keeps him mid-screen) and of the thumbs' usual fighting area.</summary>
        Vector2 RampageButtonCentre(float w, float h) => new Vector2(96f, 236f);

        /// <summary>When the Flow bar is full: a big round RAMPAGE button at the bottom centre,
        /// pulsing, with rings rippling out; one tap and the rampage starts. The moment it becomes
        /// ready there is a call-out, a hit, and a few heartbeats.</summary>
        void RampageButton(float w, float h, bool repaint)
        {
            if (director == null || hero == null) return;
            bool ready = director.FlowReady && !hero.InRampage && !hero.IsDead && !TimeController.Paused;
            if (repaint && ready && !wasReady)
            {
                readySince = Time.unscaledTime;
                director.AddStyle(0, "RAMPAGE READY");
                Sound.Play(Sfx.CineHit, 0.6f);
                for (int i = 0; i < 3; i++) Sound.PlayLater(Sfx.Boom, 0.6f + i * 0.9f, 0.3f);
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
            switch (g)
            {
                case Gesture.TapHigh: a = b = new Vector2(w * 0.76f, h * 0.30f); break;
                case Gesture.TapLow: a = b = new Vector2(w * 0.76f, h * 0.70f); break;
                case Gesture.TapAgain: a = b = new Vector2(w * 0.76f, Mathf.Repeat(t, 0.7f) < 0.35f ? h * 0.30f : h * 0.70f); cyc = Mathf.Repeat(t, 0.35f) / 0.35f; break;
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
            // two rows of big buttons, sized to fit whatever height is left
            float bw = 300, bh = 58, gap = 16;
            float y = h * 0.12f + 160;
            resumeBtn = new Rect(w * 0.5f - bw - gap * 0.5f, y, bw, bh);
            movesBtn = new Rect(w * 0.5f + gap * 0.5f, y, bw, bh);
            soundBtn = new Rect(w * 0.5f - bw - gap * 0.5f, y + bh + gap, bw, bh);
            restartBtn = new Rect(w * 0.5f + gap * 0.5f, y + bh + gap, bw, bh);
            DrawButton(resumeBtn, "RESUME", 28);
            DrawButton(movesBtn, "MOVES", 24);
            DrawButton(soundBtn, Sound.Master > 0f ? "SOUND  ON" : "SOUND  OFF", 24);
            DrawButton(restartBtn, "RESTART", 24);
            if (!repaint) return;
            GestureInput.Blocked.Add(ToScreen(new Rect(0, 0, w, h)));         // the screen belongs to the pause card
            if (Pressed(resumeBtn)) SetPause(false);
            else if (Pressed(movesBtn)) { helpOpen = true; TimeController.Paused = true; }
            else if (Pressed(soundBtn)) Sound.SetMuted(Sound.Master > 0f);
            else if (Pressed(restartBtn)) { SetPause(false); if (director != null) director.Restart(); }
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
                { "RAMPAGE", "When the bar burns red: tap the round RAMPAGE button (or two fingers, or Space)" },
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
            Label(new Rect(panel.x + 20, y + 4, panel.width - 40, 50), "Some moves happen by themselves: dive from above, over the edge, head scissors, hand stomp, springboard", hint);
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

        GUIStyle winTitle, winStats;

        /// <summary>The victory card: FLOOR n CLEAR in big spaced capitals with a hairline, the
        /// grade, score and kills beneath, sliding up a little as they fade in.</summary>
        void VictoryCards(VictoryCinematic v, float w, float h)
        {
            LoadFonts();
            if (winTitle == null)
            {
                winTitle = new GUIStyle { font = Display, fontSize = 96, fontStyle = Display != null ? FontStyle.Normal : FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                winStats = new GUIStyle(winTitle) { font = Body, fontSize = 22, fontStyle = FontStyle.Normal };
            }
            if (v.TitleAlpha > 0.001f)
            {
                float rise = (1f - v.TitleAlpha) * 18f;
                winTitle.normal.textColor = new Color(1f, 1f, 1f, v.TitleAlpha);
                Label(new Rect(0, h * 0.30f + rise, w, 110), Spaced(v.Title), winTitle);
                float lw = 380f * v.TitleAlpha;
                Fill(new Rect(w * 0.5f - lw * 0.5f, h * 0.30f + 112 + rise, lw, 2), new Color(1f, 1f, 1f, 0.75f * v.TitleAlpha));
            }
            if (v.StatsAlpha > 0.001f)
            {
                winStats.normal.textColor = new Color(1f, 1f, 1f, 0.85f * v.StatsAlpha);
                Label(new Rect(0, h * 0.30f + 124 + (1f - v.StatsAlpha) * 10f, w, 34), v.Stats, winStats);
            }
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
