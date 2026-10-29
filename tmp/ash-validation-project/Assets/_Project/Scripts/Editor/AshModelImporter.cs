using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    /// <summary>Keep Ash's original rig and bind each of his three atlases within his own folder.</summary>
    public class AshModelImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Resources/Models/Trainers/ImportedFbx/Ash/";
        public override uint GetVersion() => 1;
        static bool pending;
        bool IsAsh => assetPath.StartsWith(Folder, StringComparison.Ordinal);
        void OnPreprocessModel()
        {
            if (!IsAsh) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.preserveHierarchy = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            string name = Path.GetFileNameWithoutExtension(assetPath) == "Run" ? "run" : "idle";
            var defaults = importer.defaultClipAnimations;
            if (defaults.Length > 0)
            {
                var clip = defaults[0];
                clip.name = name;
                clip.loopTime = true;
                clip.wrapMode = WrapMode.Loop;
                importer.clipAnimations = new[] { clip };
            }
        }

        void OnPostprocessMaterial(Material material)
        {
            if (!IsAsh) return;
            Texture2D texture = null;
            string materialName = Normalize(material.name.Replace(" (Instance)", ""));
            foreach (string name in new[] { "Ash_arms_hat_hair.png", "PokeTra_Ash_face.png", "trAsh_00_body_col.png" })
                if (Normalize(name) == materialName || Normalize(Path.GetFileNameWithoutExtension(name)) == materialName)
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + name);
            if (texture == null) return;
            material.shader = Shader.Find("Standard");
            material.mainTexture = texture;
            material.color = Color.white;
            material.SetFloat("_Glossiness", .12f);
            material.SetFloat("_Metallic", 0);
        }

        static string Normalize(string value) => System.Text.RegularExpressions.Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", "");
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] old)
        {
            if (pending || !Array.Exists(imported, p => p.StartsWith(Folder, StringComparison.Ordinal) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))) return;
            pending = true;
            EditorApplication.delayCall += () =>
            {
                pending = false;
                foreach (string name in new[] { "Ash.fbx", "Run.fbx" })
                    AssetDatabase.ImportAsset(Folder + name, ImportAssetOptions.ForceUpdate);
            };
        }
    }
}
