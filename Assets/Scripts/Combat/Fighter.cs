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
        public float lift;         // upward launch speed (the sword's rising cut), 0 = default
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
        /// <summary>Chibi proportions: about 1.45 m tall, hips at 0.70 m. Melee reaches scale with it.</summary>
        public const float BodyScale = 0.76f;
        public Vector3 Chest => transform.position + Vector3.up * 0.92f * transform.localScale.y;
        public float Reach(AttackData a) => a.ranged ? a.reach : a.reach * BodyScale * transform.localScale.x;

        [Tooltip("The eyes (built by FighterFactory): two small white ovals that blink.")]
        public Transform[] eyes;
        [Tooltip("How tall the eyes are when open (0.07 round; smaller reads as narrowed slits).")]
        public float eyeOpenY = 0.07f;
        float nextBlink, blinkUntil;
        bool facingSet;

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
            int f = dir >= 0 ? 1 : -1;
            // turning round is eased on the body, so it never snaps
            if (facingSet && f != Facing && body != null) body.EaseTurn(180f);
            facingSet = true;
            Facing = f;
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
            UpdateEyes();
            Color c = Time.time < tintUntil ? tintColor : baseColor;
            block.SetColor(ColorId, c);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(block);
        }

        void UpdateEyes()
        {
            if (eyes == null || eyes.Length == 0) return;
            float t = Time.time;
            if (t > nextBlink) { blinkUntil = t + 0.11f; nextBlink = t + Random.Range(2.2f, 5.5f); }
            bool closed = !Alive || t < blinkUntil;
            foreach (var e in eyes)
                if (e != null) e.localScale = new Vector3(e.localScale.x, closed ? 0.012f : eyeOpenY, e.localScale.z);
        }

        /// <summary>Height of the walkable surface under a point (floors, stairs, landings).</summary>
        /// <summary>Underside of the roof for the current building (set by LevelLayout).</summary>
        public static float RoofY = 6.8f;
        /// <summary>Highest point a floor probe starts from: just under the roof slab, so its top
        /// is never mistaken for a floor (nobody can end up standing on the roof).</summary>
        public static float ProbeCeiling => RoofY - 0.3f;
        /// <summary>Highest a fighter's feet may go: their head meets the roof.</summary>
        public static float HeadRoom => RoofY - 1.6f;

        public static float GroundY(Vector3 p, float probeUp = 0.9f)
        {
            var origin = p + Vector3.up * probeUp;
            origin.y = Mathf.Min(origin.y, ProbeCeiling);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return 0f;
        }

        /// <summary>While set, the ground probe starts below the fighter's feet, so they drop
        /// through the floor they stand on (diving down to the level below).</summary>
        protected float dropThroughUntil;
        float fallStartY;

        /// <summary>The level a fighter is on: 0 = ground floor, 1 = upper floor.</summary>
        public int Level => transform.position.y > 1.9f ? 1 : 0;

        /// <summary>Floor under a fighter in the air: probes from just above the higher of where
        /// the feet were and are, so a thin platform passed from below is not landed on, while
        /// one fallen onto from above is.</summary>
        float ProbeAir(Vector3 p, float prevY)
        {
            if (Time.time < dropThroughUntil) return Probe(p);
            var origin = new Vector3(p.x, Mathf.Min(Mathf.Max(prevY, p.y) + 0.5f, ProbeCeiling), p.z);
            return Physics.Raycast(origin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        }

        float Probe(Vector3 p)
        {
            if (Time.time < dropThroughUntil)
            {
                var below = p + Vector3.down * 0.35f;
                below.y = Mathf.Min(below.y, ProbeCeiling);
                return Physics.Raycast(below, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
            }
            // walking: step up at most a stair's worth, so you pass under open stairs and slabs ...
            if (!LevelNav.Active) return GroundY(p);
            float g = GroundY(p, 0.35f);
            // ... except onto a crate, which anyone can step up on (and stand on to fight)
            if (Physics.Raycast(p + Vector3.up * 0.9f, Vector3.down, out var crateHit, 1.3f, ~0, QueryTriggerInteraction.Ignore) &&
                crateHit.point.y > g && crateHit.collider.GetComponentInParent<Crate>() != null)
                g = crateHit.point.y;
            return g;
        }

        /// <summary>Move by velocity, apply gravity, land on the ground and follow stairs.
        /// Returns the wall side (-1/+1) if the fighter was stopped by the arena edge.</summary>
        protected int Integrate(float dt)
        {
            var p = transform.position;
            float prevY = p.y;
            p.x += velocity.x * dt;
            p.z += velocity.z * dt;
            int wall = 0;
            if (p.x < arenaMinX) { p.x = arenaMinX; wall = -1; }
            else if (p.x > arenaMaxX) { p.x = arenaMaxX; wall = 1; }
            float ground = Grounded ? Probe(p) : ProbeAir(p, prevY);
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
                // the roof: a flying fighter bumps their head and falls back into the hall
                if (p.y > HeadRoom && velocity.y > 0f) { p.y = HeadRoom; velocity.y = 0f; }
                ground = ProbeAir(p, prevY);
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
        // A platform's edge: fighters jump up, hang by their hands, and pull up onto it.
        // (LedgeX/LedgeY are the classic hall's single ledge; the tower passes each ledge in.)
        public static float LedgeX = 0f, LedgeY = 3.2f;
        float ledgeX, ledgeY;
        int ledgeDir = -1;                          // which way the floor lies from the hanging fighter
        const float HangDrop = 1.43f;               // hands above the feet with the arms straight up
        protected enum LedgeStage { None, Jump, Hang, Climb }
        protected LedgeStage ledge = LedgeStage.None;
        float ledgeTime, hangSeconds;
        Vector3 ledgeFrom;

        public bool OnLedge => ledge != LedgeStage.None;
        public bool Hanging => ledge == LedgeStage.Hang || ledge == LedgeStage.Jump;
        /// <summary>Where to stand on the floor below to jump for the ledge.</summary>
        public static float LedgeFootX => LedgeX + 0.25f;

        /// <summary>Jump from the floor below up to the classic hall's ledge.</summary>
        protected void BeginLedgeClimb(float hang) => BeginLedgeClimb(hang, LedgeX, LedgeY, -1);

        /// <summary>Jump up to the ledge at (x, y), hang, then climb onto the floor lying towards `dir`.</summary>
        protected void BeginLedgeClimb(float hang, float x, float y, int dir)
        {
            ledgeX = x; ledgeY = y; ledgeDir = dir >= 0 ? 1 : -1;
            ledge = LedgeStage.Jump;
            ledgeTime = 0f;
            hangSeconds = hang;
            ledgeFrom = transform.position;
            velocity = Vector3.zero;
            SetFacing(ledgeDir);                    // face the floor we are climbing onto
            body.Play("hang", 0.08f);
        }

        /// <summary>Advance the ledge sequence. Returns true while it owns the fighter.</summary>
        protected bool UpdateLedge(float dt)
        {
            if (ledge == LedgeStage.None) return false;
            ledgeTime += dt;
            var hangPos = new Vector3(ledgeX - ledgeDir * 0.2f, ledgeY - HangDrop, 0f);
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
                        transform.position = new Vector3(ledgeX + ledgeDir * 0.24f, ledgeY, 0f);
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
            velocity = new Vector3(-ledgeDir * 0.5f, 0f, 0f);
        }

        protected virtual void OnLedgeClimbed() { }

        public abstract void TakeHit(HitInfo hit);
    }
}
