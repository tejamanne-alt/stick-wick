using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (Button(story, "STORY MODE", "CHAPTER 1  ·  THE WAREHOUSE", true) && !open) { StoryIntro.NewRun(); SceneManager.LoadScene("Story_Warehouse"); }
            if (Button(fight, "FIGHT MODE", "THE TOWER  ·  ENDLESS FLOORS", false) && !open) SceneManager.LoadScene("Prototype");
            // TEMPORARY, for testing: jump straight to any checkpoint (set DevShortcuts false to remove)
            if (DevShortcuts)
            {
                var dev = new Rect(w * 0.5f - 150, y + bh + 22, 300, 40);
                Fill(dev, new Color(1f, 0.75f, 0.1f, dev.Contains(Event.current.mousePosition) || testOpen ? 0.24f : 0.12f));
                btnSub.normal.textColor = new Color(1f, 0.8f, 0.3f, 0.9f);
                GUI.Label(dev, "TEST  ·  CHECKPOINTS", btnSub);
                if (GUI.Button(dev, GUIContent.none, GUIStyle.none) && !open) testOpen = true;
                GUI.enabled = true;
                if (testOpen) TestPanel(w, h);
            }
            GUI.enabled = true;
            GUI.matrix = Matrix4x4.identity;
        }

        bool testOpen;

        /// <summary>Every place in the story you can jump to, to test it without playing up to it.</summary>
        static readonly (string chapter, string label, System.Action go)[] Checkpoints =
        {
            ("CHAPTER 1  ·  THE WAREHOUSE", "Opening", () => Ch1(() => { })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "At the shutter (the walk)", () => Ch1(() => { StoryIntro.Retry = true; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 1  ·  Loading bay", () => Ch1(() => Hall(0))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 2  ·  Racking aisle", () => Ch1(() => Hall(1))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 3  ·  Mezzanine", () => Ch1(() => Hall(2))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Hall 4  ·  Crane bay", () => Ch1(() => Hall(3))),
            ("CHAPTER 1  ·  THE WAREHOUSE", "The Foreman (the fight)", () => Ch1(() => { Hall(3); StoryIntro.BossReached = true; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Ending  ·  boss beaten", () => Ch1(() => { StoryIntro.EscapeSeen = true; StoryIntro.TestEnding = 1; })),
            ("CHAPTER 1  ·  THE WAREHOUSE", "Ending  ·  the key on the body", () => Ch1(() => { StoryIntro.EscapeSeen = true; StoryIntro.TestEnding = 2; })),
            ("CHAPTER 2  ·  THE BANK", "Start  ·  the street", () => Ch2(0)),
            ("CHAPTER 2  ·  THE BANK", "At the counter", () => Ch2(1)),
            ("CHAPTER 2  ·  THE BANK", "The vault  ·  two keys", () => Ch2(2)),
            ("CHAPTER 2  ·  THE BANK", "The box  ·  passports, ring, memory", () => Ch2(3)),
            ("CHAPTER 2  ·  THE BANK", "Walk out", () => Ch2(4)),
            ("CHAPTER 2  ·  THE STREET", "The box  ·  the men from the car", () => Street(0)),
            ("CHAPTER 2  ·  THE STREET", "The market street", () => Street(1)),
            ("CHAPTER 2  ·  THE STREET", "Under the railway  ·  the train", () => Street(2)),
            ("CHAPTER 2  ·  THE STREET", "The crossroads", () => Street(3)),
            ("CHAPTER 2  ·  THE STREET", "The Driver (the fight)", () => Street(4)),
            ("CHAPTER 2  ·  THE STREET", "After the Driver  ·  \"We got there first\"", () => Street(5)),
            ("CHAPTER 3  ·  HOME", "Alder Lane  ·  the arrival", () => Home(0)),
            ("CHAPTER 3  ·  HOME", "The study  ·  the photo, Maya", () => Home(1)),
            ("CHAPTER 3  ·  HOME", "The kitchen  ·  Elena", () => Home(2)),
            ("CHAPTER 3  ·  HOME", "The house fight (rage)", () => Home(3)),
            ("CHAPTER 3  ·  HOME", "The Cleaner (the fight)", () => Home(4)),
            ("CHAPTER 3  ·  HOME", "The kitchen chair", () => Home(5)),
            ("CHAPTER 3  ·  HOME", "The footprints  ·  the garden", () => Home(6)),
            ("CHAPTER 4  ·  THE BACKYARD", "The garden  ·  her footprints", () => Backyard(0)),
            ("CHAPTER 4  ·  THE BACKYARD", "The ribbon  ·  how she was taken", () => Backyard(1)),
            ("CHAPTER 4  ·  THE BACKYARD", "The promise", () => Backyard(2)),
            ("CHAPTER 4  ·  THE BACKYARD", "The fight (garden to car park)", () => Backyard(3)),
            ("CHAPTER 4  ·  THE BACKYARD", "The Handler (the fight)", () => Backyard(4)),
            ("CHAPTER 4  ·  THE BACKYARD", "The address  ·  the hill", () => Backyard(5)),
            ("CHAPTER 5  ·  THE FORTRESS", "The storm  ·  the arrival", () => Fortress(0)),
            ("CHAPTER 5  ·  THE FORTRESS", "The rock stair", () => Fortress(1)),
            ("CHAPTER 5  ·  THE FORTRESS", "The gate  ·  the trap", () => Fortress(2)),
            ("CHAPTER 5  ·  THE FORTRESS", "The courtyard", () => Fortress(3)),
            ("CHAPTER 5  ·  THE FORTRESS", "The blackout", () => Fortress(4)),
            ("CHAPTER 5  ·  THE FORTRESS", "The Warden (the fight)", () => Fortress(5)),
            ("CHAPTER 5  ·  THE FORTRESS", "The tower", () => Fortress(6)),
        };

        static void Ch1(System.Action setUp)
        {
            StoryIntro.NewRun();
            setUp();
            SceneManager.LoadScene("Story_Warehouse");
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
            SceneManager.LoadScene("Story_Bank");
        }

        static void Fortress(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.FortressFrom = at;
            SceneManager.LoadScene("Story_Fortress");
        }

        static void Backyard(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.BackyardFrom = at;
            SceneManager.LoadScene("Story_Backyard");
        }

        static void Home(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.HomeFrom = at;
            SceneManager.LoadScene("Story_Home");
        }

        static void Street(int at)
        {
            StoryIntro.NewRun();
            StoryIntro.StreetFrom = at;
            SceneManager.LoadScene("Story_Street");
        }

        void TestPanel(float w, float h)
        {
            int heads = 0;
            string seen = null;
            foreach (var c in Checkpoints) if (c.chapter != seen) { seen = c.chapter; heads++; }
            int rows = Checkpoints.Length + heads;
            float pw = Mathf.Min(760f, w - 40f), rowH = Mathf.Min(34f, (h - 90f) / rows);
            float ph = rows * rowH + 60f;
            var r = new Rect((w - pw) * 0.5f, Mathf.Max(10f, (h - ph) * 0.5f), pw, ph);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.75f));
            Fill(r, new Color(0.08f, 0.08f, 0.09f, 0.98f));
            Fill(new Rect(r.x, r.y, r.width, 3), new Color(1f, 0.75f, 0.1f, 0.9f));
            btnSub.normal.textColor = new Color(1f, 0.8f, 0.3f, 0.95f);
            GUI.Label(new Rect(r.x, r.y + 8, pw, 30), "TEST  ·  CHECKPOINTS   (testing only)", btnSub);
            float yy = r.y + 46f;
            string last = null;
            foreach (var c in Checkpoints)
            {
                if (c.chapter != last)
                {
                    last = c.chapter;
                    btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
                    GUI.Label(new Rect(r.x + 20, yy, pw - 40, rowH), Spaced(c.chapter), btnSub);
                    yy += rowH;
                }
                var row = new Rect(r.x + 40, yy + 2, pw - 80, rowH - 4);
                Fill(row, new Color(1f, 1f, 1f, row.Contains(Event.current.mousePosition) ? 0.14f : 0.05f));
                btnSub.normal.textColor = Color.white;
                GUI.Label(row, c.label, btnSub);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { testOpen = false; c.go(); }
                yy += rowH;
            }
            var close = new Rect(r.xMax - 44, r.y + 6, 36, 30);
            btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(close, "X", btnSub);
            if (GUI.Button(close, GUIContent.none, GUIStyle.none)) testOpen = false;
        }

        /// <summary>Testing shortcuts on the menu (TEST · CHECKPOINTS). Off for release.</summary>
        public const bool DevShortcuts = true;

        bool Button(Rect r, string label, string sub, bool accent)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, new Color(1f, 1f, 1f, hover ? 0.16f : 0.08f));
            Fill(new Rect(r.x, r.yMax - 3, r.width, 3), accent ? new Color(0.8f, 0.06f, 0.07f) : new Color(1f, 1f, 1f, 0.6f));
            btn.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x, r.y + 10, r.width, 50), Spaced(label), btn);
            btnSub.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(r.x, r.y + 58, r.width, 26), sub, btnSub);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
    }
}
