using UnityEngine;

namespace ProjectInk
{
    public enum FightStyle { Fists, Sword, Gun }

    /// <summary>The player's stickman. The player picks direction and timing; this
    /// script picks the target, dashes to it and chooses the move. Three fighting styles
    /// share the same controls (swipe down to switch):
    /// Fists — mocap martial arts combo, back kick behind, flying kick up the stairs.
    /// Sword — big cleaving cuts, back slash behind, leaping slam.
    /// Gun   — shots at range (behind without turning), pistol-whip and throw up close,
    ///         then a finishing shot on the ground; 12-round magazine with reloads.</summary>
    public class HeroController : Fighter
    {
        enum State { Idle, Dash, Attack, Dodge, Hurt, Dead }

        public float targetRange = 9f;
        public float gunRange = 14f;
        public float closeQuarters = 2.0f;
        public float dashSpeed = 17f;
        public float comboWindow = 0.9f;

        [Header("Weapons (set by FighterFactory)")]
        public GameObject sword;
        public GameObject pistol;
        public TrailRenderer swordTrail;
        public Transform muzzle;

        State state = State.Idle;
        AttackData currentAttack;
        Enemy target, pendingExecution;
        bool hitDone, isCounter, reloading;
        int attackDir;
        float stateTime, invulnerableUntil, lastAttackEnd;
        int comboStep, closeStep;
        TrailRenderer[] limbTrails;

        public FightStyle Style { get; private set; } = FightStyle.Fists;
        public int Ammo { get; private set; } = AttackData.MagazineSize;
        public bool Reloading => reloading;
        public int ComboCount { get; private set; }
        public float ComboTimer { get; private set; }
        public bool IsDead => state == State.Dead;
        public bool Invulnerable => Time.time < invulnerableUntil || state == State.Dead;

        string IdleClip => Style == FightStyle.Sword ? "sword_idle" : Style == FightStyle.Gun ? "gun_idle" : "idle";

        protected override void Awake()
        {
            base.Awake();
            var all = GetComponentsInChildren<TrailRenderer>(true);
            limbTrails = System.Array.FindAll(all, t => t != swordTrail);
            SetTrails(false);
            ApplyStyleVisuals();
        }

        void Start() { body.Play(IdleClip, 0f); }

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

            if (state != State.Dash && state != State.Dodge && Grounded) velocity.x = Mathf.MoveTowards(velocity.x, 0f, 30f * dt);
            Integrate(dt);
            UpdateTint();
        }

        // ------------------------------------------------------------------ style

        public void SetStyle(FightStyle s)
        {
            if (Style == s) return;
            Style = s;
            comboStep = 0;
            closeStep = 0;
            ApplyStyleVisuals();
            if (state == State.Idle) body.Play(IdleClip, 0.15f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, s == FightStyle.Sword ? "SWORD" : s == FightStyle.Gun ? "GUN-FU" : "FISTS");
        }

        void ApplyStyleVisuals()
        {
            if (sword != null) sword.SetActive(Style == FightStyle.Sword);
            if (pistol != null) pistol.SetActive(Style == FightStyle.Gun);
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
            return hitDone && pendingExecution == null && !reloading && e != null && body.Time >= e.hitTime + 0.06f;
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
                case Command.CycleStyle: SetStyle((FightStyle)(((int)Style + 1) % 3)); break;
                case Command.StyleFists: SetStyle(FightStyle.Fists); break;
                case Command.StyleSword: SetStyle(FightStyle.Sword); break;
                case Command.StyleGun: SetStyle(FightStyle.Gun); break;
            }
        }

        Enemy PickTarget(int side, float range)
        {
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (!e.Targetable) continue;
                float dx = e.transform.position.x - transform.position.x;
                if (Mathf.Sign(dx) != side && Mathf.Abs(dx) > 0.2f) continue;
                float d = Mathf.Abs(dx) + Mathf.Abs(e.transform.position.y - transform.position.y) * 0.5f;
                if (d > range) continue;
                float score = d - (e.Telegraphing ? 2.5f : 0f) - (e.HasToken ? 0.8f : 0f);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------------ choosing the move

        void Strike(int side)
        {
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
            bool counter = t.Telegraphing && Mathf.Abs(dx) < 4f;
            bool behindClose = Facing != side && Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 0.5f;

            AttackData move;
            if (Style == FightStyle.Sword)
            {
                if (counter) move = AttackData.SwordC;
                else if (behindClose) move = AttackData.SwordBack;               // cut behind without turning
                else if (dy > 0.8f) move = AttackData.SwordLeap;                 // leap at enemies up the stairs
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
                else if (dy > 0.8f) move = AttackData.FlyingKick;                // enemy above (stairs)
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
            if (reloading) return;
            var t = PickTarget(side, gunRange);
            bool behind = Facing != side;
            bool close = t != null && Mathf.Abs(t.transform.position.x - transform.position.x) <= closeQuarters;
            if (Ammo <= 0 && !close) { StartReload(); return; }
            if (t == null)
            {
                // nothing there: fire anyway, it reads as suppressing fire
                if (!behind) SetFacing(side);
                StartAttack(behind ? AttackData.GunShootBack : AttackData.GunShoot, null, side, false);
                return;
            }
            float dist = Mathf.Abs(t.transform.position.x - transform.position.x);
            bool counter = t.Telegraphing && dist < 4f;

            if (dist <= closeQuarters && !counter && Mathf.Abs(t.transform.position.y - transform.position.y) < 0.6f)
            {
                // close quarters: pistol-whip, then grapple and throw (followed by a finishing shot)
                SetFacing(side);
                var move = closeStep++ % 2 == 0 ? AttackData.GunWhip : AttackData.GunThrow;
                if (t.Health <= AttackData.GunWhip.damage) move = AttackData.GunThrow;
                StartAttack(move, t, side, false);
                return;
            }
            // at range: shoot, behind without turning if the target is behind
            if (!behind) SetFacing(side);
            StartAttack(behind ? AttackData.GunShootBack : AttackData.GunShoot, t, side, counter);
        }

        void JumpAttack()
        {
            var t = PickTarget(Facing, 7f) ?? PickTarget(-Facing, 7f);
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
            if (t != null && !move.ranged && !move.behind)
            {
                float gap = Mathf.Abs(t.transform.position.x - transform.position.x);
                if (gap > move.reach + 0.15f)
                {
                    state = State.Dash;
                    stateTime = 0f;
                    body.Play("run", 0.05f, false, 1.6f);
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
                // the animation already jumps; physics only lifts the hero to targets up the stairs
                velocity.x = 0f;
                if (target != null && target.transform.position.y - transform.position.y > 0.5f)
                {
                    velocity.y = 6.5f;
                    Grounded = false;
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
                p.x = Mathf.MoveTowards(p.x, goal, dashSpeed * 0.6f * Time.deltaTime);
                transform.position = p;
            }

            if (!hitDone && e.hitTime >= 0f && body.Time >= e.hitTime)
            {
                if (currentAttack.ranged) ResolveShot();
                else ResolveHit();
            }

            if (CanCancel())
            {
                var next = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
                if (next != Command.None) { HandleCommand(next); return; }
            }
            if (body.Finished && Grounded)
            {
                if (reloading)
                {
                    reloading = false;
                    Ammo = AttackData.MagazineSize;
                }
                else if (pendingExecution != null)
                {
                    StartExecution();
                    return;
                }
                else if (Style == FightStyle.Gun && Ammo <= 0)
                {
                    StartReload();
                    return;
                }
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
            // primary target, then anyone else in the strike's path (heavy moves and swords)
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
                bool killing = e.Health <= hit.damage;
                e.TakeHit(hit);
                landed = true;
                OnLanded(e, killing);
                if (currentAttack.clip == "gun_throw" && primary && !killing) pendingExecution = e;
            }
            if (landed) Impact();
        }

        void ResolveShot()
        {
            hitDone = true;
            Vector3 from = muzzle != null ? muzzle.position : Chest + Vector3.right * attackDir * 0.6f;
            bool execution = currentAttack.clip == "gun_shoot_down";
            if (!execution) Ammo = Mathf.Max(0, Ammo - 1);

            Vector3 to;
            if (target != null && (execution ? target.Alive : target.Targetable))
            {
                to = execution ? target.transform.position + Vector3.up * 0.25f : target.Chest;
                bool killing = execution || target.Health <= currentAttack.damage;
                if (execution) target.Execute(this);
                else
                {
                    target.TakeHit(new HitInfo
                    {
                        damage = currentAttack.damage, direction = attackDir, knockback = currentAttack.knockback,
                        knockdown = killing, heavy = false, attacker = this
                    });
                }
                OnLanded(target, killing);
                Impact();
            }
            else to = from + new Vector3(attackDir, 0f, 0f) * 16f;

            if (InkFx.Instance != null) InkFx.Instance.Shot(from, to);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.05f, 0.08f);
        }

        void OnLanded(Enemy e, bool killing)
        {
            ComboCount++;
            ComboTimer = 2f;
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroHit(e, ComboCount, currentAttack.heavy || currentAttack.cleave, killing);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(e.Chest, new Vector3(attackDir, 0.3f, 0f), killing ? 40 : currentAttack.heavy ? 26 : 12);
                if (killing || currentAttack.heavy || isCounter) InkFx.Instance.Burst(e.Chest, killing ? 5f : 3.2f);
            }
        }

        void Impact()
        {
            var time = TimeController.Instance;
            if (time != null) time.HitStop(currentAttack.hitStopFrames);
            var cam = CameraDirector.Instance;
            if (cam != null)
            {
                cam.Shake(currentAttack.shake, 0.18f);
                if (currentAttack.heavy) cam.ZoomPunch(4f, 0.08f);
            }
            if (InkFx.Instance != null)
            {
                if (!currentAttack.ranged) InkFx.Instance.PlayHit(currentAttack.heavy);
                if (currentAttack.heavy || isCounter) InkFx.Instance.ImpactFlash(isCounter ? 0.07f : 0.035f);
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

        void StartReload()
        {
            reloading = true;
            state = State.Attack;
            stateTime = 0f;
            hitDone = true;
            currentAttack = new AttackData { clip = "gun_reload", ranged = true };
            body.Play("gun_reload", 0.08f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, "RELOAD");
        }

        // ------------------------------------------------------------------ defence

        void Dodge(int dir)
        {
            state = State.Dodge;
            stateTime = 0f;
            invulnerableUntil = Time.time + 0.5f;
            pendingExecution = null;
            SetFacing(-dir);          // the dodge is a backflip: face away so it carries the hero towards `dir`
            velocity.x = dir * 7f;
            body.Play("dodge", 0.04f);
            SetTrails(false);
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
            // a perfect dodge (an enemy was about to hit) earns bullet time
            foreach (var e in Enemy.All)
            {
                if (e.Telegraphing && Mathf.Abs(e.transform.position.x - transform.position.x) < 2.2f)
                {
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.8f);
                    if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(150, "PERFECT DODGE");
                    break;
                }
            }
        }

        void UpdateDodge()
        {
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
            if (reloading) { reloading = false; Ammo = AttackData.MagazineSize; }
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
