using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>Builds and opens the prototype scene automatically: on the first open, and
    /// again whenever a pull brings a newer scene version (PrototypeBuilder.SceneVersion).
    /// Also re-applies the landscape-only player settings. Skipped in batch mode.</summary>
    [InitializeOnLoad]
    static class FirstOpenSetup
    {
        const string ScenePath = PrototypeBuilder.MenuScenePath;
        const string SessionKey = "ProjectInk.FirstOpenSetup.Ran";

        static FirstOpenSetup()
        {
            if (Application.isBatchMode) return;
            if (SessionState.GetBool(SessionKey, false) && !Outdated()) return;
            EditorApplication.delayCall += Run;
        }

        static bool Outdated()
        {
            if (!File.Exists(ScenePath)) return true;               // deleted (a clean checkout): build it again
            if (!File.Exists(PrototypeBuilder.VersionFile)) return true;
            return File.ReadAllText(PrototypeBuilder.VersionFile).Trim() != PrototypeBuilder.SceneVersion.ToString();
        }

        static void Run()
        {
            // wait until Unity has finished importing and compiling
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            PrototypeBuilder.ConfigurePlayer();
            if (!File.Exists(ScenePath) || Outdated())
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(StickmanImport.ModelPath) == null)
                {
                    Debug.LogWarning("[Stick Wick] Stickman.fbx not imported yet; use Stick Wick → Build Prototype Scene once it is.");
                    return;
                }
                try { PrototypeBuilder.BuildScene(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[Stick Wick] Could not build the prototype scene: " + e);
                    return;
                }
            }

            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return;
            if (active.path != ScenePath && !active.isDirty)
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
