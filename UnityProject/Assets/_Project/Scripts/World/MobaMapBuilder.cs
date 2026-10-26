using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>
    /// Mapa del MOBA (GDD §7.3): 3 carriles entre dos bases, río en el centro y jungla con árboles.
    /// El equipo 0 (azul) está a la izquierda (x negativa) y el 1 (rojo) a la derecha.
    /// </summary>
    public static class MobaMapBuilder
    {
        public const float HalfX = 40f;
        public const float HalfZ = 24f;
        public const float LaneZ = 15f;
        public const float BaseX = 34f;
        public const float TowerX = 17f;

        public static readonly string[] LaneNames = { "Superior", "Central", "Inferior" };

        /// <summary>Recorrido de cada carril desde la base azul hasta la roja.</summary>
        public static Vector3[] LanePath(int lane)
        {
            float z = lane == 0 ? LaneZ : lane == 2 ? -LaneZ : 0f;
            if (lane == 1) return new[] { new Vector3(-BaseX, 0f, 0f), new Vector3(BaseX, 0f, 0f) };
            return new[]
            {
                new Vector3(-BaseX, 0f, 0f), new Vector3(-BaseX + 8f, 0f, z),
                new Vector3(BaseX - 8f, 0f, z), new Vector3(BaseX, 0f, 0f)
            };
        }

        /// <summary>Recorrido para un equipo (el rojo lo hace al revés).</summary>
        public static Vector3[] PathFor(int lane, int team)
        {
            var path = LanePath(lane);
            if (team == 1) System.Array.Reverse(path);
            return path;
        }

        public static Vector3 TowerPosition(int lane, int team)
        {
            float z = lane == 0 ? LaneZ : lane == 2 ? -LaneZ : 0f;
            return new Vector3(team == 0 ? -TowerX : TowerX, 0f, z);
        }

        public static Vector3 CorePosition(int team) => new Vector3(team == 0 ? -BaseX : BaseX, 0f, 0f);

        public static Vector3 SpawnPoint(int team, int lane)
        {
            float z = (lane - 1) * -2.5f;
            return new Vector3(team == 0 ? -BaseX - 3f : BaseX + 3f, 0f, z);
        }

        public static Transform Build(Transform parent, Color blue, Color red)
        {
            var root = new GameObject("Mapa MOBA").transform;
            root.SetParent(parent, false);
            PlayArea.SetRectangle(HalfX, HalfZ);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.45f, 0.65f, 0.85f);
            }
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.6f, 0.62f);
            var light = new GameObject("Sol").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Hard; // más barato que Soft en celulares de gama media
            light.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            Block(root, "Suelo", new Vector3(0f, -0.06f, 0f), new Vector3(HalfX * 2f + 30f, 0.1f, HalfZ * 2f + 30f), new Color(0.22f, 0.4f, 0.22f));
            Block(root, "Campo", new Vector3(0f, -0.05f, 0f), new Vector3(HalfX * 2f, 0.1f, HalfZ * 2f), new Color(0.32f, 0.55f, 0.3f));

            // Río
            Block(root, "Río", new Vector3(0f, -0.01f, 0f), new Vector3(5f, 0.04f, HalfZ * 2f), new Color(0.3f, 0.55f, 0.85f));

            // Carriles
            var laneColor = new Color(0.78f, 0.7f, 0.5f);
            for (int lane = 0; lane < 3; lane++)
            {
                var path = LanePath(lane);
                for (int i = 0; i < path.Length - 1; i++) Segment(root, path[i], path[i + 1], 4.5f, laneColor);
            }

            // Bases
            Disc(root, "Base azul", CorePosition(0) + Vector3.up * 0.01f, 13f, Color.Lerp(blue, Color.white, 0.4f));
            Disc(root, "Base roja", CorePosition(1) + Vector3.up * 0.01f, 13f, Color.Lerp(red, Color.white, 0.4f));

            // Jungla: árboles entre carriles (bloquean el paso)
            var rng = new System.Random(151);
            for (int i = 0; i < 70; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * (TowerX + 4f);
                float z = ((float)rng.NextDouble() * 2f - 1f) * (HalfZ - 1f);
                if (Mathf.Abs(x) < 3.5f) continue;                                    // río
                if (Mathf.Abs(z) < 3.5f || Mathf.Abs(Mathf.Abs(z) - LaneZ) < 3.5f) continue; // carriles
                Tree(root, new Vector3(x, 0f, z), 0.8f + (float)rng.NextDouble() * 0.5f);
            }
            FieldTheme.Apply(root, HalfX, HalfZ);
            return root;
        }

        static void Segment(Transform root, Vector3 a, Vector3 b, float width, Color color)
        {
            Vector3 direction = b - a;
            var t = Block(root, "Carril", (a + b) * 0.5f + Vector3.up * 0.005f, new Vector3(width, 0.02f, direction.magnitude + width), color);
            t.rotation = Quaternion.LookRotation(direction);
        }

        static void Tree(Transform root, Vector3 position, float r)
        {
            Block(root, "Tronco", position + new Vector3(0f, 0.9f * r, 0f), new Vector3(0.35f, 1.8f, 0.35f) * r, new Color(0.45f, 0.3f, 0.18f));
            var crown = Vfx.Primitive(PrimitiveType.Sphere, "Copa", new Color(0.18f, 0.45f, 0.2f)).transform;
            crown.SetParent(root, false);
            crown.localPosition = position + new Vector3(0f, 2.3f * r, 0f);
            crown.localScale = new Vector3(2f, 1.7f, 2f) * r;
            PlayArea.AddObstacle(position, 0.5f * r);
        }

        static Transform Block(Transform root, string name, Vector3 position, Vector3 scale, Color color)
        {
            var t = Vfx.Primitive(PrimitiveType.Cube, name, color).transform;
            t.SetParent(root, false);
            t.localPosition = position;
            t.localScale = scale;
            return t;
        }

        static void Disc(Transform root, string name, Vector3 position, float diameter, Color color)
        {
            var t = Vfx.Primitive(PrimitiveType.Cylinder, name, color).transform;
            t.SetParent(root, false);
            t.localPosition = position;
            t.localScale = new Vector3(diameter, 0.01f, diameter);
        }
    }
}
