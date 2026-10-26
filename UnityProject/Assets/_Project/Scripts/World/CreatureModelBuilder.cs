using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>
    /// Construye el modelo de un Pokémon.
    /// 1) Si existe un modelo real en Resources/Models/Pokemon/NNN_clave (por ejemplo 006_charizard), usa ese.
    /// 2) Si no, genera una figura con primitivas según la forma del cuerpo y el color de PokeAPI,
    ///    con detalles según el tipo (llama, bulbo, caparazón, alas, cuernos…).
    /// </summary>
    public static class CreatureModelBuilder
    {
        public const string ModelsFolder = "Models/Pokemon/";

        public static Transform Build(SpeciesData species, Transform parent, bool shiny)
        {
            var model = new GameObject("Model").transform;
            model.SetParent(parent, false);

            var prefab = Resources.Load<GameObject>($"{ModelsFolder}{species.DexNumber:000}_{species.Key}");
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab, model);
                FitHeight(instance.transform, 1.6f * species.Size);
                foreach (var c in instance.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                return model;
            }

            // "Model" lo mueve CreatureMotor (saltos, esquiva, caída); "Rig" lo anima CreatureAnimator.
            var rig = new GameObject("Rig").transform;
            rig.SetParent(model, false);

            var palette = Palette.For(species, shiny);
            var parts = new Parts(rig, palette, species.Size);
            BuildShape(species, parts);
            AddTypeDetails(species, parts);

            if (shiny) parts.ShinyStar();

            var animator = model.gameObject.AddComponent<CreatureAnimator>();
            animator.Setup(rig, parts.Wings, parts.Legs, parts.Tail, parts.Segments, parts.Flame, parts.Floating);
            return model;
        }

        static void FitHeight(Transform t, float height)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (bounds.size.y > 0.01f) t.localScale *= height / bounds.size.y;
        }

        // ------------------------------------------------------------------ formas

        static void BuildShape(SpeciesData s, Parts p)
        {
            switch (s.BodyShape)
            {
                case "quadruped": Quadruped(p); break;
                case "wings": Bird(p); break;
                case "bug-wings": WingedBug(p); break;
                case "squiggle": Serpent(p); break;
                case "fish": Fish(p); break;
                case "ball": Ball(p); break;
                case "blob": Blob(p); break;
                case "tentacles": Tentacles(p); break;
                case "heads": Heads(p); break;
                case "humanoid": Humanoid(p, bigArms: false); break;
                case "arms": Humanoid(p, bigArms: true); break;
                case "legs": LongLegs(p); break;
                case "armor": Armored(p); break;
                default: Upright(p); break;
            }
        }

        static void Quadruped(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Capsule, "Cuerpo", new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.72f, 0.9f), p.Main, new Vector3(90f, 0f, 0f));
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.3f, 0.8f), Vector3.one * 0.78f, p.Main);
            p.Add(PrimitiveType.Sphere, "Hocico", new Vector3(0f, 1.18f, 1.15f), new Vector3(0.42f, 0.32f, 0.3f), p.Pale);
            p.Eyes(new Vector3(0f, 1.42f, 1.08f), 0.2f);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pata", new Vector3(0.32f * x, 0.32f, 0.45f * z), new Vector3(0.26f, 0.34f, 0.26f), p.Main));
            p.Tail = p.Add(PrimitiveType.Capsule, "Cola", new Vector3(0f, 0.95f, -0.85f), new Vector3(0.18f, 0.35f, 0.18f), p.Main, new Vector3(-55f, 0f, 0f));
            p.Ears(new Vector3(0f, 1.68f, 0.72f));
        }

        static void Upright(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Capsule, "Cuerpo", new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.62f, 0.72f), p.Main);
            p.Add(PrimitiveType.Sphere, "Barriga", new Vector3(0f, 0.7f, 0.22f), new Vector3(0.6f, 0.75f, 0.4f), p.Pale);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.62f, 0.05f), Vector3.one * 0.82f, p.Main);
            p.Eyes(new Vector3(0f, 1.7f, 0.38f), 0.2f);
            p.Add(PrimitiveType.Capsule, "Brazo", new Vector3(-0.43f, 0.95f, 0.15f), new Vector3(0.18f, 0.25f, 0.18f), p.Main, new Vector3(0f, 0f, 35f));
            p.Add(PrimitiveType.Capsule, "Brazo", new Vector3(0.43f, 0.95f, 0.15f), new Vector3(0.18f, 0.25f, 0.18f), p.Main, new Vector3(0f, 0f, -35f));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(-0.24f, 0.25f, 0.05f), new Vector3(0.3f, 0.25f, 0.32f), p.Main));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(0.24f, 0.25f, 0.05f), new Vector3(0.3f, 0.25f, 0.32f), p.Main));
            p.Tail = p.Add(PrimitiveType.Capsule, "Cola", new Vector3(0f, 0.55f, -0.55f), new Vector3(0.22f, 0.42f, 0.22f), p.Main, new Vector3(-60f, 0f, 0f));
            p.Ears(new Vector3(0f, 2.0f, 0f));
        }

        static void Humanoid(Parts p, bool bigArms)
        {
            p.Body = p.Add(PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.72f, 0.55f, 0.5f), p.Main);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.95f, 0.02f), Vector3.one * 0.62f, p.Main);
            p.Eyes(new Vector3(0f, 2.0f, 0.27f), 0.15f);
            float arm = bigArms ? 0.34f : 0.2f;
            p.Add(PrimitiveType.Capsule, "Brazo", new Vector3(-0.52f, 1.15f, 0f), new Vector3(arm, 0.45f, arm), p.Main, new Vector3(0f, 0f, 12f));
            p.Add(PrimitiveType.Capsule, "Brazo", new Vector3(0.52f, 1.15f, 0f), new Vector3(arm, 0.45f, arm), p.Main, new Vector3(0f, 0f, -12f));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(-0.2f, 0.45f, 0f), new Vector3(0.26f, 0.45f, 0.26f), p.Main));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(0.2f, 0.45f, 0f), new Vector3(0.26f, 0.45f, 0.26f), p.Main));
        }

        static void LongLegs(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 1.35f, 0f), new Vector3(0.9f, 0.75f, 0.9f), p.Main);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 2.0f, 0.15f), Vector3.one * 0.55f, p.Main);
            p.Eyes(new Vector3(0f, 2.05f, 0.4f), 0.14f);
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(-0.25f, 0.55f, 0f), new Vector3(0.16f, 0.6f, 0.16f), p.Dark));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pierna", new Vector3(0.25f, 0.55f, 0f), new Vector3(0.16f, 0.6f, 0.16f), p.Dark));
        }

        static void Bird(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 1.0f, 0f), new Vector3(0.8f, 0.78f, 1.05f), p.Main);
            p.Add(PrimitiveType.Sphere, "Pecho", new Vector3(0f, 0.95f, 0.25f), new Vector3(0.62f, 0.62f, 0.6f), p.Pale);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.6f, 0.45f), Vector3.one * 0.58f, p.Main);
            p.Add(PrimitiveType.Sphere, "Pico", new Vector3(0f, 1.55f, 0.8f), new Vector3(0.16f, 0.13f, 0.32f), Palette.Beak);
            p.Eyes(new Vector3(0f, 1.68f, 0.68f), 0.17f);
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(-0.65f, 1.15f, -0.05f), new Vector3(1.05f, 0.12f, 0.6f), p.Dark));
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(0.65f, 1.15f, -0.05f), new Vector3(1.05f, 0.12f, 0.6f), p.Dark));
            p.Tail = p.Add(PrimitiveType.Sphere, "Cola", new Vector3(0f, 1.0f, -0.65f), new Vector3(0.45f, 0.1f, 0.6f), p.Dark);
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pata", new Vector3(-0.18f, 0.3f, 0.05f), new Vector3(0.1f, 0.3f, 0.1f), Palette.Beak));
            p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pata", new Vector3(0.18f, 0.3f, 0.05f), new Vector3(0.1f, 0.3f, 0.1f), Palette.Beak));
        }

        static void WingedBug(Parts p)
        {
            p.Floating = true;
            p.Body = p.Add(PrimitiveType.Capsule, "Cuerpo", new Vector3(0f, 1.0f, -0.05f), new Vector3(0.5f, 0.45f, 0.5f), p.Main);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.5f, 0.15f), Vector3.one * 0.55f, p.Main);
            p.Eyes(new Vector3(0f, 1.55f, 0.37f), 0.17f, big: true);
            var wingColor = Color.Lerp(p.Pale, Color.white, 0.5f);
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(-0.6f, 1.35f, -0.15f), new Vector3(0.95f, 0.06f, 0.7f), wingColor, new Vector3(0f, 0f, 20f)));
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(0.6f, 1.35f, -0.15f), new Vector3(0.95f, 0.06f, 0.7f), wingColor, new Vector3(0f, 0f, -20f)));
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(-0.5f, 0.95f, -0.2f), new Vector3(0.65f, 0.05f, 0.5f), wingColor, new Vector3(0f, 0f, -15f)));
            p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(0.5f, 0.95f, -0.2f), new Vector3(0.65f, 0.05f, 0.5f), wingColor, new Vector3(0f, 0f, 15f)));
            p.Add(PrimitiveType.Capsule, "Antena", new Vector3(-0.12f, 1.85f, 0.2f), new Vector3(0.04f, 0.2f, 0.04f), p.Dark, new Vector3(20f, 0f, 15f));
            p.Add(PrimitiveType.Capsule, "Antena", new Vector3(0.12f, 1.85f, 0.2f), new Vector3(0.04f, 0.2f, 0.04f), p.Dark, new Vector3(20f, 0f, -15f));
        }

        static void Serpent(Parts p)
        {
            const int count = 7;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float size = Mathf.Lerp(0.72f, 0.32f, t);
                p.Segments.Add(p.Add(PrimitiveType.Sphere, "Segmento", new Vector3(0f, 0.35f + size * 0.2f, -0.45f * i), Vector3.one * size, i % 2 == 0 ? p.Main : p.Pale));
            }
            p.Body = p.Segments[0];
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 0.95f, 0.45f), new Vector3(0.72f, 0.65f, 0.8f), p.Main);
            p.Eyes(new Vector3(0f, 1.08f, 0.78f), 0.2f);
        }

        static void Fish(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.85f, 1.25f), p.Main);
            p.Head = p.Body;
            p.Add(PrimitiveType.Sphere, "Barriga", new Vector3(0f, 0.6f, 0.1f), new Vector3(0.6f, 0.55f, 1.0f), p.Pale);
            p.Tail = p.Add(PrimitiveType.Sphere, "Aleta caudal", new Vector3(0f, 0.8f, -0.8f), new Vector3(0.1f, 0.7f, 0.5f), p.Dark);
            p.Add(PrimitiveType.Sphere, "Aleta dorsal", new Vector3(0f, 1.25f, -0.05f), new Vector3(0.08f, 0.45f, 0.45f), p.Dark);
            p.Eyes(new Vector3(0f, 0.95f, 0.45f), 0.3f);
        }

        static void Ball(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 0.7f, 0f), Vector3.one * 1.25f, p.Main);
            p.Head = p.Body;
            p.Eyes(new Vector3(0f, 0.9f, 0.55f), 0.24f, big: true);
            p.Legs.Add(p.Add(PrimitiveType.Sphere, "Pie", new Vector3(-0.3f, 0.1f, 0.15f), new Vector3(0.35f, 0.2f, 0.45f), p.Dark));
            p.Legs.Add(p.Add(PrimitiveType.Sphere, "Pie", new Vector3(0.3f, 0.1f, 0.15f), new Vector3(0.35f, 0.2f, 0.45f), p.Dark));
        }

        static void Blob(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.85f, 1.3f), p.Main);
            p.Add(PrimitiveType.Sphere, "Gota", new Vector3(-0.55f, 0.2f, 0.45f), Vector3.one * 0.4f, p.Main);
            p.Add(PrimitiveType.Sphere, "Gota", new Vector3(0.6f, 0.18f, -0.3f), Vector3.one * 0.35f, p.Main);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 0.95f, 0.15f), new Vector3(0.75f, 0.6f, 0.7f), p.Main);
            p.Eyes(new Vector3(0f, 1.05f, 0.45f), 0.2f);
        }

        static void Tentacles(Parts p)
        {
            p.Floating = true;
            p.Body = p.Add(PrimitiveType.Sphere, "Cúpula", new Vector3(0f, 1.25f, 0f), new Vector3(1.0f, 0.75f, 1.0f), p.Main);
            p.Head = p.Body;
            p.Add(PrimitiveType.Sphere, "Gema", new Vector3(-0.22f, 1.5f, 0.3f), Vector3.one * 0.2f, Palette.Gem);
            p.Add(PrimitiveType.Sphere, "Gema", new Vector3(0.22f, 1.5f, 0.3f), Vector3.one * 0.2f, Palette.Gem);
            p.Eyes(new Vector3(0f, 1.15f, 0.45f), 0.18f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                p.Segments.Add(p.Add(PrimitiveType.Capsule, "Tentáculo",
                    new Vector3(Mathf.Cos(a) * 0.35f, 0.55f, Mathf.Sin(a) * 0.35f), new Vector3(0.12f, 0.45f, 0.12f), p.Dark));
            }
        }

        static void Heads(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 0.65f, 0f), new Vector3(0.95f, 0.8f, 0.85f), p.Main);
            Vector3[] heads = { new Vector3(-0.45f, 1.35f, 0f), new Vector3(0.45f, 1.35f, 0f), new Vector3(0f, 1.6f, 0.05f) };
            foreach (var position in heads)
            {
                p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", position, Vector3.one * 0.55f, p.Main);
                p.Eyes(position + new Vector3(0f, 0.05f, 0.25f), 0.13f);
            }
        }

        static void Armored(Parts p)
        {
            p.Body = p.Add(PrimitiveType.Sphere, "Concha", new Vector3(0f, 0.6f, -0.05f), new Vector3(1.2f, 0.8f, 1.3f), p.Dark);
            p.Add(PrimitiveType.Sphere, "Borde", new Vector3(0f, 0.35f, -0.05f), new Vector3(1.3f, 0.3f, 1.4f), p.Main);
            p.Head = p.Add(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 0.55f, 0.7f), Vector3.one * 0.5f, p.Pale);
            p.Eyes(new Vector3(0f, 0.65f, 0.92f), 0.13f);
            for (int x = -1; x <= 1; x += 2)
                p.Legs.Add(p.Add(PrimitiveType.Capsule, "Pata", new Vector3(0.4f * x, 0.15f, 0.3f), new Vector3(0.18f, 0.18f, 0.18f), p.Pale));
        }

        // ------------------------------------------------------------------ detalles por tipo

        static void AddTypeDetails(SpeciesData s, Parts p)
        {
            var main = s.Types[0];
            bool hasSecond = s.Types.Length > 1;
            var second = hasSecond ? s.Types[1] : main;

            switch (main)
            {
                case PokemonType.Fuego:
                    Vector3 flameAt = p.Tail != null ? p.TipOf(p.Tail) : p.Top(p.Head);
                    p.Flame = p.Add(PrimitiveType.Sphere, "Llama", flameAt, Vector3.one * 0.32f, new Color(1f, 0.55f, 0.1f));
                    break;

                case PokemonType.Planta:
                    if (s.BodyShape == "quadruped")
                        p.Add(PrimitiveType.Sphere, "Bulbo", new Vector3(0f, 1.25f, -0.2f), new Vector3(0.75f, 0.7f, 0.75f), new Color(0.2f, 0.55f, 0.28f));
                    else if (p.Head != null)
                        p.Add(PrimitiveType.Sphere, "Hoja", p.Top(p.Head), new Vector3(0.45f, 0.08f, 0.25f),
                            new Color(0.3f, 0.7f, 0.3f), new Vector3(0f, 0f, 25f));
                    break;

                case PokemonType.Agua:
                    if (s.BodyShape == "upright")
                        p.Add(PrimitiveType.Sphere, "Caparazón", new Vector3(0f, 0.8f, -0.3f), new Vector3(0.95f, 0.95f, 0.55f), new Color(0.6f, 0.4f, 0.22f));
                    break;

                case PokemonType.Electrico:
                    if (p.Tail != null)
                    {
                        Vector3 tip = p.TipOf(p.Tail);
                        p.Add(PrimitiveType.Cube, "Rayo", tip + new Vector3(0f, 0.15f, 0f), new Vector3(0.08f, 0.35f, 0.25f), Palette.Spark, new Vector3(0f, 0f, 30f));
                        p.Add(PrimitiveType.Cube, "Rayo", tip + new Vector3(0f, 0.4f, -0.05f), new Vector3(0.08f, 0.3f, 0.3f), Palette.Spark, new Vector3(0f, 0f, -30f));
                    }
                    break;

                case PokemonType.Roca:
                case PokemonType.Tierra:
                    for (int i = 0; i < 3; i++)
                        p.Add(PrimitiveType.Sphere, "Roca", new Vector3(-0.3f + i * 0.3f, 1.05f + (i % 2) * 0.15f, -0.25f),
                            Vector3.one * (0.25f + 0.05f * i), new Color(0.55f, 0.5f, 0.45f));
                    break;

                case PokemonType.Psiquico:
                    if (p.Head != null)
                        p.Add(PrimitiveType.Sphere, "Gema", p.Front(p.Head), Vector3.one * 0.14f, Palette.Gem);
                    break;

                case PokemonType.Dragon:
                    Horns(p, new Color(0.95f, 0.9f, 0.75f));
                    break;

                case PokemonType.Hielo:
                    if (p.Head != null)
                        p.Add(PrimitiveType.Cube, "Cristal", p.Top(p.Head), Vector3.one * 0.25f,
                            new Color(0.75f, 0.95f, 1f), new Vector3(45f, 0f, 45f));
                    break;

                case PokemonType.Fantasma:
                    p.Floating = true;
                    break;

                case PokemonType.Lucha:
                    if (p.Body != null)
                        p.Add(PrimitiveType.Cylinder, "Cinturón", p.Unit(p.Body) + new Vector3(0f, -0.2f, 0f),
                            new Vector3(0.78f, 0.05f, 0.58f), new Color(0.15f, 0.15f, 0.2f));
                    break;
            }

            if (main == PokemonType.Veneno || second == PokemonType.Veneno)
                for (int i = 0; i < 3; i++)
                    p.Add(PrimitiveType.Capsule, "Púa", new Vector3(0f, 1.05f + 0.12f * i, -0.25f - 0.12f * i), new Vector3(0.08f, 0.15f, 0.08f),
                        new Color(0.6f, 0.3f, 0.7f), new Vector3(-30f, 0f, 0f));

            if (hasSecond && second == PokemonType.Volador && p.Wings.Count == 0)
            {
                p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(-0.6f, 1.2f, -0.2f), new Vector3(0.85f, 0.08f, 0.5f), p.Dark));
                p.Wings.Add(p.Add(PrimitiveType.Sphere, "Ala", new Vector3(0.6f, 1.2f, -0.2f), new Vector3(0.85f, 0.08f, 0.5f), p.Dark));
            }

            if (hasSecond && second == PokemonType.Dragon && main != PokemonType.Dragon)
                Horns(p, new Color(0.95f, 0.9f, 0.75f));
        }

        static void Horns(Parts p, Color color)
        {
            if (p.Head == null) return;
            Vector3 top = p.Top(p.Head);
            p.Add(PrimitiveType.Capsule, "Cuerno", top + new Vector3(-0.15f, 0.1f, -0.05f), new Vector3(0.07f, 0.18f, 0.07f), color, new Vector3(-20f, 0f, 15f));
            p.Add(PrimitiveType.Capsule, "Cuerno", top + new Vector3(0.15f, 0.1f, -0.05f), new Vector3(0.07f, 0.18f, 0.07f), color, new Vector3(-20f, 0f, -15f));
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>
        /// Piezas del modelo. Todas las posiciones se escriben "en unidades": como si el Pokémon
        /// midiera 1 de tamaño; Add las multiplica por el tamaño real de la especie.
        /// </summary>
        sealed class Parts
        {
            public readonly Transform Model;
            public readonly float Size;
            public readonly Color Main, Pale, Dark;

            public Transform Body, Head, Tail, Flame;
            public bool Floating;
            public readonly System.Collections.Generic.List<Transform> Wings = new System.Collections.Generic.List<Transform>();
            public readonly System.Collections.Generic.List<Transform> Legs = new System.Collections.Generic.List<Transform>();
            public readonly System.Collections.Generic.List<Transform> Segments = new System.Collections.Generic.List<Transform>();

            public Parts(Transform model, Palette palette, float size)
            {
                Model = model;
                Size = size;
                Main = palette.Main;
                Pale = palette.Pale;
                Dark = palette.Dark;
            }

            /// <summary>Posición de una pieza en unidades.</summary>
            public Vector3 Unit(Transform part) => part.localPosition / Size;

            /// <summary>Punto más alto de una pieza en unidades.</summary>
            public Vector3 Top(Transform part) => Unit(part) + new Vector3(0f, part.localScale.y * 0.5f / Size, 0f);

            /// <summary>Extremo de una pieza alargada (cola) siguiendo su rotación, en unidades.</summary>
            public Vector3 TipOf(Transform part) =>
                (part.localPosition + part.localRotation * Vector3.up * part.localScale.y) / Size;

            /// <summary>Frente de una pieza (para gemas en la frente), en unidades.</summary>
            public Vector3 Front(Transform part) => Unit(part) + new Vector3(0f, 0.1f, part.localScale.z * 0.48f / Size);

            /// <summary>Pieza en coordenadas "de figura de 1 de tamaño" que se escalan con la especie.</summary>
            public Transform Add(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
            {
                var go = Vfx.Primitive(type, name, color);
                var t = go.transform;
                t.SetParent(Model, false);
                t.localPosition = position * Size;
                t.localRotation = Quaternion.Euler(euler);
                t.localScale = scale * Size;
                return t;
            }

            public void Eyes(Vector3 center, float spacing, bool big = false)
            {
                float eye = big ? 0.2f : 0.14f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Add(PrimitiveType.Sphere, "Ojo", center + new Vector3(spacing * side, 0f, 0f), Vector3.one * eye, new Color(0.08f, 0.08f, 0.1f));
                    Add(PrimitiveType.Sphere, "Brillo", center + new Vector3(spacing * side + 0.03f, 0.04f, eye * 0.4f), Vector3.one * eye * 0.35f, Color.white);
                }
            }

            /// <summary>Orejas para mamíferos (se omiten en los que no tienen pelo).</summary>
            public void Ears(Vector3 top)
            {
                Add(PrimitiveType.Capsule, "Oreja", top + new Vector3(-0.22f, 0f, 0f), new Vector3(0.14f, 0.24f, 0.1f), Main, new Vector3(0f, 0f, 20f));
                Add(PrimitiveType.Capsule, "Oreja", top + new Vector3(0.22f, 0f, 0f), new Vector3(0.14f, 0.24f, 0.1f), Main, new Vector3(0f, 0f, -20f));
            }

            public void ShinyStar()
            {
                Add(PrimitiveType.Sphere, "Brillo shiny", new Vector3(0f, 2.4f, 0f), Vector3.one * 0.15f, new Color(1f, 0.9f, 0.3f));
            }
        }

        sealed class Palette
        {
            public Color Main, Pale, Dark;

            public static readonly Color Beak = new Color(0.95f, 0.65f, 0.2f);
            public static readonly Color Gem = new Color(0.9f, 0.2f, 0.35f);
            public static readonly Color Spark = new Color(1f, 0.85f, 0.15f);

            public static Palette For(SpeciesData species, bool shiny)
            {
                Color main = FromName(species.BodyColor, species.Types[0].TypeColor());
                if (shiny)
                {
                    Color.RGBToHSV(main, out float h, out float s, out float v);
                    main = Color.HSVToRGB((h + 0.35f) % 1f, Mathf.Clamp01(s * 1.1f + 0.1f), Mathf.Clamp01(v * 1.05f));
                }
                return new Palette
                {
                    Main = main,
                    Pale = Color.Lerp(main, Color.white, 0.45f),
                    Dark = Color.Lerp(main, Color.black, 0.3f)
                };
            }

            static Color FromName(string name, Color fallback)
            {
                switch (name)
                {
                    case "black": return new Color(0.22f, 0.22f, 0.25f);
                    case "blue": return new Color(0.35f, 0.55f, 0.9f);
                    case "brown": return new Color(0.62f, 0.44f, 0.3f);
                    case "gray": return new Color(0.6f, 0.6f, 0.63f);
                    case "green": return new Color(0.4f, 0.72f, 0.45f);
                    case "pink": return new Color(0.96f, 0.66f, 0.76f);
                    case "purple": return new Color(0.6f, 0.42f, 0.76f);
                    case "red": return new Color(0.9f, 0.32f, 0.26f);
                    case "white": return new Color(0.93f, 0.93f, 0.9f);
                    case "yellow": return new Color(0.98f, 0.85f, 0.3f);
                    default: return fallback;
                }
            }
        }
    }
}
