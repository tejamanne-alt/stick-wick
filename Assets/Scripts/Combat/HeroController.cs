using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum FightStyle { Fists, Sword, Gun, Knife, Pipe }

    /// <summary>The player's stickman. The player picks direction and timing; this script picks
    /// the target, dashes, leaps or climbs to it, and chooses the move.
    ///
    /// Fists  motion-captured martial arts; back kick behind; grab-and-throw (human bowling);
    ///        stomp or giant-swing an enemy on the floor.
    /// Knife  caught from a thrower or picked up: stab up close, throw at range.
    /// Pipe   taken from a piper: heavy staff swings that send enemies flying.
    /// Sword  taken from a swordsman: cleaving cuts, back slash, leaping slam.
    /// Gun    taken from a gunner: shots at range (behind without turning), pistol-whip and
    ///        throw up close, then a finishing shot.
    /// Weapons wear out and are thrown (also on swipe down) as a spinning kill.
    ///
    /// Movement and defence: climb to the upper floor by hanging from the ledge, dive down from
    /// it; dodge into a red flash = vault over the attacker; dodge towards an enemy = slide kick
    /// under its guard; dodge towards a wall = wall flip; swipe down with empty hands = duck
    /// (knives and shots pass over); tap as a knife arrives = catch it and throw it back;
    /// grabbed = tap to break free with a reversal throw. Full Flow = Rampage.</summary>
    public partial class HeroController : Fighter
    {
        enum State { Idle, Dash, Attack, Dodge, Hurt, Dead, Swing, Grabbed, Duck, ToLedge, Air, Rope, Bar, WallRun, Scissors, Dive, Clinch }

        public float targetRange = 9f;
        public float gunRange = 14f;
        public float closeQuarters = 2.0f;
        public float dashSpeed = 20f;
        public float comboWindow = 0.9f;
        public float rampageSeconds = 5f;
        public float runSpeed = 5.2f;
        public float jumpSpeed = 8.2f;
        const float HandReach = 1.43f;            // hands above the feet, arms up

        [Header("Weapons (set by FighterFactory)")]
        public GameObject sword;
        public GameObject pistol;
        public GameObject knife;
        public GameObject pipe;
        public TrailRenderer swordTrail;
        public TrailRenderer pipeTrail;
        public Transform muzzle;
        public Material weaponMaterial;
        public Material trailMaterial;

        State state = State.Idle;
        AttackData currentAttack;
        Enemy target, pendingExecution, downTarget, grabber;
        bool hitDone, isCounter, throwPending, knifeThrowPending, catchPending;
        int attackDir, swingSide, grabTaps, vaultFacing;
        float stateTime, invulnerableUntil, lastAttackEnd, rampageUntil, swingAngle, grabTick;
        int comboStep, closeStep;
        TrailRenderer[] limbTrails;
        readonly HashSet<Enemy> swingHit = new HashSet<Enemy>();
        Swingable holding;
        Crate pendingCrate;
        OilDrum pendingDrum;
        float swingAng, swingVel, swingTurned;
        int wallDir;
        bool running;

        public FightStyle Style { get; private set; } = FightStyle.Fists;
        public int Ammo { get; private set; }
        public int SwordUses { get; private set; }
        public int PipeUses { get; private set; }
        public int Knives { get; private set; }
        public bool Reloading => false;
        public bool InRampage => Time.unscaledTime < rampageUntil;
        public float RampageLeft => Mathf.Max(0f, rampageUntil - Time.unscaledTime);
        public int ComboCount { get; private set; }
        public float ComboTimer { get; private set; }
        public bool IsDead => state == State.Dead;
        public bool Ducking => state == State.Duck || (state == State.Attack && currentAttack.clip == "slide_kick");
        public bool IsGrabbed => state == State.Grabbed;
        public int GrabTaps => grabTaps;
        public bool Invulnerable => Time.time < invulnerableUntil || state == State.Dead || InRampage || OnLedge || state == State.Swing ||
                                    state == State.Rope || state == State.Bar || state == State.WallRun || state == State.Scissors;
        public bool IsGrabbedBy(Enemy e) => state == State.Grabbed && grabber == e;

        string IdleClip => Style == FightStyle.Sword || Style == FightStyle.Pipe ? "sword_idle" : Style == FightStyle.Gun ? "gun_idle" : "idle";

        protected override void Awake()
        {
            base.Awake();
            var all = GetComponentsInChildren<TrailRenderer>(true);
            limbTrails = System.Array.FindAll(all, t => t != swordTrail && t != pipeTrail);
            swordFx = SwordFx.Attach(sword, trailMaterial, trailMaterial, true);
            if (swordTrail != null) swordTrail.enabled = false;           // replaced by the swept ribbon
            bodyMesh = GetComponentInChildren<SkinnedMeshRenderer>(true);
            HeroSpot.Attach(this);                                          // the spotlight pool that marks the hero
            SetTrails(false);
            ApplyStyleVisuals();
        }

        void Start()
        {
            if (Contact && GetComponent<ContactHints>() == null) gameObject.AddComponent<ContactHints>();
            body.Play(IdleClip, 0f);
            V3Init();
            Invoke(nameof(CheckBody), 0.3f);
        }

        void CheckBody() { body.CheckVisible(); }

        // ------------------------------------------------------------------ the building's moves
        // Situational moves built on the level and its objects:
        //   DEATH FROM ABOVE  drop, run or jump off a high floor with an enemy below: a slow-motion
        //                     spinning dive onto him and a shockwave (a downward plunge with a blade)
        //   HERO LANDING      a big fall with nobody to land on: a crouched landing, dust and a thud
        //   OVER THE EDGE     an enemy near a floor's edge: grab him and heave him into the drop
        //   HAND STOMP        an enemy hanging from a ledge below you: stamp on his hands
        //   SPRINGBOARD       a crate between you and an enemy: run up it into a flying kick

        bool wasGroundedForDive = true, diveLanded, edgeThrow;
        Enemy diveTarget, springTarget;
        float diveLandedAt;

        /// <summary>An enemy on a lower level, roughly below and ahead of the hero.</summary>
        Enemy PickBelow(int side, float range)
        {
            Enemy best = null;
            float bestD = range;
            var p = transform.position;
            foreach (var e in Enemy.All)
            {
                if (!e.Targetable) continue;
                var q = e.transform.position;
                if (q.y > p.y - 1.8f) continue;
                float dx = (q.x - p.x) * side;
                if (dx < -0.8f || Mathf.Abs(q.x - p.x) > range) continue;
                float d = Mathf.Abs(q.x - p.x);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        bool TryDeathFromAbove(Enemy preferred)
        {
            if (CinematicLock || InRampage) return false;
            var p = transform.position;
            Enemy t = preferred;
            if (t == null || !t.Targetable || t.transform.position.y > p.y - 1.8f)
            {
                int side = Mathf.Abs(velocity.x) > 0.5f ? (velocity.x > 0f ? 1 : -1) : Facing;
                t = PickBelow(side, 4.5f);
            }
            if (t == null) return false;
            BeginDive(t);
            return true;
        }

        void BeginDive(Enemy t)
        {
            Tell("dive");
            var p = transform.position;
            var q = t.transform.position;
            int side = q.x >= p.x ? 1 : -1;
            SetFacing(side);
            diveTarget = t;
            diveLanded = false;
            state = State.Dive;
            stateTime = 0f;
            const float vy0 = 4.2f;
            float h = Mathf.Max(0.5f, p.y - q.y);
            float T = (vy0 + Mathf.Sqrt(vy0 * vy0 + 2f * gravity * h)) / gravity;
            velocity = new Vector3(Mathf.Clamp((q.x - p.x) / T, -9f, 9f), vy0, 0f);
            Grounded = false;
            // pass through any floors on the way down, but land on his
            float pass = Mathf.Max(0.1f, h - 0.6f);
            dropThroughUntil = Time.time + (vy0 + Mathf.Sqrt(vy0 * vy0 + 2f * gravity * pass)) / gravity;
            invulnerableUntil = Time.time + 1.6f;
            body.Play(body.Has("front_flip") ? "front_flip" : "jump", 0.04f, true, 0.8f);
            SetTrails(true);
            Sound.Play(Sfx.WhooshBig, 1f, p);
            Sound.Play(Sfx.HeroEffort, 1f, p);
            // the moment: slow motion, the camera holding both of them
            swordSlowAt = Time.unscaledTime;
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 0.8f, 0.05f, 0.3f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Focus(t.transform, 1.1f);
        }

        void UpdateDive(float dt)
        {
            if (diveLanded)
            {
                velocity.x = 0f;
                if (Time.time - diveLandedAt > 0.38f) { SetTrails(false); EnterIdle(); }
                return;
            }
            // home in a little on the target as he falls
            if (diveTarget != null && diveTarget.Targetable)
            {
                float want = (diveTarget.transform.position.x - transform.position.x) * 3f;
                velocity.x = Mathf.MoveTowards(velocity.x, Mathf.Clamp(want, -9f, 9f), 14f * dt);
            }
            // the strike pose for the last part of the fall: a plunge with a blade, a stamp without
            if (velocity.y < -2f && stateTime > 0.25f)
            {
                string pose = (Style == FightStyle.Sword || Style == FightStyle.Pipe) && body.Has("sword_leap") ? "sword_leap" : body.Has("stomp") ? "stomp" : "flying_kick";
                if (body.CurrentName != pose) body.Play(pose, 0.06f);
            }
            if (stateTime > 3f) { SetTrails(false); EnterIdle(); }
        }

        void DiveImpact(float fall)
        {
            diveLanded = true;
            diveLandedAt = Time.time;
            velocity = Vector3.zero;
            var at = transform.position;
            bool blade = Style == FightStyle.Sword || Style == FightStyle.Pipe;
            int hits = 0;
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable) continue;
                var q = e.transform.position;
                if (Mathf.Abs(q.x - at.x) > 1.9f || Mathf.Abs(q.y - at.y) > 0.8f) continue;
                bool main = e == diveTarget || Mathf.Abs(q.x - at.x) < 0.8f;
                int dir = q.x >= at.x ? 1 : -1;
                float dmg = main ? (blade ? 55f : 45f) : 22f;
                bool killing = e.Health <= dmg;
                e.TakeHit(new HitInfo { damage = dmg, direction = dir, knockback = main ? 4f : 8f, knockdown = true, heavy = true, lift = main ? 3f : 5f, attacker = this });
                lastKilled = killing;
                lastHitAt = e.Chest;
                OnStrikeLanded(e, killing);
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(dir, 0.6f, 0f), main ? 36 : 16, e.InkColor);
                if (blade && main) StreakFx.Slash(e.Chest, Vector3.down + Vector3.right * dir * 0.2f, 2.6f, 0.08f, 0.3f);
                hits++;
            }
            if (blade && hits > 0 && !InRampage && Style == FightStyle.Sword) SwordUses--;
            // the shockwave: dust rolling out both ways along the floor
            StreakFx.Sparks(at + Vector3.up * 0.1f, Vector3.right, 16, 9f);
            StreakFx.Sparks(at + Vector3.up * 0.1f, Vector3.left, 16, 9f);
            if (InkFx.Instance != null) { InkFx.Instance.Burst(at + Vector3.up * 0.3f, 5f); InkFx.Instance.ImpactFlash(0.05f); }
            Sound.Play(Sfx.Boom, 1f, at);
            Sound.Play(Sfx.CineHit, 0.7f);
            Sound.Play(hits > 0 ? (blade ? Sfx.SwordHit : Sfx.PunchHeavy) : Sfx.Land, 1f, at);
            if (hits > 0) Sound.Play(Sfx.BodyFall, 1f, at);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(hits > 0 ? 8 : 4);
            if (CameraDirector.Instance != null) { CameraDirector.Instance.Shake(hits > 0 ? 0.35f : 0.2f, 0.3f); CameraDirector.Instance.ZoomPunch(6f, 0.15f); }
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(0.6f);
            body.Play(body.Has("duck") ? "duck" : IdleClip, 0.04f);
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.AddStyle(hits > 0 ? 300 : 40, hits > 1 ? "DEATH FROM ABOVE x" + hits + "!" : hits == 1 ? "DEATH FROM ABOVE!" : "HERO LANDING");
        }

        /// <summary>A big fall with nobody under him: a crouched superhero landing.</summary>
        void HeroLanding(float fall)
        {
            var at = transform.position;
            StreakFx.Sparks(at + Vector3.up * 0.1f, Vector3.right, 10, 7f);
            StreakFx.Sparks(at + Vector3.up * 0.1f, Vector3.left, 10, 7f);
            if (InkFx.Instance != null) InkFx.Instance.Burst(at + Vector3.up * 0.2f, 3f);
            Sound.Play(Sfx.Boom, 0.6f, at);
            Sound.Play(Sfx.Land, 1f, at, 0.85f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.18f, 0.25f);
            // anyone right beside him is knocked off his feet
            foreach (var e in Enemy.All.ToArray())
                if (e.Targetable && Mathf.Abs(e.transform.position.x - at.x) < 1.2f && Mathf.Abs(e.transform.position.y - at.y) < 0.6f)
                    e.TakeHit(new HitInfo { damage = 8f, direction = e.transform.position.x >= at.x ? 1 : -1, knockback = 6f, knockdown = true, attacker = this });
            state = State.Dive;            // reuse the dive's short crouch-and-rise
            diveLanded = true;
            diveLandedAt = Time.time;
            body.Play(body.Has("duck") ? "duck" : IdleClip, 0.04f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(40, "HERO LANDING");
        }

        /// <summary>An enemy close to the edge of the floor he stands on, with a real drop beyond.</summary>
        bool NearEdge(Enemy t, int side)
        {
            if (!LevelNav.Active || t == null) return false;
            var q = t.transform.position;
            int pl = LevelNav.PlatformAt(q);
            if (pl < 0) return false;
            var P = LevelNav.Platforms[pl];
            if (!P.oneWay) return false;
            float edge = side > 0 ? P.x1 : P.x0;
            if ((edge - q.x) * side > 1.8f || (edge - q.x) * side < -0.1f) return false;
            var beyond = new Vector3(edge + side * 0.6f, P.y, 0f);
            if (LevelNav.PlatformAt(beyond) >= 0) return false;                  // the floor just carries on
            float below = GroundY(new Vector3(beyond.x, P.y - 0.2f, 0f), 0f);
            return P.y - below >= 2.2f;
        }

        bool TryOverTheEdge(Enemy t, int side)
        {
            if (t == null || !t.Targetable || t.Type == EnemyType.Brute || !Grounded || Style == FightStyle.Gun) return false;
            var p = transform.position; var q = t.transform.position;
            float dx = (q.x - p.x) * side;
            if (dx < 0.2f || dx > 1.8f || Mathf.Abs(q.y - p.y) > 0.4f || !NearEdge(t, side)) return false;
            edgeThrow = true;
            SetFacing(side);
            StartAttack(AttackData.GrabThrow, t, side, false);
            return true;
        }

        bool TryHandStomp(int side)
        {
            if (!Grounded) return false;
            var p = transform.position;
            foreach (var e in Enemy.All)
            {
                if (!e.Alive || !e.Hanging) continue;
                var q = e.transform.position;
                float dx = (q.x - p.x) * side;
                if (dx < -0.3f || dx > 1.8f || p.y - q.y < 0.8f || p.y - q.y > 2.4f) continue;
                SetFacing(side);
                currentAttack = new AttackData { clip = "stomp", ranged = true };
                target = null;
                hitDone = true;
                state = State.Attack;
                stateTime = 0f;
                body.Play(body.Has("stomp") ? "stomp" : "front_kick", 0.04f);
                StartCoroutine(StompHands(e));
                return true;
            }
            return false;
        }

        System.Collections.IEnumerator StompHands(Enemy e)
        {
            yield return new WaitForSeconds(0.14f);
            if (e == null || !e.Hanging) yield break;
            bool killing = e.Health <= 20f;
            e.TakeHit(new HitInfo { damage = 20f, direction = e.transform.position.x >= transform.position.x ? 1 : -1, knockback = 1f, knockdown = true, hitsDowned = true, attacker = this });
            OnStrikeLanded(e, killing);
            Sound.Play(Sfx.Bone, 1f, e.Chest);
            Sound.Play(Sfx.PunchHeavy, 0.8f, e.Chest);
            Sound.Play(Sfx.FallScream, 0.9f, e.Chest);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.15f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(180, "HAND STOMP!");
        }

        bool TrySpringboard(Enemy t, int side)
        {
            if (t == null || !t.Targetable || !Grounded) return false;
            var p = transform.position;
            foreach (var c in Crate.All)
            {
                if (!c.Available) continue;
                var cp = c.transform.position;
                float cdx = (cp.x - p.x) * side;
                if (cdx < 0.3f || cdx > 1.6f || Mathf.Abs(cp.y - 0.35f - p.y) > 0.5f) continue;
                float tdx = (t.transform.position.x - cp.x) * side;
                if (tdx < 0.6f || tdx > 4.5f || Mathf.Abs(t.transform.position.y - p.y) > 0.6f) continue;
                SetFacing(side);
                state = State.Air;
                stateTime = 0f;
                velocity = new Vector3(side * 5.5f, 8.5f, 0f);
                Grounded = false;
                springTarget = t;
                body.Play(body.Has("front_flip") ? "front_flip" : "jump", 0.04f);
                Sound.Play(Sfx.Footstep, 1.6f, cp);
                Sound.Play(Sfx.WhooshBig, 1f, p);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(120, "SPRINGBOARD!");
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ head scissors takedown
        // Leap onto the enemy's shoulders, lock the legs round his head, throw yourself into a
        // back-flip and whip him through a forward somersault over you, slamming him down behind
        // you (into anyone standing there). Scripted like the giant swing; chosen by situation.

        Enemy scissorsVictim;
        int scissorsDir;
        Vector3 scissorsFrom, scissorsNeck, scissorsVictimFrom;
        float scissorsGround, lastScissorsAt = -99f;
        bool scissorsSlowed, scissorsSlammed;
        const float ScissorsLand = 2.1f;       // how far behind the start the victim lands

        /// <summary>Is the takedown possible here? Not on the brute, same level, close, and
        /// with floor behind to slam him onto.</summary>
        bool CanScissors(Enemy t, int side, bool fromAir = false)
        {
            if (t == null || !t.Targetable || t.Type == EnemyType.Brute || t.OnLedge || t.Carried) return false;
            if (Time.time - lastScissorsAt < 4f) return false;
            var p = transform.position;
            var e = t.transform.position;
            float dx = (e.x - p.x) * side;
            if (dx < 0.2f || dx > (fromAir ? 1.9f : 2.6f)) return false;
            if (fromAir ? (p.y - e.y < -0.3f || p.y - e.y > 2.2f) : Mathf.Abs(e.y - p.y) > 0.3f) return false;
            float landX = e.x - side * ScissorsLand;
            if (landX > arenaMaxX - 0.3f || landX < arenaMinX + 0.3f) return false;
            float g = GroundY(new Vector3(landX, e.y + 0.4f, 0f), 0.4f);
            return Mathf.Abs(g - e.y) < 0.45f;
        }

        /// <summary>The situational choice, made on a strike: a counter to a close wind-up, a
        /// crowd behind you to throw him into, the end of a kick string, or a finishing blow.</summary>
        bool TryHeadScissors(Enemy t, int side, bool kick)
        {
            if (t == null || !Grounded || !CanScissors(t, side)) return false;
            float dx = Mathf.Abs(t.transform.position.x - transform.position.x);
            bool counter = t.Telegraphing && dx < 2.3f;
            bool crowdBehind = false;
            foreach (var o in Enemy.All)
            {
                if (o == t || !o.Targetable) continue;
                float bx = (o.transform.position.x - transform.position.x) * -side;
                if (bx > 0.2f && bx < 3.2f && Mathf.Abs(o.transform.position.y - transform.position.y) < 0.4f) crowdBehind = true;
            }
            bool kickFinisher = kick && kickStep % AttackData.KickCombo.Length == AttackData.KickCombo.Length - 1;
            bool finishing = t.Health <= 34f;
            float chance = counter ? 0.6f : crowdBehind ? 0.45f : kickFinisher ? 0.5f : finishing ? 0.3f : 0f;
            if (chance <= 0f || Random.value > chance) return false;
            if (kickFinisher) kickStep++;
            BeginScissors(t, side);
            return true;
        }

        void BeginScissors(Enemy t, int side)
        {
            lastScissorsAt = Time.time;
            scissorsVictim = t;
            scissorsDir = side;
            scissorsFrom = transform.position;
            scissorsVictimFrom = t.transform.position;
            scissorsNeck = t.Chest + Vector3.up * 0.2f * t.transform.localScale.y;
            scissorsGround = t.transform.position.y;
            scissorsSlowed = scissorsSlammed = false;
            if (t.Telegraphing) t.Interrupt();
            t.BeginCarried();
            SetFacing(side);
            state = State.Scissors;
            stateTime = 0f;
            velocity = Vector3.zero;
            Grounded = false;
            invulnerableUntil = Time.time + 1.2f;
            SetTrails(true);
            body.Play(body.Has("pike") ? "pike" : "jump", 0.04f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position);
            Sound.Play(Sfx.HeroEffort, 1f, transform.position);
        }

        static float Ease01(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>Place a fighter by its body centre, rolled about the camera axis.</summary>
        static void PlaceRolled(Transform tr, Vector3 centre, float rollWorld, float yaw, float centreHeight)
        {
            var rot = Quaternion.AngleAxis(rollWorld, Vector3.forward) * Quaternion.Euler(0f, yaw, 0f);
            tr.SetPositionAndRotation(centre - rot * Vector3.up * centreHeight, rot);
        }

        void UpdateScissors()
        {
            var e = scissorsVictim;
            int dir = scissorsDir;
            if (e == null || !e.Carried) { EndScissors(); return; }
            float t = stateTime;
            float heroYaw = dir > 0 ? 0f : 180f, victimYaw = dir > 0 ? 180f : 0f;
            float hc = 0.5f * transform.localScale.y, vc = 0.5f * e.transform.localScale.y;

            // A: leap up onto his shoulders, legs locked round his head
            var wrapC = new Vector3(scissorsNeck.x - dir * 0.08f, scissorsNeck.y + 0.1f, 0f);
            var startC = scissorsFrom + Vector3.up * hc;
            var victimC = scissorsVictimFrom + Vector3.up * vc;
            Vector3 heroC; float heroRoll; Vector3 vicC; float vicRoll;
            if (t < 0.16f)
            {
                float k = Ease01(0f, 0.16f, t);
                heroC = Vector3.Lerp(startC, wrapC, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                heroRoll = 0f; vicC = victimC; vicRoll = 0f;
            }
            else if (t < 0.30f)
            {
                // B: the lock: a beat on his shoulders, leaning back
                float k = Ease01(0.16f, 0.30f, t);
                heroC = wrapC; heroRoll = 22f * k;
                vicC = victimC + Vector3.right * dir * 0.05f * Mathf.Sin(t * 60f); vicRoll = -6f * k;
            }
            else if (t < 0.64f)
            {
                // C: the whip. He flips back and down; the victim is dragged over in a somersault
                float k = Ease01(0.30f, 0.64f, t);
                if (body.CurrentName != "jump") { body.Play("jump", 0.05f); Sound.Play(Sfx.WhooshBig, 1f, transform.position, 0.85f); Sound.Play(Sfx.EnemyHurt, 1f, e.Chest); }
                heroRoll = Mathf.Lerp(22f, 205f, k);
                var handstand = new Vector3(scissorsVictimFrom.x - dir * 0.95f, scissorsGround + 0.62f, 0f);
                heroC = Vector3.Lerp(wrapC, handstand, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f;
                var land = new Vector3(scissorsVictimFrom.x - dir * ScissorsLand, scissorsGround + 0.45f, 0f);
                var apex = new Vector3(scissorsVictimFrom.x - dir * 0.9f, scissorsGround + 2.1f, 0f);
                vicC = Bezier(victimC, apex, land, k * 0.82f);
                vicRoll = Mathf.Lerp(0f, 230f, k);
                // the top of the somersault, in slow motion (now and then)
                if (!scissorsSlowed && k > 0.4f)
                {
                    scissorsSlowed = true;
                    if (SwordSlowReady)
                    {
                        swordSlowAt = Time.unscaledTime;
                        if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 0.55f, 0.03f, 0.25f);
                        if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(5f, 0.35f);
                    }
                }
            }
            else
            {
                // D: the slam, and he rolls out of the flip onto his feet
                float k = Ease01(0.64f, 0.88f, t);
                var handstand = new Vector3(scissorsVictimFrom.x - dir * 0.95f, scissorsGround + 0.62f, 0f);
                var stand = new Vector3(scissorsVictimFrom.x - dir * 1.25f, scissorsGround + hc, 0f);
                heroC = Vector3.Lerp(handstand, stand, k);
                heroRoll = Mathf.Lerp(205f, 360f, k);
                var land = new Vector3(scissorsVictimFrom.x - dir * ScissorsLand, scissorsGround + 0.45f, 0f);
                var apex = new Vector3(scissorsVictimFrom.x - dir * 0.9f, scissorsGround + 2.1f, 0f);
                vicC = Bezier(victimC, apex, land, Mathf.Lerp(0.82f, 1f, Mathf.Clamp01((t - 0.64f) / 0.08f)));
                vicRoll = Mathf.Lerp(230f, 270f, Mathf.Clamp01((t - 0.64f) / 0.08f));
                if (!scissorsSlammed && t >= 0.72f) Slam(e, dir);
                if (t >= 0.88f)
                {
                    transform.position = new Vector3(stand.x, scissorsGround, 0f);
                    SetFacing(dir);
                    Grounded = true;
                    body.Play(body.Has("duck") ? "duck" : IdleClip, 0.05f);
                    EndScissors();
                    return;
                }
            }
            PlaceRolled(transform, heroC, dir * heroRoll, heroYaw, hc);
            if (!scissorsSlammed) PlaceRolled(e.transform, vicC, dir * vicRoll, victimYaw, vc);
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float u)
        {
            float v = 1f - u;
            return v * v * a + 2f * v * u * b + u * u * c;
        }

        void Slam(Enemy e, int dir)
        {
            scissorsSlammed = true;
            var at = new Vector3(scissorsVictimFrom.x - dir * ScissorsLand, scissorsGround, 0f);
            e.transform.position = at;
            e.EndCarried(new Vector3(-dir * 2f, -2f, 0f), this);
            bool killing = e.Health <= 34f;
            e.TakeHit(new HitInfo { damage = 34f, direction = -dir, knockback = 2.5f, knockdown = true, heavy = true, hitsDowned = true, lift = 2.2f, attacker = this });
            lastKilled = killing;
            lastHitAt = at + Vector3.up * 0.3f;
            OnStrikeLanded(e, killing);
            // anyone standing where he comes down goes over too
            foreach (var o in Enemy.All.ToArray())
            {
                if (o == e || !o.Targetable) continue;
                if (Mathf.Abs(o.transform.position.x - at.x) < 1.2f && Mathf.Abs(o.transform.position.y - at.y) < 0.6f)
                {
                    o.TakeHit(new HitInfo { damage = 18f, direction = -dir, knockback = 7f, knockdown = true, heavy = true, attacker = this });
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(o, "COLLATERAL!", 120);
                }
            }
            Sound.Play(Sfx.BodyFall, 1f, at);
            Sound.Play(Sfx.Bone, 0.8f, at);
            Sound.Play(Sfx.CineHit, 0.6f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(at + Vector3.up * 0.2f, Vector3.up, 30, e.InkColor);
                InkFx.Instance.Burst(at + Vector3.up * 0.2f, 3.5f);
                InkFx.Instance.ImpactFlash(0.04f);
            }
            if (TimeController.Instance != null) TimeController.Instance.HitStop(6);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.25f, 0.25f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(220, "HEAD SCISSORS!");
        }

        void EndScissors()
        {
            var e = scissorsVictim;
            scissorsVictim = null;
            if (e != null && e.Carried && !scissorsSlammed) e.EndCarried(Vector3.zero, this);   // interrupted: drop him
            SetFacing(scissorsDir != 0 ? scissorsDir : Facing);
            SetTrails(false);
            lastAttackEnd = Time.time;
            if (state == State.Scissors) EnterIdle();
        }

        // ------------------------------------------------------------------ the sword

        SwordFx swordFx;
        SkinnedMeshRenderer bodyMesh;
        static float swordSlowAt = -9f;
        float ghostAt;
        bool SwordSlowReady => Time.unscaledTime - swordSlowAt > 2.5f;

        static readonly Color GhostColor = new Color(0.92f, 0.95f, 1f, 0.42f);

        void Ghosts(float every, float alpha)
        {
            if (bodyMesh == null || Time.unscaledTime - ghostAt < every) return;
            ghostAt = Time.unscaledTime;
            var c = GhostColor; c.a = alpha;
            Afterimage.Spawn(bodyMesh, c);
        }

        /// <summary>Enemies standing in a line on one side, close enough for the draw-cut.</summary>
        int InLine(int side)
        {
            int n = 0;
            foreach (var e in Enemy.All)
            {
                if (!e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * side;
                if (dx > 0.3f && dx < 4.2f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.6f) n++;
            }
            return n;
        }

        /// <summary>A sword hit's look: the white slice line across the body along the blade's path,
        /// sparks off the steel, ink sprayed the way the blade travelled.</summary>
        void SwordHitFx(Enemy e, bool killing, bool clash)
        {
            Vector3 dir = swordFx != null ? swordFx.TipVelocity : new Vector3(attackDir, 0.3f, 0f);
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.5f) dir = new Vector3(attackDir, 0.3f, 0f);
            dir.Normalize();
            StreakFx.Slash(e.Chest, dir, killing ? 2.8f : 1.9f, killing ? 0.08f : 0.05f, killing ? 0.28f : 0.18f);
            StreakFx.Sparks(e.Chest, dir, clash ? 18 : killing ? 10 : 5, clash ? 9f : 6f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(e.Chest, dir * 0.8f + Vector3.up * 0.2f, killing ? 34 : 14, e.InkColor);
                if (killing) InkFx.Instance.ImpactFlash(0.035f);
            }
            if (clash)
            {
                Sound.Play(Sfx.SwordClash, 1f, e.Chest);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(60, "CLASH!");
            }
        }

        // ---- the draw-cut (iai): a blur through everyone in line; stillness; then they fall
        float iaiFrom, iaiTo;
        readonly List<Enemy> iaiVictims = new List<Enemy>();

        void BeginIai()
        {
            float x = transform.position.x;
            iaiFrom = x;
            iaiVictims.Clear();
            float far = target != null ? target.transform.position.x : x + attackDir * 2f;
            foreach (var e in Enemy.All)
            {
                if (!e.Targetable) continue;
                float dx = (e.transform.position.x - x) * attackDir;
                if (dx > 0.2f && dx < 4.4f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.7f)
                {
                    iaiVictims.Add(e);
                    if ((e.transform.position.x - far) * attackDir > 0f) far = e.transform.position.x;
                }
            }
            iaiTo = Mathf.Clamp(far + attackDir * 1.25f, arenaMinX + 0.3f, arenaMaxX - 0.3f);
            foreach (var e in iaiVictims) e.Interrupt();
            velocity = Vector3.zero;
            Sound.Play(Sfx.WhooshBig, 1f, transform.position, 0.9f);
            Sound.Play(Sfx.SwordSwing, 1f, transform.position, 0.85f);
        }

        void UpdateIai()
        {
            // frames 1..5 at 30 fps: the dash itself, eased, leaving ghosts all the way
            const float a = 1f / 30f, b = 5f / 30f;
            float u = Mathf.InverseLerp(a, b, body.Time);
            if (u > 0f && u < 1f + 1e-3f)
            {
                var p = transform.position;
                p.x = Mathf.Lerp(iaiFrom, iaiTo, 1f - (1f - u) * (1f - u));
                transform.position = p;
                Ghosts(0.016f, 0.5f);
            }
            velocity.x = 0f;
        }

        void ResolveIai()
        {
            hitDone = true;
            if (!InRampage) SwordUses--;
            swordSlowAt = Time.unscaledTime;
            // the world stops; the hero holds the follow-through
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.1f, 1.0f, 0.02f, 0.35f);
            var cam = CameraDirector.Instance;
            if (cam != null) { cam.ZoomPunch(7f, 0.6f); cam.Focus(transform, 0.95f); }
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.03f);
            StreakFx.Slash(Chest + Vector3.up * 0.05f, new Vector3(attackDir, 0f, 0f), Mathf.Abs(iaiTo - iaiFrom) + 1.2f, 0.035f, 0.5f);
            StartCoroutine(IaiCut(new List<Enemy>(iaiVictims)));
        }

        System.Collections.IEnumerator IaiCut(List<Enemy> victims)
        {
            yield return new WaitForSecondsRealtime(0.38f);
            int n = 0;
            var hit = new HitInfo { damage = currentAttack.iai ? currentAttack.damage : AttackData.SwordIai.damage, direction = attackDir, knockback = 6f, knockdown = true, heavy = true, attacker = this };
            foreach (var e in victims)
            {
                if (e == null || !e.Alive) continue;
                bool killing = InRampage || e.Health <= hit.damage;
                var dir = new Vector3(attackDir * 0.9f, Random.Range(-0.45f, 0.45f), 0f).normalized;
                StreakFx.Slash(e.Chest, dir, 3.2f, 0.09f, 0.32f);
                StreakFx.Sparks(e.Chest, dir, 12, 8f);
                if (InkFx.Instance != null)
                {
                    InkFx.Instance.Splash(e.Chest, dir + Vector3.up * 0.3f, 44, e.InkColor);
                    InkFx.Instance.Burst(e.Chest, 4f);
                }
                if (InRampage) e.Kill(hit); else e.TakeHit(hit);
                lastKilled = killing;
                lastHitAt = e.Chest;
                OnStrikeLanded(e, killing);
                Sound.Play(Sfx.SwordHit, 1f, e.Chest);
                n++;
            }
            if (n > 0)
            {
                Sound.Play(Sfx.Finisher, 1f, transform.position);
                Sound.Play(Sfx.CineHit, 0.7f);
                if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.06f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.28f, 0.3f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150 * n, n >= 3 ? "IAI x" + n + "!" : n == 2 ? "DOUBLE IAI!" : "IAI!");
            }
            Sound.PlayLater(Sfx.Pickup, 0.45f, 0.8f, transform.position);   // the blade clicks home
        }

        // ------------------------------------------------------------------ sound

        bool IsKickClip(string c) => c != null && (c.Contains("kick") || c.Contains("geri") || c.Contains("knee") || c == "stomp" || c == "slide_kick" || c == "sweep");

        void SwingSound()
        {
            var c = currentAttack.clip;
            if (Style == FightStyle.Sword && c != null && c.StartsWith("sword")) Sound.Play(Sfx.SwordSwing, 1f, transform.position);
            else if (currentAttack.heavy || IsKickClip(c)) Sound.Play(Sfx.WhooshBig, 0.8f, transform.position);
            else Sound.Play(Sfx.Whoosh, 1f, transform.position);
            if (currentAttack.heavy && Random.value < 0.5f) Sound.Play(Sfx.HeroEffort, 1f, transform.position);
        }

        /// <summary>What a landed blow sounds like: by weapon, by limb, and heavier for kills.</summary>
        void HitSound()
        {
            var c = currentAttack.clip;
            var at = lastHitAt;
            bool blade = Style == FightStyle.Sword && c != null && c.StartsWith("sword");
            bool pipe = Style == FightStyle.Pipe && c != null && (c.StartsWith("sword") || c.StartsWith("pipe"));
            if (blade) Sound.Play(Sfx.SwordHit, 1f, at);
            else if (pipe) Sound.Play(Sfx.PipeHit, 1f, at);
            else if (IsKickClip(c)) Sound.Play(Sfx.Kick, currentAttack.heavy ? 1f : 0.85f, at);
            else Sound.Play(currentAttack.heavy || InRampage || isCounter ? Sfx.PunchHeavy : Sfx.PunchLight, 1f, at);
            bool lastOne = lastKilled && Enemy.All.TrueForAll(x => x == null || !x.Alive);
            if (lastKilled && (currentAttack.heavy || isCounter || lastOne)) Sound.Play(Sfx.Finisher, lastOne ? 1f : 0.7f, at);
            else if (lastKilled) Sound.Play(Sfx.Bone, 0.6f, at);
            if (isCounter) Sound.Play(Sfx.CineHit, 0.5f, at);
        }

        float stepTimer;
        bool wasGrounded = true;

        /// <summary>Footsteps while running (synced to the run speed) and the landing thump.</summary>
        void FootSounds(float dt)
        {
            if (running && Grounded && state == State.Idle)
            {
                stepTimer -= dt * Mathf.Max(0.6f, Mathf.Abs(velocity.x) / runSpeed);
                if (stepTimer <= 0f) { stepTimer = 0.27f; Sound.Play(Sfx.Footstep, 1f, transform.position); }
            }
            else stepTimer = 0.05f;
            if (Grounded && !wasGrounded && state != State.Dead) Sound.Play(Sfx.Land, 1f, transform.position);
            wasGrounded = Grounded;
        }

        // ------------------------------------------------------------------ opening cinematic

        /// <summary>While set, the opening cinematic drives the hero: no input, no state machine.</summary>
        public bool CinematicLock { get; private set; }
        bool cineFrozen;
        Renderer[] cineHidden;

        /// <summary>Hold the hero, hidden, at a point (above the skylight) until CineDrop.</summary>
        public void CineHold(Vector3 at)
        {
            CinematicLock = true;
            cineFrozen = true;
            transform.position = at;
            velocity = Vector3.zero;
            Grounded = false;
            var all = GetComponentsInChildren<Renderer>(true);
            cineHidden = System.Array.FindAll(all, r => r.enabled && !(r is TrailRenderer));
            foreach (var r in cineHidden) r.enabled = false;
        }

        /// <summary>Let go: the hero falls through the skylight, tucked.</summary>
        public void CineDrop()
        {
            Unhide();
            cineFrozen = false;
            dropThroughUntil = Time.time + 0.45f;   // fall through the roof slab
            velocity = new Vector3(0f, -1.5f, 0f);
            body.Play(body.Has("front_flip") ? "front_flip" : "jump", 0.03f, true, 0.9f);
        }

        /// <summary>Lock a hero who is already standing (the short replay intro).</summary>
        public void CineLock()
        {
            if (state == State.Clinch) EndClinch();
            CinematicLock = true;
            cineFrozen = false;
            // a run in progress stops here: no sliding, no run cycle on the spot
            velocity.x = 0f;
            if (running || body.CurrentName == "run") { running = false; if (state == State.Idle) body.Play(IdleClip, 0.2f); }
        }

        /// <summary>A cutscene moves him by hand: no input, no physics (the boss finale).</summary>
        public void CinePuppet() { if (state == State.Clinch) EndClinch(); CinematicLock = true; cineFrozen = true; velocity = Vector3.zero; }
        public void CinePose(Vector3 at) { transform.position = at; velocity = Vector3.zero; }
        public void CineTrails(bool on) => SetTrails(on);

        /// <summary>Standing, free and doing nothing special: a cutscene may take him now.</summary>
        public bool Free => Grounded && !OnLedge && !CinematicLock && !InRampage && !RampageIntro &&
                            (state == State.Idle || state == State.Dash || state == State.Duck);

        /// <summary>Cut a rampage short (a cutscene takes over).</summary>
        public void EndRampage()
        {
            if (!InRampage && !RampageIntro) return;
            rampageUntil = 0f;
            rampageIntroUntil = 0f;
            SetTrails(false);
        }

        /// <summary>Hand control back. With `home`, a hero still in the air is put down there.</summary>
        public void CineRelease(Vector3? home = null)
        {
            Unhide();
            if (home.HasValue && (!Grounded || cineFrozen))
            {
                var p = home.Value;
                p.y = GroundY(p);
                transform.position = p;
                velocity = Vector3.zero;
                Grounded = true;
            }
            cineFrozen = false;
            CinematicLock = false;
            if (GestureInput.Instance != null) GestureInput.Instance.Flush();
            EnterIdle();
        }

        void Unhide()
        {
            if (cineHidden == null) return;
            foreach (var r in cineHidden) if (r != null) r.enabled = true;
            cineHidden = null;
        }

        /// <summary>The floor the hero is fighting on (0 ground, 1 upper). Changes only when he
        /// lands, so a jump or a rope swing does not send every enemy running for the stairs.</summary>
        public int Floor { get; private set; }
        /// <summary>The tower platform the hero last stood on (-1 in the classic hall).</summary>
        public int Platform { get; private set; } = -1;

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            V3Always();
            if (Grounded && !OnLedge && !CinematicLock)
            {
                Floor = Level;
                if (LevelNav.Active) { int pl = LevelNav.PlatformAt(transform.position); if (pl >= 0) Platform = pl; }
            }
            ComboTimer -= dt;
            if (ComboTimer <= 0f) ComboCount = 0;
            UpdateLock();
            if (CinematicLock)
            {
                if (!cineFrozen) Integrate(dt);
                UpdateTint();
                return;
            }
            if (RampageIntro) { UpdateTint(); return; }     // the activation cinematic owns him
            // rampage ready: white sparks rise off him
            var wdr = WaveDirector.Instance;
            if (wdr != null && wdr.FlowReady && !InRampage && Time.unscaledTime > sparkAt && InTrouble)
            {
                sparkAt = Time.unscaledTime + 0.16f;
                StreakFx.Sparks(transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 1.0f), 0f), Vector3.up, 2, 3.2f);
            }
            if (UpdateLedge(dt)) { UpdateTint(); return; }

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Dash: UpdateDash(dt); break;
                case State.Attack: UpdateAttack(); break;
                case State.Dodge: UpdateDodge(); break;
                case State.Hurt: if (stateTime > 0.35f) EnterIdle(); break;
                case State.Swing: UpdateSwing(dt); break;
                case State.Grabbed: UpdateGrabbed(dt); break;
                case State.Duck: UpdateDuck(); break;
                case State.ToLedge: UpdateToLedge(dt); break;
                case State.Air: UpdateAir(dt); break;
                case State.Rope: UpdateRope(dt); break;
                case State.Bar: UpdateBar(dt); break;
                case State.WallRun: UpdateWallRun(dt); break;
                case State.Scissors: UpdateScissors(); break;
                case State.Dive: UpdateDive(dt); break;
                case State.Clinch: UpdateClinch(dt); break;
                case State.Dead: break;
            }
            if (state != State.Dead) TryPickup();

            if (state != State.Dash && state != State.Dodge && state != State.ToLedge && state != State.Air && !running &&
                !(state == State.Attack && currentAttack.clip == "slide_kick") && Grounded)
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, 30f * dt);
            bool scripted = state == State.Swing || state == State.Grabbed || state == State.Rope || state == State.Bar || state == State.Scissors || state == State.Clinch ||
                            (state == State.WallRun && stateTime < 0.25f);
            if (!scripted) Integrate(dt);
            // ran off an edge with someone below: dive on them
            if (wasGroundedForDive && !Grounded && state == State.Idle && velocity.y <= 0.5f) TryDeathFromAbove(null);
            wasGroundedForDive = Grounded;
            FootSounds(dt);
            UpdateTint();
        }

        // ------------------------------------------------------------------ weapons

        /// <summary>Put a weapon in his hands (the training reel's throw lesson).</summary>
        public void GiveWeapon(FightStyle s, int uses) { if (Style == FightStyle.Fists) Equip(s, uses); }

        void Equip(FightStyle s, int uses)
        {
            Style = s;
            comboStep = 0;
            closeStep = 0;
            switch (s)
            {
                case FightStyle.Sword: SwordUses = uses; break;
                case FightStyle.Gun: Ammo = uses; break;
                case FightStyle.Pipe: PipeUses = uses; break;
                case FightStyle.Knife: Knives = Mathf.Min(6, uses); break;
            }
            ApplyStyleVisuals();
            if (state == State.Idle) body.Play(IdleClip, 0.15f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, s == FightStyle.Fists ? "FISTS" : s.ToString().ToUpper() + "!");
        }

        public void SetStyle(FightStyle s) => Equip(s, s == FightStyle.Sword ? 10 : s == FightStyle.Gun ? 8 : s == FightStyle.Pipe ? 14 : s == FightStyle.Knife ? 4 : 0); // debug keys

        void ApplyStyleVisuals()
        {
            if (sword != null) sword.SetActive(Style == FightStyle.Sword);
            if (pistol != null) pistol.SetActive(Style == FightStyle.Gun);
            if (knife != null) knife.SetActive(Style == FightStyle.Knife);
            if (pipe != null) pipe.SetActive(Style == FightStyle.Pipe);
        }

        void TryPickup()
        {
            foreach (var p in WeaponPickup.All)
            {
                if (Mathf.Abs(p.transform.position.x - transform.position.x) > 0.9f ||
                    Mathf.Abs(p.transform.position.y - 0.35f - transform.position.y) > 0.8f) continue;
                if (Style == FightStyle.Knife && p.kind == FightStyle.Knife) Knives = Mathf.Min(6, Knives + p.uses);
                else if (Style == FightStyle.Fists) Equip(p.kind, p.uses);
                else continue;
                if (InkFx.Instance != null) InkFx.Instance.Burst(p.transform.position, 1.6f);
                Tell("pickup");
                Sound.Play(p.kind == FightStyle.Sword ? Sfx.SwordClash : Sfx.Pickup, p.kind == FightStyle.Sword ? 0.5f : 1f, p.transform.position);
                Destroy(p.gameObject);
                return;
            }
        }

        bool WeaponSpent => (Style == FightStyle.Sword && SwordUses <= 0) || (Style == FightStyle.Gun && Ammo <= 0) ||
                            (Style == FightStyle.Pipe && PipeUses <= 0);

        void ThrowWeapon()
        {
            if (Style != FightStyle.Fists) Tell("throw");
            if (Style == FightStyle.Fists) { Duck(); return; }
            if (Style == FightStyle.Knife) { KnifeThrow(PickTarget(Facing, 16f) ?? PickTarget(-Facing, 16f)); return; }
            var t = PickTarget(Facing, 16f) ?? PickTarget(-Facing, 16f);
            if (t != null) FaceTowards(t.transform.position.x);
            BeginThrow(t);
            throwPending = true;
        }

        void BeginThrow(Enemy t)
        {
            currentAttack = new AttackData { clip = "throw_weapon", ranged = true };
            target = t;
            attackDir = Facing;
            hitDone = false;
            state = State.Attack;
            stateTime = 0f;
            body.Play("throw_weapon", 0.05f, true, 1.4f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position);
            Sound.Play(Sfx.HeroEffort, 1f, transform.position);
        }

        void ReleaseThrow()
        {
            hitDone = true;
            var from = HandPoint();
            if (knifeThrowPending || catchPending)
            {
                ThrownWeapon.Throw(this, FightStyle.Knife, from, target, Facing, weaponMaterial, trailMaterial);
                knifeThrowPending = false;
                catchPending = false;
                return;
            }
            throwPending = false;
            ThrownWeapon.Throw(this, Style, from, target, Facing, weaponMaterial, trailMaterial);
            Style = FightStyle.Fists;
            ApplyStyleVisuals();
        }

        Vector3 HandPoint()
        {
            var w = Style == FightStyle.Sword ? sword : Style == FightStyle.Pipe ? pipe : Style == FightStyle.Knife ? knife : pistol;
            return (w != null ? w.transform.position : Chest) + Vector3.up * 0.2f;
        }

        void KnifeThrow(Enemy t)
        {
            if (Knives <= 0) { Equip(FightStyle.Fists, 0); return; }
            if (t != null) FaceTowards(t.transform.position.x);
            Knives--;
            knifeThrowPending = true;
            BeginThrow(t);
            if (Knives <= 0) { Style = FightStyle.Fists; }     // the last one leaves the hand
        }

        // ------------------------------------------------------------------ input

        void EnterIdle()
        {
            edgeThrow = false;
            if (clinchFoe != null) { var f = clinchFoe; clinchFoe = null; clinchMove = null; if (f.Held) f.ReleaseHeld(); }
            state = State.Idle;
            stateTime = 0f;
            body.Play(IdleClip, 0.12f);
            SetTrails(false);
            ApplyStyleVisuals();
        }

        float Axis => GestureInput.Instance != null ? GestureInput.Instance.MoveAxis : 0f;
        int AxisDir => Mathf.Abs(Axis) > 0.3f ? (Axis > 0 ? 1 : -1) : 0;

        void UpdateIdle()
        {
            HandleCommand(GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None);
            if (state != State.Idle) { running = false; return; }
            float a = Axis;
            if (Mathf.Abs(a) > 0.01f && Grounded)
            {
                // run: the stick sets the speed; the run cycle speeds up with it
                SetFacing(a > 0 ? 1 : -1);
                if (Contact && WalkIntoClinch(a > 0 ? 1 : -1)) return;           // walk into him: take hold
                velocity.x = a * (Contact ? Mathf.Min(runSpeed, ContactRun) : runSpeed);
                if (!running || body.CurrentName != "run") body.Play("run", 0.12f, false, Mathf.Lerp(0.8f, 1.5f, Mathf.Abs(a)));
                else body.SetSpeed(Mathf.Lerp(0.8f, 1.5f, Mathf.Abs(a)));
                running = true;
            }
            else if (running)
            {
                running = false;
                velocity.x = 0f;
                body.Play(IdleClip, 0.15f);
            }
        }

        bool CanCancel()
        {
            // after the strike lands, the next tap cancels the follow-through: fluid chains
            var e = body.Current;
            float window = InRampage ? 0.02f : 0.04f;
            return hitDone && pendingExecution == null && !throwPending && !knifeThrowPending && !catchPending && e != null && body.Time >= e.hitTime + window;
        }

        void HandleCommand(Command cmd)
        {
            if (state == State.Grabbed)
            {
                if (cmd == Command.StrikeLeft || cmd == Command.StrikeRight || cmd == Command.JumpKick || cmd == Command.DodgeLeft || cmd == Command.DodgeRight)
                    StruggleTap();
                return;
            }
            running = false;
            if (TryShovePushKick(cmd)) return;
            switch (cmd)
            {
                case Command.StrikeLeft: Strike(-1); break;
                case Command.StrikeRight: Strike(1); break;
                case Command.StrikeForward: Strike(AxisDir != 0 ? AxisDir : Facing); break;
                case Command.KickLeft: Strike(-1, true); break;
                case Command.KickRight: Strike(1, true); break;
                case Command.KickForward: Strike(AxisDir != 0 ? AxisDir : Facing, true); break;
                case Command.DodgeForward: Dodge(AxisDir != 0 ? AxisDir : Facing); break;
                case Command.JumpKick: Jump(); break;
                case Command.DodgeLeft: Dodge(-1); break;
                case Command.DodgeRight: Dodge(1); break;
                // swipe down: throw the weapon at someone ahead on this level, otherwise drop through the floor
                case Command.ThrowWeapon: if (WantsThrow() || !TryDropThrough()) ThrowWeapon(); break;
                case Command.Rampage: StartRampage(); break;
                case Command.LockNext: LockNext(); break;
                case Command.CycleStyle: SetStyle((FightStyle)(((int)Style + 1) % 5)); break;
                case Command.StyleFists: SetStyle(FightStyle.Fists); break;
                case Command.StyleSword: SetStyle(FightStyle.Sword); break;
                case Command.StyleGun: SetStyle(FightStyle.Gun); break;
            }
        }

        // ------------------------------------------------------------------ target lock
        /// <summary>The enemy the player picked (tap or click him; T on the keyboard). Every strike
        /// on his side goes to him; the others hang back while the duel lasts.</summary>
        public Enemy Locked { get; private set; }

        public void LockOn(Enemy e)
        {
            if (e == null || !e.Alive || e.Fleeing) return;
            bool changed = Locked != e;
            Locked = e;
            if (changed)
            {
                Sound.Play(Sfx.CineHit, 0.3f, null, 1.4f);
                Tell("lock");
            }
        }

        public void ClearLock() { Locked = null; }

        /// <summary>Lock the next one along, the most dangerous first (the keyboard's T).</summary>
        public void LockNext()
        {
            var list = new System.Collections.Generic.List<Enemy>();
            foreach (var e in Enemy.All) if (e.Alive && !e.Fleeing && !e.Passive && Mathf.Abs(e.transform.position.x - transform.position.x) < 14f) list.Add(e);
            foreach (var e in Enemy.All) if (e.Passive && e.Alive) list.Add(e);
            if (list.Count == 0) { Locked = null; return; }
            var p = transform.position;
            list.Sort((a, b) =>
            {
                int t = b.Threat.CompareTo(a.Threat);
                return t != 0 ? t : Vector3.Distance(a.transform.position, p).CompareTo(Vector3.Distance(b.transform.position, p));
            });
            int i = Locked != null ? list.IndexOf(Locked) : -1;
            LockOn(list[(i + 1) % list.Count]);
        }

        void UpdateLock()
        {
            if (Locked == null) return;
            if (!Locked.Alive || Locked.Fleeing || Mathf.Abs(Locked.transform.position.x - transform.position.x) > 16f) Locked = null;
        }

        Enemy PickTarget(int side, float range, bool anyLevel = true)
        {
            // the one he picked, if he is on that side and within reach
            if (Locked != null && Locked.Targetable)
            {
                float ldx = Locked.transform.position.x - transform.position.x;
                float ldy = Mathf.Abs(Locked.transform.position.y - transform.position.y);
                if ((Mathf.Sign(ldx) == side || Mathf.Abs(ldx) < 0.3f) && Mathf.Abs(ldx) <= Mathf.Max(range, targetRange) && (anyLevel || ldy < 1.4f) && ldy < 2.2f)
                    return Locked;
            }
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (!e.Targetable) continue;
                float dx = e.transform.position.x - transform.position.x;
                if (Mathf.Sign(dx) != side && Mathf.Abs(dx) > 0.2f) continue;
                float dy = Mathf.Abs(e.transform.position.y - transform.position.y);
                if (!anyLevel && dy > 1.4f) continue;
                float d = Mathf.Abs(dx) + dy * 1.2f;          // prefer my own floor
                if (d > range) continue;
                float score = d - (e.Telegraphing ? 2.5f : 0f) - (e.HasToken ? 0.8f : 0f);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        /// <summary>An enemy lying on the floor close by on that side (for stomps and swings).</summary>
        Enemy PickDowned(int side)
        {
            foreach (var e in Enemy.All)
            {
                if (!e.Alive || !e.IsDown || e.Carried || !e.Grounded) continue;
                float dx = (e.transform.position.x - transform.position.x) * side;
                if (dx > -0.2f && dx < 1.7f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.5f) return e;
            }
            return null;
        }

        EnemyKnife IncomingKnife(int side)
        {
            foreach (var k in EnemyKnife.All)
            {
                float dx = k.transform.position.x - transform.position.x;
                if (Mathf.Sign(dx) == side && Mathf.Abs(dx) < 2.4f && Mathf.Sign(k.Velocity.x) == -side &&
                    Mathf.Abs(k.transform.position.y - Chest.y) < 1.0f) return k;
            }
            return null;
        }

        // ------------------------------------------------------------------ rampage

        // ------------------------------------------------------------------ rampage
        // Activation is a 1.1 s cinematic: time all but stops, the camera pushes in low and close,
        // ink explodes round him with white speed lines, RAMPAGE slams onto the screen (the HUD),
        // he crouches and rises, a shockwave throws everyone back, and the rampage begins.

        public const float RampageIntroSeconds = 1.1f;
        float rampageIntroUntil, sparkAt;
        public bool RampageIntro => Time.unscaledTime < rampageIntroUntil;
        public float RampageIntroT => 1f - Mathf.Clamp01((rampageIntroUntil - Time.unscaledTime) / RampageIntroSeconds);

        /// <summary>For the on-screen RAMPAGE button.</summary>
        public void TriggerRampage() => StartRampage();

        /// <summary>A moment that calls for the rampage: low on health, three or more enemies
        /// pressing in, or the brute close by. The button only appears then.</summary>
        public bool InTrouble
        {
            get
            {
                if (IsDead) return false;
                if (Health <= maxHealth * 0.35f) return true;
                int near = 0;
                var p = transform.position;
                foreach (var e in Enemy.All)
                {
                    if (!e.Alive || e.Passive) continue;
                    var q = e.transform.position;
                    if (Mathf.Abs(q.y - p.y) > 1.2f) continue;
                    float d = Mathf.Abs(q.x - p.x);
                    if (e.Type == EnemyType.Brute && d < 7f) return true;
                    if (d < 3.2f) near++;
                }
                return near >= 3;
            }
        }
        bool surroundedAtStart;

        void StartRampage()
        {
            if (StoryIntro.Instance != null) return;                 // no rampage in the story: it's grounded there
            var wd = WaveDirector.Instance;
            if (wd == null || !wd.FlowReady || InRampage || CinematicLock || IsDead) return;
            wd.SpendFlow();
            Tell("rampage");
            float now = Time.unscaledTime;
            // used at the right moment, it gives more: a second wind when nearly beaten ...
            if (Health <= maxHealth * 0.35f)
            {
                Health = Mathf.Min(maxHealth, Health + maxHealth * 0.33f);
                wd.AddStyle(150, "SECOND WIND");
            }
            // ... and a bigger blast when surrounded
            int near = 0;
            foreach (var e in Enemy.All)
                if (e.Alive && Mathf.Abs(e.transform.position.x - transform.position.x) < 3.2f && Mathf.Abs(e.transform.position.y - transform.position.y) < 1.2f) near++;
            surroundedAtStart = near >= 3;
            rampageIntroUntil = now + RampageIntroSeconds;
            rampageUntil = rampageIntroUntil + rampageSeconds;      // (invulnerable all through)
            StartCoroutine(RampageIntroRoutine());
        }

        System.Collections.IEnumerator RampageIntroRoutine()
        {
            var tc = TimeController.Instance;
            var cam = CameraDirector.Instance;
            var fx = InkFx.Instance;
            var chest = Chest;
            // the freeze, the flash, the ink and the speed lines
            if (tc != null) tc.SlowMo(0.04f, RampageIntroSeconds, 0.02f, 0.12f);
            if (fx != null) { fx.ImpactFlash(0.08f); fx.Burst(chest, 8f); fx.Splash(chest, Vector3.up, 46, true); }
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f + Random.Range(-0.08f, 0.08f);
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                StreakFx.Slash(chest + d * 1.6f, d, Random.Range(2.2f, 3.6f), 0.035f, 0.55f);
            }
            Sound.Play(Sfx.SlowmoIn, 1f);
            Sound.Play(Sfx.CineMassive, 1f);
            Sound.Play(Sfx.Boom, 0.8f);
            Sound.Play(Sfx.HeroEffort, 1f, transform.position, 0.85f);
            body.Play(body.Has("duck") ? "duck" : IdleClip, 0.05f);
            SetTrails(true);
            bool risen = false, waved = false;
            while (RampageIntro)
            {
                float t = RampageIntroT;
                // the push-in: low, close, slightly to his front
                if (cam != null)
                {
                    float wIn = Mathf.SmoothStep(0f, 1f, t / 0.12f), wOut = Mathf.SmoothStep(0f, 1f, (1f - t) / 0.2f);
                    var c = transform.position + Vector3.up * 0.75f;
                    var pos = c + new Vector3(Facing * 0.9f, -0.3f, -Mathf.Lerp(3.3f, 2.3f, Mathf.SmoothStep(0f, 1f, t)));
                    cam.Scripted(pos, c + Vector3.up * 0.2f, Mathf.Min(wIn, wOut), Mathf.Lerp(32f, 26f, t));
                }
                if (!risen && t > 0.55f) { risen = true; body.Play(body.Has("uppercut") ? "uppercut" : IdleClip, 0.05f); }
                if (!waved && t > 0.72f) { waved = true; RampageShockwave(); }
                yield return null;
            }
            if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
            if (tc != null) tc.Rampage(0.4f, rampageSeconds);
            SetTrails(false);
            EnterIdle();
            Sound.Play(Sfx.SlowmoOut, 0.8f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(300, "RAMPAGE!");
        }

        /// <summary>The blast at the end of the intro: everyone near is thrown back.</summary>
        void RampageShockwave()
        {
            var p = transform.position;
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable) continue;
                var q = e.transform.position;
                float d = Mathf.Abs(q.x - p.x);
                float reach = surroundedAtStart ? 5.2f : 3.8f;
                if (d > reach || Mathf.Abs(q.y - p.y) > 1.2f) continue;
                int dir = q.x >= p.x ? 1 : -1;
                e.Interrupt();
                e.TakeHit(new HitInfo { damage = surroundedAtStart ? 12f : 6f, direction = dir, knockback = Mathf.Lerp(12f, 5f, d / reach),
                                         knockdown = surroundedAtStart || d < 2.2f, attacker = this });
            }
            StreakFx.Sparks(p + Vector3.up * 0.1f, Vector3.right, 22, 10f);
            StreakFx.Sparks(p + Vector3.up * 0.1f, Vector3.left, 22, 10f);
            if (InkFx.Instance != null) { InkFx.Instance.ImpactFlash(0.05f); InkFx.Instance.Burst(p + Vector3.up * 0.4f, 6f); }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.4f, 0.35f);
            Sound.Play(Sfx.Boom, 1f);
            Sound.Play(Sfx.CineHit, 0.8f);
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
        }

        /// <summary>During Rampage every tap blinks next to an enemy on that side (any floor)
        /// and lands a one-hit finisher.</summary>
        void RampageStrike(int side)
        {
            var t = PickTarget(side, 30f) ?? PickTarget(-side, 30f);
            if (t == null) return;
            int dir = t.transform.position.x > transform.position.x ? 1 : -1;
            var p = t.transform.position - new Vector3(dir * 1.0f, 0f, t.transform.position.z);
            if (InkFx.Instance != null) InkFx.Instance.Burst(Chest, 2.2f);   // vanish...
            transform.position = p;                                           // ...and appear
            velocity = Vector3.zero;
            Grounded = true;
            SetFacing(dir);
            AttackData move = Style == FightStyle.Sword ? AttackData.SwordC
                            : Style == FightStyle.Pipe ? AttackData.PipeC
                            : Style == FightStyle.Gun ? AttackData.GunShoot
                            : (Random.value < 0.5f ? AttackData.SpinKick : AttackData.Roundhouse);
            currentAttack = move;
            target = t;
            attackDir = dir;
            hitDone = false;
            isCounter = true;
            state = State.Attack;
            stateTime = 0f;
            body.Play(move.clip, 0.02f, true, 1.25f);
            SetTrails(!move.ranged);
            if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(6f, 0.12f);
        }

        // ------------------------------------------------------------------ choosing the move

        int punchStep, kickStep;
        string comboLabel;

        /// <summary>The next move of the punch (tap high) or kick (tap low) string. A string
        /// carries on while taps keep coming and resets after a pause; its last hit is the finisher.</summary>
        AttackData NextInString(bool kick, Enemy t)
        {
            var list = kick ? AttackData.KickCombo : AttackData.PunchCombo;
            int i = (kick ? kickStep++ : punchStep++) % list.Length;
            if (kick) punchStep = 0; else kickStep = 0;          // switching limbs starts the other string
            var move = list[i];
            // the killing blow is always the string's finisher: cinematic, never a plain jab
            if (t != null && t.Health <= move.damage && i < list.Length - 1) { move = list[list.Length - 1]; i = list.Length - 1; }
            comboLabel = i == list.Length - 1 ? (kick ? "KICK COMBO!" : "PUNCH COMBO!") : null;
            return move;
        }

        void Strike(int side, bool kick = false)
        {
            if (InRampage) { RampageStrike(side); return; }
            if (state == State.Idle && Time.time - lastAttackEnd > comboWindow) { comboStep = 0; closeStep = 0; punchStep = 0; kickStep = 0; ResetString(); }
            comboLabel = null;

            // a knife flying in from that side: catch it and send it back
            var incoming = IncomingKnife(side);
            if (incoming != null) { CatchKnife(incoming, side); return; }

            if (Style == FightStyle.Gun && !kick) { GunStrike(side); return; }   // kicks work with any weapon in hand

            var t = PickTarget(side, Style == FightStyle.Knife ? 16f : targetRange);
            if (Contact && t != null && Mathf.Abs(t.transform.position.y - transform.position.y) > 1.0f) t = null;   // no leaps between floors
            if (ContactBare && !kick && TryParry(t, side)) return;
            if (ContactBare && !kick && TryClothesline(t, side)) return;

            // a crate right there and no enemy in its way: kick it like a bowling ball
            pendingCrate = null; pendingDrum = null;                // a kick cut short never fires later
            var crate = NearCrate(side);
            var drum = crate == null ? OilDrum.Near(transform.position, side) : null;     // or an oil drum: over it goes, rolling
            if ((crate != null || drum != null) && (t == null || Mathf.Abs(t.transform.position.x - transform.position.x) > 1.4f) && Grounded)
            {
                SetFacing(side);
                pendingCrate = crate;
                pendingDrum = drum;
                StartAttack(AttackData.FrontKick, null, side, false);
                return;
            }

            // an enemy on the floor right there, nobody else in reach: stomp it or swing it
            var downed = PickDowned(side);
            bool someoneClose = t != null && Mathf.Abs(t.transform.position.x - transform.position.x) < 1.3f &&
                                Mathf.Abs(t.transform.position.y - transform.position.y) < 1.0f;
            if (downed != null && !someoneClose && (Style == FightStyle.Fists || Style == FightStyle.Knife))
            {
                if (!Contact && comboStep++ % 2 == 0 && Enemy.All.Count > 1) BeginGiantSwing(downed, side);
                else BeginStomp(downed, side);
                return;
            }

            if (!kick && Style == FightStyle.Knife && t != null && Mathf.Abs(t.transform.position.x - transform.position.x) > 1.8f)
            {
                KnifeThrow(t);
                return;
            }

            // situational moves, the building and its objects first
            if (TryHandStomp(side)) return;
            if (TryOverTheEdge(t, side)) return;
            if (!Contact && TrySpringboard(t, side)) return;
            if (!Contact && t != null && t.transform.position.y < transform.position.y - 1.8f && TryDeathFromAbove(t)) return;
            // the head scissors takedown: in story mode, only on a man out on his feet (no dice)
            if ((!Contact || (t != null && t.Dazed && kick)) && TryHeadScissors(t, side, kick)) return;

            bool armed = !kick && (Style == FightStyle.Sword || Style == FightStyle.Pipe || Style == FightStyle.Knife);
            var combo = Style == FightStyle.Sword ? AttackData.SwordCombo : AttackData.PipeCombo;
            if (t == null)
            {
                SetFacing(side);
                if (!kick && Style == FightStyle.Knife) { StartAttack(AttackData.KnifeStab, null, side, false); return; }
                if (ContactFists || (Contact && kick && Style != FightStyle.Gun)) { var air = ContactMove(kick, null, false); comboLabel = air.name; StartAttack(air, null, side, false); return; }
                StartAttack(armed ? combo[comboStep++ % combo.Length] : NextInString(kick, null), null, side, false);
                return;
            }

            float dx = t.transform.position.x - transform.position.x;
            float dy = t.transform.position.y - transform.position.y;
            bool counter = t.Telegraphing && Mathf.Abs(dx) < 4f && Mathf.Abs(dy) < 1.4f;
            bool behindClose = Facing != side && Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 0.5f;


            AttackData move;
            if (ContactFists || (Contact && kick && Style != FightStyle.Gun))
            {
                move = ContactMove(kick, t, behindClose);
                comboLabel = move.name;
                counter = false;
            }
            else if (dy > 1.4f || dy < -1.4f)
                move = Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick;
            else if (armed && (Style == FightStyle.Sword || Style == FightStyle.Pipe))
            {
                var finisher = Style == FightStyle.Sword ? AttackData.SwordC : AttackData.PipeC;
                bool iai = Style == FightStyle.Sword && SwordSlowReady && Mathf.Abs(dy) < 0.6f && Grounded &&
                           (counter || InLine(side) >= 2 || (t.Health <= AttackData.SwordIai.damage && comboStep % 3 == 2 && t.Type != EnemyType.Brute)
                            || (t.Type == EnemyType.Brute && t.Health <= AttackData.SwordIai.damage));
                if (iai) { move = AttackData.SwordIai; comboStep++; }
                else if (counter) move = finisher;
                else if (behindClose && Style == FightStyle.Sword) move = AttackData.SwordBack;
                else if (dy > 0.8f && Style == FightStyle.Sword) move = AttackData.SwordLeap;
                else
                {
                    move = combo[comboStep % combo.Length];
                    if (t.Health <= move.damage && comboStep % 3 == 2) move = finisher;
                    comboStep++;
                }
            }
            else if (armed && Style == FightStyle.Knife)
                move = AttackData.KnifeStab;
            else
            {
                if (counter) move = kick ? AttackData.SpinKick : AttackData.Counter;
                else if (behindClose) move = AttackData.BackKick;
                else if (dy > 0.8f) move = AttackData.FlyingKick;
                // after a full punch string, the next tap grabs and hurls them
                else if (!kick && punchStep >= AttackData.PunchCombo.Length && Mathf.Abs(dx) < 2.2f && t.Type != EnemyType.Brute)
                { move = AttackData.GrabThrow; punchStep = 0; }
                else move = NextInString(kick, t);
            }

            if (!move.behind) SetFacing(side);
            StartAttack(move, t, side, counter);
        }

        void GunStrike(int side)
        {
            var t = PickTarget(side, gunRange);
            bool behind = Facing != side;
            if (t == null)
            {
                if (!behind) SetFacing(side);
                StartAttack(behind ? AttackData.GunShootBack : AttackData.GunShoot, null, side, false);
                return;
            }
            float dist = Mathf.Abs(t.transform.position.x - transform.position.x);
            bool counter = t.Telegraphing && dist < 4f;
            bool sameFloor = Mathf.Abs(t.transform.position.y - transform.position.y) < 0.6f;

            if (dist <= closeQuarters && !counter && sameFloor)
            {
                // close quarters: pistol-whip, then grapple and throw (followed by a finishing shot)
                SetFacing(side);
                var move = closeStep++ % 2 == 0 ? AttackData.GunWhip : AttackData.GunThrow;
                if (t.Health <= AttackData.GunWhip.damage) move = AttackData.GunThrow;
                StartAttack(move, t, side, false);
                return;
            }
            if (!behind) SetFacing(side);
            StartAttack(behind ? AttackData.GunShootBack : AttackData.GunShoot, t, side, counter);
        }

        void JumpAttack()
        {
            if (InRampage) { RampageStrike(Facing); return; }
            var t = PickTarget(Facing, 9f) ?? PickTarget(-Facing, 9f);
            if (t != null) SetFacing(t.transform.position.x > transform.position.x ? 1 : -1);
            StartAttack(Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick, t, Facing, false);
        }

        // ------------------------------------------------------------------ jumping, ropes, bars, walls

        Crate NearCrate(int side)
        {
            foreach (var c in Crate.All)
            {
                if (!c.Available) continue;
                float dx = (c.transform.position.x - transform.position.x) * side;
                if (dx > 0f && dx < 1.3f && Mathf.Abs(c.transform.position.y - 0.4f - transform.position.y) < 0.7f) return c;
            }
            return null;
        }

        /// <summary>Swipe up: grab a rope or bar in reach, run up a wall into a corkscrew,
        /// climb the ledge, or simply jump (with air control and air strikes).</summary>
        void Jump()
        {
            if (InRampage) { RampageStrike(Facing); return; }
            if (!Grounded) return;
            var p = transform.position;

            // the tower: a ledge in reach above, climb it (jump, hang by the hands, pull up)
            if (LevelNav.Active && Swingable.Reachable(p, HandReach + 1.5f, 1.8f) == null)
            {
                int k = LevelNav.LedgeFor(p, LevelNav.PlatformAt(p));
                if (k >= 0) { GoToLedge(k); return; }
            }

            // a rope or bar overhead: jump for it
            var sw = Swingable.Reachable(p, HandReach + 1.5f, 1.8f);
            if (sw != null)
            {
                float rise = Mathf.Max(0.4f, sw.Grip.y - (p.y + HandReach) + 0.25f);
                state = State.Air;
                stateTime = 0f;
                holding = null;
                velocity.y = Mathf.Sqrt(2f * gravity * rise);
                float t = velocity.y / gravity;
                velocity.x = (sw.Grip.x - p.x) / Mathf.Max(0.2f, t);
                Grounded = false;
                body.Play("jump", 0.05f);
                return;
            }

            // running into a wall: up it and off in a corkscrew (not in story mode: nobody runs up walls)
            int towards = AxisDir != 0 ? AxisDir : Facing;
            float wallX = towards > 0 ? arenaMaxX : arenaMinX;
            if (!Contact && Mathf.Abs(wallX - p.x) < 1.4f)
            {
                wallDir = towards;
                SetFacing(towards);
                state = State.WallRun;
                stateTime = 0f;
                swingHit.Clear();
                body.Play("run", 0.04f, true, 2f);
                Sound.Play(Sfx.Footstep, 1.6f, transform.position);
                Sound.Play(Sfx.Cloth, 1f, transform.position);
                return;
            }

            // a plain jump
            state = State.Air;
            stateTime = 0f;
            velocity.y = Contact ? ContactJump : jumpSpeed;           // story: a short, heavy hop
            Grounded = false;
            body.Play("jump", 0.05f);
            Sound.Play(Sfx.Footstep, 1.4f, transform.position);
            Sound.Play(Sfx.Cloth, 0.8f, transform.position);
        }

        void UpdateAir(float dt)
        {
            if (springTarget != null && stateTime > 0.22f)
            {
                // off the crate and into a flying kick
                var st = springTarget;
                springTarget = null;
                if (st.Targetable)
                {
                    int sd = st.transform.position.x > transform.position.x ? 1 : -1;
                    SetFacing(sd);
                    currentAttack = AttackData.FlyingKick;
                    target = st;
                    attackDir = sd;
                    hitDone = false;
                    state = State.Attack;
                    stateTime = 0f;
                    body.Play("flying_kick", 0.04f, true, 1.3f);
                    SetTrails(true);
                    return;
                }
            }
            // air control, a little softer than on the ground
            float a = Axis;
            if (Mathf.Abs(a) > 0.1f) { velocity.x = Mathf.MoveTowards(velocity.x, a * runSpeed, 18f * dt); SetFacing(a > 0 ? 1 : -1); }
            // catch a rope or bar as the hands pass it
            var hands = transform.position + Vector3.up * HandReach;
            foreach (var sw in Swingable.All)
            {
                if (sw.Held) continue;
                if (Vector3.Distance(new Vector3(hands.x, hands.y, 0f), new Vector3(sw.Grip.x, sw.Grip.y, 0f)) < 0.5f)
                {
                    if (sw.kind == SwingKind.Rope) GrabRope(sw); else GrabBar(sw);
                    return;
                }
            }
            // strike in the air: a flying kick at whoever is near
            var cmd = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
            if (cmd == Command.StrikeLeft || cmd == Command.StrikeRight || cmd == Command.StrikeForward)
            {
                int side = cmd == Command.StrikeLeft ? -1 : cmd == Command.StrikeRight ? 1 : (AxisDir != 0 ? AxisDir : Facing);
                var t = PickTarget(side, 5f);
                // close enough in the air: land on his shoulders instead of kicking him
                if (t != null && Mathf.Abs(t.transform.position.x - transform.position.x) < 1.9f && Random.value < 0.65f && CanScissors(t, side, true))
                { BeginScissors(t, side); return; }
                // far below: dive on him
                var below = PickBelow(side, 4.5f);
                if (below != null && TryDeathFromAbove(below)) return;
                SetFacing(side);
                currentAttack = AttackData.FlyingKick;
                target = t;
                attackDir = side;
                hitDone = false;
                state = State.Attack;
                stateTime = 0f;
                body.Play("flying_kick", 0.04f, true, 1.3f);
                SetTrails(true);
                return;
            }
            if (cmd == Command.ThrowWeapon && Style != FightStyle.Fists) { ThrowWeapon(); return; }
            if (Grounded && stateTime > 0.1f) EnterIdle();
        }

        // ---- rope: a pendulum the hero pumps; feet sweep enemies; let go into a flying kick
        void GrabRope(Swingable sw)
        {
            Tell("grab");
            holding = sw;
            sw.Held = true;
            var hands = transform.position + Vector3.up * HandReach;
            var d = hands - sw.anchor;
            swingAng = Mathf.Atan2(d.x, -d.y);
            swingVel = velocity.x * Mathf.Cos(swingAng) / sw.length;
            swingHit.Clear();
            state = State.Rope;
            stateTime = 0f;
            velocity = Vector3.zero;
            body.Play("pike", 0.08f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            Sound.Play(Sfx.WhooshBig, 0.7f, transform.position);
            if (sw.trapeze) Sound.Play(Sfx.MetalClang, 0.35f, transform.position, 1.3f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(30, sw.trapeze ? "TRAPEZE!" : "ROPE!");
        }

        void UpdateRope(float dt)
        {
            var sw = holding;
            if (sw == null) { EnterIdle(); return; }
            float L = sw.length;
            // gravity, plus pumping in the stick's direction (strongest at the bottom of the arc)
            swingVel += (-(gravity / L) * Mathf.Sin(swingAng) + Axis * 3.2f * Mathf.Cos(swingAng)) * dt;
            swingVel *= 1f - 0.12f * dt;
            swingAng = swingAng + swingVel * dt;
            if (swingAng < sw.minAngle || swingAng > sw.maxAngle) { swingAng = Mathf.Clamp(swingAng, sw.minAngle, sw.maxAngle); swingVel *= -0.3f; }
            sw.SetAngle(swingAng);
            PlaceHanging(sw.Grip, swingAng, swingVel);
            SweepWithFeet(Mathf.Abs(swingVel) * L, sw.trapeze ? "TRAPEZE KICK!" : "ROPE KICK!");

            var cmd = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
            bool letGo = cmd == Command.JumpKick || cmd == Command.StrikeLeft || cmd == Command.StrikeRight ||
                         cmd == Command.StrikeForward || cmd == Command.DodgeLeft || cmd == Command.DodgeRight || stateTime > 6f;
            if (letGo)
            {
                sw.LetGo(swingVel);
                var tangent = new Vector3(Mathf.Cos(swingAng), Mathf.Sin(swingAng), 0f) * (swingVel * L);
                LaunchKick(tangent + Vector3.up * 2.5f);
            }
        }

        // ---- bar: giant circles round the rod, then a dismount into a flying kick
        void GrabBar(Swingable sw)
        {
            Tell("grab");
            holding = sw;
            sw.Held = true;
            int dir = Mathf.Abs(velocity.x) > 0.5f ? (velocity.x > 0 ? 1 : -1) : Facing;
            SetFacing(dir);
            swingAng = 0f;
            swingVel = dir * 5f;
            swingTurned = 0f;
            swingHit.Clear();
            state = State.Bar;
            stateTime = 0f;
            velocity = Vector3.zero;
            body.Play("pike", 0.06f);
            Sound.Play(Sfx.MetalClang, 0.45f, transform.position, 1.25f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
        }

        void UpdateBar(float dt)
        {
            var sw = holding;
            if (sw == null) { EnterIdle(); return; }
            int dir = swingVel >= 0 ? 1 : -1;
            swingVel = Mathf.MoveTowards(swingVel, dir * 10f, 9f * dt);       // building speed
            float step = swingVel * dt;
            swingAng += step;
            swingTurned += Mathf.Abs(step);
            PlaceHanging(sw.Grip, swingAng, swingVel);
            SweepWithFeet(Mathf.Abs(swingVel) * HandReach, "BAR SPIN!");

            var cmd = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
            bool asked = cmd != Command.None && cmd != Command.Restart;
            // dismount just past the bottom of a turn, so the flight goes forwards and up
            float phase = Mathf.Repeat(swingAng * dir, Mathf.PI * 2f);
            bool goodPhase = phase > 0.6f && phase < 1.0f;
            if ((asked || swingTurned > Mathf.PI * 2.2f) && goodPhase || stateTime > 4f)
            {
                sw.Held = false;
                var tangent = new Vector3(Mathf.Cos(swingAng), Mathf.Sin(swingAng), 0f) * (swingVel * HandReach);
                LaunchKick(tangent * 0.9f + Vector3.up * 2f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("DISMOUNT!");
            }
        }

        /// <summary>Hang from a grip point with the body along the line to it, rotated by angle.</summary>
        void PlaceHanging(Vector3 grip, float ang, float angVel)
        {
            if (Mathf.Abs(angVel) > 0.4f && state == State.Rope) { int f = angVel > 0 ? 1 : -1; if (f != Facing) SetFacing(f); }
            var up = new Vector3(-Mathf.Sin(ang), Mathf.Cos(ang), 0f);
            transform.position = new Vector3(grip.x, grip.y, 0f) - up * HandReach;
            transform.rotation = Quaternion.AngleAxis(ang * Mathf.Rad2Deg, Vector3.forward) * Quaternion.Euler(0f, Facing > 0 ? 0f : 180f, 0f);
        }

        /// <summary>The feet hit whoever they sweep through while moving fast.</summary>
        void SweepWithFeet(float speed, string label)
        {
            if (speed < 3f) return;
            var feet = transform.position;
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable || swingHit.Contains(e)) continue;
                if (Vector3.Distance(e.Chest, feet) < 0.9f || Vector3.Distance(e.transform.position + Vector3.up * 0.4f, feet) < 0.7f)
                {
                    swingHit.Add(e);
                    int d = e.transform.position.x > feet.x ? 1 : -1;
                    e.TakeHit(new HitInfo { damage = 24f, direction = d, knockback = 9f, knockdown = true, heavy = true, attacker = this });
                    if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(d, 0.4f, 0f), 26, e.InkColor);
                    Sound.Play(Sfx.Kick, 1f, e.Chest);
                    if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.12f);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(e, label, 170);
                }
            }
        }

        /// <summary>Let go of a rope or bar and fly into a kick.</summary>
        void LaunchKick(Vector3 vel)
        {
            holding = null;
            int dir = vel.x >= 0 ? 1 : -1;
            SetFacing(dir);                                  // also clears the swing's roll
            currentAttack = AttackData.FlyingKick;
            target = PickTarget(dir, 8f);
            attackDir = dir;
            hitDone = false;
            state = State.Attack;
            stateTime = 0f;
            velocity = vel;
            Grounded = false;
            body.Play("flying_kick", 0.05f, true, 1.2f);
            SetTrails(true);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.4f, 0.4f);
        }

        // ---- wall: run two steps up it, then a corkscrew flip off it
        void UpdateWallRun(float dt)
        {
            if (stateTime < 0.25f)
            {
                var p = transform.position;
                p.y += 6.5f * dt;
                transform.position = p;
                return;
            }
            if (body.CurrentName != "dodge")
            {
                velocity = new Vector3(-wallDir * 9.5f, 7f, 0f);
                Grounded = false;
                body.Play("dodge", 0.04f);       // the backflip carries us away from the wall
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.45f, 0.5f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("CORKSCREW!");
            }
            // the corkscrew: a full twist while flipping
            float spin = Mathf.Clamp01((stateTime - 0.25f) / 0.6f) * 360f;
            transform.rotation = Quaternion.Euler(0f, (Facing > 0 ? 0f : 180f) + spin, 0f);
            SweepWithFeet(9f, "CORKSCREW!");
            if (Grounded && stateTime > 0.45f)
            {
                SetFacing(-wallDir);
                EnterIdle();
            }
        }

        // ------------------------------------------------------------------ ledge

        int navLedge = -1;      // the tower ledge being run at (-1: the classic hall's ledge)

        void GoToLedge(int towerLedge = -1)
        {
            navLedge = towerLedge;
            state = State.ToLedge;
            stateTime = 0f;
            body.Play("run", 0.06f, false, 1.6f);
        }

        void UpdateToLedge(float dt)
        {
            var p = transform.position;
            float fx = navLedge >= 0 ? LevelNav.Ledges[navLedge].FootX : LedgeFootX;
            FaceTowards(fx);
            p.x = Mathf.MoveTowards(p.x, fx, 11f * dt);
            transform.position = p;
            if (Mathf.Abs(p.x - fx) < 0.05f || stateTime > 1.2f)
            {
                if (navLedge >= 0)
                {
                    var L = LevelNav.Ledges[navLedge];
                    BeginLedgeClimb(0.08f, L.x, L.y, L.dir);
                }
                else BeginLedgeClimb(0.1f);
                Sound.Play(Sfx.Cloth, 1f, transform.position);
                Sound.Play(Sfx.HeroEffort, 0.8f, transform.position);
                Tell("climb");
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(40, "CLIMB");
            }
        }

        /// <summary>Holding a weapon with an enemy ahead on this level: swipe down throws it.</summary>
        bool WantsThrow()
        {
            if (Style == FightStyle.Fists) return false;
            var p = transform.position;
            foreach (var e in Enemy.All)
                if (e.Targetable && Mathf.Abs(e.transform.position.y - p.y) < 0.8f && Mathf.Abs(e.transform.position.x - p.x) < 16f) return true;
            return false;
        }

        /// <summary>Swipe down on a one-way floor (a mezzanine or the catwalk): drop through it
        /// to the level below. Returns false where there is nothing to drop through.</summary>
        bool TryDropThrough()
        {
            if (!LevelNav.Active || !Grounded || state != State.Idle) return false;
            int pl = LevelNav.PlatformAt(transform.position);
            if (pl < 0 || !LevelNav.Platforms[pl].oneWay) return false;
            dropThroughUntil = Time.time + 0.3f;
            Grounded = false;
            velocity = new Vector3(velocity.x, 0.5f, 0f);
            state = State.Air;
            stateTime = 0f;
            body.Play("jump", 0.06f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);
            Tell("drop");
            TryDeathFromAbove(null);
            return true;
        }

        protected override void OnLanded(float fallHeight)
        {
            // a long fall onto the catwalk's glass goes straight through it (a dive keeps diving)
            if (LevelNav.Active && GlassPanel.Land(transform.position, fallHeight - 0.9f, state == State.Dive)) { Grounded = false; return; }
            if (state == State.Dive && !diveLanded) { DiveImpact(fallHeight); return; }
            if ((state == State.Air || state == State.Idle) && fallHeight >= 3.2f && !CinematicLock) HeroLanding(fallHeight);
        }

        protected override void OnLedgeClimbed() { EnterIdle(); }

        // ------------------------------------------------------------------ bodies on the floor

        void BeginStomp(Enemy e, int side)
        {
            SetFacing(side);
            downTarget = e;
            StartAttack(AttackData.Stomp, null, side, false);
        }

        void BeginGiantSwing(Enemy e, int side)
        {
            SetFacing(side);
            downTarget = e;
            swingSide = side;
            swingAngle = side > 0 ? 0f : 180f;
            swingHit.Clear();
            e.BeginCarried();
            state = State.Swing;
            stateTime = 0f;
            body.Play("swing_hold", 0.08f);
            Sound.Play(Sfx.WhooshBig, 1f, transform.position);
            Sound.Play(Sfx.HeroEffort, 1f, transform.position);
        }

        void UpdateSwing(float dt)
        {
            var e = downTarget;
            if (e == null || !e.Carried) { SetFacing(swingSide); EnterIdle(); return; }
            float speed = Mathf.Lerp(360f, 900f, Mathf.Clamp01(stateTime / 0.5f));
            swingAngle += speed * dt;
            transform.rotation = Quaternion.Euler(0f, swingAngle, 0f);
            Vector3 fwd = transform.right;                                     // the hero's facing
            var holdPos = transform.position + fwd * 1.1f + Vector3.up * 0.75f;
            e.transform.position = holdPos;
            e.transform.rotation = Quaternion.Euler(0f, swingAngle + 180f, 0f);  // feet to the hero, head out
            foreach (var other in Enemy.All.ToArray())
            {
                if (other == e || !other.Targetable || swingHit.Contains(other)) continue;
                if (Vector3.Distance(other.Chest, holdPos) < 1.0f)
                {
                    swingHit.Add(other);
                    other.TakeHit(new HitInfo { damage = 22f, direction = other.transform.position.x > transform.position.x ? 1 : -1, knockback = 9f, knockdown = true, heavy = true, attacker = this });
                    if (InkFx.Instance != null) InkFx.Instance.Splash(other.Chest, Vector3.up, 24, other.InkColor);
                    Sound.Play(Sfx.BodyFall, 1f, other.Chest);
                    Sound.Play(Sfx.PunchHeavy, 1f, other.Chest);
                    if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(other, "SWEPT!", 150);
                }
            }
            if (stateTime > 1.15f)
            {
                SetFacing(swingSide);
                e.EndCarried(new Vector3(swingSide * 13f, 4.5f, 0f), this);
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.5f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("GIANT SWING!");
                downTarget = null;
                EnterIdle();
            }
        }

        // ------------------------------------------------------------------ knives

        void CatchKnife(EnemyKnife k, int side)
        {
            var thrower = k.thrower;
            k.Caught();
            SetFacing(side);
            catchPending = true;
            BeginThrow(thrower != null && thrower.Targetable ? thrower : PickTarget(side, 16f));
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 0.6f);
            if (InkFx.Instance != null) InkFx.Instance.Burst(Chest + Vector3.right * side * 0.5f, 1.5f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("CAUGHT!");
            if (Style == FightStyle.Fists) Equip(FightStyle.Knife, 1);      // keep one for later
            else if (Style == FightStyle.Knife) Knives = Mathf.Min(6, Knives + 1);
        }

        // ------------------------------------------------------------------ performing it

        void StartAttack(AttackData move, Enemy t, int side, bool counter)
        {
            currentAttack = move;
            target = t;
            isCounter = counter;
            attackDir = move.behind ? -Facing : side;
            hitDone = false;
            if (counter && TimeController.Instance != null && t != null)
            {
                TimeController.Instance.SlowMo(0.25f, 0.9f);
                t.Interrupt();
            }
            bool leap = move.clip == "flying_kick" || move.clip == "sword_leap" || move.iai;
            if (t != null && !move.ranged && !move.behind && !leap)
            {
                float gap = Mathf.Abs(t.transform.position.x - transform.position.x);
                if (gap > Reach(move) + 0.15f)
                {
                    state = State.Dash;
                    stateTime = 0f;
                    body.Play("run", 0.05f, false, Contact ? 1.1f : 1.7f);     // story: a few quick steps in, not a dash
                    return;
                }
            }
            BeginSwing();
        }

        void UpdateDash(float dt)
        {
            if (target == null || !target.Targetable) { BeginSwing(); return; }
            float goal = target.transform.position.x - attackDir * Reach(currentAttack) * 0.92f;
            velocity.x = 0f;
            var p = transform.position;
            p.x = Mathf.MoveTowards(p.x, goal, (Contact ? ContactStep : dashSpeed * (Style == FightStyle.Sword ? 1.35f : 1f)) * dt);
            transform.position = p;
            if (Style == FightStyle.Sword && !Contact) Ghosts(0.045f, 0.35f);
            if (Mathf.Abs(p.x - goal) < 0.05f || stateTime > (Contact ? 0.65f : 0.6f)) BeginSwing();
        }

        void BeginSwing()
        {
            state = State.Attack;
            stateTime = 0f;
            bool swordClip = Style == FightStyle.Sword && currentAttack.clip != null && currentAttack.clip.StartsWith("sword");
            bool quick = currentAttack.clip == "jab" || currentAttack.clip == "cross" || currentAttack.clip == "hook" || currentAttack.clip == "lead_hook" || currentAttack.clip == "back_fist";
            body.Play(currentAttack.clip, swordClip || quick ? 0.03f : 0.05f, true, swordClip && !currentAttack.iai ? 1.2f : quick ? 1.15f : 1f);
            SetTrails(!currentAttack.ranged);
            if (currentAttack.iai) BeginIai();
            if (!currentAttack.ranged) SwingSound();
            if (currentAttack.clip == "slide_kick") velocity.x = attackDir * (Contact ? 7f : 11f);
            if (currentAttack.clip == "flying_kick" || currentAttack.clip == "sword_leap")
            {
                // the animation already jumps; physics carries the hero to another floor
                velocity.x = 0f;
                if (target != null)
                {
                    float dy = target.transform.position.y - transform.position.y;
                    if (dy > 0.5f)
                    {
                        velocity.y = Mathf.Sqrt(2f * gravity * (dy + 0.9f));   // leap up through to the upper floor
                        Grounded = false;
                    }
                    else if (dy < -1.4f)
                    {
                        velocity.y = 3f;                                         // hop off, down onto them
                        Grounded = false;
                        if (LevelNav.Active) dropThroughUntil = Time.time + 0.35f;   // the tower: straight through the floor
                    }
                }
            }
        }

        void UpdateAttack()
        {
            var e = body.Current;
            if (e == null) { EnterIdle(); return; }

            bool glides = currentAttack.clip == "flying_kick" || currentAttack.clip == "spin_kick" || currentAttack.clip == "sword_leap";
            if (glides && !hitDone && target != null && target.Targetable)
            {
                var p = transform.position;
                float goal = target.transform.position.x - attackDir * Reach(currentAttack) * 0.9f;
                p.x = Mathf.MoveTowards(p.x, goal, dashSpeed * 0.7f * Time.deltaTime);
                transform.position = p;
            }
            if (currentAttack.clip == "slide_kick")
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, 14f * Time.deltaTime);
            if (currentAttack.iai) UpdateIai();

            if (!hitDone && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                if (throwPending || knifeThrowPending || catchPending) ReleaseThrow();
                else if (currentAttack.clip == "stomp") ResolveStomp();
                else if (currentAttack.ranged) ResolveShot();
                else if (currentAttack.iai) ResolveIai();
                else ResolveHit();
            }

            if (CanCancel())
            {
                var next = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
                if (next != Command.None) { HandleCommand(next); return; }
            }
            if (body.Finished && Grounded)
            {
                if (pendingExecution != null) { StartExecution(); return; }
                if (WeaponSpent && !InRampage) { ThrowWeapon(); return; }   // worn out: throw it
                lastAttackEnd = Time.time;
                EnterIdle();
            }
        }

        void ResolveStomp()
        {
            hitDone = true;
            var e = downTarget;
            downTarget = null;
            if (e == null || !e.Alive) return;
            e.Kill(new HitInfo { direction = Facing, knockback = 0f, heavy = true, attacker = this });
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(e.transform.position + Vector3.up * 0.3f, Vector3.up, 40, e.InkColor);
                Sound.Play(Sfx.Finisher, 1f, e.transform.position);
                InkFx.Instance.ImpactFlash(0.04f);
            }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.2f, 0.2f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(7);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroKill(e, "STOMP!", 150, false);
        }

        void ResolveHit()
        {
            hitDone = true;
            if (pendingDrum != null)
            {
                pendingDrum.Kick(attackDir, this);
                pendingDrum = null;
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(40, "KICK!");
                Impact();
                return;
            }
            if (pendingCrate != null)
            {
                pendingCrate.Kick(attackDir, this);
                Tell("crate");
                pendingCrate = null;
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(40, "KICK!");
                Impact();
                return;
            }
            var hit = new HitInfo
            {
                damage = currentAttack.damage * RageScale,
                direction = attackDir,
                knockback = currentAttack.knockback,
                knockdown = currentAttack.knockdown,
                heavy = currentAttack.heavy,
                lift = currentAttack.lift,
                low = IsKickClip(currentAttack.clip),
                react = currentAttack.react,
                attacker = this
            };

            bool landed = false;
            int cut = 0;
            bool bladeCut = Style == FightStyle.Sword && currentAttack.clip != null && currentAttack.clip.StartsWith("sword");
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable && !(e.OnLedge && e == target)) continue;
                float dx = (e.transform.position.x - transform.position.x) * attackDir;
                float dy = Mathf.Abs(e.transform.position.y - transform.position.y);
                bool primary = e == target;
                float reach = Reach(currentAttack) + (primary ? 0.4f : 0.1f);
                float yTol = currentAttack.clip == "flying_kick" || currentAttack.clip == "sword_leap" ? 1.8f : 1.0f;
                if (dx < -0.2f || dx > reach || dy > yTol) continue;
                if (!primary && !currentAttack.heavy && !currentAttack.cleave && target != null) continue;
                if (currentAttack.clip == "gun_throw" && Style != FightStyle.Gun && primary)
                {
                    // fists: grapple and hurl them into the others
                    e.TakeHit(new HitInfo { damage = currentAttack.damage, direction = attackDir, knockback = 0f, attacker = this });
                    landed = true;
                    if (edgeThrow)
                    {
                        // over the edge: a slow, high heave into the drop, the camera watching him go
                        edgeThrow = false;
                        if (e.Alive) e.Launch(new Vector3(attackDir * 6.5f, 6.5f, 0f), this);
                        OnStrikeLanded(e, !e.Alive);
                        Sound.Play(Sfx.FallScream, 1f, e.Chest);
                        if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 1.0f, 0.03f, 0.35f);
                        if (CameraDirector.Instance != null) CameraDirector.Instance.Focus(e.transform, 1.3f);
                        if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(200, "OVER THE EDGE!");
                        continue;
                    }
                    if (e.Alive) e.Launch(new Vector3(attackDir * 12f, 5f, 0f), this);
                    OnStrikeLanded(e, !e.Alive);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(60, "THROW!");
                    continue;
                }
                bool killing = InRampage || e.Health <= hit.damage;
                bool clash = bladeCut && e.Type == EnemyType.Swordsman && e.Telegraphing;
                if (InRampage) e.Kill(hit); else e.TakeHit(hit);
                landed = true;
                OnStrikeLanded(e, killing);
                if (bladeCut) { SwordHitFx(e, killing, clash); cut++; }
                if (currentAttack.clip == "gun_throw" && primary && !killing) pendingExecution = e;
            }
            if (landed)
            {
                if (!InRampage)
                {
                    if (Style == FightStyle.Sword) SwordUses--;
                    if (Style == FightStyle.Pipe) PipeUses--;
                }
                Impact();
                V3Landed();
                // a few slow-motion beats, never on every hit: cutting two at once, or a heavy killing blow
                if (bladeCut && SwordSlowReady && (cut >= 2 || (lastKilled && currentAttack.heavy)))
                {
                    swordSlowAt = Time.unscaledTime;
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, cut >= 2 ? 0.5f : 0.4f, 0.02f, 0.25f);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(5f, 0.2f);
                    if (cut >= 2 && WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(80 * cut, cut >= 3 ? "TRIPLE CUT!" : "DOUBLE CUT!");
                }
                if (bladeCut && currentAttack.lift > 0f && WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(30, "LAUNCH!");
                if (comboLabel != null)
                {
                    Tell("combo");
                    // the string's finisher landed: a beat of slow motion if it finished someone
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(120, comboLabel);
                    if (lastKilled && SwordSlowReady)
                    {
                        swordSlowAt = Time.unscaledTime;
                        if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.45f, 0.02f, 0.25f);
                        if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(5f, 0.2f);
                    }
                    comboLabel = null;
                }
            }
        }

        void ResolveShot()
        {
            hitDone = true;
            Vector3 from = muzzle != null ? muzzle.position : Chest + Vector3.right * attackDir * 0.6f;
            bool execution = currentAttack.clip == "gun_shoot_down";
            if (!execution && !InRampage) Ammo = Mathf.Max(0, Ammo - 1);

            Vector3 to;
            if (target != null && (execution ? target.Alive : target.Targetable))
            {
                to = execution ? target.transform.position + Vector3.up * 0.25f : target.Chest;
                bool killing = execution || InRampage || target.Health <= currentAttack.damage;
                var hit = new HitInfo
                {
                    damage = currentAttack.damage, direction = attackDir, knockback = currentAttack.knockback,
                    knockdown = killing, heavy = false, attacker = this
                };
                if (execution) target.Execute(this);
                else if (InRampage) target.Kill(hit);
                else target.TakeHit(hit);
                OnStrikeLanded(target, killing);
                Impact();
            }
            else to = from + new Vector3(attackDir, 0f, 0f) * 16f;

            if (InkFx.Instance != null) InkFx.Instance.Shot(from, to);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.05f, 0.08f);
        }

        bool lastKilled;
        Vector3 lastHitAt;

        /// <summary>What the hero just did, for the training reel and the chalk hints:
        /// punch, kick, combo, run, jump, climb, drop, dodge, rampage, crate, grab, dive, throw, pickup.</summary>
        public static event System.Action<string> Did;
        static void Tell(string what) { Did?.Invoke(what); }

        void OnStrikeLanded(Enemy e, bool killing)
        {
            Tell(IsKickClip(currentAttack.clip) ? "kick" : "punch");
            lastKilled = killing;
            lastHitAt = e.Chest;
            ComboCount++;
            ComboTimer = 2f;
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.OnHeroHit(e, ComboCount, currentAttack.heavy || currentAttack.cleave, killing, currentAttack.clip);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(e.Chest, new Vector3(attackDir, 0.3f, 0f), killing ? 40 : currentAttack.heavy ? 26 : 12, e.InkColor);
                if (killing || currentAttack.heavy || isCounter) InkFx.Instance.Burst(e.Chest, killing ? 5f : 3.2f);
            }
        }

        void Impact()
        {
            var time = TimeController.Instance;
            if (time != null) time.HitStop(InRampage ? 7 : currentAttack.hitStopFrames);
            var cam = CameraDirector.Instance;
            if (cam != null)
            {
                cam.Shake(currentAttack.shake, 0.18f);
                if (currentAttack.heavy || InRampage) cam.ZoomPunch(4f, 0.08f);
            }
            if (InkFx.Instance != null)
            {
                if (!currentAttack.ranged) HitSound();
                if (currentAttack.heavy || isCounter) InkFx.Instance.ImpactFlash(isCounter ? 0.06f : 0.035f);
            }
        }

        void StartExecution()
        {
            var e = pendingExecution;
            pendingExecution = null;
            if (e == null || !e.Alive) { EnterIdle(); return; }
            FaceTowards(e.transform.position.x);
            currentAttack = AttackData.GunShootDown;
            target = e;
            attackDir = Facing;
            hitDone = false;
            isCounter = false;
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.6f);
            BeginSwing();
        }

        // ------------------------------------------------------------------ defence

        void Duck()
        {
            state = State.Duck;
            stateTime = 0f;
            body.Play("duck", 0.04f);
        }

        void UpdateDuck()
        {
            if (stateTime > 0.55f) EnterIdle();
        }

        void Dodge(int dir)
        {
            if (Contact) { ContactDodge(dir); return; }
            Tell("dodge");
            pendingExecution = null;
            throwPending = false;
            Enemy over = null, slideTarget = null;
            foreach (var e in Enemy.All)
            {
                float dx = (e.transform.position.x - transform.position.x) * dir;
                bool level = Mathf.Abs(e.transform.position.y - transform.position.y) < 0.6f;
                if (e.Telegraphing && dx > 0f && dx < 2.6f && level) { over = e; break; }
                if (e.Targetable && dx > 0.6f && dx < 4.5f && level && (slideTarget == null || dx < (slideTarget.transform.position.x - transform.position.x) * dir)) slideTarget = e;
            }
            SetTrails(false);
            Sound.Play(Sfx.Whoosh, 1f, transform.position, 0.9f);
            Sound.Play(Sfx.Cloth, 1f, transform.position);

            if (over != null)
            {
                // into a red flash: vault over them, land facing their back
                float land = over.transform.position.x + dir * 1.3f;
                state = State.Dodge;
                stateTime = 0f;
                SetFacing(dir);
                velocity.x = (land - transform.position.x) / 0.55f;
                invulnerableUntil = Time.time + 0.75f;
                body.Play("front_flip", 0.03f);
                over.Interrupt();
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.9f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("VAULT!");
                vaultFacing = -dir;
                return;
            }

            // towards a wall right there: kick off it into a flip
            float wallX = dir > 0 ? arenaMaxX : arenaMinX;
            if (Mathf.Abs(wallX - transform.position.x) < 1.6f && Grounded)
            {
                state = State.Dodge;
                stateTime = 0f;
                SetFacing(dir);                      // face the wall: the backflip carries us away from it
                velocity.x = -dir * 10f;
                velocity.y = 7f;
                Grounded = false;
                invulnerableUntil = Time.time + 0.8f;
                body.Play("dodge", 0.03f);
                vaultFacing = -dir;
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("WALL FLIP!");
                return;
            }

            // towards an enemy: slide in low under its guard
            if (slideTarget != null && Grounded)
            {
                SetFacing(dir);
                StartAttack(AttackData.SlideKick, slideTarget, dir, false);
                invulnerableUntil = Time.time + 0.35f;
                return;
            }

            // otherwise a backflip away
            state = State.Dodge;
            stateTime = 0f;
            invulnerableUntil = Time.time + 0.5f;
            SetFacing(-dir);
            velocity.x = dir * 7f;
            body.Play("dodge", 0.04f);
            vaultFacing = 0;
            foreach (var e in Enemy.All)
            {
                if (e.Telegraphing && Mathf.Abs(e.transform.position.x - transform.position.x) < 2.6f)
                {
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.8f);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("PERFECT DODGE");
                    break;
                }
            }
        }

        void UpdateDodge()
        {
            if (vaultFacing != 0)
            {
                if (stateTime > 0.55f && Grounded) { velocity.x = 0f; SetFacing(vaultFacing); vaultFacing = 0; EnterIdle(); }
                return;
            }
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 9f * Time.deltaTime);
            if (stateTime > 0.72f) EnterIdle();
        }

        // ------------------------------------------------------------------ grabbed

        /// <summary>An enemy's grab connected. Returns false if the hero cannot be held now.</summary>
        public bool GetGrabbed(Enemy by)
        {
            if (Invulnerable || state == State.Grabbed || state == State.Swing || !Grounded) return false;
            if (state == State.Clinch) EndClinch();
            grabber = by;
            grabTaps = 0;
            grabTick = 0f;
            state = State.Grabbed;
            stateTime = 0f;
            pendingExecution = null;
            SetTrails(false);
            FaceTowards(by.transform.position.x);
            var p = transform.position;
            p.x = by.transform.position.x - Facing * 0.6f;
            transform.position = p;
            body.Play("struggle", 0.06f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "GRABBED! TAP!");
            return true;
        }

        void StruggleTap()
        {
            grabTaps++;
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.05f, 0.06f);
            if (grabTaps >= 4) BreakFree();
        }

        void UpdateGrabbed(float dt)
        {
            HandleCommand(GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None);
            if (state != State.Grabbed) return;
            if (grabber == null || !grabber.Alive || !grabber.HoldingHero) { grabber = null; EnterIdle(); return; }
            grabTick += dt;
            if (grabTick > 0.7f)
            {
                grabTick = 0f;
                Health = Mathf.Max(1f, Health - 5f);
                Tint(new Color(0.8f, 0.1f, 0.1f), 0.08f);
                Sound.Play(Sfx.PunchLight, 0.8f, transform.position);
                Sound.Play(Sfx.HeroHurt, 0.7f, transform.position);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroDamaged(5f);
            }
            if (stateTime > 3f) { grabber = null; EnterIdle(); }
        }

        void BreakFree()
        {
            var e = grabber;
            grabber = null;
            invulnerableUntil = Time.time + 0.6f;
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("REVERSAL!");
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.6f);
            if (e == null || !e.Alive) { EnterIdle(); return; }
            FaceTowards(e.transform.position.x);
            var keep = Style;
            Style = FightStyle.Fists;              // the reversal is always a body throw
            StartAttack(AttackData.GrabThrow, e, Facing, false);
            Style = keep;
        }

        public override void TakeHit(HitInfo hit)
        {
            if (Invulnerable) return;
            if (state == State.Clinch) EndClinch();          // hit from behind: he lets go
            Health = Mathf.Max(0f, Health - hit.damage);
            ComboCount = 0;
            comboStep = 0;
            pendingExecution = null;
            throwPending = knifeThrowPending = catchPending = false;
            grabber = null;
            velocity.x = hit.direction * hit.knockback;
            Tint(new Color(0.8f, 0.1f, 0.1f), 0.12f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(Chest, new Vector3(hit.direction, 0.3f, 0f), 14, true);
            }
            var weaponHit = hit.attacker is Enemy ae ? ae.Type : EnemyType.Brawler;
            Sound.Play(weaponHit == EnemyType.Swordsman ? Sfx.SwordHit : weaponHit == EnemyType.Piper ? Sfx.PipeHit
                       : weaponHit == EnemyType.Gunner ? Sfx.PunchLight : hit.heavy ? Sfx.PunchHeavy : Sfx.PunchLight, 1f, Chest);
            Sound.Play(Alive ? Sfx.HeroHurt : Sfx.HeroDeath, 1f, Chest);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(hit.heavy ? 5 : 3);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.15f, 0.2f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroDamaged(hit.damage);
            SetTrails(false);

            if (!Alive)
            {
                state = State.Dead;
                body.Play("knockdown", 0.05f);
                velocity.y = 4f;
                Grounded = false;
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 1.2f);
                return;
            }
            bool fromFront = hit.direction == -Facing;
            state = State.Hurt;
            stateTime = 0f;
            invulnerableUntil = Time.time + 0.45f;
            if (Contact && fromFront && !string.IsNullOrEmpty(hit.react) && body.Has(hit.react)) body.Play(hit.react, 0.04f);   // the blow's own reaction
            else body.Play(fromFront ? "hit_front" : "hit_back", 0.04f);
        }

        void SetTrails(bool on)
        {
            if (swordFx != null) swordFx.Emitting = on && Style == FightStyle.Sword;
            bool blade = on && (Style == FightStyle.Sword || Style == FightStyle.Pipe);
            if (limbTrails != null)
                foreach (var t in limbTrails)
                {
                    if (t == null) continue;
                    bool want = on && !blade && Style != FightStyle.Gun;
                    if (want && !t.emitting) t.Clear();
                    t.emitting = want;
                }
            foreach (var tr in new[] { swordTrail, pipeTrail })
            {
                if (tr == null) continue;
                bool want = blade && ((tr == swordTrail && Style == FightStyle.Sword) || (tr == pipeTrail && Style == FightStyle.Pipe));
                if (want && !tr.emitting) tr.Clear();
                tr.emitting = want;
            }
        }
    }
}
