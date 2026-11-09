using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using SKey = UnityEngine.InputSystem.Key;
#endif

namespace ProjectInk
{
    /// <summary>The story's own controls (docs/story/story_v2.md §5): the few gestures that let the
    /// player take part in a scene instead of watching it. One set, the same all game, each shown with
    /// a prompt the first time and the same look every time after:
    ///
    ///   TAP an object   look at a clue (a glowing ring on it; or E to take the next one)
    ///   HOLD            do something hard (open his eyes, the palm on the glass, the walkie's button)
    ///   MASH            break free, run (tap fast; or E / Space fast)
    ///   SWIPE           act fast in a cinematic (a swipe the right way; or the arrow / A D keys)
    ///   CHOOSE          answer (two or three lines; tap one, or 1 2 3)
    ///   DRAG            move a physical thing (turn the photo, pull the ribbon; or hold E)
    ///
    /// Nothing here can fail a scene: a wait plays the safe outcome when the scene asks for one.
    /// Input is read from the Input System directly (pointer, touch and keyboard).</summary>
    public partial class StoryIntro
    {
        enum ScAsk { None, Hold, Mash, Swipe, Choose, Tap, Drag }

        ScAsk scAsk;
        string scTitle, scHow;
        float scAt, scProgress, scFlash = -1f;
        Vector3 scWorld;
        bool scHasWorld;
        string[] scOptions;
        int scHover = -1;
        Vector2 scDragDir = Vector2.right;
        readonly List<Vector3> scTargets = new List<Vector3>();
        readonly List<string> scLabels = new List<string>();
        readonly List<bool> scDone = new List<bool>();
        int scSwipe;                                  // which way the swipe must go: -1 left, 1 right, 2 up, -2 down, 0 any
        /// <summary>The line the player chose in the last CHOOSE (0-based).</summary>
        int chosen;
        /// <summary>Whether the last SWIPE was made in time (false: the scene's safe outcome).</summary>
        bool scOk;
        bool pointerWasDown;
        Vector2 pressAt, dragFrom;
        float pressTime;

        // ================================================================ input (the Input System)

#if ENABLE_INPUT_SYSTEM
        static bool PointerDown()
        {
            var p = Pointer.current;
            return p != null && p.press.isPressed;
        }

        static Vector2 PointerPos()
        {
            var p = Pointer.current;
            return p != null ? p.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        static bool KeyHeld(params SKey[] keys)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in keys) if (kb[k].isPressed) return true;
            return false;
        }

        static bool KeyTapped(params SKey[] keys)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in keys) if (kb[k].wasPressedThisFrame) return true;
            return false;
        }
#else
        // no Input System in this build: the story's prompts wait (never the legacy Input API)
        enum SKey { E, Space, Enter, D, A, LeftArrow, RightArrow, W, UpArrow, S, DownArrow, Digit1, Digit2, Digit3, Numpad1, Numpad2, Numpad3 }
        static bool PointerDown() => false;
        static Vector2 PointerPos() => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        static bool KeyHeld(params SKey[] keys) => false;
        static bool KeyTapped(params SKey[] keys) => false;
#endif

        /// <summary>Screen pixels (origin bottom left) to the story GUI's space (scaled, origin top left).</summary>
        Vector2 ToGui(Vector2 px) => new Vector2(px.x / Mathf.Max(0.01f, scale), (Screen.height - px.y) / Mathf.Max(0.01f, scale));

        bool WorldToGui(Vector3 world, out Vector2 g)
        {
            g = Vector2.zero;
            var c = Camera.main;
            if (c == null) return false;
            var s = c.WorldToScreenPoint(world);
            if (s.z < 0.05f) return false;
            g = ToGui(new Vector2(s.x, s.y));
            return true;
        }

        void ScBegin(ScAsk kind, string title, string how)
        {
            scAsk = kind;
            scTitle = title;
            scHow = how;
            scAt = Time.unscaledTime;
            scProgress = 0f;
            scHover = -1;
            pointerWasDown = PointerDown();
            if (GestureInput.Instance != null) GestureInput.Instance.Flush();
        }

        void ScEnd()
        {
            scAsk = ScAsk.None;
            scTitle = scHow = null;
            scHasWorld = false;
            scFlash = Time.unscaledTime;
            if (GestureInput.Instance != null) GestureInput.Instance.Flush();
        }

        static string M(string mobile, string keys) => Application.isMobilePlatform ? mobile : keys;

        // ================================================================ the gestures

        /// <summary>HOLD: keep pressing until the ring fills (`secs` of holding; it drains when let go).
        /// `frame` runs every frame with the progress 0..1 (the scene reacts as it fills).</summary>
        IEnumerator HoldTo(string title, float secs, System.Action<float> frame = null, Vector3? at = null)
        {
            ScBegin(ScAsk.Hold, title, M("PRESS AND HOLD", "HOLD  E"));
            scHasWorld = at.HasValue;
            if (at.HasValue) scWorld = at.Value;
            yield return null;
            while (scProgress < 1f)
            {
                bool held = PointerDown() || KeyHeld(SKey.E, SKey.Space, SKey.Enter);
                scProgress = Mathf.Clamp01(scProgress + (held ? 1f / secs : -0.6f / secs) * Time.unscaledDeltaTime);
                if (held && Random.value < 0.08f) Haptics.Vibrate();
                frame?.Invoke(scProgress);
                yield return null;
            }
            Sound.Play(Sfx.Cloth, 0.5f, null, 1.2f);
            ScEnd();
        }

        /// <summary>MASH: `taps` fast presses. It drains a little between them, so it must be quick.</summary>
        IEnumerator MashTo(string title, int taps, System.Action<float> frame = null)
        {
            ScBegin(ScAsk.Mash, title, M("TAP FAST", "TAP  E  FAST"));
            yield return null;
            while (scProgress < 1f)
            {
                bool down = PointerDown();
                bool tap = (down && !pointerWasDown) || KeyTapped(SKey.E, SKey.Space, SKey.Enter, SKey.D, SKey.A);
                pointerWasDown = down;
                if (tap)
                {
                    scProgress = Mathf.Clamp01(scProgress + 1f / taps);
                    Haptics.Vibrate();
                    Sound.Play(Sfx.Struggle, 0.35f, null, Random.Range(0.9f, 1.15f));
                }
                scProgress = Mathf.Max(0f, scProgress - 0.12f * Time.unscaledDeltaTime);
                frame?.Invoke(scProgress);
                yield return null;
            }
            ScEnd();
        }

        /// <summary>SWIPE: one swipe the right way within `window` real seconds (dir: -1 left, 1 right,
        /// 2 up, -2 down, 0 any). Sets `scOk`. With `window` &lt;= 0 it waits for ever.</summary>
        IEnumerator SwipeTo(string title, int dir, float window, System.Action<float> frame = null)
        {
            string arrow = dir == -1 ? "LEFT" : dir == 1 ? "RIGHT" : dir == 2 ? "UP" : dir == -2 ? "DOWN" : "";
            ScBegin(ScAsk.Swipe, title, M("SWIPE " + arrow, dir == -1 ? "A  /  LEFT" : dir == 1 ? "D  /  RIGHT" : dir == 2 ? "W  /  UP" : dir == -2 ? "S  /  DOWN" : "ANY ARROW"));
            scSwipe = dir;
            scOk = false;
            yield return null;
            float t0 = 0f;
            while (window <= 0f || t0 < window)
            {
                t0 += Time.unscaledDeltaTime;
                scProgress = window > 0f ? t0 / window : 0f;
                bool down = PointerDown();
                var pos = PointerPos();
                if (down && !pointerWasDown) { pressAt = pos; pressTime = Time.unscaledTime; }
                if (!down && pointerWasDown)
                {
                    var d = pos - pressAt;
                    if (d.magnitude > Screen.height * 0.07f && Time.unscaledTime - pressTime < 0.6f)
                    {
                        int got = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x < 0 ? -1 : 1) : (d.y > 0 ? 2 : -2);
                        if (dir == 0 || got == dir) scOk = true;
                    }
                }
                pointerWasDown = down;
                if ((dir == -1 || dir == 0) && KeyTapped(SKey.A, SKey.LeftArrow)) scOk = true;
                if ((dir == 1 || dir == 0) && KeyTapped(SKey.D, SKey.RightArrow)) scOk = true;
                if ((dir == 2 || dir == 0) && KeyTapped(SKey.W, SKey.UpArrow)) scOk = true;
                if ((dir == -2 || dir == 0) && KeyTapped(SKey.S, SKey.DownArrow)) scOk = true;
                frame?.Invoke(scProgress);
                if (scOk) break;
                yield return null;
            }
            if (scOk) { Haptics.Vibrate(); Sound.Play(Sfx.Whoosh, 0.6f); }
            ScEnd();
        }

        /// <summary>CHOOSE: two or three lines on screen; tap one, or press its number. Sets `chosen`.</summary>
        IEnumerator ChooseFrom(params string[] options)
        {
            ScBegin(ScAsk.Choose, null, null);
            scOptions = options;
            chosen = -1;
            yield return null;
            while (chosen < 0)
            {
                if (KeyTapped(SKey.Digit1, SKey.Numpad1)) chosen = 0;
                else if (options.Length > 1 && KeyTapped(SKey.Digit2, SKey.Numpad2)) chosen = 1;
                else if (options.Length > 2 && KeyTapped(SKey.Digit3, SKey.Numpad3)) chosen = 2;
                yield return null;
            }
            Sound.Play(Sfx.Cloth, 0.45f, null, 1.25f);
            scOptions = null;
            ScEnd();
        }

        /// <summary>TAP: rings on things in the world; tap each one (any order), or E for the next.
        /// `onTap(i)` runs as each is taken (a coroutine can be started from it); the scene waits
        /// for `busy` to clear before the next tap counts.</summary>
        IEnumerator TapThings(Vector3[] where, string[] labels, System.Func<int, IEnumerator> onTap)
        {
            scTargets.Clear(); scLabels.Clear(); scDone.Clear();
            for (int i = 0; i < where.Length; i++) { scTargets.Add(where[i]); scLabels.Add(labels != null && i < labels.Length ? labels[i] : null); scDone.Add(false); }
            int left = where.Length;
            while (left > 0)
            {
                ScBegin(ScAsk.Tap, null, M("TAP WHAT YOU WANT TO LOOK AT", "CLICK IT  ·  OR  E"));
                yield return null;
                int pick = -1;
                while (pick < 0)
                {
                    bool down = PointerDown();
                    if (down && !pointerWasDown)
                    {
                        var g = ToGui(PointerPos());
                        float best = 70f;
                        for (int i = 0; i < scTargets.Count; i++)
                        {
                            if (scDone[i] || !WorldToGui(scTargets[i], out var at)) continue;
                            float d = Vector2.Distance(g, at);
                            if (d < best) { best = d; pick = i; }
                        }
                    }
                    pointerWasDown = down;
                    if (pick < 0 && KeyTapped(SKey.E, SKey.Space, SKey.Enter))
                        for (int i = 0; i < scDone.Count; i++) if (!scDone[i]) { pick = i; break; }
                    yield return null;
                }
                scDone[pick] = true;
                left--;
                Sound.Play(Sfx.Glint, 0.5f);
                ScEnd();
                if (onTap != null) yield return onTap(pick);
            }
            scTargets.Clear();
        }

        /// <summary>DRAG: pull along a screen direction (or hold E) until it's done; `frame(p)` moves
        /// the thing with the drag.</summary>
        IEnumerator DragTo(string title, Vector2 dir, System.Action<float> frame = null, Vector3? at = null)
        {
            string how = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? (dir.x < 0 ? "DRAG LEFT" : "DRAG RIGHT") : (dir.y > 0 ? "DRAG UP" : "DRAG DOWN");
            ScBegin(ScAsk.Drag, title, M(how, how + "  ·  OR HOLD  E"));
            scDragDir = dir.normalized;
            scHasWorld = at.HasValue;
            if (at.HasValue) scWorld = at.Value;
            yield return null;
            float pulled = 0f;
            while (scProgress < 1f)
            {
                bool down = PointerDown();
                var pos = PointerPos();
                if (down && !pointerWasDown) dragFrom = pos;
                if (down)
                {
                    float along = Vector2.Dot(pos - dragFrom, scDragDir) / (Screen.height * 0.28f);
                    pulled = Mathf.Max(pulled, Mathf.Clamp01(along));
                }
                else pulled = Mathf.MoveTowards(pulled, scProgress >= 0.98f ? 1f : Mathf.Floor(pulled * 4f) / 4f, Time.unscaledDeltaTime);
                pointerWasDown = down;
                if (KeyHeld(SKey.E, SKey.Space)) pulled = Mathf.Min(1f, pulled + Time.unscaledDeltaTime / 1.2f);
                scProgress = pulled;
                frame?.Invoke(scProgress);
                yield return null;
            }
            Sound.Play(Sfx.Cloth, 0.55f);
            ScEnd();
        }

        // ================================================================ the walkie

        /// <summary>Maya, on her toy walkie-talkie: the radio's crackle, then her voice.</summary>
        void Walkie(string line, float secs)
        {
            Sound.Play(Sfx.Static, 0.35f, null, 1.6f);
            Say("WALKIE", line, secs);
        }

        IEnumerator WalkieLine(string line, float secs, System.Action frame = null)
        {
            Walkie(line, secs);
            yield return HoldFor(secs + 0.2f, frame);
        }

        IEnumerator HoldFor(float secs, System.Action frame = null)
        {
            for (float t0 = 0f; t0 < secs; t0 += Time.unscaledDeltaTime) { frame?.Invoke(); yield return null; }
        }

        /// <summary>A small toy walkie-talkie (an object for his hand, her pocket, a seat, a stone).</summary>
        static Transform MakeWalkie(string name)
        {
            var root = new GameObject(name).transform;
            var body = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.82f, 0.82f, 0.8f) };
            var dark = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.08f, 0.08f, 0.09f) };
            void Part(PrimitiveType p, Vector3 at, Vector3 s, Material m)
            {
                var g = GameObject.CreatePrimitive(p);
                Object.Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(root, false);
                g.transform.localPosition = at;
                g.transform.localScale = s;
                g.GetComponent<Renderer>().sharedMaterial = m;
            }
            Part(PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.06f, 0.13f, 0.035f), body);
            Part(PrimitiveType.Cylinder, new Vector3(0.018f, 0.17f, 0f), new Vector3(0.008f, 0.05f, 0.008f), dark);
            Part(PrimitiveType.Cube, new Vector3(0f, 0.09f, -0.019f), new Vector3(0.042f, 0.04f, 0.004f), dark);
            Part(PrimitiveType.Cube, new Vector3(0f, 0.035f, -0.019f), new Vector3(0.02f, 0.02f, 0.004f), dark);
            return root;
        }

        // ================================================================ how they look

        /// <summary>One silhouette each (docs/story/story_v2.md §4), built onto the stick rig.</summary>
        enum Look { Foreman, Clerk, Driver, Cleaner, Handler, Warden, Voss, Harrow, Maya }

        static Material Ink(Color c) => new Material(Shader.Find("Unlit/Color")) { color = c };

        static Transform Bit(Transform parent, PrimitiveType p, Vector3 at, Vector3 s, Material m, Vector3? euler = null)
        {
            if (parent == null) return null;
            var g = GameObject.CreatePrimitive(p);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = "Look";
            g.transform.SetParent(parent, false);
            g.transform.localPosition = at;
            g.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            g.transform.localScale = s;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        /// <summary>Dress a body (hero, enemy or puppet) in a character's look. Bones: hips, spine,
        /// chest, head, upperarm/forearm/hand .L/.R. Positions are in each bone's own space (its
        /// length runs along +y).</summary>
        static void Dress(ClipPlayer body, Look look)
        {
            if (body == null) return;
            var head = body.Bone("head");
            var chest = body.Bone("chest");
            var spine = body.Bone("spine");
            var hips = body.Bone("hips");
            var handR = body.Bone("hand.R");
            var handL = body.Bone("hand.L");
            var black = Ink(new Color(0.03f, 0.03f, 0.035f));
            var white = Ink(new Color(0.9f, 0.9f, 0.88f));
            var grey = Ink(new Color(0.5f, 0.5f, 0.5f));
            var silver = Ink(new Color(0.78f, 0.79f, 0.8f));
            switch (look)
            {
                case Look.Foreman:      // a hard hat, a hi-vis vest with its two pale bands
                    Bit(head, PrimitiveType.Sphere, new Vector3(0f, 0.19f, 0f), new Vector3(0.3f, 0.2f, 0.3f), white);
                    Bit(head, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.008f, 0.36f), white);
                    Bit(chest, PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.1f), new Vector3(0.26f, 0.03f, 0.02f), white);
                    Bit(spine, PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.11f), new Vector3(0.26f, 0.03f, 0.02f), white);
                    break;
                case Look.Clerk:        // glasses, a waistcoat
                    Bit(head, PrimitiveType.Cube, new Vector3(0f, 0.15f, -0.13f), new Vector3(0.18f, 0.012f, 0.01f), black);
                    Bit(chest, PrimitiveType.Cube, new Vector3(0f, 0f, -0.09f), new Vector3(0.2f, 0.16f, 0.03f), grey);
                    break;
                case Look.Driver:       // a flat cap, a leather jacket's collar
                    Bit(head, PrimitiveType.Sphere, new Vector3(0f, 0.2f, -0.02f), new Vector3(0.29f, 0.1f, 0.3f), black);
                    Bit(head, PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.17f), new Vector3(0.2f, 0.015f, 0.09f), black, new Vector3(-12f, 0f, 0f));
                    Bit(chest, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0.3f, 0.06f, 0.24f), black);
                    break;
                case Look.Cleaner:      // Silas: slicked hair, a long apron, gloves, a scalpel
                    Bit(head, PrimitiveType.Sphere, new Vector3(0f, 0.19f, 0.03f), new Vector3(0.28f, 0.12f, 0.29f), black);
                    Bit(chest, PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.11f), new Vector3(0.24f, 0.22f, 0.012f), white);
                    Bit(hips, PrimitiveType.Cube, new Vector3(0f, -0.18f, -0.11f), new Vector3(0.26f, 0.42f, 0.012f), white);
                    Bit(handR, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.07f, black);
                    Bit(handL, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.07f, black);
                    break;
                case Look.Handler:      // a broken-nosed boxer: a tracksuit's stripes, brass knuckles
                    foreach (var arm in new[] { "upperarm.L", "upperarm.R", "forearm.L", "forearm.R" })
                        Bit(body.Bone(arm), PrimitiveType.Cube, new Vector3(0f, 0.08f, -0.035f), new Vector3(0.012f, 0.16f, 0.012f), white);
                    Bit(handR, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.08f, 0.025f, 0.03f), silver);
                    Bit(handL, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.08f, 0.025f, 0.03f), silver);
                    break;
                case Look.Warden:       // bald, a chain round his shoulder
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i / 9f * Mathf.PI * 2f;
                        Bit(chest, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.16f, 0.06f + Mathf.Sin(a) * 0.12f, -0.08f), new Vector3(0.05f, 0.03f, 0.03f), grey, new Vector3(0f, 0f, a * Mathf.Rad2Deg));
                    }
                    break;
                case Look.Voss:         // silver hair, a long dark coat, gloves, the cane
                    Bit(head, PrimitiveType.Sphere, new Vector3(0f, 0.19f, 0.02f), new Vector3(0.3f, 0.16f, 0.3f), silver);
                    Bit(chest, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.34f, 0.08f, 0.26f), black);
                    Bit(hips, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0.02f), new Vector3(0.32f, 0.62f, 0.24f), black);
                    Bit(handR, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.075f, black);
                    Bit(handL, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.075f, black);
                    var cane = Bit(handL, PrimitiveType.Cylinder, new Vector3(0f, -0.42f, 0f), new Vector3(0.022f, 0.42f, 0.022f), black);
                    if (cane != null) { cane.name = "Cane"; Bit(handL, PrimitiveType.Sphere, new Vector3(0f, 0.01f, 0f), Vector3.one * 0.05f, silver); }
                    break;
                case Look.Harrow:       // a raincoat, a hat brim
                    Bit(head, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.008f, 0.34f), grey);
                    Bit(head, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.24f, 0.06f, 0.24f), grey);
                    Bit(hips, PrimitiveType.Cube, new Vector3(0f, -0.25f, 0.02f), new Vector3(0.3f, 0.5f, 0.22f), grey);
                    break;
                case Look.Maya:         // two pigtails (the ribbon is the story's: AttachBow)
                    Bit(head, PrimitiveType.Sphere, new Vector3(0.15f, 0.12f, 0.02f), new Vector3(0.09f, 0.14f, 0.09f), white);
                    Bit(head, PrimitiveType.Sphere, new Vector3(-0.15f, 0.12f, 0.02f), new Vector3(0.09f, 0.14f, 0.09f), white);
                    break;
            }
        }

        // ================================================================ on screen

        Texture2D scDisc;

        void ControlsGUI(float w, float h)
        {
            if (scDisc == null) scDisc = Radial(64, true);
            float k = Mathf.Clamp01((Time.unscaledTime - scAt) / 0.3f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            var red = new Color(0.8f, 0.06f, 0.07f);
            if (scAsk == ScAsk.None)
            {
                // the little white burst as one is done
                if (scFlash > 0f && Time.unscaledTime - scFlash < 0.35f)
                {
                    float f = (Time.unscaledTime - scFlash) / 0.35f;
                    DrawRing(new Vector2(w * 0.5f, h * 0.62f), 40f + 60f * f, new Color(1f, 1f, 1f, 0.6f * (1f - f)));
                }
                return;
            }
            Vector2 c = new Vector2(w * 0.5f, h * 0.62f);
            if (scHasWorld && WorldToGui(scWorld, out var wg)) c = wg;
            if (scTitle != null)
            {
                int bs = big.fontSize;
                big.fontSize = 40;
                big.normal.textColor = new Color(1f, 1f, 1f, k);
                GUI.Label(new Rect(0, h * 0.14f, w, 54), Spaced(scTitle), big);
                big.fontSize = bs;
                Fill(new Rect(w * 0.5f - 60 * k, h * 0.14f + 54, 120 * k, 3), new Color(red.r, red.g, red.b, k));
            }
            if (scHow != null)
            {
                prompt.normal.textColor = new Color(1f, 1f, 1f, (0.55f + 0.45f * pulse) * k);
                GUI.Label(new Rect(0, h * 0.14f + 64, w, 34), Spaced(scHow), prompt);
            }
            switch (scAsk)
            {
                case ScAsk.Hold:
                case ScAsk.Drag:
                    {
                        float r = 36f;
                        DrawRing(c, r + 4f * pulse, new Color(1f, 1f, 1f, 0.55f * k));
                        var o = GUI.color;
                        GUI.color = new Color(red.r, red.g, red.b, 0.85f * k);
                        float fr = r * scProgress;
                        GUI.DrawTexture(new Rect(c.x - fr, c.y - fr, fr * 2f, fr * 2f), scDisc);
                        GUI.color = o;
                        if (scAsk == ScAsk.Drag)
                        {
                            var d = new Vector2(scDragDir.x, -scDragDir.y);
                            for (int i = 0; i < 3; i++)
                            {
                                float u = Mathf.Repeat(Time.unscaledTime * 1.2f - i * 0.25f, 1f);
                                var p = c + d * (50f + 60f * u);
                                Fill(new Rect(p.x - 4, p.y - 4, 8, 8), new Color(1f, 1f, 1f, 0.7f * (1f - u) * k));
                            }
                        }
                        break;
                    }
                case ScAsk.Mash:
                    {
                        float bw = 320f, x = (w - bw) * 0.5f, y = h * 0.62f;
                        Fill(new Rect(x - 2, y - 2, bw + 4, 18), new Color(0f, 0f, 0f, 0.6f * k));
                        Fill(new Rect(x, y, bw * scProgress, 14), new Color(red.r, red.g, red.b, k));
                        float s = 30f + 12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f));
                        DrawRing(new Vector2(w * 0.5f, y - 50f), s, new Color(1f, 1f, 1f, 0.7f * k));
                        break;
                    }
                case ScAsk.Swipe:
                    {
                        Vector2 d = scSwipe == -1 ? Vector2.left : scSwipe == 1 ? Vector2.right : scSwipe == 2 ? Vector2.down : scSwipe == -2 ? Vector2.up : Vector2.right;
                        for (int i = 0; i < 4; i++)
                        {
                            float u = Mathf.Repeat(Time.unscaledTime * 2f - i * 0.2f, 1f);
                            var p = new Vector2(w * 0.5f, h * 0.6f) + d * (-80f + 160f * u);
                            Fill(new Rect(p.x - 6, p.y - 6, 12, 12), new Color(1f, 1f, 1f, 0.8f * (1f - u) * k));
                        }
                        if (scProgress > 0f) Fill(new Rect(w * 0.5f - 120, h * 0.6f + 40, 240 * (1f - scProgress), 4), new Color(red.r, red.g, red.b, k));
                        break;
                    }
                case ScAsk.Choose:
                    {
                        if (scOptions == null) break;
                        float bw = Mathf.Min(560f, w - 80f), bh = 52f, gap = 12f;
                        float total = scOptions.Length * bh + (scOptions.Length - 1) * gap;
                        float y0 = h * 0.5f - total * 0.5f;
                        var mp = ToGui(PointerPos());
                        scHover = -1;
                        for (int i = 0; i < scOptions.Length; i++)
                        {
                            var r = new Rect((w - bw) * 0.5f, y0 + i * (bh + gap), bw, bh);
                            bool hov = r.Contains(mp);
                            if (hov) scHover = i;
                            Fill(r, new Color(0f, 0f, 0f, (hov ? 0.82f : 0.66f) * k));
                            Fill(new Rect(r.x, r.y, 4, r.height), new Color(red.r, red.g, red.b, k));
                            cap.alignment = TextAnchor.MiddleLeft;
                            cap.normal.textColor = new Color(1f, 1f, 1f, (hov ? 1f : 0.86f) * k);
                            GUI.Label(new Rect(r.x + 22, r.y, r.width - 30, r.height), (Application.isMobilePlatform ? "" : (i + 1) + "    ") + scOptions[i], cap);
                            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && chosen < 0) chosen = i;
                            if (Event.current.type == EventType.Repaint) GestureInput.Blocked.Add(new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale));
                        }
                        break;
                    }
                case ScAsk.Tap:
                    {
                        for (int i = 0; i < scTargets.Count; i++)
                        {
                            if (scDone[i] || !WorldToGui(scTargets[i], out var at)) continue;
                            float r = 22f + 6f * Mathf.Sin(Time.unscaledTime * 4f + i);
                            DrawRing(at, r, new Color(1f, 1f, 1f, 0.8f * k));
                            var o = GUI.color;
                            GUI.color = new Color(1f, 1f, 1f, 0.5f * k);
                            GUI.DrawTexture(new Rect(at.x - 5, at.y - 5, 10, 10), scDisc);
                            GUI.color = o;
                            if (scLabels[i] != null)
                            {
                                cap.alignment = TextAnchor.MiddleCenter;
                                cap.normal.textColor = new Color(1f, 1f, 1f, 0.8f * k);
                                GUI.Label(new Rect(at.x - 120, at.y + r + 4, 240, 24), Spaced(scLabels[i]), cap);
                                cap.alignment = TextAnchor.MiddleLeft;
                            }
                        }
                        break;
                    }
            }
        }

        void DrawRing(Vector2 c, float r, Color col)
        {
            if (ring == null) return;
            var o = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), ring);
            GUI.color = o;
        }
    }
}
