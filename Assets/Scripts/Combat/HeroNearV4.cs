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
            bool inside = d < 0.8f;          // a step-in stops at 92% of the reach (a jab: 0.87 m): still outside, the string goes on

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
            if (!MovesV4 || !ContactBare || !Grounded || (Time.time < crossfireReadyAt && !Sparring && !CrossfireAnytime) || !body.Has("crossfire")) return false;
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

        /// <summary>No cooldown (the chapter 1 CROSSFIRE test).</summary>
        public bool CrossfireAnytime;

        public bool CrossfireReady => (CrossfirePair(out _, out _, Facing) || CrossfirePair(out _, out _, -Facing)) && state == State.Idle;

        // CROSSFIRE (docs/design/crossfire.md): two men on him, and the two-thumb tap, one thumb each side of
        // him; each thumb high (his head) or low (his legs / body). The four:
        //   HIGH + HIGH            crossfire   THE CROSSING: under both punches, they land on each other
        //   LOW + LOW              cf_knees    BOTH KNEES: stamp, mule kick; the knee, the heel
        //   front HIGH, back LOW   cf_split    BOTH AT ONCE: the cross and the back kick on the same frame
        //   front LOW, back HIGH   cf_into     ONE INTO THE OTHER: back elbow, liver hook, over the hip into him
        struct CfHit { public int f; public int who; public float dmg; public Sfx sfx; public int stop; public string label; }
        static readonly System.Collections.Generic.Dictionary<string, CfHit[]> CfHits = new System.Collections.Generic.Dictionary<string, CfHit[]>
        {
            // who: 1 the man in front, 2 the one behind, 3 both
            { "crossfire", new[] { new CfHit { f = 7, who = 3, dmg = 8, sfx = Sfx.PunchHeavy, stop = 5, label = "THEY HIT EACH OTHER" },
                                   new CfHit { f = 13, who = 3, dmg = 10, sfx = Sfx.Bone, stop = 6 },
                                   new CfHit { f = 26, who = 1, dmg = 30, sfx = Sfx.Kick, stop = 7 },
                                   new CfHit { f = 38, who = 2, dmg = 30, sfx = Sfx.Kick, stop = 8, label = "THE CROSSING" } } },
            { "cf_knees", new[] { new CfHit { f = 6, who = 1, dmg = 10, sfx = Sfx.Bone, stop = 5 },
                                  new CfHit { f = 12, who = 2, dmg = 10, sfx = Sfx.Bone, stop = 5, label = "BOTH KNEES" },
                                  new CfHit { f = 23, who = 1, dmg = 30, sfx = Sfx.PunchHeavy, stop = 7 },
                                  new CfHit { f = 33, who = 2, dmg = 30, sfx = Sfx.Kick, stop = 8, label = "BOTH KNEES" } } },
            { "cf_split", new[] { new CfHit { f = 8, who = 3, dmg = 40, sfx = Sfx.CineHit, stop = 16, label = "BOTH AT ONCE" } } },
            { "cf_into", new[] { new CfHit { f = 5, who = 2, dmg = 12, sfx = Sfx.PunchHeavy, stop = 5 },
                                 new CfHit { f = 11, who = 1, dmg = 14, sfx = Sfx.Bone, stop = 6 },
                                 new CfHit { f = 23, who = 3, dmg = 30, sfx = Sfx.BodyFall, stop = 9, label = "ONE INTO THE OTHER" } } },
        };

        /// <summary>The two-thumb tap: CROSSFIRE if two men are on him (the thumbs pick the move); otherwise
        /// it's a tap at the nearer man, at that thumb's height.</summary>
        void CrossfireCommand()
        {
            bool l = GestureInput.PairLeftLow, r = GestureInput.PairRightLow;
            if (TryCrossfire(l, r)) return;
            Enemy best = null;
            float bd = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Alive) continue;
                float d = Mathf.Abs(e.transform.position.x - transform.position.x);
                if (d < bd) { bd = d; best = e; }
            }
            int side = best != null ? (best.transform.position.x >= transform.position.x ? 1 : -1) : Facing;
            Strike(side, side < 0 ? l : r);
        }

        bool TryCrossfire(bool leftLow, bool rightLow)
        {
            if (!CrossfirePair(out var a, out var b, Facing) && !CrossfirePair(out a, out b, -Facing)) return false;
            int dir = a.transform.position.x >= transform.position.x ? 1 : -1;
            // the thumb on each man's side: the one in front is on the `dir` side of the screen
            bool frontLow = dir > 0 ? rightLow : leftLow, backLow = dir > 0 ? leftLow : rightLow;
            string move = !frontLow && !backLow ? "crossfire" : frontLow && backLow ? "cf_knees" : !frontLow ? "cf_split" : "cf_into";
            if (!body.Has(move) || !a.body.Has(move + "_a_v") || !b.body.Has(move + "_b_v"))
                move = "crossfire";
            string va = move == "crossfire" ? "crossfire_a_v" : move + "_a_v", vb = move == "crossfire" ? "crossfire_b_v" : move + "_b_v";
            SetFacing(dir);
            var me = transform.position;
            a.BeginHeld(this);
            b.BeginHeld(this);
            a.HeldAt(new Vector3(me.x + dir * 0.6f, a.transform.position.y, a.transform.position.z), me.x);
            b.HeldAt(new Vector3(me.x - dir * 0.6f, b.transform.position.y, b.transform.position.z), me.x);
            a.HeldPlay(va, 0.06f);
            b.HeldPlay(vb, 0.06f);
            farFoe = a;
            crossA = a;
            crossB = b;
            crossStep = 0;
            crossMove = move;
            BeginFarClip(move, "crossfire");
            float len = body.Current != null ? body.Current.Length : 2f;
            invulnerableUntil = Time.time + len + 0.3f;
            crossfireReadyAt = Time.time + 10f;
            if (CameraDirector.Instance != null) { CameraDirector.Instance.Focus(transform, len); CameraDirector.Instance.ZoomPunch(-6f, Mathf.Min(2f, len)); }
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.45f);
            Sound.Play(Sfx.Riser, 0.8f, transform.position);
            Tell("crossfire");
            Tell("cf:" + move);                                        // which of the four (the training ground's lessons)
            return true;
        }

        Enemy crossA, crossB;
        int crossStep;
        string crossMove = "crossfire";

        /// <summary>Every frame of the cinematic: each move's impacts on their frames.</summary>
        void UpdateCrossfire()
        {
            float f = body.Time * 30f;
            var me = transform.position;
            int dir = Facing;
            if (crossA != null && crossA.Held) crossA.HeldAt(crossA.transform.position, me.x);
            if (crossB != null && crossB.Held) crossB.HeldAt(crossB.transform.position, me.x);
            if (!CfHits.TryGetValue(crossMove, out var hits)) return;
            while (crossStep < hits.Length && f >= hits[crossStep].f)
            {
                var h = hits[crossStep++];
                bool last = crossStep == hits.Length;
                if ((h.who & 1) != 0) { Hurt(crossA, h.dmg); Sound.Play(h.sfx, 1.1f, me + Vector3.right * dir * 0.6f + Vector3.up * 0.9f); }
                if ((h.who & 2) != 0) { Hurt(crossB, h.dmg); Sound.Play(h.sfx == Sfx.CineHit ? Sfx.Kick : h.sfx, 1.1f, me - Vector3.right * dir * 0.6f + Vector3.up * 0.9f, 0.92f); }
                var cam = CameraDirector.Instance;
                if (cam != null) cam.Shake(last ? 0.26f : 0.14f, last ? 0.25f : 0.15f);
                var tc = TimeController.Instance;
                if (tc != null)
                {
                    tc.HitStop(h.stop);
                    if (crossMove == "cf_split") tc.SlowMo(0.22f, 0.9f);                     // the frame frozen on both at once
                    else if (last) tc.SlowMo(0.3f, 0.7f);
                }
                if (crossMove == "cf_split" && InkFx.Instance != null)
                {
                    // white ink at both impacts
                    if (crossA != null) InkFx.Instance.Splash(crossA.Chest + Vector3.up * 0.3f, Vector3.right * dir, 12, Color.white);
                    if (crossB != null) InkFx.Instance.Splash(crossB.Chest, -Vector3.right * dir, 12, Color.white);
                }
                if (crossMove == "cf_into" && crossStep == 2 && tc != null) tc.SlowMo(0.3f, 0.5f);   // up over the hip: slow at the top
                if (!string.IsNullOrEmpty(h.label) && WaveDirector.Instance != null)
                {
                    WaveDirector.Instance.AddStyle(last ? 400 : 150, h.label);
                    if (last) WaveDirector.Instance.ShowBanner(h.label, 1.6f);
                }
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
                e.LandThrown(sh, crossMove == "cf_split" && e == crossB);       // folded round the heel: on his face
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
            if (hero.CrossfireReady) Draw(cam, hero.transform.position + Vector3.up * 1.9f, "TWO ON YOU · BOTH THUMBS");
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
