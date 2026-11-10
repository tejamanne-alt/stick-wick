using UnityEngine;

namespace ProjectInk
{
    /// <summary>The phone's screen must never dim or lock in the middle of a cutscene (where the
    /// player only watches): set once when the game starts, for every scene.</summary>
    public static class KeepAwake
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
