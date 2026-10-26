using System.Collections.Generic;
using System.Linq;
using PokeMoba.Data;
using PokeMoba.World;

namespace PokeMoba.Game
{
    public class TrainerDef
    {
        public string Name;
        public string Title;
        public PokemonType Type;
        public (string Key, int Level)[] Team;
        /// <summary>Medalla que da al ganar (null en el Alto Mando).</summary>
        public string Medal;
        public Biome Arena;
        public int Reward;
    }

    public class ZoneDef
    {
        public int Id;
        public string Name;
        public Biome Biome;
        public int RequiredCaught;
        public string RequiredMedal;
        public bool RequiresAllMedals;
        public bool RequiresLeague;
        public int MinLevel, MaxLevel;
        public TrainerDef Gym;
        /// <summary>Solo en la Liga: Alto Mando y Campeón, en orden.</summary>
        public TrainerDef[] League;
        public string Reward;
    }

    public class ArenaDef
    {
        public string Name;
        public Biome Biome;
        /// <summary>Zona que hay que abrir en la Aventura para usarla (GDD §6).</summary>
        public int RequiredZone;
    }

    /// <summary>Las 16 zonas de Kanto del GDD §6, con sus gimnasios y la Liga.</summary>
    public static class ZoneCatalog
    {
        public static readonly string[] MedalOrder = { "Roca", "Cascada", "Trueno", "Arcoíris", "Alma", "Pantano", "Volcán", "Tierra" };

        static TrainerDef Gym(string name, PokemonType type, string medal, Biome arena, int reward, params (string, int)[] team) =>
            new TrainerDef { Name = name, Title = "Líder de gimnasio", Type = type, Medal = medal, Arena = arena, Reward = reward, Team = team };

        static TrainerDef Elite(string name, string title, PokemonType type, int reward, params (string, int)[] team) =>
            new TrainerDef { Name = name, Title = title, Type = type, Arena = Biome.Liga, Reward = reward, Team = team };

        public static readonly ZoneDef[] Zones =
        {
            new ZoneDef { Id = 1, Name = "Pueblo Paleta y Ruta 1", Biome = Biome.Pradera, MinLevel = 2, MaxLevel = 5,
                Reward = "Ropa inicial" },
            new ZoneDef { Id = 2, Name = "Bosque Verde", Biome = Biome.Bosque, RequiredCaught = 3, MinLevel = 3, MaxLevel = 8,
                Reward = "Gorra de explorador · Arena «Bosque»" },
            new ZoneDef { Id = 3, Name = "Ciudad Plateada y Ruta 3", Biome = Biome.Montana, RequiredCaught = 6, MinLevel = 6, MaxLevel = 12,
                Gym = Gym("Brock", PokemonType.Roca, "Roca", Biome.Montana, 1400, ("geodude", 12), ("onix", 14)),
                Reward = "Chaqueta de montaña" },
            new ZoneDef { Id = 4, Name = "Mt. Moon", Biome = Biome.Cueva, RequiredCaught = 12, RequiredMedal = "Roca", MinLevel = 8, MaxLevel = 14,
                Reward = "Linterna · Arena «Cueva»" },
            new ZoneDef { Id = 5, Name = "Ciudad Celeste y Rutas 24–25", Biome = Biome.Ciudad, RequiredCaught = 18, MinLevel = 12, MaxLevel = 18,
                Gym = Gym("Misty", PokemonType.Agua, "Cascada", Biome.Costa, 2100, ("staryu", 18), ("starmie", 21)),
                Reward = "Ropa marinera · Super Ball" },
            new ZoneDef { Id = 6, Name = "Ciudad Carmín y Cueva Diglett", Biome = Biome.Costa, RequiredCaught = 25, RequiredMedal = "Cascada", MinLevel = 15, MaxLevel = 22,
                Gym = Gym("Lt. Surge", PokemonType.Electrico, "Trueno", Biome.Industrial, 2400, ("voltorb", 21), ("pikachu", 18), ("raichu", 24)),
                Reward = "Uniforme militar · Modo MOBA" },
            new ZoneDef { Id = 7, Name = "Túnel Roca, Pueblo Lavanda y Torre Pokémon", Biome = Biome.Fantasma, RequiredCaught = 35, RequiredMedal = "Trueno", MinLevel = 18, MaxLevel = 26,
                Reward = "Capa oscura" },
            new ZoneDef { Id = 8, Name = "Ciudad Azulona", Biome = Biome.Ciudad, RequiredCaught = 45, MinLevel = 22, MaxLevel = 30,
                Gym = Gym("Erika", PokemonType.Planta, "Arcoíris", Biome.Bosque, 2900, ("victreebel", 29), ("tangela", 24), ("vileplume", 29)),
                Reward = "Kimono · Ultra Ball" },
            new ZoneDef { Id = 9, Name = "Ciudad Fucsia y Zona Safari", Biome = Biome.Safari, RequiredCaught = 60, RequiredMedal = "Arcoíris", MinLevel = 26, MaxLevel = 34,
                Gym = Gym("Koga", PokemonType.Veneno, "Alma", Biome.Fantasma, 4300, ("koffing", 37), ("muk", 39), ("koffing", 37), ("weezing", 43)),
                Reward = "Traje ninja · Arena «Safari»" },
            new ZoneDef { Id = 10, Name = "Ciudad Azafrán y Silph S.A.", Biome = Biome.Industrial, RequiredCaught = 75, RequiredMedal = "Alma", MinLevel = 30, MaxLevel = 38,
                Gym = Gym("Sabrina", PokemonType.Psiquico, "Pantano", Biome.Liga, 4300, ("kadabra", 38), ("mr-mime", 37), ("venomoth", 38), ("alakazam", 43)),
                Reward = "Traje de científico · Master Ball" },
            new ZoneDef { Id = 11, Name = "Central Energía", Biome = Biome.Industrial, RequiredCaught = 85, MinLevel = 33, MaxLevel = 40,
                Reward = "Casco de obrero" },
            new ZoneDef { Id = 12, Name = "Islas Espuma", Biome = Biome.Nieve, RequiredCaught = 90, MinLevel = 35, MaxLevel = 42,
                Reward = "Ropa de nieve" },
            new ZoneDef { Id = 13, Name = "Isla Canela y Mansión", Biome = Biome.Volcan, RequiredCaught = 95, RequiredMedal = "Pantano", MinLevel = 38, MaxLevel = 45,
                Gym = Gym("Blaine", PokemonType.Fuego, "Volcán", Biome.Volcan, 4700, ("growlithe", 42), ("ponyta", 40), ("rapidash", 42), ("arcanine", 47)),
                Reward = "Gafas de sol" },
            new ZoneDef { Id = 14, Name = "Ciudad Verde", Biome = Biome.Ciudad, RequiredCaught = 105, RequiredMedal = "Volcán", MinLevel = 40, MaxLevel = 47,
                Gym = Gym("Giovanni", PokemonType.Tierra, "Tierra", Biome.Montana, 5000, ("rhyhorn", 45), ("dugtrio", 42), ("nidoqueen", 44), ("nidoking", 45), ("rhydon", 50)),
                Reward = "Traje elegante · Mapa 2 del MOBA" },
            new ZoneDef { Id = 15, Name = "Calle Victoria y Liga Pokémon", Biome = Biome.Montana, RequiresAllMedals = true, MinLevel = 45, MaxLevel = 52,
                League = new[]
                {
                    Elite("Lorelei", "Alto Mando", PokemonType.Hielo, 5400, ("dewgong", 54), ("cloyster", 53), ("slowbro", 54), ("jynx", 56), ("lapras", 56)),
                    Elite("Bruno", "Alto Mando", PokemonType.Lucha, 5600, ("onix", 53), ("hitmonchan", 55), ("hitmonlee", 55), ("onix", 56), ("machamp", 58)),
                    Elite("Agatha", "Alto Mando", PokemonType.Fantasma, 5800, ("gengar", 56), ("golbat", 56), ("haunter", 55), ("arbok", 58), ("gengar", 60)),
                    Elite("Lance", "Alto Mando", PokemonType.Dragon, 6200, ("gyarados", 58), ("dragonair", 56), ("dragonair", 56), ("aerodactyl", 60), ("dragonite", 62)),
                    Elite("Azul", "Campeón", PokemonType.Normal, 9000, ("pidgeot", 61), ("alakazam", 59), ("rhydon", 61), ("arcanine", 61), ("exeggutor", 61), ("blastoise", 65)),
                },
                Reward = "Mega-Aro · Capa de campeón" },
            new ZoneDef { Id = 16, Name = "Cueva Celeste", Biome = Biome.Cueva, RequiresLeague = true, MinLevel = 55, MaxLevel = 65,
                Reward = "Arena «Cueva Celeste»" },
        };

        public static readonly ArenaDef[] Arenas =
        {
            new ArenaDef { Name = "Estadio", Biome = Biome.Estadio, RequiredZone = 1 },
            new ArenaDef { Name = "Bosque", Biome = Biome.Bosque, RequiredZone = 2 },
            new ArenaDef { Name = "Cueva", Biome = Biome.Cueva, RequiredZone = 4 },
            new ArenaDef { Name = "Playa", Biome = Biome.Costa, RequiredZone = 6 },
            new ArenaDef { Name = "Torre Pokémon", Biome = Biome.Fantasma, RequiredZone = 7 },
            new ArenaDef { Name = "Safari", Biome = Biome.Safari, RequiredZone = 9 },
            new ArenaDef { Name = "Nieve", Biome = Biome.Nieve, RequiredZone = 12 },
            new ArenaDef { Name = "Volcán", Biome = Biome.Volcan, RequiredZone = 13 },
            new ArenaDef { Name = "Liga Pokémon", Biome = Biome.Liga, RequiredZone = 15 },
            new ArenaDef { Name = "Cueva Celeste", Biome = Biome.Cueva, RequiredZone = 16 },
        };

        public static ZoneDef Get(int id) => Zones.FirstOrDefault(z => z.Id == id);

        /// <summary>Especies salvajes de una zona según overrides.json (campo "zone").</summary>
        public static List<SpeciesData> WildSpecies(ZoneDef zone, IEnumerable<SpeciesData> all) =>
            all.Where(s => s.Zone == zone.Id).ToList();
    }
}
