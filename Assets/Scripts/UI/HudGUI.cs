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

            // cinematic bars
            if (bars > 0.001f)
            {
                float bh = 70f * Mathf.SmoothStep(0f, 1f, bars);
                Fill(new Rect(0, 0, w, bh), Color.black);
                Fill(new Rect(0, h - bh, w, bh), Color.black);
            }

            if (hero != null) HeroPanel(w);
            if (director != null) DirectorPanel(w, h);
            HeroMarker();
            if (showDiagnostics) Diagnostics();
            GUI.matrix = Matrix4x4.identity;
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
            string weapon = hero.Style == FightStyle.Sword ? $"SWORD  {hero.SwordUses}" : hero.Style == FightStyle.Gun ? $"GUN  {hero.Ammo}" : "FISTS";
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
                    "Tap left / right: strike  ·  Swipe up: leap  ·  Swipe sideways: dodge (into a red flash = vault)  ·  Grab dropped weapons, swipe down to throw  ·  Full Flow: two-finger tap = RAMPAGE", small);

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
            Vector3 sp = cam.WorldToScreenPoint(hero.transform.position + Vector3.up * 2.15f);
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
