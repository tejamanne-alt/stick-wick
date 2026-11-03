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
    public class HeroController : Fighter
    {
        enum State { Idle, Dash, Attack, Dodge, Hurt, Dead, Swing, Grabbed, Duck, ToLedge }

        public float targetRange = 9f;
        public float gunRange = 14f;
        public float closeQuarters = 2.0f;
        public float dashSpeed = 20f;
        public float comboWindow = 0.9f;
        public float rampageSeconds = 5f;

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
        public bool Invulnerable => Time.time < invulnerableUntil || state == State.Dead || InRampage || OnLedge || state == State.Swing;
        public bool IsGrabbedBy(Enemy e) => state == State.Grabbed && grabber == e;

        string IdleClip => Style == FightStyle.Sword || Style == FightStyle.Pipe ? "sword_idle" : Style == FightStyle.Gun ? "gun_idle" : "idle";

        protected override void Awake()
        {
            base.Awake();
            var all = GetComponentsInChildren<TrailRenderer>(true);
            limbTrails = System.Array.FindAll(all, t => t != swordTrail && t != pipeTrail);
            SetTrails(false);
            ApplyStyleVisuals();
        }

        void Start()
        {
            body.Play(IdleClip, 0f);
            Invoke(nameof(CheckBody), 0.3f);
        }

        void CheckBody() { body.CheckVisible(); }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            ComboTimer -= dt;
            if (ComboTimer <= 0f) ComboCount = 0;
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
                case State.Dead: break;
            }
            if (state != State.Dead) TryPickup();

            if (state != State.Dash && state != State.Dodge && state != State.ToLedge && !(state == State.Attack && currentAttack.clip == "slide_kick") && Grounded)
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, 30f * dt);
            if (state != State.Swing && state != State.Grabbed) Integrate(dt);
            UpdateTint();
        }

        // ------------------------------------------------------------------ weapons

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
                Destroy(p.gameObject);
                return;
            }
        }

        bool WeaponSpent => (Style == FightStyle.Sword && SwordUses <= 0) || (Style == FightStyle.Gun && Ammo <= 0) ||
                            (Style == FightStyle.Pipe && PipeUses <= 0);

        void ThrowWeapon()
        {
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
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
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
            state = State.Idle;
            stateTime = 0f;
            body.Play(IdleClip, 0.12f);
            SetTrails(false);
            ApplyStyleVisuals();
        }

        void UpdateIdle()
        {
            HandleCommand(GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None);
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
            switch (cmd)
            {
                case Command.StrikeLeft: Strike(-1); break;
                case Command.StrikeRight: Strike(1); break;
                case Command.JumpKick: JumpAttack(); break;
                case Command.DodgeLeft: Dodge(-1); break;
                case Command.DodgeRight: Dodge(1); break;
                case Command.ThrowWeapon: ThrowWeapon(); break;
                case Command.Rampage: StartRampage(); break;
                case Command.CycleStyle: SetStyle((FightStyle)(((int)Style + 1) % 5)); break;
                case Command.StyleFists: SetStyle(FightStyle.Fists); break;
                case Command.StyleSword: SetStyle(FightStyle.Sword); break;
                case Command.StyleGun: SetStyle(FightStyle.Gun); break;
            }
        }

        Enemy PickTarget(int side, float range, bool anyLevel = true)
        {
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

        void StartRampage()
        {
            var wd = WaveDirector.Instance;
            if (wd == null || !wd.FlowReady || InRampage) return;
            wd.SpendFlow();
            rampageUntil = Time.unscaledTime + rampageSeconds;
            if (TimeController.Instance != null) TimeController.Instance.Rampage(0.4f, rampageSeconds);
            if (InkFx.Instance != null) { InkFx.Instance.ImpactFlash(0.08f); InkFx.Instance.Burst(Chest, 6f); }
            if (CameraDirector.Instance != null) CameraDirector.Instance.ZoomPunch(8f, 0.25f);
            wd.AddStyle(300, "RAMPAGE!");
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

        void Strike(int side)
        {
            if (InRampage) { RampageStrike(side); return; }
            if (state == State.Idle && Time.time - lastAttackEnd > comboWindow) { comboStep = 0; closeStep = 0; }

            // a knife flying in from that side: catch it and send it back
            var incoming = IncomingKnife(side);
            if (incoming != null) { CatchKnife(incoming, side); return; }

            if (Style == FightStyle.Gun) { GunStrike(side); return; }

            var t = PickTarget(side, Style == FightStyle.Knife ? 16f : targetRange);

            // an enemy on the floor right there, nobody else in reach: stomp it or swing it
            var downed = PickDowned(side);
            bool someoneClose = t != null && Mathf.Abs(t.transform.position.x - transform.position.x) < 1.3f &&
                                Mathf.Abs(t.transform.position.y - transform.position.y) < 1.0f;
            if (downed != null && !someoneClose && (Style == FightStyle.Fists || Style == FightStyle.Knife))
            {
                if (comboStep++ % 2 == 0 && Enemy.All.Count > 1) BeginGiantSwing(downed, side);
                else BeginStomp(downed, side);
                return;
            }

            if (Style == FightStyle.Knife && t != null && Mathf.Abs(t.transform.position.x - transform.position.x) > 1.8f)
            {
                KnifeThrow(t);
                return;
            }

            var combo = Style == FightStyle.Sword ? AttackData.SwordCombo : Style == FightStyle.Pipe ? AttackData.PipeCombo : AttackData.Combo;
            if (t == null)
            {
                SetFacing(side);
                if (Style == FightStyle.Knife) { StartAttack(AttackData.KnifeStab, null, side, false); return; }
                StartAttack(combo[comboStep++ % combo.Length], null, side, false);
                return;
            }

            float dx = t.transform.position.x - transform.position.x;
            float dy = t.transform.position.y - transform.position.y;
            bool counter = t.Telegraphing && Mathf.Abs(dx) < 4f && Mathf.Abs(dy) < 1.4f;
            bool behindClose = Facing != side && Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 0.5f;

            // another floor: climb the ledge if it is near, otherwise leap or dive
            if (dy > 1.4f && Level == 0 && transform.position.x > -3f && transform.position.x < 5f && Grounded)
            {
                GoToLedge();
                return;
            }

            AttackData move;
            if (dy > 1.4f || dy < -1.4f)
                move = Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick;
            else if (Style == FightStyle.Sword || Style == FightStyle.Pipe)
            {
                var finisher = Style == FightStyle.Sword ? AttackData.SwordC : AttackData.PipeC;
                if (counter) move = finisher;
                else if (behindClose && Style == FightStyle.Sword) move = AttackData.SwordBack;
                else if (dy > 0.8f && Style == FightStyle.Sword) move = AttackData.SwordLeap;
                else
                {
                    move = combo[comboStep % combo.Length];
                    if (t.Health <= move.damage && comboStep % 3 == 2) move = finisher;
                    comboStep++;
                }
            }
            else if (Style == FightStyle.Knife)
                move = AttackData.KnifeStab;
            else
            {
                if (counter) move = AttackData.Counter;
                else if (behindClose) move = AttackData.BackKick;
                else if (dy > 0.8f) move = AttackData.FlyingKick;
                else if (comboStep % 4 == 3 && Mathf.Abs(dx) < 2.2f && t.Type != EnemyType.Brute) { move = AttackData.GrabThrow; comboStep++; }
                else
                {
                    move = combo[comboStep % combo.Length];
                    if (t.Health <= move.damage) move = comboStep % 2 == 0 ? AttackData.SpinKick : AttackData.Roundhouse; // cinematic finisher
                    comboStep++;
                }
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
            // near the ledge with nothing to hit: climb up
            if (Level == 0 && Grounded && Mathf.Abs(transform.position.x - LedgeFootX) < 3f &&
                (t == null || t.transform.position.y - transform.position.y > 1.4f)) { GoToLedge(); return; }
            if (t != null) SetFacing(t.transform.position.x > transform.position.x ? 1 : -1);
            StartAttack(Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick, t, Facing, false);
        }

        // ------------------------------------------------------------------ ledge

        void GoToLedge()
        {
            state = State.ToLedge;
            stateTime = 0f;
            body.Play("run", 0.06f, false, 1.6f);
        }

        void UpdateToLedge(float dt)
        {
            var p = transform.position;
            float fx = LedgeFootX;
            FaceTowards(fx);
            p.x = Mathf.MoveTowards(p.x, fx, 11f * dt);
            transform.position = p;
            if (Mathf.Abs(p.x - fx) < 0.05f || stateTime > 1.2f)
            {
                BeginLedgeClimb(0.1f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(40, "CLIMB");
            }
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
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
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
                    if (InkFx.Instance != null) { InkFx.Instance.Splash(other.Chest, Vector3.up, 24, other.InkColor); InkFx.Instance.PlayHit(true); }
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
            bool leap = move.clip == "flying_kick" || move.clip == "sword_leap";
            if (t != null && !move.ranged && !move.behind && !leap)
            {
                float gap = Mathf.Abs(t.transform.position.x - transform.position.x);
                if (gap > Reach(move) + 0.15f)
                {
                    state = State.Dash;
                    stateTime = 0f;
                    body.Play("run", 0.05f, false, 1.7f);
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
            p.x = Mathf.MoveTowards(p.x, goal, dashSpeed * dt);
            transform.position = p;
            if (Mathf.Abs(p.x - goal) < 0.05f || stateTime > 0.6f) BeginSwing();
        }

        void BeginSwing()
        {
            state = State.Attack;
            stateTime = 0f;
            body.Play(currentAttack.clip, 0.05f);
            SetTrails(!currentAttack.ranged);
            if (InkFx.Instance != null && !currentAttack.ranged) InkFx.Instance.PlayWhoosh();
            if (currentAttack.clip == "slide_kick") velocity.x = attackDir * 11f;
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
                        dropThroughUntil = Time.time + 0.5f;                     // dive down to the floor below
                        velocity.y = 3f;
                        Grounded = false;
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

            if (!hitDone && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                if (throwPending || knifeThrowPending || catchPending) ReleaseThrow();
                else if (currentAttack.clip == "stomp") ResolveStomp();
                else if (currentAttack.ranged) ResolveShot();
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
                InkFx.Instance.PlayHit(true);
                InkFx.Instance.ImpactFlash(0.04f);
            }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.2f, 0.2f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(7);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroKill(e, "STOMP!", 150, false);
        }

        void ResolveHit()
        {
            hitDone = true;
            var hit = new HitInfo
            {
                damage = currentAttack.damage,
                direction = attackDir,
                knockback = currentAttack.knockback,
                knockdown = currentAttack.knockdown,
                heavy = currentAttack.heavy,
                attacker = this
            };

            bool landed = false;
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
                    if (e.Alive) e.Launch(new Vector3(attackDir * 12f, 5f, 0f), this);
                    landed = true;
                    OnStrikeLanded(e, !e.Alive);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(60, "THROW!");
                    continue;
                }
                bool killing = InRampage || e.Health <= hit.damage;
                if (InRampage) e.Kill(hit); else e.TakeHit(hit);
                landed = true;
                OnStrikeLanded(e, killing);
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

        void OnStrikeLanded(Enemy e, bool killing)
        {
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
                if (!currentAttack.ranged) InkFx.Instance.PlayHit(currentAttack.heavy || InRampage);
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
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();

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
                if (InkFx.Instance != null) InkFx.Instance.PlayHit(false);
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
                InkFx.Instance.PlayHit(true);
            }
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
            body.Play(fromFront ? "hit_front" : "hit_back", 0.04f);
        }

        void SetTrails(bool on)
        {
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
