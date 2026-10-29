using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PokeMoba.Game
{
    /// <summary>Carga y guarda la partida (un único archivo por dispositivo).</summary>
    public static class SaveService
    {
        static SaveData current;

        static string FilePath => Path.Combine(Application.persistentDataPath, "pokemoba_save.json");

        public static SaveData Current
        {
            get
            {
                if (current == null) Load();
                return current;
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(Current, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.LogError($"PokeMoba: no se pudo guardar la partida: {e.Message}");
            }
        }

        public static void Delete()
        {
            bool unlockAll = Current.UnlockAll;
            if (File.Exists(FilePath)) File.Delete(FilePath);
            current = NewGame();
            current.UnlockAll = unlockAll;
            Save();
        }

        static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    current = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(FilePath));
                    if (current != null)
                    {
                        if (string.IsNullOrEmpty(current.TrainerCharacter) ||
                            (current.TrainerCharacter == "Procedural" && !current.HasChosenTrainerCharacter))
                        {
                            current.TrainerCharacter = SaveData.DefaultTrainerCharacter;
                            Save();
                        }
                        if (!current.HasCustomProfileName && current.TrainerName != current.SelectedCharacterName)
                        {
                            current.TrainerName = current.SelectedCharacterName;
                            Save();
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PokeMoba: partida dañada, se empieza una nueva ({e.Message}).");
            }
            current = NewGame();
        }

        static SaveData NewGame()
        {
            var save = new SaveData();
            save.AddItem(Data.Balls.PokeBall.Key, 10);
            save.AddItem(Items.Potion, 3);
            return save;
        }
    }

    public static class Items
    {
        public const string Potion = "potion";
        public const string SuperPotion = "super-potion";

        public static string Name(string key)
        {
            switch (key)
            {
                case Potion: return "Poción";
                case SuperPotion: return "Superpoción";
                default: return Data.Balls.Get(key).Name;
            }
        }
    }
}
