using System;
using System.Collections;
using System.Linq;
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
    /// El equipo del jugador en la Aventura (exploración y combates contra entrenadores):
    /// saca al primer Pokémon sano, cambia de Pokémon, usa pociones, reparte experiencia,
    /// sube de nivel y evoluciona. La vida se guarda en cada PokemonInstance.
    /// </summary>
    public class PartyFighter
    {
        const float SwitchCooldown = 3f;

        readonly GameContext ctx;
        readonly Transform world;
        readonly TrainerFollower trainer;
        float nextSwitchTime;

        public Combatant Current { get; private set; }
        public PokemonInstance Instance { get; private set; }

        /// <summary>Cada vez que sale un Pokémon nuevo (para que los rivales lo apunten).</summary>
        public event Action<Combatant> Spawned;
        /// <summary>Todo el equipo debilitado.</summary>
        public event Action AllFainted;

        SaveData Save => ctx.Save;

        public PartyFighter(GameContext ctx, Transform world, bool withTrainer)
        {
            this.ctx = ctx;
            this.world = world;
            if (withTrainer)
            {
                var s = Save;
                var model = TrainerModelBuilder.Build(world,
                    TrainerPalette.Pick(TrainerPalette.Skin, s.SkinColor), TrainerPalette.Pick(TrainerPalette.Hair, s.HairColor),
                    TrainerPalette.Pick(TrainerPalette.Clothes, s.CapColor), TrainerPalette.Pick(TrainerPalette.Clothes, s.ShirtColor),
                    TrainerPalette.Pick(TrainerPalette.Clothes, s.PantsColor));
                trainer = model.GetComponent<TrainerFollower>();
            }
        }

        public bool SpawnFirstHealthy(Vector3 position, Vector3 facing)
        {
            var first = Save.FirstHealthy();
            if (first == null) return false;
            Spawn(first, position, facing);
            return true;
        }

        void Spawn(PokemonInstance instance, Vector3 position, Vector3 facing)
        {
            var species = PokemonDatabase.Get(instance.Key);
            Instance = instance;
            Current = CreatureFactory.Create(species, 0, position, facing, instance.Level, instance.Shiny,
                PokemonDatabase.MovesAt(species, instance.Level), world);
            Current.SetHpFraction(instance.HpFraction);
            Current.gameObject.AddComponent<PlayerController>()
                .Bind(ctx.Hud.Joystick, ctx.Hud.AbilityButtons, ctx.Hud.DodgeButton, ctx.Camera);
            Current.Died += OnFainted;

            ctx.Hud.Bind(Current, null);
            ctx.Follow.SetTargets(Current.transform, null);
            if (trainer != null) trainer.Follow(Current.transform);
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
            var next = Save.FirstHealthy();
            if (next != null) Replace(next);
            else AllFainted?.Invoke();
        }

        /// <summary>Saca al siguiente Pokémon sano del equipo (botón «Cambiar»).</summary>
        public void SwitchNext()
        {
            if (Current == null || Current.IsDead) return;
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
            Vector3 position = Current != null ? Current.transform.position : Vector3.zero;
            Vector3 facing = Current != null ? Current.transform.forward : Vector3.forward;
            if (Current != null) UnityEngine.Object.Destroy(Current.gameObject);
            Spawn(next, position, facing);
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

        /// <summary>Experiencia por debilitar o capturar; sube de nivel y evoluciona si toca.</summary>
        public void AwardExp(SpeciesData defeated, int defeatedLevel, bool fromTrainer)
        {
            if (Instance == null || Current == null) return;
            var species = PokemonDatabase.Get(Instance.Key);
            int exp = Leveling.ExpYield(defeated.BaseExperience, defeatedLevel, fromTrainer);
            int gained = Instance.AddExp(species, exp);
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
    }
}
