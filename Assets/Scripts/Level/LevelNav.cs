using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum LayoutKind { Hall, Tower }

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

    /// <summary>The tower: ground, two mezzanines with an open atrium between them, and a high
    /// catwalk (with a glass section in the middle) across the atrium. Shared by the scene
    /// builder (geometry) and the game (navigation), so the two always agree.</summary>
    public static class TowerPlan
    {
        public const float HalfW = 13.2f, RoofY = 9.4f, MezzY = 3.0f, CatY = 6.0f;
        public const float MezzInnerL = -4.0f, MezzInnerR = 3.0f, CatEnd = 7.5f, GlassHalf = 1.2f;

        public static readonly Platform[] Platforms =
        {
            new Platform(-HalfW, HalfW, 0f, false),                     // 0 ground (solid)
            new Platform(-HalfW, MezzInnerL, MezzY),                    // 1 left mezzanine
            new Platform(MezzInnerR, HalfW, MezzY),                     // 2 right mezzanine
            new Platform(-CatEnd, -GlassHalf, CatY),                    // 3 catwalk, left
            new Platform(-GlassHalf, GlassHalf, CatY, true, true),      // 4 the glass panel
            new Platform(GlassHalf, CatEnd, CatY),                      // 5 catwalk, right
        };

        public static readonly Ledge[] Ledges =
        {
            new Ledge(MezzInnerL, MezzY, -1),      // ground → left mezzanine (its inner edge)
            new Ledge(MezzInnerR, MezzY, +1),      // ground → right mezzanine
            new Ledge(-CatEnd, CatY, +1),          // left mezzanine → catwalk (its left end)
            new Ledge(CatEnd, CatY, -1),           // right mezzanine → catwalk
        };
    }

    /// <summary>Route-finding across the building's levels. For every pair of platforms it
    /// knows how to get from one to the next: walk (they touch), climb (a ledge above whose
    /// foot is on this platform) or drop (through a one-way platform, landing on whatever is
    /// below). A breadth-first search gives the first step of the shortest route.</summary>
    public static class LevelNav
    {
        public enum StepKind { None, Walk, Climb, Drop }
        public struct Step
        {
            public StepKind kind;
            public float x;          // where to go to take the step
            public int ledge;        // for Climb
            public int to;           // the platform it leads to
        }

        static Platform[] platforms;
        static Ledge[] ledges;
        public static bool Active => platforms != null;
        public static IReadOnlyList<Platform> Platforms => platforms;
        public static IReadOnlyList<Ledge> Ledges => ledges;
        static readonly HashSet<int> broken = new HashSet<int>();

        public static void Init(LayoutKind kind)
        {
            broken.Clear();
            if (kind == LayoutKind.Tower) { platforms = TowerPlan.Platforms; ledges = TowerPlan.Ledges; }
            else { platforms = null; ledges = null; }
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
