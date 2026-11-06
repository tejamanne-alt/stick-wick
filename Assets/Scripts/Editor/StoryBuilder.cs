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
            var shaftMat = EnvironmentBuilder.TexMat("StoryShaftMat", "Legacy Shaders/Particles/Additive", shaftTex, Vector2.one, new Color(0.30f, 0.38f, 0.58f, 0.5f));
            Transform fan = null;
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
            var dropMat = PrototypeBuilder.Mat("StoryDroplet", "Unlit/Color", new Color(0.78f, 0.86f, 0.98f));
            var glowTex = EnvironmentBuilder.SaveTex("StoryGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var puddleMat = EnvironmentBuilder.TexMat("StoryPuddle", "Sprites/Default", glowTex, Vector2.one, new Color(0.30f, 0.36f, 0.48f, 0.45f));
            // the storm seam in the roof (it flares with the lightning), under and on top
            Crack(env, "RoofCrack", HeroStartX - 0.6f, RoofY - 0.03f, -0.7f, 1.9f, 0.035f, crackMat, 5);
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
                var d = Vis(GameObject.CreatePrimitive(PrimitiveType.Sphere).transform);
                d.name = "Droplet" + i; d.SetParent(env, false);
                d.position = new Vector3(HeroStartX - 0.6f, RoofY - 0.05f, 0f);
                d.localScale = Vector3.one * 0.05f;
                d.GetComponent<Renderer>().sharedMaterial = dropMat;
                d.GetComponent<Renderer>().enabled = false;
            }
            // the night sky behind the city (the towers themselves are in the set)
            SkyQuad(env, "Sky", new Vector3(0f, 60f, -320f), new Vector2(900f, 240f), sprite);

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
        public static void AddScar(GameObject hero, Material white)
        {
            var player = hero.GetComponentInChildren<ClipPlayer>(true);
            var head = player != null ? player.Bone("head") : null;
            if (head == null) return;
            var c = head.position + Vector3.up * 0.19f;
            for (int s = -1; s <= 1; s += 2)
            {
                var pts = new[] { new Vector3(0.40f, 0.62f, 0.68f * s), new Vector3(0.20f, 0.58f, 0.79f * s), new Vector3(0.26f, 0.44f, 0.86f * s), new Vector3(0.04f, 0.40f, 0.92f * s) };
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    var a = c + pts[i].normalized * 0.213f;
                    var b = c + pts[i + 1].normalized * 0.213f;
                    var g = Vis(GameObject.CreatePrimitive(PrimitiveType.Cube).transform);
                    g.name = "Scar";
                    g.position = (a + b) * 0.5f;
                    g.rotation = Quaternion.LookRotation(b - a, (a + b) * 0.5f - c);
                    g.localScale = new Vector3(0.012f, 0.006f, Vector3.Distance(a, b) + 0.008f);
                    g.SetParent(head, true);
                    var r = g.GetComponent<Renderer>();
                    r.sharedMaterial = white;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
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
