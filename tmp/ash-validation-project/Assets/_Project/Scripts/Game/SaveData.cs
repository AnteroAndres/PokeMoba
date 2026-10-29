using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PokeMoba.Data;
using UnityEngine.Scripting;

namespace PokeMoba.Game
{
    /// <summary>Partida guardada. Se escribe como JSON en Application.persistentDataPath.</summary>
    [Serializable, Preserve]
    public class SaveData
    {
        public const int MaxTeam = 6;

        public int Version = 1;

        // Entrenador (GDD §12: personalización básica)
        public string TrainerName = "Rojo";
        public string ProfileId = Guid.NewGuid().ToString("N");
        public bool HasCustomProfileName;
        public string ProfilePortraitResource;
        [JsonIgnore] public string ProfileDisplayName => HasCustomProfileName ? TrainerName : SelectedCharacterName;

        public static string CharacterName(string key)
        {
            return TrainerCharacterCatalog.Label(key);
        }

        [JsonIgnore] public string SelectedCharacterName => CharacterName(TrainerCharacter);

        public int TrainerLevel = 1;
        public int TrainerExperience;

        [JsonIgnore] public int TrainerExperienceRequired => 500 + Math.Max(1, TrainerLevel) * 50;

        public int AwardTrainerExperience(int amount)
        {
            TrainerLevel = Math.Max(1, Math.Min(151, TrainerLevel));
            int before = TrainerLevel;
            TrainerExperience += Math.Max(0, amount);
            while (TrainerLevel < 151 && TrainerExperience >= TrainerExperienceRequired)
            {
                TrainerExperience -= TrainerExperienceRequired;
                TrainerLevel++;
            }
            if (TrainerLevel == 151) TrainerExperience = 0;
            return TrainerLevel - before;
        }
        public const string DefaultTrainerCharacter = "tr0036_Rei/Rei";
        public string TrainerCharacter = DefaultTrainerCharacter;
        public bool HasChosenTrainerCharacter;
        public int AvatarStyle;
        public int HairStyle;
        public int EyeColor;
        public int OutfitStyle;
        public int HatStyle;
        public int ScarfColor;
        public int BeardStyle;
        public int GlassesStyle;
        public int SkinColor;
        public int HairColor;
        public int CapColor;
        public int ShirtColor = 1;
        public int PantsColor = 1;

        public void CopyAppearanceFrom(SaveData source)
        {
            if (!HasCustomProfileName) TrainerName = source.TrainerName;
            TrainerCharacter = source.TrainerCharacter;
            HasChosenTrainerCharacter = source.HasChosenTrainerCharacter;
            AvatarStyle = source.AvatarStyle; HairStyle = source.HairStyle; EyeColor = source.EyeColor;
            OutfitStyle = source.OutfitStyle; HatStyle = source.HatStyle; ScarfColor = source.ScarfColor;
            BeardStyle = source.BeardStyle; GlassesStyle = source.GlassesStyle;
            SkinColor = source.SkinColor; HairColor = source.HairColor; CapColor = source.CapColor;
            ShirtColor = source.ShirtColor; PantsColor = source.PantsColor;
        }

        public bool HasStarter;
        public List<PokemonInstance> Team = new List<PokemonInstance>();
        public List<PokemonInstance> Box = new List<PokemonInstance>();

        public List<int> Seen = new List<int>();
        public List<int> Caught = new List<int>();
        public List<string> Medals = new List<string>();
        public List<string> DefeatedTrainers = new List<string>();
        public bool LeagueChampion;

        public int Money = 3000;
        public Dictionary<string, int> Items = new Dictionary<string, int>();

        public int DuelWins;
        public int MobaWins;

        /// <summary>Modo de pruebas: abre todas las zonas, arenas y modos.</summary>
        public bool UnlockAll;

        [JsonIgnore] public int CaughtCount => Caught.Count;
        public bool HasMedal(string medal) => Medals.Contains(medal);
        [JsonIgnore] public IEnumerable<PokemonInstance> AllPokemon => Team.Concat(Box);

        public void MarkSeen(int dex)
        {
            if (!Seen.Contains(dex)) Seen.Add(dex);
        }

        public void MarkCaught(int dex)
        {
            MarkSeen(dex);
            if (!Caught.Contains(dex)) Caught.Add(dex);
        }

        public int ItemCount(string key) => Items.TryGetValue(key, out var n) ? n : 0;

        public void AddItem(string key, int amount)
        {
            Items[key] = Math.Max(0, ItemCount(key) + amount);
        }

        public bool UseItem(string key)
        {
            if (ItemCount(key) <= 0) return false;
            Items[key]--;
            return true;
        }

        /// <summary>Al equipo si hay hueco; si no, a la caja del PC. Devuelve true si fue al equipo.</summary>
        public bool AddPokemon(PokemonInstance pokemon)
        {
            if (Team.Count < MaxTeam)
            {
                Team.Add(pokemon);
                return true;
            }
            Box.Add(pokemon);
            return false;
        }

        public void HealAll()
        {
            foreach (var p in AllPokemon) p.HpFraction = 1f;
        }

        public PokemonInstance FirstHealthy() => Team.FirstOrDefault(p => !p.Fainted);
    }
}
