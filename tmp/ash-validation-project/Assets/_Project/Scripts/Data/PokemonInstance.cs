using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace PokeMoba.Data
{
    /// <summary>Un Pokémon concreto del jugador (se guarda en la partida).</summary>
    [Serializable, Preserve]
    public class PokemonInstance
    {
        public string Id;
        public string Key;
        public int Level;
        public int Exp;
        public bool Shiny;
        public string Ball;
        /// <summary>Vida actual como fracción (0–1). 0 = debilitado.</summary>
        public float HpFraction = 1f;

        [JsonIgnore] public bool Fainted => HpFraction <= 0f;

        public static PokemonInstance Create(SpeciesData species, int level, bool shiny, string ball)
        {
            return new PokemonInstance
            {
                Id = Guid.NewGuid().ToString("N"),
                Key = species.Key,
                Level = level,
                Exp = Leveling.ExpForLevel(species.GrowthRate, level),
                Shiny = shiny,
                Ball = ball,
                HpFraction = 1f
            };
        }

        /// <summary>Suma experiencia y devuelve cuántos niveles subió.</summary>
        public int AddExp(SpeciesData species, int amount)
        {
            int before = Level;
            Exp += amount;
            Level = Math.Max(Level, Leveling.LevelForExp(species.GrowthRate, Exp));
            return Level - before;
        }
    }
}
