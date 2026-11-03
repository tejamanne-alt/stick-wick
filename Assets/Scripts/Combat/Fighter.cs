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

        // ------------------------------------------------------------------ ledge climbing
        // The upper floor's open edge: fighters jump up, hang by their hands, and pull up.
        public static float LedgeX = 0f, LedgeY = 3.2f;
        const float HangDrop = 2.03f;               // hands above the feet with the arms straight up
        protected enum LedgeStage { None, Jump, Hang, Climb }
        protected LedgeStage ledge = LedgeStage.None;
        float ledgeTime, hangSeconds;
        Vector3 ledgeFrom;

        public bool OnLedge => ledge != LedgeStage.None;
        public bool Hanging => ledge == LedgeStage.Hang || ledge == LedgeStage.Jump;
        /// <summary>Where to stand on the floor below to jump for the ledge.</summary>
        public static float LedgeFootX => LedgeX + 0.25f;

        /// <summary>Jump from the floor below up to the ledge, hang, then climb onto it.</summary>
        protected void BeginLedgeClimb(float hang)
        {
            ledge = LedgeStage.Jump;
            ledgeTime = 0f;
            hangSeconds = hang;
            ledgeFrom = transform.position;
            velocity = Vector3.zero;
            SetFacing(-1);                          // the upper floor lies towards -X
            body.Play("hang", 0.08f);
        }

        /// <summary>Advance the ledge sequence. Returns true while it owns the fighter.</summary>
        protected bool UpdateLedge(float dt)
        {
            if (ledge == LedgeStage.None) return false;
            ledgeTime += dt;
            var hangPos = new Vector3(LedgeX + 0.22f, LedgeY - HangDrop, 0f);
            switch (ledge)
            {
                case LedgeStage.Jump:
                    float u = Mathf.Clamp01(ledgeTime / 0.28f);
                    var p = Vector3.Lerp(ledgeFrom, hangPos, u);
                    p.y += Mathf.Sin(u * Mathf.PI) * 0.35f;        // a little arc up to the edge
                    transform.position = p;
                    if (u >= 1f) { ledge = LedgeStage.Hang; ledgeTime = 0f; }
                    break;
                case LedgeStage.Hang:
                    transform.position = hangPos;
                    if (ledgeTime >= hangSeconds)
                    {
                        ledge = LedgeStage.Climb;
                        ledgeTime = 0f;
                        // the climb clip starts from the hanging offset and ends standing here
                        transform.position = new Vector3(LedgeX - 0.38f, LedgeY, 0f);
                        body.Play("climb_up", 0.02f);
                    }
                    break;
                case LedgeStage.Climb:
                    if (body.Finished)
                    {
                        ledge = LedgeStage.None;
                        Grounded = true;
                        velocity = Vector3.zero;
                        OnLedgeClimbed();
                        return false;
                    }
                    break;
            }
            return true;
        }

        /// <summary>Let go of the ledge (hit while hanging).</summary>
        protected void DropFromLedge()
        {
            if (ledge == LedgeStage.None) return;
            ledge = LedgeStage.None;
            Grounded = false;
            velocity = new Vector3(0.5f, 0f, 0f);
        }

        protected virtual void OnLedgeClimbed() { }

        public abstract void TakeHit(HitInfo hit);
    }
}
