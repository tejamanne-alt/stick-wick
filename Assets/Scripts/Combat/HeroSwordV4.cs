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
        bool edgeClean, edgeTurned, edgeSlow;
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

        /// <summary>A man struck while attacking: DEBANA (early in his wind-up), or cut off (late, or mid-swing).</summary>
        static int Timing(Enemy e)
        {
            float wp = e.WindupProgress;
            if (wp >= 0f && wp < 0.45f) return 2;                                     // debana
            if (e.Telegraphing || e.MidSwing) return 1;                               // cut him off
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
