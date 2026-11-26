using UnityEngine;

namespace ProjectInk
{
    /// <summary>The pipe, v2 (docs/design/pipe.md): a heavy steel pipe in two hands. Three ideas carry it:
    ///
    ///   THE SWIPE IS THE PIPE'S PATH. Draw the line the pipe should travel, where it should land: a swipe
    ///   started in the upper part of the screen goes for the head and body, one in the lower part for the
    ///   legs (the game's one high/low line). The faster the swipe, the harder the swing.
    ///     → level           the FOREHAND: the big swing, through everyone in its arc
    ///     ← (after a swing) the BACKHAND: back from where the forehand left the pipe (the figure eight)
    ///     ↘                 the CHOP: short and quick, down across the collarbone; his guard arm drops
    ///     ↓                 the OVERHEAD: two hands, the whole back; he goes down (a man on the floor: it finds him)
    ///     ↑ ↗               the RISING swing: up under the jaw
    ///     low → / ↘         the LEG SWEEP: through his shins, he buckles
    ///     ← (nothing to return, no one behind)   the BACK-HOP: out of reach, the pipe still cocked
    ///     ← with a man behind                    turn and swing at him
    ///     tap               the BUTT-END: short, quick, into his belly (he folds); tap low: a rap on the shins
    ///     hold on the hero  WIND UP (the longer, the harder); let go: the FULL TURN, everyone round him
    ///
    ///   KEEP IT SWINGING. A pipe has weight: every swing ends somewhere, and the next one can start from
    ///   there. A swipe made as the pipe comes through (from the impact to just after the follow-through)
    ///   FLOWS: no reset, faster, harder, up to four deep (FLOW ×2, ×3, ×4: the trail burns hotter, the
    ///   whoosh climbs). A swipe made early is held and fires the moment it can. Stop, and the pipe settles
    ///   back on the shoulder. Swings that belong together pay more:
    ///     → then ←          FIGURE EIGHT        (and on: → ← → ← WINDMILL)
    ///     low sweep then ↓  SWEEP AND SMASH     (he's falling: the overhead meets him on the way down)
    ///     ↘ chop then ↑     UP AND UNDER        (his guard dropped: up under it)
    ///     tap then ↓        FOLD AND DROP       (he folds forward into it)
    ///     back-hop then →   STEP AND SWING      (out of his reach, and straight back in)
    ///
    ///   HIT THE HAND THAT HITS YOU. A pipe doesn't parry, it breaks: a swing that lands on a man while he's
    ///   winding up or swinging INTERCEPTS him: his blow is gone, he reels, it hurts more. Against swordsmen
    ///   the mirror parry still works (their red line).
    ///
    ///   It's a pipe, not a sword: 14 HITS and it gives out (misses don't wear it). Bent near the end; the
    ///   very last blow is the hardest it ever lands.</summary>
    public partial class HeroController
    {
        // the flow
        int pipeFlow;
        float pipeFlowFrom = -9f, pipeFlowUntil = -9f;
        string pipeLast;
        float pipePower = 1f, pipeComboMul = 1f, pipeEvadeAt = -9f, pipeWindAt;
        string pipeCombo;
        bool pipeBendWarned, pipeWasGroundSmash;
        // a swipe made before the pipe can take it: held until it can
        Command pipeBuffer = Command.None;
        Vector2 pbVec, pbStart, pbEnd;
        float pbSpeed;
        bool pbHigh;

        const int PipeFlowMax = 4;
        const float PipeFlowOpen = 0.1f;        // after the impact: the pipe is coming through, the next swipe can take it
        const float PipeFlowGrace = 0.22f;      // after the clip ends: the pipe settling, a swipe still catches it

        bool PipeInFlow => Time.time >= pipeFlowFrom && Time.time <= pipeFlowUntil;

        /// <summary>Picked up a pipe: the way it works, once, in a line.</summary>
        void OnPipeTaken()
        {
            pipeFlow = 0; pipeLast = null; pipeBendWarned = false; pipeFlowUntil = -9f;
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.ShowBanner("THE PIPE  ·  SWIPE WHERE IT SHOULD LAND  ·  KEEP IT SWINGING", 3.4f);
        }

        // ------------------------------------------------------------------ swipes and taps
        /// <summary>A swipe with the pipe in hand: its line picks the swing.</summary>
        void PipeSwipe(Vector2 v, float speed, bool high)
        {
            if (state != State.Idle && state != State.Traverse) return;
            lastStroke = InkStroke.Draw(GestureInput.CutStart, GestureInput.CutEnd, 0.14f, transform.position.z);
            if (TryMirrorParry(v)) return;                                    // a swordsman's line: meet it
            float ang = CutAngle(v);
            pipePower = Mathf.Clamp(speed / 3200f, 0.85f, 1.2f);
            if (Mathf.Abs(ang) > 112f)
            {
                // away: the backhand if the pipe's still coming through, a man behind gets it, else out of reach
                if (PipeInFlow && (pipeLast == "rod_swing" || pipeLast == "rod_chop" || pipeLast == "rod_rising") && body.Has("rod_backhand"))
                { PipeSwing("rod_backhand", 1.3f); return; }
                var behind = PickTarget(-Facing, 2.0f, false);
                if (behind != null) { SetFacing(-Facing); PipeSwing("rod_swing", 1.35f); return; }
                PipeEvade();
                return;
            }
            string clip;
            if (!high && ang <= 22f && ang >= -60f) clip = "rod_shin";                 // drawn low: the legs
            else if (ang < -60f) clip = "rod_overhead";
            else if (ang < -22f) clip = body.Has("rod_chop") ? "rod_chop" : "rod_overhead";
            else if (ang > 22f) clip = "rod_rising";
            else clip = "rod_swing";
            float reach = clip == "rod_overhead" ? 1.2f : clip == "rod_shin" ? 1.3f : clip == "rod_rising" || clip == "rod_chop" ? 1.2f : 1.35f;
            PipeSwing(clip, reach);
        }

        /// <summary>A tap: the butt end (high), a rap on the shins (low). Quick, cheap, it interrupts.</summary>
        void PipeTap(int side, bool low)
        {
            if (state != State.Idle && state != State.Traverse) return;
            if (side != Facing) SetFacing(side);
            pipePower = 1f;
            PipeSwing(low ? "rod_shin" : "rod_jab", low ? 1.3f : 1.2f, low ? 1.25f : 1f);
        }

        /// <summary>Start a swing: the flow and the combos read here, the clip played at the swing's speed.</summary>
        void PipeSwing(string clip, float reach, float extraSpeed = 1f)
        {
            bool flowing = PipeInFlow;
            pipeFlow = flowing ? Mathf.Min(pipeFlow + 1, PipeFlowMax) : 0;
            pipeCombo = null;
            pipeComboMul = 1f;
            if (flowing)
            {
                if (pipeLast == "rod_swing" && clip == "rod_backhand") { pipeCombo = pipeFlow >= 3 ? "WINDMILL" : "FIGURE EIGHT"; pipeComboMul = pipeFlow >= 3 ? 1.45f : 1.3f; }
                else if (pipeLast == "rod_backhand" && clip == "rod_swing" && pipeFlow >= 2) { pipeCombo = "WINDMILL"; pipeComboMul = 1.4f; }
                else if (pipeLast == "rod_shin" && clip == "rod_overhead") { pipeCombo = "SWEEP AND SMASH"; pipeComboMul = 1.5f; }
                else if (pipeLast == "rod_chop" && clip == "rod_rising") { pipeCombo = "UP AND UNDER"; pipeComboMul = 1.4f; }
                else if (pipeLast == "rod_jab" && clip == "rod_overhead") { pipeCombo = "FOLD AND DROP"; pipeComboMul = 1.4f; }
            }
            if (Time.time - pipeEvadeAt < 0.6f && clip != "rod_jab") { pipeCombo = "STEP AND SWING"; pipeComboMul = 1.3f; pipeEvadeAt = -9f; }
            // out of a run: the pipe arrives with all his weight behind it
            if (running && Mathf.Abs(velocity.x) > 2.6f) { pipeComboMul *= 1.2f; pipeCombo = pipeCombo ?? "CHARGING SWING"; }
            pipeLast = clip;
            pipeFlowUntil = -9f;                                              // closed until this one comes through

            katanaCut = clip;
            katanaMul = Time.time < kaeshiUntil ? 1.5f : 1f;                 // a parried swordsman: the return is free
            katanaLabel = Time.time < kaeshiUntil ? "KAESHI" : null;
            kaeshiUntil = 0f;
            var t = PickTarget(Facing, 3.0f, false);
            if (t != null && Mathf.Abs(t.transform.position.y - transform.position.y) > 0.8f) t = null;
            // the overhead finds a man on the floor when no one's standing in front
            pipeWasGroundSmash = false;
            if (clip == "rod_overhead" && t == null) { var d = DownedAhead(1.6f); if (d != null) { FaceTowards(d.transform.position.x); pipeWasGroundSmash = true; } }
            if (t != null) BeginFar(clip, t, "rod", reach); else BeginFarClip(clip, "rod");
            // faster swipe, faster swing; deeper in the flow, faster still (the pipe's already moving)
            float sp = (0.92f + (pipePower - 0.85f) * 0.6f) * (1f + 0.07f * pipeFlow) * extraSpeed;
            body.SetSpeed(sp);
            SetTrails(true);
            Sound.Play(Sfx.WhooshBig, clip == "rod_jab" ? 0.6f : 0.9f + 0.05f * pipeFlow, transform.position,
                       (clip == "rod_overhead" ? 0.8f : 0.95f) + 0.05f * pipeFlow);
            if (pipeFlow >= 2) Sound.Play(Sfx.HeroEffort, 0.5f, transform.position, 1.05f);
        }

        /// <summary>Out of his reach in one skip back, the pipe kept cocked; swing straight back in for STEP AND SWING.</summary>
        void PipeEvade()
        {
            if (!body.Has("rod_evade")) { BeginTraverse("katana_retreat", 0f); invulnerableUntil = Time.time + 0.3f; return; }
            BeginTraverse("rod_evade", 0f);
            invulnerableUntil = Time.time + 0.32f;
            pipeEvadeAt = Time.time;
            pipeFlow = 0;
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
            Tell("evade");
        }

        Enemy DownedAhead(float range)
        {
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive || !e.IsDown || e.Held) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 0.8f) continue;
                if (Mathf.Abs(e.transform.position.x - transform.position.x) < range) return e;
            }
            return null;
        }

        // ------------------------------------------------------------------ the wind-up and the full turn
        /// <summary>Held on himself: the pipe wound right round, the weight back. The longer, the harder.</summary>
        void PipeWind()
        {
            if (state != State.Idle) return;
            rodWound = true;
            pipeWindAt = Time.time;
            running = false;
            velocity.x = 0f;
            body.Play(body.Has("rod_coil") ? "rod_coil" : "rod_spin", 0.22f, true, body.Has("rod_coil") ? 1f : 0.001f);
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
            Sound.Play(Sfx.Creak, 0.35f, transform.position, 1.2f);
        }

        float PipeCharge => Mathf.Clamp01((Time.time - pipeWindAt - 0.15f) / 0.85f);

        /// <summary>Every idle frame while wound: the charge ticks, full is felt.</summary>
        void PipeWindTick()
        {
            if (!rodWound || state != State.Idle) return;
            float c = PipeCharge;
            if (c >= 1f && Time.time - pipeWindAt < 1.0f + Time.deltaTime * 1.5f)
            {
                // full: a spark off the pipe, a creak, a hold of the breath
                StreakFx.Sparks(transform.position + new Vector3(-Facing * 0.35f, 1.3f, 0f), Vector3.up, 6, 4f);
                Sound.Play(Sfx.Creak, 0.6f, transform.position, 0.9f);
                Sound.Play(Sfx.Glint, 0.5f, transform.position);
            }
        }

        /// <summary>Let go (dragged or not): the full turn, its direction the drag's (or the way he faces).</summary>
        void PipeRelease(Vector2 v, bool dragged)
        {
            if (!rodWound) { if (dragged) PipeSwipe(v, 3000f, true); return; }
            rodWound = false;
            if (state != State.Idle) return;                                 // knocked out of the wind-up: it's gone
            if (dragged && Mathf.Abs(v.x) > 0.2f) SetFacing(v.x >= 0f ? 1 : -1);
            if (dragged) InkStroke.Draw(GestureInput.CutStart, GestureInput.CutEnd, 0.2f, transform.position.z);
            float c = PipeCharge;
            pipeFlow = 0;
            pipeCombo = c >= 1f ? "FULL TURN" : null;
            pipeComboMul = 1f + 0.7f * c;
            pipePower = 1f;
            pipeLast = "rod_spin";
            katanaCut = body.Has("rod_unwind") ? "rod_unwind" : "rod_spin";
            katanaMul = 1f; katanaLabel = null;
            BeginFarClip(katanaCut, "rod");
            body.SetSpeed(1f + 0.15f * c);
            SetTrails(true);
            Sound.Play(Sfx.WhooshBig, 1.1f + 0.2f * c, transform.position, 0.85f);
            Sound.Play(Sfx.HeroEffort, 0.8f, transform.position);
        }

        // ------------------------------------------------------------------ the flow, every frame of a swing
        /// <summary>Called from FarTraverseUpdate while a pipe swing plays: early swipes are held, and taken
        /// the moment the pipe is coming through. True if this frame is handled.</summary>
        bool PipeFlowUpdate()
        {
            var gi = GestureInput.Instance;
            if (gi == null) return false;
            var c = gi.Next();
            if (c != Command.None)
            {
                pipeBuffer = c;
                if (c == Command.Cut) { pbVec = GestureInput.CutVector; pbStart = GestureInput.CutStart; pbEnd = GestureInput.CutEnd; pbSpeed = GestureInput.CutSpeed; pbHigh = gi.LastSwipeHigh; }
            }
            bool open = farHit && body.Time > farHitAt + PipeFlowOpen;
            if (!open || pipeBuffer == Command.None) return false;
            var cmd = pipeBuffer;
            pipeBuffer = Command.None;
            killedThisCut = false;
            EndTraverse();
            if (cmd == Command.Cut)
            {
                GestureInput.CutVector = pbVec; GestureInput.CutStart = pbStart; GestureInput.CutEnd = pbEnd; GestureInput.CutSpeed = pbSpeed;
                PipeSwipe(pbVec, pbSpeed, pbHigh);
            }
            else HandleCommand(cmd);
            return true;
        }

        // ------------------------------------------------------------------ the impact
        void PipeLand()
        {
            string c = katanaCut;
            bool spin = c == "rod_spin" || c == "rod_unwind";
            float reach = spin ? 1.6f : c == "rod_swing" ? 1.35f : c == "rod_backhand" || c == "rod_shin" ? 1.3f : 1.2f;
            float baseDmg = c == "rod_overhead" ? 30f : spin ? 22f : c == "rod_swing" ? 22f : c == "rod_backhand" ? 20f : c == "rod_chop" ? 18f
                          : c == "rod_jab" ? 12f : c == "rod_shin" ? 14f : 18f;
            bool lastBlow = PipeUses == 1;
            float mul = katanaMul * pipePower * (1f + 0.15f * pipeFlow) * pipeComboMul * (lastBlow ? 1.5f : 1f);
            bool arc = c == "rod_swing" || c == "rod_backhand" || spin;
            int n = 0;
            bool intercepted = false, heavyHit = false;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.Alive || Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) continue;
                float dx = (e.transform.position.x - transform.position.x) * Facing;
                bool down = e.IsDown;
                // a man on the floor: only the overhead finds him (the ground smash, or the sweep's follow-up)
                if (down && !(c == "rod_overhead" && (pipeWasGroundSmash || pipeCombo == "SWEEP AND SMASH"))) continue;
                if (!down && !e.Targetable) continue;
                bool inReach = spin ? Mathf.Abs(dx) < reach + 0.3f : dx > -0.2f && dx < reach + 0.4f;
                if (!inReach || (!arc && farFoe != null && e != farFoe && !down)) continue;
                n++;
                // caught winding up or mid-swing: his blow is gone, he reels
                bool icpt = !down && (e.Telegraphing || e.MidSwing);
                float dmg = baseDmg * mul * (icpt ? 1.3f : 1f);
                string react = c == "rod_shin" ? "leg_buckle" : c == "rod_jab" ? "fold" : c == "rod_rising" ? "hit_up" : c == "rod_overhead" ? "hit_body"
                             : c == "rod_chop" ? "hit_body" : spin ? "stumble_back" : "hit_chin";
                e.TakeHit(new HitInfo
                {
                    damage = dmg, direction = dx >= 0f ? Facing : -Facing,
                    knockback = spin ? 4f + 3f * (pipeComboMul - 1f) / 0.7f : arc ? 5.5f : 2.5f,
                    knockdown = c == "rod_overhead" || (spin && pipeComboMul > 1.3f) || lastBlow,
                    heavy = true, low = c == "rod_shin", hitsDowned = down, react = react, attacker = this,
                });
                if (e.Alive && !down)
                {
                    // the blunt after-effects: the chop drops his guard, the sweep a knee, the butt end his wind
                    if (icpt) { e.Stagger(1.4f); intercepted = true; }
                    else if (c == "rod_chop") e.Stagger(1.2f);
                    else if (c == "rod_shin") e.Stagger(0.8f);
                    else if (c == "rod_jab") e.Stagger(0.6f);
                }
                heavyHit |= c == "rod_overhead" || spin || icpt;
                Sound.Play(Sfx.PipeHit, 1.1f, e.Chest, (c == "rod_overhead" ? 0.85f : 1f) - 0.03f * pipeFlow);
                if (icpt || c == "rod_shin" || c == "rod_chop") Sound.Play(Sfx.Bone, 0.7f, e.Chest);
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(Facing, 0.3f, 0f), 14 + 3 * pipeFlow, e.InkColor);
            }
            if (c == "rod_overhead") Sound.Play(Sfx.MetalClang, 0.9f, transform.position + Vector3.right * Facing * 1.0f, 0.8f);   // it rings off the floor

            // the window for the next swing: from just after this impact to just after the follow-through
            float rest = body.Current != null ? Mathf.Max(0f, body.Current.Length - body.Time) : 0.3f;
            pipeFlowFrom = Time.time;                                        // (the swing itself holds the next one PipeFlowOpen past the impact)
            pipeFlowUntil = Time.time + rest + PipeFlowGrace;

            if (n > 0)
            {
                int stop = (heavyHit ? 7 : 5) + pipeFlow + (intercepted ? 3 : 0) + (lastBlow ? 4 : 0);
                if (TimeController.Instance != null)
                {
                    TimeController.Instance.HitStop(Mathf.Min(stop, 14));
                    if (intercepted || lastBlow || pipeCombo == "FULL TURN") TimeController.Instance.SlowMo(0.3f, 0.4f);
                }
                if (CameraDirector.Instance != null)
                {
                    CameraDirector.Instance.Shake((heavyHit ? 0.2f : 0.13f) + 0.025f * pipeFlow, 0.18f);
                    if (intercepted || lastBlow) CameraDirector.Instance.ZoomPunch(-3f, 0.35f);
                }
                if (heavyHit || n >= 2) Haptics.Vibrate();
                var wd = WaveDirector.Instance;
                if (wd != null)
                {
                    // one label, the best one
                    if (lastBlow) wd.AddStyle(300, "LAST SWING");
                    else if (pipeCombo != null) wd.AddStyle(pipeCombo == "FULL TURN" ? 260 : 200, pipeCombo);
                    else if (intercepted) wd.AddStyle(180, "INTERCEPTED");
                    else if (n >= 2) wd.AddStyle(250, "CLEARING THE ROOM");
                    else if (pipeWasGroundSmash) wd.AddStyle(140, "PUT DOWN");
                    else if (pipeFlow >= 2) wd.AddStyle(80 + 40 * pipeFlow, "FLOW ×" + (pipeFlow + 1));
                    else if (c == "rod_overhead") wd.AddStyle(120, "OVERHEAD");
                    else if (katanaLabel != null) wd.AddStyle(150, katanaLabel);
                }
                if (intercepted) Tell("intercept");
                Tell(spin ? "pipe_spin" : "pipe_" + c);                           // (the training ground listens)
                PipeWear();
            }
            else
            {
                Sound.Play(Sfx.Whoosh, 0.6f, transform.position, 0.8f);
                // a big swing at nothing: the weight carries him round, he's slow to recover (unless he's in the flow)
                if (pipeFlow == 0 && (c == "rod_swing" || c == "rod_overhead" || spin)) body.SetSpeed(0.78f);
            }
        }

        /// <summary>Hits wear it, misses don't. Bending near the end; then it gives out.</summary>
        void PipeWear()
        {
            if (Sparring) return;                                             // the training ground: it never wears
            PipeUses--;
            var wd = WaveDirector.Instance;
            if (PipeUses == 4 && !pipeBendWarned)
            {
                pipeBendWarned = true;
                Sound.Play(Sfx.Creak, 0.8f, transform.position, 0.8f);
                if (wd != null) wd.ShowBanner("THE PIPE IS BENDING", 1.4f);
            }
            if (PipeUses <= 0)
            {
                Sound.Play(Sfx.MetalClang, 1f, transform.position + Vector3.up, 0.7f);
                Style = FightStyle.Fists;
                rodWound = false;
                ApplyStyleVisuals();
                if (wd != null) wd.ShowBanner("THE PIPE GIVES OUT: FISTS", 1.4f);
            }
        }
    }
}
