using UnityEngine;

namespace ProjectInk
{
    /// <summary>Swings back and forth about Z (a searchlight beam through the skylight).</summary>
    public class Sweep : MonoBehaviour
    {
        public float amplitude = 18f, period = 9f, phase;
        float baseZ;
        void Awake() { baseZ = transform.localEulerAngles.z; }
        void Update()
        {
            float a = Mathf.Sin((Time.unscaledTime / period + phase) * Mathf.PI * 2f) * amplitude;
            var e = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(e.x, e.y, baseZ + a);
        }
    }
}
