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
        public bool CanBeHeld => Alive && (!Boss || (Cleaner && Dazed)) && !Passive && Targetable && state != State.Scripted && !OnLedge;
        /// <summary>A punch the hero can knock aside: bare hands, coming now.</summary>
        public bool Parryable => Alive && !Armed && Type != EnemyType.Piper && !Ranged &&
                                 (!Boss || ((Foreman && Enraged) || (Driver && DriverPhase != 2) || Cleaner || Handler) && attack.clip != "grab_attack") &&   // bosses, bare-handed (or the Cleaner's stab)
                                 (state == State.Windup || state == State.Attack) && !feinting;
        /// <summary>Too big to lift until he is worn down.</summary>
        public bool TooHeavy => (Type == EnemyType.Brute || Type == EnemyType.Grabber) && HealthFraction > 0.4f && !Dazed;

        public void BeginHeld(HeroController by)
        {
            ReleaseToken();
            state = State.Scripted;
            stateTime = 0f;
            velocity = Vector3.zero;
            heldBy = by;
            chain.Clear(); chained = false; feinting = false;
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
            hurtFor = Boss ? 0.8f : 1.3f;
            dazedUntil = Time.time + (Boss ? 1.0f : 1.6f);
            chain.Clear(); chained = false; feinting = false;
            velocity.x = fromDir * 1.5f;
            body.Play(body.Has("hit_chin") ? "hit_chin" : "hit_front", 0.03f);
        }

        /// <summary>Something huge goes off near him (the train overhead): he flinches, out of it for a moment.</summary>
        public void Startle(float seconds)
        {
            if (state == State.Scripted || state == State.Down || state == State.GetUp || state == State.Dead || !Alive) return;
            ReleaseToken();
            chain.Clear(); chained = false; feinting = false;
            state = State.Hurt;
            stateTime = 0f;
            hurtFor = seconds;
            dazedUntil = Time.time + seconds;
            velocity.x = 0f;
            body.Play(body.Has("block_high") ? "block_high" : "hit_front", 0.08f);
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
    
        // ------------------------------------------------------------------ the story brain: men who fight
        // Closer rings (no hanging about at three metres), real strings of two or three strikes with a
        // short beat between them (each one parryable), a guard that blocks (and reads a move used
        // twice), runners that slip punches, feints, flanking, and fear when badly hurt.

        readonly System.Collections.Generic.Queue<AttackData> chain = new System.Collections.Generic.Queue<AttackData>();
        bool chained, feinting, counterNext;
        int guardHits, flankSide;
        string lastTakenClip;
        float lastTakenAt, flankCheckAt;

        bool flankParity;                                    // half of them are the kind who go round (set in ContactInit)
        bool StoryBrain => hero != null && hero.Contact && !Ranged && (!Boss || Driver || Cleaner || Handler);

        /// <summary>Chapter 4's mini-boss, the one who carried her off: I the grip, II the knuckles, III cornered.</summary>
        public bool Handler;
        public int HandlerPhase = 1;

        /// <summary>Chapter 3's boss, the man who did it: I a knife, II the choke, III anything to hand.</summary>
        public bool Cleaner;
        public int CleanerPhase = 1;
        public bool HasKnife { get; private set; }
        Transform knife;
        /// <summary>The hero's rage (chapter 3): every man of them stands further off and hesitates.</summary>
        public static bool RageFear;

        /// <summary>Chapter 2's mini-boss, the black car's driver: I a boxer, II a tire iron, III bare-handed and furious.</summary>
        public bool Driver;
        public int DriverPhase = 1;

        void ContactInit()
        {
            AttackTokens.Max = hero != null && hero.Contact ? 3 : 2;      // story: three may come at once
            if (!StoryBrain) return;
            flankParity = Random.value < 0.5f;
            ringDistance = Driver ? 2.0f : Type == EnemyType.Runner ? Random.Range(1.5f, 2.1f) : Random.Range(1.4f, 2.2f);
            walkSpeed *= 0.85f;
        }

        /// <summary>Choose the string (the first strike now, the rest queued). False: not a story fighter.</summary>
        bool ContactWindup()
        {
            if (!StoryBrain) return false;
            if (Type == EnemyType.Swordsman || Type == EnemyType.Piper) return false;
            chain.Clear();
            chained = false;
            feinting = false;
            float r = Random.value;
            AttackData[] s;
            if (Handler)
            {
                // I: a grappler, his arms round you (tap to break them), haymakers. II: brass knuckles,
                // quick combinations (parry them). III: cornered by his van, anything.
                if (HandlerPhase == 1)
                {
                    if (r < 0.45f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.75f ? new[] { AttackData.EHaymaker } : new[] { AttackData.EElbow, AttackData.EHaymaker };
                }
                else if (HandlerPhase == 2)
                    s = r < 0.35f ? new[] { AttackData.EJab, AttackData.EJab, AttackData.ECross } : r < 0.7f ? new[] { AttackData.EJab, AttackData.ECross, AttackData.EHook } : new[] { AttackData.ELowKick, AttackData.ECross, AttackData.EHook };
                else
                {
                    if (r < 0.2f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.45f ? new[] { AttackData.Headbutt } : r < 0.75f ? new[] { AttackData.EHaymaker, AttackData.EHaymaker } : new[] { AttackData.EElbow, AttackData.EHook };
                }
                attack = s[0];
                for (int i = 1; i < s.Length; i++) chain.Enqueue(s[i]);
                body.Play("windup", 0.08f);
                return true;
            }
            if (Cleaner)
            {
                // I: the knife, quick stabs and slashes (parry the stab). II: the choke, he wants you from
                // behind. III: anything, wild.
                if (CleanerPhase == 1 && HasKnife)
                    s = r < 0.4f ? new[] { Stab, Stab } : r < 0.75f ? new[] { Slash, Stab } : new[] { Stab, Slash, Stab };
                else if (CleanerPhase <= 2)
                {
                    if (r < 0.45f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.75f ? new[] { AttackData.EJab, AttackData.ECross } : new[] { AttackData.ELowKick, AttackData.EHook };
                }
                else
                    s = r < 0.3f ? new[] { AttackData.EHaymaker } : r < 0.55f ? new[] { AttackData.Headbutt } : r < 0.8f ? new[] { AttackData.EElbow, AttackData.EHaymaker } : new[] { AttackData.ELunge, AttackData.EHook };
                attack = s[0];
                for (int i = 1; i < s.Length; i++) chain.Enqueue(s[i]);
                body.Play("windup", 0.08f);
                return true;
            }
            if (Driver)
            {
                // I: a real boxer, guard up. II: the tire iron, three swings (step out of the third and he is open).
                // III: bare hands again, furious: haymakers, the headbutt, a bear hug.
                if (DriverPhase == 2)
                {
                    s = r < 0.7f ? new[] { AttackData.CrowbarA, AttackData.CrowbarB, AttackData.CrowbarC } : new[] { AttackData.CrowbarC };
                    attack = s[0];
                    for (int i = 1; i < s.Length; i++) chain.Enqueue(s[i]);
                    body.Play("sword_idle", 0.08f);
                    return true;
                }
                if (DriverPhase >= 3)
                {
                    if (r < 0.15f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.4f ? new[] { AttackData.EHaymaker } : r < 0.6f ? new[] { AttackData.Headbutt } : r < 0.85f ? new[] { AttackData.EJab, AttackData.ECross, AttackData.EHaymaker } : new[] { AttackData.ELowKick, AttackData.EHook };
                }
                else
                {
                    s = r < 0.3f ? new[] { AttackData.EJab, AttackData.ECross, AttackData.EHook } : r < 0.55f ? new[] { AttackData.EJab, AttackData.EJab, AttackData.ECross }
                      : r < 0.75f ? new[] { AttackData.ELowKick, AttackData.EHook } : new[] { AttackData.EHaymaker };
                    feinting = Random.value < 0.2f;
                }
                attack = s[0];
                for (int i = 1; i < s.Length; i++) chain.Enqueue(s[i]);
                body.Play("windup", 0.08f);
                return true;
            }
            switch (Type)
            {
                case EnemyType.Runner:
                    s = r < 0.45f ? new[] { AttackData.EJab, AttackData.ECross } : r < 0.75f ? new[] { AttackData.EJab } : new[] { AttackData.ELowKick, AttackData.EJab };
                    feinting = Random.value < 0.22f;
                    break;
                case EnemyType.Grabber:
                    if (r < 0.45f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.75f ? new[] { AttackData.EHaymaker } : new[] { AttackData.EElbow, AttackData.EHaymaker };
                    break;
                case EnemyType.DoorMan:
                    s = r < 0.5f ? new[] { AttackData.EJab, AttackData.ECross, AttackData.EHook } : new[] { AttackData.EJab, AttackData.ECross };
                    break;
                default:
                    if (r < 0.12f) { attack = AttackData.EnemyGrab; body.Play("windup", 0.08f); return true; }
                    s = r < 0.36f ? new[] { AttackData.EJab, AttackData.ECross }
                      : r < 0.56f ? new[] { AttackData.ELowKick, AttackData.EHook }
                      : r < 0.72f ? new[] { AttackData.EFrontKick }
                      : r < 0.86f ? new[] { AttackData.EJab, AttackData.EJab, AttackData.ECross }
                      : new[] { AttackData.ELunge, AttackData.EHook };
                    feinting = Random.value < 0.15f;
                    break;
            }
            attack = s[0];
            for (int i = 1; i < s.Length; i++) chain.Enqueue(s[i]);
            body.Play("windup", 0.08f);
            return true;
        }

        /// <summary>The wind-up's length: a full one to start, a short beat between strikes of a string.</summary>
        float WindupTime => chained ? 0.2f : windupSeconds;

        /// <summary>A feint: halfway through the wind-up he pulls it and steps back.</summary>
        bool FeintNow()
        {
            if (!feinting || stateTime < WindupTime * 0.55f) return false;
            feinting = false;
            chain.Clear();
            ReleaseToken();
            state = State.Recover;
            stateTime = 0f;
            recoverFor = 0.45f;
            counterNext = true;                    // and comes straight back in
            body.Play(body.Has("block_high") ? "block_high" : IdleClip, 0.08f);
            return true;
        }

        /// <summary>The string goes on: the next strike after a short beat (red, so it can be parried).</summary>
        bool NextInChain(float dist)
        {
            if (!StoryBrain || chain.Count == 0) return false;
            if (dist > attackReach() + 0.5f || hero.IsDead) { chain.Clear(); return false; }
            attack = chain.Dequeue();
            chained = true;
            state = State.Windup;
            stateTime = 0f;
            FaceTowards(hero.transform.position.x);
            return true;
        }

        /// <summary>A blow coming at him from the front, standing: he may block it or slip it.</summary>
        bool ContactDefend(HitInfo hit)
        {
            if (!StoryBrain || hit.attacker != hero || hit.hitsDowned) return false;
            bool ready = state == State.Wait || state == State.Approach || state == State.Recover || (state == State.Advance && stateTime < 0.35f);
            if (!ready || hit.direction != -Facing) return false;
            string clip = hero.CurrentStrikeClip;
            bool repeat = clip == lastTakenClip && Time.time - lastTakenAt < 2.5f;
            lastTakenClip = clip;
            lastTakenAt = Time.time;
            if (Dazed || (hit.knockdown && hit.heavy) || guardHits >= 2) { guardHits = 0; return false; }   // finishers break the guard
            if (Type == EnemyType.Runner && !hit.heavy && !hit.low && Random.value < (repeat ? 0.7f : 0.35f))
            {
                // the quick ones slip a punch and come back
                body.Play(body.Has("slip") ? "slip" : "dodge", 0.03f, true, 1.6f);
                state = State.Recover; stateTime = 0f; recoverFor = 0.3f;
                velocity.x = -Facing * 1.5f;
                counterNext = true;
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, repeat ? "HE READ IT: MIX IT UP" : "SLIPPED");
                return true;
            }
            float chance = Handler ? (HandlerPhase == 1 ? 0.35f : HandlerPhase == 2 ? 0.3f : 0f) : Cleaner ? (CleanerPhase == 1 ? 0.3f : CleanerPhase == 2 ? 0.25f : 0f) : Driver ? (DriverPhase == 1 ? 0.45f : DriverPhase == 2 ? 0.2f : 0f)
                         : Type == EnemyType.DoorMan ? 0.4f : Type == EnemyType.Grabber ? 0.3f : Type == EnemyType.Runner ? 0.1f : 0.22f;
            if (repeat) chance += 0.45f;
            if (Random.value >= chance) { guardHits = 0; return false; }
            guardHits++;
            Health = Mathf.Max(MinHealth, Health - hit.damage * 0.2f);
            Tint(Color.white, 0.04f);
            body.Play(body.Has("block_hit") ? "block_hit" : "hit_front", 0.03f);
            velocity.x = hit.direction * 1.2f;
            state = State.Recover; stateTime = 0f; recoverFor = 0.35f;
            counterNext = true;                    // blocked: his turn
            Sound.Play(Sfx.PunchLight, 0.7f, Chest, 0.8f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(0, repeat ? "HE READ IT: MIX IT UP" : "BLOCKED");
            return true;
        }

        /// <summary>Two men on the same side: one works round to the hero's other side.</summary>
        bool ContactFlank(float dt, float dx)
        {
            if (!StoryBrain || Driver) return false;
            if (Time.time > flankCheckAt)
            {
                flankCheckAt = Time.time + 1.2f;
                int mySide = dx > 0 ? -1 : 1;                       // which side of the hero I am on
                int same = 0, other = 0;
                foreach (var e in All)
                {
                    if (e == this || e == null || !e.Alive || e.Ranged || e.Boss || e.IsDown) continue;
                    float ex = e.transform.position.x - hero.transform.position.x;
                    if (Mathf.Abs(ex) > 6f) continue;
                    if (Mathf.Sign(ex) == mySide) same++; else other++;
                }
                flankSide = same >= 1 && other == 0 && flankParity ? -mySide : 0;
            }
            if (flankSide == 0) return false;
            float goal = hero.transform.position.x + flankSide * Ring;
            float to = goal - transform.position.x;
            if (Mathf.Abs(to) < 0.3f) { flankSide = 0; return false; }
            // round him, a step out of line, then back in
            var p = transform.position;
            bool passing = Mathf.Abs(p.x - hero.transform.position.x) < 1.3f || Mathf.Sign(to) == Mathf.Sign(hero.transform.position.x - p.x);
            p.z = Mathf.MoveTowards(p.z, passing ? 0.85f : 0f, 2.5f * dt);
            transform.position = p;
            Walk(Mathf.Sign(to) * 1.1f);
            return true;
        }

        /// <summary>Hurt badly, he hangs back further and waits longer.</summary>
        float FearRing => StoryBrain && !Driver && !Cleaner && !Handler ? (HealthFraction < 0.3f ? 0.9f : 0f) + (RageFear ? 0.6f : 0f) : 0f;

        static readonly AttackData Stab = new AttackData { clip = "jab", damage = 13, reach = 1.05f, knockback = 2f, hitStopFrames = 4, shake = 0.1f, react = "hit_body" };
        static readonly AttackData Slash = new AttackData { clip = "lead_hook", damage = 11, reach = 1.0f, knockback = 2f, hitStopFrames = 4, shake = 0.1f, react = "hit_chin" };

        /// <summary>Make this (a freshly spawned brute) the Cleaner: a man's size, a knife in his hand.</summary>
        public void MakeCleaner(float health)
        {
            Cleaner = true;
            CleanerPhase = 1;
            transform.localScale = Vector3.one * 1.05f;
            walkSpeed = 2.5f;
            windupSeconds = 0.38f;
            DamageScale = 1.2f;
            SetMaxHealth(health);
            ShowIron(false);
            var hand = body != null ? body.Bone("hand.R") : null;
            if (hand != null)
            {
                var k = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(k.GetComponent<Collider>());
                k.name = "Knife";
                k.transform.SetParent(hand, false);
                k.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                k.transform.localScale = new Vector3(0.025f, 0.24f, 0.05f);
                var mr = k.GetComponent<Renderer>();
                mr.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.86f, 0.87f, 0.9f) };
                knife = k.transform;
            }
            HasKnife = true;
            ContactInit();
        }

        /// <summary>Make this (a freshly spawned brute) the Handler: big, slow to start, hands like shovels.</summary>
        public void MakeHandler(float health)
        {
            Handler = true;
            HandlerPhase = 1;
            transform.localScale = Vector3.one * 1.25f;
            walkSpeed = 2.0f;
            windupSeconds = 0.5f;
            DamageScale = 1.2f;
            SetMaxHealth(health);
            ShowIron(false);
            ContactInit();
        }

        /// <summary>Phase II: the knuckles go on, he gets quicker.</summary>
        public void HandlerKnuckles()
        {
            HandlerPhase = 2;
            windupSeconds = 0.34f;
            walkSpeed = 2.6f;
            DamageScale = 1.35f;
        }

        /// <summary>The knife taken off him (a clinch on him dazed): it clatters away.</summary>
        public void Disarm(int dir)
        {
            if (!HasKnife) return;
            HasKnife = false;
            if (knife != null)
            {
                knife.SetParent(null, true);
                var fall = knife.gameObject.AddComponent<ShieldFall>();
                fall.velocity = new Vector3(dir * 3f, 4f, 0f);
            }
            Sound.Play(Sfx.Knife, 1f, Chest);
            if (CleanerPhase == 1) CleanerPhase = 2;
        }

        /// <summary>Make this (a freshly spawned brute) the Driver: a man's size, a boxer's guard.</summary>
        public void MakeDriver(float health)
        {
            Driver = true;
            DriverPhase = 1;
            transform.localScale = Vector3.one * 1.12f;
            walkSpeed = 2.3f;
            windupSeconds = 0.42f;
            DamageScale = 1.25f;
            SetMaxHealth(health);
            ShowIron(false);
            ContactInit();
        }

        /// <summary>The Driver's tire iron: in his hand from phase II (the pipe the boss spawned with).</summary>
        public void ShowIron(bool on)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Pipe") { t.gameObject.SetActive(on); break; }
        }

        /// <summary>Hidden in the car (phase III's charge), then back out of it.</summary>
        public void Hide(bool hidden)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !hidden;
        }
    }
}
