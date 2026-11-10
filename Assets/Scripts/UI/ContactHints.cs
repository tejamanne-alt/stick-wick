using UnityEngine;

namespace ProjectInk
{
    /// <summary>Story mode: teaches the CONTACT moves in the fight itself, at the moment each one is
    /// possible, until the player has used it a couple of times (remembered between runs).
    ///   PARRY    a bare-handed man winding up close: "TAP HIM NOW"
    ///   GRAB     a man in arm's reach: "SWIPE AT HIM"
    ///   CLINCH   holding a man: the list of what each gesture does
    ///   STRINGS  once, when the first fight starts: the named combos
    /// Same look as the subtitles: a dark panel, white words, the game's red bar.</summary>
    public class ContactHints : MonoBehaviour
    {
        HeroController hero;
        GUIStyle head, line, keys;
        int parries, grabs, throws;
        float stringsUntil = -1f, firstSeen = -1f;
        bool stringsShown;

        static readonly Color Panel = new Color(0f, 0f, 0f, 0.72f);
        static readonly Color Red = new Color(0.8f, 0.06f, 0.07f);
        static Texture2D white;

        const string KeyP = "contact_parries", KeyG = "contact_grabs", KeyT = "contact_throws", KeyS = "contact_strings";

        void Awake()
        {
            hero = GetComponent<HeroController>();
            parries = PlayerPrefs.GetInt(KeyP, 0);
            grabs = PlayerPrefs.GetInt(KeyG, 0);
            throws = PlayerPrefs.GetInt(KeyT, 0);
            stringsShown = PlayerPrefs.GetInt(KeyS, 0) > 0;
        }

        void OnEnable() { HeroController.Did += OnDid; }
        void OnDisable() { HeroController.Did -= OnDid; }

        void OnDid(string what)
        {
            if (what == "parry") { parries++; PlayerPrefs.SetInt(KeyP, parries); }
            else if (what == "clinch") { grabs++; PlayerPrefs.SetInt(KeyG, grabs); }
            else if (what == "throw") { throws++; PlayerPrefs.SetInt(KeyT, throws); }
            OnDidV3(what);
        }

        bool Showing
        {
            get
            {
                var s = StoryIntro.Instance;
                return s != null && !s.HideHud && hero != null && hero.Contact && hero.Alive && !hero.CinematicLock && !TimeController.Paused;
            }
        }

        void OnGUI()
        {
            if (!Showing) return;
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            float k = Screen.height / 720f;
            if (head == null)
            {
                var display = HudGUI.Display != null ? HudGUI.Display : Resources.Load<Font>("Fonts/BebasNeue-Regular");
                var body = HudGUI.Body != null ? HudGUI.Body : Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
                head = new GUIStyle { font = display, alignment = TextAnchor.MiddleCenter };
                line = new GUIStyle { font = body, alignment = TextAnchor.MiddleLeft };
                keys = new GUIStyle { font = body, alignment = TextAnchor.MiddleCenter };
                head.normal.textColor = Color.white;
                line.normal.textColor = Color.white;
                keys.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            }
            head.fontSize = Mathf.RoundToInt(30 * k);
            line.fontSize = Mathf.RoundToInt(19 * k);
            keys.fontSize = Mathf.RoundToInt(14 * k);

            // the clinch: what each gesture does
            if (hero.InClinch && throws < 3 && CombatV3.On)
            {
                string[] rows =
                {
                    "TAP HIM  ·  knee  (third: headbutt)",
                    "TAP HIS LEGS  ·  leg sweep",
                    "SWIPE TOWARDS HIM  ·  hip throw",
                    "SWIPE AWAY  ·  shoulder throw, over you",
                    "SWIPE UP  ·  suplex   (worn down: body slam)",
                    "SWIPE DOWN  ·  DDT",
                    "SWIPE AWAY FROM YOURSELF  ·  shove him off",
                };
                Card("THE CLINCH", rows, null, k);
                return;
            }
            if (hero.InClinch && throws < 3)
            {
                string[] rows =
                {
                    "TAP HIGH  ·  knee  (third: headbutt)",
                    "TAP LOW  ·  leg sweep",
                    "SWIPE AT HIM  ·  hip throw",
                    "SWIPE AWAY  ·  shoulder throw, over you",
                    "SWIPE UP  ·  suplex   (worn down: body slam)",
                    "SWIPE DOWN  ·  DDT",
                    "TAP BEHIND YOU  ·  shove him off",
                };
                Card("THE CLINCH", rows, "keys:  A / D punch   Z / X kick   Q / E swipe   W up   S down", k);
                return;
            }

            if (firstSeen < 0f && AnyEnemyNear(6f)) firstSeen = Time.unscaledTime;
            if (CombatV3.On) { V3Cards(k); return; }
            if (!stringsShown && firstSeen > 0f && Time.unscaledTime - firstSeen > 1.2f)
            {
                stringsShown = true;
                stringsUntil = Time.unscaledTime + 6.5f;
                PlayerPrefs.SetInt(KeyS, 1);
            }
            if (Time.unscaledTime < stringsUntil)
            {
                string[] rows =
                {
                    "P P P P  ·  jab, cross, hook, ELBOW",
                    "K K K  ·  low kick, roundhouse, SIDE KICK",
                    "P P K  ·  one-two, HIGH KICK",
                    "K P P  ·  low kick, lunge punch, REVERSE PUNCH",
                    "P K P  ·  jab, low kick, PALM HEEL",
                };
                Card("COMBOS   ·   P = TAP HIGH   K = TAP LOW", rows, null, k, true);
                return;
            }

            var e = Nearest(1.9f, out float gap);
            if (e == null) return;
            if (parries < 2 && e.Parryable && gap < 1.8f) { Banner("TAP HIM NOW", "PARRY", k); return; }
            if (grabs < 2 && hero.Free && e.CanBeHeld && !e.Telegraphing && gap < 1.4f) { Banner("SWIPE AT HIM", "GRAB", k); return; }
        }

        // ---------------------------------------------------------------- Combat 3.0
        const string KeyV3 = "v3_intro", KeyV3Guard = "v3_guards", KeyV3Evade = "v3_evades";
        float v3Until = -1f;
        int v3Guards = -1, v3Evades = -1;

        /// <summary>The new controls: one card at the first fight, then a call to action at each kind of
        /// telegraph (white: guard it; red: evade it) until it has been done a few times.</summary>
        void V3Cards(float k)
        {
            if (v3Guards < 0) { v3Guards = PlayerPrefs.GetInt(KeyV3Guard, 0); v3Evades = PlayerPrefs.GetInt(KeyV3Evade, 0); }
            if (v3Until < 0f && firstSeen > 0f && Time.unscaledTime - firstSeen > 1.0f && PlayerPrefs.GetInt(KeyV3, 0) < 2)
            {
                v3Until = Time.unscaledTime + 8f;
                PlayerPrefs.SetInt(KeyV3, PlayerPrefs.GetInt(KeyV3, 0) + 1);
            }
            if (Time.unscaledTime < v3Until)
            {
                stringsUntil = v3Until;
                string[] rows =
                {
                    "TAP HIM  ·  strike where you touch: head, body, legs",
                    "SWIPE THROUGH HIM  ·  hook, roundhouse, uppercut",
                    "DRAG BACK FROM HIM, LET GO  ·  the power right",
                    "HOLD ON YOURSELF  ·  guard  (WHITE flash)",
                    "SWIPE FROM YOURSELF  ·  duck, hop, step out  (RED)",
                    "HOLD THE GROUND  ·  walk there",
                };
                Card("NEW CONTROLS   ·   WHERE YOU TOUCH", rows, "pause → COMBAT  NEW / CLASSIC", k, true);
                return;
            }
            var e = Nearest(2.4f, out float gap);
            if (e == null || !e.Telegraphing) return;
            if (e.V3Red && v3Evades < 3) { Banner("SWIPE FROM YOURSELF", "RED · EVADE", k); return; }
            if (!e.V3Red && v3Guards < 3) Banner("HOLD ON YOURSELF", "GUARD", k);
        }

        void OnDidV3(string what)
        {
            if (v3Guards < 0) return;
            if (what == "guard") { v3Guards++; PlayerPrefs.SetInt(KeyV3Guard, v3Guards); }
            else if (what == "dodge") { v3Evades++; PlayerPrefs.SetInt(KeyV3Evade, v3Evades); }
        }

        bool AnyEnemyNear(float r) => Nearest(r, out _) != null;

        Enemy Nearest(float range, out float gap)
        {
            gap = float.MaxValue;
            Enemy best = null;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float d = Mathf.Abs(e.transform.position.x - hero.transform.position.x);
                if (Mathf.Abs(e.transform.position.y - hero.transform.position.y) > 0.8f || d > range || d >= gap) continue;
                gap = d; best = e;
            }
            return best;
        }

        /// <summary>A short call to action high in the middle: the move's name over the gesture.</summary>
        void Banner(string gesture, string move, float k)
        {
            float w = 340f * k, h = 74f * k;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.16f, w, h);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 9f);
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, 5f * k, r.height), Red);
            head.normal.textColor = new Color(1f, 1f, 1f, pulse);
            GUI.Label(new Rect(r.x, r.y + 4f * k, r.width, 36f * k), move, head);
            head.normal.textColor = Color.white;
            keys.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(r.x, r.y + 40f * k, r.width, 26f * k), gesture, keys);
            keys.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        }

        /// <summary>A list card at the right edge: a title with the red bar, then one row per move.</summary>
        void Card(string title, string[] rows, string footer, float k, bool fade = false)
        {
            float rowH = 26f * k, w = 400f * k;
            float h = 50f * k + rows.Length * rowH + (footer != null ? 26f * k : 8f * k);
            var r = new Rect(Screen.width - w - 24f * k, Screen.height * 0.14f, w, h);
            float a = fade ? Mathf.Clamp01((stringsUntil - Time.unscaledTime) / 0.6f) : 1f;
            Fill(r, new Color(0f, 0f, 0f, Panel.a * a));
            Fill(new Rect(r.x, r.y, 5f * k, r.height), new Color(Red.r, Red.g, Red.b, a));
            head.fontSize = Mathf.RoundToInt(24 * k);
            head.alignment = TextAnchor.MiddleLeft;
            head.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(r.x + 18f * k, r.y + 8f * k, r.width - 24f * k, 32f * k), title, head);
            head.alignment = TextAnchor.MiddleCenter;
            head.normal.textColor = Color.white;
            line.normal.textColor = new Color(1f, 1f, 1f, 0.92f * a);
            for (int i = 0; i < rows.Length; i++)
                GUI.Label(new Rect(r.x + 18f * k, r.y + 44f * k + i * rowH, r.width - 24f * k, rowH), rows[i], line);
            if (footer != null)
            {
                keys.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(r.x + 18f * k, r.y + h - 28f * k, r.width - 24f * k, 22f * k), footer, keys);
                keys.alignment = TextAnchor.MiddleCenter;
            }
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }
    }
}
