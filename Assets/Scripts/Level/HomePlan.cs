namespace ProjectInk
{
    /// <summary>14 Alder Lane (tools/blender/build_home_set.py): the street in front, the house cut away
    /// side-on (two flat floors joined by the stairs, which the story walks him up), the garden behind.</summary>
    public static class HomePlan
    {
        public const float Floor2 = 3.2f;
        public const float HouseX0 = 0f, HouseX1 = 34f;
        public const float StairX0 = 4.2f, StairX1 = 9.4f, StairZ = 3.3f;
        public const float StudyDeskX = 12.0f, DeskZ = 3.4f;
        public const float CoffeeTableX = 20.0f;
        public const float BackDoorX = 34f, ElenaX = 32.6f;
        public const float SwingX = 53.6f, SwingZ = 3.2f;
        public const float StreetX0 = -24f, GateX = -3.4f;
        // the fight: four rooms, then the bedroom (x0, x1, floor y)
        public static readonly (float x0, float x1, float y, string name)[] Rooms =
        {
            (25.2f, 33.6f, 0f, "THE KITCHEN"),
            (15.2f, 25.2f, 0f, "THE LIVING ROOM"),
            (3.2f, 15.0f, 0f, "THE HALL"),
            (3.4f, 14.0f, Floor2, "THE LANDING"),
        };
        public const float MayaRoomX0 = 14.2f, MayaRoomX1 = 21.8f;
        public const float BedroomX0 = 22.2f, BedroomX1 = 33.6f;
    }
}
