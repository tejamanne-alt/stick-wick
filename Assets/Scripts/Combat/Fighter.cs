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
        public bool hitsDowned;    // can hit an enemy that is already down (finishers, throws)
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

        /// <summary>Change the body colour (enemies come in colours; the hero is black).</summary>
        public void SetBaseColor(Color c)
        {
            baseColor = c;
            UpdateTint();
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

        /// <summary>While set, the ground probe starts below the fighter's feet, so they drop
        /// through the floor they stand on (diving down to the level below).</summary>
        protected float dropThroughUntil;
        float fallStartY;

        /// <summary>The level a fighter is on: 0 = ground floor, 1 = upper floor.</summary>
        public int Level => transform.position.y > 1.9f ? 1 : 0;

        float Probe(Vector3 p)
        {
            if (Time.time < dropThroughUntil)
            {
                var below = p + Vector3.down * 0.35f;
                return Physics.Raycast(below, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
            }
            return GroundY(p);
        }

        /// <summary>Move by velocity, apply gravity, land on the ground and follow stairs.
        /// Returns the wall side (-1/+1) if the fighter was stopped by the arena edge.</summary>
        protected int Integrate(float dt)
        {
            var p = transform.position;
            p.x += velocity.x * dt;
            p.z += velocity.z * dt;
            int wall = 0;
            if (p.x < arenaMinX) { p.x = arenaMinX; wall = -1; }
            else if (p.x > arenaMaxX) { p.x = arenaMaxX; wall = 1; }
            float ground = Probe(p);
            if (Grounded)
            {
                if (velocity.y > 0.01f || p.y - ground > 0.55f)   // jumped, or walked off a ledge
                {
                    Grounded = false;
                    fallStartY = p.y;
                }
                else p.y = Mathf.MoveTowards(p.y, ground, 12f * dt); // follow steps
            }
            if (!Grounded)
            {
                velocity.y -= gravity * dt;
                p.y += velocity.y * dt;
                fallStartY = Mathf.Max(fallStartY, p.y);
                if (p.y <= ground && velocity.y <= 0f)
                {
                    p.y = ground;
                    velocity.y = 0f;
                    Grounded = true;
                    transform.position = p;
                    OnLanded(fallStartY - ground);
                    return wall;
                }
            }
            transform.position = p;
            return wall;
        }

        protected virtual void OnLanded(float fallHeight) { }

        public abstract void TakeHit(HitInfo hit);
    }
}
