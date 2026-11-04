using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The last wave of every floor belongs to THE BRUTE, and is shot like the end of an
    /// action film.
    ///
    /// ARRIVAL. The wave starts with his men. Once half of them are down (or the room is nearly
    /// empty, or time has passed) the lights stutter, the floor shakes and he walks in through a
    /// door. His men see him and run for the exits: the boss fight is one on one. (Hit a runner on
    /// the way out for COWARD!)
    ///
    /// ENTRANCE. Letterbox bars; a low tracking shot as he walks in, each step shaking the room;
    /// a hard cut to his face as he roars and the name card slams in; an over-the-shoulder two-shot
    /// as the hero beckons; then the bars slide away and FIGHT!
    ///
    /// THE DUEL. A boss health bar with a phase mark at half. His specials are all telegraphed in
    /// red: the BULL CHARGE (dodge through it and he runs into the wall, stunned and taking full
    /// damage), the GROUND SLAM (be in the air when he lands), the bear hug (tap to break it). At
    /// half health he is ENRAGED: a roar that blows the hero back, blood red, faster, more charges,
    /// slams twice.
    ///
    /// FINALE. The blow that would kill him does not: he staggers at 1 health and the scene takes
    /// both fighters. A three-hit flurry, each hit a hard cut; an uppercut that launches him; the
    /// hero leaps after him into the air (slow motion, the camera circling them); a spinning kick
    /// that drives him into the floor; a shockwave, the lamps swing; the hero lands beside him.
    /// BRUTE DEFEATED. Then the floor-clear victory.
    ///
    /// Intro timing is in real seconds; the finale runs on game time, so its slow motion and
    /// hit-stops stretch the edit the way they stretch the action.</summary>
    public class BossDirector : MonoBehaviour
    {
        public static BossDirector Instance { get; private set; }

        public enum Stage { None, Armed, Intro, Fight, Finale, Done }
        public Stage Current { get; private set; }
        public Enemy Boss { get; private set; }

        public string bossName = "THE BRUTE";
        public string tagline = "HE OWNS THIS BUILDING";

        /// <summary>The wave cannot end while he is due, on his way in, or still standing.</summary>
        public bool Holding => Current == Stage.Armed || Current == Stage.Intro || Current == Stage.Finale ||
                               (Current == Stage.Fight && Boss != null && Boss.Alive);
        /// <summary>A cutscene owns the screen: the HUD steps aside.</summary>
        public bool Cinematic => Current == Stage.Intro || Current == Stage.Finale ||
                                 (Current == Stage.Done && BarsPixels > 1f && (VictoryCinematic.Instance == null || !VictoryCinematic.Instance.Active));

        // --- read by the HUD (720-unit-high GUI space)
        public float BarsPixels { get; private set; }
        public float NameAlpha { get; private set; }
        public float TagAlpha { get; private set; }
        public float NameSlide { get; private set; }          // 0 = in place
        public float DefeatAlpha { get; private set; }
        public float HpAlpha { get; private set; }
        public float Hp { get; private set; } = 1f;
        public float HpLag { get; private set; } = 1f;
        public bool HpEnraged => Boss != null && Boss.Enraged;
        public float EnragedAt { get; private set; } = -10f;
        public string DefeatSub { get; private set; }

        const float Bars = 92f;
        WaveDirector wd;
        HeroController hero;
        float armTime, dueAt = -1f;
        bool sawEnrage;

        void Awake() { Instance = this; wd = GetComponent<WaveDirector>(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>The boss wave has begun: he comes once his men have had their go.</summary>
        public void Arm()
        {
            Current = Stage.Armed;
            armTime = Time.time;
            dueAt = -1f;
            Boss = null;
            Hp = HpLag = 1f;
            HpAlpha = DefeatAlpha = NameAlpha = TagAlpha = 0f;
            sawEnrage = false;
            EnragedAt = -10f;
        }

        /// <summary>A new floor: forget the last boss.</summary>
        public void ResetFloor()
        {
            Current = Stage.None;
            Boss = null;
            BarsPixels = DefeatAlpha = HpAlpha = 0f;
        }

        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        void Update()
        {
            if (wd == null) wd = GetComponent<WaveDirector>();
            if (hero == null && wd != null) hero = wd.hero;
            if (hero == null) return;
            switch (Current)
            {
                case Stage.Armed: UpdateArmed(); break;
                case Stage.Intro: UpdateIntro(); break;
                case Stage.Fight: UpdateFight(); break;
                case Stage.Finale: UpdateFinale(); break;
                case Stage.Done: UpdateDone(); break;
            }
        }

        // ================================================================== arrival

        void UpdateArmed()
        {
            if (hero.IsDead || wd.CurrentPhase != WaveDirector.Phase.Fighting) return;
            int alive = MinionsAlive();
            bool due = wd.WaveKills >= Mathf.CeilToInt(wd.WaveSize * 0.5f)
                       || (wd.Upcoming.Count == 0 && alive <= 1)
                       || Time.time - armTime > 18f;
            if (!due) { dueAt = -1f; return; }
            if (dueAt < 0f) dueAt = Time.unscaledTime;
            // take the hero between moves if we can, but do not wait for ever
            bool late = Time.unscaledTime - dueAt > 4f;
            if (hero.Free || (late && !hero.InRampage && !hero.RampageIntro && !hero.CinematicLock)) StartIntro();
        }

        static int MinionsAlive()
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e.Alive && !e.Boss && !e.Fleeing) n++;
            return n;
        }

        // ================================================================== the entrance

        const float ShotA = 1.9f, ShotB = 3.7f, OutAt = 5.2f, OutLen = 0.9f;
        float t, outStart, barsAtOut;
        int dirIn;                          // the way he walks in (+1 = towards +x)
        bool roared, heroTaunt, bossTaunt;
        Vector3 lastPos, lastLook;
        float lastFov;

        void StartIntro()
        {
            var h = hero.transform.position;
            // the ground-floor door nearer the hero, unless he is standing in it
            float right = 12.2f - h.x, left = h.x + 12.2f;
            var door = right < left ? SpawnKind.RightDoor : SpawnKind.LeftDoor;
            if (Mathf.Min(right, left) < 5f) door = door == SpawnKind.RightDoor ? SpawnKind.LeftDoor : SpawnKind.RightDoor;
            wd.CancelIncoming();
            Boss = wd.SpawnBoss(door);
            if (Boss == null) { Current = Stage.None; return; }
            Current = Stage.Intro;
            t = 0f;
            outStart = -1f;
            roared = heroTaunt = bossTaunt = false;
            NameAlpha = TagAlpha = 0f;
            NameSlide = 1f;
            Hp = HpLag = 1f;

            // his men take one look and run
            foreach (var e in Enemy.All.ToArray())
                if (e != Boss && e.Alive) e.Flee(Boss.transform.position);

            Boss.BeginBossIntro();
            dirIn = h.x >= Boss.transform.position.x ? 1 : -1;
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            hero.EndRampage();
            hero.CineLock();
            hero.SetFacing(-dirIn);                  // facing the door
            if (hero.body != null) hero.body.Play("idle", 0.15f);
            var cam = CameraDirector.Instance;
            if (cam != null) { cam.CancelOrbit(); cam.Shake(0.1f, 0.35f); }
            Sound.Tension(true);
            Sound.Play(Sfx.Boom, 0.9f);
            Sound.Play(Sfx.Riser, 0.55f);
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.04f);
        }

        void UpdateIntro()
        {
            t += Time.unscaledDeltaTime;
            float dt = Time.unscaledDeltaTime;
            if (Boss == null) { EndIntro(); return; }
            var input = GestureInput.Instance;
            if (outStart < 0f && t > 0.8f && input != null && input.AnyPressThisFrame) BeginOut();

            // the cues
            if (outStart < 0f)
            {
                if (!roared && t >= ShotA + 0.3f)
                {
                    roared = true;
                    Boss.IntroRoar();
                    Sound.Play(Sfx.CineMassive, 1f);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.18f, 0.9f);
                    if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.05f);
                    foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
                }
                if (!heroTaunt && t >= ShotB + 0.15f)
                {
                    heroTaunt = true;
                    if (hero.body != null) hero.body.Play(hero.body.Has("taunt") ? "taunt" : "idle", 0.15f);
                    Sound.Play(Sfx.Cloth, 0.8f, hero.transform.position);
                }
                if (!bossTaunt && t >= ShotB + 0.8f) { bossTaunt = true; Boss.IntroTaunt(); }
                if (t >= OutAt) BeginOut();
            }

            // the cards
            bool going = outStart >= 0f;
            if (roared && !going && t < ShotB + 0.6f)
            {
                NameAlpha = Mathf.MoveTowards(NameAlpha, 1f, dt * 6f);
                NameSlide = Mathf.MoveTowards(NameSlide, 0f, dt * 4f);
                if (t > ShotA + 0.75f) TagAlpha = Mathf.MoveTowards(TagAlpha, 1f, dt * 3f);
            }
            else
            {
                NameAlpha = Mathf.MoveTowards(NameAlpha, 0f, dt * 3f);
                TagAlpha = Mathf.MoveTowards(TagAlpha, 0f, dt * 3f);
            }

            // the bars
            BarsPixels = !going ? Mathf.Lerp(0f, Bars, Ease(0f, 0.6f, t))
                                : Mathf.Lerp(barsAtOut, 0f, Ease(outStart, outStart + OutLen * 0.9f, t));

            // the shots
            var cam = CameraDirector.Instance;
            if (cam == null) { if (going && t >= outStart + OutLen) EndIntro(); return; }
            if (!going) cam.CancelOrbit();
            if (going)
            {
                cam.Scripted(lastPos, lastLook, 1f - Ease(outStart, outStart + OutLen, t), lastFov);
                if (t >= outStart + OutLen) EndIntro();
                return;
            }
            Vector3 pos, look;
            float fov;
            var b = Boss.transform.position;
            if (t < ShotA)
            {
                // A: low, in front of him, dollying back as he walks in: he fills the frame
                float u = Ease(0f, ShotA, t);
                pos = b + new Vector3(dirIn * 3.0f, 0.22f, -2.3f);
                look = b + new Vector3(-dirIn * 0.2f, 1.15f, 0f);
                fov = Mathf.Lerp(42f, 36f, u);
            }
            else if (t < ShotB)
            {
                // B: his face, from below, a slow push-in through the roar
                float u = Ease(ShotA, ShotB, t);
                var c = Boss.Chest;
                pos = c + Vector3.Lerp(new Vector3(dirIn * 1.9f, -0.25f, -1.7f), new Vector3(dirIn * 1.45f, -0.2f, -1.35f), u);
                look = c + Vector3.up * 0.45f;
                fov = 30f;
            }
            else
            {
                // C: over the hero's shoulder, the two of them
                float u = Ease(ShotB, OutAt, t);
                var h = hero.transform.position;
                int toBoss = b.x >= h.x ? 1 : -1;
                pos = h + new Vector3(-toBoss * 1.7f, 1.45f, -3.3f) + new Vector3(toBoss * 0.3f, -0.1f, 0.3f) * u;
                look = Vector3.Lerp(hero.Chest, Boss.Chest, 0.68f);
                fov = Mathf.Lerp(37f, 34f, u);
            }
            lastPos = pos; lastLook = look; lastFov = fov;
            cam.Scripted(pos, look, 1f, fov);
        }

        void BeginOut()
        {
            if (outStart >= 0f) return;
            outStart = t;
            barsAtOut = BarsPixels;
            if (lastFov <= 0f && Camera.main != null) { lastPos = Camera.main.transform.position; lastLook = hero.Chest; lastFov = 34f; }
            hero.CineRelease();
            if (Boss != null) Boss.Engage();
            wd.Announce("FIGHT!", 1.2f);
            Sound.Play(Sfx.CineHit, 0.8f);
        }

        void EndIntro()
        {
            if (outStart < 0f) BeginOut();
            BarsPixels = 0f;
            NameAlpha = TagAlpha = 0f;
            if (CameraDirector.Instance != null) CameraDirector.Instance.Scripted(Vector3.zero, Vector3.zero, 0f);
            if (hero.CinematicLock) hero.CineRelease();
            Current = Boss != null ? Stage.Fight : Stage.None;
        }

        // ================================================================== the duel

        void UpdateFight()
        {
            float dt = Time.unscaledDeltaTime;
            if (Boss == null || !Boss.Alive) { Current = Stage.Done; return; }
            Hp = Mathf.Clamp01(Boss.Health / Mathf.Max(1f, Boss.maxHealth));
            // the white "damage taken" segment catches up after a beat
            HpLag = HpLag > Hp ? Mathf.MoveTowards(HpLag, Hp, dt * (HpLag - Hp > 0.02f ? 0.45f : 1f)) : Hp;
            HpAlpha = Mathf.MoveTowards(HpAlpha, 1f, dt * 3f);
            if (Boss.Enraged && !sawEnrage) { sawEnrage = true; EnragedAt = Time.unscaledTime; }
        }

        // ================================================================== the finale

        bool staged;                         // false: the stagger (waiting for both to land)
        float sa, g;                         // stagger time (real), finale time (game)
        int side;                            // the hero hits towards +side
        float y0, bx0, apex, landX, bossApexX;
        Vector3 heroAirFrom, heroAirTo, heroDropFrom;
        bool blade;
        string[] flurry;
        string launchClip, airClip;
        Sfx hitSfx;
        readonly HashSet<int> fired = new HashSet<int>();

        /// <summary>Called by the boss as the killing blow lands (he is left at 1 health).</summary>
        public void BeginFinale(Enemy e)
        {
            if (Current == Stage.Finale || Current == Stage.Done) return;
            Boss = e;
            Current = Stage.Finale;
            staged = false;
            sa = g = 0f;
            fired.Clear();
            HpAlpha = 1f;
            Hp = 0f;
            hero.EndRampage();
            var tc = TimeController.Instance;
            if (tc != null) { tc.Clear(); tc.HitStop(8); tc.SlowMo(0.25f, 1.2f, 0.02f, 0.3f); }
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.08f);
            Sound.Play(Sfx.CineMassive, 1f);
            Sound.Play(Sfx.SlowmoIn, 0.8f);
            Sound.Tension(false);
            hero.CineLock();                     // physics on, so he finishes landing
            if (CameraDirector.Instance != null) CameraDirector.Instance.CancelOrbit();
            // anyone left goes home
            wd.CancelIncoming();
            foreach (var other in Enemy.All.ToArray())
                if (other != e && other.Alive) other.Vanish();
        }

        void UpdateFinale()
        {
            if (Boss == null) { FinishFinale(); return; }
            BarsPixels = Mathf.MoveTowards(BarsPixels, Bars, Time.unscaledDeltaTime * Bars * 3f);
            HpAlpha = Mathf.MoveTowards(HpAlpha, 0f, Time.unscaledDeltaTime * 2f);
            var cam = CameraDirector.Instance;
            if (cam != null) cam.CancelOrbit();        // a kill-cam from the last blow must not fight the cut
            if (!staged)
            {
                // A: he reels, in slow motion, the camera easing round him
                sa += Time.unscaledDeltaTime;
                if (hero.transform.position.x < Boss.transform.position.x) side = 1; else side = -1;
                hero.FaceTowards(Boss.transform.position.x);
                var piv = Boss.Chest;
                var rot = Quaternion.Euler(0f, Mathf.Lerp(-14f, 10f, Ease(0f, 1.4f, sa)) * side, 0f);
                if (cam != null) cam.Scripted(piv + rot * new Vector3(0f, -0.35f, -3.8f), piv, Ease(0f, 0.3f, sa), 34f);
                if ((Boss.Grounded && hero.Grounded && sa > 0.8f) || sa > 1.8f) SetMarks();
                return;
            }
            g += Time.deltaTime;
            Pose(out Vector3 cpos, out Vector3 clook, out float cfov);
            if (cam != null) cam.Scripted(cpos, clook, 1f, cfov);
        }

        bool Once(int id) { if (fired.Contains(id)) return false; fired.Add(id); return true; }

        // finale beats, in game seconds from the first strike
        const float H1 = 0.12f, H2 = 0.42f, H3 = 0.78f;          // the flurry
        const float Crouch = 0.92f, Upper = 1.03f, L = 1.22f;     // the launch
        const float Jump = 1.35f, Air = 1.78f, K = 2.05f;         // the leap and the kick
        const float Crash = K + 0.16f, Touch = K + 0.45f, Rise = K + 0.95f, End = K + 2.7f;

        /// <summary>Put both fighters on their marks, with room for the whole sequence.</summary>
        void SetMarks()
        {
            staged = true;
            g = 0f;
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            Boss.Puppet();
            hero.CinePuppet();
            hero.CineTrails(true);
            var b = Boss.transform.position;
            y0 = Boss.Grounded ? b.y : Fighter.GroundY(b);
            // the walkable stretch he stands on
            float x0 = -12.3f, x1 = 12.3f;
            if (LevelNav.Active)
            {
                int pl = LevelNav.PlatformAt(new Vector3(b.x, y0, 0f));
                if (pl >= 0 && LevelNav.RegionBounds(pl, out float a, out float c)) { x0 = Mathf.Max(x0, a + 0.4f); x1 = Mathf.Min(x1, c - 0.4f); }
            }
            const float back = 1.25f, ahead = 3.4f;
            // keep him where he is if the whole sequence fits; otherwise the smallest shift
            // (from either side), hidden by the cut to the first strike
            float Fit(int sd) => sd > 0 ? Mathf.Clamp(b.x, x0 + back, Mathf.Max(x0 + back, x1 - ahead))
                                        : Mathf.Clamp(b.x, Mathf.Min(x1 - back, x0 + ahead), x1 - back);
            float c1 = Fit(side), c2 = Fit(-side);
            if (Mathf.Abs(c2 - b.x) + 0.3f < Mathf.Abs(c1 - b.x)) { side = -side; bx0 = c2; }
            else bx0 = c1;
            // headroom over the whole path: he goes up, but not through the floor above
            float ceiling = Fighter.RoofY;
            if (LevelNav.Active)
                foreach (var pl in LevelNav.Platforms)
                {
                    if (pl.y <= y0 + 0.5f) continue;
                    for (float x = bx0 - side * back; side > 0 ? x <= bx0 + ahead : x >= bx0 - ahead; x += side * 0.5f)
                        if (pl.Contains(x)) { ceiling = Mathf.Min(ceiling, pl.y); break; }
                }
            apex = Mathf.Clamp(ceiling - y0 - 2.3f, 0.7f, 2.4f);
            bossApexX = bx0 + side * (0.85f + 0.8f);      // pushed back by the flurry, then up and along
            landX = bossApexX + side * 0.9f;
            Boss.PuppetPose(new Vector3(bx0, y0, 0f));
            Boss.SetFacing(-side);
            hero.CinePose(new Vector3(bx0 - side * back, y0, 0f));
            hero.SetFacing(side);
            Boss.PuppetPlay("idle", 0.2f, 0.35f);         // swaying on his feet

            blade = hero.Style == FightStyle.Sword || hero.Style == FightStyle.Pipe;
            flurry = blade ? new[] { "sword_a", "sword_b", "sword_c" } : new[] { "jab", "cross", "roundhouse" };
            launchClip = blade ? "sword_rise" : "uppercut";
            airClip = blade ? "sword_leap" : "spin_kick";
            hitSfx = hero.Style == FightStyle.Sword ? Sfx.SwordHit : hero.Style == FightStyle.Pipe ? Sfx.PipeHit : Sfx.PunchHeavy;
            Sound.Play(Sfx.WhooshBig, 0.8f);
        }

        // where the boss is pushed to by the three hits
        float FlurryPush(float x) => 0.25f * Ease(H1, H1 + 0.1f, x) + 0.25f * Ease(H2, H2 + 0.1f, x) + 0.35f * Ease(H3, H3 + 0.1f, x);

        void Pose(out Vector3 cpos, out Vector3 clook, out float cfov)
        {
            var fx = InkFx.Instance;
            var tc = TimeController.Instance;
            var cam = CameraDirector.Instance;
            Vector3 B, Hh;

            // ---------------------------------------------------------------- bodies
            float bxF = bx0 + side * FlurryPush(g);
            float hxF = bxF - side * 1.2f;
            if (g < L)
            {
                B = new Vector3(bxF, y0, 0f);
                Hh = new Vector3(hxF, y0 + (g > Upper ? 0.15f * Ease(Upper, L, g) : 0f), 0f);
            }
            else if (g < K)
            {
                float u = Mathf.Clamp01((g - L) / (Air - L));
                float rise = apex * (1f - (1f - u) * (1f - u)) + 0.15f * Mathf.InverseLerp(Air, K, g);
                B = new Vector3(Mathf.Lerp(bxF, bossApexX, u), y0 + rise, 0f);
                if (g < Jump) Hh = new Vector3(hxF, y0 + 0.15f * (1f - Ease(L, Jump, g)), 0f);
                else
                {
                    if (Once(100)) { heroAirFrom = new Vector3(hxF, y0, 0f); heroAirTo = new Vector3(bossApexX - side * 0.75f, y0 + apex + 0.35f, 0f); }
                    float v = Ease(Jump, Air, g);
                    Hh = Vector3.Lerp(heroAirFrom, heroAirTo, v) + Vector3.up * Mathf.Sin(v * Mathf.PI) * 0.25f
                         + Vector3.up * 0.1f * Mathf.InverseLerp(Air, K, g);
                }
            }
            else
            {
                var bAir = new Vector3(bossApexX, y0 + apex + 0.15f, 0f);
                float u = Mathf.Clamp01((g - K) / (Crash - K));
                B = Vector3.Lerp(bAir, new Vector3(landX, y0, 0f), u * u);          // driven down, accelerating
                if (Once(101)) heroDropFrom = hero.transform.position;
                float v = Mathf.Clamp01((g - K - 0.05f) / (Touch - K - 0.05f));
                var heroLand = new Vector3(landX - side * 1.3f, y0, 0f);
                Hh = g < K + 0.05f ? heroDropFrom : Vector3.Lerp(heroDropFrom, heroLand, v * v);
            }
            Boss.PuppetPose(B);
            hero.CinePose(Hh);

            // ---------------------------------------------------------------- the beats
            if (Once(0)) Strike(0);
            if (g >= H2 - 0.18f && Once(1)) Strike(1);
            if (g >= H3 - 0.28f && Once(2)) Strike(2);
            if (g >= H1 && Once(10)) Impact(0, B);
            if (g >= H2 && Once(11)) Impact(1, B);
            if (g >= H3 && Once(12)) Impact(2, B);
            if (g >= Crouch && Once(20)) hero.body.Play(hero.body.Has("duck") ? "duck" : "idle", 0.05f);
            if (g >= Upper && Once(21)) { hero.body.Play(hero.body.Has(launchClip) ? launchClip : "uppercut", 0.03f, true, 1.3f); Sound.Play(Sfx.HeroEffort, 1f, hero.transform.position); }
            if (g >= L && Once(22))
            {
                // the launch
                Boss.PuppetPlay("knockdown", 0.03f, 0.55f);
                Boss.PuppetFlash();
                if (tc != null) { tc.Clear(); tc.HitStop(10); tc.SlowMo(0.28f, 2.9f, 0.05f, 0.3f); }
                if (fx != null) { fx.ImpactFlash(0.07f); fx.Splash(Boss.Chest, Vector3.up, 50, Boss.InkColor); fx.Burst(Boss.Chest, 1.2f); }
                StreakFx.Slash(Boss.Chest, new Vector3(side * 0.25f, 1f, 0f), 3.2f, 0.07f, 0.35f);
                StreakFx.Sparks(Boss.Chest, Vector3.up, 16, 8f);
                Sound.Play(Sfx.CineMassive, 1f);
                Sound.Play(Sfx.Boom, 0.9f);
                Sound.Play(hitSfx, 1f, Boss.Chest, 0.85f);
                Sound.Play(Sfx.BruteHurt, 1f, Boss.Chest, 0.7f);
                if (cam != null) cam.Shake(0.3f, 0.3f);
            }
            if (g >= Jump && Once(23))
            {
                hero.body.Play("jump", 0.05f, true, 1.1f);
                Sound.Play(Sfx.WhooshBig, 1f, hero.transform.position);
                if (fx != null) fx.Burst(new Vector3(hxF, y0 + 0.05f, 0f), 0.8f);
            }
            if (g >= Air - 0.2f && Once(24))
            {
                hero.body.Play(hero.body.Has(airClip) ? airClip : "flying_kick", 0.05f, true, 1.3f);
                Sound.Play(Sfx.Whoosh, 1f, hero.transform.position, 0.8f);
            }
            if (g >= K && Once(25))
            {
                // the kick: a frozen frame, then down he goes
                Boss.PuppetFlash();
                if (tc != null) { tc.Clear(); tc.HitStop(14); }
                if (fx != null) { fx.ImpactFlash(0.1f); fx.Splash(Boss.Chest, new Vector3(side, -1f, 0f), 60, Boss.InkColor); }
                StreakFx.Slash(Boss.Chest, new Vector3(side, -0.8f, 0f), 3.6f, 0.08f, 0.4f);
                StreakFx.Sparks(Boss.Chest, new Vector3(side, -1f, 0f), 20, 9f);
                Sound.Play(Sfx.CineMassive, 1f);
                Sound.Play(blade ? hitSfx : Sfx.Kick, 1f, Boss.Chest, 0.8f);
                Sound.Play(Sfx.Bone, 1f, Boss.Chest);
                if (cam != null) cam.Shake(0.35f, 0.3f);
            }
            if (g >= Crash && Once(26))
            {
                // into the floor
                var p = new Vector3(landX, y0, 0f);
                if (fx != null)
                {
                    fx.Burst(p + Vector3.up * 0.05f, 2.4f);
                    fx.Splash(p + Vector3.up * 0.2f, Vector3.up, 80, Boss.InkColor);
                    fx.Stain(p, Boss.InkColor, 2.6f, false);
                    fx.StainWall(p.x + Random.Range(-1f, 1f), y0 + Random.Range(0.6f, 1.8f), Boss.InkColor, 1.8f);
                    fx.ImpactFlash(0.05f);
                }
                StreakFx.Ring(p, 4.5f, 0.4f, 0.1f);
                StreakFx.Sparks(p, Vector3.up, 24, 8f);
                if (tc != null) { tc.Clear(); tc.HitStop(6); tc.SlowMo(0.35f, 1.1f, 0.02f, 0.5f); }
                if (cam != null) cam.Shake(0.5f, 0.6f);
                foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1.4f);
                Sound.Play(Sfx.Boom, 1f, p);
                Sound.Play(Sfx.BodyFall, 1f, p, 0.7f);
                Sound.Play(Sfx.BruteDeath, 1f, p);
                Sound.Play(Sfx.MetalClang, 0.7f, p, 0.6f);
            }
            if (g >= Touch && Once(27))
            {
                // the hero lands beside him, crouched
                hero.body.Play(hero.body.Has("duck") ? "duck" : "idle", 0.04f);
                hero.CineTrails(false);
                Sound.Play(Sfx.Land, 1f, hero.transform.position, 0.85f);
                if (fx != null) fx.Burst(hero.transform.position + Vector3.up * 0.05f, 1.0f);
                if (cam != null) cam.Shake(0.12f, 0.2f);
            }
            if (g >= Rise && Once(28))
            {
                hero.body.Play("idle", 0.45f, true, 0.7f);
                Sound.Play(Sfx.Cloth, 0.8f, hero.transform.position);
            }
            if (g >= Rise - 0.05f && Once(29))
            {
                DefeatSub = $"FLOOR {wd.Floor} IS YOURS";
                Sound.Play(Sfx.CineHit, 0.9f);
                if (fx != null) fx.ImpactFlash(0.04f);
            }
            if (g >= Rise - 0.05f) DefeatAlpha = Mathf.MoveTowards(DefeatAlpha, 1f, Time.unscaledDeltaTime * 3f);
            if (g >= End) { FinishFinale(); cpos = lastPos; clook = lastLook; cfov = lastFov; return; }

            // ---------------------------------------------------------------- the shots
            var bc = B + Vector3.up * 1.33f;                   // his chest
            var hc = Hh + Vector3.up * 0.92f;                  // the hero's
            if (g < H2)
            {
                // 1: over the hero's back shoulder, onto the boss
                Shot(new Vector3(hxF - side * 0.9f, y0 + 1.25f, -2.9f) + new Vector3(side * 0.15f, 0f, 0.15f) * Ease(0f, H2, g),
                    new Vector3(bxF - side * 0.2f, y0 + 1.15f, 0f), 34f, out cpos, out clook, out cfov);
            }
            else if (g < H3)
            {
                // 2: the reverse, low from behind the boss, onto the hero's face
                Shot(new Vector3(bxF + side * 1.0f, y0 + 0.55f, -2.5f), new Vector3(hxF + side * 0.3f, y0 + 1.0f, 0f), 32f, out cpos, out clook, out cfov);
            }
            else if (g < L)
            {
                // 3: profile, wide enough for the roundhouse
                float mid = (bxF + hxF) * 0.5f;
                Shot(new Vector3(mid, y0 + 1.0f, -4.6f + 0.5f * Ease(H3, L, g)), new Vector3(mid, y0 + 0.95f, 0f), 38f, out cpos, out clook, out cfov);
            }
            else if (g < Air)
            {
                // 4: from the floor, looking up as he goes up and the hero goes after him
                Shot(new Vector3(hxF - side * 0.8f, y0 + 0.25f, -2.6f), Vector3.Lerp(bc, (bc + hc) * 0.5f, Ease(Jump, Air, g)), 44f, out cpos, out clook, out cfov);
            }
            else if (g < Crash)
            {
                // 5: in the air with them, circling, slow
                var piv = (bc + hc) * 0.5f;
                float ang = Mathf.Lerp(-26f, 16f, Ease(Air, K + 0.1f, g)) * side;
                Shot(piv + Quaternion.Euler(0f, ang, 0f) * new Vector3(0f, -0.25f, -3.6f), piv, 34f, out cpos, out clook, out cfov);
            }
            else if (g < Rise)
            {
                // 6: the crater, wide and low
                Shot(new Vector3(landX - side * 4.2f, y0 + 0.35f, -4.6f), new Vector3(landX - side * 0.6f, y0 + 0.7f, 0f), 40f, out cpos, out clook, out cfov);
            }
            else
            {
                // 7: low beside the body, looking up at the hero standing over him
                float u = Ease(Rise, End, g);
                Shot(new Vector3(landX + side * Mathf.Lerp(0.9f, 0.55f, u), y0 + 0.3f, Mathf.Lerp(-2.9f, -2.4f, u)),
                    new Vector3(Hh.x, y0 + 1.15f, 0f), Mathf.Lerp(34f, 30f, u), out cpos, out clook, out cfov);
            }
            lastPos = cpos; lastLook = clook; lastFov = cfov;
        }

        static void Shot(Vector3 p, Vector3 l, float f, out Vector3 cp, out Vector3 cl, out float cf) { cp = p; cl = l; cf = f; }

        void Strike(int i)
        {
            string clip = flurry[i];
            hero.body.Play(hero.body.Has(clip) ? clip : "jab", 0.03f, true, 1.4f);
            Sound.Play(blade ? Sfx.SwordSwing : Sfx.Whoosh, 0.9f, hero.transform.position);
            if (i == 2) Sound.Play(Sfx.HeroEffort, 0.8f, hero.transform.position);
        }

        void Impact(int i, Vector3 b)
        {
            var chest = b + Vector3.up * 1.33f;
            var fx = InkFx.Instance;
            Boss.PuppetPlay(i % 2 == 0 ? "hit_front" : "hit_back", 0.02f, 0.9f);
            Boss.PuppetFlash();
            if (fx != null) fx.Splash(chest, new Vector3(side, 0.3f, 0f), 26 + i * 8, Boss.InkColor);
            var d = i == 0 ? new Vector3(side, 0.35f, 0f) : i == 1 ? new Vector3(side, -0.45f, 0f) : new Vector3(side, 0.1f, 0f);
            StreakFx.Slash(chest, d, 2.4f + i * 0.4f, 0.05f + i * 0.01f, 0.25f);
            if (blade) StreakFx.Sparks(chest, d, 10, 7f);
            Sound.Play(i == 2 && !blade ? Sfx.Kick : hitSfx, 1f, chest, 0.95f - i * 0.05f);
            if (i == 2) Sound.Play(Sfx.BruteHurt, 0.9f, chest, 0.8f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(4 + i * 2);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.1f + i * 0.05f, 0.15f);
        }

        void FinishFinale()
        {
            Current = Stage.Done;
            hero.CineTrails(false);
            if (Boss != null)
            {
                Boss.PuppetDie();
                wd.OnBossFinished(Boss);
            }
            // the floor-clear victory follows straight on (it takes the hero and the camera)
            if (VictoryCinematic.Instance == null) { hero.CineRelease(); if (CameraDirector.Instance != null) CameraDirector.Instance.Scripted(Vector3.zero, Vector3.zero, 0f); }
            else hero.CineLock();
        }

        /// <summary>Hold the bars and the last shot until the victory has its own.</summary>
        void UpdateDone()
        {
            var vc = VictoryCinematic.Instance;
            bool handed = vc == null || !vc.Active || vc.HeroShotStarted;
            if (!handed && CameraDirector.Instance != null && lastFov > 0f) CameraDirector.Instance.Scripted(lastPos, lastLook, 1f, lastFov);
            if (vc == null || !vc.Active || vc.BarsPixels > Bars - 2f) BarsPixels = Mathf.MoveTowards(BarsPixels, 0f, Time.unscaledDeltaTime * Bars * 2f);
            DefeatAlpha = Mathf.MoveTowards(DefeatAlpha, 0f, Time.unscaledDeltaTime * (handed ? 3f : 0.4f));
            HpAlpha = Mathf.MoveTowards(HpAlpha, 0f, Time.unscaledDeltaTime * 2f);
        }
    }
}
