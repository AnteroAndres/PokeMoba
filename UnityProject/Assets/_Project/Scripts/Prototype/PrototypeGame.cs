using System.Linq;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Modes;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PokeMoba.Prototype
{
    /// <summary>
    /// Punto de entrada del juego. Construye cámara, interfaz y menú principal por código,
    /// así la escena Prototype.unity solo necesita un objeto con este componente.
    /// (Se llama PrototypeGame por compatibilidad con la escena ya creada.)
    /// </summary>
    public class PrototypeGame : MonoBehaviour
    {
        GameContext ctx;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            EnsureEventSystem();
            var cam = SetupCamera();

            var canvas = UIFactory.CreateCanvas("UI");
            var hud = CombatHUD.Create(canvas.transform, showKeyboardHint: !Application.isMobilePlatform);
            hud.gameObject.SetActive(false);

            var floating = UIFactory.CreateRect("Textos flotantes", canvas.transform).Stretch();
            floating.gameObject.AddComponent<FloatingTextLayer>().Setup(cam);

            ctx = new GameContext
            {
                Runner = this,
                Camera = cam,
                Follow = cam.GetComponent<FollowCamera>(),
                Canvas = canvas,
                Hud = hud,
                Menus = new MenuScreens(canvas.transform)
            };
            ctx.ReturnToMainMenu = ShowMainMenu;

            ShowMainMenu();
        }

        // ------------------------------------------------------------------ menú principal

        void ShowMainMenu()
        {
            var save = ctx.Save;
            BuildShowcase();

            string trainer = save.HasStarter
                ? $"{save.TrainerName} · Medallas {save.Medals.Count}/8 · Capturados {save.CaughtCount}/151 · {save.Money} ₽"
                : "Empieza la Aventura para elegir tu primer Pokémon";
            var panel = ctx.Menus.Open("PokeMoba", trainer, 0.35f);

            MenuScreens.Button(panel, "Campo: " + FieldTheme.Name, new Color(0.25f, 0.45f, 0.4f),
                new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(600f, 75f), ShowFields, 30);

            bool duel = Progression.DuelUnlocked(save);
            bool moba = Progression.MobaUnlocked(save);
            Card(panel, 0, "Aventura", "Explora Kanto, captura,\ngana medallas", new Color(0.2f, 0.6f, 0.3f), true, () => new AdventureMode(ctx).Enter());
            Card(panel, 1, "Duelo 1v1", duel ? "Tus Pokémon contra la IA\nen distintas arenas" : "Elige tu inicial\nen la Aventura",
                new Color(0.2f, 0.45f, 0.8f), duel, () => new DuelMode(ctx).Enter());
            Card(panel, 2, "MOBA 3v3", moba ? "Carriles, torres y súbditos\nEvoluciona en la partida" : "Consigue la\nMedalla Trueno",
                new Color(0.8f, 0.3f, 0.25f), moba, () => new MobaMode(ctx).Enter());
            Card(panel, 3, "Práctica", "Cualquiera de los 151\ncontra la IA", new Color(0.5f, 0.35f, 0.7f), true, () => new PracticeMode(ctx).Enter());

            var bottom = new Vector2(0.5f, 0f);
            MenuScreens.Button(panel, "Pokédex", new Color(0.85f, 0.25f, 0.25f), bottom, new Vector2(-460f, 60f), new Vector2(420f, 90f),
                () => new PokedexScreen(ctx).Enter(), 32);
            MenuScreens.Button(panel, $"Modo pruebas: {(save.UnlockAll ? "Sí" : "No")}\n<size=20>abre todas las zonas y modos</size>",
                save.UnlockAll ? new Color(0.75f, 0.55f, 0.15f) : new Color(0.3f, 0.3f, 0.35f), bottom, new Vector2(0f, 60f), new Vector2(420f, 90f),
                () =>
                {
                    save.UnlockAll = !save.UnlockAll;
                    SaveService.Save();
                    ShowMainMenu();
                }, 28);
            MenuScreens.Button(panel, "Borrar partida", new Color(0.35f, 0.25f, 0.25f), bottom, new Vector2(460f, 60f), new Vector2(420f, 90f),
                ConfirmDelete, 28);
        }

        void ShowFields()
        {
            var panel = ctx.Menus.Open("Tipos de campo", "Aspecto para Aventura, Duelo, Práctica y MOBA · conserva las reglas del combate");
            var types = (PokemonType[])System.Enum.GetValues(typeof(PokemonType));
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                MenuScreens.Button(panel, type.DisplayName(), type.TypeColor(), new Vector2(0.5f, 0.5f),
                    new Vector2((i % 6 - 2.5f) * 285f, 170f - (i / 6) * 145f), new Vector2(260f, 110f),
                    () => { FieldTheme.Selected = type; ShowMainMenu(); }, 30);
            }
            MenuScreens.Button(panel, "Según la zona", new Color(0.3f, 0.4f, 0.3f), new Vector2(0.5f, 0f),
                new Vector2(-240f, 90f), new Vector2(420f, 85f),
                () => { FieldTheme.Selected = null; ShowMainMenu(); }, 30);
            MenuScreens.Button(panel, "Volver", Color.gray, new Vector2(0.5f, 0f),
                new Vector2(240f, 90f), new Vector2(420f, 85f), ShowMainMenu, 30);
        }

        static void Card(RectTransform panel, int index, string title, string description, Color color, bool enabled, System.Action onClick)
        {
            MenuScreens.Button(panel, $"{title}\n<size=24>{description}</size>", color, new Vector2(0.5f, 0.5f),
                new Vector2((index - 1.5f) * 430f, 20f), new Vector2(400f, 300f), onClick, 50, enabled);
        }

        void ConfirmDelete()
        {
            var panel = ctx.Menus.Open("¿Borrar la partida?", "Se pierden el equipo, la Pokédex, las medallas y el dinero.");
            MenuScreens.Button(panel, "Sí, borrar", new Color(0.7f, 0.2f, 0.2f), new Vector2(0.5f, 0.5f), new Vector2(-240f, 0f),
                new Vector2(420f, 110f), () =>
                {
                    SaveService.Delete();
                    ShowMainMenu();
                }, 36);
            MenuScreens.Button(panel, "Cancelar", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0.5f), new Vector2(240f, 0f),
                new Vector2(420f, 110f), ShowMainMenu, 36);
        }

        /// <summary>Fondo del menú: el equipo del jugador (o Pokémon al azar) en el estadio.</summary>
        void BuildShowcase()
        {
            var world = ctx.NewWorld("Menú principal");
            EnvironmentBuilder.BuildArena(Biome.Estadio, world, 10f);

            var team = ctx.Save.Team.Select(p => PokemonDatabase.Get(p.Key)).Where(s => s != null).Take(4).ToList();
            var all = PokemonDatabase.All;
            while (team.Count < 4 && all.Count > 0) team.Add(all[Random.Range(0, all.Count)]);

            for (int i = 0; i < team.Count; i++)
            {
                var holder = new GameObject("Expositor").transform;
                holder.SetParent(world, false);
                holder.position = new Vector3((i - 1.5f) * 3.2f, 0f, -1f);
                holder.rotation = Quaternion.Euler(0f, (i - 1.5f) * -12f, 0f);
                CreatureModelBuilder.Build(team[i], holder, shiny: false);
            }
            ctx.Follow.LookAtPoint(new Vector3(0f, 1.2f, -1f), new Vector3(0f, 3f, 10f));
        }

        // ------------------------------------------------------------------ escena

        Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 300f;
            cam.gameObject.AddComponent<FollowCamera>();
            return cam;
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
