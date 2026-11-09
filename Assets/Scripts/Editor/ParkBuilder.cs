using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 4: THE BACKYARD (docs/story/story_v2.md). Puts the baked night set together
    /// (tools/blender/build_park_set.py, laid out by ParkPlan): the back of No. 14 and its garden, the
    /// play area and the swing, the park's playground, the pond and its footbridge, the car park, the
    /// white van at its exit, the hill road, and Kessel Hill with the fortress lit under the storm.
    /// One flat floor for the whole lane; the park path, the tarmac and the pond shine wet (one
    /// mirror render feeds them all); the lamps' cones and glows, the kitchen's light, low mist over the
    /// grass and the water. The horizon has a second bake lit by a far strike (LightningSwap).
    /// The moving parts: "Swing" (the seat, its origin the pivot), "Van" (origin on the ground under
    /// its centre) with "VanDoors" holding "VanDoorLHinge"/"VanDoorRHinge" (identity-rotated pivots on
    /// the hinge lines, the door meshes "VanDoorL"/"VanDoorR" under them) and the "VanBackWindow"
    /// marker; "BarrierArm" (origin on its hinge).</summary>
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
            var swapR = new List<Renderer>();
            var swapN = new List<Material>();
            var swapF = new List<Material>();
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
                var flash = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Park/Story_{n}_Lightning.png");
                if (flash != null)
                {
                    swapR.Add(r); swapN.Add(m);
                    swapF.Add(EnvironmentBuilder.TexMat("Park_" + n + "_Lightning", "Unlit/Texture", flash, Vector2.one));
                }
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            if (swapR.Count > 0)
            {
                var swap = room.gameObject.AddComponent<LightningSwap>();
                swap.renderers = swapR.ToArray(); swap.normal = swapN.ToArray(); swap.flash = swapF.ToArray();
            }
            RigMovingParts(set.transform);

            // one flat floor under the whole lane (the footbridge's deck is at the lane's height too)
            var hidden = PrototypeBuilder.Mat("ParkGroundHidden", "Unlit/Color", Color.black);
            float x0 = ParkPlan.LaneX0 - 14f, x1 = ParkPlan.LaneX1 + 40f;
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3((x0 + x1) * 0.5f, -2f, 0f), new Vector3(x1 - x0, 4f, 40f), hidden);
            floor.GetComponent<Renderer>().enabled = false;

            // wet: the park path and the tarmac shine (the puddles strongly), the pond's still water
            // mirrors the lamps and the sky; one mirror render feeds them all (the pond sits 0.24 below)
            var mask = EnvironmentBuilder.SaveTex("ParkWetMask", StoryBuilder.WetMask(256));
            Material Wet(string name, float b, float p, float ripple, Texture2D t)
            {
                var m = PrototypeBuilder.Mat(name, "ProjectInk/WetFloor", Color.white);
                m.SetTexture("_WetTex", t);
                m.SetFloat("_WetScale", 0.08f);
                m.SetFloat("_Base", b);
                m.SetFloat("_Puddle", p);
                m.SetFloat("_Ripple", ripple);
                return m;
            }
            var dew = Wet("ParkDew", 0.05f, 0.25f, 0.0004f, mask);
            var path = FloorQuad(env, "WetPath", new Vector3((ParkPlan.GateX + ParkPlan.BridgeX0) * 0.5f, 0.025f, 0f), new Vector2(ParkPlan.BridgeX0 - ParkPlan.GateX, 2.6f), dew);
            var path2 = FloorQuad(env, "WetPath2", new Vector3((ParkPlan.BridgeX1 + 98f) * 0.5f, 0.025f, 0f), new Vector2(98f - ParkPlan.BridgeX1, 2.6f), dew);
            var patio = FloorQuad(env, "WetPatio", new Vector3(3.6f, 0.025f, 0.3f), new Vector2(7.2f, 7.4f), Wet("ParkPatioWet", 0.08f, 0.35f, 0.0004f, mask));
            var tarmac = FloorQuad(env, "WetTarmac", new Vector3(119.5f, 0.004f, 0.25f), new Vector2(41f, 18.5f), Wet("ParkTarmacWet", 0.14f, 0.6f, 0.0006f, mask));
            var water = Wet("ParkPond", 0.55f, 0.55f, 0.0012f, Texture2D.whiteTexture);
            var pond = FloorQuad(env, "PondWater", ParkPlan.Pond + Vector3.up * 0.005f, new Vector2(ParkPlan.PondHalfX * 1.66f, ParkPlan.PondHalfZ * 1.66f), water);
            var pr = tarmac.gameObject.AddComponent<PlanarReflection>();
            pr.shareWith = new[] { path.GetComponent<Renderer>(), path2.GetComponent<Renderer>(), patio.GetComponent<Renderer>(), pond.GetComponent<Renderer>() };
            foreach (var t in new[] { path, path2, patio, pond }) t.gameObject.layer = 4;

            // the light: the lamps' cones and glows, the car park's sodium heads, the kitchen window and
            // the open back door, the fairy lights over her play area; mist over the grass and the water
            var coneTex = EnvironmentBuilder.SaveTex("ParkCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("ParkGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("ParkLampCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.34f, 0.34f, 0.34f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("ParkLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            var softMat = EnvironmentBuilder.TexMat("ParkSoftGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.22f, 0.22f, 0.22f, 0.5f));
            var lamps = new GameObject("ParkLamps").transform;
            lamps.SetParent(env, false);
            foreach (var l in ParkPlan.Lamps)
            {
                float h = ParkPlan.LampY;
                StoryBuilder.Quad(lamps, "LampCone", new Vector3(l.x, h * 0.5f, l.y - 0.1f), new Vector2(h * 0.95f, h), coneMat);
                StoryBuilder.Quad(lamps, "LampGlow", new Vector3(l.x, h, l.y - 0.3f), new Vector2(1.4f, 1.4f), glowMat);
            }
            foreach (var l in ParkPlan.CarLamps)
                for (int s = -1; s <= 1; s += 2)
                {
                    float h = ParkPlan.CarLampY;
                    StoryBuilder.Quad(lamps, "CarparkCone", new Vector3(l.x + s * 1.0f, h * 0.5f, l.y - 0.4f), new Vector2(h * 0.8f, h), coneMat);
                    StoryBuilder.Quad(lamps, "CarparkGlow", new Vector3(l.x + s * 1.0f, h - 0.05f, l.y - 0.5f), new Vector2(1.6f, 1.2f), glowMat);
                }
            var kw = ParkPlan.KitchenWindow;
            var kitchen = StoryBuilder.Quad(env, "KitchenGlow", new Vector3(kw.x + 0.3f, kw.y, kw.z), new Vector2(2.6f, 2.2f), glowMat);
            kitchen.rotation = Quaternion.Euler(0f, 90f, 0f);
            var bd = ParkPlan.BackDoor;
            var door = StoryBuilder.Quad(env, "BackDoorGlow", new Vector3(bd.x + 0.3f, 1.0f, bd.z), new Vector2(2.0f, 2.6f), glowMat);
            door.rotation = Quaternion.Euler(0f, 90f, 0f);
            StoryBuilder.Quad(env, "BackDoorSpill", new Vector3(2.0f, 0.9f, -0.3f), new Vector2(3.6f, 1.8f), softMat);
            for (int i = 0; i < 4; i++)
                StoryBuilder.Quad(env, "FairyGlow", new Vector3(19.8f + i * 4.0f, 1.8f, 12.2f), new Vector2(4.4f, 1.0f), softMat);
            var hazeMat = EnvironmentBuilder.TexMat("ParkMist", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "GardenMist", new Vector3(19f, 0f, 4.5f), 30f, 0.9f, hazeMat);
            StoryBuilder.HazeStrip(env, "ParkMist", new Vector3(50f, 0f, 5.5f), 32f, 1.1f, hazeMat);
            StoryBuilder.HazeStrip(env, "PondMist", new Vector3(ParkPlan.Pond.x, ParkPlan.Pond.y, 4.0f), 26f, 1.5f, hazeMat);
            StoryBuilder.HazeStrip(env, "CarparkMist", new Vector3(116f, 0f, 6f), 36f, 0.8f, hazeMat);
            StoryBuilder.HazeStrip(env, "FarMist", new Vector3(65f, 0f, 16f), 140f, 3.0f, hazeMat);
            EnvironmentBuilder.Dust(env, new EnvironmentBuilder.Layout { halfW = 30f, roofY = 5f, wallZ = 6f }, glowTex);
            var dust = env.Find("Dust");
            if (dust != null) dust.position = new Vector3(65f, 2.5f, 0f);

            // things to use: bins in the garden and by the gates, park crates in the playground
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

        /// <summary>Clean pivots for the parts the story moves: the van (driven away along the hill
        /// road), its back doors (swung on their hinges), the marker of its back window.</summary>
        static void RigMovingParts(Transform set)
        {
            Transform Find(string n)
            {
                foreach (var t in set.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
                return null;
            }
            var van = Find("Van");
            if (van == null) { Debug.LogWarning("[Stick Wick] ParkSet: Van missing"); return; }
            var doors = new GameObject("VanDoors").transform;
            doors.SetParent(van, true);
            doors.SetPositionAndRotation(new Vector3(ParkPlan.VanX - ParkPlan.VanLength * 0.5f, 1.4f, ParkPlan.VanZ), Quaternion.identity);
            foreach (var (name, hinge) in new[] { ("VanDoorL", ParkPlan.VanHingeL), ("VanDoorR", ParkPlan.VanHingeR) })
            {
                var pivot = new GameObject(name + "Hinge").transform;
                pivot.SetParent(doors, true);
                pivot.SetPositionAndRotation(hinge, Quaternion.identity);
                var d = Find(name);
                if (d != null) d.SetParent(pivot, true);
            }
            var win = new GameObject("VanBackWindow").transform;
            win.SetParent(van, true);
            win.SetPositionAndRotation(ParkPlan.VanWindow, Quaternion.identity);
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
