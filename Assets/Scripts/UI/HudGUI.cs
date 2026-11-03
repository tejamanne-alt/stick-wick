using UnityEngine;

namespace ProjectInk
{
    /// <summary>Prototype HUD drawn with IMGUI (no UI assets): health, combo, score,
    /// wave banners, spawn warnings at the screen edges, controls hint and results.</summary>
    public class HudGUI : MonoBehaviour
    {
        public HeroController hero;
        public WaveDirector director;

        GUIStyle big, mid, small, arrow;
        Texture2D white;
        float scale;

        void Build()
        {
            white = Texture2D.whiteTexture;
            big = new GUIStyle { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = new Color(0.08f, 0.08f, 0.08f);
            mid = new GUIStyle(big) { fontSize = 30 };
            small = new GUIStyle(big) { fontSize = 18, fontStyle = FontStyle.Normal };
            small.normal.textColor = new Color(0.25f, 0.25f, 0.25f);
            arrow = new GUIStyle(big) { fontSize = 54 };
            arrow.normal.textColor = Enemy.TelegraphColor;
        }

        void OnGUI()
        {
            if (big == null) Build();
            scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = 720f;

            if (hero != null)
            {
                // health bar
                Rect bar = new Rect(24, 20, 300, 18);
                Fill(bar, new Color(0, 0, 0, 0.15f));
                Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(hero.Health / hero.maxHealth), bar.height), new Color(0.08f, 0.08f, 0.08f));
                string style = hero.Style == FightStyle.Sword ? "SWORD" : hero.Style == FightStyle.Gun ? "GUN-FU" : "FISTS";
                if (hero.Style == FightStyle.Gun) style += hero.Reloading ? "   reloading" : $"   {hero.Ammo}/{AttackData.MagazineSize}";
                GUI.Label(new Rect(w * 0.5f - 200, 14, 400, 30), style + "   (swipe down to switch)", small);
                if (hero.ComboCount >= 2)
                    GUI.Label(new Rect(24, 50, 300, 60), $"{hero.ComboCount} HITS", new GUIStyle(mid) { alignment = TextAnchor.UpperLeft });
            }

            if (director != null)
            {
                GUI.Label(new Rect(w - 324, 14, 300, 40), director.Score.ToString("N0"), new GUIStyle(mid) { alignment = TextAnchor.UpperRight });
                if (director.CurrentPhase == WaveDirector.Phase.Fighting || director.CurrentPhase == WaveDirector.Phase.BetweenWaves)
                    GUI.Label(new Rect(w - 324, 52, 300, 24), $"Wave {director.WaveIndex + 1}/{director.WaveCount}", new GUIStyle(small) { alignment = TextAnchor.UpperRight });

                if (Time.unscaledTime < director.BannerUntil && !string.IsNullOrEmpty(director.Banner))
                    GUI.Label(new Rect(0, h * 0.18f, w, 90), director.Banner, big);
                if (Time.unscaledTime < director.StyleUntil && !string.IsNullOrEmpty(director.StyleText))
                    GUI.Label(new Rect(0, h * 0.30f, w, 50), director.StyleText, mid);

                // spawn warnings: an arrow at the edge the enemy will come from
                foreach (var inc in director.Upcoming)
                {
                    if (Time.time < inc.time) continue;
                    string glyph; Rect r;
                    switch (inc.kind)
                    {
                        case SpawnKind.LeftDoor: glyph = "<"; r = new Rect(10, h * 0.5f - 40, 80, 80); break;
                        case SpawnKind.RightDoor: glyph = ">"; r = new Rect(w - 90, h * 0.5f - 40, 80, 80); break;
                        case SpawnKind.StairsTop: glyph = "^"; r = new Rect(w - 90, h * 0.22f, 80, 80); break;
                        case SpawnKind.Ceiling: glyph = "v"; r = new Rect(w * 0.5f - 40, 70, 80, 80); break;
                        default: glyph = "o"; r = new Rect(w * 0.5f - 40, h - 120, 80, 80); break;
                    }
                    GUI.Label(r, glyph, arrow);
                }

                if (director.CurrentPhase == WaveDirector.Phase.Intro || (director.WaveIndex == 0 && director.ElapsedTime < 7f))
                    GUI.Label(new Rect(0, h - 60, w, 40),
                        "Tap left / right: strike  ·  Swipe up: jump attack  ·  Swipe sideways: dodge  ·  Swipe down: fists / sword / gun  ·  Strike a red enemy to counter", small);

                if (director.CurrentPhase == WaveDirector.Phase.Cleared)
                    Results(w, h, true);
                else if (director.CurrentPhase == WaveDirector.Phase.Dead)
                    Results(w, h, false);
            }
            HeroMarker();
            if (showDiagnostics) Diagnostics();
            GUI.matrix = Matrix4x4.identity;
        }

        public bool showDiagnostics = true;   // F1 toggles; turn off once the first run is confirmed

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) showDiagnostics = !showDiagnostics;
#else
            if (Input.GetKeyDown(KeyCode.F1)) showDiagnostics = !showDiagnostics;
#endif
        }

        /// <summary>A small white marker over the hero's head (every fighter is black), drawn
        /// from the root position, so it shows where the hero is even if the body does not.</summary>
        void HeroMarker()
        {
            var cam = Camera.main;
            if (hero == null || cam == null) return;
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
            lines.AppendLine("DIAGNOSTICS (F1 to hide) - screenshot this for Claude");
            if (hero == null) lines.AppendLine("hero: MISSING reference");
            else
            {
                lines.AppendLine($"hero active={hero.gameObject.activeInHierarchy} pos={hero.transform.position:F2} rot={hero.transform.eulerAngles:F0} health={hero.Health:F0}");
                if (hero.body != null)
                    lines.AppendLine($"model localRot={hero.body.transform.localEulerAngles:F0} localScale={hero.body.transform.localScale:F2} clip={hero.body.CurrentName} t={hero.body.Time:F2}");
            }
            if (smr == null) lines.AppendLine("body renderer: NOT FOUND");
            else
            {
                var b = smr.bounds;
                lines.AppendLine($"body enabled={smr.enabled} visible={smr.isVisible} mesh={(smr.sharedMesh != null ? smr.sharedMesh.vertexCount.ToString() : "null")} bones={smr.bones.Length}");
                lines.AppendLine($"body bounds center={b.center:F2} size={b.size:F2}");
                var m = smr.sharedMaterial;
                lines.AppendLine($"material={(m != null ? m.name + " / " + m.shader.name + " supported=" + m.shader.isSupported : "null")}");
                if (cam != null)
                {
                    var vp = cam.WorldToViewportPoint(b.center);
                    lines.AppendLine($"body on screen at viewport {vp:F2} (x,y in 0..1 and z>0 means in view)");
                }
            }
            if (cam != null) lines.AppendLine($"camera pos={cam.transform.position:F2} fwd={cam.transform.forward:F2} fov={cam.fieldOfView:F0}");
            lines.AppendLine($"enemies alive={Enemy.All.Count} pipeline={(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "built-in")}");
            var st = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontSize = 15, wordWrap = false };
            st.normal.textColor = new Color(0.05f, 0.05f, 0.05f);
            Fill(new Rect(20, 100, 760, 190), new Color(1f, 1f, 1f, 0.75f));
            GUI.Label(new Rect(28, 104, 750, 186), lines.ToString(), st);
        }

        void Results(float w, float h, bool cleared)
        {
            Rect panel = new Rect(w * 0.5f - 220, h * 0.34f, 440, 230);
            Fill(panel, new Color(1f, 1f, 1f, 0.85f));
            if (cleared)
            {
                GUI.Label(new Rect(panel.x, panel.y + 20, panel.width, 110), director.Grade(), new GUIStyle(big) { fontSize = 110 });
                GUI.Label(new Rect(panel.x, panel.y + 130, panel.width, 30),
                    $"Score {director.Score:N0}  ·  Time {director.ElapsedTime:0.0}s  ·  Damage {director.DamageTaken:0}", small);
            }
            else GUI.Label(new Rect(panel.x, panel.y + 40, panel.width, 90), "Try again", mid);
            GUI.Label(new Rect(panel.x, panel.y + 180, panel.width, 30), "Tap to play again", small);
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
