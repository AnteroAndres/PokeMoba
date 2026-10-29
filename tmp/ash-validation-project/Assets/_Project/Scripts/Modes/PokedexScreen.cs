using System.Linq;
using PokeMoba.Data;
using PokeMoba.Data.Pokedex;
using PokeMoba.UI;
using PokeMoba.World;
using UnityEngine;

namespace PokeMoba.Modes
{
    /// <summary>Pokédex (GDD §15): cuadrícula con desconocidos, vistos y capturados, y ficha de cada especie con su modelo.</summary>
    public class PokedexScreen : GameMode
    {
        public PokedexScreen(GameContext ctx) : base(ctx) { }

        public override void Enter() => ShowList();

        PickerState StateOf(SpeciesData s)
        {
            if (Save.UnlockAll || Save.Caught.Contains(s.DexNumber)) return PickerState.Normal;
            return Save.Seen.Contains(s.DexNumber) ? PickerState.Seen : PickerState.Unknown;
        }

        void ShowList()
        {
            Ctx.ClearWorld();
            var panel = Ctx.Menus.Open("Pokédex", $"Vistos {Save.Seen.Count} · Capturados {Save.CaughtCount} de 151", 0.95f);
            SpeciesPicker.Build(panel, PokemonDatabase.All, ShowEntry, 200f, 130f, StateOf);
            MenuScreens.Button(panel, "Menú principal", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 25f),
                new Vector2(420f, 80f), () => Ctx.ReturnToMainMenu());
        }

        void ShowEntry(SpeciesData s)
        {
            var state = StateOf(s);
            var world = Ctx.NewWorld("Pokédex");
            EnvironmentBuilder.BuildArena(Biome.Estadio, world, 6f);
            var holder = new GameObject("Expositor").transform;
            holder.SetParent(world, false);
            holder.position = new Vector3(-2.2f, 0f, 0f);
            CreatureModelBuilder.Build(s, holder, shiny: false);
            holder.gameObject.AddComponent<Spinner>();
            Ctx.Follow.LookAtPoint(new Vector3(0f, 1.2f, 0f), new Vector3(0f, 2.2f, 7.5f));

            string types = string.Join(" / ", s.Types.Select(t => t.DisplayName()));
            var panel = Ctx.Menus.Open($"#{s.DexNumber:000} {s.Name}", $"{s.Genus} · {types}", 0.25f);

            var card = MenuScreens.Card(panel, new Color(0f, 0f, 0f, 0.55f), new Vector2(1f, 0.5f), new Vector2(-40f, -10f), new Vector2(900f, 760f));
            if (state == PickerState.Seen)
            {
                MenuScreens.Label(card, "Visto, pero todavía no capturado.\nCaptúralo para ver toda su información.", 32,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 200f));
            }
            else
            {
                var b = s.BaseStats;
                string zone = s.Zone == null ? "Se obtiene evolucionando" : s.Zone == 0 ? "Pokémon inicial" : s.Zone == 17 ? "Evento especial" : $"Zona {s.Zone}";
                string evolutions = s.Evolutions.Count == 0 ? "No evoluciona"
                    : string.Join(", ", s.Evolutions.Select(e => $"{PokemonDatabase.Get(e.TargetDex)?.Name} ({Describe(e)})"));
                string moves = string.Join(" · ", s.Moves.Select(m => m.Name));
                string text =
                    $"<i>{s.FlavorText}</i>\n\n" +
                    $"Altura {s.HeightM:0.0} m · Peso {s.WeightKg:0.0} kg · Captura {s.CaptureRate}\n" +
                    $"Hábitat: {zone} · Dieta: {DietName(s.Diet)}\n" +
                    $"Rol en el MOBA: {s.Role ?? "—"}\n" +
                    $"Evolución: {evolutions}\n\n" +
                    $"PS {b.Hp} · Atq {b.Attack} · Def {b.Defense} · AtEsp {b.SpAttack} · DefEsp {b.SpDefense} · Vel {b.Speed} (total {b.Total})\n\n" +
                    $"Movimientos (nivel 50): {moves}";
                MenuScreens.Label(card, text, 26, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 720f), TextAnchor.UpperLeft);
            }

            MenuScreens.Button(panel, "Volver", new Color(0.35f, 0.35f, 0.4f), new Vector2(0.5f, 0f), new Vector2(0f, 25f),
                new Vector2(420f, 80f), ShowList);
        }

        static string Describe(EvolutionData e)
        {
            switch (e.Trigger)
            {
                case "level-up": return e.MinLevel.HasValue ? $"nivel {e.MinLevel}" : "amistad";
                case "use-item": return e.Item != null ? e.Item.Replace("-", " ") : "objeto";
                case "trade": return "intercambio";
                default: return e.Trigger ?? "?";
            }
        }

        static string DietName(string diet)
        {
            switch (diet)
            {
                case "BayasFrutas": return "bayas y frutas";
                case "HojasPlantas": return "hojas y plantas";
                case "NectarPolen": return "néctar y polen";
                case "InsectosSemillas": return "insectos y semillas";
                case "AlgasPlancton": return "algas y plancton";
                case "Pescado": return "pescado";
                case "Minerales": return "minerales y tierra";
                case "Electricidad": return "electricidad";
                case "FuegoCalor": return "fuego y calor";
                case "EnergiaEspiritual": return "energía espiritual";
                case "Residuos": return "residuos";
                case "DeTodo": return "de todo";
                default: return "—";
            }
        }
    }
}
