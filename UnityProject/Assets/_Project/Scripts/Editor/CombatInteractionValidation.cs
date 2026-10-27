using System;
using System.IO;
using PokeMoba.Data;
using PokeMoba.Input;
using PokeMoba.AI;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    public static class CombatInteractionValidation
    {
        [MenuItem("PokeMoba/Validar XP y observar controles")]
        public static void Run()
        {
            Check("medium", 5, 148, 23, 91, 68);
            Check("fast", 5, 100, 0, 72, 72);
            Check("slow", 5, 156, 0, 114, 114);
            Check("medium-slow", 5, 135, 0, 44, 44);
            var species = new SpeciesData { Key = "validation", GrowthRate = "medium" };
            var instance = PokemonInstance.Create(species, 5, false, "poke-ball");
            if (instance.AddExp(species, 100) != 1 || instance.Level != 6) throw new Exception("XP level-up failed");
            Check("medium", instance.Level, instance.Exp, 9, 127, 118);
            if (!Leveling.Progress("medium", 100, 1000000).AtCap) throw new Exception("XP level cap failed");
            Directory.CreateDirectory("../tmp/combat-controls");
            File.WriteAllText("../tmp/combat-controls/xp-validation.txt", "PASS: growth curves, exact remaining XP, level-up rollover and maximum level.");
            EditorApplication.update -= Observe;
            EditorApplication.update += Observe;
            Debug.Log("XP validation passed. Observing jump/dodge routing while Play mode is active.");
        }
        static void Check(string growth, int level, int exp, int current, int required, int remaining)
        {
            var p = Leveling.Progress(growth, level, exp);
            if (p.Current != current || p.Required != required || p.Remaining != remaining)
                throw new Exception("Incorrect XP progress for " + growth);
        }
        static string last;
        static void Observe()
        {
            if (!Application.isPlaying) { EditorApplication.update -= Observe; return; }
            var controller = UnityEngine.Object.FindFirstObjectByType<TrainerController>();
            if (controller == null) return;
            var trainer = controller.GetComponent<PokeMoba.Combat.CreatureMotor>();
            var companion = UnityEngine.Object.FindFirstObjectByType<CompanionAI>();
            var pokemon = companion != null ? companion.GetComponent<PokeMoba.Combat.CreatureMotor>() : null;
            string state = $"combat={controller.IsInCombat} trainerJump={trainer.IsJumping} trainerDodge={trainer.IsDodging} pokemonDodge={(pokemon != null && pokemon.IsDodging)}";
            if (state == last) return;
            last = state;
            File.AppendAllText("../tmp/combat-controls/routing.txt", DateTime.Now.ToString("HH:mm:ss") + " " + state + "\n");
        }
    }
}
