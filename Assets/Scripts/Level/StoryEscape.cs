using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE ESCAPE (chapter 1, between the breach and the fight). The shutter is up and
    /// three men walk in. He does not know them, does not know he can fight: he backs off, turns
    /// and runs. The heavy chain lying across the floor takes his foot and he goes down full
    /// length into the puddle, in slow motion. On the ground, ears ringing, he hears them coming,
    /// laughing. Lightning: in the flash, a glint on the floor ahead, his knife. He drags himself
    /// to it through the water and his fingers close on the handle, and the past breaks in: four
    /// and a half seconds of fights he does not remember, in black and white, the world and the
    /// men as the fight mode draws them, cut in a frame at a time (rooftops, corridors, rain,
    /// glass, a white moon) over a racing heart. Thunder, then silence. He gets
    /// up with the knife, turns to face them, and they stop. "My hands remember."
    ///
    /// Runs once; a retry after a defeat starts at the door with the knife already in his hand.</summary>
    public partial class StoryIntro
    {
        public static bool EscapeSeen;

        float e, prevE;                       // the escape clock (real seconds)
        float tripAt = -1f, impactAt = -1f;   // escape-clock times of the trip and of hitting the floor
        float runX, tripX, landX, crawlFrom, crawlTo;
        Transform chain, floorKnife, knifeGlint;
        Transform[] links;
        Vector3[] linkBase;
        Vector3[] menFrom = new Vector3[3];
        float[] menStop = new float[3];
        Texture2D[] flashTex;
        int[] flashOrder;
        string heroAnim = "";

        // after the fall, on the ground (D = seconds since he hit the floor)
        const float D_Glint = 1.9f, D_Crawl = 3.4f, D_Reach = 5.6f, D_Grip = 6.07f, D_Flash = 7.0f, D_FlashLen = 4.95f,
                    D_Rise = D_Flash + D_FlashLen, D_Turn = D_Rise + 1.55f, D_Release = D_Rise + 3.3f;
        const float TripSlow = 0.45f, TripFall = 0.89f, TripSlide = 1.25f;

        // the montage: (seconds, what): 0 white, -1 black, -2 a cut back to him (the real world), 1..8 a memory
        static readonly float[] CutLen = { 0.08f, 0.5f, 0.05f, 0.22f, 0.42f, 0.06f, 0.38f, 0.3f, 0.05f, 0.18f, 0.3f, 0.26f, 0.06f, 0.24f, 0.5f, 0.12f, 0.13f, 0.11f, 0.1f, 0.1f, 0.09f, 0.3f, 0.4f };
        static readonly int[] CutKind = { 0, 1, -1, -2, 2, 0, 3, 4, -1, -2, 5, 6, 0, 7, 8, -2, 2, 1, 6, 3, 7, 0, -1 };
        static readonly float[] Beats = { 0f, 0.55f, 1.05f, 1.5f, 1.9f, 2.25f, 2.55f, 2.82f, 3.07f, 3.3f, 3.5f, 3.68f, 3.85f, 4.0f, 4.14f, 4.27f };

        void FindEscapeProps()
        {
            chain = Find("Chain");
            floorKnife = Find("FloorKnife");
            knifeGlint = Find("KnifeGlint");
            if (chain != null)
            {
                links = new Transform[chain.childCount];
                linkBase = new Vector3[chain.childCount];
                for (int i = 0; i < links.Length; i++) { links[i] = chain.GetChild(i); linkBase[i] = links[i].localPosition; }
            }
            flashTex = new Texture2D[9];
            for (int i = 1; i <= 8; i++) flashTex[i] = Resources.Load<Texture2D>("Story/Flash/flash_" + i);
        }

        bool CanEscape => !EscapeSeen && chain != null && floorKnife != null;
        bool AtE(float time) => prevE < time && e >= time;
        bool AtD(float d) => impactAt >= 0f && prevE - impactAt < d && e - impactAt >= d;
        float D => impactAt >= 0f ? e - impactAt : -1f;

        void HeroAnim(string clip, float fade, float speed = 1f, bool restart = true)
        {
            if (heroAnim == clip && !restart) return;
            heroAnim = clip;
            if (hero.body.Has(clip)) hero.body.Play(clip, fade, true, speed);
        }

        void BeginEscape()
        {
            phase = Phase.Escape;
            e = prevE = 0f;
            tripAt = impactAt = -1f;
            hero.CinePuppet();
            runX = hero.transform.position.x;
            tripX = chain.position.x + 0.25f;
            // a new order of memories every time (the quiet one stays where it is)
            var r = new System.Random(System.Environment.TickCount);
            flashOrder = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            for (int i = 7; i > 1; i--) { int j = r.Next(1, i + 1); (flashOrder[i], flashOrder[j]) = (flashOrder[j], flashOrder[i]); }
            for (int i = 0; i < men.Length; i++)
                if (men[i] != null)
                {
                    men[i].Puppet();
                    men[i].SetFacing(-1);
                    menFrom[i] = men[i].transform.position;
                    men[i].PuppetPlay("walk", 0.2f, 0.9f);
                }
            HeroAnim("idle_loose", 0.2f);
        }

        void UpdateEscape()
        {
            float dt = Time.unscaledDeltaTime;
            var hp = hero.transform.position;
            float y = home.y;

            // ---------------------------------------------------------------- he runs
            if (tripAt < 0f)
            {
                if (e < 0.8f)
                {
                    // a step back, then another: he does not want this
                    hero.SetFacing(1);
                    runX -= 0.6f * dt;
                    if (AtE(0.1f)) { Say("YOU", "...No.", 0.9f); Sound.Play(Sfx.Breath, 0.8f); }
                }
                else
                {
                    if (AtE(0.8f))
                    {
                        hero.SetFacing(-1);
                        HeroAnim("run", 0.1f, 1.35f);
                        Sound.Play(Sfx.Cloth, 0.8f);
                        Sound.Play(Sfx.HeroEffort, 0.6f, null, 1.1f);
                        foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("run", 0.15f, 1.1f);
                    }
                    if (AtE(1.0f)) Say("THUG", "Get him!", 1.1f);
                    float v = Mathf.Lerp(1.5f, 5.2f, Ease(0.8f, 1.2f, e));
                    runX -= v * dt;
                    if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.27f; Sound.Play(Sfx.Footstep, 0.9f, hp, 1.05f); }
                    if (AtE(1.3f)) Sound.Play(Sfx.Breath, 0.9f, null, 1.15f);
                    if (runX <= tripX) { tripAt = e; tripX = runX; Trip(); }
                }
                hero.CinePose(new Vector3(runX, y, 0f));
                MoveMen(dt, e < 0.8f ? 0.9f : 2.5f, hp.x + 2.2f);
                if (cam != null)
                {
                    if (e < 0.8f) cam.Scripted(hp + new Vector3(-1.6f, 1.0f, -2.6f) + Sway(0.03f, 0.6f, 11f), hp + new Vector3(1.4f, 1.0f, 0.6f), 1f, 40f);
                    else cam.Scripted(new Vector3(hp.x - 1.4f, 0.95f, -3.3f) + Sway(0.06f, 1.4f, 12f), new Vector3(hp.x - 0.4f, 0.9f, 0f), 1f, 44f);
                }
                return;
            }

            // ---------------------------------------------------------------- he goes down
            if (impactAt < 0f)
            {
                float k = Mathf.Clamp01((e - tripAt) / TripFall);
                float slide = 1f - (1f - k) * (1f - k);
                hero.CinePose(new Vector3(tripX - TripSlide * slide, y, 0f));
                ShakeChain(e - tripAt);
                MoveMen(dt, 2.2f, hp.x + 2.6f);
                if (cam != null) cam.Scripted(new Vector3(tripX - 2.7f, 0.24f, -1.3f), new Vector3(tripX - 0.9f, 0.35f, 0.05f), 1f, 40f);
                if (k >= 1f) { impactAt = e; Impact(); }
                return;
            }

            float d = D;
            ShakeChain(e - tripAt);
            var head = HeadC();
            landX = tripX - TripSlide;

            if (AtD(0.8f)) HeroAnim("prone", 0.4f);
            if (AtD(0.1f)) Sound.Play(Sfx.Tinnitus, 0.55f);
            if (AtD(0.5f)) Lightning(0.25f, 0.45f, 0.7f);
            if (AtD(0.6f)) Sound.Play(Sfx.Heartbeat, 0.7f);
            if (AtD(1.2f)) Sound.Play(Sfx.BruteLaugh, 0.55f, men[0] != null ? men[0].transform.position : hp, 0.92f);
            if (AtD(1.4f)) Sound.Play(Sfx.Heartbeat, 0.8f);
            if (d < D_Glint) StoryCameraFX.Set(Mathf.Lerp(0.6f, 0f, d / D_Glint), 0f);

            // the flash: his knife, there on the floor
            if (AtD(D_Glint))
            {
                Lightning(1f, 0.9f, 0.25f);
                Sound.Play(Sfx.Glint, 1f);
                Sound.Play(Sfx.Heartbeat, 1f);
            }
            if (knifeGlint != null)
            {
                float g = d >= D_Glint && d < D_Grip ? Mathf.Exp(-(d - D_Glint) * 2.2f) + 0.15f * Mathf.Max(0f, Mathf.Sin(d * 3f)) : 0f;
                var gr = knifeGlint.GetComponent<Renderer>();
                if (gr != null) gr.enabled = g > 0.02f;
                knifeGlint.localScale = Vector3.one * (0.12f + 0.5f * g);
                if (cam != null) knifeGlint.rotation = Quaternion.LookRotation(knifeGlint.position - cam.transform.position);
            }

            // the crawl: on his forearms through the water, pull by pull
            if (AtD(D_Crawl))
            {
                HeroAnim("crawl", 0.25f, 0.95f);
                crawlFrom = hero.transform.position.x;
                var handle = floorKnife.TransformPoint(new Vector3(0f, 0f, -0.07f));
                crawlTo = handle.x + 0.8f;
            }
            if (d >= D_Crawl && d < D_Reach)
            {
                float u = Mathf.InverseLerp(D_Crawl, D_Reach, d);
                float pulls = (D_Reach - D_Crawl) / 0.56f;
                float p = u - Mathf.Sin(2f * Mathf.PI * u * pulls) / (2f * Mathf.PI * pulls);
                hero.CinePose(new Vector3(Mathf.Lerp(crawlFrom, crawlTo, p), y, 0f));
                float ph = Mathf.Repeat((d - D_Crawl) / 0.56f, 1f), pph = Mathf.Repeat((prevE - impactAt - D_Crawl) / 0.56f, 1f);
                if (ph < pph) { Sound.Play(Sfx.Crawl, 0.9f, hp); if (Random.value < 0.5f) Sound.Play(Sfx.HeroEffort, 0.35f, null, 0.8f); }
            }
            if (AtD(D_Crawl + 0.4f)) Say("THUG", "Look at him. Crawling.", 1.6f);
            if (AtD(D_Crawl + 1.2f)) Sound.Play(Sfx.Breath, 0.8f, null, 0.95f);

            // the hand closes on it
            if (AtD(D_Reach)) HeroAnim("reach_grab", 0.12f);
            if (AtD(D_Grip))
            {
                Sound.Play(Sfx.KnifeGrab, 1f);
                floorKnife.gameObject.SetActive(false);
                hero.GiveWeapon(FightStyle.Knife, 4);
                Buzz();
            }
            if (AtD(D_Grip + 0.45f)) HeroAnim("prone_knife", 0.25f);
            if (AtD(D_Grip + 0.6f)) { Sound.Play(Sfx.Heartbeat, 1f); Sound.Play(Sfx.Whispers, 0.7f); }

            // the past, cut in (drawn in OnGUI); the sound of it here
            if (d >= D_Flash && d < D_Flash + D_FlashLen) FlashSounds(d - D_Flash, prevE - impactAt - D_Flash);

            // up, with the knife; he turns to face them
            if (AtD(D_Rise))
            {
                HeroAnim("rise_knife", 0.1f, 0.85f);
                Sound.Play(Sfx.Cloth, 0.9f);
                Sound.Play(Sfx.HeroEffort, 0.7f, null, 0.75f);
                Sound.Play(Sfx.Drone, 0.9f);
                foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle", 0.3f);
            }
            if (AtD(D_Rise + 0.5f)) { Lightning(1f, 1f, 0.05f); Sound.Play(Sfx.CineHit, 0.7f); if (cam != null) cam.Shake(0.12f, 0.3f); }
            if (AtD(D_Turn)) { hero.SetFacing(1); Sound.Play(Sfx.Cloth, 0.7f, null, 1.2f); }
            if (AtD(D_Turn + 0.35f)) Say("YOU", "...My hands remember.", 2.2f);
            if (AtD(D_Turn + 0.9f)) Sound.Play(Sfx.Riser, 0.5f);

            // the men: walk up, stop short of him, and step back when he stands
            if (d < D_Rise) MoveMen(dt, 1.0f, landX + 1.4f);
            else
            {
                float back = Ease(D_Rise + 0.5f, D_Rise + 1.4f, d) * 0.35f;
                for (int i = 0; i < men.Length; i++)
                    if (men[i] != null)
                    {
                        var p = men[i].transform.position;
                        if (AtD(D_Rise + 0.5f)) menStop[i] = p.x;
                        if (d >= D_Rise + 0.5f) men[i].PuppetPose(new Vector3(menStop[i] + back, p.y, Mathf.MoveTowards(p.z, 0f, dt * 0.5f)));
                    }
            }

            EscapeCamera(d, head, hp);

            if (AtD(D_Release))
            {
                EscapeSeen = true;
                StoryCameraFX.Set(0f, 0f);
                BeginTraining();                              // INSTINCT: the training ground (StoryTraining)
                return;
            }
            if (d > D_Release)
            {
                float k = Ease(D_Release, D_Release + 0.7f, d);
                barsPx = Mathf.Lerp(70f, 0f, k);
                if (k >= 1f)
                {
                    if (cam != null) { cam.Scripted(Vector3.zero, Vector3.zero, 0f); cam.ScriptRoll = 0f; }
                    barsPx = 0f;
                    phase = Phase.Fight;
                }
            }
            else barsPx = Mathf.MoveTowards(barsPx, 70f, dt * 120f);
        }

        void Trip()
        {
            HeroAnim("trip", 0.04f, TripSlow);
            Sound.Play(Sfx.Chain, 1f, chain.position);
            Sound.Play(Sfx.HeroHurt, 0.7f, null, 0.95f);
            Sound.Play(Sfx.SlowmoIn, 0.6f);
            Buzz();
        }

        void Impact()
        {
            var head = HeadC();
            Sound.Play(Sfx.BodySplash, 1f, head);
            Sound.Play(Sfx.SlowmoOut, 0.5f);
            Sound.Play(Sfx.HeroHurt, 0.6f, null, 0.8f);
            if (cam != null) cam.Shake(0.25f, 0.3f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(head + new Vector3(-0.2f, -0.1f, 0f), Vector3.up, 14, new Color(0.55f, 0.62f, 0.76f));
            Buzz();
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("walk", 0.3f, 0.75f);
        }

        /// <summary>Walk (or run) the men up behind him, each stopping at its own distance.</summary>
        void MoveMen(float dt, float speed, float stopX)
        {
            for (int i = 0; i < men.Length; i++)
            {
                if (men[i] == null) continue;
                var p = men[i].transform.position;
                float target = stopX + i * 0.75f;
                float nx = p.x > target ? Mathf.MoveTowards(p.x, target, speed * dt * (1f - 0.12f * i)) : p.x;
                float nz = Mathf.MoveTowards(p.z, (i - 1) * 0.35f, dt * 1.2f);
                men[i].PuppetPose(new Vector3(nx, p.y, nz));
                bool moving = nx < p.x - 0.0001f;
                if (!moving && D >= 0f && D < D_Rise && Mathf.Abs(p.x - target) < 0.05f && men[i].body.CurrentName != "idle") men[i].PuppetPlay("idle", 0.3f);
            }
        }

        /// <summary>The chain jumps where his foot caught it and drags a little way after him.</summary>
        void ShakeChain(float since)
        {
            if (links == null) return;
            float decay = Mathf.Exp(-since * 3.5f);
            float drag = Ease(0f, 0.35f, since) * 0.22f;
            for (int i = 0; i < links.Length; i++)
            {
                var b0 = linkBase[i];
                float near = Mathf.Exp(-(b0.z * b0.z) / 0.25f);
                var o = new Vector3(Mathf.Sin(i * 1.7f + since * 38f) * 0.03f, Mathf.Abs(Mathf.Sin(i * 2.3f + since * 31f)) * 0.06f, 0f) * (decay * near);
                links[i].localPosition = b0 + o + new Vector3(-drag * near, 0f, 0f);
            }
        }

        void EscapeCamera(float d, Vector3 head, Vector3 hp)
        {
            if (cam == null) return;
            Vector3 pos, look; float fov;
            var knife = floorKnife != null ? floorKnife.position : head + Vector3.left;
            if (d < D_Glint)
            {
                // on the floor with him, creeping in on his head in the water
                float k = Ease(0f, D_Glint, d);
                pos = Vector3.Lerp(new Vector3(tripX - 2.7f, 0.24f, -1.3f), head + new Vector3(-0.95f, -0.02f, -0.75f), k) + Sway(0.01f, 0.5f, 13f);
                look = Vector3.Lerp(new Vector3(tripX - 0.9f, 0.35f, 0.05f), head + new Vector3(0f, -0.05f, 0f), k);
                fov = 40f;
                cam.ScriptRoll = 6f;
            }
            else if (d < D_Crawl)
            {
                // the knife in the foreground, his face beyond it
                float k = Ease(D_Glint, D_Crawl, d);
                pos = knife + new Vector3(-0.5f, 0.12f, -0.4f) + Sway(0.005f, 0.4f, 14f);
                look = Vector3.Lerp(knife + new Vector3(0.1f, 0.02f, 0f), head, 0.25f + 0.55f * k);
                fov = 36f;
                cam.ScriptRoll = 0f;
            }
            else if (d < D_Reach)
            {
                // low beside him as he drags himself on, the men's legs coming in behind
                pos = new Vector3(head.x - 0.6f, 0.38f, -1.6f) + Sway(0.02f, 0.8f, 15f);
                look = new Vector3(head.x + 0.1f, 0.25f, 0f);
                fov = 42f;
                cam.ScriptRoll = -4f;
            }
            else if (d < D_Grip + 0.6f)
            {
                // the hand on the handle
                var handle = floorKnife != null && floorKnife.gameObject.activeSelf ? floorKnife.TransformPoint(new Vector3(0f, 0f, -0.07f)) : head + new Vector3(-0.55f, -0.1f, 0f);
                pos = handle + new Vector3(0.12f, 0.26f, -0.42f);
                look = handle;
                fov = 30f;
                cam.ScriptRoll = 0f;
            }
            else if (d < D_Rise)
            {
                // his face, very close; the memories cut over it
                pos = head + new Vector3(-0.8f, 0.03f, -0.32f) + Sway(0.008f, 0.9f, 16f);
                look = head;
                fov = 30f;
                cam.ScriptRoll = 3f;
            }
            else
            {
                // up: low and wide from in front, the men beyond him, pushing in
                float k = Ease(D_Rise, D_Release, d);
                pos = new Vector3(hp.x - 2.7f + 0.6f * k, 0.32f + 0.1f * k, -2.5f + 0.5f * k) + Sway(0.02f, 0.4f, 17f);
                look = new Vector3(hp.x + 0.5f, 1.0f, 0.1f);
                fov = 42f;
                cam.ScriptRoll = Mathf.Lerp(-3f, 0f, k);
            }
            float w = d > D_Release ? 1f - Ease(D_Release, D_Release + 0.7f, d) : 1f;
            cam.Scripted(pos, look, w, fov);
        }

        // ---------------------------------------------------------------- the montage

        int CutAt(float f, out float start)
        {
            start = 0f;
            for (int i = 0; i < CutLen.Length; i++)
            {
                if (f < start + CutLen[i]) return i;
                start += CutLen[i];
            }
            return -1;
        }

        int MemoryAt(int cut) { int k = CutKind[cut]; return k >= 1 ? flashOrder[k] : k; }

        void FlashSounds(float f, float pf)
        {
            foreach (var bt in Beats) if (pf < bt && f >= bt) Sound.Play(Sfx.Heartbeat, 1f, null, 1f + bt * 0.06f);
            if (pf < 0.1f && f >= 0.1f) Sound.Play(Sfx.Whispers, 0.8f, null, 0.85f);
            int c = CutAt(f, out float s0), pc = CutAt(Mathf.Max(0f, pf), out _);
            if (c < 0 || (c == pc && pf >= 0f)) return;
            int kind = MemoryAt(c);
            if (kind == -2) { Sound.Play(Sfx.Static, 0.6f, null, 1.1f); Glitch(0.9f, CutLen[c]); return; }
            if (kind == 0 && c == CutLen.Length - 2) { Lightning(1f, 1f, 0f); Sound.Play(Sfx.CineMassive, 0.8f); return; }
            if (kind < 1) return;
            Sound.Play(Sfx.FlashCut, 0.8f);
            switch (kind)
            {
                case 1: Sound.Play(Sfx.PunchHeavy, 0.8f); Sound.Play(Sfx.Thunder, 0.35f, null, 1.1f); break;
                case 2: Sound.Play(Sfx.GunBody, 0.9f); Sound.Play(Sfx.GunTail, 0.7f); break;
                case 3: Sound.Play(Sfx.Knife, 0.9f); Sound.Play(Sfx.Splat, 0.5f, null, 1.2f); break;
                case 4: Sound.Play(Sfx.Kick, 0.9f); Sound.Play(Sfx.BodyFall, 0.6f); break;
                case 5: Sound.Play(Sfx.GunBody, 0.7f, null, 0.8f); break;
                case 6: Sound.Play(Sfx.Glass, 0.9f); Sound.Play(Sfx.BodyFall, 0.5f); break;
                case 7: Sound.Play(Sfx.SwordSwing, 0.9f); Sound.Play(Sfx.SwordHit, 0.8f); break;
                case 8: if (f < 3.7f) Sound.Play(Sfx.Piano, 0.45f); break;
            }
        }

        /// <summary>The memories over the screen: each one a frame held for a beat, pushed in,
        /// shaken, split at the edges, torn, grained; white and black frames between.</summary>
        void EscapeGUI(float w, float h)
        {
            float d = D;
            if (d < D_Flash || d >= D_Flash + D_FlashLen) return;
            float f = d - D_Flash;
            int c = CutAt(f, out float s0);
            if (c < 0) return;
            int kind = MemoryAt(c);
            float k = (f - s0) / CutLen[c];
            var full = new Rect(0, 0, w, h);
            if (kind == 0) { Fill(full, Color.white); return; }
            if (kind == -1) { Fill(full, Color.black); return; }
            if (kind == -2)
            {
                // back on him for a breath: bled red, crushed
                Fill(full, new Color(0.5f, 0.02f, 0.02f, 0.28f));
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(-w * 0.1f, -h * 0.1f, w * 1.2f, h * 1.2f), vig);
                GUI.color = Color.white;
                return;
            }
            var tex = flashTex != null ? flashTex[kind] : null;
            if (tex == null) { Fill(full, Color.black); return; }
            var old = GUI.color;
            Fill(full, Color.black);
            // cover the screen; push in (or out) over the cut, a little shake on hard cuts
            float asp = tex.width / (float)tex.height;
            float rw = Mathf.Max(w, h * asp), rh = rw / asp;
            float sc = (c % 2 == 0) ? Mathf.Lerp(1.02f, 1.1f, k) : Mathf.Lerp(1.1f, 1.02f, k);
            float jx = CutLen[c] < 0.2f ? Random.Range(-7f, 7f) : Random.Range(-2f, 2f);
            float jy = CutLen[c] < 0.2f ? Random.Range(-5f, 5f) : Random.Range(-1.5f, 1.5f);
            var r = new Rect(w * 0.5f - rw * sc * 0.5f + jx, h * 0.5f - rh * sc * 0.5f + jy, rw * sc, rh * sc);
            // a double image at the edges, as memory blurs (black and white: no colour comes back)
            float split = 3f + 6f * (1f - k);
            GUI.color = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(r.x + split, r.y, r.width, r.height), tex);
            GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(r.x - split, r.y, r.width, r.height), tex);
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(r, tex);
            // torn: a couple of strips slipping sideways
            if (Random.value < 0.45f)
                for (int i = 0; i < 2; i++)
                {
                    float sy = Random.Range(0f, h), sh = Random.Range(8f, 40f), sx = Random.Range(-40f, 40f);
                    GUI.BeginGroup(new Rect(0, sy, w, sh));
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(r.x + sx, r.y - sy, r.width, r.height), tex);
                    GUI.EndGroup();
                }
            // grain, the vignette, and a white kick on the first frames of the cut
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            float ox = Random.value, oy = Random.value;
            GUI.DrawTextureWithTexCoords(full, noise, new Rect(ox, oy, w / 160f, h / 160f));
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(-w * 0.08f, -h * 0.08f, w * 1.16f, h * 1.16f), vig);
            if (f - s0 < 0.035f) Fill(full, new Color(1f, 1f, 1f, 0.45f));
            GUI.color = old;
        }
    }
}
