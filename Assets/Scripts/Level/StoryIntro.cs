using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>STORY MODE, THE PROLOGUE: he wakes in a warehouse with no memory.
    ///
    ///  SHOT 0  The Desolate Yard: wide and high over the container yard in the storm, the shed in
    ///          the middle, the city far off under its haze. A slow push-in; lightning (two white
    ///          frames, a blue afterglow) lights the crack in the roof. THE YARDS · 3:12 AM.
    ///  SHOT 1  The Roof Fracture: straight up into the dark girders. Storm water gathers at the
    ///          crack into one drop, which lets go and falls at the lens. The storm goes muffled.
    ///  SHOT 2  The Spark: his eyes. Heavy blinks behind lids, the beams out of focus, tinnitus.
    ///          SPLAT between the eyes: the lens jolts, the eyes shut and snap open, sharp.
    ///  SHOT 3  The Fractured Reflection: he has rolled onto his hands and knees over a puddle.
    ///          Ripples; lightning; in the flash the water shows his face (white eye slits, a white
    ///          scar at the temple), then a drop shatters it into slivers. He pushes up.
    ///  SHOT 4  The Lost Identity: side profile, a Dutch angle, tilting up as he rises to one knee,
    ///          then stands. He raises his hands, turns them, closes them into fists (heartbeat,
    ///          the knuckles crack). He touches the scar: the picture tears, static, then half a
    ///          second of black noise and reversed whispers. Back.
    ///  SHOT 5  Total Isolation: high in the rafters, looking down. A small black figure in a dead
    ///          hall, turning slowly all the way round. Wind in the vents, drips.
    ///  SHOT 6  The Path to the Shutter: low behind him as he walks towards the moonlight under the
    ///          roll-up door; the camera swings round into the side-on fight view and the bars open.
    ///  PLAY    HOLD to walk. Five metres from the door, shadows cross the light, a latch clanks
    ///          outside, the cello swells. The shutter flies up: three men. "Wait... who are you?"
    ///          "He's awake. Finish it." The fight. When the last one falls, a memory breaks through.
    /// All cinematic timing is in real seconds. Tap SKIP to jump to the walk (and after a defeat,
    /// the retry starts at the door).</summary>
    public class StoryIntro : MonoBehaviour
    {
        public static StoryIntro Instance { get; private set; }
        /// <summary>Survives the reload after a defeat: start again at the door, not the wake-up.</summary>
        public static bool Retry;

        public HeroController hero;
        public WaveDirector director;

        enum Phase { Cinematic, Walk, Breach, Fight, Memory, End }
        Phase phase = Phase.Cinematic;

        /// <summary>The fight HUD steps aside while the story holds the screen.</summary>
        public bool HideHud => phase == Phase.Cinematic || phase == Phase.Memory || phase == Phase.End || (phase == Phase.Breach && b < ReleaseAt);

        // the edit (real seconds)
        const float S1 = 4.2f, S2 = 6.8f, S3 = 10.8f, S4 = 15.6f, S5 = 22.2f, S6 = 27.0f, Control = 30.6f;
        const float Bar = 92f, ReleaseAt = 4.6f, BreachAt = 1.9f;
        const float ShutterX = 12.0f, DoorL = 10.4f, DoorR = 13.6f, RoofY = 7.2f;

        float t, prevT, b, prevB, m, prevM;
        bool setUp;
        Vector3 home;
        float runSpeed;
        CameraDirector cam;
        Transform head, crack, crackTop, shutter, slitShadow0, slitShadow1;
        Renderer crackR, crackTopR, slit, flood, gap, drop0R, drop1R;
        Transform drop0, drop1;
        Color crackColor;
        float shutterY;
        Enemy[] men = new Enemy[3];
        int spawned;
        float blueUntil, blueStrength, splatAt = -9f, glitchUntil, clearedAt = -1f;
        int flashFrames;
        float barsPx = Bar, promptAlpha, chipUntil, footAt;
        string subSpeaker, subLine;
        float subUntil;
        float walkFromX;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            cam = CameraDirector.Instance;
            if (hero == null) hero = FindAnyObjectByType<HeroController>();
            if (director == null) director = WaveDirector.Instance;
            head = hero.body != null ? hero.body.Bone("head") : null;
            crack = Find("RoofCrack"); crackTop = Find("RoofCrackTop");
            crackR = crack != null ? crack.GetComponent<Renderer>() : null;
            crackTopR = crackTop != null ? crackTop.GetComponent<Renderer>() : null;
            if (crackR != null) crackColor = crackR.sharedMaterial.color;
            shutter = Find("Shutter"); if (shutter != null) shutterY = shutter.position.y;
            slit = R("MoonSlit"); flood = R("MoonFlood"); gap = R("MoonGap");
            slitShadow0 = Find("SlitShadow0"); slitShadow1 = Find("SlitShadow1");
            drop0 = Find("Droplet0"); drop1 = Find("Droplet1");
            drop0R = drop0 != null ? drop0.GetComponent<Renderer>() : null;
            drop1R = drop1 != null ? drop1.GetComponent<Renderer>() : null;
            home = hero.transform.position;
            runSpeed = hero.runSpeed;
            StoryCameraFX.Set(0f, 0f);
            Sound.Ambience(2.4f, 0.2f, 22000f);
        }

        static Transform Find(string n) { var g = GameObject.Find(n); return g != null ? g.transform : null; }
        static Renderer R(string n) { var g = GameObject.Find(n); return g != null ? g.GetComponent<Renderer>() : null; }

        static float Ease(float a, float b2, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b2, x));
        bool At(float time) => prevT < time && t >= time;
        bool AtB(float time) => prevB < time && b >= time;
        bool AtM(float time) => prevM < time && m >= time;

        Vector3 HeadC() => head != null ? head.position + head.up * 0.19f : hero.transform.position + Vector3.up * 1.2f;

        static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        // ================================================================== the loop

        void Update()
        {
            if (hero == null) return;
            if (!setUp)
            {
                setUp = true;
                if (Retry) { BeginAtDoor(); }
                else
                {
                    hero.CinePuppet();
                    hero.SetFacing(1);
                    hero.CinePose(home);
                    if (hero.body.Has("lie")) hero.body.Play("lie", 0f);
                    // the crack sits right over his face
                    var hc = HeadC();
                    foreach (var c in new[] { crack, crackTop })
                        if (c != null) c.position += new Vector3(hc.x - (home.x - 0.6f), 0f, 0f);
                }
            }
            float dt = Time.unscaledDeltaTime;
            switch (phase)
            {
                case Phase.Cinematic:
                    prevT = t; t += dt;
                    Cinematic();
                    break;
                case Phase.Walk: UpdateWalk(); break;
                case Phase.Breach:
                    prevB = b; b += dt;
                    UpdateBreach();
                    break;
                case Phase.Fight: UpdateFight(); break;
                case Phase.Memory:
                    prevM = m; m += dt;
                    UpdateMemory();
                    break;
                case Phase.End:
                    barsPx = Mathf.MoveTowards(barsPx, 0f, dt * 200f);
                    break;
            }
            if (crackR != null) crackR.material.color = Time.unscaledTime < blueUntil ? Color.Lerp(crackColor, Color.white, 0.8f) : crackColor;
            if (crackTopR != null) crackTopR.material.color = Time.unscaledTime < blueUntil ? Color.white : crackColor;
            if (Time.unscaledTime > glitchUntil && StoryCameraFX.Instance != null && StoryCameraFX.Instance.Glitch > 0f && !(phase == Phase.Cinematic && t > S4 + 5.15f && t < S4 + 5.65f))
                StoryCameraFX.Set(StoryCameraFX.Instance.Blur, 0f);
        }

        void Lightning(float strength, float thunderVol, float thunderDelay)
        {
            flashFrames = strength > 0.6f ? 2 : 0;
            blueStrength = strength;
            blueUntil = Time.unscaledTime + 0.45f;
            if (thunderVol > 0f)
            {
                if (thunderDelay <= 0f) Sound.Play(Sfx.Thunder, thunderVol);
                else Sound.PlayLater(Sfx.Thunder, thunderDelay, thunderVol);
            }
            if (strength > 0.6f) Buzz();
        }

        void Glitch(float amount, float seconds)
        {
            glitchUntil = Time.unscaledTime + seconds;
            StoryCameraFX.Set(StoryCameraFX.Instance != null ? StoryCameraFX.Instance.Blur : 0f, amount);
        }

        // ================================================================== the cinematic

        void Cinematic()
        {
            if (cam == null) return;
            Vector3 pos, look;
            float fov, roll = 0f;
            var hc = HeadC();

            if (t < S1)
            {
                // SHOT 0: the desolate yard
                if (At(0.05f)) Sound.Play(Sfx.Wind, 0.9f);
                if (At(2.3f)) Lightning(1f, 1f, 0.08f);
                pos = new Vector3(-10f, 18f, 40f);                         // high over the yard, down the road to the shed
                look = new Vector3(-2f, 3f, -60f);
                fov = Mathf.Lerp(40f, 39.2f, Ease(0f, S1, t));            // a slow 2% push-in
            }
            else if (t < S2)
            {
                // SHOT 1: the roof fracture
                if (At(S1)) { Sound.Ambience(0.9f, 0.55f, 800f); }
                if (At(S1 + 0.3f)) Sound.Play(Sfx.Drip, 0.35f, null, 0.9f);
                if (At(S1 + 1.4f)) Sound.Play(Sfx.Whoosh, 0.8f, null, 1.7f);
                pos = hc + new Vector3(0f, 0.35f, 0f);
                look = new Vector3(hc.x, RoofY, hc.z + 0.25f);
                fov = 24f;
                PoseDrop(drop0, drop0R, hc, S1 + 0.2f, S1 + 1.4f, 9.8f, S2);
            }
            else if (t < S3)
            {
                // SHOT 2: the spark of consciousness (his eyes)
                if (At(S2 + 0.05f)) Sound.Play(Sfx.Tinnitus, 1f);
                if (At(S2 + 0.8f)) Sound.Play(Sfx.Heartbeat, 0.5f);
                if (At(S2 + 1.5f)) Lightning(0.3f, 0.4f, 0.6f);
                if (At(10.2f))
                {
                    splatAt = Time.unscaledTime;
                    Sound.Play(Sfx.Splat, 1f);
                    cam.Shake(0.2f, 0.25f);
                    Buzz();
                    if (drop1R != null) drop1R.enabled = false;
                }
                if (drop0R != null) drop0R.enabled = false;
                pos = hc + new Vector3(0f, 0.1f, 0f);
                look = new Vector3(hc.x + 0.5f, RoofY, hc.z + 0.2f);
                fov = 64f;
                if (t < 10.2f) PoseDrop(drop1, drop1R, hc, 9.5f, 9.5f, 26.5f, 10.2f);
                float blur = t < 10.36f ? Mathf.Lerp(1f, 0.72f, Ease(S2, 10.2f, t)) : 0f;
                StoryCameraFX.Set(blur, StoryCameraFX.Instance != null ? StoryCameraFX.Instance.Glitch : 0f);
            }
            else if (t < S4)
            {
                // SHOT 3: the fractured reflection (drawn over the screen); he is on hands and knees now
                if (At(S3))
                {
                    StoryCameraFX.Set(0f, 0f);
                    if (hero.body.Has("allfours")) hero.body.Play("allfours", 0.05f);
                }
                if (At(S3 + 0.15f)) Sound.Play(Sfx.Breath, 0.9f);
                if (At(S3 + 0.6f)) Sound.Play(Sfx.HeroEffort, 0.9f, null, 0.72f);       // a groan
                if (At(S3 + 1.2f)) Lightning(1f, 0.65f, 0.25f);
                if (At(S3 + 1.7f)) Sound.Play(Sfx.Drip, 0.9f, null, 0.8f);
                if (At(S3 + 3.7f)) { Sound.Play(Sfx.Splash, 0.9f); Sound.Play(Sfx.Cloth, 0.8f); }
                if (At(S3 + 4.1f)) Sound.Play(Sfx.Breath, 0.8f, null, 0.9f);
                pos = hc + new Vector3(0f, 0.6f, -0.1f);
                look = hc;
                fov = 50f;
            }
            else if (t < S5)
            {
                // SHOT 4: the lost identity (side profile, Dutch angle)
                if (At(S4)) { if (hero.body.Has("rise")) hero.body.Play("rise", 0.08f, true, 0.9f); Sound.Play(Sfx.HeroEffort, 0.8f, null, 0.8f); }
                if (At(S4 + 0.8f)) Sound.Play(Sfx.Cloth, 0.7f);
                if (At(S4 + 2.5f)) { if (hero.body.Has("look_hands")) hero.body.Play("look_hands", 0.15f); }
                if (At(S4 + 2.8f)) Sound.Play(Sfx.Heartbeat, 0.9f);
                if (At(S4 + 3.6f)) Sound.Play(Sfx.Heartbeat, 1f);
                if (At(S4 + 3.8f)) Sound.Play(Sfx.Bone, 0.35f, null, 1.5f);            // the knuckles crack
                if (At(S4 + 4.5f)) { if (hero.body.Has("touch_temple")) hero.body.Play("touch_temple", 0.12f); }
                if (At(S4 + 4.9f)) { Sound.Play(Sfx.Static, 1f); Glitch(1f, 0.07f); }   // touching the wound
                if (At(S4 + 5.15f)) { Sound.Play(Sfx.Whispers, 1f); Sound.Play(Sfx.Static, 0.5f, null, 0.7f); }
                if (At(S4 + 5.65f)) { Sound.Play(Sfx.Heartbeat, 1f); Glitch(0.6f, 0.05f); }
                if (At(S4 + 6.0f)) Sound.Play(Sfx.Breath, 0.7f);
                look = new Vector3(hc.x + 0.15f, Mathf.Max(0.45f, hc.y - 0.15f), 0f);
                pos = look + new Vector3(0.6f, -0.12f, -2.7f);
                fov = 33f;
                roll = 7f;
            }
            else if (t < S6)
            {
                // SHOT 5: total isolation, from the rafters
                if (At(S5)) { if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f); Sound.Play(Sfx.Wind, 1f); Sound.Ambience(0.55f, 0.3f, 700f); }
                foreach (var dd in new[] { 0.7f, 1.9f, 3.2f, 4.1f })
                    if (At(S5 + dd)) Sound.Play(Sfx.Drip, Random.Range(0.4f, 0.7f), new Vector3(home.x + Random.Range(-6f, 6f), 0f, 0f), Random.Range(0.8f, 1.15f));
                float k = Ease(S5 + 0.4f, S6 - 0.4f, t);
                hero.transform.rotation = Quaternion.Euler(0f, 360f * k, 0f);           // a slow turn, all the way round
                pos = new Vector3(home.x + 3.0f, 6.2f, -2.4f);
                look = new Vector3(home.x, 0.5f, 0.4f);
                fov = 52f;
            }
            else
            {
                // SHOT 6: the path to the shutter, then the hand-off to play
                if (At(S6)) { hero.SetFacing(1); if (hero.body.Has("walk")) hero.body.Play("walk", 0.25f, false, 1.7f); }
                if (At(S6 + 0.2f)) Sound.Play(Sfx.Drone, 1f);
                float wx = home.x + 1.3f * Mathf.Max(0f, t - (S6 + 0.2f));
                hero.CinePose(new Vector3(wx, home.y, 0f));
                if (t - (S6 + 0.2f) > 0f && Time.unscaledTime > footAt)
                {
                    footAt = Time.unscaledTime + 0.45f;
                    Sound.Play(Sfx.Splash, 0.8f, hero.transform.position);
                    Sound.Play(Sfx.Footstep, 0.9f, hero.transform.position, 0.85f);
                }
                var h = hero.transform.position;
                float a = Ease(S6, Control - 0.4f, t);
                float th = Mathf.Lerp(180f, 270f, a) * Mathf.Deg2Rad;
                float rad = Mathf.Lerp(2.0f, 10.5f, a);
                pos = h + new Vector3(Mathf.Cos(th) * rad, Mathf.Lerp(0.35f, 1.7f, a), Mathf.Sin(th) * rad);
                look = h + new Vector3(Mathf.Lerp(3.0f, 0.3f, a), Mathf.Lerp(0.9f, 1.0f, a), 0f);
                fov = Mathf.Lerp(40f, 32f, a);
                float wgt = 1f - Ease(Control - 1.0f, Control, t);
                barsPx = Mathf.Lerp(Bar, 0f, Ease(Control - 1.0f, Control, t));
                cam.ScriptRoll = 0f;
                cam.Scripted(pos, look, wgt, fov);
                if (t >= Control) HandOff();
                return;
            }
            barsPx = Bar;
            cam.ScriptRoll = roll;
            cam.Scripted(pos, look, 1f, fov);
        }

        /// <summary>A drop forms at the crack (from `form` it swells), lets go at `release`
        /// and falls with acceleration `g`, hidden after `hide`.</summary>
        void PoseDrop(Transform d, Renderer r, Vector3 hc, float form, float release, float g, float hide)
        {
            if (d == null || r == null) return;
            r.enabled = t >= form && t < hide;
            if (!r.enabled) return;
            float s = Mathf.Lerp(0.015f, 0.05f, Ease(form, release, t));
            if (t < release)
            {
                float stretch = 1f + 0.4f * Ease(release - 0.3f, release, t);
                d.position = new Vector3(hc.x, RoofY - 0.02f - s * 0.5f * stretch, hc.z);
                d.localScale = new Vector3(s, s * stretch, s);
            }
            else
            {
                float u = t - release;
                d.position = new Vector3(hc.x, RoofY - 0.05f - 0.5f * g * u * u, hc.z);
                d.localScale = new Vector3(s * 0.9f, s * 1.5f, s * 0.9f);           // streaking as it falls
            }
        }

        void HandOff()
        {
            phase = Phase.Walk;
            PlayerPrefs.SetInt("story_intro_seen", 1);
            PlayerPrefs.Save();
            StoryCameraFX.Set(0f, 0f);
            if (cam != null) { cam.ScriptRoll = 0f; cam.Scripted(Vector3.zero, Vector3.zero, 0f); }
            barsPx = 0f;
            if (drop0R != null) drop0R.enabled = false;
            if (drop1R != null) drop1R.enabled = false;
            hero.SetFacing(1);
            hero.CineRelease();
            hero.runSpeed = runSpeed * 0.5f;            // he walks, he does not run, until they come
            walkFromX = hero.transform.position.x;
            promptAlpha = 0f;
            Sound.Ambience(0.7f, 0.4f, 900f);
        }

        /// <summary>SKIP, or a retry after a defeat: straight to the walk.</summary>
        void SkipToWalk()
        {
            hero.transform.rotation = Quaternion.identity;
            hero.SetFacing(1);
            hero.CinePose(new Vector3(home.x + 4.2f, home.y, 0f));
            HandOff();
        }

        void BeginAtDoor()
        {
            hero.CinePose(new Vector3(ShutterX - 6.8f, home.y, 0f));
            HandOff();
        }

        // ================================================================== play: the walk, the breach, the fight

        void UpdateWalk()
        {
            float moved = Mathf.Abs(hero.transform.position.x - walkFromX);
            float want = moved < 1.5f ? 1f : 0f;
            promptAlpha = Mathf.MoveTowards(promptAlpha, want, Time.unscaledDeltaTime * (want > 0f ? 1.2f : 2.5f));
            Splashes();
            if (hero.transform.position.x > ShutterX - 5f) { phase = Phase.Breach; b = prevB = 0f; }
        }

        /// <summary>Footsteps in the shallow water where the puddles are.</summary>
        void Splashes()
        {
            if (!hero.Grounded || Time.unscaledTime < footAt) return;
            float x = hero.transform.position.x;
            bool wet = Mathf.Abs(x + 8.7f) < 0.7f || Mathf.Abs(x + 3.6f) < 0.8f || Mathf.Abs(x - 1.2f) < 0.6f || Mathf.Abs(x - 6.5f) < 0.9f || Mathf.Abs(x - 9.8f) < 0.5f;
            if (!wet) return;
            footAt = Time.unscaledTime + 0.32f;
            Sound.Play(Sfx.Splash, 0.7f, hero.transform.position);
        }

        void UpdateBreach()
        {
            var tc = TimeController.Instance;
            if (AtB(0.0001f))
            {
                Retry = true;
                hero.CineLock();
                hero.SetFacing(1);
                hero.runSpeed = runSpeed;
                Sound.Tension(true);
                promptAlpha = 0f;
            }
            // shadows cross the slit of light, footsteps outside
            MoveShadow(slitShadow0, 0.2f, 1.3f, DoorL - 0.6f, DoorR + 0.6f);
            MoveShadow(slitShadow1, 0.55f, 1.65f, DoorR + 0.6f, DoorL - 0.4f);
            foreach (var fs in new[] { 0.3f, 0.65f, 1.0f, 1.4f })
                if (AtB(fs)) Sound.Play(Sfx.Footstep, 1f, new Vector3(ShutterX, 0f, 3f), 0.8f);
            if (AtB(0.9f)) Sound.Play(Sfx.Latch, 1f, new Vector3(ShutterX, 1f, 3f));
            if (AtB(1.25f)) Sound.Play(Sfx.Latch, 0.8f, new Vector3(ShutterX, 1f, 3f), 0.9f);
            if (shutter != null && b < BreachAt)
            {
                float rattle = b > 1.0f ? Mathf.Sin(b * 70f) * 0.02f : 0f;
                shutter.position = new Vector3(shutter.position.x, shutterY + rattle, shutter.position.z);
            }
            if (AtB(BreachAt))
            {
                // THE BREACH
                Sound.Play(Sfx.MetalClang, 1f, new Vector3(ShutterX, 2f, 3f));
                Sound.Play(Sfx.Door, 1f, new Vector3(ShutterX, 1f, 3f));
                Sound.Play(Sfx.Boom, 0.7f);
                Lightning(0.5f, 0.6f, 0.1f);
                Buzz();
                if (cam != null) cam.Shake(0.3f, 0.3f);
                if (tc != null) tc.SlowMo(0.35f, 1.0f, 0.02f, 0.3f);
                if (slit != null) slit.enabled = false;
                if (gap != null) gap.enabled = false;
                if (flood != null) flood.enabled = true;
            }
            if (shutter != null && b >= BreachAt)
            {
                float k = Ease(BreachAt, BreachAt + 0.25f, b);
                shutter.position = new Vector3(shutter.position.x, shutterY + 3.6f * k, shutter.position.z);
                if (k >= 0.999f) foreach (var r in shutter.GetComponentsInChildren<Renderer>()) r.enabled = false;   // rolled away into its drum
            }
            for (int i = 0; i < men.Length; i++)
                if (spawned == i && b >= BreachAt + 0.05f + i * 0.28f && director != null)
                {
                    men[i] = director.SpawnScripted(EnemyType.Brawler, SpawnKind.Background);
                    if (men[i] != null) men[i].Passive = true;             // no swings until the scene lets go
                    spawned++;
                }
            if (AtB(2.7f)) Say("HIM", "Wait... who are you?", 1.25f);
            if (AtB(4.0f)) Say("THUG", "He's awake. Finish it.", 1.5f);

            // the camera: hero and door together, then back to the fight view
            if (cam != null)
            {
                float wIn = Ease(0f, 0.6f, b), wOut = 1f - Ease(ReleaseAt, ReleaseAt + 0.6f, b);
                var hp = hero.transform.position;
                cam.Scripted(new Vector3(hp.x - 1.5f, 1.35f, -5.4f), new Vector3(ShutterX - 1.2f, 1.1f, 1.6f), Mathf.Min(wIn, wOut), 38f);
            }
            barsPx = b < ReleaseAt ? Mathf.Lerp(0f, 70f, Ease(0f, 0.5f, b)) : Mathf.Lerp(70f, 0f, Ease(ReleaseAt, ReleaseAt + 0.6f, b));
            if (AtB(ReleaseAt))
            {
                foreach (var e in men) if (e != null) e.Passive = false;
                hero.CineRelease();
                chipUntil = Time.unscaledTime + 7f;
            }
            if (b > ReleaseAt + 0.65f)
            {
                if (cam != null) cam.Scripted(Vector3.zero, Vector3.zero, 0f);
                barsPx = 0f;
                phase = Phase.Fight;
            }
        }

        void MoveShadow(Transform s, float a, float b2, float x0, float x1)
        {
            if (s == null) return;
            var r = s.GetComponent<Renderer>();
            bool on = b >= a && b <= b2;
            if (r != null) r.enabled = on;
            if (on) s.position = new Vector3(Mathf.Lerp(x0, x1, Mathf.InverseLerp(a, b2, b)), 0f, 0f);
        }

        void UpdateFight()
        {
            if (hero.IsDead) return;                     // the director shows K.O.; a tap retries at the door
            bool allDown = spawned >= men.Length;
            foreach (var e in men) if (e != null && e.Alive) allDown = false;
            if (allDown && clearedAt < 0f) clearedAt = Time.unscaledTime;
            if (clearedAt > 0f && Time.unscaledTime - clearedAt > 1.8f)
            {
                phase = Phase.Memory;
                m = prevM = 0f;
                hero.CineLock();
                Sound.Tension(false);
            }
        }

        void UpdateMemory()
        {
            if (AtM(0.01f)) { Sound.Play(Sfx.Static, 1f); Sound.Play(Sfx.Whispers, 1f); Glitch(1f, 0.18f); Buzz(); }
            if (AtM(0.4f)) Sound.Play(Sfx.Heartbeat, 1f);
            if (AtM(1.25f)) Sound.Play(Sfx.Heartbeat, 1f);
            if (AtM(1.3f)) Sound.Play(Sfx.CineMassive, 0.7f);
            if (AtM(3.2f))
            {
                phase = Phase.End;
                Retry = false;
                Sound.Play(Sfx.CineHit, 0.7f);
            }
        }

        void Say(string who, string line, float seconds)
        {
            subSpeaker = who;
            subLine = line;
            subUntil = Time.unscaledTime + seconds;
        }

        // ================================================================== the picture on top

        GUIStyle cap, sub, subWho, big, prompt, btn;
        Texture2D disc, circle, ring, rain, noise, lid, vig;
        float scale;

        static Font LoadFont(string n) => Resources.Load<Font>("Fonts/" + n);

        void Styles()
        {
            if (cap != null) return;
            var display = HudGUI.Display != null ? HudGUI.Display : LoadFont("BebasNeue-Regular");
            var body = HudGUI.Body != null ? HudGUI.Body : LoadFont("BarlowCondensed-SemiBold");
            cap = new GUIStyle { font = body, fontSize = 20, alignment = TextAnchor.MiddleLeft };
            sub = new GUIStyle { font = body, fontSize = 27, alignment = TextAnchor.MiddleCenter };
            subWho = new GUIStyle(sub) { fontSize = 16 };
            big = new GUIStyle { font = display, fontSize = 72, alignment = TextAnchor.MiddleCenter };
            prompt = new GUIStyle { font = display, fontSize = 30, alignment = TextAnchor.MiddleCenter };
            btn = new GUIStyle { font = display, fontSize = 30, alignment = TextAnchor.MiddleCenter };
            disc = Radial(64, false); circle = Radial(96, true); ring = Ring(128); rain = Rain(256); noise = Noise(128);
            lid = Lid(64); vig = Vignette(128);
        }

        void OnGUI()
        {
            if (hero == null) return;
            Styles();
            float boost = Application.isMobilePlatform ? 1.35f : 1.2f;
            scale = Screen.height / 720f * boost;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale, barK = h / 720f;
            bool repaint = Event.current.type == EventType.Repaint;

            if (phase == Phase.Cinematic)
            {
                if (t >= S3 && t < S4) Puddle(w, h, t - S3);
                if (t < S1) Rain(w, h, 0.42f);
                if (t >= S2 && t < S3) Eyes(w, h);
                if (t >= S4 + 5.15f && t < S4 + 5.65f) Void(w, h);
            }
            if (phase == Phase.Memory) Memory(w, h);

            // lightning: two white frames, then a blue afterglow
            if (flashFrames > 0) { Fill(new Rect(0, 0, w, h), Color.white); if (repaint) flashFrames--; }
            else if (Time.unscaledTime < blueUntil)
            {
                float k = (blueUntil - Time.unscaledTime) / 0.45f;
                Fill(new Rect(0, 0, w, h), new Color(0.55f, 0.66f, 1f, 0.35f * blueStrength * k * k));
            }

            float bh = barsPx * barK;
            if (bh > 0.5f) { Fill(new Rect(0, 0, w, bh), Color.black); Fill(new Rect(0, h - bh, w, bh), Color.black); }

            if (phase == Phase.Cinematic)
            {
                // THE YARDS · 3:12 AM
                float ca = Mathf.Min(Ease(0.5f, 1.2f, t), 1f - Ease(3.3f, 3.9f, t));
                if (ca > 0.01f)
                {
                    cap.normal.textColor = new Color(1f, 1f, 1f, 0.85f * ca);
                    GUI.Label(new Rect(70, h - bh + (bh - 30) * 0.5f, w, 30), Spaced("THE YARDS") + "     3:12 AM", cap);
                    Fill(new Rect(70, h - bh + bh * 0.5f + 16, 40, 2), new Color(0.8f, 0.06f, 0.07f, ca));
                }
                // SKIP
                if (t > 1.5f && t < Control - 1.2f)
                {
                    var r = new Rect(w - 190, Mathf.Max(18f, bh * 0.5f - 22f), 160, 44);
                    btn.fontSize = 22;
                    btn.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
                    if (GUI.Button(r, Spaced("SKIP") + "  >", btn)) SkipToWalk();
                    btn.fontSize = 30;
                }
            }

            // subtitles, in the lower bar
            if (Time.unscaledTime < subUntil && !string.IsNullOrEmpty(subLine))
            {
                float y = h - Mathf.Max(bh, 70f * barK) * 0.5f - 26f;
                subWho.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
                GUI.Label(new Rect(0, y - 6, w, 20), Spaced(subSpeaker), subWho);
                sub.normal.textColor = Color.white;
                GUI.Label(new Rect(0, y + 14, w, 34), subLine, sub);
            }

            // the walk prompt
            if (phase == Phase.Walk && promptAlpha > 0.01f)
            {
                bool touch = Application.isMobilePlatform;
                prompt.normal.textColor = new Color(1f, 1f, 1f, 0.9f * promptAlpha);
                GUI.Label(new Rect(0, h * 0.7f, w, 40), Spaced(touch ? "HOLD THE RIGHT SIDE TO WALK" : "HOLD  D  OR  RIGHT ARROW  TO WALK"), prompt);
                for (int i = 0; i < 3; i++)
                {
                    float k = Mathf.Repeat(Time.unscaledTime * 1.4f - i * 0.25f, 1f);
                    Chevron(new Vector2(w * 0.5f - 30 + i * 30, h * 0.7f + 62), new Color(1f, 1f, 1f, promptAlpha * (1f - k)));
                }
            }
            // the fight's chip
            if (phase == Phase.Fight && Time.unscaledTime < chipUntil)
            {
                float a = Mathf.Clamp01((chipUntil - Time.unscaledTime) / 0.6f);
                var r = new Rect(w * 0.5f - 300, h - 66, 600, 40);
                Fill(r, new Color(0f, 0f, 0f, 0.5f * a));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f * a);
                GUI.Label(r, "TAP HIGH  PUNCH    ·    TAP LOW  KICK    ·    SWIPE SIDEWAYS  DODGE", cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            if (phase == Phase.End) EndCard(w, h);
            GUI.matrix = Matrix4x4.identity;
        }

        // ---- shot 2: eyelids, vignette, the splat

        static readonly float[] LidKeys = { 6.8f, 0f, 7.35f, 0.32f, 7.6f, 0.32f, 7.85f, 0.04f, 8.3f, 0.5f, 8.7f, 0.5f, 9.0f, 0.08f, 9.5f, 0.72f, 10.2f, 0.78f, 10.24f, 0f, 10.36f, 0f, 10.55f, 1f };

        static float Lids(float x)
        {
            for (int i = 0; i < LidKeys.Length - 2; i += 2)
                if (x < LidKeys[i + 2]) return Mathf.Lerp(LidKeys[i + 1], LidKeys[i + 3], Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LidKeys[i], LidKeys[i + 2], x)));
            return 1f;
        }

        void Eyes(float w, float h)
        {
            float o = Lids(t);
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.9f);
            GUI.DrawTexture(new Rect(-w * 0.1f, -h * 0.1f, w * 1.2f, h * 1.2f), vig);
            float lidH = h * 0.5f * (1f - o) + 2f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, w, lidH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, lidH, w, 60), lid);                                         // a soft edge
            GUI.DrawTexture(new Rect(0, h - lidH, w, lidH), Texture2D.whiteTexture);
            GUI.DrawTextureWithTexCoords(new Rect(0, h - lidH - 60, w, 60), lid, new Rect(0, 1, 1, -1));
            float s = Time.unscaledTime - splatAt;
            if (s >= 0f && s < 0.45f)
            {
                float k = s / 0.45f;
                GUI.color = new Color(0.9f, 0.95f, 1f, (1f - k) * 0.9f);
                float r0 = Mathf.Lerp(60f, 420f, Mathf.Sqrt(k));
                GUI.DrawTexture(new Rect(w * 0.5f - r0, h * 0.45f - r0, r0 * 2f, r0 * 2f), disc);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * 30f * Mathf.Deg2Rad + 0.3f;
                    float d = Mathf.Lerp(30f, 380f, k) * (0.7f + 0.3f * Mathf.Sin(i * 7.1f));
                    float sz = 26f * (1f - k) + 6f;
                    GUI.DrawTexture(new Rect(w * 0.5f + Mathf.Cos(a) * d - sz, h * 0.45f + Mathf.Sin(a) * d - sz, sz * 2f, sz * 2f), circle);
                }
            }
            GUI.color = old;
        }

        // ---- shot 3: the puddle, his reflection

        void Puddle(float w, float h, float u)
        {
            var old = GUI.color;
            var m0 = GUI.matrix;
            float lift = Ease(3.9f, 4.8f, u);                                  // pushing up, away from the water
            float sc = Mathf.Lerp(1f, 0.82f, lift);
            GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), new Vector2(w * 0.5f, h * 0.5f) * scale);
            GUI.color = new Color(0.10f, 0.105f, 0.12f);
            GUI.DrawTextureWithTexCoords(new Rect(-w * 0.2f, -h * 0.2f, w * 1.4f, h * 1.4f), noise, new Rect(0, 0, w / 90f, h / 90f));
            var P = new Vector2(w * 0.5f, h * 0.47f);
            var R = new Vector2(w * 0.34f, h * 0.36f);
            float flash = u >= 1.2f ? Mathf.Clamp01(1f - (u - 1.2f) / 0.6f) : 0f;
            GUI.color = Color.Lerp(new Color(0.16f, 0.19f, 0.25f), new Color(0.48f, 0.54f, 0.66f), flash);
            GUI.DrawTexture(new Rect(P.x - R.x * 1.15f, P.y - R.y * 1.15f, R.x * 2.3f, R.y * 2.3f), disc);
            GUI.color = Color.Lerp(new Color(0.05f, 0.06f, 0.09f, 0.92f), new Color(0.30f, 0.34f, 0.44f, 0.92f), flash);
            GUI.DrawTexture(new Rect(P.x - R.x * 0.95f, P.y - R.y * 0.95f, R.x * 1.9f, R.y * 1.9f), disc);
            // the girders and the crack above, reflected
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            for (int i = -1; i <= 1; i++) GUI.DrawTexture(new Rect(P.x - R.x * 0.7f, P.y + i * R.y * 0.42f, R.x * 1.4f, 6f), Texture2D.whiteTexture);
            GUI.color = new Color(0.7f, 0.8f, 1f, 0.25f + 0.7f * flash);
            for (int i = 0; i < 4; i++) GUI.DrawTexture(new Rect(P.x - R.x * 0.55f + i * R.x * 0.28f, P.y - R.y * 0.62f + (i % 2) * 7f, R.x * 0.3f, 3f), Texture2D.whiteTexture);
            // his face in the water, in the flash; a drop shatters it into slivers
            float fa = u < 1.2f ? 0f : u < 1.75f ? 1f : Mathf.Clamp01(1f - (u - 1.75f) / 0.45f);
            if (fa > 0.01f) Face(new Vector2(P.x, P.y - R.y * 0.05f), h * 0.17f, u, fa);
            // ripples
            Ripple(P, R.x * 0.9f, u, 0.1f, 0.5f);
            Ripple(new Vector2(P.x, P.y - R.y * 0.05f), R.x * 1.1f, u, 1.7f, 0.9f);
            Ripple(P + new Vector2(R.x * 0.3f, R.y * 0.2f), R.x * 0.6f, u, 2.8f, 0.4f);
            Ripple(P - new Vector2(R.x * 0.35f, -R.y * 0.1f), R.x * 0.6f, u, 3.7f, 0.4f);
            // his arms and palms, planted on the concrete, trembling
            float j = Mathf.Sin(u * 41f) * 1.4f;
            float drop = lift * h * 0.35f;
            Arm(new Vector2(w * 0.10f, h + 60f), new Vector2(w * 0.24f + j, h * 0.80f + drop));
            Arm(new Vector2(w * 0.90f, h + 60f), new Vector2(w * 0.76f - j, h * 0.82f + drop));
            GUI.matrix = m0;
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(-w * 0.05f, -h * 0.05f, w * 1.1f, h * 1.1f), vig);
            GUI.color = old;
        }

        void Face(Vector2 F, float fr, float u, float alpha)
        {
            float amp = 2f + 34f * Ease(1.7f, 2.15f, u);
            int n = 16;
            float top = F.y - fr * 1.15f, hgt = fr * 2.3f;
            for (int i = 0; i < n; i++)
            {
                float y0 = top + hgt * i / n, sh = hgt / n + 1f;
                float dx = Mathf.Sin(i * 0.9f + u * 14f) * amp;
                GUI.BeginGroup(new Rect(0, y0, 4000f, sh));
                float ox = dx, oy = -y0;
                GUI.color = new Color(0.02f, 0.02f, 0.025f, alpha);
                GUI.DrawTexture(new Rect(F.x - fr + ox, F.y - fr + oy, fr * 2f, fr * 2f), circle);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                // stark white eye slits
                GUI.DrawTexture(new Rect(F.x - fr * 0.56f + ox, F.y - fr * 0.08f + oy, fr * 0.34f, fr * 0.075f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(F.x + fr * 0.22f + ox, F.y - fr * 0.08f + oy, fr * 0.34f, fr * 0.075f), Texture2D.whiteTexture);
                // the jagged hairline fracture at the temple
                GUI.DrawTexture(new Rect(F.x - fr * 0.70f + ox, F.y - fr * 0.62f + oy, fr * 0.13f, fr * 0.035f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(F.x - fr * 0.60f + ox, F.y - fr * 0.55f + oy, fr * 0.035f, fr * 0.11f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(F.x - fr * 0.60f + ox, F.y - fr * 0.45f + oy, fr * 0.12f, fr * 0.035f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(F.x - fr * 0.50f + ox, F.y - fr * 0.45f + oy, fr * 0.035f, fr * 0.10f), Texture2D.whiteTexture);
                GUI.EndGroup();
            }
        }

        void Ripple(Vector2 c, float maxR, float u, float t0, float strength)
        {
            float k = (u - t0) / 1.6f;
            if (k <= 0f || k >= 1f) return;
            float r0 = maxR * Mathf.Sqrt(k);
            for (int i = 0; i < 2; i++)
            {
                float rr = r0 * (1f - i * 0.28f);
                GUI.color = new Color(0.75f, 0.82f, 0.95f, strength * (1f - k) * (i == 0 ? 0.6f : 0.35f));
                GUI.DrawTexture(new Rect(c.x - rr, c.y - rr * 0.82f, rr * 2f, rr * 1.64f), ring);
            }
        }

        void Arm(Vector2 from, Vector2 palm)
        {
            var d = palm - from;
            float len = d.magnitude, ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var m0 = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, (Vector2)GUI.matrix.MultiplyPoint3x4(from));
            GUI.color = new Color(0.02f, 0.02f, 0.025f);
            GUI.DrawTexture(new Rect(from.x, from.y - 22f, len, 44f), Texture2D.whiteTexture);
            GUI.matrix = m0;
            GUI.DrawTexture(new Rect(palm.x - 38f, palm.y - 32f, 76f, 64f), circle);
        }

        // ---- shot 4: the void; the memory; the end

        void Void(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), Color.black);
            for (int i = 0; i < 240; i++)
                Fill(new Rect(Random.value * w, Random.value * h, Random.Range(1f, 3f), Random.Range(1f, 3f)), new Color(1f, 1f, 1f, Random.Range(0.2f, 0.9f)));
            for (int i = 0; i < 7; i++)
                Fill(new Rect(Random.value * w * 0.6f, Random.value * h, Random.Range(40f, w * 0.5f), Random.Range(1f, 4f)), new Color(1f, 1f, 1f, Random.Range(0.1f, 0.5f)));
        }

        void Memory(float w, float h)
        {
            float white = m > 0.12f && m < 0.32f ? 1f : 0f;
            float dark = Mathf.Min(Ease(0f, 0.3f, m), 1f - Ease(2.8f, 3.2f, m));
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.88f * dark));
            if (m > 0.32f)
            {
                float k = Ease(0.32f, 0.5f, m);
                var m0 = GUI.matrix;
                float sc = Mathf.Lerp(1.35f, 1f, k);
                GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), new Vector2(w * 0.5f, h * 0.45f) * scale);
                big.normal.textColor = new Color(1f, 1f, 1f, k * dark);
                GUI.Label(new Rect(0, h * 0.45f - 45, w, 90), Spaced("YOU'VE DONE THIS BEFORE."), big);
                GUI.matrix = m0;
                Fill(new Rect(w * 0.5f - 160 * k, h * 0.45f + 48, 320 * k, 3), new Color(0.8f, 0.06f, 0.07f, k * dark));
            }
            if (white > 0f) Fill(new Rect(0, 0, w, h), Color.white);
        }

        void EndCard(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.92f));
            big.fontSize = 64;
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, h * 0.26f, w, 80), Spaced("PROLOGUE COMPLETE"), big);
            Fill(new Rect(w * 0.5f - 160, h * 0.26f + 84, 320, 3), new Color(0.8f, 0.06f, 0.07f));
            cap.alignment = TextAnchor.MiddleCenter;
            cap.fontSize = 22;
            cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(0, h * 0.26f + 100, w, 30), Spaced("CHAPTER 1") + "     THE HITMAN     " + Spaced("COMING SOON"), cap);
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = 20;
            var again = new Rect(w * 0.5f - 300, h * 0.62f, 280, 62);
            var menu = new Rect(w * 0.5f + 20, h * 0.62f, 280, 62);
            Fill(again, new Color(1f, 1f, 1f, 0.12f)); Fill(menu, new Color(1f, 1f, 1f, 0.12f));
            btn.normal.textColor = Color.white;
            if (GUI.Button(again, Spaced("PLAY AGAIN"), btn)) { Retry = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            if (GUI.Button(menu, Spaced("MAIN MENU"), btn)) { Retry = false; SceneManager.LoadScene("Menu"); }
        }

        // ---- shot 0: rain over the lens

        void Rain(float w, float h, float a)
        {
            var old = GUI.color;
            float tt = Time.unscaledTime;
            GUI.color = new Color(0.8f, 0.85f, 1f, a);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), rain, new Rect(tt * 0.35f, tt * 2.6f, w / 300f, h / 300f));
            GUI.color = new Color(0.8f, 0.85f, 1f, a * 0.6f);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), rain, new Rect(tt * 0.2f + 0.37f, tt * 1.6f + 0.21f, w / 520f, h / 520f));
            GUI.color = old;
        }

        // ---- helpers

        static string Spaced(string s) => string.IsNullOrEmpty(s) ? s : string.Join(" ", s.ToCharArray());

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void Chevron(Vector2 c, Color col)
        {
            var m0 = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, (Vector2)GUI.matrix.MultiplyPoint3x4(c));
            Fill(new Rect(c.x - 9, c.y - 9, 18, 4), col);
            Fill(new Rect(c.x + 5, c.y - 9, 4, 18), col);
            GUI.matrix = m0;
        }

        static Texture2D Radial(int n, bool hard)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = hard ? Mathf.Clamp01((1f - d) * c * 0.5f) : Mathf.Clamp01(1f - d);
                    if (!hard) a *= a;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D Ring(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) * 30f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D Rain(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[n * n];
            var r = new System.Random(4);
            for (int i = 0; i < 70; i++)
            {
                int x0 = r.Next(n), y0 = r.Next(n), len = 10 + r.Next(26);
                for (int k = 0; k < len; k++)
                {
                    int x = ((x0 + k / 4) % n + n) % n, y = ((y0 - k) % n + n) % n;
                    byte a = (byte)(200 * (k / (float)len));
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D Noise(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[n * n];
            var r = new System.Random(9);
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)(170 + r.Next(85));
                px[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D Lid(int n)
        {
            var tex = new Texture2D(4, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[4 * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < 4; x++)
                    px[y * 4 + x] = new Color32(255, 255, 255, (byte)(255 * (y / (float)(n - 1))));
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D Vignette(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01((d - 0.55f) / 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }
    }
}
