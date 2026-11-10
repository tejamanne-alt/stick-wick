using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>Every scene of the game goes into every build. Unity 6's build profiles can keep a
    /// scene list of their own that overrides the shared one: an Android profile made before the
    /// later chapters existed still builds only the menu and chapter 1, and the chapters' buttons then
    /// do nothing on the phone. This writes the full list both to the shared list and to the active
    /// build profile, and checks it before every build.</summary>
    public class BuildScenes : IPreprocessBuildWithReport
    {
        public static readonly string[] All =
        {
            PrototypeBuilder.MenuScenePath, PrototypeBuilder.StoryScenePath, PrototypeBuilder.BankScenePath,
            PrototypeBuilder.StreetScenePath, PrototypeBuilder.HomeScenePath, PrototypeBuilder.ParkScenePath,
            PrototypeBuilder.RoadScenePath, PrototypeBuilder.FortressScenePath, PrototypeBuilder.EpilogueScenePath,
            PrototypeBuilder.ScenePath,
        };

        public int callbackOrder => -100;

        [MenuItem("Stick Wick/Add All Scenes To The Build")]
        public static void Apply()
        {
            var list = All.Where(p => System.IO.File.Exists(p)).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            EditorBuildSettings.scenes = list;
            SetProfileScenes(list);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stick Wick] build scenes: {list.Length} ({string.Join(", ", list.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)))})");
        }

        /// <summary>The active build profile's own scene list (Unity 6), through reflection so this
        /// compiles on any version.</summary>
        static void SetProfileScenes(EditorBuildSettingsScene[] list)
        {
            try
            {
                System.Type t = null;
                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = a.GetType("UnityEditor.Build.Profile.BuildProfile");
                    if (t != null) break;
                }
                if (t == null) return;
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
                var get = t.GetMethod("GetActiveBuildProfile", F);
                var profile = get != null ? get.Invoke(null, null) as Object : null;
                if (profile == null) return;
                var scenes = t.GetProperty("scenes", F);
                if (scenes != null && scenes.CanWrite) scenes.SetValue(profile, list);
                EditorUtility.SetDirty(profile);
                Debug.Log($"[Stick Wick] build profile '{profile.name}': scene list set too");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Stick Wick] could not set the build profile's scenes: " + e.Message);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // make sure the lists are right for the next build, and stop this one if it is missing chapters
            var missing = All.Where(p => System.IO.File.Exists(p) && !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == p)).ToList();
            Apply();
            CheckCombatV3();
            if (missing.Count > 0)
                throw new BuildFailedException("[Stick Wick] the build was missing scenes (" + string.Join(", ", missing.Select(System.IO.Path.GetFileNameWithoutExtension)) +
                                               "). They have now been added to the build's scene list: build again.");
        }

        /// <summary>Chapter 1 is Combat 3.0's test bed: its scene must be built on the V3 body (the
        /// model, its move set and the InkDepth shader), or the phone gets the old fighters with no
        /// COMBAT NEW / CLASSIC switch. A scene built before the V3 body existed stops the build.</summary>
        static void CheckCombatV3()
        {
            string want = System.IO.File.Exists(StickmanImport.ModelPathV4) ? StickmanImport.ModelPathV4 : StickmanImport.ModelPathV3;
            if (!System.IO.File.Exists(want)) return;                                 // no new body in this checkout
            var deps = AssetDatabase.GetDependencies(PrototypeBuilder.StoryScenePath, true);
            bool model = deps.Contains(want);
            bool shader = deps.Any(d => d.EndsWith("InkDepth.shader"));
            if (model && shader)
            {
                Debug.Log("[Stick Wick] chapter 1 builds on the new body (" + System.IO.Path.GetFileNameWithoutExtension(want) + " + InkDepth)");
                return;
            }
            throw new BuildFailedException("[Stick Wick] chapter 1's scene is not on the Combat 3.0 body (" + (model ? "" : System.IO.Path.GetFileNameWithoutExtension(want) + " missing ") +
                                           (shader ? "" : "InkDepth missing") + "). Run Stick Wick → Build All Scenes (or Build Android APK), then build again.");
        }
    }
}
