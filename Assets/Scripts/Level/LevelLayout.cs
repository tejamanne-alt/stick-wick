using UnityEngine;

namespace ProjectInk
{
    /// <summary>Which building this scene is (the classic hall, or the tower). Sets the roof
    /// height for the physics and the platform map for navigation before anything moves.</summary>
    [DefaultExecutionOrder(-100)]
    public class LevelLayout : MonoBehaviour
    {
        public static LevelLayout Instance { get; private set; }
        public LayoutKind kind = LayoutKind.Hall;
        public float roofY = 6.8f;

        public static LayoutKind Kind => Instance != null ? Instance.kind : LayoutKind.Hall;

        void Awake()
        {
            Instance = this;
            Fighter.RoofY = roofY;
            LevelNav.Init(kind);
        }
    }
}
