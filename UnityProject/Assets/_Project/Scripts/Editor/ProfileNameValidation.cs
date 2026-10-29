using System;
using System.IO;
using PokeMoba.Game;
using UnityEditor;
using UnityEngine;
namespace PokeMoba.Editor
{
    public static class ProfileNameValidation
    {
        [MenuItem("PokeMoba/Validar nombres de perfil")]
        public static void Run()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var registry = new ProfileNameRegistry(path);
                if (!registry.Check("JugadorPrueba", "test-a", true, out _, out _)) throw new Exception("No se pudo reservar el nombre.");
                if (registry.Check("jugadorprueba", "test-b", false, out _, out var options)) throw new Exception("Se aceptó un duplicado.");
                if (options.Count != 3 || !registry.Check(options[0], "test-b", true, out _, out _)) throw new Exception("Fallaron las variantes.");
                if (!registry.Check("JugadorPrueba", "test-a", false, out _, out _)) throw new Exception("No reconoce al propietario.");
                Debug.Log("Perfil: prueba correcta. Se rechazó el nombre duplicado y se reservó una variante. No se modificó la partida.");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}
