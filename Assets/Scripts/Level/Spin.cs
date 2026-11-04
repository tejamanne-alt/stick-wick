using UnityEngine;

namespace ProjectInk
{
    /// <summary>Turns slowly about its own Z (the big ventilation fan). Real time, so it keeps
    /// turning in slow motion, only slower: it reads the time scale for a heavy, filmic drag.</summary>
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 40f;
        void Update()
        {
            float k = Mathf.Lerp(0.25f, 1f, Time.timeScale);
            transform.Rotate(0f, 0f, degreesPerSecond * k * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
