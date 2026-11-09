namespace ProjectInk
{
    /// <summary>Behind 14 Alder Lane at night (tools/blender/build_park_set.py, whose constants these
    /// mirror): one flat lane along x (z = 0, clear of obstacles between z -0.8 and 0.8) through four
    /// stretches: the garden, the playground, the pond and its footbridge, the car park. The swings,
    /// the gate, the park and the car park line up along z = SightZ, so a long lens from the swings
    /// sees down the whole park to the van's back window; Kessel Hill and its lit fortress behind it.
    /// Unity coordinates: x along the lane, y up, z away from the gameplay camera.</summary>
    public static class ParkPlan
    {
        // ---------------------------------------------------------------- the lane and its stretches
        /// <summary>Stretch edges: the garden 4-34, the playground 34-66, the pond 66-98, the car park 98-131.</summary>
        public static readonly float[] ZoneX = { 4f, 34f, 66f, 98f, 131f };
        public static readonly string[] ZoneName = { "THE GARDEN", "THE PLAYGROUND", "THE POND", "THE CAR PARK" };
        /// <summary>Where he can walk: from the back door (x 0 is the kitchen's wall) to past the van's back.</summary>
        public const float LaneX0 = 1.0f, LaneX1 = 131f;
        /// <summary>The lane is kept clear of the set between z -LaneHalfZ and LaneHalfZ.</summary>
        public const float LaneHalfZ = 0.8f;

        // ---------------------------------------------------------------- the garden
        /// <summary>The back door (in the kitchen's wall at x 0), the lit kitchen window beside it.</summary>
        public static readonly UnityEngine.Vector3 BackDoor = new UnityEngine.Vector3(0f, 0f, -0.3f);
        public static readonly UnityEngine.Vector3 KitchenWindow = new UnityEngine.Vector3(0f, 1.5f, 1.8f);
        /// <summary>The swing frame's centre (x, z); the moving seat ("Swing") hangs at SwingX - 0.8 from
        /// the top bar at SwingTopY: its pivot is SwingPivot (the object's own origin too).</summary>
        public const float SwingX = 27.0f, SwingZ = 2.9f, SwingTopY = 2.4f;
        public static readonly UnityEngine.Vector3 SwingPivot = new UnityEngine.Vector3(SwingX - 0.8f, SwingTopY, SwingZ);
        /// <summary>Her ribbon in the grass by the swing (the prop is the story's: the game's one red).</summary>
        public const float RibbonX = 26.2f, RibbonZ = 2.2f;
        /// <summary>The garden's end: the low picket fence and the broken back gate on the lane.</summary>
        public const float GateX = 34f;

        // ---------------------------------------------------------------- the park
        /// <summary>The long sightline's depth: the swings, the park and the van all sit on it.</summary>
        public const float SightZ = 2.9f;
        /// <summary>The tall slide's platform (2.6 m up); the dark den under it, where she hid, opens to -z.</summary>
        public static readonly UnityEngine.Vector3 TallSlide = new UnityEngine.Vector3(52f, 2.6f, 7f);
        public static readonly UnityEngine.Vector3 SlideDen = new UnityEngine.Vector3(52f, 0f, 6.6f);
        /// <summary>The playground's back railings (1.85 m) the men with torches climb over.</summary>
        public const float BackFenceZ = 12.6f;
        /// <summary>The pond (centre, half extents in x and z; its water 0.24 below the lane) and the
        /// footbridge the lane crosses (deck top at y 0, parapets at z +-1.25).</summary>
        public static readonly UnityEngine.Vector3 Pond = new UnityEngine.Vector3(82f, -0.24f, 1.6f);
        public const float PondHalfX = 12f, PondHalfZ = 7.2f;
        public const float BridgeX0 = 71.5f, BridgeX1 = 92.5f;
        /// <summary>The park lamps (x, z), their globes LampY up; the car park's double-headed columns.</summary>
        public static readonly UnityEngine.Vector2[] Lamps =
        {
            new UnityEngine.Vector2(37f, -1.75f), new UnityEngine.Vector2(49f, -1.75f), new UnityEngine.Vector2(61f, -1.75f),
            new UnityEngine.Vector2(69f, -1.75f), new UnityEngine.Vector2(82f, -1.6f), new UnityEngine.Vector2(96f, -1.75f),
            new UnityEngine.Vector2(56.5f, 4.7f), new UnityEngine.Vector2(88f, 9.6f),
        };
        public const float LampY = 3.95f;
        public static readonly UnityEngine.Vector2[] CarLamps = { new UnityEngine.Vector2(110.5f, 10f), new UnityEngine.Vector2(124f, 10f) };
        public const float CarLampY = 6.4f;

        // ---------------------------------------------------------------- the car park, the van
        /// <summary>The van ("Van"): its origin, on the ground under its centre; it is VanLength long, its
        /// back (towards the park) at VanX - VanLength / 2, its nose towards the barrier and the hill road.</summary>
        public const float VanX = 128.2f, VanZ = 2.9f, VanLength = 5.4f;
        /// <summary>The back window Maya looks through (in the left back door, seen from behind).</summary>
        public static readonly UnityEngine.Vector3 VanWindow = new UnityEngine.Vector3(VanX - VanLength / 2 - 0.06f, 1.78f, VanZ + 0.5f);
        /// <summary>The back doors' hinges (VanDoorL: the +z one, VanDoorR: the -z one); they swing about y.</summary>
        public static readonly UnityEngine.Vector3 VanHingeL = new UnityEngine.Vector3(VanX - VanLength / 2 - 0.03f, 0f, VanZ + 0.99f);
        public static readonly UnityEngine.Vector3 VanHingeR = new UnityEngine.Vector3(VanX - VanLength / 2 - 0.03f, 0f, VanZ - 0.99f);
        /// <summary>The barrier ("BarrierArm", raised, hinged at BarrierHinge) across the exit; the hill
        /// road goes out past it along +x, curving to +z and rising towards Kessel Hill.</summary>
        public static readonly UnityEngine.Vector3 BarrierHinge = new UnityEngine.Vector3(134.6f, 1.0f, 6.5f);
        public static readonly UnityEngine.Vector3 HillRoadOut = new UnityEngine.Vector3(173.6f, 1.7f, 14.9f);

        // ---------------------------------------------------------------- the horizon
        /// <summary>The fortress on the crown of Kessel Hill (its keep), lit, storm clouds over it.</summary>
        public static readonly UnityEngine.Vector3 Fortress = new UnityEngine.Vector3(251.5f, 41f, 43f);
        public static readonly UnityEngine.Vector3 KesselHill = new UnityEngine.Vector3(250f, 30f, 40f);
    }
}
