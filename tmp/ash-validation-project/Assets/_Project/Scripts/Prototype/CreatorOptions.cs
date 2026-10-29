#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>Editor-only values. No creator menu or settings are compiled into player builds.</summary>
    [Serializable]
    public sealed class CreatorOptions
    {
        [Serializable] public class ItemAmount
        {
            public string Key;
            [Min(0)] public int Amount = 99;
        }
        [InspectorName("Aplicar recursos al iniciar Play")] public bool ApplyOnStart;
        [Header("Recursos de mi partida")]
        [InspectorName("Dinero"), Min(0)] public int Money = 99999;
        [InspectorName("Desbloquear zonas y modos")] public bool UnlockAll = true;
        [InspectorName("Poké Balls y objetos")] public List<ItemAmount> Inventory = new List<ItemAmount>();
        [Header("Pokémon del equipo")]
        [InspectorName("Puesto del equipo (1–6)"), Range(1,6)] public int TeamSlot = 1;
        [InspectorName("Nivel del Pokémon"), Range(1,100)] public int PokemonLevel = 50;
        [InspectorName("Nuevo Pokémon: número Pokédex"), Range(1,151)] public int PokemonDex = 25;
        [InspectorName("Nuevo Pokémon shiny")] public bool Shiny;
        [Header("Vida durante la prueba")]
        [InspectorName("Vida máxima del entrenador"), Min(1)] public int TrainerHealth = 1000;
        [InspectorName("Vida máxima del Pokémon"), Min(1)] public int PokemonHealth = 1000;
        [InspectorName("Vida actual (%)"), Range(1,100)] public int HealthPercent = 100;
        [InspectorName("Invulnerabilidad")] public bool Invulnerable;

        public void SyncItems()
        {
            if (Inventory == null) Inventory = new List<ItemAmount>();
            foreach (var ball in Balls.All) EnsureItem(ball.Key);
            EnsureItem(Items.Potion); EnsureItem(Items.SuperPotion);
        }
        void EnsureItem(string key)
        {
            if (!Inventory.Exists(i => i != null && i.Key == key)) Inventory.Add(new ItemAmount { Key = key });
        }
        public void ApplyResources(SaveData save)
        {
            SyncItems();
            save.Money = Mathf.Clamp(Money,0,1000000000);
            save.UnlockAll = UnlockAll;
            foreach(var item in Inventory)
                if(item != null && !string.IsNullOrWhiteSpace(item.Key)) save.Items[item.Key] = Mathf.Clamp(item.Amount,0,1000000000);
        }
        public PokemonInstance ApplyLevel(SaveData save)
        {
            int index = Mathf.Clamp(TeamSlot,1,6)-1;
            if(index >= save.Team.Count) throw new InvalidOperationException("No hay Pokémon en ese puesto del equipo.");
            var pokemon=save.Team[index]; var species=PokemonDatabase.Get(pokemon.Key);
            if(species==null) throw new InvalidOperationException("No se encontró la especie del Pokémon.");
            pokemon.Level=Mathf.Clamp(PokemonLevel,1,Leveling.MaxLevel);
            pokemon.Exp=Leveling.ExpForLevel(species.GrowthRate,pokemon.Level);
            return pokemon;
        }
        public PokemonInstance GivePokemon(SaveData save)
        {
            var species=PokemonDatabase.Get(Mathf.Clamp(PokemonDex,1,151));
            if(species==null) throw new InvalidOperationException("Esa especie no está disponible.");
            var pokemon=PokemonInstance.Create(species,Mathf.Clamp(PokemonLevel,1,100),Shiny,Balls.PokeBall.Key);
            save.AddPokemon(pokemon);save.MarkCaught(species.DexNumber);save.HasStarter=true;
            return pokemon;
        }
    }
}
#endif
