using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace ProjectInk
{
    public enum Command { None, StrikeLeft, StrikeRight, JumpKick, DodgeLeft, DodgeRight, Restart, CycleStyle, StyleFists, StyleSword, StyleGun, ThrowWeapon, Rampage }

    /// <summary>Turns touches (or keys in the editor) into fight commands.
    /// Tap left/right half = strike that side · swipe up = flying kick ·
    /// swipe left/right = dodge that way · swipe down = throw the weapon in hand ·
    /// two-finger tap = Rampage (when the Flow meter is full).
    /// Keys: A/D or arrows strike, W/Up jump attack, Q/E dodge, S/Down/F throw, Space rampage,
    /// Tab or 1/2/3 give a weapon (testing), R restart. Commands queue briefly so fast taps are never lost.</summary>
    public class GestureInput : MonoBehaviour
    {
        public static GestureInput Instance { get; private set; }

        [Tooltip("Swipe length as a fraction of screen height.")]
        public float swipeFraction = 0.08f;
        [Tooltip("How long a queued command stays valid (input buffer), real seconds.")]
        public float bufferSeconds = 0.25f;

        struct Pending { public Command cmd; public float time; }
        readonly Queue<Pending> queue = new Queue<Pending>();
        readonly Dictionary<int, Vector2> touchStart = new Dictionary<int, Vector2>();
        readonly Dictionary<int, float> touchTime = new Dictionary<int, float>();
        readonly HashSet<int> consumed = new HashSet<int>();

        /// <summary>A new touch while another began moments ago = two-finger tap.</summary>
        void TouchBegan(int id, Vector2 pos)
        {
            float now = UnityEngine.Time.unscaledTime;
            foreach (var kv in touchTime)
            {
                if (kv.Key != id && now - kv.Value < 0.15f && !consumed.Contains(kv.Key))
                {
                    consumed.Add(kv.Key);
                    consumed.Add(id);
                    Push(Command.Rampage);
                    break;
                }
            }
            touchStart[id] = pos;
            touchTime[id] = now;
        }

        void TouchEnded(int id, Vector2 pos)
        {
            if (!consumed.Remove(id) && touchStart.TryGetValue(id, out var s)) Push(Classify(s, pos));
            touchStart.Remove(id);
            touchTime.Remove(id);
        }
        Vector2 mouseStart;
        bool mouseDown;

        public bool AnyPressThisFrame { get; private set; }

        void Awake()
        {
            Instance = this;
#if !ENABLE_INPUT_SYSTEM
            Input.simulateMouseWithTouches = false; // touches are read directly; avoid double taps
#endif
        }

#if ENABLE_INPUT_SYSTEM
        void OnEnable() { EnhancedTouchSupport.Enable(); }
        void OnDisable() { EnhancedTouchSupport.Disable(); }
#endif

        /// <summary>Next buffered command, or None. Expired commands are dropped.</summary>
        public Command Next()
        {
            float now = UnityEngine.Time.unscaledTime;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (now - p.time <= bufferSeconds) return p.cmd;
            }
            return Command.None;
        }

        public Command Peek()
        {
            float now = UnityEngine.Time.unscaledTime;
            while (queue.Count > 0 && now - queue.Peek().time > bufferSeconds) queue.Dequeue();
            return queue.Count > 0 ? queue.Peek().cmd : Command.None;
        }

        void Push(Command c)
        {
            if (c == Command.None) return;
            if (queue.Count >= 3) queue.Dequeue();
            queue.Enqueue(new Pending { cmd = c, time = UnityEngine.Time.unscaledTime });
        }

        Command Classify(Vector2 start, Vector2 end)
        {
            Vector2 d = end - start;
            float min = Screen.height * swipeFraction;
            if (d.magnitude < min) return start.x < Screen.width * 0.5f ? Command.StrikeLeft : Command.StrikeRight;
            if (Mathf.Abs(d.y) > Mathf.Abs(d.x)) return d.y > 0 ? Command.JumpKick : Command.ThrowWeapon;
            return d.x < 0 ? Command.DodgeLeft : Command.DodgeRight;
        }

        void Update()
        {
            AnyPressThisFrame = false;
#if ENABLE_INPUT_SYSTEM
            foreach (var t in ETouch.activeTouches)
            {
                int id = t.touchId;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    TouchBegan(id, t.screenPosition);
                    AnyPressThisFrame = true;
                }
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    TouchEnded(id, t.screenPosition);
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Push(Command.StrikeLeft);
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Push(Command.StrikeRight);
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Push(Command.JumpKick);
                if (kb.qKey.wasPressedThisFrame) Push(Command.DodgeLeft);
                if (kb.eKey.wasPressedThisFrame) Push(Command.DodgeRight);
                if (kb.rKey.wasPressedThisFrame) Push(Command.Restart);
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) Push(Command.ThrowWeapon);
                if (kb.spaceKey.wasPressedThisFrame) Push(Command.Rampage);
                if (kb.tabKey.wasPressedThisFrame) Push(Command.CycleStyle);
                if (kb.digit1Key.wasPressedThisFrame) Push(Command.StyleFists);
                if (kb.digit2Key.wasPressedThisFrame) Push(Command.StyleSword);
                if (kb.digit3Key.wasPressedThisFrame) Push(Command.StyleGun);
                if (kb.anyKey.wasPressedThisFrame) AnyPressThisFrame = true;
            }
            var mouse = Mouse.current;
            if (mouse != null && ETouch.activeTouches.Count == 0)
            {
                if (mouse.leftButton.wasPressedThisFrame) { mouseStart = mouse.position.ReadValue(); mouseDown = true; AnyPressThisFrame = true; }
                if (mouse.leftButton.wasReleasedThisFrame && mouseDown) { mouseDown = false; Push(Classify(mouseStart, mouse.position.ReadValue())); }
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    TouchBegan(t.fingerId, t.position);
                    AnyPressThisFrame = true;
                }
                else if (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled)
                    TouchEnded(t.fingerId, t.position);
            }
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) { mouseStart = Input.mousePosition; mouseDown = true; AnyPressThisFrame = true; }
                if (Input.GetMouseButtonUp(0) && mouseDown) { mouseDown = false; Push(Classify(mouseStart, Input.mousePosition)); }
            }
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Push(Command.StrikeLeft);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Push(Command.StrikeRight);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Push(Command.JumpKick);
            if (Input.GetKeyDown(KeyCode.Q)) Push(Command.DodgeLeft);
            if (Input.GetKeyDown(KeyCode.E)) Push(Command.DodgeRight);
            if (Input.GetKeyDown(KeyCode.R)) Push(Command.Restart);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.F)) Push(Command.ThrowWeapon);
            if (Input.GetKeyDown(KeyCode.Space)) Push(Command.Rampage);
            if (Input.GetKeyDown(KeyCode.Tab)) Push(Command.CycleStyle);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Push(Command.StyleFists);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Push(Command.StyleSword);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Push(Command.StyleGun);
            if (Input.anyKeyDown) AnyPressThisFrame = true;
#endif
        }
    }
}
