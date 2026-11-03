using UnityEngine;

namespace ProjectInk
{
    /// <summary>A weapon dropped by a defeated armed enemy (knives, pipe, sword, pistol). It hovers and turns slowly;
    /// the hero grabs it by running over it (only with empty hands). It fades after a while.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<WeaponPickup> All = new System.Collections.Generic.List<WeaponPickup>();

        public FightStyle kind;
        public int uses;
        public float lifetime = 14f;

        float born, baseY;
        Transform model;

        public static WeaponPickup Spawn(FightStyle kind, int uses, Vector3 at, Material mat)
        {
            var go = new GameObject(kind + "Pickup");
            float ground = Fighter.GroundY(at);
            go.transform.position = new Vector3(at.x, ground + 0.35f, 0f);
            var p = go.AddComponent<WeaponPickup>();
            p.kind = kind;
            p.uses = uses;
            p.baseY = go.transform.position.y;
            p.model = new GameObject("Model").transform;
            p.model.SetParent(go.transform, false);
            var holder = new GameObject("Holder").transform;
            holder.SetParent(p.model, false);
            switch (kind)
            {
                case FightStyle.Sword: holder.localPosition = new Vector3(0, 0, -0.45f); break;
                case FightStyle.Pipe: holder.localPosition = new Vector3(0, 0, -0.55f); break;
                case FightStyle.Knife: holder.localScale = Vector3.one * 1.8f; holder.localPosition = new Vector3(0, 0, -0.2f); break;
                default: holder.localScale = Vector3.one * 1.6f; break;
            }
            FighterFactory.Model(holder, kind, mat);
            p.model.rotation = Quaternion.Euler(0f, 90f, 0f);
            p.born = Time.time;
            return p;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            float age = Time.time - born;
            var pos = transform.position;
            pos.y = baseY + Mathf.Sin(age * 3f) * 0.08f;
            transform.position = pos;
            model.Rotate(0f, 0f, 90f * Time.deltaTime, Space.World);
            // blink in the last seconds
            if (age > lifetime - 3f) model.gameObject.SetActive(Mathf.Repeat(age * 6f, 1f) > 0.35f);
            if (age > lifetime) Destroy(gameObject);
        }
    }
}
