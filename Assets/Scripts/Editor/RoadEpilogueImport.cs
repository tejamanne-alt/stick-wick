using UnityEditor;

namespace ProjectInk.EditorTools
{
    /// <summary>Import rules for the chapter 5 and 7 sets (tools/blender/build_road_set.py,
    /// build_epilogue_set.py), the same as StorySetImport's for the other story sets: the FBX carries
    /// meshes only (the scene builders assign the baked textures), the baked PNGs are clamped and
    /// mipmapped.</summary>
    public class RoadEpilogueImport : AssetPostprocessor
    {
        static bool Ours(string path) => path.StartsWith("Assets/Art/Road/") || path.StartsWith("Assets/Art/Epilogue/");

        void OnPreprocessModel()
        {
            if (!Ours(assetPath)) return;
            var m = (ModelImporter)assetImporter;
            m.importAnimation = false;
            m.importCameras = false;
            m.importLights = false;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.isReadable = false;
            m.addCollider = false;
            m.importBlendShapes = false;
        }

        void OnPreprocessTexture()
        {
            if (!Ours(assetPath)) return;
            var t = (TextureImporter)assetImporter;
            t.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            t.mipmapEnabled = true;
            t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.sRGBTexture = true;
        }
    }
}
