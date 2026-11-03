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
            GUI.matrix = Matrix4x4.identity;
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
