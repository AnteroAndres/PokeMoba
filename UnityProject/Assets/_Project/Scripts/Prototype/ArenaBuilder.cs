using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>Arena circular provisional, luz y ambiente.</summary>
    public static class ArenaBuilder
    {
        public static void Build()
        {
            var arena = new GameObject("Arena").transform;
            float r = CombatTuning.ArenaRadius;

            var floor = Vfx.Primitive(PrimitiveType.Cylinder, "Suelo", new Color(0.36f, 0.6f, 0.32f));
            floor.transform.SetParent(arena, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(r * 2f + 1f, 0.05f, r * 2f + 1f);

            var center = Vfx.Primitive(PrimitiveType.Cylinder, "Círculo central", new Color(0.45f, 0.68f, 0.4f));
            center.transform.SetParent(arena, false);
            center.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            center.transform.localScale = new Vector3(6f, 0.005f, 6f);

            var line = Vfx.Primitive(PrimitiveType.Cube, "Línea central", new Color(0.85f, 0.9f, 0.8f));
            line.transform.SetParent(arena, false);
            line.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            line.transform.localScale = new Vector3(r * 2f, 0.01f, 0.15f);

            const int posts = 36;
            for (int i = 0; i < posts; i++)
            {
                float angle = i * Mathf.PI * 2f / posts;
                var post = Vfx.Primitive(PrimitiveType.Cube, "Valla", i % 2 == 0 ? Color.white : new Color(0.85f, 0.2f, 0.2f));
                post.transform.SetParent(arena, false);
                post.transform.localPosition = new Vector3(Mathf.Cos(angle) * (r + 0.3f), 0.4f, Mathf.Sin(angle) * (r + 0.3f));
                post.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                post.transform.localScale = new Vector3(0.3f, 0.8f, 2.2f);
            }

            var light = new GameObject("Sol").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
        }
    }
}
