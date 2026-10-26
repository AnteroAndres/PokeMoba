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

        /// <summary>Nivel con el que se eligen los movimientos (el del Duelo 1v1, GDD §7.2).</summary>
        public const int MoveLevel = 50;

        static List<SpeciesData> species;
        static Dictionary<string, SpeciesData> byKey;

        public static IReadOnlyList<SpeciesData> All
        {
            get
            {
                if (species == null) Load();
                return species;
            }
        }

        public static SpeciesData Get(string key)
        {
            if (species == null) Load();
            return key != null && byKey.TryGetValue(key, out var result) ? result : null;
        }

        static void Load()
        {
            var dataAsset = Resources.Load<TextAsset>(DataResource);
            if (dataAsset == null)
            {
                Debug.LogWarning($"PokeMoba: no se encontró Resources/{DataResource}.json. " +
                                 "Ejecuta Tools/import_pokeapi. Se usan los 3 iniciales de prueba.");
                SetSpecies(PrototypeDex.Starters.ToList());
                return;
            }

            var dex = JsonConvert.DeserializeObject<RawPokedex>(dataAsset.text);
            var overridesAsset = Resources.Load<TextAsset>(OverridesResource);
            var overrides = overridesAsset != null
                ? JsonConvert.DeserializeObject<RawOverrides>(overridesAsset.text)
                : new RawOverrides();

            SetSpecies(PokedexBuilder.Build(dex, overrides, MoveLevel, message => Debug.LogWarning(message)));
            Debug.Log($"PokeMoba: {species.Count} especies cargadas.");
        }

        static void SetSpecies(List<SpeciesData> list)
        {
            species = list;
            byKey = list.Where(s => s.Key != null).ToDictionary(s => s.Key);
        }
    }
}
