using PokeMoba.Data;

namespace PokeMoba.Combat
{
    /// <summary>Estadísticas reales de un Pokémon a un nivel dado (IV 31, sin EV, naturaleza neutra).</summary>
    public class CombatStats
    {
        public int Level { get; private set; }
        public int MaxHp { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int SpAttack { get; private set; }
        public int SpDefense { get; private set; }
        public int Speed { get; private set; }

        const int Iv = 31;

        public static CombatStats FromBase(BaseStats b, int level) => new CombatStats
        {
            Level = level,
            MaxHp = (2 * b.Hp + Iv) * level / 100 + level + 10,
            Attack = Other(b.Attack, level),
            Defense = Other(b.Defense, level),
            SpAttack = Other(b.SpAttack, level),
            SpDefense = Other(b.SpDefense, level),
            Speed = Other(b.Speed, level)
        };

        static int Other(int baseStat, int level) => (2 * baseStat + Iv) * level / 100 + 5;
    }
}
