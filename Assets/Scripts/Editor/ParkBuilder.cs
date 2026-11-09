using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 4: THE BACKYARD (docs/story/chapter4_backyard.md). Puts the baked night set together
    /// (tools/blender/build_park_set.py): the garden behind No. 14, the play area, the park, the pond,
    /// the car park and the van; the fortress lit on Kessel Hill. Moonlight and the park lamps' cones,
    /// a low mist over the grass and the water, the pond's still surface reflecting. Bins and park
    /// crates to use. The swing seat and the van's doors are separate (the story moves them).</summary>
    public static class ParkBuilder
    {
        public const string SetPath = "Assets/Art/Park/ParkSet.fbx";
        public const float RoofY = 10f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Park").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("ParkSet.fbx missing - run tools/blender/build_park_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "ParkSet";
            StoryBuilder.AlignAxes(set.transform);
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Park/Story_{n}.png");
                var m = EnvironmentBuilder.TexMat("Park_" + n, "Unlit/Texture", tex, Vector2.one);
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            var hidden = PrototypeBuilder.Mat("ParkGroundHidden", "Unlit/Color", Color.black);
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(65f, -2f, 0f), new Vector3(180f, 4f, 40f), hidden);
            floor.GetComponent<Renderer>().enabled = false;

            // the pond's still water and the dew on the path reflect (one mirror render)
            var water = PrototypeBuilder.Mat("ParkPond", "ProjectInk/WetFloor", Color.white);
            water.SetTexture("_WetTex", Texture2D.whiteTexture);
            water.SetFloat("_Base", 0.5f);
            water.SetFloat("_Puddle", 0.5f);
            water.SetFloat("_Ripple", 0.0012f);
            var dew = PrototypeBuilder.Mat("ParkDew", "ProjectInk/WetFloor", Color.white);
            dew.SetTexture("_WetTex", EnvironmentBuilder.SaveTex("ParkWetMask", StoryBuilder.WetMask(256)));
            dew.SetFloat("_WetScale", 0.08f);
            dew.SetFloat("_Base", 0.06f);
            dew.SetFloat("_Puddle", 0.3f);
            dew.SetFloat("_Ripple", 0.0004f);
            var path = FloorQuad(env, "DewPath", new Vector3(65f, 0.003f, 0f), new Vector2(130f, 2.4f), dew);
            var pond = FloorQuad(env, "PondWater", new Vector3(85f, -0.045f, 3.2f), new Vector2(18f, 6f), water);
            var pr = path.gameObject.AddComponent<PlanarReflection>();
            pr.shareWith = new[] { pond.GetComponent<Renderer>() };
            pond.gameObject.layer = 4;

            // the park lamps' cones, a mist over the grass and the water, dust (moths) in the light
            var coneTex = EnvironmentBuilder.SaveTex("ParkCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("ParkGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("ParkLampCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.34f, 0.34f, 0.34f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("ParkLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            foreach (float x in new[] { 37f, 50f, 66f, 74f, 98f, 104f })
            {
                StoryBuilder.Quad(env, "LampCone", new Vector3(x, 1.85f, -1.3f), new Vector2(3.4f, 3.7f), coneMat);
                StoryBuilder.Quad(env, "LampGlow", new Vector3(x, 3.75f, -1.6f), new Vector2(1.4f, 1.4f), glowMat);
            }
            StoryBuilder.Quad(env, "CarparkCone", new Vector3(124f, 2.5f, -0.8f), new Vector2(4.6f, 5.0f), coneMat);
            StoryBuilder.Quad(env, "KitchenGlow", new Vector3(0.6f, 1.3f, 0.2f), new Vector2(3.0f, 2.6f), glowMat);
            var hazeMat = EnvironmentBuilder.TexMat("ParkMist", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "GardenMist", new Vector3(20f, 0f, 4f), 30f, 0.9f, hazeMat);
            StoryBuilder.HazeStrip(env, "ParkMist", new Vector3(55f, 0f, 5f), 40f, 1.1f, hazeMat);
            StoryBuilder.HazeStrip(env, "PondMist", new Vector3(85f, 0f, 3.5f), 26f, 1.3f, hazeMat);
            EnvironmentBuilder.Dust(env, new EnvironmentBuilder.Layout { halfW = 30f, roofY = 5f, wallZ = 6f }, glowTex);
            var dust = env.Find("Dust");
            if (dust != null) dust.position = new Vector3(65f, 2.5f, 0f);

            // things to use: bins at the gardens, park crates in the playground
            var kit = new GameObject("ParkKit").transform;
            kit.SetParent(env, false);
            var wood = PrototypeBuilder.Mat("ParkCrateWood", "Unlit/Color", new Color(0.22f, 0.22f, 0.22f));
            var edge = PrototypeBuilder.Mat("ParkCrateEdge", "Unlit/Color", new Color(0.5f, 0.5f, 0.5f));
            foreach (var cx in new[] { 42f, 57f, 108f })
                EnvironmentBuilder.Crate(kit, new Vector3(cx, 0f, 0f), 0.6f, wood, edge);
            var bin = PrototypeBuilder.Mat("ParkBin", "Unlit/Color", new Color(0.14f, 0.14f, 0.15f));
            var hoop = PrototypeBuilder.Mat("ParkBinHoop", "Unlit/Color", new Color(0.55f, 0.55f, 0.55f));
            var lid = PrototypeBuilder.Mat("ParkBinLid", "Unlit/Color", new Color(0.05f, 0.05f, 0.06f));
            foreach (var bx in new[] { 6.0f, 32.6f, 68.0f, 112.0f })
                StoryBuilder.Drum(kit, new Vector3(bx, 0f, 0f), bin, hoop, lid);

            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(30f, 0f, 0f));
            heroStart = new Vector3(2.2f, 0f, 0f);
        }

        static Transform FloorQuad(Transform parent, string name, Vector3 at, Vector2 size, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.position = at;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }
    }
}
