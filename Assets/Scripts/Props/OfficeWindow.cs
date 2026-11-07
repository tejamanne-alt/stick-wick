using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The site office's window on the mezzanine, just behind the fight lane. Knock a man
    /// down hard in front of it and he goes backwards through the glass: an explosion of shards
    /// over the deck and down onto the floor below, and he lands among the desks inside, out of
    /// the fight for good (THROUGH THE GLASS!). Jagged teeth stay in the frame. It is whole again
    /// on a new run.</summary>
    public class OfficeWindow : MonoBehaviour
    {
        public static readonly List<OfficeWindow> All = new List<OfficeWindow>();

        [Tooltip("The opening: its ends along the lane, sill and head heights, and the glass's depth.")]
        public float x0 = 46.3f, x1 = 49.2f, sill = 4.3f, head = 5.7f, z = 0.86f;
        [Tooltip("The floor in front of it (the mezzanine deck) and the office floor's depth behind.")]
        public float deckY = 3.4f, insideZ = 2.0f;

        public bool Broken { get; private set; }
        public Renderer[] whole;          // the pane and its glints
        public Renderer[] teeth;          // what is left in the frame once it goes

        readonly Dictionary<Enemy, bool> wasDown = new Dictionary<Enemy, bool>();
        Enemy thrown;
        Vector3 from;
        float throwT, side;
        bool smashed;
        readonly List<Shard> shards = new List<Shard>();
        static Material shardMat;

        class Shard { public Transform tr; public Vector3 vel, spin; public float life; public bool landed; }

        void Awake() { All.Add(this); SetWhole(true); }
        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (thrown != null) UpdateThrow(Time.deltaTime);
            else if (!Broken) Watch();
            UpdateShards(Time.deltaTime);
        }

        /// <summary>A man just knocked down, hard, on the deck in front of the glass.</summary>
        void Watch()
        {
            foreach (var e in Enemy.All)
            {
                if (e == null) continue;
                bool down = e.IsDown;
                bool had = wasDown.TryGetValue(e, out var before);
                wasDown[e] = down;
                if (!had || before || !down || !e.Alive || e.Boss || e.Passive) continue;
                var p = e.transform.position;
                if (p.x < x0 + 0.15f || p.x > x1 - 0.15f || Mathf.Abs(p.y - deckY) > 0.6f) continue;
                if (Mathf.Abs(e.Velocity.x) < 3f) continue;               // only a real blow sends him through
                Throw(e);
                return;
            }
        }

        void Throw(Enemy e)
        {
            thrown = e;
            from = e.transform.position;
            side = e.Velocity.x >= 0f ? 1f : -1f;
            throwT = 0f;
            smashed = false;
            e.Puppet();
            e.PuppetPlay("knockdown", 0.03f, 0.8f);
            Sound.Play(Sfx.WhooshBig, 1f, e.Chest);
            Sound.Play(Sfx.FallScream, 0.9f, e.Chest);
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.35f, 0.5f);
        }

        /// <summary>Up over the sill, back through the glass, down on the office floor.</summary>
        void UpdateThrow(float dt)
        {
            throwT += dt;
            const float hitAt = 0.22f, landAt = 0.55f;
            var glass = new Vector3(Mathf.Clamp(from.x + side * 0.35f, x0 + 0.3f, x1 - 0.3f), sill - 0.55f, z);
            var land = new Vector3(Mathf.Clamp(from.x + side * 0.7f, x0 + 0.2f, x1 - 0.2f), deckY, insideZ);
            Vector3 p;
            if (throwT < hitAt)
            {
                float k = throwT / hitAt;
                p = Vector3.Lerp(from, glass, k) + Vector3.up * Mathf.Sin(k * Mathf.PI * 0.5f) * 0.25f;
            }
            else
            {
                if (!smashed) { smashed = true; Shatter(new Vector3(glass.x, sill + 0.55f, z), side); }
                float k = Mathf.Clamp01((throwT - hitAt) / (landAt - hitAt));
                p = Vector3.Lerp(glass, land, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
            }
            if (thrown != null) thrown.PuppetPose(p);
            if (throwT >= landAt && thrown != null)
            {
                var e = thrown;
                thrown = null;
                Sound.Play(Sfx.BodyFall, 1f, e.transform.position);
                Sound.Play(Sfx.Crate, 0.6f, e.transform.position, 0.8f);         // the desk goes over
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.1f, 0.15f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(250, "THROUGH THE GLASS!");
                e.PuppetDie();
            }
        }

        /// <summary>The pane bursts: shards out over the deck and the floor below, teeth left in the frame.</summary>
        public void Shatter(Vector3 at, float dir)
        {
            if (Broken) return;
            Broken = true;
            SetWhole(false);
            Sound.Play(Sfx.Glass, 1f, at);
            Sound.Play(Sfx.Glass, 0.85f, at, 0.75f);
            StreakFx.Sparks(at, new Vector3(0f, 0.3f, -1f), 22, 6f);
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.04f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.18f, 0.22f);
            if (TimeController.Instance != null) TimeController.Instance.HitStop(5);
            if (shardMat == null)
            {
                shardMat = new Material(Shader.Find("Sprites/Default"));
                shardMat.color = new Color(0.72f, 0.86f, 0.95f, 0.75f);
            }
            for (int i = 0; i < 34; i++)
            {
                var go = new GameObject("Shard");
                go.transform.SetParent(transform, true);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = ShardMesh(Random.Range(0.05f, 0.18f));
                go.AddComponent<MeshRenderer>().sharedMaterial = shardMat;
                go.transform.position = new Vector3(Random.Range(x0 + 0.1f, x1 - 0.1f), Random.Range(sill + 0.05f, head - 0.05f), z - 0.02f);
                go.transform.rotation = Random.rotation;
                // most of it blows out towards the camera and rains down on the deck and over the rail
                var v = new Vector3(Random.Range(-1.5f, 1.5f) + dir * 0.6f, Random.Range(-0.5f, 2.5f), -Random.Range(1.2f, 4.2f));
                shards.Add(new Shard { tr = go.transform, vel = v, spin = Random.insideUnitSphere * 720f, life = Random.Range(2.5f, 4f) });
            }
        }

        void UpdateShards(float dt)
        {
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                var s = shards[i];
                if (s.tr == null) { shards.RemoveAt(i); continue; }
                s.life -= dt;
                if (s.life <= 0f) { Destroy(s.tr.gameObject); shards.RemoveAt(i); continue; }
                if (s.landed) continue;
                s.vel.y -= 18f * dt;
                var p = s.tr.position + s.vel * dt;
                // they land on the deck (it reaches out over the lane to z -0.85) or carry on to the floor
                float floor = p.z > -0.85f && p.x > 40f && p.x < 50f && s.tr.position.y >= deckY ? deckY : 0f;
                if (p.y <= floor + 0.01f)
                {
                    p.y = floor + 0.01f;
                    s.landed = true;
                    s.tr.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);  // lying flat, glinting
                    if (Random.value < 0.25f) Sound.Play(Sfx.Glass, 0.25f, p, 1.4f);
                }
                s.tr.position = p;
                if (!s.landed) s.tr.Rotate(s.spin * dt, Space.World);
            }
        }

        static Mesh ShardMesh(float size)
        {
            var m = new Mesh { name = "Shard" };
            m.vertices = new[]
            {
                new Vector3(0f, size, 0f),
                new Vector3(-size * Random.Range(0.3f, 0.7f), -size * Random.Range(0.3f, 0.8f), 0f),
                new Vector3(size * Random.Range(0.3f, 0.7f), -size * Random.Range(0.2f, 0.6f), 0f),
            };
            m.triangles = new[] { 0, 1, 2, 0, 2, 1 };
            m.colors = new[] { Color.white, new Color(1f, 1f, 1f, 0.6f), new Color(1f, 1f, 1f, 0.8f) };
            m.RecalculateBounds();
            return m;
        }

        void SetWhole(bool w)
        {
            if (whole != null) foreach (var r in whole) if (r != null) r.enabled = w;
            if (teeth != null) foreach (var r in teeth) if (r != null) r.enabled = !w;
        }

        public static void ResetAll()
        {
            foreach (var g in All)
            {
                if (g == null) continue;
                g.Broken = false;
                g.thrown = null;
                g.wasDown.Clear();
                foreach (var s in g.shards) if (s.tr != null) Destroy(s.tr.gameObject);
                g.shards.Clear();
                g.SetWhole(true);
            }
        }
    }
}
