namespace PokeMoba.World
{
    /// <summary>Tipo de escenario. Decide suelo, cielo, luz y decoración (EnvironmentBuilder).</summary>
    public enum Biome
    {
        Estadio,    // arena estándar del duelo
        Pradera,    // Pueblo Paleta, rutas
        Bosque,     // Bosque Verde
        Montana,    // Ciudad Plateada, Calle Victoria
        Cueva,      // Mt. Moon, Túnel Roca, Cueva Celeste
        Ciudad,     // Celeste, Carmín, Azulona, Azafrán, Ciudad Verde
        Costa,      // Carmín, Islas Espuma (playa)
        Fantasma,   // Pueblo Lavanda, Torre Pokémon
        Safari,     // Zona Safari
        Industrial, // Central Energía, Silph S.A.
        Nieve,      // Islas Espuma (interior)
        Volcan,     // Isla Canela
        Liga        // Meseta Añil
    }
}
