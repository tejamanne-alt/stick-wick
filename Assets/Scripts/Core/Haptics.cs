using UnityEngine;

namespace ProjectInk
{
    /// <summary>The phone's vibration: on unless the player turns it off in the pause menu
    /// (saved, like the sound setting). Everything that buzzes the phone goes through here.</summary>
    public static class Haptics
    {
        const string Key = "vibration_off";
        static int enabled = -1;

        public static bool Enabled
        {
            get
            {
                if (enabled < 0) enabled = PlayerPrefs.GetInt(Key, 0) == 1 ? 0 : 1;
                return enabled == 1;
            }
            set
            {
                enabled = value ? 1 : 0;
                PlayerPrefs.SetInt(Key, value ? 0 : 1);
                PlayerPrefs.Save();
                if (value) Vibrate();                     // a little buzz to say it is back on
            }
        }

        public static void Vibrate()
        {
            if (!Enabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
