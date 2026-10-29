using System.Collections.Generic;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Illustrated banner with live trainer data and Kanto level themes.</summary>
    public static class KantoTrainerProfile
    {
        public static readonly Vector2 Position = new Vector2(20, -20);
        public static readonly Vector2 Size = new Vector2(640, 216);
        static readonly Dictionary<int, Material> themes = new Dictionary<int, Material>();
        static Sprite frame, squarePortrait, selectedPortrait, pill;
        static readonly Dictionary<int, Sprite> pokemonArt = new Dictionary<int, Sprite>();

        public static int DexForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 151);
            return level <= 3 ? level + 3 : level <= 6 ? level - 3 : level;
        }

        public static void Build(RectTransform panel, SaveData save, Sprite portrait)
        {
            if (!string.IsNullOrEmpty(save.ProfilePortraitResource))
            {
                var photo = Resources.Load<Texture2D>(save.ProfilePortraitResource);
                if (photo != null && (selectedPortrait == null || selectedPortrait.texture != photo))
                    selectedPortrait = Sprite.Create(photo, new Rect(0, 0, photo.width, photo.height), new Vector2(.5f, .5f), 100);
                if (photo != null) portrait = selectedPortrait;
            }
            int level = Mathf.Clamp(save.TrainerLevel, 1, 151);
            int dex = DexForLevel(level);
            var species = PokemonDatabase.Get(dex);
            var type = species != null && species.Types != null && species.Types.Length > 0
                ? species.Types[0] : PokemonType.Fuego;
            Color accent = type.TypeColor();
            string name = species != null ? species.Name : "Charmander";
            var card = UIFactory.CreateRect("Perfil Kanto · " + name, panel).Place(new Vector2(0, 1), Position, Size);

            // Square crop and explicit square geometry prevent a narrow or squeezed portrait.
            var window = AddImage(card, "Retrato circular", Color.white, UIFactory.Circle, 40, 28, 147, 147);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            if (portrait != null)
            {
                if (squarePortrait == null || squarePortrait.texture != portrait.texture)
                {
                    var rect = portrait.rect;
                    float side = Mathf.Min(rect.width, rect.height);
                    squarePortrait = Sprite.Create(portrait.texture,
                        new Rect(rect.x + (rect.width - side) * .5f, rect.y + (rect.height - side) * .5f, side, side),
                        new Vector2(.5f, .5f), 100);
                }
                AddImage(window.transform, "Entrenador", Color.white, squarePortrait, 0, 0, 147, 147).preserveAspect = false;
            }

            if (frame == null)
            {
                var texture = Resources.Load<Texture2D>("ModeCards/kanto-profile-frame");
                if (texture != null)
                    frame = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            }
            var border = AddImage(card, "Marco ilustrado", Color.white, frame, 0, 0, Size.x, Size.y);
            if (!themes.TryGetValue(dex, out var material))
            {
                var shader = Resources.Load<Shader>("UI/KantoProfileTheme");
                if (shader != null)
                {
                    material = new Material(shader) { name = "Perfil " + name };
                    material.SetColor("_ThemeColor", accent);
                    themes[dex] = material;
                }
            }
            if (material != null) border.material = material;
            if (!pokemonArt.TryGetValue(dex, out var artwork))
            {
                var texture = Resources.Load<Texture2D>($"ProfilePokemon/{dex:000}");
                if (texture != null)
                {
                    artwork = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                    pokemonArt[dex] = artwork;
                }
            }
            if (artwork != null)
                AddImage(card, "Pokémon del perfil · " + name, Color.white, artwork, 506, 56, 103, 103).preserveAspect = true;

            Label(card, save.ProfileDisplayName,
                30, 228, 44, 275, 34, Color.white);
            Label(card, $"Nivel {level}", 22, 228, 77, 150, 27, Color.Lerp(accent, Color.white, .35f));
            Label(card, $"{name} · {type.DisplayName()}", 14, 366, 80, 140, 22, Color.Lerp(accent, Color.white, .35f));
            AddImage(card, "Borde de experiencia", accent, Pill(), 228, 106, 177, 15).type = Image.Type.Sliced;
            AddImage(card, "Fondo de experiencia", new Color(.025f, .025f, .04f), Pill(), 230, 108, 173, 11).type = Image.Type.Sliced;
            float progress = level == 151 ? 1f : Mathf.Clamp01((float)save.TrainerExperience / save.TrainerExperienceRequired);
            var fill = AddImage(card, "Experiencia", Color.Lerp(accent, Color.white, .18f), Pill(), 230, 108, 173 * progress, 11);
            fill.type = Image.Type.Sliced;
            fill.enabled = progress > 0;
            Label(card, level == 151 ? "MÁXIMO" : $"{save.TrainerExperience} / {save.TrainerExperienceRequired}",
                14, 416, 103, 90, 22, Color.white);
            AddImage(card, "Separador", new Color(accent.r, accent.g, accent.b, .35f), null, 228, 130, 275, 1);
            Ball(card);
            Label(card, "Capturados", 14, 266, 131, 150, 18, Color.Lerp(accent, Color.white, .45f));
            Label(card, $"{save.CaughtCount} / 151", 25, 265, 147, 150, 27, Color.white);
            Label(card, $"Medallas {save.Medals.Count}/8", 14, 450, 146, 145, 25, Color.white);
            var badge = Label(card, "Nv.\n" + level, 19, 159, 139, 48, 48, Color.white);
            badge.alignment = TextAnchor.MiddleCenter;
            badge.lineSpacing = .85f;
        }

        static Sprite Pill()
        {
            if (pill == null)
                pill = Sprite.Create(UIFactory.Circle.texture, UIFactory.Circle.rect, new Vector2(.5f, .5f),
                    100, 0, SpriteMeshType.FullRect, new Vector4(63, 63, 63, 63));
            return pill;
        }

        static void Ball(Transform parent)
        {
            Color dark = new Color(.035f, .045f, .07f);
            var outer = AddImage(parent, "Poké Ball", dark, UIFactory.Circle, 230, 141, 29, 29);
            AddImage(outer.transform, "Blanco", Color.white, UIFactory.Circle, 2.5f, 2.5f, 24, 24);
            var red = AddImage(outer.transform, "Rojo", new Color(.96f, .12f, .2f), UIFactory.Circle, 2.5f, 2.5f, 24, 24);
            red.type = Image.Type.Filled;
            red.fillMethod = Image.FillMethod.Vertical;
            red.fillOrigin = 1;
            red.fillAmount = .5f;
            AddImage(outer.transform, "Banda", dark, null, 2, 13, 25, 3);
            AddImage(outer.transform, "Centro", dark, UIFactory.Circle, 9, 9, 11, 11);
            AddImage(outer.transform, "Botón", Color.white, UIFactory.Circle, 11.5f, 11.5f, 6, 6);
        }

        static Image AddImage(Transform parent, string name, Color color, Sprite sprite, float x, float y, float width, float height)
        {
            var image = UIFactory.CreateImage(name, parent, color, sprite);
            image.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            image.raycastTarget = false;
            return image;
        }

        static Text Label(Transform parent, string value, int size, float x, float y, float width, float height, Color color)
        {
            var label = UIFactory.CreateText("Datos del perfil", parent, value, size, color, TextAnchor.MiddleLeft);
            label.supportRichText = false;
            label.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            return label;
        }
    }
}