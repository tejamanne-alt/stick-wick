using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Development builds and the editor only: watches every fighter and reports any
    /// frame in which one jumps further than a body can move (a "teleport"): a position write
    /// that skips the animation. Each report names the fighter, its state and clip, and draws a
    /// red line in the Scene view from where it was to where it appeared.
    /// Designed fast moves open a window with Fighter.AllowWarp (the iai blur, cutscene placement,
    /// the ledge climb hand-off); cinematics and bodies carried by the hero are ignored.
    /// The count is in <see cref="Violations"/>: a normal fight should end with 0.</summary>
    public class TeleportGuard : MonoBehaviour
    {
        /// <summary>Faster than any run, leap or knockback in the game (sprint 10, leap glide 9, throws 12).</summary>
        public const float MaxSpeed = 16f;
        public static int Violations { get; private set; }

        readonly Dictionary<Fighter, Vector3> last = new Dictionary<Fighter, Vector3>();
        readonly Dictionary<Fighter, float> quietUntil = new Dictionary<Fighter, float>();
        readonly List<Fighter> fighters = new List<Fighter>();
        HeroController hero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Debug.isDebugBuild) return;
            var go = new GameObject("TeleportGuard");
            DontDestroyOnLoad(go);
            go.AddComponent<TeleportGuard>();
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float now = Time.unscaledTime;
            if (hero == null) hero = FindAnyObjectByType<HeroController>();
            fighters.Clear();
            if (hero != null) fighters.Add(hero);
            fighters.AddRange(Enemy.All);

            foreach (var f in fighters)
            {
                if (f == null) continue;
                var p = f.transform.position;
                if (dt > 0f && last.TryGetValue(f, out var q) && now >= f.WarpAllowedUntil && !Exempt(f))
                {
                    float dx = Mathf.Abs(p.x - q.x), dy = Mathf.Abs(p.y - q.y);
                    bool sideways = dx > 0.25f && dx > MaxSpeed * dt * 1.2f;
                    bool vertical = dy > 1.5f && dy > 30f * dt;          // falls reach ~20 m/s; more is a snap
                    if (sideways || vertical) Report(f, q, p, dx, dy, dt, now);
                }
                last[f] = p;
            }

            // forget fighters that are gone
            if (Time.frameCount % 300 == 0)
            {
                var gone = new List<Fighter>();
                foreach (var k in last.Keys) if (k == null) gone.Add(k);
                foreach (var k in gone) { last.Remove(k); quietUntil.Remove(k); }
            }
        }

        static bool Exempt(Fighter f)
        {
            if (f is HeroController h) return h.CinematicLock || h.RampageIntro;
            if (f is Enemy e) return e.Carried;
            return false;
        }

        void Report(Fighter f, Vector3 from, Vector3 to, float dx, float dy, float dt, float now)
        {
            Violations++;
            Debug.DrawLine(from + Vector3.up, to + Vector3.up, Color.red, 3f);
            if (quietUntil.TryGetValue(f, out var q) && now < q) return;
            quietUntil[f] = now + 0.5f;
            string clip = f.body != null ? f.body.CurrentName : "?";
            Debug.LogWarning($"[TeleportGuard] {f.name} jumped {dx:0.00} m sideways, {dy:0.00} m up/down in one frame " +
                             $"({dx / dt:0} m/s). State {f.DebugState}, clip '{clip}'. Something set its position " +
                             $"without the animation carrying it. (violation #{Violations})");
        }
    }
}
