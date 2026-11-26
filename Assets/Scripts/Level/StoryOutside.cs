using UnityEngine;

namespace ProjectInk
{
    /// <summary>Chapter 1: the yard and the city outside the shed exist only for the opening's wide shot
    /// (SHOT 0, the only one that looks at them). Once the story is inside, they're switched off, so
    /// anything ever seen past a wall (a scripted shot, an odd screen shape) is the dark outside, never a
    /// lit set ending. The camera itself keeps to the walls (CameraDirector.Frame).</summary>
    public partial class StoryIntro
    {
        Renderer[] outsideParts;
        bool outsideShown = true;

        bool OutsideWanted => phase == Phase.Cinematic && t < S1 + 0.2f;

        void UpdateOutside()
        {
            if (chapter != 1) return;
            if (outsideParts == null)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (var n in new[] { "SetYard", "SetCity" })
                {
                    var g = GameObject.Find(n);
                    if (g != null) list.AddRange(g.GetComponentsInChildren<Renderer>(true));
                }
                outsideParts = list.ToArray();
            }
            bool want = OutsideWanted;
            if (want == outsideShown) return;
            outsideShown = want;
            foreach (var r in outsideParts) if (r != null) r.enabled = want;
        }
    }
}
