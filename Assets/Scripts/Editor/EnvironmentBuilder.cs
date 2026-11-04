using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectInk.EditorTools
{
    /// <summary>The premium black-and-white dressing of the building, generated from code: architectural
    /// concrete with formwork panels and tie holes, windows onto a night skyline, hanging lamps
    /// with warm light cones, ceiling pipes, an exit sign, stair balusters, edge trims, crates,
    /// haze and dust. Textures are generated once and saved under Assets/Generated/Textures.</summary>
    public static class EnvironmentBuilder
    {
        const string TexDir = "Assets/Generated/Textures";

        public struct Layout
        {
            public float halfW, floorY, roofY, wallZ, laneFront, ledgeX;
            public float deckY, deckHalf, stairRun;    // the raised central deck and its two flights of stairs
        }

        // ---------------------------------------------------------------- materials

        public static Material TexMat(string name, string shader, Texture2D tex, Vector2 tiling, Color? color = null)
        {
            string path = $"Assets/Generated/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            if (color.HasValue)
            {
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color.Value);
                if (m.HasProperty("_Color")) m.SetColor("_Color", color.Value);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static Material ColorMat(string name, Color c)
        {
            string path = $"Assets/Generated/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Unlit/Color");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.color = c;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- build

        public static void Build(Transform root, Layout L, Material ground)
        {
            Directory.CreateDirectory(TexDir);
            var concrete = SaveTex("Concrete", ConcreteTexture(512, new Color(0.60f, 0.60f, 0.60f), 11));
            var concreteDark = SaveTex("ConcreteDark", ConcreteTexture(512, new Color(0.20f, 0.20f, 0.20f), 23));
            var sky = SaveTex("Skyline", SkylineTexture(1024, 512, 5), TextureWrapMode.Clamp);
            var cone = SaveTex("LightCone", ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glow = SaveTex("Glow", GlowTexture(128), TextureWrapMode.Clamp);

            var wallMat = TexMat("WallConcrete", "Unlit/Texture", concrete, new Vector2(L.halfW * 2f / 4.4f, L.roofY / 4.4f));
            var pillarMat = TexMat("PillarConcrete", "Unlit/Texture", concreteDark, new Vector2(0.4f, 2f));
            var skyMat = TexMat("Sky", "Unlit/Texture", sky, Vector2.one);
            var glassMat = TexMat("Glass", "Sprites/Default", null, Vector2.one, new Color(0.75f, 0.75f, 0.78f, 0.10f));
            var coneMat = TexMat("LampCone", "Legacy Shaders/Particles/Additive", cone, Vector2.one, new Color(0.42f, 0.42f, 0.42f, 0.5f));
            var glowMat = TexMat("LampGlow", "Legacy Shaders/Particles/Additive", glow, Vector2.one, new Color(0.55f, 0.55f, 0.55f, 0.5f));
            var moonMat = TexMat("MoonShaft", "Legacy Shaders/Particles/Additive", cone, Vector2.one, new Color(0.16f, 0.16f, 0.17f, 0.5f));
            var exitGlowMat = TexMat("ExitGlow", "Legacy Shaders/Particles/Additive", glow, Vector2.one, new Color(0.35f, 0.35f, 0.35f, 0.5f));
            var hazeMat = TexMat("Haze", "Sprites/Default", null, Vector2.one, Color.white);
            var frame = ColorMat("Frame", new Color(0.07f, 0.07f, 0.07f));
            var trim = ColorMat("Trim", new Color(0.78f, 0.78f, 0.78f));          // crisp white edge highlights
            var metal = ColorMat("Metal", new Color(0.13f, 0.13f, 0.13f));
            var highlight = ColorMat("Highlight", new Color(0.55f, 0.55f, 0.55f));
            var wood = ColorMat("Crate", new Color(0.19f, 0.19f, 0.19f));
            var exitGreen = ColorMat("ExitSign", new Color(0.93f, 0.93f, 0.93f));
            var shade = ColorMat("LampShade", new Color(0.05f, 0.05f, 0.05f));
            var bulb = ColorMat("Bulb", new Color(1f, 1f, 1f));

            var env = new GameObject("Environment").transform;
            env.SetParent(root, false);

            // ---- back wall: high windows over each ground area, two high on the left, a tall hall window,
            // a back door on the right and a doorway onto the deck
            var windows = new List<Rect>
            {
                // high clerestory windows, above head height: fighters always stand against lit concrete
                new Rect(-11.2f, 1.95f, 2.6f, 0.85f), new Rect(7.4f, 1.95f, 2.4f, 0.85f),
                new Rect(-10.6f, 5.0f, 2.6f, 0.95f), new Rect(-5.6f, 5.2f, 2.2f, 0.9f),
                new Rect(4.2f, 3.8f, 4.2f, 2.4f),                                              // tall window over the hall
            };
            var doors = new List<Rect>
            {
                new Rect(10.2f, 0f, 1.6f, 2.3f),                                               // back door (ground, right)
                new Rect(-0.8f, L.deckY, 1.6f, 2.3f),                                          // doorway onto the deck
            };
            var openings = new List<Rect>(windows);
            openings.AddRange(doors);
            WallWithOpenings(env, "BackWall", -L.halfW, L.halfW, 0f, L.roofY, L.wallZ, 0.3f, openings, wallMat);
            foreach (var wr in windows) Window(env, wr, L.wallZ, frame, glassMat);
            // a dim, warm corridor beyond each door (not black: the hero must read in front of it)
            var corridor = ColorMat("Corridor", new Color(0.36f, 0.36f, 0.36f));
            foreach (var door in doors)
            {
                Box(env, "BackCorridor", new Vector3(door.center.x, door.center.y, L.wallZ + 0.25f), new Vector3(door.width, door.height, 0.05f), corridor);
                FrameAround(env, door, L.wallZ - 0.17f, 0.12f, frame);
            }

            // night sky and skyline behind the windows
            var skyGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(skyGo.GetComponent<Collider>());
            skyGo.name = "Skyline";
            skyGo.transform.SetParent(env, false);
            skyGo.transform.position = new Vector3(0f, 4.5f, 12f);
            skyGo.transform.localScale = new Vector3(60f, 30f, 1f);
            skyGo.GetComponent<Renderer>().sharedMaterial = skyMat;

            // structure: concrete pillars at the outer walls and the hall, crisp edge trims on the floors
            Box(env, "PillarLeft", new Vector3(-L.halfW + 0.45f, L.roofY * 0.5f, L.wallZ - 0.3f), new Vector3(0.5f, L.roofY, 0.3f), pillarMat);
            Box(env, "PillarRight", new Vector3(L.halfW - 0.45f, L.roofY * 0.5f, L.wallZ - 0.3f), new Vector3(0.5f, L.roofY, 0.3f), pillarMat);
            Box(env, "TrimGround", new Vector3(0f, -0.02f, L.laneFront - 0.01f), new Vector3(L.halfW * 2f, 0.05f, 0.02f), trim);
            Box(env, "Skirting", new Vector3(0f, 0.08f, L.wallZ - 0.16f), new Vector3(L.halfW * 2f, 0.16f, 0.03f), frame);
            Box(env, "SkirtingDeck", new Vector3(0f, L.deckY + 0.08f, L.wallZ - 0.16f), new Vector3(L.deckHalf * 2f, 0.16f, 0.03f), frame);

            // ceiling pipes
            Pipe(env, "PipeRoofA", new Vector3(0f, L.roofY - 0.35f, L.wallZ - 0.35f), L.halfW * 2f, 0.1f, metal);
            Pipe(env, "PipeRoofB", new Vector3(0f, L.roofY - 0.6f, L.wallZ - 0.5f), L.halfW * 2f, 0.05f, metal);
            // a thin lit edge along the top of each big pipe (rim light)
            Box(env, "PipeRoofRim", new Vector3(0f, L.roofY - 0.25f, L.wallZ - 0.42f), new Vector3(L.halfW * 2f, 0.014f, 0.02f), highlight);

            // moonlight falling through the tall hall window in long slanted shafts
            for (int i = 0; i < 3; i++)
            {
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(shaft.GetComponent<Collider>());
                shaft.name = "MoonShaft" + i;
                shaft.transform.SetParent(env, false);
                shaft.transform.position = new Vector3(5.2f + i * 1.3f, L.floorY + 0.6f, L.wallZ - 0.6f);
                shaft.transform.rotation = Quaternion.Euler(0f, 0f, 24f);
                shaft.transform.localScale = new Vector3(1.4f, 7.5f, 1f);
                shaft.GetComponent<Renderer>().sharedMaterial = moonMat;
            }

            // hanging lamps with warm cones of light
            Lamp(env, new Vector3(-9.5f, L.roofY - 0.05f, 1.6f), 2.6f, shade, bulb, coneMat, glowMat);
            Lamp(env, new Vector3(-1.5f, L.roofY - 0.05f, 1.6f), 1.4f, shade, bulb, coneMat, glowMat);   // over the deck
            Lamp(env, new Vector3(4.0f, L.roofY - 0.05f, 1.6f), 2.6f, shade, bulb, coneMat, glowMat);
            Lamp(env, new Vector3(9.5f, L.roofY - 0.05f, 1.6f), 2.6f, shade, bulb, coneMat, glowMat);

            // exit sign over the right door
            Box(env, "ExitSign", new Vector3(L.halfW - 1.0f, 2.75f, L.wallZ - 0.2f), new Vector3(0.6f, 0.22f, 0.05f), exitGreen);
            Billboard(env, "ExitGlow", new Vector3(L.halfW - 1.0f, 2.75f, L.wallZ - 0.25f), new Vector2(1.6f, 1.0f), exitGlowMat);

            // crates in the corners (behind the fight lane, decoration)
            Box(env, "CrateA", new Vector3(-11.6f, 0.4f, 1.7f), new Vector3(0.8f, 0.8f, 0.8f), wood);
            Box(env, "CrateB", new Vector3(-10.8f, 0.3f, 1.8f), new Vector3(0.6f, 0.6f, 0.6f), wood);
            Box(env, "CrateC", new Vector3(-11.3f, 1.05f, 1.8f), new Vector3(0.5f, 0.5f, 0.5f), wood);
            Box(env, "CrateD", new Vector3(12.4f, 0.35f, 1.8f), new Vector3(0.6f, 0.7f, 0.6f), wood);

            // low haze over each floor and drifting dust in the light
            Haze(env, "HazeGround", 0f, L, hazeMat);
            Haze(env, "HazeUpper", L.deckY, L, hazeMat);
            Dust(env, L, glow);
        }

        // ---------------------------------------------------------------- props you can use

        /// <summary>Ropes to swing on, steel bars for giant circles, crates to kick.</summary>
        public static void Props(Transform root, Layout L, Material trailMat)
        {
            var steel = ColorMat("Steel", new Color(0.16f, 0.16f, 0.16f));
            var steelRim = ColorMat("SteelRim", new Color(0.7f, 0.7f, 0.7f));
            var crateMat = ColorMat("CrateLane", new Color(0.24f, 0.24f, 0.24f));
            var crateEdge = ColorMat("CrateEdge", new Color(0.52f, 0.52f, 0.52f));
            var ropeMat = TexMat("Rope", "Sprites/Default", null, Vector2.one, Color.white);
            var props = new GameObject("Props").transform;
            props.SetParent(root, false);

            // bars: low enough under each ceiling for a full giant circle (bar ± 1.43 m)
            Bar(props, new Vector3(-10.2f, 1.55f, 0f), L.roofY, steel, steelRim);              // over the left ground
            Bar(props, new Vector3(10.0f, 1.55f, 0f), L.roofY, steel, steelRim);               // over the right ground
            Bar(props, new Vector3(0f, L.deckY + 1.55f, 0f), L.roofY, steel, steelRim);        // over the deck
            // ropes from the roof over each flight of stairs: swing between the ground and the deck
            float stairMid = L.deckHalf + L.stairRun * 0.5f;
            Rope(props, new Vector3(-stairMid, L.roofY - 0.2f, 0f), 4.0f, ropeMat);
            Rope(props, new Vector3(stairMid, L.roofY - 0.2f, 0f), 4.0f, ropeMat);
            // crates in the fight lane: stand on them, or kick them into enemies
            Crate(props, new Vector3(-8.6f, 0f, 0f), 0.7f, crateMat, crateEdge);
            Crate(props, new Vector3(8.4f, 0f, 0f), 0.7f, crateMat, crateEdge);
            Crate(props, new Vector3(1.8f, L.deckY, 0f), 0.7f, crateMat, crateEdge);
        }

        internal static void Bar(Transform parent, Vector3 center, float ceiling, Material steel, Material rim)
        {
            var root = new GameObject("Bar").transform;
            root.SetParent(parent, false);
            root.position = center;
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(rod.GetComponent<Collider>());
            rod.name = "Rod";
            rod.transform.SetParent(root, false);
            rod.transform.position = center;
            rod.transform.rotation = Quaternion.Euler(90f, 0f, 0f);           // end-on to the camera
            rod.transform.localScale = new Vector3(0.09f, 0.7f, 0.09f);
            rod.GetComponent<Renderer>().sharedMaterial = steel;
            // hung from the ceiling by two thin rods, front and back
            foreach (float z in new[] { -0.62f, 0.62f })
            {
                float h = ceiling - center.y;
                Box(root, "Hanger", new Vector3(center.x, center.y + h * 0.5f, z), new Vector3(0.025f, h, 0.025f), steel);
            }
            Box(root, "Glint", center + new Vector3(-0.015f, 0.025f, -0.71f), new Vector3(0.02f, 0.02f, 0.01f), rim);
            var sw = root.gameObject.AddComponent<Swingable>();
            sw.kind = SwingKind.Bar;
            sw.anchor = center;
        }

        internal static void Rope(Transform parent, Vector3 anchor, float length, Material mat)
        {
            var go = new GameObject("Rope");
            go.transform.SetParent(parent, false);
            go.transform.position = anchor;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.045f;
            lr.startColor = lr.endColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            lr.SetPosition(0, anchor);
            lr.SetPosition(1, anchor + Vector3.down * length);
            lr.shadowCastingMode = ShadowCastingMode.Off;
            var sw = go.AddComponent<Swingable>();
            sw.kind = SwingKind.Rope;
            sw.anchor = anchor;
            sw.length = length;
            sw.line = lr;
            Box(go.transform, "Knot", anchor + Vector3.down * length, new Vector3(0.09f, 0.12f, 0.09f), mat).GetComponent<Renderer>().sharedMaterial = ColorMat("RopeKnot", new Color(0.12f, 0.12f, 0.12f));
        }

        internal static void Crate(Transform parent, Vector3 floorPoint, float size, Material mat, Material edge)
        {
            var c = Box(parent, "Crate", floorPoint + Vector3.up * size * 0.5f, Vector3.one * size, mat, true);
            c.gameObject.isStatic = false;
            // pale edges make it read as a crate in black and white
            Box(c, "EdgeTop", c.position + Vector3.up * size * 0.47f + Vector3.back * size * 0.51f, new Vector3(size, size * 0.06f, 0.01f), edge).localScale = new Vector3(1f, 0.06f, 0.01f);
            Box(c, "Cross", c.position + Vector3.back * size * 0.51f, new Vector3(size * 1.3f, size * 0.05f, 0.01f), edge).localScale = new Vector3(1.3f, 0.05f, 0.01f);
            c.Find("Cross").rotation = Quaternion.Euler(0f, 0f, 45f);
            c.gameObject.AddComponent<global::ProjectInk.Crate>();
        }

        // ---------------------------------------------------------------- pieces

        internal static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.isStatic = true;
            return go.transform;
        }

        internal static void WallWithOpenings(Transform parent, string name, float x0, float x1, float y0, float y1, float z, float thick,
                                     List<Rect> holes, Material mat)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var xs = new List<float> { x0, x1 };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); }
            xs.Sort();
            for (int i = 0; i < xs.Count - 1; i++)
            {
                float a = xs[i], b = xs[i + 1];
                if (b - a < 0.001f) continue;
                float mid = (a + b) * 0.5f;
                var ys = new List<(float, float)> { (y0, y1) };
                foreach (var h in holes)
                {
                    if (mid <= h.xMin || mid >= h.xMax) continue;
                    var next = new List<(float, float)>();
                    foreach (var (lo, hi) in ys)
                    {
                        if (h.yMax <= lo || h.yMin >= hi) { next.Add((lo, hi)); continue; }
                        if (h.yMin > lo) next.Add((lo, h.yMin));
                        if (h.yMax < hi) next.Add((h.yMax, hi));
                    }
                    ys = next;
                }
                foreach (var (lo, hi) in ys)
                {
                    if (hi - lo < 0.001f) continue;
                    var piece = Box(root, "Piece", new Vector3(mid, (lo + hi) * 0.5f, z), new Vector3(b - a, hi - lo, thick), mat);
                    // keep the concrete texture continuous across pieces (world-space tiling)
                    var mesh = Object.Instantiate(piece.GetComponent<MeshFilter>().sharedMesh);
                    var uv = mesh.uv;
                    var v = mesh.vertices;
                    for (int k = 0; k < v.Length; k++)
                    {
                        var w = piece.TransformPoint(v[k]);
                        uv[k] = new Vector2((w.x - x0) / (x1 - x0), (w.y - y0) / (y1 - y0));
                    }
                    mesh.uv = uv;
                    string meshPath = $"Assets/Generated/WallMesh_{name}_{i}_{lo:0.00}.asset";
                    AssetDatabase.DeleteAsset(meshPath);
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    piece.GetComponent<MeshFilter>().sharedMesh = mesh;
                }
            }
        }

        internal static void Window(Transform parent, Rect r, float wallZ, Material frame, Material glass)
        {
            var root = new GameObject("Window").transform;
            root.SetParent(parent, false);
            FrameAround(root, r, wallZ - 0.17f, 0.1f, frame);
            // mullions: a cross of bars
            Box(root, "MullionV", new Vector3(r.center.x, r.center.y, wallZ - 0.15f), new Vector3(0.05f, r.height, 0.04f), frame);
            Box(root, "MullionH", new Vector3(r.center.x, r.center.y + r.height * 0.12f, wallZ - 0.15f), new Vector3(r.width, 0.05f, 0.04f), frame);
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.name = "Glass";
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(r.center.x, r.center.y, wallZ);
            g.transform.localScale = new Vector3(r.width, r.height, 1f);
            g.GetComponent<Renderer>().sharedMaterial = glass;
            // a sill
            Box(root, "Sill", new Vector3(r.center.x, r.yMin - 0.05f, wallZ - 0.22f), new Vector3(r.width + 0.3f, 0.08f, 0.14f), frame);
        }

        internal static void FrameAround(Transform parent, Rect r, float z, float t, Material mat)
        {
            Box(parent, "FrameTop", new Vector3(r.center.x, r.yMax + t * 0.5f, z), new Vector3(r.width + t * 2f, t, 0.06f), mat);
            if (r.yMin > 0.05f) Box(parent, "FrameBottom", new Vector3(r.center.x, r.yMin - t * 0.5f, z), new Vector3(r.width + t * 2f, t, 0.06f), mat);
            Box(parent, "FrameLeft", new Vector3(r.xMin - t * 0.5f, r.center.y, z), new Vector3(t, r.height, 0.06f), mat);
            Box(parent, "FrameRight", new Vector3(r.xMax + t * 0.5f, r.center.y, z), new Vector3(t, r.height, 0.06f), mat);
        }

        internal static void Pipe(Transform parent, string name, Vector3 center, float length, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            for (float x = center.x - length * 0.5f + 1f; x < center.x + length * 0.5f; x += 3f)
                Box(parent, name + "Bracket", new Vector3(x, center.y + radius + 0.08f, center.z), new Vector3(0.04f, 0.16f + radius, 0.04f), mat);
        }

        internal static void Billboard(Transform parent, string name, Vector3 pos, Vector2 size, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.position = pos;
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        internal static void Lamp(Transform parent, Vector3 mount, float cord, Material shade, Material bulb, Material cone, Material glow)
        {
            var pivot = new GameObject("Lamp").transform;
            pivot.SetParent(parent, false);
            pivot.position = mount;
            var lamp = pivot.gameObject.AddComponent<LampFlicker>();
            Box(pivot, "Cord", mount + Vector3.down * cord * 0.5f, new Vector3(0.015f, cord, 0.015f), shade);
            var shadeGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(shadeGo.GetComponent<Collider>());
            shadeGo.name = "Shade";
            shadeGo.transform.SetParent(pivot, false);
            shadeGo.transform.position = mount + Vector3.down * (cord + 0.08f);
            shadeGo.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            shadeGo.GetComponent<Renderer>().sharedMaterial = shade;
            Box(pivot, "Bulb", mount + Vector3.down * (cord + 0.18f), new Vector3(0.12f, 0.08f, 0.12f), bulb);
            var lights = new List<Renderer>();
            // the cone of light falls on the wall behind, a glow sits round the bulb
            var coneGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(coneGo.GetComponent<Collider>());
            coneGo.name = "LightCone";
            coneGo.transform.SetParent(pivot, false);
            coneGo.transform.position = mount + Vector3.down * (cord + 0.2f + 1.5f) + Vector3.forward * 0.4f;
            coneGo.transform.localScale = new Vector3(3.4f, 3.0f, 1f);
            coneGo.GetComponent<Renderer>().sharedMaterial = cone;
            lights.Add(coneGo.GetComponent<Renderer>());
            var glowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(glowGo.GetComponent<Collider>());
            glowGo.name = "Glow";
            glowGo.transform.SetParent(pivot, false);
            glowGo.transform.position = mount + Vector3.down * (cord + 0.18f) + Vector3.back * 0.05f;
            glowGo.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            glowGo.GetComponent<Renderer>().sharedMaterial = glow;
            lights.Add(glowGo.GetComponent<Renderer>());
            lamp.lights = lights.ToArray();
        }

        internal static void Haze(Transform parent, string name, float y, Layout L, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, y, L.wallZ - 0.5f);
            var mf = go.AddComponent<MeshFilter>();
            var mesh = new Mesh { name = name };
            float w = L.halfW * 2f, h = 1.3f;
            mesh.vertices = new[] { new Vector3(-w / 2, 0, 0), new Vector3(w / 2, 0, 0), new Vector3(-w / 2, h, 0), new Vector3(w / 2, h, 0) };
            var low = new Color(0.85f, 0.85f, 0.85f, 0.10f);
            var high = new Color(0.85f, 0.85f, 0.85f, 0f);
            mesh.colors = new[] { low, low, high, high };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            string path = $"Assets/Generated/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            mf.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        internal static void Dust(Transform parent, Layout L, Texture2D glow)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, L.roofY * 0.5f, 1.0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 22f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(L.halfW * 2f, L.roofY, 1.6f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = TexMat("DustMote", "Legacy Shaders/Particles/Additive", glow, Vector2.one, new Color(0.45f, 0.45f, 0.45f, 0.5f));
            r.sharedMaterial = mat;
        }

        // ---------------------------------------------------------------- textures

        internal static Texture2D SaveTex(string name, Texture2D tex, TextureWrapMode wrap = TextureWrapMode.Repeat)
        {
            string path = $"{TexDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = wrap;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        internal static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        /// <summary>Tileable value noise (period in cells).</summary>
        internal static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            int m(int v) => ((v % period) + period) % period;
            float a = Hash(m(x0), m(y0), seed), b = Hash(m(x0 + 1), m(y0), seed);
            float c = Hash(m(x0), m(y0 + 1), seed), d = Hash(m(x0 + 1), m(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        internal static float Fbm(float u, float v, int seed)
        {
            float sum = 0f, amp = 0.5f;
            int period = 4;
            for (int o = 0; o < 5; o++)
            {
                sum += amp * Noise(u * period, v * period, period, seed + o * 17);
                amp *= 0.5f;
                period *= 2;
            }
            return sum;
        }

        /// <summary>Architectural concrete: soft mottling, formwork panel seams, tie holes,
        /// drip stains running down, darker grime near the floor line.</summary>
        internal static Texture2D ConcreteTexture(int size, Color baseColor, int seed)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    float n = Fbm(u, v, seed);
                    float k = 0.86f + (n - 0.5f) * 0.32f;
                    // drips: vertical streaks, stronger towards the top of each panel
                    float drip = Noise(u * 48f, v * 2f, 48, seed + 99);
                    k -= Mathf.Max(0f, drip - 0.72f) * 0.5f * v;
                    // panel seams every half texture, with a lit edge
                    float sx = Mathf.Repeat(u * 2f, 1f), sy = Mathf.Repeat(v * 2f, 1f);
                    if (sx < 0.006f || sy < 0.006f) k *= 0.72f;
                    else if (sx < 0.012f || sy < 0.012f) k *= 1.06f;
                    // tie holes: a regular grid inside each panel
                    float hx = Mathf.Repeat(u * 6f, 1f) - 0.5f, hy = Mathf.Repeat(v * 6f, 1f) - 0.5f;
                    float hole = Mathf.Sqrt(hx * hx + hy * hy);
                    if (hole < 0.045f) k *= 0.62f;
                    else if (hole < 0.06f) k *= 0.9f;
                    // grime at the bottom
                    k *= Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(v * 4f));
                    px[y * size + x] = new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, 1f);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A night city seen through the windows: deep sky, moon with a halo, three
        /// layers of tower silhouettes with lit windows, haze at the horizon.</summary>
        internal static Texture2D SkylineTexture(int w, int h, int seed)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var px = new Color[w * h];
            var top = new Color(0.03f, 0.03f, 0.035f);
            var horizon = new Color(0.19f, 0.19f, 0.20f);
            Vector2 moon = new Vector2(w * 0.72f, h * 0.78f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = (float)y / h;
                    var c = Color.Lerp(horizon, top, Mathf.SmoothStep(0.15f, 0.95f, v));
                    float d = Vector2.Distance(new Vector2(x, y), moon);
                    if (d < 26f) c = Color.Lerp(c, new Color(0.96f, 0.96f, 0.95f), Mathf.Clamp01((26f - d) / 2f));
                    else c += new Color(0.24f, 0.24f, 0.24f) * Mathf.Exp(-(d - 26f) / 60f) * 0.6f;
                    if (Hash(x, y, seed) > 0.9985f && v > 0.45f) c = Color.Lerp(c, Color.white, 0.6f);   // stars
                    px[y * w + x] = c;
                }
            var rng = new System.Random(seed);
            Color[] layerColors = { new Color(0.12f, 0.12f, 0.13f), new Color(0.07f, 0.07f, 0.075f), new Color(0.035f, 0.035f, 0.04f) };
            for (int layer = 0; layer < 3; layer++)
            {
                int x = 0;
                while (x < w)
                {
                    int bw = rng.Next(18 + layer * 6, 60 + layer * 20);
                    int bh = (int)(h * (0.18f + 0.3f * (float)rng.NextDouble() + layer * 0.06f));
                    for (int yy = 0; yy < bh && yy < h; yy++)
                        for (int xx = x; xx < x + bw && xx < w; xx++)
                        {
                            var c = layerColors[layer];
                            // lit windows on a grid
                            int gx = (xx - x) % 6, gy = yy % 8;
                            if (gx > 1 && gx < 4 && gy > 2 && gy < 6 && yy < bh - 4 && Hash((xx - x) / 6 + x * 7, yy / 8, seed + layer) > 0.72f)
                                c = Color.Lerp(c, new Color(0.9f, 0.9f, 0.88f), 0.75f - layer * 0.15f);
                            px[yy * w + xx] = c;
                        }
                    if (rng.NextDouble() < 0.25)   // an antenna
                        for (int yy = bh; yy < bh + 18 && yy < h; yy++) px[yy * w + Mathf.Min(w - 1, x + bw / 2)] = layerColors[layer];
                    x += bw + rng.Next(0, 6);
                }
                // haze between layers
                for (int y = 0; y < h; y++)
                    for (int xx = 0; xx < w; xx++)
                    {
                        float hz = Mathf.Exp(-y / (h * 0.12f)) * 0.12f;
                        px[y * w + xx] = Color.Lerp(px[y * w + xx], horizon, hz);
                    }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A downward cone of light: narrow and bright at the top, wide and faint below.</summary>
        internal static Texture2D ConeTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = 1f - (float)y / h;                  // 0 at the top (the lamp), 1 at the bottom
                    float half = Mathf.Lerp(0.08f, 0.5f, v);
                    float u = Mathf.Abs((float)x / w - 0.5f);
                    float edge = Mathf.Clamp01((half - u) / (half * 0.6f));
                    float a = edge * Mathf.Lerp(1f, 0.15f, v) * Mathf.Clamp01(v * 8f);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        internal static Texture2D GlowTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
