using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A steel oil drum on the hall floor. The hero kicks it (any strike with no one in
    /// the way, like a crate): it goes over with a clang, lands on its side and rolls along the
    /// floor, knocking flat every man in its path (it slows a little with each) until it slams
    /// into the zone's wall or rolls to a stop, and then it can be kicked again. The Foreman's
    /// swinging crane hook sends drums rolling too. All of them stand back up on a new run.</summary>
    public class OilDrum : MonoBehaviour
    {
        public static readonly List<OilDrum> All = new List<OilDrum>();
        public const float Radius = 0.29f, Height = 0.88f;

        enum Mode { Standing, Tipping, Rolling, Lying }

        Mode mode;
        Vector3 home;
        Quaternion homeRot;
        float vx, vy, t, roll, tipFrom;
        int dir = 1;
        Fighter kicker;
        readonly HashSet<Enemy> struck = new HashSet<Enemy>();
        static readonly Quaternion OnSide = Quaternion.Euler(90f, 0f, 0f);       // axis into the screen: it rolls along the lane
        static HeroController hero;

        /// <summary>Kickable: standing, or lying still after a roll.</summary>
        public bool Available => gameObject.activeInHierarchy && (mode == Mode.Standing || mode == Mode.Lying);
        public bool Rolling => mode == Mode.Tipping || mode == Mode.Rolling;
        /// <summary>The floor it stands on.</summary>
        public float FloorY => transform.position.y - (mode == Mode.Standing ? Height * 0.5f : Radius);

        void Awake()
        {
            home = transform.position;
            homeRot = transform.rotation;
            All.Add(this);
        }

        void OnDestroy() { All.Remove(this); }

        /// <summary>Send it rolling that way (a hero's kick, or the crane hook).</summary>
        public void Kick(int direction, Fighter by, float speed = 9.5f)
        {
            if (!Available) return;
            dir = direction >= 0 ? 1 : -1;
            kicker = by;
            struck.Clear();
            vx = dir * speed;
            t = 0f;
            Sound.Play(Sfx.Kick, 1f, transform.position);
            Sound.Play(Sfx.MetalClang, 0.9f, transform.position, 0.8f);
            if (mode == Mode.Standing)
            {
                // over it goes: a little hop as it tips onto its side, then away
                mode = Mode.Tipping;
                tipFrom = transform.position.y;
                vy = 2.2f;
            }
            else
            {
                mode = Mode.Rolling;
                vy = 1.2f;
                Sound.Play(Sfx.WhooshBig, 0.6f, transform.position);
            }
        }

        void Update()
        {
            if (!Rolling) return;
            float dt = Time.deltaTime;
            t += dt;
            var p = transform.position;

            if (mode == Mode.Tipping)
            {
                // 0.16 s from upright to on its side, carried forward by the kick
                float k = Mathf.Clamp01(t / 0.16f);
                transform.rotation = Quaternion.Slerp(homeRotStanding, OnSide, k);
                p.x += vx * 0.5f * dt;
                float floor = Fighter.GroundY(p, 0.3f);
                p.y = Mathf.Max(floor + Mathf.Lerp(Height * 0.5f, Radius, k), tipFrom + vy * t - 11f * t * t);
                transform.position = p;
                if (k >= 1f)
                {
                    mode = Mode.Rolling;
                    vy = 0f;
                    Sound.Play(Sfx.MetalClang, 0.7f, p, 0.65f);
                    if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.06f, 0.1f);
                }
                return;
            }

            // rolling: along the floor, off edges, slowing on the concrete
            p.x += vx * dt;
            float ground = Fighter.GroundY(new Vector3(p.x, p.y, 0f), 0.2f);
            vy -= 22f * dt;
            p.y += vy * dt;
            if (p.y - Radius <= ground)
            {
                if (vy < -4f) { Sound.Play(Sfx.MetalClang, 0.8f, p, 0.7f); vy = -vy * 0.3f; }
                else vy = 0f;
                p.y = ground + Radius;
            }
            roll -= vx * dt / Radius * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, roll) * OnSide;
            vx = Mathf.MoveTowards(vx, 0f, 2.4f * dt);

            // the walls of the fight it is in: a ringing slam and back a little way
            float lo, hi;
            Walls(out lo, out hi);
            if ((p.x < lo && vx < 0f) || (p.x > hi && vx > 0f))
            {
                p.x = Mathf.Clamp(p.x, lo, hi);
                Sound.Play(Sfx.MetalClang, 1f, p, 0.55f);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.1f, 0.12f);
                vx = -vx * 0.35f;
                struck.Clear();
            }
            transform.position = p;

            // everyone in its path goes down
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || !e.Targetable || struck.Contains(e)) continue;
                var ep = e.transform.position;
                if (Mathf.Abs(ep.x - p.x) > Radius + 0.32f || Mathf.Abs(ep.y - (p.y - Radius)) > 0.45f) continue;
                if (Mathf.Abs(vx) < 1.2f) continue;                       // a drum rolling to a stop just nudges feet
                struck.Add(e);
                int d = vx >= 0f ? 1 : -1;
                e.TakeHit(new HitInfo { damage = 22f, direction = d, knockback = 6.5f, knockdown = true, heavy = true, attacker = kicker });
                if (InkFx.Instance != null) InkFx.Instance.Splash(e.Chest, new Vector3(d, 0.6f, 0f), 18, e.InkColor);
                Sound.Play(Sfx.PipeHit, 1f, e.Chest, 0.8f);
                if (TimeController.Instance != null) TimeController.Instance.HitStop(3);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.12f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(e, "DRUM ROLL!", 160);
                vx *= 0.82f;
            }

            if (Mathf.Abs(vx) < 0.35f && vy == 0f)
            {
                vx = 0f;
                mode = Mode.Lying;
                Sound.Play(Sfx.MetalClang, 0.4f, p, 0.6f);
            }
        }

        Quaternion homeRotStanding => homeRot;

        void Walls(out float lo, out float hi)
        {
            var f = kicker;
            if (f == null)
            {
                if (hero == null) hero = FindAnyObjectByType<HeroController>();
                f = hero;
            }
            lo = (f != null ? f.arenaMinX : home.x - 14f) + Radius;
            hi = (f != null ? f.arenaMaxX : home.x + 14f) - Radius;
        }

        /// <summary>The nearest drum the hero can kick on that side, if any.</summary>
        public static OilDrum Near(Vector3 from, int side)
        {
            foreach (var d in All)
            {
                if (d == null || !d.Available) continue;
                float dx = (d.transform.position.x - from.x) * side;
                if (dx > 0f && dx < 1.3f && Mathf.Abs(d.FloorY - from.y) < 0.6f) return d;
            }
            return null;
        }

        public static void ResetAll()
        {
            foreach (var d in All)
            {
                if (d == null) continue;
                d.mode = Mode.Standing;
                d.vx = d.vy = 0f;
                d.roll = 0f;
                d.kicker = null;
                d.transform.SetPositionAndRotation(d.home, d.homeRot);
            }
        }
    }
}
