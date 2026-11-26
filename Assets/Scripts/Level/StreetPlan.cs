namespace ProjectInk
{
    /// <summary>The street's plan (tools/blender/build_street_set.py): one flat lane along the
    /// sidewalk, four zones from the bank's steps to the crossroads.</summary>
    public static class StreetPlan
    {
        public static readonly float[] ZoneX = { 0f, 30f, 62f, 92f, 130f };
        public static readonly string[] ZoneName = { "OUTSIDE THE BANK", "THE MARKET STREET", "UNDER THE RAILWAY", "THE CROSSROADS" };
        // the lane ends at the stalled bus across the far end (its front at x 125.4, on the pavement from z -0.9 to 1.8):
        // nobody walks further than a body's width short of it
        public const float LaneX0 = -12f, LaneX1 = 124.8f;
        public const float KerbCarX = 16f, KerbCarZ = -3.3f;        // the black car the first men come out of
        public const float DriverCarX = 122f, DriverCarZ = -3.3f;   // the Driver's car (moves); its nose points -x once parked
        public const float RailX0 = 63f, RailX1 = 91f, DeckY = 6.4f;
        public const float CrashX = 92.6f;                          // the railway's last pillar: where the car ends
        public const float BankDoorX = -6.5f;
        public const float KerbEdgeZ = -2.4f, RoadY = -0.15f;       // the kerb's road-side edge (the pavement ends there), the road a step below
        public const float LightsX = 121f, LightsZ = -1.95f;        // the traffic lights' post at the kerb, by the Driver's car
        public const float WreckClearX = 97.6f;                     // once the car is in the pillar, the lane is clear from here on
    }
}
