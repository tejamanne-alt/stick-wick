using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE TRAINING GROUND (chapter 1, the three men at the shutter): INSTINCT, played. Every
    /// bare-handed move is a lesson in two beats, with almost nothing to read:
    ///
    ///   WATCH      the men take their marks and the game plays the move itself. A finger appears on the
    ///              screen and makes the gesture, and that finger IS the input (GestureInput.Demo: two
    ///              virtual fingers fed through the same gesture code as a real thumb), so what the player
    ///              sees the finger do is exactly what moves the hero. Along the bottom, the steps as
    ///              little phone screens showing where to touch (high, low, a swipe, a hold), the one
    ///              being made lit up.
    ///   YOUR TURN  back on the marks; the same finger replays as a faint ghost (when he's just started,
    ///              or when he's stuck), the steps stay along the bottom, dots count the reps. GOT IT, and
    ///              the next move. WATCH AGAIN plays the demonstration again at any time.
    ///
    /// The men are sparring partners (Enemy.Sparring / HoldFire / Keep): they swing only when the lesson
    /// needs it, go down and get up, nobody can lose (HeroController.Sparring). After the last lesson (or
    /// at FREE PLAY) it is a playground: all three come at him, a checklist lights each move he lands, a
    /// row tapped replays that lesson, and CONTINUE STORY makes it real (the knife out of his belt, the men
    /// fight to the finish, then the memory). A retry after a defeat in that fight skips the ground.
    /// Moves are recognised from HeroController.Did / Landed and WaveDirector.Styled.</summary>
    public partial class StoryIntro
    {
        /// <summary>The training ground has been played (and left) in this run: a retry goes straight to the fight.</summary>
        public static bool TrainedThisRun;

        /// <summary>Where on the screen a step is made (drawn as a little phone screen).</summary>
        enum Gest { TapHigh, TapLow, TapMan, SwipeAt, SwipeAway, SwipeUp, SwipeUpAt, Hold, CrouchHold, Creep }

        class Step { public Gest g; public string say; public Step(Gest g, string say) { this.g = g; this.say = say; } }

        class Lesson
        {
            public string title;
            public int reps = 1;
            public int guard;                         // man 0's guard: 0 down (no read counters break a string), 1 high, -1 his own
            public bool restage;                      // back on their marks after every rep
            public Step[] steps;
            public Action stage;                      // the marks: where the men stand, who swings
            public Func<IEnumerator> demo;            // the demonstration, made with the virtual fingers
            public Func<char, string, string, bool, bool> match;   // (kind, what, label, low tap)
        }

        enum TS { Title, Demo, Try, Free }

        Lesson[] lessons;
        bool[] learned;
        int li = -1, lreps, demoStep = -1, demoTries;
        bool training, practiseOne, demoHit;
        TS ts = TS.Title;
        float lessonAt, tryAt, repAt = -9f, lessonDoneAt = -9f, lastMoveAt, groundAt, flashRowAt = -9f, restageAt = -1f;
        int flashRow = -1;
        float trainCx;
        float dipAt = -9f, dipSince; Action dipThen; bool dipDone = true;
        bool wasCharging; float chargeMissAt = -1f;
        float runFor, chargeAt, unawareLostAt = -1f, slipAt = -9f, bottleCheckAt;
        bool wasAir, listOpen = true;
        float lastTapAt;
        Coroutine demoCo;
        GUIStyle tTag, tTitle, tCap, tBtn, tRow, tBig, tPill;

        // the demo fingers, as drawn (pixels, Unity screen space: y up)
        readonly Vector2[] fPos = new Vector2[2];
        readonly bool[] fDown = new bool[2];
        readonly float[] fShow = new float[2], fShowNow = new float[2];
        readonly List<Vector2>[] fTrail = { new List<Vector2>(), new List<Vector2>() };
        float rippleAt = -9f; Vector2 rippleAtPos;
        // the swipe's path, shown before and while it is made: marching chevrons from start to end
        bool hintOn; Vector2 hintA, hintB; float hintAt = -9f, hintOffAt = -9f;
        // the recording the ghost replays in YOUR TURN
        struct Rec { public float t; public Vector2 p0, p1; public bool d0, d1; public float s0, s1; public int step; public bool hint; public Vector2 ha, hb; }
        readonly List<Rec> rec = new List<Rec>();
        float recT, recIdle;

        const string PrefMask = "training_mask_v2", PrefSeen = "training_seen";

        // ================================================================ start and end

        void BeginTraining()
        {
            phase = Phase.Training;
            training = true;
            practiseOne = false;
            ts = TS.Title;
            groundAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            StoryCameraFX.Set(0f, 0f);
            if (cam != null) { cam.ScriptRoll = 0f; cam.Scripted(Vector3.zero, Vector3.zero, 0f); }
            barsPx = 0f;
            if (TimeController.Instance != null) TimeController.Instance.CineScale = 1f;
            hero.SetStyle(FightStyle.Fists);                // bare hands: the knife stays in his belt until it is real
            hero.CineRelease(hero.transform.position);
            hero.runSpeed = runSpeed;
            HeroController.Sparring = true;
            float x0 = hero.arenaMinX, x1 = hero.arenaMaxX;
            trainCx = x1 - x0 < 20f ? (x0 + x1) * 0.5f - 1.5f : Mathf.Clamp(hero.transform.position.x, x0 + 8f, x1 - 11f);   // room on the right for the men set aside
            foreach (var e in men)
                if (e != null)
                {
                    e.Passive = false;
                    e.Sparring = true;
                    e.HoldFire = true;
                    e.Keep = false;
                    e.PuppetRelease();
                    e.HealFull();
                }
            BuildLessons();
            learned = new bool[lessons.Length];
            int mask = PlayerPrefs.GetInt(PrefMask, 0);
            for (int i = 0; i < lessons.Length && i < 31; i++) learned[i] = (mask & (1 << i)) != 0;
            lastTapAt = GestureInput.TappedAt;
            HeroController.Did += TrainDid;
            HeroController.Landed += TrainLanded;
            WaveDirector.Styled += TrainStyled;
            Sound.Tension(false);
            Sound.Play(Sfx.Riser, 0.5f);
            Say("THUG", "Easy... boss wants him breathing. Don't break him.", 2.2f);
            if (PlayerPrefs.GetInt(PrefSeen, 0) == 1) EnterFreePlay(false);     // seen it: straight to the playground
        }

        void TrainingUnhook()
        {
            HeroController.Did -= TrainDid;
            HeroController.Landed -= TrainLanded;
            WaveDirector.Styled -= TrainStyled;
            HeroController.Sparring = false;
            GestureInput.Demo = false;
            for (int i = 0; i < GestureInput.Sim.Length; i++) GestureInput.Sim[i].down = false;
        }

        /// <summary>CONTINUE THE STORY: it is real now.</summary>
        void EndTraining()
        {
            if (!training) return;
            StopDemo();
            training = false;
            TrainedThisRun = true;
            PlayerPrefs.SetInt(PrefSeen, 1);
            PlayerPrefs.Save();
            TrainingUnhook();
            foreach (var e in men)
                if (e != null) { e.Sparring = false; e.HoldFire = false; e.Keep = false; e.HealFull(); }
            hero.HealFull();
            if (EscapeSeen) { hero.GiveWeapon(FightStyle.Knife, 4); Sound.Play(Sfx.KnifeGrab, 1f); if (floorKnife != null) floorKnife.gameObject.SetActive(false); }
            Sound.Tension(true);
            Sound.Play(Sfx.CineHit, 0.7f);
            Say("THUG", "Enough. Forget the boss... put him down.", 1.8f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner("NOW IT'S REAL", 1.6f);
            clearedAt = -1f;
            chipUntil = Time.unscaledTime + 5f;
            phase = Phase.Fight;
        }

        // ================================================================ marks

        Enemy Man(int i) => men != null && i >= 0 && i < men.Length ? men[i] : null;
        float MarkX(float dx) => Mathf.Clamp(trainCx + dx, hero.arenaMinX + 0.6f, hero.arenaMaxX - 0.6f);

        /// <summary>A man on his mark: on his feet, healed, standing or coming, swinging or not.</summary>
        void Mark(int i, float dx, bool keep, bool swings)
        {
            var e = Man(i);
            if (e == null) return;
            e.ResetTo(new Vector3(MarkX(dx), home.y, 0f), hero.transform.position.x);
            e.Keep = keep;
            e.HoldFire = !swings;
            e.Sparring = true;
        }

        void HeroAt(float dx)
        {
            hero.CinePose(new Vector3(MarkX(dx), home.y, 0f));
            hero.SetFacing(1);
            hero.CineRelease();
        }

        /// <summary>The common mark: him in the middle, the man for the lesson in front, the other two well
        /// out of the way (standing, watching).</summary>
        void Spar(float gap, bool keep, bool swings)
        {
            HeroAt(0f);
            Mark(0, gap, keep, swings);
            Aside();
        }

        /// <summary>The two men the lesson isn't about: far off on the right, beyond the rush's 8 m, so no
        /// swipe either way can go to them (a swipe away must be a step back, the slip).</summary>
        void Aside()
        {
            float hx = hero.transform.position.x - trainCx;
            Mark(1, hx + 9.5f, true, false);
            Mark(2, hx + 10.6f, true, false);
        }

        void PostAway(int i, float dx)
        {
            var e = Man(i);
            if (e == null) return;
            e.HealFull();
            e.Sparring = true;
            e.Keep = true;
            e.HoldFire = true;
            e.PostUnaware(new Vector3(MarkX(dx), home.y, 0f), 1);
            unawareLostAt = -1f;
        }

        void Bottle(float dx)
        {
            var at = new Vector3(MarkX(dx), home.y, 0f);
            if (Throwable.Near(at, 1.5f) == null) Throwable.Spawn(PropKind.Bottle, at);
        }

        static bool IsBody(string clip) => clip == "liver_hook" || clip == "body_cross";
        static readonly string[] OwnStrings = { "DUTCH TWO-AND-LOW", "ONE-TWO-LIVER", "UP-DOWN-UP", "LIVER AND LIGHTS", "DIG AND LIFT", "CLOSE QUARTERS", "KARATE", "PALM HEEL", "FOUR-PIECE", "ONE-TWO-KICK" };
        static bool Named(string label, string name) => label != null && label.StartsWith(name);
        static Step S(Gest g, string say) => new Step(g, say);

        // ================================================================ the lessons

        void BuildLessons()
        {
            lessons = new[]
            {
                new Lesson { title = "RUN",
                    steps = new[] { S(Gest.Hold, "HOLD") },
                    stage = () => { HeroAt(-4f); Mark(0, 6.5f, true, false); Mark(1, 7.5f, true, false); Mark(2, -7f, true, false); },
                    demo = () => DemoRun(),
                    match = (k, a, l, low) => k == 't' && a == "run" },
                new Lesson { title = "JUMP", reps = 2,
                    steps = new[] { S(Gest.SwipeUp, "SWIPE UP"), S(Gest.SwipeUpAt, "UP-RIGHT") },
                    stage = () => { HeroAt(-2f); Mark(0, 6.5f, true, false); Mark(1, 7.5f, true, false); Mark(2, -7f, true, false); },
                    demo = () => DemoJump(),
                    match = (k, a, l, low) => k == 't' && a == "jump" },
                new Lesson { title = "PICK YOUR MAN",
                    steps = new[] { S(Gest.TapMan, "TAP HIM") },
                    stage = () => { HeroAt(-1f); Mark(1, 1.6f, true, false); Mark(0, -5f, true, false); Mark(2, 6.5f, true, false); },
                    demo = () => DemoTapMan(1),
                    match = (k, a, l, low) => k == 't' && a == "lock" },
                new Lesson { title = "PUNCH", reps = 3,
                    steps = new[] { S(Gest.TapHigh, "TAP HIGH") },
                    stage = () => Spar(1.2f, true, false),
                    demo = () => DemoTaps(true, 3, 0.45f),
                    match = (k, a, l, low) => k == 'l' && !low },
                new Lesson { title = "KICK", reps = 3,
                    steps = new[] { S(Gest.TapLow, "TAP LOW") },
                    stage = () => Spar(1.45f, true, false),
                    demo = () => DemoTaps(false, 3, 0.55f),
                    match = (k, a, l, low) => k == 'l' && low },
                new Lesson { title = "TO THE BODY", reps = 2, restage = true,
                    steps = new[] { S(Gest.TapLow, "CLOSE: TAP LOW") },
                    stage = () => Spar(0.75f, true, false),
                    demo = () => DemoTaps(false, 1, 0.5f),
                    match = (k, a, l, low) => k == 'l' && IsBody(a) },
                new Lesson { title = "BOXER'S FOUR",
                    steps = new[] { S(Gest.TapHigh, "HIGH"), S(Gest.TapHigh, "HIGH"), S(Gest.TapHigh, "HIGH"), S(Gest.TapHigh, "HIGH") },
                    stage = () => Spar(1.05f, true, false),
                    demo = () => DemoString("HHHH"),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && Named(l, "BOXER'S FOUR") },
                new Lesson { title = "KICK STRING",
                    steps = new[] { S(Gest.TapLow, "LOW"), S(Gest.TapLow, "LOW"), S(Gest.TapLow, "LOW") },
                    stage = () => Spar(1.45f, true, false),
                    demo = () => DemoString("LLL"),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && Named(l, "KICK STRING") },
                new Lesson { title = "MIX A STRING",
                    steps = new[] { S(Gest.TapHigh, "HIGH"), S(Gest.TapHigh, "HIGH"), S(Gest.TapLow, "LOW") },
                    stage = () => Spar(1.05f, true, false),
                    demo = () => DemoString("HHL"),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && l != null && Array.Exists(OwnStrings, n => l.StartsWith(n)) },
                new Lesson { title = "UNDER THE GUARD", reps = 2, guard = 1,
                    steps = new[] { S(Gest.TapLow, "HANDS UP? GO LOW") },
                    stage = () => { Spar(1.2f, true, false); var a = Man(0); if (a != null) a.ForceGuard(1, 6f); },
                    demo = () => DemoGuard(),
                    match = (k, a, l, low) => k == 'd' && a == "read" },
                new Lesson { title = "PARRY", reps = 2, guard = -1,
                    steps = new[] { S(Gest.TapHigh, "HE SWINGS: TAP") },
                    stage = () => Spar(1.3f, false, true),
                    demo = () => DemoParry(),
                    match = (k, a, l, low) => k == 'd' && a == "parry" },
                new Lesson { title = "SLIP AND COUNTER", guard = -1,
                    steps = new[] { S(Gest.SwipeAway, "HE SWINGS: SWIPE AWAY"), S(Gest.TapHigh, "HIT BACK") },
                    stage = () => Spar(1.4f, false, true),
                    demo = () => DemoSlip(),
                    match = (k, a, l, low) => k == 't' && a == "slip" },
                new Lesson { title = "GRAB HIM",
                    steps = new[] { S(Gest.SwipeAt, "CLOSE: SWIPE AT HIM") },
                    stage = () => Spar(0.9f, true, false), restage = true,
                    demo = () => DemoGrab(false),
                    match = (k, a, l, low) => k == 'd' && a == "clinch" },
                new Lesson { title = "THROW HIM", reps = 2,
                    steps = new[] { S(Gest.SwipeAt, "GRAB"), S(Gest.TapHigh, "KNEE"), S(Gest.SwipeAt, "SWIPE AT: THROW") },
                    stage = () => Spar(0.9f, true, false), restage = true,
                    demo = () => DemoGrab(true),
                    match = (k, a, l, low) => k == 'd' && a == "throw" },          // any throw from the hold (reap, hip, shoulder, suplex, DDT, sweep)
                new Lesson { title = "SPEAR",
                    steps = new[] { S(Gest.SwipeAt, "TWO STEPS OFF: SWIPE FLAT AT HIM") },
                    stage = () => Spar(2.2f, true, false), restage = true,
                    demo = () => DemoSwipeAt(false),
                    match = (k, a, l, low) => k == 's' && l != null && l.StartsWith("SPEAR") },
                new Lesson { title = "HEAD SCISSORS",
                    steps = new[] { S(Gest.SwipeUpAt, "CLOSE: SWIPE UP AT HIM") },
                    stage = () => Spar(1.4f, true, false), restage = true,
                    demo = () => DemoSwipeAt(true),
                    match = (k, a, l, low) => k == 's' && l != null && l.StartsWith("HEAD SCISSORS") },
                new Lesson { title = "RUSH IN",
                    steps = new[] { S(Gest.SwipeAt, "FAR OFF: SWIPE AT HIM") },
                    stage = () => { HeroAt(-3f); Mark(0, 2.6f, true, false); Aside(); }, restage = true,
                    demo = () => DemoRush(),
                    match = (k, a, l, low) => k == 'd' && a == "far_rush" },
                new Lesson { title = "FLYING KNEE", reps = 2,
                    steps = new[] { S(Gest.Hold, "HOLD: RUN AT HIM"), S(Gest.TapHigh, "CLOSE: TAP HIGH") },
                    stage = () => { HeroAt(-3.5f); Mark(0, 3.5f, true, false); Aside(); }, restage = true,
                    demo = () => DemoRunning(true),
                    match = (k, a, l, low) => k == 'd' && (a == "far_knee" || a == "far_barge") },
                new Lesson { title = "SLIDE TACKLE",
                    steps = new[] { S(Gest.Hold, "HOLD: RUN AT HIM"), S(Gest.TapLow, "CLOSE: TAP LOW") },
                    stage = () => { HeroAt(-3.5f); Mark(0, 3.5f, true, false); Aside(); }, restage = true,
                    demo = () => DemoRunning(false),
                    match = (k, a, l, low) => k == 'd' && a == "far_slide" },
                new Lesson { title = "SIDESTEP THE BULL",
                    steps = new[] { S(Gest.SwipeAway, "HE CHARGES: SWIPE") },
                    stage = () => { HeroAt(-2f); Mark(0, 3.8f, true, false); Aside(); chargeAt = Time.time + 1.2f; }, restage = true,
                    demo = () => DemoSidestep(),
                    match = (k, a, l, low) => k == 'd' && a == "sidestep" },
                new Lesson { title = "CROSSFIRE",
                    steps = new[] { S(Gest.TapHigh, "ONE EACH SIDE: TAP") },
                    stage = () => { HeroAt(0f); Mark(0, 1.0f, true, false); Mark(1, -1.0f, true, false); Mark(2, 6.5f, true, false); }, restage = true,
                    demo = () => DemoTaps(true, 1, 0.3f),
                    match = (k, a, l, low) => k == 'd' && a == "crossfire" },
                new Lesson { title = "SILENT TAKEDOWN",
                    steps = new[] { S(Gest.CrouchHold, "SWIPE DOWN, HOLD"), S(Gest.Creep, "SLIDE: CREEP"), S(Gest.TapMan, "TAP HIS BACK") },
                    stage = () => { HeroAt(-2f); PostAway(2, 0.2f); Mark(0, 8f, true, false); Mark(1, 9f, true, false); }, restage = true,
                    demo = () => DemoTakedown(),
                    match = (k, a, l, low) => k == 'd' && (a == "takedown" || a == "far_takedown") },
                new Lesson { title = "THROW A BOTTLE",
                    steps = new[] { S(Gest.Hold, "WALK OVER IT"), S(Gest.TapMan, "TAP A MAN") },
                    stage = () => { HeroAt(-3f); Mark(0, 3.5f, true, false); Aside(); Bottle(-2f); }, restage = true,
                    demo = () => DemoBottle(),
                    match = (k, a, l, low) => k == 'd' && (a == "far_throw" || a == "far_smash") },
            };
        }

        // ================================================================ the demonstrations (real input, made by a visible finger)

        float W => Screen.width;
        float H => Screen.height;
        Vector2 Px(float fx, float fy) => new Vector2(W * fx, H * fy);

        Vector2 ScreenPx(Vector3 world)
        {
            var c = Camera.main;
            if (c == null) return Px(0.5f, 0.5f);
            var p = c.WorldToScreenPoint(world);
            return new Vector2(p.x, p.y);
        }

        float KickY => H * (GestureInput.Instance != null ? GestureInput.Instance.kickLine : 0.45f);

        /// <summary>A clear place to tap on that half of the screen, high or low: not on a man (a tap on a man
        /// picks him and always punches), not on a thing.</summary>
        Vector2 Spot(int side, bool high)
        {
            float y = high ? Mathf.Lerp(KickY, H, 0.42f) : KickY * 0.45f;
            float[] xs = side > 0 ? new[] { 0.9f, 0.8f, 0.7f, 0.62f } : new[] { 0.1f, 0.2f, 0.3f, 0.38f };
            foreach (var fx in xs)
            {
                var p = new Vector2(W * fx, y);
                if (GestureInput.EnemyAt(p) == null && GestureInput.ThingAt(p) == null && !GestureInput.IsBlocked(p)) return p;
            }
            return new Vector2(W * xs[0], y);
        }

        Vector2 ManPx(Enemy e) => e != null ? ScreenPx(e.Chest) : Px(0.75f, 0.55f);
        Vector2 HeroPx => ScreenPx(hero.Chest);

        /// <summary>Demo time: stands still while the game is paused.</summary>
        static float UDt => TimeController.Paused ? 0f : Time.unscaledDeltaTime;
        static IEnumerator WaitU(float s) { float t = 0f; while (t < s) { t += UDt; yield return null; } }

        void SetF(int i, Vector2 p, bool down)
        {
            fPos[i] = p;
            fDown[i] = down;
            GestureInput.Sim[i].pos = p;
            GestureInput.Sim[i].down = down && GestureInput.Demo;
        }

        /// <summary>The finger glides there (lifted, not touching).</summary>
        IEnumerator Glide(int i, Vector2 to, float t = 0.32f)
        {
            if (fShow[i] < 0.5f) { fPos[i] = to + new Vector2(W * 0.06f, -H * 0.12f); fTrail[i].Clear(); }
            fShow[i] = 1f;
            var from = fPos[i];
            float dist = (to - from).magnitude;
            if (dist < 6f) { SetF(i, to, false); yield break; }
            t = Mathf.Lerp(0.1f, t, Mathf.Clamp01(dist / (W * 0.25f)));          // a short hop is quick
            for (float k = 0f; k < 1f; k += UDt / t) { SetF(i, Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k)), false); yield return null; }
            SetF(i, to, false);
        }

        IEnumerator Tap(Vector2 at, int step, int i = 0, float settle = 0.14f)
        {
            demoStep = step;
            yield return Glide(i, at);
            yield return WaitU(settle);
            SetF(i, at, true);
            yield return WaitU(0.08f);
            SetF(i, at, false);
            rippleAt = Time.unscaledTime; rippleAtPos = at;
            yield return WaitU(0.1f);
        }

        void Hint(Vector2 a, Vector2 b) { hintOn = true; hintA = a; hintB = b; hintAt = Time.unscaledTime; }
        void HintOff() { if (hintOn) { hintOn = false; hintOffAt = Time.unscaledTime; } }

        /// <summary>A swipe: its path appears (chevrons marching from start to end), the finger lands on the
        /// start, presses, and sweeps (fast off the mark, easing at the end) leaving a streak behind it.</summary>
        IEnumerator Swipe(Vector2 a, Vector2 b, int step, float dur = 0.3f, int i = 0, bool hinted = false)
        {
            demoStep = step;
            if (!hinted) Hint(a, b);
            yield return Glide(i, a);
            if (!hinted) yield return WaitU(0.35f);
            fTrail[i].Clear();
            SetF(i, a, true);
            fTrail[i].Add(a);
            yield return null;
            for (float k = 0f; k < 1f; k += UDt / dur)
            {
                float e = 1f - (1f - k) * (1f - k);              // ease out: well past the swipe threshold at once
                SetF(i, Vector2.Lerp(a, b, e), true);
                fTrail[i].Add(fPos[i]);
                yield return null;
            }
            SetF(i, b, true);
            fTrail[i].Add(b);
            yield return null;
            SetF(i, b, false);
            rippleAt = Time.unscaledTime; rippleAtPos = b;
            HintOff();
            yield return WaitU(0.25f);
        }

        /// <summary>Pressed and held there until `until` (at least `min`, at most `max` seconds).</summary>
        IEnumerator HoldAt(Vector2 at, int step, Func<bool> until, float min, float max, int i = 0)
        {
            demoStep = step;
            yield return Glide(i, at);
            yield return WaitU(0.12f);
            float t = 0f;
            while (t < max && (t < min || until == null || !until()))
            {
                SetF(i, at, true);
                t += UDt;
                yield return null;
            }
        }

        void Lift(int i) { SetF(i, fPos[i], false); }

        float Gap(Enemy e) => e != null ? Mathf.Abs(e.transform.position.x - hero.transform.position.x) : 99f;

        IEnumerator DemoRun()
        {
            yield return HoldAt(Px(0.86f, 0.5f), 0, () => false, 1.6f, 1.6f);
            Lift(0);
        }

        IEnumerator DemoJump()
        {
            yield return Swipe(Px(0.62f, 0.32f), Px(0.62f, 0.6f), 0);
            yield return WaitU(1.0f);
            yield return Swipe(Px(0.6f, 0.32f), Px(0.74f, 0.56f), 1);
        }

        IEnumerator DemoTapMan(int who)
        {
            yield return Tap(ManPx(Man(who)), 0);
        }

        IEnumerator DemoTaps(bool high, int n, float every)
        {
            for (int k = 0; k < n; k++) { yield return Tap(Spot(1, high), 0, 0, k == 0 ? 0.14f : 0.02f); yield return WaitU(every); }
        }

        /// <summary>A string, in rhythm: H high, L low.</summary>
        IEnumerator DemoString(string s)
        {
            for (int k = 0; k < s.Length; k++) { yield return Tap(Spot(1, s[k] == 'H'), k, 0, k == 0 ? 0.14f : 0.02f); yield return WaitU(0.28f); }
        }

        IEnumerator DemoGuard()
        {
            var a = Man(0);
            float t = 0f;
            while (t < 3f && (a == null || a.Read != Opening.GuardHigh)) { if (a != null) a.ForceGuard(1, 2f); t += UDt; yield return null; }
            yield return WaitU(0.3f);
            yield return Tap(Spot(1, false), 0);
        }

        IEnumerator DemoParry()
        {
            var a = Man(0);
            yield return Glide(0, Spot(1, true));
            float t = 0f;
            while (t < 6f && (a == null || !a.Telegraphing)) { t += UDt; yield return null; }
            yield return Tap(Spot(1, true), 0, 0, 0.02f);
            yield return WaitU(0.8f);
        }

        IEnumerator DemoSlip()
        {
            var a = Man(0);
            var c = HeroPx;
            Hint(c + new Vector2(-W * 0.04f, 0f), c + new Vector2(-W * 0.26f, 0f));
            yield return Glide(0, c + new Vector2(-W * 0.04f, 0f));
            float t = 0f;
            while (t < 6f && (a == null || !a.Telegraphing)) { t += UDt; yield return null; }
            yield return Swipe(fPos[0], fPos[0] + new Vector2(-W * 0.22f, 0f), 0, 0.16f, 0, true);
            yield return WaitU(0.25f);
            yield return Tap(Spot(1, true), 1, 0, 0.02f);
            yield return WaitU(0.2f);
            yield return Tap(Spot(1, true), 1, 0, 0.02f);
        }

        IEnumerator DemoGrab(bool throwHim)
        {
            var c = HeroPx;
            yield return Swipe(c + new Vector2(W * 0.04f, 0f), c + new Vector2(W * 0.28f, 0f), 0);
            if (!throwHim) { yield return WaitU(0.8f); yield break; }
            yield return WaitU(0.5f);
            yield return Tap(Spot(1, true), 1);
            yield return WaitU(0.55f);
            c = HeroPx;
            yield return Swipe(c + new Vector2(W * 0.04f, 0f), c + new Vector2(W * 0.28f, 0f), 2);
            yield return WaitU(1.0f);
        }

        /// <summary>A swipe at the man from beside the hero: flat (the spear) or up and across (the head scissors).</summary>
        IEnumerator DemoSwipeAt(bool up)
        {
            var c = HeroPx;
            var a = c + new Vector2(W * 0.04f, up ? -H * 0.04f : 0f);
            var b = up ? a + new Vector2(W * 0.15f, H * 0.26f) : a + new Vector2(W * 0.26f, 0f);
            yield return Swipe(a, b, 0);
            yield return WaitU(1.6f);
        }

        IEnumerator DemoRush()
        {
            var c = HeroPx;
            yield return Swipe(c + new Vector2(W * 0.04f, 0f), c + new Vector2(W * 0.3f, 0f), 0);
            yield return WaitU(1.4f);
        }

        IEnumerator DemoRunning(bool high)
        {
            var a = Man(0);
            var hp = Px(0.62f, 0.5f);
            var tp = Spot(1, high);
            demoStep = 0;
            yield return Glide(0, hp);
            yield return WaitU(0.12f);
            // finger one holds (he runs); finger two comes to hover over its spot, and taps at the moment
            fShow[1] = 1f;
            fPos[1] = tp + new Vector2(W * 0.05f, -H * 0.1f);
            fTrail[1].Clear();
            float t = 0f, thr = high ? 2.3f : 2.2f;
            while (t < 3f && (t < 0.5f || Gap(a) > thr))
            {
                SetF(0, hp, true);
                tp = Spot(1, high);
                SetF(1, Vector2.Lerp(fPos[1], tp, UDt * 8f), false);
                t += UDt;
                yield return null;
            }
            demoStep = 1;
            tp = Spot(1, high);
            SetF(1, tp, true);
            yield return WaitU(0.08f);
            SetF(1, tp, false);
            rippleAt = Time.unscaledTime; rippleAtPos = tp;
            yield return WaitU(0.25f);
            Lift(0);
            yield return WaitU(1.0f);
        }

        IEnumerator DemoSidestep()
        {
            var b = Man(0);
            var c = HeroPx;
            Hint(c + new Vector2(-W * 0.04f, 0f), c + new Vector2(-W * 0.26f, 0f));
            yield return Glide(0, c + new Vector2(-W * 0.04f, 0f));
            float t = 0f;
            while (t < 6f && (b == null || !b.Charging || Gap(b) > 4.3f)) { t += UDt; yield return null; }
            yield return Swipe(fPos[0], fPos[0] + new Vector2(-W * 0.22f, 0f), 0, 0.16f, 0, true);
            yield return WaitU(1.0f);
        }

        IEnumerator DemoTakedown()
        {
            var e = Man(2);
            // finger one: swipe down and keep holding (the crouch), then slide to creep
            var a = Px(0.7f, 0.62f);
            var b = a + new Vector2(0f, -H * 0.16f);
            demoStep = 0;
            Hint(a, b);
            yield return Glide(0, a);
            yield return WaitU(0.35f);
            fTrail[0].Clear();
            for (float k = 0f; k < 1f; k += UDt / 0.24f) { SetF(0, Vector2.Lerp(a, b, k), true); fTrail[0].Add(fPos[0]); yield return null; }
            HintOff();
            for (float k = 0f; k < 0.35f; k += UDt) { SetF(0, b, true); yield return null; }
            demoStep = 1;
            var cr = b + new Vector2(W * 0.06f, 0f);
            for (float k = 0f; k < 1f; k += UDt / 0.2f) { SetF(0, Vector2.Lerp(b, cr, k), true); yield return null; }
            float t = 0f;
            while (t < 6f && e != null && (e.transform.position.x - hero.transform.position.x) > 0.85f) { SetF(0, cr, true); t += UDt; yield return null; }
            SetF(0, b, true);
            // finger two: the tap on his back, the crouch still held
            yield return Tap(ManPx(e), 2, 1);
            for (float k = 0f; k < 0.3f; k += UDt) { SetF(0, b, true); yield return null; }
            Lift(0);
            yield return WaitU(1.6f);
        }

        IEnumerator DemoBottle()
        {
            yield return HoldAt(Px(0.86f, 0.5f), 0, () => hero.Carried != null, 0.3f, 3f);
            Lift(0);
            yield return WaitU(0.6f);
            yield return Tap(ManPx(Man(0)), 1);
            yield return WaitU(1.0f);
        }

        // ================================================================ the flow

        void StartLesson(int i, bool one = false)
        {
            if (lessons == null || i < 0 || i >= lessons.Length) return;
            li = i;
            lreps = 0;
            practiseOne = one;
            lessonAt = Time.unscaledTime;
            lessonDoneAt = -9f;
            restageAt = -1f;
            demoTries = 0;
            Sound.Play(Sfx.Glint, 0.8f);
            StartDemo();
        }

        void StartDemo()
        {
            StopDemo();
            ts = TS.Demo;
            demoHit = false;
            demoStep = -1;
            rec.Clear();
            recT = recIdle = 0f;
            Dip(lessons[li].stage);
            demoCo = StartCoroutine(RunDemo());
        }

        void StopDemo()
        {
            if (demoCo != null) { StopCoroutine(demoCo); demoCo = null; }
            for (int i = 0; i < 2; i++) { fDown[i] = false; fShow[i] = 0f; }
            if (GestureInput.Instance != null) GestureInput.Instance.CancelSim();
            hintOn = false;
            GestureInput.Demo = false;
        }

        IEnumerator RunDemo()
        {
            yield return new WaitUntil(() => dipDone);
            if (GestureInput.Instance != null) GestureInput.Instance.ReleaseReal();
            GestureInput.Demo = true;
            yield return WaitU(0.55f);
            yield return lessons[li].demo();
            for (int i = 0; i < 2; i++) Lift(i);
            yield return null;
            yield return WaitU(0.5f);
            fShow[0] = fShow[1] = 0f;
            demoStep = -1;
            yield return WaitU(0.5f);
            GestureInput.Demo = false;
            demoCo = null;
            // it didn't come off (a man moved): once more
            if (!demoHit && demoTries < 1) { demoTries++; StartDemo(); yield break; }
            BeginTry();
        }

        void BeginTry()
        {
            ts = TS.Try;
            tryAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            if (GestureInput.Instance != null) GestureInput.Instance.Flush();
            Dip(lessons[li].stage);
            Sound.Play(Sfx.Glint, 1f);
        }

        void Dip(Action then)
        {
            dipAt = Time.unscaledTime;
            dipSince = 0f;
            dipThen = then;
            dipDone = false;
        }

        void UpdateDip()
        {
            if (dipDone) return;
            dipSince += UDt;
            bool busy = !hero.Free;
            foreach (var e in men) if (e != null && e.Held) busy = true;
            if (dipSince >= 0.22f && (!busy || dipSince > 2.5f))
            {
                hero.CineLock();
                hero.DropCarried();
                foreach (var tp in Throwable.All.ToArray()) if (tp != null && tp.Lying) Destroy(tp.gameObject);
                foreach (var e in men) if (e != null) { e.HealFull(); e.DropProp(); }
                hero.HealFull();
                dipThen?.Invoke();
                dipDone = true;
                dipAt = Time.unscaledTime;
                Sound.Play(Sfx.Whoosh, 0.4f);
            }
        }

        void Rep()
        {
            lreps++;
            repAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            chargeMissAt = -1f;
            var L = lessons[li];
            if (lreps >= L.reps)
            {
                lessonDoneAt = Time.unscaledTime;
                restageAt = -1f;
                MarkLearned(li);
                Sound.Play(Sfx.RankUp, 0.9f);
            }
            else
            {
                Sound.Play(Sfx.Glint, 1f);
                if (L.restage) restageAt = Time.unscaledTime + 1.1f;
            }
        }

        void MarkLearned(int i)
        {
            if (learned[i]) return;
            learned[i] = true;
            int mask = 0;
            for (int k = 0; k < learned.Length && k < 31; k++) if (learned[k]) mask |= 1 << k;
            PlayerPrefs.SetInt(PrefMask, mask);
            PlayerPrefs.Save();
        }

        void NextLesson()
        {
            if (practiseOne) { EnterFreePlay(false); return; }
            int n = li + 1;
            if (n >= lessons.Length) { EnterFreePlay(true); return; }
            StartLesson(n);
        }

        void EnterFreePlay(bool graduated)
        {
            StopDemo();
            ts = TS.Free;
            practiseOne = false;
            li = -1;
            lessonAt = Time.unscaledTime;
            PlayerPrefs.SetInt(PrefSeen, 1);
            PlayerPrefs.Save();
            foreach (var e in men) if (e != null) { e.Keep = false; e.HoldFire = false; e.Sparring = true; if (e.Unaware) e.PlaceAt(e.transform.position, -1); }
            if (graduated)
            {
                Sound.Play(Sfx.WaveClear, 0.9f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner("EVERY MOVE. NOW PLAY", 2f);
                Say("THUG", "He's fighting like... like one of us. Get him!", 2f);
            }
            Bottle(-4f);
        }

        // ================================================================ what he did

        void TrainDid(string what)
        {
            if (!training) return;
            if (what == "dodge")
                foreach (var e in men)
                    if (e != null && e.Alive && (e.Telegraphing || e.Engaged) && Mathf.Abs(e.transform.position.x - hero.transform.position.x) < 2.6f) slipAt = Time.unscaledTime;
            Fire('d', what, null, false);
        }

        void TrainLanded(string clip, string label, bool low)
        {
            if (!training) return;
            lastMoveAt = Time.unscaledTime;
            if (Time.unscaledTime - slipAt < 1.8f) { slipAt = -9f; Fire('t', "slip", null, false); }
            Fire('l', clip, label, low);
        }

        void TrainStyled(string label)
        {
            if (!training) return;
            if (label != null && (label.Contains("OVERREACH") || label.Contains("SLIPPED"))) slipAt = Time.unscaledTime;
            Fire('s', label, label, false);
        }

        void Fire(char k, string a, string label, bool low)
        {
            if (lessons == null) return;
            if (ts == TS.Free)
            {
                for (int i = 0; i < lessons.Length; i++)
                    if (lessons[i].match(k, a, label, low))
                    {
                        if (!learned[i]) Sound.Play(Sfx.Glint, 0.9f);
                        MarkLearned(i);
                        flashRow = i;
                        flashRowAt = Time.unscaledTime;
                    }
                return;
            }
            if (li < 0 || li >= lessons.Length || !dipDone) return;
            if (!lessons[li].match(k, a, label, low)) return;
            if (ts == TS.Demo) { demoHit = true; return; }
            if (ts == TS.Try && lessonDoneAt < 0f) Rep();
        }

        // ================================================================ every frame

        void UpdateTraining()
        {
            if (!training) return;
            if (TimeController.Paused)
            {
                // paused: every timer of the ground is pushed on by the pause, so nothing moves on under it
                float d = Time.unscaledDeltaTime;
                groundAt += d; lessonAt += d; tryAt += d; lastMoveAt += d;
                if (lessonDoneAt > 0f) lessonDoneAt += d;
                if (restageAt > 0f) restageAt += d;
                if (unawareLostAt > 0f) unawareLostAt += d;
                return;
            }
            float now = Time.unscaledTime;
            UpdateDip();
            if (ts == TS.Title && now - groundAt > 3.2f) StartLesson(0);
            if (ts == TS.Try && lessonDoneAt > 0f && now - lessonDoneAt > 1.5f) { lessonDoneAt = -9f; NextLesson(); }
            if (ts == TS.Try && restageAt > 0f && now > restageAt) { restageAt = -1f; Dip(lessons[li].stage); }

            // sparring: nobody stays hurt for long, nobody dies
            foreach (var e in men)
                if (e != null && e.Alive && !e.Held && e.HealthFraction < 0.999f) e.HealFull();
            if (hero.Free && hero.Health < hero.maxHealth) hero.HealFull();

            // moves read off his body
            if (hero.RunningFast) runFor += Time.deltaTime; else runFor = 0f;
            if (runFor > 0.35f) { runFor = -99f; Fire('t', "run", null, false); }
            bool air = !hero.Grounded && !hero.OnLedge;
            if (air && !wasAir) Fire('t', "jump", null, false);
            wasAir = air;
            if (GestureInput.TappedAt > lastTapAt) { lastTapAt = GestureInput.TappedAt; if (GestureInput.TappedEnemy != null) Fire('t', "lock", null, false); }

            // the demo fingers: drawn alpha, and the recording the ghost replays
            for (int i = 0; i < 2; i++) fShowNow[i] = Mathf.MoveTowards(fShowNow[i], fShow[i], Time.unscaledDeltaTime * 5f);
            if (ts == TS.Demo && GestureInput.Demo)
            {
                bool active = fDown[0] || fDown[1] || (rec.Count > 0 && (rec[rec.Count - 1].p0 - fPos[0]).sqrMagnitude > 1f);
                recIdle = active ? 0f : recIdle + Time.unscaledDeltaTime;
                if (recIdle < 0.35f) recT += Time.unscaledDeltaTime;            // long waits (for his swing) shrink in the replay
                rec.Add(new Rec { t = recT, p0 = fPos[0], p1 = fPos[1], d0 = fDown[0], d1 = fDown[1], s0 = fShowNow[0], s1 = fShowNow[1], step = demoStep, hint = hintOn, ha = hintA, hb = hintB });
            }

            // the lessons' own staging, kept up
            var L = (ts == TS.Demo || ts == TS.Try) && li >= 0 ? lessons[li] : null;
            if (L != null && dipDone && lessonDoneAt < 0f)
            {
                if (L.guard >= 0) { var a = Man(0); if (a != null && a.Alive) a.ForceGuard(L.guard, 1.5f); }
                if (L.title == "SIDESTEP THE BULL")
                {
                    var b = Man(0);
                    // a charge that ended without the sidestep (he was hit, or jumped it): back on the marks
                    if (b != null && wasCharging && !b.Charging && ts == TS.Try) chargeMissAt = now + 1.0f;
                    wasCharging = b != null && b.Charging;
                    if (chargeMissAt > 0f && now > chargeMissAt) { chargeMissAt = -1f; if (lessonDoneAt < 0f && restageAt < 0f) Dip(L.stage); }
                    if (b != null && b.Alive && !b.Charging && !b.IsDown && Time.time > chargeAt)
                    {
                        float gap = Gap(b);
                        if (gap > 3f && gap < 9f) b.ChargeAt();
                        chargeAt = Time.time + 3.6f;
                    }
                }
                if (L.title == "SILENT TAKEDOWN" && ts == TS.Try)
                {
                    var c = Man(2);
                    if (c != null && !c.Unaware && !c.Held)
                    {
                        if (unawareLostAt < 0f) { unawareLostAt = now; Say("THUG", "Hey! I can hear you!", 1.2f); }
                        else if (now - unawareLostAt > 1.6f) Dip(L.stage);
                    }
                }
                if (L.title == "THROW A BOTTLE" && ts == TS.Try && now > bottleCheckAt)
                {
                    bottleCheckAt = now + 1.5f;
                    if (hero.Carried == null && Throwable.Near(hero.transform.position, 30f) == null) Bottle(-2f);
                }
            }
            if (ts == TS.Free && now > bottleCheckAt)
            {
                bottleCheckAt = now + 4f;
                if (hero.Carried == null && Throwable.Near(new Vector3(trainCx, home.y, 0f), 30f) == null) Bottle(UnityEngine.Random.value < 0.5f ? -4f : 4f);
            }
        }

        // ================================================================ the picture

        void TrainingStyles()
        {
            if (tTitle != null) return;
            tTag = new GUIStyle(cap) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
            tTitle = new GUIStyle(big) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            tCap = new GUIStyle(cap) { fontSize = 17, alignment = TextAnchor.UpperCenter, wordWrap = true };
            tPill = new GUIStyle(big) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            tBtn = new GUIStyle(btn) { fontSize = 16 };
            tRow = new GUIStyle(cap) { fontSize = 15, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            tBig = new GUIStyle(big) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
        }

        bool TrainButton(Rect r, string label, bool strong = false, float pulse = 0f)
        {
            float p = pulse > 0f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            Fill(r, strong ? new Color(0.62f, 0.05f, 0.06f, 0.92f) : new Color(0.5f * p, 0.02f * p, 0.02f * p, 0.62f + 0.2f * p));
            Fill(new Rect(r.x, r.y, r.width, 1), new Color(1f, 1f, 1f, strong ? 0.6f : 0.35f + 0.4f * p));
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(1f, 1f, 1f, strong ? 0.6f : 0.35f + 0.4f * p));
            tBtn.normal.textColor = Color.white;
            if (Event.current.type == EventType.Repaint) GestureInput.BlockGui(r, scale);
            return GUI.Button(r, label, tBtn);
        }

        Vector2 Gui(Vector2 px) => new Vector2(px.x / scale, (Screen.height - px.y) / scale);

        void Line(Vector2 a, Vector2 b, float th, Color c)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
            Fill(new Rect(a.x, a.y - th * 0.5f, len, th), c);
            GUI.matrix = m;
        }

        void Arrow(Vector2 a, Vector2 b, float th, Color c)
        {
            Line(a, b, th, c);
            var d = (b - a).normalized;
            var n = new Vector2(-d.y, d.x);
            float hs = th * 3.2f;
            Line(b, b - d * hs + n * hs * 0.8f, th, c);
            Line(b, b - d * hs - n * hs * 0.8f, th, c);
        }

        /// <summary>A step as a little landscape phone screen: where the thumb goes, and how.</summary>
        void StepIcon(Rect r, Gest g, float a, bool lit)
        {
            var edge = new Color(1f, 1f, 1f, (lit ? 0.95f : 0.45f) * a);
            var ink = lit ? new Color(1f, 1f, 1f, a) : new Color(1f, 1f, 1f, 0.55f * a);
            var red = new Color(0.9f, 0.12f, 0.1f, a);
            Fill(r, new Color(0f, 0f, 0f, 0.55f * a));
            Fill(new Rect(r.x, r.y, r.width, 2), edge); Fill(new Rect(r.x, r.yMax - 2, r.width, 2), edge);
            Fill(new Rect(r.x, r.y, 2, r.height), edge); Fill(new Rect(r.xMax - 2, r.y, 2, r.height), edge);
            float midY = r.y + r.height * 0.5f;
            var h0 = new Vector2(r.center.x, r.y + r.height * 0.62f);
            // the hero: a tiny figure in the middle
            Fill(new Rect(h0.x - 2, h0.y - 12, 4, 16), new Color(1f, 1f, 1f, 0.35f * a));
            Fill(new Rect(h0.x - 4, h0.y - 19, 8, 8), new Color(1f, 1f, 1f, 0.35f * a));
            float rx = r.x + r.width * 0.78f;
            switch (g)
            {
                case Gest.TapHigh:
                    Fill(new Rect(r.x + r.width * 0.5f, midY, r.width * 0.5f - 2, 1), new Color(1f, 1f, 1f, 0.3f * a));
                    Dot(new Vector2(rx, r.y + r.height * 0.28f), red); break;
                case Gest.TapLow:
                    Fill(new Rect(r.x + r.width * 0.5f, midY, r.width * 0.5f - 2, 1), new Color(1f, 1f, 1f, 0.3f * a));
                    Dot(new Vector2(rx, r.y + r.height * 0.76f), red); break;
                case Gest.TapMan:
                    Fill(new Rect(rx - 2, r.y + r.height * 0.3f, 4, r.height * 0.45f), new Color(0.9f, 0.3f, 0.3f, 0.7f * a));
                    Dot(new Vector2(rx, r.y + r.height * 0.42f), red); break;
                case Gest.SwipeAt: Arrow(new Vector2(r.center.x + 6, midY), new Vector2(r.xMax - 10, midY), 3f, ink); break;
                case Gest.SwipeAway: Arrow(new Vector2(r.center.x - 6, midY), new Vector2(r.x + 10, midY), 3f, ink); break;
                case Gest.SwipeUp: Arrow(new Vector2(r.x + r.width * 0.68f, r.yMax - 8), new Vector2(r.x + r.width * 0.68f, r.y + 8), 3f, ink); break;
                case Gest.SwipeUpAt: Arrow(new Vector2(r.x + r.width * 0.6f, r.yMax - 8), new Vector2(r.xMax - 10, r.y + 8), 3f, ink); break;
                case Gest.Hold:
                {
                    var p = new Vector2(rx, midY);
                    var o = GUI.color;
                    float k = Mathf.Repeat(Time.unscaledTime, 1f);
                    GUI.color = new Color(1f, 1f, 1f, 0.6f * a * (1f - k));
                    float rr = 8f + 10f * k;
                    GUI.DrawTexture(new Rect(p.x - rr, p.y - rr, rr * 2f, rr * 2f), ring);
                    GUI.color = o;
                    Dot(p, red);
                    break;
                }
                case Gest.CrouchHold:
                    Arrow(new Vector2(rx, r.y + 8), new Vector2(rx, r.yMax - 14), 3f, ink);
                    Dot(new Vector2(rx, r.yMax - 12), red); break;
                case Gest.Creep:
                    Arrow(new Vector2(rx - 14, r.yMax - 12), new Vector2(rx + 6, r.yMax - 12), 3f, ink);
                    Dot(new Vector2(rx - 14, r.yMax - 12), red); break;
            }
        }

        void Dot(Vector2 p, Color c)
        {
            var o = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(p.x - 7, p.y - 7, 14, 14), circle);
            GUI.color = o;
        }

        /// <summary>The streak behind a swiping finger: thick at the finger, thinning and fading back to where it began.</summary>
        void Streak(List<Vector2> pts, float a)
        {
            int n = pts.Count;
            if (n < 2) return;
            for (int k = 1; k < n; k++)
            {
                float f = (float)k / (n - 1);
                var p0 = Gui(pts[k - 1]);
                var p1 = Gui(pts[k]);
                Line(p0, p1, 4f + 18f * f, new Color(1f, 1f, 1f, 0.12f + 0.4f * f * a));
                Line(p0, p1, 2f + 6f * f, new Color(0.9f, 0.12f, 0.1f, (0.2f + 0.7f * f) * a));
            }
        }

        /// <summary>The path of a swipe: a faint rail and chevrons marching along it, from start to end.</summary>
        void SwipePath(Vector2 aPx, Vector2 bPx, float alpha)
        {
            if (alpha <= 0.01f) return;
            var a = Gui(aPx);
            var b = Gui(bPx);
            var d = b - a;
            float len = d.magnitude;
            if (len < 10f) return;
            var u = d / len;
            var n = new Vector2(-u.y, u.x);
            Line(a, b, 3f, new Color(1f, 1f, 1f, 0.22f * alpha));
            int count = Mathf.Max(3, Mathf.RoundToInt(len / 46f));
            float phase = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f);
            for (int k = 0; k < count; k++)
            {
                float f = (k + phase) / count;
                var c = a + d * f;
                float bump = Mathf.Sin(f * Mathf.PI);                     // brightest mid-path, fading at both ends
                float sz = 13f + 6f * bump;
                var col = new Color(1f, 1f, 1f, alpha * (0.25f + 0.75f * bump));
                Line(c - u * sz * 0.7f + n * sz, c, 5f, col);
                Line(c - u * sz * 0.7f - n * sz, c, 5f, col);
            }
            // the head: a bigger arrow at the end
            var hcol = new Color(0.9f, 0.12f, 0.1f, alpha);
            Line(b - u * 26f + n * 20f, b, 6f, hcol);
            Line(b - u * 26f - n * 20f, b, 6f, hcol);
        }

        /// <summary>The finger: lifted (a soft disc), or pressing (solid, with a trail on a swipe).</summary>
        void DrawFinger(Vector2 px, bool down, float a, List<Vector2> trail, float ghost)
        {
            if (a <= 0.01f) return;
            var p = Gui(px);
            var o = GUI.color;
            if (down && trail != null)
                for (int i = 0; i < trail.Count; i++)
                {
                    var q = Gui(trail[i]);
                    float k = (i + 1f) / trail.Count;
                    Fill(new Rect(q.x - 4, q.y - 4, 8, 8), new Color(1f, 1f, 1f, 0.45f * a * k * ghost));
                }
            float s = down ? 1f : 1.18f;
            GUI.color = new Color(0f, 0f, 0f, 0.35f * a * ghost);
            GUI.DrawTexture(new Rect(p.x - 26 * s + 3, p.y - 26 * s + 4, 52 * s, 52 * s), disc);
            GUI.color = new Color(1f, 1f, 1f, (down ? 0.95f : 0.6f) * a * ghost);
            GUI.DrawTexture(new Rect(p.x - 24 * s, p.y - 24 * s, 48 * s, 48 * s), disc);
            GUI.color = new Color(0.85f, 0.08f, 0.08f, (down ? 1f : 0.5f) * a * ghost);
            GUI.DrawTexture(new Rect(p.x - 10, p.y - 10, 20, 20), circle);
            GUI.color = o;
        }

        void TrainingGUI(float w, float h)
        {
            if (!training) return;
            TrainingStyles();
            float now = Time.unscaledTime;
            var o = GUI.color;

            // the dip between marks
            if (!dipDone || now - dipAt < 0.35f)
            {
                float a = !dipDone ? Mathf.Clamp01(dipSince / 0.22f) : 1f - (now - dipAt) / 0.35f;
                Fill(new Rect(0, 0, w, h), new Color(0.01f, 0.01f, 0.015f, 0.92f * a));
            }

            if (ts == TS.Title)
            {
                float ta = now - groundAt;
                float a = Mathf.Min(Ease(0f, 0.4f, ta), 1f - Ease(2.6f, 3.2f, ta));
                tBig.normal.textColor = new Color(1f, 1f, 1f, 0.92f * a);
                GUI.Label(new Rect(0, h * 0.22f, w, 90), Spaced("INSTINCT"), tBig);
                Fill(new Rect(w * 0.5f - 110 * a, h * 0.22f + 88, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                tCap.normal.textColor = new Color(1f, 1f, 1f, 0.75f * a);
                GUI.Label(new Rect(w * 0.5f - 360, h * 0.22f + 100, 720, 30), "watch the move  ·  then do it", tCap);
                return;
            }
            if (ts == TS.Free) { FreePlayGUI(w, h); return; }
            if (li < 0 || li >= lessons.Length) return;
            var L = lessons[li];
            bool demo = ts == TS.Demo;
            float since = now - lessonAt;
            float slide = Ease(0f, 0.3f, since);

            // ---------------------------------------------------------------- top: the name, WATCH / YOUR TURN, the reps
            var head = new Rect(w * 0.5f - 230, 12 - (1f - slide) * 30f, 460, 50);
            GUI.color = new Color(1f, 1f, 1f, slide);
            Fill(head, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(head.x, head.y, 4, head.height), new Color(0.8f, 0.06f, 0.07f, 0.95f));
            tTag.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(head.x + 14, head.y, 60, head.height), (li + 1) + "/" + lessons.Length, tTag);
            tTitle.normal.textColor = Color.white;
            GUI.Label(head, Spaced(L.title), tTitle);
            var pill = new Rect(w * 0.5f - 90, head.yMax + 6, 180, 30);
            if (demo)
            {
                float bl = 0.6f + 0.4f * Mathf.Sin(now * 5f);
                Fill(pill, new Color(1f, 1f, 1f, 0.9f));
                tPill.normal.textColor = Color.black;
                GUI.Label(pill, Spaced("WATCH"), tPill);
                Fill(new Rect(pill.x + 16, pill.center.y - 5, 10, 10), new Color(0.85f, 0.08f, 0.08f, bl));
            }
            else
            {
                Fill(pill, new Color(0.75f, 0.06f, 0.07f, 0.95f));
                tPill.normal.textColor = Color.white;
                GUI.Label(pill, Spaced("YOUR TURN"), tPill);
                for (int i = 0; i < L.reps; i++)
                {
                    bool lit = i < lreps;
                    float pop = lit && i == lreps - 1 ? 1f + 0.6f * Mathf.Clamp01(1f - (now - repAt) / 0.3f) : 1f;
                    float s = 14f * pop;
                    GUI.color = lit ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                    GUI.DrawTexture(new Rect(pill.xMax + 12 + i * 22 - (s - 14f) * 0.5f, pill.center.y - s * 0.5f, s, s), circle);
                }
            }
            GUI.color = o;

            // ---------------------------------------------------------------- the buttons, top right
            float bx = w - 182, by = 12;
            if (!demo && TrainButton(new Rect(bx, by, 170, 40), "▶  " + Spaced("WATCH AGAIN"), false, now - lastMoveAt > 8f ? 1f : 0f)) { demoTries = 0; restageAt = -1f; StartDemo(); return; }
            if (demo && TrainButton(new Rect(bx, by, 170, 40), Spaced("TRY IT NOW"))) { StopDemo(); BeginTry(); return; }
            if (li > 0 && TrainButton(new Rect(bx, by + 48, 82, 40), "‹ PREV")) { StopDemo(); lessonDoneAt = -9f; StartLesson(li - 1); return; }
            if (TrainButton(new Rect(bx + 88, by + 48, 82, 40), "NEXT ›")) { StopDemo(); lessonDoneAt = -9f; if (li + 1 < lessons.Length) StartLesson(li + 1); else EnterFreePlay(false); return; }
            if (TrainButton(new Rect(bx, by + 96, 170, 40), Spaced("FREE PLAY"))) { EnterFreePlay(false); return; }

            // ---------------------------------------------------------------- the steps along the bottom
            int lit0 = demo ? demoStep : GhostStep();
            int n = L.steps.Length;
            float iw = 96f, ih = 58f, gap = 22f;
            float rowW = n * iw + (n - 1) * gap;
            float x0 = w * 0.5f - rowW * 0.5f, y0 = h - ih - 58f;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x0 + i * (iw + gap), y0, iw, ih);
                bool lit = lit0 == i || n == 1;
                StepIcon(r, L.steps[i].g, slide, lit);
                tCap.normal.textColor = new Color(1f, 1f, 1f, (lit ? 1f : 0.55f) * slide);
                GUI.Label(new Rect(r.x - 30, r.yMax + 4, r.width + 60, 44), L.steps[i].say, tCap);
                if (i < n - 1) Arrow(new Vector2(r.xMax + 4, r.center.y), new Vector2(r.xMax + gap - 4, r.center.y), 2f, new Color(1f, 1f, 1f, 0.4f * slide));
            }

            // ---------------------------------------------------------------- the finger
            if (demo)
            {
                float ha = hintOn ? Mathf.Clamp01((now - hintAt) / 0.2f) : 1f - Mathf.Clamp01((now - hintOffAt) / 0.4f);
                SwipePath(hintA, hintB, ha);
                for (int i = 0; i < 2; i++) if (fDown[i]) Streak(fTrail[i], 1f);
                for (int i = 0; i < 2; i++) DrawFinger(fPos[i], fDown[i], fShowNow[i], null, 1f);
                float rk = (now - rippleAt) / 0.4f;
                if (rk >= 0f && rk < 1f)
                {
                    var p = Gui(rippleAtPos);
                    float rr = 20f + 50f * rk;
                    GUI.color = new Color(1f, 1f, 1f, 0.8f * (1f - rk));
                    GUI.DrawTexture(new Rect(p.x - rr, p.y - rr, rr * 2f, rr * 2f), ring);
                    GUI.color = o;
                }
            }
            else if (rec.Count > 1 && (now - tryAt < 5f || now - lastMoveAt > 4f) && lessonDoneAt < 0f && dipDone)
                Ghost(now);

            // ---------------------------------------------------------------- done
            if (lessonDoneAt > 0f)
            {
                float dk = now - lessonDoneAt;
                float k = Mathf.Clamp01(dk / 0.18f);
                float s = Mathf.Lerp(1.6f, 1f, k);
                var m = GUI.matrix;
                var c = new Vector2(w * 0.5f, h * 0.3f);
                GUIUtility.ScaleAroundPivot(new Vector2(s, s), c);
                tBig.normal.textColor = new Color(0.9f, 0.1f, 0.1f, k * (1f - Ease(1.1f, 1.5f, dk)));
                GUI.Label(new Rect(0, c.y - 45, w, 90), Spaced("GOT IT"), tBig);
                GUI.matrix = m;
            }
        }

        // ---------------------------------------------------------------- the ghost (the demo, replayed faintly)

        Rec RecAt(float now)
        {
            float len = rec[rec.Count - 1].t + 0.8f;
            float t = Mathf.Repeat(now - tryAt, len);
            int i = 0;
            while (i < rec.Count - 1 && rec[i + 1].t <= t) i++;
            return rec[i];
        }

        int GhostStep() => rec.Count > 1 ? RecAt(Time.unscaledTime).step : -1;

        void Ghost(float now)
        {
            float len = rec[rec.Count - 1].t + 0.8f;
            float t = Mathf.Repeat(now - tryAt, len);
            int idx = 0;
            while (idx < rec.Count - 1 && rec[idx + 1].t <= t) idx++;
            var r = rec[idx];
            if (r.hint) SwipePath(r.ha, r.hb, 0.75f);
            if (r.d0)
            {
                // the streak: back through the recording while the finger was down
                var pts = new List<Vector2>();
                int k = idx;
                while (k > 0 && rec[k - 1].d0 && idx - k < 40) k--;
                for (; k <= idx; k++) pts.Add(rec[k].p0);
                if (pts.Count > 1 && (pts[pts.Count - 1] - pts[0]).sqrMagnitude > 100f) Streak(pts, 0.7f);
            }
            DrawFinger(r.p0, r.d0, r.s0, null, 0.6f);
            DrawFinger(r.p1, r.d1, r.s1, null, 0.6f);
        }

        // ---------------------------------------------------------------- free play

        void FreePlayGUI(float w, float h)
        {
            float now = Time.unscaledTime;
            float a = Ease(0f, 0.4f, now - lessonAt);
            float cw = Mathf.Min(560f, w - 400f);
            var head = new Rect(w * 0.5f - cw * 0.5f, 12, cw, 60);
            Fill(head, new Color(0f, 0f, 0f, 0.6f * a));
            Fill(new Rect(head.x, head.y, 4, head.height), new Color(0.8f, 0.06f, 0.07f, 0.95f * a));
            tTitle.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(head.x, head.y + 2, head.width, 36), Spaced("FREE PLAY"), tTitle);
            tCap.normal.textColor = new Color(1f, 1f, 1f, 0.6f * a);
            int got = 0;
            foreach (var b in learned) if (b) got++;
            GUI.Label(new Rect(head.x, head.y + 38, head.width, 22), got + " / " + lessons.Length + " moves  ·  they keep getting up", tCap);
            float bw = Mathf.Min(260f, cw * 0.5f - 6), by = head.yMax + 8;
            if (TrainButton(new Rect(w * 0.5f + 6, by, bw, 44), Spaced("CONTINUE STORY") + "  ›", true)) { EndTraining(); return; }
            if (TrainButton(new Rect(w * 0.5f - 6 - bw, by, bw, 44), Spaced("LESSONS")))
            {
                int first = Array.IndexOf(learned, false);
                StartLesson(first < 0 ? 0 : first);
                return;
            }

            // the checklist: lit when landed; a row tapped plays its lesson (watch, then your turn)
            float rw = Mathf.Min(240f, w * 0.24f), rh = Mathf.Min(24f, (h - 150f) / (lessons.Length + 1));
            var tog = new Rect(w - rw - 12, 12, rw, 30);
            if (TrainButton(tog, (listOpen ? "▾  " : "▸  ") + Spaced("MOVES"))) listOpen = !listOpen;
            if (!listOpen) return;
            float y = tog.yMax + 4;
            Fill(new Rect(tog.x, y, rw, rh * lessons.Length + 8), new Color(0f, 0f, 0f, 0.5f));
            y += 4;
            for (int i = 0; i < lessons.Length; i++)
            {
                var row = new Rect(tog.x, y, rw, rh);
                float fl = i == flashRow ? Mathf.Clamp01(1f - (now - flashRowAt) / 0.8f) : 0f;
                if (fl > 0f) Fill(row, new Color(0.8f, 0.06f, 0.07f, 0.55f * fl));
                Fill(new Rect(row.x + 8, row.y + rh * 0.5f - 5, 10, 10), learned[i] ? new Color(0.85f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.25f));
                tRow.normal.textColor = learned[i] ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(new Rect(row.x + 26, row.y, rw - 54, rh), lessons[i].title, tRow);
                tRow.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
                GUI.Label(new Rect(row.xMax - 24, row.y, 20, rh), "▶", tRow);
                if (Event.current.type == EventType.Repaint) GestureInput.BlockGui(row, scale);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { StartLesson(i, true); return; }
                y += rh;
            }
        }
    }
}
