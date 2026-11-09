using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 5: THE ROAD (docs/story/story_v2.md). Puts the night drive up Kessel Hill together
    /// (tools/blender/build_road_set.py): the hairpins, the cuttings, the barriers and the pines, the
    /// valley's lights, the fortress at the top. Every lit part has two bakes, the storm night and lit by
    /// a strike, and LightningSwap flickers between them. RoadCar (the exterior, parked at the stop) gets
    /// its headlight beams (children "Headlight*"); CarInterior stays where the story films it
    /// (RoadPlan.InteriorOrigin), with its wipers ("WiperL"/"WiperR": clean pivots on their spindles) and
    /// the windscreen glass (transparent: the rain is the story's). The top road shines wet; mist on the
    /// hill; the floodlights' cones. Floors where people stand: the top road and the stop.</summary>
    public static class RoadBuilder
    {
        public const string SetPath = "Assets/Art/Road/RoadSet.fbx";
        public const float RoofY = 60f;
        public const float LaneX0 = -60f, LaneX1 = 50f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Road").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("RoadSet.fbx missing - run tools/blender/build_road_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "RoadSet";
            StoryBuilder.AlignAxes(set.transform);

            var swapR = new List<Renderer>();
            var swapN = new List<Material>();
            var swapF = new List<Material>();
            var glass = PrototypeBuilder.Mat("RoadWindscreen", "Sprites/Default", new Color(0.75f, 0.8f, 0.85f, 0.07f));
            var cityLights = PrototypeBuilder.Mat("RoadCityLights", "Unlit/Color", new Color(0.92f, 0.92f, 0.9f));
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                Material m;
                if (n == "Windscreen") m = glass;
                else if (n == "RoadCityLights") m = cityLights;
                else
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Road/Story_{n}.png");
                    m = EnvironmentBuilder.TexMat("Road_" + n, "Unlit/Texture", tex, Vector2.one);
                    var flash = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Road/Story_{n}_Lightning.png");
                    if (flash != null)
                    {
                        swapR.Add(r); swapN.Add(m);
                        swapF.Add(EnvironmentBuilder.TexMat("Road_" + n + "_Lightning", "Unlit/Texture", flash, Vector2.one));
                    }
                }
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var swap = room.gameObject.AddComponent<LightningSwap>();
            swap.renderers = swapR.ToArray(); swap.normal = swapN.ToArray(); swap.flash = swapF.ToArray();

            Transform Find(string name)
            {
                foreach (var t in set.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                return null;
            }

            // the interior: parked at InteriorOrigin; the wipers get clean pivots (identity, on the spindle)
            // so the story can turn them: wiper.localRotation = Quaternion.AngleAxis(a, RoadPlan.WiperAxis)
            var cab = Find("CarInterior");
            foreach (var wn in new[] { "WiperL", "WiperR" })
            {
                var w = Find(wn);
                if (w == null) continue;
                var pivot = new GameObject(wn).transform;
                pivot.SetParent(room, false);
                pivot.position = w.position;
                pivot.rotation = Quaternion.identity;
                if (cab != null) pivot.SetParent(cab, true);
                w.name = wn + "_Mesh";
                w.SetParent(pivot, true);
            }

            // the car outside: its headlights (beams and the pool on the road ahead), children of RoadCar,
            // set up while it stands at the stop facing +x
            var car = Find("RoadCar");
            if (car != null)
            {
                var beamTex = EnvironmentBuilder.SaveTex("RoadBeam", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
                var glowTex = EnvironmentBuilder.SaveTex("RoadGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
                var beamMat = EnvironmentBuilder.TexMat("RoadHeadBeam", "Legacy Shaders/Particles/Additive", beamTex, Vector2.one, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                var lampMat = EnvironmentBuilder.TexMat("RoadHeadGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.6f, 0.6f, 0.6f, 0.5f));
                var poolMat = EnvironmentBuilder.TexMat("RoadHeadPool", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.22f, 0.22f, 0.22f, 0.5f));
                var lights = new GameObject("Headlights").transform;
                lights.SetParent(room, false);
                lights.position = car.position;
                lights.rotation = Quaternion.identity;
                foreach (float side in new[] { 1f, -1f })
                {
                    var lamp = RoadPlan.HeadlampLocal; lamp.z *= side;
                    string nm = side > 0f ? "HeadlightL" : "HeadlightR";
                    var beam = BeamMesh(lights, nm, 22f, 0.25f, 6.5f, beamMat);
                    beam.localPosition = lamp;
                    beam.localRotation = Quaternion.Euler(0f, 0f, -3f);
                    var glow = StoryBuilder.Quad(lights, nm + "Glow", Vector3.zero, new Vector2(0.9f, 0.6f), lampMat);
                    glow.localPosition = lamp + new Vector3(0.05f, 0f, 0f);
                    glow.localRotation = Quaternion.Euler(0f, -90f, 0f);
                }
                var pool = StoryBuilder.Quad(lights, "HeadlightPool", Vector3.zero, new Vector2(16f, 6f), poolMat);
                pool.localPosition = new Vector3(11f, 0.05f, 0f);
                pool.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lights.SetParent(car, true);               // rides with the car (whatever the import's axes)
            }

            // the floor where people stand: the top road and the stop (the climb below has no floor: the
            // car is driven along RoadPlan.Path)
            var hidden = PrototypeBuilder.Mat("RoadGroundHidden", "Unlit/Color", Color.black);
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(-5f, -2f, 0f), new Vector3(110f, 4f, 9f), hidden);
            floor.GetComponent<Renderer>().enabled = false;

            // wet: the top road shines in the floodlight, rippling in the rain
            var mask = EnvironmentBuilder.SaveTex("RoadWetMask", StoryBuilder.WetMask(256));
            var wet = PrototypeBuilder.Mat("RoadWetTop", "ProjectInk/WetFloor", Color.white);
            wet.SetTexture("_WetTex", mask);
            wet.SetFloat("_WetScale", 0.08f);
            wet.SetFloat("_Base", 0.18f);
            wet.SetFloat("_Puddle", 0.75f);
            wet.SetFloat("_Ripple", 0.004f);
            var wetRoad = FloorQuad(env, "WetRoad", new Vector3(0f, 0.03f, -0.2f), new Vector2(96f, 5.0f), wet);
            wetRoad.gameObject.AddComponent<PlanarReflection>();

            // the floodlights' cones (the stop, the cliff road), mist on the top and low cloud on the hill
            var coneTex = EnvironmentBuilder.SaveTex("RoadCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowT = EnvironmentBuilder.SaveTex("RoadFloodGlowTex", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("RoadFloodCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.36f, 0.36f, 0.36f, 0.5f));
            var fglowMat = EnvironmentBuilder.TexMat("RoadFloodGlow", "Legacy Shaders/Particles/Additive", glowT, Vector2.one, new Color(0.6f, 0.6f, 0.6f, 0.5f));
            var floods = new GameObject("Floodlights").transform;
            floods.SetParent(env, false);
            foreach (var (pos, nm) in new[] { (RoadPlan.StopFloodlight, "Stop"), (new Vector3(38f, 5.9f, 1.4f), "Road") })
            {
                StoryBuilder.Quad(floods, "FloodCone_" + nm, new Vector3(pos.x - 1.2f, pos.y * 0.5f, pos.z - 2.2f), new Vector2(pos.y * 0.95f, pos.y), coneMat);
                StoryBuilder.Quad(floods, "FloodGlow_" + nm, new Vector3(pos.x, pos.y - 0.1f, pos.z - 0.9f), new Vector2(2.0f, 2.0f), fglowMat);
            }
            var hazeMat = EnvironmentBuilder.TexMat("RoadMist", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "TopMist", new Vector3(0f, 0f, 3.2f), 100f, 1.6f, hazeMat);
            StoryBuilder.HazeStrip(env, "StopMist", new Vector3(-12f, 0f, -3.6f), 30f, 0.9f, hazeMat);
            for (int k = 0; k < 5; k++)
            {
                // low cloud lying along each leg's drop side, thicker further down
                float y = -12f - k * 12f, z = -6f - k * 18f;
                StoryBuilder.HazeStrip(env, "HillMist" + k, new Vector3(-80f, y, z), 170f, 4f + k * 1.5f, hazeMat);
            }

            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(20f, 0f, 0f));
            heroStart = RoadPlan.GetOutSpot;
        }

        /// <summary>A headlight's beam: two crossed quads from a narrow end at the lamp to a wide end
        /// `length` ahead along local +x, bright at the lamp (the cone texture, its tip up).</summary>
        static Transform BeamMesh(Transform parent, string name, float length, float w0, float w1, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            foreach (var axis in new[] { Vector3.up, Vector3.forward })
            {
                int k = v.Count;
                v.Add(-axis * w0 * 0.5f); v.Add(axis * w0 * 0.5f);
                v.Add(axis * w1 * 0.5f + Vector3.right * length); v.Add(-axis * w1 * 0.5f + Vector3.right * length);
                uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 0));
                tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            string path = $"Assets/Generated/Road_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        internal static Transform FloorQuad(Transform parent, string name, Vector3 at, Vector2 size, Material m)
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
