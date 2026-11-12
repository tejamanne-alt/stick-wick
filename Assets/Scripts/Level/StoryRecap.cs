using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using SKey = UnityEngine.InputSystem.Key;
#endif

namespace ProjectInk
{
    /// <summary>THE WHOLE STORY: the timeline recap after the end title (and from the main menu). The
    /// truth in the order it happened, from the boy Voss pulled off the street to the dawn he came home,
    /// each moment a card: a still, when, what happened in plain words, the lines that connect the dots,
    /// and the threads it carries (THE BOOK, V, THE PROMISE, THE RIBBON, THE WALKIE, SILAS, HARROW). Then
    /// the chain of cause and effect, drawn link by link. Its own score: music_recap
    /// (tools/audio/make_score.py), five movements that follow the cards.
    ///
    /// Tap (or the right half / D / → / Space) for the next card, the left half (or A / ←) to go back; it
    /// moves on by itself otherwise.</summary>
    public partial class StoryIntro
    {
        /// <summary>Set by the main menu: open the epilogue's scene straight on the recap.</summary>
        public static bool RecapOnly;

        struct RecapCard
        {
            public string when, title, still, note;
            public string[] lines;      // "SPEAKER|line"
            public string[] threads;
            public float secs;
            public RecapCard(string when, string title, string still, string note, string[] lines, string[] threads, float secs = 11f)
            { this.when = when; this.title = title; this.still = still; this.note = note; this.lines = lines; this.threads = threads; this.secs = secs; }
        }

        static readonly RecapCard[] Recap =
        {
            new RecapCard("TWENTY YEARS AGO", "THE STUDENT", "Story/Memories/vphoto",
                "VOSS pulled him off the street at nineteen and raised him as a killer: how to fight, how to wait, how to never miss. Voss had a son of his own, SILAS, who grew up in his shadow.",
                new[] { "V.|To my best student.", "SILAS|Every day of my life it was you. \"He'd have done it cleaner.\"" }, new[] { "V", "SILAS" }),
            new RecapCard("FIFTEEN YEARS", "THE BEST OF THEM", "@book",
                "Forty-one jobs. Every order in Voss's own hand, signed \"V.\" He wrote every one of them down: names, dates, money. THE BOOK.",
                new[] { "YOU|Forty-one names. And every order signed... \"V.\"" }, new[] { "THE BOOK", "V" }),
            new RecapCard("EIGHT YEARS AGO", "ELENA", "Story/Memories/wed_3",
                "He met ELENA. They married. She never asked what he did; she asked him for one thing.",
                new[] { "HER|Promise me you'll always come home." }, new[] { "THE PROMISE" }),
            new RecapCard("SIX YEARS AGO", "\"I'M OUT, VOSS.\"", "Story/Flash/voss_1",
                "MAYA was born. He laid his gun on Voss's desk and walked away from the life.",
                new[] { "YOU|I'm out, Voss." }, new[] { "V", "THE PROMISE" }),
            new RecapCard("SIX YEARS AGO", "THE DEAL: BOX 417", "@box",
                "He locked THE BOOK in box 417 at Meridian Trust. Only his living hand opens it. If he dies, his lawyer takes the box, and the book goes to the police. That is what kept his family alive for six years: Voss could not touch him.",
                new[] { "MR. PELL|If anything happens to you, the box goes to your lawyer. Your instructions, sir.", "VOSS|Six years I lived on your mercy." }, new[] { "THE BOOK" }),
            new RecapCard("LAST WEEK", "THE DETECTIVE", "@card",
                "DETECTIVE HARROW of Organised Crime came to the house, twice. He knew the book existed. Elena wanted him to call back. Voss heard about it, and was afraid the book would buy him a new life while Voss went to prison.",
                new[] { "HER|He came again. Says you'd be safe if you talk. Call him?", "V.|A detective at your door, son. You know what that looks like to me." }, new[] { "HARROW", "V" }),
            new RecapCard("LAST NIGHT · 11 PM", "THE TRAP", "Story/Flash/voss_3",
                "Voss asked for one last meeting at the warehouse. A pipe from behind. They needed him alive, because only his hand opens the box, and his death would send the book to the police. The plan: walk him into the bank at nine, take the book, then finish him. But the blow took his memory.",
                new[] { "V.|Only his hand opens 417. Keep him alive till nine. Get the book. Then finish him." }, new[] { "V", "THE BOOK" }, 12f),
            new RecapCard("LAST NIGHT · THE SAME HOUR", "THE ORDER", "Story/Flash/voss_2",
                "At his window, Voss gave a careful order for the house: Elena and Maya, alive. Leverage, to make him open the box.",
                new[] { "VOSS|Bring them to me. All of them. Alive." }, new[] { "V" }),
            new RecapCard("LAST NIGHT · 14 ALDER LANE", "SILAS", "Story/Flash/elena_2",
                "Silas disobeyed. Elena ran for the back door with Maya, pushed the walkie-talkie into her pocket and sent her to hide. Silas shot her. Not because he had to. Because his father loved the hero more.",
                new[] { "SILAS|He said bring her. ...I didn't feel like it.", "VOSS|I told him to bring her. He never listened. Not once in his life." }, new[] { "SILAS", "THE WALKIE" }, 12f),
            new RecapCard("LAST NIGHT · THE PARK", "SHE LEFT A TRAIL", "Story/Flash/taken_1",
                "Maya hid under the slide. Her walkie crackled and gave her away. The Handler's men dragged her to the white van; on the way she dropped her red ribbon, so her papa could find the way.",
                new[] { "MAYA|Mama said leave a trail if you're lost." }, new[] { "THE RIBBON", "THE WALKIE" }),
            new RecapCard("CHAPTER 1 · 3:12 AM", "THE HITMAN", "Story/Flash/flash_3",
                "He woke on a wet floor with no name. He fought his way out, and on the Foreman he found his own key and Voss's note. Only his hand opens it. So that was why he was still alive.",
                new[] { "THE FOREMAN|The old man wants his book back. Nine o'clock, you open a box for him.", "YOU|If my hand opens it... then I'll open it first." }, new[] { "THE BOOK", "V" }),
            new RecapCard("CHAPTER 2 · 9:00 AM", "THE RING", "Story/Memories/wed_5",
                "He opened box 417 himself: passports for a family of three, Elena's ring, THE BOOK, and a letter in his own hand: \"They'll come for E. and M. 14 Alder Lane. Get them out.\" The clerk was already calling Voss. The book was out of the box now, and that changed everything.",
                new[] { "CLERK|Yes, Mr. V. He took it with him.", "THE DRIVER|We got there first." }, new[] { "THE BOOK", "V", "THE PROMISE" }, 12f),
            new RecapCard("CHAPTER 3 · DUSK", "THE DAUGHTER", "Story/Memories/maya_3",
                "Home, too late. Elena on the kitchen floor. Silas had been sent back to clean up his own mess. The photo of V behind the family photo. Voss called his son's phone; Silas lunged with a scalpel; the hero killed him with the line still open.",
                new[] { "SILAS|They always come home.", "V.|...Silas? Silas!", "V.|Bring me the book. Then we're even." }, new[] { "SILAS", "V", "THE WALKIE" }, 12f),
            new RecapCard("CHAPTER 4 · NIGHT", "THE PROMISE", "Story/Memories/ribbon",
                "Her ribbon in the grass. Her voice on the walkie, from the van at the end of the park. He fought all the way to it, and missed her by seconds. The Handler gave him the place: Kessel Hill.",
                new[] { "MAYA|I knew you'd answer. Mama said you always answer.", "YOU|I'm not his son. I'm hers." }, new[] { "THE RIBBON", "THE WALKIE" }),
            new RecapCard("CHAPTER 5 · THE ROAD", "OVER AND OUT", "@road",
                "The drive up the hill, her voice coming back as he climbed. Hot chocolate, counting the thunder, the music box song sung wrong. And the truth he saw in the floodlights: with the book out of the box, Voss didn't need him alive any more. It was never a trade.",
                new[] { "MAYA|Papa? Are you a bad man?", "YOU|...It was never a trade." }, new[] { "THE WALKIE", "THE BOOK" }),
            new RecapCard("CHAPTER 6 · THE STORM", "THE LAST ROOF", "Story/Flash/voss_2",
                "Every man on the hill wanted the son's chair. He went through all of them. On the roof Voss held Maya over the edge for the book; then he drew the blade from his cane. Voss fell with the stone of his own fortress.",
                new[] { "VOSS|...I know. And I still loved him. That's what fathers do.", "YOU|No. I came home." }, new[] { "V", "SILAS", "THE BOOK" }, 12f),
            new RecapCard("CHAPTER 7 · DAWN", "HOME", "@grave",
                "At the bottom of the hill, Harrow let them go. At Elena's grave, Maya left her walkie so Mama could hear them. He kept the book, locked in a drawer under her photo. He kept his promise.",
                new[] { "HARROW|...Then I never saw you. Take her home.", "YOU|I came home." }, new[] { "THE PROMISE", "THE WALKIE", "HARROW" }, 12f),
        };

        static readonly string[] Chain =
        {
            "THE BOOK IN BOX 417", "KEPT HIS FAMILY SAFE", "HARROW CAME ASKING", "VOSS STRUCK FIRST",
            "SILAS DISOBEYED", "ELENA DIED", "MAYA BECAME LEVERAGE", "THE BOOK LEFT THE BOX",
            "NOTHING KEPT HIM ALIVE", "THE FORTRESS", "HE CAME HOME",
        };

        int recapIdx = -1;
        float recapCardAt, recapChainAt = -1f, recapEndAt = -1f;
        bool recapOn, recapSkip;
        Texture2D[] recapTex;

        IEnumerator RecapSequence()
        {
            recapOn = true;
            recapSkip = false;
            if (recapTex == null)
            {
                recapTex = new Texture2D[Recap.Length];
                for (int i = 0; i < Recap.Length; i++)
                    if (!Recap[i].still.StartsWith("@")) recapTex[i] = Resources.Load<Texture2D>(Recap[i].still);
            }
            Sound.Music("recap", 0.75f, false, 2.0f);
            barsPx = 0f;
            recapIdx = 0;
            recapCardAt = Time.unscaledTime;
            yield return RealWait(0.5f);
            pointerWasDown = PointerDown();
            while (recapIdx < Recap.Length && !recapSkip)
            {
                bool down = PointerDown();
                bool tap = down && !pointerWasDown;
                pointerWasDown = down;
                bool back = KeyTapped(SKey.A, SKey.LeftArrow) || (tap && PointerPos().x < Screen.width * 0.3f);
                bool next = KeyTapped(SKey.D, SKey.RightArrow, SKey.Space, SKey.Enter) || (tap && PointerPos().x >= Screen.width * 0.3f);
                if (Time.unscaledTime - recapCardAt > Recap[recapIdx].secs) next = true;
                if (back && recapIdx > 0) { recapIdx--; recapCardAt = Time.unscaledTime; Sound.Play(Sfx.Cloth, 0.4f, null, 1.3f); }
                else if (next) { recapIdx++; recapCardAt = Time.unscaledTime; Sound.Play(Sfx.Cloth, 0.4f, null, 1.5f); }
                yield return null;
            }
            // the chain: why it happened, link by link
            recapIdx = Recap.Length;
            recapChainAt = Time.unscaledTime;
            float hold = 0f;
            while (hold < 2.2f + Chain.Length * 1.15f + 5f && !recapSkip)
            {
                hold += Time.unscaledDeltaTime;
                bool down = PointerDown();
                if (down && !pointerWasDown && hold > 3f) hold = Mathf.Max(hold, 2.2f + Chain.Length * 1.15f + 4.4f);
                pointerWasDown = down;
                yield return null;
            }
            recapChainAt = -1f;
            recapEndAt = Time.unscaledTime;
            yield return RealWait(6f);
            recapOn = false;
            recapIdx = -1;
            recapEndAt = -1f;
            Sound.MusicStop(3f);
        }

        // ---------------------------------------------------------------- on screen

        void RecapGUI(float w, float h)
        {
            if (!recapOn) return;
            Fill(new Rect(0, 0, w, h), new Color(0.015f, 0.015f, 0.02f, 1f));
            var red = new Color(0.8f, 0.06f, 0.07f);
            // skip
            var skipR = new Rect(w - 170, 18, 150, 40);
            btn.fontSize = 18;
            btn.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            if (GUI.Button(skipR, Spaced("SKIP"), btn)) recapSkip = true;
            if (Event.current.type == EventType.Repaint) GestureInput.BlockGui(skipR, scale);
            btn.fontSize = 30;
            btn.normal.textColor = Color.white;
            // the heading
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = 18;
            cap.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(92, 18, 600, 30), Spaced("THE WHOLE STORY"), cap);          // clear of the pause button (top left)
            if (recapIdx >= 0 && recapIdx < Recap.Length) RecapCardGUI(w, h, Recap[recapIdx], recapTex[recapIdx], Time.unscaledTime - recapCardAt);
            else if (recapChainAt > 0f) ChainGUI(w, h, Time.unscaledTime - recapChainAt);
            else if (recapEndAt > 0f)
            {
                float t0 = Time.unscaledTime - recapEndAt;
                float a = Mathf.Clamp01(t0 / 1.2f) * Mathf.Clamp01((6f - t0) / 1.0f);
                int bs = big.fontSize;
                big.fontSize = 84;
                big.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(0, h * 0.38f, w, 100), Spaced("HE CAME HOME"), big);
                big.fontSize = bs;
                Fill(new Rect(w * 0.5f - 140 * a, h * 0.38f + 104, 280 * a, 3), new Color(red.r, red.g, red.b, a));
            }
            // the timeline along the bottom
            int n = Recap.Length;
            float x0 = 60f, x1 = w - 60f, y = h - 46f;
            Fill(new Rect(x0, y, x1 - x0, 2), new Color(1f, 1f, 1f, 0.25f));
            int before = 10;                // cards before chapter 1: the past
            float split = Mathf.Lerp(x0, x1, (before - 0.5f) / (n - 1));
            cap.fontSize = 14;
            cap.alignment = TextAnchor.MiddleCenter;
            cap.normal.textColor = new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(new Rect(x0, y + 8, split - x0, 20), Spaced("BEFORE"), cap);
            GUI.Label(new Rect(split, y + 8, x1 - split, 20), Spaced("THE STORY"), cap);
            Fill(new Rect(split - 1, y - 8, 2, 18), new Color(1f, 1f, 1f, 0.35f));
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)(n - 1));
                bool cur = i == recapIdx, done = recapIdx > i || recapIdx >= n;
                float s = cur ? 12f : 7f;
                Fill(new Rect(x - s * 0.5f, y + 1 - s * 0.5f, s, s), cur ? red : new Color(1f, 1f, 1f, done ? 0.75f : 0.3f));
            }
            cap.fontSize = 20;
            cap.alignment = TextAnchor.MiddleLeft;
        }

        void RecapCardGUI(float w, float h, RecapCard c, Texture2D tex, float t0)
        {
            var red = new Color(0.8f, 0.06f, 0.07f);
            float a = Mathf.Clamp01(t0 / 0.6f);
            // the picture: left, slowly pushing in
            float pw = w * 0.52f, ph = Mathf.Min(h - 170f, pw * 9f / 16f);
            pw = ph * 16f / 9f;
            var pr = new Rect(40f, (h - ph) * 0.5f - 10f, pw, ph);
            Fill(new Rect(pr.x - 2, pr.y - 2, pr.width + 4, pr.height + 4), new Color(1f, 1f, 1f, 0.12f * a));
            GUI.BeginGroup(pr);
            var o = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            float z = 1.02f + 0.05f * Mathf.Clamp01(t0 / c.secs);
            var ir = new Rect(-(pw * (z - 1f)) * 0.5f, -(ph * (z - 1f)) * 0.5f, pw * z, ph * z);
            if (tex != null) GUI.DrawTexture(ir, tex, ScaleMode.ScaleAndCrop);
            else RecapDrawn(c.still, new Rect(0, 0, pw, ph), a);
            GUI.color = o;
            GUI.EndGroup();
            // the words: right
            float tx = pr.xMax + 34f, tw = w - tx - 40f;
            float y = pr.y;
            cap.alignment = TextAnchor.UpperLeft;
            cap.fontSize = 18;
            cap.normal.textColor = new Color(red.r + 0.1f, red.g + 0.1f, red.b + 0.1f, a);
            GUI.Label(new Rect(tx, y, tw, 26), Spaced(c.when), cap);
            y += 30f;
            int bs = big.fontSize;
            big.fontSize = 42;
            big.alignment = TextAnchor.UpperLeft;
            big.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(tx, y, tw, 56), c.title, big);
            big.alignment = TextAnchor.MiddleCenter;
            big.fontSize = bs;
            y += 60f;
            Fill(new Rect(tx, y, 90f * a, 3), red);
            y += 14f;
            sub.wordWrap = true;
            sub.alignment = TextAnchor.UpperLeft;
            int ss = sub.fontSize;
            sub.fontSize = 20;
            sub.normal.textColor = new Color(1f, 1f, 1f, 0.82f * Mathf.Clamp01((t0 - 0.4f) / 0.8f));
            float nh = sub.CalcHeight(new GUIContent(c.note), tw);
            GUI.Label(new Rect(tx, y, tw, nh), c.note, sub);
            y += nh + 14f;
            // the lines that connect the dots, one after another
            for (int i = 0; i < c.lines.Length; i++)
            {
                var parts = c.lines[i].Split('|');
                float la = Mathf.Clamp01((t0 - 1.6f - i * 1.6f) / 0.6f);
                if (la <= 0f) break;
                string who = parts[0], line = parts.Length > 1 ? parts[1] : "";
                var col = who == "YOU" ? YouBar : who == "HER" || who == "MAYA" ? OffColor : BossColor;
                Fill(new Rect(tx, y + 2, 3, 40), new Color(col.r, col.g, col.b, la));
                cap.fontSize = 15;
                cap.normal.textColor = new Color(col.r, col.g, col.b, la);
                GUI.Label(new Rect(tx + 12, y, tw - 12, 20), Spaced(who), cap);
                sub.fontSize = 19;
                sub.normal.textColor = new Color(1f, 1f, 1f, la);
                string q = "“" + line + "”";
                float qh = sub.CalcHeight(new GUIContent(q), tw - 12);
                GUI.Label(new Rect(tx + 12, y + 20, tw - 12, qh), q, sub);
                y += 26f + qh + 10f;
            }
            sub.fontSize = ss;
            sub.alignment = TextAnchor.MiddleCenter;
            // the threads it carries
            float cx = tx;
            cap.fontSize = 13;
            for (int i = 0; i < c.threads.Length; i++)
            {
                string label = Spaced(c.threads[i]);
                float lw = cap.CalcSize(new GUIContent(label)).x + 20f;
                var r = new Rect(cx, pr.yMax - 26f, lw, 24f);
                bool hot = c.threads[i] == "THE RIBBON";
                Fill(r, hot ? new Color(red.r, red.g, red.b, 0.85f * a) : new Color(1f, 1f, 1f, 0.12f * a));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f * a);
                GUI.Label(r, label, cap);
                cx += lw + 8f;
            }
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = 20;
            // the card's own clock
            Fill(new Rect(40f, pr.yMax + 10f, pw * Mathf.Clamp01(t0 / c.secs), 2), new Color(1f, 1f, 1f, 0.35f));
        }

        /// <summary>The cards with no still: drawn (the book, the box, the card, the road, the grave).</summary>
        void RecapDrawn(string what, Rect r, float a)
        {
            Fill(r, new Color(0.08f, 0.08f, 0.09f, a));
            if (markerFont == null) markerFont = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
            var st = new GUIStyle(sub) { font = markerFont != null ? markerFont : sub.font, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            switch (what)
            {
                case "@book":
                    {
                        var pg = new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.1f, r.width * 0.76f, r.height * 0.8f);
                        Fill(new Rect(pg.x - 8, pg.y - 6, pg.width + 16, pg.height + 12), new Color(0.02f, 0.02f, 0.025f, a));
                        Fill(pg, new Color(0.84f, 0.83f, 0.79f, a));
                        Fill(new Rect(pg.center.x - 1, pg.y, 2, pg.height), new Color(0f, 0f, 0f, 0.4f * a));
                        st.fontSize = Mathf.RoundToInt(pg.height * 0.055f);
                        st.normal.textColor = new Color(0.1f, 0.1f, 0.12f, a);
                        string[] rows = { "K. ADLER  ·  40  ·  V.", "R. MOSS  ·  25  ·  V.", "T. VANCE  ·  30  ·  V.", "S. ORLOV  ·  50  ·  V.", "P. KANE  ·  35  ·  V.", "M. HOLT  ·  20  ·  V." };
                        for (int i = 0; i < rows.Length; i++) GUI.Label(new Rect(pg.x + pg.width * 0.04f, pg.y + pg.height * (0.08f + i * 0.14f), pg.width * 0.46f, pg.height * 0.1f), rows[i], st);
                        st.normal.textColor = new Color(0.62f, 0.05f, 0.06f, a);
                        GUI.Label(new Rect(pg.center.x + pg.width * 0.04f, pg.y + pg.height * 0.78f, pg.width * 0.46f, pg.height * 0.1f), "41 — I'M OUT.", st);
                        break;
                    }
                case "@box":
                    {
                        var b = new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.2f, r.width * 0.5f, r.height * 0.6f);
                        for (int i = 0; i < 5; i++) for (int j = 0; j < 4; j++)
                        {
                            var cell = new Rect(r.x + r.width * (0.06f + i * 0.18f), r.y + r.height * (0.08f + j * 0.22f), r.width * 0.16f, r.height * 0.19f);
                            Fill(cell, new Color(0.3f, 0.3f, 0.31f, a));
                            bool it = i == 2 && j == 1;
                            cap.alignment = TextAnchor.MiddleCenter;
                            cap.fontSize = Mathf.RoundToInt(r.height * 0.06f);
                            cap.normal.textColor = it ? new Color(1f, 1f, 1f, a) : new Color(0.6f, 0.6f, 0.6f, a);
                            GUI.Label(cell, (415 + i + j * 5 - 5).ToString(), cap);
                            if (it) Fill(new Rect(cell.x, cell.yMax - 3, cell.width, 3), new Color(0.8f, 0.06f, 0.07f, a));
                        }
                        cap.alignment = TextAnchor.MiddleLeft;
                        cap.fontSize = 20;
                        break;
                    }
                case "@card":
                    {
                        var c = new Rect(r.x + r.width * 0.12f, r.y + r.height * 0.28f, r.width * 0.42f, r.height * 0.4f);
                        Fill(c, new Color(0.93f, 0.93f, 0.91f, a));
                        cap.alignment = TextAnchor.MiddleCenter;
                        cap.fontSize = Mathf.RoundToInt(c.height * 0.14f);
                        cap.normal.textColor = new Color(0.05f, 0.05f, 0.06f, a);
                        GUI.Label(new Rect(c.x, c.y + c.height * 0.3f, c.width, c.height * 0.2f), "DET. J. HARROW", cap);
                        cap.fontSize = Mathf.RoundToInt(c.height * 0.08f);
                        GUI.Label(new Rect(c.x, c.y + c.height * 0.55f, c.width, c.height * 0.15f), Spaced("ORGANISED CRIME"), cap);
                        var n = new Rect(r.x + r.width * 0.5f, r.y + r.height * 0.22f, r.width * 0.4f, r.height * 0.52f);
                        Fill(n, new Color(0.86f, 0.85f, 0.8f, a));
                        st.fontSize = Mathf.RoundToInt(n.height * 0.09f);
                        st.wordWrap = true;
                        st.normal.textColor = new Color(0.1f, 0.1f, 0.13f, a);
                        GUI.Label(new Rect(n.x + 10, n.y + 6, n.width - 20, n.height - 12), "He came again.\nSays you'd be safe\nif you talk.\nCall him?\n   — E.", st);
                        cap.alignment = TextAnchor.MiddleLeft;
                        cap.fontSize = 20;
                        break;
                    }
                case "@road":
                    {
                        // a windscreen at night: the dash glow, the walkie on the empty seat, the fortress in the rain
                        Fill(new Rect(r.x, r.y, r.width, r.height * 0.62f), new Color(0.12f, 0.13f, 0.15f, a));
                        for (int i = 0; i < 40; i++) Fill(new Rect(r.x + (i * 37 % 97) / 97f * r.width, r.y + (i * 53 % 89) / 89f * r.height * 0.6f, 1, 9), new Color(0.8f, 0.85f, 0.9f, 0.3f * a));
                        Fill(new Rect(r.x + r.width * 0.42f, r.y + r.height * 0.18f, r.width * 0.16f, r.height * 0.14f), new Color(0.04f, 0.04f, 0.05f, a));
                        for (int i = 0; i < 6; i++) Fill(new Rect(r.x + r.width * (0.44f + i * 0.02f), r.y + r.height * 0.22f, 3, 4), new Color(1f, 0.92f, 0.75f, a));
                        Fill(new Rect(r.x, r.y + r.height * 0.62f, r.width, r.height * 0.38f), new Color(0.03f, 0.03f, 0.035f, a));
                        Fill(new Rect(r.x + r.width * 0.68f, r.y + r.height * 0.7f, r.width * 0.05f, r.height * 0.13f), new Color(0.82f, 0.82f, 0.8f, a));
                        Fill(new Rect(r.x + r.width * 0.71f, r.y + r.height * 0.66f, 2, r.height * 0.05f), new Color(0.82f, 0.82f, 0.8f, a));
                        break;
                    }
                case "@grave":
                    {
                        Fill(new Rect(r.x, r.y, r.width, r.height * 0.6f), new Color(0.78f, 0.77f, 0.74f, a));
                        Fill(new Rect(r.x, r.y + r.height * 0.6f, r.width, r.height * 0.4f), new Color(0.35f, 0.36f, 0.34f, a));
                        var s = new Rect(r.x + r.width * 0.4f, r.y + r.height * 0.3f, r.width * 0.2f, r.height * 0.38f);
                        Fill(s, new Color(0.55f, 0.55f, 0.54f, a));
                        cap.alignment = TextAnchor.MiddleCenter;
                        cap.fontSize = Mathf.RoundToInt(s.height * 0.12f);
                        cap.normal.textColor = new Color(0.2f, 0.2f, 0.22f, a);
                        GUI.Label(new Rect(s.x, s.y + s.height * 0.2f, s.width, s.height * 0.2f), "ELENA", cap);
                        Fill(new Rect(s.center.x - r.width * 0.012f, s.y - r.height * 0.07f, r.width * 0.024f, r.height * 0.07f), new Color(0.82f, 0.82f, 0.8f, a));
                        cap.alignment = TextAnchor.MiddleLeft;
                        cap.fontSize = 20;
                        break;
                    }
            }
        }

        void ChainGUI(float w, float h, float t0)
        {
            var red = new Color(0.8f, 0.06f, 0.07f);
            float a0 = Mathf.Clamp01(t0 / 0.8f);
            int bs = big.fontSize;
            big.fontSize = 46;
            big.normal.textColor = new Color(1f, 1f, 1f, a0);
            GUI.Label(new Rect(0, 60, w, 60), Spaced("WHY IT HAPPENED"), big);
            big.fontSize = bs;
            Fill(new Rect(w * 0.5f - 80 * a0, 124, 160 * a0, 3), red);
            // two rows of links, snake-wise
            int n = Chain.Length, perRow = 6;
            float bw = Mathf.Min(200f, (w - 120f) / perRow - 26f), bh = 64f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Clamp01((t0 - 1.2f - i * 1.15f) / 0.5f);
                if (a <= 0f) break;
                int row = i / perRow, col = i % perRow;
                if (row == 1) col = perRow - 1 - col;
                float span = perRow * (bw + 26f) - 26f;
                float x = (w - span) * 0.5f + col * (bw + 26f), y = h * 0.34f + row * 150f;
                var r = new Rect(x, y, bw, bh);
                bool last = i == n - 1;
                Fill(r, last ? new Color(red.r, red.g, red.b, a) : new Color(1f, 1f, 1f, 0.1f * a));
                Fill(new Rect(r.x, r.y, 3, r.height), new Color(red.r, red.g, red.b, a));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.fontSize = 15;
                cap.wordWrap = true;
                cap.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(r.x + 8, r.y, r.width - 12, r.height), c(Chain[i]), cap);
                cap.wordWrap = false;
                // the arrow to the next
                if (i < n - 1 && t0 > 1.2f + (i + 1) * 1.15f)
                {
                    int nr = (i + 1) / perRow;
                    if (nr == row)
                    {
                        float ax = row == 0 ? r.xMax + 4 : r.x - 22, ay = r.center.y;
                        Fill(new Rect(ax, ay - 1, 18, 2), new Color(1f, 1f, 1f, 0.6f));
                    }
                    else Fill(new Rect(r.center.x - 1, r.yMax + 4, 2, 150f - bh - 8f), new Color(1f, 1f, 1f, 0.6f));
                }
            }
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = 20;
            string c(string s0) => s0;
        }
    }
}
