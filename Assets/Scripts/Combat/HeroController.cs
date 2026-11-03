using UnityEngine;

namespace ProjectInk
{
    /// <summary>The player's stickman. The player picks direction and timing; this
    /// script picks the target, dashes to it and chooses the move (combo step, back kick
    /// for enemies behind, flying kick for enemies above, counter on a red flash).</summary>
    public class HeroController : Fighter
    {
        enum State { Idle, Dash, Attack, Dodge, Hurt, Dead }

        public float targetRange = 9f;
        public float dashSpeed = 17f;
        public float comboWindow = 0.9f;

        State state = State.Idle;
        AttackData currentAttack;
        Enemy target;
        bool hitDone, isCounter;
        int attackDir;
        float stateTime, invulnerableUntil, lastAttackEnd;
        int comboStep;
        TrailRenderer[] trails;

        public int ComboCount { get; private set; }
        public float ComboTimer { get; private set; }
        public bool IsDead => state == State.Dead;
        public bool Invulnerable => Time.time < invulnerableUntil || state == State.Dead;

        protected override void Awake()
        {
            base.Awake();
            trails = GetComponentsInChildren<TrailRenderer>();
            SetTrails(false);
        }

        void Start() { body.Play("idle", 0f); }

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

        void EnterIdle()
        {
            state = State.Idle;
            stateTime = 0f;
            body.Play("idle", 0.12f);
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
            return hitDone && e != null && body.Time >= e.hitTime + 0.06f;
        }

        void HandleCommand(Command cmd)
        {
            switch (cmd)
            {
                case Command.StrikeLeft: Strike(-1); break;
                case Command.StrikeRight: Strike(1); break;
                case Command.JumpKick: JumpKick(); break;
                case Command.DodgeLeft: Dodge(-1); break;
                case Command.DodgeRight: Dodge(1); break;
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

        void Strike(int side)
        {
            if (state == State.Idle && Time.time - lastAttackEnd > comboWindow) comboStep = 0;
            var t = PickTarget(side, targetRange);
            if (t == null)
            {
                SetFacing(side);
                StartAttack(AttackData.Combo[comboStep % AttackData.Combo.Length], null, side, false);
                comboStep++;
                return;
            }

            float dx = t.transform.position.x - transform.position.x;
            float dy = t.transform.position.y - transform.position.y;
            bool counter = t.Telegraphing && Mathf.Abs(dx) < 4f;

            AttackData move;
            if (counter) move = AttackData.Counter;
            else if (Facing != side && Mathf.Abs(dx) < 1.7f && Mathf.Abs(dy) < 0.5f) move = AttackData.BackKick; // context: hit behind without turning
            else if (dy > 0.8f) move = AttackData.FlyingKick;                                                    // context: enemy above (stairs)
            else
            {
                move = AttackData.Combo[comboStep % AttackData.Combo.Length];
                if (t.Health <= move.damage && !move.knockdown) move = AttackData.Roundhouse;                   // finisher
                comboStep++;
            }

            if (move.clip != "back_kick") SetFacing(side);
            StartAttack(move, t, side, counter);
        }

        void JumpKick()
        {
            var t = PickTarget(Facing, 7f) ?? PickTarget(-Facing, 7f);
            if (t != null) SetFacing(t.transform.position.x > transform.position.x ? 1 : -1);
            StartAttack(AttackData.FlyingKick, t, Facing, false);
        }

        void StartAttack(AttackData move, Enemy t, int side, bool counter)
        {
            currentAttack = move;
            target = t;
            isCounter = counter;
            attackDir = move.clip == "back_kick" ? -Facing : side;
            hitDone = false;
            if (counter && TimeController.Instance != null)
            {
                TimeController.Instance.SlowMo(0.25f, 0.9f);
                t.Interrupt();
            }
            if (t != null)
            {
                float gap = Mathf.Abs(t.transform.position.x - transform.position.x);
                if (gap > move.reach + 0.15f && move.clip != "back_kick")
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
            float x = transform.position.x;
            float step = dashSpeed * dt;
            velocity.x = 0f;
            var p = transform.position;
            p.x = Mathf.MoveTowards(x, goal, step);
            transform.position = p;
            if (Mathf.Abs(p.x - goal) < 0.05f || stateTime > 0.6f) BeginSwing();
        }

        void BeginSwing()
        {
            state = State.Attack;
            stateTime = 0f;
            body.Play(currentAttack.clip, 0.05f);
            SetTrails(true);
            if (InkFx.Instance != null) InkFx.Instance.PlayWhoosh();
            if (currentAttack.clip == "flying_kick")
            {
                velocity.y = 6.5f;
                velocity.x = 0f;
                Grounded = false;
            }
        }

        void UpdateAttack()
        {
            var e = body.Current;
            if (e == null) { EnterIdle(); return; }

            if (currentAttack.clip == "flying_kick" && !Grounded && target != null && target.Targetable)
            {
                // glide onto the target during the jump
                var p = transform.position;
                float goal = target.transform.position.x - attackDir * currentAttack.reach * 0.9f;
                p.x = Mathf.MoveTowards(p.x, goal, dashSpeed * 0.6f * Time.deltaTime);
                transform.position = p;
            }

            if (!hitDone && e.hitTime >= 0f && body.Time >= e.hitTime) ResolveHit();

            if (CanCancel())
            {
                var next = GestureInput.Instance != null ? GestureInput.Instance.Next() : Command.None;
                if (next != Command.None) { HandleCommand(next); return; }
            }
            if (body.Finished && Grounded)
            {
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
            // primary target, then anyone else in the strike's path (heavy moves only)
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable) continue;
                float dx = (e.transform.position.x - transform.position.x) * attackDir;
                float dy = Mathf.Abs(e.transform.position.y - transform.position.y);
                bool primary = e == target;
                float reach = currentAttack.reach + (primary ? 0.45f : 0.1f);
                float yTol = currentAttack.clip == "flying_kick" ? 1.8f : 1.0f;
                if (dx < -0.2f || dx > reach || dy > yTol) continue;
                if (!primary && !currentAttack.heavy && target != null) continue; // a whiff can still catch someone stepping in
                bool killing = e.Health <= hit.damage;
                e.TakeHit(hit);
                landed = true;
                ComboCount++;
                ComboTimer = 2f;
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnHeroHit(e, ComboCount, currentAttack.heavy, killing);
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(attackDir, 0.3f, 0f), currentAttack.heavy ? 26 : 12);
            }

            if (!landed) return;
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
                InkFx.Instance.PlayHit(currentAttack.heavy);
                if (currentAttack.heavy || isCounter) InkFx.Instance.ImpactFlash(isCounter ? 0.07f : 0.035f);
            }
        }

        void Dodge(int dir)
        {
            state = State.Dodge;
            stateTime = 0f;
            invulnerableUntil = Time.time + 0.4f;
            velocity.x = dir * 8f;
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
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 22f * Time.deltaTime);
            if (stateTime > 0.38f) EnterIdle();
        }

        public override void TakeHit(HitInfo hit)
        {
            if (Invulnerable) return;
            Health = Mathf.Max(0f, Health - hit.damage);
            ComboCount = 0;
            comboStep = 0;
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
            if (trails == null) return;
            foreach (var t in trails)
            {
                if (t == null) continue;
                if (on) t.Clear();
                t.emitting = on;
            }
        }
    }
}
