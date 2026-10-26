using UnityEngine;

namespace PokeMoba.Data
{
    /// <summary>Los 18 tipos modernos.</summary>
    public enum PokemonType
    {
        Normal, Fuego, Agua, Planta, Electrico, Hielo, Lucha, Veneno, Tierra,
        Volador, Psiquico, Bicho, Roca, Fantasma, Dragon, Siniestro, Acero, Hada
    }

    public static class PokemonTypeInfo
    {
        static readonly string[] Names =
        {
            "Normal", "Fuego", "Agua", "Planta", "Eléctrico", "Hielo", "Lucha", "Veneno", "Tierra",
            "Volador", "Psíquico", "Bicho", "Roca", "Fantasma", "Dragón", "Siniestro", "Acero", "Hada"
        };

        static readonly Color[] Colors =
        {
            new Color32(168, 167, 122, 255), new Color32(238, 129, 48, 255), new Color32(99, 144, 240, 255),
            new Color32(122, 199, 76, 255), new Color32(247, 208, 44, 255), new Color32(150, 217, 214, 255),
            new Color32(194, 46, 40, 255), new Color32(163, 62, 161, 255), new Color32(226, 191, 101, 255),
            new Color32(169, 143, 243, 255), new Color32(249, 85, 135, 255), new Color32(166, 185, 26, 255),
            new Color32(182, 161, 54, 255), new Color32(115, 87, 151, 255), new Color32(111, 53, 252, 255),
            new Color32(112, 87, 70, 255), new Color32(183, 183, 206, 255), new Color32(214, 133, 173, 255)
        };

        public static string DisplayName(this PokemonType type) => Names[(int)type];
        public static Color TypeColor(this PokemonType type) => Colors[(int)type];
    }
}
