using System;
using System.IO;
using System.Linq;
using PokeMoba.World;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    public static class AnimatedPokemonValidation
    {
        [MenuItem("PokeMoba/Validar Pokemon con animaciones")]
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int models = 0, clips = 0;
            foreach (string directory in Directory.GetDirectories(PokemonModelImporter.AnimatedFolder).OrderBy(p => p))
            {
                string folder = directory.Replace('\\', '/');
                string path = folder + "/Model.fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) throw new Exception("Model failed to import: " + path);
                var host = new GameObject("AnimatedPokemonValidation");
                try
                {
                    var instance = UnityEngine.Object.Instantiate(asset, host.transform);
                    ImportedPokemonModel.Configure(instance, 1.6f, false, true);
                    var initialBounds = GeometryBounds(instance);
                    if (Mathf.Abs(initialBounds.size.y - 1.6f) > .02f || Mathf.Abs(initialBounds.min.y) > .02f)
                        throw new Exception("Model is tiny, oversized or off the ground: " + path + " " + initialBounds);
                    if (instance.GetComponent<PokemonAnimationDriver>() == null)
                        throw new Exception("Missing animation driver: " + path);
                    var player = instance.GetComponentInChildren<Animation>();
                    var info = JsonUtility.FromJson<PokemonModelImporter.ImportInfo>(File.ReadAllText(folder + "/import.json"));
                    foreach (var clip in info.clips)
                    {
                        if (player[clip.name] == null || player[clip.name].length <= 0)
                            throw new Exception("Missing/empty clip: " + path + " " + clip.name);
                        player.Stop();
                        player.Play(clip.name);
                        player[clip.name].time = player[clip.name].length * .33f;
                        player.Sample();
                        var animatedBounds = GeometryBounds(instance);
                        if (animatedBounds.size.y < .1f || animatedBounds.size.magnitude > 20f)
                            throw new Exception("Collapsed or exploded skin: " + path + " " + clip.name + " " + animatedBounds);
                        clips++;
                    }
                    player.Stop();
                    player.Play("walk");
                    player["walk"].time = 0;
                    player.Sample();
                    var bones = instance.GetComponentsInChildren<Transform>();
                    var before = bones.Select(b => b.localToWorldMatrix).ToArray();
                    player["walk"].time = player["walk"].length * .33f;
                    player.Sample();
                    bool moved = bones.Where((b,i) => b.localToWorldMatrix != before[i]).Any();
                    if (!moved) throw new Exception("Walk clip does not animate the hierarchy: " + path);
                    models++;
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
            if (models != 151 || clips != 869) throw new Exception($"Unexpected coverage: {models} models, {clips} clips");
            Directory.CreateDirectory("../tmp/animated-pokemon");
            File.WriteAllText("../tmp/animated-pokemon/unity-validation.txt", $"PASS: {models} models at correct height and ground position; {clips} clips with visible, bounded skinned geometry; walk sampling changed hierarchy for every species.");
            Debug.Log($"PASS: {models} models and {clips} animation clips.");
        }

        static Bounds GeometryBounds(GameObject instance)
        {
            Bounds bounds = default;
            bool initialized = false;
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                skin.BakeMesh(mesh, true);
                foreach (var v in mesh.vertices)
                {
                    var point = skin.transform.TransformPoint(v);
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            return bounds;
        }
    }
}
