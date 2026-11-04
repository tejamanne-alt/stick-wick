using UnityEngine;

namespace ProjectInk
{
    /// <summary>Speed ghosts: a frozen white silhouette of the body (baked from the skinned
    /// mesh) left behind on a sword dash, fading in a fraction of a second.</summary>
    public class Afterimage : MonoBehaviour
    {
        const int Pool = 10;
        static Afterimage[] pool;
        static int next;
        static Color32[] white;

        MeshRenderer mr;
        Mesh mesh;
        MaterialPropertyBlock block;
        float born, life;
        Color color;

        static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Leave a ghost of `body` where it is now.</summary>
        public static void Spawn(SkinnedMeshRenderer body, Color color, float life = 0.22f)
        {
            var fx = InkFx.Instance;
            if (body == null || fx == null || fx.lineMaterial == null) return;
            if (pool == null || pool[0] == null)
            {
                pool = new Afterimage[Pool];
                for (int i = 0; i < Pool; i++)
                {
                    var g = new GameObject("Afterimage" + i);
                    var a = g.AddComponent<Afterimage>();
                    a.mesh = new Mesh { name = "Ghost" };
                    g.AddComponent<MeshFilter>().sharedMesh = a.mesh;
                    a.mr = g.AddComponent<MeshRenderer>();
                    a.mr.sharedMaterial = fx.lineMaterial;
                    a.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    a.mr.receiveShadows = false;
                    a.block = new MaterialPropertyBlock();
                    g.SetActive(false);
                    pool[i] = a;
                }
            }
            var ghost = pool[next];
            next = (next + 1) % Pool;
            body.BakeMesh(ghost.mesh, true);
            int n = ghost.mesh.vertexCount;
            if (white == null || white.Length != n) { white = new Color32[n]; for (int i = 0; i < n; i++) white[i] = new Color32(255, 255, 255, 255); }
            ghost.mesh.colors32 = white;              // the sprite shader multiplies by vertex colour
            ghost.transform.SetPositionAndRotation(body.transform.position, body.transform.rotation);
            ghost.transform.position += Vector3.forward * 0.05f;     // just behind the fighter
            ghost.born = Time.unscaledTime;
            ghost.life = life;
            ghost.color = color;
            ghost.gameObject.SetActive(true);
            ghost.Apply(0f);
        }

        void Apply(float age)
        {
            var c = color;
            c.a *= (1f - age) * (1f - age);
            block.SetColor(ColorId, c);
            mr.SetPropertyBlock(block);
        }

        void Update()
        {
            float age = (Time.unscaledTime - born) / life;
            if (age >= 1f) { gameObject.SetActive(false); return; }
            Apply(age);
        }
    }
}
