using UnityEngine;

namespace ProjectInk
{
    /// <summary>A door-man's sheet of iron, torn out of his hands: it tumbles to the floor, clangs,
    /// and lies there.</summary>
    public class ShieldFall : MonoBehaviour
    {
        public Vector3 velocity;
        bool landed;

        void Update()
        {
            if (landed) return;
            float dt = Time.deltaTime;
            velocity.y -= 18f * dt;
            transform.position += velocity * dt;
            transform.Rotate(0f, 0f, -velocity.x * 60f * dt, Space.World);
            float floor = Fighter.GroundY(transform.position + Vector3.up * 0.3f, 0.6f) + 0.04f;
            if (transform.position.y <= floor && velocity.y < 0f)
            {
                landed = true;
                transform.position = new Vector3(transform.position.x, floor, transform.position.z);
                transform.rotation = Quaternion.Euler(0f, 30f, 90f);
                Sound.Play(Sfx.MetalClang, 0.9f, transform.position, 0.7f);
            }
        }
    }
}
