using System;
using System.IO;
using System.Collections.Generic;
using PokeMoba.Game;
class ProfileNameRegistryTests
{
    static void Assert(bool ok, string test) { if (!ok) throw new Exception(test); Console.WriteLine("PASS: " + test); }
    static void Main()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var registry = new ProfileNameRegistry(path);
            Assert(registry.Check("JugadorPrueba", "account-a", true, out _, out _), "Reserva un nombre disponible");
            Assert(registry.Check("JugadorPrueba", "account-a", false, out _, out _), "El propietario conserva su nombre");
            Assert(!registry.Check("jugadorprueba", "account-b", false, out _, out var options), "Rechaza duplicados sin distinguir mayusculas");
            Assert(options.Count == 3 && options[0] == "jugadorprueba_1", "Ofrece tres variantes disponibles");
            Assert(registry.Check(options[0], "account-b", true, out _, out _), "Permite reservar una variante");
            Assert(!new ProfileNameRegistry(path).Check("JUGADORPRUEBA", "account-c", true, out _, out _), "Conserva reservas al volver a abrir el registro");
            Assert(!registry.Check("a", "account-c", true, out _, out _), "Rechaza nombres demasiado cortos");
            Assert(!registry.Check("Jugador !", "account-c", true, out _, out _), "Rechaza caracteres no permitidos");
            Assert(registry.Check("OtroJugador", "account-a", true, out _, out _) && registry.Check("JugadorPrueba", "account-c", true, out _, out _), "Libera el nombre anterior al renombrar");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
