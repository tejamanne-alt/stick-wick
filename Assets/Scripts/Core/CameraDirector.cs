using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    /// <summary>Side-on 2.5D gameplay camera that frames the hero and every active attacker,
    /// plus screen shake and the slow-motion orbit "kill cam". After any orbit it always
    /// returns to the same side, so left on screen stays left for the controls.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraDirector : MonoBehaviour
    {
        public static CameraDirector Instance { get; private set; }

        public Transform hero;
        public float minDistance = 7.5f;
        public float maxDistance = 14f;
        public float height = 1.6f;
        public float lookHeight = 1.1f;
        public float followSmooth = 0.18f;
        public float minX = -12f, maxX = 12f;

        readonly List<Transform> extras = new List<Transform>();
        Camera cam;
        Vector3 focus, focusVel;
        float distance, distanceVel;

        // shake
        float shakeAmp, shakeUntil;
        // kill cam
        Vector3 orbitPivot;
        float orbitStart, orbitDuration, orbitAngle;
        bool orbiting;
        // zoom punch
        float punchUntil, punchAmount;
        float baseFov;

        public bool Orbiting => orbiting;

        Transform focusTarget;
        float focusUntil;

        /// <summary>Close in on one subject (a boss entrance) for a few real seconds.</summary>
        public void Focus(Transform t, float realSeconds)
        {
            focusTarget = t;
            focusUntil = UnityEngine.Time.unscaledTime + realSeconds;
        }

        // scripted shots (the opening cinematic): blended over the gameplay camera by weight
        Vector3 scriptPos, scriptLook;
        float scriptWeight, scriptFov;

        /// <summary>Drive the camera from a cutscene this frame. weight 1 = fully scripted,
        /// 0 = gameplay framing; fov 0 keeps the gameplay field of view.</summary>
        public void Scripted(Vector3 pos, Vector3 look, float weight, float fov = 0f)
        {
            scriptPos = pos;
            scriptLook = look;
            scriptWeight = Mathf.Clamp01(weight);
            scriptFov = fov;
        }

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            baseFov = cam.fieldOfView;
        }

        void Start()
        {
            // wide enough to hold the whole hall (both walls, both floors) on a phone in landscape
            // close enough that fighters stay big on a phone; anyone beyond gets an edge marker
            maxDistance = 17f;
            if (hero != null) focus = hero.position;
            distance = minDistance;
            Snap();
        }

        public void SetFramed(IEnumerable<Transform> targets)
        {
            extras.Clear();
            foreach (var t in targets)
                if (t != null) extras.Add(t);
        }

        public void Shake(float amplitude, float seconds)
        {
            shakeAmp = Mathf.Max(shakeAmp * 0.5f, amplitude);
            shakeUntil = Mathf.Max(shakeUntil, UnityEngine.Time.unscaledTime + seconds);
        }

        public void ZoomPunch(float fovDelta, float seconds)
        {
            punchAmount = fovDelta;
            punchUntil = UnityEngine.Time.unscaledTime + seconds;
        }

        /// <summary>Orbit around a point (the final blow) by `angle` degrees and back.</summary>
        public void KillCam(Vector3 pivot, float realSeconds = 1.3f, float angle = 110f)
        {
            orbitPivot = pivot;
            orbitStart = UnityEngine.Time.unscaledTime;
            orbitDuration = realSeconds;
            orbitAngle = angle;
            orbiting = true;
        }

        [Tooltip("Inner faces of the side walls: the camera never shows past them if it can help it.")]
        public float wallX = 13.0f;
        const float BodyHeight = 1.35f;     // feet to the top of the head, chibi scale
        readonly List<Transform> candidates = new List<Transform>();

        /// <summary>The smallest shot that holds the hero and everyone fighting, with margins for
        /// the HUD. The hero always fits; enemies are added nearest first while the shot stays
        /// within maxDistance (any left out get an edge marker from the HUD). The shot slides
        /// to stay inside the side walls, pulling back if that would cut anyone off.</summary>
        void Frame(out Vector3 center, out float dist)
        {
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(0.5f, cam.aspect);
            var h = hero.position;
            float loX = h.x, hiX = h.x, loY = h.y, hiY = h.y + BodyHeight;

            candidates.Clear();
            foreach (var t in extras) if (t != null) candidates.Add(t);
            candidates.Sort((a, b) => Mathf.Abs(a.position.x - h.x).CompareTo(Mathf.Abs(b.position.x - h.x)));
            foreach (var t in candidates)
            {
                var p = t.position;
                float nLoX = Mathf.Min(loX, p.x), nHiX = Mathf.Max(hiX, p.x);
                float nLoY = Mathf.Min(loY, p.y), nHiY = Mathf.Max(hiY, p.y + BodyHeight);
                bool close = Mathf.Abs(p.x - h.x) < 5f && Mathf.Abs(p.y - h.y) < 2f;
                if (!close && Need(nLoX, nHiX, nLoY, nHiY, tanV, tanH) > maxDistance) continue;
                loX = nLoX; hiX = nHiX; loY = nLoY; hiY = nHiY;
            }

            dist = Mathf.Max(minDistance, Need(loX, hiX, loY, hiY, tanV, tanH));
            float cx = (loX + hiX) * 0.5f;
            // keep the walls at the frame edges where possible, then make sure nobody was pushed out
            for (int i = 0; i < 2; i++)
            {
                float half = dist * tanH;
                if (half < wallX) cx = Mathf.Clamp(cx, -wallX + half, wallX - half);
                float needHalf = Mathf.Max(cx - loX, hiX - cx) + MarginX;
                dist = Mathf.Max(dist, needHalf / tanH);
            }
            dist = Mathf.Min(dist, maxDistance + 2f);
            // vertical: centre on the group, a little low so the HUD at the top never covers a head
            float cy = (loY + hiY) * 0.5f + 0.15f;
            center = new Vector3(cx, cy, 0f);
        }

        const float MarginX = 1.3f;

        static float Need(float loX, float hiX, float loY, float hiY, float tanV, float tanH)
        {
            float halfW = (hiX - loX) * 0.5f + MarginX;
            float halfH = (hiY - loY) * 0.5f + 1.0f;      // room above heads for the HUD, below feet for the floor
            return Mathf.Max(halfW / tanH, halfH / tanV);
        }

        /// <summary>Slide the camera (and its aim, together) so the hero's feet and head sit inside
        /// a safe frame: clear of the screen sides, and below the HUD at the top. Works for any
        /// shot, including orbits, in the camera's own right/up directions.</summary>
        void KeepHeroInShot(ref Vector3 pos, ref Vector3 look)
        {
            if (hero == null) return;
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(0.5f, cam.aspect);
            const float limX = 0.86f, limTop = 0.74f, limBottom = -0.88f;
            for (int pass = 0; pass < 6; pass++)
            {
                var rot = Quaternion.LookRotation(look - pos, Vector3.up);
                var inv = Quaternion.Inverse(rot);
                Vector3 shift = Vector3.zero;
                var cf = inv * (hero.position + Vector3.up * 0.05f - pos);
                var ch = inv * (hero.position + Vector3.up * BodyHeight - pos);
                if (cf.z < 0.5f || ch.z < 0.5f)
                    shift.z = Mathf.Min(cf.z, ch.z) - 3f;                         // he is behind us: back right off
                else
                {
                    float fy = cf.y / (cf.z * tanV), hy = ch.y / (ch.z * tanV);
                    float band = limTop - limBottom;
                    if (hy - fy > band * 0.95f)
                        shift.z = -(ch.z * ((hy - fy) / (band * 0.9f) - 1f) + 0.5f);   // too close to fit him: back away
                    else
                    {
                        foreach (var c in new[] { cf, ch })
                        {
                            float hx = c.x / (c.z * tanH);
                            if (hx > limX) shift.x = Mathf.Max(shift.x, (hx - limX) * c.z * tanH);
                            if (hx < -limX) shift.x = Mathf.Min(shift.x, (hx + limX) * c.z * tanH);
                        }
                        if (hy > limTop) shift.y = (hy - limTop) * ch.z * tanV;
                        else if (fy < limBottom) shift.y = (fy - limBottom) * cf.z * tanV;
                    }
                }
                if (shift == Vector3.zero) break;
                var world = rot * shift;
                pos += world;
                if (shift.z == 0f) look += world;
            }
        }

        void Snap()
        {
            focus += Vector3.up * BodyHeight * 0.5f;
            transform.position = focus + new Vector3(0f, distance * 0.1f, -distance);
            transform.LookAt(focus);
        }

        void LateUpdate()
        {
            if (hero == null) return;
            float dt = UnityEngine.Time.unscaledDeltaTime;

            // frame the hero and everyone in the fight, on both floors (see Frame)
            Frame(out Vector3 target, out float want);
            focus = Vector3.SmoothDamp(focus, target, ref focusVel, followSmooth, Mathf.Infinity, dt);
            distance = Mathf.SmoothDamp(distance, want, ref distanceVel, 0.3f, Mathf.Infinity, dt);

            // a slight downward look, so both floors and the floor itself read
            Vector3 pos = focus + new Vector3(0f, distance * 0.1f, -distance);
            Vector3 look = focus;

            if (focusTarget != null && UnityEngine.Time.unscaledTime < focusUntil)
            {
                float k = Mathf.Clamp01((focusUntil - UnityEngine.Time.unscaledTime) / 0.4f);
                var fp = focusTarget.position + Vector3.up * 1.1f;
                // a close shot of the subject that still holds the hero: centre between them,
                // pulled back as far as their separation needs
                var hp = hero.position + Vector3.up * 0.8f;
                float tanH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Max(0.5f, cam.aspect);
                bool both = focusTarget != hero && Vector3.Distance(hp, fp) < 9f;
                var c = both ? (fp + hp) * 0.5f : fp;
                float back = both ? Mathf.Max(3.6f, (Mathf.Abs(fp.x - hp.x) * 0.5f + 1.4f) / tanH) : 3.6f;
                pos = Vector3.Lerp(pos, c + new Vector3(-1.0f, 0.25f, -back), 0.85f * k);
                look = Vector3.Lerp(look, c, 0.85f * k);
            }

            if (scriptWeight > 0.0001f)
            {
                pos = Vector3.Lerp(pos, scriptPos, scriptWeight);
                look = Vector3.Lerp(look, scriptLook, scriptWeight);
            }

            if (orbiting)
            {
                float u = (UnityEngine.Time.unscaledTime - orbitStart) / orbitDuration;
                if (u >= 1f) orbiting = false;
                else
                {
                    // out and back: ease to the far angle, then return to the gameplay side
                    float w = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                    float ang = orbitAngle * Mathf.SmoothStep(0f, 1f, w);
                    var rot = Quaternion.Euler(-8f * w, ang, 0f);
                    // orbit the middle of the kill and the hero, wide enough to keep him in it
                    var hp = hero.position + Vector3.up * 0.8f;
                    bool both = Vector3.Distance(hp, orbitPivot) < 8f;
                    var mid = both ? (orbitPivot + hp) * 0.5f : orbitPivot;
                    float radius = both ? Mathf.Max(2.9f, Vector3.Distance(hp, orbitPivot) * 1.15f + 1.6f) : 2.9f;
                    var close = mid + rot * new Vector3(0, 0.4f, -radius);
                    pos = Vector3.Lerp(pos, close, w);
                    look = Vector3.Lerp(look, mid + Vector3.up * 0.2f, w);
                }
            }

            // the guarantee: whatever the shot, the hero stays in it
            if (scriptWeight < 0.01f) KeepHeroInShot(ref pos, ref look);

            if (UnityEngine.Time.unscaledTime < shakeUntil)
            {
                float k = (shakeUntil - UnityEngine.Time.unscaledTime) * 8f;
                float a = shakeAmp * Mathf.Clamp01(k);
                float n = UnityEngine.Time.unscaledTime * 60f;
                pos += new Vector3(Mathf.PerlinNoise(n, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, n) - 0.5f, 0f) * (2f * a);
            }
            else shakeAmp = 0f;

            float fov = baseFov;
            if (UnityEngine.Time.unscaledTime < punchUntil) fov -= punchAmount;
            if (scriptFov > 0f && scriptWeight > 0.0001f)
            {
                fov = Mathf.Lerp(fov, scriptFov, scriptWeight);
                if (scriptWeight > 0.999f) cam.fieldOfView = fov;   // hard cuts between shots
            }
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-25f * dt));

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }
    }
}
