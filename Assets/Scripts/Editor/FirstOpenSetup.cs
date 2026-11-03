using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>On the first open of the project (no prototype scene yet), build the
    /// prototype scene automatically and open it, so pressing Play just works.
    /// Skipped in batch mode (CI builds the scene itself).</summary>
    [InitializeOnLoad]
    static class FirstOpenSetup
    {
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        const string SessionKey = "ProjectInk.FirstOpenSetup.Ran";

        static FirstOpenSetup()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += Run;
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

            if (!File.Exists(ScenePath))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(StickmanImport.ModelPath) == null)
                {
                    Debug.LogWarning("[Project INK] Stickman.fbx not imported yet; use Project INK → Build Prototype Scene once it is.");
                    return;
                }
                try { PrototypeBuilder.BuildScene(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[Project INK] Could not build the prototype scene: " + e);
                    return;
                }
            }

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath && !active.isDirty)
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
