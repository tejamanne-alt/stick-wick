using UnityEngine;

namespace ProjectInk
{
    /// <summary>Builds a playable fighter from the imported stickman model:
    /// root (Fighter script) → model (Animator + ClipPlayer) → skinned mesh, plus the weapons
    /// in its right hand (the hero carries one of each, shown by style; an enemy its own).</summary>
    public static class FighterFactory
    {
        public static GameObject Build(GameObject model, MoveSet moves, Material material, string name, bool hero,
                                       Material trailMaterial = null, FightStyle enemyWeapon = FightStyle.Fists)
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

            GameObject sword = null, pistol = null, knife = null, pipe = null;
            TrailRenderer swordTrail = null, pipeTrail = null;
            Transform muzzle = null;
            if (hero || enemyWeapon == FightStyle.Sword) sword = BuildWeapon(player, FightStyle.Sword, material, trailMaterial, out swordTrail, out _);
            if (hero || enemyWeapon == FightStyle.Gun) pistol = BuildWeapon(player, FightStyle.Gun, material, null, out _, out muzzle);
            if (hero || enemyWeapon == FightStyle.Knife) knife = BuildWeapon(player, FightStyle.Knife, material, null, out _, out _);
            if (hero || enemyWeapon == FightStyle.Pipe) pipe = BuildWeapon(player, FightStyle.Pipe, material, trailMaterial, out pipeTrail, out _);

            Fighter f = hero ? root.AddComponent<HeroController>() : (Fighter)root.AddComponent<Enemy>();
            f.body = player;
            if (f is HeroController h)
            {
                h.sword = sword;
                h.pistol = pistol;
                h.knife = knife;
                h.pipe = pipe;
                h.swordTrail = swordTrail;
                h.pipeTrail = pipeTrail;
                h.muzzle = muzzle;
                h.weaponMaterial = material;
                h.trailMaterial = trailMaterial;
            }
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

        public static Transform Part(Transform parent, Vector3 localPos, Vector3 size, Material mat)
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

        /// <summary>Weapon geometry along the parent's +Z, starting at the fist.</summary>
        public static void Model(Transform parent, FightStyle kind, Material mat)
        {
            switch (kind)
            {
                case FightStyle.Sword:
                    Part(parent, new Vector3(0, 0, -0.04f), new Vector3(0.035f, 0.035f, 0.2f), mat);   // handle
                    Part(parent, new Vector3(0, 0, 0.07f), new Vector3(0.05f, 0.13f, 0.025f), mat);   // guard
                    Part(parent, new Vector3(0, 0, 0.55f), new Vector3(0.022f, 0.05f, 0.95f), mat);   // blade
                    break;
                case FightStyle.Gun:
                    Part(parent, new Vector3(0, 0.02f, 0.06f), new Vector3(0.045f, 0.06f, 0.22f), mat);
                    Part(parent, new Vector3(0, -0.05f, -0.01f), new Vector3(0.04f, 0.12f, 0.055f), mat);
                    break;
                case FightStyle.Knife:
                    Part(parent, new Vector3(0, 0, -0.02f), new Vector3(0.03f, 0.03f, 0.11f), mat);    // handle
                    Part(parent, new Vector3(0, 0, 0.13f), new Vector3(0.015f, 0.04f, 0.22f), mat);    // blade
                    break;
                case FightStyle.Pipe:
                    Part(parent, new Vector3(0, 0, 0.45f), new Vector3(0.045f, 0.045f, 1.25f), mat);   // steel pipe
                    Part(parent, new Vector3(0, 0, 1.06f), new Vector3(0.07f, 0.07f, 0.05f), mat);    // joint
                    break;
            }
        }

        /// <summary>Kept for callers of the earlier API.</summary>
        public static Transform SwordModel(Transform parent, Material mat) { Model(parent, FightStyle.Sword, mat); return parent; }
        public static Transform PistolModel(Transform parent, Material mat) { Model(parent, FightStyle.Gun, mat); return parent; }

        static GameObject BuildWeapon(ClipPlayer player, FightStyle kind, Material mat, Material trailMat,
                                      out TrailRenderer trail, out Transform muzzle)
        {
            trail = null;
            muzzle = null;
            var mount = Mount(player, kind.ToString());
            if (mount == null) return null;
            Model(mount, kind, mat);
            if (kind == FightStyle.Gun)
            {
                muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(mount, false);
                muzzle.localPosition = new Vector3(0, 0.02f, 0.2f);
            }
            if ((kind == FightStyle.Sword || kind == FightStyle.Pipe) && trailMat != null)
            {
                // a wide white ribbon behind the tip reads as the crescent slash of the reference
                var tip = new GameObject("Tip").transform;
                tip.SetParent(mount, false);
                tip.localPosition = new Vector3(0, 0, kind == FightStyle.Pipe ? 1.08f : 1.0f);
                trail = tip.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = 0.16f;
                trail.minVertexDistance = 0.03f;
                trail.widthCurve = AnimationCurve.EaseInOut(0f, kind == FightStyle.Pipe ? 0.22f : 0.32f, 1f, 0f);
                trail.startColor = new Color(1f, 1f, 1f, 0.95f);
                trail.endColor = new Color(1f, 1f, 1f, 0f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.emitting = false;
            }
            return mount.gameObject;
        }

        /// <summary>White motion trail (a slash arc, as in the reference) at the end of a limb.
        /// In the rest pose the limb is straight, so the tip lies on the line parent → bone.</summary>
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
