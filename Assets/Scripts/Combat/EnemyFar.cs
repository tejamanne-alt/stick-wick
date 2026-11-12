using UnityEngine;

namespace ProjectInk
{
    /// <summary>Enemies and the far game: a man on watch who hasn't seen the hero yet (stealth), and
    /// men who pick up the chapter's props and throw them at him.
    ///
    /// ON WATCH (Unaware): he stands at his post facing one way, smoking, bored (the "unaware" clip).
    /// He sees the hero when:
    ///   · the hero is in front of him within 7 m (a crouched hero further than 4 m is not seen),
    ///   · the hero walks up behind him standing within 1.6 m, or runs within 5 m (the footsteps),
    ///   · something loud happens within 9 m (a fight, a throw, a heavy landing),
    ///   · he is hit.
    /// Seen: "Hey!", he turns and joins the fight. Crouched and behind him, the hero can choke him out.</summary>
    public partial class Enemy
    {
        public bool Unaware => state == State.Unaware;
        Throwable heldProp;
        float propCheckAt, throwAt;
        bool propThrown;
        static float lastEnemyThrowAt = -10f;
        /// <summary>The last loud moment (a blow, a throw, a hard landing) and where.</summary>
        public static float NoiseAt = -10f;
        public static Vector3 NoisePos;

        public static void Noise(Vector3 at) { NoiseAt = Time.time; NoisePos = at; }
        /// <summary>A man above (the upper road, the gatehouse) who throws down at the hero below.</summary>
        public bool ThrowsDown;
        /// <summary>One of Voss's best (the observatory): faster lines, more feints.</summary>
        public bool Master;

        /// <summary>Put him there, ready to fight (the fortress's levels place their men).</summary>
        public void PlaceAt(Vector3 at, int facing)
        {
            ReleaseToken();
            transform.position = at;
            velocity = Vector3.zero;
            SetFacing(facing);
            state = State.Approach;
            stateTime = 0f;
            body.Play(IdleClip, 0.1f);
        }

        /// <summary>Post him here, facing `facing`, not knowing the hero is coming.</summary>
        public void PostUnaware(Vector3 at, int facing)
        {
            ReleaseToken();
            transform.position = at;
            velocity = Vector3.zero;
            SetFacing(facing);
            state = State.Unaware;
            stateTime = 0f;
            body.Play(body.Has("unaware") ? "unaware" : "idle", 0.1f, false, Random.Range(0.85f, 1.1f));
        }

        void UpdateUnaware(float dt)
        {
            velocity.x = 0f;
            if (hero == null || hero.IsDead) return;
            float dx = hero.transform.position.x - transform.position.x;
            float d = Mathf.Abs(dx);
            bool sameFloor = Mathf.Abs(hero.transform.position.y - transform.position.y) < 1.4f;
            bool inFront = Mathf.Sign(dx) == Facing;
            bool seen = sameFloor && inFront && d < 7f && !(hero.Crouching && d > 4f);
            bool heard = sameFloor && !inFront && ((d < 1.6f && !hero.Crouching) || (d < 5f && hero.RunningFast));
            bool noise = Time.time - NoiseAt < 0.3f && Vector3.Distance(NoisePos, transform.position) < 9f;
            if (seen || heard || noise) Alert();
        }

        /// <summary>He's seen him: turn and fight.</summary>
        public void Alert()
        {
            if (state != State.Unaware) return;
            state = State.Wait;
            stateTime = 0f;
            waitUntil = Time.time + 0.6f;
            if (hero != null) FaceTowards(hero.transform.position.x);
            body.Play(body.Has("hit_back") ? "hit_back" : "idle", 0.06f);       // a start
            Sound.Play(Sfx.EnemyEffort, 1.1f, Chest);
            if (WaveDirector.Instance != null) WaveDirector.Instance.ShowBanner("HEY!", 0.8f);
            // the shout carries: the others on watch nearby hear it
            Noise(transform.position);
        }

        /// <summary>Choked out from behind (the hero's takedown): held, then laid down, gone.</summary>
        public void TakenDown()
        {
            HeldDamage(Health + 1f);
        }

        /// <summary>Bleach in the eyes, a torch in the face: out of it, his hands at his eyes.</summary>
        public void Blind(float seconds)
        {
            if (!Alive || state == State.Scripted || state == State.Down || state == State.GetUp || state == State.Dead) return;
            if (Boss && seconds > 1.2f) seconds = 1.2f;
            ReleaseToken();
            chain.Clear(); chained = false; feinting = false;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = seconds;
            dazedUntil = Time.time + seconds;
            body.Play(body.Has("dazed") ? "dazed" : "hit_chin", 0.05f);
        }

        // ------------------------------------------------------------------ throwing the chapter's props
        /// <summary>While waiting his turn in the ring (or closing in): take a prop he's standing by,
        /// and throw it from range. One throw in the room every few seconds, never a boss.</summary>
        bool TryThrowProp(float dist, bool sameLevel)
        {
            if (Boss || Passive || NoAttack || Ranged || Armed || hero == null || hero.IsDead) return false;
            if (Time.time < propCheckAt) return false;
            propCheckAt = Time.time + 0.4f;
            if (heldProp == null)
            {
                var p = Throwable.Near(transform.position, 0.7f);
                if (p == null || p.kind == PropKind.Chain || Random.value > 0.5f) return false;
                var hand = Throwable.FindBone(body.transform, "hand.R");
                if (hand == null) return false;
                p.TakeInto(this, hand);
                heldProp = p;
                Sound.Play(Sfx.Pickup, 0.8f, Chest);
                return false;
            }
            if ((!sameLevel && !ThrowsDown) || dist < 2.2f || dist > 11f || Time.time - lastEnemyThrowAt < (ThrowsDown ? 2.2f : 3.5f)) return false;
            lastEnemyThrowAt = Time.time;
            ReleaseToken();
            state = State.Throwing;
            stateTime = 0f;
            propThrown = false;
            velocity.x = 0f;
            FaceTowards(hero.transform.position.x);
            bool v4 = body.Has("throw_object");
            body.Play(v4 ? "throw_object" : "windup", 0.08f, true, 1f);
            var e = body.Current;
            throwAt = v4 && e != null && e.hitTime > 0f ? e.hitTime : 0.35f;
            Sound.Play(Sfx.EnemyEffort, 1f, Chest);
            return true;
        }

        void UpdateThrowing()
        {
            velocity.x = 0f;
            if (!propThrown && stateTime >= throwAt)
            {
                propThrown = true;
                if (heldProp != null)
                {
                    var from = heldProp.transform.position;
                    var k = heldProp.kind;
                    Destroy(heldProp.gameObject);
                    heldProp = null;
                    ThrownProp.Launch(k, this, hero, from, Facing);
                    Sound.Play(Sfx.WhooshBig, 0.9f, from);
                }
            }
            if (stateTime > throwAt + 0.4f && (body.Finished || stateTime > 1.2f)) { state = State.Wait; stateTime = 0f; waitUntil = Time.time + 0.8f; body.Play("idle", 0.15f); }
        }

        /// <summary>Hit or killed holding a prop: it drops.</summary>
        public void DropProp()
        {
            if (heldProp == null) return;
            var k = heldProp.kind;
            var at = heldProp.transform.position;
            Destroy(heldProp.gameObject);
            heldProp = null;
            Throwable.Spawn(k, new Vector3(at.x, transform.position.y, 0f));
        }
    }
}
