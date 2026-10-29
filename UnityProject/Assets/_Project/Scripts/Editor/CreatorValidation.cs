#if UNITY_EDITOR
using System;
using System.IO;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Prototype;
using UnityEditor;
using UnityEngine;
namespace PokeMoba.Editor
{
    public static class CreatorValidation
    {
        [MenuItem("PokeMoba/Validar panel del creador")]
        public static void Run()
        {
            var save = new SaveData();
            var options = new CreatorOptions(); options.SyncItems();
            Check(options.Inventory.Count == Balls.All.Length + 2,"Catálogo completo de objetos");
            options.Inventory[0].Amount = -10; options.Money = -20; options.ApplyResources(save);
            Check(save.Money == 0 && save.Items[Balls.PokeBall.Key] == 0,"Cantidades negativas");
            options.PokemonLevel = 120;
            var pokemon = options.GivePokemon(save);
            Check(pokemon.Level == 100 && save.Caught.Contains(25),"Añadir Pokémon");
            options.PokemonLevel = 8; options.ApplyLevel(save);
            var species = PokemonDatabase.Get(pokemon.Key);
            Check(pokemon.Level == 8 && pokemon.Exp == Leveling.ExpForLevel(species.GrowthRate,8),"Nivel y experiencia");
            for(int i=0;i<6;i++) options.GivePokemon(save);
            Check(save.Team.Count == 6 && save.Box.Count == 1,"Equipo lleno envía al PC");
            var actorObject = new GameObject("Creator validation");
            try
            {
                var actor = actorObject.AddComponent<Combatant>(); actor.Init(species,0,8);
                actor.SetCreatorVitals(1000,.5f,true);
                Check(actor.MaxHp == 1000 && actor.Hp == 500 && actor.DamageImmune,"Vida e invulnerabilidad");
                actor.Reinitialize(species,9,PokemonDatabase.MovesAt(species,9),.5f);
                actor.SetCreatorVitals(1000,.5f,false);
                Check(actor.MaxHp == 1000 && !actor.DamageImmune,"Cambiar nivel conserva configuración");
            }
            finally { UnityEngine.Object.DestroyImmediate(actorObject); }
            string report = "PASS: inventario, límites, nivel/XP, equipo/PC y vida/invulnerabilidad. Partida real intacta.";
            Directory.CreateDirectory("../creator-validation"); File.WriteAllText("../creator-validation/result.txt",report);
            Debug.Log(report);
        }
        static void Check(bool condition,string label) { if(!condition) throw new Exception(label); }
    }
}
#endif
