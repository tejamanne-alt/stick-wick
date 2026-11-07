using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE FOREMAN (chapter 1's boss, docs/story/chapter1_fight.md). When the crane bay
    /// is clear the gantry crane groans into life and he rides its hook down out of the dark,
    /// hanging from it, drops the last metres to the floor, and roars: the name card. Three phases:
    ///   I   THE CROWBAR (100-66%): a guard up (punches from the front bounce off), three-swing
    ///       crowbar combos (dodge the third and he is open), the charge, the slam, the bear hug.
    ///   II  THE CRANE (at 66%): he runs for the crane's hanging control box. Two of his men come
    ///       in, the hook swings far more often, and the crane drops its loads where you stand. Get
    ///       to him and he lets go of it and fights again, guard up.
    ///   III BARE HANDS (at 33%): the crowbar flung away, enraged, hitting harder: headbutts, the
    ///       hug, double slams. Hurt, he limps, and after a slam he is winded: the opening.
    /// All through it he reads you: the same strike five times running is caught and thrown back.
    /// The hook hangs; every so often it creaks, flashes red and swings across the floor: jump it or
    /// duck it, or it knocks you flat. His last blow does not kill him: it hands him, alive, to the
    /// ending (StoryEnding).</summary>
    public partial class StoryIntro
    {
        const float HookX = 56f, HookTop = 7.2f, HookLen = 5.4f;
        Enemy foreman;
        Transform hook;
        LineRenderer hookChain;
        Renderer hookR;
        float bossT = -1f, prevBossT, hookSwingAt, hookSwingFrom = -1f;
        bool bossLive;
        int bossPhase;                    // 1 the crowbar, 2 the crane (and after it), 3 bare hands
        bool atCrane, craneDone, craneArrived, craneMen;
        float craneT, pendantX, nextDropAt, breathAt;
        Transform pendant;
        LineRenderer pendantCable;

        class Load { public Transform tr; public float x, y, vy, wait, restAt = -1f; }
        readonly List<Load> loads = new List<Load>();
        static Material loadMat, bandMat;

        /// <summary>The crane bay is clear: the Foreman's entrance.</summary>
        void BossArrives()
        {
            bossT = prevBossT = 0f;
            bossLive = false;
            hero.CineLock();
            Sound.Play(Sfx.Riser, 0.7f);
            Sound.Play(Sfx.Chain, 0.9f, new Vector3(HookX, 6f, 0f));
            BuildHook();
            ClearCrane();
            bossPhase = 1;
            atCrane = craneDone = false;
            WaveDirector.BossWeapon = FightStyle.Pipe;               // his crowbar
            WaveDirector.StoryBoss = true;
            foreman = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            WaveDirector.BossWeapon = FightStyle.Fists;
            if (foreman == null) { BeginEnding(null); return; }
            foreman.Foreman = true;
            foreman.Guard = true;
            foreman.SetMaxHealth(foreman.maxHealth * 1.4f);          // three phases: a longer fight
            foreman.Puppet();
            foreman.SetFacing(-1);
            foreman.PuppetPlay("hang", 0.05f);
            Enemy.StoryFinale = (e, hit) => BeginEnding(e);
            HallCheckpoint = 3;
            BossReached = true;
        }

        bool AtBoss(float x) => prevBossT < x && bossT >= x;

        void UpdateBoss()
        {
            if (bossT < 0f || foreman == null) return;
            prevBossT = bossT; bossT += Time.unscaledDeltaTime;
            float y = home.y;
            if (!bossLive)
            {
                // the crane brings him down out of the dark, hanging from the hook
                float k = Ease(0.4f, 2.6f, bossT);
                float hy = Mathf.Lerp(8.6f, 2.15f, k);
                PoseHook(new Vector3(HookX, hy, 0f));
                foreman.PuppetPose(new Vector3(HookX - 0.05f, hy - 2.05f, 0f));
                if (AtBoss(0.4f)) { Sound.Play(Sfx.Rotor, 0.5f, new Vector3(HookX, 7f, 0f), 0.6f); Lightning(0.4f, 0.5f, 0.3f); }
                if (AtBoss(1.4f)) Sound.Play(Sfx.Chain, 0.8f, new Vector3(HookX, 5f, 0f), 0.9f);
                if (AtBoss(2.65f))
                {
                    // he lets go the last metre: the floor shakes
                    foreman.PuppetPose(new Vector3(HookX - 0.05f, y, 0f));
                    foreman.PuppetPlay("duck", 0.03f);
                    Sound.Play(Sfx.Boom, 0.9f);
                    Sound.Play(Sfx.BodyFall, 1f, foreman.transform.position, 0.7f);
                    if (cam != null) cam.Shake(0.35f, 0.4f);
                    if (InkFx.Instance != null) InkFx.Instance.Splash(foreman.transform.position + Vector3.up * 0.1f, Vector3.up, 16, new Color(0.45f, 0.4f, 0.36f));
                    Buzz();
                }
                if (AtBoss(3.1f))
                {
                    foreman.PuppetPlay("taunt", 0.15f);
                    Sound.Play(Sfx.BruteLaugh, 1f, foreman.Chest);
                    Sound.Play(Sfx.CineMassive, 0.8f);
                    if (director != null) director.ShowBanner("THE FOREMAN", 2.6f);
                    if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.05f);
                }
                if (cam != null)
                {
                    // low, from behind the hero, up at him coming down; then on his face for the roar
                    var fp = foreman.transform.position;
                    if (bossT < 3.0f) cam.Scripted(hero.transform.position + new Vector3(-1.4f, 0.5f, -2.4f), new Vector3(HookX, Mathf.Max(1.6f, fp.y + 1.2f), 0f), 1f, 44f);
                    else cam.Scripted(fp + new Vector3(-2.4f, 1.0f, -2.2f), fp + Vector3.up * 1.7f, 1f - Ease(4.3f, 4.9f, bossT), 34f);
                }
                barsPx = Mathf.MoveTowards(barsPx, bossT < 4.3f ? 56f : 0f, Time.unscaledDeltaTime * 120f);
                if (bossT >= 4.9f)
                {
                    bossLive = true;
                    foreman.PuppetRelease();
                    foreman.Passive = false;
                    hero.CineRelease();
                    if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
                    barsPx = 0f;
                    hookSwingAt = Time.time + 7f;
                }
                return;
            }
            UpdatePhases();
            UpdateHookSwing();
            UpdateLoads();
        }

        // ---------------------------------------------------------------- the phases

        void UpdatePhases()
        {
            if (foreman == null || !foreman.Alive || hero.IsDead) return;
            float hp = foreman.Health / Mathf.Max(1f, foreman.maxHealth);
            if (bossPhase == 1 && hp <= 0.66f) BeginCrane();
            else if (atCrane) UpdateCrane();
            else if (bossPhase == 2 && craneDone && hp <= 0.33f)
            {
                // III: the crowbar goes, and so does the guard
                bossPhase = 3;
                foreman.Guard = false;
                foreman.DamageScale = 1.2f;
                foreman.QueueEnrage();
            }
            // hurt badly, he breathes like a bellows: you can hear him tiring
            if (hp < 0.4f && Time.time > breathAt)
            {
                breathAt = Time.time + Random.Range(3f, 4.5f);
                Sound.Play(Sfx.Breath, 0.55f, foreman.Chest, 0.7f);
            }
        }

        /// <summary>II: he breaks off and runs for the crane's control box.</summary>
        void BeginCrane()
        {
            bossPhase = 2;
            atCrane = true;
            craneArrived = craneMen = false;
            craneT = 0f;
            foreman.Guard = false;
            foreman.Puppet();
            pendantX = hero.transform.position.x < HookX ? 61.0f : 51.0f;    // the far end from the hero
            BuildPendant(pendantX);
            foreman.FaceTowards(hero.transform.position.x);
            foreman.PuppetPlay("taunt", 0.1f);
            Sound.Play(Sfx.BruteLaugh, 1f, foreman.Chest);
            Sound.Play(Sfx.Riser, 0.6f);
            if (director != null)
            {
                director.ShowBanner("THE CRANE", 1.6f);
                director.AddStyle(0, "HE'S GOING FOR THE CRANE: GET TO HIM!");
            }
            hookSwingAt = Time.time + 2.5f;
            nextDropAt = Time.time + 3.2f;
        }

        void UpdateCrane()
        {
            float dt = Time.deltaTime;
            craneT += dt;
            var fp = foreman.transform.position;
            if (craneT > 0.8f && !craneArrived)
            {
                // the run to the box
                float nx = Mathf.MoveTowards(fp.x, pendantX - Mathf.Sign(pendantX - HookX) * 0.35f, 4.6f * dt);
                foreman.SetFacing(pendantX > fp.x ? 1 : -1);
                if (foreman.body.CurrentName != "run") foreman.PuppetPlay("run", 0.1f, 1.1f);
                foreman.PuppetPose(new Vector3(nx, 0f, 0f));
                if (Mathf.Abs(nx - (pendantX - Mathf.Sign(pendantX - HookX) * 0.35f)) < 0.02f)
                {
                    craneArrived = true;
                    foreman.FaceTowards(hero.transform.position.x);
                    foreman.PuppetPlay("sword_idle", 0.15f);
                    Sound.Play(Sfx.Latch, 0.8f, new Vector3(pendantX, 1.4f, 0f));
                    Sound.Play(Sfx.Chain, 0.9f, new Vector3(HookX, 6f, 0f), 0.8f);
                }
            }
            else if (craneArrived) foreman.FaceTowards(hero.transform.position.x);

            if (!craneMen && craneT > 1.2f && director != null)
            {
                // two of his men come back in to keep the hero busy
                craneMen = true;
                director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                director.SpawnScripted(EnemyType.Piper, pendantX > HookX ? SpawnKind.LeftDoor : SpawnKind.RightDoor);
            }
            // the crane works: its loads come down where the hero stands
            if (craneArrived && Time.time >= nextDropAt)
            {
                nextDropAt = Time.time + Random.Range(3.6f, 4.8f);
                DropLoad(Mathf.Clamp(hero.transform.position.x + hero.Velocity.x * 0.35f, hero.arenaMinX + 0.8f, hero.arenaMaxX - 0.8f));
            }
            // he is reached (or has had his fun): he lets go and comes for the hero himself
            var hp = hero.transform.position;
            bool reached = craneArrived && Mathf.Abs(hp.x - fp.x) < 2.3f && Mathf.Abs(hp.y - fp.y) < 0.8f;
            if (reached || craneT > 28f)
            {
                atCrane = false;
                craneDone = true;
                foreman.PuppetRelease();
                foreman.Guard = true;
                Sound.Play(Sfx.BruteHurt, 1f, foreman.Chest, 0.7f);
                if (director != null) director.AddStyle(0, "HE LETS GO OF THE CRANE");
                hookSwingAt = Mathf.Max(hookSwingAt, Time.time + 4f);
            }
        }

        /// <summary>The control box hanging on its cable from the crane, at the floor's far end.</summary>
        void BuildPendant(float x)
        {
            var unlit = Shader.Find("Unlit/Color");
            if (pendant == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "CranePendant";
                pendant = go.transform;
                pendant.localScale = new Vector3(0.2f, 0.34f, 0.14f);
                if (unlit != null) go.GetComponent<Renderer>().material = new Material(unlit) { color = new Color(0.78f, 0.78f, 0.78f) };
                var btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(btn.GetComponent<Collider>());
                btn.transform.SetParent(pendant, false);
                btn.transform.localPosition = new Vector3(0f, 0.18f, -0.55f);
                btn.transform.localScale = new Vector3(0.45f, 0.18f, 0.2f);
                if (unlit != null) btn.GetComponent<Renderer>().material = new Material(unlit) { color = new Color(0.8f, 0.08f, 0.06f) };
                pendantCable = new GameObject("PendantCable").AddComponent<LineRenderer>();
                pendantCable.positionCount = 2;
                pendantCable.widthMultiplier = 0.025f;
                var sp = Shader.Find("Sprites/Default");
                if (sp != null) pendantCable.material = new Material(sp);
                pendantCable.startColor = pendantCable.endColor = new Color(0.1f, 0.1f, 0.11f);
            }
            pendant.position = new Vector3(x, 1.45f, -0.2f);
            pendantCable.SetPosition(0, new Vector3(x, 7.5f, -0.2f));
            pendantCable.SetPosition(1, new Vector3(x, 1.62f, -0.2f));
        }

        /// <summary>A pallet of crates let go from the crane: a red ring where it will land, a rattle
        /// of chain overhead, then it comes down. Roll out of the ring.</summary>
        void DropLoad(float x)
        {
            StreakFx.Ring(new Vector3(x, 0.02f, 0f), 1.15f, 1.1f, 0.06f, new Color(1f, 0.15f, 0.1f));
            Sound.Play(Sfx.Chain, 1f, new Vector3(x, 6f, 0f), 1.2f);
            Sound.Play(Sfx.Latch, 0.7f, new Vector3(x, 6f, 0f), 0.7f);
            if (loadMat == null)
            {
                var unlit = Shader.Find("Unlit/Color");
                loadMat = new Material(unlit) { color = new Color(0.27f, 0.27f, 0.28f) };
                bandMat = new Material(unlit) { color = new Color(0.07f, 0.07f, 0.08f) };
            }
            var root = new GameObject("CraneLoad").transform;
            foreach (var (pos, size) in new[] { (new Vector3(-0.32f, 0.3f, 0f), new Vector3(0.6f, 0.55f, 0.8f)), (new Vector3(0.32f, 0.3f, 0f), new Vector3(0.6f, 0.55f, 0.8f)), (new Vector3(0f, 0.82f, 0f), new Vector3(0.7f, 0.5f, 0.75f)) })
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(root, false);
                b.transform.localPosition = pos;
                b.transform.localScale = size;
                b.GetComponent<Renderer>().sharedMaterial = loadMat;
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(band.GetComponent<Collider>());
                band.transform.SetParent(b.transform, false);
                band.transform.localScale = new Vector3(1.02f, 0.12f, 1.02f);
                band.GetComponent<Renderer>().sharedMaterial = bandMat;
            }
            var plt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(plt.GetComponent<Collider>());
            plt.transform.SetParent(root, false);
            plt.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            plt.transform.localScale = new Vector3(1.3f, 0.06f, 0.9f);
            plt.GetComponent<Renderer>().sharedMaterial = bandMat;
            root.position = new Vector3(x, 8.5f, 0f);
            loads.Add(new Load { tr = root, x = x, y = 8.5f, wait = 0.35f });
        }

        void UpdateLoads()
        {
            float dt = Time.deltaTime;
            for (int i = loads.Count - 1; i >= 0; i--)
            {
                var l = loads[i];
                if (l.tr == null) { loads.RemoveAt(i); continue; }
                if (l.restAt >= 0f)
                {
                    if (Time.time > l.restAt)
                    {
                        l.tr.localScale = Vector3.MoveTowards(l.tr.localScale, Vector3.zero, dt * 0.9f);
                        if (l.tr.localScale.x <= 0.01f) { Destroy(l.tr.gameObject); loads.RemoveAt(i); }
                    }
                    continue;
                }
                if (l.wait > 0f) { l.wait -= dt; continue; }
                l.vy -= 22f * dt;
                l.y += l.vy * dt;
                if (l.y <= 0f)
                {
                    l.y = 0f;
                    l.restAt = Time.time + 3f;
                    var at = new Vector3(l.x, 0.3f, 0f);
                    Sound.Play(Sfx.Boom, 0.9f, at);
                    Sound.Play(Sfx.Crate, 1f, at);
                    if (cam != null) cam.Shake(0.28f, 0.3f);
                    if (InkFx.Instance != null) InkFx.Instance.Splash(at, Vector3.up, 26, new Color(0.45f, 0.45f, 0.45f));
                    StreakFx.Ring(new Vector3(l.x, 0.02f, 0f), 1.4f, 0.3f, 0.08f);
                    var hc = hero.transform.position;
                    if (Mathf.Abs(hc.x - l.x) < 1.15f && hc.y < 1.2f && !hero.Invulnerable && !hero.IsDead)
                        hero.TakeHit(new HitInfo { damage = 18f, direction = hc.x >= l.x ? 1 : -1, knockback = 8f, knockdown = true, heavy = true });
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || !e.Targetable || e == foreman || Mathf.Abs(e.transform.position.x - l.x) > 1.15f || e.transform.position.y > 1.2f) continue;
                        e.TakeHit(new HitInfo { damage = 40f, direction = e.transform.position.x >= l.x ? 1 : -1, knockback = 7f, knockdown = true, heavy = true });
                        if (director != null) director.OnEnvironmentKill(e, "CRUSHED!", 200);
                    }
                }
                l.tr.position = new Vector3(l.x, l.y, 0f);
            }
        }

        void ClearCrane()
        {
            foreach (var l in loads) if (l.tr != null) Destroy(l.tr.gameObject);
            loads.Clear();
            if (pendant != null) { Destroy(pendant.gameObject); pendant = null; }
            if (pendantCable != null) { Destroy(pendantCable.gameObject); pendantCable = null; }
        }

        // ---------------------------------------------------------------- the hook

        void BuildHook()
        {
            if (hook != null) return;
            var unlit = Shader.Find("Unlit/Color");
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "CraneHook";
            hook = go.transform;
            hook.localScale = new Vector3(0.32f, 0.42f, 0.26f);
            hookR = go.GetComponent<Renderer>();
            if (unlit != null) hookR.material = new Material(unlit) { color = new Color(0.72f, 0.72f, 0.72f) };
            var claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(claw.GetComponent<Collider>());
            claw.transform.SetParent(hook, false);
            claw.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            claw.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
            if (unlit != null) claw.GetComponent<Renderer>().material = new Material(unlit) { color = new Color(0.22f, 0.22f, 0.24f) };
            hookChain = new GameObject("CraneChain").AddComponent<LineRenderer>();
            hookChain.positionCount = 2;
            hookChain.widthMultiplier = 0.05f;
            var sp = Shader.Find("Sprites/Default");
            if (sp != null) hookChain.material = new Material(sp);
            hookChain.startColor = hookChain.endColor = new Color(0.16f, 0.16f, 0.18f);
        }

        void PoseHook(Vector3 p)
        {
            if (hook == null) return;
            hook.position = p;
            hook.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.x - HookX, HookTop - p.y) * Mathf.Rad2Deg);
            hookChain.SetPosition(0, new Vector3(HookX, HookTop, 0f));
            hookChain.SetPosition(1, p + Vector3.up * 0.2f);
        }

        /// <summary>The hook hangs; every so often it creaks, flashes red and swings right across the
        /// floor. A hero standing in its path (not ducking, not in the air) is knocked flat.</summary>
        void UpdateHookSwing()
        {
            if (hook == null) return;
            float now = Time.time;
            if (hookSwingFrom < 0f && now >= hookSwingAt && foreman != null && foreman.Alive && !hero.IsDead)
            {
                hookSwingFrom = now;
                Sound.Play(Sfx.Chain, 1f, hook.position, 0.75f);
                Sound.Play(Sfx.EnemyEffort, 0.9f, foreman.Chest, 0.8f);
            }
            float angle = 0.06f * Mathf.Sin(now * 1.3f);
            if (hookSwingFrom >= 0f)
            {
                float u = now - hookSwingFrom;
                hookR.material.color = u < 0.7f && Mathf.Repeat(u * 8f, 1f) < 0.5f ? new Color(1f, 0.15f, 0.1f) : new Color(0.72f, 0.72f, 0.72f);
                if (u >= 0.7f)
                {
                    float s = u - 0.7f;
                    angle = 0.95f * Mathf.Sin(s * 2.4f) * Mathf.Exp(-s * 0.35f);
                    var hp = hook.position + Vector3.down * 0.5f;
                    var hc = hero.Chest;
                    if (s < 4f && Mathf.Abs(hp.x - hc.x) < 0.55f && Mathf.Abs(hp.y - hc.y) < 0.7f && !hero.Ducking && hero.Grounded && !hero.Invulnerable && now > hookHitAt)
                    {
                        hookHitAt = now + 1.2f;
                        float vx = Mathf.Cos(s * 2.4f) >= 0f ? 1f : -1f;
                        hero.TakeHit(new HitInfo { damage = 14f, direction = (int)vx, knockback = 7f, knockdown = true, heavy = true });
                        Sound.Play(Sfx.MetalClang, 1f, hp);
                        if (cam != null) cam.Shake(0.25f, 0.25f);
                    }
                    // and it sends any drum in its way rolling across the floor
                    if (s < 4f && hp.y < 1.7f)
                        foreach (var d in OilDrum.All)
                            if (d != null && d.Available && Mathf.Abs(d.transform.position.x - hp.x) < 0.5f)
                            {
                                d.Kick(Mathf.Cos(s * 2.4f) >= 0f ? 1 : -1, null, 7f);
                                Sound.Play(Sfx.MetalClang, 1f, hp, 0.9f);
                            }
                    if (s > 6f) { hookSwingFrom = -1f; hookSwingAt = now + (atCrane ? Random.Range(3.5f, 5f) : bossPhase == 3 ? Random.Range(6f, 8f) : Random.Range(8f, 12f)); }
                }
            }
            PoseHook(new Vector3(HookX + Mathf.Sin(angle) * HookLen, HookTop - Mathf.Cos(angle) * HookLen, 0f));
        }

        float hookHitAt;

        /// <summary>His health across the top while he fights: name, a mark at half (the rage).</summary>
        void ForemanGUI(float w, float h)
        {
            if (!bossLive || foreman == null || !foreman.Alive || phase != Phase.Hall) return;
            float bw = Mathf.Min(560f, w * 0.48f), x = (w - bw) * 0.5f, y = 96f, bh = 14f;
            float hp = Mathf.Clamp01(foreman.Health / Mathf.Max(1f, foreman.maxHealth));
            cap.alignment = TextAnchor.LowerLeft;
            cap.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y - 30, bw, 28), Spaced("THE FOREMAN"), cap);
            cap.alignment = TextAnchor.LowerRight;
            string phaseName = bossPhase >= 3 ? "III · BARE HANDS" : bossPhase == 2 ? "II · THE CRANE" : "I · THE CROWBAR";
            cap.normal.textColor = bossPhase >= 3 ? new Color(1f, 0.25f, 0.2f, 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f)) : new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(x, y - 30, bw, 28), phaseName, cap);
            cap.alignment = TextAnchor.MiddleLeft;
            Fill(new Rect(x - 2, y - 2, bw + 4, bh + 4), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(x, y, bw, bh), new Color(0.2f, 0.02f, 0.03f, 0.75f));
            Fill(new Rect(x, y, bw * hp, bh), foreman.Enraged ? new Color(1f, 0.2f, 0.1f) : new Color(0.75f, 0.06f, 0.07f));
            Fill(new Rect(x + bw * 0.66f - 1.5f, y - 4, 3, bh + 8), new Color(1f, 1f, 1f, 0.9f));      // the crane
            Fill(new Rect(x + bw * 0.33f - 1.5f, y - 4, 3, bh + 8), new Color(1f, 1f, 1f, 0.9f));      // bare hands
        }
    }
}
