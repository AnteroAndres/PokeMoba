using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeMoba.Data.Pokedex
{
    /// <summary>
    /// Construye la Pokédex del juego a partir del JSON de PokeAPI y de overrides.json,
    /// y elige el ataque básico y las 2 habilidades de cada especie según su nivel.
    /// C# puro (sin Unity) para poder probarlo fuera del editor.
    /// </summary>
    public static class PokedexBuilder
    {
        // Movimientos que debilitan al usuario o no tienen sentido en tiempo real.
        static readonly HashSet<string> Excluded = new HashSet<string>
        {
            "self-destruct", "explosion", "misty-explosion", "memento", "final-gambit",
            "healing-wish", "lunar-dance", "struggle"
        };

        public static Pokedex Build(RawPokedex dex, RawOverrides overrides, int level, Action<string> warn = null)
        {
            overrides = overrides ?? new RawOverrides();
            var context = new Context
            {
                Moves = dex.Moves.ToDictionary(m => m.Key),
                SpeciesOverrides = overrides.Species ?? new Dictionary<string, SpeciesOverride>(),
                MoveOverrides = overrides.Moves ?? new Dictionary<string, MoveOverride>(),
                Warn = warn
            };

            var raws = new Dictionary<string, RawSpecies>();
            var species = new List<SpeciesData>();
            foreach (var raw in dex.Species.OrderBy(s => s.Id))
            {
                raws[raw.Key] = raw;
                species.Add(BuildSpecies(raw, context, level));
            }
            return new Pokedex(species, (s, lvl) => ChooseMoves(raws[s.Key], s.Types, context, lvl));
        }

        sealed class Context
        {
            public Dictionary<string, RawMove> Moves;
            public Dictionary<string, SpeciesOverride> SpeciesOverrides;
            public Dictionary<string, MoveOverride> MoveOverrides;
            public Action<string> Warn;
        }

        static SpeciesData BuildSpecies(RawSpecies raw, Context context, int level)
        {
            context.SpeciesOverrides.TryGetValue(raw.Key, out var speciesOverride);

            var types = raw.Types
                .Select(t => Enum.TryParse(t, out PokemonType type) ? type : PokemonType.Normal)
                .ToArray();

            var species = new SpeciesData
            {
                DexNumber = raw.Id,
                Key = raw.Key,
                Name = raw.Name ?? raw.Key,
                Genus = raw.Genus,
                FlavorText = raw.FlavorText,
                Role = speciesOverride?.Role,
                Diet = speciesOverride?.Diet,
                Zone = speciesOverride?.Zone,
                Types = types,
                BaseStats = new BaseStats(raw.BaseStats.Hp, raw.BaseStats.Attack, raw.BaseStats.Defense,
                    raw.BaseStats.SpAttack, raw.BaseStats.SpDefense, raw.BaseStats.Speed),
                CaptureRate = raw.CaptureRate,
                BaseExperience = raw.BaseExperience ?? 64,
                GrowthRate = raw.GrowthRate ?? "medium",
                HeightM = raw.HeightM,
                WeightKg = raw.WeightKg,
                IsLegendary = raw.IsLegendary,
                IsMythical = raw.IsMythical,
                BodyShape = raw.Shape ?? "upright",
                BodyColor = raw.Color,
                EvolvesFrom = raw.EvolvesFrom,
                Size = Clamp(0.7f + raw.HeightM * 0.25f, 0.7f, 2.2f)
            };

            foreach (var evolution in raw.Evolutions ?? new List<RawEvolution>())
            {
                var method = evolution.Methods?.FirstOrDefault() ?? new RawEvolutionMethod();
                species.Evolutions.Add(new EvolutionData
                {
                    TargetDex = evolution.Species,
                    Trigger = method.Trigger,
                    MinLevel = method.MinLevel,
                    Item = method.Item
                });
            }

            species.Moves = ChooseMoves(raw, types, context, level);
            return species;
        }

        static MoveData[] ChooseMoves(RawSpecies raw, PokemonType[] types, Context context, int level)
        {
            context.SpeciesOverrides.TryGetValue(raw.Key, out var speciesOverride);
            var candidates = Candidates(raw, context, level);

            MoveData basic = FromOverride(speciesOverride?.BasicMove, context, basic: true, raw.Key)
                             ?? ChooseBasic(candidates, types, context);

            var skills = (speciesOverride?.Skills ?? new List<string>())
                .Select(key => FromOverride(key, context, basic: false, raw.Key))
                .Where(m => m != null)
                .Take(2)
                .ToList();
            MoveData strongest = FromOverride(speciesOverride?.Ultimate, context, basic: false, raw.Key);

            // Lo que falte se elige solo: la 4.ª ranura es el movimiento más fuerte y las 2 habilidades, los siguientes.
            int missing = (2 - skills.Count) + (strongest == null ? 1 : 0);
            if (missing > 0)
            {
                var taken = skills.Concat(new[] { basic, strongest }).Where(m => m != null).ToList();
                var picked = ChooseSkills(candidates, types, taken, context, missing).ToList();
                if (strongest == null && picked.Count > 0)
                {
                    strongest = picked[0];
                    picked.RemoveAt(0);
                }
                skills.AddRange(picked.Take(2 - skills.Count));
            }

            while (skills.Count < 2) skills.Add(MoveFactory.Fallback(basic: false));

            return new[] { basic ?? MoveFactory.Fallback(basic: true), skills[0], skills[1], strongest ?? MoveFactory.Fallback(basic: false) };
        }

        /// <summary>Movimientos de daño que aprende hasta el nivel dado; si son pocos, se suman los de MT.</summary>
        static List<RawMove> Candidates(RawSpecies raw, Context context, int level)
        {
            var learnset = raw.Learnset ?? new RawLearnset();
            var keys = (learnset.LevelUp ?? new List<RawLevelMove>())
                .Where(m => m.Level <= level)
                .Select(m => m.Move)
                .Distinct()
                .ToList();

            var usable = keys.Select(k => Usable(k, context)).Where(m => m != null).ToList();
            if (usable.Count < 4)
            {
                // Las MT completan, pero sin movimientos más fuertes de lo que toca a ese nivel
                // (que un Charmander de nivel 5 no tenga Sofoco).
                int maxPower = 30 + level * 3;
                usable.AddRange((learnset.Machine ?? new List<string>())
                    .Where(k => !keys.Contains(k))
                    .Select(k => Usable(k, context))
                    .Where(m => m != null && (m.Power ?? MoveFactory.VariablePower) <= maxPower));
            }
            return usable;
        }

        static RawMove Usable(string key, Context context)
        {
            if (key == null || Excluded.Contains(key) || !context.Moves.TryGetValue(key, out var move)) return null;
            if (move.Category == "Estado") return null;
            context.MoveOverrides.TryGetValue(key, out var ov);
            return MoveFactory.IsSupportedShape(ov?.Shape ?? move.SuggestedShape) ? move : null;
        }

        static MoveData FromOverride(string key, Context context, bool basic, string speciesKey)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!context.Moves.TryGetValue(key, out var raw))
            {
                context.Warn?.Invoke($"overrides.json: {speciesKey} usa el movimiento '{key}', que no está en los datos.");
                return null;
            }
            context.MoveOverrides.TryGetValue(key, out var ov);
            var move = MoveFactory.Create(raw, ov, basic);
            if (move == null)
                context.Warn?.Invoke($"overrides.json: '{key}' ({speciesKey}) no es un movimiento de daño con forma válida.");
            return move;
        }

        static MoveData ChooseBasic(List<RawMove> candidates, PokemonType[] types, Context context)
        {
            var light = candidates
                .Where(m => (m.Power ?? MoveFactory.VariablePower) <= 60)
                .Where(m => Shape(m, context) == "Melee" || Shape(m, context) == "Projectile")
                .OrderByDescending(m => Score(m, types))
                .FirstOrDefault();

            var pick = light ?? candidates.OrderBy(m => m.Power ?? MoveFactory.VariablePower).FirstOrDefault();
            if (pick == null) return null;
            context.MoveOverrides.TryGetValue(pick.Key, out var ov);
            return MoveFactory.Create(pick, ov, basic: true);
        }

        /// <summary>
        /// Elige <paramref name="count"/> movimientos de mayor a menor daño. Cada uno, si puede, de una forma
        /// que todavía no esté elegida y sin perder demasiado daño (más variedad al jugar).
        /// </summary>
        static IEnumerable<MoveData> ChooseSkills(List<RawMove> candidates, PokemonType[] types,
            List<MoveData> alreadyChosen, Context context, int count)
        {
            var taken = new HashSet<string>(alreadyChosen.Select(m => m.Name));
            var usedShapes = new HashSet<MoveShape>(alreadyChosen.Select(m => m.Shape));

            var ranked = candidates
                .Where(m => !taken.Contains(m.Name ?? m.Key))
                .OrderByDescending(m => Score(m, types))
                .ToList();

            for (int i = 0; i < count && ranked.Count > 0; i++)
            {
                float bar = Score(ranked[0], types) * 0.6f;
                var pick = i == 0
                    ? ranked[0] // el primero (el más fuerte) siempre
                    : ranked.FirstOrDefault(m => !usedShapes.Contains(ShapeOf(m, context)) && Score(m, types) >= bar) ?? ranked[0];
                ranked.Remove(pick);

                context.MoveOverrides.TryGetValue(pick.Key, out var ov);
                var move = MoveFactory.Create(pick, ov, basic: false);
                if (move == null) continue;
                usedShapes.Add(move.Shape);
                yield return move;
            }
        }

        static MoveShape ShapeOf(RawMove move, Context context) =>
            Enum.TryParse(Shape(move, context), out MoveShape shape) ? shape : MoveShape.Melee;

        static float Score(RawMove move, PokemonType[] types) =>
            (move.Power ?? MoveFactory.VariablePower)
            * (IsStab(move, types) ? 1.5f : 1f)
            * (move.Accuracy ?? 100) / 100f;

        static bool IsStab(RawMove move, PokemonType[] types) =>
            Enum.TryParse(move.Type, out PokemonType type) && Array.IndexOf(types, type) >= 0;

        static string Shape(RawMove move, Context context) =>
            context.MoveOverrides.TryGetValue(move.Key, out var ov) && ov?.Shape != null ? ov.Shape : move.SuggestedShape;

        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
