using System;
using System.Collections;
using System.Linq;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Input;
using PokeMoba.Prototype;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;

namespace PokeMoba.Modes
{
    /// <summary>
    /// El jugador en la Aventura (estilo Legends: Z-A): un entrenador que se controla con el joystick y puede recibir
    /// daño, y su Pokémon compañero, que le sigue y obedece órdenes. Gestiona cambiar de Pokémon, pociones,
    /// experiencia, subida de nivel y evolución. La vida de cada Pokémon se guarda en su PokemonInstance.
    /// </summary>
    public class PartyFighter
    {
        const float SwitchCooldown = 3f;
        const float TrainerRegenPerSecond = 0.03f;

        readonly GameContext ctx;
        readonly Transform world;
        float nextSwitchTime;

        /// <summary>El entrenador (también es un combatiente: los salvajes pueden atacarle).</summary>
        public Combatant Trainer { get; }
        public TrainerController Controller { get; }
        /// <summary>Pokémon compañero que está fuera.</summary>
        public Combatant Current { get; private set; }
        public PokemonInstance Instance { get; private set; }

        /// <summary>Cada vez que sale un Pokémon nuevo (para que los rivales lo apunten).</summary>
        public event Action<Combatant> Spawned;
        /// <summary>Todo el equipo debilitado o el entrenador desmayado.</summary>
        public event Action AllFainted;

        SaveData Save => ctx.Save;

        public PartyFighter(GameContext ctx, Transform world, Vector3 position, Vector3 facing)
        {
            this.ctx = ctx;
            this.world = world;

            Trainer = CreateTrainer(position, facing);
            Controller = Trainer.gameObject.AddComponent<TrainerController>();
            Controller.EnsureCompanion = () => Current != null && !Current.IsDead || SpawnFirstHealthy();
            Controller.Bind(ctx.Hud.Joystick, ctx.Hud.AbilityButtons, ctx.Hud.DodgeButton, ctx.Camera);
            Controller.ContextChanged = ctx.Hud.SetCombatContext;
            ctx.Hud.SetCombatContext(false);
            Controller.TargetChanged += target =>
            {
                ctx.Hud.SetEnemy(target);
                ctx.Follow.SetTargets(Trainer.transform, target != null ? target.transform : null);
            };
            Trainer.Died += _ => OnTrainerFainted();

            ctx.Hud.SetTrainer(Trainer);
            ctx.Follow.SetTargets(Trainer.transform, null);
            ctx.Runner.StartCoroutine(RegenerateTrainer());
        }

        Combatant CreateTrainer(Vector3 position, Vector3 facing)
        {
            var s = Save;
            var root = new GameObject("Entrenador (jugador)");
            root.transform.SetParent(world, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing);

            var model = TrainerModelBuilder.Build(root.transform, s, follower: false);
            model.name = "Model"; // CreatureMotor y Combatant buscan el modelo por este nombre

            var trainer = root.AddComponent<Combatant>();
            int level = Mathf.Max(10, s.Team.Count > 0 ? s.Team.Max(p => p.Level) : 10);
            trainer.Init(TrainerSpecies(s.TrainerName), team: 0, level: level);
            return trainer;
        }

        /// <summary>"Especie" del entrenador: solo sirve para la vida, la defensa y la velocidad.</summary>
        static SpeciesData TrainerSpecies(string name)
        {
            var noMove = MoveFactory.Fallback(basic: true);
            return new SpeciesData
            {
                DexNumber = 0, Key = "trainer", Name = name,
                Types = new[] { PokemonType.Normal },
                BaseStats = new BaseStats(80, 10, 70, 10, 70, 75),
                Moves = new[] { noMove, noMove, noMove, noMove },
                Size = 1f
            };
        }

        IEnumerator RegenerateTrainer()
        {
            while (Trainer != null)
            {
                if (!Trainer.IsDead) Trainer.Heal(Trainer.MaxHp * TrainerRegenPerSecond * 0.5f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ------------------------------------------------------------------ Pokémon compañero

        public void Recall()
        {
            if (Current == null) return;
            SyncHp();
            Controller.SetLock(null);
            Controller.SetCompanion(null);
            Current.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(Current.gameObject);
            Current = null;
            ctx.Hud.Bind(null, null);
        }

        public bool SpawnFirstHealthy()
        {
            if (Current != null && !Current.IsDead) return true;
            var first = Save.FirstHealthy();
            if (first == null) return false;
            Spawn(first);
            return true;
        }

        void Spawn(PokemonInstance instance)
        {
            var species = PokemonDatabase.Get(instance.Key);
            Instance = instance;
            Vector3 position = Trainer.transform.position + Trainer.transform.right * 1.5f - Trainer.transform.forward * 0.5f;
            Current = CreatureFactory.Create(species, 0, position, Trainer.transform.forward, instance.Level, instance.Shiny,
                PokemonDatabase.MovesAt(species, instance.Level), world);
            Current.SetHpFraction(instance.HpFraction);
            Current.OwnedInstance = instance;
            var teamRing = Current.transform.Find("Anillo de equipo");
            if (teamRing != null) teamRing.gameObject.SetActive(false);
            Current.gameObject.AddComponent<CompanionAI>().Setup(Trainer.transform);
            Current.Died += OnFainted;

            Controller.SetCompanion(Current);
            ctx.Hud.Bind(Current, Controller.LockedTarget);
            ctx.Hud.ExperienceSource = () => Leveling.Progress(Current.Species.GrowthRate, Instance.Level, Instance.Exp);
            Vfx.Burst(position + Vector3.up, Color.white, 1.8f);
            Spawned?.Invoke(Current);
        }

        /// <summary>Guarda la vida actual en la partida.</summary>
        public void SyncHp()
        {
            if (Current == null || Instance == null) return;
            Instance.HpFraction = Current.IsDead ? 0f : Current.Hp / Current.MaxHp;
        }

        void OnFainted(Combatant fainted)
        {
            SyncHp();
            ctx.Hud.Toast($"¡{fainted.DisplayName} se debilitó!");
            ctx.Runner.StartCoroutine(AfterFaint());
        }

        IEnumerator AfterFaint()
        {
            yield return new WaitForSeconds(1.3f);
            if (Trainer.IsDead) yield break;
            var next = Save.FirstHealthy();
            if (next != null) Replace(next);
            else AllFainted?.Invoke();
        }

        void OnTrainerFainted()
        {
            SyncHp();
            ctx.Hud.Toast($"¡{Save.TrainerName} se ha desmayado!", 2.5f);
            ctx.Runner.StartCoroutine(AfterTrainerFaint());
        }

        IEnumerator AfterTrainerFaint()
        {
            yield return new WaitForSeconds(1.5f);
            AllFainted?.Invoke();
        }

        /// <summary>Saca al siguiente Pokémon sano del equipo (botón «Cambiar»).</summary>
        public void SwitchNext()
        {
            if (Current == null || Trainer.IsDead) return;
            if (Time.time < nextSwitchTime)
            {
                ctx.Hud.Toast("Espera un momento para volver a cambiar", 1.2f);
                return;
            }

            SyncHp();
            int index = Save.Team.IndexOf(Instance);
            for (int step = 1; step < Save.Team.Count; step++)
            {
                var candidate = Save.Team[(index + step) % Save.Team.Count];
                if (candidate.Fainted) continue;
                Replace(candidate);
                nextSwitchTime = Time.time + SwitchCooldown;
                return;
            }
            ctx.Hud.Toast("No tienes otro Pokémon en condiciones", 1.5f);
        }

        void Replace(PokemonInstance next)
        {
            if (Current != null) UnityEngine.Object.Destroy(Current.gameObject);
            Spawn(next);
            ctx.Hud.Toast($"¡Adelante, {Current.DisplayName}!", 1.5f);
        }

        /// <summary>Usa la mejor poción disponible con el Pokémon que está fuera.</summary>
        public void UsePotion()
        {
            if (Current == null || Current.IsDead) return;
            if (Current.Hp >= Current.MaxHp)
            {
                ctx.Hud.Toast("Ya tiene la vida completa", 1.2f);
                return;
            }

            float heal;
            if (Save.UseItem(Items.SuperPotion)) heal = 0.7f;
            else if (Save.UseItem(Items.Potion)) heal = 0.35f;
            else
            {
                ctx.Hud.Toast("No te quedan pociones", 1.2f);
                return;
            }

            Current.Heal(Current.MaxHp * heal);
            SyncHp();
            Vfx.Burst(Current.transform.position + Vector3.up, new Color(0.5f, 1f, 0.6f), 2f);
        }

        public int PotionCount => Save.ItemCount(Items.Potion) + Save.ItemCount(Items.SuperPotion);

        // ------------------------------------------------------------------ experiencia y evolución

        /// <summary>Experiencia por debilitar o capturar; sube de nivel y evoluciona si toca.</summary>
        public void AwardExp(SpeciesData defeated, int defeatedLevel, bool fromTrainer)
        {
            if (Instance == null || Current == null) return;
            var species = PokemonDatabase.Get(Instance.Key);
            int exp = Leveling.ExpYield(defeated.BaseExperience, defeatedLevel, fromTrainer);
            int trainerLevels = Save.AwardTrainerExperience(exp);
            if (trainerLevels > 0)
                ctx.Hud.Toast($"¡Entrenador nivel {Save.TrainerLevel}! Nuevo diseño de perfil desbloqueado.", 3f);
            int gained = Instance.AddExp(species, exp);
            SaveService.Save();
            if (gained <= 0) return;

            ctx.Hud.Toast($"¡{Current.DisplayName} subió al nivel {Instance.Level}!", 2f);
            if (!Current.IsDead)
            {
                Current.Reinitialize(species, Instance.Level, PokemonDatabase.MovesAt(species, Instance.Level),
                    Current.Hp / Current.MaxHp);
                ctx.Hud.RefreshPlayer();
            }

            var options = EvolutionRules.Options(species, Instance.Level, PokemonDatabase.Get);
            if (options.Count == 1) Evolve(options[0]);
            else if (options.Count > 1) AskEvolution(species, options);
        }

        void AskEvolution(SpeciesData species, System.Collections.Generic.List<SpeciesData> options)
        {
            Time.timeScale = 0f;
            var panel = ctx.Menus.Open($"¡{species.Name} quiere evolucionar!", "Elige su evolución", 0.6f);
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                MenuScreens.Button(panel, $"{option.Name}\n<size=24>{string.Join(" / ", option.Types.Select(t => t.DisplayName()))}</size>",
                    option.Types[0].TypeColor(), new Vector2(0.5f, 0.5f), new Vector2((i - (options.Count - 1) / 2f) * 380f, 40f),
                    new Vector2(350f, 180f), () =>
                    {
                        Time.timeScale = 1f;
                        ctx.Menus.Close();
                        Evolve(option);
                    });
            }
            MenuScreens.Button(panel, "Ahora no", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f),
                new Vector2(350f, 90f), () =>
                {
                    Time.timeScale = 1f;
                    ctx.Menus.Close();
                });
        }

        void Evolve(SpeciesData target)
        {
            string before = Current.DisplayName;
            Instance.Key = target.Key;
            Save.MarkCaught(target.DexNumber);
            if (!Current.IsDead)
            {
                CreatureFactory.Rebuild(Current, target, Instance.Level, PokemonDatabase.MovesAt(target, Instance.Level));
                ctx.Hud.RefreshPlayer();
            }
            ctx.Hud.Toast($"¡{before} evolucionó a {target.Name}!", 3f);
            SaveService.Save();
        }

        /// <summary>
        /// A quién atacan los rivales: normalmente a tu Pokémon; si no está (cambiando o debilitado), al entrenador.
        /// </summary>
        public Combatant TargetForEnemies() =>
            Current != null && !Current.IsDead ? Current : Trainer;
    }
}
