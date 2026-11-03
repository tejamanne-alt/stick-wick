using UnityEngine;

namespace ProjectInk
{
    public enum FightStyle { Fists, Sword, Gun }

    /// <summary>The player's stickman. The player picks direction and timing; this script picks
    /// the target, dashes or leaps to it and chooses the move.
    /// Fists  — motion-captured martial arts; back kick behind; flying kick up a floor.
    /// Sword  — taken from a fallen swordsman: cleaving cuts, back slash, leaping slam. Wears out.
    /// Gun    — taken from a fallen gunner: shots at range (behind without turning), pistol-whip
    ///          and throw up close, then a finishing shot. Runs dry.
    /// A worn-out or empty weapon is thrown (also on swipe down) as a spinning kill.
    /// Dodging an attacker's red flash vaults over them in slow-mo. A full Flow meter unlocks
    /// Rampage: slowed time where every tap teleports into a one-hit cinematic finisher.</summary>
    public class HeroController : Fighter
    {
        enum State { Idle, Dash, Attack, Dodge, Hurt, Dead }

        public float targetRange = 9f;
        public float gunRange = 14f;
        public float closeQuarters = 2.0f;
        public float dashSpeed = 20f;
        public float comboWindow = 0.9f;
        public float rampageSeconds = 5f;

        [Header("Weapons (set by FighterFactory)")]
        public GameObject sword;
        public GameObject pistol;
        public TrailRenderer swordTrail;
        public Transform muzzle;
        public Material weaponMaterial;
        public Material trailMaterial;

        State state = State.Idle;
        AttackData currentAttack;
        Enemy target, pendingExecution;
        bool hitDone, isCounter, throwPending;
        int attackDir;
        float stateTime, invulnerableUntil, lastAttackEnd, rampageUntil;
        int comboStep, closeStep;
        TrailRenderer[] limbTrails;

        public FightStyle Style { get; private set; } = FightStyle.Fists;
        public int Ammo { get; private set; }
        public int SwordUses { get; private set; }
        public bool Reloading => false;
        public bool InRampage => Time.unscaledTime < rampageUntil;
        public float RampageLeft => Mathf.Max(0f, rampageUntil - Time.unscaledTime);
        public int ComboCount { get; private set; }
        public float ComboTimer { get; private set; }
        public bool IsDead => state == State.Dead;
        public bool Invulnerable => Time.time < invulnerableUntil || state == State.Dead || InRampage;

        string IdleClip => Style == FightStyle.Sword ? "sword_idle" : Style == FightStyle.Gun ? "gun_idle" : "idle";

        protected override void Awake()
        {
            base.Awake();
            var all = GetComponentsInChildren<TrailRenderer>(true);
            limbTrails = System.Array.FindAll(all, t => t != swordTrail);
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

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Dash: UpdateDash(dt); break;
                case State.Attack: UpdateAttack(); break;
                case State.Dodge: UpdateDodge(); break;
                case State.Hurt: if (stateTime > 0.35f) EnterIdle(); break;
                case State.Dead: break;
            }
            if (state != State.Dead) TryPickup();

            if (state != State.Dash && state != State.Dodge && Grounded) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 30f * dt);
            Integrate(dt);
            UpdateTint();
        }

        // ------------------------------------------------------------------ weapons

        void Equip(FightStyle s, int uses)
        {
            Style = s;
            comboStep = 0;
            closeStep = 0;
            if (s == FightStyle.Sword) SwordUses = uses;
            if (s == FightStyle.Gun) Ammo = uses;
            ApplyStyleVisuals();
            if (state == State.Idle) body.Play(IdleClip, 0.15f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, s == FightStyle.Sword ? "SWORD!" : s == FightStyle.Gun ? "GUN!" : "FISTS");
        }

        public void SetStyle(FightStyle s) { Equip(s, s == FightStyle.Sword ? 10 : s == FightStyle.Gun ? 8 : 0); } // debug keys

        void ApplyStyleVisuals()
        {
            if (sword != null) sword.SetActive(Style == FightStyle.Sword);
            if (pistol != null) pistol.SetActive(Style == FightStyle.Gun);
        }

        void TryPickup()
        {
            if (Style != FightStyle.Fists) return;
            foreach (var p in WeaponPickup.All)
            {
                if (Mathf.Abs(p.transform.position.x - transform.position.x) < 0.9f &&
                    Mathf.Abs(p.transform.position.y - 0.35f - transform.position.y) < 0.8f)
                {
                    Equip(p.kind, p.uses);
                    if (InkFx.Instance != null) InkFx.Instance.Burst(p.transform.position, 1.6f);
                    Destroy(p.gameObject);
                    return;
                }
            }
        }

        bool WeaponSpent => (Style == FightStyle.Sword && SwordUses <= 0) || (Style == FightStyle.Gun && Ammo <= 0);

        void ThrowWeapon()
        {
            if (Style == FightStyle.Fists) return;
            var t = PickTarget(Facing, 16f) ?? PickTarget(-Facing, 16f);
            if (t != null) FaceTowards(t.transform.position.x);
            currentAttack = new AttackData { clip = "throw_weapon", ranged = true };
            target = t;
            attackDir = Facing;
            hitDone = false;
            throwPending = true;
            state = State.Attack;
            stateTime = 0f;
            body.Play("throw_weapon", 0.05f);
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
        }

        void ReleaseThrow()
        {
            hitDone = true;
            throwPending = false;
            var from = (Style == FightStyle.Sword && sword != null) ? sword.transform.position : muzzle != null ? muzzle.position : Chest;
            ThrownWeapon.Throw(this, Style, from + Vector3.up * 0.2f, target, Facing, weaponMaterial, trailMaterial);
            Style = FightStyle.Fists;
            ApplyStyleVisuals();
        }

        // ------------------------------------------------------------------ input

        void EnterIdle()
        {
            state = State.Idle;
            stateTime = 0f;
            body.Play(IdleClip, 0.12f);
            SetTrails(false);
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
            return hitDone && pendingExecution == null && !throwPending && e != null && body.Time >= e.hitTime + window;
        }

        void HandleCommand(Command cmd)
        {
            switch (cmd)
            {
                case Command.StrikeLeft: Strike(-1); break;
                case Command.StrikeRight: Strike(1); break;
                case Command.JumpKick: JumpAttack(); break;
                case Command.DodgeLeft: Dodge(-1); break;
                case Command.DodgeRight: Dodge(1); break;
                case Command.ThrowWeapon: ThrowWeapon(); break;
                case Command.Rampage: StartRampage(); break;
                case Command.CycleStyle: SetStyle((FightStyle)(((int)Style + 1) % 3)); break;
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
            if (Style == FightStyle.Gun) { GunStrike(side); return; }

            var t = PickTarget(side, targetRange);
            var combo = Style == FightStyle.Sword ? AttackData.SwordCombo : AttackData.Combo;
            if (t == null)
            {
                SetFacing(side);
                StartAttack(combo[comboStep++ % combo.Length], null, side, false);
                return;
            }

            float dx = t.transform.position.x - transform.position.x;
            float dy = t.transform.position.y - transform.position.y;
            bool counter = t.Telegraphing && Mathf.Abs(dx) < 4f && Mathf.Abs(dy) < 1.4f;
            bool behindClose = Facing != side && Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 0.5f;

            AttackData move;
            if (dy > 1.4f || dy < -1.4f)
                move = Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick;   // another floor: leap or dive
            else if (Style == FightStyle.Sword)
            {
                if (counter) move = AttackData.SwordC;
                else if (behindClose) move = AttackData.SwordBack;               // cut behind without turning
                else if (dy > 0.8f) move = AttackData.SwordLeap;
                else
                {
                    move = combo[comboStep % combo.Length];
                    if (t.Health <= move.damage && comboStep % 3 == 2) move = AttackData.SwordC; // finishing thrust
                    comboStep++;
                }
            }
            else
            {
                if (counter) move = AttackData.Counter;
                else if (behindClose) move = AttackData.BackKick;                // kick behind without turning
                else if (dy > 0.8f) move = AttackData.FlyingKick;
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
            if (t != null) SetFacing(t.transform.position.x > transform.position.x ? 1 : -1);
            StartAttack(Style == FightStyle.Sword ? AttackData.SwordLeap : AttackData.FlyingKick, t, Facing, false);
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
                if (gap > move.reach + 0.15f)
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
            float goal = target.transform.position.x - attackDir * currentAttack.reach * 0.92f;
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
                // glide onto the target during the jump
                var p = transform.position;
                float goal = target.transform.position.x - attackDir * currentAttack.reach * 0.9f;
                p.x = Mathf.MoveTowards(p.x, goal, dashSpeed * 0.7f * Time.deltaTime);
                transform.position = p;
            }

            if (!hitDone && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                if (throwPending) ReleaseThrow();
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
                if (!e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * attackDir;
                float dy = Mathf.Abs(e.transform.position.y - transform.position.y);
                bool primary = e == target;
                float reach = currentAttack.reach + (primary ? 0.45f : 0.1f);
                float yTol = currentAttack.clip == "flying_kick" || currentAttack.clip == "sword_leap" ? 1.8f : 1.0f;
                if (dx < -0.2f || dx > reach || dy > yTol) continue;
                if (!primary && !currentAttack.heavy && !currentAttack.cleave && target != null) continue;
                bool killing = InRampage || e.Health <= hit.damage;
                if (InRampage) e.Kill(hit); else e.TakeHit(hit);
                landed = true;
                OnStrikeLanded(e, killing);
                if (currentAttack.clip == "gun_throw" && primary && !killing) pendingExecution = e;
            }
            if (landed)
            {
                if (Style == FightStyle.Sword && !InRampage) SwordUses--;
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

        void Dodge(int dir)
        {
            pendingExecution = null;
            throwPending = false;
            // a dodge into an attacker that is about to strike: vault over them, land behind
            Enemy over = null;
            foreach (var e in Enemy.All)
            {
                float dx = (e.transform.position.x - transform.position.x) * dir;
                if (e.Telegraphing && dx > 0f && dx < 2.6f && Mathf.Abs(e.transform.position.y - transform.position.y) < 0.6f) { over = e; break; }
            }
            state = State.Dodge;
            stateTime = 0f;
            SetTrails(false);
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
            if (over != null)
            {
                float land = over.transform.position.x + dir * 1.3f;
                SetFacing(dir);                                    // flip forwards over them...
                velocity.x = (land - transform.position.x) / 0.55f;
                invulnerableUntil = Time.time + 0.75f;
                body.Play("front_flip", 0.03f);
                over.Interrupt();
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.9f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("VAULT!");
                vaultFacing = -dir;                                // ...and land facing their back
                return;
            }
            invulnerableUntil = Time.time + 0.5f;
            SetFacing(-dir);          // a backflip: face away so it carries the hero towards `dir`
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

        int vaultFacing;

        void UpdateDodge()
        {
            if (vaultFacing != 0)
            {
                if (stateTime > 0.55f) { velocity.x = 0f; SetFacing(vaultFacing); vaultFacing = 0; EnterIdle(); }
                return;
            }
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 9f * Time.deltaTime);
            if (stateTime > 0.72f) EnterIdle();
        }

        public override void TakeHit(HitInfo hit)
        {
            if (Invulnerable) return;
            Health = Mathf.Max(0f, Health - hit.damage);
            ComboCount = 0;
            comboStep = 0;
            pendingExecution = null;
            throwPending = false;
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
            bool blade = on && Style == FightStyle.Sword;
            if (limbTrails != null)
                foreach (var t in limbTrails)
                {
                    if (t == null) continue;
                    bool want = on && !blade && Style != FightStyle.Gun;
                    if (want && !t.emitting) t.Clear();
                    t.emitting = want;
                }
            if (swordTrail != null)
            {
                if (blade && !swordTrail.emitting) swordTrail.Clear();
                swordTrail.emitting = blade;
            }
        }
    }
}
