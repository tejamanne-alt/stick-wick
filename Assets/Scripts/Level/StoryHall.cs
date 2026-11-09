using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>CHAPTER 1, the main hall (docs/story/chapter1_fight.md). After the first memory he
    /// hears the rest of the crew behind the inner shutter, heaves it up, and the hall opens in
    /// front of him: four zones along the lane, each locked until its waves are down, then GO.
    ///
    ///   ZONE 1 THE LOADING BAY    3 brawlers / 2 brawlers + 2 runners
    ///   ZONE 2 THE RACKING AISLE  2 pipers + 2 brawlers / a grabber, a piper, 2 runners
    ///   ZONE 3 THE MEZZANINE      a door-man, 2 pipers, a runner / everything at once
    ///   ZONE 4 THE CRANE BAY      the last of the crew, then THE FOREMAN
    ///
    /// A death restarts at the zone you were in (HallCheckpoint survives the scene reload).</summary>
    public partial class StoryIntro
    {
        /// <summary>The zone to resume at after a defeat (-1: not in the hall yet).</summary>
        public static int HallCheckpoint = -1;

        /// <summary>From the menu: the chapter from the top (no checkpoint, no retry state).</summary>
        public static void NewRun() { HallCheckpoint = -1; Retry = false; EscapeSeen = false; BossReached = false; Enemy.StoryFinale = null; TestEnding = 0; BankFrom = 0; EndingFrom = 0; }

        struct Spawn { public EnemyType type; public SpawnKind kind; public Spawn(EnemyType t, SpawnKind k) { type = t; kind = k; } }

        static Spawn S(EnemyType t, SpawnKind k) => new Spawn(t, k);
        const EnemyType Br = EnemyType.Brawler, Pi = EnemyType.Piper, Ru = EnemyType.Runner, Gr = EnemyType.Grabber, Dm = EnemyType.DoorMan;
        const SpawnKind Rt = SpawnKind.RightDoor, Lt = SpawnKind.LeftDoor, Bg = SpawnKind.Background, Up = SpawnKind.UpperDoor, Ce = SpawnKind.Ceiling;

        static readonly string[] ZoneName = { "THE LOADING BAY", "THE RACKING AISLE", "THE MEZZANINE", "THE CRANE BAY" };
        // per zone: its waves, and how many may be up at once in each
        static readonly Spawn[][][] Waves =
        {
            new[] { new[] { S(Br, Rt), S(Br, Bg), S(Br, Rt) },
                    new[] { S(Br, Rt), S(Ru, Bg), S(Ru, Rt), S(Br, Lt) } },
            new[] { new[] { S(Pi, Rt), S(Br, Bg), S(Pi, Bg), S(Br, Rt) },
                    new[] { S(Gr, Rt), S(Pi, Bg), S(Ru, Up), S(Ru, Rt) } },
            new[] { new[] { S(Dm, Rt), S(Pi, Up), S(Pi, Bg), S(Ru, Rt) },
                    new[] { S(Br, Rt), S(Br, Bg), S(Gr, Rt), S(Dm, Bg), S(Pi, Up), S(Pi, Up) } },
            new[] { new[] { S(Br, Rt), S(Pi, Bg), S(Ru, Rt), S(Gr, Bg) } },
        };
        static readonly int[][] Caps = { new[] { 2, 3 }, new[] { 3, 3 }, new[] { 3, 4 }, new[] { 4 } };

        enum ZoneState { Fighting, Clear }
        int zone, wave, queued;
        ZoneState zoneState;
        float nextSpawnAt, waveDoneAt = -1f, hallT, prevHallT;
        readonly List<Enemy> hallMen = new List<Enemy>();
        Transform innerShutter;
        float innerShutterY;
        bool hallReady;

        float ZoneX0(int z) => WarehousePlan.ZoneX[z];
        float ZoneX1(int z) => WarehousePlan.ZoneX[z + 1];

        // ================================================================ into the hall

        /// <summary>After the memory: voices and boots behind the inner shutter; he goes to it and
        /// heaves it up; the hall opens, and the first of them turn round.</summary>
        void BeginToHall()
        {
            phase = Phase.ToHall;
            hallT = prevHallT = 0f;
            innerShutter = Find("InnerShutter");
            if (innerShutter != null) innerShutterY = innerShutter.position.y;
            hero.CinePuppet();
            hero.SetFacing(1);
            if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f);
            Sound.Tension(true);
        }

        bool AtH(float x) => prevHallT < x && hallT >= x;

        void UpdateToHall()
        {
            float y = home.y;
            var hp = hero.transform.position;
            barsPx = Mathf.MoveTowards(barsPx, 56f, Time.unscaledDeltaTime * 120f);
            if (AtH(0.2f)) { Say("VOICE, BEYOND THE SHUTTER", "Kovac went quiet. Get in there. ALL of you.", 2.4f); Sound.Play(Sfx.Footstep, 0.8f, new Vector3(16f, 0f, 0f), 0.8f); }
            if (AtH(0.6f)) Sound.Play(Sfx.Footstep, 0.9f, new Vector3(16f, 0f, 0f), 0.85f);
            if (AtH(0.9f)) Sound.Play(Sfx.Chain, 0.6f, new Vector3(17f, 1f, 0f));
            if (AtH(1.0f)) Sound.Play(Sfx.Heartbeat, 0.8f);
            if (hallT < 1.8f)
            {
                // he turns his head to the sound: the shutter at the end of the room
                if (cam != null) cam.Scripted(hp + new Vector3(-1.2f, 1.1f, -1.6f), new Vector3(14f, 1.4f, 0.5f), 1f, 38f);
                return;
            }
            if (AtH(1.8f))
            {
                // cut: he is at the shutter
                hero.CinePose(new Vector3(WarehousePlan.StoreHalf - 0.9f, y, 0f));
                hero.SetFacing(1);
                if (hero.body.Has("duck")) hero.body.Play("duck", 0.15f, true, 0.9f);
                Sound.Play(Sfx.Cloth, 0.8f);
            }
            if (AtH(2.25f)) { if (hero.body.Has("idle")) hero.body.Play("idle", 0.2f); Sound.Play(Sfx.HeroEffort, 0.9f, null, 0.9f); Sound.Play(Sfx.Latch, 1f, new Vector3(14f, 1f, 0f), 0.85f); }
            if (innerShutter != null && hallT >= 2.25f)
            {
                float k = Ease(2.25f, 2.75f, hallT);
                innerShutter.position = new Vector3(innerShutter.position.x, innerShutterY + 4.0f * k, innerShutter.position.z);
                if (k >= 0.999f) foreach (var r in innerShutter.GetComponentsInChildren<Renderer>()) r.enabled = false;
            }
            if (AtH(2.3f)) { Sound.Play(Sfx.Door, 1f, new Vector3(14f, 1f, 0f)); Sound.Play(Sfx.Chain, 0.9f, new Vector3(14f, 2f, 0f)); if (cam != null) cam.Shake(0.12f, 0.3f); }
            if (hallT < 3.0f)
            {
                // low behind him as he heaves it up
                if (cam != null) cam.Scripted(new Vector3(hero.transform.position.x - 1.6f, 0.5f, -1.0f), new Vector3(15.5f, 1.6f, 0.3f), 1f, 42f);
                return;
            }
            if (AtH(3.0f))
            {
                // the hall: the first of them turn round
                BeginHall(0, false);
                Lightning(0.6f, 0.7f, 0.4f);
                Sound.Play(Sfx.CineHit, 0.8f);
            }
            // he walks in through the doorway; the first of them come out to meet him (held back
            // until the scene lets go)
            if (AtH(3.2f) && hero.body.Has("walk")) hero.body.Play("walk", 0.2f, true, 1.1f);
            if (AtH(5.5f)) hero.body.Play("idle", 0.25f);
            hero.CinePose(new Vector3(Mathf.Lerp(WarehousePlan.StoreHalf - 0.9f, ZoneX0(0) + 1.4f, Ease(3.2f, 5.5f, hallT)), y, 0f));
            UpdateHall();
            if (cam != null)
            {
                float k = Ease(3.0f, 5.2f, hallT);
                cam.Scripted(Vector3.Lerp(new Vector3(14.6f, 2.4f, -0.4f), new Vector3(15.6f, 1.9f, -1.0f), k), new Vector3(24f, 1.2f, 3.0f), 1f - Ease(5.2f, 5.9f, hallT), 46f);
            }
            if (hallT >= 5.9f)
            {
                foreach (var m0 in hallMen) if (m0 != null) m0.Passive = false;
                if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
                barsPx = 0f;
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                phase = Phase.Hall;
            }
        }

        // ================================================================ the zones

        /// <summary>Start (or resume, after a defeat) at zone z. `cut`: put him at its start and give
        /// control at once (a resume); otherwise the cinematic hands over.</summary>
        void BeginHall(int z, bool cut)
        {
            HallCheckpoint = z;
            zone = z;
            wave = 0;
            queued = 0;
            zoneState = ZoneState.Fighting;
            nextSpawnAt = Time.time + (cut ? 1.2f : 0.3f);
            waveDoneAt = -1f;
            hallMen.Clear();
            Retry = true;
            EscapeSeen = true;
            hallReady = true;
            LockZone(z);
            SetDoors(z);
            if (director != null) director.ShowBanner($"{z + 1}  ·  {ZoneName[z]}", 2.4f);
            if (cut)
            {
                phase = Phase.Hall;
                hero.CinePose(new Vector3(ZoneX0(z) + 1.2f, 0f, 0f));
                hero.SetFacing(1);
                hero.CineRelease();
                hero.runSpeed = runSpeed;
                if (hero.Style == FightStyle.Fists) hero.GiveWeapon(FightStyle.Knife, 3);
                var sh = Find("InnerShutter");
                if (sh != null) foreach (var r in sh.GetComponentsInChildren<Renderer>()) r.enabled = false;
                Sound.Tension(true);
                Sound.Ambience(0.6f, 0.4f, 1200f);
                barsPx = 0f;
                if (z == Waves.Length - 1 && BossReached) BossArrives();     // a defeat by the Foreman: back to him, not the wave before
            }
        }

        /// <summary>Walls for him, his enemies and the camera: this zone only.</summary>
        void LockZone(int z) => SetArena(ZoneX0(z) + 0.6f, ZoneX1(z) - 0.6f);

        void SetArena(float x0, float x1)
        {
            hero.arenaMinX = x0; hero.arenaMaxX = x1;
            foreach (var e in Enemy.All) if (e != null) { e.arenaMinX = x0; e.arenaMaxX = x1; }
            if (cam != null) cam.SetBounds(x0 - 0.9f, x1 + 0.9f);
        }

        /// <summary>Where this zone's men come from: the far end, the back of the hall (stepping
        /// out of the dark between the racks), a rack top or the office door, from above.</summary>
        void SetDoors(int z)
        {
            if (director == null) return;
            float x0 = ZoneX0(z), x1 = ZoneX1(z), mid = (x0 + x1) * 0.5f;
            var pts = director.spawnPoints;
            void At(SpawnKind k, Vector3 p) { var t = pts[(int)k]; if (t != null) t.position = p; }
            At(SpawnKind.RightDoor, new Vector3(x1 - 0.7f, 0f, 0f));
            At(SpawnKind.LeftDoor, new Vector3(x0 + 0.7f, 0f, 0f));
            At(SpawnKind.Background, new Vector3(mid + 1.5f, 0f, 3.5f));
            At(SpawnKind.UpperDoor, z == 1 ? new Vector3(WarehousePlan.Rack2X0 + 1.5f, WarehousePlan.RackY, 1.0f)
                                  : z == 2 ? new Vector3(WarehousePlan.MezzX1 - 2.6f, WarehousePlan.MezzY, 2.0f)
                                  : new Vector3(x1 - 1.0f, 0f, 2.5f));
            At(SpawnKind.Ceiling, new Vector3(mid, 6.0f, 0f));
        }

        void UpdateHall()
        {
            if (!hallReady || hero.IsDead) return;
            if (bossT >= 0f) { UpdateBoss(); return; }                // the Foreman's floor: no more waves
            hallMen.RemoveAll(e => e == null || !e.Alive);
            var waves = Waves[zone];
            if (zoneState == ZoneState.Fighting)
            {
                var list = waves[wave];
                int cap = Caps[zone][wave];
                if (queued < list.Length && hallMen.Count < cap && Time.time >= nextSpawnAt && director != null)
                {
                    var sp = list[queued];
                    var e = director.SpawnScripted(sp.type, sp.kind);
                    if (e != null) { hallMen.Add(e); if (phase == Phase.ToHall) e.Passive = true; }
                    queued++;
                    nextSpawnAt = Time.time + Random.Range(0.6f, 1.3f);
                }
                if (queued >= list.Length && hallMen.Count == 0)
                {
                    if (waveDoneAt < 0f) waveDoneAt = Time.time;
                    if (Time.time - waveDoneAt > 1.2f)
                    {
                        waveDoneAt = -1f;
                        if (wave + 1 < waves.Length)
                        {
                            wave++;
                            queued = 0;
                            nextSpawnAt = Time.time + 0.8f;
                            if (director != null) director.ShowBanner("MORE OF THEM", 1.4f);
                            Sound.Play(Sfx.Door, 0.8f, new Vector3(ZoneX1(zone) - 1f, 0f, 0f));
                        }
                        else ZoneCleared();
                    }
                }
            }
            else if (zoneState == ZoneState.Clear && zone + 1 < Waves.Length)
            {
                // into the next zone: once he is well in, it closes behind him
                if (hero.transform.position.x > ZoneX0(zone + 1) + 1.5f) BeginHall(zone + 1, false);
            }
        }

        void ZoneCleared()
        {
            if (zone + 1 >= Waves.Length)
            {
                // the crane bay is empty of his men: the Foreman (built next), for now the chapter ends
                BossArrives();
                return;
            }
            zoneState = ZoneState.Clear;
            SetArena(ZoneX0(zone) + 0.6f, ZoneX1(zone + 1) - 0.6f);
            if (cam != null) cam.SetBounds(ZoneX0(zone) - 0.3f, ZoneX1(zone + 1) + 0.3f);
            if (director != null) director.ShowBanner("CLEAR", 1.4f);
            Sound.Play(Sfx.WaveClear, 0.7f);
            goUntil = Time.unscaledTime + 30f;
        }

        float goUntil;

        /// <summary>GO, at the right edge, until he moves on into the next zone.</summary>
        void HallGUI(float w, float h)
        {
            if (phase != Phase.Hall || zoneState != ZoneState.Clear || Time.unscaledTime > goUntil) return;
            float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
            prompt.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(w - 260, h * 0.42f, 160, 40), Spaced("GO"), prompt);
            for (int i = 0; i < 3; i++)
            {
                float k = Mathf.Repeat(Time.unscaledTime * 1.4f - i * 0.25f, 1f);
                Chevron(new Vector2(w - 120 + i * 26, h * 0.42f + 20), new Color(1f, 1f, 1f, a * (1f - k)));
            }
        }

    }
}
