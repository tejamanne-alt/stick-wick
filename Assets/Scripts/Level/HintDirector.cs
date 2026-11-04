using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>CHALK MARKS: just-in-time hints drawn in the world, next to the thing they are
    /// about, at the moment it is useful, in a hand-chalked style:
    ///   by a striped ledge you can reach: "CLIMB" (swipe up)
    ///   under a rope, trapeze or bar: "GRAB" (swipe up)
    ///   a crate with an enemy beyond it: "KICK IT" (tap)
    ///   standing above an enemy: "DIVE ON HIM" (swipe down)
    ///   an enemy flashing red beside you: "DODGE" (swipe sideways)
    ///   a weapon on the floor: "TAKE IT" (walk over it)
    /// Each mark retires for good once the move has been done three times (saved).</summary>
    public class HintDirector : MonoBehaviour
    {
        public static HintDirector Instance { get; private set; }

        public struct Hint { public string id, text; public Gesture gesture; public Vector3 world; }
        public readonly List<Hint> Shown = new List<Hint>();

        const int RetireAfter = 3;
        HeroController hero;
        readonly List<Hint> all = new List<Hint>();

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void OnEnable() { HeroController.Did += OnDid; }
        void OnDisable() { HeroController.Did -= OnDid; }

        void Start()
        {
            var wd = GetComponent<WaveDirector>();
            hero = wd != null ? wd.hero : FindAnyObjectByType<HeroController>();
        }

        static string Key(string id) => "hint_" + id;
        static bool Retired(string id) => PlayerPrefs.GetInt(Key(id), 0) >= RetireAfter;

        void OnDid(string what)
        {
            string id = what switch
            {
                "climb" => "ledge", "grab" => "grab", "crate" => "crate", "dive" => "dive",
                "dodge" => "dodge", "pickup" => "pickup", _ => null,
            };
            if (id == null) return;
            PlayerPrefs.SetInt(Key(id), PlayerPrefs.GetInt(Key(id), 0) + 1);
            PlayerPrefs.Save();
        }

        /// <summary>Bring every chalk mark back (the help card's REPLAY TRAINING).</summary>
        public static void ResetAll()
        {
            foreach (var id in new[] { "ledge", "grab", "crate", "dive", "dodge", "pickup" }) PlayerPrefs.DeleteKey(Key(id));
            PlayerPrefs.Save();
        }

        void Update()
        {
            Shown.Clear();
            if (hero == null || hero.IsDead || hero.CinematicLock) return;
            var oc = OpeningCinematic.Instance;
            if (oc != null && oc.Active) return;
            var vc = VictoryCinematic.Instance;
            if (vc != null && vc.Active) return;
            var tut = TutorialDirector.Instance;
            bool training = tut != null && tut.Active;

            all.Clear();
            var p = hero.transform.position;
            int myPlat = LevelNav.Active ? LevelNav.PlatformAt(p) : -1;

            // a ledge to climb
            if (LevelNav.Active && hero.Grounded && !Retired("ledge"))
            {
                int k = LevelNav.LedgeFor(p, myPlat, 3.0f);
                if (k >= 0)
                {
                    var L = LevelNav.Ledges[k];
                    all.Add(new Hint { id = "ledge", text = "CLIMB", gesture = Gesture.SwipeUp, world = new Vector3(L.x - L.dir * 0.3f, L.y + 0.2f, 0f) });
                }
            }
            // something to swing on
            if (hero.Grounded && !Retired("grab"))
            {
                var sw = Swingable.Reachable(p, 2.95f, 2.4f);
                if (sw != null) all.Add(new Hint { id = "grab", text = "GRAB", gesture = Gesture.SwipeUp, world = sw.Grip + Vector3.up * 0.25f });
            }
            // a crate with someone beyond it
            if (!training && hero.Grounded && !Retired("crate"))
                foreach (var c in Crate.All)
                {
                    if (!c.Available) continue;
                    var cp = c.transform.position;
                    float dx = cp.x - p.x;
                    if (Mathf.Abs(dx) > 2.2f || Mathf.Abs(cp.y - 0.35f - p.y) > 0.5f) continue;
                    int side = dx >= 0f ? 1 : -1;
                    bool someone = false;
                    foreach (var e in Enemy.All)
                        if (e.Targetable && (e.transform.position.x - cp.x) * side > 0.4f && (e.transform.position.x - cp.x) * side < 6f &&
                            Mathf.Abs(e.transform.position.y - p.y) < 0.6f) someone = true;
                    if (someone) { all.Add(new Hint { id = "crate", text = "KICK IT", gesture = Gesture.TapAgain, world = cp + Vector3.up * 0.7f }); break; }
                }
            // an enemy below a floor we can drop through
            if (LevelNav.Active && hero.Grounded && myPlat >= 0 && LevelNav.Platforms[myPlat].oneWay && !Retired("dive"))
                foreach (var e in Enemy.All)
                    if (e.Targetable && e.transform.position.y < p.y - 1.8f && Mathf.Abs(e.transform.position.x - p.x) < 4.5f)
                    { all.Add(new Hint { id = "dive", text = "DIVE ON HIM", gesture = Gesture.SwipeDown, world = p + Vector3.up * 1.9f }); break; }
            // someone about to hit us
            if (!Retired("dodge"))
                foreach (var e in Enemy.All)
                    if (e.Telegraphing && Mathf.Abs(e.transform.position.x - p.x) < 2.6f && Mathf.Abs(e.transform.position.y - p.y) < 1f)
                    { all.Add(new Hint { id = "dodge", text = "DODGE", gesture = Gesture.SwipeSide, world = e.Chest + Vector3.up * 1.0f }); break; }
            // a weapon on the floor
            if (!training && hero.Style == FightStyle.Fists && !Retired("pickup"))
                foreach (var w in WeaponPickup.All)
                    if (Mathf.Abs(w.transform.position.x - p.x) < 5f && Mathf.Abs(w.transform.position.y - p.y) < 1f)
                    { all.Add(new Hint { id = "pickup", text = "TAKE IT", gesture = Gesture.None, world = w.transform.position + Vector3.up * 0.8f }); break; }

            // at most two at a time, the most urgent first
            all.Sort((a, b) => Priority(a.id).CompareTo(Priority(b.id)));
            for (int i = 0; i < all.Count && i < 2; i++) Shown.Add(all[i]);
        }

        static int Priority(string id) => id switch { "dodge" => 0, "dive" => 1, "crate" => 2, "ledge" => 3, "grab" => 4, _ => 5 };
    }
}
