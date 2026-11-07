using UnityEngine;

namespace ProjectInk
{
    /// <summary>A real reflection in a flat floor (the wet concrete of the story's warehouse and
    /// yard): just before the floor is drawn, a second camera renders the scene mirrored in the
    /// floor's plane, clipped at it, into a texture the WetFloor shader lays over the floor. Half
    /// resolution (the rain blurs it anyway). The floor itself sits on the Water layer, which the
    /// mirror camera leaves out.</summary>
    [RequireComponent(typeof(Renderer))]
    public class PlanarReflection : MonoBehaviour
    {
        [Tooltip("1 = full screen resolution, 2 = half (default), 3 = a third.")]
        public int downsample = 2;
        [Tooltip("Push the clip plane a hair below the floor so feet meet their reflection.")]
        public float clipOffset = 0.02f;

        const int WaterLayer = 4;
        Camera mirror;
        RenderTexture rt;
        Material mat;
        static bool rendering;
        static readonly int ReflId = Shader.PropertyToID("_ReflectionTex");

        void OnEnable()
        {
            gameObject.layer = WaterLayer;
            if (Application.isPlaying) mat = GetComponent<Renderer>().material;      // its own copy: play never edits the asset
        }

        void OnDisable()
        {
            if (mirror != null) Destroy(mirror.gameObject);
            if (rt != null) { rt.Release(); Destroy(rt); }
            mirror = null; rt = null;
        }

        void OnWillRenderObject()
        {
            var cam = Camera.current;
            if (cam == null || rendering || cam == mirror || mat == null) return;
            if (cam.cameraType != CameraType.Game) return;
            rendering = true;
            try { Render(cam); }
            finally { rendering = false; }
        }

        void Render(Camera cam)
        {
            int w = Mathf.Max(64, cam.pixelWidth / Mathf.Max(1, downsample)), h = Mathf.Max(64, cam.pixelHeight / Mathf.Max(1, downsample));
            if (rt == null || rt.width != w || rt.height != h)
            {
                if (rt != null) { rt.Release(); Destroy(rt); }
                rt = new RenderTexture(w, h, 16, RenderTextureFormat.Default) { name = "FloorReflection", hideFlags = HideFlags.DontSave };
            }
            if (mirror == null)
            {
                var go = new GameObject("MirrorCamera") { hideFlags = HideFlags.HideAndDontSave };
                mirror = go.AddComponent<Camera>();
                mirror.enabled = false;
            }
            mirror.CopyFrom(cam);
            mirror.cullingMask = cam.cullingMask & ~(1 << WaterLayer);
            mirror.targetTexture = rt;
            mirror.clearFlags = CameraClearFlags.SolidColor;
            mirror.backgroundColor = Color.black;

            // mirror the camera in the floor's plane
            var pos = transform.position;
            var normal = Vector3.up;
            float d = -Vector3.Dot(normal, pos) - clipOffset;
            var plane = new Vector4(normal.x, normal.y, normal.z, d);
            var reflection = Reflect(plane);
            mirror.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;
            // and clip away everything under the floor (an oblique near plane)
            var clip = CameraSpacePlane(mirror, pos, normal, clipOffset);
            mirror.projectionMatrix = cam.CalculateObliqueMatrix(clip);

            mirror.transform.position = reflection.MultiplyPoint(cam.transform.position);
            var e = cam.transform.eulerAngles;
            mirror.transform.eulerAngles = new Vector3(-e.x, e.y, e.z);

            bool invert = GL.invertCulling;
            GL.invertCulling = !invert;               // the mirror flips the winding
            mirror.Render();
            GL.invertCulling = invert;
            mat.SetTexture(ReflId, rt);
        }

        static Vector4 CameraSpacePlane(Camera c, Vector3 pos, Vector3 normal, float offset)
        {
            var m = c.worldToCameraMatrix;
            var cpos = m.MultiplyPoint(pos + normal * offset);
            var cnormal = m.MultiplyVector(normal).normalized;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        static Matrix4x4 Reflect(Vector4 p)
        {
            var r = Matrix4x4.identity;
            r.m00 = 1f - 2f * p[0] * p[0]; r.m01 = -2f * p[0] * p[1]; r.m02 = -2f * p[0] * p[2]; r.m03 = -2f * p[3] * p[0];
            r.m10 = -2f * p[1] * p[0]; r.m11 = 1f - 2f * p[1] * p[1]; r.m12 = -2f * p[1] * p[2]; r.m13 = -2f * p[3] * p[1];
            r.m20 = -2f * p[2] * p[0]; r.m21 = -2f * p[2] * p[1]; r.m22 = 1f - 2f * p[2] * p[2]; r.m23 = -2f * p[3] * p[2];
            r.m30 = 0f; r.m31 = 0f; r.m32 = 0f; r.m33 = 1f;
            return r;
        }
    }
}
