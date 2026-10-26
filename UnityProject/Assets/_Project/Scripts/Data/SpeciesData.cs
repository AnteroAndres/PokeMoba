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
    }

    public class SpeciesData
    {
        public int DexNumber;
        /// <summary>Clave de PokeAPI ("charmander"); es la que se usa en overrides.json.</summary>
        public string Key;
        public string Name;
        /// <summary>Categoría de la Pokédex ("Pokémon Lagartija").</summary>
        public string Genus;
        /// <summary>Rol del MOBA (GDD §7.3): Atacante, Luchador, Defensor, Veloz o Soporte.</summary>
        public string Role;
        public PokemonType[] Types;
        public BaseStats BaseStats;

        /// <summary>Ranura 0 = ataque básico, 1 y 2 = habilidades.</summary>
        public MoveData[] Moves;

        /// <summary>Escala del modelo provisional.</summary>
        public float Size = 1f;
    }
}
