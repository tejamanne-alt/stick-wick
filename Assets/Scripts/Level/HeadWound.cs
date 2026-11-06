using UnityEngine;

namespace ProjectInk
{
    /// <summary>The wound at the hero's right temple, healing across the five story chapters as
    /// his memory comes back: time passes, and you can see it on him.
    ///
    ///   1  The night of the blow: an open gash, wet red, a trickle of blood down past the eye.
    ///   2  Days later: the blood is gone, the cut is a dark crusted scab.
    ///   3  A week or two: the scab is breaking up, the edges closing and turning pink.
    ///   4  Weeks: a closed, raised pink line, thinner.
    ///   5  The finale: an old scar, a thin pale silver line. The mark of the man he was.
    ///
    /// The wound is three ribbon meshes on the head (built by StoryBuilder.AddScar) plus a bead.
    /// Each stage sets their colour (a property block, so no material copies) and their width
    /// (the meshes are copied once and narrowed in place).</summary>
    public class HeadWound : MonoBehaviour
    {
        public MeshFilter lip, cut, blood;
        public Renderer bead;
        [Tooltip("Preview a stage in play mode (1-5); 0 = follow the story chapter.")]
        [Range(0, 5)] public int previewChapter;

        /// <summary>The story chapter the player is in (1-5). Each chapter's scene sets it.</summary>
        public static int Chapter
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("story_chapter", 1), 1, 5);
            set { PlayerPrefs.SetInt("story_chapter", Mathf.Clamp(value, 1, 5)); PlayerPrefs.Save(); }
        }

        struct Strip { public Mesh mesh; public Vector3[] mid, half, verts; public Renderer r; }
        Strip sLip, sCut, sBlood;
        MaterialPropertyBlock block;
        int shown = -1;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void Start()
        {
            block = new MaterialPropertyBlock();
            sLip = Prepare(lip); sCut = Prepare(cut); sBlood = Prepare(blood);
            Apply(Stage);
        }

        int Stage => previewChapter > 0 ? previewChapter : Chapter;

        void Update()
        {
            if (Stage != shown) Apply(Stage);
        }

        static Strip Prepare(MeshFilter f)
        {
            var s = new Strip();
            if (f == null || f.sharedMesh == null) return s;
            s.mesh = Instantiate(f.sharedMesh);
            f.sharedMesh = s.mesh;
            s.r = f.GetComponent<Renderer>();
            var v = s.mesh.vertices;
            int n = v.Length / 2;
            s.mid = new Vector3[n]; s.half = new Vector3[n]; s.verts = v;
            for (int i = 0; i < n; i++)
            {
                s.mid[i] = (v[i * 2] + v[i * 2 + 1]) * 0.5f;
                s.half[i] = (v[i * 2 + 1] - v[i * 2]) * 0.5f;
            }
            return s;
        }

        /// <summary>Show chapter `stage` (1-5).</summary>
        public void Apply(int stage)
        {
            shown = stage;
            switch (stage)
            {
                case 1:     // fresh: open and wet
                    Set(sLip, new Color(0.30f, 0.02f, 0.025f), 1f, 0f);
                    Set(sCut, new Color(0.72f, 0.07f, 0.06f), 1f, 0f);
                    Set(sBlood, new Color(0.40f, 0.025f, 0.025f), 1f, 0f);
                    Bead(true, new Color(0.40f, 0.025f, 0.025f));
                    break;
                case 2:     // crusted: the blood washed off, a dark scab in the cut
                    Set(sLip, new Color(0.30f, 0.07f, 0.05f), 0.85f, 0f);
                    Set(sCut, new Color(0.47f, 0.13f, 0.07f), 0.8f, 0.1f);
                    Set(sBlood, Color.clear, 0f, 0f);
                    Bead(false, Color.clear);
                    break;
                case 3:     // closing: the scab flaking off in pieces, pink new skin at the edges
                    Set(sLip, new Color(0.46f, 0.17f, 0.16f), 0.6f, 0.15f);
                    Set(sCut, new Color(0.27f, 0.07f, 0.05f), 0.5f, 0.55f);
                    Set(sBlood, Color.clear, 0f, 0f);
                    Bead(false, Color.clear);
                    break;
                case 4:     // healed: a raised pink line
                    Set(sLip, new Color(0.62f, 0.30f, 0.29f), 0.42f, 0.05f);
                    Set(sCut, Color.clear, 0f, 0f);
                    Set(sBlood, Color.clear, 0f, 0f);
                    Bead(false, Color.clear);
                    break;
                default:    // an old scar: thin, pale, silvered
                    Set(sLip, new Color(0.78f, 0.75f, 0.73f), 0.32f, 0f);
                    Set(sCut, Color.clear, 0f, 0f);
                    Set(sBlood, Color.clear, 0f, 0f);
                    Bead(false, Color.clear);
                    break;
            }
        }

        /// <summary>Colour a strip and narrow it to `width` of its built width; `broken` (0-1)
        /// knocks gaps into it along its length (a scab coming away in flakes).</summary>
        void Set(Strip s, Color c, float width, float broken)
        {
            if (s.mesh == null || s.r == null) return;
            bool on = width > 0f && c.a > 0f;
            s.r.enabled = on;
            if (!on) return;
            int n = s.mid.Length;
            for (int i = 0; i < n; i++)
            {
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float m = 1f;
                if (broken > 0f)
                {
                    float noise = 0.5f + 0.5f * Mathf.Sin(k * 31f + 0.7f) * Mathf.Sin(k * 13f + 2.1f);
                    m = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((noise - broken * 0.7f) * 6f));
                }
                var h = s.half[i] * (width * m);
                s.verts[i * 2] = s.mid[i] - h;
                s.verts[i * 2 + 1] = s.mid[i] + h;
            }
            s.mesh.vertices = s.verts;
            s.mesh.RecalculateBounds();
            block.Clear();
            block.SetColor(ColorId, c);
            s.r.SetPropertyBlock(block);
        }

        void Bead(bool on, Color c)
        {
            if (bead == null) return;
            bead.enabled = on;
            if (!on) return;
            block.Clear();
            block.SetColor(ColorId, c);
            bead.SetPropertyBlock(block);
        }
    }
}
