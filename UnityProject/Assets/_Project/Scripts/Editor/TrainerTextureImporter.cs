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
                System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
        }
    }
}
