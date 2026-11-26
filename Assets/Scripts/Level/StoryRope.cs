using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A real rope (chapter 1, the chair): one continuous line, not a heap of pieces. While he's
    /// tied it holds its shape on the chair (every point pinned to the chair's own space, so it rocks and
    /// tips with it). Freed, it's a chain of points under gravity (Verlet: each point remembers where it
    /// was; fixed lengths between neighbours, solved a dozen times a frame), lying on the floor, sliding
    /// round his body and the seat. Drawn as a twisted three-strand line (a lay that repeats every three
    /// diameters, the grooves dark, the strands round), thinning where it's failing, and fibres springing
    /// out of a frayed end. It fails like rope: strand by strand, then the rest parts all at once and the
    /// two ends whip back (StoryIntro.ChairScene).</summary>
    public class StoryRope : MonoBehaviour
    {
        public Transform rig;                  // the chair: the bound shape lives in its space
        public float radius = 0.011f;
        Vector3[] p, q, rest;
        bool[] pin;                            // held to the chair (bound: all; free: the knots, the leg lashings)
        float seg;
        LineRenderer lr;
        bool free;
        float neckAt = -1f, neck;              // where it's failing (0..1 along it) and how much it has thinned
        readonly List<Fiber> tufts = new List<Fiber>();

        /// <summary>The bodies a free rope slides round (the hero's chest and hips, the seat, the back).</summary>
        public static readonly List<System.Func<(Vector3 c, float r)>> Bodies = new List<System.Func<(Vector3 c, float r)>>();

        static Material ropeMat, fiberMat;

        // ---------------------------------------------------------------- making one

        /// <summary>A rope along `pathLocal` (points in the chair's space), resampled to even steps.</summary>
        public static StoryRope Make(string name, Transform rig, List<Vector3> pathLocal, float radius)
        {
            var go = new GameObject(name);
            var r = go.AddComponent<StoryRope>();
            r.rig = rig;
            r.radius = radius;
            r.Build(pathLocal);
            return r;
        }

        void Build(List<Vector3> path)
        {
            float len = 0f;
            for (int i = 1; i < path.Count; i++) len += Vector3.Distance(path[i - 1], path[i]);
            int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.024f) + 1);
            seg = len / (n - 1);
            rest = new Vector3[n];
            // walk the path at even steps
            int k = 0; float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                float want = i * seg;
                while (k < path.Count - 2 && acc + Vector3.Distance(path[k], path[k + 1]) < want) { acc += Vector3.Distance(path[k], path[k + 1]); k++; }
                float sl = Vector3.Distance(path[k], path[k + 1]);
                rest[i] = Vector3.Lerp(path[k], path[k + 1], sl > 1e-5f ? Mathf.Clamp01((want - acc) / sl) : 0f);
            }
            p = new Vector3[n]; q = new Vector3[n]; pin = new bool[n];
            for (int i = 0; i < n; i++) { pin[i] = true; p[i] = q[i] = rig.TransformPoint(rest[i]); }
            lr = gameObject.AddComponent<LineRenderer>();
            lr.sharedMaterial = RopeMat();
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = new Vector2(1f / (radius * 6f), 1f);          // one lay every three diameters
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.widthMultiplier = radius * 2f;
            lr.positionCount = n;
            lr.SetPositions(p);
        }

        /// <summary>Points from `from` to `to` (inclusive, 0..1 along it) stay held to the chair when it's freed.</summary>
        public void Keep(float from, float to)
        {
            int a = Mathf.RoundToInt(from * (p.Length - 1)), b = Mathf.RoundToInt(to * (p.Length - 1));
            for (int i = a; i <= b && i < p.Length; i++) keep.Add(i);
        }
        readonly HashSet<int> keep = new HashSet<int>();

        // ---------------------------------------------------------------- failing

        /// <summary>The point most in view of a camera at `eye`, in the middle part of the rope (where it fails).</summary>
        public float FailPoint(Vector3 eye, float from = 0.3f, float to = 0.7f)
        {
            int best = p.Length / 2; float bd = float.MaxValue;
            for (int i = Mathf.RoundToInt(from * (p.Length - 1)); i <= Mathf.RoundToInt(to * (p.Length - 1)); i++)
            {
                float d = (p[i] - eye).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best / (float)(p.Length - 1);
        }

        public Vector3 PointAt(float u) => p[Mathf.Clamp(Mathf.RoundToInt(u * (p.Length - 1)), 0, p.Length - 1)];

        /// <summary>A strand gives at `u`: the rope thins there and a little tuft of fibres springs out.</summary>
        public void StrandGives(float u, int strands)
        {
            neckAt = u;
            neck = Mathf.Clamp01(strands / 3f);
            int i = Mathf.Clamp(Mathf.RoundToInt(u * (p.Length - 1)), 1, p.Length - 2);
            for (int f = 0; f < 5; f++) tufts.Add(Fiber.Make(i, Random.insideUnitSphere, 0.02f + 0.02f * Random.value, true));
            for (int f = 0; f < 6; f++) Fiber.Loose(p[i], FiberMat(), Random.onUnitSphere * 0.4f);
            UpdateWidth();
        }

        void UpdateWidth()
        {
            if (neckAt < 0f) { lr.widthCurve = AnimationCurve.Constant(0f, 1f, 1f); return; }
            float w = 0.04f, low = 1f - 0.55f * neck;
            var c = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(Mathf.Max(0.001f, neckAt - w), 1f), new Keyframe(neckAt, low),
                                       new Keyframe(Mathf.Min(0.999f, neckAt + w), 1f), new Keyframe(1f, 1f));
            lr.widthCurve = c;
        }

        /// <summary>The last strand goes: the rope parts at `u` into two, both freed, the ends whipping back
        /// from the break. Returns the far half.</summary>
        public StoryRope Part(float u, float whip)
        {
            int k = Mathf.Clamp(Mathf.RoundToInt(u * (p.Length - 1)), 2, p.Length - 3);
            var other = new GameObject(name + "_b").AddComponent<StoryRope>();
            other.rig = rig; other.radius = radius; other.seg = seg;
            int m = p.Length - k;
            other.p = new Vector3[m]; other.q = new Vector3[m]; other.rest = new Vector3[m]; other.pin = new bool[m];
            System.Array.Copy(p, k, other.p, 0, m); System.Array.Copy(q, k, other.q, 0, m);
            System.Array.Copy(rest, k, other.rest, 0, m); System.Array.Copy(pin, k, other.pin, 0, m);
            foreach (var i in keep) if (i >= k) other.keep.Add(i - k);
            other.lr = other.gameObject.AddComponent<LineRenderer>();
            CopyLook(lr, other.lr);
            System.Array.Resize(ref p, k); System.Array.Resize(ref q, k); System.Array.Resize(ref rest, k); System.Array.Resize(ref pin, k);
            keep.RemoveWhere(i => i >= k);
            lr.positionCount = p.Length; other.lr.positionCount = other.p.Length;
            neckAt = -1f; UpdateWidth();
            Release(); other.Release();
            // the stored tension goes: each end flies back along itself, away from the break
            Whip(this, p.Length - 1, -1, whip);
            Whip(other, 0, 1, whip);
            // frayed ends: fibres splayed out of both
            for (int f = 0; f < 7; f++) tufts.Add(Fiber.Make(p.Length - 1, Random.onUnitSphere, 0.025f + 0.025f * Random.value, false));
            for (int f = 0; f < 7; f++) other.tufts.Add(Fiber.Make(0, Random.onUnitSphere, 0.025f + 0.025f * Random.value, false));
            foreach (var t in tufts) if (t.lr != null && t.at >= p.Length) t.lr.enabled = false;
            for (int f = 0; f < 14; f++) Fiber.Loose(PointAtIndex(p.Length - 1), FiberMat(), Random.onUnitSphere * 0.9f + Vector3.up * 0.3f);
            return other;
        }

        Vector3 PointAtIndex(int i) => p[Mathf.Clamp(i, 0, p.Length - 1)];

        static void Whip(StoryRope r, int end, int inward, float speed)
        {
            // the last ~25 cm recoil, fastest at the very end
            int n = Mathf.Min(10, r.p.Length - 1);
            var dir = (r.p[end] - r.p[end + inward * Mathf.Max(1, n / 2)]).normalized;      // outward along the rope
            for (int j = 0; j <= n; j++)
            {
                int i = end + inward * j;
                float k = 1f - j / (float)(n + 1);
                // move "where it was" ahead, so it carries on away from the break (recoil back along itself)
                r.q[i] = r.p[i] + dir * speed * k * Step;                   // `speed` m/s, back along the rope
            }
        }

        /// <summary>Let it go: only the kept points stay held to the chair.</summary>
        public void Release()
        {
            free = true;
            for (int i = 0; i < pin.Length; i++) pin[i] = keep.Contains(i);
        }

        /// <summary>Freed and settled at once (a skip past the scene).</summary>
        public void Settle(int steps = 240)
        {
            if (!free) Release();
            for (int s = 0; s < steps * 2; s++) Simulate(Step);
            Render();
        }

        static void CopyLook(LineRenderer a, LineRenderer b)
        {
            b.sharedMaterial = a.sharedMaterial; b.alignment = a.alignment; b.textureMode = a.textureMode; b.textureScale = a.textureScale;
            b.numCapVertices = a.numCapVertices; b.numCornerVertices = a.numCornerVertices;
            b.shadowCastingMode = a.shadowCastingMode; b.receiveShadows = false; b.widthMultiplier = a.widthMultiplier;
        }

        // ---------------------------------------------------------------- every frame

        const float Step = 1f / 120f;         // fixed steps of game time: slow motion slows the rope too
        float acc;

        void LateUpdate()
        {
            if (rig == null) return;
            if (!free)
                for (int i = 0; i < p.Length; i++) p[i] = q[i] = rig.TransformPoint(rest[i]);
            else
            {
                acc = Mathf.Min(acc + Time.deltaTime, Step * 6f);
                while (acc >= Step) { Simulate(Step); acc -= Step; }
            }
            Render();
        }

        void Simulate(float dt)
        {
            float floor = 0.012f + radius;
            var g = Physics.gravity * dt * dt;
            for (int i = 0; i < p.Length; i++)
            {
                if (pin[i]) { q[i] = p[i]; p[i] = rig.TransformPoint(rest[i]); continue; }
                var v = (p[i] - q[i]) * 0.985f;                              // a little air drag
                q[i] = p[i];
                p[i] += v + g;
            }
            for (int it = 0; it < 14; it++)
            {
                for (int i = 0; i < p.Length - 1; i++)
                {
                    var d = p[i + 1] - p[i];
                    float l = d.magnitude;
                    if (l < 1e-6f) continue;
                    var c = d * (1f - seg / l);
                    bool a = pin[i], b = pin[i + 1];
                    if (a && b) continue;
                    if (a) p[i + 1] -= c;
                    else if (b) p[i] += c;
                    else { p[i] += c * 0.5f; p[i + 1] -= c * 0.5f; }
                }
                // round the bodies, onto the floor
                for (int i = 0; i < p.Length; i++)
                {
                    if (pin[i]) continue;
                    foreach (var body in Bodies)
                    {
                        var (bc, br) = body();
                        var d = p[i] - bc;
                        float rr = br + radius;
                        if (d.sqrMagnitude < rr * rr && d.sqrMagnitude > 1e-8f) p[i] = bc + d.normalized * rr;
                    }
                    if (p[i].y < floor)
                    {
                        p[i].y = floor;
                        // friction: it lies where it lands, sliding only a little
                        q[i].x = Mathf.Lerp(q[i].x, p[i].x, 0.35f);
                        q[i].z = Mathf.Lerp(q[i].z, p[i].z, 0.35f);
                        q[i].y = p[i].y;
                    }
                }
            }
        }

        void Render()
        {
            if (lr == null) return;
            lr.positionCount = p.Length;
            lr.SetPositions(p);
            for (int t = tufts.Count - 1; t >= 0; t--)
            {
                var f = tufts[t];
                if (f.lr == null) { tufts.RemoveAt(t); continue; }
                if (f.at >= p.Length) continue;
                int i = f.at;
                var tan = (p[Mathf.Min(i + 1, p.Length - 1)] - p[Mathf.Max(i - 1, 0)]).normalized;
                var outward = f.mid ? Vector3.Cross(tan, f.dir).normalized : (i == 0 ? -tan : tan);
                var spread = (outward + (f.dir - Vector3.Dot(f.dir, outward) * outward) * (f.mid ? 0.4f : 0.9f)).normalized;
                var a = p[i] + (f.mid ? outward * radius * 0.6f : Vector3.zero);
                var bend = Vector3.down * f.len * 0.25f;                      // a fibre droops a little
                f.lr.SetPosition(0, a);
                f.lr.SetPosition(1, a + spread * f.len * 0.55f + bend * 0.3f);
                f.lr.SetPosition(2, a + spread * f.len + bend);
            }
        }

        // ---------------------------------------------------------------- the look

        static Material RopeMat()
        {
            if (ropeMat != null) return ropeMat;
            ropeMat = new Material(Shader.Find("Unlit/Texture")) { mainTexture = RopeTexture(64, 32) };
            return ropeMat;
        }

        static Material FiberMat()
        {
            if (fiberMat != null) return fiberMat;
            fiberMat = new Material(Shader.Find("Sprites/Default"));
            return fiberMat;
        }

        /// <summary>A three-strand lay, seen from the side: three round strands spiralling diagonally,
        /// dark grooves between them, the whole rope lit round (bright along the middle, dark at its
        /// edges), and fibre grain along each strand. Hemp, aged grey to suit the warehouse.</summary>
        static Texture2D RopeTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var rnd = new System.Random(7);
            var grain = new float[w * 2];
            for (int i = 0; i < grain.Length; i++) grain[i] = (float)rnd.NextDouble();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = (y + 0.5f) / h;
                    float across = v * 2f - 1f;
                    float round = Mathf.Sqrt(Mathf.Max(0f, 1f - across * across));      // a cylinder, lit from the front
                    float band = Mathf.Repeat((u + v * 0.9f) * 3f, 1f);                    // three strands, laid diagonally
                    float strand = Mathf.Sin(band * Mathf.PI);                               // each one round
                    float groove = Mathf.SmoothStep(0f, 0.18f, strand);
                    float fibre = 0.9f + 0.1f * grain[(x * 3 + y * 7) % grain.Length] * Mathf.Sin((band * 14f + v * 3f) * Mathf.PI);
                    float l = (0.25f + 0.75f * round) * (0.45f + 0.55f * strand * groove) * fibre;
                    var c = new Color(0.72f * l, 0.68f * l, 0.6f * l, 1f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply(true);
            return tex;
        }

        // ---------------------------------------------------------------- fibres

        /// <summary>A fibre: either part of a tuft on the rope (follows its point) or loose in the air.</summary>
        class Fiber
        {
            public LineRenderer lr; public int at; public Vector3 dir; public float len; public bool mid;

            public static Fiber Make(int at, Vector3 dir, float len, bool mid)
            {
                var go = new GameObject("Fibre");
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = FiberMat();
                lr.positionCount = 3;
                lr.widthMultiplier = 0.0022f;
                lr.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
                var col = new Color(0.78f, 0.74f, 0.66f, 1f);
                lr.startColor = col; lr.endColor = new Color(col.r, col.g, col.b, 0.6f);
                lr.alignment = LineAlignment.View;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return new Fiber { lr = lr, at = at, dir = dir.normalized, len = len, mid = mid };
            }

            /// <summary>A loose fibre or speck of fibre dust: drifts, turns, settles, fades. Never shiny.</summary>
            public static void Loose(Vector3 at, Material mat, Vector3 vel)
            {
                var go = new GameObject("FibreDust");
                go.transform.position = at;
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = mat;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                lr.widthMultiplier = 0.0018f;
                float len = 0.006f + Random.value * 0.016f;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, Random.onUnitSphere * len);
                var col = new Color(0.74f, 0.7f, 0.62f, 0.9f);
                lr.startColor = lr.endColor = col;
                lr.alignment = LineAlignment.View;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.AddComponent<FibreDrift>().vel = vel;
            }
        }
    }

    /// <summary>One bit of fibre in the air: light, so the air holds it; it tumbles, sinks, lies on the floor, fades.</summary>
    public class FibreDrift : MonoBehaviour
    {
        public Vector3 vel;
        float life, spin;
        LineRenderer lr;

        void Start() { lr = GetComponent<LineRenderer>(); spin = Random.Range(-360f, 360f); life = 0f; }

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            vel += Vector3.down * 1.2f * dt;                 // it barely falls
            vel *= Mathf.Exp(-3.5f * dt);                     // the air takes it
            vel += new Vector3(Mathf.Sin(life * 3.1f + spin), 0f, Mathf.Cos(life * 2.3f)) * 0.08f * dt;
            var p = transform.position + vel * dt;
            if (p.y < 0.02f) { p.y = 0.02f; vel = Vector3.zero; }
            transform.position = p;
            if (p.y > 0.021f) transform.Rotate(Vector3.one, spin * dt * 0.3f, Space.Self);
            float a = Mathf.Clamp01(1f - (life - 2.5f) / 1.5f);
            if (lr != null) { var c = lr.startColor; c.a = 0.9f * a; lr.startColor = lr.endColor = c; }
            if (life > 4f) Destroy(gameObject);
        }
    }
}
