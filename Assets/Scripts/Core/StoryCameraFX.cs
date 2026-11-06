using UnityEngine;

namespace ProjectInk
{
    /// <summary>Two lens effects for the story's cinematics, with no custom shaders:
    /// BLUR (waking eyes out of focus): the frame is drawn small and scaled back up, so it softens;
    /// GLITCH (a fractured memory): horizontal slices of the frame jump sideways for a frame or two.
    /// Costs nothing when both are zero (the component switches itself off).</summary>
    [RequireComponent(typeof(Camera))]
    public class StoryCameraFX : MonoBehaviour
    {
        public static StoryCameraFX Instance { get; private set; }

        /// <summary>0 = sharp, 1 = badly out of focus.</summary>
        public float Blur;
        /// <summary>0 = clean, 1 = torn apart.</summary>
        public float Glitch;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void Set(float blur, float glitch)
        {
            if (Instance == null) return;
            Instance.Blur = blur;
            Instance.Glitch = glitch;
            Instance.enabled = blur > 0.01f || glitch > 0.01f;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            RenderTexture small = null, mid = null;
            Texture frame = src;
            if (Blur > 0.01f)
            {
                int div = Mathf.Clamp(Mathf.RoundToInt(1f + Blur * 15f), 2, 16);
                small = RenderTexture.GetTemporary(Mathf.Max(8, src.width / div), Mathf.Max(8, src.height / div), 0, src.format);
                small.filterMode = FilterMode.Bilinear;
                Graphics.Blit(src, small);
                mid = RenderTexture.GetTemporary(Mathf.Max(8, src.width / 2), Mathf.Max(8, src.height / 2), 0, src.format);
                mid.filterMode = FilterMode.Bilinear;
                Graphics.Blit(small, mid);                     // a second pass smooths the blocks
                frame = mid;
            }
            Graphics.Blit(frame, dst);
            if (Glitch > 0.01f)
            {
                int w = dst != null ? dst.width : Screen.width, h = dst != null ? dst.height : Screen.height;
                RenderTexture.active = dst;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, w, h, 0);
                int slices = 7 + Mathf.RoundToInt(Glitch * 9f);
                for (int i = 0; i < slices; i++)
                {
                    float y = Random.value * h;
                    float sh = h * Random.Range(0.01f, 0.07f) * (0.5f + Glitch);
                    float dx = (Random.value - 0.5f) * w * 0.12f * Glitch;
                    Graphics.DrawTexture(new Rect(dx, y, w, sh), frame, new Rect(0f, 1f - (y + sh) / h, 1f, sh / h), 0, 0, 0, 0);
                }
                GL.PopMatrix();
            }
            if (small != null) RenderTexture.ReleaseTemporary(small);
            if (mid != null) RenderTexture.ReleaseTemporary(mid);
        }
    }
}
