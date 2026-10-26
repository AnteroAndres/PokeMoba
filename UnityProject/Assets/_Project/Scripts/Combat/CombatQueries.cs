using System.Collections.Generic;
using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Búsquedas de objetivos. Devuelven copias para poder aplicar golpes sin modificar Combatant.All mientras se recorre.</summary>
    public static class CombatQueries
    {
        public static bool IsEnemy(Combatant self, Combatant other) =>
            other != null && other != self && !other.IsDead && other.Team != self.Team;

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public static Combatant NearestEnemy(Combatant self, float maxDistance = float.MaxValue)
        {
            Combatant best = null;
            float bestDistance = maxDistance;
            foreach (var other in Combatant.All)
            {
                if (!IsEnemy(self, other)) continue;
                float d = FlatDistance(self.transform.position, other.transform.position) - other.Radius;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = other;
                }
            }
            return best;
        }

        public static List<Combatant> EnemiesInRadius(Combatant self, Vector3 center, float radius)
        {
            var result = new List<Combatant>();
            foreach (var other in Combatant.All)
            {
                if (IsEnemy(self, other) && FlatDistance(center, other.transform.position) <= radius + other.Radius)
                    result.Add(other);
            }
            return result;
        }

        public static List<Combatant> EnemiesInCone(Combatant self, Vector3 origin, Vector3 direction, float range, float angleDegrees)
        {
            var result = new List<Combatant>();
            direction.y = 0f;
            foreach (var other in Combatant.All)
            {
                if (!IsEnemy(self, other)) continue;
                Vector3 offset = other.transform.position - origin;
                offset.y = 0f;
                if (offset.magnitude > range + other.Radius) continue;
                if (offset.magnitude > other.Radius && Vector3.Angle(direction, offset) > angleDegrees * 0.5f) continue;
                result.Add(other);
            }
            return result;
        }
    }
}
