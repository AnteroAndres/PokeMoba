using System.Collections.Generic;
using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>
    /// Límites del escenario actual (círculo para arenas, rectángulo para zonas y el mapa del MOBA)
    /// y obstáculos circulares (árboles, rocas, casas) que no se pueden atravesar.
    /// </summary>
    public static class PlayArea
    {
        struct Obstacle
        {
            public Vector3 Center;
            public float Radius;
        }

        static readonly List<Obstacle> Obstacles = new List<Obstacle>();
        static bool isCircle = true;
        static float radius = CombatTuning.ArenaRadius;
        static Vector2 halfSize;

        public static bool IsCircle => isCircle;
        public static float Radius => radius;
        public static Vector2 HalfSize => halfSize;

        public static void SetCircle(float circleRadius)
        {
            isCircle = true;
            radius = circleRadius;
            Obstacles.Clear();
        }

        public static void SetRectangle(float halfX, float halfZ)
        {
            isCircle = false;
            halfSize = new Vector2(halfX, halfZ);
            Obstacles.Clear();
        }

        public static void AddObstacle(Vector3 center, float obstacleRadius)
        {
            center.y = 0f;
            Obstacles.Add(new Obstacle { Center = center, Radius = obstacleRadius });
        }

        /// <summary>Saca el punto de los obstáculos y lo mete dentro de los límites.</summary>
        public static Vector3 Clamp(Vector3 position, float bodyRadius = CombatTuning.BodyRadius)
        {
            position.y = 0f;

            foreach (var obstacle in Obstacles)
            {
                Vector3 offset = position - obstacle.Center;
                float min = obstacle.Radius + bodyRadius;
                float distance = offset.magnitude;
                if (distance < min)
                    position = obstacle.Center + (distance > 0.0001f ? offset / distance : Vector3.forward) * min;
            }

            if (isCircle)
            {
                float limit = radius - bodyRadius;
                if (position.magnitude > limit) position = position.normalized * limit;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, -halfSize.x + bodyRadius, halfSize.x - bodyRadius);
                position.z = Mathf.Clamp(position.z, -halfSize.y + bodyRadius, halfSize.y - bodyRadius);
            }
            return position;
        }

        public static bool IsFree(Vector3 position, float clearance)
        {
            position.y = 0f;
            foreach (var obstacle in Obstacles)
                if (Vector3.Distance(position, obstacle.Center) < obstacle.Radius + clearance) return false;
            return Clamp(position, clearance) == position;
        }

        /// <summary>Punto aleatorio libre dentro del escenario (o el centro si no encuentra uno).</summary>
        public static Vector3 RandomFreePoint(float clearance, float margin = 1f)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                Vector3 point;
                if (isCircle)
                {
                    Vector2 p = Random.insideUnitCircle * (radius - margin);
                    point = new Vector3(p.x, 0f, p.y);
                }
                else
                {
                    point = new Vector3(
                        Random.Range(-halfSize.x + margin, halfSize.x - margin), 0f,
                        Random.Range(-halfSize.y + margin, halfSize.y - margin));
                }
                if (IsFree(point, clearance)) return point;
            }
            return Vector3.zero;
        }
    }
}
