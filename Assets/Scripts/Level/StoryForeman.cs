using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE FOREMAN (chapter 1's boss, docs/story/chapter1_fight.md). When the crane bay
    /// is clear the gantry crane groans into life and he rides its hook down out of the dark,
    /// hanging from it, drops the last metres to the floor, and roars: the name card. He fights
    /// with a crowbar (the brute's specials: the charge, the slam, the bear hug) and flings it
    /// away when he is enraged at half health. The hook stays hanging, and every so often it creaks,
    /// flashes red and swings across the floor: jump it or duck it, or it knocks you flat. His last
    /// blow does not kill him: it hands him, alive, to the ending (StoryEnding).</summary>
    public partial class StoryIntro
    {
        const float HookX = 56f, HookTop = 7.2f, HookLen = 5.4f;
        Enemy foreman;
        Transform hook;
        LineRenderer hookChain;
        Renderer hookR;
        float bossT = -1f, prevBossT, hookSwingAt, hookSwingFrom = -1f;
        bool bossLive;

        /// <summary>The crane bay is clear: the Foreman's entrance.</summary>
        void BossArrives()
        {
            bossT = prevBossT = 0f;
            bossLive = false;
            hero.CineLock();
            Sound.Play(Sfx.Riser, 0.7f);
            Sound.Play(Sfx.Chain, 0.9f, new Vector3(HookX, 6f, 0f));
            BuildHook();
            WaveDirector.BossWeapon = FightStyle.Pipe;               // his crowbar
            WaveDirector.StoryBoss = true;
            foreman = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            WaveDirector.BossWeapon = FightStyle.Fists;
            if (foreman == null) { BeginEnding(null); return; }
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
            UpdateHookSwing();
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
            if (unlit != null) hookR.material = new Material(unlit) { color = new Color(0.7f, 0.55f, 0.1f) };
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
                hookR.material.color = u < 0.7f && Mathf.Repeat(u * 8f, 1f) < 0.5f ? new Color(1f, 0.15f, 0.1f) : new Color(0.7f, 0.55f, 0.1f);
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
                    if (s > 6f) { hookSwingFrom = -1f; hookSwingAt = now + Random.Range(8f, 12f); }
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
            if (foreman.Enraged)
            {
                cap.alignment = TextAnchor.LowerRight;
                cap.normal.textColor = new Color(1f, 0.25f, 0.2f, 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f));
                GUI.Label(new Rect(x, y - 30, bw, 28), Spaced("ENRAGED"), cap);
            }
            cap.alignment = TextAnchor.MiddleLeft;
            Fill(new Rect(x - 2, y - 2, bw + 4, bh + 4), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(x, y, bw, bh), new Color(0.2f, 0.02f, 0.03f, 0.75f));
            Fill(new Rect(x, y, bw * hp, bh), foreman.Enraged ? new Color(1f, 0.2f, 0.1f) : new Color(0.75f, 0.06f, 0.07f));
            Fill(new Rect(x + bw * 0.5f - 1.5f, y - 4, 3, bh + 8), new Color(1f, 1f, 1f, 0.9f));
        }
    }
}
