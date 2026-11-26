using UnityEngine;

namespace ProjectInk
{
    /// <summary>SKIP, for the whole story: while a scene plays by itself (the hero locked, no prompt waiting),
    /// two buttons sit bottom right:
    ///   SKIP ›         this scene only: the shot in progress jumps to its end (where it leaves everyone), the line
    ///                  being spoken is cut, and the story goes on to its next scene. A tap on the right half of
    ///                  the screen does the same.
    ///   TO PLAY »      on to the next moment the player has the controls (a prompt to make, or the hero free):
    ///                  the scenes run through behind a black card, silent.
    /// How: the shared timing helpers (Hold, Cut, Move, RealWait, HoldFor, PuppetWalk, the fades, WoLerp) end
    /// their wait when SkipNow is true; the clock-driven sequences (the opening, the breach, the escape, the
    /// memory, the walk to the hall, the walk-out) jump their clock to the next beat their At() checks are
    /// waiting for. Nothing that waits for the player (HOLD, MASH, a choice) is ever skipped.</summary>
    public partial class StoryIntro
    {
        static int skipFrame = -10;
        bool kGapAwait;
        static bool fastForward;
        float ffAt = -9f, ffEndAt = -9f, ffVolume = 1f, skipTapAt = -9f;
        // the next beat of each clock (what a skip jumps to), refilled every frame by the At() checks
        float nextT = 1e9f, nextB = 1e9f, nextM = 1e9f, nextE = 1e9f, nextH = 1e9f;

        /// <summary>True for the one frame a skip was asked for (and every frame of a fast-forward).</summary>
        public static bool SkipNow => fastForward || skipFrame == Time.frameCount;

        static float NextBeat(float clock, float beat, float best) => beat > clock && beat < best ? beat : best;

        /// <summary>The player has the controls: a prompt is waiting for them, or the hero is free.</summary>
        bool PlayerHasControl => scAsk != ScAsk.None || hero == null || hero.IsDead || !hero.CinematicLock
                                 || !string.IsNullOrEmpty(tapTitle) || kGapAwait || carPrompt;      // the chapters' own tap moments

        /// <summary>A scene playing by itself, that SKIP may cut.</summary>
        bool AutoPlaying => hero != null && !PlayerHasControl && phase != Phase.End && !recapOn && !TimeController.Paused && blackout < 0.98f;

        /// <summary>Skip this scene. From the GUI (after the frame's coroutines ran) it lands next frame.</summary>
        void SkipScene(bool fromGui)
        {
            skipFrame = Time.frameCount + (fromGui ? 1 : 0);
            subUntil = 0f;                                         // the line being spoken is cut
            skipTapAt = Time.unscaledTime;
            Sound.Play(Sfx.Whoosh, 0.35f, null, 1.3f);
        }

        /// <summary>On to the next moment the player has the controls.</summary>
        void SkipToPlay()
        {
            // the opening has its own, proven way straight to the walk
            if (phase == Phase.Cinematic && !gateBusy) { SkipToWalk(); return; }
            fastForward = true;
            ffAt = Time.unscaledTime;
            ffVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            subUntil = 0f;
        }

        void StopFastForward()
        {
            if (!fastForward) return;
            fastForward = false;
            ffEndAt = Time.unscaledTime;
            AudioListener.volume = ffVolume;
        }

        /// <summary>Every frame (from Update): the fast-forward ends where the player has the controls.</summary>
        void UpdateSkip()
        {
            if (fastForward && (PlayerHasControl || phase == Phase.End || Time.unscaledTime - ffAt > 25f)) StopFastForward();
        }

        /// <summary>A tap on the right half of the screen while a scene plays: skip it.</summary>
        void OnSkipTap(Vector2 at, bool low, int side)
        {
            if (!AutoPlaying || fastForward) return;
            if (at.x < Screen.width * 0.5f || GestureInput.IsBlocked(at)) return;
            if (Time.unscaledTime - skipTapAt < 0.25f) return;     // one tap, one scene
            SkipScene(false);
        }

        void SkipGUI(float w, float h)
        {
            float now = Time.unscaledTime;
            if (fastForward || now - ffEndAt < 0.5f)
            {
                // the scenes running through, behind black
                float a = fastForward ? Mathf.Clamp01((now - ffAt) / 0.15f) : 1f - (now - ffEndAt) / 0.5f;
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
                if (fastForward)
                {
                    big.normal.textColor = new Color(1f, 1f, 1f, 0.85f * a);
                    int fs = big.fontSize;
                    big.fontSize = 40;
                    int dots = 1 + (int)(now * 4f) % 3;
                    GUI.Label(new Rect(0, h * 0.44f, w, 60), Spaced("SKIPPING") + "  " + new string('›', dots), big);
                    big.fontSize = fs;
                }
                return;
            }
            if (!AutoPlaying) return;
            // bottom right, clear of the subtitles (bottom centre) and the pause button (top left)
            float bw = 150f, bh = 44f, gap = 10f;
            var toPlay = new Rect(w - bw - 18f, h - bh - 18f, bw, bh);
            var one = new Rect(toPlay.x - bw - gap, toPlay.y, bw, bh);
            SkipButton(one, Spaced("SKIP") + "  ›", false);
            SkipButton(toPlay, Spaced("TO PLAY") + "  »", true);
            if (Event.current.type == EventType.Repaint)
            {
                GestureInput.BlockGui(one, scale);
                GestureInput.BlockGui(toPlay, scale);
            }
            if (GUI.Button(one, GUIContent.none, GUIStyle.none)) { SkipScene(true); return; }
            if (GUI.Button(toPlay, GUIContent.none, GUIStyle.none)) { SkipToPlay(); return; }
        }

        void SkipButton(Rect r, string label, bool strong)
        {
            float flash = Mathf.Clamp01(1f - (Time.unscaledTime - skipTapAt) / 0.3f);
            Fill(r, strong ? new Color(0.55f, 0.05f, 0.06f, 0.75f) : new Color(0f, 0f, 0f, 0.55f + 0.3f * flash));
            Fill(new Rect(r.x, r.y, r.width, 1), new Color(1f, 1f, 1f, 0.4f));
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(1f, 1f, 1f, 0.4f));
            int fs = btn.fontSize;
            var col = btn.normal.textColor;
            btn.fontSize = 18;
            btn.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            GUI.Label(r, label, btn);
            btn.fontSize = fs;
            btn.normal.textColor = col;
        }
    }
}
