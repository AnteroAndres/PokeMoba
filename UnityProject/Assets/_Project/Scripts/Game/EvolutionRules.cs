using System;
using System.Collections.Generic;
using PokeMoba.Data;

namespace PokeMoba.Game
{
    /// <summary>
    /// Cuándo puede evolucionar un Pokémon en la Aventura.
    /// Por nivel: como en los juegos. Por piedra o por intercambio: de momento, al llegar a un nivel fijo
    /// (hasta que existan la tienda de piedras y el Cable Unión del GDD §11).
    /// </summary>
    public static class EvolutionRules
    {
        public const int ItemEvolutionLevel = 30;
        public const int TradeEvolutionLevel = 37;
        public const int OtherEvolutionLevel = 30;

        public static List<SpeciesData> Options(SpeciesData species, int level, Func<int, SpeciesData> getByDex)
        {
            var result = new List<SpeciesData>();
            foreach (var evolution in species.Evolutions)
            {
                var target = getByDex(evolution.TargetDex);
                if (target == null) continue;
                if (level >= RequiredLevel(evolution)) result.Add(target);
            }
            return result;
        }

        public static int RequiredLevel(EvolutionData evolution)
        {
            switch (evolution.Trigger)
            {
                case "level-up": return evolution.MinLevel ?? OtherEvolutionLevel;
                case "trade": return TradeEvolutionLevel;
                case "use-item": return ItemEvolutionLevel;
                default: return OtherEvolutionLevel;
            }
        }
    }
}
