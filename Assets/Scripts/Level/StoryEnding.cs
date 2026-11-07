using System.Collections;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>THE ENDING of chapter 1 (docs/story/chapter1_fight.md, section 5): directed, no input.
    /// The Foreman goes down in slow motion among the drums and does not get up. Rain through the
    /// torn skylight. The hero kneels over him and asks; he laughs, and gives him two things that
    /// will not leave him ("his son", "say hello to her"). A pistol comes out of his jacket where
    /// only we see it, up into the hero's face. They struggle for it, the barrel turns, a white
    /// frame and silence. He dies; a photo of a little girl slides out of his jacket into the
    /// water. The hero gets up, backing away, staring at his hands, shaking his head: no, no, no.
    /// He walks out of the big door into the rain and the searchlight. CHAPTER 1 COMPLETE.</summary>
    public partial class StoryIntro
    {
        public static bool BossReached;
        float headYaw;
        bool headYawOn;
        Transform gun, photo;
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
            float landX = Mathf.Clamp(bx + 1.4f * Mathf.Sign(bx - hero.transform.position.x), 52f, 60f);
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
            yield return Hold(1.2f, () => EndShot(new Vector3(landX - 4.0f, 0.32f, -2.8f), new Vector3(landX - 0.7f, 0.55f, 0f), 38f));

            // ---------------------------------------------------------------- 3-5 the questions
            var heroHead = new System.Func<Vector3>(() => HeadC());
            var bossHead = new System.Func<Vector3>(() => BossHead(boss, landX));
            Say("HIM", "Why are you trying to kill me?", 2.4f);
            Sound.Play(Sfx.Heartbeat, 0.6f);
            yield return Hold(2.4f, () => EndShot(heroHead() + new Vector3(-0.55f, 0.32f, -0.65f), bossHead(), 34f));
            if (boss != null) Sound.Play(Sfx.BruteLaugh, 0.55f, boss.Chest, 0.8f);
            Say("THE FOREMAN", "You... really don't remember.", 2.4f);
            yield return Hold(2.4f, () => EndShot(bossHead() + new Vector3(-0.35f, 0.55f, -0.85f), bossHead(), 30f));
            Say("HIM", "Tell me who I am. WHO SENT YOU?", 2.4f);
            Sound.Play(Sfx.Tinnitus, 0.25f);
            yield return Hold(2.4f, () => EndShot(heroHead() + new Vector3(0.75f, 0.0f, -0.75f), heroHead(), 30f));
            Say("THE FOREMAN", "He'll never stop... not after what you did to his son.", 3.0f);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Hold(3.0f, () => EndShot(bossHead() + new Vector3(-0.3f, 0.5f, -0.8f), bossHead(), 28f));

            // ---------------------------------------------------------------- 6 the gun, where only we see it
            BuildGun(boss);
            Sound.Play(Sfx.Cloth, 0.5f);
            Sound.Play(Sfx.Heartbeat, 1f, null, 1.1f);
            yield return Hold(1.6f, () =>
            {
                var hand = HandOf(boss, landX);
                EndShot(hand + new Vector3(0.35f, 0.18f, -0.55f), hand, 34f);
            });

            // ---------------------------------------------------------------- 7 up into his face
            if (boss != null) boss.PuppetPlay("lie_gun", 0.08f, 0.8f);
            Sound.Play(Sfx.Latch, 0.6f, null, 1.8f);                       // the hammer back
            Say("THE FOREMAN", "Say hello to her for me.", 2.0f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.4f, 1.8f, 0.05f, 0.3f);
            yield return Hold(2.0f, () =>
            {
                var hand = HandOf(boss, landX);
                EndShot(hand + new Vector3(0.05f, 0.08f, -0.32f), heroHead(), 30f);
            });

            // ---------------------------------------------------------------- 8 the struggle
            if (hero.body.Has("kneel_grip")) hero.body.Play("kneel_grip", 0.06f, true, 1.4f);
            Sound.Play(Sfx.HeroEffort, 1f);
            Sound.Play(Sfx.EnemyEffort, 1f, null, 0.8f);
            float ts = 0f;
            while (ts < 1.6f)
            {
                ts += Time.unscaledDeltaTime;
                var mid = Vector3.Lerp(heroHead(), bossHead(), 0.5f);
                var shake = new Vector3(Random.Range(-0.04f, 0.04f), Random.Range(-0.03f, 0.03f), 0f) * (1f + ts);
                EndShot(mid + new Vector3(0.2f, 0.25f, -1.2f) + shake, mid + shake * 0.5f, 34f);
                if (Random.value < 0.06f) Sound.Play(Sfx.Cloth, 0.6f, null, Random.Range(0.9f, 1.2f));
                yield return null;
            }

            // ---------------------------------------------------------------- 9 BANG
            flashFrames = 4;
            Sound.Play(Sfx.GunBody, 1f);
            Sound.Play(Sfx.GunTail, 1f);
            Buzz();
            if (cam != null) cam.Shake(0.2f, 0.2f);
            Sound.Ambience(0.25f, 0.05f, 600f);                            // the world goes away
            Sound.Play(Sfx.Tinnitus, 1f);
            if (InkFx.Instance != null && boss != null) InkFx.Instance.Splash(boss.Chest, new Vector3(1f, 0.4f, 0f), 30, boss.InkColor);
            yield return RealWait(0.25f);

            // ---------------------------------------------------------------- 10 he dies
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.2f, 2.0f, 0.02f, 0.5f);
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.5f);
            if (boss != null) boss.PuppetPlay("lie", 1.2f);
            DropGun(landX);
            for (int i = 0; i < 4 && boss != null; i++)
                if (InkFx.Instance != null) InkFx.Instance.Stain(boss.transform.position + Vector3.right * (0.2f * i - 0.3f), boss.InkColor, 0.8f + 0.4f * i, false);
            yield return Hold(2.2f, () => EndShot(bossHead() + new Vector3(-0.25f, 0.45f, -0.7f), bossHead(), 28f));

            // ---------------------------------------------------------------- 11 the photo
            BuildPhoto(landX);
            Sound.Play(Sfx.Piano, 0.55f);
            float tp = 0f;
            var pStart = photo.position;
            while (tp < 2.2f)
            {
                tp += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, tp / 1.2f);
                photo.position = Vector3.Lerp(pStart, pStart + new Vector3(0.35f, 0f, -0.25f), k);
                EndShot(photo.position + new Vector3(-0.15f, 0.75f, -0.35f), photo.position, 34f);
                yield return null;
            }

            // ---------------------------------------------------------------- 12 wide, high: small in the empty hall
            Sound.Ambience(1.2f, 0.25f, 22000f);
            yield return Hold(2.4f, () => EndShot(new Vector3(landX - 7.0f, 7.2f, -4.5f), new Vector3(landX - 1.0f, 0.4f, 0.5f), 46f));

            // ---------------------------------------------------------------- 13 no... no... no...
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.8f);
            Sound.Play(Sfx.Cloth, 0.7f);
            yield return Hold(0.9f, () => EndShot(heroHead() + new Vector3(0.4f, -0.1f, -1.9f), heroHead() + Vector3.down * 0.3f, 34f));
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
                EndShot(heroHead() + new Vector3(0.6f - 0.15f * tn / 4.2f, -0.05f, -1.6f + 0.35f * tn / 4.2f), heroHead() + Vector3.down * 0.15f, 32f);
                yield return null;
            }
            headYawOn = false;

            // ---------------------------------------------------------------- 14 he walks out into the rain
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

            // ---------------------------------------------------------------- 15 black: chapter complete
            cardAt = Time.unscaledTime;
            cardLine = "You kill people. You are good at it.";
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

        void BuildGun(Enemy boss)
        {
            var h = boss != null && boss.body != null ? boss.body.Bone("hand.R") : null;
            if (h == null) return;
            var go = new GameObject("ForemanPistol");
            gun = go.transform;
            gun.SetParent(h, false);
            gun.localPosition = Vector3.zero;
            gun.localRotation = Quaternion.identity;
            gun.localScale = Vector3.one * 1.5f;
            FighterFactory.Model(gun, FightStyle.Gun, hero.weaponMaterial);
        }

        void DropGun(float x)
        {
            if (gun == null) return;
            gun.SetParent(null, true);
            var f = gun.gameObject.AddComponent<ShieldFall>();
            f.velocity = new Vector3(0.6f, 1.2f, 0f);
        }

        /// <summary>The photo: a little girl in a summer garden, red balloon, worn white border.</summary>
        void BuildPhoto(float x)
        {
            var tex = new Texture2D(96, 120, TextureFormat.RGBA32, false);
            var sky = new Color(0.62f, 0.74f, 0.82f); var grass = new Color(0.36f, 0.52f, 0.28f);
            for (int j = 0; j < 120; j++)
                for (int i = 0; i < 96; i++)
                {
                    Color c = j < 8 || j > 111 || i < 6 || i > 89 ? new Color(0.92f, 0.9f, 0.84f) : (j < 42 ? grass : sky);
                    // the girl: a little ink figure, ponytail, a dress
                    float hx = i - 46f, hy = j - 64f;
                    if (hx * hx + hy * hy < 64f) c = new Color(0.05f, 0.05f, 0.06f);                            // head
                    if (i >= 51 && i <= 56 && j >= 60 && j <= 68 && (i - 51) <= (j - 60)) c = new Color(0.05f, 0.05f, 0.06f);   // ponytail
                    if (Mathf.Abs(i - 46f) < (j < 56 && j > 38 ? (56 - j) * 0.45f + 2f : 0f)) c = new Color(0.82f, 0.36f, 0.42f);    // dress
                    if ((i == 44 || i == 48) && j >= 30 && j <= 38) c = new Color(0.05f, 0.05f, 0.06f);                     // legs
                    if (i >= 50 && i <= 51 && j >= 52 && j <= 84) c = new Color(0.25f, 0.25f, 0.25f);                      // the string
                    float bx2 = i - 54f, by2 = j - 92f;
                    if (bx2 * bx2 * 1.2f + by2 * by2 < 70f) c = new Color(0.85f, 0.12f, 0.1f);                           // the balloon
                    if (((i * 7 + j * 13) % 97) == 0) c *= 0.9f;                                                          // worn
                    tex.SetPixel(i, j, c);
                }
            tex.Apply();
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Photo";
            photo = q.transform;
            photo.position = new Vector3(x + 0.1f, 0.02f, 0.05f);
            photo.rotation = Quaternion.Euler(90f, 18f, 0f);
            photo.localScale = new Vector3(0.16f, 0.2f, 1f);
            var sh = Shader.Find("Unlit/Texture");
            var r = q.GetComponent<Renderer>();
            if (sh != null) r.material = new Material(sh) { mainTexture = tex };
        }

        /// <summary>The closing card, over black: chapter complete, and the memory it gave back.</summary>
        void EndingGUI(float w, float h)
        {
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
