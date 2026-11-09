using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum LayoutKind { Hall, Tower, Warehouse, Bank }

    /// <summary>A walkable surface: [x0, x1] at height y. One-way platforms (gratings, slabs)
    /// can be jumped up through and dropped down through; the ground is solid.</summary>
    public struct Platform
    {
        public float x0, x1, y;
        public bool oneWay, glass;
        public Platform(float x0, float x1, float y, bool oneWay = true, bool glass = false)
        { this.x0 = x0; this.x1 = x1; this.y = y; this.oneWay = oneWay; this.glass = glass; }
        public bool Contains(float x, float margin = 0f) => x >= x0 - margin && x <= x1 + margin;
    }

    /// <summary>A climbable edge: a fighter stands on the floor below at FootX, jumps, hangs by
    /// the hands at the edge (x, y) and pulls up onto the platform lying towards `dir`.</summary>
    public struct Ledge
    {
        public float x, y;
        public int dir;
        public Ledge(float x, float y, int dir) { this.x = x; this.y = y; this.dir = dir; }
        public float FootX => x - dir * 0.25f;
    }

    /// <summary>A flight of stairs: walk on at the bottom (x0, y0) and up to the top (x1, y1),
    /// joining two platforms.</summary>
    public struct Stair
    {
        public float x0, y0, x1, y1;
        public int bottom, top;
        public Stair(float x0, float y0, float x1, float y1, int bottom, int top)
        { this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; this.bottom = bottom; this.top = top; }
        public int UpDir => x1 > x0 ? 1 : -1;
    }

    /// <summary>The tower: ground, two mezzanines with an open atrium between them, and a high
    /// catwalk (with a glass section in the middle) across the atrium. Shared by the scene
    /// builder (geometry) and the game (navigation), so the two always agree.</summary>
    public static class TowerPlan
    {
        public const float HalfW = 13.2f, RoofY = 9.4f, MezzY = 3.0f, CatY = 6.0f;
        public const float MezzInnerL = -4.0f, MezzInnerR = 4.5f, CatEnd = 7.5f, GlassHalf = 1.2f;
        // the right side has stairs: a grand flight from the lobby to the right mezzanine (rising
        // in front of the atrium window), and a flight from the mezzanine up to the catwalk
        public const float StairLowX0 = 1.5f, StairUpX0 = 11.5f;

        public static readonly Platform[] Platforms =
        {
            new Platform(-HalfW, StairLowX0 - 0.1f, 0f, false),        // 0 lobby, left of the grand stairs (solid)
            new Platform(-HalfW, MezzInnerL, MezzY),                    // 1 left mezzanine
            new Platform(MezzInnerR, HalfW, MezzY),                     // 2 right mezzanine
            new Platform(-CatEnd, -GlassHalf, CatY),                    // 3 catwalk, left
            new Platform(-GlassHalf, GlassHalf, CatY, true, true),      // 4 the glass panel
            new Platform(GlassHalf, CatEnd, CatY),                      // 5 catwalk, right
            new Platform(StairLowX0 + 0.5f, HalfW, 0f, false),          // 6 lobby, right of and under the grand stairs
        };

        /// <summary>The left side climbs: hang from the edge and pull up.</summary>
        public static readonly Ledge[] Ledges =
        {
            new Ledge(MezzInnerL, MezzY, -1),      // lobby → left mezzanine (its inner edge)
            new Ledge(-CatEnd, CatY, +1),          // left mezzanine → catwalk (its left end)
        };

        /// <summary>The right side walks: two flights of open steel stairs.</summary>
        public static readonly Stair[] Stairs =
        {
            new Stair(StairLowX0, 0f, MezzInnerR, MezzY, 0, 2),        // lobby → right mezzanine
            new Stair(StairUpX0, MezzY, CatEnd, CatY, 2, 5),           // right mezzanine → catwalk
        };

        /// <summary>Walks that are not just "touching": from under the grand stairs you can walk
        /// out to the left side of the lobby (but walking right from there takes the stairs).</summary>
        public static readonly (int from, int to, float x)[] ExtraWalks = { (6, 0, StairLowX0 - 0.4f) };
    }

    /// <summary>The story warehouse: the store he wakes in (x -14..14) and through its inner
    /// shutter the main hall (x 14..62, tools/blender/story_hall.py), in four zones: the
    /// forklift's raised forks, two pallet racks across the lane, the mezzanine with its stairs
    /// up from the left and a ledge at its right end. Shared by the scene builder and the game.</summary>
    public static class WarehousePlan
    {
        public const float StoreHalf = 14.0f, HallX0 = 14.3f, HallX1 = 62.0f;
        public const float ForkX0 = 19.6f, ForkX1 = 21.2f, ForkY = 1.62f;
        public const float RackY = 2.2f, Rack1X0 = 27.0f, Rack1X1 = 31.0f, Rack2X0 = 33.0f, Rack2X1 = 37.0f;
        public const float MezzX0 = 40.0f, MezzX1 = 50.0f, MezzY = 3.4f, StairX0 = 37.6f;
        public const float HoistX = 32.0f, BarX = 45.0f;
        /// <summary>The fight zones along the hall: where each one's arena begins and ends.</summary>
        public static readonly float[] ZoneX = { HallX0, 26.0f, 38.0f, 50.0f, HallX1 };

        public static readonly Platform[] Platforms =
        {
            new Platform(-StoreHalf, StairX0 - 0.1f, 0f, false),     // 0 the floor: store, loading bay, racking
            new Platform(ForkX0, ForkX1, ForkY),                       // 1 the forklift's forks
            new Platform(Rack1X0, Rack1X1, RackY),                     // 2 rack 1
            new Platform(Rack2X0, Rack2X1, RackY),                     // 3 rack 2
            new Platform(MezzX0, MezzX1, MezzY),                       // 4 the mezzanine
            new Platform(StairX0 + 0.5f, HallX1, 0f, false),          // 5 the floor under the stairs and on to the crane bay
        };

        public static readonly Ledge[] Ledges =
        {
            new Ledge(ForkX0, ForkY, +1), new Ledge(ForkX1, ForkY, -1),
            new Ledge(Rack1X0, RackY, +1), new Ledge(Rack1X1, RackY, -1),
            new Ledge(Rack2X0, RackY, +1), new Ledge(Rack2X1, RackY, -1),
            new Ledge(MezzX1, MezzY, -1),                              // up onto the mezzanine from the crane bay
        };

        public static readonly Stair[] Stairs =
        {
            new Stair(StairX0, 0f, MezzX0, MezzY, 0, 4),
        };

        public static readonly (int from, int to, float x)[] ExtraWalks = { (5, 0, StairX0 - 0.4f) };
    }

    /// <summary>Route-finding across the building's levels. For every pair of platforms it
    /// knows how to get from one to the next: walk (they touch), climb (a ledge above whose
    /// foot is on this platform) or drop (through a one-way platform, landing on whatever is
    /// below). A breadth-first search gives the first step of the shortest route.</summary>
    public static class LevelNav
    {
        public enum StepKind { None, Walk, Climb, Drop, Stairs }
        public struct Step
        {
            public StepKind kind;
            public float x;          // where to go to take the step
            public int ledge;        // for Climb
            public int to;           // the platform it leads to
            public int dir;          // for Stairs: which way to keep walking once on them
        }

        static Platform[] platforms;
        static Ledge[] ledges;
        static Stair[] stairs;
        static (int from, int to, float x)[] extraWalks;
        public static IReadOnlyList<Stair> StairList => stairs;
        public static bool Active => platforms != null;
        public static IReadOnlyList<Platform> Platforms => platforms;
        public static IReadOnlyList<Ledge> Ledges => ledges;
        static readonly HashSet<int> broken = new HashSet<int>();

        public static void Init(LayoutKind kind)
        {
            broken.Clear();
            if (kind == LayoutKind.Tower)
            {
                platforms = TowerPlan.Platforms; ledges = TowerPlan.Ledges;
                stairs = TowerPlan.Stairs; extraWalks = TowerPlan.ExtraWalks;
            }
            else if (kind == LayoutKind.Warehouse)
            {
                platforms = WarehousePlan.Platforms; ledges = WarehousePlan.Ledges;
                stairs = WarehousePlan.Stairs; extraWalks = WarehousePlan.ExtraWalks;
            }
            else { platforms = null; ledges = null; stairs = null; extraWalks = null; }
        }

        public static void SetBroken(int platform, bool isBroken)
        {
            if (isBroken) broken.Add(platform); else broken.Remove(platform);
        }
        public static bool IsBroken(int platform) => broken.Contains(platform);

        /// <summary>The platform a point stands on (feet within 0.35 m of its top), or -1.</summary>
        public static int PlatformAt(Vector3 p)
        {
            if (platforms == null) return -1;
            int best = -1;
            float bestD = 0.35f;
            for (int i = 0; i < platforms.Length; i++)
            {
                if (broken.Contains(i)) continue;
                var pl = platforms[i];
                if (!pl.Contains(p.x, 0.15f)) continue;
                float d = Mathf.Abs(p.y - pl.y);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>The highest intact platform strictly below height y at x.</summary>
        public static int Below(float x, float y)
        {
            int best = -1;
            for (int i = 0; i < platforms.Length; i++)
            {
                if (broken.Contains(i)) continue;
                var pl = platforms[i];
                if (pl.y >= y - 0.1f || !pl.Contains(x)) continue;
                if (best < 0 || pl.y > platforms[best].y) best = i;
            }
            return best;
        }

        /// <summary>Platforms joined by walking (touching, same height) share a region.</summary>
        public static int Region(int platform)
        {
            if (platform < 0 || platforms == null) return platform;
            int root = platform;
            bool grew = true;
            var seen = new HashSet<int> { platform };
            while (grew)
            {
                grew = false;
                foreach (var a in new List<int>(seen))
                    for (int j = 0; j < platforms.Length; j++)
                        if (!seen.Contains(j) && Touch(a, j)) { seen.Add(j); grew = true; if (j < root) root = j; }
            }
            return root;
        }

        /// <summary>The left and right ends of the walkable region a platform belongs to.</summary>
        public static bool RegionBounds(int platform, out float x0, out float x1)
        {
            x0 = x1 = 0f;
            if (platforms == null || platform < 0 || platform >= platforms.Length) return false;
            int r = Region(platform);
            x0 = float.MaxValue; x1 = float.MinValue;
            for (int i = 0; i < platforms.Length; i++)
                if (!broken.Contains(i) && Region(i) == r) { x0 = Mathf.Min(x0, platforms[i].x0); x1 = Mathf.Max(x1, platforms[i].x1); }
            return x1 > x0;
        }

        static bool Touch(int a, int b)
        {
            if (broken.Contains(a) || broken.Contains(b)) return false;
            var A = platforms[a]; var B = platforms[b];
            return Mathf.Abs(A.y - B.y) < 0.05f && A.x1 >= B.x0 - 0.05f && B.x1 >= A.x0 - 0.05f;
        }

        /// <summary>The first step from platform `from` (fighter at x) towards platform `to` (target at tx).</summary>
        public static Step Next(int from, float x, int to, float tx)
        {
            var none = new Step { kind = StepKind.None };
            if (platforms == null || from < 0 || to < 0 || from == to) return none;
            // breadth-first over platforms
            var prev = new int[platforms.Length];
            var how = new Step[platforms.Length];
            for (int i = 0; i < prev.Length; i++) prev[i] = -2;
            var q = new Queue<int>();
            q.Enqueue(from);
            prev[from] = -1;
            while (q.Count > 0)
            {
                int a = q.Dequeue();
                if (a == to) break;
                foreach (var s in Out(a, x, tx))
                {
                    if (prev[s.to] != -2) continue;
                    prev[s.to] = a;
                    how[s.to] = s;
                    q.Enqueue(s.to);
                }
            }
            if (prev[to] == -2) return none;
            int n = to;
            while (prev[n] != from) n = prev[n];
            return how[n];
        }

        static IEnumerable<Step> Out(int a, float x, float tx)
        {
            var A = platforms[a];
            if (broken.Contains(a)) yield break;
            // walk onto a touching platform
            for (int j = 0; j < platforms.Length; j++)
                if (j != a && Touch(a, j))
                {
                    var B = platforms[j];
                    yield return new Step { kind = StepKind.Walk, to = j, x = Mathf.Clamp(tx, B.x0 + 0.2f, B.x1 - 0.2f) };
                }
            // stairs: up from their bottom platform, down from their top one
            if (stairs != null)
                foreach (var st in stairs)
                {
                    if (st.bottom == a && !broken.Contains(st.top))
                        yield return new Step { kind = StepKind.Stairs, to = st.top, x = st.x0, dir = st.UpDir };
                    if (st.top == a && !broken.Contains(st.bottom))
                        yield return new Step { kind = StepKind.Stairs, to = st.bottom, x = st.x1, dir = -st.UpDir };
                }
            if (extraWalks != null)
                foreach (var w in extraWalks)
                    if (w.from == a) yield return new Step { kind = StepKind.Walk, to = w.to, x = w.x };
            // climb: any ledge whose foot stands on this platform
            for (int k = 0; k < ledges.Length; k++)
            {
                var L = ledges[k];
                if (!A.Contains(L.FootX) || L.y - A.y < 0.8f || L.y - A.y > 3.6f) continue;
                int up = -1;
                for (int j = 0; j < platforms.Length; j++)
                    if (!broken.Contains(j) && Mathf.Abs(platforms[j].y - L.y) < 0.05f && platforms[j].Contains(L.x + L.dir * 0.3f)) up = j;
                if (up >= 0) yield return new Step { kind = StepKind.Climb, to = up, ledge = k, x = L.FootX };
            }
            // drop: through a one-way platform, or off its ends, onto whatever is below
            if (A.oneWay)
            {
                // sample along the platform: where would we land?
                var best = new Dictionary<int, float>();
                for (float sx = A.x0 + 0.3f; sx <= A.x1 - 0.3f; sx += 0.25f)
                {
                    int b = Below(sx, A.y);
                    if (b < 0) continue;
                    // choose the drop point nearest the target for each landing platform
                    if (!best.ContainsKey(b) || Mathf.Abs(sx - tx) < Mathf.Abs(best[b] - tx)) best[b] = sx;
                }
                foreach (var kv in best) yield return new Step { kind = StepKind.Drop, to = kv.Key, x = kv.Value };
            }
        }

        /// <summary>A ledge the hero can climb from where he stands: its foot on his platform,
        /// within reach horizontally. Nearest first.</summary>
        public static int LedgeFor(Vector3 p, int platform, float reach = 2.2f)
        {
            if (ledges == null || platform < 0) return -1;
            int best = -1;
            float bestD = reach;
            var A = platforms[platform];
            for (int k = 0; k < ledges.Length; k++)
            {
                var L = ledges[k];
                if (!A.Contains(L.FootX) || L.y - A.y < 0.8f || L.y - A.y > 3.6f) continue;
                float d = Mathf.Abs(L.FootX - p.x);
                if (d < bestD) { bestD = d; best = k; }
            }
            return best;
        }
    }
}
