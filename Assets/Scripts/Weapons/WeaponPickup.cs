using UnityEngine;

namespace ProjectInk
{
    /// <summary>A weapon on the floor (knives, a pipe, a sword, a pistol). It lies flat and still on the
    /// concrete like a real thing, and a faint glint now and then says it can be taken. One dropped by a
    /// man (WaveDirector) falls from his hand, bounces once with a clang and settles; one placed by the
    /// level is simply there, and never goes. Dropped ones fade after a while.
    ///
    /// The hero takes it by its GRIP end (the hilt of a sword; either end of a pipe): HeroController
    /// lines his hand up with GripX, and WeaponLift carries the model from where it lay into his hand,
    /// so it's lifted, never swapped.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<WeaponPickup> All = new System.Collections.Generic.List<WeaponPickup>();

        public FightStyle kind;
        public int uses;
        /// <summary>Seconds before it fades; below 0 it stays (placed by the level).</summary>
        public float lifetime = -1f;
        /// <summary>The way it points along the floor from its grip end (+1: towards +x).</summary>
        public int lieDir = 1;

        float born, ground;
        Transform model;
        Transform glint;
        Renderer glintR;
        float glintAt, glintT = -1f;
        // falling (a drop): the centre, its velocity, the angle in the side plane and its spin
        bool settled = true;
        Vector2 c, v;
        float ang, spin;
        int bounces;

        public bool Settled => settled;
        public Transform Model => model;
        /// <summary>The model's world scale (the hand's copy is 0.8: sized for the chibi body).</summary>
        public float Scale => kind == FightStyle.Pipe || kind == FightStyle.Sword ? 0.8f : 1.2f;
        /// <summary>From the grip end (the weapon's z = 0) to its middle, in world metres.</summary>
        float GripOffset => kind == FightStyle.Pipe ? 0.36f : kind == FightStyle.Sword ? 0.354f : kind == FightStyle.Knife ? 0.1f : 0.07f;
        /// <summary>From the grip end to the far end.</summary>
        float Length => kind == FightStyle.Pipe ? 0.86f : kind == FightStyle.Sword ? 0.82f : 0.2f;
        float Thick => kind == FightStyle.Pipe ? 0.028f : kind == FightStyle.Sword ? 0.02f : 0.027f;
        /// <summary>World x of the end his hand closes on.</summary>
        public float GripX => transform.position.x - lieDir * GripOffset;

        public static WeaponPickup Spawn(FightStyle kind, int uses, Vector3 at, Material mat) => Spawn(kind, uses, at, mat, false, 0f);

        /// <summary>A weapon on the floor at `at`. Dropped: it falls from hand height (thrown with `throwVx`,
        /// or a random little toss), bounces and settles, and fades after 20 s.</summary>
        public static WeaponPickup Spawn(FightStyle kind, int uses, Vector3 at, Material mat, bool dropped, float throwVx)
        {
            var go = new GameObject(kind + "Pickup");
            float ground = Fighter.GroundY(at);
            go.transform.position = new Vector3(at.x, ground, 0f);
            var p = go.AddComponent<WeaponPickup>();
            p.kind = kind;
            p.uses = uses;
            p.ground = ground;
            p.lieDir = Random.value < 0.5f ? 1 : -1;
            p.model = new GameObject("Model").transform;
            p.model.SetParent(go.transform, false);
            p.model.localScale = Vector3.one * p.Scale;
            FighterFactory.Model(p.model, kind, mat);
            p.BuildGlint();
            p.born = Time.time;
            p.glintAt = Time.time + Random.Range(0.6f, 1.4f);
            if (dropped)
            {
                p.lifetime = 20f;
                p.settled = false;
                p.c = new Vector2(at.x, ground + 0.95f);
                float vx = Mathf.Abs(throwVx) > 0.01f ? throwVx : Random.Range(0.5f, 1.3f) * (Random.value < 0.5f ? 1f : -1f);
                p.v = new Vector2(vx, 1.1f);
                p.ang = Random.Range(0f, 360f);
                p.spin = Random.Range(520f, 860f) * (vx >= 0f ? -1f : 1f);    // tumbling the way it's thrown
            }
            p.Pose();
            return p;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>A pipe has two ends: if his hand is coming to the far one, that's its grip end now
        /// (the same pipe, lying where it lay).</summary>
        public void GripNearest(float fromX)
        {
            if (kind != FightStyle.Pipe || !settled) return;
            float tip = GripX + lieDir * Length;
            if (Mathf.Abs(tip - fromX) >= Mathf.Abs(GripX - fromX)) return;
            lieDir = -lieDir;                                       // (its middle stays where it is)
            Pose();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!settled && dt > 0f) Fall(dt);
            float age = Time.time - born;
            if (lifetime > 0f)
            {
                if (age > lifetime - 3f) model.gameObject.SetActive(Mathf.Repeat(age * 6f, 1f) > 0.35f);    // blinking out
                if (age > lifetime) { Destroy(gameObject); return; }
            }
            if (settled) Glint();
        }

        // ------------------------------------------------------------------ on the floor
        /// <summary>Lying flat along the floor, its thin side down, the grip end at GripX.</summary>
        void Pose()
        {
            if (settled)
            {
                model.position = new Vector3(GripX, ground + Thick, 0f);
                model.rotation = Quaternion.LookRotation(Vector3.right * lieDir, Vector3.forward);
                return;
            }
            var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
            model.position = new Vector3(c.x, c.y, 0f) - dir * GripOffset;
            model.rotation = Quaternion.LookRotation(dir, Vector3.forward);
        }

        /// <summary>A drop: it tumbles down, hits the floor with a clang, one small bounce (it tips flat
        /// as it comes down again), and it lies still.</summary>
        void Fall(float dt)
        {
            v.y -= 22f * dt;
            c += v * dt;
            ang += spin * dt;
            float halfLen = GripOffset + Thick;
            float low = c.y - Mathf.Abs(Mathf.Sin(ang * Mathf.Deg2Rad)) * halfLen - Thick;
            if (bounces > 0)
            {
                // on the way back down: tipping flat
                float flat = Mathf.Cos(ang * Mathf.Deg2Rad) >= 0f ? Mathf.Round(ang / 360f) * 360f : Mathf.Round((ang - 180f) / 360f) * 360f + 180f;
                ang = Mathf.MoveTowardsAngle(ang, flat, 900f * dt);
                spin = 0f;
            }
            if (low <= ground && v.y < 0f)
            {
                c.y += ground - low;
                bounces++;
                var at = new Vector3(c.x, ground, 0f);
                bool steel = kind == FightStyle.Pipe || kind == FightStyle.Sword;
                if (bounces == 1)
                {
                    // the clang (a sword rings; a pipe rings lower and longer)
                    if (kind == FightStyle.Sword) Sound.Play(Sfx.SwordClash, 0.45f, at, 1.25f);
                    else Sound.Play(Sfx.MetalClang, steel ? 0.8f : 0.45f, at, kind == FightStyle.Pipe ? 0.82f : 1.3f);
                    v.y = -v.y * 0.28f;
                    v.x *= 0.45f;
                    spin *= 0.3f;
                    if (v.y < 0.6f) bounces = 2;
                }
                if (bounces >= 2)
                {
                    if (bounces == 2) Sound.Play(Sfx.MetalClang, steel ? 0.35f : 0.2f, at, kind == FightStyle.Pipe ? 0.9f : 1.4f);   // the second, smaller
                    // settled: lying flat, pointing whichever way it came down
                    lieDir = Mathf.Cos(ang * Mathf.Deg2Rad) >= 0f ? 1 : -1;
                    var pos = transform.position;
                    pos.x = c.x;                                   // its middle
                    transform.position = pos;
                    settled = true;
                }
            }
            Pose();
        }

        // ------------------------------------------------------------------ the glint
        static Texture2D starTex;
        static Material starMat;

        void BuildGlint()
        {
            if (starTex == null)
            {
                const int n = 48;
                starTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float core = Mathf.Clamp01(1f - r * 2.2f);
                        float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dx) * 16f) * Mathf.Clamp01(1f - Mathf.Abs(dy)),
                                               Mathf.Clamp01(1f - Mathf.Abs(dy) * 16f) * Mathf.Clamp01(1f - Mathf.Abs(dx)));
                        starTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core * core + rays * 0.9f)));
                    }
                starTex.Apply();
            }
            if (starMat == null) starMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = starTex, color = new Color(1f, 1f, 1f, 0.9f) };
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Glint";
            glint = q.transform;
            glint.SetParent(transform, false);
            glintR = q.GetComponent<Renderer>();
            glintR.sharedMaterial = starMat;
            glintR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glintR.receiveShadows = false;
            glintR.enabled = false;
        }

        /// <summary>Every couple of seconds a small star runs along it, grip to tip, and flares.</summary>
        void Glint()
        {
            if (glint == null) return;
            if (glintT < 0f)
            {
                if (Time.time < glintAt) return;
                glintT = 0f;
                glintR.enabled = true;
            }
            glintT += Time.deltaTime;
            float u = Mathf.Clamp01(glintT / 0.28f);
            float x = GripX + lieDir * Length * (0.15f + 0.85f * u);
            glint.position = new Vector3(x, ground + Thick * 2f + 0.01f, -0.06f);
            var cam = Camera.main;
            if (cam != null) glint.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
            float size = glintT < 0.28f ? 0.07f + 0.06f * u : Mathf.Lerp(0.2f, 0f, (glintT - 0.28f) / 0.22f);
            glint.localScale = Vector3.one * Mathf.Max(0.0001f, size);
            if (glintT > 0.5f)
            {
                glintT = -1f;
                glintR.enabled = false;
                glintAt = Time.time + Random.Range(1.5f, 2.3f);
            }
        }
    }
}
