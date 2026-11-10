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

        // ---------------------------------------------------------------- Combat 3.0: one pulse per event
        /// <summary>A block on the forearms: a dull thud.</summary>
        public static void Block() => Pulse(38, 110);
        /// <summary>A parry: a sharp tick.</summary>
        public static void Parry() => Pulse(14, 255);
        /// <summary>A counter: a double knock.</summary>
        public static void Counter() => Pattern(new long[] { 0, 22, 55, 34 }, new[] { 0, 255, 0, 200 });
        /// <summary>A blow landing: light ones a tap, heavy ones one strong jolt.</summary>
        public static void Landed(bool heavy) => Pulse(heavy ? 60 : 16, heavy ? 255 : 120);
        /// <summary>Taking a blow: a long buzz.</summary>
        public static void Hurt(bool heavy) => Pulse(heavy ? 160 : 90, heavy ? 230 : 160);

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;
        static AndroidJavaObject Vibrator()
        {
            if (vibrator != null) return vibrator;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
            }
            catch (System.Exception) { vibrator = null; }
            return vibrator;
        }
#endif

        /// <summary>One pulse: milliseconds and strength (1-255). On phones without amplitude
        /// control, a plain buzz of that length; on iOS only the strong ones (a full buzz).</summary>
        public static void Pulse(long ms, int amplitude)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var v = Vibrator();
            if (v == null) { Handheld.Vibrate(); return; }
            try
            {
                if (sdk >= 26)
                    using (var fx = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = fx.CallStatic<AndroidJavaObject>("createOneShot", ms, Mathf.Clamp(amplitude, 1, 255)))
                        v.Call("vibrate", e);
                else v.Call("vibrate", ms);
            }
            catch (System.Exception) { }
#elif UNITY_IOS && !UNITY_EDITOR
            if (amplitude >= 200) Handheld.Vibrate();
#endif
        }

        /// <summary>A pattern: timings (ms, starting with an off gap) and strengths.</summary>
        public static void Pattern(long[] timings, int[] amplitudes)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var v = Vibrator();
            if (v == null) { Handheld.Vibrate(); return; }
            try
            {
                if (sdk >= 26)
                    using (var fx = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = fx.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        v.Call("vibrate", e);
                else v.Call("vibrate", timings, -1);
            }
            catch (System.Exception) { }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
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
