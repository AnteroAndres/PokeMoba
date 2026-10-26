using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.World;
using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>Crea Pokémon listos para pelear (modelo, anillo de equipo, combate y habilidades).</summary>
    public static class CreatureFactory
    {
        public static readonly Color[] TeamColors =
        {
            new Color(0.2f, 0.55f, 1f),   // 0: jugador / equipo azul
            new Color(1f, 0.25f, 0.2f),   // 1: rival / equipo rojo
            new Color(0.95f, 0.85f, 0.3f) // 2: salvajes
        };

        public static Combatant Create(SpeciesData species, int team, Vector3 position, Vector3 facing, int level,
            bool shiny = false, MoveData[] moves = null, Transform parent = null)
        {
            var root = new GameObject($"{species.Name} (equipo {team})");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing);

            CreatureModelBuilder.Build(species, root.transform, shiny);
            AddTeamRing(root.transform, team, species.Size);

            // El "Model" tiene que existir antes de añadir los componentes: lo buscan al iniciarse.
            var combatant = root.AddComponent<Combatant>();
            combatant.Shiny = shiny;
            combatant.Init(species, team, level, moves);
            root.AddComponent<AbilityCaster>();

            if (shiny) Vfx.Burst(position + Vector3.up, new Color(1f, 0.9f, 0.3f), 2.5f);
            return combatant;
        }

        /// <summary>Cambia el modelo y los datos de un combatiente ya creado (evolución).</summary>
        public static void Rebuild(Combatant combatant, SpeciesData species, int level, MoveData[] moves)
        {
            var old = combatant.transform.Find("Model");
            if (old != null)
            {
                old.name = "Model (antiguo)";
                old.SetParent(null, false);
                Object.Destroy(old.gameObject);
            }

            CreatureModelBuilder.Build(species, combatant.transform, combatant.Shiny);
            combatant.name = $"{species.Name} (equipo {combatant.Team})";
            combatant.Reinitialize(species, level, moves, combatant.MaxHp > 0f ? combatant.Hp / combatant.MaxHp : 1f);
            combatant.Motor.RefreshModel();
            combatant.GetComponent<AbilityCaster>().ResetCooldowns();
            Vfx.Burst(combatant.transform.position + Vector3.up, Color.white, 3f);
        }

        static void AddTeamRing(Transform root, int team, float size)
        {
            var ring = Vfx.Primitive(PrimitiveType.Cylinder, "Anillo de equipo", TeamColors[Mathf.Clamp(team, 0, TeamColors.Length - 1)]);
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            float d = 1.6f * Mathf.Max(1f, size * 0.85f);
            ring.transform.localScale = new Vector3(d, 0.005f, d);
        }
    }
}
