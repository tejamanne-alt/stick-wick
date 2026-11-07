using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE ENDING of chapter 1 (docs/story/chapter1_fight.md, section 5): directed, no input.
    /// The Foreman goes down in slow motion under the torn skylight and does not get up. The hero
    /// kneels over him, and from here it is side on: two black shapes against a curtain of lit
    /// rain. He asks; the Foreman laughs ("Five million reasons, friend"). A pistol comes up out of
    /// the dark into his face. They struggle, and the barrel turns, inch by inch: a white frame and
    /// silence. The hero backs away, staring at his hands: no, no, no. Then the dead man's phone
    /// buzzes in the water. On its screen, a surveillance still of the hero: THE FERRYMAN,
    /// $5,000,000, OPEN. He answers. A calm voice: "Is it done?" ... "Ferryman. You were supposed to
    /// stay dead." "The contract is open. Every gun in this city is coming for you." He lets the
    /// phone fall and walks out of the big door into the rain. CHAPTER 1 COMPLETE.</summary>
    public partial class StoryIntro
    {
        public static bool BossReached;
        float headYaw;
        bool headYawOn;
        Transform gun;
        string cardLine;
        float cardAt = -1f;

        void BeginEnding(Enemy boss)
        {
            Enemy.StoryFinale = null;
            HallCheckpoint = -1;
            BossReached = false;
            phase = Phase.Ending;
            StartCoroutine(Ending(boss));
        }

        IEnumerator Ending(Enemy boss)
        {
            Sound.Tension(false);
            hero.CinePuppet();
            float bx = boss != null ? boss.transform.position.x : hero.transform.position.x + 1.6f;
            // ---------------------------------------------------------------- 1 the final blow
            if (boss != null)
            {
                boss.Puppet();
                boss.SetFacing(1);                                        // he will lie with his head towards the hero
                boss.PuppetPlay("knockdown", 0.03f, 0.6f);
            }
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 1.6f, 0.02f, 0.4f);
            Sound.Play(Sfx.Finisher, 1f);
            Sound.Play(Sfx.SlowmoIn, 0.6f);
            // he comes down under the torn skylight, in the column of rain the floodlight pours through
            float landX = Mathf.Clamp(bx + 1.4f * Mathf.Sign(bx - hero.transform.position.x), 54.8f, 57.6f);
            float t0 = 0f;
            var bp0 = boss != null ? boss.transform.position : new Vector3(landX, 0f, 0f);
            while (t0 < 1.7f)
            {
                t0 += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t0 / 1.5f);
                if (boss != null) boss.PuppetPose(new Vector3(Mathf.Lerp(bp0.x, landX, 1f - (1f - k) * (1f - k)), Mathf.Max(0f, Mathf.Sin(k * Mathf.PI) * 0.8f), 0f));
                EndShot(new Vector3(landX - 3.2f, 1.0f, -4.6f), new Vector3(landX - 0.6f, 0.8f, 0f), 40f);
                barsPx = Mathf.MoveTowards(barsPx, 70f, Time.unscaledDeltaTime * 140f);
                yield return null;
            }
            Sound.Play(Sfx.Boom, 0.8f);
            Sound.Play(Sfx.BodySplash, 1f, new Vector3(landX, 0f, 0f));
            if (cam != null) cam.Shake(0.3f, 0.35f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(new Vector3(landX, 0.2f, 0f), Vector3.up, 18, new Color(0.5f, 0.56f, 0.7f));
            if (boss != null) boss.PuppetPlay("lie_up", 0.4f);
            Sound.Ambience(1.6f, 0.2f, 22000f);                           // the rain, loud in the silence
            yield return RealWait(1.0f);

            // ---------------------------------------------------------------- 2 he walks over and kneels
            hero.SetFacing(1);
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.25f, true, 0.8f);
            float hx0 = hero.transform.position.x, kneelX = landX - 1.05f;
            float t1 = 0f;
            while (t1 < 2.4f)
            {
                t1 += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t1 / 2.2f);
                hero.CinePose(new Vector3(Mathf.Lerp(hx0, kneelX, k), 0f, 0f));
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.7f, hero.transform.position, 0.8f); }
                EndShot(new Vector3(landX - 4.6f, 0.32f, -3.1f), new Vector3(landX - 0.8f, 0.6f, 0f), 40f);
                yield return null;
            }
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.35f);
            Sound.Play(Sfx.Cloth, 0.9f);
            Sound.Play(Sfx.Breath, 0.8f);

            // ---------------------------------------------------------------- 3 from here on, side on: two black shapes against the light
            float mx = landX - 0.5f;
            BuildBacklight(mx);
            if (boss != null) boss.SetBaseColor(new Color(0.02f, 0.02f, 0.025f));       // the light takes his colour away
            System.Action wide = () => { Rain(); EndShot(new Vector3(mx, 0.28f, -3.6f), new Vector3(mx, 0.6f, 0f), 34f); };
            System.Action close = () => { Rain(); EndShot(new Vector3(mx, 0.3f, -2.3f), new Vector3(mx, 0.52f, 0f), 30f); };
            yield return Hold(1.4f, wide);

            Say("HIM", "Why are you trying to kill me?", 2.4f);
            Sound.Play(Sfx.Heartbeat, 0.6f);
            yield return Hold(2.4f, wide);
            if (boss != null) Sound.Play(Sfx.BruteLaugh, 0.55f, boss.Chest, 0.8f);
            Say("THE FOREMAN", "You... really don't remember.", 2.4f);
            yield return Hold(2.4f, close);
            Say("HIM", "Tell me who I am. WHO SENT YOU?", 2.4f);
            Sound.Play(Sfx.Tinnitus, 0.25f);
            yield return Hold(2.4f, close);
            Say("THE FOREMAN", "Nobody had to send us. Five million reasons, friend.", 3.0f);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Hold(3.0f, close);

            // ---------------------------------------------------------------- 4 the gun, up out of the dark
            BuildGun();
            Sound.Play(Sfx.Cloth, 0.6f);
            Sound.Play(Sfx.Heartbeat, 1f, null, 1.1f);
            if (boss != null) boss.PuppetPlay("lie_gun", 0.25f, 0.6f);
            float tg = 0f;
            while (tg < 1.6f)
            {
                tg += Time.unscaledDeltaTime;
                var hand = HandOf(boss, landX);
                PoseGun(hand, Vector3.Slerp(Vector3.right, HeadC() - hand, Mathf.SmoothStep(0f, 1f, tg / 1.2f)));
                wide();
                yield return null;
            }
            Sound.Play(Sfx.Latch, 0.6f, null, 1.8f);                       // the hammer back
            Say("THE FOREMAN", "Nothing personal.", 1.8f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.4f, 1.6f, 0.05f, 0.3f);
            yield return Hold(1.8f, () => { var hand = HandOf(boss, landX); PoseGun(hand, HeadC() - hand); close(); });

            // ---------------------------------------------------------------- 5 the struggle: the barrel turns, inch by inch
            if (hero.body.Has("kneel_grip")) hero.body.Play("kneel_grip", 0.06f, true, 1.4f);
            Sound.Play(Sfx.HeroEffort, 1f);
            Sound.Play(Sfx.EnemyEffort, 1f, null, 0.8f);
            float ts = 0f;
            while (ts < 2.0f)
            {
                ts += Time.unscaledDeltaTime;
                var hand = HandOf(boss, landX);
                var grip = HeroHand();
                float turn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ts - 0.5f) / 1.3f));
                var at = Vector3.Lerp(hand, grip, 0.45f) + new Vector3(Random.Range(-0.025f, 0.025f), Random.Range(-0.02f, 0.02f), 0f) * (1f + ts);
                var toHero = HeadC() - at;
                var toBoss = (boss != null ? boss.Chest : new Vector3(landX, 0.4f, 0f)) - at;
                PoseGun(at, Vector3.Slerp(toHero, toBoss, turn) + Random.insideUnitSphere * 0.08f);
                var shake = new Vector3(Random.Range(-0.03f, 0.03f), Random.Range(-0.02f, 0.02f), 0f) * (1f + ts);
                Rain();
                EndShot(new Vector3(mx, 0.3f, -2.6f + ts * 0.2f) + shake, new Vector3(mx, 0.55f, 0f) + shake * 0.5f, 30f);
                if (Random.value < 0.06f) Sound.Play(Sfx.Cloth, 0.6f, null, Random.Range(0.9f, 1.2f));
                backGlow = 1f + Mathf.Sin(ts * 23f) * 0.08f;
                yield return null;
            }

            // ---------------------------------------------------------------- 6 BANG
            flashFrames = 4;
            backGlow = 2.2f;
            Sound.Play(Sfx.GunBody, 1f);
            Sound.Play(Sfx.GunTail, 1f);
            Buzz();
            if (cam != null) cam.Shake(0.2f, 0.2f);
            Sound.Ambience(0.25f, 0.05f, 600f);                            // the world goes away
            Sound.Play(Sfx.Tinnitus, 1f);
            if (InkFx.Instance != null && boss != null) InkFx.Instance.Splash(boss.Chest, new Vector3(1f, 0.4f, 0f), 30, boss.InkColor);
            yield return Hold(0.25f, wide);

            // ---------------------------------------------------------------- 7 he dies
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.2f, 2.0f, 0.02f, 0.5f);
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.5f);
            if (boss != null) boss.PuppetPlay("lie", 1.2f);
            DropGun();
            for (int i = 0; i < 4 && boss != null; i++)
                if (InkFx.Instance != null) InkFx.Instance.Stain(boss.transform.position + Vector3.right * (0.2f * i - 0.3f), boss.InkColor, 0.8f + 0.4f * i, false);
            yield return Hold(3.0f, () => { backGlow = Mathf.MoveTowards(backGlow, 1f, Time.unscaledDeltaTime * 2f); wide(); });

            // ---------------------------------------------------------------- 8 no... no... no...
            Sound.Ambience(1.2f, 0.25f, 22000f);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.8f);
            Sound.Play(Sfx.Cloth, 0.7f);
            yield return Hold(0.9f, wide);
            if (hero.body.Has("look_hands")) hero.body.Play("look_hands", 0.3f, true, 0.7f);
            headYawOn = true;
            Say("HIM", "No... no... no...", 2.6f);
            Sound.Play(Sfx.Breath, 0.9f, null, 0.9f);
            float tn = 0f;
            float stand = hero.transform.position.x;
            while (tn < 4.2f)
            {
                tn += Time.unscaledDeltaTime;
                headYaw = Mathf.Sin(tn * 3.2f) * 26f * Mathf.Clamp01(tn / 0.6f) * (1f - Mathf.Clamp01((tn - 3.6f) / 0.6f));
                hero.CinePose(new Vector3(stand - 0.5f * Mathf.SmoothStep(0f, 1f, tn / 3f), 0f, 0f));      // backing away
                if (tn > 2.6f && tn - Time.unscaledDeltaTime <= 2.6f) Say("HIM", "That wasn't... I didn't...", 1.8f);
                Rain();
                EndShot(new Vector3(mx - 0.3f, 0.9f, -3.0f + 0.4f * tn / 4.2f), HeadC() + Vector3.down * 0.4f, 32f);
                yield return null;
            }
            headYawOn = false;

            // ---------------------------------------------------------------- 9 a phone, buzzing in the water beside the dead man
            var phoneAt = new Vector3(landX + 0.35f, 0.012f, -0.35f);
            BuildPhone(phoneAt);
            for (int b = 0; b < 2; b++)
            {
                Sound.Play(Sfx.PhoneBuzz, 1f, phoneAt);
                float tb = 0f;
                while (tb < 1.5f)
                {
                    tb += Time.unscaledDeltaTime;
                    PhoneBuzzing(tb < 1.1f);
                    Rain();
                    EndShot(phoneAt + new Vector3(-0.45f, 0.22f, -0.5f), phoneAt, 34f);
                    yield return null;
                }
            }
            // he goes to it, kneels, picks it up
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 0.7f);
            float hx1 = hero.transform.position.x, tk = 0f;
            while (tk < 1.0f)
            {
                tk += Time.unscaledDeltaTime;
                hero.CinePose(new Vector3(Mathf.Lerp(hx1, landX - 0.4f, Mathf.SmoothStep(0f, 1f, tk)), 0f, 0f));
                PhoneBuzzing(true);
                wide();
                yield return null;
            }
            hero.CinePose(new Vector3(landX - 0.4f, 0f, 0f));
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.3f);
            Sound.Play(Sfx.PhoneBuzz, 1f, phoneAt);
            yield return Hold(0.7f, () => { PhoneBuzzing(true); wide(); });
            if (phone != null) phone.gameObject.SetActive(false);

            // ---------------------------------------------------------------- 10 the screen: his own face, and a price on it
            phoneUi = 1; phoneUiAt = Time.unscaledTime;
            Sound.Play(Sfx.PhoneBuzz, 0.8f);
            yield return Hold(3.6f, () => { Rain(); EndShot(HeadC() + new Vector3(-0.2f, 0.1f, -0.9f), HeadC() + Vector3.down * 0.25f, 30f); });

            // ---------------------------------------------------------------- 11 the call
            phoneUi = 2; phoneUiAt = Time.unscaledTime;
            Sound.Play(Sfx.PhoneClick, 0.9f);
            Sound.Play(Sfx.Static, 0.12f);
            System.Action pov = () => { Rain(); EndShot(HeadC() + new Vector3(-0.2f, 0.1f, -0.9f), HeadC() + Vector3.down * 0.25f, 30f); };
            yield return Hold(0.6f, pov);
            Say("THE VOICE", "Is it done?", 2.2f);
            yield return Hold(2.2f, pov);
            Sound.Play(Sfx.Heartbeat, 0.7f);
            Sound.Play(Sfx.Breath, 0.5f, null, 0.9f);
            yield return Hold(1.8f, pov);                                 // he says nothing. The line hears him breathe
            Say("THE VOICE", "...Ferryman.", 1.8f);
            yield return Hold(1.8f, pov);
            Say("THE VOICE", "You were supposed to stay dead.", 2.6f);
            Sound.Play(Sfx.Tinnitus, 0.3f);
            yield return Hold(2.6f, pov);
            Say("HIM", "Who is this?", 1.8f);
            yield return Hold(1.8f, pov);
            Say("THE VOICE", "The contract is open. Every gun in this city is coming for you.", 3.6f);
            yield return Hold(3.6f, pov);
            phoneUi = 3; phoneUiAt = Time.unscaledTime;
            Sound.Play(Sfx.PhoneClick, 0.9f);
            yield return Hold(1.4f, pov);
            phoneUi = 0;
            // the phone drops into the water at his knee
            if (phone != null)
            {
                phone.position = new Vector3(landX - 0.25f, 0.012f, -0.3f);
                phone.gameObject.SetActive(true);
                phoneGlow = 0.35f;
            }
            Sound.Play(Sfx.Splash, 0.5f, null, 1.4f);
            ClearBacklight();
            if (boss != null) boss.SetBaseColor(boss.InkColor);

            // ---------------------------------------------------------------- 12 wide, high: small in the empty hall
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.8f);
            yield return Hold(2.6f, () => EndShot(new Vector3(landX - 7.0f, 7.2f, -4.5f), new Vector3(landX - 1.0f, 0.4f, 0.5f), 46f));

            // ---------------------------------------------------------------- 13 he walks out into the rain
            hero.SetFacing(1);
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.3f, true, 0.75f);
            Lightning(0.9f, 1f, 0.6f);
            ShowSearch(true);
            float tw = 0f, wx0 = hero.transform.position.x;
            while (tw < 6.5f)
            {
                tw += Time.unscaledDeltaTime;
                float x = wx0 + 1.05f * tw;
                hero.CinePose(new Vector3(x, 0f, 0.2f * Mathf.Clamp01((x - 60f) / 2f)));
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.6f; Sound.Play(Sfx.Footstep, 0.6f, hero.transform.position, 0.8f); }
                if (searchlight != null)
                {
                    var heli = new Vector3(78f, 26f, 30f);
                    var target = new Vector3(70f + Mathf.Sin(tw * 0.7f) * 6f, 0f, 6f + Mathf.Cos(tw * 0.5f) * 4f);
                    searchlight.position = heli;
                    searchlight.rotation = Quaternion.FromToRotation(Vector3.down, target - heli);
                    if (searchPool != null) searchPool.position = target + Vector3.up * 0.05f;
                }
                if (tw > 1.5f && tw - Time.unscaledDeltaTime <= 1.5f) Sound.Play(Sfx.Rotor, 0.5f);
                EndShot(new Vector3(landX - 4.5f, 1.5f, -0.6f), new Vector3(63f, 2.0f, 1.5f), 42f);
                barsPx = Mathf.MoveTowards(barsPx, 90f, Time.unscaledDeltaTime * 8f);
                yield return null;
            }
            Sound.Play(Sfx.Thunder, 0.8f);

            // ---------------------------------------------------------------- 14 black: chapter complete
            cardAt = Time.unscaledTime;
            cardLine = "Someone put you in the ground. You climbed out.";
            yield return RealWait(6.0f);
            ShowSearch(false);
            Retry = false;
            EscapeSeen = false;
            phase = Phase.End;
        }

        // ---------------------------------------------------------------- helpers

        static IEnumerator RealWait(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>Hold for `s` real seconds, framing each frame with `frame`.</summary>
        static IEnumerator Hold(float s, System.Action frame)
        {
            float t = 0f;
            while (t < s) { t += Time.unscaledDeltaTime; frame(); yield return null; }
        }

        void EndShot(Vector3 pos, Vector3 look, float fov)
        {
            if (cam == null) return;
            cam.ScriptRoll = 0f;
            cam.Scripted(pos + Sway(0.012f, 0.4f, 41f), look, 1f, fov);
        }

        Vector3 BossHead(Enemy boss, float x)
        {
            var hd = boss != null && boss.body != null ? boss.body.Bone("head") : null;
            return hd != null ? hd.position + hd.up * 0.27f : new Vector3(x - 0.5f, 0.3f, 0f);
        }

        Vector3 HandOf(Enemy boss, float x)
        {
            var h = boss != null && boss.body != null ? boss.body.Bone("hand.R") : null;
            return h != null ? h.position : new Vector3(x, 0.4f, 0f);
        }

        Vector3 HeroHand()
        {
            var h = hero.body != null ? hero.body.Bone("hand.R") : null;
            return h != null ? h.position : hero.transform.position + new Vector3(0.6f, 0.6f, 0f);
        }

        // ---------------------------------------------------------------- the pistol (posed in the world, never trusted to a bone's axes)

        void BuildGun()
        {
            if (gun != null) return;
            gun = new GameObject("ForemanPistol").transform;
            gun.localScale = Vector3.one * 1.6f;
            FighterFactory.Model(gun, FightStyle.Gun, hero.weaponMaterial);
        }

        /// <summary>The pistol at `at`, its barrel (the model's +z) along `aim`, grip down.</summary>
        void PoseGun(Vector3 at, Vector3 aim)
        {
            if (gun == null) return;
            aim.z = 0f;
            if (aim.sqrMagnitude < 1e-4f) aim = Vector3.right;
            aim.Normalize();
            var up = new Vector3(-aim.y, aim.x, 0f);              // square to the barrel, in the picture plane ...
            if (up.y < 0f) up = -up;                              // ... on the upper side: the grip hangs below
            gun.position = at;
            gun.rotation = Quaternion.LookRotation(aim, up);
        }

        void DropGun()
        {
            if (gun == null) return;
            var f = gun.gameObject.AddComponent<ShieldFall>();
            f.velocity = new Vector3(0.6f, 1.2f, 0f);
            gun = null;
        }

        // ---------------------------------------------------------------- the light behind them: rain under the torn skylight

        Transform backdrop;
        Material backMat;
        float backGlow = 1f;
        readonly System.Collections.Generic.List<Transform> drops = new System.Collections.Generic.List<Transform>();

        /// <summary>A curtain of cold light behind the two of them (the floodlight through the torn
        /// skylight, caught in the rain), falling rain in front of it: everything in front of it
        /// reads as a black shape.</summary>
        void BuildBacklight(float x)
        {
            ClearBacklight();
            const int W = 128, H = 72;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    float u = (i + 0.5f) / W - 0.5f, v = (j + 0.5f) / H;
                    // a soft column of light, brightest a little above head height, falling off to the dark
                    float col = Mathf.Exp(-u * u / 0.035f);
                    float hgt = Mathf.Clamp01(1.15f - Mathf.Abs(v - 0.42f) * 1.3f);
                    float k = Mathf.Clamp01(col * hgt * 1.15f + Mathf.Exp(-u * u / 0.2f) * 0.18f);
                    var c = Color.Lerp(new Color(0.015f, 0.017f, 0.025f), new Color(0.78f, 0.85f, 0.95f), k);
                    c.a = 1f;
                    tex.SetPixel(i, j, c);
                }
            tex.Apply();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Backlight";
            backdrop = q.transform;
            backdrop.position = new Vector3(x, 2.4f, 2.8f);
            backdrop.localScale = new Vector3(10f, 5.6f, 1f);
            backMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };   // tintable: the shot brightens it
            q.GetComponent<Renderer>().material = backMat;
            backGlow = 1f;
            // the rain, lit
            var dropMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.85f, 0.92f, 1f, 0.35f) };
            for (int i = 0; i < 90; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(d.GetComponent<Collider>());
                d.name = "LitRain";
                d.transform.SetParent(backdrop, true);
                d.transform.position = new Vector3(x + Random.Range(-2.6f, 2.6f), Random.Range(0f, 4.6f), Random.Range(1.2f, 2.6f));
                d.transform.localScale = new Vector3(0.008f / backdrop.localScale.x, 0.32f / backdrop.localScale.y, 1f);
                d.GetComponent<Renderer>().sharedMaterial = dropMat;
                drops.Add(d.transform);
            }
        }

        /// <summary>The rain falls and the light breathes (called every frame of the lit shots).</summary>
        void Rain()
        {
            if (backdrop == null) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var d in drops)
            {
                if (d == null) continue;
                var p = d.position;
                p.y -= 9f * dt;
                if (p.y < 0f) p.y += 4.6f;
                d.position = p;
            }
            if (backMat != null) backMat.color = Color.white * Mathf.Min(backGlow, 2.5f) * (0.97f + 0.03f * Mathf.Sin(Time.unscaledTime * 1.7f));
        }

        void ClearBacklight()
        {
            if (backdrop != null) Destroy(backdrop.gameObject);
            backdrop = null;
            drops.Clear();
        }

        // ---------------------------------------------------------------- the phone

        Transform phone;
        Renderer phoneScreen;
        float phoneGlow;
        int phoneUi;                       // 0 none, 1 the screen (the contract), 2 on the call, 3 call ended
        float phoneUiAt;
        Texture2D portrait;

        void BuildPhone(Vector3 at)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(body.GetComponent<Collider>());
            body.name = "DeadMansPhone";
            phone = body.transform;
            phone.position = at;
            phone.rotation = Quaternion.Euler(0f, 24f, 0f);
            phone.localScale = new Vector3(0.075f, 0.01f, 0.15f);
            var unlit = Shader.Find("Unlit/Color");
            body.GetComponent<Renderer>().material = new Material(unlit) { color = new Color(0.03f, 0.03f, 0.035f) };
            var scr = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(scr.GetComponent<Collider>());
            scr.transform.SetParent(phone, false);
            scr.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            scr.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            scr.transform.localScale = new Vector3(0.88f, 0.9f, 1f);
            phoneScreen = scr.GetComponent<Renderer>();
            phoneScreen.material = new Material(unlit) { color = Color.black };
            // the glow it throws on the wet floor
            var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(pool.GetComponent<Collider>());
            pool.name = "PhoneGlow";
            pool.transform.SetParent(phone, false);
            pool.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(9f, 4.5f, 1f);
            var pm = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.55f, 0.7f, 1f, 0.0f) };
            pool.GetComponent<Renderer>().material = pm;
            phoneGlow = 0f;
        }

        /// <summary>Lit and rattling on the concrete while it rings; dim otherwise.</summary>
        void PhoneBuzzing(bool on)
        {
            if (phone == null) return;
            phoneGlow = Mathf.MoveTowards(phoneGlow, on ? 1f : 0.6f, Time.unscaledDeltaTime * 4f);
            if (phoneScreen != null) phoneScreen.material.color = Color.Lerp(Color.black, new Color(0.75f, 0.85f, 1f), phoneGlow);
            var pool = phone.Find("PhoneGlow");
            if (pool != null) pool.GetComponent<Renderer>().material.color = new Color(0.55f, 0.7f, 1f, 0.28f * phoneGlow);
            if (on)
            {
                var p = phone.position;
                p.x += Mathf.Sin(Time.unscaledTime * 90f) * 0.0006f;
                phone.position = p;
                phone.rotation = Quaternion.Euler(0f, 24f + Mathf.Sin(Time.unscaledTime * 70f) * 1.2f, 0f);
            }
        }

        /// <summary>A grainy surveillance still of him (the scar on his temple), for the contract.</summary>
        Texture2D Portrait()
        {
            if (portrait != null) return portrait;
            const int W = 90, H = 110;
            portrait = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var r = new System.Random(5);
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    float g = 0.42f + 0.18f * j / H;
                    var c = new Color(g * 0.92f, g * 0.95f, g, 1f);
                    float hx = i - 45f, hy = j - 66f;
                    bool headIn = hx * hx + hy * hy * 0.85f < 17f * 17f;
                    bool neck = Mathf.Abs(hx) < 5f && j > 30 && j < 52;
                    bool shoulders = j < 32 && Mathf.Abs(hx) < 36f - (32 - j) * 0.1f && j > 0 && (j < 30 - Mathf.Abs(hx) * 0.15f + 6f);
                    if (headIn || neck || shoulders) c = new Color(0.04f, 0.04f, 0.05f);
                    // the eyes: two white ovals, looking off
                    float e1 = (i - 39f) * (i - 39f) / 9f + (j - 68f) * (j - 68f) / 16f, e2 = (i - 51f) * (i - 51f) / 9f + (j - 68f) * (j - 68f) / 16f;
                    if (e1 < 1f || e2 < 1f) c = new Color(0.92f, 0.92f, 0.9f);
                    // the wound on the temple
                    if (i >= 57 && i <= 60 && j >= 72 && j <= 80 && (i + j) % 2 == 0) c = new Color(0.7f, 0.06f, 0.05f);
                    // grain and scan lines: a camera still, blown up
                    float n = (float)r.NextDouble() * 0.09f - 0.045f;
                    c = new Color(c.r + n, c.g + n, c.b + n, 1f);
                    if (j % 3 == 0) c *= 0.93f;
                    portrait.SetPixel(i, j, c);
                }
            portrait.Apply();
            return portrait;
        }

        /// <summary>The phone in his hand, in front of everything: the contract on him, then the call.</summary>
        void PhoneGUI(float w, float h)
        {
            if (phoneUi == 0) return;
            float t = Time.unscaledTime - phoneUiAt;
            float pw = Mathf.Min(300f, w * 0.3f), ph = pw * 1.9f;
            float shake = Mathf.Sin(Time.unscaledTime * 11f) * 1.5f;
            float rise = phoneUi == 1 ? (1f - Mathf.SmoothStep(0f, 1f, t / 0.5f)) * 120f : phoneUi == 3 ? Mathf.SmoothStep(0f, 1f, (t - 0.9f) / 0.5f) * h : 0f;
            var r = new Rect((w - pw) * 0.5f + shake, h * 0.5f - ph * 0.56f + rise, pw, ph);
            Fill(new Rect(r.x - 8, r.y - 8, r.width + 16, r.height + 16), new Color(0.02f, 0.02f, 0.025f, 0.98f));     // the case
            var sc = new Rect(r.x, r.y, r.width, r.height);
            Fill(sc, new Color(0.05f, 0.06f, 0.08f, 1f));
            var old = cap.alignment;
            cap.alignment = TextAnchor.MiddleCenter;
            if (phoneUi == 1 || phoneUi == 2 || phoneUi == 3)
            {
                // the message that came with the call: the contract
                float pad = 14f, y = sc.y + 16f;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(sc.x, y, sc.width, 22), Spaced("CONTRACT UPDATE"), cap);
                y += 28f;
                var img = new Rect(sc.x + pad * 2f, y, sc.width - pad * 4f, (sc.width - pad * 4f) * 1.2f);
                GUI.DrawTexture(img, Portrait());
                Fill(new Rect(img.x, img.y, img.width, 2), new Color(0.8f, 0.06f, 0.07f));
                y = img.yMax + 10f;
                cap.normal.textColor = Color.white;
                GUI.Label(new Rect(sc.x, y, sc.width, 26), Spaced("THE FERRYMAN"), cap);
                y += 28f;
                big.fontSize = Mathf.RoundToInt(pw * 0.16f);
                big.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
                GUI.Label(new Rect(sc.x, y, sc.width, pw * 0.2f), "$5,000,000", big);
                big.fontSize = 72;
                y += pw * 0.2f + 4f;
                cap.normal.textColor = new Color(1f, 0.25f, 0.2f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
                GUI.Label(new Rect(sc.x, y, sc.width, 22), Spaced("OPEN  ·  CONFIRM KILL"), cap);
                // the call, across the bottom
                var bar = new Rect(sc.x + 10f, sc.yMax - 78f, sc.width - 20f, 64f);
                Fill(bar, new Color(0.12f, 0.13f, 0.16f, 0.96f));
                cap.normal.textColor = Color.white;
                if (phoneUi == 1)
                {
                    GUI.Label(new Rect(bar.x, bar.y + 6, bar.width, 26), Spaced("UNKNOWN"), cap);
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
                    GUI.Label(new Rect(bar.x, bar.y + 32, bar.width, 24), "incoming call", cap);
                }
                else if (phoneUi == 2)
                {
                    int secs = Mathf.FloorToInt(t);
                    GUI.Label(new Rect(bar.x, bar.y + 6, bar.width, 26), Spaced("UNKNOWN"), cap);
                    cap.normal.textColor = new Color(0.4f, 0.9f, 0.5f);
                    GUI.Label(new Rect(bar.x, bar.y + 32, bar.width, 24), $"{secs / 60:00}:{secs % 60:00}", cap);
                }
                else
                {
                    cap.normal.textColor = new Color(1f, 0.3f, 0.25f);
                    GUI.Label(new Rect(bar.x, bar.y + 18, bar.width, 28), Spaced("CALL ENDED"), cap);
                }
            }
            cap.alignment = old;
        }

        /// <summary>The closing card, over black: chapter complete, and the memory it gave back.</summary>
        void EndingGUI(float w, float h)
        {
            PhoneGUI(w, h);
            if (cardAt < 0f || phase == Phase.End) return;
            float t = Time.unscaledTime - cardAt;
            float a = Mathf.Clamp01(t / 1.0f);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
            float ta = Mathf.Clamp01((t - 0.8f) / 0.8f);
            big.normal.textColor = new Color(1f, 1f, 1f, ta);
            GUI.Label(new Rect(0, h * 0.36f, w, 90), Spaced("CHAPTER 1 COMPLETE"), big);
            Fill(new Rect(w * 0.5f - 140 * ta, h * 0.36f + 86, 280 * ta, 3), new Color(0.8f, 0.06f, 0.07f, ta));
            float ca = Mathf.Clamp01((t - 1.8f) / 0.8f);
            cap.alignment = TextAnchor.MiddleCenter;
            cap.normal.textColor = new Color(1f, 1f, 1f, 0.75f * ca);
            GUI.Label(new Rect(0, h * 0.36f + 100, w, 30), Spaced("THE HITMAN") + "     ·     MEMORY RESTORED", cap);
            sub.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01((t - 2.8f) / 0.8f));
            GUI.Label(new Rect(0, h * 0.36f + 140, w, 40), "\u201C" + cardLine + "\u201D", sub);
            cap.alignment = TextAnchor.MiddleLeft;
        }
    }
}
