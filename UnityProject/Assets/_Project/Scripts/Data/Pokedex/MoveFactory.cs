using System;

namespace PokeMoba.Data.Pokedex
{
    /// <summary>
    /// Convierte un movimiento de PokeAPI en un movimiento de combate en tiempo real (GDD §8.4):
    /// la forma decide alcance, radio y preparación; la potencia decide la recarga.
    /// C# puro (sin Unity) para poder probarlo fuera del editor.
    /// </summary>
    public static class MoveFactory
    {
        /// <summary>Potencia para movimientos de daño variable (Sísmico, Patada Baja…).</summary>
        public const int VariablePower = 60;

        /// <summary>Devuelve null si el movimiento no se puede usar en combate (estado o forma no soportada).</summary>
        public static MoveData Create(RawMove raw, MoveOverride overrides, bool basic)
        {
            if (raw == null || raw.Category == "Estado") return null;
            if (!Enum.TryParse(overrides?.Shape ?? raw.SuggestedShape, out MoveShape shape)) return null;
            if (!Enum.TryParse(raw.Type, out PokemonType type)) type = PokemonType.Normal;

            var move = new MoveData
            {
                Name = raw.Name ?? raw.Key,
                Type = type,
                Category = raw.Category == "Fisico" ? MoveCategory.Fisico : MoveCategory.Especial,
                Power = raw.Power ?? VariablePower,
                Shape = shape,
                Status = ToStatus(raw.Ailment),
                StatusChance = raw.AilmentChance / 100f
            };
            // Un estado sin probabilidad indicada lo provoca siempre (por ejemplo, Tóxico).
            if (move.Status != StatusEffect.None && raw.AilmentChance == 0) move.StatusChance = 1f;

            ApplyTemplate(move, basic);

            if (overrides?.Range != null) move.Range = overrides.Range.Value;
            if (overrides?.Radius != null) move.Radius = overrides.Radius.Value;
            if (overrides?.Cooldown != null) move.Cooldown = overrides.Cooldown.Value;
            return move;
        }

        /// <summary>Movimiento de reserva para especies sin ataques de daño (Ditto, Magikarp…).</summary>
        public static MoveData Fallback(bool basic)
        {
            var move = new MoveData
            {
                Name = "Forcejeo", Type = PokemonType.Normal, Category = MoveCategory.Fisico,
                Power = 50, Shape = MoveShape.Melee
            };
            ApplyTemplate(move, basic);
            return move;
        }

        public static bool IsSupportedShape(string shape) => Enum.TryParse(shape, out MoveShape _);

        static void ApplyTemplate(MoveData move, bool basic)
        {
            float p = move.Power;
            switch (move.Shape)
            {
                case MoveShape.Melee:
                    move.Range = 2.4f;
                    move.Radius = 1.6f;
                    move.Windup = 0.12f;
                    break;

                case MoveShape.Projectile:
                    move.Range = basic ? 7f : 10f;
                    move.Radius = 0.45f + p / 400f;
                    move.ProjectileSpeed = Math.Max(9f, 16f - p / 25f);
                    move.Windup = 0.15f + p / 1000f;
                    break;

                case MoveShape.Cone:
                    move.Range = 5.5f;
                    move.Radius = 50f;
                    move.Windup = 0.35f;
                    break;

                case MoveShape.GroundArea:
                    move.Range = 9f;
                    move.Radius = 1.8f + p / 200f;
                    move.TelegraphTime = 0.8f;
                    move.Windup = 0.2f;
                    break;

                case MoveShape.Dash:
                    move.Range = 6f;
                    move.Radius = 1.2f;
                    move.Windup = 0.2f;
                    break;
            }

            if (basic)
                move.Cooldown = move.Shape == MoveShape.Melee ? 0.6f : 0.85f;
            else
                move.Cooldown = 1.5f + p * 0.05f
                    + (move.Shape == MoveShape.Cone || move.Shape == MoveShape.GroundArea ? 1f : 0f);
        }

        static StatusEffect ToStatus(string ailment)
        {
            switch (ailment)
            {
                case "burn": return StatusEffect.Quemado;
                case "poison": return StatusEffect.Envenenado;
                default: return StatusEffect.None;
            }
        }
    }
}
