using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Gives a fighter a thin light outline: a copy of the skinned body, inflated along
    /// its normals and drawn inside-out with a flat colour (InkOutline shader), driven by the
    /// same bones so it follows every move. Built at runtime, so it works for the hero placed
    /// in the scene and for every enemy spawned later.</summary>
    public class InkOutline : MonoBehaviour
    {
        public Color color = new Color(0.92f, 0.92f, 0.92f, 1f);
        [Tooltip("Outline thickness in mesh units (metres before the body scale).")]
        public float width = 0.03f;

        static Shader shader;
        static readonly Dictionary<Mesh, Mesh> hulls = new Dictionary<Mesh, Mesh>();
        readonly List<Renderer> mine = new List<Renderer>();

        void Awake()
        {
            if (shader == null) shader = Shader.Find("ProjectInk/InkOutline");
            if (shader == null) { var s = Resources.Load<Shader>("Shaders/InkOutline"); shader = s; }
            if (shader == null) return;
            var mat = new Material(shader) { name = "InkOutline" };
            mat.SetColor("_OutlineColor", color);
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || smr.name.EndsWith("Outline")) continue;
                var go = new GameObject(smr.name + "Outline");
                go.transform.SetParent(smr.transform.parent, false);
                go.transform.localPosition = smr.transform.localPosition;
                go.transform.localRotation = smr.transform.localRotation;
                go.transform.localScale = smr.transform.localScale;
                var o = go.AddComponent<SkinnedMeshRenderer>();
                o.sharedMesh = Hull(smr.sharedMesh, width);
                o.bones = smr.bones;
                o.rootBone = smr.rootBone;
                o.updateWhenOffscreen = true;
                o.sharedMaterial = mat;
                o.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                o.receiveShadows = false;
                mine.Add(o);
            }
        }

        /// <summary>Show or hide the outline (with the body: cinematic hides, deaths).</summary>
        public void SetVisible(bool on) { foreach (var r in mine) if (r != null) r.enabled = on; }

        static Mesh Hull(Mesh src, float w)
        {
            if (hulls.TryGetValue(src, out var m) && m != null) return m;
            m = Object.Instantiate(src);
            m.name = src.name + "Hull";
            var v = m.vertices;
            var n = m.normals;
            if (n == null || n.Length != v.Length) { m.RecalculateNormals(); n = m.normals; }
            // average the normals of vertices that share a position, so seams do not split open
            var avg = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3(Mathf.Round(v[i].x * 1e4f), Mathf.Round(v[i].y * 1e4f), Mathf.Round(v[i].z * 1e4f));
                avg[key] = (avg.TryGetValue(key, out var a) ? a : Vector3.zero) + n[i];
            }
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3(Mathf.Round(v[i].x * 1e4f), Mathf.Round(v[i].y * 1e4f), Mathf.Round(v[i].z * 1e4f));
                v[i] += avg[key].normalized * w;
            }
            m.vertices = v;
            m.RecalculateBounds();
            hulls[src] = m;
            return m;
        }
    }
}
