using System;
using System.Collections.Generic;
using System.IO;
using PokeMoba.Game;
using PokeMoba.Modes;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    public class ProfileEditor
    {
        readonly GameContext ctx;
        readonly Action back;
        readonly List<Sprite> previews = new List<Sprite>();
        InputField name;
        Text message;
        Image portrait;
        string selected;
        RectTransform panel;
        readonly List<GameObject> variants = new List<GameObject>();
        static readonly string[] Portraits = { "Models/Trainers/Icons/Ash/Ash", "Models/Trainers/Icons/tr0002_Laventon/Laventon", "Models/Trainers/Icons/tr0003_Adaman/Adaman", "Models/Trainers/Icons/tr0004_Irida/Irida", "Models/Trainers/Icons/tr0005_Kamado/Kamado1", "Models/Trainers/Icons/tr0006_Komado/Komado_Samurai", "Models/Trainers/Icons/tr0007_Volo/Volo", "Models/Trainers/Icons/tr0008_Volo/Volo2", "Models/Trainers/Icons/tr0011_Cogita/Cogita", "Models/Trainers/Icons/tr0018_Ingo/Ingo", "Models/Trainers/Icons/tr0025_Ginter/Ginter", "Models/Trainers/Icons/tr0033_Charm/Charm", "Models/Trainers/Icons/tr0034_Clover/Clover", "Models/Trainers/Icons/tr0035_Coin/Coin", "Models/Trainers/Icons/tr0036_Rei/Rei", "Models/Trainers/Icons/tr0037_Akari/Akari" };
        public ProfileEditor(GameContext ctx, Action back) { this.ctx = ctx; this.back = back; }
        public void Open()
        {
            selected = ctx.Save.ProfilePortraitResource;
            panel = ctx.Menus.Open("Mi perfil", "Tu nombre y tu foto son independientes del personaje que uses.", .95f);
            var anchor = new Vector2(.5f, .5f);
            name = UIFactory.CreateInputField("Nombre propio", panel, ctx.Save.ProfileDisplayName, "Nombre de jugador", 32);
            name.characterLimit = 16;
            ((RectTransform)name.transform).Place(anchor, new Vector2(-450, 180), new Vector2(600, 80));
            message = MenuScreens.Label(panel, "3–16 letras, números o _ · sin distinguir mayúsculas.\nDisponibilidad local; todavía no hay registro online.", 23,
                anchor, new Vector2(-450, 60), new Vector2(650, 110));
            message.supportRichText = false;
            MenuScreens.Button(panel, "Comprobar disponibilidad", new Color(.25f, .45f, .65f), anchor,
                new Vector2(-450, -65), new Vector2(570, 70), () => Check(false), 27);
            var image = UIFactory.CreateImage("Foto elegida", panel, Color.white);
            portrait = image;
            image.rectTransform.Place(anchor, new Vector2(450, 225), new Vector2(130, 130));
            image.preserveAspect = true; image.raycastTarget = false;
            SetPortrait(selected);
            for (int i = 0; i < Portraits.Length; i++)
            {
                string key = Portraits[i];
                var texture = Resources.Load<Texture2D>(key);
                if (texture == null) continue;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                previews.Add(sprite);
                var button = MenuScreens.Button(panel, "", new Color(.18f, .25f, .35f), anchor,
                    new Vector2(170 + (i % 6) * 110, 75 - (i / 6) * 110), new Vector2(98, 98), () => SetPortrait(key));
                var thumbnail = UIFactory.CreateImage("Retrato", button.transform, Color.white, sprite);
                thumbnail.rectTransform.Stretch(); thumbnail.preserveAspect = true; thumbnail.raycastTarget = false;
            }
            MenuScreens.Button(panel, "Guardar perfil", new Color(.2f, .55f, .3f), new Vector2(.5f, 0),
                new Vector2(240, 65), new Vector2(420, 85), () => Check(true), 32);
            MenuScreens.Button(panel, "Cancelar", new Color(.35f, .35f, .4f), new Vector2(.5f, 0),
                new Vector2(-240, 65), new Vector2(420, 85), Close, 32);
        }
        void SetPortrait(string key)
        {
            selected = key;
            var texture = Resources.Load<Texture2D>(string.IsNullOrEmpty(key) ? "ModeCards/trainer-profile" : key);
            if (texture == null) return;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            previews.Add(sprite); portrait.sprite = sprite;
        }
        void Check(bool save)
        {
            foreach (var variant in variants) UnityEngine.Object.Destroy(variant);
            variants.Clear();
            var registry = new ProfileNameRegistry(Path.Combine(Application.persistentDataPath, "profile_names.json"));
            bool valid = registry.Check(name.text, ctx.Save.ProfileId, save, out var result, out var suggestions);
            message.text = result;
            for (int i = 0; i < suggestions.Count; i++)
            {
                string suggestion = suggestions[i];
                var button = MenuScreens.Button(panel, suggestion, new Color(.3f, .4f, .55f), new Vector2(.5f, .5f),
                    new Vector2(-650 + i * 215, -170), new Vector2(205, 65), () => { name.text = suggestion; Check(false); }, 22);
                variants.Add(button.gameObject);
            }
            if (!valid || !save) return;
            ctx.Save.TrainerName = name.text.Trim().Normalize();
            ctx.Save.HasCustomProfileName = true;
            ctx.Save.ProfilePortraitResource = selected;
            SaveService.Save();
            Close();
        }
        void Close()
        {
            ctx.Menus.Close();
            foreach (var sprite in previews) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            previews.Clear();
            back();
        }
    }
}
