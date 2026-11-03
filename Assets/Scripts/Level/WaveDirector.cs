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
        public Transform[] spawnPoints = new Transform[5]; // indexed by SpawnKind

        public const int WavesPerFloor = 5;
        const float SpawnWarning = 0.6f;

        static readonly Color[] BrawlerColors =
        {
            new Color(0.10f, 0.55f, 0.55f), new Color(0.45f, 0.52f, 0.12f), new Color(0.82f, 0.55f, 0.10f),
            new Color(0.48f, 0.22f, 0.62f), new Color(0.72f, 0.32f, 0.14f), new Color(0.14f, 0.46f, 0.26f),
        };
        static readonly Color SwordColor = new Color(0.72f, 0.07f, 0.16f);
        static readonly Color GunColor = new Color(0.18f, 0.34f, 0.72f);
        static readonly Color BruteColor = new Color(0.40f, 0.04f, 0.07f);

        public enum Phase { Intro, Fighting, BetweenWaves, FloorClear, Dead }
        public Phase CurrentPhase { get; private set; } = Phase.Intro;
        public int Floor { get; private set; } = 1;
        public int WaveIndex { get; private set; }
        public int WaveCount => WavesPerFloor;
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public float DamageTaken { get; private set; }
        public float ElapsedTime { get; private set; }
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
            Enemy.Corpses.Clear();
            BestScore = PlayerPrefs.GetInt("best_score", 0);
            BestFloor = PlayerPrefs.GetInt("best_floor", 0);
        }

        void Start()
        {
            ShowBanner("FLOOR 1", 1.6f);
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

            switch (CurrentPhase)
            {
                case Phase.Intro:
                    if (phaseTime > 1.4f) StartWave(0);
                    break;
                case Phase.Fighting:
                    SpawnDue();
                    if (hero != null && hero.IsDead) { EnterDead(); }
                    else if (incoming.Count == 0 && AliveCount() == 0)
                    {
                        if (WaveIndex + 1 < WavesPerFloor) { CurrentPhase = Phase.BetweenWaves; phaseTime = 0f; }
                        else
                        {
                            CurrentPhase = Phase.FloorClear;
                            phaseTime = 0f;
                            ShowBanner($"FLOOR {Floor} CLEAR", 2.4f);
                            AddScore(1000 * Floor, "FLOOR BONUS");
                        }
                    }
                    break;
                case Phase.BetweenWaves:
                    if (hero != null && hero.IsDead) { EnterDead(); break; }
                    if (phaseTime > 1.4f) StartWave(WaveIndex + 1);
                    break;
                case Phase.FloorClear:
                    if (phaseTime > 2.8f) NextFloor();
                    break;
                case Phase.Dead:
                    if (phaseTime > 1.5f && input != null && input.AnyPressThisFrame) Restart();
                    break;
            }
            UpdateFraming();
        }

        // ------------------------------------------------------------------ waves

        void StartWave(int index)
        {
            WaveIndex = index;
            CurrentPhase = Phase.Fighting;
            phaseTime = 0f;
            bool boss = index == WavesPerFloor - 1;
            ShowBanner(boss ? "BOSS WAVE" : $"WAVE {index + 1}", 1.2f);

            // more enemies, more weapons, more gunners as the floors climb
            int count = Mathf.Min(3 + index + Floor, 11);
            var kinds = new[] { SpawnKind.LeftDoor, SpawnKind.RightDoor, SpawnKind.UpperDoor, SpawnKind.Background, SpawnKind.Ceiling };
            float t = Time.time + 0.5f;
            if (boss)
            {
                incoming.Add(new Incoming { kind = SpawnKind.RightDoor, type = EnemyType.Brute, time = t });
                t += 1.2f;
                count -= 2;
            }
            for (int i = 0; i < count; i++)
            {
                float r = Random.value;
                float gunP = index >= 1 ? 0.12f + 0.04f * Floor : 0f;
                float swordP = index >= 1 ? 0.22f + 0.04f * Floor : 0.1f;
                var type = r < gunP ? EnemyType.Gunner : r < gunP + swordP ? EnemyType.Swordsman : EnemyType.Brawler;
                var kind = kinds[Random.Range(0, index == 0 ? 3 : kinds.Length)];
                incoming.Add(new Incoming { kind = kind, type = type, time = t });
                t += Random.Range(0.35f, 0.9f) / (1f + 0.1f * Floor);
            }
        }

        void SpawnDue()
        {
            for (int i = incoming.Count - 1; i >= 0; i--)
            {
                if (Time.time < incoming[i].time + SpawnWarning) continue;
                Spawn(incoming[i]);
                incoming.RemoveAt(i);
            }
        }

        void Spawn(Incoming inc)
        {
            var point = spawnPoints[(int)inc.kind];
            if (point == null || enemyModel == null) return;
            Vector3 pos = point.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
            if (inc.kind == SpawnKind.Ceiling && hero != null)
                pos.x = Mathf.Clamp(hero.transform.position.x + Random.Range(-2.5f, 2.5f), -11f, 11f);
            bool sword = inc.type == EnemyType.Swordsman;
            var go = FighterFactory.Build(enemyModel, moves, enemyMaterial, $"Enemy_{spawnSerial++}", false, trailMaterial, sword,
                                          inc.type == EnemyType.Gunner);
            go.transform.position = pos;
            var e = go.GetComponent<Enemy>();
            float scale = 1f + 0.12f * (Floor - 1);
            float hp = inc.type switch
            {
                EnemyType.Brute => 220f * scale,
                EnemyType.Swordsman => 40f * scale,
                EnemyType.Gunner => 25f * scale,
                _ => 30f * scale,
            };
            Color c = inc.type switch
            {
                EnemyType.Brute => BruteColor,
                EnemyType.Swordsman => SwordColor,
                EnemyType.Gunner => GunColor,
                _ => BrawlerColors[Random.Range(0, BrawlerColors.Length)],
            };
            e.Init(hero, inc.kind, inc.type, hp, c, trailMaterial);
            if (inc.type == EnemyType.Brute) ShowBanner("THE BRUTE", 1.4f);
        }

        int AliveCount()
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e.Alive) n++;
            return n;
        }

        void NextFloor()
        {
            Floor++;
            foreach (var c in Enemy.Corpses) if (c != null) Destroy(c);
            Enemy.Corpses.Clear();
            foreach (var p in WeaponPickup.All.ToArray()) Destroy(p.gameObject);
            if (InkFx.Instance != null) InkFx.Instance.ClearStains();
            ShowBanner($"FLOOR {Floor}", 1.6f);
            CurrentPhase = Phase.Intro;
            phaseTime = 0f;
        }

        void EnterDead()
        {
            CurrentPhase = Phase.Dead;
            phaseTime = 0f;
            ShowBanner("K.O.", 99f);
            if (Score > BestScore) { BestScore = Score; NewBest = true; PlayerPrefs.SetInt("best_score", Score); }
            if (Floor > BestFloor) { BestFloor = Floor; PlayerPrefs.SetInt("best_floor", Floor); }
            PlayerPrefs.Save();
        }

        void UpdateFraming()
        {
            var cam = CameraDirector.Instance;
            if (cam == null || hero == null) return;
            framed.Clear();
            foreach (var e in Enemy.All)
                if (e.Alive && (e.Engaged || Mathf.Abs(e.transform.position.x - hero.transform.position.x) < 5f))
                    framed.Add(e.transform);
            cam.SetFramed(framed);
        }

        // ------------------------------------------------------------------ scoring and style

        void UpdateRank()
        {
            int r = 0;
            for (int i = 0; i < RankAt.Length; i++) if (StylePoints >= RankAt[i]) r = i;
            if (r > RankIndex && r >= 4) ShowStyle(Ranks[r] + "!");
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

        void AddFlow(float amount) { Flow = Mathf.Clamp(Flow + amount, 0f, 100f); }

        public void SpendFlow() { Flow = 0f; }

        /// <summary>Called by the hero for every strike that lands.</summary>
        public void OnHeroHit(Enemy e, int combo, bool heavy, bool killing, string move = "hit")
        {
            AddScore(heavy ? 25 : 10);
            AddStylePoints(heavy ? 45f : 25f, move);
            AddFlow(killing ? 14f : 6f);
            if (killing) RegisterKill(e, 100);
        }

        /// <summary>Kills by thrown weapons and other specials.</summary>
        public void OnHeroKill(Enemy e, string label, int points, bool special)
        {
            AddScore(points, label);
            AddStylePoints(special ? 160f : 60f, label);
            AddFlow(18f);
            RegisterKill(e, 0);
        }

        /// <summary>Wall splats and falls from the upper floor.</summary>
        public void OnEnvironmentKill(Enemy e, string label, int points)
        {
            AddScore(points, label);
            AddStylePoints(140f, label);
            AddFlow(15f);
            if (!e.Alive) RegisterKill(e, 0);
        }

        void RegisterKill(Enemy e, int bonus)
        {
            Kills++;
            if (bonus > 0) AddScore(bonus);
            bool lastOfWave = incoming.Count == 0 && AliveCount() == 0; // the victim is already down
            if (lastOfWave)
            {
                // last-kill cam: the signature cinematic moment of every wave
                bool big = WaveIndex == WavesPerFloor - 1;
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.18f, big ? 1.6f : 1.1f, 0.03f, 0.35f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.KillCam(e.Chest, big ? 1.6f : 1.1f, big ? 160f : 100f);
                AddScore(big ? 500 : 200, big ? "FINISH!" : "WAVE CLEAR");
            }
        }

        public void OnEnemyDied(Enemy e)
        {
            // armed enemies drop their weapons for the hero to take
            if (e.Type == EnemyType.Swordsman) WeaponPickup.Spawn(FightStyle.Sword, 10, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Gunner) WeaponPickup.Spawn(FightStyle.Gun, 8, e.transform.position, weaponMaterial);
            else if (e.Type == EnemyType.Brute) AddScore(800, "BRUTE DOWN");
        }

        public void OnHeroDamaged(float amount)
        {
            DamageTaken += amount;
            StylePoints *= 0.35f;
            Flow = Mathf.Max(0f, Flow - 25f);
        }

        public void OnPerfectDodge(string label)
        {
            AddScore(150, label);
            AddStylePoints(120f, label);
            AddFlow(20f);
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

        void ShowBanner(string text, float seconds)
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
