using System.Collections.Generic;
using UnityEditor;
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

        public static void Build(Transform room, Material ground, Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            // ------------------------------------------------------------ materials
            var wallTex = EnvironmentBuilder.SaveTex("StoryCorrugated", CorrugatedTexture(256, new Color(0.33f, 0.36f, 0.41f), 3));
            var shutterTex = EnvironmentBuilder.SaveTex("StoryShutter", ShutterTexture(128, new Color(0.27f, 0.28f, 0.30f)));
            var skylineTex = EnvironmentBuilder.SaveTex("StorySkyline", EnvironmentBuilder.SkylineTexture(1024, 512, 17), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("StoryGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var wallMat = EnvironmentBuilder.TexMat("StoryWall", "Unlit/Texture", wallTex, new Vector2(HalfW * 2f / 3.2f, RoofY / 3.2f));
            var sideMat = EnvironmentBuilder.TexMat("StoryWallSide", "Unlit/Texture", wallTex, new Vector2(1.2f, RoofY / 3.2f));
            var shutterMat = EnvironmentBuilder.TexMat("StoryShutterMat", "Unlit/Texture", shutterTex, new Vector2(1f, 1f));
            var sprite = PrototypeBuilder.Mat("StoryVertex", "Sprites/Default", Color.white);
            var glow = EnvironmentBuilder.TexMat("StoryGlowMat", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.55f, 0.36f, 0.16f, 0.5f));
            var neonGlow = EnvironmentBuilder.TexMat("StoryNeonGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.40f, 0.16f, 0.45f, 0.5f));
            var puddleMat = EnvironmentBuilder.TexMat("StoryPuddle", "Sprites/Default", glowTex, Vector2.one, new Color(0.30f, 0.35f, 0.45f, 0.55f));
            var skyline = EnvironmentBuilder.TexMat("StorySkylineMat", "Sprites/Default", skylineTex, Vector2.one, new Color(0.10f, 0.08f, 0.14f, 1f));
            var skylineFar = EnvironmentBuilder.TexMat("StorySkylineFar", "Sprites/Default", skylineTex, new Vector2(1.6f, 1f), new Color(0.16f, 0.10f, 0.20f, 1f));
            var roofMat = PrototypeBuilder.Mat("StoryRoof", "Unlit/Color", new Color(0.10f, 0.10f, 0.11f));
            var steel = PrototypeBuilder.Mat("StorySteel", "Unlit/Color", new Color(0.06f, 0.065f, 0.075f));
            var machine = PrototypeBuilder.Mat("StoryMachine", "Unlit/Color", new Color(0.12f, 0.13f, 0.15f));
            var machineDark = PrototypeBuilder.Mat("StoryMachineDark", "Unlit/Color", new Color(0.08f, 0.085f, 0.095f));
            var rust = PrototypeBuilder.Mat("StoryRust", "Unlit/Color", new Color(0.20f, 0.10f, 0.06f));
            var floorMat = PrototypeBuilder.Mat("StoryFloor", "Unlit/Color", new Color(0.115f, 0.12f, 0.135f));
            var yardMat = PrototypeBuilder.Mat("StoryYard", "Unlit/Color", new Color(0.035f, 0.04f, 0.05f));
            var crackMat = PrototypeBuilder.Mat("StoryCrack", "Unlit/Color", new Color(0.55f, 0.65f, 0.85f));
            var moonMat = PrototypeBuilder.Mat("StoryMoonGap", "Unlit/Color", new Color(0.72f, 0.80f, 0.95f));
            var outsideMat = PrototypeBuilder.Mat("StoryOutside", "Unlit/Color", new Color(0.08f, 0.10f, 0.16f));
            var dropMat = PrototypeBuilder.Mat("StoryDroplet", "Unlit/Color", new Color(0.78f, 0.86f, 0.98f));
            Material[] containerMats =
            {
                PrototypeBuilder.Mat("StoryBoxRust", "Unlit/Color", new Color(0.20f, 0.07f, 0.06f)),
                PrototypeBuilder.Mat("StoryBoxTeal", "Unlit/Color", new Color(0.05f, 0.14f, 0.15f)),
                PrototypeBuilder.Mat("StoryBoxOchre", "Unlit/Color", new Color(0.17f, 0.12f, 0.05f)),
                PrototypeBuilder.Mat("StoryBoxGrey", "Unlit/Color", new Color(0.11f, 0.11f, 0.13f)),
                PrototypeBuilder.Mat("StoryBoxBlue", "Unlit/Color", new Color(0.06f, 0.08f, 0.16f)),
            };

            var env = new GameObject("Warehouse").transform;
            env.SetParent(room, false);

            // ------------------------------------------------------------ the floor (the only collider besides the ground)
            PrototypeBuilder.Box(room, "Ground", new Vector3(0, -2f, 10f), new Vector3(160, 4, 160), yardMat);
            VBox(env, "ShedFloor", new Vector3(0, -0.005f, 0.9f), new Vector3(HalfW * 2f, 0.01f, 3.8f), floorMat);

            // ------------------------------------------------------------ the shed
            float doorL = ShutterX - 1.6f, doorR = ShutterX + 1.6f, doorH = 3.6f;
            VBox(env, "BackWallLeft", new Vector3((-HalfW - 0.2f + doorL) * 0.5f, RoofY * 0.5f, BackZ + 0.15f), new Vector3(doorL + HalfW + 0.2f, RoofY, 0.3f), wallMat);
            VBox(env, "BackWallRight", new Vector3((doorR + HalfW + 0.2f) * 0.5f, RoofY * 0.5f, BackZ + 0.15f), new Vector3(HalfW + 0.2f - doorR, RoofY, 0.3f), sideMat);
            VBox(env, "BackWallOverDoor", new Vector3(ShutterX, (doorH + RoofY) * 0.5f, BackZ + 0.15f), new Vector3(doorR - doorL, RoofY - doorH, 0.3f), sideMat);
            VBox(env, "WallLeft", new Vector3(-HalfW - 0.2f, RoofY * 0.5f, 1.0f), new Vector3(0.4f, RoofY, 3.8f), sideMat);
            VBox(env, "WallRight", new Vector3(HalfW + 0.2f, RoofY * 0.5f, 1.0f), new Vector3(0.4f, RoofY, 3.8f), sideMat);
            VBox(env, "Roof", new Vector3(0, RoofY + 0.15f, 1.0f), new Vector3(HalfW * 2f + 0.8f, 0.3f, 4.0f), roofMat);
            // the crack: a seam of storm light under the roof over where he lies, and on the roof top
            Crack(env, "RoofCrack", HeroStartX - 0.6f, RoofY - 0.005f, -0.7f, 1.9f, 0.035f, crackMat, 5);
            Crack(env, "RoofCrackTop", HeroStartX - 0.6f, RoofY + 0.305f, -0.7f, 1.9f, 0.06f, crackMat, 5);
            // girders across the depth, two long beams, columns on the back wall
            for (float x = -HalfW + 1.2f; x < HalfW; x += 3.4f)
                VBox(env, "Girder", new Vector3(x, RoofY - 0.32f, 1.0f), new Vector3(0.16f, 0.42f, 3.9f), steel);
            VBox(env, "BeamFront", new Vector3(0, RoofY - 0.62f, 0.2f), new Vector3(HalfW * 2f, 0.2f, 0.14f), steel);
            VBox(env, "BeamBack", new Vector3(0, RoofY - 0.62f, 2.2f), new Vector3(HalfW * 2f, 0.2f, 0.14f), steel);
            for (float x = -HalfW + 3.5f; x < HalfW - 2f; x += 7f)
                if (Mathf.Abs(x - ShutterX) > 2.2f) VBox(env, "Column", new Vector3(x, RoofY * 0.5f, BackZ - 0.12f), new Vector3(0.28f, RoofY, 0.24f), steel);
            // chains hanging from the front beam, with hooks
            foreach (var cx in new[] { -3.2f, 2.6f, 7.4f })
            {
                float len = 1.6f + Mathf.Abs(cx) * 0.06f;
                VBox(env, "Chain", new Vector3(cx, RoofY - 0.72f - len * 0.5f, 0.2f), new Vector3(0.035f, len, 0.035f), steel);
                VBox(env, "Hook", new Vector3(cx + 0.06f, RoofY - 0.78f - len, 0.2f), new Vector3(0.16f, 0.06f, 0.05f), steel);
            }
            // dead machinery against the back wall, and scrap
            VBox(env, "PressBase", new Vector3(-1.4f, 0.55f, 1.95f), new Vector3(2.4f, 1.1f, 1.0f), machine);
            VBox(env, "PressColumnL", new Vector3(-2.35f, 2.0f, 1.95f), new Vector3(0.35f, 2.0f, 0.6f), machineDark);
            VBox(env, "PressColumnR", new Vector3(-0.45f, 2.0f, 1.95f), new Vector3(0.35f, 2.0f, 0.6f), machineDark);
            VBox(env, "PressHead", new Vector3(-1.4f, 3.15f, 1.95f), new Vector3(2.5f, 0.7f, 0.9f), machine);
            VBox(env, "LatheBed", new Vector3(5.2f, 0.55f, 2.0f), new Vector3(3.4f, 0.35f, 0.7f), machine);
            VBox(env, "LatheLegs", new Vector3(5.2f, 0.2f, 2.0f), new Vector3(3.0f, 0.4f, 0.5f), machineDark);
            VBox(env, "LatheHead", new Vector3(3.8f, 0.95f, 2.0f), new Vector3(0.8f, 0.6f, 0.7f), machineDark);
            VBox(env, "Bench", new Vector3(-5.6f, 0.45f, 2.1f), new Vector3(2.2f, 0.08f, 0.7f), rust);
            VBox(env, "BenchLegs", new Vector3(-5.6f, 0.21f, 2.1f), new Vector3(2.0f, 0.42f, 0.5f), machineDark);
            var rnd = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                var size = new Vector3(0.5f + (float)rnd.NextDouble() * 1.2f, 0.25f + (float)rnd.NextDouble() * 0.7f, 0.4f + (float)rnd.NextDouble() * 0.6f);
                var t = VBox(env, "Scrap", new Vector3(-12.2f + (float)rnd.NextDouble() * 2.6f, size.y * 0.5f + (float)rnd.NextDouble() * 0.5f, 1.9f + (float)rnd.NextDouble() * 0.5f), size, i % 3 == 0 ? rust : machineDark);
                t.rotation = Quaternion.Euler((float)rnd.NextDouble() * 30f - 15f, (float)rnd.NextDouble() * 60f - 30f, (float)rnd.NextDouble() * 40f - 20f);
            }
            foreach (var bx in new[] { 8.4f, 9.1f, 8.75f })
            {
                var b = Vis(GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform);
                b.name = "Barrel"; b.SetParent(env, false);
                b.position = new Vector3(bx, bx == 8.75f ? 1.32f : 0.45f, 2.15f);
                b.localScale = new Vector3(0.58f, 0.45f, 0.58f);
                b.GetComponent<Renderer>().sharedMaterial = rust;
            }

            // ------------------------------------------------------------ the shutter, and the light under it
            var shutter = VBox(env, "Shutter", new Vector3(ShutterX, doorH * 0.5f + 0.06f, BackZ + 0.14f), new Vector3(doorR - doorL, doorH - 0.12f, 0.06f), shutterMat);
            VBox(env, "ShutterHousing", new Vector3(ShutterX, doorH + 0.2f, BackZ - 0.05f), new Vector3(doorR - doorL + 0.4f, 0.4f, 0.3f), steel);
            VBox(env, "MoonGap", new Vector3(ShutterX, 0.05f, BackZ + 0.02f), new Vector3(doorR - doorL - 0.1f, 0.1f, 0.02f), moonMat);
            VBox(env, "Outside", new Vector3(ShutterX, doorH * 0.5f, BackZ + 1.2f), new Vector3(doorR - doorL + 0.6f, doorH + 0.2f, 0.05f), outsideMat);
            var moon = new Color(0.62f, 0.70f, 0.88f, 0.5f); var moon0 = new Color(0.62f, 0.70f, 0.88f, 0f);
            FloorQuad(env, "MoonSlit", new[] {
                new Vector3(doorL + 0.1f, 0.006f, BackZ), new Vector3(doorR - 0.1f, 0.006f, BackZ),
                new Vector3(doorL - 0.6f, 0.006f, -0.6f), new Vector3(doorR + 0.4f, 0.006f, -0.6f) },
                new[] { moon, moon, moon0, moon0 }, sprite);
            var flood = new Color(0.62f, 0.70f, 0.88f, 0.42f);
            var floodQuad = FloorQuad(env, "MoonFlood", new[] {
                new Vector3(doorL, 0.007f, BackZ), new Vector3(doorR, 0.007f, BackZ),
                new Vector3(doorL - 3.2f, 0.007f, -1.4f), new Vector3(doorR + 1.0f, 0.007f, -1.4f) },
                new[] { flood, flood, moon0, moon0 }, sprite);
            floodQuad.GetComponent<Renderer>().enabled = false;
            // two shadows that will cross the slit (feet outside the door)
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

            // ------------------------------------------------------------ puddles and long shadows on the floor
            Puddle(env, puddleMat, HeroStartX - 0.7f, 0.15f, 1.3f, 0.7f);
            Puddle(env, puddleMat, -3.6f, 0.4f, 1.6f, 0.6f);
            Puddle(env, puddleMat, 1.2f, -0.1f, 1.1f, 0.5f);
            Puddle(env, puddleMat, 6.5f, 0.3f, 1.8f, 0.6f);
            Puddle(env, puddleMat, 9.8f, -0.2f, 1.0f, 0.45f);
            foreach (var (x0, len) in new[] { (-1.4f, 4.0f), (5.2f, 3.4f), (-11.3f, 2.6f), (8.8f, 2.2f), (-5.6f, 2.4f) })
            {
                FloorQuad(env, "LongShadow", new[] {
                    new Vector3(x0 - 1.0f, 0.004f, 1.6f), new Vector3(x0 + 1.0f, 0.004f, 1.6f),
                    new Vector3(x0 - 1.0f - len, 0.004f, -0.9f), new Vector3(x0 + 0.6f - len, 0.004f, -0.9f) },
                    new[] { new Color(0, 0, 0, 0.5f), new Color(0, 0, 0, 0.5f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0f) }, sprite);
            }

            // ------------------------------------------------------------ the droplets (posed by the intro)
            for (int i = 0; i < 2; i++)
            {
                var d = Vis(GameObject.CreatePrimitive(PrimitiveType.Sphere).transform);
                d.name = "Droplet" + i; d.SetParent(env, false);
                d.position = new Vector3(HeroStartX - 0.6f, RoofY - 0.05f, 0f);
                d.localScale = Vector3.one * 0.05f;
                d.GetComponent<Renderer>().sharedMaterial = dropMat;
                d.GetComponent<Renderer>().enabled = false;
            }

            // ------------------------------------------------------------ the yard (seen in the opening wide shot)
            var yard = new GameObject("Yard").transform;
            yard.SetParent(room, false);
            var r2 = new System.Random(23);
            foreach (float z in new[] { 7f, 13.5f, 21f, 28.5f, 36f, -9f, -16f })
            {
                for (float x = -48f; x < 48f; x += 6.6f)
                {
                    if (r2.NextDouble() < 0.28) continue;                          // gaps: the labyrinth
                    if (z < 0f && Mathf.Abs(x) < 20f) continue;                    // keep the shed's front clear
                    int stack = 1 + r2.Next(3);
                    for (int k = 0; k < stack; k++)
                    {
                        var m = containerMats[r2.Next(containerMats.Length)];
                        VBox(yard, "Container", new Vector3(x + (float)(r2.NextDouble() - 0.5) * 0.6f, 1.3f + k * 2.6f, z + (float)(r2.NextDouble() - 0.5)),
                             new Vector3(6.0f, 2.55f, 2.44f), m, (float)(r2.NextDouble() - 0.5) * 6f);
                    }
                }
            }
            for (int i = 0; i < 14; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (17f + (float)r2.NextDouble() * 12f);
                float z = 2f + (float)r2.NextDouble() * 4f;
                float y = 0.65f + (i / 2 % 3) * 1.1f;
                var car = VBox(yard, "ScrapCar", new Vector3(x, y, z), new Vector3(4.2f, 1.1f, 1.9f), i % 3 == 0 ? rust : machineDark, (float)r2.NextDouble() * 50f - 25f);
                car.rotation *= Quaternion.Euler((float)r2.NextDouble() * 16f - 8f, 0f, (float)r2.NextDouble() * 14f - 7f);
                VBox(car, "Cabin", car.position + Vector3.up * 0.75f, new Vector3(0.5f, 0.55f, 0.85f), machineDark);
            }
            foreach (var (lx, lz) in new[] { (-24f, 10f), (20f, 16f), (-8f, 24f), (34f, 4f), (-36f, 30f) })
            {
                VBox(yard, "LampPole", new Vector3(lx, 4f, lz), new Vector3(0.18f, 8f, 0.18f), steel);
                var g = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
                g.name = "LampGlow"; g.SetParent(yard, false);
                g.position = new Vector3(lx, 8.1f, lz); g.localScale = Vector3.one * 5f;
                g.rotation = Quaternion.Euler(60f, 180f, 0f);
                g.GetComponent<Renderer>().sharedMaterial = glow;
                var pool = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
                pool.name = "LampPool"; pool.SetParent(yard, false);
                pool.position = new Vector3(lx, 0.02f, lz); pool.localScale = Vector3.one * 9f;
                pool.rotation = Quaternion.Euler(90f, 0f, 0f);
                pool.GetComponent<Renderer>().sharedMaterial = glow;
            }
            for (int i = 0; i < 8; i++)
            {
                var n = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
                n.name = "NeonPuddle"; n.SetParent(yard, false);
                n.position = new Vector3(-30f + i * 8.5f, 0.02f, 4f + (i % 3) * 7f); n.localScale = new Vector3(4f, 2.2f, 1f);
                n.rotation = Quaternion.Euler(90f, 0f, 0f);
                n.GetComponent<Renderer>().sharedMaterial = neonGlow;
            }
            // the city: two skyline layers and a sky with the haze over the horizon (all far beyond the
            // shed's front, so only the opening wide shot, looking that way, sees them)
            SkyQuad(yard, "Sky", new Vector3(0f, 40f, -230f), new Vector2(600f, 160f), sprite);
            var far = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            far.name = "SkylineFar"; far.SetParent(yard, false);
            far.position = new Vector3(0f, 26f, -200f); far.localScale = new Vector3(520f, 60f, 1f);
            far.rotation = Quaternion.Euler(0f, 180f, 0f);
            far.GetComponent<Renderer>().sharedMaterial = skylineFar;
            var near = Vis(GameObject.CreatePrimitive(PrimitiveType.Quad).transform);
            near.name = "Skyline"; near.SetParent(yard, false);
            near.position = new Vector3(20f, 16f, -150f); near.localScale = new Vector3(360f, 40f, 1f);
            near.rotation = Quaternion.Euler(0f, 180f, 0f);
            near.GetComponent<Renderer>().sharedMaterial = skyline;

            // ------------------------------------------------------------ spawn points: everyone comes through the shutter
            var door = new Vector3(ShutterX, 0f, BackZ + 0.5f);
            for (int i = 0; i < points.Length; i++)
                points[i] = PrototypeBuilder.Point(spawnRoot, ((SpawnKind)i).ToString(), door);
            heroStart = new Vector3(HeroStartX, 0f, 0f);
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
