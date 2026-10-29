#if UNITY_EDITOR
using PokeMoba.Prototype;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    [CustomEditor(typeof(PrototypeGame))]
    public sealed class PrototypeGameEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Panel privado de creador: solo existe en el Editor. Durante Play, abre F10 en la ventana Game. Los botones de recursos y nivel guardan tu partida con una copia previa.",MessageType.Info);
            DrawDefaultInspector();
            var game = (PrototypeGame)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Abrir / cerrar menú en el juego (F10)")) game.ToggleCreator();
                if (GUILayout.Button("Aplicar dinero, Poké Balls y objetos")) game.CreatorResources();
                if (GUILayout.Button("Aplicar nivel al puesto del equipo")) game.CreatorLevel();
                if (GUILayout.Button("Añadir Pokémon al equipo / PC")) game.CreatorGive();
                if (GUILayout.Button("Aplicar vida e invulnerabilidad")) game.CreatorHealth();
                if (GUILayout.Button("Curar equipo y entrenador")) game.CreatorHeal();
            }
            EditorGUILayout.HelpBox(game.CreatorStatus,MessageType.None);
        }
    }
}
#endif
