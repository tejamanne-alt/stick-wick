using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>A man on watch with his back to the hero, posted at the start of a fight zone: the street's
    /// arches, the living room, the car park (the warehouse's mezzanine and the fortress have their own).
    /// The zone's men wait while he's unaware: creep up (swipe down and hold to crouch) and take him
    /// quietly, or he turns, and the fight starts.</summary>
    public partial class StoryIntro
    {
        Enemy lookout;

        /// <summary>Post the zone's lookout (into the zone's list, so the zone isn't clear till he's dealt with).</summary>
        void PostLookout(float x, float y, int facing, List<Enemy> men, string what)
        {
            lookout = null;
            if (director == null) return;
            var e = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
            if (e == null) return;
            e.PostUnaware(new Vector3(x, y, 0f), facing);
            men.Add(e);
            lookout = e;
            director.ShowBanner(what + "  ·  SWIPE DOWN AND HOLD TO CROUCH  ·  TAP HIS BACK", 3.2f);
        }

        /// <summary>The lookout is still unaware: the zone's other men hold back.</summary>
        bool LookoutHolds => lookout != null && lookout.Alive && lookout.Unaware;
    }
}
