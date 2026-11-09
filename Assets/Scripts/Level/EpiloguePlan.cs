using UnityEngine;

namespace ProjectInk
{
    /// <summary>The epilogue's places (tools/blender/build_epilogue_set.py). PLACEHOLDER values until the
    /// set's builder writes the real ones: the bottom of the hill road, the cemetery, the backyard swing,
    /// the hallway drawer, laid out along x.</summary>
    public static class EpiloguePlan
    {
        public static readonly Vector3 RoadSpot = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 HarrowSpot = new Vector3(1.6f, 0f, 0.2f);
        public static readonly Vector3 CemeteryGate = new Vector3(40f, 0f, 0f);
        public static readonly Vector3 ElenaStone = new Vector3(48f, 0.9f, 2f);
        public static readonly Vector3 KneelSpot = new Vector3(47.4f, 0f, 1.2f);
        public static readonly Vector3 SwingSeat = new Vector3(90f, 0.5f, 1f);
        public static readonly Vector3 PushSpot = new Vector3(88.8f, 0f, 1f);
        public static readonly Vector3 HallDrawer = new Vector3(130f, 0.8f, 1.5f);
        public static readonly Vector3 HallSpot = new Vector3(129.2f, 0f, 0.6f);
        public static readonly Vector3 FrontDoor = new Vector3(133f, 1f, 0f);
    }
}
