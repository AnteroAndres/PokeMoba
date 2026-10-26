using System.Linq;
using CharacterCustomizer;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Modes
{
    /// <summary>
    /// Modo Aventura (GDD §4–§6): crear entrenador, elegir inicial, mapa de Kanto con zonas que se abren
    /// al capturar y ganar medallas, gimnasios, Liga, tienda, Centro Pokémon y equipo.
    /// </summary>
    public class AdventureMode : GameMode
    {
        static readonly string[] StarterKeys = { "bulbasaur", "charmander", "squirtle" };
        const int StarterLevel = 5;

        Transform trainerPreview;
        SaveData appearance;
        float previewYaw = 20f;
        int customizationCategory;
        CharacterCustomizerUI packageCustomizer;

        public AdventureMode(GameContext ctx) : base(ctx) { }

        public void OpenCustomization()
        {
            ShowTrainerCreation();
        }

        public override void Enter()
        {
            if (Save.HasStarter) ShowZoneMap();
            else ShowTrainerCreation();
        }

        // ------------------------------------------------------------------ inicio

        void ShowTrainerCreation()
        {
            appearance = new SaveData();
            appearance.CopyAppearanceFrom(Save);
            previewYaw = 20f;
            customizationCategory = 0;
            OpenPackageCustomizer();
        }

        void OpenPackageCustomizer()
        {
            var world = Ctx.NewWorld("Vestidor del entrenador");
            EnvironmentBuilder.BuildArena(Biome.Liga, world, 8f);
            Ctx.Follow.LookAtPoint(new Vector3(2.5f, 1.2f, 0f), new Vector3(0f, .45f, 5.5f));
            RebuildTrainerPreview(world);

            var go = new GameObject("Character Customizer");
            go.SetActive(false);
            packageCustomizer = go.AddComponent<CharacterCustomizerUI>();
            packageCustomizer.BuildOnAwake = false;
            packageCustomizer.Initialize(trainerPreview, appearance.SkinColor, appearance.HairStyle, appearance.HairColor,
                RoleName(appearance.AvatarStyle), appearance.TrainerCharacter, SavePackageCustomization, CancelPackageCustomization,
                key =>
                {
                    appearance.TrainerCharacter = key;
                    RebuildTrainerPreview(world);
                    packageCustomizer.SetCharacterRoot(trainerPreview);
                });
            go.SetActive(true);
            packageCustomizer.OpenCharacterPicker();
        }

        string RoleName(int style) => style == 1 ? "Exploradora" : style == 2 ? "Sabio" : "Explorador";

        void SavePackageCustomization()
        {
            if (packageCustomizer == null) return;
            appearance.SkinColor = packageCustomizer.SelectedSkin;
            appearance.HairStyle = packageCustomizer.SelectedHair;
            appearance.HairColor = packageCustomizer.SelectedHairColor;
            appearance.TrainerCharacter = packageCustomizer.SelectedCharacter;
            appearance.HasChosenTrainerCharacter = true;
            appearance.AvatarStyle = packageCustomizer.SelectedRole == "Exploradora" ? 1 : packageCustomizer.SelectedRole == "Sabio" ? 2 : 0;
            appearance.TrainerName = string.IsNullOrWhiteSpace(appearance.TrainerName) ? "Entrenador" : appearance.TrainerName.Trim();
            Save.CopyAppearanceFrom(appearance);
            SaveService.Save();
            ClosePackageCustomizer();
            if (Save.HasStarter) ShowZoneMap(); else ShowStarterChoice();
        }

        void CancelPackageCustomization()
        {
            ClosePackageCustomizer();
            if (Save.HasStarter) ShowZoneMap(); else Ctx.ReturnToMainMenu();
        }

        void ClosePackageCustomizer()
        {
            if (packageCustomizer != null)
            {
                Object.Destroy(packageCustomizer.gameObject);
                packageCustomizer = null;
            }
        }

        void DrawTrainerCreation()
        {
            var world = Ctx.NewWorld("Vestidor del entrenador");
            EnvironmentBuilder.BuildArena(Biome.Liga, world, 8f);
            Ctx.Follow.LookAtPoint(new Vector3(1.6f, 1.2f, 0f), new Vector3(0f, .45f, 5.5f));
            RebuildTrainerPreview(world);
            var panel = Ctx.Menus.Open(null, null, .12f);

            MenuScreens.Label(panel, "Tu personaje", 48, new Vector2(0f, 1f), new Vector2(130f, -55f),
                new Vector2(760f, 70f), TextAnchor.MiddleLeft);
            MenuScreens.Label(panel, appearance.AvatarStyle == 2
                ? "Sabio · modelo importado · algunos accesorios están bloqueados"
                : "Personaliza tu entrenador · los cambios se ven al instante", 18, new Vector2(0f, 1f),
                new Vector2(130f, -105f), new Vector2(800f, 36f), TextAnchor.MiddleLeft);

            MenuScreens.Button(panel, "‹", new Color(.12f, .2f, .3f, .95f), new Vector2(0f, 1f),
                new Vector2(48f, -55f), new Vector2(70f, 70f), () =>
                {
                    if (Save.HasStarter) ShowZoneMap(); else Ctx.ReturnToMainMenu();
                }, 46);

            MenuScreens.Card(panel, new Color(.035f, .1f, .18f, .91f), new Vector2(0f, .5f),
                new Vector2(25f, -20f), new Vector2(850f, 780f));
            var nameField = UIFactory.CreateInputField("Nombre", panel, appearance.TrainerName, "Tu nombre", 28);
            nameField.characterLimit = 12;
            ((RectTransform)nameField.transform).Place(new Vector2(0f, .5f), new Vector2(245f, 310f), new Vector2(560f, 55f));
            nameField.onValueChanged.AddListener(value => appearance.TrainerName = value);

            string[] presets = { "Explorador", "Exploradora", "Sabio" };
            for (int i = 0; i < presets.Length; i++)
            {
                int style = i;
                MenuScreens.Button(panel, presets[i], appearance.AvatarStyle == i ? new Color(.25f, .55f, .6f) : new Color(.22f, .27f, .36f),
                    new Vector2(0f, .5f), new Vector2(245f + i * 190f, 245f), new Vector2(175f, 58f), () =>
                    {
                        appearance.AvatarStyle = style;
                        appearance.HairStyle = style == 0 ? 0 : style == 1 ? 2 : 3;
                        appearance.HatStyle = style;
                        appearance.OutfitStyle = style;
                        appearance.BeardStyle = style == 2 ? 1 : 0;
                        appearance.GlassesStyle = style == 2 ? 1 : 0;
                        appearance.HairColor = style == 2 ? 5 : 0;
                        appearance.CapColor = style == 1 ? 5 : 0;
                        appearance.ShirtColor = style == 2 ? 7 : 1;
                        appearance.PantsColor = style == 2 ? 4 : 1;
                        DrawTrainerCreation();
                    }, 21);
            }

            string[] categories = { "Apariencia", "Cabeza", "Ropa", "Accesorios", "Vista previa" };
            for (int i = 0; i < categories.Length; i++)
            {
                int category = i;
                MenuScreens.Button(panel, categories[i], customizationCategory == i ? new Color(.25f, .55f, .8f) : new Color(.12f, .22f, .32f),
                    new Vector2(0f, .5f), new Vector2(125f, 170f - i * 82f), new Vector2(190f, 64f),
                    () => { customizationCategory = category; DrawTrainerCreation(); }, 18);
            }

            DrawCustomizationCategory(panel, world);

            MenuScreens.Button(panel, "↻  Girar vista", new Color(.18f, .3f, .42f), new Vector2(1f, .5f), new Vector2(-270f, -300f),
                new Vector2(260f, 65f), () => { previewYaw += 45f; RebuildTrainerPreview(world); }, 22);
            MenuScreens.Button(panel, Save.HasStarter ? "Guardar apariencia" : "Continuar", new Color(.2f, .6f, .3f),
                new Vector2(1f, 0f), new Vector2(-75f, 45f), new Vector2(430f, 85f), () =>
                {
                    appearance.TrainerName = string.IsNullOrWhiteSpace(appearance.TrainerName) ? "Entrenador" : appearance.TrainerName.Trim();
                    Save.CopyAppearanceFrom(appearance);
                    SaveService.Save();
                    if (Save.HasStarter) ShowZoneMap(); else ShowStarterChoice();
                }, 32);
            MenuScreens.Button(panel, "Cancelar", new Color(.35f, .35f, .4f), new Vector2(0f, 0f), new Vector2(75f, 45f),
                new Vector2(300f, 75f), () => { if (Save.HasStarter) ShowZoneMap(); else Ctx.ReturnToMainMenu(); }, 28);
        }

        void DrawCustomizationCategory(RectTransform panel, Transform world)
        {
            bool locked = appearance.AvatarStyle == 2;
            switch (customizationCategory)
            {
                case 0:
                    ColorRow(panel, "Piel", TrainerPalette.Skin, () => appearance.SkinColor, v => appearance.SkinColor = v, 300, 0, true);
                    ColorRow(panel, "Color de cabello", TrainerPalette.Hair, () => appearance.HairColor, v => appearance.HairColor = v, 300, -145, !locked);
                    ColorRow(panel, "Color de ojos", TrainerPalette.Eyes, () => appearance.EyeColor, v => appearance.EyeColor = v, 300, -290, !locked);
                    break;
                case 1:
                    OptionRow(panel, "Peinado", new[] { "Corto", "Largo", "Coleta", "Rapado" }, () => appearance.HairStyle,
                        v => { appearance.HairStyle = v; RebuildTrainerPreview(world); }, 300, 0, !locked);
                    OptionRow(panel, "Prenda de cabeza", new[] { "Gorra", "Pañuelo", "Sin prenda" }, () => appearance.HatStyle,
                        v => { appearance.HatStyle = v; RebuildTrainerPreview(world); }, 300, -145, !locked);
                    ColorRow(panel, "Color de gorra", TrainerPalette.Clothes, () => appearance.CapColor, v => appearance.CapColor = v, 300, -290, !locked);
                    break;
                case 2:
                    OptionRow(panel, "Conjunto", new[] { "Explorador", "Túnica", "Abrigo" }, () => appearance.OutfitStyle,
                        v => { appearance.OutfitStyle = v; RebuildTrainerPreview(world); }, 300, 0, !locked);
                    ColorRow(panel, "Color de ropa", TrainerPalette.Clothes, () => appearance.ShirtColor, v => appearance.ShirtColor = v, 300, -145, !locked);
                    ColorRow(panel, "Color de pantalón", TrainerPalette.Clothes, () => appearance.PantsColor, v => appearance.PantsColor = v, 300, -290, !locked);
                    break;
                case 3:
                    ColorRow(panel, "Pañuelo", TrainerPalette.Clothes, () => appearance.ScarfColor, v => appearance.ScarfColor = v, 300, 0, !locked);
                    OptionRow(panel, "Barba", new[] { "No", "Sí" }, () => appearance.BeardStyle,
                        v => { appearance.BeardStyle = v; RebuildTrainerPreview(world); }, 300, -145, !locked);
                    OptionRow(panel, "Gafas", new[] { "No", "Sí" }, () => appearance.GlassesStyle,
                        v => { appearance.GlassesStyle = v; RebuildTrainerPreview(world); }, 300, -290, !locked);
                    break;
                default:
                    MenuScreens.Label(panel, "Vista previa en 3D\n\nGira al entrenador con el botón inferior derecho.\nLos cambios se aplican al guardar.", 25,
                        new Vector2(0f, .5f), new Vector2(475f, 0f), new Vector2(650f, 200f));
                    break;
            }
        }

        void ColorRow(RectTransform panel, string title, Color[] colors, System.Func<int> get, System.Action<int> set,
            float x, float y, bool enabled)
        {
            MenuScreens.Label(panel, title, 21, new Vector2(0f, .5f), new Vector2(x, y + 42f), new Vector2(620f, 34f), TextAnchor.MiddleLeft);
            for (int i = 0; i < colors.Length; i++)
            {
                int index = i;
                Color swatch = Color.Lerp(colors[i], Color.white, get() == i ? .3f : 0f);
                MenuScreens.Button(panel, "", swatch, new Vector2(0f, .5f), new Vector2(x + i * 66f, y),
                    new Vector2(52f, 52f), () => { set(index); DrawTrainerCreation(); }, 1, enabled);
            }
        }

        void OptionRow(RectTransform panel, string title, string[] options, System.Func<int> get, System.Action<int> set,
            float x, float y, bool interactable)
        {
            MenuScreens.Label(panel, title, 21, new Vector2(0f, .5f), new Vector2(x, y + 42f), new Vector2(620f, 34f), TextAnchor.MiddleLeft);
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                MenuScreens.Button(panel, options[i], get() == i ? new Color(.2f, .55f, .75f) : new Color(.16f, .28f, .4f),
                    new Vector2(0f, .5f), new Vector2(x + i * 155f, y), new Vector2(140f, 60f),
                    () => { set(index); DrawTrainerCreation(); }, 17, interactable);
            }
        }

        void AppearanceButtonAt(RectTransform panel, Transform world, string part, System.Func<int> get, System.Action<int> set,
            int count, Vector2 position, string[] names = null, bool interactable = true)
        {
            string Label()
            {
                int value = ((get() % count) + count) % count;
                return part + "\n<size=20>" + (names != null ? names[value] : (value + 1) + "/" + count) + "</size>";
            }
            Button button = null;
            button = MenuScreens.Button(panel, Label(), new Color(.18f, .32f, .45f), new Vector2(0f, .5f), position,
                new Vector2(320f, 86f), () =>
                {
                    set((((get() % count) + count) % count + 1) % count);
                    button.GetComponentInChildren<Text>().text = Label();
                    RebuildTrainerPreview(world);
                }, 22, interactable);
        }

        void AppearanceButton(RectTransform panel, Transform world, string part, System.Func<int> get, System.Action<int> set,
            int count, int index, string[] names = null)
        {
            string Label()
            {
                int value = ((get() % count) + count) % count;
                return part + ": " + (names != null ? names[value] : (value + 1) + "/" + count);
            }
            Button button = null;
            button = MenuScreens.Button(panel, Label(), new Color(.22f, .30f, .42f), new Vector2(0f, .5f),
                new Vector2(50f + (index % 2) * 345f, 130f - (index / 2) * 76f), new Vector2(330f, 64f), () =>
                {
                    set((((get() % count) + count) % count + 1) % count);
                    button.GetComponentInChildren<Text>().text = Label();
                    RebuildTrainerPreview(world);
                }, 23, appearance.AvatarStyle != 2);
        }

        void RebuildTrainerPreview(Transform world)
        {
            if (trainerPreview != null)
            {
                trainerPreview.gameObject.SetActive(false);
                Object.Destroy(trainerPreview.gameObject);
            }
            trainerPreview = TrainerModelBuilder.Build(world, appearance, follower: false);
            trainerPreview.position = new Vector3(-0.9f, 0f, 0f);
            trainerPreview.rotation = Quaternion.Euler(0f, previewYaw, 0f);
            trainerPreview.localScale *= 1.15f;
        }

        void ShowStarterChoice()
        {
            var world = Ctx.NewWorld("Laboratorio del Profesor Oak");
            EnvironmentBuilder.BuildArena(Biome.Liga, world, 9f);
            Ctx.Follow.LookAtPoint(new Vector3(0f, 1f, 0f), new Vector3(0f, 4f, 8.5f));

            var starters = StarterKeys.Select(PokemonDatabase.Get).Where(s => s != null).ToArray();
            for (int i = 0; i < starters.Length; i++)
            {
                var pedestal = Pedestal(world, new Vector3((i - 1) * 3.2f, 0.25f, 0f));
                var model = CreatureModelBuilder.Build(starters[i], pedestal, shiny: false);
                model.localPosition = new Vector3(0f, 0.3f, 0f);
                model.localRotation = Quaternion.identity;
            }

            var panel = Ctx.Menus.Open("Laboratorio del Profesor Oak", $"{Save.TrainerName}, elige tu primer Pokémon", 0.1f);
            for (int i = 0; i < starters.Length; i++)
            {
                var species = starters[i];
                string types = string.Join(" / ", species.Types.Select(t => t.DisplayName()));
                MenuScreens.Button(panel, $"{species.Name}\n<size=24>{types}</size>", species.Types[0].TypeColor(),
                    new Vector2(0.5f, 0f), new Vector2((i - 1) * 420f, 60f), new Vector2(380f, 150f), () => ChooseStarter(species), 40);
            }
        }

        static Transform Pedestal(Transform world, Vector3 position)
        {
            var pedestal = Combat.Vfx.Primitive(PrimitiveType.Cylinder, "Pedestal", new Color(0.9f, 0.9f, 0.95f)).transform;
            pedestal.SetParent(world, false);
            pedestal.localPosition = position;
            pedestal.localScale = new Vector3(2.2f, 0.25f, 2.2f);
            var holder = new GameObject("Soporte").transform;
            holder.SetParent(world, false);
            holder.localPosition = position;
            return holder;
        }

        void ChooseStarter(SpeciesData species)
        {
            var instance = PokemonInstance.Create(species, StarterLevel, false, Balls.PokeBall.Key);
            Save.AddPokemon(instance);
            Save.MarkCaught(species.DexNumber);
            Save.HasStarter = true;
            SaveService.Save();
            ShowZoneMap($"¡{species.Name} se une a tu equipo! Explora la Ruta 1 y captura Pokémon.");
        }

        // ------------------------------------------------------------------ mapa

        void ShowZoneMap() => ShowZoneMap(null);

        void ShowZoneMap(string message)
        {
            Ctx.ClearWorld();
            string subtitle = message ??
                $"{Save.TrainerName} · Medallas {Save.Medals.Count}/8 · Capturados {Save.CaughtCount}/151 · {Save.Money} ₽";
            var panel = Ctx.Menus.Open("Kanto", subtitle, 0.95f);

            MenuScreens.Button(panel, "Personalizar personaje", new Color(.3f, .4f, .6f), new Vector2(1f, 1f),
                new Vector2(-40f, -100f), new Vector2(360f, 70f), ShowTrainerCreation, 25);

            var zones = ZoneCatalog.Zones;
            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                string lockReason = Progression.LockReason(zone, Save);
                bool unlocked = Progression.IsZoneUnlocked(zone, Save);
                int column = i % 4, row = i / 4;

                string gym = "";
                if (zone.Gym != null)
                    gym = Save.HasMedal(zone.Gym.Medal) ? $" · Medalla {zone.Gym.Medal} ✓" : $" · Gimnasio de {zone.Gym.Name}";
                if (zone.League != null)
                    gym = Save.LeagueChampion ? " · ¡Campeón!" : " · Liga Pokémon";

                string detail = unlocked ? $"Nv. {zone.MinLevel}–{zone.MaxLevel}{gym}" : lockReason ?? "";
                MenuScreens.Button(panel, $"{zone.Id}. {zone.Name}\n<size=20>{detail}</size>",
                    unlocked ? BiomeColor(zone.Biome) : new Color(0.25f, 0.25f, 0.28f),
                    new Vector2(0.5f, 0.5f), new Vector2((column - 1.5f) * 450f, 250f - row * 160f), new Vector2(430f, 145f),
                    () => ShowZone(zone), 24, unlocked);
            }

            var bottom = new Vector2(0.5f, 0f);
            MenuScreens.Button(panel, "Centro Pokémon", new Color(0.85f, 0.35f, 0.45f), bottom, new Vector2(-690f, 25f), new Vector2(420f, 85f), () =>
            {
                Save.HealAll();
                SaveService.Save();
                ShowZoneMap("Tus Pokémon se han recuperado por completo.");
            }, 30);
            MenuScreens.Button(panel, "Tienda", new Color(0.25f, 0.5f, 0.8f), bottom, new Vector2(-230f, 25f), new Vector2(420f, 85f), ShowShop, 30);
            MenuScreens.Button(panel, "Equipo", new Color(0.3f, 0.6f, 0.35f), bottom, new Vector2(230f, 25f), new Vector2(420f, 85f), ShowTeam, 30);
            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), bottom, new Vector2(690f, 25f), new Vector2(420f, 85f),
                () => Ctx.ReturnToMainMenu(), 30);
        }

        static Color BiomeColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Bosque: return new Color(0.2f, 0.45f, 0.25f);
                case Biome.Montana: return new Color(0.5f, 0.4f, 0.3f);
                case Biome.Cueva: return new Color(0.3f, 0.27f, 0.35f);
                case Biome.Ciudad: return new Color(0.35f, 0.45f, 0.6f);
                case Biome.Costa: return new Color(0.2f, 0.5f, 0.7f);
                case Biome.Fantasma: return new Color(0.4f, 0.28f, 0.5f);
                case Biome.Safari: return new Color(0.55f, 0.5f, 0.25f);
                case Biome.Industrial: return new Color(0.4f, 0.42f, 0.45f);
                case Biome.Nieve: return new Color(0.45f, 0.6f, 0.75f);
                case Biome.Volcan: return new Color(0.65f, 0.3f, 0.2f);
                case Biome.Liga: return new Color(0.45f, 0.35f, 0.65f);
                default: return new Color(0.3f, 0.55f, 0.3f);
            }
        }

        void ShowZone(ZoneDef zone)
        {
            var wild = ZoneCatalog.WildSpecies(zone, PokemonDatabase.All);
            string species = string.Join(", ", wild.Select(s => Save.Seen.Contains(s.DexNumber) ? s.Name : "???"));
            var panel = Ctx.Menus.Open($"{zone.Id}. {zone.Name}", $"Pokémon: {species}", 0.95f);
            MenuScreens.Label(panel, $"Niveles {zone.MinLevel}–{zone.MaxLevel} · Recompensa: {zone.Reward}", 28,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1500f, 60f));

            MenuScreens.Button(panel, "Explorar", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f),
                new Vector2(520f, 120f), () => new ExplorationSession(Ctx, zone, ShowZoneMap).Start(), 44);

            if (zone.Gym != null)
            {
                bool done = Save.HasMedal(zone.Gym.Medal);
                string team = string.Join(", ", zone.Gym.Team.Select(t => $"{PokemonDatabase.Get(t.Key)?.Name} Nv.{t.Level}"));
                MenuScreens.Button(panel, $"Gimnasio de {zone.Gym.Name} ({zone.Gym.Type.DisplayName()}){(done ? " ✓" : "")}\n<size=22>{team}</size>",
                    zone.Gym.Type.TypeColor(), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 130f),
                    () => new TrainerBattle(Ctx, zone.Gym, () => ShowZoneMap($"¡Ganaste la Medalla {zone.Gym.Medal}!"), ShowZoneMap).Start(), 30);
            }

            if (zone.League != null)
            {
                MenuScreens.Button(panel, $"Desafiar a la Liga Pokémon{(Save.LeagueChampion ? " ✓" : "")}\n<size=22>Alto Mando y Campeón seguidos</size>",
                    new Color(0.5f, 0.35f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(900f, 130f),
                    () => RunLeague(zone, 0), 30);
            }

            MenuScreens.Button(panel, "Volver al mapa", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(420f, 85f), ShowZoneMap, 30);
        }

        void RunLeague(ZoneDef zone, int index)
        {
            var trainer = zone.League[index];
            new TrainerBattle(Ctx, trainer, () =>
            {
                if (index + 1 < zone.League.Length)
                {
                    var next = zone.League[index + 1];
                    var panel = Ctx.Menus.Open($"¡Venciste a {trainer.Name}!", $"Siguiente: {next.Title} {next.Name}");
                    MenuScreens.Button(panel, "Continuar", new Color(0.2f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                        new Vector2(420f, 110f), () => RunLeague(zone, index + 1), 40);
                }
                else
                {
                    Save.LeagueChampion = true;
                    SaveService.Save();
                    ShowZoneMap("¡Eres el nuevo Campeón de Kanto! Has conseguido el Mega-Aro.");
                }
            }, ShowZoneMap).Start();
        }

        // ------------------------------------------------------------------ tienda

        void ShowShop()
        {
            var panel = Ctx.Menus.Open("Tienda", $"Tienes {Save.Money} ₽", 0.95f);
            int zoneReached = Progression.HighestUnlockedZone(Save);

            var products = new (string Key, string Name, int Price, int Zone)[]
            {
                (Balls.PokeBall.Key, Balls.PokeBall.Name, Balls.PokeBall.Price, Balls.PokeBall.ShopZone),
                (Balls.SuperBall.Key, Balls.SuperBall.Name, Balls.SuperBall.Price, Balls.SuperBall.ShopZone),
                (Balls.UltraBall.Key, Balls.UltraBall.Name, Balls.UltraBall.Price, Balls.UltraBall.ShopZone),
                (Items.Potion, "Poción (35 % de vida)", 300, 0),
                (Items.SuperPotion, "Superpoción (70 % de vida)", 700, 5),
            };

            for (int i = 0; i < products.Length; i++)
            {
                var p = products[i];
                bool available = Save.UnlockAll || zoneReached >= p.Zone;
                float y = 250f - i * 115f;
                string label = available
                    ? $"{p.Name} — {p.Price} ₽   <size=22>(tienes {Save.ItemCount(p.Key)})</size>"
                    : $"{p.Name} — <size=22>disponible desde la zona {p.Zone}</size>";
                MenuScreens.Label(panel, label, 30, new Vector2(0.5f, 0.5f), new Vector2(-250f, y), new Vector2(900f, 90f), TextAnchor.MiddleLeft);
                MenuScreens.Button(panel, "×1", new Color(0.25f, 0.5f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(330f, y),
                    new Vector2(150f, 90f), () => Buy(p.Key, p.Price, 1), 30, available && Save.Money >= p.Price);
                MenuScreens.Button(panel, "×10", new Color(0.2f, 0.4f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(500f, y),
                    new Vector2(150f, 90f), () => Buy(p.Key, p.Price, 10), 30, available && Save.Money >= p.Price * 10);
            }

            MenuScreens.Button(panel, "Volver al mapa", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(420f, 85f), ShowZoneMap, 30);
        }

        void Buy(string key, int price, int amount)
        {
            if (Save.Money < price * amount) return;
            Save.Money -= price * amount;
            Save.AddItem(key, amount);
            SaveService.Save();
            ShowShop();
        }

        // ------------------------------------------------------------------ equipo

        void ShowTeam()
        {
            var panel = Ctx.Menus.Open("Tu equipo", $"El primero es el que sale a combatir · En el PC: {Save.Box.Count}", 0.95f);

            for (int i = 0; i < Save.Team.Count; i++)
            {
                var p = Save.Team[i];
                var species = PokemonDatabase.Get(p.Key);
                int column = i % 3, row = i / 3;
                var position = new Vector2((column - 1) * 560f, 180f - row * 250f);

                var card = MenuScreens.Card(panel, Color.Lerp(species.Types[0].TypeColor(), Color.black, 0.35f), new Vector2(0.5f, 0.5f), position, new Vector2(530f, 230f));
                int nextExp = Leveling.ExpForLevel(species.GrowthRate, p.Level + 1);
                string hp = p.Fainted ? "<color=#ff7777>Debilitado</color>" : $"Vida {Mathf.RoundToInt(p.HpFraction * 100f)}%";
                MenuScreens.Label(card, $"{species.Name}{(p.Shiny ? " ★" : "")}  Nv. {p.Level}\n<size=22>{hp} · Exp {p.Exp}/{nextExp}</size>", 30,
                    new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(500f, 110f));

                int index = i;
                if (i > 0)
                    MenuScreens.Button(card, "Al frente", new Color(0.3f, 0.55f, 0.35f), new Vector2(0f, 0f), new Vector2(15f, 15f),
                        new Vector2(240f, 70f), () => { MoveToFront(index); ShowTeam(); }, 24);
                if (Save.Team.Count > 1)
                    MenuScreens.Button(card, "Al PC", new Color(0.45f, 0.35f, 0.35f), new Vector2(1f, 0f), new Vector2(-15f, 15f),
                        new Vector2(240f, 70f), () => { Save.Box.Add(Save.Team[index]); Save.Team.RemoveAt(index); SaveService.Save(); ShowTeam(); }, 24);
            }

            // PC: los primeros, para pasarlos al equipo
            var box = Save.Box.Take(8).ToList();
            for (int i = 0; i < box.Count; i++)
            {
                var p = box[i];
                var species = PokemonDatabase.Get(p.Key);
                MenuScreens.Button(panel, $"{species.Name} Nv.{p.Level}\n<size=20>al equipo</size>", new Color(0.3f, 0.3f, 0.4f),
                    new Vector2(0.5f, 0f), new Vector2((i - 3.5f) * 225f, 140f), new Vector2(210f, 90f), () =>
                    {
                        Save.Box.Remove(p);
                        if (Save.Team.Count >= SaveData.MaxTeam)
                        {
                            Save.Box.Add(Save.Team[Save.Team.Count - 1]);
                            Save.Team.RemoveAt(Save.Team.Count - 1);
                        }
                        Save.Team.Add(p);
                        SaveService.Save();
                        ShowTeam();
                    }, 22);
            }

            MenuScreens.Button(panel, "Volver al mapa", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 30f),
                new Vector2(420f, 85f), ShowZoneMap, 30);
        }

        void MoveToFront(int index)
        {
            var p = Save.Team[index];
            Save.Team.RemoveAt(index);
            Save.Team.Insert(0, p);
            SaveService.Save();
        }
    }
}
