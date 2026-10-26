using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>
    /// Escenarios hechos con primitivas: suelo, cielo, luz y decoración según el bioma.
    /// Arena = círculo (Duelo, Práctica, gimnasios). Campo = rectángulo con obstáculos (zonas de la Aventura).
    /// </summary>
    public static class EnvironmentBuilder
    {
        class Look
        {
            public Color Ground, GroundAlt, Sky, Ambient, Fog;
            public float FogDensity;
            public float LightIntensity = 1.1f;
            public Color LightColor = Color.white;
        }

        static Look LookFor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Bosque: return new Look { Ground = C(0.25f, 0.45f, 0.22f), GroundAlt = C(0.3f, 0.52f, 0.26f), Sky = C(0.45f, 0.65f, 0.55f), Ambient = C(0.42f, 0.5f, 0.42f), Fog = C(0.45f, 0.6f, 0.5f), FogDensity = 0.015f, LightIntensity = 0.9f };
                case Biome.Montana: return new Look { Ground = C(0.6f, 0.52f, 0.42f), GroundAlt = C(0.66f, 0.58f, 0.48f), Sky = C(0.6f, 0.75f, 0.92f), Ambient = C(0.58f, 0.56f, 0.55f) };
                case Biome.Cueva: return new Look { Ground = C(0.3f, 0.27f, 0.25f), GroundAlt = C(0.36f, 0.32f, 0.3f), Sky = C(0.06f, 0.05f, 0.07f), Ambient = C(0.28f, 0.26f, 0.32f), Fog = C(0.08f, 0.07f, 0.1f), FogDensity = 0.035f, LightIntensity = 0.55f, LightColor = C(1f, 0.9f, 0.75f) };
                case Biome.Ciudad: return new Look { Ground = C(0.62f, 0.62f, 0.6f), GroundAlt = C(0.42f, 0.62f, 0.36f), Sky = C(0.55f, 0.75f, 0.95f), Ambient = C(0.58f, 0.6f, 0.62f) };
                case Biome.Costa: return new Look { Ground = C(0.93f, 0.85f, 0.62f), GroundAlt = C(0.88f, 0.8f, 0.58f), Sky = C(0.5f, 0.78f, 0.98f), Ambient = C(0.62f, 0.64f, 0.66f), LightIntensity = 1.2f };
                case Biome.Fantasma: return new Look { Ground = C(0.3f, 0.25f, 0.36f), GroundAlt = C(0.35f, 0.29f, 0.42f), Sky = C(0.22f, 0.15f, 0.3f), Ambient = C(0.35f, 0.3f, 0.45f), Fog = C(0.3f, 0.22f, 0.4f), FogDensity = 0.03f, LightIntensity = 0.6f, LightColor = C(0.8f, 0.75f, 1f) };
                case Biome.Safari: return new Look { Ground = C(0.72f, 0.66f, 0.4f), GroundAlt = C(0.6f, 0.62f, 0.32f), Sky = C(0.65f, 0.8f, 0.95f), Ambient = C(0.62f, 0.6f, 0.52f), LightIntensity = 1.2f, LightColor = C(1f, 0.95f, 0.85f) };
                case Biome.Industrial: return new Look { Ground = C(0.45f, 0.47f, 0.5f), GroundAlt = C(0.4f, 0.42f, 0.45f), Sky = C(0.5f, 0.55f, 0.62f), Ambient = C(0.5f, 0.52f, 0.56f) };
                case Biome.Nieve: return new Look { Ground = C(0.9f, 0.93f, 0.97f), GroundAlt = C(0.82f, 0.88f, 0.95f), Sky = C(0.75f, 0.85f, 0.95f), Ambient = C(0.7f, 0.75f, 0.82f), Fog = C(0.85f, 0.9f, 0.95f), FogDensity = 0.012f };
                case Biome.Volcan: return new Look { Ground = C(0.25f, 0.2f, 0.2f), GroundAlt = C(0.32f, 0.25f, 0.22f), Sky = C(0.55f, 0.32f, 0.25f), Ambient = C(0.5f, 0.38f, 0.35f), Fog = C(0.45f, 0.25f, 0.2f), FogDensity = 0.015f, LightColor = C(1f, 0.8f, 0.65f) };
                case Biome.Liga: return new Look { Ground = C(0.72f, 0.7f, 0.78f), GroundAlt = C(0.55f, 0.5f, 0.68f), Sky = C(0.3f, 0.3f, 0.45f), Ambient = C(0.6f, 0.58f, 0.68f) };
                case Biome.Pradera: return new Look { Ground = C(0.42f, 0.66f, 0.35f), GroundAlt = C(0.48f, 0.72f, 0.4f), Sky = C(0.55f, 0.78f, 0.98f), Ambient = C(0.56f, 0.6f, 0.6f) };
                default: return new Look { Ground = C(0.36f, 0.6f, 0.32f), GroundAlt = C(0.45f, 0.68f, 0.4f), Sky = C(0.55f, 0.75f, 0.95f), Ambient = C(0.55f, 0.58f, 0.62f) };
            }
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);

        // ------------------------------------------------------------------ arenas y campos

        /// <summary>Arena circular para combates 1v1 (con público/decoración alrededor).</summary>
        public static Transform BuildArena(Biome biome, Transform parent, float radius = CombatTuning.ArenaRadius)
        {
            var root = NewRoot("Arena " + biome, parent);
            var look = LookFor(biome);
            SetupSky(look, root);
            PlayArea.SetCircle(radius);

            Disc(root, "Suelo", Vector3.down * 0.05f, radius * 2f + 1f, look.Ground, 0.05f);
            Disc(root, "Círculo central", new Vector3(0f, 0.005f, 0f), 6f, look.GroundAlt, 0.005f);
            Block(root, "Línea central", new Vector3(0f, 0.012f, 0f), new Vector3(radius * 2f, 0.01f, 0.15f), Color.Lerp(look.GroundAlt, Color.white, 0.6f));

            const int posts = 36;
            for (int i = 0; i < posts; i++)
            {
                float angle = i * Mathf.PI * 2f / posts;
                var post = Block(root, "Valla", new Vector3(Mathf.Cos(angle) * (radius + 0.3f), 0.4f, Mathf.Sin(angle) * (radius + 0.3f)),
                    new Vector3(0.3f, 0.8f, 2.2f), i % 2 == 0 ? Color.white : Color.Lerp(look.GroundAlt, Color.red, 0.6f));
                post.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            // Decoración fuera de la arena
            var rng = new System.Random((int)biome * 97 + 7);
            for (int i = 0; i < 26; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float distance = radius + 3f + (float)rng.NextDouble() * 8f;
                Decoration(biome, root, new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance), rng, registerObstacle: false);
            }
            Disc(root, "Exterior", Vector3.down * 0.1f, radius * 2f + 30f, look.GroundAlt, 0.05f);
            FieldTheme.Apply(root, radius, radius);
            return root;
        }

        /// <summary>Campo rectangular para explorar (zonas de la Aventura). Los objetos bloquean el paso.</summary>
        public static Transform BuildField(Biome biome, Transform parent, float halfSize, int seed)
        {
            var root = NewRoot("Zona " + biome, parent);
            var look = LookFor(biome);
            SetupSky(look, root);
            PlayArea.SetRectangle(halfSize, halfSize);

            Block(root, "Suelo", new Vector3(0f, -0.05f, 0f), new Vector3(halfSize * 2f + 40f, 0.1f, halfSize * 2f + 40f), look.GroundAlt);
            Block(root, "Zona jugable", new Vector3(0f, -0.04f, 0f), new Vector3(halfSize * 2f, 0.1f, halfSize * 2f), look.Ground);

            var rng = new System.Random(seed);

            // Parches de suelo para dar variedad
            for (int i = 0; i < 14; i++)
            {
                var pos = RandomIn(rng, halfSize - 2f);
                Disc(root, "Parche", pos + Vector3.up * 0.005f, 3f + (float)rng.NextDouble() * 5f, look.GroundAlt, 0.005f);
            }

            // Borde: fila de decoración que marca el límite
            for (float x = -halfSize; x <= halfSize; x += 3.2f)
            {
                Decoration(biome, root, new Vector3(x, 0f, halfSize + 1.5f), rng, registerObstacle: false);
                Decoration(biome, root, new Vector3(x, 0f, -halfSize - 1.5f), rng, registerObstacle: false);
                Decoration(biome, root, new Vector3(halfSize + 1.5f, 0f, x), rng, registerObstacle: false);
                Decoration(biome, root, new Vector3(-halfSize - 1.5f, 0f, x), rng, registerObstacle: false);
            }

            // Interior: obstáculos dispersos, dejando libre el centro (donde aparece el jugador)
            int count = biome == Biome.Bosque ? 55 : biome == Biome.Ciudad ? 14 : 32;
            for (int i = 0; i < count; i++)
            {
                var pos = RandomIn(rng, halfSize - 2f);
                if (pos.magnitude < 6f) continue;
                Decoration(biome, root, pos, rng, registerObstacle: true);
            }

            if (biome == Biome.Ciudad || biome == Biome.Pradera)
                Buildings(root, rng, halfSize, biome == Biome.Ciudad ? 7 : 3);

            AdventureLandscape.Build(root, biome, halfSize, seed);
            FieldTheme.Apply(root, halfSize, halfSize);
            return root;
        }

        // ------------------------------------------------------------------ piezas

        static Transform NewRoot(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        static void SetupSky(Look look, Transform root)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = look.Sky;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = look.Ambient;
            RenderSettings.fog = look.FogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogDensity = look.FogDensity;

            var light = new GameObject("Sol").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.type = LightType.Directional;
            light.intensity = look.LightIntensity;
            light.color = look.LightColor;
            light.shadows = LightShadows.Hard; // más barato que Soft en celulares de gama media
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static Vector3 RandomIn(System.Random rng, float half) =>
            new Vector3(((float)rng.NextDouble() * 2f - 1f) * half, 0f, ((float)rng.NextDouble() * 2f - 1f) * half);

        static Transform Disc(Transform root, string name, Vector3 pos, float diameter, Color color, float height)
        {
            var t = Vfx.Primitive(PrimitiveType.Cylinder, name, color).transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            t.localScale = new Vector3(diameter, height, diameter);
            return t;
        }

        static Transform Block(Transform root, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var t = Vfx.Primitive(PrimitiveType.Cube, name, color).transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            t.localScale = scale;
            return t;
        }

        static Transform Ball(Transform root, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var t = Vfx.Primitive(PrimitiveType.Sphere, name, color).transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            t.localScale = scale;
            return t;
        }

        /// <summary>Un elemento decorativo típico del bioma (árbol, roca, tumba, máquina…).</summary>
        static void Decoration(Biome biome, Transform root, Vector3 pos, System.Random rng, bool registerObstacle)
        {
            float r = 0.8f + (float)rng.NextDouble() * 0.6f;
            float radius;
            switch (biome)
            {
                case Biome.Bosque:
                case Biome.Pradera:
                case Biome.Estadio:
                    radius = Tree(root, pos, r, new Color(0.2f, 0.5f + (float)rng.NextDouble() * 0.15f, 0.22f));
                    break;
                case Biome.Safari:
                    if (rng.NextDouble() < 0.4) radius = Tree(root, pos, r * 1.2f, new Color(0.45f, 0.55f, 0.2f));
                    else radius = Rock(root, pos, r * 0.7f, new Color(0.7f, 0.6f, 0.4f));
                    break;
                case Biome.Nieve:
                    radius = Pine(root, pos, r, new Color(0.9f, 0.95f, 1f));
                    break;
                case Biome.Montana:
                case Biome.Cueva:
                    radius = rng.NextDouble() < 0.5
                        ? Rock(root, pos, r, new Color(0.5f, 0.46f, 0.42f))
                        : Stalagmite(root, pos, r, new Color(0.45f, 0.4f, 0.38f));
                    break;
                case Biome.Fantasma:
                    radius = Grave(root, pos, r);
                    break;
                case Biome.Industrial:
                    radius = Machine(root, pos, r, rng);
                    break;
                case Biome.Volcan:
                    radius = rng.NextDouble() < 0.4 ? Lava(root, pos, r) : Rock(root, pos, r, new Color(0.2f, 0.17f, 0.17f));
                    break;
                case Biome.Costa:
                    radius = rng.NextDouble() < 0.5 ? Palm(root, pos, r) : Rock(root, pos, r * 0.6f, new Color(0.6f, 0.58f, 0.55f));
                    break;
                case Biome.Ciudad:
                    radius = Tree(root, pos, r * 0.8f, new Color(0.25f, 0.55f, 0.28f));
                    break;
                case Biome.Liga:
                    radius = Pillar(root, pos, r);
                    break;
                default:
                    radius = Tree(root, pos, r, new Color(0.25f, 0.55f, 0.25f));
                    break;
            }

            if (registerObstacle && radius > 0f) PlayArea.AddObstacle(pos, radius);
        }

        static float Tree(Transform root, Vector3 pos, float r, Color leaves)
        {
            Block(root, "Tronco", pos + new Vector3(0f, 0.9f * r, 0f), new Vector3(0.35f, 1.8f, 0.35f) * r, new Color(0.45f, 0.3f, 0.18f));
            Ball(root, "Copa", pos + new Vector3(0f, 2.3f * r, 0f), new Vector3(1.9f, 1.6f, 1.9f) * r, leaves);
            Ball(root, "Copa", pos + new Vector3(0.35f * r, 2.9f * r, 0.1f), Vector3.one * 1.1f * r, Color.Lerp(leaves, Color.white, 0.12f));
            return 0.45f * r;
        }

        static float Pine(Transform root, Vector3 pos, float r, Color snow)
        {
            Block(root, "Tronco", pos + new Vector3(0f, 0.6f * r, 0f), new Vector3(0.3f, 1.2f, 0.3f) * r, new Color(0.4f, 0.28f, 0.18f));
            for (int i = 0; i < 3; i++)
            {
                float size = (1.8f - i * 0.45f) * r;
                Ball(root, "Copa", pos + new Vector3(0f, (1.3f + i * 0.75f) * r, 0f), new Vector3(size, 0.7f * r, size),
                    Color.Lerp(new Color(0.15f, 0.4f, 0.25f), snow, 0.15f + i * 0.25f));
            }
            return 0.45f * r;
        }

        static float Palm(Transform root, Vector3 pos, float r)
        {
            var trunk = Block(root, "Tronco", pos + new Vector3(0.2f * r, 1.3f * r, 0f), new Vector3(0.3f, 2.6f, 0.3f) * r, new Color(0.6f, 0.45f, 0.28f));
            trunk.localRotation = Quaternion.Euler(0f, 0f, -8f);
            for (int i = 0; i < 5; i++)
            {
                var leaf = Ball(root, "Hoja", pos + new Vector3(0.4f * r, 2.65f * r, 0f), new Vector3(1.8f, 0.12f, 0.45f) * r, new Color(0.25f, 0.6f, 0.3f));
                leaf.localRotation = Quaternion.Euler(0f, i * 72f, -15f);
            }
            return 0.35f * r;
        }

        static float Rock(Transform root, Vector3 pos, float r, Color color)
        {
            Ball(root, "Roca", pos + new Vector3(0f, 0.35f * r, 0f), new Vector3(1.6f, 0.9f, 1.3f) * r, color);
            Ball(root, "Roca", pos + new Vector3(0.5f * r, 0.25f * r, 0.3f * r), new Vector3(0.8f, 0.6f, 0.7f) * r, Color.Lerp(color, Color.white, 0.1f));
            return 0.75f * r;
        }

        static float Stalagmite(Transform root, Vector3 pos, float r, Color color)
        {
            var t = Vfx.Primitive(PrimitiveType.Capsule, "Estalagmita", color).transform;
            t.SetParent(root, false);
            t.localPosition = pos + new Vector3(0f, 1f * r, 0f);
            t.localScale = new Vector3(0.6f, 1.3f, 0.6f) * r;
            Ball(root, "Cristal", pos + new Vector3(0.4f * r, 0.3f * r, 0f), Vector3.one * 0.3f * r, new Color(0.55f, 0.75f, 1f));
            return 0.35f * r;
        }

        static float Grave(Transform root, Vector3 pos, float r)
        {
            Block(root, "Lápida", pos + new Vector3(0f, 0.55f * r, 0f), new Vector3(0.8f, 1.1f, 0.25f) * r, new Color(0.55f, 0.55f, 0.6f));
            Ball(root, "Lápida", pos + new Vector3(0f, 1.1f * r, 0f), new Vector3(0.8f, 0.4f, 0.25f) * r, new Color(0.55f, 0.55f, 0.6f));
            Ball(root, "Fuego fatuo", pos + new Vector3(0.6f * r, 1.6f * r, 0f), Vector3.one * 0.18f, new Color(0.6f, 0.45f, 1f));
            return 0.45f * r;
        }

        static float Machine(Transform root, Vector3 pos, float r, System.Random rng)
        {
            Block(root, "Máquina", pos + new Vector3(0f, 0.7f * r, 0f), new Vector3(1.4f, 1.4f, 1.1f) * r, new Color(0.55f, 0.58f, 0.62f));
            Block(root, "Panel", pos + new Vector3(0f, 0.95f * r, 0.56f * r), new Vector3(0.9f, 0.4f, 0.04f) * r,
                rng.NextDouble() < 0.5 ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 0.85f, 1f));
            return 0.8f * r;
        }

        static float Lava(Transform root, Vector3 pos, float r)
        {
            Disc(root, "Lava", pos + new Vector3(0f, 0.01f, 0f), 2.4f * r, new Color(1f, 0.4f, 0.05f), 0.01f);
            Disc(root, "Borde", pos, 2.8f * r, new Color(0.15f, 0.12f, 0.12f), 0.008f);
            return 1.2f * r;
        }

        static float Pillar(Transform root, Vector3 pos, float r)
        {
            var t = Vfx.Primitive(PrimitiveType.Cylinder, "Columna", new Color(0.85f, 0.83f, 0.9f)).transform;
            t.SetParent(root, false);
            t.localPosition = pos + new Vector3(0f, 1.6f * r, 0f);
            t.localScale = new Vector3(0.8f, 1.6f, 0.8f) * r;
            Block(root, "Capitel", pos + new Vector3(0f, 3.25f * r, 0f), new Vector3(1.1f, 0.25f, 1.1f) * r, new Color(0.95f, 0.85f, 0.4f));
            return 0.45f * r;
        }

        static void Buildings(Transform root, System.Random rng, float halfSize, int count)
        {
            Color[] roofs = { new Color(0.75f, 0.25f, 0.2f), new Color(0.25f, 0.4f, 0.75f), new Color(0.3f, 0.6f, 0.35f) };
            for (int i = 0; i < count; i++)
            {
                var pos = RandomIn(rng, halfSize - 5f);
                if (pos.magnitude < 9f) continue;
                Block(root, "Casa", pos + new Vector3(0f, 1.2f, 0f), new Vector3(4f, 2.4f, 3.5f), new Color(0.95f, 0.92f, 0.85f));
                var roof = Block(root, "Tejado", pos + new Vector3(0f, 2.9f, 0f), new Vector3(3f, 3f, 3.7f), roofs[rng.Next(roofs.Length)]);
                roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
                roof.localScale = new Vector3(2.9f, 2.9f, 3.7f);
                Block(root, "Puerta", pos + new Vector3(0f, 0.6f, 1.76f), new Vector3(0.8f, 1.2f, 0.05f), new Color(0.45f, 0.3f, 0.2f));
                PlayArea.AddObstacle(pos, 2.4f);
            }
        }
    }
}
