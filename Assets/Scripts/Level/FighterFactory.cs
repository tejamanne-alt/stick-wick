using UnityEngine;

namespace ProjectInk
{
    /// <summary>Builds a playable fighter from the imported stickman model:
    /// root (Fighter script) → model (Animator + ClipPlayer) → skinned mesh.</summary>
    public static class FighterFactory
    {
        public static GameObject Build(GameObject model, MoveSet moves, Material material, string name, bool hero,
                                       Material trailMaterial = null)
        {
            var root = new GameObject(name);
            // the model is inactive while we wire it up, so Awake runs once everything is set
            root.SetActive(false);
            var instance = Object.Instantiate(model, root.transform);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            var player = instance.AddComponent<ClipPlayer>();
            player.moves = moves;
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            if (hero && trailMaterial != null)
            {
                AddTrail(player, "forearm.L", "upperarm.L", trailMaterial);
                AddTrail(player, "forearm.R", "upperarm.R", trailMaterial);
                AddTrail(player, "shin.L", "thigh.L", trailMaterial);
                AddTrail(player, "shin.R", "thigh.R", trailMaterial);
            }

            Fighter f = hero ? root.AddComponent<HeroController>() : (Fighter)root.AddComponent<Enemy>();
            f.body = player;
            root.SetActive(true);
            return root;
        }

        /// <summary>Motion trail at the end of a limb (the hand or foot). In the rest pose the
        /// limb is straight, so the tip lies on the line parent → bone.</summary>
        static void AddTrail(ClipPlayer player, string boneName, string parentName, Material mat)
        {
            var bone = player.Bone(boneName);
            var parent = player.Bone(parentName);
            if (bone == null || parent == null) return;
            Vector3 dir = (bone.position - parent.position).normalized;
            float length = Vector3.Distance(bone.position, parent.position);
            var tip = new GameObject(boneName + ".tip").transform;
            tip.SetParent(bone, true);
            tip.position = bone.position + dir * length;
            var tr = tip.gameObject.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = 0.12f;
            tr.minVertexDistance = 0.02f;
            tr.widthCurve = AnimationCurve.Linear(0f, 0.07f, 1f, 0f);
            tr.startColor = new Color(0f, 0f, 0f, 0.55f);
            tr.endColor = new Color(0f, 0f, 0f, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.emitting = false;
        }
    }
}
