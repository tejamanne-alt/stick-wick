using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace ProjectInk
{
    public enum Command
    {
        None, StrikeLeft, StrikeRight, JumpKick, DodgeLeft, DodgeRight, Restart, CycleStyle,
        StyleFists, StyleSword, StyleGun, ThrowWeapon, Rampage, StrikeForward, DodgeForward,
        KickLeft, KickRight, KickForward, LockNext,
        DropDown,             // two swipes down: drop through the floor (a one-way platform)
        Interact,             // a tap on a thing (a weapon on the floor, a drum): go and use it
        JumpLeft, JumpRight   // a swipe up and to the side: a jump that way
    }

    public enum ControlScheme { Classic, TwoThumb }

    /// <summary>Two control schemes (C toggles; the choice is saved).
    /// CLASSIC (default): tap the left or right half of the screen = strike that way (the hero
    /// dashes in); swipe up = jump kick; swipe left/right = dodge that way; swipe down = throw;
    /// two-finger tap = Rampage; press and HOLD a half = run that way (ropes, bars, walls).
    /// Keys: A/D or arrows strike (hold to run) · W/Up jump · Q/E dodge · S/Down/F throw ·
    /// Space rampage · R restart.
    /// TWO-THUMB: LEFT side (left 40% of the screen): a floating stick. Touch anywhere, drag left or right
    /// to run; flick up to jump.
    /// RIGHT side: tap = strike (towards the side of the hero you tapped, or the way you run);
    /// swipe up = jump / leap / climb; swipe left or right = dodge that way (vault, slide, wall
    /// flip by context); swipe down = duck, or throw the weapon in hand; two-finger tap = Rampage.
    /// Keys: A/D or arrows run · J strike · Space/W/Up jump · L or Shift dodge · S/Down duck or
    /// throw · F throw · E rampage · R restart · Tab or 1/2/3 give a weapon (testing).
    /// Commands queue briefly so fast taps are never lost.</summary>
    public class GestureInput : MonoBehaviour
    {
        public static GestureInput Instance { get; private set; }

        public Transform hero;
        public ControlScheme scheme = ControlScheme.Classic;
        [Tooltip("Taps above this fraction of the screen height punch; below it, kick.")]
        public float kickLine = 0.45f;
        [Tooltip("Classic: how long a still press must be held before it becomes a run, real seconds.")]
        public float holdToRun = 0.22f;
        public bool Classic => scheme == ControlScheme.Classic;
        [Tooltip("Width of the movement zone, as a fraction of the screen width.")]
        public float stickZone = 0.4f;
        [Tooltip("Stick travel for full speed, as a fraction of screen height.")]
        public float stickRange = 0.09f;
        [Tooltip("Swipe length as a fraction of screen height.")]
        public float swipeFraction = 0.08f;
        [Tooltip("How long a queued command stays valid (input buffer), real seconds.")]
        public float bufferSeconds = 0.25f;

        struct Pending { public Command cmd; public float time; }
        readonly Queue<Pending> queue = new Queue<Pending>();

        // touches
        readonly Dictionary<int, Vector2> touchStart = new Dictionary<int, Vector2>();
        readonly Dictionary<int, float> touchTime = new Dictionary<int, float>();
        readonly HashSet<int> consumed = new HashSet<int>();
        readonly Dictionary<int, int> holding = new Dictionary<int, int>();   // classic: touch id -> run direction
        float keyDownAt = -1f;
        int stickId = int.MinValue;
        Vector2 stickOrigin, stickNow;
        bool stickFlicked;
        float keyAxis;

        public bool AnyPressThisFrame { get; private set; }
        /// <summary>The last swipe started in the upper part of the screen (the clinch: high throws).</summary>
        public bool LastSwipeHigh { get; private set; }
        /// <summary>A press being held still: 0 none, 1 high (upper part of the screen, or A/D held),
        /// 2 low (lower part, or Z/X held). The clinch reads it: hold high = the choke, low = the lift.</summary>
        public int HoldHeight { get; private set; }
        float lowKeyAt = -1f;
        float lastDownSwipeAt = -9f;
        /// <summary>The thing a tap landed on (Command.Interact): a WeaponPickup or an OilDrum.</summary>
        public static Component InteractTarget;
        /// <summary>The man a tap landed on (his body), and when: a knife in hand is thrown at him.</summary>
        public static Enemy TappedEnemy;
        public static float TappedAt = -9f;
        // the crouch: a swipe down that stays held; sliding that finger sideways creeps
        int crouchId = int.MinValue;
        Vector2 crouchAnchor, crouchNow;
        bool keyCrouch;
        float keyCrouchAxis;
        /// <summary>Crouching (a swipe down held, or C on a keyboard).</summary>
        public bool CrouchHeld => crouchId != int.MinValue || keyCrouch;
        /// <summary>The creep while crouched, -1 .. +1 (the held finger slid sideways, or A/D with C).</summary>
        public float CrouchAxis { get; private set; }
        /// <summary>Run input, -1 (left) .. +1 (right).</summary>
        public float MoveAxis { get; private set; }
        public bool StickActive => stickId != int.MinValue;
        public Vector2 StickOrigin => stickOrigin;
        public Vector2 StickNow => stickNow;

        void Awake()
        {
            Instance = this;
            // classic controls, always. (A C-key toggle used to switch and save this; a stray press
            // next to the Z/X kick keys could leave a player on the stick, so it is gone, and any
            // saved choice from it is cleared.)
            scheme = ControlScheme.Classic;
            if (PlayerPrefs.HasKey("controls")) { PlayerPrefs.DeleteKey("controls"); PlayerPrefs.Save(); }
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

        /// <summary>Forget every buffered command (taps made during a cutscene).</summary>
        public void Flush() { queue.Clear(); }

        public Command Peek()
        {
            float now = UnityEngine.Time.unscaledTime;
            while (queue.Count > 0 && now - queue.Peek().time > bufferSeconds) queue.Dequeue();
            return queue.Count > 0 ? queue.Peek().cmd : Command.None;
        }

        void Push(Command c)
        {
            if (c == Command.None || TimeController.Paused) return;
            if (queue.Count >= 3) queue.Dequeue();
            queue.Enqueue(new Pending { cmd = c, time = UnityEngine.Time.unscaledTime });
        }

        /// <summary>Which side of the hero a screen point is on.</summary>
        int SideOf(Vector2 screen)
        {
            if (Mathf.Abs(MoveAxis) > 0.3f) return MoveAxis > 0 ? 1 : -1;     // running: strike that way
            var cam = Camera.main;
            if (hero == null || cam == null) return screen.x < Screen.width * 0.5f ? -1 : 1;
            float hx = cam.WorldToScreenPoint(hero.position + Vector3.up * 0.8f).x;
            return screen.x < hx ? -1 : 1;
        }

        /// <summary>Switch scheme (for a future settings menu; no key is bound to it).</summary>
        public void ToggleScheme()
        {
            scheme = Classic ? ControlScheme.TwoThumb : ControlScheme.Classic;
            stickId = int.MinValue;
            holding.Clear();
        }

        Command Classify(Vector2 start, Vector2 end)
        {
            Vector2 d = end - start;
            float min = Screen.height * swipeFraction;
            if (d.magnitude < min)
            {
                var thing = ThingAt(start);
                if (thing != null && EnemyAt(start) == null) { InteractTarget = thing; return Command.Interact; }
                // tap high = punch, tap low = kick
                bool kick = start.y < Screen.height * kickLine;
                int side = Classic ? (start.x < Screen.width * 0.5f ? -1 : 1) : SideOf(start);
                // a tap on an enemy picks him: he is the target from now on, and this strike goes his way
                var picked = EnemyAt(start);
                if (picked != null) { TappedEnemy = picked; TappedAt = UnityEngine.Time.unscaledTime; }
                if (picked != null && hero != null)
                {
                    var hc = hero.GetComponent<HeroController>();
                    if (hc != null)
                    {
                        hc.LockOn(picked);
                        side = picked.transform.position.x >= hero.position.x ? 1 : -1;
                        kick = false;
                    }
                }
                if (kick) return side < 0 ? Command.KickLeft : Command.KickRight;
                return side < 0 ? Command.StrikeLeft : Command.StrikeRight;
            }
            LastSwipeHigh = start.y >= Screen.height * kickLine;
            // up and to the side (between about 28 and 62 degrees): a jump that way
            float ang = Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
            if (d.y > 0f && ang > 28f && ang < 62f) return d.x < 0f ? Command.JumpLeft : Command.JumpRight;
            if (Mathf.Abs(d.y) > Mathf.Abs(d.x))
            {
                if (d.y > 0) return Command.JumpKick;
                return DownSwipe();
            }
            return d.x < 0 ? Command.DodgeLeft : Command.DodgeRight;
        }

        /// <summary>The enemy under a screen point (pixels), if any: his body, with a generous
        /// margin for thumbs.</summary>
        public static Enemy EnemyAt(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            float pad = Screen.height * 0.035f;
            Enemy best = null;
            float bestD = float.MaxValue;
            foreach (var e in Enemy.All)
            {
                if (!e.Alive || e.Fleeing || !e.Targetable) continue;
                var feet = cam.WorldToScreenPoint(e.transform.position);
                var head = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 1.4f * e.transform.localScale.y);
                if (feet.z <= 0f) continue;
                float hgt = head.y - feet.y;
                float half = Mathf.Max(hgt * 0.32f, Screen.height * 0.03f) + pad;
                if (screen.x < feet.x - half || screen.x > feet.x + half || screen.y < feet.y - pad || screen.y > head.y + pad) continue;
                float d = Mathf.Abs(screen.x - feet.x) + Mathf.Abs(screen.y - (feet.y + head.y) * 0.5f) * 0.5f;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        bool InStickZone(Vector2 p) => !Classic && p.x < Screen.width * stickZone;

        /// <summary>One swipe down throws the weapon in hand (or ducks); a second within half a second
        /// drops through the floor.</summary>
        Command DownSwipe()
        {
            float now = UnityEngine.Time.unscaledTime;
            if (now - lastDownSwipeAt < 0.45f) { lastDownSwipeAt = -9f; return Command.DropDown; }
            lastDownSwipeAt = now;
            return Command.ThrowWeapon;
        }

        /// <summary>A weapon lying on the floor, or a standing drum, under a screen point.</summary>
        public static Component ThingAt(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            float pad = Screen.height * 0.05f;
            Component best = null;
            float bestD = float.MaxValue;
            foreach (var w in WeaponPickup.All)
            {
                if (w == null) continue;
                var c = cam.WorldToScreenPoint(w.transform.position);
                if (c.z <= 0f) continue;
                float dd = Vector2.Distance(screen, c);
                if (dd < pad * 1.4f && dd < bestD) { bestD = dd; best = w; }
            }
            foreach (var dr in OilDrum.All)
            {
                if (dr == null || !dr.Available || dr.Rolling) continue;
                var lo = cam.WorldToScreenPoint(new Vector3(dr.transform.position.x, dr.FloorY, dr.transform.position.z));
                var hi = cam.WorldToScreenPoint(new Vector3(dr.transform.position.x, dr.FloorY + OilDrum.Height, dr.transform.position.z));
                if (lo.z <= 0f) continue;
                float half = Mathf.Abs(hi.y - lo.y) * 0.4f + pad * 0.5f;
                if (screen.x < lo.x - half || screen.x > lo.x + half || screen.y < lo.y - pad * 0.5f || screen.y > hi.y + pad * 0.5f) continue;
                float dd = Mathf.Abs(screen.x - lo.x);
                if (dd < bestD) { bestD = dd; best = dr; }
            }
            return best;
        }

        /// <summary>Screen rects (pixels, origin bottom-left) owned by on-screen buttons: touches
        /// starting there are not fight input. Set each frame by the HUD.</summary>
        public static readonly System.Collections.Generic.List<Rect> Blocked = new System.Collections.Generic.List<Rect>();
        /// <summary>The last press on an on-screen button (pixels), for the HUD to act on.</summary>
        public static Vector2? LastButtonTap;

        void TouchBegan(int id, Vector2 pos)
        {
            foreach (var r in Blocked) if (r.Contains(pos)) { consumed.Add(id); LastButtonTap = pos; return; }
            if (TimeController.Paused) { consumed.Add(id); return; }
            AnyPressThisFrame = true;
            if (InStickZone(pos) && stickId == int.MinValue)
            {
                stickId = id;
                stickOrigin = stickNow = pos;
                stickFlicked = false;
                return;
            }
            float now = UnityEngine.Time.unscaledTime;
            foreach (var kv in touchTime)
            {
                if (kv.Key != id && kv.Key != stickId && now - kv.Value < 0.15f && !consumed.Contains(kv.Key))
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

        void TouchMoved(int id, Vector2 pos)
        {
            if (id == crouchId) { crouchNow = pos; return; }
            if (Classic)
            {
                // a still press held a moment becomes a run towards that half of the screen
                if (holding.ContainsKey(id) || consumed.Contains(id)) return;
                if (!touchStart.TryGetValue(id, out var s0) || !touchTime.TryGetValue(id, out var t0)) return;
                // a swipe down that stays held: the crouch (no swipe fires when it is let go)
                var dd0 = pos - s0;
                if (crouchId == int.MinValue && -dd0.y > Screen.height * swipeFraction && -dd0.y > Mathf.Abs(dd0.x) * 1.2f &&
                    UnityEngine.Time.unscaledTime - t0 > 0.16f)
                {
                    crouchId = id;
                    crouchAnchor = crouchNow = pos;
                    consumed.Add(id);
                    return;
                }
                if (UnityEngine.Time.unscaledTime - t0 > holdToRun && (pos - s0).magnitude < Screen.height * swipeFraction)
                    holding[id] = s0.x < Screen.width * 0.5f ? -1 : 1;
                return;
            }
            if (id != stickId) return;
            stickNow = pos;
            // flick up on the stick = jump (once per flick)
            float up = (stickNow.y - stickOrigin.y) / Screen.height;
            if (!stickFlicked && up > swipeFraction) { stickFlicked = true; Push(Command.JumpKick); }
            if (up < swipeFraction * 0.4f) stickFlicked = false;
        }

        void TouchEnded(int id, Vector2 pos)
        {
            if (id == crouchId) { crouchId = int.MinValue; consumed.Remove(id); touchStart.Remove(id); touchTime.Remove(id); return; }
            if (id == stickId) { stickId = int.MinValue; return; }
            if (holding.Remove(id)) { consumed.Remove(id); touchStart.Remove(id); touchTime.Remove(id); return; }
            if (!consumed.Remove(id) && touchStart.TryGetValue(id, out var s)) Push(Classify(s, pos));
            touchStart.Remove(id);
            touchTime.Remove(id);
        }

        /// <summary>Classic keys: a press of A/D strikes that way at once; keep holding to run.</summary>
        void ClassicKeys(bool left, bool right, bool leftDown, bool rightDown)
        {
            float now = UnityEngine.Time.unscaledTime;
            if (leftDown) { Push(Command.StrikeLeft); keyDownAt = now; }
            if (rightDown) { Push(Command.StrikeRight); keyDownAt = now; }
            if (!left && !right) { keyDownAt = -1f; return; }
            if (keyDownAt >= 0f && now - keyDownAt > holdToRun)
                keyAxis = (right ? 1f : 0f) - (left ? 1f : 0f);
        }

        void Update()
        {
            AnyPressThisFrame = false;
            keyAxis = 0f;
#if ENABLE_INPUT_SYSTEM
            foreach (var t in ETouch.activeTouches)
            {
                int id = t.touchId;
                switch (t.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began: TouchBegan(id, t.screenPosition); break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary: TouchMoved(id, t.screenPosition); break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled: TouchEnded(id, t.screenPosition); break;
                }
            }
            var mouse = Mouse.current;
            if (mouse != null && ETouch.activeTouches.Count == 0)
            {
                var mp = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) TouchBegan(-1, mp);
                else if (mouse.leftButton.isPressed) TouchMoved(-1, mp);
                if (mouse.leftButton.wasReleasedThisFrame) TouchEnded(-1, mp);
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                bool left = kb.aKey.isPressed || kb.leftArrowKey.isPressed, right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                bool leftDown = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
                bool rightDown = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
                keyCrouch = kb.cKey.isPressed;
                keyCrouchAxis = keyCrouch ? (right ? 1f : 0f) - (left ? 1f : 0f) : 0f;
                if (Classic)
                {
                    if (keyCrouch) { left = right = leftDown = rightDown = false; }
                    ClassicKeys(left, right, leftDown, rightDown);
                    if (kb.zKey.wasPressedThisFrame) Push(Command.KickLeft);
                    if (kb.xKey.wasPressedThisFrame) Push(Command.KickRight);
                    bool lowHeld = kb.zKey.isPressed || kb.xKey.isPressed;
                    if (kb.zKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame) lowKeyAt = UnityEngine.Time.unscaledTime;
                    else if (!lowHeld) lowKeyAt = -1f;
                    if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                        Push(kb.aKey.isPressed || kb.leftArrowKey.isPressed ? Command.JumpLeft : kb.dKey.isPressed || kb.rightArrowKey.isPressed ? Command.JumpRight : Command.JumpKick);
                    if (kb.qKey.wasPressedThisFrame) Push(Command.DodgeLeft);
                    if (kb.eKey.wasPressedThisFrame) Push(Command.DodgeRight);
                    if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Push(DownSwipe());
                    if (kb.fKey.wasPressedThisFrame) Push(Command.ThrowWeapon);
                    if (kb.spaceKey.wasPressedThisFrame) Push(Command.Rampage);
                }
                else
                {
                    if (left) keyAxis -= 1f;
                    if (right) keyAxis += 1f;
                    if (kb.jKey.wasPressedThisFrame) Push(Command.StrikeForward);
                    if (kb.kKey.wasPressedThisFrame) Push(Command.KickForward);
                    if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Push(Command.JumpKick);
                    if (kb.lKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame) Push(Command.DodgeForward);
                    if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) Push(Command.ThrowWeapon);
                    if (kb.eKey.wasPressedThisFrame) Push(Command.Rampage);
                }
                if (kb.rKey.wasPressedThisFrame) Push(Command.Restart);
                if (kb.tKey.wasPressedThisFrame) Push(Command.LockNext);
                if (kb.tabKey.wasPressedThisFrame) Push(Command.CycleStyle);
                if (kb.digit1Key.wasPressedThisFrame) Push(Command.StyleFists);
                if (kb.digit2Key.wasPressedThisFrame) Push(Command.StyleSword);
                if (kb.digit3Key.wasPressedThisFrame) Push(Command.StyleGun);
                if (kb.anyKey.wasPressedThisFrame) AnyPressThisFrame = true;
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                switch (t.phase)
                {
                    case UnityEngine.TouchPhase.Began: TouchBegan(t.fingerId, t.position); break;
                    case UnityEngine.TouchPhase.Moved:
                    case UnityEngine.TouchPhase.Stationary: TouchMoved(t.fingerId, t.position); break;
                    case UnityEngine.TouchPhase.Ended:
                    case UnityEngine.TouchPhase.Canceled: TouchEnded(t.fingerId, t.position); break;
                }
            }
            if (Input.touchCount == 0)
            {
                Vector2 mp = Input.mousePosition;
                if (Input.GetMouseButtonDown(0)) TouchBegan(-1, mp);
                else if (Input.GetMouseButton(0)) TouchMoved(-1, mp);
                if (Input.GetMouseButtonUp(0)) TouchEnded(-1, mp);
            }
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow), right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool leftDown = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            bool rightDown = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            if (Classic)
            {
                ClassicKeys(left, right, leftDown, rightDown);
                if (Input.GetKeyDown(KeyCode.Z)) Push(Command.KickLeft);
                if (Input.GetKeyDown(KeyCode.X)) Push(Command.KickRight);
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Push(Command.JumpKick);
                if (Input.GetKeyDown(KeyCode.Q)) Push(Command.DodgeLeft);
                if (Input.GetKeyDown(KeyCode.E)) Push(Command.DodgeRight);
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.F)) Push(Command.ThrowWeapon);
                if (Input.GetKeyDown(KeyCode.Space)) Push(Command.Rampage);
            }
            else
            {
                if (left) keyAxis -= 1f;
                if (right) keyAxis += 1f;
                if (Input.GetKeyDown(KeyCode.J)) Push(Command.StrikeForward);
                if (Input.GetKeyDown(KeyCode.K)) Push(Command.KickForward);
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Push(Command.JumpKick);
                if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.LeftShift)) Push(Command.DodgeForward);
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.F)) Push(Command.ThrowWeapon);
                if (Input.GetKeyDown(KeyCode.E)) Push(Command.Rampage);
            }
            if (Input.GetKeyDown(KeyCode.R)) Push(Command.Restart);
            if (Input.GetKeyDown(KeyCode.T)) Push(Command.LockNext);
            if (Input.GetKeyDown(KeyCode.Tab)) Push(Command.CycleStyle);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Push(Command.StyleFists);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Push(Command.StyleSword);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Push(Command.StyleGun);
            if (Input.anyKeyDown) AnyPressThisFrame = true;
#endif
            // the stick: horizontal travel → run speed (with a small dead zone)
            float axis = keyAxis;
            foreach (var kv in holding) axis = kv.Value;   // classic: a held press runs
            if (stickId != int.MinValue)
            {
                float dx = (stickNow.x - stickOrigin.x) / (Screen.height * stickRange);
                axis = Mathf.Abs(dx) < 0.15f ? 0f : Mathf.Clamp(dx, -1f, 1f);
            }
            MoveAxis = axis;
            // the creep while crouched
            float ca = keyCrouchAxis;
            if (crouchId != int.MinValue)
            {
                float cx = (crouchNow.x - crouchAnchor.x) / (Screen.height * 0.08f);
                ca = Mathf.Abs(cx) < 0.2f ? 0f : Mathf.Clamp(cx, -1f, 1f);
            }
            CrouchAxis = ca;
            // which height a held press is at
            int hh = 0;
            foreach (var kv in holding)
                if (touchStart.TryGetValue(kv.Key, out var hs)) hh = hs.y >= Screen.height * kickLine ? 1 : 2;
            if (keyDownAt >= 0f && UnityEngine.Time.unscaledTime - keyDownAt > holdToRun) hh = 1;
            if (lowKeyAt >= 0f && UnityEngine.Time.unscaledTime - lowKeyAt > holdToRun) hh = 2;
            HoldHeight = hh;
        }
    }
}
