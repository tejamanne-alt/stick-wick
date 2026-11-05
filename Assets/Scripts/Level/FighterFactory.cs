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
                                       Material eyeMaterial = null, Material lensMaterial = null, Material accentMaterial = null)
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

            // enemy swordsmen carry the katana too (the hero builds his own at runtime, with the slash ribbon)
            if (!hero && sword != null && Application.isPlaying) SwordFx.Attach(sword, trailMaterial, null, false);
            Fighter f = hero ? root.AddComponent<HeroController>() : (Fighter)root.AddComponent<Enemy>();
            f.body = player;
            // the hero wears mirrored shades instead of the cute eyes, and a red scarf knotted at the neck
            if (hero && lensMaterial != null) BuildShades(player, lensMaterial, eyeMaterial);
            else if (eyeMaterial != null) f.eyes = BuildEyes(player, eyeMaterial);
            if (hero && accentMaterial != null) BuildScarfKnot(player, accentMaterial);
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
        /// Measured in the rest pose: the head bone points up, the model faces +X.</summary>
        static Transform[] BuildEyes(ClipPlayer player, Material mat)
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
                e.transform.position = center + new Vector3(0.84f, 0.17f, i == 0 ? 0.52f : -0.52f).normalized * 0.2f;
                e.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
                var r = e.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                eyes[i] = e.transform;
            }
            return eyes;
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.name = name;
            g.transform.position = pos;
            g.transform.rotation = rot;
            g.transform.localScale = scale;
            g.transform.SetParent(parent, true);
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return g;
        }

        /// <summary>The hero's mirrored wraparound shades: a silver lens on each side of the face
        /// (one always faces the camera, whichever way he faces), a temple arm back towards the ear,
        /// a bridge across the front and a white glint on each lens. Silver reads on the black head
        /// at any size. Measured in the rest pose: head bone up, model facing +X.</summary>
        static void BuildShades(ClipPlayer player, Material lens, Material glint)
        {
            var head = player.Bone("head");
            if (head == null) return;
            var c = head.position + Vector3.up * 0.19f;            // head sphere centre
            const float R = 0.214f;                                  // just proud of the head (radius 0.21)
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? 1f : -1f;
                // the lens: a flat, slightly swept oval lying on the face, a touch above the eye line
                var n = new Vector3(0.60f, 0.13f, 0.79f * s).normalized;       // front three-quarters: it faces the camera
                var rot = Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(0f, 0f, -8f * s);
                var l = Part(PrimitiveType.Sphere, "Lens" + i, head, c + n * R, rot, new Vector3(0.165f, 0.062f, 0.026f), lens);
                // the glint: a sliver of white on the lens's upper front
                if (glint != null)
                    Part(PrimitiveType.Cube, "Glint" + i, l.transform, c + n * (R + 0.014f) + Vector3.up * 0.017f + Vector3.right * 0.014f,
                         rot * Quaternion.Euler(0f, 0f, 25f), new Vector3(0.045f, 0.011f, 0.004f), glint);
                // the temple arm, back along the side of the head towards the ear
                var side = new Vector3(0.05f, 0.16f, 0.99f * s).normalized;
                var back = Vector3.ProjectOnPlane(Vector3.left, side).normalized;
                Part(PrimitiveType.Cube, "Temple" + i, head, c + side * (R + 0.004f) + back * 0.02f,
                     Quaternion.LookRotation(back, Vector3.up), new Vector3(0.012f, 0.012f, 0.15f), lens);
            }
            // the bridge over the nose
            Part(PrimitiveType.Cube, "Bridge", head, c + new Vector3(R - 0.004f, 0.03f, 0f), Quaternion.identity, new Vector3(0.016f, 0.018f, 0.14f), lens);
        }

        /// <summary>The knot of the hero's red scarf, at the neck (the tails are cloth, simulated
        /// at runtime by HeroScarf).</summary>
        static void BuildScarfKnot(ClipPlayer player, Material accent)
        {
            var head = player.Bone("head");
            var chest = player.Bone("chest");
            if (head == null || chest == null) return;
            var neck = head.position + Vector3.up * 0.02f;
            Part(PrimitiveType.Sphere, "ScarfWrap", chest, neck, Quaternion.identity, new Vector3(0.24f, 0.10f, 0.24f), accent);
            Part(PrimitiveType.Sphere, "ScarfKnot", chest, neck + new Vector3(-0.11f, -0.02f, 0f), Quaternion.identity, new Vector3(0.09f, 0.09f, 0.09f), accent);
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
