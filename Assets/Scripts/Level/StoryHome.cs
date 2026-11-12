using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>CHAPTER 3: HOME (docs/story/chapter3_home.md). 14 Alder Lane at dusk: the photo gives
    /// him his daughter, Maya; the kitchen gives him Elena, dead. Rage, the house fight room by room,
    /// Maya's searched room, THE CLEANER, the kitchen chair and the lamp flex, and the small bare
    /// footprints running out to the garden.
    ///
    /// The camera picks its move for each beat (CineCam, docs/story/camera_language.md): an aerial orbit
    /// to arrive, a pan to the car, an overhead on the chalk, a dolly in on the door and the desk, a
    /// lateral reveal past the kitchen doorway, a crane down to him kneeling, a dolly zoom when the voice
    /// upstairs comes, a pedestal up as he stands, a truck up the stairs, an arc in her room, a low arc
    /// for the Cleaner, still frames and a creeping dolly in the kitchen, a roll as the world tips, an
    /// overhead on the footprints, and a drone pulling away from him at the back door.</summary>
    public partial class StoryIntro
    {
        /// <summary>Where to resume after a defeat: a room 0-3, 4 the Cleaner (-1: from the top).</summary>
        public static int HomeCheckpoint = -1;
        /// <summary>TEST: 0 the arrival, 1 the study (photo, memory), 2 the kitchen (Elena), 3 the house fight,
        /// 4 the Cleaner, 5 the interrogation, 6 the footprints.</summary>
        public static int HomeFrom;

        // the rooms' men, in order: the kitchen (in from the garden), the living room, the hall (down
        // the stairs), the landing (out of the rooms)
        static readonly Spawn[][] HomeWaves =
        {
            new[] { S(Br, Rt), S(Br, Rt), S(Ru, Bg) },
            new[] { S(Br, Lt), S(Gr, Rt), S(Br, Bg), S(Ru, Lt) },
            new[] { S(Br, Up), S(Dm, Lt), S(Br, Up) },
            new[] { S(Br, Rt), S(Ru, Bg), S(Br, Rt), S(Gr, Rt) },
        };
        static readonly int[] HomeCaps = { 3, 3, 3, 3 };
        static readonly float[] MayaDur = { 2.8f, 2.6f, 2.6f, 2.6f, 2.6f, 3.4f };
        const float MayaSeconds = 16.6f;

        int hRoom = -1, hQueued;
        bool hReady, hClear, hRoomsDone, hClimbing;
        float hNextSpawn, hDone = -1f, hGoUntil, hRage, homeTitleAt = -1f, photoAt = -1f, mayaAt = -1f, flashAt = -1f, dripAt;
        readonly List<Enemy> homeMen = new List<Enemy>();
        Enemy elenaBody, cleaner;
        int cleanerStage;            // 0 not here, 1-3 his phases, 5 the finale
        Transform swingPivot, bulb, chair, prints;
        Renderer bulbR;
        float bulbFlicker = -1f, swingFrom = -1f;
        Texture2D[] mayaStills, flashStills;
        Texture2D photoTex;
        Font markerFont;

        // ================================================================ the camera

        void Cine(CineCam.Shot s)
        {
            if (cam == null) return;
            cam.ScriptRoll = s.roll;
            cam.ScriptUp = s.up;
            cam.Scripted(s.pos, s.look, 1f, s.fov);
        }

        void CineOff()
        {
            if (cam == null) return;
            cam.ScriptRoll = 0f;
            cam.ScriptUp = Vector3.zero;
            cam.Scripted(Vector3.zero, Vector3.zero, 0f);
        }

        /// <summary>Hold a move for `secs` real seconds (u runs 0..1), with something to do each frame.</summary>
        IEnumerator Move(float secs, System.Func<float, CineCam.Shot> shot, System.Action<float> each = null)
        {
            float t0 = 0f;
            while (t0 < secs)
            {
                t0 += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t0 / secs);
                Cine(shot(u));
                each?.Invoke(u);
                yield return null;
            }
        }

        Vector3 HeroHead() => head != null ? head.position + Vector3.up * 0.1f : hero.transform.position + Vector3.up * 1.2f;

        // ================================================================ setting up

        void BeginHome()
        {
            phase = Phase.Home;
            HeadWound.Chapter = 3;
            Sound.Ambience(0f, 0.6f, 22000f);
            HeroController.RageScale = 1f;
            Enemy.RageFear = false;
            HomeProps();
            int from = HomeFrom;
            HomeFrom = 0;
            if (from == 0 && HomeCheckpoint >= 0) from = HomeCheckpoint >= 4 ? 4 : 3;
            StartCoroutine(Home(from));
        }

        void HomeProps()
        {
            // Elena, where they left her: a pale figure on the kitchen floor, reaching for the back door
            if (director != null)
            {
                elenaBody = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                if (elenaBody != null)
                {
                    elenaBody.Puppet();
                    elenaBody.SetBaseColor(new Color(0.84f, 0.84f, 0.83f));
                    elenaBody.PuppetPose(new Vector3(HomePlan.ElenaX - 0.6f, 0f, 0.7f));
                    elenaBody.SetFacing(1);
                    elenaBody.PuppetPlay("prone", 0f);
                    elenaBody.transform.localScale = Vector3.one * 0.94f;
                }
            }
            var ink = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.05f, 0.05f, 0.055f) };
            var white = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            // the kitchen chair (for later) and the bulb over the table
            var bakedChair = GameObject.Find("KitchenChair");          // the set has one too: the story's is the one that matters
            if (bakedChair != null) bakedChair.SetActive(false);
            chair = new GameObject("KitchenChair").transform;
            chair.position = new Vector3(29.2f, 0f, 0.9f);
            void Part(Transform p, Vector3 at, Vector3 size, Material m)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(p, false);
                c.transform.localPosition = at;
                c.transform.localScale = size;
                c.GetComponent<Renderer>().sharedMaterial = m;
            }
            Part(chair, new Vector3(0f, 0.44f, 0f), new Vector3(0.46f, 0.04f, 0.44f), ink);
            Part(chair, new Vector3(0.22f, 0.8f, 0f), new Vector3(0.04f, 0.72f, 0.44f), ink);
            foreach (var dx in new[] { -0.2f, 0.2f })
                foreach (var dz in new[] { -0.19f, 0.19f })
                    Part(chair, new Vector3(dx, 0.22f, dz), new Vector3(0.04f, 0.44f, 0.04f), ink);
            chair.gameObject.SetActive(false);
            bulb = new GameObject("KitchenBulb").transform;
            bulb.position = new Vector3(29.0f, 2.45f, 0.9f);
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(bulb, false);
            b.transform.localScale = Vector3.one * 0.16f;
            bulbR = b.GetComponent<Renderer>();
            bulbR.sharedMaterial = white;
            Part(bulb, new Vector3(0f, 0.28f, 0f), new Vector3(0.012f, 0.5f, 0.012f), ink);
            // the footprints in the blood: boots, and between them small bare feet, out to the back door
            prints = new GameObject("Footprints").transform;
            var blood = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.09f, 0.02f, 0.02f) };
            for (int i = 0; i < 9; i++)
            {
                float x = 30.0f + i * 0.45f;
                Part(prints, new Vector3(x, 0.006f, 0.25f + (i % 2) * 0.22f), new Vector3(0.28f, 0.004f, 0.11f), blood);           // boots
                if (i % 2 == 0) Part(prints, new Vector3(x + 0.2f, 0.007f, 0.95f + (i % 4 == 0 ? 0.08f : -0.06f)), new Vector3(0.13f, 0.004f, 0.06f), blood);   // her feet
            }
            Part(prints, new Vector3(32.4f, 0.005f, 0.6f), new Vector3(0.9f, 0.003f, 0.6f), blood);                                // where Elena lies
            prints.gameObject.SetActive(false);
            // the swing seat, on a pivot at the frame's top bar
            var sw = Find("Swing");
            if (sw != null)
            {
                swingPivot = new GameObject("SwingPivot").transform;
                swingPivot.position = new Vector3(HomePlan.SwingX, 2.4f, HomePlan.SwingZ);
                sw.SetParent(swingPivot, true);
            }
        }

        // ================================================================ the chapter

        IEnumerator Home(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            if (from <= 0)
            {
                // ------------------------------------------------ H1 black: the title, running footsteps
                blackout = 1f;
                homeTitleAt = Time.unscaledTime;
                for (int i = 0; i < 9; i++) Sound.PlayLater(Sfx.Footstep, 0.3f + i * 0.28f, 0.7f);
                Sound.PlayLater(Sfx.Breath, 0.6f, 0.8f);
                yield return RealWait(3.4f);

                // ------------------------------------------------ H2 Alder Lane at dusk: an aerial orbit as he arrives, running
                CutawayWall.ShowAll = true;
                hero.CinePose(new Vector3(HomePlan.StreetX0 + 1f, 0f, 0f));
                hero.body.Play("run", 0.1f, true, 1.2f);
                Sound.Play(Sfx.Wind, 0.4f);
                StartCoroutine(FadeIn(1.2f));
                float runFrom = hero.transform.position.x;
                yield return Move(5.2f, u => CineCam.AerialOrbit(new Vector3(-7f, 1.2f, 1.5f), u, 16f, 7.5f, -62f, -28f, 42f),
                    u =>
                    {
                        hero.CinePose(new Vector3(Mathf.Lerp(runFrom, -10.5f, Mathf.Min(1f, u * 1.15f)), 0f, 0f));
                        if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.3f; Sound.Play(Sfx.Footstep, 0.6f, hero.transform.position); }
                    });
                hero.body.Play(hero.body.Has("idle_loose") ? "idle_loose" : "idle", 0.2f);
                Sound.Play(Sfx.Breath, 0.8f, hero.Chest);

                // ------------------------------------------------ H2b the black car: he turns to it (a pan off him onto it)
                var carAt = new Vector3(-18f, 0.8f, -5.2f);
                yield return Move(2.4f, u => CineCam.Pan(new Vector3(-8.2f, 1.55f, -2.2f), HeroHead(), carAt, u, 40f));
                Say("YOU", "...Another black car.", 2.0f);
                yield return Hold(2.0f, () => Cine(CineCam.Pan(new Vector3(-8.2f, 1.55f, -2.2f), HeroHead(), carAt, 1f, 40f)));

                // ------------------------------------------------ H3 play: up to the gate; the chalk on the path, from straight above
                CineOff();
                yield return PlayerWalk(HomePlan.GateX + 0.3f, HomePlan.StreetX0 + 0.5f, 2.2f,
                    Application.isMobilePlatform ? "HOLD THE RIGHT SIDE TO WALK" : "HOLD  D  OR  RIGHT ARROW  TO WALK");
                hero.CinePuppet();
                if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
                yield return Move(4.2f, u => CineCam.OverheadRotation(new Vector3(-2.2f, 0f, 0.15f), u, 3.4f, -8f, 26f, 48f),
                    u => { if (u > 0.3f && u - Time.unscaledDeltaTime / 4.2f <= 0.3f) Say("YOU", "...Three.", 2.2f); });

                // ------------------------------------------------ H4 the door, ajar: a slow dolly in on it
                hero.CinePose(new Vector3(-1.4f, 0f, 0f));
                Sound.Play(Sfx.Door, 0.6f, new Vector3(0f, 1f, 0f), 0.65f);
                yield return Move(3.0f, u => CineCam.DollyIn(new Vector3(0f, 1.1f, 0f), new Vector3(1f, -0.05f, 0.2f), u, 4.4f, 2.0f, 40f));
                CutawayWall.ShowAll = false;
                CineOff();
                yield return PlayerWalk(5.6f, -1.6f, 1.6f, Application.isMobilePlatform ? "HOLD TO WALK IN" : "HOLD  D  TO WALK IN");
                hero.CinePuppet();
                // the hall: a small yellow raincoat on the rack (a pan from him to it)
                var coat = new Vector3(5.1f, 1.35f, 3.6f);
                Sound.Play(Sfx.Heartbeat, 0.3f);
                yield return Move(2.6f, u => CineCam.Pan(new Vector3(7.4f, 1.45f, -1.6f), HeroHead(), coat, u, 40f));
                yield return Hold(0.8f, () => Cine(CineCam.Pan(new Vector3(7.4f, 1.45f, -1.6f), HeroHead(), coat, 1f, 40f)));
                // the hall table: a detective's card, and a note in her hand
                yield return HallCard();
                CineOff();
            }
            if (from <= 1)
            {
                // ------------------------------------------------ H5 the study: the desk (a dolly in), TAP: the photo
                if (from == 1) { hero.CinePose(new Vector3(9.6f, 0f, 0f)); yield return FadeIn(0.5f); }
                yield return PlayerWalk(11.2f, 3.4f, 1.6f, Application.isMobilePlatform ? "HOLD TO WALK" : "HOLD  D  TO WALK");
                hero.CinePuppet();
                var desk = new Vector3(HomePlan.StudyDeskX, 0.95f, HomePlan.DeskZ);
                yield return Move(2.4f, u => CineCam.DollyIn(desk, new Vector3(0.2f, -0.35f, 1f), u, 3.6f, 1.9f, 38f));
                yield return WaitTap("THE DESK", Application.isMobilePlatform ? "TAP" : "TAP  /  CLICK", () => Cine(CineCam.DollyIn(desk, new Vector3(0.2f, -0.35f, 1f), 1f, 3.6f, 1.9f, 38f)));
                photoAt = Time.unscaledTime;
                Sound.Play(Sfx.MusicNote, 0.8f);
                yield return RealWait(4.2f);
                Say("YOU", "Elena... and Maya.", 2.6f);
                yield return RealWait(2.8f);
                yield return PhotoOfV();
                // ------------------------------------------------ H6 the memory: Maya
                mayaAt = Time.unscaledTime;
                Sound.Play(Sfx.MemoryMaya, 0.9f);
                float mt = 0f;
                bool l1 = false, l2 = false;
                while (mt < MayaSeconds)
                {
                    mt += Time.unscaledDeltaTime;
                    if (!l1 && mt > 0.6f) { l1 = true; Say("HER", "She has your frown.", 2.0f); }
                    if (!l2 && mt > MayaSeconds - 2.8f) { l2 = true; Say("MAYA", "Papa! Look!", 2.2f); }
                    yield return null;
                }
                mayaAt = -1f;
                yield return RealWait(0.6f);
            }
            if (from <= 2)
            {
                // ------------------------------------------------ H7 play: towards the kitchen, a tap dripping
                if (from == 2) { hero.CinePose(new Vector3(23f, 0f, 0f)); yield return FadeIn(0.5f); }
                dripAt = Time.unscaledTime;
                yield return PlayerWalk(25.6f, hero.transform.position.x - 0.5f, 1.4f, Application.isMobilePlatform ? "HOLD TO WALK" : "HOLD  D  TO WALK");
                hero.CinePuppet();
                // ------------------------------------------------ H8 the kitchen, past the doorway: there she is
                Sound.Play(Sfx.Tinnitus, 0.7f);
                Say("YOU", "...Bleach. Plastic on the floor. Someone's been cleaning.", 3.2f);
                var her = new Vector3(HomePlan.ElenaX - 0.4f, 0.25f, 0.7f);
                yield return Move(4.6f, u => CineCam.LateralReveal(new Vector3(24.7f, 1.2f, 1.0f), new Vector3(26.6f, 1.25f, -0.9f), her, u, 40f));
                // ------------------------------------------------ H9 he goes to her and kneels: the crane comes down to him
                hero.body.Play("walk", 0.2f, true, 0.7f);
                var h0 = hero.transform.position;
                yield return Move(2.6f, u => CineCam.LateralReveal(new Vector3(26.6f, 1.25f, -0.9f), new Vector3(27.6f, 1.25f, -1.4f), her, u, 40f),
                    u => hero.CinePose(Vector3.Lerp(h0, new Vector3(HomePlan.ElenaX - 0.55f, 0f, -0.05f), u)));       // at her side, by her hand
                if (hero.body.Has("kneel_hand")) hero.body.Play("kneel_hand", 0.35f);
                Sound.Play(Sfx.Cloth, 0.6f, hero.Chest);
                var kneelAt = hero.transform.position + new Vector3(0.4f, 0.6f, 0.1f);
                yield return Move(4.0f, u => CineCam.CraneDown(kneelAt, u, 3.6f, 0.9f, 2.8f, 38f));
                Say("YOU", "No...  no, no, no.", 3.0f);
                Sound.Play(Sfx.Heartbeat, 0.8f);
                yield return Move(3.4f, u => CineCam.DollyIn(HeroHead(), new Vector3(0.6f, -0.1f, 1f), u, 2.6f, 1.2f, 34f));
                // he closes her eyes: the music goes, the house goes quiet
                Sound.MusicStop(1.5f);
                Sound.Tension(false);
                {
                    var herFace = new Vector3(HomePlan.ElenaX - 0.2f, 0.28f, 0.7f);
                    StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, () => Cine(new CineCam.Shot(herFace + new Vector3(-0.4f, 0.9f, -0.9f), herFace, 30f))));
                    yield return HoldTo("CLOSE HER EYES", 2.8f, null, herFace);
                }
                yield return RealWait(0.6f);
                Say("YOU", "...I'm home.", 2.6f);
                yield return Hold(3.2f, () => Cine(CineCam.CraneDown(kneelAt, 1f, 3.6f, 0.9f, 2.8f, 38f)));
                // ------------------------------------------------ H10 how it happened: read off the room, not remembered
                // (he can't remember a night he wasn't there for; Maya tells it in chapter 4). From her, his eyes go
                // to the back door, a few steps away, still open on the dark garden.
                var backDoor = new Vector3(HomePlan.BackDoorX, 1.1f, 0f);
                Sound.Play(Sfx.Wind, 0.5f, backDoor);
                Sound.Play(Sfx.Creak, 0.5f, backDoor);
                yield return Hold(2.4f, () => Cine(new CineCam.Shot(new Vector3(HomePlan.ElenaX - 0.8f, 0.6f, -1.6f), backDoor, 34f)));
                Say("YOU", "The back door's open.", 2.0f);
                yield return Hold(2.2f, () => Cine(new CineCam.Shot(new Vector3(HomePlan.ElenaX - 0.8f, 0.6f, -1.6f), backDoor, 30f)));
                Say("YOU", "She got her out of the door first.", 2.6f);
                yield return Hold(2.8f, () => Cine(new CineCam.Shot(new Vector3(HomePlan.ElenaX - 0.3f, 0.5f, -1.2f), backDoor + Vector3.down * 0.4f, 28f)));
                // ------------------------------------------------ H11 back, on him: the voice upstairs, the floor drops away (a dolly zoom)
                Sound.Play(Sfx.Creak, 0.8f, new Vector3(20f, 4f, 2f));
                Say("VOICE, UPSTAIRS", "Bag the rest. Silas wants this place spotless before the police come back.", 3.6f);
                yield return Move(3.6f, u => CineCam.DollyZoom(HeroHead() + Vector3.down * 0.15f, u, 1.5f, 4.2f, 1.2f));
                Say("YOU", "...Spotless.", 2.0f);
                yield return Hold(2.2f, () => Cine(CineCam.DollyZoom(HeroHead() + Vector3.down * 0.15f, 1f, 1.5f, 4.2f, 1.2f)));
                // he lets her hand go, and stands: the camera rises with him
                hero.body.Play("idle", 0.4f);
                Sound.Play(Sfx.Riser, 0.7f);
                var standAt = hero.transform.position + Vector3.up * 0.9f;
                yield return Move(3.2f, u => CineCam.PedestalUp(standAt, u, 2.6f, 0.35f, 1.9f, 36f), u => hRage = u);
                Say("YOU", "All of you.", 2.0f);
                yield return Hold(1.6f, () => Cine(CineCam.PedestalUp(standAt, 1f, 2.6f, 0.35f, 1.9f, 36f)));
                dripAt = -1f;
            }
            // ---------------------------------------------------- H12 the house fight (rage)
            if (from <= 3)
            {
                hRage = 1f;
                HeroController.RageScale = 1.25f;
                Enemy.RageFear = true;
                int startRoom = from == 3 && HomeCheckpoint >= 0 && HomeCheckpoint < 4 ? HomeCheckpoint : 0;
                if (from == 3) yield return FadeIn(0.4f);
                StartHomeRoom(startRoom, from == 3);
                CineOff();
                barsPx = 0f;
                while (!hRoomsDone) yield return null;

                // ------------------------------------------------ H13 her room: the toys on the floor, her drawing; an arc round him
                yield return PlayerWalk(18.2f, HomePlan.Rooms[3].x0, 1.5f, Application.isMobilePlatform ? "HOLD TO WALK" : "HOLD  D  TO WALK");
                hero.CinePuppet();
                if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
                Sound.Play(Sfx.MusicNote, 0.7f);
                var inRoom = hero.transform.position + Vector3.up * 1.0f;
                yield return Move(3.0f, u => CineCam.ArcShot(inRoom, u, 2.8f, -46f, 0f, 0.3f, 36f));
                yield return ShelfWalkie();
                yield return Move(3.0f, u => CineCam.ArcShot(inRoom, u, 2.8f, 0f, 22f, 0.3f, 36f),
                    u => { if (u > 0.4f && u - Time.unscaledDeltaTime / 3f <= 0.4f) Say("YOU", "...Where are you, sweetheart?", 2.6f); });
                CineOff();
                yield return PlayerWalk(24.2f, 17.5f, 1.6f, Application.isMobilePlatform ? "HOLD TO WALK" : "HOLD  D  TO WALK");
            }
            // ---------------------------------------------------- H14 the bedroom: THE CLEANER (a low arc round him)
            if (from <= 4)
            {
                if (from == 4)
                {
                    hRage = 1f;
                    HeroController.RageScale = 1.25f;
                    Enemy.RageFear = true;
                    hero.CinePose(new Vector3(24.2f, HomePlan.Floor2, 0f));
                    yield return FadeIn(0.5f);
                }
                yield return CleanerArrives();
                while (cleanerStage < 5) yield return null;
            }
            // ---------------------------------------------------- H16-H19 the kitchen chair
            if (from == 5)
            {
                yield return FadeIn(0.3f);
                SpawnCleaner(new Vector3(29.2f, 0f, 0.9f));
            }
            if (from <= 5) yield return Interrogation();
            else
            {
                // TEST: straight to the footprints
                hero.CinePuppet();
                hero.CinePose(new Vector3(28.6f, 0f, 0.4f));
                hero.SetFacing(1);
                hero.body.Play("idle", 0f);
                barsPx = 92f;
                yield return FadeIn(0.5f);
            }
            // ---------------------------------------------------- H20-H22 the footprints, the garden, the card
            yield return Footprints();
        }

        // ================================================================ the rooms

        void StartHomeRoom(int r, bool cut)
        {
            HomeCheckpoint = r;
            hRoom = r;
            hQueued = 0;
            hClear = false;
            hReady = true;
            hDone = -1f;
            hNextSpawn = Time.time + (cut ? 1.2f : 0.5f);
            homeMen.Clear();
            phase = Phase.Home;
            var R = HomePlan.Rooms[r];
            SetArena(R.x0 + 0.3f, R.x1 - 0.3f);
            SetHomeDoors(r);
            if (director != null) director.ShowBanner($"{r + 1}  ·  {R.name}", 2.0f);
            if (cut || hero.CinematicLock)
            {
                if (cut) hero.CinePose(new Vector3(R.x1 - 1.4f, R.y, 0f));
                hero.SetFacing(-1);
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                barsPx = 0f;
                CineOff();
            }
            Sound.Tension(true);
        }

        void SetHomeDoors(int r)
        {
            if (director == null) return;
            var R = HomePlan.Rooms[r];
            float mid = (R.x0 + R.x1) * 0.5f;
            var pts = director.spawnPoints;
            void At(SpawnKind k, Vector3 p) { var t = pts[(int)k]; if (t != null) t.position = p; }
            At(SpawnKind.RightDoor, new Vector3(R.x1 - 0.5f, R.y, 0f));
            At(SpawnKind.LeftDoor, new Vector3(R.x0 + 0.5f, R.y, 0f));
            At(SpawnKind.Background, new Vector3(mid + 1.2f, R.y, 2.4f));
            At(SpawnKind.UpperDoor, r == 2 ? new Vector3(HomePlan.StairX1 - 0.4f, 0f, 2.9f) : new Vector3(mid, R.y, 2.4f));
            At(SpawnKind.Ceiling, new Vector3(mid, R.y, 2.4f));
        }

        void UpdateHome()
        {
            UpdateSwing();
            if (bulbFlicker >= 0f && bulbR != null) bulbR.enabled = Mathf.Sin(Time.unscaledTime * 53f) * Mathf.Sin(Time.unscaledTime * 17f) > -0.2f;
            if (dripAt > 0f && Time.unscaledTime > dripAt) { dripAt = Time.unscaledTime + 1.3f; Sound.Play(Sfx.Drip, 0.35f, new Vector3(27.4f, 1f, 3.8f)); }
            if (cleanerStage > 0 && cleanerStage < 5) { UpdateCleaner(); return; }
            if (!hReady || hRoom < 0 || hero.IsDead || hClimbing || hRoomsDone) return;
            homeMen.RemoveAll(e => e == null || !e.Alive);
            if (!hClear)
            {
                var list = HomeWaves[hRoom];
                if (hQueued < list.Length && homeMen.Count < HomeCaps[hRoom] && Time.time >= hNextSpawn && director != null)
                {
                    var sp = list[hQueued];
                    var e = director.SpawnScripted(sp.type, sp.kind);
                    if (e != null) homeMen.Add(e);
                    hQueued++;
                    hNextSpawn = Time.time + Random.Range(0.7f, 1.4f);
                }
                if (hQueued >= list.Length && homeMen.Count == 0)
                {
                    if (hDone < 0f) hDone = Time.time;
                    if (Time.time - hDone > 1.0f) HomeRoomCleared();
                }
            }
            else if (hRoom < 2)
            {
                // on, leftwards, into the next room
                if (hero.transform.position.x < HomePlan.Rooms[hRoom + 1].x1 - 1.4f) StartHomeRoom(hRoom + 1, false);
            }
        }

        void HomeRoomCleared()
        {
            hClear = true;
            Sound.Play(Sfx.WaveClear, 0.6f);
            if (hRoom == 2) { StartCoroutine(UpTheStairs()); return; }
            if (hRoom == 3) { hRoomsDone = true; Sound.Tension(false); return; }
            var R = HomePlan.Rooms[hRoom];
            var N = HomePlan.Rooms[hRoom + 1];
            SetArena(N.x0 + 0.3f, R.x1 - 0.3f);
            if (director != null) director.ShowBanner("CLEAR", 1.2f);
            hGoUntil = Time.unscaledTime + 30f;
        }

        /// <summary>Up the stairs: the camera trucks alongside him as he climbs.</summary>
        IEnumerator UpTheStairs()
        {
            hClimbing = true;
            hero.CinePuppet();
            var p0 = hero.transform.position;
            var foot = new Vector3(HomePlan.StairX0 + 0.2f, 0f, HomePlan.StairZ - 0.2f);
            var top = new Vector3(HomePlan.StairX1 + 0.1f, HomePlan.Floor2, HomePlan.StairZ - 0.2f);
            var land = new Vector3(HomePlan.StairX1 + 0.6f, HomePlan.Floor2, 0f);
            hero.SetFacing(p0.x > foot.x ? -1 : 1);
            hero.body.Play("walk", 0.2f, true, 1.1f);
            float d0 = Mathf.Abs(p0.x - foot.x) / 2.6f + 0.3f;
            barsPx = 56f;
            yield return Move(d0, u => CineCam.Truck(new Vector3(p0.x - 0.5f, 1.6f, -5.2f), new Vector3(foot.x + 1.2f, 1.6f, -5.2f), new Vector3(0f, -0.2f, 5.2f), u, 42f),
                u => hero.CinePose(Vector3.Lerp(p0, foot, u)));
            hero.SetFacing(1);
            yield return Move(2.4f, u => CineCam.Truck(new Vector3(foot.x + 1.2f, 1.7f, -5.0f), new Vector3(top.x + 0.4f, HomePlan.Floor2 + 1.6f, -5.0f), new Vector3(0f, -0.3f, 5.0f), u, 42f),
                u =>
                {
                    hero.CinePose(Vector3.Lerp(foot, top, u));
                    if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.32f; Sound.Play(Sfx.Creak, 0.25f, hero.transform.position); }
                });
            yield return Move(0.6f, u => CineCam.Truck(new Vector3(top.x + 0.4f, HomePlan.Floor2 + 1.6f, -5.0f), new Vector3(land.x, HomePlan.Floor2 + 1.6f, -5.6f), new Vector3(0f, -0.3f, 5.6f), u, 42f),
                u => hero.CinePose(Vector3.Lerp(top, land, u)));
            hClimbing = false;
            StartHomeRoom(3, false);
            hero.SetFacing(1);
        }

        void UpdateSwing()
        {
            if (swingPivot == null) return;
            float t0 = swingFrom < 0f ? 0f : Time.unscaledTime - swingFrom;
            float amp = swingFrom < 0f ? 0f : 26f * Mathf.Exp(-t0 * 0.08f);
            swingPivot.rotation = Quaternion.Euler(amp * Mathf.Sin(t0 * 2.1f), 0f, 0f);
        }

        // ================================================================ THE CLEANER

        void SpawnCleaner(Vector3 at)
        {
            WaveDirector.BossWeapon = FightStyle.Fists;
            WaveDirector.StoryBoss = true;
            cleaner = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            if (cleaner == null) return;
            cleaner.MakeCleaner(cleaner.maxHealth * 0.75f);
            Dress(cleaner.body, Look.Cleaner);
            cleaner.Puppet();
            cleaner.PuppetPose(at);
            cleaner.SetFacing(-1);
            Enemy.StoryFinale = (e, hit) => { if (cleanerStage < 5) { cleanerStage = 5; } };
        }

        IEnumerator CleanerArrives()
        {
            HomeCheckpoint = 4;
            hero.CinePuppet();
            barsPx = 92f;
            SetArena(HomePlan.BedroomX0 + 0.3f, HomePlan.BedroomX1 - 0.3f);
            SpawnCleaner(new Vector3(31.4f, HomePlan.Floor2, 1.6f));
            if (cleaner == null) { cleanerStage = 5; yield break; }
            cleaner.PuppetPlay("idle", 0f);
            Sound.Play(Sfx.Creak, 0.9f, cleaner.transform.position);
            // he steps out of the dark by the wardrobe, wiping the knife: a low arc round him
            var c0 = cleaner.transform.position;
            cleaner.PuppetPlay("walk", 0.2f, 0.7f);
            yield return Move(3.4f, u => CineCam.LowAngleArc(cleaner.transform.position + Vector3.up * 1.0f, u, 3.2f, -58f, 4f, 36f),
                u => cleaner.PuppetPose(Vector3.Lerp(c0, new Vector3(29.6f, HomePlan.Floor2, 0f), u)));
            cleaner.PuppetPlay("idle", 0.2f);
            Say("THE CLEANER", "You're early. I wasn't finished.", 2.4f, HeadOf(cleaner));
            yield return Hold(2.4f, () => Cine(CineCam.LowAngleArc(cleaner.transform.position + Vector3.up * 1.0f, 1f, 3.2f, -58f, 4f, 36f)));
            Say("THE CLEANER", "They always come home.", 2.4f, HeadOf(cleaner));
            yield return Hold(2.8f, () => Cine(CineCam.LowAngleArc(cleaner.transform.position + Vector3.up * 1.0f, 1f, 3.2f, -58f, 4f, 36f)));
            if (director != null) director.ShowBanner("THE CLEANER", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.7f);
            yield return Hold(1.2f, () => Cine(CineCam.DollyIn(HeroHead(), new Vector3(1f, -0.1f, 0.8f), 1f, 2.6f, 1.6f, 34f)));
            CineOff();
            barsPx = 0f;
            cleaner.PuppetRelease();
            cleaner.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            cleanerStage = 1;
            ShowStreetBanner("I  ·  THE KNIFE");
            Sound.Tension(true);
        }

        void UpdateCleaner()
        {
            if (cleaner == null || !cleaner.Alive) return;
            float hp = cleaner.HealthFraction;
            if (cleanerStage == 1 && hp <= 0.6f)
            {
                cleanerStage = 2;
                cleaner.Disarm(cleaner.Facing);
                cleaner.CleanerPhase = 2;
                ShowStreetBanner("II  ·  THE CHOKE");
                Say("THE CLEANER", "You taught me this. Remember?", 2.2f, HeadOf(cleaner));
            }
            else if (cleanerStage == 2 && hp <= 0.3f)
            {
                cleanerStage = 3;
                cleaner.CleanerPhase = 3;
                cleaner.QueueEnrage();
                ShowStreetBanner("III  ·  ANYTHING");
                Say("THE CLEANER", "You taught all of us! Then you just LEFT!", 2.6f, HeadOf(cleaner));
            }
        }

        // ================================================================ the kitchen chair

        IEnumerator Interrogation()
        {
            Sound.Tension(false);
            hRage = 0.5f;
            foreach (var e in Enemy.All.ToArray()) if (e != null && e != cleaner && e != elenaBody && e.Alive) e.PuppetDie();
            // later: black, then the kitchen
            float t0 = 0f;
            while (t0 < 0.8f) { t0 += Time.unscaledDeltaTime; blackout = t0 / 0.8f; yield return null; }
            blackout = 1f;
            hero.CinePuppet();
            barsPx = 92f;
            var seat = new Vector3(29.2f, 0f, 0.9f);
            if (chair != null) chair.gameObject.SetActive(true);
            if (cleaner != null)
            {
                cleaner.Puppet();
                cleaner.Hide(false);
                cleaner.PuppetPose(seat + new Vector3(0.05f, 0f, 0f));
                cleaner.SetFacing(-1);
                cleaner.PuppetPlay("sit_bound", 0f);
            }
            hero.CinePose(new Vector3(27.7f, 0f, 0.55f));
            hero.SetFacing(1);
            hero.body.Play(hero.body.Has("hold_cord") ? "hold_cord" : "idle", 0f);
            Sound.Play(Sfx.FridgeHum, 0.6f);
            var ch = cleaner != null ? HeadOf(cleaner) : null;
            System.Func<Vector3> cHead = () => ch != null ? ch.position : seat + Vector3.up * 1.0f;
            yield return FadeIn(1.0f);
            // H16 wide, still: the two of them, the bulb over them (the frame barely creeping in)
            var two = new Vector3(28.45f, 1.0f, 0.75f);
            yield return Move(3.2f, u => CineCam.DollyIn(two, new Vector3(0f, -0.12f, 1f), u * 0.25f, 4.4f, 3.4f, 36f));
            yield return ChairTalk(ch, cHead, two);
        }

        /// <summary>The current: the bulb flickers, a buzz, his silhouette jerks in the chair.</summary>
        IEnumerator Shock(float secs, System.Action frame, bool roll = false)
        {
            Sound.Play(Sfx.Buzz, 1f, bulb != null ? bulb.position : hero.Chest);
            if (cleaner != null) cleaner.PuppetPlay("sit_shock", 0.03f);
            bulbFlicker = 0f;
            Buzz();
            float t0 = 0f;
            while (t0 < secs)
            {
                t0 += Time.unscaledDeltaTime;
                if (roll) Cine(CineCam.Roll(new Vector3(28.4f, 1.3f, -3.4f), new Vector3(28.45f, 1.0f, 0.75f), t0 / secs, 0f, -9f, 36f));
                else frame?.Invoke();
                yield return null;
            }
            bulbFlicker = -1f;
            if (bulbR != null) bulbR.enabled = true;
            if (cleaner != null && !roll) cleaner.PuppetPlay("sit_bound", 0.1f);
        }

        // ================================================================ the footprints

        IEnumerator Footprints()
        {
            if (prints != null) prints.gameObject.SetActive(true);
            // H20 he turns: on the floor, from straight above, turning
            var floorAt = new Vector3(31.8f, 0f, 0.6f);
            hero.body.Play("walk", 0.2f, true, 0.6f);
            var p0 = hero.transform.position;
            yield return Move(4.6f, u => CineCam.OverheadRotation(floorAt, u, 4.4f, -20f, 24f, 50f),
                u => { hero.CinePose(Vector3.Lerp(p0, new Vector3(30.2f, 0f, 0.2f), Mathf.Min(1f, u * 2f))); if (u > 0.5f && u - Time.unscaledDeltaTime / 4.6f <= 0.5f) hero.body.Play("idle", 0.3f); });
            Say("YOU", "...Small feet. She ran.", 2.6f);
            yield return Hold(2.4f, () => Cine(CineCam.OverheadRotation(floorAt, 1f, 4.4f, -20f, 24f, 50f)));
            // H21 out through the back door: the garden, the swing still moving; the camera pulls away from him
            swingFrom = Time.unscaledTime;
            Sound.Play(Sfx.SwingCreak, 0.7f, new Vector3(HomePlan.SwingX, 1f, HomePlan.SwingZ));
            Sound.Play(Sfx.Door, 0.5f, new Vector3(HomePlan.BackDoorX, 1f, 0f), 0.7f);
            hero.body.Play("walk", 0.2f, true, 0.8f);
            var w0 = hero.transform.position;
            var outside = new Vector3(36.4f, 0f, 0.4f);
            yield return Move(2.6f, u => new CineCam.Shot(new Vector3(31.0f, 1.35f, -1.6f), Vector3.Lerp(HeroHead(), new Vector3(HomePlan.SwingX, 1.2f, HomePlan.SwingZ), u), 40f),
                u => hero.CinePose(Vector3.Lerp(w0, outside, u)));
            hero.body.Play("idle", 0.3f);
            Sound.Play(Sfx.SwingCreak, 0.6f, new Vector3(HomePlan.SwingX, 1f, HomePlan.SwingZ));
            yield return Move(6.0f, u => CineCam.DronePullAway(hero.transform.position + Vector3.up * 1.0f, u, 2.2f, 13f, 0.6f, 7.5f, 42f));
            cardAt = Time.unscaledTime;
            cardLine = "They always come home.";
            cardTitle = "THE DAUGHTER";
            yield return RealWait(6.0f);
            CineOff();
            phase = Phase.End;
        }

        // ================================================================ on screen

        void HomeGUI(float w, float h)
        {
            // the rage: the edges of the frame go red
            if (hRage > 0.01f && phase == Phase.Home)
            {
                float a = hRage * (0.55f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f));
                for (int i = 0; i < 6; i++)
                {
                    float k = (6 - i) / 6f, th = 10f + i * 9f;
                    var c = new Color(0.55f, 0.02f, 0.03f, a * 0.16f * k);
                    Fill(new Rect(0, 0, w, th), c); Fill(new Rect(0, h - th, w, th), c);
                    Fill(new Rect(0, 0, th, h), c); Fill(new Rect(w - th, 0, th, h), c);
                }
            }
            // the choke: the world narrowing
            if (hero != null && hero.IsGrabbed && cleanerStage >= 2 && cleanerStage < 5)
            {
                float k = Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 2f) * 0.2f + 0.6f);
                for (int i = 0; i < 8; i++)
                {
                    float th = w * 0.03f * (i + 1) * k;
                    var c = new Color(0f, 0f, 0f, 0.13f);
                    Fill(new Rect(0, 0, th, h), c); Fill(new Rect(w - th, 0, th, h), c);
                    Fill(new Rect(0, 0, w, th * 0.6f), c); Fill(new Rect(0, h - th * 0.6f, w, th * 0.6f), c);
                }
            }
            if (bulbFlicker >= 0f) Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, bulbR != null && !bulbR.enabled ? 0.55f : 0f));
            // GO, leftwards (the rooms run right to left downstairs)
            if (phase == Phase.Home && hClear && hRoom < 2 && !hClimbing && Time.unscaledTime < hGoUntil)
            {
                float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
                prompt.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(70, h * 0.42f, 240, 40), "<  <   " + Spaced("GO"), prompt);
            }
            // the title over black
            if (homeTitleAt > 0f)
            {
                float t0 = Time.unscaledTime - homeTitleAt;
                float a = Mathf.Clamp01(t0 / 0.8f) * Mathf.Clamp01((3.6f - t0) / 0.8f);
                if (a > 0f)
                {
                    cap.alignment = TextAnchor.MiddleCenter;
                    cap.fontSize = 22;
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * a);
                    GUI.Label(new Rect(0, h * 0.38f, w, 30), Spaced("CHAPTER 3"), cap);
                    int bs = big.fontSize;
                    big.fontSize = 84;
                    big.normal.textColor = new Color(1f, 1f, 1f, a);
                    GUI.Label(new Rect(0, h * 0.38f + 30, w, 96), Spaced("HOME"), big);
                    big.fontSize = bs;
                    Fill(new Rect(w * 0.5f - 90 * a, h * 0.38f + 128, 180 * a, 3), new Color(0.8f, 0.06f, 0.07f, a));
                    cap.alignment = TextAnchor.MiddleLeft;
                    cap.fontSize = 20;
                }
            }
            if (photoAt > 0f) PhotoGUI(w, h, Time.unscaledTime - photoAt);
            HomeV2GUI(w, h);
            if (mayaAt > 0f) MayaGUI(w, h, Time.unscaledTime - mayaAt);
            if (flashAt > 0f) ElenaFlashGUI(w, h, Time.unscaledTime - flashAt);
            // the Cleaner's health and his phase
            if (cleaner != null && cleanerStage >= 1 && cleanerStage < 5 && cleaner.Alive)
            {
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 96f;           // below the pause and ? buttons (top centre)
                Fill(new Rect(x - 2, y - 2, bw + 4, 14), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(x, y, bw * cleaner.HealthFraction, 10), new Color(0.85f, 0.85f, 0.85f));
                foreach (float mk in new[] { 0.6f, 0.3f }) Fill(new Rect(x + bw * mk - 1, y - 3, 2, 16), new Color(0.8f, 0.06f, 0.07f));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                string ph = cleanerStage >= 3 ? "III  ·  ANYTHING" : cleanerStage == 2 ? "II  ·  THE CHOKE" : "I  ·  THE KNIFE";
                GUI.Label(new Rect(x, y - 30, bw, 26), Spaced("THE CLEANER") + "     " + ph, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            PromptsGUI(w, h);
        }

        /// <summary>H5: the family photo, held up to the light; then its back, in her hand.</summary>
        void PhotoGUI(float w, float h, float t)
        {
            if (photoTex == null) photoTex = Resources.Load<Texture2D>("Story/Memories/maya_3");
            if (markerFont == null) markerFont = Resources.Load<Font>("Fonts/PermanentMarker-Regular");
            float a = Mathf.Clamp01(t / 0.5f);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.55f * a));
            float ph = Mathf.Min(h * 0.56f, 360f), pw = ph * 1.5f;
            var m0 = GUI.matrix;
            GUIUtility.RotateAroundPivot(-3f + 2f * Mathf.Clamp01(t / 4f), (Vector2)GUI.matrix.MultiplyPoint3x4(new Vector2(w * 0.5f, h * 0.46f)));
            var r = new Rect((w - pw) * 0.5f, h * 0.46f - ph * 0.5f, pw, ph);
            Fill(new Rect(r.x - 14, r.y - 14, r.width + 28, r.height + 46), new Color(0.92f, 0.91f, 0.88f, a));    // the white border
            if (photoTex != null)
            {
                var o = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(r, photoTex, ScaleMode.ScaleAndCrop);
                GUI.color = o;
            }
            var style = new GUIStyle(sub) { font = markerFont != null ? markerFont : sub.font, fontSize = Mathf.RoundToInt(ph * 0.075f), alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(0.12f, 0.12f, 0.14f, a * Mathf.Clamp01((t - 1.4f) / 0.8f));
            GUI.Label(new Rect(r.x, r.yMax + 2, r.width, 30), "Maya, 6 today.  Our whole world.  — E.", style);
            GUI.matrix = m0;
        }

        /// <summary>H6: Maya's memories, soft, pushing in, one dissolving into the next.</summary>
        void MayaGUI(float w, float h, float t)
        {
            int count = MayaDur.Length;
            if (mayaStills == null)
            {
                mayaStills = new Texture2D[count];
                for (int i = 0; i < count; i++) mayaStills[i] = Resources.Load<Texture2D>($"Story/Memories/maya_{i + 1}");
            }
            float a = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((MayaSeconds - t) / 0.8f);
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, a));
            float start = 0f;
            for (int i = 0; i < count; i++)
            {
                float per = MayaDur[i], lt = t - start;
                start += per;
                float ka = Mathf.Clamp01((lt + 0.6f) / 0.9f) * Mathf.Clamp01((per + 0.6f - lt) / 0.9f);
                if (ka <= 0f || mayaStills[i] == null) continue;
                float zoom = 1.04f + 0.08f * Mathf.Clamp01(lt / per);
                float iw = w * zoom, ih = iw * 9f / 16f;
                if (ih < h * zoom) { ih = h * zoom; iw = ih * 16f / 9f; }
                var o = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, ka * a);
                GUI.DrawTexture(new Rect((w - iw) * 0.5f, (h - ih) * 0.5f, iw, ih), mayaStills[i]);
                GUI.color = o;
            }
        }

        /// <summary>H10: how it happened. Hard cuts, shaking, the last one burning out to white.</summary>
        void ElenaFlashGUI(float w, float h, float t)
        {
            if (flashStills == null)
            {
                flashStills = new Texture2D[3];
                for (int i = 0; i < 3; i++) flashStills[i] = Resources.Load<Texture2D>($"Story/Flash/elena_{i + 1}");
            }
            float[] at = { 0f, 1.5f, 4.8f, 7.6f };
            Fill(new Rect(0, 0, w, h), Color.black);
            int idx = t < at[1] ? 0 : t < at[2] ? 1 : t < at[3] ? 2 : -1;
            if (idx >= 0 && flashStills[idx] != null)
            {
                bool cut = Mathf.Repeat(t * 7f, 1f) < 0.07f;                // the odd dropped frame
                if (!cut)
                {
                    float jx = (Mathf.PerlinNoise(t * 9f, 0.2f) - 0.5f) * 14f, jy = (Mathf.PerlinNoise(0.6f, t * 9f) - 0.5f) * 10f;
                    float iw = w * 1.06f, ih = iw * 9f / 16f;
                    if (ih < h * 1.06f) { ih = h * 1.06f; iw = ih * 16f / 9f; }
                    GUI.DrawTexture(new Rect((w - iw) * 0.5f + jx, (h - ih) * 0.5f + jy, iw, ih), flashStills[idx]);
                }
            }
            if (t > at[3]) Fill(new Rect(0, 0, w, h), new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - at[3]) / 0.45f)));
        }
    }
}
