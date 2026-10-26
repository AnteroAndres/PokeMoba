using System.Collections.Generic;
using System.Linq;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using PokeMoba.Modes;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PokeMoba.Prototype
{
    /// <summary>Composites the selected 3D trainer over the illustrated lobby floor.</summary>
    public sealed class LobbyTrainerPreview : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const int PreviewLayer = 30;
        RectTransform panel, display, shadow;
        float backgroundAspect;
        RenderTexture target;
        Camera previewCamera;
        Transform trainer;
        bool dragging;
        int dragPointer;
        float previousDragX;

        public void Initialize(RectTransform parent, Transform world, SaveData save, Sprite background)
        {
            panel = parent;
            ((RectTransform)transform).Stretch();
            backgroundAspect = background != null ? background.rect.width / background.rect.height : 1852f / 849f;
            var origin = new Vector3(2000, 0, 0);
            trainer = TrainerModelBuilder.Build(world, save, follower: false);
            trainer.position = origin;
            trainer.rotation = Quaternion.Euler(0, 12, 0);
            foreach (var child in trainer.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;

            var cameraObject = new GameObject("Cámara del personaje del lobby");
            cameraObject.transform.SetParent(world, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.transform.position = origin + new Vector3(0, 1.175f, 5);
            previewCamera.transform.rotation = Quaternion.Euler(0, 180, 0);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 1.175f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.nearClipPlane = .1f;
            previewCamera.farClipPlane = 12f;
            previewCamera.allowHDR = false;
            target = new RenderTexture(1024, 1280, 24, RenderTextureFormat.ARGB32)
            {
                name = "Entrenador del lobby HD",
                antiAliasing = 4,
                filterMode = FilterMode.Bilinear
            };
            target.Create();
            previewCamera.targetTexture = target;
            AddLight(world, new Vector3(35, -35, 0), 1.15f);
            AddLight(world, new Vector3(15, 145, 0), .55f);

            var contact = UIFactory.CreateImage("Sombra de apoyo", transform, new Color(.025f, .04f, .09f, .2f), UIFactory.Circle);
            contact.raycastTarget = false;
            shadow = contact.rectTransform;
            display = UIFactory.CreateRect("Modelo 3D", transform);
            var image = display.gameObject.AddComponent<RawImage>();
            image.texture = target;
            image.raycastTarget = true;
            LateUpdate();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragging || trainer == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(display, eventData.position,
                eventData.pressEventCamera, out var local)) return;
            dragging = true;
            dragPointer = eventData.pointerId;
            previousDragX = local.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || eventData.pointerId != dragPointer || trainer == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(display, eventData.position,
                eventData.pressEventCamera, out var local)) return;
            float delta = local.x - previousDragX;
            previousDragX = local.x;
            // Normalize against the displayed width so mouse and touch feel alike at any UI scale.
            float angle = -delta / Mathf.Max(1f, display.rect.width) * 360f;
            trainer.Rotate(0, angle, 0, Space.World);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointer) dragging = false;
        }

        void OnDisable() => dragging = false;

        static void AddLight(Transform world, Vector3 rotation, float intensity)
        {
            var lightObject = new GameObject("Luz del personaje del lobby");
            lightObject.transform.SetParent(world, false);
            lightObject.transform.rotation = Quaternion.Euler(rotation);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << PreviewLayer;
            light.shadows = LightShadows.None;
        }

        void LateUpdate()
        {
            if (panel == null || display == null) return;
            // Same centered cover calculation used by the background's EnvelopeParent fitter.
            float height = Mathf.Max(panel.rect.height, panel.rect.width / backgroundAspect);
            float width = height * backgroundAspect;
            var feet = new Vector2((.568f - .5f) * width, (.165f - .5f) * height);
            float trainerHeight = height * .5f;
            display.anchorMin = display.anchorMax = new Vector2(.5f, .5f);
            display.pivot = new Vector2(.5f, 0);
            display.anchoredPosition = feet;
            display.sizeDelta = new Vector2(trainerHeight * .8f, trainerHeight);
            shadow.Place(new Vector2(.5f, .5f), feet, new Vector2(height * .17f, height * .027f));
        }

        void OnDestroy()
        {
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }

    /// <summary>Reports device network availability; does not imply a server connection or latency.</summary>
    public sealed class LobbyNetworkIndicator : MonoBehaviour
    {
        Image icon;
        float nextRefresh;

        void OnEnable()
        {
            icon = GetComponent<Image>();
            nextRefresh = 0;
        }

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f;
            icon.color = Application.internetReachability == NetworkReachability.NotReachable
                ? new Color(.5f, .55f, .65f) : new Color(.12f, .92f, .91f);
        }
    }

    /// <summary>
    /// Punto de entrada del juego. Construye cámara, interfaz y menú principal por código,
    /// así la escena Prototype.unity solo necesita un objeto con este componente.
    /// (Se llama PrototypeGame por compatibilidad con la escena ya creada.)
    /// </summary>
    public class PrototypeGame : MonoBehaviour
    {
        GameContext ctx;
        readonly Dictionary<string, Sprite> menuCardSprites = new Dictionary<string, Sprite>();

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
            var world = ctx.NewWorld("Lobby");

            var panel = ctx.Menus.Open(null, null, 0.35f);
            var background = LoadMenuSprite("main-background");
            if (background != null) ctx.Menus.SetBackground(background, 1f);
            var trainer = UIFactory.CreateRect("Personaje del lobby", panel);
            trainer.gameObject.AddComponent<LobbyTrainerPreview>().Initialize(panel, world, save, background);
            AddMainMenuProfile(panel, save);
            AddLobbyStatusBar(panel);

            bool duel = Progression.DuelUnlocked(save);
            bool moba = Progression.MobaUnlocked(save);
            ImageCard(panel, "aventura", 0, true, () => new AdventureMode(ctx).Enter());
            ImageCard(panel, "moba", 1, moba, () => new MobaMode(ctx).Enter());
            ImageCard(panel, "entrenamiento", 2, true, () => new PracticeMode(ctx).Enter());
            ImageCard(panel, "duelo", 3, duel, () => new DuelMode(ctx).Enter());
            AddMainMenuProfileHitArea(panel);

        }

        void AddMainMenuProfile(RectTransform panel, SaveData save)
        {
            KantoTrainerProfile.Build(panel, save, LoadMenuSprite("trainer-profile"));
        }

        void AddMainMenuProfileHitArea(RectTransform panel)
        {
            MenuScreens.Button(panel, "", new Color(1f, 1f, 1f, 0f), new Vector2(0f, 1f),
                KantoTrainerProfile.Position, KantoTrainerProfile.Size, () => new AdventureMode(ctx).OpenCustomization(), 1);
        }

        void AddLobbyStatusBar(RectTransform panel)
        {
            const string frameKey = "lobby-trapezoid";
            if (!menuCardSprites.TryGetValue(frameKey, out var frame))
            {
                frame = CreateLobbyBarSprite();
                menuCardSprites[frameKey] = frame;
            }
            var rim = UIFactory.CreateImage("Barra superior del lobby", panel, Color.white, frame);
            rim.rectTransform.Place(new Vector2(1f, 1f), Vector2.zero, new Vector2(532f, 76f));
            rim.raycastTarget = false;
            string[] names = { "Correo", "Amigos", "Ajustes", "Conexión de red" };
            for (int i = 0; i < names.Length; i++)
            {
                string key = "lobby-icon-" + i;
                if (!menuCardSprites.TryGetValue(key, out var sprite))
                {
                    sprite = CreateLobbyIcon(i);
                    menuCardSprites[key] = sprite;
                }
                var icon = UIFactory.CreateImage(names[i], rim.transform,
                    i == 3 ? new Color(.12f, .92f, .91f) : new Color(.93f, .97f, 1f), sprite);
                icon.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(-174f + i * 116f, 0), new Vector2(38, 38));
                icon.raycastTarget = false;
                if (i == 3) icon.gameObject.AddComponent<LobbyNetworkIndicator>();
                if (i < 3)
                {
                    var separator = UIFactory.CreateImage("Separador", rim.transform, new Color(.15f, .45f, .75f, .25f));
                    separator.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(-116f + i * 116f, 0), new Vector2(1, 28));
                    separator.raycastTarget = false;
                }
            }
        }

        static Sprite CreateLobbyBarSprite()
        {
            const int width = 1064, height = 152;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barra trapezoidal azul",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float slant = (x - (height - 1 - y) * .62f) / 1.1766f;
                float edge = Mathf.Min(slant, y);
                float glow = Mathf.Exp(-Mathf.Max(0, edge) / 13f);
                Color color = Color.Lerp(new Color(.012f, .045f, .13f), new Color(.015f, .15f, .30f), (float)x / width);
                color = Color.Lerp(color, new Color(.02f, .4f, .72f), glow * .55f);
                if (edge < 3.5f) color = new Color(.05f, .72f, 1f);
                else if (slant > 15 && slant < 17 && y > 13) color = new Color(.025f, .24f, .45f);
                color.a = Mathf.Clamp01(edge + .5f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100);
        }

        static Sprite CreateLobbyIcon(int kind)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Lobby icon " + kind, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                float distance = 1;
                if (kind == 0)
                {
                    distance = Mathf.Abs(Mathf.Max(Mathf.Abs(p.x) - .75f, Mathf.Abs(p.y) - .49f)) - .045f;
                    distance = Mathf.Min(distance, SegmentDistance(p, new Vector2(-.72f, .46f), new Vector2(0, -.12f)) - .04f);
                    distance = Mathf.Min(distance, SegmentDistance(p, new Vector2(.72f, .46f), new Vector2(0, -.12f)) - .04f);
                    distance = Mathf.Min(distance, SegmentDistance(p, new Vector2(-.72f, -.46f), new Vector2(-.21f, -.03f)) - .025f);
                    distance = Mathf.Min(distance, SegmentDistance(p, new Vector2(.72f, -.46f), new Vector2(.21f, -.03f)) - .025f);
                }
                else if (kind == 1)
                {
                    distance = Mathf.Min(Vector2.Distance(p, new Vector2(-.32f, .38f)) - .25f, Vector2.Distance(p, new Vector2(.35f, .33f)) - .22f);
                    if (p.y < -.02f && p.y > -.62f)
                    {
                        var a = new Vector2((p.x + .32f) / .43f, (p.y + .48f) / .45f);
                        var b = new Vector2((p.x - .35f) / .37f, (p.y + .48f) / .41f);
                        distance = Mathf.Min(distance, (Mathf.Min(a.magnitude, b.magnitude) - 1) * .35f);
                    }
                }
                else if (kind == 2)
                {
                    float radius = p.magnitude;
                    float teeth = Mathf.Cos(Mathf.Atan2(p.y, p.x) * 8);
                    float outer = teeth > .25f ? .78f : .60f;
                    distance = Mathf.Max(radius - outer, .27f - radius);
                }
                else
                {
                    var q = p - new Vector2(0, -.65f);
                    float radius = q.magnitude;
                    if (q.y > Mathf.Abs(q.x))
                        distance = Mathf.Min(Mathf.Abs(radius - 1.35f), Mathf.Min(Mathf.Abs(radius - .95f), Mathf.Abs(radius - .55f))) - .055f;
                    distance = Mathf.Min(distance, Vector2.Distance(p, new Vector2(0, -.57f)) - .105f);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(.5f - distance * size * .5f)));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var d = b - a;
            return (p - a - d * Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude)).magnitude;
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
            var button = MenuScreens.Button(panel, $"{title}\n<size=18>{description}</size>", color, new Vector2(1f, .5f),
                Vector2.zero, new Vector2(320f, 188f), onClick, 30, enabled);
            PlaceModeCard((RectTransform)button.transform, index);
            var background = button.GetComponent<Image>();
            background.sprite = UIFactory.RoundedRectangle;
            background.type = Image.Type.Sliced;
        }

        static void PlaceModeCard(RectTransform card, int index)
        {
            // Compact banners in one right-hand column, ordered from top to bottom.
            card.Place(new Vector2(1f, .5f),
                new Vector2(-32f, 242f - index * 188f),
                new Vector2(500f, 174f));
        }

        void ImageCard(RectTransform panel, string key, int index, bool enabled, System.Action onClick)
        {
            var sprite = LoadMenuSprite(key);
            if (sprite == null)
            {
                Card(panel, index, key, "Modo de juego", new Color(.25f, .35f, .5f), enabled, onClick);
                return;
            }

            var card = UIFactory.CreateImage("Tarjeta " + key, panel, Color.white, UIFactory.RoundedRectangle);
            card.type = Image.Type.Sliced;
            card.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            PlaceModeCard(card.rectTransform, index);

            string artworkKey = key + "-artwork";
            if (!menuCardSprites.TryGetValue(artworkKey, out var artwork))
            {
                // Use the illustration above the baked-in caption; labels are rendered at UI resolution.
                var source = sprite.rect;
                artwork = Sprite.Create(sprite.texture,
                    new Rect(source.x, source.y + source.height * .32f, source.width, source.height * .58f),
                    new Vector2(.5f, .5f), 100f);
                menuCardSprites[artworkKey] = artwork;
            }
            var image = UIFactory.CreateImage("Ilustración " + key, card.transform,
                enabled ? Color.white : new Color(.5f, .5f, .5f, .65f), artwork);
            image.rectTransform.Stretch();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectRatio = artwork.rect.width / artwork.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            string title;
            string description;
            Color accent;
            switch (key)
            {
                case "aventura": title = "AVENTURA"; description = "Explora, combate y descubre nuevos Pokémon."; accent = new Color(.06f, .85f, .48f); break;
                case "moba": title = "MOBA 3 vs 3"; description = "Combate en equipo en batallas 3 contra 3."; accent = new Color(.08f, .5f, 1f); break;
                case "entrenamiento": title = "ENTRENAMIENTO"; description = "Practica tus movimientos y mejora tu equipo."; accent = new Color(1f, .7f, .08f); break;
                default: title = "DUELO 1 vs 1"; description = "Desafía a otros entrenadores en combates 1 vs 1."; accent = new Color(1f, .12f, .2f); break;
            }
            var footer = UIFactory.CreateImage("Rótulo nítido", card.transform,
                Color.Lerp(new Color(.015f, .025f, .05f, .97f), new Color(accent.r, accent.g, accent.b, .97f), .22f));
            footer.rectTransform.Place(new Vector2(0f, 0f), Vector2.zero, new Vector2(500f, 72f));
            footer.raycastTarget = false;
            var line = UIFactory.CreateImage("Acento", footer.transform, accent);
            line.rectTransform.Place(new Vector2(0f, 1f), Vector2.zero, new Vector2(500f, 2f));
            line.raycastTarget = false;
            AddModeEmblem(footer.rectTransform, key, sprite, enabled);
            var label = UIFactory.CreateText("Título del modo", footer.transform, title, 28,
                enabled ? Color.white : new Color(.7f, .7f, .7f), TextAnchor.MiddleLeft);
            label.rectTransform.Place(new Vector2(0f, 1f), new Vector2(94f, -4f), new Vector2(350f, 32f));
            label.GetComponent<Outline>().effectDistance = new Vector2(.75f, -.75f);
            var subtitle = UIFactory.CreateText("Descripción del modo", footer.transform, description, 16,
                enabled ? new Color(.93f, .95f, 1f) : new Color(.65f, .65f, .65f), TextAnchor.MiddleLeft);
            subtitle.fontStyle = FontStyle.Normal;
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Truncate;
            subtitle.rectTransform.Place(new Vector2(0f, 0f), new Vector2(94f, 5f), new Vector2(350f, 32f));
            subtitle.GetComponent<Outline>().effectDistance = new Vector2(.5f, -.5f);
            var arrow = UIFactory.CreateText("Abrir modo", footer.transform, "›", 48, Color.white, TextAnchor.MiddleCenter);
            arrow.rectTransform.Place(new Vector2(1f, .5f), new Vector2(-14f, 0f), new Vector2(32f, 48f));
            AddModeCardNeon(panel, card.rectTransform, key, accent, enabled);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            button.onClick.AddListener(() => onClick());
        }

        void AddModeEmblem(RectTransform footer, string key, Sprite source, bool enabled)
        {
            string cacheKey = key + "-emblem";
            if (!menuCardSprites.TryGetValue(cacheKey, out var emblem))
            {
                // Reuse the original circular emblems without bringing back the baked-in lettering.
                Rect crop;
                switch (key)
                {
                    case "aventura": crop = new Rect(.038f, .093f, .167f, .25f); break;
                    case "moba": crop = new Rect(.031f, .113f, .13f, .23f); break;
                    case "entrenamiento": crop = new Rect(.0395f, .094f, .141f, .251f); break;
                    default: crop = new Rect(.037f, .097f, .141f, .251f); break;
                }
                var rect = source.rect;
                emblem = Sprite.Create(source.texture,
                    new Rect(rect.x + crop.x * rect.width, rect.y + crop.y * rect.height,
                        crop.width * rect.width, crop.height * rect.height), new Vector2(.5f, .5f), 100);
                menuCardSprites[cacheKey] = emblem;
            }
            var window = UIFactory.CreateImage("Emblema circular " + key, footer, Color.white, UIFactory.Circle);
            window.rectTransform.Place(new Vector2(0f, 0f), new Vector2(12, 5), new Vector2(72, 72));
            window.raycastTarget = false;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var icon = UIFactory.CreateImage("Icono original " + key, window.transform,
                enabled ? Color.white : new Color(.6f, .6f, .6f, .65f), emblem);
            icon.rectTransform.Stretch();
            icon.raycastTarget = false;
        }

        void AddModeCardNeon(RectTransform panel, RectTransform card, string key, Color accent, bool enabled)
        {
            string cacheKey = key + "-neon-frame";
            if (!menuCardSprites.TryGetValue(cacheKey, out var sprite))
            {
                // Render outside the card's mask so the halo can extend beyond all four corners.
                const int width = 1048, height = 396;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Neón " + key,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float px = Mathf.Abs((x + .5f - width * .5f) * .5f);
                    float py = Mathf.Abs((y + .5f - height * .5f) * .5f);
                    float qx = px - 230f, qy = py - 67f;
                    float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                    float distance = Mathf.Abs(outside + Mathf.Min(Mathf.Max(qx, qy), 0) - 20f + 1.5f);
                    float core = Mathf.Clamp01(1.4f - distance);
                    float halo = .48f * Mathf.Exp(-distance * distance / 22f);
                    var color = Color.Lerp(accent, Color.white, core * .65f);
                    color.a = Mathf.Clamp01(core * .95f + halo);
                    pixels[y * width + x] = color;
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100);
                menuCardSprites[cacheKey] = sprite;
            }
            var neon = UIFactory.CreateImage("Borde de neón " + key, panel,
                enabled ? Color.white : new Color(.65f, .65f, .65f, .5f), sprite);
            neon.rectTransform.Place(card.anchorMin, card.anchoredPosition + new Vector2(12, 0), card.sizeDelta + new Vector2(24, 24));
            neon.raycastTarget = false;
        }

        Sprite LoadMenuSprite(string key)
        {
            if (menuCardSprites.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>("ModeCards/" + key);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f);
            menuCardSprites[key] = sprite;
            return sprite;
        }

        Sprite LoadAshProfileSprite()
        {
            const string key = "ash-profile";
            if (menuCardSprites.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>("ModeCards/ash-profile-sheet");
            if (texture == null) return null;

            // Bottom-left portrait from the downloaded 4x2 Ash portrait sheet.
            var crop = new Rect(16f, 50f, 410f, 420f);
            var sprite = Sprite.Create(texture, crop, new Vector2(.5f, .5f), 100f);
            menuCardSprites[key] = sprite;
            return sprite;
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
