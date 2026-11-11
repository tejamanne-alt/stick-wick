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
    /// Menu: Stick Wick → Build Prototype Scene / Build Android APK.
    /// CLI:  -executeMethod ProjectInk.EditorTools.PrototypeBuilder.CI_BuildAndroid</summary>
    public static class PrototypeBuilder
    {
        const string GenDir = "Assets/Generated";
        public const string ScenePath = "Assets/Scenes/Prototype.unity";
        const string ApkPath = "build/Android/StickWick.apk";
        /// <summary>Bump whenever the generated scene changes; open editors rebuild it on pull.</summary>
        public const int SceneVersion = 54;
        public const string VersionFile = "Assets/Generated/scene_version.txt";

        const string HallScenePath = "Assets/Scenes/ClassicHall.unity";
        public const string StoryScenePath = "Assets/Scenes/Story_Warehouse.unity";
        public const string BankScenePath = "Assets/Scenes/Story_Bank.unity";
        public const string StreetScenePath = "Assets/Scenes/Story_Street.unity";
        public const string HomeScenePath = "Assets/Scenes/Story_Home.unity";
        public const string ParkScenePath = "Assets/Scenes/Story_Backyard.unity";
        public const string FortressScenePath = "Assets/Scenes/Story_Fortress.unity";
        public const string RoadScenePath = "Assets/Scenes/Story_Road.unity";
        public const string EpilogueScenePath = "Assets/Scenes/Story_Epilogue.unity";
        public const string MenuScenePath = "Assets/Scenes/Menu.unity";

        /// <summary>The main scene: the tower.</summary>
        [MenuItem("Stick Wick/Build All Scenes")]
        [MenuItem("Stick Wick/Build Prototype Scene")]
        public static void BuildScene()
        {
            // scenes cannot be built in play mode: stop first, then build once the editor is back
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += BuildAfterStop;
                Debug.Log("[Stick Wick] Stopping play mode to build the scene…");
                return;
            }
            BuildAllNow();
        }

        static void BuildAfterStop(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= BuildAfterStop;
            EditorApplication.delayCall += BuildAllNow;
        }

        /// <summary>Every scene of the game: the main menu, story mode's warehouse and the tower
        /// (fight mode). Opens the menu at the end.</summary>
        public static void BuildAllNow()
        {
            Build(LayoutKind.Tower, ScenePath, false);
            Build(LayoutKind.Warehouse, StoryScenePath, false);
            Build(LayoutKind.Bank, BankScenePath, false);
            Build(LayoutKind.Street, StreetScenePath, false);
            Build(LayoutKind.Home, HomeScenePath, false);
            Build(LayoutKind.Park, ParkScenePath, false);
            Build(LayoutKind.Park, RoadScenePath, false, StorySet.Road);
            Build(LayoutKind.Fortress, FortressScenePath, false);
            Build(LayoutKind.Park, EpilogueScenePath, false, StorySet.Epilogue);
            BuildMenu();
            File.WriteAllText(VersionFile, SceneVersion.ToString());
            AssetDatabase.ImportAsset(VersionFile);
            BuildScenes.Apply();                         // the shared list and the active build profile's (Unity 6)
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log("[Stick Wick] built: menu, story (warehouse) and fight mode (tower). Press Play.");
        }

        /// <summary>The title screen: STORY MODE or FIGHT MODE.</summary>
        static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            camGo.AddComponent<AudioListener>();
            new GameObject("Menu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        /// <summary>The backup: the classic hall, saved as its own scene (open it and press Play).</summary>
        [MenuItem("Stick Wick/Build Classic Hall (backup)")]
        public static void BuildClassicHall() => Build(LayoutKind.Hall, HallScenePath, false);

        /// <summary>Story scenes that are not a LayoutKind of their own (no navigation map, no fight lane).</summary>
        enum StorySet { None, Road, Epilogue }

        static void Build(LayoutKind kind, string scenePath, bool main, StorySet storySet = StorySet.None)
        {
            Directory.CreateDirectory(GenDir);
            Directory.CreateDirectory(GenDir + "/Textures");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();                     // so the asset database knows the folders (they may have been deleted)
            // chapter 1 is Combat 3.0's test bed: it runs on the V3 body (both the NEW controls and the
            // CLASSIC ones, chosen in its pause card); every other scene keeps the classic body
            // (the 29-bone V4 body when it is there, else the V3 one)
            // every story chapter runs on the 29-bone V4 body (docs/design/backlog.md steps 1 and 7); the
            // tower and the classic hall are fight mode and keep the classic one
            bool newBody = kind == LayoutKind.Warehouse || kind == LayoutKind.Bank || kind == LayoutKind.Street ||
                           kind == LayoutKind.Home || kind == LayoutKind.Park || kind == LayoutKind.Fortress;
            bool v4 = newBody && File.Exists(StickmanImport.ModelPathV4) && File.Exists(StickmanImport.ClipJsonPathV4);
            bool v3 = newBody && (v4 || (File.Exists(StickmanImport.ModelPathV3) && File.Exists(StickmanImport.ClipJsonPathV3)));
            string modelPath = v4 ? StickmanImport.ModelPathV4 : v3 ? StickmanImport.ModelPathV3 : StickmanImport.ModelPath;
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new BuildFailedException(modelPath + " not found - run tools/blender/build_stickman.py (and build_stickman_v3.py)");
            var moves = v4 ? BuildMoveSet(modelPath, StickmanImport.ClipJsonPathV4, GenDir + "/StickmanMovesV4.asset")
                      : v3 ? BuildMoveSet(modelPath, StickmanImport.ClipJsonPathV3, GenDir + "/StickmanMovesV3.asset")
                           : BuildMoveSet(modelPath, StickmanImport.ClipJsonPath, GenDir + "/StickmanMoves.asset");

            // Look of the reference: flat black silhouettes on a soft grey backdrop. Everything is
            // unlit, so no lights are needed and phones render it cheaply.
            // the V3 body's ink shades its far limbs by depth in fights (Combat 3.0, option C)
            var ink = v3 ? Mat("InkV3", "ProjectInk/InkDepth", new Color(0.02f, 0.02f, 0.02f)) : Mat("Ink", "Unlit/Color", new Color(0.02f, 0.02f, 0.02f));
            var enemyInk = v3 ? Mat("EnemyInkV3", "ProjectInk/InkDepth", new Color(0.02f, 0.02f, 0.02f)) : Mat("EnemyInk", "Unlit/Color", new Color(0.02f, 0.02f, 0.02f));
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
            // A cut-away hall seen side-on, built as ONE continuous walking surface so moving
            // between levels is just running left or right (no hidden stair lane, no ledge climb):
            //   ground (left) | wide stairs up | raised central deck | wide stairs down | ground (right)
            // Light concrete back wall (black fighters and coloured enemies both read on it),
            // doors at both ends, a back door on the right and a doorway onto the deck.
            var interior = Mat("Interior", "Unlit/Color", new Color(0.76f, 0.75f, 0.72f));
            var room = new GameObject("Building").transform;
            var layout = room.gameObject.AddComponent<LevelLayout>();
            layout.kind = kind;
            var spawnRoot = new GameObject("SpawnPoints").transform;
            var points = new Transform[5];
            Vector3 heroStart;
            bool bank = kind == LayoutKind.Bank;
            bool street = kind == LayoutKind.Street;
            bool home = kind == LayoutKind.Home;
            bool park = kind == LayoutKind.Park;
            bool fortress = kind == LayoutKind.Fortress;
            bool road = storySet == StorySet.Road;
            bool epilogue = storySet == StorySet.Epilogue;
            if (road || epilogue) park = false;
            bool story = kind == LayoutKind.Warehouse || bank || street || home || park || fortress || road || epilogue;
            if (road)
            {
                layout.roofY = RoadBuilder.RoofY;
                RoadBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (epilogue)
            {
                layout.roofY = EpilogueBuilder.RoofY;
                EpilogueBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (kind == LayoutKind.Hall)
            {
                layout.roofY = 6.8f;
                BuildHall(room, ground, arch, trail, spawnRoot, points, out heroStart);
            }
            else if (fortress)
            {
                layout.roofY = FortressBuilder.RoofY;
                FortressBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (park)
            {
                layout.roofY = ParkBuilder.RoofY;
                ParkBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (home)
            {
                layout.roofY = HomeBuilder.RoofY;
                HomeBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (street)
            {
                layout.roofY = StreetBuilder.RoofY;
                StreetBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (bank)
            {
                layout.roofY = BankBuilder.RoofY;
                BankBuilder.Build(room, spawnRoot, points, out heroStart);
            }
            else if (story)
            {
                layout.roofY = StoryBuilder.RoofY;
                StoryBuilder.Build(room, ground, spawnRoot, points, out heroStart);
            }
            else
            {
                layout.roofY = TowerPlan.RoofY;
                TowerBuilder.Build(room, ground, arch, trail, spawnRoot, points, out heroStart);
            }

            // ---------------------------------------------------------------- hero + systems
            var eyeMat = Mat("Eyes", "Unlit/Color", new Color(0.97f, 0.96f, 0.92f));
            var heroGo = FighterFactory.Build(model, moves, ink, "Hero", true, trail, FightStyle.Fists, eyeMat);
            heroGo.transform.position = heroStart;
            var hero = heroGo.GetComponent<HeroController>();
            if (story)
            {
                // the man in the warehouse: narrowed eyes and the scar at his temple
                hero.eyeOpenY = 0.034f;
                StoryBuilder.AddScar(heroGo, eyeMat);
                hero.arenaMinX = road ? RoadBuilder.LaneX0 : epilogue ? EpilogueBuilder.LaneX0 : fortress ? 0.4f : park ? ParkPlan.LaneX0 : home ? HomePlan.StreetX0 : street ? StreetPlan.LaneX0 : bank ? BankBuilder.StreetX0 : -StoryBuilder.HalfW + 0.6f;
                hero.arenaMaxX = road ? RoadBuilder.LaneX1 : epilogue ? EpilogueBuilder.LaneX1 : fortress ? FortressPlan.HallX1 - 0.6f : park ? ParkPlan.LaneX1 : home ? HomePlan.HouseX1 - 0.4f : street ? StreetPlan.LaneX1 : bank ? BankBuilder.VaultX1 : StoryBuilder.HalfW - 0.6f;
            }

            var systems = new GameObject("Systems");
            systems.AddComponent<TimeController>();
            var gesture = systems.AddComponent<GestureInput>();
            var fx = systems.AddComponent<InkFx>();
            fx.particleMaterial = particle;
            fx.glowMaterial = glow;
            fx.lineMaterial = trail;
            fx.wallZ = 2.24f;
            gesture.hero = heroGo.transform;
            var director = systems.AddComponent<WaveDirector>();
            director.hero = hero;
            director.enemyModel = model;
            director.moves = moves;
            director.enemyMaterial = enemyInk;
            director.trailMaterial = trail;
            director.weaponMaterial = ink;
            director.eyeMaterial = eyeMat;
            director.spawnPoints = points;
            var hud = systems.AddComponent<HudGUI>();
            hud.hero = hero;
            hud.director = director;
            if (story)
            {
                director.scripted = true;                 // no waves: the story spawns its own
                var intro = systems.AddComponent<StoryIntro>();
                intro.hero = hero;
                intro.director = director;
            }

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.035f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            var dirCam = camGo.AddComponent<CameraDirector>();
            dirCam.hero = heroGo.transform;
            dirCam.minDistance = 9.5f;
            dirCam.maxDistance = 22f;
            dirCam.height = 1.35f;
            dirCam.lookHeight = 1.1f;
            dirCam.minX = -7f;
            dirCam.maxX = 7f;
            camGo.transform.position = new Vector3(6f, 1.6f, -11f);
            if (story)
            {
                cam.backgroundColor = new Color(0.012f, 0.014f, 0.024f);
                cam.farClipPlane = road ? 600f : 320f;     // the city on the horizon in the opening shot (the valley's, far below, on the road)
                dirCam.minX = -9f;
                dirCam.maxX = 9f;
                dirCam.wallX = StoryBuilder.HalfW - 0.4f;
                camGo.AddComponent<StoryCameraFX>();
            }

            // grey gradient backdrop that travels with the camera, so it fills every shot,
            // including the kill-cam orbits (not in the story: the night is the backdrop there)
            var backdrop = new GameObject("Backdrop");
            if (story) backdrop.SetActive(false);
            backdrop.transform.SetParent(camGo.transform, false);
            backdrop.transform.localPosition = new Vector3(0, 0, 60f);
            backdrop.AddComponent<MeshFilter>().sharedMesh = BackdropMesh();
            var br = backdrop.AddComponent<MeshRenderer>();
            br.sharedMaterial = backdropMat;
            br.shadowCastingMode = ShadowCastingMode.Off;
            br.receiveShadows = false;

            ConfigurePlayer();

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Stick Wick] scene built: " + scenePath);
        }

        static MoveSet BuildMoveSet(string modelPath, string jsonPath, string path)
        {
            var set = AssetDatabase.LoadAssetAtPath<MoveSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<MoveSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            var info = StickmanImport.LoadClipInfo(jsonPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
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
                    hitTime = meta != null && meta.hitFrame >= 0 ? meta.hitFrame / (float)Mathf.Max(1, meta.fps) : -1f,
                    endShift = meta != null ? meta.endShift : 0f,
                    loadTime = meta != null && meta.loadFrame >= 0 ? meta.loadFrame / (float)Mathf.Max(1, meta.fps) : -1f,
                    reach = meta != null ? meta.reach : 0f
                });
            }
            EditorUtility.SetDirty(set);
            Debug.Log($"[Stick Wick] MoveSet: {set.entries.Count} clips ({string.Join(", ", set.entries.Select(e => e.name))})");
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
                    float light = Mathf.Exp(-(dx * dx * 2.2f + dy * dy * 1.6f));   // soft city glow low in the middle
                    cols.Add(Color.Lerp(new Color(0.02f, 0.02f, 0.025f), new Color(0.13f, 0.13f, 0.14f), light * (1f - v * 0.6f)));
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

        internal static Material Mat(string name, string shader, Color color)
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

        internal static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
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

        /// <summary>The classic hall (kept as a backup): ground, wide stairs, a raised deck, stairs, ground.</summary>
        static void BuildHall(Transform room, Material ground, Material arch, Material trail, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            const float halfW = 13.2f, floorY = 3.2f, roofY = 6.8f, laneFront = -0.6f, wallZ = 2.4f;
            const float deckY = 2.6f, deckHalf = 3f, stairRun = 4f;      // deck: x -3..3; stairs: 3..7 each side
            Box(room, "Ground", new Vector3(0, -2f, 0.5f), new Vector3(60, 4, 9), ground);
            // the back wall (concrete with windows), lamps, pipes, skyline, haze and dust
            EnvironmentBuilder.Build(room, new EnvironmentBuilder.Layout
            {
                halfW = halfW, floorY = floorY, roofY = roofY, wallZ = wallZ, laneFront = laneFront, ledgeX = 0f,
                deckY = deckY, deckHalf = deckHalf, stairRun = stairRun
            }, ground);
            EnvironmentBuilder.Props(room, new EnvironmentBuilder.Layout
            {
                halfW = halfW, floorY = floorY, roofY = roofY, wallZ = wallZ, laneFront = laneFront, ledgeX = 0f,
                deckY = deckY, deckHalf = deckHalf, stairRun = stairRun
            }, trail);
            // the roof is scenery only: no collider, so nothing can ever stand on top of it
            Object.DestroyImmediate(Box(room, "Roof", new Vector3(0, roofY + 0.1f, (laneFront + wallZ) * 0.5f), new Vector3(halfW * 2f + 0.4f, 0.2f, wallZ - laneFront), ground).GetComponent<Collider>());
            Box(room, "WallLeft", new Vector3(-halfW, roofY * 0.5f, (laneFront + wallZ) * 0.5f), new Vector3(0.4f, roofY, wallZ - laneFront), ground);
            Box(room, "WallRight", new Vector3(halfW, roofY * 0.5f, (laneFront + wallZ) * 0.5f), new Vector3(0.4f, roofY, wallZ - laneFront), ground);
            BuildDeckAndStairs(room, deckY, deckHalf, stairRun, laneFront, wallZ, arch);
            DoorFrame(room, "DoorLeft", new Vector3(-12.4f, 0, 0.9f), arch, 1.8f);
            DoorFrame(room, "DoorRight", new Vector3(12.4f, 0, 0.9f), arch, 1.8f);
            DoorFrame(room, "Vent", new Vector3(3f, roofY - 0.1f, wallZ - 0.2f), arch, 1.0f, true);

            points[(int)SpawnKind.LeftDoor] = Point(spawnRoot, "LeftDoor", new Vector3(-12.2f, 0, 0));
            points[(int)SpawnKind.RightDoor] = Point(spawnRoot, "RightDoor", new Vector3(12.2f, 0, 0));
            points[(int)SpawnKind.UpperDoor] = Point(spawnRoot, "UpperDoor", new Vector3(0f, deckY, wallZ - 0.4f));   // the doorway onto the deck
            points[(int)SpawnKind.Background] = Point(spawnRoot, "Background", new Vector3(11f, 0, wallZ - 0.4f));   // the back door
            points[(int)SpawnKind.Ceiling] = Point(spawnRoot, "Ceiling", new Vector3(0f, roofY - 0.6f, 0));

            heroStart = new Vector3(9f, 0f, 0f);
        }

        /// <summary>The raised deck on its solid base, and a wide flight of stairs on each side.
        /// Walking colliders are smooth ramps (no bumps, no snagging); the visible steps sit on
        /// them, each with a crisp white nosing. A slim handrail runs along the back.</summary>
        static void BuildDeckAndStairs(Transform room, float deckY, float deckHalf, float run, float front, float back, Material arch)
        {
            var root = new GameObject("DeckAndStairs").transform;
            root.SetParent(room, false);
            float depth = back - front, zc = (front + back) * 0.5f;
            var faceMat = Mat("DeckFace", "Unlit/Color", new Color(0.17f, 0.17f, 0.18f));
            var nosing = Mat("StepNosing", "Unlit/Color", new Color(0.86f, 0.86f, 0.86f));
            var rail = Mat("Handrail", "Unlit/Color", new Color(0.08f, 0.08f, 0.08f));

            // the deck: one solid block whose top is the walkable floor
            Box(root, "Deck", new Vector3(0f, deckY * 0.5f, zc), new Vector3(deckHalf * 2f, deckY, depth), faceMat);
            Object.DestroyImmediate(Box(root, "DeckLip", new Vector3(0f, deckY - 0.03f, front - 0.01f), new Vector3(deckHalf * 2f, 0.06f, 0.02f), nosing).GetComponent<Collider>());
            foreach (float px in new[] { -deckHalf + 0.6f, deckHalf - 0.6f })   // two pale pilasters on the deck's face
                Object.DestroyImmediate(Box(root, "DeckPilaster", new Vector3(px, deckY * 0.5f, front - 0.01f), new Vector3(0.05f, deckY - 0.2f, 0.02f), nosing).GetComponent<Collider>());

            const int steps = 9;
            float rise = deckY / steps, tread = run / steps;
            float len = Mathf.Sqrt(run * run + deckY * deckY), ang = Mathf.Atan2(deckY, run) * Mathf.Rad2Deg;
            foreach (int s in new[] { -1, 1 })
            {
                // ramp collider: from the ground at x = s*(deckHalf+run) up to the deck edge at x = s*deckHalf
                var ramp = new GameObject(s < 0 ? "RampLeft" : "RampRight").transform;
                ramp.SetParent(root, false);
                var dir = new Vector3(-s * run, deckY, 0f).normalized;                 // up the stairs
                var normal = new Vector3(-dir.y * -s, Mathf.Abs(dir.x), 0f).normalized;
                if (normal.y < 0f) normal = -normal;
                Vector3 top = new Vector3(s * deckHalf, deckY, zc), bottom = new Vector3(s * (deckHalf + run), 0f, zc);
                const float thick = 0.5f, extra = 0.4f;                                  // the low end runs on under the ground
                ramp.position = (top + bottom) * 0.5f - dir * (extra * 0.5f) - normal * (thick * 0.5f);
                ramp.rotation = Quaternion.Euler(0f, 0f, s < 0 ? ang : -ang);
                var col = ramp.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(len + extra, thick, depth);
                ramp.gameObject.isStatic = true;

                // visible steps (no colliders): treads level with the ramp at their centres
                for (int i = 0; i < steps; i++)
                {
                    float xi = s * (deckHalf + run - (i + 0.5f) * tread);                  // centre of step i, counted from the bottom
                    float topY = rise * (i + 0.5f);
                    var st = Box(root, $"Step{(s < 0 ? "L" : "R")}{i}", new Vector3(xi, topY * 0.5f, zc), new Vector3(tread, topY, depth), arch);
                    Object.DestroyImmediate(st.GetComponent<Collider>());
                    var edge = Box(root, "Nosing", new Vector3(xi, topY - 0.015f, front - 0.01f), new Vector3(tread, 0.03f, 0.02f), nosing);
                    Object.DestroyImmediate(edge.GetComponent<Collider>());
                }

                // handrail along the back of the flight, on slim posts
                const float railZ = 2.05f, railH = 0.95f;
                var mid = (top + bottom) * 0.5f;
                var r = Box(root, "Handrail", new Vector3(mid.x, mid.y + railH, railZ), new Vector3(len, 0.04f, 0.04f), rail);
                r.rotation = Quaternion.Euler(0f, 0f, s < 0 ? ang : -ang);
                Object.DestroyImmediate(r.GetComponent<Collider>());
                for (int k = 0; k <= 4; k++)
                {
                    float u = k / 4f;
                    var p = Vector3.Lerp(bottom, top, u);
                    var post = Box(root, "RailPost", new Vector3(p.x, p.y + railH * 0.5f, railZ), new Vector3(0.03f, railH, 0.03f), rail);
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }
            }
            // the deck's rail, across the back
            var dr = Box(root, "DeckRail", new Vector3(0f, deckY + 0.95f, 2.05f), new Vector3(deckHalf * 2f, 0.04f, 0.04f), rail);
            Object.DestroyImmediate(dr.GetComponent<Collider>());
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

        internal static void DoorFrame(Transform parent, string name, Vector3 basePos, Material mat, float width, bool facingCamera = false)
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

        internal static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = pos;
            return t;
        }

        /// <summary>Landscape only, on every build path.</summary>
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Stick Wick";
            PlayerSettings.productName = "Stick Wick";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.stickwick.game");
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

        [MenuItem("Stick Wick/Build Android APK")]
        public static void BuildAndroid()
        {
            BuildScene();
            ConfigurePlayer();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes.All,                      // every chapter (it used to be the menu, chapter 1 and the tower only)
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            Debug.Log($"[Stick Wick] Android build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
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
