using UnityEngine;

namespace ProjectInk
{
    /// <summary>The story's camera vocabulary: fifteen moves, each a pure function of progress u (0..1,
    /// eased here), returning where the camera is, what it looks at, its field of view and roll.
    /// A cutscene picks the move that fits the beat (docs/story/camera_language.md):
    ///
    ///   AerialOrbit       high, circling a place: establishing, isolation
    ///   ArcShot           a slow half-circle at eye level round a figure: a turn in him
    ///   DronePullAway     back and up, away from someone small: loss, aftermath
    ///   Truck             sideways, alongside him: walking, searching
    ///   Roll              the horizon tilting: the world coming apart
    ///   Pan               turning on the spot: he hears something, finds something
    ///   CraneDown         from above down to him: arriving, kneeling
    ///   DollyZoom         in while zooming out (or the reverse): the floor drops away
    ///   TiltUp            from the floor up to a face: the reveal of a man
    ///   DollyIn           slowly closer: resolve, a decision
    ///   OverheadRotation  straight down, turning: a body, a scene seen whole
    ///   DollyOut          slowly away: alone with it
    ///   PedestalUp        rising straight up on a man standing: power coming back
    ///   LowAngleArc       low, curving round a man: a threat, a boss
    ///   LateralReveal     sideways past something in the foreground: what is behind the wall</summary>
    public static class CineCam
    {
        public struct Shot
        {
            public Vector3 pos, look, up;
            public float fov, roll;
            public Shot(Vector3 p, Vector3 l, float f, float r = 0f) { pos = p; look = l; fov = f; roll = r; up = Vector3.zero; }
        }

        public static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }
        static float EaseOut(float u) { u = Mathf.Clamp01(u); return 1f - (1f - u) * (1f - u); }

        /// <summary>The point at angle a (degrees, 0 = from the camera side, -z) round c.</summary>
        static Vector3 Around(Vector3 c, float a, float r, float h)
        {
            float rad = a * Mathf.Deg2Rad;
            return c + new Vector3(Mathf.Sin(rad) * r, h, -Mathf.Cos(rad) * r);
        }

        public static Shot AerialOrbit(Vector3 c, float u, float r = 14f, float h = 9f, float a0 = -40f, float a1 = 20f, float fov = 40f)
            => new Shot(Around(c, Mathf.Lerp(a0, a1, Ease(u)), r, h), c, fov);

        public static Shot ArcShot(Vector3 subject, float u, float r = 2.6f, float a0 = -35f, float a1 = 35f, float h = 0f, float fov = 34f)
            => new Shot(Around(subject, Mathf.Lerp(a0, a1, Ease(u)), r, h), subject, fov);

        public static Shot DronePullAway(Vector3 subject, float u, float d0 = 2f, float d1 = 14f, float h0 = 1f, float h1 = 9f, float fov = 40f)
        {
            float k = EaseOut(u);
            return new Shot(subject + new Vector3(-0.3f * d1 * k, Mathf.Lerp(h0, h1, k), -Mathf.Lerp(d0, d1, k)), subject, fov);
        }

        public static Shot Truck(Vector3 from, Vector3 to, Vector3 lookOffset, float u, float fov = 38f)
        {
            var p = Vector3.Lerp(from, to, Ease(u));
            return new Shot(p, p + lookOffset, fov);
        }

        public static Shot Roll(Vector3 pos, Vector3 look, float u, float r0 = 0f, float r1 = 14f, float fov = 34f)
            => new Shot(pos, look, fov, Mathf.Lerp(r0, r1, Ease(u)));

        public static Shot Pan(Vector3 pos, Vector3 look0, Vector3 look1, float u, float fov = 38f)
            => new Shot(pos, Vector3.Lerp(look0, look1, Ease(u)), fov);

        public static Shot CraneDown(Vector3 subject, float u, float h0 = 4.5f, float h1 = 0.6f, float back = 3.2f, float fov = 38f)
            => new Shot(subject + new Vector3(-0.4f, Mathf.Lerp(h0, h1, Ease(u)), -back), subject, fov);

        /// <summary>The camera moves while the lens zooms against it, keeping the subject the same size:
        /// the background swells or falls away. width = how wide the subject is held.</summary>
        public static Shot DollyZoom(Vector3 subject, float u, float d0 = 2.2f, float d1 = 6.5f, float width = 1.4f)
        {
            float d = Mathf.Lerp(d0, d1, Ease(u));
            float fov = 2f * Mathf.Atan(width / (2f * d)) * Mathf.Rad2Deg;
            return new Shot(subject + new Vector3(0f, 0.05f, -d), subject, fov);
        }

        public static Shot TiltUp(Vector3 pos, Vector3 low, Vector3 high, float u, float fov = 34f)
            => new Shot(pos, Vector3.Lerp(low, high, Ease(u)), fov);

        public static Shot DollyIn(Vector3 subject, Vector3 dir, float u, float d0 = 4f, float d1 = 1.6f, float fov = 32f)
            => new Shot(subject - dir.normalized * Mathf.Lerp(d0, d1, Ease(u)), subject, fov);

        public static Shot DollyOut(Vector3 subject, Vector3 dir, float u, float d0 = 1.6f, float d1 = 5f, float fov = 32f)
            => new Shot(subject - dir.normalized * Mathf.Lerp(d0, d1, Ease(u)), subject, fov);

        /// <summary>Straight down on a point, turning slowly about it.</summary>
        public static Shot OverheadRotation(Vector3 c, float u, float h = 4.5f, float a0 = 0f, float a1 = 40f, float fov = 46f)
        {
            float a = Mathf.Lerp(a0, a1, u) * Mathf.Deg2Rad;
            var s = new Shot(c + Vector3.up * h, c, fov);
            s.up = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            return s;
        }

        public static Shot PedestalUp(Vector3 subject, float u, float back = 3f, float h0 = 0.3f, float h1 = 1.9f, float fov = 34f)
        {
            float h = Mathf.Lerp(h0, h1, Ease(u));
            return new Shot(subject + new Vector3(0f, h - subject.y + subject.y * 0f, -back), subject + Vector3.up * (h - 1f) * 0.5f, fov);
        }

        public static Shot LowAngleArc(Vector3 subject, float u, float r = 3f, float a0 = -50f, float a1 = 10f, float fov = 36f)
        {
            var p = Around(subject, Mathf.Lerp(a0, a1, Ease(u)), r, 0f);
            p.y = 0.25f;
            return new Shot(p, subject + Vector3.up * 0.4f, fov);
        }

        /// <summary>Sideways past something close to the lens (a door frame, a wall): what is behind it appears.</summary>
        public static Shot LateralReveal(Vector3 from, Vector3 to, Vector3 subject, float u, float fov = 36f)
            => new Shot(Vector3.Lerp(from, to, Ease(u)), subject, fov);
    }
}
