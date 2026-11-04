using UnityEngine;

namespace ProjectInk
{
    /// <summary>The opening of the game, played like the first minute of a film:
    /// black screen; letterbox bars part to reveal a slow crane shot over the building
    /// with the title; hard cut to a floor-level shot as the hero crashes through the
    /// skylight; a slow-motion landing that shakes the lamps; a push-in on the hero
    /// beckoning with the chapter card; then the bars slide away, "FIGHT!", and control.
    /// Tap anywhere to skip. A restart plays a short version (bars open, push-in, fight).
    /// All timing is in real seconds, so slow motion does not stretch the edit.</summary>
    public class OpeningCinematic : MonoBehaviour
    {
        public static OpeningCinematic Instance { get; private set; }
        /// <summary>Survives scene reloads: the full opening plays once per launch.</summary>
        static bool playedFull;

        public string title = "LAST MAN STANDING";
        public string tagline = "ONE MAN.   ONE BUILDING.   NO WAY DOWN.";
        public string chapter = "CHAPTER 1";
        public string chapterName = "THE LOBBY";

        // --- read by the HUD (720-unit-high GUI space)
        /// <summary>True until the bars have gone and the HUD should come back.</summary>
        public bool Active { get; private set; } = true;
        /// <summary>True once the player has control (the bars may still be leaving).</summary>
        public bool ControlReleased { get; private set; }
        public float BarsPixels { get; private set; } = 360f;   // 360 = the screen is fully black
        public float TitleAlpha { get; private set; }
        public float CaptionAlpha { get; private set; }
        public float SkipAlpha { get; private set; }

        const float LetterboxBar = 92f;
        const float OutSeconds = 1.1f;

        HeroController hero;
        Vector3 home;
        bool full;
        float t;                         // real seconds since the start
        float cutB, dropAt, cStart, cLength, outStart = -1f;
        bool dropped, landed;
        float barsAtOut;
        Vector3 lastPos, lastLook;
        float lastFov;

        void Awake() { Instance = this; }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            var director = GetComponent<WaveDirector>();
            hero = director != null ? director.hero : FindAnyObjectByType<HeroController>();
            if (hero == null) { Finish(); return; }
            home = hero.transform.position;
            hero.SetFacing(-1);                 // facing into the room

            full = !playedFull;
            playedFull = true;
            if (full)
            {
                cutB = 3.4f;                    // establishing shot length
                dropAt = cutB + 0.35f;          // a beat of the empty floor, then the crash
                cStart = float.MaxValue;        // decided by the landing
                cLength = 2.5f;
                hero.CineHold(new Vector3(home.x, 6.45f, 0f));
            }
            else
            {
                cutB = dropAt = 0f;
                cStart = 0f;
                cLength = 1.5f;
                landed = dropped = true;
                hero.CineLock();
                Invoke(nameof(PlayTaunt), 0.05f);   // after the hero's own Start has set idle
            }
        }

        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        static float Fade(float x, float inA, float inB, float outA, float outB) =>
            Mathf.Min(Ease(inA, inB, x), 1f - Ease(outA, outB, x));

        void Update()
        {
            if (!Active || hero == null) return;
            t += Time.unscaledDeltaTime;

            var input = GestureInput.Instance;
            if (outStart < 0f && t > 0.4f && input != null && input.AnyPressThisFrame) Skip();

            // the hero crashes in
            if (full && !dropped && t >= dropAt)
            {
                dropped = true;
                hero.CineDrop();
                var fx = InkFx.Instance;
                if (fx != null)
                {
                    fx.Burst(new Vector3(home.x, 6.6f, 0f), 1.1f);
                    fx.Splash(new Vector3(home.x, 6.5f, 0f), Vector3.down, 10, new Color(0.75f, 0.75f, 0.75f));
                    fx.PlayHit(true);
                }
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.25f);
            }
            if (full && dropped && !landed && (hero.Grounded || t > dropAt + 2.5f)) Land();

            if (outStart < 0f && t >= cStart + cLength) BeginOut();

            Shoot();

            // bars: black screen, part to letterbox, slide away at the end
            if (outStart >= 0f)
            {
                BarsPixels = Mathf.Lerp(barsAtOut, 0f, Ease(outStart, outStart + OutSeconds * 0.9f, t));
                if (t >= outStart + OutSeconds) Finish();
            }
            else if (full) BarsPixels = Mathf.Lerp(360f, LetterboxBar, Ease(0.5f, 2.3f, t));
            else BarsPixels = Mathf.Lerp(360f, LetterboxBar, Ease(0f, 0.6f, t));

            bool going = outStart >= 0f;
            TitleAlpha = full && !going ? Fade(t, 1.0f, 1.7f, 2.8f, 3.3f) : 0f;
            CaptionAlpha = going ? Mathf.Max(0f, CaptionAlpha - Time.unscaledDeltaTime * 4f)
                                 : (t > cStart ? Fade(t, cStart + 0.25f, cStart + 0.8f, cStart + cLength - 0.2f, cStart + cLength + 0.4f) : 0f);
            SkipAlpha = full && !going ? Ease(0.8f, 1.4f, t) * 0.45f : 0f;
        }

        /// <summary>Camera for the current shot, blended back to gameplay on the way out.</summary>
        void Shoot()
        {
            var cam = CameraDirector.Instance;
            if (cam == null) return;
            Vector3 pos, look;
            float fov;
            Vector3 chest = hero.transform.position + Vector3.up * 0.75f;

            if (outStart >= 0f)
            {
                pos = lastPos; look = lastLook; fov = lastFov;
                cam.Scripted(pos, look, 1f - Ease(outStart, outStart + OutSeconds, t), fov);
                return;
            }
            if (full && t < cutB)
            {
                // A: establishing. A slow crane down and in across the whole black-and-white building.
                float u = Ease(0f, cutB, t);
                pos = Vector3.Lerp(new Vector3(-10.5f, 9.2f, -25f), new Vector3(-3.5f, 4.6f, -17.5f), u);
                look = Vector3.Lerp(new Vector3(0.5f, 4.4f, 1.5f), new Vector3(3.5f, 2.6f, 0.5f), u);
                fov = Mathf.Lerp(44f, 38f, u);
            }
            else if (t < cStart)
            {
                // B: floor level, looking up at the skylight; tilts down with the hero as he falls.
                float u = Ease(cutB, cutB + 2.4f, t);
                pos = new Vector3(home.x + 2.6f, 0.28f, -3.3f) + new Vector3(-0.35f, 0.15f, 0.25f) * u;
                Vector3 up = new Vector3(home.x, 6.2f, 0.4f);
                look = dropped ? Vector3.Lerp(up, chest + Vector3.up * 0.35f, Ease(dropAt, dropAt + 0.55f, t)) : up;
                fov = 46f;
            }
            else
            {
                // C: the hero, close, from the front-left: a slow push-in while he beckons.
                float u = Ease(cStart, cStart + cLength, t);
                var h = hero.transform.position;
                pos = h + Vector3.Lerp(new Vector3(-2.3f, 1.25f, -3.4f), new Vector3(-1.35f, 0.95f, -2.55f), u);
                look = chest + Vector3.up * 0.2f + Vector3.left * 0.15f;
                fov = Mathf.Lerp(34f, 30f, u);
            }
            lastPos = pos; lastLook = look; lastFov = fov;
            cam.Scripted(pos, look, 1f, fov);
        }

        void Land()
        {
            landed = true;
            cStart = t + 1.15f;
            var fx = InkFx.Instance;
            if (fx != null)
            {
                fx.Burst(hero.transform.position + Vector3.up * 0.05f, 1.4f);
                fx.ImpactFlash(0.06f);
                fx.PlayHit(true);
            }
            if (TimeController.Instance != null) TimeController.Instance.SlowMo(0.22f, 0.9f, 0.02f, 0.3f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.22f, 0.35f);
            foreach (var lamp in FindObjectsByType<LampFlicker>()) lamp.Shake(1f);
            hero.body.Play(hero.body.Has("duck") ? "duck" : "idle", 0.04f);   // the crouched landing
            Invoke(nameof(PlayTaunt), 0.7f);   // scaled time: rises out of the slow motion
        }

        void PlayTaunt()
        {
            if (hero == null || ControlReleased) return;
            hero.body.Play(hero.body.Has("taunt") ? "taunt" : "idle", 0.15f);
        }

        void Skip()
        {
            CancelInvoke();
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            BeginOut();
        }

        /// <summary>Give the player control and start sliding the bars away.</summary>
        void BeginOut()
        {
            if (outStart >= 0f) return;
            outStart = t;
            barsAtOut = BarsPixels;
            if (lastFov <= 0f) { lastPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero; lastLook = hero.Chest; lastFov = 32f; }
            ControlReleased = true;
            hero.CineRelease(home);
        }

        void Finish()
        {
            if (!ControlReleased && hero != null) hero.CineRelease(home);
            ControlReleased = true;
            Active = false;
            BarsPixels = 0f;
            TitleAlpha = CaptionAlpha = SkipAlpha = 0f;
            if (CameraDirector.Instance != null) CameraDirector.Instance.Scripted(Vector3.zero, Vector3.zero, 0f);
        }
    }
}
