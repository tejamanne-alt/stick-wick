using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 3: HOME (docs/story/chapter3_home.md). Puts the baked house together
    /// (tools/blender/build_home_set.py): Alder Lane at dusk, No. 14 cut away side-on, two floors,
    /// the garden and the play area behind. The front wall is cut away while the camera is inside
    /// its line (the story shows it whole for the street shots). The floors: the ground and an
    /// upstairs slab (fighters stand on whichever is under them). The ground floor's boards reflect
    /// softly; the low sun comes through the back windows in shafts; a haze, dust in the light.
    /// Props: the glass coffee table (a man thrown down on it goes through it), the lamp, the chair
    /// and its flex for the kitchen (StoryHome), the swing seat (it moves).</summary>
    public static class HomeBuilder
    {
        public const string SetPath = "Assets/Art/Home/HomeSet.fbx";
        public const float RoofY = 8.5f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Home").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("HomeSet.fbx missing - run tools/blender/build_home_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "HomeSet";
            StoryBuilder.AlignAxes(set.transform);
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Home/Story_{n}.png");
                var m = EnvironmentBuilder.TexMat("Home_" + n, "Unlit/Texture", tex, Vector2.one);
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                if (n == "HomeFront") r.gameObject.AddComponent<CutawayWall>().insideZ = -2.3f;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            // the floors: the ground, and the upstairs slab over the house
            var hidden = PrototypeBuilder.Mat("HomeGroundHidden", "Unlit/Color", Color.black);
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(10f, -2f, 0f), new Vector3(160f, 4f, 40f), hidden);
            floor.GetComponent<Renderer>().enabled = false;
            var slab = new GameObject("Upstairs").transform;
            slab.SetParent(room, false);
            slab.position = new Vector3((HomePlan.HouseX0 + HomePlan.HouseX1) * 0.5f, HomePlan.Floor2 - 0.05f, 0.2f);
            slab.gameObject.AddComponent<BoxCollider>().size = new Vector3(HomePlan.HouseX1 - HomePlan.HouseX0, 0.1f, 4.4f);
            slab.gameObject.isStatic = true;

            // the boards downstairs reflect, softly (the tower's floors)
            var boards = PrototypeBuilder.Mat("HomeBoards", "ProjectInk/WetFloor", Color.white);
            boards.SetTexture("_WetTex", Texture2D.whiteTexture);
            boards.SetFloat("_Base", 0.12f);
            boards.SetFloat("_Puddle", 0.12f);
            boards.SetFloat("_Ripple", 0f);
            var shine = FloorQuad(env, "BoardShine", new Vector3(17f, 0.004f, 1.1f), new Vector2(34f, 6.2f), boards);
            shine.gameObject.AddComponent<PlanarReflection>();

            // the low sun through the back windows, the lamps' glow, a haze, dust in the light
            var coneTex = EnvironmentBuilder.SaveTex("HomeCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("HomeGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var shaftMat = EnvironmentBuilder.TexMat("HomeShaft", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("HomeLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.45f, 0.45f, 0.45f, 0.5f));
            foreach (var (x, y) in new[] { (10.4f, 1.6f), (21.6f, 1.6f), (28.6f, 1.8f), (12.4f, HomePlan.Floor2 + 1.6f), (18.0f, HomePlan.Floor2 + 1.5f), (30.8f, HomePlan.Floor2 + 1.7f) })
            {
                var s = StoryBuilder.Quad(env, "SunShaft", new Vector3(x - 1.0f, y - 0.6f, 2.6f), new Vector2(1.8f, 3.2f), shaftMat);
                s.rotation = Quaternion.Euler(0f, 0f, 38f);
            }
            foreach (var p in new[] { new Vector3(12.7f, 1.0f, 3.3f), new Vector3(24.2f, 1.65f, 3.2f), new Vector3(26.8f, HomePlan.Floor2 + 0.8f, 3.6f), new Vector3(18.0f, HomePlan.Floor2 + 2.3f, 3.9f) })
                StoryBuilder.Quad(env, "LampGlow", p + Vector3.back * 0.1f, new Vector2(1.3f, 1.3f), glowMat);
            var hazeMat = EnvironmentBuilder.TexMat("HomeHaze", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "DownHaze", new Vector3(17f, 0f, 3.6f), 34f, 1.0f, hazeMat);
            StoryBuilder.HazeStrip(env, "UpHaze", new Vector3(17f, HomePlan.Floor2, 3.6f), 34f, 0.9f, hazeMat);
            StoryBuilder.HazeStrip(env, "StreetHaze", new Vector3(-12f, 0f, 6f), 26f, 1.4f, hazeMat);
            EnvironmentBuilder.Dust(env, new EnvironmentBuilder.Layout { halfW = 16f, roofY = 5.5f, wallZ = 4f }, glowTex);
            var dust = env.Find("Dust");
            if (dust != null) dust.position = new Vector3(17f, 2.8f, 1.5f);

            // the glass coffee table in the living room: a pale top on a black frame
            var glass = PrototypeBuilder.Mat("HomeTableGlass", "Unlit/Color", new Color(0.62f, 0.64f, 0.66f));
            var frameMat = PrototypeBuilder.Mat("HomeTableFrame", "Unlit/Color", new Color(0.08f, 0.08f, 0.09f));
            var table = new GameObject("CoffeeTable").transform;
            table.SetParent(env, false);
            table.position = new Vector3(HomePlan.CoffeeTableX, 0f, 1.2f);
            var top = PrototypeBuilder.Box(table, "Top", Vector3.zero, new Vector3(1.4f, 0.04f, 0.7f), glass);
            top.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            Object.DestroyImmediate(top.GetComponent<Collider>());
            foreach (var dx in new[] { -0.62f, 0.62f })
                foreach (var dz in new[] { -0.28f, 0.28f })
                {
                    var leg = PrototypeBuilder.Box(table, "Leg", Vector3.zero, new Vector3(0.05f, 0.42f, 0.05f), frameMat);
                    leg.transform.localPosition = new Vector3(dx, 0.21f, dz);
                    Object.DestroyImmediate(leg.GetComponent<Collider>());
                }
            table.gameObject.AddComponent<GlassTable>();

            // spawn points (the story moves them room by room)
            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(30f, 0f, 0f));
            heroStart = new Vector3(HomePlan.StreetX0 + 2f, 0f, 0f);
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
