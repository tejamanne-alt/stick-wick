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
        bool carPrompt, carDodged;
        float carPromptAt;
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
            while (t0 < s) { t0 += Time.unscaledDeltaTime; blackout = 1f - t0 / s; yield return null; }
            blackout = 0f;
        }

        // ================================================================ S0: the box

        IEnumerator StreetOpening()
        {
            float door = StreetPlan.BankDoorX;
            hero.CinePose(new Vector3(door, 0f, 0.4f));
            if (hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 0.75f);
            // the car's four, out on the pavement already, by the open door
            var men = new Enemy[4];
            float[] stand = { 9.6f, 11.2f, 12.6f, 14.4f };
            for (int i = 0; i < 4; i++)
            {
                if (director == null) break;
                var e = director.SpawnScripted(i == 3 ? Ru : Br, SpawnKind.RightDoor);
                if (e == null) continue;
                e.Puppet();
                e.PuppetPose(new Vector3(StreetPlan.KerbCarX - 1.0f + i * 0.5f, 0f, -1.9f));
                e.SetFacing(-1);
                e.PuppetPlay("idle", 0f);
                men[i] = e;
            }
            Sound.Play(Sfx.Door, 0.5f, new Vector3(door, 1f, 3f), 0.8f);
            // he comes out of the doors and down the steps into the white morning
            yield return FadeIn(1.0f);
            float tt = 0f;
            var exit0 = hero.transform.position;
            while (tt < 3.2f)
            {
                tt += Time.unscaledDeltaTime;
                hero.CinePose(Vector3.Lerp(exit0, new Vector3(door + 6.0f, 0f, 0f), Mathf.SmoothStep(0f, 1f, tt / 3.2f)));
                // the four walk out across the pavement to meet him
                for (int i = 0; i < 4; i++)
                {
                    if (men[i] == null) continue;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tt - 0.6f) / 2.4f));
                    men[i].PuppetPose(Vector3.Lerp(new Vector3(StreetPlan.KerbCarX - 1.0f + i * 0.5f, 0f, -1.9f), new Vector3(stand[i], 0f, i == 2 ? 0.9f : 0f), k));
                    if (tt - Time.unscaledDeltaTime < 0.6f && tt >= 0.6f) men[i].PuppetPlay("walk", 0.2f);
                }
                EndShot(new Vector3(door + 1.5f, 1.5f, -6.5f), new Vector3(Mathf.Lerp(door + 3f, 9f, tt / 3.2f), 1.2f, 0f), 40f);
                yield return null;
            }
            foreach (var m0 in men) if (m0 != null) m0.PuppetPlay("idle", 0.2f);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
            var lead = men[0];
            yield return Hold(0.6f, () => EndShot(new Vector3(4.5f, 1.4f, -4.2f), new Vector3(8.5f, 1.3f, 0f), 34f));
            Say("THUG", "The box. Hand it over.", 2.4f, lead != null ? HeadOf(lead) : null);
            yield return Hold(2.4f, () => EndShot(new Vector3(6.8f, 1.5f, -2.6f), new Vector3(9.6f, 1.55f, 0f), 30f));
            Say("YOU", "It's empty.", 1.6f);
            yield return Hold(2.2f, () => EndShot(new Vector3(6.6f, 1.45f, -2.4f), new Vector3(1.6f, 1.45f, 0f), 30f));
            if (hero.body.Has("idle")) hero.body.Play("idle", 0.25f);              // the guard comes up
            Sound.Play(Sfx.Cloth, 0.8f);
            Say("YOU", "...Come and look.", 2.0f);
            yield return Hold(2.0f, () => EndShot(new Vector3(4.6f, 1.3f, -3.8f), new Vector3(9.0f, 1.25f, 0f), 34f));
            Say("THUG", "Have it your way.", 1.8f, lead != null ? HeadOf(lead) : null);
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
            SetArena(SZ0(z) + (z == 0 ? -10f : 0.6f), SZ1(z) - 0.6f);
            SetStreetDoors(z);
            if (!cut && z > 0 && director != null) director.ShowBanner($"{z + 1}  ·  {StreetPlan.ZoneName[z]}", 2.2f);
            if (z == 2) { nextTrainAt = Time.time + (cut ? 2.5f : 1.0f); }
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
            At(SpawnKind.RightDoor, new Vector3(x1 - 0.8f, 0f, 0f));
            At(SpawnKind.LeftDoor, new Vector3(x0 + 0.8f, 0f, 0f));
            // the back: out of a shop doorway, the side street, the arches
            At(SpawnKind.Background, new Vector3(z == 3 ? 108f : mid + 2.5f, 0f, z == 3 ? 6f : 3.0f));
            At(SpawnKind.UpperDoor, new Vector3(mid - 3f, 0f, 3.0f));
            At(SpawnKind.Ceiling, new Vector3(mid, 0f, 3.0f));
        }

        void UpdateStreet()
        {
            if (blackout > 0f && driverStage < 5 && sReady && !hero.CinematicLock) blackout = Mathf.MoveTowards(blackout, 0f, Time.unscaledDeltaTime);
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
                if (sQueued < list.Length && streetMen.Count < cap && Time.time >= sNextSpawn && director != null)
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
            SetArena(SZ0(sZone) + 0.6f, SZ1(sZone + 1) - 0.6f);
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
            SetArena(SZ0(3) + 0.6f, SZ1(3) - 0.6f);
            if (!cut) yield return RealWait(1.0f);
            // the black car screeches in off the road and stops at the kerb
            var park = new Vector3(StreetPlan.DriverCarX, 0f, StreetPlan.DriverCarZ);
            if (carPivot != null) { carPivot.position = park + Vector3.right * 34f; carPivot.rotation = Quaternion.Euler(0f, 180f, 0f); }
            Sound.Play(Sfx.CarRev, 1f, park + Vector3.right * 20f);
            barsPx = 92f;
            float tt = 0f;
            while (tt < 2.0f)
            {
                tt += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(tt / 1.8f), 3f);
                if (carPivot != null) carPivot.position = Vector3.Lerp(park + Vector3.right * 34f, park, k);
                EndShot(new Vector3(112f, 1.3f, -9.5f), new Vector3(Mathf.Lerp(130f, StreetPlan.DriverCarX, k), 1.0f, -2f), 40f);
                yield return null;
            }
            if (cam != null) cam.Shake(0.06f, 0.3f);
            Sound.Play(Sfx.CarDoor, 0.9f, park);
            SpawnDriver(park + new Vector3(-1.2f, 0f, 1.5f));
            if (driver == null) { phase = Phase.End; yield break; }
            driver.PuppetPlay("walk", 0.2f);
            tt = 0f;
            var d0 = driver.transform.position;
            while (tt < 2.0f)
            {
                tt += Time.unscaledDeltaTime;
                driver.PuppetPose(Vector3.Lerp(d0, new Vector3(StreetPlan.DriverCarX - 4.5f, 0f, 0f), Mathf.SmoothStep(0f, 1f, tt / 2.0f)));
                EndShot(new Vector3(StreetPlan.DriverCarX - 9f, 1.4f, -5.5f), driver.transform.position + Vector3.up * 1.4f, 36f);
                yield return null;
            }
            driver.PuppetPlay("idle", 0.2f);
            Sound.Play(Sfx.Cloth, 0.8f, driver.Chest);
            Say("THE DRIVER", "Kovac said you were dead. Kovac was always wrong about something.", 3.2f, HeadOf(driver));
            yield return Hold(3.0f, () => EndShot(driver.transform.position + new Vector3(-2.4f, 1.3f, -2.4f), HeadOf(driver).position, 30f));
            Say("THE DRIVER", "Let's see what's left of you.", 2.2f, HeadOf(driver));
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
            float hp = driver.HealthFraction;
            if (driverStage == 1 && hp <= 0.6f && driver.Alive) { driverStage = 2; StartCoroutine(TireIron()); }
            else if (driverStage == 2 && hp <= 0.3f && driver.Alive && driver.DriverPhase == 2) { driverStage = 3; StartCoroutine(TheCar()); }
        }

        /// <summary>II: he backs off to the car's boot and comes back with a tire iron.</summary>
        IEnumerator TireIron()
        {
            driver.Puppet();
            var boot = new Vector3(StreetPlan.DriverCarX + 2.6f, 0f, -1.4f);
            var from = driver.transform.position;
            driver.SetFacing(1);
            driver.PuppetPlay("walk", 0.15f);
            float tt = 0f;
            while (tt < 1.2f) { tt += Time.deltaTime; driver.PuppetPose(Vector3.Lerp(from, boot, tt / 1.2f)); yield return null; }
            Sound.Play(Sfx.CarDoor, 1f, boot);
            driver.PuppetPlay("idle", 0.1f);
            yield return new WaitForSeconds(0.5f);
            driver.ShowIron(true);
            Sound.Play(Sfx.PipeHit, 0.6f, boot, 1.3f);
            driver.SetFacing(-1);
            driver.DriverPhase = 2;
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
            var door = new Vector3(StreetPlan.DriverCarX - 0.6f, 0f, -1.9f);
            var from = driver.transform.position;
            driver.PuppetPlay("walk", 0.15f, 1.4f);
            float tt = 0f;
            while (tt < 1.0f) { tt += Time.deltaTime; driver.PuppetPose(Vector3.Lerp(from, door, tt / 1.0f)); yield return null; }
            driver.Hide(true);
            Sound.Play(Sfx.CarDoor, 1f, door);
            Sound.Play(Sfx.CarRev, 1f, door);
            ShowStreetBanner("III  ·  THE CAR");
            // he pulls out onto the pavement, then floors it along it
            var park = carPivot != null ? carPivot.position : door;
            var start = new Vector3(Mathf.Min(SZ1(3) - 3f, hero.transform.position.x + 16f), 0f, -0.1f);
            tt = 0f;
            while (tt < 1.0f && carPivot != null)
            {
                tt += Time.deltaTime;
                carPivot.position = Vector3.Lerp(park, start, Mathf.SmoothStep(0f, 1f, tt / 1.0f));
                yield return null;
            }
            float x = start.x, v = 4f;
            float crashAt = StreetPlan.CrashX + 2.4f;
            carPrompt = false;
            carDodged = false;
            bool hit = false;
            var gi = GestureInput.Instance;
            if (gi != null) gi.Flush();
            while (x > crashAt && carPivot != null)
            {
                v = Mathf.Min(17f, v + 14f * Time.deltaTime);
                x -= v * Time.deltaTime;
                carPivot.position = new Vector3(x, 0f, -0.1f);
                float front = x - 2.4f, hx = hero.transform.position.x;
                if (!carPrompt && !carDodged && front - hx < 7.5f && front > hx)
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
                if (!carDodged && !hit && front < hx + 0.5f && front > hx - 2.5f && hero.transform.position.z < 0.8f)
                {
                    hit = true;
                    carPrompt = false;
                    hero.TakeHit(new HitInfo { damage = 26f, direction = -1, knockback = 9f, knockdown = true, heavy = true });
                    Sound.Play(Sfx.PunchHeavy, 1f, hero.Chest, 0.7f);
                    if (cam != null) cam.Shake(0.3f, 0.3f);
                }
                yield return null;
            }
            carPrompt = false;
            // into the pillar
            if (carPivot != null)
            {
                carPivot.position = new Vector3(crashAt, 0f, -0.1f);
                carPivot.rotation = Quaternion.Euler(0f, 192f, 0f);
                carPivot.localScale = new Vector3(0.9f, 0.96f, 1f);
            }
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
            // he falls out of the wreck, bleeding, bare-handed, and comes on
            Sound.Play(Sfx.CarDoor, 1f, crash);
            driver.PuppetPose(new Vector3(crashAt + 0.4f, 0f, -1.4f));
            driver.SetFacing(1);
            driver.Hide(false);
            driver.PuppetPlay("dazed", 0.1f);
            Sound.Play(Sfx.BruteHurt, 0.9f, driver.Chest, 0.8f);
            yield return new WaitForSeconds(0.6f);
            float t2 = 0f;
            var w0 = driver.transform.position;
            driver.PuppetPlay("walk", 0.2f, 0.7f);
            while (t2 < 1.0f) { t2 += Time.deltaTime; driver.PuppetPose(Vector3.Lerp(w0, new Vector3(crashAt + 1.6f, 0f, 0f), t2)); yield return null; }
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
            tt = 0f;
            var p2 = hero.transform.position;
            while (tt < 0.4f) { tt += Time.deltaTime; hero.CinePose(Vector3.Lerp(p2, new Vector3(p2.x, 0f, 0f), tt / 0.4f)); yield return null; }
            hero.CineRelease();
        }

        void ShowStreetBanner(string s)
        {
            streetBanner = s;
            streetBannerAt = Time.unscaledTime;
            if (director != null) director.ShowBanner(s, 2.2f);
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
            float dx = Mathf.Clamp(d.transform.position.x, SZ0(3) + 3f, SZ1(3) - 4f);
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
            Say("YOU", "Who sent you?", 2.0f);
            yield return Hold(2.2f, onYou);
            Sound.Play(Sfx.BruteLaugh, 0.7f, d.Chest, 1.1f);
            Say("THE DRIVER", "...You'll find out at home.", 2.6f, dh);
            yield return Hold(2.8f, onHim);
            Say("THE DRIVER", "We got there first.", 2.4f, dh);
            Sound.Play(Sfx.Heartbeat, 0.9f);
            yield return Hold(2.6f, onHim);
            yield return Hold(1.4f, onYou);
            Say("YOU", "...Elena.", 1.8f);
            Sound.Play(Sfx.Riser, 0.6f);
            yield return Hold(1.8f, onYou);
            // he lets go, gets up, and runs for the side street, north, home
            if (hero.body.Has("idle")) hero.body.Play("idle", 0.25f);
            yield return Hold(0.5f, close);
            hero.SetFacing(1);
            hero.body.Play("run", 0.15f, true, 1.4f);
            var h0 = hero.transform.position;
            float tt = 0f;
            while (tt < 3.4f)
            {
                tt += Time.unscaledDeltaTime;
                float k = tt / 3.4f;
                var p = k < 0.45f ? Vector3.Lerp(h0, new Vector3(108f, 0f, 0.5f), k / 0.45f)
                                  : Vector3.Lerp(new Vector3(108f, 0f, 0.5f), new Vector3(108f, 0f, 16f), (k - 0.45f) / 0.55f);
                if (k >= 0.45f) hero.transform.rotation = Quaternion.Euler(0f, -90f, 0f);        // up the side street, away from us
                hero.CinePose(p);
                if (Time.unscaledTime > footAt) { footAt = Time.unscaledTime + 0.28f; Sound.Play(Sfx.Footstep, 0.6f, hero.transform.position, 1.1f); }
                EndShot(new Vector3(106.5f, 1.6f, -8f), new Vector3(108f, 1.2f, Mathf.Max(0f, p.z * 0.6f)), 38f);
                yield return null;
            }
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
                float bw = Mathf.Min(520f, w - 80f), x = (w - bw) * 0.5f, y = 54f;
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
            while (t0 < 1.2f) { t0 += Time.unscaledDeltaTime; blackout = t0 / 1.2f; yield return null; }
            blackout = 1f;
            StreetCheckpoint = -1;
            StreetFrom = 0;
            SceneManager.LoadScene("Story_Street");
        }
    }
}
