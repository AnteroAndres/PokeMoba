using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PokeMoba.Game
{
    /// <summary>Registro local por instalación. La exclusividad online necesita un servidor.</summary>
    public class ProfileNameRegistry
    {
        readonly string path;
        public ProfileNameRegistry(string path) => this.path = path;
        public static string Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Escribe un nombre.";
            value = value.Trim().Normalize();
            if (value.Length < 3 || value.Length > 16) return "Usa entre 3 y 16 caracteres.";
            foreach (char c in value) if (!char.IsLetterOrDigit(c) && c != '_') return "Usa letras, números o guion bajo.";
            return null;
        }
        public bool Check(string value, string owner, bool reserve, out string message, out List<string> suggestions)
        {
            suggestions = new List<string>();
            message = Validate(value);
            if (message != null) return false;
            value = value.Trim().Normalize();
            try
            {
                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    string json;
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true)) json = reader.ReadToEnd();
                    var data = string.IsNullOrWhiteSpace(json) ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (data == null) throw new InvalidDataException();
                    var names = new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
                    if (names.TryGetValue(value, out var account) && account != owner)
                    {
                        for (int i = 1; suggestions.Count < 3 && i < 10000; i++)
                        {
                            string suffix = "_" + i;
                            string candidate = value.Substring(0, Math.Min(value.Length, 16 - suffix.Length)) + suffix;
                            if (!names.ContainsKey(candidate)) suggestions.Add(candidate);
                        }
                        message = "Este nombre ya está ocupado en el registro local. Prueba una variante.";
                        return false;
                    }
                    if (reserve)
                    {
                        var old = new List<string>();
                        foreach (var pair in names) if (pair.Value == owner) old.Add(pair.Key);
                        foreach (var key in old) names.Remove(key);
                        names[value] = owner;
                        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(names, Formatting.Indented));
                        stream.Position = 0; stream.Write(bytes, 0, bytes.Length); stream.SetLength(bytes.Length); stream.Flush();
                    }
                    message = "Nombre disponible en este dispositivo.";
                    return true;
                }
            }
            catch (Exception) { message = "No se pudo comprobar el registro. Inténtalo de nuevo."; return false; }
        }
    }
}
