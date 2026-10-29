using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PokeMoba.World
{
    /// <summary>Arte procedural de Aventura. Recursos propiedad de cada escenario.</summary>
    public class AdventureLandscape : MonoBehaviour
    {
        readonly List<Object> resources = new List<Object>();
        T Keep<T>(T value) where T : Object { resources.Add(value); return value; }
        void OnDestroy() { foreach (var item in resources) if (item != null) Destroy(item); }

        public static void Build(Transform root, Biome biome, float half, int seed)
        {
            var art = root.gameObject.AddComponent<AdventureLandscape>();
            art.Create(biome, half, seed);
        }

        Material Matte(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = Keep(new Material(shader));
            material.color = color;
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Smoothness", 0f);
            return material;
        }

        void Create(Biome biome, float half, int seed)
        {
            bool outdoors = biome != Biome.Cueva && biome != Biome.Fantasma && biome != Biome.Industrial && biome != Biome.Liga;
            bool grassy = biome == Biome.Pradera || biome == Biome.Bosque || biome == Biome.Safari;
            Color ground = biome == Biome.Nieve ? new Color(.76f, .83f, .85f)
                : biome == Biome.Costa ? new Color(.56f, .49f, .33f)
                : biome == Biome.Volcan ? new Color(.21f, .19f, .18f)
                : grassy ? new Color(.26f, .33f, .17f) : new Color(.36f, .34f, .29f);
            var groundMaterial = Matte(Color.white);
            var texture = Keep(new Texture2D(256, 256, TextureFormat.RGB24, true));
            texture.wrapMode = TextureWrapMode.Repeat;
            var pixels = new Color[256 * 256];
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    float noise = Mathf.PerlinNoise(x * .06f + seed, y * .06f) * .65f + Mathf.PerlinNoise(x * .5f, y * .5f) * .35f;
                    pixels[y * 256 + x] = Color.Lerp(ground * .65f, ground * 1.45f, noise);
                }
            texture.SetPixels(pixels); texture.Apply(true, true);
            groundMaterial.mainTexture = texture;
            groundMaterial.mainTextureScale = new Vector2(18f, 18f);
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                string name = renderer.name;
                if (name == "Parche") renderer.gameObject.SetActive(false);
                if (name == "Suelo" || name == "Zona jugable") renderer.sharedMaterial = groundMaterial;
                else if (name == "Copa" || name == "Tronco" || name == "Roca")
                {
                    renderer.material.color = Color.Lerp(renderer.material.color, ground, .22f);
                    renderer.material.SetFloat("_Glossiness", 0f);
                    renderer.material.SetFloat("_Smoothness", 0f);
                }
            }
            if (grassy) Grass(half, seed, biome == Biome.Safari);
            if (!outdoors) return;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .65f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.38f, .42f, .35f);
            RenderSettings.ambientGroundColor = new Color(.19f, .20f, .15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = half * 1.5f;
            RenderSettings.fogEndDistance = half * 6f;
            RenderSettings.fogColor = new Color(.58f, .69f, .74f);
            foreach (var light in GetComponentsInChildren<Light>())
            {
                light.color = new Color(1f, .91f, .76f);
                light.intensity = 1.05f;
                light.shadows = Application.isMobilePlatform ? LightShadows.Hard : LightShadows.Soft;
                light.shadowStrength = .7f;
                light.transform.rotation = Quaternion.Euler(34f, -35f, 0f);
            }
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = Keep(new Material(skyShader));
                sky.SetFloat("_AtmosphereThickness", 1.1f);
                sky.SetColor("_SkyTint", new Color(.55f, .62f, .68f));
                sky.SetColor("_GroundColor", ground);
                RenderSettings.skybox = sky;
                if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
            }
            Horizon(half, seed);
        }

        void Grass(float half, int seed, bool dry)
        {
            var rng = new System.Random(seed);
            var vertices = new List<Vector3>(); var indices = new List<int>();
            int count = Application.isMobilePlatform ? 2600 : 7000;
            for (int i = 0; i < count; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                // Sendero sinuoso: espacio abierto para leer al entrenador y al compañero.
                if (Mathf.Abs(x - Mathf.Sin(z * .12f) * 3f) < 1.7f || !PokeMoba.Combat.PlayArea.IsFree(new Vector3(x, 0f, z), .3f)) continue;
                float height = .16f + (float)rng.NextDouble() * .48f;
                for (int blade = 0; blade < 2; blade++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI;
                    var side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * .065f;
                    var pos = new Vector3(x, .025f, z);
                    int n = vertices.Count;
                    vertices.Add(pos - side); vertices.Add(pos + side);
                    vertices.Add(pos + new Vector3(.1f, height, .04f));
                    indices.Add(n); indices.Add(n + 2); indices.Add(n + 1);
                    indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
                }
            }
            MeshObject("Hierba", vertices, indices, Matte(dry ? new Color(.55f, .48f, .23f) : new Color(.34f, .43f, .19f)), false);
        }

        void Horizon(float half, int seed)
        {
            var rng = new System.Random(seed + 82);
            for (int layer = 0; layer < 2; layer++)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                const int segments = 64;
                float radius = half * (2.7f + layer * 1.4f);
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float height = 9f + (float)rng.NextDouble() * 22f + layer * 8f;
                    var position = new Vector3(Mathf.Cos(angle) * radius, -2f, Mathf.Sin(angle) * radius);
                    v.Add(position); v.Add(position * .88f + Vector3.up * height);
                    if (i == 0) continue;
                    int n = i * 2;
                    t.AddRange(new[] { n - 2, n - 1, n, n, n - 1, n + 1 });
                    t.AddRange(new[] { n, n - 1, n - 2, n + 1, n - 1, n });
                }
                MeshObject("Montañas lejanas", v, t, Matte(layer == 0 ? new Color(.29f, .37f, .36f) : new Color(.43f, .51f, .53f)), false);
            }
        }

        void MeshObject(string name, List<Vector3> vertices, List<int> triangles, Material material, bool shadows)
        {
            // Normales independientes para las dos caras de cada hoja.
            var faces = new Vector3[triangles.Count];
            var indices = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++) { faces[i] = vertices[triangles[i]]; indices[i] = i; }
            var mesh = Keep(new Mesh { name = name, indexFormat = faces.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 });
            mesh.vertices = faces; mesh.triangles = indices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }
}
