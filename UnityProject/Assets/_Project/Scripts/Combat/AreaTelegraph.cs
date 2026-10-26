using System.Collections.Generic;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Ataque en área con aviso previo: se ve un círculo que se llena y al completarse golpea.</summary>
    public class AreaTelegraph : MonoBehaviour
    {
        public static readonly List<AreaTelegraph> Active = new List<AreaTelegraph>();

        public Combatant Owner { get; private set; }
        public float Radius { get; private set; }
        public float Remaining => duration - elapsed;

        MoveData move;
        Transform fill;
        float duration;
        float elapsed;

        public static AreaTelegraph Spawn(Combatant owner, MoveData move, Vector3 center)
        {
            center.y = 0f;
            var root = new GameObject($"Aviso {move.Name}");
            root.transform.position = center;

            Color color = move.Type.TypeColor();
            float diameter = move.Radius * 2f;

            var outline = Vfx.Primitive(PrimitiveType.Cylinder, "Borde", color * 0.45f);
            outline.transform.SetParent(root.transform, false);
            outline.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            outline.transform.localScale = new Vector3(diameter, 0.01f, diameter);

            var fill = Vfx.Primitive(PrimitiveType.Cylinder, "Relleno", color);
            fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            fill.transform.localScale = new Vector3(0f, 0.012f, 0f);

            var telegraph = root.AddComponent<AreaTelegraph>();
            telegraph.Owner = owner;
            telegraph.move = move;
            telegraph.Radius = move.Radius;
            telegraph.fill = fill.transform;
            telegraph.duration = Mathf.Max(0.1f, move.TelegraphTime);
            return telegraph;
        }

        public bool Contains(Vector3 position, float bodyRadius = CombatTuning.BodyRadius) =>
            CombatQueries.FlatDistance(transform.position, position) <= Radius + bodyRadius;

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float diameter = Radius * 2f * t;
            fill.localScale = new Vector3(diameter, 0.012f, diameter);

            if (t < 1f) return;

            if (Owner != null)
            {
                foreach (var target in CombatQueries.EnemiesInRadius(Owner, transform.position, Radius))
                    target.ReceiveHit(Owner, move, target.transform.position - transform.position);
            }

            Vfx.Burst(transform.position + Vector3.up * 0.5f, move.Type.TypeColor(), Radius * 2f);
            Destroy(gameObject);
        }
    }
}
