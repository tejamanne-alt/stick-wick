using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The katana's brushstroke (docs/design/backlog.md step 3): the finger's swipe drawn into the
    /// world as one tapered stroke, wet at the start, thin at the tail, and the blade follows it. It fades in
    /// a breath; a killing cut's stroke stays a beat longer; a finished kata keeps every stroke of it on
    /// screen together for half a second, then they wash away.
    ///
    /// Pale ink, not black: the story's sets are dark, and a stroke is the blade's light as much as ink.</summary>
    public class InkStroke : MonoBehaviour
    {
        static readonly List<InkStroke> Live = new List<InkStroke>();
        static Material mat;
        LineRenderer line;
        float bornAt, holdUntil, fade = 0.35f;
        Color color;

        /// <summary>A stroke along a screen-space line, drawn into the fighting plane (z = planeZ).</summary>
        public static InkStroke Draw(Vector2 screenA, Vector2 screenB, float hold = 0.18f, float planeZ = 0f, Color? tint = null)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var a = ToPlane(cam, screenA, planeZ);
            var b = ToPlane(cam, screenB, planeZ);
            return DrawWorld(a, b, hold, tint ?? new Color(0.93f, 0.93f, 0.95f, 0.95f));
        }

        public static InkStroke DrawWorld(Vector3 a, Vector3 b, float hold, Color c, float width = 0.11f)
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("InkStroke");
            var s = go.AddComponent<InkStroke>();
            s.line = go.AddComponent<LineRenderer>();
            s.line.sharedMaterial = mat;
            s.line.positionCount = 14;
            s.line.numCapVertices = 3;
            s.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.line.receiveShadows = false;
            // a brush: it lands wide, swells, and lifts off thin
            s.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.18f, 1f), new Keyframe(0.7f, 0.7f), new Keyframe(1f, 0.04f));
            s.line.widthMultiplier = width;
            var n = Vector3.Cross((b - a).normalized, Vector3.forward);
            float bow = (b - a).magnitude * 0.06f;                       // the stroke bows a little, as a hand does
            for (int i = 0; i < 14; i++)
            {
                float u = i / 13f;
                s.line.SetPosition(i, Vector3.Lerp(a, b, u) + n * Mathf.Sin(u * Mathf.PI) * bow + Vector3.back * 0.25f);
            }
            s.color = c;
            s.bornAt = Time.unscaledTime;
            s.holdUntil = s.bornAt + hold;
            s.Paint(1f);
            Live.Add(s);
            return s;
        }

        static Vector3 ToPlane(Camera cam, Vector2 screen, float z)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 1e-4f) return ray.origin;
            float t = (z - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * t;
        }

        /// <summary>A killing cut: the stroke stays on him a beat.</summary>
        public void Linger(float seconds) { holdUntil = Mathf.Max(holdUntil, Time.unscaledTime + seconds); }

        /// <summary>A finished kata: every stroke still on screen holds together.</summary>
        public static void HoldAll(float seconds)
        {
            foreach (var s in Live) if (s != null) s.Linger(seconds);
        }

        void Paint(float a)
        {
            var c = color; c.a *= a;
            var tail = c; tail.a *= 0.55f;
            line.startColor = c;
            line.endColor = tail;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            if (t < holdUntil) return;
            float u = (t - holdUntil) / fade;
            if (u >= 1f) { Live.Remove(this); Destroy(gameObject); return; }
            Paint(1f - u);
            line.widthMultiplier *= 1f + Time.unscaledDeltaTime * 0.6f;    // the ink spreads as it washes out
        }
    }

    /// <summary>A swordsman's coming cut, shown before it comes (backlog step 3): a red ghost of the line his
    /// blade will travel, through the point he's aiming at; or, for a thrust, a red point. Faint at first,
    /// it firms up as the wind-up ends. Mirror the line with your own swipe to parry (HeroKatanaV4).</summary>
    public class RedLine : MonoBehaviour
    {
        LineRenderer line;
        static Material mat;
        float shownAt, length;
        Vector3 center;
        Vector2 dir;
        bool point;

        public static RedLine Make(Transform parent)
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("RedLine");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RedLine>();
            r.line = go.AddComponent<LineRenderer>();
            r.line.sharedMaterial = mat;
            r.line.useWorldSpace = true;
            r.line.positionCount = 12;
            r.line.numCapVertices = 2;
            r.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.2f));
            r.line.widthMultiplier = 0.06f;
            r.line.enabled = false;
            return r;
        }

        /// <summary>His cut: through `at` (the point he aims at), along `d` (world x, y; the way the blade moves).</summary>
        public void ShowCut(Vector3 at, Vector2 d, float len = 1.5f)
        {
            center = at; dir = d.normalized; length = len; point = false;
            shownAt = Time.time;
            line.enabled = true;
            line.loop = false;
        }

        /// <summary>His thrust: a red point (a small ring) where the tip will arrive.</summary>
        public void ShowThrust(Vector3 at)
        {
            center = at; point = true;
            shownAt = Time.time;
            line.enabled = true;
            line.loop = true;
        }

        public void Hide() { if (line != null) line.enabled = false; }
        public bool Showing => line != null && line.enabled;
        public bool IsThrust => point;
        /// <summary>The line's direction (world x, y).</summary>
        public Vector2 Dir => dir;

        void LateUpdate()
        {
            if (!line.enabled) return;
            float age = Time.time - shownAt;
            if (age > 1.6f) { line.enabled = false; return; }                // never outlives a wind-up
            float a = Mathf.Clamp01(0.25f + age * 1.6f) * (0.75f + 0.25f * Mathf.Sin(age * 22f));
            var c = new Color(0.95f, 0.07f, 0.04f, a);
            line.startColor = c; line.endColor = c;
            if (point)
            {
                float r = 0.12f + 0.04f * Mathf.Sin(age * 14f);
                for (int i = 0; i < 12; i++)
                {
                    float t = i / 12f * Mathf.PI * 2f;
                    line.SetPosition(i, center + new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r, -0.3f));
                }
                return;
            }
            var d3 = new Vector3(dir.x, dir.y, 0f);
            var n = new Vector3(-dir.y, dir.x, 0f);
            for (int i = 0; i < 12; i++)
            {
                float u = i / 11f;
                line.SetPosition(i, center + d3 * (u - 0.5f) * length + n * Mathf.Sin(u * Mathf.PI) * length * 0.08f + Vector3.back * 0.3f);
            }
        }
    }
}
