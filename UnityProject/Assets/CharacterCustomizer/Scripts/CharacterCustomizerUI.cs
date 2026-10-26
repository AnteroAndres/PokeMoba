using System;
using System.Collections.Generic;
using PokeMoba.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CharacterCustomizer
{
    /// <summary>Runtime uGUI menu. Attach to an empty GameObject, then assign the existing 3D character in Inspector.</summary>
    public sealed class CharacterCustomizerUI : MonoBehaviour
    {
        public bool BuildOnAwake = true;

        [Header("Optional existing character")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Renderer skinRenderer;
        [SerializeField] private Renderer hairRenderer;
        [SerializeField] private string skinColorProperty = "_BaseColor";
        [SerializeField] private string hairColorProperty = "_BaseColor";
        [SerializeField] private GameObject[] hairstyles;
        [Header("Actions")]
        public UnityEvent onSave;
        public UnityEvent onCancel;

        private readonly Color[] skin = { C("#F6D2AE"), C("#E8B98F"), C("#D89A6C"), C("#BB7957"), C("#905E49"), C("#65453B") };
        private readonly Color[] hair = { Color.white, C("#828B95"), C("#272D35"), C("#8D5835"), C("#FFD05B"), C("#D86D35"), C("#E53C39"), C("#63304B") };
        private Font font;
        private RectTransform panel;
        private Text sectionTitle;
        private RectTransform options;
        private string category = "Apariencia";
        private int selectedHair;
        private int selectedSkin;
        private int selectedHairColor;
        private string selectedRole = "Sabio";
        private string selectedCharacter = "Procedural";
        private Action<string> characterChanged;
        private RectTransform characterPicker;
        private Text characterChoiceText;
        readonly Dictionary<string, Sprite> characterIcons = new Dictionary<string, Sprite>();
        private Vector2 lastPointer;
        private bool dragging;

        static readonly string[] Characters =
        {
            "Procedural", "tr0037_Akari/Akari", "tr0036_Rei/Rei", "tr0003_Adaman/Adaman",
            "tr0004_Irida/Irida", "tr0002_Laventon/Laventon", "tr0005_Kamado/Kamado1",
            "tr0006_Komado/Komado_Samurai", "tr0007_Volo/Volo", "tr0008_Volo/Volo2",
            "tr0011_Cogita/Cogita", "tr0018_Ingo/Ingo", "tr0025_Ginter/Ginter",
            "tr0033_Charm/Charm", "tr0034_Clover/Clover", "tr0035_Coin/Coin"
        };

        public int SelectedSkin => selectedSkin;
        public int SelectedHair => selectedHair;
        public int SelectedHairColor => selectedHairColor;
        public string SelectedRole => selectedRole;
        public string SelectedCharacter => selectedCharacter;

        public void SetCharacterRoot(Transform character) => characterRoot = character;

        public void OpenCharacterPicker() => ShowCharacterPicker();

        private static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        private static RectTransform Rect(GameObject go, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = offMin; r.offsetMax = offMax;
            return r;
        }
        private static Image Image(GameObject go, Color color) { var i = go.AddComponent<Image>(); i.color = color; return i; }
        private Text Label(Transform parent, string value, int size, Color color, TextAnchor align, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject("Text " + value); Rect(go, parent, min, max, offMin, offMax);
            var t = go.AddComponent<Text>();
            t.font = font != null ? font : UIFactory.Font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.resizeTextForBestFit = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
        private Button Button(Transform parent, string text, Color bg, Action action, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, int size = 19)
        {
            var go = new GameObject(text); Rect(go, parent, min, max, offMin, offMax);
            Image(go, bg); var b = go.AddComponent<Button>(); b.targetGraphic = go.GetComponent<Image>(); b.onClick.AddListener(() => action?.Invoke());
            Label(go.transform, text, size, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return b;
        }
        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (BuildOnAwake) Build();
        }

        public void Initialize(Transform character, int skin, int hairStyle, int hairColor, string role,
            string characterKey, Action save, Action cancel, Action<string> onCharacterChanged = null)
        {
            characterRoot = character;
            selectedSkin = Mathf.Max(0, skin);
            selectedHair = Mathf.Max(0, hairStyle);
            selectedHairColor = Mathf.Max(0, hairColor);
            selectedRole = string.IsNullOrEmpty(role) ? "Explorador" : role;
            selectedCharacter = string.IsNullOrEmpty(characterKey) ? "Procedural" : characterKey;
            characterChanged = onCharacterChanged;
            onSave = new UnityEvent();
            onCancel = new UnityEvent();
            if (save != null) onSave.AddListener(() => save());
            if (cancel != null) onCancel.AddListener(() => cancel());
            if (panel != null)
            {
                Destroy(panel.transform.root.gameObject);
                panel = null;
            }
            Build();
        }

        static string CharacterLabel(string key)
        {
            if (key == "Procedural") return "Roshi";
            int slash = key.IndexOf('/');
            return slash >= 0 ? key.Substring(slash + 1) : key;
        }
        private void Build()
        {
            if (panel != null) return;
            var root = new GameObject("Customizer Canvas"); root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            // Transparent central/right area: existing Unity 3D scene remains visible.
            var header = new GameObject("Header"); Rect(header, root.transform, new Vector2(0, .86f), Vector2.one, Vector2.zero, Vector2.zero);
            Image(header, C("#16365CAA"));
            Label(header.transform, "Tu personaje", 36, Color.white, TextAnchor.MiddleLeft, new Vector2(.33f, .42f), new Vector2(.72f, 1), Vector2.zero, Vector2.zero);
            Label(header.transform, "Personaliza tu entrenador", 17, C("#DCE8F8"), TextAnchor.MiddleLeft, new Vector2(.33f, 0), new Vector2(.75f, .45f), Vector2.zero, Vector2.zero);
            Button(header.transform, "‹", C("#16385C"), () => onCancel?.Invoke(), Vector2.zero, new Vector2(.075f, 1), new Vector2(8, 8), new Vector2(-8, -8), 34);

            var profileTexture = Resources.Load<Texture2D>("ModeCards/trainer-profile");
            if (profileTexture != null)
            {
                var profile = new GameObject("Trainer Profile");
                Rect(profile, root.transform, new Vector2(.08f, .86f), new Vector2(.30f, 1f), Vector2.zero, Vector2.zero);
                var profileImage = Image(profile, Color.white);
                profileImage.sprite = Sprite.Create(profileTexture, new Rect(0f, 0f, profileTexture.width, profileTexture.height),
                    new Vector2(.5f, .5f), 100f);
                profileImage.type = UnityEngine.UI.Image.Type.Simple;
                profileImage.preserveAspect = true;

                var profileTextCover = new GameObject("Profile Text Background");
                Rect(profileTextCover, root.transform, new Vector2(.17f, .86f), new Vector2(.30f, 1f), Vector2.zero, Vector2.zero);
                Image(profileTextCover, C("#16365CEE"));
                Label(root.transform, "Usuario 1", 17, Color.white, TextAnchor.MiddleLeft,
                    new Vector2(.18f, .92f), new Vector2(.30f, 1f), Vector2.zero, Vector2.zero);
                Label(root.transform, "Nv. 1", 15, Color.white, TextAnchor.MiddleLeft,
                    new Vector2(.18f, .86f), new Vector2(.30f, .93f), Vector2.zero, Vector2.zero);
            }
            characterChoiceText = Label(root.transform, CharacterLabel(selectedCharacter), 18, Color.white, TextAnchor.MiddleCenter,
                new Vector2(.70f, .68f), new Vector2(.98f, .78f), Vector2.zero, Vector2.zero);
            var characterChoice = Button(root.transform, "", new Color(1f, 1f, 1f, 0f), ShowCharacterPicker,
                new Vector2(.70f, .66f), new Vector2(.98f, .80f), Vector2.zero, Vector2.zero, 1);
            characterChoice.gameObject.name = "Character Choice";
            var nav = new GameObject("Navigation"); Rect(nav, root.transform, new Vector2(.018f, .16f), new Vector2(.16f, .75f), Vector2.zero, Vector2.zero); Image(nav, C("#132E4CD9"));
            string[] categories = { "Apariencia", "Cabeza", "Ropa", "Accesorios", "Vista previa" };
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i]; float top = 1 - i * .195f;
                Button(nav.transform, cat, i == 0 ? C("#385CD9") : C("#1E385B"), () => { category = cat; RenderOptions(); }, new Vector2(0, top - .18f), new Vector2(1, top - .01f), new Vector2(5, 0), new Vector2(-5, 0), 17);
            }
            var optionsGo = new GameObject("Options Panel"); panel = Rect(optionsGo, root.transform, new Vector2(.17f, .16f), new Vector2(.54f, .75f), Vector2.zero, Vector2.zero); Image(optionsGo, C("#193A5CE8"));
            sectionTitle = Label(panel, "", 21, Color.white, TextAnchor.MiddleLeft, new Vector2(.05f, .88f), new Vector2(.95f, .99f), Vector2.zero, Vector2.zero);
            var optionGo = new GameObject("Dynamic Options"); options = Rect(optionGo, panel, new Vector2(.04f, .06f), new Vector2(.96f, .86f), Vector2.zero, Vector2.zero);
            Button(root.transform, "Cancelar", C("#233E5B"), () => onCancel?.Invoke(), new Vector2(.02f, .025f), new Vector2(.18f, .12f), Vector2.zero, Vector2.zero);
            Button(root.transform, "Guardar apariencia", C("#DCA71D"), Save, new Vector2(.71f, .025f), new Vector2(.98f, .12f), Vector2.zero, Vector2.zero, 21);

            var dragGo = new GameObject("Character Drag Surface");
            Rect(dragGo, root.transform, new Vector2(.54f, .14f), new Vector2(1f, .86f), Vector2.zero, Vector2.zero);
            Image(dragGo, new Color(0f, 0f, 0f, 0f)).raycastTarget = true;
            dragGo.AddComponent<DragSurface>().OnRotate = Rotate;
            RenderOptions();
        }

        private void ShowCharacterPicker()
        {
            if (characterPicker != null) Destroy(characterPicker.gameObject);

            var pickerObject = new GameObject("Character Picker");
            characterPicker = Rect(pickerObject, transform.GetChild(0), new Vector2(.02f, .03f), new Vector2(.98f, .96f), Vector2.zero, Vector2.zero);
            Image(pickerObject, C("#0B1D32FA"));
            Label(characterPicker, "Elige tu personaje", 30, Color.white, TextAnchor.MiddleCenter,
                new Vector2(.04f, .90f), new Vector2(.96f, .99f), Vector2.zero, Vector2.zero);

            for (int i = 0; i < Characters.Length; i++)
            {
                int index = i;
                int column = i % 4;
                int row = i / 4;
                float x = .04f + column * .235f;
                float top = .84f - row * .18f;
                Label(characterPicker, CharacterLabel(Characters[i]), 12, Color.white, TextAnchor.MiddleCenter,
                    new Vector2(x, top - .03f), new Vector2(x + .215f, top + .08f), Vector2.zero, Vector2.zero);
                var button = Button(characterPicker, "",
                    Characters[i] == selectedCharacter ? C("#3189BF") : C("#24466E"),
                    () => SelectCharacter(Characters[index]), new Vector2(x, top - .18f),
                    new Vector2(x + .215f, top - .03f), Vector2.zero, Vector2.zero, 1);
                button.interactable = true;
                SetCharacterIcon(button, Characters[i]);
            }

            Button(characterPicker, "Cancelar", C("#3B4A5C"), () =>
            {
                Destroy(characterPicker.gameObject);
                characterPicker = null;
            }, new Vector2(.40f, .02f), new Vector2(.60f, .12f), Vector2.zero, Vector2.zero, 16);
        }

        private void SelectCharacter(string key)
        {
            selectedCharacter = key;
            if (characterChoiceText != null) characterChoiceText.text = CharacterLabel(key);
            characterChanged?.Invoke(key);
            if (characterPicker != null)
            {
                Destroy(characterPicker.gameObject);
                characterPicker = null;
            }
        }

        void SetCharacterIcon(Button button, string key)
        {
            if (key == "Procedural") return;
            var texture = Resources.Load<Texture2D>("Models/Trainers/Icons/" + key);
            if (texture == null) return;
            if (!characterIcons.TryGetValue(key, out var sprite))
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f), 100f);
                characterIcons[key] = sprite;
            }
            var image = button.GetComponent<Image>();
            image.color = Color.white;
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = 11;
        }
        private void RenderOptions()
        {
            sectionTitle.text = category;
            for (int i = options.childCount - 1; i >= 0; i--) Destroy(options.GetChild(i).gameObject);
            if (category == "Apariencia" || category == "Cabeza")
            {
                Swatches("Piel", skin, .73f, i => { selectedSkin = i; Apply(skinRenderer, skinColorProperty, skin[i]); });
                Swatches("Color de cabello", hair, .43f, i => { selectedHairColor = i; Apply(hairRenderer, hairColorProperty, hair[i]); });
                Label(options, "Peinado", 19, Color.white, TextAnchor.MiddleLeft, new Vector2(0, .2f), new Vector2(1, .3f), Vector2.zero, Vector2.zero);
                for (int i = 0; i < 4; i++)
                {
                    int index = i;
                    Button(options, "0" + (i + 1), i == selectedHair ? C("#318EDB") : C("#426182"), () => SelectHair(index), new Vector2(i * .19f, 0), new Vector2(i * .19f + .17f, .19f), Vector2.zero, Vector2.zero, 17);
                }
            }
            else
            {
                Label(options, category == "Vista previa" ? "Gira el personaje para revisar los cambios." : "Conecta aquí tus modelos y variantes de " + category.ToLower() + ".", 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }
        private void Swatches(string name, Color[] colors, float y, Action<int> onSelect)
        {
            Label(options, name, 18, Color.white, TextAnchor.MiddleLeft, new Vector2(0, y + .15f), new Vector2(1, y + .27f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < colors.Length; i++)
            {
                int index = i; var b = Button(options, "", colors[i], () => onSelect(index), new Vector2(i * (1f / colors.Length) + .007f, y), new Vector2((i + 1f) / colors.Length - .015f, y + .14f), Vector2.zero, Vector2.zero);
                var outline = b.gameObject.AddComponent<Outline>(); outline.effectColor = C("#E9F4FF"); outline.effectDistance = new Vector2(2, -2);
            }
        }
        private void Apply(Renderer renderer, string property, Color color)
        {
            if (!renderer) return;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            string key = renderer.sharedMaterial && renderer.sharedMaterial.HasProperty(property) ? property : "_Color";
            block.SetColor(key, color); renderer.SetPropertyBlock(block);
        }
        private void SelectHair(int i)
        {
            selectedHair = i;
            if (hairstyles != null)
                for (int j = 0; j < hairstyles.Length; j++) if (hairstyles[j]) hairstyles[j].SetActive(j == i);
            RenderOptions();
        }
        private void Rotate(float degrees) { if (characterRoot) characterRoot.Rotate(Vector3.up, degrees, Space.World); }
        private void Save()
        {
            PlayerPrefs.SetInt("Customizer.Skin", selectedSkin);
            PlayerPrefs.SetInt("Customizer.HairColor", selectedHairColor);
            PlayerPrefs.SetInt("Customizer.Hair", selectedHair);
            PlayerPrefs.SetString("Customizer.Role", selectedRole);
            PlayerPrefs.Save(); onSave?.Invoke();
        }

        private sealed class DragSurface : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            public Action<float> OnRotate;
            Vector2 last;

            public void OnBeginDrag(PointerEventData eventData) => last = eventData.position;

            public void OnDrag(PointerEventData eventData)
            {
                float delta = eventData.position.x - last.x;
                last = eventData.position;
                OnRotate?.Invoke(-delta * .45f);
            }
        }
    }
}
