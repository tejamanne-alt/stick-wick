using UnityEngine;

namespace ProjectInk
{
    /// <summary>The only script allowed to touch Time.timeScale. Handles hit-stop
    /// (a near-freeze for a few frames) and slow-motion requests; the strongest
    /// active request wins. All timers run on unscaled time.</summary>
    public class TimeController : MonoBehaviour
    {
        public static TimeController Instance { get; private set; }

        const float BaseFixedDelta = 0.02f;

        float freezeUntil;
        float slowScale = 1f, slowUntil, slowIn, slowOut, slowStart;
        float rampageScale = 1f, rampageUntil;

        /// <summary>Rampage: hold time at `scale` for `seconds` of real time, under any other slow-mo.</summary>
        public void Rampage(float scale, float seconds)
        {
            rampageScale = scale;
            rampageUntil = UnityEngine.Time.unscaledTime + seconds;
        }

        public bool InRampage => UnityEngine.Time.unscaledTime < rampageUntil;

        /// <summary>A story scene holding time (its directed fight crawls almost to a stop while it
        /// waits for the player's input): applied under everything else, 1 = off.</summary>
        public float CineScale = 1f;

        public bool InSlowMo => UnityEngine.Time.unscaledTime < slowUntil;
        /// <summary>The help card is open: the world stops.</summary>
        public static bool Paused;
        public float Scale => UnityEngine.Time.timeScale;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            // landscape only, whatever the build settings say
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.AutoRotation;
            UnityEngine.Time.timeScale = 1f;
            UnityEngine.Time.fixedDeltaTime = BaseFixedDelta;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                UnityEngine.Time.timeScale = 1f;
                UnityEngine.Time.fixedDeltaTime = BaseFixedDelta;
            }
        }

        /// <summary>Freeze for the given number of 60 fps frames (2 light, 5 heavy, 8 finisher).</summary>
        public void HitStop(int frames)
        {
            freezeUntil = Mathf.Max(freezeUntil, UnityEngine.Time.unscaledTime + frames / 60f);
        }

        /// <summary>Slow time to `scale` for `seconds` of real time, easing in and out.</summary>
        public void SlowMo(float scale, float seconds, float easeIn = 0.05f, float easeOut = 0.25f)
        {
            float now = UnityEngine.Time.unscaledTime;
            if (InSlowMo && scale > slowScale) return; // a stronger slow-mo is already running
            slowScale = Mathf.Clamp(scale, 0.05f, 1f);
            slowStart = now;
            slowUntil = now + seconds;
            slowIn = easeIn;
            slowOut = easeOut;
        }

        public void Clear()
        {
            CineScale = 1f;
            freezeUntil = 0f;
            slowUntil = 0f;
            rampageUntil = 0f;
        }

        void Update()
        {
            float now = UnityEngine.Time.unscaledTime;
            float scale = 1f;
            if (now < slowUntil)
            {
                float a = Mathf.Clamp01((now - slowStart) / Mathf.Max(0.001f, slowIn));
                float b = Mathf.Clamp01((slowUntil - now) / Mathf.Max(0.001f, slowOut));
                scale = Mathf.Lerp(1f, slowScale, Mathf.Min(a, b));
            }
            if (now < rampageUntil)
            {
                float k = Mathf.Clamp01((rampageUntil - now) / 0.3f);   // ease out at the end
                scale = Mathf.Min(scale, Mathf.Lerp(1f, rampageScale, k));
            }
            scale = Mathf.Min(scale, Mathf.Clamp(CineScale, 0.005f, 1f));
            if (now < freezeUntil) scale = Mathf.Min(scale, 0.02f);
            if (Paused) scale = 0f;
            UnityEngine.Time.timeScale = scale;
            UnityEngine.Time.fixedDeltaTime = BaseFixedDelta * Mathf.Max(scale, 0.05f);
        }
    }
}
