using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum SpawnKind { LeftDoor, RightDoor, UpperDoor, Background, Ceiling }
    public enum EnemyType { Brawler, Swordsman, Gunner, Brute, Thrower, Piper }

    /// <summary>An enemy. Comes in through a door (or drops from above), finds its way to the
    /// hero's floor (up the stairs, or leaping off the upper floor), circles at a distance and
    /// attacks only while holding an attack token. Every attack is telegraphed: a red flash for
    /// melee, a red laser sight for gunners. Dodge it, or strike first to counter.
    /// Brawlers punch and kick, swordsmen cut, gunners keep their distance and shoot, the brute
    /// is a giant that shrugs off light hits, throwers hurl knives (catchable), pipers swing steel
    /// pipes. Brawlers sometimes grab and hold the hero. Some climb by hanging from the ledge.
    /// Enemies can be thrown as projectiles (human bowling) or carried in a giant swing.
    /// Each has its own colour, which it leaves behind.</summary>
    public class Enemy : Fighter
    {
        public static readonly List<Enemy> All = new List<Enemy>();
        public static readonly Queue<GameObject> Corpses = new Queue<GameObject>();
        public static int MaxCorpses = 40;

        enum State { Enter, Approach, Wait, Advance, Windup, Attack, Recover, Hurt, Down, GetUp, Dead, Holding, Carried, Pounce }

        public float walkSpeed = 2.6f;
        public float windupSeconds = 0.45f;
        public static readonly Color TelegraphColor = new Color(0.9f, 0.08f, 0.05f);

        // building layout (set by the WaveDirector from the scene)
        public static float StairsBottomX = 5.6f, StairsTopX = -0.8f, StairsLaneZ = 1.3f, LedgeX = 0f;

        State state = State.Enter;
        float stateTime, ringDistance, waitUntil;
        AttackData attack;
        bool hitDone, onStairs, tauntedThisWait, useLedge, projectile;
        Fighter launchedBy;
        Material weaponMat;
        SpawnKind kind;
        HeroController hero;
        LineRenderer laser;

        public EnemyType Type { get; private set; }
        public Color InkColor { get; private set; } = Color.black;
        public bool HasToken { get; private set; }
        public bool Telegraphing => state == State.Windup || (state == State.Pounce && !pounceLaunched);
        public bool Targetable => Alive && state != State.Down && state != State.GetUp && state != State.Dead && state != State.Carried &&
                                  !(state == State.Enter && kind == SpawnKind.Background);
        public bool IsDown => state == State.Down || state == State.GetUp;
        public bool Engaged => state == State.Advance || state == State.Windup || state == State.Attack || state == State.Pounce;
        public bool Armed => Type == EnemyType.Swordsman;
        public bool Gunner => Type == EnemyType.Gunner;
        public bool Ranged => Type == EnemyType.Gunner || Type == EnemyType.Thrower;
        public bool Carried => state == State.Carried;
        public bool HoldingHero => state == State.Holding;
        string IdleClip => Armed || Type == EnemyType.Piper ? "sword_idle" : Gunner ? "gun_idle" : "idle";

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); AttackTokens.Release(this); if (laser != null) laser.enabled = false; }

        public void Init(HeroController h, SpawnKind spawnKind, EnemyType type, float health, Color color, Material lineMaterial)
        {
            hero = h;
            Type = type;
            maxHealth = health;
            Health = health;
            kind = spawnKind;
            InkColor = color;
            SetBaseColor(color);
            // they arrive with a sound: a door banging open, or the ceiling hatch
            Sound.Play(spawnKind == SpawnKind.Ceiling ? Sfx.MetalClang : Sfx.Door, 1f, transform.position);
            ringDistance = Gunner ? Random.Range(4.5f, 7f) : Type == EnemyType.Thrower ? Random.Range(4f, 6f) : Random.Range(2.2f, 3.4f);
            useLedge = Random.value < 0.55f;
            leapDown = type != EnemyType.Brute && Random.value < 0.7f;
            perchX = Random.Range(-2.8f, -0.6f);
            weaponMat = hero.weaponMaterial;
            if (type == EnemyType.Brute)
            {
                transform.localScale = Vector3.one * 1.35f;
                walkSpeed = 1.9f;
                windupSeconds = 0.6f;
                ringDistance = 2.4f;
            }
            if (Gunner && lineMaterial != null)
            {
                laser = new GameObject("Laser").AddComponent<LineRenderer>();
                laser.transform.SetParent(transform, false);
                laser.sharedMaterial = lineMaterial;
                laser.positionCount = 2;
                laser.widthMultiplier = 0.025f;
                laser.startColor = laser.endColor = new Color(1f, 0.1f, 0.05f, 0.85f);
                laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                laser.enabled = false;
            }
            state = State.Enter;
            stateTime = 0f;
            if (kind == SpawnKind.Ceiling)
            {
                Grounded = false;
                velocity.y = -1f;
                body.Play("front_flip", 0f); // flips down from above
            }
            else body.Play("run", 0.05f, true, 0.85f);
            FaceTowards(hero.transform.position.x);
        }

        // ------------------------------------------------------------------ brain

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            if (hero == null) return;
            if (state == State.Carried) { UpdateTint(); return; }          // the hero swings us around
            if (UpdateLedge(dt)) { UpdateTint(); return; }
            float dx = hero.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            // same floor as the hero's (stable) floor; mid-stairs counts as neither
            bool sameLevel = Level == hero.Floor && !onStairs && !onStairsDown && Mathf.Abs(hero.transform.position.y - transform.position.y) < 2.4f;

            switch (state)
            {
                case State.Enter: UpdateEnter(dx); break;
                case State.Approach:
                    if (!sameLevel || onStairs) { Travel(dt); break; }
                    LeaveStairsLane(dt);
                    FaceTowards(hero.transform.position.x);
                    if (dist > ringDistance + 0.3f) Walk(Mathf.Sign(dx));
                    else if (dist < ringDistance - 0.7f) Walk(-Mathf.Sign(dx) * 0.6f);
                    else GoWait();
                    break;
                case State.Wait:
                    FaceTowards(hero.transform.position.x);
                    velocity.x = 0f;
                    if (!sameLevel || Mathf.Abs(dist - ringDistance) > 1.0f) { state = State.Approach; body.Play("run", 0.1f, false, 0.85f); }
                    else if (Time.time > waitUntil && !hero.IsDead && AttackTokens.TryTake(this))
                    {
                        HasToken = true;
                        state = Ranged ? State.Windup : State.Advance;
                        stateTime = 0f;
                        if (Ranged) BeginWindup(); else body.Play("run", 0.08f, false, 1.1f);
                    }
                    else if (!tauntedThisWait && stateTime > 0.4f && Random.value < 0.004f)
                    {
                        tauntedThisWait = true;
                        body.Play("taunt", 0.15f);
                    }
                    break;
                case State.Advance:
                    FaceTowards(hero.transform.position.x);
                    if (!sameLevel) { ReleaseToken(); state = State.Approach; break; }
                    if (dist > attackReach() - 0.25f && stateTime < 2.5f) Walk(Mathf.Sign(dx) * 1.6f);
                    else BeginWindup();
                    break;
                case State.Windup:
                    velocity.x = 0f;
                    Tint(TelegraphColor, 0.05f);
                    if (Gunner) AimLaser();
                    if (stateTime >= (Gunner ? 0.75f : Type == EnemyType.Thrower ? 0.55f : windupSeconds)) BeginAttack();
                    break;
                case State.Attack:
                    velocity.x = 0f;
                    if (!hitDone && body.Current != null && body.Current.hitTime >= 0f && body.Time >= body.Current.hitTime) ResolveAttack();
                    if (body.Finished) { ReleaseToken(); state = State.Recover; stateTime = 0f; body.Play(IdleClip, 0.12f); }
                    break;
                case State.Holding:
                    velocity.x = 0f;
                    FaceTowards(hero.transform.position.x);
                    if (!hero.IsGrabbedBy(this)) { ReleaseToken(); GoWait(); }
                    break;
                case State.Recover:
                    if (dist < ringDistance && !Ranged) Walk(-Mathf.Sign(dx) * 0.7f); else velocity.x = 0f;
                    if (stateTime > 0.5f) GoWait();
                    break;
                case State.Hurt:
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 20f * dt);
                    if (stateTime > 0.35f) GoWait();
                    break;
                case State.Down:
                    if (Grounded) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 14f * dt);
                    if (Grounded && stateTime > 1.0f)
                    {
                        if (Alive) { state = State.GetUp; stateTime = 0f; body.Play("getup", 0.1f); }
                        else Die();
                    }
                    break;
                case State.GetUp:
                    velocity.x = 0f;
                    if (body.Finished) { GoWait(); body.Play(IdleClip, 0.15f); }
                    break;
                case State.Pounce: UpdatePounce(); break;
                case State.Dead:
                    velocity.x = 0f;
                    if (Grounded && stateTime > 0.3f) { Settle(); return; }
                    break;
            }

            if (laser != null && state != State.Windup) laser.enabled = false;
            if (projectile && !Grounded) BowlOthers();
            int wall = Integrate(dt);
            if (wall != 0 && IsDown && Mathf.Abs(velocity.x) > 4f && Mathf.Sign(velocity.x) == wall) WallSplat(wall);
            UpdateTint();
        }

        float attackReach() => (Type == EnemyType.Brute ? 1.3f : Armed || Type == EnemyType.Piper ? 1.45f : 1.05f) * BodyScale * transform.localScale.x;

        void UpdateEnter(float dx)
        {
            switch (kind)
            {
                case SpawnKind.Background:
                    // step out of the back door into the fight lane
                    var p = transform.position;
                    p.z = Mathf.MoveTowards(p.z, 0f, walkSpeed * Time.deltaTime);
                    transform.position = p;
                    velocity.x = Mathf.Sign(dx) * walkSpeed * 0.3f;
                    if (Mathf.Abs(p.z) < 0.02f) state = State.Approach;
                    break;
                case SpawnKind.Ceiling:
                    if (Grounded) { body.Play(IdleClip, 0.1f); GoWait(); }
                    break;
                default:
                    Walk(Mathf.Sign(dx));
                    if (stateTime > 0.7f) state = State.Approach;
                    break;
            }
        }

        /// <summary>Find a way to the hero's floor: up the stairs (a lane behind the fight lane),
        /// or off the upper floor's open edge.</summary>
        void Travel(float dt)
        {
            var p = transform.position;
            if (!Grounded) return;                      // mid-leap: let the flip play out
            bool heroAbove = hero.transform.position.y > p.y + 1.4f;
            if (onStairs)
            {
                // climbing: step sideways into the stairs lane first, then walk up it
                p.z = Mathf.MoveTowards(p.z, StairsLaneZ, 3f * dt);
                transform.position = p;
                if (Mathf.Abs(p.z - StairsLaneZ) > 0.05f && p.y < 0.2f) { velocity.x = 0f; return; }
                Walk(Mathf.Sign(StairsTopX - p.x));
                if (p.x <= StairsTopX + 0.2f && p.y > 2.8f) onStairs = false;
                return;
            }
            if (heroAbove && useLedge && p.y < 0.3f)
            {
                // run to the ledge, jump, hang by the hands and pull up
                LeaveStairsLane(dt);
                float fx = LedgeFootX;
                Walk(Mathf.Sign(fx - p.x));
                if (Mathf.Abs(fx - p.x) < 0.25f && Mathf.Abs(p.z) < 0.05f) BeginLedgeClimb(Random.Range(0.4f, 0.8f));
                return;
            }
            if (heroAbove)
            {
                // walk to the foot of the stairs (moving into the stairs lane), then climb
                float tx = StairsBottomX;
                Walk(Mathf.Sign(tx - p.x));
                if (Mathf.Abs(tx - p.x) < 0.35f) { onStairs = true; velocity.x = 0f; }
            }
            else
            {
                // hero below. Shooters hold the balcony and fire down; fighters take a turn to
                // pounce off the edge onto him; the rest (and anyone with no clear drop) take the stairs
                bool heroInOpen = hero.transform.position.x > LedgeX - 0.2f;     // not under the upper floor
                if (Ranged && heroInOpen && !onStairsDown) { HoldBalcony(dt); return; }
                if (leapDown && heroInOpen && hero.transform.position.x < 7f && !onStairsDown) { ToTheEdge(dt); return; }
                StairsDown(dt);
            }
        }

        // ------------------------------------------------------------------ the upper floor

        bool leapDown, onStairsDown, pounceLaunched;
        float perchX;
        int lastPerchStep;

        /// <summary>Shooters and knife throwers take a spot by the rail and attack from above.</summary>
        void HoldBalcony(float dt)
        {
            LeaveStairsLane(dt);
            var p = transform.position;
            if (Mathf.Abs(perchX - p.x) > 0.15f) { Walk(Mathf.Sign(perchX - p.x)); return; }
            velocity.x = 0f;
            FaceTowards(hero.transform.position.x);
            if (body.CurrentName == "run") body.Play(IdleClip, 0.15f);
            if (Time.time > waitUntil && !hero.IsDead && AttackTokens.TryTake(this))
            {
                HasToken = true;
                state = State.Windup;
                stateTime = 0f;
                BeginWindup();
            }
            else if (Time.time > waitUntil + 3f) waitUntil = Time.time + Random.Range(0.5f, 1.5f);
        }

        /// <summary>Fighters go to the open edge and wait their turn there, taunting, then pounce.</summary>
        void ToTheEdge(float dt)
        {
            LeaveStairsLane(dt);
            var p = transform.position;
            // stagger along the rail so they do not stand inside each other
            float spot = LedgeX - 0.35f - 0.7f * (lastPerchStep = RailSlot());
            if (Mathf.Abs(spot - p.x) > 0.15f) { Walk(Mathf.Sign(spot - p.x) * 1.2f); return; }
            velocity.x = 0f;
            FaceTowards(hero.transform.position.x);
            if (lastPerchStep == 0 && Time.time > waitUntil && !hero.IsDead && AttackTokens.TryTake(this))
            {
                HasToken = true;
                state = State.Pounce;
                stateTime = 0f;
                pounceLaunched = false;
                body.Play(body.Has("duck") ? "duck" : IdleClip, 0.1f);       // crouch at the edge: about to jump
                return;
            }
            if (body.CurrentName == "run") body.Play(IdleClip, 0.15f);
            if (stateTime > 1.2f && Random.value < 0.01f) { body.Play("taunt", 0.15f); stateTime = 0f; }
        }

        /// <summary>This enemy's place in the queue at the rail (0 = at the edge).</summary>
        int RailSlot()
        {
            int ahead = 0;
            foreach (var e in All)
            {
                if (e == this || !e.Alive || e.Level != 1 || e.Ranged || !e.leapDown) continue;
                if (e.transform.position.x > transform.position.x + 0.05f) ahead++;
            }
            return Mathf.Min(ahead, 3);
        }

        /// <summary>The pounce: a red-flashing crouch at the edge, then a diving kick that lands
        /// beside the hero. It hits if he is still there; a dodge leaves them sprawling.</summary>
        void UpdatePounce()
        {
            if (!pounceLaunched)
            {
                velocity.x = 0f;
                FaceTowards(hero.transform.position.x);
                Tint(TelegraphColor, 0.05f);
                if (stateTime >= 0.55f) LaunchPounce();
                return;
            }
            if (Grounded && stateTime > 0.12f) LandPounce();
        }

        void LaunchPounce()
        {
            pounceLaunched = true;
            stateTime = 0f;
            var p = transform.position;
            float hx = hero.transform.position.x;
            int side = hx >= p.x ? 1 : -1;
            float tx = Mathf.Max(LedgeX + 0.5f, hx - side * 0.75f);
            float ty = GroundY(new Vector3(tx, 0.5f, 0f), 0.4f);
            float T = Mathf.Clamp(Mathf.Abs(tx - p.x) / 6.5f, 0.5f, 0.85f);
            velocity.x = (tx - p.x) / T;
            velocity.y = (ty - p.y + 0.5f * gravity * T * T) / T;
            Grounded = false;
            body.Play("flying_kick", 0.05f, true, 0.9f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position);
            Sound.Play(Sfx.EnemyEffort, 1f, transform.position);
        }

        void LandPounce()
        {
            float dx = hero.transform.position.x - transform.position.x;
            float dy = Mathf.Abs(hero.transform.position.y - transform.position.y);
            velocity.x = 0f;
            if (Mathf.Abs(dx) < 1.2f && dy < 1f && !hero.Invulnerable && !hero.IsDead)
            {
                hero.TakeHit(new HitInfo { damage = 14f, direction = dx >= 0 ? 1 : -1, knockback = 6f, heavy = true, attacker = this });
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.15f);
            }
            ReleaseToken();
            state = State.Recover;
            stateTime = 0f;
            body.Play(IdleClip, 0.12f);
        }

        /// <summary>Walk down: to the top of the stairs, into the stairs lane, down, and out.</summary>
        void StairsDown(float dt)
        {
            var p = transform.position;
            if (!onStairsDown)
            {
                LeaveStairsLane(dt);
                float tx = StairsTopX;
                if (Mathf.Abs(tx - p.x) > 0.3f) { Walk(Mathf.Sign(tx - p.x)); return; }
                onStairsDown = true;
            }
            p.z = Mathf.MoveTowards(p.z, StairsLaneZ, 3f * dt);
            transform.position = p;
            if (Mathf.Abs(p.z - StairsLaneZ) > 0.05f && p.y > 2.8f) { velocity.x = 0f; return; }
            Walk(Mathf.Sign(StairsBottomX - p.x));
            if (p.x >= StairsBottomX - 0.25f && p.y < 0.3f) onStairsDown = false;
        }

        void LeaveStairsLane(float dt)
        {
            var p = transform.position;
            if (Mathf.Abs(p.z) > 0.01f && kind != SpawnKind.Background)
            {
                p.z = Mathf.MoveTowards(p.z, 0f, 3f * dt);
                transform.position = p;
            }
        }

        protected override void OnLedgeClimbed()
        {
            state = State.Approach;
            stateTime = 0f;
            body.Play("run", 0.1f, false, 0.85f);
        }

        protected override void OnLanded(float fallHeight)
        {
            projectile = false;
            if (state == State.Enter && kind == SpawnKind.Ceiling && CameraDirector.Instance != null)
                CameraDirector.Instance.Shake(0.08f, 0.12f);
            if (IsDown || !Alive) Sound.Play(Sfx.BodyFall, Mathf.Clamp(0.6f + fallHeight * 0.15f, 0.6f, 1f), transform.position);
            else if (fallHeight > 0.8f) Sound.Play(Sfx.Land, 1f, transform.position);
            if (IsDown && fallHeight > 2.3f && Alive) Sound.Play(Sfx.Bone, 1f, transform.position);
            // knocked off the upper floor: the fall finishes them
            if (IsDown && fallHeight > 2.3f && Alive)
            {
                Health = 0f;
                if (InkFx.Instance != null)
                {
                    InkFx.Instance.Splash(transform.position + Vector3.up * 0.3f, Vector3.up, 40, InkColor);
                    InkFx.Instance.Stain(transform.position, InkColor, 1.6f, false);
                }
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(this, "FLOOR DROP", 250);
            }
            else if (state != State.Down && state != State.Dead && body.CurrentName == "front_flip" && state != State.Enter)
                body.Play("run", 0.1f, false, 0.85f);
        }

        void WallSplat(int wall)
        {
            velocity.x = -wall * 1.5f;
            float before = Health;
            Health = Mathf.Max(0f, Health - 18f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(Chest, new Vector3(-wall, 0.4f, 0f), 30, InkColor);
                InkFx.Instance.StainWall(transform.position.x + wall * 0.2f, Chest.y, InkColor, 1.4f);
            }
            Sound.Play(Sfx.WallSplat, 1f, Chest);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.14f, 0.15f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.OnEnvironmentKill(this, before > 0f && Health <= 0f ? "WALL SPLAT KILL" : "WALL SPLAT", before > 0f && Health <= 0f ? 220 : 120);
        }

        void Walk(float dirTimesSpeed)
        {
            velocity.x = dirTimesSpeed * walkSpeed;
            if (body.CurrentName != "run") body.Play("run", 0.1f, false, 0.85f);
        }

        void GoWait()
        {
            state = State.Wait;
            stateTime = 0f;
            tauntedThisWait = false;
            waitUntil = Time.time + (Type == EnemyType.Brute ? Random.Range(0.6f, 1.4f) : Random.Range(0.3f, 1.2f));
            if (body.CurrentName != IdleClip) body.Play(IdleClip, 0.15f);
        }

        // ------------------------------------------------------------------ attacking

        void BeginWindup()
        {
            state = State.Windup;
            stateTime = 0f;
            velocity.x = 0f;
            switch (Type)
            {
                case EnemyType.Swordsman:
                    attack = AttackData.EnemySword;
                    body.Play("sword_idle", 0.08f);
                    break;
                case EnemyType.Gunner:
                    attack = AttackData.EnemyShot;
                    body.Play("gun_aim", 0.08f);
                    break;
                case EnemyType.Brute:
                    attack = Random.value < 0.5f ? AttackData.BruteSmash : AttackData.BruteKick;
                    body.Play("windup", 0.08f);
                    break;
                case EnemyType.Thrower:
                    attack = AttackData.EnemyKnife;
                    body.Play("idle", 0.08f);
                    break;
                case EnemyType.Piper:
                    attack = AttackData.EnemyPipe;
                    body.Play("sword_idle", 0.08f);
                    break;
                default:
                    int r = Random.Range(0, 4);
                    attack = r == 0 ? AttackData.EnemyJab : r == 1 ? AttackData.EnemyCross : r == 2 ? AttackData.EnemyKick : AttackData.EnemyGrab;
                    body.Play("windup", 0.08f);
                    break;
            }
        }

        void AimLaser()
        {
            if (laser == null) return;
            var muzzle = Chest + Vector3.up * 0.1f + Vector3.right * Facing * 0.55f;
            laser.enabled = true;
            laser.SetPosition(0, muzzle);
            laser.SetPosition(1, hero.Chest);
        }

        void BeginAttack()
        {
            state = State.Attack;
            stateTime = 0f;
            hitDone = false;
            body.Play(attack.clip, 0.04f);
            if (Type == EnemyType.Swordsman) Sound.Play(Sfx.SwordSwing, 0.8f, transform.position);
            else if (!Gunner && Type != EnemyType.Thrower) Sound.Play(Sfx.Whoosh, 0.8f, transform.position, 0.92f);
            if (Type == EnemyType.Brute) Sound.Play(Sfx.BruteHurt, 0.6f, transform.position, 0.8f);   // a roar into the swing
            else if (Random.value < 0.25f) Sound.Play(Sfx.EnemyEffort, 1f, transform.position);
        }

        void ResolveAttack()
        {
            hitDone = true;
            float dx = (hero.transform.position.x - transform.position.x) * Facing;
            float dy = Mathf.Abs(hero.transform.position.y - transform.position.y);
            if (Type == EnemyType.Thrower)
            {
                var from = Chest + Vector3.up * 0.2f + Vector3.right * Facing * 0.4f;
                EnemyKnife.Throw(this, hero, from, hero.Chest, weaponMat);
                Sound.Play(Sfx.Knife, 1f, from);
                return;
            }
            if (attack.clip == "grab_attack")
            {
                if (dx > -0.1f && dx < Reach(attack) + 0.3f && dy < 0.8f && hero.GetGrabbed(this))
                {
                    state = State.Holding;
                    stateTime = 0f;
                    body.Play("hold_victim", 0.06f);
                }
                return;
            }
            if (Gunner)
            {
                var from = Chest + Vector3.up * 0.1f + Vector3.right * Facing * 0.55f;
                bool fromAbove = transform.position.y - hero.transform.position.y > 2f;   // shooting down from the balcony
                bool hit = dx > 0f && (dy < 1.2f || fromAbove) && !hero.Invulnerable;
                if (InkFx.Instance != null) InkFx.Instance.Shot(from, hit ? hero.Chest : from + Vector3.right * Facing * 16f);
                hit &= !hero.Ducking;
                if (hit) hero.TakeHit(new HitInfo { damage = attack.damage, direction = Facing, knockback = attack.knockback, attacker = this });
                return;
            }
            float reach = Reach(attack) + 0.3f;
            if (dx > -0.1f && dx < reach && dy < 0.9f)
            {
                hero.TakeHit(new HitInfo
                {
                    damage = attack.damage,
                    direction = Facing,
                    knockback = attack.knockback,
                    heavy = attack.heavy,
                    attacker = this
                });
            }
        }

        /// <summary>Cancel a windup or attack (the hero countered or vaulted).</summary>
        public void Interrupt()
        {
            if (state == State.Windup || state == State.Attack || state == State.Advance || (state == State.Pounce && !pounceLaunched))
            {
                ReleaseToken();
                state = State.Hurt;
                stateTime = 0f;
                body.Play("hit_front", 0.05f);
            }
        }

        void ReleaseToken()
        {
            if (!HasToken) return;
            HasToken = false;
            AttackTokens.Release(this);
        }

        // ------------------------------------------------------------------ taking hits

        public override void TakeHit(HitInfo hit)
        {
            if (!Targetable && !hit.hitsDowned && !OnLedge) return;
            if (OnLedge) { DropFromLedge(); }
            ReleaseToken();
            float dmg = hit.damage;
            bool brute = Type == EnemyType.Brute;
            if (brute && !hit.heavy) dmg *= 0.6f;          // the brute shrugs off light hits
            bool wasAlive = Alive;
            Health = Mathf.Max(0f, Health - dmg);
            Tint(Color.white, 0.06f);
            if (wasAlive)
            {
                if (!Alive) Sound.Play(brute ? Sfx.BruteDeath : Sfx.EnemyDeath, 1f, Chest);
                else Sound.Play(brute ? Sfx.BruteHurt : Sfx.EnemyHurt, 1f, Chest);
            }
            FaceTowards(hit.attacker != null ? hit.attacker.transform.position.x : transform.position.x - hit.direction);
            velocity.x = hit.direction * hit.knockback * (brute ? 0.4f : 1f);
            bool knock = (hit.knockdown && (!brute || dmg >= 25f || Health <= 0f)) || !Alive;
            if (knock)
            {
                state = State.Down;
                stateTime = 0f;
                velocity.y = hit.lift > 0f ? hit.lift * (brute ? 0.5f : 1f) : hit.knockdown ? 5.5f : 3f;
                velocity.x = hit.direction * Mathf.Max(hit.knockback, 4f) * (brute ? 0.6f : 1f);
                Grounded = false;
                body.Play("knockdown", 0.04f);
            }
            else if (!brute || dmg >= 15f)
            {
                state = State.Hurt;
                stateTime = 0f;
                bool fromFront = hit.direction == -Facing;
                body.Play(fromFront ? "hit_front" : "hit_back", 0.03f);
            }
        }

        /// <summary>Thrown by the hero: flies, knocking down whoever it hits (human bowling).</summary>
        public void Launch(Vector3 vel, Fighter by)
        {
            ReleaseToken();
            state = State.Down;
            stateTime = 0f;
            transform.rotation = Quaternion.Euler(0f, vel.x >= 0f ? 180f : 0f, 0f);
            velocity = vel;
            Grounded = false;
            projectile = true;
            launchedBy = by;
            body.Play("knockdown", 0.03f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position);
            Sound.Play(Sfx.FallScream, 0.8f, transform.position);
        }

        void BowlOthers()
        {
            foreach (var other in All.ToArray())
            {
                if (other == this || !other.Targetable) continue;
                if (Vector3.Distance(other.Chest, transform.position + Vector3.up * 0.35f) < 0.85f)
                {
                    other.TakeHit(new HitInfo { damage = 22f, direction = velocity.x >= 0 ? 1 : -1, knockback = 7f, knockdown = true, heavy = true, attacker = launchedBy });
                    if (InkFx.Instance != null) InkFx.Instance.Splash(other.Chest, velocity.normalized, 24, other.InkColor);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(other, "HUMAN BOWLING", 180);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.15f, 0.15f);
                    if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
                }
            }
        }

        /// <summary>Picked up by the legs for a giant swing (the hero moves us).</summary>
        public void BeginCarried()
        {
            ReleaseToken();
            state = State.Carried;
            stateTime = 0f;
            velocity = Vector3.zero;
            body.Play("knockdown", 0.05f);
        }

        public void EndCarried(Vector3 vel, Fighter by)
        {
            Launch(vel, by);
        }

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

        /// <summary>Instant kill (rampage finishers, thrown weapons).</summary>
        public void Kill(HitInfo hit)
        {
            hit.damage = 9999f;
            hit.knockdown = true;
            hit.hitsDowned = true;
            TakeHit(hit);
        }

        void Die()
        {
            state = State.Dead;
            stateTime = 0f;
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnemyDied(this);
        }

        void Settle()
        {
            // the fallen stay where they lie, and the building keeps their colour
            UpdateTint();
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Stain(transform.position + Vector3.right * Random.Range(-0.3f, 0.3f), InkColor, Random.Range(1.0f, 1.6f), false);
                if (Random.value < 0.6f) InkFx.Instance.StainWall(transform.position.x + Random.Range(-0.8f, 0.8f), transform.position.y + Random.Range(0.4f, 1.8f), InkColor, Random.Range(0.6f, 1.3f));
            }
            Corpses.Enqueue(gameObject);
            while (Corpses.Count > MaxCorpses)
            {
                var old = Corpses.Dequeue();
                if (old != null) Destroy(old);
            }
            enabled = false;
        }
    }

    /// <summary>Only a few enemies may attack at once; the rest circle, taunt and wait.</summary>
    public static class AttackTokens
    {
        public static int Max = 3;
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
