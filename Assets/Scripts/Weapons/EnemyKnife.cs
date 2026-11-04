using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A knife thrown by an enemy. It flies straight at chest height; the hero can duck
    /// under it, take it, or — tapping towards it as it arrives — catch it and send it back.</summary>
    public class EnemyKnife : MonoBehaviour
    {
        public static readonly List<EnemyKnife> All = new List<EnemyKnife>();

        public Enemy thrower;
        Vector3 velocity;
        float born;
        Transform spin;
        HeroController hero;

        public Vector3 Velocity => velocity;
        public int Side(Vector3 heroPos) => transform.position.x < heroPos.x ? -1 : 1;

        public static void Throw(Enemy by, HeroController hero, Vector3 from, Vector3 to, Material mat)
        {
            var go = new GameObject("EnemyKnife");
            go.transform.position = from;
            var k = go.AddComponent<EnemyKnife>();
            k.thrower = by;
            k.hero = hero;
            k.velocity = (to - from).normalized * 13f;
            k.spin = new GameObject("Spin").transform;
            k.spin.SetParent(go.transform, false);
            var holder = new GameObject("Holder").transform;
            holder.SetParent(k.spin, false);
            holder.localRotation = Quaternion.Euler(0f, 90f, 0f);
            holder.localScale = Vector3.one * 1.5f;
            FighterFactory.Model(holder, FightStyle.Knife, mat);
            k.born = Time.time;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += velocity * dt;
            spin.Rotate(0f, 0f, -Mathf.Sign(velocity.x) * 1100f * dt, Space.Self);
            if (hero != null && !hero.IsDead)
            {
                var d = hero.Chest - transform.position;
                if (Mathf.Abs(d.x) < 0.35f && Mathf.Abs(d.y) < 0.6f)
                {
                    if (!hero.Invulnerable && !hero.Ducking)
                    {
                        hero.TakeHit(new HitInfo { damage = AttackData.EnemyKnife.damage, direction = velocity.x > 0 ? 1 : -1, knockback = 2f, attacker = thrower });
                        Sound.Play(Sfx.SwordHit, 0.6f, transform.position, 1.2f);
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            if (Time.time - born > 2.2f || Mathf.Abs(transform.position.x) > 14f) Destroy(gameObject);
        }

        /// <summary>The hero caught it.</summary>
        public void Caught()
        {
            Sound.Play(Sfx.SwordClash, 0.7f, transform.position, 1.15f);   // the blade snatched out of the air
            Destroy(gameObject);
        }
    }
}
