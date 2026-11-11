using UnityEngine;

namespace ProjectInk
{
    /// <summary>Near range on the V4 body (docs/design/combat_v3.md, "near range"): strings that know
    /// the distance, counters that read the man, and CROSSFIRE for two men on you.
    ///
    /// The tap is still only high or low. What it throws depends on how close he is:
    ///   INSIDE (under 0.9 m)   high: short shots (elbows, hooks, the uppercut)   low: to the BODY
    ///   KICKING RANGE          high: jab, cross, straight shots                   low: KICKS
    /// Tokens: P high at range, I high inside, B low inside, K low at range.
    ///   P P P P   jab, cross, hook, uppercut                 BOXER'S FOUR
    ///   P P K     jab, cross, calf kick (his leg goes)       DUTCH TWO-AND-LOW
    ///   P P B     jab, cross, the liver: he folds            ONE-TWO-LIVER
    ///   P B P     jab, body, head                            UP-DOWN-UP
    ///   B P / B I the liver, then the head as his hands drop LIVER AND LIGHTS
    ///   B B P     two to the body, the uppercut as he bends  DIG AND LIFT
    ///   I I I     lead elbow, rear elbow, rising elbow       CLOSE QUARTERS
    ///   K K K     calf, body, head                           KICK STRING
    ///   K P P     calf, lunge punch, reverse punch           KARATE
    /// Off the book: the natural next shot (the other hand, the same range), no name.
    /// READ THE MAN (EnemyRead.Opening), before the string: his body's opening picks the counter.</summary>
    public partial class HeroController
    {
        /// <summary>How often each read has been answered: its words over the man fade after three.</summary>
        public static readonly int[] ReadsUsed = new int[16];
        /// <summary>This chapter's named combo (docs/design/backlog.md step 12): it lands harder on this chapter's boss.</summary>
        public static string ChapterCombo;
        bool chapterComboShown;
        Enemy calfFoe;
        int calfKicks;
        float calfAt, crossfireReadyAt;
        bool lastLead;

        AttackData Use(AttackData m, AttackData alt) => body.Has(m.clip) ? m : alt;

        static AttackData Named(AttackData m, string label, float dmgScale = 1f)
        {
            m.name = label;
            m.damage *= dmgScale;
            m.hitStopFrames += 2;
            m.shake += 0.04f;
            return m;
        }

        AttackData ContactMove(bool kick, Enemy t, bool behindClose)
        {
            if (!body.Has("liver_hook")) return ContactMoveClassic(kick, t, behindClose);
            if (behindClose) { strSeq = ""; return AttackData.SBackKick; }
            if (t != null && t.Dazed)
            {
                strSeq = "";
                return kick ? AttackData.SideKick.Finisher("OUT ON HIS FEET") : AttackData.PalmStrike.Finisher("PALM TO THE CHIN");
            }
            float d = t != null ? Mathf.Abs(t.transform.position.x - transform.position.x) : 1.2f;
            bool inside = d < 0.9f;

            // read him first
            if (t != null)
            {
                var r = ReadCounter(kick, inside, t);
                if (r.clip != null) { strSeq = ""; Tell("read"); ReadsUsed[(int)t.Read]++; return Pin(r, t); }
            }

            char tok = kick ? (inside ? 'B' : 'K') : (inside ? 'I' : 'P');
            strSeq += tok;
            AttackData m;
            switch (strSeq)
            {
                case "P": m = AttackData.SJab; break;
                case "PP": m = AttackData.SCross; break;
                case "PPP": m = AttackData.LeadHook; break;
                case "PPPP": m = Use(AttackData.SUppercut, AttackData.Elbow).Finisher("BOXER'S FOUR"); strSeq = ""; break;
                case "PPPK": m = AttackData.SideKick.Finisher("FOUR-PIECE"); strSeq = ""; break;
                case "PPK": m = Named(CalfKick(t), "DUTCH TWO-AND-LOW", 1.25f); strSeq = ""; break;
                case "PPB": case "PPI": m = Named(Use(AttackData.LiverHook, AttackData.GyakuZuki), "ONE-TWO-LIVER", 1.25f); strSeq = ""; break;
                case "PB": case "PI": m = Use(AttackData.BodyCross, AttackData.GyakuZuki); break;
                case "PBP": case "PBI": m = AttackData.LeadHook.Finisher("UP-DOWN-UP"); strSeq = ""; break;
                case "B": m = Use(AttackData.LiverHook, AttackData.GyakuZuki); break;
                case "BB": m = Use(AttackData.BodyCross, AttackData.GyakuZuki); break;
                case "BP": case "BI": m = AttackData.LeadHook.Finisher("LIVER AND LIGHTS"); strSeq = ""; break;
                case "BBP": case "BBI": m = Use(AttackData.SUppercut, AttackData.Elbow).Finisher("DIG AND LIFT"); strSeq = ""; break;
                case "I": m = AttackData.Elbow; break;
                case "II": m = Use(AttackData.RearElbow, AttackData.LeadHook); break;
                case "III": m = Use(AttackData.ElbowUp, AttackData.Elbow).Finisher("CLOSE QUARTERS"); strSeq = ""; break;
                case "K": m = CalfKick(t); break;
                case "KK": m = AttackData.Mawashi; break;
                case "KKK": m = Use(AttackData.HeadKick, AttackData.SideKick).Finisher("KICK STRING"); strSeq = ""; break;
                case "KP": case "KI": m = AttackData.OiZuki; break;
                case "KPP": case "KPI": m = AttackData.GyakuZuki.Finisher("KARATE"); strSeq = ""; break;
                case "PK": m = CalfKick(t); break;
                case "PKP": m = AttackData.PalmStrike.Finisher("PALM HEEL"); strSeq = ""; break;
                default:
                    // off the book: the natural next shot, the other hand, at this range
                    strSeq = tok.ToString();
                    lastLead = !lastLead;
                    switch (tok)
                    {
                        case 'P': m = lastLead ? AttackData.SJab : AttackData.SCross; break;
                        case 'I': m = lastLead ? AttackData.LeadHook : Use(AttackData.RearElbow, AttackData.Elbow); break;
                        case 'B': m = lastLead ? Use(AttackData.LiverHook, AttackData.GyakuZuki) : Use(AttackData.BodyCross, AttackData.GyakuZuki); break;
                        default: m = lastLead ? CalfKick(t) : AttackData.Mawashi; break;
                    }
                    break;
            }
            if (t != null && t.Health <= m.damage && string.IsNullOrEmpty(m.name)) m = m.Finisher(null);
            if (t != null && t.Boss && ChapterCombo != null && m.name == ChapterCombo) { m.damage *= 1.5f; m.name += "  ★"; }
            return Pin(m, t);
        }

        /// <summary>The calf kick: the second one on the same man inside three seconds takes his leg.</summary>
        AttackData CalfKick(Enemy t)
        {
            var m = AttackData.LowKick;
            if (t != null && t == calfFoe && Time.time - calfAt < 3f) calfKicks++; else calfKicks = 1;
            calfFoe = t;
            calfAt = Time.time;
            if (calfKicks >= 2 && t != null && t.body != null && t.body.Has("leg_buckle")) { m.react = "leg_buckle"; calfKicks = 0; }
            return m;
        }

        /// <summary>His body's opening, answered: null clip if the tap doesn't answer it.</summary>
        AttackData ReadCounter(bool kick, bool inside, Enemy t)
        {
            switch (t.Read)
            {
                case Opening.Folded:
                    if (!kick) return Use(AttackData.RisingKnee, AttackData.SUppercut).Finisher("KNEE TO THE FACE");
                    break;
                case Opening.HeadBack:
                    if (kick) return Named(inside ? Use(AttackData.LiverHook, AttackData.GyakuZuki) : WithReact(AttackData.Mawashi, "fold"), "HE FORGOT HIS BODY", 1.3f);
                    break;
                case Opening.GuardHigh:
                    if (kick) return Named(inside ? Use(AttackData.LiverHook, AttackData.GyakuZuki) : CalfKick(t), "UNDER THE GUARD", 1.3f);
                    break;
                case Opening.GuardLow:
                    if (!kick) return Named(Use(AttackData.Overhand, AttackData.SCross), "OVER THE TOP", 1.3f);
                    break;
                case Opening.Overreach:
                    if (!kick) { var c = Use(AttackData.Overhand, AttackData.SCross).Finisher("COUNTER"); if (t.Boss && ChapterCombo == "COUNTER") { c.damage *= 1.5f; c.name += "  ★"; } return c; }
                    break;
                case Opening.Stumbling:
                    if (kick) return AttackData.MaeGeri.Finisher("SENT OVER");
                    break;
                case Opening.Buckled:
                    if (!kick) return Use(AttackData.HeadKick, AttackData.Mawashi).Finisher("TIMBER!");
                    break;
            }
            return default;
        }

        static AttackData WithReact(AttackData m, string react) { m.react = react; return m; }

        /// <summary>His back to the wall: everything lands harder, and a finisher slams him down it.</summary>
        AttackData Pin(AttackData m, Enemy t)
        {
            if (t == null || !t.Pinned || t.Boss) return m;
            m.damage *= 1.25f;
            if (!string.IsNullOrEmpty(m.name) && t.body != null && t.body.Has("wall_slump"))
            {
                m.react = "wall_slump";
                m.knockdown = false;
                m.name = "PINNED · " + m.name;
            }
            return m;
        }

        // ------------------------------------------------------------------ CROSSFIRE: two men on you
        /// <summary>One man close in front, one close behind: the next tap is CROSSFIRE.</summary>
        bool CrossfirePair(out Enemy front, out Enemy back, int side)
        {
            front = back = null;
            if (!MovesV4 || !ContactBare || !Grounded || Time.time < crossfireReadyAt || !body.Has("crossfire")) return false;
            float bf = 1.8f, bb = 1.8f;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.CanBeHeld || e.Boss || !e.body.Has("crossfire_a_v")) continue;
                if (Mathf.Abs(e.transform.position.y - transform.position.y) > 0.5f) continue;
                float dx = (e.transform.position.x - transform.position.x) * side;
                if (dx > 0.2f && dx < bf) { bf = dx; front = e; }
                else if (dx < -0.2f && -dx < bb) { bb = -dx; back = e; }
            }
            return front != null && back != null;
        }

        public bool CrossfireReady => (CrossfirePair(out _, out _, Facing) || CrossfirePair(out _, out _, -Facing)) && state == State.Idle;

        bool TryCrossfire(int side)
        {
            if (!CrossfirePair(out var a, out var b, side) && !CrossfirePair(out a, out b, -side)) return false;
            int dir = a.transform.position.x >= transform.position.x ? 1 : -1;
            SetFacing(dir);
            var me = transform.position;
            a.BeginHeld(this);
            b.BeginHeld(this);
            a.HeldAt(new Vector3(me.x + dir * 0.6f, a.transform.position.y, a.transform.position.z), me.x);
            b.HeldAt(new Vector3(me.x - dir * 0.6f, b.transform.position.y, b.transform.position.z), me.x);
            a.HeldPlay("crossfire_a_v", 0.06f);
            b.HeldPlay("crossfire_b_v", 0.06f);
            farFoe = a;
            crossA = a;
            crossB = b;
            crossStep = 0;
            invulnerableUntil = Time.time + 2.4f;
            crossfireReadyAt = Time.time + 16f;
            BeginFarClip("crossfire", "crossfire");
            if (CameraDirector.Instance != null) { CameraDirector.Instance.Focus(transform, 2.2f); CameraDirector.Instance.ZoomPunch(-6f, 2.0f); }
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.45f);
            Sound.Play(Sfx.Riser, 0.8f, transform.position);
            Tell("crossfire");
            return true;
        }

        Enemy crossA, crossB;
        int crossStep;

        /// <summary>Every frame of the cinematic: the four impacts on their frames.</summary>
        void UpdateCrossfire()
        {
            float f = body.Time * 30f;
            var me = transform.position;
            int dir = Facing;
            if (crossA != null && crossA.Held) crossA.HeldAt(crossA.transform.position, me.x);
            if (crossB != null && crossB.Held) crossB.HeldAt(crossB.transform.position, me.x);
            if (crossStep == 0 && f >= 7f)
            {
                // their punches cross over him: each one lands on the other man
                crossStep = 1;
                Hurt(crossA, 8f); Hurt(crossB, 8f);
                Sound.Play(Sfx.PunchHeavy, 1f, me + Vector3.up * 1.1f);
                Sound.Play(Sfx.PunchHeavy, 0.9f, me + Vector3.up * 1.1f, 0.9f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.15f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, "THEY HIT EACH OTHER");
                if (TimeController.Instance != null) TimeController.Instance.HitStop(5);
            }
            if (crossStep == 1 && f >= 13f)
            {
                crossStep = 2;                                    // up between them: the palm, the elbow
                Hurt(crossA, 10f); Hurt(crossB, 10f);
                Sound.Play(Sfx.PunchHeavy, 1f, me + Vector3.right * dir * 0.6f + Vector3.up);
                Sound.Play(Sfx.Bone, 0.8f, me - Vector3.right * dir * 0.6f + Vector3.up);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
            }
            if (crossStep == 2 && f >= 26f)
            {
                crossStep = 3;                                    // the push kick: the front man flies
                Hurt(crossA, 30f);
                Sound.Play(Sfx.Kick, 1.2f, me + Vector3.right * dir * 0.8f);
                Sound.Play(Sfx.BodyFall, 1f, me + Vector3.right * dir * 1.6f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.2f, 0.2f);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(7);
            }
            if (crossStep == 3 && f >= 38f)
            {
                crossStep = 4;                                    // the back kick: the other one
                Hurt(crossB, 30f);
                Sound.Play(Sfx.Kick, 1.3f, me - Vector3.right * dir * 0.8f);
                Sound.Play(Sfx.CineHit, 0.9f, me);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.26f, 0.25f);
                if (TimeController.Instance != null) { TimeController.Instance.HitStop(8); TimeController.Instance.SlowMo(0.3f, 0.7f); }
                if (WaveDirector.Instance != null) { WaveDirector.Instance.AddStyle(400, "CROSSFIRE"); WaveDirector.Instance.ShowBanner("CROSSFIRE", 1.6f); }
            }
        }

        static void Hurt(Enemy e, float dmg)
        {
            if (e == null || !e.Held) return;
            e.HeldDamage(dmg);
            if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest + Vector3.up * 0.25f, Vector3.up, 8, e.InkColor);
        }

        /// <summary>The cinematic is over: both men where their clips threw them, on the floor.</summary>
        void EndCrossfire()
        {
            foreach (var e in new[] { crossA, crossB })
            {
                if (e == null || !e.Held) continue;
                float sh = e.body != null && e.body.Current != null ? e.body.Current.endShift : 0f;
                e.LandThrown(sh, false);
            }
            crossA = crossB = null;
        }
    }

    /// <summary>The read, spelled out: over the nearest man, which tap answers what his body is
    /// showing ("TAP LOW · BODY"); over the hero when CROSSFIRE is ready. White ink, big enough for a
    /// phone, only while the opening lasts.</summary>
    public class OpeningCue : MonoBehaviour
    {
        public HeroController hero;
        GUIStyle style;
        float pulse;

        void OnGUI()
        {
            if (hero == null || hero.IsDead || !hero.Contact || TimeController.Paused) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                var f = Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
                if (f != null) style.font = f;
            }
            style.fontSize = Mathf.RoundToInt(Screen.height * 0.034f);
            pulse += Time.unscaledDeltaTime;
            Enemy best = null;
            float bd = 2.6f;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Read == Opening.None) continue;
                if (Mathf.Abs(e.transform.position.y - hero.transform.position.y) > 1f) continue;
                float d = Mathf.Abs(e.transform.position.x - hero.transform.position.x);
                if (d < bd) { bd = d; best = e; }
            }
            if (best != null && HeroController.ReadsUsed[(int)best.Read] >= 3)
            {
                // learnt: only a small mark where the opening is
                var sp0 = cam.WorldToScreenPoint(best.transform.position + Vector3.up * (best.Read == Opening.GuardHigh || best.Read == Opening.HeadBack || best.Read == Opening.Stumbling ? 0.75f : 1.4f));
                if (sp0.z > 0f) { style.normal.textColor = new Color(1f, 1f, 1f, 0.6f + 0.3f * Mathf.Sin(pulse * 9f)); GUI.Label(new Rect(sp0.x - 20f, Screen.height - sp0.y - 20f, 40f, 40f), "•", style); }
                best = null;
            }
            if (best != null)
            {
                string text; float h;
                switch (best.Read)
                {
                    case Opening.GuardHigh: text = "TAP LOW · BODY"; h = 0.75f; break;
                    case Opening.GuardLow: text = "TAP HIGH · HEAD"; h = 1.45f; break;
                    case Opening.Folded: text = "TAP HIGH · KNEE"; h = 1.25f; break;
                    case Opening.HeadBack: text = "TAP LOW · LIVER"; h = 0.75f; break;
                    case Opening.Stumbling: text = "TAP LOW · PUSH KICK"; h = 0.6f; break;
                    case Opening.Overreach: text = "TAP HIGH · COUNTER"; h = 1.45f; break;
                    case Opening.Buckled: text = "TAP HIGH · HEAD KICK"; h = 1.35f; break;
                    default: text = "PINNED · FINISH HIM"; h = 1.6f; break;
                }
                Draw(cam, best.transform.position + Vector3.up * h * best.transform.localScale.y, text);
            }
            if (hero.CrossfireReady) Draw(cam, hero.transform.position + Vector3.up * 1.9f, "TWO ON YOU · TAP: CROSSFIRE");
        }

        void Draw(Camera cam, Vector3 world, string text)
        {
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            float a = 0.75f + 0.25f * Mathf.Sin(pulse * 9f);
            var r = new Rect(sp.x - 260f, Screen.height - sp.y - 30f, 520f, 60f);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f * a);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(r, text, style);
        }
    }
}
