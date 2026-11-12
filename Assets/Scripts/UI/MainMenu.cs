using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectInk
{
    /// <summary>The title screen. Night, rain on the lens and the odd flash of lightning behind
    /// STICK WICK; two choices:
    ///   STORY MODE  the prologue (the warehouse: he wakes with no memory)
    ///   FIGHT MODE  the tower: endless floors, the fight game as it was.
    /// Drawn with IMGUI like the rest of the interface.</summary>
    public class MainMenu : MonoBehaviour
    {
        GUIStyle title, tagline, btn, btnSub;
        Texture2D rain;
        float flashAt, flash, t;

        void Start()
        {
            Time.timeScale = 1f;
            TimeController.Paused = false;
            AudioListener.pause = false;
            Sound.ResetAmbience();
            Sound.Ambience(1.6f, 0.3f, 1400f);
            Sound.Tension(false);
            flashAt = 2.5f;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t > flashAt)
            {
                flash = 1f;
                flashAt = t + Random.Range(6f, 11f);
                Sound.PlayLater(Sfx.Thunder, Random.Range(0.2f, 0.7f), 0.5f);
            }
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
#if ENABLE_INPUT_SYSTEM
            // a tap: pressed and released close together (a finger or a mouse), read straight from the
            // Input System, so the menu never depends on IMGUI's touch emulation
            var p = Pointer.current;
            if (p != null)
            {
                bool down = p.press.isPressed;
                var pos = p.position.ReadValue();
                if (down && !pressing) { pressing = true; pressAt = pos; }
                else if (!down && pressing)
                {
                    pressing = false;
                    if ((pos - pressAt).magnitude < Mathf.Max(24f, Screen.height * 0.04f)) tapScreen = pos;
                }
            }
#endif
        }

        static string notice;
        static float noticeAt = -10f;

        /// <summary>Open a scene, or say plainly why not (a chapter missing from this build: its
        /// scene must be in the build's scene list; Stick Wick → Add All Scenes To The Build).</summary>
        static void Go(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                notice = scene.Replace("_", " ").ToUpperInvariant() + " IS NOT IN THIS BUILD  ·  IN UNITY: STICK WICK  >  ADD ALL SCENES TO THE BUILD, THEN BUILD AGAIN";
                noticeAt = Time.unscaledTime;
                Debug.LogError("[Stick Wick] scene '" + scene + "' is not in the build's scene list");
                return;
            }
            SceneManager.LoadScene(scene);
        }

        bool pressing;
        Vector2 pressAt;
        Vector2? tapScreen, tapGui;

        /// <summary>Was this rect tapped this frame? Only a live button takes it (a button under an
        /// open panel never does), and a tap is taken by one button at most.</summary>
        bool Clicked(Rect r, bool live)
        {
#if ENABLE_INPUT_SYSTEM
            if (!live || !tapGui.HasValue || !r.Contains(tapGui.Value)) return false;
            tapGui = null;
            return true;
#else
            return GUI.Button(r, GUIContent.none, GUIStyle.none) && live;
#endif
        }

        void Styles()
        {
            if (title != null) return;
            var display = Resources.Load<Font>("Fonts/BebasNeue-Regular");
            var body = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
            title = new GUIStyle { font = display, fontSize = 130, alignment = TextAnchor.MiddleCenter };
            tagline = new GUIStyle { font = body, fontSize = 22, alignment = TextAnchor.MiddleCenter };
            btn = new GUIStyle { font = display, fontSize = 40, alignment = TextAnchor.MiddleCenter };
            btnSub = new GUIStyle { font = body, fontSize = 18, alignment = TextAnchor.MiddleCenter };
            rain = new Texture2D(256, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[256 * 256];
            var r = new System.Random(4);
            for (int i = 0; i < 60; i++)
            {
                int x0 = r.Next(256), y0 = r.Next(256), len = 12 + r.Next(24);
                for (int k = 0; k < len; k++)
                {
                    int x = ((x0 + k / 4) % 256 + 256) % 256, y = ((y0 - k) % 256 + 256) % 256;
                    px[y * 256 + x] = new Color32(255, 255, 255, (byte)(180 * (k / (float)len)));
                }
            }
            rain.SetPixels32(px); rain.Apply();
        }

        static string Spaced(string s) => string.Join(" ", s.ToCharArray());

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        void OnGUI()
        {
            Styles();
            float boost = Application.isMobilePlatform ? 1.35f : 1.2f;
            float scale = Screen.height / 720f * boost;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            if (tapScreen.HasValue) { tapGui = new Vector2(tapScreen.Value.x / scale, (Screen.height - tapScreen.Value.y) / scale); tapScreen = null; }

            // night, with the storm in it
            Fill(new Rect(0, 0, w, h), new Color(0.012f, 0.014f, 0.024f));
            Fill(new Rect(0, h * 0.62f, w, h * 0.38f), new Color(0.06f, 0.04f, 0.08f, 0.6f));
            Fill(new Rect(0, h * 0.78f, w, h * 0.22f), new Color(0.16f, 0.08f, 0.05f, 0.35f));
            var old = GUI.color;
            float tt = Time.unscaledTime;
            GUI.color = new Color(0.8f, 0.85f, 1f, 0.32f);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), rain, new Rect(tt * 0.35f, tt * 2.4f, w / 300f, h / 300f));
            GUI.color = old;
            if (flash > 0.01f) Fill(new Rect(0, 0, w, h), new Color(0.6f, 0.7f, 1f, 0.35f * flash * flash));

            title.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(new Rect(4, h * 0.2f + 5, w, 140), Spaced("STICK WICK"), title);
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(0, h * 0.2f, w, 140), Spaced("STICK WICK"), title);
            Fill(new Rect(w * 0.5f - 190, h * 0.2f + 138, 380, 4), new Color(0.80f, 0.06f, 0.07f));
            tagline.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            GUI.Label(new Rect(0, h * 0.2f + 150, w, 30), "HE DOESN'T REMEMBER WHO HE IS.   THEY DO.", tagline);

            float bw = Mathf.Min(360f, w * 0.38f), bh = 96f, gap = 30f, y = h * 0.62f;
            var story = new Rect(w * 0.5f - bw - gap * 0.5f, y, bw, bh);
            var fight = new Rect(w * 0.5f + gap * 0.5f, y, bw, bh);
            // while the test panel is open it owns every click: the buttons under it must not take one
            // (IMGUI gives a click to the first button drawn under the pointer, and these come first)
            bool open = DevShortcuts && testOpen;
            GUI.enabled = !open;
            if (Button(story, "STORY MODE", "CHAPTER 1  ·  THE WAREHOUSE", true, !open)) { StoryIntro.NewRun(); Go("Story_Warehouse"); }
            if (Button(fight, "FIGHT MODE", "THE TOWER  ·  ENDLESS FLOORS", false, !open)) Go("Prototype");
            var whole = new Rect(w * 0.5f - 150, y - 56, 300, 38);
            Fill(whole, new Color(1f, 1f, 1f, whole.Contains(Event.current.mousePosition) ? 0.14f : 0.07f));
            btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(whole, "THE WHOLE STORY  ·  TIMELINE", btnSub);
            if (Clicked(whole, !open)) Epilogue(5);
            // TEMPORARY, for testing: jump straight to any checkpoint (set DevShortcuts false to remove)
            if (DevShortcuts)
            {
                var dev = new Rect(w * 0.5f - 150, y + bh + 22, 300, 40);
                Fill(dev, new Color(1f, 0.75f, 0.1f, dev.Contains(Event.current.mousePosition) || testOpen ? 0.24f : 0.12f));
                btnSub.normal.textColor = new Color(1f, 0.8f, 0.3f, 0.9f);
                GUI.Label(dev, "TEST  ·  CHECKPOINTS", btnSub);
                GUI.enabled = true;
                // the panel first: while it is open it takes every tap
                if (testOpen) TestPanel(w, h);
                else if (Clicked(dev, true)) testOpen = true;
            }
            GUI.enabled = true;
            if (notice != null && Time.unscaledTime - noticeAt < 8f)
            {
                var nr = new Rect(20, h - 70, w - 40, 50);
                Fill(nr, new Color(0.5f, 0.04f, 0.05f, 0.92f));
                btnSub.normal.textColor = Color.white;
                btnSub.wordWrap = true;
                GUI.Label(nr, notice, btnSub);
                btnSub.wordWrap = false;
            }
            if (Event.current.type == EventType.Repaint) tapGui = null;          // a tap lasts one frame
            GUI.matrix = Matrix4x4.identity;
        }

        bool testOpen;

        /// <summary>Every place in the story you can jump to, to test it without playing up to it.</summary>
        static readonly (string chapter, string label, System.Action go)[] Checkpoints =
        {
            ("CHAPTER 1  ·  THE WAREHOUSE", "Opening  ·  open your eyes, break the ties", () => Ch1(() => { })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "At the shutter (the training ground)", () => Ch1(() => { StoryIntro.Retry = true; StoryIntro.TrainedThisRun = false; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 1  ·  Loading bay", () => Ch1(() => Hall(0))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 2  ·  Racking aisle", () => Ch1(() => Hall(1))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 3  ·  Mezzanine", () => Ch1(() => Hall(2))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 4  ·  Crane bay", () => Ch1(() => Hall(3))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "TEST  ·  the katana (crane bay, swordsmen)", () => Ch1(() => { Hall(3); StoryIntro.KatanaTest = true; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "The Foreman (the fight)", () => Ch1(() => { Hall(3); StoryIntro.BossReached = true; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Ending  ·  \"the old man wants his book\"", () => Ch1(() => { StoryIntro.EscapeSeen = true; StoryIntro.TestEnding = 1; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Ending  ·  the search, the note signed V", () => Ch1(() => { StoryIntro.EscapeSeen = true; StoryIntro.TestEnding = 2; })),
            ("CHAPTER 2  ·  THE BANK", "Start  ·  the street", () => Ch2(0)),
            ("CHAPTER 2  ·  THE BANK", "At the counter", () => Ch2(1)),
            ("CHAPTER 2  ·  THE BANK", "The vault  ·  the standing instruction, the palm", () => Ch2(2)),
            ("CHAPTER 2  ·  THE BANK", "The box  ·  passports, ring, the book, the letter", () => Ch2(3)),
            ("CHAPTER 2  ·  THE BANK", "Walk out  ·  \"Yes, Mr. V\"", () => Ch2(4)),
            ("CHAPTER 2  ·  THE STREET", "\"The book. Hand it over.\"", () => Street(0)),
            ("CHAPTER 2  ·  THE STREET", "The market street", () => Street(1)),
            ("CHAPTER 2  ·  THE STREET", "Under the railway  ·  the train", () => Street(2)),
            ("CHAPTER 2  ·  THE STREET", "The crossroads", () => Street(3)),
            ("CHAPTER 2  ·  THE STREET", "The Driver (the fight)", () => Street(4)),
            ("CHAPTER 2  ·  THE STREET", "After the Driver  ·  the question", () => Street(5)),
            ("CHAPTER 3  ·  HOME", "Alder Lane  ·  the arrival, Harrow's card", () => Home(0)),
            ("CHAPTER 3  ·  HOME", "The study  ·  the photo of V", () => Home(1)),
            ("CHAPTER 3  ·  HOME", "The kitchen  ·  Elena", () => Home(2)),
            ("CHAPTER 3  ·  HOME", "The house fight (rage)  ·  her shelf", () => Home(3)),
            ("CHAPTER 3  ·  HOME", "Silas, the Cleaner (the fight)", () => Home(4)),
            ("CHAPTER 3  ·  HOME", "The kitchen chair  ·  the call", () => Home(5)),
            ("CHAPTER 3  ·  HOME", "The footprints  ·  the garden", () => Home(6)),
            ("CHAPTER 4  ·  THE BACKYARD", "Find her  ·  the footprints", () => Backyard(0)),
            ("CHAPTER 4  ·  THE BACKYARD", "The ribbon  ·  her voice  ·  the van", () => Backyard(1)),
            ("CHAPTER 4  ·  THE BACKYARD", "Reach her  ·  the torches", () => Backyard(2)),
            ("CHAPTER 4  ·  THE BACKYARD", "The fight  ·  lose her", () => Backyard(3)),
            ("CHAPTER 4  ·  THE BACKYARD", "The Handler (the fight)", () => Backyard(4)),
            ("CHAPTER 4  ·  THE BACKYARD", "The address  ·  the hill", () => Backyard(5)),
            ("CHAPTER 5  ·  THE ROAD", "The drive  ·  her voice", () => Road(0)),
            ("CHAPTER 5  ·  THE ROAD", "Counting the thunder", () => Road(1)),
            ("CHAPTER 5  ·  THE ROAD", "\"Are you a bad man?\"  ·  the song", () => Road(2)),
            ("CHAPTER 5  ·  THE ROAD", "The battery", () => Road(3)),
            ("CHAPTER 5  ·  THE ROAD", "The last hairpin  ·  never a trade", () => Road(4)),
            ("CHAPTER 6  ·  THE FORTRESS", "The storm  ·  the arrival", () => Fortress(0)),
            ("CHAPTER 6  ·  THE FORTRESS", "1 · The switchback road", () => Fortress(20)),
            ("CHAPTER 6  ·  THE FORTRESS", "2 · The great stair", () => Fortress(21)),
            ("CHAPTER 6  ·  THE FORTRESS", "3 · The barbican  ·  the chain", () => Fortress(22)),
            ("CHAPTER 6  ·  THE FORTRESS", "4 · The gate passage  ·  the windlass", () => Fortress(23)),
            ("CHAPTER 6  ·  THE FORTRESS", "5 · The wall walk  ·  the first katana", () => Fortress(24)),
            ("CHAPTER 6  ·  THE FORTRESS", "6 · The wall tower", () => Fortress(25)),
            ("CHAPTER 6  ·  THE FORTRESS", "7 · The courtyard  ·  the bell, the blackout", () => Fortress(26)),
            ("CHAPTER 6  ·  THE FORTRESS", "8 · Voss's gallery  ·  the photograph", () => Fortress(27)),
            ("CHAPTER 6  ·  THE FORTRESS", "9 · The great hall  ·  THE WARDEN", () => Fortress(28)),
            ("CHAPTER 6  ·  THE FORTRESS", "10 · The tower stair  ·  \"I see you!\"", () => Fortress(29)),
            ("CHAPTER 6  ·  THE FORTRESS", "11 · The observatory", () => Fortress(30)),
            ("CHAPTER 6  ·  THE FORTRESS", "The last roof  ·  Voss, the trade", () => Fortress(7)),
            ("CHAPTER 6  ·  THE FORTRESS", "Voss (the duel)", () => Fortress(8)),
            ("CHAPTER 6  ·  THE FORTRESS", "The fall  ·  her  ·  the dawn", () => Fortress(9)),
            ("CHAPTER 6  ·  THE FORTRESS", "The walk-out  ·  \"Over and out\"", () => Fortress(10)),
            ("CHAPTER 7  ·  HOME", "Harrow  ·  \"Then I never saw you\"", () => Epilogue(0)),
            ("CHAPTER 7  ·  HOME", "The grave", () => Epilogue(1)),
            ("CHAPTER 7  ·  HOME", "The swing", () => Epilogue(2)),
            ("CHAPTER 7  ·  HOME", "The drawer  ·  THE END", () => Epilogue(3)),
            ("THE WHOLE STORY", "The timeline recap", () => Epilogue(5)),
        };

        static void Ch1(System.Action setUp)
        {
            StoryIntro.NewRun();
            setUp();
            Go("Story_Warehouse");
        }

        static void Hall(int zone)
        {
            StoryIntro.EscapeSeen = true;
            StoryIntro.HallCheckpoint = zone;
        }

        static void Ch2(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.BankFrom = at;
            Go("Story_Bank");
        }

        static void Fortress(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.FortressFrom = at;
            Go("Story_Fortress");
        }

        static void Road(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.RoadFrom = at;
            Go("Story_Road");
        }

        static void Epilogue(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.EpilogueFrom = at;
            StoryIntro.RecapOnly = at >= 5;
            Go("Story_Epilogue");
        }

        static void Backyard(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.BackyardFrom = at;
            Go("Story_Backyard");
        }

        static void Home(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.HomeFrom = at;
            Go("Story_Home");
        }

        static void Street(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.StreetFrom = at;
            Go("Story_Street");
        }

        int testChapter;

        /// <summary>The checkpoints as tabs: the chapters down the left, the selected chapter's
        /// checkpoints on the right (never more than a dozen rows, so nothing overlaps on a phone).</summary>
        void TestPanel(float w, float h)
        {
            var chapters = new System.Collections.Generic.List<string>();
            foreach (var c in Checkpoints) if (!chapters.Contains(c.chapter)) chapters.Add(c.chapter);
            testChapter = Mathf.Clamp(testChapter, 0, chapters.Count - 1);
            int most = 0;
            foreach (var ch in chapters) { int n = 0; foreach (var c in Checkpoints) if (c.chapter == ch) n++; most = Mathf.Max(most, n); }
            float pw = Mathf.Min(1100f, w - 30f), ph = Mathf.Min(h - 30f, 70f + Mathf.Max(chapters.Count, most) * 46f);
            var r = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.8f));
            Fill(r, new Color(0.08f, 0.08f, 0.09f, 0.98f));
            Fill(new Rect(r.x, r.y, r.width, 3), new Color(1f, 0.75f, 0.1f, 0.9f));
            btnSub.normal.textColor = new Color(1f, 0.8f, 0.3f, 0.95f);
            GUI.Label(new Rect(r.x, r.y + 8, pw, 30), "TEST  ·  CHECKPOINTS   (testing only)", btnSub);
            float top = r.y + 50f, avail = r.yMax - top - 10f;
            float leftW = Mathf.Min(340f, pw * 0.36f);
            float tabH = Mathf.Min(44f, avail / chapters.Count);
            for (int i = 0; i < chapters.Count; i++)
            {
                var tr = new Rect(r.x + 12, top + i * tabH, leftW - 12, tabH - 4);
                bool sel = i == testChapter;
                Fill(tr, new Color(1f, 1f, 1f, sel ? 0.18f : tr.Contains(Event.current.mousePosition) ? 0.1f : 0.04f));
                if (sel) Fill(new Rect(tr.x, tr.y, 4, tr.height), new Color(0.8f, 0.06f, 0.07f));
                btnSub.normal.textColor = new Color(1f, 1f, 1f, sel ? 1f : 0.65f);
                GUI.Label(tr, chapters[i], btnSub);
                if (Clicked(tr, true)) testChapter = i;
            }
            var rows = new System.Collections.Generic.List<(string chapter, string label, System.Action go)>();
            foreach (var c in Checkpoints) if (c.chapter == chapters[testChapter]) rows.Add(c);
            float rowH = Mathf.Min(44f, avail / Mathf.Max(1, rows.Count));
            float rx = r.x + leftW + 12f, rw = r.xMax - rx - 12f;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new Rect(rx, top + i * rowH, rw, rowH - 4);
                Fill(row, new Color(1f, 1f, 1f, row.Contains(Event.current.mousePosition) ? 0.14f : 0.05f));
                btnSub.normal.textColor = Color.white;
                GUI.Label(row, rows[i].label, btnSub);
                if (Clicked(row, true)) { testOpen = false; rows[i].go(); }
            }
            var close = new Rect(r.xMax - 50, r.y + 6, 44, 34);
            Fill(close, new Color(1f, 1f, 1f, 0.08f));
            btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(close, "X", btnSub);
            if (Clicked(close, true)) testOpen = false;
        }

        /// <summary>Testing shortcuts on the menu (TEST · CHECKPOINTS). Off for release.</summary>
        public const bool DevShortcuts = true;

        bool Button(Rect r, string label, string sub, bool accent, bool live)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, new Color(1f, 1f, 1f, hover ? 0.16f : 0.08f));
            Fill(new Rect(r.x, r.yMax - 3, r.width, 3), accent ? new Color(0.8f, 0.06f, 0.07f) : new Color(1f, 1f, 1f, 0.6f));
            btn.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x, r.y + 10, r.width, 50), Spaced(label), btn);
            btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(r.x, r.y + 58, r.width, 26), sub, btnSub);
            return Clicked(r, live);
        }
    }
}
