using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    /// <summary>Explicit per-species bindings avoid matching another Pokemon's "Material #1".</summary>
    public class PokemonModelImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Resources/Models/Pokemon/Imported/";
        [Serializable] public class Binding { public string material; public string texture; public string channel; }
        [Serializable] public class ImportInfo { public Binding[] bindings; public int dex; public string key; }
        static readonly HashSet<string> Pending = new HashSet<string>();
        public override uint GetVersion() => 1;
        bool IsPokemon => assetPath.StartsWith(Folder, StringComparison.Ordinal);

        void OnPreprocessModel()
        {
            if (!IsPokemon) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.meshCompression = ModelImporterMeshCompression.Off;
        }

        void OnPreprocessTexture()
        {
            if (!IsPokemon) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
        }

        void OnPostprocessMaterial(Material material)
        {
            if (!IsPokemon) return;
            string directory = Path.GetDirectoryName(assetPath).Replace('\\','/');
            string infoPath = directory + "/import.json";
            if (!File.Exists(infoPath)) return;
            var info = JsonUtility.FromJson<ImportInfo>(File.ReadAllText(infoPath));
            // Seaking's DAE has geometry and named regions, but the ZIP contains no skin texture.
            if (info.dex == 119)
            {
                material.shader = Shader.Find("Standard");
                material.color = material.name.Contains("BodyA") ? new Color(1f, .43f, .12f)
                    : material.name.Contains("BodyB") ? new Color(1f, .94f, .8f) : new Color(.12f, .1f, .09f);
                material.SetFloat("_Glossiness", .12f);
            }
            if (info.bindings == null) return;
            foreach (var binding in info.bindings)
            {
                if (Normalize(binding.material) != Normalize(material.name)) continue;
                if (!binding.channel.Contains("Diffuse") && binding.channel.Length > 0) continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + binding.texture);
                if (texture == null) continue;
                var shader = Shader.Find("Standard");
                if (shader != null) material.shader = shader;
                material.mainTexture = texture;
                material.color = Color.white;
                material.SetFloat("_Glossiness", .12f);
                material.SetFloat("_Metallic", 0);
                var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (textureImporter != null && textureImporter.DoesSourceTextureHaveAlpha())
                {
                    material.SetFloat("_Mode", 1);
                    material.SetFloat("_Cutoff", .25f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                }
                break;
            }
        }

        // Models may be imported before their image dependencies on the first project refresh.
        // Rebind only affected species after those textures/sidecars have finished importing.
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            foreach (string path in imported)
            {
                if (!path.StartsWith(Folder, StringComparison.Ordinal)) continue;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".png" && extension != ".tga" && extension != ".jpg" && extension != ".json") continue;
                string relative = path.Substring(Folder.Length);
                int slash = relative.IndexOf('/');
                if (slash < 0) continue;
                Pending.Add(Folder + relative.Substring(0, slash));
            }
            if (Pending.Count == 0) return;
            EditorApplication.delayCall -= RebindPending;
            EditorApplication.delayCall += RebindPending;
        }

        static void RebindPending()
        {
            var paths = new List<string>(Pending);
            Pending.Clear();
            foreach (string directory in paths)
            {
                if (!Directory.Exists(directory)) continue;
                foreach (string file in Directory.GetFiles(directory, "Model.*"))
                    if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        AssetDatabase.ImportAsset(file.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }
        }
        static string Normalize(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", "");
    }
}
