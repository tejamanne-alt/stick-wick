using UnityEngine;

namespace ProjectInk
{
    /// <summary>Hit effects in the style of the reference: black ink spatter, soft white
    /// light bursts behind big hits, the one-frame impact flash, and procedural punch
    /// sounds (so the prototype needs no audio files). Textures are generated at start.</summary>
    public class InkFx : MonoBehaviour
    {
        public static InkFx Instance { get; private set; }

        [Tooltip("Alpha-blended particle material (Legacy Shaders/Particles/Alpha Blended).")]
        public Material particleMaterial;
        [Tooltip("Additive particle material for the light bursts (Legacy Shaders/Particles/Additive).")]
        public Material glowMaterial;
        [Tooltip("Vertex-coloured material for bullet tracers (Sprites/Default).")]
        public Material lineMaterial;
        public Color inkColor = new Color(0.02f, 0.02f, 0.02f, 1f);
        [Tooltip("Z of the building's back wall, where wall stains go.")]
        public float wallZ = 2.18f;
        public int maxStains = 180;
        public Color redInk = new Color(0.75f, 0.05f, 0.05f, 1f);

        ParticleSystem splash, glow;
        AudioSource audioSource;
        AudioClip punchLight, punchHeavy, whoosh, gunshot;
        LineRenderer[] tracers;
        float[] tracerUntil;
        int nextTracer;
        const float TracerSeconds = 0.09f;
        float flashUntil;
        Texture2D white;

        void Awake()
        {
            Instance = this;
            splash = BuildSplash();
            glow = BuildGlow();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            punchLight = Synth("punch_light", 0.09f, 900f, 0.6f, 1);
            punchHeavy = Synth("punch_heavy", 0.16f, 420f, 0.9f, 2);
            whoosh = Synth("whoosh", 0.14f, 2400f, 0.25f, 3);
            gunshot = Synth("gunshot", 0.22f, 5200f, 1.0f, 4);
            BuildTracers();
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
            if (particleMaterial != null)
                renderer.sharedMaterial = new Material(particleMaterial) { mainTexture = RadialTexture(32, 0.8f, 6f) }; // round ink blots
            return ps;
        }

        ParticleSystem BuildGlow()
        {
            var go = new GameObject("LightBurst");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startSpeed = 0f;
            main.startLifetime = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 16;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1f));
            var colorLife = ps.colorOverLifetime;
            colorLife.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(0f, 1f) });
            colorLife.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 50f; // draw behind the fighters
            if (glowMaterial != null)
                renderer.sharedMaterial = new Material(glowMaterial) { mainTexture = RadialTexture(128, 0f, 2.2f) };
            return ps;
        }

        void BuildTracers()
        {
            tracers = new LineRenderer[6];
            tracerUntil = new float[tracers.Length];
            for (int i = 0; i < tracers.Length; i++)
            {
                var go = new GameObject("Tracer" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0.015f);
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                if (lineMaterial != null) lr.sharedMaterial = lineMaterial;
                lr.enabled = false;
                tracers[i] = lr;
            }
        }

        /// <summary>A gunshot: muzzle flash, a white tracer to the hit point, the bang.</summary>
        public void Shot(Vector3 from, Vector3 to)
        {
            if (tracers != null)
            {
                var lr = tracers[nextTracer];
                tracerUntil[nextTracer] = UnityEngine.Time.unscaledTime + TracerSeconds;
                nextTracer = (nextTracer + 1) % tracers.Length;
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
                lr.startColor = lr.endColor = Color.white;
                lr.enabled = true;
            }
            Burst(from, 1.4f);
            if (audioSource != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.08f);
                audioSource.PlayOneShot(gunshot, 0.9f);
            }
        }

        void Update()
        {
            if (tracers == null) return;
            float now = UnityEngine.Time.unscaledTime;
            for (int i = 0; i < tracers.Length; i++)
            {
                if (!tracers[i].enabled) continue;
                float left = tracerUntil[i] - now;
                if (left <= 0f) { tracers[i].enabled = false; continue; }
                var c = new Color(1f, 1f, 1f, left / TracerSeconds);
                tracers[i].startColor = tracers[i].endColor = c;
            }
        }

        /// <summary>White radial falloff: solid out to `core` (0..1 of the radius), then fading.</summary>
        static Texture2D RadialTexture(int size, float core, float power)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = d <= core ? 1f : Mathf.Pow(Mathf.Clamp01(1f - (d - core) / (1f - core + 1e-4f)), power);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A soft white burst of light behind a big hit, like a camera flare.</summary>
        public void Burst(Vector3 point, float size)
        {
            if (glow == null) return;
            var emit = new ParticleSystem.EmitParams
            {
                position = point + Vector3.forward * 0.6f,
                startSize = size,
                startColor = Color.white,
                applyShapeToPosition = false
            };
            glow.Emit(emit, 1);
        }

        /// <summary>Splash ink in an enemy's colour.</summary>
        public void Splash(Vector3 point, Vector3 direction, int count, Color color)
        {
            if (splash == null) return;
            splash.transform.position = point;
            splash.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.01f ? direction : Vector3.right);
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = true };
            splash.Emit(emit, count);
        }

        // ---------------------------------------------------------------- stains that stay
        readonly System.Collections.Generic.Queue<GameObject> stains = new System.Collections.Generic.Queue<GameObject>();
        Material stainMaterial;
        MaterialPropertyBlock stainBlock;
        static readonly int TintId = Shader.PropertyToID("_TintColor");

        GameObject NewStain(Vector3 pos, Quaternion rot, float size, Color color)
        {
            if (stainMaterial == null)
            {
                if (particleMaterial == null) return null;
                stainMaterial = new Material(particleMaterial) { mainTexture = BlotTexture(64, 7) };
                stainBlock = new MaterialPropertyBlock();
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "Stain";
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = new Vector3(size, size * Random.Range(0.6f, 1f), 1f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = stainMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            // legacy particle shaders double the tint: halve it
            stainBlock.SetColor(TintId, new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.5f));
            r.SetPropertyBlock(stainBlock);
            stains.Enqueue(go);
            while (stains.Count > maxStains) { var old = stains.Dequeue(); if (old != null) Destroy(old); }
            return go;
        }

        /// <summary>A puddle of colour on the floor where someone fell.</summary>
        public void Stain(Vector3 floorPoint, Color color, float size, bool unused)
        {
            var pos = new Vector3(floorPoint.x, floorPoint.y + 0.012f + Random.Range(0f, 0.004f), Random.Range(-0.5f, 0.9f));
            NewStain(pos, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f), size, color);
        }

        /// <summary>A splatter on the building's back wall.</summary>
        public void StainWall(float x, float y, Color color, float size)
        {
            var pos = new Vector3(x, y, wallZ - 0.01f - Random.Range(0f, 0.004f));
            NewStain(pos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), size, color);
        }

        public void ClearStains()
        {
            while (stains.Count > 0) { var s = stains.Dequeue(); if (s != null) Destroy(s); }
        }

        /// <summary>An irregular ink blot: a disc with lumpy edges and a few droplets.</summary>
        static Texture2D BlotTexture(int size, int seed)
        {
            var rng = new System.Random(seed);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            var lobes = new float[7];
            for (int i = 0; i < lobes.Length; i++) lobes[i] = (float)rng.NextDouble();
            var drops = new Vector3[6];
            for (int i = 0; i < drops.Length; i++)
            {
                float a = (float)rng.NextDouble() * 6.283f, d = 0.62f + (float)rng.NextDouble() * 0.3f;
                drops[i] = new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.05f + (float)rng.NextDouble() * 0.07f);
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x - c) / c, v = (y - c) / c;
                    float ang = Mathf.Atan2(v, u), r = Mathf.Sqrt(u * u + v * v);
                    float edge = 0.48f;
                    for (int i = 0; i < lobes.Length; i++) edge += 0.07f * Mathf.Sin(ang * (i + 2) + lobes[i] * 6.28f) * lobes[i];
                    float a = Mathf.Clamp01((edge - r) * 18f);
                    foreach (var dr in drops)
                        a = Mathf.Max(a, Mathf.Clamp01((dr.z - Vector2.Distance(new Vector2(u, v), new Vector2(dr.x, dr.y))) * 30f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
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
