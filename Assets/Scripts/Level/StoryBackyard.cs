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
        Material torchMat, torchGlowMat;
        /// <summary>The two men who come through the broken gate in 4.8: the first of the garden's fight.</summary>
        readonly List<Enemy> parkGateMen = new List<Enemy>();

        float PZ0(int z) => ParkPlan.ZoneX[z];
        float PZ1(int z) => ParkPlan.ZoneX[z + 1];
        /// <summary>Where he kneels for the ribbon: on the lane, the ribbon under his right (near) hand.</summary>
        static Vector3 RibbonKneel => new Vector3(ParkPlan.RibbonX - 0.4f, 0f, 0f);

        /// <summary>The reverse down the whole park on a long lens: from the swings, along the sightline the
        /// set keeps clear (ParkPlan.SightZ; build_park_set.py checks it with a ray), to the van's back window,
        /// the lens creeping in. The van's back faces the park, so its window is only seen from this end: from
        /// the camera's side (-z) it is edge on and the van shows its flank.</summary>
        static CineCam.Shot ParkLongLens(Vector3 window, float u)
        {
            var from = new Vector3(ParkPlan.SwingX + 2.6f, 1.45f, ParkPlan.SightZ);
            float d = Mathf.Max(1f, Vector3.Distance(from, window));
            float held = Mathf.Lerp(5.0f, 3.2f, CineCam.Ease(u));            // metres of the van's back in the frame
            return new CineCam.Shot(from, window, 2f * Mathf.Atan(held / (2f * d)) * Mathf.Rad2Deg);
        }

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
            // her ribbon in the grass by the swing: crumpled, the tails trailing. It lies where his kneeling
            // hand reaches (RibbonKneel: 0.4 m ahead of him, on his near side), the swing behind it in the crane
            // shot; at 1.1 m deep it was 1.4 m from the hand that "pulls it free".
            ribbonProp = Ribbon.Lying(new Vector3(ParkPlan.RibbonX, 0.02f, -0.15f), 20f);
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
            var add = Shader.Find("Legacy Shaders/Particles/Additive");
            torchMat = new Material(add);
            if (torchMat.HasProperty("_TintColor")) torchMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.48f, 0.32f));
            // a soft beam: narrow and bright at the lens (v 0), widening and fading out ahead (v 1)
            var beam = new Texture2D(64, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float v = y / 127f, half = 0.035f + 0.43f * v, d = Mathf.Abs(x / 63f - 0.5f);
                    float edge = Mathf.Clamp01(1f - d / half);
                    float a = edge * edge * Mathf.Pow(1f - v, 1.6f);
                    beam.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            beam.Apply();
            torchMat.mainTexture = beam;
            // the glow round the lens, seen from any side
            torchGlowMat = new Material(add);
            if (torchGlowMat.HasProperty("_TintColor")) torchGlowMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.48f, 0.45f));
            var glow = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float r = Mathf.Clamp01(new Vector2(x / 31f - 0.5f, y / 31f - 0.5f).magnitude * 2f);
                    glow.SetPixel(x, y, new Color(1f, 1f, 1f, (1f - r) * (1f - r)));
                }
            glow.Apply();
            torchGlowMat.mainTexture = glow;
        }

        /// <summary>A torch in a man's hand (TorchHold): the flashlight in his right fist, held out before him,
        /// its beam where he looks. Only for a man whose right hand is free: not a pipe, a blade, a gun or a
        /// knife in it, not the door-man's sheet of iron in front of him, not a grabber's two-handed hold, not
        /// a boss. Returns the hold (a scene can give it a point to look at), or null.</summary>
        TorchHold GiveTorch(Enemy e)
        {
            if (e == null || e.body == null || e.Boss) return null;
            if (e.Type != EnemyType.Brawler && e.Type != EnemyType.Runner) return null;
            EnsureTorchMat();
            return TorchHold.Give(e, torchMat, torchGlowMat);
        }

        // ================================================================ the chapter

        IEnumerator Backyard(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            Sound.Play(Sfx.Crickets, 0.5f);
            ParkCast();
            if (from >= 2)
            {
                AttachRibbon();
                if (ribbonProp != null) ribbonProp.gameObject.SetActive(false);   // on his wrist now, not in the grass as well
            }
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
                        if (u > 0.5f && u - StoryDt / 4.2f <= 0.5f) Say("YOU", "...To the swings.", 2.2f);
                    });
                CineOff();
                yield return PlayerWalk(RibbonKneel.x, 7.5f, 1.6f, Application.isMobilePlatform ? "HOLD TO FOLLOW THEM" : "HOLD  D  TO FOLLOW THEM");
            }
            if (from <= 1)
            {
                // ------------------------------------------------ 4.2 the swing stops; under it, her ribbon: a crane down. He pulls it free.
                if (from == 1) { hero.CinePose(RibbonKneel); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                hero.CinePose(RibbonKneel);                          // on his mark (the walk stops within a step of it), under the cut to the crane
                hero.SetFacing(1);
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
                // he stands, the walkie out of his coat and up to his mouth (not a boxer's guard: he's talking to his girl)
                hero.body.Play(hero.body.Has("talk_walkie") ? "talk_walkie" : hero.body.Has("idle_loose") ? "idle_loose" : "idle", 0.3f);
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
                yield return Move(2.6f, u => ParkLongLens(vw, u), null);
                Say("YOU", "I see you.", 1.8f);
                yield return Hold(2.0f, () => Cine(ParkLongLens(vw, 1f)));
                Say("YOU", "Stay down. Keep the walkie hidden. Papa's coming.", 3.0f);
                yield return Hold(3.2f, atMouth);
                Say("VOICE, IN THE VAN", "Who are you talking to? Get away from that window!", 2.4f);
                Sound.Play(Sfx.Static, 0.7f, null, 1.2f);
                yield return Hold(1.4f, closeOn);
                if (vanLights != null) vanLights.gameObject.SetActive(true);
                Sound.Play(Sfx.CarRev, 0.7f, new Vector3(ParkPlan.VanX, 1f, ParkPlan.VanZ));
                yield return Hold(1.4f, () => Cine(ParkLongLens(vw, 1f)));
                HoldWalkie(false);
                if (vanMaya != null) vanMaya.gameObject.SetActive(false);
            }
            if (from <= 2)
            {
                // ================================================ ACT 2 · REACH HER
                // ------------------------------------------------ 4.8 torches over the fence: a pan to them
                if (from == 2) { hero.CinePose(RibbonKneel); hero.SetFacing(1); yield return FadeIn(0.5f); }
                hero.CinePuppet();
                hero.body.Play("idle", 0.3f);
                var fence = new Vector3(PZ1(0) - 1f, 1.0f, 0.4f);
                Sound.Play(Sfx.Door, 0.6f, fence, 0.7f);
                Sound.Play(Sfx.Footstep, 0.7f, fence);
                // two of them come through the broken gate (its gap is z -1 to 1), torches on him: the pan was
                // to an empty gate, a voice from nobody. They are the garden's first two (StartParkZone takes them).
                ParkGateMen();
                yield return Move(2.0f, u => CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, u, 40f));
                Say("THUG", "There he is! Take the book. Break his legs.", 2.6f, parkGateMen.Count > 0 ? HeadOf(parkGateMen[0]) : null);
                yield return Hold(1.8f, () => Cine(CineCam.Pan(new Vector3(ParkPlan.RibbonX - 2.6f, 1.4f, -3.4f), HeroHead(), fence, 1f, 40f)));
            }
            if (from <= 3)
            {
                // ------------------------------------------------ 4.9 the fight out through the park, the van always at the far end
                int start = from == 3 && BackyardCheckpoint >= 0 && BackyardCheckpoint < 3 ? BackyardCheckpoint : 0;
                if (from == 3) yield return FadeIn(0.4f);
                if (vanLights != null) vanLights.gameObject.SetActive(true);
                StartParkZone(start, from == 3);
                ParkAdoptGateMen(start);
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
                At(SpawnKind.Background, ParkBackSpawn(z, mid));
                At(SpawnKind.UpperDoor, new Vector3(mid, 0f, 4f));
                At(SpawnKind.Ceiling, new Vector3(mid, 0f, 4f));
                director.ShowBanner($"{z + 1}  ·  {ParkPlan.ZoneName[z]}", 2.0f);
            }
            // the car park: one watching the van, his back to the park (StoryLookouts)
            if (z == 3) PostLookout(PZ0(z) + 9f, 0f, 1, parkMen, "ONE WATCHING THE VAN");
            // a railing bar wrenched off the park fence, lying in the grass
            if (z == 1) PlaceWeapon(FightStyle.Pipe, 14, new Vector3(PZ0(z) + 5f, 0f, 0f));
            else lookout = null;
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

        /// <summary>Where a man comes in from the dark behind the lane (he walks straight down to z 0, drifting
        /// up to a metre and a half towards the hero; the spawn adds +-0.4 in x). Each is a clear way in
        /// (build_park_set.py): mid + 2 at z 4 was the small slide's run-out in the garden (x 21.6-22.0),
        /// the playground's low railing itself (z 4.0, x 37-64.6), and the middle of the pond (x 70-94 at
        /// z 4: he stood on the water and walked through the footbridge's parapet at z 1.25).</summary>
        static Vector3 ParkBackSpawn(int zone, float mid)
        {
            switch (zone)
            {
                case 0: return new Vector3(19.6f, 0f, 4f);                   // the lawn by the stepping stones
                case 1: return new Vector3(39.9f, 0f, 4.12f);                // out through the play area's gate (x 39.2-40.6), in front of its AGES 5-12 sign (z 4.4)
                case 2: return new Vector3(95.6f, 0f, 4f);                   // the dry bank past the pond, clear of the footbridge's end (92.5)
                default: return new Vector3(mid + 2f, 0f, 4f);               // the car park's empty bays
            }
        }

        /// <summary>4.8: two men through the broken back gate from the park, walking in with their torches on
        /// him, and squaring up inside it.</summary>
        void ParkGateMen()
        {
            parkGateMen.Clear();
            if (director == null) return;
            var rd = director.spawnPoints[(int)SpawnKind.RightDoor];
            if (rd != null) rd.position = new Vector3(ParkPlan.GateX + 2f, 0f, 0f);     // their arrival is heard at the gate
            // in through the gap side by side (z +-0.32: the posts are at +-1.05), stopping inside the garden's
            // fight (its arena ends at 33.4: released past it they would snap back) and short of the bin (x 32.6, r 0.29)
            var walks = new List<(Enemy man, Vector3 a, Vector3 b)>();
            foreach (var (a, b) in new[] { (new Vector3(ParkPlan.GateX + 1.7f, 0f, 0.32f), new Vector3(ParkPlan.GateX - 0.85f, 0f, 0.32f)),
                                           (new Vector3(ParkPlan.GateX + 2.2f, 0f, -0.32f), new Vector3(ParkPlan.GateX - 0.62f, 0f, -0.32f)) })
            {
                var g = director.SpawnScripted(EnemyType.Brawler, SpawnKind.RightDoor);
                if (g == null) continue;
                g.Puppet();
                g.PuppetPose(a);
                g.SetFacing(-1);
                g.PuppetPlay("walk", 0.1f, 0.75f);
                var torch = GiveTorch(g);
                if (torch != null) torch.lookAt = () => hero.Chest;
                parkGateMen.Add(g);
                walks.Add((g, a, b));
            }
            float t = 0f;
            StartCoroutine(HoldFor(3.6f, () =>
            {
                t += StoryDt;
                float u = Mathf.SmoothStep(0f, 1f, t / 3.4f);
                foreach (var w in walks)
                {
                    if (w.man == null || !w.man.Alive) continue;
                    w.man.PuppetPose(Vector3.Lerp(w.a, w.b, u));
                    if (u >= 1f && w.man.body.CurrentName == "walk") w.man.PuppetPlay("idle", 0.25f);
                }
            }));
        }

        /// <summary>The gate's two men join the garden's fight as its first two (the wave's two brawlers).</summary>
        void ParkAdoptGateMen(int zone)
        {
            foreach (var g in parkGateMen)
            {
                if (g == null || !g.Alive) continue;
                var t = g.body != null ? g.body.GetComponentInChildren<TorchHold>() : null;
                if (t != null) t.lookAt = null;                              // from now on, where he faces
                if (zone != 0) { g.Vanish(); continue; }
                g.PuppetRelease();
                parkMen.Add(g);
                pQueued++;
            }
            parkGateMen.Clear();
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
                if (LookoutHolds) pNextSpawn = Mathf.Max(pNextSpawn, Time.time + 0.6f);      // the rest wait on the lookout
                if (pQueued < list.Length && parkMen.Count < ParkCaps[pZone][pWave] + (lookout != null && lookout.Alive ? 1 : 0) && Time.time >= pNextSpawn && director != null)
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
            var h1 = new Vector3(ParkPlan.VanX - 6.5f, 0f, 0f);
            // he walks the way he faces (the walk is 27 degrees off the lane: side on to it he crabbed), then
            // turns to the hero down the lane
            handler.LookAtXZ(h1);
            yield return Move(3.4f, u => CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, u, 3.6f, -60f, 0f, 36f),
                u => handler.PuppetPose(Vector3.Lerp(h0, h1, u)));
            handler.PuppetPlay("idle", 0.2f);
            yield return EpTurnTo(handler, h1 + Vector3.left * 5f, 0.3f, () => Cine(CineCam.LowAngleArc(handler.transform.position + Vector3.up * 1.2f, 1f, 3.6f, -60f, 0f, 36f)));
            handler.SetFacing(-1);
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
            // face to face across the 0.2 m of depth between them: his fist goes to the man's collar
            hero.LookAtXZ(at);
            if (handler != null) handler.LookAtXZ(hero.transform.position);
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
            hero.body.Play(hero.body.Has("idle_alone") ? "idle_alone" : hero.body.Has("idle_loose") ? "idle_loose" : "idle", 0.3f);   // the fight is over: standing, not in a guard
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
            // he keeps walking through the card's fade to black (it takes a second): not walking on the spot
            var w1 = new Vector3(Mathf.Min(w0.x + 10f, 129.6f), 0f, 0f);
            float walked = 0f;
            StartCoroutine(HoldFor(7.7f, () => { walked += StoryDt; hero.CinePose(Vector3.Lerp(w0, w1, walked / 7.7f)); }));
            yield return Move(6.5f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 1.0f, u, 2.4f, 16f, 0.8f, 9f, 42f));
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

    /// <summary>A man's torch (the backyard's men, the fortress's guards): a flashlight gripped in his right
    /// fist, following the hand bone every frame, its beam going out from the lens. While he stands, walks or
    /// runs he holds it out before him (his arm reaches forward: a two-bone reach) and his wrist
    /// points it where he looks, a little down at the ground ahead. When he swings, is hit or goes down, the
    /// arm is the clip's again and the torch simply goes with his fist; when he is dead the beam goes out.
    /// Runs after the clip has posed the bones (LateUpdate), and does its own reach (PuppetRig's two-bone
    /// solve) so that nothing depends on the order it runs in against PuppetRig: a class in a file of
    /// another name may not get its DefaultExecutionOrder.</summary>
    [DefaultExecutionOrder(210)]
    public class TorchHold : MonoBehaviour
    {
        /// <summary>Degrees the beam dips below his line of sight (the ground a few metres ahead).</summary>
        public float dip = 14f;
        /// <summary>A point he looks at (the beam and the arm follow it); null: straight ahead, where he faces.</summary>
        public System.Func<Vector3> lookAt;

        Enemy man;
        Transform body, upper, fore, hand, fingers, chest, glow;
        GameObject[] beams;
        float grip, deadFor, weight, want;
        Camera cam;

        /// <summary>The clips he holds it out in: standing, walking, running. Anything else (a swing, a
        /// wind-up, a hit, a fall, a grab, a taunt) plays its own arm.</summary>
        static readonly HashSet<string> HeldOut = new HashSet<string>
        {
            "idle", "idle_loose", "idle_alone", "sentry", "walk", "walk_alone", "run", "sprint", "sneak",
            "guard_stalk", "guard_up", "guard_hold", "advance", "retreat", "shuffle_fwd", "shuffle_back",
            "step_in", "step_out", "sidestep", "hold_cord",
        };

        public static TorchHold Give(Enemy e, Material beamMat, Material glowMat)
        {
            var b = e != null ? e.body : null;
            var hand = b != null ? b.Bone("hand.R") : null;
            if (hand == null) return null;
            var had = hand.GetComponentInChildren<TorchHold>();
            if (had != null) return had;
            var root = new GameObject("Torch").transform;
            root.SetParent(hand, false);
            // sized with the man (a runner's a little smaller), whatever scale the bone carries
            root.localScale = Vector3.one * (e.transform.localScale.x / Mathf.Max(0.01f, hand.lossyScale.x));
            var t = root.gameObject.AddComponent<TorchHold>();
            t.man = e;
            t.hand = hand;
            t.fore = b.Bone("forearm.R");
            t.upper = b.Bone("upperarm.R");
            t.fingers = b.Bone("fingers.R");
            t.chest = b.Bone("chest");
            t.body = b.transform;
            float handLen = t.fingers != null ? Vector3.Distance(t.fingers.position, hand.position) : 0.05f * e.transform.localScale.x;
            t.grip = handLen * 0.6f;                                   // the fist's middle, just short of the knuckles
            // the flashlight along +z: a dark barrel through the fist, a wider head, the pale lens
            var dark = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.07f, 0.07f, 0.08f) };
            var grey = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.3f, 0.3f, 0.32f) };
            var lens = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.96f, 0.96f, 0.92f) };
            Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.03f), new Vector3(0.036f, 0.075f, 0.036f), dark, true);
            Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.125f), new Vector3(0.054f, 0.022f, 0.054f), grey, true);
            Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.148f), new Vector3(0.046f, 0.002f, 0.046f), lens, true);
            // the beam: two crossed quads along +z from the lens (bright and narrow there, wide and faint
            // ahead), one standing (seen side on, as the game is), one lying (seen from above)
            const float L = 2.8f, W = 1.1f;
            t.beams = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                var q = Part(root, PrimitiveType.Quad, new Vector3(0f, 0f, 0.15f + L * 0.5f), new Vector3(W, L, 1f), beamMat, false);
                q.name = "TorchBeam";
                q.localRotation = Quaternion.LookRotation(i == 0 ? Vector3.right : Vector3.up, Vector3.forward);
                t.beams[i] = q.gameObject;
            }
            t.glow = Part(root, PrimitiveType.Quad, new Vector3(0f, 0f, 0.16f), Vector3.one * 0.24f, glowMat, false);
            t.glow.name = "TorchGlow";
            t.beams = new[] { t.beams[0], t.beams[1], t.glow.gameObject };
            t.cam = Camera.main;
            t.LateUpdate();                                            // on his fist from the first frame
            return t;
        }

        static Transform Part(Transform parent, PrimitiveType p, Vector3 at, Vector3 size, Material m, bool alongZ)
        {
            var g = GameObject.CreatePrimitive(p);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = at;
            if (alongZ) g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);    // a cylinder's length is its y
            g.transform.localScale = size;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return g.transform;
        }

        bool Holding => man != null && man.Alive && man.body != null && HeldOut.Contains(man.body.CurrentName);

        void Update() { want = Holding ? 1f : 0f; }

        /// <summary>The way he looks, on the ground (his facing, or towards what he looks at).</summary>
        Vector3 Ahead(Vector3 from)
        {
            Vector3 f = body != null ? body.right : man.transform.right;          // the body's own facing (it eases round in a turn)
            if (lookAt != null) { var d = lookAt() - from; d.y = 0f; if (d.sqrMagnitude > 0.01f) f = d; }
            f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.right;
        }

        /// <summary>Out from his chest towards the right shoulder, on the ground.</summary>
        Vector3 Side(Vector3 f)
        {
            var c = chest != null ? chest.position : man.transform.position + Vector3.up * 0.9f;
            var o = upper.position - c;
            o.y = 0f;
            o -= Vector3.Dot(o, f) * f;
            return o.sqrMagnitude > 1e-6f ? o.normalized : Vector3.Cross(Vector3.up, f);
        }

        /// <summary>The arm out for it, by w (0 the clip's arm, 1 held out): the fist in front of the shoulder at
        /// chest height, a little in, the elbow soft and down (PuppetRig's two-bone reach).</summary>
        void Reach(float w)
        {
            Vector3 S = upper.position, E = fore.position, H = hand.position;
            float l1 = (E - S).magnitude, l2 = (H - E).magnitude;
            if (l1 < 1e-4f || l2 < 1e-4f) return;
            var f = Ahead(S);
            var side = Side(f);
            var T = S + f * (0.8f * (l1 + l2)) + Vector3.down * (0.32f * (l1 + l2)) - side * (0.14f * (l1 + l2));
            var toT = T - S;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, (l1 + l2) * 0.999f);
            var dir = toT.sqrMagnitude > 1e-8f ? toT.normalized : Vector3.down;
            var pole = Vector3.down + side * 0.7f;
            var perp = pole - Vector3.Dot(pole, dir) * dir;
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.Cross(dir, Vector3.right);
            perp.Normalize();
            float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            float sinA = Mathf.Sqrt(1f - cosA * cosA);
            var E2 = S + dir * (cosA * l1) + perp * (sinA * l1);
            upper.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(E - S, E2 - S), w) * upper.rotation;
            E = fore.position; H = hand.position;
            fore.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(H - E, S + dir * d - E), w) * fore.rotation;
        }

        /// <summary>Where the beam points: where he looks, a little down (or at the point he looks at).</summary>
        Vector3 Aim()
        {
            if (lookAt != null)
            {
                var d = lookAt() - transform.position;
                if (d.sqrMagnitude > 0.01f) return d.normalized;
            }
            var f = Ahead(transform.position);
            float r = dip * Mathf.Deg2Rad;
            return (f * Mathf.Cos(r) + Vector3.down * Mathf.Sin(r)).normalized;
        }

        Vector3 HandDir()
        {
            var d = fingers != null ? fingers.position - hand.position : fore != null ? hand.position - fore.position : man.transform.right;
            return d.sqrMagnitude > 1e-8f ? d.normalized : man.transform.right;
        }

        void LateUpdate()
        {
            if (hand == null || man == null) return;
            // the arm out (or back to the clip's), then the wrist pointing the torch, as much as it is held out
            weight = Mathf.MoveTowards(weight, want, Time.deltaTime * (want > weight ? 5f : 6f));
            float w = upper != null && fore != null ? Mathf.SmoothStep(0f, 1f, weight) : 0f;
            if (w > 0.001f) Reach(w);
            var hd = HandDir();
            if (w > 0.001f)
            {
                hand.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(hd, Aim()), w) * hand.rotation;
                hd = HandDir();
            }
            transform.position = hand.position + hd * grip;
            transform.rotation = Quaternion.LookRotation(hd, Mathf.Abs(hd.y) > 0.96f ? man.transform.right : Vector3.up);
            // dead: the torch goes out
            deadFor = man.Alive ? 0f : deadFor + Time.deltaTime;
            bool lit = deadFor < 0.3f;
            if (beams != null && beams[0].activeSelf != lit) foreach (var b in beams) b.SetActive(lit);
            if (lit && glow != null)
            {
                if (cam == null) cam = Camera.main;
                if (cam != null) glow.rotation = Quaternion.LookRotation(glow.position - cam.transform.position, Vector3.up);
            }
        }
    }
}
