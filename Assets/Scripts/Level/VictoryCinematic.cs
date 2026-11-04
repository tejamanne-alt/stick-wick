using UnityEngine;

namespace ProjectInk
{
    /// <summary>The floor-clear victory, cut like the last minute of an action film:
    ///  1. the final blow's slow-motion kill-cam plays out (or the boss finale);
    ///  2. THE WALK: the victory sting starts and the hero turns his back on the bodies and walks
    ///     away, the camera low in front of him, dollying back at his pace;
    ///  3. hard cut, THE HERO SHOT: a low angle arcing round him as he turns and beckons; CLEAR
    ///     slams in letter by letter over a red ink stroke, under FLOOR n;
    ///  4. THE TALLY: kills, score and time count up one after another, then the style grade is
    ///     stamped in red ink with a thud (and NEW HIGH SCORE if it is);
    ///  5. THE CRANE: up and back over the floor he has taken;
    ///  6. the bars slide away and play goes on. Tap to skip once the tally has started.
    /// All timing is real seconds. The hero is locked (no input) from the walk until the end.</summary>
    public class VictoryCinematic : MonoBehaviour
    {
        public static VictoryCinematic Instance { get; private set; }

        public bool Active { get; private set; }
        public bool Done { get; private set; } = true;
        public float BarsPixels { get; private set; }
        /// <summary>The hero shot has begun (the boss finale hands the camera over then).</summary>
        public bool HeroShotStarted => Active && shotStart >= 0f;
        /// <summary>The walk has begun: the victory owns the camera from here.</summary>
        public bool CameraTaken => Active && walkStart >= 0f;

        // --- read by the HUD: everything on the card is timed from CardT (seconds since CLEAR)
        public float CardT { get; private set; } = -1f;
        public float CardAlpha { get; private set; }
        public int FloorNo { get; private set; }
        public int Kills { get; private set; }
        public int Score { get; private set; }
        public float Seconds { get; private set; }
        public string Grade { get; private set; }
        public bool NewBest { get; private set; }

        // the tally's beats, in seconds after CLEAR lands (the HUD uses the same)
        public const float KillsAt = 0.75f, ScoreAt = 1.1f, TimeAt = 1.55f, GradeAt = 2.05f, BestAt = 2.5f, CountLen = 0.55f;

        const float Bars = 92f;
        const float WaitKill = 1.5f;        // let the kill-cam finish
        const float WalkLen = 2.1f;         // the walk away
        const float WalkSpeed = 1.3f;
        const float HeroShot = 3.7f;        // the low-angle arc, with the card and the tally
        const float TitleAt = 0.35f;        // CLEAR lands this far into the hero shot
        const float Crane = 1.5f;           // up and back
        const float Out = 1.0f;             // bars away, camera home

        HeroController hero;
        float t, walkStart = -1f, shotStart = -1f, outStart, waitKill = WaitKill, walkLen;
        bool locked, titled, cut, puppet;
        int cue;
        Vector3 anchor;                     // the hero's feet when the shot starts
        Vector3 walkFrom;
        int side, walkDir;
        float walkY;
        Vector3 lastPos, lastLook;
        float lastFov;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Start the sequence (called by the wave director as the floor is cleared).
        /// `afterBoss`: the boss finale has just played, so no kill-cam to wait for.</summary>
        public void Begin(HeroController h, int floor, string grade, int score, int kills, bool afterBoss = false)
        {
            hero = h;
            Active = true;
            Done = false;
            t = 0f;
            walkStart = shotStart = -1f;
            outStart = 0f;
            locked = titled = puppet = false;
            cut = afterBoss;
            waitKill = afterBoss ? 0.15f : WaitKill;
            cue = 0;
            CardT = -1f;
            CardAlpha = 0f;
            var wd = WaveDirector.Instance;
            FloorNo = floor;
            Grade = grade;
            Score = score;
            Kills = wd != null ? wd.FloorKills : kills;
            Seconds = wd != null ? wd.FloorSeconds : 0f;
            NewBest = wd != null && score > wd.BestScore;
        }

        static float Ease(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        void Update()
        {
            if (!Active || hero == null) return;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            var cam = CameraDirector.Instance;

            // bars in early, so the kill-cam already plays letterboxed (after the boss they are already in)
            BarsPixels = outStart <= 0f ? (cut ? Bars : Mathf.Lerp(0f, Bars, Ease(0.4f, 1.2f, t)))
                                        : Mathf.Lerp(Bars, 0f, Ease(outStart, outStart + Out * 0.9f, t));

            // 2. the walk starts once the kill-cam is over and he is on his feet
            if (walkStart < 0f)
            {
                if (t < waitKill) return;
                if (!hero.Grounded && t < waitKill + 1.5f) return;
                StartWalk();
            }

            // tap to skip, once the card is up
            var input = GestureInput.Instance;
            if (outStart <= 0f && CardT > 0.6f && input != null && input.AnyPressThisFrame) { BeginOut(); }

            Vector3 pos, look;
            float fov;
            float weight = 1f;
            if (shotStart < 0f)
            {
                float w = t - walkStart;
                if (w >= walkLen) { StartHeroShot(); return; }
                // he walks away from the bodies; the camera stays low in front of him, backing off at his pace
                float k = walkLen > 0.01f ? Mathf.Clamp01(w / walkLen) : 1f;
                float s = Mathf.Min(w, walkLen), brake = Mathf.Max(0f, s - (walkLen - 0.4f));
                float dist = WalkSpeed * (s - brake * brake / 0.8f);          // a steady pace, easing to a stop
                var p = walkFrom + Vector3.right * walkDir * dist;
                p.y = walkY;
                hero.CinePose(p);
                if (Mathf.Repeat(w, 0.48f) < dt && w < walkLen - 0.3f) Sound.Play(Sfx.Footstep, 1f, p, 0.92f);
                var chest = p + Vector3.up * 0.95f;
                var cp = p + new Vector3(walkDir * 3.3f, 0.45f, -1.5f);
                if (Mathf.Abs(cp.x) > 12.2f) { float over = Mathf.Abs(cp.x) - 12.2f; cp.x = Mathf.Sign(cp.x) * 12.2f; cp.z -= over * 1.2f; }
                pos = cp;
                look = chest + Vector3.up * 0.1f;
                fov = Mathf.Lerp(36f, 32f, k);
                if (!cut) weight = Ease(0f, 0.35f, w);
            }
            else
            {
                float u = t - shotStart;
                var chest = anchor + Vector3.up * 0.8f;
                if (outStart > 0f)
                {
                    // 6. hand back to the gameplay camera
                    float w = 1f - Ease(outStart, outStart + Out, t);
                    if (cam != null) cam.Scripted(lastPos, lastLook, w, lastFov);
                    CardAlpha = Mathf.MoveTowards(CardAlpha, 0f, dt * 3f);
                    if (t >= outStart + Out) Finish();
                    return;
                }
                if (u < HeroShot)
                {
                    // 3. low, close, arcing ~30 degrees round him while slowly rising: he looks tall
                    float k = Ease(0f, HeroShot, u);
                    float ang = Mathf.Lerp(-16f, 14f, k) * side;
                    var rot = Quaternion.Euler(0f, ang, 0f);
                    pos = chest + rot * new Vector3(0f, Mathf.Lerp(-0.45f, -0.2f, k), -Mathf.Lerp(3.1f, 2.6f, k));
                    look = chest + Vector3.up * 0.25f;
                    fov = Mathf.Lerp(34f, 30f, k);
                    if (!titled && u > TitleAt) LandTitle();
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
                    CardAlpha = Mathf.MoveTowards(CardAlpha, 0f, dt * 1.2f);
                }
                else { BeginOut(); return; }

                if (titled)
                {
                    CardT += dt;
                    if (u < HeroShot) CardAlpha = 1f;
                    TallyCues();
                }
            }
            lastPos = pos; lastLook = look; lastFov = fov;
            if (cam != null) cam.Scripted(pos, look, weight, fov);
        }

        void StartWalk()
        {
            walkStart = t;
            if (TimeController.Instance != null) TimeController.Instance.Clear();
            hero.CineLock();
            locked = true;
            Sound.Play(Sfx.FloorClear, 1f);                  // the sting: he can walk away now
            var p = hero.transform.position;
            walkFrom = p;
            walkY = p.y;
            // walk towards the side with more room on his floor
            float x0 = -12.2f, x1 = 12.2f;
            if (LevelNav.Active)
            {
                int pl = LevelNav.PlatformAt(p);
                if (pl >= 0 && LevelNav.RegionBounds(pl, out float a, out float b)) { x0 = Mathf.Max(x0, a + 0.5f); x1 = Mathf.Min(x1, b - 0.5f); }
            }
            float roomR = x1 - p.x, roomL = p.x - x0;
            walkDir = roomR >= roomL ? 1 : -1;
            float room = Mathf.Max(roomR, roomL);
            // the hall's floor is not flat everywhere (stairs, deck): there he does not walk
            walkLen = !LevelNav.Active ? 0f : Mathf.Min(WalkLen, (room - 0.3f + 0.26f) / WalkSpeed);
            if (walkLen < 0.8f) walkLen = 0f;
            if (walkLen > 0f)
            {
                puppet = true;
                hero.CinePuppet();
                hero.SetFacing(walkDir);
                // a slow, heavy stride: the run cycle, slowed right down
                if (hero.body != null) hero.body.Play("run", 0.25f, false, WalkSpeed / hero.runSpeed * 1.15f);
            }
        }

        void StartHeroShot()
        {
            shotStart = t;
            outStart = 0f;
            if (puppet) { puppet = false; hero.CineLock(); }        // physics back on, he stays put
            anchor = hero.transform.position;
            // arc from the side with more room in the building
            side = anchor.x > 0f ? -1 : 1;
            hero.SetFacing(side > 0 ? -1 : 1);      // turns to face into the camera's arc
            if (hero.body != null) hero.body.Play(hero.body.Has("taunt") ? "taunt" : "idle", 0.15f);
            Sound.Play(Sfx.WhooshBig, 0.6f);
            Sound.Play(Sfx.Cloth, 0.8f, anchor);
        }

        void LandTitle()
        {
            titled = true;
            CardT = 0f;
            CardAlpha = 1f;
            if (InkFx.Instance != null) InkFx.Instance.ImpactFlash(0.05f);
            if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.06f, 0.15f);
            Sound.Play(Sfx.CineMassive, 0.9f);
            Sound.Play(Sfx.SwordSwing, 0.7f, null, 0.9f);
        }

        /// <summary>The sounds of the tally: a thud as each column lands, ticks while it counts,
        /// a heavy stamp for the grade.</summary>
        void TallyCues()
        {
            float[] at = { KillsAt, ScoreAt, TimeAt, GradeAt, BestAt };
            while (cue < at.Length && CardT >= at[cue])
            {
                switch (cue)
                {
                    case 0: case 1: case 2: Sound.Play(Sfx.CineHit, 0.35f, null, 1.1f); break;
                    case 3:
                        Sound.Play(Sfx.PunchHeavy, 1f, null, 0.75f);
                        Sound.Play(Sfx.Boom, 0.7f);
                        if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(0.12f, 0.2f);
                        break;
                    case 4: if (NewBest) Sound.Play(Sfx.RankUp, 1f); break;
                }
                cue++;
            }
            // ticks while a number is counting
            foreach (float a in new[] { KillsAt, ScoreAt, TimeAt })
                if (CardT > a && CardT < a + CountLen && Mathf.Repeat(CardT - a, 0.06f) < Time.unscaledDeltaTime)
                    Sound.Play(Sfx.Shell, 0.25f, null, 1.5f);
        }

        void BeginOut()
        {
            if (outStart > 0f) return;
            outStart = t;
            if (locked) { hero.CineRelease(); locked = false; }
        }

        void Finish()
        {
            Active = false;
            Done = true;
            BarsPixels = 0f;
            CardT = -1f;
            CardAlpha = 0f;
            if (locked && hero != null) { hero.CineRelease(); locked = false; }
            if (CameraDirector.Instance != null) CameraDirector.Instance.Scripted(Vector3.zero, Vector3.zero, 0f);
        }

        /// <summary>Stop at once (a restart).</summary>
        public void Abort() { if (Active) Finish(); }
    }
}
