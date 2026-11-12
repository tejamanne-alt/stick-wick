using System;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE TRAINING GROUND (chapter 1, the three men at the shutter): INSTINCT, played rather than directed.
    /// He is on his feet, three men in front of him, and his body remembers what his head does not.
    /// Every bare-handed move is a lesson, taught one at a time on a card at the top of the screen (the move's
    /// name, the gesture in plain words, a ghost finger making it at the right place, dots for the reps), and
    /// the men are sparring partners: they fight back only when the lesson needs it, they go down and get up
    /// again, nobody can lose. When a lesson needs the men somewhere (a man far off to rush, one behind him
    /// for the crossfire, one with his back turned for the choke) the picture dips for a breath and they are
    /// on their marks.
    ///
    /// After the last lesson (or at FREE PLAY, any time) it is a playground: all three come at him, every move
    /// he lands lights its row on the checklist, a row tapped replays that lesson, and CONTINUE THE STORY makes
    /// it real: the knife comes out of his belt and the men fight to finish it (then the memory, as before).
    /// A retry after a defeat in that real fight skips the ground and goes straight back to it.
    ///
    /// Moves are recognised from the hero's own reports (HeroController.Did / Landed) and the style labels
    /// (WaveDirector.Styled), so a lesson ticks when the real move happens, whatever led to it.</summary>
    public partial class StoryIntro
    {
        /// <summary>The training ground has been played (and left) in this run: a retry goes straight to the fight.</summary>
        public static bool TrainedThisRun;

        enum Glyph { None, Hold, TapHigh, TapLow, TapMan, SwipeAt, SwipeAway, SwipeUp, CrouchHold, Tap }

        class Lesson
        {
            public string group, title, how, tip;
            public Glyph glyph;
            public int reps = 1, who = -1;          // who: the man the ghost points at (0..2), -1 the nearest
            public bool restage;                     // put them back on their marks after every rep
            public Action stage;                     // where the men stand, who swings (null: as they are)
            public Func<char, string, string, bool, bool> match;   // (kind, what, label, low tap)
        }

        Lesson[] lessons;
        bool[] learned;
        int li = -1, lreps;
        bool training, freePlay, practiseOne;
        float lessonAt, repAt = -9f, lessonDoneAt = -9f, lastMoveAt, groundAt, flashRowAt = -9f;
        int flashRow = -1;
        float trainCx;
        float dipAt = -9f, dipSince; Action dipThen; bool dipDone = true;
        float runFor, chargeAt, unawareLostAt = -1f, slipAt = -9f, bottleCheckAt;
        bool wasAir;
        Enemy lastLock;
        bool listOpen = true;
        GUIStyle tTag, tTitle, tHow, tTip, tBtn, tRow, tBig;

        const string PrefMask = "training_mask", PrefSeen = "training_seen";

        // ================================================================ start and end

        void BeginTraining()
        {
            phase = Phase.Training;
            training = true;
            freePlay = practiseOne = false;
            groundAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            StoryCameraFX.Set(0f, 0f);
            if (cam != null) { cam.ScriptRoll = 0f; cam.Scripted(Vector3.zero, Vector3.zero, 0f); }
            barsPx = 0f;
            if (TimeController.Instance != null) TimeController.Instance.CineScale = 1f;
            // bare hands: the knife stays in his belt until it is real
            hero.SetStyle(FightStyle.Fists);
            hero.CineRelease(hero.transform.position);
            hero.runSpeed = runSpeed;
            HeroController.Sparring = true;
            float x0 = hero.arenaMinX, x1 = hero.arenaMaxX;
            trainCx = Mathf.Clamp(hero.transform.position.x, x0 + 7.5f, x1 - 7.5f);
            if (x1 - x0 < 15f) trainCx = (x0 + x1) * 0.5f;
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
            HeroController.Did += TrainDid;
            HeroController.Landed += TrainLanded;
            WaveDirector.Styled += TrainStyled;
            Sound.Tension(false);
            Sound.Play(Sfx.Riser, 0.5f);
            Say("THUG", "Easy... boss wants him breathing. Don't break him.", 2.2f);
            // seen it before: straight into the playground (the lessons are a tap away)
            if (PlayerPrefs.GetInt(PrefSeen, 0) == 1) EnterFreePlay(false);
            else li = -1;                                    // the first lesson starts after the title
        }

        void TrainingUnhook()
        {
            HeroController.Did -= TrainDid;
            HeroController.Landed -= TrainLanded;
            WaveDirector.Styled -= TrainStyled;
            HeroController.Sparring = false;
        }

        /// <summary>CONTINUE THE STORY: it is real now.</summary>
        void EndTraining()
        {
            if (!training) return;
            training = false;
            TrainedThisRun = true;
            PlayerPrefs.SetInt(PrefSeen, 1);
            PlayerPrefs.Save();
            TrainingUnhook();
            foreach (var e in men)
                if (e != null)
                {
                    e.Sparring = false;
                    e.HoldFire = false;
                    e.Keep = false;
                    e.HealFull();
                }
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

        // ================================================================ the lessons

        Enemy Man(int i) => men != null && i >= 0 && i < men.Length ? men[i] : null;

        /// <summary>A man on his mark: on his feet, healed, standing or coming, swinging or not.</summary>
        void Mark(int i, float dx, bool keep, bool swings)
        {
            var e = Man(i);
            if (e == null) return;
            float x = Mathf.Clamp(trainCx + dx, hero.arenaMinX + 0.6f, hero.arenaMaxX - 0.6f);
            e.ResetTo(new Vector3(x, home.y, 0f), hero.transform.position.x);
            e.Keep = keep;
            e.HoldFire = !swings;
            e.Sparring = true;
        }

        void HeroAt(float dx, int face)
        {
            float x = Mathf.Clamp(trainCx + dx, hero.arenaMinX + 0.6f, hero.arenaMaxX - 0.6f);
            hero.CinePose(new Vector3(x, home.y, 0f));
            hero.SetFacing(face);
            hero.CineRelease();
        }

        /// <summary>No repositioning: who stands, who swings.</summary>
        void Men(bool keep, int swinger = -1)
        {
            for (int i = 0; i < men.Length; i++)
                if (men[i] != null) { men[i].Keep = keep; men[i].HoldFire = i != swinger; }
        }

        static bool IsBody(string clip) => clip == "liver_hook" || clip == "body_cross";
        static readonly string[] OwnStrings = { "DUTCH TWO-AND-LOW", "ONE-TWO-LIVER", "UP-DOWN-UP", "LIVER AND LIGHTS", "DIG AND LIFT", "CLOSE QUARTERS", "KARATE", "PALM HEEL", "FOUR-PIECE", "ONE-TWO-KICK" };
        static bool Named(string label, string name) => label != null && label.StartsWith(name);

        void BuildLessons()
        {
            lessons = new[]
            {
                // ---------------------------------------------------------------- basics
                new Lesson { group = "BASICS", title = "WALK AND RUN", glyph = Glyph.Hold,
                    how = "HOLD a finger on the side you want to go. Keep holding: he runs.",
                    tip = "Let go to stop.",
                    stage = () => Men(true),
                    match = (k, a, l, low) => k == 't' && a == "run" },
                new Lesson { group = "BASICS", title = "JUMP", glyph = Glyph.SwipeUp, reps = 2,
                    how = "SWIPE UP to jump. Up-left or up-right: jump that way.",
                    tip = "Over drums, up onto ledges, out of a sweep.",
                    stage = () => Men(true),
                    match = (k, a, l, low) => k == 't' && a == "jump" },
                new Lesson { group = "BASICS", title = "PICK YOUR MAN", glyph = Glyph.TapMan, who = 0,
                    how = "TAP a man to target him. Your blows go to him; the others hang back.",
                    tip = "Tap him again, or another, to switch.",
                    stage = () => Men(true),
                    match = (k, a, l, low) => k == 't' && a == "lock" },
                // ---------------------------------------------------------------- strikes
                new Lesson { group = "STRIKES", title = "PUNCH", glyph = Glyph.TapHigh, reps = 3, who = 0,
                    how = "TAP HIGH on his side of the screen (above the faint line).",
                    tip = "Too far? Tap anyway: he steps in to hit.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => k == 'l' && !low },
                new Lesson { group = "STRIKES", title = "KICK", glyph = Glyph.TapLow, reps = 3, who = 0,
                    how = "TAP LOW (below the line) from a step away: kicks.",
                    tip = "Two calf kicks in a row take his leg.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => k == 'l' && low && !IsBody(a) },
                new Lesson { group = "STRIKES", title = "TO THE BODY", glyph = Glyph.TapLow, reps = 2, who = 0,
                    how = "Step in chest to chest, then TAP LOW: hooks to the ribs.",
                    tip = "Up close the same taps throw elbows and hooks.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => k == 'l' && IsBody(a) },
                // ---------------------------------------------------------------- strings
                new Lesson { group = "STRINGS", title = "BOXER'S FOUR", glyph = Glyph.TapHigh, who = 0,
                    how = "TAP HIGH four times, in rhythm: jab, cross, hook, uppercut.",
                    tip = "A named string hits harder on its last blow.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && Named(l, "BOXER'S FOUR") },
                new Lesson { group = "STRINGS", title = "KICK STRING", glyph = Glyph.TapLow, who = 0,
                    how = "From a step away, TAP LOW three times: calf, body, head.",
                    tip = "Keep your distance or it turns into body shots.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && Named(l, "KICK STRING") },
                new Lesson { group = "STRINGS", title = "YOUR OWN STRING", glyph = Glyph.TapHigh, who = 0,
                    how = "Mix them: HIGH HIGH LOW · HIGH LOW HIGH · LOW LOW HIGH · LOW HIGH HIGH...",
                    tip = "Every string has a name. Find one.",
                    stage = () => Men(false),
                    match = (k, a, l, low) => (k == 'l' || k == 's') && l != null && Array.Exists(OwnStrings, n => l.StartsWith(n)) },
                // ---------------------------------------------------------------- defence
                new Lesson { group = "READ HIM", title = "UNDER THE GUARD", glyph = Glyph.TapLow, reps = 2, who = 0,
                    how = "His hands are UP (the word over him says so): go LOW, under them.",
                    tip = "Hands low: go high. Folded over: the knee. Watch the word.",
                    stage = () => { Men(false); var a = Man(0); if (a != null) a.ForceGuard(1, 4f); },
                    match = (k, a, l, low) => k == 'd' && a == "read" },
                new Lesson { group = "DEFENCE", title = "PARRY", glyph = Glyph.Tap, reps = 2, who = 0,
                    how = "He winds up and swings: TAP toward him as it comes. His fist goes wide.",
                    tip = "Tap the moment his arm moves. A parried man is open.",
                    stage = () => { HeroAt(0f, 1); Mark(0, 1.4f, false, true); Mark(1, 5.5f, true, false); Mark(2, -5.5f, true, false); },
                    match = (k, a, l, low) => k == 'd' && a == "parry" },
                new Lesson { group = "DEFENCE", title = "SLIP AND COUNTER", glyph = Glyph.SwipeAway, who = 0,
                    how = "As he swings, SWIPE AWAY from him. He falls in after it: hit him.",
                    tip = "A swipe AT a swinging man slides under it instead.",
                    stage = () => { HeroAt(0f, 1); Mark(0, 1.4f, false, true); Mark(1, 5.5f, true, false); Mark(2, -5.5f, true, false); },
                    match = (k, a, l, low) => k == 't' && a == "slip" },
                // ---------------------------------------------------------------- grappling
                new Lesson { group = "CLINCH", title = "GRAB HIM", glyph = Glyph.SwipeAt, who = 0,
                    how = "Right next to him: SWIPE AT him. Hands behind his neck.",
                    tip = "Walking into him takes hold too.",
                    stage = () => { HeroAt(0f, 1); Mark(0, 0.9f, true, false); Mark(1, 5.5f, true, false); Mark(2, -5.5f, true, false); },
                    match = (k, a, l, low) => k == 'd' && a == "clinch" },
                new Lesson { group = "CLINCH", title = "THROW HIM", glyph = Glyph.SwipeAt, reps = 2, who = 0,
                    how = "Holding him: SWIPE AT him hip throw · AWAY shoulder throw · UP suplex · DOWN DDT.",
                    tip = "TAP HIGH: knees · TAP LOW: the leg sweep. Knees first, then the throw.",
                    stage = () => { HeroAt(0f, 1); Mark(0, 0.9f, true, false); Mark(1, 5.5f, true, false); Mark(2, -5.5f, true, false); },
                    restage = true,
                    match = (k, a, l, low) => k == 's' && l != null && (l.Contains("THROW") || l.Contains("SWEEP") || l.Contains("SUPLEX") || l.Contains("DDT") || l.Contains("SLAM")) },
                // ---------------------------------------------------------------- from far
                new Lesson { group = "FROM FAR", title = "RUSH IN", glyph = Glyph.SwipeAt, who = 1,
                    how = "A man 3 to 8 metres off: SWIPE TOWARD him. You close the gap with a blow.",
                    tip = "Keep swiping: the rush chains into the next man.",
                    stage = () => { HeroAt(-3f, 1); Mark(1, 2.6f, true, false); Mark(0, -6.5f, true, false); Mark(2, -7.5f, true, false); },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && a == "far_rush" },
                new Lesson { group = "FROM FAR", title = "RUNNING ATTACKS", glyph = Glyph.Hold, reps = 2, who = 1,
                    how = "HOLD to run at him. Two steps out: TAP HIGH flying knee · TAP LOW slide tackle.",
                    tip = "Run flat out into him without tapping: the shoulder barge.",
                    stage = () => { HeroAt(-3.5f, 1); Mark(1, 3.5f, true, false); Mark(0, -6.5f, true, false); Mark(2, -7.5f, true, false); },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && (a == "far_knee" || a == "far_slide" || a == "far_barge") },
                new Lesson { group = "FROM FAR", title = "SIDESTEP THE BULL", glyph = Glyph.SwipeAway, who = 1,
                    how = "He charges head down: SWIPE as he comes. Off the line, a hammer as he passes.",
                    tip = "Wait for it: swipe when he is a few steps away.",
                    stage = () => { HeroAt(-2f, 1); Mark(1, 3.8f, true, false); Mark(0, -6.5f, true, false); Mark(2, -7.5f, true, false); chargeAt = Time.time + 1.6f; },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && a == "sidestep" },
                // ---------------------------------------------------------------- tricks
                new Lesson { group = "TWO ON YOU", title = "CROSSFIRE", glyph = Glyph.Tap,
                    how = "One man in front, one behind, both close: TAP. They hit each other.",
                    tip = "Don't run from two men. Let them close in.",
                    stage = () => { HeroAt(0f, 1); Mark(0, 1.0f, true, false); Mark(1, -1.0f, true, false); Mark(2, 6.5f, true, false); },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && a == "crossfire" },
                new Lesson { group = "STEALTH", title = "SILENT TAKEDOWN", glyph = Glyph.CrouchHold, who = 2,
                    how = "SWIPE DOWN and HOLD to crouch. Creep up behind him. TAP his back: the choke.",
                    tip = "Upright, he hears you. Stay low.",
                    stage = () => { HeroAt(-2f, 1); Mark(0, -6.5f, true, false); Mark(1, -7.5f, true, false); PostAway(2, 2.6f); },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && (a == "takedown" || a == "far_takedown") },
                new Lesson { group = "PROPS", title = "THROW SOMETHING", glyph = Glyph.TapMan, who = 1,
                    how = "Walk over the bottle to pick it up. Then TAP a man: it flies at him.",
                    tip = "Every room has things to throw: bottles, bricks, stones.",
                    stage = () => { HeroAt(-2.5f, 1); Mark(1, 3.5f, true, false); Mark(0, 5f, true, false); Mark(2, -7f, true, false); Bottle(-1.2f); },
                    restage = true,
                    match = (k, a, l, low) => k == 'd' && (a == "far_throw" || a == "far_smash") },
            };
        }

        /// <summary>A man with his back turned, not knowing (the choke lesson).</summary>
        void PostAway(int i, float dx)
        {
            var e = Man(i);
            if (e == null) return;
            float x = Mathf.Clamp(trainCx + dx, hero.arenaMinX + 0.6f, hero.arenaMaxX - 0.6f);
            e.HealFull();
            e.Sparring = true;
            e.Keep = true;
            e.HoldFire = true;
            e.PostUnaware(new Vector3(x, home.y, 0f), 1);
            unawareLostAt = -1f;
        }

        void Bottle(float dx)
        {
            var at = new Vector3(Mathf.Clamp(trainCx + dx, hero.arenaMinX + 0.6f, hero.arenaMaxX - 0.6f), home.y, 0f);
            if (Throwable.Near(at, 1.5f) == null) Throwable.Spawn(PropKind.Bottle, at);
        }

        // ================================================================ the flow

        void StartLesson(int i, bool one = false)
        {
            if (lessons == null || i < 0 || i >= lessons.Length) return;
            li = i;
            lreps = 0;
            practiseOne = one;
            freePlay = false;
            lessonAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            lessonDoneAt = -9f;
            Sound.Play(Sfx.Glint, 0.8f);
            Stage();
        }

        void Stage()
        {
            var L = lessons[li];
            if (L.stage == null) return;
            // a lesson that only says who stands and who swings needs no dip; one with marks does
            bool marks = L.group == "DEFENCE" || L.group == "CLINCH" || L.group == "FROM FAR" || L.group == "TWO ON YOU" || L.group == "STEALTH" || L.group == "PROPS";
            if (!marks) { foreach (var e in men) if (e != null) e.HealFull(); L.stage(); return; }
            Dip(L.stage);
        }

        /// <summary>The picture dips for a breath; in the dark they are on their marks.</summary>
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
            dipSince += Time.unscaledDeltaTime;
            // wait for him to be free (out of a throw, a choke, the air), but not for ever
            bool busy = !hero.Free;
            foreach (var e in men) if (e != null && e.Held) busy = true;
            if (dipSince >= 0.22f && (!busy || dipSince > 2.5f))
            {
                hero.CineLock();
                foreach (var e in men) if (e != null) e.HealFull();
                hero.HealFull();
                dipThen?.Invoke();
                dipDone = true;
                dipAt = Time.unscaledTime;                  // the fade back up starts now
                Sound.Play(Sfx.Whoosh, 0.5f);
            }
        }

        void Rep()
        {
            lreps++;
            repAt = Time.unscaledTime;
            lastMoveAt = Time.unscaledTime;
            var L = lessons[li];
            if (lreps >= L.reps)
            {
                lessonDoneAt = Time.unscaledTime;
                MarkLearned(li);
                Sound.Play(Sfx.RankUp, 0.9f);
            }
            else
            {
                Sound.Play(Sfx.Glint, 1f);
                if (L.restage) restageAt = Time.unscaledTime + 1.1f;
            }
        }
        float restageAt = -1f;

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
            freePlay = true;
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
            if (label != null && label.Contains("OVERREACH")) Fire('t', "slip", null, false);
            Fire('s', label, label, false);
        }

        void Fire(char k, string a, string label, bool low)
        {
            if (lessons == null) return;
            if (freePlay)
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
            if (li < 0 || li >= lessons.Length || lessonDoneAt > 0f || !dipDone) return;
            if (lessons[li].match(k, a, label, low)) Rep();
        }

        // ================================================================ every frame

        void UpdateTraining()
        {
            if (!training) return;
            float now = Time.unscaledTime;
            UpdateDip();
            // the title, then the first lesson
            if (!freePlay && li < 0 && now - groundAt > 3.2f) StartLesson(0);
            // a finished lesson: a beat to enjoy it, then the next card
            if (lessonDoneAt > 0f && now - lessonDoneAt > 1.5f) { lessonDoneAt = -9f; NextLesson(); }
            if (restageAt > 0f && now > restageAt && li >= 0) { restageAt = -1f; Stage(); }

            // sparring: nobody stays hurt for long, nobody dies
            foreach (var e in men)
                if (e != null && e.Alive && !e.Held && !e.IsDown && e.HealthFraction < 0.45f) e.HealFull();
            if (hero.Health < hero.maxHealth * 0.4f && hero.Free) hero.HealFull();

            // the moves read off his body
            if (hero.RunningFast) runFor += Time.deltaTime; else runFor = 0f;
            if (runFor > 0.35f) { runFor = -99f; Fire('t', "run", null, false); }
            bool air = !hero.Grounded && !hero.OnLedge;
            if (air && !wasAir) Fire('t', "jump", null, false);
            wasAir = air;
            var lk = hero.Locked;
            if (lk != null && lk != lastLock) Fire('t', "lock", null, false);
            lastLock = lk;

            // the lessons' own staging, kept up
            var L = !freePlay && li >= 0 ? lessons[li] : null;
            if (L != null && dipDone && lessonDoneAt < 0f)
            {
                if (L.title == "UNDER THE GUARD") { var a = Man(0); if (a != null && a.Alive) a.ForceGuard(1, 1.5f); }
                if (L.title == "SIDESTEP THE BULL")
                {
                    var b = Man(1);
                    if (b != null && b.Alive && !b.Charging && !b.IsDown && Time.time > chargeAt)
                    {
                        float gap = Mathf.Abs(b.transform.position.x - hero.transform.position.x);
                        if (gap > 3f && gap < 9f) b.ChargeAt();
                        chargeAt = Time.time + 3.6f;
                    }
                }
                if (L.title == "SILENT TAKEDOWN")
                {
                    var c = Man(2);
                    if (c != null && !c.Unaware && !c.Held)
                    {
                        if (unawareLostAt < 0f) { unawareLostAt = now; Say("THUG", "Hey! I can hear you!", 1.2f); }
                        else if (now - unawareLostAt > 1.6f) Stage();
                    }
                }
                if (L.title == "THROW SOMETHING" && now > bottleCheckAt)
                {
                    bottleCheckAt = now + 1.5f;
                    if (hero.Carried == null && Throwable.Near(hero.transform.position, 30f) == null) Bottle(-1.2f);
                }
            }
            if (freePlay && now > bottleCheckAt)
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
            tTitle = new GUIStyle(big) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
            tHow = new GUIStyle(prompt) { fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            tTip = new GUIStyle(cap) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            tBtn = new GUIStyle(btn) { fontSize = 17 };
            tRow = new GUIStyle(cap) { fontSize = 15, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            tBig = new GUIStyle(big) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
        }

        bool TrainButton(Rect r, string label, bool strong = false)
        {
            Fill(r, strong ? new Color(0.62f, 0.05f, 0.06f, 0.92f) : new Color(0f, 0f, 0f, 0.62f));
            Fill(new Rect(r.x, r.y, r.width, 1), new Color(1f, 1f, 1f, strong ? 0.6f : 0.35f));
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(1f, 1f, 1f, strong ? 0.6f : 0.35f));
            tBtn.normal.textColor = Color.white;
            if (Event.current.type == EventType.Repaint) GestureInput.Blocked.Add(new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale));
            return GUI.Button(r, label, tBtn);
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

            // the title: INSTINCT, his body remembers
            float ta = now - groundAt;
            if (ta < 3.2f && !freePlay)
            {
                float a = Mathf.Min(Ease(0f, 0.4f, ta), 1f - Ease(2.6f, 3.2f, ta));
                tBig.normal.textColor = new Color(1f, 1f, 1f, 0.92f * a);
                GUI.Label(new Rect(0, h * 0.22f, w, 90), Spaced("INSTINCT"), tBig);
                Fill(new Rect(w * 0.5f - 110 * a, h * 0.22f + 88, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                tHow.normal.textColor = new Color(1f, 1f, 1f, 0.85f * a);
                GUI.Label(new Rect(w * 0.5f - 360, h * 0.22f + 100, 720, 34), "His body remembers what his head forgot.", tHow);
                tTip.normal.textColor = new Color(1f, 1f, 1f, 0.6f * a);
                GUI.Label(new Rect(w * 0.5f - 360, h * 0.22f + 136, 720, 30), "THE TRAINING GROUND  ·  every bare-handed move, one at a time  ·  nobody can lose here", tTip);
                return;
            }

            if (freePlay) { FreePlayGUI(w, h); return; }
            if (li < 0 || li >= lessons.Length) return;
            var L = lessons[li];

            // ---------------------------------------------------------------- the card
            float since = now - lessonAt;
            float slide = Ease(0f, 0.3f, since);
            bool doneNow = lessonDoneAt > 0f;
            float dk = doneNow ? now - lessonDoneAt : 0f;
            float cw = Mathf.Min(720f, w - 380f), ch = 150f;
            var card = new Rect(w * 0.5f - cw * 0.5f, 14f - (1f - slide) * 40f, cw, ch);
            GUI.color = new Color(1f, 1f, 1f, slide);
            Fill(card, new Color(0f, 0f, 0f, 0.66f));
            Fill(new Rect(card.x, card.y, 4, card.height), new Color(0.8f, 0.06f, 0.07f, 0.95f));
            // the progress through all the lessons, along the bottom of the card
            float prog = (li + (doneNow ? 1f : 0f)) / lessons.Length;
            Fill(new Rect(card.x, card.yMax - 3, card.width, 3), new Color(1f, 1f, 1f, 0.12f));
            Fill(new Rect(card.x, card.yMax - 3, card.width * prog, 3), new Color(1f, 1f, 1f, 0.75f));

            tTag.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(card.x + 18, card.y + 6, 400, 22), Spaced(L.group) + "     " + (li + 1) + " / " + lessons.Length, tTag);
            // the reps: dots, top right of the card; a fresh one pops
            for (int i = 0; i < L.reps; i++)
            {
                bool lit = i < lreps;
                float pop = lit && i == lreps - 1 ? 1f + 0.6f * Mathf.Clamp01(1f - (now - repAt) / 0.3f) : 1f;
                float s = 12f * pop;
                var dot = new Rect(card.xMax - 24 - (L.reps - 1 - i) * 22 - s * 0.5f, card.y + 16 - s * 0.5f, s, s);
                GUI.color = lit ? Color.white : new Color(1f, 1f, 1f, 0.28f);
                GUI.DrawTexture(dot, circle);
            }
            GUI.color = new Color(1f, 1f, 1f, slide);
            tTitle.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x, card.y + 22, card.width, 50), Spaced(L.title), tTitle);
            tHow.normal.textColor = new Color(1f, 1f, 1f, 0.95f);
            GUI.Label(new Rect(card.x + 20, card.y + 70, card.width - 40, 50), L.how, tHow);
            tTip.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            var key = TrainKey(L.glyph);
            GUI.Label(new Rect(card.x + 20, card.y + 118, card.width - 40, 26), key != null ? L.tip + "      " + key : L.tip, tTip);

            // done: a red stamp over the card
            if (doneNow)
            {
                float k = Mathf.Clamp01(dk / 0.18f);
                float s = Mathf.Lerp(1.6f, 1f, k);
                Fill(card, new Color(1f, 1f, 1f, 0.12f * (1f - Mathf.Clamp01(dk / 0.5f))));
                var m = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(s, s), card.center);
                tBig.normal.textColor = new Color(0.9f, 0.1f, 0.1f, k);
                GUI.Label(new Rect(card.x, card.y + 30, card.width, 90), Spaced("GOT IT"), tBig);
                GUI.matrix = m;
            }
            GUI.color = o;

            // ---------------------------------------------------------------- the buttons, under the card
            // beside the card, clear of the banners the fight writes across the middle
            float bx = card.xMax + 12, bw = Mathf.Min(160f, w - bx - 12);
            if (TrainButton(new Rect(bx, card.y, bw, 40), Spaced("SKIP") + "  ›")) { lessonDoneAt = -9f; restageAt = -1f; NextLesson(); }
            if (TrainButton(new Rect(bx, card.y + 48, bw, 40), practiseOne ? Spaced("BACK") : Spaced("FREE PLAY"))) EnterFreePlay(false);

            // ---------------------------------------------------------------- the ghost finger, when he's stuck
            bool idle = since < 4.5f || now - lastMoveAt > 5f;
            if (!doneNow && dipDone && idle && L.glyph != Glyph.None) Ghost(L, w, h, Mathf.Min(1f, Ease(0.3f, 0.7f, since)) * 0.85f);
        }

        string TrainKey(Glyph g)
        {
            if (Application.isMobilePlatform) return null;
            switch (g)
            {
                case Glyph.Hold: return "keys: hold  ←  →";
                case Glyph.TapHigh: return "keys:  A  /  D";
                case Glyph.TapLow: return "keys:  Z  /  X";
                case Glyph.TapMan: return "keys:  T";
                case Glyph.SwipeAt: case Glyph.SwipeAway: return "keys:  Q  /  E";
                case Glyph.SwipeUp: return "keys:  W";
                case Glyph.CrouchHold: return "keys:  S , then hold  ←  →";
                case Glyph.Tap: return "keys:  A  /  D";
            }
            return null;
        }

        Enemy GhostMan(Lesson L)
        {
            var e = Man(L.who);
            if (e != null && e.Alive) return e;
            Enemy best = null;
            float bd = 99f;
            foreach (var m0 in men)
            {
                if (m0 == null || !m0.Alive) continue;
                float d = Mathf.Abs(m0.transform.position.x - hero.transform.position.x);
                if (d < bd) { bd = d; best = m0; }
            }
            return best;
        }

        /// <summary>The gesture, made by a ghost finger at the place on the screen the game reads it.</summary>
        void Ghost(Lesson L, float w, float h, float fade)
        {
            var t = GhostMan(L);
            var hp = ScreenOf(hero.Chest);
            var tp = t != null ? ScreenOf(t.Chest) : new Vector2(w * 0.75f, h * 0.45f);
            int side = t != null && t.transform.position.x < hero.transform.position.x ? -1 : 1;
            var gi = GestureInput.Instance;
            float line = h * (gi != null ? 1f - gi.kickLine : 0.55f);
            float sx = side > 0 ? Mathf.Clamp(tp.x, w * 0.6f, w - 80f) : Mathf.Clamp(tp.x, 80f, w * 0.4f);
            float cyc = Mathf.Repeat(Time.unscaledTime - lessonAt, 1.4f) / 1.4f;
            switch (L.glyph)
            {
                case Glyph.TapHigh: case Glyph.Tap: GhostTap(new Vector2(sx, line * 0.55f), cyc, fade); GhostLine(side, line, w, fade); break;
                case Glyph.TapLow: GhostTap(new Vector2(sx, line + (h - line) * 0.5f), cyc, fade); GhostLine(side, line, w, fade); break;
                case Glyph.TapMan: GhostTap(tp, cyc, fade); break;
                case Glyph.SwipeAt: GhostSwipe(new Vector2(hp.x + side * 40f, h * 0.5f), new Vector2(hp.x + side * w * 0.3f, h * 0.5f), cyc, fade); break;
                case Glyph.SwipeAway: GhostSwipe(new Vector2(hp.x - side * 40f, h * 0.5f), new Vector2(hp.x - side * w * 0.3f, h * 0.5f), cyc, fade); break;
                case Glyph.SwipeUp: GhostSwipe(new Vector2(w * 0.62f, h * 0.7f), new Vector2(w * 0.62f, h * 0.3f), cyc, fade); break;
                case Glyph.Hold: GhostHold(new Vector2(side > 0 ? w * 0.82f : w * 0.18f, h * 0.62f), side, cyc, fade); break;
                case Glyph.CrouchHold:
                    if (hero.Crouching) GhostHold(new Vector2(side > 0 ? w * 0.82f : w * 0.18f, h * 0.7f), side, cyc, fade);
                    else GhostSwipe(new Vector2(w * 0.62f, h * 0.35f), new Vector2(w * 0.62f, h * 0.72f), cyc, fade);
                    break;
            }
        }

        void GhostLine(int side, float line, float w, float fade)
        {
            Fill(new Rect(side > 0 ? w * 0.5f : 0f, line, w * 0.5f, 1), new Color(1f, 1f, 1f, 0.2f * fade));
            tTag.normal.textColor = new Color(1f, 1f, 1f, 0.35f * fade);
            GUI.Label(new Rect(side > 0 ? w - 150 : 16, line - 22, 140, 20), "HIGH  ▲", tTag);
            GUI.Label(new Rect(side > 0 ? w - 150 : 16, line + 2, 140, 20), "LOW  ▼", tTag);
        }

        void GhostTap(Vector2 spot, float cyc, float fade)
        {
            var o = GUI.color;
            float tap = cyc < 0.18f ? cyc / 0.18f : 1f;
            float k = Mathf.Clamp01((cyc - 0.15f) / 0.5f);
            float r = 18f + 46f * k;
            GUI.color = new Color(1f, 1f, 1f, 0.7f * fade * (1f - k));
            if (cyc > 0.15f) GUI.DrawTexture(new Rect(spot.x - r, spot.y - r, r * 2f, r * 2f), ring);
            GUI.color = o;
            Finger(spot + new Vector2(0f, (1f - tap) * -14f), fade);
        }

        void GhostSwipe(Vector2 from, Vector2 to, float cyc, float fade)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cyc - 0.15f) / 0.45f));
            float vis = cyc < 0.8f ? 1f : 1f - (cyc - 0.8f) / 0.2f;
            for (int i = 0; i < 12; i++)
            {
                var q = Vector2.Lerp(from, to, k * i / 11f);
                Fill(new Rect(q.x - 3, q.y - 3, 6, 6), new Color(1f, 1f, 1f, 0.35f * fade * vis));
            }
            Finger(Vector2.Lerp(from, to, k), fade * vis);
        }

        void GhostHold(Vector2 spot, int side, float cyc, float fade)
        {
            var o = GUI.color;
            // a ring that fills as the finger stays down, and the arrow of where he goes
            float k = Mathf.Clamp01(cyc / 0.85f);
            GUI.color = new Color(1f, 1f, 1f, 0.25f * fade);
            GUI.DrawTexture(new Rect(spot.x - 44, spot.y - 44, 88, 88), ring);
            GUI.color = new Color(0.85f, 0.08f, 0.08f, 0.8f * fade);
            float r = 20f + 24f * k;
            GUI.DrawTexture(new Rect(spot.x - r, spot.y - r, r * 2f, r * 2f), ring);
            GUI.color = o;
            Finger(spot, fade);
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Repeat(cyc * 3f - i * 0.33f, 1f);
                Fill(new Rect(spot.x + side * (60 + i * 18) - 4, spot.y - 4, 8, 8), new Color(1f, 1f, 1f, 0.6f * fade * a));
            }
        }

        // ---------------------------------------------------------------- free play

        void FreePlayGUI(float w, float h)
        {
            float now = Time.unscaledTime;
            float a = Ease(0f, 0.4f, now - lessonAt);
            // the heading and the two ways on
            float cw = Mathf.Min(560f, w - 220f);
            var head = new Rect(w * 0.5f - cw * 0.5f, 14, cw, 64);
            Fill(head, new Color(0f, 0f, 0f, 0.6f * a));
            Fill(new Rect(head.x, head.y, 4, head.height), new Color(0.8f, 0.06f, 0.07f, 0.95f * a));
            tTitle.normal.textColor = new Color(1f, 1f, 1f, a);
            var t2 = new GUIStyle(tTitle) { fontSize = 30 };
            GUI.Label(new Rect(head.x, head.y + 2, head.width, 38), Spaced("FREE PLAY"), t2);
            tTip.normal.textColor = new Color(1f, 1f, 1f, 0.6f * a);
            int got = 0;
            foreach (var b in learned) if (b) got++;
            GUI.Label(new Rect(head.x, head.y + 38, head.width, 22), "All three come at you · they keep getting up · " + got + " / " + lessons.Length + " moves landed", tTip);
            float by = head.yMax + 8;
            if (TrainButton(new Rect(w * 0.5f + 6, by, Mathf.Min(270f, cw * 0.5f - 6), 44), Spaced("CONTINUE STORY") + "  ›", true)) { EndTraining(); return; }
            if (TrainButton(new Rect(w * 0.5f - 6 - Mathf.Min(270f, cw * 0.5f - 6), by, Mathf.Min(270f, cw * 0.5f - 6), 44), Spaced("LESSONS")))
            {
                int first = Array.IndexOf(learned, false);
                StartLesson(first < 0 ? 0 : first);
                return;
            }

            // the checklist, down the right: every move, lit when landed; a row tapped replays its lesson
            float rw = Mathf.Min(250f, w * 0.26f), rh = Mathf.Min(24f, (h - 150f) / (lessons.Length + 1));
            var tog = new Rect(w - rw - 12, 14, rw, 30);
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
                var box = new Rect(row.x + 8, row.y + rh * 0.5f - 5, 10, 10);
                Fill(box, learned[i] ? new Color(0.85f, 0.1f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.25f));
                tRow.normal.textColor = learned[i] ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(new Rect(row.x + 26, row.y, rw - 30, rh), lessons[i].title, tRow);
                if (Event.current.type == EventType.Repaint) GestureInput.Blocked.Add(new Rect(row.x * scale, row.y * scale, row.width * scale, row.height * scale));
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { StartLesson(i, true); return; }
                y += rh;
            }
        }
    }
}
