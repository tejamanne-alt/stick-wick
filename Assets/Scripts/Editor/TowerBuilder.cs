using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectInk.EditorTools
{
    /// <summary>Builds the tower: a three-level cut-away of a black-and-white high-rise lobby.
    ///
    ///   LEVEL 03  catwalk (steel grating, a glass panel in the middle) across the atrium,
    ///             under a long skylight, a giant turning ventilation fan, sweeping searchlights
    ///   LEVEL 02  two mezzanines (offices with blinds, back doors) and the open atrium between
    ///   LEVEL 01  the lobby: tall atrium window onto the city, side doors, columns
    ///
    /// Moving between levels is physical: jump and climb at the striped ledges (hang by the
    /// hands, pull up), drop through a floor (swipe down), fall or be thrown off an edge,
    /// swing across the atrium on the ropes under the catwalk, giant-swing on the bars, and
    /// smash through the glass. The platform data comes from TowerPlan, the same table the
    /// game's route-finding uses.</summary>
    public static class TowerBuilder
    {
        const float LaneFront = -0.6f, WallZ = 2.4f;
        const float SlabThick = 0.24f, CatThick = 0.12f;

        public static void Build(Transform room, Material ground, Material arch, Material trail,
                                 Transform spawnRoot, Transform[] points, out Vector3 heroStart)
        {
            float W = TowerPlan.HalfW, roof = TowerPlan.RoofY, mezz = TowerPlan.MezzY, cat = TowerPlan.CatY;
            var L = new EnvironmentBuilder.Layout { halfW = W, floorY = mezz, roofY = roof, wallZ = WallZ, laneFront = LaneFront };

            // ------------------------------------------------------------ materials
            var concrete = EnvironmentBuilder.SaveTex("TowerConcrete", EnvironmentBuilder.ConcreteTexture(512, new Color(0.70f, 0.70f, 0.68f), 41));
            var concreteDark = EnvironmentBuilder.SaveTex("TowerConcreteDark", EnvironmentBuilder.ConcreteTexture(512, new Color(0.24f, 0.24f, 0.25f), 43));
            var sky = EnvironmentBuilder.SaveTex("TowerSkyline", EnvironmentBuilder.SkylineTexture(1024, 512, 9), TextureWrapMode.Clamp);
            var coneTex = EnvironmentBuilder.SaveTex("TowerCone", EnvironmentBuilder.ConeTexture(128, 256), TextureWrapMode.Clamp);
            var glowTex = EnvironmentBuilder.SaveTex("TowerGlow", EnvironmentBuilder.GlowTexture(128), TextureWrapMode.Clamp);
            var hazardTex = EnvironmentBuilder.SaveTex("TowerHazard", HazardTexture(128));
            var gratingTex = EnvironmentBuilder.SaveTex("TowerGrating", GratingTexture(128));
            var blindsTex = EnvironmentBuilder.SaveTex("TowerBlinds", BlindsTexture(64));

            var wallMat = EnvironmentBuilder.TexMat("TowerWall", "Unlit/Texture", concrete, new Vector2(W * 2f / 4.4f, roof / 4.4f));
            var slabMat = EnvironmentBuilder.TexMat("TowerSlab", "Unlit/Texture", concreteDark, new Vector2(4f, 0.25f));
            var columnMat = EnvironmentBuilder.TexMat("TowerColumn", "Unlit/Texture", concreteDark, new Vector2(0.3f, 2f));
            var skyMat = EnvironmentBuilder.TexMat("TowerSky", "Unlit/Texture", sky, Vector2.one);
            var hazardMat = EnvironmentBuilder.TexMat("TowerHazardStripe", "Unlit/Texture", hazardTex, new Vector2(3f, 1f));
            var gratingMat = EnvironmentBuilder.TexMat("TowerGratingFace", "Sprites/Default", gratingTex, new Vector2(30f, 1f), Color.white);
            var blindsMat = EnvironmentBuilder.TexMat("TowerBlindsMat", "Unlit/Texture", blindsTex, new Vector2(1f, 6f));
            var glassMat = EnvironmentBuilder.TexMat("TowerGlass", "Sprites/Default", null, Vector2.one, new Color(0.78f, 0.80f, 0.84f, 0.12f));
            var floorGlass = EnvironmentBuilder.TexMat("TowerFloorGlass", "Sprites/Default", null, Vector2.one, new Color(0.85f, 0.88f, 0.95f, 0.38f));
            var coneMat = EnvironmentBuilder.TexMat("TowerLampCone", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.42f, 0.42f, 0.42f, 0.5f));
            var lampGlow = EnvironmentBuilder.TexMat("TowerLampGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.55f, 0.55f, 0.55f, 0.5f));
            var searchMat = EnvironmentBuilder.TexMat("TowerSearchlight", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.22f, 0.22f, 0.24f, 0.5f));
            var moonMat = EnvironmentBuilder.TexMat("TowerMoonShaft", "Legacy Shaders/Particles/Additive", coneTex, Vector2.one, new Color(0.14f, 0.14f, 0.15f, 0.5f));
            var fanGlowMat = EnvironmentBuilder.TexMat("TowerFanGlow", "Legacy Shaders/Particles/Additive", glowTex, Vector2.one, new Color(0.6f, 0.6f, 0.62f, 0.5f));
            var hazeMat = EnvironmentBuilder.TexMat("TowerHaze", "Sprites/Default", null, Vector2.one, Color.white);
            var steel = PrototypeBuilder.Mat("TowerSteel", "Unlit/Color", new Color(0.07f, 0.07f, 0.08f));
            var steelMid = PrototypeBuilder.Mat("TowerSteelMid", "Unlit/Color", new Color(0.16f, 0.16f, 0.17f));
            var edgeWhite = PrototypeBuilder.Mat("TowerEdge", "Unlit/Color", new Color(0.88f, 0.88f, 0.88f));
            var frame = PrototypeBuilder.Mat("TowerFrame", "Unlit/Color", new Color(0.06f, 0.06f, 0.06f));
            var corridor = PrototypeBuilder.Mat("TowerCorridor", "Unlit/Color", new Color(0.62f, 0.62f, 0.6f));   // lit, so fighters read in doorways
            var shade = PrototypeBuilder.Mat("TowerLampShade", "Unlit/Color", new Color(0.05f, 0.05f, 0.05f));
            var bulb = PrototypeBuilder.Mat("TowerBulb", "Unlit/Color", Color.white);
            var sign = PrototypeBuilder.Mat("TowerSign", "Unlit/Color", new Color(0.93f, 0.93f, 0.93f));

            var env = new GameObject("Tower").transform;
            env.SetParent(room, false);

            // ------------------------------------------------------------ shell
            PrototypeBuilder.Box(room, "Ground", new Vector3(0, -2f, 0.5f), new Vector3(60, 4, 9), ground);
            NoCollider(PrototypeBuilder.Box(env, "Roof", new Vector3(0, roof + 0.15f, (LaneFront + WallZ) * 0.5f), new Vector3(W * 2f + 0.4f, 0.3f, WallZ - LaneFront), ground));
            PrototypeBuilder.Box(env, "WallLeft", new Vector3(-W, roof * 0.5f, (LaneFront + WallZ) * 0.5f), new Vector3(0.4f, roof, WallZ - LaneFront), ground);
            PrototypeBuilder.Box(env, "WallRight", new Vector3(W, roof * 0.5f, (LaneFront + WallZ) * 0.5f), new Vector3(0.4f, roof, WallZ - LaneFront), ground);
            NoCollider(PrototypeBuilder.Box(env, "LobbyEdge", new Vector3(0f, -0.02f, LaneFront - 0.01f), new Vector3(W * 2f, 0.04f, 0.02f), edgeWhite));

            // ------------------------------------------------------------ back wall and its openings
            float mL = TowerPlan.MezzInnerL, mR = TowerPlan.MezzInnerR;
            var windows = new List<Rect>
            {
                new Rect(-11.6f, 1.75f, 2.4f, 0.9f), new Rect(7.2f, 1.75f, 2.4f, 0.9f),           // lobby clerestories (under the mezzanines)
                new Rect(mL + 0.5f, 0.6f, (mR - mL) - 1.0f, 4.9f),                                  // the tall atrium window
                new Rect(-11.2f, 7.75f, 9.8f, 1.25f), new Rect(1.4f, 7.75f, 9.8f, 1.25f),           // the skylight band (above heads on the catwalk)
            };
            var offices = new List<Rect> { new Rect(-8.4f, 3.75f, 3.4f, 1.3f), new Rect(8.4f, 3.75f, 3.4f, 1.3f) };
            var doors = new List<Rect> { new Rect(-10.8f, mezz, 1.6f, 2.3f), new Rect(5.4f, mezz, 1.6f, 2.3f) };
            var fanHole = new Rect(-1.15f, 7.0f, 2.3f, 2.0f);
            var openings = new List<Rect>(windows);
            openings.AddRange(offices);
            openings.AddRange(doors);
            openings.Add(fanHole);
            EnvironmentBuilder.WallWithOpenings(env, "BackWall", -W, W, 0f, roof, WallZ, 0.3f, openings, wallMat);
            foreach (var r in windows) EnvironmentBuilder.Window(env, r, WallZ, frame, glassMat);
            foreach (var r in offices)
            {
                // lit offices behind venetian blinds
                Quad(env, "Blinds", new Vector3(r.center.x, r.center.y, WallZ + 0.2f), new Vector2(r.width, r.height), blindsMat);
                EnvironmentBuilder.FrameAround(env, r, WallZ - 0.17f, 0.1f, frame);
            }
            foreach (var r in doors)
            {
                NoCollider(PrototypeBuilder.Box(env, "Corridor", new Vector3(r.center.x, r.center.y, WallZ + 0.25f), new Vector3(r.width, r.height, 0.05f), corridor));
                EnvironmentBuilder.FrameAround(env, r, WallZ - 0.17f, 0.12f, frame);
            }
            // mullions across the atrium window: a tall grid
            var aw = windows[2];
            for (int i = 1; i < 5; i++)
                NoCollider(PrototypeBuilder.Box(env, "Mullion", new Vector3(aw.x + aw.width * i / 5f, aw.center.y, WallZ - 0.12f), new Vector3(0.05f, aw.height, 0.04f), frame));
            for (int j = 1; j < 4; j++)
                NoCollider(PrototypeBuilder.Box(env, "Transom", new Vector3(aw.center.x, aw.y + aw.height * j / 4f, WallZ - 0.12f), new Vector3(aw.width, 0.04f, 0.04f), frame));
            // the skyline behind it all
            Quad(env, "Skyline", new Vector3(0f, 5.5f, 14f), new Vector2(70f, 34f), skyMat);
            // the atrium window is BACKLIT: pale moonlit fog with the city in soft grey, so black
            // fighters in front of it read as crisp silhouettes (dark glass would swallow them)
            var fogTex = EnvironmentBuilder.SaveTex("TowerFogCity", FogCityTexture(256, 512), TextureWrapMode.Clamp);
            var fogMat = EnvironmentBuilder.TexMat("TowerFogCityMat", "Unlit/Texture", fogTex, Vector2.one);
            Quad(env, "AtriumBacklight", new Vector3(aw.center.x, aw.center.y, WallZ + 0.3f), new Vector2(aw.width + 0.2f, aw.height + 0.2f), fogMat);

            // ------------------------------------------------------------ the giant fan in the top wall
            var fan = new GameObject("VentFan").transform;
            fan.SetParent(env, false);
            fan.position = new Vector3(fanHole.center.x, fanHole.center.y, WallZ + 0.1f);
            Quad(env, "FanGlow", new Vector3(fanHole.center.x, fanHole.center.y, WallZ + 0.35f), new Vector2(3.6f, 3.6f), fanGlowMat);
            for (int b = 0; b < 4; b++)
            {
                var blade = Quad(fan, "Blade", fan.position, new Vector2(0.34f, 1.8f), frame);
                blade.localRotation = Quaternion.Euler(0f, 0f, b * 45f);
                blade.localPosition = Vector3.zero;
            }
            NoCollider(PrototypeBuilder.Box(fan, "Hub", fan.position + Vector3.back * 0.02f, new Vector3(0.28f, 0.28f, 0.05f), steelMid));
            fan.gameObject.AddComponent<Spin>().degreesPerSecond = 55f;
            EnvironmentBuilder.FrameAround(env, fanHole, WallZ - 0.17f, 0.14f, frame);

            // ------------------------------------------------------------ columns (behind the fight lane)
            foreach (float x in new[] { -12.6f, mL - 0.4f, -7.9f, mR + 0.4f, 7.9f, 12.6f })
            {
                float top = Mathf.Abs(x) > 7.6f && Mathf.Abs(x) < 8.2f ? roof : (x < mL || x > mR) ? roof : mezz;
                IBeam(env, x, 0f, top, WallZ - 0.45f, steel, steelMid);
            }

            // ------------------------------------------------------------ level 02: the two mezzanines
            var plats = TowerPlan.Platforms;
            for (int i = 1; i <= 2; i++)
            {
                var p = plats[i];
                Slab(env, $"Mezzanine{i}", p.x0, p.x1, p.y, SlabThick, LaneFront, WallZ, slabMat, edgeWhite, frame);
                // a handrail along the back
                Rail(env, p.x0 + 0.3f, p.x1 - 0.3f, p.y, 2.05f, steel);
            }

            // ------------------------------------------------------------ level 03: the catwalk and its glass
            float cz0 = -0.5f, cz1 = 1.25f;
            for (int i = 3; i <= 5; i++)
            {
                var p = plats[i];
                if (!p.glass)
                {
                    var t = PrototypeBuilder.Box(env, $"Catwalk{i}", new Vector3((p.x0 + p.x1) * 0.5f, p.y - CatThick * 0.5f, (cz0 + cz1) * 0.5f), new Vector3(p.x1 - p.x0, CatThick, cz1 - cz0), steelMid);
                    Quad(env, "Grating", new Vector3((p.x0 + p.x1) * 0.5f, p.y - CatThick * 0.5f, cz0 - 0.011f), new Vector2(p.x1 - p.x0, CatThick + 0.02f), gratingMat);
                }
                else
                {
                    var g = PrototypeBuilder.Box(env, "GlassPanel", new Vector3(0f, p.y - 0.03f, (cz0 + cz1) * 0.5f), new Vector3(p.x1 - p.x0, 0.06f, cz1 - cz0), floorGlass);
                    var gp = g.gameObject.AddComponent<GlassPanel>();
                    gp.platform = i;
                    gp.x0 = p.x0; gp.x1 = p.x1; gp.y = p.y;
                    var glint = PrototypeBuilder.Box(g, "Glint", new Vector3(0f, p.y + 0.005f, cz0 - 0.005f), new Vector3(p.x1 - p.x0, 0.012f, 0.012f), edgeWhite);
                    NoCollider(glint);
                    glint.SetParent(g, true);
                    gp.visuals = g.GetComponentsInChildren<Renderer>();
                }
            }
            // the truss under it: top and bottom chords and diagonals, end to end
            float c0 = -TowerPlan.CatEnd, c1 = TowerPlan.CatEnd, topC = cat - CatThick, botC = cat - 0.62f;
            Bar(env, new Vector3(c0, topC, cz0 + 0.02f), new Vector3(c1, topC, cz0 + 0.02f), 0.05f, steel);
            Bar(env, new Vector3(c0, botC, cz0 + 0.02f), new Vector3(c1, botC, cz0 + 0.02f), 0.05f, steel);
            int bays = 12;
            for (int k = 0; k < bays; k++)
            {
                float a = Mathf.Lerp(c0, c1, k / (float)bays), b = Mathf.Lerp(c0, c1, (k + 1f) / bays);
                Bar(env, new Vector3(a, k % 2 == 0 ? botC : topC, cz0 + 0.02f), new Vector3(b, k % 2 == 0 ? topC : botC, cz0 + 0.02f), 0.03f, steel);
                Bar(env, new Vector3(a, botC, cz0 + 0.02f), new Vector3(a, topC, cz0 + 0.02f), 0.025f, steel);
            }
            NoCollider(PrototypeBuilder.Box(env, "CatwalkLip", new Vector3(0f, cat - 0.01f, cz0 - 0.012f), new Vector3(c1 - c0, 0.025f, 0.01f), edgeWhite));
            Rail(env, c0 + 0.2f, c1 - 0.2f, cat, cz1 - 0.05f, steel);
            // hangers: the catwalk hangs from the roof on rods behind it
            foreach (float x in new[] { -7.2f, -3.6f, 3.6f, 7.2f })
                NoCollider(PrototypeBuilder.Box(env, "Hanger", new Vector3(x, (cat + roof) * 0.5f, cz1 + 0.05f), new Vector3(0.03f, roof - cat, 0.03f), steel));

            // ------------------------------------------------------------ the stairs (right side)
            foreach (var st in TowerPlan.Stairs) SteelStairs(env, st, steel, steelMid, edgeWhite);

            // ------------------------------------------------------------ the ledges: hazard stripes say "climb here"
            foreach (var lg in TowerPlan.Ledges)
            {
                float len = 0.9f;
                float x = lg.x + lg.dir * len * 0.5f;
                float thick = lg.y == cat ? CatThick : SlabThick;
                float z = lg.y == cat ? cz0 - 0.013f : LaneFront - 0.013f;
                Quad(env, "LedgeStripes", new Vector3(x, lg.y - thick * 0.5f, z), new Vector2(len, thick + 0.01f), hazardMat);
            }

            // ------------------------------------------------------------ the things you use
            var ropeMat = EnvironmentBuilder.TexMat("TowerRope", "Sprites/Default", null, Vector2.one, Color.white);
            var crateMat = PrototypeBuilder.Mat("TowerCrate", "Unlit/Color", new Color(0.24f, 0.24f, 0.24f));
            var crateEdge = PrototypeBuilder.Mat("TowerCrateEdge", "Unlit/Color", new Color(0.52f, 0.52f, 0.52f));
            var rim = PrototypeBuilder.Mat("TowerSteelRim", "Unlit/Color", new Color(0.7f, 0.7f, 0.7f));
            var props = new GameObject("Props").transform;
            props.SetParent(env, false);
            // ropes hang from the truss into the atrium: swing from one mezzanine to the other
            EnvironmentBuilder.Rope(props, new Vector3(mL + 1.4f, botC, 0f), 2.3f, ropeMat);
            EnvironmentBuilder.Rope(props, new Vector3(mR - 1.4f, botC, 0f), 2.3f, ropeMat);
            // bars: under each mezzanine, above each mezzanine, and high over the catwalk
            EnvironmentBuilder.Bar(props, new Vector3(-9.2f, 1.4f, 0f), mezz - SlabThick, steel, rim);
            EnvironmentBuilder.Bar(props, new Vector3(9.8f, 1.4f, 0f), mezz - SlabThick, steel, rim);
            EnvironmentBuilder.Bar(props, new Vector3(-10.6f, mezz + 1.5f, 0f), roof, steel, rim);
            EnvironmentBuilder.Bar(props, new Vector3(10.8f, mezz + 1.5f, 0f), roof, steel, rim);
            EnvironmentBuilder.Bar(props, new Vector3(-4.6f, cat + 1.5f, 0f), roof, steel, rim);
            // crates to kick
            EnvironmentBuilder.Crate(props, new Vector3(-6.4f, 0f, 0f), 0.7f, crateMat, crateEdge);
            EnvironmentBuilder.Crate(props, new Vector3(6.2f, 0f, 0f), 0.7f, crateMat, crateEdge);
            EnvironmentBuilder.Crate(props, new Vector3(-12.0f, mezz, 0f), 0.7f, crateMat, crateEdge);
            EnvironmentBuilder.Crate(props, new Vector3(12.4f, mezz, 0f), 0.7f, crateMat, crateEdge);

            // ------------------------------------------------------------ light
            EnvironmentBuilder.Lamp(env, new Vector3(-8.0f, mezz - SlabThick, 1.6f), 0.35f, shade, bulb, coneMat, lampGlow);
            EnvironmentBuilder.Lamp(env, new Vector3(8.6f, mezz - SlabThick, 1.6f), 0.35f, shade, bulb, coneMat, lampGlow);
            EnvironmentBuilder.Lamp(env, new Vector3(-0.6f, botC, 1.6f), 1.6f, shade, bulb, coneMat, lampGlow);
            EnvironmentBuilder.Lamp(env, new Vector3(-10.0f, roof, 1.6f), 2.6f, shade, bulb, coneMat, lampGlow);
            EnvironmentBuilder.Lamp(env, new Vector3(10.4f, roof, 1.6f), 2.6f, shade, bulb, coneMat, lampGlow);
            // searchlights from outside, sweeping through the skylight
            foreach (var (x, ph) in new[] { (-6.0f, 0f), (6.5f, 0.45f) })
            {
                var pivot = new GameObject("Searchlight").transform;
                pivot.SetParent(env, false);
                pivot.position = new Vector3(x, roof - 0.3f, WallZ - 0.7f);
                var beam = Quad(pivot, "Beam", pivot.position + Vector3.down * 4.6f, new Vector2(2.2f, 9.5f), searchMat);
                beam.localPosition = Vector3.down * 4.6f;
                pivot.localRotation = Quaternion.Euler(0f, 0f, x < 0 ? -14f : 14f);
                var sw = pivot.gameObject.AddComponent<Sweep>();
                sw.amplitude = 16f; sw.period = 11f; sw.phase = ph;
            }
            // moonlight through the atrium window
            for (int i = 0; i < 3; i++)
            {
                var shaft = Quad(env, "MoonShaft", new Vector3(mL + 1.6f + i * 1.5f, 2.6f, WallZ - 0.7f), new Vector2(1.3f, 7f), moonMat);
                shaft.rotation = Quaternion.Euler(0f, 0f, 22f);
            }
            // exit signs over the side doors
            foreach (float x in new[] { -W + 0.9f, W - 0.9f })
            {
                NoCollider(PrototypeBuilder.Box(env, "ExitSign", new Vector3(x, 2.55f, WallZ - 0.2f), new Vector3(0.6f, 0.2f, 0.05f), sign));
                Quad(env, "ExitGlow", new Vector3(x, 2.55f, WallZ - 0.26f), new Vector2(1.5f, 0.9f), lampGlow);
            }
            PrototypeBuilder.DoorFrame(env, "DoorLeft", new Vector3(-12.4f, 0f, 0.9f), arch, 1.8f);
            PrototypeBuilder.DoorFrame(env, "DoorRight", new Vector3(12.4f, 0f, 0.9f), arch, 1.8f);

            // big stencilled level numbers on the back wall
            FloorNumber(env, "01", new Vector3(-12.75f, 0.3f, WallZ - 0.16f));
            FloorNumber(env, "02", new Vector3(-12.75f, mezz + 2.5f, WallZ - 0.16f));
            FloorNumber(env, "03", new Vector3(11.5f, cat + 0.35f, WallZ - 0.16f));

            // haze on every level and dust in the light
            EnvironmentBuilder.Haze(env, "TowerHazeLobby", 0f, L, hazeMat);
            EnvironmentBuilder.Haze(env, "TowerHazeMezz", mezz, L, hazeMat);
            EnvironmentBuilder.Haze(env, "TowerHazeTop", cat, L, hazeMat);
            EnvironmentBuilder.Dust(env, L, glowTex);

            // ------------------------------------------------------------ spawn points and the hero
            points[(int)SpawnKind.LeftDoor] = PrototypeBuilder.Point(spawnRoot, "LeftDoor", new Vector3(-12.2f, 0f, 0f));
            points[(int)SpawnKind.RightDoor] = PrototypeBuilder.Point(spawnRoot, "RightDoor", new Vector3(12.2f, 0f, 0f));
            points[(int)SpawnKind.UpperDoor] = PrototypeBuilder.Point(spawnRoot, "MezzDoorLeft", new Vector3(doors[0].center.x, mezz, WallZ - 0.4f));
            points[(int)SpawnKind.Background] = PrototypeBuilder.Point(spawnRoot, "MezzDoorRight", new Vector3(doors[1].center.x, mezz, WallZ - 0.4f));
            points[(int)SpawnKind.Ceiling] = PrototypeBuilder.Point(spawnRoot, "Ceiling", new Vector3(0f, roof - 0.6f, 0f));
            heroStart = new Vector3(0.3f, 0f, 0f);     // under the glass: the opening crashes him through it
        }

        // ---------------------------------------------------------------- pieces

        static Transform NoCollider(Transform t)
        {
            var c = t.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            return t;
        }

        static Transform Quad(Transform parent, string name, Vector3 pos, Vector2 size, Material mat)
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
            r.receiveShadows = false;
            return q.transform;
        }

        /// <summary>A floor slab: concrete body (its top is the walkable floor), a bright lip on
        /// the front edge, a dark beam under it.</summary>
        static void Slab(Transform parent, string name, float x0, float x1, float top, float thick, float front, float back, Material body, Material lip, Material beam)
        {
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            PrototypeBuilder.Box(parent, name, new Vector3(cx, top - thick * 0.5f, (front + back) * 0.5f), new Vector3(w, thick, back - front), body);
            NoCollider(PrototypeBuilder.Box(parent, name + "Lip", new Vector3(cx, top - 0.015f, front - 0.012f), new Vector3(w, 0.03f, 0.01f), lip));
            NoCollider(PrototypeBuilder.Box(parent, name + "Beam", new Vector3(cx, top - thick - 0.12f, front + 0.25f), new Vector3(w, 0.24f, 0.18f), beam));
        }

        /// <summary>An open steel staircase from (x0, y0) to (x1, y1): a thin walkable slope
        /// (one-way: you can walk underneath it), open treads with white nosings, a stringer on
        /// each side and a handrail along the back.</summary>
        static void SteelStairs(Transform parent, Stair st, Material steel, Material tread, Material nosing)
        {
            var root = new GameObject("Stairs").transform;
            root.SetParent(parent, false);
            float dx = st.x1 - st.x0, dy = st.y1 - st.y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            const float z0 = -0.5f, z1 = 1.3f;
            // the walkable slope: a thin collider along the line of the tread nosings
            var mid = new Vector3((st.x0 + st.x1) * 0.5f, (st.y0 + st.y1) * 0.5f, (z0 + z1) * 0.5f);
            var dir = new Vector3(dx, dy, 0f) / len;
            var normal = new Vector3(-dir.y, dir.x, 0f);
            if (normal.y < 0f) normal = -normal;
            var ramp = new GameObject("StairSlope").transform;
            ramp.SetParent(root, false);
            ramp.position = mid - normal * 0.05f;
            ramp.rotation = Quaternion.Euler(0f, 0f, ang);
            var col = ramp.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(len, 0.1f, z1 - z0);
            ramp.gameObject.isStatic = true;
            // treads
            int steps = Mathf.Max(6, Mathf.RoundToInt(Mathf.Abs(dy) / 0.25f));
            float rise = dy / steps, run = dx / steps;
            for (int i = 0; i < steps; i++)
            {
                float x = st.x0 + run * (i + 0.5f), y = st.y0 + rise * (i + 0.5f);
                NoCollider(PrototypeBuilder.Box(root, "Tread", new Vector3(x, y - 0.03f, (z0 + z1) * 0.5f), new Vector3(Mathf.Abs(run) + 0.02f, 0.06f, z1 - z0), tread));
                NoCollider(PrototypeBuilder.Box(root, "TreadNosing", new Vector3(x, y - 0.01f, z0 - 0.012f), new Vector3(Mathf.Abs(run) + 0.02f, 0.025f, 0.01f), nosing));
            }
            // stringers, front and back, and the handrail
            foreach (float z in new[] { z0 - 0.03f, z1 + 0.03f })
            {
                var s1 = NoCollider(PrototypeBuilder.Box(root, "Stringer", mid + Vector3.down * 0.16f + new Vector3(0f, 0f, z - mid.z), new Vector3(len + 0.1f, 0.22f, 0.05f), steel));
                s1.rotation = Quaternion.Euler(0f, 0f, ang);
            }
            var rail = NoCollider(PrototypeBuilder.Box(root, "StairRail", mid + Vector3.up * 0.95f + new Vector3(0f, 0f, z1 + 0.05f - mid.z), new Vector3(len, 0.04f, 0.04f), steel));
            rail.rotation = Quaternion.Euler(0f, 0f, ang);
            for (int k = 0; k <= 4; k++)
            {
                var p = new Vector3(Mathf.Lerp(st.x0, st.x1, k / 4f), Mathf.Lerp(st.y0, st.y1, k / 4f), z1 + 0.05f);
                NoCollider(PrototypeBuilder.Box(root, "StairPost", p + Vector3.up * 0.475f, new Vector3(0.03f, 0.95f, 0.03f), steel));
            }
        }

        /// <summary>A slim handrail on posts along a floor, at depth z (behind the fighters).</summary>
        static void Rail(Transform parent, float x0, float x1, float floorY, float z, Material mat)
        {
            NoCollider(PrototypeBuilder.Box(parent, "Rail", new Vector3((x0 + x1) * 0.5f, floorY + 0.95f, z), new Vector3(x1 - x0, 0.04f, 0.04f), mat));
            NoCollider(PrototypeBuilder.Box(parent, "RailMid", new Vector3((x0 + x1) * 0.5f, floorY + 0.5f, z), new Vector3(x1 - x0, 0.02f, 0.02f), mat));
            int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 1.5f));
            for (int i = 0; i <= n; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)n);
                NoCollider(PrototypeBuilder.Box(parent, "RailPost", new Vector3(x, floorY + 0.475f, z), new Vector3(0.03f, 0.95f, 0.03f), mat));
            }
        }

        /// <summary>A thin steel member from a to b (no collider).</summary>
        static void Bar(Transform parent, Vector3 a, Vector3 b, float thick, Material mat)
        {
            var t = NoCollider(PrototypeBuilder.Box(parent, "Member", (a + b) * 0.5f, new Vector3((b - a).magnitude, thick, thick), mat));
            t.rotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
        }

        /// <summary>An I-beam column: web and two flanges, with a pale edge.</summary>
        static void IBeam(Transform parent, float x, float y0, float y1, float z, Material steel, Material edge)
        {
            float h = y1 - y0, cy = (y0 + y1) * 0.5f;
            NoCollider(PrototypeBuilder.Box(parent, "ColumnWeb", new Vector3(x, cy, z), new Vector3(0.12f, h, 0.3f), steel));
            NoCollider(PrototypeBuilder.Box(parent, "ColumnFlange", new Vector3(x - 0.14f, cy, z), new Vector3(0.04f, h, 0.36f), steel));
            NoCollider(PrototypeBuilder.Box(parent, "ColumnFlange", new Vector3(x + 0.14f, cy, z), new Vector3(0.04f, h, 0.36f), steel));
            NoCollider(PrototypeBuilder.Box(parent, "ColumnEdge", new Vector3(x - 0.165f, cy, z - 0.17f), new Vector3(0.012f, h, 0.012f), edge));
        }

        static void FloorNumber(Transform parent, string text, Vector3 pos)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/BebasNeue-Regular.ttf");
            if (font == null) return;
            var go = new GameObject("Level" + text);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 100;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.LowerLeft;
            tm.color = new Color(0.25f, 0.25f, 0.26f, 1f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = font.material;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- textures

        /// <summary>Diagonal black and pale stripes: marks a climbable ledge.</summary>
        static Texture2D HazardTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool light = ((x + y) / (n / 4)) % 2 == 0;
                    byte v = light ? (byte)225 : (byte)18;
                    px[y * n + x] = new Color32(v, v, v, 255);
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>Steel grating seen edge-on: dark bars with lighter gaps.</summary>
        static Texture2D GratingTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool bar = (x % 16) < 5 || y < 18 || y > n - 18;
                    byte v = bar ? (byte)28 : (byte)70;
                    px[y * n + x] = new Color32(v, v, v, (byte)(bar ? 255 : 160));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>Moonlit fog behind the atrium window: a pale gradient, a soft moon, and distant
        /// towers in light grey at the bottom (never dark enough to hide a fighter).</summary>
        static Texture2D FogCityTexture(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            var rng = new System.Random(17);
            var tops = new float[w];
            int x0 = 0;
            while (x0 < w)
            {
                int bw = rng.Next(14, 40);
                float top = (float)(0.12 + rng.NextDouble() * 0.30);
                for (int x = x0; x < Mathf.Min(w, x0 + bw); x++) tops[x] = top;
                x0 += bw + rng.Next(2, 8);
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = y / (float)(h - 1);
                    float c = Mathf.Lerp(0.70f, 0.88f, v);                                     // brighter towards the top
                    float mx = (x - w * 0.68f) / (w * 0.5f), my = (v - 0.78f) / 0.5f;          // the moon
                    float md = Mathf.Sqrt(mx * mx + my * my * 0.25f);
                    c = Mathf.Max(c, Mathf.Lerp(0.98f, c, Mathf.Clamp01((md - 0.08f) / 0.05f)));
                    if (v < tops[x]) c = Mathf.Lerp(0.60f, 0.66f, v / Mathf.Max(0.01f, tops[x]));   // distant towers, light grey
                    if (v < tops[x] && (x % 7 == 3) && ((int)(v * h) % 9 == 4)) c += 0.12f;        // a few lit windows
                    byte b = (byte)(Mathf.Clamp01(c) * 255);
                    px[y * w + x] = new Color32(b, b, (byte)Mathf.Min(255, b + 3), 255);
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>Venetian blinds lit from behind: pale slats with thin dark gaps.</summary>
        static Texture2D BlindsTexture(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool gap = (y % 16) < 3;
                    byte v = gap ? (byte)40 : (byte)(200 - (y % 16) * 3);
                    px[y * n + x] = new Color32(v, v, v, 255);
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }
    }
}
