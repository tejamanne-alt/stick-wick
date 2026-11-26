using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>CHAPTER 2, part two: THE STREET (docs/story/chapter2_street.md). Straight on from the
    /// bank: he comes down the steps, and the black car's men are waiting.
    ///
    ///   S0  THE BOX         the four from the car fan out. "The box. Hand it over." "It's empty...
    ///                       Come and look." The first swing: the parry, in slow motion.
    ///   S1  OUTSIDE THE BANK    the car's men, then more.
    ///                       Walking on: "They'll keep coming. Alder Lane is twenty minutes north."
    ///   S2  THE MARKET STREET   a second car's men pour in from both ends.
    ///   S3  UNDER THE RAILWAY   a train goes over: the roar, the light strobing through the sleepers,
    ///                       everyone flinches, and he moves first. It comes back every so often.
    ///   S4  THE CROSSROADS      the last of them, then THE DRIVER: the black car screeches in.
    ///         I   THE BOXER       a real guard, real combinations (parry them).
    ///         II  THE TIRE IRON   from the boot: three swings, the third leaves him open.
    ///         III THE CAR         he gets back in and drives at him: SWIPE to dive clear. It ends in
    ///                             the railway's pillar; he crawls out, bare-handed and furious.
    ///   FINALE  on the ground, a fist in his collar: "Who sent you?" "You'll find out at home.
    ///           We got there first." He runs. CHAPTER 2 COMPLETE · THE RING.
    ///
    /// No rain: it stopped with the warehouse. A defeat restarts at the zone (or the Driver).</summary>
    public partial class StoryIntro
    {
        /// <summary>Where to resume after a defeat: a zone 0-3, 4 the Driver (-1: from the top).</summary>
        public static int StreetCheckpoint = -1;
        /// <summary>TEST: 0 the start, 1 the market, 2 the railway (the train), 3 the crossroads, 4 the Driver, 5 after the Driver.</summary>
        public static int StreetFrom;

        static readonly Spawn[][][] StreetWaves =
        {
            new[] { new Spawn[0],                                                                  // the car's four (placed by the opening)
                    new[] { S(Br, Rt), S(Ru, Lt), S(Gr, Rt) } },
            new[] { new[] { S(Br, Rt), S(Br, Lt), S(Ru, Bg), S(Dm, Rt) },
                    new[] { S(Gr, Rt), S(Ru, Lt), S(Br, Bg), S(Br, Rt) } },
            new[] { new[] { S(Br, Bg), S(Gr, Rt), S(Ru, Lt), S(Br, Rt) },
                    new[] { S(Pi, Rt), S(Br, Bg), S(Ru, Rt) } },
            new[] { new[] { S(Dm, Rt), S(Br, Bg), S(Ru, Rt), S(Br, Lt) } },
        };
        static readonly int[][] StreetCaps = { new[] { 4, 3 }, new[] { 3, 3 }, new[] { 3, 3 }, new[] { 3 } };

        int sZone, sWave, sQueued;
        bool sClear, sReady, sOpenedNext, parryTaught, walkLineSaid;
        float sNextSpawn, sWaveDone = -1f, sGoUntil, trainAt = -1f, nextTrainAt, blackout;
        readonly List<Enemy> streetMen = new List<Enemy>();
        Transform carPivot, trainT, kerbCar;
        Vector3 trainHome;
        Enemy driver;
        int driverStage;                 // 0 not here, 1 boxer, 2 iron, 3 the car (scripted), 4 bare hands, 5 finale
        bool carPrompt, carDodged, carWrecked;
        float carPromptAt, carSwerveEnd = 113.5f;
        string streetBanner;
        float streetBannerAt = -1f;

        float SZ0(int z) => StreetPlan.ZoneX[z];
        float SZ1(int z) => StreetPlan.ZoneX[z + 1];

        // ================================================================ setting up

        void BeginStreet()
        {
            phase = Phase.Street;
            HeadWound.Chapter = 2;
            Sound.Ambience(0f, 0.55f, 22000f);                        // the morning after: no rain
            blackout = 1f;
            FindStreetProps();
            int from = StreetFrom;
            StreetFrom = 0;
            if (from == 0 && StreetCheckpoint >= 0) from = StreetCheckpoint == 0 ? -1 : StreetCheckpoint;   // a defeat: back to that zone (4: the Driver)
            StartCoroutine(StreetRun(from));
        }

        void FindStreetProps()
        {
            kerbCar = Find("BlackCar");
            trainT = Find("Train");
            if (trainT != null) { trainHome = trainT.position; trainT.position = trainHome + Vector3.right * 200f; }   // out of sight until it comes
            var car = Find("DriverCar");
            if (car != null)
            {
                // a pivot at the car's middle, so it can drive and turn
                var pivot = new GameObject("DriverCarPivot").transform;
                pivot.position = new Vector3(StreetPlan.DriverCarX, 0f, StreetPlan.DriverCarZ);
                car.SetParent(pivot, true);
                carPivot = pivot;
                carPivot.position = new Vector3(400f, 0f, StreetPlan.DriverCarZ);      // not here yet
            }
        }

        IEnumerator StreetRun(int from)
        {
            hero.CinePuppet();
            hero.SetFacing(1);
            barsPx = 92f;
            if (from == 0)
            {
                yield return StreetOpening();
                StartStreetZone(0, false);
            }
            else if (from < 0 || from <= 3)
            {
                // straight into a zone (a test, or a defeat): -1 the bank's front, 1 the market, 2 the railway, 3 the crossroads
                StartStreetZone(from < 0 ? 0 : from, true);
                yield return FadeIn(0.6f);
            }
            else if (from == 4)
            {
                StartStreetZone(3, true);
                sClear = true;
                yield return FadeIn(0.6f);
                yield return DriverArrives(true);
            }
            else
            {
                // TEST: the Driver beaten, straight into the finale
                StartStreetZone(3, true);
                sClear = true;
                yield return FadeIn(0.4f);
                SpawnDriver(new Vector3(112f, 0f, 0f));
                driver.Puppet();
                yield return DriverFinale(driver);
            }
        }

        IEnumerator FadeIn(float s)
        {
            float t0 = 0f;
            while (t0 < s) { t0 += StoryDt; if (SkipNow) t0 = s; blackout = 1f - t0 / s; yield return null; }
            blackout = 0f;
        }

        // ================================================================ S0: the box

        IEnumerator StreetOpening()
        {
            float door = StreetPlan.BankDoorX;
            hero.CinePose(new Vector3(door, 0f, 0.4f));
            // just down the steps: he stands while the picture comes up (no walk cycle on the spot)
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0f);
            // the car's four, out on the pavement already, by the open door: standing, not squared up yet
            var men = new Enemy[4];
            var from = new Vector3[4];
            var stand = new[] { new Vector3(9.6f, 0f, 0f), new Vector3(11.2f, 0f, 0f), new Vector3(12.6f, 0f, 0.9f), new Vector3(14.4f, 0f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                if (director == null) break;
                var e = director.SpawnScripted(i == 3 ? Ru : Br, SpawnKind.RightDoor);
                if (e == null) continue;
                e.Puppet();
                from[i] = new Vector3(StreetPlan.KerbCarX - 1.0f + i * 0.5f, 0f, -1.9f);
                e.PuppetPose(from[i]);
                e.SetFacing(-1);
                e.PuppetPlay("idle_loose", 0f);
                men[i] = e;
            }
            Sound.Play(Sfx.Door, 0.5f, new Vector3(door, 1f, 3f), 0.8f);
            yield return FadeIn(1.0f);
            // he walks out across the pavement into the white morning; the four walk out to meet him. Each
            // walk cycle is played at the pace he really covers the ground (no skating)
            const float walkT = 4.2f, menFrom = 0.6f;
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 1.3f);
            float tt = 0f;
            var exit0 = hero.transform.position;
            var exit1 = new Vector3(door + 6.0f, 0f, 0f);
            while (tt < walkT)
            {
                tt += StoryDt;
                hero.CinePose(Vector3.Lerp(exit0, exit1, WalkPace(tt / walkT)));
                for (int i = 0; i < 4; i++)
                {
                    if (men[i] == null) continue;
                    men[i].PuppetPose(Vector3.Lerp(from[i], stand[i], WalkPace(Mathf.Clamp01((tt - menFrom) / (walkT - menFrom)))));
                    if (tt - StoryDt < menFrom && tt >= menFrom)
                        men[i].PuppetPlay("walk", 0.2f, Mathf.Clamp(Vector3.Distance(from[i], stand[i]) / (walkT - menFrom) * 1.18f / 1.25f, 0.6f, 1.6f));
                }
                EndShot(new Vector3(door + 1.5f, 1.5f, -6.5f), new Vector3(Mathf.Lerp(door + 3f, 9f, tt / walkT), 1.2f, 0f), 40f);
                yield return null;
            }
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle_loose", 0.25f);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
            var lead = men[0];
            yield return Hold(0.6f, () => EndShot(new Vector3(4.5f, 1.4f, -4.2f), new Vector3(8.5f, 1.3f, 0f), 34f));
            Say("THUG", "The book. Hand it over.", 2.4f, lead != null ? HeadOf(lead) : null);
            yield return Hold(2.4f, () => EndShot(new Vector3(6.8f, 1.5f, -2.6f), new Vector3(9.6f, 1.55f, 0f), 30f));
            Say("YOU", "You want it?", 1.6f);
            yield return Hold(2.2f, () => EndShot(new Vector3(6.6f, 1.45f, -2.4f), new Vector3(1.6f, 1.45f, 0f), 30f));
            if (hero.body.Has("idle")) hero.body.Play("idle", 0.25f);              // the guard comes up
            Sound.Play(Sfx.Cloth, 0.8f);
            Say("YOU", "...Come and look.", 2.0f);
            // the one out on the flank steps into the line: the fight is on the lane (z 0), and a man let go off it stays off it
            if (men[2] != null)
            {
                var flank = men[2];
                flank.PuppetPlay("walk", 0.15f, 1.0f);
                StartCoroutine(StrollPath(flank, flank.PuppetPose, new[] { new Vector3(stand[2].x, 0f, 0f) }, 1.3f, StreetGround, true, null, -1,
                                          () => flank.PuppetPlay("idle_loose", 0.2f)));
            }
            yield return Hold(2.0f, () => EndShot(new Vector3(4.6f, 1.3f, -3.8f), new Vector3(9.0f, 1.25f, 0f), 34f));
            Say("THUG", "Have it your way. V wants it off your body.", 2.4f, lead != null ? HeadOf(lead) : null);
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle", 0.2f);  // now their guards come up
            Sound.Tension(true);
            if (director != null) director.ShowBanner("THE STREET  ·  1  ·  " + StreetPlan.ZoneName[0], 2.4f);
            foreach (var m0 in men)
                if (m0 != null) { m0.PuppetRelease(); m0.Passive = false; streetMen.Add(m0); }
            if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
            barsPx = 0f;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
        }

        // ================================================================ the zones

        void StartStreetZone(int z, bool cut)
        {
            StreetCheckpoint = z;
            sZone = z;
            sWave = 0;
            sQueued = 0;
            sClear = false;
            sOpenedNext = false;
            sNextSpawn = Time.time + (cut ? 1.4f : 0.6f);
            sWaveDone = -1f;
            sReady = true;
            phase = Phase.Street;
            SetArena(SZ0(z) + (z == 0 ? -10f : 0.6f), Mathf.Min(SZ1(z) - 0.6f, StreetPlan.LaneX1));   // the last zone ends at the bus
            SetStreetDoors(z);
            if (!cut && z > 0 && director != null) director.ShowBanner($"{z + 1}  ·  {StreetPlan.ZoneName[z]}", 2.2f);
            if (z == 2) { nextTrainAt = Time.time + (cut ? 2.5f : 1.0f); }
            // under the arches: one on watch, his back to the way the hero comes (StoryLookouts)
            if (z == 2) PostLookout(SZ0(z) + 8.5f, 0f, 1, streetMen, "ONE ON WATCH UNDER THE ARCHES");
            else lookout = null;
            // the market: a length of scaffold pipe off a stall's frame, lying in the road (the pipe, early), between
            // the crates (35.2, 39.0) and clear of the bottle by the first one
            if (z == 1) PlaceWeapon(FightStyle.Pipe, 14, new Vector3(SZ0(z) + 7.0f, 0f, 0f));
            if (cut)
            {
                if (z == 0) streetMen.Clear();
                if (z == 0 && director != null)
                {
                    // the car's four, straight in
                    foreach (var t0 in new[] { Br, Br, Br, Ru })
                    {
                        var e = director.SpawnScripted(t0, SpawnKind.RightDoor);
                        if (e != null) streetMen.Add(e);
                    }
                }
                hero.CinePose(new Vector3(SZ0(z) + (z == 0 ? 2f : 1.4f), 0f, 0f));
                hero.SetFacing(1);
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                Sound.Tension(true);
                barsPx = 0f;
                if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
                if (director != null) director.ShowBanner($"{z + 1}  ·  {StreetPlan.ZoneName[z]}", 2.2f);
            }
        }

        void SetStreetDoors(int z)
        {
            if (director == null) return;
            float x0 = SZ0(z), x1 = SZ1(z), mid = (x0 + x1) * 0.5f;
            var pts = director.spawnPoints;
            void At(SpawnKind k, Vector3 p) { var t = pts[(int)k]; if (t != null) t.position = p; }
            // the far end: at the crossroads that's the stalled bus (they come in off its front, not out of it)
            At(SpawnKind.RightDoor, new Vector3(Mathf.Min(x1 - 0.8f, StreetPlan.LaneX1 - 0.4f), 0f, 0f));
            At(SpawnKind.LeftDoor, new Vector3(x0 + 0.8f, 0f, 0f));
            // the back: out of a shop doorway, the side street, the arches (under the railway: between the
            // dumpsters at 67 and 79.5, not inside the second one)
            At(SpawnKind.Background, new Vector3(z == 3 ? 108f : z == 2 ? mid - 1.5f : mid + 2.5f, 0f, z == 3 ? 6f : 3.0f));
            At(SpawnKind.UpperDoor, new Vector3(mid - 3f, 0f, 3.0f));
            At(SpawnKind.Ceiling, new Vector3(mid, 0f, 3.0f));
        }

        void UpdateStreet()
        {
            if (blackout > 0f && driverStage < 5 && sReady && !hero.CinematicLock) blackout = Mathf.MoveTowards(blackout, 0f, StoryDt);
            if (!sReady || hero.IsDead) return;
            UpdateTrain();
            if (driverStage > 0) { UpdateDriver(); return; }
            streetMen.RemoveAll(e => e == null || !e.Alive);
            TeachParry();
            var waves = StreetWaves[sZone];
            if (!sClear)
            {
                var list = waves[sWave];
                int cap = StreetCaps[sZone][sWave];
                if (LookoutHolds) sNextSpawn = Mathf.Max(sNextSpawn, Time.time + 0.6f);      // the rest wait on the lookout
                if (sQueued < list.Length && streetMen.Count < cap + (lookout != null && lookout.Alive ? 1 : 0) && Time.time >= sNextSpawn && director != null)
                {
                    var sp = list[sQueued];
                    var e = director.SpawnScripted(sp.type, sp.kind);
                    if (e != null) streetMen.Add(e);
                    sQueued++;
                    sNextSpawn = Time.time + Random.Range(0.6f, 1.3f);
                }
                if (sQueued >= list.Length && streetMen.Count == 0)
                {
                    if (sWaveDone < 0f) sWaveDone = Time.time;
                    if (Time.time - sWaveDone > 1.2f)
                    {
                        sWaveDone = -1f;
                        if (sWave + 1 < waves.Length)
                        {
                            sWave++;
                            sQueued = 0;
                            sNextSpawn = Time.time + 0.8f;
                            if (director != null) director.ShowBanner(sZone == 1 ? "BOTH ENDS" : "MORE OF THEM", 1.4f);
                            if (sZone <= 1) Sound.Play(Sfx.CarDoor, 0.8f, new Vector3(SZ1(sZone) - 2f, 0.6f, -3f));
                        }
                        else StreetZoneCleared();
                    }
                }
            }
            else if (sZone + 1 < StreetPlan.ZoneX.Length - 1)
            {
                if (hero.transform.position.x > SZ0(sZone + 1) + 1.5f) StartStreetZone(sZone + 1, false);
            }
        }

        void StreetZoneCleared()
        {
            if (sZone >= 3) { StartCoroutine(DriverArrives(false)); return; }
            sClear = true;
            SetArena(SZ0(sZone) + 0.6f, Mathf.Min(SZ1(sZone + 1) - 0.6f, StreetPlan.LaneX1));
            if (director != null) director.ShowBanner("CLEAR", 1.4f);
            Sound.Play(Sfx.WaveClear, 0.7f);
            sGoUntil = Time.unscaledTime + 30f;
            if (sZone == 0 && !walkLineSaid)
            {
                walkLineSaid = true;
                Sound.Play(Sfx.Breath, 0.6f, hero.transform.position, 0.9f);
                Say("YOU", "They'll keep coming. Alder Lane is twenty minutes north.", 3.2f);
            }
            if (sZone == 2) Say("YOU", "Somebody's paying a lot of men to stop me getting home.", 3.0f);
        }

        /// <summary>The first swing he sees in the street: time slows, so the parry can be learned.</summary>
        void TeachParry()
        {
            if (parryTaught || sZone != 0) return;
            foreach (var e in streetMen)
            {
                if (e == null || !e.Parryable) continue;
                if (Mathf.Abs(e.transform.position.x - hero.transform.position.x) > 1.9f) continue;
                parryTaught = true;
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.12f, 1.2f, 0.02f, 0.2f);
                Sound.Play(Sfx.SlowmoIn, 0.6f);
                break;
            }
        }

        // ================================================================ S3: the train

        void UpdateTrain()
        {
            if (trainT == null) return;
            if (trainAt < 0f)
            {
                if (sZone == 2 && driverStage == 0 && Time.time >= nextTrainAt && Mathf.Abs(hero.transform.position.x - 77f) < 16f) StartTrain();
                return;
            }
            float u = (Time.time - trainAt) / 5.5f;
            trainT.position = trainHome + Vector3.right * Mathf.Lerp(70f, -95f, u);
            if (u > 0.2f && u < 0.6f) Enemy.CoverUntil = Time.time + 0.3f;      // its roar covers footsteps: the lookout hears nothing
            if (u > 0.36f && u - Time.deltaTime / 5.5f <= 0.36f)
            {
                // right overhead: everyone flinches; he moves first
                foreach (var e in Enemy.All) if (e != null && e.Alive && !e.Boss) e.Startle(1.1f);
                if (cam != null) cam.Shake(0.12f, 1.4f);
                Buzz();
                if (director != null) director.ShowBanner("THEY FLINCH  ·  MOVE FIRST", 1.4f);
            }
            if (u >= 1f) { trainAt = -1f; trainT.position = trainHome + Vector3.right * 200f; nextTrainAt = Time.time + Random.Range(22f, 28f); }
        }

        void StartTrain()
        {
            trainAt = Time.time;
            Sound.Play(Sfx.TrainPass, 1f, new Vector3(77f, StreetPlan.DeckY, 1f));
        }

        /// <summary>The train is over him: the light strobes through the sleepers.</summary>
        float TrainStrobe()
        {
            if (trainAt < 0f) return 0f;
            float u = (Time.time - trainAt) / 5.5f;
            float near = Mathf.Clamp01(1f - Mathf.Abs(u - 0.4f) / 0.3f);
            bool under = hero.transform.position.x > StreetPlan.RailX0 - 2f && hero.transform.position.x < StreetPlan.RailX1 + 2f;
            return under ? near * (0.5f + 0.5f * Mathf.Sign(Mathf.Sin(Time.unscaledTime * 38f))) * 0.42f : 0f;
        }

        // ================================================================ S4: the Driver

        void SpawnDriver(Vector3 at)
        {
            WaveDirector.BossWeapon = FightStyle.Pipe;             // the tire iron (hidden until phase II)
            WaveDirector.StoryBoss = true;
            driver = director != null ? director.SpawnScripted(EnemyType.Brute, SpawnKind.RightDoor) : null;
            WaveDirector.BossWeapon = FightStyle.Fists;
            if (driver == null) return;
            driver.MakeDriver(driver.maxHealth * 0.85f);
            Dress(driver.body, Look.Driver);
            driver.Puppet();
            driver.PuppetPose(at);
            driver.SetFacing(-1);
            Enemy.StoryFinale = (e, hit) => { if (driverStage < 5) { driverStage = 5; StartCoroutine(DriverFinale(e)); } };
        }

        IEnumerator DriverArrives(bool cut)
        {
            sClear = true;
            StreetCheckpoint = 3;
            driverStage = 0;
            hero.CineLock();
            SetArena(SZ0(3) + 0.6f, StreetPlan.LaneX1);
            if (!cut) yield return RealWait(1.0f);
            // the black car screeches in off the road and stops at the kerb
            var park = new Vector3(StreetPlan.DriverCarX, 0f, StreetPlan.DriverCarZ);
            if (carPivot != null) { carPivot.position = park + Vector3.right * 34f; carPivot.rotation = Quaternion.Euler(0f, 180f, 0f); }
            Sound.Play(Sfx.CarRev, 1f, park + Vector3.right * 20f);
            barsPx = 92f;
            // the cut to the car: he's set back to where the Driver walks up to him (the car shot can't see x < 113),
            // so the Driver never walks into him or stops behind him
            if (hero.transform.position.x > 113f) hero.CinePose(new Vector3(113f, 0f, 0f));
            hero.SetFacing(1);
            float tt = 0f;
            while (tt < 2.0f)
            {
                tt += StoryDt;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(tt / 1.8f), 3f);
                if (carPivot != null) carPivot.position = Vector3.Lerp(park + Vector3.right * 34f, park, k);
                EndShot(new Vector3(112f, 1.3f, -9.5f), new Vector3(Mathf.Lerp(130f, StreetPlan.DriverCarX, k), 1.0f, -2f), 40f);
                yield return null;
            }
            if (cam != null) cam.Shake(0.06f, 0.3f);
            Sound.Play(Sfx.CarDoor, 0.9f, park);
            // out of the car on the pavement side, by the front door (clear of the lights' post at x 121), and up to him
            SpawnDriver(park + new Vector3(-0.3f, 0f, 1.5f));
            if (driver == null) { phase = Phase.End; yield break; }
            driver.PuppetPlay("walk", 0.2f, 1.15f);
            yield return StrollPath(driver, driver.PuppetPose, new[] { new Vector3(StreetPlan.DriverCarX - 4.5f, 0f, 0f) }, 1.45f, StreetGround, true,
                () => EndShot(new Vector3(StreetPlan.DriverCarX - 9f, 1.4f, -5.5f), driver.transform.position + Vector3.up * 1.4f, 36f), -1);
            driver.PuppetPlay("idle_loose", 0.2f);                // he talks first; the guard comes up after
            Sound.Play(Sfx.Cloth, 0.8f, driver.Chest);
            Say("THE DRIVER", "Remember me? I drove you. Every job, eleven years.", 3.2f, HeadOf(driver));
            yield return Hold(3.0f, () => EndShot(driver.transform.position + new Vector3(-2.4f, 1.3f, -2.4f), HeadOf(driver).position, 30f));
            driver.PuppetPlay("idle", 0.25f);
            Say("THE DRIVER", "No? Then let's see what's left of you.", 2.4f, HeadOf(driver));
            if (director != null) director.ShowBanner("THE DRIVER", 2.6f);
            Sound.Play(Sfx.CineMassive, 0.7f);
            yield return Hold(2.0f, () => EndShot(driver.transform.position + new Vector3(-3.4f, 1.1f, -3.2f), driver.transform.position + Vector3.up * 1.4f, 34f));
            barsPx = 0f;
            if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
            driver.PuppetRelease();
            driver.Passive = false;
            hero.CineRelease();
            hero.runSpeed = runSpeed;
            StreetCheckpoint = 4;          // a defeat comes back to the Driver's arrival
            driverStage = 1;
            ShowStreetBanner("I  ·  THE BOXER");
        }

        void UpdateDriver()
        {
            if (driver == null || driverStage >= 5) return;
            if (carWrecked) KeepClearOfWreck();
            float hp = driver.HealthFraction;
            if (driverStage == 1 && hp <= 0.6f && driver.Alive) { driverStage = 2; StartCoroutine(TireIron()); }
            else if (driverStage == 2 && hp <= 0.3f && driver.Alive && driver.DriverPhase == 2) { driverStage = 3; StartCoroutine(TheCar()); }
        }

        /// <summary>II: he backs off round the back of his car to the boot and comes back with a tire iron.</summary>
        IEnumerator TireIron()
        {
            driver.Puppet();
            // the car's nose points -x, so its boot is at +x: off the kerb behind it onto the road (clear of the
            // lamp post at 126 and the bus's front at 125.4), facing the boot
            var kerb = new Vector3(StreetPlan.DriverCarX + 3.0f, 0f, -1.9f);
            var boot = new Vector3(StreetPlan.DriverCarX + 2.95f, 0f, StreetPlan.DriverCarZ);
            driver.PuppetPlay("run", 0.15f, 0.8f);
            yield return StrollPath(driver, driver.PuppetPose, new[] { kerb, boot }, 3.2f, StreetGround, false, null, -1);
            Sound.Play(Sfx.CarDoor, 1f, boot);
            driver.PuppetPlay(driver.body.Has("pick_up") ? "pick_up" : "idle_loose", 0.12f);    // a reach into the boot, not a guard
            yield return new WaitForSeconds(0.45f);
            driver.ShowIron(true);
            Sound.Play(Sfx.PipeHit, 0.6f, boot, 1.3f);
            yield return new WaitForSeconds(0.4f);
            driver.DriverPhase = 2;
            // back up onto the pavement with it and into the lane (released off it, he'd fight off it), to his side of him
            float hx = hero.transform.position.x;
            float back = hx > 123f ? hx - 1.1f : StreetPlan.LaneX1 - 0.5f;
            driver.PuppetPlay("walk", 0.15f, 1.1f);
            yield return StrollPath(driver, driver.PuppetPose, new[] { kerb, new Vector3(back, 0f, 0f) }, 1.4f, StreetGround, false, null,
                                    hero.transform.position.x >= back ? 1 : -1);
            driver.FaceTowards(hero.transform.position.x);
            driver.PuppetPlay("sword_idle", 0.15f);
            Say("THE DRIVER", "Hold still.", 1.6f, HeadOf(driver));
            yield return new WaitForSeconds(0.4f);
            driver.PuppetRelease();
            ShowStreetBanner("II  ·  THE TIRE IRON");
        }

        /// <summary>III: back in the car and straight at him. Swipe to dive clear; it ends in the pillar.</summary>
        IEnumerator TheCar()
        {
            driver.Puppet();
            driver.ShowIron(false);
            Sound.Play(Sfx.PipeHit, 0.8f, driver.Chest, 0.8f);                   // flung down
            // back to his door, the pavement side: round the lights' post (x 121), never through it
            var door = new Vector3(StreetPlan.DriverCarX - 0.3f, 0f, -1.8f);
            driver.PuppetPlay("run", 0.15f, 0.85f);
            yield return StrollPath(driver, driver.PuppetPose, new[] { new Vector3(door.x, 0f, -1.1f), door }, 3.4f, StreetGround, false);
            driver.Hide(true);
            Sound.Play(Sfx.CarDoor, 1f, door);
            Sound.Play(Sfx.CarRev, 1f, door);
            ShowStreetBanner("III  ·  THE CAR");
            // he pulls away from the kerb along the road and swings up onto the pavement (past the lights, which
            // leave no room before the bus) straight at him, then floors it along the pavement into the pillar.
            // The car follows its line (CarRunZ), turned the way it's going and lifted onto the kerb.
            carSwerveEnd = Mathf.Clamp(hero.transform.position.x - 1.5f, 103f, 113.5f);
            float x = StreetPlan.DriverCarX, v = 2.5f;
            float crashAt = StreetPlan.CrashX + 2.5f;              // the nose against the pillar's face
            carPrompt = false;
            carDodged = false;
            bool hit = false;
            var gi = GestureInput.Instance;
            if (gi != null) gi.Flush();
            while (x > crashAt && carPivot != null)
            {
                v = Mathf.Min(17f, v + 14f * Time.deltaTime);
                x = Mathf.Max(crashAt, x - v * Time.deltaTime);
                PoseCar(x, crashAt);
                KeepClearOfWreck();
                float cz = CarRunZ(x, crashAt);
                float front = x - 2.4f, hx = hero.transform.position.x, hz = hero.transform.position.z;
                // a bin in its way goes flying on ahead of it
                foreach (var dr in OilDrum.All)
                    if (dr != null && dr.Available && Mathf.Abs(dr.transform.position.x - front) < 0.5f && Mathf.Abs(dr.transform.position.z - cz) < 1.25f)
                        dr.Kick(-1, null, 19f);
                // coming at him: it will be in his lane by the time it reaches him (past the lights it passes him by on the road)
                bool coming = Mathf.Abs(CarRunZ(hx, crashAt) - hz) < 1.4f;
                if (!carPrompt && !carDodged && coming && front - hx < 7.5f && front > hx)
                {
                    carPrompt = true;
                    carPromptAt = Time.unscaledTime;
                    if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.22f, 1.1f, 0.02f, 0.15f);
                    if (gi != null) gi.Flush();
                }
                if (carPrompt && !carDodged && gi != null)
                {
                    for (var c = gi.Next(); c != Command.None; c = gi.Next())
                        if (c == Command.DodgeLeft || c == Command.DodgeRight || c == Command.DodgeForward || c == Command.JumpKick || c == Command.ThrowWeapon)
                        { carDodged = true; break; }
                    if (carDodged) StartCoroutine(DiveClear());
                }
                // hit only where the car's body really meets him (not from the road, two metres off)
                if (!carDodged && !hit && CarTouches(hero.transform.position))
                {
                    hit = true;
                    carPrompt = false;
                    hero.TakeHit(new HitInfo { damage = 26f, direction = -1, knockback = 9f, knockdown = true, heavy = true });
                    Sound.Play(Sfx.PunchHeavy, 1f, hero.Chest, 0.7f);
                    if (cam != null) cam.Shake(0.3f, 0.3f);
                    StartCoroutine(ThrownAside());
                }
                yield return null;
            }
            carPrompt = false;
            // into the pillar: the nose's corner against its face, kicked out a touch by the blow, the bonnet crumpled.
            // Up on the pavement (its wheels on it, not sunk in it); the wreck fills the lane short of x 97.6
            carWrecked = true;
            if (carPivot != null)
            {
                carPivot.position = new Vector3(StreetPlan.CrashX + 2.27f, -StreetPlan.RoadY, -0.6f);
                carPivot.rotation = Quaternion.Euler(0f, 184f, 0f);
                carPivot.localScale = new Vector3(0.9f, 0.96f, 1f);
            }
            KeepClearOfWreck();
            var crash = new Vector3(StreetPlan.CrashX, 0.8f, 0f);
            Sound.Play(Sfx.Boom, 1f, crash);
            Sound.Play(Sfx.Glass, 1f, crash);
            Sound.Play(Sfx.MetalClang, 1f, crash, 0.7f);
            if (cam != null) cam.Shake(0.45f, 0.6f);
            if (InkFx.Instance != null)
            {
                InkFx.Instance.ImpactFlash(0.06f);
                InkFx.Instance.Splash(crash, new Vector3(1f, 0.6f, 0f), 50, new Color(0.5f, 0.5f, 0.52f));
            }
            Buzz();
            yield return new WaitForSeconds(1.4f);
            // he falls out of the wreck on the pavement side (its flank there is at z 0.4), bleeding, bare-handed, and
            // comes on round its tail into the lane (to this side of the hero if he's standing there)
            Sound.Play(Sfx.CarDoor, 1f, crash);
            driver.PuppetPose(new Vector3(StreetPlan.CrashX + 2.0f, 0f, 0.75f));
            driver.SetFacing(1);
            driver.Hide(false);
            driver.PuppetPlay("dazed", 0.1f);
            Sound.Play(Sfx.BruteHurt, 0.9f, driver.Chest, 0.8f);
            yield return new WaitForSeconds(0.6f);
            float hx2 = hero.transform.position.x;
            float lane = Mathf.Abs(hx2 - (StreetPlan.WreckClearX + 1.0f)) < 1.2f ? hx2 + 1.2f : StreetPlan.WreckClearX + 1.0f;
            driver.PuppetPlay("walk", 0.2f, 1.1f);
            yield return StrollPath(driver, driver.PuppetPose, new[] { new Vector3(StreetPlan.WreckClearX + 0.2f, 0f, 0.75f), new Vector3(lane, 0f, 0.75f), new Vector3(lane, 0f, 0f) },
                                    1.4f, StreetGround, false, null, hero.transform.position.x >= lane ? 1 : -1);
            driver.FaceTowards(hero.transform.position.x);
            Say("THE DRIVER", "You owe me a car.", 1.8f, HeadOf(driver));
            driver.DriverPhase = 3;
            driver.PuppetRelease();
            driver.QueueEnrage();
            driverStage = 4;
            ShowStreetBanner("IV  ·  BARE HANDS");
        }

        /// <summary>The dive clear: into the shop doorway, the car missing him by a hand.</summary>
        IEnumerator DiveClear()
        {
            carPrompt = false;
            hero.CinePuppet();
            var p0 = hero.transform.position;
            var p1 = new Vector3(p0.x - 0.6f, 0f, 2.3f);
            if (hero.body.Has("dodge")) hero.body.Play("dodge", 0.03f, true, 1.2f);
            Sound.Play(Sfx.Whoosh, 1f, hero.transform.position);
            Sound.Play(Sfx.HeroEffort, 1f, hero.transform.position);
            if (WaveDirector.Instance != null) WaveDirector.Instance.OnPerfectDodge("DIVED CLEAR");
            float tt = 0f;
            while (tt < 0.45f) { tt += Time.deltaTime; hero.CinePose(Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, tt / 0.45f))); yield return null; }
            if (hero.body.Has("idle")) hero.body.Play("idle", 0.2f);
            yield return new WaitForSeconds(1.2f);
            // back into the lane on his feet (not a glide in his guard), and never into the wreck
            var p2 = hero.transform.position;
            var lane = new Vector3(carWrecked ? Mathf.Max(p2.x, StreetPlan.WreckClearX + 0.3f) : p2.x, 0f, 0f);
            // from behind the wreck: along the pavement past its tail first, then into the lane
            var way = lane.x > p2.x + 0.05f ? new[] { new Vector3(lane.x + 0.2f, 0f, Mathf.Min(p2.z, 1.1f)), lane } : new[] { lane };
            if (hero.body.Has("run")) hero.body.Play("run", 0.12f, true, 0.75f);
            yield return StrollPath(hero, hero.CinePose, way, 3.0f, StreetGround, false, null, -1);     // turned after the car
            if (hero.body.Has("idle")) hero.body.Play("idle", 0.15f);
            hero.CineRelease();
        }

        /// <summary>Clipped by the car: thrown aside onto the pavement, out of its path (a hit sends him back along
        /// x only, so the car would drive straight through him), then back into the lane once it's past.</summary>
        IEnumerator ThrownAside()
        {
            const float aside = 1.4f;                       // the car's flank is at z 0.85 at most
            float t = 0f;
            while (t < 0.25f) { t += Time.deltaTime; SetHeroZ(Mathf.Lerp(0f, aside, Mathf.SmoothStep(0f, 1f, t / 0.25f))); yield return null; }
            while (carPivot != null && !carWrecked && CarTouches(hero.transform.position + Vector3.back * aside)) yield return null;
            yield return new WaitForSeconds(0.5f);
            // short of the pillar the wreck is in the lane: along the pavement past its tail first
            float x0 = hero.transform.position.x, x1 = carWrecked ? Mathf.Max(x0, StreetPlan.WreckClearX + 0.3f) : x0;
            t = 0f;
            if (x1 > x0 + 0.05f)
                while (t < 0.4f) { t += Time.deltaTime; SetHeroXZ(Mathf.Lerp(x0, x1, Mathf.SmoothStep(0f, 1f, t / 0.4f)), aside); yield return null; }
            t = 0f;
            while (t < 0.45f) { t += Time.deltaTime; SetHeroZ(Mathf.Lerp(aside, 0f, Mathf.SmoothStep(0f, 1f, t / 0.45f))); yield return null; }
            SetHeroZ(0f);
        }

        void SetHeroZ(float z) => SetHeroXZ(hero.transform.position.x, z);

        void SetHeroXZ(float x, float z)
        {
            var p = hero.transform.position;
            p.x = x;
            p.z = z;
            hero.transform.position = p;
        }

        void ShowStreetBanner(string s)
        {
            streetBanner = s;
            streetBannerAt = Time.unscaledTime;
            if (director != null) director.ShowBanner(s, 2.2f);
        }

        // ================================================================ the car's line, walking people round things

        /// <summary>Where the Driver's car's middle is in depth when its middle is at x: along the kerb on the
        /// road (z -3.3), swung up onto the pavement ending at carSwerveEnd (its whole length clear of the
        /// lights' post at 121 before it crosses the kerb: the bus leaves no room on the far side of the post),
        /// along the pavement in the lane (z -0.1), and in its last 5 m drifting over towards the pillar (z -0.6).</summary>
        float CarRunZ(float x, float crashAt)
        {
            const float road = StreetPlan.DriverCarZ, walk = -0.1f, pillar = -0.6f;
            float xs = carSwerveEnd + 5.5f, xf = crashAt + 5f;
            if (x >= xs) return road;
            if (x > carSwerveEnd) return Mathf.Lerp(road, walk, Mathf.SmoothStep(0f, 1f, (xs - x) / 5.5f));
            if (x > xf) return walk;
            return Mathf.Lerp(walk, pillar, Mathf.SmoothStep(0f, 1f, (xf - x) / 5f));
        }

        /// <summary>The car on its line at x: turned the way it's going (its nose points -x at yaw 180), and up
        /// the kerb's 15 cm as its wheels (at ±0.78 m) get onto the pavement.</summary>
        void PoseCar(float x, float crashAt)
        {
            if (carPivot == null) return;
            float z = CarRunZ(x, crashAt);
            float slope = (CarRunZ(x - 0.1f, crashAt) - CarRunZ(x + 0.1f, crashAt)) / 0.2f;     // sideways per metre it goes
            float y = -StreetPlan.RoadY * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3.2f, -1.5f, z));
            carPivot.position = new Vector3(x, y, z);
            carPivot.rotation = Quaternion.Euler(0f, 180f + Mathf.Atan(slope) * Mathf.Rad2Deg, 0f);
        }

        /// <summary>The car's body (5.0 x 1.9 m) meets someone (0.5 m wide) at p.</summary>
        bool CarTouches(Vector3 p)
        {
            if (carPivot == null) return false;
            var local = Quaternion.Inverse(carPivot.rotation) * (p - carPivot.position);
            return Mathf.Abs(local.x) < 2.76f && Mathf.Abs(local.z) < 1.2f;
        }

        /// <summary>From the car's run on, the lane short of the pillar is the wreck's: no one is sent or walks
        /// back into it (the left edge follows him up to x 97.6, then stays). The camera's bounds are left alone.</summary>
        void KeepClearOfWreck()
        {
            float min = Mathf.Min(StreetPlan.WreckClearX, hero.transform.position.x);
            if (min <= hero.arenaMinX + 0.01f) return;
            hero.arenaMinX = min;
            foreach (var e in Enemy.All) if (e != null) e.arenaMinX = Mathf.Max(e.arenaMinX, min);
        }

        /// <summary>Feet on the pavement, or on the road a kerb's step below it.</summary>
        static float StreetGround(Vector3 p) =>
            Mathf.Lerp(StreetPlan.RoadY, 0f, Mathf.InverseLerp(StreetPlan.KerbEdgeZ - 0.1f, StreetPlan.KerbEdgeZ + 0.1f, p.z));

        /// <summary>Progress along a walk: easing in and out over its first and last 15%, a steady pace between
        /// (1.18x the average), so a walk cycle played at that pace keeps its feet on the ground.</summary>
        static float WalkPace(float u)
        {
            const float a = 0.15f, vmax = 1f / (1f - a);
            u = Mathf.Clamp01(u);
            if (u < a) return vmax * u * u / (2f * a);
            if (u <= 1f - a) return vmax * (a * 0.5f + u - a);
            return 1f - vmax * (1f - u) * (1f - u) / (2f * a);
        }

        /// <summary>A puppet (one of them, or him) walked through waypoints at `speed` m/s, turning smoothly to face
        /// the way he goes (depth included, LookAtXZ), his feet on `ground`; at the end turned to face `endFace`
        /// (-1/+1; 0 leaves him as he is) and `done` called. Story time, or game time in a fight (`story` false).
        /// The caller plays the walk (or run) at the pace it covers.</summary>
        IEnumerator StrollPath(Fighter f, System.Action<Vector3> pose, Vector3[] pts, float speed, System.Func<Vector3, float> ground, bool story,
                               System.Action frame = null, int endFace = 0, System.Action done = null)
        {
            if (f == null || pts == null || pts.Length == 0) yield break;
            var p = f.transform.position;
            var fwd = f.transform.rotation * Vector3.right;                  // a fighter faces his local +x
            float yaw = Mathf.Atan2(fwd.z, fwd.x) * Mathf.Rad2Deg;
            int i = 0;
            while (i < pts.Length)
            {
                float dt = story ? StoryDt : Time.deltaTime;
                var to = pts[i];
                var d = new Vector3(to.x - p.x, 0f, to.z - p.z);
                float left = d.magnitude, step = speed * dt;
                if (left > 1e-3f) yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg, 540f * dt);
                if (story && SkipNow) { p = pts[pts.Length - 1]; i = pts.Length; }
                else if (step >= left) { p = new Vector3(to.x, p.y, to.z); i++; }
                else p += d / left * step;
                p.y = ground(p);
                pose(p);
                f.LookAtXZ(p + new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0f, Mathf.Sin(yaw * Mathf.Deg2Rad)));
                frame?.Invoke();
                yield return null;
                if (f == null) yield break;
            }
            if (endFace != 0)
            {
                float want = endFace > 0 ? 0f : 180f;
                while (Mathf.Abs(Mathf.DeltaAngle(yaw, want)) > 2f && !(story && SkipNow))
                {
                    yaw = Mathf.MoveTowardsAngle(yaw, want, 540f * (story ? StoryDt : Time.deltaTime));
                    f.LookAtXZ(f.transform.position + new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0f, Mathf.Sin(yaw * Mathf.Deg2Rad)));
                    frame?.Invoke();
                    yield return null;
                    if (f == null) yield break;
                }
                f.SetFacing(endFace);
            }
            done?.Invoke();
        }

        // ================================================================ the finale

        IEnumerator DriverFinale(Enemy d)
        {
            driverStage = 5;
            StreetCheckpoint = -1;
            sReady = false;
            Sound.Tension(false);
            foreach (var e in Enemy.All.ToArray()) if (e != null && e != d && e.Alive) e.PuppetDie();
            yield return RealWait(0.8f);
            hero.CinePuppet();
            d.Puppet();
            // on the ground clear of the wreck (him kneeling at his left) and of the bus (his legs to his right)
            float dx = Mathf.Clamp(d.transform.position.x, carWrecked ? StreetPlan.WreckClearX + 1.4f : SZ0(3) + 3f, StreetPlan.LaneX1 - 1.4f);
            d.PuppetPose(new Vector3(dx, 0f, 0f));
            d.SetFacing(-1);
            d.PuppetPlay(d.body.Has("lie_up") ? "lie_up" : "lie", 0.2f);
            hero.CinePose(new Vector3(dx - 0.85f, 0f, 0f));
            hero.SetFacing(1);
            if (hero.body.Has("kneel_hold")) hero.body.Play("kneel_hold", 0.2f);
            barsPx = 92f;
            Sound.Play(Sfx.Cloth, 1f, d.Chest);
            Sound.Play(Sfx.Breath, 0.8f, hero.Chest, 0.9f);
            var dh = HeadOf(d);
            System.Action close = () => EndShot(new Vector3(dx - 0.2f, 0.9f, -2.2f), new Vector3(dx - 0.3f, 0.6f, 0f), 34f);
            System.Action onHim = () => EndShot(new Vector3(dx + 1.4f, 0.7f, -1.4f), dh != null ? dh.position : d.Chest, 30f);
            System.Action onYou = () => EndShot(new Vector3(dx + 0.6f, 1.0f, -1.6f), HeadC(), 28f);
            yield return Hold(1.0f, close);
            Say("THE DRIVER", "You'd sit in the back and never say a word.", 3.4f, dh);
            yield return Hold(3.6f, onHim);
            // the player asks
            StartCoroutine(HoldWhile(() => scAsk != ScAsk.None, onYou));
            yield return ChooseFrom("\"Who is V?\"", "\"What's waiting at home?\"");
            if (chosen == 0)
            {
                Say("YOU", "Who is V?", 1.6f);
                yield return Hold(1.8f, onYou);
                Sound.Play(Sfx.BruteLaugh, 0.7f, d.Chest, 1.1f);
                Say("THE DRIVER", "Ha... Ask him yourself. You two go way back.", 2.8f, dh);
                yield return Hold(3.0f, onHim);
                Say("THE DRIVER", "And go home first. You'll want to see what's waiting.", 3.0f, dh);
                yield return Hold(3.2f, onHim);
            }
            else
            {
                Say("YOU", "What's waiting at home?", 1.8f);
                yield return Hold(2.0f, onYou);
                Say("THE DRIVER", "...", 1.4f, dh);
                yield return Hold(1.6f, onHim);
                Say("THE DRIVER", "You'll see.", 1.8f, dh);
                yield return Hold(2.0f, onHim);
            }
            Say("THE DRIVER", "We got there first.", 2.4f, dh);
            Sound.Play(Sfx.Heartbeat, 0.9f);
            yield return Hold(2.6f, onHim);
            yield return Hold(1.4f, onYou);
            Say("YOU", "...Elena.", 1.8f);
            Sound.Play(Sfx.Riser, 0.6f);
            yield return Hold(1.8f, onYou);
            // he lets go, gets up, and runs for the side street, north, home: at a running pace whatever the
            // distance, facing the way he runs, and round the man on the ground (z 0.95), never over him
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.25f);
            yield return Hold(0.5f, close);
            const float sideX = 108f;
            var h0 = hero.transform.position;
            var run = h0.x < sideX
                ? new[] { new Vector3(h0.x + 0.3f, 0f, 0.95f), new Vector3(sideX, 0f, 0.95f), new Vector3(sideX, 0f, 16f) }
                : new[] { new Vector3(sideX, 0f, 0.5f), new Vector3(sideX, 0f, 16f) };
            hero.body.Play("run", 0.15f, true, 1.25f);
            yield return StrollPath(hero, hero.CinePose, run, 5.0f, StreetGround, true, () =>
            {
                var p = hero.transform.position;
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.28f; Sound.Play(Sfx.Footstep, 0.6f, p, 1.1f); }
                // along the pavement with him, then held at the corner as he goes up the side street, away from us
                float up = Mathf.Clamp01(p.z / 3f);
                EndShot(new Vector3(Mathf.Lerp(p.x - 1.5f, 106.5f, up), 1.6f, -8f), new Vector3(Mathf.Lerp(p.x, sideX, up), 1.2f, Mathf.Max(0f, p.z * 0.6f)), 38f);
            });
            yield return Hold(1.0f, () => EndShot(new Vector3(106.5f, 1.6f, -8f), new Vector3(108f, 1.2f, 8f), 38f));
            cardAt = Time.unscaledTime;
            cardLine = "We got there first.";
            cardTitle = "THE RING";
            yield return RealWait(6.0f);
            phase = Phase.End;
        }

        // ================================================================ on screen

        void StreetGUI(float w, float h)
        {
            float strobe = TrainStrobe();
            if (strobe > 0f) Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, strobe));
            if (phase == Phase.Street && sClear && driverStage == 0 && sZone < 3 && Time.unscaledTime < sGoUntil)
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
            if (carPrompt && !carDodged)
            {
                // the car is coming: SWIPE
                float a = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 14f);
                var r = new Rect(w * 0.5f - 230, h * 0.22f, 460, 96);
                Fill(r, new Color(0f, 0f, 0f, 0.75f));
                Fill(new Rect(r.x, r.y, 6, r.height), new Color(0.8f, 0.06f, 0.07f));
                big.fontSize = 52;
                big.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(r.x, r.y + 6, r.width, 56), Spaced("DIVE CLEAR"), big);
                big.fontSize = 72;
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(new Rect(r.x, r.y + 62, r.width, 26), Application.isMobilePlatform ? "SWIPE  ANY WAY" : "Q  /  E  /  W  /  S", cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            // the Driver's health and his phase, along the top
            if (driver != null && driverStage >= 1 && driverStage < 5 && driver.Alive)
            {
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 96f;           // below the pause and ? buttons (top centre)
                Fill(new Rect(x - 2, y - 2, bw + 4, 14), new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(x, y, bw * driver.HealthFraction, 10), new Color(0.85f, 0.85f, 0.85f));
                foreach (float mk in new[] { 0.6f, 0.3f }) Fill(new Rect(x + bw * mk - 1, y - 3, 2, 16), new Color(0.8f, 0.06f, 0.07f));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
                string ph = driverStage >= 4 ? "IV  ·  BARE HANDS" : driverStage == 3 ? "III  ·  THE CAR" : driverStage == 2 ? "II  ·  THE TIRE IRON" : "I  ·  THE BOXER";
                GUI.Label(new Rect(x, y - 30, bw, 26), Spaced("THE DRIVER") + "     " + ph, cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
        }

        /// <summary>A plain fade over everything (the cut from the bank, the street's first frame).</summary>
        void BlackoutGUI(float w, float h)
        {
            if (blackout > 0.001f) Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, blackout));
        }

        /// <summary>The bank's last shot: fade, and on into the street (no chapter card: the chapter goes on).</summary>
        IEnumerator OnToTheStreet()
        {
            float t0 = 0f;
            while (t0 < 1.2f) { t0 += StoryDt; blackout = t0 / 1.2f; yield return null; }
            blackout = 1f;
            StreetCheckpoint = -1;
            StreetFrom = 0;
            SceneManager.LoadScene("Story_Street");
        }
    }
}
