using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>CHAPTER 2: THE BANK (docs/story/chapter2_bank.md). Puts the baked set together
    /// (tools/blender/build_bank_set.py): the street, MERIDIAN TRUST's stone front and marble lobby,
    /// the corridor, the vault and its wall of boxes, in the same black and white as the warehouse
    /// and the tower. The front wall is cut away while the camera is outside it; the marble and the
    /// wet street really reflect; the lobby's globes throw the tower's soft cones; the morning comes
    /// through the back windows in shafts; a low haze. The story itself is StoryIntro's chapter 2
    /// (StoryBank.cs).</summary>
    public static class BankBuilder
    {
        public const string SetPath = "Assets/Art/Bank/BankSet.fbx";
        public const float StreetX0 = -26f, VaultX1 = 45.4f, RoofY = 11f;
        public const float HeroStartX = -22f;

        public static void Build(Transform room, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            var env = new GameObject("Bank").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null) throw new BuildFailedException("BankSet.fbx missing - run tools/blender/build_bank_set.py");
            var set = (GameObject)Object.Instantiate(model, room);
            set.name = "BankSet";
            StoryBuilder.AlignAxes(set.transform);
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Bank/Story_{n}.png");
                var m = EnvironmentBuilder.TexMat("Bank_" + n, "Unlit/Texture", tex, Vector2.one);
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                if (n == "BankFront") r.gameObject.AddComponent<CutawayWall>().insideZ = -2.25f;
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            // the floor (one level everywhere: a level entrance) and the walls of the walk
            var ground = PrototypeBuilder.Mat("BankGroundHidden", "Unlit/Color", Color.black);
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(10f, -2f, 0f), new Vector3(120f, 4f, 40f), ground);
            floor.GetComponent<Renderer>().enabled = false;

            // the reflections: polished marble in the lobby, the wet street outside (one mirror render)
            var mask = EnvironmentBuilder.SaveTex("BankWetMask", StoryBuilder.WetMask(256));
            var wet = PrototypeBuilder.Mat("BankWetStreet", "ProjectInk/WetFloor", Color.white);
            wet.SetTexture("_WetTex", mask);
            wet.SetFloat("_WetScale", 0.07f);
            wet.SetFloat("_Base", 0.12f);
            wet.SetFloat("_Puddle", 0.6f);
            wet.SetFloat("_Ripple", 0.0008f);
            var marble = PrototypeBuilder.Mat("BankMarble", "ProjectInk/WetFloor", new Color(1f, 1f, 1f, 1f));
            marble.SetTexture("_WetTex", Texture2D.whiteTexture);
            marble.SetFloat("_Base", 0.34f);
            marble.SetFloat("_Puddle", 0.34f);
            marble.SetFloat("_Ripple", 0f);
            var street = FloorQuad(env, "WetStreet", new Vector3(-13f, 0.003f, 2.4f), new Vector2(30f, 9.4f), wet);   // the sidewalk (the road is a curb lower)
            var lobby = FloorQuad(env, "MarbleFloor", new Vector3(13f, 0.004f, 7f), new Vector2(26f, 18f), marble);
            var pr = street.gameObject.AddComponent<PlanarReflection>();
            pr.shareWith = new[] { lobby.GetComponent<Renderer>() };
            lobby.gameObject.layer = 4;

            // the tower's light: cones and glows under the lobby's globes, morning shafts through the
            // back windows, a low haze, dust in the light
            var coneTex = EnvironmentBuilder.SaveTex("BankCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("BankGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var coneMat = EnvironmentBuilder.TexMat("BankLampCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.38f, 0.38f, 0.38f, 0.5f));
            var glowMat = EnvironmentBuilder.TexMat("BankLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            var shaftMat = EnvironmentBuilder.TexMat("BankShaft", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.16f, 0.16f, 0.17f, 0.5f));
            foreach (float x in new[] { 6f, 13f, 20f })
            {
                StoryBuilder.Quad(env, "LightCone", new Vector3(x, 2.8f, 1.4f), new Vector2(4.4f, 5.4f), coneMat);
                StoryBuilder.Quad(env, "GlobeGlow", new Vector3(x, 5.6f, 1.1f), new Vector2(1.8f, 1.8f), glowMat);
            }
            foreach (float x in new[] { 4f, 10f, 16f, 22f })
            {
                var s = StoryBuilder.Quad(env, "MorningShaft", new Vector3(x - 1.6f, 4.0f, 9.0f), new Vector2(2.6f, 10.5f), shaftMat);
                s.rotation = Quaternion.Euler(28f, 0f, 24f);
            }
            var hazeMat = EnvironmentBuilder.TexMat("BankHaze", "Sprites/Default", null, Vector2.one, Color.white);
            StoryBuilder.HazeStrip(env, "LobbyHaze", new Vector3(13f, 0f, 6f), 26f, 1.4f, hazeMat);
            StoryBuilder.HazeStrip(env, "StreetHaze", new Vector3(-13f, 0f, 7f), 28f, 2.0f, hazeMat);
            EnvironmentBuilder.Dust(env, new EnvironmentBuilder.Layout { halfW = 13f, roofY = 9f, wallZ = 6f }, glowTex);
            var dust = env.Find("Dust");
            if (dust != null) dust.position = new Vector3(13f, 4.5f, 3f);

            // spawn points (the story places its people itself; these are where they first appear)
            foreach (int i in new[] { 0, 1, 2, 3, 4 })
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), new Vector3(16f, 0f, 3.2f));
            heroStart = new Vector3(HeroStartX, 0f, 0f);
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
