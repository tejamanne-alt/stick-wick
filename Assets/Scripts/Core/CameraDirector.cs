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
            maxDistance = Mathf.Max(maxDistance, 22f);
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
                pos = Vector3.Lerp(pos, fp + new Vector3(-1.0f, 0.25f, -3.6f), 0.85f * k);
                look = Vector3.Lerp(look, fp, 0.85f * k);
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
                    var close = orbitPivot + rot * new Vector3(0, 0.4f, -2.9f);
                    pos = Vector3.Lerp(pos, close, w);
                    look = Vector3.Lerp(look, orbitPivot + Vector3.up * 0.2f, w);
                }
            }

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
