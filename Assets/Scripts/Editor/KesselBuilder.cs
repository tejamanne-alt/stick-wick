using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>Kessel Hill v3 in the fortress scene (docs/story/chapter6_fortress_v3.md): the baked set from
    /// tools/blender/build_kessel_v3.py, its night / lightning bakes on the LightningSwap, and the floors
    /// and stairs as solid colliders from KesselPlan (generated from the same numbers the set is modelled
    /// on). Then the self-test: every floor and stair has its collider, and every level's lane is clear.</summary>
    public static class KesselBuilder
    {
        public const string SetPath = "Assets/Art/FortressV3/KesselSet.fbx";
        // the parts the game moves (so they keep their own transforms)
        static readonly HashSet<string> Movers = new HashSet<string> { "KTruck", "KPortcullisA", "KPortcullisB", "KChandelier1", "KChandelier2", "KChandelier3", "KTelescope" };

        public static void Build(Transform room, LightningSwap swap)
        {
            var root = new GameObject("Kessel").transform;
            root.SetParent(room, false);
            AssetDatabase.ImportAsset(SetPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SetPath);
            if (model == null)
            {
                Debug.LogWarning("[Stick Wick] " + SetPath + " missing: run tools/blender/build_kessel_v3.py --bake --export. Building the floors only.");
            }
            else
            {
                var set = (GameObject)Object.Instantiate(model, root);
                set.name = "KesselSet";
                StoryBuilder.AlignAxes(set.transform);
                var rs = new List<Renderer>(); var ns = new List<Material>(); var fs = new List<Material>(); var ds = new List<Material>();
                foreach (var r in set.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    string n = r.gameObject.name;
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/FortressV3/Story_{n}.png");
                    var m = EnvironmentBuilder.TexMat("Kessel_" + n, "Unlit/Texture", tex, Vector2.one);
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = m;
                    r.sharedMaterials = mats;
                    var flash = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/FortressV3/Story_{n}_Lightning.png");
                    if (flash != null) { rs.Add(r); ns.Add(m); fs.Add(EnvironmentBuilder.TexMat("Kessel_" + n + "_Lightning", "Unlit/Texture", flash, Vector2.one)); ds.Add(null); }
                    if (Movers.Contains(n)) r.gameObject.name = n;               // findable by name at run time
                }
                foreach (var c in set.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                if (swap != null && rs.Count > 0)
                {
                    swap.renderers = Concat(swap.renderers, rs.ToArray());
                    swap.normal = Concat(swap.normal, ns.ToArray());
                    swap.flash = Concat(swap.flash, fs.ToArray());
                    swap.dawn = Concat(swap.dawn, ds.ToArray());
                }
            }

            // the floors and stairs: solid, from the plan
            var floors = new GameObject("KesselFloors").transform;
            floors.SetParent(root, false);
            int made = 0;
            for (int i = 0; i < KesselPlan.Platforms.Length; i++)
            {
                var pl = KesselPlan.Platforms[i];
                if (pl.x0 < 280f) continue;                                         // the old set's floors are its own
                var slab = new GameObject("Floor" + i).transform;
                slab.SetParent(floors, false);
                float thick = pl.oneWay ? 0.1f : 1.0f;
                slab.position = new Vector3((pl.x0 + pl.x1) * 0.5f, pl.y - thick * 0.5f, 0.6f);
                slab.gameObject.AddComponent<BoxCollider>().size = new Vector3(pl.x1 - pl.x0, thick, 3.4f);
                slab.gameObject.isStatic = true;
                made++;
            }
            foreach (var st in KesselPlan.Stairs)
            {
                float sdx = st.x1 - st.x0, sdy = st.y1 - st.y0, len = Mathf.Sqrt(sdx * sdx + sdy * sdy);
                var ramp = new GameObject("KStairSlope").transform;
                ramp.SetParent(floors, false);
                var dir = new Vector3(sdx, sdy, 0f) / len;
                var nrm = new Vector3(-dir.y, dir.x, 0f);
                if (nrm.y < 0f) nrm = -nrm;
                ramp.position = new Vector3((st.x0 + st.x1) * 0.5f, (st.y0 + st.y1) * 0.5f, 0.2f) - nrm * 0.05f;
                ramp.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(sdy, sdx) * Mathf.Rad2Deg);
                ramp.gameObject.AddComponent<BoxCollider>().size = new Vector3(len, 0.1f, 1.4f);
                ramp.gameObject.isStatic = true;
                made++;
            }

            // each room's own acoustic (docs/story/chapter6_fortress_v3.md §4): open air, the passage's slap echo,
            // the tower's stone spiral, the gallery's dry closeness, the hall's long reverb, the dome's ring
            var presets = new[] { AudioReverbPreset.Mountains, AudioReverbPreset.Mountains, AudioReverbPreset.Plain, AudioReverbPreset.StoneCorridor,
                                  AudioReverbPreset.Mountains, AudioReverbPreset.Cave, AudioReverbPreset.Quarry, AudioReverbPreset.Room,
                                  AudioReverbPreset.Auditorium, AudioReverbPreset.Mountains, AudioReverbPreset.Hangar };
            for (int i = 0; i < KesselPlan.Levels.Length && i < presets.Length; i++)
            {
                var L = KesselPlan.Levels[i];
                var z = new GameObject("Acoustic_" + L.name).AddComponent<AudioReverbZone>();
                z.transform.SetParent(root, false);
                z.transform.position = new Vector3((L.x0 + L.x1) * 0.5f, L.y + 2f, 0f);
                z.reverbPreset = presets[i];
                z.minDistance = (L.x1 - L.x0) * 0.5f;
                z.maxDistance = (L.x1 - L.x0) * 0.5f + 3f;
            }

            SelfTest(floors);
            Debug.Log("[Stick Wick] Kessel Hill v3: " + made + " floors and stairs");
        }

        static T[] Concat<T>(T[] a, T[] b)
        {
            if (a == null) return b;
            var r = new T[a.Length + b.Length];
            a.CopyTo(r, 0); b.CopyTo(r, a.Length);
            return r;
        }

        /// <summary>The build fails if a floor of the plan isn't solid, or a level's lane would let a fighter
        /// stand inside the set (a probe down every 0.5 m of every floor must find that floor's own top).</summary>
        static void SelfTest(Transform floors)
        {
            Physics.SyncTransforms();
            var errs = new List<string>();
            foreach (var pl in KesselPlan.Platforms)
            {
                if (pl.x0 < 280f) continue;
                for (float x = pl.x0 + 0.25f; x < pl.x1 - 0.2f; x += 0.5f)
                {
                    if (!Physics.Raycast(new Vector3(x, pl.y + 0.5f, 0f), Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore))
                    { errs.Add($"no floor under x {x:0.0} at y {pl.y}"); break; }
                    if (hit.point.y < pl.y - 0.1f) { errs.Add($"the floor at x {x:0.0} is at {hit.point.y:0.00}, the plan says {pl.y}"); break; }
                }
            }
            if (errs.Count > 0) throw new BuildFailedException("[Stick Wick] Kessel Hill self-test failed:\n  " + string.Join("\n  ", errs));
        }
    }
}
