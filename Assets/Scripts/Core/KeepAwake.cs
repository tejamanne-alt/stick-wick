using UnityEngine;

namespace ProjectInk
{
    /// <summary>The phone's screen must never dim or lock in the middle of a cutscene (where the
    /// player only watches): set once when the game starts, for every scene. Phones only: in the
    /// Editor it would hold the Mac's display (and so the Mac) awake until Unity quits.</summary>
    public static class KeepAwake
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
        }
    }
}
