using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A wooden crate in the fight lane. Fighters can stand on it; the hero kicks it
    /// into enemies like a bowling ball, and it smashes into splinters. Crates come back on
    /// every new chapter.</summary>
    public class Crate : MonoBehaviour
    {
        public static readonly List<Crate> All = new List<Crate>();

        public Color splinters = new Color(0.32f, 0.3f, 0.28f);
        Vector3 home;
        Quaternion homeRot;
        Vector3 vel;
        bool flying;
        float flightTime;
        Fighter kicker;
        Collider col;

        public bool Available => gameObject.activeSelf && !flying;

        void Awake()
        {
            home = transform.position;
            homeRot = transform.rotation;
            col = GetComponent<Collider>();
            All.Add(this);
        }

        void OnDestroy() { All.Remove(this); }

        public void Kick(int dir, Fighter by)
        {
            flying = true;
            flightTime = 0f;
            kicker = by;
            vel = new Vector3(dir * 12f, 3.5f, 0f);
            if (col != null) col.enabled = false;
            Sound.Play(Sfx.Kick, 1f, transform.position);
            Sound.Play(Sfx.WhooshBig, 0.7f, transform.position);
        }

        void Update()
        {
            if (!flying) return;
            float dt = Time.deltaTime;
            flightTime += dt;
            vel.y -= 22f * dt;
            transform.position += vel * dt;
            transform.Rotate(0f, 0f, -Mathf.Sign(vel.x) * 540f * dt, Space.World);
            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable) continue;
                if (Vector3.Distance(e.Chest, transform.position) < 0.85f)
                {
                    e.TakeHit(new HitInfo { damage = 26f, direction = vel.x >= 0 ? 1 : -1, knockback = 8f, knockdown = true, heavy = true, attacker = kicker });
                    if (WaveDirector.Instance != null) WaveDirector.Instance.OnEnvironmentKill(e, "CRATE STRIKE!", 160);
                    Smash();
                    return;
                }
            }
            float ground = Fighter.GroundY(transform.position);
            if ((flightTime > 0.15f && transform.position.y - 0.3f <= ground) || OutOfArena()) Smash();
        }

        /// <summary>Past the walls of the fight it was kicked in (the hall runs far beyond the old room's ±12.6).</summary>
        bool OutOfArena()
        {
            float x = transform.position.x;
            if (kicker != null) return x < kicker.arenaMinX - 0.4f || x > kicker.arenaMaxX + 0.4f;
            return Mathf.Abs(x - home.x) > 14f;
        }

        void Smash()
        {
            flying = false;
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(transform.position, Vector3.up, 30, splinters);
            }
            Sound.Play(Sfx.Crate, 1f, transform.position);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.15f);
            gameObject.SetActive(false);
        }

        public static void ResetAll()
        {
            foreach (var c in All)
            {
                c.flying = false;
                c.transform.SetPositionAndRotation(c.home, c.homeRot);
                if (c.col != null) c.col.enabled = true;
                c.gameObject.SetActive(true);
            }
        }
    }
}
