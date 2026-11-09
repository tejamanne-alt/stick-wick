using UnityEngine;

namespace ProjectInk
{
    /// <summary>A wall on the camera side of the building (the warehouse's front walls). It is
    /// really there, and every shot from inside sees it; while the camera is outside it (the
    /// side-on fight, and the cinematic angles from beyond it) it is cut away, so we see in.</summary>
    public class CutawayWall : MonoBehaviour
    {
        [Tooltip("World z of the wall's inner face: the camera further out than this sees through it.")]
        public float insideZ = -1.15f;
        /// <summary>A story shot from outside wants the house whole (the street at dusk): show every front wall.</summary>
        public static bool ShowAll;
        Renderer[] rs;
        bool shown = true;

        void Awake() { rs = GetComponentsInChildren<Renderer>(true); }

        void LateUpdate()
        {
            var c = Camera.main;
            if (c == null) return;
            bool show = ShowAll || c.transform.position.z > insideZ;
            if (show == shown) return;
            shown = show;
            foreach (var r in rs) if (r != null) r.enabled = show;
        }
    }
}
