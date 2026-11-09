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
    public partial class StoryIntro : MonoBehaviour
    {
        public static StoryIntro Instance { get; private set; }
        /// <summary>Survives the reload after a defeat: start again at the door, not the wake-up.</summary>
        public static bool Retry;

        public HeroController hero;
        public WaveDirector director;

        enum Phase { Cinematic, Walk, Breach, Escape, Duel, Fight, Memory, ToHall, Hall, Ending, End, Bank, Street, Home, Park, Fortress }
        Phase phase = Phase.Cinematic;

        /// <summary>The fight HUD steps aside while the story holds the screen.</summary>
        public bool HideHud => chapter == 2 || (chapter == 3 && (barsPx > 1f || driverStage >= 5)) || (chapter == 4 && (barsPx > 1f || cleanerStage >= 5 || hClimbing)) || (chapter == 5 && (barsPx > 1f || handlerStage >= 5)) || (chapter == 6 && (barsPx > 1f || wardenStage >= 5)) || phase == Phase.Cinematic || phase == Phase.Escape || phase == Phase.Duel || phase == Phase.Memory || phase == Phase.ToHall || phase == Phase.Ending || phase == Phase.End || (phase == Phase.Breach && b < ReleaseAt);

        // the edit (real seconds)
        const float S1 = 4.2f, S2 = 6.8f, S3 = 10.8f, S4 = 15.6f, S5 = 22.2f, S6 = 27.0f, Control = 30.6f;
        const float Bar = 92f, ReleaseAt = 4.6f, BreachAt = 1.9f;
        /// <summary>The opening plays slower than written: half as long again, so it breathes.</summary>
        const float Pace = 1.5f;
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
        Color[] menColor = new Color[3];
        float dustAt;
        const float BackZ = 2.6f;
        int spawned;
        float blueUntil, blueStrength, glitchUntil, clearedAt = -1f;
        int flashFrames;
        float subBottom;                 // how much of the screen's bottom the bar and the words take
        float barsPx = Bar, promptAlpha, chipUntil, footAt;
        float walkFromX;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; HeroController.Did -= OnFortHit; }

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
            searchlight = Find("Searchlight"); searchPool = Find("SearchPool"); heliLight = Find("HeliLight");
            drop1R = drop1 != null ? drop1.GetComponent<Renderer>() : null;
            home = hero.transform.position;
            runSpeed = hero.runSpeed;
            StoryCameraFX.Set(0f, 0f);
            string sceneName = SceneManager.GetActiveScene().name;
            chapter = sceneName == "Story_Bank" ? 2 : sceneName == "Story_Street" ? 3 : sceneName == "Story_Home" ? 4 : sceneName == "Story_Backyard" ? 5 : sceneName == "Story_Fortress" ? 6 : 1;      // 4: chapter 3, home
            HeroController.RageScale = 1f;
            Enemy.RageFear = false;
            CutawayWall.ShowAll = false;      // 3: the street, chapter 2's second half
            if (chapter >= 2) { HeadWound.Chapter = 2; Sound.Ambience(0f, 0.55f, 22000f); }   // the rain stopped with the warehouse
            else Sound.Ambience(2.4f, 0.2f, 22000f);
            FindEscapeProps();
        }

        static Transform Find(string n) { var g = GameObject.Find(n); return g != null ? g.transform : null; }
        static Renderer R(string n) { var g = GameObject.Find(n); return g != null ? g.GetComponent<Renderer>() : null; }

        static float Ease(float a, float b2, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b2, x));
        bool At(float time) => prevT < time && t >= time;
        bool AtB(float time) => prevB < time && b >= time;
        bool AtM(float time) => prevM < time && m >= time;

        Vector3 HeadC() => head != null ? head.position + head.up * 0.19f : hero.transform.position + Vector3.up * 1.2f;

        static void Buzz() => Haptics.Vibrate();

        // ================================================================== the loop

        void Update()
        {
            if (hero == null) return;
            if (!setUp && chapter == 2) { setUp = true; BeginBank(); }
            if (!setUp && chapter == 3) { setUp = true; BeginStreet(); }
            if (!setUp && chapter == 4) { setUp = true; BeginHome(); }
            if (!setUp && chapter == 5) { setUp = true; BeginBackyard(); }
            if (!setUp && chapter == 6) { setUp = true; BeginFortress(); }
            if (!setUp)
            {
                setUp = true;
                HeadWound.Chapter = 1;                       // the warehouse: the night of the blow
                if (!Retry) EscapeSeen = false;              // from the top: he has not found the knife yet
                if (TestEnding > 0) { JumpToEnding(); }                         // TEST: straight into the ending
                else if (HallCheckpoint >= 0) { BeginHall(HallCheckpoint, true); }      // a defeat in the hall: back to that zone
                else if (Retry) { BeginAtDoor(); }
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
                    prevT = t; t += dt / Pace;
                    Cinematic();
                    break;
                case Phase.Walk: UpdateWalk(); break;
                case Phase.Escape:
                    prevE = e; e += dt;
                    UpdateEscape();
                    break;
                case Phase.Duel: UpdateDuel(); break;
                case Phase.ToHall:
                    prevHallT = hallT; hallT += dt;
                    UpdateToHall();
                    break;
                case Phase.Hall: UpdateHall(); break;
                case Phase.Street: UpdateStreet(); break;
                case Phase.Home: UpdateHome(); break;
                case Phase.Park: UpdatePark(); break;
                case Phase.Fortress: UpdateFortress(); break;
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
            if (beads != null && (phase != Phase.Cinematic || t >= S3)) CrackWater(Vector3.zero, 0f, 0f, false);
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
            if (strength > 0.6f) { Buzz(); LightningSwap.Strike(0.42f); }
        }

        void Glitch(float amount, float seconds)
        {
            glitchUntil = Time.unscaledTime + seconds;
            StoryCameraFX.Set(StoryCameraFX.Instance != null ? StoryCameraFX.Instance.Blur : 0f, amount);
        }

        // ================================================================== the cinematic

        /// <summary>The camera breathes: a slow handheld drift, never still.</summary>
        static Vector3 Sway(float amp, float speed, float seed)
        {
            float T = Time.unscaledTime * speed;
            return new Vector3(Mathf.PerlinNoise(T, seed) - 0.5f, Mathf.PerlinNoise(seed + 3.1f, T) - 0.5f, Mathf.PerlinNoise(T + 7.7f, seed + 1.3f) - 0.5f) * (2f * amp);
        }

        float ambientAt = 2f;
        Transform searchlight, searchPool, heliLight;

        /// <summary>Between the beats the place keeps living: thunder rolling far off, the iron
        /// creaking in the wind, a chain chinking, water dripping somewhere in the dark.</summary>
        void Ambient()
        {
            if (Time.unscaledTime < ambientAt || t < S1) return;
            ambientAt = Time.unscaledTime + Random.Range(3.5f, 7f);
            var spot = new Vector3(home.x + Random.Range(-10f, 10f), 4f, 1f);
            switch (Random.Range(0, 4))
            {
                case 0: Sound.Play(Sfx.Thunder, Random.Range(0.18f, 0.3f), null, Random.Range(0.8f, 0.95f)); break;
                case 1: Sound.Play(Sfx.MetalClang, 0.12f, spot, Random.Range(0.42f, 0.55f)); break;      // the iron creaks
                case 2: Sound.Play(Sfx.Shell, 0.25f, spot, Random.Range(0.55f, 0.7f)); break;           // a chain chinks
                default: Sound.Play(Sfx.Drip, Random.Range(0.3f, 0.6f), spot, Random.Range(0.8f, 1.2f)); break;
            }
        }

        void Cinematic()
        {
            if (cam == null) return;
            Vector3 pos, look;
            float fov, roll = 0f;
            var hc = HeadC();
            Ambient();
            ShowSearch(t < S1);

            if (t < S1)
            {
                // SHOT 0: the desolate yard. Craning down the road to the shed; a helicopter's beam
                // combs the stacks; lightning forks over the city and the whole yard flashes white.
                if (At(0.05f)) { Sound.Play(Sfx.Wind, 0.9f); Sound.Play(Sfx.Rotor, 0.7f); }
                if (At(2.3f))
                {
                    Lightning(1f, 1f, 0.12f);
                    if (LightningSwap.Instance != null) LightningSwap.Instance.Bolt(new Vector3(-46f, 95f, -170f), new Vector3(-30f, 18f, -160f), 1.6f);
                }
                float k = Ease(0f, S1, t);
                pos = Vector3.Lerp(new Vector3(-12f, 23f, 45f), new Vector3(-10f, 18f, 40f), k) + Sway(0.12f, 0.25f, 1f);
                look = Vector3.Lerp(new Vector3(-3f, 1f, -60f), new Vector3(-2f, 3f, -60f), k);
                fov = Mathf.Lerp(40f, 39.2f, k);
                // the searchlight: from the sky, sweeping across the yard towards the shed
                if (searchlight != null)
                {
                    var heli = Vector3.Lerp(new Vector3(34f, 30f, 30f), new Vector3(16f, 27f, 22f), k);
                    var target = Vector3.Lerp(new Vector3(26f, 0f, 14f), new Vector3(2f, 0f, 9f), k) + new Vector3(Mathf.Sin(t * 1.3f) * 3f, 0f, Mathf.Cos(t * 0.9f) * 2f);
                    searchlight.position = heli;
                    searchlight.rotation = Quaternion.FromToRotation(Vector3.down, target - heli);
                    if (searchPool != null) searchPool.position = target + Vector3.up * 0.05f;
                    if (heliLight != null)
                    {
                        heliLight.position = heli + Vector3.up * 0.6f;
                        heliLight.rotation = Quaternion.LookRotation(heliLight.position - pos);
                        heliLight.gameObject.SetActive(Mathf.Repeat(Time.unscaledTime, 1.1f) < 0.2f);
                    }
                }
            }
            else if (t < S2)
            {
                // SHOT 1: the roof fracture. Looking straight up, the camera turning slowly as the drop gathers.
                if (At(S1)) { Sound.Ambience(0.9f, 0.55f, 800f); }
                if (At(S1 + 0.3f)) Sound.Play(Sfx.Drip, 0.35f, null, 0.9f);
                if (At(S1 + DropRelease)) Sound.Play(Sfx.Drip, 0.25f, null, 1.5f);          // the tiny tick as it lets go
                if (At(S1 + DropRelease + 0.75f)) Sound.Play(Sfx.Whoosh, 0.35f, null, 1.9f);  // the air of it, close
                float k = Ease(S1, S2, t);
                // straight under the drop, looking straight up the line it will fall: it stays dead
                // centre and only grows as it comes, until it lands on the lens
                var a = CrackPoint(hc.z);
                var sw = Sway(0.004f, 0.4f, 2f);
                pos = new Vector3(a.x + sw.x, hc.y + 0.35f + 0.12f * k, a.z + sw.z);
                look = new Vector3(a.x, RoofY, a.z + 0.04f);
                fov = Mathf.Lerp(26f, 22f, k);
                roll = Mathf.Lerp(0f, 12f, k);
                if (!dropHit)
                {
                    PoseDrop(drop0, drop0R, hc, S1 + 0.2f, S1 + DropRelease, 9.8f, S2);
                    if (drop0R != null && drop0R.enabled && t > S1 + DropRelease && (drop0.position.y - pos.y < 0.45f || t > S2 - 0.06f)) LensHit();
                }
                else if (drop0R != null) drop0R.enabled = false;
                CrackWater(hc, S1 + 0.05f, S1 + 0.75f, true);
            }
            else if (t < S3)
            {
                // SHOT 2: the spark of consciousness (his eyes)
                if (At(S2 + 0.05f)) Sound.Play(Sfx.Tinnitus, 1f);
                if (At(S2 + 0.8f)) Sound.Play(Sfx.Heartbeat, 0.5f);
                if (At(S2 + 1.5f)) Lightning(0.3f, 0.4f, 0.6f);
                if (At(S2 + 2.4f)) Sound.Play(Sfx.Heartbeat, 0.6f);
                if (drop0R != null) drop0R.enabled = false;
                if (drop1R != null) drop1R.enabled = false;
                if (At(10.25f)) Sound.Play(Sfx.Breath, 0.85f, null, 1.1f);                    // he comes to
                // his eyes drift up at the roof as he breathes, settling as they open and clear
                float settle = Ease(9.4f, S3, t);
                pos = hc + new Vector3(0f, 0.1f, 0f) + Sway(0.02f, 0.3f, 3f) * (1f - 0.6f * settle);
                look = new Vector3(hc.x + 0.5f, RoofY, hc.z + 0.2f) + Sway(0.25f, 0.2f, 4f) * (1f - 0.6f * settle);
                fov = 64f;
                CrackWater(hc, 0f, 0f, true);                                       // still beading on the seam above him
                float blur = Mathf.Lerp(Mathf.Lerp(1f, 0.72f, Ease(S2, 9.0f, t)), 0f, Ease(9.4f, 10.5f, t));   // clearing as they open
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
                if (At(S3 + 0.1f)) Sound.Play(Sfx.Breath, 0.9f);
                if (At(S3 + 0.25f)) Sound.Play(Sfx.HeroEffort, 0.7f, null, 0.72f);      // a groan
                if (At(S3 + PuddleShot.DropAt)) Sound.Play(Sfx.Drip, 1f, null, 0.7f);    // plop, slowed down
                if (At(S3 + 1.6f)) Sound.Play(Sfx.Piano, 0.75f);                         // a face, surfacing: who is this?
                if (At(S3 + PuddleShot.FlashAt)) Lightning(1f, 0.8f, 0.1f);              // and the sky answers
                if (At(S3 + 4.4f)) { Sound.Play(Sfx.Splash, 0.9f); Sound.Play(Sfx.Cloth, 0.8f); }
                if (At(S3 + 4.6f)) Sound.Play(Sfx.Breath, 0.8f, null, 0.9f);
                pos = hc + new Vector3(0f, 0.6f, -0.1f);
                look = hc;
                fov = 50f;
            }
            else if (t < S5)
            {
                // SHOT 4: the lost identity (side profile, Dutch angle), creeping in as he rises
                if (At(S4)) { if (hero.body.Has("rise")) hero.body.Play("rise", 0.08f, true, 0.9f / Pace); Sound.Play(Sfx.HeroEffort, 0.8f, null, 0.8f); }
                if (At(S4 + 0.8f)) Sound.Play(Sfx.Cloth, 0.7f);
                if (At(S4 + 2.5f)) { if (hero.body.Has("look_hands")) hero.body.Play("look_hands", 0.15f, true, 1f / Pace); }
                if (At(S4 + 2.8f)) Sound.Play(Sfx.Heartbeat, 0.9f);
                if (At(S4 + 3.6f)) Sound.Play(Sfx.Heartbeat, 1f);
                if (At(S4 + 3.8f)) Sound.Play(Sfx.Bone, 0.35f, null, 1.5f);            // the knuckles crack
                if (At(S4 + 4.5f)) { if (hero.body.Has("touch_temple")) hero.body.Play("touch_temple", 0.12f, true, 1f / Pace); }
                if (At(S4 + 4.9f)) { Sound.Play(Sfx.Static, 1f); Glitch(1f, 0.09f); }   // touching the wound
                if (At(S4 + 5.15f)) { Sound.Play(Sfx.Whispers, 1f); Sound.Play(Sfx.Static, 0.5f, null, 0.7f); }
                if (At(S4 + 5.65f)) { Sound.Play(Sfx.Heartbeat, 1f); Glitch(0.6f, 0.06f); }
                if (At(S4 + 6.0f)) Sound.Play(Sfx.Breath, 0.7f);
                float k = Ease(S4, S5, t);
                look = new Vector3(hc.x + 0.15f, Mathf.Max(0.45f, hc.y - 0.15f), 0f);
                pos = look + Vector3.Lerp(new Vector3(0.7f, -0.14f, -3.0f), new Vector3(0.45f, -0.08f, -2.2f), k) + Sway(0.015f, 0.35f, 5f);
                fov = 33f;
                roll = Mathf.Lerp(8f, 5f, k);
            }
            else if (t < S6)
            {
                // SHOT 5: WHERE AM I. The static has passed and nothing came back with it. He looks
                // round: the hall to his left, up into the rafters, round at a sound behind him (nothing),
                // and at last, small in all that dark, towards the thin moonlight under the shutter.
                // His face, then what he sees, cut against each other; his head really turns and tips.
                float u = t - S5;
                if (At(S5)) { if (hero.body.Has("idle_loose")) hero.body.Play("idle_loose", 0.3f); Sound.Ambience(0.55f, 0.3f, 700f); }
                if (At(S5 + 0.1f)) Sound.Play(Sfx.Breath, 0.7f);
                if (At(S5 + 0.45f)) Sound.Play(Sfx.Cloth, 0.5f);
                if (At(S5 + 1.15f)) Sound.Play(Sfx.Drip, 0.5f, new Vector3(home.x - 5f, 0f, 0f), 0.85f);
                if (At(S5 + 2.2f)) Sound.Play(Sfx.Cloth, 0.4f);
                if (At(S5 + 2.6f)) Sound.Play(Sfx.Heartbeat, 0.8f);
                if (At(S5 + 3.0f)) Sound.Play(Sfx.Heartbeat, 0.9f);
                if (At(S5 + 3.3f)) Sound.Play(Sfx.Chain, 0.4f, new Vector3(home.x, 1.5f, 2.4f));    // behind him
                if (At(S5 + 3.42f)) Sound.Play(Sfx.Whoosh, 0.5f, null, 1.3f);
                if (At(S5 + 3.5f)) Sound.Play(Sfx.Breath, 0.8f, null, 1.2f);
                if (At(S5 + 4.1f)) Sound.Play(Sfx.Wind, 0.6f);
                if (At(S5 + 4.3f)) Sound.Play(Sfx.Heartbeat, 0.6f);

                // where he faces (degrees about up: 0 = towards the shutter) and how far his head tips back
                float th = Key(GazeYaw, u), ph = Key(GazePitch, u);
                hero.transform.rotation = Quaternion.Euler(0f, th, 0f);
                headPitch = ph; headPitchOn = true;
                var d = new Vector3(Mathf.Cos(th * Mathf.Deg2Rad), 0f, -Mathf.Sin(th * Mathf.Deg2Rad));
                var gaze = Quaternion.AngleAxis(ph, Vector3.Cross(d, Vector3.up)) * d;
                var eye = hc + d * 0.12f + Vector3.up * 0.03f;
                var hp5 = hero.transform.position;
                float blur = 0f;
                if (u < 0.95f)
                {
                    // his face, close, front three-quarter: he lifts his head and turns past us
                    pos = hc + new Vector3(0.42f, 0.04f, -1.15f) + Sway(0.008f, 0.5f, 31f);
                    look = hc + new Vector3(0f, -0.02f, 0f);
                    fov = 30f; roll = -2f;
                }
                else if (u < 2.0f)
                {
                    // what he sees: the hall to his left, drifting, not quite in focus
                    pos = eye;
                    look = eye + gaze * 5f + Sway(0.25f, 0.35f, 32f);
                    fov = 52f; roll = 2f * Mathf.Sin(u * 1.3f);
                    blur = 0.32f;
                }
                else if (u < 2.6f)
                {
                    // under his chin as he tips his head back: his black shape against the rafters
                    pos = hc + d * 0.55f + new Vector3(0f, -0.62f, -0.25f);
                    look = hc + new Vector3(0f, 1.3f, 0f) - d * 0.2f;
                    fov = 44f; roll = 4f;
                }
                else if (u < 3.4f)
                {
                    // what he sees up there: the rafters, the fan, the crack's glow; the room starts to turn
                    pos = eye;
                    look = eye + gaze * 5f + Sway(0.2f, 0.3f, 33f);
                    fov = 56f; roll = Mathf.Lerp(0f, 16f, Ease(2.6f, 3.4f, u));
                    blur = 0.25f + 0.2f * Mathf.Abs(Mathf.Sin(u * 5f));
                }
                else if (u < 3.95f)
                {
                    // a chain, behind him: he spins round (his eyes, whipping) and there is nothing
                    pos = eye;
                    look = eye + gaze * 5f;
                    fov = 50f; roll = Mathf.Lerp(16f, 0f, Ease(3.4f, 3.75f, u));
                    blur = u < 3.75f ? 0.9f * Mathf.Sin(Mathf.InverseLerp(3.4f, 3.75f, u) * Mathf.PI) : 0.12f;
                }
                else
                {
                    // wide, low, from far across the floor: small in all that dark, he turns to the light
                    // under the shutter at the far end
                    float kw = Ease(3.95f, 4.8f, u);
                    pos = new Vector3(hp5.x - 4.6f + 0.6f * kw, 0.45f, -4.3f + 0.4f * kw);
                    look = new Vector3(hp5.x + 2.6f, 0.95f, 0.8f);
                    fov = 40f; roll = 0f;
                }
                StoryCameraFX.Set(blur, 0f);
            }
            else
            {
                // SHOT 6: the path to the shutter, then the hand-off to play
                if (At(S6)) { hero.SetFacing(1); if (hero.body.Has("walk")) hero.body.Play("walk", 0.25f, false, 1.7f / Pace); }
                if (At(S6 + 0.2f)) Sound.Play(Sfx.Drone, 1f);
                float wx = home.x + 1.3f * Mathf.Max(0f, t - (S6 + 0.2f));
                hero.CinePose(new Vector3(wx, home.y, 0f));
                if (t - (S6 + 0.2f) > 0f && Time.unscaledTime > footAt)
                {
                    footAt = Time.unscaledTime + 0.45f * Pace;
                    Sound.Play(Sfx.Footstep, 0.9f, hero.transform.position, 0.85f);
                }
                var h = hero.transform.position;
                float a = Ease(S6, Control - 0.4f, t);
                float th = Mathf.Lerp(180f, 270f, a) * Mathf.Deg2Rad;
                float rad = Mathf.Lerp(2.0f, 10.5f, a);
                pos = h + new Vector3(Mathf.Cos(th) * rad, Mathf.Lerp(0.35f, 1.7f, a), Mathf.Sin(th) * rad) + Sway(0.03f * (1f - a), 0.5f, 7f);
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

        // shot 5: where he faces (u, degrees: 0 = towards the shutter, 180 = the hall to his left,
        // 270 = the back wall) and how far his head tips back (u, degrees)
        static readonly float[] GazeYaw = { 0f, 0f, 0.3f, 0f, 1.0f, 160f, 1.9f, 178f, 3.3f, 190f, 3.45f, 190f, 3.72f, 268f, 3.95f, 272f, 4.25f, 272f, 4.8f, 360f };
        static readonly float[] GazePitch = { 0f, -12f, 0.4f, -2f, 1.9f, 2f, 2.25f, 48f, 3.2f, 58f, 3.4f, 8f, 4.8f, 0f };
        float headPitch;
        bool headPitchOn;

        /// <summary>Piecewise smooth interpolation through (x, value) pairs.</summary>
        static float Key(float[] k, float x)
        {
            if (x <= k[0]) return k[1];
            for (int i = 0; i < k.Length - 2; i += 2)
                if (x < k[i + 2]) return Mathf.Lerp(k[i + 1], k[i + 3], Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(k[i], k[i + 2], x)));
            return k[k.Length - 1];
        }

        /// <summary>After the animation: his head tipped back as far as the shot wants (shot 5).</summary>
        void LateUpdate()
        {
            if (head == null || hero == null) return;
            // the ending: his head turning side to side (no... no...)
            if (headYawOn) head.rotation = Quaternion.AngleAxis(headYaw, Vector3.up) * head.rotation;
            if (!headPitchOn) return;
            if (phase != Phase.Cinematic || t < S5 || t >= S6) { headPitchOn = false; return; }
            var fwd = hero.transform.rotation * Vector3.right;
            head.rotation = Quaternion.AngleAxis(headPitch, Vector3.Cross(fwd, Vector3.up)) * head.rotation;
        }

        /// <summary>The helicopter only exists in the opening shot (its red light blinks itself).</summary>
        void ShowSearch(bool on)
        {
            if (searchlight != null && searchlight.gameObject.activeSelf != on) searchlight.gameObject.SetActive(on);
            if (searchPool != null && searchPool.gameObject.activeSelf != on) searchPool.gameObject.SetActive(on);
            if (!on && heliLight != null && heliLight.gameObject.activeSelf) heliLight.gameObject.SetActive(false);
        }

        /// <summary>A drop forms at the crack (from `form` it swells), lets go at `release`
        /// and falls with acceleration `g`, hidden after `hide`. It is a clear lens on a quad,
        /// always turned to the camera; it trembles a little as it gathers.</summary>
        void PoseDrop(Transform d, Renderer r, Vector3 hc, float form, float release, float g, float hide)
        {
            if (d == null || r == null) return;
            r.enabled = t >= form && t < hide;
            if (!r.enabled) return;
            // it hangs from the crack itself: on its jagged line, right at its underside
            var a = CrackPoint(hc.z);
            float s = Mathf.Lerp(0.016f, 0.065f, Ease(form, release, t)) * 1.28f;
            Vector3 p;
            if (t < release)
            {
                float wob = 1f + 0.05f * Mathf.Sin(t * 23f) * Ease(release - 0.5f, release, t);
                p = new Vector3(a.x, a.y - s * 0.42f, a.z);
                d.localScale = new Vector3(s * wob, s / wob, 1f);
            }
            else
            {
                float u = t - release;
                p = new Vector3(a.x, a.y - s * 0.42f - 0.5f * g * u * u, a.z);
                float osc = 1f + 0.08f * Mathf.Sin(u * 60f) * Mathf.Exp(-u * 6f);     // it rings as it lets go
                d.localScale = new Vector3(s * osc, s / osc, 1f);
            }
            d.position = p;
            if (cam != null) d.rotation = Quaternion.LookRotation(p - cam.transform.position, cam.transform.up);
        }

        /// <summary>When the first drop lets go (story time after S1): timed so it reaches the lens
        /// just before the cut to his eyes.</summary>
        const float DropRelease = 1.2f;
        bool dropHit;
        float lensHitAt = -9f;

        /// <summary>The drop lands on the lens: a wet tap, the picture blurs, water on the glass.</summary>
        void LensHit()
        {
            dropHit = true;
            if (drop0R != null) drop0R.enabled = false;
            LensSplash(0.7f);
            if (cam != null) cam.Shake(0.05f, 0.15f);
        }

        /// <summary>A drop landing on the lens (on his eye): a wet tap, a blur, water on the glass.</summary>
        void LensSplash(float vol)
        {
            lensHitAt = Time.unscaledTime;
            Sound.Play(Sfx.Splat, vol, null, 1.25f);
            Sound.Play(Sfx.Drip, 0.6f * vol, null, 0.7f);
            Buzz();
            StoryCameraFX.Set(0.85f, 0f);
        }

        static readonly Vector4[] LensDrops =
        {
            new Vector4(-0.21f, -0.12f, 46f, 0.9f), new Vector4(0.18f, -0.2f, 30f, 0.7f), new Vector4(0.27f, 0.14f, 22f, 0.8f),
            new Vector4(-0.3f, 0.18f, 26f, 0.6f), new Vector4(0.06f, 0.27f, 18f, 0.75f), new Vector4(-0.08f, -0.3f, 16f, 0.65f),
            new Vector4(0.36f, -0.05f, 14f, 0.5f),
        };

        /// <summary>Water on the lens after the hit: the burst blob in the middle with its bright
        /// edge, beads thrown round it, and a few runs sliding down; all fading as the eyes go.</summary>
        void LensWater(float w, float h)
        {
            float k = (Time.unscaledTime - lensHitAt) / 1.1f;
            if (k < 0f || k >= 1f) return;
            var o = GUI.color;
            float fade = 1f - k * k;
            var c = new Vector2(w * 0.5f, h * 0.5f);
            float big = Mathf.Lerp(w * 0.2f, w * 0.32f, Mathf.Sqrt(Mathf.Min(1f, k * 4f)));
            GUI.color = new Color(0.8f, 0.86f, 1f, 0.22f * fade);
            GUI.DrawTexture(new Rect(c.x - big, c.y - big * 0.9f, big * 2f, big * 1.8f), disc);
            GUI.color = new Color(0.9f, 0.95f, 1f, 0.45f * fade);
            GUI.DrawTexture(new Rect(c.x - big * 0.92f, c.y - big * 0.84f, big * 1.84f, big * 1.68f), ring);
            foreach (var d in LensDrops)
            {
                float out1 = Mathf.Min(1f, k * 6f);
                var p = c + new Vector2(d.x * w, d.y * h) * (0.6f + 0.4f * out1) + new Vector2(0f, k * k * 40f * d.w);
                float r = d.z * (0.7f + 0.3f * out1);
                GUI.color = new Color(0.85f, 0.9f, 1f, 0.3f * fade * d.w);
                GUI.DrawTexture(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), circle);
                GUI.color = new Color(1f, 1f, 1f, 0.55f * fade * d.w);
                GUI.DrawTexture(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), ring);
                // the run below it
                Fill(new Rect(p.x - 1.5f, p.y + r * 0.8f, 3f, k * 110f * d.w), new Color(0.85f, 0.9f, 1f, 0.22f * fade));
            }
            GUI.color = o;
        }

        Vector3[] crackLine;

        /// <summary>The point on the roof crack's jagged centre line at depth z, at its underside
        /// (world). Read from the crack's own mesh, so a drop on it is really on it.</summary>
        Vector3 CrackPoint(float z)
        {
            if (crackLine == null)
            {
                var mf = crack != null ? crack.GetComponent<MeshFilter>() : null;
                var v = mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable ? mf.sharedMesh.vertices : null;
                if (v == null || v.Length < 4) return new Vector3(crack != null ? crack.position.x : home.x, RoofY - 0.03f, z);
                crackLine = new Vector3[v.Length / 2];
                for (int i = 0; i < crackLine.Length; i++) crackLine[i] = crack.TransformPoint((v[i * 2] + v[i * 2 + 1]) * 0.5f);
            }
            if (z <= crackLine[0].z) return crackLine[0];
            for (int i = 1; i < crackLine.Length; i++)
                if (z <= crackLine[i].z)
                    return Vector3.Lerp(crackLine[i - 1], crackLine[i], Mathf.InverseLerp(crackLine[i - 1].z, crackLine[i].z, z));
            return crackLine[crackLine.Length - 1];
        }

        Transform[] beads;
        Renderer[] beadR;
        static readonly float[] BeadZ = { -0.5f, 0.42f, 0.78f, 1.2f, 1.62f };
        static readonly float[] BeadS = { 0.022f, 0.03f, 0.018f, 0.026f, 0.02f };

        /// <summary>Water on the crack: small beads clinging along it, and one running down the seam
        /// to where the drop gathers (from `form` to `merge`), feeding it.</summary>
        void CrackWater(Vector3 hc, float form, float merge, bool show)
        {
            if (drop0 == null) return;
            if (beads == null)
            {
                beads = new Transform[BeadZ.Length + 1];
                beadR = new Renderer[beads.Length];
                for (int i = 0; i < beads.Length; i++)
                {
                    beads[i] = Instantiate(drop0.gameObject, drop0.parent).transform;
                    beads[i].name = "CrackBead" + i;
                    beadR[i] = beads[i].GetComponent<Renderer>();
                }
            }
            for (int i = 0; i < beads.Length; i++)
            {
                bool trickle = i == beads.Length - 1;
                bool on = show && (!trickle || (t >= form && t < merge));
                if (beadR[i] != null) beadR[i].enabled = on;
                if (!on) continue;
                float sz;
                Vector3 p;
                if (trickle)
                {
                    float k = Ease(form, merge, t);
                    var a = CrackPoint(Mathf.Lerp(hc.z + 0.75f, hc.z, k));
                    sz = Mathf.Lerp(0.024f, 0.012f, k) * 1.28f;
                    p = a + Vector3.down * sz * 0.4f;
                }
                else
                {
                    var a = CrackPoint(hc.z + BeadZ[i] - 0.1f);
                    sz = BeadS[i] * 1.28f * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2f + i));
                    p = a + Vector3.down * sz * 0.4f;
                }
                beads[i].position = p;
                beads[i].localScale = new Vector3(sz, sz, 1f);
                if (cam != null) beads[i].rotation = Quaternion.LookRotation(p - cam.transform.position, cam.transform.up);
            }
        }

        void HandOff()
        {
            phase = Phase.Walk;
            ShowSearch(false);
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
            if (hero.transform.position.x > ShutterX - 5f) { phase = Phase.Breach; b = prevB = 0f; }
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
            var latch = new Vector3(DoorL + 0.6f, 1.0f, 2.6f);
            if (AtB(0.9f)) { Sound.Play(Sfx.Latch, 1f, new Vector3(ShutterX, 1f, 3f)); StreakFx.Sparks(latch, new Vector3(-0.4f, 0.6f, -1f), 12, 4f); }
            if (AtB(1.25f)) { Sound.Play(Sfx.Latch, 0.8f, new Vector3(ShutterX, 1f, 3f), 0.9f); StreakFx.Sparks(latch, new Vector3(-0.3f, 0.4f, -1f), 8, 3f); }
            if (shutter != null && b < BreachAt)
            {
                float rattle = b > 1.0f ? Mathf.Sin(b * 70f) * 0.02f : 0f;
                shutter.position = new Vector3(shutter.position.x, shutterY + rattle, shutter.position.z);
                // rust and dust shaken loose from the drum
                if (b > 1.0f && Time.unscaledTime > dustAt && InkFx.Instance != null)
                {
                    dustAt = Time.unscaledTime + 0.09f;
                    InkFx.Instance.Splash(new Vector3(Random.Range(DoorL, DoorR), 3.7f, 2.45f), Vector3.down, 3, new Color(0.45f, 0.36f, 0.28f));
                }
            }
            if (AtB(BreachAt))
            {
                // THE BREACH
                Sound.Play(Sfx.MetalClang, 1f, new Vector3(ShutterX, 2f, 3f));
                Sound.Play(Sfx.Door, 1f, new Vector3(ShutterX, 1f, 3f));
                Sound.Play(Sfx.Boom, 0.7f);
                Lightning(1f, 0.8f, 0.05f);                       // the strike lights the doorway: three men in it
                Buzz();
                var beam = Find("DoorBeam");
                if (beam != null) foreach (var r in beam.GetComponentsInChildren<Renderer>()) r.enabled = true;
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
                    if (men[i] != null)
                    {
                        men[i].Passive = true;                              // no swings until the scene lets go
                        menColor[i] = men[i].InkColor;
                        men[i].SetBaseColor(new Color(0.015f, 0.015f, 0.02f));   // black against the moonlight
                    }
                    spawned++;
                }
            // their colour comes up as they step out of the backlight into the room
            for (int i = 0; i < men.Length; i++)
                if (men[i] != null)
                {
                    float k = Mathf.InverseLerp(2.4f, 0.4f, men[i].transform.position.z);
                    men[i].SetBaseColor(Color.Lerp(new Color(0.015f, 0.015f, 0.02f), menColor[i], k * k));
                }
            if (AtB(2.7f)) Say("YOU", "Wait... who are you?", 1.25f);
            if (AtB(4.0f)) Say("THUG", "He's awake. Finish it.", 1.5f);

            // the camera: hero and door together, then back to the fight view
            if (cam != null)
            {
                float wIn = Ease(0f, 0.6f, b), wOut = 1f - Ease(ReleaseAt, ReleaseAt + 0.6f, b);
                var hp = hero.transform.position;
                // low, over his shoulder, the doorway beyond him
                cam.Scripted(new Vector3(hp.x - 1.7f, 0.7f, -1.9f) + Sway(0.03f, 0.6f, 8f), new Vector3(ShutterX, 1.6f, BackZ + 0.6f), Mathf.Min(wIn, wOut), 46f);
            }
            barsPx = b < ReleaseAt ? Mathf.Lerp(0f, 70f, Ease(0f, 0.5f, b)) : Mathf.Lerp(70f, 0f, Ease(ReleaseAt, ReleaseAt + 0.6f, b));
            if (AtB(ReleaseAt) && CanEscape) { BeginEscape(); return; }       // he runs (see StoryEscape)
            if (AtB(ReleaseAt))
            {
                // a retry: the knife is already his
                if (EscapeSeen) { hero.GiveWeapon(FightStyle.Knife, 4); if (floorKnife != null) floorKnife.gameObject.SetActive(false); }
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
            // the inserts: a scope on a man, a muzzle flash, a target photo, each a stab of static
            if (AtM(0.01f)) { Sound.Play(Sfx.Static, 1f); Sound.Play(Sfx.Whispers, 1f); Glitch(1f, 0.18f); Buzz(); }
            if (AtM(0.45f)) { Sound.Play(Sfx.Heartbeat, 1f); Sound.Play(Sfx.Static, 0.7f, null, 1.2f); }
            if (AtM(0.85f)) { Sound.Play(Sfx.GunBody, 0.9f); Sound.Play(Sfx.GunTail, 0.8f); Glitch(0.8f, 0.1f); Buzz(); }
            if (AtM(1.2f)) { Sound.Play(Sfx.Static, 0.8f, null, 0.9f); Sound.Play(Sfx.CineHit, 0.6f); }
            if (AtM(1.75f)) { Sound.Play(Sfx.Piano, 0.8f); Sound.Play(Sfx.Heartbeat, 1f); }
            if (AtM(1.8f)) Sound.Play(Sfx.CineMassive, 0.7f);
            if (AtM(5.2f))
            {
                Sound.Play(Sfx.CineHit, 0.7f);
                BeginToHall();                                     // the rest of the crew, beyond the inner shutter (StoryHall)
            }
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
                if (t < S3) LensWater(w, h);
                if (t >= S2 && t < S3) Eyes(w, h);
                if (t >= S4 + 5.15f && t < S4 + 5.65f) Void(w, h);
            }
            if (phase == Phase.Memory) Memory(w, h);
            if (phase == Phase.Escape) EscapeGUI(w, h);
            if (phase == Phase.Duel || Time.unscaledTime - okAt < 0.4f) DuelGUI(w, h);
            HallGUI(w, h);

            // lightning: two white frames, then a blue afterglow
            if (flashFrames > 0) { Fill(new Rect(0, 0, w, h), Color.white); if (repaint) flashFrames--; }
            else if (Time.unscaledTime < blueUntil)
            {
                float k = (blueUntil - Time.unscaledTime) / 0.45f;
                Fill(new Rect(0, 0, w, h), new Color(0.55f, 0.66f, 1f, 0.35f * blueStrength * k * k));
            }

            float bh = barsPx * barK;
            float sb = SubtitleBand(w, repaint);                 // the bottom bar grows to hold a line of dialogue
            if (bh > 0.5f) { Fill(new Rect(0, 0, w, bh), Color.black); Fill(new Rect(0, h - Mathf.Max(bh, sb), w, Mathf.Max(bh, sb)), Color.black); }
            subBottom = Mathf.Max(bh, sb);

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
                var r = new Rect(w * 0.5f - 300, h - 66 - (sb > 1f ? sb : 0f), 600, 40);      // above any line of dialogue
                Fill(r, new Color(0f, 0f, 0f, 0.5f * a));
                cap.alignment = TextAnchor.MiddleCenter;
                cap.normal.textColor = new Color(1f, 1f, 1f, 0.9f * a);
                GUI.Label(r, EscapeSeen ? "TAP HIGH  STAB    ·    TAP LOW  KICK    ·    SWIPE SIDEWAYS  DODGE" : "TAP HIGH  PUNCH    ·    TAP LOW  KICK    ·    SWIPE SIDEWAYS  DODGE", cap);
                cap.alignment = TextAnchor.MiddleLeft;
            }
            ForemanGUI(w, h);
            if (chapter == 2) BankGUI(w, h);
            if (chapter == 3) StreetGUI(w, h);
            if (chapter == 4) HomeGUI(w, h);
            if (chapter == 5) ParkGUI(w, h);
            if (chapter == 6) FortressGUI(w, h);
            EndingGUI(w, h);
            BlackoutGUI(w, h);
            // the words last: nothing in the story is drawn over them
            if (phase != Phase.End && (cardAt < 0f)) DrawSubtitle(w, h, bh);
            if (phase == Phase.End) EndCard(w, h);
            GUI.matrix = Matrix4x4.identity;
        }

        // ---- shot 2: eyelids, vignette, the splat

        static readonly float[] LidKeys = { 6.8f, 0f, 7.35f, 0.32f, 7.6f, 0.32f, 7.85f, 0.04f, 8.3f, 0.5f, 8.7f, 0.5f, 9.0f, 0.08f, 9.5f, 0.72f, 10.0f, 0.8f, 10.45f, 1f };

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
            GUI.color = old;
        }

        // ---- shot 3: the puddle, his reflection

        PuddleShot puddle;

        /// <summary>Looking straight down into the water between his hands: a drop falls in, ONE ripple
        /// spreads in slow motion and dies away, and as it calms his face surfaces; then the lightning.</summary>
        void Puddle(float w, float h, float u)
        {
            if (puddle == null) puddle = new PuddleShot(Screen.width / (float)Screen.height);
            if (Event.current.type == EventType.Repaint) puddle.Update(u);
            var old = GUI.color;
            var m0 = GUI.matrix;
            float lift = Ease(4.4f, 4.8f, u);                                  // pushing up, away from the water
            float sc = Mathf.Lerp(1f, 0.8f, lift);
            GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), new Vector2(w * 0.5f, h * 0.5f) * scale);
            float fl = PuddleShot.Flash(u);
            Fill(new Rect(-w * 0.2f, -h * 0.2f, w * 1.4f, h * 1.4f), Color.Lerp(new Color(0.07f, 0.072f, 0.08f), new Color(0.2f, 0.22f, 0.26f), fl));
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), puddle.Concrete);
            if (fl > 0f) Fill(new Rect(-w * 0.2f, -h * 0.2f, w * 1.4f, h * 1.4f), new Color(0.75f, 0.82f, 1f, 0.22f * fl));   // the concrete lit blue-white
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), puddle.Water);
            var C = new Vector2(w * 0.5f, h * 0.5f);
            // the drop: a bright streak falling from the crack above, straight down into the centre
            if (u >= 0.25f && u < PuddleShot.DropAt)
            {
                float k = Ease(0.25f, PuddleShot.DropAt, u);
                float ds = Mathf.Lerp(3f, 11f, k * k);
                var at = Vector2.Lerp(C + new Vector2(-w * 0.02f, -h * 0.08f), C, k * k);
                GUI.color = new Color(0.8f, 0.86f, 1f, 0.35f + 0.5f * k);
                GUI.DrawTexture(new Rect(at.x - ds, at.y - ds, ds * 2f, ds * 2f), disc);
                GUI.color = new Color(1f, 1f, 1f, 0.7f * k);
                GUI.DrawTexture(new Rect(at.x - ds * 0.3f, at.y - ds * 0.3f, ds * 0.6f, ds * 0.6f), disc);
            }
            // the crown: a few beads thrown up and out, falling back, in slow motion
            if (u >= PuddleShot.DropAt && u < PuddleShot.DropAt + 0.55f)
            {
                float k = (u - PuddleShot.DropAt) / 0.55f;
                for (int i = 0; i < 9; i++)
                {
                    float ang = i * 0.698f + 0.3f, rr = 14f + 44f * k * (0.8f + 0.25f * Mathf.Sin(i * 2.1f));
                    float up = 46f * k * (1f - k) * (0.7f + 0.3f * Mathf.Cos(i * 1.7f));
                    var bp = C + new Vector2(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr * 0.55f - up);
                    float bs = 3.2f * (1f - k * 0.6f);
                    GUI.color = new Color(0.85f, 0.9f, 1f, 0.8f * (1f - k));
                    GUI.DrawTexture(new Rect(bp.x - bs, bp.y - bs, bs * 2f, bs * 2f), disc);
                }
                // the rebound column (the Worthington jet), rising and sinking back
                float jh = 34f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(k * 1.6f));
                if (jh > 1f)
                {
                    GUI.color = new Color(0.78f, 0.84f, 0.98f, 0.55f * (1f - k));
                    GUI.DrawTexture(new Rect(C.x - 2.5f, C.y - jh, 5f, jh), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(C.x - 4.5f, C.y - jh - 4.5f, 9f, 9f), disc);
                }
            }
            // his arms and palms, planted either side of the water, trembling
            float j = Mathf.Sin(u * 41f) * 1.2f;
            float drop = lift * h * 0.35f;
            Arm(new Vector2(w * 0.06f, h + 60f), new Vector2(w * 0.17f + j, h * 0.84f + drop));
            Arm(new Vector2(w * 0.94f, h + 60f), new Vector2(w * 0.83f - j, h * 0.86f + drop));
            GUI.matrix = m0;
            GUI.color = new Color(0, 0, 0, 0.8f * (1f - fl * 0.6f));
            GUI.DrawTexture(new Rect(-w * 0.05f, -h * 0.05f, w * 1.1f, h * 1.1f), vig);
            if (fl > 0f) { GUI.color = new Color(0.8f, 0.86f, 1f, 0.18f * fl); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); }
            GUI.color = old;
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
            float dark = Mathf.Min(Ease(0f, 0.25f, m), 1f - Ease(4.8f, 5.2f, m));
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.9f * dark));
            if (m > 0.12f && m < 0.2f) { Fill(new Rect(0, 0, w, h), Color.white); return; }
            float jx = Random.Range(-4f, 4f), jy = Random.Range(-3f, 3f);
            if (m >= 0.2f && m < 0.82f) Scope(w, h, m - 0.2f, jx, jy);
            else if (m >= 0.82f && m < 1.18f) Muzzle(w, h, m - 0.82f);
            else if (m >= 1.18f && m < 1.7f) Photo(w, h, m - 1.18f, jx, jy);
            else if (m >= 1.7f && m < 1.78f) Fill(new Rect(0, 0, w, h), Color.white);
            else if (m >= 1.78f)
            {
                float k = Ease(1.78f, 2.0f, m);
                var m0 = GUI.matrix;
                float sc = Mathf.Lerp(1.35f, 1f, k);
                GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), (Vector2)GUI.matrix.MultiplyPoint3x4(new Vector2(w * 0.5f, h * 0.45f)));
                big.normal.textColor = new Color(1f, 1f, 1f, k * dark);
                GUI.Label(new Rect(0, h * 0.45f - 45, w, 90), Spaced("YOU'VE DONE THIS BEFORE."), big);
                GUI.matrix = m0;
                Fill(new Rect(w * 0.5f - 160 * k, h * 0.45f + 48, 320 * k, 3), new Color(0.8f, 0.06f, 0.07f, k * dark));
            }
        }

        /// <summary>Through a rifle scope: a man's silhouette in the circle, the crosshair settling on him.</summary>
        void Scope(float w, float h, float u, float jx, float jy)
        {
            float r = h * 0.4f;
            var c = new Vector2(w * 0.5f + jx, h * 0.5f + jy);
            var drift = new Vector2(Mathf.Sin(u * 9f) * 18f * (1f - u), Mathf.Cos(u * 7f) * 12f * (1f - u));
            var old = GUI.color;
            GUI.color = new Color(0.12f, 0.16f, 0.2f);
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), circle);
            // the target: head and shoulders, unaware
            var tgt = c + new Vector2(30f, 20f) - drift;
            GUI.color = new Color(0.02f, 0.02f, 0.03f);
            GUI.DrawTexture(new Rect(tgt.x - 34f, tgt.y - 92f, 68f, 68f), circle);
            GUI.DrawTexture(new Rect(tgt.x - 70f, tgt.y - 30f, 140f, 160f), circle);
            GUI.color = old;
            // crosshair and mil dots
            var red = new Color(0.9f, 0.08f, 0.06f, 0.95f);
            Fill(new Rect(c.x - r, c.y - 1, r * 2f, 2f), new Color(0f, 0f, 0f, 0.9f));
            Fill(new Rect(c.x - 1, c.y - r, 2f, r * 2f), new Color(0f, 0f, 0f, 0.9f));
            for (int i = -4; i <= 4; i++) if (i != 0) { Fill(new Rect(c.x + i * r * 0.18f - 2, c.y - 2, 4, 4), Color.black); Fill(new Rect(c.x - 2, c.y + i * r * 0.18f - 2, 4, 4), Color.black); }
            Fill(new Rect(c.x - 3, c.y - 3, 6, 6), red);
            cap.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            GUI.Label(new Rect(c.x + r * 0.45f, c.y + r * 0.55f, 200, 24), "450 M", cap);
        }

        /// <summary>A muzzle flash: a white star in the dark, a ring of fire.</summary>
        void Muzzle(float w, float h, float u)
        {
            float k = 1f - u / 0.36f;
            var c = new Vector2(w * 0.55f, h * 0.48f);
            var old = GUI.color;
            GUI.color = new Color(1f, 0.75f, 0.35f, 0.9f * k);
            float r = h * 0.55f * (0.6f + 0.4f * k);
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), disc);
            GUI.color = new Color(1f, 1f, 1f, k);
            GUI.DrawTexture(new Rect(c.x - r * 0.3f, c.y - r * 0.3f, r * 0.6f, r * 0.6f), disc);
            var m0 = GUI.matrix;
            for (int i = 0; i < 6; i++)
            {
                GUIUtility.RotateAroundPivot(30f, (Vector2)GUI.matrix.MultiplyPoint3x4(c));
                Fill(new Rect(c.x - r * 0.9f, c.y - 3f, r * 1.8f, 6f), new Color(1f, 0.95f, 0.8f, 0.8f * k));
            }
            GUI.matrix = m0;
            GUI.color = old;
        }

        /// <summary>A target photo, pinned, a red cross through the face, TARGET stamped on it.</summary>
        void Photo(float w, float h, float u, float jx, float jy)
        {
            var c = new Vector2(w * 0.5f + jx, h * 0.48f + jy);
            float pw = h * 0.5f, ph = h * 0.6f;
            var m0 = GUI.matrix;
            GUIUtility.RotateAroundPivot(-6f + u * 4f, (Vector2)GUI.matrix.MultiplyPoint3x4(c));
            Fill(new Rect(c.x - pw / 2, c.y - ph / 2, pw, ph), new Color(0.86f, 0.84f, 0.78f));
            Fill(new Rect(c.x - pw / 2 + 14, c.y - ph / 2 + 14, pw - 28, ph - 70), new Color(0.32f, 0.33f, 0.36f));
            var old = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.06f);
            GUI.DrawTexture(new Rect(c.x - 34f, c.y - ph * 0.3f, 68f, 68f), circle);
            GUI.DrawTexture(new Rect(c.x - 66f, c.y - ph * 0.3f + 60f, 132f, 120f), circle);
            GUI.color = old;
            var red = new Color(0.82f, 0.05f, 0.05f, 0.95f);
            var m1 = GUI.matrix;
            var face = new Vector2(c.x, c.y - ph * 0.3f + 34f);
            GUIUtility.RotateAroundPivot(45f, (Vector2)GUI.matrix.MultiplyPoint3x4(face));
            Fill(new Rect(face.x - 60, face.y - 4, 120, 8), red);
            GUI.matrix = m1;
            GUIUtility.RotateAroundPivot(-45f, (Vector2)GUI.matrix.MultiplyPoint3x4(face));
            Fill(new Rect(face.x - 60, face.y - 4, 120, 8), red);
            GUI.matrix = m1;
            big.fontSize = 44;
            big.normal.textColor = red;
            GUIUtility.RotateAroundPivot(-12f, (Vector2)GUI.matrix.MultiplyPoint3x4(new Vector2(c.x, c.y + ph * 0.36f)));
            GUI.Label(new Rect(c.x - 150, c.y + ph * 0.36f - 26, 300, 52), Spaced("TARGET"), big);
            big.fontSize = 72;
            GUI.matrix = m0;
        }

        /// <summary>The chapter just finished, as the player knows it (the street is chapter 2's second half).</summary>
        string ChapterDone => chapter >= 6 ? "CHAPTER 5 COMPLETE" : chapter >= 5 ? "CHAPTER 4 COMPLETE" : chapter >= 4 ? "CHAPTER 3 COMPLETE" : chapter >= 2 ? "CHAPTER 2 COMPLETE" : "CHAPTER 1 COMPLETE";

        void EndCard(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.92f));
            big.fontSize = 64;
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, h * 0.26f, w, 80), Spaced(ChapterDone), big);
            Fill(new Rect(w * 0.5f - 160, h * 0.26f + 84, 320, 3), new Color(0.8f, 0.06f, 0.07f));
            cap.alignment = TextAnchor.MiddleCenter;
            cap.fontSize = 22;
            cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(0, h * 0.26f + 100, w, 30), chapter >= 6 ? Spaced("NEXT") + "     " + Spaced("CHAPTER 6") + "  ·  THE LAST ROOF     " + Spaced("COMING SOON")
                                                              : chapter >= 5 ? Spaced("NEXT") + "     " + Spaced("CHAPTER 5") + "  ·  THE FORTRESS"
                                                              : chapter >= 4 ? Spaced("NEXT") + "     " + Spaced("CHAPTER 4") + "  ·  THE BACKYARD"
                                                              : chapter >= 2 ? Spaced("NEXT") + "     " + Spaced("CHAPTER 3") + "  ·  HOME"
                                                              : Spaced("NEXT") + "     " + Spaced("CHAPTER 2") + "  ·  THE BANK", cap);
            cap.alignment = TextAnchor.MiddleLeft;
            cap.fontSize = 20;
            var next = new Rect(w * 0.5f - 300, h * 0.56f, 600, 66);
            var again = new Rect(w * 0.5f - 300, h * 0.56f + 80, 290, 54);
            var menu = new Rect(w * 0.5f + 10, h * 0.56f + 80, 290, 54);
            Fill(next, new Color(1f, 1f, 1f, 0.16f));
            Fill(new Rect(next.x, next.yMax - 3, next.width, 3), new Color(0.8f, 0.06f, 0.07f));
            Fill(again, new Color(1f, 1f, 1f, 0.1f)); Fill(menu, new Color(1f, 1f, 1f, 0.1f));
            btn.normal.textColor = Color.white;
            if (chapter == 1 && GUI.Button(next, Spaced("CONTINUE") + "   >", btn)) { Retry = false; NewRun(); SceneManager.LoadScene("Story_Bank"); }
            if (chapter >= 2 && chapter <= 3 && GUI.Button(next, Spaced("CONTINUE") + "   >", btn)) { Retry = false; NewRun(); SceneManager.LoadScene("Story_Home"); }
            if (chapter == 4 && GUI.Button(next, Spaced("CONTINUE") + "   >", btn)) { Retry = false; NewRun(); SceneManager.LoadScene("Story_Backyard"); }
            if (chapter == 5 && GUI.Button(next, Spaced("CONTINUE") + "   >", btn)) { Retry = false; NewRun(); SceneManager.LoadScene("Story_Fortress"); }
            if (chapter >= 6) { btn.normal.textColor = new Color(1f, 1f, 1f, 0.4f); GUI.Label(next, Spaced("CHAPTER 6 COMING SOON"), btn); btn.normal.textColor = Color.white; }
            btn.fontSize = 24;
            if (GUI.Button(again, Spaced("PLAY AGAIN"), btn)) { Retry = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            if (GUI.Button(menu, Spaced("MAIN MENU"), btn)) { Retry = false; SceneManager.LoadScene("Menu"); }
            btn.fontSize = 30;
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
