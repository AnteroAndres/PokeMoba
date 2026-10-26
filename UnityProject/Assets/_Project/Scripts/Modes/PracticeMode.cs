using System.Linq;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Modes
{
    /// <summary>Práctica: cualquiera de los 151 contra la IA, sin progreso. Sirve para probar y ajustar el combate.</summary>
    public class PracticeMode : GameMode
    {
        const float PickerTop = 200f;
        const float PickerBottom = 130f;

        SpeciesData playerSpecies;
        SpeciesData rivalChoice; // null = aleatorio

        public PracticeMode(GameContext ctx) : base(ctx) { }

        public override void Enter() => ShowPlayerPicker();

        void ShowPlayerPicker()
        {
            Ctx.ClearWorld();
            var panel = Ctx.Menus.Open("Práctica: elige tu Pokémon");
            SpeciesPicker.Build(panel, PokemonDatabase.All, ShowRivalPicker, PickerTop, PickerBottom);

            MenuScreens.Button(panel, "Menú principal", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-240f, 25f),
                new Vector2(420f, 80f), () => Ctx.ReturnToMainMenu());
            Button difficulty = null;
            difficulty = MenuScreens.Button(panel, Difficulty.Label, new Color(0.25f, 0.25f, 0.3f), new Vector2(0.5f, 0f),
                new Vector2(240f, 25f), new Vector2(420f, 80f), () =>
                {
                    Difficulty.Next();
                    difficulty.GetComponentInChildren<Text>().text = Difficulty.Label;
                });
        }

        void ShowRivalPicker(SpeciesData species)
        {
            var panel = Ctx.Menus.Open($"Elige el rival de {species.Name}");
            SpeciesPicker.Build(panel, PokemonDatabase.All, rival => StartBattle(species, rival), PickerTop, PickerBottom);

            MenuScreens.Button(panel, "Volver", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-240f, 25f),
                new Vector2(420f, 80f), ShowPlayerPicker);
            MenuScreens.Button(panel, "Rival aleatorio", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0f), new Vector2(240f, 25f),
                new Vector2(420f, 80f), () => StartBattle(species, null));
        }

        void StartBattle(SpeciesData species, SpeciesData rival)
        {
            playerSpecies = species;
            rivalChoice = rival;
            var opponent = rival ?? RandomOther(species);
            new ArenaBattle(Ctx).Start(Biome.Estadio, species, opponent, PokemonDatabase.MoveLevel, ShowResult, ShowPlayerPicker);
        }

        static SpeciesData RandomOther(SpeciesData species)
        {
            var others = PokemonDatabase.All.Where(s => s != species).ToArray();
            return others.Length > 0 ? others[Random.Range(0, others.Length)] : species;
        }

        void ShowResult(bool won)
        {
            var panel = Ctx.Menus.Open(won ? "¡Ganaste!" : "Perdiste...");
            MenuScreens.Button(panel, "Revancha", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(-440f, -40f),
                new Vector2(400f, 120f), () => StartBattle(playerSpecies, rivalChoice), 40);
            MenuScreens.Button(panel, "Elegir otros", new Color(0.3f, 0.3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f),
                new Vector2(400f, 120f), ShowPlayerPicker, 40);
            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(440f, -40f),
                new Vector2(400f, 120f), () => Ctx.ReturnToMainMenu(), 40);
        }
    }
}
