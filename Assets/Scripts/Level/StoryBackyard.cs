using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>CHAPTER 4: THE BACKYARD (docs/story/chapter4_backyard.md). Night behind No. 14: the small
    /// footprints across the lawn, her ribbon under the swing (the game's one red), how she was taken,
    /// his promise, the men with torches through the broken gate, four stretches of fighting out
    /// through the park to the car park, THE HANDLER, and the name of the place: the fortress on
    /// Kessel Hill, lit on the horizon.
    ///
    /// Camera: a truck low beside him across the lawn, a crane down onto the ribbon, a slow arc round
    /// him for the promise, a pan to the torches at the fence, a low arc for the Handler, a held frame
    /// and a dolly in for the answer, a tilt from the ribbon on his wrist up to the fortress, a drone
    /// pulling away as he walks towards it (docs/story/camera_language.md).</summary>
    public partial class StoryIntro
    {
        /// <summary>Where to resume after a defeat: a stretch 0-3, 4 the Handler (-1: from the top).</summary>
        public static int BackyardCheckpoint = -1;
        /// <summary>TEST: 0 the garden, 1 the ribbon, 2 the promise, 3 the fight, 4 the Handler, 5 the address, 6 the hill.</summary>
        public static int BackyardFrom;

        static readonly Spawn[][][] ParkWaves =
        {
            new[] { new[] { S(Br, Rt), S(Br, Rt), S(Ru, Rt), S(Gr, Bg) } },
            new[] { new[] { S(Br, Rt), S(Br, Lt), S(Ru, Bg), S(Dm, Rt) },
                    new[] { S(Br, Rt), S(Gr, Rt), S(Ru, Bg) } },
            new[] { new[] { S(Br, Rt), S(Ru, Rt), S(Pi, Bg), S(Br, Lt) } },
            new[] { new[] { S(Br, Rt), S(Br, Bg), S(Ru, Rt) } },
        };
        static readonly int[][] ParkCaps = { new[] { 3 }, new[] { 3, 3 }, new[] { 3 }, new[] { 3 } };

        int pZone = -1, pWave, pQueued, handlerStage;
        bool pReady, pClear;
        float pNextSpawn, pDone = -1f, pGoUntil, ribbonAt = -1f, takenAt = -1f, parkTitleAt = -1f;
        readonly List<Enemy> parkMen = new List<Enemy>();
        Enemy handler;
        Transform ribbonProp, parkSwing, vanDoors, wristBand;
        Texture2D ribbonTex;
        Texture2D[] takenStills;
        float parkSwingFrom = -1f;
        Material torchMat;

        float PZ0(int z) => ParkPlan.ZoneX[z];
        float PZ1(int z) => ParkPlan.ZoneX[z + 1];

        // ================================================================ setting up

        void BeginBackyard()
        {
            phase = Phase.Park;
            HeadWound.Chapter = 3;
            Sound.Ambience(0f, 0.45f, 22000f);
            ParkProps();
            int from = BackyardFrom;
            BackyardFrom = 0;
            if (from == 0 && BackyardCheckpoint >= 0) from = BackyardCheckpoint >= 4 ? 4 : 3;
            StartCoroutine(Backyard(from));
        }

        void ParkProps()
        {
            var red = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.78f, 0.05f, 0.06f) };
            var dark = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.03f, 0.03f, 0.035f) };
            void Part(Transform p, Vector3 at, Vector3 size, Material m, float yaw = 0f)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(p, false);
                c.transform.localPosition = at;
                c.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                c.transform.localScale = size;
                c.GetComponent<Renderer>().sharedMaterial = m;
            }
            // her ribbon in the grass under the swing
            ribbonProp = new GameObject("Ribbon").transform;
            ribbonProp.position = new Vector3(ParkPlan.RibbonX, 0.02f, 1.1f);
            Part(ribbonProp, new Vector3(-0.07f, 0f, 0f), new Vector3(0.12f, 0.02f, 0.07f), red, 20f);
            Part(ribbonProp, new Vector3(0.07f, 0f, 0f), new Vector3(0.12f, 0.02f, 0.07f), red, -20f);
            Part(ribbonProp, new Vector3(0.02f, 0f, -0.08f), new Vector3(0.03f, 0.015f, 0.14f), red, 15f);
            // her footprints across the lawn, small and bare, to the swing
            var prints = new GameObject("SmallPrints").transform;
            for (int i = 0; i < 26; i++)
                Part(prints, new Vector3(3.5f + i * 0.85f, 0.006f, 0.55f + (i % 2) * 0.16f), new Vector3(0.12f, 0.004f, 0.055f), dark);
            parkSwing = Find("Swing");
            if (parkSwing != null)
            {
                var pivot = new GameObject("SwingPivot").transform;
                pivot.position = new Vector3(ParkPlan.SwingX - 0.8f, 2.4f, ParkPlan.SwingZ);
                parkSwing.SetParent(pivot, true);
                parkSwing = pivot;
            }
            vanDoors = Find("VanDoors");
            EnsureTorchMat();
        }

        /// <summary>Her ribbon, round his wrist: a small red band, from here on.</summary>
        void AttachRibbon()
        {
            if (wristBand != null || hero.body == null) return;
            var arm = hero.body.Bone("forearm.L");
            if (arm == null) return;
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(c.GetComponent<Collider>());
            c.name = "Ribbon";
            c.transform.SetParent(arm, false);
            c.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            c.transform.localScale = new Vector3(0.11f, 0.018f, 0.11f);
            c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.78f, 0.05f, 0.06f) };
            wristBand = c.transform;
        }

        void EnsureTorchMat()
        {
            if (torchMat != null) return;
            torchMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { color = new Color(0.35f, 0.35f, 0.35f, 0.5f) };
            // a soft beam: bright at the torch, widening and fading
            var beam = new Texture2D(64, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float v = y / 127f, half = 0.12f + 0.38f * v, d = Mathf.Abs(x / 63f - 0.5f);
                    float a = Mathf.Clamp01(1f - d / half) * (1f - v) * (1f - v);
                    beam.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            beam.Apply();
            torchMat.mainTexture = beam;
        }

        /// <summary>A torch in a man's hand: a pale cone ahead of him.</summary>
        void GiveTorch(Enemy e)
        {
            EnsureTorchMat();
            var hand = e.body != null ? e.body.Bone("hand.R") : null;
            if (hand == null) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "TorchBeam";
            q.transform.SetParent(e.transform, false);
            q.transform.localPosition = new Vector3(1.6f, 0.9f, 0f);
            q.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            q.transform.localScale = new Vector3(1.2f, 2.8f, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = torchMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ================================================================ the chapter

        IEnumerator Backyard(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            Sound.Play(Sfx.Crickets, 0.5f);
            if (from >= 3) AttachRibbon();
            if (from <= 0)
            {
                // ------------------------------------------------ Y1 black: the title, crickets, a swing creaking
                blackout = 1f;
                parkTitleAt = Time.unscaledTime;
                parkSwingFrom = Time.unscaledTime;
                Sound.PlayLater(Sfx.SwingCreak, 0.8f, 0.6f);
                yield return RealWait(3.4f);
                // ------------------------------------------------ Y2 the small footprints across the lawn: a truck, low, beside him
                hero.CinePose(new Vector3(2.2f, 0f, 0f));
                hero.body.Play("walk", 0.2f, true, 0.8f);
                StartCoroutine(FadeIn(1.0f));
                yield return Move(4.2f, u => CineCam.Truck(new Vector3(1.4f, 0.45f, -2.6f), new Vector3(7.6f, 0.45f, -2.6f), new Vector3(1.4f, 0.1f, 2.6f), u, 44f),
                    u =>
                    {
                        hero.CinePose(new Vector3(Mathf.Lerp(2.2f, 8.2f, u), 0f, 0f));
                        if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.25f, hero.transform.position, 0.8f); }
                    });
                CineOff();
                yield return PlayerWalk(ParkPlan.RibbonX - 0.9f, 7.5f, 1.6f, Application.isMobilePlatform ? "HOLD TO FOLLOW THEM" : "HOLD  D  TO FOLLOW THEM");
            }
            if (from <= 1)
            {
                // ------------------------------------------------ Y3 the swing stops; under it, her ribbon: the crane comes down to it
                if (from == 1) { hero.CinePose(new Vector3(ParkPlan.RibbonX - 0.9f, 0f, 0f)); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                parkSwingFrom = -1f;
                if (hero.body.Has("kneel_hand")) hero.body.Play("kneel_hand", 0.4f);
                var rib = ribbonProp.position;
                yield return Move(3.6f, u => CineCam.CraneDown(rib + Vector3.up * 0.1f, u, 3.2f, 0.5f, 1.6f, 40f));
                yield return WaitTap("HER RIBBON", Application.isMobilePlatform ? "TAP" : "TAP  /  CLICK", () => Cine(CineCam.CraneDown(rib + Vector3.up * 0.1f, 1f, 3.2f, 0.5f, 1.6f, 40f)));
                ribbonAt = Time.unscaledTime;
                Sound.Play(Sfx.MusicNote, 0.8f);
                yield return RealWait(3.4f);
                ribbonAt = -1f;
                // ------------------------------------------------ Y4 how she was taken: broken stills
                takenAt = Time.unscaledTime;
                Sound.Play(Sfx.Static, 0.6f);
                float tt = 0f;
                bool cry = false, door = false;
                while (tt < 7.4f)
                {
                    tt += Time.unscaledDeltaTime;
                    if (!cry && tt > 3.6f) { cry = true; Say("MAYA", "PAPA!", 2.0f); Sound.Play(Sfx.Static, 0.6f); Buzz(); }
                    if (!door && tt > 5.8f) { door = true; Sound.Play(Sfx.CarDoor, 1f, null, 0.8f); }
                    yield return null;
                }
                takenAt = -1f;
                if (ribbonProp != null) ribbonProp.gameObject.SetActive(false);
            }
            if (from <= 2)
            {
                // ------------------------------------------------ Y5 the promise: the ribbon round his wrist; an arc round him, the house behind
                if (from == 2) { hero.CinePose(new Vector3(ParkPlan.RibbonX - 0.9f, 0f, 0f)); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                AttachRibbon();
                hero.body.Play(hero.body.Has("kneel_hand") ? "kneel_hand" : "idle", 0.3f);
                Sound.Play(Sfx.MemoryMaya, 0.45f);
                var him = hero.transform.position + Vector3.up * 0.75f;
                bool said1 = false, said2 = false, stood = false;
                yield return Move(9.0f, u => CineCam.ArcShot(him, u, 2.4f, 40f, -50f, 0.15f, 36f),
                    u =>
                    {
                        if (!said1 && u > 0.12f) { said1 = true; Say("YOU", "I will find you, wherever you are in the world, and bring you back.", 4.0f); }
                        if (!said2 && u > 0.6f) { said2 = true; Say("YOU", "Trust me, my sweetheart.", 3.0f); }
                        if (!stood && u > 0.82f) { stood = true; hero.body.Play("idle", 0.5f); }
                    });
                // ------------------------------------------------ Y6 torches through the fence slats: a pan to them
                var fence = new Vector3(34f, 1.0f, 0.4f);
                Sound.Play(Sfx.Door, 0.6f, fence, 0.7f);
                Sound.Play(Sfx.Footstep, 0.7f, fence);
                yield return Move(2.0f, u => CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, u, 40f));
                Say("VOICE, BY THE FENCE", "There! He's in the garden!", 2.4f);
                yield return Hold(1.4f, () => Cine(CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, 1f, 40f)));
            }
            if (from <= 3)
            {
                // ------------------------------------------------ Y7-Y9 the fight, out through the park
                int start = from == 3 && BackyardCheckpoint >= 0 && BackyardCheckpoint < 4 ? BackyardCheckpoint : 0;
                if (from == 3) yield return FadeIn(0.4f);
                StartParkZone(start, from == 3);
                while (pZone < 3 || !pClear) yield return null;
            }
            if (from <= 4)
            {
                // ------------------------------------------------ Y10-Y11 THE HANDLER
                if (from == 4) { StartParkZone(3, true); pClear = true; parkMen.Clear(); yield return FadeIn(0.4f); }
                yield return HandlerArrives();
                while (handlerStage < 5) yield return null;
            }
            // ---------------------------------------------------- Y12 the address
            if (from == 5)
            {
                SpawnHandler(new Vector3(ParkPlan.VanX - 3.4f, 0f, 0.6f));
                yield return FadeIn(0.3f);
            }
            if (from <= 5) yield return TheAddress();
            else { hero.CinePose(new Vector3(112f, 0f, 0f)); yield return FadeIn(0.4f); }
            // ---------------------------------------------------- Y13-Y15 the hill
            yield return TheHill();
        }

        // ================================================================ the fight

        void StartParkZone(int z, bool cut)
        {
            BackyardCheckpoint = z;
            pZone = z;
            pWave = 0;
            pQueued = 0;
            pClear = false;
            pReady = true;
            pDone = -1f;
            pNextSpawn = Time.time + (cut ? 1.2f : 0.4f);
            parkMen.Clear();
            phase = Phase.Park;
            SetArena(PZ0(z) + 0.6f, PZ1(z) - 0.6f);
            if (director != null)
            {
                float x0 = PZ0(z), x1 = PZ1(z), mid = (x0 + x1) * 0.5f;
                var pts = director.spawnPoints;
                void At(SpawnKind k, Vector3 p) { var t = pts[(int)k]; if (t != null) t.position = p; }
                At(SpawnKind.RightDoor, new Vector3(x1 - 0.8f, 0f, 0f));
                At(SpawnKind.LeftDoor, new Vector3(x0 + 0.8f, 0f, 0f));
                At(SpawnKind.Background, new Vector3(mid + 2f, 0f, 4f));
                At(SpawnKind.UpperDoor, new Vector3(mid, 0f, 4f));
                At(SpawnKind.Ceiling, new Vector3(mid, 0f, 4f));
                director.ShowBanner($"{z + 1}  ·  {ParkPlan.ZoneName[z]}", 2.0f);
            }
            if (cut || hero.CinematicLock)
            {
                if (cut) hero.CinePose(new Vector3(PZ0(z) + 1.6f, 0f, 0f));
                hero.SetFacing(1);
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                barsPx = 0f;
                CineOff();
            }
            Sound.Tension(true);
        }

        void UpdatePark()
        {
            if (parkSwing != null)
            {
                float t0 = parkSwingFrom < 0f ? 0f : Time.unscaledTime - parkSwingFrom;
                float amp = parkSwingFrom < 0f ? 0f : 22f * Mathf.Exp(-t0 * 0.15f);
                parkSwing.rotation = Quaternion.Slerp(parkSwing.rotation, Quaternion.Euler(amp * Mathf.Sin(t0 * 2.0f), 0f, 0f), 0.2f);
            }
            if (handlerStage > 0 && handlerStage < 5) { UpdateHandler(); return; }
            if (!pReady || pZone < 0 || hero.IsDead) return;
            parkMen.RemoveAll(e => e == null || !e.Alive);
            var waves = ParkWaves[pZone];
            if (!pClear)
            {
                var list = waves[pWave];
                if (pQueued < list.Length && parkMen.Count < ParkCaps[pZone][pWave] && Time.time >= pNextSpawn && director != null)
                {
                    var sp = list[pQueued];
                    var e = director.SpawnScripted(sp.type, sp.kind);
                    if (e != null) { parkMen.Add(e); if (Random.value < 0.5f) GiveTorch(e); }
                    pQueued++;
                    pNextSpawn = Time.time + Random.Range(0.6f, 1.3f);
                }
                if (pQueued >= list.Length && parkMen.Count == 0)
                {
                    if (pDone < 0f) pDone = Time.time;
                    if (Time.time - pDone > 1.1f)
                    {
                        pDone = -1f;
                        if (pWave + 1 < waves.Length) { pWave++; pQueued = 0; pNextSpawn = Time.time + 0.8f; if (director != null) director.ShowBanner("MORE OF THEM", 1.4f); }
                        else
                        {
                            pClear = true;
                            Sound.Play(Sfx.WaveClear, 0.6f);
                            if (pZone < 3)
                            {
                                SetArena(PZ0(pZone) + 0.6f, PZ1(pZone + 1) - 0.6f);
                                if (director != null) director.ShowBanner("CLEAR", 1.2f);
                                pGoUntil = Time.unscaledTime + 30f;
                                if (pZone == 0) Say("YOU", "They came this way. With her.", 2.6f);
                            }
                        }
                    }
                }
            }
            else if (pZone < 3 && hero.transform.position.x > PZ0(pZone + 1) + 1.4f) StartParkZone(pZone + 1, false);
        }

        // ================================================================ THE HANDLER

        void SpawnHandler(Vector3 at)
        {
            WaveDirector.BossWeapon = FightStyle.Fists;
            WaveDirector.StoryBoss = true;
            handler = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            if (handler == null) return;
            handler.MakeHandler(handler.maxHealth * 0.8f);
            handler.Puppet();
            handler.PuppetPose(at);
            handler.SetFacing(-1);
            Enemy.StoryFinale = (e, hit) => { if (handlerStage < 5) handlerStage = 5; };
        }

        IEnumerator HandlerArrives()
        {
            BackyardCheckpoint = 4;
            hero.CineLock();
            barsPx = 92f;
            SetArena(PZ0(3) + 0.6f, PZ1(3) - 0.6f);
            SpawnHandler(new Vector3(ParkPlan.VanX - 3.0f, 0f, 1.8f));
            if (handler == null) { handlerStage = 5; yield break; }
            if (vanDoors != null) Sound.Play(Sfx.CarDoor, 1f, vanDoors.position);
            handler.PuppetPlay("walk", 0.2f, 0.75f);
            var h0 = handler.transform.position;
            yield return Move(3.4f, u => CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, u, 3.6f, -60f, 0f, 36f),
                u => handler.PuppetPose(Vector3.Lerp(h0, new Vector3(ParkPlan.VanX - 6.5f, 0f, 0f), u)));
            handler.PuppetPlay("idle", 0.2f);
            Say("THE HANDLER", "She bit me, your girl. Kicked all the way to the van.", 3.4f, HeadOf(handler));
            yield return Hold(3.4f, () => Cine(CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, 1f, 3.6f, -60f, 0f, 36f)));
            if (director != null) director.ShowBanner("THE HANDLER", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.7f);
            yield return Hold(1.0f, () => Cine(CineCam.DollyIn(HeroHead(), new Vector3(1f, -0.1f, 0.8f), 1f, 2.6f, 1.6f, 34f)));
            CineOff();
            barsPx = 0f;
            handler.PuppetRelease();
            handler.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            handlerStage = 1;
            ShowStreetBanner("I  ·  THE GRIP");
            Sound.Tension(true);
        }

        void UpdateHandler()
        {
            if (handler == null || !handler.Alive) return;
            float hp = handler.HealthFraction;
            if (handlerStage == 1 && hp <= 0.6f)
            {
                handlerStage = 2;
                handler.HandlerKnuckles();
                Sound.Play(Sfx.MetalClang, 0.6f, handler.Chest, 1.4f);
                ShowStreetBanner("II  ·  THE KNUCKLES");
                Say("THE HANDLER", "Let's do this properly.", 1.8f, HeadOf(handler));
            }
            else if (handlerStage == 2 && hp <= 0.3f)
            {
                handlerStage = 3;
                handler.HandlerPhase = 3;
                handler.QueueEnrage();
                ShowStreetBanner("III  ·  CORNERED");
            }
        }

        // ================================================================ the address

        IEnumerator TheAddress()
        {
            Sound.Tension(false);
            foreach (var e in Enemy.All.ToArray()) if (e != null && e != handler && e.Alive) e.PuppetDie();
            yield return RealWait(0.6f);
            hero.CinePuppet();
            barsPx = 92f;
            var at = new Vector3(ParkPlan.VanX - 3.4f, 0f, 0.6f);
            if (handler != null)
            {
                handler.Puppet();
                handler.PuppetPose(at);
                handler.SetFacing(-1);
                handler.PuppetPlay(handler.body.Has("kneel_hurt") ? "kneel_hurt" : "idle", 0.2f);
            }
            hero.CinePose(at + new Vector3(-0.95f, 0f, -0.2f));
            hero.SetFacing(1);
            hero.body.Play(hero.body.Has("kneel_hold") ? "kneel_hold" : "idle", 0.2f);
            Sound.Play(Sfx.Breath, 0.8f, hero.Chest, 0.9f);
            var hh = handler != null ? HeadOf(handler) : null;
            System.Func<Vector3> hHead = () => hh != null ? hh.position : at + Vector3.up * 1.2f;
            var two = at + new Vector3(-0.5f, 0.8f, 0f);
            // held, wide: the two of them against the van
            yield return Hold(1.4f, () => Cine(new CineCam.Shot(two + new Vector3(-0.2f, 0.4f, -3.8f), two, 34f)));
            Say("YOU", "Where is she?", 2.0f);
            yield return Hold(2.2f, () => Cine(new CineCam.Shot(two + new Vector3(-0.2f, 0.4f, -3.8f), two, 34f)));
            Say("THE HANDLER", "...Kessel Hill. The old fort. The boss's place.", 3.2f, hh);
            yield return Move(3.4f, u => CineCam.DollyIn(hHead(), new Vector3(-1f, -0.15f, 0.7f), u, 2.6f, 1.4f, 32f));
            Say("YOU", "Who's the boss?", 2.0f);
            yield return Hold(2.2f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(1.4f, 0.1f, -1.1f), HeroHead(), 32f)));
            Say("THE HANDLER", "You'd know him. He knows you. He said you'd come.", 3.6f, hh);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Hold(3.8f, () => Cine(CineCam.DollyIn(hHead(), new Vector3(-1f, -0.15f, 0.7f), 1f, 2.6f, 1.4f, 32f)));
            // he lets go; the man slumps against the van
            if (handler != null) handler.PuppetPlay(handler.body.Has("lie_up") ? "lie_up" : "lie", 0.3f);
            Sound.Play(Sfx.BodyFall, 0.7f, at);
        }

        // ================================================================ the hill

        IEnumerator TheHill()
        {
            hero.CinePuppet();
            hero.body.Play("idle", 0.3f);
            hero.SetFacing(1);
            Sound.Play(Sfx.Wind, 0.5f);
            // from the red on his wrist, up to the fortress on the hill, its windows lit
            var wrist = wristBand != null ? wristBand.position : hero.transform.position + Vector3.up * 0.9f;
            var camAt = hero.transform.position + new Vector3(-1.6f, 1.1f, -1.6f);
            yield return Move(5.0f, u => CineCam.TiltUp(camAt, wrist, ParkPlan.Fortress, u, Mathf.Lerp(34f, 26f, u)));
            yield return Hold(1.8f, () => Cine(CineCam.TiltUp(camAt, wrist, ParkPlan.Fortress, 1f, 26f)));
            // he walks out of the car park towards it; the camera pulls away and up
            hero.body.Play("walk", 0.25f, true, 0.9f);
            var w0 = hero.transform.position;
            yield return Move(6.5f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 1.0f, u, 2.4f, 16f, 0.8f, 9f, 42f),
                u => hero.CinePose(Vector3.Lerp(w0, new Vector3(Mathf.Min(w0.x + 9f, 128.5f), 0f, 0f), u)));
            cardAt = Time.unscaledTime;
            cardLine = "I will find you, wherever you are.";
            cardTitle = "THE PROMISE";
            yield return RealWait(6.0f);
            CineOff();
            phase = Phase.End;
        }

        // ================================================================ on screen

        void ParkGUI(float w, float h)
        {
            if (phase == Phase.Park && pClear && pZone < 3 && handlerStage == 0 && Time.unscaledTime < pGoUntil)
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
            if (parkTitleAt > 0f)
            {
                float t0 = Time.unscaledTime - parkTitleAt;
                float a = Mathf.Clamp01(t0 / 0.8f) * Mathf.Clamp01((3.6f - t0) / 0.8f);
                if (a > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 22;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    GUI.Label(new Rect(0, h * 0.38f, w, 30), Spaced("CHAPTER 4"), cap);
                    int bs = big.fontSize;
                    big.fontSize = 78;
                    big.normal.textColor = new Color(1f, 1f, 1f, a);
                    GUI.Label(new Rect(0, h * 0.38f + 30, w, 96), Spaced("THE BACKYARD"), big);
                    big.fontSize = bs;
                    Fill(new Rect(w * 0.5f - 110 * a, h * 0.38f + 128, 220 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            if (ribbonAt > 0f)
            {
                if (ribbonTex == null) ribbonTex = Resources.Load<Texture2D>("Story/Memories/ribbon");
                float t0 = Time.unscaledTime - ribbonAt;
                float a = Mathf.Clamp01(t0 / 0.5f) * Mathf.Clamp01((3.4f - t0) / 0.5f);
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
                if (ribbonTex != null)
                {
                    float zoom = 1.02f + 0.06f * Mathf.Clamp01(t0 / 3.4f);
                    float iw = w * zoom, ih = iw * 9f / 16f;
                    if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                    var o = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, a);
                    GUI.DrawTexture(new Rect((w - iw) * 0.5f, (h - ih) * 0.5f, iw, ih), ribbonTex);
                    GUI.color = o;
                }
            }
            if (takenAt > 0f)
            {
                if (takenStills == null)
                {
                    takenStills = new Texture2D[4];
                    for (int i = 0; i < 4; i++) takenStills[i] = Resources.Load<Texture2D>($"Story/Flash/taken_{i + 1}");
                }
                float t0 = Time.unscaledTime - takenAt;
                float[] at = { 0f, 1.5f, 3.3f, 5.6f, 7.4f };
                Fill(new Rect(0, 0, w, h), Color.black);
                int idx = -1;
                for (int i = 0; i < 4; i++) if (t0 >= at[i] && t0 < at[i + 1]) idx = i;
                if (idx >= 0 && takenStills[idx] != null && Mathf.Repeat(t0 * 7f, 1f) > 0.07f)
                {
                    float jx = (Mathf.PerlinNoise(t0 * 9f, 0.2f) - 0.5f) * 14f, jy = (Mathf.PerlinNoise(0.6f, t0 * 9f) - 0.5f) * 10f;
                    float iw = w * 1.06f, ih = iw * 9f / 16f;
                    if (ih < h * 1.06f) { ih = h * 1.06f; iw = ih * 16f / 9f; }
                    GUI.DrawTexture(new Rect((w - iw) * 0.5f + jx, (h - ih) * 0.5f + jy, iw, ih), takenStills[idx]);
                }
            }
            if (handler != null && handlerStage >= 1 && handlerStage < 5 && handler.Alive)
            {
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 54f;
                Fill(new Rect(x - 2, y - 2, bw + 4, 14), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(x, y, bw * handler.HealthFraction, 10), new Color(0.85f, 0.85f, 0.85f));
                foreach (float mk in new[] { 0.6f, 0.3f }) Fill(new Rect(x + bw * mk - 1, y - 3, 2, 16), new Color(0.8f, 0.06f, 0.07f));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                string ph = handlerStage >= 3 ? "III  ·  CORNERED" : handlerStage == 2 ? "II  ·  THE KNUCKLES" : "I  ·  THE GRIP";
                GUI.Label(new Rect(x, y - 30, bw, 26), Spaced("THE HANDLER") + "     " + ph, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            PromptsGUI(w, h);
        }
    }
}
