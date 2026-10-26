using System.Collections.Generic;

namespace PokeMoba.Data
{
    public struct BaseStats
    {
        public int Hp, Attack, Defense, SpAttack, SpDefense, Speed;

        public BaseStats(int hp, int attack, int defense, int spAttack, int spDefense, int speed)
        {
            Hp = hp; Attack = attack; Defense = defense;
            SpAttack = spAttack; SpDefense = spDefense; Speed = speed;
        }

        public int Total => Hp + Attack + Defense + SpAttack + SpDefense + Speed;
    }

    public class EvolutionData
    {
        public int TargetDex;
        /// <summary>level-up, use-item, trade…</summary>
        public string Trigger;
        public int? MinLevel;
        public string Item;
    }

    public class SpeciesData
    {
        public int DexNumber;
        /// <summary>Clave de PokeAPI ("charmander"); es la que se usa en overrides.json.</summary>
        public string Key;
        public string Name;
        /// <summary>Categoría de la Pokédex ("Pokémon Lagartija").</summary>
        public string Genus;
        public string FlavorText;
        /// <summary>Rol del MOBA (GDD §7.3): Atacante, Luchador, Defensor, Veloz o Soporte.</summary>
        public string Role;
        public string Diet;
        /// <summary>Zona de Kanto donde aparece (GDD §6); null si se obtiene evolucionando.</summary>
        public int? Zone;

        public PokemonType[] Types;
        public BaseStats BaseStats;
        public int CaptureRate = 45;
        public int BaseExperience = 64;
        public string GrowthRate = "medium";
        public float HeightM = 1f;
        public float WeightKg = 10f;
        public bool IsLegendary;
        public bool IsMythical;

        /// <summary>Forma del cuerpo (PokeAPI): quadruped, upright, wings, squiggle… Decide el modelo provisional.</summary>
        public string BodyShape = "upright";
        /// <summary>Color principal (PokeAPI): red, blue, green, yellow…</summary>
        public string BodyColor;

        public int? EvolvesFrom;
        public List<EvolutionData> Evolutions = new List<EvolutionData>();

        /// <summary>Movimientos a nivel 50 (Duelo). Ranura 0 = ataque básico, 1 y 2 = habilidades.</summary>
        public MoveData[] Moves;

        /// <summary>Escala del modelo provisional.</summary>
        public float Size = 1f;

        public bool IsSpecial => IsLegendary || IsMythical;
    }
}
