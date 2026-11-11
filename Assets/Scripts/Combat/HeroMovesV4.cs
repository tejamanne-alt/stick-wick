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
        const float WalkSpeed = 1.1f, JogSpeed = 3.3f, RunSpeedV4 = 4.8f, SneakSpeed = 0.55f;
        const float AloneRange = 6f;

        bool MovesV4 => Contact && body != null && body.Has("sprint");
        bool crouching;
        float holdTime, idleCheckAt, traverseFrom, traverseTo;
        float stepGoal = float.NaN;
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
                    if (e != null && e.Alive && !e.Fleeing && Mathf.Abs(e.transform.position.x - transform.position.x) < AloneRange &&
                        Mathf.Abs(e.transform.position.y - transform.position.y) < 1.6f) return false;
                return true;
            }
        }

        string IdleClipV4 => MovesV4 && Style == FightStyle.Fists && Alone ? "idle_alone" : "idle";

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
                if (body.CurrentName != clip) body.Play(clip, crouching ? 0.15f : 0.2f, true, 1f);
                if (moving) body.SetSpeed(Mathf.Clamp(Mathf.Abs(velocity.x) / SneakSpeed, 0.4f, 1.4f));
                crouching = true;
                return;
            }
            if (crouching) { crouching = false; body.Play(IdleClipV4, 0.25f); }

            float a = Axis;
            int dir = Mathf.Abs(a) > 0.01f ? (a > 0 ? 1 : -1) : 0;
            // a step towards a goal (a tap with nobody near, or walking to a thing)
            if (dir == 0 && !float.IsNaN(stepGoal))
            {
                float dg = stepGoal - transform.position.x;
                if (Mathf.Abs(dg) < 0.05f) { stepGoal = float.NaN; velocity.x = 0f; ArriveAtThing(); }
                else { dir = dg > 0 ? 1 : -1; a = dir * 0.4f; }
            }
            else if (dir != 0) stepGoal = float.NaN;

            if (dir != 0 && Grounded)
            {
                bool alone = Alone;
                holdTime += dt;
                // running hard the other way: a real turn
                if (velocity.x * dir < -2.2f) { BeginTraverse("run_turn", 0f); SetFacing(dir); return; }
                if (TryLowerDown(dir)) return;                          // a raised floor's edge: lower himself down
                SetFacing(dir);
                if (Contact && WalkIntoClinch(dir)) return;
                float want = !float.IsNaN(stepGoal) ? WalkSpeed
                           : holdTime < 0.4f ? WalkSpeed : holdTime < 1.25f || !alone ? JogSpeed : RunSpeedV4;
                float accel = Mathf.Abs(velocity.x) < want ? 9f : 12f;
                velocity.x = Mathf.MoveTowards(velocity.x, dir * want, accel * dt);
                float sp = Mathf.Abs(velocity.x);
                string clip = sp < 1.7f ? "walk_alone" : sp < 4.0f ? "run" : "sprint";
                float clipSpeed = clip == "walk_alone" ? WalkSpeed : clip == "run" ? JogSpeed : RunSpeedV4;
                float play = Mathf.Clamp(sp / clipSpeed, 0.55f, 1.5f);
                if (body.CurrentName != clip) body.Play(clip, 0.15f, false, play);
                else body.SetSpeed(play);
                running = true;
                return;
            }

            // let go
            holdTime = 0f;
            if (running)
            {
                running = false;
                if (Mathf.Abs(velocity.x) > 2.4f && Grounded) { BeginTraverse("run_stop", 0f); velocity.x = 0f; return; }
                velocity.x = 0f;
                body.Play(IdleClipV4, 0.2f);
                return;
            }
            // relaxed when alone, the guard when someone comes near
            if (Time.time > idleCheckAt)
            {
                idleCheckAt = Time.time + 0.3f;
                string want = IdleClipV4;
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
            int side = tx >= transform.position.x ? 1 : -1;
            interactWith = thing;
            SetFacing(side);
            float stand = thing is OilDrum ? 0.55f : 0.32f;
            stepGoal = Mathf.Clamp(tx - side * stand, arenaMinX, arenaMaxX);
            if (Mathf.Abs(stepGoal - transform.position.x) < 0.06f) { stepGoal = float.NaN; ArriveAtThing(); }
        }

        void ArriveAtThing()
        {
            var thing = interactWith;
            interactWith = null;
            if (thing == null) return;
            FaceTowards(thing.transform.position.x);
            if (thing is WeaponPickup wp) { pickingUp = wp; BeginTraverse("pick_up", 0f); }
            else if (thing is OilDrum) BeginTraverse("push", 0f);
        }

        /// <summary>Walking over a weapon with the new body: he bends and picks it up.</summary>
        bool PickUpV4(WeaponPickup p)
        {
            if (!MovesV4) return false;
            if (state != State.Idle || traverse != null) return true;     // handled (not now)
            pickingUp = p;
            FaceTowards(p.transform.position.x);
            BeginTraverse("pick_up", 0f);
            return true;
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
            body.Play("jump_run", 0.06f, true, fast ? 1f : 0.9f);
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
            traverse = clip;
            traverseHit = false;
            traverseFrom = transform.position.x;
            traverseTo = clip == "vault" ? startX : transform.position.x;
            state = State.Traverse;
            stateTime = 0f;
            if (clip != "run_turn") velocity.x = 0f;
            SetTrails(false);
            body.Play(clip, clip == "run_turn" || clip == "run_stop" ? 0.08f : 0.12f, true, 1f);
        }

        void UpdateTraverse(float dt)
        {
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
            if (!traverseHit && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                traverseHit = true;
                if (traverse == "pick_up" && pickingUp != null)
                {
                    var p = pickingUp;
                    pickingUp = null;
                    if (p != null && Mathf.Abs(p.transform.position.x - transform.position.x) < 1.0f) DoPickup(p);
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
            EnterIdle();
        }
    }
}
