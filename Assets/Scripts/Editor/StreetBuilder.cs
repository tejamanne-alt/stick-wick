using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 2, part two: THE STREET (docs/design/combat_v2.md, part 2). Puts the baked set
    /// together (tools/blender/build_street_set.py): the bank's steps, Cafe Noir, the market, the
    /// railway arches and the crossroads, in the same black and white as the warehouse, the bank
    /// and the tower. No rain: the wet tarmac and the puddles reflect (one mirror render for the
    /// sidewalk and the road), the railway's strip lamps throw the tower's cones, a low haze, dust.
    /// Street furniture to use: fruit crates to kick, bins (the drums) to send rolling. The Driver's
    /// car and the train are separate objects the story moves (StoryStreet.cs).</summary>
    public static class StreetBuilder
    {
        public const string SetPath = "Assets/Art/Street/StreetSet.fbx";
        public const float RoofY = 12f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Street").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("StreetSet.fbx missing - run tools/blender/build_street_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "StreetSet";
            StoryBuilder.AlignAxes(set.transform);
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Street/Story_{n}.png");
                var m = EnvironmentBuilder.TexMat("Street_" + n, "Unlit/Texture", tex, Vector2.one);
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            // the floor: one level along the sidewalk
            var ground = PrototypeBuilder.Mat("StreetGroundHidden", "Unlit/Color", Color.black);
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(60f, -2f, 0f), new Vector3(200f, 4f, 40f), ground);
            floor.GetComponent<Renderer>().enabled = false;

            // the wet street: the sidewalk and the road below it, sharing one mirror render
            var mask = EnvironmentBuilder.SaveTex("StreetWetMask", StoryBuilder.WetMask(256));
            var wet = PrototypeBuilder.Mat("StreetWet", "ProjectInk/WetFloor", Color.white);
            wet.SetTexture("_WetTex", mask);
            wet.SetFloat("_WetScale", 0.07f);
            wet.SetFloat("_Base", 0.12f);
            wet.SetFloat("_Puddle", 0.6f);
            wet.SetFloat("_Ripple", 0.0006f);                  // no rain: still water
            var road = PrototypeBuilder.Mat("StreetRoadWet", "ProjectInk/WetFloor", Color.white);
            road.SetTexture("_WetTex", mask);
            road.SetFloat("_WetScale", 0.05f);
            road.SetFloat("_Base", 0.2f);
            road.SetFloat("_Puddle", 0.7f);
            road.SetFloat("_Ripple", 0.0006f);
            var walk = FloorQuad(env, "WetSidewalk", new Vector3(64f, 0.003f, 2.0f), new Vector2(160f, 8.4f), wet);
            var tarmac = FloorQuad(env, "WetRoad", new Vector3(64f, -0.147f, -9f), new Vector2(160f, 13.4f), road);
            var pr = walk.gameObject.AddComponent<PlanarReflection>();
            pr.shareWith = new[] { tarmac.GetComponent<Renderer>() };
            tarmac.gameObject.layer = 4;

            // the light: cones under the railway's strip lamps and the street lamps, haze, dust
            var coneTex = EnvironmentBuilder.SaveTex("StreetCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("StreetGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("StreetLampCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.36f, 0.36f, 0.36f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("StreetLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            foreach (float x in new[] { StreetPlan.RailX0 + 5f, StreetPlan.RailX0 + 14f, StreetPlan.RailX0 + 23f })
            {
                StoryBuilder.Quad(env, "RailCone", new Vector3(x, 3.1f, 1.2f), new Vector2(5.0f, 6.2f), coneMat);
                StoryBuilder.Quad(env, "RailGlow", new Vector3(x, StreetPlan.DeckY - 0.3f, 0.9f), new Vector2(3.2f, 1.2f), glowMat);
            }
            var hazeMat = EnvironmentBuilder.TexMat("StreetHaze", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "MorningHaze", new Vector3(30f, 0f, 6f), 70f, 1.6f, hazeMat);
            StoryBuilder.HazeStrip(env, "RailHaze", new Vector3(77f, 0f, 4f), 30f, 2.2f, hazeMat);
            StoryBuilder.HazeStrip(env, "JunctionHaze", new Vector3(112f, 0f, 6f), 40f, 1.6f, hazeMat);
            EnvironmentBuilder.Dust(env, new EnvironmentBuilder.Layout { halfW = 14f, roofY = 6f, wallZ = 5f }, glowTex);
            var dust = env.Find("Dust");
            if (dust != null) dust.position = new Vector3(77f, 3f, 1.5f);

            // street furniture to fight with: fruit crates at the stalls and the scaffold, bins
            var kit = new GameObject("StreetKit").transform;
            kit.SetParent(env, false);
            var wood = PrototypeBuilder.Mat("StreetCrateWood", "Unlit/Color", new Color(0.26f, 0.26f, 0.26f));
            var edge = PrototypeBuilder.Mat("StreetCrateEdge", "Unlit/Color", new Color(0.55f, 0.55f, 0.55f));
            foreach (var cx in new[] { 35.2f, 39.0f, 42.8f, 55.5f, 57.0f })
                EnvironmentBuilder.Crate(kit, new Vector3(cx, 0f, 0f), 0.62f, wood, edge);
            var bin = PrototypeBuilder.Mat("StreetBin", "Unlit/Color", new Color(0.16f, 0.16f, 0.17f));
            var binDark = PrototypeBuilder.Mat("StreetBinDark", "Unlit/Color", new Color(0.09f, 0.09f, 0.1f));
            var hoop = PrototypeBuilder.Mat("StreetBinHoop", "Unlit/Color", new Color(0.62f, 0.62f, 0.62f));
            var lid = PrototypeBuilder.Mat("StreetBinLid", "Unlit/Color", new Color(0.05f, 0.05f, 0.06f));
            int nb = 0;
            foreach (var bx in new[] { 6.5f, 27.6f, 68.5f, 74.0f, 86.0f, 101.0f })
                StoryBuilder.Drum(kit, new Vector3(bx, 0f, 0f), nb++ % 2 == 0 ? bin : binDark, hoop, lid);

            // spawn points (the story moves them zone by zone)
            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(20f, 0f, 0f));
            heroStart = new Vector3(StreetPlan.BankDoorX + 2f, 0f, 0f);
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
