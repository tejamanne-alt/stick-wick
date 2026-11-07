using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>Runs the fight: endless floors of five waves (a brute boss on every fifth), enemy
    /// mix and colours, weapon drops, the live style rank (D to SSS), the Flow meter that powers
    /// Rampage, the last-kill slow-mo camera, environment kills, score and saved bests.</summary>
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        public HeroController hero;
        public GameObject enemyModel;
        public MoveSet moves;
        public Material enemyMaterial;
        public Material trailMaterial;
        public Material weaponMaterial;
        public Material eyeMaterial;
        public Transform[] spawnPoints = new Transform[5]; // indexed by SpawnKind
        [Tooltip("Story mode: no waves, floors or score; the story spawns its own enemies.")]
        public bool scripted;

        public const int WavesPerFloor = 5;
        const float SpawnWarning = 0.6f;

        static readonly Color[] BrawlerColors =
        {
            new Color(0.10f, 0.55f, 0.55f), new Color(0.45f, 0.52f, 0.12f), new Color(0.82f, 0.55f, 0.10f),
            new Color(0.48f, 0.22f, 0.62f), new Color(0.72f, 0.32f, 0.14f), new Color(0.14f, 0.46f, 0.26f),
        };
        static readonly Color SwordColor = new Color(0.72f, 0.07f, 0.16f);
        static readonly Color ThrowerColor = new Color(0.42f, 0.46f, 0.20f);
        static readonly Color PiperColor = new Color(0.62f, 0.38f, 0.12f);
        static readonly string[] ChapterTitles = { "THE LOBBY", "KNIVES OUT", "PIPE DREAMS", "COLD STEEL", "LEAD RAIN" };
        static readonly Color GunColor = new Color(0.18f, 0.34f, 0.72f);
        static readonly Color BruteColor = new Color(0.40f, 0.04f, 0.07f);
        static readonly Color RunnerColor = new Color(0.86f, 0.74f, 0.22f);     // quick and yellow
        static readonly Color GrabberColor = new Color(0.28f, 0.36f, 0.16f);    // big, dull olive
        static readonly Color DoorManColor = new Color(0.30f, 0.42f, 0.50f);    // slate, like his sheet of iron

        public enum Phase { Intro, Fighting, BetweenWaves, FloorClear, Dead }
        public Phase CurrentPhase { get; private set; } = Phase.Intro;
        public int Floor { get; private set; } = 1;
        public int WaveIndex { get; private set; }
        public int WaveCount => WavesPerFloor;
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public float DamageTaken { get; private set; }
        public float ElapsedTime { get; private set; }
        float floorStartTime;
        int floorStartKills;
        /// <summary>This floor alone: its kills and its time (for the victory tally).</summary>
        public int FloorKills => Kills - floorStartKills;
        public float FloorSeconds => ElapsedTime - floorStartTime;
        public string Banner { get; private set; }
        public float BannerUntil { get; private set; }
        public string StyleText { get; private set; }
        public float StyleUntil { get; private set; }

        // style rank: points decay; variety and environment kills push it up, getting hit knocks it down
        static readonly string[] Ranks = { "D", "C", "B", "A", "S", "SS", "SSS" };
        static readonly float[] RankAt = { 0f, 150f, 350f, 600f, 900f, 1300f, 1800f };
        public float StylePoints { get; private set; }
        public int RankIndex { get; private set; }
        public string Rank => Ranks[RankIndex];
        public float RankProgress => RankIndex >= Ranks.Length - 1 ? 1f : Mathf.InverseLerp(RankAt[RankIndex], RankAt[RankIndex + 1], StylePoints);
        public int BestRankIndex { get; private set; }
        readonly Queue<string> recentMoves = new Queue<string>();

        // flow: fills with action, spent on Rampage
        public float Flow { get; private set; }
        public bool FlowReady => Flow >= 100f;

        public int BestScore { get; private set; }
        public int BestFloor { get; private set; }
        public bool NewBest { get; private set; }

        public struct Incoming { public SpawnKind kind; public EnemyType type; public float time; }
        readonly List<Incoming> incoming = new List<Incoming>();
        public IReadOnlyList<Incoming> Upcoming => incoming;

        float phaseTime;
        int spawnSerial;
        readonly List<Transform> framed = new List<Transform>();

        void Awake()
        {
            Instance = this;
            AttackTokens.Clear();
            AttackTokens.Hero = hero;
            Enemy.Corpses.Clear();
            BestScore = PlayerPrefs.GetInt("best_score", 0);
            BestFloor = PlayerPrefs.GetInt("best_floor", 0);
            Sound.ResetAmbience();
            if (!scripted)
            {
                if (GetComponent<OpeningCinematic>() == null) gameObject.AddComponent<OpeningCinematic>();
                if (GetComponent<VictoryCinematic>() == null) gameObject.AddComponent<VictoryCinematic>();
                if (GetComponent<TutorialDirector>() == null) gameObject.AddComponent<TutorialDirector>();
                if (GetComponent<BossDirector>() == null) gameObject.AddComponent<BossDirector>();
            }
            if (GetComponent<HintDirector>() == null) gameObject.AddComponent<HintDirector>();
        }

        static bool CinematicHolding => OpeningCinematic.Instance != null && !OpeningCinematic.Instance.ControlReleased;
        static bool HadCinematic => OpeningCinematic.Instance != null;

        void Start()
        {
            if (!HadCinematic && !scripted) ShowBanner("CHAPTER 1", 1.6f);   // the opening has its own chapter card
            phaseTime = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            phaseTime += dt;
            if (CurrentPhase == Phase.Fighting || CurrentPhase == Phase.BetweenWaves) ElapsedTime += dt;

            // style decays (in real time, so slow-mo does not freeze it)
            StylePoints = Mathf.Max(0f, StylePoints - 22f * Time.unscaledDeltaTime * (1f + RankIndex * 0.25f));
            UpdateRank();

            var input = GestureInput.Instance;
            if (input != null && input.Peek() == Command.Restart) { input.Next(); Restart(); return; }

            if (scripted)
            {
                // the story runs the scene; this only keeps score of the fighting and handles death
                if (hero != null && !hero.CinematicLock) ElapsedTime += dt;
                if (hero != null && hero.IsDead && CurrentPhase != Phase.Dead) EnterDead();
                if (CurrentPhase == Phase.Dead && phaseTime > 1.5f && input != null && input.AnyPressThisFrame) Restart();
                UpdateFraming();
                return;
            }

            switch (CurrentPhase)
            {
                case Phase.Intro:
                    if (Floor == 1 && CinematicHolding) { phaseTime = 0f; break; }
                    if (TutorialDirector.Instance != null && TutorialDirector.Instance.Holding) { phaseTime = 0f; break; }
                    if (phaseTime > (Floor == 1 && HadCinematic ? 0.2f : 1.4f)) StartWave(0);
                    break;
                case Phase.Fighting:
                    SpawnDue();
                    if (hero != null && hero.IsDead) { EnterDead(); }
                    else if (incoming.Count == 0 && AliveCount() == 0 && !BossHolding)
                    {
                        if (WaveIndex + 1 < WavesPerFloor) { CurrentPhase = Phase.BetweenWaves; phaseTime = 0f; }
                        else
                        {
                            CurrentPhase = Phase.FloorClear;
                            phaseTime = 0f;
                            Sound.Tension(false);
                            AddScore(1000 * Floor, null);
                            var vc = VictoryCinematic.Instance;
                            if (vc != null) vc.Begin(hero, Floor, Grade(), Score, Kills, BossDirector.Instance != null && BossDirector.Instance.Current == BossDirector.Stage.Done);
                            else ShowBanner($"FLOOR {Floor} CLEAR", 2.4f);
                        }
                    }
                    break;
                case Phase.BetweenWaves:
                    if (hero != null && hero.IsDead) { EnterDead(); break; }
                    if (phaseTime > 1.4f) StartWave(WaveIndex + 1);
                    break;
                case Phase.FloorClear:
                    // the victory cinematic plays out first (with a safety timeout)
                    if ((VictoryCinematic.Instance == null || VictoryCinematic.Instance.Done) && phaseTime > 1f || phaseTime > 16f) NextFloor();
                    break;
                case Phase.Dead:
                    if (phaseTime > 1.5f && input != null && input.AnyPressThisFrame) Restart();
                    break;
            }
            UpdateFraming();
        }

        // ------------------------------------------------------------------ waves

        /// <summary>Each floor is a chapter; each wave brings in the next kind of armed opponent,
        /// whose weapon the hero takes and uses from then on: brawlers (fists), knife throwers,
        /// pipers, swordsmen, gunners and the brute.</summary>
        void StartWave(int index)
        {
            WaveIndex = index;
            CurrentPhase = Phase.Fighting;
            phaseTime = 0f;
            bool boss = index == WavesPerFloor - 1;
            ShowBanner(index == 0 ? (Floor == 1 && HadCinematic ? "FIGHT!" : $"CHAPTER {Floor}: THE LOBBY") : ChapterTitles[Mathf.Clamp(index, 0, ChapterTitles.Length - 1)], 1.6f);

            int count = Mathf.Min(3 + index + Floor, 11);
            var kinds = new[] { SpawnKind.LeftDoor, SpawnKind.RightDoor, SpawnKind.UpperDoor, SpawnKind.Background, SpawnKind.Ceiling };
            float t = Time.time + 0.6f;
            // the wave's new opponent comes first, so its weapon is the first one on the floor
            EnemyType featured = index switch
            {
                1 => EnemyType.Thrower,
                2 => EnemyType.Piper,
                3 => EnemyType.Swordsman,
                4 => EnemyType.Gunner,
                _ => EnemyType.Brawler,
            };
            if (index > 0)
            {
                incoming.Add(new Incoming { kind = kinds[Random.Range(0, 3)], type = featured, time = t });
                t += 0.8f;
                count--;
            }
            if (boss)
            {
                // the brute is not in the queue: he walks in once his men have had their go (BossDirector)
                if (BossDirector.Instance != null) BossDirector.Instance.Arm();
                else incoming.Add(new Incoming { kind = SpawnKind.RightDoor, type = EnemyType.Brute, time = t + 2.5f });
                count -= 1;
            }
            for (int i = 0; i < count; i++)
            {
                float r = Random.value;
                EnemyType type = EnemyType.Brawler;
                // earlier armed types keep appearing; later floors mix in more of everything
                float armed = Mathf.Clamp01(0.15f * index + 0.08f * (Floor - 1));
                if (index > 0 && r < armed)
                {
                    int pick = Random.Range(1, index + 1);
                    type = pick switch { 1 => EnemyType.Thrower, 2 => EnemyType.Piper, 3 => EnemyType.Swordsman, _ => EnemyType.Gunner };
                }
                var kind = kinds[Random.Range(0, index == 0 ? 3 : kinds.Length)];
                incoming.Add(new Incoming { kind = kind, type = type, time = t });
                t += Random.Range(0.35f, 0.9f) / (1f + 0.1f * Floor);
            }
            incoming.Sort((a, b) => a.time.CompareTo(b.time));
            WaveSize = incoming.Count;
            WaveKills = 0;
        }

        /// <summary>How many came in this wave, and how many of them are down (the boss's cue).</summary>
        public int WaveSize { get; private set; }
        public int WaveKills { get; private set; }
        static bool BossHolding => BossDirector.Instance != null && BossDirector.Instance.Holding;

        /// <summary>No more reinforcements (the boss has arrived).</summary>
        public void CancelIncoming() { incoming.Clear(); }

        /// <summary>The boss walks in through a ground-floor door.</summary>
        public Enemy SpawnBoss(SpawnKind door)
        {
            return Spawn(new Incoming { kind = door, type = EnemyType.Brute, time = Time.time });
        }

        public void Announce(string text, float seconds) { ShowBanner(text, seconds); }

        /// <summary>Story mode: bring one in through a door now.</summary>
        public Enemy SpawnScripted(EnemyType type, SpawnKind kind)
        {
            return Spawn(new Incoming { kind = kind, type = type, time = Time.time });
        }

        /// <summary>The finale is over: the boss is down.</summary>
        public void OnBossFinished(Enemy e)
        {
            Kills++;
            WaveKills++;
            AddScore(1500, "BRUTE DEFEATED");
            AddStylePoints(400f, "boss finale");
        }

        /// <summary>How many may be in the fight at once: never more than five, so every fight
        /// reads as a scene rather than a crowd (the first floor starts at three).</summary>
        public int ActiveCap => Floor == 1 ? Mathf.Min(3 + WaveIndex, 5) : 5;
        float lastSpawnAt = -10f;

        /// <summary>Those still in the fight (runners and stunt doubles do not count).</summary>
        public int ActiveCount()
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e.Alive && !e.Fleeing && !e.Passive) n++;
            return n;
        }

        /// <summary>Let the wave in as the room has space: a wave is a queue at the doors, not a rush.
        /// Only as many as there are free places get their door warning; the rest wait outside. If the
        /// room gets quiet (under three) the next one comes straight in. Arrivals are spaced out.</summary>
        void SpawnDue()
        {
            float now = Time.time;
            int active = ActiveCount();
            int room = ActiveCap - active;
            for (int i = 0; i < incoming.Count; i++)
            {
                var inc = incoming[i];
                if (room <= 0)
                {
                    if (inc.time < now + 0.05f) { inc.time = now + 0.05f; incoming[i] = inc; }   // waiting outside, no warning yet
                    continue;
                }
                room--;
                if (active < 3 && inc.time > now) { inc.time = now; incoming[i] = inc; }          // the room is quiet: in he comes
                if (now >= inc.time + SpawnWarning && now - lastSpawnAt > 0.45f)
                {
                    Spawn(inc);
                    incoming.RemoveAt(i);
                    i--;
                    lastSpawnAt = now;
                    active++;
                }
            }
        }

        Enemy Spawn(Incoming inc)
        {
            var point = spawnPoints[(int)inc.kind];
            if (point == null || enemyModel == null) return null;
            Vector3 pos = point.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
            if (inc.kind == SpawnKind.Ceiling && hero != null)
                pos.x = Mathf.Clamp(hero.transform.position.x + Random.Range(-2.5f, 2.5f), hero.arenaMinX + 1.5f, hero.arenaMaxX - 1.5f);
            var weapon = inc.type switch
            {
                EnemyType.Swordsman => FightStyle.Sword,
                EnemyType.Gunner => FightStyle.Gun,
                EnemyType.Thrower => FightStyle.Knife,
                EnemyType.Piper => FightStyle.Pipe,
                _ => FightStyle.Fists,
            };
            var go = FighterFactory.Build(enemyModel, moves, enemyMaterial, $"Enemy_{spawnSerial++}", false, trailMaterial, weapon, eyeMaterial);
            go.transform.position = pos;
            var e = go.GetComponent<Enemy>();
            if (hero != null) { e.arenaMinX = hero.arenaMinX; e.arenaMaxX = hero.arenaMaxX; }      // the same walls as him (a zone of a long building)
            float scale = 1f + 0.12f * (Floor - 1);
            float hp = inc.type switch
            {
                EnemyType.Brute => 300f * scale,
                EnemyType.Swordsman => 40f * scale,
                EnemyType.Gunner => 25f * scale,
                EnemyType.Thrower => 25f * scale,
                EnemyType.Piper => 38f * scale,
                EnemyType.Runner => 15f * scale,
                EnemyType.Grabber => 55f * scale,
                EnemyType.DoorMan => 42f * scale,
                _ => 30f * scale,
            };
            Color c = inc.type switch
            {
                EnemyType.Brute => BruteColor,
                EnemyType.Swordsman => SwordColor,
                EnemyType.Gunner => GunColor,
                EnemyType.Thrower => ThrowerColor,
                EnemyType.Piper => PiperColor,
                EnemyType.Runner => RunnerColor,
                EnemyType.Grabber => GrabberColor,
                EnemyType.DoorMan => DoorManColor,
                _ => BrawlerColors[Random.Range(0, BrawlerColors.Length)],
            };
            e.Init(hero, inc.kind, inc.type, hp, c, trailMaterial);
            if (inc.type == EnemyType.Brute && BossDirector.Instance == null) BossIntro(e);
            return e;
        }

        int AliveCount()
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e.Alive) n++;
            return n;
        }

        /// <summary>The boss walks in: the camera closes in on it in slow motion, bars, name card.</summary>
        void BossIntro(Enemy e)
        {
            ShowBanner("THE BRUTE", 2.2f);
            Sound.Play(Sfx.CineMassive);
            Sound.PlayLater(Sfx.BruteLaugh, 0.5f, 1f, e.transform.position);
            Sound.Tension(true);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 1.6f, 0.1f, 0.4f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Focus(e.transform, 1.8f);
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.05f);
        }

        void NextFloor()
        {
            Floor++;
            floorStartTime = ElapsedTime;
            floorStartKills = Kills;
            if (BossDirector.Instance != null) BossDirector.Instance.ResetFloor();
            foreach (var c in Enemy.Corpses) if (c != null) Destroy(c);
            Enemy.Corpses.Clear();
            foreach (var p in WeaponPickup.All.ToArray()) Destroy(p.gameObject);
            if (InkFx.Instance != null) InkFx.Instance.ClearStains();
            Crate.ResetAll();
            GlassPanel.ResetAll();
            ShowBanner($"CHAPTER {Floor}", 1.6f);
            CurrentPhase = Phase.Intro;
            phaseTime = 0f;
        }

        void EnterDead()
        {
            CurrentPhase = Phase.Dead;
            phaseTime = 0f;
            Sound.Tension(false);
            Sound.PlayLater(Sfx.Defeat, 0.9f);
            ShowBanner("K.O.", 99f);
            if (Score > BestScore) { BestScore = Score; NewBest = true; PlayerPrefs.SetInt("best_score", Score); }
            if (!scripted && Floor > BestFloor) { BestFloor = Floor; PlayerPrefs.SetInt("best_floor", Floor); }
            PlayerPrefs.Save();
        }

        void UpdateFraming()
        {
            var cam = CameraDirector.Instance;
            if (cam == null || hero == null) return;
            framed.Clear();
            // everyone still in the fight: the camera keeps as many in shot as it can, nearest first
            foreach (var e in Enemy.All)
                if (e.Alive && !e.Fleeing) framed.Add(e.transform);
            cam.SetFramed(framed);
        }

        // ------------------------------------------------------------------ scoring and style

        void UpdateRank()
        {
            int r = 0;
            for (int i = 0; i < RankAt.Length; i++) if (StylePoints >= RankAt[i]) r = i;
            if (r > RankIndex && r >= 4) { ShowStyle(Ranks[r] + "!"); Sound.Play(Sfx.RankUp, 1f, null, 0.9f + 0.08f * (r - 4)); }
            RankIndex = r;
            BestRankIndex = Mathf.Max(BestRankIndex, r);
        }

        void AddStylePoints(float pts, string move)
        {
            // variety: a move not used among the last four counts for more
            float mult = recentMoves.Contains(move) ? 0.6f : 1.4f;
            recentMoves.Enqueue(move);
            while (recentMoves.Count > 4) recentMoves.Dequeue();
            StylePoints = Mathf.Min(2200f, StylePoints + pts * mult);
        }

        void AddScore(int points, string label = null)
        {
            Score += points * (1 + RankIndex);
            if (label != null) ShowStyle(label);
        }

        // Rampage is rare: about once or twice a floor. Big moves fill it more than plain hits,
        // and taking damage builds it too, so it comes to you when you are struggling.
        void AddFlow(float amount) { Flow = Mathf.Clamp(Flow + amount, 0f, 100f); }

        public void SpendFlow() { Flow = 0f; }
        public void FillFlow() { Flow = 100f; }

        /// <summary>A training stunt double: a pale grey brawler who takes hits and never attacks.</summary>
        public Enemy SpawnDummy(Vector3 pos)
        {
            if (enemyModel == null) return null;
            var go = FighterFactory.Build(enemyModel, moves, enemyMaterial, $"StuntDouble_{spawnSerial++}", false, trailMaterial, FightStyle.Fists, eyeMaterial);
            go.transform.position = pos;
            var e = go.GetComponent<Enemy>();
            e.Passive = true;
            e.Init(hero, SpawnKind.RightDoor, EnemyType.Brawler, 400f, new Color(0.62f, 0.62f, 0.64f), trailMaterial);
            return e;
        }

        /// <summary>Called by the hero for every strike that lands.</summary>
        public void OnHeroHit(Enemy e, int combo, bool heavy, bool killing, string move = "hit")
        {
            AddScore(heavy ? 25 : 10);
            AddStylePoints(heavy ? 45f : 25f, move);
            AddFlow(killing ? 2.5f : 0.5f);
            if (killing) RegisterKill(e, 100);
        }

        /// <summary>Kills by thrown weapons and other specials.</summary>
        public void OnHeroKill(Enemy e, string label, int points, bool special)
        {
            AddScore(points, label);
            AddStylePoints(special ? 160f : 60f, label);
            AddFlow(4f);
            RegisterKill(e, 0);
        }

        /// <summary>Wall splats and falls from the upper floor.</summary>
        public void OnEnvironmentKill(Enemy e, string label, int points)
        {
            AddScore(points, label);
            AddStylePoints(140f, label);
            AddFlow(3.5f);
            if (!e.Alive) RegisterKill(e, 0);
        }

        void RegisterKill(Enemy e, int bonus)
        {
            if (e != null && (e.Boss || e.Passive)) return;   // the boss's kill is the finale's; stunt doubles don't count
            Kills++;
            WaveKills++;
            if (bonus > 0) AddScore(bonus);
            if (e != null && e.Fleeing) AddScore(150, "COWARD!");
            bool lastOfWave = incoming.Count == 0 && AliveCount() == 0 && !BossHolding; // the victim is already down
            if (lastOfWave)
            {
                // last-kill cam: the signature cinematic moment of every wave
                bool big = WaveIndex == WavesPerFloor - 1;
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.18f, big ? 1.6f : 1.1f, 0.03f, 0.35f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.KillCam(e.Chest, big ? 1.6f : 1.1f, big ? 160f : 100f);
                if (scripted) { Sound.Play(Sfx.CineHit, 0.6f); return; }      // the story frames its own ending
                AddScore(big ? 500 : 200, big ? "FINISH!" : "WAVE CLEAR");
                Sound.Play(big ? Sfx.CineMassive : Sfx.CineHit, big ? 1f : 0.6f);
                if (!big) Sound.PlayLater(Sfx.WaveClear, 0.7f);
            }
        }

        public void OnEnemyDied(Enemy e)
        {
            // armed enemies drop their weapons for the hero to take
            if (e.Type == EnemyType.Swordsman) WeaponPickup.Spawn(FightStyle.Sword, 10, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Gunner) WeaponPickup.Spawn(FightStyle.Gun, 8, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Thrower) WeaponPickup.Spawn(FightStyle.Knife, 3, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Piper) WeaponPickup.Spawn(FightStyle.Pipe, 14, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Brute) AddScore(800);
        }

        public void OnHeroDamaged(float amount)
        {
            DamageTaken += amount;
            StylePoints *= 0.35f;
            AddFlow(amount * 0.45f);                 // pain feeds the rage
        }

        public void OnPerfectDodge(string label)
        {
            AddScore(150, label);
            AddStylePoints(120f, label);
            AddFlow(4f);
        }

        public void AddStyle(int points, string label)
        {
            if (points > 0) AddScore(points);
            ShowStyle(label);
        }

        void ShowStyle(string label)
        {
            StyleText = label;
            StyleUntil = Time.unscaledTime + 1.2f;
        }

        public void ShowBanner(string text, float seconds)
        {
            Banner = text;
            BannerUntil = Time.unscaledTime + seconds;
        }

        public string Grade() => Ranks[BestRankIndex];

        public void Restart()
        {
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            Time.timeScale = 1f;
            AttackTokens.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
