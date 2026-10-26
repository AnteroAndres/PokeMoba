using System.Collections.Generic;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Entrenador original de exploración: mallas de ropa, articulaciones y accesorios.</summary>
    public class TrainerOutfit : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        Transform scarfTail;
        Quaternion scarfRest;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        void OnDestroy() { foreach (var item in owned) if (item != null) Destroy(item); }

        void LateUpdate()
        {
            if (scarfTail == null) return;
            scarfTail.localRotation = scarfRest * Quaternion.Euler(Mathf.Sin(Time.time * 3.5f) * 4f, Mathf.Sin(Time.time * 2.1f) * 5f, 0f);
        }

        Material Fabric(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color; material.SetFloat("_Glossiness", .08f); material.SetFloat("_Smoothness", .08f);
            owned.Add(material); materials.Add(color, material); return material;
        }

        // Perfil elíptico: radio X, altura Y, radio Z. Cierra los extremos sin colliders.
        Transform Shape(string name, Transform parent, Vector3 pos, Color color, params Vector3[] rings)
        {
            const int sides = 16;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int r = 0; r < rings.Length; r++)
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    v.Add(new Vector3(Mathf.Cos(a) * rings[r].x, rings[r].y, Mathf.Sin(a) * rings[r].z));
                    uv.Add(new Vector2(i / (float)sides, r / (float)(rings.Length - 1)));
                    if (r == 0 || i == sides) continue;
                    int n = r * (sides + 1) + i;
                    triangles.AddRange(new[] { n, n + 1, n - sides - 1, n + 1, n - sides, n - sides - 1 });
                }
            var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = Fabric(color);
            return go.transform;
        }
        Transform Oval(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var rings = new Vector3[9];
            for (int i = 0; i < rings.Length; i++)
            {
                float a = i * Mathf.PI / (rings.Length - 1);
                rings[i] = new Vector3(Mathf.Sin(a) * size.x, -Mathf.Cos(a) * size.y, Mathf.Sin(a) * size.z);
            }
            return Shape(name, parent, pos, color, rings);
        }
        Transform Joint(string name, Vector3 pos)
        {
            var t = new GameObject(name).transform; t.SetParent(transform, false); t.localPosition = pos; return t;
        }

        public void Build(Color skin, Color hair, Color cap, Color shirt, Color pants,
            out List<Transform> legs, out List<Transform> arms, PokeMoba.Game.SaveData appearance = null)
        {
            int avatar = Mathf.Clamp(appearance?.AvatarStyle ?? 0, 0, 2);
            int hairstyle = Mathf.Clamp(appearance?.HairStyle ?? 0, 0, 3);
            int outfit = Mathf.Clamp(appearance?.OutfitStyle ?? 0, 0, 2);
            int hat = Mathf.Clamp(appearance?.HatStyle ?? 0, 0, 2);
            var eye = TrainerPalette.Pick(TrainerPalette.Eyes, appearance?.EyeColor ?? 0);
            var navy = Color.Lerp(shirt, Color.black, .25f);
            var trousers = Color.Lerp(pants, Color.black, .35f);
            var red = TrainerPalette.Pick(TrainerPalette.Clothes, appearance?.ScarfColor ?? 0);
            var cream = new Color(.88f, .84f, .69f);
            var leather = new Color(.52f, .34f, .15f);
            var sole = new Color(.75f, .60f, .29f);
            legs = new List<Transform>(); arms = new List<Transform>();
            Shape("Veste de voyage", transform, new Vector3(0f, .87f, 0f), navy,
                new Vector3(0f, 0f, 0f), new Vector3(.26f, 0f, .17f), new Vector3(.22f, .14f, .15f),
                new Vector3(.27f, .43f, .17f), new Vector3(.19f, .52f, .12f), new Vector3(0f, .54f, 0f));
            Shape("Cuello", transform, new Vector3(0f, 1.37f, 0f), skin,
                new Vector3(0f, 0f, 0f), new Vector3(.085f, 0f, .08f), new Vector3(.08f, .15f, .075f), new Vector3(0f, .15f, 0f));
            // Cinturón y riñonera visibles desde la cámara trasera.
            Shape("Cinturón", transform, new Vector3(0f, .98f, 0f), leather,
                new Vector3(0f, 0f, 0f), new Vector3(.24f, 0f, .175f), new Vector3(.24f, .06f, .175f), new Vector3(0f, .06f, 0f));
            Oval("Bolsa de exploración", transform, new Vector3(.05f, 1.03f, -.21f), new Vector3(.20f, .105f, .08f), sole);
            Oval("Solapa de bolsa", transform, new Vector3(.05f, 1.07f, -.27f), new Vector3(.17f, .055f, .018f), cream);
            Oval("Broche", transform, new Vector3(.05f, 1.04f, -.29f), new Vector3(.023f, .028f, .01f), leather);
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = Joint("Cadera", new Vector3(side * .135f, .9f, 0f)); legs.Add(leg);
                Shape("Pantalón", leg, Vector3.zero, trousers,
                    new Vector3(0f, 0f, 0f), new Vector3(.12f, -.02f, .13f), new Vector3(.115f, -.34f, .12f),
                    new Vector3(.08f, -.65f, .085f), new Vector3(0f, -.66f, 0f));
                Shape("Bota", leg, new Vector3(0f, -.66f, 0f), leather,
                    new Vector3(0f, 0f, 0f), new Vector3(.09f, .10f, .09f), new Vector3(.10f, -.12f, .14f), new Vector3(0f, -.14f, 0f));
                Oval("Zapato", leg, new Vector3(0f, -.76f, .075f), new Vector3(.115f, .09f, .21f), leather);
                Oval("Suela", leg, new Vector3(0f, -.82f, .075f), new Vector3(.12f, .033f, .215f), sole);
                var arm = Joint("Hombro", new Vector3(side * .27f, 1.30f, 0f)); arms.Add(arm);
                Shape("Manga", arm, Vector3.zero, navy,
                    new Vector3(0f, .07f, 0f), new Vector3(.11f, 0f, .115f), new Vector3(.09f, -.29f, .10f),
                    new Vector3(.08f, -.40f, .09f), new Vector3(0f, -.41f, 0f));
                Shape("Puño claro", arm, new Vector3(0f, -.36f, 0f), cream,
                    new Vector3(0f, 0f, 0f), new Vector3(.097f, 0f, .106f), new Vector3(.097f, -.09f, .106f), new Vector3(0f, -.09f, 0f));
                Oval("Mano", arm, new Vector3(0f, -.49f, .015f), new Vector3(.07f, .095f, .055f), skin);
                Oval("Pulgar", arm, new Vector3(-side * .06f, -.47f, .04f), new Vector3(.028f, .05f, .03f), skin);
                Oval("Oreja", transform, new Vector3(side * .205f, 1.66f, 0f), new Vector3(.045f, .065f, .035f), skin);
            }
            Oval("Rostro", transform, new Vector3(0f, 1.69f, .015f), new Vector3(.195f, .235f, .18f), skin);
            if (hairstyle != 3) Oval("Cabello trasero", transform, new Vector3(0f, 1.75f, -.07f), new Vector3(.207f, .18f, .16f), hair);
            for (int i = -2; hairstyle != 3 && i <= 2; i++)
                Oval("Mechón", transform, new Vector3(i * .07f, 1.64f + Mathf.Abs(i) * .015f, -.13f), new Vector3(.055f, .09f, .06f), hair);
            for (int side = -1; side <= 1; side += 2)
            {
                Oval("Blanco del ojo", transform, new Vector3(side * .076f, 1.71f, .177f), new Vector3(.041f, .045f, .014f), new Color(.96f, .95f, .9f));
                Oval("Iris", transform, new Vector3(side * .076f, 1.706f, .189f), new Vector3(.022f, .033f, .009f), eye);
                Oval("Brillo de ojo", transform, new Vector3(side * .076f - .006f, 1.72f, .196f), new Vector3(.008f, .01f, .004f), Color.white);
            }
            Oval("Nariz", transform, new Vector3(0f, 1.65f, .191f), new Vector3(.025f, .033f, .025f), skin);
            Oval("Boca", transform, new Vector3(0f, 1.59f, .173f), new Vector3(.035f, .007f, .005f), new Color(.46f, .25f, .22f));
            if (hat == 0) Shape("Gorra de explorador", transform, new Vector3(0f, 1.81f, 0f), cap,
                new Vector3(0f, 0f, 0f), new Vector3(.223f, 0f, .203f), new Vector3(.225f, .06f, .208f),
                new Vector3(.18f, .14f, .17f), new Vector3(.08f, .185f, .08f), new Vector3(0f, .19f, 0f));
            if (hat == 0) Oval("Visera", transform, new Vector3(0f, 1.82f, .18f), new Vector3(.215f, .024f, .17f), Color.Lerp(cap, Color.black, .15f));
            Shape("Pañuelo", transform, new Vector3(0f, 1.39f, 0f), red,
                new Vector3(0f, 0f, 0f), new Vector3(.12f, 0f, .115f), new Vector3(.11f, .085f, .10f), new Vector3(0f, .085f, 0f));
            var scarf = Shape("Cola del pañuelo", transform, new Vector3(-.07f, 1.42f, -.12f), red,
                new Vector3(0f, 0f, 0f), new Vector3(.085f, -.04f, .018f), new Vector3(.105f, -.24f, .018f),
                new Vector3(.06f, -.38f, .015f), new Vector3(0f, -.43f, 0f));
            scarf.localRotation = Quaternion.Euler(25f, 0f, -18f);
            scarfTail = scarf; scarfRest = scarf.localRotation;
            if (hairstyle == 1)
            {
                for (int i = -2; i <= 2; i++)
                    Oval("Cabello largo", transform, new Vector3(i * .075f, 1.48f, -.14f), new Vector3(.07f, .23f, .075f), hair);
            }
            if (hairstyle == 2)
            {
                Oval("Coleta", transform, new Vector3(0f, 1.66f, -.28f), new Vector3(.09f, .20f, .16f), hair);
                Oval("Lazo", transform, new Vector3(0f, 1.76f, -.19f), new Vector3(.10f, .035f, .04f), red);
            }
            if (hat == 1)
            {
                Shape("Pañuelo de cabeza", transform, new Vector3(0f, 1.79f, 0f), cap,
                    new Vector3(0f, 0f, 0f), new Vector3(.225f, 0f, .21f), new Vector3(.21f, .14f, .19f),
                    new Vector3(.10f, .22f, .12f), new Vector3(0f, .24f, 0f));
                Oval("Nudo del pañuelo", transform, new Vector3(.15f, 1.82f, -.17f), new Vector3(.09f, .06f, .07f), cap);
            }
            if (outfit == 1)
                Shape("Túnica de exploradora", transform, new Vector3(0f, .96f, 0f), navy,
                    new Vector3(0f, 0f, 0f), new Vector3(.235f, 0f, .17f), new Vector3(.32f, -.28f, .23f),
                    new Vector3(.33f, -.34f, .24f), new Vector3(0f, -.34f, 0f));
            if (outfit == 2)
            {
                Shape("Abrigo de sabio", transform, new Vector3(0f, 1.35f, 0f), shirt,
                    new Vector3(0f, 0f, 0f), new Vector3(.29f, 0f, .19f), new Vector3(.32f, -.35f, .23f),
                    new Vector3(.34f, -.65f, .24f), new Vector3(0f, -.65f, 0f));
                Oval("Camisa del sabio", transform, new Vector3(0f, 1.05f, .225f), new Vector3(.16f, .28f, .025f), cream);
                for (int i = 0; i < 3; i++)
                    Oval("Broche de abrigo", transform, new Vector3(0f, 1.20f - i * .14f, .253f), new Vector3(.045f, .01f, .01f), leather);
            }
            if ((appearance?.BeardStyle ?? 0) != 0)
            {
                Shape("Barba", transform, new Vector3(0f, 1.61f, .16f), hair,
                    new Vector3(0f, .03f, 0f), new Vector3(.17f, 0f, .055f), new Vector3(.14f, -.16f, .07f),
                    new Vector3(.055f, -.30f, .045f), new Vector3(0f, -.35f, 0f));
                for (int side = -1; side <= 1; side += 2)
                    Oval("Bigote", transform, new Vector3(side * .075f, 1.61f, .205f), new Vector3(.10f, .045f, .035f), hair);
            }
            if ((appearance?.GlassesStyle ?? 0) != 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Oval("Montura", transform, new Vector3(side * .09f, 1.72f, .199f), new Vector3(.085f, .06f, .018f), sole);
                    Oval("Cristal verde", transform, new Vector3(side * .09f, 1.72f, .215f), new Vector3(.069f, .045f, .01f), new Color(.13f, .24f, .16f));
                }
                Oval("Puente de gafas", transform, new Vector3(0f, 1.72f, .21f), new Vector3(.04f, .012f, .012f), sole);
            }
            if (avatar == 2)
            {
                Shape("Bastón", arms[1], new Vector3(.14f, -.48f, .08f), leather,
                    new Vector3(0f, -.78f, 0f), new Vector3(.028f, -.77f, .028f),
                    new Vector3(.035f, .77f, .035f), new Vector3(0f, .8f, 0f));
                Oval("Remate de bastón", arms[1], new Vector3(.14f, .36f, .08f), new Vector3(.07f, .12f, .06f), leather);
            }
            // Variación de proporciones del cuerpo; la ropa se puede elegir independientemente.
            if (avatar == 1) transform.localScale = new Vector3(.94f, .98f, .94f);
            if (avatar == 2) transform.localScale = new Vector3(1.15f, .95f, 1.12f);

            // Pequeña mochila de cuero: deja visible el pañuelo y la silueta de la chaqueta.
            Oval("Mochila", transform, new Vector3(.06f, 1.21f, -.21f), new Vector3(.14f, .17f, .07f), leather);
        }
    }
}
