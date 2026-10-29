using System;
using PokeMoba.Data;

namespace PokeMoba.Combat
{
    public struct DamageResult
    {
        public int Damage;
        public float Effectiveness;
        public bool Critical;
        /// <summary>Daño de quemadura o veneno (no viene de un golpe).</summary>
        public bool FromStatus;
    }

    /// <summary>Fórmula de daño del GDD §8.3. C# puro para poder probarla sin Unity.</summary>
    public static class DamageCalculator
    {
        static readonly Random Rng = new Random();

        public static DamageResult Calculate(
            MoveData move,
            CombatStats attacker, PokemonType[] attackerTypes, bool attackerBurned,
            CombatStats defender, PokemonType[] defenderTypes)
        {
            bool physical = move.Category == MoveCategory.Fisico;
            float atk = physical ? attacker.Attack : attacker.SpAttack;
            float def = physical ? defender.Defense : defender.SpDefense;

            float baseDamage = (2f * attacker.Level / 5f + 2f) * move.Power * (atk / def) / 50f + 2f;

            float stab = Array.IndexOf(attackerTypes, move.Type) >= 0 ? CombatTuning.StabMultiplier : 1f;
            float effectiveness = TypeChart.Effectiveness(move.Type, defenderTypes);
            bool critical = Rng.NextDouble() < CombatTuning.CritChance;
            float crit = critical ? CombatTuning.CritMultiplier : 1f;
            float variance = CombatTuning.MinVariance + (float)Rng.NextDouble() * (1f - CombatTuning.MinVariance);
            float burn = attackerBurned && physical ? 0.5f : 1f;

            int damage = effectiveness <= 0f
                ? 0
                : Math.Max(1, (int)Math.Round(baseDamage * stab * effectiveness * crit * variance * burn));

            return new DamageResult { Damage = damage, Effectiveness = effectiveness, Critical = critical && damage > 0 };
        }
    }
}
