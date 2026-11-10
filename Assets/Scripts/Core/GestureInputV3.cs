using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectInk
{
    /// <summary>What a touch meant, in Combat 3.0's grammar.</summary>
    public enum V3Kind
    {
        None,
        Tap,          // on a man: a fast blow aimed at the height touched (0 head, 1 body, 2 legs)
        Swipe,        // through a man: a swinging blow along the finger's path
        Grab,         // a hold on a man: take hold of him (the clinch)
        PullStart,    // a drag back from a man towards the hero: the wind-up begins
        PullRelease,  // let go: the heavy blow (power 0..1)
        PullCancel,
        Duck, Hop, StepOut, StepIn,   // a swipe from the hero: evade that way
        GroundTap,    // a single step towards a point
        GroundSwipe,  // a hop that way
    }

    public struct V3Event
    {
        public V3Kind kind;
        public Enemy enemy;           // null: the hero picks the man in front of him
        public int height;
        public Vector2 dir;           // a swipe's direction on the screen (normalised)
        public float power;
        public float worldX;
        public float time;            // real seconds
        public Vector2 at;            // where the finger was (pixels), when it was a finger
    }

    /// <summary>Combat 3.0's touch grammar (docs/design/combat_v3.md §1): WHERE the finger starts says
    /// what (on a man = attack him, on the hero = defend, on the ground = move), HOW it moves says how
    /// (tap, hold, swipe, pull and release). Taps fire on release, holds at 0.2 s, swipes as soon as the
    /// finger has travelled far enough, so nothing waits.
    /// Keys, for testing at a desk: A/D walk · J/K/L tap the man at head/body/legs · I swipe across ·
    /// U swipe up · N swipe down · hold P to wind up, let go to throw · hold H to guard · S duck ·
    /// W hop · Q step out · E step in · G grab.</summary>
    public partial class GestureInput
    {
        enum Zone { Enemy, Hero, Ground }

        class V3Touch
        {
            public Zone zone;
            public Enemy enemy;
            public Vector2 start, now;
            public float t0;
            public int height;
            public bool done, guard, pulling, held, walking;
            public float crossedAt = -1f;
        }

        readonly Dictionary<int, V3Touch> v3Touches = new Dictionary<int, V3Touch>();
        readonly Queue<V3Event> v3Queue = new Queue<V3Event>();
        float v3Axis, keyPullAt = -1f;

        /// <summary>A finger (or H) is holding the guard up.</summary>
        public bool V3Guard { get; private set; }
        /// <summary>The live wind-up of a pull: 0 (none) .. 1 (full), and on whom.</summary>
        public float V3Pull { get; private set; }
        public Enemy V3PullEnemy { get; private set; }
        public bool V3Pulling { get; private set; }

        const float TapMax = 0.3f, HoldAt = 0.2f, GuardAt = 0.11f, PullCommit = 0.1f;
        float SwipeMin => Screen.height * 0.065f;

        bool V3Owns(int id) => v3Touches.ContainsKey(id);

        public bool NextV3(out V3Event e)
        {
            float now = Time.unscaledTime;
            while (v3Queue.Count > 0)
            {
                e = v3Queue.Dequeue();
                if (now - e.time <= 0.3f) return true;
            }
            e = default;
            return false;
        }

        void PushV3(V3Event e)
        {
            if (TimeController.Paused) return;
            e.time = Time.unscaledTime;
            if (v3Queue.Count >= 3) v3Queue.Dequeue();
            v3Queue.Enqueue(e);
            // the same touch read the classic way, for the story's directed beats (the prologue's duel
            // waits for "tap high", "swipe"...); in a fight the hero reads only the new events
            Push(AsClassic(e));
        }

        Command AsClassic(V3Event e)
        {
            switch (e.kind)
            {
                case V3Kind.Tap:
                case V3Kind.Grab:
                    {
                        int side;
                        if (e.enemy != null && hero != null)
                        {
                            var hc = hero.GetComponent<HeroController>();
                            if (hc != null) hc.LockOn(e.enemy);
                            side = e.enemy.transform.position.x >= hero.position.x ? 1 : -1;
                        }
                        else side = e.at.x < HeroScreenX ? -1 : 1;
                        bool kick = e.height == 2;
                        return kick ? (side < 0 ? Command.KickLeft : Command.KickRight) : (side < 0 ? Command.StrikeLeft : Command.StrikeRight);
                    }
                case V3Kind.GroundTap:
                    {
                        int side = e.at.x < HeroScreenX ? -1 : 1;
                        bool kick = e.at.y < Screen.height * kickLine;
                        return kick ? (side < 0 ? Command.KickLeft : Command.KickRight) : (side < 0 ? Command.StrikeLeft : Command.StrikeRight);
                    }
                case V3Kind.Swipe: case V3Kind.GroundSwipe: case V3Kind.StepIn: case V3Kind.StepOut:
                    if (e.dir.y > 0.65f) return Command.JumpKick;
                    if (e.dir.y < -0.65f) return Command.ThrowWeapon;
                    return e.dir.x < 0f ? Command.DodgeLeft : Command.DodgeRight;
                case V3Kind.Hop: return Command.JumpKick;
                case V3Kind.Duck: return Command.ThrowWeapon;
            }
            return Command.None;
        }

        // ---------------------------------------------------------------- where a touch starts
        bool HeroBox(out Vector2 feet, out Vector2 head)
        {
            feet = head = Vector2.zero;
            var cam = Camera.main;
            if (hero == null || cam == null) return false;
            var f = cam.WorldToScreenPoint(hero.position);
            var h = cam.WorldToScreenPoint(hero.position + Vector3.up * 1.45f);
            if (f.z <= 0f) return false;
            feet = f; head = h;
            return true;
        }

        bool OnHero(Vector2 p, out float centreDist)
        {
            centreDist = float.MaxValue;
            if (!HeroBox(out var f, out var h)) return false;
            float hgt = h.y - f.y;
            float half = Mathf.Max(hgt * 0.34f, Screen.width * 0.06f);
            float pad = Screen.height * 0.04f;
            if (p.x < f.x - half || p.x > f.x + half || p.y < f.y - pad || p.y > h.y + pad) return false;
            centreDist = Mathf.Abs(p.x - f.x);
            return true;
        }

        static int HeightOn(Enemy e, Vector2 p)
        {
            var cam = Camera.main;
            if (cam == null || e == null) return 0;
            var f = cam.WorldToScreenPoint(e.transform.position);
            var h = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 1.45f * e.transform.localScale.y);
            float k = (p.y - f.y) / Mathf.Max(1f, h.y - f.y);
            return k > 0.6f ? 0 : k > 0.34f ? 1 : 2;
        }

        float WorldX(Vector2 p)
        {
            var cam = Camera.main;
            if (cam == null || hero == null) return 0f;
            var ray = cam.ScreenPointToRay(p);
            var plane = new Plane(Vector3.back, hero.position);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d).x : hero.position.x;
        }

        float HeroScreenX => HeroBox(out var f, out _) ? f.x : Screen.width * 0.5f;

        // ---------------------------------------------------------------- the touches
        void V3Began(int id, Vector2 pos)
        {
            var t = new V3Touch { start = pos, now = pos, t0 = Time.unscaledTime };
            var e = EnemyAt(pos);
            bool onHero = OnHero(pos, out float heroDist);
            if (e != null && onHero)
            {
                // close quarters: whichever body the finger is nearer the middle of
                var cam = Camera.main;
                float ex = cam != null ? Mathf.Abs(pos.x - cam.WorldToScreenPoint(e.transform.position).x) : 0f;
                if (heroDist < ex) e = null; else onHero = false;
            }
            if (e != null) { t.zone = Zone.Enemy; t.enemy = e; t.height = HeightOn(e, pos); }
            else if (onHero) t.zone = Zone.Hero;
            else t.zone = Zone.Ground;
            v3Touches[id] = t;
        }

        void V3Moved(int id, Vector2 pos)
        {
            var t = v3Touches[id];
            t.now = pos;
            if (t.done) return;
            float age = Time.unscaledTime - t.t0;
            Vector2 d = pos - t.start;
            switch (t.zone)
            {
                case Zone.Hero:
                    if (d.magnitude > SwipeMin)
                    {
                        // an evasion fires the moment the finger has gone far enough
                        t.done = true;
                        t.guard = false;
                        PushV3(new V3Event { kind = EvadeKind(d), dir = d.normalized, at = pos });
                    }
                    else if (!t.guard && age > GuardAt) t.guard = true;
                    break;
                case Zone.Enemy:
                    {
                        float towardHero = Mathf.Sign(HeroScreenX - t.start.x);
                        float back = d.x * towardHero;
                        if (t.pulling)
                        {
                            V3Pull = Mathf.Clamp01((back - SwipeMin * 0.6f) / (Screen.height * 0.24f));
                            break;
                        }
                        if (d.magnitude > SwipeMin && back > d.magnitude * 0.7f)
                        {
                            // dragged back towards the hero: still down a moment later = a pull, not a swipe
                            if (t.crossedAt < 0f) t.crossedAt = Time.unscaledTime;
                            else if (Time.unscaledTime - t.crossedAt > PullCommit)
                            {
                                t.pulling = true;
                                V3Pulling = true;
                                V3PullEnemy = t.enemy;
                                V3Pull = 0f;
                                PushV3(new V3Event { kind = V3Kind.PullStart, enemy = t.enemy });
                            }
                        }
                        else if (!t.held && age > HoldAt && d.magnitude < SwipeMin * 0.5f)
                        {
                            t.held = true;
                            t.done = true;
                            PushV3(new V3Event { kind = V3Kind.Grab, enemy = t.enemy, height = t.height, at = pos });
                        }
                        break;
                    }
                case Zone.Ground:
                    if (!t.walking && d.magnitude > SwipeMin && age < TapMax)
                    {
                        t.done = true;
                        PushV3(new V3Event { kind = V3Kind.GroundSwipe, dir = d.normalized, worldX = WorldX(pos), at = pos });
                    }
                    else if (age > 0.16f) t.walking = true;
                    break;
            }
        }

        void V3Ended(int id, Vector2 pos)
        {
            var t = v3Touches[id];
            v3Touches.Remove(id);
            consumed.Remove(id);
            V3Moved2(t, pos);
            float age = Time.unscaledTime - t.t0;
            Vector2 d = pos - t.start;
            if (t.zone == Zone.Enemy && t.pulling)
            {
                V3Pulling = false;
                PushV3(new V3Event { kind = V3Pull > 0.12f ? V3Kind.PullRelease : V3Kind.PullCancel, enemy = t.enemy, power = V3Pull });
                return;
            }
            if (t.done || t.walking || t.guard) return;
            if (t.zone == Zone.Enemy)
            {
                if (d.magnitude > SwipeMin) PushV3(new V3Event { kind = V3Kind.Swipe, enemy = t.enemy, height = t.height, dir = d.normalized, at = t.start });
                else if (age < 0.45f) PushV3(new V3Event { kind = V3Kind.Tap, enemy = t.enemy, height = t.height, at = t.start });
            }
            else if (t.zone == Zone.Hero && d.magnitude > SwipeMin)
                PushV3(new V3Event { kind = EvadeKind(d), dir = d.normalized, at = pos });
            else if (t.zone == Zone.Ground && age < TapMax)
            {
                if (d.magnitude > SwipeMin) PushV3(new V3Event { kind = V3Kind.GroundSwipe, dir = d.normalized, worldX = WorldX(pos), at = pos });
                else PushV3(new V3Event { kind = V3Kind.GroundTap, worldX = WorldX(pos), at = t.start });
            }
        }

        void V3Moved2(V3Touch t, Vector2 pos) { t.now = pos; }

        /// <summary>A swipe from the hero: down = duck, up = hop, towards the nearest man = step in,
        /// away = step out.</summary>
        V3Kind EvadeKind(Vector2 d)
        {
            if (Mathf.Abs(d.y) > Mathf.Abs(d.x) * 1.1f) return d.y < 0f ? V3Kind.Duck : V3Kind.Hop;
            int toward = NearestSide();
            return Mathf.Sign(d.x) == toward ? V3Kind.StepIn : V3Kind.StepOut;
        }

        int NearestSide()
        {
            if (hero == null) return 1;
            float best = float.MaxValue;
            int side = hero.right.x >= 0f ? 1 : -1;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.Targetable) continue;
                float dx = e.transform.position.x - hero.position.x;
                if (Mathf.Abs(dx) < best) { best = Mathf.Abs(dx); side = dx >= 0f ? 1 : -1; }
            }
            return side;
        }

        /// <summary>Every frame: the held guard and the walk towards a held finger.</summary>
        void V3Update()
        {
            bool guard = false;
            float axis = 0f;
            foreach (var kv in v3Touches)
            {
                var t = kv.Value;
                if (t.zone == Zone.Hero && t.guard && !t.done)
                {
                    // a still finger on yourself is the guard (checked here too: a still finger sends no moves)
                    guard = true;
                }
                else if (t.zone == Zone.Hero && !t.done && Time.unscaledTime - t.t0 > GuardAt && (t.now - t.start).magnitude < SwipeMin)
                {
                    t.guard = true;
                    guard = true;
                }
                if (t.zone == Zone.Ground && !t.done && (t.walking || Time.unscaledTime - t.t0 > 0.16f) && hero != null)
                {
                    t.walking = true;
                    // walk towards the finger: the further away it is, the faster (walk, jog, run)
                    float dx = WorldX(t.now) - hero.position.x;
                    axis = Mathf.Abs(dx) < 0.18f ? 0f : Mathf.Clamp(dx / 2.6f, -1f, 1f);
                    if (Mathf.Abs(axis) > 0f && Mathf.Abs(axis) < 0.25f) axis = Mathf.Sign(axis) * 0.25f;
                }
            }
            if (keyGuard) guard = true;
            if (Mathf.Abs(keyAxisV3) > 0f) axis = keyAxisV3;
            V3Guard = guard;
            v3Axis = axis;
            if (!V3Pulling) { V3Pull = 0f; V3PullEnemy = null; }
        }

        bool keyGuard;
        float keyAxisV3;

#if ENABLE_INPUT_SYSTEM
        void V3Keys(Keyboard kb)
        {
            keyAxisV3 = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            keyAxisV3 *= kb.leftShiftKey.isPressed ? 1f : 0.45f;           // shift: run
            keyGuard = kb.hKey.isPressed;
            if (kb.jKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Tap, height = 0 });
            if (kb.kKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Tap, height = 1 });
            if (kb.lKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Tap, height = 2 });
            if (kb.iKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Swipe, height = 0, dir = Vector2.right });
            if (kb.uKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Swipe, height = 0, dir = Vector2.up });
            if (kb.nKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Swipe, height = 1, dir = Vector2.down });
            if (kb.gKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Grab });
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Duck });
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.Hop });
            if (kb.qKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.StepOut });
            if (kb.eKey.wasPressedThisFrame) PushV3(new V3Event { kind = V3Kind.StepIn });
            if (kb.pKey.wasPressedThisFrame)
            {
                keyPullAt = Time.unscaledTime;
                V3Pulling = true;
                V3PullEnemy = null;
                PushV3(new V3Event { kind = V3Kind.PullStart });
            }
            if (keyPullAt >= 0f)
            {
                V3Pull = Mathf.Clamp01((Time.unscaledTime - keyPullAt) / 0.7f);
                if (!kb.pKey.isPressed)
                {
                    keyPullAt = -1f;
                    V3Pulling = false;
                    PushV3(new V3Event { kind = V3Kind.PullRelease, power = V3Pull });
                }
            }
        }
#endif
    }
}
