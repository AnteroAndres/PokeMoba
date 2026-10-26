using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace PokeMoba.Data.Pokedex
{
    /// <summary>
    /// Carga los Pokémon desde Resources/Data (generados por Tools/import_pokeapi).
    /// Si faltan los datos, usa los 3 iniciales escritos a mano de PrototypeDex.
    /// </summary>
    public static class PokemonDatabase
    {
        const string DataResource = "Data/pokeapi_001-151";
        const string OverridesResource = "Data/overrides";

        /// <summary>Nivel con el que se eligen los movimientos por defecto (el del Duelo 1v1, GDD §7.2).</summary>
        public const int MoveLevel = 50;

        static Pokedex dex;

        public static Pokedex Dex
        {
            get
            {
                if (dex == null) Load();
                return dex;
            }
        }

        public static IReadOnlyList<SpeciesData> All => Dex.All;
        public static SpeciesData Get(string key) => Dex.Get(key);
        public static SpeciesData Get(int dexNumber) => Dex.Get(dexNumber);
        public static MoveData[] MovesAt(SpeciesData species, int level) => Dex.MovesAt(species, level);

        static void Load()
        {
            var dataAsset = Resources.Load<TextAsset>(DataResource);
            if (dataAsset == null)
            {
                Debug.LogWarning($"PokeMoba: no se encontró Resources/{DataResource}.json. " +
                                 "Ejecuta Tools/import_pokeapi. Se usan los 3 iniciales de prueba.");
                dex = new Pokedex(PrototypeDex.Starters.ToList(), null);
                return;
            }

            var raw = JsonConvert.DeserializeObject<RawPokedex>(dataAsset.text);
            var overridesAsset = Resources.Load<TextAsset>(OverridesResource);
            var overrides = overridesAsset != null
                ? JsonConvert.DeserializeObject<RawOverrides>(overridesAsset.text)
                : new RawOverrides();

            dex = PokedexBuilder.Build(raw, overrides, MoveLevel, message => Debug.LogWarning(message));
            Debug.Log($"PokeMoba: {dex.All.Count} especies cargadas.");
        }
    }
}
