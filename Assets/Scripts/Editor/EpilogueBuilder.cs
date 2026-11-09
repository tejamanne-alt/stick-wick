using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 7: HOME, the epilogue (docs/story/story_v2.md). Puts the morning set together
    /// (tools/blender/build_epilogue_set.py): three places along x, each in its own sky (EpiloguePlan):
    /// the bottom of the cliff road with the police (the light bars' lenses "PoliceLight_0..7" are plain
    /// white, each with a glow, for the story to flash), the hillside cemetery and Elena's stone, and No. 14:
    /// the garden with the swing and, inside the back door, the hall with the drawer under her photo.
    /// The moving parts get clean pivots (identity rotation, parented to the room): "SwingSeat" (on the top
    /// bar; turn it about EpiloguePlan.SwingAxis), "BackDoor" (on the hinge; built open, closed at
    /// Euler(0, BackDoorOpen, 0)), "HallDrawerFront" (slides along +x). Floors where people stand; the
    /// wet road and the patio reflect; mist on the road, low mist between the graves.</summary>
    public static class EpilogueBuilder
    {
        public const string SetPath = "Assets/Art/Epilogue/EpilogueSet.fbx";
        public const float RoofY = 12f;
        public const float LaneX0 = -60f, LaneX1 = 1030f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Epilogue").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("EpilogueSet.fbx missing - run tools/blender/build_epilogue_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "EpilogueSet";
            StoryBuilder.AlignAxes(set.transform);

            var lens = PrototypeBuilder.Mat("EpiPoliceLens", "Unlit/Color", new Color(0.96f, 0.96f, 0.96f));
            var glowTex = EnvironmentBuilder.SaveTex("EpiGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var glowMat = EnvironmentBuilder.TexMat("EpiPoliceGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.7f, 0.7f, 0.7f, 0.5f));
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                Material m;
                if (n.StartsWith("PoliceLight"))
                {
                    m = lens;
                    // the flash's glow, a child of the lens (the story switches both)
                    var g = StoryBuilder.Quad(room, n + "_Glow", r.bounds.center + new Vector3(0f, 0.05f, -0.3f), new Vector2(1.6f, 1.0f), glowMat);
                    g.SetParent(r.transform, true);
                }
                else
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Epilogue/Story_{n}.png");
                    m = EnvironmentBuilder.TexMat("Epi_" + n, "Unlit/Texture", tex, Vector2.one);
                }
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            Transform Find(string name)
            {
                foreach (var t in set.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                return null;
            }
            // clean pivots for the moving parts: the mesh goes under a plain transform of the same name
            // (identity rotation, unit scale), so the story turns or slides that
            foreach (var pn in new[] { "SwingSeat", "BackDoor", "HallDrawerFront", "ElenaStone" })
            {
                var t = Find(pn);
                if (t == null) continue;
                var pivot = new GameObject(pn).transform;
                pivot.SetParent(room, false);
                pivot.position = t.position;
                pivot.rotation = Quaternion.identity;
                t.name = pn + "_Mesh";
                t.SetParent(pivot, true);
            }

            // floors where people stand: the road, the cemetery's front (the path and Elena's row), the
            // garden and the patio, the hall (a step up)
            var hidden = PrototypeBuilder.Mat("EpiGroundHidden", "Unlit/Color", Color.black);
            void Floor(string name, Vector3 centre, Vector3 size)
            {
                var b = PrototypeBuilder.Box(room, name, centre, size, hidden);
                b.GetComponent<Renderer>().enabled = false;
            }
            var A = EpiloguePlan.RoadOrigin; var B = EpiloguePlan.CemeteryOrigin; var C = EpiloguePlan.HomeOrigin;
            Floor("GroundRoad", A + new Vector3(0f, -2f, 0f), new Vector3(120f, 4f, 13f));
            Floor("GroundCemetery", B + new Vector3(0f, -2f, -1.5f), new Vector3(66f, 4f, 7f));
            Floor("GroundGarden", C + new Vector3(6f, -2f, -1.5f), new Vector3(28f, 4f, 16f));
            Floor("GroundPatio", C + new Vector3(2f, -1.95f, 8.3f), new Vector3(16f, 4f, 3.4f));
            Floor("GroundHall", new Vector3((EpiloguePlan.HallX0 + EpiloguePlan.HallX1) * 0.5f, EpiloguePlan.HallFloorY - 2f, (EpiloguePlan.HallZ0 + EpiloguePlan.HallZ1) * 0.5f),
                  new Vector3(EpiloguePlan.HallX1 - EpiloguePlan.HallX0, 4f, EpiloguePlan.HallZ1 - EpiloguePlan.HallZ0));

            // the night's rain still on the road and the patio: they reflect the morning
            var mask = EnvironmentBuilder.SaveTex("EpiWetMask", StoryBuilder.WetMask(256));
            Material Wet(string name, float b, float p)
            {
                var m = PrototypeBuilder.Mat(name, "ProjectInk/WetFloor", Color.white);
                m.SetTexture("_WetTex", mask);
                m.SetFloat("_WetScale", 0.08f);
                m.SetFloat("_Base", b);
                m.SetFloat("_Puddle", p);
                m.SetFloat("_Ripple", 0.0006f);
                return m;
            }
            RoadBuilder.FloorQuad(env, "WetRoad", A + new Vector3(0f, 0.03f, 0f), new Vector2(120f, 6.2f), Wet("EpiWetRoad", 0.12f, 0.6f))
                .gameObject.AddComponent<PlanarReflection>();
            RoadBuilder.FloorQuad(env, "WetPatio", C + new Vector3(2f, 0.058f, 8.3f), new Vector2(16f, 3.3f), Wet("EpiWetPatio", 0.06f, 0.45f))
                .gameObject.AddComponent<PlanarReflection>();

            // mist: along the foot of the cliff, low between the graves (in layers up the slope), a breath
            // of it over the lawn
            var hazeMat = EnvironmentBuilder.TexMat("EpiMist", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "RoadMist", A + new Vector3(0f, 0f, 7f), 120f, 2.2f, hazeMat);
            StoryBuilder.HazeStrip(env, "ValleyMist", A + new Vector3(0f, -6f, -14f), 140f, 4f, hazeMat);
            for (int k = 0; k < 5; k++)
            {
                float z = 0.6f + k * 4.2f;
                float y = Mathf.Max(0f, z - 2.6f) * 0.17f;
                StoryBuilder.HazeStrip(env, "GraveMist" + k, B + new Vector3(0f, y, z), 66f, 1.0f + k * 0.15f, hazeMat);
            }
            StoryBuilder.HazeStrip(env, "CemeteryValleyMist", B + new Vector3(0f, -5f, -14f), 160f, 5f, hazeMat);
            StoryBuilder.HazeStrip(env, "GardenMist", C + new Vector3(6f, 0f, -6f), 30f, 0.6f, hazeMat);
            StoryBuilder.HazeStrip(env, "GardensMist", C + new Vector3(6f, 0f, -20f), 70f, 2.4f, hazeMat);

            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), A + new Vector3(30f, 0f, 0f));
            heroStart = EpiloguePlan.RoadSpot;
        }
    }
}
