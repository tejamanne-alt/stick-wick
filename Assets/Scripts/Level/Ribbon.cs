using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Maya's red ribbon, the game's one red, built in detail wherever it appears: a satin strip
    /// (a highlight down its middle, darker turned edges, pale stitching along both sides), a bow of two
    /// teardrop loops that twist as they turn, a gathered knot with its wrinkles, two tails ending in a
    /// V-cut, the tails moving in the wind. Three uses:
    ///   Bow(parent, size)       in her hair (and on the stills' Maya)
    ///   WristBand(forearm)      wound twice round his wrist, a small bow on top (chapter 4 on)
    ///   Lying(position)         dropped in the wet grass by the swings, crumpled, tails trailing</summary>
    public static class Ribbon
    {
        static Material red, hi, lo, stitch;

        static Material Unlit(float r, float g, float b) => new Material(Shader.Find("Unlit/Color")) { color = new Color(r, g, b) };

        static void Mats()
        {
            if (red != null) return;
            red = Unlit(0.74f, 0.05f, 0.07f);
            hi = Unlit(0.93f, 0.22f, 0.24f);
            lo = Unlit(0.42f, 0.02f, 0.04f);
            stitch = Unlit(0.97f, 0.78f, 0.78f);
        }

        static Transform Box(Transform p, Vector3 at, Quaternion rot, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = "R";
            g.transform.SetParent(p, false);
            g.transform.localPosition = at;
            g.transform.localRotation = rot;
            g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        /// <summary>A strip of ribbon through points (local space), its face normal at each point: the body,
        /// the highlight down the middle, a darker underside, and the stitched edges.</summary>
        static void Strip(Transform p, IList<Vector3> pts, IList<Vector3> nrm, float w, float t)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var d = b - a;
                float len = d.magnitude;
                if (len < 1e-5f) continue;
                var n = Vector3.Slerp(nrm[i], nrm[i + 1], 0.5f).normalized;
                var rot = Quaternion.LookRotation(d / len, n);
                var mid = (a + b) * 0.5f;
                var side = rot * Vector3.right;
                Box(p, mid, rot, new Vector3(w, t, len * 1.04f), red);
                Box(p, mid + n * t * 0.55f + side * w * 0.12f, rot, new Vector3(w * 0.22f, t * 0.2f, len * 1.02f), hi);   // the sheen
                Box(p, mid - n * t * 0.55f, rot, new Vector3(w * 0.9f, t * 0.2f, len * 1.02f), lo);                      // the underside
                if (i % 2 == 0)
                {
                    Box(p, mid + n * t * 0.56f + side * w * 0.42f, rot, new Vector3(w * 0.05f, t * 0.15f, len * 0.6f), stitch);
                    Box(p, mid + n * t * 0.56f - side * w * 0.42f, rot, new Vector3(w * 0.05f, t * 0.15f, len * 0.6f), stitch);
                }
            }
        }

        /// <summary>A teardrop loop from the knot outwards along `dir`, in the plane of dir and `side`,
        /// facing `face`, with a quarter-twist as it goes round.</summary>
        static void Loop(Transform p, Vector3 dir, Vector3 side, Vector3 face, float size, float w, float t, float twist)
        {
            var pts = new List<Vector3>(); var nrm = new List<Vector3>();
            int n = 18;
            for (int i = 0; i <= n; i++)
            {
                float th = i / (float)n * Mathf.PI * 2f;
                float out1 = size * (0.5f - 0.5f * Mathf.Cos(th));
                float up = size * 0.36f * Mathf.Sin(th) * (0.6f + 0.4f * (1f - Mathf.Cos(th)) * 0.5f);
                pts.Add(dir * out1 + side * up);
                nrm.Add(Quaternion.AngleAxis(Mathf.Sin(th) * twist, dir) * face);
            }
            Strip(p, pts, nrm, w, t);
        }

        /// <summary>A tail from the knot, hanging along `dir`, a gentle S, a V-cut at the end. Built under its
        /// own pivot so it can move in the wind.</summary>
        static Transform Tail(Transform p, Vector3 dir, Vector3 face, float len, float w, float t, float bend)
        {
            var pivot = new GameObject("Tail").transform;
            pivot.SetParent(p, false);
            var pts = new List<Vector3>(); var nrm = new List<Vector3>();
            var side = Vector3.Cross(face, dir).normalized;
            int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                pts.Add(dir * len * u + side * Mathf.Sin(u * Mathf.PI * 1.4f) * bend * len);
                nrm.Add(Quaternion.AngleAxis(u * 25f, dir) * face);
            }
            Strip(pivot, pts, nrm, w, t);
            // the V-cut: two short angled tips
            var end = pts[n];
            var rot = Quaternion.LookRotation(dir, face);
            Box(pivot, end + dir * w * 0.25f + side * w * 0.25f, rot * Quaternion.Euler(0f, 28f, 0f), new Vector3(w * 0.48f, t, w * 0.55f), red);
            Box(pivot, end + dir * w * 0.25f - side * w * 0.25f, rot * Quaternion.Euler(0f, -28f, 0f), new Vector3(w * 0.48f, t, w * 0.55f), red);
            return pivot;
        }

        /// <summary>The gathered knot: a rounded wrap with its wrinkles.</summary>
        static void Knot(Transform p, Vector3 face, float s, float w)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(p, false);
            g.transform.localScale = new Vector3(w * 1.1f, w * 1.25f, w * 0.75f);
            g.transform.localRotation = Quaternion.LookRotation(face);
            g.GetComponent<Renderer>().sharedMaterial = red;
            var rot = Quaternion.LookRotation(face);
            for (int i = 0; i < 3; i++)
                Box(p, face * w * 0.36f + rot * new Vector3(0f, (i - 1) * w * 0.32f, 0f), rot * Quaternion.Euler(0f, 0f, 8f * (i - 1)), new Vector3(w * 0.9f, w * 0.05f, w * 0.06f), i == 1 ? hi : lo);
        }

        /// <summary>A bow. Its knot at the root, facing local +z, loops left and right along x, tails down.</summary>
        public static Transform Bow(Transform parent, float size, bool flutter = true)
        {
            Mats();
            var root = new GameObject("RibbonBow").transform;
            if (parent != null) root.SetParent(parent, false);
            float w = size * 0.32f, t = size * 0.045f;
            Loop(root, Vector3.right, Vector3.up, Vector3.forward, size, w, t, 22f);
            Loop(root, Vector3.left, Vector3.up, Vector3.forward, size, w, t, -22f);
            Knot(root, Vector3.forward, size, w);
            var f = root.gameObject.AddComponent<RibbonFlutter>();
            f.Add(Tail(root, Quaternion.Euler(0f, 0f, -20f) * Vector3.down, Vector3.forward, size * 0.95f, w * 0.9f, t, 0.12f), flutter ? 9f : 2f);
            f.Add(Tail(root, Quaternion.Euler(0f, 0f, 24f) * Vector3.down, Vector3.forward, size * 0.8f, w * 0.9f, t, -0.1f), flutter ? 11f : 2f);
            return root;
        }

        /// <summary>Round his wrist: wound twice (a flat helix round the forearm bone, whose length runs along
        /// local y), a small bow on the top, its tails moving.</summary>
        public static Transform WristBand(Transform forearm)
        {
            Mats();
            var root = new GameObject("Ribbon").transform;
            root.SetParent(forearm, false);
            root.localPosition = new Vector3(0f, 0.16f, 0f);
            float r = 0.058f, w = 0.022f, t = 0.004f;
            var pts = new List<Vector3>(); var nrm = new List<Vector3>();
            int n = 40;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n, a = u * Mathf.PI * 4f;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                pts.Add(radial * r + Vector3.up * Mathf.Lerp(-0.012f, 0.012f, u));
                nrm.Add(radial);
            }
            Strip(root, pts, nrm, w, t);
            var bow = Bow(root, 0.045f, true);
            bow.localPosition = new Vector3(r + 0.004f, 0f, 0f);
            bow.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            return root;
        }

        /// <summary>Dropped in the wet grass: the bow lying on its back, a little crushed, the tails trailing.</summary>
        public static Transform Lying(Vector3 at, float yaw)
        {
            Mats();
            var root = new GameObject("Ribbon").transform;
            root.position = at;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            var bow = Bow(root, 0.085f, false);
            bow.localPosition = new Vector3(0f, 0.012f, 0f);
            bow.localRotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 12f);
            bow.localScale = new Vector3(1f, 1f, 0.55f);            // crushed flat
            // a long loose end trailing across the grass
            var pts = new List<Vector3>(); var nrm = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float u = i / 10f;
                pts.Add(new Vector3(0.03f + u * 0.24f, 0.006f, -0.05f + Mathf.Sin(u * 5f) * 0.03f));
                nrm.Add(Quaternion.Euler(u * 40f, 0f, 0f) * Vector3.up);
            }
            Strip(root, pts, nrm, 0.026f, 0.004f);
            return root;
        }
    }

    /// <summary>The ribbon's tails, moving in the wind (gently, out of step with each other).</summary>
    public class RibbonFlutter : MonoBehaviour
    {
        /// <summary>How hard the wind blows (1 = a breeze; the blast's hot wind in the walk-out: 3).</summary>
        public static float Wind = 1f;

        readonly List<Transform> tails = new List<Transform>();
        readonly List<float> amps = new List<float>();
        readonly List<Quaternion> rest = new List<Quaternion>();

        public void Add(Transform tail, float amp) { tails.Add(tail); amps.Add(amp); rest.Add(tail.localRotation); }

        void Update()
        {
            float t0 = Time.time;                     // scaled: the slow motion slows them
            float w = Mathf.Max(0f, Wind);
            for (int i = 0; i < tails.Count; i++)
            {
                if (tails[i] == null) continue;
                float f = 1f + 0.6f * (w - 1f);
                float a = amps[i] * w * (0.6f * Mathf.Sin(t0 * 2.3f * f + i * 1.7f) + 0.4f * Mathf.Sin(t0 * 5.1f * f + i));
                float lift = (w - 1f) * 14f;              // blown out and up
                tails[i].localRotation = rest[i] * Quaternion.Euler(a + lift, 0f, a * 0.5f);
            }
        }
    }
}
