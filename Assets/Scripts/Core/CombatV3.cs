using UnityEngine;

namespace ProjectInk
{
    /// <summary>The V3 body (chapter 1): the strike lab's stickman, with longer arms, fists and the
    /// hand-keyed strikes, guard and reactions (tools/blender/build_stickman_v3.py). The classic
    /// controls drive it. (Combat 3.0's touch grammar was tried on it and dropped: a thumb on the man
    /// hides the man; see docs/design/combat_v3.md.)</summary>
    public static class CombatV3
    {
        /// <summary>The scene's fighters are the V3 body (their move set has its guard).</summary>
        public static bool V3Body;

        // ---------------------------------------------------------------- the far-limb shade
        static readonly int ShadeId = Shader.PropertyToID("_InkDepthShade");
        static float shade = -1f;

        /// <summary>Option C: the far limbs a charcoal grey in fights, by depth; pure ink in cutscenes.
        /// Eased, so a cut into a cinematic never pops.</summary>
        public static void Shade(bool fighting, float dt)
        {
            float goal = fighting ? 1f : 0f;
            shade = shade < 0f ? goal : Mathf.MoveTowards(shade, goal, dt / 0.45f);
            Shader.SetGlobalFloat(ShadeId, shade);
        }
    }
}
