using UnityEngine;

namespace ProjectInk
{
    /// <summary>Hand business on top of a playing clip (the walk-out: holding hands, a walkie raised to
    /// the mouth, a hand on a child's head, a child hugging a leg). Each arm can be pulled to a target
    /// with a two-bone reach (the elbow bends towards a pole), blended in and out. The head can nod and
    /// turn, the chest can drop. Runs after the clip has posed the bones (LateUpdate).</summary>
    [DefaultExecutionOrder(200)]
    public class PuppetRig : MonoBehaviour
    {
        public class Arm
        {
            public Transform upper, fore, hand;
            public System.Func<Vector3> target, pole;
            public float weight, want, rate = 4f;
            public void To(System.Func<Vector3> t, System.Func<Vector3> p, float rate = 4f) { target = t; pole = p; want = 1f; this.rate = rate; }
            public void Off(float rate = 3f) { want = 0f; this.rate = rate; }
        }

        public ClipPlayer body;
        public Arm L = new Arm(), R = new Arm();
        /// <summary>Degrees: nod (+ down), turn (+ to the body's left as seen from its front... use Look for a point), tilt.</summary>
        public float nod, turn, chestNod;
        float nodNow, turnNow, chestNow;
        public float headRate = 3f;
        /// <summary>The way the body faces (its forward on the ground).</summary>
        public Vector3 Forward => transform.right;

        public static PuppetRig On(ClipPlayer body)
        {
            if (body == null) return null;
            var r = body.GetComponent<PuppetRig>();
            if (r == null) r = body.gameObject.AddComponent<PuppetRig>();
            r.body = body;
            r.L.upper = body.Bone("upperarm.L"); r.L.fore = body.Bone("forearm.L"); r.L.hand = body.Bone("hand.L");
            r.R.upper = body.Bone("upperarm.R"); r.R.fore = body.Bone("forearm.R"); r.R.hand = body.Bone("hand.R");
            return r;
        }

        /// <summary>The arm whose shoulder is nearer to a point (the side a partner walks on).</summary>
        public Arm Near(Vector3 p)
        {
            if (L.upper == null) return R;
            if (R.upper == null) return L;
            return (L.upper.position - p).sqrMagnitude <= (R.upper.position - p).sqrMagnitude ? L : R;
        }
        public Arm Other(Arm a) => a == L ? R : L;

        public Transform Head => body != null ? body.Bone("head") : null;
        public Transform Chest => body != null ? body.Bone("chest") : null;

        /// <summary>Point the head at a world point (turn only, in degrees, limited).</summary>
        public void LookTowards(Vector3 p, float limit = 70f)
        {
            var d = p - transform.position; d.y = 0f;
            var f = Forward; f.y = 0f;
            if (d.sqrMagnitude < 1e-4f || f.sqrMagnitude < 1e-4f) { turn = 0f; return; }
            turn = Mathf.Clamp(Vector3.SignedAngle(f, d, Vector3.up), -limit, limit);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;                       // scaled: in a slow motion the hands slow too
            Solve(L, dt);
            Solve(R, dt);
            float k = 1f - Mathf.Exp(-headRate * dt);
            nodNow = Mathf.Lerp(nodNow, nod, k);
            turnNow = Mathf.Lerp(turnNow, turn, k);
            chestNow = Mathf.Lerp(chestNow, chestNod, k);
            var f = Forward; f.y = 0f; f.Normalize();
            var side = Vector3.Cross(Vector3.up, f);          // rotating up towards forward: a nod down
            var chest = Chest;
            if (chest != null && Mathf.Abs(chestNow) > 0.01f) chest.rotation = Quaternion.AngleAxis(chestNow, side) * chest.rotation;
            var head = Head;
            if (head != null && (Mathf.Abs(nodNow) > 0.01f || Mathf.Abs(turnNow) > 0.01f))
            {
                var yaw = Quaternion.AngleAxis(turnNow, Vector3.up);
                head.rotation = yaw * Quaternion.AngleAxis(nodNow, yaw * side) * head.rotation;
            }
        }

        void Solve(Arm a, float dt)
        {
            a.weight = Mathf.MoveTowards(a.weight, a.want, dt * a.rate);
            if (a.weight <= 0.001f || a.target == null || a.upper == null || a.fore == null || a.hand == null) return;
            float w = Mathf.SmoothStep(0f, 1f, a.weight);
            Vector3 S = a.upper.position, E = a.fore.position, H = a.hand.position, T = a.target();
            float l1 = (E - S).magnitude, l2 = (H - E).magnitude;
            if (l1 < 1e-4f || l2 < 1e-4f) return;
            var toT = T - S;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, (l1 + l2) * 0.999f);
            var dir = toT.sqrMagnitude > 1e-8f ? toT.normalized : Vector3.down;
            var pole = a.pole != null ? a.pole() : -Forward;
            var perp = pole - Vector3.Dot(pole, dir) * dir;
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.Cross(dir, Vector3.right);
            perp.Normalize();
            float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            float sinA = Mathf.Sqrt(1f - cosA * cosA);
            var E2 = S + dir * (cosA * l1) + perp * (sinA * l1);
            var q1 = Quaternion.FromToRotation(E - S, E2 - S);
            a.upper.rotation = Quaternion.Slerp(Quaternion.identity, q1, w) * a.upper.rotation;
            E = a.fore.position; H = a.hand.position;
            var T2 = S + dir * d;
            var q2 = Quaternion.FromToRotation(H - E, T2 - E);
            a.fore.rotation = Quaternion.Slerp(Quaternion.identity, q2, w) * a.fore.rotation;
        }
    }
}
