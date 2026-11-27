using UnityEngine;

namespace ProjectInk
{
    /// <summary>Moving on his own, on the V4 body (chapter 1): the "hero alone" set.
    ///   HOLD a half          walk → jog → run: the speed builds the longer you hold (near men: no
    ///                        faster than a jog); let go from a jog or run and he plants and settles
    ///   HOLD the other half  while running: a real turn (plant, pivot, push off)
    ///   SWIPE DOWN, HOLD     crouch; slide that finger sideways to creep (quiet: no footsteps)
    ///   SWIPE UP             jump; at a drum: vault it; while running: a running jump
    ///   SWIPE UP-LEFT/-RIGHT jump that way (a drum in the way: vault it)
    ///   SWIPE DOWN ×2        drop through the floor (one swipe: throw the weapon / duck)
    ///   TAP, nobody near     a step that way (no punching the air)
    ///   TAP A THING          walk to it and use it: pick up a weapon, push a drum over
    ///   (on its own)         loose and looking around when alone, the guard when a man is within 6 m</summary>
    public partial class HeroController
    {
        // HOLD walks (the Wick walk); TAP, THEN HOLD runs: off at RunStart (quicker than an ordinary man's
        // 2.6), building to RunSpeedV4 over RunBuild seconds (the barge needs 4.0: half a second's run-up)
        const float WalkSpeed = 1.25f, JogSpeed = 3.4f, RunSpeedV4 = 4.6f, SneakSpeed = 0.8f, RunBuild = 0.9f;
        const float RunClipSpeed = 4.0f;          // the run clip's own ground speed (played 0.85-1.15 across the range)
        /// <summary>A directed story walk's pace (StoryBank.PlayerWalk): a hold there moves at it, walking or running
        /// by how fast it is (the walk to the bank doors walks; the chase after the van runs). 0: none.</summary>
        public float ScriptedPace;
        const float AloneRange = 6f;

        bool MovesV4 => Contact && body != null && body.Has("sprint");
        bool crouching;
        float holdTime, idleCheckAt, traverseFrom, traverseTo;
        float stepGoal = float.NaN, stepLastX, stepStuck, walkLastX, walkBlocked;
        /// <summary>Pushing a way he can't go (the story's walks finish on it rather than wait for ever).</summary>
        public bool WalkBlocked => walkBlocked > 0.5f;
        string traverse;                     // the clip of a traversal in progress (vault, pick_up, push, run_stop, run_turn)
        Component interactWith;
        WeaponPickup pickingUp;
        bool traverseHit;

        /// <summary>Nobody within six metres on this floor: he relaxes.</summary>
        bool Alone
        {
            get
            {
                foreach (var e in Enemy.All)
                    if (e != null && e.Alive && !e.Fleeing && !e.Puppeted && Mathf.Abs(e.transform.position.x - transform.position.x) < AloneRange &&
                        Mathf.Abs(e.transform.position.y - transform.position.y) < 1.6f) return false;
                return true;
            }
        }

        /// <summary>No man up and able to fight within twelve metres on this floor: a story walk, not a fight.</summary>
        bool NoFightNear
        {
            get
            {
                if (StoryIntro.Instance != null && StoryIntro.Instance.InTraining) return false;     // the training ground: running is a lesson
                foreach (var e in Enemy.All)
                    if (e != null && e.Alive && !e.Fleeing && !e.Passive && !e.Puppeted && Mathf.Abs(e.transform.position.x - transform.position.x) < 12f &&
                        Mathf.Abs(e.transform.position.y - transform.position.y) < 1.6f) return false;
                return true;
            }
        }

        /// <summary>A real threat: someone attacking, winding up or charging within six metres, a man up and
        /// fighting within three, or he has just struck or been hit. Otherwise he stands easy (not a boxer's
        /// guard all day: the men who are down, unaware, held or standing off don't count).</summary>
        bool Threatened
        {
            get
            {
                // the guard comes up at once and goes down only when nobody has threatened him for a while, and a man
                // must step further off (4 m) than he came in (3.2 m): a man hovering at the edge, or one flicking in and
                // out of his attack, no longer has the guard going up and down (the "shaking" guard)
                bool now = ThreatNow(guardUp ? 4.0f : 3.2f);
                if (now) { guardUp = true; guardDownAt = Time.time + 0.8f; }
                else if (Time.time > guardDownAt) guardUp = false;
                return guardUp;
            }
        }
        bool guardUp;
        float guardDownAt;

        bool ThreatNow(float near)
        {
            if (Time.time - lastAttackEnd < 1.6f || Time.time - lastHurtAt < 1.6f) return true;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive || e.Fleeing || e.Unaware || e.IsDown || e.Held || e.Passive || e.Puppeted) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 1.6f) continue;
                float dx = Mathf.Abs(e.transform.position.x - transform.position.x);
                if ((e.Engaged || e.Telegraphing || e.Charging) && dx < AloneRange) return true;
                if (!e.HoldFire && !e.Keep && dx < near) return true;
            }
            return false;
        }

        string IdleClipV4 => MovesV4 && Style == FightStyle.Fists && !Threatened ? "idle_alone" : "idle";

        // ------------------------------------------------------------------ footwork on guard
        const float GuardStepSpeed = 1.5f;      // m/s: quick, short step-drags (the old advance / retreat loops)
        const float GuardClipSpeed = 0.7f;      // the advance / retreat clips' own ground speed
        const float GuardStepTime = 0.9f;       // held longer than this, he walks
        // bare-handed on guard (reviewed: the step-drags above played at 2x were seven twitchy shuffles a
        // second, the hips lurching inside every step): towards him, THE STALK, a low real walk behind the
        // guard; away, GIVING GROUND, the body gliding back while the feet step. Each at its own pace,
        // played near 1x. Walking him down never drops the guard; backing off, after a while he turns and goes.
        // (reviewed again: the stalk crouched and crossed its feet and the back-step snapped a foot every step, so
        // the guard shook; both are now a boxer's step-drag, v4_far.py glide_drag, played at their own pace)
        const float StalkSpeed = 0.8f, StalkClip = 0.788f;        // guard_stalk: the lead foot out, the rear after it
        const float GiveSpeed = 0.72f, GiveClip = 0.712f;         // guard_back: the rear foot back, the lead after it
        const float GiveTime = 1.2f;            // backing off longer than this: he turns and walks away
        // the pipe near a man (reviewed like the bare footwork): rod_step, walking him down with it cocked on the
        // shoulder; rod_back, the gliding back-step. Each at its own clip's pace, played near 1x.
        const float RodStepSpeed = 0.9f, RodStepClip = 0.9f;      // rod_step (the pipe's stalk)
        const float RodBackSpeed = 0.75f, RodBackClip = 0.6f;     // rod_back
        const float RodWalkSpeed = 1.4f;        // rod_walk: a real walk with the pipe on the shoulder
        // the katana near a man: okuri-ashi both ways, the body gliding level, the point on his eyes
        const float SwordStepSpeed = 0.9f, SwordBackSpeed = 0.8f;
        const float SwordStepClip = 0.75f;      // katana_step / katana_back ground speed
        const float SwordWalkSpeed = 1.3f;      // katana_walk: the swordsman's walk, the blade trailing

        /// <summary>The nearest man who makes him keep his guard (see Threatened), within `range`.</summary>
        Enemy NearestThreat(float range)
        {
            Enemy best = null;
            float bd = range;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive || e.Fleeing || e.Unaware || e.IsDown || e.Held || e.Passive || e.Puppeted) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 1.6f) continue;
                float d = Mathf.Abs(e.transform.position.x - transform.position.x);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------------ never a walk on the spot
        // Whatever put it there (input let go, a wall, a scene that started a walk and stopped moving him),
        // a walk or run cycle on a hero who isn't going anywhere is wrong: after a moment of standing
        // still he simply stands. Checked last thing every frame, against where he really is.
        static readonly string[] Gaits = { "walk_alone", "walk", "run", "sprint", "katana_walk", "rod_walk", "advance", "retreat", "guard_stalk", "guard_back", "rod_step", "rod_back", "katana_step", "katana_back", "rod_run", "katana_run" };
        Vector3 gaitLastPos;
        float gaitStill;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var p = transform.position;
            float moved = Mathf.Abs(p.x - gaitLastPos.x) + Mathf.Abs(p.z - gaitLastPos.z);
            gaitLastPos = p;
            if (dt <= 0f || body == null || state == State.Dead || traverse != null) { gaitStill = 0f; return; }
            if (System.Array.IndexOf(Gaits, body.CurrentName) < 0) { gaitStill = 0f; return; }
            // under 0.15 m/s is standing (a real walk is 1.1 m/s or more)
            gaitStill = moved < 0.15f * dt ? gaitStill + dt : 0f;
            if (gaitStill < 0.12f || !Grounded) return;
            gaitStill = 0f;
            running = false;
            if (state == State.Idle || CinematicLock) body.Play(IdleClip, 0.2f);
        }

        // ------------------------------------------------------------------ the idle loop
        void UpdateIdleV4()
        {
            var gi = GestureInput.Instance;
            HandleCommand(gi != null ? gi.Next() : Command.None);
            if (state != State.Idle) { running = false; crouching = false; return; }
            float dt = Time.deltaTime;

            // crouched: creep
            if (gi != null && gi.CrouchHeld && Grounded)
            {
                running = false;
                holdTime = 0f;
                stepGoal = float.NaN;
                float c = gi.CrouchAxis;
                velocity.x = Mathf.MoveTowards(velocity.x, c * SneakSpeed, 6f * dt);
                if (Mathf.Abs(c) > 0.05f) SetFacing(c > 0 ? 1 : -1);
                bool moving = Mathf.Abs(velocity.x) > 0.06f;
                string clip = moving ? "sneak" : "crouch_idle";
                if (KatanaMode)
                {
                    // armed: low with it (the pipe at the hip, the blade in gedan)
                    string pre = RodMode ? "rod_" : "katana_";
                    string armed = pre + (moving ? "sneak" : "crouch");
                    if (body.Has(armed)) clip = armed;
                }
                if (body.CurrentName != clip) body.Play(clip, crouching ? 0.15f : 0.2f, true, 1f);
                if (moving) body.SetSpeed(Mathf.Clamp(Mathf.Abs(velocity.x) / SneakSpeed, 0.4f, 1.4f));
                crouching = true;
                return;
            }
            if (crouching) { crouching = false; body.Play(IdleClip, 0.25f); }

            float a = Axis;
            int dir = Mathf.Abs(a) > 0.01f ? (a > 0 ? 1 : -1) : 0;
            // a step towards a goal (a tap with nobody near, or walking to a thing)
            if (dir == 0 && !float.IsNaN(stepGoal))
            {
                float dg = stepGoal - transform.position.x;
                // blocked short of it (a wall, the edge of where the story lets him go): stop, don't run on the spot
                if (Mathf.Abs(transform.position.x - stepLastX) < 0.002f) stepStuck += dt; else stepStuck = 0f;
                stepLastX = transform.position.x;
                if (Mathf.Abs(dg) < 0.05f || stepStuck > 0.2f)
                {
                    bool arrived = Mathf.Abs(dg) < 0.05f;
                    stepGoal = float.NaN; stepStuck = 0f; velocity.x = 0f;
                    if (arrived) ArriveAtThing();
                    if (running) { running = false; body.Play(IdleClip, 0.2f); }
                    return;
                }
                dir = dg > 0 ? 1 : -1; a = dir * 0.4f;
            }
            else if (dir != 0) stepGoal = float.NaN;

            // walking off away from the man he picked (a moment's backing off, still facing him, doesn't count):
            // he's turned away from him, the pick goes
            if (dir != 0 && LockedSide != 0 && dir != LockedSide) { lockAwayFor += dt; if (lockAwayFor > 1.2f) ReleaseLock(); }
            else lockAwayFor = 0f;

            if (dir != 0 && Grounded)
            {
                holdTime += dt;
                // running hard the other way: a real turn
                if (velocity.x * dir < -2.2f && !KatanaMode) { BeginTraverse("run_turn", 0f); SetFacing(dir); return; }   // (armed: the weapon's run just turns)
                if (TryLowerDown(dir)) return;                          // a raised floor's edge: lower himself down
                // in a fight, on his guard: a boxer's footwork for the first steps (step-drag in, or the
                // back-step out still facing his man), never the street walk with the guard dropping;
                // held on, he breaks into a run (turning away if he's leaving)
                // (the pipe the same, in its own stance: rod_step in, rod_back out)
                bool rodSteps = RodMode && KatanaMode && body.Has("rod_back");
                // (the katana: okuri-ashi in chudan, katana_step in, katana_back out, the point on his eyes)
                bool swordSteps = KatanaMode && !RodMode && !sheathed && body.Has("katana_back");
                var giNow = GestureInput.Instance;
                bool runHeld = giNow != null && giNow.RunHeld && float.IsNaN(stepGoal);
                // (the knife too: chapter 1's fights are fought with it in hand, the guard the same)
                bool bareSteps = (Style == FightStyle.Fists || Style == FightStyle.Knife) && !KatanaMode && carried == null && body.Has("retreat");
                bool stalk = bareSteps && body.Has("guard_stalk");
                // how long he may keep it up: the weapons' steps a moment; bare-handed walking him down, as long
                // as it's held; backing off, a little longer, then he turns and goes
                // (the pipe and the katana the same: walking him down they keep their guard; backing off, a while)
                bool glide = stalk || rodSteps || swordSteps;
                bool towards = false;
                if (glide) { var near = NearestThreat(4.5f); towards = near == null || (near.transform.position.x > transform.position.x ? 1 : -1) == dir; }
                float keepUp = glide ? (towards ? float.MaxValue : GiveTime) : GuardStepTime;
                bool footwork = !runHeld && Contact && (rodSteps || swordSteps || bareSteps) &&
                                float.IsNaN(stepGoal) && holdTime < keepUp && Threatened;
                if (footwork)
                {
                    var man = NearestThreat(4.5f);
                    int face = man != null ? (man.transform.position.x > transform.position.x ? 1 : -1) : dir;
                    SetFacing(face);
                    if (dir == face && WalkIntoClinch(dir)) return;               // stepping into him: take hold
                    bool fwd = dir == face;
                    float stepSpeed = rodSteps ? (fwd ? RodStepSpeed : RodBackSpeed) : swordSteps ? (fwd ? SwordStepSpeed : SwordBackSpeed)
                                    : stalk ? (fwd ? StalkSpeed : GiveSpeed) : GuardStepSpeed;
                    velocity.x = Mathf.MoveTowards(velocity.x, dir * stepSpeed, (glide ? 6f : 10f) * dt);   // (eased in: no lurch)
                    string step = rodSteps ? (dir == face ? "rod_step" : "rod_back") : swordSteps ? (dir == face ? "katana_step" : "katana_back")
                                : stalk ? (dir == face ? "guard_stalk" : "guard_back")
                                : dir == face ? "advance" : "retreat";
                    float stepClip = rodSteps ? (fwd ? RodStepClip : RodBackClip) : swordSteps ? SwordStepClip : stalk ? (fwd ? StalkClip : GiveClip) : GuardClipSpeed;
                    float stepPlay = Mathf.Clamp(Mathf.Abs(velocity.x) / stepClip, glide ? 0.5f : 0.6f, glide ? 1.5f : 2.6f);
                    if (body.CurrentName != step) body.Play(step, glide ? 0.2f : 0.12f, false, stepPlay);
                    else body.SetSpeed(stepPlay);
                    walkLastX = transform.position.x;
                    walkBlocked = 0f;
                    running = true;
                    return;
                }
                SetFacing(dir);
                if (TryBarge(dir)) return;                              // flat out into him: the shoulder barge
                if (Contact && WalkIntoClinch(dir)) return;
                // hold: the walk; tap, then hold: the run, off quick and building (the barge comes with the run-up)
                float want = runHeld ? Mathf.Lerp(JogSpeed, RunSpeedV4, Mathf.Clamp01(holdTime / RunBuild)) : WalkSpeed;
                // a directed story walk: its own pace (a walk, or a run if it's that fast)
                if (ScriptedPace > 0f) want = ScriptedPace >= 2.8f ? ScriptedPace : Mathf.Min(WalkSpeed, ScriptedPace);
                if (KatanaMode)
                {
                    if (sheathed && !GestureInput.IaiHolding) { sheathed = false; body.Play("katana_guard", 0.15f); }   // walking off: the blade comes out
                    // armed: the weapon's own walk; tap, then hold: its own run (the pipe heavy, the katana light)
                    bool armedRun = runHeld && body.Has(RodMode ? "rod_run" : "katana_run") && ScriptedPace <= 0f;
                    float runK = Mathf.Clamp01(holdTime / RunBuild);
                    want = sheathed ? 0f
                         : armedRun ? (RodMode ? Mathf.Lerp(3.2f, 4.0f, runK) : Mathf.Lerp(3.4f, 4.4f, runK))
                         : Mathf.Min(want, RodMode && body.Has("rod_step") ? RodWalkSpeed : !RodMode && body.Has("katana_back") ? SwordWalkSpeed : 1.4f);
                }
                float accel = Mathf.Abs(velocity.x) < want ? 9f : 12f;
                velocity.x = Mathf.MoveTowards(velocity.x, dir * want, accel * dt);
                // held against something (the edge of where he may go, a wall, a man): he stands, still pushing
                // that way, and walks on the moment the way is clear (never a walk cycle on the spot)
                float movedX = Mathf.Abs(transform.position.x - walkLastX);
                walkLastX = transform.position.x;
                walkBlocked = movedX < 0.002f && holdTime > 0.15f ? walkBlocked + dt : 0f;
                if (walkBlocked > 0.12f)
                {
                    string stand = IdleClip;
                    if (body.CurrentName != stand) body.Play(stand, 0.2f);
                    running = false;
                    return;
                }
                float sp = Mathf.Abs(velocity.x);
                string clip = KatanaMode ? (sp >= 2.2f && body.Has(RodMode ? "rod_run" : "katana_run") ? (RodMode ? "rod_run" : "katana_run")
                                            : RodMode && body.Has("rod_walk") ? "rod_walk" : "katana_walk")
                            : sp < 1.9f ? "walk_alone" : "run";
                float clipSpeed = clip == "rod_run" ? 3.6f : clip == "katana_run" ? 3.9f
                                : clip == "rod_walk" && body.Has("rod_step") ? RodWalkSpeed : clip == "katana_walk" && body.Has("katana_back") ? SwordWalkSpeed
                                : clip == "katana_walk" || clip == "rod_walk" ? 0.7f
                                : clip == "walk_alone" ? WalkSpeed : RunClipSpeed;
                float play = Mathf.Clamp(sp / clipSpeed, 0.55f, 1.5f);
                if (Time.time < landLockUntil) { }                       // a running landing playing out (land_run), then the run
                else if (body.CurrentName != clip) body.Play(clip, clip == "rod_walk" || clip == "katana_walk" ? 0.25f : 0.15f, false, play);   // the pipe: out of its stance into the walk, eased
                else body.SetSpeed(play);
                running = true;
                return;
            }

            // let go
            holdTime = 0f;
            if (running)
            {
                running = false;
                if (Mathf.Abs(velocity.x) > 2.4f && Grounded && !KatanaMode) { NoteRunStop(); BeginTraverse("run_stop", 0f); velocity.x = 0f; return; }
                velocity.x = 0f;
                body.Play(IdleClip, KatanaMode ? 0.3f : 0.2f);         // the blade comes up from the carry into the guard, not snapped
                return;
            }
            // relaxed when alone, the guard when someone comes near
            if (Time.time > idleCheckAt)
            {
                idleCheckAt = Time.time + 0.3f;
                string want = IdleClip;
                if ((body.CurrentName == "idle" || body.CurrentName == "idle_alone") && body.CurrentName != want) body.Play(want, 0.45f);
            }
        }

        // ------------------------------------------------------------------ taps
        /// <summary>A tap with nobody within reach: a step that way, not a punch at the air.</summary>
        bool TryAloneTap(int side, bool kick)
        {
            if (!MovesV4 || Style != FightStyle.Fists || state != State.Idle) return false;
            if (PickTarget(side, AloneRange) != null || PickTarget(-side, 2.5f) != null) return false;
            if (NearCrate(side) != null || OilDrum.Near(transform.position, side) != null || PickDowned(side) != null) return false;
            SetFacing(side);
            stepGoal = Mathf.Clamp(transform.position.x + side * 0.45f, arenaMinX, arenaMaxX);
            return true;
        }

        /// <summary>A tap on a thing: walk to it, then use it.</summary>
        void InteractV4(Component thing)
        {
            if (!MovesV4 || thing == null || state != State.Idle) return;
            float tx = thing.transform.position.x;
            if (thing is WeaponPickup w0) { w0.GripNearest(transform.position.x); tx = w0.GripX; }    // to the end his hand will close on
            int side = tx >= transform.position.x ? 1 : -1;
            interactWith = thing;
            SetFacing(side);
            float stand = thing is OilDrum ? 0.55f : thing is SetPiece sp0 ? sp0.stand : thing is WeaponPickup ? PickReach : 0.32f;
            stepGoal = Mathf.Clamp(tx - side * stand, arenaMinX, arenaMaxX);
            if (Mathf.Abs(stepGoal - transform.position.x) < 0.06f) { stepGoal = float.NaN; ArriveAtThing(); }
        }

        void ArriveAtThing()
        {
            var thing = interactWith;
            interactWith = null;
            if (thing == null) return;
            FaceTowards(thing.transform.position.x);
            if (thing is WeaponPickup wp) { if (CanTake(wp)) PickUpV4(wp, true); }
            else if (thing is Throwable tp) { if (carried == null) PickProp(tp); }
            else if (thing is SetPiece sp) sp.Use(this);
            else if (thing is OilDrum) BeginTraverse("push", 0f);
        }

        /// <summary>Walking over a weapon with the new body: he bends and picks it up.</summary>
        // picking a weapon up (docs/design/pipe.md, sword.md): his hand goes to its grip end, the pipe and the
        // katana each with their own way of taking it (rod_pick_up: the heave onto the shoulder; katana_pick_up:
        // one knee, the blade rising into chudan), and at a run a scoop on the stride that never breaks it
        const float PickReach = 0.42f;          // root → the hand on the floor at the grab (every pick-up clip)
        float pickFrom, pickTo, pickHitAt;
        bool pickScoop;

        static bool PickClip(string c) => c == "pick_up" || c == "rod_pick_up" || c == "katana_pick_up" || c == "rod_scoop" || c == "katana_scoop";

        /// <summary>Walking over (or tapped and walked to) a weapon he can take: start picking it up. On the
        /// run, going its way, he scoops it without stopping (started early enough for the hand to meet it).</summary>
        bool PickUpV4(WeaponPickup p, bool asked)
        {
            if (!MovesV4 || p == null) return false;
            if (state != State.Idle || traverse != null || !Grounded) return false;
            float x = transform.position.x;
            p.GripNearest(x);
            float gx = p.GripX, dx = gx - x;
            string pre = p.kind == FightStyle.Pipe ? "rod_" : p.kind == FightStyle.Sword ? "katana_" : null;
            float sp = Mathf.Abs(velocity.x);
            bool scoop = false;
            bool fast = !asked && running && sp > 2.6f;
            if (fast && dx * velocity.x <= 0f) return false;              // running on past it: let it go
            if (fast && pre != null && Contact && body.Has(pre + "scoop"))
            {
                float ahead = Mathf.Abs(dx);
                float hitIn = 5f / 30f;                                   // the scoops' grab frame
                if (ahead > sp * hitIn + PickReach + 0.05f) return false; // not yet
                if (ahead < 0.2f) return false;                           // over it already: let it go
                scoop = true;
            }
            else if (Mathf.Abs(dx) > (asked ? 1.2f : 0.9f)) return false; // (a knife, a gun at a run: he stops for it)
            // holding a bent pipe: it's tossed aside (a clang), the fresh one taken
            if (SwapsPipe(p)) TossPipe();
            if (Mathf.Abs(dx) > 0.03f) SetFacing(dx > 0f ? 1 : -1);
            pickingUp = p;
            pickFrom = x;
            pickTo = Mathf.Clamp(gx - Facing * PickReach, arenaMinX, arenaMaxX);
            string clip = pre != null && Contact && body.Has(pre + (scoop ? "scoop" : "pick_up")) ? pre + (scoop ? "scoop" : "pick_up") : "pick_up";
            BeginTraverse(clip, 0f);
            pickScoop = scoop;
            pickHitAt = body.Current != null && body.Current.hitTime > 0f ? body.Current.hitTime : 0.35f;
            if (scoop) velocity.x = Facing * Mathf.Max(3.4f, sp);
            return true;
        }

        /// <summary>The bent pipe out of his hands: thrown down behind him, it clangs on the floor and lies there.</summary>
        void TossPipe() => TossWeapon(-Facing * 1.6f);

        /// <summary>The weapon in his hands let go of: it falls (thrown with `vx`), clangs and lies on the floor.</summary>
        void TossWeapon(float vx)
        {
            if (Style == FightStyle.Fists) return;
            var held = Style == FightStyle.Sword ? sword : Style == FightStyle.Pipe ? pipe : Style == FightStyle.Knife ? knife : pistol;
            var hand = Throwable.FindBone(body.transform, "hand.R");
            Vector3 at = hand != null ? hand.position : transform.position + Vector3.up * 0.9f;
            Material mat = WaveDirector.Instance != null ? WaveDirector.Instance.weaponMaterial : null;
            if (mat == null && held != null) { var r = held.GetComponentInChildren<Renderer>(); if (r != null) mat = r.sharedMaterial; }
            int uses = Style == FightStyle.Sword ? SwordUses : Style == FightStyle.Pipe ? PipeUses : Style == FightStyle.Knife ? Knives : Ammo;
            WeaponPickup.Spawn(Style, uses, new Vector3(at.x, transform.position.y, 0f), mat, true, vx);
            Style = FightStyle.Fists;
            ApplyStyleVisuals();
            if (state == State.Idle) body.Play(IdleClip, 0.25f);
            Sound.Play(Sfx.Whoosh, 0.4f, at);
        }

        /// <summary>The knife away (into his belt): chapter 1's hall is fought bare-handed.</summary>
        public void PutAwayKnife()
        {
            if (Style != FightStyle.Knife) return;
            Knives = 0;
            Style = FightStyle.Fists;
            ApplyStyleVisuals();
            if (state == State.Idle) body.Play(IdleClip, 0.2f);
        }

        /// <summary>The pick-up test (chapter 1): the weapon drops out of his hands, so it can be taken again.</summary>
        public void PutDownForTest()
        {
            if (state != State.Idle || traverse != null || !Grounded) return;
            TossWeapon(Facing * 0.7f);
        }

        /// <summary>While a pick-up plays: standing, he settles into place for the hand to meet the grip
        /// (inside the step the clip already has); scooping, the run carries him, paced so the hand meets
        /// it on the grab frame. True if it ended (the scoop back into the run).</summary>
        bool PickUpdate(MoveSet.Entry e)
        {
            if (!PickClip(traverse)) return false;
            if (pickScoop)
            {
                float spd = Mathf.Max(3.4f, Mathf.Abs(velocity.x));
                float left = pickHitAt - body.Time;
                if (!traverseHit && left > 0.02f) spd = Mathf.Clamp(Mathf.Abs(pickTo - transform.position.x) / left, 2.4f, 5.6f);
                velocity.x = Facing * spd;
                if (body.Finished || stateTime > e.Length)
                {
                    // out of the scoop straight into the armed run (still held: it runs on; let go: he stops)
                    traverse = null; pickingUp = null; pickScoop = false;
                    state = State.Idle; stateTime = 0f; running = true; holdTime = 0.5f;
                    string run = KatanaMode ? (RodMode ? "rod_run" : "katana_run") : "run";
                    body.Play(body.Has(run) ? run : "run", 0.08f);
                    return true;
                }
                return false;
            }
            if (!traverseHit)
            {
                float k = Mathf.Clamp01(stateTime / Mathf.Max(0.05f, pickHitAt));
                k = k * k * (3f - 2f * k);
                var p = transform.position;
                p.x = Mathf.Lerp(pickFrom, pickTo, k);
                transform.position = p;
            }
            return false;
        }

        // ------------------------------------------------------------------ jumps and the vault
        /// <summary>Before a plain jump: a drum in reach is vaulted; running, the jump carries.</summary>
        bool JumpV4(int dir)
        {
            if (!MovesV4 || !Grounded) return false;
            bool aimed = dir != 0;                                     // a jump that way (a diagonal swipe)
            if (dir == 0) dir = Mathf.Abs(velocity.x) > 0.5f ? (velocity.x > 0 ? 1 : -1) : Facing;
            var drum = DrumAhead(dir);
            if (drum != null)
            {
                SetFacing(dir);
                running = false;
                float start = drum.transform.position.x - dir * 0.55f;
                BeginTraverse("vault", start);
                Sound.Play(Sfx.Cloth, 1f, transform.position);
                return true;
            }
            bool fast = Mathf.Abs(velocity.x) > 2.6f && Mathf.Sign(velocity.x) == dir;
            if (!fast && !aimed) return false;                         // straight up: the plain jump
            state = State.Air;
            stateTime = 0f;
            SetFacing(dir);
            velocity.y = fast ? ContactJump * 0.85f : ContactJump;
            velocity.x = fast ? velocity.x : dir * 2.6f;              // a standing jump that way carries 2.6 m/s
            Grounded = false;
            airStrikeOn = false;
            body.Play(ArmedJump("jump_run"), 0.06f, true, fast ? 1f : 0.9f);
            Sound.Play(Sfx.Footstep, 1.4f, transform.position);
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
            return true;
        }

        OilDrum DrumAhead(int dir)
        {
            foreach (var d in OilDrum.All)
            {
                if (d == null || !d.Available || d.Rolling) continue;
                float dx = (d.transform.position.x - transform.position.x) * dir;
                if (dx < 0.2f || dx > 1.15f) continue;
                if (Mathf.Abs(d.FloorY - transform.position.y) > 0.3f) continue;
                if (d.transform.position.y - d.FloorY > OilDrum.Height * 0.4f) return d;      // standing upright
            }
            return null;
        }

        /// <summary>A jump that way (a swipe up and to the side).</summary>
        void JumpTowards(int dir)
        {
            if (!MovesV4) { SetFacing(dir); Jump(); return; }
            if (state != State.Idle) return;
            JumpV4(dir);
        }

        // ------------------------------------------------------------------ traversals (one clip, its own travel)
        void BeginTraverse(string clip, float startX)
        {
            farMove = null;
            farSlideUntil = 0f;
            traverse = clip;
            traverseHit = false;
            traverseFrom = transform.position.x;
            traverseTo = clip == "vault" ? startX : transform.position.x;
            state = State.Traverse;
            stateTime = 0f;
            pickScoop = false;
            if (clip != "run_turn" && !clip.EndsWith("_scoop")) velocity.x = 0f;
            SetTrails(false);
            body.Play(clip, clip == "run_turn" || clip == "run_stop" ? 0.08f : 0.12f, true, 1f);
        }

        void UpdateTraverse(float dt)
        {
            // the safety net: a one-clip move whose clip was replaced by a loop (or never ends) can't hold him
            if (traverse != "advance" && traverse != "rush" && body.Current != null && (body.Current.loop || stateTime > body.Current.Length + 1.5f))
            { EndTraverse(); return; }
            if (FarTraverseUpdate(dt)) return;
            var e = body.Current;
            if (e == null) { EndTraverse(); return; }
            // the vault: slide into place for the hands to land on the lid
            if (traverse == "vault" && stateTime < 0.12f)
            {
                var p = transform.position;
                p.x = Mathf.Lerp(traverseFrom, traverseTo, stateTime / 0.12f);
                transform.position = p;
            }
            if (traverse == "run_turn")
            {
                // braking, then away the other way
                velocity.x = Mathf.MoveTowards(velocity.x, Facing * (stateTime < 0.2f ? 0f : JogSpeed), 16f * dt);
                if (body.Finished || stateTime > e.Length) { traverse = null; state = State.Idle; stateTime = 0f; running = true; holdTime = 0.5f; body.Play("run", 0.1f); }
                return;
            }
            if (PickUpdate(e)) return;
            if (!traverseHit && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                traverseHit = true;
                if (pickingUp != null)
                {
                    var p = pickingUp;
                    pickingUp = null;
                    if (p != null && CanTake(p) && Mathf.Abs(p.GripX - transform.position.x) < 1.2f) DoPickup(p);
                }
                if (traverse == "push")
                {
                    var d = OilDrum.Near(transform.position, Facing);
                    if (d != null) { d.Kick(Facing, this, 4.5f); Sound.Play(Sfx.Cloth, 1f, transform.position); }
                }
                if (traverse == "vault") Sound.Play(Sfx.Footstep, 0.8f, transform.position);
            }
            if (body.Finished) EndTraverse();
        }

        void EndTraverse()
        {
            string ended = traverse;
            var e = body.Current;
            if (e != null && Mathf.Abs(e.endShift) > 0.001f)
            {
                var p = transform.position;
                p.x = Mathf.Clamp(p.x + Facing * e.endShift, arenaMinX, arenaMaxX);
                transform.position = p;
            }
            if (traverse == "vault" || traverse == "run_stop") Sound.Play(Sfx.Land, 0.7f, transform.position);
            traverse = null;
            pickingUp = null;
            // a far move ends: keep what it needs before EnterIdle, then let it chain on
            string move = farMove;
            EnterIdle();
            farMove = move;
            FarTraverseEnd(ended);
        }
    }
}
