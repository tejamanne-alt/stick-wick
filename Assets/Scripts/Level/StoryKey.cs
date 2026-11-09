using UnityEngine;

namespace ProjectInk
{
    /// <summary>The key from his coat: a brass safe-deposit key on a worn leather tag stamped with
    /// a bank's mark (a keyhole in a shield) and MERIDIAN TRUST · SAFE DEPOSIT · Nº 417. Shown
    /// full screen at the end of chapter 1 and at the start of chapter 2 (where it dissolves into
    /// the same mark carved over the bank's doors). Drawn in the game's black and white.</summary>
    public partial class StoryIntro
    {
        float keyUi;                       // 0 hidden .. 1 full
        float keyUiTarget;
        Texture2D keyTex, markTex;

        void ShowKey(bool on) => keyUiTarget = on ? 1f : 0f;

        /// <summary>The bank's mark alone (a keyhole in a shield), pale on clear: for the tag and the stone.</summary>
        public static Texture2D BankMark(int n, Color ink)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float u = (i + 0.5f) / n * 2f - 1f, v = (j + 0.5f) / n * 2f - 1f;   // -1..1, v up
                    // the shield: flat top, sides curving to a point at the bottom
                    float half = v > 0.1f ? 0.82f : 0.82f * Mathf.Sqrt(Mathf.Max(0f, (v + 0.95f) / 1.05f));
                    float edge = Mathf.Min(half - Mathf.Abs(u), 0.88f - v);
                    float a = Mathf.Clamp01(edge * n * 0.25f);
                    float rim = Mathf.Clamp01((0.07f - Mathf.Abs(edge - 0.04f)) * n * 0.25f);
                    // the keyhole cut through it: a circle over a tapering slot
                    float hole = Mathf.Max(
                        Mathf.Clamp01((0.2f - Mathf.Sqrt(u * u + (v - 0.22f) * (v - 0.22f))) * n * 0.25f),
                        (v < 0.18f && v > -0.55f && Mathf.Abs(u) < 0.07f + (0.18f - v) * 0.12f) ? 1f : 0f);
                    float body = Mathf.Max(a * 0.35f, rim);
                    float alpha = Mathf.Clamp01(body * (1f - hole) + 0f);
                    px[j * n + i] = new Color(ink.r, ink.g, ink.b, alpha);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>The key and its tag, as one picture: a pale key on a ring, a dark leather tag
        /// with stitched edges and the mark pressed into it.</summary>
        static Texture2D KeyTagTexture(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            var rnd = new System.Random(9);
            var leather = new Color(0.16f, 0.16f, 0.17f, 1f);
            var brass = new Color(0.78f, 0.77f, 0.72f, 1f);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = i / (float)w, y = j / (float)h;     // y up
                    var c = new Color(0, 0, 0, 0);
                    // the tag: lower 62%, rounded corners, a hole at the top
                    float tx = Mathf.Abs(x - 0.5f), ty = y;
                    bool inTag = ty < 0.62f && ty > 0.02f && tx < 0.42f && !(ty > 0.52f && Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.56f) * (y - 0.56f)) < 0.035f);
                    float corner = Mathf.Max(0f, tx - 0.34f), cy = Mathf.Max(0f, Mathf.Max(ty - 0.54f, 0.10f - ty));
                    if (corner * corner + cy * cy > 0.08f * 0.08f) inTag = false;
                    if (inTag)
                    {
                        float n = (float)rnd.NextDouble() * 0.05f;
                        c = leather + new Color(n, n, n, 0f);
                        // stitching just inside the edge
                        bool stitchBand = Mathf.Abs(tx - 0.38f) < 0.006f || Mathf.Abs(ty - 0.06f) < 0.004f || Mathf.Abs(ty - 0.58f) < 0.004f;
                        if (stitchBand && ((i + j) / 5) % 2 == 0) c = new Color(0.55f, 0.55f, 0.55f, 1f);
                    }
                    // the ring through the hole, and the key hanging above it
                    float rx = x - 0.5f, ry = y - 0.66f;
                    float rr = Mathf.Sqrt(rx * rx + ry * ry);
                    if (Mathf.Abs(rr - 0.075f) < 0.009f) c = brass;
                    // key: bow (a ring) at top, shank down to the ring, bit to the right
                    float bx = x - 0.5f, by = y - 0.9f;
                    float br = Mathf.Sqrt(bx * bx + by * by);
                    if (br < 0.075f && br > 0.035f) c = brass;
                    if (Mathf.Abs(x - 0.5f) < 0.016f && y > 0.73f && y < 0.83f) c = brass;
                    if (y > 0.74f && y < 0.79f && x > 0.5f && x < 0.56f && ((int)(y * 200) % 3 != 0)) c = brass;
                    px[j * w + i] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>The key full screen, held in a trembling hand's light: the tag's words lettered
        /// over the picture so they stay crisp at any size.</summary>
        void KeyGUI(float w, float h)
        {
            keyUi = Mathf.MoveTowards(keyUi, keyUiTarget, Time.unscaledDeltaTime * 1.6f);
            if (keyUi <= 0.001f) return;
            if (keyTex == null) keyTex = KeyTagTexture(256, 384);
            if (markTex == null) markTex = BankMark(128, new Color(0.74f, 0.74f, 0.74f));
            float a = keyUi;
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.72f * a));
            float bh = barsPx * (h / 720f);
            float ph = Mathf.Min(h - bh * 2f - 40f, 520f), pw = ph * 256f / 384f;
            float sway = Mathf.Sin(Time.unscaledTime * 0.9f) * 3f;
            var r = new Rect((w - pw) * 0.5f + sway, (h - ph) * 0.5f, pw, ph);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(r, keyTex);
            // the mark pressed into the tag
            float ms = pw * 0.34f;
            GUI.DrawTexture(new Rect(r.center.x - ms * 0.5f, r.y + ph * 0.44f, ms, ms), markTex);
            GUI.color = old;
            int capSize = cap.fontSize, bigSize = big.fontSize;
            var capAlign = cap.alignment;
            cap.alignment = TextAnchor.MiddleCenter;
            cap.fontSize = Mathf.RoundToInt(ph * 0.04f);
            cap.normal.textColor = new Color(0.82f, 0.82f, 0.8f, a);
            GUI.Label(new Rect(r.x, r.y + ph * 0.715f, pw, ph * 0.05f), Spaced("MERIDIAN TRUST"), cap);
            cap.fontSize = Mathf.RoundToInt(ph * 0.026f);
            cap.normal.textColor = new Color(0.62f, 0.62f, 0.6f, a);
            GUI.Label(new Rect(r.x, r.y + ph * 0.765f, pw, ph * 0.04f), Spaced("SAFE DEPOSIT"), cap);
            big.fontSize = Mathf.RoundToInt(ph * 0.075f);
            big.normal.textColor = new Color(0.86f, 0.86f, 0.84f, a);
            GUI.Label(new Rect(r.x, r.y + ph * 0.8f, pw, ph * 0.09f), "Nº 417", big);
            cap.fontSize = capSize; big.fontSize = bigSize; cap.alignment = capAlign;
        }
    }
}
