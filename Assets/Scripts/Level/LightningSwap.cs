using UnityEngine;

namespace ProjectInk
{
    /// <summary>Lightning that really lights the place: the set has a second bake, lit by a strike
    /// (the windows, the roof crack and the door blazing cold white). A strike swaps the set's
    /// materials to that bake in a film flicker: on, off, on, gone. Also draws a forked bolt.</summary>
    public class LightningSwap : MonoBehaviour
    {
        public static LightningSwap Instance { get; private set; }

        public Renderer[] renderers = new Renderer[0];
        public Material[] normal = new Material[0];
        public Material[] flash = new Material[0];
        /// <summary>The dawn bake for each renderer (null: it has none); and the parts that exist only
        /// in the dawn (the way out the front: the walk-out), hidden until then.</summary>
        public Material[] dawn = new Material[0];
        public Renderer[] dawnOnly = new Renderer[0];

        /// <summary>The morning after: the whole set in its dawn bake, the way out shown (the walk-out).</summary>
        public static bool Dawn;
        int mode = -1;

        float struckAt = -10f, length;
        bool lit;
        /// <summary>The strike's light is up this frame (the blackout lets it through).</summary>
        public bool Lit => lit;
        LineRenderer[] bolt;
        float boltUntil;

        void Awake() { Instance = this; Dawn = false; Held = false; BlastFx.Clear(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Hold the strike's bake on (the dawn after the storm: the set in its brightest light).</summary>
        public static bool Held;

        /// <summary>A strike: the flash pattern lasts `seconds` (real time).</summary>
        public static void Strike(float seconds = 0.38f)
        {
            if (Instance == null) return;
            Instance.struckAt = Time.unscaledTime;
            Instance.length = seconds;
        }

        void Update()
        {
            float u = (Time.unscaledTime - struckAt) / Mathf.Max(0.05f, length);
            // on, off, on, then fading out: the stutter of a real strike
            bool on = Held || (u >= 0f && u < 1f && !(u > 0.16f && u < 0.3f) && !(u > 0.62f && u < 0.7f));
            int want = Dawn ? 2 : on ? 1 : 0;
            lit = on && !Dawn;
            if (want != mode)
            {
                mode = want;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    var m = want == 2 && i < dawn.Length && dawn[i] != null ? dawn[i] : want == 1 ? flash[i] : normal[i];
                    var arr = renderers[i].sharedMaterials;
                    for (int k = 0; k < arr.Length; k++) arr[k] = m;
                    renderers[i].sharedMaterials = arr;
                }
                foreach (var r in dawnOnly) if (r != null) r.enabled = Dawn;
            }
            if (bolt != null)
            {
                bool show = Time.unscaledTime < boltUntil && on;
                foreach (var b in bolt) if (b != null) b.enabled = show;
            }
        }

        /// <summary>A forked bolt between two points in the sky, shown with the next strike.</summary>
        public void Bolt(Vector3 top, Vector3 bottom, float width)
        {
            var mat = InkFx.Instance != null ? InkFx.Instance.lineMaterial : null;
            if (mat == null) return;
            if (bolt == null)
            {
                bolt = new LineRenderer[3];
                for (int i = 0; i < bolt.Length; i++)
                {
                    var g = new GameObject("Bolt" + i);
                    g.transform.SetParent(transform, false);
                    var lr = g.AddComponent<LineRenderer>();
                    lr.sharedMaterial = mat;
                    lr.useWorldSpace = true;
                    lr.startColor = lr.endColor = new Color(0.9f, 0.94f, 1f, 1f);
                    lr.numCapVertices = 1;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bolt[i] = lr;
                }
            }
            var main = Jagged(top, bottom, 14, (bottom - top).magnitude * 0.06f);
            Set(bolt[0], main, width);
            for (int i = 1; i < bolt.Length; i++)
            {
                int from = Random.Range(3, main.Length - 4);
                var dir = (bottom - top) * Random.Range(0.25f, 0.45f) + new Vector3(Random.Range(-1f, 1f), 0f, 0f) * (bottom - top).magnitude * 0.3f;
                Set(bolt[i], Jagged(main[from], main[from] + dir, 7, dir.magnitude * 0.08f), width * 0.5f);
            }
            boltUntil = Time.unscaledTime + 0.5f;
        }

        static Vector3[] Jagged(Vector3 a, Vector3 b, int n, float jitter)
        {
            var p = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                p[i] = Vector3.Lerp(a, b, i / (float)(n - 1));
                if (i > 0 && i < n - 1) p[i] += new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), 0f) * jitter;
            }
            return p;
        }

        static void Set(LineRenderer lr, Vector3[] pts, float w)
        {
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, w), new Keyframe(1f, w * 0.3f));
        }
    }
}
