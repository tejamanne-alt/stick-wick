using UnityEngine;

namespace ProjectInk
{
    /// <summary>The epilogue's three places (tools/blender/build_epilogue_set.py; written by its --write-plan),
    /// laid out along x with wide gaps, each in its own sky: A the bottom of the cliff road (x 0), B the
    /// cemetery (x 500), C the backyard of No. 14 and the hall inside (x 1000). World coordinates.</summary>
    public static class EpiloguePlan
    {
        public static readonly Vector3 RoadOrigin = new Vector3(0.000f, 0.000f, 0.000f), CemeteryOrigin = new Vector3(500.000f, 0.000f, 0.000f), HomeOrigin = new Vector3(1000.000f, 0.000f, 0.000f);

        // ---- A: the road. Harrow stands by his car; he and Maya face him from the +x side.
        public static readonly Vector3 RoadSpot = new Vector3(2.100f, 0.000f, -0.450f);
        public static readonly Vector3 MayaRoadSpot = new Vector3(2.550f, 0.000f, -0.900f);
        public static readonly Vector3 HarrowSpot = new Vector3(0.200f, 0.000f, -0.600f);
        public static readonly Vector3 HarrowCar = new Vector3(-2.400f, 0.000f, -2.350f);   // its middle; faces +x
        public static readonly Vector3 ForensicTent = new Vector3(15.000f, 1.298f, 9.200f);
        public static readonly Vector3 CliffTop = new Vector3(16.000f, 60.000f, 31.000f);
        /// <summary>PoliceLight_0.._7: two lenses per car (0/1, 2/3, 4/5) and the van's (6/7).</summary>
        public const int PoliceLightCount = 8;
        public const float RoadX0 = -60f, RoadX1 = 60f;

        // ---- B: the grave. Elena's stone faces -z (towards the path); he kneels before it.
        public static readonly Vector3 CemeteryGate = new Vector3(468.000f, 0.000f, -1.300f);
        /// <summary>The stone's flat top, centre (the walkie-talkie stands here).</summary>
        public static readonly Vector3 ElenaStone = new Vector3(500.000f, 0.920f, 1.600f);
        public static readonly Vector3 ElenaStoneFace = new Vector3(500.000f, 0.570f, 1.530f);
        public static readonly Vector3 KneelSpot = new Vector3(500.000f, 0.000f, 0.820f);
        public static readonly Vector3 MayaGraveSpot = new Vector3(500.620f, 0.000f, 0.880f);
        public static readonly Vector3 GraveFlowers = new Vector3(499.800f, 0.100f, 1.340f);
        public static readonly Vector3 CemeteryPath = new Vector3(500.000f, 0.000f, -1.300f);   // the gravel path runs along x here

        // ---- C: home. SwingSeat's origin is on the top bar; it swings about SwingAxis (the seat moves along x).
        public static readonly Vector3 SwingPivot = new Vector3(1008.500f, 2.250f, -2.000f);
        public static readonly Vector3 SwingSeat = new Vector3(1008.500f, 0.470f, -2.000f);   // the seat's top at rest
        public static readonly Vector3 SwingAxis = Vector3.forward;
        public static readonly Vector3 PushSpot = new Vector3(1007.550f, 0.000f, -2.000f);   // behind the seat, pushing towards +x
        public static readonly Vector3 HouseView = new Vector3(1001.000f, 1.600f, 9.400f);   // wide from the house: at the back door
        public static readonly Vector3 BackDoor = new Vector3(1001.000f, 0.120f, 10.300f);
        /// <summary>BackDoor (a pivot on its hinge) is built standing open; closed: localRotation = Euler(0, BackDoorOpen, 0).</summary>
        public const float BackDoorOpen = 75f;
        public const float HallFloorY = 0.12f;
        /// <summary>HallDrawerFront's origin: its front face. It slides along +x (world) to open, HallDrawerTravel metres.</summary>
        public static readonly Vector3 HallDrawer = new Vector3(1000.360f, 0.680f, 16.600f);
        public static readonly Vector3 HallDrawerSlide = Vector3.right;
        public const float HallDrawerTravel = 0.28f;
        public static readonly Vector3 HallPhoto = new Vector3(1000.036f, 1.550f, 16.600f);
        public static readonly Vector3 HallSpot = new Vector3(1000.950f, 0.120f, 16.600f);   // facing -x, at the drawer
        public static readonly Vector3 FrontDoor = new Vector3(1000.900f, 1.100f, 19.650f);   // its inside face
        public static readonly Vector3 KitchenDoor = new Vector3(1002.000f, 0.120f, 11.250f);
        public const float HallX0 = 1000.00f, HallX1 = 1002.00f, HallZ0 = 10.30f, HallZ1 = 19.70f;
    }
}
