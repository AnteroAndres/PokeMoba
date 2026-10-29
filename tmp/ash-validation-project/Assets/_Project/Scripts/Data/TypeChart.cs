using System.Collections.Generic;

namespace PokeMoba.Data
{
    /// <summary>Tabla de efectividad moderna (18 tipos). Atacante → defensor.</summary>
    public static class TypeChart
    {
        static readonly float[,] Table = Build();

        public static float Effectiveness(PokemonType attack, PokemonType defense) =>
            Table[(int)attack, (int)defense];

        public static float Effectiveness(PokemonType attack, IReadOnlyList<PokemonType> defenderTypes)
        {
            float result = 1f;
            for (int i = 0; i < defenderTypes.Count; i++)
                result *= Effectiveness(attack, defenderTypes[i]);
            return result;
        }

        static float[,] Build()
        {
            const int n = 18;
            var t = new float[n, n];
            for (int a = 0; a < n; a++)
                for (int d = 0; d < n; d++)
                    t[a, d] = 1f;

            void Set(PokemonType atk, float value, params PokemonType[] defs)
            {
                foreach (var d in defs) t[(int)atk, (int)d] = value;
            }

            const PokemonType Nor = PokemonType.Normal, Fue = PokemonType.Fuego, Agu = PokemonType.Agua,
                Pla = PokemonType.Planta, Ele = PokemonType.Electrico, Hie = PokemonType.Hielo,
                Luc = PokemonType.Lucha, Ven = PokemonType.Veneno, Tie = PokemonType.Tierra,
                Vol = PokemonType.Volador, Psi = PokemonType.Psiquico, Bic = PokemonType.Bicho,
                Roc = PokemonType.Roca, Fan = PokemonType.Fantasma, Dra = PokemonType.Dragon,
                Sin = PokemonType.Siniestro, Ace = PokemonType.Acero, Had = PokemonType.Hada;

            Set(Nor, 0.5f, Roc, Ace); Set(Nor, 0f, Fan);
            Set(Fue, 2f, Pla, Hie, Bic, Ace); Set(Fue, 0.5f, Fue, Agu, Roc, Dra);
            Set(Agu, 2f, Fue, Tie, Roc); Set(Agu, 0.5f, Agu, Pla, Dra);
            Set(Ele, 2f, Agu, Vol); Set(Ele, 0.5f, Ele, Pla, Dra); Set(Ele, 0f, Tie);
            Set(Pla, 2f, Agu, Tie, Roc); Set(Pla, 0.5f, Fue, Pla, Ven, Vol, Bic, Dra, Ace);
            Set(Hie, 2f, Pla, Tie, Vol, Dra); Set(Hie, 0.5f, Fue, Agu, Hie, Ace);
            Set(Luc, 2f, Nor, Hie, Roc, Sin, Ace); Set(Luc, 0.5f, Ven, Vol, Psi, Bic, Had); Set(Luc, 0f, Fan);
            Set(Ven, 2f, Pla, Had); Set(Ven, 0.5f, Ven, Tie, Roc, Fan); Set(Ven, 0f, Ace);
            Set(Tie, 2f, Fue, Ele, Ven, Roc, Ace); Set(Tie, 0.5f, Pla, Bic); Set(Tie, 0f, Vol);
            Set(Vol, 2f, Pla, Luc, Bic); Set(Vol, 0.5f, Ele, Roc, Ace);
            Set(Psi, 2f, Luc, Ven); Set(Psi, 0.5f, Psi, Ace); Set(Psi, 0f, Sin);
            Set(Bic, 2f, Pla, Psi, Sin); Set(Bic, 0.5f, Fue, Luc, Ven, Vol, Fan, Ace, Had);
            Set(Roc, 2f, Fue, Hie, Vol, Bic); Set(Roc, 0.5f, Luc, Tie, Ace);
            Set(Fan, 2f, Psi, Fan); Set(Fan, 0.5f, Sin); Set(Fan, 0f, Nor);
            Set(Dra, 2f, Dra); Set(Dra, 0.5f, Ace); Set(Dra, 0f, Had);
            Set(Sin, 2f, Psi, Fan); Set(Sin, 0.5f, Luc, Sin, Had);
            Set(Ace, 2f, Hie, Roc, Had); Set(Ace, 0.5f, Fue, Agu, Ele, Ace);
            Set(Had, 2f, Luc, Dra, Sin); Set(Had, 0.5f, Fue, Ven, Ace);

            return t;
        }
    }
}
