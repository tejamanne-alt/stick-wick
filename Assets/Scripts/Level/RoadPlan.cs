using UnityEngine;

namespace ProjectInk
{
    /// <summary>Kessel Hill's road at night (tools/blender/build_road_set.py; written by its --write-plan):
    /// five legs climbing the hill's face in hairpins to the fortress's cliff road (y 0), where the car
    /// stops in the floodlight short of x 0 (the fortress chapter starts the hero at x 2). The car keeps the
    /// left lane. World coordinates (x along, y up, z depth). Right-hand drive: the driver sits at -z.</summary>
    public static class RoadPlan
    {
        /// <summary>The car's line up the hill, every 5 m, from the foot (x -240) to the stop.</summary>
        public static readonly Vector3[] Path =
        {
            new Vector3(-240.00f, -65.34f, -70.40f), new Vector3(-235.00f, -64.92f, -70.40f), new Vector3(-230.00f, -64.50f, -70.40f), new Vector3(-225.00f, -64.09f, -70.40f),
            new Vector3(-220.00f, -63.67f, -70.40f), new Vector3(-215.00f, -63.25f, -70.40f), new Vector3(-210.00f, -62.83f, -70.40f), new Vector3(-205.00f, -62.42f, -70.40f),
            new Vector3(-200.00f, -62.00f, -70.40f), new Vector3(-195.00f, -61.58f, -70.40f), new Vector3(-190.00f, -61.17f, -70.40f), new Vector3(-185.00f, -60.75f, -70.40f),
            new Vector3(-180.00f, -60.33f, -70.40f), new Vector3(-175.00f, -59.91f, -70.40f), new Vector3(-170.00f, -59.50f, -70.40f), new Vector3(-165.00f, -59.08f, -70.40f),
            new Vector3(-160.00f, -58.66f, -70.40f), new Vector3(-155.00f, -58.25f, -70.40f), new Vector3(-150.00f, -57.83f, -70.40f), new Vector3(-145.00f, -57.41f, -70.40f),
            new Vector3(-140.00f, -56.99f, -70.40f), new Vector3(-135.00f, -56.58f, -70.40f), new Vector3(-130.00f, -56.16f, -70.40f), new Vector3(-125.00f, -55.74f, -70.40f),
            new Vector3(-120.00f, -55.32f, -70.40f), new Vector3(-115.00f, -54.91f, -70.40f), new Vector3(-110.00f, -54.49f, -70.40f), new Vector3(-105.00f, -54.07f, -70.40f),
            new Vector3(-100.00f, -53.66f, -70.40f), new Vector3(-95.00f, -53.24f, -70.40f), new Vector3(-90.00f, -52.82f, -70.40f), new Vector3(-85.00f, -52.40f, -70.40f),
            new Vector3(-80.00f, -51.99f, -70.40f), new Vector3(-75.00f, -51.57f, -70.40f), new Vector3(-70.00f, -51.15f, -70.40f), new Vector3(-65.00f, -50.74f, -70.40f),
            new Vector3(-60.00f, -50.32f, -70.40f), new Vector3(-55.00f, -49.90f, -70.40f), new Vector3(-50.00f, -49.48f, -70.40f), new Vector3(-45.00f, -49.07f, -70.40f),
            new Vector3(-40.00f, -48.65f, -70.40f), new Vector3(-35.00f, -48.23f, -70.40f), new Vector3(-30.00f, -47.81f, -70.40f), new Vector3(-25.00f, -47.40f, -70.40f),
            new Vector3(-19.17f, -46.90f, -70.35f), new Vector3(-14.21f, -46.31f, -67.61f), new Vector3(-12.65f, -45.72f, -62.17f), new Vector3(-15.39f, -45.13f, -57.21f),
            new Vector3(-21.00f, -44.54f, -55.60f), new Vector3(-26.00f, -44.12f, -55.60f), new Vector3(-31.00f, -43.70f, -55.60f), new Vector3(-36.00f, -43.29f, -55.60f),
            new Vector3(-41.00f, -42.87f, -55.60f), new Vector3(-46.00f, -42.45f, -55.60f), new Vector3(-51.00f, -42.04f, -55.60f), new Vector3(-56.00f, -41.62f, -55.60f),
            new Vector3(-61.00f, -41.20f, -55.60f), new Vector3(-66.00f, -40.78f, -55.60f), new Vector3(-71.00f, -40.37f, -55.60f), new Vector3(-76.00f, -39.95f, -55.60f),
            new Vector3(-81.00f, -39.53f, -55.60f), new Vector3(-86.00f, -39.12f, -55.60f), new Vector3(-91.00f, -38.70f, -55.60f), new Vector3(-96.00f, -38.28f, -55.60f),
            new Vector3(-101.00f, -37.86f, -55.60f), new Vector3(-106.00f, -37.45f, -55.60f), new Vector3(-111.00f, -37.03f, -55.60f), new Vector3(-116.00f, -36.61f, -55.60f),
            new Vector3(-121.00f, -36.20f, -55.60f), new Vector3(-126.00f, -35.78f, -55.60f), new Vector3(-131.00f, -35.36f, -55.60f), new Vector3(-136.00f, -34.94f, -55.60f),
            new Vector3(-141.19f, -34.53f, -55.53f), new Vector3(-146.61f, -34.10f, -53.29f), new Vector3(-150.01f, -33.68f, -48.50f), new Vector3(-150.33f, -33.26f, -42.64f),
            new Vector3(-147.50f, -32.84f, -37.50f), new Vector3(-142.36f, -32.42f, -34.67f), new Vector3(-137.00f, -32.00f, -34.40f), new Vector3(-132.00f, -31.58f, -34.40f),
            new Vector3(-127.00f, -31.17f, -34.40f), new Vector3(-122.00f, -30.75f, -34.40f), new Vector3(-117.00f, -30.33f, -34.40f), new Vector3(-112.00f, -29.92f, -34.40f),
            new Vector3(-107.00f, -29.50f, -34.40f), new Vector3(-102.00f, -29.08f, -34.40f), new Vector3(-97.00f, -28.66f, -34.40f), new Vector3(-92.00f, -28.25f, -34.40f),
            new Vector3(-87.00f, -27.83f, -34.40f), new Vector3(-82.00f, -27.41f, -34.40f), new Vector3(-77.00f, -26.99f, -34.40f), new Vector3(-72.00f, -26.58f, -34.40f),
            new Vector3(-67.00f, -26.16f, -34.40f), new Vector3(-62.00f, -25.74f, -34.40f), new Vector3(-57.00f, -25.33f, -34.40f), new Vector3(-52.00f, -24.91f, -34.40f),
            new Vector3(-47.00f, -24.49f, -34.40f), new Vector3(-42.00f, -24.07f, -34.40f), new Vector3(-37.00f, -23.66f, -34.40f), new Vector3(-32.00f, -23.24f, -34.40f),
            new Vector3(-27.00f, -22.82f, -34.40f), new Vector3(-22.00f, -22.41f, -34.40f), new Vector3(-16.79f, -21.90f, -33.67f), new Vector3(-13.02f, -21.31f, -29.44f),
            new Vector3(-13.33f, -20.72f, -23.79f), new Vector3(-17.56f, -20.13f, -20.02f), new Vector3(-23.00f, -19.63f, -19.60f), new Vector3(-28.00f, -19.21f, -19.60f),
            new Vector3(-33.00f, -18.80f, -19.60f), new Vector3(-38.00f, -18.38f, -19.60f), new Vector3(-43.00f, -17.96f, -19.60f), new Vector3(-48.00f, -17.54f, -19.60f),
            new Vector3(-53.00f, -17.13f, -19.60f), new Vector3(-58.00f, -16.71f, -19.60f), new Vector3(-63.00f, -16.29f, -19.60f), new Vector3(-68.00f, -15.88f, -19.60f),
            new Vector3(-73.00f, -15.46f, -19.60f), new Vector3(-78.00f, -15.04f, -19.60f), new Vector3(-83.00f, -14.62f, -19.60f), new Vector3(-88.00f, -14.21f, -19.60f),
            new Vector3(-93.00f, -13.79f, -19.60f), new Vector3(-98.00f, -13.37f, -19.60f), new Vector3(-103.00f, -12.96f, -19.60f), new Vector3(-108.00f, -12.54f, -19.60f),
            new Vector3(-113.00f, -12.12f, -19.60f), new Vector3(-118.00f, -11.70f, -19.60f), new Vector3(-123.00f, -11.29f, -19.60f), new Vector3(-128.00f, -10.87f, -19.60f),
            new Vector3(-133.00f, -10.45f, -19.60f), new Vector3(-138.00f, -10.03f, -19.60f), new Vector3(-143.50f, -9.62f, -19.01f), new Vector3(-148.29f, -9.19f, -15.61f),
            new Vector3(-150.53f, -8.77f, -10.19f), new Vector3(-149.55f, -8.35f, -4.40f), new Vector3(-145.64f, -7.93f, -0.02f), new Vector3(-140.05f, -7.51f, 1.60f),
            new Vector3(-135.00f, -7.09f, 1.60f), new Vector3(-130.00f, -6.68f, 1.60f), new Vector3(-125.00f, -6.26f, 1.60f), new Vector3(-120.00f, -5.84f, 1.60f),
            new Vector3(-115.00f, -5.42f, 1.60f), new Vector3(-110.00f, -5.01f, 1.60f), new Vector3(-105.00f, -4.59f, 1.60f), new Vector3(-100.00f, -4.17f, 1.60f),
            new Vector3(-95.00f, -3.75f, 1.60f), new Vector3(-90.00f, -3.34f, 1.60f), new Vector3(-85.00f, -2.92f, 1.60f), new Vector3(-80.00f, -2.50f, 1.60f),
            new Vector3(-75.00f, -2.09f, 1.60f), new Vector3(-70.00f, -1.67f, 1.60f), new Vector3(-65.00f, -1.25f, 1.60f), new Vector3(-60.00f, -0.88f, 1.60f),
            new Vector3(-55.00f, -0.57f, 1.60f), new Vector3(-50.00f, -0.33f, 1.60f), new Vector3(-45.00f, -0.16f, 1.60f), new Vector3(-40.00f, -0.05f, 1.60f),
            new Vector3(-35.00f, -0.00f, 1.52f), new Vector3(-30.00f, 0.00f, 1.43f), new Vector3(-25.00f, 0.00f, 1.35f), new Vector3(-20.00f, 0.00f, 1.27f),
            new Vector3(-15.00f, 0.00f, 1.20f), new Vector3(-10.00f, 0.00f, 1.20f), new Vector3(-9.00f, 0.00f, 1.20f),
        };
        /// <summary>The same route as key points (the ends of the legs, the hairpins round), bottom to the stop.</summary>
        public static readonly Vector3[] CarPath =
        {
            new Vector3(-240.00f, -65.34f, -70.40f), new Vector3(-17.56f, -46.73f, -69.98f), new Vector3(-13.73f, -46.22f, -66.94f), new Vector3(-12.65f, -45.72f, -62.17f),
            new Vector3(-14.77f, -45.21f, -57.77f), new Vector3(-19.17f, -44.71f, -55.65f), new Vector3(-141.19f, -34.53f, -55.53f), new Vector3(-147.50f, -34.02f, -52.50f),
            new Vector3(-150.60f, -33.43f, -45.00f), new Vector3(-147.50f, -32.84f, -37.50f), new Vector3(-141.19f, -32.34f, -34.47f), new Vector3(-19.17f, -22.15f, -34.35f),
            new Vector3(-14.77f, -21.65f, -32.23f), new Vector3(-13.02f, -21.31f, -29.44f), new Vector3(-13.02f, -20.81f, -24.56f), new Vector3(-16.06f, -20.30f, -20.73f),
            new Vector3(-21.00f, -19.80f, -19.60f), new Vector3(-141.19f, -9.78f, -19.53f), new Vector3(-148.29f, -9.19f, -15.61f), new Vector3(-150.53f, -8.60f, -7.81f),
            new Vector3(-147.50f, -8.10f, -1.50f), new Vector3(-141.19f, -7.59f, 1.53f), new Vector3(-56.00f, -0.63f, 1.60f), new Vector3(-9.00f, 0.00f, 1.20f),
        };
        public const float PathLength = 824.9f;
        /// <summary>Distance along Path where the car comes out of the last hairpin and the fortress fills the windscreen.</summary>
        public const float RevealDistance = 698.6f;
        public static readonly Vector3 FinalHairpin = new Vector3(-140.000f, -8.689f, -9.000f);
        public static readonly Vector3 CarStop = new Vector3(-9.000f, 0.000f, 1.200f);
        public const float CarStopYaw = 0f;   // the car faces +x at the stop
        public const float GetOutX = -9.35f;
        public static readonly Vector3 GetOutSpot = new Vector3(-9.350f, 0.000f, -0.240f);
        public static readonly Vector3 StopFloodlight = new Vector3(-6.400f, 6.400f, 2.800f);
        public static readonly Vector3 KesselSign = new Vector3(-214.000f, -61.218f, -66.800f);
        public static readonly Vector3 FortressGate = new Vector3(69.000f, 10.000f, 0.000f);
        public static readonly Vector3 Fortress = new Vector3(102.000f, 12.500f, 4.100f);   // the keep's lit windows over the courtyard
        public static readonly Vector3 ObservatoryTop = new Vector3(150.000f, 34.000f, 9.000f);
        public const float CliffRoadY = 0f, FortressHeroStartX = 2f;


        // ---- inside the car. The interior (CarInterior, with WiperL/WiperR and Windscreen under it) stays
        // parked here, facing +x, and is filmed there: these are world points.
        public static readonly Vector3 InteriorOrigin = new Vector3(-30.000f, 0.000f, 1.600f);
        public static readonly Vector3 DriverHead = new Vector3(-30.620f, 1.140f, 1.230f);
        public static readonly Vector3 DriverHands = new Vector3(-29.870f, 0.800f, 1.230f);   // the wheel's centre
        /// <summary>The empty passenger seat's cushion, top centre: the walkie-talkie lies here.</summary>
        public static readonly Vector3 PassengerSeat = new Vector3(-30.420f, 0.488f, 1.970f);
        public static readonly Vector3 PassengerHead = new Vector3(-30.620f, 1.140f, 1.970f);
        public static readonly Vector3 Windscreen = new Vector3(-29.650f, 1.168f, 1.600f);
        /// <summary>Far ahead through the windscreen: the road's end and the fortress over it.</summary>
        public static readonly Vector3 OutsideAhead = new Vector3(90.000f, 20.000f, 3.000f);
        public static readonly Vector3 RearViewMirror = new Vector3(-30.154f, 1.245f, 1.600f);
        public static readonly Vector3 Gauges = new Vector3(-29.538f, 0.865f, 1.230f);
        public static readonly Vector3 WiperPivotR = new Vector3(-29.200f, 0.958f, 1.100f);
        public static readonly Vector3 WiperPivotL = new Vector3(-29.200f, 0.958f, 1.700f);
        /// <summary>The wipers turn about this world axis (the screen's normal), from 0 (parked, lying along the
        /// screen's foot towards +z) to WiperSweep degrees: w.localRotation = Quaternion.AngleAxis(a, WiperAxis).</summary>
        public static readonly Vector3 WiperAxis = new Vector3(0.469f, 0.883f, 0.000f);
        public const float WiperSweep = -100f;
        /// <summary>The exterior car's lamps, in its own frame (RoadCar: +x forward, +z its left).</summary>
        public static readonly Vector3 HeadlampLocal = new Vector3(2.370f, 0.645f, 0.600f);   // the left one; -z for the right
        public const float CarLength = 4.72f, CarHalfWidth = 0.89f, RoofY = 1.42f;


        /// <summary>The car's turn for a direction of travel (the model faces +x).</summary>
        public static Quaternion CarRotation(Vector3 dir)
        {
            dir.y *= 0.6f;   // pitch with the hill, a little less than the road
            return Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
        }

        /// <summary>A point on the path `distance` metres from its start, and the way it is heading.</summary>
        public static Vector3 Sample(float distance, out Vector3 dir)
        {
            distance = Mathf.Clamp(distance, 0f, PathLength);
            for (int i = 0; i < Path.Length - 1; i++)
            {
                float seg = Vector3.Distance(Path[i], Path[i + 1]);
                if (distance <= seg || i == Path.Length - 2)
                {
                    dir = (Path[i + 1] - Path[i]).normalized;
                    return Vector3.Lerp(Path[i], Path[i + 1], seg > 0f ? Mathf.Clamp01(distance / seg) : 0f);
                }
                distance -= seg;
            }
            dir = Vector3.right;
            return Path[Path.Length - 1];
        }
    }
}
