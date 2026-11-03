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

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            baseFov = cam.fieldOfView;
        }

        void Start()
        {
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

        void Snap()
        {
            transform.position = focus + new Vector3(0, height, -distance);
            transform.LookAt(focus + Vector3.up * (lookHeight - 0.2f));
        }

        void LateUpdate()
        {
            if (hero == null) return;
            float dt = UnityEngine.Time.unscaledDeltaTime;

            // frame hero + attackers
            float lo = hero.position.x, hi = hero.position.x;
            float yLo = hero.position.y, yHi = hero.position.y;
            foreach (var t in extras)
            {
                if (t == null) continue;
                if (Mathf.Abs(t.position.x - hero.position.x) > 9f) continue;
                lo = Mathf.Min(lo, t.position.x);
                hi = Mathf.Max(hi, t.position.x);
                yLo = Mathf.Min(yLo, t.position.y);
                yHi = Mathf.Max(yHi, t.position.y);
            }
            float cx = Mathf.Lerp(hero.position.x, (lo + hi) * 0.5f, 0.6f);
            cx = Mathf.Clamp(cx, minX, maxX);
            var target = new Vector3(cx, (yLo + yHi) * 0.5f, 0f);
            float halfWidth = (hi - lo) * 0.5f + 2.2f;
            float halfHeight = (yHi - yLo) * 0.5f + 1.6f;
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float want = Mathf.Max(halfWidth / (tanHalf * Mathf.Max(0.5f, cam.aspect)), halfHeight / tanHalf);
            want = Mathf.Clamp(want, minDistance, maxDistance);

            focus = Vector3.SmoothDamp(focus, target, ref focusVel, followSmooth, Mathf.Infinity, dt);
            distance = Mathf.SmoothDamp(distance, want, ref distanceVel, 0.35f, Mathf.Infinity, dt);

            Vector3 pos = focus + new Vector3(0, height, -distance);
            Vector3 look = focus + Vector3.up * lookHeight;

            if (focusTarget != null && UnityEngine.Time.unscaledTime < focusUntil)
            {
                float k = Mathf.Clamp01((focusUntil - UnityEngine.Time.unscaledTime) / 0.4f);
                var fp = focusTarget.position + Vector3.up * 1.4f;
                pos = Vector3.Lerp(pos, fp + new Vector3(-1.2f, 0.3f, -4.2f), 0.85f * k);
                look = Vector3.Lerp(look, fp, 0.85f * k);
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
                    var close = orbitPivot + rot * new Vector3(0, 0.5f, -3.4f);
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
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-25f * dt));

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }
    }
}
