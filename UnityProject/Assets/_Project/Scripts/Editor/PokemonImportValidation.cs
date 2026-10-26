using System;
using System.IO;
using System.Linq;
using PokeMoba.World;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    public static class PokemonImportValidation
    {
        [MenuItem("PokeMoba/Validar modelos de Pokemon importados")]
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = PokemonModelImporter.Folder;
            int total = 0, textured = 0;
            var failures = new System.Collections.Generic.List<string>();
            foreach (string path in Directory.GetDirectories(root).OrderBy(p => p))
            {
                string folder = path.Replace('\\', '/');
                string modelPath = Directory.GetFiles(folder, "Model.*").FirstOrDefault(p => !p.EndsWith(".meta"));
                if (modelPath == null) { failures.Add(folder + ": no model"); continue; }
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (asset == null) { failures.Add(folder + ": not imported"); continue; }
                var host = new GameObject("ImportValidation");
                host.transform.rotation = Quaternion.Euler(0, 123, 0);
                var instance = UnityEngine.Object.Instantiate(asset, host.transform);
                ImportedPokemonModel.Configure(instance, 1.6f, false, true);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) failures.Add(folder + ": no renderers");
                else
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    if (Mathf.Abs(bounds.size.y - 1.6f) > .05f || Mathf.Abs(bounds.min.y) > .05f)
                        failures.Add(folder + ": incorrect bounds " + bounds);
                    int textures = 0;
                    foreach (var renderer in renderers)
                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null || material.shader == null || material.shader.name.Contains("Error"))
                                failures.Add(folder + ": missing material/shader");
                            else if (material.mainTexture != null) textures++;
                        }
                    if (textures > 0) textured++;
                    var info = JsonUtility.FromJson<PokemonModelImporter.ImportInfo>(File.ReadAllText(folder + "/import.json"));
                    if (info.bindings.Length > 0 && textures == 0) failures.Add(folder + ": missing textures");
                }
                UnityEngine.Object.DestroyImmediate(host);
                total++;
            }
            if (total != 151) failures.Add("Expected 151 models; found " + total);
            string report = $"Models: {total}; textured: {textured}; failures: {failures.Count}\n" + string.Join("\n", failures);
            Directory.CreateDirectory("../tmp/pokemon-import");
            File.WriteAllText("../tmp/pokemon-import/unity-validation.txt", report);
            Debug.Log(report);
            AssetDatabase.SaveAssets();
            if (failures.Count > 0) throw new Exception(report);
        }
    }
}
