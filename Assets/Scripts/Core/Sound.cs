using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Every sound in the game. Each maps to a folder Resources/Audio/&lt;snake_case&gt;/
    /// holding several takes (built by tools/audio/build_audio.py).</summary>
    public enum Sfx
    {
        PunchLight, PunchHeavy, Kick, Finisher, Bone,
        Whoosh, WhooshBig, Footstep, Land, Cloth, BodyFall,
        SwordSwing, SwordHit, SwordClash, PipeHit, Knife, Shell, GunBody, GunTail, Pickup,
        Crate, MetalClang, Glass, Door, WallSplat,
        HeroHurt, HeroEffort, HeroDeath, EnemyHurt, EnemyEffort, EnemyDeath,
        BruteHurt, BruteDeath, BruteLaugh, FallScream,
        SlowmoIn, SlowmoOut, CineHit, CineMassive, Riser, Boom,
        RankUp, WaveClear, FloorClear, Defeat,
        // story mode (synthesised by tools/audio/make_story_sfx.py)
        Thunder, Drip, Splat, Tinnitus, Heartbeat, Static, Whispers, Drone, Latch, Splash, Breath, Wind, Piano, Rotor,
        Chain, BodySplash, Crawl, Glint, KnifeGrab, FlashCut,
    }

    /// <summary>The mixer. Plays recorded takes with no immediate repeats, small pitch and
    /// level variation, stereo placement by where the sound is on screen, per-sound voice
    /// limits, and a film slow-motion treatment: in bullet time everything drops in pitch and
    /// is muffled through a low-pass, with a reverse whoosh going in and a breath coming out.
    /// Under it all, the building's room tone and the rain outside; a low drone when the boss
    /// is on the floor. Creates itself and survives restarts, so the ambience never cuts.</summary>
    public class Sound : MonoBehaviour
    {
        public static Sound Instance { get; private set; }
        public static float Master = 1f;

        /// <summary>Sound on or off (saved).</summary>
        public static void SetMuted(bool muted)
        {
            Master = muted ? 0f : 1f;
            PlayerPrefs.SetInt("sound_muted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        const int Voices = 24;

        class Bank { public AudioClip[] clips; public int last = -1; public float lastTime = -9f; }
        readonly Dictionary<Sfx, Bank> banks = new Dictionary<Sfx, Bank>();
        AudioSource[] voices;
        AudioLowPassFilter[] filters;
        float[] voicePitch;
        int nextVoice;

        AudioSource room, rain, drone;
        AudioLowPassFilter roomLp, rainLp;
        float droneWant;
        bool wasSlow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Sound");
            DontDestroyOnLoad(go);
            go.AddComponent<Sound>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Master = PlayerPrefs.GetInt("sound_muted", 0) == 1 ? 0f : 1f;
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
            {
                var clips = Resources.LoadAll<AudioClip>("Audio/" + Folder(s));
                if (clips.Length > 0) banks[s] = new Bank { clips = clips };
            }
            voices = new AudioSource[Voices];
            filters = new AudioLowPassFilter[Voices];
            voicePitch = new float[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var g = new GameObject("Voice" + i);
                g.transform.SetParent(transform, false);
                voices[i] = g.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
                filters[i] = g.AddComponent<AudioLowPassFilter>();
                filters[i].cutoffFrequency = 22000f;
            }
            room = Loop("amb_room", 0.32f, out roomLp);
            rain = Loop("amb_rain", 0.22f, out rainLp);
            drone = Loop("amb_tension", 0f, out _);
        }

        static string Folder(Sfx s)
        {
            var name = s.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        AudioSource Loop(string folder, float volume, out AudioLowPassFilter lp)
        {
            lp = null;
            var clips = Resources.LoadAll<AudioClip>("Audio/" + folder);
            if (clips.Length == 0) return null;
            var g = new GameObject(folder);
            g.transform.SetParent(transform, false);
            var a = g.AddComponent<AudioSource>();
            a.clip = clips[0];
            a.loop = true;
            a.volume = volume * Master;
            a.spatialBlend = 0f;
            a.playOnAwake = false;
            lp = g.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 22000f;
            a.time = Random.Range(0f, clips[0].length * 0.8f);   // never start the loop at the same place
            a.Play();
            return a;
        }

        // ------------------------------------------------------------------ playing

        /// <summary>Per-sound settings: base level, pitch spread, minimum gap between takes,
        /// and whether slow motion lowers its pitch.</summary>
        static void Traits(Sfx s, out float level, out float spread, out float gap, out bool slows)
        {
            level = 1f; spread = 0.06f; gap = 0.03f; slows = true;
            switch (s)
            {
                case Sfx.PunchLight: level = 0.75f; break;
                case Sfx.PunchHeavy: level = 0.9f; break;
                case Sfx.Kick: level = 0.85f; break;
                case Sfx.Finisher: level = 1f; spread = 0.03f; break;
                case Sfx.Bone: level = 0.6f; break;
                case Sfx.Whoosh: level = 0.32f; spread = 0.1f; gap = 0.05f; break;
                case Sfx.WhooshBig: level = 0.45f; gap = 0.08f; break;
                case Sfx.Footstep: level = 0.18f; spread = 0.08f; gap = 0.12f; break;
                case Sfx.Land: level = 0.45f; gap = 0.1f; break;
                case Sfx.Cloth: level = 0.3f; gap = 0.15f; break;
                case Sfx.BodyFall: level = 0.7f; gap = 0.06f; break;
                case Sfx.SwordSwing: level = 0.5f; break;
                case Sfx.SwordHit: level = 0.85f; break;
                case Sfx.SwordClash: level = 0.75f; gap = 0.1f; break;
                case Sfx.PipeHit: level = 0.8f; break;
                case Sfx.Knife: level = 0.45f; break;
                case Sfx.Shell: level = 0.3f; spread = 0.12f; gap = 0.05f; break;
                case Sfx.GunBody: level = 0.6f; break;
                case Sfx.GunTail: level = 0.4f; break;
                case Sfx.Pickup: level = 0.55f; slows = false; break;
                case Sfx.Crate: level = 0.8f; break;
                case Sfx.MetalClang: level = 0.6f; break;
                case Sfx.Glass: level = 0.6f; break;
                case Sfx.Door: level = 0.45f; spread = 0.04f; gap = 0.3f; slows = false; break;
                case Sfx.WallSplat: level = 0.95f; break;
                case Sfx.HeroHurt: case Sfx.EnemyHurt: level = 0.5f; spread = 0.05f; gap = 0.35f; break;
                case Sfx.HeroEffort: case Sfx.EnemyEffort: level = 0.4f; spread = 0.05f; gap = 0.6f; break;
                case Sfx.HeroDeath: level = 0.7f; spread = 0f; break;
                case Sfx.EnemyDeath: level = 0.5f; spread = 0.07f; gap = 0.25f; break;
                case Sfx.BruteHurt: level = 0.6f; gap = 0.4f; break;
                case Sfx.BruteDeath: level = 0.8f; break;
                case Sfx.BruteLaugh: level = 0.75f; spread = 0f; slows = false; break;
                case Sfx.FallScream: level = 0.5f; gap = 0.5f; break;
                case Sfx.SlowmoIn: level = 0.5f; spread = 0f; gap = 0.4f; slows = false; break;
                case Sfx.SlowmoOut: level = 0.3f; spread = 0f; gap = 0.4f; slows = false; break;
                case Sfx.CineHit: level = 0.8f; spread = 0f; slows = false; break;
                case Sfx.CineMassive: level = 0.9f; spread = 0f; slows = false; break;
                case Sfx.Riser: level = 0.55f; spread = 0f; slows = false; break;
                case Sfx.Boom: level = 0.8f; spread = 0f; slows = false; break;
                case Sfx.RankUp: level = 0.35f; spread = 0.02f; gap = 0.5f; slows = false; break;
                case Sfx.WaveClear: level = 0.55f; spread = 0f; slows = false; break;
                case Sfx.FloorClear: case Sfx.Defeat: level = 0.6f; spread = 0f; gap = 1f; slows = false; break;
                case Sfx.Thunder: level = 1f; spread = 0.04f; gap = 0.8f; slows = false; break;
                case Sfx.Drip: level = 0.5f; spread = 0.1f; gap = 0.08f; break;
                case Sfx.Splat: level = 0.9f; spread = 0.03f; slows = false; break;
                case Sfx.Tinnitus: level = 0.22f; spread = 0f; gap = 1f; slows = false; break;
                case Sfx.Heartbeat: level = 0.8f; spread = 0.02f; gap = 0.3f; slows = false; break;
                case Sfx.Static: level = 0.45f; spread = 0.05f; gap = 0.1f; slows = false; break;
                case Sfx.Whispers: level = 0.55f; spread = 0.05f; gap = 0.3f; slows = false; break;
                case Sfx.Drone: level = 0.5f; spread = 0f; gap = 2f; slows = false; break;
                case Sfx.Latch: level = 0.85f; spread = 0.03f; gap = 0.2f; break;
                case Sfx.Splash: level = 0.3f; spread = 0.1f; gap = 0.12f; break;
                case Sfx.Breath: level = 0.45f; spread = 0.05f; gap = 0.4f; slows = false; break;
                case Sfx.Wind: level = 0.35f; spread = 0.05f; gap = 2f; slows = false; break;
                case Sfx.Piano: level = 0.6f; spread = 0f; gap = 2f; slows = false; break;
                case Sfx.Rotor: level = 0.5f; spread = 0.03f; gap = 2f; slows = false; break;
                case Sfx.Chain: level = 0.85f; spread = 0.04f; gap = 0.3f; break;
                case Sfx.BodySplash: level = 0.95f; spread = 0.03f; gap = 0.3f; break;
                case Sfx.Crawl: level = 0.45f; spread = 0.08f; gap = 0.25f; slows = false; break;
                case Sfx.Glint: level = 0.4f; spread = 0.03f; gap = 0.5f; slows = false; break;
                case Sfx.KnifeGrab: level = 0.75f; spread = 0.03f; gap = 0.3f; slows = false; break;
                case Sfx.FlashCut: level = 0.7f; spread = 0.06f; gap = 0.05f; slows = false; break;
            }
        }

        /// <summary>Play a sound. `at` places it in stereo by where it is on screen.</summary>
        public static void Play(Sfx s, float volume = 1f, Vector3? at = null, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayNow(s, volume, at, pitch);
        }

        /// <summary>Play after a delay in real seconds (shell casings hitting the floor, tails).</summary>
        public static void PlayLater(Sfx s, float seconds, float volume = 1f, Vector3? at = null)
        {
            if (Instance != null) Instance.StartCoroutine(Instance.Later(s, seconds, volume, at));
        }

        IEnumerator Later(Sfx s, float seconds, float volume, Vector3? at)
        {
            yield return new WaitForSecondsRealtime(seconds);
            PlayNow(s, volume, at, 1f);
        }

        void PlayNow(Sfx s, float volume, Vector3? at, float pitch)
        {
            if (!banks.TryGetValue(s, out var bank)) return;
            Traits(s, out float level, out float spread, out float gap, out bool slows);
            float now = Time.unscaledTime;
            if (now - bank.lastTime < gap) return;
            bank.lastTime = now;

            int pick = Random.Range(0, bank.clips.Length);
            if (bank.clips.Length > 1 && pick == bank.last) pick = (pick + 1) % bank.clips.Length;
            bank.last = pick;

            var v = voices[nextVoice];
            int vi = nextVoice;
            nextVoice = (nextVoice + 1) % Voices;
            float p = pitch * (1f + Random.Range(-spread, spread));
            if (slows) p *= SlowPitch();
            voicePitch[vi] = slows ? p / SlowPitch() : -1f;
            v.Stop();
            v.clip = bank.clips[pick];
            v.pitch = p;
            v.volume = Mathf.Clamp01(level * volume * Random.Range(0.92f, 1f) * Master);
            v.panStereo = at.HasValue ? Pan(at.Value) : 0f;
            v.Play();
        }

        static float Pan(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null) return 0f;
            float x = cam.WorldToViewportPoint(world).x;
            return Mathf.Clamp((x - 0.5f) * 1.2f, -0.6f, 0.6f);
        }

        static float SlowPitch()
        {
            float ts = Time.timeScale;
            if (ts <= 0.02f) return 1f;                   // hit-stop: a frozen frame, not slow motion
            return Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(0.15f, 1f, ts));
        }

        // the scene's ambience (story mode): how loud the rain is, how muffled the room is
        public static float RainLevel = 1f, RoomLevel = 1f, AmbienceCutoff = 22000f;

        /// <summary>Set the weather for the shot: rain level (1 = normal), the room tone, and a
        /// low-pass on both (indoors, the storm is muffled). Reset with ResetAmbience.</summary>
        public static void Ambience(float rain, float room, float cutoff)
        {
            RainLevel = rain; RoomLevel = room; AmbienceCutoff = cutoff;
        }
        public static void ResetAmbience() { RainLevel = 1f; RoomLevel = 1f; AmbienceCutoff = 22000f; }

        /// <summary>Fade the boss drone in or out.</summary>
        public static void Tension(bool on) { if (Instance != null) Instance.droneWant = on ? 0.3f : 0f; }

        // ------------------------------------------------------------------ the slow-motion treatment

        void Update()
        {
            var tc = TimeController.Instance;
            bool slow = tc != null && tc.InSlowMo && Time.timeScale < 0.8f;
            if (slow && !wasSlow) PlayNow(Sfx.SlowmoIn, 1f, null, 1f);
            if (!slow && wasSlow) PlayNow(Sfx.SlowmoOut, 1f, null, 1f);
            wasSlow = slow;

            float ts = Time.timeScale <= 0.02f ? 1f : Time.timeScale;
            float cutoff = Mathf.Lerp(1600f, 22000f, Mathf.Pow(Mathf.InverseLerp(0.15f, 1f, ts), 1.6f));
            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            for (int i = 0; i < Voices; i++)
            {
                filters[i].cutoffFrequency = Mathf.Lerp(filters[i].cutoffFrequency, cutoff, k);
                // sounds already playing bend down into slow motion too
                if (voices[i].isPlaying && voicePitch[i] > 0f) voices[i].pitch = voicePitch[i] * SlowPitch();
            }
            float ambCut = Mathf.Lerp(900f, 22000f, Mathf.Pow(Mathf.InverseLerp(0.15f, 1f, ts), 1.6f));
            ambCut = Mathf.Min(ambCut, AmbienceCutoff);
            if (roomLp != null) roomLp.cutoffFrequency = Mathf.Lerp(roomLp.cutoffFrequency, ambCut, k);
            if (rainLp != null) rainLp.cutoffFrequency = Mathf.Lerp(rainLp.cutoffFrequency, ambCut, k);
            if (drone != null) drone.volume = Mathf.MoveTowards(drone.volume, droneWant * Master, Time.unscaledDeltaTime * 0.15f);
            if (room != null) room.volume = Mathf.MoveTowards(room.volume, 0.32f * Master * RoomLevel, Time.unscaledDeltaTime * 0.6f);
            if (rain != null) rain.volume = Mathf.MoveTowards(rain.volume, Mathf.Min(1f, 0.22f * Master * RainLevel), Time.unscaledDeltaTime * 0.8f);
        }
    }
}
