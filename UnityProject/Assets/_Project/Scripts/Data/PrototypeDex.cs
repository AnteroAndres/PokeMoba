namespace PokeMoba.Data
{
    /// <summary>
    /// Especies y movimientos de la fase 1, escritos a mano. Desde la fase 2 solo se usan
    /// como reserva si faltan los datos importados de PokeAPI (ver PokemonDatabase).
    /// </summary>
    public static class PrototypeDex
    {
        // --- Movimientos ---

        static readonly MoveData Placaje = new MoveData
        {
            Name = "Placaje", Type = PokemonType.Normal, Category = MoveCategory.Fisico, Power = 40,
            Shape = MoveShape.Melee, Range = 2.2f, Radius = 1.6f, Cooldown = 0.6f, Windup = 0.1f
        };

        static readonly MoveData Aranazo = new MoveData
        {
            Name = "Arañazo", Type = PokemonType.Normal, Category = MoveCategory.Fisico, Power = 40,
            Shape = MoveShape.Melee, Range = 2.2f, Radius = 1.6f, Cooldown = 0.55f, Windup = 0.1f
        };

        static readonly MoveData Ascuas = new MoveData
        {
            Name = "Ascuas", Type = PokemonType.Fuego, Category = MoveCategory.Especial, Power = 40,
            Shape = MoveShape.Projectile, Range = 10f, Radius = 0.5f, Cooldown = 2.5f, Windup = 0.15f,
            ProjectileSpeed = 16f, Status = StatusEffect.Quemado, StatusChance = 0.1f
        };

        static readonly MoveData Lanzallamas = new MoveData
        {
            Name = "Lanzallamas", Type = PokemonType.Fuego, Category = MoveCategory.Especial, Power = 90,
            Shape = MoveShape.Cone, Range = 5.5f, Radius = 50f, Cooldown = 7f, Windup = 0.35f,
            Status = StatusEffect.Quemado, StatusChance = 0.1f
        };

        static readonly MoveData PistolaAgua = new MoveData
        {
            Name = "Pistola Agua", Type = PokemonType.Agua, Category = MoveCategory.Especial, Power = 40,
            Shape = MoveShape.Projectile, Range = 10f, Radius = 0.5f, Cooldown = 2.5f, Windup = 0.15f,
            ProjectileSpeed = 14f
        };

        static readonly MoveData Hidrobomba = new MoveData
        {
            Name = "Hidrobomba", Type = PokemonType.Agua, Category = MoveCategory.Especial, Power = 110,
            Shape = MoveShape.GroundArea, Range = 9f, Radius = 2.2f, Cooldown = 8f, Windup = 0.2f,
            TelegraphTime = 0.8f
        };

        static readonly MoveData LatigoCepa = new MoveData
        {
            Name = "Látigo Cepa", Type = PokemonType.Planta, Category = MoveCategory.Fisico, Power = 45,
            Shape = MoveShape.Melee, Range = 2.8f, Radius = 1.6f, Cooldown = 0.65f, Windup = 0.12f
        };

        static readonly MoveData BombaLodo = new MoveData
        {
            Name = "Bomba Lodo", Type = PokemonType.Veneno, Category = MoveCategory.Especial, Power = 90,
            Shape = MoveShape.Projectile, Range = 9f, Radius = 0.7f, Cooldown = 4.5f, Windup = 0.25f,
            ProjectileSpeed = 11f, Status = StatusEffect.Envenenado, StatusChance = 0.3f
        };

        static readonly MoveData Derribo = new MoveData
        {
            Name = "Derribo", Type = PokemonType.Normal, Category = MoveCategory.Fisico, Power = 90,
            Shape = MoveShape.Dash, Range = 6f, Radius = 1.2f, Cooldown = 6f, Windup = 0.2f
        };

        // --- Especies ---

        public static readonly SpeciesData Bulbasaur = new SpeciesData
        {
            DexNumber = 1, Key = "bulbasaur", Name = "Bulbasaur",
            Types = new[] { PokemonType.Planta, PokemonType.Veneno },
            BaseStats = new BaseStats(45, 49, 49, 65, 65, 45),
            Moves = new[] { LatigoCepa, BombaLodo, Derribo },
            Size = 1f
        };

        public static readonly SpeciesData Charmander = new SpeciesData
        {
            DexNumber = 4, Key = "charmander", Name = "Charmander",
            Types = new[] { PokemonType.Fuego },
            BaseStats = new BaseStats(39, 52, 43, 60, 50, 65),
            Moves = new[] { Aranazo, Ascuas, Lanzallamas },
            Size = 0.95f
        };

        public static readonly SpeciesData Squirtle = new SpeciesData
        {
            DexNumber = 7, Key = "squirtle", Name = "Squirtle",
            Types = new[] { PokemonType.Agua },
            BaseStats = new BaseStats(44, 48, 65, 50, 64, 43),
            Moves = new[] { Placaje, PistolaAgua, Hidrobomba },
            Size = 0.95f
        };

        public static readonly SpeciesData[] Starters = { Bulbasaur, Charmander, Squirtle };
    }
}
