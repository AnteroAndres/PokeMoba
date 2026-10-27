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

            ctx.Hud.gameObject.SetActive(true);
            ctx.Follow.SetOffset(FollowCamera.DefaultOffset);

            // El entrenador se queda detrás; su Pokémon sale delante.
            party = new PartyFighter(ctx, world, PlayerSpawn + Vector3.back * 3f, Vector3.forward);
            party.Controller.BattleActive = true;
            party.AllFainted += () => Finish(false);
            party.Spawned += _ =>
            {
                if (enemyAi != null) enemyAi.SetTarget(party.TargetForEnemies());
                if (enemy != null) party.Controller.SetLock(enemy);
            };

            if (!party.SpawnFirstHealthy())
            {
                Save.HealAll();
                party.SpawnFirstHealthy();
            }

            var hud = ctx.Hud;
            hud.AddButton("Fijar", new Color(0.85f, 0.65f, 0.15f), new Vector2(1f, 0f), new Vector2(-40f, 495f), new Vector2(140f, 70f),
                party.Controller.CycleLock, 24);
            potionButton = hud.AddButton("", new Color(0.3f, 0.65f, 0.4f), new Vector2(0f, 1f), new Vector2(30f, -240f), new Vector2(240f, 70f),
                () => { party.UsePotion(); RefreshPotion(); }, 22);
            hud.AddButton("Cambiar Pokémon", new Color(0.3f, 0.45f, 0.7f), new Vector2(0f, 1f), new Vector2(290f, -240f), new Vector2(260f, 70f),
                party.SwitchNext, 22);
            hud.AddButton("Rendirse", new Color(0.35f, 0.35f, 0.4f), new Vector2(1f, 1f), new Vector2(-30f, -160f), new Vector2(200f, 70f),
                () => Finish(false), 22);
            RefreshPotion();

            hud.Toast($"{trainer.Title} {trainer.Name}\n<size=36>¡Te desafía!</size>", 2.5f);
            SpawnEnemy(0);
            ctx.Runner.StartCoroutine(RetargetEnemy());
        }

        /// <summary>El Pokémon rival ataca a tu Pokémon; mientras cambias (o si no queda), va a por el entrenador.</summary>
        IEnumerator RetargetEnemy()
        {
            while (!over)
            {
                yield return new WaitForSeconds(0.5f);
                if (enemyAi == null || enemy == null || enemy.IsDead) continue;
                var target = party.TargetForEnemies();
                if (enemyAi.Target != target) enemyAi.SetTarget(target);
            }
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
            enemyAi.SetTarget(party.TargetForEnemies());
            Save.MarkSeen(species.DexNumber);

            var defeated = enemy;
            enemy.Died += _ => OnEnemyDefeated(species, level, defeated);

            party.Controller.SetLock(enemy);
            ctx.Hud.SetStatus($"{trainer.Title} {trainer.Name}\n<size=24>Pokémon {index + 1} de {trainer.Team.Length}</size>");
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
