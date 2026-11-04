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

        enum State { Enter, Approach, Wait, Advance, Windup, Attack, Recover, Hurt, Down, GetUp, Dead, Holding, Carried, Pounce,
                     Flee, BossIntro, Charge, Slam, Stunned, Enrage, Stagger, Scripted }

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
        public bool Telegraphing => state == State.Windup || (state == State.Pounce && !pounceLaunched) ||
                                    (state == State.Charge && !chargeRunning) || (state == State.Slam && !slamLeapt);
        public bool Targetable => Alive && state != State.Down && state != State.GetUp && state != State.Dead && state != State.Carried &&
                                  !(state == State.Enter && kind == SpawnKind.Background) && !Untouchable;
        /// <summary>Cutscene states: no one may hit him.</summary>
        bool Untouchable => state == State.BossIntro || state == State.Enrage || state == State.Stagger || state == State.Scripted;

        // ---- the boss (the brute): special moves, a second phase and a scripted finale
        public bool Boss { get; private set; }
        public bool Enraged { get; private set; }
        public bool Stunned => state == State.Stunned;
        public bool Charging => state == State.Charge;
        /// <summary>A henchman running for the exit now the boss is here.</summary>
        public bool Fleeing { get; private set; }
        bool finaleTriggered, enragePending, chargeHit, chargePassed, chargeRunning, slamLeapt, introWalking;
        float chargeLimit = float.NaN;
        float chargeDir, slamTargetX, nextSpecialAt, stepAt, starAt, recoverFor = 0.5f;
        int fleeDir, slamsLeft;
        float ChargeWind => Enraged ? 0.6f : 0.8f;
        static readonly Color EnragedColor = new Color(0.78f, 0.05f, 0.05f);
        public bool IsDown => state == State.Down || state == State.GetUp;
        public bool Engaged => state == State.Advance || state == State.Windup || state == State.Attack || state == State.Pounce;
        public bool Armed => Type == EnemyType.Swordsman;
        /// <summary>A training stunt double: walks up and takes the hits, never attacks.</summary>
        public bool Passive;
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
                Boss = true;
                transform.localScale = Vector3.one * 1.45f;
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
            if (state == State.Carried || state == State.Scripted) { UpdateTint(); return; }   // the hero swings us / a cutscene poses us
            if (UpdateLedge(dt)) { UpdateTint(); return; }
            float dx = hero.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            // the hall is one continuous walking surface (ground, stairs, deck): walking towards the
            // hero always gets there, so everyone is always on "the same level" for the brain.
            // (The old stair-lane / ledge travel code below is no longer reached.)
            bool sameLevel = !onStairs && !onStairsDown;
            if (LevelNav.Active)
            {
                // the tower: the same level means the same walkable region (catwalk + glass count as one)
                if (Grounded) navPlatform = LevelNav.PlatformAt(transform.position);
                int hp = hero.Platform;
                sameLevel = navPlatform >= 0 && hp >= 0 && LevelNav.Region(navPlatform) == LevelNav.Region(hp);
            }
            // the boss is here: whatever they were doing, the henchmen run
            if (Fleeing && (state == State.Enter || state == State.Approach || state == State.Wait || state == State.Advance ||
                            state == State.Windup || state == State.Attack || state == State.Recover || state == State.Holding))
                BeginFlee(false);
            if (Boss && enragePending && (state == State.Wait || state == State.Approach || state == State.Recover)) BeginEnrage();

            switch (state)
            {
                case State.Enter: UpdateEnter(dx); break;
                case State.Approach:
                    if (!sameLevel || onStairs) { if (LevelNav.Active) NavTravel(dt, dx); else Travel(dt); break; }
                    LeaveStairsLane(dt);
                    FaceTowards(hero.transform.position.x);
                    if (Boss && dist > 4f && dist < 13f && Time.time > nextSpecialAt && !hero.IsDead && Random.value < dt * 0.9f && AttackTokens.TryTake(this))
                    { HasToken = true; BeginCharge(dx); break; }
                    if (dist > ringDistance + 0.3f) Walk(Mathf.Sign(dx));
                    else if (dist < ringDistance - 0.7f) Walk(-Mathf.Sign(dx) * 0.6f);
                    else GoWait();
                    break;
                case State.Wait:
                    FaceTowards(hero.transform.position.x);
                    velocity.x = 0f;
                    if (!sameLevel || Mathf.Abs(dist - ringDistance) > 1.0f) { state = State.Approach; body.Play("run", 0.1f, false, 0.85f); }
                    else if (!Passive && Time.time > waitUntil && !hero.IsDead && AttackTokens.TryTake(this))
                    {
                        HasToken = true;
                        if (Boss && TryBossSpecial(dist, dx)) break;
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
                    if (Boss && recoverFor > 0.6f) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 14f * dt);   // a slow get-up: the opening
                    else if (dist < ringDistance && !Ranged) Walk(-Mathf.Sign(dx) * 0.7f); else velocity.x = 0f;
                    if (stateTime > recoverFor) { recoverFor = 0.5f; GoWait(); }
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
                case State.Flee: UpdateFlee(dt); break;
                case State.BossIntro: UpdateBossIntro(dx); break;
                case State.Charge: UpdateCharge(dt); break;
                case State.Slam: UpdateSlam(); break;
                case State.Stunned: UpdateStunned(dt); break;
                case State.Enrage:
                    velocity.x = 0f;
                    if (stateTime > 1.25f) GoWait();
                    break;
                case State.Stagger:
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 10f * dt);
                    break;
                case State.Dead:
                    velocity.x = 0f;
                    if (Grounded && stateTime > 0.3f) { Settle(); return; }
                    break;
            }

            if (laser != null && state != State.Windup) laser.enabled = false;
            if (projectile && !Grounded) BowlOthers();
            int wall = Integrate(dt);
            if (wall != 0 && IsDown && Mathf.Abs(velocity.x) > 4f && Mathf.Sign(velocity.x) == wall) WallSplat(wall);
            if (wall != 0 && state == State.Charge && chargeRunning && wall == (int)chargeDir) BeginStun(true);
            UpdateTint();
        }

        float attackReach() => (Type == EnemyType.Brute ? 1.3f : Armed || Type == EnemyType.Piper ? 1.45f : 1.05f) * BodyScale * transform.localScale.x;

        void UpdateEnter(float dx)
        {
            switch (kind)
            {
                case SpawnKind.Background:
                case SpawnKind.UpperDoor:
                    // step out of a back doorway (ground or deck) into the fight lane
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

        // ------------------------------------------------------------------ the tower: route-finding

        int navPlatform = -1;
        int stairDir;          // while on a flight of stairs: the way we are going

        /// <summary>Get to the hero's level: walk, climb the next ledge on the route, or drop
        /// through the floor (fighters with an attack token pounce straight onto the hero instead).
        /// Shooters and throwers just fire across levels when the hero is in range.</summary>
        void NavTravel(float dt, float dx)
        {
            if (!Grounded) return;
            LeaveStairsLane(dt);
            var p = transform.position;
            float dy = hero.transform.position.y - p.y;
            if (!Passive && Ranged && Mathf.Abs(dx) < 9f && Mathf.Abs(dy) < 6.5f) { FireAcrossLevels(); return; }

            int me = LevelNav.PlatformAt(p);
            if (me >= 0) stairDir = 0;
            if (me < 0) { Walk(stairDir != 0 ? stairDir : Mathf.Sign(dx)); return; }   // on a flight of stairs: keep going
            var step = LevelNav.Next(me, p.x, hero.Platform, hero.transform.position.x);
            if (step.kind == LevelNav.StepKind.None)
            {
                // no route right now (the glass is gone, say): wait by the hero's side of the platform
                var pl = LevelNav.Platforms[me];
                float wx = Mathf.Clamp(hero.transform.position.x, pl.x0 + 0.4f, pl.x1 - 0.4f);
                if (Mathf.Abs(wx - p.x) > 0.2f) Walk(Mathf.Sign(wx - p.x)); else { velocity.x = 0f; FaceTowards(hero.transform.position.x); }
                return;
            }
            float d = step.x - p.x;
            if (step.kind == LevelNav.StepKind.Walk) { Walk(Mathf.Sign(d)); return; }
            if (step.kind == LevelNav.StepKind.Stairs)
            {
                // walk to the foot (or head) of the flight, then straight on up (or down) it
                if (Mathf.Abs(d) > 0.2f && Mathf.Sign(d) != step.dir) { Walk(Mathf.Sign(d)); return; }
                stairDir = step.dir;
                Walk(step.dir);
                return;
            }
            if (Mathf.Abs(d) > 0.15f) { Walk(Mathf.Sign(d) * (Mathf.Abs(d) < 0.6f ? 0.6f : 1f)); return; }
            velocity.x = 0f;

            if (step.kind == LevelNav.StepKind.Climb)
            {
                var L = LevelNav.Ledges[step.ledge];
                BeginLedgeClimb(Random.Range(0.45f, 0.9f), L.x, L.y, L.dir);   // long enough to be stamped on
                return;
            }
            // drop. A fighter whose turn it is leaps down at the hero; the rest step off.
            bool heroBelowClose = hero.Grounded && Mathf.Abs(hero.transform.position.x - p.x) < 6.5f && dy < -1f;
            if (!Passive && !Ranged && leapDown && heroBelowClose && !hero.IsDead && AttackTokens.TryTake(this))
            {
                HasToken = true;
                state = State.Pounce;
                stateTime = 0f;
                pounceLaunched = false;
                body.Play(body.Has("duck") ? "duck" : IdleClip, 0.1f);
                return;
            }
            dropThroughUntil = Time.time + 0.3f;
            Grounded = false;
            velocity.y = 0.5f;
            body.Play("jump", 0.08f);
            Sound.Play(Sfx.Cloth, 0.8f, p);
        }

        /// <summary>A shooter or thrower with the hero on another level: stand and fire.</summary>
        void FireAcrossLevels()
        {
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
            float tx = LevelNav.Active ? hx - side * 0.75f : Mathf.Max(LedgeX + 0.5f, hx - side * 0.75f);
            if (LevelNav.Active) dropThroughUntil = Time.time + 0.25f;       // off a one-way platform, not back onto it
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
            if (Mathf.Abs(p.z) > 0.01f && kind != SpawnKind.Background && kind != SpawnKind.UpperDoor)
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
            // the catwalk's glass gives way under a hard landing
            if (LevelNav.Active && GlassPanel.Land(transform.position, fallHeight, (IsDown || Type == EnemyType.Brute) && fallHeight > 0.4f))
            {
                Grounded = false;
                if (IsDown || !Alive) Sound.Play(Sfx.FallScream, 0.8f, transform.position);
                return;
            }
            projectile = false;
            if (state == State.Slam && slamLeapt) { SlamImpact(); return; }
            if (state == State.Charge && chargeRunning) { BeginStun(false); return; }      // ran clean off the edge
            if (state == State.Enter && kind == SpawnKind.Ceiling && CameraDirector.Instance != null)
                CameraDirector.Instance.Shake(0.08f, 0.12f);
            if (IsDown || !Alive) Sound.Play(Sfx.BodyFall, Mathf.Clamp(0.6f + fallHeight * 0.15f, 0.6f, 1f), transform.position);
            else if (fallHeight > 0.8f) Sound.Play(Sfx.Land, 1f, transform.position);
            if (IsDown && fallHeight > 2.3f && Alive) Sound.Play(Sfx.Bone, 1f, transform.position);
            // knocked off the upper floor: the fall finishes them (the boss just takes a beating)
            if (IsDown && fallHeight > 2.3f && Alive && Boss)
            {
                float d = maxHealth * 0.12f;
                if (!FinalBlow(d, new HitInfo { direction = Facing })) Health = Mathf.Max(1f, Health - d);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.2f, 0.25f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(250, "FLOOR DROP");
            }
            else if (IsDown && fallHeight > 2.3f && Alive)
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
            if (Boss && finaleTriggered) return;
            velocity.x = -wall * 1.5f;
            float before = Health;
            if (FinalBlow(18f, new HitInfo { direction = -wall })) return;
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
            waitUntil = Time.time + (Boss ? (Enraged ? Random.Range(0.3f, 0.8f) : Random.Range(0.6f, 1.3f)) : Random.Range(0.3f, 1.2f));
            if (Fleeing) { BeginFlee(false); return; }
            if (Boss && enragePending) { BeginEnrage(); return; }
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
                    // the bear hug now and then: tap to break it
                    float r0 = Random.value;
                    attack = r0 < 0.2f ? AttackData.EnemyGrab : r0 < 0.6f ? AttackData.BruteSmash : AttackData.BruteKick;
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
                bool hit = dx > 0f && (dy < (LevelNav.Active ? 6.5f : 3.2f) || fromAbove) && !hero.Invulnerable;   // fires up or down levels too
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
            if (Boss && Enraged && Random.value < 0.5f) return;      // enraged, he powers through half the time
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

        // ------------------------------------------------------------------ the boss

        /// <summary>In the ring with a token: maybe a special instead of a plain swing.</summary>
        bool TryBossSpecial(float dist, float dx)
        {
            if (Time.time < nextSpecialAt || hero.IsDead) return false;
            float r = Random.value;
            if (dist > 3.2f && r < (Enraged ? 0.6f : 0.4f)) { BeginCharge(dx); return true; }
            if (dist < 6f && r < (Enraged ? 0.8f : 0.6f)) { BeginSlam(); return true; }
            return false;
        }

        /// <summary>BULL CHARGE: he paws the floor glowing red, then thunders across the room.
        /// Dodge through him (or jump over) and he runs on into the wall, dazed.</summary>
        void BeginCharge(float dx)
        {
            state = State.Charge;
            stateTime = 0f;
            chargeDir = dx >= 0f ? 1f : -1f;
            SetFacing((int)chargeDir);
            chargeHit = chargePassed = chargeRunning = false;
            velocity.x = 0f;
            // where he must pull up: the end of solid floor that is not a wall (the foot of a stair).
            // Off the end of a gallery he just keeps going, and comes down hard.
            chargeLimit = float.NaN;
            if (LevelNav.Active && navPlatform >= 0 && LevelNav.RegionBounds(navPlatform, out float ra, out float rb))
            {
                float lim = chargeDir > 0f ? rb : ra;
                int pl = LevelNav.PlatformAt(new Vector3(lim - chargeDir * 0.2f, transform.position.y, 0f));
                if (Mathf.Abs(lim) < 12.4f && (pl < 0 || !LevelNav.Platforms[pl].oneWay)) chargeLimit = lim - chargeDir * 0.3f;
            }
            body.Play("windup", 0.08f, true, 0.55f);
            Sound.Play(Sfx.BruteHurt, 0.9f, transform.position, 0.7f);    // a bellow
            nextSpecialAt = Time.time + (Enraged ? 2.6f : 4.2f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "BULL CHARGE!");
        }

        void UpdateCharge(float dt)
        {
            if (!chargeRunning)
            {
                velocity.x = 0f;
                Tint(TelegraphColor, 0.05f);
                if (Time.time > stepAt)
                {
                    stepAt = Time.time + 0.22f;     // pawing the floor
                    if (InkFx.Instance != null) InkFx.Instance.Splash(transform.position + Vector3.up * 0.05f, new Vector3(-chargeDir, 0.6f, 0f), 4, new Color(0.6f, 0.6f, 0.6f));
                    Sound.Play(Sfx.Footstep, 1f, transform.position, 0.7f);
                }
                if (stateTime >= ChargeWind)
                {
                    chargeRunning = true;
                    body.Play("run", 0.05f, false, 1.7f);
                    Sound.Play(Sfx.WhooshBig, 1f, transform.position, 0.8f);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(3f, 0.25f);
                }
                return;
            }
            velocity.x = chargeDir * (Enraged ? 11f : 9.5f);
            if (body.CurrentName != "run") body.Play("run", 0.05f, false, 1.7f);
            if (Time.time > stepAt)
            {
                stepAt = Time.time + 0.16f;
                Sound.Play(Sfx.Footstep, 1f, transform.position, 0.75f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.04f, 0.08f);
                StreakFx.Slash(Chest + new Vector3(-chargeDir * 1.2f, Random.Range(-0.5f, 0.5f), 0f), Vector3.right, 1.4f, 0.02f, 0.15f);
            }
            float rel = (hero.transform.position.x - transform.position.x) * chargeDir;
            float dy = Mathf.Abs(hero.transform.position.y - transform.position.y);
            if (!chargeHit && rel > -0.4f && rel < 0.9f && dy < 1.1f && !hero.IsDead)
            {
                if (hero.Invulnerable || (!hero.Grounded && dy > 0.6f))
                {
                    if (!chargePassed)
                    {
                        chargePassed = true;           // he went straight past: olé
                        if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("MATADOR!");
                        if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.5f);
                    }
                }
                else
                {
                    chargeHit = true;
                    hero.TakeHit(new HitInfo { damage = Enraged ? 24f : 20f, direction = (int)chargeDir, knockback = 12f, knockdown = true, heavy = true, attacker = this });
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.25f, 0.25f);
                    Sound.Play(Sfx.Boom, 0.6f, transform.position);
                    StopCharge();
                    return;
                }
            }
            if (stateTime - ChargeWind > 1.9f) StopCharge();
            else if (!float.IsNaN(chargeLimit) && (transform.position.x - chargeLimit) * chargeDir > 0f) StopCharge();   // skids to a stop
        }

        void StopCharge()
        {
            chargeRunning = false;
            ReleaseToken();
            state = State.Recover;
            stateTime = 0f;
            recoverFor = 0.5f;
            velocity.x = chargeDir * 3f;
            body.Play(IdleClip, 0.15f);
        }

        /// <summary>Head first into the wall (or off the edge onto the floor below): dazed, stars
        /// round his head, and every hit lands full for a couple of seconds.</summary>
        void BeginStun(bool wall)
        {
            chargeRunning = false;
            ReleaseToken();
            state = State.Stunned;
            stateTime = 0f;
            velocity.x = -chargeDir * (wall ? 3f : 0.5f);
            body.Play("hit_front", 0.03f, true, 0.8f);
            var at = Chest + Vector3.right * chargeDir * 0.5f;
            if (wall)
            {
                if (InkFx.Instance != null) { InkFx.Instance.Burst(at, 1.2f); InkFx.Instance.ImpactFlash(0.04f); }
                StreakFx.Sparks(at, new Vector3(-chargeDir, 0.5f, 0f), 18, 8f);
                Sound.Play(Sfx.WallSplat, 1f, at);
                Sound.Play(Sfx.MetalClang, 0.9f, at, 0.7f);
                Sound.Play(Sfx.Boom, 0.8f, at);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
                foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(0.8f);
            }
            else Sound.Play(Sfx.Land, 1f, transform.position, 0.6f);
            Sound.Play(Sfx.BruteHurt, 1f, Chest, 0.85f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(wall ? 0.3f : 0.18f, 0.35f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "STUNNED!  HIT HIM!");
        }

        void UpdateStunned(float dt)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 12f * dt);
            if (body.Finished && body.CurrentName != "idle") body.Play("idle", 0.2f, false, 0.35f);   // a slow, swaying daze
            if (Time.time > starAt)
            {
                starAt = Time.time + 0.1f;          // stars circling his head
                float a = Time.time * 7f;
                var head = transform.position + Vector3.up * 1.42f * transform.localScale.y;
                StreakFx.Sparks(head + new Vector3(Mathf.Cos(a) * 0.4f, 0.05f, Mathf.Sin(a) * 0.4f), Vector3.up, 1, 0.8f);
            }
            if (stateTime > (Enraged ? 1.8f : 2.4f)) { body.Play(IdleClip, 0.2f); GoWait(); }
        }

        /// <summary>GROUND SLAM: he crouches, leaps and comes down on the spot the hero stood,
        /// with a red ring showing where. The shockwave runs along the floor: be in the air.</summary>
        void BeginSlam()
        {
            state = State.Slam;
            stateTime = 0f;
            slamLeapt = false;
            slamsLeft = Enraged ? 2 : 1;           // enraged, he does it twice
            velocity.x = 0f;
            body.Play(body.Has("duck") ? "duck" : "windup", 0.1f);
            Sound.Play(Sfx.BruteHurt, 0.7f, transform.position, 0.9f);
            nextSpecialAt = Time.time + (Enraged ? 2.6f : 4.2f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "GROUND SLAM!  JUMP!");
        }

        void UpdateSlam()
        {
            if (!slamLeapt)
            {
                velocity.x = 0f;
                Tint(TelegraphColor, 0.05f);
                FaceTowards(hero.transform.position.x);
                if (stateTime >= (Enraged ? 0.45f : 0.6f)) SlamLeap();
                return;
            }
            if (stateTime > 3f) { slamLeapt = false; GoWait(); }    // safety
        }

        float SlamRadius => Enraged ? 3.8f : 3.2f;

        void SlamLeap()
        {
            slamLeapt = true;
            stateTime = 0f;
            float tx = hero.transform.position.x;
            if (LevelNav.Active && navPlatform >= 0 && LevelNav.RegionBounds(navPlatform, out float x0, out float x1))
                tx = Mathf.Clamp(tx, x0 + 0.6f, x1 - 0.6f);
            slamTargetX = tx;
            const float up = 9.5f;
            float air = 2f * up / gravity;
            Grounded = false;
            velocity.y = up;
            velocity.x = Mathf.Clamp((tx - transform.position.x) / air, -7f, 7f);
            body.Play("jump", 0.05f, true, 1f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position, 0.75f);
            // the warning: a red ring spreads where he will come down
            StreakFx.Ring(new Vector3(tx, transform.position.y, 0f), SlamRadius, air * 0.9f, 0.05f, TelegraphColor);
        }

        void SlamImpact()
        {
            var p = transform.position;
            float radius = SlamRadius;
            var fx = InkFx.Instance;
            if (fx != null)
            {
                fx.Burst(p + Vector3.up * 0.05f, 2.2f);
                fx.Splash(p + Vector3.up * 0.1f, new Vector3(1f, 0.5f, 0f), 14, new Color(0.62f, 0.62f, 0.62f));
                fx.Splash(p + Vector3.up * 0.1f, new Vector3(-1f, 0.5f, 0f), 14, new Color(0.62f, 0.62f, 0.62f));
                fx.ImpactFlash(0.04f);
            }
            StreakFx.Ring(p, radius, 0.32f, 0.09f);
            StreakFx.Sparks(p, Vector3.up, 14, 6f);
            Sound.Play(Sfx.Boom, 1f, p);
            Sound.Play(Sfx.Land, 1f, p, 0.6f);
            Sound.Play(Sfx.MetalClang, 0.6f, p, 0.7f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.32f, 0.35f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(5);
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
            float hx = hero.transform.position.x - p.x, hy = hero.transform.position.y - p.y;
            if (Mathf.Abs(hx) < radius && Mathf.Abs(hy) < 0.6f && !hero.IsDead)
            {
                if (!hero.Grounded) { if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("JUMPED IT!"); }
                else if (!hero.Invulnerable)
                    hero.TakeHit(new HitInfo { damage = Enraged ? 20f : 16f, direction = hx >= 0f ? 1 : -1, knockback = 9f, knockdown = true, heavy = true, attacker = this });
            }
            body.Play(body.Has("duck") ? "duck" : IdleClip, 0.04f);
            if (--slamsLeft > 0 && !hero.IsDead)
            {
                state = State.Slam;           // and again
                slamLeapt = false;
                stateTime = 0.25f;
                return;
            }
            slamLeapt = false;
            ReleaseToken();
            state = State.Recover;           // slow to rise: the opening
            stateTime = 0f;
            recoverFor = 1.0f;
            velocity.x = 0f;
        }

        /// <summary>Phase two, at half health: a roar that blows the hero back, and he turns
        /// blood red, faster, charging more and slamming twice.</summary>
        void BeginEnrage()
        {
            enragePending = false;
            Enraged = true;
            ReleaseToken();
            state = State.Enrage;
            stateTime = 0f;
            velocity.x = 0f;
            walkSpeed = 2.7f;
            windupSeconds = 0.42f;
            InkColor = EnragedColor;
            SetBaseColor(EnragedColor);
            FaceTowards(hero.transform.position.x);
            body.Play("windup", 0.1f, true, 0.4f);
            Sound.Play(Sfx.BruteHurt, 1f, Chest, 0.55f);
            Sound.PlayLater(Sfx.BruteLaugh, 0.7f, 1f, transform.position);
            Sound.Play(Sfx.CineMassive, 0.7f);
            Sound.Play(Sfx.Boom, 0.7f);
            var fx = InkFx.Instance;
            if (fx != null) { fx.ImpactFlash(0.05f); fx.Burst(Chest, 1.6f); }
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                StreakFx.Slash(Chest + d * 1.4f, d, 2.2f, 0.03f, 0.45f);
            }
            StreakFx.Ring(transform.position, 4f, 0.45f);
            if (CameraDirector.Instance != null) { CameraDirector.Instance.Shake(0.25f, 0.9f); CameraDirector.Instance.Focus(transform, 1.3f); }
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.8f);
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
            // the roar blows him back
            float hx = hero.transform.position.x - transform.position.x;
            if (Mathf.Abs(hx) < 3.5f && Mathf.Abs(hero.transform.position.y - transform.position.y) < 1f && hero.Grounded && !hero.Invulnerable)
                hero.TakeHit(new HitInfo { damage = 4f, direction = hx >= 0f ? 1 : -1, knockback = 10f, attacker = this });
            if (WaveDirector.Instance != null) WaveDirector.Instance.Announce("ENRAGED", 1.4f);
            nextSpecialAt = Time.time + 1.6f;
        }

        /// <summary>The blow that would kill the boss does not: he staggers at 1 health and the
        /// finale cinematic takes over. Returns true if it caught the blow.</summary>
        bool FinalBlow(float dmg, HitInfo hit)
        {
            if (!Boss || finaleTriggered || Health - dmg > 0f || BossDirector.Instance == null) return false;
            finaleTriggered = true;
            Health = 1f;
            ReleaseToken();
            if (OnLedge) DropFromLedge();
            if (laser != null) laser.enabled = false;
            chargeRunning = slamLeapt = false;
            state = State.Stagger;
            stateTime = 0f;
            velocity.x = hit.direction * 2.5f;
            if (velocity.y > 0f) velocity.y = 0f;
            body.Play("hit_front", 0.03f, true, 0.6f);
            Tint(Color.white, 0.1f);
            Sound.Play(Sfx.BruteHurt, 1f, Chest, 0.75f);
            BossDirector.Instance.BeginFinale(this);
            return true;
        }

        // --- the entrance (driven by the BossDirector)
        public void BeginBossIntro()
        {
            ReleaseToken();
            state = State.BossIntro;
            stateTime = 0f;
            introWalking = true;
            body.Play("run", 0.1f, false, 0.42f);
        }

        void UpdateBossIntro(float dx)
        {
            LeaveStairsLane(Time.deltaTime);
            if (introWalking && Mathf.Abs(dx) > 4.2f)
            {
                FaceTowards(hero.transform.position.x);
                velocity.x = Mathf.Sign(dx) * 1.35f;              // a slow, heavy walk
                if (body.CurrentName != "run") body.Play("run", 0.1f, false, 0.42f);
                if (Time.time > stepAt)
                {
                    stepAt = Time.time + 0.62f;
                    Sound.Play(Sfx.Land, 1f, transform.position, 0.55f);
                    Sound.Play(Sfx.Boom, 0.3f, transform.position);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.05f, 0.1f);
                    if (InkFx.Instance != null) InkFx.Instance.Splash(transform.position + Vector3.up * 0.05f, Vector3.up, 4, new Color(0.6f, 0.6f, 0.6f));
                }
            }
            else
            {
                velocity.x = 0f;
                if (introWalking) { introWalking = false; body.Play(IdleClip, 0.2f); }
            }
        }

        /// <summary>The roar on his name card.</summary>
        public void IntroRoar()
        {
            introWalking = false;
            velocity.x = 0f;
            FaceTowards(hero.transform.position.x);
            body.Play("windup", 0.1f, true, 0.45f);
            Sound.Play(Sfx.BruteHurt, 1f, Chest, 0.6f);
            Sound.PlayLater(Sfx.BruteLaugh, 0.9f, 1f, transform.position);
        }

        public void IntroTaunt() { body.Play("taunt", 0.15f); }

        /// <summary>Cutscene over: fight.</summary>
        public void Engage()
        {
            if (state != State.BossIntro) return;
            state = State.Approach;
            stateTime = 0f;
            nextSpecialAt = Time.time + 2f;
            body.Play("run", 0.1f, false, 0.85f);
        }

        // --- the finale (posed frame by frame by the BossDirector)
        public void Puppet()
        {
            ReleaseToken();
            state = State.Scripted;
            stateTime = 0f;
            velocity = Vector3.zero;
            if (laser != null) laser.enabled = false;
        }
        public void PuppetPose(Vector3 at) { transform.position = at; }
        public void PuppetPlay(string clip, float fade = 0.05f, float speed = 1f) { if (body.Has(clip)) body.Play(clip, fade, true, speed); }
        public void PuppetFlash() { Tint(Color.white, 0.07f); }
        public void PuppetDie()
        {
            Health = 0f;
            velocity = Vector3.zero;
            Die();
        }

        // --- henchmen
        float fleeFromX;

        /// <summary>The boss is here: run for the exit. Hit one on the way out for COWARD!</summary>
        public void Flee(Vector3 from)
        {
            if (Boss || !Alive || Fleeing || Passive || state == State.Dead) return;
            Fleeing = true;
            ReleaseToken();
            if (laser != null) laser.enabled = false;
            fleeFromX = from.x;
            fleeDir = transform.position.x >= from.x ? 1 : -1;
            if (state == State.Down || state == State.GetUp || state == State.Carried || state == State.Pounce || OnLedge) return;  // they go when they can
            BeginFlee(true);
        }

        void BeginFlee(bool flinch)
        {
            ReleaseToken();
            state = State.Flee;
            stateTime = flinch ? 0f : 0.35f;
            if (flinch)
            {
                body.Play("hit_front", 0.05f);        // a jolt of fear
                Sound.Play(Random.value < 0.5f ? Sfx.EnemyHurt : Sfx.FallScream, 0.6f, Chest);
            }
        }

        void UpdateFlee(float dt)
        {
            if (stateTime < 0.35f) { velocity.x = 0f; FaceTowards(fleeFromX); return; }
            LeaveStairsLane(dt);
            SetFacing(fleeDir);
            velocity.x = fleeDir * walkSpeed * 1.7f;
            if (body.CurrentName != "run") body.Play("run", 0.08f, false, 1.35f);
            var x = transform.position.x;
            if ((fleeDir > 0 ? x > arenaMaxX - 0.5f : x < arenaMinX + 0.5f) || stateTime > 7f) Vanish();
        }

        /// <summary>Gone (out of a door, or just out of the fight). Not a kill.</summary>
        public void Vanish()
        {
            if (InkFx.Instance != null) InkFx.Instance.Burst(Chest, 0.5f);
            Sound.Play(Sfx.Door, 0.6f, transform.position);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ taking hits

        public override void TakeHit(HitInfo hit)
        {
            if (Untouchable || state == State.Dead) return;
            if (!Targetable && !hit.hitsDowned && !OnLedge) return;
            if (OnLedge) { DropFromLedge(); }
            bool armored = Boss && ((state == State.Charge && chargeRunning) || state == State.Slam || state == State.Stunned);
            if (!armored) ReleaseToken();
            float dmg = hit.damage;
            bool brute = Type == EnemyType.Brute;
            if (Boss && state == State.Stunned) dmg *= 1.5f;           // dazed against the wall: everything lands full
            else if (brute && !hit.heavy) dmg *= 0.6f;          // the brute shrugs off light hits
            if (FinalBlow(dmg, hit)) return;                        // the killing blow becomes the finale
            bool wasAlive = Alive;
            Health = Mathf.Max(0f, Health - dmg);
            Tint(Color.white, 0.06f);
            if (wasAlive)
            {
                if (!Alive) Sound.Play(brute ? Sfx.BruteDeath : Sfx.EnemyDeath, 1f, Chest);
                else Sound.Play(brute ? Sfx.BruteHurt : Sfx.EnemyHurt, 1f, Chest);
            }
            if (Boss && !Enraged && !enragePending && Health <= maxHealth * 0.5f) enragePending = true;
            if (armored)
            {
                // charging, slamming or dazed: he takes it without flinching
                if (state == State.Stunned) Tint(Color.white, 0.08f);
                return;
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
            if (Untouchable || (Boss && ((state == State.Charge && chargeRunning) || state == State.Slam))) return;   // not mid-special or mid-cutscene
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
            if (Boss)
            {
                TakeHit(new HitInfo { damage = 35f, heavy = true, hitsDowned = true, direction = by != null && by.transform.position.x > transform.position.x ? -1 : 1, attacker = by });
                return;
            }
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
            hit.damage = Boss ? maxHealth * 0.12f : 9999f;     // no one-shotting the boss
            hit.heavy = true;
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
