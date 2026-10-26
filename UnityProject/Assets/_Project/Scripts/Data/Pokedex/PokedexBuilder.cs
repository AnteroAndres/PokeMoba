using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeMoba.Data.Pokedex
{
    /// <summary>
    /// Construye las SpeciesData del juego a partir del JSON de PokeAPI y de overrides.json.
    /// Elige el ataque básico y las 2 habilidades de cada especie según su nivel.
    /// C# puro (sin Unity) para poder probarlo fuera del editor.
    /// </summary>
    public static class PokedexBuilder
    {
        // Movimientos que debilitan al usuario o no tienen sentido en el prototipo.
        static readonly HashSet<string> Excluded = new HashSet<string>
        {
            "self-destruct", "explosion", "misty-explosion", "memento", "final-gambit",
            "healing-wish", "lunar-dance", "struggle"
        };

        public static List<SpeciesData> Build(RawPokedex dex, RawOverrides overrides, int level, Action<string> warn = null)
        {
            var moves = dex.Moves.ToDictionary(m => m.Key);
            overrides = overrides ?? new RawOverrides();
            var speciesOverrides = overrides.Species ?? new Dictionary<string, SpeciesOverride>();
            var moveOverrides = overrides.Moves ?? new Dictionary<string, MoveOverride>();

            var result = new List<SpeciesData>();
            foreach (var raw in dex.Species.OrderBy(s => s.Id))
            {
                speciesOverrides.TryGetValue(raw.Key, out var speciesOverride);
                result.Add(BuildSpecies(raw, speciesOverride, moves, moveOverrides, level, warn));
            }
            return result;
        }

        static SpeciesData BuildSpecies(
            RawSpecies raw, SpeciesOverride speciesOverride,
            Dictionary<string, RawMove> moves, Dictionary<string, MoveOverride> moveOverrides,
            int level, Action<string> warn)
        {
            var types = raw.Types
                .Select(t => Enum.TryParse(t, out PokemonType type) ? type : PokemonType.Normal)
                .ToArray();

            var species = new SpeciesData
            {
                DexNumber = raw.Id,
                Key = raw.Key,
                Name = raw.Name ?? raw.Key,
                Genus = raw.Genus,
                Role = speciesOverride?.Role,
                Types = types,
                BaseStats = new BaseStats(raw.BaseStats.Hp, raw.BaseStats.Attack, raw.BaseStats.Defense,
                    raw.BaseStats.SpAttack, raw.BaseStats.SpDefense, raw.BaseStats.Speed),
                Size = Clamp(0.7f + raw.HeightM * 0.25f, 0.7f, 2.2f)
            };

            var candidates = Candidates(raw, moves, moveOverrides, level);

            MoveData basic = FromOverride(speciesOverride?.BasicMove, moves, moveOverrides, basic: true, raw.Key, warn)
                             ?? ChooseBasic(candidates, types, moveOverrides);

            var skills = (speciesOverride?.Skills ?? new List<string>())
                .Select(key => FromOverride(key, moves, moveOverrides, basic: false, raw.Key, warn))
                .Where(m => m != null)
                .Take(2)
                .ToList();
            if (skills.Count < 2)
                skills.AddRange(ChooseSkills(candidates, types, basic, skills, moveOverrides).Take(2 - skills.Count));

            while (skills.Count < 2) skills.Add(MoveFactory.Fallback(basic: false));

            species.Moves = new[] { basic ?? MoveFactory.Fallback(basic: true), skills[0], skills[1] };
            return species;
        }

        /// <summary>Movimientos de daño que aprende hasta el nivel dado; si son pocos, se suman los de MT.</summary>
        static List<RawMove> Candidates(RawSpecies raw, Dictionary<string, RawMove> moves,
            Dictionary<string, MoveOverride> moveOverrides, int level)
        {
            var learnset = raw.Learnset ?? new RawLearnset();
            var keys = (learnset.LevelUp ?? new List<RawLevelMove>())
                .Where(m => m.Level <= level)
                .Select(m => m.Move)
                .Distinct()
                .ToList();

            var usable = keys.Select(k => Usable(k, moves, moveOverrides)).Where(m => m != null).ToList();
            if (usable.Count < 3)
            {
                usable.AddRange((learnset.Machine ?? new List<string>())
                    .Where(k => !keys.Contains(k))
                    .Select(k => Usable(k, moves, moveOverrides))
                    .Where(m => m != null));
            }
            return usable;
        }

        static RawMove Usable(string key, Dictionary<string, RawMove> moves, Dictionary<string, MoveOverride> moveOverrides)
        {
            if (key == null || Excluded.Contains(key) || !moves.TryGetValue(key, out var move)) return null;
            if (move.Category == "Estado") return null;
            moveOverrides.TryGetValue(key, out var ov);
            return MoveFactory.IsSupportedShape(ov?.Shape ?? move.SuggestedShape) ? move : null;
        }

        static MoveData FromOverride(string key, Dictionary<string, RawMove> moves,
            Dictionary<string, MoveOverride> moveOverrides, bool basic, string speciesKey, Action<string> warn)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!moves.TryGetValue(key, out var raw))
            {
                warn?.Invoke($"overrides.json: {speciesKey} usa el movimiento '{key}', que no está en los datos.");
                return null;
            }
            moveOverrides.TryGetValue(key, out var ov);
            var move = MoveFactory.Create(raw, ov, basic);
            if (move == null)
                warn?.Invoke($"overrides.json: '{key}' ({speciesKey}) no es un movimiento de daño con forma válida.");
            return move;
        }

        static MoveData ChooseBasic(List<RawMove> candidates, PokemonType[] types,
            Dictionary<string, MoveOverride> moveOverrides)
        {
            var light = candidates
                .Where(m => (m.Power ?? MoveFactory.VariablePower) <= 60)
                .Where(m => Shape(m, moveOverrides) == "Melee" || Shape(m, moveOverrides) == "Projectile")
                .OrderByDescending(m => Score(m, types))
                .FirstOrDefault();

            var pick = light ?? candidates.OrderBy(m => m.Power ?? MoveFactory.VariablePower).FirstOrDefault();
            if (pick == null) return null;
            moveOverrides.TryGetValue(pick.Key, out var ov);
            return MoveFactory.Create(pick, ov, basic: true);
        }

        static IEnumerable<MoveData> ChooseSkills(List<RawMove> candidates, PokemonType[] types, MoveData basic,
            List<MoveData> alreadyChosen, Dictionary<string, MoveOverride> moveOverrides)
        {
            var taken = new HashSet<string>(alreadyChosen.Select(m => m.Name));
            if (basic != null) taken.Add(basic.Name);

            var ranked = candidates
                .Where(m => !taken.Contains(m.Name ?? m.Key))
                .OrderByDescending(m => Score(m, types))
                .ToList();
            if (ranked.Count == 0) yield break;

            var first = ranked[0];
            ranked.RemoveAt(0);

            // La segunda, de otra forma distinta si no pierde demasiado daño (más variedad al jugar).
            RawMove second = null;
            if (ranked.Count > 0)
            {
                float bar = Score(ranked[0], types) * 0.6f;
                second = ranked.FirstOrDefault(m => Shape(m, moveOverrides) != Shape(first, moveOverrides) && Score(m, types) >= bar)
                         ?? ranked[0];
            }

            foreach (var raw in new[] { first, second })
            {
                if (raw == null) continue;
                moveOverrides.TryGetValue(raw.Key, out var ov);
                var move = MoveFactory.Create(raw, ov, basic: false);
                if (move != null) yield return move;
            }
        }

        static float Score(RawMove move, PokemonType[] types) =>
            (move.Power ?? MoveFactory.VariablePower)
            * (IsStab(move, types) ? 1.5f : 1f)
            * (move.Accuracy ?? 100) / 100f;

        static bool IsStab(RawMove move, PokemonType[] types) =>
            Enum.TryParse(move.Type, out PokemonType type) && Array.IndexOf(types, type) >= 0;

        static string Shape(RawMove move, Dictionary<string, MoveOverride> moveOverrides) =>
            moveOverrides.TryGetValue(move.Key, out var ov) && ov?.Shape != null ? ov.Shape : move.SuggestedShape;

        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
