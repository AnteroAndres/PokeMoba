using System.Collections;
using System.Collections.Generic;
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
    /// MOBA 3v3 (GDD §7.3): tres carriles con una torre por carril y un núcleo por equipo, oleadas de súbditos,
    /// bots, niveles de partida (1–15) con evolución durante la partida y reaparición. Gana quien destruya el
    /// núcleo rival; si se acaba el tiempo, quien haya destruido más torres (y luego más derribos).
    /// </summary>
    public class MobaMode : GameMode
    {
        const float MatchSeconds = 600f;
        const float WaveInterval = 25f;
        const int MinionsPerWave = 3;
        const int MaxMatchLevel = 15;
        const float BaseHealRadius = 8f;
        const float ExpShareRadius = 14f;
        static readonly string[] MinionKeys = { "rattata", "spearow" };

        class Champion
        {
            public Combatant Unit;
            public MobaUnitAI Ai;
            public List<SpeciesData> Path;
            public int Team;
            public int Lane;
            public int MatchLevel = 1;
            public int Exp;
            public float RespawnAt;
            public bool IsPlayer;
        }

        readonly List<Champion> champions = new List<Champion>();
        readonly List<Combatant> towers = new List<Combatant>();
        readonly Combatant[] cores = new Combatant[2];
        readonly int[] kills = new int[2];
        readonly int[] towersDestroyed = new int[2]; // torres que ha destruido cada equipo

        Transform world;
        Champion player;
        float startTime;
        bool over;

        public MobaMode(GameContext ctx) : base(ctx) { }

        public override void Enter() => ShowPicker();

        void ShowPicker()
        {
            Ctx.ClearWorld();
            var owned = Save.UnlockAll
                ? PokemonDatabase.All.ToList()
                : PokemonDatabase.All.Where(s => Save.AllPokemon.Any(p => p.Key == s.Key)).ToList();

            var panel = Ctx.Menus.Open("MOBA 3v3: elige tu Pokémon",
                "Empiezas en su primera etapa y evolucionas durante la partida · Victorias: " + Save.MobaWins);
            if (owned.Count == 0)
                MenuScreens.Label(panel, "Todavía no tienes Pokémon. Captura en la Aventura o activa el modo pruebas en el menú.",
                    34, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 200f));
            else
                SpeciesPicker.Build(panel, owned, StartMatch, 200f, 130f);

            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 25f),
                new Vector2(420f, 80f), () => Ctx.ReturnToMainMenu());
        }

        // ------------------------------------------------------------------ preparación

        void StartMatch(SpeciesData chosen)
        {
            champions.Clear();
            towers.Clear();
            kills[0] = kills[1] = 0;
            towersDestroyed[0] = towersDestroyed[1] = 0;
            over = false;

            world = Ctx.NewWorld("MOBA");
            MobaMapBuilder.Build(world, CreatureFactory.TeamColors[0], CreatureFactory.TeamColors[1]);

            for (int team = 0; team < 2; team++)
            {
                for (int lane = 0; lane < 3; lane++)
                    towers.Add(CreateStructure(team, MobaMapBuilder.TowerPosition(lane, team), isCore: false));
                cores[team] = CreateStructure(team, MobaMapBuilder.CorePosition(team), isCore: true);
            }

            // Jugador en el carril central; aliados arriba y abajo; un rival por carril.
            player = AddChampion(chosen, team: 0, lane: 1, isPlayer: true);
            var bots = PokemonDatabase.Dex.FinalStages.Where(s => !s.IsSpecial && s != chosen).OrderBy(_ => Random.value).Take(5).ToList();
            AddChampion(bots[0], 0, 0, false);
            AddChampion(bots[1], 0, 2, false);
            for (int lane = 0; lane < 3; lane++) AddChampion(bots[2 + lane], 1, lane, false);

            Combatant.AnyKilled += OnKilled;

            Ctx.Hud.gameObject.SetActive(true);
            Ctx.Hud.Bind(player.Unit, null);
            Ctx.Hud.ExperienceSource = () => new Leveling.ExperienceProgress(player.Exp, ExpToNext(player.MatchLevel),
                CombatLevel(Mathf.Min(MaxMatchLevel, player.MatchLevel + 1)), player.MatchLevel >= MaxMatchLevel);
            Ctx.Hud.AddButton("Rendirse", new Color(0.35f, 0.35f, 0.4f), new Vector2(1f, 1f), new Vector2(-30f, -160f), new Vector2(200f, 70f),
                () => End(1), 22);
            Ctx.Follow.SetOffset(new Vector3(0f, 16f, -11.5f));
            Ctx.Follow.SetTargets(player.Unit.transform, null);
            Ctx.Hud.Toast("¡Destruye el núcleo rojo!", 2.5f);

            startTime = Time.time;
            Ctx.Runner.StartCoroutine(Loop());
        }

        static int CombatLevel(int matchLevel) => 18 + matchLevel * 3;
        static int ExpToNext(int matchLevel) => 100 + 40 * matchLevel;

        Champion AddChampion(SpeciesData target, int team, int lane, bool isPlayer)
        {
            var path = PokemonDatabase.Dex.EvolutionPath(target);
            var champion = new Champion { Path = path, Team = team, Lane = lane, IsPlayer = isPlayer };
            var species = StageFor(champion);
            int level = CombatLevel(1);
            var spawn = MobaMapBuilder.SpawnPoint(team, lane);
            champion.Unit = CreatureFactory.Create(species, team, spawn, team == 0 ? Vector3.right : Vector3.left, level,
                false, PokemonDatabase.MovesAt(species, level), world);

            if (isPlayer)
            {
                champion.Unit.gameObject.AddComponent<PlayerController>()
                    .Bind(Ctx.Hud.Joystick, Ctx.Hud.AbilityButtons, Ctx.Hud.DodgeButton, Ctx.Camera);
            }
            else
            {
                var ai = champion.Unit.gameObject.AddComponent<EnemyAI>();
                ai.DodgeChance = Difficulty.DodgeChance * 0.8f;
                champion.Ai = champion.Unit.gameObject.AddComponent<MobaUnitAI>();
                champion.Ai.Setup(MobaUnitAI.Kind.Champion, MobaMapBuilder.PathFor(lane, team), MobaMapBuilder.CorePosition(team));
            }

            champions.Add(champion);
            return champion;
        }

        /// <summary>Etapa evolutiva según el nivel de partida (2 etapas: evoluciona a nivel 6; 3 etapas: a 5 y 9).</summary>
        static SpeciesData StageFor(Champion c)
        {
            int stage;
            if (c.Path.Count >= 3) stage = c.MatchLevel >= 9 ? 2 : c.MatchLevel >= 5 ? 1 : 0;
            else if (c.Path.Count == 2) stage = c.MatchLevel >= 6 ? 1 : 0;
            else stage = 0;
            return c.Path[Mathf.Min(stage, c.Path.Count - 1)];
        }

        Combatant CreateStructure(int team, Vector3 position, bool isCore)
        {
            var root = new GameObject(isCore ? "Núcleo" : "Torre");
            root.transform.SetParent(world, false);
            root.transform.position = position;

            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            Color color = CreatureFactory.TeamColors[team];
            if (isCore)
            {
                Part(model, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(4f, 0.4f, 4f), new Color(0.75f, 0.75f, 0.8f));
                Part(model, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f), new Vector3(3f, 2.4f, 3f), Color.Lerp(color, Color.white, 0.2f));
                Part(model, PrimitiveType.Cube, new Vector3(0f, 3.4f, 0f), Vector3.one * 0.9f, Color.Lerp(color, Color.white, 0.6f), new Vector3(45f, 0f, 45f));
            }
            else
            {
                Part(model, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(2.4f, 0.3f, 2.4f), new Color(0.6f, 0.6f, 0.65f));
                Part(model, PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(1.3f, 1.8f, 1.3f), new Color(0.82f, 0.82f, 0.86f));
                Part(model, PrimitiveType.Sphere, new Vector3(0f, 4.1f, 0f), Vector3.one * 1.4f, color);
            }

            var shot = new MoveData
            {
                Name = isCore ? "Pulso del núcleo" : "Disparo de torre", Type = PokemonType.Normal, Category = MoveCategory.Especial,
                Power = isCore ? 70 : 55, Shape = MoveShape.Projectile, Range = isCore ? 10f : 9f, Radius = 0.5f,
                Cooldown = 1.1f, Windup = 0.05f, ProjectileSpeed = 20f
            };
            var species = new SpeciesData
            {
                DexNumber = 0, Key = isCore ? "core" : "tower", Name = isCore ? "Núcleo" : "Torre",
                Types = new[] { PokemonType.Normal }, BaseStats = new BaseStats(200, 80, 110, 90, 110, 1),
                Moves = new[] { shot, shot, shot }, Size = 1.5f
            };

            root.AddComponent<CreatureMotor>();
            var combatant = root.AddComponent<Combatant>();
            combatant.IsStructure = true;
            combatant.Radius = isCore ? 2f : 1.3f;
            combatant.HpScale = isCore ? 6f : 3.5f;
            combatant.Init(species, team, 30);
            combatant.DamageImmune = isCore;
            root.AddComponent<AbilityCaster>();
            root.AddComponent<TowerAI>().Range = shot.Range;
            return combatant;
        }

        static void Part(Transform model, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
        {
            var t = Vfx.Primitive(type, "Pieza", color).transform;
            t.SetParent(model, false);
            t.localPosition = position;
            t.localScale = scale;
            t.localRotation = Quaternion.Euler(euler);
        }

        // ------------------------------------------------------------------ partida

        IEnumerator Loop()
        {
            float nextWave = Time.time + 3f;
            while (!over)
            {
                yield return null;
                float now = Time.time;

                if (now >= nextWave)
                {
                    nextWave = now + WaveInterval;
                    SpawnWave();
                }

                foreach (var c in champions)
                {
                    if (c.Unit.IsDead)
                    {
                        if (now >= c.RespawnAt) Respawn(c);
                        else if (c.IsPlayer) Ctx.Hud.SetStatus(StatusLine() + $"\n<size=26>Reapareces en {Mathf.CeilToInt(c.RespawnAt - now)} s</size>");
                        continue;
                    }

                    // Curación en la base propia
                    if (CombatQueries.FlatDistance(c.Unit.transform.position, MobaMapBuilder.CorePosition(c.Team)) < BaseHealRadius)
                        c.Unit.Heal(c.Unit.MaxHp * 0.12f * Time.deltaTime);
                }

                // El núcleo es invulnerable mientras su equipo conserve todas las torres
                for (int team = 0; team < 2; team++)
                    cores[team].DamageImmune = towers.Where(t => t.Team == team).All(t => !t.IsDead);

                if (!player.Unit.IsDead)
                {
                    Ctx.Hud.SetStatus(StatusLine());
                    Ctx.Hud.SetEnemy(CombatQueries.NearestEnemy(player.Unit, 10f));
                }

                if (now - startTime >= MatchSeconds) TimeUp();
            }
        }

        string StatusLine()
        {
            float left = Mathf.Max(0f, MatchSeconds - (Time.time - startTime));
            string clock = $"{(int)left / 60:00}:{(int)left % 60:00}";
            string exp = player.MatchLevel >= MaxMatchLevel ? "máx." : $"{player.Exp}/{ExpToNext(player.MatchLevel)}";
            return $"<color=#5aa0ff>Azul {kills[0]}</color> – <color=#ff6a5a>{kills[1]} Rojo</color>   ·   {clock}\n" +
                   $"<size=22>Torres {towersDestroyed[0]}–{towersDestroyed[1]} · Nivel de partida {player.MatchLevel} ({exp})</size>";
        }

        void SpawnWave()
        {
            int minute = Mathf.FloorToInt((Time.time - startTime) / 60f);
            int level = 14 + minute * 2;
            for (int team = 0; team < 2; team++)
            {
                var species = PokemonDatabase.Get(MinionKeys[team]) ?? PokemonDatabase.All[0];
                for (int lane = 0; lane < 3; lane++)
                {
                    var path = MobaMapBuilder.PathFor(lane, team);
                    for (int i = 0; i < MinionsPerWave; i++)
                    {
                        var position = path[0] + new Vector3(0f, 0f, (i - 1) * 1.4f) + (team == 0 ? Vector3.right : Vector3.left) * 3f;
                        var minion = CreatureFactory.Create(species, team, position, team == 0 ? Vector3.right : Vector3.left, level,
                            false, PokemonDatabase.MovesAt(species, level), world);
                        minion.HpScale = 0.5f;
                        minion.Reinitialize(species, level, PokemonDatabase.MovesAt(species, level), 1f);
                        var ai = minion.gameObject.AddComponent<EnemyAI>();
                        ai.DodgeChance = 0f;
                        minion.gameObject.AddComponent<MobaUnitAI>().Setup(MobaUnitAI.Kind.Minion, path, MobaMapBuilder.CorePosition(team));
                    }
                }
            }
        }

        void Respawn(Champion c)
        {
            c.Unit.Revive();
            c.Unit.Motor.Teleport(MobaMapBuilder.SpawnPoint(c.Team, c.Lane));
            if (c.Ai != null) c.Ai.ResetPath();
            if (c.IsPlayer) Ctx.Hud.Toast("¡De vuelta a la batalla!", 1.2f);
        }

        void OnKilled(Combatant victim, Combatant killer)
        {
            if (over || victim == null) return;

            if (victim.IsStructure)
            {
                int destroyer = 1 - victim.Team;
                if (victim == cores[victim.Team])
                {
                    End(destroyer);
                    return;
                }
                towersDestroyed[destroyer]++;
                victim.transform.Find("Model").localScale = new Vector3(1f, 0.3f, 1f);
                Ctx.Hud.Toast(destroyer == 0 ? "¡Torre enemiga destruida!" : "¡Han destruido una de tus torres!", 2f);
                foreach (var c in champions.Where(c => c.Team == destroyer)) GiveExp(c, 150);
                return;
            }

            var champion = champions.FirstOrDefault(c => c.Unit == victim);
            if (champion != null)
            {
                kills[1 - champion.Team]++;
                champion.RespawnAt = Time.time + 4f + champion.MatchLevel * 0.6f;
                ShareExp(victim, 1 - champion.Team, 120 + 20 * champion.MatchLevel);
                if (champion.IsPlayer) Ctx.Hud.Toast("¡Te han debilitado!", 1.5f);
                else if (killer != null && killer == player.Unit) Ctx.Hud.Toast($"¡Has debilitado a {victim.DisplayName}!", 1.5f);
                return;
            }

            // Súbdito
            ShareExp(victim, 1 - victim.Team, 40);
            Ctx.Runner.StartCoroutine(RemoveLater(victim.gameObject, 1f));
        }

        void ShareExp(Combatant victim, int team, int amount)
        {
            foreach (var c in champions)
            {
                if (c.Team != team || c.Unit.IsDead) continue;
                if (CombatQueries.FlatDistance(c.Unit.transform.position, victim.transform.position) <= ExpShareRadius)
                    GiveExp(c, amount);
            }
        }

        void GiveExp(Champion c, int amount)
        {
            if (c.MatchLevel >= MaxMatchLevel) return;
            c.Exp += amount;
            bool leveled = false;
            while (c.MatchLevel < MaxMatchLevel && c.Exp >= ExpToNext(c.MatchLevel))
            {
                c.Exp -= ExpToNext(c.MatchLevel);
                c.MatchLevel++;
                leveled = true;
            }
            if (!leveled) return;

            var species = StageFor(c);
            int level = CombatLevel(c.MatchLevel);
            var moves = PokemonDatabase.MovesAt(species, level);
            if (species != c.Unit.Species)
            {
                string before = c.Unit.DisplayName;
                CreatureFactory.Rebuild(c.Unit, species, level, moves);
                if (c.IsPlayer) Ctx.Hud.Toast($"¡{before} evolucionó a {species.Name}!", 2.5f);
            }
            else
            {
                c.Unit.Reinitialize(species, level, moves, c.Unit.IsDead ? 0f : c.Unit.Hp / c.Unit.MaxHp);
            }

            if (c.IsPlayer)
            {
                Ctx.Hud.RefreshPlayer();
                if (species == c.Unit.Species) Ctx.Hud.Toast($"Nivel de partida {c.MatchLevel}", 1.2f);
            }
        }

        static IEnumerator RemoveLater(GameObject go, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (go != null) Object.Destroy(go);
        }

        void TimeUp()
        {
            int winner;
            if (towersDestroyed[0] != towersDestroyed[1]) winner = towersDestroyed[0] > towersDestroyed[1] ? 0 : 1;
            else if (kills[0] != kills[1]) winner = kills[0] > kills[1] ? 0 : 1;
            else winner = -1;
            End(winner);
        }

        void End(int winner)
        {
            if (over) return;
            over = true;
            Combatant.AnyKilled -= OnKilled;

            bool won = winner == 0;
            if (won)
            {
                Save.MobaWins++;
                Save.Money += 500;
                SaveService.Save();
            }

            string title = winner < 0 ? "¡Empate!" : won ? "¡Victoria!" : "Derrota";
            Ctx.Hud.Toast(title, 2f);
            Ctx.Runner.StartCoroutine(ShowResult(title, won));
        }

        IEnumerator ShowResult(string title, bool won)
        {
            yield return new WaitForSeconds(2f);
            Ctx.Hud.gameObject.SetActive(false);
            var panel = Ctx.Menus.Open(title,
                $"Derribos {kills[0]}–{kills[1]} · Torres {towersDestroyed[0]}–{towersDestroyed[1]}{(won ? " · +500 ₽" : "")}");
            MenuScreens.Button(panel, "Otra partida", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(-240f, -40f),
                new Vector2(420f, 120f), ShowPicker, 40);
            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(240f, -40f),
                new Vector2(420f, 120f), () => Ctx.ReturnToMainMenu(), 40);
        }
    }
}
