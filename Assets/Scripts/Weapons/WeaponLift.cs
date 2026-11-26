using UnityEngine;

namespace ProjectInk
{
    /// <summary>The moment a weapon is picked up: the copy in his hand starts exactly where the one on the
    /// floor lay (its place, its angle, its size) and comes up into his grip over a quarter of a second,
    /// turning in the side plane and over the top (a sword lying with its point towards him comes up and
    /// over in an arc), so it's lifted, never swapped. Runs after the pose, the stretch and the puppet rig.</summary>
    [DefaultExecutionOrder(300)]
    public class WeaponLift : MonoBehaviour
    {
        Transform mount;
        Vector3 baseLocalPos, baseLocalScale;
        Quaternion baseLocalRot;
        Vector3 fromPos;
        Quaternion fromRot;
        float fromScale, t, dur;

        /// <summary>Lift `weapon` (the hero's held copy, already shown) from the floor copy's world pose.</summary>
        public static void Begin(GameObject weapon, Transform floor, float floorScale, float seconds)
        {
            if (weapon == null || floor == null) return;
            var l = weapon.GetComponent<WeaponLift>();
            if (l == null) l = weapon.AddComponent<WeaponLift>();
            else l.Restore();
            l.mount = weapon.transform;
            l.baseLocalPos = l.mount.localPosition;
            l.baseLocalRot = l.mount.localRotation;
            l.baseLocalScale = l.mount.localScale;
            l.fromPos = floor.position;
            l.fromRot = floor.rotation;
            l.fromScale = floorScale;
            l.t = 0f;
            l.dur = Mathf.Max(0.05f, seconds);
            l.enabled = true;
        }

        void Restore()
        {
            if (mount == null) return;
            mount.localPosition = baseLocalPos;
            mount.localRotation = baseLocalRot;
            mount.localScale = baseLocalScale;
        }

        void OnDisable() { Restore(); }

        void LateUpdate()
        {
            if (mount == null) { enabled = false; return; }
            Restore();                                        // where the hand has it this frame
            t += Time.deltaTime / dur;
            if (t >= 1f) { enabled = false; return; }
            float k = t * t * (3f - 2f * t);
            Vector3 natPos = mount.position;
            Quaternion natRot = mount.rotation;
            float natScale = Mathf.Max(0.0001f, mount.lossyScale.x);

            // the direction: from the floor's to the hand's, round through the top if it's a long way
            Vector3 d0 = fromRot * Vector3.forward, d1 = natRot * Vector3.forward;
            float a0 = Mathf.Atan2(d0.y, d0.x) * Mathf.Rad2Deg, a1 = Mathf.Atan2(d1.y, d1.x) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(a0, a1);
            if (Mathf.Abs(delta) > 120f && Mathf.Sin((a0 + delta * 0.5f) * Mathf.Deg2Rad) < 0f) delta -= 360f * Mathf.Sign(delta);
            float a = (a0 + delta * k) * Mathf.Deg2Rad;
            float flat = Mathf.Lerp(new Vector2(d0.x, d0.y).magnitude, new Vector2(d1.x, d1.y).magnitude, k);
            var dir = new Vector3(Mathf.Cos(a) * flat, Mathf.Sin(a) * flat, Mathf.Lerp(d0.z, d1.z, k)).normalized;
            var rot = Quaternion.Slerp(fromRot, natRot, k);
            rot = Quaternion.FromToRotation(rot * Vector3.forward, dir) * rot;

            mount.position = Vector3.Lerp(fromPos, natPos, k);
            mount.rotation = rot;
            mount.localScale = baseLocalScale * Mathf.Lerp(fromScale / natScale, 1f, k);
        }
    }
}
