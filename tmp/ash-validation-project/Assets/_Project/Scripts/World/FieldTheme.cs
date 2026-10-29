using System;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Aspecto del campo compartido por Aventura, Duelo, Práctica y MOBA.</summary>
    public static class FieldTheme
    {
        const string Preference = "PokeMoba.FieldType";
        public static PokemonType? Selected
        {
            get
            {
                int value = PlayerPrefs.GetInt(Preference, -1);
                return value >= 0 && value < Enum.GetValues(typeof(PokemonType)).Length
                    ? (PokemonType?)value : null;
            }
            set
            {
                PlayerPrefs.SetInt(Preference, value.HasValue ? (int)value.Value : -1);
                PlayerPrefs.Save();
            }
        }

        public static string Name => Selected.HasValue ? Selected.Value.DisplayName() : "Según la zona";

        public static void Apply(Transform root, float halfX, float halfZ)
        {
            if (!Selected.HasValue) return;
            var type = Selected.Value;
            Color color = type.TypeColor();
            root.name += " · Campo " + type.DisplayName();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                string name = renderer.gameObject.name;
                if (name == "Suelo" || name == "Campo" || name == "Zona jugable" || name == "Exterior")
                    renderer.material.color = Color.Lerp(color, new Color(0.25f, 0.25f, 0.25f), 0.35f);
                else if (name == "Parche" || name == "Círculo central")
                    renderer.material.color = Color.Lerp(color, Color.white, 0.3f);
            }
            if (Camera.main != null)
                Camera.main.backgroundColor = Color.Lerp(color, new Color(0.6f, 0.75f, 0.9f), 0.65f);

            // Hitos fuera del área jugable: no alteran colisiones, carriles ni puntos de aparición.
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                var shape = type == PokemonType.Roca || type == PokemonType.Tierra ? PrimitiveType.Cube
                    : type == PokemonType.Planta || type == PokemonType.Bicho ? PrimitiveType.Capsule
                    : type == PokemonType.Acero || type == PokemonType.Electrico ? PrimitiveType.Cylinder
                    : PrimitiveType.Sphere;
                var marker = GameObject.CreatePrimitive(shape);
                marker.name = "Hito " + type.DisplayName();
                marker.transform.SetParent(root, false);
                marker.transform.localPosition = new Vector3(Mathf.Cos(angle) * (halfX + 4f), 1.5f, Mathf.Sin(angle) * (halfZ + 4f));
                marker.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
                marker.GetComponent<Collider>().enabled = false;
                var material = marker.GetComponent<Renderer>().material;
                material.color = Color.Lerp(color, Color.white, i % 2 == 0 ? 0.15f : 0.45f);
            }
        }
    }
}
