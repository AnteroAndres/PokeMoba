using System;
using System.Collections;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Input;
using PokeMoba.Prototype;
using PokeMoba.World;
using UnityEngine;

namespace PokeMoba.Modes
{
    /// <summary>Dificultad de la IA (probabilidad de esquivar). Compartida por Práctica, Duelo y Aventura.</summary>
    public static class Difficulty
    {
        public static readonly (string Name, float DodgeChance)[] Levels =
        {
            ("Fácil", 0.2f), ("Normal", 0.55f), ("Difícil", 0.85f)
        };

        public static int Current = 1;
        public static float DodgeChance => Levels[Current].DodgeChance;
        public static string Label => $"Rival: {Levels[Current].Name}";
        public static void Next() => Current = (Current + 1) % Levels.Length;
    }

    /// <summary>Un combate 1v1 en una arena contra la IA (Práctica y Duelo).</summary>
    public class ArenaBattle
    {
        static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -6f);
        static readonly Vector3 EnemySpawn = new Vector3(0f, 0f, 6f);

        readonly GameContext ctx;
        bool over;

        public ArenaBattle(GameContext ctx) => this.ctx = ctx;

        /// <param name="onEnd">Se llama con true si gana el jugador.</param>
        /// <param name="onQuit">Botón «Salir» durante el combate.</param>
        public void Start(Biome biome, SpeciesData playerSpecies, SpeciesData rivalSpecies, int level,
            Action<bool> onEnd, Action onQuit)
        {
            over = false;
            var world = ctx.NewWorld("Duelo");
            EnvironmentBuilder.BuildArena(biome, world);

            var player = CreatureFactory.Create(playerSpecies, 0, PlayerSpawn, Vector3.forward, level,
                moves: PokemonDatabaseMoves(playerSpecies, level), parent: world);
            var enemy = CreatureFactory.Create(rivalSpecies, 1, EnemySpawn, Vector3.back, level,
                moves: PokemonDatabaseMoves(rivalSpecies, level), parent: world);

            player.gameObject.AddComponent<PlayerController>()
                .Bind(ctx.Hud.Joystick, ctx.Hud.AbilityButtons, ctx.Hud.DodgeButton, ctx.Camera);

            var ai = enemy.gameObject.AddComponent<EnemyAI>();
            ai.DodgeChance = Difficulty.DodgeChance;
            ai.SetTarget(player);

            player.Died += _ => Finish(false, onEnd);
            enemy.Died += _ => Finish(true, onEnd);

            ctx.Hud.gameObject.SetActive(true);
            ctx.Hud.Bind(player, enemy);
            ctx.Hud.AddButton("Salir", new Color(0.35f, 0.35f, 0.4f), new Vector2(1f, 1f), new Vector2(-30f, -160f),
                new Vector2(180f, 70f), onQuit);
            ctx.Follow.SetOffset(FollowCamera.DefaultOffset);
            ctx.Follow.SetTargets(player.transform, enemy.transform);
        }

        static MoveData[] PokemonDatabaseMoves(SpeciesData species, int level) =>
            Data.Pokedex.PokemonDatabase.MovesAt(species, level);

        void Finish(bool won, Action<bool> onEnd)
        {
            if (over) return;
            over = true;
            ctx.Runner.StartCoroutine(FinishAfter(won, onEnd));
        }

        IEnumerator FinishAfter(bool won, Action<bool> onEnd)
        {
            ctx.Hud.Toast(won ? "¡Victoria!" : "Derrota...");
            yield return new WaitForSeconds(1.5f);
            ctx.Hud.gameObject.SetActive(false);
            onEnd(won);
        }
    }
}
