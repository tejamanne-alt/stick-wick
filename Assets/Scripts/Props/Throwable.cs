using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectInk
{
    /// <summary>What lies about in each chapter that a man can pick up and use. Every kit is chosen
    /// from the place and the story, so no two chapters fight alike (docs/design/combat_v3.md, "props"):
    ///   1 WAREHOUSE  the night crew's beer bottles (the mezzanine office), a forklift wrench, the
    ///                crane's hoist chain lying in the crane bay
    ///   2 STREET     the café's and the market's bottles, bricks shaken loose from the railway arches
    ///   3 HOME       the Cleaner's bleach bottle in the kitchen (thrown in the eyes: he's blind for a
    ///                moment), a wine bottle in the living room. Nothing of Elena's or Maya's: he
    ///                would never throw their things.
    ///   4 BACKYARD   flower pots in the garden, stones from the pond's edge
    ///   6 FORTRESS   loose stones from the crumbling battlements, the guards' torches (in the
    ///                blackout a thrown torch spins its beam across the dark and blinds who it hits)
    /// Picked up by walking over it (or a tap on it); a tap on a man throws it at him wherever he is;
    /// within arm's reach it is smashed on him instead. The men pick them up too, and throw them at
    /// you: tap towards one as it comes and you swat it aside.</summary>
    public enum PropKind { Bottle, Wrench, Brick, Bleach, Pot, Stone, Torch, Chain }

    public class Throwable : MonoBehaviour
    {
        public static readonly List<Throwable> All = new List<Throwable>();
        public PropKind kind;
        public bool Lying => holder == null;
        public Component holder;
        float bob;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>How each one hits: damage, knocked down, the reaction, how long it blinds or
        /// dazes, whether it breaks, how fast it flies, its sound.</summary>
        public struct Spec { public float damage, daze, speed; public bool down, breaks, heavy; public string react, label; public Sfx sound; }

        public static Spec SpecOf(PropKind k)
        {
            switch (k)
            {
                case PropKind.Bottle: return new Spec { damage = 10, react = "hit_chin", breaks = true, speed = 15, sound = Sfx.Glass, label = "BOTTLE!" };
                case PropKind.Wrench: return new Spec { damage = 18, down = true, heavy = true, react = "hit_chin", speed = 14, sound = Sfx.MetalClang, label = "WRENCH!" };
                case PropKind.Brick: return new Spec { damage = 16, down = true, heavy = true, breaks = true, react = "hit_front", speed = 13, sound = Sfx.Crate, label = "BRICK!" };
                case PropKind.Bleach: return new Spec { damage = 4, daze = 2.6f, breaks = true, react = "hit_chin", speed = 12, sound = Sfx.Splash, label = "BLINDED!" };
                case PropKind.Pot: return new Spec { damage = 11, breaks = true, react = "hit_chin", speed = 13, sound = Sfx.Crate, label = "FLOWER POT!" };
                case PropKind.Stone: return new Spec { damage = 14, heavy = true, react = "hit_front", speed = 16, sound = Sfx.PunchHeavy, label = "STONE!" };
                case PropKind.Torch: return new Spec { damage = 8, daze = 1.6f, react = "hit_chin", speed = 15, sound = Sfx.MetalClang, label = "TORCH!" };
                default: return new Spec { damage = 16, react = "hit_front", speed = 0, sound = Sfx.Chain, label = "CHAIN!" };
            }
        }

        public static Throwable Spawn(PropKind kind, Vector3 at)
        {
            var go = new GameObject("Prop" + kind);
            var t = go.AddComponent<Throwable>();
            t.kind = kind;
            Build(go.transform, kind);
            float y = Fighter.GroundY(at + Vector3.up * 0.5f);
            go.transform.position = new Vector3(at.x, y, 0.15f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, kind == PropKind.Bottle || kind == PropKind.Bleach || kind == PropKind.Pot ? 0f : 90f);
            if (kind == PropKind.Bottle) go.transform.rotation = Quaternion.Euler(0f, 0f, Random.value < 0.5f ? 0f : 84f);   // some stand, some lie
            t.bob = Random.value * 6f;
            return t;
        }

        void Update()
        {
            if (!Lying) return;
            // a slow glint so the eye finds it (the ink world has no item glow)
            bob += Time.deltaTime;
            float g = 0.5f + 0.5f * Mathf.Sin(bob * 3.2f);
            if (glint != null) glint.localScale = Vector3.one * (0.05f + 0.05f * g);
        }

        Transform glint;

        // ------------------------------------------------------------------ the models (ink palette)
        static Material ink, pale, mid;
        static Material Mat(ref Material m, Color c)
        {
            if (m == null) { var sh = Shader.Find("Unlit/Color"); m = new Material(sh != null ? sh : Shader.Find("Sprites/Default")) { color = c }; }
            return m;
        }

        static Transform Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        /// <summary>The model, its origin at the bottom (lying) / the grip (held).</summary>
        public static void Build(Transform root, PropKind k)
        {
            var dark = Mat(ref ink, new Color(0.09f, 0.09f, 0.1f));
            var light = Mat(ref pale, new Color(0.82f, 0.83f, 0.85f));
            var grey = Mat(ref mid, new Color(0.42f, 0.42f, 0.44f));
            var m = new GameObject("Model").transform;
            m.SetParent(root, false);
            switch (k)
            {
                case PropKind.Bottle:
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.09f, 0), new Vector3(0.07f, 0.09f, 0.07f), grey);
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.22f, 0), new Vector3(0.028f, 0.05f, 0.028f), grey);
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(0.072f, 0.03f, 0.072f), light);          // the label
                    break;
                case PropKind.Bleach:
                    Part(m, PrimitiveType.Cube, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.22f, 0.07f), light);
                    Part(m, PrimitiveType.Cylinder, new Vector3(0.02f, 0.25f, 0), new Vector3(0.035f, 0.02f, 0.035f), dark);       // the cap
                    Part(m, PrimitiveType.Cube, new Vector3(-0.035f, 0.17f, 0), new Vector3(0.025f, 0.08f, 0.072f), light);       // the handle
                    break;
                case PropKind.Wrench:
                    Part(m, PrimitiveType.Cube, new Vector3(0, 0.16f, 0), new Vector3(0.035f, 0.32f, 0.018f), grey);
                    Part(m, PrimitiveType.Cube, new Vector3(0, 0.34f, 0), new Vector3(0.09f, 0.05f, 0.02f), grey);
                    Part(m, PrimitiveType.Cube, new Vector3(0.03f, 0.375f, 0), new Vector3(0.025f, 0.05f, 0.02f), grey);
                    break;
                case PropKind.Brick:
                    Part(m, PrimitiveType.Cube, new Vector3(0, 0.035f, 0), new Vector3(0.21f, 0.065f, 0.1f), grey);
                    break;
                case PropKind.Pot:
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(0.15f, 0.08f, 0.15f), grey);
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.165f, 0), new Vector3(0.175f, 0.012f, 0.175f), grey);
                    Part(m, PrimitiveType.Sphere, new Vector3(0, 0.2f, 0), new Vector3(0.13f, 0.08f, 0.13f), dark);              // soil and a dead plant
                    Part(m, PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(0.01f, 0.18f, 0.01f), dark, new Vector3(0, 0, 12));
                    break;
                case PropKind.Stone:
                    Part(m, PrimitiveType.Sphere, new Vector3(0, 0.06f, 0), new Vector3(0.16f, 0.11f, 0.13f), grey, new Vector3(0, 0, 20));
                    break;
                case PropKind.Torch:
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.045f, 0.12f, 0.045f), dark);
                    Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.07f, 0.02f, 0.07f), light);
                    break;
                case PropKind.Chain:
                    for (int i = 0; i < 9; i++)                                                                                  // a loose coil on the floor
                        Part(m, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(i * 1.4f) * 0.12f, 0.012f + i * 0.004f, Mathf.Sin(i * 1.4f) * 0.06f),
                             new Vector3(0.05f, 0.006f, 0.03f), grey, new Vector3(90f, i * 40f, 0f));
                    break;
            }
            var t = root.GetComponent<Throwable>();
            if (t != null && k != PropKind.Chain)
                t.glint = Part(root, PrimitiveType.Sphere, new Vector3(0.05f, 0.18f, -0.1f), Vector3.one * 0.06f, light);
        }

        /// <summary>Held: the model moved to a hand.</summary>
        public void TakeInto(Component by, Transform hand)
        {
            holder = by;
            if (glint != null) { Destroy(glint.gameObject); glint = null; }
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            transform.localScale = Vector3.one / Mathf.Max(0.01f, hand.lossyScale.x);
            if (kind == PropKind.Chain) foreach (Transform c in transform) c.gameObject.SetActive(false);   // the chain is drawn by its line
        }

        public static Throwable Near(Vector3 at, float range)
        {
            Throwable best = null;
            float bd = range;
            foreach (var t in All)
            {
                if (t == null || !t.Lying) continue;
                if (Mathf.Abs(t.transform.position.y - at.y) > 0.6f) continue;
                float d = Mathf.Abs(t.transform.position.x - at.x);
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }

        public static Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var f = FindBone(c, name);
                if (f != null) return f;
            }
            return null;
        }

        // ------------------------------------------------------------------ the kits, chapter by chapter
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            All.RemoveAll(t => t == null);
            (PropKind k, float x, float y)[] kit;
            switch (s.name)
            {
                case "Story_Warehouse":
                    kit = new[]
                    {
                        (PropKind.Wrench, WarehousePlan.ForkX0 - 1.0f, 0f),            // dropped by the forklift
                        (PropKind.Bottle, 23.5f, 0f),                                  // the loading bay's night crew
                        (PropKind.Bottle, 24.2f, 0f),
                        (PropKind.Wrench, 34.8f, 0f),
                        (PropKind.Bottle, WarehousePlan.MezzX0 + 2.6f, WarehousePlan.MezzY),   // the office on the mezzanine
                        (PropKind.Bottle, WarehousePlan.MezzX0 + 5.4f, WarehousePlan.MezzY),
                        (PropKind.Chain, WarehousePlan.HoistX + 0.8f, 0f),             // the crane's hoist chain, fallen
                        (PropKind.Bottle, 53.5f, 0f),
                    };
                    break;
                case "Story_Street":
                    kit = new[]
                    {
                        (PropKind.Bottle, 35.5f, 0f), (PropKind.Bottle, 40.5f, 0f), (PropKind.Bottle, 51f, 0f),     // the café, the market
                        (PropKind.Brick, 66.5f, 0f), (PropKind.Brick, 74f, 0f), (PropKind.Brick, 83.5f, 0f),        // loose from the arches
                        (PropKind.Bottle, 104f, 0f), (PropKind.Brick, 113f, 0f),
                    };
                    break;
                case "Story_Home":
                    kit = new[]
                    {
                        (PropKind.Bleach, 28.6f, 0f),                                  // the Cleaner's, in the kitchen
                        (PropKind.Bottle, HomePlan.CoffeeTableX + 0.6f, 0f),           // wine, the living room
                    };
                    break;
                case "Story_Backyard":
                    kit = new[]
                    {
                        (PropKind.Pot, 9f, 0f), (PropKind.Pot, 17.5f, 0f), (PropKind.Pot, 30.5f, 0f),             // the garden
                        (PropKind.Stone, 69f, 0f), (PropKind.Stone, 77.5f, 0f), (PropKind.Stone, 90f, 0f),        // the pond's edge
                        (PropKind.Bottle, 104f, 0f), (PropKind.Bottle, 118f, 0f),                                 // the car park
                    };
                    break;
                case "Story_Fortress":
                    kit = new[]
                    {
                        (PropKind.Stone, 57f, FortressPlan.Up), (PropKind.Stone, 64.5f, FortressPlan.Up),         // rubble at the gate
                        (PropKind.Stone, 79f, FortressPlan.Up), (PropKind.Torch, 92f, FortressPlan.Up),
                        (PropKind.Torch, FortressPlan.GeneratorX - 2.5f, FortressPlan.Up),                         // where the lights go out
                        (PropKind.Torch, 108f, FortressPlan.Up), (PropKind.Stone, 124f, FortressPlan.Up),
                    };
                    break;
                default: return;
            }
            foreach (var (k, x, y) in kit) Spawn(k, new Vector3(x, y, 0f));
        }
    }

    /// <summary>A prop in flight: spins along a low arc to the man it was thrown at, and does what it
    /// does when it lands (Throwable.SpecOf). Swatted, it goes spinning off and breaks on the floor.</summary>
    public class ThrownProp : MonoBehaviour
    {
        public static readonly List<ThrownProp> All = new List<ThrownProp>();
        public PropKind kind;
        public Fighter thrower, target;
        public Vector3 velocity;
        public bool swatted;
        float born, spin;
        Transform model;
        Light beam;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool AtHero => target is HeroController;

        public static ThrownProp Launch(PropKind kind, Fighter thrower, Fighter target, Vector3 from, int facing)
        {
            var go = new GameObject("Thrown" + kind);
            go.transform.position = from;
            var t = go.AddComponent<ThrownProp>();
            t.kind = kind;
            t.thrower = thrower;
            t.target = target;
            var spec = Throwable.SpecOf(kind);
            t.model = new GameObject("Spin").transform;
            t.model.SetParent(go.transform, false);
            Throwable.Build(t.model, kind);
            foreach (Transform c in t.model) c.localPosition -= new Vector3(0f, 0.12f, 0f);   // spin about its middle
            if (target != null)
            {
                // a low arc: the time of flight from the distance, the lift to land on his chest
                Vector3 to = target.Chest;
                float dist = Mathf.Abs(to.x - from.x);
                float T = Mathf.Clamp(dist / spec.speed, 0.12f, 0.75f);
                t.velocity = new Vector3((to.x - from.x) / T, (to.y - from.y) / T + 0.5f * 9f * T, 0f);
            }
            else t.velocity = new Vector3(facing * spec.speed, 2f, 0f);
            t.spin = (kind == PropKind.Bottle || kind == PropKind.Wrench || kind == PropKind.Torch ? 900f : 520f) * -Mathf.Sign(t.velocity.x);
            if (kind == PropKind.Torch)
            {
                var l = new GameObject("Beam").AddComponent<Light>();
                l.transform.SetParent(t.model, false);
                l.type = LightType.Spot;
                l.spotAngle = 40f;
                l.range = 9f;
                l.intensity = 3f;
                l.color = new Color(0.95f, 0.95f, 0.9f);
                l.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                t.beam = l;
            }
            t.born = Time.time;
            return t;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity.y -= 9f * dt;
            transform.position += velocity * dt;
            model.Rotate(0f, 0f, spin * dt, Space.Self);
            if (!swatted)
            {
                // the man it was thrown at (or anyone of the other side in its way)
                if (AtHero)
                {
                    var h = (HeroController)target;
                    if (h != null && Vector3.Distance(h.Chest, transform.position) < 0.55f) { HitFighter(h); return; }
                }
                else
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || !e.Targetable || e == thrower) continue;
                        if (Vector3.Distance(e.Chest, transform.position) < 0.6f * Mathf.Max(1f, e.transform.localScale.x)) { HitFighter(e); return; }
                    }
            }
            if (transform.position.y < Fighter.GroundY(transform.position + Vector3.up * 0.3f) + 0.05f || Time.time - born > 2.5f)
            {
                Break(transform.position, false);
            }
        }

        /// <summary>Knocked aside by a forearm: off it goes, up and away, and breaks on the floor.</summary>
        public void Swat(int awayDir)
        {
            swatted = true;
            velocity = new Vector3(awayDir * 3.5f, 4.5f, 0f);
            spin *= -1.6f;
            Sound.Play(Sfx.PunchLight, 1f, transform.position);
        }

        void HitFighter(Fighter f)
        {
            var spec = Throwable.SpecOf(kind);
            int d = velocity.x >= 0f ? 1 : -1;
            f.TakeHit(new HitInfo { damage = spec.damage, direction = d, knockback = spec.down ? 3f : 1.5f, knockdown = spec.down, heavy = spec.heavy,
                                    react = spec.react, attacker = thrower });
            if (spec.daze > 0f && f is Enemy e) e.Blind(spec.daze);
            Sound.Play(spec.sound, 1f, transform.position);
            Sound.Play(Sfx.PunchHeavy, 0.6f, transform.position);
            if (!AtHero)
            {
                if (TimeController.Instance != null) TimeController.Instance.HitStop(spec.heavy ? 6 : 4);
                if (CameraDirector.Instance != null) CameraDirector.Instance.Shake(spec.heavy ? 0.14f : 0.08f, 0.15f);
                if (WaveDirector.Instance != null) WaveDirector.Instance.AddStyle(spec.heavy ? 140 : 100, spec.label);
            }
            if (spec.breaks) { Break(transform.position, true); return; }
            // it bounces off him and drops
            swatted = true;
            velocity = new Vector3(-d * 1.5f, 2.5f, 0f);
        }

        void Break(Vector3 at, bool onMan)
        {
            var spec = Throwable.SpecOf(kind);
            if (InkFx.Instance != null)
            {
                Color c = kind == PropKind.Pot ? new Color(0.1f, 0.1f, 0.1f) : kind == PropKind.Bleach ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.7f, 0.72f, 0.75f);
                InkFx.Instance.Splash(at, new Vector3(Mathf.Sign(velocity.x), 0.6f, 0f), spec.breaks ? 18 : 6, c);
            }
            if (!onMan) Sound.Play(spec.breaks ? spec.sound : Sfx.Land, 0.8f, at);
            Enemy.Noise(at);
            // a wrench, a stone or a torch that missed lies where it fell: it can be picked up again
            if (!spec.breaks && kind != PropKind.Chain) Throwable.Spawn(kind, new Vector3(at.x, at.y, 0f));
            Destroy(gameObject);
        }
    }
}
