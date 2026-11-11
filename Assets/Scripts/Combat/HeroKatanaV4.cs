using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The katana (docs/design/backlog.md step 5; docs/story/chapter6_fortress_v3.md).
    /// A cut is a line, and a swipe is a line, so with the katana in hand every swipe is a cut along its
    /// own angle, drawn anywhere on the screen (never on the man: the thumb never hides him). The swipe
    /// is drawn as a brushstroke (InkStroke) and the blade follows it.
    ///   swipe ↘ towards     kesa-giri, the diagonal shoulder cut
    ///   swipe ↗ towards     gyaku-kesa, rising from the hip
    ///   swipe ↓             kiri-oroshi, the heaviest
    ///   swipe ↑             kiri-age, rising (and the answer to a thrust)
    ///   swipe → level       yoko-giri, catches two men
    ///   swipe ← away        the retreat out of distance (a man behind: the turning cut)
    ///   tap high / low      tsuki, the thrust (throat / belly), the longest reach
    ///   hold ON the hero    sheathe and settle (the world slows); drag out and let go: the draw cut
    ///                       along the drag; through two or three men in a line: THE PASS
    /// A swordsman's coming cut shows as a red line (RedLine): swipe the SAME line to parry it
    /// (uke-nagashi): within 20° PERFECT, within 45° a parry, then a free return cut (kaeshi). A red
    /// point is a thrust: swipe up, or retreat. Kata: ↓ then ↑ = tsubame-gaeshi; ↘ then ↗ = jūji;
    /// three cuts without a pause = the Kata of Three.</summary>
    public partial class HeroController
    {
        /// <summary>Two hands on a blade (the katana) or a rod (the pipe: the same lines, swung blunt; backlog step 13).</summary>
        public bool KatanaMode => Contact && (Style == FightStyle.Sword || Style == FightStyle.Pipe) && body != null && body.Has("katana_guard");
        bool RodMode => Style == FightStyle.Pipe;
        /// <summary>The knife in story mode: swipes slash along their line too (taps still stab, or throw at a man far off).</summary>
        bool KnifeLines => Contact && Style == FightStyle.Knife && body != null && body.Has("katana_guard");
        bool sheathed;
        float kaeshiUntil, katanaMul = 1f, iaiSlowAt;
        string katanaLabel, katanaCut;
        InkStroke lastStroke;
        readonly List<(string cut, float t)> kataLog = new List<(string, float)>();
        static readonly HashSet<string> lessonsSaid = new HashSet<string>();

        /// <summary>A katana cut in progress (the gallery's paper screens tear under it).</summary>
        public bool IsCutting => farMove == "katana" || farMove == "pass";

        string KatanaIdle => sheathed ? "iai_hold" : "katana_guard";

        /// <summary>Every idle frame (FarTick): the grammar follows the weapon.</summary>
        void KatanaTick()
        {
            GestureInput.Grammar = KatanaMode || KnifeLines ? GestureInput.WeaponGrammar.Katana : GestureInput.WeaponGrammar.Fists;
            if (!KatanaMode) { sheathed = false; return; }
            // the world slows while he waits on the draw
            if (sheathed && GestureInput.IaiHolding && Time.unscaledTime > iaiSlowAt && TimeController.Instance != null)
            {
                iaiSlowAt = Time.unscaledTime + 0.3f;
                TimeController.Instance.SlowMo(0.45f, 0.35f);
            }
        }

        /// <summary>Voss, remembered: each lesson the first time it's needed.</summary>
        void Lesson(string key, string line)
        {
            if (!lessonsSaid.Add(key) || StoryIntro.Instance == null) return;
            StoryIntro.Instance.SayLine("VOSS MEMORY", line, 3.4f);
        }

        /// <summary>Picked up a katana (DoPickup).</summary>
        void OnKatanaTaken()
        {
            if (!KatanaMode) return;
            sheathed = false;
            SwordUses = 999;                                          // a katana doesn't wear out
            Lesson("take", "The line comes before the blade.");
            body.Play("katana_guard", 0.2f);
        }

        // ------------------------------------------------------------------ commands
        bool KatanaCommand(Command cmd)
        {
            if (KnifeLines && cmd == Command.Cut) { KnifeSlash(GestureInput.CutVector); return true; }
            if (!KatanaMode) return false;
            switch (cmd)
            {
                case Command.Cut: KatanaCut(GestureInput.CutVector, GestureInput.CutSpeed); return true;
                case Command.IaiHold: BeginIaiStance(); return true;
                case Command.IaiDraw: IaiDraw(GestureInput.CutVector); return true;
                case Command.IaiCancel: if (sheathed) { sheathed = false; body.Play("katana_guard", 0.15f); Sound.Play(Sfx.SwordSwing, 0.5f, transform.position, 1.3f); } return true;
            }
            return false;
        }

        /// <summary>The line's angle relative to the man he faces: 0 = level towards, +90 up, −90 down, ±180 away.</summary>
        float CutAngle(Vector2 v) => Mathf.Atan2(v.y, v.x * Facing) * Mathf.Rad2Deg;

        static float LineDiff(Vector2 a, Vector2 b)
        {
            float x = Mathf.Repeat(Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg, 180f);
            float y = Mathf.Repeat(Mathf.Atan2(b.y, b.x) * Mathf.Rad2Deg, 180f);
            float d = Mathf.Abs(x - y);
            return Mathf.Min(d, 180f - d);
        }

        void KatanaCut(Vector2 v, float speed)
        {
            if (state != State.Idle && state != State.Traverse) return;
            if (sheathed) { IaiDraw(v); return; }
            lastStroke = InkStroke.Draw(GestureInput.CutStart, GestureInput.CutEnd, 0.16f, transform.position.z);

            // a swordsman's line coming: mirror it (the parry)
            if (TryMirrorParry(v)) return;

            float ang = CutAngle(v);
            var behind = PickTarget(-Facing, 1.9f, false);
            if (Mathf.Abs(ang) > 112f)
            {
                if (behind != null) { KatanaStrike("ushiro", behind, 1.2f, speed); return; }
                BeginTraverse("katana_retreat", 0f);                 // the retreat: out of distance
                invulnerableUntil = Time.time + 0.3f;
                Sound.Play(Sfx.Cloth, 0.8f, transform.position);
                return;
            }
            string cut = ang > 67f ? "kiriage" : ang > 22f ? "gyaku" : ang > -22f ? "yoko" : ang > -67f ? "kesa" : "oroshi";
            float reach = cut == "yoko" ? 1.3f : cut == "kiriage" ? 1.15f : cut == "gyaku" ? 1.2f : 1.25f;
            var t = PickTarget(Facing, 3.2f, false);
            if (t != null && Mathf.Abs(t.transform.position.y - transform.position.y) > 0.8f) t = null;
            KatanaStrike(cut, t, reach, speed);
        }

        void KatanaStrike(string cut, Enemy t, float reach, float speed)
        {
            // kata: the lines just drawn, read together
            float now = Time.time;
            kataLog.RemoveAll(k => now - k.t > 1.5f);
            katanaMul = 1f;
            katanaLabel = null;
            string prev = kataLog.Count > 0 ? kataLog[kataLog.Count - 1].cut : null;
            float gap = kataLog.Count > 0 ? now - kataLog[kataLog.Count - 1].t : 9f;
            if (prev == "oroshi" && cut == "kiriage" && gap < 0.75f) { katanaMul = 1.6f; katanaLabel = "TSUBAME-GAESHI"; }
            else if (prev == "kesa" && cut == "gyaku" && gap < 0.75f) { katanaMul = 1.5f; katanaLabel = "JŪJI"; }
            if (kataLog.Count >= 2 && gap < 0.65f && now - kataLog[kataLog.Count - 2].t < 1.3f)
            {
                katanaMul = 2f; katanaLabel = "KATA OF THREE";
                InkStroke.HoldAll(0.5f);
                kataLog.Clear();
            }
            else kataLog.Add((cut, now));
            if (Time.time < kaeshiUntil) { katanaMul *= 1.6f; katanaLabel = katanaLabel ?? "KAESHI"; kaeshiUntil = 0f; }
            katanaCut = cut;
            if (cut == "ushiro") { farFoe = t; BeginFarClip("ushiro", "katana"); }
            else if (t != null) BeginFar(cut, t, "katana", reach);
            else BeginFarClip(cut, "katana");
            body.SetSpeed(Mathf.Clamp(0.95f + speed / 9000f, 1f, 1.3f));
            Sound.Play(Sfx.SwordSwing, Mathf.Clamp(0.7f + speed / 6000f, 0.7f, 1.2f), transform.position, cut == "oroshi" ? 0.85f : 1f);
            Tell("cut");
        }

        /// <summary>A tap with the katana: the thrust, high (throat) or low (belly).</summary>
        void KatanaThrust(int side, bool low)
        {
            if (sheathed) { IaiDraw(new Vector2(side, 0f)); return; }
            if (side != Facing) SetFacing(side);
            var t = PickTarget(Facing, 3.4f, false);
            katanaMul = Time.time < kaeshiUntil ? 1.6f : 1f;
            katanaLabel = Time.time < kaeshiUntil ? "KAESHI" : null;
            kaeshiUntil = 0f;
            katanaCut = low ? "tsuki_low" : "tsuki";
            if (t != null) BeginFar(katanaCut, t, "katana", 1.5f); else BeginFarClip(katanaCut, "katana");
            Sound.Play(Sfx.Whoosh, 1f, transform.position, 1.2f);
            Lesson("thrust", "Point first. The edge is for after.");
        }

        /// <summary>The impact of a katana strike (from FarImpact).</summary>
        void KatanaImpact()
        {
            string cut = katanaCut;
            float reach = cut == "ushiro" ? -1.2f : cut.StartsWith("tsuki") ? 1.5f : cut == "yoko" ? 1.3f : 1.25f;
            float dmg = (cut == "oroshi" ? 28f : cut.StartsWith("tsuki") ? 22f : cut == "kiriage" ? 18f : 22f) * katanaMul;
            if (RodMode) { RodImpact(cut, reach, dmg * 0.8f); return; }
            bool any = false;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.Targetable) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) continue;
                float dx = (e.transform.position.x - transform.position.x) * Facing;
                bool inReach = reach > 0f ? dx > -0.2f && dx < reach + 0.35f : dx < 0.2f && dx > reach - 0.35f;
                if (!inReach) continue;
                if (e != farFoe && cut != "yoko" && farFoe != null) continue;           // one man, except the level cut
                any = true;
                if (e.ReadsCut(cut))
                {
                    // he read it: blade on blade
                    Clash(e.Chest + Vector3.up * 0.2f, false);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "HE READ YOUR LINE: CHANGE IT");
                    continue;
                }
                if (e.Health <= dmg && !e.Boss && e.CanBeHeld) { CutDown(e); continue; }
                e.TakeHit(new HitInfo { damage = dmg, direction = reach > 0f ? Facing : -Facing, knockback = 2.5f, heavy = true,
                                        react = cut == "kiriage" ? "head_back" : cut == "tsuki_low" ? "fold" : "hit_chin", attacker = this });
                Sound.Play(Sfx.SwordHit, 1f, e.Chest);
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(Facing, 0.3f, 0f), 18, e.InkColor);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(5);
            }
            if (any && katanaLabel != null && WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(katanaLabel == "KATA OF THREE" ? 400 : 180, katanaLabel);
            if (!any) Sound.Play(Sfx.Whoosh, 0.6f, transform.position);
        }

        /// <summary>The rod: the same lines, blunt. The level sweep catches everyone in reach (CLEARING THE ROOM);
        /// the overhead floors a man; it wears out with use.</summary>
        void RodImpact(string cut, float reach, float dmg)
        {
            int n = 0;
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.Targetable || Mathf.Abs(e.transform.position.y - transform.position.y) > 1.0f) continue;
                float dx = (e.transform.position.x - transform.position.x) * Facing;
                if (dx < -0.2f || dx > reach + 0.4f) continue;
                if (e != farFoe && cut != "yoko" && farFoe != null) continue;
                n++;
                e.TakeHit(new HitInfo { damage = dmg, direction = Facing, knockback = cut == "yoko" ? 5f : 3f, knockdown = cut == "oroshi" || cut == "yoko",
                                        heavy = true, react = cut == "kiriage" ? "hit_up" : "hit_chin", attacker = this });
                Sound.Play(Sfx.PipeHit, 1f, e.Chest);
            }
            if (n >= 2 && WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(250, "CLEARING THE ROOM");
            else if (n == 1 && katanaLabel != null && WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, katanaLabel);
            if (n > 0 && TimeController.Instance != null) TimeController.Instance.HitStop(5);
            if (--PipeUses <= 0) { Style = FightStyle.Fists; ApplyStyleVisuals(); if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner("THE ROD BENDS: FISTS", 1.2f); }
        }

        /// <summary>The knife: a swipe is a one-handed slash along its line (a tap still stabs, or throws at a man far off).</summary>
        void KnifeSlash(Vector2 v)
        {
            if (state != State.Idle && state != State.Traverse) return;
            lastStroke = InkStroke.Draw(GestureInput.CutStart, GestureInput.CutEnd, 0.14f, transform.position.z);
            float ang = CutAngle(v);
            if (Mathf.Abs(ang) > 112f)
            {
                BeginTraverse("katana_retreat", 0f);
                invulnerableUntil = Time.time + 0.3f;
                return;
            }
            string clip = ang > 40f ? "uppercut" : ang < -55f ? "hammer_fist" : "hook";
            var t = PickTarget(Facing, 2.2f, false);
            katanaCut = "knife";
            if (t != null) BeginFar(clip, t, "knife", 0.9f); else BeginFarClip(clip, "knife");
            Sound.Play(Sfx.Knife, 1f, transform.position);
        }

        void KnifeImpact()
        {
            var e = farFoe;
            if (e == null || !e.Targetable || Mathf.Abs((e.transform.position.x - transform.position.x) * Facing - 0.9f) > 0.6f) { Sound.Play(Sfx.Whoosh, 0.6f, transform.position); return; }
            if (e.Health <= 18f && !e.Boss && e.CanBeHeld) { CutDown(e); return; }
            e.TakeHit(new HitInfo { damage = 18f, direction = Facing, knockback = 1.5f, react = "hit_chin", attacker = this });
            Sound.Play(Sfx.SwordHit, 0.8f, e.Chest, 1.3f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(Facing, 0.2f, 0f), 12, e.InkColor);
        }

        /// <summary>A killing cut: he stands a beat with the stroke across him, the knees go, he falls on his face.</summary>
        void CutDown(Enemy e)
        {
            e.BeginHeld(this);
            e.HeldPlay("cut_down", 0.04f);
            e.HeldDamage(e.Health + 1f);
            if (lastStroke != null) lastStroke.Linger(0.7f);
            Sound.Play(Sfx.SwordHit, 1.1f, e.Chest);
            Sound.Play(Sfx.FlashCut, 0.8f, e.Chest);
            if (TimeController.Instance != null) { TimeController.Instance.HitStop(6); TimeController.Instance.SlowMo(0.3f, 0.45f); }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.1f, 0.12f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, katanaLabel ?? "CUT DOWN");
            StartCoroutine(LayDown(e, 1.5f));
        }

        IEnumerator LayDown(Enemy e, float after)
        {
            yield return new WaitForSeconds(after);
            if (e == null || !e.Held) yield break;
            float sh = e.body != null && e.body.Current != null ? e.body.Current.endShift : 0f;
            e.LandThrown(sh, true);
        }

        void Clash(Vector3 at, bool perfect)
        {
            Sound.Play(Sfx.SwordClash, perfect ? 1.2f : 1f, at);
            if (InkFx.Instance != null) InkFx.Instance.Burst(at, perfect ? 2.6f : 1.6f);
        }

        // ------------------------------------------------------------------ the mirror parry
        bool TryMirrorParry(Vector2 swipe)
        {
            Enemy best = null;
            float bd = 2.8f;
            Vector2 line = Vector2.zero;
            bool thrust = false;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.KatanaThreat(out var d, out var th)) continue;
                float dist = Mathf.Abs(e.transform.position.x - transform.position.x);
                if (dist < bd) { bd = dist; best = e; line = d; thrust = th; }
            }
            if (best == null) return false;
            float diff;
            if (thrust)
            {
                float a = Mathf.Atan2(swipe.y, Mathf.Abs(swipe.x)) * Mathf.Rad2Deg;     // a thrust: answered by a rising swipe
                diff = Mathf.Abs(90f - a);
                if (diff > 45f) return false;
            }
            else
            {
                diff = LineDiff(swipe, line);
                if (diff > 45f) return false;
            }
            bool perfect = diff < 20f;
            FaceTowards(best.transform.position.x);
            string cut = best.KatanaCutName;
            string clip = thrust ? "k_parry_rise" : cut == "gyaku" || cut == "kiriage" ? "k_parry_low" : "k_parry_high";
            BeginFarClip(clip, "parry");
            best.KatanaParried(perfect);
            kaeshiUntil = Time.time + (perfect ? 1.0f : 0.7f);
            invulnerableUntil = Time.time + 0.35f;
            Clash(best.Chest + Vector3.up * 0.25f + Vector3.right * -Facing * 0.3f, perfect);
            if (TimeController.Instance != null) { TimeController.Instance.HitStop(perfect ? 7 : 4); if (perfect) TimeController.Instance.SlowMo(0.3f, 0.55f); }
            if (CameraDirector.Instance != null && perfect) CameraDirector.Instance.ZoomPunch(-4f, 0.4f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(perfect ? 250 : 120, perfect ? "PERFECT PARRY" : "PARRIED");
            Lesson("parry", "Don't meet his sword. Meet his line.");
            Tell("parry");
            return true;
        }

        // ------------------------------------------------------------------ iai: the draw
        void BeginIaiStance()
        {
            if (state != State.Idle || RodMode) return;                // a rod has no scabbard
            sheathed = true;
            running = false;
            velocity.x = 0f;
            BeginTraverse("iai_sheathe", 0f);
            Sound.Play(Sfx.SwordSwing, 0.4f, transform.position, 1.4f);
            Lesson("draw", "Draw only once. Make it enough.");
        }

        /// <summary>The held press dragged out and let go: the draw cut along the drag; men in a line along it: the pass.</summary>
        void IaiDraw(Vector2 v)
        {
            if (!sheathed) { KatanaCut(v, 3000f); return; }
            sheathed = false;
            if (state == State.Traverse && traverse == "iai_sheathe") { traverse = null; state = State.Idle; }
            int dir = v.x >= 0f ? 1 : -1;
            SetFacing(dir);
            InkStroke.Draw(GestureInput.CutStart, GestureInput.CutEnd, 0.3f, transform.position.z);
            // the men along the line within the pass's reach
            var line = new List<Enemy>();
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable || e.Boss || !e.CanBeHeld) continue;
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (dx > 0.3f && dx < 3.0f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.5f) line.Add(e);
            }
            if (line.Count >= 2 && body.Has("katana_pass")) { BeginPass(line); return; }
            katanaCut = "iai";
            katanaMul = 1.9f;
            katanaLabel = "NUKITSUKE";
            var t = line.Count > 0 ? line[0] : PickTarget(dir, 3f, false);
            if (t != null) BeginFar("iai_draw", t, "katana", 1.45f); else BeginFarClip("iai_draw", "katana");
            Sound.Play(Sfx.FlashCut, 1f, transform.position);
            Sound.Play(Sfx.SwordSwing, 1.2f, transform.position, 0.9f);
        }

        // ------------------------------------------------------------------ the pass
        readonly List<Enemy> passMen = new List<Enemy>();

        void BeginPass(List<Enemy> men)
        {
            passMen.Clear();
            men.Sort((a, b) => Mathf.Abs(a.transform.position.x - transform.position.x).CompareTo(Mathf.Abs(b.transform.position.x - transform.position.x)));
            for (int i = 0; i < men.Count && i < 3; i++)
            {
                var e = men[i];
                e.BeginHeld(this);                                    // frozen where he stands until the click
                e.HeldPlay(e.body.Has("k_parried") ? "k_parried" : "hit_front", 0.05f);
                passMen.Add(e);
            }
            invulnerableUntil = Time.time + 1.6f;
            BeginFarClip("katana_pass", "pass");
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.9f);
            if (CameraDirector.Instance != null) { CameraDirector.Instance.Focus(transform, 1.6f); CameraDirector.Instance.ZoomPunch(-5f, 1.2f); }
            Sound.Play(Sfx.FlashCut, 1.2f, transform.position);
            Tell("pass");
        }

        /// <summary>The click of the blade going home: now they fall, in order.</summary>
        void PassClick()
        {
            Sound.Play(Sfx.Latch, 1f, transform.position);
            sheathed = true;
            StartCoroutine(PassFalls(new List<Enemy>(passMen)));
            passMen.Clear();
            if (WaveDirector.Instance != null) { WaveDirector.Instance.AddStyle(500, "THE PASS"); WaveDirector.Instance.ShowBanner("THE PASS", 1.4f); }
        }

        IEnumerator PassFalls(List<Enemy> men)
        {
            foreach (var e in men)
            {
                yield return new WaitForSeconds(0.18f);
                if (e == null || !e.Held) continue;
                e.HeldPlay("cut_down", 0.04f);
                e.HeldDamage(e.Health + 1f);
                Sound.Play(Sfx.SwordHit, 1f, e.Chest);
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, Vector3.up, 14, e.InkColor);
                StartCoroutine(LayDown(e, 1.5f));
            }
        }

        /// <summary>Thrown aside (the duel): the katana skids away across the stones, the fists come up.</summary>
        public void DiscardKatana()
        {
            if (Style != FightStyle.Sword) return;
            Style = FightStyle.Fists;
            sheathed = false;
            ApplyStyleVisuals();
            GestureInput.Grammar = GestureInput.WeaponGrammar.Fists;
            Sound.Play(Sfx.SwordClash, 0.7f, transform.position + Vector3.right * -Facing * 1.5f, 0.7f);
        }
    }
}
