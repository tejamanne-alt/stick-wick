using UnityEngine;

namespace ProjectInk
{
    /// <summary>Builds a playable fighter from the imported stickman model:
    /// root (Fighter script) → model (Animator + ClipPlayer) → skinned mesh, plus the weapons
    /// in its right hand (the hero carries one of each, shown by style; an enemy its own).</summary>
    public static class FighterFactory
    {
        public static GameObject Build(GameObject model, MoveSet moves, Material material, string name, bool hero,
                                       Material trailMaterial = null, FightStyle enemyWeapon = FightStyle.Fists,
                                       Material eyeMaterial = null)
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
            // everything below (weapon mounts, trails, eyes) is measured in the rest pose; the
            // model's default pose is whatever frame it was exported on, so put it there first
            var restore = ToBindPose(instance);
            var forward = Forward(player, moves);
            ToBindPose(instance);

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

            // enemy swordsmen carry the katana too (the hero builds his own at runtime, with the slash ribbon)
            if (!hero && sword != null && Application.isPlaying) SwordFx.Attach(sword, trailMaterial, null, false);
            Fighter f = hero ? root.AddComponent<HeroController>() : (Fighter)root.AddComponent<Enemy>();
            f.body = player;
            if (eyeMaterial != null) f.eyes = BuildEyes(player, eyeMaterial, forward);
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
            restore?.Invoke();
            root.SetActive(true);
            return root;
        }

        /// <summary>Which way the model faces (world, horizontal), measured exactly as
        /// ClipPlayer.CalibrateFacing does at runtime: the knee against the hips at the hit of
        /// the front kick. Leaves the model in that pose. +X if it cannot tell.</summary>
        public static Vector3 Forward(ClipPlayer player, MoveSet moves)
        {
            var kick = moves != null ? moves.Get("front_kick") : null;
            var hips = player.Bone("hips");
            var knee = player.Bone("shin.R");
            if (kick == null || kick.clip == null || hips == null || knee == null) return Vector3.right;
            try { kick.clip.SampleAnimation(player.gameObject, kick.hitTime > 0 ? kick.hitTime : kick.Length * 0.4f); }
            catch { return Vector3.right; }
            var fwd = knee.position - hips.position;
            fwd.y = 0f;
            return fwd.sqrMagnitude < 1e-4f ? Vector3.right : fwd.normalized;
        }

        /// <summary>Put a model into its bind (rest) pose, from the skinned mesh's bind poses,
        /// so measurements made on its bones do not depend on the frame it was exported on.
        /// Returns how to put it back (null if there was nothing to do).</summary>
        public static System.Action ToBindPose(GameObject model)
        {
            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null || smr.sharedMesh == null) return null;
            Matrix4x4[] bind;
            try { bind = smr.sharedMesh.bindposes; } catch { return null; }
            var bones = smr.bones;
            if (bind == null || bones == null || bind.Length != bones.Length) return null;
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < bones.Length; i++) if (bones[i] != null) order.Add(i);
            int Depth(Transform t) { int d = 0; for (; t != null; t = t.parent) d++; return d; }
            order.Sort((a, b) => Depth(bones[a]).CompareTo(Depth(bones[b])));
            var savedP = new Vector3[bones.Length];
            var savedR = new Quaternion[bones.Length];
            foreach (int i in order) { savedP[i] = bones[i].localPosition; savedR[i] = bones[i].localRotation; }
            var mesh = smr.transform.localToWorldMatrix;
            foreach (int i in order)                                   // parents first
            {
                var w = mesh * bind[i].inverse;
                bones[i].SetPositionAndRotation(w.GetColumn(3), w.rotation);
            }
            return () =>
            {
                for (int k = order.Count - 1; k >= 0; k--)
                {
                    int i = order[k];
                    bones[i].localPosition = savedP[i];
                    bones[i].localRotation = savedR[i];
                }
            };
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
            mount.localScale = Vector3.one * 0.8f;     // sized for the chibi body
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

        /// <summary>Two small white ovals on the head, one on each side, so whichever way the
        /// fighter faces the eye towards the camera shows (cute, and it blinks).
        /// Measured in the rest pose (the head bone points up), towards `fwd`.</summary>
        static Transform[] BuildEyes(ClipPlayer player, Material mat, Vector3 fwd)
        {
            var head = player.Bone("head");
            if (head == null) return null;
            var center = head.position + Vector3.up * 0.19f;          // head sphere centre above the bone
            var eyes = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(e.GetComponent<Collider>());
                e.name = "Eye" + i;
                e.transform.SetParent(head, true);
                // on the sphere's surface, towards the front of the face, one on each side
                var side = Vector3.Cross(fwd, Vector3.up);
                e.transform.position = center + (fwd * 0.84f + Vector3.up * 0.17f + side * (i == 0 ? 0.52f : -0.52f)).normalized * 0.2f;
                e.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
                var r = e.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                eyes[i] = e.transform;
            }
            return eyes;
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
