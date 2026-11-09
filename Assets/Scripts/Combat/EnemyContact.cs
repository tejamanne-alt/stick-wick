using UnityEngine;

namespace ProjectInk
{
    /// <summary>CONTACT (story mode): what the hero's grappling and parries do to a man.
    ///  HELD     the hero has him in the clinch: he is posed by the hero's moves (clinch_v, the
    ///           victim half of a throw) and takes damage straight from them.
    ///  THROWN   the throw ends: his body is moved to where the clip put him (MoveSet endShift),
    ///           he lies there (on his back, or face down after a DDT) and gets up the way he fell.
    ///  PARRIED  his punch knocked aside: his chin up, then out on his feet for a moment (Dazed),
    ///           when the hero's next blow has a context finisher.</summary>
    public partial class Enemy
    {
        HeroController heldBy;
        float hurtFor = 0.35f, dazedUntil;
        string getupClip = "getup";
        float getupSpeed = 1f;

        /// <summary>Out on his feet (after a parry): the next blow can finish the job.</summary>
        public bool Dazed => Alive && Time.time < dazedUntil && state != State.Down && state != State.GetUp;
        public bool Held => heldBy != null && state == State.Scripted;
        public float HealthFraction => maxHealth > 0f ? Health / maxHealth : 0f;
        /// <summary>A man the hero can take hold of: on his feet, not a boss, not mid-cutscene.</summary>
        public bool CanBeHeld => Alive && !Boss && !Passive && Targetable && state != State.Scripted && !OnLedge;
        /// <summary>A punch the hero can knock aside: bare hands, coming now.</summary>
        public bool Parryable => Alive && !Boss && !Armed && Type != EnemyType.Piper && !Ranged &&
                                 (state == State.Windup || state == State.Attack);
        /// <summary>Too big to lift until he is worn down.</summary>
        public bool TooHeavy => Type == EnemyType.Brute && HealthFraction > 0.4f && !Dazed;

        public void BeginHeld(HeroController by)
        {
            ReleaseToken();
            state = State.Scripted;
            stateTime = 0f;
            velocity = Vector3.zero;
            heldBy = by;
            if (laser != null) laser.enabled = false;
            body.Play(body.Has("clinch_v") ? "clinch_v" : "hit_front", 0.08f);
        }

        /// <summary>Held at a spot, facing the man holding him.</summary>
        public void HeldAt(Vector3 at, float faceX)
        {
            transform.position = at;
            FaceTowards(faceX);
        }

        public void HeldPlay(string clip, float fade = 0.05f)
        {
            if (body.Has(clip)) body.Play(clip, fade, true, 1f);
        }

        /// <summary>A blow while held (a knee, a headbutt) or the impact of a throw.</summary>
        public void HeldDamage(float dmg)
        {
            bool wasAlive = Alive;
            Health = Mathf.Max(MinHealth, Health - dmg);
            Tint(Color.white, 0.07f);
            bool brute = Type == EnemyType.Brute;
            if (wasAlive) Sound.Play(Alive ? (brute ? Sfx.BruteHurt : Sfx.EnemyHurt) : (brute ? Sfx.BruteDeath : Sfx.EnemyDeath), 1f, Chest);
        }

        /// <summary>Let go standing (shoved off, or the hero was hit): back into the fight.</summary>
        public void ReleaseHeld(string clip = null)
        {
            if (!Held) { heldBy = null; return; }
            heldBy = null;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = 0.5f;
            if (!string.IsNullOrEmpty(clip) && body.Has(clip)) body.Play(clip, 0.05f);
            if (!Alive) { state = State.Down; body.Play("lie", 0.1f); }
        }

        /// <summary>The throw is over: put the body where it landed, on the floor.</summary>
        public void LandThrown(float shift, bool faceDown)
        {
            heldBy = null;
            var p = transform.position;
            p.x += Facing * shift;                    // endShift is along his own forward
            transform.position = p;
            velocity = Vector3.zero;
            Grounded = true;
            state = State.Down;
            stateTime = 0f;
            getupClip = faceDown ? "getup_face" : "getup";
            getupSpeed = faceDown ? 1.7f : 1f;
            string lieClip = faceDown ? "prone" : "lie";
            if (body.Has(lieClip)) body.Play(lieClip, 0f);
        }

        /// <summary>His punch knocked aside: head back, then out on his feet.</summary>
        public void Parried(int fromDir)
        {
            ReleaseToken();
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = 1.3f;
            dazedUntil = Time.time + 1.6f;
            velocity.x = fromDir * 1.5f;
            body.Play(body.Has("hit_chin") ? "hit_chin" : "hit_front", 0.03f);
        }

        /// <summary>The CONTACT hit reaction: the clip the blow asks for, held for its length.</summary>
        bool ContactReact(string react)
        {
            if (string.IsNullOrEmpty(react) || !body.Has(react)) return false;
            body.Play(react, 0.03f);
            hurtFor = react == "hit_body" ? 0.6f : react == "stumble_back" ? 0.75f : react == "hit_legs" ? 0.45f : 0.42f;
            return true;
        }

        /// <summary>Called every frame in the Hurt state.</summary>
        void UpdateHurtContact()
        {
            if (Dazed && body.Finished && body.Has("dazed") && body.CurrentName != "dazed") body.Play("dazed", 0.15f);
        }
    }
}
