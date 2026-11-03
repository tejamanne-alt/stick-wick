using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum SpawnKind { LeftDoor, RightDoor, StairsTop, Background, Ceiling }

    /// <summary>A grunt. Enters from its spawn point, circles the hero at a distance and
    /// only attacks while holding one of the two attack tokens, so fights stay readable.
    /// Every attack is telegraphed with a red flash: dodge it, or strike to counter.</summary>
    public class Enemy : Fighter
    {
        public static readonly List<Enemy> All = new List<Enemy>();
        public static readonly Queue<GameObject> Corpses = new Queue<GameObject>();
        public static int MaxCorpses = 30;

        enum State { Enter, Approach, Wait, Advance, Windup, Attack, Recover, Hurt, Down, Dead }

        public float walkSpeed = 2.4f;
        public float windupSeconds = 0.55f;
        public static readonly Color TelegraphColor = new Color(0.85f, 0.08f, 0.05f);

        State state = State.Enter;
        float stateTime, ringDistance, waitUntil, enterZ;
        AttackData attack;
        bool hitDone;
        SpawnKind kind;
        HeroController hero;

        public bool HasToken { get; private set; }
        public bool Telegraphing => state == State.Windup;
        public bool Targetable => Alive && state != State.Down && state != State.Dead && (state != State.Enter || kind != SpawnKind.Background);
        public bool Finished => state == State.Dead;
        public bool Engaged => state == State.Advance || state == State.Windup || state == State.Attack;
        /// <summary>Carries a sword (set by the WaveDirector): cuts instead of punching.</summary>
        public bool Armed { get; set; }
        string IdleClip => Armed ? "sword_idle" : "idle";

        protected override void Awake()
        {
            base.Awake();
            ringDistance = Random.Range(2.3f, 3.6f);
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); AttackTokens.Release(this); }

        public void Init(HeroController h, SpawnKind spawnKind, float health)
        {
            hero = h;
            maxHealth = health;
            Health = health;
            kind = spawnKind;
            state = State.Enter;
            stateTime = 0f;
            enterZ = transform.position.z;
            if (kind == SpawnKind.Ceiling)
            {
                Grounded = false;
                velocity.y = -1f;
                body.Play("dodge", 0f); // flips down from above
            }
            else body.Play("run", 0.05f, true, 0.8f);
            FaceTowards(hero.transform.position.x);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            if (hero == null) return;
            float dx = hero.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            switch (state)
            {
                case State.Enter: UpdateEnter(dt, dx); break;
                case State.Approach:
                    FaceTowards(hero.transform.position.x);
                    if (dist > ringDistance + 0.3f) Walk(Mathf.Sign(dx));
                    else if (dist < ringDistance - 0.6f) Walk(-Mathf.Sign(dx) * 0.6f);
                    else GoWait();
                    break;
                case State.Wait:
                    FaceTowards(hero.transform.position.x);
                    velocity.x = 0f;
                    if (Mathf.Abs(dist - ringDistance) > 1.0f) { state = State.Approach; body.Play("run", 0.1f, false, 0.8f); }
                    else if (Time.time > waitUntil && !hero.IsDead && AttackTokens.TryTake(this))
                    {
                        HasToken = true;
                        state = State.Advance;
                        stateTime = 0f;
                        body.Play("run", 0.08f, false, 1.0f);
                    }
                    break;
                case State.Advance:
                    FaceTowards(hero.transform.position.x);
                    if (dist > 1.05f && stateTime < 2.5f) Walk(Mathf.Sign(dx) * 1.5f);
                    else BeginWindup();
                    break;
                case State.Windup:
                    velocity.x = 0f;
                    Tint(TelegraphColor, 0.05f);
                    if (stateTime >= windupSeconds) BeginAttack();
                    break;
                case State.Attack:
                    velocity.x = 0f;
                    if (!hitDone && body.Current != null && body.Current.hitTime >= 0f && body.Time >= body.Current.hitTime) ResolveHit();
                    if (body.Finished) { ReleaseToken(); state = State.Recover; stateTime = 0f; body.Play(IdleClip, 0.12f); }
                    break;
                case State.Recover:
                    if (dist < ringDistance) Walk(-Mathf.Sign(dx) * 0.7f); else velocity.x = 0f;
                    if (stateTime > 0.7f) GoWait();
                    break;
                case State.Hurt:
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 20f * dt);
                    if (stateTime > 0.38f) GoWait();
                    break;
                case State.Down:
                    if (Grounded) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 14f * dt);
                    if (Grounded && stateTime > 1.3f)
                    {
                        if (Alive) { GoWait(); body.Play(IdleClip, 0.3f); }
                        else { state = State.Dead; stateTime = 0f; }
                    }
                    break;
                case State.Dead:
                    // the fallen stay on the ground and pile up, as in the reference;
                    // the script switches off so a corpse costs nothing per frame
                    velocity.x = 0f;
                    if (Grounded && stateTime > 0.3f)
                    {
                        UpdateTint();
                        Corpses.Enqueue(gameObject);
                        while (Corpses.Count > MaxCorpses)
                        {
                            var old = Corpses.Dequeue();
                            if (old != null) Destroy(old);
                        }
                        enabled = false;
                        return;
                    }
                    break;
            }

            Integrate(dt);
            UpdateTint();
        }

        void UpdateEnter(float dt, float dx)
        {
            switch (kind)
            {
                case SpawnKind.Background:
                    // walk forward out of the background into the fight lane
                    var p = transform.position;
                    p.z = Mathf.MoveTowards(p.z, 0f, walkSpeed * dt);
                    transform.position = p;
                    velocity.x = Mathf.Sign(dx) * walkSpeed * 0.3f;
                    if (Mathf.Abs(p.z) < 0.02f) { state = State.Approach; }
                    break;
                case SpawnKind.Ceiling:
                    if (Grounded) { body.Play(IdleClip, 0.1f); GoWait(); }
                    break;
                default:
                    Walk(Mathf.Sign(dx));
                    if (stateTime > 0.8f) state = State.Approach;
                    break;
            }
        }

        protected override void OnLanded()
        {
            if (kind == SpawnKind.Ceiling && state == State.Enter && CameraDirector.Instance != null)
                CameraDirector.Instance.Shake(0.08f, 0.12f);
        }

        void Walk(float dirTimesSpeed)
        {
            velocity.x = dirTimesSpeed * walkSpeed;
            if (body.CurrentName != "run") body.Play("run", 0.1f, false, 0.8f);
        }

        void GoWait()
        {
            state = State.Wait;
            stateTime = 0f;
            waitUntil = Time.time + Random.Range(0.6f, 2.0f);
            if (body.CurrentName != IdleClip) body.Play(IdleClip, 0.15f);
        }

        void BeginWindup()
        {
            state = State.Windup;
            stateTime = 0f;
            velocity.x = 0f;
            int r = Random.Range(0, 3);
            attack = r == 0 ? AttackData.EnemyJab : r == 1 ? AttackData.EnemyCross : AttackData.EnemyKick;
            if (Armed) attack = AttackData.EnemySword;
            body.Play(Armed ? "sword_idle" : "windup", 0.08f);
        }

        void BeginAttack()
        {
            state = State.Attack;
            stateTime = 0f;
            hitDone = false;
            body.Play(attack.clip, 0.04f);
        }

        void ResolveHit()
        {
            hitDone = true;
            float dx = (hero.transform.position.x - transform.position.x) * Facing;
            float dy = Mathf.Abs(hero.transform.position.y - transform.position.y);
            if (dx > -0.1f && dx < attack.reach + 0.35f && dy < 0.9f)
            {
                hero.TakeHit(new HitInfo
                {
                    damage = attack.damage,
                    direction = Facing,
                    knockback = attack.knockback,
                    heavy = attack.clip == "front_kick",
                    attacker = this
                });
            }
        }

        /// <summary>Cancel a windup or attack (the hero countered).</summary>
        /// <summary>The gun-fu finishing shot on an enemy that is down.</summary>
        public void Execute(Fighter by)
        {
            if (!Alive) return;
            ReleaseToken();
            Health = 0f;
            Tint(Color.white, 0.06f);
            if (state != State.Down)
            {
                state = State.Down;
                stateTime = 0f;
                body.Play("knockdown", 0.05f);
            }
        }

        public void Interrupt()
        {
            if (state == State.Windup || state == State.Attack || state == State.Advance)
            {
                ReleaseToken();
                state = State.Hurt;
                stateTime = 0f;
            }
        }

        void ReleaseToken()
        {
            if (!HasToken) return;
            HasToken = false;
            AttackTokens.Release(this);
        }

        public override void TakeHit(HitInfo hit)
        {
            if (!Targetable) return;
            ReleaseToken();
            Health = Mathf.Max(0f, Health - hit.damage);
            Tint(Color.white, 0.06f);
            FaceTowards(hit.attacker != null ? hit.attacker.transform.position.x : transform.position.x - hit.direction);
            velocity.x = hit.direction * hit.knockback;
            if (hit.knockdown || !Alive)
            {
                state = State.Down;
                stateTime = 0f;
                velocity.y = hit.knockdown ? 5.5f : 3f;
                velocity.x = hit.direction * Mathf.Max(hit.knockback, 4f);
                Grounded = false;
                body.Play("knockdown", 0.04f);
            }
            else
            {
                state = State.Hurt;
                stateTime = 0f;
                bool fromFront = hit.direction == -Facing;
                body.Play(fromFront ? "hit_front" : "hit_back", 0.03f);
            }
        }
    }

    /// <summary>At most two enemies may attack at once; the rest circle and wait.</summary>
    public static class AttackTokens
    {
        public static int Max = 2;
        static readonly HashSet<Enemy> holders = new HashSet<Enemy>();

        public static bool TryTake(Enemy e)
        {
            holders.RemoveWhere(h => h == null || !h.Alive);
            if (holders.Contains(e)) return true;
            if (holders.Count >= Max) return false;
            holders.Add(e);
            return true;
        }

        public static void Release(Enemy e) { holders.Remove(e); }
        public static void Clear() { holders.Clear(); }
    }
}
