using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>Import rules for the generated stickman FBX: generic rig (all characters
    /// share one skeleton), no imported materials, loop flags from the clip JSON.</summary>
    public class StickmanImport : AssetPostprocessor
    {
        public const string ModelPath = "Assets/Art/Stickman/Stickman.fbx";
        public const string ClipJsonPath = "Assets/Art/Stickman/stickman_clips.json";
        /// <summary>Combat 3.0's body (chapter 1's experiment): the same rig with longer arms and
        /// fists, every clip carried over, the strikes, guard and reactions hand-keyed anew.</summary>
        public const string ModelPathV3 = "Assets/Art/StickmanV3/StickmanV3.fbx";
        public const string ClipJsonPathV3 = "Assets/Art/StickmanV3/stickman_v3_clips.json";
        /// <summary>The 29-bone body (chapter 1): spine in three, neck, collarbones, wrists and fingers,
        /// feet and toes; every clip carried over (tools/blender/build_stickman_v4.py).</summary>
        public const string ModelPathV4 = "Assets/Art/StickmanV4/StickmanV4.fbx";
        public const string ClipJsonPathV4 = "Assets/Art/StickmanV4/stickman_v4_clips.json";
        /// <summary>The V4 clips are split over several files (each well under GitHub's 50 MB): StickmanV4.fbx has
        /// the body, the rig and the core clips; StickmanV4_&lt;part&gt;.fbx the rig and the rest (build_stickman_v4.py
        /// FILES). A clip binds to the body by bone path, whichever file it comes from.</summary>
        public static bool IsV4File(string p) => p.StartsWith("Assets/Art/StickmanV4/StickmanV4") && p.EndsWith(".fbx");

        /// <summary>Every file holding V4 clips: the body's first, then its parts.</summary>
        public static string[] ModelPathsV4()
        {
            var parts = System.IO.Directory.Exists("Assets/Art/StickmanV4")
                ? System.IO.Directory.GetFiles("Assets/Art/StickmanV4", "StickmanV4_*.fbx").Select(f => f.Replace('\\', '/')).OrderBy(f => f)
                : Enumerable.Empty<string>();
            return new[] { ModelPathV4 }.Concat(parts).ToArray();
        }

        static bool Ours(string p) => p == ModelPath || p == ModelPathV3 || IsV4File(p);
        static string JsonFor(string p) => IsV4File(p) ? ClipJsonPathV4 : p == ModelPathV3 ? ClipJsonPathV3 : ClipJsonPath;

        [System.Serializable] public class ClipInfo { public string name; public int frames; public int fps; public bool loop; public int hitFrame; public float endShift; public int loadFrame = -1; public float reach; }
        [System.Serializable] public class ClipList { public ClipInfo[] clips; }

        public static ClipList LoadClipInfo() => LoadClipInfo(ClipJsonPath);

        public static ClipList LoadClipInfo(string jsonPath)
        {
            // read the file directly: during import the JSON may not be an imported asset yet
            string full = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), jsonPath);
            if (!System.IO.File.Exists(full)) return new ClipList { clips = new ClipInfo[0] };
            return JsonUtility.FromJson<ClipList>(System.IO.File.ReadAllText(full));
        }

        /// <summary>"Stickman|idle" → "idle".</summary>
        public static string ShortName(string takeName)
        {
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        void OnPreprocessModel()
        {
            if (!Ours(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            // The V4 part files hold the rig alone, a single root ("Stickman"), and Unity reuses a lone root as the
            // prefab root: their clips' curves then start at "hips/..." while the body's (two roots: the rig and
            // the mesh) start at "Stickman/hips/...", so none of them would move the body (walk, idle_loose, the
            // far moves, CROSSFIRE, the weapons: the men froze in the bind pose and slid). Keep every root.
            if (IsV4File(assetPath)) importer.preserveHierarchy = true;
        }

        /// <summary>Part files imported before the rule above keep their old curve paths until imported again:
        /// on load, any V4 file still without it is re-imported (only those files, once).</summary>
        [InitializeOnLoadMethod]
        static void ReimportV4Parts()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var p in ModelPathsV4())
                    if (AssetImporter.GetAtPath(p) is ModelImporter m && !m.preserveHierarchy)
                    {
                        m.preserveHierarchy = true;
                        m.SaveAndReimport();
                        Debug.Log("Stick Wick: re-imported " + p + " so its clips bind to the body");
                    }
            };
        }

        void OnPreprocessAnimation()
        {
            if (!Ours(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            var info = LoadClipInfo(JsonFor(assetPath));
            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                var meta = info.clips.FirstOrDefault(i => i.name == ShortName(c.takeName));
                c.name = ShortName(c.takeName);
                c.loopTime = meta != null && meta.loop;
                c.lockRootRotation = false;
                c.lockRootHeightY = false;
                c.lockRootPositionXZ = false;
            }
            importer.clipAnimations = clips;
        }
    }
}
