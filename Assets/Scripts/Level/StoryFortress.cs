using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>CHAPTER 5: THE FORTRESS (docs/story/chapter5_fortress.md). Kessel Hill in the storm: the
    /// cliff road, the rock stair, the gate, the trap (the portcullis), the courtyard, the blackout (the
    /// lightning is the only light), the great hall and THE WARDEN, and the tower: Maya at the rail with
    /// the man who knows him.
    ///
    /// Every beat is covered from several angles (establishing, medium, close, insert, reverse, high and
    /// low), handheld in the rain, cut on the lightning. Big hits in the fight get the storm too: a strike,
    /// slow motion, a low angle (docs/story/camera_language.md).</summary>
    public partial class StoryIntro
    {
        public static int FortressCheckpoint = -1;
        /// <summary>TEST: 0 the road, 1 the stair, 2 the gate, 3 the courtyard, 4 the blackout, 5 the Warden, 6 the tower,
        /// 7 the last roof (Voss), 8 the duel, 9 the end (the fall, her, the dawn).</summary>
        public static int FortressFrom;

        static readonly Spawn[][] FortWaves =
        {
            new[] { S(Br, Rt), S(Br, Rt), S(Ru, Lt), S(Gr, Rt), S(Br, Bg) },
            new[] { S(Br, Rt), S(Dm, Rt), S(Ru, Bg), S(Br, Lt) },
            new[] { S(Br, Rt), S(Br, Bg), S(Gr, Rt), S(Ru, Lt), S(Pi, Rt) },
            new[] { S(Br, Rt), S(Ru, Bg), S(Br, Rt), S(Gr, Bg), S(Br, Lt) },
        };
        static readonly int[] FortCaps = { 3, 3, 3, 3 };

        int fZone = -1, fQueued, wardenStage;
        bool fReady, fClear, storm, fBlackout, indoors;
        float fNextSpawn, fDone = -1f, fGoUntil, nextStrike, fortTitleAt = -1f, lastBigHit = -10f, lensRain;
        readonly List<Enemy> fortMen = new List<Enemy>();
        Enemy warden;
        Transform portcullis;
        /// <summary>0 dry, 1 the full downpour: it only starts on the roof, with Voss.</summary>
        float rainAmount;
        Material[] rippleMats;

        /// <summary>The wet stones: still puddles while it's dry, rippling once the rain comes.</summary>
        void SetRipple(float k)
        {
            if (rippleMats == null)
            {
                var list = new List<Material>();
                foreach (var n in new[] { "WetRoad", "WetCourt", "WetRoof" })
                {
                    var g = GameObject.Find(n);
                    var r = g != null ? g.GetComponent<Renderer>() : null;
                    if (r != null && r.material != null && r.material.HasProperty("_Ripple")) list.Add(r.material);
                }
                rippleMats = list.ToArray();
            }
            foreach (var m in rippleMats) if (m != null) m.SetFloat("_Ripple", Mathf.Lerp(0.0005f, 0.004f, k));
        }
        Vector3 portUp;
        ParticleSystem rainPs;
        readonly List<Renderer> floodR = new List<Renderer>();
        readonly List<Transform> chandeliers = new List<Transform>();
        float nextChandelier;
        Enemy towerMan, towerGirl;

        // ================================================================ setting up

        void BeginFortress()
        {
            phase = Phase.Fortress;
            HeadWound.Chapter = 3;
            storm = true;
            Sound.Ambience(0.35f, 0.25f, 22000f);                    // a dry storm: wind, thunder, no rain yet
            LightningSwap.Held = false;
            Sound.Music("storm", 0.5f, true, 2.5f);
            nextStrike = Time.unscaledTime + 2.5f;
            FortProps();
            HeroController.Did += OnFortHit;
            int from = FortressFrom;
            FortressFrom = 0;
            if (from == 0 && FortressCheckpoint >= 0) from = FortressCheckpoint >= 7 ? 8 : FortressCheckpoint >= 5 ? 5 : FortressCheckpoint == 4 ? 4 : FortressCheckpoint == 0 ? -1 : FortressCheckpoint + 1;
            StartCoroutine(Fortress(from));
        }

        void FortProps()
        {
            portcullis = Find("Portcullis");
            if (portcullis != null) { portUp = portcullis.position + Vector3.up * 4.2f; portcullis.position = portUp; }
            foreach (var n in new[] { "FloodCone_Court1", "FloodGlow_Court1", "FloodCone_Court2", "FloodGlow_Court2" })
            {
                var t = Find(n);
                if (t != null) floodR.Add(t.GetComponent<Renderer>());
            }
            // the rain: falls round the camera, wherever it goes outdoors
            var go = new GameObject("StormRain");
            rainPs = go.AddComponent<ParticleSystem>();
            rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rainPs.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 22f;
            main.startSize = 0.035f;
            main.startColor = new Color(0.86f, 0.88f, 0.92f, 0.42f);
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = rainPs.emission; em.rateOverTime = 0f;                // no rain until the roof (StoryFinale: TheRain)
            var sh = rainPs.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40f, 16f, 0.5f);
            go.transform.rotation = Quaternion.Euler(100f, 0f, 0f);          // slanted: the wind
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 3f;
            r.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            rainPs.Play();
        }

        /// <summary>The storm, every frame: the rain follows the camera (not indoors), the strikes come.</summary>
        void UpdateStorm()
        {
            if (!storm) return;
            var c = Camera.main;
            indoors = hero != null && hero.transform.position.y < FortressPlan.RoofY - 1f && hero.transform.position.x > FortressPlan.HallX0 + 0.5f && (c == null || c.transform.position.z > -3.5f || hero.transform.position.x < FortressPlan.HallX1);
            if (rainPs != null && c != null)
            {
                rainPs.transform.position = new Vector3(c.transform.position.x, c.transform.position.y + 9f, 1.5f);
                var em = rainPs.emission;
                em.enabled = rainAmount > 0.001f && !(indoors && cardAt < 0f && fZone >= 4);
                em.rateOverTime = 2600f * rainAmount;
            }
            float wet = Mathf.Lerp(0.35f, 2.2f, rainAmount);              // before the rain only the wind; then the downpour
            Sound.Ambience(indoors && fZone >= 4 ? wet * 0.45f : wet, indoors ? 0.55f : 0.25f, indoors && fZone >= 4 ? 1400f : 22000f);
            lensRain = Mathf.MoveTowards(lensRain, indoors && fZone >= 4 ? 0f : 0.32f * rainAmount, Time.unscaledDeltaTime * 0.5f);
            SetRipple(rainAmount);
            if (Time.unscaledTime >= nextStrike)
            {
                float s = Random.Range(0.75f, 1f);
                Lightning(s, Random.Range(0.6f, 1f), fBlackout ? Random.Range(0.1f, 0.5f) : Random.Range(0.4f, 2.2f));
                if (LightningSwap.Instance != null && c != null && Random.value < 0.7f)
                {
                    float bx = c.transform.position.x + Random.Range(-18f, 18f);
                    LightningSwap.Instance.Bolt(new Vector3(bx, 46f, 90f), new Vector3(bx + Random.Range(-8f, 8f), 8f, 80f), 0.5f);
                }
                nextStrike = Time.unscaledTime + (fBlackout ? Random.Range(1.5f, 3f) : Random.Range(6f, 12f));
            }
            if (fBlackout) foreach (var r in floodR) if (r != null) r.enabled = false;
        }

        /// <summary>A strike now, on cue (a cut in a cutscene lands on it).</summary>
        void Strike(float strength = 1f, float thunderDelay = 0.3f)
        {
            Lightning(strength, 1f, thunderDelay);
            var c = Camera.main;
            if (LightningSwap.Instance != null && c != null)
            {
                float bx = c.transform.position.x + Random.Range(-10f, 10f);
                LightningSwap.Instance.Bolt(new Vector3(bx, 46f, 90f), new Vector3(bx + Random.Range(-6f, 6f), 10f, 80f), 0.6f);
            }
            nextStrike = Time.unscaledTime + Random.Range(5f, 9f);
        }

        /// <summary>The fight's big moments get the storm: a strike, slow motion, a low angle.</summary>
        void OnFortHit(string what)
        {
            if (!storm || phase != Phase.Fortress || (what != "throw" && what != "combo")) return;
            if (Time.unscaledTime - lastBigHit < 6f || Random.value > 0.45f) return;
            lastBigHit = Time.unscaledTime;
            Strike(0.95f, 0.15f);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.3f, 0.6f, 0.02f, 0.25f);
            if (cam != null) cam.KillCam(hero.transform.position + Vector3.right * hero.Facing * 0.9f + Vector3.up * 0.6f, 1.1f, Random.value < 0.5f ? 30f : -30f);
        }

        // ================================================================ cinema

        /// <summary>A cut: one shot held for `secs`, a little handheld sway in it (the rain, the wind).</summary>
        IEnumerator Cut(float secs, System.Func<float, CineCam.Shot> shot, float sway = 0.025f, System.Action<float> each = null)
        {
            float t0 = 0f;
            while (t0 < secs)
            {
                t0 += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t0 / secs);
                var s = shot(u);
                s.pos += Sway(sway, 0.55f, 13f);
                Cine(s);
                each?.Invoke(u);
                yield return null;
            }
        }

        static CineCam.Shot Static(Vector3 pos, Vector3 look, float fov) => new CineCam.Shot(pos, look, fov);

        // ================================================================ the chapter

        IEnumerator Fortress(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            if (from >= 1) AttachRibbon();
            if (from == 0)
            {
                // ------------------------------------------------ T1 black, a flash: the title
                AttachRibbon();
                blackout = 1f;
                yield return RealWait(0.8f);
                Strike(1f, 0.6f);
                fortTitleAt = Time.unscaledTime;
                yield return RealWait(3.2f);
                hero.CinePose(new Vector3(2f, 0f, 0f));
                // ------------------------------------------------ T2 (a) the fortress in the storm, from the air
                StartCoroutine(FadeIn(0.8f));
                bool hit = false;
                yield return Cut(5.0f, u => CineCam.AerialOrbit(new Vector3(100f, 10f, 4f), u, 70f, 26f, -38f, -8f, 38f), 0.05f,
                    u => { if (!hit && u > 0.62f) { hit = true; Strike(1f, 0.2f); } });
                // (b) high, down the cliff road: a small figure walking up
                hero.body.Play("walk", 0.2f, true, 0.85f);
                float wx0 = 2f;
                yield return Cut(3.6f, u => Static(new Vector3(30f, 16f, -9f), hero.transform.position + Vector3.up * 0.8f, 30f), 0.03f,
                    u => hero.CinePose(new Vector3(wx0 + u * 3.2f, 0f, 0f)));
                // ------------------------------------------------ T3 (a) a boot in a puddle, at ground level
                yield return Cut(1.6f, u => Static(hero.transform.position + new Vector3(1.2f, 0.12f, -0.9f), hero.transform.position + new Vector3(0f, 0.15f, 0f), 34f), 0.01f,
                    u => { hero.CinePose(new Vector3(wx0 + 3.2f + u * 1.2f, 0f, 0f)); if (u > 0.35f && u - Time.unscaledDeltaTime / 1.6f <= 0.35f) Sound.Play(Sfx.Splash, 0.6f, hero.transform.position); });
                // (b) walking into the camera, the rain sheeting between
                float wx1 = hero.transform.position.x;
                yield return Cut(3.4f, u => CineCam.DollyOut(hero.transform.position + Vector3.up * 1.1f, new Vector3(-1f, -0.05f, 0.25f), u, 3.2f, 4.0f, 36f), 0.03f,
                    u => hero.CinePose(new Vector3(wx1 + u * 3.4f, 0f, 0f)));
                // (c) close, his profile, water running off him
                float wx2 = hero.transform.position.x;
                yield return Cut(2.6f, u => Static(HeroHead() + new Vector3(0.1f, -0.05f, -1.3f), HeroHead(), 30f), 0.015f,
                    u => hero.CinePose(new Vector3(wx2 + u * 1.6f, 0f, 0f)));
                // (d) the ribbon on his wrist, soaked
                yield return Cut(1.8f, u => Static((wristBand != null ? wristBand.position : hero.Chest) + new Vector3(0.1f, 0.2f, -0.7f), wristBand != null ? wristBand.position : hero.Chest, 26f), 0.01f,
                    u => hero.CinePose(new Vector3(wx2 + 1.6f + u * 0.8f, 0f, 0f)));
                // ------------------------------------------------ T4 (a) reverse from the battlements: two guards look down
                hero.body.Play("idle", 0.3f);
                var g1 = director != null ? director.SpawnScripted(Br, SpawnKind.Background) : null;
                var g2 = director != null ? director.SpawnScripted(Br, SpawnKind.Background) : null;
                foreach (var (g, x) in new[] { (g1, 56.2f), (g2, 57.4f) })
                    if (g != null) { g.Puppet(); g.PuppetPose(new Vector3(x, FortressPlan.Up + 8.05f, 4.1f)); g.SetFacing(-1); g.PuppetPlay("idle", 0f); GiveTorch(g); }
                yield return Cut(2.4f, u => Static(new Vector3(58.5f, FortressPlan.Up + 9.6f, 4.8f), hero.transform.position + Vector3.up * 0.8f, 26f), 0.03f);
                // (b) on the guards: "Movement on the road!"
                Say("GUARD", "It's him! The book's on him!", 2.0f, g1 != null ? HeadOf(g1) : null);
                yield return Cut(2.0f, u => Static(new Vector3(55.0f, FortressPlan.Up + 8.8f, 0.6f), new Vector3(56.8f, FortressPlan.Up + 9.2f, 4.1f), 34f), 0.03f);
                Say("GUARD", "Whoever brings the boss that book gets the son's place!", 2.6f, g2 != null ? HeadOf(g2) : null);
                yield return Cut(2.6f, u => Static(new Vector3(57.6f, FortressPlan.Up + 8.6f, 0.4f), new Vector3(57.4f, FortressPlan.Up + 9.2f, 4.1f), 32f), 0.03f);
                // (c) back on him, low: he doesn't stop
                hero.body.Play("walk", 0.2f, true, 0.9f);
                float wx3 = hero.transform.position.x;
                yield return Cut(2.2f, u => CineCam.LowAngleArc(hero.transform.position + Vector3.up * 1.1f, u, 2.6f, -30f, -10f, 34f), 0.02f,
                    u => hero.CinePose(new Vector3(wx3 + u * 1.2f, 0f, 0f)));
                foreach (var g in new[] { g1, g2 }) if (g != null) g.PuppetDie();
            }
            if (from == 0 || from == -1)
            {
                // ------------------------------------------------ T5 play: the cliff road
                if (from == -1) yield return FadeIn(0.4f);
                StartFortZone(0, from == -1);
                while (!(fZone == 0 && fClear)) yield return null;
            }
            if (from <= 1 || from == -1)
            {
                // ------------------------------------------------ T6 the rock stair: a truck, a silhouette on a strike, a crane up
                if (from == 1) { hero.CinePose(new Vector3(42.5f, 0f, 0f)); yield return FadeIn(0.4f); }
                yield return PlayerWalk(43.6f, hero.transform.position.x - 0.5f, 2.4f, Application.isMobilePlatform ? "HOLD TO CLIMB" : "HOLD  D  TO CLIMB");
                hero.CinePuppet();
                hero.body.Play("walk", 0.2f, true, 1.0f);
                var s0 = new Vector3(FortressPlan.StairX0, 0f, 0.4f);
                var s1 = new Vector3(FortressPlan.StairX1 + 0.6f, FortressPlan.Up, 0f);
                var sMid = Vector3.Lerp(s0, s1, 0.5f);
                yield return Cut(2.0f, u => CineCam.Truck(new Vector3(s0.x - 0.5f, 1.5f, -4.5f), new Vector3(sMid.x, sMid.y + 1.5f, -4.5f), new Vector3(0.3f, -0.3f, 4.5f), u, 40f), 0.03f,
                    u => hero.CinePose(Vector3.Lerp(s0, sMid, u)));
                Strike(1f, 0.1f);
                yield return Cut(1.2f, u => Static(sMid + new Vector3(3.5f, -1.0f, -3.5f), hero.transform.position + Vector3.up * 1.2f, 40f), 0.02f,
                    u => hero.CinePose(Vector3.Lerp(sMid, Vector3.Lerp(s0, s1, 0.7f), u)));
                var s2 = Vector3.Lerp(s0, s1, 0.7f);
                yield return Cut(2.2f, u => CineCam.PedestalUp(hero.transform.position + Vector3.up * 0.9f, u, 3.4f, -1.2f, 1.6f, 38f), 0.02f,
                    u => hero.CinePose(Vector3.Lerp(s2, s1, u)));
            }
            if (from <= 2 || from == -1)
            {
                // ------------------------------------------------ T7 play: the gate
                if (from == 2) { hero.CinePose(new Vector3(53.4f, FortressPlan.Up, 0f)); yield return FadeIn(0.4f); }
                StartFortZone(1, from == 2);
                while (!(fZone == 1 && fClear)) yield return null;
                // ------------------------------------------------ T8 the trap
                yield return ThePortcullis();
            }
            if (from <= 3 || from == -1)
            {
                if (from == 3) { if (portcullis != null) portcullis.position = portUp - Vector3.up * 4.2f; yield return FadeIn(0.4f); }
                StartFortZone(2, from == 3);
                while (!(fZone == 2 && fClear)) yield return null;
            }
            if (from <= 4 || from == -1)
            {
                // ------------------------------------------------ T10 the blackout, T11 the dark fight
                if (from == 4) { if (portcullis != null) portcullis.position = portUp - Vector3.up * 4.2f; hero.CinePose(new Vector3(101f, FortressPlan.Up, 0f)); yield return FadeIn(0.4f); }
                yield return TheBlackout();
                StartFortZone(3, false);
                while (!(fZone == 3 && fClear)) yield return null;
            }
            if (from <= 5 || from == -1)
            {
                // ------------------------------------------------ T12 the hall, T13 THE WARDEN
                if (from == 5) { fBlackout = true; hero.CinePose(new Vector3(126.5f, FortressPlan.Up, 0f)); yield return FadeIn(0.4f); }
                yield return TheHall();
                while (wardenStage < 5) yield return null;
                yield return AfterWarden();
            }
            if (from == 6) { hero.CinePose(new Vector3(146f, FortressPlan.Up, 0f)); yield return FadeIn(0.3f); }
            // ---------------------------------------------------- T15 the tower
            if (from <= 6 || from == -1) yield return TheTower();
            // ---------------------------------------------------- the last roof (StoryFinale)
            yield return LastRoof(from);
        }

        // ================================================================ the zones

        void StartFortZone(int z, bool cut)
        {
            FortressCheckpoint = z;
            fZone = z;
            fQueued = 0;
            fClear = false;
            fReady = true;
            fDone = -1f;
            fNextSpawn = Time.time + (cut ? 1.2f : 0.5f);
            fortMen.Clear();
            phase = Phase.Fortress;
            var Z = FortressPlan.Zones[z];
            SetArena(Z.x0, Z.x1);
            if (director != null)
            {
                float mid = (Z.x0 + Z.x1) * 0.5f;
                var pts = director.spawnPoints;
                void At(SpawnKind k, Vector3 p) { var t = pts[(int)k]; if (t != null) t.position = p; }
                At(SpawnKind.RightDoor, new Vector3(Z.x1 - 0.6f, Z.y, 0f));
                At(SpawnKind.LeftDoor, new Vector3(Z.x0 + 0.6f, Z.y, 0f));
                At(SpawnKind.Background, new Vector3(mid + 2f, Z.y, 3.2f));
                At(SpawnKind.UpperDoor, new Vector3(mid, Z.y, 3.2f));
                At(SpawnKind.Ceiling, new Vector3(mid, Z.y, 3.2f));
                director.ShowBanner($"{z + 1}  ·  {Z.name}", 2.0f);
            }
            if (cut || hero.CinematicLock)
            {
                if (cut) hero.CinePose(new Vector3(Z.x0 + 1.4f, Z.y, 0f));
                hero.SetFacing(1);
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                barsPx = 0f;
                CineOff();
            }
            Sound.Tension(true);
            Sound.Music(fBlackout ? "dark" : "fight", 0.55f, true, 1.2f);
        }

        static readonly string[] BookBarks =
        {
            "The book! Get the book!", "Silas's place is mine!", "Take it off his body!", "The chair goes to whoever gets the book!",
            "He's one man! ONE MAN!", "Boss is watching! Make it count!",
        };
        float nextBark;

        void UpdateFortress()
        {
            if (fZone >= 0 && fZone <= 3 && !fClear && fortMen.Count > 0 && Time.unscaledTime > nextBark)
            {
                nextBark = Time.unscaledTime + Random.Range(7f, 12f);
                Say("THUG", BookBarks[Random.Range(0, BookBarks.Length)], 2.0f);
            }
            UpdateStorm();
            UpdateChandeliers();
            UpdateVoss();
            if (fZone >= 6) return;
            if (wardenStage > 0 && wardenStage < 5) { UpdateWarden(); return; }
            if (!fReady || fZone < 0 || fZone > 3 || hero.IsDead) return;
            fortMen.RemoveAll(e => e == null || !e.Alive);
            if (!fClear)
            {
                var list = FortWaves[fZone];
                if (fQueued < list.Length && fortMen.Count < FortCaps[fZone] && Time.time >= fNextSpawn && director != null)
                {
                    var sp = list[fQueued];
                    var e = director.SpawnScripted(sp.type, sp.kind);
                    if (e != null) { fortMen.Add(e); if (Random.value < 0.4f) GiveTorch(e); }
                    fQueued++;
                    fNextSpawn = Time.time + Random.Range(0.6f, 1.2f);
                }
                if (fQueued >= list.Length && fortMen.Count == 0)
                {
                    if (fDone < 0f) fDone = Time.time;
                    if (Time.time - fDone > 1.0f)
                    {
                        fClear = true;
                        Sound.Music("storm", 0.45f, true, 3f);
                        Sound.Play(Sfx.WaveClear, 0.6f);
                        if (director != null) director.ShowBanner("CLEAR", 1.2f);
                        fGoUntil = Time.unscaledTime + 30f;
                        if (fZone == 2) SetArena(FortressPlan.Zones[2].x0, FortressPlan.Zones[3].x0 + 2f);
                    }
                }
            }
        }

        // ================================================================ the set pieces

        /// <summary>T8: through the arch; the portcullis slams down behind him. They let him in.</summary>
        IEnumerator ThePortcullis()
        {
            yield return PlayerWalk(FortressPlan.GateArchX + 2.6f, FortressPlan.Zones[1].x0, 1.8f, Application.isMobilePlatform ? "HOLD TO GO THROUGH THE GATE" : "HOLD  D  TO GO THROUGH THE GATE");
            hero.CinePuppet();
            hero.body.Play("walk", 0.2f, true, 0.8f);
            var p0 = hero.transform.position;
            // (a) low behind him, under the arch
            yield return Cut(1.8f, u => Static(new Vector3(FortressPlan.GateArchX - 1.8f, FortressPlan.Up + 0.5f, -0.6f), hero.transform.position + Vector3.up * 1.0f, 40f), 0.02f,
                u => hero.CinePose(p0 + Vector3.right * u * 1.4f));
            // (b) the portcullis: down, in slow motion, sparks off the stones
            Sound.Play(Sfx.Chain, 1f, new Vector3(FortressPlan.GateArchX, FortressPlan.Up + 4f, -1.4f));
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.25f, 1.2f, 0.02f, 0.3f);
            var down = portUp - Vector3.up * 4.2f;
            yield return Cut(1.4f, u => Static(new Vector3(FortressPlan.GateArchX + 2.4f, FortressPlan.Up + 0.6f, -3.6f), new Vector3(FortressPlan.GateArchX, FortressPlan.Up + 1.6f, -1.4f), 40f), 0.01f,
                u => { if (portcullis != null) portcullis.position = Vector3.Lerp(portUp, down, u * u); });
            Sound.Play(Sfx.MetalClang, 1f, down);
            Sound.Play(Sfx.Boom, 0.7f, down);
            if (cam != null) cam.Shake(0.3f, 0.4f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(new Vector3(FortressPlan.GateArchX, FortressPlan.Up + 0.1f, -1.4f), Vector3.up, 30, new Color(0.9f, 0.9f, 0.9f));
            // (c) on him: he doesn't turn
            hero.body.Play("idle", 0.3f);
            Say("VOICE, FROM THE WALLS", "He's in. Close it. Nobody touches the girl. Everybody touches him.", 3.0f);
            yield return Cut(2.2f, u => CineCam.DollyIn(HeroHead(), new Vector3(-0.6f, -0.05f, 1f), u, 2.0f, 1.3f, 32f), 0.015f);
            // (d) high: men stepping out of every doorway into the floodlight
            Strike(0.8f, 0.4f);
            yield return Cut(2.6f, u => Static(new Vector3(84f, FortressPlan.Up + 9f, -6f), new Vector3(90f, FortressPlan.Up + 0.6f, 1.2f), 38f), 0.03f);
            CineOff();
        }

        /// <summary>T10: the strike on the floodlight pole, the generator bursting, every light dying.</summary>
        IEnumerator TheBlackout()
        {
            hero.CinePuppet();
            barsPx = 92f;
            hero.body.Play("idle", 0.2f);
            // (a) low, up the floodlight's pole: the strike
            var pole = new Vector3(FortressPlan.FloodPoleX, FortressPlan.Up + 6.2f, 3.4f);
            yield return Cut(1.4f, u => Static(pole + new Vector3(-3.2f, -5.8f, -3.0f), pole, 44f), 0.03f);
            Strike(1f, 0.0f);
            Sound.Play(Sfx.Boom, 1f, pole);
            if (cam != null) cam.Shake(0.35f, 0.5f);
            yield return Cut(0.8f, u => Static(pole + new Vector3(-3.2f, -5.8f, -3.0f), pole, 44f), 0.06f);
            // (b) the generator bursts: sparks, smoke
            var gen = new Vector3(FortressPlan.GeneratorX, FortressPlan.Up + 1.3f, 1.6f);
            Sound.Play(Sfx.Glass, 1f, gen);
            Sound.Play(Sfx.Buzz, 1f, gen);
            if (InkFx.Instance != null) { InkFx.Instance.Splash(gen, Vector3.up, 60, new Color(0.95f, 0.95f, 0.95f)); InkFx.Instance.ImpactFlash(0.08f); }
            yield return Cut(1.4f, u => Static(gen + new Vector3(-2.6f, 0.4f, -3.2f), gen, 40f), 0.05f);
            // (c) wide: every light dies
            fBlackout = true;
            Sound.Music("dark", 0.55f, true, 0.8f);
            Sound.Play(Sfx.Riser, 0.5f);
            yield return Cut(2.2f, u => Static(new Vector3(104f, FortressPlan.Up + 3.4f, -11f), new Vector3(110f, FortressPlan.Up + 1.4f, 1f), 42f), 0.03f);
            Say("YOU", "...Good. I don't need to see you.", 2.6f);
            yield return Cut(2.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.7f, -0.05f, 1f), u, 2.0f, 1.2f, 32f), 0.015f);
            CineOff();
        }

        // ================================================================ THE WARDEN

        IEnumerator TheHall()
        {
            SetArena(FortressPlan.Zones[3].x0, FortressPlan.HallX0 + 1.5f);
            yield return PlayerWalk(FortressPlan.HallX0 + 1.2f, FortressPlan.Zones[3].x0, 1.8f, Application.isMobilePlatform ? "HOLD TO GO IN" : "HOLD  D  TO GO IN");
            hero.CinePuppet();
            fZone = 4;
            Sound.Play(Sfx.Door, 1f, new Vector3(FortressPlan.HallX0, FortressPlan.Up + 1f, 0f), 0.7f);
            // spawn him at the head of the table
            WaveDirector.BossWeapon = FightStyle.Pipe;
            WaveDirector.StoryBoss = true;
            warden = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            WaveDirector.BossWeapon = FightStyle.Fists;
            if (warden == null) { wardenStage = 5; yield break; }
            warden.MakeDriver(warden.maxHealth * 1.15f);
            warden.transform.localScale = Vector3.one * 1.22f;
            warden.ShowIron(true);
            warden.DriverPhase = 2;
            warden.Puppet();
            warden.PuppetPose(new Vector3(153.5f, FortressPlan.Up, 2.6f));
            warden.SetFacing(-1);
            warden.PuppetPlay("idle", 0f);
            Enemy.StoryFinale = (e, hit) => { if (wardenStage < 5) wardenStage = 5; };
            // (a) a lateral reveal down the hall: the table, the fire, the chandeliers
            yield return Cut(3.6f, u => CineCam.LateralReveal(new Vector3(FortressPlan.HallX0 + 0.6f, FortressPlan.Up + 2.2f, -1.6f), new Vector3(FortressPlan.HallX0 + 4f, FortressPlan.Up + 2.4f, -2.4f), new Vector3(150f, FortressPlan.Up + 1.6f, 2f), u, 46f), 0.02f);
            // (b) a slow dolly in on the far end: the man at the head of the table
            Dress(warden.body, Look.Warden);
            Say("THE WARDEN", "Twenty years I kept his door. Boss says whoever brings the book gets the chair.", 3.6f, HeadOf(warden));
            yield return Cut(3.6f, u => CineCam.DollyIn(HeadOf(warden).position, new Vector3(1f, -0.1f, 0.4f), u, 7f, 3.2f, 30f), 0.015f);
            // (c) low as he comes round the table, the chain dragging
            warden.PuppetPlay("walk", 0.2f, 0.7f);
            var w0 = warden.transform.position;
            Sound.Play(Sfx.Chain, 0.8f, w0);
            yield return Cut(2.6f, u => CineCam.LowAngleArc(warden.transform.position + Vector3.up * 1.3f, u, 3.6f, -60f, -10f, 36f), 0.02f,
                u => warden.PuppetPose(Vector3.Lerp(w0, new Vector3(146f, FortressPlan.Up, 0f), u)));
            warden.PuppetPlay("sword_idle", 0.2f);
            Say("THE WARDEN", "No further.", 1.6f, HeadOf(warden));
            if (director != null) director.ShowBanner("THE WARDEN", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.7f);
            Strike(0.8f, 0.3f);
            yield return Cut(1.2f, u => Static(HeroHead() + new Vector3(1.4f, 0.1f, -1.2f), HeroHead(), 30f), 0.01f);
            hero.CinePose(new Vector3(FortressPlan.HallX0 + 6f, FortressPlan.Up, 0f));
            SetArena(FortressPlan.Zones[4].x0, FortressPlan.Zones[4].x1);
            FortressCheckpoint = 5;
            CineOff();
            barsPx = 0f;
            warden.PuppetRelease();
            warden.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            wardenStage = 1;
            ShowStreetBanner("I  ·  THE CHAIN");
            Sound.Music("boss", 0.65f, true, 1.0f);
            Sound.Tension(true);
        }

        void UpdateWarden()
        {
            if (warden == null || !warden.Alive) return;
            float hp = warden.HealthFraction;
            if (wardenStage == 1 && hp <= 0.6f)
            {
                wardenStage = 2;
                warden.DriverPhase = 1;
                warden.ShowIron(false);
                Sound.Play(Sfx.PipeHit, 0.8f, warden.Chest, 0.8f);
                ShowStreetBanner("II  ·  THE CHANDELIERS");
                Say("THE WARDEN", "Mind your head.", 1.6f, HeadOf(warden));
                nextChandelier = Time.time + 1.2f;
            }
            else if (wardenStage == 2 && hp <= 0.3f)
            {
                wardenStage = 3;
                warden.DriverPhase = 3;
                warden.QueueEnrage();
                ShowStreetBanner("III  ·  BARE HANDS");
            }
            if (wardenStage == 2 && Time.time >= nextChandelier)
            {
                nextChandelier = Time.time + Random.Range(5f, 7.5f);
                DropChandelier(hero.transform.position.x + Random.Range(-0.4f, 0.4f));
            }
        }

        /// <summary>A chandelier cut loose over him: its shadow on the floor first, then it comes down.</summary>
        void DropChandelier(float x)
        {
            var root = new GameObject("FallingChandelier").transform;
            root.position = new Vector3(x, FortressPlan.Up + 7.4f, 0f);
            var ink = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.05f, 0.05f, 0.06f) };
            var white = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(root, false);
                bar.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.9f, 0f, Mathf.Sin(a) * 0.9f);
                bar.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                bar.transform.localScale = new Vector3(0.06f, 0.06f, 0.5f);
                bar.GetComponent<Renderer>().sharedMaterial = ink;
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(root, false);
                c.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.9f, 0.12f, Mathf.Sin(a) * 0.9f);
                c.transform.localScale = new Vector3(0.05f, 0.18f, 0.05f);
                c.GetComponent<Renderer>().sharedMaterial = white;
            }
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(shadow.GetComponent<Collider>());
            shadow.name = "ChandelierShadow";
            shadow.transform.position = new Vector3(x, FortressPlan.Up + 0.02f, 0f);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(2.2f, 1.2f, 1f);
            shadow.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0f, 0f, 0f) };
            Sound.Play(Sfx.Chain, 0.9f, root.position);
            chandeliers.Add(root);
            chandeliers.Add(shadow.transform);
            StartCoroutine(FallChandelier(root, shadow.transform));
        }

        IEnumerator FallChandelier(Transform c, Transform shadow)
        {
            yield return new WaitForSeconds(0.9f);
            float v = 0f;
            while (c != null && c.position.y > FortressPlan.Up + 0.15f)
            {
                v += 22f * Time.deltaTime;
                c.position += Vector3.down * v * Time.deltaTime;
                yield return null;
            }
            if (c == null) yield break;
            var at = c.position;
            Sound.Play(Sfx.MetalClang, 1f, at);
            Sound.Play(Sfx.Glass, 0.8f, at);
            if (cam != null) cam.Shake(0.25f, 0.3f);
            if (InkFx.Instance != null) InkFx.Instance.Splash(at + Vector3.up * 0.2f, Vector3.up, 30, new Color(0.9f, 0.9f, 0.9f));
            if (Mathf.Abs(hero.transform.position.x - at.x) < 1.2f && !hero.Invulnerable)
                hero.TakeHit(new HitInfo { damage = 18f, direction = hero.transform.position.x < at.x ? -1 : 1, knockback = 5f, knockdown = true, heavy = true });
            if (warden != null && warden.Alive && Mathf.Abs(warden.transform.position.x - at.x) < 1.2f)
            {
                warden.TakeHit(new HitInfo { damage = 20f, direction = 1, knockback = 3f, heavy = true, hitsDowned = true });
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(200, "HIS OWN CHANDELIER!");
            }
            if (shadow != null) Destroy(shadow.gameObject);
            c.rotation = Quaternion.Euler(Random.Range(-14f, 14f), 0f, Random.Range(-10f, 10f));
        }

        void UpdateChandeliers() { }

        IEnumerator AfterWarden()
        {
            Sound.Tension(false);
            Sound.Music("storm", 0.45f, true, 3f);
            foreach (var e in Enemy.All.ToArray()) if (e != null && e != warden && e.Alive) e.PuppetDie();
            yield return RealWait(0.6f);
            hero.CinePuppet();
            barsPx = 92f;
            var at = new Vector3(145f, FortressPlan.Up, 1.6f);
            if (warden != null)
            {
                warden.Puppet();
                warden.PuppetPose(at);
                warden.SetFacing(-1);
                warden.PuppetPlay(warden.body.Has("lie_up") ? "lie_up" : "lie", 0.2f);
            }
            hero.CinePose(at + new Vector3(-1.2f, 0f, -1.4f));
            hero.SetFacing(1);
            hero.body.Play("idle", 0.2f);
            var wh = warden != null ? HeadOf(warden) : null;
            // (a) him down against the table, laughing
            Sound.Play(Sfx.BruteLaugh, 0.7f, at);
            Say("THE WARDEN", "Go on up. He's waiting. He's been waiting years.", 3.4f, wh);
            yield return Cut(3.6f, u => Static(at + new Vector3(1.6f, 0.5f, -1.8f), wh != null ? wh.position : at + Vector3.up * 0.6f, 32f), 0.015f);
            // (b) far above, through the storm: the tower, lit by a strike
            Strike(0.9f, 0.2f);
            yield return Cut(1.6f, u => CineCam.TiltUp(new Vector3(147f, FortressPlan.Up + 1.0f, -2.0f), new Vector3(148f, FortressPlan.Up + 2.0f, 2f), new Vector3(150f, FortressPlan.Up + 9f, 4f), u, 42f), 0.03f);
            // (c) on him: his head snaps up
            Sound.Play(Sfx.Heartbeat, 0.9f);
            yield return Cut(2.0f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.5f, 0.15f, 1f), u, 1.8f, 1.0f, 30f), 0.01f);
        }

        // ================================================================ the tower

        IEnumerator TheTower()
        {
            hero.CinePuppet();
            barsPx = 92f;
            Sound.Tension(false);
            // they are up there: a man at the rail holding a child (lit only by the lightning)
            var gal = new Vector3(FortressPlan.TowerX, FortressPlan.GalleryY, FortressPlan.TowerZ - 4.6f);
            if (director != null)
            {
                towerMan = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                towerGirl = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                if (towerMan != null) { towerMan.Puppet(); towerMan.transform.localScale = Vector3.one * 1.1f; towerMan.PuppetPose(gal); towerMan.SetFacing(-1); towerMan.PuppetPlay("idle", 0f); }
                if (towerGirl != null)
                {
                    towerGirl.Puppet();
                    towerGirl.SetBaseColor(new Color(0.86f, 0.86f, 0.85f));
                    towerGirl.transform.localScale = Vector3.one * 0.58f;
                    towerGirl.PuppetPose(gal + new Vector3(-0.5f, 0.55f, -0.1f));
                    towerGirl.SetFacing(-1);
                    towerGirl.PuppetPlay(towerGirl.body.Has("struggle") ? "struggle" : "idle", 0f);
                }
            }
            hero.CinePose(new Vector3(146f, FortressPlan.Up, -1.0f));
            hero.SetFacing(1);
            fZone = 5;
            // (a) outside, low, up the tower in the rain, a strike behind it
            var foot = new Vector3(FortressPlan.TowerX - 9f, FortressPlan.Up + 0.6f, -6f);
            Strike(1f, 0.3f);
            yield return Cut(3.0f, u => CineCam.TiltUp(foot, new Vector3(FortressPlan.TowerX, FortressPlan.Up + 6f, 6f), gal + Vector3.up * 1f, u, 46f), 0.03f);
            // (b) a dolly zoom on the gallery: for a flash, the man and the child
            Strike(1f, 0.1f);
            yield return Cut(2.6f, u => CineCam.DollyZoom(gal + Vector3.up * 0.9f, u, 18f, 8f, 3.4f), 0.02f);
            // (c2) the walkie in his pocket clicks on: she sees him
            Sound.Play(Sfx.Static, 0.6f, hero.Chest, 1.6f);
            HoldWalkie(true);
            Walkie("Papa! I see you! You're tiny!", 2.6f);
            yield return Cut(2.8f, u => Static(HeroHead() + new Vector3(0.45f, -0.12f, -0.6f), HeroHead() + Vector3.down * 0.1f, 28f), 0.015f);
            Walkie("The cane man is taking me up to the roof.", 2.8f);
            yield return Cut(3.0f, u => Static(gal + new Vector3(-2.6f, -1.4f, -3.4f), gal + Vector3.up * 1.0f, 30f), 0.03f);
            Say("YOU", "I see you too. I'm coming up. Over.", 2.6f);
            yield return Cut(2.8f, u => Static(HeroHead() + new Vector3(0.45f, -0.12f, -0.6f), HeroHead() + Vector3.down * 0.1f, 28f), 0.015f);
            HoldWalkie(false);
            // (c) his voice, down through the storm
            Say("VOICE, FROM THE TOWER", "Come up, old friend. She's been asking for you.", 3.6f);
            yield return Cut(3.6f, u => Static(gal + new Vector3(-2.6f, -1.4f, -3.4f), gal + Vector3.up * 1.0f, 30f), 0.03f, u => { if (u > 0.5f && u - Time.unscaledDeltaTime / 3.6f <= 0.5f) Strike(0.9f, 0.1f); });
            // (d) on the hero, close, lightning across his face
            Strike(1f, 0.0f);
            yield return Cut(2.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.4f, 0.25f, 1f), u, 1.6f, 1.0f, 28f), 0.01f);
            Say("YOU", "...I'm coming.", 2.0f);
            yield return Cut(2.0f, u => Static(HeroHead() + new Vector3(0.6f, 0.1f, -1.0f), HeroHead(), 28f), 0.01f);
            // (e) pull away: small at the foot of the tower in the storm
            yield return Cut(4.0f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 1.0f, u, 2.4f, 22f, 0.8f, 16f, 44f), 0.04f,
                u => { if (u > 0.4f && u - Time.unscaledDeltaTime / 4f <= 0.4f) Strike(1f, 0.4f); });
            if (towerMan != null) towerMan.gameObject.SetActive(false);
            if (towerGirl != null) towerGirl.gameObject.SetActive(false);
        }

        // ================================================================ on screen

        void FortressGUI(float w, float h)
        {
            // the blackout: dark, except when the lightning is up
            if (fBlackout && fZone <= 3 && cardAt < 0f)
            {
                bool lit = LightningSwap.Instance != null && LightningSwap.Instance.Lit;
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0.01f, lit ? 0.05f : 0.82f));
            }
            if (lensRain > 0.01f && cardAt < 0f) Rain(w, h, lensRain);
            if (phase == Phase.Fortress && fClear && fZone < 4 && Time.unscaledTime < fGoUntil && !hero.CinematicLock)
            {
                float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
                prompt.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(w - 260, h * 0.42f, 160, 40), Spaced("GO"), prompt);
                for (int i = 0; i < 3; i++)
                {
                    float k = Mathf.Repeat(Time.unscaledTime * 1.4f - i * 0.25f, 1f);
                    Chevron(new Vector2(w - 120 + i * 26, h * 0.42f + 20), new Color(1f, 1f, 1f, a * (1f - k)));
                }
            }
            if (fortTitleAt > 0f)
            {
                float t0 = Time.unscaledTime - fortTitleAt;
                float a = Mathf.Clamp01(t0 / 0.25f) * Mathf.Clamp01((3.2f - t0) / 0.8f);
                if (a > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 22;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    GUI.Label(new Rect(0, h * 0.38f, w, 30), Spaced("CHAPTER 6"), cap);
                    int bs = big.fontSize;
                    big.fontSize = 84;
                    big.normal.textColor = new Color(1f, 1f, 1f, a);
                    GUI.Label(new Rect(0, h * 0.38f + 30, w, 96), Spaced("THE FORTRESS"), big);
                    big.fontSize = bs;
                    Fill(new Rect(w * 0.5f - 120 * a, h * 0.38f + 128, 240 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            if (warden != null && wardenStage >= 1 && wardenStage < 5 && warden.Alive)
            {
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 54f;
                Fill(new Rect(x - 2, y - 2, bw + 4, 14), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(x, y, bw * warden.HealthFraction, 10), new Color(0.85f, 0.85f, 0.85f));
                foreach (float mk in new[] { 0.6f, 0.3f }) Fill(new Rect(x + bw * mk - 1, y - 3, 2, 16), new Color(0.8f, 0.06f, 0.07f));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                string ph = wardenStage >= 3 ? "III  ·  BARE HANDS" : wardenStage == 2 ? "II  ·  THE CHANDELIERS" : "I  ·  THE CHAIN";
                GUI.Label(new Rect(x, y - 30, bw, 26), Spaced("THE WARDEN") + "     " + ph, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            FinaleGUI(w, h);
            PromptsGUI(w, h);
        }
    }
}
