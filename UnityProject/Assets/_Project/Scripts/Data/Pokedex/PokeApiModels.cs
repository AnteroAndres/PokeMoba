using System.Collections.Generic;
using UnityEngine.Scripting;

// Estructura de los JSON que genera Tools/import_pokeapi. Solo se declaran los campos que usa el juego;
// Newtonsoft ignora el resto y no distingue mayúsculas (camelCase del JSON → PascalCase aquí).
// [Preserve] evita que IL2CPP elimine estas clases en el APK (solo se crean por reflexión).
namespace PokeMoba.Data.Pokedex
{
    [Preserve]
    public class RawPokedex
    {
        public List<RawSpecies> Species;
        public List<RawMove> Moves;
    }

    [Preserve]
    public class RawSpecies
    {
        public int Id;
        public string Key;
        public string Name;
        public string Genus;
        public string FlavorText;
        public List<string> Types;
        public RawStats BaseStats;
        public int CaptureRate;
        public int? BaseExperience;
        public string GrowthRate;
        public float HeightM;
        public float WeightKg;
        public string Shape;   // forma del cuerpo de PokeAPI: quadruped, upright, wings…
        public string Color;   // color principal de PokeAPI: red, blue, green…
        public bool IsLegendary;
        public bool IsMythical;
        public int? EvolvesFrom;
        public List<RawEvolution> Evolutions;
        public RawLearnset Learnset;
    }

    [Preserve]
    public class RawEvolution
    {
        public int Species;
        public List<RawEvolutionMethod> Methods;
    }

    [Preserve]
    public class RawEvolutionMethod
    {
        public string Trigger;   // level-up, use-item, trade…
        public int? MinLevel;
        public string Item;
    }

    [Preserve]
    public class RawStats
    {
        public int Hp, Attack, Defense, SpAttack, SpDefense, Speed;
    }

    [Preserve]
    public class RawLearnset
    {
        public string VersionGroup;
        public List<RawLevelMove> LevelUp;
        public List<string> Machine;
    }

    [Preserve]
    public class RawLevelMove
    {
        public int Level;
        public string Move;
    }

    [Preserve]
    public class RawMove
    {
        public string Key;
        public string Name;
        public string Type;
        public string Category;       // Fisico, Especial, Estado
        public int? Power;
        public int? Accuracy;
        public int Priority;
        public string Ailment;        // burn, poison, paralysis…
        public int AilmentChance;
        public string SuggestedShape; // Melee, Projectile, Cone, GroundArea, Dash, Self
    }

    [Preserve]
    public class RawOverrides
    {
        public Dictionary<string, SpeciesOverride> Species;
        public Dictionary<string, MoveOverride> Moves;
    }

    [Preserve]
    public class SpeciesOverride
    {
        public string Role;
        public string Diet;
        public string FavoriteFlavor;
        public int? Zone;
        public string BasicMove;
        public List<string> Skills;
        /// <summary>4.ª ranura: el movimiento más potente.</summary>
        public string Ultimate;
    }

    [Preserve]
    public class MoveOverride
    {
        public string Shape;
        public float? Range;
        public float? Radius;
        public float? Cooldown;
    }
}
