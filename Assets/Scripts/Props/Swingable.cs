using System.Collections.Generic;
using UnityEngine;

namespace ProjectInk
{
    public enum SwingKind { Rope, Bar }

    /// <summary>Something to swing on. A rope hangs from an anchor and sways; the hero grabs its
    /// end, pumps it like a pendulum, kicks enemies in passing and lets go into a flying kick.
    /// A bar is a steel rod (end-on to the camera): the hero grabs it and does giant circles.</summary>
    public class Swingable : MonoBehaviour
    {
        public static readonly List<Swingable> All = new List<Swingable>();

        public SwingKind kind;
        [Tooltip("Rope: the top anchor. Bar: the bar's centre.")]
        public Vector3 anchor;
        public float length = 4f;
        public LineRenderer line;
        [Header("Trapeze: a rod hung from two threads, swinging rigidly like a swing seat")]
        public bool trapeze;
        public float rodHalf = 0.45f;
        [Tooltip("How far it may swing each way (radians): keeps a long swing off the walls.")]
        public float minAngle = -1.4f, maxAngle = 1.4f;
        public LineRenderer threadA, threadB;
        public Transform rod;

        float angle, angVel;
        public bool Held { get; set; }
        public float Angle => angle;

        /// <summary>Where the hands go: the rope's end, or the bar.</summary>
        public Vector3 Grip => kind == SwingKind.Bar ? anchor : anchor + new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f) * length;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            angle = Random.Range(-0.05f, 0.05f);
        }

        /// <summary>The hero drives the rope while holding it.</summary>
        public void SetAngle(float a) { angle = a; }
        public void LetGo(float velocity) { Held = false; angVel = velocity * 0.4f; }

        void LateUpdate()
        {
            if (kind != SwingKind.Rope) return;
            if (!Held)
            {
                // a free rope keeps swaying, and settles
                float dt = Time.deltaTime;
                angVel += -(22f / length) * Mathf.Sin(angle) * dt;
                angVel *= 1f - 0.6f * dt;
                angle += angVel * dt;
            }
            if (line != null)
            {
                line.SetPosition(0, anchor);
                line.SetPosition(1, Grip);
            }
            if (trapeze)
            {
                // the two threads stay parallel, so the rod stays level with the swing (a rigid swing)
                var along = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * rodHalf;
                var g = Grip;
                if (threadA != null) { threadA.SetPosition(0, anchor + Vector3.left * rodHalf); threadA.SetPosition(1, g - along); }
                if (threadB != null) { threadB.SetPosition(0, anchor + Vector3.right * rodHalf); threadB.SetPosition(1, g + along); }
                if (rod != null) { rod.position = g; rod.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg); }
            }
        }

        /// <summary>The nearest grip the hands can reach from a fighter's position.</summary>
        public static Swingable Reachable(Vector3 feet, float handReach, float slack)
        {
            Swingable best = null;
            float bestD = float.MaxValue;
            foreach (var s in All)
            {
                var g = s.Grip;
                float dx = Mathf.Abs(g.x - feet.x);
                float above = g.y - feet.y;
                float minAbove = s.kind == SwingKind.Bar ? -0.05f : 0.6f;   // a bar can be just above the hands
                if (dx > slack || above < minAbove || above > handReach) continue;
                if (dx < bestD) { bestD = dx; best = s; }
            }
            return best;
        }
    }
}
