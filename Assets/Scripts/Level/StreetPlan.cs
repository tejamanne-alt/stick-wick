namespace ProjectInk
{
    /// <summary>The street's plan (tools/blender/build_street_set.py): one flat lane along the
    /// sidewalk, four zones from the bank's steps to the crossroads.</summary>
    public static class StreetPlan
    {
        public static readonly float[] ZoneX = { 0f, 30f, 62f, 92f, 130f };
        public static readonly string[] ZoneName = { "OUTSIDE THE BANK", "THE MARKET STREET", "UNDER THE RAILWAY", "THE CROSSROADS" };
        public const float LaneX0 = -12f, LaneX1 = 129f;
        public const float KerbCarX = 16f, KerbCarZ = -3.3f;        // the black car the first men come out of
        public const float DriverCarX = 122f, DriverCarZ = -3.3f;   // the Driver's car (moves)
        public const float RailX0 = 63f, RailX1 = 91f, DeckY = 6.4f;
        public const float CrashX = 92.6f;                          // the railway's last pillar: where the car ends
        public const float BankDoorX = -6.5f;
    }
}
