using UnityEngine;

namespace ProjectInk
{
    /// <summary>The floor-clear victory, shot like the end of a fight in a film:
    ///  1. the final blow's slow-motion kill-cam plays out;
    ///  2. letterbox bars slide in and the gameplay HUD steps aside;
    ///  3. HERO SHOT: a low angle that arcs slowly round the hero while he beckons, looking up;
    ///  4. the title card, FLOOR n CLEAR, lands with a flash and a cinematic hit, with the style
    ///     grade, score and kills beneath it, over the victory sting;
    ///  5. the camera cranes up and back over the floor he has taken;
    ///  6. the bars slide away and play goes on to the next chapter.
    /// All timing is real seconds. The hero is locked (no input) from the hero shot until the end.</summary>
    public class VictoryCinematic : MonoBehaviour
    {
        public static VictoryCinematic Instance { get; private set; }

        public bool Active { get; private set; }
        public bool Done { get; private set; } = true;
        public float BarsPixels { get; private set; }
        public float TitleAlpha { get; private set; }
        public float StatsAlpha { get; private set; }
        public string Title { get; private set; }
        public string Stats { get; private set; }
        /// <summary>The hero shot has begun (the boss finale hands the camera over then).</summary>
        public bool HeroShotStarted => Active && shotStart >= 0f;

        const float Bars = 92f;
        const float WaitKill = 1.5f;        // let the kill-cam finish
        const float HeroShot = 2.6f;        // the low-angle arc
        const float Crane = 1.5f;           // up and back
        const float Out = 1.0f;             // bars away, camera home

        HeroController hero;
        float t, shotStart = -1f, outStart;
        bool locked, titled, cut;
        float waitKill = WaitKill;
        Vector3 anchor;                     // the hero's feet when the shot starts
        int side;                           // which side the camera arcs from
        Vector3 lastPos, lastLook;
        float lastFov;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Start the sequence (called by the wave director as the floor is cleared).</summary>
        /// <summary>`afterBoss`: the boss finale has just played (no kill-cam to wait for), so cut
        /// straight to the hero shot.</summary>
        public void Begin(HeroController h, int floor, string grade, int score, int kills, bool afterBoss = false)
        {
            cut = afterBoss;
            waitKill = afterBoss ? 0.15f : WaitKill;
            hero = h;
            Active = true;
            Done = false;
            t = 0f;
            shotStart = -1f;
            locked = titled = false;
            Title = $"FLOOR {floor} CLEAR";
            Stats = $"STYLE  {grade}     ·     {score:N0}     ·     {kills} KILLS";
            TitleAlpha = StatsAlpha = 0f;
        }

        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        void Update()
        {
            if (!Active || hero == null) return;
            t += Time.unscaledDeltaTime;
            var cam = CameraDirector.Instance;

            // bars in early, so the kill-cam already plays letterboxed
            BarsPixels = shotStart < 0f || t < outStart || outStart <= 0f
                ? (cut ? Bars : Mathf.Lerp(0f, Bars, Ease(0.4f, 1.2f, t)))
                : Mathf.Lerp(Bars, 0f, Ease(outStart, outStart + Out * 0.9f, t));

            // 3. the hero shot starts once the kill-cam is over and he is on his feet
            if (shotStart < 0f)
            {
                if (t < waitKill) return;
                if (!hero.Grounded && t < waitKill + 1.5f) return;
                StartHeroShot();
            }

            float u = t - shotStart;
            Vector3 pos, look;
            float fov;
            var chest = anchor + Vector3.up * 0.8f;
            if (u < HeroShot)
            {
                // low, close, arcing ~30 degrees round him while slowly rising: the hero looks tall
                float k = Ease(0f, HeroShot, u);
                float ang = Mathf.Lerp(-16f, 14f, k) * side;
                var rot = Quaternion.Euler(0f, ang, 0f);
                pos = chest + rot * new Vector3(0f, Mathf.Lerp(-0.45f, -0.2f, k), -Mathf.Lerp(3.1f, 2.6f, k));
                look = chest + Vector3.up * 0.25f;
                fov = Mathf.Lerp(34f, 30f, k);
                if (!titled && u > 0.5f) LandTitle();
            }
            else if (u < HeroShot + Crane)
            {
                // 5. crane up and back over the floor
                float k = Ease(HeroShot, HeroShot + Crane, u);
                var start = chest + Quaternion.Euler(0f, 14f * side, 0f) * new Vector3(0f, -0.2f, -2.6f);
                var end = chest + new Vector3(-1.5f * side, 4.5f, -13f);
                pos = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                look = Vector3.Lerp(chest + Vector3.up * 0.25f, chest + Vector3.up * 0.8f, k);
                fov = Mathf.Lerp(30f, 36f, k);
            }
            else
            {
                // 6. hand back to the gameplay camera
                if (outStart <= 0f)
                {
                    outStart = t;
                    if (locked) { hero.CineRelease(); locked = false; }
                }
                float w = 1f - Ease(outStart, outStart + Out, t);
                if (cam != null) cam.Scripted(lastPos, lastLook, w, lastFov);
                TitleAlpha = Mathf.MoveTowards(TitleAlpha, 0f, Time.unscaledDeltaTime * 3f);
                StatsAlpha = Mathf.MoveTowards(StatsAlpha, 0f, Time.unscaledDeltaTime * 3f);
                if (t >= outStart + Out) Finish();
                return;
            }
            lastPos = pos; lastLook = look; lastFov = fov;
            if (cam != null) cam.Scripted(pos, look, cut ? 1f : Ease(0f, 0.35f, u), fov);

            // the card fades in under the title, then holds
            TitleAlpha = titled ? Mathf.MoveTowards(TitleAlpha, 1f, Time.unscaledDeltaTime * 4f) : 0f;
            StatsAlpha = titled && u > 1.0f ? Mathf.MoveTowards(StatsAlpha, 1f, Time.unscaledDeltaTime * 2.5f) : StatsAlpha;
        }

        void StartHeroShot()
        {
            shotStart = t;
            outStart = 0f;
            anchor = hero.transform.position;
            // arc from the side with more room in the building
            side = anchor.x > 0f ? -1 : 1;
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            hero.CineLock();
            locked = true;
            hero.SetFacing(side > 0 ? -1 : 1);      // face into the camera's arc
            if (hero.body != null) hero.body.Play(hero.body.Has("taunt") ? "taunt" : "idle", 0.15f);
            Sound.Play(Sfx.WhooshBig, 0.6f);
        }

        void LandTitle()
        {
            titled = true;
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.05f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.06f, 0.15f);
            Sound.Play(Sfx.CineMassive, 0.9f);
        }

        void Finish()
        {
            Active = false;
            Done = true;
            BarsPixels = 0f;
            TitleAlpha = StatsAlpha = 0f;
            if (locked && hero != null) { hero.CineRelease(); locked = false; }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Scripted(Vector3.zero, Vector3.zero, 0f);
        }

        /// <summary>Stop at once (a restart).</summary>
        public void Abort() { if (Active) Finish(); }
    }
}
