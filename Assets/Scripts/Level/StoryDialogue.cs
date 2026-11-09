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
            else if (w == "THE FOREMAN" || w == "THE DRIVER" || w == "THE CLEANER" || w == "THE HANDLER" || w == "THE WARDEN" || w == "VOSS" || w == "V." || w == "SILAS") { subKind = VoiceKind.Boss; subSpeaker = w; }
            else if (w == "MAYA" && mayaHere) { subKind = VoiceKind.Enemy; subSpeaker = "MAYA"; }
            else if (w == "PHONE") { subKind = VoiceKind.OffScreen; subSpeaker = "V.  ·  ON THE PHONE"; }
            else if (w == "WALKIE") { subKind = VoiceKind.OffScreen; subSpeaker = "MAYA  ·  ON THE WALKIE"; }
            else if (w == "MAYA")
            {
                subKind = VoiceKind.OffScreen;                 // her voice, out of a memory
                subSpeaker = "MAYA  ·  A MEMORY";
            }
            else if (w == "HER")
            {
                subKind = VoiceKind.OffScreen;                 // a voice from a memory: heard, not in the shot
                subSpeaker = "HER  ·  A MEMORY";
            }
            else if (w.StartsWith("VOICE") || w == "THE VOICE")
            {
                subKind = VoiceKind.OffScreen;
                subSpeaker = w == "THE VOICE" ? "VOICE ON THE PHONE" : w.Replace(",", "");
            }
            else { subKind = VoiceKind.Enemy; subSpeaker = w; }
            subColor = subKind == VoiceKind.You ? YouColor : subKind == VoiceKind.Boss ? BossColor : subKind == VoiceKind.OffScreen ? OffColor : EnemyColor;
            if (subSpeaker == "MAYA") subColor = new Color(1f, 0.86f, 0.86f);
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

        float subBand;                   // the height the bottom band has grown to for the current line

        bool SubActive => !string.IsNullOrEmpty(subLine) && Time.unscaledTime < subUntil + 0.25f;

        const float SubNameH = 20f, SubGap = 6f, SubPadTop = 12f, SubPadBottom = 16f;

        /// <summary>The line's width and height in the subtitle font, wrapping a long one.</summary>
        void SubMeasure(float w, out float lineW, out float lineH)
        {
            if (subWrap == null) subWrap = new GUIStyle(sub) { wordWrap = true, alignment = TextAnchor.UpperCenter };
            subWrap.font = sub.font;
            subWrap.fontSize = sub.fontSize;
            var content = new GUIContent(subLine);
            float maxW = Mathf.Min(w * 0.72f, 760f);
            lineW = Mathf.Min(sub.CalcSize(content).x + 4f, maxW);
            lineH = subWrap.CalcHeight(content, lineW);
        }

        /// <summary>How tall the bottom band must be for the line that is up (it grows smoothly and
        /// shrinks back when the line is gone). The story's letterbox bar grows to it, so the words
        /// always sit in black under the picture, never on top of it.</summary>
        float SubtitleBand(float w, bool repaint)
        {
            float need = 0f;
            if (SubActive)
            {
                SubMeasure(w, out _, out float lineH);
                need = SubPadTop + SubNameH + SubGap + lineH + SubPadBottom;
            }
            if (repaint) subBand = need > subBand ? Mathf.MoveTowards(subBand, need, Time.unscaledDeltaTime * 900f) : Mathf.MoveTowards(subBand, need, Time.unscaledDeltaTime * 260f);
            return subBand;
        }

        /// <summary>The words, in the bottom band: the speaker's name in his colour (a short dash before
        /// it, like the title's red rule), the line under it, both leaning towards him; and the
        /// marker over his head. Drawn last, so nothing in the story covers them.</summary>
        void DrawSubtitle(float w, float h, float bh)
        {
            if (!SubActive) { subX = -1f; subTickX = -1f; return; }
            float now = Time.unscaledTime;
            float a = Mathf.Clamp01((now - subAt) / 0.15f) * Mathf.Clamp01((subUntil + 0.25f - now) / 0.25f);
            SubMeasure(w, out float lineW, out float lineH);
            float need = SubPadTop + SubNameH + SubGap + lineH + SubPadBottom;
            float band = Mathf.Max(bh, need);
            float top = h - band;
            if (bh < need - 0.5f)
            {
                // no bars (he is playing): a soft dark fade up from the bottom edge instead
                for (int i = 0; i < 10; i++)
                {
                    float k = i / 10f;
                    Fill(new Rect(0, top - 24f + (band + 24f) * k, w, (band + 24f) / 10f + 1f), new Color(0f, 0f, 0f, 0.78f * a * Mathf.Sqrt(k + 0.1f)));
                }
            }
            // centre the two rows in the band, leaning towards the speaker
            float y0 = top + (band - need) * 0.5f + SubPadTop;
            bool seen = SpeakerOnScreen(w, h, out var head2d);
            float target = seen ? Mathf.Clamp(head2d.x, w * 0.38f, w * 0.62f) : w * 0.5f;
            subX = subX < 0f ? target : Mathf.Lerp(subX, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f));
            float cx = Mathf.Clamp(subX, lineW * 0.5f + 16f, w - lineW * 0.5f - 16f);

            // the name
            subWho.fontSize = subKind == VoiceKind.Boss ? 18 : 16;
            var name = new GUIContent(Spaced(subSpeaker));
            float nw = subWho.CalcSize(name).x;
            var oldAlign = subWho.alignment;
            subWho.alignment = TextAnchor.MiddleCenter;
            subWho.normal.textColor = subColor * new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(cx - nw * 0.5f - 4f, y0, nw + 8f, SubNameH), name, subWho);
            subWho.alignment = oldAlign;
            var mark = (subKind == VoiceKind.You ? YouBar : subColor) * new Color(1f, 1f, 1f, a);
            float lx = cx - nw * 0.5f - 12f;
            if (subKind == VoiceKind.OffScreen)
            {
                // a voice down a line: three rising bars before the name
                for (int i = 0; i < 3; i++)
                {
                    float hh = 4f + i * 3.5f;
                    Fill(new Rect(lx - 14f + i * 5f, y0 + SubNameH * 0.5f + 6f - hh, 3f, hh), mark);
                }
            }
            else Fill(new Rect(lx - 16f, y0 + SubNameH * 0.5f - 1f, 14f, 2f), mark);          // the dash

            // the line
            subWrap.normal.textColor = (subKind == VoiceKind.OffScreen ? new Color(0.84f, 0.92f, 1f) : Color.white) * new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(cx - lineW * 0.5f, y0 + SubNameH + SubGap, lineW, lineH), subLine, subWrap);

            // the marker over his head
            if (seen && head2d.y < top - 20f)
            {
                subTickX = subTickX < 0f ? head2d.x : Mathf.Lerp(subTickX, head2d.x, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
                subTickY = Mathf.Max(head2d.y, bh + 26f);
                float pop = 1f + 0.35f * Mathf.Exp(-(now - subAt) * 6f);
                float bob = Mathf.Sin(now * 5f) * 2f;
                Tick(subTickX, subTickY - 6f + bob, 18f * pop, new Color(0f, 0f, 0f, 0.7f * a));
                Tick(subTickX, subTickY - 8f + bob, 14f * pop, mark);
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
