using UnityEngine;

namespace ProjectInk
{
    /// <summary>The katana, v2 (docs/design/sword.md). Where the pipe is momentum and wear, the sword is
    /// precision and lethality: one clean line at the right moment ends a man. Four things on top of the
    /// cuts, the thrust, the draw, the pass and the mirror parry (HeroKatanaV4):
    ///
    ///   HASUJI, THE EDGE. A cut is one line: the swipe's straightness is the edge's alignment. Straight and
    ///   fast: a CLEAN cut (×1.25, the stroke lingers white). Wobbly: THE EDGE TURNED: it glances (×0.6, it
    ///   never cuts a man down, a dull clank). Slow: a weak cut (×0.85).
    ///
    ///   DEBANA, THE MOMENT HE DECIDES. Cut a man in the first part of his wind-up (before it's 45% through)
    ///   and the cut lands before his blow exists: an ordinary man is CUT DOWN outright; a boss takes ×2
    ///   and reels. Later in the wind-up, or mid-swing: CUT HIM OFF (his blow gone, ×1.3, he reels). The
    ///   earlier the braver, the better.
    ///
    ///   SAYA NO UCHI, VICTORY IN THE SCABBARD. Sheathed (hold on the hero, the world slows), let him come;
    ///   draw as he winds up or swings at you and the draw wins before his blade is out: he's cut down
    ///   (a boss: ×3).
    ///
    ///   IN ONE BREATH. Men cut down one after another, each within 1.3 s of the last: TWO IN ONE BREATH,
    ///   THREE..., the slow motion deeper with each.
    ///
    /// And: early swipes during a cut are held and taken the moment the blade lands (never lost); the Kata
    /// of Three now needs three DIFFERENT lines (the same line again is what swordsmen read); a man cut by a
    /// rising line falls on his back (cut_down_back).</summary>
    public partial class HeroController
    {
        /// <summary>The training ground: put this in his hands (FISTS puts it away). Never wears out there.</summary>
        public void TrainWith(FightStyle s)
        {
            if (Style == s) return;
            if (carried != null) DropCarried();
            rodWound = false; sheathed = false;
            Equip(s, s == FightStyle.Fists ? 0 : 999);
            if (s == FightStyle.Sword) OnKatanaTaken();
            if (s == FightStyle.Pipe) { pipeFlow = 0; pipeLast = null; pipeFlowUntil = -9f; }
            Sound.Play(s == FightStyle.Sword ? Sfx.SwordClash : Sfx.Pickup, 0.6f, transform.position);
        }

        bool edgeClean, edgeTurned, edgeSlow;

        // ------------------------------------------------------------------ the armed evasion, timed
        // Reviewed: a step back out of his swing with a weapon in hand was the same as a casual step back. Now,
        // made as his swing starts (or comes), it's rewarded the way each weapon would: the pipe, MAKE HIM PAY
        // (the next swing within 1.2 s, ×1.5); the katana, NUKI-WAZA (the next cut within 1.1 s, as his blow
        // falls through empty air: ×1.5, and an ordinary man it would finish is cut down).
        float nukiUntil = -9f, pipeSlipAt = -9f;
        bool nukiCut;

        /// <summary>A man in front of him, close, winding up or swinging at him.</summary>
        Enemy SwingComing(float range)
        {
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable || e.IsDown) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 0.6f) continue;
                float dx = (e.transform.position.x - transform.position.x) * Facing;
                if (dx > 0f && dx < range && (e.Telegraphing || e.MidSwing)) return e;
            }
            return null;
        }

        /// <summary>His swing whiffs past: a beat of slow motion, the label, a little longer out of reach.</summary>
        void ArmedSlip(string label)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + 0.5f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.55f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge(label);
            Sound.Play(Sfx.Whoosh, 1f, transform.position + Vector3.right * Facing * 0.6f + Vector3.up, 0.8f);
            Tell("armed_slip");
        }

        /// <summary>The katana's retreat (hiki): out of distance in one gliding step; timed into his swing, NUKI-WAZA is on.</summary>
        void KatanaRetreat()
        {
            BeginTraverse("katana_retreat", 0f);
            invulnerableUntil = Time.time + 0.3f;
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
            if (SwingComing(2.4f) != null)
            {
                nukiUntil = Time.time + 1.1f;                                 // the retreat (0.4 s), then 0.7 s to cut
                ArmedSlip("OUT OF DISTANCE");
            }
        }

        /// <summary>A dodge key with a weapon in hand (Q / E): the weapon's own evasion, still facing him.</summary>
        bool ArmedDodge()
        {
            if (!KatanaMode || sheathed || !Grounded) return false;
            if (state != State.Idle && state != State.Traverse) return false;
            var man = NearestThreat(4.5f);
            if (man != null) FaceTowards(man.transform.position.x);
            if (RodMode && body.Has("rod_evade")) PipeEvade();
            else KatanaRetreat();
            return true;
        }
        int breath;
        float lastCutDownAt = -9f;
        // a swipe made before the blade has landed: held until it has
        Command kBuffer = Command.None;
        Vector2 kbVec, kbStart, kbEnd;
        float kbSpeed, kbWobble;

        /// <summary>The swipe just made, read as an edge: straight and fast, wandering, or slow.</summary>
        void ReadEdge(float speed)
        {
            float perScreen = speed / Mathf.Max(1f, Screen.height);              // screen heights a second
            float wob = GestureInput.CutWobble;
            edgeTurned = wob > 0.3f;
            edgeClean = !edgeTurned && wob < 0.12f && perScreen > 1.6f;
            edgeSlow = !edgeTurned && perScreen < 0.7f;
        }

        void EdgeNeutral() { edgeClean = true; edgeTurned = false; edgeSlow = false; }     // the point, the draw: no edge to turn

        float EdgeMul => edgeTurned ? 0.6f : edgeClean ? 1.25f : edgeSlow ? 0.85f : 1f;

        /// <summary>The man whose wind-up had only just begun when this cut was started (debana is judged
        /// by when the cut is begun, not when it lands: the blade takes a quarter of a second to get there).</summary>
        Enemy debanaFoe;

        /// <summary>At the start of a cut: is the man it's going for just deciding (the first 45% of his wind-up)?</summary>
        void ReadDebana(Enemy t)
        {
            // NUKI-WAZA: out of distance as he swung; this cut comes as his blow falls through empty air
            nukiCut = Time.time < nukiUntil;
            if (nukiCut) nukiUntil = -9f;
            debanaFoe = null;
            if (t == null) return;
            float wp = t.WindupProgress;
            if (wp >= 0f && wp < 0.45f) debanaFoe = t;
        }

        /// <summary>A man struck while attacking: DEBANA (the cut was begun as he decided), or cut off (later).</summary>
        int Timing(Enemy e)
        {
            bool attacking = e.Telegraphing || e.MidSwing;
            if (attacking && e == debanaFoe) return 2;                                // debana
            if (attacking) return 1;                                                  // cut him off
            return 0;
        }

        /// <summary>A man cut down: the breath count, the label, the slow motion it earns.</summary>
        string CutDownLabel(out int points, out float slow)
        {
            breath = Time.time - lastCutDownAt < 1.3f ? breath + 1 : 1;
            lastCutDownAt = Time.time;
            points = 150 + 120 * (breath - 1);
            slow = 0.45f + 0.12f * Mathf.Min(breath - 1, 3);
            if (breath >= 2) return breath == 2 ? "TWO IN ONE BREATH" : breath == 3 ? "THREE IN ONE BREATH" : breath + " IN ONE BREATH";
            return null;
        }

        /// <summary>Called from FarTraverseUpdate while a katana cut plays: early swipes are held, and taken
        /// the moment the blade has landed (a new line cancels the follow-through). True if handled.</summary>
        bool KatanaFlowUpdate()
        {
            var gi = GestureInput.Instance;
            if (gi == null) return false;
            var c = gi.Next();
            if (c != Command.None)
            {
                kBuffer = c;
                if (c == Command.Cut) { kbVec = GestureInput.CutVector; kbStart = GestureInput.CutStart; kbEnd = GestureInput.CutEnd; kbSpeed = GestureInput.CutSpeed; kbWobble = GestureInput.CutWobble; }
            }
            if (!(farHit && body.Time > farHitAt + 0.12f) || kBuffer == Command.None) return false;
            var cmd = kBuffer;
            kBuffer = Command.None;
            killedThisCut = false;
            EndTraverse();
            if (cmd == Command.Cut)
            {
                GestureInput.CutVector = kbVec; GestureInput.CutStart = kbStart; GestureInput.CutEnd = kbEnd;
                GestureInput.CutSpeed = kbSpeed; GestureInput.CutWobble = kbWobble;
            }
            HandleCommand(cmd);
            return true;
        }
    }
}
