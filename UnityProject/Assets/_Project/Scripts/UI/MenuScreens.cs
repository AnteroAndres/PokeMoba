using System;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Pantallas de menú a pantalla completa (fondo oscuro, título y contenido). Solo hay una abierta a la vez.</summary>
    public class MenuScreens
    {
        readonly Transform canvas;
        GameObject current;

        public MenuScreens(Transform canvas) => this.canvas = canvas;

        public static bool AnyOpen { get; private set; }

        public bool IsOpen => current != null;

        public RectTransform Open(string title, string subtitle = null, float dim = 0.7f)
        {
            Close();
            AnyOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var overlay = UIFactory.CreateImage("Menú", canvas, new Color(0.02f, 0.03f, 0.08f, dim));
            overlay.rectTransform.Stretch();
            current = overlay.gameObject;

            if (!string.IsNullOrEmpty(title))
            {
                var titleText = UIFactory.CreateText("Título", overlay.transform, title, 68, Color.white, TextAnchor.MiddleCenter);
                titleText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 100f));
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = Label(overlay.rectTransform, subtitle, 30, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1600f, 50f));
                sub.color = new Color(0.85f, 0.88f, 1f);
            }
            return overlay.rectTransform;
        }

        public void Close()
        {
            if (current != null) UnityEngine.Object.Destroy(current);
            current = null;
            AnyOpen = false;
        }

        public void SetBackground(Sprite sprite, float opacity = 0.78f)
        {
            if (current == null || sprite == null) return;

            var overlay = current.GetComponent<Image>();
            overlay.sprite = null;
            overlay.color = Color.black;
            overlay.raycastTarget = false;

            var existing = current.transform.Find("Fondo del lobby");
            var image = existing != null ? existing.GetComponent<Image>()
                : UIFactory.CreateImage("Fondo del lobby", current.transform, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            image.raycastTarget = false;
            image.transform.SetAsFirstSibling();

            // Cover the viewport without stretching; wider/narrower screens crop the edges.
            var fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        public static Button Button(RectTransform panel, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size,
            Action onClick, int fontSize = 34, bool interactable = true)
        {
            var button = UIFactory.CreateButton(label, panel, label, fontSize, interactable ? color : Color.Lerp(color, Color.gray, 0.7f), onClick);
            button.interactable = interactable;
            var text = button.GetComponentInChildren<Text>();
            text.supportRichText = true;
            ((RectTransform)button.transform).Place(anchor, position, size);
            return button;
        }

        public static Text Label(RectTransform panel, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = UIFactory.CreateText("Texto", panel, text, fontSize, Color.white, alignment);
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.rectTransform.Place(anchor, position, size);
            return label;
        }

        /// <summary>Panel de color dentro de un menú (tarjetas, fichas).</summary>
        public static RectTransform Card(RectTransform panel, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = UIFactory.CreateImage("Tarjeta", panel, color);
            image.raycastTarget = false;
            return image.rectTransform.Place(anchor, position, size);
        }
    }
}
