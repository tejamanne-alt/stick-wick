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
            if (Button(story, "STORY MODE", "CHAPTER 1  ·  THE WAREHOUSE", true)) { StoryIntro.NewRun(); SceneManager.LoadScene("Story_Warehouse"); }
            if (Button(fight, "FIGHT MODE", "THE TOWER  ·  ENDLESS FLOORS", false)) SceneManager.LoadScene("Prototype");
            // TEMPORARY, for testing: straight into chapter 1's boss fight (set DevShortcuts false to remove)
            if (DevShortcuts)
            {
                var dev = new Rect(w * 0.5f - 130, y + bh + 22, 260, 40);
                Fill(dev, new Color(1f, 0.75f, 0.1f, dev.Contains(Event.current.mousePosition) ? 0.22f : 0.12f));
                btnSub.normal.textColor = new Color(1f, 0.8f, 0.3f, 0.9f);
                GUI.Label(dev, "TEST  ·  SKIP TO THE BOSS", btnSub);
                if (GUI.Button(dev, GUIContent.none, GUIStyle.none))
                {
                    StoryIntro.NewRun();
                    StoryIntro.EscapeSeen = true;                 // as if he had come all the way: the knife found
                    StoryIntro.HallCheckpoint = 3;                // the crane bay ...
                    StoryIntro.BossReached = true;                // ... with the Foreman already on his way down
                    SceneManager.LoadScene("Story_Warehouse");
                }
            }
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>Testing shortcuts on the menu (SKIP TO THE BOSS). Off for release.</summary>
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
