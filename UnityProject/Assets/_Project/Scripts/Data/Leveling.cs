using System;

namespace PokeMoba.Data
{
    /// <summary>Experiencia y niveles con las curvas de crecimiento de los juegos principales.</summary>
    public static class Leveling
    {
        public const int MaxLevel = 100;

        /// <summary>Experiencia total necesaria para estar en <paramref name="level"/>.</summary>
        public static int ExpForLevel(string growthRate, int level)
        {
            if (level <= 1) return 0;
            double n = level;
            double exp;
            switch (growthRate)
            {
                case "fast": exp = 4 * n * n * n / 5; break;
                case "slow": exp = 5 * n * n * n / 4; break;
                case "medium-slow": exp = 6 * n * n * n / 5 - 15 * n * n + 100 * n - 140; break;
                default: exp = n * n * n; break; // medium (medium-fast) y curvas raras
            }
            return Math.Max(0, (int)exp);
        }

        public static int LevelForExp(string growthRate, int exp)
        {
            int level = 1;
            while (level < MaxLevel && exp >= ExpForLevel(growthRate, level + 1)) level++;
            return level;
        }

        /// <summary>
        /// Multiplicador sobre la fórmula clásica para que en el celular no haga falta entrenar tanto
        /// (equivale al Repartir Experiencia de los juegos modernos).
        /// </summary>
        public const double ExpMultiplier = 3.0;

        /// <summary>Experiencia por debilitar a un Pokémon (fórmula clásica; ×1,5 contra entrenadores).</summary>
        public static int ExpYield(int baseExperience, int defeatedLevel, bool fromTrainer)
        {
            double exp = baseExperience * defeatedLevel / 7.0 * (fromTrainer ? 1.5 : 1.0) * ExpMultiplier;
            return Math.Max(1, (int)exp);
        }
    }
}
