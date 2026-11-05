using UnityEngine;

namespace ProjectInk
{
    /// <summary>The hero's signature: two tails of an ink-red scarf, knotted at the back of the
    /// neck, simulated as light cloth (a verlet chain). They hang and sway when he stands, stream
    /// out behind him when he runs, whip round on a dodge and lift in a fall, so his silhouette is
    /// always his, and it shows which way he is moving. Drawn as camera-facing ribbons, just
    /// behind the body. Also: a glint runs across his shades when he beckons.
    /// Scaled time, so the cloth floats in slow motion.</summary>
    public class HeroScarf : MonoBehaviour
    {
        public static readonly Color Red = new Color(0.80f, 0.06f, 0.07f);

        const int N = 10;
        HeroController hero;
        Transform knot;
        Renderer body;
        readonly Tail[] tails = new Tail[2];
        Renderer[] accents;
        Transform[] lenses;
        Vector3 lastAnchor;
        bool started;
        string lastClip;

        class Tail
        {
            public Vector3[] p = new Vector3[N], q = new Vector3[N];
            public float seg, phase, drop;
            public LineRenderer line;
        }

        public static void Attach(HeroController h)
        {
            if (h.GetComponent<HeroScarf>() != null) return;
            h.gameObject.AddComponent<HeroScarf>().hero = h;
        }

        void Start()
        {
            if (hero == null) hero = GetComponent<HeroController>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ScarfKnot") knot = t;
            }
            body = GetComponentInChildren<SkinnedMeshRenderer>();
            var acc = new System.Collections.Generic.List<Renderer>();
            var lens = new System.Collections.Generic.List<Transform>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Scarf") || r.name.StartsWith("Lens") || r.name.StartsWith("Temple") || r.name == "Bridge" || r.name.StartsWith("Glint")) acc.Add(r);
                if (r.name.StartsWith("Lens")) lens.Add(r.transform);
            }
            accents = acc.ToArray();
            lenses = lens.ToArray();
            if (knot == null || hero.trailMaterial == null) { enabled = acc.Count > 0; return; }
            for (int i = 0; i < 2; i++)
            {
                var t = new Tail { seg = i == 0 ? 0.085f : 0.07f, phase = i * 1.7f, drop = i == 0 ? 0f : -0.03f };
                var go = new GameObject("ScarfTail" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = hero.trailMaterial;
                lr.positionCount = N;
                lr.useWorldSpace = true;
                lr.numCapVertices = 2;
                lr.numCornerVertices = 2;
                lr.widthCurve = new AnimationCurve(new Keyframe(0f, i == 0 ? 0.085f : 0.07f), new Keyframe(0.7f, 0.06f), new Keyframe(1f, 0.035f));
                var dark = Red * 0.8f; dark.a = 1f;
                lr.startColor = Red;
                lr.endColor = i == 0 ? Red : dark;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                t.line = lr;
                tails[i] = t;
            }
        }

        Vector3 Anchor(Tail t)
        {
            // from the knot at the back of the neck, a hair behind the body (away from the camera)
            return knot.position + new Vector3(0f, t.drop, 0.05f);
        }

        void ResetChain(Vector3 a)
        {
            foreach (var t in tails)
            {
                if (t == null) continue;
                var back = new Vector3(-hero.Facing, 0f, 0f);
                for (int i = 0; i < N; i++)
                {
                    t.p[i] = t.q[i] = a + (back * 0.4f + Vector3.down) .normalized * t.seg * i;
                }
            }
            lastAnchor = a;
            started = true;
        }

        void LateUpdate()
        {
            // the accessories follow the body: hidden when he is (the opening holds him out of sight)
            bool shown = body == null || body.enabled;
            if (accents != null) foreach (var r in accents) if (r != null && r.enabled != shown) r.enabled = shown;
            Glint();
            if (knot == null || tails[0] == null) return;
            foreach (var t in tails) t.line.enabled = shown;
            if (!shown) { started = false; return; }

            var a0 = Anchor(tails[0]);
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (!started || (a0 - lastAnchor).sqrMagnitude > 1.5f * 1.5f) ResetChain(a0);   // a cut or a teleport
            if (dt <= 0f) { Draw(); return; }
            var vel = (a0 - lastAnchor) / dt;
            lastAnchor = a0;

            float time = Time.time;
            foreach (var t in tails)
            {
                var a = Anchor(t);
                // the air: it streams back against his motion, a steady drift behind him, and a flutter
                var wind = new Vector3(-vel.x * 0.9f - hero.Facing * 1.2f, -vel.y * 0.35f, 0f);
                for (int i = 1; i < N; i++)
                {
                    float k = i / (float)(N - 1);
                    var flutter = new Vector3(0f, Mathf.Sin(time * 9f + t.phase + i * 0.8f) * (0.6f + Mathf.Abs(vel.x) * 0.35f) * k, 0f);
                    var cur = t.p[i];
                    var acc = Vector3.down * 9f + (wind + flutter) * 2.2f * k;
                    var next = cur + (cur - t.q[i]) * 0.9f + acc * dt * dt;
                    t.q[i] = cur;
                    next.z = a.z + 0.01f * i;         // stays behind him
                    t.p[i] = next;
                }
                t.p[0] = a;
                // keep the links their length
                for (int it = 0; it < 4; it++)
                {
                    t.p[0] = a;
                    for (int i = 1; i < N; i++)
                    {
                        var d = t.p[i] - t.p[i - 1];
                        float len = d.magnitude;
                        if (len < 1e-5f) continue;
                        var corr = d * (1f - t.seg / len);
                        if (i == 1) t.p[i] -= corr;
                        else { t.p[i] -= corr * 0.5f; t.p[i - 1] += corr * 0.5f; }
                    }
                }
                // never through the floor
                float floor = hero.transform.position.y + 0.03f;
                for (int i = 1; i < N; i++) if (t.p[i].y < floor) t.p[i].y = floor;
            }
            Draw();
        }

        void Draw()
        {
            foreach (var t in tails) if (t != null) t.line.SetPositions(t.p);
        }

        /// <summary>When he beckons, light runs across his shades.</summary>
        void Glint()
        {
            if (hero == null || hero.body == null || lenses == null || lenses.Length == 0) return;
            string c = hero.body.CurrentName;
            if (c == "taunt" && lastClip != "taunt")
            {
                var cam = Camera.main;
                Transform best = lenses[0];
                if (cam != null)
                    foreach (var l in lenses) if (l != null && (l.position - cam.transform.position).sqrMagnitude < (best.position - cam.transform.position).sqrMagnitude) best = l;
                if (best != null && best.gameObject.activeInHierarchy)
                {
                    StreakFx.Slash(best.position + new Vector3(0f, 0.01f, -0.03f), new Vector3(1f, 0.35f, 0f), 0.32f, 0.03f, 0.35f);
                    StreakFx.Sparks(best.position + new Vector3(0f, 0.02f, -0.03f), Vector3.up, 3, 1.2f);
                    Sound.Play(Sfx.Pickup, 0.25f, best.position, 1.8f);
                }
            }
            lastClip = c;
        }
    }
}
