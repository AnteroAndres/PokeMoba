using System;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Ayudas para construir la interfaz por código (el prototipo no usa prefabs).</summary>
    public static class UIFactory
    {
        static Sprite circle;
        static Sprite roundedRectangle;
        static Font font;

        public static Sprite Circle => circle != null ? circle : (circle = CreateCircleSprite(128));
        public static Sprite RoundedRectangle => roundedRectangle != null
            ? roundedRectangle : (roundedRectangle = CreateRoundedRectangleSprite());
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Coloca un rect anclado a un punto (anchor = pivot) con posición y tamaño.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, Color color, Action onClick)
        {
            var image = CreateImage(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            CreateText("Texto", image.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter).rectTransform.Stretch();
            return button;
        }

        /// <summary>Campo de texto de una línea (en el celular abre el teclado).</summary>
        public static InputField CreateInputField(string name, Transform parent, string value, string placeholder, int fontSize)
        {
            var background = CreateImage(name, parent, new Color(1f, 1f, 1f, 0.92f));
            var input = background.gameObject.AddComponent<InputField>();

            var text = CreateText("Texto", background.transform, "", fontSize, new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleLeft);
            Destroy(text.GetComponent<Outline>());
            text.supportRichText = false;
            text.rectTransform.Stretch();
            text.rectTransform.offsetMin = new Vector2(16f, 0f);
            text.rectTransform.offsetMax = new Vector2(-16f, 0f);

            var hint = CreateText("Sugerencia", background.transform, placeholder, fontSize, new Color(0.4f, 0.4f, 0.45f), TextAnchor.MiddleLeft);
            Destroy(hint.GetComponent<Outline>());
            hint.fontStyle = FontStyle.Italic;
            hint.rectTransform.Stretch();
            hint.rectTransform.offsetMin = new Vector2(16f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-16f, 0f);

            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = background;
            input.text = value;
            return input;
        }

        static void Destroy(UnityEngine.Object obj)
        {
            if (obj != null) UnityEngine.Object.Destroy(obj);
        }

        /// <summary>Barra horizontal: devuelve el rect del relleno, que se ajusta con SetBarFill.</summary>
        public static RectTransform CreateBar(string name, Transform parent, Color background, Color fill)
        {
            var bg = CreateImage(name, parent, background);
            var fillImage = CreateImage("Relleno", bg.transform, fill);
            var fillRect = fillImage.rectTransform.Stretch();
            fillRect.pivot = new Vector2(0f, 0.5f);
            return fillRect;
        }

        public static void SetBarFill(RectTransform fill, float fraction)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        static Sprite CreateRoundedRectangleSprite()
        {
            const int size = 64;
            const float radius = 20f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Rounded UI rectangle",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius), 0f);
                byte alpha = (byte)(255 * Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float radius = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    byte alpha = (byte)(Mathf.Clamp01(radius - d) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
