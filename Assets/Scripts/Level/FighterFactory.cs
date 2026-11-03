using UnityEngine;

namespace ProjectInk
{
    /// <summary>Builds a playable fighter from the imported stickman model:
    /// root (Fighter script) → model (Animator + ClipPlayer) → skinned mesh.</summary>
    public static class FighterFactory
    {
        public static GameObject Build(GameObject model, MoveSet moves, Material material, string name, bool hero,
                                       Material trailMaterial = null, bool armedWithSword = false)
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

            // weapons: the hero carries both (shown by fighting style); armed enemies a sword
            GameObject sword = null, pistol = null;
            TrailRenderer swordTrail = null;
            Transform muzzle = null;
            if (hero || armedWithSword)
                sword = BuildSword(player, material, trailMaterial, out swordTrail);
            if (hero)
                pistol = BuildPistol(player, material, out muzzle);

            Fighter f = hero ? root.AddComponent<HeroController>() : (Fighter)root.AddComponent<Enemy>();
            f.body = player;
            if (f is HeroController h)
            {
                h.sword = sword;
                h.pistol = pistol;
                h.swordTrail = swordTrail;
                h.muzzle = muzzle;
            }
            else if (f is Enemy e) e.Armed = armedWithSword;
            root.SetActive(true);
            return root;
        }

        /// <summary>A weapon mount on the right hand bone, its +Z pointing where the hand
        /// bone points (the poses aim the hand bone like the blade or barrel). Measured in
        /// the rest pose, where the arm is straight: hand direction = forearm → hand.</summary>
        static Transform Mount(ClipPlayer player, string name)
        {
            var hand = player.Bone("hand.R");
            var fore = player.Bone("forearm.R");
            if (hand == null || fore == null) return null;
            Vector3 dir = (hand.position - fore.position).normalized;
            var mount = new GameObject(name).transform;
            mount.SetParent(hand, false);
            mount.position = hand.position;
            mount.rotation = Quaternion.LookRotation(dir, Vector3.forward);
            return mount;
        }

        static Transform Part(Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        static GameObject BuildSword(ClipPlayer player, Material mat, Material trailMat, out TrailRenderer trail)
        {
            trail = null;
            var mount = Mount(player, "Sword");
            if (mount == null) return null;
            Part(mount, new Vector3(0, 0, -0.04f), new Vector3(0.035f, 0.035f, 0.2f), mat);   // handle
            Part(mount, new Vector3(0, 0, 0.07f), new Vector3(0.05f, 0.13f, 0.025f), mat);   // guard
            Part(mount, new Vector3(0, 0, 0.55f), new Vector3(0.022f, 0.05f, 0.95f), mat);   // blade
            var tip = new GameObject("Tip").transform;
            tip.SetParent(mount, false);
            tip.localPosition = new Vector3(0, 0, 1.0f);
            if (trailMat != null)
            {
                // a wide white ribbon behind the tip reads as the crescent slash of the reference
                trail = tip.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = 0.16f;
                trail.minVertexDistance = 0.03f;
                trail.widthCurve = AnimationCurve.EaseInOut(0f, 0.32f, 1f, 0f);
                trail.startColor = new Color(1f, 1f, 1f, 0.95f);
                trail.endColor = new Color(1f, 1f, 1f, 0f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.emitting = false;
            }
            return mount.gameObject;
        }

        static GameObject BuildPistol(ClipPlayer player, Material mat, out Transform muzzle)
        {
            muzzle = null;
            var mount = Mount(player, "Pistol");
            if (mount == null) return null;
            Part(mount, new Vector3(0, 0.02f, 0.06f), new Vector3(0.045f, 0.06f, 0.22f), mat);  // slide and barrel
            Part(mount, new Vector3(0, -0.05f, -0.01f), new Vector3(0.04f, 0.12f, 0.055f), mat); // grip
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(mount, false);
            muzzle.localPosition = new Vector3(0, 0.02f, 0.2f);
            return mount.gameObject;
        }

        /// <summary>White motion trail (a slash arc, as in the reference) at the end of a limb (the hand or foot). In the rest pose the
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
            tr.time = 0.14f;
            tr.minVertexDistance = 0.02f;
            tr.widthCurve = AnimationCurve.Linear(0f, 0.11f, 1f, 0f);
            tr.startColor = new Color(1f, 1f, 1f, 0.9f); // white slash arcs
            tr.endColor = new Color(1f, 1f, 1f, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.emitting = false;
        }
    }
}
