using UnityEngine;

namespace ProjectInk
{
    /// <summary>WHO IS SPEAKING: one rule for every line in the story (docs/story/dialogue.md).
    ///
    ///  1. A plain name, from the player's side: YOU (the hero, who has no name of his own),
    ///     THUG, THE FOREMAN, VOICE ON THE PHONE, VOICE BEYOND THE SHUTTER.
    ///  2. A colour for each kind of speaker, the same all game:
    ///       YOU             white, with the game's red bar (he is the red of the title)
    ///       his enemies     amber
    ///       a boss          blood orange, and his name a size up
    ///       off screen      cold blue, the line itself tinted, with a signal mark by the name
    ///  3. A marker over the speaker's head while he talks: a small triangle in his colour,
    ///     pulsing once with each new line. Off-screen voices have none.
    ///  4. The subtitle leans towards the speaker: its panel slides left or right under him
    ///     (within the middle of the screen), so even without reading the name the eye goes from
    ///     the words to the mouth.
    /// Each line sits on a dark panel, so white text reads over rain, lightning and backlight.</summary>
    public partial class StoryIntro
    {
        enum VoiceKind { You, Enemy, Boss, OffScreen }

        string subSpeaker, subLine;
        float subUntil, subAt, subX = -1f, subTickX = -1f, subTickY;
        VoiceKind subKind;
        Color subColor;
        Transform subSource;
        GUIStyle subWrap;

        static readonly Color YouColor = new Color(1f, 1f, 1f);
        static readonly Color YouBar = new Color(0.8f, 0.06f, 0.07f);
        static readonly Color EnemyColor = new Color(1f, 0.7f, 0.28f);
        static readonly Color BossColor = new Color(1f, 0.38f, 0.22f);
        static readonly Color OffColor = new Color(0.58f, 0.8f, 1f);

        void Say(string who, string line, float seconds) => Say(who, line, seconds, null);

        /// <summary>A line of dialogue. `source` pins it to a body; left out, the speaker is found:
        /// YOU is the hero, THE FOREMAN the boss, any other on-screen name the nearest of his men.</summary>
        void Say(string who, string line, float seconds, Transform source)
        {
            string w = who.ToUpperInvariant();
            if (w == "HIM" || w == "YOU") { subKind = VoiceKind.You; subSpeaker = "YOU"; }
            else if (w == "THE FOREMAN") { subKind = VoiceKind.Boss; subSpeaker = "THE FOREMAN"; }
            else if (w.StartsWith("VOICE") || w == "THE VOICE")
            {
                subKind = VoiceKind.OffScreen;
                subSpeaker = w == "THE VOICE" ? "VOICE ON THE PHONE" : w.Replace(",", "");
            }
            else { subKind = VoiceKind.Enemy; subSpeaker = w; }
            subColor = subKind == VoiceKind.You ? YouColor : subKind == VoiceKind.Boss ? BossColor : subKind == VoiceKind.OffScreen ? OffColor : EnemyColor;
            subLine = line;
            subAt = Time.unscaledTime;
            subUntil = subAt + seconds;
            subSource = source != null ? source : FindSpeaker(subKind);
        }

        Transform FindSpeaker(VoiceKind kind)
        {
            switch (kind)
            {
                case VoiceKind.You: return head != null ? head : hero != null ? hero.transform : null;
                case VoiceKind.Boss: return foreman != null ? HeadOf(foreman) : null;
                case VoiceKind.Enemy:
                    {
                        // the nearest of his men to the hero (the one in his face)
                        Enemy best = null;
                        float bd = 14f;
                        foreach (var e in Enemy.All)
                        {
                            if (e == null || e == foreman || !e.gameObject.activeInHierarchy) continue;
                            float d = Mathf.Abs(e.transform.position.x - hero.transform.position.x);
                            if (d < bd) { bd = d; best = e; }
                        }
                        return best != null ? HeadOf(best) : null;
                    }
                default: return null;
            }
        }

        static Transform HeadOf(Enemy e)
        {
            var hd = e.body != null ? e.body.Bone("head") : null;
            return hd != null ? hd : e.transform;
        }

        /// <summary>Where over his head the marker goes, on screen (GUI space); false if he is not in shot.</summary>
        bool SpeakerOnScreen(float w, float h, out Vector2 at)
        {
            at = Vector2.zero;
            var c = Camera.main;
            if (subSource == null || c == null || subKind == VoiceKind.OffScreen) return false;
            bool isHead = subSource == head || subSource.name == "head";
            var world = subSource.position + Vector3.up * (isHead ? 0.42f : 1.75f) * Mathf.Max(1f, subSource.root.localScale.y * 0.85f);
            var sp = c.WorldToScreenPoint(world);
            if (sp.z <= 0.05f) return false;
            at = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
            return at.x > 20f && at.x < w - 20f && at.y > 30f && at.y < h - 40f;
        }

        /// <summary>The line on its panel in the lower bar, the name above it in the speaker's
        /// colour, the panel leaning towards him, and the marker over his head.</summary>
        void DrawSubtitle(float w, float h, float bh, float barK)
        {
            if (Time.unscaledTime >= subUntil + 0.25f || string.IsNullOrEmpty(subLine)) { subX = -1f; return; }
            float now = Time.unscaledTime;
            float a = Mathf.Clamp01((now - subAt) / 0.15f) * Mathf.Clamp01((subUntil + 0.25f - now) / 0.25f);
            if (subWrap == null) subWrap = new GUIStyle(sub) { wordWrap = true, alignment = TextAnchor.MiddleLeft };

            bool seen = SpeakerOnScreen(w, h, out var head2d);
            // the panel leans towards the speaker (and glides when the camera moves)
            float target = seen ? Mathf.Clamp(head2d.x, w * 0.34f, w * 0.66f) : w * 0.5f;
            subX = subX < 0f ? target : Mathf.Lerp(subX, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));

            // size the panel to the line (wrapping a long one onto two)
            float maxW = Mathf.Min(w * 0.72f, 760f);
            var content = new GUIContent(subLine);
            float lineW = Mathf.Min(sub.CalcSize(content).x, maxW);
            float lineH = subWrap.CalcHeight(content, lineW);
            subWho.fontSize = subKind == VoiceKind.Boss ? 18 : 16;
            var nameContent = new GUIContent(Spaced(subSpeaker));
            float nameW = subWho.CalcSize(nameContent).x + (subKind == VoiceKind.OffScreen ? 22f : 0f);
            float pw = Mathf.Max(lineW, nameW) + 40f, ph = lineH + 34f;
            float baseY = h - Mathf.Max(bh, 70f * barK) * 0.5f;
            var panel = new Rect(Mathf.Clamp(subX - pw * 0.5f, 12f, w - pw - 12f), Mathf.Min(baseY - ph * 0.5f, h - ph - 8f), pw, ph);

            Fill(panel, new Color(0f, 0f, 0f, 0.62f * a));
            Fill(new Rect(panel.x, panel.y, 4f, panel.height), (subKind == VoiceKind.You ? YouBar : subColor) * new Color(1f, 1f, 1f, a));
            // the name
            var old = subWho.alignment;
            subWho.alignment = TextAnchor.MiddleLeft;
            float nx = panel.x + 20f;
            if (subKind == VoiceKind.OffScreen)
            {
                // a little signal mark: three rising bars (a voice coming down a line)
                for (int i = 0; i < 3; i++)
                {
                    float bhh = 4f + i * 3.5f;
                    Fill(new Rect(nx + i * 5f, panel.y + 18f - bhh, 3f, bhh), subColor * new Color(1f, 1f, 1f, a));
                }
                nx += 22f;
            }
            subWho.normal.textColor = subColor * new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(nx, panel.y + 4f, pw, 20f), nameContent, subWho);
            subWho.alignment = old;
            // the line
            subWrap.font = sub.font;
            subWrap.fontSize = sub.fontSize;
            subWrap.normal.textColor = (subKind == VoiceKind.OffScreen ? new Color(0.84f, 0.92f, 1f) : Color.white) * new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 26f, lineW, lineH), content, subWrap);

            // the marker over his head
            if (seen)
            {
                subTickX = subTickX < 0f ? head2d.x : Mathf.Lerp(subTickX, head2d.x, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
                subTickY = head2d.y;
                float pop = 1f + 0.35f * Mathf.Exp(-(now - subAt) * 6f);           // a pulse as he starts
                float bob = Mathf.Sin(now * 5f) * 2f;
                Tick(subTickX, subTickY - 6f + bob, 18f * pop, new Color(0f, 0f, 0f, 0.7f * a));
                Tick(subTickX, subTickY - 8f + bob, 14f * pop, (subKind == VoiceKind.You ? YouBar : subColor) * new Color(1f, 1f, 1f, a));
            }
            else subTickX = -1f;
        }

        /// <summary>A small downward triangle (the speech marker), its point at (x, y).</summary>
        static void Tick(float x, float y, float size, Color c)
        {
            int rows = 7;
            float rh = size * 0.7f / rows;
            for (int i = 0; i < rows; i++)
            {
                float rw = size * (1f - (float)i / rows);
                Fill(new Rect(x - rw * 0.5f, y - size * 0.7f + i * rh, rw, rh + 0.5f), c);
            }
        }
    }
}
