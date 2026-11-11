using UnityEngine;

namespace ProjectInk
{
    /// <summary>What a man's body is telling the hero right now (docs/design/combat_v3.md, "read the
    /// man"). Every one is a pose held long enough to read on a phone, and each one opens a door:
    ///   GuardHigh   hands glued to his head          → tap LOW: under the guard
    ///   GuardLow    elbows down over his belly       → tap HIGH: over the top
    ///   Folded      doubled over a body shot         → tap HIGH: the rising knee
    ///   HeadBack    head thrown back, arms wide      → tap LOW: the liver
    ///   Stumbling   staggering back                  → tap LOW: the push kick sends him over
    ///   Overreach   his punch missed, falling in     → tap HIGH: the overhand counter
    ///   Buckled     the leg gave, the head dropped   → tap HIGH: the head kick (TIMBER)
    ///   Pinned      his back to the wall             → every blow harder; the finisher: he slides down it</summary>
    public enum Opening { None, GuardHigh, GuardLow, Folded, HeadBack, Stumbling, Overreach, Buckled, Pinned }

    public partial class Enemy
    {
        int guardLevel;                 // 0 none, 1 high shell, 2 low guard
        float guardSwitchAt, readUntil;

        public Opening Read
        {
            get
            {
                if (!Alive || Held || state == State.Down || state == State.GetUp || state == State.Dead || state == State.Unaware) return Opening.None;
                if (state == State.Hurt && Time.time < readUntil + 0.05f)
                {
                    switch (body.CurrentName)
                    {
                        case "fold": return Opening.Folded;
                        case "head_back": return Opening.HeadBack;
                        case "stumble_back": return Opening.Stumbling;
                        case "overreach": return Opening.Overreach;
                        case "leg_buckle": return Opening.Buckled;
                    }
                }
                if (Pinned && (state == State.Hurt || state == State.Wait || state == State.Recover)) return Opening.Pinned;
                if (state == State.Wait && guardLevel == 1 && body.CurrentName == "cover_high") return Opening.GuardHigh;
                if (state == State.Wait && guardLevel == 2 && body.CurrentName == "cover_low") return Opening.GuardLow;
                return Opening.None;
            }
        }

        /// <summary>His back to the wall (the zone's end): nowhere to go.</summary>
        public bool Pinned
        {
            get
            {
                float wall = Facing > 0 ? arenaMinX : arenaMaxX;
                return Mathf.Abs(transform.position.x - wall) < 0.8f;
            }
        }

        /// <summary>Waiting his turn: a guard you can read, high or low, changed every few seconds.</summary>
        void UpdateGuardStance()
        {
            if (!StoryBrain || !body.Has("cover_high")) return;
            if (Time.time >= guardSwitchAt)
            {
                float r = Random.value;
                guardLevel = r < 0.3f ? 0 : r < 0.68f ? 1 : 2;
                guardSwitchAt = Time.time + Random.Range(1.6f, 3.0f);
            }
            string want = guardLevel == 1 ? "cover_high" : guardLevel == 2 ? "cover_low" : "idle";
            string cur = body.CurrentName;
            if (cur != want && (cur == "idle" || cur == "cover_high" || cur == "cover_low" || cur == "block_hit" || body.Finished))
                body.Play(want, 0.22f);
        }

        /// <summary>The hero slipped his punch: it hits air and he falls in after it.</summary>
        public void Overreached()
        {
            if (!StoryBrain || Boss || !body.Has("overreach") || (state != State.Windup && state != State.Attack)) return;
            ReleaseToken();
            chain.Clear(); chained = false; feinting = false;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = 0.75f;
            readUntil = Time.time + hurtFor;
            velocity.x = 0f;
            body.Play("overreach", 0.04f);
        }
    }
}
