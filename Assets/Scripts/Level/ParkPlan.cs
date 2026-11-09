namespace ProjectInk
{
    /// <summary>Behind 14 Alder Lane at night (tools/blender/build_park_set.py): the garden, the park, the
    /// pond and the car park, one flat lane. Kessel Hill and its fortress on the horizon.</summary>
    public static class ParkPlan
    {
        public static readonly float[] ZoneX = { 8f, 34f, 70f, 100f, 130f };
        public static readonly string[] ZoneName = { "THE GARDEN", "THE PLAYGROUND", "THE POND", "THE CAR PARK" };
        public const float LaneX0 = 1.0f, LaneX1 = 129f;
        public const float SwingX = 27.0f, SwingZ = 2.8f, RibbonX = 26.2f;
        public const float VanX = 118f, VanZ = 3.0f;
        public static readonly UnityEngine.Vector3 Fortress = new UnityEngine.Vector3(130f, 42f, 108f);
    }
}
