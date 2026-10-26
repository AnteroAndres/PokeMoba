using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Input;
using PokeMoba.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PokeMoba.Prototype
{
    /// <summary>
    /// Fase 1: arena 1v1 contra la IA. Construye toda la escena por código,
    /// así la escena Prototype.unity solo necesita un objeto con este componente.
    /// </summary>
    public class PrototypeGame : MonoBehaviour
    {
        static readonly (string Name, float DodgeChance)[] Difficulties =
        {
            ("Fácil", 0.2f), ("Normal", 0.55f), ("Difícil", 0.85f)
        };

        static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -6f);
        static readonly Vector3 EnemySpawn = new Vector3(0f, 0f, 6f);

        Camera cam;
        FollowCamera follow;
        Canvas canvas;
        CombatHUD hud;
        GameObject menu;

        Combatant player;
        Combatant enemy;
        SpeciesData playerSpecies;
        SpeciesData rivalChoice; // null = rival aleatorio en cada partida
        int difficulty = 1;
        bool matchOver;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            EnsureEventSystem();
            SetupCamera();
            ArenaBuilder.Build();

            canvas = UIFactory.CreateCanvas("UI");
            hud = CombatHUD.Create(canvas.transform, showKeyboardHint: !Application.isMobilePlatform);
            hud.gameObject.SetActive(false);

            var floating = UIFactory.CreateRect("Textos flotantes", canvas.transform).Stretch();
            floating.gameObject.AddComponent<FloatingTextLayer>().Setup(cam);

            ShowPlayerPicker();
        }

        // --- Flujo de partida ---

        void StartMatch(SpeciesData species, SpeciesData rival)
        {
            CloseMenu();
            ClearMatch();
            playerSpecies = species;
            rivalChoice = rival;
            matchOver = false;

            var enemySpecies = rival;
            if (enemySpecies == null)
            {
                var others = PokemonDatabase.All.Where(s => s != species).ToArray();
                enemySpecies = others.Length > 0 ? others[Random.Range(0, others.Length)] : species;
            }

            player = CreatureFactory.Create(species, 0, PlayerSpawn, Vector3.forward, CombatTuning.DuelLevel);
            enemy = CreatureFactory.Create(enemySpecies, 1, EnemySpawn, Vector3.back, CombatTuning.DuelLevel);

            player.gameObject.AddComponent<PlayerController>()
                .Bind(hud.Joystick, hud.AbilityButtons, hud.DodgeButton, cam);

            var ai = enemy.gameObject.AddComponent<EnemyAI>();
            ai.DodgeChance = Difficulties[difficulty].DodgeChance;
            ai.SetTarget(player);

            player.Died += _ => EndMatch(won: false);
            enemy.Died += _ => EndMatch(won: true);

            hud.Bind(player, enemy);
            hud.gameObject.SetActive(true);
            follow.SetTargets(player.transform, enemy.transform);
        }

        void EndMatch(bool won)
        {
            if (matchOver) return;
            matchOver = true;
            StartCoroutine(ShowResultAfter(1.2f, won));
        }

        IEnumerator ShowResultAfter(float seconds, bool won)
        {
            yield return new WaitForSeconds(seconds);
            hud.gameObject.SetActive(false);
            ShowResult(won);
        }

        void ClearMatch()
        {
            if (player != null) Destroy(player.gameObject);
            if (enemy != null) Destroy(enemy.gameObject);
            player = null;
            enemy = null;

            foreach (var projectile in new List<Projectile>(Projectile.Active)) Destroy(projectile.gameObject);
            foreach (var area in new List<AreaTelegraph>(AreaTelegraph.Active)) Destroy(area.gameObject);
        }

        // --- Menús ---

        const float PickerTop = 200f;
        const float PickerBottom = 130f;

        void ShowPlayerPicker()
        {
            hud.gameObject.SetActive(false);
            var panel = OpenMenu("Elige tu Pokémon");
            SpeciesPicker.Build(panel, PokemonDatabase.All, ShowRivalPicker, PickerTop, PickerBottom);

            Button difficultyButton = null;
            difficultyButton = MenuButton(panel, DifficultyLabel(), new Color(0.25f, 0.25f, 0.3f), 0f, () =>
            {
                difficulty = (difficulty + 1) % Difficulties.Length;
                difficultyButton.GetComponentInChildren<Text>().text = DifficultyLabel();
            });
        }

        void ShowRivalPicker(SpeciesData species)
        {
            var panel = OpenMenu($"Elige el rival de {species.Name}");
            SpeciesPicker.Build(panel, PokemonDatabase.All, rival => StartMatch(species, rival), PickerTop, PickerBottom);

            MenuButton(panel, "Volver", new Color(0.3f, 0.3f, 0.5f), -240f, ShowPlayerPicker);
            MenuButton(panel, "Rival aleatorio", new Color(0.2f, 0.6f, 0.3f), 240f, () => StartMatch(species, null));
        }

        /// <summary>Botón en la franja inferior del menú, desplazado en horizontal desde el centro.</summary>
        static Button MenuButton(RectTransform panel, string label, Color color, float x, System.Action onClick)
        {
            var button = UIFactory.CreateButton(label, panel, label, 32, color, onClick);
            ((RectTransform)button.transform).Place(new Vector2(0.5f, 0f), new Vector2(x, 25f), new Vector2(420f, 80f));
            return button;
        }

        string DifficultyLabel() => $"Rival: {Difficulties[difficulty].Name}";

        void ShowResult(bool won)
        {
            var panel = OpenMenu(won ? "¡Ganaste!" : "Perdiste...");

            var rematch = UIFactory.CreateButton("Revancha", panel, "Revancha", 40, new Color(0.2f, 0.6f, 0.3f),
                () => StartMatch(playerSpecies, rivalChoice));
            ((RectTransform)rematch.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(-230f, -40f), new Vector2(400f, 120f));

            var change = UIFactory.CreateButton("Cambiar", panel, "Elegir otros", 40, new Color(0.3f, 0.3f, 0.5f), () =>
            {
                ClearMatch();
                ShowPlayerPicker();
            });
            ((RectTransform)change.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(230f, -40f), new Vector2(400f, 120f));
        }

        RectTransform OpenMenu(string title)
        {
            CloseMenu();
            var overlay = UIFactory.CreateImage("Menú", canvas.transform, new Color(0f, 0f, 0f, 0.7f));
            overlay.rectTransform.Stretch();
            menu = overlay.gameObject;

            var titleText = UIFactory.CreateText("Título", overlay.transform, title, 72, Color.white, TextAnchor.MiddleCenter);
            titleText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1400f, 120f));
            return overlay.rectTransform;
        }

        void CloseMenu()
        {
            if (menu != null) Destroy(menu);
            menu = null;
        }

        // --- Escena ---

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cam.fieldOfView = 50f;
            follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.SetTargets(null, null);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
