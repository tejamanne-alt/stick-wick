using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>STORY MODE, PROLOGUE: the warehouse where he wakes up, and the yard around it.
    ///
    /// Inside (the 2.5D fight lane runs along x at z = 0): a corrugated-iron shed 28 m long, open
    /// to the camera like the tower. A cracked roof with a seam of storm light over the spot he
    /// lies on; girders, hanging chains, a dead press and lathe, a scrap pile, barrels, puddles
    /// and long fake shadows; at the far right, a rusted roll-up shutter with moonlight spilling
    /// under it (the way out, and the way the first men come in).
    ///
    /// Outside (only the opening wide shot sees it): a labyrinth of stacked containers and
    /// crushed cars, sodium lamps, and far off a rain-soaked city under an amber and purple haze.
    /// Every prop is visual only (no colliders) so nothing can become a floor.</summary>
    public static class StoryBuilder
    {
        public const float HalfW = 14.0f, RoofY = 7.2f, BackZ = 2.6f, ShutterX = 12.0f;
        public const float HeroStartX = -8.0f;
        /// <summary>Where the mooring chain lies across the floor, and where his knife lies.</summary>
        public const float ChainX = 3.3f, KnifeX = 0.45f;

        /// <summary>A water drop seen from below against the light, as a ball lens shows it: nearly
        /// clear; the light above it seen through it small, upside down and soft (a curved smear,
        /// no edges); a thin fresnel rim; a soft dark ring where the light reflects inside; one sharp
        /// highlight; and a bright crescent at the bottom where it focuses the light. Nothing in it
        /// is a hard line.</summary>
        static Texture2D DropTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float c = n * 0.5f, r = n * 0.39f;
            float G(float x, float s) => Mathf.Exp(-(x * x) / (2f * s * s));
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float dx = (i + 0.5f - c) / r, dy = (j + 0.5f - c) / r;          // -1..1 across the drop
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01((1f - d) * r + 0.5f);
                    if (inside <= 0f) { tex.SetPixel(i, j, new Color(1f, 1f, 1f, 0f)); continue; }
                    // the body: water, barely there, a touch of the cold light it carries
                    Color col = new Color(0.8f, 0.87f, 0.98f);
                    float a = 0.14f + 0.1f * (1f - d * d);                          // enough body to follow as it falls
                    // the light above, through the lens: inverted and soft, a straight band low across it
                    float band = G(dy + 0.2f, 0.12f) * G(dx, 0.55f);
                    col = Color.Lerp(col, new Color(0.95f, 0.97f, 1f), band * 0.9f); a = Mathf.Max(a, 0.55f * band);
                    // the edge: water bends the dark room in from round the sides, so it darkens towards the rim
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.97f, d));
                    col = Color.Lerp(col, new Color(0.06f, 0.07f, 0.1f), edge * 0.75f); a = Mathf.Lerp(a, 0.42f, edge * 0.8f);
                    // light caught on the lit side only: a thin crescent, upper left
                    float lit = Mathf.Clamp01((-dx + dy) * 0.9f);
                    float rim = G(d - 0.955f, 0.04f) * (0.35f + 0.65f * lit * lit);
                    col = Color.Lerp(col, new Color(0.94f, 0.97f, 1f), rim); a = Mathf.Max(a, 0.9f * rim);
                    // the bright caustic at the bottom: the lens focusing the light it caught
                    float cres = G(d - 0.8f, 0.08f) * Mathf.Clamp01(-dy * 1.3f) * Mathf.Clamp01(1f - Mathf.Abs(dx) * 1.2f);
                    col = Color.Lerp(col, Color.white, cres * 0.7f); a = Mathf.Max(a, 0.45f * cres);
                    // one small sharp highlight, up on the lit side, near the edge
                    float hl = Mathf.Clamp01(G(dx + 0.4f, 0.09f) * G(dy - 0.48f, 0.075f) * 1.3f);
                    col = Color.Lerp(col, Color.white, hl); a = Mathf.Max(a, hl);
                    col.a = a * inside;
                    tex.SetPixel(i, j, col);
                }
            tex.Apply();
            return tex;
        }

        static Transform Vis(Transform t)
        {
            var c = t.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            return t;
        }

        static Transform VBox(Transform parent, string name, Vector3 pos, Vector3 size, Material m, float yaw = 0f)
        {
            var t = Vis(PrototypeBuilder.Box(parent, name, pos, size, m));
            if (yaw != 0f) t.rotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        /// <summary>A flat floor quad (y up) from four corners with vertex colours, Sprites/Default.</summary>
        static Transform FloorQuad(Transform parent, string name, Vector3[] c, Color[] col, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = c;
            mesh.colors = col;
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            string path = $"Assets/Generated/Story_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>A zigzag strip (the roof crack), lying in a horizontal plane at height y.</summary>
        static Transform Crack(Transform parent, string name, float x, float y, float z0, float z1, float width, Material m, int seed)
        {
            var rnd = new System.Random(seed);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                float z = Mathf.Lerp(z0, z1, i / (float)n);
                float jx = x + (float)(rnd.NextDouble() - 0.5) * 0.36f;
                float wv = width * (0.4f + (float)rnd.NextDouble());
                verts.Add(new Vector3(jx - wv, y, z));
                verts.Add(new Vector3(jx + wv, y, z));
                if (i > 0)
                {
                    int k = i * 2;
                    tris.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 });
                    tris.AddRange(new[] { k - 2, k - 1, k, k - 1, k + 1, k });       // both faces
                }
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            string path = $"Assets/Generated/Story_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>The roof sheet torn along the crack: on each side a ragged lip of iron bent down
        /// into the room (built on the crack's own jagged outline, so it follows it exactly), a pale
        /// worn edge where the metal tore, and rust bled out round it. Children of the crack, so they
        /// move with it.</summary>
        static void TornLips(Transform crack, Texture2D glowTex)
        {
            var src = crack.GetComponent<MeshFilter>().sharedMesh.vertices;
            int n = src.Length / 2;
            var rnd = new System.Random(17);
            var iron = PrototypeBuilder.Mat("StoryTornIron", "Unlit/Color", new Color(0.075f, 0.07f, 0.068f));
            var worn = PrototypeBuilder.Mat("StoryTornEdge", "Unlit/Color", new Color(0.34f, 0.29f, 0.25f));
            foreach (int side in new[] { -1, 1 })
            {
                var v = new List<Vector3>(); var e = new List<Vector3>(); var tri = new List<int>(); var etri = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    var inner = src[i * 2 + (side < 0 ? 0 : 1)];
                    float reach = 0.05f + (float)rnd.NextDouble() * 0.09f;          // how far the torn sheet hangs
                    float bend = 0.03f + (float)rnd.NextDouble() * 0.07f;           // and how far down it is bent
                    var tip = inner + new Vector3(side * 0.004f, -bend, 0f);
                    var root = inner + new Vector3(side * reach, 0.012f, 0f);
                    v.Add(root); v.Add(tip);
                    e.Add(tip); e.Add(tip + new Vector3(side * 0.012f, 0.004f, 0f));
                    if (i > 0)
                    {
                        int k = i * 2;
                        tri.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1, k - 2, k - 1, k, k - 1, k + 1, k });
                        etri.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1, k - 2, k - 1, k, k - 1, k + 1, k });
                    }
                }
                Strip(crack, "CrackLip" + (side < 0 ? "L" : "R"), v, tri, iron);
                Strip(crack, "CrackLipEdge" + (side < 0 ? "L" : "R"), e, etri, worn);
            }
            // rust bled out round the tear, soft
            var stain = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            stain.name = "CrackRust"; stain.SetParent(crack, false);
            var mid = (src[n] + src[n + 1]) * 0.5f;
            stain.localPosition = new Vector3(mid.x, mid.y + 0.022f, (src[0].z + src[src.Length - 1].z) * 0.5f);
            stain.localRotation = Quaternion.Euler(90f, 0f, 0f);
            stain.localScale = new Vector3(0.9f, (src[src.Length - 1].z - src[0].z) + 0.6f, 1f);
            stain.GetComponent<Renderer>().sharedMaterial = EnvironmentBuilder.TexMat("StoryCrackRust", "Sprites/Default", glowTex, Vector2.one, new Color(0.36f, 0.17f, 0.08f, 0.55f));
        }

        static void Strip(Transform parent, string name, List<Vector3> v, List<int> tri, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            string path = $"Assets/Generated/Story_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public const string SetPath = "Assets/Art/Story/StorySet.fbx";

        public static void Build(Transform room, Material ground, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            // ------------------------------------------------------------ the modelled, baked set (tools/blender/build_story_set.py)
            var env = new GameObject("Warehouse").transform;
            env.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var setModel = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (setModel == null) throw new BuildFailedException("StorySet.fbx missing - run tools/blender/build_story_set.py");
            var set = (GameObject)Object.Instantiate(setModel, room);
            set.name = "StorySet";
            AlignAxes(set.transform);
            var shaftTex = EnvironmentBuilder.SaveTex("StoryShaft", ShaftTexture(64, 256), TextureWrapMode.Clamp);
            var shaftMat = EnvironmentBuilder.TexMat("StoryShaftMat", "Legacy Shaders/Particles/Additive", shaftTex, Vector2.one, new Color(0.15f, 0.19f, 0.31f, 0.5f));
            var searchMat = EnvironmentBuilder.TexMat("StorySearchBeam", "Legacy Shaders/Particles/Additive", shaftTex, Vector2.one, new Color(0.42f, 0.48f, 0.62f, 0.5f));
            Transform fan = null;
            var swapR = new List<Renderer>(); var swapN = new List<Material>(); var swapF = new List<Material>();
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                string n = r.gameObject.name;
                Material m;
                if (n.StartsWith("Shafts")) m = shaftMat;
                else
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Story/Story_{n}.png");
                    m = EnvironmentBuilder.TexMat("StorySet_" + n, "Unlit/Texture", tex, Vector2.one);
                }
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                if (n == "Fan") fan = r.transform;
                // the lightning bake of this part, if there is one
                var flashTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Story/Story_{n}_Lightning.png");
                if (flashTex != null)
                {
                    swapR.Add(r); swapN.Add(m);
                    swapF.Add(EnvironmentBuilder.TexMat("StorySet_" + n + "_Lightning", "Unlit/Texture", flashTex, Vector2.one));
                }
            }
            foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            if (fan != null)
            {
                // the blades turn about the wall's normal (world z), round their own centre
                var pivot = new GameObject("FanPivot").transform;
                pivot.SetParent(set.transform, true);
                pivot.position = fan.GetComponent<Renderer>().bounds.center;
                pivot.rotation = Quaternion.identity;
                fan.SetParent(pivot, true);
                pivot.gameObject.AddComponent<Spin>().degreesPerSecond = 70f;
            }

            // ------------------------------------------------------------ what the scene animates, and the invisible floor
            var floor = PrototypeBuilder.Box(room, "Ground", new Vector3(0, -2f, 10f), new Vector3(160, 4, 160), ground);
            floor.GetComponent<Renderer>().enabled = false;                 // the baked slab and yard are the visible floor
            var sprite = PrototypeBuilder.Mat("StoryVertex", "Sprites/Default", Color.white);
            var crackMat = PrototypeBuilder.Mat("StoryCrack", "Unlit/Color", new Color(0.55f, 0.65f, 0.85f));
            var moonMat = PrototypeBuilder.Mat("StoryMoonGap", "Unlit/Color", new Color(0.72f, 0.80f, 0.95f));
            var dropMat = EnvironmentBuilder.TexMat("StoryDroplet", "Sprites/Default", EnvironmentBuilder.SaveTex("StoryDroplet", DropTexture(128), TextureWrapMode.Clamp), Vector2.one, Color.white);
            var glowTex = EnvironmentBuilder.SaveTex("StoryGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var puddleMat = EnvironmentBuilder.TexMat("StoryPuddle", "Sprites/Default", glowTex, Vector2.one, new Color(0.30f, 0.36f, 0.48f, 0.45f));
            // the storm seam in the roof (it flares with the lightning), under and on top
            var roofCrack = Crack(env, "RoofCrack", HeroStartX - 0.6f, RoofY - 0.03f, -0.7f, 1.9f, 0.035f, crackMat, 5);
            TornLips(roofCrack, glowTex);
            Crack(env, "RoofCrackTop", HeroStartX - 0.6f, RoofY + 0.36f, -0.7f, 1.9f, 0.06f, crackMat, 5);
            float doorL = ShutterX - 1.6f, doorR = ShutterX + 1.6f;
            VBox(env, "MoonGap", new Vector3(ShutterX, 0.05f, BackZ + 0.1f), new Vector3(doorR - doorL - 0.12f, 0.08f, 0.02f), moonMat);
            var moon = new Color(0.62f, 0.70f, 0.88f, 0.42f); var moon0 = new Color(0.62f, 0.70f, 0.88f, 0f);
            FloorQuad(env, "MoonSlit", new[] {
                new Vector3(doorL + 0.1f, 0.006f, BackZ), new Vector3(doorR - 0.1f, 0.006f, BackZ),
                new Vector3(doorL - 0.6f, 0.006f, -0.6f), new Vector3(doorR + 0.4f, 0.006f, -0.6f) },
                new[] { moon, moon, moon0, moon0 }, sprite);
            var flood = new Color(0.62f, 0.70f, 0.88f, 0.38f);
            var floodQuad = FloorQuad(env, "MoonFlood", new[] {
                new Vector3(doorL, 0.007f, BackZ), new Vector3(doorR, 0.007f, BackZ),
                new Vector3(doorL - 3.2f, 0.007f, -1.4f), new Vector3(doorR + 1.0f, 0.007f, -1.4f) },
                new[] { flood, flood, moon0, moon0 }, sprite);
            floodQuad.GetComponent<Renderer>().enabled = false;
            var shade = new Color(0f, 0f, 0f, 0.75f); var shade0 = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < 2; i++)
            {
                var q = FloorQuad(env, "SlitShadow" + i, new[] {
                    new Vector3(-0.35f, 0.009f, BackZ), new Vector3(0.35f, 0.009f, BackZ),
                    new Vector3(-0.9f, 0.009f, 0.2f), new Vector3(0.6f, 0.009f, 0.2f) },
                    new[] { shade, shade, shade0, shade0 }, sprite);
                q.position = new Vector3(doorL - 2f, 0f, 0f);
                q.GetComponent<Renderer>().enabled = false;
            }
            // a wet sheen on the puddles (the bake is matte)
            Puddle(env, puddleMat, HeroStartX - 0.7f, 0.15f, 1.3f, 0.7f);
            Puddle(env, puddleMat, -3.6f, 0.4f, 1.6f, 0.6f);
            Puddle(env, puddleMat, 1.2f, -0.1f, 1.1f, 0.5f);
            Puddle(env, puddleMat, 6.5f, 0.3f, 1.8f, 0.6f);
            Puddle(env, puddleMat, 9.8f, -0.2f, 1.0f, 0.45f);
            for (int i = 0; i < 2; i++)
            {
                var d = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);       // a lens, turned to the camera
                d.name = "Droplet" + i; d.SetParent(env, false);
                d.position = new Vector3(HeroStartX - 0.6f, RoofY - 0.05f, 0f);
                d.localScale = Vector3.one * 0.05f;
                d.GetComponent<Renderer>().sharedMaterial = dropMat;
                d.GetComponent<Renderer>().enabled = false;
            }
            // the escape: the chain he trips on, and his knife at the far edge of the puddle
            Chain(env, ChainX);
            FloorKnife(env, new Vector3(KnifeX, 0f, 0.04f), glowTex);
            // the night sky behind the city (the towers themselves are in the set)
            SkyQuad(env, "Sky", new Vector3(0f, 60f, -320f), new Vector2(900f, 240f), sprite);
            var swap = room.gameObject.AddComponent<LightningSwap>();
            swap.renderers = swapR.ToArray(); swap.normal = swapN.ToArray(); swap.flash = swapF.ToArray();

            // a helicopter searching the yard (the opening shot): a beam, its pool on the ground, a red light
            var beam = Beam(env, "Searchlight", 40f, 0.6f, 7f, searchMat);
            beam.position = new Vector3(30f, 30f, 28f);
            var redMat = EnvironmentBuilder.TexMat("StoryHeliRed", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(1f, 0.1f, 0.05f, 0.5f));
            var red = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            red.name = "HeliLight"; red.SetParent(env, false); red.localScale = Vector3.one * 2.2f;
            red.GetComponent<Renderer>().sharedMaterial = redMat;
            var poolMat = EnvironmentBuilder.TexMat("StorySearchPool", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.45f, 0.52f, 0.7f, 0.5f));
            var pool = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            pool.name = "SearchPool"; pool.SetParent(env, false); pool.rotation = Quaternion.Euler(90f, 0f, 0f); pool.localScale = new Vector3(9f, 6f, 1f);
            pool.GetComponent<Renderer>().sharedMaterial = poolMat;
            // the moonlight that floods in through the doorway when the shutter goes (hidden until then)
            var doorBeam = Beam(env, "DoorBeam", 7.5f, 3.0f, 4.4f, searchMat);
            doorBeam.position = new Vector3(ShutterX, 3.4f, BackZ + 0.4f);
            doorBeam.rotation = Quaternion.FromToRotation(Vector3.down, new Vector3(-0.15f, -0.55f, -1f));
            foreach (var rr in doorBeam.GetComponentsInChildren<Renderer>()) rr.enabled = false;

            // ------------------------------------------------------------ weather: rain outside, dust in the moonlight
            var particle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/InkParticle.mat");
            Rain(env, particle);
            Dust(env, particle);

            // ------------------------------------------------------------ spawn points: everyone comes through the shutter
            var door = new Vector3(ShutterX, 0f, BackZ + 0.5f);
            for (int i = 0; i < points.Length; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), door);
            heroStart = new Vector3(HeroStartX, 0f, 0f);
        }

        /// <summary>A light beam: two crossed quads from a narrow end (at the origin) to a wide end
        /// `length` down local -y, bright at the source; for the additive shaft material.</summary>
        static Transform Beam(Transform parent, string name, float length, float w0, float w1, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            foreach (var axis in new[] { Vector3.right, Vector3.forward })
            {
                int k = v.Count;
                v.Add(-axis * w0 * 0.5f); v.Add(axis * w0 * 0.5f);
                v.Add(axis * w1 * 0.5f + Vector3.down * length); v.Add(-axis * w1 * 0.5f + Vector3.down * length);
                uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 0));
                tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            string path = $"Assets/Generated/Story_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>The set is modelled in Blender (x along the lane, y the depth, z up). Three marker
        /// empties tell us where those axes landed after the FBX import; turn and (if needed) mirror the
        /// root so Blender x = Unity x, Blender y = Unity z, Blender z = Unity y, at 1:1.</summary>
        static void AlignAxes(Transform root)
        {
            root.position = Vector3.zero;
            root.rotation = Quaternion.identity;
            root.localScale = Vector3.one;
            Transform mx = null, my = null, mz = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Mark_X") mx = t;
                if (t.name == "Mark_Y") my = t;
                if (t.name == "Mark_Z") mz = t;
            }
            if (mx == null || my == null || mz == null) { Debug.LogWarning("[Stick Wick] StorySet: axis markers missing"); return; }
            var ax = mx.position / 10f; var ay = my.position / 10f; var az = mz.position / 10f;
            // M maps Blender axes to where they are now; we want T = (x -> x, y -> z, z -> y)
            var M = new Matrix4x4(ax, ay, az, new Vector4(0, 0, 0, 1));
            var T = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
            var C = T * M.inverse;                                       // correction to apply on top of the import
            float det = C.determinant;
            var S = Matrix4x4.identity;
            if (det < 0f) S = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
            var R = C * S;                                               // C = R * S  (S is its own inverse)
            Vector3 fwd = R.GetColumn(2), up = R.GetColumn(1);
            float k = fwd.magnitude;
            root.rotation = Quaternion.LookRotation(fwd, up);
            root.localScale = new Vector3(det < 0f ? -k : k, k, k);
            foreach (var t in new[] { mx, my, mz }) Object.DestroyImmediate(t.gameObject);
        }

        static void Rain(Transform parent, Material mat)
        {
            var go = new GameObject("RainOutside");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 14f, 10f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 14f;
            main.startSize = 0.03f;
            main.startColor = new Color(0.72f, 0.80f, 1f, 0.35f);
            main.maxParticles = 1800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = false;
            var em = ps.emission; em.rateOverTime = 1100f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(70f, 14f, 0.5f);
            go.transform.rotation = Quaternion.Euler(95f, 0f, 0f);       // emits along local z: down, a little slanted by the wind
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 2f;
            r.sharedMaterial = mat;
        }

        static void Dust(Transform parent, Material mat)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-1.5f, 2.4f, 0.8f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 9f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            main.startColor = new Color(0.75f, 0.82f, 1f, 0.22f);
            main.maxParticles = 220;
            main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 24f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(24f, 4.4f, 3.2f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.06f; noise.frequency = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
        }

        /// <summary>A moonbeam: bright near the window, fading towards the floor, soft at its edges.</summary>
        static Texture2D ShaftTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1), v = y / (float)(h - 1);
                    float edge = Mathf.Sin(u * Mathf.PI);
                    float a = edge * edge * Mathf.Lerp(0.15f, 1f, v);
                    px[y * w + x] = new Color(a, a, a, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A heavy rusted chain dropped across the floor in a lazy S, every link a real
        /// ring, alternating flat and on edge as a chain lies. Named "Chain" (the story shakes it).</summary>
        static void Chain(Transform parent, float x)
        {
            var root = new GameObject("Chain").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(x, 0f, 0f);
            var ring = RingMesh(0.05f, 0.03f, 0.0125f, 14, 6);
            var mat = PrototypeBuilder.Mat("StoryChain", "Unlit/Color", new Color(0.19f, 0.12f, 0.08f));
            var hi = PrototypeBuilder.Mat("StoryChainWorn", "Unlit/Color", new Color(0.33f, 0.24f, 0.17f));
            const int n = 30;
            Vector3 P(float k) => new Vector3(0.28f * Mathf.Sin(k * 5.2f) + 0.12f * Mathf.Sin(k * 11f), 0f, Mathf.Lerp(-1.25f, 1.35f, k));
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                var a = P(k); var b2 = P(Mathf.Min(1f, k + 0.01f));
                var link = new GameObject("Link" + i).transform;
                link.SetParent(root, false);
                bool edge = i % 2 == 1;
                link.localPosition = a + new Vector3(0f, edge ? 0.05f : 0.013f, 0f);
                var dir = (b2 - a).normalized;
                link.localRotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up) * Quaternion.Euler(0f, 90f, 0f)
                                     * (edge ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity);
                link.gameObject.AddComponent<MeshFilter>().sharedMesh = ring;
                var r = link.gameObject.AddComponent<MeshRenderer>();
                r.sharedMaterial = i % 5 == 2 ? hi : mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>An oval ring (a chain link) in the XZ plane: half-length a along X, half-width b
        /// along Z, tube radius r.</summary>
        static Mesh RingMesh(float a, float b, float r, int seg, int side)
        {
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(u) * a, 0f, Mathf.Sin(u) * b);
                var nrm = new Vector3(Mathf.Cos(u) * b, 0f, Mathf.Sin(u) * a).normalized;
                for (int j = 0; j <= side; j++)
                {
                    float w = j / (float)side * Mathf.PI * 2f;
                    v.Add(c + nrm * (Mathf.Cos(w) * r) + Vector3.up * (Mathf.Sin(w) * r));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a0 = i * (side + 1) + j, a1 = a0 + side + 1;
                    tri.AddRange(new[] { a0, a1, a0 + 1, a0 + 1, a1, a1 + 1, a0, a0 + 1, a1, a0 + 1, a1 + 1, a1 });
                }
            var m = new Mesh { name = "ChainLink" };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            System.IO.Directory.CreateDirectory("Assets/Generated");
            AssetDatabase.DeleteAsset("Assets/Generated/ChainLink.asset");
            AssetDatabase.CreateAsset(m, "Assets/Generated/ChainLink.asset");
            return m;
        }

        /// <summary>His combat knife lying flat on the wet floor, blade half in the puddle, with a
        /// glint sprite the story flares when the lightning catches it. "FloorKnife", "KnifeGlint".</summary>
        static void FloorKnife(Transform parent, Vector3 at, Texture2D glowTex)
        {
            var root = new GameObject("FloorKnife").transform;
            root.SetParent(parent, false);
            root.position = at;
            root.rotation = Quaternion.Euler(0f, -72f, 0f);               // blade towards the puddle, handle towards him
            var steel = PrototypeBuilder.Mat("StoryKnifeSteel", "Unlit/Color", new Color(0.52f, 0.56f, 0.62f));
            var edge = PrototypeBuilder.Mat("StoryKnifeEdge", "Unlit/Color", new Color(0.82f, 0.86f, 0.92f));
            var grip = PrototypeBuilder.Mat("StoryKnifeGrip", "Unlit/Color", new Color(0.05f, 0.05f, 0.055f));
            void Part(string n, Vector3 p, Vector3 sz, Material m)
            {
                var c = Vis(GameObject.CreatePrimitive(PrimitiveType.Cube).transform);
                c.name = n; c.SetParent(root, false);
                c.localPosition = p; c.localScale = sz;
                c.GetComponent<Renderer>().sharedMaterial = m;
                c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Part("Grip", new Vector3(0f, 0.018f, -0.07f), new Vector3(0.036f, 0.032f, 0.13f), grip);
            Part("Guard", new Vector3(0f, 0.018f, 0.0f), new Vector3(0.075f, 0.03f, 0.016f), grip);
            Part("Blade", new Vector3(0f, 0.008f, 0.12f), new Vector3(0.042f, 0.008f, 0.22f), steel);
            Part("Edge", new Vector3(0.019f, 0.009f, 0.12f), new Vector3(0.006f, 0.009f, 0.21f), edge);
            Part("Tip", new Vector3(0.008f, 0.008f, 0.24f), new Vector3(0.025f, 0.008f, 0.04f), edge);
            var glint = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            glint.name = "KnifeGlint"; glint.SetParent(root, false);
            glint.localPosition = new Vector3(0.01f, 0.03f, 0.17f);
            glint.localScale = Vector3.one * 0.35f;
            var gm = EnvironmentBuilder.TexMat("StoryKnifeGlint", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.8f, 0.88f, 1f, 0.5f));
            var gr = glint.GetComponent<Renderer>();
            gr.sharedMaterial = gm;
            gr.enabled = false;
        }

        static void Puddle(Transform parent, Material m, float x, float z, float sx, float sz)
        {
            var p = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            p.name = "Puddle"; p.SetParent(parent, false);
            p.position = new Vector3(x, 0.005f, z);
            p.rotation = Quaternion.Euler(90f, 0f, 0f);
            p.localScale = new Vector3(sx, sz, 1f);
            p.GetComponent<Renderer>().sharedMaterial = m;
        }

        static void SkyQuad(Transform parent, string name, Vector3 c, Vector2 size, Material m)
        {
            // facing +z (towards the yard), a vertical gradient: night up top, purple, then the amber haze
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var verts = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            float[] ys = { -0.5f, -0.38f, -0.2f, 0.1f, 0.5f };
            Color[] cs = { new Color(0.30f, 0.16f, 0.08f), new Color(0.24f, 0.11f, 0.14f), new Color(0.12f, 0.06f, 0.16f), new Color(0.04f, 0.035f, 0.08f), new Color(0.015f, 0.018f, 0.035f) };
            for (int i = 0; i < ys.Length; i++)
            {
                verts.Add(c + new Vector3(size.x * 0.5f, ys[i] * size.y, 0f)); cols.Add(cs[i]);
                verts.Add(c + new Vector3(-size.x * 0.5f, ys[i] * size.y, 0f)); cols.Add(cs[i]);
                if (i > 0) { int k = i * 2; tris.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1, k - 2, k - 1, k, k - 1, k + 1, k }); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            string path = $"Assets/Generated/Story_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        /// <summary>The scar he wakes with: a jagged white hairline at the temple (both sides of
        /// the head, so it shows whichever way he faces). Built in the rest pose like the eyes.</summary>
        /// <summary>The head wound that took his memory, on the right temple (the side his right
        /// hand goes to, and the side the camera sees): one short, slightly curved gash, ragged
        /// and tapered, dark at its lips and wet red in the cut, with a thin trickle of dried blood
        /// running down past the eye and ending in a bead. Real ribbons laid on the head sphere.</summary>
        public static void AddScar(GameObject hero, Material unused = null)
        {
            var player = hero.GetComponentInChildren<ClipPlayer>(true);
            var head = player != null ? player.Bone("head") : null;
            if (head == null) return;
            var restore = FighterFactory.ToBindPose(player.gameObject);       // measured in the rest pose, like the eyes
            var fwd = FighterFactory.Forward(player, player.moves);
            FighterFactory.ToBindPose(player.gameObject);
            var right = -Vector3.Cross(fwd, Vector3.up);                        // Cross(fwd, up) is his left
            var c = head.position + Vector3.up * 0.19f;
            const float R = 0.21f;
            Vector3 On(float f, float u, float r) => c + (fwd * f + Vector3.up * u + right * r).normalized * R;

            var lip = PrototypeBuilder.Mat("WoundLip", "Unlit/Color", new Color(0.30f, 0.02f, 0.025f));
            var cut = PrototypeBuilder.Mat("WoundCut", "Unlit/Color", new Color(0.72f, 0.07f, 0.06f));
            var dried = PrototypeBuilder.Mat("WoundBlood", "Unlit/Color", new Color(0.40f, 0.025f, 0.025f));

            // the gash: from up behind the temple, down and forward towards the brow
            var gash = Smooth(new[] { On(0.22f, 0.66f, 0.72f), On(0.34f, 0.55f, 0.77f), On(0.47f, 0.45f, 0.77f), On(0.58f, 0.36f, 0.73f) }, 10);
            var wound = hero.AddComponent<HeadWound>();
            wound.lip = Ribbon(head, "WoundLip", gash, c, k => 0.016f * Taper(k, 0.18f) * (1f + 0.1f * Mathf.Sin(k * 23f)), 0.0016f, lip);
            wound.cut = Ribbon(head, "WoundCut", gash, c, k => 0.0065f * Taper(k, 0.3f) * (1f + 0.2f * Mathf.Sin(k * 37f + 1f)), 0.0028f, cut);
            // the trickle: from the low end of the cut, running down the side of the face
            var drip = Smooth(new[] { On(0.56f, 0.35f, 0.74f), On(0.585f, 0.22f, 0.78f), On(0.6f, 0.08f, 0.8f), On(0.59f, -0.05f, 0.81f) }, 10);
            wound.blood = Ribbon(head, "WoundBlood", drip, c, k => 0.0045f * (1f - 0.55f * k) * Mathf.Min(1f, k * 8f + 0.4f), 0.0014f, dried);
            var bead = Vis(GameObject.CreatePrimitive(PrimitiveType.Sphere).transform);
            bead.name = "WoundBead";
            bead.position = drip[drip.Length - 1] + (drip[drip.Length - 1] - c).normalized * 0.001f;
            bead.localScale = new Vector3(0.0075f, 0.009f, 0.0075f);
            bead.SetParent(head, true);
            var br = bead.GetComponent<Renderer>();
            br.sharedMaterial = dried;
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wound.bead = br;
            restore?.Invoke();
        }

        static float Taper(float k, float ends) => Mathf.SmoothStep(0f, 1f, Mathf.Min(k, 1f - k) / ends);

        /// <summary>Catmull-Rom through the points, `n` steps per span.</summary>
        static Vector3[] Smooth(Vector3[] p, int n)
        {
            var o = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < p.Length - 1; i++)
            {
                Vector3 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
                for (int s = 0; s < n; s++)
                {
                    float t = s / (float)n, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(p[p.Length - 1]);
            return o.ToArray();
        }

        /// <summary>A flat strip along `path` on the head's surface (centre `c`), width(k) for k in 0..1,
        /// lifted `lift` off the skin, parented to the head bone. Both faces, so it never vanishes.</summary>
        static MeshFilter Ribbon(Transform head, string name, Vector3[] path, Vector3 c, System.Func<float, float> width, float lift, Material mat)
        {
            int n = path.Length;
            var go = new GameObject(name);
            go.transform.SetParent(head, false);
            var verts = new Vector3[n * 2];
            for (int i = 0; i < n; i++)
            {
                var nrm = (path[i] - c).normalized;
                var tan = (path[Mathf.Min(n - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                var across = Vector3.Cross(nrm, tan).normalized;
                float w = width(i / (float)(n - 1)) * 0.5f;
                var p = path[i] + nrm * lift;
                verts[i * 2] = go.transform.InverseTransformPoint(p - across * w);
                verts[i * 2 + 1] = go.transform.InverseTransformPoint(p + across * w);
            }
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2, b = a + 1, d = a + 2, e = a + 3;
                tris.AddRange(new[] { a, d, b, b, d, e, a, b, d, b, e, d });
            }
            var mesh = new Mesh { name = name, vertices = verts, triangles = tris.ToArray() };
            mesh.RecalculateBounds();
            System.IO.Directory.CreateDirectory("Assets/Generated");
            string asset = $"Assets/Generated/{name}.asset";
            AssetDatabase.DeleteAsset(asset);
            AssetDatabase.CreateAsset(mesh, asset);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mf;
        }

        // preview the wound's healing in play mode (each story chapter sets its own stage)
        [MenuItem("Stick Wick/Wound Stage/1 Fresh")] static void W1() => PreviewWound(1);
        [MenuItem("Stick Wick/Wound Stage/2 Scabbed")] static void W2() => PreviewWound(2);
        [MenuItem("Stick Wick/Wound Stage/3 Closing")] static void W3() => PreviewWound(3);
        [MenuItem("Stick Wick/Wound Stage/4 Healed")] static void W4() => PreviewWound(4);
        [MenuItem("Stick Wick/Wound Stage/5 Old Scar")] static void W5() => PreviewWound(5);
        [MenuItem("Stick Wick/Wound Stage/Follow Chapter")] static void W0() => PreviewWound(0);

        static void PreviewWound(int stage)
        {
            foreach (var w in Object.FindObjectsByType<HeadWound>()) w.previewChapter = stage;
        }

        // ------------------------------------------------------------ textures

        /// <summary>Corrugated iron: vertical ribs, rust streaks running down, grime at the foot.</summary>
        static Texture2D CorrugatedTexture(int n, Color baseColor, int seed)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[n * n];
            var r = new System.Random(seed);
            var streak = new float[n];
            for (int x = 0; x < n; x++) streak[x] = r.NextDouble() < 0.12 ? (float)r.NextDouble() : 0f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float rib = 0.82f + 0.18f * Mathf.Sin(x / (float)n * Mathf.PI * 2f * 14f);
                    float noise = EnvironmentBuilder.Noise(x / 16f, y / 16f, n / 16, seed) * 0.12f - 0.06f;
                    float v = y / (float)(n - 1);
                    float rustAmt = streak[x] * Mathf.Clamp01(1.1f - v) * 0.5f;
                    var c = baseColor * (rib + noise);
                    c = Color.Lerp(c, new Color(0.30f, 0.15f, 0.08f), rustAmt);
                    c *= Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(v * 3f));           // grime at the foot
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A roll-up shutter: horizontal slats with dark seams and a rusty bottom edge.</summary>
        static Texture2D ShutterTexture(int n, Color baseColor)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float slat = (y % 8) / 8f;
                    float k = slat < 0.15f ? 0.45f : 0.9f + 0.1f * Mathf.Sin(slat * Mathf.PI);
                    var c = baseColor * k;
                    if (y < 10) c = Color.Lerp(c, new Color(0.26f, 0.13f, 0.07f), 0.6f);
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
