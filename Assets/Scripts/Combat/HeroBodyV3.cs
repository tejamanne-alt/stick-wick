using UnityEngine;

namespace ProjectInk
{
    /// <summary>The V3 body's extras on the hero (chapter 1): the far-limb shade in fights (option C)
    /// and the impact stretch of the striking limb.</summary>
    public partial class HeroController
    {
        LimbStretch stretch;

        void V3Init()
        {
            CombatV3.V3Body = body != null && body.Has("guard_hold") && body.Has("coil");
            if (CombatV3.V3Body && stretch == null) stretch = gameObject.AddComponent<LimbStretch>();
            if (stretch != null) stretch.player = body;
        }

        /// <summary>Every frame, cutscenes included: the shade fades in for fights, out for cinematics.</summary>
        void V3Always()
        {
            if (!CombatV3.V3Body) return;
            bool fighting = !CinematicLock && state != State.Dead && (StoryIntro.Instance == null || !StoryIntro.Instance.HideHud) && AnyFoeNear(7f);
            CombatV3.Shade(fighting, Time.unscaledDeltaTime);
        }

        bool AnyFoeNear(float range)
        {
            foreach (var e in Enemy.All)
                if (e != null && e.Targetable && Mathf.Abs(e.transform.position.x - transform.position.x) < range &&
                    Mathf.Abs(e.transform.position.y - transform.position.y) < 1.2f) return true;
            return false;
        }

        /// <summary>The blow landed: the striking limb stretches through the impact.</summary>
        void V3Landed()
        {
            if (stretch != null) stretch.Hit(currentAttack.clip, currentAttack.heavy || isCounter ? 0.11f : 0.07f);
        }
    }
}
