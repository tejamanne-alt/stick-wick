using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A pallet rack in the racking aisle. Fling a man through it (knocked flying, low,
    /// fast) and the whole frame rattles: a few beats later the stock on its decks shakes loose and
    /// boxes rain down into the bay, flattening anyone standing underneath. Each rack has stock
    /// for a few spills, and needs a moment to settle between them.</summary>
    public class RackSpill : MonoBehaviour
    {
        public static readonly List<RackSpill> All = new List<RackSpill>();

        [Tooltip("The rack's ends along the lane.")]
        public float x0 = 27f, x1 = 31f;
        [Tooltip("Heights of the decks the stock sits on.")]
        public float[] decks = { 2.2f, 4.4f };
        [Tooltip("How far back the stock sits (lane depth); the boxes tumble forward into the lane.")]
        public float stockZ = 0.6f;
        public int spills = 3;

        int left;
        float readyAt;
        readonly Dictionary<Enemy, float> lastX = new Dictionary<Enemy, float>();
        readonly List<Box> boxes = new List<Box>();
        static Material cardMat, tapeMat, wrapMat;

        class Box
        {
            public Transform tr;
            public Vector3 vel, spin;
            public float delay, size, restAt = -1f, floor;
            public bool landed, hit;
        }

        void Awake() { All.Add(this); left = spills; }
        void OnDestroy() { All.Remove(this); Clear(); }

        void Update()
        {
            Watch();
            Fall(Time.deltaTime);
        }

        /// <summary>A man knocked flying into the bay, low enough to hit the frame.</summary>
        void Watch()
        {
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                float x = e.transform.position.x;
                bool had = lastX.TryGetValue(e, out var px);
                lastX[e] = x;
                if (!had || left <= 0 || Time.time < readyAt) continue;
                if (!e.IsDown || Mathf.Abs(e.Velocity.x) < 4f || e.transform.position.y > 1.6f) continue;
                bool enters = (px < x0 && x >= x0) || (px > x1 && x <= x1);
                bool leaves = (px <= x1 && x > x1) || (px >= x0 && x < x0);
                if (!enters && !leaves) continue;
                Shake(enters ? x : Mathf.Clamp(x, x0, x1), e.Velocity.x > 0f ? 1 : -1);
            }
        }

        /// <summary>The frame takes the hit: a clang, a shudder, then the stock comes down.</summary>
        public void Shake(float atX, int dir)
        {
            if (left <= 0) return;
            left--;
            readyAt = Time.time + 2.5f;
            var at = new Vector3(Mathf.Clamp(atX, x0, x1), 1.2f, 0f);
            Sound.Play(Sfx.MetalClang, 1f, at, 0.7f);
            Sound.Play(Sfx.WallSplat, 0.7f, at);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.16f, 0.25f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(4);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(60, "RACK SLAM!");
            Materials();
            int n = Random.Range(4, 7);
            for (int i = 0; i < n; i++)
            {
                float size = Random.Range(0.34f, 0.56f);
                float deck = decks[Random.Range(0, decks.Length)];
                float x = Mathf.Clamp(atX + dir * Random.Range(-0.6f, 1.6f), x0 + 0.3f, x1 - 0.3f);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "SpillBox";
                go.transform.SetParent(transform, true);
                go.transform.position = new Vector3(x, deck + size * 0.5f, stockZ);
                go.transform.localScale = new Vector3(size, size * Random.Range(0.75f, 1f), size);
                bool wrapped = Random.value < 0.25f;
                go.GetComponent<Renderer>().sharedMaterial = wrapped ? wrapMat : cardMat;
                if (!wrapped)
                {
                    // the tape across the top and down the front: reads as a carton
                    var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(tape.GetComponent<Collider>());
                    tape.transform.SetParent(go.transform, false);
                    tape.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                    tape.transform.localScale = new Vector3(0.16f, 1.02f, 1.02f);
                    tape.GetComponent<Renderer>().sharedMaterial = tapeMat;
                }
                go.SetActive(false);
                boxes.Add(new Box
                {
                    tr = go.transform,
                    size = size,
                    delay = 0.12f + i * 0.09f + Random.Range(0f, 0.12f),
                    vel = new Vector3(dir * Random.Range(-0.4f, 1.2f), Random.Range(0.4f, 1.6f), -Random.Range(1.2f, 2.2f)),
                    spin = new Vector3(Random.Range(-260f, 260f), Random.Range(-120f, 120f), Random.Range(-320f, 320f)),
                    floor = Fighter.GroundY(new Vector3(x, 1.0f, 0f), 0f),
                });
            }
        }

        void Fall(float dt)
        {
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                var b = boxes[i];
                if (b.tr == null) { boxes.RemoveAt(i); continue; }
                if (b.delay > 0f)
                {
                    b.delay -= dt;
                    if (b.delay <= 0f) { b.tr.gameObject.SetActive(true); Sound.Play(Sfx.Crate, 0.35f, b.tr.position, 1.3f); }
                    continue;
                }
                if (b.landed)
                {
                    // lie there a while, then sink out of the way
                    if (Time.time > b.restAt)
                    {
                        b.tr.localScale = Vector3.MoveTowards(b.tr.localScale, Vector3.zero, dt * 0.8f);
                        if (b.tr.localScale.x <= 0.01f) { Destroy(b.tr.gameObject); boxes.RemoveAt(i); }
                    }
                    continue;
                }
                b.vel.y -= 22f * dt;
                var p = b.tr.position + b.vel * dt;
                if (p.z < 0f) { p.z = 0f; b.vel.z = 0f; }                   // into the lane, no further
                b.tr.position = p;
                b.tr.Rotate(b.spin * dt, Space.World);

                // whoever is underneath takes it on the head
                if (!b.hit && b.vel.y < -2f)
                {
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || !e.Targetable) continue;
                        var ep = e.transform.position;
                        if (Mathf.Abs(ep.x - p.x) > 0.45f + b.size * 0.5f || p.y < ep.y + 0.3f || p.y > ep.y + 1.7f) continue;
                        b.hit = true;
                        int d = b.vel.x >= 0f ? 1 : -1;
                        e.TakeHit(new HitInfo { damage = 16f, direction = d, knockback = 2.5f, knockdown = true, heavy = true });
                        Sound.Play(Sfx.Crate, 1f, p);
                        Sound.Play(Sfx.PunchHeavy, 0.7f, e.Chest);
                        if (InkFx.Instance != null) InkFx.Instance.Splash(p, Vector3.up, 16, new Color(0.42f, 0.42f, 0.42f));
                        if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(e, "INCOMING!", 140);
                        b.vel = new Vector3(-d * 1.5f, 3f, 0f);
                        break;
                    }
                }

                if (p.y - b.size * 0.4f <= b.floor && b.vel.y < 0f)
                {
                    p.y = b.floor + b.size * 0.4f;
                    b.tr.position = p;
                    if (b.vel.y < -4f)
                    {
                        // one bounce, then it stays down
                        b.vel = new Vector3(b.vel.x * 0.4f, -b.vel.y * 0.25f, 0f);
                        b.spin *= 0.4f;
                        Sound.Play(Sfx.Crate, 0.6f, p, 0.9f);
                        if (InkFx.Instance != null) InkFx.Instance.Splash(new Vector3(p.x, b.floor + 0.05f, p.z), Vector3.up, 6, new Color(0.35f, 0.33f, 0.3f));
                    }
                    else
                    {
                        b.landed = true;
                        b.restAt = Time.time + 6f;
                        var e = b.tr.eulerAngles;                                // settle flat on a face
                        b.tr.rotation = Quaternion.Euler(Mathf.Round(e.x / 90f) * 90f, e.y, Mathf.Round(e.z / 90f) * 90f);
                    }
                }
            }
        }

        void Clear()
        {
            foreach (var b in boxes) if (b.tr != null) Destroy(b.tr.gameObject);
            boxes.Clear();
        }

        static void Materials()
        {
            if (cardMat != null) return;
            var sh = Shader.Find("Unlit/Color");
            cardMat = new Material(sh) { color = new Color(0.30f, 0.30f, 0.31f) };
            tapeMat = new Material(sh) { color = new Color(0.64f, 0.64f, 0.64f) };
            wrapMat = new Material(sh) { color = new Color(0.5f, 0.5f, 0.52f) };
        }

        public static void ResetAll()
        {
            foreach (var r in All)
            {
                if (r == null) continue;
                r.Clear();
                r.lastX.Clear();
                r.left = r.spills;
                r.readyAt = 0f;
            }
        }
    }
}
