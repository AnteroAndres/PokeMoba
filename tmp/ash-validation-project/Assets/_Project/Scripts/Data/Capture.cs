using System;

namespace PokeMoba.Data
{
    public class BallInfo
    {
        public string Key;
        public string Name;
        public float Bonus;
        public int Price;
        /// <summary>Zona que hay que tener abierta para comprarla (0 = desde el inicio; -1 = no se vende).</summary>
        public int ShopZone;
    }

    /// <summary>Poké Balls de la primera versión de la Aventura (GDD §9.3).</summary>
    public static class Balls
    {
        public static readonly BallInfo PokeBall = new BallInfo { Key = "poke-ball", Name = "Poké Ball", Bonus = 1f, Price = 200, ShopZone = 0 };
        public static readonly BallInfo SuperBall = new BallInfo { Key = "great-ball", Name = "Super Ball", Bonus = 1.5f, Price = 600, ShopZone = 5 };
        public static readonly BallInfo UltraBall = new BallInfo { Key = "ultra-ball", Name = "Ultra Ball", Bonus = 2f, Price = 800, ShopZone = 8 };
        public static readonly BallInfo MasterBall = new BallInfo { Key = "master-ball", Name = "Master Ball", Bonus = float.PositiveInfinity, Price = 0, ShopZone = -1 };

        public static readonly BallInfo[] All = { PokeBall, SuperBall, UltraBall, MasterBall };

        public static BallInfo Get(string key) => Array.Find(All, b => b.Key == key) ?? PokeBall;
    }

    public struct CaptureResult
    {
        public bool Caught;
        /// <summary>Sacudidas que se ven antes de cerrarse o abrirse (0–3).</summary>
        public int Shakes;
    }

    /// <summary>Fórmula de captura moderna (GDD §9.2). C# puro para poder probarla.</summary>
    public static class CaptureCalculator
    {
        public static float StatusBonus(StatusEffect status) =>
            status == StatusEffect.None ? 1f : 1.5f; // quemado y envenenado; dormido y congelado (×2,5) llegarán con esos estados

        public static CaptureResult Attempt(int captureRate, float hp, float maxHp, float ballBonus,
            float statusBonus, float extraBonus, Random rng)
        {
            if (float.IsPositiveInfinity(ballBonus)) return new CaptureResult { Caught = true, Shakes = 3 };

            double a = (3 * maxHp - 2 * hp) * captureRate * ballBonus / (3 * maxHp) * statusBonus * extraBonus;
            if (a >= 255) return new CaptureResult { Caught = true, Shakes = 3 };
            if (a <= 0) return new CaptureResult { Caught = false, Shakes = 0 };

            double b = 65536 / Math.Pow(255 / a, 0.1875);
            for (int check = 0; check < 4; check++)
            {
                if (rng.Next(65536) >= b)
                    return new CaptureResult { Caught = false, Shakes = Math.Min(check, 3) };
            }
            return new CaptureResult { Caught = true, Shakes = 3 };
        }

        /// <summary>Probabilidad aproximada (0–1), para mostrarla en pantalla.</summary>
        public static float Probability(int captureRate, float hp, float maxHp, float ballBonus, float statusBonus, float extraBonus)
        {
            if (float.IsPositiveInfinity(ballBonus)) return 1f;
            double a = (3 * maxHp - 2 * hp) * captureRate * ballBonus / (3 * maxHp) * statusBonus * extraBonus;
            if (a >= 255) return 1f;
            if (a <= 0) return 0f;
            double b = 65536 / Math.Pow(255 / a, 0.1875);
            return (float)Math.Pow(b / 65536, 4);
        }
    }
}
