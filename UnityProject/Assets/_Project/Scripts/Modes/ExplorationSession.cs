using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Prototype;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Modes
{
    /// <summary>
    /// Explorar una zona de la Aventura: Pokémon salvajes que pasean, combate en el sitio,
    /// captura con Poké Ball (GDD §9), experiencia, evolución y Pokédex.
    /// </summary>
    public class ExplorationSession
    {
        const float FieldHalfSize = 30f;
        const int MaxWild = 7;
        const float SpawnInterval = 4f;
        const float ThrowRange = 12f;
        const float SeenRange = 14f;
        const float BossCaptureHp = 0.25f;

        readonly GameContext ctx;
        readonly ZoneDef zone;
        readonly Action onLeave;
        readonly List<WildBehaviour> wilds = new List<WildBehaviour>();
        readonly System.Random rng = new System.Random();

        Transform world;
        PartyFighter party;
        List<SpeciesData> pool;
        SpeciesData boss;
        int ballIndex;
        Button ballButton;
        Button potionButton;
        bool capturing;
        bool leaving;

        SaveData Save => ctx.Save;

        public ExplorationSession(GameContext ctx, ZoneDef zone, Action onLeave)
        {
            this.ctx = ctx;
            this.zone = zone;
            this.onLeave = onLeave;
        }

        public void Start()
        {
            world = ctx.NewWorld(zone.Name);
            EnvironmentBuilder.BuildField(zone.Biome, world, FieldHalfSize, zone.Id * 31 + 7);

            var species = ZoneCatalog.WildSpecies(zone, PokemonDatabase.All);
            if (zone.Id == 16 && Save.CaughtCount >= 150) species.AddRange(PokemonDatabase.All.Where(s => s.Zone == 17));
            pool = species.Where(s => !s.IsSpecial).ToList();
            boss = species.FirstOrDefault(s => s.IsSpecial && !Save.Caught.Contains(s.DexNumber));

            ctx.Hud.gameObject.SetActive(true);
            ctx.Follow.SetOffset(FollowCamera.DefaultOffset);
            party = new PartyFighter(ctx, world, Vector3.zero, Vector3.forward);
            party.AllFainted += OnTeamFainted;
            if (!party.SpawnFirstHealthy())
            {
                Save.HealAll();
                party.SpawnFirstHealthy();
            }

            BuildButtons();
            ctx.Hud.Toast(zone.Name, 2.5f);

            for (int i = 0; i < MaxWild - 2; i++) SpawnWild();
            if (boss != null) SpawnWild(boss);

            ctx.Runner.StartCoroutine(Loop());
        }

        void BuildButtons()
        {
            var hud = ctx.Hud;
            var right = new Vector2(1f, 0f);
            ballButton = hud.AddButton("", new Color(0.85f, 0.2f, 0.2f), right, new Vector2(-600f, 250f), new Vector2(200f, 90f), ThrowBall, 22);
            hud.AddButton("Cambiar bola", new Color(0.45f, 0.2f, 0.2f), right, new Vector2(-600f, 160f), new Vector2(200f, 70f), NextBall, 20);
            hud.AddButton("Fijar", new Color(0.85f, 0.65f, 0.15f), right, new Vector2(-40f, 495f), new Vector2(140f, 70f),
                party.Controller.CycleLock, 24);
            potionButton = hud.AddButton("", new Color(0.3f, 0.65f, 0.4f), new Vector2(0f, 1f), new Vector2(30f, -240f), new Vector2(240f, 70f),
                () => { party.UsePotion(); RefreshButtons(); }, 22);
            hud.AddButton("Cambiar Pokémon", new Color(0.3f, 0.45f, 0.7f), new Vector2(0f, 1f), new Vector2(290f, -240f), new Vector2(260f, 70f),
                party.SwitchNext, 22);
            hud.AddButton("Salir de la zona", new Color(0.35f, 0.35f, 0.4f), new Vector2(1f, 1f), new Vector2(-30f, -160f), new Vector2(240f, 70f),
                Leave, 22);
            RefreshButtons();
        }

        BallInfo CurrentBall => Balls.All[ballIndex];

        void NextBall()
        {
            for (int step = 1; step <= Balls.All.Length; step++)
            {
                int index = (ballIndex + step) % Balls.All.Length;
                if (Save.ItemCount(Balls.All[index].Key) > 0)
                {
                    ballIndex = index;
                    break;
                }
            }
            RefreshButtons();
        }

        void RefreshButtons()
        {
            if (Save.ItemCount(CurrentBall.Key) == 0) NextBallSilently();
            ballButton.GetComponentInChildren<Text>().text = $"Lanzar {CurrentBall.Name}\n×{Save.ItemCount(CurrentBall.Key)}";
            potionButton.GetComponentInChildren<Text>().text = $"Poción ×{party.PotionCount}";
        }

        void NextBallSilently()
        {
            for (int i = 0; i < Balls.All.Length; i++)
                if (Save.ItemCount(Balls.All[i].Key) > 0) { ballIndex = i; return; }
        }

        IEnumerator Loop()
        {
            float spawnTimer = SpawnInterval;
            while (!leaving)
            {
                yield return new WaitForSeconds(0.25f);
                wilds.RemoveAll(w => w == null);

                spawnTimer -= 0.25f;
                if (spawnTimer <= 0f && wilds.Count < MaxWild)
                {
                    spawnTimer = SpawnInterval;
                    SpawnWild();
                }

                var trainer = party.Trainer;
                if (trainer == null || trainer.IsDead) continue;
                party.SyncHp();

                // Pokédex: visto al acercarse. Panel del rival: el objetivo fijado o, si no hay, el más cercano peleando.
                WildBehaviour focus = null;
                float best = float.MaxValue;
                foreach (var wild in wilds)
                {
                    var c = wild.GetComponent<Combatant>();
                    if (c == null || c.IsDead || !c.gameObject.activeInHierarchy) continue;
                    float d = CombatQueries.FlatDistance(c.transform.position, trainer.transform.position);
                    if (d < SeenRange) Save.MarkSeen(c.Species.DexNumber);
                    float score = wild.Aggro ? d - 100f : d;
                    if (d < ThrowRange && score < best)
                    {
                        best = score;
                        focus = wild;
                    }
                }

                var target = party.Controller.LockedTarget ?? (focus != null ? focus.GetComponent<Combatant>() : null);
                ctx.Hud.SetEnemy(target);
                ctx.Hud.SetStatus(StatusLine(target));
            }
        }

        string StatusLine(Combatant target)
        {
            string line = $"{zone.Name}\n<size=22>Capturados {Save.CaughtCount}/151 · {Save.Money} ₽</size>";
            if (target != null && !capturing)
            {
                var wild = target.GetComponent<WildBehaviour>();
                if (wild.Boss && target.Hp / target.MaxHp > BossCaptureHp)
                    line += "\n<size=22>Debilítalo para poder capturarlo</size>";
                else
                {
                    float p = CaptureCalculator.Probability(target.Species.CaptureRate, target.Hp, target.MaxHp, CurrentBall.Bonus,
                        CaptureCalculator.StatusBonus(target.Status), wild.Aggro ? 1f : 1.5f);
                    line += $"\n<size=22>Captura con {CurrentBall.Name}: {Mathf.RoundToInt(p * 100f)}%</size>";
                }
            }
            return line;
        }

        // ------------------------------------------------------------------ salvajes

        void SpawnWild(SpeciesData forced = null)
        {
            var player = party.Trainer;
            if (pool.Count == 0 && forced == null) return;

            var species = forced ?? PickWeighted();
            bool isBoss = forced != null && forced.IsSpecial;
            int level = isBoss ? zone.MaxLevel + 5 : rng.Next(zone.MinLevel, zone.MaxLevel + 1);

            Vector3 position = Vector3.zero;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                position = PlayArea.RandomFreePoint(species.Size, 2f);
                if (player == null || CombatQueries.FlatDistance(position, player.transform.position) > 10f) break;
            }

            int shinyOdds = Save.CaughtCount >= 150 ? 1365 : 4096;
            bool shiny = rng.Next(shinyOdds) == 0;

            float hpScale = isBoss ? 3f : 1f;
            var facing = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f) * Vector3.forward;
            var combatant = CreatureFactory.Create(species, 2, position, facing, level, shiny, PokemonDatabase.MovesAt(species, level), world);
            combatant.HpScale = hpScale;
            combatant.Reinitialize(species, level, PokemonDatabase.MovesAt(species, level), 1f);

            var ai = combatant.gameObject.AddComponent<EnemyAI>();
            ai.DodgeChance = isBoss ? 0.6f : 0.25f;
            var wild = combatant.gameObject.AddComponent<WildBehaviour>();
            bool aggressive = isBoss || rng.NextDouble() < 0.25;
            // Como en los Legends, algunos agresivos van directamente a por el entrenador.
            bool huntsTrainer = aggressive && !isBoss && rng.NextDouble() < 0.35;
            wild.Setup(party.TargetForEnemies, () => party.Trainer, level, isBoss, aggressive, huntsTrainer);
            combatant.Died += c => OnWildDefeated(wild, c);
            wilds.Add(wild);

            if (shiny) ctx.Hud.Toast("¡Ha aparecido un Pokémon variocolor!", 2.5f);
            if (isBoss) ctx.Hud.Toast($"¡Un {species.Name} legendario ronda la zona!", 3f);
        }

        SpeciesData PickWeighted()
        {
            int total = pool.Sum(s => Mathf.Max(5, s.CaptureRate));
            int roll = rng.Next(total);
            foreach (var s in pool)
            {
                roll -= Mathf.Max(5, s.CaptureRate);
                if (roll < 0) return s;
            }
            return pool[pool.Count - 1];
        }

        void OnWildDefeated(WildBehaviour wild, Combatant c)
        {
            party.AwardExp(c.Species, wild.Level, fromTrainer: false);
            Save.Money += wild.Level * 5;
            ctx.Runner.StartCoroutine(RemoveLater(c.gameObject, 1.5f));
        }

        static IEnumerator RemoveLater(GameObject go, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (go != null) UnityEngine.Object.Destroy(go);
        }

        // ------------------------------------------------------------------ captura

        /// <summary>El entrenador lanza la Poké Ball: al objetivo fijado o, si no hay, al salvaje más cercano.</summary>
        void ThrowBall()
        {
            var player = party.Trainer;
            if (capturing || player == null || player.IsDead) return;

            var ball = CurrentBall;
            if (Save.ItemCount(ball.Key) <= 0)
            {
                ctx.Hud.Toast("No te quedan Poké Balls. Cómpralas en la tienda.", 2f);
                return;
            }

            var locked = party.Controller.LockedTarget;
            bool lockedIsWild = locked != null && locked.GetComponent<WildBehaviour>() != null
                && CombatQueries.FlatDistance(locked.transform.position, player.transform.position) < ThrowRange;
            var target = lockedIsWild ? locked : wilds
                .Select(w => w != null ? w.GetComponent<Combatant>() : null)
                .Where(c => c != null && !c.IsDead && c.gameObject.activeInHierarchy)
                .OrderBy(c => CombatQueries.FlatDistance(c.transform.position, player.transform.position))
                .FirstOrDefault(c => CombatQueries.FlatDistance(c.transform.position, player.transform.position) < ThrowRange);
            if (target == null)
            {
                ctx.Hud.Toast("No hay ningún Pokémon salvaje cerca", 1.5f);
                return;
            }

            var wild = target.GetComponent<WildBehaviour>();
            if (wild.Boss && target.Hp / target.MaxHp > BossCaptureHp)
            {
                ctx.Hud.Toast("¡Es demasiado fuerte! Debilítalo primero.", 2f);
                return;
            }

            Save.UseItem(ball.Key);
            RefreshButtons();
            ctx.Runner.StartCoroutine(CaptureSequence(player, target, wild, ball));
        }

        IEnumerator CaptureSequence(Combatant player, Combatant target, WildBehaviour wild, BallInfo ball)
        {
            capturing = true;
            var ballObject = BuildBall(ball);
            ballObject.transform.SetParent(world, true);
            Vector3 from = player.transform.position + Vector3.up * 1.2f;

            // Vuelo en arco hacia donde está el objetivo
            const float flight = 0.55f;
            for (float t = 0f; t < flight; t += Time.deltaTime)
            {
                if (target == null) break;
                float k = t / flight;
                Vector3 to = target.transform.position + Vector3.up * 0.8f;
                ballObject.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2f;
                ballObject.transform.Rotate(720f * Time.deltaTime, 0f, 0f);
                yield return null;
            }

            bool hit = target != null && !target.IsDead && !target.Motor.IsInvulnerable
                       && CombatQueries.FlatDistance(ballObject.transform.position, target.transform.position) < target.Radius + 1f;
            if (!hit)
            {
                ctx.Hud.Toast("¡Falló!", 1.2f);
                UnityEngine.Object.Destroy(ballObject);
                if (wild != null) wild.Engage();
                capturing = false;
                yield break;
            }

            var result = CaptureCalculator.Attempt(target.Species.CaptureRate, target.Hp, target.MaxHp, ball.Bonus,
                CaptureCalculator.StatusBonus(target.Status), wild.Aggro ? 1f : 1.5f, rng);

            // El Pokémon entra en la bola; la bola cae y se sacude
            Vfx.Burst(target.transform.position + Vector3.up, Color.white, 2f);
            target.gameObject.SetActive(false);
            Vector3 ground = target.transform.position + Vector3.up * 0.25f;
            ballObject.transform.position = ground;
            ballObject.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(0.4f);

            for (int shake = 0; shake < result.Shakes; shake++)
            {
                for (float t = 0f; t < 0.45f; t += Time.deltaTime)
                {
                    ballObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t / 0.45f * Mathf.PI * 2f) * 25f);
                    yield return null;
                }
                ballObject.transform.rotation = Quaternion.identity;
                yield return new WaitForSeconds(0.35f);
            }

            if (result.Caught) Caught(target, wild, ball, ballObject);
            else
            {
                Vfx.Burst(ground, Color.white, 2f);
                UnityEngine.Object.Destroy(ballObject);
                target.gameObject.SetActive(true);
                wild.Engage();
                ctx.Hud.Toast("¡Oh, no! ¡Se ha escapado!", 1.8f);
            }
            capturing = false;
        }

        void Caught(Combatant target, WildBehaviour wild, BallInfo ball, GameObject ballObject)
        {
            var species = target.Species;
            bool isNew = !Save.Caught.Contains(species.DexNumber);
            var instance = PokemonInstance.Create(species, wild.Level, target.Shiny, ball.Key);
            bool toTeam = Save.AddPokemon(instance);
            Save.MarkCaught(species.DexNumber);
            party.AwardExp(species, wild.Level, fromTrainer: false);

            Vfx.Burst(ballObject.transform.position + Vector3.up * 0.5f, new Color(1f, 0.9f, 0.3f), 2.5f);
            UnityEngine.Object.Destroy(ballObject, 1f);
            wilds.Remove(wild);
            UnityEngine.Object.Destroy(target.gameObject);

            string where = toTeam ? "" : "\n<size=34>Enviado al PC</size>";
            string fresh = isNew ? "\n<size=34>¡Nuevo registro en la Pokédex!</size>" : "";
            ctx.Hud.Toast($"¡{species.Name} capturado!{where}{fresh}", 3f);
            SaveService.Save();
        }

        static GameObject BuildBall(BallInfo ball)
        {
            Color top = ball.Key == Balls.SuperBall.Key ? new Color(0.2f, 0.4f, 0.9f)
                : ball.Key == Balls.UltraBall.Key ? new Color(0.15f, 0.15f, 0.15f)
                : ball.Key == Balls.MasterBall.Key ? new Color(0.5f, 0.2f, 0.7f)
                : new Color(0.9f, 0.15f, 0.15f);
            var root = new GameObject(ball.Name);
            var upper = Vfx.Primitive(PrimitiveType.Sphere, "Arriba", top).transform;
            upper.SetParent(root.transform, false);
            upper.localScale = Vector3.one * 0.45f;
            var lower = Vfx.Primitive(PrimitiveType.Sphere, "Abajo", Color.white).transform;
            lower.SetParent(root.transform, false);
            lower.localPosition = new Vector3(0f, -0.02f, 0f);
            lower.localScale = new Vector3(0.452f, 0.22f, 0.452f);
            var band = Vfx.Primitive(PrimitiveType.Cylinder, "Banda", new Color(0.1f, 0.1f, 0.1f)).transform;
            band.SetParent(root.transform, false);
            band.localScale = new Vector3(0.46f, 0.02f, 0.46f);
            return root;
        }

        // ------------------------------------------------------------------ salida

        void OnTeamFainted()
        {
            leaving = true;
            int lost = Save.Money / 10;
            Save.Money -= lost;
            Save.HealAll();
            SaveService.Save();
            ctx.Hud.Toast($"Tu equipo se ha debilitado...\n<size=34>Vuelves al Centro Pokémon (-{lost} ₽)</size>", 3f);
            ctx.Runner.StartCoroutine(LeaveAfter(2.5f));
        }

        IEnumerator LeaveAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            onLeave();
        }

        void Leave()
        {
            if (capturing) return;
            leaving = true;
            party.SyncHp();
            SaveService.Save();
            onLeave();
        }
    }
}
