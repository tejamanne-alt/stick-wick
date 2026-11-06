using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>INSTINCT: the prologue's first fight, directed. He is on his feet with the knife and
    /// three men in front of him, and his body knows what his head does not. The scene is
    /// choreographed beat by beat: the men attack as the script says, the camera cuts as the
    /// script says. At each beat's moment of decision the world crawls almost to a stop, the
    /// move appears (its name, how to make it, a ghost finger making it at the right place on
    /// the screen, a ring on the man it is for), and waits. The player makes the gesture, the real
    /// one, read through the game's own input, and the move happens, full speed, with all its
    /// weight. So the fight teaches every control by using it once, at the moment it is needed:
    ///
    ///   1 PUNCH   (tap high)        the first man swings; a jab snaps his head back
    ///   2 KICK    (tap low)         his guard comes up; a kick under it folds him into the second
    ///   3 COMBO   (tap tap tap)     the second rushes in; jab, cross, uppercut
    ///   4 SLIDE   (swipe at him)    the third swings a haymaker; he slides under it and takes his legs
    ///      the knife comes out of his belt, lightning
    ///   5 STAB    (tap high, left)  the third comes up behind him: one stab
    ///   6 THROW   (swipe down)      the second charges from across the room: the knife flies
    ///   7 TAP HIM (tap the man)     the first, the last one standing: he picks him, and ends it
    ///
    /// Nobody can lose it; a wrong gesture only shakes the prompt. It runs every playthrough
    /// (it is the scene, not a tutorial screen).</summary>
    public partial class StoryIntro
    {
        enum Ask { None, TapHigh, TapLow, Swipe, SwipeDown, TapHim }

        Ask ask = Ask.None;
        int askSide = 1, askStep, askSteps;
        Enemy askTarget;
        string askTitle, askHow;
        float askSince, wrongAt = -9f, okAt = -9f, beatAt;
        Vector2 okSpot;
        float cineScale = 1f, cineTarget = 1f;
        float duelX;                                   // where he stands when it starts
        Enemy A, Bm, C;                                // the first, the second, the third
        float instinctAt = -9f;
        bool duelOver;
        Vector3 shotPos, shotLook; float shotFov = 38f, shotUntil = -1f;

        void BeginDuel()
        {
            phase = Phase.Duel;
            duelOver = false;
            A = men[0]; Bm = men[1]; C = men[2];
            duelX = hero.transform.position.x;
            hero.CinePuppet();
            StartCoroutine(Duel());
        }

        // ================================================================ the fight, beat by beat

        IEnumerator Duel()
        {
            float H = duelX, y = home.y;
            hero.SetFacing(1);
            // the knife goes into his belt: his hands first
            hero.SetStyle(FightStyle.Fists);
            Sound.Play(Sfx.Cloth, 0.8f);
            instinctAt = Time.unscaledTime;
            foreach (var m0 in men) if (m0 != null) { m0.SetFacing(-1); m0.PuppetPlay("walk", 0.2f, 0.8f); }
            HeroAnim("idle", 0.25f);
            // the men fan out on their marks
            StartCoroutine(Move(A, H + 1.9f, 0.7f));
            StartCoroutine(Move(Bm, H + 2.7f, 0.7f));
            yield return Move(C, H + 3.5f, 0.7f);
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle", 0.25f);
            yield return Wait(0.3f);

            // ---------------------------------------------------------------- 1 PUNCH
            Say("THUG", "Stay down this time.", 1.4f);
            A.PuppetPlay("walk", 0.15f, 1.1f);
            yield return Move(A, H + 1.15f, 0.8f);
            A.PuppetPlay("windup", 0.05f);
            Sound.Play(Sfx.EnemyEffort, 0.8f, A.Chest);
            yield return Wait(0.22f);
            yield return Prompt(Ask.TapHigh, A, 1, "PUNCH", "TAP HIGH, on his side");
            HeroAnim("jab", 0.02f, 1.2f);
            yield return Wait(0.1f);
            Hit(A, "hit_front", false, Sfx.PunchLight, 1);
            StartCoroutine(Move(A, H + 1.55f, 0.25f));
            yield return Wait(0.45f);
            HeroAnim("idle", 0.2f);

            // ---------------------------------------------------------------- 2 KICK
            A.PuppetPlay("idle", 0.15f);                    // his guard comes up, high
            Sound.Play(Sfx.Cloth, 0.6f, A.Chest);
            yield return Wait(0.35f);
            yield return Prompt(Ask.TapLow, A, 1, "KICK", "His guard is high. TAP LOW");
            HeroAnim("front_kick", 0.02f, 1.1f);
            yield return Wait(0.17f);
            Hit(A, "knockdown", true, Sfx.Kick, 1);
            Slow(0.35f, 0.35f);
            StartCoroutine(Move(A, H + 3.6f, 0.6f, true));
            yield return Wait(0.3f);
            Bm.PuppetPlay("hit_back", 0.05f);              // the first goes into the second
            StartCoroutine(Move(Bm, H + 3.95f, 0.3f, true));
            Sound.Play(Sfx.BodyFall, 0.8f, A.transform.position);
            yield return Wait(0.5f);
            HeroAnim("idle", 0.2f);

            // ---------------------------------------------------------------- 3 COMBO
            Say("THUG", "Get him!", 1.0f);
            Bm.PuppetPlay("run", 0.1f, 1.2f);
            yield return Move(Bm, H + 1.15f, 0.5f, false, 0.35f);
            Bm.PuppetPlay("windup", 0.05f);
            Sound.Play(Sfx.EnemyEffort, 0.8f, Bm.Chest);
            yield return Wait(0.15f);
            string[] hits = { "jab", "cross", "uppercut" };
            for (int i = 0; i < 3; i++)
            {
                yield return Prompt(Ask.TapHigh, Bm, 1, i == 0 ? "COMBO" : i == 1 ? "AGAIN" : "FINISH IT",
                                    i == 0 ? "TAP HIGH, three times" : i == 1 ? "TAP" : "TAP", i, 3, i == 0 ? 0.035f : 0.12f);
                HeroAnim(hits[i], 0.02f, 1.25f);
                yield return Wait(i == 2 ? 0.16f : 0.09f);
                if (i < 2)
                {
                    Hit(Bm, i == 0 ? "hit_front" : "hit_back", i == 1, i == 0 ? Sfx.PunchLight : Sfx.PunchHeavy, 1);
                    StartCoroutine(Move(Bm, Bm.transform.position.x + 0.22f, 0.15f));
                }
                else
                {
                    Hit(Bm, "knockdown", true, Sfx.Finisher, 1);
                    Slow(0.25f, 0.5f);
                    if (cam != null) cam.ZoomPunch(6f, 0.25f);
                    StartCoroutine(Move(Bm, H + 5.0f, 0.75f, true));
                    StartCoroutine(Later(0.55f, () => Sound.Play(Sfx.BodyFall, 0.9f, Bm.transform.position)));
                }
            }
            yield return Wait(0.6f);
            HeroAnim("idle", 0.2f);

            // ---------------------------------------------------------------- 4 SLIDE
            C.PuppetPlay("run", 0.1f, 1.15f);
            Sound.Play(Sfx.EnemyEffort, 0.9f, C.Chest, 0.85f);
            yield return Move(C, H + 1.35f, 0.65f, false, 0.4f);
            C.PuppetPlay("windup", 0.04f, 0.8f);            // a big one, from the shoulder
            yield return Wait(0.2f);
            yield return Prompt(Ask.Swipe, C, 1, "SLIDE", "SWIPE towards him: under his swing");
            HeroAnim("slide_kick", 0.02f, 1.1f);
            Sound.Play(Sfx.Whoosh, 0.8f);
            StartCoroutine(Move(hero, H + 2.6f, 0.36f));
            yield return Wait(0.14f);
            Hit(C, "knockdown", true, Sfx.Kick, 1);
            Slow(0.3f, 0.45f);
            StartCoroutine(Later(0.45f, () => Sound.Play(Sfx.BodyFall, 1f, C.transform.position)));
            yield return Wait(0.55f);
            hero.SetFacing(-1);                               // he comes up facing back the way he came
            HeroAnim("idle", 0.25f);
            yield return Wait(0.35f);

            // ---------------------------------------------------------------- the knife
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("getup", 0.15f, 0.8f);
            Sound.Play(Sfx.Cloth, 0.7f);
            yield return Wait(0.25f);
            StartCoroutine(Later(0.8f, () => { A.PuppetPlay("walk", 0.2f, 0.7f); StartCoroutine(Move(A, H + 4.1f, 1.2f, false, 0.2f, 0.9f)); }));
            hero.GiveWeapon(FightStyle.Knife, 3);
            Sound.Play(Sfx.KnifeGrab, 1f);
            Sound.Play(Sfx.Glint, 0.8f);
            Lightning(1f, 0.85f, 0.15f);
            Shot(new Vector3(hero.transform.position.x + 1.2f, 0.45f, -2.0f), hero.transform.position + Vector3.up * 1.05f, 34f, 1.3f);
            yield return Wait(0.6f);
            Say("THUG", "...He's different.", 1.4f);
            yield return Wait(0.9f);
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle", 0.2f);

            // ---------------------------------------------------------------- 5 STAB (the man behind him, on the left)
            float hx = hero.transform.position.x;
            C.SetFacing(1);
            C.PuppetPlay("run", 0.1f, 1.2f);
            yield return Move(C, hx - 1.0f, 0.4f);
            C.PuppetPlay("windup", 0.05f);
            Sound.Play(Sfx.EnemyEffort, 0.8f, C.Chest);
            yield return Wait(0.15f);
            yield return Prompt(Ask.TapHigh, C, -1, "STAB", "TAP HIGH, on his side");
            HeroAnim("jab", 0.02f, 1.2f);
            yield return Wait(0.1f);
            Kill(C, Sfx.Knife, -1);
            Slow(0.2f, 0.7f);
            Shot(new Vector3(hx - 0.5f, 0.3f, -1.6f), C.Chest, 32f, 0.9f);
            yield return Wait(0.8f);

            // ---------------------------------------------------------------- 6 THROW (the second, charging from across the room)
            hero.SetFacing(1);
            HeroAnim("idle", 0.15f);
            Bm.SetFacing(-1);
            Bm.PuppetPlay("run", 0.1f, 1.3f);
            Sound.Play(Sfx.EnemyEffort, 0.9f, Bm.Chest, 0.9f);
            StartCoroutine(Move(Bm, hx + 1.7f, 0.9f));
            yield return Wait(0.35f);
            yield return Prompt(Ask.SwipeDown, Bm, 1, "THROW", "SWIPE DOWN: throw the knife");
            HeroAnim("throw_weapon", 0.02f, 1.2f);
            Sound.Play(Sfx.Whoosh, 0.9f);
            yield return Wait(0.12f);
            yield return Fly(Bm, 0.18f);
            Kill(Bm, Sfx.Knife, 1);
            Slow(0.2f, 0.6f);
            yield return Wait(0.7f);

            // ---------------------------------------------------------------- 7 TAP HIM (the first, the last one)
            A.SetFacing(-1);
            A.PuppetPlay("walk", 0.2f, 0.6f);                // the last one: he backs off a step, then he has to come
            Sound.Play(Sfx.Breath, 0.7f, A.Chest, 1.1f);
            yield return Move(A, hx + 2.2f, 0.9f, false, 0.2f, 0.4f);
            Say("THUG", "...Who ARE you?", 1.3f);
            yield return Wait(0.9f);
            A.PuppetPlay("run", 0.1f, 1.2f);
            Sound.Play(Sfx.EnemyEffort, 0.9f, A.Chest);
            yield return Move(A, hx + 1.2f, 0.4f);
            A.PuppetPlay("windup", 0.06f, 0.8f);
            yield return Wait(0.18f);
            A.PuppetPickable = true;
            yield return Prompt(Ask.TapHim, A, 1, "TAP HIM", "TAP ON HIM: pick your man, and strike", 0, 1, 0.025f);
            A.PuppetPickable = false;
            HeroAnim("run", 0.05f, 1.4f);
            yield return Move(hero, hx + 0.55f, 0.16f);
            HeroAnim("jab", 0.02f, 1.1f);
            Slow(0.12f, 1.0f);
            Shot(new Vector3(hx + 0.2f, 0.25f, -1.9f), A.Chest, 30f, 1.4f);
            yield return Wait(0.1f);
            Kill(A, Sfx.Knife, 1);
            Lightning(1f, 1f, 0.3f);
            yield return Wait(1.1f);
            HeroAnim("idle_loose", 0.4f);
            Sound.Play(Sfx.Breath, 0.9f);
            yield return Wait(1.4f);
            duelOver = true;
            if (TimeController.Instance != null) TimeController.Instance.CineScale = 1f;
            phase = Phase.Fight;                              // UpdateFight sees them all down: the memory
        }

        // ================================================================ helpers for the choreography

        static IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Later(float s, System.Action a) { yield return Wait(s); a(); }

        void Place(Enemy e, float x) { if (e != null) e.PuppetPose(new Vector3(x, home.y, 0f)); }

        /// <summary>Move a fighter along the floor to x over `dur` (scaled time). `fall`: decelerating
        /// as a body thrown down does; `ease`: how much of it is the run-up.</summary>
        IEnumerator Move(Fighter f, float x, float dur, bool fall = false, float ease = 0.2f, float z = 0f)
        {
            if (f == null) yield break;
            float x0 = f.transform.position.x, z0 = f.transform.position.z, t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = fall ? 1f - (1f - k) * (1f - k) : Mathf.SmoothStep(0f, 1f, k);
                var p = new Vector3(Mathf.Lerp(x0, x, k), home.y, Mathf.Lerp(z0, z, k));
                if (f is Enemy en) en.PuppetPose(p); else hero.CinePose(p);
                yield return null;
            }
        }

        void Hit(Enemy e, string react, bool heavy, Sfx s, int dir)
        {
            if (e == null) return;
            e.PuppetPlay(react, 0.03f);
            e.PuppetFlash();
            Sound.Play(s, 1f, e.Chest);
            Sound.Play(Sfx.EnemyHurt, heavy ? 0.8f : 0.55f, e.Chest);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Burst(e.Chest + new Vector3(dir * 0.15f, 0.1f, 0f), heavy ? 1.2f : 0.7f);
                InkFx.Instance.Splash(e.Chest, new Vector3(dir, 0.35f, 0f), heavy ? 10 : 5, e.InkColor);
            }
            if (cam != null) cam.Shake(heavy ? 0.2f : 0.08f, heavy ? 0.25f : 0.15f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(heavy ? 6 : 3);
            Buzz();
        }

        void Kill(Enemy e, Sfx s, int dir)
        {
            if (e == null) return;
            Hit(e, "knockdown", true, s, dir);
            Sound.Play(Sfx.Finisher, 0.7f, e.Chest);
            if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(dir, 0.5f, 0f), 16, e.InkColor);
            StartCoroutine(Move(e, e.transform.position.x + dir * 0.9f, 0.5f, true));
            StartCoroutine(Later(0.45f, () => { Sound.Play(Sfx.BodyFall, 1f, e.transform.position); e.PuppetDie(); }));
        }

        /// <summary>A beat of slow motion after a blow (scaled under the story's own clock).</summary>
        static void Slow(float scale, float seconds)
        {
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(scale, seconds, 0.02f, 0.25f);
        }

        /// <summary>The knife leaves his hand and turns over in the air into the man.</summary>
        IEnumerator Fly(Enemy target, float dur)
        {
            var from = hero.body != null && hero.body.Bone("hand.R") != null ? hero.body.Bone("hand.R").position : hero.Chest;
            var go = new GameObject("ThrownKnife");
            var holder = new GameObject("Holder").transform;
            holder.SetParent(go.transform, false);
            holder.localScale = Vector3.one * 1.4f;
            FighterFactory.Model(holder, FightStyle.Knife, hero.weaponMaterial);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                var to = target.Chest + Vector3.up * 0.1f;
                go.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.12f;
                go.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(k * 900f, 0f, 0f);
                yield return null;
            }
            go.transform.SetParent(target.transform, true);   // it stays in him
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }

        /// <summary>Hold a camera shot for `seconds` (real), over the fight framing.</summary>
        void Shot(Vector3 pos, Vector3 look, float fov, float seconds)
        {
            shotPos = pos; shotLook = look; shotFov = fov;
            shotUntil = Time.unscaledTime + seconds;
        }

        // ================================================================ the prompt

        /// <summary>The world crawls almost to a stop; the move appears and waits for the gesture.</summary>
        IEnumerator Prompt(Ask kind, Enemy target, int side, string title, string how, int step = 0, int steps = 1, float freeze = 0.03f)
        {
            ask = kind; askTarget = target; askSide = side; askTitle = title; askHow = how; askStep = step; askSteps = steps;
            askSince = Time.unscaledTime;
            beatAt = Time.unscaledTime + 0.9f;
            cineTarget = freeze;
            if (step == 0) Sound.Play(Sfx.SlowmoIn, 0.45f);
            var gi = GestureInput.Instance;
            if (gi != null) gi.Flush();
            hero.FaceTowards(target != null ? target.transform.position.x : hero.transform.position.x + side);
            bool done = false;
            while (!done)
            {
                if (gi != null)
                {
                    for (var c = gi.Next(); c != Command.None; c = gi.Next())
                    {
                        if (Accepts(kind, side, target, c)) { done = true; break; }
                        wrongAt = Time.unscaledTime;
                        Sound.Play(Sfx.Whoosh, 0.25f, null, 0.6f);
                    }
                }
                if (!done && Time.unscaledTime > beatAt) { beatAt = Time.unscaledTime + 1.0f; Sound.Play(Sfx.Heartbeat, 0.6f); }
                if (!done) yield return null;
            }
            okAt = Time.unscaledTime;
            okSpot = GhostSpot(kind, side, target, out _);
            ask = Ask.None;
            cineTarget = 1f;
            cineScale = Mathf.Max(cineScale, 0.6f);
            if (step == steps - 1) Sound.Play(Sfx.SlowmoOut, 0.35f);
        }

        bool Accepts(Ask kind, int side, Enemy target, Command c)
        {
            bool toward(Command right, Command left, Command fwd) => c == (side > 0 ? right : left) || (c == fwd && hero.Facing == side);
            switch (kind)
            {
                case Ask.TapHigh: return toward(Command.StrikeRight, Command.StrikeLeft, Command.StrikeForward);
                case Ask.TapLow: return toward(Command.KickRight, Command.KickLeft, Command.KickForward);
                case Ask.Swipe: return toward(Command.DodgeRight, Command.DodgeLeft, Command.DodgeForward);
                case Ask.SwipeDown: return c == Command.ThrowWeapon;
                case Ask.TapHim:
                    if (c == Command.LockNext) return true;                                  // the keyboard's T
                    return hero.Locked == target && toward(Command.StrikeRight, Command.StrikeLeft, Command.StrikeForward);
            }
            return false;
        }

        // ================================================================ every frame

        void UpdateDuel()
        {
            float dt = Time.unscaledDeltaTime;
            // the story's clock: crawl into a prompt fast, come out of it faster
            float rate = cineTarget < cineScale ? 6f : 12f;
            cineScale = Mathf.MoveTowards(cineScale, cineTarget, dt * rate);
            if (TimeController.Instance != null) TimeController.Instance.CineScale = duelOver ? 1f : cineScale;
            barsPx = Mathf.MoveTowards(barsPx, duelOver ? 0f : 56f, dt * 120f);
            DuelCamera();
        }

        void DuelCamera()
        {
            if (cam == null) return;
            var hp = hero.transform.position;
            if (Time.unscaledTime < shotUntil)
            {
                cam.ScriptRoll = 2f;
                cam.Scripted(shotPos, shotLook, 1f, shotFov);
                return;
            }
            // side on, the hero in the middle of the frame (the halves of the screen are his left and
            // right, as the taps are), pulled back enough to hold whoever the beat is about
            float reach = 1.6f;
            foreach (var m0 in men) if (m0 != null && m0.Alive) reach = Mathf.Max(reach, Mathf.Min(4.5f, Mathf.Abs(m0.transform.position.x - hp.x)));
            float dist = 3.4f + reach * 0.9f;
            float lean = ask != Ask.None ? 0.97f : 1f;      // in the crawl the lens creeps in
            cam.ScriptRoll = Mathf.MoveTowards(cam.ScriptRoll, 0f, Time.unscaledDeltaTime * 8f);
            cam.Scripted(new Vector3(hp.x, 1.25f, -dist * lean) + Sway(0.02f, 0.5f, 21f), new Vector3(hp.x, 0.95f, 0f), 1f, 40f);
        }

        // ================================================================ the prompt on screen

        Vector2 ScreenOf(Vector3 world)
        {
            var c = Camera.main;
            if (c == null) return Vector2.zero;
            var p = c.WorldToScreenPoint(world);
            return new Vector2(p.x / scale, (Screen.height - p.y) / scale);
        }

        /// <summary>Where the gesture is made: a tap on the target's half of the screen, high or low
        /// as the game reads it; a tap on the man himself; a swipe from beside the hero.</summary>
        Vector2 GhostSpot(Ask kind, int side, Enemy target, out Vector2 to)
        {
            float w = Screen.width / scale, h = Screen.height / scale;
            var tp = target != null ? ScreenOf(target.Chest) : new Vector2(w * (side > 0 ? 0.75f : 0.25f), h * 0.4f);
            float tx = side > 0 ? Mathf.Clamp(tp.x, w * 0.58f, w - 70f) : Mathf.Clamp(tp.x, 70f, w * 0.42f);
            var gi = GestureInput.Instance;
            float line = gi != null ? 1f - gi.kickLine : 0.55f;            // GUI y where low begins
            to = Vector2.zero;
            switch (kind)
            {
                case Ask.TapHigh: return new Vector2(tx, h * line * 0.5f);
                case Ask.TapLow: return new Vector2(tx, h * (line + (1f - line) * 0.55f));
                case Ask.TapHim: return tp;
                case Ask.Swipe:
                    to = new Vector2(w * 0.5f + side * w * 0.32f, h * 0.5f);
                    return new Vector2(w * 0.5f + side * w * 0.1f, h * 0.5f);
                case Ask.SwipeDown:
                    to = new Vector2(w * 0.68f, h * 0.72f);
                    return new Vector2(w * 0.68f, h * 0.32f);
            }
            return tp;
        }

        string KeyHint(Ask kind, int side)
        {
            if (Application.isMobilePlatform) return null;
            switch (kind)
            {
                case Ask.TapHigh: return side > 0 ? "or press  D" : "or press  A";
                case Ask.TapLow: return side > 0 ? "or press  X" : "or press  Z";
                case Ask.Swipe: return side > 0 ? "or press  E" : "or press  Q";
                case Ask.SwipeDown: return "or press  S";
                case Ask.TapHim: return "or press  T";
            }
            return null;
        }

        void DuelGUI(float w, float h)
        {
            // INSTINCT, once, as the knife goes into his belt
            float ia = Time.unscaledTime - instinctAt;
            if (ia >= 0f && ia < 2.6f)
            {
                float a = Mathf.Min(Ease(0f, 0.4f, ia), 1f - Ease(2.0f, 2.6f, ia));
                big.normal.textColor = new Color(1f, 1f, 1f, 0.9f * a);
                GUI.Label(new Rect(0, h * 0.2f, w, 90), Spaced("INSTINCT"), big);
                Fill(new Rect(w * 0.5f - 90 * a, h * 0.2f + 84, 180 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
            }
            // a made gesture: a white burst where it landed
            float ok = Time.unscaledTime - okAt;
            if (ok >= 0f && ok < 0.35f)
            {
                float k = ok / 0.35f, r = 30f + 70f * k;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 1f - k);
                GUI.DrawTexture(new Rect(okSpot.x - r, okSpot.y - r, r * 2f, r * 2f), ring);
                GUI.color = old;
            }
            if (ask == Ask.None) return;

            float since = Time.unscaledTime - askSince;
            float fade = Ease(0f, 0.18f, since);
            // the world held: the edges darken
            var o = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * fade);
            GUI.DrawTexture(new Rect(-w * 0.05f, -h * 0.05f, w * 1.1f, h * 1.1f), vig);
            GUI.color = o;

            // the man it is for: a ring on him
            if (askTarget != null)
            {
                var tp = ScreenOf(askTarget.Chest);
                float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 7f);
                float r = 46f * pulse;
                GUI.color = new Color(0.85f, 0.08f, 0.08f, 0.85f * fade);
                GUI.DrawTexture(new Rect(tp.x - r, tp.y - r * 1.25f, r * 2f, r * 2.5f), ring);
                GUI.color = o;
            }

            // the move: its name, how to make it, (the key on a desktop), steps for the combo
            float shake = Time.unscaledTime - wrongAt < 0.3f ? Mathf.Sin(Time.unscaledTime * 80f) * 8f * (1f - (Time.unscaledTime - wrongAt) / 0.3f) : 0f;
            big.normal.textColor = new Color(1f, 1f, 1f, fade);
            GUI.Label(new Rect(shake, h * 0.13f, w, 84), Spaced(askTitle), big);
            prompt.normal.textColor = Time.unscaledTime - wrongAt < 0.6f ? new Color(1f, 0.55f, 0.5f, fade) : new Color(1f, 1f, 1f, 0.9f * fade);
            GUI.Label(new Rect(shake, h * 0.13f + 82, w, 40), askHow, prompt);
            var key = KeyHint(ask, askSide);
            if (key != null)
            {
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.6f * fade);
                GUI.Label(new Rect(0, h * 0.13f + 120, w, 26), key, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            if (askSteps > 1)
                for (int i = 0; i < askSteps; i++)
                    Fill(new Rect(w * 0.5f - askSteps * 14 + i * 28, h * 0.13f + 152, 20, 5), i < askStep ? Color.white : i == askStep ? new Color(0.85f, 0.08f, 0.08f, fade) : new Color(1f, 1f, 1f, 0.25f * fade));

            // the ghost finger, making the gesture where it is made
            var spot = GhostSpot(ask, askSide, askTarget, out var to);
            float cyc = Mathf.Repeat(since, 1.2f) / 1.2f;
            bool swipe = ask == Ask.Swipe || ask == Ask.SwipeDown;
            if (swipe)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cyc - 0.15f) / 0.5f));
                var p = Vector2.Lerp(spot, to, k);
                for (int i = 0; i < 10; i++)
                {
                    float kk = k * i / 9f;
                    var q = Vector2.Lerp(spot, to, kk);
                    Fill(new Rect(q.x - 3, q.y - 3, 6, 6), new Color(1f, 1f, 1f, 0.35f * fade * (cyc < 0.8f ? 1f : 0f)));
                }
                Finger(p, fade * (cyc < 0.8f ? 1f : 1f - (cyc - 0.8f) / 0.2f));
            }
            else
            {
                float tap = cyc < 0.18f ? cyc / 0.18f : 1f;
                float r = 18f + 46f * Mathf.Clamp01((cyc - 0.15f) / 0.5f);
                GUI.color = new Color(1f, 1f, 1f, 0.7f * fade * (1f - Mathf.Clamp01((cyc - 0.15f) / 0.5f)));
                if (cyc > 0.15f) GUI.DrawTexture(new Rect(spot.x - r, spot.y - r, r * 2f, r * 2f), ring);
                GUI.color = o;
                Finger(spot + new Vector2(0f, (1f - tap) * -14f), fade);
                if (ask == Ask.TapHigh || ask == Ask.TapLow)
                {
                    // the line the game reads high and low by, faint, on that half
                    var gi = GestureInput.Instance;
                    float ly = h * (gi != null ? 1f - gi.kickLine : 0.55f);
                    Fill(new Rect(askSide > 0 ? w * 0.5f : 0f, ly, w * 0.5f, 1), new Color(1f, 1f, 1f, 0.18f * fade));
                }
            }
        }

        void Finger(Vector2 p, float a)
        {
            var o = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f * a);
            GUI.DrawTexture(new Rect(p.x - 20, p.y - 20, 40, 40), disc);
            GUI.color = new Color(1f, 1f, 1f, 0.95f * a);
            GUI.DrawTexture(new Rect(p.x - 11, p.y - 11, 22, 22), circle);
            GUI.color = o;
        }
    }
}
