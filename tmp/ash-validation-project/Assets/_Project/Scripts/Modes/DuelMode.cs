using System.Collections.Generic;
using System.Linq;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Modes
{
    /// <summary>
    /// Duelo 1v1 (GDD §7.2): eliges arena y uno de tus Pokémon capturados; todos pelean a nivel 50.
    /// El rival es aleatorio entre los 151.
    /// </summary>
    public class DuelMode : GameMode
    {
        ArenaDef arena;
        SpeciesData playerSpecies;

        public DuelMode(GameContext ctx) : base(ctx) { }

        public override void Enter() => ShowArenas();

        void ShowArenas()
        {
            Ctx.ClearWorld();
            var panel = Ctx.Menus.Open("Duelo 1v1", $"Victorias: {Save.DuelWins} · Todos pelean a nivel 50");

            var arenas = ZoneCatalog.Arenas;
            for (int i = 0; i < arenas.Length; i++)
            {
                var a = arenas[i];
                bool unlocked = Progression.IsArenaUnlocked(a, Save);
                int column = i % 5, row = i / 5;
                string label = unlocked ? a.Name : $"{a.Name}\n<size=22>Abre la zona {a.RequiredZone}</size>";
                MenuScreens.Button(panel, label, unlocked ? new Color(0.25f, 0.45f, 0.75f) : new Color(0.3f, 0.3f, 0.3f),
                    new Vector2(0.5f, 0.5f), new Vector2((column - 2) * 340f, 140f - row * 200f), new Vector2(320f, 170f),
                    () => ShowPokemon(a), 32, unlocked);
            }

            MenuScreens.Button(panel, "Menú principal", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-240f, 30f),
                new Vector2(420f, 80f), () => Ctx.ReturnToMainMenu());
            Button difficulty = null;
            difficulty = MenuScreens.Button(panel, Difficulty.Label, new Color(0.25f, 0.25f, 0.3f), new Vector2(0.5f, 0f),
                new Vector2(240f, 30f), new Vector2(420f, 80f), () =>
                {
                    Difficulty.Next();
                    difficulty.GetComponentInChildren<Text>().text = Difficulty.Label;
                });
        }

        void ShowPokemon(ArenaDef chosenArena)
        {
            arena = chosenArena;
            var owned = OwnedSpecies();
            var panel = Ctx.Menus.Open($"Arena «{arena.Name}»: elige tu Pokémon",
                Save.UnlockAll ? "Modo pruebas: todos disponibles" : "Solo puedes usar Pokémon que hayas capturado");

            if (owned.Count == 0)
            {
                MenuScreens.Label(panel, "Todavía no tienes Pokémon.\nEmpieza la Aventura para elegir tu inicial y capturar más.",
                    36, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 200f));
            }
            else
            {
                SpeciesPicker.Build(panel, owned, StartBattle, 200f, 130f);
            }

            MenuScreens.Button(panel, "Volver", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 25f),
                new Vector2(420f, 80f), ShowArenas);
        }

        List<SpeciesData> OwnedSpecies()
        {
            if (Save.UnlockAll) return PokemonDatabase.All.ToList();
            var keys = new HashSet<string>(Save.AllPokemon.Select(p => p.Key));
            return PokemonDatabase.All.Where(s => keys.Contains(s.Key)).ToList();
        }

        void StartBattle(SpeciesData species)
        {
            playerSpecies = species;
            var others = PokemonDatabase.All.Where(s => s != species).ToArray();
            var rival = others[Random.Range(0, others.Length)];
            new ArenaBattle(Ctx).Start(arena.Biome, species, rival, PokemonDatabase.MoveLevel, ShowResult, ShowArenas);
        }

        void ShowResult(bool won)
        {
            if (won)
            {
                Save.DuelWins++;
                Save.Money += 300;
                SaveService.Save();
            }

            var panel = Ctx.Menus.Open(won ? "¡Ganaste el duelo!" : "Perdiste el duelo",
                won ? $"+300 ₽ · Victorias: {Save.DuelWins}" : $"Victorias: {Save.DuelWins}");
            MenuScreens.Button(panel, "Otro rival", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(-440f, -40f),
                new Vector2(400f, 120f), () => StartBattle(playerSpecies), 40);
            MenuScreens.Button(panel, "Otra arena", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f),
                new Vector2(400f, 120f), ShowArenas, 40);
            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(440f, -40f),
                new Vector2(400f, 120f), () => Ctx.ReturnToMainMenu(), 40);
        }
    }
}
