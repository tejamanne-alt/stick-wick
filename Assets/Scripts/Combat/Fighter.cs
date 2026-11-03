using UnityEngine;

namespace ProjectInk
{
    public struct HitInfo
    {
        public float damage;
        public int direction;      // +1 pushes towards +X, -1 towards -X
        public float knockback;    // horizontal speed, m/s
        public bool knockdown;     // launch and fall
        public bool heavy;
        public Fighter attacker;
    }

    /// <summary>Shared body for the hero and enemies: health, facing, simple 2.5D physics
    /// (gravity, ground snapping on floors and stairs) and tint flashes.</summary>
    public abstract class Fighter : MonoBehaviour
    {
        public ClipPlayer body;
        public float maxHealth = 100f;
        public float gravity = 22f;
        public float arenaMinX = -12.5f, arenaMaxX = 12.5f;

        public float Health { get; protected set; }
        public bool Alive => Health > 0f;
        public int Facing { get; private set; } = 1;
        public bool Grounded { get; protected set; } = true;
        public Vector3 Chest => transform.position + Vector3.up * 1.3f;

        protected Vector3 velocity;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color baseColor = Color.black, tintColor;
        float tintUntil;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        protected virtual void Awake()
        {
            Health = maxHealth;
            if (body == null) body = GetComponentInChildren<ClipPlayer>();
            body.CalibrateFacing();
            renderers = GetComponentsInChildren<SkinnedMeshRenderer>(); // body only, not the motion trails
            block = new MaterialPropertyBlock();
            if (renderers.Length > 0 && renderers[0].sharedMaterial != null && renderers[0].sharedMaterial.HasProperty(ColorId))
                baseColor = renderers[0].sharedMaterial.GetColor(ColorId);
            SetFacing(1);
        }

        public void SetFacing(int dir)
        {
            Facing = dir >= 0 ? 1 : -1;
            transform.rotation = Quaternion.Euler(0f, Facing > 0 ? 0f : 180f, 0f);
        }

        public void FaceTowards(float x)
        {
            if (Mathf.Abs(x - transform.position.x) > 0.05f) SetFacing(x > transform.position.x ? 1 : -1);
        }

        public void Tint(Color c, float seconds)
        {
            tintColor = c;
            tintUntil = Time.time + seconds;
        }

        protected void UpdateTint()
        {
            Color c = Time.time < tintUntil ? tintColor : baseColor;
            block.SetColor(ColorId, c);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(block);
        }

        /// <summary>Height of the walkable surface under a point (floors, stairs, landings).</summary>
        public static float GroundY(Vector3 p, float probeUp = 0.9f)
        {
            if (Physics.Raycast(p + Vector3.up * probeUp, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }

        /// <summary>Move by velocity, apply gravity, land on the ground and follow stairs.</summary>
        protected void Integrate(float dt)
        {
            var p = transform.position;
            p.x += velocity.x * dt;
            p.z += velocity.z * dt;
            p.x = Mathf.Clamp(p.x, arenaMinX, arenaMaxX);
            float ground = GroundY(p);
            if (Grounded)
            {
                if (velocity.y > 0.01f) Grounded = false;
                else if (p.y - ground > 0.55f) Grounded = false;   // walked off a ledge
                else p.y = Mathf.MoveTowards(p.y, ground, 12f * dt); // follow steps
            }
            if (!Grounded)
            {
                velocity.y -= gravity * dt;
                p.y += velocity.y * dt;
                if (p.y <= ground && velocity.y <= 0f)
                {
                    p.y = ground;
                    velocity.y = 0f;
                    Grounded = true;
                    OnLanded();
                }
            }
            transform.position = p;
        }

        protected virtual void OnLanded() { }

        public abstract void TakeHit(HitInfo hit);
    }
}
