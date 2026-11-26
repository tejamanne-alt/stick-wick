using UnityEngine;

namespace ProjectInk
{
    /// <summary>SKIP, for the whole story: while a scene plays by itself (the hero locked, no prompt waiting),
    /// two buttons sit bottom right:
    ///   SKIP ›         this scene only: the shot in progress jumps to its end (where it leaves everyone), the line
    ///                  being spoken is cut, and the story goes on to its next scene. Only the button skips: a
    ///                  tap anywhere else belongs to the scene (a mash to break free must never skip what follows).
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
        static bool fastForward;              // TO PLAY: racing on until the player has the controls
        static bool toNextLine;
        static int skipStartFrame;               // SKIP: racing on until the next line of dialogue (or the controls)
        const float SkipRate = 30f;           // how fast the story runs while it's being skipped

        /// <summary>The story's clock: real seconds, or many of them a frame while a scene is being skipped (every
        /// story timer runs on it, so a skip takes the whole scene with it: the shots, the walks, the waits).</summary>
        public static float StoryDt => TimeController.Paused ? 0f : Time.unscaledDeltaTime * (Skipping ? SkipRate : 1f);
        static bool Skipping => fastForward || toNextLine;
        float ffAt = -9f, ffEndAt = -9f, ffVolume = 1f, skipTapAt = -9f;
        float skipAhead;
        float controlAt = -9f;                // the last moment the player had the controls (SKIP waits a beat after it)                      // story seconds run beyond real time during this skip (the score catches up by it)
        // the next beat of each clock (what a skip jumps to), refilled every frame by the At() checks
        float nextT = 1e9f, nextB = 1e9f, nextM = 1e9f, nextE = 1e9f, nextH = 1e9f;

        /// <summary>True for the one frame a skip was asked for (and every frame of a fast-forward).</summary>
        public static bool SkipNow => Skipping || skipFrame == Time.frameCount;

        static float NextBeat(float clock, float beat, float best) => beat > clock && beat < best ? beat : best;

        /// <summary>The player has the controls: a prompt is waiting for them, or the hero is free.</summary>
        bool PlayerHasControl => scAsk != ScAsk.None || hero == null || hero.IsDead
                                 || !string.IsNullOrEmpty(tapTitle) || kGapAwait || carPrompt       // the chapters' own tap moments
                                 || (!hero.CinematicLock && !SceneByItself);

        /// <summary>Phases that are scenes from first frame to last (whatever the hero's lock says): the
        /// opening, the breach until the men are let go, the memory, the walk to the hall, the ending.</summary>
        bool SceneByItself => phase == Phase.Cinematic || phase == Phase.Memory || phase == Phase.ToHall || phase == Phase.Ending
                              || (phase == Phase.Breach && b < ReleaseAt);

        /// <summary>A scene playing by itself, that SKIP may cut.</summary>
        bool AutoPlaying => hero != null && !PlayerHasControl && phase != Phase.End && !recapOn && !TimeController.Paused && blackout < 0.98f;

        /// <summary>Skip this scene: the story races on (behind a quick black, silent) to its next line of
        /// dialogue, which then plays as normal; or to the controls, if they come first.</summary>
        void SkipScene(bool fromGui)
        {
            if (Skipping) return;
            Sound.Play(Sfx.Whoosh, 0.35f, null, 1.3f);
            toNextLine = true;
            skipStartFrame = Time.frameCount;
            skipAhead = 0f;
            Sound.Skipping = true;
            ffAt = Time.unscaledTime;
            ffVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            subUntil = 0f;                                         // the line being spoken is cut
            skipTapAt = Time.unscaledTime;
        }

        /// <summary>A line is about to be spoken: a SKIP ends here (the new scene plays from its first line).</summary>
        void SkipReachedLine()
        {
            if (!toNextLine || Time.frameCount <= skipStartFrame) return;          // not the line we skipped from
            StopFastForward();
        }

        /// <summary>On to the next moment the player has the controls.</summary>
        void SkipToPlay()
        {
            // the opening has its own, proven way straight to the walk
            if (phase == Phase.Cinematic && !gateBusy) { Sound.SkipEnd(0f); SkipToWalk(); return; }   // (the opening's sounds stop with it)
            if (toNextLine) { toNextLine = false; fastForward = true; return; }
            fastForward = true;
            skipAhead = 0f;
            Sound.Skipping = true;
            ffAt = Time.unscaledTime;
            ffVolume = AudioListener.volume;
            AudioListener.volume = 0f;
            subUntil = 0f;
        }

        void StopFastForward()
        {
            if (!Skipping) return;
            fastForward = false;
            toNextLine = false;
            ffEndAt = Time.unscaledTime;
            Sound.SkipEnd(skipAhead);                                 // the score jumps to where the story is; the skipped scene's sounds stop
            AudioListener.volume = ffVolume;
        }

        /// <summary>Every frame (from Update): the fast-forward ends where the player has the controls.</summary>
        void UpdateSkip()
        {
            if (Skipping && !TimeController.Paused) skipAhead += Time.unscaledDeltaTime * (SkipRate - 1f);
            if (PlayerHasControl) controlAt = Time.unscaledTime;
            if (Skipping && (PlayerHasControl || phase == Phase.End || Time.unscaledTime - ffAt > (fastForward ? 25f : 8f))) StopFastForward();
        }

        void SkipGUI(float w, float h)
        {
            float now = Time.unscaledTime;
            if (Skipping || now - ffEndAt < 0.35f)
            {
                // the scenes running through, behind black
                float a = Skipping ? Mathf.Clamp01((now - ffAt) / 0.08f) : 1f - (now - ffEndAt) / 0.35f;
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
            // a beat after the player had the controls (a mash, a choice) before SKIP appears: a tap still on
            // its way down never lands on it and skips the very scene it set off
            if (Time.unscaledTime - controlAt < 1.0f) return;
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
