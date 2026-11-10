using UnityEngine;

namespace ProjectInk
{
    /// <summary>Combat 3.0 (docs/design/combat_v3.md), the experiment: chapter 1 only, for now.
    /// Chapter 1 runs on the V3 body (longer arms, fists, the hand-keyed strikes) whichever controls
    /// are chosen; the pause card's COMBAT button picks NEW (the touch grammar: touch the man to hit
    /// him where you touch, yourself to defend, the ground to move) or CLASSIC (the old taps and
    /// swipes). The choice is saved. The old controls are kept intact on the backup/combat-v2 branch.</summary>
    public static class CombatV3
    {
        const string Key = "combat_v3";
        static int chosen = -1;

        /// <summary>The player wants the new controls (default: yes, it is the experiment).</summary>
        public static bool Chosen
        {
            get { if (chosen < 0) chosen = PlayerPrefs.GetInt(Key, 1); return chosen == 1; }
            set { chosen = value ? 1 : 0; PlayerPrefs.SetInt(Key, chosen); PlayerPrefs.Save(); }
        }

        /// <summary>This scene can run the new controls: chapter 1 (the warehouse), on the V3 body.</summary>
        public static bool Available => StoryIntro.Instance != null && StoryIntro.Instance.Chapter == 1 && V3Body;

        /// <summary>The scene's fighters are the V3 body (their move set has the guard).</summary>
        public static bool V3Body;

        public static bool On => Chosen && Available;

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
