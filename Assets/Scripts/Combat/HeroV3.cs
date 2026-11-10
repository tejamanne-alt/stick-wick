using UnityEngine;

namespace ProjectInk
{
    /// <summary>Combat 3.0's hero (docs/design/combat_v3.md, step 1; chapter 1 with COMBAT: NEW).
    ///
    /// ATTACK   touch the man. Tap = a fast blow at the height you touched: HEAD jab, cross, lead hook,
    ///          rear hook (taps in rhythm chain them) · BODY a hook to the liver (a knee if he is close) ·
    ///          LEGS a low kick. Swipe through him: ACROSS the big hook (head), the roundhouse (body), the
    ///          low kick (legs) · UP the uppercut (a knee in close) · DOWN the hammer fist. Drag back from him
    ///          towards yourself and let go: the power right, as hard as you pulled. Hold on him: take hold.
    /// DEFEND   touch yourself. Hold = guard (blocks white attacks for a scratch of damage; a red one
    ///          goes through it). Swipe down = duck (under head shots), up = hop (over leg kicks), away =
    ///          step out (anything), towards him = step in (inside a long swing, not a quick jab).
    /// TIMING   defend in the last 0.2 s before his blow lands and time slows: for 0.6 s your next touch
    ///          on him is a COUNTER: parry → trap and elbow · duck → liver shot and uppercut · step out
    ///          → the counter right as he overreaches · hop → a knee · step in → an elbow.
    /// MOVE     hold the ground: walk towards your finger (further = faster). Near a man you move like a
    ///          boxer, facing him; tap the ground for one step; swipe it for a long step.</summary>
    public partial class HeroController
    {
        enum V3Def { None, Guard, Duck, Hop, StepOut, StepIn }

        bool V3 => CombatV3.On && Contact;
        V3Def v3Def;
        float v3DefAt = -9f;
        Enemy counterFoe;
        V3Def counterKind;
        float counterUntil;
        string v3Seq = "";
        float v3SeqAt;
        Enemy coilFoe;
        float coilShown;
        bool v3Next;
        AttackData v3NextMove;
        float stepGoalX = float.NaN;
        LimbStretch stretch;

        // the blows (clip, damage, knockback, hit-stop, shake, reaction); reach comes from the clip
        static AttackData A(string clip, float dmg, float kb, int stop, float shake, string react, bool heavy = false, bool down = false) =>
            new AttackData { clip = clip, damage = dmg, reach = 1f, knockback = kb, hitStopFrames = stop, shake = shake, react = react, heavy = heavy, knockdown = down };
        static readonly AttackData VJab = A("jab", 8, 1.0f, 3, 0.04f, "hit_front");
        static readonly AttackData VCross = A("cross", 12, 1.8f, 4, 0.07f, "hit_chin");
        static readonly AttackData VLeadHook = A("lead_hook", 12, 1.6f, 4, 0.08f, "hit_chin");
        static readonly AttackData VHook = A("hook", 16, 2.6f, 6, 0.12f, "hit_chin", true);
        static readonly AttackData VUppercut = A("uppercut", 16, 1.2f, 6, 0.12f, "hit_up", true);
        static readonly AttackData VBody = A("body_shot", 11, 1.0f, 4, 0.07f, "hit_body");
        static readonly AttackData VKnee = A("knee", 13, 1.5f, 5, 0.09f, "hit_body");
        static readonly AttackData VLowKick = A("low_kick", 10, 0.8f, 4, 0.08f, "hit_legs");
        static readonly AttackData VRound = A("mawashi_geri", 16, 3.5f, 6, 0.12f, "hit_body", true);
        static readonly AttackData VPush = A("mae_geri", 12, 5f, 5, 0.1f, "hit_shoved", true);
        static readonly AttackData VHammer = A("hammer_fist", 16, 0.5f, 6, 0.13f, "hit_body", true);
        static readonly AttackData VElbow = A("elbow", 18, 2.2f, 6, 0.13f, "hit_chin", true);
        // counters
        static readonly AttackData VTrapElbow = A("trap_elbow", 24, 1.5f, 8, 0.18f, "hit_down", true);
        static readonly AttackData VLiver = A("liver_shot", 14, 0.8f, 6, 0.1f, "hit_body", true);
        static readonly AttackData VCounterUpper = A("uppercut", 22, 2f, 8, 0.2f, "hit_up", true, true);
        static readonly AttackData VCounterCross = A("counter_cross", 26, 6f, 8, 0.2f, "hit_launch", true, true);
        static readonly AttackData VCounterKnee = A("knee", 20, 3f, 7, 0.16f, "hit_body", true);
        static readonly AttackData VCounterElbow = A("elbow", 22, 3f, 7, 0.16f, "hit_chin", true);

        void V3Init()
        {
            CombatV3.V3Body = body != null && body.Has("guard_hold") && body.Has("coil");
            if (CombatV3.V3Body && stretch == null) stretch = gameObject.AddComponent<LimbStretch>();
            if (stretch != null) stretch.player = body;
        }

        /// <summary>Every frame, cutscenes included: the far-limb shade (option C), the counter outline.</summary>
        void V3Always(float dt)
        {
            if (!CombatV3.V3Body) return;
            bool fighting = !CinematicLock && state != State.Dead && (StoryIntro.Instance == null || !StoryIntro.Instance.HideHud) && AnyFoeNear(7f);
            CombatV3.Shade(fighting, Time.unscaledDeltaTime);
            if (counterFoe != null)
            {
                if (Time.unscaledTime > counterUntil || !counterFoe.Alive) counterFoe = null;
                else counterFoe.Tint(Color.Lerp(new Color(0.02f, 0.02f, 0.02f), Color.white, 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 30f)), 0.04f);
            }
        }

        bool AnyFoeNear(float range)
        {
            foreach (var e in Enemy.All)
                if (e != null && e.Targetable && Mathf.Abs(e.transform.position.x - transform.position.x) < range &&
                    Mathf.Abs(e.transform.position.y - transform.position.y) < 1.2f) return true;
            return false;
        }

        Enemy NearestFoe(float range)
        {
            Enemy best = null;
            float bd = range;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable || Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) continue;
                float d = Mathf.Abs(e.transform.position.x - transform.position.x);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------------ the idle loop
        void UpdateIdleV3()
        {
            var gi = GestureInput.Instance;
            while (gi != null && gi.NextV3(out var ev))
                if (HandleV3(ev)) { running = false; return; }
            if (gi != null && gi.V3Guard && Grounded) { EnterGuardV3(); return; }
            V3Move(Axis);
        }

        void V3Move(float a)
        {
            var foe = NearestFoe(3.2f);
            if (!float.IsNaN(stepGoalX))
            {
                float dxg = stepGoalX - transform.position.x;
                if (Mathf.Abs(dxg) < 0.04f || Mathf.Abs(a) > 0.05f) stepGoalX = float.NaN;
                else a = Mathf.Sign(dxg) * 0.4f;
            }
            if (Mathf.Abs(a) > 0.05f && Grounded)
            {
                if (foe != null && Mathf.Abs(a) < 0.9f)
                {
                    // near a man: the boxer's step, still facing him
                    FaceTowards(foe.transform.position.x);
                    float v = a * 1.15f;
                    velocity.x = v;
                    string clip = Mathf.Sign(v) == Facing ? "shuffle_fwd" : "shuffle_back";
                    float spd = Mathf.Clamp(Mathf.Abs(v) / 0.45f, 0.6f, 2.8f);
                    if (body.CurrentName != clip) body.Play(clip, 0.1f, true, spd);
                    else body.SetSpeed(spd);
                    if (body.Finished) body.Play(clip, 0.05f, true, spd);
                }
                else
                {
                    SetFacing(a > 0 ? 1 : -1);
                    velocity.x = a * ContactRun;
                    if (body.CurrentName != "run") body.Play("run", 0.12f, false, Mathf.Lerp(0.8f, 1.5f, Mathf.Abs(a)));
                    else body.SetSpeed(Mathf.Lerp(0.8f, 1.5f, Mathf.Abs(a)));
                }
                running = true;
            }
            else if (running)
            {
                running = false;
                velocity.x = 0f;
                body.Play(IdleClip, 0.15f);
            }
        }

        /// <summary>One touch, in the idle state or as a follow-up. True when he starts something.</summary>
        bool HandleV3(V3Event ev)
        {
            switch (ev.kind)
            {
                case V3Kind.Tap:
                case V3Kind.Swipe:
                    {
                        var t = ev.enemy != null && ev.enemy.Targetable ? ev.enemy : PickTarget(Facing, targetRange) ?? PickTarget(-Facing, targetRange);
                        if (TryCounter(t)) return true;
                        V3Strike(t, ev);
                        return true;
                    }
                case V3Kind.PullStart:
                    {
                        var t = ev.enemy != null ? ev.enemy : NearestFoe(6f);
                        if (TryCounter(t)) return true;
                        EnterCoilV3(t);
                        return true;
                    }
                case V3Kind.PullRelease: return false;     // a release with no wind-up on: nothing
                case V3Kind.Grab:
                    {
                        var t = ev.enemy != null ? ev.enemy : NearestFoe(1.6f);
                        if (TryCounter(t)) return true;
                        if (t == null) return false;
                        int side = t.transform.position.x >= transform.position.x ? 1 : -1;
                        if (Mathf.Abs(t.transform.position.x - transform.position.x) > 1.5f)
                        {
                            stepGoalX = t.transform.position.x - side * 0.8f;     // walk in to take hold
                            return false;
                        }
                        return TryClinch(t, side);
                    }
                case V3Kind.Duck: EvadeV3(V3Def.Duck, "duck", 0f); return true;
                case V3Kind.Hop: EvadeV3(V3Def.Hop, "hop", 0f); return true;
                case V3Kind.StepOut: EvadeV3(V3Def.StepOut, "step_out", 0f); return true;
                case V3Kind.StepIn: EvadeV3(V3Def.StepIn, "step_in", 0f); return true;
                case V3Kind.GroundTap:
                    stepGoalX = Mathf.Clamp(ev.worldX, transform.position.x - 0.6f, transform.position.x + 0.6f);
                    stepGoalX = Mathf.Clamp(stepGoalX, arenaMinX, arenaMaxX);
                    return false;
                case V3Kind.GroundSwipe:
                    if (ev.dir.y > 0.7f) { Jump(); return true; }
                    {
                        int dir = ev.dir.x >= 0f ? 1 : -1;
                        var foe = NearestFoe(4f);
                        if (foe != null) FaceTowards(foe.transform.position.x);
                        EvadeV3(dir == Facing ? V3Def.StepIn : V3Def.StepOut, dir == Facing ? "step_in" : "step_out", 0f);
                        return true;
                    }
            }
            return false;
        }

        // ------------------------------------------------------------------ attacking
        void V3Strike(Enemy t, V3Event ev)
        {
            int side = t != null ? (t.transform.position.x >= transform.position.x ? 1 : -1) : Facing;
            SetFacing(side);
            if (Time.time - v3SeqAt > 0.85f) v3Seq = "";
            v3SeqAt = Time.time;
            float gap = t != null ? Mathf.Abs(t.transform.position.x - transform.position.x) : 9f;
            bool close = gap < 0.72f;
            bool bent = t != null && t.body != null && t.body.CurrentName == "hit_body";
            AttackData m;
            string label = null;
            if (ev.kind == V3Kind.Swipe)
            {
                if (ev.dir.y > 0.65f) m = close ? VKnee : VUppercut;
                else if (ev.dir.y < -0.65f) m = VHammer;
                else m = ev.height == 0 ? VHook : ev.height == 1 ? VRound : VLowKick;
                v3Seq = "";
            }
            else if (t != null && t.Dazed && ev.height == 0) { m = VCross; label = "OUT ON HIS FEET"; v3Seq = ""; }
            else if (bent && ev.height == 0) { m = VUppercut; label = "DOUBLED OVER"; v3Seq = ""; }
            else if (ev.height == 2) { m = VLowKick; v3Seq += "L"; }
            else if (ev.height == 1) { m = close ? VKnee : VBody; v3Seq += "B"; }
            else
            {
                v3Seq += "H";
                int n = 0;
                for (int i = v3Seq.Length - 1; i >= 0 && v3Seq[i] == 'H'; i--) n++;
                m = n == 1 ? VJab : n == 2 ? VCross : n == 3 ? VLeadHook : VHook;
                if (n >= 4) { label = "BOXER'S FOUR"; v3Seq = ""; }
                if (v3Seq.EndsWith("BH") && n == 1) { m = VCross; label = "LOW-HIGH"; }        // to the body, then up top
                if (v3Seq.EndsWith("LH") && n == 1) { m = VCross; }
            }
            if (v3Seq.Length > 6) v3Seq = v3Seq.Substring(v3Seq.Length - 6);
            if (t != null && t.Health <= m.damage && string.IsNullOrEmpty(label)) label = "";   // the killing blow: clean and heavy
            if (label != null) m = m.Finisher(label.Length > 0 ? label : null);
            comboLabel = string.IsNullOrEmpty(m.name) ? null : m.name;
            StartAttack(m, t, side, false);
        }

        // ------------------------------------------------------------------ the pull and release
        void EnterCoilV3(Enemy t)
        {
            if (t != null) FaceTowards(t.transform.position.x);
            coilFoe = t;
            state = State.Coil;
            stateTime = 0f;
            velocity.x = 0f;
            coilShown = 0f;
            body.Play("coil", 0.08f, true, 0f);
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
        }

        void UpdateCoilV3()
        {
            var gi = GestureInput.Instance;
            float want = gi != null ? gi.V3Pull : 0f;
            // the body follows the finger, a little behind it (muscles, not a slider)
            coilShown = Mathf.MoveTowards(coilShown, want, Time.deltaTime * 3.2f);
            var e = body.Current;
            if (e != null && body.CurrentName == "coil") body.SetTime(coilShown * e.Length);
            if (coilFoe != null && coilFoe.Targetable) FaceTowards(coilFoe.transform.position.x);
            while (gi != null && gi.NextV3(out var ev))
            {
                if (ev.kind == V3Kind.PullRelease) { ReleaseCoilV3(Mathf.Max(coilShown, ev.power * 0.9f)); return; }
                if (ev.kind == V3Kind.PullCancel) { EnterIdle(); return; }
                if (ev.kind == V3Kind.Duck || ev.kind == V3Kind.Hop || ev.kind == V3Kind.StepOut || ev.kind == V3Kind.StepIn) { HandleV3(ev); return; }
            }
            if (gi == null || (!gi.V3Pulling && stateTime > 0.3f)) { ReleaseCoilV3(coilShown); return; }
            if (stateTime > 2.5f) ReleaseCoilV3(coilShown);       // he can't hold it for ever
        }

        void ReleaseCoilV3(float power)
        {
            var t = coilFoe != null && coilFoe.Targetable ? coilFoe : NearestFoe(6f);
            int side = t != null ? (t.transform.position.x >= transform.position.x ? 1 : -1) : Facing;
            if (power < 0.25f) { StartAttack(VCross, t, side, false); return; }
            var m = A("power_cross", 12f + 26f * power, 2f + 7f * power, 5 + Mathf.RoundToInt(5 * power), 0.08f + 0.16f * power, "hit_launch", true, power > 0.85f);
            if (power > 0.85f) { m.name = "FULL POWER"; comboLabel = m.name; }
            StartAttack(m, t, side, false);
            if (power > 0.6f && CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(3f + 4f * power, 0.15f);
        }

        // ------------------------------------------------------------------ defence
        void EnterGuardV3()
        {
            var foe = NearestFoe(4f);
            if (foe != null) FaceTowards(foe.transform.position.x);
            state = State.Guard;
            stateTime = 0f;
            velocity.x = 0f;
            running = false;
            v3Def = V3Def.Guard;
            v3DefAt = Time.time;
            body.Play("guard_up", 0.05f);
            Sound.Play(Sfx.Cloth, 0.7f, transform.position);
            Tell("guard");
        }

        void UpdateGuardV3()
        {
            var gi = GestureInput.Instance;
            var foe = NearestFoe(4f);
            if (foe != null) FaceTowards(foe.transform.position.x);
            if (body.Finished && (body.CurrentName == "guard_up" || body.CurrentName == "guard_hit" || body.CurrentName == "parry"))
                body.Play("guard_hold", 0.08f);
            // a second finger can still strike or evade while the guard is up: the guard drops for it
            while (gi != null && gi.NextV3(out var ev))
                if (ev.kind != V3Kind.PullRelease && HandleV3(ev)) { if (state != State.Guard) v3Def = state == State.Dodge ? v3Def : V3Def.None; return; }
            if (gi == null || !gi.V3Guard) { v3Def = V3Def.None; EnterIdle(); }
        }

        void EvadeV3(V3Def kind, string clip, float _)
        {
            var foe = NearestFoe(4f);
            if (foe != null) FaceTowards(foe.transform.position.x);
            state = State.Dodge;
            stateTime = 0f;
            velocity.x = 0f;
            vaultFacing = 0;
            v3Def = kind;
            v3DefAt = Time.time;
            SetTrails(false);
            body.Play(clip, 0.03f, true, 1f);
            Sound.Play(Sfx.Whoosh, 0.7f, transform.position, kind == V3Def.Duck ? 0.8f : 0.95f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            Tell("dodge");
        }

        void UpdateDodgeV3()
        {
            if (body.Finished || body.Current == null)
            {
                ApplyEndShift();
                v3Def = V3Def.None;
                EnterIdle();
            }
        }

        /// <summary>A clip that travels (a step, a lunge): when it ends, the man is moved to where his
        /// feet are, so nothing slides.</summary>
        void ApplyEndShift()
        {
            var e = body.Current;
            if (e == null || Mathf.Abs(e.endShift) < 0.001f) return;
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + Facing * e.endShift, arenaMinX, arenaMaxX);
            transform.position = p;
        }

        /// <summary>An enemy's blow is about to land on him (Enemy.ResolveAttack): judge the defence.
        /// True = it did not land as a hit (blocked, parried or evaded).</summary>
        public bool V3Defend(Enemy foe, AttackData atk, bool red, int height, bool longBlow)
        {
            if (!V3 || IsDead || foe == null) return false;
            bool perfect = Time.time - v3DefAt <= 0.2f;
            if (state == State.Guard)
            {
                if (red)
                {
                    // a blade, a bar, a grab, a brute's smash: through the guard
                    v3Def = V3Def.None;
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "RED: EVADE IT");
                    return false;
                }
                if (perfect && !foe.Armed)
                {
                    body.Play("parry", 0.02f, true, 1.1f);
                    foe.V3Parried(Facing);
                    Sound.Play(Sfx.Bone, 0.6f, foe.Chest, 1.6f);
                    Haptics.Parry();
                    OpenCounter(foe, V3Def.Guard, "PARRY!");
                    return true;
                }
                // blocked: a scratch, driven back a little
                Health = Mathf.Max(1f, Health - atk.damage * 0.15f);
                velocity.x = foe.Facing * Mathf.Min(2.5f, atk.knockback * 0.5f + 1f);
                body.Play("guard_hit", 0.02f);
                Sound.Play(Sfx.PunchLight, 0.7f, Chest, 0.7f);
                Sound.Play(Sfx.Cloth, 1f, Chest);
                Haptics.Block();
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.05f, 0.1f);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(2);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroDamaged(atk.damage * 0.15f);
                return true;
            }
            if (state == State.Dodge && v3Def != V3Def.None && v3Def != V3Def.Guard)
            {
                bool beats = v3Def == V3Def.StepOut || (v3Def == V3Def.Duck && height == 0) || (v3Def == V3Def.Hop && height == 2) ||
                             (v3Def == V3Def.StepIn && longBlow);
                if (!beats) return false;
                if (perfect)
                {
                    if (v3Def == V3Def.StepOut) foe.V3Whiff();
                    Haptics.Parry();
                    OpenCounter(foe, v3Def, v3Def == V3Def.Duck ? "UNDER!" : v3Def == V3Def.Hop ? "OVER!" : v3Def == V3Def.StepOut ? "WHIFF!" : "INSIDE!");
                }
                else Sound.Play(Sfx.Whoosh, 0.5f, foe.Chest, 1.2f);
                return true;
            }
            return false;
        }

        void OpenCounter(Enemy foe, V3Def kind, string label)
        {
            counterFoe = foe;
            counterKind = kind;
            counterUntil = Time.unscaledTime + 0.75f;
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.33f, 0.6f, 0.02f, 0.15f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(3f, 0.12f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge(label);
            Tell("parry");
        }

        /// <summary>In the counter window, a touch on that man is the counter.</summary>
        bool TryCounter(Enemy t)
        {
            if (counterFoe == null || Time.unscaledTime > counterUntil) return false;
            if (t != null && t != counterFoe) return false;
            var foe = counterFoe;
            counterFoe = null;
            int side = foe.transform.position.x >= transform.position.x ? 1 : -1;
            AttackData m;
            switch (counterKind)
            {
                case V3Def.Guard: m = VTrapElbow; break;
                case V3Def.Duck: m = VLiver; v3NextMove = VCounterUpper; v3Next = true; break;
                case V3Def.StepOut: m = VCounterCross; break;
                case V3Def.Hop: m = VCounterKnee; break;
                default: m = VCounterElbow; break;
            }
            m.name = "COUNTER";
            comboLabel = m.name;
            v3Def = V3Def.None;
            SetFacing(side);
            StartAttack(m, foe, side, true);
            return true;
        }

        // ------------------------------------------------------------------ the other states
        /// <summary>The next command for the states the touch grammar does not change (in the air, on
        /// a rope or bar, held): with the new controls, a touch is read as the classic gesture it is.</summary>
        Command NextCmd()
        {
            var gi = GestureInput.Instance;
            if (gi == null) return Command.None;
            if (!V3) return gi.Next();
            if (!gi.NextV3(out var ev)) return Command.None;
            int side = ev.enemy != null ? (ev.enemy.transform.position.x >= transform.position.x ? 1 : -1) : Facing;
            switch (ev.kind)
            {
                case V3Kind.Tap: case V3Kind.Grab: case V3Kind.PullRelease:
                    return ev.height == 2 ? (side < 0 ? Command.KickLeft : Command.KickRight) : (side < 0 ? Command.StrikeLeft : Command.StrikeRight);
                case V3Kind.Hop: return Command.JumpKick;
                case V3Kind.Duck: return Command.ThrowWeapon;
                case V3Kind.StepIn: return Facing < 0 ? Command.DodgeLeft : Command.DodgeRight;
                case V3Kind.StepOut: return Facing < 0 ? Command.DodgeRight : Command.DodgeLeft;
                case V3Kind.Swipe: case V3Kind.GroundSwipe:
                    if (ev.dir.y > 0.65f) return Command.JumpKick;
                    if (ev.dir.y < -0.65f) return Command.ThrowWeapon;
                    return ev.dir.x < 0f ? Command.DodgeLeft : Command.DodgeRight;
                case V3Kind.GroundTap: return ev.worldX < transform.position.x ? Command.StrikeLeft : Command.StrikeRight;
            }
            return Command.None;
        }

        /// <summary>Holding a man: tap him = knee (then the headbutt), tap his legs = the sweep, swipe
        /// towards him = hip throw, away = shoulder throw, up = suplex, down = DDT, step out = shove off.</summary>
        Command V3ClinchCmd()
        {
            var gi = GestureInput.Instance;
            if (gi == null || !gi.NextV3(out var ev)) return Command.None;
            switch (ev.kind)
            {
                case V3Kind.Tap: return ev.height == 2 ? Command.KickForward : Command.StrikeForward;
                case V3Kind.Swipe: case V3Kind.GroundSwipe:
                    if (ev.dir.y > 0.65f) return Command.JumpKick;
                    if (ev.dir.y < -0.65f) return Command.ThrowWeapon;
                    return (ev.dir.x >= 0f ? 1 : -1) == Facing ? Command.DodgeForward : (Facing > 0 ? Command.DodgeLeft : Command.DodgeRight);
                case V3Kind.Hop: return Command.JumpKick;
                case V3Kind.Duck: return Command.ThrowWeapon;
                case V3Kind.StepOut: return Facing > 0 ? Command.StrikeLeft : Command.StrikeRight;     // shove him off
                case V3Kind.StepIn: return Command.DodgeForward;
            }
            return Command.None;
        }

        // ------------------------------------------------------------------ in the attack
        /// <summary>After the blow lands, the next touch cancels the follow-through (fluid strings).
        /// True when it started something.</summary>
        bool V3Cancel()
        {
            var e = body.Current;
            bool travels = e != null && Mathf.Abs(e.endShift) > 0.001f;
            if (v3Next && hitDone && e != null && body.Time >= e.hitTime + 0.12f)
            {
                v3Next = false;
                ApplyEndShift();
                StartAttack(v3NextMove, target, Facing, false);
                return true;
            }
            if (!CanCancel() || (travels && body.Time < e.Length * 0.8f)) return false;
            var gi = GestureInput.Instance;
            if (gi == null) return false;
            if (gi.V3Guard) { if (travels) ApplyEndShift(); EnterGuardV3(); return true; }
            while (gi.NextV3(out var ev))
            {
                if (travels) ApplyEndShift();
                if (HandleV3(ev)) return true;
            }
            return false;
        }

        /// <summary>The blow landed: the limb stretches through the impact, the phone kicks.</summary>
        void V3Landed(bool killing)
        {
            if (!CombatV3.V3Body) return;
            if (stretch != null) stretch.Hit(currentAttack.clip, currentAttack.heavy || isCounter ? 0.11f : 0.07f);
            if (!V3) return;
            if (isCounter) Haptics.Counter();
            else Haptics.Landed(currentAttack.heavy || killing);
        }
    }
}
