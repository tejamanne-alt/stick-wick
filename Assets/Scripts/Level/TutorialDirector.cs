using UnityEngine;

namespace ProjectInk
{
    public enum Gesture { None, TapHigh, TapLow, TapAgain, Hold, SwipeUp, SwipeDown, SwipeSide, TwoFingers, TapRampage, TapEnemy }

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
            new Lesson { title = "TAP HIGH", line = "Punch. Tap high, on the side of the screen he is on.", gesture = Gesture.TapHigh, goal = 3, evt = "punch" },
            new Lesson { title = "TAP LOW", line = "Kick. Tap low, on his side.", gesture = Gesture.TapLow, goal = 2, evt = "kick" },
            new Lesson { title = "KEEP TAPPING", line = "Chain them: a full punch or kick combo.", gesture = Gesture.TapAgain, goal = 1, evt = "combo" },
            new Lesson { title = "TAP HIM", line = "Pick your fight: tap an enemy to target him. Your strikes go to him; the rest wait.", gesture = Gesture.TapEnemy, goal = 1, evt = "lock" },
            new Lesson { title = "TAKE HIS WEAPON", line = "Enemies drop their weapons. Walk over one to pick it up.", gesture = Gesture.Hold, goal = 1, evt = "pickup", weapon = true },
            new Lesson { title = "TAP TO SWING", line = "A weapon changes your moves. Two swings.", gesture = Gesture.TapHigh, goal = 2, evt = "punch" },
            new Lesson { title = "SWIPE DOWN", line = "Throw it! It spins across the room and takes him down.", gesture = Gesture.SwipeDown, goal = 1, evt = "throw" },
            new Lesson { title = "HOLD", line = "Run. Press and hold a side. Go to the striped ledge.", gesture = Gesture.Hold, goal = 1, evt = null },
            new Lesson { title = "SWIPE UP", line = "Jump. At a striped ledge: climb.", gesture = Gesture.SwipeUp, goal = 1, evt = "climb" },
            new Lesson { title = "SWIPE DOWN", line = "Drop through the floor.", gesture = Gesture.SwipeDown, goal = 1, evt = "drop" },
            new Lesson { title = "SWIPE SIDEWAYS", line = "Dodge. When anyone flashes red, get out of the way.", gesture = Gesture.SwipeSide, goal = 1, evt = "dodge" },
            new Lesson { title = "RAMPAGE", line = "Your rare power. When you're in trouble and it's charged, this button appears. Tap it.", gesture = Gesture.TapRampage, goal = 1, evt = "rampage" },
        };

        /// <summary>STORY MODE: the first fight of the prologue teaches the knife against the three
        /// real men (held back, unkillable while he learns), as his instinct coming back. Set before
        /// the component is added, so it does not start the fight-mode reel on its own.</summary>
        public static bool StoryMode;
        const string StoryDoneKey = "story_training_done";
        public static bool StoryDone => PlayerPrefs.GetInt(StoryDoneKey, 0) == 1;

        static readonly Lesson[] StoryLessons =
        {
            new Lesson { title = "TAP HIGH", line = "Stab. Tap high, on the side he is on.", gesture = Gesture.TapHigh, goal = 2, evt = "punch" },
            new Lesson { title = "TAP LOW", line = "Kick. Tap low, on his side.", gesture = Gesture.TapLow, goal = 2, evt = "kick" },
            new Lesson { title = "TAP HIM", line = "Pick your man: tap him. Your strikes go to him; the others wait.", gesture = Gesture.TapEnemy, goal = 1, evt = "lock" },
            new Lesson { title = "SWIPE SIDEWAYS", line = "Dodge. When one of them flashes red, get out of the way.", gesture = Gesture.SwipeSide, goal = 1, evt = "dodge" },
            new Lesson { title = "SWIPE DOWN", line = "Throw the knife. You carry more than one.", gesture = Gesture.SwipeDown, goal = 1, evt = "throw" },
        };

        Lesson[] lessons = Lessons;
        bool story;
        System.Action onFinished;
        /// <summary>The card's heading and the closing title (the story calls it instinct).</summary>
        public string Header => story ? "I N S T I N C T" : "T R A I N I N G";
        public string FinaleTitle => story ? "THEY'RE YOURS." : "THE BUILDING IS YOUR WEAPON";
        public string FinaleLine => story ? "your hands remember: the rest comes back as you fight" : "ledges, ropes, crates, edges, glass... look for the chalk marks";

        public bool Active { get; private set; }
        /// <summary>Holding the first wave back (training running, or waiting to start).</summary>
        public bool Holding => Active || pendingStart;
        public int Index { get; private set; }
        public int Count => lessons.Length;
        public Lesson Current => lessons[Mathf.Clamp(Index, 0, lessons.Length - 1)];
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
            pendingStart = !Done && !StoryMode;
            StoryMode = false;                       // read once: a fight-mode scene after the story gets its own reel
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
            // keep the stunt doubles in front of him (the story's men keep their own distance)
            if (!story) TendDoubles();
        }

        /// <summary>The prologue's first fight: teach the knife on these men, then call `done`.</summary>
        public void BeginStory(Enemy[] men, System.Action done)
        {
            story = true;
            lessons = StoryLessons;
            onFinished = done;
            pendingStart = false;
            if (hero == null && WaveDirector.Instance != null) hero = WaveDirector.Instance.hero;
            doubles.Clear();
            foreach (var m in men) if (m != null) doubles.Add(m);
            Begin();
        }

        void Begin()
        {
            if (hero == null) return;
            Active = true;
            Index = 0;
            StartedAt = Time.unscaledTime;
            StartLesson();
        }

        float lessonStart;
        /// <summary>The lesson has been running a while: the HUD draws attention to SKIP MOVE.</summary>
        public bool Stuck => Active && !Finale && nextAt < 0f && Time.unscaledTime - lessonStart > 20f;

        /// <summary>Climbing and dropping need the tower's ledges and gratings.</summary>
        static bool Applicable(Lesson L) =>
            (L.evt != null && L.evt != "climb" && L.evt != "drop") || (LevelNav.Active && LevelNav.Ledges.Count > 0);

        void StartLesson()
        {
            if (Index >= lessons.Length) { BeginFinale(); return; }
            var L = lessons[Index];
            if (!Applicable(L)) { Index++; StartLesson(); return; }
            Progress = 0;
            Marker = null;
            lessonStart = Time.unscaledTime;
            var wd = WaveDirector.Instance;
            if (story)
            {
                // the men stay where they are; he turns to the nearest, so the first tap goes his way
                if (hero != null)
                {
                    Enemy near = null;
                    foreach (var d in doubles) if (d != null && (near == null || Mathf.Abs(d.transform.position.x - hero.transform.position.x) < Mathf.Abs(near.transform.position.x - hero.transform.position.x))) near = d;
                    if (near != null && hero.Free) hero.FaceTowards(near.transform.position.x);
                }
                if (L.evt == "throw" && hero != null && hero.Style == FightStyle.Fists) hero.GiveWeapon(FightStyle.Knife, 2);
                Sound.Play(Sfx.CineHit, 0.35f);
                return;
            }
            // every lesson starts clean: the stunt doubles back on their marks, on their feet
            SetupDoubles(L);
            if (L.evt == "rampage" && wd != null) wd.FillFlow();
            if ((L.evt == null || L.evt == "climb") && LevelNav.Active && LevelNav.Ledges.Count > 0)
            {
                var ledge = LevelNav.Ledges[0];
                Marker = new Vector3(ledge.FootX, 0f, 0f);
            }
            if (L.weapon && hero != null)
            {
                if (hero.Style != FightStyle.Fists) { Complete(); return; }     // already armed: nothing to pick up
                // a sword on the floor a few steps behind him (away from the stunt double), marked HERE
                var at = Spot(-hero.Facing * 2.4f);
                if (wd != null) WeaponPickup.Spawn(FightStyle.Sword, 10, at, wd.weaponMaterial);
                Marker = at;
            }
            if (L.evt == "throw" && hero != null && hero.Style == FightStyle.Fists)
            {
                // spent or dropped his sword: hand him another, so there is something to throw
                hero.GiveWeapon(FightStyle.Sword, 10);
                if (InkFx.Instance != null) InkFx.Instance.Burst(hero.Chest, 1.2f);
                Sound.Play(Sfx.SwordClash, 0.5f, hero.transform.position);
            }
            // face the stunt double, so the first tap goes his way
            if (doubles.Count > 0 && doubles[0] != null && hero != null) hero.FaceTowards(doubles[0].transform.position.x);
            Sound.Play(Sfx.CineHit, 0.35f);
        }

        /// <summary>Which side of the hero the nearest stunt double stands (+1 right, -1 left):
        /// the ghost finger taps on that side.</summary>
        public int TargetSide
        {
            get
            {
                if (hero == null) return 1;
                Enemy best = null;
                float bd = float.MaxValue;
                foreach (var d in doubles)
                {
                    if (d == null) continue;
                    float dd = Mathf.Abs(d.transform.position.x - hero.transform.position.x);
                    if (dd < bd) { bd = dd; best = d; }
                }
                return best == null || best.transform.position.x >= hero.transform.position.x ? 1 : -1;
            }
        }

        /// <summary>Where the ghost finger taps in the TAP HIM lesson: the second stunt double.</summary>
        public Vector3? FingerTarget
        {
            get
            {
                if (!Active || Current.gesture != Gesture.TapEnemy) return null;
                for (int i = doubles.Count - 1; i >= 0; i--) if (doubles[i] != null) return doubles[i].Chest;
                return null;
            }
        }

        /// <summary>Skip just this lesson (the SKIP MOVE button).</summary>
        public void SkipLesson()
        {
            if (!Active || Finale || nextAt > 0f) return;
            nextAt = Time.unscaledTime + 0.15f;
            Sound.Play(Sfx.Whoosh, 0.6f);
        }

        /// <summary>A point on the hero's own floor, dx from him (the other side if there is no room).</summary>
        Vector3 Spot(float dx)
        {
            var p = hero.transform.position;
            float x0 = -11.5f, x1 = 11.5f;
            if (LevelNav.Active)
            {
                int pl = LevelNav.PlatformAt(p);
                if (pl < 0) pl = hero.Platform;
                if (pl >= 0 && LevelNav.RegionBounds(pl, out float a, out float b)) { x0 = Mathf.Max(x0, a + 0.5f); x1 = Mathf.Min(x1, b - 0.5f); }
            }
            float x = p.x + dx;
            if (x < x0 || x > x1) x = p.x - dx;
            x = Mathf.Clamp(x, x0, x1);
            float y = hero.Grounded ? p.y : Fighter.GroundY(new Vector3(x, p.y + 0.5f, 0f), 0.6f);
            return new Vector3(x, y, 0f);
        }

        /// <summary>Where each stunt double stands for this lesson, relative to the hero.</summary>
        float[] Marks(Lesson L)
        {
            int f = 1;          // to his right if there is room (Spot flips it otherwise): the taps the demo shows land on the right
            switch (L.evt)
            {
                case "punch": case "kick": case "combo": case "pickup": case "dodge": return new[] { f * 2.3f };
                case "throw": return new[] { f * 5f };
                case "lock": return new[] { f * 2.3f, -f * 2.6f };            // two of them: tap the one behind you                       // across the room: a throw, not a swing
                case "rampage": return new[] { f * 2f, -f * 2.2f, f * 3.4f };
                default: return new float[0];                              // moving lessons: a clear floor
            }
        }

        float[] marks = new float[0];
        float[] awayFor = new float[4];

        void SetupDoubles(Lesson L)
        {
            if (hero == null) return;
            marks = Marks(L);
            for (int i = doubles.Count - 1; i >= 0; i--) if (doubles[i] == null) doubles.RemoveAt(i);
            // too many: send the extras off in a puff
            while (doubles.Count > marks.Length)
            {
                var d = doubles[doubles.Count - 1];
                if (InkFx.Instance != null) InkFx.Instance.Burst(d.Chest, 0.6f);
                Destroy(d.gameObject);
                doubles.RemoveAt(doubles.Count - 1);
            }
            var wd = WaveDirector.Instance;
            for (int i = 0; i < marks.Length; i++)
            {
                var at = Spot(marks[i]);
                if (i < doubles.Count) Place(doubles[i], at);
                else if (wd != null)
                {
                    var e = wd.SpawnDummy(at);
                    if (e != null) { doubles.Add(e); Place(e, at); }
                }
                if (i < awayFor.Length) awayFor[i] = 0f;
            }
        }

        void Place(Enemy e, Vector3 at)
        {
            bool moved = Vector3.Distance(e.transform.position, at) > 0.5f || !e.Targetable;
            if (moved && InkFx.Instance != null) InkFx.Instance.Burst(e.Chest, 0.6f);
            e.ResetTo(at, hero.transform.position.x);
            if (moved && InkFx.Instance != null) InkFx.Instance.Burst(at + Vector3.up * 0.9f, 0.6f);
        }

        /// <summary>Keep the stunt doubles usable: one thrown across the room, knocked off a floor
        /// or left lying too long is put back on his mark.</summary>
        void TendDoubles()
        {
            if (hero == null || marks.Length == 0) return;
            float dt = Time.unscaledDeltaTime;
            var wd = WaveDirector.Instance;
            for (int i = 0; i < marks.Length; i++)
            {
                if (i >= doubles.Count || doubles[i] == null)
                {
                    if (i < doubles.Count) doubles.RemoveAt(i);
                    if (wd == null || !hero.Grounded) continue;
                    var e = wd.SpawnDummy(Spot(marks[i]));
                    if (e != null) { doubles.Insert(Mathf.Min(i, doubles.Count), e); Place(e, Spot(marks[i])); }
                    continue;
                }
                var d = doubles[i];
                var dp = d.transform.position - hero.transform.position;
                bool away = Mathf.Abs(dp.x) > 8f || (Mathf.Abs(dp.y) > 1.2f && d.Grounded && hero.Grounded) || (d.IsDown && d.Grounded);
                awayFor[i] = away ? awayFor[i] + dt : 0f;
                float limit = d.IsDown && Mathf.Abs(dp.x) < 8f && Mathf.Abs(dp.y) < 1.2f ? 3f : 1.2f;   // lying there: give him a moment to get up
                if (awayFor[i] > limit && hero.Grounded) { Place(d, Spot(marks[i])); awayFor[i] = 0f; }
            }
        }

        void OnDid(string what)
        {
            if (!Active || Finale || nextAt > 0f) return;
            var L = lessons[Index];
            if (L.evt == null || L.evt != what) return;
            Progress++;
            if (Progress >= L.goal) Complete();
        }

        void Complete()
        {
            Progress = lessons[Index].goal;
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
            if (story)
            {
                doubles.Clear();
                Active = false;
                Finale = false;
                PlayerPrefs.SetInt(StoryDoneKey, 1);
                PlayerPrefs.Save();
                var done = onFinished;
                onFinished = null;
                done?.Invoke();
                return;
            }
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
