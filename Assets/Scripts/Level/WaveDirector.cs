using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>Runs the prototype room: three waves from five kinds of spawn point,
    /// the last-kill slow-mo camera, style score, grade and restart.</summary>
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        public HeroController hero;
        public GameObject enemyModel;
        public MoveSet moves;
        public Material enemyMaterial;
        public Transform[] spawnPoints = new Transform[5]; // indexed by SpawnKind
        public float enemyHealth = 30f;

        // the prototype's waves (data-driven in the vertical slice)
        static readonly SpawnKind[][] Waves =
        {
            new[] { SpawnKind.RightDoor, SpawnKind.LeftDoor, SpawnKind.RightDoor },
            new[] { SpawnKind.StairsTop, SpawnKind.LeftDoor, SpawnKind.StairsTop, SpawnKind.Background },
            new[] { SpawnKind.Ceiling, SpawnKind.RightDoor, SpawnKind.LeftDoor, SpawnKind.StairsTop, SpawnKind.Background, SpawnKind.Ceiling },
        };
        const float SpawnWarning = 0.6f;

        public enum Phase { Intro, Fighting, BetweenWaves, Cleared, Dead }
        public Phase CurrentPhase { get; private set; } = Phase.Intro;
        public int WaveIndex { get; private set; }
        public int WaveCount => Waves.Length;
        public int Score { get; private set; }
        public float DamageTaken { get; private set; }
        public float ElapsedTime { get; private set; }
        public string Banner { get; private set; }
        public float BannerUntil { get; private set; }
        public string StyleText { get; private set; }
        public float StyleUntil { get; private set; }

        public struct Incoming { public SpawnKind kind; public float time; }
        readonly List<Incoming> incoming = new List<Incoming>();
        public IReadOnlyList<Incoming> Upcoming => incoming;

        float phaseTime;
        int spawnedThisWave, spawnSerial;
        readonly List<Transform> framed = new List<Transform>();

        void Awake()
        {
            Instance = this;
            AttackTokens.Clear();
        }

        void Start()
        {
            ShowBanner("ROOM 1", 1.6f);
            phaseTime = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            phaseTime += dt;
            if (CurrentPhase == Phase.Fighting || CurrentPhase == Phase.BetweenWaves) ElapsedTime += dt;

            var input = GestureInput.Instance;
            if (input != null && input.Peek() == Command.Restart) { input.Next(); Restart(); return; }

            switch (CurrentPhase)
            {
                case Phase.Intro:
                    if (phaseTime > 1.4f) StartWave(0);
                    break;
                case Phase.Fighting:
                    SpawnDue();
                    if (hero != null && hero.IsDead) { CurrentPhase = Phase.Dead; phaseTime = 0f; ShowBanner("K.O.", 99f); }
                    else if (incoming.Count == 0 && AliveCount() == 0)
                    {
                        if (WaveIndex + 1 < Waves.Length) { CurrentPhase = Phase.BetweenWaves; phaseTime = 0f; }
                        else { CurrentPhase = Phase.Cleared; phaseTime = 0f; ShowBanner("ROOM CLEAR", 99f); }
                    }
                    break;
                case Phase.BetweenWaves:
                    if (phaseTime > 1.6f) StartWave(WaveIndex + 1);
                    break;
                case Phase.Cleared:
                case Phase.Dead:
                    if (phaseTime > 1.5f && input != null && input.AnyPressThisFrame) Restart();
                    break;
            }
            UpdateFraming();
        }

        void StartWave(int index)
        {
            WaveIndex = index;
            CurrentPhase = Phase.Fighting;
            phaseTime = 0f;
            spawnedThisWave = 0;
            ShowBanner(index + 1 < Waves.Length ? $"WAVE {index + 1}" : "FINAL WAVE", 1.2f);
            float t = Time.time + 0.5f;
            foreach (var kind in Waves[index])
            {
                incoming.Add(new Incoming { kind = kind, time = t });
                t += Random.Range(0.7f, 1.3f);
            }
        }

        void SpawnDue()
        {
            for (int i = incoming.Count - 1; i >= 0; i--)
            {
                if (Time.time < incoming[i].time + SpawnWarning) continue;
                Spawn(incoming[i].kind);
                incoming.RemoveAt(i);
            }
        }

        void Spawn(SpawnKind kind)
        {
            var point = spawnPoints[(int)kind];
            if (point == null || enemyModel == null) return;
            Vector3 pos = point.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, kind == SpawnKind.Background ? 0f : Random.Range(-0.25f, 0.25f));
            if (kind == SpawnKind.Ceiling && hero != null)
                pos.x = Mathf.Clamp(hero.transform.position.x + Random.Range(-2.5f, 2.5f), -11f, 11f);
            var go = FighterFactory.Build(enemyModel, moves, enemyMaterial, $"Enemy_{spawnSerial++}", false);
            go.transform.position = pos;
            var enemy = go.GetComponent<Enemy>();
            enemy.Init(hero, kind, enemyHealth);
            spawnedThisWave++;
        }

        int AliveCount()
        {
            int n = 0;
            foreach (var e in Enemy.All) if (e.Alive) n++;
            return n;
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

        /// <summary>Called by the hero for every strike that lands.</summary>
        public void OnHeroHit(Enemy e, int combo, bool heavy, bool killing)
        {
            Score += (heavy ? 25 : 10) * Mathf.Max(1, combo / 3 + 1);
            if (!killing) return;
            Score += 100;
            bool lastOfWave = incoming.Count == 0 && AliveCount() == 0; // the victim is already down
            if (lastOfWave)
            {
                // last-kill cam: the signature cinematic moment of every wave
                bool final = WaveIndex == Waves.Length - 1;
                if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.18f, final ? 1.6f : 1.1f, 0.03f, 0.35f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.KillCam(e.Chest, final ? 1.6f : 1.1f, final ? 160f : 100f);
                AddStyle(final ? 500 : 200, final ? "FINISH!" : "WAVE CLEAR");
            }
        }

        public void OnHeroDamaged(float amount) { DamageTaken += amount; }

        public void AddStyle(int points, string label)
        {
            Score += points;
            StyleText = label;
            StyleUntil = Time.unscaledTime + 1.2f;
        }

        void ShowBanner(string text, float seconds)
        {
            Banner = text;
            BannerUntil = Time.unscaledTime + seconds;
        }

        public string Grade()
        {
            float s = Score - DamageTaken * 12f - Mathf.Max(0f, ElapsedTime - 60f) * 5f;
            if (s >= 2600 && DamageTaken <= 0f) return "S";
            if (s >= 2000) return "A";
            if (s >= 1300) return "B";
            return "C";
        }

        public void Restart()
        {
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            Time.timeScale = 1f;
            AttackTokens.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
