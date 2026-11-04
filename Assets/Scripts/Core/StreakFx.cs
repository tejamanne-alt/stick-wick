using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Thin bright streaks, all drawn in one camera-facing world mesh: the anime
    /// "slice" line that flashes across a body on a sword hit (it shoots out to full length
    /// then thins to nothing), steel sparks that fly from clashes, and speed lines.</summary>
    public class StreakFx : MonoBehaviour
    {
        public static StreakFx Instance { get; private set; }

        struct Streak
        {
            public Vector3 pos, vel, dir;
            public float length, width, born, life, grow, drag;
            public Color color;
            public bool real;   // real-time (keeps flashing through slow motion) or scaled
        }
        readonly List<Streak> live = new List<Streak>();
        Mesh mesh;
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        /// <summary>Created on demand with the line material InkFx already has.</summary>
        public static StreakFx Get()
        {
            if (Instance != null) return Instance;
            var fx = InkFx.Instance;
            if (fx == null || fx.lineMaterial == null) return null;
            var g = new GameObject("Streaks");
            var s = g.AddComponent<StreakFx>();
            s.mesh = new Mesh { name = "Streaks" };
            s.mesh.MarkDynamic();
            g.AddComponent<MeshFilter>().sharedMesh = s.mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = fx.lineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Instance = s;
            return s;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static float Now(bool real) => real ? Time.unscaledTime : Time.time;

        /// <summary>The slice: a white line through `center` along `dir`, drawn out and fading.</summary>
        public static void Slash(Vector3 center, Vector3 dir, float length = 2.2f, float width = 0.06f, float life = 0.22f)
        {
            var s = Get(); if (s == null) return;
            dir.z = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.right;
            dir.Normalize();
            s.live.Add(new Streak { pos = center, dir = dir, length = length, width = width, born = Time.unscaledTime, life = life, grow = 0.06f, color = Color.white, real = true });
            // a fainter, wider echo for the glow
            s.live.Add(new Streak { pos = center, dir = dir, length = length * 1.15f, width = width * 3.5f, born = Time.unscaledTime, life = life * 0.8f, grow = 0.06f, color = new Color(1f, 1f, 1f, 0.25f), real = true });
        }

        /// <summary>Sparks spraying from a point, mostly along `dir`.</summary>
        public static void Sparks(Vector3 at, Vector3 dir, int count = 10, float speed = 7f)
        {
            var s = Get(); if (s == null) return;
            dir.z = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.up;
            dir.Normalize();
            for (int i = 0; i < count; i++)
            {
                var d = (dir + (Vector3)Random.insideUnitCircle * 0.9f).normalized;
                float sp = speed * Random.Range(0.5f, 1.3f);
                s.live.Add(new Streak
                {
                    pos = at, vel = d * sp, dir = d, length = Random.Range(0.12f, 0.3f), width = Random.Range(0.012f, 0.025f),
                    born = Time.time, life = Random.Range(0.12f, 0.28f), drag = 6f,
                    color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.75f), Random.value), real = false
                });
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            mesh.Clear();
            if (live.Count == 0 || cam == null) return;
            v.Clear(); c.Clear(); t.Clear();
            Vector3 toCam = -cam.transform.forward;
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var s = live[i];
                float age = (Now(s.real) - s.born) / s.life;
                if (age >= 1f) { live.RemoveAt(i); continue; }
                if (s.vel != Vector3.zero)
                {
                    s.pos += s.vel * dt;
                    s.vel *= Mathf.Exp(-s.drag * dt);
                    s.vel.y -= 9f * dt;
                    if (s.vel.sqrMagnitude > 0.01f) s.dir = s.vel.normalized;
                    live[i] = s;
                }
                // slices shoot out to full length, then thin away; sparks just shrink
                float len = s.grow > 0f ? s.length * Mathf.Clamp01((age * s.life) / s.grow) : s.length * (1f - age);
                float wid = s.width * (1f - age);
                var side = Vector3.Cross(s.dir, toCam).normalized * wid * 0.5f;
                Vector3 a = s.pos - s.dir * len * 0.5f, b = s.pos + s.dir * len * 0.5f;
                if (s.grow <= 0f) { a = s.pos - s.dir * len; b = s.pos; }
                var col = s.color; col.a *= 1f - age * age;
                var tipCol = col; tipCol.a *= 0.15f;
                int k = v.Count;
                float end = s.grow > 0f ? 0.12f : 1f;               // slices taper to needle points
                v.Add(a - side * end); v.Add(a + side * end); v.Add(b + side * end); v.Add(b - side * end);
                // pointed ends: bright in the middle of the slice
                c.Add(tipCol); c.Add(tipCol); c.Add(s.grow > 0f ? tipCol : col); c.Add(s.grow > 0f ? tipCol : col);
                if (s.grow > 0f)
                {
                    // add a bright centre row
                    int m = v.Count;
                    v.Add(s.pos - side * 1.2f); v.Add(s.pos + side * 1.2f);
                    c.Add(col); c.Add(col);
                    t.Add(k); t.Add(m); t.Add(k + 1); t.Add(k + 1); t.Add(m); t.Add(m + 1);
                    t.Add(m); t.Add(k + 3); t.Add(m + 1); t.Add(m + 1); t.Add(k + 3); t.Add(k + 2);
                }
                else { t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k); t.Add(k + 2); t.Add(k + 3); }
            }
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0, true);
        }
    }
}
