using UnityEngine;
using PokeMoba.Data;
namespace PokeMoba.Combat
{
    /// <summary>Efectos visuales del movimiento; el daño permanece en AbilityCaster/Projectile.</summary>
    public class ElementalAttackEffect : MonoBehaviour
    {
        LineRenderer line;
        Material material;
        Vector3 origin, end;
        float age;
        PokemonType type;
        bool vine;
        Transform source;
        public static Vector3 Origin(Combatant owner)
        {
            var model = owner.transform.Find("Model");
            return (model != null ? model.position : owner.transform.position) + Vector3.up * .8f;
        }
        public static void Cast(Combatant owner, MoveData move, Vector3 direction)
        {
            var go = new GameObject("Efecto " + move.Name);
            var effect = go.AddComponent<ElementalAttackEffect>();
            effect.source = owner.transform;
            effect.origin = Origin(owner);
            effect.end = effect.origin + direction * Mathf.Min(move.Range, 12f);
            effect.type = move.Type;
            string name = (move.Name ?? "").ToLowerInvariant();
            effect.vine = name.Contains("cepa") || name.Contains("vine") || name.Contains("látigo") || name.Contains("latigo");
            effect.material = new Material(Shader.Find("Sprites/Default"));
            effect.line = go.AddComponent<LineRenderer>();
            effect.line.sharedMaterial = effect.material;
            effect.line.positionCount = 25;
            effect.line.startColor = move.Type.TypeColor(); effect.line.endColor = Color.Lerp(move.Type.TypeColor(), Color.white, .35f);
            effect.line.startWidth = effect.vine ? .12f : .16f; effect.line.endWidth = .025f;
            effect.line.numCapVertices = 3;
        }
        public static void Impact(Vector3 point, MoveData move)
        {
            Vfx.Burst(point, move.Type.TypeColor(), 1.1f);
            for (int i = 0; i < 6; i++)
            {
                var shape = move.Type == PokemonType.Planta ? PrimitiveType.Capsule
                    : move.Type == PokemonType.Roca || move.Type == PokemonType.Tierra || move.Type == PokemonType.Hielo || move.Type == PokemonType.Acero
                    ? PrimitiveType.Cube : PrimitiveType.Sphere;
                var go = Vfx.Primitive(shape, "Partícula " + move.Type, move.Type.TypeColor());
                go.transform.position = point;
                var destination = point + Quaternion.Euler(0, i * 60f, 0) * new Vector3(.7f, .3f, 0);
                go.AddComponent<TimedScale>().Setup(new Vector3(.12f, .25f, .12f), Vector3.zero, .35f, destination);
            }
        }
        void Update()
        {
            if (source == null || age > .4f) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            float extend = Mathf.Sin(Mathf.Clamp01(age / .4f) * Mathf.PI);
            Vector3 tip = Vector3.Lerp(origin, end, extend);
            Vector3 side = Vector3.Cross((end - origin).normalized, Vector3.up);
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 point = Vector3.Lerp(origin, tip, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                if (vine || type == PokemonType.Planta)
                    point += Vector3.up * envelope * .7f + side * Mathf.Sin(t * 9f + age * 15f) * envelope * .2f;
                else if (type == PokemonType.Electrico)
                    point += side * (i % 2 == 0 ? .25f : -.25f) * envelope;
                else if (type == PokemonType.Agua || type == PokemonType.Volador || type == PokemonType.Hielo)
                    point += side * Mathf.Sin(t * 18f - age * 20f) * envelope * .2f;
                else if (type == PokemonType.Psiquico || type == PokemonType.Fantasma || type == PokemonType.Hada || type == PokemonType.Dragon)
                    point += (Vector3.up * Mathf.Cos(t * 20f) + side * Mathf.Sin(t * 20f)) * envelope * .25f;
                else point += Vector3.up * envelope * Mathf.Sin(t * 12f + age * 20f) * .18f;
                line.SetPosition(i, point);
            }
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
