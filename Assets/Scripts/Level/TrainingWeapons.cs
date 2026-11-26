using System;
using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The training ground with a weapon in hand (for now all on chapter 1's ground, for testing;
    /// later blended into the story where each weapon arrives). Nine lessons in the same WATCH / YOUR TURN
    /// form as the bare-handed ones, and FREE PLAY's FISTS · PIPE · SWORD chips to hold either at will.
    ///   THE PIPE (docs/design/pipe.md): THE SWINGS (→, ↓, → drawn low) · FIGURE EIGHT (→ then ← as it comes
    ///     through) · INTERCEPT (hit him as he winds up) · THE FULL TURN (hold on yourself, let go)
    ///   THE SWORD (docs/design/sword.md): ONE LINE (straight and fast: CLEAN) · THE SWALLOW (↓ then ↑) ·
    ///     DEBANA (cut as he starts) · SAYA NO UCHI (sheathed, draw as he swings) · THE PARRY (his red line)</summary>
    public partial class StoryIntro
    {
        /// <summary>What FREE PLAY has in his hands (and whose moves its checklist shows).</summary>
        FightStyle trainWeapon = FightStyle.Fists;

        Lesson[] WeaponLessons() => new[]
        {
            // ------------------------------------------------------------------ the pipe
            new Lesson { title = "PIPE · THE SWINGS", weapon = FightStyle.Pipe,
                steps = new[] { S(Gest.CutAt, "SWIPE AT HIM: FOREHAND"), Due(S(Gest.CutDown, "SWIPE DOWN: OVERHEAD"), After(1.0f)),
                                Due(S(Gest.CutLow, "DRAWN LOW: THE LEGS"), After(1.0f)) },
                stage = () => Spar(1.4f, true, false), restage = true,
                demo = () => DemoCuts(1.1f),
                match = (k, a, l, low) => k == 'd' && a == "pipe_rod_shin" },
            new Lesson { title = "PIPE · FIGURE EIGHT", weapon = FightStyle.Pipe, reps = 2,
                steps = new[] { S(Gest.CutAt, "FOREHAND"), Due(S(Gest.CutAway, "AS IT COMES THROUGH: SWIPE BACK"), After(0.45f)) },
                stage = () => Spar(1.4f, true, false), restage = true,
                demo = () => DemoCuts(0.42f),
                match = (k, a, l, low) => k == 's' && (Named(l, "FIGURE EIGHT") || Named(l, "WINDMILL")) },
            new Lesson { title = "PIPE · INTERCEPT", weapon = FightStyle.Pipe, reps = 2, guard = -1,
                steps = new[] { Timed(S(Gest.TapHigh, "HE WINDS UP: HIT HIM NOW"), _ => Swinging()) },
                stage = () => Spar(1.3f, false, true),
                demo = () => DemoParry(),
                match = (k, a, l, low) => k == 'd' && a == "intercept" },
            new Lesson { title = "PIPE · THE FULL TURN", weapon = FightStyle.Pipe,
                steps = new[] { S(Gest.HoldSelf, "HOLD ON YOURSELF: WIND UP"), S(Gest.DrawOut, "LET GO") },
                cast = new[] { 0, 1 }, stage = () => { HeroAt(0f); Mark(0, 1.2f, true, false); Mark(1, -1.2f, true, false); }, restage = true,
                demo = () => DemoHoldRelease(1.2f, null),
                match = (k, a, l, low) => k == 'd' && a == "pipe_spin" },

            // ------------------------------------------------------------------ the sword
            new Lesson { title = "SWORD · ONE LINE", weapon = FightStyle.Sword, reps = 2,
                steps = new[] { S(Gest.CutAt, "ONE STRAIGHT, FAST LINE") },
                stage = () => Spar(1.3f, true, false), restage = true,
                demo = () => DemoCuts(1.2f),
                match = (k, a, l, low) => k == 'd' && a == "cut_clean" },
            new Lesson { title = "SWORD · THE SWALLOW", weapon = FightStyle.Sword,
                steps = new[] { S(Gest.CutDown, "SWIPE DOWN"), Due(S(Gest.CutUp, "AND STRAIGHT BACK UP"), After(0.35f)) },
                stage = () => Spar(1.3f, true, false), restage = true,
                demo = () => DemoCuts(0.35f),
                match = (k, a, l, low) => k == 's' && Named(l, "TSUBAME-GAESHI") },
            new Lesson { title = "SWORD · DEBANA", weapon = FightStyle.Sword, reps = 2, guard = -1,
                steps = new[] { Timed(S(Gest.CutAt, "HE STARTS: CUT NOW"), _ => EarlyWindup()) },
                stage = () => Spar(1.3f, false, true), restage = true,
                demo = () => DemoTimedCut(EarlyWindup),
                match = (k, a, l, low) => k == 's' && Named(l, "DEBANA") },
            new Lesson { title = "SWORD · SAYA NO UCHI", weapon = FightStyle.Sword, guard = -1,
                steps = new[] { S(Gest.HoldSelf, "HOLD ON YOURSELF: SHEATHE"), S(Gest.DrawOut, "HE SWINGS: DRAW") },
                stage = () => Spar(1.5f, false, true), restage = true,
                demo = () => DemoHoldRelease(0.8f, Swinging),
                match = (k, a, l, low) => k == 's' && Named(l, "SAYA NO UCHI") },
            new Lesson { title = "SWORD · THE PARRY", weapon = FightStyle.Sword, fencer = true, reps = 2, guard = -1,
                steps = new[] { Timed(S(Gest.CutLine, "SWIPE HIS RED LINE"), t0 => FencerLine(out _)) },
                stage = () => Spar(1.5f, false, true),
                demo = () => DemoTimedCut(() => FencerLine(out _)),
                match = (k, a, l, low) => k == 's' && (Named(l, "PARRIED") || Named(l, "PERFECT PARRY")) },
        };

        // ---------------------------------------------------------------- arming

        /// <summary>Before a lesson's marks: the weapon in his hands, the fencer (or not).</summary>
        void ArmFor(Lesson L)
        {
            trainWeapon = L.weapon;
            hero.TrainWith(L.weapon);
            SetFencer(L.fencer);
        }

        /// <summary>Man 0 fences: a blade in his hand, his cuts shown as red lines (the parry lesson).</summary>
        void SetFencer(bool on)
        {
            var m = Man(0);
            if (m == null) return;
            m.KatanaBoss = on;
            m.ShowCaneBlade(on);
        }

        bool EarlyWindup()
        {
            var a = Man(0);
            if (a == null || !a.Alive || Gap(a) > 2.6f) return false;
            float wp = a.WindupProgress;
            return wp >= 0f && wp < 0.35f;
        }

        /// <summary>The fencer's red line, if it's showing and can still be met.</summary>
        bool FencerLine(out Vector2 line)
        {
            line = Vector2.zero;
            var a = Man(0);
            return a != null && a.Alive && a.KatanaThreat(out line, out _);
        }

        // ---------------------------------------------------------------- gestures

        bool WeaponMatches(Step st, Command c, int side)
        {
            var v = GestureInput.CutVector;
            bool cut = c == Command.Cut;
            switch (st.g)
            {
                case Gest.CutAt: return cut && v.x * side > 0.75f;
                case Gest.CutAway: return cut && v.x * side < -0.75f;
                case Gest.CutDown: return cut && v.y < -0.8f;
                case Gest.CutUp: return cut && v.y > 0.6f;
                case Gest.CutAtDown: return cut && v.x * side > 0.3f && v.y < -0.3f && v.y > -0.92f;
                case Gest.CutLow: return cut && GestureInput.Instance != null && !GestureInput.Instance.LastSwipeHigh && v.x * side > 0.4f;
                case Gest.CutLine: return cut;
                case Gest.HoldSelf: return c == Command.IaiHold;
                case Gest.DrawOut: return c == Command.IaiDraw || c == Command.IaiCancel;
            }
            return false;
        }

        /// <summary>Where a weapon step starts (pixels, y up): `c` is the hero's chest on screen.</summary>
        Vector2 WeaponFrom(Gest g, int side, Vector2 c)
        {
            switch (g)
            {
                case Gest.CutAt: return c + new Vector2(side * W * 0.04f, H * 0.04f);
                case Gest.CutAway: return c + new Vector2(side * W * 0.24f, H * 0.04f);
                case Gest.CutDown: return c + new Vector2(side * W * 0.12f, H * 0.16f);
                case Gest.CutUp: return c + new Vector2(side * W * 0.12f, -H * 0.14f);
                case Gest.CutAtDown: return c + new Vector2(side * W * 0.03f, H * 0.14f);
                case Gest.CutLow: return new Vector2(c.x + side * W * 0.04f, H * 0.2f);
                case Gest.CutLine:
                    {
                        FencerLine(out var line);
                        if (line.sqrMagnitude < 0.01f) line = new Vector2(side, 0f);
                        return c + new Vector2(side * W * 0.12f, 0f) - line.normalized * W * 0.11f;
                    }
                case Gest.HoldSelf: case Gest.DrawOut: return c;
            }
            return c;
        }

        Vector2 WeaponTo(Gest g, int side, Vector2 a)
        {
            switch (g)
            {
                case Gest.CutAt: return a + new Vector2(side * W * 0.24f, 0f);
                case Gest.CutAway: return a + new Vector2(-side * W * 0.24f, 0f);
                case Gest.CutDown: return a + new Vector2(0f, -H * 0.3f);
                case Gest.CutUp: return a + new Vector2(0f, H * 0.3f);
                case Gest.CutAtDown: return a + new Vector2(side * W * 0.18f, -H * 0.24f);
                case Gest.CutLow: return a + new Vector2(side * W * 0.24f, 0f);
                case Gest.CutLine:
                    {
                        FencerLine(out var line);
                        if (line.sqrMagnitude < 0.01f) line = new Vector2(side, 0f);
                        return a + line.normalized * W * 0.22f;
                    }
                case Gest.DrawOut: return a + new Vector2(side * W * 0.24f, 0f);
            }
            return a;
        }

        // ---------------------------------------------------------------- the demonstrations

        /// <summary>The lesson's swipes in order, fast and straight (a clean edge), `gap` seconds apart.</summary>
        IEnumerator DemoCuts(float gap)
        {
            var L = lessons[li];
            for (int k = 0; k < L.steps.Length; k++)
            {
                yield return Swipe(From(k), To(k), k, 0.13f);
                yield return WaitU(k == L.steps.Length - 1 ? 1.4f : gap);
            }
        }

        /// <summary>Waits for the moment (his wind-up just begun, his red line), then the cut, fast.</summary>
        IEnumerator DemoTimedCut(Func<bool> moment)
        {
            demoStep = 0;
            yield return Glide(0, From(0));
            float t = 0f;
            while (t < 7f && !moment()) { t += UDt; yield return null; }
            yield return Swipe(From(0), To(0), 0, 0.11f, 0, true);
            yield return WaitU(1.4f);
        }

        /// <summary>A press held on the hero (at least `hold` seconds; with `until`, until that's true), then
        /// dragged out and let go.</summary>
        IEnumerator DemoHoldRelease(float hold, Func<bool> until)
        {
            yield return HoldAt(From(0), 0, until, hold, until != null ? 7f : hold);
            demoStep = 1;
            var a = From(1);
            var b = To(1);
            fTrail[0].Clear();
            for (float k = 0f; k < 1f; k += UDt / 0.12f)
            {
                SetF(0, Vector2.Lerp(a, b, 1f - (1f - k) * (1f - k)), true);
                fTrail[0].Add(fPos[0]);
                yield return null;
            }
            SetF(0, b, true);
            yield return null;
            Lift(0);
            rippleAt = Time.unscaledTime; rippleAtPos = b;
            yield return WaitU(1.6f);
        }

        // ---------------------------------------------------------------- free play: what's in his hands

        /// <summary>FISTS · PIPE · SWORD: tap to hold it (the checklist below shows its moves).</summary>
        void WeaponChips(float cx, float y)
        {
            var kinds = new[] { FightStyle.Fists, FightStyle.Pipe, FightStyle.Sword };
            var names = new[] { "FISTS", "PIPE", "SWORD" };
            float cw = 120f, gap = 8f, x = cx - (cw * 3f + gap * 2f) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(x + i * (cw + gap), y, cw, 38);
                bool on = trainWeapon == kinds[i];
                Fill(r, on ? new Color(0.8f, 0.06f, 0.07f, 0.9f) : new Color(0f, 0f, 0f, 0.55f));
                Fill(new Rect(r.x, r.yMax - 2, r.width, 2), new Color(1f, 1f, 1f, on ? 0.9f : 0.35f));
                tBtn.normal.textColor = new Color(1f, 1f, 1f, on ? 1f : 0.7f);
                GUI.Label(r, Spaced(names[i]), tBtn);
                if (Event.current.type == EventType.Repaint) GestureInput.BlockGui(r, scale);
                if (!on && GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    trainWeapon = kinds[i];
                    hero.TrainWith(trainWeapon);
                    if (WaveDirector.Instance != null)
                        WaveDirector.Instance.ShowBanner(trainWeapon == FightStyle.Pipe ? "THE PIPE  ·  SWIPE WHERE IT SHOULD LAND  ·  KEEP IT SWINGING"
                                                       : trainWeapon == FightStyle.Sword ? "THE SWORD  ·  ONE STRAIGHT LINE  ·  CUT AS HE DECIDES" : "BARE HANDS", 2.4f);
                }
            }
        }

        /// <summary>The lessons for what he's holding (the free play checklist).</summary>
        bool ForWeapon(int i) => lessons[i].weapon == trainWeapon;
    }
}
