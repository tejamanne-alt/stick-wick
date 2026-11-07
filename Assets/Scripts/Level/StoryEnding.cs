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

            Say("YOU", "Why are you trying to kill me?", 2.4f);
            Sound.Play(Sfx.Heartbeat, 0.6f);
            yield return Hold(2.4f, wide);
            if (boss != null) Sound.Play(Sfx.BruteLaugh, 0.55f, boss.Chest, 0.8f);
            Say("THE FOREMAN", "You... really don't remember.", 2.4f);
            yield return Hold(2.4f, close);
            Say("YOU", "Tell me who I am. WHO SENT YOU?", 2.4f);
            Sound.Play(Sfx.Tinnitus, 0.25f);
            yield return Hold(2.4f, close);
            Say("THE FOREMAN", "Nobody had to send us. Five million reasons, friend.", 3.0f);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Hold(3.0f, close);

            // ---------------------------------------------------------------- 4a the draw: his hand, going under the jacket (only we see it)
            heartBpm = 62f; nextBeat = Time.unscaledTime;
            if (boss != null) boss.PuppetPlay("lie_gun", 0.9f, 0.35f);
            Sound.Play(Sfx.GunDraw, 1f);
            BuildGun();
            float td = 0f;
            while (td < 1.4f)
            {
                td += Time.unscaledDeltaTime;
                var hand = HandOf(boss, landX);
                PoseGun(hand, Vector3.Slerp(new Vector3(0.3f, -1f, 0f), Vector3.right, Mathf.SmoothStep(0f, 1f, td / 1.4f)));
                Heart(); Rain();
                EndShot(hand + new Vector3(-0.1f, 0.1f, -0.85f), hand, 24f);
                yield return null;
            }

            // ---------------------------------------------------------------- 4b his face: he has not seen it
            Sound.Play(Sfx.Breath, 0.5f, null, 0.95f);
            yield return Hold(1.1f, () =>
            {
                var hand = HandOf(boss, landX);
                PoseGun(hand, Vector3.right + Vector3.up * 0.2f);
                Heart(); Rain();
                EndShot(HeadC() + new Vector3(0.05f, 0.02f, -0.75f), HeadC(), 28f);
            });

            // ---------------------------------------------------------------- 4c the gun comes up into his face
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.45f, 2.6f, 0.2f, 0.3f);
            heartBpm = 95f;
            float tg = 0f;
            while (tg < 1.5f)
            {
                tg += Time.unscaledDeltaTime;
                var hand = HandOf(boss, landX);
                PoseGun(hand, Vector3.Slerp(Vector3.right + Vector3.up * 0.2f, HeadC() - hand, Mathf.SmoothStep(0f, 1f, tg / 1.1f)));
                Heart();
                wide();
                yield return null;
            }

            // ---------------------------------------------------------------- 4d the hammer: the gun in profile against the light
            Sound.Play(Sfx.GunCock, 1f);
            Say("THE FOREMAN", "Nothing personal.", 1.9f);
            heartBpm = 120f;
            yield return Hold(1.6f, () =>
            {
                var hand = HandOf(boss, landX);
                PoseGun(hand, HeadC() - hand);
                Heart(); Rain();
                var gp = gun != null ? gun.position : hand;
                EndShot(gp + new Vector3(-0.05f, 0.04f, -0.62f), gp + (HeadC() - gp) * 0.35f, 24f);
            });

            // ---------------------------------------------------------------- 4e his eyes: the world closes in to the barrel
            Sound.Ambience(0.3f, 0.04f, 900f);
            heartBpm = 150f;
            yield return Hold(0.9f, () =>
            {
                var hand = HandOf(boss, landX);
                PoseGun(hand, HeadC() - hand);
                Heart(); Rain();
                EndShot(HeadC() + new Vector3(0.18f, -0.02f, -0.5f), HeadC(), 22f);
            });

            // ---------------------------------------------------------------- 5a instinct: he grabs the wrist
            if (hero.body.Has("kneel_grip")) hero.body.Play("kneel_grip", 0.04f, true, 1.6f);
            Sound.Play(Sfx.PunchLight, 1f);
            Sound.Play(Sfx.Cloth, 1f, null, 1.2f);
            Sound.Play(Sfx.HeroEffort, 1f);
            Sound.Play(Sfx.Struggle, 1f);
            heartBpm = 175f;
            float tgrab = 0f;
            while (tgrab < 0.35f)
            {
                tgrab += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, tgrab / 0.35f);
                var hand = HandOf(boss, landX);
                PoseGun(Vector3.Lerp(hand, Vector3.Lerp(hand, HeroHand(), 0.45f), k), HeadC() - hand);
                Heart(); Rain();
                EndShot(Vector3.Lerp(HeadC() + new Vector3(0.18f, -0.02f, -0.5f), new Vector3(mx, 0.32f, -2.4f), k), Vector3.Lerp(HeadC(), new Vector3(mx, 0.55f, 0f), k), Mathf.Lerp(22f, 30f, k));
                yield return null;
            }

            // ---------------------------------------------------------------- 5b the struggle: fast cuts, the barrel shaking between their faces
            float ts = 0f;
            int lastCut = -1;
            float gruntAt = 0.3f;
            while (ts < 2.6f)
            {
                ts += Time.unscaledDeltaTime;
                var hand = HandOf(boss, landX);
                var at = Vector3.Lerp(hand, HeroHand(), 0.45f) + new Vector3(Random.Range(-0.03f, 0.03f), Random.Range(-0.025f, 0.025f), 0f) * (1f + ts);
                var toHero = HeadC() - at;
                var toBoss = (boss != null ? boss.Chest : new Vector3(landX, 0.4f, 0f)) - at;
                PoseGun(at, Vector3.Slerp(toHero, toBoss, TurnCurve(ts)) + Random.insideUnitSphere * 0.1f);
                var shake = new Vector3(Random.Range(-0.035f, 0.035f), Random.Range(-0.025f, 0.025f), 0f);
                int cut = Mathf.FloorToInt(ts / 0.55f) % 4;
                if (cut != lastCut) { lastCut = cut; Sound.Play(Sfx.Cloth, 0.7f, null, Random.Range(0.9f, 1.2f)); }
                var gp = gun != null ? gun.position : at;
                switch (cut)
                {
                    case 0: EndShot(new Vector3(mx, 0.32f, -2.4f) + shake, new Vector3(mx, 0.55f, 0f) + shake * 0.5f, 30f); break;   // the two of them
                    case 1: EndShot(gp + new Vector3(0.02f, 0.06f, -0.55f) + shake * 0.6f, gp, 22f); break;                          // the gun between them
                    case 2: EndShot(BossHead(boss, landX) + new Vector3(-0.1f, 0.1f, -0.7f) + shake, BossHead(boss, landX), 24f); break; // his teeth bared
                    default: EndShot(HeadC() + new Vector3(0.15f, -0.05f, -0.6f) + shake, HeadC(), 24f); break;                     // his
                }
                if (ts > gruntAt) { gruntAt += Random.Range(0.45f, 0.75f); Sound.Play(Random.value < 0.5f ? Sfx.HeroEffort : Sfx.EnemyEffort, 0.8f, null, Random.Range(0.85f, 1f)); }
                if (ts > 1.3f && ts - Time.unscaledDeltaTime <= 1.3f) Sound.Play(Sfx.Struggle, 0.9f);
                backGlow = 1f + Mathf.Sin(ts * 23f) * 0.08f;
                Heart(); Rain();
                yield return null;
            }

            // ---------------------------------------------------------------- 5c the turn: the barrel on his chest; the heart stops; no sound
            heartBpm = 0f;
            Sound.Ambience(0.05f, 0.0f, 400f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.12f, 1.4f, 0.05f, 0.3f);
            Sound.Play(Sfx.Breath, 0.35f, null, 0.8f);
            yield return Hold(1.0f, () =>
            {
                var hand = HandOf(boss, landX);
                var at = Vector3.Lerp(hand, HeroHand(), 0.45f);
                var toBoss = (boss != null ? boss.Chest : new Vector3(landX, 0.4f, 0f)) - at;
                PoseGun(at, toBoss);
                Rain();
                var gp = gun != null ? gun.position : at;
                EndShot(gp + new Vector3(0.0f, 0.05f, -0.75f), gp + toBoss * 0.4f, 26f);
            });

            // ---------------------------------------------------------------- 6 BANG
            flashFrames = 3;
            backGlow = 2.5f;
            MuzzleFlash();
            Sound.Play(Sfx.GunshotIndoor, 1f);
            Sound.Play(Sfx.GunBody, 0.5f);
            Buzz();
            if (cam != null) cam.Shake(0.35f, 0.3f);
            Sound.Ambience(0.15f, 0.03f, 500f);                            // the world goes away
            Sound.Play(Sfx.Tinnitus, 1f);
            Sound.PlayLater(Sfx.ShellCasing, 0.7f, 0.8f);                  // the brass, ringing on the concrete, through the ringing
            if (InkFx.Instance != null && boss != null) InkFx.Instance.Splash(boss.Chest, new Vector3(1f, 0.4f, 0f), 30, boss.InkColor);
            yield return Hold(0.3f, wide);

            // ---------------------------------------------------------------- 7 he dies: slowly, in the smoke
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.2f, 2.4f, 0.02f, 0.6f);
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.5f);
            if (boss != null) boss.PuppetPlay("lie", 1.6f);
            float tdie = 0f;
            bool dropped = false, breathed = false, stirred = false;
            while (tdie < 4.2f)
            {
                tdie += Time.unscaledDeltaTime;
                if (!breathed && tdie > 0.6f) { breathed = true; Sound.Play(Sfx.DeathBreath, 0.9f); }
                if (!dropped && tdie > 1.3f)
                {
                    dropped = true;
                    DropGun();
                    Sound.Play(Sfx.GunDropWater, 0.9f);
                }
                else if (!dropped && gun != null)
                {
                    var hand = HandOf(boss, landX);
                    PoseGun(hand, Vector3.Slerp(Vector3.right, new Vector3(0.4f, -1f, 0f), tdie / 1.3f));
                }
                // the ink spreading out from under him into the rain water
                if (boss != null && InkFx.Instance != null && Random.value < 0.08f && tdie < 3f)
                    InkFx.Instance.Stain(boss.transform.position + Vector3.right * Random.Range(-0.5f, 0.4f), boss.InkColor, 0.6f + tdie * 0.35f, false);
                if (!stirred && tdie > 2.4f) { stirred = true; Sound.Ambience(0.7f, 0.12f, 2500f); }   // the rain comes back, slowly
                backGlow = Mathf.MoveTowards(backGlow, 1f, Time.unscaledDeltaTime * 1.5f);
                Rain();
                EndShot(new Vector3(mx, 0.28f, -3.6f + tdie * 0.18f), new Vector3(mx, 0.55f, 0f), 34f - tdie * 1.2f);
                yield return null;
            }

            // ---------------------------------------------------------------- 8 no... no... no...
            Sound.Ambience(1.2f, 0.25f, 22000f);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.8f);
            Sound.Play(Sfx.Cloth, 0.7f);
            yield return Hold(0.9f, wide);
            if (hero.body.Has("look_hands")) hero.body.Play("look_hands", 0.3f, true, 0.7f);
            headYawOn = true;
            Say("YOU", "No... no... no...", 2.6f);
            Sound.Play(Sfx.Breath, 0.9f, null, 0.9f);
            float tn = 0f;
            float stand = hero.transform.position.x;
            while (tn < 4.2f)
            {
                tn += Time.unscaledDeltaTime;
                headYaw = Mathf.Sin(tn * 3.2f) * 26f * Mathf.Clamp01(tn / 0.6f) * (1f - Mathf.Clamp01((tn - 3.6f) / 0.6f));
                hero.CinePose(new Vector3(stand - 0.5f * Mathf.SmoothStep(0f, 1f, tn / 3f), 0f, 0f));      // backing away
                if (tn > 2.6f && tn - Time.unscaledDeltaTime <= 2.6f) Say("YOU", "That wasn't... I didn't...", 1.8f);
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
            Say("YOU", "Who is this?", 1.8f);
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

        // ---------------------------------------------------------------- the heartbeat, the turn, the flash, the smoke

        float heartBpm, nextBeat;

        /// <summary>His heart, louder and faster as the gun comes up (0 bpm: it stops).</summary>
        void Heart()
        {
            if (heartBpm <= 0f || Time.unscaledTime < nextBeat) return;
            Sound.Play(Sfx.Heartbeat, Mathf.Clamp(0.35f + heartBpm / 260f, 0.35f, 1f), null, 1f + (heartBpm - 60f) / 600f);
            nextBeat = Time.unscaledTime + 60f / heartBpm;
        }

        /// <summary>How far the barrel has turned from the hero (0) to the Foreman (1) in the struggle:
        /// a little away, back at him, then over, inch by inch.</summary>
        static float TurnCurve(float t)
        {
            float[] kt = { 0f, 0.6f, 1.0f, 1.6f, 2.1f, 2.6f };
            float[] kv = { 0f, 0.18f, 0.04f, 0.45f, 0.8f, 1f };
            for (int i = 1; i < kt.Length; i++)
                if (t <= kt[i]) return Mathf.Lerp(kv[i - 1], kv[i], Mathf.SmoothStep(0f, 1f, (t - kt[i - 1]) / (kt[i] - kt[i - 1])));
            return 1f;
        }

        Transform flashQuad;
        float flashOff;
        readonly System.Collections.Generic.List<(Transform tr, Vector3 v, float born)> smoke = new System.Collections.Generic.List<(Transform, Vector3, float)>();
        Material smokeMat;

        /// <summary>The muzzle flash (two frames of hot white-gold at the barrel) and the smoke
        /// that curls up out of it through the light.</summary>
        void MuzzleFlash()
        {
            if (gun == null) return;
            var tip = gun.position + gun.forward * 0.38f;
            var tex = Radial(64, false);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "MuzzleFlash";
            flashQuad = q.transform;
            flashQuad.position = tip + Vector3.back * 0.05f;
            flashQuad.localScale = new Vector3(0.75f, 0.45f, 1f);
            flashQuad.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(gun.forward.y, gun.forward.x) * Mathf.Rad2Deg);
            q.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = new Color(1f, 0.88f, 0.6f, 1f) };
            flashOff = Time.unscaledTime + 0.07f;
            smokeMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, color = new Color(0.75f, 0.78f, 0.82f, 0.22f) };
            for (int i = 0; i < 18; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(p.GetComponent<Collider>());
                p.name = "GunSmoke";
                p.transform.position = tip + Random.insideUnitSphere * 0.03f;
                p.transform.localScale = Vector3.one * 0.06f;
                p.GetComponent<Renderer>().sharedMaterial = smokeMat;
                var v = gun.forward * Random.Range(0.1f, 0.5f) + new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(0.08f, 0.22f), 0f);
                smoke.Add((p.transform, v, Time.unscaledTime + i * 0.05f));
            }
        }

        void UpdateFlashSmoke()
        {
            if (flashQuad != null && Time.unscaledTime > flashOff) { Destroy(flashQuad.gameObject); flashQuad = null; }
            float dt = Time.unscaledDeltaTime;
            for (int i = smoke.Count - 1; i >= 0; i--)
            {
                var (tr, v, born) = smoke[i];
                if (tr == null) { smoke.RemoveAt(i); continue; }
                float age = Time.unscaledTime - born;
                if (age < 0f) { tr.gameObject.SetActive(false); continue; }
                tr.gameObject.SetActive(true);
                if (age > 5f) { Destroy(tr.gameObject); smoke.RemoveAt(i); continue; }
                v *= 1f - 0.8f * dt;                                       // the push dies; it hangs and rises
                v.y += 0.03f * dt;
                v.x += Mathf.Sin(age * 2.3f + i) * 0.02f * dt;               // curling
                tr.position += v * dt;
                tr.localScale = Vector3.one * (0.06f + age * 0.11f);
                smoke[i] = (tr, v, born);
            }
            if (smokeMat != null && smoke.Count > 0) smokeMat.color = new Color(0.75f, 0.78f, 0.82f, 0.2f);
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
            UpdateFlashSmoke();
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
            foreach (var sm in smoke) if (sm.tr != null) Destroy(sm.tr.gameObject);
            smoke.Clear();
            if (flashQuad != null) { Destroy(flashQuad.gameObject); flashQuad = null; }
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
