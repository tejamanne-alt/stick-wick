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
        public const int SceneVersion = 3;
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
            var particle = Mat("InkParticle", "Legacy Shaders/Particles/Alpha Blended", Color.white);
            var glow = Mat("LightBurst", "Legacy Shaders/Particles/Additive", new Color(0.5f, 0.5f, 0.5f));
            var trail = Mat("Trail", "Sprites/Default", Color.white);
            var backdropMat = Mat("Backdrop", "Sprites/Default", Color.white);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.skybox = null;

            // ---------------------------------------------------------------- room (all black silhouette)
            var room = new GameObject("Room").transform;
            Box(room, "Ground", new Vector3(0, -2f, 0.5f), new Vector3(60, 4, 9), ground);
            DoorFrame(room, "DoorLeft", new Vector3(-12.6f, 0, 0.9f), ground, 1.8f);
            DoorFrame(room, "DoorRight", new Vector3(12.6f, 0, 0.9f), ground, 1.8f);
            DoorFrame(room, "DoorBack", new Vector3(-3f, 0, 4.4f), ground, 1.6f, true);

            // staircase up to a landing: enemies come down it, and from under it
            const int steps = 8;
            const float rise = 0.3f, run = 0.5f, x0 = 2f;
            for (int i = 0; i < steps; i++)
            {
                float hgt = rise * (i + 1);
                Box(room, $"Step{i}", new Vector3(x0 + run * (i + 0.5f), hgt * 0.5f, 0.6f), new Vector3(run, hgt, 2.2f), ground);
            }
            float top = rise * steps;
            float landingX0 = x0 + run * steps;
            Box(room, "Landing", new Vector3(landingX0 + 2f, top - 0.15f, 0.6f), new Vector3(4f, 0.3f, 2.2f), ground);
            Box(room, "LandingPillar", new Vector3(landingX0 + 3.8f, (top - 0.3f) * 0.5f, 1.5f), new Vector3(0.25f, top - 0.3f, 0.25f), ground);
            Box(room, "Railing", new Vector3(landingX0 + 2f, top + 0.5f, 1.55f), new Vector3(4f, 0.06f, 0.06f), ground);

            // ---------------------------------------------------------------- spawn points
            var spawnRoot = new GameObject("SpawnPoints").transform;
            var points = new Transform[5];
            points[(int)SpawnKind.LeftDoor] = Point(spawnRoot, "LeftDoor", new Vector3(-12.2f, 0, 0));
            points[(int)SpawnKind.RightDoor] = Point(spawnRoot, "RightDoor", new Vector3(12.2f, 0, 0));
            points[(int)SpawnKind.StairsTop] = Point(spawnRoot, "StairsTop", new Vector3(landingX0 + 3.2f, top, 0));
            points[(int)SpawnKind.Background] = Point(spawnRoot, "Background", new Vector3(-3f, 0, 4.3f));
            points[(int)SpawnKind.Ceiling] = Point(spawnRoot, "Ceiling", new Vector3(0f, 7.5f, 0));

            // ---------------------------------------------------------------- hero + systems
            var heroGo = FighterFactory.Build(model, moves, ink, "Hero", true, trail);
            heroGo.transform.position = Vector3.zero;
            var hero = heroGo.GetComponent<HeroController>();

            var systems = new GameObject("Systems");
            systems.AddComponent<TimeController>();
            systems.AddComponent<GestureInput>();
            var fx = systems.AddComponent<InkFx>();
            fx.particleMaterial = particle;
            fx.glowMaterial = glow;
            var director = systems.AddComponent<WaveDirector>();
            director.hero = hero;
            director.enemyModel = model;
            director.moves = moves;
            director.enemyMaterial = enemyInk;
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
            dirCam.minDistance = 9.5f;
            dirCam.maxDistance = 16f;
            dirCam.height = 1.3f;
            dirCam.lookHeight = 1.05f;
            camGo.transform.position = new Vector3(0, 1.3f, -9.5f);

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
