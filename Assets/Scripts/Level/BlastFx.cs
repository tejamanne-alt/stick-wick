using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The fortress going up (the walk-out, docs/story/walkout.md), in the game's ink: fire is
    /// white-hot and pale, smoke is black. Each blast is a flash, a fireball that rolls out and up,
    /// a burst of black smoke and a column that keeps climbing, stone blocks thrown out that land and
    /// bounce, sparks, glass glinting as it falls, a ring of dust along the ground, then a fire that
    /// keeps burning where it was. Everything runs on scaled time, so the slow motion slows it.</summary>
    public static class BlastFx
    {
        static Material add, smoke, stone, stoneDark;
        static Texture2D soft, puff;
        static Mesh cube;
        static Transform ground;
        public static readonly List<ParticleSystem> Fires = new List<ParticleSystem>();

        static void Init(float groundY)
        {
            if (add == null)
            {
                soft = Soft(64);
                puff = Puff(128);
                add = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { mainTexture = soft };
                add.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                smoke = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = puff };
                smoke.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                stone = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.3f, 0.3f, 0.31f) };
                stoneDark = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.12f, 0.12f, 0.13f) };
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = g.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(g);
            }
            if (ground == null)
            {
                ground = new GameObject("BlastGround").transform;
                ground.position = new Vector3(0f, groundY, 0f);
            }
        }

        /// <summary>A soft round spot (the glow, the fire's tongues).</summary>
        static Texture2D Soft(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>A billowing puff: layered noise inside a soft round edge, darker in its folds.</summary>
        static Texture2D Puff(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float f = 0f, amp = 0.5f, fr = 3f;
                    for (int o = 0; o < 5; o++) { f += amp * Mathf.PerlinNoise(u * fr + o * 7.1f, v * fr + o * 3.7f); amp *= 0.5f; fr *= 2f; }
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float edge = Mathf.Clamp01((1f - d) * 2.2f - (1f - f) * 0.9f);
                    float a = Mathf.Clamp01(edge);
                    byte g = (byte)(Mathf.Lerp(150f, 255f, f) );
                    px[y * n + x] = new Color32(g, g, g, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        static ParticleSystem Sys(string name, Vector3 at, Material m, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.position = at;
            if (parent != null) go.transform.SetParent(parent, true);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.duration = 1f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var em = ps.emission;
            em.rateOverTime = 0f;
            return ps;
        }

        static void Burst(ParticleSystem ps, int n) { ps.Play(); ps.Emit(n); }

        static ParticleSystem.MinMaxCurve R(float a, float b) => new ParticleSystem.MinMaxCurve(a, b);

        static ParticleSystem.MinMaxGradient Fade(Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.35f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        static ParticleSystem.MinMaxCurve Grow(float a, float b) => new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, a, 1f, b));

        /// <summary>One blast at `at`, size k (1 = a window; 2 = the tower's dome), thrown towards `outward`.</summary>
        public static void Blast(Vector3 at, float k, Vector3 outward, float groundY, Transform parent = null, bool fire = true, bool column = true)
        {
            Init(groundY);
            ground.position = new Vector3(0f, groundY, 0f);
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.up;
            var holder = new GameObject("Blast").transform;
            holder.position = at;
            if (parent != null) holder.SetParent(parent, true);

            // the flash: one huge soft white bloom
            {
                var ps = Sys("Flash", at, add, holder);
                var main = ps.main;
                main.startLifetime = 0.5f;
                main.startSize = 14f * k;
                main.startSpeed = 0f;
                main.startColor = new Color(1f, 1f, 0.98f, 1f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(0.9f, 0.88f, 0.85f), new Color(0.5f, 0.5f, 0.5f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.4f, 1.3f);
                Burst(ps, 2);
            }
            // the fireball: white-hot tongues rolling out and up, slowing in the air
            {
                var ps = Sys("Fireball", at, add, holder);
                var main = ps.main;
                main.startLifetime = R(0.9f, 1.8f);
                main.startSize = R(2.2f * k, 5.0f * k);
                main.startSpeed = R(3f * k, 10f * k);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new Color(1f, 0.98f, 0.95f, 1f);
                main.gravityModifier = -0.15f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = 0.6f * k;
                sh.rotation = Quaternion.LookRotation(outward).eulerAngles;
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1.5f * k; lv.dampen = 0.12f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(0.86f, 0.84f, 0.8f), new Color(0.35f, 0.34f, 0.33f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.5f, 1.8f);
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-1.2f, 1.2f);
                Burst(ps, (int)(24 * Mathf.Clamp(k, 0.6f, 2.2f)));
            }
            // black smoke: a burst that boils up out of the fire
            {
                var ps = Sys("SmokeBurst", at + Vector3.up * 0.5f * k, smoke, holder);
                var main = ps.main;
                main.startLifetime = R(6f, 12f);
                main.startSize = R(3.5f * k, 7f * k);
                main.startSpeed = R(1.5f * k, 5.5f * k);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.06f, 0.06f, 0.065f, 0.95f), new Color(0.2f, 0.2f, 0.21f, 0.9f));
                main.gravityModifier = -0.06f;
                main.maxParticles = 400;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.2f * k;
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1.2f; lv.dampen = 0.06f;
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = R(0.6f, 1.6f); vel.y = R(1.5f, 3.2f); vel.z = R(0.2f, 0.9f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.6f, 3.0f);
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(0.75f, 0.73f, 0.7f), 0f), new GradientColorKey(Color.white, 0.12f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
                col.color = new ParticleSystem.MinMaxGradient(g);
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.3f, 0.3f);
                var nz = ps.noise; nz.enabled = true; nz.strength = 0.6f; nz.frequency = 0.25f;
                Burst(ps, (int)(26 * Mathf.Clamp(k, 0.6f, 2.2f)));
            }
            // stone thrown out: real blocks, tumbling, landing and bouncing on the ground
            for (int s = 0; s < 2; s++)
            {
                var ps = Sys(s == 0 ? "Stones" : "StonesDark", at, s == 0 ? stone : stoneDark, holder);
                var main = ps.main;
                main.startLifetime = R(3.5f, 6f);
                main.startSize3D = true;
                main.startSizeX = R(0.12f * k, 0.55f * k); main.startSizeY = R(0.1f * k, 0.4f * k); main.startSizeZ = R(0.12f * k, 0.5f * k);
                main.startSpeed = R(7f * Mathf.Sqrt(k), 19f * Mathf.Sqrt(k));
                main.startRotation3D = true;
                main.startRotationX = R(0f, 6.28f); main.startRotationY = R(0f, 6.28f); main.startRotationZ = R(0f, 6.28f);
                main.gravityModifier = 1f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 50f; sh.radius = 0.8f * k;
                sh.rotation = Quaternion.LookRotation((outward + Vector3.up * 0.8f).normalized).eulerAngles;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = R(-6f, 6f); rot.y = R(-6f, 6f); rot.z = R(-6f, 6f);
                var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.Planes; colm.SetPlane(0, ground);
                colm.bounce = R(0.15f, 0.35f); colm.dampen = R(0.3f, 0.6f); colm.lifetimeLoss = 0f;
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = cube;
                Burst(ps, (int)(22 * Mathf.Clamp(k, 0.6f, 2.2f)));
            }
            // sparks and glass: tiny bright points flung out, falling
            {
                var ps = Sys("Sparks", at, add, holder);
                var main = ps.main;
                main.startLifetime = R(1.2f, 3.5f);
                main.startSize = R(0.05f, 0.16f);
                main.startSpeed = R(5f * k, 16f * k);
                main.startColor = new Color(1f, 1f, 0.96f, 1f);
                main.gravityModifier = 0.5f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f * k;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(0.9f, 0.88f, 0.8f), new Color(0.3f, 0.3f, 0.3f));
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
                Burst(ps, (int)(60 * Mathf.Clamp(k, 0.6f, 2.2f)));
            }
            // a ring of dust rolling out along the ground
            if (at.y - groundY < 6f)
            {
                var ps = Sys("DustRing", new Vector3(at.x, groundY + 0.4f, at.z), smoke, holder);
                var main = ps.main;
                main.startLifetime = R(1.6f, 3f);
                main.startSize = R(1.8f, 3.6f);
                main.startSpeed = R(8f, 15f);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new Color(0.55f, 0.55f, 0.56f, 0.55f);
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.8f * k; sh.rotation = new Vector3(90f, 0f, 0f);
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0.6f; lv.dampen = 0.1f;
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.6f, 2.6f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white, Color.white);
                Burst(ps, (int)(36 * Mathf.Clamp(k, 0.6f, 1.6f)));
            }
            if (column) Column(at, k, holder);
            if (fire) Fire(at, k, holder);
        }

        /// <summary>The smoke that keeps climbing from where it went up, leaning with the wind.</summary>
        static void Column(Vector3 at, float k, Transform holder)
        {
            var ps = Sys("SmokeColumn", at + Vector3.up * 1.0f * k, smoke, holder);
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = R(10f, 16f);
            main.startSize = R(4f * k, 8f * k);
            main.startSpeed = R(0.5f, 1.5f);
            main.startRotation = R(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.05f, 0.05f, 0.055f, 0.9f), new Color(0.17f, 0.17f, 0.18f, 0.85f));
            main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = 3.2f * k;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.4f * k;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = R(0.8f, 2.2f); vel.y = R(2.5f, 4.5f); vel.z = R(0.5f, 1.5f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.5f, 3.4f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.8f, 0.78f, 0.75f), 0f), new GradientColorKey(Color.white, 0.1f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.75f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.2f, 0.2f);
            var nz = ps.noise; nz.enabled = true; nz.strength = 0.5f; nz.frequency = 0.2f;
            ps.Play();
        }

        /// <summary>The fire that stays: pale tongues licking up, a glow, embers lifting off it.</summary>
        static void Fire(Vector3 at, float k, Transform holder)
        {
            {
                var ps = Sys("Fire", at, add, holder);
                var main = ps.main;
                main.loop = true;
                main.duration = 2f;
                main.startLifetime = R(0.5f, 1.1f);
                main.startSize = R(0.9f * k, 2.0f * k);
                main.startSpeed = R(0.5f, 1.5f);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.97f, 0.92f, 0.8f), new Color(0.85f, 0.82f, 0.78f, 0.6f));
                main.gravityModifier = -0.4f;
                main.maxParticles = 120;
                var em = ps.emission; em.rateOverTime = 26f * Mathf.Clamp(k, 0.6f, 1.8f);
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.2f * k, 0.3f, 0.6f * k);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(0.9f, 0.88f, 0.84f), new Color(0.3f, 0.3f, 0.3f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(1f, 0.3f);
                ps.Play();
                Fires.Add(ps);
            }
            {
                var ps = Sys("Glow", at, add, holder);
                var main = ps.main;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = 0.6f;
                main.startSize = R(4.5f * k, 6f * k);
                main.startSpeed = 0f;
                main.startColor = new Color(0.55f, 0.53f, 0.5f, 0.35f);
                var em = ps.emission; em.rateOverTime = 6f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white, Color.white);
                ps.Play();
            }
            {
                var ps = Sys("Embers", at, add, holder);
                var main = ps.main;
                main.loop = true;
                main.duration = 3f;
                main.startLifetime = R(3f, 7f);
                main.startSize = R(0.04f, 0.1f);
                main.startSpeed = R(0.5f, 2f);
                main.startColor = new Color(1f, 0.98f, 0.94f, 1f);
                main.gravityModifier = -0.08f;
                var em = ps.emission; em.rateOverTime = 8f * k;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1f * k;
                var nz = ps.noise; nz.enabled = true; nz.strength = 1.2f; nz.frequency = 0.5f;
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = R(0.5f, 2f); vel.y = R(0.5f, 1.5f); vel.z = R(-0.5f, -2.5f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, new Color(0.9f, 0.9f, 0.88f), new Color(0.4f, 0.4f, 0.4f));
                ps.Play();
            }
        }

        /// <summary>Ash and embers drifting round the camera, wherever it goes (after the first blast).</summary>
        public static ParticleSystem Drift(Transform follow)
        {
            Init(0f);
            var ps = Sys("Drift", follow.position, add, null);
            var main = ps.main;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = R(4f, 8f);
            main.startSize = R(0.02f, 0.07f);
            main.startSpeed = R(0.1f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.98f, 0.94f, 0.9f), new Color(0.6f, 0.6f, 0.6f, 0.7f));
            main.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 26f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10f, 6f, 8f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = R(0.3f, 1.2f); vel.y = R(-0.4f, 0.3f); vel.z = R(-1.2f, -0.3f);
            var nz = ps.noise; nz.enabled = true; nz.strength = 0.6f; nz.frequency = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, Color.white, Color.white);
            ps.Play();
            return ps;
        }

        /// <summary>A small steady glow (a lantern still burning at dawn), facing the camera's side.</summary>
        public static Transform Glow(Vector3 at, float size, float alpha)
        {
            Init(0f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "LanternGlow";
            q.transform.position = at;
            q.transform.rotation = Quaternion.identity;               // faces -z: towards the camera's side
            q.transform.localScale = Vector3.one * size;
            var m = new Material(add);
            m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.48f, alpha));
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        public static void Clear()
        {
            Fires.Clear();
        }
    }
}
