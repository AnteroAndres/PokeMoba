using System.IO;
using PokeMoba.Prototype;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokeMoba.EditorTools
{
    /// <summary>
    /// Crea la escena del prototipo la primera vez que se abre el proyecto
    /// (o desde el menú PokeMoba) y deja lista la configuración de Android.
    /// </summary>
    [InitializeOnLoad]
    public static class PrototypeSceneCreator
    {
        const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";

        static PrototypeSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
                CreatePrototypeScene();
            };
        }

        [MenuItem("PokeMoba/Crear escena del prototipo")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("PrototypeGame").AddComponent<PrototypeGame>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            ConfigurePlayer();
            Debug.Log($"PokeMoba: escena creada en {ScenePath}. Pulsa Play para probar.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Hermanos PokeMoba";
            PlayerSettings.productName = "PokeMoba";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.hermanos.pokemoba");
        }
    }
}
