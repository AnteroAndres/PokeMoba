using System.Collections.Generic;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Combat
{
    public class Projectile : MonoBehaviour
    {
        public static readonly List<Projectile> Active = new List<Projectile>();

        public Combatant Owner { get; private set; }
        public Vector3 Direction { get; private set; }
        public float HitRadius { get; private set; }

        MoveData move;
        float traveled;
        readonly HashSet<Combatant> dodgedBy = new HashSet<Combatant>();

        public static Projectile Spawn(Combatant owner, MoveData move, Vector3 origin, Vector3 direction)
        {
            var go = Vfx.Primitive(PrimitiveType.Sphere, move.Name, move.Type.TypeColor());
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * move.Radius * 2f;

            var projectile = go.AddComponent<Projectile>();
            projectile.Owner = owner;
            projectile.move = move;
            projectile.Direction = direction.normalized;
            projectile.HitRadius = move.Radius;
            return projectile;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            if (Owner == null)
            {
                Destroy(gameObject);
                return;
            }

            float step = move.ProjectileSpeed * Time.deltaTime;
            transform.position += Direction * step;
            traveled += step;

            foreach (var target in CombatQueries.EnemiesInRadius(Owner, transform.position, HitRadius))
            {
                if (dodgedBy.Contains(target)) continue;

                if (target.ReceiveHit(Owner, move, Direction))
                {
                    ElementalAttackEffect.Impact(ElementalAttackEffect.Origin(target), move);
                    Destroy(gameObject);
                    return;
                }

                // Lo esquivó: el proyectil sigue de largo.
                dodgedBy.Add(target);
            }

            if (traveled >= move.Range)
            {
                Vfx.Burst(transform.position, move.Type.TypeColor(), 0.6f);
                Destroy(gameObject);
            }
        }
    }
}
