namespace ProjectInk
{
    /// <summary>Kessel Hill (tools/blender/build_fortress_set.py): the cliff road on the ground, then the
    /// gate, the courtyard and the great hall up on the hill (Up metres higher), joined by the rock stair.</summary>
    public static class FortressPlan
    {
        public const float Up = 6f;
        public const float StairX0 = 44f, StairX1 = 52f;
        public const float GateArchX = 69f, PortcullisZ = -1.4f;
        public const float HallX0 = 128f, HallX1 = 166f;
        public const float TowerX = 150f, TowerZ = 9f, GalleryY = Up + 24.6f;
        public const float FloodPoleX = 116f, GeneratorX = 121f;
        // the fight: (x0, x1, floor y, name)
        public static readonly (float x0, float x1, float y, string name)[] Zones =
        {
            (0.6f, 43.4f, 0f, "THE CLIFF ROAD"),
            (53.0f, 68.2f, Up, "THE GATE"),
            (70.2f, 100f, Up, "THE COURTYARD"),
            (100f, 127.4f, Up, "THE COURTYARD"),
            (128.8f, 165.2f, Up, "THE GREAT HALL"),
        };
    }
}
