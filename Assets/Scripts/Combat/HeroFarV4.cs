using UnityEngine;

namespace ProjectInk
{
    /// <summary>When the men are FAR (docs/design/combat_v3.md, "far range"). On the V4 body:
    ///   TAP a man 2.2–6.5 m off        he closes in behind his guard (the advance), and strikes in reach
    ///   SWIPE TOWARDS a man 3–8 m off   the rush: explosive step-drags until he's in front of him
    ///   RUNNING, TAP high / low         (any tap counts as forward; just after letting go too, 0.35 s)
    ///                                   high: the flying knee (from 0.7 m) or the clothesline (closer)
    ///                                   low:  the baseball slide through his ankles
    ///   RUNNING FLAT OUT INTO HIM       the shoulder barge (a jog into him still takes hold)
    ///   SWIPE as a charge comes         the matador's sidestep, a hammer on his head as he passes
    ///   CROUCHED, TAP a man's back      the rear naked choke: silent, the others don't hear
    ///   CHAIN in hand: TAP              the lash (2.6 m); HOLD ON A MAN: hauled in, into the clinch
    /// Every body (all chapters):
    ///   a prop in hand (Throwable): TAP A MAN anywhere = throw it at him; within arm's reach it is
    ///   smashed on him; swipe down = throw it at the nearest
    ///   something thrown at him: TAP = the swat</summary>
    public partial class HeroController
    {
        public bool Crouching => crouching;
        public bool RunningFast => running && Mathf.Abs(velocity.x) > 2.6f && !crouching;
        public Throwable Carried => carried;

        Throwable carried, pickingProp;
        int chainUses;
        Enemy farFoe, advanceFoe;
        string farMove;
        float farSlideFrom, farSlideTo, farSlideUntil, farHitAt, advanceGoal;
        bool farHit, advanceKick, advanceLash;
        int rushLeft, advanceSide;
        float runEndAt = -10f, runEndSpeed;
        ChainLine chainLine;

        // reach at the impact (metres from where the clip starts), from the clips (tools/blender/v4_far.py)
        const float KneeReach = 1.15f, SlideReach = 2.1f, BargeReach = 0.95f, LashReach = 2.6f;

        // ------------------------------------------------------------------ every idle frame
        OpeningCue cue;

        void FarTick()
        {
            if (Contact && cue == null) { cue = gameObject.AddComponent<OpeningCue>(); cue.hero = this; }
            if (GestureInput.Instance != null) GestureInput.HoldOnArmed = carried != null && carried.kind == PropKind.Chain && MovesV4;
            // walking over a prop with empty hands: pick it up
            if (carried == null && Style == FightStyle.Fists && Grounded && state == State.Idle && traverse == null)
            {
                var p = Throwable.Near(transform.position, 0.3f);
                if (p != null) PickProp(p);
            }
        }

        void PickProp(Throwable p)
        {
            if (p == null || !p.Lying) return;
            FaceTowards(p.transform.position.x);
            if (MovesV4 && body.Has("pick_up")) { pickingProp = p; BeginTraverse("pick_up", 0f); return; }
            TakeProp(p);
        }

        void TakeProp(Throwable p)
        {
            if (p == null || !p.Lying) return;
            var hand = Throwable.FindBone(body.transform, "hand.R") ?? Throwable.FindBone(body.transform, "forearm.R") ?? body.transform;
            p.TakeInto(this, hand);
            carried = p;
            if (p.kind == PropKind.Chain)
            {
                chainUses = 8;
                if (chainLine == null) chainLine = gameObject.AddComponent<ChainLine>();
                chainLine.hero = this;
                Sound.Play(Sfx.Chain, 1f, transform.position);
            }
            else Sound.Play(Sfx.Pickup, 1f, transform.position);
            if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner(p.kind == PropKind.Chain ? "CHAIN · TAP: LASH · HOLD ON HIM: DRAG" : "TAP A MAN TO THROW", 1.6f);
            Tell("prop");
        }

        void DropCarried()
        {
            if (carried == null) return;
            var k = carried.kind;
            var at = carried.transform.position;
            Destroy(carried.gameObject);
            carried = null;
            if (k != PropKind.Chain || chainUses > 0) Throwable.Spawn(k, new Vector3(at.x, transform.position.y, 0f));
        }

        void UseUpCarried()
        {
            if (carried == null) return;
            Destroy(carried.gameObject);
            carried = null;
        }

        // ------------------------------------------------------------------ taps
        /// <summary>Before the ordinary strike: the swat, the takedown, a prop, the chain, the running
        /// attacks, the advance. True when the tap was used.</summary>
        bool FarStrike(int side, bool kick)
        {
            if (TrySwat()) return true;
            if (TryCrossfire(side)) return true;                    // two men on him: CROSSFIRE (HeroNearV4)
            if (TryTakedown(side)) return true;
            Enemy.Noise(transform.position);                         // anything else is heard

            var tapped = GestureInput.TappedEnemy;
            bool tapFresh = tapped != null && tapped.Targetable && Time.unscaledTime - GestureInput.TappedAt < 0.4f;
            if (carried != null && carried.kind != PropKind.Chain && Style == FightStyle.Fists && !kick)
            {
                var t = tapFresh ? tapped : PickTarget(side, 12f);
                if (t != null)
                {
                    GestureInput.TappedEnemy = null;
                    float d = Mathf.Abs(t.transform.position.x - transform.position.x);
                    if (d > 1.3f || Mathf.Abs(t.transform.position.y - transform.position.y) > 0.8f) ThrowProp(t);
                    else SmashProp(t);
                    return true;
                }
            }
            if (!MovesV4 || Style != FightStyle.Fists) return false;

            // the chain: lash him, or close in and lash
            if (carried != null && carried.kind == PropKind.Chain && !kick)
            {
                var t = tapFresh ? tapped : PickTarget(side, 7f, false);
                if (t != null && Mathf.Abs(t.transform.position.y - transform.position.y) < 1.0f)
                {
                    float d = Mathf.Abs(t.transform.position.x - transform.position.x);
                    if (d <= LashReach + 0.5f) { BeginFar("chain_lash", t, "lash", LashReach); return true; }
                    BeginAdvance(t, side, false, true);
                    return true;
                }
            }

            // running (or just let go): the running attacks
            if (Mathf.Abs(velocity.x) > 2.4f)
            {
                int dir = velocity.x > 0 ? 1 : -1;
                var t = PickTarget(dir, 3.6f, false);
                if (t != null && Mathf.Abs(t.transform.position.y - transform.position.y) < 0.8f)
                {
                    float d = Mathf.Abs(t.transform.position.x - transform.position.x);
                    SetFacing(dir);
                    if (kick && d >= 0.9f) { BeginFar("slide_tackle", t, "slide", SlideReach); return true; }
                    if (!kick && d >= 1.05f && d <= 2.6f) { BeginFar("flying_knee", t, "knee", KneeReach); return true; }
                    if (!kick && TryClothesline(t, dir)) return true;
                }
            }

            // a man well out of reach: close in behind the guard, then strike
            if (ContactBare)
            {
                var t = tapFresh ? tapped : PickTarget(side, 6.5f, false);
                if (t != null && Mathf.Abs(t.transform.position.y - transform.position.y) < 0.8f)
                {
                    float d = Mathf.Abs(t.transform.position.x - transform.position.x);
                    if (d > 2.2f && d < 6.5f) { BeginAdvance(t, t.transform.position.x > transform.position.x ? 1 : -1, kick, false); return true; }
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ props
        void ThrowProp(Enemy t)
        {
            if (carried == null) return;
            if (t != null) FaceTowards(t.transform.position.x);
            farFoe = t;
            string clip = body.Has("throw_object") ? "throw_object" : "throw_weapon";
            BeginFarClip(clip, "throw");
            Sound.Play(Sfx.HeroEffort, 0.9f, transform.position);
        }

        void SmashProp(Enemy t)
        {
            FaceTowards(t.transform.position.x);
            farFoe = t;
            string clip = body.Has("hook") ? "hook" : body.Has("cross") ? "cross" : "punch";
            BeginFarClip(clip, "smash");
        }

        void LaunchCarried()
        {
            if (carried == null) return;
            var k = carried.kind;
            var from = carried.transform.position;
            UseUpCarried();
            ThrownProp.Launch(k, this, farFoe, from, Facing);
            Sound.Play(Sfx.WhooshBig, 1f, from);
            Enemy.Noise(from);
        }

        /// <summary>Something thrown at him on its way in: knock it aside.</summary>
        bool TrySwat()
        {
            ThrownProp best = null;
            float bd = 3.0f;
            foreach (var p in ThrownProp.All)
            {
                if (p == null || p.swatted || !p.AtHero || p.target != this) continue;
                float d = Vector3.Distance(p.transform.position, Chest);
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null) return false;
            int from = best.transform.position.x >= transform.position.x ? 1 : -1;
            SetFacing(from);
            best.Swat(from);
            string clip = body.Has("swat") ? "swat" : body.Has("parry") ? "parry" : null;
            if (clip != null && state == State.Idle) BeginFarClip(clip, "swat");
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(120, "SWATTED!");
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 0.45f);
            Tell("swat");
            return true;
        }

        // ------------------------------------------------------------------ the far moves (one clip each)
        void BeginFarClip(string clip, string move)
        {
            BeginTraverse(clip, 0f);
            farMove = move;
            farHit = false;
            var e = body.Current;
            farHitAt = e != null && e.hitTime >= 0f ? e.hitTime : 0.25f;
        }

        /// <summary>A far move at a man: the clip, and his root slid on the way in so the blow lands
        /// on him wherever he stands (within reason).</summary>
        void BeginFar(string clip, Enemy foe, string move, float reach)
        {
            farFoe = foe;
            int dir = foe != null ? (foe.transform.position.x >= transform.position.x ? 1 : -1) : Facing;
            SetFacing(dir);
            BeginFarClip(clip, move);
            if (foe != null)
            {
                float d = Mathf.Abs(foe.transform.position.x - transform.position.x);
                farSlideFrom = transform.position.x;
                farSlideTo = Mathf.Clamp(transform.position.x + dir * Mathf.Clamp(d - reach, -0.5f, 1.4f), arenaMinX, arenaMaxX);
                farSlideUntil = Mathf.Max(0.05f, farHitAt);
            }
            if (move == "slide") invulnerableUntil = Time.time + 0.5f;
            Sound.Play(move == "slide" ? Sfx.Cloth : Sfx.HeroEffort, 1f, transform.position);
            if (move == "knee" || move == "slide" || move == "barge") Sound.Play(Sfx.WhooshBig, 0.8f, transform.position);
        }

        /// <summary>Called first in UpdateTraverse: true if this frame is handled here.</summary>
        bool FarTraverseUpdate(float dt)
        {
            var gi = GestureInput.Instance;
            // the momentum window: a tap just after letting go of the run cuts the stop short
            if (traverse == "run_stop" && Time.time - runEndAt < 0.35f && gi != null)
            {
                var cmd = gi.Next();
                if (cmd != Command.None)
                {
                    traverse = null;
                    state = State.Idle;
                    stateTime = 0f;
                    velocity.x = runEndSpeed;
                    HandleCommand(cmd);
                    if (state == State.Idle) { velocity.x = 0f; body.Play(IdleClip, 0.15f); }
                    return true;
                }
            }
            if (traverse == "advance") { UpdateAdvance(dt); return true; }
            if (farMove == null) return false;
            if (farMove == "crossfire") { UpdateCrossfire(); if (body.Finished) EndTraverse(); return true; }
            // slide the root so the hit lands on him
            if (farSlideUntil > 0f && stateTime <= farSlideUntil + dt)
            {
                var p = transform.position;
                p.x = Mathf.Lerp(farSlideFrom, farSlideTo, Mathf.Clamp01(stateTime / farSlideUntil));
                transform.position = p;
            }
            if (!farHit && body.Time >= farHitAt) { farHit = true; FarImpact(); }
            if (farMove == "drag" && farFoe != null && farFoe.Held)
            {
                // his root stays where it started; his clip carries him in
                farFoe.HeldAt(farFoe.transform.position, transform.position.x);
            }
            if (body.Finished) EndTraverse();
            return true;
        }

        /// <summary>Where the blow lands, in front of his root at the moment of impact.</summary>
        bool Connects(Enemy e, float reachFromRoot, float tol = 0.75f)
        {
            if (e == null || !e.Targetable) return false;
            if (Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) return false;
            float at = transform.position.x + Facing * reachFromRoot;
            return Mathf.Abs(e.transform.position.x - at) < tol;
        }

        void FarImpact()
        {
            var e = farFoe;
            int dir = Facing;
            switch (farMove)
            {
                case "throw": LaunchCarried(); break;
                case "smash":
                    if (carried != null && e != null && Connects(e, 0.9f, 0.6f))
                    {
                        var spec = Throwable.SpecOf(carried.kind);
                        e.TakeHit(new HitInfo { damage = spec.damage + 6f, direction = dir, knockback = 2.5f, knockdown = spec.down, heavy = true, react = spec.react, attacker = this });
                        if (spec.daze > 0f) e.Blind(spec.daze);
                        Sound.Play(spec.sound, 1f, e.Chest);
                        Sound.Play(Sfx.PunchHeavy, 1f, e.Chest);
                        if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest + Vector3.up * 0.3f, new Vector3(dir, 0.5f, 0f), 14, new Color(0.7f, 0.72f, 0.75f));
                        if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
                        if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(130, spec.label);
                        if (spec.breaks) UseUpCarried();
                    }
                    break;
                case "knee":
                    if (Connects(e, KneeReach - 0.05f))
                        Land(e, 24f, true, 3.5f, "hit_chin", "FLYING KNEE!", 180, 0.4f);
                    break;
                case "slide":
                    if (Connects(e, SlideReach - 0.1f, 0.9f))
                    {
                        e.TakeHit(new HitInfo { damage = 12f, direction = dir, knockback = 0.5f, knockdown = true, low = true, react = "hit_legs", attacker = this });
                        Sound.Play(Sfx.Kick, 1f, e.transform.position);
                        Sound.Play(Sfx.BodyFall, 1f, e.transform.position);
                        if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, "SLIDE TACKLE!");
                        if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
                    }
                    break;
                case "barge":
                    if (Connects(e, BargeReach, 0.6f))
                        Land(e, 14f, true, 6.5f, "stumble_back", "SHOULDER BARGE!", 140, 0f);
                    break;
                case "lash":
                    if (Connects(e, LashReach - 0.1f, 0.9f))
                    {
                        Land(e, 16f, false, 2.2f, "hit_front", "CHAIN!", 120, 0f);
                        Sound.Play(Sfx.Chain, 1.2f, e.Chest);
                    }
                    else Sound.Play(Sfx.Chain, 0.8f, transform.position + Vector3.right * Facing * 2f);
                    chainUses--;
                    if (chainUses <= 0) { UseUpCarried(); if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner("THE CHAIN SNAPS", 1f); }
                    break;
                case "sidestep":
                    if (e != null && e.Alive && Mathf.Abs(e.transform.position.x - transform.position.x) < 2.0f)
                    {
                        e.TakeHit(new HitInfo { damage = 12f, direction = e.Facing, knockback = 2f, heavy = true, react = "hit_back", attacker = this });
                        Sound.Play(Sfx.PunchHeavy, 1f, e.Chest);
                        if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(220, "OLÉ!");
                        if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.5f);
                    }
                    break;
                case "takedown":
                    if (e != null) { e.TakenDown(); Sound.Play(Sfx.Struggle, 0.7f, e.Chest); }
                    break;
                case "drag":
                    Sound.Play(Sfx.Chain, 1f, transform.position);
                    break;
            }
        }

        void Land(Enemy e, float dmg, bool down, float knock, string react, string label, int style, float slow)
        {
            e.TakeHit(new HitInfo { damage = dmg, direction = Facing, knockback = knock, knockdown = down, heavy = true, react = react, attacker = this });
            Sound.Play(Sfx.PunchHeavy, 1f, e.Chest);
            Sound.Play(Sfx.Bone, 0.7f, e.Chest);
            if (TimeController.Instance != null) { TimeController.Instance.HitStop(7); if (slow > 0f) TimeController.Instance.SlowMo(0.3f, slow); }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.16f, 0.18f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(style, label);
        }

        /// <summary>Called last in EndTraverse (after EnterIdle).</summary>
        void FarTraverseEnd(string ended)
        {
            string move = farMove;
            var e = farFoe;
            farMove = null;
            farFoe = null;
            farSlideUntil = 0f;
            if (ended == "pick_up" && pickingProp != null) { var p = pickingProp; pickingProp = null; TakeProp(p); }
            if (move == "rush" && e != null && e.Targetable && rushLeft > 0)
            {
                float d = Mathf.Abs(e.transform.position.x - transform.position.x);
                if (d > 1.7f) { rushLeft--; BeginFar("rush_in", e, "rush", 0f); farSlideUntil = 0f; }
            }
            if (move == "sidestep" && e != null) FaceTowards(e.transform.position.x);
            if (move == "crossfire") EndCrossfire();
            if (move == "takedown" && e != null)
            {
                float sh = e.body != null && e.body.Current != null ? e.body.Current.endShift : 0f;
                e.LandThrown(sh, false);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(250, "SILENT TAKEDOWN");
                crouching = true;
                body.Play("crouch_idle", 0.1f);
            }
            if (move == "drag" && e != null)
            {
                float sh = e.body != null && e.body.Current != null ? e.body.Current.endShift : 0f;
                e.ReleaseHeldAt(sh, 0.4f);
                if (!TryClinch(e, Facing)) EnterIdle();
            }
        }

        // ------------------------------------------------------------------ the advance
        void BeginAdvance(Enemy t, int side, bool kick, bool lash)
        {
            advanceFoe = t;
            advanceSide = side;
            advanceKick = kick;
            advanceLash = lash;
            advanceGoal = lash ? LashReach - 0.1f : 1.25f;
            SetFacing(side);
            traverse = "advance";
            traverseHit = false;
            state = State.Traverse;
            stateTime = 0f;
            velocity.x = 0f;
            body.Play("advance", 0.12f, false, 2.4f);
        }

        void UpdateAdvance(float dt)
        {
            var gi = GestureInput.Instance;
            var t = advanceFoe;
            var cmd = gi != null ? gi.Next() : Command.None;
            bool stop = t == null || !t.Targetable || stateTime > 4f || cmd != Command.None || (gi != null && Mathf.Abs(gi.MoveAxis) > 0.3f);
            float d = t != null ? Mathf.Abs(t.transform.position.x - transform.position.x) : 0f;
            if (!stop && d > advanceGoal)
            {
                if (t != null) FaceTowards(t.transform.position.x);
                var p = transform.position;
                p.x = Mathf.Clamp(p.x + Facing * 1.7f * dt, arenaMinX, arenaMaxX);
                transform.position = p;
                return;
            }
            traverse = null;
            advanceFoe = null;
            EnterIdle();
            if (cmd != Command.None) { HandleCommand(cmd); return; }
            if (stop || t == null) return;
            if (advanceLash && carried != null && carried.kind == PropKind.Chain) { BeginFar("chain_lash", t, "lash", LashReach); return; }
            Strike(Facing, advanceKick);
        }

        // ------------------------------------------------------------------ swipes
        /// <summary>A swipe towards a man 3–8 m off: the rush.</summary>
        bool TryRush(int dir)
        {
            if (!MovesV4 || !ContactBare || !Grounded || !body.Has("rush_in")) return false;
            Enemy best = null;
            float bd = 8f;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (dx < 3f || dx > bd || Mathf.Abs(e.transform.position.y - transform.position.y) > 0.6f) continue;
                bd = dx;
                best = e;
            }
            if (best == null) return false;
            rushLeft = 4;
            BeginFar("rush_in", best, "rush", 0f);
            farSlideUntil = 0f;
            Tell("rush");
            return true;
        }

        /// <summary>A swipe as a charge comes at him: off the line, and a hammer as he goes past.</summary>
        bool TrySidestep(int dir)
        {
            if (!Grounded) return false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive || !e.Charging) continue;
                float dx = e.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 4.5f || Mathf.Abs(e.transform.position.y - transform.position.y) > 0.8f) continue;
                if (e.Facing != (dx > 0 ? -1 : 1)) continue;          // not coming this way
                FaceTowards(e.transform.position.x);
                farFoe = e;
                invulnerableUntil = Time.time + 0.7f;
                if (body.Has("sidestep")) BeginFarClip("sidestep", "sidestep");
                else { state = State.Dodge; stateTime = 0f; body.Play("dodge", 0.04f, true, 1f); farFoe = null; }
                Sound.Play(Sfx.Cloth, 1f, transform.position);
                Tell("sidestep");
                return true;
            }
            return false;
        }

        /// <summary>Running flat out into a man: the shoulder barge (a jog into him still takes hold).</summary>
        bool TryBarge(int dir)
        {
            if (!MovesV4 || !ContactBare || Mathf.Abs(velocity.x) < 4.0f || !body.Has("shoulder_barge")) return false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (dx > 0.5f && dx < 1.6f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.5f)
                {
                    BeginFar("shoulder_barge", e, "barge", BargeReach);
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ the takedown and the drag
        bool TryTakedown(int side)
        {
            if (!MovesV4 || !crouching || (Style != FightStyle.Fists && Style != FightStyle.Knife) || carried != null || !body.Has("takedown_choke")) return false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.CanBeHeld || e.TooHeavy && !e.Unaware) continue;
                float dx = (e.transform.position.x - transform.position.x) * side;
                if (dx < 0.15f || dx > 1.15f || Mathf.Abs(e.transform.position.y - transform.position.y) > 0.4f) continue;
                if (e.Facing != side) continue;                      // only from behind
                SetFacing(side);
                crouching = false;
                e.BeginHeld(this);
                e.HeldAt(new Vector3(transform.position.x + side * 0.42f, e.transform.position.y, e.transform.position.z), transform.position.x + side * 10f);
                e.HeldPlay("takedown_choke_v", 0.06f);
                farFoe = e;
                invulnerableUntil = Time.time + 1.2f;
                BeginFarClip("takedown_choke", "takedown");
                Sound.Play(Sfx.Cloth, 1f, transform.position);
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.6f);
                Tell("takedown");
                return true;
            }
            return false;
        }

        /// <summary>The chain in hand and a press held on a man: haul him in, into the clinch.</summary>
        void TryChainDrag(Enemy e)
        {
            if (!MovesV4 || carried == null || carried.kind != PropKind.Chain || e == null || !e.CanBeHeld || state != State.Idle) return;
            float dx = e.transform.position.x - transform.position.x;
            float d = Mathf.Abs(dx);
            if (d > 3.4f || d < 1.0f || Mathf.Abs(e.transform.position.y - transform.position.y) > 0.6f) return;
            int side = dx > 0 ? 1 : -1;
            SetFacing(side);
            e.BeginHeld(this);
            // his clip starts him 2.42 out (the clinch's 0.62 plus the 1.8 he's hauled)
            e.HeldAt(new Vector3(transform.position.x + side * 2.42f, e.transform.position.y, e.transform.position.z), transform.position.x);
            e.HeldPlay("chain_drag_v", 0.08f);
            farFoe = e;
            BeginFarClip("chain_drag", "drag");
            chainUses -= 2;
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(160, "REELED IN");
            Tell("drag");
        }

        /// <summary>Where the chain's far end is now (for the line): hanging, whipping, or round his neck.</summary>
        public Vector3 ChainTip(Vector3 hand, out bool taut)
        {
            taut = false;
            if (traverse == "chain_drag" && farFoe != null && !farHit) { taut = true; return farFoe.Chest + Vector3.up * 0.18f; }
            if (traverse == "chain_lash")
            {
                var fa = Throwable.FindBone(body.transform, "forearm.R");
                var dirv = fa != null ? (hand - fa.position).normalized : Vector3.right * Facing;
                float u = body.Current != null ? Mathf.Clamp01(body.Time / Mathf.Max(0.01f, farHitAt)) : 1f;
                taut = u > 0.7f && body.Time < farHitAt + 0.15f;
                return hand + dirv * Mathf.Lerp(0.8f, 2.4f, u * u);
            }
            return hand + new Vector3(-Facing * 0.08f, -0.62f, 0f);
        }

        /// <summary>Hit in the middle of a far move: let go of whoever he had hold of.</summary>
        void ReleaseFarFoe()
        {
            if (farFoe != null && farFoe.Held) farFoe.ReleaseHeld();
            if (crossA != null && crossA.Held) crossA.ReleaseHeld();
            if (crossB != null && crossB.Held) crossB.ReleaseHeld();
            crossA = crossB = null;
            farFoe = null;
            farMove = null;
            advanceFoe = null;
        }

        public void NoteRunStop() { runEndAt = Time.time; runEndSpeed = velocity.x; }
    }

    /// <summary>The chain in the hero's hand, drawn as a line of links with a sag.</summary>
    public class ChainLine : MonoBehaviour
    {
        public HeroController hero;
        LineRenderer line;
        Transform hand;
        static Material mat;

        void LateUpdate()
        {
            bool on = hero != null && hero.Carried != null && hero.Carried.kind == PropKind.Chain;
            if (line == null)
            {
                line = new GameObject("Chain").AddComponent<LineRenderer>();
                if (mat == null) mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.16f, 0.16f, 0.18f) };
                line.sharedMaterial = mat;
                line.widthMultiplier = 0.035f;
                line.positionCount = 12;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            line.enabled = on;
            if (!on) return;
            if (hand == null) hand = Throwable.FindBone(hero.body.transform, "hand.R");
            var a = hand != null ? hand.position : hero.Chest;
            var b = hero.ChainTip(a, out bool taut);
            float sag = taut ? 0.03f : 0.18f;
            for (int i = 0; i < 12; i++)
            {
                float u = i / 11f;
                var p = Vector3.Lerp(a, b, u);
                p.y -= Mathf.Sin(u * Mathf.PI) * sag;
                p.z -= 0.05f;
                line.SetPosition(i, p);
            }
        }

        void OnDestroy() { if (line != null) Destroy(line.gameObject); }
    }
}
