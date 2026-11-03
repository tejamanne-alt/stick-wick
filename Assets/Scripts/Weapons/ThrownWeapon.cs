using UnityEngine;

namespace ProjectInk
{
    /// <summary>A weapon the hero throws: it spins through the air and kills the first enemy it
    /// meets (homing gently on its target). A sword pins the kill with a long white streak.</summary>
    public class ThrownWeapon : MonoBehaviour
    {
        public float speed = 24f;
        public float spinDegrees = 1440f;
        public float maxLifetime = 1.2f;

        Enemy target;
        Vector3 dir;
        HeroController thrower;
        FightStyle kind;
        float born;
        Transform spinner;

        public static void Throw(HeroController hero, FightStyle kind, Vector3 from, Enemy target, int facing, Material mat, Material trailMat)
        {
            var go = new GameObject("Thrown" + kind);
            go.transform.position = from;
            var t = go.AddComponent<ThrownWeapon>();
            t.thrower = hero;
            t.kind = kind;
            t.target = target;
            t.dir = target != null ? (target.Chest - from).normalized : new Vector3(facing, 0f, 0f);
            t.spinner = new GameObject("Spin").transform;
            t.spinner.SetParent(go.transform, false);
            var holder = new GameObject("Holder").transform;
            holder.SetParent(t.spinner, false);
            holder.localRotation = Quaternion.Euler(0f, 90f, 0f);  // lies in the screen plane
            switch (kind)
            {
                case FightStyle.Sword: holder.localPosition = new Vector3(-0.45f, 0f, 0f); break;
                case FightStyle.Pipe: holder.localPosition = new Vector3(-0.55f, 0f, 0f); break;
                case FightStyle.Knife: holder.localScale = Vector3.one * 1.5f; break;
                default: holder.localScale = Vector3.one * 1.4f; break;
            }
            FighterFactory.Model(holder, kind, mat);
            if (trailMat != null)
            {
                var tr = go.AddComponent<TrailRenderer>();
                tr.sharedMaterial = trailMat;
                tr.time = 0.12f;
                tr.widthCurve = AnimationCurve.Linear(0f, 0.18f, 1f, 0f);
                tr.startColor = new Color(1f, 1f, 1f, 0.9f);
                tr.endColor = new Color(1f, 1f, 1f, 0f);
            }
            t.born = Time.time;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (target != null && target.Targetable)
                dir = Vector3.Slerp(dir, (target.Chest - transform.position).normalized, 8f * dt);
            transform.position += dir * speed * dt;
            spinner.Rotate(0f, 0f, -Mathf.Sign(dir.x == 0f ? 1f : dir.x) * spinDegrees * dt, Space.Self);

            foreach (var e in Enemy.All.ToArray())
            {
                if (!e.Targetable) continue;
                if (Vector3.Distance(e.Chest, transform.position) < 0.7f * Mathf.Max(1f, e.transform.localScale.x))
                {
                    Hit(e);
                    return;
                }
            }
            if (Time.time - born > maxLifetime) Destroy(gameObject);
        }

        void Hit(Enemy e)
        {
            int d = dir.x >= 0f ? 1 : -1;
            e.Kill(new HitInfo { direction = d, knockback = 9f, knockdown = true, heavy = true, attacker = thrower });
            if (InkFx.Instance != null)
            {
                InkFx.Instance.Splash(e.Chest, new Vector3(d, 0.3f, 0f), 40, e.InkColor);
                InkFx.Instance.Burst(e.Chest, 4.5f);
                InkFx.Instance.PlayHit(true);
            }
            if (TimeController.Instance != null) { TimeController.Instance.HitStop(6); TimeController.Instance.SlowMo(0.3f, 0.5f); }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.18f, 0.2f);
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.OnHeroKill(e, kind == FightStyle.Sword ? "SWORD THROW" : kind == FightStyle.Gun ? "GUN THROW"
                                                 : kind == FightStyle.Pipe ? "PIPE THROW" : "KNIFE!", 260, kind != FightStyle.Knife);
            Destroy(gameObject);
        }
    }
}
