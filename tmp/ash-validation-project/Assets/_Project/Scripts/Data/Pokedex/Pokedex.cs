using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeMoba.Data.Pokedex
{
    /// <summary>
    /// Todas las especies cargadas, con búsqueda por clave y por número, y los movimientos
    /// de una especie a cualquier nivel (la Aventura y el MOBA no pelean a nivel 50).
    /// </summary>
    public class Pokedex
    {
        readonly List<SpeciesData> species;
        readonly Dictionary<string, SpeciesData> byKey;
        readonly Dictionary<int, SpeciesData> byDex;
        readonly Func<SpeciesData, int, MoveData[]> movesAtLevel;
        readonly Dictionary<(string, int), MoveData[]> moveCache = new Dictionary<(string, int), MoveData[]>();

        public Pokedex(List<SpeciesData> species, Func<SpeciesData, int, MoveData[]> movesAtLevel)
        {
            this.species = species;
            this.movesAtLevel = movesAtLevel;
            byKey = species.Where(s => s.Key != null).ToDictionary(s => s.Key);
            byDex = species.ToDictionary(s => s.DexNumber);
        }

        public IReadOnlyList<SpeciesData> All => species;

        public SpeciesData Get(string key) => key != null && byKey.TryGetValue(key, out var s) ? s : null;

        public SpeciesData Get(int dexNumber) => byDex.TryGetValue(dexNumber, out var s) ? s : null;

        public MoveData[] MovesAt(SpeciesData s, int level)
        {
            if (movesAtLevel == null) return s.Moves;
            var key = (s.Key, level);
            if (!moveCache.TryGetValue(key, out var moves))
                moveCache[key] = moves = movesAtLevel(s, level);
            return moves;
        }

        /// <summary>Primera etapa de la línea evolutiva (Charizard → Charmander).</summary>
        public SpeciesData BaseForm(SpeciesData s)
        {
            while (s.EvolvesFrom.HasValue && Get(s.EvolvesFrom.Value) != null)
                s = Get(s.EvolvesFrom.Value);
            return s;
        }

        /// <summary>Camino de evolución desde la primera etapa hasta <paramref name="target"/> (incluidos).</summary>
        public List<SpeciesData> EvolutionPath(SpeciesData target)
        {
            var path = new List<SpeciesData> { target };
            var current = target;
            while (current.EvolvesFrom.HasValue && Get(current.EvolvesFrom.Value) != null)
            {
                current = Get(current.EvolvesFrom.Value);
                path.Insert(0, current);
            }
            return path;
        }

        /// <summary>Especies que no evolucionan más (para elegir bots del MOBA).</summary>
        public IEnumerable<SpeciesData> FinalStages => species.Where(s => s.Evolutions.Count == 0);
    }
}
