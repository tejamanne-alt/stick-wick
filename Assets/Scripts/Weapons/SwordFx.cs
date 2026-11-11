using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The katana: a curved, tapered steel blade with a bright cutting edge, a round
    /// guard and a wrapped grip (built as a mesh, replacing the placeholder boxes), plus the
    /// premium slash: a solid swept ribbon between the blade's middle and its tip, sampled
    /// every frame and smoothed with Catmull-Rom, so even the fastest cut draws a clean white
    /// crescent that fades from the tip inwards. In slow motion the ribbon lingers.</summary>
    public class SwordFx : MonoBehaviour
    {
        public const float BladeLength = 1.0f;     // along the mount's +Z, from the guard
        const float RibbonFrom = 0.38f;            // the ribbon spans this fraction of the blade to the tip
        const float Life = 0.11f;                  // seconds a sample stays (scaled time)
        const int Sub = 4;                         // smoothing subdivisions between samples

        public bool Emitting { get; set; }
        /// <summary>World velocity of the blade tip (for aiming slash lines and sparks).</summary>
        public Vector3 TipVelocity { get; private set; }
        public Vector3 Tip => transform.TransformPoint(new Vector3(0f, Curve(1f), 0.07f + BladeLength));

        struct Sample { public Vector3 a, b; public float t; }
        readonly List<Sample> samples = new List<Sample>();
        Mesh ribbon;
        MeshRenderer ribbonRenderer;
        Vector3 lastTip;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> cols = new List<Color>();
        readonly List<int> tris = new List<int>();

        static Mesh bladeMesh, fittingsMesh;

        /// <summary>Swap a weapon mount's placeholder boxes for the katana; `ribbon` adds the slash.</summary>
        public static SwordFx Attach(GameObject mount, Material bladeMat, Material ribbonMat, bool ribbon)
        {
            if (mount == null) return null;
            var fx = mount.GetComponent<SwordFx>();
            if (fx != null) return fx;
            foreach (Transform child in mount.transform)
            {
                var r = child.GetComponent<MeshRenderer>();
                if (r != null) r.enabled = false;                    // the old box blade
                var tr = child.GetComponent<TrailRenderer>();
                if (tr != null) tr.enabled = false;                  // the old thin trail
            }
            fx = mount.AddComponent<SwordFx>();
            fx.Build(bladeMat, ribbon ? ribbonMat : null);
            return fx;
        }

        static float Curve(float u) => 0.045f * u * u;              // a gentle katana curve towards the spine

        void Build(Material bladeMat, Material ribbonMat)
        {
            if (bladeMesh == null) { bladeMesh = BuildBlade(); fittingsMesh = BuildFittings(); }
            Child("Blade", bladeMesh, bladeMat);
            Child("Fittings", fittingsMesh, bladeMat);
            BuildShine();
            if (ribbonMat != null)
            {
                var g = new GameObject("SlashRibbon");               // world-space mesh, so it is not parented
                g.hideFlags = HideFlags.DontSave;
                ribbon = new Mesh { name = "SlashRibbon" };
                ribbon.MarkDynamic();
                g.AddComponent<MeshFilter>().sharedMesh = ribbon;
                ribbonRenderer = g.AddComponent<MeshRenderer>();
                ribbonRenderer.sharedMaterial = ribbonMat;
                ribbonRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ribbonRenderer.receiveShadows = false;
            }
            lastTip = Tip;
        }

        void Child(string name, Mesh mesh, Material mat)
        {
            var g = new GameObject(name);
            g.transform.SetParent(transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Blade cross-section: a flat diamond (spine, two flats, edge). Vertex colours:
        /// steel grey at the spine, bright white at the edge, a pale ridge line between.</summary>
        static Mesh BuildBlade()
        {
            const int segs = 14;
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            Color spine = new Color(0.8f, 0.82f, 0.86f), ridge = new Color(0.97f, 0.98f, 1f), edge = Color.white;   // polished: bright from spine to edge
            for (int i = 0; i <= segs; i++)
            {
                float u = i / (float)segs;
                float z = 0.07f + u * BladeLength;
                float width = Mathf.Lerp(0.052f, 0.034f, u);           // tapers to the tip
                if (u > 0.9f) width *= Mathf.Lerp(1f, 0.05f, (u - 0.9f) / 0.1f);   // the kissaki
                float thick = Mathf.Lerp(0.012f, 0.006f, u);
                float y0 = Curve(u);                                    // spine line
                // ring: spine, ridge (+x), edge, ridge (-x)
                v.Add(new Vector3(0f, y0 + width * 0.5f, z)); c.Add(spine);
                v.Add(new Vector3(thick, y0 + width * 0.05f, z)); c.Add(ridge);
                v.Add(new Vector3(0f, y0 - width * 0.5f, z)); c.Add(edge);
                v.Add(new Vector3(-thick, y0 + width * 0.05f, z)); c.Add(ridge);
            }
            for (int i = 0; i < segs; i++)
                for (int k = 0; k < 4; k++)
                {
                    int a = i * 4 + k, b = i * 4 + (k + 1) % 4, a2 = a + 4, b2 = b + 4;
                    t.Add(a); t.Add(a2); t.Add(b);
                    t.Add(b); t.Add(a2); t.Add(b2);
                }
            var m = new Mesh { name = "KatanaBlade" };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0, true);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Round guard (tsuba), collar and a wrapped grip with diamond bands.</summary>
        static Mesh BuildFittings()
        {
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            Color dark = new Color(0.08f, 0.08f, 0.09f), wrap = new Color(0.22f, 0.22f, 0.24f), gold = new Color(0.72f, 0.70f, 0.66f);
            Cylinder(v, c, t, 0.075f, 0.075f, 0.055f, 0.071f, 12, dark);      // tsuba (flat disc)
            Cylinder(v, c, t, 0.071f, 0.09f, 0.022f, 0.02f, 8, gold);         // habaki collar
            // the grip: alternating dark and wrapped bands
            for (int i = 0; i < 6; i++)
            {
                float z0 = 0.05f - i * 0.045f, z1 = z0 - 0.045f;
                Cylinder(v, c, t, z1, z0, 0.02f + (i % 2) * 0.003f, 0.02f + (i % 2) * 0.003f, 8, i % 2 == 0 ? wrap : dark);
            }
            Cylinder(v, c, t, -0.24f, -0.22f, 0.022f, 0.024f, 8, gold);        // kashira end cap
            var m = new Mesh { name = "KatanaFittings" };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0, true);
            m.RecalculateBounds();
            return m;
        }

        static void Cylinder(List<Vector3> v, List<Color> c, List<int> t, float z0, float z1, float r0, float r1, int n, Color col)
        {
            int start = v.Count;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v.Add(d * r0 + Vector3.forward * z0); c.Add(col * 0.85f);
                v.Add(d * r1 + Vector3.forward * z1); c.Add(col);
            }
            for (int i = 0; i < n; i++)
            {
                int a = start + i * 2, b = start + ((i + 1) % n) * 2;
                t.Add(a); t.Add(a + 1); t.Add(b);
                t.Add(b); t.Add(a + 1); t.Add(b + 1);
            }
            // caps
            int c0 = v.Count; v.Add(Vector3.forward * z0); c.Add(col * 0.7f);
            int c1 = v.Count; v.Add(Vector3.forward * z1); c.Add(col);
            for (int i = 0; i < n; i++)
            {
                int a = start + i * 2, b = start + ((i + 1) % n) * 2;
                t.Add(c0); t.Add(a); t.Add(b);
                t.Add(c1); t.Add(b + 1); t.Add(a + 1);
            }
        }

        void OnDisable()
        {
            samples.Clear();
            if (ribbon != null) ribbon.Clear();
        }

        void OnDestroy() { if (ribbonRenderer != null) Destroy(ribbonRenderer.gameObject); }

        // ------------------------------------------------------------------ the shine
        // A polished blade in the dark: a soft halo along it (additive), and a glint that runs from the
        // guard to the tip and flares there as a star: when it's drawn, when it kills, when it parries
        // perfectly, and now and then on its own while it's out.
        static Material glowMat;
        static Texture2D glowTex;
        Transform halo, star;
        Renderer haloR, starR;
        float glintT = -1f, nextIdleGlint;

        static Texture2D Soft()
        {
            if (glowTex != null) return glowTex;
            const int n = 64;
            glowTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a;
                    float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dx) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(dy)), Mathf.Clamp01(1f - Mathf.Abs(dy) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(dx)));
                    glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a + rays * 0.8f)));
                }
            glowTex.Apply();
            return glowTex;
        }

        void BuildShine()
        {
            if (glowMat == null)
            {
                var sh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
                glowMat = new Material(sh) { mainTexture = Soft() };
                if (glowMat.HasProperty("_TintColor")) glowMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.52f, 0.5f));
            }
            halo = Quad("Halo", new Vector3(0f, 0.01f, 0.07f + BladeLength * 0.5f), new Vector3(0.16f, BladeLength * 1.15f, 1f), out haloR);
            halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            star = Quad("Star", new Vector3(0f, 0f, 0.07f + BladeLength), Vector3.one * 0.0001f, out starR);
            SetAlpha(haloR, 0.16f);
            SetAlpha(starR, 0f);
        }

        Transform Quad(string name, Vector3 at, Vector3 scale, out Renderer r)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(transform, false);
            q.transform.localPosition = at;
            q.transform.localScale = scale;
            r = q.GetComponent<Renderer>();
            r.sharedMaterial = glowMat;
            r.material = new Material(glowMat);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }

        static void SetAlpha(Renderer r, float a)
        {
            if (r == null) return;
            var m = r.material;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.52f, a * 0.5f));
            else m.color = new Color(1f, 1f, 1f, a);
        }

        /// <summary>The glint: a light runs up the blade and flares at the tip.</summary>
        public void Glint() { glintT = 0f; }

        void UpdateShine()
        {
            if (halo == null) return;
            var cam = Camera.main;
            if (cam != null) star.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
            // the halo breathes a little, brighter while it moves
            float speed = TipVelocity.magnitude;
            SetAlpha(haloR, Mathf.Clamp(0.14f + speed * 0.012f, 0.14f, 0.45f));
            if (glintT < 0f && Time.unscaledTime > nextIdleGlint)
            {
                nextIdleGlint = Time.unscaledTime + Random.Range(3.5f, 6f);
                glintT = 0f;
            }
            if (glintT >= 0f)
            {
                glintT += Time.unscaledDeltaTime;
                float u = glintT / 0.32f;                               // up the blade
                float z = 0.07f + Mathf.Clamp01(u) * BladeLength;
                star.localPosition = new Vector3(0f, Curve(Mathf.Clamp01(u)), z);
                float size = u < 1f ? 0.12f + 0.1f * u : Mathf.Lerp(0.42f, 0f, (glintT - 0.32f) / 0.28f);   // the flare at the tip
                star.localScale = Vector3.one * Mathf.Max(0.0001f, size);
                SetAlpha(starR, u < 1f ? 0.7f : Mathf.Lerp(1f, 0f, (glintT - 0.32f) / 0.28f));
                if (glintT > 0.6f) { glintT = -1f; SetAlpha(starR, 0f); star.localScale = Vector3.one * 0.0001f; }
            }
        }

        void LateUpdate()
        {
            UpdateShine();
            var tip = Tip;
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            TipVelocity = (tip - lastTip) / dt;
            lastTip = tip;
            if (ribbon == null) return;

            float now = Time.time;
            if (Emitting)
                samples.Add(new Sample
                {
                    a = transform.TransformPoint(new Vector3(0f, Curve(RibbonFrom), 0.07f + BladeLength * RibbonFrom)),
                    b = tip,
                    t = now
                });
            while (samples.Count > 0 && now - samples[0].t > Life) samples.RemoveAt(0);
            BuildRibbon(now);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
        }

        void BuildRibbon(float now)
        {
            ribbon.Clear();
            int n = samples.Count;
            if (n < 2) return;
            verts.Clear(); cols.Clear(); tris.Clear();
            for (int i = 0; i < n - 1; i++)
            {
                var s0 = samples[Mathf.Max(0, i - 1)];
                var s1 = samples[i];
                var s2 = samples[i + 1];
                var s3 = samples[Mathf.Min(n - 1, i + 2)];
                int steps = i == n - 2 ? Sub + 1 : Sub;
                for (int k = 0; k < steps; k++)
                {
                    float u = k / (float)Sub;
                    var a = CatmullRom(s0.a, s1.a, s2.a, s3.a, u);
                    var b = CatmullRom(s0.b, s1.b, s2.b, s3.b, u);
                    float age = (now - Mathf.Lerp(s1.t, s2.t, u)) / Life;    // 0 new .. 1 gone
                    float fade = Mathf.Clamp01(1f - age);
                    fade *= fade;
                    verts.Add(a); cols.Add(new Color(1f, 1f, 1f, 0.05f * fade));
                    verts.Add(Vector3.Lerp(a, b, 0.72f)); cols.Add(new Color(0.92f, 0.94f, 1f, 0.55f * fade));
                    verts.Add(b); cols.Add(new Color(1f, 1f, 1f, 0.95f * fade));
                }
            }
            int rows = verts.Count / 3;
            for (int r = 0; r < rows - 1; r++)
                for (int k = 0; k < 2; k++)
                {
                    int a = r * 3 + k, b = a + 1, a2 = a + 3, b2 = b + 3;
                    tris.Add(a); tris.Add(a2); tris.Add(b);
                    tris.Add(b); tris.Add(a2); tris.Add(b2);   // the sprite shader draws both faces
                }
            ribbon.SetVertices(verts);
            ribbon.SetColors(cols);
            ribbon.SetTriangles(tris, 0, true);
        }
    }
}
