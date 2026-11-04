using UnityEditor;
using UnityEngine;

namespace ProjectInk.EditorTools
{
    /// <summary>Import settings for the sound bank (Resources/Audio): one-shots decompress on
    /// load so they play on the exact frame; the long ambience loops stream from disk.
    /// Vorbis throughout, which keeps the APK small.</summary>
    public class AudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            bool loop = assetPath.Contains("/amb_");
            var s = imp.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = loop ? 0.5f : 0.7f;
            s.loadType = loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !loop;
            imp.loadInBackground = loop;
        }
    }
}
