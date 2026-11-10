using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The fortress going up (the walk-out, docs/story/walkout.md), as realistic as particles
    /// get. Each blast is built in layers:
    /// - a white-hot flash, and a shockwave ring racing out;
    /// - a fireball that blooms and rolls (an animated flipbook: white core, orange, deep red, then
    ///   black smoke at its edges), with a glow under it;
    /// - stone blocks thrown out that trail smoke as they arc, land and bounce;
    /// - burning fragments streaking out; sparks; window glass glinting as it falls;
    /// - a wall of dust rolling along the ground;
    /// - a burst of black smoke, then a column that keeps climbing and spreads across the sky;
    /// - a fire that stays: flipbook flames, a glow, embers lifting off it.
    /// Each fire also lights the fortress's stone (ProjectInk/FireLit: up to eight fire lights, a
    /// flash, the fade into the burning bake). Everything runs on scaled time, so the slow motion
    /// floats it.</summary>
    public static class BlastFx
    {
        static Material add, alpha, fireball, smokeMat, flameMat, dustMat, ringMat, stone, stoneDark, trailMat;
        static Texture2D glowTex;
        static Mesh cube;
        static Transform ground;
        public static readonly List<ParticleSystem> Fires = new List<ParticleSystem>();

        // the fire lights on the stone (ProjectInk/FireLit)
        class Light { public Vector3 at; public float reach, flash, steady, born, k; }
        static readonly List<Light> lights = new List<Light>();
        static readonly Vector4[] lp = new Vector4[8], lc = new Vector4[8];
        static float flashGlobal;
        static readonly int FireLightsId = Shader.PropertyToID("_FireLights"), FireColorsId = Shader.PropertyToID("_FireColors"),
            FireCountId = Shader.PropertyToID("_FireCount"), FlashId = Shader.PropertyToID("_FortFlash"), BurnId = Shader.PropertyToID("_FortBurn");

        static Material Mat(string shader, Texture tex)
        {
            var m = new Material(Shader.Find(shader)) { mainTexture = tex };
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return m;
        }

        static void Init(float groundY)
        {
            if (add == null)
            {
                glowTex = Resources.Load<Texture2D>("Fx/fx_glow");
                if (glowTex == null) glowTex = Soft(64);
                add = Mat("Legacy Shaders/Particles/Additive", glowTex);
                alpha = Mat("Legacy Shaders/Particles/Alpha Blended", glowTex);
                fireball = Mat("Legacy Shaders/Particles/Alpha Blended", Resources.Load<Texture2D>("Fx/fx_fireball"));
                smokeMat = Mat("Legacy Shaders/Particles/Alpha Blended", Resources.Load<Texture2D>("Fx/fx_smoke"));
                flameMat = Mat("Legacy Shaders/Particles/Additive", Resources.Load<Texture2D>("Fx/fx_flame"));
                dustMat = Mat("Legacy Shaders/Particles/Alpha Blended", Resources.Load<Texture2D>("Fx/fx_dust"));
                ringMat = Mat("Legacy Shaders/Particles/Additive", Resources.Load<Texture2D>("Fx/fx_ring"));
                trailMat = Mat("Legacy Shaders/Particles/Additive", glowTex);
                stone = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.34f, 0.33f, 0.32f) };
                stoneDark = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.13f, 0.12f, 0.12f) };
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = g.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(g);
            }
            if (ground == null) ground = new GameObject("BlastGround").transform;
            ground.position = new Vector3(0f, groundY, 0f);
        }

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
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        // ---------------------------------------------------------------- particle plumbing

        static ParticleSystem Sys(string name, Vector3 at, Material m, Transform parent, int max = 1000)
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
            main.maxParticles = max;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 0f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            return ps;
        }

        /// <summary>Velocity over lifetime, all three axes in the same mode (Unity insists).</summary>
        static void Vel(ParticleSystem ps, ParticleSystem.MinMaxCurve x, ParticleSystem.MinMaxCurve y, ParticleSystem.MinMaxCurve z)
        {
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = x; v.y = y; v.z = z;
        }

        static void Burst(ParticleSystem ps, int n) { ps.Play(); ps.Emit(Mathf.Max(1, n)); }
        static ParticleSystem.MinMaxCurve R(float a, float b) => new ParticleSystem.MinMaxCurve(a, b);
        static ParticleSystem.MinMaxCurve Grow(float a, float b) => new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, a, 1f, b));
        static ParticleSystem.MinMaxCurve GrowFast(float a, float b) => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, a, 0f, 6f), new Keyframe(0.25f, a + (b - a) * 0.7f), new Keyframe(1f, b)));

        static ParticleSystem.MinMaxGradient Grad(Color[] cols, float[] cpos, float[] alphas, float[] apos)
        {
            var ck = new GradientColorKey[cols.Length];
            for (int i = 0; i < cols.Length; i++) ck[i] = new GradientColorKey(cols[i], cpos[i]);
            var ak = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], apos[i]);
            var g = new Gradient();
            g.SetKeys(ck, ak);
            return new ParticleSystem.MinMaxGradient(g);
        }

        static void Flipbook(ParticleSystem ps, int tiles, bool randomStart = false, float cycles = 1f)
        {
            var ts = ps.textureSheetAnimation;
            ts.enabled = true;
            ts.numTilesX = tiles; ts.numTilesY = tiles;
            ts.animation = ParticleSystemAnimationType.WholeSheet;
            ts.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            ts.cycleCount = Mathf.Max(1, Mathf.RoundToInt(cycles));
            if (randomStart) ts.startFrame = R(0f, tiles * tiles - 1);
        }

        static readonly Color Hot = new Color(1f, 0.93f, 0.75f), Orange = new Color(1f, 0.55f, 0.18f), Ember = new Color(1f, 0.42f, 0.12f);

        // ---------------------------------------------------------------- one blast

        /// <summary>One blast at `at`, size k (1 = a window, 3 = the tower's dome), thrown towards `outward`.</summary>
        public static void Blast(Vector3 at, float k, Vector3 outward, float groundY, Transform parent = null, bool fire = true, bool column = true)
        {
            Init(groundY);
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.up;
            float kc = Mathf.Clamp(k, 0.6f, 3.5f);
            var holder = new GameObject("Blast").transform;
            holder.position = at;
            if (parent != null) holder.SetParent(parent, true);

            // the flash: a white-hot bloom, gone in a quarter of a second
            {
                var ps = Sys("Flash", at, add, holder, 4);
                var main = ps.main;
                main.startLifetime = R(0.18f, 0.32f);
                main.startSize = R(16f * k, 24f * k);
                main.startSpeed = 0f;
                main.startColor = new Color(1f, 0.92f, 0.78f, 1f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Orange }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 0f, 1f });
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.6f, 1.2f);
                Burst(ps, 2);
            }
            // the shockwave: a ring racing out, and a ring along the ground
            {
                var ps = Sys("Shockwave", at, ringMat, holder, 2);
                var main = ps.main;
                main.startLifetime = 0.55f;
                main.startSize = 3f * k;
                main.startSpeed = 0f;
                main.startColor = new Color(1f, 0.95f, 0.85f, 0.35f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = GrowFast(1f, 22f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0.9f, 0f }, new[] { 0f, 1f });
                Burst(ps, 1);
                if (at.y - groundY < 8f)
                {
                    var pg = Sys("GroundRing", new Vector3(at.x, groundY + 0.3f, at.z), ringMat, holder, 2);
                    var m2 = pg.main;
                    m2.startLifetime = 0.9f; m2.startSize = 4f * k; m2.startSpeed = 0f;
                    m2.startColor = new Color(0.9f, 0.85f, 0.78f, 0.3f);
                    var s2 = pg.sizeOverLifetime; s2.enabled = true; s2.size = GrowFast(1f, 16f);
                    var c2 = pg.colorOverLifetime; c2.enabled = true;
                    c2.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0.8f, 0f }, new[] { 0f, 1f });
                    pg.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    Burst(pg, 1);
                }
            }
            // the fireball: rolling, cooling flipbook billows thrown out and up, slowing in the air
            {
                var ps = Sys("Fireball", at, fireball, holder, 80);
                var main = ps.main;
                main.startLifetime = R(2.0f, 3.4f);
                main.startSize = R(4.5f * k, 9f * k);
                main.startSpeed = R(4f * k, 13f * k);
                main.startRotation = R(0f, 6.28f);
                main.startColor = Color.white;
                main.gravityModifier = -0.12f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 60f; sh.radius = 0.8f * k;
                sh.rotation = Quaternion.LookRotation(outward).eulerAngles;
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1.2f * k; lv.dampen = 0.15f;
                Vel(ps, R(0f, 0f), R(1.2f, 3.2f), R(0f, 0f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = GrowFast(0.35f, 1.9f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 1f, 1f, 0f }, new[] { 0f, 0.7f, 1f });
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.8f, 0.8f);
                Flipbook(ps, 8);
                Burst(ps, (int)(14 * kc));
            }
            // its glow: the heat lighting the air round it
            {
                var ps = Sys("Heat", at, add, holder, 12);
                var main = ps.main;
                main.startLifetime = R(0.8f, 1.6f);
                main.startSize = R(10f * k, 16f * k);
                main.startSpeed = R(0f, 2f);
                main.startColor = new Color(1f, 0.5f, 0.15f, 0.55f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Hot, Ember }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 0f, 1f });
                Burst(ps, 4);
            }
            // stone blocks: tumbling out, trailing smoke, landing and bouncing
            for (int s = 0; s < 2; s++)
            {
                var ps = Sys(s == 0 ? "Stones" : "StonesDark", at, s == 0 ? stone : stoneDark, holder, 120);
                var main = ps.main;
                main.startLifetime = R(4f, 7f);
                main.startSize3D = true;
                main.startSizeX = R(0.15f * k, 0.8f * k); main.startSizeY = R(0.12f * k, 0.5f * k); main.startSizeZ = R(0.15f * k, 0.7f * k);
                main.startSpeed = R(9f * Mathf.Sqrt(k), 26f * Mathf.Sqrt(k));
                main.startRotation3D = true;
                main.startRotationX = R(0f, 6.28f); main.startRotationY = R(0f, 6.28f); main.startRotationZ = R(0f, 6.28f);
                main.gravityModifier = 1f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = 1.0f * k;
                sh.rotation = Quaternion.LookRotation((outward + Vector3.up * 0.9f).normalized).eulerAngles;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = R(-7f, 7f); rot.y = R(-7f, 7f); rot.z = R(-7f, 7f);
                var colm = ps.collision; colm.enabled = true; colm.type = ParticleSystemCollisionType.Planes; colm.SetPlane(0, ground);
                colm.bounce = R(0.12f, 0.32f); colm.dampen = R(0.3f, 0.6f); colm.lifetimeLoss = 0f;
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = cube;
                if (s == 0)
                {
                    // a smoke trail behind each block as it flies
                    var trail = Sys("StoneTrail", at, smokeMat, ps.transform, 900);
                    var tm = trail.main;
                    tm.startLifetime = R(1.2f, 2.4f);
                    tm.startSize = R(0.6f * k, 1.4f * k);
                    tm.startSpeed = 0f;
                    tm.startRotation = R(0f, 6.28f);
                    tm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.18f, 0.16f, 0.15f, 0.7f), new Color(0.32f, 0.3f, 0.28f, 0.55f));
                    tm.loop = true;
                    var te = trail.emission; te.rateOverTime = 0f; te.rateOverDistance = 1.6f;
                    var tsz = trail.sizeOverLifetime; tsz.enabled = true; tsz.size = Grow(0.6f, 2.0f);
                    var tc = trail.colorOverLifetime; tc.enabled = true;
                    tc.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0.9f, 0f }, new[] { 0f, 1f });
                    Flipbook(trail, 8, true);
                    var se = ps.subEmitters; se.enabled = true;
                    se.AddSubEmitter(trail, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
                }
                Burst(ps, (int)(16 * kc));
            }
            // burning fragments: bright streaks arcing out with fiery trails
            {
                var ps = Sys("Burning", at, add, holder, 120);
                var main = ps.main;
                main.startLifetime = R(1.6f, 3.6f);
                main.startSize = R(0.25f, 0.6f);
                main.startSpeed = R(10f * Mathf.Sqrt(k), 28f * Mathf.Sqrt(k));
                main.startColor = new Color(1f, 0.75f, 0.35f, 1f);
                main.gravityModifier = 0.9f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.8f * k;
                sh.rotation = Quaternion.LookRotation(Vector3.up).eulerAngles;
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Hot, Orange, Ember }, new[] { 0f, 0.5f, 1f }, new[] { 1f, 0.8f, 0f }, new[] { 0f, 0.7f, 1f });
                var tr = ps.trails; tr.enabled = true; tr.ratio = 1f; tr.lifetime = R(0.25f, 0.5f); tr.minVertexDistance = 0.3f;
                tr.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                tr.colorOverTrail = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.2f, 0.9f), new Color(0.6f, 0.2f, 0.05f, 0f));
                tr.inheritParticleColor = true;
                ps.GetComponent<ParticleSystemRenderer>().trailMaterial = trailMat;
                Burst(ps, (int)(18 * kc));
            }
            // sparks and glass: tiny points flung out and falling, the glass glinting
            {
                var ps = Sys("Sparks", at, add, holder, 300);
                var main = ps.main;
                main.startLifetime = R(1.0f, 3.5f);
                main.startSize = R(0.05f, 0.16f);
                main.startSpeed = R(8f * k, 24f * k);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.5f, 1f), new Color(0.9f, 0.95f, 1f, 1f));
                main.gravityModifier = 0.7f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.6f * k;
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Orange }, new[] { 0f, 1f }, new[] { 1f, 1f, 0f }, new[] { 0f, 0.6f, 1f });
                var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 2f;
                Burst(ps, (int)(70 * kc));
            }
            // a wall of dust rolling out along the ground
            if (at.y - groundY < 8f)
            {
                var ps = Sys("DustWall", new Vector3(at.x, groundY + 0.8f, at.z), dustMat, holder, 120);
                var main = ps.main;
                main.startLifetime = R(3.5f, 7f);
                main.startSize = R(4f * k, 8f * k);
                main.startSpeed = R(10f, 20f);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.58f, 0.55f, 0.5f, 0.75f), new Color(0.42f, 0.4f, 0.37f, 0.7f));
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f * k; sh.rotation = new Vector3(90f, 0f, 0f);
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0.8f; lv.dampen = 0.12f;
                Vel(ps, R(0f, 0f), R(0.3f, 1.2f), R(0f, 0f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.5f, 2.6f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0.6f, 0f }, new[] { 0f, 0.1f, 0.6f, 1f });
                Flipbook(ps, 4);
                Burst(ps, (int)(30 * Mathf.Clamp(k, 0.6f, 2f)));
            }
            // black smoke boiling up out of the fire
            {
                var ps = Sys("SmokeBurst", at + Vector3.up * 0.6f * k, smokeMat, holder, 200);
                var main = ps.main;
                main.startLifetime = R(9f, 16f);
                main.startSize = R(6f * k, 12f * k);
                main.startSpeed = R(2f * k, 7f * k);
                main.startRotation = R(0f, 6.28f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.09f, 0.08f, 0.075f, 1f), new Color(0.24f, 0.22f, 0.2f, 0.95f));
                main.gravityModifier = -0.05f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.6f * k;
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 1.4f; lv.dampen = 0.07f;
                Vel(ps, R(0.8f, 2.2f), R(2.0f, 4.5f), R(0.4f, 1.4f));
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.55f, 3.2f);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { new Color(1f, 0.6f, 0.35f), Color.white, Color.white }, new[] { 0f, 0.12f, 1f }, new[] { 0f, 1f, 0.9f, 0f }, new[] { 0f, 0.06f, 0.65f, 1f });
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.25f, 0.25f);
                Flipbook(ps, 8, true);
                Burst(ps, (int)(18 * kc));
            }
            if (k >= 2.4f) Pillar(at, k, holder);
            if (column) Column(at, k, holder);
            if (fire) Fire(at, k, holder);
            AddLight(at, k);
            flashGlobal = Mathf.Max(flashGlobal, Mathf.Clamp01(0.35f * k));
        }

        /// <summary>The great ones (the dome, the keep): a fireball climbing on its own column, its
        /// smoke spreading into a cap.</summary>
        static void Pillar(Vector3 at, float k, Transform holder)
        {
            var ps = Sys("Pillar", at, fireball, holder, 120);
            var main = ps.main;
            main.duration = 1.6f;
            main.startLifetime = R(2.4f, 3.6f);
            main.startSize = R(5f * k, 9f * k);
            main.startSpeed = R(1f, 3f);
            main.startRotation = R(0f, 6.28f);
            var em = ps.emission; em.rateOverTime = 24f;
            Vel(ps, R(0f, 0f), R(9f, 15f), R(0f, 0f));
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.5f, 2.0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 1f, 1f, 0f }, new[] { 0f, 0.75f, 1f });
            Flipbook(ps, 8);
            ps.Play();
            var cap = Sys("Cap", at + Vector3.up * 22f, smokeMat, holder, 120);
            var cm = cap.main;
            cm.duration = 3f;
            cm.startLifetime = R(14f, 20f);
            cm.startSize = R(12f, 22f);
            cm.startSpeed = R(1f, 5f);
            cm.startRotation = R(0f, 6.28f);
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.08f, 0.075f, 0.07f, 1f), new Color(0.2f, 0.19f, 0.18f, 0.95f));
            var ce = cap.emission; ce.rateOverTime = 18f;
            var csh = cap.shape; csh.enabled = true; csh.shapeType = ParticleSystemShapeType.Circle; csh.radius = 6f; csh.rotation = new Vector3(90f, 0f, 0f);
            Vel(cap, R(1f, 3f), R(3f, 6f), R(0f, 0f));
            var csz = cap.sizeOverLifetime; csz.enabled = true; csz.size = Grow(0.6f, 3.0f);
            var cc = cap.colorOverLifetime; cc.enabled = true;
            cc.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0.85f, 0f }, new[] { 0f, 0.1f, 0.7f, 1f });
            Flipbook(cap, 8, true);
            cap.Play();
        }

        /// <summary>The smoke that keeps climbing from where it went up, leaning with the wind and
        /// spreading into the sky.</summary>
        static void Column(Vector3 at, float k, Transform holder)
        {
            var ps = Sys("SmokeColumn", at + Vector3.up * 1.5f * k, smokeMat, holder, 260);
            var main = ps.main;
            main.loop = true;
            main.duration = 6f;
            main.startLifetime = R(14f, 22f);
            main.startSize = R(6f * k, 11f * k);
            main.startSpeed = R(0.5f, 1.5f);
            main.startRotation = R(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.07f, 0.065f, 0.06f, 1f), new Color(0.22f, 0.2f, 0.19f, 0.9f));
            var em = ps.emission; em.rateOverTime = 2.2f * k;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.8f * k;
            Vel(ps, R(1.2f, 3.0f), R(3.5f, 6.5f), R(0.5f, 2.0f));
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.5f, 4.0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Grad(new[] { new Color(1f, 0.55f, 0.3f), Color.white, Color.white }, new[] { 0f, 0.1f, 1f }, new[] { 0f, 1f, 0.8f, 0f }, new[] { 0f, 0.08f, 0.7f, 1f });
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.15f, 0.15f);
            Flipbook(ps, 8, true);
            ps.Play();
        }

        /// <summary>The fire that stays: tongues of flame, a glow, embers lifting off it.</summary>
        static void Fire(Vector3 at, float k, Transform holder)
        {
            {
                var ps = Sys("Flames", at, flameMat, holder, 120);
                var main = ps.main;
                main.loop = true;
                main.duration = 2f;
                main.startLifetime = R(0.8f, 1.4f);
                main.startSize = R(2.2f * k, 4.2f * k);
                main.startSpeed = R(0.3f, 1.0f);
                main.startRotation = R(-0.15f, 0.15f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.85f, 0.95f), new Color(1f, 0.75f, 0.55f, 0.8f));
                main.gravityModifier = -0.25f;
                var em = ps.emission; em.rateOverTime = 11f * Mathf.Clamp(k, 0.6f, 2.5f);
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f * k, 0.4f, 0.8f * k);
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0.9f, 0f }, new[] { 0f, 0.12f, 0.6f, 1f });
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Grow(0.8f, 1.15f);
                Flipbook(ps, 8, true);
                ps.Play();
                Fires.Add(ps);
            }
            {
                var ps = Sys("Glow", at, add, holder, 20);
                var main = ps.main;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = R(0.4f, 0.8f);
                main.startSize = R(6f * k, 9f * k);
                main.startSpeed = 0f;
                main.startColor = new Color(1f, 0.45f, 0.12f, 0.32f);
                var em = ps.emission; em.rateOverTime = 7f;
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0f }, new[] { 0f, 0.3f, 1f });
                ps.Play();
            }
            {
                var ps = Sys("Embers", at, add, holder, 200);
                var main = ps.main;
                main.loop = true;
                main.duration = 3f;
                main.startLifetime = R(3f, 8f);
                main.startSize = R(0.05f, 0.14f);
                main.startSpeed = R(1f, 3f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.35f, 1f), new Color(1f, 0.5f, 0.15f, 1f));
                main.gravityModifier = -0.1f;
                var em = ps.emission; em.rateOverTime = 10f * k;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.2f * k;
                var nz = ps.noise; nz.enabled = true; nz.strength = 1.4f; nz.frequency = 0.5f;
                Vel(ps, R(0.5f, 2.5f), R(1f, 2.5f), R(-0.5f, -2.5f));
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Grad(new[] { Color.white, Ember }, new[] { 0f, 1f }, new[] { 1f, 1f, 0f }, new[] { 0f, 0.6f, 1f });
                ps.Play();
            }
        }

        // ---------------------------------------------------------------- the light on the stone

        static void AddLight(Vector3 at, float k)
        {
            lights.Add(new Light { at = at, reach = 10f + 7f * k, flash = 2.6f * Mathf.Min(k, 2.5f), steady = 0.35f + 0.18f * k, born = Time.time, k = k });
            if (lights.Count > 24) lights.RemoveAt(0);
        }

        /// <summary>Every frame of the walk-out: the fires flicker on the stone, the flashes fade.</summary>
        public static void Tick(float burn)
        {
            float t = Time.time;
            lights.Sort((a, b) => Strength(b, t).CompareTo(Strength(a, t)));
            int n = Mathf.Min(8, lights.Count);
            for (int i = 0; i < 8; i++)
            {
                if (i < n)
                {
                    var l = lights[i];
                    float s = Strength(l, t);
                    lp[i] = new Vector4(l.at.x, l.at.y, l.at.z, l.reach * (1f + 0.5f * Mathf.Exp(-(t - l.born) * 4f)));
                    lc[i] = new Vector4(1f * s, 0.5f * s, 0.16f * s, 0f);
                }
                else { lp[i] = Vector4.zero; lc[i] = Vector4.zero; }
            }
            Shader.SetGlobalVectorArray(FireLightsId, lp);
            Shader.SetGlobalVectorArray(FireColorsId, lc);
            Shader.SetGlobalFloat(FireCountId, n);
            flashGlobal *= Mathf.Exp(-Time.deltaTime * 7f);
            Shader.SetGlobalVector(FlashId, new Vector4(1f, 0.6f, 0.3f, 0f) * flashGlobal);
            Shader.SetGlobalFloat(BurnId, burn);
        }

        static float Strength(Light l, float t)
        {
            float age = t - l.born;
            float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(t * 6f + l.at.x, l.at.z) + 0.1f * Mathf.Sin(t * 23f + l.at.y);
            return l.flash * Mathf.Exp(-age * 2.8f) + l.steady * Mathf.Clamp01(age * 3f) * flicker;
        }

        /// <summary>Ash and embers drifting round the camera, wherever it goes (after the first blast).</summary>
        public static ParticleSystem Drift(Transform follow)
        {
            Init(0f);
            var ps = Sys("Drift", follow.position, add, null, 500);
            var main = ps.main;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = R(4f, 8f);
            main.startSize = R(0.02f, 0.07f);
            main.startSpeed = R(0.1f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.35f, 0.9f), new Color(0.6f, 0.58f, 0.55f, 0.7f));
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10f, 6f, 8f);
            Vel(ps, R(0.3f, 1.2f), R(-0.4f, 0.3f), R(-1.2f, -0.3f));
            var nz = ps.noise; nz.enabled = true; nz.strength = 0.6f; nz.frequency = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0f }, new[] { 0f, 0.2f, 1f });
            ps.Play();
            // and the smoke reaching the bridge: a thin haze drifting past
            var hz = Sys("Haze", follow.position, smokeMat, ps.transform, 60);
            var hm = hz.main;
            hm.loop = true; hm.duration = 5f;
            hm.startLifetime = R(8f, 12f);
            hm.startSize = R(6f, 12f);
            hm.startSpeed = 0f;
            hm.startRotation = R(0f, 6.28f);
            hm.startColor = new Color(0.3f, 0.28f, 0.26f, 0.22f);
            var he = hz.emission; he.rateOverTime = 3f;
            var hs = hz.shape; hs.enabled = true; hs.shapeType = ParticleSystemShapeType.Box; hs.scale = new Vector3(24f, 6f, 20f);
            Vel(hz, R(0.4f, 1.0f), R(0.1f, 0.4f), R(0f, 0f));
            var hc = hz.colorOverLifetime; hc.enabled = true;
            hc.color = Grad(new[] { Color.white, Color.white }, new[] { 0f, 1f }, new[] { 0f, 1f, 0f }, new[] { 0f, 0.4f, 1f });
            Flipbook(hz, 8, true);
            hz.Play();
            return ps;
        }

        /// <summary>A small steady glow (a lantern still burning at dawn).</summary>
        public static Transform Glow(Vector3 at, float size, float a)
        {
            Init(0f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "LanternGlow";
            q.transform.position = at;
            q.transform.rotation = Quaternion.identity;
            q.transform.localScale = Vector3.one * size;
            var m = new Material(add);
            m.SetColor("_TintColor", new Color(0.5f, 0.42f, 0.3f, a));
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        public static void Clear()
        {
            Fires.Clear();
            lights.Clear();
            flashGlobal = 0f;
            Shader.SetGlobalFloat(FireCountId, 0f);
            Shader.SetGlobalFloat(BurnId, 0f);
            Shader.SetGlobalVector(FlashId, Vector4.zero);
        }
    }
}
