using UnityEditor;

namespace ProjectInk.EditorTools
{
    /// <summary>Import rules for the story set (tools/blender/build_story_set.py): the FBX carries
    /// meshes only (no animation, cameras, lights or materials: the scene builder assigns the baked
    /// textures), and the baked PNGs are clamped, mipmapped and kept at full resolution.</summary>
    public class StorySetImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Art/Story/")) return;
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
            if (!assetPath.StartsWith("Assets/Art/Story/")) return;
            var t = (TextureImporter)assetImporter;
            t.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            t.mipmapEnabled = true;
            t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.sRGBTexture = true;
        }
    }
}
