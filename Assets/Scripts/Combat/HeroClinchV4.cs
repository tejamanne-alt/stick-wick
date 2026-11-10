using UnityEngine;

namespace ProjectInk
{
    /// <summary>The clinch on the V4 body (chapter 1): more moves, and combos, with every move keyed as a
    /// pair (the hero's clip and the held man's, the same length; tools/blender/v4_clinch.py).
    ///
    /// HOLDING HIM          the gesture                      the move
    ///   tap high             (his side)                       knee to the body
    ///   tap low                                                knee to the thigh (a dead leg)
    ///   hold high                                              guillotine: squeeze while you hold, let go to stop
    ///   hold low                                               body lock: lifted off his feet; then swipe
    ///                                                            towards = slam in front · away = suplex behind · down = dropped on his knees
    ///   swipe towards him                                      o-soto-gari: driven back, the leg reaped, slammed down in front
    ///   swipe away, high / low                                 shoulder throw / hip throw: over you, he lands behind you
    ///   swipe up                                               suplex (worn down: body slam)
    ///   swipe down                                             DDT
    ///   tap behind you                                         shove him off
    /// COMBOS (each tap counts from halfway through the move before; about a second between them)
    ///   high high high       THE PLUM: knee, knee, the head dragged down onto a knee to the face
    ///   low low              DEAD LEG: the hurt leg swept out
    ///   low high             KNEE, HEADBUTT: his hands drop to the leg, the forehead goes in
    ///   high, swipe down     BENT OVER: the DDT, harder
    ///   high high, swipe up  KNEES TO SUPLEX: winded, he goes up (even a big man)
    ///   high, hold high      KNEE INTO GUILLOTINE: bent over, he goes out fast
    ///   low, swipe away high OFF BALANCE: the shoulder throw works on a big man
    ///   shove, tap low       SHOVE, PUSH KICK
    /// RULES  a big man can't be lifted or thrown until he's worn down (except as above) · wait and he
    ///        fights the hold, then breaks it · a choke takes time and the others can hit you meanwhile</summary>
    public partial class HeroController
    {
        bool NewClinch => body != null && body.Has("plum_knee");

        static readonly GrappleMove KneeV4 = new GrappleMove { clip = "clinch_knee", victim = "clinch_knee_v", damage = 12, keepHold = true, label = "KNEE" };
        static readonly GrappleMove ThighKnee = new GrappleMove { clip = "thigh_knee", victim = "thigh_knee_v", damage = 9, keepHold = true, label = "DEAD LEG" };
        static readonly GrappleMove Plum = new GrappleMove { clip = "plum_knee", victim = "plum_knee_v", damage = 28, throwDown = true, label = "THE PLUM" };
        static readonly GrappleMove HeadbuttV4 = new GrappleMove { clip = "headbutt", victim = "headbutt_v", damage = 18, daze = 1.2f, label = "KNEE, HEADBUTT" };
        static readonly GrappleMove SweepV4 = new GrappleMove { clip = "sweep", victim = "sweep_v", damage = 16, throwDown = true, label = "DEAD LEG SWEEP" };
        static readonly GrappleMove Reap = new GrappleMove { clip = "reap", victim = "reap_v", damage = 22, throwDown = true, label = "O-SOTO-GARI" };
        static readonly GrappleMove HipThrowV4 = new GrappleMove { clip = "hip_throw", victim = "hip_throw_v", damage = 22, throwDown = true, behind = true, label = "HIP THROW" };
        static readonly GrappleMove ShoulderThrowV4 = new GrappleMove { clip = "shoulder_throw", victim = "shoulder_throw_v", damage = 26, throwDown = true, behind = true, label = "SHOULDER THROW" };
        static readonly GrappleMove SuplexV4 = new GrappleMove { clip = "suplex", victim = "suplex_v", damage = 32, throwDown = true, behind = true, label = "SUPLEX" };
        static readonly GrappleMove BodySlamV4 = new GrappleMove { clip = "body_slam", victim = "body_slam_v", damage = 30, throwDown = true, label = "BODY SLAM" };
        static readonly GrappleMove DdtV4 = new GrappleMove { clip = "ddt", victim = "ddt_v", damage = 34, throwDown = true, faceDown = true, label = "DDT" };
        static readonly GrappleMove ChokeIn = new GrappleMove { clip = "guillotine_in", victim = "guillotine_in_v", damage = 0, keepHold = true, label = "GUILLOTINE" };
        static readonly GrappleMove ChokeDrop = new GrappleMove { clip = "guillotine_drop", victim = "guillotine_drop_v", damage = 999, throwDown = true, faceDown = true, label = "CHOKED OUT" };
        static readonly GrappleMove ChokeLet = new GrappleMove { clip = "guillotine_let", victim = "guillotine_let_v", damage = 0, daze = 1.6f, label = "" };
        static readonly GrappleMove LockLift = new GrappleMove { clip = "lock_lift", victim = "lock_lift_v", damage = 0, keepHold = true, label = "BODY LOCK" };
        static readonly GrappleMove LockSlam = new GrappleMove { clip = "lock_slam", victim = "lock_slam_v", damage = 28, throwDown = true, label = "SLAMMED" };
        static readonly GrappleMove LockSuplex = new GrappleMove { clip = "lock_suplex", victim = "lock_suplex_v", damage = 32, throwDown = true, behind = true, label = "BELLY-TO-BELLY" };
        static readonly GrappleMove LockDrop = new GrappleMove { clip = "lock_drop", victim = "lock_drop_v", damage = 10, daze = 0.9f, label = "" };

        string clinchSeq = "";
        float clinchSeqAt;
        Command clinchQueued;
        int clinchQueuedHold;
        float chokeTime, chokeTick;
        bool chokeFast, strainShown;
        bool holdArmed;              // a hold counts only once the finger has been lifted since taking hold
        Enemy shovedFoe;
        float shovedAt = -9f;

        bool SeqLive => Time.time - clinchSeqAt < 1.1f;
        bool SeqEnds(string tail) => SeqLive && clinchSeq.EndsWith(tail);

        void SeqAdd(char c)
        {
            if (!SeqLive) clinchSeq = "";
            clinchSeq += c;
            if (clinchSeq.Length > 6) clinchSeq = clinchSeq.Substring(clinchSeq.Length - 6);
            clinchSeqAt = Time.time;
        }

        GrappleMove Labelled(GrappleMove m, string label, float dmgScale = 1f)
        {
            m.label = label;
            m.damage *= dmgScale;
            return m;
        }

        // ------------------------------------------------------------------ holding him
        void UpdateHoldV4(float dt)
        {
            var gi = GestureInput.Instance;
            var cmd = gi != null ? gi.Next() : Command.None;
            if (cmd != Command.None) { HandleClinchV4(cmd, gi.LastSwipeHigh); return; }
            int hold = gi != null ? gi.HoldHeight : 0;
            if (hold == 0) holdArmed = true;
            if (hold != 0 && holdArmed && clinchIdle > 0.05f) { HoldClinchV4(hold); return; }
            clinchIdle += dt;
            if (clinchIdle > 1.5f && !strainShown)
            {
                // he fights the hold
                strainShown = true;
                body.Play("clinch_strain", 0.15f);
                clinchFoe.HeldPlay("clinch_strain_v", 0.15f);
                Sound.Play(Sfx.EnemyEffort, 0.8f, clinchFoe.Chest);
            }
            if (clinchIdle > 2.6f) { strainShown = false; BrokenOff(); }
        }

        void HoldClinchV4(int hold)
        {
            strainShown = false;
            if (hold == 1)
            {
                chokeFast = SeqEnds("H");
                SeqAdd('C');
                BeginGrapple(chokeFast ? Labelled(ChokeIn, "KNEE INTO GUILLOTINE") : ChokeIn, clinchFoe, Facing);
                chokeTime = 0f;
                chokeTick = 0f;
            }
            else
            {
                if (clinchFoe.TooHeavy) { TooHeavy(); return; }
                SeqAdd('B');
                BeginGrapple(LockLift, clinchFoe, Facing);
            }
        }

        void HandleClinchV4(Command cmd, bool swipeHigh)
        {
            strainShown = false;
            int toward = Facing;
            switch (cmd)
            {
                case Command.StrikeLeft: case Command.StrikeRight: case Command.StrikeForward:
                    {
                        int side = cmd == Command.StrikeLeft ? -1 : cmd == Command.StrikeRight ? 1 : toward;
                        if (side != toward) { SeqAdd('S'); ShoveOff(); return; }
                        GrappleMove m;
                        if (SeqEnds("L")) m = HeadbuttV4;                                    // low, high
                        else if (SeqEnds("HH")) m = Plum;                                     // high, high, high
                        else m = KneeV4;
                        SeqAdd('H');
                        if (m.clip != KneeV4.clip) clinchSeq = "";
                        BeginGrapple(m, clinchFoe, Facing);
                        return;
                    }
                case Command.KickLeft: case Command.KickRight: case Command.KickForward:
                    {
                        bool dead = SeqEnds("L");
                        SeqAdd('L');
                        if (dead) clinchSeq = "";
                        BeginGrapple(dead ? SweepV4 : ThighKnee, clinchFoe, Facing);
                        return;
                    }
                case Command.DodgeLeft: case Command.DodgeRight: case Command.DodgeForward:
                    {
                        int side = cmd == Command.DodgeLeft ? -1 : cmd == Command.DodgeRight ? 1 : toward;
                        bool offBalance = SeqEnds("L");
                        clinchSeq = "";
                        if (side == toward)
                        {
                            if (clinchFoe.TooHeavy && !offBalance) { TooHeavy(); return; }
                            BeginGrapple(Reap, clinchFoe, Facing);
                            return;
                        }
                        if (clinchFoe.TooHeavy && !offBalance) { TooHeavy(); return; }
                        var m = swipeHigh ? ShoulderThrowV4 : HipThrowV4;
                        if (offBalance) m = Labelled(m, "OFF BALANCE");
                        BeginGrapple(m, clinchFoe, Facing);
                        return;
                    }
                case Command.JumpKick:
                    {
                        bool winded = SeqEnds("HH");
                        clinchSeq = "";
                        if (clinchFoe.TooHeavy && !winded) { TooHeavy(); return; }
                        var m = clinchFoe.HealthFraction < 0.35f ? BodySlamV4 : SuplexV4;
                        if (winded) m = Labelled(m, "KNEES TO " + m.label, 1.2f);
                        BeginGrapple(m, clinchFoe, Facing);
                        return;
                    }
                case Command.ThrowWeapon:
                    {
                        bool bent = SeqEnds("H");
                        clinchSeq = "";
                        if (clinchFoe.TooHeavy && !bent) { TooHeavy(); return; }
                        BeginGrapple(bent ? Labelled(DdtV4, "BENT OVER: DDT", 1.3f) : DdtV4, clinchFoe, Facing);
                        return;
                    }
            }
        }

        /// <summary>A tap made in the second half of a knee is kept and played when it lands back in the hold.</summary>
        void RunQueuedV4()
        {
            var q = clinchQueued;
            int h = clinchQueuedHold;
            clinchQueued = Command.None;
            clinchQueuedHold = 0;
            if (clinchFoe == null || !clinchFoe.Held) return;
            var gi = GestureInput.Instance;
            if (q != Command.None) HandleClinchV4(q, gi != null && gi.LastSwipeHigh);
            else if (h != 0) HoldClinchV4(h);
        }

        // ------------------------------------------------------------------ during a move
        /// <summary>The clinch's long holds (the choke, the lift) and the input buffer. True = handled.</summary>
        bool UpdateGrappleV4()
        {
            var e = body.Current;
            var gi = GestureInput.Instance;
            float dt = Time.deltaTime;
            // the buffer: the next gesture, made in the second half of a knee
            if (grapple.keepHold && clinchMove != "guillotine" && clinchMove != "lock_hold" && e != null && body.Time >= e.Length * 0.5f &&
                clinchQueued == Command.None && clinchQueuedHold == 0 && gi != null)
            {
                clinchQueued = gi.Next();
                if (gi.HoldHeight == 0) holdArmed = true;
                if (clinchQueued == Command.None && grapple.clip == "clinch_knee" && holdArmed) clinchQueuedHold = gi.HoldHeight;
            }

            // ---- the guillotine: in, then held while the finger is
            if (clinchMove == "guillotine_in" && body.Finished)
            {
                clinchMove = "guillotine";
                body.Play("guillotine", 0.06f);
                clinchFoe.HeldPlay("guillotine_v", 0.06f);
                Sound.Play(Sfx.EnemyHurt, 0.6f, clinchFoe.Chest, 0.7f);
                return true;
            }
            if (clinchMove == "guillotine")
            {
                PinFoe();
                bool squeezing = gi != null && gi.HoldHeight == 1;
                float need = (clinchFoe.HealthFraction < 0.5f ? 1.5f : 2.6f) * (chokeFast ? 0.55f : 1f);
                if (squeezing)
                {
                    chokeTime += dt;
                    chokeTick += dt;
                    if (chokeTick > 0.45f)
                    {
                        chokeTick = 0f;
                        clinchFoe.HeldDamage(3f);
                        if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.02f, 0.08f);
                    }
                    if (chokeTime >= need)
                    {
                        // a hurt man goes out; a fresh one is only left reeling
                        if (clinchFoe.HealthFraction < 0.5f || chokeFast) BeginGrapple(ChokeDrop, clinchFoe, Facing);
                        else BeginGrapple(Labelled(ChokeLet, "CHOKED"), clinchFoe, Facing);
                    }
                }
                else BeginGrapple(ChokeLet, clinchFoe, Facing);
                return true;
            }

            // ---- the body lock: up, then held while the finger is; a swipe says where he lands
            if (clinchMove == "lock_lift" && body.Finished)
            {
                clinchMove = "lock_hold";
                stateTime = 0f;
                body.Play("lock_hold", 0.06f);
                clinchFoe.HeldPlay("lock_hold_v", 0.06f);
                return true;
            }
            if (clinchMove == "lock_hold")
            {
                PinFoe();
                var cmd = gi != null ? gi.Next() : Command.None;
                if (cmd == Command.DodgeLeft || cmd == Command.DodgeRight || cmd == Command.DodgeForward)
                {
                    int side = cmd == Command.DodgeLeft ? -1 : cmd == Command.DodgeRight ? 1 : Facing;
                    BeginGrapple(side == Facing ? LockSlam : LockSuplex, clinchFoe, Facing);
                }
                else if (cmd == Command.ThrowWeapon || gi == null || (gi.HoldHeight != 2 && stateTime > 0.3f) || stateTime > 4f)
                    BeginGrapple(LockDrop, clinchFoe, Facing);           // let go, or swiped down: dropped on his knees
                return true;
            }
            return false;
        }

        /// <summary>A low tap just after a shove: the push kick.</summary>
        bool TryShovePushKick(Command cmd)
        {
            if (!NewClinch || shovedFoe == null || Time.time - shovedAt > 0.8f) return false;
            if (cmd != Command.KickLeft && cmd != Command.KickRight && cmd != Command.KickForward) return false;
            var foe = shovedFoe;
            shovedFoe = null;
            if (!foe.Targetable) return false;
            int side = foe.transform.position.x >= transform.position.x ? 1 : -1;
            var m = AttackData.MaeGeri.Finisher("SHOVE, PUSH KICK");
            m.react = "hit_shoved";
            comboLabel = m.name;
            StartAttack(m, foe, side, false);
            return true;
        }
    }
}
