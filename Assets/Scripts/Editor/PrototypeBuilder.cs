using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectInk.EditorTools
{
    /// <summary>Builds the whole prototype from code, so it can run headless on CI:
    /// generated materials, the MoveSet, the grey-box room with its staircase, the hero,
    /// the systems, and the Android build.
    /// Menu: Project INK → Build Prototype Scene / Build Android APK.
    /// CLI:  -executeMethod ProjectInk.EditorTools.PrototypeBuilder.CI_BuildAndroid</summary>
    public static class PrototypeBuilder
    {
        const string GenDir = "Assets/Generated";
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        const string ApkPath = "build/Android/ProjectInk.apk";
        /// <summary>Bump whenever the generated scene changes; open editors rebuild it on pull.</summary>
        public const int SceneVersion = 6;
        public const string VersionFile = "Assets/Generated/scene_version.txt";

        [MenuItem("Project INK/Build Prototype Scene")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(GenDir);
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.ImportAsset(StickmanImport.ModelPath, ImportAssetOptions.ForceUpdate);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanImport.ModelPath);
            if (model == null) throw new BuildFailedException("Stickman.fbx not found - run tools/blender/build_stickman.py");
            var moves = BuildMoveSet();

            // Look of the reference: flat black silhouettes on a soft grey backdrop. Everything is
            // unlit, so no lights are needed and phones render it cheaply.
            var ink = Mat("Ink", "Unlit/Color", new Color(0.02f, 0.02f, 0.02f));
            var enemyInk = Mat("EnemyInk", "Unlit/Color", new Color(0.02f, 0.02f, 0.02f));
            var ground = Mat("Ground", "Unlit/Color", new Color(0.02f, 0.02f, 0.02f));
            // architecture is dark grey, not black: black fighters must read against it
            var arch = Mat("Architecture", "Unlit/Color", new Color(0.30f, 0.30f, 0.30f));
            var particle = Mat("InkParticle", "Legacy Shaders/Particles/Alpha Blended", Color.white);
            var glow = Mat("LightBurst", "Legacy Shaders/Particles/Additive", new Color(0.5f, 0.5f, 0.5f));
            var trail = Mat("Trail", "Sprites/Default", Color.white);
            var backdropMat = Mat("Backdrop", "Sprites/Default", Color.white);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.skybox = null;

            // ---------------------------------------------------------------- the building
            // A cut-away of a two-floor building, seen side-on: black floors and outer walls, a
            // light interior back wall (black fighters and coloured enemies both read on it, and
            // it keeps the colour of every fight), a staircase in a lane just behind the fight
            // lane, an open ledge on the upper floor, doors on both floors and at the back.
            var interior = Mat("Interior", "Unlit/Color", new Color(0.76f, 0.75f, 0.72f));
            var room = new GameObject("Building").transform;
            const float halfW = 13.2f, floorY = 3.2f, roofY = 6.8f, laneFront = -0.6f, wallZ = 2.4f;
            Box(room, "Ground", new Vector3(0, -2f, 0.5f), new Vector3(60, 4, 9), ground);
            Box(room, "BackWall", new Vector3(0, roofY * 0.5f, wallZ), new Vector3(halfW * 2f, roofY, 0.3f), interior);
            Box(room, "Roof", new Vector3(0, roofY + 0.1f, (laneFront + wallZ) * 0.5f), new Vector3(halfW * 2f + 0.4f, 0.2f, wallZ - laneFront), ground);
            Box(room, "WallLeft", new Vector3(-halfW, roofY * 0.5f, (laneFront + wallZ) * 0.5f), new Vector3(0.4f, roofY, wallZ - laneFront), ground);
            Box(room, "WallRight", new Vector3(halfW, roofY * 0.5f, (laneFront + wallZ) * 0.5f), new Vector3(0.4f, roofY, wallZ - laneFront), ground);
            // upper floor: from the left wall to an open ledge at x = 0
            Box(room, "UpperFloor", new Vector3(-halfW * 0.5f, floorY - 0.075f, (laneFront + wallZ) * 0.5f), new Vector3(halfW, 0.15f, wallZ - laneFront), ground);
            // staircase up to the ledge, in its own lane (z 0.9 to 1.7) so fights on the ground pass in front of it
            const int steps = 10;
            const float rise = floorY / steps, run = 0.5f;
            for (int i = 0; i < steps; i++)
            {
                float hgt = rise * (steps - i);
                LaneBox(room, $"Step{i}", run * (i + 0.5f), hgt * 0.5f, run, hgt, 0.9f, 1.7f, 0.9f, arch);
            }
            Box(room, "StairRail", new Vector3(steps * run * 0.5f, floorY * 0.5f + 0.9f, 1.75f), new Vector3(Mathf.Sqrt(steps * run * steps * run + floorY * floorY), 0.05f, 0.05f), arch)
                .rotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(floorY, steps * run) * Mathf.Rad2Deg);
            DoorFrame(room, "DoorLeft", new Vector3(-12.4f, 0, 0.9f), arch, 1.8f);
            DoorFrame(room, "DoorRight", new Vector3(12.4f, 0, 0.9f), arch, 1.8f);
            DoorFrame(room, "DoorUpper", new Vector3(-12.4f, floorY, 0.9f), arch, 1.8f);
            DoorFrame(room, "DoorBack", new Vector3(-6f, 0, wallZ - 0.2f), arch, 1.6f, true);
            DoorFrame(room, "Window", new Vector3(-6f, floorY + 1.0f, wallZ - 0.2f), arch, 1.4f, true);
            DoorFrame(room, "Vent", new Vector3(3f, roofY - 0.1f, wallZ - 0.2f), arch, 1.0f, true);

            // ---------------------------------------------------------------- spawn points
            var spawnRoot = new GameObject("SpawnPoints").transform;
            var points = new Transform[5];
            points[(int)SpawnKind.LeftDoor] = Point(spawnRoot, "LeftDoor", new Vector3(-12.2f, 0, 0));
            points[(int)SpawnKind.RightDoor] = Point(spawnRoot, "RightDoor", new Vector3(12.2f, 0, 0));
            points[(int)SpawnKind.UpperDoor] = Point(spawnRoot, "UpperDoor", new Vector3(-12.2f, floorY, 0));
            points[(int)SpawnKind.Background] = Point(spawnRoot, "Background", new Vector3(-6f, 0, wallZ - 0.4f));
            points[(int)SpawnKind.Ceiling] = Point(spawnRoot, "Ceiling", new Vector3(0f, roofY - 0.6f, 0));

            // ---------------------------------------------------------------- hero + systems
            var heroGo = FighterFactory.Build(model, moves, ink, "Hero", true, trail);
            heroGo.transform.position = new Vector3(6f, 0f, 0f);
            var hero = heroGo.GetComponent<HeroController>();

            var systems = new GameObject("Systems");
            systems.AddComponent<TimeController>();
            systems.AddComponent<GestureInput>();
            var fx = systems.AddComponent<InkFx>();
            fx.particleMaterial = particle;
            fx.glowMaterial = glow;
            fx.lineMaterial = trail;
            fx.wallZ = 2.24f;
            var director = systems.AddComponent<WaveDirector>();
            director.hero = hero;
            director.enemyModel = model;
            director.moves = moves;
            director.enemyMaterial = enemyInk;
            director.trailMaterial = trail;
            director.weaponMaterial = ink;
            director.spawnPoints = points;
            var hud = systems.AddComponent<HudGUI>();
            hud.hero = hero;
            hud.director = director;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            var dirCam = camGo.AddComponent<CameraDirector>();
            dirCam.hero = heroGo.transform;
            dirCam.minDistance = 11f;
            dirCam.maxDistance = 19f;
            dirCam.height = 1.6f;
            dirCam.lookHeight = 1.35f;
            dirCam.minX = -7f;
            dirCam.maxX = 7f;
            camGo.transform.position = new Vector3(6f, 1.6f, -11f);

            // grey gradient backdrop that travels with the camera, so it fills every shot,
            // including the kill-cam orbits
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(camGo.transform, false);
            backdrop.transform.localPosition = new Vector3(0, 0, 60f);
            backdrop.AddComponent<MeshFilter>().sharedMesh = BackdropMesh();
            var br = backdrop.AddComponent<MeshRenderer>();
            br.sharedMaterial = backdropMat;
            br.shadowCastingMode = ShadowCastingMode.Off;
            br.receiveShadows = false;

            ConfigurePlayer();

            EditorSceneManager.SaveScene(scene, ScenePath);
            File.WriteAllText(VersionFile, SceneVersion.ToString());
            AssetDatabase.ImportAsset(VersionFile);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Project INK] prototype scene built: " + ScenePath);
        }

        static MoveSet BuildMoveSet()
        {
            string path = GenDir + "/StickmanMoves.asset";
            var set = AssetDatabase.LoadAssetAtPath<MoveSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<MoveSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            var info = StickmanImport.LoadClipInfo();
            var clips = AssetDatabase.LoadAllAssetsAtPath(StickmanImport.ModelPath)
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
            set.entries = new List<MoveSet.Entry>();
            foreach (var c in clips)
            {
                string n = StickmanImport.ShortName(c.name);
                var meta = info.clips.FirstOrDefault(i => i.name == n);
                set.entries.Add(new MoveSet.Entry
                {
                    name = n,
                    clip = c,
                    loop = meta != null && meta.loop,
                    hitTime = meta != null && meta.hitFrame >= 0 ? meta.hitFrame / (float)Mathf.Max(1, meta.fps) : -1f
                });
            }
            EditorUtility.SetDirty(set);
            Debug.Log($"[Project INK] MoveSet: {set.entries.Count} clips ({string.Join(", ", set.entries.Select(e => e.name))})");
            if (set.Get("idle") == null || set.Get("jab") == null)
                throw new BuildFailedException("Stickman clips missing - check the FBX import");
            return set;
        }

        /// <summary>A large grid quad with vertex colours: light grey in the middle, darker
        /// towards the edges and the bottom, like the soft-lit backdrop of the reference.</summary>
        static Mesh BackdropMesh()
        {
            string path = GenDir + "/Backdrop.asset";
            AssetDatabase.DeleteAsset(path);
            const int nx = 24, ny = 12;
            const float w = 110f, h = 48f;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            for (int y = 0; y <= ny; y++)
                for (int x = 0; x <= nx; x++)
                {
                    float u = (float)x / nx, v = (float)y / ny;
                    verts.Add(new Vector3((u - 0.5f) * w, (v - 0.5f) * h, 0f));
                    float dx = (u - 0.5f) * 2f, dy = (v - 0.55f) * 2.2f;
                    float light = Mathf.Exp(-(dx * dx * 2.2f + dy * dy * 1.6f));   // soft glow in the middle
                    float g = Mathf.Lerp(0.42f, 0.74f, light);
                    cols.Add(new Color(g, g, g, 1f));
                }
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                {
                    int i = y * (nx + 1) + x;
                    tris.AddRange(new[] { i, i + nx + 1, i + 1, i + 1, i + nx + 1, i + nx + 2 });
                }
            var mesh = new Mesh { name = "Backdrop" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material Mat(string name, string shader, Color color)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) throw new BuildFailedException("Shader not found: " + shader);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go.transform;
        }

        /// <summary>A block whose visible part spans z = visFront..back (behind the fighters)
        /// while its collider spans colFront..back, so the walkable top reaches the lane.</summary>
        static void LaneBox(Transform parent, string name, float x, float y, float sx, float sy,
                            float visFront, float back, float colFront, Material mat)
        {
            float depth = back - visFront;
            var t = Box(parent, name, new Vector3(x, y, (visFront + back) * 0.5f), new Vector3(sx, sy, depth), mat);
            var col = t.GetComponent<BoxCollider>();
            float cz = ((colFront + back) * 0.5f - (visFront + back) * 0.5f) / depth;
            col.center = new Vector3(0f, 0f, cz);
            col.size = new Vector3(1f, 1f, (back - colFront) / depth);
        }

        static void DoorFrame(Transform parent, string name, Vector3 basePos, Material mat, float width, bool facingCamera = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = basePos;
            const float h = 2.3f, t = 0.12f;
            // posts sit to the sides of the opening; no colliders in the walking lane
            Vector3 side = facingCamera ? Vector3.right : Vector3.forward;
            foreach (float s in new[] { -1f, 1f })
            {
                var post = Box(root, "Post", basePos + side * (s * width * 0.5f) + Vector3.up * (h * 0.5f), facingCamera ? new Vector3(t, h, t) : new Vector3(t, h, t), mat);
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }
            var lintel = Box(root, "Lintel", basePos + Vector3.up * h, facingCamera ? new Vector3(width + t, t, t) : new Vector3(t, t, width + t), mat);
            Object.DestroyImmediate(lintel.GetComponent<Collider>());
        }

        static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = pos;
            return t;
        }

        /// <summary>Landscape only, on every build path.</summary>
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Project INK";
            PlayerSettings.productName = "Project INK";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.projectink.prototype");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation; // landscape left/right only (below)
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        [MenuItem("Project INK/Build Android APK")]
        public static void BuildAndroid()
        {
            BuildScene();
            ConfigurePlayer();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            Debug.Log($"[Project INK] Android build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException("Android build failed");
        }

        /// <summary>Entry point for command-line and CI builds; exits with a status code.</summary>
        public static void CI_BuildAndroid()
        {
            try
            {
                BuildAndroid();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Command-line: just build the scene (used to check the project imports cleanly).</summary>
        public static void CI_BuildScene()
        {
            try
            {
                BuildScene();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
