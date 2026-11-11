using UnityEngine;

namespace ProjectInk
{
    /// <summary>Swordsmen who fence (docs/design/backlog.md step 6), on the V4 body:
    ///   · the tell: up to jōdan (a high cut coming) or down to waki-gamae (a rising one), and his coming
    ///     cut shown as a red line through the hero (a thrust: a red point), for about 0.6 s
    ///   · the cuts: kesa, oroshi, yoko, gyaku, and the lunging thrust from further off
    ///   · feints: the line, the step, no cut (bait for an early parry)
    ///   · he reads you: the same line twice in a row and he parries the third
    ///   · parried, his blade is knocked wide and he's open (the hero's kaeshi)
    ///   · he drops his katana when he falls (WaveDirector.OnEnemyDied)</summary>
    public partial class Enemy
    {
        bool KatanaBrain => Type == EnemyType.Swordsman && hero != null && hero.Contact && body != null && body.Has("k_windup_high");
        RedLine redLine;
        string kCut, readCut;
        int readCount;
        float readAt;
        bool kParried, kFeint;

        public string KatanaCutName => kCut;

        /// <summary>His wind-up, katana style: the tell, the red line. False: not a katana man.</summary>
        bool KatanaWindup()
        {
            if (!KatanaBrain) return false;
            if (redLine == null) redLine = RedLine.Make(transform);
            kParried = false;
            float d = Mathf.Abs(hero.transform.position.x - transform.position.x);
            float r = Random.value;
            kCut = d > 1.6f ? "tsuki" : r < 0.3f ? "kesa" : r < 0.5f ? "oroshi" : r < 0.7f ? "yoko" : r < 0.88f ? "gyaku" : "tsuki";
            float dmg = kCut == "oroshi" ? 18f : kCut == "tsuki" ? 12f : 15f;
            attack = new AttackData { clip = kCut, damage = dmg, reach = kCut == "tsuki" ? 1.6f : 1.35f, knockback = 3f, hitStopFrames = 5, shake = 0.12f, heavy = true, react = "hit_chin" };
            windupSeconds = Boss ? 0.42f : 0.62f;
            kFeint = !Boss && Random.value < 0.18f;
            body.Play(kCut == "gyaku" ? "k_windup_low" : kCut == "tsuki" ? "katana_guard" : "k_windup_high", 0.1f);
            var at = hero.Chest + Vector3.up * 0.15f;
            if (kCut == "tsuki") redLine.ShowThrust(at);
            else
            {
                // the way his blade will travel, in the world, as the hero sees it
                Vector2 dir = kCut == "kesa" ? new Vector2(-Facing, -1f) : kCut == "oroshi" ? new Vector2(0f, -1f)
                            : kCut == "yoko" ? new Vector2(-Facing, 0f) : new Vector2(-Facing, 1f);
                redLine.ShowCut(at, dir, 1.4f);
            }
            Sound.Play(Sfx.SwordSwing, 0.35f, Chest, 1.5f);
            return true;
        }

        /// <summary>Every wind-up frame: the feint pulls up short.</summary>
        bool KatanaFeintNow()
        {
            if (!KatanaBrain || !kFeint || stateTime < windupSeconds * 0.8f) return false;
            kFeint = false;
            if (redLine != null) redLine.Hide();
            ReleaseToken();
            state = State.Recover;
            stateTime = 0f;
            recoverFor = 0.5f;
            body.Play("katana_guard", 0.12f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "A FEINT");
            return true;
        }

        /// <summary>His cut is coming and can still be met: the line it travels (world), or a thrust.</summary>
        public bool KatanaThreat(out Vector2 line, out bool thrust)
        {
            line = Vector2.zero; thrust = false;
            if (!KatanaBrain || redLine == null || !redLine.Showing || kParried) return false;
            bool live = state == State.Windup || (state == State.Attack && !hitDone);
            if (!live) return false;
            line = redLine.Dir;
            thrust = redLine.IsThrust;
            return true;
        }

        /// <summary>Met on his line: his blade knocked wide, open for a moment.</summary>
        public void KatanaParried(bool perfect)
        {
            kParried = true;
            if (redLine != null) redLine.Hide();
            ReleaseToken();
            chain.Clear(); chained = false; feinting = false;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = perfect ? 1.0f : 0.7f;
            readUntil = Time.time + hurtFor;
            velocity.x = -Facing * 1.2f;
            body.Play(body.Has("k_parried") ? "k_parried" : "hit_chin", 0.03f);
        }

        /// <summary>A cut coming at him: the same line a third time running, and he's ready for it.</summary>
        public bool ReadsCut(string cut)
        {
            if (!KatanaBrain) return false;
            if (cut == readCut && Time.time - readAt < 3f) readCount++; else readCount = 1;
            readCut = cut;
            readAt = Time.time;
            bool ready = state == State.Wait || state == State.Recover || state == State.Approach;
            if (readCount >= 3 && ready && !Dazed)
            {
                readCount = 0;
                string clip = cut == "gyaku" || cut == "kiriage" ? "k_parry_low" : cut.StartsWith("tsuki") ? "k_parry_rise" : "k_parry_high";
                body.Play(clip, 0.03f);
                state = State.Recover; stateTime = 0f; recoverFor = 0.3f;
                counterNext = true;
                return true;
            }
            return false;
        }

        /// <summary>After his cut lands (or misses): the line goes.</summary>
        void KatanaAfterHit()
        {
            if (redLine != null) redLine.Hide();
        }
    }
}
