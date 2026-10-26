using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>
    /// Crea un Pokémon provisional con primitivas (cuerpo, cabeza, ojos y un detalle según su tipo).
    /// En la fase de arte se sustituye por el prefab del modelo real.
    /// </summary>
    public static class CreatureFactory
    {
        static readonly Color PlayerRing = new Color(0.2f, 0.55f, 1f);
        static readonly Color EnemyRing = new Color(1f, 0.25f, 0.2f);

        public static Combatant Create(SpeciesData species, int team, Vector3 position, Vector3 facing, int level)
        {
            var root = new GameObject($"{species.Name} (equipo {team})");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing);

            BuildModel(species, root.transform);

            var ring = Vfx.Primitive(PrimitiveType.Cylinder, "Anillo de equipo", team == 0 ? PlayerRing : EnemyRing);
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            ring.transform.localScale = new Vector3(1.6f, 0.005f, 1.6f);

            // El "Model" tiene que existir antes de añadir los componentes: lo buscan al iniciarse.
            var combatant = root.AddComponent<Combatant>();
            combatant.Init(species, team, level);
            root.AddComponent<AbilityCaster>();
            return combatant;
        }

        static void BuildModel(SpeciesData species, Transform parent)
        {
            var model = new GameObject("Model").transform;
            model.SetParent(parent, false);
            float s = species.Size;
            Color bodyColor = Color.Lerp(species.Types[0].TypeColor(), Color.white, 0.15f);

            var body = Vfx.Primitive(PrimitiveType.Capsule, "Cuerpo", bodyColor);
            body.transform.SetParent(model, false);
            body.transform.localPosition = new Vector3(0f, 0.8f * s, 0f);
            body.transform.localScale = new Vector3(0.95f, 0.8f, 0.95f) * s;

            var head = Vfx.Primitive(PrimitiveType.Sphere, "Cabeza", bodyColor);
            head.transform.SetParent(model, false);
            head.transform.localPosition = new Vector3(0f, 1.75f * s, 0.15f * s);
            head.transform.localScale = Vector3.one * 0.85f * s;

            for (int side = -1; side <= 1; side += 2)
            {
                var eye = Vfx.Primitive(PrimitiveType.Sphere, "Ojo", Color.black);
                eye.transform.SetParent(head.transform, false);
                eye.transform.localPosition = new Vector3(0.2f * side, 0.1f, 0.42f);
                eye.transform.localScale = Vector3.one * 0.16f;
            }

            switch (species.Types[0])
            {
                case PokemonType.Fuego:
                    var tail = Vfx.Primitive(PrimitiveType.Sphere, "Llama de la cola", new Color(1f, 0.45f, 0.05f));
                    tail.transform.SetParent(model, false);
                    tail.transform.localPosition = new Vector3(0f, 0.9f * s, -0.85f * s);
                    tail.transform.localScale = Vector3.one * 0.4f * s;
                    break;

                case PokemonType.Agua:
                    var shell = Vfx.Primitive(PrimitiveType.Sphere, "Caparazón", new Color(0.6f, 0.4f, 0.2f));
                    shell.transform.SetParent(model, false);
                    shell.transform.localPosition = new Vector3(0f, 0.85f * s, -0.3f * s);
                    shell.transform.localScale = new Vector3(1.05f, 1.1f, 0.6f) * s;
                    break;

                case PokemonType.Planta:
                    var bulb = Vfx.Primitive(PrimitiveType.Sphere, "Bulbo", new Color(0.2f, 0.55f, 0.25f));
                    bulb.transform.SetParent(model, false);
                    bulb.transform.localPosition = new Vector3(0f, 1.35f * s, -0.35f * s);
                    bulb.transform.localScale = Vector3.one * 0.85f * s;
                    break;
            }
        }
    }
}
