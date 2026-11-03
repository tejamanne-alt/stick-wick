using UnityEngine;

namespace ProjectInk
{
    /// <summary>A hanging lamp: sways gently on its cord, and now and then its light stutters.
    /// Fights shake it (a hit nearby gives it a push).</summary>
    public class LampFlicker : MonoBehaviour
    {
        public Renderer[] lights;            // additive glow and cone (legacy particle shaders: _TintColor)
        public Color baseTint = new Color(0.5f, 0.38f, 0.22f, 0.5f);
        public float swayDegrees = 2.5f;
        public float swaySpeed = 0.7f;

        static readonly int TintId = Shader.PropertyToID("_TintColor");
        MaterialPropertyBlock block;
        float phase, flickerUntil, nextFlicker, kick;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            phase = Random.value * 10f;
            nextFlicker = Time.time + Random.Range(4f, 14f);
        }

        /// <summary>Give the lamp a shove (big hits, falls).</summary>
        public void Shake(float amount) { kick = Mathf.Max(kick, amount); }

        void Update()
        {
            float t = Time.time;
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 4f);
            float angle = Mathf.Sin(t * swaySpeed + phase) * swayDegrees + Mathf.Sin(t * 9f) * kick * 12f;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (t > nextFlicker) { flickerUntil = t + Random.Range(0.15f, 0.5f); nextFlicker = t + Random.Range(5f, 16f); }
            float k = 1f;
            if (t < flickerUntil) k = Random.value < 0.5f ? 0.15f : 0.9f;
            var c = new Color(baseTint.r * k, baseTint.g * k, baseTint.b * k, baseTint.a);
            if (lights == null) return;
            foreach (var r in lights)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(TintId, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
