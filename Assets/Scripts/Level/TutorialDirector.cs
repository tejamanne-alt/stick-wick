using UnityEngine;

namespace ProjectInk
{
    public enum Gesture { None, TapHigh, TapLow, TapAgain, Hold, SwipeUp, SwipeDown, SwipeSide, TwoFingers, TapRampage }

    /// <summary>THE TRAINING REEL. On the first launch, after the opening, the hero learns by
    /// doing, against grey stunt doubles who never hit back. Each lesson shows a ghost finger
    /// performing the gesture in the right part of the screen; it completes when the player does
    /// it for real ("NICE."), and the next begins. Skippable; replayable from the help card.
    ///   1 TAP HIGH punch · 2 TAP LOW kick · 3 KEEP TAPPING combo · 4 HOLD run to the ledge ·
    ///   5 SWIPE UP climb · 6 SWIPE DOWN drop · 7 SWIPE SIDEWAYS dodge · 8 TWO FINGERS rampage
    /// then "THE BUILDING IS YOUR WEAPON", and the fight starts.</summary>
    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }
        const string DoneKey = "tutorial_done";

        public struct Lesson
        {
            public string title, line;
            public Gesture gesture;
            public int goal;
            public string evt;       // the hero event that counts (null: reach the marker)
            public bool weapon;      // drop a sword in front of him for this lesson
        }

        static readonly Lesson[] Lessons =
        {
            new Lesson { title = "TAP HIGH", line = "Punch. Tap the upper half, on his side of you.", gesture = Gesture.TapHigh, goal = 3, evt = "punch" },
            new Lesson { title = "TAP LOW", line = "Kick. Tap the lower half.", gesture = Gesture.TapLow, goal = 2, evt = "kick" },
            new Lesson { title = "KEEP TAPPING", line = "Chain them: a full punch or kick combo.", gesture = Gesture.TapAgain, goal = 1, evt = "combo" },
            new Lesson { title = "TAKE HIS WEAPON", line = "Enemies drop their weapons. Walk over one to pick it up.", gesture = Gesture.Hold, goal = 1, evt = "pickup", weapon = true },
            new Lesson { title = "TAP TO SWING", line = "A weapon changes your moves. Two swings.", gesture = Gesture.TapHigh, goal = 2, evt = "punch" },
            new Lesson { title = "SWIPE DOWN", line = "Throw it! It spins across the room and takes him down.", gesture = Gesture.SwipeDown, goal = 1, evt = "throw" },
            new Lesson { title = "HOLD", line = "Run. Press and hold a side. Go to the striped ledge.", gesture = Gesture.Hold, goal = 1, evt = null },
            new Lesson { title = "SWIPE UP", line = "Jump. At a striped ledge: climb.", gesture = Gesture.SwipeUp, goal = 1, evt = "climb" },
            new Lesson { title = "SWIPE DOWN", line = "Drop through the floor.", gesture = Gesture.SwipeDown, goal = 1, evt = "drop" },
            new Lesson { title = "SWIPE SIDEWAYS", line = "Dodge. When anyone flashes red, get out of the way.", gesture = Gesture.SwipeSide, goal = 1, evt = "dodge" },
            new Lesson { title = "RAMPAGE", line = "Your rare power. When you're in trouble and it's charged, this button appears. Tap it.", gesture = Gesture.TapRampage, goal = 1, evt = "rampage" },
        };

        public bool Active { get; private set; }
        /// <summary>Holding the first wave back (training running, or waiting to start).</summary>
        public bool Holding => Active || pendingStart;
        public int Index { get; private set; }
        public int Count => Lessons.Length;
        public Lesson Current => Lessons[Mathf.Clamp(Index, 0, Lessons.Length - 1)];
        public int Progress { get; private set; }
        public float NiceUntil { get; private set; }
        public bool Finale { get; private set; }
        public float StartedAt { get; private set; }
        /// <summary>Where the run lesson wants the hero to go (world), if anywhere.</summary>
        public Vector3? Marker { get; private set; }

        HeroController hero;
        bool pendingStart;
        float nextAt = -1f, finaleUntil;
        readonly System.Collections.Generic.List<Enemy> doubles = new System.Collections.Generic.List<Enemy>();

        public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        /// <summary>Forget it was done (the help card's REPLAY TRAINING).</summary>
        public static void ResetDone() { PlayerPrefs.SetInt(DoneKey, 0); PlayerPrefs.Save(); }

        void Awake()
        {
            Instance = this;
            pendingStart = !Done;
        }

        void OnEnable() { HeroController.Did += OnDid; }
        void OnDisable() { HeroController.Did -= OnDid; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            var wd = GetComponent<WaveDirector>();
            hero = wd != null ? wd.hero : FindAnyObjectByType<HeroController>();
        }

        void Update()
        {
            if (pendingStart)
            {
                // after the opening has handed control over
                var oc = OpeningCinematic.Instance;
                if (oc == null || oc.ControlReleased) { pendingStart = false; Begin(); }
                return;
            }
            if (!Active) return;
            if (Finale)
            {
                if (Time.unscaledTime > finaleUntil) End(true);
                return;
            }
            if (nextAt > 0f)
            {
                if (Time.unscaledTime >= nextAt) { nextAt = -1f; Index++; StartLesson(); }
                return;
            }
            // lessons checked by position
            if (Current.evt == null && Marker.HasValue && hero != null &&
                Mathf.Abs(hero.transform.position.x - Marker.Value.x) < 0.7f && hero.Grounded)
                Complete();
            // keep the stunt doubles in front of him
            for (int i = doubles.Count - 1; i >= 0; i--) if (doubles[i] == null) doubles.RemoveAt(i);
        }

        void Begin()
        {
            if (hero == null) return;
            Active = true;
            Index = 0;
            StartedAt = Time.unscaledTime;
            StartLesson();
        }

        void StartLesson()
        {
            if (Index >= Lessons.Length) { BeginFinale(); return; }
            Progress = 0;
            Marker = null;
            var L = Lessons[Index];
            // stunt doubles for the fighting lessons
            if (L.evt == "punch" || L.evt == "kick" || L.evt == "combo" || L.evt == "dodge" || L.evt == "throw") EnsureDoubles(1);
            if (L.evt == "rampage") { EnsureDoubles(3); var wd = WaveDirector.Instance; if (wd != null) wd.FillFlow(); }
            if (L.evt == null && LevelNav.Active && LevelNav.Ledges.Count > 0)
            {
                var ledge = LevelNav.Ledges[0];
                Marker = new Vector3(ledge.FootX, 0f, 0f);
            }
            if (L.weapon && hero != null)
            {
                // a sword on the floor a few steps away (away from the stunt double), marked HERE
                var wd = WaveDirector.Instance;
                var p = hero.transform.position;
                float x = Mathf.Clamp(p.x - 2.4f, -11f, 11f);
                var at = new Vector3(x, Fighter.GroundY(new Vector3(x, p.y + 0.5f, 0f), 0.6f), 0f);
                if (wd != null && hero.Style == FightStyle.Fists) WeaponPickup.Spawn(FightStyle.Sword, 10, at, wd.weaponMaterial);
                Marker = at;
            }
            Sound.Play(Sfx.CineHit, 0.35f);
        }

        void EnsureDoubles(int n)
        {
            var wd = WaveDirector.Instance;
            if (wd == null || hero == null) return;
            for (int i = doubles.Count - 1; i >= 0; i--) if (doubles[i] == null || !doubles[i].Alive) doubles.RemoveAt(i);
            while (doubles.Count < n)
            {
                float side = doubles.Count % 2 == 0 ? 1f : -1f;
                var p = hero.transform.position;
                float x = Mathf.Clamp(p.x + side * (2.6f + doubles.Count * 0.6f), -11f, 11f);
                var e = wd.SpawnDummy(new Vector3(x, Fighter.GroundY(new Vector3(x, p.y + 0.5f, 0f), 0.6f), 0f));
                if (e == null) break;
                doubles.Add(e);
            }
        }

        void OnDid(string what)
        {
            if (!Active || Finale || nextAt > 0f) return;
            var L = Lessons[Index];
            if (L.evt == null || L.evt != what) return;
            Progress++;
            if (Progress >= L.goal) Complete();
        }

        void Complete()
        {
            Progress = Lessons[Index].goal;
            NiceUntil = Time.unscaledTime + 0.9f;
            nextAt = Time.unscaledTime + 1.0f;
            Sound.Play(Sfx.Pickup, 0.8f, null, 1.2f);
        }

        void BeginFinale()
        {
            Finale = true;
            finaleUntil = Time.unscaledTime + 2.6f;
            Sound.Play(Sfx.CineMassive, 0.8f);
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.04f);
        }

        /// <summary>Finish (or skip): clear the stunt doubles and let the fight begin.</summary>
        public void End(bool completed)
        {
            foreach (var d in doubles) if (d != null) Destroy(d.gameObject);
            doubles.Clear();
            Active = false;
            Finale = false;
            pendingStart = false;
            Marker = null;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            var wd = WaveDirector.Instance;
            if (wd != null) wd.SpendFlow();
        }
    }
}
