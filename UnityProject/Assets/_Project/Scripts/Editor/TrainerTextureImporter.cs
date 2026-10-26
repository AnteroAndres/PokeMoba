using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    /// <summary>Los PNG del personaje son mapas 2D, no cubemaps. No inicia reimportaciones.</summary>
    public class TrainerTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Resources/Models/Trainers/MasterRoshi/",
                System.StringComparison.Ordinal) &&
                !assetPath.StartsWith("Assets/_Project/Resources/Models/Trainers/ImportedFbx/",
                System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.alphaIsTransparency = true;
            foreach (string platform in new[] { "Standalone", "Android", "iPhone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = false;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
