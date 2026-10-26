using System.Collections.Generic;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Colores disponibles para personalizar al entrenador (GDD §12.1, personalización básica).</summary>
    public static class TrainerPalette
    {
        public static readonly Color[] Skin =
        {
            new Color(1f, 0.86f, 0.72f), new Color(0.93f, 0.75f, 0.58f), new Color(0.76f, 0.56f, 0.4f), new Color(0.5f, 0.34f, 0.24f)
        };

        public static readonly Color[] Hair =
        {
            new Color(0.15f, 0.1f, 0.08f), new Color(0.45f, 0.28f, 0.15f), new Color(0.9f, 0.75f, 0.4f),
            new Color(0.75f, 0.2f, 0.15f), new Color(0.3f, 0.35f, 0.75f), new Color(0.85f, 0.85f, 0.85f)
        };

        public static readonly Color[] Clothes =
        {
            new Color(0.85f, 0.15f, 0.15f), new Color(0.2f, 0.35f, 0.75f), new Color(0.2f, 0.6f, 0.3f), new Color(0.95f, 0.8f, 0.2f),
            new Color(0.15f, 0.15f, 0.18f), new Color(0.95f, 0.95f, 0.95f), new Color(0.55f, 0.3f, 0.7f), new Color(0.95f, 0.5f, 0.15f)
        };

        public static Color Pick(Color[] palette, int index) => palette[Mathf.Abs(index) % palette.Length];
    }

    public static class TrainerModelBuilder
    {
        public static Transform Build(Transform parent, Color skin, Color hair, Color cap, Color shirt, Color pants)
        {
            var root = new GameObject("Entrenador").transform;
            root.SetParent(parent, false);

            Transform Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default)
            {
                var t = Vfx.Primitive(type, name, color).transform;
                t.SetParent(root, false);
                t.localPosition = pos;
                t.localRotation = Quaternion.Euler(euler);
                t.localScale = scale;
                return t;
            }

            var legs = new List<Transform>
            {
                Part(PrimitiveType.Capsule, "Pierna", new Vector3(-0.13f, 0.4f, 0f), new Vector3(0.2f, 0.4f, 0.2f), pants),
                Part(PrimitiveType.Capsule, "Pierna", new Vector3(0.13f, 0.4f, 0f), new Vector3(0.2f, 0.4f, 0.2f), pants)
            };
            Part(PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.38f, 0.32f), shirt);
            var arms = new List<Transform>
            {
                Part(PrimitiveType.Capsule, "Brazo", new Vector3(-0.33f, 1.1f, 0f), new Vector3(0.14f, 0.32f, 0.14f), shirt),
                Part(PrimitiveType.Capsule, "Brazo", new Vector3(0.33f, 1.1f, 0f), new Vector3(0.14f, 0.32f, 0.14f), shirt)
            };
            Part(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.72f, 0f), Vector3.one * 0.42f, skin);
            Part(PrimitiveType.Sphere, "Pelo", new Vector3(0f, 1.76f, -0.05f), new Vector3(0.45f, 0.38f, 0.42f), hair);
            Part(PrimitiveType.Sphere, "Gorra", new Vector3(0f, 1.9f, 0f), new Vector3(0.46f, 0.2f, 0.46f), cap);
            Part(PrimitiveType.Cube, "Visera", new Vector3(0f, 1.86f, 0.25f), new Vector3(0.36f, 0.04f, 0.22f), cap);
            Part(PrimitiveType.Sphere, "Ojo", new Vector3(-0.08f, 1.74f, 0.19f), Vector3.one * 0.06f, Color.black);
            Part(PrimitiveType.Sphere, "Ojo", new Vector3(0.08f, 1.74f, 0.19f), Vector3.one * 0.06f, Color.black);
            Part(PrimitiveType.Cube, "Mochila", new Vector3(0f, 1.1f, -0.25f), new Vector3(0.38f, 0.42f, 0.2f), Color.Lerp(pants, Color.black, 0.2f));

            root.gameObject.AddComponent<TrainerFollower>().Setup(legs, arms);
            return root;
        }
    }

    /// <summary>Hace que el entrenador siga a su Pokémon a cierta distancia y anime las piernas al andar.</summary>
    public class TrainerFollower : MonoBehaviour
    {
        const float FollowDistance = 2.4f;
        const float Speed = 7f;

        Transform target;
        List<Transform> legs;
        List<Transform> arms;
        float phase;

        public void Setup(List<Transform> legs, List<Transform> arms)
        {
            this.legs = legs;
            this.arms = arms;
        }

        public void Follow(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
                transform.position = target.position - target.forward * FollowDistance;
        }

        void Update()
        {
            if (target == null) return;

            Vector3 offset = transform.position - target.position;
            offset.y = 0f;
            float moved = 0f;
            if (offset.magnitude > FollowDistance)
            {
                Vector3 desired = target.position + offset.normalized * FollowDistance;
                Vector3 next = Vector3.MoveTowards(transform.position, desired, Speed * Time.deltaTime);
                moved = Vector3.Distance(next, transform.position);
                transform.position = new Vector3(next.x, 0f, next.z);
            }

            Vector3 look = target.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 8f * Time.deltaTime);

            float walking = moved > 0.001f ? 1f : 0f;
            phase += Time.deltaTime * 10f * walking;
            float swing = Mathf.Sin(phase) * 30f * walking;
            for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler(i == 0 ? swing : -swing, 0f, 0f);
            for (int i = 0; i < arms.Count; i++) arms[i].localRotation = Quaternion.Euler(i == 0 ? -swing : swing, 0f, 0f);
        }
    }
}
