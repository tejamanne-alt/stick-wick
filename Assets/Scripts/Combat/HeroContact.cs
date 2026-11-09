using UnityEngine;

namespace ProjectInk
{
    /// <summary>CONTACT: the story mode's grounded, realistic fighting (docs/design/combat_v2.md).
    ///
    /// MOVEMENT  a jog, not a sprint; short, heavy jumps; no dash across the room (he steps in),
    ///           no wall runs, no flips. A swipe away is a quick step back (a slip).
    /// STRINGS   taps make named strings: P = tap high (punch), K = tap low (kick).
    ///             P P P P   jab, cross, hook, ELBOW                    "BOXER'S FOUR"
    ///             K K K     low kick, roundhouse, SIDE KICK             "KICK STRING"
    ///             P P K     jab, cross, HIGH ROUNDHOUSE                 "ONE-TWO-KICK"
    ///             K P P     low kick, lunge punch, REVERSE PUNCH        "KARATE"
    ///             P K P     jab, low kick, PALM STRIKE                  "PALM HEEL"
    ///             P P P K   jab, cross, hook, SIDE KICK                 "FOUR-PIECE"
    ///           Behind you, close: a back kick. A man out on his feet (dazed): palm to the chin /
    ///           a side kick. A man bent double: the hammer fist.
    /// PARRY     tap at a bare-handed man as he throws: his punch is knocked aside and he is
    ///           out on his feet for a moment.
    /// CLINCH    swipe at a man close by (or walk into him): hands behind his neck. Then
    ///             tap high       knee (twice), then a headbutt
    ///             tap low        leg sweep (o-soto-gari)
    ///             swipe at him   hip throw: down in front of you
    ///             swipe away     shoulder throw: over you, down behind you
    ///             swipe up       suplex (a worn-down man: body slam)
    ///             swipe down     DDT
    ///           Do nothing for a while and he shoves you off. A big man cannot be lifted until he
    ///           is worn down: he shrugs you off.
    /// RUNNING   a punch while running into a man: CLOTHESLINE. A swipe at a man 1.6 to 3 m
    ///           away: SPEAR.</summary>
    public partial class HeroController
    {
        /// <summary>Story mode: the CONTACT rules.</summary>
        public bool Contact => StoryIntro.Instance != null;
        bool ContactFists => Contact && Style == FightStyle.Fists;
        /// <summary>Hands free enough to grab, throw and parry: bare, or with a knife in one of them.</summary>
        bool ContactBare => Contact && (Style == FightStyle.Fists || Style == FightStyle.Knife);
        /// <summary>Holding a man in the clinch, waiting for the next move.</summary>
        /// <summary>The strike being thrown (enemies read a move used twice).</summary>
        public string CurrentStrikeClip => currentAttack.clip;
        public bool InClinch => state == State.Clinch && clinchMove == null && clinchFoe != null;

        const float ContactRun = 3.3f, ContactStep = 4.6f, ContactJump = 5.6f, VictimGap = 0.62f;

        string strSeq = "";              // the string so far: "P", "PK", ...
        Enemy clinchFoe;
        string clinchMove;               // the move being done in the clinch (null = holding)
        bool clinchHitDone, clinchLanded;
        int clinchKnees;
        float clinchIdle, pairedFromX, pairedToX;
        GrappleMove grapple;

        struct GrappleMove
        {
            public string clip, victim, label;
            public float damage;
            public bool throwDown, faceDown, behind;   // ends with him on the floor (face down); lands behind the hero
            public bool fromRange;                     // a spear: closes the gap on the way in
            public bool keepHold;                      // a knee: back to the clinch afterwards
            public string react;                       // a held blow: the clip he plays at the hit
        }

        static readonly GrappleMove Knee = new GrappleMove { clip = "clinch_knee", damage = 12, keepHold = true, react = "hit_body", label = "KNEE" };
        static readonly GrappleMove Headbutt = new GrappleMove { clip = "headbutt", damage = 16, keepHold = false, react = "hit_chin", label = "HEADBUTT" };
        static readonly GrappleMove Sweep = new GrappleMove { clip = "sweep", victim = "sweep_v", damage = 14, throwDown = true, label = "LEG SWEEP" };
        static readonly GrappleMove HipThrow = new GrappleMove { clip = "hip_throw", victim = "hip_throw_v", damage = 22, throwDown = true, label = "HIP THROW" };
        static readonly GrappleMove ShoulderThrow = new GrappleMove { clip = "shoulder_throw", victim = "shoulder_throw_v", damage = 26, throwDown = true, behind = true, label = "SHOULDER THROW" };
        static readonly GrappleMove Suplex = new GrappleMove { clip = "suplex", victim = "suplex_v", damage = 32, throwDown = true, behind = true, label = "SUPLEX" };
        static readonly GrappleMove BodySlam = new GrappleMove { clip = "body_slam", victim = "body_slam_v", damage = 30, throwDown = true, label = "BODY SLAM" };
        static readonly GrappleMove Ddt = new GrappleMove { clip = "ddt", victim = "ddt_v", damage = 34, throwDown = true, faceDown = true, label = "DDT" };
        static readonly GrappleMove Spear = new GrappleMove { clip = "spear", victim = "spear_v", damage = 24, throwDown = true, fromRange = true, label = "SPEAR" };
        static readonly GrappleMove ClotheslineMove = new GrappleMove { clip = "clothesline", victim = "clothesline_v", damage = 20, throwDown = true, fromRange = true, label = "CLOTHESLINE" };

        // ------------------------------------------------------------------ strings

        /// <summary>The next strike of the string the taps are making.</summary>
        AttackData ContactMove(bool kick, Enemy t, bool behindClose)
        {
            if (behindClose) { strSeq = ""; return AttackData.SBackKick; }
            if (t != null && t.Dazed)
            {
                strSeq = "";
                return kick ? AttackData.SideKick.Finisher("OUT ON HIS FEET") : AttackData.PalmStrike.Finisher("PALM TO THE CHIN");
            }
            if (t != null && !kick && t.body != null && t.body.CurrentName == "hit_body") { strSeq = ""; return AttackData.HammerFist; }

            strSeq += kick ? "K" : "P";
            AttackData m;
            switch (strSeq)
            {
                case "P": m = AttackData.SJab; break;
                case "PP": m = AttackData.SCross; break;
                case "PPP": m = AttackData.LeadHook; break;
                case "PPPP": m = AttackData.Elbow.Finisher("BOXER'S FOUR"); strSeq = ""; break;
                case "K": m = AttackData.LowKick; break;
                case "KK": m = AttackData.Mawashi; break;
                case "KKK": m = AttackData.SideKick.Finisher("KICK STRING"); strSeq = ""; break;
                case "PPK": m = AttackData.Mawashi.Finisher("ONE-TWO-KICK"); strSeq = ""; break;
                case "KP": m = AttackData.OiZuki; break;
                case "KPP": m = AttackData.GyakuZuki.Finisher("KARATE"); strSeq = ""; break;
                case "PK": m = AttackData.LowKick; break;
                case "PKP": m = AttackData.PalmStrike.Finisher("PALM HEEL"); strSeq = ""; break;
                case "PPPK": m = AttackData.SideKick.Finisher("FOUR-PIECE"); strSeq = ""; break;
                default:
                    // off the book: start a new string from this tap
                    strSeq = kick ? "K" : "P";
                    m = kick ? AttackData.MaeGeri : AttackData.BackFist;
                    break;
            }
            // the killing blow is always a clean, heavy one
            if (t != null && t.Health <= m.damage && string.IsNullOrEmpty(m.name)) m = m.Finisher(null);
            return m;
        }

        void ResetString() { strSeq = ""; }

        // ------------------------------------------------------------------ the parry

        /// <summary>A tap at a man throwing a punch, close: knock it aside.</summary>
        bool TryParry(Enemy t, int side)
        {
            if (t == null || !t.Parryable) return false;
            float dx = (t.transform.position.x - transform.position.x) * side;
            if (dx < 0f || dx > 1.8f || Mathf.Abs(t.transform.position.y - transform.position.y) > 0.6f) return false;
            SetFacing(side);
            state = State.Attack;
            stateTime = 0f;
            currentAttack = new AttackData { clip = "parry", damage = 0, reach = 1f, hitStopFrames = 3, shake = 0.06f };
            target = null;
            hitDone = true;                       // no blow of its own: it is free to cancel at once
            body.Play("parry", 0.03f, true, 1.2f);
            t.Parried(side);
            invulnerableUntil = Time.time + 0.35f;
            ResetString();
            Sound.Play(Sfx.Bone, 0.5f, t.Chest, 1.6f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.5f, 0.02f, 0.2f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(3f, 0.1f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("PARRY!");
            Tell("parry");
            return true;
        }

        // ------------------------------------------------------------------ movement

        /// <summary>A swipe in story mode: under a swing (slide), into a hold, a spear, or a step back.</summary>
        void ContactDodge(int dir)
        {
            Tell("dodge");
            pendingExecution = null;
            throwPending = false;
            Enemy swinging = null, near = null, mid = null;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 0.6f || dx < 0.1f) continue;
                if (e.Telegraphing && dx < 2.4f && (swinging == null || dx < Gap(swinging))) swinging = e;
                if (dx < 1.5f && (near == null || dx < Gap(near))) near = e;
                else if (dx < 3.0f && (mid == null || dx < Gap(mid))) mid = e;
            }
            SetTrails(false);
            if (swinging != null && Grounded && ContactBare)
            {
                // the slide under his swing (the duel's lesson)
                SetFacing(dir);
                StartAttack(AttackData.SlideKick, swinging, dir, false);
                invulnerableUntil = Time.time + 0.35f;
                return;
            }
            if (near != null && Grounded && ContactBare && TryClinch(near, dir)) return;
            if (mid != null && Grounded && ContactBare && mid.CanBeHeld && !mid.TooHeavy) { BeginGrapple(Spear, mid, dir); return; }

            // a quick step back, still facing them
            state = State.Dodge;
            stateTime = 0f;
            vaultFacing = 0;
            invulnerableUntil = Time.time + 0.32f;
            SetFacing(-dir);
            velocity.x = dir * 4.2f;
            body.Play(body.Has("slip") ? "slip" : "dodge", 0.04f, true, 1.25f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            foreach (var e in Enemy.All)
                if (e != null && e.Telegraphing && Mathf.Abs(e.transform.position.x - transform.position.x) < 2.2f)
                {
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.4f, 0.4f);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("SLIPPED IT");
                    break;
                }
        }

        float Gap(Enemy e) => Mathf.Abs(e.transform.position.x - transform.position.x);

        /// <summary>Running into a man: take hold of him.</summary>
        bool WalkIntoClinch(int dir)
        {
            if (!ContactBare || !Grounded) return false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.CanBeHeld) continue;
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (dx > 0.1f && dx < 0.8f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.5f)
                    return TryClinch(e, dir);
            }
            return false;
        }

        // ------------------------------------------------------------------ the clinch

        bool TryClinch(Enemy e, int dir)
        {
            if (e == null || !e.CanBeHeld) return false;
            SetFacing(dir);
            velocity = Vector3.zero;
            running = false;
            clinchFoe = e;
            clinchMove = null;
            clinchKnees = 0;
            clinchIdle = 0f;
            ResetString();
            state = State.Clinch;
            stateTime = 0f;
            e.BeginHeld(this);
            PinFoe();
            body.Play("clinch_enter", 0.05f, true, 1.2f);
            Sound.Play(Sfx.Cloth, 1.2f, transform.position);
            Sound.Play(Sfx.HeroEffort, 0.6f, transform.position);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(10, "CLINCH");
            Tell("clinch");
            return true;
        }

        float FoeGap => VictimGap * Mathf.Max(1f, clinchFoe != null ? clinchFoe.transform.localScale.x : 1f);

        void PinFoe()
        {
            if (clinchFoe == null) return;
            var p = transform.position;
            clinchFoe.HeldAt(new Vector3(p.x + Facing * FoeGap, p.y, clinchFoe.transform.position.z), p.x);
        }

        void UpdateClinch(float dt)
        {
            velocity.x = 0f;
            if (clinchFoe == null || (!clinchFoe.Held && clinchMove == null)) { EndClinch(); return; }
            if (clinchMove == null) UpdateHold(dt);
            else UpdateGrapple();
        }

        void UpdateHold(float dt)
        {
            PinFoe();
            if (body.CurrentName == "clinch_enter" && body.Finished) body.Play("clinch", 0.1f);
            var cmd = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
            int toward = Facing;
            switch (cmd)
            {
                case Command.StrikeLeft: case Command.StrikeRight: case Command.StrikeForward:
                    {
                        int side = cmd == Command.StrikeLeft ? -1 : cmd == Command.StrikeRight ? 1 : toward;
                        if (side != toward) { ShoveOff(); return; }
                        BeginGrapple(clinchKnees >= 2 ? Headbutt : Knee, clinchFoe, Facing);
                        return;
                    }
                case Command.KickLeft: case Command.KickRight: case Command.KickForward:
                    BeginGrapple(Sweep, clinchFoe, Facing); return;
                case Command.DodgeLeft: case Command.DodgeRight: case Command.DodgeForward:
                    {
                        int side = cmd == Command.DodgeLeft ? -1 : cmd == Command.DodgeRight ? 1 : toward;
                        var m = side == toward ? HipThrow : ShoulderThrow;
                        if (clinchFoe.TooHeavy) { TooHeavy(); return; }
                        BeginGrapple(m, clinchFoe, Facing);
                        return;
                    }
                case Command.JumpKick:
                    if (clinchFoe.TooHeavy) { TooHeavy(); return; }
                    BeginGrapple(clinchFoe.HealthFraction < 0.35f ? BodySlam : Suplex, clinchFoe, Facing);
                    return;
                case Command.ThrowWeapon:
                    if (clinchFoe.TooHeavy) { TooHeavy(); return; }
                    BeginGrapple(Ddt, clinchFoe, Facing);
                    return;
            }
            clinchIdle += dt;
            if (clinchIdle > 2.4f) BrokenOff();
        }

        /// <summary>Start a throw, a slam or a held blow, with both bodies on the same timing.</summary>
        void BeginGrapple(GrappleMove m, Enemy foe, int dir)
        {
            if (foe == null) return;
            if (state != State.Clinch)
            {
                // from range (spear, clothesline): take hold as he arrives
                SetFacing(dir);
                running = false;
                velocity = Vector3.zero;
                clinchFoe = foe;
                clinchKnees = 0;
                foe.BeginHeld(this);
                state = State.Clinch;
                ResetString();
            }
            grapple = m;
            clinchMove = m.clip;
            clinchHitDone = false;
            clinchLanded = false;
            clinchIdle = 0f;
            stateTime = 0f;
            pairedFromX = transform.position.x;
            pairedToX = foe.transform.position.x - Facing * FoeGap;
            if (!m.fromRange) PinFoe();
            else foe.HeldAt(foe.transform.position, transform.position.x);
            currentAttack = new AttackData { clip = m.clip, damage = m.damage, reach = 1f, heavy = true, hitStopFrames = m.keepHold ? 4 : 7, shake = m.keepHold ? 0.1f : 0.22f, name = m.label };
            body.Play(m.clip, 0.05f, true, 1f);
            if (!string.IsNullOrEmpty(m.victim)) foe.HeldPlay(m.victim, 0.05f);
            invulnerableUntil = m.keepHold ? invulnerableUntil : Time.time + 1.4f;      // nobody cuts in on a throw
            SwingSound();
            if (!m.keepHold) Sound.Play(Sfx.HeroEffort, 1f, transform.position);
        }

        void UpdateGrapple()
        {
            var e = body.Current;
            if (e == null || clinchFoe == null) { EndClinch(); return; }
            if (grapple.fromRange)
            {
                // close the gap in the first beat (the tackle's drive, the arm's sweep)
                float u = Mathf.Clamp01(body.Time / Mathf.Max(0.05f, e.hitTime));
                var p = transform.position;
                p.x = Mathf.Lerp(pairedFromX, pairedToX, u * u * (3f - 2f * u));
                transform.position = p;
            }
            else if (grapple.keepHold) PinFoe();

            if (!clinchHitDone && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                clinchHitDone = true;
                GrappleImpact();
            }
            if (body.Finished)
            {
                if (grapple.keepHold && clinchFoe.Alive && clinchFoe.Held)
                {
                    clinchMove = null;
                    clinchIdle = 0f;
                    body.Play("clinch", 0.12f);
                    clinchFoe.HeldPlay("clinch_v", 0.12f);
                    return;
                }
                FinishGrapple();
            }
        }

        void GrappleImpact()
        {
            var foe = clinchFoe;
            bool killing = foe.Health <= grapple.damage;
            foe.HeldDamage(grapple.damage);
            if (!string.IsNullOrEmpty(grapple.react)) foe.HeldPlay(grapple.react, 0.03f);
            if (grapple.clip == "clinch_knee") clinchKnees++;
            attackDir = Facing;
            isCounter = false;
            clinchLanded = true;
            OnStrikeLanded(foe, killing);
            Impact();
            if (grapple.throwDown)
            {
                Sound.Play(Sfx.BodyFall, 1.2f, foe.Chest);
                Sound.Play(Sfx.Bone, 0.7f, foe.Chest);
                if (InkFx.Instance != null) InkFx.Instance.Splash(foe.transform.position + Vector3.up * 0.2f, Vector3.up, killing ? 40 : 22, foe.InkColor);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.22f, 0.25f);
                // the big ones get a beat of slow motion
                if ((killing || grapple.damage >= 30f) && TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.45f, 0.02f, 0.25f);
            }
            if (WaveDirector.Instance != null && !string.IsNullOrEmpty(grapple.label))
                WaveDirector.Instance.AddStyle(grapple.throwDown ? 120 : 30, grapple.label + (grapple.throwDown ? "!" : ""));
            Tell(grapple.throwDown ? "throw" : "knee");
        }

        void FinishGrapple()
        {
            var foe = clinchFoe;
            if (foe != null)
            {
                var entry = foe.body != null ? foe.body.Current : null;
                if (grapple.throwDown) foe.LandThrown(entry != null ? entry.endShift : 0f, grapple.faceDown);
                else foe.ReleaseHeld(foe.Alive ? "stumble_back" : null);
            }
            // where the hero's own clip carried him
            var mine = body.Current;
            if (mine != null && Mathf.Abs(mine.endShift) > 0.001f)
            {
                var p = transform.position;
                p.x += Facing * mine.endShift;
                transform.position = p;
            }
            // a man thrown over him lies behind him: turn to face him
            if (grapple.behind && foe != null && foe.Alive) SetFacing(-Facing);
            clinchFoe = null;
            clinchMove = null;
            lastAttackEnd = Time.time;
            state = State.Idle;
            stateTime = 0f;
            body.Play(IdleClip, 0.12f);
        }

        /// <summary>Tap away from him in the clinch: shove him off.</summary>
        void ShoveOff()
        {
            var foe = clinchFoe;
            clinchFoe = null;
            clinchMove = null;
            state = State.Attack;
            stateTime = 0f;
            currentAttack = new AttackData { clip = "shove", damage = 0, reach = 1f };
            hitDone = true;
            body.Play("shove", 0.05f);
            if (foe != null)
            {
                foe.ReleaseHeld("shoved_v");
                Sound.Play(Sfx.Cloth, 1f, foe.Chest);
            }
        }

        /// <summary>Held too long without a move: he breaks the hold and shoves the hero back.</summary>
        void BrokenOff()
        {
            var foe = clinchFoe;
            clinchFoe = null;
            clinchMove = null;
            if (foe != null) { foe.ReleaseHeld("shove"); }
            state = State.Hurt;
            stateTime = 0f;
            velocity.x = -Facing * 3f;
            body.Play(body.Has("block_hit") ? "block_hit" : "hit_front", 0.05f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
        }

        /// <summary>A big man will not be lifted until he is worn down.</summary>
        void TooHeavy()
        {
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "TOO HEAVY: WEAR HIM DOWN");
            Sound.Play(Sfx.HeroEffort, 1f, transform.position);
            BrokenOff();
        }

        /// <summary>Let go (hit by someone else, a cutscene): he goes back to the fight.</summary>
        void EndClinch()
        {
            if (clinchFoe != null)
            {
                if (clinchMove != null && grapple.throwDown && clinchHitDone)
                    clinchFoe.LandThrown(clinchFoe.body != null && clinchFoe.body.Current != null ? clinchFoe.body.Current.endShift : 0f, grapple.faceDown);
                else clinchFoe.ReleaseHeld();
            }
            clinchFoe = null;
            clinchMove = null;
            if (state == State.Clinch) EnterIdle();
        }

        /// <summary>A punch while running into a man: the clothesline.</summary>
        bool TryClothesline(Enemy t, int side)
        {
            if (!ContactBare || t == null || !t.CanBeHeld || t.TooHeavy) return false;
            if (Mathf.Abs(velocity.x) < 2.4f || Mathf.Sign(velocity.x) != side) return false;
            float dx = (t.transform.position.x - transform.position.x) * side;
            if (dx < 0.4f || dx > 1.7f) return false;
            BeginGrapple(ClotheslineMove, t, side);
            return true;
        }
    }
}
