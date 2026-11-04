using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>The glass floor in the middle of the catwalk. It holds people walking on it,
    /// but anyone landing hard (thrown, knocked down from above, the brute) smashes through
    /// and falls six metres to the lobby. It is repaired for each new floor.</summary>
    public class GlassPanel : MonoBehaviour
    {
        public static readonly List<GlassPanel> All = new List<GlassPanel>();
        public int platform = -1;              // its index in the level's platform map
        public float x0, x1, y;
        public Renderer[] visuals;
        Collider col;
        public bool Broken { get; private set; }

        void Awake() { col = GetComponent<Collider>(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Someone landed at `at` after falling `fall` metres. Breaks the glass if it is
        /// hard enough; returns true if they are now falling through.</summary>
        public static bool Land(Vector3 at, float fall, bool heavy)
        {
            foreach (var g in All)
            {
                if (g.Broken || at.x < g.x0 - 0.1f || at.x > g.x1 + 0.1f || Mathf.Abs(at.y - g.y) > 0.3f) continue;
                if (fall < 1.4f && !heavy) return false;
                g.Shatter(at);
                return true;
            }
            return false;
        }

        void Shatter(Vector3 at)
        {
            Broken = true;
            if (col != null) col.enabled = false;
            if (visuals != null) foreach (var r in visuals) if (r != null) r.enabled = false;
            if (platform >= 0) LevelNav.SetBroken(platform, true);
            Sound.Play(Sfx.Glass, 1f, at);
            Sound.Play(Sfx.Glass, 0.8f, at, 0.8f);
            StreakFx.Sparks(at, Vector3.down, 26, 6f);
            StreakFx.Sparks(at, Vector3.up, 10, 4f);
            if (InkFx.Instance != null) InkFx.Instance.Burst(at, 3f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.15f, 0.2f);
            if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(120, "THROUGH THE GLASS!");
        }

        public static void ResetAll()
        {
            foreach (var g in All)
            {
                g.Broken = false;
                if (g.col != null) g.col.enabled = true;
                if (g.visuals != null) foreach (var r in g.visuals) if (r != null) r.enabled = true;
                if (g.platform >= 0) LevelNav.SetBroken(g.platform, false);
            }
        }
    }
}
