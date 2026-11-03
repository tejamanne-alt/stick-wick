using UnityEngine;

namespace ProjectInk
{
    /// <summary>Hit effects: ink splashes, the one-frame impact flash and procedural
    /// punch sounds (so the prototype needs no audio files).</summary>
    public class InkFx : MonoBehaviour
    {
        public static InkFx Instance { get; private set; }

        public Material particleMaterial;
        public Color inkColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        public Color redInk = new Color(0.75f, 0.05f, 0.05f, 1f);

        ParticleSystem splash;
        AudioSource audioSource;
        AudioClip punchLight, punchHeavy, whoosh;
        float flashUntil;
        Texture2D white;

        void Awake()
        {
            Instance = this;
            splash = BuildSplash();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            punchLight = Synth("punch_light", 0.09f, 900f, 0.6f, 1);
            punchHeavy = Synth("punch_heavy", 0.16f, 420f, 0.9f, 2);
            whoosh = Synth("whoosh", 0.14f, 2400f, 0.25f, 3);
            white = Texture2D.whiteTexture;
        }

        ParticleSystem BuildSplash()
        {
            var go = new GameObject("InkSplash");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
            return ps;
        }

        /// <summary>Splash ink at a hit point, sprayed along `direction`.</summary>
        public void Splash(Vector3 point, Vector3 direction, int count, bool red = false)
        {
            if (splash == null) return;
            splash.transform.position = point;
            splash.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.01f ? direction : Vector3.right);
            var emit = new ParticleSystem.EmitParams { startColor = red ? redInk : inkColor, applyShapeToPosition = true };
            splash.Emit(emit, count);
        }

        public void ImpactFlash(float seconds = 0.05f)
        {
            flashUntil = UnityEngine.Time.unscaledTime + seconds;
        }

        public void PlayHit(bool heavy)
        {
            if (audioSource == null) return;
            audioSource.pitch = Random.Range(0.9f, 1.1f) * Mathf.Lerp(0.75f, 1f, UnityEngine.Time.timeScale);
            audioSource.PlayOneShot(heavy ? punchHeavy : punchLight, heavy ? 1f : 0.8f);
        }

        public void PlayWhoosh()
        {
            if (audioSource == null) return;
            audioSource.pitch = Random.Range(0.9f, 1.15f);
            audioSource.PlayOneShot(whoosh, 0.35f);
        }

        void OnGUI()
        {
            if (UnityEngine.Time.unscaledTime >= flashUntil) return;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = old;
        }

        /// <summary>Filtered noise burst with a fast attack and exponential decay.</summary>
        static AudioClip Synth(string clipName, float seconds, float cutoff, float body, int seed)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(seconds * rate);
            var data = new float[n];
            var rng = new System.Random(seed);
            float lp = 0f, a = Mathf.Clamp01(cutoff / rate * 6.28f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += a * (noise - lp);
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / (seconds * 0.3f));
                float thump = Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(140f, 55f, t / seconds)) * body;
                data[i] = Mathf.Clamp((lp * 1.8f + thump) * env, -1f, 1f);
            }
            var clip = AudioClip.Create(clipName, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
