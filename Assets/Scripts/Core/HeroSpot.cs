using UnityEngine;

namespace ProjectInk
{
    /// <summary>Marks the hero the way a film would: a soft pool of white light on the floor
    /// under him, as if a spotlight follows him, breathing gently. It brightens when enemies
    /// crowd in (so he is never lost in the scrum), shrinks while he is in the air and goes out
    /// when he falls.</summary>
    public class HeroSpot : MonoBehaviour
    {
        HeroController hero;
        Transform pool;
        MeshRenderer mr;
        MaterialPropertyBlock block;
        float alpha;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void Attach(HeroController h)
        {
            var go = new GameObject("HeroSpot");
            var s = go.AddComponent<HeroSpot>();
            s.hero = h;
        }

        void Build()
        {
            var fx = InkFx.Instance;
            if (fx == null || fx.lineMaterial == null) return;
            var mat = new Material(fx.lineMaterial) { name = "HeroSpot" };
            mat.mainTexture = Pool(128);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "LightPool";
            q.transform.SetParent(transform, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);     // lying on the floor
            mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            block = new MaterialPropertyBlock();
            pool = q.transform;
        }

        static Texture2D Pool(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    // a soft disc with a slightly brighter rim, like light from a lamp above
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.SmoothStep(0f, 1f, a) * 0.85f + Mathf.Exp(-Mathf.Pow((d - 0.62f) / 0.09f, 2f)) * 0.15f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        void LateUpdate()
        {
            if (hero == null) { Destroy(gameObject); return; }
            if (pool == null) { Build(); if (pool == null) return; }
            var p = hero.transform.position;
            float ground = Fighter.GroundY(p, 0.3f);
            float height = Mathf.Max(0f, p.y - ground);
            // crowded: someone within 1.6 m makes the pool brighter
            float crowd = 0f;
            foreach (var e in Enemy.All)
                if (e.Alive && Mathf.Abs(e.transform.position.x - p.x) < 1.6f && Mathf.Abs(e.transform.position.y - p.y) < 1f) crowd = 1f;
            float want = hero.IsDead ? 0f : Mathf.Lerp(0.16f, 0.3f, crowd) * Mathf.Clamp01(1f - height / 3f);
            var wd = WaveDirector.Instance;
            if (wd != null && wd.FlowReady && !hero.InRampage && hero.InTrouble) want = Mathf.Max(want, 0.32f + 0.14f * Mathf.Sin(Time.unscaledTime * 6f));   // charged: the light pulses
            if (hero.InRampage) want = 0.42f;
            alpha = Mathf.MoveTowards(alpha, want, Time.unscaledDeltaTime * 0.8f);
            float breathe = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2.2f);
            float size = Mathf.Lerp(1.5f, 0.9f, Mathf.Clamp01(height / 2.5f)) * breathe;
            transform.position = new Vector3(p.x, ground + 0.015f, p.z);
            pool.localScale = new Vector3(size, size * 0.8f, 1f);
            block.SetColor(ColorId, new Color(1f, 1f, 1f, alpha));
            mr.SetPropertyBlock(block);
        }
    }
}
