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
            // her ribbon in the grass under the swing: crumpled, the tails trailing
            ribbonProp = Ribbon.Lying(new Vector3(ParkPlan.RibbonX, 0.02f, 1.1f), 20f);
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
            wristBand = Ribbon.WristBand(arm);          // wound twice round his wrist, a small bow, the tails moving
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
            ParkCast();
            if (from >= 2) AttachRibbon();
            if (from <= 0)
            {
                // ================================================ ACT 1 · FIND HER
                // ------------------------------------------------ 4.0 black: the title, crickets, a swing creaking
                blackout = 1f;
                parkTitleAt = Time.unscaledTime;
                parkSwingFrom = Time.unscaledTime;
                Sound.PlayLater(Sfx.SwingCreak, 0.8f, 0.6f);
                yield return RealWait(3.4f);
                // ------------------------------------------------ 4.1 the small footprints across the wet lawn: a truck, low, beside him
                hero.CinePose(new Vector3(2.2f, 0f, 0f));
                hero.body.Play("walk", 0.2f, true, 0.8f);
                StartCoroutine(FadeIn(1.0f));
                yield return Move(4.2f, u => CineCam.Truck(new Vector3(1.4f, 0.45f, -2.6f), new Vector3(7.6f, 0.45f, -2.6f), new Vector3(1.4f, 0.1f, 2.6f), u, 44f),
                    u =>
                    {
                        hero.CinePose(new Vector3(Mathf.Lerp(2.2f, 8.2f, u), 0f, 0f));
                        if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.55f; Sound.Play(Sfx.Footstep, 0.25f, hero.transform.position, 0.8f); }
                        if (u > 0.5f && u - Time.unscaledDeltaTime / 4.2f <= 0.5f) Say("YOU", "...To the swings.", 2.2f);
                    });
                CineOff();
                yield return PlayerWalk(ParkPlan.RibbonX - 0.9f, 7.5f, 1.6f, Application.isMobilePlatform ? "HOLD TO FOLLOW THEM" : "HOLD  D  TO FOLLOW THEM");
            }
            if (from <= 1)
            {
                // ------------------------------------------------ 4.2 the swing stops; under it, her ribbon: a crane down. He pulls it free.
                if (from == 1) { hero.CinePose(new Vector3(ParkPlan.RibbonX - 0.9f, 0f, 0f)); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                parkSwingFrom = -1f;
                if (hero.body.Has("kneel_hand")) hero.body.Play("kneel_hand", 0.4f);
                var rib = ribbonProp.position;
                yield return Move(3.6f, u => CineCam.CraneDown(rib + Vector3.up * 0.1f, u, 3.2f, 0.5f, 1.6f, 40f));
                Say("YOU", "...Her ribbon.", 2.0f);
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(CineCam.CraneDown(rib + Vector3.up * 0.1f, 1f, 3.2f, 0.5f, 1.6f, 40f))));
                yield return DragTo("PULL IT FREE", Vector2.up, p => { if (ribbonProp != null) ribbonProp.position = rib + Vector3.up * 0.25f * p; }, rib);
                ribbonAt = Time.unscaledTime;
                Sound.Play(Sfx.MusicNote, 0.8f);
                yield return RealWait(3.4f);
                ribbonAt = -1f;
                if (ribbonProp != null) ribbonProp.gameObject.SetActive(false);
                // ------------------------------------------------ 4.3 a crackle in his coat: the walkie from her shelf
                var face = HeroHead();
                System.Action closeOn = () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.75f, -0.05f, -1.0f), HeroHead(), 30f));
                yield return Hold(1.0f, closeOn);
                Sound.Play(Sfx.Static, 0.6f, hero.Chest, 1.5f);
                yield return Hold(0.8f, closeOn);
                yield return WalkieLine("...Papa? Is that you? Over.", 2.6f, closeOn);
                hero.body.Play("idle", 0.3f);
                HoldWalkie(true);
                // ------------------------------------------------ 4.4 he answers: an extreme close on the walkie at his mouth
                System.Action atMouth = () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.45f, -0.12f, -0.55f), HeroHead() + Vector3.down * 0.12f, 26f));
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, atMouth));
                yield return HoldTo("HOLD TO TALK", 1.2f, null);
                Sound.Play(Sfx.Static, 0.3f, null, 2f);
                Say("YOU", "Maya. It's me. It's Papa.", 2.4f);
                yield return Hold(2.6f, atMouth);
                yield return WalkieLine("Papa!", 1.4f, closeOn);
                yield return WalkieLine("I knew you'd answer. Mama said you always answer.", 3.2f, closeOn);
                // ------------------------------------------------ 4.5 her story, in her words, over the stills
                yield return MayaStory();
                // ------------------------------------------------ 4.6 the ribbon round his wrist as he listens
                AttachRibbon();
                Sound.Play(Sfx.Cloth, 0.6f, hero.Chest);
                System.Action wrist = () => Cine(new CineCam.Shot((wristBand != null ? wristBand.position : hero.Chest) + new Vector3(0.1f, 0.25f, -0.6f), wristBand != null ? wristBand.position : hero.Chest, 26f));
                Say("YOU", "That was so clever, sweetheart.", 2.2f);
                yield return Hold(2.4f, wrist);
                StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, atMouth));
                yield return ChooseFrom("\"Are you hurt?\"", "\"Where are you now?\"", "\"I'm coming for you.\"");
                if (chosen == 0)
                {
                    Say("YOU", "Are you hurt?", 1.6f);
                    yield return Hold(1.8f, atMouth);
                    yield return WalkieLine("My knee. I was brave. I didn't cry... a lot.", 3.0f, closeOn);
                    Say("YOU", "Where are you now?", 1.8f);
                    yield return Hold(2.0f, atMouth);
                }
                else if (chosen == 1)
                {
                    Say("YOU", "Where are you now?", 1.8f);
                    yield return Hold(2.0f, atMouth);
                }
                else
                {
                    Say("YOU", "I'm coming for you. Where are you?", 2.4f);
                    yield return Hold(2.6f, atMouth);
                }
                yield return WalkieLine("In a van. It smells like petrol. I can see the swings through the little window...", 3.8f, closeOn);
                yield return WalkieLine("...and I can see you!", 2.0f, closeOn);
                // ------------------------------------------------ 4.7 he looks up. The reverse, down the whole dark park, on a long lens: the van, her face
                if (vanMaya != null) vanMaya.gameObject.SetActive(true);
                if (vanWindow != null) vanWindow.gameObject.SetActive(true);
                Sound.Play(Sfx.Heartbeat, 0.8f);
                var vw = VanWindow();
                yield return Move(2.6f, u => CineCam.DollyZoom(vw, u, 30f, 22f, 4.0f), null);
                Say("YOU", "I see you.", 1.8f);
                yield return Hold(2.0f, () => Cine(CineCam.DollyZoom(vw, 1f, 30f, 22f, 4.0f)));
                Say("YOU", "Stay down. Keep the walkie hidden. Papa's coming.", 3.0f);
                yield return Hold(3.2f, atMouth);
                Say("VOICE, IN THE VAN", "Who are you talking to? Get away from that window!", 2.4f);
                Sound.Play(Sfx.Static, 0.7f, null, 1.2f);
                yield return Hold(1.4f, closeOn);
                if (vanLights != null) vanLights.gameObject.SetActive(true);
                Sound.Play(Sfx.CarRev, 0.7f, new Vector3(ParkPlan.VanX, 1f, ParkPlan.VanZ));
                yield return Hold(1.4f, () => Cine(CineCam.DollyZoom(vw, 1f, 30f, 22f, 4.0f)));
                HoldWalkie(false);
                if (vanMaya != null) vanMaya.gameObject.SetActive(false);
            }
            if (from <= 2)
            {
                // ================================================ ACT 2 · REACH HER
                // ------------------------------------------------ 4.8 torches over the fence: a pan to them
                if (from == 2) { hero.CinePose(new Vector3(ParkPlan.RibbonX - 0.9f, 0f, 0f)); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                hero.body.Play("idle", 0.3f);
                var fence = new Vector3(PZ1(0) - 1f, 1.0f, 0.4f);
                Sound.Play(Sfx.Door, 0.6f, fence, 0.7f);
                Sound.Play(Sfx.Footstep, 0.7f, fence);
                yield return Move(2.0f, u => CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, u, 40f));
                Say("THUG", "There he is! Take the book. Break his legs.", 2.6f);
                yield return Hold(1.8f, () => Cine(CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, 1f, 40f)));
            }
            if (from <= 3)
            {
                // ------------------------------------------------ 4.9 the fight out through the park, the van always at the far end
                int start = from == 3 && BackyardCheckpoint >= 0 && BackyardCheckpoint < 3 ? BackyardCheckpoint : 0;
                if (from == 3) yield return FadeIn(0.4f);
                if (vanLights != null) vanLights.gameObject.SetActive(true);
                StartParkZone(start, from == 3);
                while (pZone < 2 || !pClear) yield return null;
                // ================================================ ACT 3 · LOSE HER
                yield return PlayerWalk(PZ0(3) + 2.5f, PZ0(2) + 1f, 3.2f, Application.isMobilePlatform ? "HOLD TO RUN" : "HOLD  D  TO RUN");
                yield return LoseHer();
            }
            if (from <= 4)
            {
                // ------------------------------------------------ 4.12-4.13 THE HANDLER
                if (from == 4) { pZone = 3; SetArena(PZ0(3) + 0.6f, PZ1(3) - 0.6f); parkMen.Clear(); if (van != null) van.gameObject.SetActive(false); hero.CinePose(new Vector3(PZ0(3) + 4f, 0f, 0f)); yield return FadeIn(0.4f); }
                yield return HandlerArrives();
                while (handlerStage < 5) yield return null;
            }
            // ---------------------------------------------------- 4.14 the address
            if (from == 5)
            {
                if (van != null) van.gameObject.SetActive(false);
                SpawnHandler(new Vector3(ParkPlan.VanX - 3.4f, 0f, 0.6f));
                yield return FadeIn(0.3f);
            }
            if (from <= 5) yield return TheAddress();
            else { hero.CinePose(new Vector3(ParkPlan.VanX - 4f, 0f, 0f)); yield return FadeIn(0.4f); }
            // ---------------------------------------------------- 4.15 the hill
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
                            StartCoroutine(BetweenStretches(pZone));
                            if (pZone < 2)
                            {
                                SetArena(PZ0(pZone) + 0.6f, PZ1(pZone + 1) - 0.6f);
                                if (director != null) director.ShowBanner("CLEAR", 1.2f);
                                pGoUntil = Time.unscaledTime + 30f;
                            }
                        }
                    }
                }
            }
            else if (pZone < 2 && hero.transform.position.x > PZ0(pZone + 1) + 1.4f) StartParkZone(pZone + 1, false);
        }

        // ================================================================ THE HANDLER

        void SpawnHandler(Vector3 at)
        {
            WaveDirector.BossWeapon = FightStyle.Fists;
            WaveDirector.StoryBoss = true;
            handler = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            if (handler == null) return;
            handler.MakeHandler(handler.maxHealth * 0.8f);
            Dress(handler.body, Look.Handler);
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
            // slow clapping behind him
            for (int i = 0; i < 5; i++) Sound.PlayLater(Sfx.PunchLight, 0.2f + i * 0.55f, 0.35f, new Vector3(ParkPlan.VanX - 3f, 1.2f, 1.8f));
            handler.PuppetPlay("walk", 0.2f, 0.75f);
            var h0 = handler.transform.position;
            yield return Move(3.4f, u => CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, u, 3.6f, -60f, 0f, 36f),
                u => handler.PuppetPose(Vector3.Lerp(h0, new Vector3(ParkPlan.VanX - 6.5f, 0f, 0f), u)));
            handler.PuppetPlay("idle", 0.2f);
            Say("THE HANDLER", "Close. So close. You always did cut it fine.", 2.8f, HeadOf(handler));
            yield return Hold(2.8f, () => Cine(CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, 1f, 3.6f, -60f, 0f, 36f)));
            Say("THE HANDLER", "I set up your jobs for ten years. Now I get the book. And the son's chair.", 3.6f, HeadOf(handler));
            yield return Hold(3.6f, () => Cine(CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, 1f, 3.6f, -60f, 0f, 36f)));
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
                Say("THE HANDLER", "Ten years I made you look good! You owe me a thank you!", 2.8f, HeadOf(handler));
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
            Say("YOU", "Where's the van going?", 2.0f);
            yield return Hold(2.2f, () => Cine(new CineCam.Shot(two + new Vector3(-0.2f, 0.4f, -3.8f), two, 34f)));
            Say("THE HANDLER", "...Kessel Hill. The old fort, with the observatory.", 3.2f, hh);
            yield return Move(3.4f, u => CineCam.DollyIn(hHead(), new Vector3(-1f, -0.15f, 0.7f), u, 2.6f, 1.4f, 32f));
            Say("THE HANDLER", "He won't hurt her. She was bait. We kept her by the swings till you came home.", 3.2f, hh);
            yield return Hold(3.4f, () => Cine(CineCam.DollyIn(hHead(), new Vector3(-1f, -0.15f, 0.7f), 1f, 2.6f, 1.4f, 32f)));
            Say("THE HANDLER", "He pulled you off the street at nineteen. You were more his son than Silas ever was...", 4.0f, hh);
            yield return Hold(4.2f, () => Cine(CineCam.DollyIn(hHead(), new Vector3(-1f, -0.15f, 0.7f), 1f, 2.6f, 1.4f, 32f)));
            Say("THE HANDLER", "...and you just killed Silas.", 2.4f, hh);
            Sound.Play(Sfx.Heartbeat, 0.8f);
            yield return Hold(2.6f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(1.4f, 0.1f, -1.1f), HeroHead(), 32f)));
            Say("YOU", "I'm not his son.", 2.0f);
            yield return Hold(2.2f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(1.1f, 0.05f, -0.95f), HeroHead(), 30f)));
            Say("YOU", "I'm hers.", 2.0f);
            yield return Hold(2.4f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(0.9f, 0.0f, -0.8f), HeroHead(), 28f)));
            // he lets go; the man slumps against the van
            if (handler != null) handler.PuppetPlay(handler.body.Has("lie_up") ? "lie_up" : "lie", 0.3f);
            Sound.Play(Sfx.BodyFall, 0.7f, at);
            // his car keys, off the man's belt
            Sound.Play(Sfx.KnifeGrab, 0.5f, at, 1.6f);
            Say("YOU", "Your car. I'm borrowing it.", 2.2f);
            yield return Hold(2.4f, () => Cine(new CineCam.Shot(HeroHead() + new Vector3(1.0f, 0.05f, -1.0f), HeroHead(), 30f)));
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
            Say("YOU", "I'm coming, sweetheart.", 2.4f);
            yield return Hold(2.6f, () => Cine(CineCam.TiltUp(camAt, wrist, ParkPlan.Fortress, 1f, 26f)));
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
            BackyardV2GUI(w, h);
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
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 96f;           // below the pause and ? buttons (top centre)
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
