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

        GUIStyle big, mid, small, arrow;
        Texture2D white;
        float scale, bars;

        static readonly Color Ink = new Color(0.06f, 0.06f, 0.06f);
        static readonly Color[] RankColors =
        {
            new Color(0.35f, 0.35f, 0.35f), new Color(0.25f, 0.45f, 0.6f), new Color(0.2f, 0.55f, 0.3f),
            new Color(0.85f, 0.6f, 0.1f), new Color(0.9f, 0.35f, 0.1f), new Color(0.9f, 0.15f, 0.15f), new Color(0.95f, 0.1f, 0.4f),
        };

        void Build()
        {
            white = Texture2D.whiteTexture;
            big = new GUIStyle { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Ink;
            mid = new GUIStyle(big) { fontSize = 30 };
            small = new GUIStyle(big) { fontSize = 18, fontStyle = FontStyle.Normal };
            small.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
            arrow = new GUIStyle(big) { fontSize = 54 };
            arrow.normal.textColor = Enemy.TelegraphColor;
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

            // cinematic bars
            if (bars > 0.001f)
            {
                float bh = 70f * Mathf.SmoothStep(0f, 1f, bars);
                Fill(new Rect(0, 0, w, bh), Color.black);
                Fill(new Rect(0, h - bh, w, bh), Color.black);
            }

            DrawStick();
            if (hero != null) HeroPanel(w);
            if (director != null) DirectorPanel(w, h);
            HeroMarker();
            if (showDiagnostics) Diagnostics();
            GUI.matrix = Matrix4x4.identity;
        }

        Texture2D ring, knob;

        /// <summary>The floating movement stick, drawn where the left thumb is.</summary>
        void DrawStick()
        {
            var gi = GestureInput.Instance;
            if (gi == null) return;
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
                    v[y * 128 + x] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01((d - 0.55f) / 0.6f) * 170));
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
            Rect bar = new Rect(24, 20, 300, 16);
            Fill(bar, new Color(0, 0, 0, 0.15f));
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hero.Health / hero.maxHealth), bar.height), Ink);
            // flow
            if (director != null)
            {
                Rect fb = new Rect(24, 42, 300, 9);
                Fill(fb, new Color(0, 0, 0, 0.12f));
                bool ready = director.FlowReady;
                float pulse = ready ? 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
                Fill(new Rect(fb.x, fb.y, fb.width * director.Flow / 100f, fb.height), new Color(1f, 1f, 1f, pulse));
                var lbl = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 15 };
                if (hero.InRampage) { lbl.normal.textColor = Color.white; GUI.Label(new Rect(24, 54, 400, 22), $"RAMPAGE  {hero.RampageLeft:0.0}", lbl); }
                else if (ready) { lbl.normal.textColor = new Color(1f, 1f, 1f, pulse); GUI.Label(new Rect(24, 54, 400, 22), "RAMPAGE READY: two-finger tap", lbl); }
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
                var g = new GUIStyle(big) { fontSize = 46 };
                g.normal.textColor = new Color(0.9f, 0.1f, 0.1f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 18f));
                GUI.Label(new Rect(0, 200, w, 60), $"TAP TAP TAP!  {hero.GrabTaps}/4", g);
            }
            GUI.Label(new Rect(24, 76, 300, 30), weapon, new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 20, fontStyle = FontStyle.Bold });
            if (hero.ComboCount >= 2)
                GUI.Label(new Rect(24, 104, 300, 50), $"{hero.ComboCount} HITS", new GUIStyle(mid) { alignment = TextAnchor.UpperLeft });
        }

        void DirectorPanel(float w, float h)
        {
            GUI.Label(new Rect(w - 324, 14, 300, 40), director.Score.ToString("N0"), new GUIStyle(mid) { alignment = TextAnchor.UpperRight });
            GUI.Label(new Rect(w - 324, 50, 300, 24), $"Floor {director.Floor}   Wave {Mathf.Min(director.WaveIndex + 1, director.WaveCount)}/{director.WaveCount}",
                      new GUIStyle(small) { alignment = TextAnchor.UpperRight });

            // style rank
            int ri = director.RankIndex;
            var rankStyle = new GUIStyle(big) { fontSize = 70 + ri * 6, alignment = TextAnchor.MiddleRight };
            rankStyle.normal.textColor = RankColors[ri];
            GUI.Label(new Rect(w - 224, 90, 200, 90), director.Rank, rankStyle);
            Fill(new Rect(w - 144, 182, 120, 5), new Color(0, 0, 0, 0.12f));
            Fill(new Rect(w - 144, 182, 120 * director.RankProgress, 5), RankColors[ri]);

            if (Time.unscaledTime < director.BannerUntil && !string.IsNullOrEmpty(director.Banner))
                GUI.Label(new Rect(0, h * 0.2f, w, 90), director.Banner, big);
            if (Time.unscaledTime < director.StyleUntil && !string.IsNullOrEmpty(director.StyleText))
                GUI.Label(new Rect(0, h * 0.32f, w, 50), director.StyleText, mid);

            // spawn warnings: an arrow at the edge the enemy will come from
            foreach (var inc in director.Upcoming)
            {
                if (Time.time < inc.time) continue;
                string glyph; Rect r;
                switch (inc.kind)
                {
                    case SpawnKind.LeftDoor: glyph = "<"; r = new Rect(10, h * 0.62f, 80, 80); break;
                    case SpawnKind.RightDoor: glyph = ">"; r = new Rect(w - 90, h * 0.62f, 80, 80); break;
                    case SpawnKind.UpperDoor: glyph = "<"; r = new Rect(10, h * 0.28f, 80, 80); break;
                    case SpawnKind.Ceiling: glyph = "v"; r = new Rect(w * 0.5f - 40, 80, 80, 80); break;
                    default: glyph = "o"; r = new Rect(w * 0.5f - 40, h - 130, 80, 80); break;
                }
                GUI.Label(r, glyph, arrow);
            }

            if (director.CurrentPhase == WaveDirector.Phase.Intro && director.Floor == 1 || (director.Floor == 1 && director.WaveIndex == 0 && director.ElapsedTime < 9f))
                GUI.Label(new Rect(0, h - 56, w, 40),
                    "LEFT thumb: drag to run, flick up to jump   ·   RIGHT: tap = strike, swipe up = jump, sideways = dodge, down = duck/throw   ·   Two fingers: RAMPAGE   ·   Jump at ropes, bars and walls", small);

            if (director.CurrentPhase == WaveDirector.Phase.Dead) Results(w, h);
        }

        void Results(float w, float h)
        {
            Rect panel = new Rect(w * 0.5f - 260, h * 0.3f, 520, 290);
            Fill(panel, new Color(1f, 1f, 1f, 0.88f));
            var rankStyle = new GUIStyle(big) { fontSize = 96 };
            rankStyle.normal.textColor = RankColors[Mathf.Clamp(director.BestRankIndex, 0, RankColors.Length - 1)];
            GUI.Label(new Rect(panel.x, panel.y + 10, panel.width, 110), director.Grade(), rankStyle);
            GUI.Label(new Rect(panel.x, panel.y + 120, panel.width, 40), director.Score.ToString("N0") + (director.NewBest ? "   NEW BEST!" : ""), mid);
            GUI.Label(new Rect(panel.x, panel.y + 165, panel.width, 30),
                $"Floor {director.Floor}  ·  {director.Kills} kills  ·  Best {director.BestScore:N0} (floor {director.BestFloor})", small);
            GUI.Label(new Rect(panel.x, panel.y + 240, panel.width, 30), "Tap to play again", small);
        }

        /// <summary>A small white marker over the hero's head (every fighter is dark).</summary>
        void HeroMarker()
        {
            var cam = Camera.main;
            if (hero == null || cam == null || hero.IsDead) return;
            Vector3 sp = cam.WorldToScreenPoint(hero.transform.position + Vector3.up * 1.75f);
            if (sp.z <= 0f) return;
            float x = sp.x / scale, y = (Screen.height - sp.y) / scale;
            var st = new GUIStyle(small) { fontSize = 26, fontStyle = FontStyle.Bold };
            st.normal.textColor = Color.white;
            GUI.Label(new Rect(x - 20, y - 20, 40, 30), "v", st);
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
