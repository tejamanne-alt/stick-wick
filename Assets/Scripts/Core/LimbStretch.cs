using UnityEngine;

namespace ProjectInk
{
    /// <summary>Combat 3.0's impact stretch (combat_v3.md §3, §6.5): when a blow lands, the striking
    /// limb lengthens a few per cent along its own bones for a beat, then snaps back, the old
    /// animator's smear. It runs after the animation each frame (the clips key every bone's position,
    /// so nothing accumulates) and in real time, so it shows through the impact freeze.</summary>
    [DefaultExecutionOrder(180)]
    public class LimbStretch : MonoBehaviour
    {
        public ClipPlayer player;
        Transform[] bones;
        Vector3[] baseLocal;
        float startAt = -9f, amount;
        const float Rise = 0.025f, Hold = 0.05f, Fall = 0.12f;

        /// <summary>Which bones move out along their parents for a blow: the child of each segment to
        /// lengthen (the forearm's position lengthens the upper arm, the hand's the forearm...).</summary>
        static string[] Chain(string clip)
        {
            switch (clip)
            {
                case "jab": case "lead_hook": case "body_shot": case "liver_shot":
                    return new[] { "forearm.L", "hand.L" };
                case "cross": case "hook": case "uppercut": case "power_cross": case "counter_cross": case "hammer_fist":
                    return new[] { "forearm.R", "hand.R" };
                case "elbow": case "trap_elbow":
                    return new[] { "forearm.R" };
                case "low_kick": case "mawashi_geri": case "mae_geri": case "knee":
                    return new[] { "shin.R" };
            }
            return null;
        }

        public void Hit(string clip, float k)
        {
            var names = Chain(clip);
            if (names == null || player == null) return;
            Restore();
            bones = new Transform[names.Length];
            baseLocal = new Vector3[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                bones[i] = player.Bone(names[i]);
                if (bones[i] != null) baseLocal[i] = bones[i].localPosition;     // limb bones never translate: this is their length
            }
            amount = k;
            startAt = Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (bones == null) return;
            float t = Time.unscaledTime - startAt;
            if (t > Rise + Hold + Fall) { Restore(); return; }
            float w = t < Rise ? t / Rise : t < Rise + Hold ? 1f : 1f - (t - Rise - Hold) / Fall;
            w = w * w * (3f - 2f * w);
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].localPosition = baseLocal[i] * (1f + amount * w);
        }

        void Restore()
        {
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].localPosition = baseLocal[i];
            bones = null;
        }
    }
}
