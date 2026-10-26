using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Efectos visuales provisionales hechos con primitivas. Se sustituyen por partículas en la fase de arte.</summary>
    public static class Vfx
    {
        public static GameObject Primitive(PrimitiveType type, string name, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }

        /// <summary>Esfera que crece y desaparece (impactos y explosiones).</summary>
        public static void Burst(Vector3 position, Color color, float size)
        {
            var go = Primitive(PrimitiveType.Sphere, "Burst", color);
            go.transform.position = position;
            go.AddComponent<TimedScale>().Setup(Vector3.one * size * 0.3f, Vector3.one * size, 0.2f);
        }

        /// <summary>Tajo blanco delante del atacante.</summary>
        public static void Slash(Vector3 position, Vector3 direction, Color color, float width)
        {
            var go = Primitive(PrimitiveType.Cube, "Slash", color);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(direction);
            go.AddComponent<TimedScale>().Setup(new Vector3(width * 0.3f, 0.1f, 0.3f), new Vector3(width, 0.1f, 0.3f), 0.15f);
        }

        /// <summary>Abanico de esferas que salen en cono.</summary>
        public static void ConeBurst(Vector3 origin, Vector3 direction, float range, float angleDegrees, Color color)
        {
            const int rays = 7;
            for (int i = 0; i < rays; i++)
            {
                float angle = Mathf.Lerp(-angleDegrees * 0.5f, angleDegrees * 0.5f, i / (float)(rays - 1));
                Vector3 ray = Quaternion.Euler(0f, angle, 0f) * direction;
                var go = Primitive(PrimitiveType.Sphere, "ConeParticle", color);
                go.transform.position = origin;
                go.AddComponent<TimedScale>().Setup(Vector3.one * 0.4f, Vector3.one * 1.2f, 0.3f, origin + ray * range);
            }
        }
    }
}
