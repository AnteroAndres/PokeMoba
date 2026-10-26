using System;
using System.Collections;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Prototype;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Modes
{
    /// <summary>
    /// Combate contra un entrenador (líder de gimnasio o Alto Mando): sus Pokémon salen de uno en uno
    /// y tú peleas con tu equipo, cambiando cuando quieras (GDD §6, combates de gimnasio).
    /// </summary>
    public class TrainerBattle
    {
        static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -6f);
        static readonly Vector3 EnemySpawn = new Vector3(0f, 0f, 6f);

        readonly GameContext ctx;
        readonly TrainerDef trainer;
        readonly Action onWin;
        readonly Action onLose;

        Transform world;
        PartyFighter party;
        Combatant enemy;
        EnemyAI enemyAi;
        int enemyIndex;
        bool over;
        Button potionButton;

        SaveData Save => ctx.Save;

        public TrainerBattle(GameContext ctx, TrainerDef trainer, Action onWin, Action onLose)
        {
            this.ctx = ctx;
            this.trainer = trainer;
            this.onWin = onWin;
            this.onLose = onLose;
        }

        public void Start()
        {
            world = ctx.NewWorld($"Combate contra {trainer.Name}");
            EnvironmentBuilder.BuildArena(trainer.Arena, world);

            party = new PartyFighter(ctx, world, withTrainer: false);
            party.AllFainted += () => Finish(false);
            party.Spawned += player =>
            {
                if (enemyAi != null) enemyAi.SetTarget(player);
                if (enemy != null) ctx.Hud.SetEnemy(enemy);
                ctx.Follow.SetTargets(player.transform, enemy != null ? enemy.transform : null);
            };

            ctx.Hud.gameObject.SetActive(true);
            ctx.Follow.SetOffset(FollowCamera.DefaultOffset);
            if (!party.SpawnFirstHealthy(PlayerSpawn, Vector3.forward))
            {
                Save.HealAll();
                party.SpawnFirstHealthy(PlayerSpawn, Vector3.forward);
            }

            var hud = ctx.Hud;
            potionButton = hud.AddButton("", new Color(0.3f, 0.65f, 0.4f), new Vector2(0f, 1f), new Vector2(30f, -190f), new Vector2(240f, 70f),
                () => { party.UsePotion(); RefreshPotion(); }, 22);
            hud.AddButton("Cambiar Pokémon", new Color(0.3f, 0.45f, 0.7f), new Vector2(0f, 1f), new Vector2(290f, -190f), new Vector2(260f, 70f),
                party.SwitchNext, 22);
            hud.AddButton("Rendirse", new Color(0.35f, 0.35f, 0.4f), new Vector2(1f, 1f), new Vector2(-30f, -160f), new Vector2(200f, 70f),
                () => Finish(false), 22);
            RefreshPotion();

            hud.Toast($"{trainer.Title} {trainer.Name}\n<size=36>¡Te desafía!</size>", 2.5f);
            SpawnEnemy(0);
        }

        void RefreshPotion() => potionButton.GetComponentInChildren<Text>().text = $"Poción ×{party.PotionCount}";

        void SpawnEnemy(int index)
        {
            enemyIndex = index;
            var (key, level) = trainer.Team[index];
            var species = PokemonDatabase.Get(key);
            enemy = CreatureFactory.Create(species, 1, EnemySpawn, Vector3.back, level, false, PokemonDatabase.MovesAt(species, level), world);
            enemyAi = enemy.gameObject.AddComponent<EnemyAI>();
            enemyAi.DodgeChance = Mathf.Max(Difficulty.DodgeChance, 0.5f);
            enemyAi.SetTarget(party.Current);
            Save.MarkSeen(species.DexNumber);

            var defeated = enemy;
            enemy.Died += _ => OnEnemyDefeated(species, level, defeated);

            ctx.Hud.SetEnemy(enemy);
            ctx.Hud.SetStatus($"{trainer.Title} {trainer.Name}\n<size=24>Pokémon {index + 1} de {trainer.Team.Length}</size>");
            if (party.Current != null) ctx.Follow.SetTargets(party.Current.transform, enemy.transform);
        }

        void OnEnemyDefeated(Data.SpeciesData species, int level, Combatant defeated)
        {
            if (over) return;
            party.AwardExp(species, level, fromTrainer: true);
            ctx.Runner.StartCoroutine(NextEnemy(defeated));
        }

        IEnumerator NextEnemy(Combatant defeated)
        {
            yield return new WaitForSeconds(1.5f);
            if (defeated != null) UnityEngine.Object.Destroy(defeated.gameObject);
            if (enemyIndex + 1 < trainer.Team.Length) SpawnEnemy(enemyIndex + 1);
            else Finish(true);
        }

        void Finish(bool won)
        {
            if (over) return;
            over = true;
            party.SyncHp();

            if (won)
            {
                Save.Money += trainer.Reward;
                if (!Save.DefeatedTrainers.Contains(trainer.Name)) Save.DefeatedTrainers.Add(trainer.Name);
                if (trainer.Medal != null && !Save.HasMedal(trainer.Medal)) Save.Medals.Add(trainer.Medal);
                if (trainer.Medal == "Pantano") Save.AddItem(Data.Balls.MasterBall.Key, 1);
                string medal = trainer.Medal != null ? $"\n<size=36>¡Medalla {trainer.Medal}!</size>" : "";
                ctx.Hud.Toast($"¡Has vencido a {trainer.Name}!{medal}", 3f);
            }
            else
            {
                int lost = Save.Money / 10;
                Save.Money -= lost;
                Save.HealAll();
                ctx.Hud.Toast($"Has perdido contra {trainer.Name}...\n<size=34>-{lost} ₽</size>", 3f);
            }
            SaveService.Save();
            ctx.Runner.StartCoroutine(After(won));
        }

        IEnumerator After(bool won)
        {
            yield return new WaitForSeconds(2.5f);
            if (won) onWin();
            else onLose();
        }
    }
}
