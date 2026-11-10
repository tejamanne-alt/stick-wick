using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 5: THE FORTRESS (docs/story/chapter5_fortress.md). Puts the storm set together
    /// (tools/blender/build_fortress_set.py): every part has two bakes, the storm-dark night and lit by a
    /// strike, and LightningSwap flickers between them. The ground and the hill's level (6 m up, from
    /// the gate on) are solid; the hall's front wall is cut away while the camera is out; the stone and
    /// the road shine wet (the rain ripples them); the braziers', floodlights' and chandeliers' cones,
    /// a mist on the ground. The rain itself and the lightning are the story's (StoryFortress).</summary>
    public static class FortressBuilder
    {
        public const string SetPath = "Assets/Art/Fortress/FortressSet.fbx";
        public const float RoofY = 32f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Fortress").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("FortressSet.fbx missing - run tools/blender/build_fortress_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "FortressSet";
            StoryBuilder.AlignAxes(set.transform);
            var swapR = new List<Renderer>();
            var swapN = new List<Material>();
            var swapF = new List<Material>();
            var swapD = new List<Material>();
            var dawnOnly = new List<Renderer>();
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Fortress/Story_{n}.png");
                var dawnTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Fortress/Story_{n}_Dawn.png");
                Material dawnMat = null;
                if (dawnTex != null)
                {
                    // dawn: lit by the blasts' fires, and fading into its burning bake (ProjectInk/FireLit)
                    dawnMat = EnvironmentBuilder.TexMat("Fort_" + n + "_Dawn", "ProjectInk/FireLit", dawnTex, Vector2.one);
                    var burnTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Fortress/Story_{n}_Burn.png");
                    dawnMat.SetTexture("_BurnTex", burnTex != null ? burnTex : dawnTex);
                }
                var m = tex != null || dawnMat == null ? EnvironmentBuilder.TexMat("Fort_" + n, "Unlit/Texture", tex, Vector2.one) : dawnMat;
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                if (tex == null && dawnMat != null)
                {
                    // the way out the front (the walk-out): there only at dawn
                    dawnOnly.Add(r);
                    r.enabled = false;
                    continue;
                }
                var flash = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Fortress/Story_{n}_Lightning.png");
                if (flash != null || dawnMat != null)
                {
                    swapR.Add(r); swapN.Add(m); swapD.Add(dawnMat);
                    swapF.Add(flash != null ? EnvironmentBuilder.TexMat("Fort_" + n + "_Lightning", "Unlit/Texture", flash, Vector2.one) : m);
                }
                if (n == "FortHallFront") r.gameObject.AddComponent<CutawayWall>().insideZ = -3.2f;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var swap = room.gameObject.AddComponent<LightningSwap>();
            swap.renderers = swapR.ToArray(); swap.normal = swapN.ToArray(); swap.flash = swapF.ToArray();
            swap.dawn = swapD.ToArray(); swap.dawnOnly = dawnOnly.ToArray();

            // the ground (the road) and the hill's level (the gate, the courtyard, the hall)
            var hidden = PrototypeBuilder.Mat("FortGroundHidden", "Unlit/Color", Color.black);
            var road = PrototypeBuilder.Box(room, "Ground", new Vector3(20f, -2f, 0f), new Vector3(70f, 4f, 40f), hidden);
            road.GetComponent<Renderer>().enabled = false;
            var hill = PrototypeBuilder.Box(room, "HillLevel", new Vector3(110f, FortressPlan.Up - 2f, 0f), new Vector3(116f, 4f, 40f), hidden);
            hill.GetComponent<Renderer>().enabled = false;
            hill.transform.position = new Vector3((FortressPlan.StairX1 + FortressPlan.HallX1 + 2f) * 0.5f, FortressPlan.Up - 2f, 0f);
            hill.transform.localScale = new Vector3(FortressPlan.HallX1 + 2f - FortressPlan.StairX1, 4f, 40f);

            // wet: the road and the stones shine, rippling in the rain (one mirror render per level)
            var mask = EnvironmentBuilder.SaveTex("FortWetMask", StoryBuilder.WetMask(256));
            Material Wet(string name, float b, float p)
            {
                var m = PrototypeBuilder.Mat(name, "ProjectInk/WetFloor", Color.white);
                m.SetTexture("_WetTex", mask);
                m.SetFloat("_WetScale", 0.08f);
                m.SetFloat("_Base", b);
                m.SetFloat("_Puddle", p);
                m.SetFloat("_Ripple", 0.004f);
                return m;
            }
            var wetRoad = FloorQuad(env, "WetRoad", new Vector3(22f, 0.01f, -0.2f), new Vector2(54f, 5.2f), Wet("FortWetRoad", 0.18f, 0.7f));
            wetRoad.gameObject.AddComponent<PlanarReflection>();
            var wetHill = FloorQuad(env, "WetCourt", new Vector3(98f, FortressPlan.Up + 0.012f, 0.6f), new Vector2(60f, 7f), Wet("FortWetCourt", 0.12f, 0.55f));
            wetHill.gameObject.AddComponent<PlanarReflection>();
            // the last roof's terrace (over the hall; the hall's own floor is the hill level below it)
            var roof = PrototypeBuilder.Box(room, "RoofTerrace", Vector3.zero, Vector3.one, hidden);
            roof.GetComponent<Renderer>().enabled = false;
            roof.transform.position = new Vector3((FortressPlan.RoofX0 + FortressPlan.HallX1) * 0.5f, FortressPlan.RoofY - 0.1f, 0.6f);
            roof.transform.localScale = new Vector3(FortressPlan.HallX1 - FortressPlan.RoofX0 + 1f, 0.2f, 8f);
            var wetRoof = FloorQuad(env, "WetRoof", new Vector3((FortressPlan.RoofX0 + FortressPlan.HallX1) * 0.5f, FortressPlan.RoofY + 0.012f, 0.8f), new Vector2(38f, 7.6f), Wet("FortWetRoof", 0.14f, 0.65f));
            wetRoof.gameObject.AddComponent<PlanarReflection>();

            // the light: the braziers, the floodlights, the chandeliers; mist on the ground
            var coneTex = EnvironmentBuilder.SaveTex("FortCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("FortGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("FortCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.38f, 0.38f, 0.38f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("FortGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.55f, 0.55f, 0.55f, 0.5f));
            var floods = new GameObject("Floodlights").transform;
            floods.SetParent(env, false);
            foreach (var (x, y, z, len) in new[] { (38f, 5.9f, 1.2f, 5.8f), (99f, FortressPlan.Up + 6.2f, 2.6f, 6.1f), (FortressPlan.FloodPoleX, FortressPlan.Up + 6.2f, 2.6f, 6.1f) })
            {
                var c = StoryBuilder.Quad(floods, "FloodCone", new Vector3(x, y - len * 0.5f, z - 0.4f), new Vector2(len * 0.9f, len), coneMat);
                c.gameObject.name = x > 100f ? "FloodCone_Court2" : x > 90f ? "FloodCone_Court1" : "FloodCone_Road";
                StoryBuilder.Quad(floods, "FloodGlow", new Vector3(x, y, z - 0.6f), new Vector2(1.8f, 1.8f), glowMat).gameObject.name = c.gameObject.name.Replace("Cone", "Glow");
            }
            foreach (var bx in new[] { 65.6f, 72.4f })
                StoryBuilder.Quad(env, "BrazierGlow", new Vector3(bx, FortressPlan.Up + 1.6f, -1.3f), new Vector2(1.6f, 1.8f), glowMat);
            foreach (var cx in new[] { 136f, 146f, 156f })
                StoryBuilder.Quad(env, "ChandelierGlow", new Vector3(cx, FortressPlan.Up + 5.8f, 0.6f), new Vector2(3.2f, 1.8f), glowMat);
            StoryBuilder.Quad(env, "FireGlow", new Vector3(160f, FortressPlan.Up + 0.9f, 3.0f), new Vector2(3.6f, 2.4f), glowMat);
            var hazeMat = EnvironmentBuilder.TexMat("FortMist", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "RoadMist", new Vector3(22f, 0f, 3f), 50f, 1.4f, hazeMat);
            StoryBuilder.HazeStrip(env, "CourtMist", new Vector3(98f, FortressPlan.Up, 3.6f), 60f, 1.2f, hazeMat);
            StoryBuilder.HazeStrip(env, "HallHaze", new Vector3(147f, FortressPlan.Up, 3.6f), 36f, 1.6f, hazeMat);

            for (int i = 0; i < 5; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(30f, 0f, 0f));
            heroStart = new Vector3(2f, 0f, 0f);
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
